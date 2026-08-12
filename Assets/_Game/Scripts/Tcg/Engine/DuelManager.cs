using System;
using System.Collections;
using System.Collections.Generic;

namespace Rouge.Tcg
{
    public enum DuelResult { None, Player1Wins, Player2Wins }
    public enum ControllerKind { Human, Bot }

    /// <summary>Konfiguration eines Duells — im Client vom DuelHost (Szene) befüllt, später vom Server.</summary>
    public class DuelConfig
    {
        public GameRules Rules;
        public CardCatalog Catalog;             // löst Kartennamen aus dem Netzwerk in Definitionen auf
        public IDuelUi Ui;                      // nur nötig, wenn ein Mensch spielt
        public IDuelPresenter Presenter;        // null = keine Animationen (Server/Headless)
        public Action<IEnumerator> RunRoutine;  // Coroutine-Treiber des Hosts

        public DeckDefinition Player1Deck;      // Offline-/Inspector-Duell
        public DeckDefinition Player2Deck;
        public ControllerKind Player1Controller = ControllerKind.Human;
        public ControllerKind Player2Controller = ControllerKind.Bot;
        public bool Player1Starts = true;
        public bool EnableCoinToss = true;
        public float BotActionDelay = 0.6f;
    }

    /// <summary>
    /// Kern der Duell-Engine: Setup, Phasen-Ablauf, Regeln. Aktionen und Effekte in DuelActions.cs (partial).
    /// Bewusst KEIN MonoBehaviour: die Engine läuft überall, wo jemand ihre Coroutinen
    /// treibt — im Client der DuelHost, auf dem Server ein eigener Scheduler.
    /// </summary>
    public partial class DuelManager
    {
        private const int MaxTurns = 200;

        private readonly GameRules rules;
        private readonly DeckDefinition player1Deck;
        private readonly DeckDefinition player2Deck;
        private readonly ControllerKind player1Controller;
        private readonly ControllerKind player2Controller;
        private readonly bool player1Starts;
        private readonly bool enableCoinToss;
        private readonly IDuelUi ui;
        private readonly IDuelPresenter presenter;
        private readonly CardCatalog catalog;
        private readonly Action<IEnumerator> runRoutine;

        public DuelManager(DuelConfig config)
        {
            config = config ?? new DuelConfig();
            rules = config.Rules;
            catalog = config.Catalog;
            ui = config.Ui;
            presenter = config.Presenter;
            runRoutine = config.RunRoutine ?? DriveImmediately;
            player1Deck = config.Player1Deck;
            player2Deck = config.Player2Deck;
            player1Controller = config.Player1Controller;
            player2Controller = config.Player2Controller;
            player1Starts = config.Player1Starts;
            enableCoinToss = config.EnableCoinToss;
            BotActionDelay = config.BotActionDelay;
        }

        /// <summary>
        /// Treiber-Notnagel ohne Host: führt die Coroutine synchron bis zum Ende aus.
        /// Wartezeiten (DuelWait/null) entfallen — genau richtig für headless Bot-Duelle.
        /// </summary>
        private static void DriveImmediately(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                var current = stack.Peek();
                if (!current.MoveNext()) { stack.Pop(); continue; }
                if (current.Current is IEnumerator nested) stack.Push(nested);
            }
        }

        public CardCatalog Catalog => catalog;

        public PlayerState Player1 { get; private set; }
        public PlayerState Player2 { get; private set; }
        public PlayerState TurnPlayer { get; private set; }
        public DuelPhase Phase { get; private set; }
        public int TurnNumber { get; private set; }
        public DuelResult Result { get; private set; }
        public GameRules Rules => rules;
        public float BotActionDelay { get; set; }
        public bool DuelRunning { get; private set; }
        public readonly List<string> LogHistory = new List<string>();

        /// <summary>Karten des letzten TryDraw-Aufrufs (für die Zieh-Präsentation).</summary>
        public readonly List<CardInstance> LastDrawn = new List<CardInstance>();


        public event Action OnBoardChanged;
        public event Action OnPhaseChanged;
        public event Action<string> OnLog;
        public event Action<DuelResult> OnDuelEnded;

        /// <summary>LP-Änderung eines Spielers (Delta: negativ = Schaden, positiv = Heilung) — für Schadenszahlen/Animationen.</summary>
        public event Action<PlayerState, int> OnLifeChanged;

        private System.Random rng = new System.Random();
        private int responseDepth;
        private int activationSerial;   // zählt Effekt-Aktivierungen — erkennt, ob eine Chain entstand

