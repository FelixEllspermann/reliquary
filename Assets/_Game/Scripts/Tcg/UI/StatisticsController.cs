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
    /// Statistik-Seite der Sammlung: welche Decks gespielt werden, wie oft sie
    /// gewinnen und was drinsteckt. Die Zahlen kommen fertig vom Server
    /// (stats_decks) — der Client rechnet nur die Prozentanzeige.
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

        [Header("Deck-Liste")]
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private TMP_Text emptyHint;

        [Header("Detail-Panel")]
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailSummary;
        [SerializeField] private TMP_Text detailCards;

        [Header("Farben")]
        [SerializeField] private Color rowIdleColor = new Color(0.10f, 0.08f, 0.05f, 0.85f);
        [SerializeField] private Color rowSelectedColor = new Color(0.72f, 0.58f, 0.28f, 0.35f);
        [SerializeField] private Color goodWinrate = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] private Color badWinrate = new Color(0.90f, 0.45f, 0.38f);

        private readonly List<GameObject> rows = new List<GameObject>();
        private StatsDeck[] decks = Array.Empty<StatsDeck>();
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
            net.RequestDeckStats();
        }

        private void OnDisable()
        {
            var net = NetworkManager.Instance;
            if (net != null) net.OnMessage -= HandleMessage;
        }

        private void HandleMessage(NetMessage m)
        {
            if (m == null || m.t != "stats_decks") return;
            decks = m.decks ?? Array.Empty<StatsDeck>();
            BuildList();
        }

        private void BuildList()
        {
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            selected = -1;

            if (decks.Length == 0 || listContent == null || rowTemplate == null)
            {
                ShowEmpty("NO MATCHES TRACKED YET — PLAY SOME DUELS.");
                ClearDetail();
                return;
            }
            if (emptyHint != null) emptyHint.gameObject.SetActive(false);

            for (int i = 0; i < decks.Length; i++)
            {
                int index = i;
                var deck = decks[i];
                var row = Instantiate(rowTemplate, listContent);
                row.SetActive(true);
                SetRowText(row, "NameText", string.IsNullOrEmpty(deck.name) ? "UNNAMED DECK" : deck.name.ToUpperInvariant());
                SetRowText(row, "HeroText", string.IsNullOrEmpty(deck.hero) ? "" : "HERO · " + deck.hero.ToUpperInvariant());
                SetRowText(row, "GamesText", deck.games + (deck.games == 1 ? " GAME" : " GAMES"));
                var rateText = FindRowText(row, "WinrateText");
                if (rateText != null)
                {
                    int rate = Winrate(deck.wins, deck.games);
                    rateText.text = rate + "%";
                    rateText.color = rate >= 50 ? goodWinrate : badWinrate;
                }
                var button = row.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => Select(index));
                rows.Add(row);
            }
            Select(0);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= decks.Length) return;
            selected = index;
            for (int i = 0; i < rows.Count; i++)
            {
                var image = rows[i].GetComponent<Image>();
                if (image != null) image.color = i == index ? rowSelectedColor : rowIdleColor;
            }
            ShowDetail(decks[index]);
        }

        private void ShowDetail(StatsDeck deck)
        {
            if (detailTitle != null)
            {
                string hero = string.IsNullOrEmpty(deck.hero) ? "" : "  ·  HERO: " + deck.hero.ToUpperInvariant();
                detailTitle.text = (string.IsNullOrEmpty(deck.name) ? "UNNAMED DECK" : deck.name.ToUpperInvariant()) + hero;
            }
            if (detailSummary != null)
            {
                int rate = Winrate(deck.wins, deck.games);
                string line = $"{deck.games} GAMES  ·  {deck.wins} WINS  ·  <color=#{ColorUtility.ToHtmlStringRGB(rate >= 50 ? goodWinrate : badWinrate)}>{rate}% WINRATE</color>";
                if (deck.pvpGames > 0)
                    line += $"\n<size=80%>PVP: {deck.pvpWins}/{deck.pvpGames} ({Winrate(deck.pvpWins, deck.pvpGames)}%)  ·  SOLO: {deck.wins - deck.pvpWins}/{deck.games - deck.pvpGames}</size>";
                else
                    line += $"\n<size=80%>ALL MATCHES AGAINST BOTS (SOLO)</size>";
                detailSummary.text = line;
            }
            if (detailCards != null)
            {
                var text = new System.Text.StringBuilder();
                int mainTotal = deck.cards?.Sum(e => e.c) ?? 0;
                text.AppendLine($"<color=#C9B37E>MAIN DECK ({mainTotal})</color>");
                AppendCardLines(text, deck.cards);
                if (deck.extra != null && deck.extra.Length > 0)
                {
                    text.AppendLine();
                    text.AppendLine($"<color=#C9B37E>EXTRA DECK ({deck.extra.Sum(e => e.c)})</color>");
                    AppendCardLines(text, deck.extra);
                }
                detailCards.text = text.ToString();
            }
        }

        private static void AppendCardLines(System.Text.StringBuilder text, StatsCardCount[] entries)
        {
            if (entries == null) return;
            foreach (var entry in entries.OrderByDescending(e => e.c).ThenBy(e => e.n, StringComparer.Ordinal))
                text.AppendLine($"<color=#8F8069>{entry.c}×</color>  {entry.n}");
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
