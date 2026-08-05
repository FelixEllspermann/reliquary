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
    /// Deck Builder im Collection-Design: Pool mit Suche + Typ-/Attribut-Filtern,
    /// Deck-Liste mit Hero-Chips und Legalitäts-Bar, Detail-Rail mit Kartenvorschau,
    /// Dust/Craft, abgeleiteter Deck-Balance und Advice-Strip. Server-Fluss wie gehabt
    /// (save_deck/delete_deck/craft/dust über den NetworkManager).
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

        private static readonly MonsterAttribute[] AttrOrder =
        {
            MonsterAttribute.Fire, MonsterAttribute.Water, MonsterAttribute.Light,
            MonsterAttribute.Dark, MonsterAttribute.Earth, MonsterAttribute.Wind
        };

        private NetworkManager network;
        private List<CardDefinition> pool = new List<CardDefinition>();
        private List<PlayerCardData> heroes = new List<PlayerCardData>();
        private readonly List<CollectionRow> poolRows = new List<CollectionRow>();
        private readonly List<CollectionRow> deckRows = new List<CollectionRow>();
        private readonly List<Button> heroChips = new List<Button>();
        private GameObject extraHeader;  // Laufzeit-Trenner "EXTRA DECK x/20"

        private int typeFilter;   // 0 all, 1 monster, 2 spell, 3 artifact, 4 reliquary
        private int attrFilter;   // 0 all, 1..6 = AttrOrder
        private string search = "";
        private CardDefinition selected;
        private bool savedState;      // SAVE DECK ↔ SAVED ✓
        private bool awaitingSave;

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
                pool = catalog.cards.Where(c => c != null && !(c is PlayerCardData)).ToList();
                heroes = catalog.cards.OfType<PlayerCardData>().ToList();
            }

            if (searchInput != null) searchInput.onValueChanged.AddListener(value => { search = value ?? ""; RebuildPool(); });
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
            if (dustButton != null) dustButton.onClick.AddListener(DustSelected);
            if (craftButton != null) craftButton.onClick.AddListener(CraftSelected);
            if (network != null) network.OnMessage += HandleNet;

            BuildHeroChips();
            RefreshChips();
            RefreshDeckDropdown();
            RebuildAll();
            if (pool.Count > 0) Select(pool[0]);
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
                    if (awaitingSave) { awaitingSave = false; savedState = true; }
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

        private bool PassesFilter(CardDefinition card)
        {
            // MONSTER meint das Hauptdeck — Reliquarys haben ihren eigenen Filter
            if (typeFilter == 1 && (!(card is MonsterCardData) || card is ReliquaryCardData)) return false;
            if (typeFilter == 2 && !(card is SpellCardData)) return false;
            if (typeFilter == 3 && !(card is ArtifactCardData)) return false;
            if (typeFilter == 4 && !(card is ReliquaryCardData)) return false;
            if (attrFilter > 0)
            {
                var monster = card as MonsterCardData;
                if (monster == null || monster.attribute != AttrOrder[attrFilter - 1]) return false;
            }
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
            var fresh = new RuntimeDeck { Name = "Deck " + (PlayerProfile.Decks.Count + 1), Hero = heroes.Count > 0 ? heroes[0].cardName : "" };
            network.SendSaveDeck(PlayerProfile.Decks.Count, fresh);
            ShowFeedback($"Creating {fresh.Name}…");
        }

        private void DeleteDeck()
        {
            if (!CollectionMode || PlayerProfile.Decks.Count <= 1) return;
            network.SendDeleteDeck(CurrentIndex);
            ShowFeedback("Deleting deck…");
        }

        private void BuildHeroChips()
        {
            heroChips.Clear();
            if (heroChipContainer == null) return;
            foreach (Transform child in heroChipContainer)
            {
                var button = child.GetComponent<Button>();
                if (button != null) heroChips.Add(button);
            }
            for (int i = 0; i < heroChips.Count; i++)
            {
                int index = i;
                var label = heroChips[i].GetComponentInChildren<TMP_Text>(true);
                bool exists = i < heroes.Count;
                heroChips[i].gameObject.SetActive(exists);
                if (!exists) continue;
                if (label != null)
                {
                    string first = heroes[i].cardName.Split(' ')[0];
                    label.text = first.ToUpperInvariant();
                }
                heroChips[i].onClick.AddListener(() => SetHero(index));
            }
        }

        private void SetHero(int index)
        {
            var deck = CurrentDeck;
            if (deck == null || index < 0 || index >= heroes.Count) return;
            deck.Hero = heroes[index].cardName;
            MarkEdited();
            RefreshHeroChips();
            Select(heroes[index]);
        }

        private void RefreshHeroChips()
        {
            var deck = CurrentDeck;
            for (int i = 0; i < heroChips.Count && i < heroes.Count; i++)
            {
                bool active = deck != null && deck.Hero == heroes[i].cardName;
                var bg = heroChips[i].GetComponent<Image>();
                var label = heroChips[i].GetComponentInChildren<TMP_Text>(true);
                StyleChip(bg, label, active, new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f));
            }
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
            RebuildPool();
            RebuildDeck();
            RefreshHeroChips();
            RefreshWallet();
            RefreshBalance();
            RefreshSaveButton();
        }

        private void RebuildPool()
        {
            foreach (var row in poolRows) if (row != null) Destroy(row.gameObject);
            poolRows.Clear();
            if (poolContent == null || rowPrefab == null) return;

            var filtered = pool.Where(c => c != null && PassesFilter(c))
                .OrderBy(c => c.Kind).ThenBy(c => c is MonsterCardData m ? m.level : 0).ThenBy(c => c.cardName)
                .ToList();

            // Je Karte eine Zeile pro besessenem Finish — so lassen sich gezielt die
            // zwei Static einbauen statt der schlichten Exemplare.
            foreach (var card in filtered)
                foreach (var finish in FinishRowsFor(card))
                {
                    var row = Instantiate(rowPrefab, poolContent);
                    row.Setup(card, finish, CountInDeck(card, finish), OwnedOf(card, finish),
                        AllowedCopies(card), CountInDeck(card), false,
                        AddCard, RemoveCard, Select, BanLimitOf(card));
                    poolRows.Add(row);
                }
            if (poolCountText != null) poolCountText.text = $"{filtered.Count} of {pool.Count} cards";
            HighlightSelection();
        }

        private void RebuildDeck()
        {
            foreach (var row in deckRows) if (row != null) Destroy(row.gameObject);
            deckRows.Clear();
            if (extraHeader != null) { Destroy(extraHeader); extraHeader = null; }
            var deck = CurrentDeck;
            if (deckContent == null || rowPrefab == null) return;

            var inDeck = deck == null
                ? new List<CardDefinition>()
                : pool.Where(c => c != null && !(c is ReliquaryCardData) && CountInDeck(c) > 0)
                    .OrderBy(c => c.Kind).ThenBy(c => c is MonsterCardData m ? m.level : 0).ThenBy(c => c.cardName)
                    .ToList();

            foreach (var card in inDeck)
                foreach (var finish in FinishRowsFor(card))
                {
                    if (CountInDeck(card, finish) <= 0) continue;
                    var row = Instantiate(rowPrefab, deckContent);
                    row.Setup(card, finish, CountInDeck(card, finish), OwnedOf(card, finish),
                        AllowedCopies(card), CountInDeck(card), true,
                        AddCard, RemoveCard, Select, BanLimitOf(card));
                    deckRows.Add(row);
                }

            // Extra-Deck-Sektion: Trenner-Header + Reliquary-Zeilen unter der Hauptliste
            var inExtra = deck == null
                ? new List<CardDefinition>()
                : pool.Where(c => c is ReliquaryCardData && CountInDeck(c) > 0)
                    .OrderBy(c => c.cardName).ToList();
            int extraCount = deck != null ? deck.Extra.Count : 0;
            if (extraCount > 0 || inExtra.Count > 0)
            {
                extraHeader = BuildExtraHeader(extraCount);
                foreach (var card in inExtra)
                    foreach (var finish in FinishRowsFor(card))
                    {
                        if (CountInDeck(card, finish) <= 0) continue;
                        var row = Instantiate(rowPrefab, deckContent);
                        row.Setup(card, finish, CountInDeck(card, finish), OwnedOf(card, finish),
                            AllowedCopies(card), CountInDeck(card), true,
                            AddCard, RemoveCard, Select, BanLimitOf(card));
                        deckRows.Add(row);
                    }
            }

            if (emptyState != null) emptyState.SetActive(inDeck.Count == 0 && inExtra.Count == 0);

            int count = deck != null ? deck.Cards.Count : 0;
            bool legal = count >= DeckMin && count <= DeckMax;
            if (countFill != null) countFill.fillAmount = Mathf.Clamp01(count / (float)DeckMax);
            if (countText != null)
                countText.text = $"<color={(legal ? "#7ACD96" : "#E9A183")}>{count}</color>" +
                                 $" <size=55%><color=#8C7B5F>/ {DeckMin}–{DeckMax}</color></size>";
            HighlightSelection();
        }

        /// <summary>Laufzeit-Header "EXTRA DECK x/20" zwischen Haupt- und Reliquary-Zeilen.</summary>
        private GameObject BuildExtraHeader(int extraCount)
        {
            var header = new GameObject("ExtraDeckHeader", typeof(RectTransform));
            header.transform.SetParent(deckContent, false);
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
            Select(card);
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
        private void Select(CardDefinition card)
        {
            selected = card;
            if (previewView != null && card != null)
            {
                previewView.gameObject.SetActive(true);
                previewView.Show(new CardInstance(card, null), false, upright: true);
                previewView.SetHighlight(false);
            }
            if (previewEmpty != null) previewEmpty.SetActive(card == null);
            RefreshCardText(card);
            RefreshCraftButtons();
            HighlightSelection();
        }

        private void HighlightSelection()
        {
            foreach (var row in poolRows) if (row != null) row.SetSelected(row.Card == selected);
            foreach (var row in deckRows) if (row != null) row.SetSelected(row.Card == selected);
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
