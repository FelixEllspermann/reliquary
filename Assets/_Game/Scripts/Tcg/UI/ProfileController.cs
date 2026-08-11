using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Eigenständige Profil-Seite: Rang und Saison-Bilanz, Gesamt-Statistiken
    /// (PvP/Solo), die letzten Spiele und laufende Duelle zum Zuschauen.
    /// Kosmetik/Titel bleiben im bestehenden ProfilePanel — der CUSTOMIZE-Knopf
    /// öffnet es als Overlay über dieser Seite.
    /// </summary>
    public class ProfileController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button customizeButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string duelSceneName = "Duel";

        [Header("Spieler-Panel")]
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text seasonText;
        [SerializeField] private TMP_Text totalsText;
        [SerializeField] private TMP_Text collectionText;

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

        private void HandleMessage(NetMessage m)
        {
            if (m == null || m.t != "profile_stats") return;

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
