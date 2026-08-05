using System.Collections;
using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Unity-Wirt der Duell-Engine: hält die Szenen-Verdrahtung, erzeugt den
    /// DuelManager (reine C#-Klasse) und treibt dessen Coroutinen. Er übersetzt
    /// die Engine-Warteanweisung DuelWait in WaitForSeconds — die Engine selbst
    /// kennt keine Unity-Typen mehr.
    ///
    /// Die Feldnamen entsprechen exakt den alten DuelManager-Feldern, damit die
    /// serialisierten Szenen-Daten den Skript-Tausch überleben.
    /// </summary>
    public class DuelHost : MonoBehaviour
    {
        [Header("Konfiguration (im Inspector verdrahten)")]
        [SerializeField]
        [Tooltip("Regelwerk (ScriptableObject) — hier balancen!")]
        private GameRules rules;

        [SerializeField]
        [Tooltip("Deck von Spieler 1 (untere Feldhälfte)")]
        private DeckDefinition player1Deck;

        [SerializeField]
        [Tooltip("Deck von Spieler 2 (obere Feldhälfte)")]
        private DeckDefinition player2Deck;

        [SerializeField]
        [Tooltip("Wer steuert Spieler 1")]
        private ControllerKind player1Controller = ControllerKind.Human;

        [SerializeField]
        [Tooltip("Wer steuert Spieler 2")]
        private ControllerKind player2Controller = ControllerKind.Bot;

        [SerializeField]
        [Tooltip("Beginnt Spieler 1? (Startspieler: 5 Karten, kein Draw in Zug 1)")]
        private bool player1Starts = true;

        [SerializeField]
        [Tooltip("Duell automatisch beim Szenenstart beginnen")]
        private bool autoStart = true;

        [SerializeField]
        [Tooltip("Münzwurf vor dem Duell: der Gewinner wählt, wer beginnt (für Tests abschaltbar)")]
        private bool enableCoinToss = true;

        [Range(0f, 2f)]
        [SerializeField]
        [Tooltip("Pause zwischen Bot-Aktionen in Sekunden (0 = sofort)")]
        private float botActionDelay = 0.6f;

        [Header("UI (nur nötig, wenn ein Mensch spielt)")]
        [SerializeField] private DuelUIController ui;

        [Header("Präsentation (optional)")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("presenter")]
        [Tooltip("Master-Duel-artige Anzeige von Draws/Aktivierungen. Leer = keine Animationen.")]
        private DuelPresenter scenePresenter;

        [Header("Netzwerk")]
        [SerializeField]
        [Tooltip("Karten-Katalog — löst Kartennamen aus dem Netzwerk in lokale Assets auf")]
        private CardCatalog catalog;

        private DuelManager duel;

        /// <summary>
        /// Die Engine. Lazy erzeugt, damit die Awake/OnEnable-Reihenfolge anderer
        /// Komponenten (BoardRenderer, GameOverScreen) keine Rolle spielt.
        /// </summary>
        public DuelManager Duel
        {
            get
            {
                if (duel == null) Rebuild();
                return duel;
            }
        }

        // Zugriffe für Tests und Laufzeit-Hosts (NetworkLoopbackTest)
        public bool AutoStart { get => autoStart; set => autoStart = value; }
        public GameRules Rules => rules;
        public CardCatalog Catalog => catalog;
        public DeckDefinition Player1Deck => player1Deck;
        public DeckDefinition Player2Deck => player2Deck;
        public DuelPresenter ScenePresenter => scenePresenter;

        private void Awake()
        {
            if (duel == null) Rebuild();
        }

        /// <summary>
        /// Baut die Engine mit neuer Konfiguration — für zur Laufzeit erzeugte Hosts
        /// (z.B. die versteckte B-Seite des Loopback-Tests).
        /// </summary>
        public DuelManager Configure(GameRules newRules, CardCatalog newCatalog,
            DuelUIController newUi = null, DuelPresenter newPresenter = null, bool newAutoStart = false)
        {
            rules = newRules;
            catalog = newCatalog;
            ui = newUi;
            scenePresenter = newPresenter;
            autoStart = newAutoStart;
            Rebuild();
            return duel;
        }

        private void Rebuild()
        {
            duel = new DuelManager(new DuelConfig
            {
                Rules = rules,
                Catalog = catalog,
                Ui = ui,
                Presenter = scenePresenter,
                RunRoutine = Run,
                Player1Deck = player1Deck,
                Player2Deck = player2Deck,
                Player1Controller = player1Controller,
                Player2Controller = player2Controller,
                Player1Starts = player1Starts,
                EnableCoinToss = enableCoinToss,
                BotActionDelay = botActionDelay
            });
        }

        private void Start()
        {
            // Server-Duell: der ServerDuelClient spiegelt — kein lokaler Duellstart
            if (Net.MatchContext.IsServerMatch) return;
            if (autoStart) StartCoroutine(StartWhenCurtainLifts());
        }

        /// <summary>
        /// Der Lade-Übergang liegt beim Szenenstart noch über allem. Erst wenn er
        /// freigibt, darf die Eröffnung laufen — sonst zieht der Spieler seine
        /// Starthand hinter dem Vorhang.
        /// </summary>
        private IEnumerator StartWhenCurtainLifts()
        {
            // Ab hier steht das Duell — der Vorhang darf hoch. Vorher wusste das
            // die CoinToss-Szene, die es nicht mehr gibt.
            DuelLoadTransition.Release();
            while (DuelLoadTransition.CurtainHolding) yield return null;
            Duel.StartDuel();
        }

        private void Run(IEnumerator routine) => StartCoroutine(Drive(routine));

        /// <summary>
        /// Treibt eine Engine-Coroutine und übersetzt dabei: DuelWait → WaitForSeconds,
        /// verschachtelte IEnumeratoren rekursiv, alles andere (null, Unity-Yields aus
        /// Presenter/UI-Coroutinen) unverändert an Unity durchreichen.
        /// </summary>
        private IEnumerator Drive(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                var current = routine.Current;
                if (current is DuelWait wait) yield return new WaitForSeconds(wait.Seconds);
                else if (current is IEnumerator nested) yield return Drive(nested);
                else yield return current;
            }
        }
    }
}
