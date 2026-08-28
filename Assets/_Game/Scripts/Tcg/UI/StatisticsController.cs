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
    /// Statistik-Seite der Sammlung, zwei Ansichten über einen Umschalter:
    /// CARDS — welche Karten gespielt werden, wie oft sie gewinnen und mit wem
    /// sie im Deck stehen (filterbar nach Kartenart und Seltenheit).
    /// ARCHETYPES — welche Familien gespielt werden und welche Duos gewinnen.
    /// Gewertet werden NUR Online-Matches (Solo gegen Bots verzerrt); ein Match
    /// zählt je Karte einmal, ein Deck "spielt" einen Archetype ab 4 Karten.
    /// Die Zahlen kommen fertig vom Server — der Client rechnet nur Prozente.
    /// </summary>
    public class StatisticsController : MonoBehaviour
    {
        [Header("Tabs & Navigation")]
        [SerializeField] private Button decksTabButton;
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button menuButton;

        [Header("Szenen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string decksSceneName = "DeckEditor";
        [SerializeField] private string shopSceneName = "Shop";

        [Header("Karten-Liste")]
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private TMP_Text emptyHint;

        [Header("Detail-Panel")]
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailSummary;
        [SerializeField] private TMP_Text detailCards;

        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;

        [Header("Farben")]
        [SerializeField] private Color rowIdleColor = new Color(0.10f, 0.08f, 0.05f, 0.85f);
        [SerializeField] private Color rowSelectedColor = new Color(0.72f, 0.58f, 0.28f, 0.35f);
        [SerializeField] private Color goodWinrate = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] private Color badWinrate = new Color(0.90f, 0.45f, 0.38f);

        private readonly List<GameObject> rows = new List<GameObject>();
        private StatsCard[] cards = Array.Empty<StatsCard>();
        private StatsArchetype[] archetypes = Array.Empty<StatsArchetype>();
        private StatsArchPair[] archetypePairs = Array.Empty<StatsArchPair>();
        private int selected = -1;

        // ---- Ansicht & Filter (alles Laufzeit-UI über der Liste) ----
        private int viewMode;      // 0 = Karten, 1 = Archetypes
        private int typeFilter;    // 0 alle, 1 Monster, 2 Spell, 3 Artifact, 4 Reliquary
        private int rarityFilter;  // 0 alle, sonst 1+(int)CardRarity
        private readonly List<(Button button, Image bg, TMP_Text label, Func<bool> isActive)> chips
            = new List<(Button, Image, TMP_Text, Func<bool>)>();
        private GameObject filterRow;   // nur in der Karten-Ansicht sichtbar

        private static readonly string[] TypeNames = { "ALL", "MONSTER", "SPELL", "ARTIFACT", "RELIQUARY", "INCARNATE" };
        private static readonly string[] RarityNames = { "C", "U", "R", "L" };

        private void Awake()
        {
            if (decksTabButton != null) decksTabButton.onClick.AddListener(() => SceneManager.LoadScene(decksSceneName));
            if (shopTabButton != null) shopTabButton.onClick.AddListener(() => SceneManager.LoadScene(shopSceneName));
            if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (rowTemplate != null) rowTemplate.SetActive(false);
            BuildToolbar();
        }

        private void OnEnable()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                ShowEmpty("NOT CONNECTED — STATISTICS NEED A SERVER SESSION.");
                ClearDetail();
                return;
            }
            net.OnMessage += HandleMessage;
            ShowEmpty("LOADING STATISTICS...");
            ClearDetail();
            net.RequestCardStats();
            net.RequestArchetypeStats();
        }

        private void OnDisable()
        {
            var net = NetworkManager.Instance;
            if (net != null) net.OnMessage -= HandleMessage;
        }

        private void HandleMessage(NetMessage m)
        {
            if (m == null) return;
            if (m.t == "stats_cards")
            {
                cards = m.cardStats ?? Array.Empty<StatsCard>();
                if (viewMode == 0) BuildList();
            }
            else if (m.t == "stats_archetypes")
            {
                archetypes = m.archetypeStats ?? Array.Empty<StatsArchetype>();
                archetypePairs = m.archetypePairs ?? Array.Empty<StatsArchPair>();
                if (viewMode == 1) BuildList();
            }
            else if (m.t == "stats_card_detail")
            {
                // Nur übernehmen, wenn die Antwort noch zur Auswahl passt
                var filtered = FilteredCards();
                if (viewMode == 0 && selected >= 0 && selected < filtered.Count && filtered[selected].n == m.card)
                    ShowPartners(m.partners ?? Array.Empty<StatsPair>());
            }
        }

        // ---------- Laufzeit-Toolbar: Umschalter + Filter ----------

        /// <summary>
        /// Zwei Zeilen über der Liste: CARDS|ARCHETYPES, darunter (nur Karten)
        /// die Kartenart- und Rarity-Chips. Der Scroll-Bereich rückt dafür herab.
        /// </summary>
        private void BuildToolbar()
        {
            if (listContent == null) return;
            var viewport = listContent.parent as RectTransform;
            var scroll = viewport != null ? viewport.parent as RectTransform : null;
            var panel = scroll != null ? scroll.parent as RectTransform : null;
            if (panel == null) return;

            const float rowH = 40f;
            float listTop = scroll.offsetMax.y;
            scroll.offsetMax = new Vector2(scroll.offsetMax.x, listTop - rowH * 2f);

            var fontSource = emptyHint != null ? emptyHint : panel.GetComponentInChildren<TMP_Text>(true);

            RectTransform MakeRow(string name, float top)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                var rect = (RectTransform)go.transform;
                rect.SetParent(panel, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(14f, top - rowH + 4f);
                rect.offsetMax = new Vector2(-14f, top - 2f);
                var layout = go.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 6f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                return rect;
            }

            void MakeChip(RectTransform parent, string label, float width, Func<bool> isActive, Action onClick)
            {
                var go = new GameObject("Chip_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                go.GetComponent<LayoutElement>().preferredWidth = width;
                var bg = go.GetComponent<Image>();
                var button = go.GetComponent<Button>();
                button.transition = Selectable.Transition.None;
                var textGo = new GameObject("Label", typeof(RectTransform));
                var textRect = (RectTransform)textGo.transform;
                textRect.SetParent(rect, false);
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
                var text = textGo.AddComponent<TextMeshProUGUI>();
                if (fontSource != null) { text.font = fontSource.font; text.fontSharedMaterial = fontSource.fontSharedMaterial; }
                text.fontSize = 12f;
                text.alignment = TextAlignmentOptions.Center;
                text.text = label;
                text.raycastTarget = false;
                button.onClick.AddListener(() => { SfxManager.Click(); onClick(); RefreshChips(); BuildList(); });
                chips.Add((button, bg, text, isActive));
            }

            var modeRow = MakeRow("ModeRow", listTop);
            MakeChip(modeRow, "CARDS", 92f, () => viewMode == 0, () => { viewMode = 0; selected = -1; });
            MakeChip(modeRow, "ARCHETYPES", 122f, () => viewMode == 1, () => { viewMode = 1; selected = -1; });

            filterRow = MakeRow("FilterRow", listTop - rowH).gameObject;
            var filterRect = (RectTransform)filterRow.transform;
            for (int i = 0; i < TypeNames.Length; i++)
            {
                int index = i;
                MakeChip(filterRect, TypeNames[i], i == 0 ? 48f : 86f, () => typeFilter == index, () => typeFilter = index);
            }
            for (int i = 0; i < RarityNames.Length; i++)
            {
                int index = i + 1;
                MakeChip(filterRect, RarityNames[i], 34f, () => rarityFilter == index,
                    () => rarityFilter = rarityFilter == index ? 0 : index);
            }
            RefreshChips();
        }

        private void RefreshChips()
        {
            var gold = new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f);
            foreach (var (button, bg, label, isActive) in chips)
            {
                bool active = isActive();
                bg.color = active ? new Color(gold.r, gold.g, gold.b, 0.30f) : new Color(0f, 0f, 0f, 0.45f);
                label.color = active ? new Color32(0xF3, 0xDD, 0xA4, 0xFF) : new Color32(0x8F, 0x80, 0x69, 0xFF);
            }
            if (filterRow != null) filterRow.SetActive(viewMode == 0);
        }

        // ---------- Liste ----------

        private List<StatsCard> FilteredCards()
        {
            IEnumerable<StatsCard> result = cards;
            if (typeFilter > 0 || rarityFilter > 0)
                result = result.Where(card =>
                {
                    var def = catalog != null ? catalog.FindByName(card.n) : null;
                    if (def == null) return false;
                    if (typeFilter == 1 && (!(def is MonsterCardData) || def.IsExtraDeckCard)) return false;
                    if (typeFilter == 2 && !(def is SpellCardData)) return false;
                    if (typeFilter == 3 && !(def is ArtifactCardData)) return false;
                    if (typeFilter == 4 && !(def is ReliquaryCardData)) return false;
                    if (typeFilter == 5 && !(def is IncarnateCardData)) return false;
                    if (rarityFilter > 0 && (int)def.rarity != rarityFilter - 1) return false;
                    return true;
                });
            return result.ToList();
        }

        private void BuildList()
        {
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            selected = -1;
            RefreshChips();

            if (listContent == null || rowTemplate == null) return;
            if (viewMode == 0) BuildCardList();
            else BuildArchetypeList();
        }

        private void BuildCardList()
        {
            var filtered = FilteredCards();
            if (filtered.Count == 0)
            {
                ShowEmpty(cards.Length == 0
                    ? "NO ONLINE MATCHES TRACKED YET — PLAY SOME ONLINE DUELS.\n<size=70%>SOLO GAMES DO NOT COUNT HERE.</size>"
                    : "NO CARDS MATCH THIS FILTER.");
                ClearDetail();
                return;
            }
            if (emptyHint != null) emptyHint.gameObject.SetActive(false);

            for (int i = 0; i < filtered.Count; i++)
            {
                int index = i;
                var card = filtered[i];
                var row = Instantiate(rowTemplate, listContent);
                row.SetActive(true);
                SetRowText(row, "NameText", card.n);
                SetRowText(row, "HeroText", DescribeCard(card.n));
                SetRowText(row, "GamesText", card.pvpGames + (card.pvpGames == 1 ? " GAME" : " GAMES"));
                var rateText = FindRowText(row, "WinrateText");
                if (rateText != null)
                {
                    int rate = Winrate(card.pvpWins, card.pvpGames);
                    rateText.text = rate + "%";
                    rateText.color = rate >= 50 ? goodWinrate : badWinrate;
                }
                var button = row.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => Select(index));
                rows.Add(row);
            }
            Select(0);
        }

        private void BuildArchetypeList()
        {
            if (archetypes.Length == 0)
            {
                ShowEmpty("NO ARCHETYPE DATA YET — PLAY SOME ONLINE DUELS.\n<size=70%>A DECK COUNTS FOR AN ARCHETYPE FROM 4 CARDS UP.</size>");
                ClearDetail();
                return;
            }
            if (emptyHint != null) emptyHint.gameObject.SetActive(false);

            for (int i = 0; i < archetypes.Length; i++)
            {
                int index = i;
                var archetype = archetypes[i];
                var row = Instantiate(rowTemplate, listContent);
                row.SetActive(true);
                SetRowText(row, "NameText", archetype.n.ToUpperInvariant());
                SetRowText(row, "HeroText", "ARCHETYPE");
                SetRowText(row, "GamesText", archetype.games + (archetype.games == 1 ? " GAME" : " GAMES"));
                var rateText = FindRowText(row, "WinrateText");
                if (rateText != null)
                {
                    int rate = Winrate(archetype.wins, archetype.games);
                    rateText.text = rate + "%";
                    rateText.color = rate >= 50 ? goodWinrate : badWinrate;
                }
                var button = row.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => Select(index));
                rows.Add(row);
            }
            Select(0);
        }

        /// <summary>Kartenart + Rarity aus dem Katalog — reine Anzeige-Hilfe.</summary>
        private string DescribeCard(string cardName)
        {
            var def = catalog != null ? catalog.FindByName(cardName) : null;
            if (def == null) return "";
            string kind = def is ReliquaryCardData ? "RELIQUARY"
                : def is IncarnateCardData ? "INCARNATE"
                : def is MonsterCardData ? "MONSTER"
                : def is SpellCardData s && s.isRite ? "RITE"
                : def is SpellCardData ? "SPELL"
                : def is ArtifactCardData ? "ARTIFACT" : "CARD";
            return kind + " · " + def.rarity.ToString().ToUpperInvariant();
        }

        private void Select(int index)
        {
            selected = index;
            for (int i = 0; i < rows.Count; i++)
            {
                var image = rows[i].GetComponent<Image>();
                if (image != null) image.color = i == index ? rowSelectedColor : rowIdleColor;
            }

            if (viewMode == 0)
            {
                var filtered = FilteredCards();
                if (index < 0 || index >= filtered.Count) return;
                ShowDetail(filtered[index]);
                var net = NetworkManager.Instance;
                if (net != null && net.IsConnected) net.RequestCardDetail(filtered[index].n);
            }
            else
            {
                if (index < 0 || index >= archetypes.Length) return;
                ShowArchetypeDetail(archetypes[index]);
            }
        }

        private void ShowDetail(StatsCard card)
        {
            if (detailTitle != null) detailTitle.text = card.n;
            if (detailSummary != null)
            {
                int rate = Winrate(card.pvpWins, card.pvpGames);
                string line = $"{card.pvpGames} GAMES  ·  {card.pvpWins} WINS  ·  <color=#{ColorUtility.ToHtmlStringRGB(rate >= 50 ? goodWinrate : badWinrate)}>{rate}% WINRATE</color>";
                line += "\n<size=80%>ONLINE MATCHES ONLY — SOLO GAMES ARE NOT TRACKED.</size>";
                detailSummary.text = line;
            }
            if (detailCards != null)
                detailCards.text = "<color=#C9B37E>OFTEN PAIRED WITH</color>\n<color=#8F8069>loading...</color>";
        }

        private void ShowArchetypeDetail(StatsArchetype archetype)
        {
            if (detailTitle != null) detailTitle.text = archetype.n.ToUpperInvariant();
            if (detailSummary != null)
            {
                int rate = Winrate(archetype.wins, archetype.games);
                string line = $"{archetype.games} GAMES  ·  {archetype.wins} WINS  ·  <color=#{ColorUtility.ToHtmlStringRGB(rate >= 50 ? goodWinrate : badWinrate)}>{rate}% WINRATE</color>";
                line += "\n<size=80%>ONLINE MATCHES ONLY — A DECK COUNTS FROM 4 CARDS UP.</size>";
                detailSummary.text = line;
            }
            if (detailCards == null) return;

            // Die Duos dieses Archetypes, beste Bilanz zuerst
            var duos = archetypePairs
                .Where(pair => pair.a == archetype.n || pair.b == archetype.n)
                .OrderByDescending(pair => Winrate(pair.wins, pair.games))
                .ThenByDescending(pair => pair.games)
                .ToList();
            var text = new System.Text.StringBuilder();
            text.AppendLine("<color=#C9B37E>BEST PAIRED WITH</color>");
            if (duos.Count == 0)
                text.AppendLine("<color=#8F8069>No archetype duos tracked yet.</color>");
            else foreach (var duo in duos)
            {
                string partner = duo.a == archetype.n ? duo.b : duo.a;
                int rate = Winrate(duo.wins, duo.games);
                string rateHex = ColorUtility.ToHtmlStringRGB(rate >= 50 ? goodWinrate : badWinrate);
                text.AppendLine($"<color=#8F8069>{duo.games}× together · <color=#{rateHex}>{rate}%</color></color>  {partner.ToUpperInvariant()}");
            }
            detailCards.text = text.ToString();
        }

        private void ShowPartners(StatsPair[] partners)
        {
            if (detailCards == null || viewMode != 0) return;
            var text = new System.Text.StringBuilder();
            text.AppendLine("<color=#C9B37E>OFTEN PAIRED WITH</color>");
            if (partners.Length == 0)
            {
                text.AppendLine("<color=#8F8069>No pairings tracked yet.</color>");
            }
            else foreach (var partner in partners)
            {
                int rate = Winrate(partner.wins, partner.games);
                string rateHex = ColorUtility.ToHtmlStringRGB(rate >= 50 ? goodWinrate : badWinrate);
                text.AppendLine($"<color=#8F8069>{partner.games}× together · <color=#{rateHex}>{rate}%</color></color>  {partner.n}");
            }
            detailCards.text = text.ToString();
        }

        private void ClearDetail()
        {
            if (detailTitle != null) detailTitle.text = "";
            if (detailSummary != null) detailSummary.text = "";
            if (detailCards != null) detailCards.text = "";
        }

        private void ShowEmpty(string message)
        {
            if (emptyHint == null) return;
            emptyHint.gameObject.SetActive(true);
            emptyHint.text = message;
        }

        private static int Winrate(int wins, int games) =>
            games <= 0 ? 0 : Mathf.RoundToInt(100f * wins / games);

        private static void SetRowText(GameObject row, string childName, string value)
        {
            var text = FindRowText(row, childName);
            if (text != null) text.text = value;
        }

        private static TMP_Text FindRowText(GameObject row, string childName)
        {
            var child = row.transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