        // Wie tief wir gerade in verschachtelten Aktivierungen stecken. NICHT
        // dasselbe wie responseDepth: das zählt nur Reaktionsfenster, während
        // ein Trigger mitten in einer Auflösung ActivateEffect erneut aufruft,
        // ohne je durch ein Fenster zu gehen. Wer die Kettennummer aus
        // responseDepth zieht, vergibt zweimal die 1 und schliesst die Anzeige,
        // während die äussere Aktivierung noch läuft.
        private int chainDepth;

        // Die Karten der offenen Kettenglieder, außen → innen. Die Kette selbst
        // lebt nur im Aufrufstapel (siehe chainDepth) — diese Liste ist der
        // einzige Zugriff von innen nach außen, den NegateRestOfChain braucht.
        private readonly List<CardInstance> chainCards = new List<CardInstance>();

        // Von NegateRestOfChain annullierte Glieder: die Annullierung gilt dem
        // Kettenglied, nicht der Karte — am Kettenende wird sie aufgehoben.
        private readonly List<CardInstance> chainNegatedCards = new List<CardInstance>();

        // > 0, solange irgendein Ketten-Glied AUFLOEST. In dieser Zeit öffnen
        // sich keine neuen Reaktionsfenster — in einen Abbau grätscht niemand.
        private int resolvingChain;

        private PlayerState localPlayer;

        /// <summary>Der Spieler, der an diesem Client sitzt (bestimmt die untere Board-Hälfte).</summary>
        public PlayerState LocalPlayer
        {
            get => localPlayer;
            // Die Marke am Spieler mitziehen, damit die Kartenansicht ohne
            // Rückweg zum Manager weiss, wem eine Karte gehört
            private set
            {
                if (localPlayer != null) localPlayer.IsLocal = false;
                localPlayer = value;
                if (localPlayer != null) localPlayer.IsLocal = true;
            }
        }

        public void StartDuel()
        {
            if (DuelRunning) return;

            // Solo-Duell mit einem Account-Deck (Gegner = Bot mit dem Inspector-Deck 2)
            if (Net.MatchContext.UseCustomLocalDeck && catalog != null && player2Deck != null)
            {
                StartSoloDuelFromContext();
                return;
            }

            if (rules == null || player1Deck == null || player2Deck == null)
            {
                Log("ERROR: DuelManager — rules or decks missing.");
                return;
            }

            Player1 = BuildPlayer("Player 1", player1Deck, player1Controller);
            Player2 = BuildPlayer("Player 2", player2Deck, player2Controller);
            Player1.Opponent = Player2;
            Player2.Opponent = Player1;
            LocalPlayer = player1Controller == ControllerKind.Human ? Player1
                : (player2Controller == ControllerKind.Human ? Player2 : Player1);
            TurnPlayer = player1Starts ? Player1 : Player2;

            TurnNumber = 0;
            responseDepth = 0;
            Result = DuelResult.None;
            DuelRunning = true;

            Log($"Duel: {Player1.Name} vs {Player2.Name}.");
            BoardChanged();
            runRoutine(RunDuel());
        }

