using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die Kettenanzeige am oberen Bildschirmrand.
    ///
    /// <para>
    /// <b>Warum es die braucht.</b> Die Engine führt keine Kette als Liste: eine
    /// Aktivierung öffnet ein Reaktionsfenster, darin läuft die nächste
    /// Aktivierung vollständig durch, und die Reihenfolge steckt allein im
    /// Aufrufstapel. Für den Spieler passierte dadurch alles auf einmal und in
    /// einer Reihenfolge, die er nicht sehen konnte.
    /// </para>
    /// <para>
    /// <b>Bauen und Abbauen sind getrennt.</b> Solange Glieder dazukommen, steht
    /// oben BUILDING CHAIN. Sobald das erste Glied auflöst, hält die Anzeige
    /// einen Moment inne, wechselt auf RESOLVING und arbeitet dann von unten
    /// nach oben ab.
    /// </para>
    /// <para>
    /// <b>Warum oben und nicht links.</b> Die linke Leiste sieht leer aus,
    /// solange man nichts anfasst — sobald man aber eine Karte hovert, steht
    /// dort ihr Effekttext. Genau dann liest man auch die Kette, und beides
    /// zugleich geht nicht. Oben liegt nur die gegnerische Hand, und die ist
    /// verdeckt.
    /// </para>
    /// </summary>
    public class ChainTracker : MonoBehaviour
    {
        // Ein einzelnes Glied wächst in 0.22 s ein, die Denkpause dauert 0.75 s,
        // und jedes aufgelöste Glied bekommt 0.35 s, bevor das nächste dran ist.
        private const float GrowTime = 0.22f;
        private const float SetPause = 0.75f;
        private const float ResolveHold = 0.35f;
        private const float FadeOut = 0.4f;

        private const float PanelWidth = 640f;
        private const float HeaderHeight = 42f;
        private const float RowHeight = 50f;
        private const float RowSpacing = 5f;
        private const float Padding = 10f;

        private static readonly Color Ink = Hex("#F3DDA4");
        private static readonly Color InkDim = Hex("#7E7059");
        private static readonly Color Gold = Hex("#C8A45C");
        private static readonly Color Mine = Hex("#6FD3E0");
        private static readonly Color Theirs = Hex("#E0603A");
        private static readonly Color PanelBg = new Color(0.03f, 0.026f, 0.02f, 0.94f);
        private static readonly Color HeaderBg = new Color(0.07f, 0.058f, 0.042f, 0.97f);

        private RectTransform panel;
        private TMP_Text headerText;
        private TMP_Text chevron;
        private RectTransform rows;
        private CanvasGroup group;
        private CardDetailPanel detail;

        private readonly List<Row> links = new List<Row>();
        private bool resolving;
        private bool expanded = true;

        /// <summary>Ein Glied: die Zeile und ihre Teile, damit Auflösen sie einfärben kann.</summary>
        private class Row
        {
            public RectTransform Rect;
            public Image Background;
            public Image Accent;
            public TMP_Text Number;
            public TMP_Text Name;
            public TMP_Text Label;
            public CanvasGroup Group;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// <summary>
        /// Hängt die Anzeige an einen Canvas. Sie beginnt unsichtbar.
        /// <paramref name="detailPanel"/> darf null sein — dann gibt es beim
        /// Hovern über eine Zeile eben keine Kartenvorschau.
        /// </summary>
        public static ChainTracker Create(RectTransform canvas, CardDetailPanel detailPanel)
        {
            var go = new GameObject("ChainTracker", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            // Oben mittig, hängend — der Kasten wächst nach unten, die Oberkante
            // bleibt stehen. Als letztes Kind des Canvas zeichnet er über allem;
            // das PromptPanel liegt in der Bildmitte und bleibt frei.
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(PanelWidth, HeaderHeight);
            rect.SetAsLastSibling();

            var tracker = go.AddComponent<ChainTracker>();
            tracker.detail = detailPanel;
            tracker.Build();
            return tracker;
        }

        private void Build()
        {
            panel = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            // Solange nichts zu sehen ist, darf der Kasten auch keine Klicks
            // schlucken — er liegt über dem Brett.
            group.blocksRaycasts = false;

            var bg = NewChild("Backdrop", panel).gameObject.AddComponent<Image>();
            Stretch((RectTransform)bg.transform);
            bg.color = PanelBg;
            bg.raycastTarget = false;

            var edge = NewChild("Edge", panel);
            edge.anchorMin = new Vector2(0f, 0f);
            edge.anchorMax = new Vector2(1f, 0f);
            edge.pivot = new Vector2(0.5f, 0f);
            edge.offsetMin = Vector2.zero;
            edge.offsetMax = new Vector2(0f, 3f);
            var line = edge.gameObject.AddComponent<Image>();
            line.color = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
            line.raycastTarget = false;

            // ---- Kopfzeile: zugleich der Knopf zum Auf- und Zuklappen ----
            var header = NewChild("Header", panel);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -HeaderHeight);
            header.offsetMax = Vector2.zero;
            var headerBg = header.gameObject.AddComponent<Image>();
            headerBg.color = HeaderBg;
            headerBg.raycastTarget = true;
            var button = header.gameObject.AddComponent<Button>();
            button.targetGraphic = headerBg;
            button.onClick.AddListener(Toggle);

            var title = NewChild("Title", header);
            title.anchorMin = new Vector2(0f, 0f);
            title.anchorMax = new Vector2(1f, 1f);
            title.offsetMin = new Vector2(16f, 0f);
            title.offsetMax = new Vector2(-40f, 0f);
            headerText = title.gameObject.AddComponent<TextMeshProUGUI>();
            headerText.text = "BUILDING CHAIN";
            headerText.fontSize = 18f;
            headerText.characterSpacing = 7f;
            headerText.color = Gold;
            headerText.alignment = TextAlignmentOptions.Left;
            headerText.raycastTarget = false;
            ApplyFont(headerText);

            var arrow = NewChild("Chevron", header);
            arrow.anchorMin = new Vector2(1f, 0f);
            arrow.anchorMax = new Vector2(1f, 1f);
            arrow.pivot = new Vector2(1f, 0.5f);
            arrow.offsetMin = new Vector2(-36f, 0f);
            arrow.offsetMax = new Vector2(-12f, 0f);
            chevron = arrow.gameObject.AddComponent<TextMeshProUGUI>();
            chevron.text = "▼";
            chevron.fontSize = 16f;
            chevron.color = Gold;
            chevron.alignment = TextAlignmentOptions.Center;
            chevron.raycastTarget = false;

            // ---- Die Glieder ----
            rows = NewChild("Links", panel);
            rows.anchorMin = new Vector2(0f, 1f);
            rows.anchorMax = new Vector2(1f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.offsetMin = new Vector2(8f, -(HeaderHeight + RowHeight));
            rows.offsetMax = new Vector2(-8f, -HeaderHeight);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            var fitter = rows.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ================== was die Engine ruft ==================

        public IEnumerator AddLink(CardInstance card, string cardName, string label, bool mine, int link)
        {
            // Glied 1 beginnt IMMER eine neue Kette. Was noch steht, gehört zur
            // vorigen und muss weg — auch wenn ShowChainEnd nie ankam, etwa weil
            // das Duell mitten in einer Auflösung endete. Ohne diese Zeile hängen
            // alte Zeilen an der nächsten Kette und die Anzeige öffnet sich für
            // etwas, das nie passiert ist.
            if (link <= 1 || resolving) { ClearRows(); resolving = false; }

            var row = BuildRow(card, cardName, label, mine, link);
            links.Add(row);
            Resize();

            // Ein einzelner Effekt ohne Antwort ist keine Kette. Das erste Glied
            // wird gemerkt, aber die Anzeige bleibt zu — erst das zweite macht
            // daraus etwas, das man erklären muss.
            if (links.Count < 2)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                yield break;
            }

            SetHeader("BUILDING CHAIN", Gold);
            if (links.Count == 2)
            {
                group.blocksRaycasts = true;
                yield return Fade(0f, 1f, 0.18f);
            }

            if (!expanded) yield break;

            // Das neue Glied wächst ein
            float t = 0f;
            row.Group.alpha = 0f;
            while (t < GrowTime)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / GrowTime);
                row.Group.alpha = p;
                row.Rect.localScale = new Vector3(Mathf.Lerp(0.94f, 1f, Ease(p)), 1f, 1f);
                yield return null;
            }
            row.Group.alpha = 1f;
            row.Rect.localScale = Vector3.one;
            SfxManager.Hover(SfxManager.ButtonHoverGain);
        }

        public IEnumerator Resolve(int link)
        {
            if (links.Count < 2) yield break;

            // Der Wechsel von Bauen auf Abbauen passiert genau einmal, beim
            // ersten aufgelösten Glied.
            if (!resolving)
            {
                resolving = true;
                yield return Wait(SetPause);
                SetHeader("RESOLVING", Ink);
                SfxManager.Click();
                yield return Wait(0.15f);
            }

            int index = link - 1;
            if (index < 0 || index >= links.Count) yield break;
            var row = links[index];

            // Das Glied, das gerade dran ist, leuchtet auf
            row.Background.color = new Color(Gold.r, Gold.g, Gold.b, 0.22f);
            row.Name.color = Ink;
            row.Number.color = Ink;
            float t = 0f;
            while (t < ResolveHold)
            {
                t += Time.unscaledDeltaTime;
                float pulse = 1f + 0.04f * Mathf.Sin(t / ResolveHold * Mathf.PI);
                row.Rect.localScale = new Vector3(pulse, pulse, 1f);
                yield return null;
            }
            row.Rect.localScale = Vector3.one;

            // …und wird danach ausgegraut, damit man sieht, was schon durch ist
            row.Background.color = new Color(0f, 0f, 0f, 0.28f);
            row.Name.color = InkDim;
            row.Number.color = InkDim;
            row.Label.color = InkDim;
            row.Accent.color = new Color(row.Accent.color.r, row.Accent.color.g, row.Accent.color.b, 0.3f);
        }

        public IEnumerator Finish()
        {
            if (links.Count >= 2 && group.alpha > 0f)
            {
                yield return Wait(0.25f);
                yield return Fade(1f, 0f, FadeOut);
            }
            ClearRows();
            resolving = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        // ================== Auf- und Zuklappen ==================

        /// <summary>Kopfzeile angeklickt: Glieder ein- oder ausblenden.</summary>
        public void Toggle()
        {
            expanded = !expanded;
            rows.gameObject.SetActive(expanded);
            chevron.text = expanded ? "▼" : "▶";
            Resize();
            SfxManager.Click();
        }

        private void SetHeader(string state, Color colour)
        {
            headerText.text = links.Count > 1 ? $"{state}   ·   {links.Count} LINKS" : state;
            headerText.color = colour;
        }

        // ================== Bauteile ==================

        private Row BuildRow(CardInstance card, string cardName, string label, bool mine, int link)
        {
            var go = new GameObject("Link" + link, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(rows, false);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;

            var row = new Row { Rect = rect };
            row.Group = go.AddComponent<CanvasGroup>();
            row.Background = go.AddComponent<Image>();
            row.Background.color = new Color(0f, 0f, 0f, 0.34f);
            // Muss Strahlen fangen, sonst kommt kein Hover an
            row.Background.raycastTarget = true;

            // Über den Namen fahren zeigt die Karte in der linken Vorschau
            var hover = go.AddComponent<ChainRowHover>();
            hover.Card = card;
            hover.Panel = detail;

            // Farbstreifen links: wer das Glied gelegt hat, sieht man sofort
            var accent = NewChild("Accent", rect);
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.offsetMin = new Vector2(0f, 4f);
            accent.offsetMax = new Vector2(4f, -4f);
            row.Accent = accent.gameObject.AddComponent<Image>();
            row.Accent.color = mine ? Mine : Theirs;
            row.Accent.raycastTarget = false;

            var number = NewChild("Number", rect);
            number.anchorMin = new Vector2(0f, 0f);
            number.anchorMax = new Vector2(0f, 1f);
            number.pivot = new Vector2(0f, 0.5f);
            number.offsetMin = new Vector2(12f, 0f);
            number.offsetMax = new Vector2(44f, 0f);
            row.Number = number.gameObject.AddComponent<TextMeshProUGUI>();
            row.Number.text = link.ToString();
            row.Number.fontSize = 22f;
            row.Number.color = Gold;
            row.Number.alignment = TextAlignmentOptions.Center;
            row.Number.raycastTarget = false;
            ApplyFont(row.Number);

            var name = NewChild("Name", rect);
            name.anchorMin = new Vector2(0f, 0.5f);
            name.anchorMax = new Vector2(1f, 1f);
            name.offsetMin = new Vector2(52f, 0f);
            name.offsetMax = new Vector2(-12f, -5f);
            row.Name = name.gameObject.AddComponent<TextMeshProUGUI>();
            row.Name.text = cardName;
            row.Name.enableAutoSizing = true;
            row.Name.fontSizeMin = 11f;
            row.Name.fontSizeMax = 16f;
            row.Name.color = Ink;
            row.Name.alignment = TextAlignmentOptions.BottomLeft;
            // Ohne NoWrap würde die Auto-Größe versuchen, in die halbe Zeilenhöhe
            // zu umbrechen, statt schmaler zu werden — und unten abgeschnitten.
            row.Name.textWrappingMode = TextWrappingModes.NoWrap;
            row.Name.overflowMode = TextOverflowModes.Ellipsis;
            row.Name.raycastTarget = false;
            ApplyFont(row.Name);

            var lab = NewChild("Label", rect);
            lab.anchorMin = new Vector2(0f, 0f);
            lab.anchorMax = new Vector2(1f, 0.5f);
            lab.offsetMin = new Vector2(52f, 5f);
            lab.offsetMax = new Vector2(-12f, 0f);
            row.Label = lab.gameObject.AddComponent<TextMeshProUGUI>();
            row.Label.text = string.IsNullOrEmpty(label) ? (mine ? "your effect" : "their effect") : label;
            row.Label.enableAutoSizing = true;
            row.Label.fontSizeMin = 9f;
            row.Label.fontSizeMax = 13f;
            row.Label.color = mine ? Mine : Theirs;
            row.Label.alignment = TextAlignmentOptions.TopLeft;
            row.Label.textWrappingMode = TextWrappingModes.NoWrap;
            row.Label.overflowMode = TextOverflowModes.Ellipsis;
            row.Label.raycastTarget = false;
            ApplyFont(row.Label);

            return row;
        }

        private void ClearRows()
        {
            foreach (var row in links)
                if (row.Rect != null) Destroy(row.Rect.gameObject);
            links.Clear();
            Resize();
        }

        /// <summary>Der Kasten folgt der Zahl der Glieder — und schrumpft zugeklappt auf die Kopfzeile.</summary>
        private void Resize()
        {
            float rowsHeight = expanded && links.Count > 0
                ? links.Count * RowHeight + (links.Count - 1) * RowSpacing + Padding
                : 0f;
            panel.sizeDelta = new Vector2(PanelWidth, HeaderHeight + rowsHeight);
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Ease(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            group.alpha = to;
        }

        private static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private static float Ease(float p) => p < 0.5f ? 2f * p * p : 1f - Mathf.Pow(-2f * p + 2f, 2f) * 0.5f;

        private static RectTransform NewChild(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyFont(TMP_Text label)
        {
            var skin = TransitionSkin.Load();
            if (skin != null && skin.oswald != null) label.font = skin.oswald;
        }
    }

    /// <summary>
    /// Zeigt die Karte einer Kettenzeile in der linken Vorschau, solange der
    /// Zeiger darauf steht. Eigene Klasse, weil ein Kettenglied auch von einer
    /// Karte stammen kann, die gar nicht auf dem Brett liegt — eine Handkarte
    /// des Gegners etwa. Über das Brett wäre sie nicht erreichbar.
    /// </summary>
    public class ChainRowHover : MonoBehaviour, IPointerEnterHandler
    {
        public CardInstance Card;
        public CardDetailPanel Panel;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Card != null && Panel != null) Panel.ShowCard(Card);
        }
    }
}
