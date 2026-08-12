using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Klickbare Friedhof-/Banishment-Stapel auf dem Feld. Ein Klick öffnet ein Overlay
    /// mit allen Karten des Stapels; Hovern zeigt die Karte im Detail-Panel.
    /// </summary>
    public class DuelPileViewer : MonoBehaviour
    {
        private enum PileKind { Graveyard, Banished, Extra }

        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private DuelBoardRenderer board;
        [SerializeField] private CardDetailPanel detailPanel;
        [SerializeField] private TcgCardView cardViewPrefab;
        [SerializeField] private DuelUIController uiController;

        [Header("Stapel-Buttons (unten = eigener Spieler)")]
        [SerializeField] private Button bottomGraveButton;
        [SerializeField] private Button bottomBanishButton;
        [SerializeField] private Button topGraveButton;
        [SerializeField] private Button topBanishButton;
        [Tooltip("Leuchtet, wenn eine Friedhofskarte gerade aktivierbar ist")]
        [SerializeField] private Image bottomGraveGlow;
        [Tooltip("Leuchtet, wenn eine Verbannungs-Karte gerade aktivierbar ist")]
        [SerializeField] private Image bottomBanishGlow;
        [SerializeField] private TMP_Text bottomGraveCount;
        [SerializeField] private TMP_Text bottomBanishCount;
        [SerializeField] private TMP_Text topGraveCount;
        [SerializeField] private TMP_Text topBanishCount;

        [Header("Extra-Deck-Stapel (auf dem Feld)")]
        [SerializeField] private Button bottomExtraButton;
        [SerializeField] private TMP_Text bottomExtraCount;
        [SerializeField] private Image bottomExtraGlow;   // leuchtet, wenn eine Reliquary beschworen werden kann
        [SerializeField] private TMP_Text topExtraCount;
        [SerializeField] private GameObject bottomExtraRoot;
        [SerializeField] private GameObject topExtraRoot;

        [Header("Deck-Stapel (auf dem Feld)")]
        [SerializeField] private TMP_Text bottomDeckCount;
        [SerializeField] private TMP_Text topDeckCount;
        [Tooltip("Gestaffelte Kartenrücken des Stapels, Index 0 = unterste Lage — die Dicke folgt der Deckgröße")]
        [SerializeField] private Image[] bottomDeckLayers = new Image[0];
        [SerializeField] private Image[] topDeckLayers = new Image[0];

        [Header("Overlay")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform gridContent;
        [SerializeField] private Button closeButton;

        private readonly List<TcgCardView> spawnedViews = new List<TcgCardView>();
        private bool openBottom;
        private PileKind openKind;
        private bool picking;         // Auswahl-Modus (Deck-/Friedhof-Suchen)
        private bool pickCancelled;
        private int lastBottomGrave = -1, lastBottomBanish = -1, lastTopGrave = -1, lastTopBanish = -1;
        private int lastBottomExtra = -1, lastTopExtra = -1;
        private int lastBottomDeck = -1, lastTopDeck = -1;

        private void Start()
        {
            ApplyDeckSleeves();
            if (bottomGraveButton != null) bottomGraveButton.onClick.AddListener(() => Open(true, PileKind.Graveyard));
            if (bottomBanishButton != null) bottomBanishButton.onClick.AddListener(() => Open(true, PileKind.Banished));
            if (topGraveButton != null) topGraveButton.onClick.AddListener(() => Open(false, PileKind.Graveyard));
            if (topBanishButton != null) topBanishButton.onClick.AddListener(() => Open(false, PileKind.Banished));
            if (bottomExtraButton != null) bottomExtraButton.onClick.AddListener(() => Open(true, PileKind.Extra));
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (board != null) board.AfterRebuild += HandleBoardChanged;
        }

        private void OnDisable()
        {
            if (board != null) board.AfterRebuild -= HandleBoardChanged;
        }

        private void Update()
        {
            RefreshCounts();
            RefreshExtraGlow();
            // Friedhof/Verbannung: gleiche Sprache wie Handkarten — grün heißt
            // "hier liegt gerade eine legale Aktivierung"
            RefreshGlow(bottomGraveGlow, uiController != null && uiController.HasPileActivation(ZoneType.Graveyard));
            RefreshGlow(bottomBanishGlow, uiController != null && uiController.HasPileActivation(ZoneType.Banished));
        }

        private PlayerState BottomPlayer => board != null ? board.BottomPlayer : null;
        private PlayerState TopPlayer => BottomPlayer != null ? BottomPlayer.Opponent : null;

        private void RefreshCounts()
        {
            var bottom = BottomPlayer;
            var top = TopPlayer;
            if (bottom == null || top == null) return;

            SetCount(bottomGraveCount, bottom.Graveyard.Count, ref lastBottomGrave);
            SetCount(bottomBanishCount, bottom.Banished.Count, ref lastBottomBanish);
            SetCount(topGraveCount, top.Graveyard.Count, ref lastTopGrave);
            SetCount(topBanishCount, top.Banished.Count, ref lastTopBanish);

            // Extra-Stapel: Feld + Extra Deck zusammen zählen (Bosse pendeln zwischen
            // beiden). Seit die Piles einen festen Rasterplatz neben der Deck-Zone
            // haben, bleiben sie IMMER sichtbar — ein ausgeblendeter Stapel riss
            // ein Loch ins Raster und sah aus wie ein Fehler.
            int bottomExtra = bottom.ExtraDeckPile.Count;
            int topExtra = top.ExtraDeckPile.Count;
            if (bottomExtraRoot != null && !bottomExtraRoot.activeSelf) bottomExtraRoot.SetActive(true);
            if (topExtraRoot != null && !topExtraRoot.activeSelf) topExtraRoot.SetActive(true);
            SetCount(bottomExtraCount, bottomExtra, ref lastBottomExtra);
            SetCount(topExtraCount, topExtra, ref lastTopExtra);

            // Deck-Zone: Zähler + Stapel-Dicke (leer = gar kein Stapel mehr)
            SetCount(bottomDeckCount, bottom.DeckPile.Count, ref lastBottomDeck);
            SetCount(topDeckCount, top.DeckPile.Count, ref lastTopDeck);
            UpdateDeckStack(bottomDeckLayers, bottom.DeckPile.Count);
            UpdateDeckStack(topDeckLayers, top.DeckPile.Count);
        }

        /// <summary>
        /// Die Deck-Stapel tragen den Kartenrücken des jeweiligen Spielers — dieselbe
        /// Sleeve-Kosmetik wie verdeckte Karten. Unbekannte Gegenstände fallen still
        /// auf den in der Szene gesetzten Standard-Rücken zurück.
        /// </summary>
        private void ApplyDeckSleeves()
        {
            SetLayerSprites(bottomDeckLayers, Rouge.Tcg.Net.CosmeticArt.EquippedCardBack());
            SetLayerSprites(topDeckLayers, Rouge.Tcg.Net.CosmeticArt.CardBack(Rouge.Tcg.Net.MatchContext.RemoteEquipped("sleeve")));
        }

        private static void SetLayerSprites(Image[] layers, Sprite back)
        {
            if (back == null || layers == null) return;
            foreach (var layer in layers)
                if (layer != null) layer.sprite = back;
        }

        /// <summary>Wie viele Rücken-Lagen liegen sichtbar: 3 ab 20 Karten, 2 ab 8, 1 ab 1.</summary>
        private static void UpdateDeckStack(Image[] layers, int count)
        {
            if (layers == null) return;
            int visible = count >= 20 ? 3 : count >= 8 ? 2 : count >= 1 ? 1 : 0;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                bool show = i < visible;
                if (layers[i].gameObject.activeSelf != show) layers[i].gameObject.SetActive(show);
            }
        }

        private static bool AnyReliquaryOnField(PlayerState player)
        {
            foreach (var monster in player.Monsters())
                if (monster.Definition is ReliquaryCardData) return true;
            return false;
        }

        /// <summary>Gold-grüner Puls auf dem eigenen Extra-Stapel, solange eine Reliquary beschworen werden kann.</summary>
        private void RefreshExtraGlow() =>
            RefreshGlow(bottomExtraGlow, uiController != null && uiController.HasReliquarySummon());

        /// <summary>Grüner Puls im Playable-Ton — dieselbe Sprache wie spielbare Handkarten.</summary>
        private static void RefreshGlow(Image glow, bool active)
        {
            if (glow == null) return;
            if (!active)
            {
                if (glow.enabled) glow.enabled = false;
                return;
            }
            glow.enabled = true;
            var baseColor = TcgCardView.PlayableHighlight;
            float pulse = 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * 1.8f, 1f);
            glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * pulse + 0.25f);
        }

        private static void SetCount(TMP_Text text, int count, ref int cache)
        {
            if (text == null || count == cache) return;
            cache = count;
            text.text = count.ToString();
        }

        private void HandleBoardChanged()
        {
            // Offenes Overlay aktuell halten (z.B. wenn währenddessen etwas zerstört wird)
            if (panel != null && panel.activeSelf) RebuildGrid();
        }

        private void Open(bool bottomSide, PileKind kind)
        {
            if (panel == null || picking) return;
            openBottom = bottomSide;
            openKind = kind;
            panel.SetActive(true);
            RebuildGrid();
        }

        private void Close()
        {
            if (picking) { pickCancelled = true; return; } // PickCards räumt selbst auf
            ClearGrid();
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>
        /// Schliesst das STÖBER-Overlay, lässt eine laufende Kartenwahl in Ruhe.
        /// Anfragen rufen das, bevor sie den Spieler brauchen — ein offener
        /// Extra-Deck-Blick beim Rundenstart blockierte sonst die komplette
        /// Prompt-UI, und das Duell stand scheinbar in der Draw Phase fest.
        /// </summary>
        public void CloseIfBrowsing()
        {
            if (picking) return;
            if (panel != null && panel.activeSelf) Close();
        }

        /// <summary>
        /// Karten-Auswahl für unsichtbare Kandidaten (Deck-/Friedhof-Suchen): scrollbares
        /// Karten-Grid im Pile-Overlay, Klick wählt (Gold-Markierung, abwählbar), der
        /// Close-Button bricht ab, wenn der Request das erlaubt.
        /// </summary>
        public IEnumerator PickCards(TargetRequest request)
        {
            if (panel == null || gridContent == null || cardViewPrefab == null)
            {
                for (int i = 0; i < request.Candidates.Count && request.Result.Count < request.Count; i++)
                    request.Result.Add(request.Candidates[i]);
                request.Answered = true;
                yield break;
            }

            picking = true;
            pickCancelled = false;
            panel.SetActive(true);
            ClearGrid();
            if (closeButton != null) closeButton.gameObject.SetActive(request.AllowCancel);

            var picked = new List<CardInstance>();
            var gold = new Color(1f, 0.78f, 0.2f, 0.9f);
            foreach (var card in request.Candidates)
            {
                var view = Instantiate(cardViewPrefab, gridContent);
                view.Show(card, false);
                view.SetHighlight(true, TcgCardView.PlayableHighlight);
                var chosen = card;
                var chosenView = view;
                view.Hovered += _ => { if (detailPanel != null) detailPanel.ShowCard(chosen); };
                view.Clicked += _ =>
                {
                    if (picked.Remove(chosen)) chosenView.SetHighlight(true, TcgCardView.PlayableHighlight);
                    else if (picked.Count < request.Count)
                    {
                        picked.Add(chosen);
                        chosenView.SetHighlight(true, gold);
                    }
                };
                spawnedViews.Add(view);
            }

            while (picked.Count < request.Count && !pickCancelled)
            {
                if (titleText != null)
                    titleText.text = $"{request.Title}  ·  {picked.Count} / {request.Count}";
                yield return null;
            }

            if (pickCancelled && request.AllowCancel) request.Cancelled = true;
            else foreach (var card in picked) request.Result.Add(card);
            request.Answered = true;

            picking = false;
            pickCancelled = false;
            ClearGrid();
            if (closeButton != null) closeButton.gameObject.SetActive(true);
            panel.SetActive(false);
        }

        private void RebuildGrid()
        {
            ClearGrid();
            var player = openBottom ? BottomPlayer : TopPlayer;
            if (player == null || gridContent == null || cardViewPrefab == null) return;

            List<CardInstance> pile = openKind == PileKind.Graveyard ? player.Graveyard
                : openKind == PileKind.Banished ? player.Banished
                : player.ExtraDeckPile;
            string pileName = openKind == PileKind.Graveyard ? "Graveyard"
                : openKind == PileKind.Banished ? "Banished" : "Extra Deck";
            if (titleText != null)
                titleText.text = $"{pileName} — {player.Name} ({pile.Count})";

            foreach (var card in pile)
            {
                var view = Instantiate(cardViewPrefab, gridContent);
                view.Show(card, false);
                var shownCard = card;
                view.Hovered += _ => { if (detailPanel != null) detailPanel.ShowCard(shownCard); };

                // Extra Deck: beschwörbare Reliquarys leuchten und sind direkt klickbar
                bool summonable = openKind == PileKind.Extra && openBottom
                    && uiController != null && uiController.CanReliquarySummon(shownCard);
                // Friedhof/Banishment: aktivierbare Effekte genauso — Klick aktiviert
                bool activatable = (openKind == PileKind.Graveyard || openKind == PileKind.Banished)
                    && openBottom && uiController != null && uiController.CanActivatePileCard(shownCard);
                view.SetHighlight(summonable || activatable, TcgCardView.PlayableHighlight);
                if (summonable)
                    view.Clicked += _ =>
                    {
                        if (uiController.TryChooseReliquarySummon(shownCard)) Close();
                    };
                else if (activatable)
                    view.Clicked += _ =>
                    {
                        if (uiController.TryActivatePileCard(shownCard)) Close();
                    };
                spawnedViews.Add(view);
            }
        }

        private void ClearGrid()
        {
            foreach (var view in spawnedViews)
                if (view != null) Destroy(view.gameObject);
            spawnedViews.Clear();
        }
    }
}