        /// <summary>Solo gegen den Bot mit einem Account-Deck aus dem MatchContext.</summary>
        private void StartSoloDuelFromContext()
        {
            // Die eigenen Finishes kommen mit — solo sieht man die glänzende Kopie
            // genauso wie online.
            var deckFinishes = new List<Net.CardFinish>();
            var extraFinishes = new List<Net.CardFinish>();
            var deckCards = catalog.ResolveList(Net.MatchContext.LocalDeckCards, Net.MatchContext.LocalDeckFinishes, deckFinishes);
            var extraCards = catalog.ResolveList(Net.MatchContext.LocalExtraCards, Net.MatchContext.LocalExtraFinishes, extraFinishes);
            var hero = catalog.FindByName(Net.MatchContext.LocalHero) as PlayerCardData;

            Player1 = BuildListPlayer(Net.MatchContext.LocalName, deckCards, extraCards, hero,
                new HumanDuelController(ui), deckFinishes, extraFinishes);

            if (!string.IsNullOrEmpty(Net.MatchContext.BotName) && Net.MatchContext.BotDeckCards.Count > 0)
            {
                // Gewählter Gegner aus dem Solo-Roster (Deck, Extra Deck, Modifikatoren)
                var botDeck = catalog.ResolveList(Net.MatchContext.BotDeckCards);
                var botExtra = catalog.ResolveList(Net.MatchContext.BotExtraCards);
                var botHero = catalog.FindByName(Net.MatchContext.BotHero) as PlayerCardData;
                Player2 = BuildListPlayer(Net.MatchContext.BotName, botDeck, botExtra, botHero, new BotDuelController());
                if (Net.MatchContext.BotLifePoints > 0) Player2.LifePoints = Net.MatchContext.BotLifePoints;
                Player2.BonusManaPerTurn = Net.MatchContext.BotBonusMana;
                if (Player2.Controller is BotDuelController rosterBot) rosterBot.NoviceMode = Net.MatchContext.BotNovice;
                Player1.Opponent = Player2;
                Player2.Opponent = Player1;
            }
            else
            {
                Player2 = BuildPlayer("Bot", player2Deck, ControllerKind.Bot);
                Player1.Opponent = Player2;
                Player2.Opponent = Player1;

                // Legacy-Schwierigkeit (0 Novice, 1 Warden, 2 Sealed)
                switch (Net.MatchContext.SoloDifficulty)
                {
                    case 0:
                        Player2.Name = "The Novice";
                        if (Player2.Controller is BotDuelController novice) novice.NoviceMode = true;
                        break;
                    case 2:
                        Player2.Name = "The Sealed Warden";
                        Player2.LifePoints = 12000;
                        Player2.BonusManaPerTurn = 2;
                        break;
                    default:
                        Player2.Name = "The Warden";
                        break;
                }
            }
            LocalPlayer = Player1;
            TurnPlayer = player1Starts ? Player1 : Player2;

            TurnNumber = 0;
            responseDepth = 0;
            Result = DuelResult.None;
            DuelRunning = true;

            Log($"Solo-Duel: {Player1.Name} vs {Player2.Name}.");
            BoardChanged();
            runRoutine(RunDuel());
        }

        /// <summary>
        /// Startet ein Duell auf dem Server: beide Seiten kommen als fertige Listen
        /// mit eigenem Controller herein, es gibt keinen lokalen Spieler und keine
        /// Präsentation im eigentlichen Sinn — der Presenter zeichnet nur Ereignisse
        /// auf, die die Clients später abspielen.
        /// </summary>
        public void StartServerDuel(int seed,
            string nameA, List<CardDefinition> deckA, List<CardDefinition> extraA, PlayerCardData heroA, DuelController controllerA,
            string nameB, List<CardDefinition> deckB, List<CardDefinition> extraB, PlayerCardData heroB, DuelController controllerB,
            bool aStarts,
            List<Net.CardFinish> deckFinishesA = null, List<Net.CardFinish> extraFinishesA = null,
            List<Net.CardFinish> deckFinishesB = null, List<Net.CardFinish> extraFinishesB = null)
        {
            if (DuelRunning) return;
            if (rules == null)
            {
                Log("ERROR: DuelManager — rules missing.");
                return;
            }

            rng = new System.Random(seed);

            Player1 = BuildListPlayer(nameA, deckA, extraA, heroA, controllerA, deckFinishesA, extraFinishesA);
            Player2 = BuildListPlayer(nameB, deckB, extraB, heroB, controllerB, deckFinishesB, extraFinishesB);
            Player1.Opponent = Player2;
            Player2.Opponent = Player1;
            // Auf dem Server sitzt niemand — LocalPlayer bleibt leer, damit nichts
            // versehentlich eine Sicht bevorzugt.
            LocalPlayer = null;
            TurnPlayer = aStarts ? Player1 : Player2;

            TurnNumber = 0;
            responseDepth = 0;
            chainDepth = 0;
            chainCards.Clear();
            chainNegatedCards.Clear();
            resolvingChain = 0;
            Result = DuelResult.None;
            DuelRunning = true;

            Log($"Server-Duel: {Player1.Name} vs {Player2.Name}.");
            BoardChanged();
            runRoutine(RunDuel());
        }

