using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Feedback-Overlay im Reliquary-Stil: freier Text, geht per WebSocket an den Server
    /// und landet dort in data/feedback.jsonl (mit Account, Zeit und Build-Version).
    /// Öffnet über den Feedback-Button in der TopBar; Scrim-Klick oder CLOSE schließt.
    /// </summary>
    public class FeedbackPanel : MonoBehaviour
    {
        [Header("Verdrahtung")]
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject overlay;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button scrimButton;
        [SerializeField] private Button closeButton;

        [Header("Inhalt")]
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button sendButton;
        [SerializeField] private TMP_Text sendLabel;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private TMP_Text statusText;

        [Header("Verhalten")]
        [Range(0.05f, 0.6f)] [SerializeField] private float fadeDuration = 0.16f;
        [SerializeField] private int maxLength = 1000;

        private Coroutine animRoutine;
        private bool awaitingReply;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (scrimButton != null) scrimButton.onClick.AddListener(Close);
            if (sendButton != null) sendButton.onClick.AddListener(Send);
            if (input != null)
            {
                input.characterLimit = maxLength;
                input.onValueChanged.AddListener(_ => RefreshState());
            }
            if (overlay != null) overlay.SetActive(false);
        }

        private void OnEnable()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.OnMessage += HandleMessage;
        }

        private void OnDisable()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.OnMessage -= HandleMessage;
        }

        public void Open()
        {
            if (overlay == null) return;
            overlay.SetActive(true);
            awaitingReply = false;
            if (input != null) input.text = "";
            SetStatus(PlayerProfile.LoggedIn
                ? "Bugs, Kartenideen, alles willkommen — das landet direkt beim Entwickler."
                : "Du musst eingeloggt sein, um Feedback zu senden.");
            RefreshState();
            StartAnim(true);
            if (input != null) input.ActivateInputField();
        }

        public void Close() => StartAnim(false);

        private bool CanSend =>
            !awaitingReply
            && PlayerProfile.LoggedIn
            && NetworkManager.Instance != null && NetworkManager.Instance.IsConnected
            && input != null && input.text.Trim().Length >= 3;

        private void RefreshState()
        {
            if (counterText != null && input != null)
                counterText.text = $"{input.text.Length} / {maxLength}";
            if (sendButton != null)
            {
                sendButton.interactable = CanSend;
                // Gold-CTA nur im aktiven Zustand — sonst abgedunkelt, damit klar ist: geht gerade nicht
                if (sendButton.targetGraphic is Image sendBg)
                    sendBg.color = CanSend ? Color.white : new Color(0.28f, 0.24f, 0.18f, 0.55f);
            }
            if (sendLabel != null)
                sendLabel.color = CanSend ? new Color32(0x1E, 0x14, 0x05, 0xFF) : new Color32(0x8C, 0x7B, 0x5F, 0xFF);
        }

        private void Send()
        {
            if (!CanSend) return;
            awaitingReply = true;
            RefreshState();
            SetStatus("Sending…");
            NetworkManager.Instance.SendFeedback(input.text.Trim());
        }

        private void HandleMessage(NetMessage message)
        {
            if (!awaitingReply) return;
            if (message.t == "feedback_ok")
            {
                awaitingReply = false;
                if (input != null) input.text = "";
                SetStatus("Thank you — your feedback came through.");
                RefreshState();
            }
            else if (message.t == "error")
            {
                awaitingReply = false;
                SetStatus(string.IsNullOrEmpty(message.msg) ? "Couldn't send. Try again." : message.msg);
                RefreshState();
            }
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        private void StartAnim(bool show)
        {
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Animate(show));
        }

        private IEnumerator Animate(bool show)
        {
            float from = overlayGroup != null ? overlayGroup.alpha : (show ? 0f : 1f);
            float to = show ? 1f : 0f;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeDuration;
                if (overlayGroup != null) overlayGroup.alpha = Mathf.Lerp(from, to, k);
                if (panel != null)
                {
                    float s = show ? Mathf.Lerp(0.96f, 1f, k) : Mathf.Lerp(1f, 0.97f, k);
                    panel.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
            if (overlayGroup != null) overlayGroup.alpha = to;
            if (panel != null) panel.localScale = Vector3.one;
            if (!show && overlay != null) overlay.SetActive(false);
            animRoutine = null;
        }
    }
}
