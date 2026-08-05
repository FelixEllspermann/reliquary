using System;
using System.Collections;
using System.Collections.Generic;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die Niederlage (Handoff „Animations", Abschnitt 5). Fünf Szenen, 9,4 s:
    ///   Brink    1.8 — der letzte direkte Angriff steht bevor
    ///   Strike   1.7 — er trifft, die LP laufen leer, Risse ziehen
    ///   Break    1.5 — die Spielerkarte zerspringt, das Feld entsättigt
    ///   Collapse 1.2 — die Keile fallen weiter, Asche rieselt
    ///   Defeat   3.2 — der Abspann, und was NICHT verloren ging
    ///
    /// Die Prämisse: der Spieler IST eine Karte, ein direkter Angriff trifft diese
    /// Karte. Verlieren benutzt darum dieselbe Grammatik wie jede Zerstörung —
    /// getroffen, gerissen, zersplittert — nur langsamer, und das Brett geht mit.
    ///
    /// <b>Die Rahmenfarbe ist die Lebensanzeige</b>: critical = 1 − clamp(lp/1200)
    /// zieht den Rand von Gold nach Ember. Bei 400 Leben ist die Karte quer über
    /// den Tisch als Ember zu erkennen, ohne eine Ziffer zu lesen.
    /// </summary>
    public class PlayerDefeatSequence : MonoBehaviour
    {
        private const float W = 1280f, H = 720f;
        private const float CardW = 200f, CardH = 280f;
        private const float CardY = 476f;      // Mitte der Spielerkarte von oben
        private const float FoeW = 120f, FoeH = 168f;
        private const float FoeRestY = 181f;

        private static readonly float[] Durations = { 1.8f, 1.7f, 1.5f, 1.2f, 3.2f };

        private static readonly Color Gold = new Color(0.784f, 0.643f, 0.361f);      // #C8A45C
        private static readonly Color Ember = new Color(0.878f, 0.376f, 0.227f);     // #E0603A
        private static readonly Color EmberLit = new Color(0.953f, 0.765f, 0.651f);  // #F3C3A6
        private static readonly Color Pale = new Color(0.973f, 0.933f, 0.839f);      // #F8EED6

        public static bool Playing { get; private set; }

        private static PlayerDefeatSequence instance;

        private CanvasGroup group;
        private RectTransform stage, fieldRoot;
        private TransitionSkin skin;

        private Image tableGlow, weave, vignette, ashVeil, blackout, halo, shockRing;
        private RectTransform playerCard, foeCard, crackRoot, defeatBlock, chipRow;
        private readonly List<RectTransform> cracks = new List<RectTransform>();
        private readonly List<RectTransform> flecks = new List<RectTransform>();
        private readonly List<Image> fleckImages = new List<Image>();
        private readonly List<RectTransform> chips = new List<RectTransform>();

        private Image cardFrame, cardGlow, lifeBarFill, lifeBarTrack;
        private TMP_Text cardName, cardRole, lifeLabel, lifeValue, lifeFraction;
        private TMP_Text labelTop, labelSub, damageNumber, seal, defeatWord, defeatLine;

        private CardShatter shatter;
        private Action finished;
        private string winner = "Your opponent";
        private int turn = 1, rpDelta;
        private int lifeStart = 400, lifeMax = 1200;
        private int damage = 2600;

        // ================== START ==================

        public static void Play(string winnerName, int turnNumber, int rpChange,
                                int lifeLeft, int hit, Action onDone = null)
        {
            if (instance == null)
            {
                var host = new GameObject("~Defeat");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<PlayerDefeatSequence>();
                instance.Build();
            }
            instance.StartSequence(winnerName, turnNumber, rpChange, lifeLeft, hit, onDone);
        }

        private void StartSequence(string winnerName, int turnNumber, int rpChange,
                                   int lifeLeft, int hit, Action onDone)
        {
            StopAllCoroutines();
            finished = onDone;
            winner = string.IsNullOrEmpty(winnerName) ? "Your opponent" : winnerName;
            turn = Mathf.Max(1, turnNumber);
            rpDelta = rpChange;
            lifeStart = Mathf.Max(0, lifeLeft);
            damage = Mathf.Max(1, hit);
            gameObject.SetActive(true);
            StartCoroutine(Run());
        }

        // ================== ABLAUF ==================

        private IEnumerator Run()
        {
            Playing = true;
            group.alpha = 1f;
            group.blocksRaycasts = true;

            for (int scene = 0; scene < Durations.Length; scene++)
            {
                float duration = Durations[scene];
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    Frame(scene, Mathf.Clamp01(t / duration));
                    // Überspringen führt an den Anfang von Defeat — nie mittendrin
                    if (scene < 4 && SkipPressed())
                    {
                        Frame(3, 1f);
                        scene = 3;
                        break;
                    }
                    yield return null;
                }
                Frame(scene, 1f);
            }

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

        private void Frame(int scene, float p)
        {
            Clear();
            switch (scene)
            {
                case 0: SceneBrink(p); break;
                case 1: SceneStrike(p); break;
                case 2: SceneBreak(p); break;
                case 3: SceneCollapse(p); break;
                default: SceneDefeat(p); break;
            }
        }

        private void Clear()
        {
            playerCard.gameObject.SetActive(false);
            foeCard.gameObject.SetActive(false);
            crackRoot.gameObject.SetActive(false);
            defeatBlock.gameObject.SetActive(false);
            chipRow.gameObject.SetActive(false);
            halo.gameObject.SetActive(false);
            shockRing.gameObject.SetActive(false);
            damageNumber.gameObject.SetActive(false);
            labelTop.transform.parent.gameObject.SetActive(false);
            ashVeil.gameObject.SetActive(false);
            blackout.gameObject.SetActive(false);
            foreach (var fleck in flecks) fleck.gameObject.SetActive(false);
            if (shatter != null) shatter.gameObject.SetActive(false);
        }

        // ---- 1 · Brink: der Schlag steht bevor ----
        private void SceneBrink(float p)
        {
            // 2-Hz-Puls auf allem zugleich — Angreifer, Halo, Ring
            float pulse = 0.5f + 0.5f * Mathf.Sin(Mathf.PI * 2f * p * 2f);
            float inn = Motion.Enter(Motion.Seg(p, 0.04f, 0.3f));

            Field(0f, 0f);
            SetPlayerCard(lifeStart, 1f, 0f);
            SetFoe(FoeRestY, 1f, 0.4f + pulse * 0.6f);
            SetHalo((0.25f + pulse * 0.35f) * inn);
            SetShock(Mathf.Repeat(p * 2f, 1f));
            SetLabel("DIRECT ATTACK",
                $"{lifeStart} life left · no monsters to block", inn);
        }

        // ---- 2 · Strike: er trifft ----
        private void SceneStrike(float p)
        {
            // Der Angreifer steht auf 1.14 — die Endhöhe muss aus der SKALIERTEN
            // Höhe kommen, sonst deckt er das Namensschild zu. Und das Schild ist,
            // was die Karte zur Spielerkarte macht.
            const float scale = 1.14f;
            float lungeY = CardY - CardH * 0.5f - (FoeH * scale) * 0.5f - 6f;

            float lunge = Motion.Enter(Motion.Seg(p, 0.06f, 0.42f));
            float drain = Motion.Seg(p, 0.42f, 0.82f);
            float crack = Motion.Seg(p, 0.52f, 1f);
            float rise = Motion.Enter(Motion.Seg(p, 0.44f, 1f));
            int life = Mathf.RoundToInt(Motion.Mix(lifeStart, 0f, drain));

            Field(0f, Mathf.Sin(Mathf.PI * Motion.Seg(p, 0.4f, 0.66f)) * 0.9f);
            SetPlayerCard(life, 1f, 0f);
            SetCracks(crack);
            SetFoe(Motion.Mix(FoeRestY, lungeY, lunge), scale, 1f - Motion.Seg(p, 0.7f, 1f));
            SetHalo(0.35f);
            SetDamage(rise, 1f - Motion.Enter(Motion.Seg(p, 0.8f, 1f)));
            if (p >= 0.42f && p <= 0.44f) ScreenShake.Shake(0.022f, 0.9f, 18f);
        }

        // ---- 3 · Break: die Karte zerspringt ----
        private void SceneBreak(float p)
        {
            float ash = Motion.Seg(p, 0f, 1f) * 0.5f;
            float fly = Motion.Enter(p);

            Field(ash, Mathf.Sin(Mathf.PI * Motion.Seg(p, 0f, 0.3f)) * 0.5f);
            SetShatter(fly * 150f, fly, 1f, Motion.Mix(0.2f, 1f, p), 1f);
            SetFlecks(ash, p);
        }

        // ---- 4 · Collapse: die Keile fallen weiter ----
        private void SceneCollapse(float p)
        {
            // Die Verdunklung beginnt erst bei 0.8 und erreicht nur 50 %: ein fast
            // schwarzes Bild mitten in der Sequenz liest sich, als wäre die
            // Animation stehengeblieben.
            float fall = Motion.Seg(p, 0f, 0.92f);
            float visible = 1f - Motion.Seg(p, 0.9f, 1f);

            Field(1f, 0f);
            SetShatter(150f, 1f, Motion.Mix(1f, 0.86f, fall), 1f, visible);
            if (shatter != null)
                shatter.Rect.anchoredPosition = new Vector2(0f, -(CardY + fall * 180f));
            SetFlecks(1f, 1f + p);
            SetBlackout(Motion.Seg(p, 0.8f, 1f) * 0.5f);
        }

        // ---- 5 · Defeat: der Abspann ----
        private void SceneDefeat(float p)
        {
            float inn = Motion.Enter(Motion.Seg(p, 0.06f, 0.36f));
            float chipsIn = Motion.Enter(Motion.Seg(p, 0.38f, 0.64f));
            float outro = 1f - Motion.Enter(Motion.Seg(p, 0.94f, 1f));

            Field(1f, 0f);
            SetFlecks(1f, 2f + p);
            SetBlackout(Motion.Mix(0.5f, 0.72f, Motion.Seg(p, 0f, 0.4f)));
            SetDefeat(inn * outro);
            SetChips(chipsIn * outro);
        }

        // ================== BÜHNE ==================

        private void Field(float ash, float shake)
        {
            // Das Feld verdunkelt mit: brightness(1 − ash × 0.42). Der Schleier muss
            // deshalb DUNKEL sein — ein heller Ascheton hellt auf und lässt das
            // ganze Bild flach zulaufen.
            ashVeil.gameObject.SetActive(ash > 0.002f);
            if (ash > 0.002f)
                ashVeil.color = new Color(0.055f, 0.05f, 0.047f, 0.42f * ash);
            tableGlow.color = Color.Lerp(new Color(0.165f, 0.11f, 0.071f),
                new Color(0.06f, 0.05f, 0.05f), ash);

            float x = Mathf.Sin(shake * Mathf.PI * 11f) * shake * 6f;
            float y = Mathf.Cos(shake * Mathf.PI * 8f) * shake * 4f;
            fieldRoot.anchoredPosition = new Vector2(x, y);
        }

        private void SetPlayerCard(int life, float alpha, float lift)
        {
            playerCard.gameObject.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;
            playerCard.anchoredPosition = new Vector2(0f, -(CardY - lift));

            // Die Rahmenfarbe IST die Lebensanzeige
            float critical = 1f - Mathf.Clamp01(life / (float)lifeMax);
            var edge = Color.Lerp(Gold, Ember, critical);
            cardFrame.color = new Color(edge.r, edge.g, edge.b, alpha);
            cardGlow.color = new Color(Ember.r, Ember.g, Ember.b, 0.42f * critical * alpha);

            lifeValue.text = life.ToString();
            lifeValue.color = Motion.Alpha(Color.Lerp(Pale, EmberLit, critical), alpha);
            lifeFraction.text = $"{life} / {lifeMax}";
            lifeFraction.color = Motion.Alpha(new Color(0.612f, 0.541f, 0.416f), alpha);
            lifeBarFill.rectTransform.sizeDelta =
                new Vector2((CardW - 36f) * Mathf.Clamp01(life / (float)lifeMax), 0f);
            lifeBarFill.color = Motion.Alpha(edge, alpha);
            lifeBarTrack.color = new Color(0f, 0f, 0f, 0.5f * alpha);

            cardName.color = Motion.Alpha(Pale, alpha);
            cardRole.color = Motion.Alpha(new Color(0.612f, 0.541f, 0.416f), alpha);
            lifeLabel.color = Motion.Alpha(new Color(0.612f, 0.541f, 0.416f), alpha);
        }

        private void SetFoe(float y, float scale, float glow)
        {
            foeCard.gameObject.SetActive(glow > 0.002f);
            if (glow <= 0.002f) return;
            foeCard.anchoredPosition = new Vector2(0f, -y);
            foeCard.localScale = Vector3.one * scale;
            var image = foeCard.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(glow));
            var frame = foeCard.Find("Frame").GetComponent<Image>();
            frame.color = Motion.Alpha(Ember, 0.55f + glow * 0.45f);
        }

        private void SetHalo(float amount)
        {
            halo.gameObject.SetActive(amount > 0.002f);
            if (amount <= 0.002f) return;
            halo.color = Motion.Alpha(Ember, amount * 0.5f);
            halo.rectTransform.anchoredPosition = new Vector2(0f, -CardY);
        }

        private void SetShock(float amount)
        {
            shockRing.gameObject.SetActive(amount > 0.02f && amount < 0.98f);
            if (!shockRing.gameObject.activeSelf) return;
            float size = Motion.Mix(220f, 620f, Motion.Enter(amount));
            shockRing.rectTransform.sizeDelta = new Vector2(size, size);
            shockRing.rectTransform.anchoredPosition = new Vector2(0f, -CardY);
            shockRing.color = Motion.Alpha(Ember, (1f - amount) * 0.5f);
        }

        private void SetCracks(float amount)
        {
            crackRoot.gameObject.SetActive(amount > 0.002f);
            if (amount <= 0.002f) return;
            crackRoot.anchoredPosition = new Vector2(0f, -CardY);
            for (int i = 0; i < cracks.Count; i++)
            {
                float own = Motion.Seg(amount, i * 0.08f, 0.55f + i * 0.08f);
                cracks[i].localScale = new Vector3(own, 1f, 1f);
                var image = cracks[i].GetComponent<Image>();
                image.color = Motion.Alpha(EmberLit, 0.85f * own);
            }
        }

        private void SetDamage(float rise, float alpha)
        {
            damageNumber.gameObject.SetActive(alpha > 0.002f && rise > 0.002f);
            if (!damageNumber.gameObject.activeSelf) return;
            damageNumber.text = "−" + damage.ToString("#,##0",
                System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
            damageNumber.color = Motion.Alpha(EmberLit, alpha);
            // Rechts NEBEN der Karte, nicht darauf — der Rand muss lesbar bleiben
            ((RectTransform)damageNumber.transform).anchoredPosition =
                new Vector2(CardW * 0.5f + 90f, -(CardY - rise * 70f));
        }

        private void SetShatter(float fly, float spin, float scale, float drain, float fade)
        {
            if (shatter == null) return;
            shatter.gameObject.SetActive(fade > 0.002f);
            if (fade <= 0.002f) return;
            shatter.Rect.anchoredPosition = new Vector2(0f, -CardY);
            shatter.Apply(fly, spin, scale, drain, fade);
        }

        private void SetFlecks(float amount, float t)
        {
            if (amount <= 0.002f) return;
            for (int i = 0; i < flecks.Count; i++)
            {
                flecks[i].gameObject.SetActive(true);
                // Feste Versätze je Flocke — kein Zufall, die Sequenz muss
                // Bild für Bild reproduzierbar bleiben
                float phase = Mathf.Repeat(t * 0.35f + i * 0.137f, 1f);
                float x = (i / (float)flecks.Count - 0.5f) * W * 0.9f
                          + Mathf.Sin(Mathf.PI * 2f * (t * 0.4f + i * 0.31f)) * 26f;
                flecks[i].anchoredPosition = new Vector2(x, -(phase * H));
                fleckImages[i].color = new Color(0.541f, 0.522f, 0.482f,
                    Mathf.Sin(Mathf.PI * phase) * 0.5f * amount);
            }
        }

        private void SetBlackout(float amount)
        {
            blackout.gameObject.SetActive(amount > 0.002f);
            if (amount > 0.002f) blackout.color = new Color(0.039f, 0.027f, 0.02f, amount);
        }

        private void SetLabel(string top, string sub, float alpha)
        {
            var holder = labelTop.transform.parent.gameObject;
            holder.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;
            labelTop.text = top;
            labelTop.color = Motion.Alpha(Ember, alpha);
            labelSub.text = sub;
            labelSub.color = Motion.Alpha(new Color(0.612f, 0.541f, 0.416f), alpha);
        }

        private void SetDefeat(float alpha)
        {
            defeatBlock.gameObject.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;
            seal.text = "THE SEAL HOLDS";
            seal.color = Motion.Alpha(new Color(0.612f, 0.541f, 0.416f), alpha);
            defeatWord.text = "DEFEAT";
            defeatWord.color = Motion.Alpha(EmberLit, alpha);
            defeatLine.text = $"{winner} wins on turn {turn}";
            defeatLine.color = Motion.Alpha(new Color(0.784f, 0.694f, 0.537f), alpha);
        }

        /// <summary>
        /// Die Folgen — und ausdrücklich das, was NICHT verloren ging. Das ist der
        /// Unterschied zwischen einem Niederlage-Bildschirm und einer Bestrafung.
        /// </summary>
        private void SetChips(float alpha)
        {
            chipRow.gameObject.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;

            var rank = PlayerProfile.Rank;
            var texts = new List<string>
            {
                rpDelta != 0 ? $"{rpDelta} RP · {rank.Seal.Label}" : rank.Seal.Label,
                $"{RankLadder.Names[Mathf.Clamp(rank.Rank, 1, 10) - 1]} I is your floor",
            };
            if (PlayerProfile.DailyStreak > 0)
                texts.Add($"Daily Seal {PlayerProfile.DailyStreak} of 7 kept");

            for (int i = 0; i < chips.Count; i++)
            {
                bool used = i < texts.Count;
                chips[i].gameObject.SetActive(used);
                if (!used) continue;
                chips[i].GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f * alpha);
                chips[i].Find("Frame").GetComponent<Image>().color =
                    Motion.Alpha(i == 0 ? Ember : Gold, 0.5f * alpha);
                chips[i].Find("Gem").GetComponent<Image>().color =
                    Motion.Alpha(i == 0 ? Ember : Gold, alpha);
                var label = chips[i].Find("Label").GetComponent<TMP_Text>();
                label.text = texts[i];
                label.color = Motion.Alpha(new Color(0.784f, 0.694f, 0.537f), alpha);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(chipRow);
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

            fieldRoot = MakeRect("Field", stage);
            fieldRoot.anchorMin = Vector2.zero; fieldRoot.anchorMax = Vector2.one;
            fieldRoot.offsetMin = Vector2.zero; fieldRoot.offsetMax = Vector2.zero;

            tableGlow = Make("TableGlow", fieldRoot, new Color(0.165f, 0.11f, 0.071f));
            tableGlow.sprite = skin.glow;
            Stretch(tableGlow.rectTransform, -60f);
            weave = Make("Weave", fieldRoot, new Color(0.784f, 0.643f, 0.361f, 0.04f));
            weave.sprite = skin.weave; weave.type = Image.Type.Tiled;
            Stretch(weave.rectTransform);
            vignette = Make("Vignette", fieldRoot, Color.black);
            vignette.sprite = skin.vignette;
            Stretch(vignette.rectTransform);

            halo = Make("Halo", fieldRoot, new Color(0f, 0f, 0f, 0f));
            halo.sprite = skin.glow;
            halo.rectTransform.anchorMin = halo.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            halo.rectTransform.sizeDelta = new Vector2(CardW * 2.4f, CardH * 2f);

            shockRing = Make("ShockRing", fieldRoot, new Color(0f, 0f, 0f, 0f));
            shockRing.sprite = skin.frame; shockRing.type = Image.Type.Sliced;
            shockRing.rectTransform.anchorMin = shockRing.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shockRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            BuildPlayerCard();
            BuildFoe();      // NACH der Spielerkarte, damit er davor malt
            BuildCracks();
            BuildFlecks();
            BuildText();

            // Die Keile werden aus derselben Bau-Funktion geschnitten wie die Karte
            shatter = CardShatter.Build(fieldRoot, ComposeCardFace, new Vector2(CardW, CardH));
            // Wie die Spielerkarte an der Oberkante verankern — sonst rechnet
            // -CardY von der Bildmitte aus und die Keile liegen unterhalb des Bildes
            shatter.Rect.anchorMin = shatter.Rect.anchorMax = new Vector2(0.5f, 1f);
            shatter.gameObject.SetActive(false);

            ashVeil = Make("AshVeil", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(ashVeil.rectTransform);
            blackout = Make("Blackout", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(blackout.rectTransform);

            gameObject.SetActive(false);
        }

        private void BuildPlayerCard()
        {
            playerCard = MakeRect("PlayerCard", fieldRoot);
            playerCard.anchorMin = playerCard.anchorMax = new Vector2(0.5f, 1f);
            playerCard.sizeDelta = new Vector2(CardW, CardH);
            var face = ComposeCardFace(playerCard);
            face.anchorMin = Vector2.zero; face.anchorMax = Vector2.one;
            face.offsetMin = Vector2.zero; face.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Setzt ein Spielerkarten-Gesicht in <paramref name="parent"/>. Wird
        /// zweimal gebraucht: für die Karte selbst und je Keil beim Bruch —
        /// dadurch bricht genau das, was vorher zu sehen war.
        /// </summary>
        private RectTransform ComposeCardFace(RectTransform parent)
        {
            var root = MakeRect("Face", parent);
            root.sizeDelta = new Vector2(CardW, CardH);

            var body = Make("Body", root, new Color(0.11f, 0.082f, 0.055f, 1f));
            body.sprite = skin.diagFade;
            Stretch(body.rectTransform);

            var glow = Make("Glow", root, new Color(0f, 0f, 0f, 0f));
            glow.sprite = skin.glow;
            Stretch(glow.rectTransform, -18f);

            var frame = Make("Frame", root, Gold);
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);

            // Vier Eckrauten, 10 px eingerückt
            foreach (var corner in new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f),
                                           new Vector2(-1f, -1f), new Vector2(1f, -1f) })
            {
                var gem = Make("Corner", root, Gold);
                gem.sprite = skin.square;
                gem.rectTransform.sizeDelta = new Vector2(8f, 8f);
                gem.rectTransform.anchoredPosition = new Vector2(
                    corner.x * (CardW * 0.5f - 10f), corner.y * (CardH * 0.5f - 10f));
                gem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            }

            // Namensschild — DAS macht die Karte zur Spielerkarte
            var plate = Make("Plate", root, new Color(0.259f, 0.188f, 0.11f, 1f));
            plate.rectTransform.sizeDelta = new Vector2(CardW - 20f, 30f);
            plate.rectTransform.anchoredPosition = new Vector2(0f, CardH * 0.5f - 24f);
            var name = MakeText("Name", root, skin.cinzel, CardH * 0.05f, Pale);
            name.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)name.transform, CardW - 24f, 26f, CardH * 0.5f - 24f);

            var role = MakeText("Role", root, skin.oswald, 10f, new Color(0.612f, 0.541f, 0.416f));
            role.characterSpacing = 22f;
            role.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)role.transform, CardW - 24f, 14f, CardH * 0.5f - 52f);

            // Artwork-Feld: quadratisch, nimmt den Rest — feste Höhe, damit der
            // LIFE-Block unten nicht aus der Karte fällt
            var art = Make("Art", root, new Color(0.243f, 0.176f, 0.086f, 1f));
            art.sprite = skin.diagFade;
            art.rectTransform.sizeDelta = new Vector2(CardW - 28f, CardW - 28f);
            art.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            var artFrame = Make("ArtFrame", art.rectTransform, Motion.Alpha(Gold, 0.7f));
            artFrame.sprite = skin.frame; artFrame.type = Image.Type.Sliced;
            Stretch(artFrame.rectTransform);

            // LIFE-Block statt Effektbox und DMG/DEF
            var label = MakeText("LifeLabel", root, skin.oswald, 9f, new Color(0.612f, 0.541f, 0.416f));
            label.text = "LIFE";
            label.characterSpacing = 24f;
            label.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)label.transform, CardW - 24f, 12f, -CardH * 0.5f + 62f);

            var value = MakeText("LifeValue", root, skin.cinzel, 34f, Pale);
            value.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)value.transform, CardW - 24f, 40f, -CardH * 0.5f + 38f);

            var track = Make("BarTrack", root, new Color(0f, 0f, 0f, 0.5f));
            track.rectTransform.sizeDelta = new Vector2(CardW - 36f, 8f);
            track.rectTransform.anchoredPosition = new Vector2(0f, -CardH * 0.5f + 20f);
            var fill = MakeRect("BarFill", track.rectTransform);
            fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(CardW - 36f, 0f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = Gold; fillImage.raycastTarget = false;

            var fraction = MakeText("Fraction", root, skin.spectral, 11f, new Color(0.612f, 0.541f, 0.416f));
            fraction.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)fraction.transform, CardW - 24f, 14f, -CardH * 0.5f + 8f);

            // Nur die echte Karte merkt sich ihre Teile — Keil-Kopien nicht
            if (cardFrame == null)
            {
                cardFrame = frame; cardGlow = glow;
                cardName = name; cardRole = role;
                lifeLabel = label; lifeValue = value; lifeFraction = fraction;
                lifeBarFill = fillImage; lifeBarTrack = track;
                name.text = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Duelist";
                role.text = "DUELIST · " + PlayerProfile.Rank.Seal.Label.ToUpperInvariant();
            }
            else
            {
                name.text = PlayerProfile.LoggedIn ? PlayerProfile.AccountName : "Duelist";
                role.text = "DUELIST · " + PlayerProfile.Rank.Seal.Label.ToUpperInvariant();
                value.text = "0";
                fraction.text = "0 / " + lifeMax;
                fillImage.rectTransform.sizeDelta = new Vector2(0f, 0f);
                frame.color = Ember;
            }
            return root;
        }

        private void BuildFoe()
        {
            foeCard = MakeRect("Foe", fieldRoot);
            foeCard.anchorMin = foeCard.anchorMax = new Vector2(0.5f, 1f);
            foeCard.sizeDelta = new Vector2(FoeW, FoeH);
            var body = foeCard.gameObject.AddComponent<Image>();
            body.sprite = skin.diagFade;
            body.color = new Color(0.212f, 0.106f, 0.075f, 1f);
            body.raycastTarget = false;
            var frame = Make("Frame", foeCard, Ember);
            frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);
            var crest = Make("Crest", foeCard, Motion.Alpha(Ember, 0.6f));
            crest.sprite = skin.square;
            crest.rectTransform.sizeDelta = new Vector2(34f, 34f);
            crest.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        private void BuildCracks()
        {
            crackRoot = MakeRect("Cracks", fieldRoot);
            crackRoot.anchorMin = crackRoot.anchorMax = new Vector2(0.5f, 1f);
            // Die sechs Linien laufen vom Bruchpunkt nach aussen — dieselben
            // Linien, an denen die Keile später auseinandergehen
            var origin = new Vector2((CardShatter.Origin.x - 0.5f) * CardW,
                                     (0.5f - CardShatter.Origin.y) * CardH);
            float[] angles = { 118f, 60f, 196f, 340f, 262f, 300f };
            foreach (float angle in angles)
            {
                var line = Make("Crack", crackRoot, Motion.Alpha(EmberLit, 0f));
                line.sprite = skin.rule;
                line.rectTransform.pivot = new Vector2(0f, 0.5f);
                line.rectTransform.sizeDelta = new Vector2(CardH * 0.55f, 2f);
                line.rectTransform.anchoredPosition = origin;
                line.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                cracks.Add(line.rectTransform);
            }
        }

        private void BuildFlecks()
        {
            for (int i = 0; i < 22; i++)
            {
                var fleck = Make("Fleck" + i, fieldRoot, new Color(0f, 0f, 0f, 0f));
                fleck.sprite = skin.square;
                fleck.rectTransform.anchorMin = fleck.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                fleck.rectTransform.sizeDelta = Vector2.one * (3f + i % 3);
                fleck.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                flecks.Add(fleck.rectTransform);
                fleckImages.Add(fleck);
            }
        }

        private void BuildText()
        {
            var labelHolder = MakeRect("Label", stage);
            labelHolder.anchorMin = labelHolder.anchorMax = new Vector2(0.5f, 1f);
            labelHolder.sizeDelta = new Vector2(900f, 60f);
            labelHolder.anchoredPosition = new Vector2(0f, -74f);
            labelTop = MakeText("Top", labelHolder, skin.oswald, 14f, Ember);
            labelTop.characterSpacing = 40f;
            labelTop.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)labelTop.transform, 900f, 20f, 14f);
            labelSub = MakeText("Sub", labelHolder, skin.spectral, 15f, Gold);
            labelSub.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)labelSub.transform, 900f, 20f, -12f);

            damageNumber = MakeText("Damage", fieldRoot, skin.cinzel, 46f, EmberLit);
            damageNumber.alignment = TextAlignmentOptions.Left;
            var damageRect = (RectTransform)damageNumber.transform;
            damageRect.anchorMin = damageRect.anchorMax = new Vector2(0.5f, 1f);
            damageRect.sizeDelta = new Vector2(260f, 56f);

            defeatBlock = MakeRect("DefeatBlock", stage);
            defeatBlock.anchorMin = defeatBlock.anchorMax = new Vector2(0.5f, 0.5f);
            defeatBlock.sizeDelta = new Vector2(1000f, 220f);
            defeatBlock.anchoredPosition = new Vector2(0f, 40f);
            seal = MakeText("Seal", defeatBlock, skin.oswald, 13f, Gold);
            seal.characterSpacing = 38f;
            seal.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)seal.transform, 1000f, 18f, 92f);
            defeatWord = MakeText("Defeat", defeatBlock, skin.cinzel, 104f, EmberLit);
            defeatWord.alignment = TextAlignmentOptions.Center;
            defeatWord.characterSpacing = 6f;
            Strip((RectTransform)defeatWord.transform, 1000f, 120f, 10f);
            defeatLine = MakeText("Line", defeatBlock, skin.spectral, 17f, Gold);
            defeatLine.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)defeatLine.transform, 1000f, 22f, -74f);

            chipRow = MakeRect("Chips", stage);
            chipRow.anchorMin = chipRow.anchorMax = new Vector2(0.5f, 0.5f);
            chipRow.sizeDelta = new Vector2(1000f, 44f);
            chipRow.anchoredPosition = new Vector2(0f, -130f);
            var layout = chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            layout.childControlWidth = true; layout.childControlHeight = true;
            for (int i = 0; i < 3; i++)
            {
                var chip = MakeRect("Chip" + i, chipRow);
                var element = chip.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 260f; element.preferredHeight = 40f;
                var bg = chip.gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.5f); bg.raycastTarget = false;
                var frame = Make("Frame", chip, Gold);
                frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
                Stretch(frame.rectTransform);
                var gem = Make("Gem", chip, Gold);
                gem.sprite = skin.square;
                gem.rectTransform.sizeDelta = new Vector2(8f, 8f);
                gem.rectTransform.anchorMin = gem.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                gem.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                gem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                var label = MakeText("Label", chip, skin.spectral, 15f, Gold);
                label.alignment = TextAlignmentOptions.Left;
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(32f, 0f); labelRect.offsetMax = new Vector2(-12f, 0f);
                chips.Add(chip);
            }
        }

        // ---- Bau-Helfer ----

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Strip(RectTransform rect, float width, float height, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
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

        private static TMP_Text MakeText(string name, RectTransform parent, TMP_FontAsset font,
                                         float size, Color colour)
        {
            var text = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = colour;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