        /// <summary>
        /// Baut einen Spieler aus einer Kartenliste (Laufzeit-Deck) mit beliebigem
        /// Controller. Die Finish-Listen laufen parallel zu den Kartenlisten und
        /// dürfen fehlen — dann ist jedes Exemplar schlicht.
        /// </summary>
        private PlayerState BuildListPlayer(string name, List<CardDefinition> deckCards, List<CardDefinition> extraCards,
            PlayerCardData hero, DuelController controller,
            List<Net.CardFinish> deckFinishes = null, List<Net.CardFinish> extraFinishes = null)
        {
            var player = new PlayerState { Name = string.IsNullOrWhiteSpace(name) ? "Player" : name };
            player.Controller = controller;
            player.Controller.Player = player;
            player.Controller.Duel = this;

            if (deckCards != null)
                for (int i = 0; i < deckCards.Count; i++)
                {
                    var definition = deckCards[i];
                    if (definition == null || definition is PlayerCardData || definition is ReliquaryCardData) continue;
                    player.DeckPile.Add(new CardInstance(definition, player)
                    {
                        Zone = ZoneType.Deck,
                        Finish = FinishOf(deckFinishes, i)
                    });
                }
            Shuffle(player.DeckPile);

            if (extraCards != null)
                for (int i = 0; i < extraCards.Count; i++)
                {
                    if (!(extraCards[i] is ReliquaryCardData)) continue;
                    player.ExtraDeckPile.Add(new CardInstance(extraCards[i], player)
                    {
                        Zone = ZoneType.ExtraDeck,
                        Finish = FinishOf(extraFinishes, i)
                    });
                }

            if (hero != null)
            {
                player.PlayerCard = new CardInstance(hero, player) { Zone = ZoneType.PlayerZone };
                player.LifePoints = hero.startLifePoints;
            }
            else player.LifePoints = 8000;

            player.Mana = rules.startMana;
            player.ManaPerTurn = rules.startMana;
            return player;
        }

        /// <summary>Finish an einer Deck-Position — fehlt die Angabe, ist das Exemplar schlicht.</summary>
        private static Net.CardFinish FinishOf(List<Net.CardFinish> finishes, int index) =>
            finishes != null && index >= 0 && index < finishes.Count ? finishes[index] : Net.CardFinish.Plain;

        /// <summary>Ein Spieler gibt auf (z.B. weil die Verbindung des Gegners abriss).</summary>
        public void Forfeit(PlayerState loser)
        {
            if (Result != DuelResult.None || loser == null) return;
            EndDuelByLoss(loser);
            // Sofort ausrufen: die Engine-Schleife steckt womöglich in einer
            // Anfrage an genau den Spieler, der gerade aufgegeben hat.
            AnnounceEnd();
        }

        /// <summary>Der Gegner hat im Netz-Duell aufgegeben — hier gewinnt der lokale Spieler.</summary>
        /// <summary>Leitet einen Request an den Controller des Spielers.</summary>
        private IEnumerator DecideRouted(PlayerState player, DuelRequest request)
        {
            switch (request)
            {
                case StartChoiceRequest start: yield return player.Controller.Decide(start); break;
                case MainActionRequest main: yield return player.Controller.Decide(main); break;
                case BattleActionRequest battle: yield return player.Controller.Decide(battle); break;
                case YesNoRequest yesNo: yield return player.Controller.Decide(yesNo); break;
                case OptionRequest option: yield return player.Controller.Decide(option); break;
                case TargetRequest target: yield return player.Controller.Decide(target); break;
                case ZoneSelectRequest zoneSelect: yield return player.Controller.Decide(zoneSelect); break;
            }
        }

        private PlayerState BuildPlayer(string fallbackName, DeckDefinition deckDef, ControllerKind kind)
        {
            var player = new PlayerState
            {
                Name = string.IsNullOrWhiteSpace(deckDef.deckName) ? fallbackName : deckDef.deckName,
                DeckSource = deckDef
            };

            player.Controller = kind == ControllerKind.Human
                ? (DuelController)new HumanDuelController(ui)
                : new BotDuelController();
            player.Controller.Player = player;
            player.Controller.Duel = this;

            foreach (var definition in deckDef.cards)
            {
                if (definition == null || definition is PlayerCardData) continue;
                if (definition is ReliquaryCardData reliquary)
                {
                    player.ExtraDeckPile.Add(new CardInstance(reliquary, player) { Zone = ZoneType.ExtraDeck });
                    continue;
                }
                player.DeckPile.Add(new CardInstance(definition, player) { Zone = ZoneType.Deck });
            }
            Shuffle(player.DeckPile);

            foreach (var definition in deckDef.extraCards)
                if (definition is ReliquaryCardData)
                    player.ExtraDeckPile.Add(new CardInstance(definition, player) { Zone = ZoneType.ExtraDeck });

            if (deckDef.playerCard != null)
            {
                player.PlayerCard = new CardInstance(deckDef.playerCard, player) { Zone = ZoneType.PlayerZone };
                player.LifePoints = deckDef.playerCard.startLifePoints;
            }
            else
            {
                player.LifePoints = 8000;
            }

            player.Mana = rules.startMana;
            player.ManaPerTurn = rules.startMana;
            return player;
        }

