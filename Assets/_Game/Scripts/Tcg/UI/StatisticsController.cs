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
    /// Statistik-Seite der Sammlung: welche KARTEN gespielt werden, wie oft sie
    /// gewinnen und mit wem sie im Deck stehen. Gewertet werden NUR
    /// Online-Matches (Solo gegen Bots verzerrt die Winrates); ein Match zählt
    /// je Karte einmal (Kopien egal). Die Zahlen kommen fertig vom Server
    /// (stats_cards / stats_card_detail) — der Client rechnet nur die Prozente.
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
        private int selected = -1;

        private void Awake()
        {
            if (decksTabButton != null) decksTabButton.onClick.AddListener(() => SceneManager.LoadScene(decksSceneName));
            if (shopTabButton != null) shopTabButton.onClick.AddListener(() => SceneManager.LoadScene(shopSceneName));
            if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (rowTemplate != null) rowTemplate.SetActive(false);
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
                BuildList();
            }
            else if (m.t == "stats_card_detail")
            {
                // Nur übernehmen, wenn die Antwort noch zur Auswahl passt
                if (selected >= 0 && selected < cards.Length && cards[selected].n == m.card)
                    ShowPartners(m.partners ?? Array.Empty<StatsPair>());
            }
        }

        private void BuildList()
        {
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            selected = -1;

            if (cards.Length == 0 || listContent == null || rowTemplate == null)
            {
                ShowEmpty("NO ONLINE MATCHES TRACKED YET — PLAY SOME ONLINE DUELS.\n<size=70%>SOLO GAMES DO NOT COUNT HERE.</size>");
                ClearDetail();
                return;
            }
            if (emptyHint != null) emptyHint.gameObject.SetActive(false);

            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                var card = cards[i];
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

        /// <summary>Kartenart + Rarity aus dem Katalog — reine Anzeige-Hilfe.</summary>
        private string DescribeCard(string cardName)
        {
            var def = catalog != null ? catalog.FindByName(cardName) : null;
            if (def == null) return "";
            string kind = def is ReliquaryCardData ? "RELIQUARY"
                : def is MonsterCardData ? "MONSTER"
                : def is SpellCardData ? "SPELL"
                : def is ArtifactCardData ? "ARTIFACT" : "CARD";
            return kind + " · " + def.rarity.ToString().ToUpperInvariant();
        }

        private void Select(int index)
        {
            if (index < 0 || index >= cards.Length) return;
            selected = index;
            for (int i = 0; i < rows.Count; i++)
            {
                var image = rows[i].GetComponent<Image>();
                if (image != null) image.color = i == index ? rowSelectedColor : rowIdleColor;
            }
            ShowDetail(cards[index]);
            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected) net.RequestCardDetail(cards[index].n);
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

        private void ShowPartners(StatsPair[] partners)
        {
            if (detailCards == null) return;
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
