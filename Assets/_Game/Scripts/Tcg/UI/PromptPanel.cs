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

        public void ShowOptions(string title, string question, List<string> options, bool allowCancel, Action<int> callback)
        {
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
    }
}
