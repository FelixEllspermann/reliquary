using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Der Übergang zwischen Münzwurf-Wahl und Brett (Handoff "Duel Load"): das
    /// Deck sammelt sich aus dem Dunkeln, taumelt als eine Masse — das Mischen —
    /// und wird dann über das Feld geschleudert, während darunter das Zonenraster
    /// auftaucht. Zum Schluss landet das Zug-Banner.
    ///
    /// Wie der Tresor-Übergang ist das eine LADEMASKE: Phasen 1–3 sind fest
    /// (4,2 s), Phase 4 hält, bis das Duell wirklich bereit ist. Anders als beim
    /// Tresor darf man hier überspringen — der Übergang läuft ja jedes Match.
    ///
    /// Die sieben Karten sind Deko und NICHT die Starthand. Ihre Konstanten sind
    /// fest verdrahtet: die Animation muss Bild für Bild identisch laufen.
    /// </summary>
    public class DuelLoadTransition : MonoBehaviour
    {
        // ---- Zeitachse (Handoff: Grenzen bei 1,2 / 2,8 / 4,2 s) ----
        private const float GatherDuration = 1.2f;
        private const float TumbleDuration = 1.6f;
        private const float DealDuration = 1.4f;
        private const float SettleDuration = 2.0f;

        private const float BaseWidth = 1280f;
        private const float BaseHeight = 720f;

        /// <summary>Zonenraster steht — ab hier darf das echte Brett einblenden.</summary>
        public event Action OnBoardVisible;

        /// <summary>Banner sitzt. Phase 4 hält ab hier, bis <see cref="ReleaseToDuel"/> kommt.</summary>
        public event Action OnBannerSettled;

        /// <summary>
        /// Solange true, hält der Vorhang — Duellstart und Server-Pipeline warten
        /// darauf, damit der Spieler die Eröffnung nicht hinter dem Schleier verpasst.
        /// </summary>
        public static bool CurtainHolding { get; private set; }

        private bool released;
        public void ReleaseToDuel() => released = true;

        /// <summary>
        /// Den laufenden Vorhang freigeben. Seit der Münzwurf im Duell liegt, weiss
        /// erst der DuelHost, wann wirklich gespielt werden kann — und der kennt
        /// diese Instanz nicht.
        /// </summary>
        public static void Release()
        {
            if (current != null) current.released = true;
        }

        private static DuelLoadTransition current;

        /// <summary>Die sieben Karten. Ganzzahlige n/fl sind Pflicht — siehe Handoff.</summary>
        private struct CardSpec
        {
            public float a0, a1, dx, dy, d;
            public int n, fl;
            public CardSpec(float a0, float a1, int n, int fl, float dx, float dy, float d)
            { this.a0 = a0; this.a1 = a1; this.n = n; this.fl = fl; this.dx = dx; this.dy = dy; this.d = d; }
        }

        // dy ist im Handoff nach unten positiv — hier gleich auf Unity-Y gedreht
        private static readonly CardSpec[] Cards =
        {
            new CardSpec(-34f, -13f,  2, 3, -420f, -118f, 0.00f),
            new CardSpec(-17f, -6.5f, 3, 2, -212f, -118f, 0.06f),
            new CardSpec(  4f,  0f,   2, 4,    0f, -118f, 0.12f),
            new CardSpec( 21f,  6.5f, 3, 3,  212f, -118f, 0.18f),
            new CardSpec( 38f, 13f,   2, 2,  420f, -118f, 0.24f),
            new CardSpec(-26f,  0f,   3, 4, -108f,  126f, 0.30f),
            new CardSpec( 29f,  0f,   2, 3,  108f,  126f, 0.36f),
        };

        // ---- Bauteile ----
        private CanvasGroup rootGroup;
        private RectTransform stage;
        private Image vaultGlow;
        private RectTransform weave;
        private RectTransform ornament;
        private Image ornamentImage;
        private CanvasGroup boardGroup;
        private Image opponentTint, playerTint;
        private readonly RectTransform[] cardRoots = new RectTransform[7];
        private readonly RectTransform[] cardFlips = new RectTransform[7];
        private readonly CanvasGroup[] cardGroups = new CanvasGroup[7];
        private readonly Image[] cardKeylines = new Image[7];
        private readonly Image[] cardGlows = new Image[7];
        private CanvasGroup captionGroup;
        private RectTransform captionRect;
        private CanvasGroup bannerGroup;
        private RectTransform bannerRect;
        private TMP_Text holdNote;

        private TransitionSkin skin;
        private bool skipRequested;

        // ================== KURVEN ==================
        private static float Enter(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        private static float Drift(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(t));
        private static float Seg(float p, float a, float b) => Mathf.Clamp01((p - a) / (b - a));

        // ================== ÖFFENTLICHER EINSTIEG ==================

        /// <summary>
        /// Spielt den Übergang. Der Aufrufer lädt parallel und ruft
        /// <see cref="ReleaseToDuel"/>, sobald das Duell steht.
        /// </summary>
        public static DuelLoadTransition Play(bool? localStarts, string opponentName, string deckName, int deckCount)
        {
            var host = new GameObject("~DuelLoad");
            DontDestroyOnLoad(host);
            var transition = host.AddComponent<DuelLoadTransition>();
            current = transition;
            CurtainHolding = true;
            transition.Build(localStarts, opponentName, deckName, deckCount);
            transition.StartCoroutine(transition.Run());
            return transition;
        }

        private IEnumerator Run()
        {
            yield return Phase(GatherDuration, Gather);
            yield return Phase(TumbleDuration, Tumble);
            yield return Phase(DealDuration, Deal);

            // Überspringen landet exakt hier — das Banner sieht man immer
            if (skipRequested && !boardFired) { boardFired = true; OnBoardVisible?.Invoke(); }

            yield return Phase(SettleDuration, Settle);

            if (!released)
            {
                // Notausgang. Wer hier hält, wartet auf eine Freigabe von aussen —
                // und wenn die ausbleibt, steht das ganze Spiel. Genau das ist schon
                // passiert. Lieber ein Vorhang, der zu früh hochgeht, als einer, der
                // nie hochgeht.
                const float maxHold = 8f;
                holdNote.gameObject.SetActive(true);
                float waited = 0f;
                while (!released && waited < maxHold)
                {
                    waited += Time.unscaledDeltaTime;
                    holdNote.alpha = Mathf.Clamp01((waited - 0.4f) / 0.6f);
                    yield return null;
                }
                if (!released)
                    Debug.LogWarning($"DuelLoadTransition: niemand hat nach {maxHold:0} s freigegeben — Vorhang geht von selbst hoch.");
            }

            CurtainHolding = false;

            float fade = 0f;
            while (fade < 0.25f)
            {
                fade += Time.unscaledDeltaTime;
                rootGroup.alpha = 1f - Mathf.Clamp01(fade / 0.25f);
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator Phase(float duration, Action<float> apply)
        {
            float elapsed = 0f;
            apply(0f);
            while (elapsed < duration)
            {
                if (skipRequested) { apply(1f); yield break; }
                elapsed += Time.unscaledDeltaTime;
                apply(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            apply(1f);
        }

        /// <summary>
        /// Sicherheitsnetz: geht der Übergang aus irgendeinem Grund vorzeitig
        /// kaputt, darf das Duell nicht ewig auf einen Vorhang warten, den es
        /// nicht mehr gibt.
        /// </summary>
        private void OnDestroy()
        {
            CurtainHolding = false;
            if (current == this) current = null;
        }

        private void Update()
        {
            if (skipRequested || bannerFired) return;
            bool pressed =
                (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (pressed) skipRequested = true;
        }

        // ================== PHASEN ==================

        private void Gather(float p)
        {
            if (!gatherHeard) { gatherHeard = true; SfxManager.CardShuffle(); }

            ApplyBackdrop(warm: false, weaveTurn: 0f, weaveAlpha: 0.045f);
            ornamentImage.color = Hex("#C8A45C", 0f);
            boardGroup.alpha = 0f;

            for (int i = 0; i < Cards.Length; i++)
            {
                var c = Cards[i];
                float t = Enter(Seg(p, c.d * 0.5f, 0.5f + c.d * 0.5f));
                ApplyCard(i,
                    x: Mathf.Lerp(c.dx * 1.9f, 0f, t),
                    y: Mathf.Lerp(c.dy * 2.4f - 300f, 0f, t),   // von unterhalb des Rahmens
                    rot: Mathf.Lerp(c.a0 * 2.4f, c.a0 * 0.35f, t),
                    spin: 0f,
                    scale: Mathf.Lerp(0.8f, 1f, t),
                    alpha: t,
                    lit: 0f);
            }

            float caption = Enter(Seg(p, 0.24f, 0.60f));
            captionGroup.alpha = caption;
            captionRect.anchoredPosition = new Vector2(0f, CaptionY - Mathf.Lerp(18f, 0f, caption));
            bannerGroup.alpha = 0f;
        }

        private void Tumble(float p)
        {
            float swirl = Drift(p);
            float wob = Mathf.Sin(2f * Mathf.PI * p) * 26f;
            float bloom = Mathf.Sin(Mathf.PI * p);

            ApplyBackdrop(warm: false, weaveTurn: swirl * 22f, weaveAlpha: 0.05f);
            boardGroup.alpha = 0f;

            float size = Mathf.Lerp(380f, 620f, swirl);
            ornament.sizeDelta = new Vector2(size, size);
            ornament.localEulerAngles = new Vector3(0f, 0f, -(45f + swirl * 90f));
            ornamentImage.color = Hex("#C8A45C", 0.2f - swirl * 0.1f);

            for (int i = 0; i < Cards.Length; i++)
            {
                var c = Cards[i];
                // Ganzzahlige Umläufe: bei p = 1 steht jede Karte wieder exakt
                // im Ausgangswinkel und mit der Ausgangsseite — sonst ruckt der Schnitt.
                float a = c.a0 * 0.35f + swirl * 360f * c.n;
                float rad = bloom * 96f;
                ApplyCard(i,
                    x: Mathf.Cos(a * Mathf.Deg2Rad) * rad,
                    y: -(Mathf.Sin(a * Mathf.Deg2Rad) * rad * 0.5f + wob * 0.2f),
                    rot: a,
                    spin: p * c.fl,
                    scale: Mathf.Lerp(1f, 1.06f, bloom),
                    alpha: 1f,
                    lit: bloom * 0.35f);
            }

            float caption = 1f - Enter(Seg(p, 0.60f, 0.94f));
            captionGroup.alpha = caption;
            captionRect.anchoredPosition = new Vector2(0f, CaptionY + Mathf.Lerp(0f, 12f, 1f - caption));
            bannerGroup.alpha = 0f;
        }

        private bool boardFired;
        private bool bannerFired;
        private bool gatherHeard;
        private bool bannerHeard;
        private readonly bool[] cardLanded = new bool[7];

        private void Deal(float p)
        {
            float board = Enter(Seg(p, 0.30f, 0.90f));

            ApplyBackdrop(warm: true, weaveTurn: 0f, weaveAlpha: 0.045f);
            ornamentImage.color = Hex("#C8A45C", 0f);
            ApplyBoard(board);

            for (int i = 0; i < Cards.Length; i++)
            {
                var c = Cards[i];
                float t = Enter(Seg(p, c.d * 0.9f, 0.52f + c.d * 0.9f));
                float arc = Mathf.Sin(Mathf.PI * t) * 54f;   // Hub — sonst liest es sich als Schieben
                ApplyCard(i,
                    x: Mathf.Lerp(0f, c.dx, t),
                    y: Mathf.Lerp(0f, c.dy, t) + arc,
                    rot: Mathf.Lerp(c.a0 * 0.35f, c.a1, t),
                    spin: 0f,
                    scale: Mathf.Lerp(1f, 0.79f, t),
                    alpha: 1f,
                    lit: (1f - t) * 0.3f);

                // Sieben versetzte Schnapper, wenn die Karten aufsetzen
                if (!cardLanded[i] && t >= 0.94f) { cardLanded[i] = true; SfxManager.CardPlace(); }
            }

            captionGroup.alpha = 0f;
            bannerGroup.alpha = 0f;
            if (!boardFired && board >= 0.999f) { boardFired = true; OnBoardVisible?.Invoke(); }
        }

        private void Settle(float p)
        {
            float fade = 1f - Enter(Seg(p, 0.10f, 0.62f));
            float appear = Enter(Seg(p, 0.16f, 0.50f));

            ApplyBackdrop(warm: true, weaveTurn: 0f, weaveAlpha: 0.045f);
            ApplyBoard(1f);

            for (int i = 0; i < Cards.Length; i++)
            {
                var c = Cards[i];
                ApplyCard(i,
                    x: c.dx, y: c.dy,
                    rot: Mathf.Lerp(c.a1, c.a1 * 0.3f, Enter(p)),
                    spin: 0f,
                    scale: Mathf.Lerp(0.79f, 0.76f, Enter(p)),
                    alpha: fade,
                    lit: 0f);
            }

            captionGroup.alpha = 0f;
            bannerGroup.alpha = appear;
            bannerRect.anchoredPosition = new Vector2(0f, BannerY - Mathf.Lerp(20f, 0f, appear));
            if (!bannerHeard && appear > 0.02f) { bannerHeard = true; SfxManager.CoinHit(); }

            if (!bannerFired && p >= 0.98f) { bannerFired = true; OnBannerSettled?.Invoke(); }
        }

        // ================== ANWENDUNG ==================

        private void ApplyBackdrop(bool warm, float weaveTurn, float weaveAlpha)
        {
            vaultGlow.color = Hex(warm ? "#241811" : "#2A1C12", 1f);
            weave.localEulerAngles = new Vector3(0f, 0f, -weaveTurn);
            weave.GetComponent<Image>().color = Hex("#C8A45C", weaveAlpha);
        }

        private void ApplyBoard(float opacity)
        {
            boardGroup.alpha = opacity;
            opponentTint.color = Hex("#283E56", 0.42f * opacity);
            playerTint.color = Hex("#603412", 0.30f * opacity);
        }

        private void ApplyCard(int i, float x, float y, float rot, float spin, float scale, float alpha, float lit)
        {
            var root = cardRoots[i];
            root.anchoredPosition = new Vector2(x, y);
            root.localEulerAngles = new Vector3(0f, 0f, -rot);
            root.localScale = Vector3.one * scale;
            cardGroups[i].alpha = alpha;

            // Der echte Y-Dreh statt des DOM-Squash-Tricks — der Canvas plattet
            // ihn von selbst korrekt ab.
            cardFlips[i].localEulerAngles = new Vector3(0f, spin * 360f, 0f);

            // Umschlagende Kante als leiser Hinweis, dass die Karte sich gedreht hat
            bool front = Mathf.Cos(spin * Mathf.PI * 2f) >= 0f;
            cardKeylines[i].color = front ? Hex("#C8A45C", 1f) : Hex("#EBCE8A", 1f);

            // Handoff: Schein von 18 bis 48 px um die Karte — nicht mehr
            float spread = 18f + lit * 30f;
            cardGlows[i].rectTransform.sizeDelta = new Vector2(116f + spread * 2f, 162f + spread * 2f);
            cardGlows[i].color = Hex("#EBCE8A", lit * 0.5f);
            cardGlows[i].gameObject.SetActive(lit > 0.001f);
        }

        // ================== AUFBAU ==================

        private const float CaptionY = 243f;   // Handoff: Block bei top:74
        private const float BannerY = 0f;      // Handoff: Block bei top:CY-62

        private void Build(bool? localStarts, string opponentName, string deckName, int deckCount)
        {
            skin = TransitionSkin.Load();
            if (skin == null)
            {
                Debug.LogError("[DuelLoad] TransitionSkin fehlt in Resources — Menü: Rouge TCG/Rebuild Transition Skin");
                skin = ScriptableObject.CreateInstance<TransitionSkin>();
            }

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            rootGroup = canvasGo.AddComponent<CanvasGroup>();
            rootGroup.blocksRaycasts = true;

            var canvasRect = (RectTransform)canvasGo.transform;

            var backdrop = MakeImage("Backdrop", canvasRect, Hex("#0B0705", 1f));
            FullScreen(backdrop.rectTransform);

            stage = MakeRect("Stage", canvasRect);
            stage.localScale = Vector3.one * (1920f / BaseWidth);

            // Handoff: Ellipse 1000x640, aussen bei 76 %
            vaultGlow = MakeImage("TableGlow", stage, Hex("#2A1C12", 1f));
            vaultGlow.sprite = skin.glow;
            vaultGlow.rectTransform.sizeDelta = new Vector2(1520f, 972f);
            vaultGlow.rectTransform.anchoredPosition = new Vector2(0f, 14f);

            weave = MakeRect("Weave", stage);
            weave.sizeDelta = new Vector2(BaseWidth * 2f, BaseHeight * 2f);
            var weaveImage = weave.gameObject.AddComponent<Image>();
            weaveImage.sprite = skin.weave;
            weaveImage.type = Image.Type.Tiled;
            weaveImage.color = Hex("#C8A45C", 0.045f);
            weaveImage.raycastTarget = false;

            ornament = MakeRect("Ornament", stage);
            ornamentImage = ornament.gameObject.AddComponent<Image>();
            ornamentImage.sprite = skin.frame;
            ornamentImage.type = Image.Type.Sliced;
            ornamentImage.color = Hex("#C8A45C", 0f);
            ornamentImage.raycastTarget = false;

            BuildBoard();
            for (int i = 0; i < Cards.Length; i++) BuildCard(i);
            BuildCaption(deckName, deckCount);
            BuildBanner(localStarts, opponentName);

            // Das Sprite trägt die Abdunklung in der Alpha — die Tint muss schwarz sein
            var vignette = MakeImage("Vignette", canvasRect, Color.black);
            vignette.sprite = skin.vignette;
            FullScreen(vignette.rectTransform);
        }

        /// <summary>
        /// Platzhalter-Brett: nur so viel, dass unter den fliegenden Karten schon
        /// ein Feld steht. Das echte Brett blendet ab <see cref="OnBoardVisible"/> darunter ein.
        /// </summary>
        private void BuildBoard()
        {
            var board = MakeRect("Board", stage);
            boardGroup = board.gameObject.AddComponent<CanvasGroup>();
            boardGroup.alpha = 0f;

            // Handoff: lineare Verläufe von den Rändern zur Mitte, nicht radial
            opponentTint = MakeImage("OpponentTint", board, Hex("#283E56", 0.42f));
            opponentTint.sprite = skin.fade;
            opponentTint.rectTransform.sizeDelta = new Vector2(BaseWidth * 1.6f, 300f);
            opponentTint.rectTransform.anchoredPosition = new Vector2(0f, 210f);

            playerTint = MakeImage("PlayerTint", board, Hex("#603412", 0.30f));
            playerTint.sprite = skin.fade;
            playerTint.rectTransform.sizeDelta = new Vector2(BaseWidth * 1.6f, 280f);
            playerTint.rectTransform.anchoredPosition = new Vector2(0f, -220f);
            playerTint.rectTransform.localScale = new Vector3(1f, -1f, 1f);   // Verlauf gespiegelt

            var midline = MakeImage("Midline", board, Hex("#C8A45C", 0.5f));
            midline.sprite = skin.rule;
            midline.rectTransform.sizeDelta = new Vector2(520f, 1f);
            midline.rectTransform.anchoredPosition = new Vector2(-260f, 0f);
            var midline2 = MakeImage("MidlineRight", board, Hex("#C8A45C", 0.5f));
            midline2.sprite = skin.rule;
            midline2.rectTransform.sizeDelta = new Vector2(520f, 1f);
            midline2.rectTransform.anchoredPosition = new Vector2(260f, 0f);
            midline2.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            float[] rowY = { 177.5f, 39.5f, -100.5f, -238.5f };
            for (int row = 0; row < 4; row++)
                for (int i = 0; i < 5; i++)
                {
                    Sprite sprite;
                    if (row == 1 || row == 2) sprite = skin.zoneMonster;
                    else if (row == 0) sprite = i < 3 ? skin.zoneSpell : skin.zoneArtifact;
                    else sprite = i < 2 ? skin.zoneSpell : skin.zoneArtifact;

                    var zone = MakeImage($"Zone{row}_{i}", board, new Color(1f, 1f, 1f, 0.85f));
                    zone.sprite = sprite;
                    zone.rectTransform.sizeDelta = new Vector2(92f, 129f);
                    zone.rectTransform.anchoredPosition = new Vector2((i - 2) * 104f, rowY[row]);
                }
        }

        private void BuildCard(int i)
        {
            var root = MakeRect("Card" + i, stage);
            cardRoots[i] = root;
            cardGroups[i] = root.gameObject.AddComponent<CanvasGroup>();

            var glow = MakeImage("Glow", root, Hex("#EBCE8A", 0f));
            glow.sprite = skin.glow;
            glow.rectTransform.sizeDelta = new Vector2(280f, 320f);
            glow.gameObject.SetActive(false);
            cardGlows[i] = glow;

            var flip = MakeRect("Flip", root);
            cardFlips[i] = flip;

            var back = MakeImage("Back", flip, Color.white);
            back.sprite = skin.cardBack;
            back.rectTransform.sizeDelta = new Vector2(116f, 162f);

            var keyline = MakeImage("Keyline", flip, Hex("#C8A45C", 1f));
            keyline.sprite = skin.frame;
            keyline.type = Image.Type.Sliced;
            keyline.rectTransform.sizeDelta = new Vector2(116f, 162f);
            cardKeylines[i] = keyline;
        }

        private void BuildCaption(string deckName, int deckCount)
        {
            captionRect = MakeRect("Caption", stage);
            captionRect.sizeDelta = new Vector2(900f, 86f);
            captionRect.anchoredPosition = new Vector2(0f, CaptionY);
            captionGroup = captionRect.gameObject.AddComponent<CanvasGroup>();
            captionGroup.alpha = 0f;

            var head = MakeText("Head", captionRect, skin.cinzel, 46f, Hex("#F1DFB8", 1f));
            head.text = "Shuffling";
            head.characterSpacing = 5f;
            head.lineSpacing = 20f;
            head.alignment = TextAlignmentOptions.Center;
            PlaceStrip((RectTransform)head.transform, 900f, 60f, 15f);

            var sub = MakeText("Sub", captionRect, skin.spectral, 17f, Hex("#A2917A", 1f));
            sub.text = $"{Mathf.Max(1, deckCount)} cards · {(string.IsNullOrEmpty(deckName) ? "Your deck" : deckName)}";
            sub.alignment = TextAlignmentOptions.Center;
            PlaceStrip((RectTransform)sub.transform, 900f, 22f, -34f);
        }

        private void BuildBanner(bool? localStarts, string opponentName)
        {
            bannerRect = MakeRect("Banner", stage);
            bannerRect.sizeDelta = new Vector2(900f, 125f);
            bannerRect.anchoredPosition = new Vector2(0f, BannerY);
            bannerGroup = bannerRect.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;

            string foe = string.IsNullOrEmpty(opponentName) ? "Opponent" : opponentName.ToUpperInvariant();
            // Ohne Angabe steht der Münzwurf noch aus — er läuft jetzt im Duell,
            // also darf der Vorhang nichts versprechen, was noch niemand weiss.
            string headline = localStarts == null ? "THE VAULT OPENS"
                : localStarts.Value ? "YOUR TURN" : $"{foe} OPENS";
            string note = localStarts == null
                ? "The coin has yet to fall."
                : localStarts.Value
                    ? "Draw phase skipped — you chose to go first."
                    : "You drew one extra card — you chose to go second.";

            var row = MakeRect("HeadRow", bannerRect);
            row.sizeDelta = new Vector2(900f, 68f);
            row.anchoredPosition = new Vector2(0f, 29f);
            // childControlWidth muss an: sonst nimmt die Gruppe die aktuelle Breite
            // der Kinder (0 beim frisch gebauten Text) und alles klebt in der Mitte.
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            MakeDiamond(row);
            var head = MakeText("Headline", row, skin.cinzel, 56f, Hex("#F8EED6", 1f));
            head.text = headline;
            head.characterSpacing = 6f;
            head.lineSpacing = 20f;
            head.alignment = TextAlignmentOptions.Center;
            ((RectTransform)head.transform).sizeDelta = new Vector2(0f, 68f);
            MakeDiamond(row);

            var parchment = MakeImage("Parchment", bannerRect, Color.white);
            parchment.sprite = skin.parchment;
            parchment.type = Image.Type.Sliced;
            parchment.rectTransform.anchoredPosition = new Vector2(0f, -42.5f);
            var pLayout = parchment.gameObject.AddComponent<HorizontalLayoutGroup>();
            pLayout.childAlignment = TextAnchor.MiddleCenter;
            pLayout.padding = new RectOffset(20, 20, 12, 12);
            pLayout.childControlWidth = pLayout.childControlHeight = true;
            pLayout.childForceExpandWidth = pLayout.childForceExpandHeight = false;
            var pFit = parchment.gameObject.AddComponent<ContentSizeFitter>();
            pFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var noteText = MakeText("Note", parchment.rectTransform, skin.spectral, 16f, Hex("#2E2417", 1f));
            noteText.text = note;
            noteText.alignment = TextAlignmentOptions.Center;

            holdNote = MakeText("HoldNote", bannerRect, skin.spectral, 15f, Hex("#8C7B5F", 1f));
            holdNote.text = "Preparing the field…";
            holdNote.alignment = TextAlignmentOptions.Center;
            PlaceStrip((RectTransform)holdNote.transform, 900f, 22f, -84f);
            holdNote.alpha = 0f;
            holdNote.gameObject.SetActive(false);
        }

        private void MakeDiamond(RectTransform parent)
        {
            var diamond = MakeImage("Diamond", parent, Hex("#C8A45C", 1f));
            diamond.sprite = skin.square;
            diamond.rectTransform.sizeDelta = new Vector2(10f, 10f);
            diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            var element = diamond.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 10f;
            element.preferredHeight = 10f;
        }

        // ---------- kleine Bau-Helfer ----------

        private static Color Hex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            return color;
        }

        private static void FullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PlaceStrip(RectTransform rect, float width, float height, float y)
        {
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image MakeImage(string name, RectTransform parent, Color color)
        {
            var image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
