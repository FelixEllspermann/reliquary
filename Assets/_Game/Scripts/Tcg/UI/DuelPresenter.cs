using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Master-Duel-artige Präsentation: Draw-Flug, Showcases, Phasen-Banner sowie die
    /// physischen Karten-Animationen — nichtlinearer Angriff (Aufladen → Sprint →
    /// Einschlag → Rückstoß), Treffer-Feedback, Zersplittern bei Zerstörung und
    /// sichtbare Kartenflüge zwischen den Zonen (Hand→Feld, Feld→Friedhof/Verbannung).
    /// Die Engine wartet auf diese Coroutinen — Effekte lösen erst nach der Anzeige auf.
    /// </summary>
    public class DuelPresenter : MonoBehaviour, IDuelPresenter
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private DuelBoardRenderer board;
        [SerializeField] private TcgCardView cardViewPrefab;
        [SerializeField] private CanvasGroup showcaseGroup;
        [SerializeField] private RectTransform showcaseCardHolder;
        [SerializeField] private TMP_Text showcaseBanner;
        [SerializeField] private RectTransform flyLayer;
        [SerializeField] private Transform p1DeckAnchor;
        [SerializeField] private Transform p2DeckAnchor;
        [SerializeField] private Transform p1HandAnchor;
        [SerializeField] private Transform p2HandAnchor;
        [Tooltip("Helden-Slots: Ursprung der LP-Zahlen und Ziel von Direktangriffen")]
        [SerializeField] private Transform p1LifeAnchor;
        [SerializeField] private Transform p2LifeAnchor;
        [Tooltip("Friedhofs-Zähler in der Rail — Ziel der Friedhofs-Flüge")]
        [SerializeField] private Transform p1GraveAnchor;
        [SerializeField] private Transform p2GraveAnchor;
        [Tooltip("Verbannungs-Zähler in der Rail — Ziel der Banish-Flüge")]
        [SerializeField] private Transform p1BanishAnchor;
        [SerializeField] private Transform p2BanishAnchor;
        [Tooltip("Schrift der fliegenden Schadens-/Heilzahlen (Cinzel)")]
        [SerializeField] private TMP_FontAsset numberFont;

        [Header("Timing (alles hier balancen)")]
        [Tooltip("Präsentationen komplett an/aus (aus = Duell läuft ohne Verzögerungen)")]
        [SerializeField] private bool enablePresentations = true;

        [Header("Aktivierung & Zielwahl (Handoff „Animations\", Abschnitt 3)")]
        [Tooltip("Karte hebt aus der Zone in die Bildmitte. Handoff: 1.4 s")]
        [Range(0.2f, 2f)] [SerializeField] private float liftDuration = 0.85f;
        [Tooltip("Aufschlag zurück in die Zone. Handoff: 1.6 s")]
        [Range(0.2f, 2f)] [SerializeField] private float activateDuration = 1.0f;
        [Tooltip("Faden und Fadenkreuz bis zum Einrasten. Handoff: 2.1 s")]
        [Range(0.3f, 3f)] [SerializeField] private float targetDuration = 1.25f;
        [Tooltip("Aufdecken einer verdeckten Karte, bevor sie aktiviert")]
        [Range(0.1f, 0.8f)] [SerializeField] private float revealDuration = 0.28f;

        [Header("Zerstörung (Handoff „Animations\", Abschnitt 4)")]
        [Tooltip("Einschlag und Risslinien. Handoff: 1.4 s")]
        [Range(0.1f, 2f)] [SerializeField] private float struckDuration = 0.5f;
        [Tooltip("Die Keile bersten. Handoff: 1.4 s")]
        [Range(0.1f, 2f)] [SerializeField] private float burstDuration = 0.6f;
        [Tooltip("Die Keile fliegen zurück und werden zum Kartenrücken. Handoff: 1.3 s")]
        [Range(0.1f, 2f)] [SerializeField] private float gatherDuration = 0.5f;
        [Tooltip("Bogen zum Friedhof. Handoff: 1.6 s + 2.2 s Aufsetzen")]
        [Range(0.2f, 2f)] [SerializeField] private float flightDuration = 0.6f;

        [Range(0.05f, 2f)] [Tooltip("Flugdauer der gezogenen Karte vom Deck zur Hand")]
        [SerializeField] private float drawFlyDuration = 0.35f;

        [Range(0f, 2f)] [Tooltip("Wie lange die eigene gezogene Karte groß gezeigt wird")]
        [SerializeField] private float drawHoldDuration = 0.45f;

        [Range(0.1f, 3f)] [Tooltip("Anzeigedauer des Aktivierungs-Showcase, bevor der Effekt auflöst")]
        [SerializeField] private float activationHoldDuration = 1.15f;

        [Range(0f, 2f)] [Tooltip("Anzeigedauer des Beschwörungs-Showcase")]
        [SerializeField] private float summonHoldDuration = 0.75f;

        [Range(0f, 2f)] [Tooltip("Dauer der Angriffs-Hervorhebung")]
        [SerializeField] private float attackFlashDuration = 0.55f;

        [Range(0.05f, 1f)] [Tooltip("Ein-/Ausblendzeit des Showcase")]
        [SerializeField] private float fadeDuration = 0.15f;

        [Range(1f, 4f)] [Tooltip("Vergrößerung der Showcase-Karte")]
        [SerializeField] private float showcaseScale = 2.4f;

        [Header("Angriffs-Animation")]
        [Range(0.05f, 1.5f)] [Tooltip("Dauer des beschleunigten Vorwärts-Sprints zum Ziel")]
        [SerializeField] private float attackLungeDuration = 0.14f;

        [Range(0f, 1f)] [Tooltip("Wo auf der Strecke der Einschlag passiert (0–1)")]
        [SerializeField] private float attackLungeDistance = 0.88f;

        [Range(0.05f, 0.6f)] [Tooltip("Dauer des Aufladens (hoch- und zurückziehen)")]
        [SerializeField] private float anticipationDuration = 0.18f;

        [Range(10f, 120f)] [Tooltip("Wie weit der Angreifer beim Aufladen zurückzieht")]
        [SerializeField] private float anticipationDistance = 46f;

        [Range(0f, 0.3f)] [Tooltip("Kurzes Einfrieren beim Einschlag (Hit-Stop)")]
        [SerializeField] private float impactHold = 0.05f;

        [Range(10f, 120f)] [Tooltip("Wie weit der Angreifer nach dem Einschlag zurückgestoßen wird")]
        [SerializeField] private float recoilDistance = 44f;

        [Range(0.05f, 0.6f)] [Tooltip("Dauer des langsamen Zurückgestoßen-Werdens")]
        [SerializeField] private float recoilDuration = 0.16f;

        [Range(0.05f, 0.8f)] [Tooltip("Dauer der beschleunigten Rückkehr zum Platz")]
        [SerializeField] private float returnDuration = 0.2f;

        [Header("Zonen-Flüge")]
        [Range(0.1f, 1f)] [Tooltip("Flugdauer einer Karte zwischen Zonen (Hand→Feld etc.)")]
        [SerializeField] private float moveFlyDuration = 0.28f;

        [Range(0.1f, 1f)] [Tooltip("Flugdauer zum Friedhof/zur Verbannung")]
        [SerializeField] private float pileFlyDuration = 0.3f;


        [Header("Phasen-Banner")]
        [Range(0.2f, 3f)]
        [SerializeField]
        [Tooltip("Gesamtdauer der Phasen-Einblendung (Engine wartet solange)")]
        private float phaseBannerDuration = 1f;
        [SerializeField] private CanvasGroup phaseBannerGroup;
        [SerializeField] private TMP_Text phaseBannerText;

        [Header("Schadenszahlen")]
        [Range(0.3f, 3f)] [Tooltip("Lebensdauer einer fliegenden Zahl")]
        [SerializeField] private float numberDuration = 1.1f;

        [Range(10f, 300f)] [Tooltip("Wie weit die Zahl nach oben schwebt")]
        [SerializeField] private float numberRise = 90f;

        [Range(20f, 90f)] [Tooltip("Schriftgröße der Zahlen")]
        [SerializeField] private float numberFontSize = 46f;

        private TcgCardView showcaseView;

        // ================== EASING ==================

        private static float EaseIn(float k) => k * k * k;
        private static float EaseOut(float k) => 1f - Mathf.Pow(1f - k, 3f);
        private static float EaseInOut(float k) => k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;

        private void OnEnable()
        {
            if (board != null && board.Duel != null) board.Duel.OnLifeChanged += HandleLifeChanged;

            // Der Duell-Canvas läuft im Overlay-Modus und sieht die Kamera nicht.
            // Ein Kamera-Stoß wäre dort unsichtbar — also bekommt der Screenshake
            // die Brett-Wurzel als Ziel.
            if (flyLayer != null && flyLayer.parent != null)
                ScreenShake.SetUiTarget(flyLayer.parent);
        }

        private void OnDisable()
        {
            if (board != null && board.Duel != null) board.Duel.OnLifeChanged -= HandleLifeChanged;
        }

        private void HandleLifeChanged(PlayerState player, int delta)
        {
            if (!enablePresentations || delta == 0 || flyLayer == null) return;
            StartCoroutine(FloatNumber(player, delta));
        }

        // ================== ANKER-HELFER ==================

        /// <summary>Alle Showcases/Flüge an- oder abschalten (Loopback-Test läuft ohne).</summary>
        public bool EnablePresentations
        {
            get => enablePresentations;
            set => enablePresentations = value;
        }

        /// <summary>Aktuelle Bildschirmposition der View einer Karte (falls sichtbar).</summary>
        public bool TryCaptureViewPosition(CardInstance card, out Vector3 position)
        {
            position = Vector3.zero;
            if (board == null || card == null) return false;
            if (!board.TryGetView(card, out var view) || view == null) return false;
            position = view.transform.position;
            return true;
        }

        /// <summary>Anker eines Karten-Stapels (Friedhof/Verbannung/Deck) des Spielers.</summary>
        public Transform PileAnchor(PlayerState player, ZoneType zone)
        {
            bool isBottom = board != null && player == board.BottomPlayer;
            switch (zone)
            {
                case ZoneType.Graveyard: return isBottom ? p1GraveAnchor : p2GraveAnchor;
                case ZoneType.Banished: return isBottom ? p1BanishAnchor : p2BanishAnchor;
                case ZoneType.Deck: return isBottom ? p1DeckAnchor : p2DeckAnchor;
                case ZoneType.Hand: return isBottom ? p1HandAnchor : p2HandAnchor;
                default: return null;
            }
        }

        /// <summary>Startpunkt für einen Flug: Karten-View, sonst der Stapel-Anker ihrer Zone.</summary>
        public bool TryCaptureCardOrigin(CardInstance card, out Vector3 position)
        {
            if (TryCaptureViewPosition(card, out position)) return true;
            var anchor = card != null ? PileAnchor(card.Owner, card.Zone) : null;
            if (anchor == null) return false;
            position = anchor.position;
            return true;
        }

        // Von der Engine gemerkte Ausgangspositionen (IDuelPresenter): sie merkt vor
        // der Datenänderung, ShowCardMoved(card) fliegt danach von dort los. Die
        // Engine selbst kennt dadurch keine Bildschirmkoordinaten mehr.
        private readonly Dictionary<CardInstance, Vector3> rememberedOrigins = new Dictionary<CardInstance, Vector3>();

        public void RememberView(CardInstance card)
        {
            if (card != null && TryCaptureViewPosition(card, out var position))
                rememberedOrigins[card] = position;
        }

        public void RememberOrigin(CardInstance card)
        {
            if (card != null && TryCaptureCardOrigin(card, out var position))
                rememberedOrigins[card] = position;
        }

        /// <summary>Flug von der gemerkten Position; ohne Merkeintrag ein No-op.</summary>
        public IEnumerator ShowCardMoved(CardInstance card)
        {
            if (card == null || !rememberedOrigins.TryGetValue(card, out var fromPosition)) yield break;
            rememberedOrigins.Remove(card);
            yield return ShowCardMoved(card, fromPosition);
        }

        // ================== PHASEN-BANNER ==================

        public IEnumerator ShowPhaseBanner(string text, float holdOverride = -1f)
        {
            if (!enablePresentations || phaseBannerGroup == null || phaseBannerText == null) yield break;
            phaseBannerText.text = text;
            phaseBannerGroup.gameObject.SetActive(true);
            float total = holdOverride > 0f ? holdOverride : phaseBannerDuration;
            float fade = Mathf.Min(0.15f, total * 0.2f);
            float hold = Mathf.Max(0f, total - fade * 2f);

            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                phaseBannerGroup.alpha = Mathf.Clamp01(elapsed / fade);
                yield return null;
            }
            phaseBannerGroup.alpha = 1f;
            yield return new WaitForSeconds(hold);
            elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                phaseBannerGroup.alpha = 1f - Mathf.Clamp01(elapsed / fade);
                yield return null;
            }
            phaseBannerGroup.alpha = 0f;
            phaseBannerGroup.gameObject.SetActive(false);
        }

        private IEnumerator FloatNumber(PlayerState player, int delta)
        {
            bool isBottom = player == board.BottomPlayer;
            var anchor = isBottom
                ? (p1LifeAnchor != null ? p1LifeAnchor : p1DeckAnchor)
                : (p2LifeAnchor != null ? p2LifeAnchor : p2DeckAnchor);
            if (anchor == null) yield break;

            var go = new GameObject("FloatNumber", typeof(RectTransform));
            go.transform.SetParent(flyLayer, false);
            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            if (numberFont != null) text.font = numberFont;
            text.fontSize = numberFontSize;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = delta > 0 ? $"+{delta}" : delta.ToString();
            ColorUtility.TryParseHtmlString(delta > 0 ? "#7DDB6E" : "#E0603A", out var numberColor);
            text.color = numberColor;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(260, 70);
            float rise = isBottom ? numberRise : -numberRise;
            var spawn = anchor.position + new Vector3(UnityEngine.Random.Range(-20f, 20f), isBottom ? -30f : -90f, 0f);
            spawn.x = Mathf.Clamp(spawn.x, 170f, Screen.width - 170f);
            spawn.y = Mathf.Clamp(spawn.y, 110f + Mathf.Max(0f, -rise), Screen.height - 110f - Mathf.Max(0f, rise));
            rect.position = spawn;

            Vector3 start = rect.position;
            float elapsed = 0f;
            while (elapsed < numberDuration && go != null)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / numberDuration);
                rect.position = start + Vector3.up * (rise * k);
                var color = text.color;
                color.a = k < 0.5f ? 1f : 1f - (k - 0.5f) * 2f;
                text.color = color;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        // ================== ANGRIFF ==================

        /// <summary>
        /// Nichtlinearer Angriff: Aufladen (hoch + zurück, ausbremsend) → kurze Spannung →
        /// beschleunigter Sprint zum Ziel → abrupter Einschlag mit Hit-Stop und Treffer-
        /// Feedback → langsamer Rückstoß → beschleunigte Rückkehr zum Platz.
        /// </summary>
        public IEnumerator ShowAttackImpact(CardInstance attacker, CardInstance target, bool direct)
        {
            if (!enablePresentations || board == null) yield break;
            if (!board.TryGetView(attacker, out var attackerView)) yield break;

            Vector3 home = attackerView.transform.position;
            Vector3 to;
            TcgCardView targetView = null;
            if (direct || target == null)
            {
                bool attackerIsBottom = attacker.Owner == board.BottomPlayer;
                var anchor = attackerIsBottom
                    ? (p2LifeAnchor != null ? p2LifeAnchor : p2DeckAnchor)
                    : (p1LifeAnchor != null ? p1LifeAnchor : p1DeckAnchor);
                if (anchor == null) yield break;
                to = anchor.position;
            }
            else
            {
                if (!board.TryGetView(target, out targetView)) yield break;
                to = targetView.transform.position;
            }

            Vector3 dir = (to - home).normalized;
            bool attackerBottom = attacker.Owner == board.BottomPlayer;
            Vector3 up = attackerBottom ? Vector3.up : Vector3.down;
            Vector3 windupPos = home - dir * anticipationDistance + up * (anticipationDistance * 0.45f);
            Vector3 impactPos = Vector3.Lerp(home, to, attackLungeDistance);
            Vector3 baseScale = attackerView.transform.localScale;

            // Während der Animation über ALLEM rendern (sonst verdecken Hero-Slots
            // & Co. den Angreifer) — danach exakt in den Slot zurückhängen.
            var viewRect = (RectTransform)attackerView.transform;
            var homeParent = viewRect.parent;
            int homeSibling = viewRect.GetSiblingIndex();
            Vector2 homeAnchorMin = viewRect.anchorMin;
            Vector2 homeAnchorMax = viewRect.anchorMax;
            Vector2 homePivot = viewRect.pivot;
            Vector2 homeAnchoredPos = viewRect.anchoredPosition;
            Quaternion homeRotation = viewRect.localRotation;
            if (flyLayer != null) viewRect.SetParent(flyLayer, true);

            try
            {
                // 1) Aufladen: nach oben ziehen und leicht zurück (ausbremsend), leicht aufbäumen
                float elapsed = 0f;
                while (elapsed < anticipationDuration)
                {
                    if (attackerView == null) yield break;
                    elapsed += Time.deltaTime;
                    float k = EaseOut(Mathf.Clamp01(elapsed / anticipationDuration));
                    attackerView.transform.position = Vector3.Lerp(home, windupPos, k);
                    attackerView.transform.localScale = baseScale * (1f + 0.07f * k);
                    yield return null;
                }
                yield return new WaitForSeconds(0.05f); // kurze Spannung auf dem Höhepunkt

                // 2) Sprint: stark beschleunigend nach vorn
                elapsed = 0f;
                while (elapsed < attackLungeDuration)
                {
                    if (attackerView == null) yield break;
                    elapsed += Time.deltaTime;
                    float k = EaseIn(Mathf.Clamp01(elapsed / attackLungeDuration));
                    attackerView.transform.position = Vector3.Lerp(windupPos, impactPos, k);
                    attackerView.transform.localScale = baseScale * (1.07f - 0.07f * k);
                    yield return null;
                }
                SfxManager.Hit();   // genau im Moment des Aufpralls
                // Ein Direktangriff geht auf die Lebenspunkte — der darf mehr weh tun
                if (direct) ScreenShake.HeavyImpact(); else ScreenShake.Impact();
                if (attackerView != null) attackerView.transform.position = impactPos;

                // 3) Einschlag: Hit-Stop + Feedback des Getroffenen + Board-Wackler
                if (targetView != null) StartCoroutine(HitFeedback(targetView, dir));
                StartCoroutine(BoardShake());
                if (impactHold > 0f) yield return new WaitForSeconds(impactHold);

                // 4) Langsamer Rückstoß
                Vector3 recoilPos = impactPos - dir * recoilDistance;
                elapsed = 0f;
                while (elapsed < recoilDuration)
                {
                    if (attackerView == null) yield break;
                    elapsed += Time.deltaTime;
                    float k = EaseOut(Mathf.Clamp01(elapsed / recoilDuration));
                    attackerView.transform.position = Vector3.Lerp(impactPos, recoilPos, k);
                    yield return null;
                }

                // 5) Beschleunigte Rückkehr zum Platz
                elapsed = 0f;
                while (elapsed < returnDuration)
                {
                    if (attackerView == null) yield break;
                    elapsed += Time.deltaTime;
                    float k = EaseInOut(Mathf.Clamp01(elapsed / returnDuration));
                    attackerView.transform.position = Vector3.Lerp(recoilPos, home, k);
                    yield return null;
                }
            }
            finally
            {
                if (attackerView != null && homeParent != null)
                {
                    viewRect.SetParent(homeParent, false);
                    viewRect.SetSiblingIndex(homeSibling);
                    viewRect.anchorMin = homeAnchorMin;
                    viewRect.anchorMax = homeAnchorMax;
                    viewRect.pivot = homePivot;
                    viewRect.anchoredPosition = homeAnchoredPos;
                    viewRect.localRotation = homeRotation;
                    viewRect.localScale = baseScale;
                }
            }
        }

        /// <summary>Feedback der getroffenen Karte: weißer Blitz, Wegstoßen, Zittern.</summary>
        private IEnumerator HitFeedback(TcgCardView view, Vector3 hitDirection)
        {
            if (view == null) yield break;
            Vector3 home = view.transform.position;
            Vector3 baseScale = view.transform.localScale;
            view.SetHighlight(true, Color.white);

            // Wegstoßen (schnell raus, ausbremsend)
            Vector3 pushed = home + hitDirection * 26f;
            float elapsed = 0f;
            const float pushDur = 0.08f;
            while (elapsed < pushDur)
            {
                if (view == null) yield break;
                elapsed += Time.deltaTime;
                float k = EaseOut(Mathf.Clamp01(elapsed / pushDur));
                view.transform.position = Vector3.Lerp(home, pushed, k);
                view.transform.localScale = baseScale * (1f + 0.12f * k);
                yield return null;
            }

            // Zittern beim Zurückgleiten
            elapsed = 0f;
            const float shakeDur = 0.22f;
            while (elapsed < shakeDur)
            {
                if (view == null) yield break;
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / shakeDur);
                float fadeOff = 1f - k;
                Vector3 shake = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 55f, 0.3f) - 0.5f) * 12f * fadeOff,
                    (Mathf.PerlinNoise(0.7f, Time.time * 55f) - 0.5f) * 12f * fadeOff, 0f);
                view.transform.position = Vector3.Lerp(pushed, home, EaseInOut(k)) + shake;
                view.transform.localScale = baseScale * (1f + 0.12f * fadeOff);
                yield return null;
            }
            if (view != null)
            {
                view.transform.position = home;
                view.transform.localScale = baseScale;
                view.SetHighlight(false);
            }
        }

        /// <summary>Kleiner Wackler des ganzen Boards beim Einschlag.</summary>
        private IEnumerator BoardShake()
        {
            if (board == null) yield break;
            var t = board.transform;
            Vector3 home = t.localPosition;
            float elapsed = 0f;
            const float duration = 0.14f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fadeOff = 1f - Mathf.Clamp01(elapsed / duration);
                t.localPosition = home + (Vector3)(UnityEngine.Random.insideUnitCircle * 5f * fadeOff);
                yield return null;
            }
            t.localPosition = home;
        }

        // ================== ZONEN-FLÜGE ==================

        /// <summary>
        /// Karte fliegt sichtbar von einer alten Position zu ihrer neuen View
        /// (nach dem Board-Rebuild aufrufen — z.B. Hand → Feld beim Ausspielen).
        /// </summary>
        public IEnumerator ShowCardMoved(CardInstance card, Vector3 fromPosition)
        {
            if (!enablePresentations) yield break;
            SfxManager.CardPlace();
            if (board == null || cardViewPrefab == null || flyLayer == null) yield break;
            if (!board.TryGetView(card, out var realView) || realView == null) yield break;

            Vector3 to = realView.transform.position;
            Vector3 targetScale = realView.transform.localScale;
            realView.gameObject.SetActive(false);

            bool hideFace = card.FaceDown;
            bool sideways = card.Zone == ZoneType.MonsterZone && card.Position == BattlePosition.Defense;
            var fly = Instantiate(cardViewPrefab, flyLayer);
            fly.Show(card, hideFace, upright: !sideways);
            var flyRect = (RectTransform)fly.transform;
            flyRect.position = fromPosition;
            flyRect.localScale = targetScale;

            // leichter Bogen zur Seite für natürliche Bewegung
            Vector3 mid = Vector3.Lerp(fromPosition, to, 0.5f) + Vector3.up * 26f;
            float elapsed = 0f;
            while (elapsed < moveFlyDuration)
            {
                if (fly == null) break;
                elapsed += Time.deltaTime;
                float k = EaseInOut(Mathf.Clamp01(elapsed / moveFlyDuration));
                Vector3 a = Vector3.Lerp(fromPosition, mid, k);
                Vector3 b = Vector3.Lerp(mid, to, k);
                flyRect.position = Vector3.Lerp(a, b, k);
                yield return null;
            }
            if (fly != null) Destroy(fly.gameObject);
            if (realView != null) realView.gameObject.SetActive(true);
        }

        /// <summary>Karte fliegt schrumpfend auf einen Stapel (Friedhof/Verbannung).</summary>
        private IEnumerator FlyToPile(CardInstance card, Vector3 fromPosition, ZoneType pile, float startScale, float startAlpha)
        {
            var anchor = PileAnchor(card.Owner, pile);
            if (anchor == null || cardViewPrefab == null || flyLayer == null) yield break;
            SfxManager.CardMoving();

            var fly = Instantiate(cardViewPrefab, flyLayer);
            fly.Show(card, false, upright: true);
            var group = fly.gameObject.AddComponent<CanvasGroup>();
            group.alpha = startAlpha;
            group.blocksRaycasts = false;
            var flyRect = (RectTransform)fly.transform;
            flyRect.position = fromPosition;
            flyRect.localScale = Vector3.one * startScale;

            Vector3 to = anchor.position;
            float elapsed = 0f;
            while (elapsed < pileFlyDuration)
            {
                if (fly == null) yield break;
                elapsed += Time.deltaTime;
                float k = EaseInOut(Mathf.Clamp01(elapsed / pileFlyDuration));
                flyRect.position = Vector3.Lerp(fromPosition, to, k);
                flyRect.localScale = Vector3.one * Mathf.Lerp(startScale, 0.16f, k);
                group.alpha = startAlpha * (1f - 0.55f * k);
                if (pile == ZoneType.Banished) fly.SetHighlight(true, new Color(1f, 1f, 1f, 1f - k));
                yield return null;
            }
            if (fly != null) Destroy(fly.gameObject);
        }

        /// <summary>
        /// Zerstörung nach Handoff „Animations", Abschnitt 4. Fünf Beats:
        ///   Struck   — Einschlag, dann zeichnen sich sechs Risslinien
        ///   Shatter  — die Keile bersten, Farbe läuft nach Asche aus
        ///   Gather   — sie fliegen ZURÜCK und werden zu einem Kartenrücken
        ///   Flight   — ein einziger Bogen zum Friedhof, mit Spur
        ///   Land     — Aufsetzen auf dem Stapel
        ///
        /// Die Umkehrung in Gather ist der Kern: eine zerstörte Karte wird zu
        /// einer anonymen Karte. Genau das hält der Friedhof.
        /// </summary>
        /// <summary>
        /// Die Spielerkarte des Verlierers zerspringt an Ort und Stelle — Teil
        /// der End-Sequenz, auf BEIDEN Clients. Anders als ShowCardDestroyed
        /// gibt es keinen Flug zum Friedhof: der Held fällt, er zieht nicht um.
        /// Die Keile bersten und verglimmen, die Ansicht bleibt aus.
        /// </summary>
        public IEnumerator ShowPlayerCardShatter(PlayerState loser)
        {
            var card = loser?.PlayerCard;
            if (card == null || board == null || flyLayer == null) yield break;
            if (!board.TryGetView(card, out var view) || view == null) yield break;

            SfxManager.Destroyed();
            ScreenShake.Shake(0.03f, 1f, 16f);

            var rect = (RectTransform)view.transform;
            var fromPos = rect.position;
            var size = rect.rect.size;
            var shatter = CardShatter.Build(flyLayer, view, card, size);
            shatter.Rect.position = fromPos;
            view.gameObject.SetActive(false);

            try
            {
                // Einschlag und Risse
                for (float t = 0f; t < 0.3f; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / 0.3f);
                    shatter.Apply(0f, 0f, 1f, Motion.Seg(p, 0.4f, 1f) * 0.3f, 1f);
                    yield return null;
                }
                // Bersten — weiter auseinander als eine normale Zerstörung
                for (float t = 0f; t < 0.55f; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / 0.55f);
                    float out01 = Motion.Enter(p);
                    shatter.Apply(out01 * 220f, out01, 1f, 1f, 1f);
                    yield return null;
                }
                // Verglimmen an Ort und Stelle
                for (float t = 0f; t < 0.5f; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / 0.5f);
                    shatter.Apply(Motion.Mix(220f, 260f, p), 1f, 1f, 1f, 1f - Motion.Enter(p));
                    yield return null;
                }
            }
            finally
            {
                if (shatter != null) Destroy(shatter.gameObject);
            }
        }

        public IEnumerator ShowCardDestroyed(CardInstance card)
        {
            if (!enablePresentations) yield break;
            SfxManager.Destroyed();
            if (board == null || flyLayer == null) yield break;
            if (!TryCaptureCardOrigin(card, out var fromPos)) yield break;

            board.TryGetView(card, out var view);
            if (view == null) { yield return FlyToPile(card, fromPos, ZoneType.Graveyard, 0.5f, 0.55f); yield break; }

            var size = ((RectTransform)view.transform).rect.size;
            var shatter = CardShatter.Build(flyLayer, view, card, size);
            shatter.Rect.position = fromPos;

            // Das Original verschwindet, sobald die Keile stehen — sonst sieht man
            // beides übereinander
            view.gameObject.SetActive(false);

            try
            {
                // ---- Struck: Einschlag und Risslinien ----
                ScreenShake.Shake(0.020f, 0.8f, 18f);
                for (float t = 0f; t < struckDuration; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / struckDuration);
                    shatter.Apply(0f, 0f, 1f, Motion.Seg(p, 0.55f, 1f) * 0.25f, 1f);
                    yield return null;
                }

                // ---- Shatter: die Keile bersten, die Farbe läuft aus ----
                for (float t = 0f; t < burstDuration; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / burstDuration);
                    float out01 = Motion.Enter(p);
                    shatter.Apply(out01 * 150f, out01, 1f, Motion.Mix(0.25f, 1f, p), 1f);
                    yield return null;
                }

                // ---- Gather: zurück nach innen, schrumpfend ----
                for (float t = 0f; t < gatherDuration; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / gatherDuration);
                    float back = 1f - Motion.Enter(p);
                    shatter.Apply(Motion.Mix(118f, 0f, Motion.Enter(p)), back,
                        Motion.Mix(1f, 0.72f, p), 1f, 1f - Motion.Seg(p, 0.6f, 1f));
                    yield return null;
                }
            }
            finally
            {
                if (shatter != null) Destroy(shatter.gameObject);
            }

            // ---- Flight und Land: ein Bogen zum Friedhof, verdeckt ----
            yield return FlyToPile(card, fromPos, ZoneType.Graveyard, 0.72f, flightDuration);
        }

        public IEnumerator ShowCardBanished(CardInstance card)
        {
            if (!enablePresentations || board == null) yield break;
            if (!TryCaptureCardOrigin(card, out var fromPos)) yield break;
            if (board.TryGetView(card, out var view) && view != null) view.gameObject.SetActive(false);
            yield return FlyToPile(card, fromPos, ZoneType.Banished, 0.85f, 1f);
        }

        /// <summary>Karte wandert sichtbar in den Friedhof (Abwurf, Tribut, verbrauchte Zauber).</summary>
        public IEnumerator ShowCardSentToGrave(CardInstance card)
        {
            if (!enablePresentations || board == null) yield break;
            if (!TryCaptureCardOrigin(card, out var fromPos)) yield break;
            if (board.TryGetView(card, out var view) && view != null) view.gameObject.SetActive(false);
            yield return FlyToPile(card, fromPos, ZoneType.Graveyard, 0.85f, 1f);
        }

        /// <summary>Verbrauchter Zauber fliegt vom Showcase-Zentrum in den Friedhof.</summary>
        public IEnumerator ShowSpellToGrave(CardInstance spell)
        {
            if (!enablePresentations || flyLayer == null) yield break;
            Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            yield return FlyToPile(spell, center, ZoneType.Graveyard, 0.7f, 0.8f);
        }

        /// <summary>Kleiner Dreh-Impuls nach einem Positionswechsel auf dem Feld.</summary>
        public IEnumerator ShowPositionSwitch(CardInstance card)
        {
            if (!enablePresentations || board == null) yield break;
            if (!board.TryGetView(card, out var view) || view == null) yield break;

            Quaternion targetRot = view.transform.localRotation;
            Vector3 baseScale = view.transform.localScale;
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                if (view == null) yield break;
                elapsed += Time.deltaTime;
                float k = EaseOut(Mathf.Clamp01(elapsed / duration));
                float wobble = Mathf.Sin(k * Mathf.PI) * 10f;
                view.transform.localRotation = targetRot * Quaternion.Euler(0f, 0f, wobble);
                view.transform.localScale = baseScale * (1f + 0.08f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (view != null)
            {
                view.transform.localRotation = targetRot;
                view.transform.localScale = baseScale;
            }
        }

        // ================== BESTEHENDE SHOWS ==================

        private void Awake()
        {
            if (showcaseGroup != null)
            {
                showcaseGroup.alpha = 0f;
                showcaseGroup.gameObject.SetActive(false);
            }
        }

        public IEnumerator ShowCardDrawn(PlayerState player, CardInstance card, float speed = 1f)
        {
            if (!enablePresentations) yield break;
            SfxManager.CardDraw();
            if (cardViewPrefab == null || flyLayer == null || board == null) yield break;

            bool isBottom = player == board.BottomPlayer;
            var from = isBottom ? p1DeckAnchor : p2DeckAnchor;
            var fallback = isBottom ? p1HandAnchor : p2HandAnchor;
            if (from == null) yield break;

            // Ziel = die echte Position der Karte in der Hand (View existiert nach dem Rebuild schon)
            TcgCardView realView = null;
            Vector3 end = fallback != null ? fallback.position : from.position;
            if (board.TryGetView(card, out realView))
            {
                end = realView.transform.position;
                realView.gameObject.SetActive(false);
            }

            var fly = Instantiate(cardViewPrefab, flyLayer);
            fly.Show(card, !isBottom, upright: true);
            var flyRect = (RectTransform)fly.transform;
            flyRect.position = from.position;
            float startScale = isBottom ? 1.05f : 0.75f;
            float endScale = isBottom ? 1f : 0.55f;
            flyRect.localScale = Vector3.one * startScale;

            Vector3 start = from.position;
            Vector3 lift = Vector3.up * (isBottom ? 60f : -40f); // leichter Bogen statt gerader Linie
            float elapsed = 0f;
            while (elapsed < drawFlyDuration)
            {
                elapsed += Time.deltaTime * speed;
                float k = EaseInOut(Mathf.Clamp01(elapsed / drawFlyDuration));
                flyRect.position = Vector3.Lerp(start, end, k) + lift * Mathf.Sin(k * Mathf.PI);
                flyRect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
                yield return null;
            }
            flyRect.position = end;

            // Eigene Karten am Zielplatz kurz vergrößert zeigen, damit man sie lesen kann
            if (isBottom && drawHoldDuration > 0f)
            {
                flyRect.localScale = Vector3.one * 1.3f;
                yield return new WaitForSeconds(drawHoldDuration / speed);
            }

            Destroy(fly.gameObject);
            if (realView != null) realView.gameObject.SetActive(true);
            else if (board.TryGetView(card, out var handView)) handView.gameObject.SetActive(true);
        }

        [Range(0f, 2f)] [Tooltip("Wie lange eine gemillte Karte aufgedeckt auf dem Deck liegen bleibt")]
        [SerializeField] private float millRevealHold = 1.2f;

        /// <summary>
        /// Eine gemillte Karte: erscheint aufgedeckt auf der Deck-Zone, bleibt dort
        /// kurz liegen und fliegt dann in einem flachen Bogen in den Friedhof.
        /// </summary>
        public IEnumerator ShowMilled(PlayerState player, CardInstance card)
        {
            if (!enablePresentations || cardViewPrefab == null || flyLayer == null || board == null) yield break;

            bool isBottom = player == board.BottomPlayer;
            var deckAnchor = isBottom ? p1DeckAnchor : p2DeckAnchor;
            var graveAnchor = isBottom ? p1GraveAnchor : p2GraveAnchor;
            if (deckAnchor == null) yield break;

            SfxManager.CardPlace();
            var fly = Instantiate(cardViewPrefab, flyLayer);
            fly.Show(card, false, upright: true);   // aufgedeckt
            fly.SetHighlight(false);
            var rect = (RectTransform)fly.transform;
            rect.position = deckAnchor.position;
            float scale = isBottom ? 0.95f : 0.85f;
            rect.localScale = Vector3.one * scale;

            try
            {
                if (millRevealHold > 0f) yield return new WaitForSeconds(millRevealHold);

                Vector3 start = rect.position;
                Vector3 end = graveAnchor != null ? graveAnchor.position : start;
                Vector3 lift = Vector3.up * (isBottom ? 46f : -34f);
                const float duration = 0.28f;
                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    float k = EaseInOut(Mathf.Clamp01(t / duration));
                    rect.position = Vector3.Lerp(start, end, k) + lift * Mathf.Sin(k * Mathf.PI);
                    rect.localScale = Vector3.one * Mathf.Lerp(scale, scale * 0.72f, k);
                    yield return null;
                }
            }
            finally { Destroy(fly.gameObject); }
        }

        /// <summary>Sichtbarer Münzwurf: goldene Reliquary-Raute flippt in der Bildmitte und kommt taumelnd zur Ruhe.</summary>
        /// <summary>
        /// Der Münzwurf (Handoff „Coin Flip“). Vier Schläge in 3,4 s:
        /// Aufstieg — Trudeln — Aufschlag mit Staubring — Ergebnis.
        ///
        /// <para>
        /// <b>Warum das ein Overlay ist und keine Szene:</b> im Server-Duell würfelt
        /// der Server mitten im Duell und schickt ein Ereignis. Dazwischen lässt sich
        /// keine Szene schieben, ein Overlay schon. Damit sehen Solo und Online
        /// denselben Wurf — es gibt nur noch diese eine Fassung.
        /// </para>
        ///
        /// <paramref name="winner"/> bestimmt, welche Seite oben liegen bleibt:
        /// RELIC für den lokalen Spieler, SEAL für den Gegner. Die Münze ist die
        /// ausgerüstete Wurfmünze, sonst die Standardmünze.
        /// </summary>
        public IEnumerator ShowCoinToss(PlayerState winner)
        {
            if (!enablePresentations || flyLayer == null) yield break;

            bool localWins = winner == null || winner.IsLocal;
            string foeName = winner != null && !winner.IsLocal ? winner.Name : "Your opponent";
            var skin = TransitionSkin.Load();
            var relic = Net.CosmeticArt.MatchCoinRelic() ?? Net.CosmeticArt.CoinRelic("vanilla");
            var seal = Net.CosmeticArt.MatchCoinSeal() ?? Net.CosmeticArt.CoinSeal("vanilla");
            var shadowSprite = Resources.Load<Sprite>("Cosmetics/coin_shadow");
            var ringSprite = Resources.Load<Sprite>("Cosmetics/coin_dustring");

            var gold = new Color(0.784f, 0.643f, 0.361f);
            var goldLight = new Color(0.922f, 0.808f, 0.541f);
            var pale = new Color(0.973f, 0.933f, 0.839f);
            var foeTint = new Color(0.561f, 0.776f, 0.824f);
            var muted = new Color(0.635f, 0.541f, 0.412f);
            var headTint = localWins ? pale : foeTint;

            var root = new GameObject("CoinToss", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(flyLayer, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;

            RectTransform MakeRect(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                return rect;
            }
            Image Add(string name, Transform parent, Sprite sprite, Vector2 size)
            {
                var rect = MakeRect(name, parent);
                rect.sizeDelta = size;
                var img = rect.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                return img;
            }
            void Stretch(RectTransform rect)
            {
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            }
            TMP_Text AddText(string name, Transform parent, TMP_FontAsset font, float size, float spacing)
            {
                var rect = MakeRect(name, parent);
                var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) text.font = font;
                text.fontSize = size;
                text.alignment = TextAlignmentOptions.Center;
                text.characterSpacing = spacing;
                text.raycastTarget = false;
                text.enableWordWrapping = false;
                rect.sizeDelta = new Vector2(1400f, size * 1.8f);
                return text;
            }

            // Das Brett tritt zurück: Abdunklung und Randvignette
            var scrim = Add("Scrim", root, skin != null ? skin.square : null, Vector2.zero);
            Stretch(scrim.rectTransform);
            var vignette = Add("Vignette", root, skin != null ? skin.vignette : null, Vector2.zero);
            Stretch(vignette.rectTransform);

            // Glut steigt auf — feste Offsets, damit der Wurf jedes Mal gleich aussieht
            var emberX = new[] { -520f, -320f, -85f, 155f, 350f, 545f };
            var emberDelay = new[] { 0f, 0.35f, 0.62f, 0.18f, 0.8f, 0.48f };
            var embers = new Image[emberX.Length];
            for (int i = 0; i < embers.Length; i++)
            {
                embers[i] = Add("Ember" + i, root, skin != null ? skin.square : null, new Vector2(7f, 7f));
                embers[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            var stage = MakeRect("Stage", root);   // trägt die Kamerafahrt
            var shadow = Add("Shadow", stage, shadowSprite, new Vector2(180f, 54f));
            var ring = Add("DustRing", stage, ringSprite, new Vector2(120f, 29f));
            var squash = MakeRect("Squash", stage);
            squash.sizeDelta = new Vector2(168f, 168f);
            var face = Add("Face", squash, relic, Vector2.zero);
            Stretch(face.rectTransform);

            var caption = AddText("Caption", root, skin != null ? skin.oswald : null, 15f, 34f);
            caption.text = "THE COIN DECIDES WHO CHOOSES";
            caption.rectTransform.anchoredPosition = new Vector2(0f, -230f);

            var verdict = MakeRect("Verdict", root);
            var chip = AddText("Chip", verdict, skin != null ? skin.oswald : null, 13f, 42f);
            chip.text = "COIN TOSS";
            chip.rectTransform.anchoredPosition = new Vector2(0f, 96f);
            var headline = AddText("Headline", verdict, skin != null ? skin.cinzel : null, 52f, 6f);
            headline.fontStyle = FontStyles.Bold;
            headline.text = localWins ? "YOU WIN THE TOSS" : foeName.ToUpperInvariant() + " WINS THE TOSS";
            headline.rectTransform.anchoredPosition = new Vector2(0f, 36f);
            var sub = AddText("Sub", verdict, skin != null ? skin.spectral : null, 18f, 0f);
            sub.text = localWins ? "You choose who opens the duel."
                                 : foeName + " chooses who opens the duel.";
            sub.rectTransform.anchoredPosition = new Vector2(0f, -8f);

            SfxManager.CoinToss();
            bool landed = false;

            try
            {
                const float duration = 3.4f;
                // Eine halbe Drehung ist Pi: gerades Vielfaches = RELIC oben,
                // UNGERADES = SEAL oben. 8.5 war der Kanten-Bug — cos(8.5π) = 0,
                // die Münze blieb hochkant als Strich stehen.
                float finalSpin = localWins ? 8f : 9f;

                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / duration);
                    float flight = Motion.Seg(p, 0f, 0.52f);
                    float land = Motion.Seg(p, 0.52f, 0.62f);
                    float show = Motion.Enter(Motion.Seg(p, 0.62f, 0.78f));
                    float settle = Motion.Drift(Mathf.Clamp01(p / 0.62f));

                    if (!landed && p >= 0.52f) { landed = true; SfxManager.CoinHit(); }

                    // Kamerafahrt: heranfahren, beim Aufschlag zurück, danach Platz für den Text
                    float zoom = Motion.Mix(1.12f, 1f, settle);
                    stage.localScale = new Vector3(zoom, zoom, 1f);
                    stage.anchoredPosition = new Vector2(0f, Motion.Mix(-34f, 0f, settle) - show * 60f);

                    float height = Mathf.Sin(flight * Mathf.PI) * 230f;
                    float cos = Mathf.Cos(Mathf.Lerp(0f, finalSpin, Motion.Enter(flight)) * Mathf.PI);
                    squash.anchoredPosition = new Vector2(0f, height + 30f);
                    float squashY = Mathf.Max(0.03f, Mathf.Abs(cos)) * (1f - Mathf.Sin(land * Mathf.PI) * 0.35f);
                    squash.localScale = new Vector3(1f, squashY, 1f);
                    face.sprite = cos >= 0f ? relic : seal;

                    float shadowK = 1f - Mathf.Sin(flight * Mathf.PI) * 0.55f;
                    shadow.rectTransform.anchoredPosition = new Vector2(0f, -104f);
                    shadow.rectTransform.sizeDelta = new Vector2(180f * shadowK, 54f * shadowK);
                    shadow.color = new Color(0f, 0f, 0f, 0.5f * shadowK);

                    float ringK = Motion.Enter(Motion.Seg(p, 0.52f, 0.86f));
                    float ringW = Mathf.Lerp(120f, 420f, ringK);
                    ring.rectTransform.anchoredPosition = new Vector2(0f, -100f);
                    ring.rectTransform.sizeDelta = new Vector2(ringW, ringW * 0.24f);
                    ring.color = new Color(gold.r, gold.g, gold.b,
                        ringK > 0f ? (1f - Motion.Seg(p, 0.52f, 0.9f)) * 0.5f : 0f);

                    float fade = Motion.Enter(Motion.Seg(p, 0f, 0.12f)) * (1f - Motion.Seg(p, 0.92f, 1f));
                    scrim.color = new Color(0.02f, 0.03f, 0.05f, 0.72f * fade);
                    vignette.color = new Color(0f, 0f, 0f, 0.9f * fade);

                    for (int i = 0; i < embers.Length; i++)
                    {
                        float e = (p * 1.4f + emberDelay[i]) % 1f;
                        embers[i].rectTransform.anchoredPosition = new Vector2(emberX[i], -340f + e * 500f);
                        embers[i].color = new Color(goldLight.r, goldLight.g, goldLight.b,
                            (e < 0.18f ? e / 0.18f : 1f - (e - 0.18f) / 0.82f) * 0.5f * fade);
                    }

                    float captionK = Motion.Enter(Motion.Seg(p, 0.08f, 0.2f)) * (1f - Motion.Seg(p, 0.46f, 0.56f));
                    caption.color = new Color(gold.r, gold.g, gold.b, captionK);

                    chip.color = new Color(gold.r, gold.g, gold.b, show);
                    headline.color = new Color(headTint.r, headTint.g, headTint.b, show);
                    sub.color = new Color(muted.r, muted.g, muted.b, show * 0.9f);
                    verdict.anchoredPosition = new Vector2(0f, Motion.Mix(196f, 210f, show));

                    yield return null;
                }
            }
            finally { if (root != null) Destroy(root.gameObject); }
        }

        /// <summary>Misch-Animation: alle Handkarten eines Spielers ziehen sich kurz zur Mitte zusammen und fächern neu auf.</summary>
        public IEnumerator ShowHandShuffle(PlayerState player)
        {
            if (!enablePresentations || board == null) yield break;
            var views = new List<RectTransform>();
            var homes = new List<Vector3>();
            foreach (var card in player.Hand)
                if (board.TryGetView(card, out var view))
                {
                    views.Add((RectTransform)view.transform);
                    homes.Add(view.transform.position);
                }
            if (views.Count < 2) yield break;

            Vector3 center = Vector3.zero;
            foreach (var home in homes) center += home;
            center /= homes.Count;

            const float phase = 0.18f;
            float elapsed = 0f;
            while (elapsed < phase)
            {
                elapsed += Time.deltaTime;
                float k = EaseInOut(Mathf.Clamp01(elapsed / phase));
                for (int i = 0; i < views.Count; i++) views[i].position = Vector3.Lerp(homes[i], center, k);
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
            elapsed = 0f;
            while (elapsed < phase)
            {
                elapsed += Time.deltaTime;
                float k = EaseInOut(Mathf.Clamp01(elapsed / phase));
                for (int i = 0; i < views.Count; i++) views[i].position = Vector3.Lerp(center, homes[i], k);
                yield return null;
            }
            for (int i = 0; i < views.Count; i++) views[i].position = homes[i];
        }

        public IEnumerator ShowCardActivation(CardInstance card, EffectDefinition effect)
        {
            if (!enablePresentations || showcaseGroup == null) yield break;

            SpawnShowcaseCard(card);
            if (showcaseBanner != null)
            {
                string kind = "ACTIVATION";
                if (effect != null && effect.isInfused)
                    kind = effect.infusedKind == InfusedKind.Coupled ? "INFUSED UPGRADE" : "INFUSED ACTIVATION";
                string label = effect != null && !string.IsNullOrEmpty(effect.label) ? $"\n„{effect.label}“" : "";
                showcaseBanner.text = $"{kind}\n{card.Name}{label}";
            }

            yield return FadeShowcase(1f);
            yield return new WaitForSeconds(activationHoldDuration);
            yield return FadeShowcase(0f);
        }

        public IEnumerator ShowSummon(CardInstance monster)
        {
            if (!enablePresentations || showcaseGroup == null) yield break;

            SpawnShowcaseCard(monster);
            if (showcaseBanner != null) showcaseBanner.text = $"SUMMON\n{monster.Name}";

            yield return FadeShowcase(1f);
            yield return new WaitForSeconds(summonHoldDuration);
            yield return FadeShowcase(0f);
        }

        /// <summary>
        /// Beschwörung aus dem Extra Deck (Handoff „Animations", Abschnitt 6).
        /// Ein Reliquary kommt nicht aus der Hand, sondern aus dem Tresor — die
        /// Beschwörung IST das Öffnen, darum ein eigener Auftritt statt des
        /// gewöhnlichen Summon-Showcase.
        /// </summary>
        [Header("Extra Deck (Handoff „Animations\", Abschnitt 6)")]
        [Tooltip("Ablage des eigenen Extra Decks — Startpunkt der Beschwörung")]
        [SerializeField] private Transform p1ExtraAnchor;
        [Tooltip("Ablage des gegnerischen Extra Decks")]
        [SerializeField] private Transform p2ExtraAnchor;

        /// <summary>
        /// Beschwörung aus dem Extra Deck (Handoff „Animations", Abschnitt 6).
        /// Sie startet am Extra Deck DESSEN, der beschwört, und endet auf der Zone,
        /// in die die Karte gelegt wird — das Feld übernimmt sie erst danach.
        /// </summary>
        public IEnumerator ShowReliquarySummon(CardInstance monster, PlayerState owner, int zoneIndex)
        {
            if (!enablePresentations) yield break;
            if (cardViewPrefab == null || board == null) yield break;

            bool mine = owner == null || board.Duel == null || owner == board.Duel.LocalPlayer
                        || (board.Duel.LocalPlayer == null && owner == board.Duel.Player1);
            var fromAnchor = mine ? p1ExtraAnchor : p2ExtraAnchor;
            if (fromAnchor == null) fromAnchor = mine ? p1DeckAnchor : p2DeckAnchor;
            var toSlot = board.GetMonsterSlot(mine, zoneIndex);

            SfxManager.CardActivate();
            bool done = false;
            ReliquarySummonSequence.Play(cardViewPrefab, monster,
                fromAnchor != null ? fromAnchor.position : (Vector3?)null,
                toSlot != null ? toSlot.position : (Vector3?)null,
                () => done = true);
            while (!done) yield return null;
        }

        public IEnumerator ShowAttackDeclared(CardInstance attacker, CardInstance target, bool direct)
        {
            if (!enablePresentations || board == null) yield break;

            TcgCardView attackerView = null;
            TcgCardView targetView = null;
            if (board.TryGetView(attacker, out attackerView)) attackerView.SetHighlight(true);
            if (target != null && board.TryGetView(target, out targetView)) targetView.SetHighlight(true);

            yield return new WaitForSeconds(attackFlashDuration);

            if (attackerView != null) attackerView.SetHighlight(false);
            if (targetView != null) targetView.SetHighlight(false);
        }

        /// <summary>
        /// Aktivierungs-Puls direkt auf der Karten-View: warmes Aufblinken + kurzer Größen-Pop;
        /// aus der Hand zusätzlich mit einer vollen Drehung.
        /// </summary>
        /// <summary>
        /// Aktivierung nach Handoff „Animations", Abschnitt 3: Lift und Aufschlag.
        /// Die Karte verlässt ihren Platz, steht gross in der Bildmitte — volle
        /// Lesezeit, bevor irgendetwas mit ihr passiert — und schlägt dann in ihre
        /// Zone ein. Getragen wird das von `charge`: Rahmen, Innenschein und
        /// Effektbox hellen gemeinsam auf.
        /// </summary>
        // ================== Kettenanzeige ==================
        //
        // Die Engine kennt keine Kette als Liste — sie ruft sich rekursiv auf.
        // Diese drei Meldungen sind die einzige Stelle, an der ein Zuschauer die
        // Kette als Kette sehen kann. Der Tracker entsteht beim ersten Glied,
        // damit ein Duell ohne Ketten gar nichts davon anlegt.

        private ChainTracker chain;

        private ChainTracker Chain()
        {
            if (chain != null || flyLayer == null) return chain;

            // An die CANVAS-Wurzel, nicht an den PresentationLayer: nur als
            // letztes Kind des Canvas zeichnet die Kette über allem. Der
            // PresentationLayer hat noch Geschwister über sich.
            var canvas = flyLayer.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            chain = ChainTracker.Create((RectTransform)canvas.transform,
                FindAnyObjectByType<CardDetailPanel>(FindObjectsInactive.Include));
            return chain;
        }

        public IEnumerator ShowChainLink(CardInstance card, string label, PlayerState owner, int link)
        {
            if (!enablePresentations) yield break;
            var tracker = Chain();
            if (tracker == null) yield break;
            yield return tracker.AddLink(card, card != null ? card.Name : "?", label,
                owner != null && owner.IsLocal, link);
        }

        public IEnumerator ShowChainResolve(CardInstance card, int link)
        {
            if (!enablePresentations || chain == null) yield break;
            yield return chain.Resolve(link);
        }

        public IEnumerator ShowChainEnd()
        {
            if (chain == null) yield break;
            yield return chain.Finish();
        }

        public IEnumerator ShowActivationPulse(CardInstance card, bool spin, EffectDefinition effect = null)
        {
            if (!enablePresentations || board == null) yield break;
            SfxManager.CardActivate();

            // Karten ohne Feld-View (Friedhof/Banishment) poppen aus ihrem Stapel
            // auf: Wegwerf-View am Pile-Anker, derselbe Auftritt, danach zurück.
            if (!board.TryGetView(card, out var view))
            {
                if (card.Zone != ZoneType.Graveyard && card.Zone != ZoneType.Banished) yield break;
                if (cardViewPrefab == null || flyLayer == null) yield break;
                var anchor = PileAnchor(card.Owner, card.Zone);
                if (anchor == null) yield break;

                var popup = Instantiate(cardViewPrefab, flyLayer);
                popup.Show(card, false, upright: true);
                popup.SetHighlight(false);
                var popupRect = (RectTransform)popup.transform;
                popupRect.position = anchor.position;
                popupRect.localScale = Vector3.one * 0.8f;
                try
                {
                    yield return PulseView(popup, popupRect, card, anchor.position, Vector3.one * 0.8f, effect);
                }
                finally { Destroy(popup.gameObject); }
                yield break;
            }

            var rect = (RectTransform)view.transform;
            var parent = rect.parent;
            int siblingIndex = rect.GetSiblingIndex();
            Vector3 homeScale = rect.localScale;
            Vector3 homePosition = rect.position;

            // Erst zeigen, dann wirken. Eine Handkarte des Gegners liegt immer
            // verdeckt, ein gesetzter Zauber auch — beide müssen aufgedeckt sein,
            // bevor die Aktivierung läuft, sonst spielt sie auf einem Rücken.
            yield return RevealBeforeActivation(view, card);

            // Über die Nachbarkarten heben, sonst schneidet die Zone die grosse Karte an
            if (flyLayer != null) rect.SetParent(flyLayer, true);
            try
            {
                yield return PulseView(view, rect, card, homePosition, homeScale, effect);
            }
            finally
            {
                rect.localScale = homeScale;
                if (flyLayer != null && rect.parent == flyLayer)
                {
                    rect.SetParent(parent, true);
                    rect.SetSiblingIndex(siblingIndex);
                }
                rect.position = homePosition;
            }
        }

        /// <summary>
        /// Der eigentliche Puls-Auftritt: Lift in die Mitte, Hold mit Effekt-Panel,
        /// Slam zurück zur Heimposition. Wird vom Feld-Pfad (echte View) und vom
        /// Friedhofs-Popup (Wegwerf-View) geteilt.
        /// </summary>
        private IEnumerator PulseView(TcgCardView view, RectTransform rect, CardInstance card,
            Vector3 homePosition, Vector3 homeScale, EffectDefinition effect)
        {
            var centre = flyLayer != null
                ? flyLayer.TransformPoint(Vector3.zero) : homePosition;

            try
            {
                // ---- Lift: in die Mitte, auf 1.62x, Brett dimmt auf 55 % ----
                float lift = liftDuration;
                for (float t = 0f; t < lift; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / lift);
                    float k = Motion.Enter(Motion.Seg(p, 0.06f, 0.78f));
                    rect.position = Vector3.Lerp(homePosition, centre, k);
                    rect.localScale = homeScale * Motion.Mix(1f, 1.62f, k);
                    view.SetCharge(k * 0.4f);
                    SetBoardDim(Motion.Enter(Motion.Seg(p, 0.1f, 0.6f)) * 0.45f);
                    yield return null;
                }

                // ---- Hold: die Karte steht gross, das Panel darunter erklärt den
                // Effekt (Kartentext). Nur bei echten Aktivierungen — der kleine
                // Passiv-Puls (Mill-Burn) kommt ohne Effekt und ohne Haltezeit. ----
                if (effect != null && activationHoldDuration > 0f)
                {
                    ShowEffectCaption(card, effect);
                    for (float t = 0f; t < activationHoldDuration; t += Time.deltaTime)
                    {
                        SetBoardDim(0.45f);
                        if (captionGroup != null)
                            captionGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.05f, fadeDuration));
                        yield return null;
                    }
                }

                // ---- Activate: Aufschlag mit Ringen, Blitz und Erschütterung ----
                float activate = activateDuration;
                for (float t = 0f; t < activate; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / activate);
                    float slam = Motion.Enter(Motion.Seg(p, 0.16f, 0.54f));
                    rect.position = Vector3.Lerp(centre, homePosition, slam);
                    rect.localScale = homeScale * Motion.Mix(1.62f, 1f, slam);
                    view.SetCharge(Motion.Mix(0.4f, 1f, Motion.Enter(Motion.Seg(p, 0f, 0.6f))));
                    if (captionGroup != null && captionGroup.gameObject.activeSelf)
                        captionGroup.alpha = 1f - Motion.Enter(Motion.Seg(p, 0f, 0.45f));
                    // Das Brett hellt wieder auf, während der Zauber greift
                    SetBoardDim(0.45f * (1f - Motion.Enter(Motion.Seg(p, 0.5f, 1f))));
                    if (p >= 0.46f && p <= 0.48f) ScreenShake.Shake(0.018f, 0.7f, 16f);
                    yield return null;
                }
            }
            finally
            {
                view.ClearCharge();
                SetBoardDim(0f);
                HideEffectCaption();
            }
        }

        // ================== EFFEKT-PANEL UNTER DEM PULS ==================
        // Laufzeit-gebaut, einmalig, dann recycelt. Liegt im flyLayer unter der
        // gehobenen Karte und zeigt Label + Kartentext des aktivierten Effekts.

        private RectTransform captionRoot;
        private CanvasGroup captionGroup;
        private TMP_Text captionTitle;
        private TMP_Text captionBody;

        private void EnsureCaption()
        {
            if (captionRoot != null) return;
            var parentLayer = flyLayer != null ? flyLayer : (RectTransform)transform;

            var rootGo = new GameObject("EffectCaption", typeof(RectTransform), typeof(CanvasGroup));
            captionRoot = (RectTransform)rootGo.transform;
            captionRoot.SetParent(parentLayer, false);
            captionRoot.anchorMin = captionRoot.anchorMax = new Vector2(0.5f, 0.5f);
            captionRoot.pivot = new Vector2(0.5f, 1f);
            captionGroup = rootGo.GetComponent<CanvasGroup>();
            captionGroup.blocksRaycasts = false;
            captionGroup.interactable = false;

            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var frameRect = (RectTransform)frame.transform;
            frameRect.SetParent(captionRoot, false);
            frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero; frameRect.offsetMax = Vector2.zero;
            frame.GetComponent<Image>().color = new Color(0.79f, 0.66f, 0.42f, 0.55f);

            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(captionRoot, false);
            plateRect.anchorMin = Vector2.zero; plateRect.anchorMax = Vector2.one;
            plateRect.offsetMin = new Vector2(1.5f, 1.5f); plateRect.offsetMax = new Vector2(-1.5f, -1.5f);
            plate.GetComponent<Image>().color = new Color(0.055f, 0.045f, 0.03f, 0.95f);

            captionTitle = BuildCaptionText("Title", 23f, new Color(0.87f, 0.74f, 0.47f), FontStyles.Bold);
            captionBody = BuildCaptionText("Body", 20f, new Color(0.92f, 0.88f, 0.8f), FontStyles.Normal);

            captionRoot.gameObject.SetActive(false);
        }

        private TMP_Text BuildCaptionText(string name, float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(captionRoot, false);
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (showcaseBanner != null) text.font = showcaseBanner.font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Füllt das Panel mit Label + Kartentext und legt es unter die Karte.</summary>
        private void ShowEffectCaption(CardInstance card, EffectDefinition effect)
        {
            EnsureCaption();
            if (captionRoot == null || captionTitle == null || captionBody == null) return;

            string title = !string.IsNullOrWhiteSpace(effect.label) ? effect.label : card.Name;
            string kind = effect.isInfused
                ? (effect.infusedKind == InfusedKind.Coupled ? "  ·  INFUSED UPGRADE" : "  ·  INFUSED")
                : "";
            string cost = effect.manaCost > 0 ? $"[{effect.manaCost} Mana]  " : "";
            string body = string.IsNullOrWhiteSpace(effect.text) ? "" : effect.text.Trim();

            captionTitle.text = $"„{title}“{kind}";
            captionBody.text = cost + body;

            const float width = 640f, padX = 24f, padY = 14f, gap = 7f;
            float innerWidth = width - padX * 2f;
            float titleHeight = captionTitle.GetPreferredValues(captionTitle.text, innerWidth, 0f).y;
            float bodyHeight = captionBody.text.Length > 0
                ? captionBody.GetPreferredValues(captionBody.text, innerWidth, 0f).y : 0f;
            float height = padY * 2f + titleHeight + (bodyHeight > 0f ? gap + bodyHeight : 0f);

            captionRoot.sizeDelta = new Vector2(width, height);
            // Pivot oben-Mitte: Oberkante 168 px unter der Bildschirmmitte — knapp
            // unter der grössten gehobenen Karte (Handkarte 168 px × 1.62 / 2 ≈ 136)
            captionRoot.anchoredPosition = new Vector2(0f, -168f);

            var titleRect = (RectTransform)captionTitle.transform;
            titleRect.offsetMin = new Vector2(padX, 0f); titleRect.offsetMax = new Vector2(-padX, 0f);
            titleRect.anchoredPosition = new Vector2(0f, -padY);
            titleRect.sizeDelta = new Vector2(-padX * 2f, titleHeight);

            var bodyRect = (RectTransform)captionBody.transform;
            bodyRect.offsetMin = new Vector2(padX, 0f); bodyRect.offsetMax = new Vector2(-padX, 0f);
            bodyRect.anchoredPosition = new Vector2(0f, -(padY + titleHeight + gap));
            bodyRect.sizeDelta = new Vector2(-padX * 2f, bodyHeight);

            captionGroup.alpha = 0f;
            captionRoot.SetAsLastSibling();
            captionRoot.gameObject.SetActive(true);
        }

        private void HideEffectCaption()
        {
            if (captionRoot == null) return;
            captionGroup.alpha = 0f;
            captionRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Zielwahl nach Handoff Abschnitt 3: ein Faden vom Zauberer zum Ziel und
        /// ein Fadenkreuz, das von 120 px auf 8 px zufährt. Das Fadenkreuz wandert
        /// dabei vom Zauberer zum Ziel — man sieht, WOHER die Zielwahl kommt.
        /// </summary>
        public IEnumerator ShowTargetsFlash(List<CardInstance> targets)
        {
            if (!enablePresentations || board == null || targets == null) yield break;
            var views = new List<TcgCardView>();
            foreach (var target in targets)
                if (target != null && board.TryGetView(target, out var view) && !views.Contains(view))
                    views.Add(view);
            if (views.Count == 0) yield break;

            var reticles = new List<TargetReticle>();
            foreach (var view in views)
                reticles.Add(TargetReticle.Build(flyLayer != null ? flyLayer : (RectTransform)view.transform.parent));

            var from = flyLayer != null ? (Vector2)flyLayer.InverseTransformPoint(
                board.transform.position) : Vector2.zero;

            try
            {
                float duration = targetDuration;
                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / duration);
                    float thread = Motion.Enter(Motion.Seg(p, 0.08f, 0.46f));
                    float travel = Motion.Enter(Motion.Seg(p, 0.12f, 0.52f));
                    float lock01 = Motion.Enter(Motion.Seg(p, 0.5f, 0.86f));
                    float spin = Motion.Mix(120f, 0f, Motion.Enter(Motion.Seg(p, 0.12f, 0.86f)));

                    for (int i = 0; i < reticles.Count; i++)
                    {
                        var target = (RectTransform)views[i].transform;
                        var to = reticles[i].Rect.parent.InverseTransformPoint(target.position);
                        reticles[i].Apply(Vector2.Lerp(from, to, travel), to, target.rect.size,
                            thread, lock01, spin, Motion.Mix(0.9f, 0.45f, lock01));
                    }
                    yield return null;
                }
            }
            finally
            {
                foreach (var reticle in reticles)
                    if (reticle != null) Destroy(reticle.gameObject);
            }
        }

        /// <summary>
        /// Dreht eine verdeckte Karte auf, bevor sie aktiviert. Die Karte staucht
        /// über ihre Mittelachse zusammen, wechselt bei null Breite das Gesicht und
        /// fährt wieder auf — dieselbe Bewegung wie beim Pack-Öffnen, nur kürzer.
        /// Liegt die Karte bereits offen, kostet das keine Zeit.
        /// </summary>
        private IEnumerator RevealBeforeActivation(TcgCardView view, CardInstance card)
        {
            if (view == null || !view.HiddenFace) yield break;

            var rect = (RectTransform)view.transform;
            var baseScale = rect.localScale;
            SfxManager.CardPlace();

            float half = revealDuration * 0.5f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Clamp01(t / half);
                rect.localScale = new Vector3(baseScale.x * k, baseScale.y, baseScale.z);
                yield return null;
            }

            view.Show(card, false, upright: true);
            view.SetHighlight(false);

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / half);
                rect.localScale = new Vector3(baseScale.x * k, baseScale.y, baseScale.z);
                yield return null;
            }
            rect.localScale = baseScale;
        }

        /// <summary>Dimmt alles ausser der aktiven Karte — der Blick soll dort bleiben.</summary>
        private void SetBoardDim(float amount)
        {
            if (boardDim == null)
            {
                if (flyLayer == null || flyLayer.parent == null) return;
                var go = new GameObject("~BoardDim", typeof(RectTransform));
                go.layer = flyLayer.gameObject.layer;
                var rect = (RectTransform)go.transform;
                // Direkt hinter die Flugebene: das Brett dimmt, die gehobene Karte nicht
                rect.SetParent(flyLayer.parent, false);
                rect.SetSiblingIndex(flyLayer.GetSiblingIndex());
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                boardDim = go.AddComponent<Image>();
                boardDim.raycastTarget = false;
            }
            boardDim.gameObject.SetActive(amount > 0.002f);
            boardDim.color = new Color(0.039f, 0.027f, 0.02f, Mathf.Clamp01(amount));
        }

        private Image boardDim;

        private void SpawnShowcaseCard(CardInstance card)
        {
            if (showcaseView != null) Destroy(showcaseView.gameObject);
            showcaseView = null;
            if (cardViewPrefab == null || showcaseCardHolder == null) return;

            showcaseView = Instantiate(cardViewPrefab, showcaseCardHolder);
            var rect = (RectTransform)showcaseView.transform;
            rect.anchoredPosition = Vector2.zero;
            showcaseView.Show(card, false, upright: true); // Show setzt Rotation/Scale zurück — Scale danach anwenden
            rect.localScale = Vector3.one * showcaseScale;
        }

        private IEnumerator FadeShowcase(float targetAlpha)
        {
            showcaseGroup.gameObject.SetActive(true);
            float start = showcaseGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                showcaseGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            showcaseGroup.alpha = targetAlpha;

            if (Mathf.Approximately(targetAlpha, 0f))
            {
                showcaseGroup.gameObject.SetActive(false);
                if (showcaseView != null)
                {
                    Destroy(showcaseView.gameObject);
                    showcaseView = null;
                }
            }
        }
    }
}
