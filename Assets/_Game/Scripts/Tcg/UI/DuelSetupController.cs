using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Duel Setup (Handoff 5): EIN Screen für Online-Duell und Solo-Trial mit Modus-Tabs.
    /// Links der Deck-Picker mit Legalitäts-Gating, rechts Mode-Banner, großer Start-Button,
    /// Private-Lobby-/Join-Code-Flow bzw. Schwierigkeit + Trial-Briefing, dazu das Such-Overlay.
    /// Netzwerk-Handshake (Deck-Austausch, MatchContext) wie der alte PlayController.
    /// </summary>
    public class DuelSetupController : MonoBehaviour
    {
        /// <summary>Vom Hauptmenü gesetzt: true = Solo-Tab vorwählen.</summary>
        public static bool OpenSolo;

        /// <summary>Von der Turm-Rückkehr gesetzt: true = Tower-Tab vorwählen.</summary>
        public static bool OpenTower;

        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;
        [SerializeField] private GameRules rules;
        [SerializeField] private CardSkin skin;
        [SerializeField] private DeckPickRow rowPrefab;

        [Header("Top-Bar")]
        [SerializeField] private Button onlineTabButton;
        [SerializeField] private Button soloTabButton;
        [SerializeField] private Image onlineTabBg;
        [SerializeField] private Image soloTabBg;
        [SerializeField] private TMP_Text onlineTabLabel;
        [SerializeField] private TMP_Text soloTabLabel;
        [SerializeField] private TMP_Text playerInitial;
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text playerRank;
        [SerializeField] private Button menuButton;

        [Header("Deck-Picker")]
        [SerializeField] private TMP_Text deckHeaderInfo;
        [SerializeField] private Transform deckListContent;
        [SerializeField] private Button deckBuilderButton;

        [Header("Mode-Banner")]
        [SerializeField] private Image bannerBg;
        [SerializeField] private Image bannerFrame;
        [SerializeField] private Image bannerKeyline;
        [SerializeField] private Image bannerDeco;
        [SerializeField] private Image opponentCard;
        [SerializeField] private Image opponentGem;
        [SerializeField] private TMP_Text bannerEyebrow;
        [SerializeField] private TMP_Text bannerTitle;
        [SerializeField] private TMP_Text bannerBlurb;
        [SerializeField] private TMP_Text stat1Label;
        [SerializeField] private TMP_Text stat1Value;
        [SerializeField] private TMP_Text stat2Label;
        [SerializeField] private TMP_Text stat2Value;

        [Header("Start-Button")]
        [SerializeField] private Button startButton;
        [SerializeField] private Image startBg;
        [SerializeField] private Image startFrame;
        [SerializeField] private Image startDiamond;
        [SerializeField] private TMP_Text startTitle;
        [SerializeField] private TMP_Text startSub;

        [Header("Online-Optionen")]
        [SerializeField] private GameObject onlineGroup;
        [SerializeField] private GameObject lobbyIdle;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private GameObject lobbyActive;
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private Button closeLobbyButton;
        [SerializeField] private TMP_InputField joinInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Image joinBg;
        [SerializeField] private TMP_Text joinLabel;

        [Header("Solo-Optionen")]
        [SerializeField] private GameObject soloGroup;
        [SerializeField] private Button[] difficultyButtons = new Button[3];
        [SerializeField] private Image[] difficultyBgs = new Image[3];
        [SerializeField] private Image[] difficultyFrames = new Image[3];
        [SerializeField] private TMP_Text[] difficultyNames = new TMP_Text[3];
        [SerializeField] private TMP_Text[] difficultyNotes = new TMP_Text[3];
        [SerializeField] private TMP_Text trialAdvice;
        [SerializeField] private TMP_Text trialReward;

        [Header("Solo-Gegner-Roster (ersetzt die statischen Schwierigkeiten)")]
        [SerializeField] private BotOpponentDefinition[] opponents = new BotOpponentDefinition[0];

        [Header("The Tower (Story-Modus)")]
        [SerializeField] private TowerDefinition tower;

        [Header("Illegal-Strip")]
        [SerializeField] private GameObject illegalStrip;
        [SerializeField] private TMP_Text illegalText;

        [Header("Such-Overlay")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private Image overlayFrame;
        [SerializeField] private Image spinnerRing;
        [SerializeField] private TMP_Text overlayTitle;
        [SerializeField] private TMP_Text overlayNote;
        [SerializeField] private RectTransform overlaySweep;
        [SerializeField] private TMP_Text receiptText;
        [SerializeField] private Button cancelButton;

        [Header("Szenen")]
        [SerializeField] private string duelSceneName = "Duel";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string decksSceneName = "DeckEditor";

        private NetworkManager network;
        private readonly List<DeckPickRow> rows = new List<DeckPickRow>();
        private int mode;             // 0 online, 1 solo, 2 tower
        private int selectedDeck;

        // ---- The Tower (alles zur Laufzeit gebaut) ----
        private Button towerTabButton;
        private Image towerTabBg;
        private TMP_Text towerTabLabel;
        private RectTransform towerGroup;
        private readonly List<(RectTransform row, Image bg, Image frame, TMP_Text name, TMP_Text state)> towerRows
            = new List<(RectTransform, Image, Image, TMP_Text, TMP_Text)>();
        private ScrollRect towerScroll;
        private int selectedFloor = 1;    // 1-basiert
        private GameObject dialogOverlay;

        // ---- Dynamische Roster-Liste (ersetzt die festen 5 Chips) ----
        private readonly List<(Image bg, Image frame, TMP_Text name, TMP_Text note)> rosterRows
            = new List<(Image, Image, TMP_Text, TMP_Text)>();
        private int difficulty = 1;   // Warden
        private string lobbyCode;
        private bool searching;
        private bool loadingMatch;
        private Coroutine sweepRoutine;

        private static readonly string[] DiffNames = { "Novice", "Warden", "Sealed" };
        private static readonly string[] DiffNotes = { "PLAYS OPENLY · NO TRAPS", "FULL DECK · REAL AI", "+2 MANA · 12 000 LP" };
        private static readonly string[] DiffAdvice =
        {
            "The Novice never reacts on your turn — no quick spells, no ambushes. Learn the flow of the vault safely.",
            "The Warden plays the full Frost & Light list with real reactions. Bring removal for its high-DEF walls and watch for quick spells.",
            "The Sealed Warden opens every turn with two extra mana behind 12 000 LP. Expect early Level 3 drops — pressure fast or be buried."
        };

        private RuntimeDeck SelectedDeck =>
            PlayerProfile.Decks.Count == 0 ? null
            : PlayerProfile.Decks[Mathf.Clamp(selectedDeck, 0, PlayerProfile.Decks.Count - 1)];

        private int DeckMin => rules != null ? rules.deckMinSize : 40;
        private int DeckMax => rules != null ? rules.deckMaxSize : 80;

        private bool DeckLegal(RuntimeDeck deck) =>
            deck != null && deck.Cards.Count >= DeckMin && deck.Cards.Count <= DeckMax && DeckIsOwned(deck);

        private static bool DeckIsOwned(RuntimeDeck deck)
        {
            foreach (var cardName in deck.Cards)
                if (PlayerProfile.Owned(cardName) < 1) return false;
            if (!string.IsNullOrEmpty(deck.Hero) && PlayerProfile.Owned(deck.Hero) < 1) return false;
            return true;
        }

        private void Start()
        {
            network = NetworkManager.Instance;
            mode = OpenTower ? 2 : OpenSolo ? 1 : 0;
            OpenSolo = false;
            OpenTower = false;

            if (onlineTabButton != null) onlineTabButton.onClick.AddListener(() => SetMode(0));
            if (soloTabButton != null) soloTabButton.onClick.AddListener(() => SetMode(1));
            BuildTowerTab();
            BuildRosterList();
            if (menuButton != null) menuButton.onClick.AddListener(Back);
            if (deckBuilderButton != null) deckBuilderButton.onClick.AddListener(() => SceneManager.LoadScene(decksSceneName));
            if (startButton != null) startButton.onClick.AddListener(PressStart);
            if (createLobbyButton != null) createLobbyButton.onClick.AddListener(CreateLobby);
            if (copyCodeButton != null) copyCodeButton.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(lobbyCode)) GUIUtility.systemCopyBuffer = lobbyCode;
            });
            if (closeLobbyButton != null) closeLobbyButton.onClick.AddListener(CloseLobby);
            if (joinInput != null) joinInput.onValueChanged.AddListener(OnJoinCodeChanged);
            if (joinButton != null) joinButton.onClick.AddListener(JoinLobby);
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelSearch);
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int index = i;
                if (difficultyButtons[i] != null)
                    difficultyButtons[i].onClick.AddListener(() => { difficulty = index; RefreshAll(); });
            }
            if (network != null)
            {
                network.OnMessage += HandleMessage;
                network.OnDisconnected += HandleDisconnected;
            }

            // Spieler-Plate
            string accountName = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Wanderer";
            if (playerInitial != null) playerInitial.text = accountName.Length > 0 ? accountName.Substring(0, 1).ToUpperInvariant() : "?";
            if (playerName != null) playerName.text = accountName;
            if (playerRank != null) playerRank.text = "DUELIST OF THE VAULT";

            if (overlay != null) overlay.SetActive(false);
            if (lobbyActive != null) lobbyActive.SetActive(false);

            BuildRows();
            // Erstes legales Deck vorwählen — es sei denn, das zuletzt gespielte
            // (gemerkt über Play/Solo/Builder hinweg) ist legal, dann das.
            for (int i = 0; i < PlayerProfile.Decks.Count; i++)
                if (DeckLegal(PlayerProfile.Decks[i])) { selectedDeck = i; break; }
            int remembered = PlayerPrefs.GetInt(MainMenuController.ActiveDeckPrefKey, -1);
            if (remembered >= 0 && remembered < PlayerProfile.Decks.Count && DeckLegal(PlayerProfile.Decks[remembered]))
                selectedDeck = remembered;

            // Rückkehr aus einem gewonnenen Turm-Duell: die Siegzeile des Keepers
            // steht noch aus. Danach ist die nächste Ebene die aktive.
            if (MatchContext.TowerWon && MatchContext.TowerFloor > 0)
            {
                int wonFloor = MatchContext.TowerFloor;
                MatchContext.TowerWon = false;
                MatchContext.TowerFloor = 0;
                selectedFloor = Mathf.Min(wonFloor + 1, TowerFloorCount());
                ShowVictoryLine(wonFloor);
            }
            else selectedFloor = Mathf.Min(PlayerProfile.TowerFloor + 1, Mathf.Max(1, TowerFloorCount()));

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (network == null) return;
            network.OnMessage -= HandleMessage;
            network.OnDisconnected -= HandleDisconnected;
        }

        private void Back()
        {
            if (network != null) network.SendLeave();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ---------- Deck-Picker ----------
        private void BuildRows()
        {
            foreach (var row in rows) if (row != null) Destroy(row.gameObject);
            rows.Clear();
            if (deckListContent == null || rowPrefab == null) return;
            for (int i = 0; i < PlayerProfile.Decks.Count; i++)
            {
                var deck = PlayerProfile.Decks[i];
                bool legal = DeckLegal(deck);
                string label = legal ? "LEGAL"
                    : deck.Cards.Count < DeckMin ? "TOO FEW CARDS"
                    : deck.Cards.Count > DeckMax ? "TOO MANY CARDS" : "UNOWNED CARDS";
                var row = Instantiate(rowPrefab, deckListContent);
                int index = i;
                row.Setup(deck, i, legal, label, catalog, () =>
                {
                    selectedDeck = index;
                    PlayerPrefs.SetInt(MainMenuController.ActiveDeckPrefKey, index);
                    PlayerPrefs.Save();
                    RefreshAll();
                });
                rows.Add(row);
            }
            if (deckHeaderInfo != null)
                deckHeaderInfo.text = $"{PlayerProfile.Decks.Count} deck{(PlayerProfile.Decks.Count == 1 ? "" : "s")} · only legal decks can duel";
        }

        // ---------- Modus & Zustände ----------
        private void SetMode(int newMode)
        {
            if (searching) return;
            mode = newMode;
            RefreshAll();
        }

        private Color Accent => mode == 0 ? new Color32(0xC8, 0xA4, 0x5C, 0xFF) : new Color32(0x8F, 0xC6, 0xD2, 0xFF);
        private Color AccentBright => mode == 0 ? new Color32(0xEB, 0xCE, 0x8A, 0xFF) : new Color32(0xB4, 0xE2, 0xEC, 0xFF);
        private Sprite AccentBadge => skin == null ? null : (mode == 0 ? skin.badgeMonster : skin.badgeTeal);
        private Color AccentInk => mode == 0 ? new Color32(0x1E, 0x14, 0x05, 0xFF) : new Color32(0x04, 0x19, 0x1D, 0xFF);

        private void RefreshAll()
        {
            var deck = SelectedDeck;
            bool legal = DeckLegal(deck);
            bool online = PlayerProfile.LoggedIn && network != null && network.IsConnected;

            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null) rows[i].SetSelected(i == selectedDeck);

            // Tabs
            StyleTab(onlineTabBg, onlineTabLabel, mode == 0, skin != null ? skin.badgeMonster : null, new Color32(0x1E, 0x14, 0x05, 0xFF));
            StyleTab(soloTabBg, soloTabLabel, mode == 1, skin != null ? skin.badgeTeal : null, new Color32(0x04, 0x19, 0x1D, 0xFF));

            // Banner
            if (bannerBg != null) bannerBg.color = mode == 0 ? new Color32(0x2A, 0x1A, 0x0E, 0xF2) : new Color32(0x12, 0x2A, 0x31, 0xF2);
            if (bannerFrame != null) bannerFrame.color = Accent;
            if (bannerKeyline != null) bannerKeyline.color = new Color(Accent.r, Accent.g, Accent.b, 0.25f);
            if (bannerDeco != null) bannerDeco.color = new Color(Accent.r, Accent.g, Accent.b, 0.4f);
            if (opponentCard != null) opponentCard.color = mode == 0 ? new Color32(0xE8, 0xD5, 0xA8, 0xFF) : new Color32(0xA5, 0xD8, 0xE2, 0xFF);
            if (opponentGem != null) opponentGem.color = AccentBright;
            if (bannerEyebrow != null)
            {
                bannerEyebrow.text = mode == 0 ? "RANKED QUEUE · SEASON OF ASH" : "TRIAL OF THE WARDEN";
                bannerEyebrow.color = new Color(Accent.r, Accent.g, Accent.b, 0.85f);
            }
            if (bannerTitle != null) bannerTitle.text = mode == 0 ? "Online Duel" : DiffTitle();
            if (bannerBlurb != null)
                bannerBlurb.text = mode == 0
                    ? "Face another duelist over the vault's relay. Both of you earn coins — the winner takes double."
                    : OppBlurb();
            if (stat1Label != null) stat1Label.text = mode == 0 ? "DUELISTS ONLINE" : "TRIAL REWARD";
            if (stat1Value != null) stat1Value.text = mode == 0 ? Mathf.Max(1, PlayerProfile.OnlineCount).ToString() : "+50";
            if (stat2Label != null) stat2Label.text = mode == 0 ? "DUEL REWARD" : "DAILY STREAK";
            if (stat2Value != null) stat2Value.text = mode == 0 ? "+100" : PlayerProfile.DailyStreak.ToString();

            // Start-Button
            bool canStart = legal && (mode == 1 || online);
            if (startButton != null) startButton.interactable = canStart;
            if (startBg != null)
            {
                startBg.sprite = canStart ? AccentBadge : null;
                startBg.color = canStart ? Color.white : new Color(0f, 0f, 0f, 0.4f);
            }
            if (startFrame != null)
                startFrame.color = canStart ? AccentBright : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.2f);
            if (startDiamond != null) startDiamond.color = canStart ? AccentInk : new Color32(0x5C, 0x51, 0x3F, 0xFF);
            if (startTitle != null)
            {
                startTitle.text = mode == 0 ? "QUICK MATCH" : "START TRIAL";
                startTitle.color = canStart ? AccentInk : new Color32(0x5C, 0x51, 0x3F, 0xFF);
            }
            if (startSub != null)
            {
                startSub.text = mode == 0
                    ? "CASUAL QUEUE · +100 COINS PER DUEL"
                    : $"{OppName(difficulty).ToUpperInvariant()} · {OppNote(difficulty)}";
                startSub.color = canStart ? new Color(AccentInk.r, AccentInk.g, AccentInk.b, 0.75f) : new Color32(0x5C, 0x51, 0x3F, 0xB0);
            }

            // Options-Gruppen
            if (onlineGroup != null) onlineGroup.SetActive(mode == 0);
            if (soloGroup != null) soloGroup.SetActive(mode == 1);
            if (createLobbyButton != null) createLobbyButton.interactable = legal && online;
            RefreshJoinButton();

            // Gegner-Chips (Roster liefert Namen/Notizen; Styling wie gehabt)
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                bool active = i == difficulty;
                if (i < difficultyNames.Length && difficultyNames[i] != null) difficultyNames[i].text = OppName(i).ToUpperInvariant();
                if (i < difficultyNotes.Length && difficultyNotes[i] != null) difficultyNotes[i].text = OppNote(i);
                if (i < difficultyBgs.Length && difficultyBgs[i] != null)
                    difficultyBgs[i].color = active ? new Color(143f / 255f, 198f / 255f, 210f / 255f, 0.16f) : new Color(0f, 0f, 0f, 0.4f);
                if (i < difficultyFrames.Length && difficultyFrames[i] != null)
                    difficultyFrames[i].color = active ? new Color32(0x8F, 0xC6, 0xD2, 0xFF) : new Color(143f / 255f, 198f / 255f, 210f / 255f, 0.25f);
                if (i < difficultyNames.Length && difficultyNames[i] != null)
                    difficultyNames[i].color = active ? new Color32(0xE4, 0xF4, 0xF8, 0xFF) : new Color32(0x7E, 0x8E, 0x94, 0xFF);
                if (i < difficultyNotes.Length && difficultyNotes[i] != null)
                    difficultyNotes[i].color = active ? new Color32(0xB4, 0xE2, 0xEC, 0xC0) : new Color32(0x7E, 0x8E, 0x94, 0xA0);
            }
            if (trialAdvice != null) trialAdvice.text = OppBlurb();
            if (trialReward != null) trialReward.text = "+50";

            // Illegal-Strip
            bool showStrip = deck != null && !legal;
            if (illegalStrip != null) illegalStrip.SetActive(showStrip);
            if (illegalText != null && deck != null)
                illegalText.text = !DeckIsOwned(deck)
                    ? $"\"{deck.Name}\" contains cards you no longer own — fix it in the Deck Builder."
                    : $"\"{deck.Name}\" has {deck.Cards.Count} cards — a legal deck needs {DeckMin}–{DeckMax}. Fix it in the Deck Builder.";

            RefreshRosterRows();
            ApplyTowerMode();
        }

        // ---------- Gegner-Roster (Fallback: statische Legacy-Schwierigkeiten) ----------
        private BotOpponentDefinition OpponentAt(int index) =>
            opponents != null && index >= 0 && index < opponents.Length ? opponents[index] : null;

        private BotOpponentDefinition CurrentOpponent => OpponentAt(difficulty);

        private string OppName(int index)
        {
            var opponent = OpponentAt(index);
            if (opponent != null) return opponent.displayName;
            return index < DiffNames.Length ? DiffNames[index] : "Bot";
        }

        private string OppNote(int index)
        {
            var opponent = OpponentAt(index);
            if (opponent != null) return opponent.chipNote;
            return index < DiffNotes.Length ? DiffNotes[index] : "";
        }

        private string OppBlurb()
        {
            var opponent = CurrentOpponent;
            if (opponent != null && !string.IsNullOrEmpty(opponent.blurb)) return opponent.blurb;
            return DiffAdvice[Mathf.Clamp(difficulty, 0, DiffAdvice.Length - 1)];
        }

        private string DiffTitle() => OppName(difficulty);

        private static void StyleTab(Image bg, TMP_Text label, bool active, Sprite activeSprite, Color activeInk)
        {
            if (bg != null)
            {
                bg.sprite = active ? activeSprite : null;
                bg.color = active ? Color.white : Color.clear;
            }
            if (label != null) label.color = active ? activeInk : new Color32(0x8C, 0x7B, 0x5F, 0xFF);
        }

        // ---------- Aktionen ----------
        private bool Prepare()
        {
            var deck = SelectedDeck;
            if (deck == null || !DeckLegal(deck)) return false;
            if (network == null || !network.IsConnected || !PlayerProfile.LoggedIn) return false;
            loadingMatch = false;
            network.SendHello(PlayerProfile.AccountName);
            return true;
        }

        private void PressStart()
        {
            if (mode == 0) QuickMatch();
            else if (mode == 2) StartTowerFlow();
            else StartCoroutine(StartSolo());
        }

        private void QuickMatch()
        {
            if (!Prepare()) return;
            network.SendQueue(Mathf.Clamp(selectedDeck, 0, Mathf.Max(0, PlayerProfile.Decks.Count - 1)));
            ShowOverlay("Searching for a duelist", $"Casual queue · both duelists earn coins, the winner double. {Mathf.Max(1, PlayerProfile.OnlineCount)} online right now.");
        }

        private IEnumerator StartSolo()
        {
            var deck = SelectedDeck;
            if (deck == null || !DeckLegal(deck)) yield break;
            ShowOverlay($"Waking {OppName(difficulty)}", OppBlurb());
            yield return new WaitForSecondsRealtime(1.2f);
            MatchContext.Clear();
            MatchContext.UseCustomLocalDeck = true;
            MatchContext.SoloDifficulty = difficulty;
            MatchContext.LocalDeckCards = new List<string>(deck.Cards);
            MatchContext.LocalExtraCards = new List<string>(deck.Extra);
            MatchContext.LocalDeckFinishes = deck.DeckFinishNumbers();
            MatchContext.LocalExtraFinishes = deck.ExtraFinishNumbers();
            MatchContext.LocalHero = deck.Hero;
            MatchContext.LocalName = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Duelist";

            // Gewählten Roster-Gegner übergeben (Duel-Szene baut den Bot daraus)
            FillBotContext(CurrentOpponent, null, 0, 0);
            SceneManager.LoadScene(duelSceneName);
        }

        private void CreateLobby()
        {
            if (!Prepare()) return;
            network.SendCreate();
        }

        private void CloseLobby()
        {
            lobbyCode = null;
            if (network != null) network.SendLeave();
            if (lobbyActive != null) lobbyActive.SetActive(false);
            if (lobbyIdle != null) lobbyIdle.SetActive(true);
        }

        private void OnJoinCodeChanged(string value)
        {
            string cleaned = (value ?? "").ToUpperInvariant();
            if (cleaned.Length > 6) cleaned = cleaned.Substring(0, 6);
            if (joinInput != null && joinInput.text != cleaned) joinInput.SetTextWithoutNotify(cleaned);
            RefreshJoinButton();
        }

        private void RefreshJoinButton()
        {
            bool legal = DeckLegal(SelectedDeck);
            bool online = PlayerProfile.LoggedIn && network != null && network.IsConnected;
            bool ready = legal && online && joinInput != null && joinInput.text.Length == 6;
            if (joinButton != null) joinButton.interactable = ready;
            if (joinBg != null)
            {
                joinBg.sprite = ready && skin != null ? skin.badgeMonster : null;
                joinBg.color = ready ? Color.white : new Color(0f, 0f, 0f, 0.4f);
            }
            if (joinLabel != null)
                joinLabel.color = ready ? new Color32(0x1E, 0x14, 0x05, 0xFF) : new Color32(0x5C, 0x51, 0x3F, 0xFF);
        }

        private void JoinLobby()
        {
            if (!Prepare()) return;
            string code = joinInput != null ? joinInput.text.Trim().ToUpperInvariant() : "";
            if (code.Length != 6) return;
            network.SendJoin(code);
            ShowOverlay("Joining the lobby", $"Sealing into lobby {code}…");
        }

        private void CancelSearch()
        {
            if (loadingMatch) return;
            searching = false;
            if (network != null) network.SendLeave();
            HideOverlay();
        }

        // ---------- Overlay ----------
        private void ShowOverlay(string title, string note)
        {
            searching = true;
            if (overlay != null) overlay.SetActive(true);
            if (overlayFrame != null) overlayFrame.color = Accent;
            if (spinnerRing != null) spinnerRing.color = AccentBright;
            if (overlayTitle != null) overlayTitle.text = title;
            if (overlayNote != null) overlayNote.text = note;
            var deck = SelectedDeck;
            if (receiptText != null && deck != null)
                receiptText.text = $"Queued with {deck.Name} · {deck.Cards.Count} cards";
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
            if (sweepRoutine != null) StopCoroutine(sweepRoutine);
            if (overlaySweep != null) sweepRoutine = StartCoroutine(SweepLoop());
        }

        private void HideOverlay()
        {
            searching = false;
            if (sweepRoutine != null) { StopCoroutine(sweepRoutine); sweepRoutine = null; }
            if (overlay != null) overlay.SetActive(false);
        }

        private IEnumerator SweepLoop()
        {
            var track = (RectTransform)overlaySweep.parent;
            while (overlay != null && overlay.activeSelf)
            {
                float width = track.rect.width;
                float band = width * 0.4f;
                overlaySweep.sizeDelta = new Vector2(band, overlaySweep.sizeDelta.y);
                float elapsed = 0f;
                const float duration = 1.6f;
                while (elapsed < duration && overlay.activeSelf)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    overlaySweep.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.6f, width * 0.6f + band, k), 0f);
                    yield return null;
                }
            }
        }

        // ---------- Netzwerk ----------
        private void HandleDisconnected(string reason)
        {
            HideOverlay();
            RefreshAll();
        }

        private void HandleMessage(NetMessage message)
        {
            switch (message.t)
            {
                case "profile":
                    BuildRows();
                    RefreshAll();
                    break;
                case "queued":
                    break;
                case "lobby":
                    lobbyCode = message.code;
                    if (lobbyCodeText != null) lobbyCodeText.text = lobbyCode;
                    if (lobbyIdle != null) lobbyIdle.SetActive(false);
                    if (lobbyActive != null) lobbyActive.SetActive(true);
                    break;
                case "error":
                    HideOverlay();
                    if (illegalStrip != null && illegalText != null)
                    {
                        illegalStrip.SetActive(true);
                        illegalText.text = message.msg;
                    }
                    break;
                case "peer_left":
                    if (!loadingMatch) HideOverlay();
                    break;
                case "sduel_start":
                    // Server-autoritatives Duell: der Server kennt beide Decks und rechnet selbst.
                    // Der Münzwurf läuft im Duell (Server-Request) — direkt in die Duel-Szene.
                    if (loadingMatch) break;
                    loadingMatch = true;
                    MatchContext.Clear();
                    MatchContext.IsServerMatch = true;
                    MatchContext.LocalIsPlayerA = message.youAre == "A";
                    MatchContext.LocalName = PlayerProfile.AccountName;
                    MatchContext.RemoteName = string.IsNullOrEmpty(message.opponent) ? "Opponent" : message.opponent;
                    MatchContext.SetRemoteCosmetics(message.oppSlots, message.oppIds);
                    HideOverlay();
                    StartCoroutine(EnterDuel());
                    break;
            }
        }


        /// <summary>
        /// Erst der Moment: „MATCH FOUND" steht kurz im Raum, dann der Lade-Übergang
        /// (Handoff „Duel Load") und das Brett. Der Vorhang hält, bis der DuelHost
        /// ihn freigibt.
        /// </summary>
        private IEnumerator EnterDuel()
        {
            var plate = BuildMatchFoundPopup();
            SfxManager.Claim();
            // kurzer Punch beim Erscheinen, dann stehen lassen
            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                if (plate != null) plate.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, t / 0.18f);
                yield return null;
            }
            if (plate != null) plate.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(1.4f);

            var deck = SelectedDeck;
            int deckCount = deck != null ? deck.Cards.Count : 40;
            DuelLoadTransition.Play(null, MatchContext.RemoteName,
                deck != null ? deck.Name : "", deckCount > 0 ? deckCount : 40);
            yield return null;
            SceneManager.LoadScene("Duel");
        }

        /// <summary>Zentrales „MATCH FOUND"-Popup auf eigenem Canvas über allem.</summary>
        private RectTransform BuildMatchFoundPopup()
        {
            var deco = TransitionSkin.Load();   // Fonts + Rahmen; das Feld `skin` ist der Karten-Skin
            var host = new GameObject("~MatchFound", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;
            var root = (RectTransform)host.transform;

            var scrim = new GameObject("Scrim", typeof(RectTransform), typeof(Image));
            var scrimRect = (RectTransform)scrim.transform;
            scrimRect.SetParent(root, false);
            scrimRect.anchorMin = Vector2.zero; scrimRect.anchorMax = Vector2.one;
            scrimRect.offsetMin = Vector2.zero; scrimRect.offsetMax = Vector2.zero;
            var scrimImg = scrim.GetComponent<Image>();
            scrimImg.color = new Color(0f, 0f, 0f, 0.78f);

            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(root, false);
            plateRect.anchorMin = plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.sizeDelta = new Vector2(560f, 200f);
            var plateImg = plate.GetComponent<Image>();
            plateImg.color = new Color(0.055f, 0.071f, 0.106f, 0.97f);

            if (deco != null && deco.frame != null)
            {
                var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
                var frameRect = (RectTransform)frame.transform;
                frameRect.SetParent(plateRect, false);
                frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = Vector2.zero; frameRect.offsetMax = Vector2.zero;
                var frameImg = frame.GetComponent<Image>();
                frameImg.sprite = deco.frame; frameImg.type = Image.Type.Sliced;
                frameImg.color = new Color(0.784f, 0.643f, 0.361f, 1f);
                frameImg.raycastTarget = false;
            }

            TMP_Text MakeLine(string name, string text, TMPro.TMP_FontAsset font, float size, float spacing, Color color, float y)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(plateRect, false);
                rect.anchorMin = new Vector2(0f, 0.5f); rect.anchorMax = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(0f, size * 1.6f);
                rect.anchoredPosition = new Vector2(0f, y);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = text; tmp.fontSize = size; tmp.characterSpacing = spacing;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = color; tmp.raycastTarget = false;
                if (font != null) tmp.font = font;
                return tmp;
            }

            MakeLine("Eyebrow", "CASUAL DUEL", deco != null ? deco.oswald : null, 13f, 40f,
                new Color(0.612f, 0.541f, 0.416f, 1f), 62f);
            MakeLine("Headline", "MATCH FOUND", deco != null ? deco.cinzel : null, 46f, 8f,
                new Color(0.922f, 0.808f, 0.541f, 1f), 8f);
            MakeLine("Sub", $"{MatchContext.RemoteName} steps to the board…",
                deco != null ? deco.spectral : null, 18f, 0f,
                new Color(0.635f, 0.541f, 0.412f, 1f), -52f);

            return plateRect;
        }

        // ================== THE TOWER ==================

        private int TowerFloorCount() => tower != null && tower.floors != null ? tower.floors.Count : 0;

        private TowerFloorDefinition FloorAt(int floorNumber) =>
            tower != null && tower.floors != null && floorNumber >= 1 && floorNumber <= tower.floors.Count
                ? tower.floors[floorNumber - 1] : null;

        /// <summary>Portrait der Ebene: eigenes Sprite, sonst das Artwork der Heldenkarte des Gegner-Decks.</summary>
        private static Sprite KeeperPortrait(TowerFloorDefinition floor)
        {
            if (floor == null) return null;
            if (floor.portrait != null) return floor.portrait;
            var hero = floor.opponent != null && floor.opponent.deck != null ? floor.opponent.deck.playerCard : null;
            return hero != null ? hero.artwork : null;
        }

        /// <summary>Gemeinsamer Bot-Kontext für Solo und Turm (Overrides > Gegnerwerte).</summary>
        private static void FillBotContext(BotOpponentDefinition opponent, string nameOverride, int lpOverride, int manaOverride)
        {
            if (opponent == null || opponent.deck == null) return;
            MatchContext.BotName = string.IsNullOrEmpty(nameOverride) ? opponent.displayName : nameOverride;
            MatchContext.BotHero = opponent.deck.playerCard != null ? opponent.deck.playerCard.cardName : "";
            foreach (var card in opponent.deck.cards)
            {
                if (card == null) continue;
                if (card is ReliquaryCardData) MatchContext.BotExtraCards.Add(card.cardName);
                else MatchContext.BotDeckCards.Add(card.cardName);
            }
            foreach (var card in opponent.deck.extraCards)
                if (card != null) MatchContext.BotExtraCards.Add(card.cardName);
            MatchContext.BotLifePoints = lpOverride > 0 ? lpOverride : opponent.lifePointsOverride;
            MatchContext.BotBonusMana = manaOverride > 0 ? manaOverride : opponent.bonusManaPerTurn;
            MatchContext.BotNovice = opponent.noviceMode;
        }

        // ---------- Kleine UI-Fabrik (Laufzeit-Elemente im Turm-Stil) ----------

        private static readonly Color TowerGold = new Color32(0xC8, 0xA4, 0x5C, 0xFF);
        private static readonly Color TowerGoldBright = new Color32(0xEB, 0xCE, 0x8A, 0xFF);
        private static readonly Color TowerInk = new Color32(0x1E, 0x14, 0x05, 0xFF);
        private static readonly Color TowerMuted = new Color32(0x8C, 0x7B, 0x5F, 0xFF);

        private static RectTransform MakeUiRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image MakeUiImage(string name, Transform parent, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rect = MakeUiRect(name, parent);
            var img = rect.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sliced && sprite != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text MakeUiText(string name, Transform parent, TMP_FontAsset font, float size, Color color, string text = "")
        {
            var rect = MakeUiRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }

        // ---------- Dritter Tab ----------

        private void BuildTowerTab()
        {
            if (soloTabButton == null) return;
            var template = soloTabButton.gameObject;
            var clone = Instantiate(template, template.transform.parent);
            clone.name = "TowerTabButton";
            var rect = (RectTransform)clone.transform;
            var src = (RectTransform)template.transform;
            rect.anchoredPosition = src.anchoredPosition + new Vector2(src.rect.width + 8f, 0f);

            towerTabButton = clone.GetComponent<Button>();
            towerTabButton.onClick.RemoveAllListeners();
            towerTabButton.onClick.AddListener(() => SetMode(2));
            towerTabBg = clone.GetComponent<Image>();
            towerTabLabel = clone.GetComponentInChildren<TMP_Text>(true);
            if (towerTabLabel != null)
            {
                towerTabLabel.text = "THE TOWER";
                towerTabLabel.fontSizeMin = 8f;
                towerTabLabel.enableAutoSizing = true;
            }
        }

        // ---------- Dynamische Gegner-Liste (Solo-Tab, alle Roster-Einträge) ----------

        private void BuildRosterList()
        {
            if (opponents == null || opponents.Length == 0) return;
            if (difficultyButtons == null || difficultyButtons.Length == 0 || difficultyButtons[0] == null) return;

            // Fläche = Umriss der bisherigen festen Chips, im selben Elternobjekt
            var parent = (RectTransform)difficultyButtons[0].transform.parent;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var chip in difficultyButtons)
            {
                if (chip == null) continue;
                var r = (RectTransform)chip.transform;
                Vector2 lo = (Vector2)r.localPosition + r.rect.min;
                Vector2 hi = (Vector2)r.localPosition + r.rect.max;
                min = Vector2.Min(min, lo); max = Vector2.Max(max, hi);
                chip.gameObject.SetActive(false);   // die alten fünf verschwinden
            }

            var scrollGo = MakeUiRect("RosterScroll", parent);
            scrollGo.anchorMin = scrollGo.anchorMax = new Vector2(0.5f, 0.5f);
            scrollGo.sizeDelta = max - min;
            scrollGo.localPosition = (min + max) * 0.5f;
            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            var viewport = MakeUiRect("Viewport", scrollGo);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;   // fängt das Mausrad
            scroll.viewport = viewport;

            var content = MakeUiRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            scroll.content = content;

            const float rowH = 54f, gap = 6f;
            rosterRows.Clear();
            for (int i = 0; i < opponents.Length; i++)
            {
                int index = i;
                var row = MakeUiRect("Opponent_" + i, content);
                row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(0f, rowH);
                row.anchoredPosition = new Vector2(0f, -i * (rowH + gap));

                var bg = row.gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.4f);
                var button = row.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { SfxManager.Click(); difficulty = index; RefreshAll(); });

                var frame = MakeUiImage("Frame", row, Color.white, skin != null ? skin.whiteFrame : null, true);
                frame.rectTransform.anchorMin = Vector2.zero; frame.rectTransform.anchorMax = Vector2.one;
                frame.rectTransform.offsetMin = Vector2.zero; frame.rectTransform.offsetMax = Vector2.zero;

                var name = MakeUiText("Name", row, null, 15f, Color.white);
                name.rectTransform.anchorMin = new Vector2(0f, 0.5f); name.rectTransform.anchorMax = new Vector2(1f, 1f);
                name.rectTransform.offsetMin = new Vector2(12f, 0f); name.rectTransform.offsetMax = new Vector2(-12f, -4f);
                name.alignment = TextAlignmentOptions.MidlineLeft;
                name.enableAutoSizing = true; name.fontSizeMin = 10f; name.fontSizeMax = 15f;

                var note = MakeUiText("Note", row, null, 10f, TowerMuted);
                note.rectTransform.anchorMin = new Vector2(0f, 0f); note.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                note.rectTransform.offsetMin = new Vector2(12f, 4f); note.rectTransform.offsetMax = new Vector2(-12f, 0f);
                note.alignment = TextAlignmentOptions.MidlineLeft;
                note.characterSpacing = 12f;

                rosterRows.Add((bg, frame, name, note));
            }
            content.sizeDelta = new Vector2(0f, opponents.Length * (rowH + gap) - gap);
            scroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshRosterRows()
        {
            for (int i = 0; i < rosterRows.Count && i < opponents.Length; i++)
            {
                bool active = i == difficulty;
                var (bg, frame, name, note) = rosterRows[i];
                if (bg != null) bg.color = active ? new Color(143f / 255f, 198f / 255f, 210f / 255f, 0.16f) : new Color(0f, 0f, 0f, 0.4f);
                if (frame != null) frame.color = active ? new Color32(0x8F, 0xC6, 0xD2, 0xFF) : new Color(143f / 255f, 198f / 255f, 210f / 255f, 0.25f);
                if (name != null)
                {
                    name.text = OppName(i).ToUpperInvariant();
                    name.color = active ? new Color32(0xE4, 0xF4, 0xF8, 0xFF) : new Color32(0x7E, 0x8E, 0x94, 0xFF);
                }
                if (note != null)
                {
                    note.text = OppNote(i);
                    note.color = active ? new Color32(0xB4, 0xE2, 0xEC, 0xC0) : new Color32(0x7E, 0x8E, 0x94, 0xA0);
                }
            }
        }

        // ---------- Turm-Panel (Ebenen-Leiter) ----------

        private void EnsureTowerGroup()
        {
            if (towerGroup != null || soloGroup == null) return;
            var soloRect = (RectTransform)soloGroup.transform;
            towerGroup = MakeUiRect("TowerGroup", soloRect.parent);
            towerGroup.anchorMin = soloRect.anchorMin; towerGroup.anchorMax = soloRect.anchorMax;
            towerGroup.pivot = soloRect.pivot;
            towerGroup.anchoredPosition = soloRect.anchoredPosition;
            towerGroup.sizeDelta = soloRect.sizeDelta;

            var scrollGo = MakeUiRect("FloorScroll", towerGroup);
            scrollGo.anchorMin = Vector2.zero; scrollGo.anchorMax = Vector2.one;
            scrollGo.offsetMin = new Vector2(0f, 8f); scrollGo.offsetMax = new Vector2(0f, -8f);
            towerScroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            towerScroll.horizontal = false; towerScroll.vertical = true;
            towerScroll.movementType = ScrollRect.MovementType.Clamped;
            towerScroll.scrollSensitivity = 30f;
            var viewport = MakeUiRect("Viewport", scrollGo);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            towerScroll.viewport = viewport;

            var content = MakeUiRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            towerScroll.content = content;

            // Von OBEN nach unten bauen: Ebene 15 zuoberst, Ebene 1 zuunterst —
            // der Blick klettert also wirklich den Turm hinauf.
            int count = TowerFloorCount();
            const float rowH = 52f, gap = 6f;
            towerRows.Clear();
            for (int i = 0; i < count; i++)
            {
                int floorNumber = count - i;   // oberste Zeile = höchste Ebene
                var row = MakeUiRect("Floor_" + floorNumber, content);
                row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(0f, rowH);
                row.anchoredPosition = new Vector2(0f, -i * (rowH + gap));

                var bg = row.gameObject.AddComponent<Image>();
                var button = row.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                int captured = floorNumber;
                button.onClick.AddListener(() =>
                {
                    if (captured > PlayerProfile.TowerFloor + 1) return;   // verschlossen
                    SfxManager.Click();
                    selectedFloor = captured;
                    RefreshAll();
                });

                var frame = MakeUiImage("Frame", row, Color.white, skin != null ? skin.whiteFrame : null, true);
                frame.rectTransform.anchorMin = Vector2.zero; frame.rectTransform.anchorMax = Vector2.one;
                frame.rectTransform.offsetMin = Vector2.zero; frame.rectTransform.offsetMax = Vector2.zero;

                var name = MakeUiText("Name", row, null, 14f, Color.white);
                name.rectTransform.anchorMin = new Vector2(0f, 0f); name.rectTransform.anchorMax = new Vector2(0.72f, 1f);
                name.rectTransform.offsetMin = new Vector2(12f, 0f); name.rectTransform.offsetMax = Vector2.zero;
                name.alignment = TextAlignmentOptions.MidlineLeft;
                name.enableAutoSizing = true; name.fontSizeMin = 9f; name.fontSizeMax = 14f;

                var state = MakeUiText("State", row, null, 10f, TowerMuted);
                state.rectTransform.anchorMin = new Vector2(0.72f, 0f); state.rectTransform.anchorMax = new Vector2(1f, 1f);
                state.rectTransform.offsetMin = Vector2.zero; state.rectTransform.offsetMax = new Vector2(-12f, 0f);
                state.alignment = TextAlignmentOptions.MidlineRight;
                state.characterSpacing = 14f;

                towerRows.Add((row, bg, frame, name, state));
            }
            content.sizeDelta = new Vector2(0f, count * (rowH + gap) - gap);
        }

        private void RefreshTowerRows()
        {
            int count = TowerFloorCount();
            int cleared = PlayerProfile.TowerFloor;
            for (int i = 0; i < towerRows.Count; i++)
            {
                int floorNumber = count - i;
                var floor = FloorAt(floorNumber);
                var (row, bg, frame, name, state) = towerRows[i];
                bool sealedFloor = floorNumber <= cleared;
                bool active = floorNumber == cleared + 1;
                bool selected = floorNumber == selectedFloor;
                bool locked = floorNumber > cleared + 1;

                if (name != null)
                {
                    string keeper = floor != null ? floor.keeperName : "???";
                    name.text = $"FLOOR {ToRoman(floorNumber)} — {(locked ? "SEALED ABOVE" : keeper.ToUpperInvariant())}";
                    name.color = locked ? new Color32(0x4C, 0x42, 0x33, 0xFF)
                        : selected ? TowerGoldBright
                        : sealedFloor ? new Color32(0xA8, 0x93, 0x66, 0xFF)
                        : new Color32(0xE8, 0xD5, 0xA8, 0xFF);
                }
                if (state != null)
                {
                    state.text = sealedFloor ? "SEAL RENEWED" : active ? "AWAKE" : "";
                    state.color = sealedFloor ? new Color32(0x7A, 0xCD, 0x96, 0xC0) : TowerGoldBright;
                    if (active)
                    {
                        float pulse = 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f);
                        state.color = new Color(TowerGoldBright.r, TowerGoldBright.g, TowerGoldBright.b, pulse);
                    }
                }
                if (bg != null)
                    bg.color = selected ? new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.18f)
                        : locked ? new Color(0f, 0f, 0f, 0.55f) : new Color(0f, 0f, 0f, 0.38f);
                if (frame != null)
                    frame.color = selected ? TowerGoldBright
                        : locked ? new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.12f)
                        : new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.3f);
            }
        }

        private static string ToRoman(int number)
        {
            string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
            string[] tens = { "", "X", "XX" };
            return number >= 1 && number <= 29 ? tens[number / 10] + ones[number % 10] : number.ToString();
        }

        /// <summary>Übermalt Banner, Start-Button und Gruppen, wenn der Turm-Tab aktiv ist.</summary>
        private void ApplyTowerMode()
        {
            bool towerMode = mode == 2;

            // Tab-Styling (der Turm ist gold — die Geschichte des Gewölbes)
            if (towerTabBg != null)
            {
                towerTabBg.sprite = towerMode && skin != null ? skin.badgeMonster : null;
                towerTabBg.color = towerMode ? Color.white : Color.clear;
            }
            if (towerTabLabel != null) towerTabLabel.color = towerMode ? TowerInk : TowerMuted;

            if (towerGroup == null && towerMode) EnsureTowerGroup();
            if (towerGroup != null) towerGroup.gameObject.SetActive(towerMode);
            if (!towerMode) return;

            if (soloGroup != null) soloGroup.SetActive(false);
            if (onlineGroup != null) onlineGroup.SetActive(false);
            RefreshTowerRows();

            int count = TowerFloorCount();
            int cleared = PlayerProfile.TowerFloor;
            var floor = FloorAt(selectedFloor);
            bool topReached = cleared >= count && count > 0;
            bool replay = selectedFloor <= cleared;

            if (bannerBg != null) bannerBg.color = new Color32(0x22, 0x18, 0x0A, 0xF2);
            if (bannerFrame != null) bannerFrame.color = TowerGold;
            if (bannerKeyline != null) bannerKeyline.color = new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.25f);
            if (bannerDeco != null) bannerDeco.color = new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.4f);
            if (opponentCard != null) opponentCard.color = new Color32(0xE8, 0xD5, 0xA8, 0xFF);
            if (opponentGem != null) opponentGem.color = TowerGoldBright;
            if (bannerEyebrow != null)
            {
                bannerEyebrow.text = $"THE TOWER · FLOOR {ToRoman(selectedFloor)} OF {ToRoman(Mathf.Max(count, 1))}";
                bannerEyebrow.color = new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.85f);
            }
            if (bannerTitle != null) bannerTitle.text = floor != null ? floor.keeperName : "The Tower";
            if (bannerBlurb != null)
                bannerBlurb.text = topReached && selectedFloor > count
                    ? "Every seal is renewed. The Tower is quiet — for now."
                    : floor != null ? floor.blurb : "";
            if (stat1Label != null) stat1Label.text = "FIRST-CLEAR REWARD";
            if (stat1Value != null) stat1Value.text = replay ? "—" : "+5 PACKS";
            if (stat2Label != null) stat2Label.text = "SEALS RENEWED";
            if (stat2Value != null) stat2Value.text = $"{Mathf.Min(cleared, count)}/{count}";

            var deck = SelectedDeck;
            bool legal = DeckLegal(deck);
            bool canStart = legal && floor != null && selectedFloor <= cleared + 1;
            if (startButton != null) startButton.interactable = canStart;
            if (startBg != null)
            {
                startBg.sprite = canStart && skin != null ? skin.badgeMonster : null;
                startBg.color = canStart ? Color.white : new Color(0f, 0f, 0f, 0.4f);
            }
            if (startFrame != null) startFrame.color = canStart ? TowerGoldBright : new Color(TowerGold.r, TowerGold.g, TowerGold.b, 0.2f);
            if (startDiamond != null) startDiamond.color = canStart ? TowerInk : new Color32(0x5C, 0x51, 0x3F, 0xFF);
            if (startTitle != null)
            {
                startTitle.text = replay ? "DUEL AGAIN" : "ENTER THE FLOOR";
                startTitle.color = canStart ? TowerInk : new Color32(0x5C, 0x51, 0x3F, 0xFF);
            }
            if (startSub != null)
            {
                startSub.text = floor != null
                    ? $"{floor.keeperName.ToUpperInvariant()} · {(replay ? "SEAL ALREADY RENEWED" : "+5 RELIC PACKS ON FIRST CLEAR")}"
                    : "THE TOWER IS QUIET";
                startSub.color = canStart ? new Color(TowerInk.r, TowerInk.g, TowerInk.b, 0.75f) : new Color32(0x5C, 0x51, 0x3F, 0xB0);
            }
        }

        // ---------- Dialog → Duell ----------

        private void StartTowerFlow()
        {
            var floor = FloorAt(selectedFloor);
            if (floor == null || !DeckLegal(SelectedDeck)) return;
            bool replay = selectedFloor <= PlayerProfile.TowerFloor;
            if (replay || floor.dialog == null || floor.dialog.Count == 0)
            {
                StartCoroutine(LaunchTowerDuel(floor));
                return;
            }
            ShowTowerDialog(floor, floor.keeperName, floor.dialog, "BEGIN THE DUEL",
                () => StartCoroutine(LaunchTowerDuel(floor)));
        }

        private IEnumerator LaunchTowerDuel(TowerFloorDefinition floor)
        {
            var deck = SelectedDeck;
            if (deck == null || floor == null || floor.opponent == null) yield break;
            yield return null;
            MatchContext.Clear();
            MatchContext.UseCustomLocalDeck = true;
            MatchContext.TowerFloor = selectedFloor;
            MatchContext.LocalDeckCards = new List<string>(deck.Cards);
            MatchContext.LocalExtraCards = new List<string>(deck.Extra);
            MatchContext.LocalDeckFinishes = deck.DeckFinishNumbers();
            MatchContext.LocalExtraFinishes = deck.ExtraFinishNumbers();
            MatchContext.LocalHero = deck.Hero;
            MatchContext.LocalName = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Duelist";
            FillBotContext(floor.opponent, floor.keeperName, floor.lifePointsOverride, floor.bonusManaPerTurn);
            SceneManager.LoadScene(duelSceneName);
        }

        private void ShowVictoryLine(int wonFloor)
        {
            var floor = FloorAt(wonFloor);
            if (floor == null || string.IsNullOrEmpty(floor.victoryLine)) return;
            var line = new List<TowerLine> { new TowerLine { speaker = floor.keeperName.ToUpperInvariant(), text = floor.victoryLine } };
            ShowTowerDialog(floor, floor.keeperName, line, "CONTINUE", null);
            SetMode(2);
        }

        /// <summary>
        /// Dialog-Overlay im Reliquary-Stil: Portrait links, Textplatte unten,
        /// Klick blättert; die letzte Zeile trägt den Abschluss-Knopf.
        /// </summary>
        private void ShowTowerDialog(TowerFloorDefinition floor, string keeperName, List<TowerLine> lines, string finishLabel, System.Action onFinish)
        {
            if (dialogOverlay != null) Destroy(dialogOverlay);
            var deco = TransitionSkin.Load();

            dialogOverlay = new GameObject("~TowerDialog", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = dialogOverlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 530;
            var root = (RectTransform)dialogOverlay.transform;

            var scrim = MakeUiImage("Scrim", root, new Color(0.01f, 0.008f, 0.005f, 0.94f));
            scrim.rectTransform.anchorMin = Vector2.zero; scrim.rectTransform.anchorMax = Vector2.one;
            scrim.rectTransform.offsetMin = Vector2.zero; scrim.rectTransform.offsetMax = Vector2.zero;
            scrim.raycastTarget = true;

            // Portrait links — dein Bild, sobald es zugewiesen ist; bis dahin der Held des Decks
            var portraitSprite = KeeperPortrait(floor);
            if (portraitSprite != null)
            {
                var portrait = MakeUiImage("Portrait", root, Color.white);
                portrait.sprite = portraitSprite;
                portrait.preserveAspect = true;
                portrait.rectTransform.anchorMin = new Vector2(0f, 0f);
                portrait.rectTransform.anchorMax = new Vector2(0.42f, 1f);
                portrait.rectTransform.offsetMin = new Vector2(40f, 40f);
                portrait.rectTransform.offsetMax = new Vector2(0f, -40f);
            }

            var plate = MakeUiImage("Plate", root, new Color(0.055f, 0.045f, 0.03f, 0.97f));
            plate.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            plate.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            plate.rectTransform.pivot = new Vector2(0.5f, 0f);
            plate.rectTransform.sizeDelta = new Vector2(860f, 190f);
            plate.rectTransform.anchoredPosition = new Vector2(90f, 46f);
            if (deco != null && deco.frame != null)
            {
                var frame = MakeUiImage("Frame", plate.rectTransform, TowerGold, deco.frame, true);
                frame.rectTransform.anchorMin = Vector2.zero; frame.rectTransform.anchorMax = Vector2.one;
                frame.rectTransform.offsetMin = Vector2.zero; frame.rectTransform.offsetMax = Vector2.zero;
            }

            var speaker = MakeUiText("Speaker", plate.rectTransform, deco != null ? deco.oswald : null, 14f, TowerGoldBright);
            speaker.rectTransform.anchorMin = new Vector2(0f, 1f); speaker.rectTransform.anchorMax = new Vector2(1f, 1f);
            speaker.rectTransform.pivot = new Vector2(0.5f, 1f);
            speaker.rectTransform.sizeDelta = new Vector2(-48f, 26f);
            speaker.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            speaker.characterSpacing = 26f;
            speaker.alignment = TextAlignmentOptions.MidlineLeft;

            var body = MakeUiText("Body", plate.rectTransform, deco != null ? deco.spectral : null, 19f, new Color32(0xE8, 0xDC, 0xC2, 0xFF));
            body.rectTransform.anchorMin = new Vector2(0f, 0f); body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.offsetMin = new Vector2(24f, 50f); body.rectTransform.offsetMax = new Vector2(-24f, -48f);
            body.alignment = TextAlignmentOptions.TopLeft;

            var hint = MakeUiText("Hint", plate.rectTransform, deco != null ? deco.oswald : null, 12f, TowerMuted);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f); hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.sizeDelta = new Vector2(-48f, 22f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            hint.characterSpacing = 24f;
            hint.alignment = TextAlignmentOptions.MidlineRight;
            hint.text = "CLICK TO CONTINUE";

            // LEAVE oben rechts — man kann den Keeper stehen lassen
            var leaveGo = MakeUiRect("Leave", root);
            leaveGo.anchorMin = leaveGo.anchorMax = new Vector2(1f, 1f);
            leaveGo.pivot = new Vector2(1f, 1f);
            leaveGo.sizeDelta = new Vector2(120f, 40f);
            leaveGo.anchoredPosition = new Vector2(-24f, -24f);
            var leaveBg = leaveGo.gameObject.AddComponent<Image>();
            leaveBg.color = new Color(0f, 0f, 0f, 0.5f);
            var leaveButton = leaveGo.gameObject.AddComponent<Button>();
            leaveButton.onClick.AddListener(() => { SfxManager.Click(); Destroy(dialogOverlay); dialogOverlay = null; });
            var leaveLabel = MakeUiText("Label", leaveGo, deco != null ? deco.oswald : null, 13f, TowerMuted, "LEAVE");
            leaveLabel.rectTransform.anchorMin = Vector2.zero; leaveLabel.rectTransform.anchorMax = Vector2.one;
            leaveLabel.rectTransform.offsetMin = Vector2.zero; leaveLabel.rectTransform.offsetMax = Vector2.zero;
            leaveLabel.alignment = TextAlignmentOptions.Center;
            leaveLabel.characterSpacing = 20f;

            int lineIndex = 0;
            Button finishButton = null;
            void ShowLine()
            {
                var line = lines[lineIndex];
                speaker.text = string.IsNullOrEmpty(line.speaker) ? keeperName.ToUpperInvariant() : line.speaker;
                speaker.color = line.speaker == "YOU" ? new Color32(0x8F, 0xC6, 0xD2, 0xFF) : TowerGoldBright;
                body.text = line.text;
                bool last = lineIndex >= lines.Count - 1;
                hint.gameObject.SetActive(!last);
                if (last)
                {
                    var buttonGo = MakeUiRect("FinishButton", plate.rectTransform);
                    buttonGo.anchorMin = new Vector2(1f, 0f); buttonGo.anchorMax = new Vector2(1f, 0f);
                    buttonGo.pivot = new Vector2(1f, 0f);
                    buttonGo.sizeDelta = new Vector2(240f, 44f);
                    buttonGo.anchoredPosition = new Vector2(-16f, 12f);
                    var buttonBg = buttonGo.gameObject.AddComponent<Image>();
                    buttonBg.color = new Color(0.784f, 0.643f, 0.361f, 0.96f);
                    finishButton = buttonGo.gameObject.AddComponent<Button>();
                    finishButton.onClick.AddListener(() =>
                    {
                        SfxManager.Click();
                        Destroy(dialogOverlay); dialogOverlay = null;
                        onFinish?.Invoke();
                    });
                    var label = MakeUiText("Label", buttonGo, deco != null ? deco.oswald : null, 16f, TowerInk, finishLabel);
                    label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                    label.rectTransform.offsetMin = Vector2.zero; label.rectTransform.offsetMax = Vector2.zero;
                    label.alignment = TextAlignmentOptions.Center;
                    label.characterSpacing = 10f;
                }
            }

            var advance = scrim.gameObject.AddComponent<Button>();
            advance.transition = Selectable.Transition.None;
            advance.onClick.AddListener(() =>
            {
                if (lineIndex >= lines.Count - 1) return;   // letzte Zeile: nur der Knopf schliesst
                SfxManager.Click();
                lineIndex++;
                ShowLine();
            });

            ShowLine();
        }

    }
}
