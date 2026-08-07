using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Das Siegessiegel (Handoff „Cosmetics", Abschnitt 6). Der Stempel, der auf
    /// dem Sieg-Bildschirm landet — beide Spieler sehen ihn, es ist damit die
    /// sozialste Kosmetik im Spiel und die einzige mit echter Bewegung.
    ///
    /// Die fünf Siegel unterscheiden sich darin, <b>wie sie ankommen</b>, und nur
    /// darin. Das ist der ganze Witz: der Gegner soll an den ersten Bildern
    /// erkennen, welches Siegel man trägt.
    ///
    ///   Vanilla 2.2 — das Grundsiegel, das jeder hat und niemand kauft
    ///   Brand   2.6 — eingebrannt. Nichts fliegt, die Hitze macht die Arbeit
    ///   Shatter 2.4 — landet ganz und bricht dann in drei Teile
    ///   Bloom   2.6 — vier Rauten öffnen sich versetzt. Das leise
    ///   Verdict 2.4 — von oben geschlagen und in den Tisch getrieben
    ///   Eclipse 2.8 — eine dunkle Scheibe zieht über eine helle, aussermittig
    ///
    /// Jedes Siegel ist bei <b>72 % der Szene fertig</b> und hält danach still —
    /// dieses Standbild ist das, was der Spieler abfotografiert, also muss es das
    /// Halten wert sein.
    ///
    /// Die Maße sind hergeleitet, nicht gewählt: das vom Text freigelassene Band
    /// läuft von y 169 bis y 526, Mitte also 348, halbe Höhe 178. Eine um 45°
    /// gedrehte Raute der Seite r ist 0.707·r halbhoch; Blooms weitester Ring
    /// (0.987 r) gibt 0.698·r ≤ 178, also r ≤ 254. <see cref="R"/> = 236 lässt Luft.
    ///
    /// Alles ist deterministisch — kein Random. Ein aufgezeichneter Sieg-Bildschirm
    /// spielt identisch wieder ab.
    /// </summary>
    public class VictorySealSequence : MonoBehaviour
    {
        public enum Seal { Vanilla, Brand, Shatter, Bloom, Verdict, Eclipse }

        private const float W = 1280f, H = 720f;
        private const float CY = 348f;      // Siegelmitte von oben
        private const float R = 236f;       // Siegelradius, siehe Klassenkommentar

        /// <summary>Szenenlänge je Siegel, in der Reihenfolge von <see cref="Seal"/>.</summary>
        private static readonly float[] Durations = { 2.2f, 2.6f, 2.4f, 2.6f, 2.4f, 2.8f };

        private static readonly Color Gold = Hex("#C8A45C");
        private static readonly Color Light = Hex("#EBCE8A");
        private static readonly Color Pale = Hex("#F8EED6");
        private static readonly Color Dark = Hex("#7A5A1E");
        private static readonly Color Teal = Hex("#8FC6D2");
        private static readonly Color TealLit = Hex("#DFF4F8");
        private static readonly Color Violet = Hex("#B9A3E0");
        private static readonly Color VioletLit = Hex("#EFE7FA");
        private static readonly Color EmberLit = Hex("#F3C3A6");
        private static readonly Color Brandy = Hex("#7E4A20");
        private static readonly Color BrandLit = Hex("#C8894E");
        private static readonly Color Muted = Hex("#A2917A");

        public static bool Playing { get; private set; }

        private static VictorySealSequence instance;

        private CanvasGroup group;
        private RectTransform stage, sealRoot;
        private TransitionSkin skin;

        private Image tableGlow, weave, vignette, flash, blackout;

        // --- Brand ---
        private Image brandScorch, brandFill, brandCore;
        private Outline brandPlate;
        private readonly List<Image> brandEmbers = new List<Image>();

        // --- Shatter ---
        private Outline shatterPlate;
        private Image shatterCore;
        private readonly List<RectTransform> shatterCracks = new List<RectTransform>();
        private readonly List<Image> shatterCrackImages = new List<Image>();
        private readonly List<Image> shatterShards = new List<Image>();

        // --- Bloom ---
        private readonly List<Outline> bloomRings = new List<Outline>();
        private Image bloomCore, bloomHalo;

        // --- Verdict ---
        private readonly List<RectTransform> verdictLines = new List<RectTransform>();
        private readonly List<Image> verdictLineImages = new List<Image>();
        private Image verdictHit, verdictPlate, verdictSheen, verdictInner, verdictPip;

        // --- Eclipse ---
        private Image eclipseBright, eclipseDark, eclipseRim, eclipseSpark;

        // --- Vanilla ---
        private Outline vanillaPlate;
        private Image vanillaCore, vanillaRing;

        // --- Banner ---
        private RectTransform bannerTop, bannerBottom;
        private CanvasGroup bannerGroup;
        private TMP_Text headline, eyebrow, chipLabel, note;
        private Image chipPlate, chipPip, ruleLeft, ruleRight;

        private Seal seal = Seal.Vanilla;
        private bool asOpponent;   // das Siegel des Gegners auf dem Niederlage-Bildschirm
        private Action finished;
        private string opponent = "your opponent";
        private int turn = 1, rpDelta;

        // ================== START ==================

        /// <summary>
        /// Spielt das ausgerüstete Siegel. Eine unbekannte Id fällt still auf das
        /// Grundsiegel zurück — nie ein Platzhalter, nie ein Ladekringel.
        /// </summary>
        /// <summary>
        /// Dasselbe Siegel auf dem Niederlage-Bildschirm — das des <b>Gegners</b>.
        /// Der Handoff verlangt „both players see it": ein Siegel ist die
        /// Unterschrift unter einen Sieg, und eine Unterschrift, die nur der
        /// Unterzeichnende sieht, ist keine. Es läuft <b>nach</b> der
        /// Niederlage-Sequenz, nicht über ihr — sonst streiten sich zwei Abspänne
        /// um dieselbe Sekunde.
        /// </summary>
        public static void PlayForLoser(string sealId, string winnerName, int turnNumber, Action onDone = null)
        {
            Play(sealId, winnerName, turnNumber, 0, onDone, true);
        }

        public static void Play(string sealId, string opponentName, int turnNumber,
                                int rpChange, Action onDone = null, bool asOpponent = false)
        {
            if (instance == null)
            {
                var host = new GameObject("~VictorySeal");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<VictorySealSequence>();
                instance.Build();
            }
            instance.StartSequence(FromId(sealId), opponentName, turnNumber, rpChange, onDone, asOpponent);
        }

        private static Seal FromId(string id)
        {
            switch ((id ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "brand": return Seal.Brand;
                case "shatter": return Seal.Shatter;
                case "bloom": return Seal.Bloom;
                case "verdict": return Seal.Verdict;
                case "eclipse": return Seal.Eclipse;
                default: return Seal.Vanilla;
            }
        }

        private void StartSequence(Seal which, string opponentName, int turnNumber,
                                   int rpChange, Action onDone, bool foeSeal)
        {
            StopAllCoroutines();
            seal = which;
            asOpponent = foeSeal;
            finished = onDone;
            opponent = string.IsNullOrEmpty(opponentName) ? "your opponent" : opponentName;
            turn = Mathf.Max(1, turnNumber);
            rpDelta = rpChange;
            gameObject.SetActive(true);
            StartCoroutine(Run());
        }

        // ================== ABLAUF ==================

        private IEnumerator Run()
        {
            Playing = true;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            SetupBannerText();

            float duration = Durations[(int)seal];
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                Frame(Mathf.Clamp01(t / duration));
                // Überspringen führt an das Standbild, nie mitten in den Einschlag
                if (SkipPressed()) break;
                yield return null;
            }
            Frame(1f);

            gameObject.SetActive(false);
            Playing = false;
            var callback = finished;
            finished = null;
            callback?.Invoke();
        }

        private static bool SkipPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame
                || keyboard.escapeKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame)) return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private void Frame(float p)
        {
            Clear();

            // Das Siegel ist bei 72 % fertig und hält danach — das Standbild ist
            // das Bild, das der Spieler behält.
            float t = Mathf.Clamp01(p / 0.72f);

            Color tone = StageTone();
            tableGlow.color = Motion.Alpha(tone, 0.13f + Motion.Enter(Motion.Seg(p, 0f, 0.5f)) * 0.1f);
            flash.color = Motion.Alpha(Pale, Motion.Arc(p, 0.12f, 0.44f) * 0.34f);
            blackout.color = new Color(0.039f, 0.027f, 0.02f, Motion.Enter(Motion.Seg(p, 0.94f, 1f)));

            switch (seal)
            {
                case Seal.Brand: DrawBrand(t); break;
                case Seal.Shatter: DrawShatter(t); break;
                case Seal.Bloom: DrawBloom(t); break;
                case Seal.Verdict: DrawVerdict(t); break;
                case Seal.Eclipse: DrawEclipse(t); break;
                default: DrawVanilla(t); break;
            }

            DrawBanner(p);
        }

        private Color StageTone()
        {
            switch (seal)
            {
                case Seal.Brand: return Brandy;
                case Seal.Shatter: return Teal;
                case Seal.Bloom: return Violet;
                default: return Gold;
            }
        }

        private Color SealTone()
        {
            switch (seal)
            {
                case Seal.Brand: return BrandLit;
                case Seal.Shatter: return Teal;
                case Seal.Bloom: return Violet;
                case Seal.Verdict: return Light;
                default: return Pale;
            }
        }

        private void Clear()
        {
            brandScorch.gameObject.SetActive(false);
            brandFill.gameObject.SetActive(false);
            brandCore.gameObject.SetActive(false);
            brandPlate.SetActive(false);
            foreach (var ember in brandEmbers) ember.gameObject.SetActive(false);

            shatterPlate.SetActive(false);
            shatterCore.gameObject.SetActive(false);
            foreach (var crack in shatterCracks) crack.gameObject.SetActive(false);
            foreach (var shard in shatterShards) shard.gameObject.SetActive(false);

            foreach (var ring in bloomRings) ring.SetActive(false);
            bloomCore.gameObject.SetActive(false);
            bloomHalo.gameObject.SetActive(false);

            foreach (var line in verdictLines) line.gameObject.SetActive(false);
            verdictHit.gameObject.SetActive(false);
            verdictPlate.gameObject.SetActive(false);
            verdictSheen.gameObject.SetActive(false);
            verdictInner.gameObject.SetActive(false);
            verdictPip.gameObject.SetActive(false);

            eclipseBright.gameObject.SetActive(false);
            eclipseDark.gameObject.SetActive(false);
            eclipseRim.gameObject.SetActive(false);
            eclipseSpark.gameObject.SetActive(false);

            vanillaPlate.SetActive(false);
            vanillaCore.gameObject.SetActive(false);
            vanillaRing.gameObject.SetActive(false);
        }

        // ================== DIE SECHS SIEGEL ==================

        /// <summary>
        /// Grundsiegel. Eine Raute, ein Ring, ein Kern — es kommt an und bleibt.
        /// Es ist absichtlich das langweiligste: daran misst sich, ob ein gekauftes
        /// Siegel sein Geld wert ist.
        /// </summary>
        private void DrawVanilla(float t)
        {
            float land = Motion.Pop(Motion.Seg(t, 0f, 0.44f));
            float ring = Motion.Enter(Motion.Seg(t, 0.3f, 0.9f));

            vanillaPlate.Set(R * Mathf.Clamp01(land), R * 0.031f, Motion.Alpha(Gold, Motion.Enter(Motion.Seg(t, 0f, 0.2f))), 45f);
            Place(vanillaCore, 0f, 0f, R * 0.24f * Mathf.Clamp01(Motion.Pop(Motion.Seg(t, 0.2f, 0.7f))),
                  R * 0.24f * Mathf.Clamp01(Motion.Pop(Motion.Seg(t, 0.2f, 0.7f))), 45f);
            vanillaCore.color = Light;
            Place(vanillaRing, 0f, 0f, R * 1.22f * Motion.Mix(0.7f, 1f, ring), R * 1.22f * Motion.Mix(0.7f, 1f, ring));
            vanillaRing.color = Motion.Alpha(Gold, 0.4f * ring);
        }

        /// <summary>
        /// Brand — common. Eingebrannt: nichts reist an, die Hitze macht die Arbeit.
        /// Ein Sengfleck breitet sich aus, die Marke verkohlt, der Rand kühlt von
        /// Weiss über Ember nach Eisen ab. Das einzige Siegel ohne Aufschlag.
        /// </summary>
        private void DrawBrand(float t)
        {
            float spread = Motion.Enter(Motion.Seg(t, 0f, 0.44f));
            float heat = Motion.Arc(t, 0.06f, 0.72f);
            float cool = Motion.Enter(Motion.Seg(t, 0.5f, 1f));
            Color rim = cool < 0.25f ? Pale : cool < 0.5f ? EmberLit : Motion.Mix(EmberLit, Brandy, Motion.Seg(cool, 0.5f, 1f));

            Place(brandScorch, 0f, 0f, R * 1.44f * spread, R * 1.44f * spread);
            brandScorch.color = Motion.Alpha(Brandy, 0.6f * (1f - cool * 0.4f) * spread);

            float mark = Motion.Mix(0.7f, 1f, Motion.Enter(Motion.Seg(t, 0.04f, 0.5f)));
            float appear = Motion.Enter(Motion.Seg(t, 0.02f, 0.3f));
            Place(brandFill, 0f, 0f, R * mark, R * mark, 45f);
            brandFill.color = new Color(0.102f, 0.059f, 0.024f, appear);
            brandPlate.Set(R * mark, R * 0.056f, Motion.Alpha(rim, appear), 45f);

            // Die Glut sitzt im Rand, nicht hinter der Marke — darum ein zweiter,
            // weicher Schein direkt auf der Rautengrösse
            brandPlate.SetGlow(R * mark * 1.3f, Motion.Alpha(EmberLit, heat * 0.5f));

            float coreSize = R * 0.386f * Motion.Enter(Motion.Seg(t, 0.16f, 0.46f));
            Place(brandCore, 0f, 0f, coreSize, coreSize, 45f);
            brandCore.color = cool > 0.6f ? BrandLit : EmberLit;

            var offsets = new[] { -0.30f, 0.30f, -0.14f, 0.14f };
            var sizes = new[] { 0.055f, 0.045f, 0.04f, 0.05f };
            for (int i = 0; i < brandEmbers.Count; i++)
            {
                float phase = Motion.Seg(t, 0.2f + i * 0.08f, 0.9f);
                float size = R * sizes[i];
                Place(brandEmbers[i], R * offsets[i], R * 0.28f - phase * R * 0.38f, size, size, 45f);
                brandEmbers[i].color = Motion.Alpha(BrandLit, Mathf.Sin(Mathf.PI * phase) * 0.7f);
            }
        }

        /// <summary>
        /// Shatter — rare. Landet ganz, dann brechen drei Risse hindurch und die
        /// Hälften schieben sich gerade so weit auseinander, dass es als zerbrochen
        /// liest. Die Risse sind gezeichnet, nicht als Bruchstücke animiert — das
        /// hält es billig genug für den Sieg-Bildschirm.
        /// </summary>
        private void DrawShatter(float t)
        {
            float land = Motion.Pop(Motion.Seg(t, 0f, 0.3f));
            float part = Motion.Enter(Motion.Seg(t, 0.46f, 1f)) * R * 0.045f;

            shatterPlate.Set(R * Motion.Mix(1.5f, 1f, Mathf.Clamp01(land)), R * 0.031f,
                             Motion.Alpha(Teal, Motion.Seg(t, 0f, 0.14f)), 45f);
            shatterPlate.SetGlow(R * 1.2f, Motion.Alpha(Teal, 0.28f * Motion.Seg(t, 0f, 0.14f)));

            float[] angles = { 28f, -52f, 78f };
            float[] widths = { 1.56f, 1.56f, 1.25f };
            float[] delays = { 0.30f, 0.40f, 0.52f };
            for (int i = 0; i < shatterCracks.Count; i++)
            {
                float grow = Motion.Enter(Motion.Seg(t, delays[i], delays[i] + 0.3f));
                if (grow <= 0f) continue;
                float length = R * widths[i] * grow;
                float sign = i % 2 == 1 ? -1f : 1f;
                // CSS: rotate(a) translate(x,y) — der Versatz liegt im gedrehten
                // Bezugssystem, wird also mitgedreht
                var shift = RotateCss(new Vector2(part * sign, part * -sign), angles[i]);
                // Der Kasten wächst nach rechts aus einer festen linken Kante
                float centre = R * widths[i] * (grow - 1f) * 0.5f;
                var move = RotateCss(new Vector2(centre, 0f), angles[i]) + shift;
                Place(shatterCracks[i], move.x, move.y, length, R * (i == 2 ? 0.0156f : 0.021f), angles[i]);
                var glint = Motion.Alpha(TealLit, 0.9f * grow);
                shatterCrackImages[i * 2].color = glint;
                shatterCrackImages[i * 2 + 1].color = glint;
            }

            float core = R * 0.208f * Motion.Mix(0.4f, 1f, Mathf.Clamp01(Motion.Pop(Motion.Seg(t, 0.28f, 0.62f))));
            Place(shatterCore, 0f, 0f, core, core, 45f);
            shatterCore.color = TealLit;

            for (int i = 0; i < shatterShards.Count; i++)
            {
                float phase = Motion.Seg(t, 0.42f + (i % 3) * 0.06f, 1f);
                float angle = i * 61f * Mathf.Deg2Rad;
                float distance = R * (0.34f + phase * 0.34f);
                float size = R * 0.035f;
                Place(shatterShards[i], Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, size, size, 45f);
                shatterShards[i].color = Motion.Alpha(TealLit, Mathf.Sin(Mathf.PI * phase) * 0.8f);
            }
        }

        /// <summary>
        /// Bloom — epic. Vier Rauten öffnen sich versetzt nach aussen und legen
        /// sich hin. <b>Kein Aufschlag, kein Überschwingen, kein Blitz</b> — jedes
        /// andere Siegel hat einen Treffer, dieses ist die Wahl für alle, die
        /// keinen wollen.
        /// </summary>
        private void DrawBloom(float t)
        {
            float[] sizes = { 0.267f, 0.507f, 0.747f, 0.987f };
            float[] delays = { 0f, 0.12f, 0.24f, 0.36f };
            float[] opacity = { 0.95f, 0.62f, 0.36f, 0.16f };
            float[] stroke = { 0.0133f, 0.0133f, 0.010f, 0.0067f };

            for (int i = 0; i < bloomRings.Count; i++)
            {
                float ease = Motion.Drift(Motion.Seg(t, delays[i], delays[i] + 0.5f));
                float size = R * sizes[i] * Motion.Mix(0.3f, 1f, ease);
                bloomRings[i].Set(size, Mathf.Max(1f, R * stroke[i]),
                                  Motion.Alpha(VioletLit, opacity[i] * ease), 45f);
            }

            float core = R * 0.24f * Motion.Mix(0.2f, 1f, Motion.Drift(Motion.Seg(t, 0f, 0.4f)));
            Place(bloomCore, 0f, 0f, core, core, 45f);
            bloomCore.color = VioletLit;

            Place(bloomHalo, 0f, 0f, R * 1.2f, R * 1.2f);
            bloomHalo.color = Motion.Alpha(Violet, 0.22f * Motion.Drift(Motion.Seg(t, 0.1f, 0.7f)));
        }

        /// <summary>
        /// Verdict — epic. Von oben geschlagen: fünf Linien fahren herab, die
        /// Platte schnappt mit Überschwingen ein, und ein flacher Ring läuft über
        /// den Tisch davon. Der Ring ist flach (Seitenverhältnis 0.26), damit er
        /// als über den Tisch laufend liest und nicht als Kugel durch die Luft.
        /// </summary>
        private void DrawVerdict(float t)
        {
            float fall = Motion.Enter(Motion.Seg(t, 0f, 0.28f));
            float snap = Motion.Pop(Motion.Seg(t, 0.24f, 0.56f));
            float hit = Motion.Seg(t, 0.26f, 0.72f);
            float[] angles = { 0f, -26f, 26f, -48f, 48f };

            for (int i = 0; i < verdictLines.Count; i++)
            {
                float grow = Motion.Enter(Motion.Seg(t, i * 0.03f, 0.3f + i * 0.03f));
                float length = R * 0.49f * grow;
                if (length <= 0.5f) continue;
                // Ursprung oben: die Linie hängt an y = −0.62 R und wächst nach unten
                var head = RotateCss(new Vector2(0f, length * 0.5f), angles[i]);
                Place(verdictLines[i], head.x, -R * 0.62f + head.y,
                      Mathf.Max(1.5f, R * (i < 3 ? 0.0067f : 0.005f)), length, angles[i]);
                verdictLineImages[i].color = Motion.Alpha(i < 3 ? Pale : Light,
                    (i < 3 ? 1f : 0.6f) * (1f - Motion.Enter(Motion.Seg(t, 0.6f, 1f)) * 0.7f));
            }

            if (hit > 0f && hit < 1f)
            {
                Place(verdictHit, 0f, 0f, R * 2f * hit, R * 0.52f * hit);
                verdictHit.color = Motion.Alpha(Light, (1f - hit) * 0.75f);
            }

            float plate = R * Mathf.Clamp01(snap);
            float lift = Motion.Mix(-R * 0.3f, 0f, fall);
            float appear = Motion.Enter(Motion.Seg(t, 0.2f, 0.34f));
            Place(verdictPlate, 0f, lift, plate, plate, 45f);
            verdictPlate.color = Motion.Alpha(Dark, appear);
            Place(verdictSheen, 0f, lift, plate, plate, 45f);
            verdictSheen.color = Motion.Alpha(Pale, appear);

            float inner = R * 0.566f * Mathf.Clamp01(snap);
            Place(verdictInner, 0f, lift, inner, inner, 45f);
            verdictInner.color = new Color(0.102f, 0.071f, 0.024f, appear);

            float pip = R * 0.196f * Mathf.Clamp01(Motion.Pop(Motion.Seg(t, 0.4f, 0.72f)));
            Place(verdictPip, 0f, lift, pip, pip, 45f);
            verdictPip.color = Pale;
        }

        /// <summary>
        /// Eclipse — relic. Eine helle Scheibe, dann schiebt sich eine dunkle
        /// darüber und bleibt aussermittig stehen; übrig bleiben eine brennende
        /// Sichel und ein Funke auf der belichteten Kante.
        /// <b>Das einzige Siegel, das absichtlich unsymmetrisch endet</b> — das ist
        /// die Komposition, nicht ein Fehler.
        /// </summary>
        private void DrawEclipse(float t)
        {
            float born = Motion.Enter(Motion.Seg(t, 0f, 0.24f));
            float slide = Motion.Drift(Motion.Seg(t, 0.2f, 0.74f));
            float rim = Motion.Enter(Motion.Seg(t, 0.5f, 0.9f));
            float d = R * 1.04f;

            Place(eclipseBright, 0f, 0f, d * born, d * born);
            eclipseBright.color = Motion.Alpha(Light, born);

            Place(eclipseDark, Motion.Mix(-d * 1.5f, d * 0.10f, slide), 0f, d * 0.98f, d * 0.98f);
            eclipseDark.color = new Color(0.016f, 0.012f, 0.008f, born);

            Place(eclipseRim, 0f, 0f, R * 1.26f, R * 1.26f);
            eclipseRim.color = Motion.Alpha(Pale, 0.42f * rim);

            Place(eclipseSpark, -R * 0.60f, 0f, R * 0.1f, R * 0.1f, 45f);
            eclipseSpark.color = Motion.Alpha(Pale, rim);
        }

        // ================== SIEG-BILDSCHIRM-TEXT ==================

        private void SetupBannerText()
        {
            Color tone = SealTone();
            // Auf dem Niederlage-Bildschirm gehört die Bühne dem Gegner: sein
            // Siegel, sein Name. Wer verliert, soll sehen, womit unterschrieben
            // wurde — sonst wäre die halbe Kosmetik unsichtbar.
            // Der Sieger liest VICTORY, der Verlierer LOSS — dasselbe Siegel,
            // zwei Wahrheiten. Wessen Zeichen es ist, sagt die Zeile darüber.
            headline.text = asOpponent ? "LOSS" : "VICTORY";
            eyebrow.text = asOpponent
                ? $"{opponent.ToUpperInvariant()} SEALED THE DUEL"
                : "THE VAULT REMEMBERS YOUR NAME";
            eyebrow.color = tone;
            chipLabel.text = asOpponent
                ? $"TURN {turn}"
                : rpDelta > 0
                    ? $"TURN {turn}  ·  +{rpDelta} RP"
                    : $"SEALED ON TURN {turn}";
            chipLabel.color = tone;
            chipPip.color = tone;
            chipPlate.color = new Color(0f, 0f, 0f, 0.5f);
            ruleLeft.color = Motion.Alpha(tone, 0.85f);
            ruleRight.color = Motion.Alpha(tone, 0.85f);
            note.text = asOpponent ? "Their mark on the board." : $"You defeated {opponent}.";
            note.color = Muted;
        }

        private void DrawBanner(float p)
        {
            float rise = Motion.Enter(Motion.Seg(p, 0.1f, 0.4f));
            float outro = 1f - Motion.Enter(Motion.Seg(p, 0.94f, 1f));
            bannerGroup.alpha = rise * outro;
            bannerTop.anchoredPosition = new Vector2(0f, -62f - Motion.Mix(14f, 0f, rise));
        }

        // ================== AUFBAU ==================

        private void Build()
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            stage = (RectTransform)canvasGo.transform;

            tableGlow = Make("TableGlow", stage, Motion.Alpha(Gold, 0.13f));
            tableGlow.sprite = skin.glow;
            Stretch(tableGlow.rectTransform, -40f);
            weave = Make("Weave", stage, Motion.Alpha(Gold, 0.04f));
            weave.sprite = skin.weave; weave.type = Image.Type.Tiled;
            Stretch(weave.rectTransform);
            vignette = Make("Vignette", stage, Color.black);
            vignette.sprite = skin.vignette;
            Stretch(vignette.rectTransform);

            sealRoot = MakeRect("Seal", stage);
            sealRoot.anchorMin = sealRoot.anchorMax = new Vector2(0.5f, 1f);
            sealRoot.anchoredPosition = new Vector2(0f, -CY);

            BuildBrand();
            BuildShatter();
            BuildBloom();
            BuildVerdict();
            BuildEclipse();
            BuildVanilla();
            BuildBanner();

            flash = Make("Flash", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(flash.rectTransform);
            blackout = Make("Blackout", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(blackout.rectTransform);

            gameObject.SetActive(false);
        }

        private void BuildBrand()
        {
            brandScorch = Make("BrandScorch", sealRoot, Color.clear);
            brandScorch.sprite = skin.glow;
            brandFill = Make("BrandFill", sealRoot, Color.clear);
            brandFill.sprite = skin.square;
            brandPlate = Outline.Build("BrandPlate", sealRoot, skin);
            brandCore = Make("BrandCore", sealRoot, Color.clear);
            brandCore.sprite = skin.square;
            for (int i = 0; i < 4; i++)
            {
                var ember = Make("BrandEmber" + i, sealRoot, Color.clear);
                ember.sprite = skin.square;
                brandEmbers.Add(ember);
            }
        }

        private void BuildShatter()
        {
            shatterPlate = Outline.Build("ShatterPlate", sealRoot, skin);
            for (int i = 0; i < 3; i++)
            {
                // Ein Riss verläuft an BEIDEN Enden ins Nichts. RuleFade ist ein
                // einseitiger Verlauf, also zwei Hälften — die rechte gespiegelt.
                var crack = MakeRect("ShatterCrack" + i, sealRoot);
                var left = Make("A", crack, Color.clear);
                left.sprite = skin.rule;
                left.rectTransform.anchorMin = new Vector2(0f, 0f);
                left.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                left.rectTransform.offsetMin = Vector2.zero;
                left.rectTransform.offsetMax = Vector2.zero;
                var right = Make("B", crack, Color.clear);
                right.sprite = skin.rule;
                right.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                right.rectTransform.anchorMax = new Vector2(1f, 1f);
                right.rectTransform.offsetMin = Vector2.zero;
                right.rectTransform.offsetMax = Vector2.zero;
                right.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                shatterCracks.Add(crack);
                shatterCrackImages.Add(left);
                shatterCrackImages.Add(right);
            }
            shatterCore = Make("ShatterCore", sealRoot, Color.clear);
            shatterCore.sprite = skin.square;
            for (int i = 0; i < 6; i++)
            {
                var shard = Make("ShatterShard" + i, sealRoot, Color.clear);
                shard.sprite = skin.square;
                shatterShards.Add(shard);
            }
        }

        private void BuildBloom()
        {
            bloomHalo = Make("BloomHalo", sealRoot, Color.clear);
            bloomHalo.sprite = skin.glow;
            for (int i = 0; i < 4; i++) bloomRings.Add(Outline.Build("BloomRing" + i, sealRoot, skin));
            bloomCore = Make("BloomCore", sealRoot, Color.clear);
            bloomCore.sprite = skin.square;
        }

        private void BuildVerdict()
        {
            for (int i = 0; i < 5; i++)
            {
                var line = Make("VerdictLine" + i, sealRoot, Color.clear);
                line.sprite = skin.fade;
                verdictLines.Add(line.rectTransform);
                verdictLineImages.Add(line);
            }
            verdictHit = Make("VerdictHit", sealRoot, Color.clear);
            verdictHit.sprite = skin.ring;
            verdictPlate = Make("VerdictPlate", sealRoot, Color.clear);
            verdictPlate.sprite = skin.square;
            verdictSheen = Make("VerdictSheen", sealRoot, Color.clear);
            verdictSheen.sprite = skin.diagFade;
            verdictInner = Make("VerdictInner", sealRoot, Color.clear);
            verdictInner.sprite = skin.square;
            verdictPip = Make("VerdictPip", sealRoot, Color.clear);
            verdictPip.sprite = skin.square;
        }

        private void BuildEclipse()
        {
            eclipseBright = Make("EclipseBright", sealRoot, Color.clear);
            eclipseBright.sprite = skin.seal;
            eclipseDark = Make("EclipseDark", sealRoot, Color.clear);
            eclipseDark.sprite = skin.seal;
            eclipseRim = Make("EclipseRim", sealRoot, Color.clear);
            eclipseRim.sprite = skin.ring;
            eclipseSpark = Make("EclipseSpark", sealRoot, Color.clear);
            eclipseSpark.sprite = skin.square;
        }

        private void BuildVanilla()
        {
            vanillaRing = Make("VanillaRing", sealRoot, Color.clear);
            vanillaRing.sprite = skin.ring;
            vanillaPlate = Outline.Build("VanillaPlate", sealRoot, skin);
            vanillaCore = Make("VanillaCore", sealRoot, Color.clear);
            vanillaCore.sprite = skin.square;
        }

        private void BuildBanner()
        {
            var root = MakeRect("Banner", stage);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;
            bannerGroup = root.gameObject.AddComponent<CanvasGroup>();

            bannerTop = MakeRect("Top", root);
            bannerTop.anchorMin = bannerTop.anchorMax = new Vector2(0.5f, 1f);
            bannerTop.pivot = new Vector2(0.5f, 1f);
            bannerTop.sizeDelta = new Vector2(W, 140f);

            var eyebrowRow = MakeRect("EyebrowRow", bannerTop);
            eyebrowRow.anchorMin = eyebrowRow.anchorMax = new Vector2(0.5f, 1f);
            eyebrowRow.pivot = new Vector2(0.5f, 1f);
            eyebrowRow.sizeDelta = new Vector2(W, 16f);
            eyebrow = Text("Eyebrow", eyebrowRow, skin.oswald, 13f, TextAlignmentOptions.Center, Gold);
            eyebrow.characterSpacing = 42f;
            eyebrow.rectTransform.sizeDelta = new Vector2(700f, 18f);
            ruleLeft = Make("RuleLeft", eyebrowRow, Gold);
            ruleLeft.sprite = skin.rule;
            ruleLeft.rectTransform.sizeDelta = new Vector2(72f, 1f);
            ruleLeft.rectTransform.anchoredPosition = new Vector2(-430f, 0f);
            ruleRight = Make("RuleRight", eyebrowRow, Gold);
            ruleRight.sprite = skin.rule;
            ruleRight.rectTransform.sizeDelta = new Vector2(72f, 1f);
            ruleRight.rectTransform.anchoredPosition = new Vector2(430f, 0f);

            headline = Text("Headline", bannerTop, skin.cinzel, 66f, TextAlignmentOptions.Center, Pale);
            headline.fontStyle = FontStyles.Bold;
            headline.characterSpacing = 7f;
            headline.rectTransform.anchorMin = headline.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            headline.rectTransform.pivot = new Vector2(0.5f, 1f);
            headline.rectTransform.sizeDelta = new Vector2(W, 92f);
            headline.rectTransform.anchoredPosition = new Vector2(0f, -30f);

            bannerBottom = MakeRect("Bottom", root);
            bannerBottom.anchorMin = bannerBottom.anchorMax = new Vector2(0.5f, 0f);
            bannerBottom.pivot = new Vector2(0.5f, 0f);
            bannerBottom.sizeDelta = new Vector2(W, 90f);
            bannerBottom.anchoredPosition = new Vector2(0f, 96f);

            chipPlate = Make("Chip", bannerBottom, new Color(0f, 0f, 0f, 0.5f));
            chipPlate.sprite = skin.square;
            chipPlate.rectTransform.anchorMin = chipPlate.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            chipPlate.rectTransform.pivot = new Vector2(0.5f, 1f);
            chipPlate.rectTransform.sizeDelta = new Vector2(330f, 38f);
            chipPip = Make("ChipPip", chipPlate.rectTransform, Gold);
            chipPip.sprite = skin.square;
            chipPip.rectTransform.sizeDelta = new Vector2(8f, 8f);
            chipPip.rectTransform.anchoredPosition = new Vector2(-138f, 0f);
            chipPip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            chipLabel = Text("ChipLabel", chipPlate.rectTransform, skin.oswald, 13f, TextAlignmentOptions.Center, Gold);
            chipLabel.characterSpacing = 28f;
            chipLabel.rectTransform.sizeDelta = new Vector2(300f, 20f);
            chipLabel.rectTransform.anchoredPosition = new Vector2(10f, 0f);

            note = Text("Note", bannerBottom, skin.spectral, 18f, TextAlignmentOptions.Center, Muted);
            note.rectTransform.anchorMin = note.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            note.rectTransform.pivot = new Vector2(0.5f, 1f);
            note.rectTransform.sizeDelta = new Vector2(W, 28f);
            note.rectTransform.anchoredPosition = new Vector2(0f, -50f);
        }

        // ================== KLEINWERKZEUG ==================

        /// <summary>
        /// Ein Rauten-/Quadratumriss aus vier Balken. Ein 9-Slice-Rahmen kann seine
        /// Strichstärke nicht mitwachsen lassen; hier ist sie ein Parameter, und
        /// genau das braucht Bloom (vier Ringe mit vier Stärken).
        /// </summary>
        private sealed class Outline
        {
            public RectTransform Root;
            public Image Glow;
            private Image[] bars;

            public static Outline Build(string name, RectTransform parent, TransitionSkin skin)
            {
                var outline = new Outline();
                outline.Root = MakeRect(name, parent);
                outline.Glow = Make("Glow", outline.Root, Color.clear);
                outline.Glow.sprite = skin.glow;
                outline.bars = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    outline.bars[i] = Make("Bar" + i, outline.Root, Color.clear);
                    outline.bars[i].sprite = skin.square;
                }
                outline.SetActive(false);
                return outline;
            }

            public void SetActive(bool active)
            {
                Root.gameObject.SetActive(active);
                if (!active) Glow.gameObject.SetActive(false);
            }

            /// <summary>Quadrat der Kantenlänge size, Strichstärke thickness, um rotate gedreht.</summary>
            public void Set(float size, float thickness, Color colour, float rotate)
            {
                if (size <= 1f || colour.a <= 0.002f) { SetActive(false); return; }
                SetActive(true);
                Root.localEulerAngles = new Vector3(0f, 0f, -rotate);
                Root.anchoredPosition = Vector2.zero;
                float half = size * 0.5f - thickness * 0.5f;
                Bar(0, new Vector2(size, thickness), new Vector2(0f, half), colour);
                Bar(1, new Vector2(size, thickness), new Vector2(0f, -half), colour);
                Bar(2, new Vector2(thickness, size), new Vector2(-half, 0f), colour);
                Bar(3, new Vector2(thickness, size), new Vector2(half, 0f), colour);
            }

            /// <summary>Weicher Schein hinter dem Umriss (box-shadow im Handoff).</summary>
            public void SetGlow(float size, Color colour)
            {
                if (colour.a <= 0.002f) { Glow.gameObject.SetActive(false); return; }
                Glow.gameObject.SetActive(true);
                Glow.rectTransform.sizeDelta = new Vector2(size, size);
                Glow.rectTransform.anchoredPosition = Vector2.zero;
                Glow.color = colour;
                Glow.transform.SetAsFirstSibling();
            }

            private void Bar(int index, Vector2 size, Vector2 position, Color colour)
            {
                var rect = bars[index].rectTransform;
                rect.sizeDelta = size;
                rect.anchoredPosition = position;
                bars[index].color = colour;
                bars[index].gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// CSS-Koordinaten in Unity umsetzen: Ursprung ist die Siegelmitte, y zeigt
        /// im Handoff nach unten und in Unity nach oben, und CSS dreht im, Unity
        /// gegen den Uhrzeigersinn.
        /// </summary>
        private static void Place(Image image, float cssX, float cssY, float w, float h, float cssRotate = 0f)
        {
            if (w <= 0.5f || h <= 0.5f) { image.gameObject.SetActive(false); return; }
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(cssX, -cssY);
            rect.localEulerAngles = new Vector3(0f, 0f, -cssRotate);
            image.gameObject.SetActive(true);
        }

        private static void Place(RectTransform rect, float cssX, float cssY, float w, float h, float cssRotate = 0f)
        {
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(cssX, -cssY);
            rect.localEulerAngles = new Vector3(0f, 0f, -cssRotate);
            rect.gameObject.SetActive(true);
        }

        /// <summary>Dreht einen Versatz im CSS-Sinn (im Uhrzeigersinn, y nach unten).</summary>
        private static Vector2 RotateCss(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            return colour;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image Make(string name, RectTransform parent, Color colour)
        {
            var image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(string name, RectTransform parent, TMP_FontAsset font,
                                     float size, TextAlignmentOptions align, Color colour)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.color = colour;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }
    }
}
