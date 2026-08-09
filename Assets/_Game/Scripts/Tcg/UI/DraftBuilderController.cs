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
    /// Der separate Draft-Deck-Builder (Challenges → Draft Mode): 20 Packs
    /// werden serverseitig gezogen, aus dem Pool baut man ein TEMPORÄRES Deck —
    /// ohne Kopienlimit, ohne Banlist, mit frei wählbarem Helden — und steigt
    /// damit den Turm hinauf. Nichts davon landet in der Sammlung; nach dem
    /// Abschluss (oder dem Verwerfen) ist das Deck weg.
    ///
    /// Die Szene enthält nur Canvas + diesen Controller; das komplette UI
    /// entsteht in Start. Drei Zustände: kein Draft (Intro + DRAW 20 PACKS),
    /// laufender Draft (Pool-Gitter, Deck-Gitter, Aktionen), offline (Hinweis).
    /// </summary>
    public class DraftBuilderController : MonoBehaviour
    {
        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;
        [SerializeField] private GameRules rules;
        [SerializeField] private CardSkin skin;
        [SerializeField] private TcgCardView cardViewPrefab;
        [SerializeField] private TowerDefinition tower;

        [Header("Szenen")]
        [SerializeField] private string playSceneName = "Play";
        [SerializeField] private string duelSceneName = "Duel";

        private const int DraftFloors = 15;

        private NetworkManager network;
        private RuntimeDeck editedDeck;          // Arbeitskopie, bis SAVE sie zum Server trägt
        private bool awaitingStart;              // DRAW 20 PACKS unterwegs
        private bool awaitingEnter;              // Speichern läuft, danach ins Duell
        private bool armedAbandon;               // erster Klick auf ABANDON scharfgestellt

        // ---- Laufzeit-UI ----
        private RectTransform root;
        private RectTransform introPanel;
        private RectTransform builderPanel;
        private RectTransform poolContent;
        private RectTransform deckMainGrid;
        private RectTransform deckExtraGrid;
        private GameObject deckExtraHeader;
        private RectTransform deckPanel;         // Drop-Ziel
        private TMP_Text headerText;
        private TMP_Text deckCountText;
        private TMP_Text heroLabel;
        private TMP_Text enterLabel;
        private Button enterButton;
        private TMP_Text abandonLabel;
        private TMP_Text feedbackText;
        private TMP_Text introStatus;
        private Button drawButton;
        private readonly List<CollectionCardTile> poolTiles = new List<CollectionCardTile>();
        private readonly List<CollectionCardTile> deckTiles = new List<CollectionCardTile>();
        private GameObject previewOverlay;
        private GameObject heroPicker;

        private static readonly Color Emerald = new Color32(0x3F, 0xCF, 0x8C, 0xFF);
        private static readonly Color EmeraldBright = new Color32(0xBD, 0xF0, 0xD4, 0xFF);
        private static readonly Color EmeraldMuted = new Color32(0x6F, 0xBF, 0x9A, 0xFF);
        private static readonly Color Parchment = new Color32(0xF1, 0xE7, 0xD2, 0xFF);
        private static readonly Color Ink = new Color32(0x8C, 0x7B, 0x5F, 0xFF);

        private int DeckMin => rules != null ? rules.deckMinSize : 40;
        private int DeckMax => rules != null ? rules.deckMaxSize : 80;
        private int ExtraMax => rules != null ? rules.extraDeckMaxSize : 20;
        private bool Online => PlayerProfile.LoggedIn && network != null && network.IsConnected;

        private void Start()
        {
            network = NetworkManager.Instance;
            if (network != null) network.OnMessage += HandleNet;
            BuildShell();
            RebuildAll();
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
                    awaitingStart = false;
                    if (awaitingEnter && PlayerProfile.DraftDeck != null)
                    {
                        // Das Deck ist serverseitig bestätigt — jetzt in den Turm
                        awaitingEnter = false;
                        LaunchDraftDuel();
                        return;
                    }
                    awaitingEnter = false;
                    // Frischer Serverstand ersetzt die Arbeitskopie
                    editedDeck = null;
                    RebuildAll();
                    break;
                case "error":
                    awaitingStart = false;
                    awaitingEnter = false;
                    ShowFeedback(message.msg);
                    RebuildAll();
                    break;
            }
        }

        // ================== GRUNDGERÜST ==================

        private void BuildShell()
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;
            root = (RectTransform)canvas.transform;

            var bg = MakeImage("Backdrop", root, new Color(0.043f, 0.033f, 0.024f, 1f));
            Stretch(bg.rectTransform);

            headerText = MakeText("Header", root, 26f, Parchment, TextAlignmentOptions.MidlineLeft);
            Place(headerText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -64f), new Vector2(1100f, 44f));
            headerText.characterSpacing = 6f;

            var backButton = MakeButton("Back", root, "BACK", out var backLabel);
            Place((RectTransform)backButton.transform, new Vector2(1f, 1f), new Vector2(-180f, -60f), new Vector2(150f, 44f));
            backLabel.color = Parchment;
            backButton.onClick.AddListener(() => SceneManager.LoadScene(playSceneName));

            feedbackText = MakeText("Feedback", root, 15f, new Color32(0xE9, 0xA1, 0x83, 0xFF), TextAlignmentOptions.Midline);
            Place(feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1200f, 30f));

            BuildIntroPanel();
            BuildBuilderPanel();
        }

        private void BuildIntroPanel()
        {
            introPanel = MakeRect("IntroPanel", root);
            introPanel.anchorMin = introPanel.anchorMax = new Vector2(0.5f, 0.5f);
            introPanel.sizeDelta = new Vector2(760f, 560f);
            var bg = introPanel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.1f, 0.075f, 0.92f);
            AddFrame(introPanel, new Color(Emerald.r, Emerald.g, Emerald.b, 0.6f));

            var title = MakeText("Title", introPanel, 34f, EmeraldBright, TextAlignmentOptions.Midline);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(700f, 46f));
            title.characterSpacing = 10f;
            title.text = "THE DRAFT";

            var body = MakeText("Body", introPanel, 17f, Parchment, TextAlignmentOptions.Top);
            Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(640f, 260f));
            body.text =
                "Draw <color=#BDF0D4><b>20 packs</b></color> and build a deck from nothing but what you pull.\n\n" +
                "No copy limits — pulled a card eight times, play it eight times. No banlist. Any hero.\n\n" +
                "The deck is <b>borrowed</b>: the cards are not added to your collection, and when the run ends, the deck is gone.\n\n" +
                "Conquer all 15 floors of the Tower to earn <color=#BDF0D4><b>+10 Relic Packs</b></color> — every single run. " +
                "The first conquest seals the title <color=#BDF0D4><b>Draft Sovereign</b></color> into your name.";

            introStatus = MakeText("Status", introPanel, 14f, EmeraldMuted, TextAlignmentOptions.Midline);
            Place(introStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(640f, 26f));

            drawButton = MakeButton("Draw", introPanel, "DRAW 20 PACKS", out var drawLabel, 20f);
            Place((RectTransform)drawButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(360f, 58f));
            drawLabel.color = new Color32(0x0A, 0x1F, 0x14, 0xFF);
            drawLabel.fontStyle = FontStyles.Bold;
            drawButton.GetComponent<Image>().color = Emerald;
            drawButton.onClick.AddListener(BeginDraft);
        }

        private void BuildBuilderPanel()
        {
            builderPanel = MakeRect("BuilderPanel", root);
            Stretch(builderPanel);

            // ---- Pool links ----
            var poolPanel = MakeRect("PoolPanel", builderPanel);
            poolPanel.anchorMin = new Vector2(0f, 0f); poolPanel.anchorMax = new Vector2(0f, 1f);
            poolPanel.pivot = new Vector2(0f, 0.5f);
            poolPanel.offsetMin = new Vector2(40f, 70f); poolPanel.offsetMax = new Vector2(40f + 780f, -110f);
            var poolBg = poolPanel.gameObject.AddComponent<Image>();
            poolBg.color = new Color(0f, 0f, 0f, 0.35f);
            AddFrame(poolPanel, new Color(Emerald.r, Emerald.g, Emerald.b, 0.35f));
            var poolTitle = MakeText("Title", poolPanel, 16f, EmeraldMuted, TextAlignmentOptions.MidlineLeft);
            Place(poolTitle.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -24f), new Vector2(500f, 26f));
            poolTitle.characterSpacing = 8f;
            poolTitle.text = "YOUR PULLS — EVERY COPY IS PLAYABLE";
            poolContent = BuildCardScroll(poolPanel, new Vector2(0f, 10f), new Vector2(0f, -48f));

            // ---- Deck rechts daneben ----
            deckPanel = MakeRect("DeckPanel", builderPanel);
            deckPanel.anchorMin = new Vector2(0f, 0f); deckPanel.anchorMax = new Vector2(0f, 1f);
            deckPanel.pivot = new Vector2(0f, 0.5f);
            deckPanel.offsetMin = new Vector2(840f, 70f); deckPanel.offsetMax = new Vector2(840f + 620f, -110f);
            var deckBg = deckPanel.gameObject.AddComponent<Image>();
            deckBg.color = new Color(0f, 0f, 0f, 0.35f);
            AddFrame(deckPanel, new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.4f));
            deckCountText = MakeText("Count", deckPanel, 16f, Parchment, TextAlignmentOptions.MidlineLeft);
            Place(deckCountText.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -24f), new Vector2(560f, 26f));
            var deckScrollContent = BuildCardScroll(deckPanel, new Vector2(0f, 10f), new Vector2(0f, -48f));
            // Im Deck-Content stapeln zwei Gitter mit Trenner (wie im Deck Builder)
            var vertical = deckScrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 8, 8);
            vertical.spacing = 8f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            var poolGrid = poolContent.gameObject.AddComponent<GridLayoutGroup>();
            poolGrid.cellSize = new Vector2(CollectionCardTile.Width, CollectionCardTile.Height);
            poolGrid.spacing = new Vector2(8f, 10f);
            poolGrid.padding = new RectOffset(12, 12, 8, 8);
            // Feste Spaltenzahl: die Breiten-Automatik rechnet beim ersten
            // Layout-Pass mit Breite 0 und verteilt die Reihe daneben
            poolGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            poolGrid.constraintCount = 5;
            deckMainGrid = BuildDeckGrid("DeckMainGrid", deckScrollContent);
            deckExtraHeader = BuildExtraHeader(deckScrollContent);
            deckExtraGrid = BuildDeckGrid("DeckExtraGrid", deckScrollContent);

            // ---- Aktions-Leiste rechts ----
            var rail = MakeRect("Rail", builderPanel);
            rail.anchorMin = new Vector2(0f, 0f); rail.anchorMax = new Vector2(0f, 1f);
            rail.pivot = new Vector2(0f, 0.5f);
            rail.offsetMin = new Vector2(1480f, 70f); rail.offsetMax = new Vector2(1880f, -110f);

            var heroButton = MakeButton("Hero", rail, "", out heroLabel, 15f);
            Place((RectTransform)heroButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(380f, 56f));
            heroLabel.color = Parchment;
            heroButton.onClick.AddListener(OpenHeroPicker);

            enterButton = MakeButton("Enter", rail, "", out enterLabel, 18f);
            Place((RectTransform)enterButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(380f, 66f));
            enterLabel.fontStyle = FontStyles.Bold;
            enterButton.onClick.AddListener(EnterFloor);

            var saveButton = MakeButton("Save", rail, "SAVE DECK", out var saveLabel);
            Place((RectTransform)saveButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(380f, 48f));
            saveLabel.color = Parchment;
            saveButton.onClick.AddListener(() => SaveDeck(false));

            var abandonButton = MakeButton("Abandon", rail, "ABANDON DRAFT", out abandonLabel);
            Place((RectTransform)abandonButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(380f, 48f));
            abandonLabel.color = new Color32(0xE9, 0xA1, 0x83, 0xFF);
            abandonButton.onClick.AddListener(AbandonDraft);

            var hint = MakeText("Hint", rail, 13f, Ink, TextAlignmentOptions.Top);
            Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(380f, 260f));
            hint.text = "Click a card to inspect it.\nDouble-click or drag to move it between pool and deck.\n\n" +
                        "The deck must hold " + DeckMin + "–" + DeckMax + " cards.\n" +
                        "Reliquaries go to the Extra Deck (max " + ExtraMax + ").\n\n" +
                        "Losing a floor costs nothing — enter again.";
        }

        /// <summary>Scroll-Fläche für Kartengitter innerhalb eines Panels.</summary>
        private RectTransform BuildCardScroll(RectTransform panel, Vector2 offsetMin, Vector2 offsetMax)
        {
            var scrollGo = MakeRect("Scroll", panel);
            scrollGo.anchorMin = Vector2.zero; scrollGo.anchorMax = Vector2.one;
            scrollGo.offsetMin = offsetMin; scrollGo.offsetMax = offsetMax;
            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45f;
            var viewport = MakeRect("Viewport", scrollGo);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            scroll.viewport = viewport;
            var content = MakeRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            // Frische RectTransforms starten mit sizeDelta (100,100) — ohne diese
            // Null stünde der Content 100px breiter als der Viewport und die
            // erste Gitterspalte ragte halb aus dem Panel.
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return content;
        }

        private RectTransform BuildDeckGrid(string name, RectTransform parent)
        {
            var rect = MakeRect(name, parent);
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CollectionCardTile.Width, CollectionCardTile.Height);
            grid.spacing = new Vector2(8f, 10f);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = DeckGridColumns;
            return rect;
        }

        /// <summary>Feste Spaltenzahl der Deck-Gitter (620er-Panel: 4 × 130 + 3 × 8 + Rand).</summary>
        private const int DeckGridColumns = 4;

        private GameObject BuildExtraHeader(RectTransform parent)
        {
            var header = MakeRect("ExtraHeader", parent);
            header.sizeDelta = new Vector2(0f, 30f);
            var label = MakeText("Label", header, 15f, Parchment, TextAlignmentOptions.BottomLeft);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(4f, 0f);
            label.text = "<color=#F1E7D2>◆ EXTRA DECK</color>";
            return header.gameObject;
        }

        // ================== ZUSTÄNDE ==================

        private RuntimeDeck EditedDeck
        {
            get
            {
                if (editedDeck == null)
                {
                    editedDeck = new RuntimeDeck { Name = "Draft Deck", Hero = "" };
                    var saved = PlayerProfile.DraftDeck;
                    if (saved != null)
                    {
                        editedDeck.Hero = saved.Hero;
                        editedDeck.Cards.AddRange(saved.Cards);
                        editedDeck.Extra.AddRange(saved.Extra);
                    }
                    if (string.IsNullOrEmpty(editedDeck.Hero))
                    {
                        var firstHero = catalog != null ? catalog.cards.OfType<PlayerCardData>().FirstOrDefault() : null;
                        editedDeck.Hero = firstHero != null ? firstHero.cardName : "";
                    }
                }
                return editedDeck;
            }
        }

        private void RebuildAll()
        {
            bool active = Online && PlayerProfile.DraftActive;
            if (introPanel != null) introPanel.gameObject.SetActive(!active);
            if (builderPanel != null) builderPanel.gameObject.SetActive(active);
            if (headerText != null)
                headerText.text = active
                    ? $"<color=#BDF0D4>THE DRAFT</color>  <color=#8C7B5F>·</color>  NEXT: FLOOR {PlayerProfile.DraftFloor + 1} OF {DraftFloors}  <color=#8C7B5F>·</color>  <color=#8C7B5F>{PlayerProfile.DraftFloor} SEALED</color>"
                    : "<color=#BDF0D4>THE DRAFT</color>";

            if (!active)
            {
                if (introStatus != null)
                    introStatus.text = !Online ? "REQUIRES AN ACCOUNT — LOG IN FIRST"
                        : PlayerProfile.DraftClears > 0 ? $"CONQUERED {PlayerProfile.DraftClears}×  ·  A NEW DRAFT AWAITS"
                        : "NO DRAFT RUNNING";
                if (drawButton != null) drawButton.interactable = Online && !awaitingStart;
                return;
            }

            RebuildPool();
            RebuildDeck();
            RefreshRail();
        }

        private int CountInDeck(string cardName) =>
            EditedDeck.Cards.Count(n => n == cardName) + EditedDeck.Extra.Count(n => n == cardName);

        private void RebuildPool()
        {
            if (poolContent == null) return;
            var cards = PlayerProfile.DraftPool.Keys
                .Select(name => catalog != null ? catalog.FindByName(name) : null)
                .Where(c => c != null)
                // Die garantierten Stapel (3+ Exemplare) zuerst — sie sind das
                // Rückgrat des Decks und sollen nicht in der Namenswand versinken
                .OrderByDescending(c => PoolCount(c.cardName) >= 3 ? PoolCount(c.cardName) : 0)
                .ThenBy(c => c.Kind).ThenBy(c => c is MonsterCardData m ? m.level : 0).ThenBy(c => c.cardName)
                .ToList();

            int used = 0;
            foreach (var card in cards)
            {
                int pulled = PlayerProfile.DraftPool[card.cardName];
                int inDeck = CountInDeck(card.cardName);
                var tile = used < poolTiles.Count ? poolTiles[used] : CreateTile(poolContent, poolTiles);
                if (tile == null) break;
                used++;
                tile.gameObject.SetActive(true);
                // owned = gezogen, maxCopies = gezogen: die einzige Grenze ist der Pool.
                // Kein Select-Callback: die große Ansicht hängt am Inspect (nur Einzelklick).
                tile.Setup(card, CardFinish.Plain, inDeck, pulled, pulled, inDeck,
                    AddCard, RemoveCard, null, -1, true, false);
            }
            for (int i = used; i < poolTiles.Count; i++)
                if (poolTiles[i] != null) poolTiles[i].gameObject.SetActive(false);
        }

        private void RebuildDeck()
        {
            if (deckMainGrid == null) return;
            var deck = EditedDeck;
            var mainCards = deck.Cards.Distinct()
                .Select(name => catalog != null ? catalog.FindByName(name) : null)
                .Where(c => c != null)
                .OrderBy(c => c.Kind).ThenBy(c => c is MonsterCardData m ? m.level : 0).ThenBy(c => c.cardName)
                .ToList();
            var extraCards = deck.Extra.Distinct()
                .Select(name => catalog != null ? catalog.FindByName(name) : null)
                .Where(c => c != null).OrderBy(c => c.cardName).ToList();

            int used = 0, mainTiles = 0, extraTiles = 0;
            foreach (var card in mainCards)
            {
                var tile = used < deckTiles.Count ? deckTiles[used] : CreateTile(deckMainGrid, deckTiles);
                if (tile == null) break;
                used++;
                PlaceTile(tile, deckMainGrid, mainTiles++);
                int inDeck = deck.Cards.Count(n => n == card.cardName);
                int pulled = PoolCount(card.cardName);
                tile.Setup(card, CardFinish.Plain, inDeck, pulled, pulled, inDeck,
                    AddCard, RemoveCard, null, -1, true, true);
            }
            foreach (var card in extraCards)
            {
                var tile = used < deckTiles.Count ? deckTiles[used] : CreateTile(deckMainGrid, deckTiles);
                if (tile == null) break;
                used++;
                PlaceTile(tile, deckExtraGrid, extraTiles++);
                int inDeck = deck.Extra.Count(n => n == card.cardName);
                int pulled = PoolCount(card.cardName);
                tile.Setup(card, CardFinish.Plain, inDeck, pulled, pulled, inDeck,
                    AddCard, RemoveCard, null, -1, true, true);
            }
            for (int i = used; i < deckTiles.Count; i++)
                if (deckTiles[i] != null) deckTiles[i].gameObject.SetActive(false);

            SizeDeckGrid(deckMainGrid, mainTiles);
            SizeDeckGrid(deckExtraGrid, extraTiles);
            if (deckExtraHeader != null) deckExtraHeader.SetActive(extraTiles > 0);

            int count = deck.Cards.Count;
            bool legal = count >= DeckMin && count <= DeckMax;
            if (deckCountText != null)
                deckCountText.text = $"DRAFT DECK  <color={(legal ? "#7ACD96" : "#E9A183")}>{count}</color>" +
                                     $" <color=#8C7B5F>/ {DeckMin}–{DeckMax}</color>" +
                                     $"   <color=#8C7B5F>EXTRA</color> {deck.Extra.Count} <color=#8C7B5F>/ {ExtraMax}</color>";
        }

        private int PoolCount(string cardName) =>
            PlayerProfile.DraftPool.TryGetValue(cardName, out int count) ? count : 0;

        private CollectionCardTile CreateTile(RectTransform parent, List<CollectionCardTile> list)
        {
            if (cardViewPrefab == null) return null;
            var go = new GameObject("DraftTile", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tile = go.AddComponent<CollectionCardTile>();
            tile.Build(cardViewPrefab, skin);
            tile.SetDropTarget(deckPanel);
            tile.SetInspect(ShowPreview);
            list.Add(tile);
            return tile;
        }

        private static void PlaceTile(CollectionCardTile tile, RectTransform gridParent, int position)
        {
            tile.gameObject.SetActive(true);
            if (tile.transform.parent != gridParent) tile.transform.SetParent(gridParent, false);
            tile.transform.SetSiblingIndex(Mathf.Min(position, gridParent.childCount - 1));
        }

        private void SizeDeckGrid(RectTransform gridRect, int tiles)
        {
            if (gridRect == null) return;
            gridRect.gameObject.SetActive(tiles > 0);
            if (tiles <= 0) return;
            int rows = (tiles + DeckGridColumns - 1) / DeckGridColumns;
            gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                rows * CollectionCardTile.Height + (rows - 1) * 10f);
        }

        private void RefreshRail()
        {
            var deck = EditedDeck;
            if (heroLabel != null)
                heroLabel.text = $"<color=#8C7B5F>HERO</color>  {(string.IsNullOrEmpty(deck.Hero) ? "—" : deck.Hero.ToUpperInvariant())}";
            bool legal = deck.Cards.Count >= DeckMin && deck.Cards.Count <= DeckMax;
            if (enterButton != null)
            {
                enterButton.interactable = legal && !awaitingEnter;
                enterButton.GetComponent<Image>().color = legal ? Emerald : new Color(0f, 0f, 0f, 0.4f);
            }
            if (enterLabel != null)
            {
                enterLabel.text = $"ENTER FLOOR {PlayerProfile.DraftFloor + 1}";
                enterLabel.color = legal ? new Color32(0x0A, 0x1F, 0x14, 0xFF) : new Color32(0x5C, 0x51, 0x3F, 0xFF);
            }
            if (abandonLabel != null && !armedAbandon) abandonLabel.text = "ABANDON DRAFT";
        }

        // ================== AKTIONEN ==================

        private void BeginDraft()
        {
            if (!Online || awaitingStart) return;
            awaitingStart = true;
            if (drawButton != null) drawButton.interactable = false;
            network.SendDraftStart();
            ShowFeedback("Drawing 20 packs…");
        }

        private void AddCard(CardDefinition card, CardFinish finish)
        {
            var deck = EditedDeck;
            bool isReliquary = card is ReliquaryCardData;
            int inDeck = CountInDeck(card.cardName);
            if (inDeck >= PoolCount(card.cardName)) { ShowFeedback("No copies left in your pool."); return; }
            if (isReliquary)
            {
                if (deck.Extra.Count >= ExtraMax) { ShowFeedback($"Extra Deck is full (max {ExtraMax})."); return; }
                deck.Extra.Add(card.cardName);
            }
            else
            {
                if (deck.Cards.Count >= DeckMax) { ShowFeedback($"Deck is full (max {DeckMax})."); return; }
                deck.Cards.Add(card.cardName);
            }
            RebuildAll();
        }

        private void RemoveCard(CardDefinition card, CardFinish finish)
        {
            var deck = EditedDeck;
            var list = card is ReliquaryCardData ? deck.Extra : deck.Cards;
            int index = list.LastIndexOf(card.cardName);
            if (index >= 0) list.RemoveAt(index);
            RebuildAll();
        }

        /// <summary>Speichert die Arbeitskopie auf dem Server; optional geht es danach ins Duell.</summary>
        private void SaveDeck(bool thenEnter)
        {
            if (!Online) { ShowFeedback("Not connected."); return; }
            var deck = EditedDeck;
            awaitingEnter = thenEnter;
            network.SendDraftSaveDeck(deck);
            if (!thenEnter) ShowFeedback("Draft deck saved.");
        }

        private void EnterFloor()
        {
            var deck = EditedDeck;
            if (deck.Cards.Count < DeckMin || deck.Cards.Count > DeckMax)
            {
                ShowFeedback($"A legal deck needs {DeckMin}–{DeckMax} cards.");
                return;
            }
            SaveDeck(true);
        }

        private void LaunchDraftDuel()
        {
            var floors = tower != null ? tower.floors : null;
            int nextFloor = PlayerProfile.DraftFloor + 1;
            var floor = floors != null && nextFloor >= 1 && nextFloor <= floors.Count ? floors[nextFloor - 1] : null;
            if (floor == null || floor.opponent == null)
            {
                ShowFeedback("The Tower is missing — try again from the Challenges tab.");
                RebuildAll();
                return;
            }
            var deck = PlayerProfile.DraftDeck;
            if (deck == null) { RebuildAll(); return; }

            MatchContext.Clear();
            MatchContext.UseCustomLocalDeck = true;
            MatchContext.DraftRun = true;
            MatchContext.TowerFloor = nextFloor;
            MatchContext.LocalDeckCards = new List<string>(deck.Cards);
            MatchContext.LocalExtraCards = new List<string>(deck.Extra);
            MatchContext.LocalDeckFinishes = deck.DeckFinishNumbers();
            MatchContext.LocalExtraFinishes = deck.ExtraFinishNumbers();
            MatchContext.LocalHero = deck.Hero;
            MatchContext.LocalName = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Duelist";
            DuelSetupController.FillBotContext(floor.opponent, floor.keeperName, floor.lifePointsOverride, floor.bonusManaPerTurn);
            SceneManager.LoadScene(duelSceneName);
        }

        private void AbandonDraft()
        {
            if (!Online) return;
            // Zwei Klicks: der erste stellt scharf, der zweite wirft wirklich weg
            if (!armedAbandon)
            {
                armedAbandon = true;
                if (abandonLabel != null) abandonLabel.text = "REALLY? CLICK AGAIN";
                return;
            }
            armedAbandon = false;
            editedDeck = null;
            network.SendDraftAbandon();
            ShowFeedback("Draft abandoned.");
        }

        // ================== HELDEN-WÄHLER & VORSCHAU ==================

        /// <summary>Im Draft ist jeder Held erlaubt — das Deck ist geliehen.</summary>
        private void OpenHeroPicker()
        {
            if (heroPicker != null) { heroPicker.SetActive(true); heroPicker.transform.SetAsLastSibling(); return; }
            if (catalog == null || cardViewPrefab == null) return;

            heroPicker = MakeOverlay("HeroPicker", out var overlayRoot, () => heroPicker.SetActive(false));
            var grid = MakeRect("Grid", overlayRoot);
            grid.anchorMin = new Vector2(0.5f, 0.5f); grid.anchorMax = new Vector2(0.5f, 0.5f);
            grid.sizeDelta = new Vector2(1500f, 860f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(230f, 322f);
            layout.spacing = new Vector2(14f, 14f);
            layout.childAlignment = TextAnchor.MiddleCenter;

            foreach (var hero in catalog.cards.OfType<PlayerCardData>().OrderBy(h => h.cardName))
            {
                var cell = MakeRect("Hero_" + hero.cardName, grid);
                var view = Instantiate(cardViewPrefab, cell);
                var viewRect = (RectTransform)view.transform;
                viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
                viewRect.pivot = new Vector2(0.5f, 0.5f);
                viewRect.sizeDelta = new Vector2(230f, 322f);
                view.Show(new CardInstance(hero, null), false, true);
                var chosen = hero;
                var clickRect = MakeRect("Click", cell);
                Stretch(clickRect);
                var clickImg = clickRect.gameObject.AddComponent<Image>();
                clickImg.color = Color.clear;
                var button = clickRect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() =>
                {
                    SfxManager.Click();
                    EditedDeck.Hero = chosen.cardName;
                    heroPicker.SetActive(false);
                    RefreshRail();
                });
            }
        }

        private void ShowPreview(CardDefinition card, CardFinish finish)
        {
            if (card == null || cardViewPrefab == null) return;
            if (previewOverlay != null) Destroy(previewOverlay);
            previewOverlay = MakeOverlay("Preview", out var overlayRoot, () => Destroy(previewOverlay));
            var view = Instantiate(cardViewPrefab, overlayRoot);
            var viewRect = (RectTransform)view.transform;
            viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.sizeDelta = new Vector2(460f, 644f);
            view.Show(new CardInstance(card, null), false, true);
        }

        // ================== UI-BAUKASTEN ==================

        private GameObject MakeOverlay(string name, out RectTransform overlayRoot, System.Action onScrimClick)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            overlay.transform.SetParent(root, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 400;
            overlayRoot = (RectTransform)overlay.transform;
            Stretch(overlayRoot);
            var scrim = MakeImage("Scrim", overlayRoot, new Color(0f, 0f, 0f, 0.88f));
            Stretch(scrim.rectTransform);
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(() => onScrimClick?.Invoke());
            return overlay;
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message ?? "";
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 4f);
        }

        private void ClearFeedback()
        {
            if (feedbackText != null) feedbackText.text = "";
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var rect = MakeRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private TMP_Text MakeText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var rect = MakeRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private Button MakeButton(string name, Transform parent, string label, out TMP_Text labelText, float fontSize = 15f)
        {
            var rect = MakeRect(name, parent);
            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            AddFrame(rect, new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.5f));
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            labelText = MakeText("Label", rect, fontSize, Parchment, TextAlignmentOptions.Midline);
            Stretch(labelText.rectTransform);
            labelText.text = label;
            labelText.characterSpacing = 4f;
            return button;
        }

        private void AddFrame(RectTransform parent, Color color)
        {
            var frame = MakeImage("Frame", parent, color);
            Stretch(frame.rectTransform);
            if (skin != null && skin.whiteFrame != null)
            {
                frame.sprite = skin.whiteFrame;
                frame.type = Image.Type.Sliced;
            }
            frame.raycastTarget = false;
        }
    }
}
