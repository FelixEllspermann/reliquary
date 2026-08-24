using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Lebt einmal pro Sitzung (DontDestroyOnLoad) und lauscht auf das, was
    /// jederzeit hereinschneien kann: eingehende Duell-Herausforderungen
    /// (Popup mit ANNEHMEN/ABLEHNEN), Freundschaftsanfragen (kurzer Toast)
    /// und den Duell-Start einer angenommenen Herausforderung — der trifft
    /// sonst niemanden, wenn man gerade nicht im Play-Bildschirm steht.
    /// </summary>
    public class ChallengeWatcher : MonoBehaviour
    {
        private static ChallengeWatcher instance;

        private NetworkManager subscribed;
        private RectTransform popup;        // eingehende Herausforderung
        private float popupTimeout;
        private string popupFrom;
        private RectTransform toast;
        private float toastUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var host = new GameObject("~ChallengeWatcher");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<ChallengeWatcher>();
        }

        private void Update()
        {
            // Der NetworkManager entsteht erst mit der Login-Szene — anheften,
            // sobald (und wann immer) es eine frische Instanz gibt.
            var net = NetworkManager.Instance;
            if (net != subscribed)
            {
                if (subscribed != null) subscribed.OnMessage -= HandleMessage;
                subscribed = net;
                if (subscribed != null) subscribed.OnMessage += HandleMessage;
            }

            if (popup != null && Time.unscaledTime > popupTimeout) ClosePopup();
            if (toast != null && Time.unscaledTime > toastUntil) { Destroy(toast.gameObject); toast = null; }
        }

        private void HandleMessage(NetMessage m)
        {
            if (m == null) return;
            switch (m.t)
            {
                case "challenge_incoming":
                    ShowChallengePopup(m.name);
                    break;

                case "challenge_cancelled":
                    if (popup != null && popupFrom == m.name) ClosePopup();
                    break;

                case "friend_event":
                    // Nur die Anfrage verdient einen Toast — alles andere sieht
                    // man dort, wo man es ausgelöst hat.
                    if (m.kind == "request")
                        ShowToast(Loc.F("{0} sent you a friend request.", m.name));
                    break;

                case "sduel_start":
                    HandleDuelStart(m);
                    break;
            }
        }

        /// <summary>
        /// Duell-Start ausserhalb des Play-Bildschirms (Herausforderung aus dem
        /// Menü heraus angenommen). Im Play-Bildschirm übernimmt der
        /// DuelSetupController mit seinem MATCH-FOUND-Moment; hier reicht der
        /// direkte Weg ins Duell.
        /// </summary>
        private void HandleDuelStart(NetMessage m)
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "Play" || scene == "Duel") return;

            ClosePopup();
            MatchContext.Clear();
            MatchContext.IsServerMatch = true;
            MatchContext.LocalIsPlayerA = m.youAre == "A";
            MatchContext.LocalName = PlayerProfile.AccountName;
            MatchContext.RemoteName = string.IsNullOrEmpty(m.opponent) ? "Opponent" : m.opponent;
            MatchContext.SetRemoteCosmetics(m.oppSlots, m.oppIds);
            DuelLoadTransition.Play(null, MatchContext.RemoteName, "", 40);
            SceneManager.LoadScene("Duel");
        }

        // ================== POPUP ==================

        private void ShowChallengePopup(string from)
        {
            ClosePopup();
            popupFrom = from;
            popupTimeout = Time.unscaledTime + 45f;   // verwaiste Popups räumen sich selbst weg

            var skin = TransitionSkin.Load();
            var canvasGo = new GameObject("~ChallengePopup", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            popup = (RectTransform)canvasGo.transform;

            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(popup, false);
            plateRect.anchorMin = plateRect.anchorMax = new Vector2(0.5f, 1f);
            plateRect.pivot = new Vector2(0.5f, 1f);
            plateRect.anchoredPosition = new Vector2(0f, -80f);
            plateRect.sizeDelta = new Vector2(520f, 120f);
            plate.GetComponent<Image>().color = new Color32(0x0E, 0x12, 0x1B, 0xF6);

            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var frameRect = (RectTransform)frame.transform;
            frameRect.SetParent(plateRect, false);
            frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero; frameRect.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = new Color32(0xC8, 0xA4, 0x5C, 0xFF);
            if (skin != null && skin.frame != null) { frameImg.sprite = skin.frame; frameImg.type = Image.Type.Sliced; }
            frameImg.raycastTarget = false;

            var text = MakeLabel(plateRect, skin != null ? skin.cinzel : null, 17f, new Color32(0xF1, 0xDF, 0xB8, 0xFF));
            text.text = Loc.F("{0} CHALLENGES YOU TO A DUEL", from.ToUpperInvariant());
            text.alignment = TextAlignmentOptions.Center;
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f); textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -16f);
            textRect.sizeDelta = new Vector2(-30f, 30f);

            MakePopupButton(plateRect, skin, Loc.T("ACCEPT"), new Color32(0x1E, 0x33, 0x24, 0xF0),
                new Color32(0x7D, 0xDB, 0x6E, 0xFF), new Vector2(-92f, 18f), () =>
                {
                    var net = NetworkManager.Instance;
                    if (net != null && net.IsConnected)
                        net.SendChallengeAccept(popupFrom, PlayerPrefs.GetInt(MainMenuController.ActiveDeckPrefKey, 0));
                    // Das Popup bleibt kurz stehen, bis sduel_start eintrifft;
                    // scheitert der Start, räumt der Timeout auf.
                    popupTimeout = Time.unscaledTime + 10f;
                });
            MakePopupButton(plateRect, skin, Loc.T("DECLINE"), new Color32(0x33, 0x20, 0x1E, 0xF0),
                new Color32(0xE8, 0xA0, 0xA0, 0xFF), new Vector2(92f, 18f), () =>
                {
                    var net = NetworkManager.Instance;
                    if (net != null && net.IsConnected) net.SendChallengeDecline(popupFrom);
                    ClosePopup();
                });

            SfxManager.Click();
        }

        private void MakePopupButton(RectTransform parent, TransitionSkin skin, string label,
            Color32 plate, Color32 ink, Vector2 offset, System.Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(160f, 40f);
            go.GetComponent<Image>().color = plate;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());

            var text = MakeLabel(rect, skin != null ? skin.oswald : null, 14f, ink);
            text.text = label;
            text.characterSpacing = 16f;
            text.alignment = TextAlignmentOptions.Center;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        }

        private void ClosePopup()
        {
            if (popup != null) Destroy(popup.gameObject);
            popup = null;
            popupFrom = null;
        }

        // ================== TOAST ==================

        private void ShowToast(string message)
        {
            if (SceneManager.GetActiveScene().name == "Duel") return;   // im Duell nicht dazwischenfunken
            if (toast != null) Destroy(toast.gameObject);

            var skin = TransitionSkin.Load();
            var canvasGo = new GameObject("~FriendToast", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 495;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            toast = (RectTransform)canvasGo.transform;
            toastUntil = Time.unscaledTime + 4f;

            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(toast, false);
            plateRect.anchorMin = plateRect.anchorMax = new Vector2(0.5f, 0f);
            plateRect.pivot = new Vector2(0.5f, 0f);
            plateRect.anchoredPosition = new Vector2(0f, 46f);
            plateRect.sizeDelta = new Vector2(480f, 44f);
            plate.GetComponent<Image>().color = new Color32(0x0E, 0x12, 0x1B, 0xEE);

            var text = MakeLabel(plateRect, skin != null ? skin.oswald : null, 13f, new Color32(0xEB, 0xCE, 0x8A, 0xFF));
            text.text = message;
            text.alignment = TextAlignmentOptions.Center;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI MakeLabel(RectTransform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