        private IEnumerator RunDuel()
        {
            if (enableCoinToss) yield return ResolveCoinToss();

            // Starthände erst NACH dem Münzwurf — einzeln gezogen und animiert
            yield return DrawOpeningHands();

            while (Result == DuelResult.None && TurnNumber < MaxTurns)
            {
                TurnNumber++;
                yield return RunTurn(TurnPlayer);
                if (Result != DuelResult.None) break;
                TurnPlayer = TurnPlayer.Opponent;
            }

            if (Result == DuelResult.None)
            {
                Log("Turn limit reached — winner decided by life points.");
                Result = Player1.LifePoints >= Player2.LifePoints ? DuelResult.Player1Wins : DuelResult.Player2Wins;
            }

            AnnounceEnd();
        }

        /// <summary>
        /// Ruft das Duell-Ende GENAU EINMAL aus. Neben dem normalen Schleifenende
        /// ruft auch Forfeit hierher — wer aufgibt, sieht den Ergebnis-Bildschirm
        /// sofort statt erst am nächsten Prüfpunkt der Engine (die wartet sonst
        /// womöglich gerade auf eine Eingabe, die nie mehr kommt).
        /// </summary>
        private bool endAnnounced;
        private void AnnounceEnd()
        {
            if (endAnnounced || Result == DuelResult.None) return;
            endAnnounced = true;
            DuelRunning = false;
            var winner = Result == DuelResult.Player1Wins ? Player1 : Player2;
            Log($"DUEL OVER — {winner.Name} wins!");
            BoardChanged();
            OnDuelEnded?.Invoke(Result);
        }

        // ---------- Münzwurf ----------

        /// <summary>
        /// Münzwurf vor dem Duell: der Gewinner wählt First/Second über einen ganz
        /// normalen Request — Mensch per UI, Bot sofort "first", der Lockstep-Gegner
        /// über die Antwort-Warteschlange, der Server-Host über die Wire-Verbindung.
        /// Der Wurf nutzt das rng der Engine: im Netz aus dem Match-Seed, damit beide
        /// Clients denselben Gewinner sehen.
        /// </summary>
        private IEnumerator ResolveCoinToss()
        {
            PlayerState winner = rng.Next(2) == 0 ? Player1 : Player2;

            // Sichtbarer Münzwurf (drehende Münze), danach das Ergebnis als Banner
            if (presenter != null) yield return presenter.ShowCoinToss(winner);
            Log($"Coin toss: {winner.Name} wins the toss!");
            if (presenter != null) yield return presenter.ShowPhaseBanner($"COIN TOSS — {winner.Name.ToUpperInvariant()} WINS!", 1.2f);

            var startRequest = new StartChoiceRequest { Title = "Go first or second?" };
            if (winner.Controller is BotDuelController) yield return DuelWait.For(0.4f); // kurzer Beat
            yield return DecideRouted(winner, startRequest);
            bool winnerGoesFirst = startRequest.Result;

            TurnPlayer = winnerGoesFirst ? winner : winner.Opponent;
            Log($"{winner.Name} chooses to go {(winnerGoesFirst ? "first" : "second")} — {TurnPlayer.Name} begins.");
            if (presenter != null) yield return presenter.ShowPhaseBanner($"{TurnPlayer.Name.ToUpperInvariant()} GOES FIRST", 1.1f);
        }

        /// <summary>Anzeigename einer Phase — lebt in der Engine, die UI greift darauf zu.</summary>
        public static string PhaseName(DuelPhase phase)
        {
            switch (phase)
            {
                case DuelPhase.Draw: return "Draw Phase";
                case DuelPhase.Standby: return "Standby Phase";
                case DuelPhase.Main: return "Main Phase";
                case DuelPhase.Battle: return "Battle Phase";
                default: return "End Phase";
            }
        }

        /// <summary>Phasenwechsel mit sichtbarem Banner (~1s), damit jeder Übergang lesbar ist.</summary>
        private IEnumerator EnterPhase(DuelPhase phase)
        {
            SetPhase(phase);
            if (presenter != null)
                yield return presenter.ShowPhaseBanner(PhaseName(phase).ToUpperInvariant());
        }

