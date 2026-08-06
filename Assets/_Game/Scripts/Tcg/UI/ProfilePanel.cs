using System.Collections;
using System.Collections.Generic;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Das Spielerprofil (Handoff „Progression", Abschnitt 3). Öffnet über die
    /// Spieler-Plakette oben im Hauptmenü.
    ///
    /// Links die Identität — Portrait, Name, Titel, Rangblock mit RP-Balken.
    /// Rechts die Zahlen: vier Kacheln und der Sammlungsfortschritt nach Seltenheit.
    ///
    /// Bewusst NICHT enthalten sind Match-History, Seals und Attribut-Affinität aus
    /// dem Handoff: dafür führt der Server noch keine Daten, und erfundene Zahlen
    /// wären schlimmer als eine fehlende Kachel.
    ///
    /// Wird zur Laufzeit gebaut, damit der Szenenaufbau unangetastet bleibt.
    /// </summary>
    public class ProfilePanel : MonoBehaviour
    {
        private const float PanelWidth = 1180f;
        private const float PanelHeight = 700f;
        private const float LeftWidth = 440f;

        private static ProfilePanel instance;

        private CanvasGroup group;
        private RectTransform panel;
        private TransitionSkin skin;
        private Coroutine animRoutine;
        private CardCatalog catalog;

        private RectTransform titlePopover;

        /// <summary>Öffnet das Profil (erzeugt es beim ersten Mal).</summary>
        public static void Open(CardCatalog cardCatalog = null)
        {
            if (instance == null)
            {
                var host = new GameObject("~Profile");
                instance = host.AddComponent<ProfilePanel>();
                instance.catalog = cardCatalog;
                instance.Build();
            }
            instance.catalog = cardCatalog != null ? cardCatalog : instance.catalog;
            instance.Refresh();
            instance.Show(true);
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

        /// <summary>Jedes neue Profil aktualisiert die Anzeige — etwa nach einem Titelwechsel.</summary>
        private void HandleMessage(NetMessage message)
        {
            if (message.t == "profile" || message.t == "auth_ok") Refresh();
        }

        private void Show(bool visible)
        {
            gameObject.SetActive(true);
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Fade(visible));
        }

        private IEnumerator Fade(bool visible)
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
            panel.localScale = Vector3.one;
            if (!visible) gameObject.SetActive(false);
            animRoutine = null;
        }

        // ================== AUFBAU ==================

        private void Build()
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var canvasRect = (RectTransform)canvasGo.transform;

            // Scrim heisst bewusst so — der UiFxInstaller lässt ihn dann in Ruhe
            var scrim = MakeImage("Scrim", canvasRect, new Color(0f, 0f, 0f, 0.72f));
            Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(() => Show(false));

            panel = MakeRect("Panel", canvasRect);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var bg = MakeImage("BG", panel, Hex("#0E121B", 0.98f));
            Stretch(bg.rectTransform);
            var frame = MakeImage("Frame", panel, Hex("#C8A45C", 1f));
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);
            var inner = MakeImage("InnerFrame", panel, Hex("#C8A45C", 0.27f));
            inner.sprite = skin.frame; inner.type = Image.Type.Sliced;
            Stretch(inner.rectTransform, 8f);

            var diamond = MakeImage("TopDiamond", panel, Hex("#EBCE8A", 1f));
            diamond.sprite = skin.square;
            diamond.rectTransform.sizeDelta = new Vector2(12f, 12f);
            diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            diamond.rectTransform.anchoredPosition = Vector2.zero;

            BuildIdentity();
            BuildStats();
            BuildCloseButton();

            gameObject.SetActive(false);
        }

        // ---- linke Spalte ----
        private TMP_Text nameText, handleText, titleChip, rankNameText, rankStepText, factCards, factDecks, factSeason;
        private RectTransform rankEmblemSlot;
        private Image rpBarFill;
        private Image portraitFrame, portraitKeyline;
        private TMP_Text initialText;
        private readonly List<TMP_Text> tierPips = new List<TMP_Text>();
        private RectTransform pipRow;

        private void BuildIdentity()
        {
            var column = MakeRect("Identity", panel);
            column.sizeDelta = new Vector2(LeftWidth, PanelHeight - 80f);
            column.anchorMin = column.anchorMax = new Vector2(0f, 0.5f);
            column.pivot = new Vector2(0f, 0.5f);
            column.anchoredPosition = new Vector2(46f, 0f);

            float y = column.sizeDelta.y * 0.5f - 90f;

            // Portrait: Kachel, darauf das Profilbild (falls eines ausgerüstet
            // ist), darüber der Rahmen. Ohne Profilbild bleibt die Initiale.
            var portrait = MakeImage("Portrait", column, Hex("#3E2C16", 1f));
            portrait.sprite = skin.diagFade;
            portrait.rectTransform.sizeDelta = new Vector2(148f, 148f);
            portrait.rectTransform.anchoredPosition = new Vector2(0f, y);

            // VOR den Rahmen erzeugt, damit jeder Rahmen darueber zeichnet
            var equippedAvatar = Rouge.Tcg.Net.CosmeticArt.EquippedAvatar();
            Image avatarImage = null;
            if (equippedAvatar != null)
            {
                avatarImage = MakeImage("Avatar", portrait.rectTransform, Color.white);
                avatarImage.sprite = equippedAvatar;
                Stretch(avatarImage.rectTransform);
            }

            portraitFrame = MakeImage("Frame", portrait.rectTransform, Hex("#C8A45C", 1f));
            portraitFrame.sprite = skin.frame; portraitFrame.type = Image.Type.Sliced;
            Stretch(portraitFrame.rectTransform);
            portraitKeyline = MakeImage("Keyline", portrait.rectTransform, Hex("#C8A45C", 0.65f));
            portraitKeyline.sprite = skin.frame; portraitKeyline.type = Image.Type.Sliced;
            Stretch(portraitKeyline.rectTransform, 7f);

            // Ein ausgerüsteter Profilrahmen legt sich über beide Zierlinien und
            // darf über die Portraitkante hinausragen — bei Thorn Setting und
            // Vault Ring ist genau das ihr Erkennungsmerkmal.
            var equippedFrame = Rouge.Tcg.Net.CosmeticArt.EquippedFrame();
            if (equippedFrame != null)
            {
                portraitFrame.gameObject.SetActive(false);
                portraitKeyline.gameObject.SetActive(false);
                var cosmetic = MakeImage("CosmeticFrame", portrait.rectTransform, Color.white);
                cosmetic.sprite = equippedFrame;

                string frameId = Rouge.Tcg.Net.Cosmetics.EquippedIn("avatarFrame");
                if (Rouge.Tcg.Net.CosmeticArt.IsPlaque(frameId))
                {
                    // Bilderrahmen: die Kachel verschwindet (sonst lugt sie an den
                    // Ecken hervor), und skaliert wird aufs FENSTER — jede Innen-
                    // fläche erscheint gleich gross, egal wie viel Schmuck darum
                    // liegt. Breite Motive — Schwingen, Panzerhandschuhe — ragen
                    // dadurch seitlich über die Portraitfläche hinaus; das ist
                    // ihr Auftritt.
                    portrait.color = Color.clear;
                    float scale = Rouge.Tcg.Net.CosmeticArt.PlaqueScale(frameId, 126f);
                    var rect = cosmetic.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(
                        equippedFrame.rect.width * scale, equippedFrame.rect.height * scale);
                    rect.anchoredPosition = Vector2.zero;

                    // Das Profilbild schrumpft mit ins Fenster — sonst stuende es
                    // an den Ecken ueber den Rahmen hinaus
                    if (avatarImage != null)
                    {
                        var aRect = avatarImage.rectTransform;
                        aRect.anchorMin = aRect.anchorMax = new Vector2(0.5f, 0.5f);
                        aRect.pivot = new Vector2(0.5f, 0.5f);
                        aRect.sizeDelta = new Vector2(126f, 126f);
                        aRect.anchoredPosition = Vector2.zero;
                    }
                }
                else
                {
                    Stretch(cosmetic.rectTransform, -16f);
                }
            }
            initialText = MakeText("Initial", portrait.rectTransform, skin.cinzel, 64f, Hex("#EBCE8A", 1f));
            initialText.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)initialText.transform, 148f, 90f, 0f);

            y -= 108f;
            var eyebrow = MakeText("Eyebrow", column, skin.oswald, 12f, Hex("#9C8A6A", 1f));
            eyebrow.text = "DUELIST";
            eyebrow.characterSpacing = 30f;
            eyebrow.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)eyebrow.transform, LeftWidth, 18f, y);

            y -= 42f;
            nameText = MakeText("Name", column, skin.cinzel, 40f, Hex("#F1DFB8", 1f));
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableAutoSizing = true; nameText.fontSizeMin = 22f; nameText.fontSizeMax = 40f;
            Strip((RectTransform)nameText.transform, LeftWidth, 50f, y);

            y -= 36f;
            handleText = MakeText("Handle", column, skin.spectral, 15f, Hex("#8C7B5F", 1f));
            handleText.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)handleText.transform, LeftWidth, 20f, y);

            y -= 38f;
            titleChip = MakeChip(column, y, out var chipButton);
            chipButton.onClick.AddListener(ToggleTitles);

            y -= 40f;
            Divider(column, y);

            // ---- Rangblock ----
            y -= 34f;
            rankEmblemSlot = MakeRect("RankEmblem", column);
            // Tief genug, dass die obere Spitze die Trennlinie nicht schneidet
            rankEmblemSlot.anchoredPosition = new Vector2(-LeftWidth * 0.5f + 60f, y - 36f);

            rankNameText = MakeText("RankName", column, skin.cinzel, 24f, Hex("#F3DDA4", 1f));
            rankNameText.alignment = TextAlignmentOptions.Left;
            Strip((RectTransform)rankNameText.transform, 250f, 30f, y);
            ((RectTransform)rankNameText.transform).anchoredPosition = new Vector2(28f, y);

            pipRow = MakeRect("TierPips", column);
            pipRow.anchoredPosition = new Vector2(28f, y - 26f);
            pipRow.sizeDelta = new Vector2(250f, 12f);
            var pipLayout = pipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            pipLayout.childAlignment = TextAnchor.MiddleLeft;
            pipLayout.spacing = 6f;
            pipLayout.childControlWidth = pipLayout.childControlHeight = false;
            pipLayout.childForceExpandWidth = pipLayout.childForceExpandHeight = false;

            y -= 52f;
            var track = MakeImage("RpTrack", column, Hex("#C8A45C", 0.18f));
            track.sprite = skin.square;
            track.rectTransform.sizeDelta = new Vector2(250f, 4f);
            track.rectTransform.anchoredPosition = new Vector2(28f + 125f - LeftWidth * 0.5f + LeftWidth * 0.5f, y);
            Strip(track.rectTransform, 250f, 4f, y);
            track.rectTransform.anchoredPosition = new Vector2(28f, y);
            rpBarFill = MakeImage("RpFill", track.rectTransform, Hex("#EBCE8A", 1f));
            rpBarFill.sprite = skin.square;
            rpBarFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            rpBarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            rpBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            rpBarFill.rectTransform.anchoredPosition = Vector2.zero;
            rpBarFill.rectTransform.sizeDelta = new Vector2(0f, 0f);

            y -= 24f;
            rankStepText = MakeText("RankStep", column, skin.spectral, 14f, Hex("#A2917A", 1f));
            rankStepText.alignment = TextAlignmentOptions.Left;
            Strip((RectTransform)rankStepText.transform, 300f, 20f, y);
            ((RectTransform)rankStepText.transform).anchoredPosition = new Vector2(28f, y);

            y -= 30f;
            Divider(column, y);

            // ---- drei kleine Fakten ----
            y -= 40f;
            factCards = Fact(column, -120f, y, "CARDS");
            factDecks = Fact(column, 0f, y, "DECKS");
            factSeason = Fact(column, 120f, y, "SEASON");
        }

        private TMP_Text Fact(RectTransform parent, float x, float y, string label)
        {
            var caption = MakeText(label + "Caption", parent, skin.oswald, 10f, Hex("#8C7B5F", 1f));
            caption.text = label;
            caption.characterSpacing = 20f;
            caption.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)caption.transform, 120f, 14f, y - 22f);
            ((RectTransform)caption.transform).anchoredPosition = new Vector2(x, y - 22f);

            var value = MakeText(label + "Value", parent, skin.cinzel, 22f, Hex("#EBCE8A", 1f));
            value.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)value.transform, 120f, 28f, y);
            ((RectTransform)value.transform).anchoredPosition = new Vector2(x, y);
            return value;
        }

        private void Divider(RectTransform parent, float y)
        {
            var left = MakeImage("Divider", parent, Hex("#C8A45C", 0.4f));
            left.sprite = skin.rule;
            Strip(left.rectTransform, 170f, 1f, y);
            left.rectTransform.anchoredPosition = new Vector2(-88f, y);
            var right = MakeImage("DividerRight", parent, Hex("#C8A45C", 0.4f));
            right.sprite = skin.rule;
            Strip(right.rectTransform, 170f, 1f, y);
            right.rectTransform.anchoredPosition = new Vector2(88f, y);
            right.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        }

        private TMP_Text MakeChip(RectTransform parent, float y, out Button button)
        {
            var chip = MakeImage("TitleChip", parent, new Color(0f, 0f, 0f, 0.4f));
            chip.sprite = skin.frame; chip.type = Image.Type.Sliced;
            chip.raycastTarget = true;
            Strip(chip.rectTransform, 280f, 30f, y);
            var border = MakeImage("Frame", chip.rectTransform, Hex("#C8A45C", 0.45f));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            button = chip.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            var label = MakeText("Label", chip.rectTransform, skin.spectral, 15f, Hex("#EBCE8A", 1f));
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform);
            return label;
        }

        // ---- rechte Spalte ----
        private readonly TMP_Text[] tileValues = new TMP_Text[4];
        private RectTransform rarityColumn;

        private void BuildStats()
        {
            var column = MakeRect("Stats", panel);
            float width = PanelWidth - LeftWidth - 120f;
            column.sizeDelta = new Vector2(width, PanelHeight - 80f);
            column.anchorMin = column.anchorMax = new Vector2(1f, 0.5f);
            column.pivot = new Vector2(1f, 0.5f);
            column.anchoredPosition = new Vector2(-46f, 0f);

            float top = column.sizeDelta.y * 0.5f;

            var heading = MakeText("Heading", column, skin.cinzel, 26f, Hex("#EBCE8A", 1f));
            heading.text = "This season";
            heading.alignment = TextAlignmentOptions.Left;
            Strip((RectTransform)heading.transform, width, 32f, top - 26f);

            string[] captions = { "DUELS", "WIN RATE", "BEST STREAK", "SEASON RP" };
            string[] accents = { "#C8A45C", "#7ACD96", "#B9A3E0", "#8FC6D2" };
            float tileWidth = (width - 3f * 14f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                float x = -width * 0.5f + tileWidth * 0.5f + i * (tileWidth + 14f);
                var tile = MakeImage("Tile" + i, column, new Color(0f, 0f, 0f, 0.32f));
                tile.sprite = skin.frame; tile.type = Image.Type.Sliced;
                Strip(tile.rectTransform, tileWidth, 96f, top - 108f);
                tile.rectTransform.anchoredPosition = new Vector2(x, top - 108f);
                var border = MakeImage("Frame", tile.rectTransform, Hex(accents[i], 0.5f));
                border.sprite = skin.frame; border.type = Image.Type.Sliced;
                Stretch(border.rectTransform);

                var caption = MakeText("Caption", tile.rectTransform, skin.oswald, 10f, Hex("#8C7B5F", 1f));
                caption.text = captions[i];
                caption.characterSpacing = 18f;
                caption.alignment = TextAlignmentOptions.Center;
                Strip((RectTransform)caption.transform, tileWidth, 14f, -30f);

                tileValues[i] = MakeText("Value", tile.rectTransform, skin.cinzel, 34f, Hex(accents[i], 1f));
                tileValues[i].alignment = TextAlignmentOptions.Center;
                Strip((RectTransform)tileValues[i].transform, tileWidth, 42f, 10f);
            }

            var collectionHeading = MakeText("CollectionHeading", column, skin.cinzel, 22f, Hex("#EBCE8A", 1f));
            collectionHeading.text = "Collection";
            collectionHeading.alignment = TextAlignmentOptions.Left;
            Strip((RectTransform)collectionHeading.transform, width, 28f, top - 200f);

            rarityColumn = MakeRect("Rarities", column);
            rarityColumn.sizeDelta = new Vector2(width, 200f);
            rarityColumn.anchoredPosition = new Vector2(0f, top - 262f);
        }

        private void BuildCloseButton()
        {
            var close = MakeImage("CloseButton", panel, new Color(0f, 0f, 0f, 0.45f));
            close.sprite = skin.frame; close.type = Image.Type.Sliced;
            close.raycastTarget = true;
            close.rectTransform.anchorMin = close.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            close.rectTransform.pivot = new Vector2(0.5f, 0f);
            close.rectTransform.sizeDelta = new Vector2(130f, 38f);
            close.rectTransform.anchoredPosition = new Vector2(0f, 22f);
            var border = MakeImage("Frame", close.rectTransform, Hex("#C8A45C", 0.7f));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            var button = close.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Show(false));
            var label = MakeText("Label", close.rectTransform, skin.oswald, 13f, Hex("#EBCE8A", 1f));
            label.text = "CLOSE";
            label.characterSpacing = 22f;
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform);

            // Der Laden liegt neben dem Schliessen-Knopf: hier sieht man sein Aussehen,
            // hier will man es auch ändern
            var shop = MakeImage("CosmeticsButton", panel, new Color(0f, 0f, 0f, 0.45f));
            shop.sprite = skin.frame; shop.type = Image.Type.Sliced;
            shop.raycastTarget = true;
            shop.rectTransform.anchorMin = shop.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            shop.rectTransform.pivot = new Vector2(0.5f, 0f);
            shop.rectTransform.sizeDelta = new Vector2(170f, 38f);
            shop.rectTransform.anchoredPosition = new Vector2(-156f, 22f);
            var shopBorder = MakeImage("Frame", shop.rectTransform, Hex("#C8A45C", 0.7f));
            shopBorder.sprite = skin.frame; shopBorder.type = Image.Type.Sliced;
            Stretch(shopBorder.rectTransform);
            var shopButton = shop.gameObject.AddComponent<Button>();
            shopButton.transition = Selectable.Transition.None;
            shopButton.onClick.AddListener(() => { SfxManager.Click(); CosmeticsPanel.Open(); });
            var shopLabel = MakeText("Label", shop.rectTransform, skin.oswald, 13f, Hex("#EBCE8A", 1f));
            shopLabel.text = "COSMETICS";
            shopLabel.characterSpacing = 18f;
            shopLabel.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)shopLabel.transform);
        }

        // ================== INHALT ==================

        private void Refresh()
        {
            var rank = PlayerProfile.Rank;
            string name = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Wanderer";

            // Ein Profilbild ersetzt die Initiale — beides uebereinander waere
            // ein Buchstabe mitten im Monstergesicht.
            initialText.gameObject.SetActive(Rouge.Tcg.Net.CosmeticArt.EquippedAvatar() == null);
            initialText.text = name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "?";
            nameText.text = name;
            handleText.text = PlayerProfile.LoggedIn
                ? $"Season {rank.Season}"
                : "Not signed in";
            titleChip.text = TitleName(PrimaryTitle());

            // Der ausgerüstete Portraitrahmen färbt den Rahmen um — bis es echte
            // Rahmen-Grafiken gibt, ist die Seltenheitsfarbe der sichtbare Unterschied.
            var frameItem = Cosmetics.Find(Cosmetics.EquippedIn("avatarFrame"));
            var accent = frameItem != null ? frameItem.Accent : Hex("#C8A45C", 1f);
            portraitFrame.color = new Color(accent.r, accent.g, accent.b, 1f);
            portraitKeyline.color = new Color(accent.r, accent.g, accent.b, 0.65f);

            // Rangblock
            for (int i = rankEmblemSlot.childCount - 1; i >= 0; i--)
                DestroyImmediate(rankEmblemSlot.GetChild(i).gameObject);
            RankEmblem.Build(rankEmblemSlot, rank.Rank, RankEmblem.Size.Full);

            rankNameText.text = rank.Seal.Label;
            rankNameText.color = RankLadder.Edge(rank.Rank);
            rankStepText.text = rank.NextStepLine ?? "The top of the ladder.";
            rpBarFill.rectTransform.sizeDelta = new Vector2(250f * rank.TierProgress, 0f);
            rpBarFill.color = RankLadder.Edge(rank.Rank);
            BuildTierPips(rank);

            factCards.text = PlayerProfile.Collection.Count.ToString();
            factDecks.text = PlayerProfile.Decks.Count.ToString();
            factSeason.text = rank.Rp.ToString();

            tileValues[0].text = rank.Duels.ToString();
            tileValues[1].text = rank.Duels == 0 ? "—" : $"{Mathf.RoundToInt(rank.WinRate * 100f)}%";
            tileValues[2].text = rank.BestStreak.ToString();
            tileValues[3].text = rank.Rp.ToString();

            BuildRarityBars();
        }

        /// <summary>Fünf Rauten: gefüllt bis zur erreichten Unterstufe.</summary>
        private void BuildTierPips(RankState rank)
        {
            for (int i = pipRow.childCount - 1; i >= 0; i--) DestroyImmediate(pipRow.GetChild(i).gameObject);
            var edge = RankLadder.Edge(rank.Rank);
            for (int i = 1; i <= 5; i++)
            {
                bool filled = i <= rank.Tier;
                var pip = MakeImage("Pip" + i, pipRow, filled ? edge : new Color(edge.r, edge.g, edge.b, 0.45f));
                pip.sprite = filled ? skin.square : skin.frame;
                if (!filled) pip.type = Image.Type.Sliced;
                pip.rectTransform.sizeDelta = new Vector2(10f, 10f);
                pip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                var element = pip.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 10f; element.preferredHeight = 10f;
            }
        }

        /// <summary>Sammlungsfortschritt je Seltenheit — echte Zahlen aus dem Katalog.</summary>
        private void BuildRarityBars()
        {
            for (int i = rarityColumn.childCount - 1; i >= 0; i--)
                DestroyImmediate(rarityColumn.GetChild(i).gameObject);

            if (catalog == null) catalog = FindAnyObjectByType<DuelHost>()?.Catalog;
            if (catalog == null)
            {
                var note = MakeText("Note", rarityColumn, skin.spectral, 14f, Hex("#8C7B5F", 1f));
                note.text = "Card catalogue unavailable.";
                Strip((RectTransform)note.transform, 400f, 20f, 0f);
                return;
            }

            string[] names = { "COMMON", "UNCOMMON", "RARE", "LEGENDARY" };
            string[] colors = { "#A2917A", "#8FC6D2", "#B9A3E0", "#EBCE8A" };
            var total = new int[4];
            var owned = new int[4];
            foreach (var card in catalog.cards)
            {
                if (card == null) continue;
                int slot = Mathf.Clamp((int)card.rarity, 0, 3);
                total[slot]++;
                if (PlayerProfile.Collection.ContainsKey(card.cardName)) owned[slot]++;
            }

            float width = rarityColumn.sizeDelta.x;
            for (int i = 0; i < 4; i++)
            {
                float y = -i * 46f;
                var label = MakeText(names[i], rarityColumn, skin.oswald, 11f, Hex(colors[i], 1f));
                label.text = names[i];
                label.characterSpacing = 16f;
                label.alignment = TextAlignmentOptions.Left;
                Strip((RectTransform)label.transform, 140f, 16f, y);
                ((RectTransform)label.transform).anchoredPosition = new Vector2(-width * 0.5f + 70f, y);

                var count = MakeText("Count", rarityColumn, skin.spectral, 13f, Hex("#A2917A", 1f));
                count.text = $"{owned[i]} / {total[i]}";
                count.alignment = TextAlignmentOptions.Right;
                Strip((RectTransform)count.transform, 120f, 16f, y);
                ((RectTransform)count.transform).anchoredPosition = new Vector2(width * 0.5f - 60f, y);

                var track = MakeImage("Track", rarityColumn, Hex(colors[i], 0.16f));
                track.sprite = skin.square;
                float trackWidth = width - 300f;
                Strip(track.rectTransform, trackWidth, 6f, y - 20f);
                track.rectTransform.anchoredPosition = new Vector2(-width * 0.5f + 70f + trackWidth * 0.5f, y - 20f);
                var fill = MakeImage("Fill", track.rectTransform, Hex(colors[i], 1f));
                fill.sprite = skin.square;
                fill.rectTransform.anchorMin = new Vector2(0f, 0f);
                fill.rectTransform.anchorMax = new Vector2(0f, 1f);
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.rectTransform.anchoredPosition = Vector2.zero;
                float ratio = total[i] == 0 ? 0f : owned[i] / (float)total[i];
                fill.rectTransform.sizeDelta = new Vector2(trackWidth * ratio, 0f);
            }
        }

        // ---- Titel ----

        /// <summary>Der Startertitel jedes Early-Access-Spielers — steht in keinem Ladenfach.</summary>
        private const string StarterTitle = "early_vault_hunter";

        /// <summary>
        /// Der angezeigte Titel ist der ausgerüstete. Solange nichts ausgerüstet ist,
        /// gilt der erste freigeschaltete — und ganz zur Not der Startertitel.
        /// </summary>
        private string PrimaryTitle()
        {
            string equipped = Cosmetics.EquippedIn("title");
            if (!string.IsNullOrEmpty(equipped)) return equipped;
            return PlayerProfile.Titles.Count > 0 ? PlayerProfile.Titles[0] : StarterTitle;
        }

        /// <summary>
        /// Alle Titel, die dem Spieler gehören: der Startertitel plus alles, was im
        /// Titelfach gekauft wurde. Der Server ist die Quelle — hier wird nur gelesen.
        /// </summary>
        private static List<string> OwnedTitles()
        {
            var result = new List<string> { StarterTitle };
            foreach (var key in PlayerProfile.Titles)
                if (key != StarterTitle && !result.Contains(key)) result.Add(key);
            foreach (var item in Cosmetics.InSlot("title"))
                if (Cosmetics.Owns(item.Id) && !result.Contains(item.Id)) result.Add(item.Id);
            return result;
        }

        /// <summary>Schlüssel → Anzeigename. Neue Titel hier ergänzen.</summary>
        public static string TitleName(string key)
        {
            switch (key)
            {
                case "early_vault_hunter": return "Early Vault Hunter";
                case "sealbreaker": return "Sealbreaker";
                case "ash_collector": return "Ash Collector";
                case "wardens_bane": return "Warden's Bane";
                default: return key;
            }
        }

        private static string TitleHint(string key)
        {
            switch (key)
            {
                case "early_vault_hunter": return "Played during Early Access";
                default: return "Unlocked";
            }
        }

        private void ToggleTitles()
        {
            SfxManager.Click();
            if (titlePopover != null) { DestroyImmediate(titlePopover.gameObject); titlePopover = null; return; }

            var titles = OwnedTitles();
            titlePopover = MakeRect("TitlePopover", panel);
            titlePopover.sizeDelta = new Vector2(300f, 40f + titles.Count * 46f);
            titlePopover.anchorMin = titlePopover.anchorMax = new Vector2(0f, 0.5f);
            titlePopover.pivot = new Vector2(0f, 1f);
            titlePopover.anchoredPosition = new Vector2(120f, 96f);

            var bg = MakeImage("BG", titlePopover, Hex("#0E121B", 0.99f));
            Stretch(bg.rectTransform);
            var frame = MakeImage("Frame", titlePopover, Hex("#C8A45C", 0.8f));
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);

            string current = PrimaryTitle();
            float y = titlePopover.sizeDelta.y * 0.5f - 30f;
            foreach (var key in titles)
            {
                bool isCurrent = key == current;

                // Klickfläche über der ganzen Zeile — der Titel wechselt sofort
                var hit = MakeImage("Row", titlePopover, new Color(1f, 1f, 1f, isCurrent ? 0.05f : 0f));
                hit.raycastTarget = true;
                Strip(hit.rectTransform, 276f, 40f, y - 8f);
                var row = hit.gameObject.AddComponent<Button>();
                row.transition = Selectable.Transition.None;
                var captured = key;
                row.onClick.AddListener(() => EquipTitle(captured));

                var name = MakeText("Title", titlePopover, skin.spectral, 16f,
                    Hex(isCurrent ? "#F1DFB8" : "#EBCE8A", 1f));
                name.text = TitleName(key);
                name.alignment = TextAlignmentOptions.Left;
                Strip((RectTransform)name.transform, 260f, 20f, y);
                var hint = MakeText("Hint", titlePopover, skin.oswald, 10f, Hex("#8C7B5F", 1f));
                hint.text = (isCurrent ? "Equipped" : TitleHint(key)).ToUpperInvariant();
                hint.characterSpacing = 14f;
                hint.alignment = TextAlignmentOptions.Left;
                Strip((RectTransform)hint.transform, 260f, 14f, y - 18f);
                y -= 46f;
            }
        }

        /// <summary>
        /// Titel wechseln. Der Server entscheidet; die Anzeige folgt, sobald das
        /// nächste Profil eintrifft.
        /// </summary>
        private void EquipTitle(string key)
        {
            SfxManager.Click();
            if (titlePopover != null) { DestroyImmediate(titlePopover.gameObject); titlePopover = null; }
            if (key == PrimaryTitle()) return;

            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected) net.SendEquipCosmetic("title", key);
        }

        // ---- Bau-Helfer ----

        private static Color Hex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            return color;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Strip(RectTransform rect, float width, float height, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

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
    }
}
