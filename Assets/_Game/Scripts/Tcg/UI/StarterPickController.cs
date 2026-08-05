using System.Collections.Generic;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die einmalige Startdeck-Wahl, zwischen Login und Hauptmenü.
    ///
    /// Die Oberfläche entsteht zur Laufzeit: fünf Decks mit je rund 47 Karten
    /// wären als Szenen-Hierarchie 235 vorgebaute Zeilen, von denen immer nur
    /// ein Fünftel sichtbar ist. Verdrahtet wird nur die Wurzel.
    ///
    /// Der Server vergibt das Deck, nicht dieser Bildschirm — hier wird nur
    /// gewählt. Fällt die Verbindung mitten in der Auswahl weg, steht sie beim
    /// nächsten Start wieder da.
    /// </summary>
    public class StarterPickController : MonoBehaviour
    {
        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;
        [SerializeField] private TcgCardView cardViewPrefab;
        [Tooltip("Hintergrund, Rahmen und Knopf-Verlauf — dieselben wie in Login und Hauptmenü")]
        [SerializeField] private CardSkin cardSkin;

        [Header("Szenen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        // ---- Farben, an den Shell-Screens ausgerichtet ----
        private static readonly Color Ink = Hex("#F3DDA4");
        private static readonly Color InkDim = Hex("#8C7B5F");
        private static readonly Color Gold = Hex("#C8A45C");
        private static readonly Color PanelBg = new Color(0f, 0f, 0f, 0.42f);
        private static readonly Color TileBg = new Color(0f, 0f, 0f, 0.34f);

        private NetworkManager network;
        private TransitionSkin skin;

        private readonly List<NetStarterDeck> decks = new List<NetStarterDeck>();
        private readonly List<Image> tileFrames = new List<Image>();
        private readonly List<Image> tileBackgrounds = new List<Image>();
        private int selected = -1;
        private bool claiming;

        private RectTransform cardGrid;
        private TMP_Text titleText, archText, descText, countText, statusText;
        private TcgCardView preview;
        private TMP_Text previewRules;
        private Button confirmButton;
        private TMP_Text confirmLabel;

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private void Start()
        {
            network = NetworkManager.Instance;
            skin = TransitionSkin.Load();

            decks.AddRange(PlayerProfile.StarterDecks);
            if (decks.Count == 0)
            {
                // Sollte nie passieren — der Login schickt nur hierher, wenn die
                // Auswahl offen ist. Wenn doch, nicht hängen bleiben.
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            if (network != null) network.OnMessage += HandleMessage;
            BuildUi();
            Select(0);
        }

        private void OnDestroy()
        {
            if (network != null) network.OnMessage -= HandleMessage;
        }

        private void HandleMessage(NetMessage message)
        {
            if (message.t == "error")
            {
                claiming = false;
                if (confirmButton != null) confirmButton.interactable = true;
                SetStatus(message.msg, true);
                return;
            }
            // Der Server hat das Deck vergeben, wenn er kein Pending mehr meldet.
            if ((message.t == "profile" || message.t == "auth_ok") && claiming && !PlayerProfile.StarterPending)
            {
                claiming = false;
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        // ================== Aufbau ==================
        //
        // Alle Rechtecke hier sind GESTRECKT und werden ueber Raender gesetzt
        // (offsetMin/offsetMax), nicht ueber Position und Groesse. Bei gestreckten
        // Ankern bedeutet sizeDelta naemlich "Abweichung von der Elterngroesse"
        // und anchoredPosition "Versatz der Mitte" — wer damit Raender meint,
        // bekommt Elemente, die uebereinanderliegen. Genau das ist beim ersten
        // Aufbau passiert.

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            var background = Panel(root, "Background", 0f, 0f, 0f, 0f).gameObject.AddComponent<Image>();
            background.sprite = cardSkin != null ? cardSkin.shellBackground : null;
            background.color = background.sprite != null ? Color.white : new Color(0.06f, 0.05f, 0.04f, 1f);

            Text(Panel(root, "Title", 80f, 80f, 38f, 984f),
                "CHOOSE YOUR FIRST DECK", 42f, Ink, TextAlignmentOptions.Center);
            Text(Panel(root, "Sub", 80f, 80f, 98f, 950f),
                "One only. You keep every card in it — the rest of the vault you earn.",
                19f, InkDim, TextAlignmentOptions.Center);

            BuildTiles(Panel(root, "Tiles", 80f, 80f, 142f, 812f));
            BuildDetail(Panel(root, "Detail", 80f, 80f, 284f, 34f));
        }

        /// <summary>Fünf Kacheln nebeneinander — die Wahl selbst.</summary>
        private void BuildTiles(RectTransform strip)
        {
            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            layout.childControlWidth = layout.childControlHeight = true;

            for (int i = 0; i < decks.Count; i++)
            {
                int index = i;
                var deck = decks[i];

                var tileGo = new GameObject(deck.id, typeof(RectTransform));
                tileGo.transform.SetParent(strip, false);
                var bg = tileGo.AddComponent<Image>();
                bg.color = TileBg;
                tileBackgrounds.Add(bg);
                var tile = (RectTransform)tileGo.transform;

                var frame = Panel(tile, "Frame", 0f, 0f, 0f, 0f).gameObject.AddComponent<Image>();
                frame.sprite = cardSkin != null ? cardSkin.relicFrame : null;
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
                frame.color = new Color(Gold.r, Gold.g, Gold.b, 0.28f);
                tileFrames.Add(frame);

                Text(Panel(tile, "Name", 10f, 10f, 14f, 76f), deck.name, 22f, Ink, TextAlignmentOptions.Center);
                Text(Panel(tile, "Arch", 10f, 10f, 46f, 50f), deck.archetypes, 13f, Gold, TextAlignmentOptions.Center);
                Text(Panel(tile, "Blurb", 12f, 12f, 76f, 10f), deck.blurb, 15f, InkDim, TextAlignmentOptions.Center);

                var button = tileGo.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { SfxManager.Click(); Select(index); });
            }
        }

        /// <summary>Beschreibung links, Grossansicht und Kartenraster rechts.</summary>
        private void BuildDetail(RectTransform panel)
        {
            var panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = PanelBg;

            // ---- linke Spalte: was das Deck tut ----
            var left = Column(panel, "Left", 0f, 0.34f, 28f, 18f, 24f, 20f);

            titleText = Text(Band(left, "Name", 0f, 0f, 0f, 44f), "", 34f, Ink, TextAlignmentOptions.TopLeft);
            archText = Text(Band(left, "Arch", 0f, 0f, 46f, 24f), "", 16f, Gold, TextAlignmentOptions.TopLeft);

            // Unten muss Platz fuer Statuszeile und Knopf bleiben: 56 + 8 + 24 + 12.
            var scroll = Panel(left, "DescScroll", 0f, 0f, 82f, 100f);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = Panel(scroll, "Viewport", 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Ohne Raycast-Empfaenger IM Viewport landet das Mausrad auf dem Panel
            // dahinter — das ist ein Geschwister, kein Vorfahr, und scrollt nicht.
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            scrollRect.viewport = viewport;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var content = (RectTransform)contentGo.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vertical = contentGo.AddComponent<VerticalLayoutGroup>();
            vertical.childForceExpandHeight = false;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            scrollRect.content = content;

            var descGo = new GameObject("Desc", typeof(RectTransform));
            descGo.transform.SetParent(content, false);
            descText = descGo.AddComponent<TextMeshProUGUI>();
            descText.fontSize = 16.5f;
            descText.color = Hex("#C6CCD4");
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.raycastTarget = false;
            ApplyFont(descText);

            statusText = Text(Footer(left, "Status", 0f, 0f, 64f, 24f), "", 15f, Hex("#E0603A"),
                TextAlignmentOptions.BottomLeft);

            confirmButton = ConfirmRow(left, out confirmLabel);
            confirmButton.onClick.AddListener(Confirm);

            // ---- rechte Spalte: Grossansicht oben, Raster darunter ----
            var right = Column(panel, "Right", 0.34f, 1f, 18f, 28f, 24f, 20f);

            preview = Instantiate(cardViewPrefab, right);
            var previewRect = (RectTransform)preview.transform;
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0f, 1f);
            previewRect.pivot = new Vector2(0f, 1f);
            previewRect.sizeDelta = new Vector2(232f, 325f);
            previewRect.anchoredPosition = Vector2.zero;
            preview.gameObject.SetActive(false);

            previewRules = Text(Band(right, "PreviewRules", 248f, 0f, 2f, 325f),
                "Point at a card to read it.", 15f, Hex("#C6CCD4"), TextAlignmentOptions.TopLeft);
            previewRules.textWrappingMode = TextWrappingModes.Normal;

            countText = Text(Band(right, "Count", 0f, 0f, 336f, 24f), "", 14.5f, InkDim,
                TextAlignmentOptions.BottomLeft);

            var gridScroll = Panel(right, "GridScroll", 0f, 0f, 366f, 0f);
            var gridScrollRect = gridScroll.gameObject.AddComponent<ScrollRect>();
            gridScrollRect.horizontal = false;
            gridScrollRect.movementType = ScrollRect.MovementType.Clamped;
            gridScrollRect.scrollSensitivity = 36f;
            var gridViewport = Panel(gridScroll, "Viewport", 0f, 0f, 0f, 0f);
            gridViewport.gameObject.AddComponent<RectMask2D>();
            gridViewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            gridScrollRect.viewport = gridViewport;

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            cardGrid = (RectTransform)gridGo.transform;
            cardGrid.SetParent(gridViewport, false);
            cardGrid.anchorMin = new Vector2(0f, 1f);
            cardGrid.anchorMax = new Vector2(1f, 1f);
            cardGrid.pivot = new Vector2(0.5f, 1f);
            cardGrid.offsetMin = Vector2.zero;
            cardGrid.offsetMax = Vector2.zero;
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(106f, 148f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(0, 0, 0, 8);
            gridGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            gridScrollRect.content = cardGrid;
        }

        private Button ConfirmRow(RectTransform parent, out TMP_Text label)
        {
            var rect = Footer(parent, "Confirm", 0f, 0f, 0f, 56f);
            var bg = rect.gameObject.AddComponent<Image>();
            bg.sprite = cardSkin != null ? cardSkin.badgeEmber : null;
            bg.type = bg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = bg.sprite != null ? Color.white : Gold;

            label = Text(Panel(rect, "Label", 0f, 0f, 0f, 0f), "TAKE THIS DECK", 21f,
                Hex("#231A12"), TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;

            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        // ================== Auswahl ==================

        private void Select(int index)
        {
            if (index < 0 || index >= decks.Count) return;
            selected = index;
            var deck = decks[index];

            for (int i = 0; i < tileFrames.Count; i++)
            {
                bool active = i == index;
                tileFrames[i].color = active ? Gold : new Color(Gold.r, Gold.g, Gold.b, 0.28f);
                tileBackgrounds[i].color = active ? new Color(Gold.r, Gold.g, Gold.b, 0.14f) : TileBg;
            }

            titleText.text = deck.name;
            archText.text = deck.archetypes;
            descText.text = deck.description;
            SetStatus("", false);
            BuildCardGrid(deck);
        }

        /// <summary>
        /// Jede Karte des Decks als Miniatur. Mehrfach enthaltene Karten stehen
        /// einmal da, mit Anzahl — 40 Kacheln, von denen 12 dasselbe zeigen, wären
        /// zum Lesen unbrauchbar.
        /// </summary>
        private void BuildCardGrid(NetStarterDeck deck)
        {
            for (int i = cardGrid.childCount - 1; i >= 0; i--) Destroy(cardGrid.GetChild(i).gameObject);
            preview.gameObject.SetActive(false);
            previewRules.text = "Point at a card to read it.";

            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var name in All(deck))
            {
                if (counts.ContainsKey(name)) counts[name]++;
                else { counts[name] = 1; order.Add(name); }
            }

            order.Sort((a, b) =>
            {
                var da = catalog.FindByName(a);
                var db = catalog.FindByName(b);
                int ka = da != null ? (int)da.Kind : 9, kb = db != null ? (int)db.Kind : 9;
                bool ra = da is ReliquaryCardData, rb = db is ReliquaryCardData;
                if (ra != rb) return ra ? 1 : -1;              // Extra Deck ans Ende
                if (ka != kb) return ka.CompareTo(kb);
                int la = da is MonsterCardData ma ? ma.level : 0;
                int lb = db is MonsterCardData mb ? mb.level : 0;
                if (la != lb) return la.CompareTo(lb);
                return string.CompareOrdinal(a, b);
            });

            foreach (var name in order)
            {
                var definition = catalog.FindByName(name);
                if (definition == null) continue;

                var holder = new GameObject(name, typeof(RectTransform));
                holder.transform.SetParent(cardGrid, false);

                var view = Instantiate(cardViewPrefab, holder.transform);
                var viewRect = (RectTransform)view.transform;
                viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
                viewRect.offsetMin = Vector2.zero; viewRect.offsetMax = Vector2.zero;
                view.Show(new CardInstance(definition, null), false, upright: true);
                view.SetHighlight(false);

                if (counts[name] > 1)
                {
                    var badge = Text(Footer((RectTransform)holder.transform, "Badge", 0f, 4f, 2f, 22f),
                        "×" + counts[name], 16f, Ink, TextAlignmentOptions.BottomRight);
                    badge.fontStyle = FontStyles.Bold;
                    badge.outlineWidth = 0.25f;
                    badge.outlineColor = new Color32(0, 0, 0, 220);
                }

                // Ein durchsichtiger Empfänger obendrauf: die Kartenansicht selbst
                // hat eigene Zeiger-Ereignisse, die wir hier nicht wollen.
                var hit = new GameObject("Hit", typeof(RectTransform));
                var hitRect = (RectTransform)hit.transform;
                hitRect.SetParent(holder.transform, false);
                hitRect.anchorMin = Vector2.zero; hitRect.anchorMax = Vector2.one;
                hitRect.offsetMin = Vector2.zero; hitRect.offsetMax = Vector2.zero;
                var hitImage = hit.AddComponent<Image>();
                hitImage.color = new Color(0f, 0f, 0f, 0f);
                var hover = hit.AddComponent<CardHoverProxy>();
                hover.Setup(definition, ShowPreview);
            }

            int main = deck.cards != null ? deck.cards.Length : 0;
            int extra = deck.extra != null ? deck.extra.Length : 0;
            countText.text = $"{main} cards · {extra} in the Extra Deck · {order.Count} different";
        }

        private static IEnumerable<string> All(NetStarterDeck deck)
        {
            if (deck.cards != null) foreach (var n in deck.cards) yield return n;
            if (deck.extra != null) foreach (var n in deck.extra) yield return n;
        }

        private void ShowPreview(CardDefinition definition)
        {
            if (definition == null) return;
            preview.gameObject.SetActive(true);
            preview.Show(new CardInstance(definition, null), false, upright: true);
            preview.SetHighlight(false);
            previewRules.text = CardDetailPanel.BuildFormattedRulesText(definition);
        }

        private void Confirm()
        {
            if (claiming || selected < 0) return;
            if (network == null || !network.IsConnected)
            {
                SetStatus("No connection to the vault.", true);
                return;
            }
            claiming = true;
            confirmButton.interactable = false;
            confirmLabel.text = "OPENING…";
            SfxManager.Click();
            network.SendClaimStarter(decks[selected].id);
        }

        private void SetStatus(string text, bool bad)
        {
            if (statusText == null) return;
            statusText.text = text ?? "";
            statusText.color = bad ? Hex("#E0603A") : InkDim;
            if (bad && confirmLabel != null) confirmLabel.text = "TAKE THIS DECK";
        }

        // ================== kleine Helfer ==================

        /// <summary>
        /// Ein gestrecktes Rechteck mit Raendern in Pixeln — links, rechts, oben,
        /// unten. Genau so, wie man ein Layout beschreibt. Wer stattdessen
        /// anchoredPosition und sizeDelta nimmt, meint etwas anderes, als er sagt.
        /// </summary>
        private static RectTransform Panel(Transform parent, string name,
            float left, float right, float top, float bottom)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>
        /// Ein Band fester Hoehe, am OBEREN Rand festgemacht. Fuer alles, was
        /// einen bestimmten Abstand von oben haben soll, egal wie hoch das
        /// Elternteil gerade ist — die Hoehe haengt an der Bildschirmgroesse,
        /// und ein ueber den unteren Rand gerechneter Abstand wandert mit.
        /// </summary>
        private static RectTransform Band(Transform parent, string name,
            float left, float right, float top, float height)
        {
            var rect = Panel(parent, name, left, right, 0f, 0f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Dasselbe, aber am UNTEREN Rand festgemacht.</summary>
        private static RectTransform Footer(Transform parent, string name,
            float left, float right, float bottom, float height)
        {
            var rect = Panel(parent, name, left, right, 0f, 0f);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
            return rect;
        }

        /// <summary>Eine Spalte zwischen zwei Anteilen der Breite, mit eigenen Raendern.</summary>
        private static RectTransform Column(Transform parent, string name, float fromX, float toX,
            float left, float right, float top, float bottom)
        {
            var rect = Panel(parent, name, left, right, top, bottom);
            rect.anchorMin = new Vector2(fromX, 0f);
            rect.anchorMax = new Vector2(toX, 1f);
            return rect;
        }

        private static TMP_Text Text(RectTransform rect, string text, float size, Color color,
            TextAlignmentOptions alignment)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            ApplyFont(label);
            return label;
        }

        private static void ApplyFont(TMP_Text label)
        {
            var skin = TransitionSkin.Load();
            if (skin != null && skin.oswald != null) label.font = skin.oswald;
        }
    }

    /// <summary>Meldet die Karte, über der der Zeiger steht — mehr nicht.</summary>
    public class CardHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private CardDefinition definition;
        private System.Action<CardDefinition> onShow;

        public void Setup(CardDefinition card, System.Action<CardDefinition> show)
        {
            definition = card;
            onShow = show;
        }

        public void OnPointerEnter(PointerEventData eventData) => onShow?.Invoke(definition);

        public void OnPointerClick(PointerEventData eventData)
        {
            SfxManager.Click();
            onShow?.Invoke(definition);
        }
    }
}