        private IEnumerator RunTurn(PlayerState player)
        {
            player.TurnsTaken++;
            player.NormalSummonsUsed = 0;
            player.ManaPerTurn = Math.Min(rules.manaCap, rules.startMana + (player.TurnsTaken - 1) * rules.manaGrowthPerTurn) + player.BonusManaPerTurn;

            // Schulden und Guthaben aus der letzten Runde verrechnen — sie gelten
            // genau einmal. ManaPerTurn bleibt unangetastet: das ist der normale
            // Wert dieses Zuges, und die Anzeige soll zeigen, wie weit man darunter
            // liegt.
            int carried = player.ManaCredit - player.ManaDebt;
            player.Mana = Math.Max(0, player.ManaPerTurn + carried);
            if (carried != 0)
                Log($"{player.Name} starts with {player.Mana} Mana instead of {player.ManaPerTurn} " +
                    $"({(carried > 0 ? "+" : "")}{carried}).");
            player.ManaDebt = 0;
            player.ManaCredit = 0;

            ResetTurnFlags(player);

            Log($"— Turn {TurnNumber}: {player.Name} (Mana {player.Mana}) —");

            // ---- Draw Phase ----
            yield return EnterPhase(DuelPhase.Draw);
            bool skipDraw = TurnNumber == 1 && rules.turnPlayerSkipsFirstDraw;
            if (skipDraw)
            {
                Log($"{player.Name} skips the first draw.");
            }
            else
            {
                if (!TryDraw(player, 1)) yield break;
                yield return PresentDraws(player);
            }
            BoardChanged();
            if (CheckWin()) yield break;

            // ---- Standby Phase ----
            yield return EnterPhase(DuelPhase.Standby);
            yield return ResolvePhaseTriggers(player, EffectTrigger.StandbyPhase);
            if (CheckWin()) yield break;
            // Slowburn: Lunten, die vor diesem Zug gelegt wurden, zünden jetzt
            yield return ResolveChargedSpells(player);
            if (CheckWin()) yield break;

            // ---- Main Phase ----
            yield return EnterPhase(DuelPhase.Main);
            bool toBattle = false;
            while (Result == DuelResult.None)
            {
                var request = BuildMainActions(player);
                yield return DecideRouted(player, request);
                if (request.Chosen < 0 || request.Chosen >= request.Options.Count) continue;

                var option = request.Options[request.Chosen];
                if (option.Kind == MainActionKind.EndTurn) break;
                if (option.Kind == MainActionKind.ToBattlePhase) { toBattle = true; break; }

                yield return ExecuteMainAction(player, option);
                if (CheckWin()) yield break;
            }

            // ---- Battle Phase ----
            if (toBattle && Result == DuelResult.None)
            {
                yield return EnterPhase(DuelPhase.Battle);
                // Priority-Fenster: der Gegner darf vor den Angriffen reagieren
                yield return OpenResponseWindow(player.Opponent, "start of the Battle Phase", null, true);
                if (CheckWin()) yield break;
                yield return RunBattlePhase(player);
                if (CheckWin()) yield break;
            }

            // ---- End Phase ----
            yield return EnterPhase(DuelPhase.End);
            // Priority-Fenster: letzte Reaktionsmöglichkeit vor dem Zugende
            yield return OpenResponseWindow(player.Opponent, "start of the End Phase", null, true);
            if (CheckWin()) yield break;
            yield return ResolvePhaseTriggers(player, EffectTrigger.EndPhase);
            if (CheckWin()) yield break;

            // Deckay: Endphasen-Trigger, die in JEDEM bzw. im GEGNERISCHEN Zug
            // feuern — erst der Zugspieler, dann der Gegner (dessen Fiend opfert
            // sich hier, dessen Maggot millt trotzdem).
            yield return ResolvePhaseTriggers(player, EffectTrigger.EitherEndPhase);
            if (CheckWin()) yield break;
            yield return ResolvePhaseTriggers(player.Opponent, EffectTrigger.OpponentEndPhase);
            if (CheckWin()) yield break;
            yield return ResolvePhaseTriggers(player.Opponent, EffectTrigger.EitherEndPhase);
            if (CheckWin()) yield break;

            // Vulture-Konter-Reliquary: das geliehene Reliquary des ZUGSPIELERS
            // hat seine End Phase erreicht — es geht ins Grab, nicht zurück.
            foreach (var borrowed in new List<CardInstance>(player.Monsters()))
            {
                if (!borrowed.TempReliquaryUntilEndPhase) continue;
                borrowed.TempReliquaryUntilEndPhase = false;
                Log($"{borrowed.Name}'s borrowed time runs out — it is sent to the Graveyard.");
                MoveToGraveyardWithEquips(borrowed);
                BoardChanged();
            }
            yield return FirePendingGraveTriggers();
            if (CheckWin()) yield break;

            ClearTempModifiers();
            yield return EnforceHandLimit(player);
            // Handlimit-Abwürfe können Friedhofs-Trigger tragen (Deckay Vulture)
            yield return FirePendingGraveTriggers();
            BoardChanged();
        }

