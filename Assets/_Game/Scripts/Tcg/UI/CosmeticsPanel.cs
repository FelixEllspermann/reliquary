using System.Collections;
using System.Collections.Generic;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Der Kosmetik-Laden (Handoff „Progression", Abschnitt 4): ein Raster aus
    /// zwanzig Kacheln mit Detailspalte rechts.
    ///
    /// Bezahlt wird ausschliesslich mit Coins. Der Unterschied zwischen schlicht
    /// und auffällig steckt im Preis: die seltenen Stücke kosten ein Vielfaches.
    ///
    /// Kaufen und Ausrüsten entscheidet der Server; hier wird nur angezeigt und
    /// gesendet.
    /// </summary>
    public class CosmeticsPanel : MonoBehaviour
    {
        private const float PanelWidth = 1320f;
        private const float PanelHeight = 760f;
        private const float DetailWidth = 380f;

        /// <summary>Der Titel, den jeder Early-Access-Spieler mitbringt (steht in keinem Ladenfach).</summary>
        private const string StarterTitleId = "early_vault_hunter";

        private static CosmeticsPanel instance;

        private CanvasGroup group;
        private RectTransform panel;
        private RectTransform grid;
        private TransitionSkin skin;
        private Coroutine animRoutine;

        private readonly List<Tile> tiles = new List<Tile>();
        private CosmeticItem selected;

        private TMP_Text coinsText, feedbackText;
        private RectTransform detail;

        private class Tile
        {
            public CosmeticItem Item;
            public RectTransform Root;
            public Image Frame;
            public Image Swatch;
            public TMP_Text Equipped;
        }

        public static void Open()
        {
            if (instance == null)
            {
                var host = new GameObject("~Cosmetics");
                instance = host.AddComponent<CosmeticsPanel>();
                instance.Build();
            }
            instance.Refresh();
            instance.Show(true);
        }

        public static void Close() => instance?.Show(false);

        private void OnEnable()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.OnMessage += HandleMessage;
        }

        private void OnDisable()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.OnMessage -= HandleMessage;
        }

        private void HandleMessage(NetMessage message)
        {
            if (message.t == "profile") { Refresh(); return; }
            if (message.t == "error") SetFeedback(message.msg, false);
        }

        // ================== AUFBAU ==================

        private void Build()
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var canvasRect = (RectTransform)canvasGo.transform;

            // Heisst „Scrim", damit der UiFxInstaller ihn in Ruhe lässt
            var scrim = MakeImage("Scrim", canvasRect, new Color(0f, 0f, 0f, 0.74f));
            Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(() => Show(false));

            panel = MakeRect("Panel", canvasRect);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var bg = MakeImage("BG", panel, Hex("#0E121B", 0.98f));
            // Fängt Klicks auf tote Flächen im Fenster ab — nur der Scrim daneben schließt.
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);
            var frame = MakeImage("Frame", panel, Hex("#C8A45C", 1f));
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);
            var inner = MakeImage("InnerFrame", panel, Hex("#C8A45C", 0.27f));
            inner.sprite = skin.frame; inner.type = Image.Type.Sliced;
            Stretch(inner.rectTransform, 8f);

            var diamond = MakeImage("TopDiamond", panel, Hex("#EBCE8A", 1f));
            diamond.sprite = skin.square;
            diamond.rectTransform.sizeDelta = new Vector2(12f, 12f);
            diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            diamond.rectTransform.anchoredPosition = Vector2.zero;

            BuildHeader();
            BuildGrid();
            BuildDetail();
            BuildCloseButton();

            gameObject.SetActive(false);
        }

        private void BuildHeader()
        {
            // Links am Panelrand ausrichten, aber hinter dem Zurück-Knopf beginnen —
            // der belegt die ersten 78 px.
            var title = MakeText("Title", panel, skin.cinzel, 30f, Hex("#EBCE8A", 1f));
            title.text = "Cosmetics";
            title.alignment = TextAlignmentOptions.Left;
            LeftStrip((RectTransform)title.transform, 96f, 420f, 38f, PanelHeight * 0.5f - 46f);

            var sub = MakeText("Sub", panel, skin.oswald, 11f, Hex("#8C7B5F", 1f));
            sub.text = "NOTHING HERE TOUCHES GAMEPLAY";
            sub.characterSpacing = 22f;
            sub.alignment = TextAlignmentOptions.Left;
            LeftStrip((RectTransform)sub.transform, 98f, 500f, 16f, PanelHeight * 0.5f - 74f);

            coinsText = MakePill(panel, PanelWidth * 0.5f - 130f, "#EBCE8A");
        }

        private TMP_Text MakePill(RectTransform parent, float x, string hex)
        {
            var pill = MakeImage("Pill", parent, new Color(0f, 0f, 0f, 0.4f));
            pill.sprite = skin.frame; pill.type = Image.Type.Sliced;
            pill.rectTransform.sizeDelta = new Vector2(150f, 34f);
            pill.rectTransform.anchoredPosition = new Vector2(x, PanelHeight * 0.5f - 52f);
            var border = MakeImage("Frame", pill.rectTransform, Hex(hex, 0.4f));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            var gem = MakeImage("Gem", pill.rectTransform, Hex(hex, 1f));
            gem.sprite = skin.square;
            gem.rectTransform.sizeDelta = new Vector2(9f, 9f);
            gem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            gem.rectTransform.anchorMin = gem.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            gem.rectTransform.anchoredPosition = new Vector2(18f, 0f);
            var text = MakeText("Value", pill.rectTransform, skin.cinzel, 17f, Hex(hex, 1f));
            text.alignment = TextAlignmentOptions.Right;
            var rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(30f, 0f); rect.offsetMax = new Vector2(-16f, 0f);
            return text;
        }

        // ---- Filter ----
        // Fächer in der Reihenfolge, in der sie im Laden stehen. Leerer Schlüssel
        // heisst „alles".
        private static readonly (string Slot, string Label)[] Filters =
        {
            ("", "ALL"),
            ("sleeve", "BACKS"),
            ("duelMat", "MATS"),
            ("tossCoin", "COINS"),
            ("avatarFrame", "FRAMES"),
            ("avatar", "PORTRAITS"),
            ("victorySeal", "SEALS"),
            ("title", "TITLES"),
        };

        private string activeFilter = "";
        private readonly List<Image> filterPlates = new List<Image>();
        private readonly List<TMP_Text> filterLabels = new List<TMP_Text>();
        private ScrollRect gridScroll;
        private float cellWidth, cellHeight, gridWidth;

        private const int GridColumns = 4;
        private const float GridGap = 12f;

        private void BuildGrid()
        {
            gridWidth = PanelWidth - DetailWidth - 120f;
            float viewHeight = PanelHeight - 250f;   // Platz für die Filterzeile darüber

            BuildFilterRow(viewHeight);

            // Der Laden hat mehr Gegenstände als auf eine Seite passen — vorher
            // wurden schlicht die ersten zwanzig gezeichnet und der Rest fiel
            // stillschweigend hinten runter.
            var view = MakeRect("GridView", panel);
            view.sizeDelta = new Vector2(gridWidth, viewHeight);
            view.anchorMin = view.anchorMax = new Vector2(0f, 0.5f);
            view.pivot = new Vector2(0f, 0.5f);
            view.anchoredPosition = new Vector2(46f, -52f);

            gridScroll = view.gameObject.AddComponent<ScrollRect>();
            gridScroll.horizontal = false;
            gridScroll.vertical = true;
            gridScroll.movementType = ScrollRect.MovementType.Clamped;
            gridScroll.scrollSensitivity = 42f;

            var viewport = MakeRect("Viewport", view);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            gridScroll.viewport = viewport;

            // Ohne eine Fläche, die Zeigerereignisse annimmt, geht das Mausrad am
            // Scrollbereich vorbei an den Panel-Hintergrund — und der liegt daneben,
            // nicht darin.
            var catcher = MakeImage("Catcher", viewport, new Color(0f, 0f, 0f, 0f));
            Stretch(catcher.rectTransform);
            catcher.raycastTarget = true;

            grid = MakeRect("Grid", viewport);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            gridScroll.content = grid;

            cellWidth = (gridWidth - (GridColumns - 1) * GridGap) / GridColumns;
            // ~2,6 Reihen sichtbar: fast alle zusätzliche Höhe geht in die Kunstfläche
            // (die Textzeilen darunter sind fix 96px hoch), der Anschnitt zeigt „hier
            // geht es weiter".
            cellHeight = (viewHeight - 2f * GridGap) / 2.6f;

            foreach (var item in Cosmetics.Catalog)
                tiles.Add(BuildTile(item, 0f, 0f, cellWidth, cellHeight));

            BuildGridScrollbar(view, viewport);
            Relayout();
        }

        private void BuildFilterRow(float viewHeight)
        {
            var row = MakeRect("Filters", panel);
            row.sizeDelta = new Vector2(gridWidth, 28f);
            row.anchorMin = row.anchorMax = new Vector2(0f, 0.5f);
            row.pivot = new Vector2(0f, 0.5f);
            row.anchoredPosition = new Vector2(46f, viewHeight * 0.5f - 52f + 30f);

            float x = -gridWidth * 0.5f;
            for (int i = 0; i < Filters.Length; i++)
            {
                var filter = Filters[i];
                float width = 22f + filter.Label.Length * 11f;
                var plate = MakeImage("Filter_" + i, row, Hex("#C8A45C", 0.10f));
                plate.raycastTarget = true;   // sonst fällt der Klick durch auf den Scrim
                plate.rectTransform.sizeDelta = new Vector2(width, 26f);
                plate.rectTransform.anchoredPosition = new Vector2(x + width * 0.5f, 0f);
                if (skin != null && skin.frame != null) { plate.sprite = skin.frame; plate.type = Image.Type.Sliced; }

                var label = MakeText("Label", plate.rectTransform, skin != null ? skin.oswald : null, 12f, Hex("#8C7B5F", 1f));
                label.alignment = TextAlignmentOptions.Center;
                label.characterSpacing = 12f;
                label.text = filter.Label;
                Stretch(label.rectTransform);

                string slot = filter.Slot;
                var button = plate.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { activeFilter = slot; Relayout(); Refresh(); });

                filterPlates.Add(plate);
                filterLabels.Add(label);
                x += width + 8f;
            }
        }

        private void BuildGridScrollbar(RectTransform view, RectTransform viewport)
        {
            const float width = 5f;
            var barRect = MakeRect("Scrollbar", view);
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.offsetMin = new Vector2(-width, 0f);
            barRect.offsetMax = Vector2.zero;

            var track = MakeImage("Track", barRect, Hex("#C8A45C", 0.14f));
            Stretch(track.rectTransform);

            var bar = barRect.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.transition = Selectable.Transition.None;

            var slidingArea = MakeRect("SlidingArea", barRect);
            Stretch(slidingArea);
            var handle = MakeImage("Handle", slidingArea, Hex("#C8A45C", 0.72f));
            handle.rectTransform.offsetMin = Vector2.zero;
            handle.rectTransform.offsetMax = Vector2.zero;
            bar.targetGraphic = handle;
            bar.handleRect = handle.rectTransform;

            viewport.offsetMax = new Vector2(-(width + 10f), viewport.offsetMax.y);
            gridScroll.verticalScrollbar = bar;
            gridScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        /// <summary>
        /// Setzt die sichtbaren Kacheln neu und misst den Inhalt nach. Gefiltert
        /// wird durch Ein- und Ausblenden, nicht durch Neubauen — so bleibt die
        /// Auswahl erhalten und es ruckelt nicht.
        /// </summary>
        private void Relayout()
        {
            int visible = 0;
            foreach (var tile in tiles)
            {
                bool show = activeFilter.Length == 0 || tile.Item.Slot == activeFilter;
                tile.Root.gameObject.SetActive(show);
                if (!show) continue;
                int column = visible % GridColumns, row = visible / GridColumns;
                tile.Root.anchoredPosition = new Vector2(
                    -gridWidth * 0.5f + cellWidth * 0.5f + column * (cellWidth + GridGap),
                    -cellHeight * 0.5f - row * (cellHeight + GridGap));
                visible++;
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(visible / (float)GridColumns));
            grid.sizeDelta = new Vector2(0f, rows * cellHeight + (rows - 1) * GridGap);
            if (gridScroll != null) gridScroll.verticalNormalizedPosition = 1f;

            for (int i = 0; i < filterPlates.Count; i++)
            {
                bool on = Filters[i].Slot == activeFilter;
                filterPlates[i].color = Hex("#C8A45C", on ? 0.34f : 0.10f);
                filterLabels[i].color = Hex(on ? "#F3DDA4" : "#8C7B5F", 1f);
            }
        }

        private Tile BuildTile(CosmeticItem item, float x, float y, float width, float height)
        {
            var root = MakeRect("Tile_" + item.Id, grid);
            // Am oberen Rand des Rasters verankern: der Inhalt wächst nach unten,
            // und Relayout rechnet von oben.
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(width, height);
            root.anchoredPosition = new Vector2(x, y);

            var bg = MakeImage("BG", root, new Color(0f, 0f, 0f, 0.34f));
            bg.sprite = skin.frame; bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);

            var frame = MakeImage("Frame", root, item.Accent);
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);

            // Feste Abstände von der Oberkante statt Anteilen: nur so bleibt die
            // Besitzmarke zuverlässig über dem Farbfeld, auch wenn sich die
            // Kachelhöhe ändert.
            float top = height * 0.5f;
            float swatchHeight = Mathf.Max(28f, height - 96f);

            var equipped = MakeText("Equipped", root, skin.oswald, 9f, Hex("#7ACD96", 1f));
            equipped.characterSpacing = 18f;
            equipped.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)equipped.transform, width - 12f, 12f, top - 12f);

            // Der Farbfleck steht für den Gegenstand — echte Vorschauen kommen,
            // wenn es die Assets gibt.
            var swatch = MakeImage("Swatch", root, item.Accent);
            swatch.sprite = skin.diagFade;
            ShowArt(swatch, item, item.Accent);
            swatch.rectTransform.sizeDelta = new Vector2(width - 40f, swatchHeight);
            swatch.rectTransform.anchoredPosition = new Vector2(0f, top - 22f - swatchHeight * 0.5f);
            var swatchFrame = MakeImage("SwatchFrame", swatch.rectTransform, new Color(item.Accent.r, item.Accent.g, item.Accent.b, 0.6f));
            swatchFrame.sprite = skin.frame; swatchFrame.type = Image.Type.Sliced;
            Stretch(swatchFrame.rectTransform);

            float below = top - 22f - swatchHeight;   // Unterkante des Farbfelds

            var name = MakeText("Name", root, skin.cinzel, 17f, Hex("#F1DFB8", 1f));
            name.text = item.Name;
            name.alignment = TextAlignmentOptions.Center;
            name.enableAutoSizing = true; name.fontSizeMin = 11f; name.fontSizeMax = 17f;
            Strip((RectTransform)name.transform, width - 16f, 20f, below - 16f);

            var slot = MakeText("Slot", root, skin.oswald, 9f, Hex("#8C7B5F", 1f));
            slot.text = Cosmetics.SlotName(item.Slot).ToUpperInvariant();
            slot.characterSpacing = 20f;
            slot.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)slot.transform, width - 12f, 14f, below - 36f);

            var price = MakeText("Price", root, skin.spectral, 13f, Hex("#A2917A", 1f));
            price.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)price.transform, width - 12f, 18f, below - 56f);

            var button = bg.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var captured = item;
            button.onClick.AddListener(() => { SfxManager.Click(); SelectItem(captured); });

            return new Tile { Item = item, Root = root, Frame = frame, Swatch = swatch, Equipped = equipped };
        }

        // ---- Detailspalte ----
        private TMP_Text detailName, detailSlot, detailPrice, detailNote, slotStateLabel, slotStateValue;
        private Image detailSwatch, detailFrame;
        private Button actionButton;
        private TMP_Text actionLabel;
        private Image actionBg, actionBorder;

        private void BuildDetail()
        {
            detail = MakeRect("Detail", panel);
            detail.sizeDelta = new Vector2(DetailWidth, PanelHeight - 140f);
            detail.anchorMin = detail.anchorMax = new Vector2(1f, 0.5f);
            detail.pivot = new Vector2(1f, 0.5f);
            detail.anchoredPosition = new Vector2(-46f, -14f);

            var bg = MakeImage("BG", detail, new Color(0f, 0f, 0f, 0.28f));
            bg.sprite = skin.frame; bg.type = Image.Type.Sliced;
            Stretch(bg.rectTransform);
            detailFrame = MakeImage("Frame", detail, Hex("#C8A45C", 0.4f));
            detailFrame.sprite = skin.frame; detailFrame.type = Image.Type.Sliced;
            Stretch(detailFrame.rectTransform);

            float top = detail.sizeDelta.y * 0.5f;

            detailSwatch = MakeImage("Swatch", detail, Hex("#C8A45C", 1f));
            detailSwatch.sprite = skin.diagFade;
            detailSwatch.rectTransform.sizeDelta = new Vector2(DetailWidth - 80f, 150f);
            detailSwatch.rectTransform.anchoredPosition = new Vector2(0f, top - 110f);

            detailName = MakeText("Name", detail, skin.cinzel, 24f, Hex("#F1DFB8", 1f));
            detailName.alignment = TextAlignmentOptions.Center;
            detailName.enableAutoSizing = true; detailName.fontSizeMin = 15f; detailName.fontSizeMax = 24f;
            Strip((RectTransform)detailName.transform, DetailWidth - 40f, 32f, top - 216f);

            detailSlot = MakeText("Slot", detail, skin.oswald, 10f, Hex("#8C7B5F", 1f));
            detailSlot.characterSpacing = 24f;
            detailSlot.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)detailSlot.transform, DetailWidth - 40f, 16f, top - 244f);

            detailPrice = MakeText("Price", detail, skin.cinzel, 22f, Hex("#EBCE8A", 1f));
            detailPrice.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)detailPrice.transform, DetailWidth - 40f, 28f, top - 292f);

            detailNote = MakeText("Note", detail, skin.spectral, 14f, Hex("#A2917A", 1f));
            detailNote.alignment = TextAlignmentOptions.Center;
            detailNote.textWrappingMode = TextWrappingModes.Normal;
            Strip((RectTransform)detailNote.transform, DetailWidth - 56f, 60f, top - 348f);

            // Was gerade in diesem Fach steckt — beim Aussuchen die wichtigste Zeile,
            // weil Ausrüsten das Vorherige ersetzt
            var divider = MakeImage("Divider", detail, Hex("#C8A45C", 0.22f));
            divider.rectTransform.sizeDelta = new Vector2(DetailWidth - 100f, 1f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, top - 424f);

            slotStateLabel = MakeText("SlotStateLabel", detail, skin.oswald, 9f, Hex("#8C7B5F", 1f));
            slotStateLabel.characterSpacing = 24f;
            slotStateLabel.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)slotStateLabel.transform, DetailWidth - 40f, 14f, top - 452f);

            slotStateValue = MakeText("SlotStateValue", detail, skin.spectral, 15f, Hex("#CFC3AC", 1f));
            slotStateValue.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)slotStateValue.transform, DetailWidth - 40f, 22f, top - 478f);

            actionBg = MakeImage("Action", detail, new Color(0f, 0f, 0f, 0.45f));
            actionBg.sprite = skin.frame; actionBg.type = Image.Type.Sliced;
            actionBg.raycastTarget = true;
            actionBg.rectTransform.sizeDelta = new Vector2(DetailWidth - 80f, 44f);
            actionBg.rectTransform.anchoredPosition = new Vector2(0f, -detail.sizeDelta.y * 0.5f + 92f);
            actionBorder = MakeImage("Frame", actionBg.rectTransform, Hex("#C8A45C", 0.7f));
            actionBorder.sprite = skin.frame; actionBorder.type = Image.Type.Sliced;
            Stretch(actionBorder.rectTransform);
            actionLabel = MakeText("Label", actionBg.rectTransform, skin.oswald, 13f, Hex("#EBCE8A", 1f));
            actionLabel.characterSpacing = 22f;
            actionLabel.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)actionLabel.transform);
            actionButton = actionBg.gameObject.AddComponent<Button>();
            actionButton.transition = Selectable.Transition.None;
            actionButton.onClick.AddListener(Act);

            feedbackText = MakeText("Feedback", detail, skin.spectral, 13f, Hex("#A2917A", 1f));
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.textWrappingMode = TextWrappingModes.Normal;
            Strip((RectTransform)feedbackText.transform, DetailWidth - 48f, 40f, -detail.sizeDelta.y * 0.5f + 44f);
        }

        private void BuildCloseButton()
        {
            var close = MakeImage("CloseButton", panel, new Color(0f, 0f, 0f, 0.45f));
            close.sprite = skin.frame; close.type = Image.Type.Sliced;
            close.raycastTarget = true;
            close.rectTransform.anchorMin = close.rectTransform.anchorMax = new Vector2(0f, 1f);
            close.rectTransform.pivot = new Vector2(0f, 1f);
            close.rectTransform.sizeDelta = new Vector2(52f, 34f);
            close.rectTransform.anchoredPosition = new Vector2(26f, -26f);
            var border = MakeImage("Frame", close.rectTransform, Hex("#C8A45C", 0.7f));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            var button = close.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Show(false));
            var label = MakeText("Label", close.rectTransform, skin.oswald, 15f, Hex("#EBCE8A", 1f));
            label.text = "◀";
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform);
        }

        // ================== INHALT ==================

        private void Refresh()
        {
            coinsText.text = PlayerProfile.Coins.ToString();

            foreach (var tile in tiles)
            {
                bool owned = Cosmetics.Owns(tile.Item.Id);
                bool equipped = Cosmetics.EquippedIn(tile.Item.Slot) == tile.Item.Id;
                tile.Equipped.text = equipped ? "EQUIPPED" : owned ? "OWNED" : "";
                tile.Equipped.color = equipped ? Hex("#7ACD96", 1f) : Hex("#8C7B5F", 1f);

                var price = tile.Root.Find("Price")?.GetComponent<TMP_Text>();
                if (price != null)
                {
                    price.text = owned ? "—"
                        : tile.Item.ForSale ? $"{tile.Item.Price} coins"
                        : "not for sale";
                    price.color = owned ? Hex("#5C513F", 1f)
                        : tile.Item.ForSale && !Cosmetics.CanAfford(tile.Item) ? Hex("#C05A44", 1f)
                        : Hex("#A2917A", 1f);
                }

                bool isSelected = selected != null && selected.Id == tile.Item.Id;
                var accent = tile.Item.Accent;
                tile.Frame.color = isSelected ? accent : new Color(accent.r, accent.g, accent.b, owned ? 0.55f : 0.32f);
                tile.Swatch.color = new Color(accent.r, accent.g, accent.b, owned ? 1f : 0.65f);
            }

            if (selected == null && Cosmetics.Catalog.Count > 0) selected = Cosmetics.Catalog[0];
            RefreshDetail();
        }

        private void SelectItem(CosmeticItem item)
        {
            selected = item;
            SetFeedback("", true);
            Refresh();
        }

        /// <summary>
        /// Zeigt den Gegenstand als das, was er ist — Kartenrücken, Matte, Münze,
        /// Rahmen. Ein Laden aus 30 Farbfeldern sagt nichts; einer aus 30 Formen
        /// lässt sich überfliegen. Wo es noch keine Grafik gibt (Titel, Siegel),
        /// bleibt das Icon, und zur Not das Farbfeld.
        /// </summary>
        private void ShowArt(Image target, CosmeticItem item, Color accent)
        {
            if (target == null || item == null) return;
            Sprite art = null;
            switch (item.Slot)
            {
                case "sleeve": art = CosmeticArt.CardBack(item.Id); break;
                case "duelMat": art = CosmeticArt.Mat(item.Id); break;
                case "tossCoin": art = CosmeticArt.CoinRelic(item.Id); break;
                case "avatarFrame": art = CosmeticArt.Frame(item.Id); break;
                case "avatar": art = CosmeticArt.Avatar(item.Id); break;
            }
            if (art == null) art = CosmeticArt.Icon(item.Id);
            if (art == null) { target.color = accent; return; }
            target.sprite = art;
            target.preserveAspect = true;
            target.color = Color.white;
        }

        private void RefreshDetail()
        {
            if (selected == null) return;
            var accent = selected.Accent;
            ShowArt(detailSwatch, selected, accent);
            detailFrame.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            detailName.text = selected.Name;
            detailSlot.text = Cosmetics.SlotName(selected.Slot).ToUpperInvariant()
                + "  ·  " + selected.Rarity.ToUpperInvariant();

            bool owned = Cosmetics.Owns(selected.Id);
            bool equipped = Cosmetics.EquippedIn(selected.Slot) == selected.Id;

            detailPrice.text = owned ? "In your vault"
                : selected.ForSale ? $"{selected.Price} coins"
                : "Not for sale";
            detailPrice.color = Hex("#EBCE8A", 1f);

            detailNote.text = !owned && !selected.ForSale ? selected.Unlock
                : !owned && !Cosmetics.CanAfford(selected)
                    ? "Dismantling a special finish pays in coins, too."
                    : "";

            slotStateLabel.text = "IN THIS SLOT";
            var inSlot = Cosmetics.Find(Cosmetics.EquippedIn(selected.Slot));
            bool starterTitle = selected.Slot == "title" && Cosmetics.EquippedIn("title") == StarterTitleId;
            slotStateValue.text = inSlot != null ? inSlot.Name
                : starterTitle ? "Early Vault Hunter"
                : "Nothing equipped";
            slotStateValue.color = inSlot != null || starterTitle ? Hex("#CFC3AC", 1f) : Hex("#6A6152", 1f);

            // Ein Knopf, drei Zustände
            if (equipped) { actionLabel.text = "UNEQUIP"; actionButton.interactable = true; }
            else if (owned) { actionLabel.text = "EQUIP"; actionButton.interactable = true; }
            else if (!selected.ForSale) { actionLabel.text = "EARNED ONLY"; actionButton.interactable = false; }
            else { actionLabel.text = "BUY"; actionButton.interactable = Cosmetics.CanAfford(selected); }

            var tint = actionButton.interactable ? Hex("#C8A45C", 0.7f) : Hex("#5C513F", 0.5f);
            actionBorder.color = tint;
            actionLabel.color = actionButton.interactable ? Hex("#EBCE8A", 1f) : Hex("#5C513F", 1f);
        }

        private void Act()
        {
            if (selected == null) return;
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) { SetFeedback("Not connected.", false); return; }

            if (Cosmetics.EquippedIn(selected.Slot) == selected.Id)
            {
                net.SendEquipCosmetic(selected.Slot, "");
                SetFeedback($"{selected.Name} unequipped.", true);
            }
            else if (Cosmetics.Owns(selected.Id))
            {
                net.SendEquipCosmetic(selected.Slot, selected.Id);
                SetFeedback($"{selected.Name} equipped.", true);
            }
            else
            {
                net.SendBuyCosmetic(selected.Id);
                SetFeedback($"Buying {selected.Name}…", true);
            }
        }

        private void SetFeedback(string text, bool good)
        {
            if (feedbackText == null) return;
            feedbackText.text = text ?? "";
            feedbackText.color = good ? Hex("#A2917A", 1f) : Hex("#C05A44", 1f);
        }

        // ================== EIN-/AUSBLENDEN ==================

        private void Show(bool visible)
        {
            gameObject.SetActive(true);
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Fade(visible));
        }

        private IEnumerator Fade(bool visible)
        {
            group.blocksRaycasts = visible;
            float from = group.alpha, to = visible ? 1f : 0f;
            const float duration = 0.16f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                group.alpha = Mathf.Lerp(from, to, k);
                float scale = visible ? Mathf.Lerp(0.97f, 1f, k) : Mathf.Lerp(1f, 0.98f, k);
                panel.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            group.alpha = to;
            panel.localScale = Vector3.one;
            if (!visible) gameObject.SetActive(false);
            animRoutine = null;
        }

        // ---- Bau-Helfer ----

        private static Color Hex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            return color;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Wie <see cref="Strip"/>, aber am linken Panelrand verankert.</summary>
        private static void LeftStrip(RectTransform rect, float inset, float width, float height, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(inset, y);
        }

        private static void Strip(RectTransform rect, float width, float height, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image MakeImage(string name, RectTransform parent, Color color)
        {
            var image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
