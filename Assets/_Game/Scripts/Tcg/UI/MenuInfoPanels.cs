using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die beiden Info-Ansichten des Hauptmenüs: NEWS zeigt die Patch Notes aus
    /// Resources/PatchNotes.txt, BANLIST die vom Server gelieferten Limits.
    /// Das Overlay entsteht zur Laufzeit im gleichen Reliquary-Stil wie das
    /// Einstellungs- und das Feedback-Fenster (Rahmen, Innenkeyline, Kopfdiamant,
    /// Cinzel-Titel über einer goldenen Trennlinie).
    /// </summary>
    public class MenuInfoPanels : MonoBehaviour
    {
        [Header("Verdrahtung")]
        [SerializeField] private Button newsButton;
        [SerializeField] private Button banlistButton;

        [Tooltip("Einladung zur Community — leer = kein Discord-Knopf")]
        [SerializeField] private string discordUrl = "https://discord.gg/wdzu3SBbtK";

        [Tooltip("Katalog, um Banlist-Einträge nach Kartentyp einzufärben")]
        [SerializeField] private CardCatalog catalog;

        [Tooltip("Kopiervorlage für den Schließen-Knopf — irgendein Knopf im Reliquary-Stil")]
        [SerializeField] private Button buttonTemplate;

        [Tooltip("Sprites und Schriften des Reliquary-Stils")]
        [SerializeField] private CardSkin skin;

        private const float PanelWidth = 900f;
        private const float PanelHeight = 660f;

        private GameObject overlay;
        private TMP_Text titleText;
        private TMP_Text subText;
        private TMP_Text bodyText;
        private ScrollRect scroll;

        private void Awake()
        {
            if (newsButton != null) newsButton.onClick.AddListener(ShowNews);
            if (banlistButton != null) banlistButton.onClick.AddListener(ShowBanlist);
            if (buttonTemplate == null) buttonTemplate = newsButton;
            BuildDiscordButton();
        }

        /// <summary>
        /// DISCORD-Knopf links neben BANLIST — Klon des News-Knopfs, damit er
        /// automatisch im Topbar-Stil sitzt. Öffnet die Einladung im Browser.
        /// </summary>
        private void BuildDiscordButton()
        {
            if (string.IsNullOrEmpty(discordUrl) || banlistButton == null || newsButton == null) return;
            // Unten in die BottomRail, direkt rechts neben das Daily-Claim-Panel
            var dailyPanel = GameObject.Find("DailyPanel");
            var parent = dailyPanel != null ? dailyPanel.transform.parent : newsButton.transform.parent;
            var clone = Instantiate(newsButton.gameObject, parent);
            clone.name = "DiscordButton";
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(128f, 48f);
            if (dailyPanel != null)
            {
                var daily = (RectTransform)dailyPanel.transform;
                rect.anchoredPosition = new Vector2(
                    daily.anchoredPosition.x + daily.sizeDelta.x + 16f,
                    daily.anchoredPosition.y + daily.sizeDelta.y * 0.5f);
            }
            else rect.anchoredPosition = new Vector2(640f, 72f);

            var label = clone.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "DISCORD";
            var button = clone.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            string url = discordUrl;
            button.onClick.AddListener(() =>
            {
                SfxManager.Click();
                Application.OpenURL(url);
            });
        }

        // ================== INHALTE ==================

        public void ShowNews()
        {
            var notes = Resources.Load<TextAsset>("PatchNotes");
            Show("NEWS", "PATCH NOTES · WHAT CHANGED", notes != null
                ? Colourise(notes.text)
                : "<color=#8C7B5F>No patch notes found.</color>");
        }

        public void ShowBanlist()
        {
            Show("BANLIST", "CARD LIMITS · ENFORCED WHEN A DECK IS SAVED",
                BuildBanlistText() + BuildHistoryText());
        }

        /// <summary>Überschriften und Aufzählungen der Patch Notes hervorheben.</summary>
        private static string Colourise(string raw)
        {
            var lines = raw.Replace("\r", "").Split('\n');
            var builder = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                string trimmed = line.TrimEnd();
                if (trimmed.Length == 0) { builder.Append('\n'); continue; }

                if (trimmed.StartsWith("─"))
                    builder.Append("<color=#C8A45C66>").Append(trimmed).Append("</color>\n");
                else if (trimmed.StartsWith(" "))
                    builder.Append("<color=#C2B49B>").Append(trimmed).Append("</color>\n");
                else if (trimmed == trimmed.ToUpperInvariant())
                    builder.Append("<color=#EBCE8A><b>").Append(trimmed).Append("</b></color>\n");
                else
                    builder.Append("<color=#C2B49B>").Append(trimmed).Append("</color>\n");
            }
            return builder.ToString();
        }

        private string BuildBanlistText()
        {
            if (!PlayerProfile.LoggedIn)
                return "<color=#8C7B5F>Log in to see the current banlist.</color>";

            var builder = new System.Text.StringBuilder();
            int count = PlayerProfile.Banlist.Count;
            builder.Append(Heading("CURRENT LIST",
                count == 0 ? "empty" : count + (count == 1 ? " card restricted" : " cards restricted")));

            if (count == 0)
            {
                builder.Append("<color=#7ACD96><b>Nothing is banned or limited.</b></color>\n")
                       .Append("<color=#8C7B5F>Every card may be played up to ")
                       .Append(PlayerProfile.BanlistMaxCopies)
                       .Append(" copies per deck. The list opens empty and only grows if a card ")
                       .Append("proves it has to be here.</color>\n");
                return builder.ToString();
            }

            builder.Append("<color=#8C7B5F>Cards not listed here have no extra restriction.</color>\n");
            foreach (int limit in new[] { 0, 1, 2 })
            {
                var names = PlayerProfile.Banlist
                    .Where(kv => kv.Value == limit)
                    .Select(kv => kv.Key)
                    .OrderBy(n => n)
                    .ToList();
                if (names.Count == 0) continue;

                string hex = CollectionRow.RestrictionHex(limit);
                builder.Append("\n<size=110%><color=#").Append(hex).Append("><b>")
                       .Append(CollectionRow.RestrictionWord(limit))
                       .Append("</b></color></size>  <color=#8C7B5F>· ")
                       .Append(limit == 0 ? "may not be played" : "max " + limit + " per deck")
                       .Append(" · ").Append(names.Count).Append("</color>\n");

                foreach (var name in names) builder.Append(CardLine(limit, name));
            }
            return builder.ToString();
        }

        /// <summary>Die Chronik: jüngster Stand zuerst, jede Änderung mit altem und neuem Limit.</summary>
        private string BuildHistoryText()
        {
            if (!PlayerProfile.LoggedIn || PlayerProfile.BanlistHistory.Count == 0) return "";

            var builder = new System.Text.StringBuilder();
            builder.Append(Heading("HISTORY", PlayerProfile.BanlistHistory.Count == 1
                ? "1 revision" : PlayerProfile.BanlistHistory.Count + " revisions"));

            for (int i = PlayerProfile.BanlistHistory.Count - 1; i >= 0; i--)
            {
                var revision = PlayerProfile.BanlistHistory[i];
                builder.Append("\n<color=#EBCE8A><b>").Append(revision.Title).Append("</b></color>");
                if (!string.IsNullOrEmpty(revision.Date))
                    builder.Append("   <color=#8C7B5F>").Append(revision.Date).Append("</color>");
                builder.Append('\n');

                if (!string.IsNullOrEmpty(revision.Note))
                    builder.Append("<color=#8C7B5F>").Append(revision.Note).Append("</color>\n");

                if (revision.Changes.Count == 0)
                    builder.Append("<color=#5C513F>No changes.</color>\n");
                else
                    foreach (var change in revision.Changes)
                        builder.Append(CardLine(change.To, change.Card, change.From));
            }
            return builder.ToString();
        }

        /// <summary>Abschnittskopf mit goldener Trennlinie und Zusatz in Grau.</summary>
        private static string Heading(string title, string suffix) =>
            "\n<size=120%><color=#EBCE8A><b>" + title + "</b></color></size>"
            + "  <color=#8C7B5F>· " + suffix + "</color>\n";

        /// <summary>Eine Kartenzeile: Limit-Marke, Name in der Typfarbe, optional das alte Limit.</summary>
        private string CardLine(int limit, string name, int previous = -1)
        {
            var definition = catalog != null ? catalog.FindByName(name) : null;
            string ink = definition != null ? CardLinkText.InkFor(definition) : "C2B49B";
            // Ab dem normalen Kopienlimit ist die Karte frei — grün statt der Warnfarben
            string mark = limit >= PlayerProfile.BanlistMaxCopies
                ? "7ACD96" : CollectionRow.RestrictionHex(limit);
            var line = new System.Text.StringBuilder();
            line.Append("<color=#").Append(mark)
                .Append("><b>[").Append(limit).Append("]</b></color>  ")
                .Append("<color=#").Append(ink).Append(">").Append(name).Append("</color>");
            if (previous >= 0)
                line.Append("  <color=#5C513F>was ").Append(previous).Append("</color>");
            return line.Append('\n').ToString();
        }

        // ================== OVERLAY ==================

        private void Show(string title, string subtitle, string body)
        {
            EnsureOverlay();
            if (overlay == null) return;
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            if (titleText != null) titleText.text = title;
            if (subText != null) subText.text = subtitle;
            if (bodyText != null) bodyText.text = body;
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private void Hide()
        {
            if (overlay != null) overlay.SetActive(false);
        }

        private void EnsureOverlay()
        {
            if (overlay != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var root = MakeRect("MenuInfoOverlay", (RectTransform)canvas.transform);
            Stretch(root);
            overlay = root.gameObject;

            var scrim = MakeImage("Scrim", root, Hex("#05080D", 0.90f));
            Stretch((RectTransform)scrim.transform);
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Hide);
            scrim.gameObject.AddComponent<UiFxIgnore>();

            var panel = MakeRect("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panel.anchoredPosition = Vector2.zero;

            // Grund, Rahmen und Innenkeyline — exakt wie im Einstellungsfenster
            var background = MakeImage("BG", panel, Hex("#0E121B", 1f));
            Stretch((RectTransform)background.transform);
            background.raycastTarget = true;

            var frame = MakeImage("Frame", panel, Hex("#C8A45C", 1f));
            Stretch((RectTransform)frame.transform);
            Slice(frame);

            var innerFrame = MakeImage("InnerFrame", panel, Hex("#C8A45C", 0.27f));
            var innerRect = (RectTransform)innerFrame.transform;
            Stretch(innerRect);
            innerRect.offsetMin = new Vector2(8f, 8f);
            innerRect.offsetMax = new Vector2(-8f, -8f);
            Slice(innerFrame);

            var diamond = MakeImage("TopDiamond", panel, Hex("#EBCE8A", 1f));
            var diamondRect = (RectTransform)diamond.transform;
            diamondRect.anchorMin = diamondRect.anchorMax = new Vector2(0.5f, 1f);
            diamondRect.pivot = new Vector2(0.5f, 0.5f);
            diamondRect.sizeDelta = new Vector2(12f, 12f);
            diamondRect.anchoredPosition = Vector2.zero;
            diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            if (skin != null && skin.whiteSquare != null) diamond.sprite = skin.whiteSquare;
            diamond.raycastTarget = false;

            titleText = MakeText("Title", panel, 30f, skin != null ? skin.cinzelBold : null);
            PlaceTop((RectTransform)titleText.transform, 400f, 40f, 48f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Hex("#EBCE8A", 1f);

            var underline = MakeImage("Underline", panel, Hex("#C8A45C", 0.4f));
            PlaceTop((RectTransform)underline.transform, 340f, 1f, 72f);
            underline.raycastTarget = false;

            subText = MakeText("Sub", panel, 13f, skin != null ? skin.oswaldMedium : null);
            PlaceTop((RectTransform)subText.transform, 620f, 20f, 90f);
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = Hex("#8C7B5F", 1f);
            subText.characterSpacing = 6f;

            var scrollRect = MakeRect("Scroll", panel);
            Stretch(scrollRect);
            scrollRect.offsetMin = new Vector2(38f, 104f);
            scrollRect.offsetMax = new Vector2(-38f, -114f);
            scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            var viewport = MakeRect("Viewport", scrollRect);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            // Ohne eine Fläche, die Zeigerereignisse annimmt, landet das Mausrad auf
            // dem Panel-Hintergrund — und der liegt NEBEN der ScrollRect, nicht darin,
            // also erreicht das Ereignis sie nie. Eine durchsichtige Fläche im
            // Viewport hängt in der richtigen Kette und fängt Rad wie Ziehen ab.
            var catcher = MakeImage("Catcher", viewport, new Color(0f, 0f, 0f, 0f));
            Stretch((RectTransform)catcher.transform);
            catcher.raycastTarget = true;
            catcher.gameObject.AddComponent<UiFxIgnore>();

            bodyText = MakeText("Body", viewport, 17f, skin != null ? skin.spectral : null);
            var bodyRect = (RectTransform)bodyText.transform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.color = Hex("#C2B49B", 1f);
            bodyText.lineSpacing = 6f;
            bodyText.raycastTarget = false;   // der Fänger darunter nimmt die Ereignisse
            var fitter = bodyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = bodyRect;

            BuildScrollbar(scrollRect, viewport);

            BuildCloseButton(panel);
            overlay.SetActive(false);
        }

        /// <summary>
        /// Schlanker Balken am rechten Rand. Er ist nicht nur Bedienung, sondern
        /// die Anzeige, DASS es weitergeht — ohne ihn sieht ein abgeschnittener Text
        /// aus wie ein zu Ende gelesener.
        /// </summary>
        private void BuildScrollbar(RectTransform scrollRect, RectTransform viewport)
        {
            const float width = 5f;

            var barRect = MakeRect("Scrollbar", scrollRect);
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.offsetMin = new Vector2(-width, 0f);
            barRect.offsetMax = Vector2.zero;

            var track = MakeImage("Track", barRect, Hex("#C8A45C", 0.14f));
            Stretch((RectTransform)track.transform);
            track.raycastTarget = true;

            var bar = barRect.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.transition = Selectable.Transition.None;
            barRect.gameObject.AddComponent<UiFxIgnore>();

            var slidingArea = MakeRect("SlidingArea", barRect);
            Stretch(slidingArea);
            var handle = MakeImage("Handle", slidingArea, Hex("#C8A45C", 0.72f));
            var handleRect = (RectTransform)handle.transform;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            bar.targetGraphic = handle;
            bar.handleRect = handleRect;

            // Der Viewport muss dem Balken Platz lassen, sonst läuft der Text darunter
            viewport.offsetMax = new Vector2(-(width + 12f), viewport.offsetMax.y);

            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 0f;
        }

        private void BuildCloseButton(RectTransform panel)
        {
            if (buttonTemplate == null) return;
            var close = Instantiate(buttonTemplate.gameObject, panel);
            close.name = "CloseButton";
            foreach (var junk in new[] { "Glow", "~FxGlow", "Icon" })
            {
                var child = close.transform.Find(junk);
                if (child != null) Destroy(child.gameObject);
            }
            var inherited = close.GetComponent<UiButtonFx>();
            if (inherited != null) Destroy(inherited);

            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(140f, 44f);
            closeRect.anchoredPosition = new Vector2(0f, 64f);
            closeRect.localScale = Vector3.one;

            var closeButton = close.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.interactable = true;
                closeButton.onClick.AddListener(Hide);
            }
            var closeLabel = close.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null) { closeLabel.text = "CLOSE"; closeLabel.color = Hex("#D9C79B", 1f); }

            // Die Leisten-Knöpfe sind ember getönt; im Fenster gilt das Gold des Rahmens
            var closeFrame = close.transform.Find("Frame");
            if (closeFrame != null)
            {
                var frameImage = closeFrame.GetComponent<Image>();
                if (frameImage != null) frameImage.color = Hex("#C8A45C", 0.45f);
            }
        }

        // ---------- kleine Bau-Helfer ----------

        /// <summary>Mittig unter der Panel-Oberkante, Abstand von oben in Pixeln.</summary>
        private static void PlaceTop(RectTransform rect, float width, float height, float fromTop)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, -fromTop);
        }

        private void Slice(Image image)
        {
            image.raycastTarget = false;
            if (skin == null || skin.whiteFrame == null) return;
            image.sprite = skin.whiteFrame;
            image.type = Image.Type.Sliced;
        }

        private static Color Hex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            return color;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private Image MakeImage(string name, RectTransform parent, Color color)
        {
            var image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            if (skin != null && skin.whiteSquare != null) image.sprite = skin.whiteSquare;
            return image;
        }

        private TMP_Text MakeText(string name, RectTransform parent, float size, TMP_FontAsset font)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            else
            {
                var sample = newsButton != null ? newsButton.GetComponentInChildren<TMP_Text>(true) : null;
                if (sample != null) { text.font = sample.font; text.fontSharedMaterial = sample.fontSharedMaterial; }
            }
            text.fontSize = size;
            text.raycastTarget = false;
            text.richText = true;
            return text;
        }
    }
}
