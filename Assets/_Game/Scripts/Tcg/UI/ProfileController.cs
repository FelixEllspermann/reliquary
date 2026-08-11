using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Eigenständige Profil-Seite: Avatar samt Rahmen, Rang und Saison-Bilanz,
    /// Gesamt-Statistiken (PvP/Solo), ein Karten-Schaufenster (bis zu 3 langsam
    /// rotierende Karten mit Finish und dem eigenen Sleeve als Rückseite), die
    /// letzten Spiele und laufende Duelle zum Zuschauen. Kosmetik/Titel bleiben
    /// im bestehenden ProfilePanel — der CUSTOMIZE-Knopf öffnet es als Overlay.
    /// </summary>
    public class ProfileController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button customizeButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string duelSceneName = "Duel";

        [Header("Spieler-Panel")]
        [SerializeField] private RectTransform avatarCrest;
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text seasonText;
        [SerializeField] private TMP_Text totalsText;
        [SerializeField] private TMP_Text collectionText;

        [Header("Schaufenster")]
        [SerializeField] private RectTransform showcaseContainer;
        [SerializeField] private TcgCardView cardPrefab;

        [Header("Match-Liste")]
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject matchRowTemplate;
        [SerializeField] private GameObject liveRowTemplate;
        [SerializeField] private TMP_Text emptyHint;

        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;

        [Header("Farben")]
        [SerializeField] private Color winColor = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] private Color lossColor = new Color(0.90f, 0.45f, 0.38f);
        [SerializeField] private Color liveColor = new Color(0.95f, 0.80f, 0.35f);

        private readonly List<GameObject> rows = new List<GameObject>();
        private readonly List<GameObject> showcaseSlots = new List<GameObject>();
        private readonly ShowcaseCard[] showcase = new ShowcaseCard[3];
        private RectTransform pickerOverlay;

        private void Awake()
        {
            if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (customizeButton != null) customizeButton.onClick.AddListener(() => ProfilePanel.Open(catalog));
            if (matchRowTemplate != null) matchRowTemplate.SetActive(false);
            if (liveRowTemplate != null) liveRowTemplate.SetActive(false);
        }

        private void OnEnable()
        {
            FillPlayerPanel();
            ApplyCosmetics();
            BuildShowcase();
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                ShowEmpty("NOT CONNECTED — MATCH HISTORY NEEDS A SERVER SESSION.");
                return;
            }
            net.OnMessage += HandleMessage;
            ShowEmpty("LOADING MATCH HISTORY...");
            net.RequestProfileStats();
        }

        private void OnDisable()
        {
            var net = NetworkManager.Instance;
            if (net != null) net.OnMessage -= HandleMessage;
        }

        private void FillPlayerPanel()
        {
            if (playerName != null)
                playerName.text = string.IsNullOrEmpty(PlayerProfile.AccountName) ? "DUELIST" : PlayerProfile.AccountName.ToUpperInvariant();
            var rank = PlayerProfile.Rank;
            if (rankText != null)
                rankText.text = $"{rank.Name.ToUpperInvariant()}  ·  TIER {rank.Tier}  ·  {rank.Rp} RP";
            if (seasonText != null)
                seasonText.text = $"SEASON {rank.Season}\n{rank.Wins} WINS · {rank.Losses} LOSSES · BEST STREAK {rank.BestStreak}";
            if (collectionText != null && catalog != null)
            {
                int total = 0, owned = 0;
                foreach (var card in catalog.cards)
                {
                    if (card == null || card is PlayerCardData || card.isToken) continue;
                    total++;
                    if (PlayerProfile.Owned(card.cardName) > 0) owned++;
                }
                collectionText.text = $"COLLECTION: {owned} / {total} CARDS";
            }
            if (totalsText != null) totalsText.text = "";
        }

        /// <summary>Avatar + Rahmen wie im Hauptmenü-Miniprofil, nur größer.</summary>
        private void ApplyCosmetics()
        {
            if (avatarCrest == null) return;
            for (int i = avatarCrest.childCount - 1; i >= 0; i--)
            {
                var child = avatarCrest.GetChild(i);
                if (child.name == "Avatar" || child.name == "AvatarFrame") Destroy(child.gameObject);
            }
            var crestImage = avatarCrest.GetComponent<Image>();
            if (crestImage != null) crestImage.enabled = true;
            float window = avatarCrest.rect.height > 0f ? avatarCrest.rect.height : 96f;

            var avatarSprite = CosmeticArt.EquippedAvatar();
            if (avatarSprite != null)
            {
                var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)avatar.transform;
                rect.SetParent(avatarCrest, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(window, window);
                var img = avatar.GetComponent<Image>();
                img.sprite = avatarSprite;
                img.raycastTarget = false;
            }

            var frameSprite = CosmeticArt.EquippedFrame();
            if (frameSprite != null)
            {
                var frame = new GameObject("AvatarFrame", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)frame.transform;
                rect.SetParent(avatarCrest, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                var img = frame.GetComponent<Image>();
                img.sprite = frameSprite;
                img.raycastTarget = false;

                string frameId = Cosmetics.EquippedIn("avatarFrame");
                if (CosmeticArt.IsPlaque(frameId))
                {
                    // Bilderrahmen skalieren aufs Fenster; die eckige Platte
                    // dahinter würde sonst an den Ecken hervorlugen.
                    if (crestImage != null) crestImage.enabled = false;
                    float scale = CosmeticArt.PlaqueScale(frameId, window);
                    rect.sizeDelta = new Vector2(frameSprite.rect.width * scale, frameSprite.rect.height * scale);
                }
                else
                {
                    rect.sizeDelta = new Vector2(window + 10f, window + 10f);
                    img.preserveAspect = true;
                }
            }
        }

        // ================== SCHAUFENSTER ==================

        /// <summary>Baut die 3 Slots neu: rotierende Karten oder leere Plätze.</summary>
        private void BuildShowcase()
        {
            if (showcaseContainer == null) return;
            foreach (var slot in showcaseSlots) if (slot != null) Destroy(slot);
            showcaseSlots.Clear();

            var backSprite = CosmeticArt.EquippedCardBack();
            const float slotWidth = 170f, slotHeight = 238f;
            float[] xs = { -186f, 0f, 186f };

            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i;
                var entry = showcase[i];
                var slot = new GameObject("ShowcaseSlot_" + i, typeof(RectTransform));
                var slotRect = (RectTransform)slot.transform;
                slotRect.SetParent(showcaseContainer, false);
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = new Vector2(xs[i], 0f);
                slotRect.sizeDelta = new Vector2(slotWidth, slotHeight);
                showcaseSlots.Add(slot);

                var def = entry != null && catalog != null ? catalog.FindByName(entry.n) : null;
                if (def != null)
                {
                    // Drehteller: Vorderseite = Karte mit Finish, Rückseite = eigenes Sleeve
                    var spinner = new GameObject("Spinner", typeof(RectTransform));
                    var spinnerRect = (RectTransform)spinner.transform;
                    spinnerRect.SetParent(slotRect, false);
                    spinnerRect.anchorMin = spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
                    spinnerRect.sizeDelta = new Vector2(slotWidth, slotHeight);

                    GameObject frontGo = null;
                    if (cardPrefab != null)
                    {
                        var view = Instantiate(cardPrefab, spinnerRect);
                        var viewRect = (RectTransform)view.transform;
                        viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
                        viewRect.sizeDelta = new Vector2(slotWidth, slotHeight);
                        var instance = new CardInstance(def, null) { Finish = (CardFinish)Mathf.Clamp(entry.f, 0, 3) };
                        view.Show(instance, hideFace: false, upright: true);
                        frontGo = view.gameObject;
                    }

                    var back = new GameObject("Back", typeof(RectTransform), typeof(Image));
                    var backRect = (RectTransform)back.transform;
                    backRect.SetParent(spinnerRect, false);
                    backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.5f);
                    backRect.sizeDelta = new Vector2(slotWidth, slotHeight);
                    var backImg = back.GetComponent<Image>();
                    if (backSprite != null) backImg.sprite = backSprite;
                    else backImg.color = new Color32(0x2A, 0x1A, 0x0E, 0xFF);
                    backImg.raycastTarget = false;

                    var spin = spinner.AddComponent<ShowcaseSpin>();
                    spin.Bind(frontGo, back, slotIndex * 120f);   // versetzt gestartet
                }
                else
                {
                    var empty = new GameObject("Empty", typeof(RectTransform), typeof(Image));
                    var emptyRect = (RectTransform)empty.transform;
                    emptyRect.SetParent(slotRect, false);
                    emptyRect.anchorMin = Vector2.zero; emptyRect.anchorMax = Vector2.one;
                    emptyRect.offsetMin = Vector2.zero; emptyRect.offsetMax = Vector2.zero;
                    empty.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
                    var plus = new GameObject("Plus", typeof(RectTransform), typeof(TextMeshProUGUI));
                    var plusRect = (RectTransform)plus.transform;
                    plusRect.SetParent(emptyRect, false);
                    plusRect.anchorMin = Vector2.zero; plusRect.anchorMax = Vector2.one;
                    plusRect.offsetMin = Vector2.zero; plusRect.offsetMax = Vector2.zero;
                    var plusText = plus.GetComponent<TextMeshProUGUI>();
                    plusText.text = "+";
                    plusText.fontSize = 44f;
                    plusText.color = new Color32(0x8C, 0x7B, 0x5F, 0xFF);
                    plusText.alignment = TextAlignmentOptions.Center;
                    if (playerName != null) plusText.font = playerName.font;
                    plusText.raycastTarget = false;
                }

                // Der ganze Slot ist klickbar: Karte wählen oder austauschen
                var clickCatcher = slot.AddComponent<Image>();
                clickCatcher.color = new Color(0f, 0f, 0f, 0.001f);
                var button = slot.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OpenPicker(slotIndex));
            }
        }

        /// <summary>Bestes besessenes Finish einer Karte (Static > Rainbow > Glossy > Schlicht).</summary>
        private static int BestOwnedFinish(string cardName)
        {
            for (int f = 3; f >= 1; f--)
                if (PlayerProfile.Owned(cardName, (CardFinish)f) > 0) return f;
            return 0;
        }

        /// <summary>Kartenwahl fürs Schaufenster: alle besessenen Karten, alphabetisch.</summary>
        private void OpenPicker(int slotIndex)
        {
            ClosePicker();
            if (catalog == null) return;

            pickerOverlay = new GameObject("~ShowcasePicker", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            pickerOverlay.SetParent(transform, false);
            pickerOverlay.anchorMin = Vector2.zero; pickerOverlay.anchorMax = Vector2.one;
            pickerOverlay.offsetMin = Vector2.zero; pickerOverlay.offsetMax = Vector2.zero;
            pickerOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
            var overlayButton = pickerOverlay.gameObject.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.AddListener(ClosePicker);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(pickerOverlay, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 720f);
            panel.GetComponent<Image>().color = new Color32(0x1E, 0x14, 0x0C, 0xF5);
            var panelBlock = panel.AddComponent<Button>();       // schluckt Klicks
            panelBlock.transition = Selectable.Transition.None;

            var title = MakeText(panelRect, "Title", "CHOOSE A SHOWCASE CARD", playerName != null ? playerName.font : null,
                18f, new Color32(0xF1, 0xDF, 0xB8, 0xFF), TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -16f); titleRect.sizeDelta = new Vector2(-32f, 28f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(panelRect, false);
            scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(14f, 14f); scrollRect.offsetMax = new Vector2(-14f, -56f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(scrollRect, false);
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform));
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            scroll.viewport = viewportRect; scroll.content = contentRect;

            var entries = new List<(string label, Action pick)>();
            if (showcase[slotIndex] != null)
                entries.Add(("— REMOVE FROM SHOWCASE —", () => SetSlot(slotIndex, null)));
            foreach (var card in catalog.cards
                .Where(c => c != null && !(c is PlayerCardData) && !c.isToken && PlayerProfile.Owned(c.cardName) > 0)
                .OrderBy(c => c.cardName, StringComparer.Ordinal))
            {
                string cardName = card.cardName;
                int finish = BestOwnedFinish(cardName);
                string label = finish > 0 ? $"{cardName}  <size=75%><color=#EBCE8A>{(CardFinish)finish}</color></size>" : cardName;
                entries.Add((label, () => SetSlot(slotIndex, new ShowcaseCard { n = cardName, f = finish })));
            }

            const float rowH = 40f, gap = 4f;
            for (int i = 0; i < entries.Count; i++)
            {
                var (label, pick) = entries[i];
                var row = new GameObject("Row_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                var rowRect = (RectTransform)row.transform;
                rowRect.SetParent(contentRect, false);
                rowRect.anchorMin = new Vector2(0f, 1f); rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(0f, rowH);
                rowRect.anchoredPosition = new Vector2(0f, -i * (rowH + gap));
                row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
                row.GetComponent<Button>().onClick.AddListener(() => pick());
                var text = MakeText(rowRect, "Label", label, seasonText != null ? seasonText.font : null,
                    14f, new Color32(0xD8, 0xCD, 0xB8, 0xFF), TextAlignmentOptions.Left);
                var textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(14f, 0f); textRect.offsetMax = new Vector2(-8f, 0f);
                text.richText = true;
            }
            contentRect.sizeDelta = new Vector2(0f, entries.Count * (rowH + gap));
        }

        private void SetSlot(int slotIndex, ShowcaseCard card)
        {
            showcase[slotIndex] = card;
            ClosePicker();
            BuildShowcase();
            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected)
                net.SendSetShowcase(showcase.Where(s => s != null).ToArray());
        }

        private void ClosePicker()
        {
            if (pickerOverlay != null) Destroy(pickerOverlay.gameObject);
            pickerOverlay = null;
        }

        // ================== SERVER-DATEN ==================

        private void HandleMessage(NetMessage m)
        {
            if (m == null || m.t != "profile_stats") return;

            for (int i = 0; i < 3; i++)
                showcase[i] = m.showcase != null && i < m.showcase.Length ? m.showcase[i] : null;
            BuildShowcase();

            if (totalsText != null)
            {
                int pvpRate = Winrate(m.pvpWins, m.pvpGames);
                int soloRate = Winrate(m.soloWins, m.soloGames);
                totalsText.text =
                    $"PVP: {m.pvpWins}/{m.pvpGames} ({pvpRate}%)\nSOLO: {m.soloWins}/{m.soloGames} ({soloRate}%)";
            }
            BuildList(m.liveGames ?? Array.Empty<LiveGame>(), m.matches ?? Array.Empty<ProfileMatch>());
        }

        private void BuildList(LiveGame[] live, ProfileMatch[] matches)
        {
            foreach (var row in rows) Destroy(row);
            rows.Clear();

            if (live.Length == 0 && matches.Length == 0)
            {
                ShowEmpty("NO MATCHES PLAYED YET.");
                return;
            }
            if (emptyHint != null) emptyHint.gameObject.SetActive(false);

            foreach (var game in live)
            {
                if (liveRowTemplate == null || listContent == null) break;
                var row = Instantiate(liveRowTemplate, listContent);
                row.SetActive(true);
                SetRowText(row, "ResultText", "LIVE", liveColor);
                SetRowText(row, "InfoText", $"{game.a}  VS  {game.b}", default);
                SetRowText(row, "MetaText", "RUNNING NOW", default);
                var watch = row.transform.Find("WatchButton");
                if (watch != null)
                {
                    string duelId = game.duelId;
                    string nameA = game.a; string nameB = game.b;
                    watch.GetComponent<Button>().onClick.AddListener(() => Watch(duelId, nameA, nameB));
                }
                rows.Add(row);
            }

            foreach (var match in matches)
            {
                if (matchRowTemplate == null || listContent == null) break;
                var row = Instantiate(matchRowTemplate, listContent);
                row.SetActive(true);
                SetRowText(row, "ResultText", match.won ? "WIN" : "LOSS", match.won ? winColor : lossColor);
                SetRowText(row, "InfoText", $"VS {match.opponent.ToUpperInvariant()}  ·  {match.deckName}", default);
                SetRowText(row, "MetaText", $"{match.mode.ToUpperInvariant()}  ·  {Ago(match.ts)}", default);
                rows.Add(row);
            }
        }

        /// <summary>Startet den Zuschauer-Modus für ein laufendes Duell.</summary>
        private void Watch(string duelId, string nameA, string nameB)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            MatchContext.Clear();
            MatchContext.IsServerMatch = true;
            MatchContext.SpectateMode = true;
            MatchContext.LocalName = nameA;
            MatchContext.RemoteName = nameB;
            MatchContext.LocalIsPlayerA = true;
            net.SendSpectate(duelId);
            SceneManager.LoadScene(duelSceneName);
        }

        private void ShowEmpty(string message)
        {
            if (emptyHint == null) return;
            emptyHint.gameObject.SetActive(true);
            emptyHint.text = message;
        }

        private static string Ago(long tsMs)
        {
            var elapsed = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(tsMs);
            if (elapsed.TotalMinutes < 1) return "JUST NOW";
            if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}M AGO";
            if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}H AGO";
            return $"{(int)elapsed.TotalDays}D AGO";
        }

        private static int Winrate(int wins, int games) =>
            games <= 0 ? 0 : Mathf.RoundToInt(100f * wins / games);

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, string value,
            TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRowText(GameObject row, string childName, string value, Color color)
        {
            var child = row.transform.Find(childName);
            if (child == null) return;
            var text = child.GetComponent<TMP_Text>();
            if (text == null) return;
            text.text = value;
            if (color != default) text.color = color;
        }
    }
}
