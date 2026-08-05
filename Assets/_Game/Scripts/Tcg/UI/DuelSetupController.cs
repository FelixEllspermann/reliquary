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
        private int mode;             // 0 online, 1 solo
        private int selectedDeck;
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
            mode = OpenSolo ? 1 : 0;
            OpenSolo = false;

            if (onlineTabButton != null) onlineTabButton.onClick.AddListener(() => SetMode(0));
            if (soloTabButton != null) soloTabButton.onClick.AddListener(() => SetMode(1));
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
            // Erstes legales Deck vorwählen
            for (int i = 0; i < PlayerProfile.Decks.Count; i++)
                if (DeckLegal(PlayerProfile.Decks[i])) { selectedDeck = i; break; }
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
                row.Setup(deck, i, legal, label, catalog, () => { selectedDeck = index; RefreshAll(); });
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
            var opponent = CurrentOpponent;
            if (opponent != null && opponent.deck != null)
            {
                MatchContext.BotName = opponent.displayName;
                MatchContext.BotHero = opponent.deck.playerCard != null ? opponent.deck.playerCard.cardName : "";
                foreach (var card in opponent.deck.cards)
                {
                    if (card == null) continue;
                    if (card is ReliquaryCardData) MatchContext.BotExtraCards.Add(card.cardName);
                    else MatchContext.BotDeckCards.Add(card.cardName);
                }
                foreach (var card in opponent.deck.extraCards)
                    if (card != null) MatchContext.BotExtraCards.Add(card.cardName);
                MatchContext.BotLifePoints = opponent.lifePointsOverride;
                MatchContext.BotBonusMana = opponent.bonusManaPerTurn;
                MatchContext.BotNovice = opponent.noviceMode;
            }
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
                    ShowOverlay("Duelist found", $"{MatchContext.RemoteName} steps to the board…");
                    StartCoroutine(EnterDuel());
                    break;
            }
        }


        /// <summary>
        /// Der Lade-Übergang (Handoff „Duel Load"), dann das Brett. Der Vorhang hält,
        /// bis der DuelHost ihn freigibt — vorher tat das die CoinToss-Szene, die es
        /// nicht mehr gibt.
        /// </summary>
        private IEnumerator EnterDuel()
        {
            var deck = SelectedDeck;
            int deckCount = deck != null ? deck.Cards.Count : 40;
            DuelLoadTransition.Play(null, MatchContext.RemoteName,
                deck != null ? deck.Name : "", deckCount > 0 ? deckCount : 40);
            yield return null;
            SceneManager.LoadScene("Duel");
        }

    }
}
