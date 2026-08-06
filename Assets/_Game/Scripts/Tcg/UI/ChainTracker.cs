using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die Kettenanzeige an der rechten Seite des Duells.
    ///
    /// <para>
    /// <b>Warum es die braucht.</b> Die Engine führt keine Kette als Liste: eine
    /// Aktivierung öffnet ein Reaktionsfenster, darin läuft die nächste
    /// Aktivierung vollständig durch, und die Reihenfolge steckt allein im
    /// Aufrufstapel. Für den Spieler passierte dadurch alles auf einmal und in
    /// einer Reihenfolge, die er nicht sehen konnte. Diese Anzeige ist die
    /// einzige Stelle, an der die Kette als Kette sichtbar wird.
    /// </para>
    /// <para>
    /// <b>Bauen und Abbauen sind getrennt.</b> Solange Glieder dazukommen, steht
    /// oben BUILDING CHAIN. Sobald das erste Glied auflöst, hält die Anzeige
    /// einen Moment inne, wechselt auf RESOLVING und arbeitet dann von unten
    /// nach oben ab. Diese Pause ist kein Trick — sie liegt genau dort, wo die
    /// Engine aufhört zu wachsen und anfängt aufzulösen.
    /// </para>
    /// <para>
    /// Eine Kette hat höchstens drei Glieder: das Reaktionsfenster steigt bei
    /// Tiefe 2 aus (<c>DuelActions.OpenResponseWindow</c>). Die Anzeige ist
    /// darauf ausgelegt, kommt aber auch mit mehr zurecht.
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

        // Masse des Kastens. Die Höhe steht nicht fest — sie folgt der Zahl der
        // Glieder, sonst klafft unter einer Zweierkette ein leeres Feld.
        private const float PanelWidth = 292f;
        private const float HeaderBlock = 52f;   // Kopfzeile bis zur ersten Zeile
        private const float RowHeight = 52f;
        private const float RowSpacing = 6f;
        private const float Padding = 14f;

        private static readonly Color Ink = Hex("#F3DDA4");
        private static readonly Color InkDim = Hex("#7E7059");
        private static readonly Color Gold = Hex("#C8A45C");
        private static readonly Color Mine = Hex("#6FD3E0");
        private static readonly Color Theirs = Hex("#E0603A");
        private static readonly Color PanelBg = new Color(0.03f, 0.026f, 0.02f, 0.88f);

        private RectTransform panel;
        private TMP_Text headerText;
        private RectTransform rows;
        private CanvasGroup group;

        private readonly List<Row> links = new List<Row>();
        private bool resolving;

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

        /// <summary>Hängt die Anzeige an einen Canvas. Sie beginnt unsichtbar.</summary>
        public static ChainTracker Create(RectTransform canvas, CardSkin skin)
        {
            var go = new GameObject("ChainTracker", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            // Linke Leiste, in die Lücke zwischen Inspect-Karte und Legende.
            //
            // Ausgemessen im Duell-Canvas (1920x1080): die Inspect-Karte endet
            // bei y 600, der Legendentext beginnt bei y 175 — dazwischen ist die
            // einzige grössere Fläche, die nichts belegt. Rechts geht nicht: die
            // RightRail liegt dort über die volle Höhe (LP, End Turn, Surrender,
            // Log), und das sind genau die Knöpfe, die man während einer Kette
            // braucht. Das PromptPanel (x 690..1230) bleibt ebenfalls frei.
            // Oben verankert, damit der Kasten nach UNTEN wächst: die Oberkante
            // bleibt stehen, wo sie ist, und ein neues Glied schiebt nichts weg,
            // was der Spieler gerade liest.
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -494f);   // 14 px Luft unter der Inspect-Karte
            rect.sizeDelta = new Vector2(PanelWidth, HeaderBlock + Padding);
            rect.SetAsLastSibling();

            var tracker = go.AddComponent<ChainTracker>();
            tracker.Build(skin);
            return tracker;
        }

        private void Build(CardSkin skin)
        {
            panel = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var bg = NewChild("Backdrop", panel).gameObject.AddComponent<Image>();
            Stretch((RectTransform)bg.transform);
            bg.color = PanelBg;
            bg.raycastTarget = false;

            // Der 9-Slice-Rahmen nur, wenn ein Skin da ist. Ohne Sprite wuerde
            // ein Sliced-Image als volle Flaeche zeichnen und alles zudecken —
            // dann lieber eine Goldkante oben.
            if (skin != null && skin.relicFrame != null)
            {
                var frame = NewChild("Frame", panel).gameObject.AddComponent<Image>();
                Stretch((RectTransform)frame.transform);
                frame.sprite = skin.relicFrame;
                frame.type = Image.Type.Sliced;
                frame.color = new Color(Gold.r, Gold.g, Gold.b, 0.7f);
                frame.raycastTarget = false;
            }
            else
            {
                var edge = NewChild("Edge", panel);
                edge.anchorMin = new Vector2(0f, 1f);
                edge.anchorMax = new Vector2(1f, 1f);
                edge.pivot = new Vector2(0.5f, 1f);
                edge.offsetMin = new Vector2(0f, -3f);
                edge.offsetMax = Vector2.zero;
                var line = edge.gameObject.AddComponent<Image>();
                line.color = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
                line.raycastTarget = false;
            }

            var header = NewChild("Header", panel);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(16f, -46f);
            header.offsetMax = new Vector2(-16f, -12f);
            headerText = header.gameObject.AddComponent<TextMeshProUGUI>();
            headerText.text = "BUILDING CHAIN";
            headerText.fontSize = 19f;
            headerText.characterSpacing = 8f;
            headerText.color = Gold;
            headerText.alignment = TextAlignmentOptions.Left;
            headerText.raycastTarget = false;
            ApplyFont(headerText);

            rows = NewChild("Links", panel);
            rows.anchorMin = new Vector2(0f, 1f);
            rows.anchorMax = new Vector2(1f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.offsetMin = new Vector2(12f, -(HeaderBlock + RowHeight));
            rows.offsetMax = new Vector2(-12f, -HeaderBlock);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            var fitter = rows.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ================== was die Engine ruft ==================

        public IEnumerator AddLink(string cardName, string label, bool mine, int link)
        {
            // Ein einzelner Effekt ohne Antwort ist keine Kette. Das erste Glied
            // wird gemerkt, aber die Anzeige bleibt zu — erst das zweite macht
            // daraus etwas, das man erklären muss.
            if (resolving) { ClearRows(); resolving = false; }

            var row = BuildRow(cardName, label, mine, link);
            links.Add(row);
            Resize();

            if (links.Count < 2)
            {
                group.alpha = 0f;
                yield break;
            }

            headerText.text = "BUILDING CHAIN";
            headerText.color = Gold;
            if (links.Count == 2) yield return Fade(0f, 1f, 0.18f);

            // Das neue Glied wächst ein
            float t = 0f;
            row.Group.alpha = 0f;
            while (t < GrowTime)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / GrowTime);
                row.Group.alpha = p;
                row.Rect.localScale = new Vector3(Mathf.Lerp(0.9f, 1f, Ease(p)), 1f, 1f);
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
                headerText.text = "RESOLVING";
                headerText.color = Ink;
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
        }

        // ================== Bauteile ==================

        private Row BuildRow(string cardName, string label, bool mine, int link)
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
            row.Background.raycastTarget = false;

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
            name.offsetMin = new Vector2(50f, 0f);
            name.offsetMax = new Vector2(-10f, -6f);
            row.Name = name.gameObject.AddComponent<TextMeshProUGUI>();
            row.Name.text = cardName;
            // Auto-Größe statt fester Punktzahl: die Zeile ist nur 208 px breit,
            // und Kartennamen wie „Sleightwind Cardsharp" sind lang. Lieber ein
            // Stück kleiner als hinten abgeschnitten.
            row.Name.enableAutoSizing = true;
            row.Name.fontSizeMin = 11f;
            row.Name.fontSizeMax = 16f;
            row.Name.color = Ink;
            row.Name.alignment = TextAlignmentOptions.BottomLeft;
            // Ohne NoWrap würde die Auto-Größe versuchen, in die 26 px hohe Zeile
            // zu umbrechen, statt schmaler zu werden — und unten abgeschnitten.
            row.Name.textWrappingMode = TextWrappingModes.NoWrap;
            row.Name.overflowMode = TextOverflowModes.Ellipsis;
            row.Name.raycastTarget = false;
            ApplyFont(row.Name);

            var lab = NewChild("Label", rect);
            lab.anchorMin = new Vector2(0f, 0f);
            lab.anchorMax = new Vector2(1f, 0.5f);
            lab.offsetMin = new Vector2(50f, 6f);
            lab.offsetMax = new Vector2(-10f, 0f);
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

        /// <summary>Der Kasten folgt der Zahl der Glieder statt fester Höhe.</summary>
        private void Resize()
        {
            float rowsHeight = links.Count > 0
                ? links.Count * RowHeight + (links.Count - 1) * RowSpacing
                : 0f;
            panel.sizeDelta = new Vector2(PanelWidth, HeaderBlock + rowsHeight + Padding);
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
}