        private void ResetTurnFlags(PlayerState turnPlayer)
        {
            foreach (var player in new[] { Player1, Player2 })
            {
                player.SpellsCastThisTurn = 0; // Erster-Zauber-Rabatt gilt je Zug neu
                if (player == turnPlayer)
                {
                    // Deckay: "letzte Runde gemillt" — der Zähler rutscht beim
                    // eigenen Zugbeginn weiter (Mills im Gegnerzug zählten mit)
                    player.MilledLastTurn = player.MilledThisTurn;
                    player.MilledThisTurn = false;
                    player.SelfSummonedNamesThisTurn.Clear();
                }
                foreach (var card in player.FieldCards())
                {
                    card.SetThisTurn = false;
                    card.OncePerTurnUsed.Clear();
                    if (player == turnPlayer)
                    {
                        card.HasAttackedThisTurn = false;
                        card.BonusAttacks = 0;
                        card.PositionChangesUsed = 0;
                        card.SummonedThisTurn = false;
                    }
                }
            }
        }

        private void ClearTempModifiers()
        {
            foreach (var player in new[] { Player1, Player2 })
            {
                player.ExtraNormalSummons = 0;
                player.NoBattleDamageThisTurn = false;
                player.CannotSpecialSummonThisTurn = false;

                foreach (var card in player.FieldCards())
                {
                    card.TempAtkBonus = 0;
                    card.TempDefBonus = 0;
                    card.EffectsNegated = false;
                    card.CannotBeDestroyedThisTurn = false;
                    card.CannotAttackThisTurn = false;
                    card.PositionLockedThisTurn = false;
                    card.CannotBeTargetedThisTurn = false;
                    card.ImmuneToOpponentThisTurn = false;
                    card.MustBeAttackedThisTurn = false;
                    card.StatsSwappedThisTurn = false;
                    card.StatsOverriddenThisTurn = false;
                }
            }
            ReturnBorrowedCards();
        }

        /// <summary>
        /// Räumt nach dem Zug auf, was nur geliehen war: Laufzeit-Kopien verschwinden,
        /// übernommene Monster gehen an ihren Besitzer zurück. Ist dessen Feld voll,
        /// wandert die Karte stattdessen in seinen Friedhof.
        /// </summary>
        private void ReturnBorrowedCards()
        {
            foreach (var player in new[] { Player1, Player2 })
            {
                for (int i = 0; i < player.MonsterZones.Length; i++)
                {
                    var card = player.MonsterZones[i];
                    if (card == null) continue;

                    if (card.IsTemporaryCopy)
                    {
                        Log($"The copy of {card.Name} fades away.");
                        player.MonsterZones[i] = null;
                        card.Zone = ZoneType.Banished;   // Kopien existieren außerhalb der Stapel
                        continue;
                    }

                    var back = card.ControlReturnsTo;
                    if (back == null) continue;

                    player.MonsterZones[i] = null;
                    card.ControlReturnsTo = null;
                    card.Owner = back;
                    int free = back.FirstFreeZoneIndex(back.MonsterZones);
                    if (free >= 0)
                    {
                        back.MonsterZones[free] = card;
                        card.Zone = ZoneType.MonsterZone;
                        Log($"{card.Name} returns to {back.Name}.");
                    }
                    else
                    {
                        card.Zone = ZoneType.Graveyard;
                        back.Graveyard.Add(card);
                        Log($"{card.Name} returns to {back.Name} — no free zone, it goes to the graveyard.");
                    }
                }
            }
        }

        /// <summary>Zieht Karten; bei leerem Deck verliert der Spieler (Deck-Out).</summary>
        public bool TryDraw(PlayerState player, int amount)
        {
            LastDrawn.Clear();
            for (int i = 0; i < amount; i++)
            {
                if (player.DeckPile.Count == 0)
                {
                    Log($"{player.Name} cannot draw — the deck is empty!");
                    EndDuelByLoss(player);
                    return false;
                }
                var card = player.DeckPile[0];
                player.DeckPile.RemoveAt(0);
                card.Zone = ZoneType.Hand;
                player.Hand.Add(card);
                LastDrawn.Add(card);
                Log($"{player.Name} draws a card ({player.Hand.Count} in hand).");
            }
            BoardChanged();
            return true;
        }

