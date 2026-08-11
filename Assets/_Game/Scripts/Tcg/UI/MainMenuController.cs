using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Hauptmenü (Shell-Design "Reliquary"): Top-Bar mit Spieler-Plate/Coins/Decks/Logout,
    /// fünf Kachel-Tiles mit lebenden Parchment-Strips, Bottom-Rail mit aktivem Deck und
    /// Daily-Siegel (Server-gestützt), Version + Online-Zähler.
    ///
    /// Die fünfte Kachel CHALLENGES (crimson, führt zum Tower) entsteht zur
    /// Laufzeit als umgefärbter Klon der SOLO-Kachel — die Szene bleibt bei
    /// ihren vier verdrahteten Kacheln, nur die Reihe rückt enger zusammen.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Navigation (im Inspector verdrahten)")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button soloButton;
        [SerializeField] private Button challengesButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button decksButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private TMP_Text infoText;   // alt — bleibt als Fallback
        [SerializeField] private TMP_Text hintText;   // alt — bleibt als Fallback

        [Header("Top-Bar")]
        [SerializeField] private TMP_Text playerInitial;
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text playerRank;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text decksCountText;

        [Header("Tile-Strips (Pergament, lebender Kontext)")]
        [SerializeField] private TMP_Text playStrip;
        [SerializeField] private TMP_Text soloStrip;
        [SerializeField] private TMP_Text challengesStrip;
        [SerializeField] private TMP_Text shopStrip;
        [SerializeField] private TMP_Text decksStrip;

        [Header("Bottom-Rail: Aktives Deck")]
        [SerializeField] private TMP_Text activeDeckName;
        [SerializeField] private TMP_Text activeDeckComposition;
        [SerializeField] private Button switchDeckButton;

        [Header("Bottom-Rail: Daily-Siegel")]
        [SerializeField] private Image[] dailySegments = new Image[7];
        [SerializeField] private TMP_Text dailyRewardText;
        [SerializeField] private TMP_Text dailyResetText;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject claimedChip;

        [Header("Meta")]
        [SerializeField] private TMP_Text onlineText;
        [SerializeField] private CardCatalog catalog;

        [Header("Szenen")]
        [SerializeField] private string playSceneName = "Play";
        [SerializeField] private string shopSceneName = "Shop";
        [SerializeField] private string decksSceneName = "DeckEditor";
        [SerializeField] private string loginSceneName = "Login";

        public const string ActiveDeckPrefKey = "rouge_active_deck_index";

        private float displayedCoins = -1f;
        private static readonly Color SegmentEmpty = new Color(0.784f, 0.643f, 0.361f, 0.16f);

        private void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(() =>
            {
                DuelSetupController.OpenSolo = false;
                SceneManager.LoadScene(playSceneName);
            });
            if (challengesButton != null) challengesButton.onClick.AddListener(() =>
            {
                DuelSetupController.OpenTower = true;
                SceneManager.LoadScene(playSceneName);
            });
            if (soloButton != null) soloButton.onClick.AddListener(() =>
            {
                DuelSetupController.OpenSolo = true;
                SceneManager.LoadScene(playSceneName); // ein Setup-Screen für beide Modi
            });
            if (shopButton != null) shopButton.onClick.AddListener(() => SceneManager.LoadScene(shopSceneName));
            if (decksButton != null) decksButton.onClick.AddListener(() => SceneManager.LoadScene(decksSceneName));
            if (logoutButton != null) logoutButton.onClick.AddListener(Logout);
            if (switchDeckButton != null) switchDeckButton.onClick.AddListener(() => SceneManager.LoadScene(decksSceneName));
            if (claimButton != null) claimButton.onClick.AddListener(ClaimDaily);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessage += HandleMessage;
                NetworkManager.Instance.OnDisconnected += HandleDisconnected;
            }
            // Die Build-Nummer setzt jetzt VersionLabel am Textfeld selbst —
            // eine Stelle für alle Screens statt eine pro Controller.
            Refresh();
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessage -= HandleMessage;
                NetworkManager.Instance.OnDisconnected -= HandleDisconnected;
            }
        }

        /// <summary>
        /// Dreht den Farbton aller Grafiken unter root ins Ziel — Sättigung,
        /// Helligkeit und Alpha jeder Fläche bleiben, so überleben Gradient und
        /// Glow. Getauscht wird nur, was im Quell-Farbtonfenster liegt.
        /// (Die CHALLENGES-Kachel selbst steht inzwischen fest in der Szene —
        /// hiermit färbt sich nur noch der Discord-Knopf.)
        /// </summary>
        public static void SwapHue(GameObject root, float fromMin, float fromMax, float targetHue)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Color.RGBToHSV(graphic.color, out float h, out float s, out float v);
                if (s < 0.06f) continue;               // Schwarz/Weiß/Parchment bleibt
                if (h < fromMin || h > fromMax) continue;
                float alpha = graphic.color.a;
                var swapped = Color.HSVToRGB(targetHue, s, v);
                graphic.color = new Color(swapped.r, swapped.g, swapped.b, alpha);
            }
        }

        /// <summary>Ohne Offline-Modus führt ein Verbindungsabbruch zurück zum Login.</summary>
        private void HandleDisconnected(string reason)
        {
            PlayerProfile.Clear();
            SceneManager.LoadScene(loginSceneName);
        }

        private void HandleMessage(NetMessage message)
        {
            if (message.t == "profile" || message.t == "auth_ok")
            {
                // Der Server bestätigt das Einlösen mit einem frischen Profil — erst dann klingelt es
                if (awaitingDailyClaim)
                {
                    awaitingDailyClaim = false;
                    SfxManager.Claim();
                }
                Refresh();
            }
            else if (message.t == "error") awaitingDailyClaim = false;
        }

        private void Update()
        {
            // Coin-Zähler weich hochzählen (~600 ms)
            if (coinsText != null && displayedCoins >= 0f && !Mathf.Approximately(displayedCoins, PlayerProfile.Coins))
            {
                displayedCoins = Mathf.MoveTowards(displayedCoins, PlayerProfile.Coins,
                    Mathf.Max(60f, Mathf.Abs(PlayerProfile.Coins - displayedCoins) * Time.unscaledDeltaTime / 0.6f * 4f));
                if (Mathf.Abs(displayedCoins - PlayerProfile.Coins) < 1f) displayedCoins = PlayerProfile.Coins;
                coinsText.text = FormatCoins(Mathf.RoundToInt(displayedCoins));
            }

            // Daily-Countdown live ticken
            if (dailyResetText != null && PlayerProfile.LoggedIn && !PlayerProfile.DailyClaimable)
            {
                long remaining = RemainingDailyMs();
                dailyResetText.text = remaining <= 0 ? "SEAL READY" : $"RESETS IN {FormatDuration(remaining)}";
                if (remaining <= 0 && claimButton != null && !claimButton.gameObject.activeSelf) RefreshDaily();
            }
        }

        private static long RemainingDailyMs()
        {
            double elapsed = (System.DateTime.UtcNow - PlayerProfile.ProfileReceivedAt).TotalMilliseconds;
            return PlayerProfile.DailyNextInMs - (long)elapsed;
        }

        private Button plateButton;

        /// <summary>
        /// Die Spieler-Plakette oben öffnet das Profil. Der Knopf wird einmalig an
        /// das vorhandene Plaketten-Objekt gehängt, damit die Szene unverändert
        /// bleibt — gesucht wird über den gemeinsamen Elternteil von Name und Rang.
        /// </summary>
        private void EnsurePlateButton(bool online)
        {
            if (plateButton == null)
            {
                var plate = playerName != null ? playerName.transform.parent as RectTransform : null;
                if (plate == null) return;

                // Ohne eigene Grafik gäbe es keine Trefferfläche
                var hit = plate.GetComponent<Image>();
                if (hit == null)
                {
                    hit = plate.gameObject.AddComponent<Image>();
                    hit.color = new Color(0f, 0f, 0f, 0f);
                }
                hit.raycastTarget = true;

                plateButton = plate.gameObject.GetComponent<Button>();
                if (plateButton == null) plateButton = plate.gameObject.AddComponent<Button>();
                plateButton.transition = Selectable.Transition.None;
                // Das Profil ist inzwischen eine eigene Seite (Kosmetik/Titel öffnet
                // sie dort als CUSTOMIZE-Overlay).
                plateButton.onClick.AddListener(() => SceneManager.LoadScene("Profile"));
            }
            plateButton.interactable = online;
        }

        private static string FormatDuration(long ms)
        {
            var time = System.TimeSpan.FromMilliseconds(ms);
            return time.TotalHours >= 1 ? $"{(int)time.TotalHours}H {time.Minutes:D2}M" : $"{time.Minutes}M {time.Seconds:D2}S";
        }

        /// <summary>Tausender mit schmalem Abstand statt Komma (Handoff: „1 000 049“).</summary>
        public static string FormatCoins(int amount)
        {
            string raw = amount.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && (raw.Length - i) % 3 == 0) sb.Append("<space=0.18em>");
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Legt Profilbild und Kosmetik-Rahmen über das Wappen der Topbar —
        /// dieselbe Schichtung wie im Profil-Screen, aufs 42px-Fenster verkleinert.
        /// Idempotent: Refresh läuft bei jedem Profil-Update erneut.
        /// </summary>
        private void DecorateCrest(bool online)
        {
            if (playerInitial == null) return;
            var crest = playerInitial.transform.parent as RectTransform;
            if (crest == null) return;

            for (int i = crest.childCount - 1; i >= 0; i--)
            {
                var child = crest.GetChild(i);
                if (child.name == "MiniAvatar" || child.name == "MiniFrame") Destroy(child.gameObject);
            }

            var crestImage = crest.GetComponent<Image>();
            if (crestImage != null) crestImage.enabled = true;
            bool showInitial = true;

            if (online)
            {
                float window = crest.rect.height > 0f ? crest.rect.height : 42f;
                var avatarSprite = CosmeticArt.EquippedAvatar();
                if (avatarSprite != null)
                {
                    var avatar = new GameObject("MiniAvatar", typeof(RectTransform), typeof(Image));
                    var aRect = (RectTransform)avatar.transform;
                    aRect.SetParent(crest, false);
                    aRect.anchorMin = aRect.anchorMax = new Vector2(0.5f, 0.5f);
                    aRect.pivot = new Vector2(0.5f, 0.5f);
                    aRect.sizeDelta = new Vector2(window, window);
                    var aImg = avatar.GetComponent<Image>();
                    aImg.sprite = avatarSprite;
                    aImg.raycastTarget = false;
                    showInitial = false;
                }

                var frameSprite = CosmeticArt.EquippedFrame();
                if (frameSprite != null)
                {
                    var frame = new GameObject("MiniFrame", typeof(RectTransform), typeof(Image));
                    var fRect = (RectTransform)frame.transform;
                    fRect.SetParent(crest, false);
                    fRect.anchorMin = fRect.anchorMax = new Vector2(0.5f, 0.5f);
                    fRect.pivot = new Vector2(0.5f, 0.5f);
                    var fImg = frame.GetComponent<Image>();
                    fImg.sprite = frameSprite;
                    fImg.raycastTarget = false;

                    string frameId = Cosmetics.EquippedIn("avatarFrame");
                    if (CosmeticArt.IsPlaque(frameId))
                    {
                        // Bilderrahmen: aufs Fenster skaliert, die eckige Wappenplatte
                        // verschwindet dahinter — ihre Ecken lugten sonst hervor.
                        if (crestImage != null) crestImage.enabled = false;
                        float scale = CosmeticArt.PlaqueScale(frameId, window);
                        fRect.sizeDelta = new Vector2(
                            frameSprite.rect.width * scale, frameSprite.rect.height * scale);
                    }
                    else
                    {
                        fRect.sizeDelta = new Vector2(window + 8f, window + 8f);
                        fImg.preserveAspect = true;
                    }
                }
            }

            playerInitial.gameObject.SetActive(showInitial);
        }

        private void Refresh()
        {
            bool online = PlayerProfile.LoggedIn && NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;
            if (playButton != null) playButton.interactable = online;
            if (shopButton != null) shopButton.interactable = online;
            if (decksButton != null) decksButton.interactable = online;
            if (logoutButton != null) logoutButton.gameObject.SetActive(online);

            // Spieler-Plate + Pills
            string name = online ? PlayerProfile.AccountName : "Wanderer";
            if (playerInitial != null) playerInitial.text = name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "?";
            DecorateCrest(online);
            if (playerName != null) playerName.text = name;
            if (playerRank != null)
                playerRank.text = online
                    ? PlayerProfile.Rank.Seal.Label.ToUpperInvariant()
                    : "OFFLINE — NO ACCOUNT";
            EnsurePlateButton(online);
            if (coinsText != null)
            {
                if (displayedCoins < 0f) { displayedCoins = PlayerProfile.Coins; coinsText.text = FormatCoins(PlayerProfile.Coins); }
            }
            if (decksCountText != null) decksCountText.text = PlayerProfile.Decks.Count.ToString();

            // Lebende Parchment-Strips
            int duelists = Mathf.Max(1, PlayerProfile.OnlineCount);
            if (playStrip != null)
                playStrip.text = online ? $"{duelists} duelist{(duelists == 1 ? "" : "s")} in the vault — +100 coins per duel" : "Requires an account — log in first";
            if (soloStrip != null)
                soloStrip.text = "The Warden awaits — +50 coins per trial";
            if (challengesStrip != null)
            {
                int sealedFloors = PlayerProfile.TowerFloor;
                challengesStrip.text = !online ? "Requires an account — log in first"
                    : sealedFloors <= 0 ? "The Tower rises — its keepers await"
                    : $"The Tower — {sealedFloors} floor{(sealedFloors == 1 ? "" : "s")} sealed";
            }
            if (shopStrip != null)
            {
                int packs = 0;
                foreach (var count in PlayerProfile.PackInventory.Values) packs += count;
                shopStrip.text = !online ? "Requires an account — log in first"
                    : packs > 0 ? $"{packs} unopened pack{(packs == 1 ? "" : "s")} waiting in your vault"
                    : "Relic Pack — five cards per seal";
            }
            if (decksStrip != null)
                decksStrip.text = online
                    ? $"{PlayerProfile.Decks.Count} deck{(PlayerProfile.Decks.Count == 1 ? "" : "s")} · {PlayerProfile.Collection.Count} unique cards collected"
                    : "Browse the starter decks";

            RefreshActiveDeck();
            RefreshDaily();

            if (onlineText != null)
                onlineText.text = online ? $"{duelists} DUELIST{(duelists == 1 ? "" : "S")} ONLINE" : "OFFLINE MODE";

            // Alte Fallback-Texte leeren, falls noch verdrahtet
            if (infoText != null) infoText.text = "";
            if (hintText != null) hintText.text = "";
        }

        private void RefreshActiveDeck()
        {
            RuntimeDeck deck = null;
            if (PlayerProfile.Decks.Count > 0)
            {
                int index = Mathf.Clamp(PlayerPrefs.GetInt(ActiveDeckPrefKey, 0), 0, PlayerProfile.Decks.Count - 1);
                deck = PlayerProfile.Decks[index];
            }
            if (activeDeckName != null) activeDeckName.text = deck != null ? deck.Name : "No deck";
            if (activeDeckComposition != null)
            {
                if (deck == null || catalog == null) activeDeckComposition.text = "Create a deck in the workshop";
                else
                {
                    int monsters = 0, spells = 0, artifacts = 0;
                    foreach (var cardName in deck.Cards)
                    {
                        var definition = catalog.FindByName(cardName);
                        if (definition is MonsterCardData) monsters++;
                        else if (definition is SpellCardData) spells++;
                        else if (definition is ArtifactCardData) artifacts++;
                    }
                    activeDeckComposition.text = $"{deck.Cards.Count} cards · {monsters} monsters · {spells} spells · {artifacts} artifacts";
                }
            }
        }

        private void RefreshDaily()
        {
            bool online = PlayerProfile.LoggedIn && NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;
            bool claimable = online && PlayerProfile.DailyClaimable;
            int streak = PlayerProfile.DailyStreak;
            int shownDay = claimable ? (streak % 7) + 1 : (streak == 0 ? 0 : ((streak - 1) % 7) + 1);

            for (int i = 0; i < dailySegments.Length; i++)
            {
                if (dailySegments[i] == null) continue;
                bool filled = i < shownDay;
                dailySegments[i].color = filled
                    ? Color.Lerp(new Color32(0x8E, 0x6A, 0x22, 0xFF), new Color32(0xF3, 0xDD, 0xA4, 0xFF), dailySegments.Length <= 1 ? 0f : i / (float)(dailySegments.Length - 1))
                    : SegmentEmpty;
            }

            if (dailyRewardText != null)
                dailyRewardText.text = claimable
                    ? $"DAY {shownDay} · +{PlayerProfile.DailyRewardCoins} COINS"
                    : streak == 0 ? $"+{PlayerProfile.DailyRewardCoins} COINS PER DAY" : $"DAY {shownDay} SEALED";
            if (dailyResetText != null && (claimable || !online))
                dailyResetText.text = !online ? "LOG IN TO CLAIM" : "SEAL READY";
            if (claimButton != null) claimButton.gameObject.SetActive(claimable);
            if (claimedChip != null) claimedChip.SetActive(online && !claimable && streak > 0);
        }

        private bool awaitingDailyClaim;

        private void ClaimDaily()
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.SendClaimDaily();
            awaitingDailyClaim = true;
            if (claimButton != null) claimButton.gameObject.SetActive(false);
        }

        private void Logout()
        {
            PlayerProfile.Clear();
            if (NetworkManager.Instance != null) NetworkManager.Instance.SendLeave();
            SceneManager.LoadScene(loginSceneName);
        }
    }
}
