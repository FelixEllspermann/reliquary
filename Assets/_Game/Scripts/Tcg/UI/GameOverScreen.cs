using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>Sieg-/Niederlage-Overlay am Duell-Ende (aus Sicht von Spieler 1).</summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("duel")]
        private DuelHost duelHost;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button deckEditorButton;

        [Header("Reliquary-Stil")]
        [SerializeField] private Image panelFrame;     // relicFrame — Sieg gold, Niederlage ember getönt
        [SerializeField] private Image[] rivets = new Image[4];
        [SerializeField] private Image emblemOuter;
        [SerializeField] private Image emblemInner;
        [SerializeField] private Image emblemCore;
        [SerializeField] private TMP_Text eyebrowText;
        [SerializeField] private GameObject rewardStrip;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text restartLabel;
        [SerializeField] private GoldShimmerText titleShimmer;

        [Header("Einstellungen")]
        [Tooltip("Verzögerung, bevor der Screen erscheint (Sekunden)")]
        [Range(0f, 5f)]
        [SerializeField] private float showDelay = 1.2f;

        [Tooltip("Einblendzeit")]
        [Range(0.05f, 2f)]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("Name der Hauptmenü-Szene")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        /// <summary>Die Engine hinter dem Host.</summary>
        private DuelManager duel => duelHost != null ? duelHost.Duel : null;

        private void Awake()
        {
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (deckEditorButton != null) deckEditorButton.onClick.AddListener(OpenDeckEditor);
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (duel != null)
            {
                duel.OnDuelEnded += HandleDuelEnded;
                duel.OnLifeChanged += HandleLifeChanged;
            }
        }

        private void OnDisable()
        {
            if (duel != null)
            {
                duel.OnDuelEnded -= HandleDuelEnded;
                duel.OnLifeChanged -= HandleLifeChanged;
            }
        }

        // Der letzte Treffer auf den eigenen Helden — die Niederlage-Sequenz zeigt
        // ihn als Zahl und lässt die Lebensanzeige von dort leerlaufen.
        private int lastLifeBeforeHit = 400;
        private int lastHit = 1;

        private void HandleLifeChanged(PlayerState player, int delta)
        {
            var local = duel != null ? (duel.LocalPlayer ?? duel.Player1) : null;
            if (player != local || delta >= 0) return;
            lastHit = -delta;
            lastLifeBeforeHit = player.LifePoints - delta;   // delta ist negativ
        }

        private void HandleDuelEnded(DuelResult result)
        {
            StartCoroutine(ShowRoutine(result));
        }

        private IEnumerator ShowRoutine(DuelResult result)
        {
            yield return new WaitForSeconds(showDelay);

            var local = duel.LocalPlayer != null ? duel.LocalPlayer : duel.Player1;
            bool localIsPlayer1 = local == duel.Player1;
            bool victory = (result == DuelResult.Player1Wins) == localIsPlayer1;
            string opponent = local != null && local.Opponent != null ? local.Opponent.Name : "the opponent";

            // Bei einer Niederlage läuft erst die Sequenz (Handoff „Animations",
            // Abschnitt 5) — sie bringt ihren eigenen Abspann mit. Das Panel kommt
            // danach, für die Knöpfe.
            if (!victory)
            {
                bool done = false;
                PlayerDefeatSequence.Play(opponent, duel.TurnNumber,
                    Rouge.Tcg.Net.PlayerProfile.TakeRpDelta(),
                    Mathf.Max(0, lastLifeBeforeHit), Mathf.Max(1, lastHit),
                    () => done = true);
                while (!done) yield return null;

                // Danach das Siegel des Gegners: es ist seine Unterschrift unter
                // den Sieg, und eine Unterschrift, die nur er selbst sieht, ist
                // keine. Nach der Niederlage-Sequenz, nicht über ihr.
                bool sealed_ = false;
                VictorySealSequence.PlayForLoser(
                    Rouge.Tcg.Net.MatchContext.RemoteEquipped("victorySeal"),
                    opponent, duel.TurnNumber, () => sealed_ = true);
                while (!sealed_) yield return null;
            }
            else
            {
                // Beim Sieg landet das ausgerüstete Siegel (Handoff „Cosmetics",
                // Abschnitt 6). Ohne Ausrüstung läuft still das Grundsiegel.
                bool done = false;
                VictorySealSequence.Play(
                    Rouge.Tcg.Net.Cosmetics.EquippedIn("victorySeal"),
                    opponent, duel.TurnNumber,
                    Rouge.Tcg.Net.PlayerProfile.TakeRpDelta(),
                    () => done = true);
                while (!done) yield return null;
            }

            // Sieg = Gold mit Shimmer, Niederlage = Ember
            Color accent = victory ? new Color32(0xC8, 0xA4, 0x5C, 0xFF) : new Color32(0xC9, 0x7A, 0x5C, 0xFF);
            Color bright = victory ? new Color32(0xF3, 0xDD, 0xA4, 0xFF) : new Color32(0xE0, 0x60, 0x3A, 0xFF);
            if (titleText != null)
            {
                titleText.text = victory ? "VICTORY" : "DEFEAT";
                titleText.color = bright;
            }
            if (titleShimmer != null) titleShimmer.enabled = victory;
            if (panelFrame != null) panelFrame.color = victory ? Color.white : new Color(1f, 0.62f, 0.5f, 1f);
            foreach (var rivet in rivets) if (rivet != null) rivet.color = accent;
            if (emblemOuter != null) emblemOuter.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            if (emblemInner != null) emblemInner.color = new Color(accent.r, accent.g, accent.b, 0.35f);
            if (emblemCore != null) emblemCore.color = bright;
            if (eyebrowText != null) eyebrowText.text = victory ? "THE VAULT REMEMBERS YOUR NAME" : "THE DUEL IS SEALED";

            bool online = Rouge.Tcg.Net.PlayerProfile.LoggedIn
                && Rouge.Tcg.Net.NetworkManager.Instance != null && Rouge.Tcg.Net.NetworkManager.Instance.IsConnected;
            string reward = null;
            if (Rouge.Tcg.Net.MatchContext.IsServerMatch)
            {
                // Server-Duell: die Belohnung hat der Server bereits autoritativ gutgeschrieben
                reward = victory ? "+200 COINS SEALED INTO YOUR VAULT" : "+100 COINS FOR THE DUEL";
            }
            else if (online)
            {
                Rouge.Tcg.Net.NetworkManager.Instance.SendSoloResult(victory);
                reward = "+50 COINS SEALED INTO YOUR VAULT";
            }
            if (rewardStrip != null) rewardStrip.SetActive(reward != null);
            if (rewardText != null && reward != null) rewardText.text = reward;

            if (subtitleText != null)
                subtitleText.text = victory ? $"You defeated {opponent}." : $"{opponent} defeated you.";

            // Button-Beschriftung: im Netz-/Server-Match führt "Nochmal" ohnehin ins Menü
            bool network = Rouge.Tcg.Net.MatchContext.IsServerMatch;
            if (restartLabel != null) restartLabel.text = network ? "RETURN TO MENU" : "DUEL AGAIN";
            if (deckEditorButton != null) deckEditorButton.gameObject.SetActive(!network);

            if (panelGroup == null) yield break;
            panelGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                panelGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            panelGroup.alpha = 1f;
        }

        private void Restart()
        {
            if (Rouge.Tcg.Net.MatchContext.IsServerMatch)
            {
                LeaveNetworkMatch();
                AfterRankUp(() => SceneManager.LoadScene(mainMenuSceneName));
                return;
            }
            // Solo/Offline: gleiche Aufstellung nochmal (MatchContext bleibt erhalten)
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void OpenDeckEditor()
        {
            if (Rouge.Tcg.Net.MatchContext.IsServerMatch) LeaveNetworkMatch();
            else Rouge.Tcg.Net.MatchContext.Clear();
            AfterRankUp(() => SceneManager.LoadScene(mainMenuSceneName));
        }

        /// <summary>
        /// Steht ein Aufstieg an, läuft er erst — dann geht es weiter. Der
        /// Ergebnis-Bildschirm ist zu diesem Zeitpunkt bereits gelesen, und die
        /// Sequenz bringt ihren eigenen Ergebnis-Auftakt mit.
        /// </summary>
        private static void AfterRankUp(System.Action next)
        {
            var rankUp = Rouge.Tcg.Net.PlayerProfile.TakeRankUp();
            if (rankUp == null) { next(); return; }
            RankUpSequence.Play(rankUp.From, rankUp.Into, rankUp.Gain, rankUp.Opponent, next);
        }

        private static void LeaveNetworkMatch()
        {
            Rouge.Tcg.Net.MatchContext.Clear();
            if (Rouge.Tcg.Net.NetworkManager.Instance != null)
                Rouge.Tcg.Net.NetworkManager.Instance.SendLeave();
        }
    }
}
