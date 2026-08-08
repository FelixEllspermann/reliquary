using System.Collections;
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
    /// Shop im Collection-Design: Featured-Banner, vier Pack-Kacheln mit Kauf-/Öffnen-Zuständen
    /// (OPEN glüht, wenn Packs warten) und das 5-Karten-Öffnungsritual mit Klick-Aufdeckung,
    /// Legendary-Glow, REVEAL ALL und ADD TO VAULT.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        [System.Serializable]
        public class PackTile
        {
            public CardPackDefinition pack;
            public TMP_Text ownedText;
            public Button buyButton;
            public TMP_Text buyLabel;
            public Button openButton;
            public Image openBg;
            public Image openGlow;
            public TMP_Text openLabel;
        }

        [Header("Daten")]
        [SerializeField] private CardCatalog catalog;
        [SerializeField] private CardSkin skin;

        [Header("Top-Bar")]
        [SerializeField] private TMP_Text[] dustTexts = new TMP_Text[4];
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button decksTabButton;
        [SerializeField] private Button menuButton;

        [Header("Featured-Banner")]
        [SerializeField] private CardPackDefinition featuredPack;
        [SerializeField] private Button featuredBuyButton;
        [SerializeField] private TMP_Text featuredBuyLabel;
        [SerializeField] private GameObject featuredStatusPill;
        [SerializeField] private TMP_Text featuredStatusText;

        [Header("Pack-Kacheln")]
        [SerializeField] private PackTile[] tiles = new PackTile[4];
        [SerializeField] private TMP_Text footerText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Kosmetik-Kachel")]
        [Tooltip("Öffnet den Kosmetik-Laden (Overlay)")]
        [SerializeField] private Button cosmeticsButton;
        [Tooltip("Anzahl der besessenen Kosmetik-Gegenstände auf der Kachel")]
        [SerializeField] private TMP_Text cosmeticsOwnedText;

        [Header("Öffnungs-Sequenz")]
        [Tooltip("Kartenansicht für die fünf gezogenen Karten")]
        [SerializeField] private TcgCardView cardViewPrefab;

        [Header("Szenen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string decksSceneName = "DeckEditor";

        [Header("Pack-Info")]
        [Tooltip("Wie weit über BUY/OPEN die Knöpfe CARDS und ODDS sitzen")]
        [SerializeField] private float infoButtonOffsetY = 56f;

        private NetworkManager network;
        private string openingPackName;

        private Button infoButtonTemplate;
        private readonly List<(CardPackDefinition pack, Button button, TMP_Text label)> openAllButtons
            = new List<(CardPackDefinition, Button, TMP_Text)>();
        private readonly List<(CardPackDefinition pack, Button button, TMP_Text label)> buyTenButtons
            = new List<(CardPackDefinition, Button, TMP_Text)>();
        private GameObject infoOverlay;
        private TMP_Text infoTitle;
        private TMP_Text infoBody;
        private RectTransform infoGrid;   // Kartengalerie der Contents-Ansicht
        private ScrollRect infoScroll;

        private void Start()
        {
            network = NetworkManager.Instance;
            BuildInfoButtons();   // vor dem Verdrahten: die Kopien sollen keine Listener erben
            if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (decksTabButton != null) decksTabButton.onClick.AddListener(() => SceneManager.LoadScene(decksSceneName));
            if (featuredBuyButton != null) featuredBuyButton.onClick.AddListener(() => BuyPack(featuredPack));
            if (cosmeticsButton != null) cosmeticsButton.onClick.AddListener(() => CosmeticsPanel.Open());
            foreach (var tile in tiles)
            {
                var captured = tile;
                if (tile.buyButton != null) tile.buyButton.onClick.AddListener(() => BuyPack(captured.pack));
                if (tile.openButton != null) tile.openButton.onClick.AddListener(() => OpenPack(captured.pack));
            }
            if (network != null) network.OnMessage += HandleMessage;
            if (!PlayerProfile.LoggedIn) ShowFeedback("Not logged in — the shop requires an account.");
            Rebuild();
        }

        private void OnDestroy()
        {
            if (network != null) network.OnMessage -= HandleMessage;
        }

        private void HandleMessage(NetMessage message)
        {
            switch (message.t)
            {
                case "profile":
                    Rebuild();
                    break;
                case "pack_result":
                    ShowReveal(message.packCards, message.packFinishes);
                    Rebuild();
                    break;
                case "error":
                    ShowFeedback(message.msg);
                    break;
            }
        }

        private void Update()
        {
            // Atmender Glow auf öffenbaren Packs — die einzige animierte Affordanz des Screens
            float pulse = 0.35f + 0.3f * (Mathf.Sin(Time.unscaledTime * 2.4f) * 0.5f + 0.5f);
            foreach (var tile in tiles)
            {
                if (tile.openGlow != null && tile.openGlow.gameObject.activeSelf)
                {
                    var color = tile.openGlow.color;
                    tile.openGlow.color = new Color(color.r, color.g, color.b, pulse);
                }
            }
        }

        private void Rebuild()
        {
            bool online = PlayerProfile.LoggedIn && network != null && network.IsConnected;
            var dust = new[] { PlayerProfile.TokensCommon, PlayerProfile.TokensUncommon, PlayerProfile.TokensRare, PlayerProfile.TokensLegendary };
            for (int i = 0; i < dustTexts.Length && i < 4; i++)
                if (dustTexts[i] != null) dustTexts[i].text = dust[i].ToString();
            if (coinsText != null) coinsText.text = MainMenuController.FormatCoins(PlayerProfile.Coins);

            int totalPacks = 0;
            foreach (var count in PlayerProfile.PackInventory.Values) totalPacks += count;
            if (featuredBuyLabel != null && featuredPack != null)
                featuredBuyLabel.text = $"BUY PACK · {featuredPack.price}";
            if (featuredBuyButton != null)
                featuredBuyButton.interactable = online && featuredPack != null && PlayerProfile.Coins >= featuredPack.price;
            if (featuredStatusPill != null) featuredStatusPill.SetActive(totalPacks > 0);
            if (featuredStatusText != null)
                featuredStatusText.text = $"{totalPacks} PACK{(totalPacks == 1 ? "" : "S")} WAITING TO BE UNSEALED";

            foreach (var tile in tiles)
            {
                if (tile.pack == null) continue;
                int owned = PlayerProfile.PacksOf(tile.pack.packName);
                if (tile.ownedText != null) tile.ownedText.text = $"×{owned}";
                if (tile.buyLabel != null) tile.buyLabel.text = $"BUY · {tile.pack.price}";
                if (tile.buyButton != null) tile.buyButton.interactable = online && PlayerProfile.Coins >= tile.pack.price;

                bool openable = online && owned > 0;
                if (tile.openButton != null) tile.openButton.interactable = openable;
                if (tile.openLabel != null)
                {
                    tile.openLabel.text = openable ? "OPEN" : "NONE OWNED";
                    ColorUtility.TryParseHtmlString(openable ? "#1E1405" : "#4A4235", out var ink);
                    tile.openLabel.color = ink;
                }
                if (tile.openBg != null)
                {
                    tile.openBg.sprite = openable && skin != null ? skin.badgeMonster : null;
                    tile.openBg.color = openable ? Color.white : new Color(0f, 0f, 0f, 0.3f);
                }
                if (tile.openGlow != null) tile.openGlow.gameObject.SetActive(openable);
            }

            // Zehnerkauf: immer sichtbar, aktiv sobald die Coins reichen
            foreach (var (pack, button, label) in buyTenButtons)
            {
                if (button == null) continue;
                button.interactable = online && PlayerProfile.Coins >= pack.price * 10;
                if (label != null) label.text = $"BUY ×10 · {pack.price * 10}";
            }

            // Massen-Öffnung erst zeigen, wenn sie etwas bündelt (ab 2 Packs)
            foreach (var (pack, button, label) in openAllButtons)
            {
                if (button == null) continue;
                int owned = PlayerProfile.PacksOf(pack.packName);
                bool show = owned >= 2;
                if (button.gameObject.activeSelf != show) button.gameObject.SetActive(show);
                if (!show) continue;
                button.interactable = online;
                if (label != null) label.text = $"OPEN ×{Mathf.Min(10, owned)}";
            }

            // Die Kachel zeigt denselben Zähler wie die Packs daneben: was man schon hat
            if (cosmeticsOwnedText != null)
                cosmeticsOwnedText.text = $"×{Cosmetics.Owned.Count}";
            if (cosmeticsButton != null) cosmeticsButton.interactable = online;

            if (footerText != null)
                footerText.text = online
                    ? $"Signed in as {PlayerProfile.AccountName} — every card you pull is added to your vault."
                    : "Log in to buy and open packs.";
        }

        private void BuyPack(CardPackDefinition pack)
        {
            if (network == null || pack == null) return;
            network.SendBuyPack(pack.packName);
            ShowFeedback($"Buying {pack.packName} for {pack.price} coins…");
        }

        private void OpenPack(CardPackDefinition pack)
        {
            if (network == null || pack == null) return;
            openingPackName = pack.packName;
            network.SendOpenPack(pack.packName);
            ShowFeedback($"Unsealing {pack.packName}…");
        }

        /// <summary>Kauft 10 Packs auf einmal — ein Server-Roundtrip, ein Profil-Update.</summary>
        private void BuyTenPacks(CardPackDefinition pack)
        {
            if (network == null || pack == null) return;
            network.SendBuyPack(pack.packName, 10);
            ShowFeedback($"Buying 10× {pack.packName} for {pack.price * 10} coins…");
        }

        /// <summary>Öffnet bis zu 10 Packs auf einmal — der Server zieht, das Grid zeigt alles.</summary>
        private void OpenAllPacks(CardPackDefinition pack)
        {
            if (network == null || pack == null) return;
            int batch = Mathf.Min(10, PlayerProfile.PacksOf(pack.packName));
            if (batch < 1) return;
            openingPackName = pack.packName;
            network.SendOpenPack(pack.packName, batch);
            ShowFeedback($"Unsealing {batch}× {pack.packName}…");
        }

        // ================== ÖFFNUNGS-SEQUENZ ==================

        /// <summary>
        /// Übergibt an die Öffnungs-Sequenz (Handoff „Animations", Abschnitt 2).
        /// Alles steht schon fest — der Server hat gezogen, hier wird nur gezeigt.
        /// </summary>
        private void ShowReveal(string[] cardNames, int[] cardFinishes)
        {
            if (cardViewPrefab == null || catalog == null || cardNames == null) return;
            PackOpenSequence.Play(cardViewPrefab, catalog,
                openingPackName, cardNames, cardFinishes, Rebuild);
        }

        // ================== PACK-INFO: INHALT & CHANCEN ==================

        /// <summary>
        /// Setzt über BUY/OPEN eine zweite Knopfreihe: CARDS zeigt alle Karten des Packs,
        /// ODDS die Ziehchancen. Beide sind Kopien des Kaufen-Knopfes, damit sie ohne
        /// weiteres Verdrahten im Stil der Kachel sitzen.
        /// </summary>
        private void BuildInfoButtons()
        {
            if (tiles == null) return;
            foreach (var tile in tiles)
            {
                if (tile == null || tile.pack == null || tile.buyButton == null) continue;
                infoButtonTemplate = tile.buyButton;
                var pack = tile.pack;

                var cards = CloneTileButton(tile.buyButton, (RectTransform)tile.buyButton.transform, "CardsButton", "CARDS", infoButtonOffsetY);
                if (cards != null) cards.onClick.AddListener(() => ShowPackInfo(pack, false));

                var placement = tile.openButton != null ? (RectTransform)tile.openButton.transform : null;
                if (placement != null)
                {
                    var odds = CloneTileButton(tile.buyButton, placement, "OddsButton", "ODDS", infoButtonOffsetY);
                    if (odds != null) odds.onClick.AddListener(() => ShowPackInfo(pack, true));
                }

                // Mengen-Reihe über CARDS/ODDS, spiegelt die Spalten darunter:
                // links BUY ×10, rechts OPEN ×N (letzterer nur sichtbar ab 2 Packs)
                var buyTen = CloneTileButton(tile.buyButton, (RectTransform)tile.buyButton.transform, "BuyTenButton", "BUY ×10", infoButtonOffsetY * 2f);
                if (buyTen != null)
                {
                    buyTen.onClick.AddListener(() => BuyTenPacks(pack));
                    buyTenButtons.Add((pack, buyTen, buyTen.GetComponentInChildren<TMP_Text>(true)));
                }
                if (placement != null)
                {
                    var openAll = CloneTileButton(tile.buyButton, placement, "OpenAllButton", "OPEN ×10", infoButtonOffsetY * 2f);
                    if (openAll != null)
                    {
                        openAll.onClick.AddListener(() => OpenAllPacks(pack));
                        openAllButtons.Add((pack, openAll, openAll.GetComponentInChildren<TMP_Text>(true)));
                        openAll.gameObject.SetActive(false);   // Rebuild entscheidet
                    }
                }

                // Die Kachelzeile nennt eine feste Kartenzahl — die stammt aus der Anfangszeit
                // und stimmt längst nicht mehr. Aus den echten Daten setzen.
                var kicker = tile.buyButton.transform.parent.Find("Kicker");
                var kickerText = kicker != null ? kicker.GetComponent<TMP_Text>() : null;
                if (kickerText != null)
                    kickerText.text = $"ALL {pack.ResolvePool(catalog).Count} CARDS · {Mathf.Max(1, pack.raritySlots.Count)} PER SEAL";
            }
        }

        private Button CloneTileButton(Button template, RectTransform placement, string objectName, string label, float offsetY)
        {
            var copy = Instantiate(template.gameObject, template.transform.parent);
            copy.name = objectName;
            StripClonedExtras(copy);

            var rect = (RectTransform)copy.transform;
            rect.anchorMin = placement.anchorMin;
            rect.anchorMax = placement.anchorMax;
            rect.pivot = placement.pivot;
            rect.sizeDelta = placement.sizeDelta;
            rect.anchoredPosition = placement.anchoredPosition + new Vector2(0f, offsetY);

            var button = copy.GetComponent<Button>();
            if (button == null) return null;
            button.onClick.RemoveAllListeners();
            button.interactable = true;

            var text = copy.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                ColorUtility.TryParseHtmlString("#D9C79B", out var ink);
                text.color = ink;
            }
            return button;
        }

        /// <summary>
        /// Entfernt aus einer Knopf-Kopie den pulsierenden OPEN-Schein und einen bereits
        /// angehängten Hover-Schein — der wird für die Kopie neu erzeugt.
        /// </summary>
        private static void StripClonedExtras(GameObject copy)
        {
            foreach (var name in new[] { "Glow", "~FxGlow" })
            {
                var child = copy.transform.Find(name);
                if (child != null) Destroy(child.gameObject);
            }
            var inherited = copy.GetComponent<UiButtonFx>();
            if (inherited != null) Destroy(inherited);
        }

        private void ShowPackInfo(CardPackDefinition pack, bool odds)
        {
            if (pack == null) return;
            EnsureInfoOverlay();
            if (infoOverlay == null) return;

            infoOverlay.SetActive(true);
            var pool = pack.ResolvePool(catalog);
            if (infoTitle != null)
                infoTitle.text = odds
                    ? $"{pack.packName.ToUpperInvariant()} · ODDS"
                    : $"{pack.packName.ToUpperInvariant()} · CONTENTS · {pool.Count(c => c != null)} CARDS";

            // Odds sind Text, der Inhalt ist eine Galerie echter Karten — der
            // Scroller bekommt jeweils das passende Content-Rect untergeschoben.
            bool gallery = !odds;
            if (infoBody != null)
            {
                infoBody.gameObject.SetActive(!gallery);
                if (odds) infoBody.text = BuildOddsText(pack, pool);
            }
            if (infoGrid != null)
            {
                infoGrid.gameObject.SetActive(gallery);
                for (int i = infoGrid.childCount - 1; i >= 0; i--)
                    Destroy(infoGrid.GetChild(i).gameObject);
                if (gallery) PopulateContentsGallery(pack);
            }
            if (infoScroll != null)
            {
                infoScroll.content = gallery && infoGrid != null
                    ? infoGrid
                    : (RectTransform)infoBody.transform;
                infoScroll.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// Die Karten des Packs als anfassbare Galerie — jede als echte Karte
        /// gerendert (unter 200 px Breite schaltet TcgCardView von selbst auf
        /// die Kompakt-Ansicht). Sortiert von Legendary abwärts, damit das
        /// Teuerste oben steht.
        /// </summary>
        private void PopulateContentsGallery(CardPackDefinition pack)
        {
            if (cardViewPrefab == null || infoGrid == null) return;
            var sorted = pack.ResolvePool(catalog)
                .Where(c => c != null)
                .OrderByDescending(c => (int)c.rarity)
                .ThenBy(c => c.cardName, System.StringComparer.Ordinal);
            foreach (var definition in sorted)
            {
                var view = Instantiate(cardViewPrefab, infoGrid);
                view.Show(new CardInstance(definition, null), false, true);
            }
        }

        private void HideInfo()
        {
            if (infoOverlay != null) infoOverlay.SetActive(false);
        }

        /// <summary>Alle Karten des Packs, nach Seltenheit gruppiert.</summary>
        private static string BuildContentsText(CardPackDefinition pack)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append("<color=#8C7B5F>").Append(pack.cardPool.Count).Append(" cards can appear in this pack.</color>\n");

            var order = new[] { CardRarity.Legendary, CardRarity.Rare, CardRarity.Uncommon, CardRarity.Common };
            foreach (var rarity in order)
            {
                var names = pack.cardPool
                    .Where(c => c != null && c.rarity == rarity)
                    .Select(c => c.cardName)
                    .OrderBy(n => n)
                    .ToList();
                if (names.Count == 0) continue;

                string hex = ColorUtility.ToHtmlStringRGB(CollectionRow.RarityStrong(rarity));
                builder.Append("\n<size=110%><color=#").Append(hex).Append("><b>")
                       .Append(CardDefinition.RarityName(rarity).ToUpperInvariant())
                       .Append("</b></color>  <color=#8C7B5F>").Append(names.Count).Append("</color></size>\n")
                       .Append("<color=#CFC3AC>").Append(string.Join("  ·  ", names)).Append("</color>\n");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Chancen-Tabelle. Ein Pack hat feste Slots; innerhalb eines Slots wird
        /// gleichverteilt aus allen Karten dieser Seltenheit gezogen. Der letzte Slot
        /// kann zusätzlich zur Legendary aufgewertet werden — das verschiebt seinen
        /// Anteil von seiner normalen Rarity zu Legendary.
        /// </summary>
        /// <summary>
        /// Finish-Chancen, wie sie der Server würfelt. MUSS zu RATES in
        /// Server/finishes.js passen — sonst zeigt die Odds-Seite Märchen an.
        /// </summary>
        private static string FinishOddsText()
        {
            var builder = new System.Text.StringBuilder();
            builder.Append("\n\n<color=#8C7B5F>Every card also rolls a finish on top — pack or cache, same odds:</color>\n");
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Net.CardFinishInfo.Accent(Net.CardFinish.Glossy)))
                   .Append(">GLOSSY</color><pos=32%><color=#CFC3AC>1 in 12</color><pos=74%><color=#CFC3AC>8.33 %</color>\n");
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Net.CardFinishInfo.Accent(Net.CardFinish.Rainbow)))
                   .Append(">RAINBOW</color><pos=32%><color=#CFC3AC>1 in 60</color><pos=74%><color=#CFC3AC>1.67 %</color>\n");
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Net.CardFinishInfo.Accent(Net.CardFinish.Static)))
                   .Append(">STATIC</color><pos=32%><color=#CFC3AC>1 in 240</color><pos=74%><color=#CFC3AC>0.42 %</color>");
            return builder.ToString();
        }

        private static string BuildOddsText(CardPackDefinition pack, System.Collections.Generic.List<CardDefinition> pool)
        {
            // Unique-Packs (Hero Cache) haben keine Rarity-Slots: sie ziehen genau
            // EINE Karte aus dem Pool, die dem Konto fehlt — jede fehlende gleich
            // wahrscheinlich. Die normale Tabelle würde hier nur verwirren.
            if (pack.uniqueDraw)
            {
                int total = pool.Count(c => c != null);
                int missing = pool.Count(c => c != null && Net.PlayerProfile.Owned(c.cardName) < 1);
                var b = new System.Text.StringBuilder();
                b.Append("<color=#8C7B5F>Every cache contains <color=#F1E7D2>1</color> card — always one you ")
                 .Append("<color=#F1E7D2>do not own yet</color>. No duplicates, no blanks.</color>\n\n");
                if (missing > 0)
                    b.Append("<color=#8C7B5F>You are missing <color=#F1E7D2>").Append(missing)
                     .Append("</color> of ").Append(total)
                     .Append(" — each of them is equally likely: <color=#FFC24D>")
                     .Append((100f / missing).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                     .Append(" %</color> per card.</color>");
                else
                    b.Append("<color=#E9A183>You already own every card in this pack — there is nothing left to pull.</color>");
                b.Append(FinishOddsText());
                return b.ToString();
            }

            int slotCount = Mathf.Max(1, pack.raritySlots.Count);
            var expected = new float[4];
            foreach (var rarity in pack.raritySlots) expected[(int)rarity] += 1f;

            var lastSlot = pack.raritySlots.Count > 0 ? pack.raritySlots[pack.raritySlots.Count - 1] : CardRarity.Common;
            float upgrade = Mathf.Clamp01(pack.legendaryUpgradeChance);
            bool upgrades = upgrade > 0.0001f
                && lastSlot != CardRarity.Legendary
                && pool.Any(c => c != null && c.rarity == CardRarity.Legendary);
            if (upgrades)
            {
                expected[(int)lastSlot] -= upgrade;
                expected[(int)CardRarity.Legendary] += upgrade;
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("<color=#8C7B5F>Every pack contains <color=#F1E7D2>").Append(slotCount)
                   .Append("</color> cards. Each slot has a fixed rarity; within that slot every card of ")
                   .Append("that rarity is equally likely.</color>\n\n");

            builder.Append("<color=#8C7B5F>RARITY<pos=32%>PER PACK<pos=52%>IN POOL<pos=74%>PER CARD</color>\n");

            var order = new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare, CardRarity.Legendary };
            foreach (var rarity in order)
            {
                float perPack = expected[(int)rarity];
                int inPool = pool.Count(c => c != null && c.rarity == rarity);
                float perCard = inPool > 0 ? 100f * perPack / inPool : 0f;
                string hex = ColorUtility.ToHtmlStringRGB(CollectionRow.RarityStrong(rarity));
                string ink = perPack > 0.0001f ? "CFC3AC" : "6A6152";

                builder.Append("<color=#").Append(hex).Append('>')
                       .Append(CardDefinition.RarityName(rarity).ToUpperInvariant()).Append("</color>")
                       .Append("<pos=32%><color=#").Append(ink).Append('>').Append(Amount(perPack)).Append("</color>")
                       .Append("<pos=52%><color=#CFC3AC>").Append(inPool).Append("</color>")
                       .Append("<pos=74%><color=#").Append(ink).Append('>')
                       .Append(perCard.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                       .Append(" %</color>\n");
            }

            if (upgrades)
                builder.Append("\n<color=#8C7B5F>In <color=#FFC24D>")
                       .Append(Percent(upgrade)).Append("</color> of packs the last ")
                       .Append(CardDefinition.RarityName(lastSlot)).Append(" is replaced by a Legendary.</color>");
            else
                builder.Append("\n<color=#E9A183>Legendary cards cannot be pulled from this pack — they come from crafting only.</color>");
            builder.Append(FinishOddsText());
            return builder.ToString();
        }

        /// <summary>Ganze Zahlen ohne Nachkommastellen, Bruchteile mit zweien.</summary>
        private static string Amount(float value)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return Mathf.Abs(value - Mathf.Round(value)) < 0.001f
                ? Mathf.RoundToInt(value).ToString(culture)
                : value.ToString("0.00", culture);
        }

        private static string Percent(float value)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            float percent = value * 100f;
            return (Mathf.Abs(percent - Mathf.Round(percent)) < 0.05f
                ? Mathf.RoundToInt(percent).ToString(culture)
                : percent.ToString("0.0", culture)) + " %";
        }

        // ---------- Overlay-Aufbau (einmalig, zur Laufzeit) ----------

        private void EnsureInfoOverlay()
        {
            if (infoOverlay != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            infoOverlay = MakeRect("PackInfoOverlay", (RectTransform)canvas.transform).gameObject;
            var overlayRect = (RectTransform)infoOverlay.transform;
            Stretch(overlayRect, 0f);
            overlayRect.SetAsLastSibling();

            var scrim = MakeImage("Scrim", overlayRect, new Color(0f, 0f, 0f, 0.88f));
            Stretch((RectTransform)scrim.transform, 0f);
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(HideInfo);
            scrim.gameObject.AddComponent<UiFxIgnore>();

            var panel = MakeImage("Panel", overlayRect, HexColor("#100D09", 0.99f));
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1000f, 780f);
            panelRect.anchoredPosition = Vector2.zero;

            var border = MakeImage("Frame", panelRect, HexColor("#C8A45C", 0.55f));
            Stretch((RectTransform)border.transform, 0f);
            if (skin != null && skin.whiteFrame != null)
            {
                border.sprite = skin.whiteFrame;
                border.type = Image.Type.Sliced;
            }
            border.raycastTarget = false;

            infoTitle = MakeText("Title", panelRect, 26f, skin != null ? skin.cinzelSemiBold : null);
            var titleRect = (RectTransform)infoTitle.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(32f, 0f);
            titleRect.offsetMax = new Vector2(-32f, -26f);
            titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, 40f);
            infoTitle.alignment = TextAlignmentOptions.Center;
            infoTitle.color = HexColor("#F1E7D2", 1f);
            infoTitle.characterSpacing = 8f;

            var scrollGo = MakeRect("Scroll", panelRect);
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(28f, 88f);
            scrollRect.offsetMax = new Vector2(-28f, -80f);
            infoScroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            infoScroll.horizontal = false;
            infoScroll.vertical = true;
            infoScroll.movementType = ScrollRect.MovementType.Clamped;
            infoScroll.scrollSensitivity = 34f;

            var viewport = MakeRect("Viewport", scrollRect);
            Stretch(viewport, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            infoScroll.viewport = viewport;

            infoBody = MakeText("Body", viewport, 17f, skin != null ? skin.spectral : null);
            var bodyRect = (RectTransform)infoBody.transform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = new Vector2(0f, 0f);
            bodyRect.offsetMax = new Vector2(0f, 0f);
            infoBody.alignment = TextAlignmentOptions.TopLeft;
            infoBody.color = HexColor("#CFC3AC", 1f);
            infoBody.lineSpacing = 6f;
            var fitter = infoBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoScroll.content = bodyRect;

            // Galerie-Container für die Contents-Ansicht: ein Gitter aus echten
            // Karten. Unter 200 px Zellbreite rendert TcgCardView von selbst
            // kompakt — genau richtig für eine Übersicht.
            infoGrid = MakeRect("CardGrid", viewport);
            infoGrid.anchorMin = new Vector2(0f, 1f);
            infoGrid.anchorMax = new Vector2(1f, 1f);
            infoGrid.pivot = new Vector2(0.5f, 1f);
            infoGrid.offsetMin = Vector2.zero;
            infoGrid.offsetMax = Vector2.zero;
            var grid = infoGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(172f, 240f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;
            var gridFitter = infoGrid.gameObject.AddComponent<ContentSizeFitter>();
            gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoGrid.gameObject.SetActive(false);

            if (infoButtonTemplate != null)
            {
                var close = Instantiate(infoButtonTemplate.gameObject, panelRect);
                close.name = "CloseButton";
                StripClonedExtras(close);
                var closeRect = (RectTransform)close.transform;
                closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
                closeRect.pivot = new Vector2(0.5f, 0f);
                closeRect.sizeDelta = new Vector2(220f, 46f);
                closeRect.anchoredPosition = new Vector2(0f, 22f);
                var closeButton = close.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveAllListeners();
                    closeButton.interactable = true;
                    closeButton.onClick.AddListener(HideInfo);
                }
                var closeLabel = close.GetComponentInChildren<TMP_Text>(true);
                if (closeLabel != null)
                {
                    closeLabel.text = "CLOSE";
                    closeLabel.color = HexColor("#D9C79B", 1f);
                }
            }

            infoOverlay.SetActive(false);
        }

        private static Color HexColor(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            return color;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image MakeImage(string name, RectTransform parent, Color color)
        {
            var image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, float size, TMP_FontAsset font)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.raycastTarget = false;
            text.richText = true;
            return text;
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message;
        }
    }
}
