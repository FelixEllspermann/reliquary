using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>
    /// WebSocket-Verbindung zum Rouge-TCG-Relay-Server. Empfang läuft auf einem Hintergrund-Task,
    /// alle Events feuern im Update auf dem Main-Thread. Überlebt Szenenwechsel (DontDestroyOnLoad).
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        /// <summary>Die Haupt-Instanz (erste erzeugte). Weitere Instanzen (z.B. Test-Bots) sind erlaubt.</summary>
        public static NetworkManager Instance { get; private set; }

        [Header("Server")]
        [Tooltip("Adresse des Relay-Servers")]
        [SerializeField] private string serverUrl = "ws://217.154.212.82:7777";

        [Header("Testserver (wirkt NUR im Editor)")]
        [Tooltip("Im Editor gegen die Testinstanz (7778) spielen statt gegen Produktion. " +
                 "In Builds wird der Haken ignoriert — vergessen kann ihn niemand ausliefern.")]
        [SerializeField] private bool useTestServer;
        [SerializeField] private string testServerUrl = "ws://217.154.212.82:7778";

        [Tooltip("Diese Instanz bleibt über Szenenwechsel erhalten")]
        [SerializeField] private bool persistAcrossScenes = true;

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
        public bool PeerLeft { get; private set; }
        public string ServerUrl { get => serverUrl; set => serverUrl = value; }

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<NetMessage> OnMessage;

        private ClientWebSocket socket;
        private CancellationTokenSource cancel;
        private readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
        private readonly Queue<NetData> answerQueue = new Queue<NetData>();

        /// <summary>
        /// Puffer für Server-Duell-Nachrichten. Der DuelHost schickt seine Eröffnung
        /// (Münzwurf + Startwahl-Anfrage) SOFORT beim Match — der Client steckt da
        /// aber noch im „Match found"-Popup und Szenenwechsel, der ServerDuelClient
        /// existiert noch gar nicht. OnMessage feuert nur an aktuelle Abonnenten;
        /// ohne Puffer ging genau diese Eröffnung verloren, der Server wartete ewig
        /// auf die Antwort, und beide Spieler sahen ein totes Brett.
        /// </summary>
        private readonly Queue<NetMessage> sduelInbox = new Queue<NetMessage>();
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private bool connectedFlagPending;
        private string disconnectReason;
        private int answerSeq;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (persistAcrossScenes && Instance == this) DontDestroyOnLoad(gameObject);

            // Der Testserver-Haken wirkt NUR im Editor. Application.isEditor
            // statt #if UNITY_EDITOR, damit die Prüfung im Build zwar mitkommt,
            // dort aber immer false ist — ein vergessener Haken in der Szene
            // kann so nie einen Build auf die Testinstanz schicken.
            if (useTestServer && Application.isEditor && !string.IsNullOrEmpty(testServerUrl))
            {
                serverUrl = testServerUrl;
                Debug.LogWarning($"NetworkManager: TESTSERVER aktiv — {serverUrl}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Disconnect();
        }

        public async void Connect()
        {
            if (IsConnected) { connectedFlagPending = true; return; }
            try
            {
                PeerLeft = false;
                answerQueue.Clear();
                answerSeq = 0;
                cancel = new CancellationTokenSource();
                socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri(serverUrl), cancel.Token);
                connectedFlagPending = true;
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                disconnectReason = "Connection failed: " + e.Message;
                socket = null;
            }
        }

        public void Disconnect()
        {
            try { cancel?.Cancel(); } catch { }
            try { socket?.Dispose(); } catch { }
            socket = null;
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[64 * 1024];
            var segment = new ArraySegment<byte>(buffer);
            try
            {
                while (socket != null && socket.State == WebSocketState.Open)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(segment, cancel.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            disconnectReason = "The server closed the connection.";
                            return;
                        }
                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);
                    inbox.Enqueue(builder.ToString());
                }
            }
            catch (Exception e)
            {
                disconnectReason = "Connection lost: " + e.Message;
            }
        }

        private void Update()
        {
            if (connectedFlagPending)
            {
                connectedFlagPending = false;
                OnConnected?.Invoke();
            }
            if (disconnectReason != null)
            {
                string reason = disconnectReason;
                disconnectReason = null;
                OnDisconnected?.Invoke(reason);
            }

            while (inbox.TryDequeue(out var raw))
            {
                NetMessage message;
                try { message = JsonUtility.FromJson<NetMessage>(raw); }
                catch { continue; }
                if (message == null) continue;

                if (message.t == "peer_left") PeerLeft = true;

                // Server-Duell: puffern statt feuern — die Duel-Szene holt sie
                // per TryDequeueSduel ab, egal wie lange Popup und Laden dauern.
                if (message.t == "sduel") { sduelInbox.Enqueue(message); continue; }
                // Ein neues Duell beginnt: Reste eines vorigen gehören nicht hinein.
                if (message.t == "sduel_start") sduelInbox.Clear();

                // Ein Hauptrang-Aufstieg wird gemerkt, nicht sofort gezeigt —
                // erst muss der Ergebnis-Bildschirm durch sein.
                if (message.t == "rank_change") PlayerProfile.QueueRpDelta(message.rankDelta);
                if (message.t == "rank_change" && message.rankUp)
                    PlayerProfile.QueueRankUp(new PlayerProfile.RankUp
                    {
                        From = message.rankFromValue,
                        Into = message.rankValue,
                        Gain = message.rankDelta,
                        Opponent = string.IsNullOrEmpty(MatchContext.RemoteName)
                            ? MatchContext.BotName : MatchContext.RemoteName,
                    });

                if (message.t == "relay" && message.data != null && message.data.t == "answer")
                    answerQueue.Enqueue(message.data);
                // Jede Nachricht, die ein Profil mitbringt, aktualisiert den Kontostand
                if (message.profile != null && (message.t == "auth_ok" || message.t == "profile"
                    || message.t == "pack_result" || message.t == "craft_result"))
                    PlayerProfile.Apply(message.profile);

                OnMessage?.Invoke(message);
            }
        }

        public bool TryDequeueAnswer(out NetData data)
        {
            if (answerQueue.Count > 0) { data = answerQueue.Dequeue(); return true; }
            data = null;
            return false;
        }

        /// <summary>Nächste gepufferte Server-Duell-Nachricht (siehe sduelInbox).</summary>
        public bool TryDequeueSduel(out NetMessage message)
        {
            if (sduelInbox.Count > 0) { message = sduelInbox.Dequeue(); return true; }
            message = null;
            return false;
        }

        // ---- Senden ----

        private async void SendJson(object payload)
        {
            if (!IsConnected) return;
            var json = JsonUtility.ToJson(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await sendLock.WaitAsync();
            try
            {
                if (IsConnected)
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception e) { disconnectReason = "Send failed: " + e.Message; }
            finally { sendLock.Release(); }
        }

        public void SendHello(string playerName) =>
            SendJson(new NetMessage { t = "hello", name = playerName, sduel = true });

        /// <summary>Antwort auf einen Server-Duell-Request (Intent).</summary>
        public void SendSduelIntent(SduelAnswer answer) =>
            SendJson(new NetMessage { t = "sduel_intent", answer = answer });
        public void SendRegister(string name, string pass) => SendJson(new NetMessage { t = "register", name = name, pass = pass });
        public void SendLogin(string name, string pass) => SendJson(new NetMessage { t = "login", name = name, pass = pass });

        /// <summary>Anmeldung über Steam — der Server prüft das Ticket bei Valve.</summary>
        public void SendSteamAuth(string ticket, string personaName) =>
            SendJson(new NetMessage { t = "steam_auth", steamTicket = ticket, steamName = personaName });

        // Steam-Accounts und Passwort-Accounts sind getrennt — es gibt bewusst keine
        // Verknüpfung und kein nachträgliches Passwort.

        public void SendOpenPack(string packName, int count = 1) =>
            SendJson(new NetMessage { t = "open_pack", pack = packName, packCount = count });
        public void SendBuyPack(string packName, int count = 1) =>
            SendJson(new NetMessage { t = "buy_pack", pack = packName, packCount = Mathf.Clamp(count, 1, 10) });
        public void SendSaveDeck(int index, RuntimeDeck deck) => SendJson(new NetMessage
        {
            t = "save_deck",
            deckIndex = index,
            deck = new NetDeck
            {
                name = deck.Name,
                hero = deck.Hero,
                cards = deck.Cards.ToArray(),
                extra = deck.Extra.ToArray(),
                cardFinishes = FinishArray(deck.CardFinishes, deck.Cards.Count),
                extraFinishes = FinishArray(deck.ExtraFinishes, deck.Extra.Count)
            }
        });

        /// <summary>Finish-Liste als Zahlen, immer genau so lang wie die Kartenliste.</summary>
        private static int[] FinishArray(System.Collections.Generic.List<CardFinish> finishes, int count)
        {
            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = i < finishes.Count ? (int)finishes[i] : 0;
            return result;
        }
        /// <summary>Startdeck wählen. Der Server vergibt es genau einmal pro Konto.</summary>
        public void SendClaimStarter(string deckId) =>
            SendJson(new NetMessage { t = "claim_starter", starter = deckId });

        public void SendDeleteDeck(int index) => SendJson(new NetMessage { t = "delete_deck", deckIndex = index });
        public void SendCraft(string cardName) => SendJson(new NetMessage { t = "craft", card = cardName });

        /// <summary>
        /// Zerlegen. Das Finish muss mit — schlichte Exemplare geben Staub,
        /// Sonderexemplare geben Coins, und niemand soll aus Versehen ein
        /// Static für Staub verlieren.
        /// </summary>
        public void SendDust(string cardName, CardFinish finish = CardFinish.Plain) =>
            SendJson(new NetMessage { t = "dust", card = cardName, finish = (int)finish });

        public void SendBuyCosmetic(string itemId) =>
            SendJson(new NetMessage { t = "buy_cosmetic", item = itemId });

        public void SendEquipCosmetic(string slot, string itemId) =>
            SendJson(new NetMessage { t = "equip_cosmetic", slot = slot, item = itemId });
        public void SendDuelResult(bool won) => SendJson(new NetMessage { t = "duel_result", won = won });
        public void SendSoloResult(bool won) => SendJson(new NetMessage
        {
            t = "solo_result", won = won,
            // Das gespielte Deck für die Statistik — Solo, Turm und Draft füllen
            // den MatchContext, bevor die Duel-Szene lädt.
            deckName = string.IsNullOrEmpty(MatchContext.LocalDeckName) ? "Deck" : MatchContext.LocalDeckName,
            deckHero = MatchContext.LocalHero ?? "",
            deckCards = MatchContext.LocalDeckCards != null ? MatchContext.LocalDeckCards.ToArray() : new string[0],
            deckExtra = MatchContext.LocalExtraCards != null ? MatchContext.LocalExtraCards.ToArray() : new string[0],
            opponent = MatchContext.RemoteName ?? "Bot"
        });

        /// <summary>Fragt die Deck-Statistiken ab; die Antwort kommt als OnMessage mit t == "stats_decks".</summary>
        public void RequestDeckStats() => SendJson(new NetMessage { t = "stats_decks" });

        /// <summary>Karten-Statistiken (Winrate je Karte); Antwort: t == "stats_cards".</summary>
        public void RequestCardStats() => SendJson(new NetMessage { t = "stats_cards" });

        /// <summary>Meldet dem Server, dass eine "neue" Karte angeklickt wurde — das NEW-Badge verfällt.</summary>
        public void SendSeenCard(string cardName) =>
            SendJson(new NetMessage { t = "seen_card", card = cardName });

        /// <summary>Die häufigsten Deck-Partner einer Karte; Antwort: t == "stats_card_detail".</summary>
        public void RequestCardDetail(string cardName) =>
            SendJson(new NetMessage { t = "stats_card_detail", card = cardName });

        /// <summary>Match-Historie + Bilanzen + Live-Spiele; Antwort: t == "profile_stats".</summary>
        public void RequestProfileStats() => SendJson(new NetMessage { t = "profile_stats" });

        /// <summary>Liste der laufenden Duelle; Antwort: t == "watch_list" mit liveGames.</summary>
        public void RequestWatchList() => SendJson(new NetMessage { t = "watch_list" });

        /// <summary>Tritt einem laufenden Duell als Zuschauer bei (danach kommen sduel-Nachrichten).</summary>
        public void SendSpectate(string duelId) => SendJson(new NetMessage { t = "spectate", duelId = duelId });

        /// <summary>Verlässt den Zuschauer-Modus wieder.</summary>
        public void SendSpectateLeave() => SendJson(new NetMessage { t = "spectate_leave" });

        /// <summary>Speichert die bis zu 3 Schaufenster-Karten des Profils.</summary>
        public void SendSetShowcase(ShowcaseCard[] cards) =>
            SendJson(new NetMessage { t = "set_showcase", showcase = cards ?? new ShowcaseCard[0] });

        /// <summary>Meldet den ersten Sieg auf einer Turm-Ebene (Server prüft die Reihenfolge).</summary>
        public void SendTowerProgress(int floor) => SendJson(new NetMessage { t = "tower_progress", floor = floor });

        // ---- Draft-Modus (Challenges) ----
        public void SendDraftStart() => SendJson(new NetMessage { t = "draft_start" });

        /// <summary>Das Draft-Deck — validiert der Server nur gegen den gezogenen Pool.</summary>
        public void SendDraftSaveDeck(RuntimeDeck deck) => SendJson(new NetMessage
        {
            t = "draft_save_deck",
            deck = new NetDeck
            {
                name = "Draft",
                hero = deck.Hero,
                cards = deck.Cards.ToArray(),
                extra = deck.Extra.ToArray()
            }
        });

        public void SendDraftProgress(int floor) => SendJson(new NetMessage { t = "draft_progress", floor = floor });

        public void SendDraftAbandon() => SendJson(new NetMessage { t = "draft_abandon" });

        /// <summary>Eine Niederlage beendet den Draft-Lauf — Pool und Deck sind weg.</summary>
        public void SendDraftDefeat() => SendJson(new NetMessage { t = "draft_defeat" });
        public void SendClaimDaily() => SendJson(new NetMessage { t = "claim_daily" });

        /// <summary>Spieler-Feedback an den Server (landet dort in data/feedback.jsonl).</summary>
        public void SendFeedback(string text) => SendJson(new NetMessage
        {
            t = "feedback",
            msg = text,
            card = Application.version   // Build-Version mitschicken, damit Meldungen zuordenbar sind
        });
        public void SendQueue(int deckIndex = 0) => SendJson(new NetMessage { t = "queue", deckIndex = deckIndex });
        public void SendCreate() => SendJson(new NetMessage { t = "create" });
        public void SendJoin(string code) => SendJson(new NetMessage { t = "join", code = code });
        public void SendLeave() { PeerLeft = false; SendJson(new NetMessage { t = "leave" }); }
        public void SendRelay(NetData data) => SendJson(new NetMessage { t = "relay", data = data });

        /// <summary>Serialisiert und sendet die Antwort auf einen beantworteten Request (Lockstep).</summary>
        public void SendAnswer(DuelRequest request, string check)
        {
            var data = new NetData { t = "answer", seq = answerSeq++, check = check };
            switch (request)
            {
                case StartChoiceRequest start:
                    data.kind = "start";
                    data.result = start.Result;
                    break;
                case MainActionRequest main:
                    data.kind = "main";
                    data.chosen = main.Chosen;
                    data.zone = main.Chosen >= 0 && main.Chosen < main.Options.Count
                        ? main.Options[main.Chosen].PreferredZoneIndex : -1;
                    break;
                case BattleActionRequest battle:
                    data.kind = "battle";
                    data.chosen = battle.Chosen;
                    break;
                case YesNoRequest yesNo:
                    data.kind = "yesno";
                    data.result = yesNo.Result;
                    break;
                case OptionRequest option:
                    data.kind = "option";
                    data.chosen = option.Result;
                    break;
                case ZoneSelectRequest zoneSelect:
                    data.kind = "zone";
                    data.chosen = zoneSelect.Result;
                    break;
                case TargetRequest target:
                    data.kind = "target";
                    data.cancelled = target.Cancelled;
                    var indices = new List<int>();
                    foreach (var picked in target.Result)
                    {
                        int index = target.Candidates.IndexOf(picked);
                        if (index >= 0) indices.Add(index);
                    }
                    data.indices = indices.ToArray();
                    break;
            }
            SendRelay(data);
        }
    }
}
