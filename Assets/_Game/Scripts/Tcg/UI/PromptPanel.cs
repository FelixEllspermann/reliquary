using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Zentrales Prompt-Panel (Master-Duel-Stil): Ja/Nein-Fragen und Options-Listen.
    /// Die Buttons sind im Editor vorplatziert und werden zur Laufzeit beschriftet.
    /// </summary>
    public class PromptPanel : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelLabel;

        private Action<int> onResult;

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                    optionButtons[i].onClick.AddListener(() => Resolve(index));
            }
            if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(-1));
            if (panelRoot != null) panelRoot.SetActive(false);

            // Kartennamen in Titel/Frage/Optionen hoverbar machen
            CardLinkText.Attach(titleText);
            CardLinkText.Attach(questionText);
            foreach (var label in optionLabels) CardLinkText.Attach(label);
        }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void ShowYesNo(string title, string question, Action<bool> callback)
        {
            var options = new List<string> { "Yes", "No" };
            ShowOptions(title, question, options, false, index => callback?.Invoke(index == 0));
        }

        /// <summary>Ja/Nein mit Kartenbild: die fragende Karte schwebt über dem Fenster.</summary>
        public void ShowYesNo(string title, string question, CardInstance card, Action<bool> callback)
        {
            ShowYesNo(title, question, callback);
            ShowPromptCard(card);
        }

        // ---- Mini-Karte über dem Prompt-Fenster (Master-Duel-Stil) ----
        private RectTransform promptCardCell;

        private void ShowPromptCard(CardInstance card)
        {
            if (card == null || CardViewPrefab == null || panelRoot == null) return;
            if (promptCardCell == null)
            {
                promptCardCell = new GameObject("PromptCard", typeof(RectTransform)).GetComponent<RectTransform>();
                promptCardCell.SetParent(panelRoot.transform, false);
                promptCardCell.anchorMin = promptCardCell.anchorMax = new Vector2(0.5f, 1f);
                promptCardCell.pivot = new Vector2(0.5f, 0f);
                promptCardCell.sizeDelta = new Vector2(104f, 146f);
                promptCardCell.anchoredPosition = new Vector2(0f, 10f);
            }
            for (int i = promptCardCell.childCount - 1; i >= 0; i--)
                Destroy(promptCardCell.GetChild(i).gameObject);
            var view = Instantiate(CardViewPrefab, promptCardCell);
            var viewRect = (RectTransform)view.transform;
            viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero; viewRect.offsetMax = Vector2.zero;
            view.Show(card, false, upright: true, revealFaceDown: true);
            view.SetHighlight(false);
            foreach (var g in view.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            promptCardCell.gameObject.SetActive(true);
        }

        public void ShowOptions(string title, string question, List<string> options, bool allowCancel, Action<int> callback)
        {
            // Kein Karten-Rest vom letzten Prompt — wer eine will, zeigt sie danach
            if (promptCardCell != null) promptCardCell.gameObject.SetActive(false);
            onResult = callback;
            if (titleText != null) titleText.text = CardLinkText.Linkify(title);
            if (questionText != null) questionText.text = CardLinkText.Linkify(question ?? "");

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool used = i < options.Count;
                if (optionButtons[i] != null) optionButtons[i].gameObject.SetActive(used);
                if (used && i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = CardLinkText.Linkify(options[i]);
            }

            if (cancelButton != null) cancelButton.gameObject.SetActive(allowCancel);
            if (cancelLabel != null) cancelLabel.text = "Cancel";
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            onResult = null;
        }

        private void Resolve(int index)
        {
            var callback = onResult;
            Hide();
            callback?.Invoke(index);
        }

        // ================== KARTEN-LISTE & NAMENSSUCHE (Master-Duel-Stil) ==================
        // Beides zur Laufzeit gebaut — die Szene bleibt unangetastet. Die Karten-
        // liste ersetzt die alten Einzel-Ja/Nein-Fragen der Reaktionsfenster; die
        // Namenssuche gehört zu "declare a card name" (The Forbidden Name).

        /// <summary>Karten-Prefab für die Mini-Vorschauen — setzt der DuelUIController beim Start.</summary>
        public static TcgCardView CardViewPrefab;

        private RectTransform listRoot;
        private TMP_Text listTitle;
        private RectTransform listContent;
        private ScrollRect listScroll;
        private Button listPassButton;
        private TMP_Text listPassLabel;
        private TMP_InputField searchField;
        private Action<int> listCallback;
        private List<string> searchPool;

        public bool IsListOpen => listRoot != null && listRoot.gameObject.activeSelf;

        private void EnsureListRoot()
        {
            if (listRoot != null) return;
            var parent = panelRoot != null ? panelRoot.transform.parent : transform;

            listRoot = new GameObject("EffectListWindow", typeof(RectTransform)).GetComponent<RectTransform>();
            listRoot.SetParent(parent, false);
            listRoot.anchorMin = listRoot.anchorMax = new Vector2(0.5f, 0.5f);
            listRoot.pivot = new Vector2(0.5f, 0.5f);
            listRoot.sizeDelta = new Vector2(520f, 560f);
            var bg = listRoot.gameObject.AddComponent<Image>();
            bg.color = new Color(0.055f, 0.05f, 0.04f, 0.97f);

            var edge = new GameObject("Edge", typeof(RectTransform)).GetComponent<RectTransform>();
            edge.SetParent(listRoot, false);
            edge.anchorMin = new Vector2(0f, 1f); edge.anchorMax = Vector2.one;
            edge.offsetMin = new Vector2(0f, -3f); edge.offsetMax = Vector2.zero;
            edge.gameObject.AddComponent<Image>().color = new Color(0.784f, 0.643f, 0.361f, 0.9f);

            listTitle = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var titleRect = (RectTransform)listTitle.transform;
            titleRect.SetParent(listRoot, false);
            titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(18f, -46f); titleRect.offsetMax = new Vector2(-18f, -10f);
            listTitle.fontSize = 20f;
            listTitle.color = new Color(0.945f, 0.905f, 0.823f);
            listTitle.alignment = TextAlignmentOptions.MidlineLeft;
            if (titleText != null) listTitle.font = titleText.font;

            // Suchfeld (nur die Namenssuche blendet es ein)
            var searchGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            searchGo.name = "Search";
            var searchRect = (RectTransform)searchGo.transform;
            searchRect.SetParent(listRoot, false);
            searchRect.anchorMin = new Vector2(0f, 1f); searchRect.anchorMax = Vector2.one;
            searchRect.offsetMin = new Vector2(18f, -92f); searchRect.offsetMax = new Vector2(-18f, -54f);
            searchField = searchGo.GetComponent<TMP_InputField>();
            var searchBg = searchGo.GetComponent<Image>();
            if (searchBg != null) searchBg.color = new Color(0.12f, 0.11f, 0.09f, 1f);
            if (searchField.textComponent != null)
            {
                searchField.textComponent.color = new Color(0.945f, 0.905f, 0.823f);
                searchField.textComponent.fontSize = 16f;
                if (questionText != null) searchField.textComponent.font = questionText.font;
            }
            if (searchField.placeholder is TMP_Text ph)
            {
                ph.text = "Type to search card names…";
                ph.color = new Color(0.55f, 0.5f, 0.42f);
                if (questionText != null) ph.font = questionText.font;
            }
            searchField.onValueChanged.AddListener(_ => RebuildSearchRows());

            // Scrollbare Liste
            var scrollGo = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollGo.SetParent(listRoot, false);
            scrollGo.anchorMin = Vector2.zero; scrollGo.anchorMax = Vector2.one;
            scrollGo.offsetMin = new Vector2(12f, 64f); scrollGo.offsetMax = new Vector2(-12f, -96f);
            listScroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            listScroll.horizontal = false;
            listScroll.vertical = true;
            listScroll.movementType = ScrollRect.MovementType.Clamped;
            listScroll.scrollSensitivity = 30f;

            var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
            viewport.SetParent(scrollGo, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = Color.clear;   // fängt das Mausrad
            listScroll.viewport = viewport;

            listContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            listContent.SetParent(viewport, false);
            listContent.anchorMin = new Vector2(0f, 1f); listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = Vector2.zero; listContent.offsetMax = Vector2.zero;
            var layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(4, 4, 4, 4);
            var fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listScroll.content = listContent;

            // Pass/Cancel unten
            var passGo = new GameObject("Pass", typeof(RectTransform)).GetComponent<RectTransform>();
            passGo.SetParent(listRoot, false);
            passGo.anchorMin = new Vector2(0.5f, 0f); passGo.anchorMax = new Vector2(0.5f, 0f);
            passGo.pivot = new Vector2(0.5f, 0f);
            passGo.sizeDelta = new Vector2(220f, 44f);
            passGo.anchoredPosition = new Vector2(0f, 12f);
            var passImg = passGo.gameObject.AddComponent<Image>();
            passImg.color = new Color(0.30f, 0.26f, 0.19f, 1f);
            listPassButton = passGo.gameObject.AddComponent<Button>();
            listPassButton.onClick.AddListener(() => ResolveList(-1));
            listPassLabel = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var passLabelRect = (RectTransform)listPassLabel.transform;
            passLabelRect.SetParent(passGo, false);
            passLabelRect.anchorMin = Vector2.zero; passLabelRect.anchorMax = Vector2.one;
            passLabelRect.offsetMin = Vector2.zero; passLabelRect.offsetMax = Vector2.zero;
            listPassLabel.fontSize = 17f;
            listPassLabel.alignment = TextAlignmentOptions.Center;
            listPassLabel.color = new Color(0.945f, 0.905f, 0.823f);
            if (titleText != null) listPassLabel.font = titleText.font;

            listRoot.gameObject.SetActive(false);
        }

        private void ClearListRows()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);
        }

        private void ResolveList(int index)
        {
            var callback = listCallback;
            HideList();
            callback?.Invoke(index);
        }

        public void HideList()
        {
            if (listRoot != null) listRoot.gameObject.SetActive(false);
            listCallback = null;
            searchPool = null;
        }

        /// <summary>
        /// Master-Duel-Reaktionsliste: alle aktivierbaren Effekte auf einmal,
        /// jeder als Zeile mit Mini-Karte. Klick aktiviert, der Knopf unten passt.
        /// </summary>
        public void ShowCardList(string title, List<string> labels, List<CardInstance> cards,
            string passLabel, Action<int> callback)
        {
            EnsureListRoot();
            ClearListRows();
            listCallback = callback;
            listTitle.text = CardLinkText.Linkify(title ?? "");
            searchField.gameObject.SetActive(false);
            listScroll.viewport.offsetMax = Vector2.zero;
            ((RectTransform)listScroll.transform).offsetMax = new Vector2(-12f, -56f);
            listPassLabel.text = passLabel;
            listPassButton.gameObject.SetActive(true);

            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                var card = cards != null && i < cards.Count ? cards[i] : null;

                var row = new GameObject("Row" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(listContent, false);
                var rowLayout = row.gameObject.AddComponent<LayoutElement>();
                rowLayout.preferredHeight = 116f;
                var rowImg = row.gameObject.AddComponent<Image>();
                rowImg.color = new Color(0.115f, 0.105f, 0.085f, 1f);
                var rowButton = row.gameObject.AddComponent<Button>();
                var colors = rowButton.colors;
                colors.highlightedColor = new Color(1.25f, 1.2f, 1.05f);
                rowButton.colors = colors;
                rowButton.onClick.AddListener(() => ResolveList(index));

                // Mini-Karte links — gross genug, um sie zu erkennen
                if (card != null && CardViewPrefab != null)
                {
                    var cell = new GameObject("Card", typeof(RectTransform)).GetComponent<RectTransform>();
                    cell.SetParent(row, false);
                    cell.anchorMin = new Vector2(0f, 0.5f); cell.anchorMax = new Vector2(0f, 0.5f);
                    cell.pivot = new Vector2(0f, 0.5f);
                    cell.sizeDelta = new Vector2(76f, 106f);
                    cell.anchoredPosition = new Vector2(8f, 0f);
                    var view = Instantiate(CardViewPrefab, cell);
                    var viewRect = (RectTransform)view.transform;
                    viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
                    viewRect.offsetMin = Vector2.zero; viewRect.offsetMax = Vector2.zero;
                    // Die Liste zeigt die EIGENEN aktivierbaren Karten — auch gesetzte offen
                    view.Show(card, false, upright: true, revealFaceDown: true);
                    view.SetHighlight(false);
                    foreach (var g in view.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
                }

                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                var labelRect = (RectTransform)label.transform;
                labelRect.SetParent(row, false);
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(card != null && CardViewPrefab != null ? 94f : 14f, 8f);
                labelRect.offsetMax = new Vector2(-10f, -8f);
                label.fontSize = 15.5f;
                label.color = new Color(0.93f, 0.89f, 0.80f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.raycastTarget = false;
                if (questionText != null) label.font = questionText.font;
                label.text = CardLinkText.Linkify(labels[i]);
            }

            Canvas.ForceUpdateCanvases();
            listScroll.verticalNormalizedPosition = 1f;
            listRoot.gameObject.SetActive(true);
        }

        /// <summary>
        /// Namenssuche (The Forbidden Name): Suchfeld oben, Treffer darunter.
        /// Der Callback erhält den Index im URSPRÜNGLICHEN Namens-Array.
        /// </summary>
        public void ShowSearchList(string title, List<string> options, Action<int> callback)
        {
            EnsureListRoot();
            ClearListRows();
            listCallback = callback;
            searchPool = options;
            listTitle.text = title ?? "";
            searchField.gameObject.SetActive(true);
            searchField.SetTextWithoutNotify("");
            ((RectTransform)listScroll.transform).offsetMax = new Vector2(-12f, -96f);
            listPassButton.gameObject.SetActive(false); // Namenswahl ist Pflicht
            RebuildSearchRows();
            listRoot.gameObject.SetActive(true);
            searchField.Select();
            searchField.ActivateInputField();
        }

        private void RebuildSearchRows()
        {
            if (searchPool == null || listContent == null) return;
            ClearListRows();
            string query = (searchField.text ?? "").Trim();
            int shown = 0;
            const int maxRows = 40;   // mehr braucht niemand zu scrollen — tippen filtert
            for (int i = 0; i < searchPool.Count && shown < maxRows; i++)
            {
                if (query.Length > 0 && searchPool[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                int index = i;
                shown++;

                var row = new GameObject("Name" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(listContent, false);
                var rowLayout = row.gameObject.AddComponent<LayoutElement>();
                rowLayout.preferredHeight = 36f;
                var rowImg = row.gameObject.AddComponent<Image>();
                rowImg.color = new Color(0.115f, 0.105f, 0.085f, 1f);
                var rowButton = row.gameObject.AddComponent<Button>();
                rowButton.onClick.AddListener(() => ResolveList(index));

                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                var labelRect = (RectTransform)label.transform;
                labelRect.SetParent(row, false);
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 0f); labelRect.offsetMax = new Vector2(-8f, 0f);
                label.fontSize = 15f;
                label.color = new Color(0.93f, 0.89f, 0.80f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
                if (questionText != null) label.font = questionText.font;
                label.text = searchPool[index];
            }
            Canvas.ForceUpdateCanvases();
            if (listScroll != null) listScroll.verticalNormalizedPosition = 1f;
        }
    }
}