        /// <summary>Spielt die Zieh-Animation für alle zuletzt gezogenen Karten ab (falls ein Presenter existiert).</summary>
        private IEnumerator PresentDraws(PlayerState player)
        {
            if (presenter == null || LastDrawn.Count == 0) yield break;
            foreach (var card in LastDrawn.ToArray())
                yield return presenter.ShowCardDrawn(player, card);
        }


        private void DrawSilently(PlayerState player, int amount)
        {
            for (int i = 0; i < amount && player.DeckPile.Count > 0; i++)
            {
                var card = player.DeckPile[0];
                player.DeckPile.RemoveAt(0);
                card.Zone = ZoneType.Hand;
                player.Hand.Add(card);
            }
        }

        /// <summary>
        /// Starthände: beide Spieler ziehen abwechselnd einzeln (mit Zieh-Animation an die
        /// echte Hand-Position). Danach werden beide Hände synchron gemischt, damit niemand
        /// nachhalten kann, an welcher Stelle welche gezogene Karte liegt.
        /// </summary>
        private IEnumerator DrawOpeningHands()
        {
            int first = rules.startHandTurnPlayer;
            int second = rules.startHandOpponent;
            for (int i = 0; i < Math.Max(first, second); i++)
            {
                if (i < first) yield return DrawOpeningCard(TurnPlayer);
                if (i < second) yield return DrawOpeningCard(TurnPlayer.Opponent);
            }

            Shuffle(TurnPlayer.Hand);
            Shuffle(TurnPlayer.Opponent.Hand);
            BoardChanged();
            if (presenter != null)
            {
                yield return presenter.ShowHandShuffle(TurnPlayer);
                yield return presenter.ShowHandShuffle(TurnPlayer.Opponent);
            }
        }

        private IEnumerator DrawOpeningCard(PlayerState player)
        {
            if (player.DeckPile.Count == 0) yield break;
            var card = player.DeckPile[0];
            player.DeckPile.RemoveAt(0);
            card.Zone = ZoneType.Hand;
            player.Hand.Add(card);
            BoardChanged();
            if (presenter != null) yield return presenter.ShowCardDrawn(player, card, 1.33f); // Start-Draws ~25% flotter
        }

        private IEnumerator EnforceHandLimit(PlayerState player)
        {
            int excess = player.Hand.Count - rules.handLimit;
            if (excess <= 0) yield break;

            var request = new TargetRequest
            {
                Title = $"Hand limit ({rules.handLimit}) — discard {excess} card(s)",
                Kind = TargetKind.None,
                Count = excess,
                AllowCancel = false
            };
            request.Candidates.AddRange(player.Hand);
            yield return DecideRouted(player, request);

            var toDiscard = new List<CardInstance>(request.Result);
            while (toDiscard.Count < excess && player.Hand.Count > 0)
            {
                var fallback = player.Hand[player.Hand.Count - 1];
                if (!toDiscard.Contains(fallback)) toDiscard.Add(fallback);
                else break;
            }
            foreach (var card in toDiscard)
            {
                if (player.Hand.Remove(card))
                {
                    MoveToGraveyard(card);
                    Log($"{player.Name} discards {card.Name} (hand limit).");
                }
            }
            BoardChanged();
        }

        public bool CheckWin()
        {
            if (Result != DuelResult.None) return true;
            if (Player1.LifePoints <= 0) { Result = DuelResult.Player2Wins; return true; }
            if (Player2.LifePoints <= 0) { Result = DuelResult.Player1Wins; return true; }
            return false;
        }

        private void EndDuelByLoss(PlayerState loser)
        {
            if (Result != DuelResult.None) return;
            Result = loser == Player1 ? DuelResult.Player2Wins : DuelResult.Player1Wins;
        }

        private void Shuffle(List<CardInstance> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void SetPhase(DuelPhase phase)
        {
            Phase = phase;
            OnPhaseChanged?.Invoke();
        }

        public void Log(string message)
        {
            LogHistory.Add(message);
            OnLog?.Invoke(message);
        }

        public void BoardChanged() => OnBoardChanged?.Invoke();
    }
}
