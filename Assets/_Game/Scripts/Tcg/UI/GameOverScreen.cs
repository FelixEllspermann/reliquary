using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>Sieg-/Niederlage-Overlay am Duell-Ende (aus Sicht von Spieler 1).</summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("duel")]
        private DuelHost duelHost;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button deckEditorButton;

        [Header("Reliquary-Stil")]
        [SerializeField] private Image panelFrame;     // relicFrame — Sieg gold, Niederlage ember getönt
        [SerializeField] private Image[] rivets = new Image[4];
        [SerializeField] private Image emblemOuter;
        [SerializeField] private Image emblemInner;
        [SerializeField] private Image emblemCore;
        [SerializeField] private TMP_Text eyebrowText;
        [SerializeField] private GameObject rewardStrip;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text restartLabel;
        [SerializeField] private GoldShimmerText titleShimmer;

        [Header("Einstellungen")]
        [Tooltip("Verzögerung, bevor der Screen erscheint (Sekunden)")]
        [Range(0f, 5f)]
        [SerializeField] private float showDelay = 1.2f;

        [Tooltip("Einblendzeit")]
        [Range(0.05f, 2f)]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("Name der Hauptmenü-Szene")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        /// <summary>Die Engine hinter dem Host.</summary>
        private DuelManager duel => duelHost != null ? duelHost.Duel : null;

        private void Awake()
        {
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (deckEditorButton != null) deckEditorButton.onClick.AddListener(OpenDeckEditor);
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (duel != null)
            {
                duel.OnDuelEnded += HandleDuelEnded;
                duel.OnLifeChanged += HandleLifeChanged;
            }
        }

        private void OnDisable()
        {
            if (duel != null)
            {
                duel.OnDuelEnded -= HandleDuelEnded;
                duel.OnLifeChanged -= HandleLifeChanged;
            }
        }

        // Der letzte Treffer auf den eigenen Helden — die Niederlage-Sequenz zeigt
        // ihn als Zahl und lässt die Lebensanzeige von dort leerlaufen.
        private int lastLifeBeforeHit = 400;
        private int lastHit = 1;

        private void HandleLifeChanged(PlayerState player, int delta)
        {
            var local = duel != null ? (duel.LocalPlayer ?? duel.Player1) : null;
            if (player != local || delta >= 0) return;
            lastHit = -delta;
            lastLifeBeforeHit = player.LifePoints - delta;   // delta ist negativ
        }

        private void HandleDuelEnded(DuelResult result)
        {
            StartCoroutine(ShowRoutine(result));
        }

        private IEnumerator ShowRoutine(DuelResult result)
        {
            var local = duel.LocalPlayer != null ? duel.LocalPlayer : duel.Player1;
            bool localIsPlayer1 = local == duel.Player1;
            bool victory = (result == DuelResult.Player1Wins) == localIsPlayer1;
            string opponent = local != null && local.Opponent != null ? local.Opponent.Name : "the opponent";
            var loser = victory ? local.Opponent : local;

            // ---- 1+2: Der finale Schlag darf ausklingen, die LP ticken zu Ende.
            // Keine feste Wartezeit: die Sequenz beginnt, wenn die Null wirklich
            // auf dem Schirm steht — vorher wirkt jedes Ende abgehackt.
            yield return new WaitForSeconds(Mathf.Max(0.35f, showDelay * 0.3f));
            var board = FindAnyObjectByType<DuelBoardRenderer>();
            float safety = 4f;
            while (board != null && !board.LpSettled && safety > 0f)
            {
                safety -= Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.45f);   // kurzer Halt auf der Null

            // ---- 3: Die Spielerkarte des Verlierers zerspringt — auf BEIDEN
            // Clients dasselbe Bild, an derselben Stelle des Bretts.
            var presenter = duelHost != null ? duelHost.ScenePresenter : null;
            if (presenter != null && loser != null)
                yield return presenter.ShowPlayerCardShatter(loser);

            // ---- 4: Das Siegel des SIEGERS, für beide. Der Sieger liest
            // VICTORY, der Verlierer LOSS — Sieg und Unterschrift gehören dem,
            // der gewonnen hat. Ohne Ausrüstung läuft still das Grundsiegel
            // (Solo-Bots tragen keines).
            bool sealDone = false;
            if (victory)
                VictorySealSequence.Play(
                    Rouge.Tcg.Net.Cosmetics.EquippedIn("victorySeal"),
                    opponent, duel.TurnNumber,
                    Rouge.Tcg.Net.PlayerProfile.TakeRpDelta(),
                    () => sealDone = true);
            else
                VictorySealSequence.PlayForLoser(
                    Rouge.Tcg.Net.MatchContext.RemoteEquipped("victorySeal"),
                    opponent, duel.TurnNumber, () => sealDone = true);
            while (!sealDone) yield return null;

            // Sieg = Gold mit Shimmer, Niederlage = Ember
            Color accent = victory ? new Color32(0xC8, 0xA4, 0x5C, 0xFF) : new Color32(0xC9, 0x7A, 0x5C, 0xFF);
            Color bright = victory ? new Color32(0xF3, 0xDD, 0xA4, 0xFF) : new Color32(0xE0, 0x60, 0x3A, 0xFF);
            if (titleText != null)
            {
                // Zuschauer und Replays haben keine Seite — VICTORY/DEFEAT wäre
                // die Perspektive von Spieler A und damit irreführend.
                titleText.text = Rouge.Tcg.Net.MatchContext.SpectateMode
                    ? "DUEL OVER" : victory ? "VICTORY" : "DEFEAT";
                titleText.color = bright;
            }
            if (titleShimmer != null) titleShimmer.enabled = victory;
            if (panelFrame != null) panelFrame.color = victory ? Color.white : new Color(1f, 0.62f, 0.5f, 1f);
            foreach (var rivet in rivets) if (rivet != null) rivet.color = accent;
            if (emblemOuter != null) emblemOuter.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            if (emblemInner != null) emblemInner.color = new Color(accent.r, accent.g, accent.b, 0.35f);
            if (emblemCore != null) emblemCore.color = bright;
            if (eyebrowText != null) eyebrowText.text = victory ? "THE VAULT REMEMBERS YOUR NAME" : "THE DUEL IS SEALED";

            bool online = Rouge.Tcg.Net.PlayerProfile.LoggedIn
                && Rouge.Tcg.Net.NetworkManager.Instance != null && Rouge.Tcg.Net.NetworkManager.Instance.IsConnected;
            bool spectating = Rouge.Tcg.Net.MatchContext.SpectateMode;
            string reward = null;
            if (spectating)
            {
                // Zuschauer und Replays: kein "you", keine Coins — es war nicht ihr Duell.
                reward = null;
            }
            else if (Rouge.Tcg.Net.MatchContext.IsServerMatch)
            {
                // Server-Duell: die Belohnung hat der Server bereits autoritativ gutgeschrieben
                reward = victory ? "+200 COINS SEALED INTO YOUR VAULT" : "+100 COINS FOR THE DUEL";
            }
            else if (online)
            {
                Rouge.Tcg.Net.NetworkManager.Instance.SendSoloResult(victory);
                reward = "+50 COINS SEALED INTO YOUR VAULT";

                // Turm-Duell: Erstsieg meldet die Ebene (Server vergibt 5 Packs +
                // ggf. Titel und prüft die Reihenfolge — Doppelmeldungen verpuffen).
                int towerFloor = Rouge.Tcg.Net.MatchContext.TowerFloor;
                if (towerFloor > 0 && !victory && Rouge.Tcg.Net.MatchContext.DraftRun)
                {
                    // Draft-Regel: eine Niederlage beendet den Lauf. Der Server
                    // löst Pool und Deck auf; der nächste Draft zieht frisch.
                    Rouge.Tcg.Net.NetworkManager.Instance.SendDraftDefeat();
                    reward = "THE DRAFT DISSOLVES — DRAW ANEW";
                }
                else if (towerFloor > 0 && victory && Rouge.Tcg.Net.MatchContext.DraftRun)
                {
                    // Draft-Turm: eigener Fortschritt, Belohnung erst ganz oben —
                    // Ebene 15 bringt 10 Packs (jedes Mal) und beim ersten Mal den Titel.
                    Rouge.Tcg.Net.MatchContext.TowerWon = true;
                    Rouge.Tcg.Net.NetworkManager.Instance.SendDraftProgress(towerFloor);
                    if (towerFloor >= 15)
                        reward = Rouge.Tcg.Net.PlayerProfile.DraftClears == 0
                            ? "THE DRAFT IS CONQUERED — +10 RELIC PACKS & A NEW TITLE"
                            : "THE DRAFT IS CONQUERED — +10 RELIC PACKS";
                }
                else if (towerFloor > 0 && victory)
                {
                    Rouge.Tcg.Net.MatchContext.TowerWon = true;
                    bool firstClear = towerFloor > Rouge.Tcg.Net.PlayerProfile.TowerFloor;
                    Rouge.Tcg.Net.NetworkManager.Instance.SendTowerProgress(towerFloor);
                    if (firstClear) reward = "+5 RELIC PACKS — THE SEAL IS RENEWED";
                }
            }
            if (rewardStrip != null) rewardStrip.SetActive(reward != null);
            if (rewardText != null && reward != null) rewardText.text = reward;

            if (subtitleText != null)
                subtitleText.text = spectating
                    ? $"{(victory ? Rouge.Tcg.Net.MatchContext.LocalName : Rouge.Tcg.Net.MatchContext.RemoteName)} wins the duel."
                    : victory ? $"You defeated {opponent}." : $"{opponent} defeated you.";

            // ---- 5: Der Continue-Knopf. Online gibt es genau einen Weg (weiter,
            // ggf. durch den Rank-Up, dann Menü); Solo behält "nochmal".
            bool network = Rouge.Tcg.Net.MatchContext.IsServerMatch;
            bool towerReturn = Rouge.Tcg.Net.MatchContext.TowerFloor > 0 && victory;
            // Draft: Sieg wie Niederlage führen zurück in die Draft-Szene — nach
            // dem Sieg wartet dort die nächste Ebene, nach der Niederlage der
            // Neuanfang ("nochmal dasselbe Duell" gibt es nicht, das Deck ist weg).
            bool draftReturn = Rouge.Tcg.Net.MatchContext.TowerFloor > 0 && Rouge.Tcg.Net.MatchContext.DraftRun;
            if (restartLabel != null)
                restartLabel.text = network ? "CONTINUE"
                    : draftReturn && victory ? "RETURN TO THE DRAFT"
                    : draftReturn ? "BACK TO THE DRAFT"
                    : towerReturn ? "RETURN TO THE TOWER" : "DUEL AGAIN";
            if (deckEditorButton != null) deckEditorButton.gameObject.SetActive(!network);

            // Online-Nachspiel: Gegner anfragen und das Match als Replay sichern —
            // nur für Mitspieler, Zuschauer haben hier nichts zu speichern.
            if (network && !Rouge.Tcg.Net.MatchContext.SpectateMode)
                BuildOnlineExtras(opponent);

            if (panelGroup == null) yield break;
            panelGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                panelGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            panelGroup.alpha = 1f;
        }

        private void Restart()
        {
            if (Rouge.Tcg.Net.MatchContext.IsServerMatch)
            {
                LeaveNetworkMatch();
                // ---- 6+7: steht ein Aufstieg an, laeuft er jetzt — mit eigenem
                // Continue am Ende — und erst dann faellt das Hauptmenue.
                AfterRankUp(() => SceneManager.LoadScene(mainMenuSceneName));
                return;
            }
            // Draft: in beiden Fällen zurück in die Draft-Szene. Nach dem Sieg
            // wartet dort die nächste Ebene — nach der Niederlage der Start
            // eines frischen Drafts (der Lauf ist vorbei, das Deck aufgelöst).
            if (Rouge.Tcg.Net.MatchContext.DraftRun && Rouge.Tcg.Net.MatchContext.TowerFloor > 0)
            {
                AfterRankUp(() => SceneManager.LoadScene("DraftBuilder"));
                return;
            }
            // Turm-Sieg: zurück in den Turm (Play-Szene, Tower-Tab) — die Ebene
            // ist versiegelt, „nochmal" ergäbe hier keinen Sinn. TowerWon bleibt
            // gesetzt, damit der Turm die Siegzeile des Keepers zeigen kann.
            if (Rouge.Tcg.Net.MatchContext.TowerFloor > 0 && Rouge.Tcg.Net.MatchContext.TowerWon)
            {
                DuelSetupController.OpenTower = true;
                AfterRankUp(() => SceneManager.LoadScene("Play"));
                return;
            }
            // Solo/Offline: auch hier erst der Aufstieg (Solo-Siege geben RP),
            // dann dieselbe Aufstellung nochmal (MatchContext bleibt erhalten)
            string sceneName = gameObject.scene.name;
            AfterRankUp(() => SceneManager.LoadScene(sceneName));
        }

        private void OpenDeckEditor()
        {
            if (Rouge.Tcg.Net.MatchContext.IsServerMatch) LeaveNetworkMatch();
            else Rouge.Tcg.Net.MatchContext.Clear();
            AfterRankUp(() => SceneManager.LoadScene(mainMenuSceneName));
        }

        /// <summary>
        /// Steht ein Aufstieg an, läuft er erst — dann geht es weiter. Der
        /// Ergebnis-Bildschirm ist zu diesem Zeitpunkt bereits gelesen, und die
        /// Sequenz bringt ihren eigenen Ergebnis-Auftakt mit.
        /// </summary>
        private static void AfterRankUp(System.Action next)
        {
            var rankUp = Rouge.Tcg.Net.PlayerProfile.TakeRankUp();
            if (rankUp == null) { next(); return; }
            RankUpSequence.Play(rankUp.From, rankUp.Into, rankUp.Gain, rankUp.Opponent, next);
        }

        private static void LeaveNetworkMatch()
        {
            Rouge.Tcg.Net.MatchContext.Clear();
            if (Rouge.Tcg.Net.NetworkManager.Instance != null)
                Rouge.Tcg.Net.NetworkManager.Instance.SendLeave();
        }

        // ================== ONLINE-NACHSPIEL (Freund + Replay) ==================

        private TMP_Text friendExtraLabel, replayExtraLabel;
        private Button friendExtraButton, replayExtraButton;
        private bool listeningNet;

        /// <summary>Zwei Klone des Deck-Editor-Knopfs: ADD FRIEND und SAVE REPLAY.</summary>
        private void BuildOnlineExtras(string opponent)
        {
            if (deckEditorButton == null || friendExtraButton != null) return;
            var net = Rouge.Tcg.Net.NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;

            Button Clone(string label, Vector2 shift, out TMP_Text labelOut)
            {
                var go = Instantiate(deckEditorButton.gameObject, deckEditorButton.transform.parent);
                go.SetActive(true);
                var rect = (RectTransform)go.transform;
                rect.anchoredPosition = ((RectTransform)deckEditorButton.transform).anchoredPosition + shift;
                var text = go.GetComponentInChildren<TMP_Text>();
                labelOut = text;
                if (text != null) text.text = label;
                var button = go.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();   // Klon-Events gehören dem Original
                return button;
            }

            // Symmetrisch um die Position des Deck-Editor-Knopfs, statt rechts hinauszuragen
            float width = ((RectTransform)deckEditorButton.transform).sizeDelta.x;
            friendExtraButton = Clone(Loc.T("ADD FRIEND"), new Vector2(-(width * 0.5f + 7f), 0f), out friendExtraLabel);
            replayExtraButton = Clone(Loc.T("SAVE REPLAY"), new Vector2(width * 0.5f + 7f, 0f), out replayExtraLabel);

            friendExtraButton.onClick.AddListener(() =>
            {
                net.SendFriendRequest(opponent);
                friendExtraButton.interactable = false;
            });
            replayExtraButton.onClick.AddListener(() =>
            {
                net.SendReplaySave();
                replayExtraButton.interactable = false;
            });

            net.OnMessage += HandleNet;
            listeningNet = true;
        }

        /// <summary>Rückmeldungen des Servers in die Knopf-Beschriftungen spiegeln.</summary>
        private void HandleNet(Rouge.Tcg.Net.NetMessage m)
        {
            if (m == null) return;
            if (m.t == "replay_saved" && replayExtraLabel != null)
                replayExtraLabel.text = Loc.T("REPLAY SAVED");
            else if (m.t == "friend_event" && friendExtraLabel != null)
            {
                if (m.kind == "sent") friendExtraLabel.text = Loc.T("REQUEST SENT");
                else if (m.kind == "accepted") friendExtraLabel.text = Loc.T("FRIENDS NOW");
            }
            else if (m.t == "error" && !string.IsNullOrEmpty(m.msg))
            {
                // Nur die beiden eigenen Fehlerbilder abfangen — fremde Fehler
                // gehören nicht auf diese Knöpfe.
                if (m.msg.Contains("Replay slots full") && replayExtraLabel != null)
                    replayExtraLabel.text = Loc.T("SLOTS FULL — SEE PROFILE");
                else if (m.msg.Contains("already friends") && friendExtraLabel != null)
                    friendExtraLabel.text = Loc.T("ALREADY FRIENDS");
                else if (m.msg.Contains("Request already sent") && friendExtraLabel != null)
                    friendExtraLabel.text = Loc.T("REQUEST SENT");
            }
        }

        private void OnDestroy()
        {
            if (listeningNet && Rouge.Tcg.Net.NetworkManager.Instance != null)
                Rouge.Tcg.Net.NetworkManager.Instance.OnMessage -= HandleNet;
        }
    }
}
