using System;
using System.Collections;
using System.Collections.Generic;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Der Aufstieg (Handoff „Animations", Abschnitt 1). Läuft nach dem Ergebnis,
    /// wenn ein Duell den Spieler über eine Rangschwelle schiebt.
    ///
    /// Fünf Szenen, 9,4 Sekunden:
    ///   Result 1.6 — Sieg, altes Siegel, RP-Balken knapp unter der Schwelle
    ///   Award  1.8 — der Balken zählt hoch und läuft über
    ///   Break  1.4 — das alte Siegel zerspringt, der Bühnenton kreuzt zur neuen Farbe
    ///   Forge  1.8 — das neue Siegel baut sich von innen nach aussen auf
    ///   Reveal 2.8 — Name, Stufenpips, neuer Balken, Belohnungen
    ///
    /// Der Kern des Ganzen: das neue Siegel hat genau eine Ebene mehr als das
    /// zersprungene. Man sieht den Aufstieg, ohne den Namen zu lesen.
    ///
    /// Jede Bewegung hängt am Fortschritt ihrer Szene, nicht an einer eigenen Uhr —
    /// dadurch bleibt jede Szene dehnbar. Und das erste Bild einer Szene ist
    /// immer das letzte der vorherigen, sonst springt es an den Nahtstellen.
    /// </summary>
    public class RankUpSequence : MonoBehaviour
    {
        private const float W = 1280f, H = 720f;   // Bezugsfläche des Handoffs
        private const float CY = 336f;             // Mitte des Siegels von oben
        private const float SealBox = 182f;

        private static readonly float[] Durations = { 1.6f, 1.8f, 1.4f, 1.8f, 2.8f };

        private static RankUpSequence instance;

        /// <summary>Läuft gerade ein Aufstieg? Andere Bildschirme sollen dann warten.</summary>
        public static bool Playing { get; private set; }

        private CanvasGroup group;
        private RectTransform stage;
        private TransitionSkin skin;

        // Bühne
        private Image tableGlow, weave, vignette, wash, flash, shockRing, blackout;
        private RectTransform frameDiamond;

        // Siegel
        private RankSealView oldSeal, newSeal;

        // Text
        private TMP_Text eyebrowText, headline, rpLabel, rpValue, rankName;
        private RectTransform eyebrow, eyebrowLeft, eyebrowRight, rpBar, rpFill, rpSheen, gainChip, pipRow, rewardRow;
        private TMP_Text gainText;
        private Image rpBarFrame, rpTrack;
        private readonly List<Image> pips = new List<Image>();
        private readonly List<RectTransform> rewards = new List<RectTransform>();

        private Promotion promo;
        private Action finished;

        /// <summary>Alles, was die Sequenz über den Aufstieg wissen muss.</summary>
        private struct Promotion
        {
            public int From, Into;
            public int Gain;
            public string Opponent;
            public string FromLabel, ToLabel;
            public float FromFill;        // Füllstand des alten Balkens vor dem Gewinn
            public int Rp0, Rp1, FromCap;
            public float ToFill;
            public int ToCap;
            public string ToNote;         // statt „x / y RP" an der Spitze
            public int Coins;
            public string Unlock;
        }

        /// <summary>
        /// Spielt den Aufstieg. <paramref name="onDone"/> feuert am Ende — auch
        /// wenn abgebrochen wird, damit der Aufrufer nie hängen bleibt.
        /// </summary>
        public static void Play(int fromRank, int intoRank, int rpGain, string opponent, Action onDone = null)
        {
            if (instance == null)
            {
                var host = new GameObject("~RankUp");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<RankUpSequence>();
                instance.Build();
            }
            instance.StartSequence(fromRank, intoRank, rpGain, opponent, onDone);
        }

        private void StartSequence(int fromRank, int intoRank, int rpGain, string opponent, Action onDone)
        {
            StopAllCoroutines();
            finished = onDone;
            promo = Resolve(fromRank, intoRank, rpGain, opponent);
            oldSeal.Rebuild(promo.From);
            newSeal.Rebuild(promo.Into);
            gameObject.SetActive(true);
            StartCoroutine(Run());
        }

        /// <summary>
        /// Leitet aus altem und neuem Rang alles ab, was angezeigt wird. Die RP
        /// stellt der Handoff so ein, dass der Sieg immer knapp über der Schwelle
        /// landet — das ist der Fall, der die Animation verdient.
        /// </summary>
        private static Promotion Resolve(int fromRank, int intoRank, int rpGain, string opponent)
        {
            int into = Mathf.Clamp(intoRank, 2, 10);
            int from = Mathf.Clamp(fromRank, 1, into - 1);
            var band = RankBands.Of(into);
            var previous = RankBands.Of(from);

            float fromWidth = (previous.Hi - previous.Lo) / 5f;
            int rp0 = band.Lo - 20;
            int rp1 = rp0 + Mathf.Max(1, rpGain);
            float toWidth = band.Hi > 0 ? (band.Hi - band.Lo) / 5f : 0f;

            return new Promotion
            {
                From = from,
                Into = into,
                Gain = Mathf.Max(1, rpGain),
                Opponent = string.IsNullOrEmpty(opponent) ? "Your opponent" : opponent,
                FromLabel = RankLadder.Names[from - 1].ToUpperInvariant() + " V",
                ToLabel = RankLadder.Names[into - 1].ToUpperInvariant() + " I",
                FromFill = fromWidth > 0f ? 1f - 20f / fromWidth : 1f,
                Rp0 = rp0,
                Rp1 = rp1,
                FromCap = band.Lo,
                ToFill = toWidth > 0f ? (rp1 - band.Lo) / toWidth : 1f,
                ToCap = toWidth > 0f ? Mathf.RoundToInt(band.Lo + toWidth) : 0,
                ToNote = toWidth > 0f ? null : "top 8 000 · ranked by placement",
                Coins = 100 + into * 25,
                Unlock = into == 6 ? "Gilded Reliquary frame"
                       : into == 8 ? "Amber Halo frame"
                       : into == 10 ? "Vault Ring frame"
                       : null,
            };
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
                    yield return null;
                }
                Frame(scene, 1f);   // Schlussbild sauber setzen, sonst springt die Naht
            }

            gameObject.SetActive(false);
            Playing = false;
            var callback = finished;
            finished = null;
            callback?.Invoke();
        }

        private void Frame(int scene, float p)
        {
            // Jedes Bild fängt leer an. Sonst erbt eine Szene stillschweigend, was
            // eine andere stehen gelassen hat — und die Naht zwischen zwei Szenen
            // hinge davon ab, in welcher Reihenfolge sie liefen.
            Clear();
            switch (scene)
            {
                case 0: SceneResult(p); break;
                case 1: SceneAward(p); break;
                case 2: SceneBreak(p); break;
                case 3: SceneForge(p); break;
                default: SceneReveal(p); break;
            }
        }

        /// <summary>Alles aus. Jede Szene schaltet danach an, was sie braucht.</summary>
        private void Clear()
        {
            SetSeal(oldSeal, 0f, 1f, 0f, 0f);
            SetSeal(newSeal, 0f, 1f, 0f, 0f);
            eyebrow.gameObject.SetActive(false);
            headline.gameObject.SetActive(false);
            rankName.gameObject.SetActive(false);
            pipRow.gameObject.SetActive(false);
            gainChip.gameObject.SetActive(false);
            rpBar.gameObject.SetActive(false);
            rewardRow.gameObject.SetActive(false);
            flash.gameObject.SetActive(false);
            shockRing.gameObject.SetActive(false);
            wash.gameObject.SetActive(false);
            blackout.gameObject.SetActive(false);
        }

        // ---- 1 · Result: Sieg, altes Siegel, Balken knapp unter der Schwelle ----
        private void SceneResult(float p)
        {
            float inn = Motion.Enter(Motion.Seg(p, 0.04f, 0.4f));
            float glow = 0.1f + Motion.Arc(p, 0.3f, 1f) * 0.12f;

            Table(Motion.Mix(1.02f, 1.06f, Motion.Drift(p)), promo.From, promo.From, 0f);
            SetSeal(oldSeal, 0f, 1f, glow, inn);
            SetSeal(newSeal, 0f, 0f, 0f, 0f);

            SetEyebrow("DUEL WON", inn, Motion.Mix(14f, 0f, inn), Hex("#7ACD96"));
            SetHeadline($"{promo.Opponent} defeated", inn, Motion.Mix(16f, 0f, inn));
            SetRpBar(promo.FromLabel, promo.FromFill, promo.Rp0, promo.FromCap, null,
                RankLadder.Edge(promo.From), RankLadder.Text(promo.From), inn, 0f);
            SetGainChip(0f, 0f);
            SetRankName(0f);
            SetPips(0f);
            SetRewards(0f);
            SetFlash(0f);
            SetShock(0f);
            SetWash(0f);
            SetBlackout(1f - Motion.Enter(Motion.Seg(p, 0f, 0.22f)));
        }

        // ---- 2 · Award: der Balken zählt hoch und läuft über ----
        private void SceneAward(float p)
        {
            float count = Motion.Enter(Motion.Seg(p, 0.14f, 0.72f));
            float overflow = 1f - Motion.Enter(Motion.Seg(p, 0.72f, 0.94f));
            float chip = Motion.Enter(Motion.Seg(p, 0.04f, 0.24f)) * (1f - Motion.Enter(Motion.Seg(p, 0.78f, 1f)));
            float fade = 1f - Motion.Enter(Motion.Seg(p, 0.5f, 0.86f));

            Table(Motion.Mix(1.06f, 1.1f, Motion.Drift(p)), promo.From, promo.From, 0f);
            SetSeal(oldSeal, 0f, 1f, 0.22f + Motion.Enter(Motion.Seg(p, 0.6f, 1f)) * 0.6f, 1f);

            SetEyebrow("DUEL WON", fade, Motion.Mix(0f, -12f, 1f - fade), Hex("#7ACD96"));
            SetHeadline($"{promo.Opponent} defeated", 1f - Motion.Enter(Motion.Seg(p, 0.42f, 0.78f)), 0f);
            SetRpBar(promo.FromLabel, Motion.Mix(promo.FromFill, 1f, count),
                Mathf.RoundToInt(Motion.Mix(promo.Rp0, promo.Rp1, count)), promo.FromCap, null,
                RankLadder.Edge(promo.From), RankLadder.Text(promo.From), 1f, overflow);
            SetGainChip(chip, Motion.Mix(20f, 0f, Motion.Enter(Motion.Seg(p, 0.04f, 0.24f))));
            SetBlackout(0f);
        }

        // ---- 3 · Break: das alte Siegel zerspringt ----
        private void SceneBreak(float p)
        {
            float scatter = Motion.Seg(p, 0.16f, 1f);
            float flash = Motion.Arc(p, 0.06f, 0.62f);
            float barOut = 1f - Motion.Enter(Motion.Seg(p, 0.3f, 0.66f));
            float shock = Motion.Seg(p, 0.1f, 0.7f);

            Table(Motion.Mix(1.1f, 1.2f, Motion.Drift(p)), promo.From, promo.Into, Motion.Seg(p, 0.45f, 1f));
            SetSeal(oldSeal, scatter, 1f, Motion.Mix(0.82f, 0.2f, Motion.Enter(scatter)), 1f);

            SetEyebrow("DUEL WON", 0f, 0f, Hex("#7ACD96"));
            SetHeadline("", 0f, 0f);
            SetGainChip(0f, 0f);
            SetRpBar(promo.FromLabel, 1f, promo.Rp1, promo.FromCap, null,
                RankLadder.Edge(promo.From), RankLadder.Text(promo.From), barOut, barOut);
            SetShock(shock);
            SetFlash(flash);
            SetWash(Motion.Enter(Motion.Seg(p, 0.82f, 1f)) * 0.35f);
        }

        // ---- 4 · Forge: das neue Siegel baut sich auf ----
        private void SceneForge(float p)
        {
            float forge = Motion.Seg(p, 0.06f, 0.9f);
            float title = Motion.Enter(Motion.Seg(p, 0.5f, 0.86f));

            Table(Motion.Mix(1.2f, 1.08f, Motion.Drift(p)), promo.Into, promo.Into, 1f);
            SetSeal(oldSeal, 1f, 1f, 0f, 0f);
            SetSeal(newSeal, 0f, forge, 0.4f + Motion.Arc(p, 0.2f, 1f) * 0.4f, 1f);

            SetEyebrow("A NEW SEAL", title, Motion.Mix(14f, 0f, title), RankLadder.Edge(promo.Into));
            SetRpBar(promo.ToLabel, 0f, 0, 0, null, RankLadder.Edge(promo.Into), RankLadder.Text(promo.Into), 0f, 0f);
            SetShock(0f);
            SetFlash(0f);
            SetWash((1f - Motion.Enter(Motion.Seg(p, 0f, 0.34f))) * 0.35f);
        }

        // ---- 5 · Reveal: Name, Stufe, Balken, Belohnungen ----
        private void SceneReveal(float p)
        {
            float inn = Motion.Enter(Motion.Seg(p, 0.04f, 0.34f));
            float bar = Motion.Enter(Motion.Seg(p, 0.3f, 0.56f));
            float reward = Motion.Enter(Motion.Seg(p, 0.44f, 0.68f));
            float outro = 1f - Motion.Enter(Motion.Seg(p, 0.94f, 1f));
            float breathe = Mathf.Sin(Mathf.PI * 2f * Motion.Seg(p, 0.2f, 1f) - Mathf.PI * 0.5f) * 0.5f + 0.5f;

            Table(Motion.Mix(1.08f, 1.04f, Motion.Drift(p)), promo.Into, promo.Into, 1f);
            SetSeal(newSeal, 0f, 1f, (0.42f + breathe * 0.3f) * outro, 1f);

            SetEyebrow("A NEW SEAL", outro, 0f, RankLadder.Edge(promo.Into));
            SetHeadline("", 0f, 0f);
            SetRankName(inn * outro);
            SetPips(inn * outro);
            SetRpBar(promo.ToLabel, promo.ToFill, promo.Rp1, promo.ToCap, promo.ToNote,
                RankLadder.Edge(promo.Into), RankLadder.Text(promo.Into), bar * outro, 0f);
            SetRewards(reward * outro);
            SetWash(0f);
            SetBlackout(Motion.Enter(Motion.Seg(p, 0.94f, 1f)));
        }

        // ================== BÜHNE STELLEN ==================

        private void Table(float scale, int warmRank, int towardRank, float toward)
        {
            var warm = RankLadder.Stage(warmRank);
            var target = RankLadder.Stage(towardRank);
            tableGlow.color = Motion.Mix(warm, target, toward);
            tableGlow.rectTransform.localScale = Vector3.one * scale;
        }

        private void SetSeal(RankSealView seal, float scatter, float forge, float glow, float fade) =>
            seal.Apply(scatter, forge, glow, fade);

        private void SetEyebrow(string text, float alpha, float rise, Color tone)
        {
            eyebrow.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            eyebrowText.text = text;
            eyebrowText.color = Motion.Alpha(tone, alpha);
            eyebrowLeft.GetComponent<Image>().color = Motion.Alpha(tone, alpha * 0.7f);
            eyebrowRight.GetComponent<Image>().color = Motion.Alpha(tone, alpha * 0.7f);
            eyebrow.anchoredPosition = new Vector2(0f, -74f - rise);
        }

        private void SetHeadline(string text, float alpha, float rise)
        {
            headline.gameObject.SetActive(alpha > 0.001f && !string.IsNullOrEmpty(text));
            if (!headline.gameObject.activeSelf) return;
            headline.text = text;
            headline.color = Motion.Alpha(Hex("#F1DFB8"), alpha);
            ((RectTransform)headline.transform).anchoredPosition = new Vector2(0f, -118f - rise);
        }

        private void SetRpBar(string label, float fill, int rp, int cap, string note,
                              Color edge, Color textColor, float alpha, float overflow)
        {
            rpBar.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;

            rpLabel.text = label;
            rpLabel.color = Motion.Alpha(textColor, alpha);
            rpValue.text = note ?? $"{Thousands(rp)} / {Thousands(cap)} RP";
            rpValue.color = Motion.Alpha(Hex("#9C8A6A"), alpha);

            rpBarFrame.color = Motion.Alpha(edge, 0.42f * alpha);
            rpTrack.color = new Color(0f, 0f, 0f, 0.5f * alpha);
            rpFill.sizeDelta = new Vector2(560f * Mathf.Clamp01(fill), 0f);
            rpFill.GetComponent<Image>().color = Motion.Alpha(edge, alpha);
            rpSheen.gameObject.SetActive(overflow > 0.001f);
            if (overflow > 0.001f)
                rpSheen.GetComponent<Image>().color = Motion.Alpha(Hex("#F8EED6"), 0.5f * overflow * alpha);
        }

        private void SetGainChip(float alpha, float rise)
        {
            gainChip.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            gainText.text = $"+{promo.Gain} RP";
            gainText.color = Motion.Alpha(Hex("#A8E4BE"), alpha);
            gainChip.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f * alpha);
            gainChip.Find("Frame").GetComponent<Image>().color = Motion.Alpha(Hex("#7ACD96"), 0.55f * alpha);
            gainChip.Find("Gem").GetComponent<Image>().color = Motion.Alpha(Hex("#7ACD96"), alpha);
            gainChip.anchoredPosition = new Vector2(0f, -456f - rise);
        }

        private void SetRankName(float alpha)
        {
            rankName.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            rankName.text = promo.ToLabel;
            rankName.color = Motion.Alpha(RankLadder.Text(promo.Into), alpha);
            ((RectTransform)rankName.transform).anchoredPosition =
                new Vector2(0f, -122f - Motion.Mix(22f, 0f, alpha));
        }

        private void SetPips(float alpha)
        {
            pipRow.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            var edge = RankLadder.Edge(promo.Into);
            for (int i = 0; i < pips.Count; i++)
            {
                // Nach dem Aufstieg steht man auf Stufe I: genau ein Pip gefüllt
                bool filled = i == 0;
                pips[i].sprite = filled ? skin.square : skin.frame;
                pips[i].type = filled ? Image.Type.Simple : Image.Type.Sliced;
                pips[i].color = Motion.Alpha(edge, filled ? alpha : 0.5f * alpha);
            }
        }

        private void SetRewards(float alpha)
        {
            rewardRow.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;

            var texts = new List<string> { "1 Sealed Pack", $"{Thousands(promo.Coins)} Coins" };
            var tones = new List<Color> { Hex("#EBCE8A"), Hex("#EBCE8A") };
            if (!string.IsNullOrEmpty(promo.Unlock))
            {
                texts.Add(promo.Unlock);
                tones.Add(RankLadder.Edge(promo.Into));
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                bool used = i < texts.Count;
                rewards[i].gameObject.SetActive(used);
                if (!used) continue;
                rewards[i].GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f * alpha);
                rewards[i].Find("Frame").GetComponent<Image>().color = Motion.Alpha(tones[i], 0.45f * alpha);
                rewards[i].Find("Gem").GetComponent<Image>().color = Motion.Alpha(tones[i], alpha);
                var text = rewards[i].Find("Label").GetComponent<TMP_Text>();
                text.text = texts[i];
                text.color = Motion.Alpha(Hex("#C8B189"), alpha);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardRow);
        }

        private void SetFlash(float amount)
        {
            flash.gameObject.SetActive(amount > 0.001f);
            if (amount <= 0.001f) return;
            flash.color = Motion.Alpha(Hex("#F8EED6"), amount * 0.5f);
            float size = 200f + amount * 1900f;
            flash.rectTransform.sizeDelta = new Vector2(size, size);
        }

        private void SetShock(float amount)
        {
            shockRing.gameObject.SetActive(amount > 0.001f && amount < 1f);
            if (!shockRing.gameObject.activeSelf) return;
            float size = Motion.Mix(180f, 900f, Motion.Enter(amount));
            shockRing.rectTransform.sizeDelta = new Vector2(size, size);
            shockRing.color = Motion.Alpha(Hex("#F8EED6"), (1f - amount) * 0.7f);
        }

        private void SetWash(float amount)
        {
            wash.gameObject.SetActive(amount > 0.001f);
            if (amount > 0.001f) wash.color = Motion.Alpha(Hex("#F8EED6"), amount);
        }

        private void SetBlackout(float amount)
        {
            blackout.gameObject.SetActive(amount > 0.001f);
            if (amount > 0.001f) blackout.color = new Color(0.039f, 0.027f, 0.02f, amount);
        }

        /// <summary>„4 500" — Tausender mit schmalem Abstand wie im Handoff.</summary>
        private static string Thousands(int value) =>
            value.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        // ================== AUFBAU ==================

        private void Build()
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;   // über allem, auch über dem Ergebnisbildschirm
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            stage = (RectTransform)canvasGo.transform;

            BuildTable();
            oldSeal = RankSealView.Build(stage, 1, SealBox);
            newSeal = RankSealView.Build(stage, 2, SealBox);
            foreach (var seal in new[] { oldSeal, newSeal })
                seal.Rect.anchoredPosition = new Vector2(0f, H * 0.5f - CY);

            BuildShock();
            BuildText();
            BuildRpBar();
            BuildRewards();
            BuildOverlays();

            gameObject.SetActive(false);
        }

        private void BuildTable()
        {
            tableGlow = Make("TableGlow", stage, Hex("#2A1C12"));
            tableGlow.sprite = skin.glow;
            Stretch(tableGlow.rectTransform, -60f);

            weave = Make("Weave", stage, new Color(0.784f, 0.643f, 0.361f, 0.045f));
            weave.sprite = skin.weave;
            weave.type = Image.Type.Tiled;
            Stretch(weave.rectTransform);

            frameDiamond = MakeRect("FrameDiamond", stage);
            frameDiamond.sizeDelta = new Vector2(620f, 620f);
            frameDiamond.anchoredPosition = new Vector2(0f, H * 0.5f - CY);
            frameDiamond.localEulerAngles = new Vector3(0f, 0f, 45f);
            var diamondLine = frameDiamond.gameObject.AddComponent<Image>();
            diamondLine.sprite = skin.frame;
            diamondLine.type = Image.Type.Sliced;
            diamondLine.color = new Color(0.784f, 0.643f, 0.361f, 0.08f);
            diamondLine.raycastTarget = false;

            vignette = Make("Vignette", stage, Color.black);
            vignette.sprite = skin.vignette;
            Stretch(vignette.rectTransform);
        }

        private void BuildShock()
        {
            // Harter Lichtkern, nicht der weiche Schein: der Verlauf im Handoff ist
            // bei 62 % Radius schon durchsichtig. Mit dem weichen Schein läuft das
            // ganze Bild milchig zu.
            flash = Make("Flash", stage, new Color(0f, 0f, 0f, 0f));
            flash.sprite = skin.flare;
            flash.rectTransform.anchoredPosition = new Vector2(0f, H * 0.5f - CY);

            shockRing = Make("ShockRing", stage, new Color(0f, 0f, 0f, 0f));
            shockRing.sprite = skin.frame;
            shockRing.type = Image.Type.Sliced;
            shockRing.rectTransform.anchoredPosition = new Vector2(0f, H * 0.5f - CY);
            shockRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        private void BuildText()
        {
            eyebrow = MakeRect("Eyebrow", stage);
            eyebrow.anchorMin = eyebrow.anchorMax = new Vector2(0.5f, 1f);
            eyebrow.sizeDelta = new Vector2(700f, 20f);
            eyebrow.anchoredPosition = new Vector2(0f, -74f);

            eyebrowLeft = MakeRect("RuleLeft", eyebrow);
            eyebrowLeft.sizeDelta = new Vector2(80f, 1f);
            eyebrowLeft.anchoredPosition = new Vector2(-150f, 0f);
            var leftImage = eyebrowLeft.gameObject.AddComponent<Image>();
            leftImage.sprite = skin.rule; leftImage.raycastTarget = false;

            eyebrowRight = MakeRect("RuleRight", eyebrow);
            eyebrowRight.sizeDelta = new Vector2(80f, 1f);
            eyebrowRight.anchoredPosition = new Vector2(150f, 0f);
            var rightImage = eyebrowRight.gameObject.AddComponent<Image>();
            rightImage.sprite = skin.rule; rightImage.raycastTarget = false;
            eyebrowRight.localEulerAngles = new Vector3(0f, 0f, 180f);

            eyebrowText = MakeText("Label", eyebrow, skin.oswald, 14f, Color.white);
            eyebrowText.characterSpacing = 40f;
            eyebrowText.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)eyebrowText.transform, 260f, 20f, 0f);

            headline = MakeText("Headline", stage, skin.cinzel, 46f, Hex("#F1DFB8"));
            headline.alignment = TextAlignmentOptions.Center;
            headline.characterSpacing = 6f;
            var headlineRect = (RectTransform)headline.transform;
            headlineRect.anchorMin = headlineRect.anchorMax = new Vector2(0.5f, 1f);
            headlineRect.sizeDelta = new Vector2(1100f, 60f);

            rankName = MakeText("RankName", stage, skin.cinzel, 62f, Color.white);
            rankName.alignment = TextAlignmentOptions.Center;
            rankName.characterSpacing = 7f;
            var nameRect = (RectTransform)rankName.transform;
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(1100f, 80f);

            // Stufenpips in eigenem Band über dem Balken — nie im Emblem, sonst
            // sind sie von dessen Eck-Pips nicht zu unterscheiden
            pipRow = MakeRect("PipRow", stage);
            pipRow.anchorMin = pipRow.anchorMax = new Vector2(0.5f, 1f);
            pipRow.sizeDelta = new Vector2(200f, 16f);
            pipRow.anchoredPosition = new Vector2(0f, -462f);
            for (int i = 0; i < 5; i++)
            {
                var pip = Make("Pip" + i, pipRow, Color.white);
                pip.sprite = skin.square;
                pip.rectTransform.sizeDelta = new Vector2(13f, 13f);
                pip.rectTransform.anchoredPosition = new Vector2((i - 2) * 22f, 0f);
                pip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                pips.Add(pip);
            }

            gainChip = MakeRect("GainChip", stage);
            gainChip.anchorMin = gainChip.anchorMax = new Vector2(0.5f, 1f);
            gainChip.sizeDelta = new Vector2(190f, 42f);
            gainChip.anchoredPosition = new Vector2(0f, -456f);
            var chipBg = gainChip.gameObject.AddComponent<Image>();
            chipBg.color = new Color(0f, 0f, 0f, 0.5f); chipBg.raycastTarget = false;
            var chipFrame = Make("Frame", gainChip, Hex("#7ACD96"));
            chipFrame.sprite = skin.frame; chipFrame.type = Image.Type.Sliced;
            Stretch(chipFrame.rectTransform);
            var chipGem = Make("Gem", gainChip, Hex("#7ACD96"));
            chipGem.sprite = skin.square;
            chipGem.rectTransform.sizeDelta = new Vector2(9f, 9f);
            chipGem.rectTransform.anchoredPosition = new Vector2(-62f, 0f);
            chipGem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            gainText = MakeText("Label", gainChip, skin.cinzel, 20f, Hex("#A8E4BE"));
            gainText.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)gainText.transform, 140f, 26f, 0f);
            ((RectTransform)gainText.transform).anchoredPosition = new Vector2(12f, 0f);
        }

        private void BuildRpBar()
        {
            rpBar = MakeRect("RpBar", stage);
            rpBar.anchorMin = rpBar.anchorMax = new Vector2(0.5f, 1f);
            rpBar.sizeDelta = new Vector2(560f, 52f);
            rpBar.anchoredPosition = new Vector2(0f, -508f);

            rpLabel = MakeText("Label", rpBar, skin.oswald, 13f, Color.white);
            rpLabel.characterSpacing = 26f;
            rpLabel.alignment = TextAlignmentOptions.Left;
            Strip((RectTransform)rpLabel.transform, 340f, 16f, 18f);
            ((RectTransform)rpLabel.transform).anchoredPosition = new Vector2(-110f, 18f);

            rpValue = MakeText("Value", rpBar, skin.spectral, 15f, Hex("#9C8A6A"));
            rpValue.alignment = TextAlignmentOptions.Right;
            Strip((RectTransform)rpValue.transform, 300f, 18f, 18f);
            ((RectTransform)rpValue.transform).anchoredPosition = new Vector2(130f, 18f);

            rpTrack = Make("Track", rpBar, new Color(0f, 0f, 0f, 0.5f));
            rpTrack.rectTransform.sizeDelta = new Vector2(560f, 16f);
            rpTrack.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            var track = rpTrack;

            rpFill = MakeRect("Fill", track.rectTransform);
            rpFill.anchorMin = new Vector2(0f, 0f);
            rpFill.anchorMax = new Vector2(0f, 1f);
            rpFill.pivot = new Vector2(0f, 0.5f);
            rpFill.anchoredPosition = Vector2.zero;
            // sizeDelta.y bleibt 0: senkrecht gestreckt heisst der Wert Zuschlag,
            // nicht Höhe — sonst wird die Füllung doppelt so hoch wie die Schiene
            rpFill.sizeDelta = new Vector2(0f, 0f);
            var fillImage = rpFill.gameObject.AddComponent<Image>();
            fillImage.raycastTarget = false;

            rpSheen = MakeRect("Sheen", track.rectTransform);
            Stretch(rpSheen);
            var sheenImage = rpSheen.gameObject.AddComponent<Image>();
            sheenImage.sprite = skin.rule;
            sheenImage.raycastTarget = false;

            rpBarFrame = Make("Frame", track.rectTransform, Color.white);
            rpBarFrame.sprite = skin.frame;
            rpBarFrame.type = Image.Type.Sliced;
            Stretch(rpBarFrame.rectTransform);
        }

        private void BuildRewards()
        {
            rewardRow = MakeRect("Rewards", stage);
            rewardRow.anchorMin = rewardRow.anchorMax = new Vector2(0.5f, 1f);
            rewardRow.sizeDelta = new Vector2(900f, 44f);
            rewardRow.anchoredPosition = new Vector2(0f, -588f);
            var layout = rewardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;    // ContentSizeFitter am Kind funktioniert hier nicht
            layout.childControlHeight = true;

            for (int i = 0; i < 3; i++)
            {
                var chip = MakeRect("Reward" + i, rewardRow);
                var element = chip.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 230f;
                element.preferredHeight = 42f;
                var bg = chip.gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.45f); bg.raycastTarget = false;
                var frame = Make("Frame", chip, Color.white);
                frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
                Stretch(frame.rectTransform);
                var gem = Make("Gem", chip, Color.white);
                gem.sprite = skin.square;
                gem.rectTransform.sizeDelta = new Vector2(8f, 8f);
                gem.rectTransform.anchorMin = gem.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                gem.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                gem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                var label = MakeText("Label", chip, skin.spectral, 15f, Hex("#C8B189"));
                label.alignment = TextAlignmentOptions.Left;
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(32f, 0f); labelRect.offsetMax = new Vector2(-12f, 0f);
                rewards.Add(chip);
            }
        }

        private void BuildOverlays()
        {
            wash = Make("Wash", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(wash.rectTransform);
            blackout = Make("Blackout", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(blackout.rectTransform);
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
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image Make(string name, RectTransform parent, Color color)
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

    /// <summary>
    /// Die RP-Bänder der zehn Ränge. Sie stehen so im Handoff und müssen zur
    /// Servertabelle in <c>Server/ranks.js</c> passen — hier wird nur angezeigt.
    /// </summary>
    public static class RankBands
    {
        public readonly struct Band
        {
            public readonly int Lo, Hi;   // Hi = 0 an der Spitze (kein Deckel)
            public Band(int lo, int hi) { Lo = lo; Hi = hi; }
        }

        private static readonly Band[] Bands =
        {
            new Band(0, 400), new Band(400, 800), new Band(800, 1200), new Band(1200, 1600),
            new Band(1600, 2100), new Band(2100, 2600), new Band(2600, 3200), new Band(3200, 3800),
            new Band(3800, 4500), new Band(4500, 0),
        };

        public static Band Of(int rank) => Bands[Mathf.Clamp(rank, 1, 10) - 1];
    }
}
