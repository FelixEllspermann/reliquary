using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Deck Builder im Collection-Design: Pool als Kartenbild-Gitter mit Suche,
    /// Typ-/Attribut-Filtern, Sortier-Dropdown und Besitz-Filter (ALL/OWNED/MISSING),
    /// Deck-Liste mit Hero-Chips und Legalitäts-Bar, Detail-Rail mit Kartenvorschau,
    /// Dust/Craft, abgeleiteter Deck-Balance und Advice-Strip. Server-Fluss wie gehabt
    /// (save_deck/delete_deck/craft/dust über den NetworkManager).
    ///
    /// Pool UND Deck-Liste zeigen je Karte eine Kachel pro besessenem Finish
    /// (CollectionCardTile, recycelt statt neu gebaut). Die Deck-Seite besteht
    /// aus zwei Gittern (Hauptdeck, Extra Deck) mit dem alten Trenner dazwischen;
    /// Kacheln lassen sich hinein- und herausziehen.
    /// </summary>
    public class DeckBuilderController : MonoBehaviour
    {
        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;
        [SerializeField] private GameRules rules;
        [SerializeField] private CardSkin skin;
        [SerializeField] private CollectionRow rowPrefab;

        [Header("Top-Bar")]
        [SerializeField] private TMP_Text[] dustTexts = new TMP_Text[4]; // C U R L
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button statsTabButton;
        [SerializeField] private Button menuButton;

        [Header("Pool")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Text poolCountText;
        [SerializeField] private Button[] typeChipButtons = new Button[4];   // ALL MONSTER SPELL ARTIFACT (+ RELIQUARY zur Laufzeit)
        [SerializeField] private Image[] typeChipBgs = new Image[4];
        [SerializeField] private TMP_Text[] typeChipLabels = new TMP_Text[4];
        [SerializeField] private Button[] attrChipButtons = new Button[7];   // ALL FI WA LI DA EA WI
        [SerializeField] private Image[] attrChipBgs = new Image[7];
        [SerializeField] private TMP_Text[] attrChipLabels = new TMP_Text[7];
        [SerializeField] private Transform poolContent;

        [Header("Deck")]
        [SerializeField] private TMP_Dropdown deckDropdown;
        [SerializeField] private TMP_InputField deckNameInput;
        [SerializeField] private Button newDeckButton;
        [SerializeField] private Button deleteDeckButton;
        [SerializeField] private Transform heroChipContainer;
        [SerializeField] private Image countFill;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Transform deckContent;
        [SerializeField] private GameObject emptyState;

        [Header("Detail-Rail")]
        [SerializeField] private TcgCardView previewView;
        [SerializeField] private GameObject previewEmpty;
        [SerializeField] private TMP_Text cardTextBody;      // Hover-Textbox: voller Kartentext
        [SerializeField] private ScrollRect cardTextScroll;  // scrollt bei langen Texten nach oben zurück
        [SerializeField] private Button dustButton;
        [SerializeField] private TMP_Text dustLabel;
        [SerializeField] private Button craftButton;
        [SerializeField] private TMP_Text craftLabel;
        [SerializeField] private Image[] levelFills = new Image[3];
        [SerializeField] private TMP_Text[] levelCounts = new TMP_Text[3];
        [SerializeField] private TMP_Text[] typeSplitCounts = new TMP_Text[3]; // Monster Spell Artifact
        [SerializeField] private Transform attributeBar;
        [SerializeField] private TMP_Text adviceText;
        [SerializeField] private Button saveButton;
        [SerializeField] private TMP_Text saveLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Szenen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string shopSceneName = "Shop";
        [SerializeField] private string statsSceneName = "Statistics";

        private static readonly MonsterAttribute[] AttrOrder =
        {
            MonsterAttribute.Fire, MonsterAttribute.Water, MonsterAttribute.Light,
            MonsterAttribute.Dark, MonsterAttribute.Earth, MonsterAttribute.Wind
        };

        private NetworkManager network;
        private List<CardDefinition> pool = new List<CardDefinition>();
        private List<PlayerCardData> heroes = new List<PlayerCardData>();
        private List<PlayerCardData> ownedHeroes = new List<PlayerCardData>();
        private readonly List<CollectionCardTile> poolTiles = new List<CollectionCardTile>();
        private readonly List<CollectionCardTile> deckTiles = new List<CollectionCardTile>();
        private readonly List<Button> heroChips = new List<Button>();
        private GameObject extraHeader;  // Laufzeit-Trenner "EXTRA DECK x/20"
        private RectTransform deckMainGrid;   // Kachel-Gitter Hauptdeck
        private RectTransform deckExtraGrid;  // Kachel-Gitter Extra Deck

        private int typeFilter;   // 0 all, 1 monster, 2 spell, 3 artifact, 4 reliquary
        private int attrFilter;   // 0 all, 1..6 = AttrOrder
        private string search = "";

        // ---- Sortierung + Besitz-Filter der Pool-Werkzeugleiste ----
        private int sortMode;             // Index in SortOptionNames
        private bool sortAscending = true;
        private int ownedFilter;          // 0 alle, 1 nur besessene, 2 nur fehlende, 3 nur neue
        private int archetypeFilter;      // 0 alle, 1 nur Generics, sonst 2+Index in ArchetypeCatalog.Names
        private TMP_Dropdown archetypeDropdown;
        private TMP_Dropdown sortDropdown;
        private Image sortDirBg;
        private TMP_Text sortDirLabel;
        private readonly GameObject[] ownedChips = new GameObject[4];
        private readonly Image[] ownedChipBgs = new Image[4];
        private readonly TMP_Text[] ownedChipLabels = new TMP_Text[4];

        private const string SortModePref = "deckpool_sort";
        private const string SortDirPref = "deckpool_dir";

        /// <summary>Das Deck-Panel als Drop-Ziel für gezogene Pool-Kacheln (und Grenze fürs Herausziehen).</summary>
        private RectTransform deckDropArea;
        private CardDefinition selected;

        /// <summary>Welche Ausführung der gewählten Karte rechts steht.</summary>
        private CardFinish selectedFinish = CardFinish.Plain;
        private RectTransform finishStrip;
        private readonly List<FinishChip> finishChips = new List<FinishChip>();

        private bool savedState;      // SAVE DECK ↔ SAVED ✓

        // Das Deck mit ungespeicherten Aenderungen. Jede Server-Nachricht mit
        // Profil ERSETZT PlayerProfile.Decks durch frische Objekte — wer gerade
        // baut, verlor dabei alles seit dem letzten Save (ein Craft genuegte).
        // Die Arbeitskopie wird nach dem Apply zurueckgelegt.
        private RuntimeDeck editedDeck;
        private bool awaitingSave;
        private bool restoredDeckIndex;   // Pref nur beim ersten Befüllen anwenden

        private bool CollectionMode => PlayerProfile.LoggedIn && network != null && network.IsConnected;
        private int MaxCopies => rules != null ? rules.maxCopiesPerCard : 3;

        /// <summary>Erlaubte Kopien laut Banlist — sonst das normale Kopienlimit.</summary>
        private int AllowedCopies(CardDefinition card) =>
            card == null ? MaxCopies : PlayerProfile.AllowedCopies(card.cardName, MaxCopies);

        /// <summary>Banlist-Limit der Karte, oder -1 wenn sie nicht gelistet ist.</summary>
        private int BanLimitOf(CardDefinition card) =>
            card != null && PlayerProfile.IsRestricted(card.cardName)
                ? PlayerProfile.AllowedCopies(card.cardName, MaxCopies)
                : -1;
        private int DeckMin => rules != null ? rules.deckMinSize : 40;
        private int DeckMax => rules != null ? rules.deckMaxSize : 80;
        private int CurrentIndex => deckDropdown != null ? deckDropdown.value : 0;

        private RuntimeDeck CurrentDeck
        {
            get
            {
                if (PlayerProfile.Decks.Count == 0) return null;
                return PlayerProfile.Decks[Mathf.Clamp(CurrentIndex, 0, PlayerProfile.Decks.Count - 1)];
            }
        }

        private void Start()
        {
            network = NetworkManager.Instance;
            if (catalog != null)
            {
                pool = catalog.cards.Where(c => c != null && !(c is PlayerCardData) && !c.isToken).ToList();
                heroes = catalog.cards.OfType<PlayerCardData>().ToList();
            }

            if (searchInput != null) searchInput.onValueChanged.AddListener(value => { search = value ?? ""; RebuildPool(); });

            // Die Listen-ScrollRects stehen in der Szene auf Empfindlichkeit 1 —
            // ein Radklick bewegte kaum eine Zeile. Hier hochgedreht statt in der
            // Szene, damit es für Pool UND Deck gilt und im Diff sichtbar ist.
            foreach (var content in new[] { poolContent, deckContent })
            {
                var listScroll = content != null ? content.GetComponentInParent<ScrollRect>(true) : null;
                if (listScroll != null) listScroll.scrollSensitivity = 45f;
            }

            // Zuletzt gewählte Sortierung wiederherstellen (der Besitz-Filter
            // startet bewusst auf ALL — ein vergessener MISSING-Filter sähe
            // sonst nach verschwundener Sammlung aus).
            sortMode = Mathf.Clamp(PlayerPrefs.GetInt(SortModePref, 0), 0, SortOptionNames.Length - 1);
            sortAscending = PlayerPrefs.GetInt(SortDirPref, SortDefaultAscending[sortMode] ? 1 : 0) == 1;
            BuildPoolToolbar();
            ConvertPoolToGrid();
            ResolveDeckDropArea();
            ConvertDeckToGrids();

            BuildReliquaryChip();
            for (int i = 0; i < typeChipButtons.Length; i++)
            {
                int index = i;
                if (typeChipButtons[i] != null) typeChipButtons[i].onClick.AddListener(() => { typeFilter = index; RefreshChips(); RebuildPool(); });
            }
            for (int i = 0; i < attrChipButtons.Length; i++)
            {
                int index = i;
                if (attrChipButtons[i] != null) attrChipButtons[i].onClick.AddListener(() => { attrFilter = index; RefreshChips(); RebuildPool(); });
            }

            if (deckDropdown != null) deckDropdown.onValueChanged.AddListener(_ => OnDeckSelected());
            if (deckNameInput != null) deckNameInput.onEndEdit.AddListener(RenameDeck);
            if (newDeckButton != null) newDeckButton.onClick.AddListener(CreateDeck);
            if (deleteDeckButton != null) deleteDeckButton.onClick.AddListener(DeleteDeck);
            if (saveButton != null) saveButton.onClick.AddListener(SaveCurrentDeck);
            if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (shopTabButton != null) shopTabButton.onClick.AddListener(() => SceneManager.LoadScene(shopSceneName));
            if (statsTabButton != null) statsTabButton.onClick.AddListener(() => SceneManager.LoadScene(statsSceneName));
            if (dustButton != null) dustButton.onClick.AddListener(DustSelected);
            if (craftButton != null) craftButton.onClick.AddListener(CraftSelected);
            if (network != null) network.OnMessage += HandleNet;

            BuildHeroChips();
            RefreshChips();
            RefreshDeckDropdown();
            RebuildAll();
            if (pool.Count > 0) Select(pool[0], CardFinish.Plain);
        }

        private void OnDestroy()
        {
            if (network != null) network.OnMessage -= HandleNet;
        }

        private void HandleNet(NetMessage message)
        {
            switch (message.t)
            {
                case "profile":
                case "auth_ok":
                case "craft_result":
                case "pack_result":
                    bool justSaved = awaitingSave;
                    if (justSaved) { awaitingSave = false; savedState = true; editedDeck = null; }
                    // Craft/Pack aendern die SAMMLUNG, nicht das Deck — der Server
                    // schickt aber das ganze Profil, und Apply hat die Deck-Liste
                    // frisch ersetzt. Die ungespeicherte Arbeitskopie kommt zurueck
                    // an ihren Platz, sonst waere sie mit jedem Craft weg.
                    if (!justSaved && editedDeck != null
                        && CurrentIndex >= 0 && CurrentIndex < PlayerProfile.Decks.Count)
                        PlayerProfile.Decks[CurrentIndex] = editedDeck;
                    RefreshDeckDropdown();
                    RebuildAll();
                    break;
                case "error":
                    awaitingSave = false;
                    ShowFeedback(message.msg);
                    break;
            }
        }

        // ---------- Filter ----------
        private static readonly string[] TypeChipNames = { "ALL", "MONSTER", "SPELL", "ARTIFACT", "RELIQUARY" };
        private static readonly string[] AttrChipNames = { "ALL", "FI", "WA", "LI", "DA", "EA", "WI" };

        /// <summary>
        /// Fünfter Typ-Filter für das Extra Deck. Wird als Kopie des ARTIFACT-Chips
        /// erzeugt, damit er ohne weiteres Verdrahten exakt im Stil der anderen sitzt.
        /// </summary>
        private void BuildReliquaryChip()
        {
            if (typeChipButtons == null || typeChipButtons.Length < 4) return;
            if (typeChipButtons.Length >= 5 && typeChipButtons[4] != null) return;
            var template = typeChipButtons[3];
            if (template == null || template.transform.parent == null) return;

            var copy = Instantiate(template.gameObject, template.transform.parent);
            copy.name = "ChipReliquary";
            copy.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);

            // Ohne Layout-Gruppe sitzt die Kopie auf dem Original — dann eine Chip-Breite weiterrücken
            if (template.transform.parent.GetComponent<LayoutGroup>() == null && typeChipButtons[2] != null)
            {
                var previous = (RectTransform)typeChipButtons[2].transform;
                var last = (RectTransform)template.transform;
                ((RectTransform)copy.transform).anchoredPosition =
                    last.anchoredPosition + (last.anchoredPosition - previous.anchoredPosition);
            }

            System.Array.Resize(ref typeChipButtons, 5);
            System.Array.Resize(ref typeChipBgs, 5);
            System.Array.Resize(ref typeChipLabels, 5);
            typeChipButtons[4] = copy.GetComponent<Button>();
            typeChipBgs[4] = FindSame(template.gameObject, copy, typeChipBgs.Length > 3 ? typeChipBgs[3] : null);
            typeChipLabels[4] = FindSame(template.gameObject, copy, typeChipLabels.Length > 3 ? typeChipLabels[3] : null);
            if (typeChipLabels[4] != null) typeChipLabels[4].text = TypeChipNames[4];
        }

        /// <summary>Findet in einer Kopie das Gegenstück zu einer Komponente der Vorlage.</summary>
        private static T FindSame<T>(GameObject template, GameObject copy, T original) where T : Component
        {
            if (original == null) return null;
            var parts = new List<string>();
            var node = original.transform;
            while (node != null && node != template.transform) { parts.Add(node.name); node = node.parent; }
            if (node != template.transform) return null;
            parts.Reverse();
            var target = parts.Count == 0 ? copy.transform : copy.transform.Find(string.Join("/", parts));
            return target != null ? target.GetComponent<T>() : null;
        }

        private void RefreshChips()
        {
            for (int i = 0; i < typeChipBgs.Length; i++)
                StyleChip(typeChipBgs[i], typeChipLabels[i], i == typeFilter, new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f));
            for (int i = 0; i < attrChipBgs.Length; i++)
            {
                Color accent = i == 0 ? new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f)
                    : MonsterCardData.AttributeColor(AttrOrder[i - 1]);
                StyleChip(attrChipBgs[i], attrChipLabels[i], i == attrFilter, accent);
            }
        }

        private static void StyleChip(Image bg, TMP_Text label, bool active, Color accent)
        {
            if (bg != null)
            {
                bg.color = active ? new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.2f) : new Color(0f, 0f, 0f, 0.4f);
                var frameChild = bg.transform.Find("Frame");
                if (frameChild != null)
                {
                    var frameImage = frameChild.GetComponent<Image>();
                    if (frameImage != null)
                        frameImage.color = active ? accent : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.2f);
                }
            }
            if (label != null)
            {
                ColorUtility.TryParseHtmlString(active ? "#F1DFB8" : "#7E7059", out var ink);
                label.color = ink;
                label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // ---------- Pool-Werkzeugleiste (Sortierung + Besitz-Filter) ----------

        private static readonly string[] SortOptionNames = { "TYPE", "NAME", "LEVEL", "ATK", "DEF", "RARITY", "OWNED" };

        /// <summary>Natürliche Richtung je Sortierung — Stats und Rarity wollen absteigend starten.</summary>
        private static readonly bool[] SortDefaultAscending = { true, true, false, false, false, false, false };

        /// <summary>
        /// Der Pool zeigt Kartenbilder statt Zeilen: die alte VerticalLayoutGroup
        /// des Contents weicht einem Gitter. Zur Laufzeit statt in der Szene,
        /// damit der Umbau im Diff sichtbar ist — wie schon die Scroll-Empfindlichkeit.
        /// </summary>
        private void ConvertPoolToGrid()
        {
            if (poolContent == null) return;
            var vertical = poolContent.GetComponent<VerticalLayoutGroup>();
            if (vertical != null) DestroyImmediate(vertical);
            var grid = poolContent.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = poolContent.gameObject.AddComponent<GridLayoutGroup>();
            // 4 Spalten: 4×134 + 3×8 = 560 = Viewport 588 minus Polster 12+16
            grid.cellSize = new Vector2(CollectionCardTile.Width, CollectionCardTile.Height);
            grid.spacing = new Vector2(8f, 10f);
            grid.padding = new RectOffset(12, 16, 10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// Baut die Leiste zwischen Pool-Header und Liste: Sortier-Dropdown
        /// (Klon des Deck-Dropdowns), Richtungs-Chip und die Besitz-Chips
        /// ALL/OWNED/MISSING. Die Liste rückt dafür um die Leistenhöhe nach unten —
        /// im Header selbst ist kein Platz mehr.
        /// </summary>
        private void BuildPoolToolbar()
        {
            if (poolContent == null || deckDropdown == null) return;
            var viewportRect = poolContent.parent as RectTransform;
            var scrollRect = viewportRect != null ? viewportRect.parent as RectTransform : null;
            var panelRect = scrollRect != null ? scrollRect.parent as RectTransform : null;
            if (panelRect == null) return;

            // Zwei Zeilen: oben Sortierung + Besitz, darunter der Archetype-Filter
            const float toolbarHeight = 44f;
            const float archRowHeight = 40f;
            float listTop = scrollRect.offsetMax.y;
            scrollRect.offsetMax = new Vector2(scrollRect.offsetMax.x, listTop - toolbarHeight - archRowHeight);

            var row = new GameObject("PoolToolbar", typeof(RectTransform));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(panelRect, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.offsetMin = new Vector2(14f, listTop - toolbarHeight + 4f);
            rowRect.offsetMax = new Vector2(-14f, listTop - 2f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Sortier-Dropdown — Klon des Deck-Dropdowns, damit der Stil exakt passt
            sortDropdown = Instantiate(deckDropdown, rowRect);
            sortDropdown.name = "SortDropdown";
            var dropdownLayout = sortDropdown.GetComponent<LayoutElement>();
            if (dropdownLayout == null) dropdownLayout = sortDropdown.gameObject.AddComponent<LayoutElement>();
            dropdownLayout.preferredWidth = 168f;
            dropdownLayout.flexibleWidth = 0f;
            sortDropdown.onValueChanged.RemoveAllListeners();
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string>(SortOptionNames));
            sortDropdown.SetValueWithoutNotify(sortMode);
            sortDropdown.onValueChanged.AddListener(OnSortModeChanged);

            var dirChip = CloneToolbarChip("SortDirChip", "DESC", 64f, rowRect, out sortDirBg, out sortDirLabel);
            if (dirChip != null)
                dirChip.GetComponent<Button>().onClick.AddListener(() =>
                {
                    sortAscending = !sortAscending;
                    SaveSortPrefs();
                    RefreshPoolToolbar();
                    RebuildPool();
                });

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(rowRect, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;

            string[] ownedNames = { "ALL", "OWNED", "MISSING", "NEW" };
            for (int i = 0; i < ownedNames.Length; i++)
            {
                int index = i;
                var chip = CloneToolbarChip("OwnedChip" + ownedNames[i], ownedNames[i],
                    i == 2 ? 84f : i == 3 ? 60f : 74f, rowRect, out ownedChipBgs[i], out ownedChipLabels[i]);
                ownedChips[i] = chip;
                if (chip != null)
                    chip.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        ownedFilter = index;
                        RefreshPoolToolbar();
                        RebuildPool();
                    });
            }

            // Zweite Zeile: der Archetype-Filter — alle 29 Familien plus ALL
            var archRow = new GameObject("ArchetypeRow", typeof(RectTransform));
            var archRect = (RectTransform)archRow.transform;
            archRect.SetParent(panelRect, false);
            archRect.anchorMin = new Vector2(0f, 1f);
            archRect.anchorMax = new Vector2(1f, 1f);
            archRect.pivot = new Vector2(0.5f, 1f);
            archRect.offsetMin = new Vector2(14f, listTop - toolbarHeight - archRowHeight + 4f);
            archRect.offsetMax = new Vector2(-14f, listTop - toolbarHeight);
            var archLayout = archRow.AddComponent<HorizontalLayoutGroup>();
            archLayout.spacing = 6f;
            archLayout.childControlWidth = true;
            archLayout.childControlHeight = true;
            archLayout.childForceExpandWidth = false;
            archLayout.childForceExpandHeight = true;

            archetypeDropdown = Instantiate(deckDropdown, archRect);
            archetypeDropdown.name = "ArchetypeDropdown";
            var archDropLayout = archetypeDropdown.GetComponent<LayoutElement>();
            if (archDropLayout == null) archDropLayout = archetypeDropdown.gameObject.AddComponent<LayoutElement>();
            archDropLayout.preferredWidth = 236f;
            archDropLayout.flexibleWidth = 0f;
            archetypeDropdown.onValueChanged.RemoveAllListeners();
            archetypeDropdown.ClearOptions();
            // "NO ARCHETYPE" steht gleich hinter "ALL": die Generics sind eine
            // eigene Gruppe, keine Restmenge irgendwo unten in der Liste.
            var archOptions = new List<string> { "ALL ARCHETYPES", "NO ARCHETYPE" };
            archOptions.AddRange(ArchetypeCatalog.Names.Select(n => n.ToUpperInvariant()));
            archetypeDropdown.AddOptions(archOptions);
            archetypeDropdown.SetValueWithoutNotify(archetypeFilter);
            archetypeDropdown.onValueChanged.AddListener(value =>
            {
                archetypeFilter = value;
                RebuildPool();
            });

            RefreshPoolToolbar();
        }

        /// <summary>
        /// Löst das Deck-Panel als Drop-Ziel auf (deckContent → Viewport →
        /// ScrollView → Panel) und ergänzt den Leerzustands-Hinweis ums Ziehen.
        /// </summary>
        private void ResolveDeckDropArea()
        {
            if (deckContent == null) return;
            var viewport = deckContent.parent as RectTransform;
            var scroll = viewport != null ? viewport.parent as RectTransform : null;
            var panel = scroll != null ? scroll.parent as RectTransform : null;
            deckDropArea = panel != null ? panel : deckContent as RectTransform;

            if (emptyState != null)
            {
                foreach (var text in emptyState.GetComponentsInChildren<TMP_Text>(true))
                    if (text.text.Contains("double-click"))
                    {
                        text.text = "Click a card to inspect it — double-click, drag it over, or use + to seal it into this deck.";
                        break;
                    }
            }
        }

        /// <summary>Klont einen Typ-Chip als Werkzeugleisten-Knopf (Stil bleibt, Verhalten neu).</summary>
        private GameObject CloneToolbarChip(string name, string text, float width, RectTransform parent,
            out Image bg, out TMP_Text label)
        {
            bg = null;
            label = null;
            var template = typeChipButtons != null && typeChipButtons.Length > 0 ? typeChipButtons[0] : null;
            if (template == null) return null;

            var copy = Instantiate(template.gameObject, parent);
            copy.name = name;
            var layoutElement = copy.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = copy.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.flexibleWidth = 0f;
            bg = copy.GetComponent<Image>();
            label = copy.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
            }
            var button = copy.GetComponent<Button>();
            if (button != null) button.onClick.RemoveAllListeners();
            return copy;
        }

        private void OnSortModeChanged(int mode)
        {
            sortMode = Mathf.Clamp(mode, 0, SortOptionNames.Length - 1);
            // Jede Sortierung startet in ihrer natürlichen Richtung
            sortAscending = SortDefaultAscending[sortMode];
            SaveSortPrefs();
            RefreshPoolToolbar();
            RebuildPool();
        }

        private void SaveSortPrefs()
        {
            PlayerPrefs.SetInt(SortModePref, sortMode);
            PlayerPrefs.SetInt(SortDirPref, sortAscending ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void RefreshPoolToolbar()
        {
            var gold = new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f);
            if (sortDirLabel != null)
            {
                sortDirLabel.text = sortAscending ? "ASC" : "DESC";
                StyleChip(sortDirBg, sortDirLabel, true, gold);
            }
            // Ohne Sammlung (offline/Sandbox) gibt es keinen Besitz zu filtern
            if (!CollectionMode && ownedFilter != 0) ownedFilter = 0;
            for (int i = 0; i < ownedChips.Length; i++)
            {
                if (ownedChips[i] == null) continue;
                ownedChips[i].SetActive(CollectionMode);
                StyleChip(ownedChipBgs[i], ownedChipLabels[i], ownedFilter == i, gold);
            }
        }

        /// <summary>Besitz über alle Finishes hinweg — für Sortierung und Zähler.</summary>
        private int TotalOwned(CardDefinition card) =>
            CollectionMode ? PlayerProfile.Owned(card.cardName) : 0;

        /// <summary>
        /// Sortiert den gefilterten Pool nach der gewählten Ordnung. Bei Stat-
        /// Sortierungen bleiben Nicht-Monster hinten — ein Zauber hat kein ATK,
        /// egal in welche Richtung man dreht. Namensgleichstand entscheidet
        /// immer alphabetisch.
        /// </summary>
        private List<CardDefinition> SortedPool(List<CardDefinition> cards)
        {
            int dir = sortAscending ? 1 : -1;
            System.Func<CardDefinition, int> stat = null;
            IOrderedEnumerable<CardDefinition> ordered;
            switch (sortMode)
            {
                case 1: // NAME — der Name ist hier der Erstschlüssel
                    return (sortAscending
                        ? cards.OrderBy(c => c.cardName, System.StringComparer.OrdinalIgnoreCase)
                        : cards.OrderByDescending(c => c.cardName, System.StringComparer.OrdinalIgnoreCase)).ToList();
                case 2: stat = c => c is MonsterCardData m ? m.level : 0; goto default;
                case 3: stat = c => c is MonsterCardData m ? m.atk : 0; goto default;
                case 4: stat = c => c is MonsterCardData m ? m.def : 0; goto default;
                case 5: // RARITY
                    ordered = cards.OrderBy(c => dir * (int)c.rarity);
                    break;
                case 6: // OWNED — bei Gleichstand die wertvollere Karte zuerst
                    ordered = cards.OrderBy(c => dir * TotalOwned(c)).ThenByDescending(c => (int)c.rarity);
                    break;
                default:
                    ordered = stat != null
                        ? cards.OrderBy(c => c is MonsterCardData ? 0 : 1).ThenBy(c => dir * stat(c))
                        : cards.OrderBy(c => dir * (int)c.Kind) // TYPE — die bisherige Ordnung
                            .ThenBy(c => dir * (c is MonsterCardData m ? m.level : 0));
                    break;
            }
            return ordered.ThenBy(c => c.cardName, System.StringComparer.OrdinalIgnoreCase).ToList();
        }

        private bool PassesFilter(CardDefinition card)
        {
            // MONSTER meint das Hauptdeck — Reliquarys haben ihren eigenen Filter
            if (typeFilter == 1 && (!(card is MonsterCardData) || card is ReliquaryCardData)) return false;
            if (typeFilter == 2 && !(card is SpellCardData)) return false;
            if (typeFilter == 3 && !(card is ArtifactCardData)) return false;
            if (typeFilter == 4 && !(card is ReliquaryCardData)) return false;
            if (ownedFilter > 0 && CollectionMode)
            {
                // Besitz zählt über alle Finishes — wer nur die Glossy hat, "besitzt" die Karte
                bool has = PlayerProfile.Owned(card.cardName) > 0;
                if (ownedFilter == 1 && !has) return false;
                if (ownedFilter == 2 && has) return false;
                if (ownedFilter == 3 && !PlayerProfile.IsNew(card.cardName)) return false;
            }
            if (attrFilter > 0)
            {
                var monster = card as MonsterCardData;
                if (monster == null || monster.attribute != AttrOrder[attrFilter - 1]) return false;
            }
            if (archetypeFilter == 1)
            {
                // Generics: alles, was zu keinem der kuratierten Archetypes gehört
                if (ArchetypeCatalog.Of(card.cardName) != null) return false;
            }
            // Über den Katalog, nicht über das Präfix: sonst fehlen die Karten,
            // deren Familie nur namentlich geführt ist ("King of Deckay").
            else if (archetypeFilter > 1 && archetypeFilter - 1 <= ArchetypeCatalog.Names.Length
                && ArchetypeCatalog.Of(card.cardName) != ArchetypeCatalog.Names[archetypeFilter - 2])
                return false;
            if (!string.IsNullOrWhiteSpace(search))
            {
                string query = search.Trim().ToLowerInvariant();
                string haystack = card.cardName.ToLowerInvariant();
                if (card is MonsterCardData m)
                {
                    haystack += " " + m.attribute.ToString().ToLowerInvariant() + " " + m.monsterType.ToString().ToLowerInvariant() + " monster";
                    if (card is ReliquaryCardData) haystack += " reliquary extra";
                }
                else if (card is SpellCardData s)
                    haystack += s.speed == SpellSpeed.Quick ? " quick spell" : " spell";
                else if (card is ArtifactCardData) haystack += " artifact";
                if (!haystack.Contains(query)) return false;
            }
            return true;
        }

        // ---------- Deck-Verwaltung ----------
        private void RefreshDeckDropdown()
        {
            if (deckDropdown == null) return;
            int previous = deckDropdown.value;
            deckDropdown.ClearOptions();
            deckDropdown.AddOptions(CollectionMode && PlayerProfile.Decks.Count > 0
                ? PlayerProfile.Decks.ConvertAll(d => d.Name)
                : new List<string> { "—" });
            // Beim ersten echten Befüllen das zuletzt benutzte Deck wiederherstellen,
            // danach hält `previous` die Auswahl über Profil-Updates hinweg stabil.
            if (!restoredDeckIndex && CollectionMode && PlayerProfile.Decks.Count > 0)
            {
                restoredDeckIndex = true;
                previous = PlayerPrefs.GetInt(MainMenuController.ActiveDeckPrefKey, previous);
            }
            deckDropdown.SetValueWithoutNotify(Mathf.Clamp(previous, 0, Mathf.Max(0, deckDropdown.options.Count - 1)));

            bool online = CollectionMode;
            if (saveButton != null) saveButton.interactable = online;
            if (newDeckButton != null) newDeckButton.interactable = online;
            if (deleteDeckButton != null) deleteDeckButton.interactable = online && PlayerProfile.Decks.Count > 1;
            if (deckNameInput != null)
            {
                deckNameInput.interactable = online;
                deckNameInput.SetTextWithoutNotify(CurrentDeck != null ? CurrentDeck.Name : "");
            }
        }

        private void OnDeckSelected()
        {
            editedDeck = null;   // Wechsel verwirft die Arbeitskopie — wie bisher
            PlayerPrefs.SetInt(MainMenuController.ActiveDeckPrefKey, CurrentIndex);
            PlayerPrefs.Save();

            savedState = false;
            if (deckNameInput != null) deckNameInput.SetTextWithoutNotify(CurrentDeck != null ? CurrentDeck.Name : "");
            RebuildAll();
        }

        private void RenameDeck(string newName)
        {
            var deck = CurrentDeck;
            if (deck == null || !CollectionMode) return;
            string trimmed = (newName ?? "").Trim();
            if (trimmed.Length < 1 || trimmed.Length > 30)
            {
                ShowFeedback("Deck name must be 1–30 characters.");
                if (deckNameInput != null) deckNameInput.SetTextWithoutNotify(deck.Name);
                return;
            }
            if (trimmed == deck.Name) return;
            deck.Name = trimmed;
            MarkEdited();
            RefreshDeckDropdown();
        }

        private void CreateDeck()
        {
            if (!CollectionMode) return;
            var fresh = new RuntimeDeck { Name = "Deck " + (PlayerProfile.Decks.Count + 1), Hero = ownedHeroes.Count > 0 ? ownedHeroes[0].cardName : (heroes.Count > 0 ? heroes[0].cardName : "") };
            network.SendSaveDeck(PlayerProfile.Decks.Count, fresh);
            ShowFeedback($"Creating {fresh.Name}…");
        }

        private void DeleteDeck()
        {
            if (!CollectionMode || PlayerProfile.Decks.Count <= 1) return;
            network.SendDeleteDeck(CurrentIndex);
            ShowFeedback("Deleting deck…");
        }

        /// <summary>
        /// Mit zwölf Helden im Spiel taugt die Chip-Reihe nicht mehr: winzige
        /// Vornamen, kein Effekttext, und Unbekanntes bleibt unsichtbar. Der
        /// erste Chip wird zum KNOPF, der den Helden-Wähler öffnet — ein Overlay
        /// mit allen Helden als echte Karten. Besessene sind wählbar, fehlende
        /// ausgegraut mit Hero-Cache-Hinweis: man sieht, was es zu holen gibt.
        /// </summary>
        private void BuildHeroChips()
        {
            heroChips.Clear();
            if (heroChipContainer == null) return;

            var buttons = new List<Button>();
            foreach (Transform child in heroChipContainer)
            {
                var button = child.GetComponent<Button>();
                if (button != null) buttons.Add(button);
            }
            if (buttons.Count == 0) return;

            ownedHeroes = heroes.Where(h => PlayerProfile.Owned(h.cardName) > 0).ToList();
            if (ownedHeroes.Count == 0) ownedHeroes = new List<PlayerCardData>(heroes);

            for (int i = 1; i < buttons.Count; i++) buttons[i].gameObject.SetActive(false);
            var heroButton = buttons[0];
            heroButton.gameObject.SetActive(true);
            heroChips.Add(heroButton);
            heroButton.onClick.RemoveAllListeners();
            heroButton.onClick.AddListener(OpenHeroPicker);
            RefreshHeroChips();
        }

        private void SetHero(PlayerCardData hero)
        {
            var deck = CurrentDeck;
            if (deck == null || hero == null) return;
            deck.Hero = hero.cardName;
            MarkEdited();
            RefreshHeroChips();
            Select(hero, CardFinish.Plain);
        }

        private void RefreshHeroChips()
        {
            if (heroChips.Count == 0) return;
            var deck = CurrentDeck;
            var bg = heroChips[0].GetComponent<Image>();
            var label = heroChips[0].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = deck != null && !string.IsNullOrEmpty(deck.Hero)
                    ? "HERO · " + deck.Hero.ToUpperInvariant()
                    : "CHOOSE YOUR HERO";
                label.enableAutoSizing = true;
                label.fontSizeMin = 9f;
            }
            StyleChip(bg, label, true, new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f));
        }

        // ---------- Helden-Wähler ----------

        private GameObject heroPicker;
        private RectTransform heroPickerGrid;

        private void OpenHeroPicker()
        {
            EnsureHeroPicker();
            if (heroPicker == null) return;

            // Besitz kann sich seit dem Szenenstart geändert haben (Hero Cache)
            ownedHeroes = heroes.Where(h => PlayerProfile.Owned(h.cardName) > 0).ToList();
            if (ownedHeroes.Count == 0) ownedHeroes = new List<PlayerCardData>(heroes);

            for (int i = heroPickerGrid.childCount - 1; i >= 0; i--)
                Destroy(heroPickerGrid.GetChild(i).gameObject);

            var deck = CurrentDeck;
            // Besessene zuerst, dann der Rest als Schaufenster
            var sorted = heroes.OrderByDescending(h => PlayerProfile.Owned(h.cardName) > 0)
                               .ThenBy(h => h.cardName, System.StringComparer.Ordinal);
            foreach (var hero in sorted)
            {
                bool owned = ownedHeroes.Contains(hero);
                bool current = deck != null && deck.Hero == hero.cardName;

                var cell = new GameObject("Hero_" + hero.cardName, typeof(RectTransform)).GetComponent<RectTransform>();
                cell.SetParent(heroPickerGrid, false);

                var view = Instantiate(previewView, cell);
                view.gameObject.SetActive(true);
                var viewRect = (RectTransform)view.transform;
                // FESTE Groesse statt Stretch: beim Show() ist die Gitterzelle
                // noch 0 breit (das Layout laeuft erst danach), und TcgCardView
                // wuerde bei Breite < 200 in den Kompaktmodus springen.
                viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
                viewRect.pivot = new Vector2(0.5f, 0.5f);
                viewRect.sizeDelta = new Vector2(320f, 448f);
                viewRect.anchoredPosition = Vector2.zero;
                viewRect.localScale = Vector3.one;
                view.Show(new CardInstance(hero, null), false, true);

                var group = cell.gameObject.AddComponent<CanvasGroup>();
                group.alpha = owned ? 1f : 0.4f;

                // Rahmen um den aktuell gewählten Helden
                if (current)
                {
                    var mark = new GameObject("Current", typeof(RectTransform)).GetComponent<RectTransform>();
                    mark.SetParent(cell, false);
                    mark.anchorMin = Vector2.zero; mark.anchorMax = Vector2.one;
                    mark.offsetMin = new Vector2(-5f, -5f); mark.offsetMax = new Vector2(5f, 5f);
                    var outline = mark.gameObject.AddComponent<Image>();
                    outline.color = new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.9f);
                    if (skin != null && skin.whiteFrame != null) { outline.sprite = skin.whiteFrame; outline.type = Image.Type.Sliced; }
                    outline.raycastTarget = false;
                }

                if (!owned)
                {
                    var pill = new GameObject("CachePill", typeof(RectTransform)).GetComponent<RectTransform>();
                    pill.SetParent(cell, false);
                    pill.anchorMin = new Vector2(0.5f, 0f); pill.anchorMax = new Vector2(0.5f, 0f);
                    pill.pivot = new Vector2(0.5f, 0f);
                    pill.sizeDelta = new Vector2(180f, 34f);
                    pill.anchoredPosition = new Vector2(0f, 8f);
                    var pillBg = pill.gameObject.AddComponent<Image>();
                    pillBg.color = new Color(0f, 0f, 0f, 0.82f);
                    var pillTextGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
                    pillTextGo.SetParent(pill, false);
                    pillTextGo.anchorMin = Vector2.zero; pillTextGo.anchorMax = Vector2.one;
                    pillTextGo.offsetMin = Vector2.zero; pillTextGo.offsetMax = Vector2.zero;
                    var pillText = pillTextGo.gameObject.AddComponent<TextMeshProUGUI>();
                    pillText.text = "HERO CACHE · SHOP";
                    pillText.fontSize = 14f;
                    pillText.alignment = TextAlignmentOptions.Center;
                    pillText.color = new Color(1f, 194f / 255f, 77f / 255f, 1f);
                    pillText.raycastTarget = false;
                }

                var clickGo = new GameObject("Click", typeof(RectTransform)).GetComponent<RectTransform>();
                clickGo.SetParent(cell, false);
                clickGo.anchorMin = Vector2.zero; clickGo.anchorMax = Vector2.one;
                clickGo.offsetMin = Vector2.zero; clickGo.offsetMax = Vector2.zero;
                var clickImg = clickGo.gameObject.AddComponent<Image>();
                clickImg.color = Color.clear;
                var clickBtn = clickGo.gameObject.AddComponent<Button>();
                clickBtn.transition = Selectable.Transition.None;
                var chosen = hero;
                clickBtn.onClick.AddListener(() =>
                {
                    if (PlayerProfile.Owned(chosen.cardName) > 0)
                    {
                        SetHero(chosen);
                        CloseHeroPicker();
                    }
                    else
                    {
                        ShowFeedback("You do not own this hero — open a Hero Cache in the shop.");
                        Select(chosen, CardFinish.Plain);
                    }
                });
            }

            heroPicker.SetActive(true);
            heroPicker.transform.SetAsLastSibling();
        }

        private void CloseHeroPicker()
        {
            if (heroPicker != null) heroPicker.SetActive(false);
        }

        private void EnsureHeroPicker()
        {
            if (heroPicker != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null || previewView == null) return;
            // An die WURZEL, nicht an den naechstbesten Canvas — nur dort
            // liegt das Overlay wirklich ueber allen Panels der Szene.
            canvas = canvas.rootCanvas;

            heroPicker = new GameObject("HeroPickerOverlay", typeof(RectTransform));
            var overlayRect = (RectTransform)heroPicker.transform;
            overlayRect.SetParent(canvas.transform, false);
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero; overlayRect.offsetMax = Vector2.zero;

            // Eigener Canvas mit hoher Ordnung: Geschwister-Reihenfolge allein
            // ist zerbrechlich — ein spaeter erzeugtes Panel laege sonst drueber.
            var pickerCanvas = heroPicker.AddComponent<Canvas>();
            pickerCanvas.overrideSorting = true;
            pickerCanvas.sortingOrder = 300;
            heroPicker.AddComponent<GraphicRaycaster>();

            var scrim = new GameObject("Scrim", typeof(RectTransform)).GetComponent<RectTransform>();
            scrim.SetParent(overlayRect, false);
            scrim.anchorMin = Vector2.zero; scrim.anchorMax = Vector2.one;
            scrim.offsetMin = Vector2.zero; scrim.offsetMax = Vector2.zero;
            var scrimImg = scrim.gameObject.AddComponent<Image>();
            scrimImg.color = new Color(0f, 0f, 0f, 0.88f);
            var scrimBtn = scrim.gameObject.AddComponent<Button>();
            scrimBtn.transition = Selectable.Transition.None;
            scrimBtn.onClick.AddListener(CloseHeroPicker);

            var title = new GameObject("Title", typeof(RectTransform)).GetComponent<RectTransform>();
            title.SetParent(overlayRect, false);
            title.anchorMin = new Vector2(0f, 1f); title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.offsetMin = new Vector2(60f, -80f); title.offsetMax = new Vector2(-60f, -30f);
            var titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
            titleText.text = "CHOOSE YOUR HERO";
            titleText.fontSize = 30f;
            titleText.characterSpacing = 10f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(241f / 255f, 231f / 255f, 210f / 255f, 1f);
            titleText.raycastTarget = false;

            var scrollGo = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollGo.SetParent(overlayRect, false);
            scrollGo.anchorMin = new Vector2(0.5f, 0f); scrollGo.anchorMax = new Vector2(0.5f, 1f);
            scrollGo.pivot = new Vector2(0.5f, 0.5f);
            scrollGo.sizeDelta = new Vector2(1360f, 0f);
            scrollGo.offsetMin = new Vector2(-680f, 40f); scrollGo.offsetMax = new Vector2(680f, -100f);
            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
            viewport.SetParent(scrollGo, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            // Ohne Graphic im Viewport greift das Mausrad ins Leere
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = Color.clear;
            scroll.viewport = viewport;

            heroPickerGrid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
            heroPickerGrid.SetParent(viewport, false);
            heroPickerGrid.anchorMin = new Vector2(0f, 1f);
            heroPickerGrid.anchorMax = new Vector2(1f, 1f);
            heroPickerGrid.pivot = new Vector2(0.5f, 1f);
            heroPickerGrid.offsetMin = Vector2.zero; heroPickerGrid.offsetMax = Vector2.zero;
            var grid = heroPickerGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(320f, 448f);   // volle Karte, Effekttext lesbar
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = heroPickerGrid.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = heroPickerGrid;

            heroPicker.SetActive(false);
        }

        // ---------- Listen ----------
        private int ExtraMax => rules != null ? rules.extraDeckMaxSize : 20;

        /// <summary>Alle Exemplare dieser Karte im Deck, gleich welches Finish.</summary>
        private int CountInDeck(CardDefinition card)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return 0;
            var list = card is ReliquaryCardData ? deck.Extra : deck.Cards;
            int count = 0;
            foreach (var name in list) if (name == card.cardName) count++;
            return count;
        }

        /// <summary>Nur die Exemplare dieses einen Finishes.</summary>
        private int CountInDeck(CardDefinition card, CardFinish finish)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return 0;
            bool extra = card is ReliquaryCardData;
            var list = extra ? deck.Extra : deck.Cards;
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != card.cardName) continue;
                var slot = extra ? deck.ExtraFinishAt(i) : deck.FinishAt(i);
                if (slot == finish) count++;
            }
            return count;
        }

        /// <summary>
        /// Welche Exemplar-Typen einer Karte bekommen eine eigene Zeile? In der
        /// Sammlung alle, die man besitzt oder schon eingebaut hat — sonst nur die
        /// schlichte. Die schlichte Zeile steht immer da, auch mit null Stück:
        /// dort sitzen Craft und Dust.
        /// </summary>
        private List<CardFinish> FinishRowsFor(CardDefinition card)
        {
            var result = new List<CardFinish> { CardFinish.Plain };
            if (!CollectionMode) return result;

            var stock = PlayerProfile.StockOf(card.cardName);
            for (int i = 1; i < CardFinishInfo.Count; i++)
            {
                var finish = (CardFinish)i;
                if (stock[finish] > 0 || CountInDeck(card, finish) > 0) result.Add(finish);
            }
            return result;
        }

        /// <summary>Exemplare dieses Finishes, die der Spieler besitzt (ausserhalb der Sammlung unbegrenzt).</summary>
        private int OwnedOf(CardDefinition card, CardFinish finish) =>
            CollectionMode ? PlayerProfile.Owned(card.cardName, finish) : AllowedCopies(card);

        private void RebuildAll()
        {
            RefreshPoolToolbar();
            RebuildPool();
            RebuildDeck();
            RefreshHeroChips();
            RefreshWallet();
            RefreshBalance();
            RefreshSaveButton();
        }

        private Coroutine poolBuildRoutine;

        private void RebuildPool()
        {
            if (poolContent == null) return;

            // Bei ~750 Karten friert ein synchroner Aufbau den ersten Frame sichtbar
            // ein (jede Kachel zieht beim ersten Zugriff ihre Textur von der Platte).
            // Deshalb gestaffelt: die erste Ladung füllt den sichtbaren Bereich
            // sofort, der Rest streamt über die folgenden Frames nach.
            if (poolBuildRoutine != null) { StopCoroutine(poolBuildRoutine); poolBuildRoutine = null; }
            if (isActiveAndEnabled)
                poolBuildRoutine = StartCoroutine(RebuildPoolIncremental());
            else
                RebuildPoolNow();
        }

        private System.Collections.IEnumerator RebuildPoolIncremental()
        {
            var filtered = pool.Where(c => c != null && PassesFilter(c)).ToList();
            var sorted = SortedPool(filtered);

            if (poolCountText != null)
            {
                // In der Sammlung steht dazu, wie viele verschiedene Karten man schon hat
                string ownedInfo = CollectionMode
                    ? $" · <color=#F3DDA4>{pool.Count(c => c != null && PlayerProfile.Owned(c.cardName) > 0)}</color> owned"
                    : "";
                poolCountText.text = $"{filtered.Count} of {pool.Count} cards{ownedInfo}";
            }

            // Je Karte eine Kachel pro besessenem Finish — so lassen sich gezielt
            // die zwei Static einbauen statt der schlichten Exemplare. Kacheln
            // werden recycelt: bei ~750 Karten wäre Zerstören und Neubauen bei
            // jedem Tastendruck im Suchfeld unbezahlbar.
            //
            // Dosiert wird nach ZEIT, nicht nach Stückzahl: eine Kachel ist eine
            // volle Kartenansicht (~10 TMP-Texte, dazu ein Artwork, das beim
            // ersten Zugriff erst entpackt wird) — feste 48 pro Frame kosteten
            // je nach Kaltstart zweistellige Millisekunden und das Nachfüllen
            // ruckelte sichtbar. Jetzt nimmt sich jeder Frame nur, was ins
            // Budget passt (mindestens eine Kachel, sonst käme nichts voran).
            const double frameBudgetMs = 4.0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            int used = 0;
            foreach (var card in sorted)
                foreach (var finish in FinishRowsFor(card))
                {
                    var tile = used < poolTiles.Count ? poolTiles[used] : CreatePoolTile();
                    if (tile == null) break;
                    tile.gameObject.SetActive(true);
                    tile.Setup(card, finish, CountInDeck(card, finish), OwnedOf(card, finish),
                        AllowedCopies(card), CountInDeck(card),
                        AddCard, RemoveCard, Select, BanLimitOf(card), CollectionMode,
                        isNew: CollectionMode && PlayerProfile.IsNew(card.cardName));
                    used++;
                    if (clock.Elapsed.TotalMilliseconds >= frameBudgetMs)
                    {
                        yield return null;
                        clock.Restart();
                    }
                }
            for (int i = used; i < poolTiles.Count; i++)
                if (poolTiles[i] != null) poolTiles[i].gameObject.SetActive(false);

            HighlightSelection();
            poolBuildRoutine = null;
        }

        /// <summary>Synchroner Fallback (inaktives Objekt kann keine Coroutine fahren).</summary>
        private void RebuildPoolNow()
        {
            var routine = RebuildPoolIncremental();
            while (routine.MoveNext()) { }
        }

        private CollectionCardTile CreatePoolTile()
        {
            if (previewView == null) return null;
            var go = new GameObject("CardTile", typeof(RectTransform));
            go.transform.SetParent(poolContent, false);
            var tile = go.AddComponent<CollectionCardTile>();
            tile.Build(previewView, skin);
            tile.SetDropTarget(deckDropArea);
            poolTiles.Add(tile);
            return tile;
        }

        private void RebuildDeck()
        {
            if (extraHeader != null) { Destroy(extraHeader); extraHeader = null; }
            var deck = CurrentDeck;
            if (deckContent == null || deckMainGrid == null) return;

            var inDeck = deck == null
                ? new List<CardDefinition>()
                : pool.Where(c => c != null && !(c is ReliquaryCardData) && CountInDeck(c) > 0)
                    .OrderBy(c => c.Kind).ThenBy(c => c is MonsterCardData m ? m.level : 0).ThenBy(c => c.cardName)
                    .ToList();

            // Kacheln wie im Pool recycelt; die Position im jeweiligen Gitter wird
            // explizit gesetzt, weil eine Kachel beim Wiederverwenden auch die
            // Seite wechseln kann (Hauptdeck <-> Extra).
            int used = 0, mainTiles = 0, extraTiles = 0;
            foreach (var card in inDeck)
                foreach (var finish in FinishRowsFor(card))
                {
                    if (CountInDeck(card, finish) <= 0) continue;
                    var tile = used < deckTiles.Count ? deckTiles[used] : CreateDeckTile();
                    if (tile == null) break;
                    used++;
                    PlaceDeckTile(tile, deckMainGrid, mainTiles++);
                    tile.Setup(card, finish, CountInDeck(card, finish), OwnedOf(card, finish),
                        AllowedCopies(card), CountInDeck(card),
                        AddCard, RemoveCard, Select, BanLimitOf(card), CollectionMode, true);
                }

            // Extra-Deck-Sektion: Trenner-Header + Reliquary-Gitter unter dem Hauptgitter
            var inExtra = deck == null
                ? new List<CardDefinition>()
                : pool.Where(c => c is ReliquaryCardData && CountInDeck(c) > 0)
                    .OrderBy(c => c.cardName).ToList();
            int extraCount = deck != null ? deck.Extra.Count : 0;
            if (extraCount > 0 || inExtra.Count > 0)
            {
                extraHeader = BuildExtraHeader(extraCount);
                // Der Trenner gehört ZWISCHEN die Gitter: Main — Header — Extra
                extraHeader.transform.SetSiblingIndex(deckExtraGrid.GetSiblingIndex());
                foreach (var card in inExtra)
                    foreach (var finish in FinishRowsFor(card))
                    {
                        if (CountInDeck(card, finish) <= 0) continue;
                        var tile = used < deckTiles.Count ? deckTiles[used] : CreateDeckTile();
                        if (tile == null) break;
                        used++;
                        PlaceDeckTile(tile, deckExtraGrid, extraTiles++);
                        tile.Setup(card, finish, CountInDeck(card, finish), OwnedOf(card, finish),
                            AllowedCopies(card), CountInDeck(card),
                            AddCard, RemoveCard, Select, BanLimitOf(card), CollectionMode, true);
                    }
            }

            for (int i = used; i < deckTiles.Count; i++)
                if (deckTiles[i] != null) deckTiles[i].gameObject.SetActive(false);
            SizeDeckGrid(deckMainGrid, mainTiles);
            SizeDeckGrid(deckExtraGrid, extraTiles);

            if (emptyState != null) emptyState.SetActive(inDeck.Count == 0 && inExtra.Count == 0);

            int count = deck != null ? deck.Cards.Count : 0;
            bool legal = count >= DeckMin && count <= DeckMax;
            if (countFill != null) countFill.fillAmount = Mathf.Clamp01(count / (float)DeckMax);
            if (countText != null)
                countText.text = $"<color={(legal ? "#7ACD96" : "#E9A183")}>{count}</color>" +
                                 $" <size=55%><color=#8C7B5F>/ {DeckMin}–{DeckMax}</color></size>";
            HighlightSelection();
        }

        /// <summary>
        /// Auch die Deck-Liste zeigt Kartenbilder: zwei Gitter-Container unter der
        /// bestehenden VerticalLayoutGroup des Contents — so bleibt der
        /// "EXTRA DECK"-Trenner eine normale Zeile dazwischen. Die Layout-Gruppe
        /// steuert nur die Breite der Container; ihre Höhe rechnet SizeDeckGrid
        /// aus der Kachelzahl aus.
        /// </summary>
        private void ConvertDeckToGrids()
        {
            if (deckContent == null) return;
            deckMainGrid = BuildDeckGrid("DeckMainGrid");
            deckExtraGrid = BuildDeckGrid("DeckExtraGrid");
        }

        private RectTransform BuildDeckGrid(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(deckContent, false);
            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CollectionCardTile.Width, CollectionCardTile.Height);
            grid.spacing = new Vector2(8f, 10f);
            grid.padding = new RectOffset(0, 0, 0, 0);   // den Rand macht schon die VerticalLayoutGroup
            grid.childAlignment = TextAnchor.UpperLeft;
            return rect;
        }

        private CollectionCardTile CreateDeckTile()
        {
            if (previewView == null || deckMainGrid == null) return null;
            var go = new GameObject("DeckTile", typeof(RectTransform));
            go.transform.SetParent(deckMainGrid, false);
            var tile = go.AddComponent<CollectionCardTile>();
            tile.Build(previewView, skin);
            tile.SetDropTarget(deckDropArea);
            deckTiles.Add(tile);
            return tile;
        }

        /// <summary>
        /// Hängt eine recycelte Kachel ins richtige Gitter und an die richtige
        /// Stelle. Deaktivierte Rest-Kacheln rutschen dabei nach hinten — fürs
        /// GridLayout zählen nur die aktiven, aber deren Reihenfolge muss stimmen.
        /// </summary>
        private static void PlaceDeckTile(CollectionCardTile tile, RectTransform gridParent, int position)
        {
            tile.gameObject.SetActive(true);
            if (tile.transform.parent != gridParent) tile.transform.SetParent(gridParent, false);
            tile.transform.SetSiblingIndex(Mathf.Min(position, gridParent.childCount - 1));
        }

        /// <summary>Gitterhöhe aus der Kachelzahl — leere Gitter verschwinden ganz.</summary>
        private void SizeDeckGrid(RectTransform gridRect, int tiles)
        {
            if (gridRect == null) return;
            gridRect.gameObject.SetActive(tiles > 0);
            if (tiles <= 0) return;
            float width = ((RectTransform)deckContent).rect.width - 12f - 16f;   // Polster der VerticalLayoutGroup
            int columns = width > CollectionCardTile.Width
                ? Mathf.Max(1, Mathf.FloorToInt((width + 8f) / (CollectionCardTile.Width + 8f)))
                : 4;   // vor dem ersten Layout-Pass ist die Breite noch 0
            int rows = (tiles + columns - 1) / columns;
            float height = rows * CollectionCardTile.Height + (rows - 1) * 10f;
            gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>Laufzeit-Header "EXTRA DECK x/20" zwischen Haupt- und Reliquary-Gitter.</summary>
        private GameObject BuildExtraHeader(int extraCount)
        {
            var header = new GameObject("ExtraDeckHeader", typeof(RectTransform));
            header.transform.SetParent(deckContent, false);
            // Die Layout-Gruppe steuert nur die Breite (childControlHeight aus) —
            // die Höhe muss am Rechteck selbst stehen, sonst stapelt sie mit 0.
            ((RectTransform)header.transform).sizeDelta = new Vector2(0f, 36f);
            var layout = header.AddComponent<LayoutElement>();
            layout.minHeight = 36f;
            layout.preferredHeight = 36f;
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(header.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f); rect.offsetMax = new Vector2(-10f, -6f);
            label.text = $"<color=#F1E7D2>◆ EXTRA DECK</color>  <color=#8C7B5F>{extraCount} / {ExtraMax}</color>";
            label.fontSize = 16f;
            label.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            var rowText = rowPrefab != null ? rowPrefab.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
            if (rowText != null) { label.font = rowText.font; label.fontSharedMaterial = rowText.fontSharedMaterial; }
            return header;
        }

        private void AddCard(CardDefinition card, CardFinish finish)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return;
            bool isReliquary = card is ReliquaryCardData;
            if (isReliquary)
            {
                if (deck.Extra.Count >= ExtraMax) { ShowFeedback($"Extra Deck is full (max {ExtraMax})."); return; }
            }
            else if (deck.Cards.Count >= DeckMax) { ShowFeedback($"Deck is full (max {DeckMax})."); return; }

            int banLimit = AllowedCopies(card);
            if (banLimit <= 0)
            {
                ShowFeedback($"{card.cardName} is forbidden by the banlist.");
                return;
            }

            // Das Kopienlimit gilt für die KARTE — drei Exemplare sind drei, egal
            // welches Finish.
            if (CountInDeck(card) >= banLimit)
            {
                ShowFeedback(PlayerProfile.IsRestricted(card.cardName)
                    ? $"{card.cardName} is limited to {banLimit} cop{(banLimit == 1 ? "y" : "ies")}."
                    : $"Maximum {banLimit} copies.");
                return;
            }

            // Der Besitz gilt für das EXEMPLAR — wer zwei Static einbaut, braucht zwei.
            if (CollectionMode && CountInDeck(card, finish) >= PlayerProfile.Owned(card.cardName, finish))
            {
                ShowFeedback(finish == CardFinish.Plain
                    ? (PlayerProfile.Owned(card.cardName, finish) < 1
                        ? "You do not own this card — craft it first."
                        : "No plain copies left.")
                    : $"No {CardFinishInfo.Label(finish)} copies left.");
                return;
            }

            if (isReliquary)
            {
                deck.Extra.Add(card.cardName);
                deck.ExtraFinishes.Add(finish);
            }
            else
            {
                deck.Cards.Add(card.cardName);
                deck.CardFinishes.Add(finish);
            }
            MarkEdited();
            Select(card, finish);
            RebuildAll();
        }

        private void RemoveCard(CardDefinition card, CardFinish finish)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return;
            bool isReliquary = card is ReliquaryCardData;
            var list = isReliquary ? deck.Extra : deck.Cards;
            var finishes = isReliquary ? deck.ExtraFinishes : deck.CardFinishes;

            // Von hinten: das zuletzt gelegte Exemplar geht zuerst wieder raus
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != card.cardName) continue;
                var slot = isReliquary ? deck.ExtraFinishAt(i) : deck.FinishAt(i);
                if (slot != finish) continue;
                list.RemoveAt(i);
                if (i < finishes.Count) finishes.RemoveAt(i);
                MarkEdited();
                RebuildAll();
                return;
            }
        }

        // ---------- Detail-Rail ----------

        /// <summary>Setzt die Karte der Detail-Rail — ausgelöst durch einen Klick auf eine Zeile.</summary>
        private void Select(CardDefinition card, CardFinish finish)
        {
            selected = card;
            selectedFinish = finish;
            MarkCardSeen(card);
            ShowPreview();
            if (previewEmpty != null) previewEmpty.SetActive(card == null);
            RefreshCardText(card);
            RefreshCraftButtons();
            HighlightSelection();
        }

        /// <summary>
        /// Anklicken einer "neuen" Karte nimmt ihr das NEW-Badge — lokal sofort
        /// (alle Kacheln dieser Karte), der Server vergisst sie über seen_card.
        /// Die Kachel bleibt bis zum nächsten Filter-Rebuild sichtbar, damit
        /// unter dem NEW-Filter nichts unterm Zeiger wegspringt.
        /// </summary>
        private void MarkCardSeen(CardDefinition card)
        {
            if (card == null || !CollectionMode) return;
            if (!PlayerProfile.MarkSeen(card.cardName)) return;
            foreach (var tile in poolTiles)
                if (tile != null && tile.Card == card) tile.HideNewBadge();
            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected) net.SendSeenCard(card.cardName);
        }

        /// <summary>Zeichnet die Vorschaukarte in der gerade gewählten Ausführung.</summary>
        private void ShowPreview()
        {
            if (previewView != null && selected != null)
            {
                previewView.gameObject.SetActive(true);
                previewView.Show(new CardInstance(selected, null) { Finish = selectedFinish }, false, upright: true);
                previewView.SetHighlight(false);
            }
            RefreshFinishChips();
        }

        private void HighlightSelection()
        {
            // Die Kachel des angezeigten Exemplars leuchtet, nicht jede Kachel der
            // Karte — sonst hätte man bei drei Finishes drei helle Kacheln und
            // wüsste nicht, welche gerade rechts steht.
            foreach (var tile in poolTiles)
                if (tile != null && tile.gameObject.activeSelf)
                    tile.SetSelected(tile.Card == selected && tile.Finish == selectedFinish);
            foreach (var tile in deckTiles)
                if (tile != null && tile.gameObject.activeSelf)
                    tile.SetSelected(tile.Card == selected && tile.Finish == selectedFinish);
        }

        // ---------- Finish-Umschalter unter der Vorschau ----------

        /// <summary>
        /// Eine Leiste mit allen vier Ausführungen unter der Vorschaukarte. Sie
        /// zeigt AUCH die, die man nicht besitzt: erst wer Regenbogen einmal
        /// gesehen hat, weiss, wofür sich das Öffnen lohnt. Was man hat, steht
        /// hell und mit Stückzahl da; der Rest bleibt gedämpft.
        /// </summary>
        private void RefreshFinishChips()
        {
            if (previewView == null) return;
            if (finishStrip == null) BuildFinishStrip();
            if (finishStrip == null) return;

            finishStrip.gameObject.SetActive(selected != null && !(selected is PlayerCardData));
            if (selected == null) return;

            var stock = CollectionMode ? PlayerProfile.StockOf(selected.cardName) : null;
            for (int i = 0; i < finishChips.Count; i++)
            {
                var finish = (CardFinish)i;
                int owned = stock != null ? stock[finish] : 0;
                bool active = finish == selectedFinish;
                bool has = owned > 0 || finish == CardFinish.Plain;

                var accent = CardFinishInfo.Accent(finish);
                finishChips[i].Background.color = active
                    ? new Color(accent.r, accent.g, accent.b, 0.22f)
                    : new Color(0f, 0f, 0f, 0.35f);
                finishChips[i].Frame.color = active
                    ? accent
                    : new Color(accent.r, accent.g, accent.b, has ? 0.4f : 0.16f);

                var label = finishChips[i].Label;
                label.text = owned > 0
                    ? $"{CardFinishInfo.Label(finish).ToUpperInvariant()} {owned}"
                    : CardFinishInfo.Label(finish).ToUpperInvariant();
                label.color = active ? accent
                    : new Color(accent.r, accent.g, accent.b, has ? 0.75f : 0.35f);
            }
        }

        private void BuildFinishStrip()
        {
            // Hochkant an die rechte Flanke der Karte, NICHT darunter: unter der
            // Vorschau liegen nur 14 Einheiten Luft, dann kommen schon Dust und
            // Craft — eine Leiste dort schnitte in die Knöpfe. Neben der Karte
            // stehen dagegen 139 Einheiten leer.
            //
            // Kind der Vorschaukarte, damit die Leiste an deren Rechteck klebt,
            // wie auch immer die Rail sonst aufgeteilt ist.
            var go = new GameObject("FinishStrip", typeof(RectTransform));
            finishStrip = (RectTransform)go.transform;
            finishStrip.SetParent((RectTransform)previewView.transform, false);
            finishStrip.anchorMin = finishStrip.anchorMax = new Vector2(1f, 0.5f);
            finishStrip.pivot = new Vector2(0f, 0.5f);
            finishStrip.anchoredPosition = new Vector2(ChipGap, 0f);
            finishStrip.sizeDelta = new Vector2(ChipWidth,
                CardFinishInfo.Count * ChipHeight + (CardFinishInfo.Count - 1) * ChipSpacing);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = ChipSpacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < CardFinishInfo.Count; i++)
                finishChips.Add(BuildFinishChip((CardFinish)i));
        }

        // Masse der Finish-Leiste. Rechts der Vorschaukarte sind 139 Einheiten
        // frei — Abstand plus Breite müssen darunter bleiben.
        private const float ChipGap = 10f;
        private const float ChipWidth = 118f;
        private const float ChipHeight = 30f;
        private const float ChipSpacing = 6f;

        private FinishChip BuildFinishChip(CardFinish finish)
        {
            var go = new GameObject(CardFinishInfo.Label(finish), typeof(RectTransform));
            go.transform.SetParent(finishStrip, false);

            var background = go.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);

            var frameGo = new GameObject("Frame", typeof(RectTransform));
            var frameRect = (RectTransform)frameGo.transform;
            frameRect.SetParent(go.transform, false);
            frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero; frameRect.offsetMax = Vector2.zero;
            var frame = frameGo.AddComponent<Image>();
            frame.sprite = skin != null ? skin.whiteFrame : null;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(go.transform, false);
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = CardFinishInfo.Label(finish).ToUpperInvariant();
            label.fontSize = 13f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 13f;
            label.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                SfxManager.Click();
                selectedFinish = finish;
                ShowPreview();
                HighlightSelection();
            });

            return new FinishChip { Background = background, Frame = frame, Label = label };
        }

        /// <summary>Die drei Teile eines Umschalt-Chips, damit RefreshFinishChips sie einfärben kann.</summary>
        private struct FinishChip
        {
            public Image Background;
            public Image Frame;
            public TMP_Text Label;
        }

        /// <summary>Hover-Textbox: Typzeile + kompletter Effekttext der Karte unter dem Cursor.</summary>
        private void RefreshCardText(CardDefinition card)
        {
            if (cardTextBody == null) return;
            if (card == null) { cardTextBody.text = "<color=#7E7059><i>Click a card to read it.</i></color>"; return; }

            string header;
            switch (card)
            {
                case ReliquaryCardData r:
                    string relicAttr = ColorUtility.ToHtmlStringRGB(MonsterCardData.AttributeColor(r.attribute));
                    header = "<color=#F1E7D2>RELIQUARY</color><color=#8C7B5F> · </color>" +
                             $"<color=#{relicAttr}>{r.attribute.ToString().ToUpperInvariant()}</color>" +
                             $"<color=#8C7B5F> / {r.monsterType.ToString().ToUpperInvariant()} · {r.atk} ATK / {r.def} DEF</color>";
                    break;
                case MonsterCardData m:
                    string attrHex = ColorUtility.ToHtmlStringRGB(MonsterCardData.AttributeColor(m.attribute));
                    header = $"<color=#{attrHex}>{m.attribute.ToString().ToUpperInvariant()}</color>" +
                             $"<color=#8C7B5F> / {m.monsterType.ToString().ToUpperInvariant()} · LV {m.level} · {m.atk} ATK / {m.def} DEF</color>";
                    break;
                case SpellCardData s:
                    header = $"<color=#8FC6D2>{(s.speed == SpellSpeed.Quick ? "QUICK SPELL" : "SPELL")}</color>";
                    break;
                case ArtifactCardData a:
                    header = $"<color=#B9A3E0>ARTIFACT / {TcgCardView.ArtifactSlotName(a.slot).ToUpperInvariant()}</color>" +
                             (a.slot == ArtifactSlot.Monster && (a.atkBonus != 0 || a.defBonus != 0)
                                 ? $"<color=#8C7B5F> · +{a.atkBonus} ATK / +{a.defBonus} DEF</color>" : "");
                    break;
                default:
                    header = "";
                    break;
            }

            string rarityHex = ColorUtility.ToHtmlStringRGB(CollectionRow.RarityStrong(card.rarity));
            header += $"<color=#8C7B5F> · </color><color=#{rarityHex}>{CardDefinition.RarityName(card.rarity).ToUpperInvariant()}</color>";
            cardTextBody.text = header + "\n\n" + CardDetailPanel.BuildFormattedRulesText(card);
            if (cardTextScroll != null) cardTextScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshCraftButtons()
        {
            bool online = CollectionMode && selected != null && !(selected is PlayerCardData);
            int dust = selected != null ? CardEconomy.DustGain(selected.rarity) : 0;
            int craft = selected != null ? CardEconomy.CraftCost(selected.rarity) : 0;
            if (dustLabel != null) dustLabel.text = $"DUST · +{dust}";
            if (craftLabel != null) craftLabel.text = $"CRAFT · −{craft}";
            if (dustButton != null) dustButton.interactable = online && PlayerProfile.Owned(selected.cardName) > 0;
            if (craftButton != null) craftButton.interactable = online && PlayerProfile.Tokens(selected.rarity) >= craft;
        }

        private void DustSelected()
        {
            if (!CollectionMode || selected == null) return;
            network.SendDust(selected.cardName);
            ShowFeedback($"Dusting {selected.cardName} (+{CardEconomy.DustGain(selected.rarity)})…");
        }

        private void CraftSelected()
        {
            if (!CollectionMode || selected == null) return;
            network.SendCraft(selected.cardName);
            ShowFeedback($"Crafting {selected.cardName} (−{CardEconomy.CraftCost(selected.rarity)})…");
        }

        // ---------- Wallet, Balance & Advice ----------
        private void RefreshWallet()
        {
            var values = new[] { PlayerProfile.TokensCommon, PlayerProfile.TokensUncommon, PlayerProfile.TokensRare, PlayerProfile.TokensLegendary };
            for (int i = 0; i < dustTexts.Length && i < 4; i++)
                if (dustTexts[i] != null) dustTexts[i].text = values[i].ToString();
            if (coinsText != null) coinsText.text = MainMenuController.FormatCoins(PlayerProfile.Coins);
        }

        private void RefreshBalance()
        {
            var deck = CurrentDeck;
            var cardsInDeck = new List<CardDefinition>();
            if (deck != null)
                foreach (var name in deck.Cards)
                {
                    var definition = catalog != null ? catalog.FindByName(name) : null;
                    if (definition != null) cardsInDeck.Add(definition);
                }

            int[] levels = new int[3];
            int monsters = 0, spells = 0, artifacts = 0;
            var attrCounts = new Dictionary<MonsterAttribute, int>();
            foreach (var card in cardsInDeck)
            {
                if (card is MonsterCardData m)
                {
                    monsters++;
                    int lv = Mathf.Clamp(m.level, 1, 3);
                    levels[lv - 1]++;
                    attrCounts[m.attribute] = attrCounts.TryGetValue(m.attribute, out int a) ? a + 1 : 1;
                }
                else if (card is SpellCardData) spells++;
                else if (card is ArtifactCardData) artifacts++;
            }

            int biggest = Mathf.Max(1, Mathf.Max(levels[0], Mathf.Max(levels[1], levels[2])));
            for (int i = 0; i < 3; i++)
            {
                if (levelFills[i] != null) levelFills[i].fillAmount = levels[i] / (float)biggest;
                if (levelCounts[i] != null) levelCounts[i].text = levels[i].ToString();
            }
            if (typeSplitCounts.Length >= 3)
            {
                if (typeSplitCounts[0] != null) typeSplitCounts[0].text = monsters.ToString();
                if (typeSplitCounts[1] != null) typeSplitCounts[1].text = spells.ToString();
                if (typeSplitCounts[2] != null) typeSplitCounts[2].text = artifacts.ToString();
            }

            // Attribut-Spread: ein Segment pro Attribut, Breiten proportional
            if (attributeBar != null)
            {
                int total = 0;
                foreach (var kv in attrCounts) total += kv.Value;
                foreach (Transform segment in attributeBar)
                {
                    var image = segment.GetComponent<Image>();
                    var layout = segment.GetComponent<LayoutElement>();
                    if (image == null || layout == null) continue;
                    if (System.Enum.TryParse(segment.name, out MonsterAttribute attr) && attrCounts.TryGetValue(attr, out int n) && total > 0)
                    {
                        segment.gameObject.SetActive(true);
                        layout.flexibleWidth = n;
                        image.color = MonsterCardData.AttributeColor(attr);
                    }
                    else segment.gameObject.SetActive(false);
                }
                attributeBar.gameObject.SetActive(total > 0);
            }

            if (adviceText != null) adviceText.text = BuildAdvice(deck, monsters, levels, artifacts);
        }

        private string BuildAdvice(RuntimeDeck deck, int monsters, int[] levels, int artifacts)
        {
            if (deck == null) return "Create a deck to begin.";
            int count = deck.Cards.Count;
            if (count < DeckMin) return $"Add {DeckMin - count} more card{(DeckMin - count == 1 ? "" : "s")} to make this deck legal.";
            if (count > DeckMax) return $"Remove {count - DeckMax} card{(count - DeckMax == 1 ? "" : "s")} — the vault seals at {DeckMax}.";
            if (monsters < 14) return $"Only {monsters} monsters — you may struggle to hold the field. Aim for 14+.";
            if (levels[2] > 6) return $"{levels[2]} Level 3 monsters is top-heavy — early turns may starve. Consider trimming.";
            if (artifacts == 0) return "No artifacts — a weapon or armor piece adds staying power.";
            return "This deck is legal and balanced. Seal it and duel.";
        }

        // ---------- Speichern ----------
        private void MarkEdited()
        {
            savedState = false;
            editedDeck = CurrentDeck;
            RefreshSaveButton();
        }

        private void RefreshSaveButton()
        {
            if (saveLabel != null) saveLabel.text = savedState ? "SAVED ✓" : "SAVE DECK";
        }

        private void SaveCurrentDeck()
        {
            var deck = CurrentDeck;
            if (!CollectionMode || deck == null) return;
            awaitingSave = true;
            network.SendSaveDeck(CurrentIndex, deck);
            ShowFeedback($"Saving {deck.Name}…");
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message;
        }
    }
}
