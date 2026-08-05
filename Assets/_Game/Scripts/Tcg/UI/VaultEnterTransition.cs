using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Der Übergang nach dem Login (Handoff "Vault Enter"): ein Siegel füllt das
    /// Bild, sechs Schlösser lösen nacheinander aus, die Hälften brechen auf, das
    /// Bild blendet weiss — und dahinter steht das Hauptmenü.
    ///
    /// Er ist ein LADESCHLEIER: Phasen 1–3 sind fest choreografiert (4,6 s), die
    /// Ankunft in Phase 4 hält so lange, wie das Laden braucht. Der Szenenwechsel
    /// passiert im Weisspunkt, wo ihn niemand sieht.
    ///
    /// Alles entsteht zur Laufzeit auf einem eigenen Canvas mit DontDestroyOnLoad —
    /// so überlebt der Übergang den Szenenwechsel und liegt über allem. Die Sprites
    /// kommen aus dem <see cref="TransitionSkin"/> in Resources, weil Szenen-
    /// Referenzen einen Szenenwechsel nicht überleben würden.
    ///
    /// Alle Zahlen stehen in der 1280x720-Bühne des Handoffs; die Bühne wird einmal
    /// hochskaliert, damit man die Werte direkt gegen die Referenz lesen kann.
    /// </summary>
    public class VaultEnterTransition : MonoBehaviour
    {
        // ---- Zeitachse (Handoff: Summen sind verbindlich) ----
        private const float ApproachDuration = 1.5f;
        private const float UnlockDuration = 2.0f;
        private const float OpenDuration = 1.1f;
        private const float ArriveDuration = 2.0f;

        private const float BaseWidth = 1280f;
        private const float BaseHeight = 720f;

        /// <summary>Weisspunkt erreicht — hier ist der Szenenwechsel unsichtbar.</summary>
        public event Action OnCurtainPeak;

        /// <summary>Lockup steht. Phase 4 hält ab hier, bis <see cref="ReleaseToMenu"/> kommt.</summary>
        public event Action OnArriveSettled;

        private bool released;
        public void ReleaseToMenu() => released = true;

        // ---- Bauteile ----
        private CanvasGroup rootGroup;
        private RectTransform stage;
        private RectTransform weave;
        private readonly RectTransform[] ornaments = new RectTransform[4];
        private readonly Image[] ornamentImages = new Image[4];
        private Image centreBloom;
        private RectTransform sealRoot;
        private Image sealGlow;
        private RectTransform leftClip, rightClip;
        private RectTransform tumblerRing;
        private readonly Image[] tumblers = new Image[6];
        private readonly RectTransform[] tumblerRects = new RectTransform[6];
        private readonly RectTransform[] reliefs = new RectTransform[4]; // je Hälfte aussen+innen
        private RectTransform core;
        private Image coreImage;
        private Image flare;
        private Image whiteOut;
        private CanvasGroup captionGroup;
        private RectTransform captionRect;
        private TMP_Text eyebrow, headline;
        private CanvasGroup pillGroup;
        private CanvasGroup lockupGroup;
        private RectTransform lockupRect;
        private TMP_Text holdNote;

        private TransitionSkin skin;

        // ================== KURVEN (identisch zum Münzwurf) ==================
        private static float Enter(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        private static float Drift(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(t));
        private static float Seg(float p, float a, float b) => Mathf.Clamp01((p - a) / (b - a));

        // ================== ÖFFENTLICHER EINSTIEG ==================

        /// <summary>
        /// Spielt den Übergang. Der Aufrufer erledigt seine Arbeit parallel und
        /// ruft <see cref="ReleaseToMenu"/>, sobald das Ziel bereit ist.
        /// </summary>
        public static VaultEnterTransition Play(string playerName, string vaultLine, int onlineCount)
        {
            var host = new GameObject("~VaultEnter");
            DontDestroyOnLoad(host);
            var transition = host.AddComponent<VaultEnterTransition>();
            transition.Build(playerName, vaultLine, onlineCount);
            transition.StartCoroutine(transition.Run());
            return transition;
        }

        private IEnumerator Run()
        {
            yield return Phase(ApproachDuration, Approach);
            yield return Phase(UnlockDuration, Unlock);
            yield return Phase(OpenDuration, Open);
            yield return Phase(ArriveDuration, Arrive);

            // Phase 4 hält, bis der Aufrufer freigibt
            if (!released)
            {
                holdNote.gameObject.SetActive(true);
                float waited = 0f;
                while (!released)
                {
                    waited += Time.unscaledDeltaTime;
                    holdNote.alpha = Mathf.Clamp01((waited - 0.4f) / 0.6f);
                    yield return null;
                }
            }

            // 200 ms Überblendung ins Menü
            float fade = 0f;
            while (fade < 0.2f)
            {
                fade += Time.unscaledDeltaTime;
                rootGroup.alpha = 1f - Mathf.Clamp01(fade / 0.2f);
                yield return null;
            }
            Destroy(gameObject);
        }

        /// <summary>Führt eine Phase über ihre feste Dauer aus (unskalierte Zeit — Ladezeit darf nicht bremsen).</summary>
        private IEnumerator Phase(float duration, Action<float> apply)
        {
            float elapsed = 0f;
            apply(0f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                apply(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            apply(1f);
        }

        // ================== PHASEN ==================
        // depth 0 → .18 → .48 → 1     split 0 → 0 → .55 → 1     turn 0 → 300 → 420 → 460

        private void Approach(float p)
        {
            float settle = Enter(Seg(p, 0f, 0.40f));

            ApplyDepth(Drift(p) * 0.18f, 0f);
            ApplySeal(Mathf.Lerp(150f, 172f, Drift(p)), 0f, 0f,
                      lit: 0.1f + Mathf.Sin(Mathf.PI * p) * 0.1f, tumblerProgress: 0f);

            SetCaption("WELCOME BACK", playerLabel);
            captionGroup.alpha = settle * (1f - Enter(Seg(p, 0.66f, 0.94f)));
            captionRect.anchoredPosition = new Vector2(0f, CaptionY - Mathf.Lerp(20f, 0f, settle));
            pillGroup.alpha = Enter(Seg(p, 0.30f, 0.62f)) * (1f - Enter(Seg(p, 0.70f, 0.96f)));
        }

        private void Unlock(float p)
        {
            float tumblerProgress = Seg(p, 0.12f, 0.86f);
            float split = Enter(Seg(p, 0.66f, 1f)) * 0.55f;   // erst im letzten Drittel

            ApplyDepth(Mathf.Lerp(0.18f, 0.48f, Drift(p)), tumblerProgress * 0.4f);
            ApplySeal(Mathf.Lerp(172f, 210f, Drift(p)), Enter(p) * 300f, split,
                      lit: 0.2f + tumblerProgress * 0.4f + split * 0.5f, tumblerProgress: tumblerProgress);

            // Jedes Schloss klickt einmal — sechs Beats zum Mitzählen
            for (int i = 0; i < tumblerClicked.Length; i++)
            {
                if (tumblerClicked[i] || tumblerProgress <= (i / 6f)) continue;
                tumblerClicked[i] = true;
                SfxManager.SealUnlock();
                ScreenShake.Tick();
            }

            SetCaption("UNSEALING", "Six locks");
            captionGroup.alpha = Enter(Seg(p, 0.04f, 0.30f)) * (1f - Enter(Seg(p, 0.72f, 1f)));
            captionRect.anchoredPosition = new Vector2(0f, CaptionY);
            pillGroup.alpha = 0f;
        }

        private void Open(float p)
        {
            // Ease-IN, weil Phase 2 verzögernd endet — sonst ruckt es am Schnitt
            float rush = p * p;
            float flareAmount = Mathf.Sin(Mathf.PI * Seg(p, 0.10f, 0.70f));
            float split = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(rush / (0.72f * 0.72f)));

            ApplyDepth(Mathf.Lerp(0.48f, 1f, rush), 0.4f + flareAmount * 0.6f);
            ApplySeal(Mathf.Lerp(210f, 340f, rush), Mathf.Lerp(300f, 460f, Drift(p)), split,
                      lit: 0.7f + flareAmount * 0.3f, tumblerProgress: 1f);

            if (!sealBroken)
            {
                sealBroken = true;
                SfxManager.SealOpen();
                ScreenShake.Shake(0.020f, 0.55f, 18f);   // das Siegel bricht — länger und tiefer
            }

            captionGroup.alpha = 0f;
            float flareSize = 200f + flareAmount * 1500f;
            flare.rectTransform.sizeDelta = new Vector2(flareSize, flareSize);
            flare.color = Hex("#F8EED6", flareAmount * 0.5f);

            whiteOut.color = Hex("#F8EED6", Enter(Seg(p, 0.76f, 1f)) * 0.9f);
            if (!curtainFired && p >= 0.88f) { curtainFired = true; OnCurtainPeak?.Invoke(); }
        }

        private bool curtainFired;
        private bool settledFired;
        private bool sealBroken;
        private readonly bool[] tumblerClicked = new bool[6];

        private void Arrive(float p)
        {
            float wash = 1f - Enter(Seg(p, 0f, 0.36f));
            float appear = Enter(Seg(p, 0.20f, 0.56f));

            ApplyDepth(Mathf.Lerp(1f, 0.04f, Enter(Seg(p, 0f, 0.50f))), wash * 0.5f);
            sealRoot.gameObject.SetActive(false);
            flare.color = Hex("#F8EED6", 0f);
            whiteOut.color = Hex("#F8EED6", wash * 0.9f);

            lockupGroup.alpha = appear;
            lockupRect.anchoredPosition = new Vector2(0f, LockupY - Mathf.Lerp(28f, 0f, appear));
            lockupRect.localScale = Vector3.one * Mathf.Lerp(1.06f, 1f, appear);

            if (!settledFired && p >= 0.98f) { settledFired = true; OnArriveSettled?.Invoke(); }
        }

        // ================== ANWENDUNG AUF DIE BAUTEILE ==================

        private void ApplyDepth(float depth, float glow)
        {
            // Ringe und Webmuster laufen unterschiedlich schnell — das liest sich
            // als Tiefe statt als flacher Zoom.
            float scale = 1f + depth * 1.9f;
            float[] baseSizes = { 980f, 700f, 470f, 300f };
            for (int i = 0; i < ornaments.Length; i++)
            {
                float size = baseSizes[i] * scale;
                ornaments[i].sizeDelta = new Vector2(size, size);
                ornamentImages[i].color = Hex("#C8A45C", Mathf.Clamp01(0.16f - i * 0.025f + glow * 0.14f));
            }
            weave.localScale = Vector3.one * (1f + depth * 0.5f);

            float bloom = 300f * scale;
            centreBloom.rectTransform.sizeDelta = new Vector2(bloom, bloom);
            centreBloom.color = Hex("#EBCE8A", 0.05f + glow * 0.3f);
        }

        private void ApplySeal(float r, float turn, float split, float lit, float tumblerProgress)
        {
            float diameter = r * 2f;
            float gap = split * r * 1.5f;

            // Jede Hälfte ist ein Maskenrechteck von der Mitte nach aussen; der
            // Inhalt hängt an der Mitte, also wandert er beim Öffnen mit.
            leftClip.sizeDelta = new Vector2(r, diameter);
            leftClip.anchoredPosition = new Vector2(-gap, 0f);
            rightClip.sizeDelta = new Vector2(r, diameter);
            rightClip.anchoredPosition = new Vector2(gap, 0f);
            foreach (Transform clip in new[] { (Transform)leftClip, rightClip })
                ((RectTransform)clip.GetChild(0)).sizeDelta = new Vector2(diameter, diameter);

            for (int i = 0; i < reliefs.Length; i++)
            {
                float size = (i % 2 == 0) ? r * 1.1f : r * 0.6f;
                reliefs[i].sizeDelta = new Vector2(size, size);
            }

            sealGlow.rectTransform.sizeDelta = new Vector2(diameter * 2.1f, diameter * 2.1f);
            sealGlow.color = Hex("#EBCE8A", (0.08f + lit * 0.22f) * (1f - split * 0.6f));

            float ringSize = diameter * 1.18f;
            tumblerRing.sizeDelta = new Vector2(ringSize, ringSize);
            tumblerRing.localEulerAngles = new Vector3(0f, 0f, -turn);
            for (int i = 0; i < tumblers.Length; i++)
            {
                float size = r * 0.11f;
                tumblerRects[i].sizeDelta = new Vector2(size, size);
                float angle = i * 60f * Mathf.Deg2Rad;
                tumblerRects[i].anchoredPosition =
                    new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (r * 1.18f);
                bool on = tumblerProgress > (i / 6f);
                tumblers[i].color = on ? Hex("#EBCE8A", 1f) : Hex("#C8A45C", 0.28f);
            }

            float coreSize = r * 0.26f * (1f - split);
            core.sizeDelta = new Vector2(coreSize, coreSize);
            core.localEulerAngles = new Vector3(0f, 0f, 45f + turn);
            coreImage.color = Hex("#F8EED6", 1f - split);
        }

        // ================== AUFBAU ==================

        private const float CaptionY = 216f;   // Handoff: Block bei top:96 in 1280x720
        private const float LockupY = 69f;     // Handoff: Block bei top:148
        private string playerLabel;

        private void SetCaption(string eyebrowText, string headText)
        {
            if (eyebrow.text != eyebrowText) eyebrow.text = eyebrowText;
            if (headline.text != headText) headline.text = headText;
        }

        private void Build(string playerName, string vaultLine, int onlineCount)
        {
            skin = TransitionSkin.Load();
            if (skin == null)
            {
                Debug.LogError("[VaultEnter] TransitionSkin fehlt in Resources — Menü: Rouge TCG/Rebuild Transition Skin");
                skin = ScriptableObject.CreateInstance<TransitionSkin>();
            }
            playerLabel = string.IsNullOrEmpty(playerName) ? "Duelist" : playerName;

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;              // über allem, auch über Szenenwechseln
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            rootGroup = canvasGo.AddComponent<CanvasGroup>();
            rootGroup.blocksRaycasts = true;         // Eingaben sind gesperrt

            var canvasRect = (RectTransform)canvasGo.transform;

            // Vollflächige Schichten hängen direkt am Canvas, damit sie unabhängig
            // vom Seitenverhältnis wirklich alles abdecken.
            var backdrop = MakeImage("Backdrop", canvasRect, Hex("#0A0705", 1f));
            FullScreen(backdrop.rectTransform);

            // Bühne: alle Handoff-Werte sind 1280x720 — hier einmal hochskaliert
            stage = MakeRect("Stage", canvasRect);
            stage.localScale = Vector3.one * (1920f / BaseWidth);

            // Warmer Kern des Tresors (Handoff: Ellipse 1100x700, aussen #0A0705)
            var vaultGlow = MakeImage("VaultGlow", stage, Hex("#2A1C12", 1f));
            vaultGlow.sprite = skin.glow;
            vaultGlow.rectTransform.sizeDelta = new Vector2(1672f, 1064f);
            vaultGlow.rectTransform.anchoredPosition = new Vector2(0f, 14f);   // 48 % statt 50 %

            weave = MakeRect("Weave", stage);
            weave.sizeDelta = new Vector2(BaseWidth * 2f, BaseHeight * 2f);
            var weaveImage = weave.gameObject.AddComponent<Image>();
            weaveImage.sprite = skin.weave;
            weaveImage.type = Image.Type.Tiled;
            weaveImage.color = Hex("#C8A45C", 0.07f);
            weaveImage.raycastTarget = false;

            for (int i = 0; i < ornaments.Length; i++)
            {
                ornaments[i] = MakeRect("Ornament" + i, stage);
                ornaments[i].localEulerAngles = new Vector3(0f, 0f, 45f);
                ornamentImages[i] = ornaments[i].gameObject.AddComponent<Image>();
                ornamentImages[i].sprite = skin.frame;
                ornamentImages[i].type = Image.Type.Sliced;
                ornamentImages[i].raycastTarget = false;
            }

            centreBloom = MakeImage("CentreBloom", stage, Hex("#EBCE8A", 0.05f));
            centreBloom.sprite = skin.glow;

            BuildSeal();

            flare = MakeImage("Flare", stage, Hex("#F8EED6", 0f));
            flare.sprite = skin.flare;

            BuildCaptions(onlineCount);
            BuildLockup(vaultLine);

            whiteOut = MakeImage("WhiteOut", canvasRect, Hex("#F8EED6", 0f));
            FullScreen(whiteOut.rectTransform);
        }

        private void BuildSeal()
        {
            sealRoot = MakeRect("Seal", stage);

            sealGlow = MakeImage("SealGlow", sealRoot, Hex("#EBCE8A", 0.1f));
            sealGlow.sprite = skin.glow;

            // Der Schlossring liegt HINTER den Hälften — beim Öffnen schiebt sich
            // die Scheibe darüber, nicht umgekehrt.
            tumblerRing = MakeRect("TumblerRing", sealRoot);
            var ringImage = tumblerRing.gameObject.AddComponent<Image>();
            ringImage.sprite = skin.ring;
            ringImage.color = Hex("#C8A45C", 0.55f);
            ringImage.raycastTarget = false;

            for (int i = 0; i < tumblers.Length; i++)
            {
                tumblers[i] = MakeImage("Tumbler" + i, tumblerRing, Hex("#C8A45C", 0.28f));
                tumblers[i].sprite = skin.square;
                tumblerRects[i] = tumblers[i].rectTransform;
                tumblerRects[i].localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                var clip = MakeRect(left ? "LeftHalf" : "RightHalf", sealRoot);
                clip.pivot = new Vector2(left ? 1f : 0f, 0.5f);
                clip.gameObject.AddComponent<RectMask2D>();

                var body = MakeImage("Body", clip, Color.white);
                body.sprite = skin.seal;
                body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(left ? 1f : 0f, 0.5f);

                // Relief liegt IN der Hälfte, damit es beim Öffnen mitbricht
                var outer = MakeImage("ReliefOuter", body.rectTransform, Hex("#3B2A10", 0.9f));
                outer.sprite = skin.reliefOuter;
                outer.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                var inner = MakeImage("ReliefInner", body.rectTransform, Hex("#3B2A10", 0.9f));
                inner.sprite = skin.reliefInner;
                inner.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                reliefs[side * 2] = outer.rectTransform;
                reliefs[side * 2 + 1] = inner.rectTransform;

                if (left) leftClip = clip; else rightClip = clip;
            }

            coreImage = MakeImage("Core", sealRoot, Hex("#F8EED6", 1f));
            coreImage.sprite = skin.square;
            core = coreImage.rectTransform;
        }

        private void BuildCaptions(int onlineCount)
        {
            captionRect = MakeRect("Caption", stage);
            captionRect.sizeDelta = new Vector2(900f, 96f);
            captionRect.anchoredPosition = new Vector2(0f, CaptionY);
            captionGroup = captionRect.gameObject.AddComponent<CanvasGroup>();
            captionGroup.alpha = 0f;

            // Zeile mit den beiden auslaufenden Zierstrichen
            var eyebrowRow = MakeRect("EyebrowRow", captionRect);
            eyebrowRow.sizeDelta = new Vector2(900f, 20f);
            eyebrowRow.anchoredPosition = new Vector2(0f, 38f);
            // childControlWidth muss an: sonst nimmt die Gruppe die aktuelle Breite
            // der Kinder (0 beim frisch gebauten Text) und alles klebt in der Mitte.
            var layout = eyebrowRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            MakeRule(eyebrowRow, false);
            eyebrow = MakeText("Eyebrow", eyebrowRow, skin.oswald, 13f, Hex("#9C8A6A", 1f));
            eyebrow.text = "WELCOME BACK";
            eyebrow.characterSpacing = 38f;
            eyebrow.alignment = TextAlignmentOptions.Center;
            ((RectTransform)eyebrow.transform).sizeDelta = new Vector2(0f, 20f);
            MakeRule(eyebrowRow, true);

            headline = MakeText("Headline", captionRect, skin.cinzel, 50f, Hex("#F1DFB8", 1f));
            headline.text = playerLabel;
            headline.characterSpacing = 5f;
            headline.lineSpacing = 20f;      // Cinzel braucht Luft, sonst kappt das Q
            headline.alignment = TextAlignmentOptions.Center;
            PlaceStrip((RectTransform)headline.transform, 900f, 66f, -18f);

            // Die Pille sitzt eng um ihren Inhalt (Handoff: padding 10/20, gap 11)
            var pill = MakeImage("StatusPill", stage, new Color(0f, 0f, 0f, 0.45f));
            pill.sprite = skin.frame;
            pill.type = Image.Type.Sliced;
            pill.rectTransform.anchoredPosition = new Vector2(0f, -251f);   // Handoff: bottom 92
            pillGroup = pill.gameObject.AddComponent<CanvasGroup>();
            pillGroup.alpha = 0f;

            var pillLayout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            pillLayout.childAlignment = TextAnchor.MiddleCenter;
            pillLayout.spacing = 11f;
            pillLayout.padding = new RectOffset(20, 20, 10, 10);
            pillLayout.childControlWidth = pillLayout.childControlHeight = true;
            pillLayout.childForceExpandWidth = pillLayout.childForceExpandHeight = false;
            var pillFitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            pillFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pillFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var border = MakeImage("PillBorder", pill.rectTransform, Hex("#C8A45C", 0.32f));
            border.sprite = skin.frame;
            border.type = Image.Type.Sliced;
            FullScreen(border.rectTransform);
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var dot = MakeImage("Dot", pill.rectTransform, Hex("#7ACD96", 1f));
            dot.sprite = skin.square;
            dot.rectTransform.sizeDelta = new Vector2(7f, 7f);
            var dotElement = dot.gameObject.AddComponent<LayoutElement>();
            dotElement.preferredWidth = 7f;
            dotElement.preferredHeight = 7f;

            var pillText = MakeText("PillText", pill.rectTransform, skin.spectral, 14f, Hex("#9C8A6A", 1f));
            pillText.text = $"Seal verified · {Mathf.Max(1, onlineCount)} duelists inside";
            pillText.alignment = TextAlignmentOptions.Center;
        }

        private void MakeRule(RectTransform parent, bool mirrored)
        {
            var rule = MakeImage(mirrored ? "RuleRight" : "RuleLeft", parent, Hex("#C8A45C", 1f));
            rule.sprite = skin.rule;
            rule.rectTransform.sizeDelta = new Vector2(70f, 1f);
            var element = rule.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 70f;
            element.preferredHeight = 1f;
            // Der Verlauf läuft nach rechts hell aus — rechts also gespiegelt
            if (mirrored) rule.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        }

        private void BuildLockup(string vaultLine)
        {
            lockupRect = MakeRect("Lockup", stage);
            lockupRect.sizeDelta = new Vector2(900f, 286f);
            lockupRect.anchoredPosition = new Vector2(0f, LockupY);
            lockupGroup = lockupRect.gameObject.AddComponent<CanvasGroup>();
            lockupGroup.alpha = 0f;

            // Marke: drei ineinanderliegende gedrehte Quadrate
            var mark = MakeRect("Mark", lockupRect);
            mark.sizeDelta = new Vector2(120f, 120f);
            mark.anchoredPosition = new Vector2(0f, 83f);
            var markOuter = MakeImage("MarkOuter", mark, Hex("#C8A45C", 1f));
            markOuter.sprite = skin.reliefOuter;
            markOuter.rectTransform.sizeDelta = new Vector2(120f, 120f);
            markOuter.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            var markMid = MakeImage("MarkMid", mark, Hex("#EBCE8A", 0.85f));
            markMid.sprite = skin.reliefInner;
            markMid.rectTransform.sizeDelta = new Vector2(66f, 66f);
            markMid.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            var markGem = MakeImage("MarkGem", mark, Hex("#F8EED6", 1f));
            markGem.sprite = skin.square;
            markGem.rectTransform.sizeDelta = new Vector2(28f, 28f);
            markGem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            var wordmark = MakeText("Wordmark", lockupRect, skin.cinzel, 78f, Hex("#EBCE8A", 1f));
            wordmark.text = "RELIQUARY";
            wordmark.characterSpacing = 9f;
            wordmark.lineSpacing = 20f;
            wordmark.alignment = TextAlignmentOptions.Center;
            wordmark.enableVertexGradient = true;
            wordmark.colorGradient = new VertexGradient(
                Hex("#F8EED6", 1f), Hex("#F8EED6", 1f), Hex("#A6802F", 1f), Hex("#A6802F", 1f));
            PlaceStrip((RectTransform)wordmark.transform, 900f, 100f, -50f);

            var lockupLine = MakeText("VaultLine", lockupRect, skin.spectral, 20f, Hex("#A2917A", 1f));
            lockupLine.text = vaultLine ?? "";
            lockupLine.alignment = TextAlignmentOptions.Center;
            PlaceStrip((RectTransform)lockupLine.transform, 900f, 28f, -133f);

            holdNote = MakeText("HoldNote", lockupRect, skin.spectral, 16f, Hex("#8C7B5F", 1f));
            holdNote.text = "Still opening…";
            holdNote.alignment = TextAlignmentOptions.Center;
            PlaceStrip((RectTransform)holdNote.transform, 900f, 24f, -171f);
            holdNote.alpha = 0f;
            holdNote.gameObject.SetActive(false);
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
