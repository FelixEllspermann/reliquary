using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// NEW CARDS — die Szene hinter dem gleichnamigen Knopf der Patch Notes:
    /// alle Karten, die in der laufenden Version neu dazukamen, gruppiert nach
    /// Ausgabe. „Laufende Version" heißt die Versionsfamilie ohne Buchstaben:
    /// bei 0.1.6b sind das 0.1.6b (The Small Print) UND 0.1.6 (Six for Thirty),
    /// bei 0.1.7 später nur 0.1.7 — bis der nächste Buchstaben-Patch kommt.
    ///
    /// Die Zuordnung steht auf der Karte selbst (CardDefinition.releaseVersion,
    /// von den Builder-Stages gestempelt), die Namen der Ausgaben holt sich die
    /// Szene aus den Patch Notes („0.1.6 · SIX FOR THIRTY").
    ///
    /// Aufbau wie die Startdeck-Wahl: alles entsteht zur Laufzeit, verdrahtet
    /// sind nur Katalog, Karten-Prefab und Skin. Links die Großansicht mit
    /// Regeltext, rechts die Kacheln — gestaffelt gefüllt, damit 240 Karten
    /// nicht in einem Bild aufgebaut werden müssen.
    /// </summary>
    public class NewCardsController : MonoBehaviour
    {
        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;
        [SerializeField] private TcgCardView cardViewPrefab;
        [Tooltip("Hintergrund, Rahmen und Knopf-Verlauf — dieselben wie in Login und Hauptmenü")]
        [SerializeField] private CardSkin cardSkin;

        [Header("Szenen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Tooltip("Leer = Application.version. Zum Testen anderer Ausgaben im Editor.")]
        [SerializeField] private string versionOverride = "";

        // ---- Farben, an den Shell-Screens ausgerichtet ----
        private static readonly Color Ink = Hex("#F3DDA4");
        private static readonly Color InkDim = Hex("#8C7B5F");
        private static readonly Color Gold = Hex("#C8A45C");
        private static readonly Color PanelBg = new Color(0f, 0f, 0f, 0.42f);

        private const float CellWidth = 118f;
        private const float CellHeight = 165f;
        private const float FillBudgetMs = 5f;

        private static readonly string[] KindNames = { "ALL", "MONSTER", "SPELL", "ARTIFACT", "RELIQUARY", "INCARNATE" };

        private TcgCardView preview;
        private TMP_Text previewName, previewRules, countText;
        private RectTransform listContent;
        private ScrollRect listScroll;
        private readonly List<(Image bg, TMP_Text label)> chips = new List<(Image, TMP_Text)>();
        private int kindFilter;
        private Coroutine fill;

        private readonly List<(string version, string title, List<CardDefinition> cards)> sections
            = new List<(string, string, List<CardDefinition>)>();

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private void Start()
        {
            CollectSections();
            BuildUi();
            Refill();
        }

        // ================== Daten ==================

        /// <summary>„0.1.6b" → „0.1.6": Buchstaben-Patches gehören zur Familie ihrer Ausgabe.</summary>
        public static string VersionFamily(string version)
        {
            if (string.IsNullOrEmpty(version)) return "";
            int end = version.Length;
            while (end > 0 && !char.IsDigit(version[end - 1])) end--;
            return version.Substring(0, end);
        }

        private string CurrentVersion => string.IsNullOrEmpty(versionOverride) ? Application.version : versionOverride;

        private void CollectSections()
        {
            sections.Clear();
            if (catalog == null) return;
            string family = VersionFamily(CurrentVersion);
            var byVersion = new Dictionary<string, List<CardDefinition>>();
            foreach (var card in catalog.cards)
            {
                if (card == null || card.isToken || card is PlayerCardData) continue;
                if (string.IsNullOrEmpty(card.releaseVersion) || VersionFamily(card.releaseVersion) != family) continue;
                if (!byVersion.TryGetValue(card.releaseVersion, out var list)) byVersion[card.releaseVersion] = list = new List<CardDefinition>();
                list.Add(card);
            }

            var titles = ReadPatchTitles();
            foreach (var version in byVersion.Keys.OrderByDescending(v => v, System.StringComparer.Ordinal))
            {
                var list = byVersion[version];
                list.Sort(CompareCards);
                titles.TryGetValue(version, out var title);
                sections.Add((version, title ?? "", list));
            }
        }

        /// <summary>Kopfzeilen der Patch Notes: „0.1.6 · SIX FOR THIRTY" → Ausgabe-Name je Version.</summary>
        private static Dictionary<string, string> ReadPatchTitles()
        {
            var titles = new Dictionary<string, string>();
            var notes = Resources.Load<TextAsset>("PatchNotes");
            if (notes == null) return titles;
            foreach (var raw in notes.text.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                int split = line.IndexOf(" · ", System.StringComparison.Ordinal);
                if (split <= 0) continue;
                string version = line.Substring(0, split).Trim();
                if (version.Length == 0 || !char.IsDigit(version[0]) || version.Contains(' ')) continue;
                if (!titles.ContainsKey(version)) titles[version] = line.Substring(split + 3).Trim();
            }
            return titles;
        }

        /// <summary>Monster vor Zaubern vor Artefakten, Reliquaries ans Ende; innerhalb nach Namen (hält Archetypes zusammen).</summary>
        private static int CompareCards(CardDefinition a, CardDefinition b)
        {
            bool ra = a.IsExtraDeckCard, rb = b.IsExtraDeckCard;
            if (ra != rb) return ra ? 1 : -1;
            int ka = (int)a.Kind, kb = (int)b.Kind;
            if (ka != kb) return ka.CompareTo(kb);
            return string.CompareOrdinal(a.cardName, b.cardName);
        }

        private bool PassesFilter(CardDefinition card)
        {
            switch (kindFilter)
            {
                case 1: return card is MonsterCardData && !card.IsExtraDeckCard;
                case 2: return card is SpellCardData;
                case 3: return card is ArtifactCardData;
                case 4: return card is ReliquaryCardData;
                case 5: return card is IncarnateCardData;
                default: return true;
            }
        }

        // ================== Aufbau ==================

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
                Debug.LogError("NewCards: kein EventSystem in der Szene — nichts reagiert auf Klicks.");

            var background = Panel(root, "Background", 0f, 0f, 0f, 0f).gameObject.AddComponent<Image>();
            background.sprite = cardSkin != null ? cardSkin.shellBackground : null;
            background.color = background.sprite != null ? Color.white : new Color(0.06f, 0.05f, 0.04f, 1f);

            string family = VersionFamily(CurrentVersion);
            int total = sections.Sum(s => s.cards.Count);
            Text(Panel(root, "Title", 80f, 80f, 38f, 984f), Loc.T("NEW CARDS"), 42f, Ink, TextAlignmentOptions.Center);
            Text(Panel(root, "Sub", 80f, 80f, 98f, 950f),
                total > 0
                    ? Loc.F("{0} CARDS ADDED IN {1} AND ITS PATCHES · POINT AT A CARD TO READ IT", total, family)
                    : Loc.F("NOTHING NEW IN {0} YET", family),
                19f, InkDim, TextAlignmentOptions.Center);

            var panel = Panel(root, "Body", 80f, 80f, 142f, 34f);
            panel.gameObject.AddComponent<Image>().color = PanelBg;

            // ---- linke Spalte: Großansicht, Regeltext, Zurück ----
            var left = Column(panel, "Left", 0f, 0.22f, 24f, 12f, 24f, 20f);

            preview = Instantiate(cardViewPrefab, left);
            var previewRect = (RectTransform)preview.transform;
            previewRect.anchorMin = new Vector2(0.5f, 1f);
            previewRect.anchorMax = new Vector2(0.5f, 1f);
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewRect.sizeDelta = new Vector2(232f, 325f);
            previewRect.anchoredPosition = Vector2.zero;
            preview.gameObject.SetActive(false);

            previewName = Text(Band(left, "PreviewName", 0f, 0f, 336f, 30f), "", 20f, Ink, TextAlignmentOptions.TopLeft);
            previewName.fontStyle = FontStyles.Bold;

            var rulesScroll = Panel(left, "RulesScroll", 0f, 0f, 372f, 76f);
            var rulesScrollRect = rulesScroll.gameObject.AddComponent<ScrollRect>();
            rulesScrollRect.horizontal = false;
            rulesScrollRect.movementType = ScrollRect.MovementType.Clamped;
            rulesScrollRect.scrollSensitivity = 28f;
            var rulesViewport = Panel(rulesScroll, "Viewport", 0f, 0f, 0f, 0f);
            rulesViewport.gameObject.AddComponent<RectMask2D>();
            rulesViewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            rulesScrollRect.viewport = rulesViewport;
            var rulesContent = new GameObject("Content", typeof(RectTransform));
            var rulesContentRect = (RectTransform)rulesContent.transform;
            rulesContentRect.SetParent(rulesViewport, false);
            rulesContentRect.anchorMin = new Vector2(0f, 1f);
            rulesContentRect.anchorMax = new Vector2(1f, 1f);
            rulesContentRect.pivot = new Vector2(0.5f, 1f);
            rulesContentRect.offsetMin = Vector2.zero;
            rulesContentRect.offsetMax = Vector2.zero;
            rulesContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rulesLayout = rulesContent.AddComponent<VerticalLayoutGroup>();
            rulesLayout.childForceExpandHeight = false;
            rulesLayout.childControlHeight = true;
            rulesLayout.childControlWidth = true;
            rulesScrollRect.content = rulesContentRect;
            var rulesGo = new GameObject("Rules", typeof(RectTransform));
            rulesGo.transform.SetParent(rulesContentRect, false);
            previewRules = rulesGo.AddComponent<TextMeshProUGUI>();
            previewRules.fontSize = 15f;
            previewRules.color = Hex("#C6CCD4");
            previewRules.alignment = TextAlignmentOptions.TopLeft;
            previewRules.textWrappingMode = TextWrappingModes.Normal;
            previewRules.raycastTarget = false;
            previewRules.text = Loc.T("Point at a card to read it.");
            ApplyFont(previewRules);

            var back = BackRow(left, out var backLabel);
            back.onClick.AddListener(() => { SfxManager.Click(); SceneManager.LoadScene(mainMenuSceneName); });

            // ---- rechte Spalte: Filter-Chips, Zähler, Karten-Stapel ----
            var right = Column(panel, "Right", 0.22f, 1f, 12f, 28f, 24f, 20f);

            BuildChips(Band(right, "Chips", 0f, 0f, 0f, 34f));
            countText = Text(Band(right, "Count", 0f, 0f, 40f, 22f), "", 14.5f, InkDim, TextAlignmentOptions.BottomLeft);

            var listScrollRect = Panel(right, "ListScroll", 0f, 0f, 68f, 0f);
            listScroll = listScrollRect.gameObject.AddComponent<ScrollRect>();
            listScroll.horizontal = false;
            listScroll.movementType = ScrollRect.MovementType.Clamped;
            listScroll.scrollSensitivity = 40f;
            var listViewport = Panel(listScrollRect, "Viewport", 0f, 0f, 0f, 0f);
            listViewport.gameObject.AddComponent<RectMask2D>();
            listViewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            listScroll.viewport = listViewport;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            listContent = (RectTransform)contentGo.transform;
            listContent.SetParent(listViewport, false);
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = Vector2.zero;
            listContent.offsetMax = Vector2.zero;
            var stack = contentGo.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 6f;
            stack.padding = new RectOffset(0, 12, 0, 12);
            stack.childForceExpandHeight = false;
            stack.childForceExpandWidth = true;
            stack.childControlHeight = true;
            stack.childControlWidth = true;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listScroll.content = listContent;
        }

        private void BuildChips(RectTransform row)
        {
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < KindNames.Length; i++)
            {
                int index = i;
                var chipGo = new GameObject(KindNames[i], typeof(RectTransform));
                chipGo.transform.SetParent(row, false);
                var bg = chipGo.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.34f);
                var element = chipGo.AddComponent<LayoutElement>();
                element.preferredWidth = i == 0 ? 72f : 118f;
                element.preferredHeight = 34f;
                var label = Text(Panel(chipGo.transform, "Label", 0f, 0f, 0f, 0f), Loc.T(KindNames[i]), 14f, InkDim, TextAlignmentOptions.Center);
                label.characterSpacing = 2f;
                var button = chipGo.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { SfxManager.Click(); kindFilter = index; Refill(); });
                chips.Add((bg, label));
            }
            PaintChips();
        }

        private void PaintChips()
        {
            for (int i = 0; i < chips.Count; i++)
            {
                bool active = i == kindFilter;
                chips[i].bg.color = active ? new Color(Gold.r, Gold.g, Gold.b, 0.22f) : new Color(0f, 0f, 0f, 0.34f);
                chips[i].label.color = active ? Ink : InkDim;
            }
        }

        private Button BackRow(RectTransform parent, out TMP_Text label)
        {
            var rect = Footer(parent, "Back", 0f, 0f, 0f, 56f);
            var bg = rect.gameObject.AddComponent<Image>();
            bg.sprite = cardSkin != null ? cardSkin.badgeEmber : null;
            bg.type = bg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = bg.sprite != null ? Color.white : Gold;
            label = Text(Panel(rect, "Label", 0f, 0f, 0f, 0f), Loc.T("BACK TO MENU"), 21f, Hex("#231A12"), TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        // ================== Karten-Stapel ==================

        private void Refill()
        {
            PaintChips();
            if (fill != null) StopCoroutine(fill);
            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);
            preview.gameObject.SetActive(false);
            previewName.text = "";
            previewRules.text = Loc.T("Point at a card to read it.");
            fill = StartCoroutine(FillRoutine());
        }

        /// <summary>
        /// Je Ausgabe eine Kopfzeile und ein Raster; die Kacheln entstehen mit
        /// Zeitbudget über mehrere Bilder — 240 Kartenansichten in einem Bild
        /// wären ein spürbarer Ruckler.
        /// </summary>
        private IEnumerator FillRoutine()
        {
            int shown = 0, all = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var section in sections)
            {
                var cards = section.cards.Where(PassesFilter).ToList();
                all += section.cards.Count;
                if (cards.Count == 0) continue;
                shown += cards.Count;

                string title = string.IsNullOrEmpty(section.title) ? section.version : $"{section.version} · {section.title}";
                var header = Text(Row(listContent, "Header_" + section.version, 44f), $"{title}   <color=#8C7B5F>{Loc.F("— {0} CARDS", cards.Count)}</color>",
                    22f, Ink, TextAlignmentOptions.BottomLeft);
                header.characterSpacing = 1.5f;

                var gridGo = new GameObject("Grid_" + section.version, typeof(RectTransform));
                var gridRect = (RectTransform)gridGo.transform;
                gridRect.SetParent(listContent, false);
                var grid = gridGo.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(CellWidth, CellHeight);
                grid.spacing = new Vector2(8f, 8f);
                grid.padding = new RectOffset(0, 0, 4, 12);
                gridGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                foreach (var card in cards)
                {
                    MakeTile(gridRect, card);
                    if (watch.ElapsedMilliseconds >= FillBudgetMs)
                    {
                        yield return null;
                        watch.Restart();
                    }
                }
            }
            countText.text = shown == all
                ? Loc.F("{0} NEW CARDS", all)
                : Loc.F("{0} OF {1} NEW CARDS SHOWN", shown, all);
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
            listScroll.verticalNormalizedPosition = 1f;
            fill = null;
        }

        private void MakeTile(RectTransform grid, CardDefinition definition)
        {
            var holder = new GameObject(definition.cardName, typeof(RectTransform));
            holder.transform.SetParent(grid, false);

            var view = Instantiate(cardViewPrefab, holder.transform);
            var viewRect = (RectTransform)view.transform;
            viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero; viewRect.offsetMax = Vector2.zero;
            view.HoverLift = false;
            view.Show(new CardInstance(definition, null), false, upright: true);
            view.SetHighlight(false);

            // Ein durchsichtiger Empfänger obendrauf: die Kartenansicht selbst
            // hat eigene Zeiger-Ereignisse, die wir hier nicht wollen.
            var hit = new GameObject("Hit", typeof(RectTransform));
            var hitRect = (RectTransform)hit.transform;
            hitRect.SetParent(holder.transform, false);
            hitRect.anchorMin = Vector2.zero; hitRect.anchorMax = Vector2.one;
            hitRect.offsetMin = Vector2.zero; hitRect.offsetMax = Vector2.zero;
            hit.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            hit.AddComponent<CardHoverProxy>().Setup(definition, ShowPreview);
        }

        private void ShowPreview(CardDefinition definition)
        {
            if (definition == null) return;
            preview.gameObject.SetActive(true);
            preview.Show(new CardInstance(definition, null), false, upright: true);
            preview.SetHighlight(false);
            previewName.text = Loc.CardName(definition.cardName);
            previewRules.text = CardDetailPanel.BuildFormattedRulesText(definition);
        }

        // ================== kleine Helfer (wie StarterPick) ==================

        private static RectTransform Panel(Transform parent, string name, float left, float right, float top, float bottom)
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

        private static RectTransform Band(Transform parent, string name, float left, float right, float top, float height)
        {
            var rect = Panel(parent, name, left, right, 0f, 0f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        private static RectTransform Footer(Transform parent, string name, float left, float right, float bottom, float height)
        {
            var rect = Panel(parent, name, left, right, 0f, 0f);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
            return rect;
        }

        private static RectTransform Column(Transform parent, string name, float fromX, float toX,
            float left, float right, float top, float bottom)
        {
            var rect = Panel(parent, name, left, right, top, bottom);
            rect.anchorMin = new Vector2(fromX, 0f);
            rect.anchorMax = new Vector2(toX, 1f);
            return rect;
        }

        /// <summary>Eine Zeile fester Höhe im Vertikal-Stapel (LayoutElement statt Anker).</summary>
        private static RectTransform Row(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return rect;
        }

        private static TMP_Text Text(RectTransform rect, string text, float size, Color color, TextAlignmentOptions alignment)
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
}
