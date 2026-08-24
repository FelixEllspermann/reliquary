using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Freundesliste als Overlay (komplett zur Laufzeit gebaut, wie das
    /// CosmeticsPanel): oben der eigene Freundescode zum Weitergeben und ein
    /// Eingabefeld für fremde Codes, darunter offene Anfragen und die Liste —
    /// je Freund mit Online-Punkt, DUEL (Herausforderung), PROFILE und Entfernen.
    /// Eingehende Herausforderungen zeigt der <see cref="ChallengeWatcher"/>,
    /// damit sie auch ohne offenes Panel ankommen.
    /// </summary>
    public class FriendsPanel : MonoBehaviour
    {
        private static FriendsPanel instance;

        private TransitionSkin skin;
        private RectTransform panel;
        private CanvasGroup group;
        private Coroutine animRoutine;

        private TMP_Text codeText;
        private TMP_InputField codeField;
        private TMP_Text feedbackText;
        private RectTransform listContent;
        private ScrollRect listScroll;
        private RectTransform challengeBar;
        private TMP_Text challengeText;

        private string pendingChallengeTarget;

        public static void Open()
        {
            if (instance == null)
            {
                var host = new GameObject("~Friends");
                instance = host.AddComponent<FriendsPanel>();
                instance.Build();
            }
            instance.Show(true);
            instance.Refresh();
        }

        public static void Close() => instance?.Show(false);

        private void OnEnable()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.OnMessage += HandleMessage;
        }

        private void OnDisable()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.OnMessage -= HandleMessage;
        }

        private void Refresh()
        {
            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected) net.RequestFriends();
            else SetFeedback(Loc.T("Not connected."), false);
        }

        // ================== AUFBAU ==================

        private void Build()
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 430;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var canvasRect = (RectTransform)canvasGo.transform;

            // Heisst „Scrim", damit der UiFxInstaller ihn in Ruhe lässt
            var scrim = MakeImage("Scrim", canvasRect, new Color(0f, 0f, 0f, 0.74f));
            Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(() => Show(false));

            panel = MakeRect("Panel", canvasRect);
            panel.sizeDelta = new Vector2(760f, 820f);
            var bg = MakeImage("BG", panel, Hex("#0E121B", 0.98f));
            // Fängt Klicks auf tote Flächen im Fenster ab — nur der Scrim daneben schließt.
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);
            var frame = MakeImage("Frame", panel, Hex("#C8A45C", 1f));
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);

            var title = MakeText("Title", panel, skin.cinzel, 26f, Hex("#F1DFB8", 1f));
            title.text = Loc.T("FRIENDS");
            title.alignment = TextAlignmentOptions.Center;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            title.rectTransform.sizeDelta = new Vector2(0f, 34f);

            BuildCloseButton();
            BuildCodeBlock();
            BuildAddRow();

            feedbackText = MakeText("Feedback", panel, skin.oswald, 13f, Hex("#EBCE8A", 1f));
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.rectTransform.anchorMin = new Vector2(0f, 1f);
            feedbackText.rectTransform.anchorMax = new Vector2(1f, 1f);
            feedbackText.rectTransform.pivot = new Vector2(0.5f, 1f);
            feedbackText.rectTransform.anchoredPosition = new Vector2(0f, -196f);
            feedbackText.rectTransform.sizeDelta = new Vector2(-60f, 22f);
            feedbackText.text = "";

            BuildList();
            BuildChallengeBar();
        }

        private void BuildCloseButton()
        {
            var close = MakeImage("Close", panel, Hex("#1A2130", 0.9f));
            close.raycastTarget = true;
            close.rectTransform.anchorMin = close.rectTransform.anchorMax = new Vector2(0f, 1f);
            close.rectTransform.pivot = new Vector2(0f, 1f);
            close.rectTransform.sizeDelta = new Vector2(52f, 34f);
            close.rectTransform.anchoredPosition = new Vector2(26f, -26f);
            var border = MakeImage("Frame", close.rectTransform, Hex("#C8A45C", 0.7f));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            var label = MakeText("Label", close.rectTransform, skin.oswald, 12f, Hex("#D8CDB8", 1f));
            label.text = Loc.T("CLOSE");
            label.alignment = TextAlignmentOptions.Center;
            Stretch(label.rectTransform);
            var button = close.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Show(false));
        }

        /// <summary>Der eigene Code, gross und kopierbar — die Adresse, die man Freunden gibt.</summary>
        private void BuildCodeBlock()
        {
            var caption = MakeText("CodeCaption", panel, skin.oswald, 12f, Hex("#8C7B5F", 1f));
            caption.text = Loc.T("YOUR FRIEND CODE");
            caption.characterSpacing = 18f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.rectTransform.anchorMin = new Vector2(0f, 1f);
            caption.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            caption.rectTransform.pivot = new Vector2(0.5f, 1f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            caption.rectTransform.sizeDelta = new Vector2(-40f, 20f);

            codeText = MakeText("Code", panel, skin.cinzel, 30f, Hex("#F3DDA4", 1f));
            codeText.text = "········";
            codeText.alignment = TextAlignmentOptions.Center;
            codeText.characterSpacing = 6f;
            codeText.rectTransform.anchorMin = new Vector2(0f, 1f);
            codeText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            codeText.rectTransform.pivot = new Vector2(0.5f, 1f);
            codeText.rectTransform.anchoredPosition = new Vector2(0f, -96f);
            codeText.rectTransform.sizeDelta = new Vector2(-40f, 40f);

            var copy = MakeImage("CopyButton", panel, Hex("#1A2130", 0.9f));
            copy.raycastTarget = true;
            // Punkt-Anker in der Mitte der linken Panelhälfte — gespreizte Anker
            // würden die sizeDelta ADDIEREN und den Knopf über die Kante ziehen.
            copy.rectTransform.anchorMin = copy.rectTransform.anchorMax = new Vector2(0.25f, 1f);
            copy.rectTransform.pivot = new Vector2(0.5f, 1f);
            copy.rectTransform.anchoredPosition = new Vector2(0f, -142f);
            copy.rectTransform.sizeDelta = new Vector2(130f, 28f);
            var copyBorder = MakeImage("Frame", copy.rectTransform, Hex("#C8A45C", 0.7f));
            copyBorder.sprite = skin.frame; copyBorder.type = Image.Type.Sliced;
            Stretch(copyBorder.rectTransform);
            var copyLabel = MakeText("Label", copy.rectTransform, skin.oswald, 12f, Hex("#EBCE8A", 1f));
            copyLabel.text = Loc.T("COPY");
            copyLabel.characterSpacing = 16f;
            copyLabel.alignment = TextAlignmentOptions.Center;
            Stretch(copyLabel.rectTransform);
            var copyButton = copy.gameObject.AddComponent<Button>();
            copyButton.transition = Selectable.Transition.None;
            copyButton.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = codeText.text.Replace("-", "");
                SetFeedback(Loc.T("Code copied."), true);
            });
        }

        /// <summary>Eingabefeld + ADD: einen fremden Code eintragen.</summary>
        private void BuildAddRow()
        {
            var caption = MakeText("AddCaption", panel, skin.oswald, 12f, Hex("#8C7B5F", 1f));
            caption.text = Loc.T("ADD A FRIEND BY CODE");
            caption.characterSpacing = 18f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            caption.rectTransform.anchorMax = new Vector2(1f, 1f);
            caption.rectTransform.pivot = new Vector2(0.5f, 1f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            caption.rectTransform.sizeDelta = new Vector2(-40f, 20f);

            // TMP_InputField von Hand: BG -> Viewport (Maske) -> Text + Platzhalter
            var fieldBg = MakeImage("CodeField", panel, Hex("#1A2130", 0.95f));
            fieldBg.raycastTarget = true;
            fieldBg.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            fieldBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            fieldBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            fieldBg.rectTransform.anchoredPosition = new Vector2(-34f, -100f);
            fieldBg.rectTransform.sizeDelta = new Vector2(-160f, 40f);
            var fieldBorder = MakeImage("Frame", fieldBg.rectTransform, Hex("#C8A45C", 0.45f));
            fieldBorder.sprite = skin.frame; fieldBorder.type = Image.Type.Sliced;
            Stretch(fieldBorder.rectTransform);

            var viewport = MakeRect("TextArea", fieldBg.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            Stretch(viewport);
            viewport.offsetMin = new Vector2(12f, 4f);
            viewport.offsetMax = new Vector2(-12f, -4f);

            var text = MakeText("Text", viewport, skin.oswald, 18f, Hex("#F1DFB8", 1f));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.characterSpacing = 8f;
            Stretch(text.rectTransform);

            var placeholder = MakeText("Placeholder", viewport, skin.oswald, 14f, Hex("#8C7B5F", 0.8f));
            placeholder.text = Loc.T("FRIEND CODE…");
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(placeholder.rectTransform);

            codeField = fieldBg.gameObject.AddComponent<TMP_InputField>();
            codeField.targetGraphic = fieldBg;
            codeField.textViewport = viewport;
            codeField.textComponent = text;
            codeField.placeholder = placeholder;
            codeField.characterLimit = 12;   // 8 Zeichen + Luft für Bindestriche
            codeField.onSubmit.AddListener(_ => SendAdd());

            var add = MakeImage("AddButton", panel, Hex("#1A2130", 0.9f));
            add.raycastTarget = true;
            add.rectTransform.anchorMin = add.rectTransform.anchorMax = new Vector2(1f, 1f);
            add.rectTransform.pivot = new Vector2(1f, 1f);
            add.rectTransform.anchoredPosition = new Vector2(-30f, -100f);
            add.rectTransform.sizeDelta = new Vector2(84f, 40f);
            var addBorder = MakeImage("Frame", add.rectTransform, Hex("#C8A45C", 0.7f));
            addBorder.sprite = skin.frame; addBorder.type = Image.Type.Sliced;
            Stretch(addBorder.rectTransform);
            var addLabel = MakeText("Label", add.rectTransform, skin.oswald, 13f, Hex("#EBCE8A", 1f));
            addLabel.text = Loc.T("ADD");
            addLabel.characterSpacing = 16f;
            addLabel.alignment = TextAlignmentOptions.Center;
            Stretch(addLabel.rectTransform);
            var addButton = add.gameObject.AddComponent<Button>();
            addButton.transition = Selectable.Transition.None;
            addButton.onClick.AddListener(SendAdd);
        }

        private void SendAdd()
        {
            var net = NetworkManager.Instance;
            string code = codeField != null ? codeField.text.Trim() : "";
            if (string.IsNullOrEmpty(code)) return;
            if (net == null || !net.IsConnected) { SetFeedback(Loc.T("Not connected."), false); return; }
            net.SendFriendAdd(code);
        }

        /// <summary>Scrollbare Liste: Anfragen zuoberst, dann die Freunde.</summary>
        private void BuildList()
        {
            var view = MakeRect("List", panel);
            view.anchorMin = new Vector2(0f, 0f);
            view.anchorMax = new Vector2(1f, 1f);
            view.offsetMin = new Vector2(30f, 76f);
            view.offsetMax = new Vector2(-30f, -224f);

            listScroll = view.gameObject.AddComponent<ScrollRect>();
            listScroll.horizontal = false;
            listScroll.vertical = true;
            listScroll.movementType = ScrollRect.MovementType.Clamped;
            listScroll.scrollSensitivity = 40f;

            var viewport = MakeRect("Viewport", view);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0.01f);
            catcher.raycastTarget = true;
            Stretch(viewport);

            listContent = MakeRect("Content", viewport);
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = Vector2.one;
            listContent.pivot = new Vector2(0.5f, 1f);

            listScroll.viewport = viewport;
            listScroll.content = listContent;
        }

        /// <summary>Statuszeile einer laufenden ausgehenden Herausforderung, mit CANCEL.</summary>
        private void BuildChallengeBar()
        {
            challengeBar = MakeRect("ChallengeBar", panel);
            challengeBar.anchorMin = new Vector2(0f, 0f);
            challengeBar.anchorMax = new Vector2(1f, 0f);
            challengeBar.pivot = new Vector2(0.5f, 0f);
            challengeBar.anchoredPosition = new Vector2(0f, 28f);
            challengeBar.sizeDelta = new Vector2(-60f, 36f);

            var bg = MakeImage("BG", challengeBar, Hex("#1A2130", 0.9f));
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);
            var border = MakeImage("Frame", challengeBar, Hex("#C8A45C", 0.5f));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);

            challengeText = MakeText("Text", challengeBar, skin.oswald, 13f, Hex("#EBCE8A", 1f));
            challengeText.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(challengeText.rectTransform);
            challengeText.rectTransform.offsetMin = new Vector2(14f, 0f);
            challengeText.rectTransform.offsetMax = new Vector2(-110f, 0f);

            var cancel = MakeImage("Cancel", challengeBar, Hex("#3A2430", 0.9f));
            cancel.raycastTarget = true;
            cancel.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            cancel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            cancel.rectTransform.pivot = new Vector2(1f, 0.5f);
            cancel.rectTransform.anchoredPosition = new Vector2(-6f, 0f);
            cancel.rectTransform.sizeDelta = new Vector2(92f, 26f);
            var cancelLabel = MakeText("Label", cancel.rectTransform, skin.oswald, 11f, Hex("#E8A0A0", 1f));
            cancelLabel.text = Loc.T("CANCEL");
            cancelLabel.characterSpacing = 14f;
            cancelLabel.alignment = TextAlignmentOptions.Center;
            Stretch(cancelLabel.rectTransform);
            var cancelButton = cancel.gameObject.AddComponent<Button>();
            cancelButton.transition = Selectable.Transition.None;
            cancelButton.onClick.AddListener(() =>
            {
                var net = NetworkManager.Instance;
                if (net != null && net.IsConnected) net.SendChallengeCancel();
                SetChallengePending(null);
            });

            challengeBar.gameObject.SetActive(false);
        }

        private void SetChallengePending(string targetName)
        {
            pendingChallengeTarget = targetName;
            if (challengeBar == null) return;
            challengeBar.gameObject.SetActive(!string.IsNullOrEmpty(targetName));
            if (!string.IsNullOrEmpty(targetName) && challengeText != null)
                challengeText.text = Loc.F("WAITING FOR {0}…", targetName.ToUpperInvariant());
        }

        // ================== LISTE ==================

        private void RebuildList(NetMessage m)
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);

            float y = 0f;
            const float rowH = 46f, gap = 6f, headH = 26f;

            void Header(string label)
            {
                var head = MakeText("Header", listContent, skin.oswald, 12f, Hex("#8C7B5F", 1f));
                head.text = label;
                head.characterSpacing = 18f;
                head.alignment = TextAlignmentOptions.MidlineLeft;
                head.rectTransform.anchorMin = new Vector2(0f, 1f);
                head.rectTransform.anchorMax = new Vector2(1f, 1f);
                head.rectTransform.pivot = new Vector2(0.5f, 1f);
                head.rectTransform.anchoredPosition = new Vector2(0f, -y);
                head.rectTransform.sizeDelta = new Vector2(0f, headH);
                y += headH + 2f;
            }

            RectTransform Row()
            {
                var row = MakeRect("Row", listContent);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -y);
                row.sizeDelta = new Vector2(0f, rowH);
                var bg = MakeImage("BG", row, new Color(0f, 0f, 0f, 0.35f));
                bg.raycastTarget = true;
                Stretch(bg.rectTransform);
                y += rowH + gap;
                return row;
            }

            Button RowButton(RectTransform row, string label, float rightOffset, float width, Color tint, Color ink)
            {
                var img = MakeImage(label, row, tint);
                img.raycastTarget = true;
                img.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                img.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                img.rectTransform.pivot = new Vector2(1f, 0.5f);
                img.rectTransform.anchoredPosition = new Vector2(-rightOffset, 0f);
                img.rectTransform.sizeDelta = new Vector2(width, 30f);
                var border = MakeImage("Frame", img.rectTransform, Hex("#C8A45C", 0.4f));
                border.sprite = skin.frame; border.type = Image.Type.Sliced;
                Stretch(border.rectTransform);
                var text = MakeText("Label", img.rectTransform, skin.oswald, 11f, ink);
                text.text = label;
                text.characterSpacing = 10f;
                text.alignment = TextAlignmentOptions.Center;
                Stretch(text.rectTransform);
                var button = img.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                return button;
            }

            var requests = m.requests ?? Array.Empty<string>();
            if (requests.Length > 0)
            {
                Header(Loc.T("REQUESTS"));
                foreach (var name in requests)
                {
                    string who = name;
                    var row = Row();
                    var label = MakeText("Name", row, skin.oswald, 15f, Hex("#F1DFB8", 1f));
                    label.text = who.ToUpperInvariant();
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    Stretch(label.rectTransform);
                    label.rectTransform.offsetMin = new Vector2(14f, 0f);
                    label.rectTransform.offsetMax = new Vector2(-240f, 0f);
                    RowButton(row, Loc.T("ACCEPT"), 118f, 100f, Hex("#1E3324", 0.95f), Hex("#7DDB6E", 1f))
                        .onClick.AddListener(() => NetworkManager.Instance?.SendFriendAccept(who));
                    RowButton(row, Loc.T("DECLINE"), 10f, 100f, Hex("#33201E", 0.95f), Hex("#E8A0A0", 1f))
                        .onClick.AddListener(() => NetworkManager.Instance?.SendFriendDecline(who));
                }
                y += 8f;
            }

            var friends = m.friends ?? Array.Empty<FriendEntry>();
            Header(Loc.T("YOUR FRIENDS"));
            if (friends.Length == 0)
            {
                var hint = MakeText("Empty", listContent, skin.oswald, 13f, Hex("#8C7B5F", 1f));
                hint.text = Loc.T("NO FRIENDS YET — SHARE YOUR CODE!");
                hint.alignment = TextAlignmentOptions.MidlineLeft;
                hint.rectTransform.anchorMin = new Vector2(0f, 1f);
                hint.rectTransform.anchorMax = new Vector2(1f, 1f);
                hint.rectTransform.pivot = new Vector2(0.5f, 1f);
                hint.rectTransform.anchoredPosition = new Vector2(0f, -y);
                hint.rectTransform.sizeDelta = new Vector2(0f, rowH);
                y += rowH;
            }
            foreach (var friend in friends)
            {
                var entry = friend;
                var row = Row();

                var dot = MakeImage("Dot", row, entry.online ? Hex("#7DDB6E", 1f) : Hex("#5A5A5A", 1f));
                dot.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                dot.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                dot.rectTransform.pivot = new Vector2(0f, 0.5f);
                dot.rectTransform.anchoredPosition = new Vector2(12f, 0f);
                dot.rectTransform.sizeDelta = new Vector2(10f, 10f);

                var label = MakeText("Name", row, skin.oswald, 15f, Hex("#F1DFB8", 1f));
                label.text = entry.name.ToUpperInvariant()
                    + (entry.inDuel ? $"  <size=70%><color=#EBCE8A>{Loc.T("IN A DUEL")}</color></size>" : "");
                label.alignment = TextAlignmentOptions.MidlineLeft;
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(32f, 0f);
                label.rectTransform.offsetMax = new Vector2(-300f, 0f);

                var duel = RowButton(row, Loc.T("DUEL"), 202f, 86f, Hex("#2A2334", 0.95f), Hex("#C9B7F0", 1f));
                duel.interactable = entry.online && !entry.inDuel;
                if (!duel.interactable)
                    duel.GetComponentInChildren<TMP_Text>().color = Hex("#5A5A5A", 1f);
                duel.onClick.AddListener(() =>
                {
                    var net = NetworkManager.Instance;
                    if (net == null || !net.IsConnected) return;
                    int deckIndex = PlayerPrefs.GetInt(MainMenuController.ActiveDeckPrefKey, 0);
                    net.SendFriendChallenge(entry.name, deckIndex);
                });

                RowButton(row, Loc.T("PROFILE"), 96f, 100f, Hex("#1A2130", 0.95f), Hex("#EBCE8A", 1f))
                    .onClick.AddListener(() =>
                    {
                        ProfileController.ViewTarget = entry.name;
                        SceneManager.LoadScene("Profile");
                    });

                RowButton(row, "X", 10f, 34f, Hex("#33201E", 0.95f), Hex("#E8A0A0", 1f))
                    .onClick.AddListener(() => NetworkManager.Instance?.SendFriendRemove(entry.name));
            }

            listContent.sizeDelta = new Vector2(0f, y + 8f);
        }

        // ================== NACHRICHTEN ==================

        private void HandleMessage(NetMessage m)
        {
            if (m == null || group == null || group.alpha <= 0f) return;
            switch (m.t)
            {
                case "friends":
                    if (codeText != null && !string.IsNullOrEmpty(m.friendCode))
                        codeText.text = m.friendCode.Length == 8
                            ? m.friendCode.Substring(0, 4) + "-" + m.friendCode.Substring(4)
                            : m.friendCode;
                    RebuildList(m);
                    break;

                case "friend_event":
                    if (m.kind == "sent") { SetFeedback(Loc.F("Request sent to {0}.", m.name), true); if (codeField != null) codeField.text = ""; }
                    else if (m.kind == "accepted") SetFeedback(Loc.F("You are now friends with {0}!", m.name), true);
                    else if (m.kind == "request") SetFeedback(Loc.F("{0} sent you a friend request.", m.name), true);
                    Refresh();
                    break;

                case "challenge_sent":
                    SetChallengePending(m.name);
                    break;

                case "challenge_declined":
                    SetChallengePending(null);
                    SetFeedback(Loc.F("{0} declined the duel.", m.name), false);
                    break;

                case "error":
                    if (!string.IsNullOrEmpty(m.msg)) SetFeedback(Loc.T(m.msg), false);
                    break;
            }
        }

        private void SetFeedback(string message, bool good)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.color = good ? Hex("#7DDB6E", 1f) : Hex("#E8A0A0", 1f);
        }

        // ================== SICHTBARKEIT ==================

        private void Show(bool visible)
        {
            gameObject.SetActive(true);
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Fade(visible));
        }

        private System.Collections.IEnumerator Fade(bool visible)
        {
            group.blocksRaycasts = visible;
            float from = group.alpha, to = visible ? 1f : 0f;
            const float duration = 0.16f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                group.alpha = Mathf.Lerp(from, to, k);
                float scale = visible ? Mathf.Lerp(0.97f, 1f, k) : Mathf.Lerp(1f, 0.98f, k);
                panel.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            group.alpha = to;
            if (!visible) gameObject.SetActive(false);
        }

        // ================== BAUSTEINE ==================

        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image MakeImage(string name, RectTransform parent, Color color)
        {
            var image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color Hex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }
    }
}
