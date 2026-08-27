using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Hover-Tooltip für die Status-Badges: sitzt auf jedem Badge-Element der
    /// Inspect-Karte (CardDetailPanel) und zeigt Name + Bedeutung des Status.
    /// Das Tooltip-Panel selbst ist ein geteiltes, zur Laufzeit gebautes
    /// Overlay am Root-Canvas — eines pro Szene, wandert zum gehoverten Badge.
    /// </summary>
    public class TcgBadgeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector] public string Title = "";
        [HideInInspector] public string Body = "";

        private static RectTransform panel;
        private static TMP_Text titleText;
        private static TMP_Text bodyText;
        private static TcgBadgeTooltip current;

        public void OnPointerEnter(PointerEventData eventData) => Show(this);

        public void OnPointerExit(PointerEventData eventData)
        {
            if (current == this) Hide();
        }

        private void OnDisable()
        {
            if (current == this) Hide();
        }

        private static void Show(TcgBadgeTooltip source)
        {
            if (string.IsNullOrEmpty(source.Title) && string.IsNullOrEmpty(source.Body)) return;
            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var root = canvas.rootCanvas;

            if (panel == null) BuildPanel(source);
            if (panel == null) return;

            current = source;
            panel.SetParent(root.transform, false);
            titleText.text = source.Title;
            bodyText.text = source.Body;
            bodyText.gameObject.SetActive(!string.IsNullOrEmpty(source.Body));
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();

            // Rechts neben dem Badge andocken; TMP braucht einen Layout-Pass
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            var badgeRect = (RectTransform)source.transform;
            Vector3[] corners = new Vector3[4];
            badgeRect.GetWorldCorners(corners); // 0=BL 1=TL 2=TR 3=BR
            Vector3 anchorWorld = (corners[2] + corners[3]) * 0.5f; // rechte Kante, Mitte
            panel.pivot = new Vector2(0f, 0.5f);
            panel.position = anchorWorld;
            panel.anchoredPosition += new Vector2(10f, 0f);
            ClampToCanvas(root);
        }

        /// <summary>Tooltip im Canvas halten — notfalls auf die linke Seite des Badges klappen.</summary>
        private static void ClampToCanvas(Canvas root)
        {
            var canvasRect = (RectTransform)root.transform;
            Vector3[] corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            Vector3[] bounds = new Vector3[4];
            canvasRect.GetWorldCorners(bounds);
            float overRight = corners[2].x - bounds[2].x;
            if (overRight > 0f) panel.position -= new Vector3(overRight + 8f, 0f, 0f);
            float underBottom = bounds[0].y - corners[0].y;
            if (underBottom > 0f) panel.position += new Vector3(0f, underBottom + 8f, 0f);
            float overTop = corners[1].y - bounds[1].y;
            if (overTop > 0f) panel.position -= new Vector3(0f, overTop + 8f, 0f);
        }

        public static void Hide()
        {
            current = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        /// <summary>Baut das geteilte Panel: dunkle Platte, Titelzeile, Fließtext.</summary>
        private static void BuildPanel(TcgBadgeTooltip fontSource)
        {
            var go = new GameObject("BadgeTooltip", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel = (RectTransform)go.transform;
            panel.sizeDelta = new Vector2(300f, 0f);

            var background = go.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.12f, 0.96f);
            background.raycastTarget = false;
            var pillSprite = Resources.Load<Sprite>("UI/Status/BadgePillCounters");
            if (pillSprite != null) { background.sprite = pillSprite; background.type = Image.Type.Sliced; }

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Schrift von einer echten Karten-Schrift erben, damit alle Sprachen zeichnen
            var fontDonor = fontSource.GetComponentInParent<TcgCardView>();
            TMP_FontAsset font = null;
            if (fontDonor != null)
            {
                foreach (var text in fontDonor.GetComponentsInChildren<TMP_Text>(true))
                    if (text != null && text.font != null) { font = text.font; break; }
            }

            TMP_Text MakeText(string name, float size, Color color, FontStyles style)
            {
                var textGo = new GameObject(name, typeof(RectTransform));
                textGo.transform.SetParent(panel, false);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.fontSize = size;
                tmp.color = color;
                tmp.fontStyle = style;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.raycastTarget = false;
                return tmp;
            }

            titleText = MakeText("Title", 21f, new Color(0.95f, 0.88f, 0.70f), FontStyles.Bold);
            bodyText = MakeText("Body", 17f, new Color(0.82f, 0.85f, 0.92f), FontStyles.Normal);
            panel.gameObject.SetActive(false);
        }
    }
}
