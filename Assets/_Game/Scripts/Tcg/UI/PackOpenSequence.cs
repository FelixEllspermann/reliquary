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
    /// Das Pack-Öffnen (Handoff „Animations", Abschnitt 2). Fünf Szenen, 10,8 s:
    ///   Seal 1.8 — das versiegelte Pack zittert
    ///   Tear 1.3 — es reisst in zwei Hälften
    ///   Fan  1.9 — fünf Karten fahren aus dem Aufschlag in ihre Plätze
    ///   Flip 2.6 — sie drehen um, versetzt um 0,09 s
    ///   Hold 3.2 — das beste Stück hebt ab, dann die Bilanz
    ///
    /// Die Entscheidung, um die alles gebaut ist: <b>die Seltenheit steht im
    /// Feuer, bevor die Karte umdreht</b>. Die Spannung liegt im Brennen, nicht im
    /// Aufdecken. Sobald eine Karte umschlägt, verlöschen ihre Flammen und der
    /// Rahmen übernimmt den Hinweis — nie beides zugleich.
    ///
    /// Alles ist vor dem ersten Bild entschieden: welche Karten, welche Finishes.
    /// Hier wird nichts gewürfelt, und es gibt keinen Zufall in der Bewegung.
    /// </summary>
    public class PackOpenSequence : MonoBehaviour
    {
        private const float W = 1280f, H = 720f;
        private const float CY = 336f;                 // Kartenmitte von oben
        private const float CardW = 176f, CardH = 246f;
        private const float Gap = 22f;
        private const float PackW = 244f, PackH = 342f;

        private static readonly float[] Durations = { 1.8f, 1.3f, 1.9f, 2.6f, 3.2f };

        /// <summary>Läuft gerade eine Öffnung?</summary>
        public static bool Playing { get; private set; }

        private static PackOpenSequence instance;

        /// <summary>Eine gezogene Karte, wie der Server sie geschickt hat.</summary>
        private class Pull
        {
            public CardDefinition Definition;
            public CardFinish Finish;
            public CardInstance Instance;
            public TcgCardView View;
            public FlameField Flames;
            public RectTransform Holder;   // trägt Position, Drehung und Skalierung
            public Image Halo;
            public TMP_Text Tag;
            public Color Colour;
            public float Pulse;            // 0 Common … 1 Legendary
            public bool FaceUp;
        }

        private readonly List<Pull> pulls = new List<Pull>();

        private CanvasGroup group;
        private RectTransform stage;
        private TransitionSkin skin;
        private TcgCardView cardPrefab;
        private CardCatalog catalog;

        private Image tableGlow, weave, vignette, wash, flash, shockRing, blackout;
        private RectTransform packRoot, packLeft, packRight;
        private readonly List<Image> packHalves = new List<Image>();
        private RectTransform eyebrow, eyebrowLeft, eyebrowRight, chipRow, heroChip;
        private TMP_Text eyebrowText, headline, subline, footnote, heroChipText;
        private readonly List<RectTransform> chips = new List<RectTransform>();

        private Action finished;
        private string packName = "Sealed Pack";
        private bool skipRequested;

        // ================== START ==================

        /// <summary>
        /// Spielt die Öffnung. Die Karten stehen fest, bevor das erste Bild läuft.
        /// <paramref name="onDone"/> feuert am Ende — auch beim Überspringen.
        /// </summary>
        public static void Play(TcgCardView prefab, CardCatalog cardCatalog,
                                string pack, string[] cardNames, int[] finishes, Action onDone)
        {
            if (prefab == null || cardCatalog == null || cardNames == null)
            {
                onDone?.Invoke();
                return;
            }
            if (instance == null)
            {
                // Eigener Canvas mit eigener Bezugsauflösung: die Sequenz ist in
                // 1280×720 gezeichnet. Hinge sie im Shop-Canvas (1920×1080), käme
                // alles auf zwei Dritteln der gedachten Größe heraus.
                var go = new GameObject("~PackOpen");
                instance = go.AddComponent<PackOpenSequence>();
                instance.cardPrefab = prefab;
                instance.catalog = cardCatalog;
                instance.Build();
            }
            instance.cardPrefab = prefab;
            instance.catalog = cardCatalog;
            instance.StartSequence(pack, cardNames, finishes, onDone);
        }

        private void StartSequence(string pack, string[] cardNames, int[] finishes, Action onDone)
        {
            StopAllCoroutines();
            finished = onDone;
            packName = string.IsNullOrEmpty(pack) ? "Sealed Pack" : pack;
            skipRequested = false;
            BuildPulls(cardNames, finishes);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StartCoroutine(Run());
        }

        private void BuildPulls(string[] cardNames, int[] finishes)
        {
            foreach (var pull in pulls)
                if (pull.Holder != null) DestroyImmediate(pull.Holder.gameObject);
            pulls.Clear();

            for (int i = 0; i < cardNames.Length && i < 5; i++)
            {
                var definition = catalog.FindByName(cardNames[i]);
                if (definition == null) continue;

                var finish = (CardFinish)(finishes != null && i < finishes.Length
                    ? Mathf.Clamp(finishes[i], 0, CardFinishInfo.Count - 1) : 0);

                var holder = MakeRect("Pull" + i, stage);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);

                var pull = new Pull
                {
                    Definition = definition,
                    Finish = finish,
                    Instance = new CardInstance(definition, null) { Zone = ZoneType.Hand, Finish = finish },
                    Holder = holder,
                    Colour = CollectionRow.RarityStrong(definition.rarity),
                    Pulse = PulseOf(definition.rarity),
                };

                // Der Schein sitzt HINTER der Karte und brennt, solange sie verdeckt ist
                pull.Halo = Make("Halo", holder, Motion.Alpha(pull.Colour, 0f));
                pull.Halo.sprite = skin.glow;
                pull.Halo.rectTransform.sizeDelta = new Vector2(
                    CardW * Mathf.Lerp(1.5f, 2.1f, pull.Pulse),
                    CardH * Mathf.Lerp(1.25f, 1.6f, pull.Pulse));

                pull.Flames = FlameField.Build(holder, pull.Colour, pull.Pulse, skin.glow);

                pull.View = Instantiate(cardPrefab, holder);
                var viewRect = (RectTransform)pull.View.transform;
                viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
                viewRect.pivot = new Vector2(0.5f, 0.5f);
                viewRect.anchoredPosition = Vector2.zero;
                viewRect.sizeDelta = new Vector2(CardW, CardH);
                pull.View.Show(pull.Instance, true, upright: true);   // verdeckt starten
                pull.View.SetHighlight(false);
                pull.FaceUp = false;

                pull.Tag = MakeText("Tag", stage, skin.oswald, 11f, pull.Colour);
                pull.Tag.characterSpacing = 24f;
                pull.Tag.alignment = TextAlignmentOptions.Center;
                var tagRect = (RectTransform)pull.Tag.transform;
                tagRect.anchorMin = tagRect.anchorMax = new Vector2(0.5f, 1f);
                tagRect.sizeDelta = new Vector2(200f, 16f);

                pulls.Add(pull);
            }
        }

        /// <summary>Wie stark eine Seltenheit brennt — 0 Common bis 1 Legendary.</summary>
        private static float PulseOf(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return 0.25f;
                case CardRarity.Rare: return 0.5f;
                case CardRarity.Legendary: return 1f;
                default: return 0f;
            }
        }

        private static float SlotX(int index) => (index - 2) * (CardW + Gap);

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
                    // Überspringen führt an den Anfang von Hold — nie über den Flip
                    // hinaus, sonst hätte man die Karten nie umdrehen sehen.
                    if (PollSkip() && scene < 4)
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

        private bool PollSkip()
        {
            if (skipRequested) { skipRequested = false; return true; }
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
                case 0: SceneSeal(p); break;
                case 1: SceneTear(p); break;
                case 2: SceneFan(p); break;
                case 3: SceneFlip(p); break;
                default: SceneHold(p); break;
            }
        }

        /// <summary>Alles aus — jede Szene schaltet danach an, was sie braucht.</summary>
        private void Clear()
        {
            packRoot.gameObject.SetActive(false);
            eyebrow.gameObject.SetActive(false);
            headline.gameObject.SetActive(false);
            subline.gameObject.SetActive(false);
            footnote.gameObject.SetActive(false);
            chipRow.gameObject.SetActive(false);
            heroChip.gameObject.SetActive(false);
            flash.gameObject.SetActive(false);
            shockRing.gameObject.SetActive(false);
            wash.gameObject.SetActive(false);
            blackout.gameObject.SetActive(false);
            foreach (var pull in pulls)
            {
                pull.Holder.gameObject.SetActive(false);
                pull.Tag.gameObject.SetActive(false);
            }
        }

        // ---- 1 · Seal: das Pack zittert ----
        private void SceneSeal(float p)
        {
            float inn = Motion.Enter(Motion.Seg(p, 0.04f, 0.36f));
            float tension = Motion.Seg(p, 0.3f, 1f);

            Table(Motion.Mix(1.02f, 1.06f, Motion.Drift(p)));
            SetPack(0f, tension * 0.5f, 0.1f + tension * 0.35f, inn);
            SetEyebrow("SEALED PACK", inn, Motion.Mix(14f, 0f, inn), Hex("#9C8A6A"));
            SetHeadline(packName, inn, Motion.Mix(16f, 0f, inn));
            SetSubline("Five cards. One guaranteed rare or better.",
                inn * (0.5f + Mathf.Sin(Mathf.PI * 2f * tension) * 0.3f + 0.2f));
            SetBlackout(1f - Motion.Enter(Motion.Seg(p, 0f, 0.2f)));
        }

        // ---- 2 · Tear: es reisst auf ----
        private void SceneTear(float p)
        {
            float split = Motion.Seg(p, 0.12f, 1f);
            float burst = Motion.Arc(p, 0.1f, 0.7f);
            float shock = Motion.Seg(p, 0.14f, 0.8f);

            Table(Motion.Mix(1.06f, 1.16f, Motion.Drift(p)));
            SetPack(split, 0.5f * (1f - Motion.Enter(split)),
                Motion.Mix(0.45f, 1f, Motion.Enter(Motion.Seg(p, 0f, 0.5f))), 1f);
            SetShock(shock);
            SetFlash(burst);
            SetWash(Motion.Enter(Motion.Seg(p, 0.84f, 1f)) * 0.42f);
        }

        // ---- 3 · Fan: die Karten fahren in ihre Plätze ----
        private void SceneFan(float p)
        {
            float wash = (1f - Motion.Enter(Motion.Seg(p, 0f, 0.3f))) * 0.42f;
            float tags = Motion.Enter(Motion.Seg(p, 0.62f, 1f));

            Table(Motion.Mix(1.16f, 1.02f, Motion.Drift(p)));
            for (int i = 0; i < pulls.Count; i++)
            {
                // Jede Karte verlässt den Aufschlag auf ihren eigenen Takt
                float t = Motion.Enter(Motion.Seg(p, 0.04f + i * 0.075f, 0.62f + i * 0.075f));
                var pull = pulls[i];
                if (t <= 0.001f) continue;
                Place(pull, Motion.Mix(0f, SlotX(i), t), Motion.Mix(-CY + 30f, -CY, t),
                    Motion.Mix((i - 2) * 22f, 0f, t), Motion.Mix(0.62f, 1f, t), 1f);
                SetGlow(pull, t, p);
                SetFace(pull, false, 0f);
                SetTag(pull, i, tags);
            }
            SetEyebrow("FIVE CARDS", Motion.Enter(Motion.Seg(p, 0.5f, 0.86f)),
                Motion.Mix(12f, 0f, Motion.Enter(Motion.Seg(p, 0.5f, 0.86f))), Hex("#9C8A6A"));
            SetWash(wash);
        }

        // ---- 4 · Flip: sie drehen um, versetzt ----
        private void SceneFlip(float p)
        {
            float tagsOut = 1f - Motion.Enter(Motion.Seg(p, 0.1f, 0.5f));

            Table(Motion.Mix(1.02f, 1.04f, Motion.Drift(p)));
            for (int i = 0; i < pulls.Count; i++)
            {
                var pull = pulls[i];
                float f = Motion.Seg(p, 0.06f + i * 0.09f, 0.5f + i * 0.09f);
                // Der Schein hält, bis die Karte über ihre Kante ist, dann
                // übernimmt der Rahmen — nie beides gleichzeitig
                float glow = (1f - Mathf.Clamp01(f / 0.55f)) * Motion.Mix(0.7f, 1f, pull.Pulse);
                float kick = Mathf.Sin(Mathf.PI * Mathf.Clamp01(f));

                Place(pull, SlotX(i), -CY - kick * Motion.Mix(10f, 26f, pull.Pulse), 0f,
                    1f + kick * 0.08f * Motion.Mix(0.5f, 1.4f, pull.Pulse), 1f);
                SetGlow(pull, glow, p);
                SetFace(pull, f >= 0.5f, f);
                SetTag(pull, i, tagsOut);
            }
            SetEyebrow("FIVE CARDS", 1f - Motion.Enter(Motion.Seg(p, 0f, 0.3f)),
                Motion.Mix(0f, -10f, Motion.Enter(Motion.Seg(p, 0f, 0.3f))), Hex("#9C8A6A"));
        }

        // ---- 5 · Hold: das beste Stück hebt ab, dann die Bilanz ----
        private void SceneHold(float p)
        {
            float inn = Motion.Enter(Motion.Seg(p, 0.06f, 0.36f));
            float chipsIn = Motion.Enter(Motion.Seg(p, 0.34f, 0.6f));
            float outro = 1f - Motion.Enter(Motion.Seg(p, 0.94f, 1f));
            float breathe = Mathf.Sin(Mathf.PI * 2f * Motion.Seg(p, 0.1f, 1f) - Mathf.PI * 0.5f) * 0.5f + 0.5f;

            int hero = BestIndex();
            Table(Motion.Mix(1.04f, 1.02f, Motion.Drift(p)));
            for (int i = 0; i < pulls.Count; i++)
            {
                var pull = pulls[i];
                bool isHero = i == hero;
                Place(pull, SlotX(i),
                    -CY - (isHero ? Motion.Mix(0f, 22f, inn) + breathe * 5f : 0f), 0f,
                    isHero ? Motion.Mix(1f, 1.07f, inn) : 1f, 1f);
                SetGlow(pull, isHero ? (0.34f + breathe * 0.26f) * outro : 0.1f * outro, p);
                SetFace(pull, true, 1f);
            }

            if (hero >= 0) SetHeroChip(pulls[hero], hero, inn * outro, breathe);
            SetEyebrow("PACK OPENED", inn * outro, Motion.Mix(12f, 0f, inn), Hex("#9C8A6A"));
            SetChips(chipsIn * outro);
            SetFootnote("Turn duplicates into crafting material in the Deck Builder.",
                chipsIn * outro * 0.9f);
            SetBlackout(Motion.Enter(Motion.Seg(p, 0.94f, 1f)));
        }

        /// <summary>Das seltenste Exemplar; bei Gleichstand das mit dem besseren Finish.</summary>
        private int BestIndex()
        {
            int best = -1;
            float score = -1f;
            for (int i = 0; i < pulls.Count; i++)
            {
                float value = pulls[i].Pulse * 10f + (int)pulls[i].Finish;
                if (value <= score) continue;
                score = value;
                best = i;
            }
            return best;
        }

        // ================== BÜHNE STELLEN ==================

        private void Table(float scale)
        {
            tableGlow.rectTransform.localScale = Vector3.one * scale;
        }

        private void Place(Pull pull, float x, float y, float rotation, float scale, float alpha)
        {
            pull.Holder.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            pull.Holder.anchoredPosition = new Vector2(x, y);
            pull.Holder.localEulerAngles = new Vector3(0f, 0f, rotation);
            pull.Holder.localScale = Vector3.one * scale;
        }

        private void SetGlow(Pull pull, float amount, float t)
        {
            pull.Halo.color = Motion.Alpha(pull.Colour, 0.26f * Mathf.Clamp01(amount));
            pull.Flames.Apply(amount, t);
        }

        /// <summary>
        /// Die Drehung. uGUI im Overlay-Canvas kennt keine Perspektive, deshalb
        /// staucht die Karte über die Y-Achse — optisch dasselbe wie ein Flip ohne
        /// Fluchtpunkt. Das Gesicht wechselt genau bei 90°.
        /// </summary>
        private void SetFace(Pull pull, bool faceUp, float flip)
        {
            if (faceUp != pull.FaceUp)
            {
                pull.View.Show(pull.Instance, !faceUp, upright: true);
                pull.FaceUp = faceUp;
                // Das Finish erscheint im selben Moment wie das Bild — die
                // Kartenansicht setzt es selbst, sobald sie das Gesicht zeigt.
                if (faceUp) pull.View.SetHighlight(true, pull.Colour);
            }
            float angle = Motion.Drift(Mathf.Clamp01(flip)) * 180f;
            var viewRect = (RectTransform)pull.View.transform;
            viewRect.localScale = new Vector3(Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad)), 1f, 1f);
        }

        private void SetTag(Pull pull, int index, float alpha)
        {
            pull.Tag.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            pull.Tag.text = RarityName(pull.Definition.rarity);
            pull.Tag.color = Motion.Alpha(pull.Colour, alpha * Motion.Mix(0.5f, 1f, pull.Pulse));
            ((RectTransform)pull.Tag.transform).anchoredPosition =
                new Vector2(SlotX(index), -(CY + CardH * 0.5f + 30f));
        }

        private static string RarityName(CardRarity rarity) =>
            CardDefinition.RarityName(rarity).ToUpperInvariant();

        private void SetPack(float split, float shake, float glow, float fade)
        {
            packRoot.gameObject.SetActive(fade > 0.001f);
            if (fade <= 0.001f) return;

            float s = Motion.Enter(split);
            float wobbleX = Mathf.Sin(shake * Mathf.PI * 14f) * shake * 7f;
            float wobbleR = Mathf.Sin(shake * Mathf.PI * 9f) * shake * 2f;
            packRoot.anchoredPosition = new Vector2(wobbleX, -CY);
            packRoot.localEulerAngles = new Vector3(0f, 0f, wobbleR);

            // Die Ruhelage der Hälfte (±PackW/4) muss drin bleiben — sonst fallen
            // beide Fenster aufeinander und das Pack wird zum halbbreiten Streifen
            float alpha = (1f - Mathf.Clamp01(split / 0.85f)) * fade;
            packLeft.anchoredPosition = new Vector2(-PackW * 0.25f - s * PackW * 0.85f, 0f);
            packLeft.localEulerAngles = new Vector3(0f, 0f, s * 13f);
            packRight.anchoredPosition = new Vector2(PackW * 0.25f + s * PackW * 0.85f, 0f);
            packRight.localEulerAngles = new Vector3(0f, 0f, -s * 13f);

            foreach (var half in packHalves)
            {
                var colour = half.color;
                half.color = new Color(colour.r, colour.g, colour.b, alpha * PackAlpha(half.name));
            }
        }

        /// <summary>Grunddeckkraft der einzelnen Packlagen, damit sie gemeinsam faden.</summary>
        private static float PackAlpha(string name)
        {
            switch (name)
            {
                case "Weave": return 0.14f;
                case "Keyline": return 0.45f;
                case "Diamond": return 0.6f;
                case "Crest": return 0.36f;
                default: return 1f;
            }
        }

        private void SetEyebrow(string text, float alpha, float rise, Color tone)
        {
            eyebrow.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            eyebrowText.text = text;
            eyebrowText.color = Motion.Alpha(tone, alpha);
            eyebrowLeft.GetComponent<Image>().color = Motion.Alpha(tone, alpha * 0.7f);
            eyebrowRight.GetComponent<Image>().color = Motion.Alpha(tone, alpha * 0.7f);
            eyebrow.anchoredPosition = new Vector2(0f, -66f - rise);
        }

        private void SetHeadline(string text, float alpha, float rise)
        {
            headline.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            headline.text = text;
            headline.color = Motion.Alpha(Hex("#F1DFB8"), alpha);
            ((RectTransform)headline.transform).anchoredPosition = new Vector2(0f, -108f - rise);
        }

        private void SetSubline(string text, float alpha)
        {
            subline.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            subline.text = text;
            subline.color = Motion.Alpha(Hex("#A2917A"), Mathf.Clamp01(alpha));
        }

        private void SetFootnote(string text, float alpha)
        {
            footnote.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            footnote.text = text;
            footnote.color = Motion.Alpha(Hex("#9C8A6A"), alpha);
        }

        private void SetHeroChip(Pull hero, int index, float alpha, float breathe)
        {
            heroChip.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            heroChipText.text = $"{RarityName(hero.Definition.rarity)} · NEW";
            heroChipText.color = Motion.Alpha(Hex("#F8EED6"), alpha);
            heroChip.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f * alpha);
            heroChip.Find("Frame").GetComponent<Image>().color = Motion.Alpha(hero.Colour, 0.75f * alpha);
            heroChip.Find("Gem").GetComponent<Image>().color = Motion.Alpha(hero.Colour, alpha);
            heroChip.anchoredPosition = new Vector2(SlotX(index), -(CY - CardH * 0.5f - 60f));
        }

        private void SetChips(float alpha)
        {
            chipRow.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;

            int fresh = 0, special = 0;
            foreach (var pull in pulls)
            {
                if (PlayerProfile.Owned(pull.Definition.cardName) <= 1) fresh++;
                if (pull.Finish != CardFinish.Plain) special++;
            }
            int duplicates = pulls.Count - fresh;

            var texts = new List<string>
            {
                fresh == 1 ? "1 new card" : $"{fresh} new cards",
                duplicates == 1 ? "1 duplicate stored" : $"{duplicates} duplicates stored",
            };
            var tones = new List<Color> { Hex("#7ACD96"), Hex("#EBCE8A") };
            if (special > 0)
            {
                texts.Add(special == 1 ? "1 special finish" : $"{special} special finishes");
                tones.Add(Hex("#B9A3E0"));
            }

            for (int i = 0; i < chips.Count; i++)
            {
                bool used = i < texts.Count;
                chips[i].gameObject.SetActive(used);
                if (!used) continue;
                chips[i].GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f * alpha);
                chips[i].Find("Frame").GetComponent<Image>().color = Motion.Alpha(tones[i], 0.5f * alpha);
                chips[i].Find("Gem").GetComponent<Image>().color = Motion.Alpha(tones[i], alpha);
                var label = chips[i].Find("Label").GetComponent<TMP_Text>();
                label.text = texts[i];
                label.color = Motion.Alpha(Hex("#C8B189"), alpha);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(chipRow);
        }

        private void SetFlash(float amount)
        {
            flash.gameObject.SetActive(amount > 0.001f);
            if (amount <= 0.001f) return;
            flash.color = Motion.Alpha(Hex("#F8EED6"), amount * 0.55f);
            float size = 220f + amount * 1800f;
            flash.rectTransform.sizeDelta = new Vector2(size, size);
        }

        private void SetShock(float amount)
        {
            shockRing.gameObject.SetActive(amount > 0.001f && amount < 1f);
            if (!shockRing.gameObject.activeSelf) return;
            float size = Motion.Mix(200f, 980f, Motion.Enter(amount));
            shockRing.rectTransform.sizeDelta = new Vector2(size, size);
            shockRing.color = Motion.Alpha(Hex("#F8EED6"), (1f - amount) * 0.72f);
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

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            return colour;
        }

        // ================== AUFBAU ==================

        private void Build()
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 480;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            stage = (RectTransform)canvasGo.transform;

            // Deckender Boden ZUERST: der Tisch-Glow darüber ist ein radialer
            // Sprite und an den Rändern durchsichtig — ohne diese Platte schien
            // der Shop durch und machte die Sequenz schwer lesbar.
            var floor = Make("Floor", stage, Hex("#120E09"));
            Stretch(floor.rectTransform, -60f);

            tableGlow = Make("TableGlow", stage, Hex("#2A1C12"));
            tableGlow.sprite = skin.glow;
            Stretch(tableGlow.rectTransform, -60f);

            weave = Make("Weave", stage, new Color(0.784f, 0.643f, 0.361f, 0.045f));
            weave.sprite = skin.weave;
            weave.type = Image.Type.Tiled;
            Stretch(weave.rectTransform);

            vignette = Make("Vignette", stage, Color.black);
            vignette.sprite = skin.vignette;
            Stretch(vignette.rectTransform);

            BuildPack();
            BuildText();
            BuildChips();

            flash = Make("Flash", stage, new Color(0f, 0f, 0f, 0f));
            flash.sprite = skin.flare;
            flash.rectTransform.anchorMin = flash.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            flash.rectTransform.anchoredPosition = new Vector2(0f, -CY);

            shockRing = Make("ShockRing", stage, new Color(0f, 0f, 0f, 0f));
            shockRing.sprite = skin.frame;
            shockRing.type = Image.Type.Sliced;
            shockRing.rectTransform.anchorMin = shockRing.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shockRing.rectTransform.anchoredPosition = new Vector2(0f, -CY);
            shockRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            wash = Make("Wash", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(wash.rectTransform);
            blackout = Make("Blackout", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(blackout.rectTransform);

            gameObject.SetActive(false);
        }

        private void BuildPack()
        {
            packRoot = MakeRect("Pack", stage);
            packRoot.anchorMin = packRoot.anchorMax = new Vector2(0.5f, 1f);
            packRoot.sizeDelta = new Vector2(PackW, PackH);
            packRoot.anchoredPosition = new Vector2(0f, -CY);

            packLeft = BuildPackHalf("Left", -1f);
            packRight = BuildPackHalf("Right", 1f);
        }

        /// <summary>
        /// Eine Packhälfte: ein Maskenfenster, in dem eine ganze Packgrafik sitzt.
        /// Dadurch reisst die Zeichnung mit, statt dass zwei fertige Hälften
        /// auseinanderfahren.
        /// </summary>
        private RectTransform BuildPackHalf(string name, float side)
        {
            var window = MakeRect("Half" + name, packRoot);
            window.sizeDelta = new Vector2(PackW * 0.5f, PackH);
            window.anchoredPosition = new Vector2(side * PackW * 0.25f, 0f);
            window.gameObject.AddComponent<RectMask2D>();

            var inner = MakeRect("Body", window);
            inner.sizeDelta = new Vector2(PackW, PackH);
            inner.anchoredPosition = new Vector2(-side * PackW * 0.25f, 0f);

            var body = inner.gameObject.AddComponent<Image>();
            body.sprite = skin.diagFade;
            body.color = Hex("#4E2A18");
            body.raycastTarget = false;
            packHalves.Add(body);

            var border = Make("Border", inner, Hex("#C8A45C"));
            border.sprite = skin.frame; border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            packHalves.Add(border);

            var pattern = Make("Weave", inner, Hex("#C8A45C"));
            pattern.sprite = skin.weave; pattern.type = Image.Type.Tiled;
            Stretch(pattern.rectTransform);
            packHalves.Add(pattern);

            var keyline = Make("Keyline", inner, Hex("#C8A45C"));
            keyline.sprite = skin.frame; keyline.type = Image.Type.Sliced;
            Stretch(keyline.rectTransform, 7f);
            packHalves.Add(keyline);

            var diamond = Make("Diamond", inner, Hex("#C8A45C"));
            diamond.sprite = skin.frame; diamond.type = Image.Type.Sliced;
            diamond.rectTransform.sizeDelta = new Vector2(120f, 120f);
            diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            packHalves.Add(diamond);

            var crest = Make("Crest", inner, Hex("#C8A45C"));
            crest.sprite = skin.diagFade;
            crest.rectTransform.sizeDelta = new Vector2(58f, 58f);
            crest.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            packHalves.Add(crest);

            var core = Make("Core", inner, Hex("#EBCE8A"));
            core.sprite = skin.diagFade;
            core.rectTransform.sizeDelta = new Vector2(24f, 24f);
            core.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            packHalves.Add(core);

            return window;
        }

        private void BuildText()
        {
            eyebrow = MakeRect("Eyebrow", stage);
            eyebrow.anchorMin = eyebrow.anchorMax = new Vector2(0.5f, 1f);
            eyebrow.sizeDelta = new Vector2(700f, 20f);
            eyebrow.anchoredPosition = new Vector2(0f, -66f);

            eyebrowLeft = MakeRect("RuleLeft", eyebrow);
            eyebrowLeft.sizeDelta = new Vector2(76f, 1f);
            eyebrowLeft.anchoredPosition = new Vector2(-148f, 0f);
            var left = eyebrowLeft.gameObject.AddComponent<Image>();
            left.sprite = skin.rule; left.raycastTarget = false;

            eyebrowRight = MakeRect("RuleRight", eyebrow);
            eyebrowRight.sizeDelta = new Vector2(76f, 1f);
            eyebrowRight.anchoredPosition = new Vector2(148f, 0f);
            var right = eyebrowRight.gameObject.AddComponent<Image>();
            right.sprite = skin.rule; right.raycastTarget = false;
            eyebrowRight.localEulerAngles = new Vector3(0f, 0f, 180f);

            eyebrowText = MakeText("Label", eyebrow, skin.oswald, 14f, Color.white);
            eyebrowText.characterSpacing = 40f;
            eyebrowText.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)eyebrowText.transform, 280f, 20f, 0f);

            headline = MakeText("Headline", stage, skin.cinzel, 44f, Hex("#F1DFB8"));
            headline.alignment = TextAlignmentOptions.Center;
            headline.characterSpacing = 6f;
            var headlineRect = (RectTransform)headline.transform;
            headlineRect.anchorMin = headlineRect.anchorMax = new Vector2(0.5f, 1f);
            headlineRect.sizeDelta = new Vector2(1100f, 58f);

            subline = MakeText("Subline", stage, skin.spectral, 17f, Hex("#A2917A"));
            subline.alignment = TextAlignmentOptions.Center;
            var sublineRect = (RectTransform)subline.transform;
            sublineRect.anchorMin = sublineRect.anchorMax = new Vector2(0.5f, 0f);
            sublineRect.sizeDelta = new Vector2(900f, 24f);
            sublineRect.anchoredPosition = new Vector2(0f, 74f);

            footnote = MakeText("Footnote", stage, skin.spectral, 15f, Hex("#9C8A6A"));
            footnote.alignment = TextAlignmentOptions.Center;
            var footRect = (RectTransform)footnote.transform;
            footRect.anchorMin = footRect.anchorMax = new Vector2(0.5f, 1f);
            footRect.sizeDelta = new Vector2(900f, 22f);
            footRect.anchoredPosition = new Vector2(0f, -(CY + CardH * 0.5f + 108f));

            heroChip = MakeRect("HeroChip", stage);
            heroChip.anchorMin = heroChip.anchorMax = new Vector2(0.5f, 1f);
            heroChip.sizeDelta = new Vector2(190f, 38f);
            var heroBg = heroChip.gameObject.AddComponent<Image>();
            heroBg.color = new Color(0f, 0f, 0f, 0.55f); heroBg.raycastTarget = false;
            var heroFrame = Make("Frame", heroChip, Color.white);
            heroFrame.sprite = skin.frame; heroFrame.type = Image.Type.Sliced;
            Stretch(heroFrame.rectTransform);
            var heroGem = Make("Gem", heroChip, Color.white);
            heroGem.sprite = skin.square;
            heroGem.rectTransform.sizeDelta = new Vector2(9f, 9f);
            heroGem.rectTransform.anchorMin = heroGem.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            heroGem.rectTransform.anchoredPosition = new Vector2(18f, 0f);
            heroGem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            heroChipText = MakeText("Label", heroChip, skin.cinzel, 16f, Hex("#F8EED6"));
            heroChipText.characterSpacing = 14f;
            heroChipText.alignment = TextAlignmentOptions.Center;
            var heroTextRect = (RectTransform)heroChipText.transform;
            heroTextRect.anchorMin = Vector2.zero; heroTextRect.anchorMax = Vector2.one;
            heroTextRect.offsetMin = new Vector2(28f, 0f); heroTextRect.offsetMax = new Vector2(-10f, 0f);
        }

        private void BuildChips()
        {
            chipRow = MakeRect("Chips", stage);
            chipRow.anchorMin = chipRow.anchorMax = new Vector2(0.5f, 1f);
            chipRow.sizeDelta = new Vector2(900f, 44f);
            chipRow.anchoredPosition = new Vector2(0f, -(CY + CardH * 0.5f + 52f));
            var layout = chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < 3; i++)
            {
                var chip = MakeRect("Chip" + i, chipRow);
                var element = chip.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 220f;
                element.preferredHeight = 40f;
                var bg = chip.gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.5f); bg.raycastTarget = false;
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
