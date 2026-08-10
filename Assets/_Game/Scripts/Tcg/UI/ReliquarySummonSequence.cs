using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die Extra-Deck-Beschwörung (Handoff „Animations", Abschnitt 6). Fünf Szenen:
    ///   Call    1.7 — der Extra-Deck-Platz leuchtet auf, ein Lichtfaden läuft zur
    ///                 Tresormitte, daraus wird das Siegel geboren
    ///   Unlock  1.8 — drei Rautenringe fahren aus ihrem Versatz in die Flucht,
    ///                 vier Schlösser beissen zu
    ///   Open    1.5 — die Platte zerfällt in vier Quadranten, Licht flutet heraus
    ///   Emerge  1.9 — die Karte steigt auf einem Lichtschaft und dreht dabei um
    ///   Present 3.2 — Banner, dann fährt sie auf Feldgrösse in ihre Zone
    ///
    /// Ein Reliquary kommt nicht aus der Hand, sondern aus dem Tresor. Die
    /// Beschwörung IST das Öffnen — deshalb ist das hier länger und lauter als
    /// eine gewöhnliche Aktivierung.
    ///
    /// <b>Die Geometrie entscheidet der Rahmen, nicht der Wunsch.</b> Ein
    /// ausgerichteter Ring ist ein um 45° gedrehtes Quadrat der Seite 2r und
    /// braucht deshalb 2·r·√2 px Höhe. Bei r = 213 sind das 602 px und passen in
    /// das nutzbare Band; bei r = 250 wären es 707 px — die obere Spitze fiele aus
    /// dem Bild, und zwar genau im Moment der Ausrichtung, also am Höhepunkt.
    /// </summary>
    public class ReliquarySummonSequence : MonoBehaviour
    {
        private const float W = 1280f, H = 720f;
        private const float VaultX = 0f, VaultY = 402f;   // Tresormitte, y von oben
        private const float R = 213f;                     // aus dem Rahmen abgeleitet
        private const float ExtraX = 266f, ExtraY = 452f; // Extra-Deck-Platz (906 − 640)
        private const float HeroW = 216f, HeroH = 302f;
        private const float FieldW = 132f, FieldH = 185f;
        private const float SlotX = -72f, SlotY = 452f;   // Zielzone (568 − 640)

        private static readonly float[] Durations = { 1.7f, 1.8f, 1.5f, 1.9f, 3.2f };

        private static readonly Color Violet = new Color(0.937f, 0.906f, 0.980f);   // #EFE7FA
        private static readonly Color Deep = new Color(0.369f, 0.306f, 0.549f);     // #5E4E8C
        private static readonly Color Gold = new Color(0.784f, 0.643f, 0.361f);
        private static readonly Color Dim = new Color(0.612f, 0.541f, 0.416f);

        public static bool Playing { get; private set; }
        private static ReliquarySummonSequence instance;

        private CanvasGroup group;
        private RectTransform stage;
        private TransitionSkin skin;

        private Image dim, flash, shockRing, thread, shaft, blackout;
        private RectTransform vaultRoot, cardHolder, bannerBlock, chipRow;
        private readonly List<RectTransform> rings = new List<RectTransform>();
        private readonly List<Image> ringImages = new List<Image>();
        private readonly List<RectTransform> locks = new List<RectTransform>();
        private readonly List<Image> lockImages = new List<Image>();
        private readonly List<RectTransform> plates = new List<RectTransform>();
        private readonly List<Image> plateImages = new List<Image>();
        private readonly List<RectTransform> motes = new List<RectTransform>();
        private readonly List<Image> moteImages = new List<Image>();
        private readonly List<RectTransform> chips = new List<RectTransform>();

        private TcgCardView cardView;
        private TMP_Text label, sublabel, bannerTop, bannerWord;
        private CardInstance card;
        private Action finished;
        private Vector2 source, destination;

        /// <summary>
        /// Rechnet einen Weltpunkt in die Bühnen-Konvention um: x von der Mitte,
        /// y VON OBEN (wie VaultY/ExtraY/SlotY). InverseTransformPoint liefert
        /// Mitte-basiert mit y nach oben — daher die Spiegelung um H/2. Wer hier
        /// das rohe local.y durchreicht, schickt die Karte am Ende der Fahrt um
        /// die Bildmitte gespiegelt auf die falsche Seite oder aus dem Bild.
        /// </summary>
        private Vector2 ToStage(Vector3 world)
        {
            var local = stage.InverseTransformPoint(world);
            return new Vector2(local.x, H * 0.5f - local.y);
        }

        /// <summary>Versatz, aus dem die drei Ringe in die Flucht fahren.</summary>
        private static readonly float[] RingOffsets = { -46f, 38f, -62f };
        private static readonly float[] RingScales = { 1f, 0.74f, 0.5f };

        // ================== START ==================

        public static void Play(TcgCardView prefab, CardInstance summoned,
                                Vector3? fromWorld, Vector3? toWorld, Action onDone = null)
        {
            if (prefab == null || summoned == null) { onDone?.Invoke(); return; }
            if (instance == null)
            {
                var host = new GameObject("~ReliquarySummon");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<ReliquarySummonSequence>();
                instance.Build(prefab);
            }
            instance.StartSequence(summoned, fromWorld, toWorld, onDone);
        }

        private void StartSequence(CardInstance summoned, Vector3? fromWorld, Vector3? toWorld,
                                   Action onDone)
        {
            StopAllCoroutines();
            finished = onDone;
            card = summoned;
            // Herkunft und Ziel kommen vom Brett: das Extra Deck DESSEN, der
            // beschwört, und die Zone, in die die Karte danach gelegt wird.
            // Beides in Bühnen-Konvention (x von der Mitte, y von oben).
            source = fromWorld.HasValue ? ToStage(fromWorld.Value) : new Vector2(ExtraX, ExtraY);
            destination = toWorld.HasValue ? ToStage(toWorld.Value) : new Vector2(SlotX, SlotY);
            cardView.Show(card, true, upright: true);   // verdeckt starten
            cardView.SetHighlight(false);
            gameObject.SetActive(true);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Playing = true;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            bool flipped = false;

            for (int scene = 0; scene < Durations.Length; scene++)
            {
                float duration = Durations[scene];
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    float p = Mathf.Clamp01(t / duration);
                    Frame(scene, p, ref flipped);
                    yield return null;
                }
                Frame(scene, 1f, ref flipped);
            }

            gameObject.SetActive(false);
            Playing = false;
            var callback = finished;
            finished = null;
            callback?.Invoke();
        }

        private void Frame(int scene, float p, ref bool flipped)
        {
            Clear();
            switch (scene)
            {
                case 0: SceneCall(p); break;
                case 1: SceneUnlock(p); break;
                case 2: SceneOpen(p); break;
                case 3: SceneEmerge(p, ref flipped); break;
                default: ScenePresent(p); break;
            }
        }

        private void Clear()
        {
            vaultRoot.gameObject.SetActive(false);
            cardHolder.gameObject.SetActive(false);
            bannerBlock.gameObject.SetActive(false);
            chipRow.gameObject.SetActive(false);
            thread.gameObject.SetActive(false);
            shaft.gameObject.SetActive(false);
            flash.gameObject.SetActive(false);
            shockRing.gameObject.SetActive(false);
            blackout.gameObject.SetActive(false);
            label.transform.parent.gameObject.SetActive(false);
            foreach (var mote in motes) mote.gameObject.SetActive(false);
        }

        // ---- 1 · Call: der Ruf ----
        private void SceneCall(float p)
        {
            float pull = Motion.Enter(Motion.Seg(p, 0.06f, 0.5f));
            float born = Motion.Enter(Motion.Seg(p, 0.42f, 0.94f));
            float inn = Motion.Enter(Motion.Seg(p, 0.04f, 0.3f));

            SetDim(0.7f * inn);
            SetThread(pull);
            SetMotes(inn, p);
            SetVault(born, 0f, 0f, 0.35f * born);
            SetLabel("SUMMONING", ReadCost(), inn);
        }

        // ---- 2 · Unlock: das Ankommen in der Flucht IST der Beat ----
        private void SceneUnlock(float p)
        {
            float align = Motion.Enter(Motion.Seg(p, 0.04f, 0.86f));
            float bite = Motion.Pop(Motion.Seg(p, 0.62f, 0.94f));

            SetDim(0.7f);
            SetMotes(1f - Motion.Seg(p, 0.3f, 0.8f), p);
            SetVault(1f, align, bite, Motion.Mix(0.35f, 0.95f, align));
            SetLabel("SUMMONING", ReadCost(), 1f - Motion.Seg(p, 0.7f, 1f));
            if (p >= 0.78f && p <= 0.8f) ScreenShake.Shake(0.018f, 0.6f, 16f);
        }

        // ---- 3 · Open: die Platte zerfällt ----
        private void SceneOpen(float p)
        {
            float open = Motion.Enter(p);
            SetDim(Motion.Mix(0.7f, 0.4f, open));
            SetVault(1f, 1f, 1f, 0.95f * (1f - open), open);
            SetFlash(Mathf.Sin(Mathf.PI * Motion.Seg(p, 0.1f, 0.9f)));
            SetShock(Motion.Seg(p, 0.12f, 1f));
        }

        // ---- 4 · Emerge: die Karte steigt und dreht um ----
        private void SceneEmerge(float p, ref bool flipped)
        {
            float rise = Motion.Enter(Motion.Seg(p, 0.08f, 0.82f));
            float flip = Motion.Drift(Motion.Seg(p, 0.30f, 0.90f));

            SetDim(0.4f);
            SetShaft(1f - Motion.Seg(p, 0.8f, 1f));
            SetCard(VaultX, Motion.Mix(VaultY + 96f, VaultY - 4f, rise),
                Motion.Mix(0.40f, 1f, rise), flip, ref flipped,
                Motion.Mix(0.90f, 0.50f, rise));
        }

        // ---- 5 · Present: Banner, dann in die Zone ----
        private void ScenePresent(float p)
        {
            float inn = Motion.Enter(Motion.Seg(p, 0.04f, 0.3f));
            float travel = Motion.Enter(Motion.Seg(p, 0.60f, 0.94f));
            float bannerOut = 1f - Motion.Enter(Motion.Seg(p, 0.55f, 0.8f));
            bool flipped = true;

            SetDim(0.4f * (1f - travel));
            SetCard(Motion.Mix(VaultX, destination.x, travel),
                Motion.Mix(VaultY - 4f, destination.y, travel),
                Motion.Mix(1f, FieldW / HeroW, travel), 1f, ref flipped, 0.5f * (1f - travel));
            SetBanner(inn * bannerOut);
            SetChips(inn * bannerOut);
        }

        // ================== BÜHNE ==================

        /// <summary>
        /// Woher die Karte kommt. Die Beschwörungsbedingung selbst steht auf keiner
        /// Karte als Text — sie steckt in den Effekt-Bausteinen —, deshalb bleibt es
        /// bei der Herkunft.
        /// </summary>
        private static string ReadCost() => "From the Extra Deck";

        private void SetDim(float amount)
        {
            dim.gameObject.SetActive(amount > 0.002f);
            if (amount > 0.002f) dim.color = new Color(0.039f, 0.027f, 0.02f, amount);
        }

        private void SetThread(float amount)
        {
            thread.gameObject.SetActive(amount > 0.002f);
            if (amount <= 0.002f) return;
            // source ist "y von oben" — der Anker des Fadens sitzt oben-Mitte,
            // also wird y für anchoredPosition negiert (wie überall auf der Bühne)
            var from = new Vector2(source.x, -source.y);
            var to = new Vector2(VaultX, -VaultY);
            var delta = to - from;
            thread.rectTransform.anchoredPosition = from;
            thread.rectTransform.sizeDelta = new Vector2(delta.magnitude * amount, 2f);
            thread.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            thread.color = Motion.Alpha(Violet, 0.85f * amount);
        }

        private void SetMotes(float amount, float t)
        {
            if (amount <= 0.002f) return;
            for (int i = 0; i < motes.Count; i++)
            {
                motes[i].gameObject.SetActive(true);
                // Feste Winkel, kein Zufall — die Sequenz muss reproduzierbar bleiben
                float angle = i / (float)motes.Count * Mathf.PI * 2f;
                float distance = Motion.Mix(R * 1.9f, R * 0.2f,
                    Mathf.Repeat(t * 0.6f + i * 0.07f, 1f));
                motes[i].anchoredPosition = new Vector2(
                    VaultX + Mathf.Cos(angle) * distance,
                    -VaultY + Mathf.Sin(angle) * distance);
                moteImages[i].color = Motion.Alpha(Violet, 0.6f * amount);
            }
        }

        private void SetVault(float form, float align, float bite, float ringAlpha, float open = 0f)
        {
            vaultRoot.gameObject.SetActive(form > 0.002f);
            if (form <= 0.002f) return;
            vaultRoot.anchoredPosition = new Vector2(VaultX, -VaultY);

            for (int i = 0; i < rings.Count; i++)
            {
                // Aus dem Versatz in die Flucht — und beim Öffnen nach aussen weg
                float rotation = Motion.Mix(RingOffsets[i], 0f, align);
                float scale = RingScales[i] * form * (1f + open * (0.6f + i * 0.3f));
                rings[i].localEulerAngles = new Vector3(0f, 0f, 45f + rotation);
                rings[i].localScale = Vector3.one * scale;
                ringImages[i].color = Motion.Alpha(Violet, ringAlpha * (1f - open));
            }

            for (int i = 0; i < locks.Count; i++)
            {
                float reach = Motion.Mix(R * 1.5f, R * 0.98f, Mathf.Clamp01(bite));
                float angle = i * 90f + 45f;
                locks[i].anchoredPosition = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * reach,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * reach);
                locks[i].localEulerAngles = new Vector3(0f, 0f, angle);
                lockImages[i].color = Motion.Alpha(Violet, Mathf.Clamp01(bite) * (1f - open));
            }

            // Die Platte zerfällt in vier Quadranten — dieselbe Technik wie beim Siegel
            for (int i = 0; i < plates.Count; i++)
            {
                float dx = i % 2 == 0 ? -1f : 1f;
                float dy = i < 2 ? -1f : 1f;
                plates[i].anchoredPosition = new Vector2(dx, dy) * (R * 1.15f * open);
                plates[i].localEulerAngles = new Vector3(0f, 0f, dx * 12f * open);
                plateImages[i].color = Motion.Alpha(Deep, 0.55f * form * (1f - open));
            }
        }

        private void SetFlash(float amount)
        {
            flash.gameObject.SetActive(amount > 0.002f);
            if (amount <= 0.002f) return;
            flash.color = Motion.Alpha(Violet, amount * 0.5f);
            float size = 260f + amount * 1500f;
            flash.rectTransform.sizeDelta = new Vector2(size, size);
            flash.rectTransform.anchoredPosition = new Vector2(VaultX, -VaultY);
        }

        private void SetShock(float amount)
        {
            shockRing.gameObject.SetActive(amount > 0.002f && amount < 1f);
            if (!shockRing.gameObject.activeSelf) return;
            float size = Motion.Mix(220f, 1180f, Motion.Enter(amount));
            shockRing.rectTransform.sizeDelta = new Vector2(size, size);
            shockRing.rectTransform.anchoredPosition = new Vector2(VaultX, -VaultY);
            shockRing.color = Motion.Alpha(Violet, (1f - amount) * 0.7f);
        }

        private void SetShaft(float amount)
        {
            shaft.gameObject.SetActive(amount > 0.002f);
            if (amount <= 0.002f) return;
            // Am Tresorende am hellsten, nach oben auslaufend
            shaft.rectTransform.anchoredPosition = new Vector2(VaultX, -VaultY + 170f);
            shaft.color = Motion.Alpha(Violet, 0.3f * amount);
        }

        private void SetCard(float x, float y, float scale, float flip, ref bool flipped, float glow)
        {
            cardHolder.gameObject.SetActive(true);
            cardHolder.anchoredPosition = new Vector2(x, -y);
            cardHolder.localScale = Vector3.one * scale;

            bool faceUp = flip >= 0.5f;
            if (faceUp != flipped)
            {
                cardView.Show(card, !faceUp, upright: true);
                flipped = faceUp;
                if (faceUp) cardView.SetHighlight(true, Violet);
            }
            float angle = flip * 180f;
            ((RectTransform)cardView.transform).localScale =
                new Vector3(Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad)), 1f, 1f);
        }

        private void SetLabel(string top, string sub, float alpha)
        {
            var holder = label.transform.parent.gameObject;
            holder.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;
            label.text = top;
            label.color = Motion.Alpha(Violet, alpha);
            sublabel.text = sub;
            sublabel.color = Motion.Alpha(Dim, alpha);
        }

        private void SetBanner(float alpha)
        {
            bannerBlock.gameObject.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;
            bannerTop.text = "RELIQUARY SUMMON";
            bannerTop.color = Motion.Alpha(Violet, alpha);
            bannerWord.text = card != null ? card.Name : "Special Summon";
            bannerWord.color = Motion.Alpha(Violet, alpha);
        }

        private void SetChips(float alpha)
        {
            chipRow.gameObject.SetActive(alpha > 0.002f);
            if (alpha <= 0.002f) return;
            var texts = new List<string> { "Special Summon" };
            if (card != null && card.Definition != null)
            {
                texts.Add($"{card.CurrentAtk} / {card.CurrentDef}");
                texts.Add(card.Definition.cardName);
            }
            for (int i = 0; i < chips.Count; i++)
            {
                bool used = i < texts.Count;
                chips[i].gameObject.SetActive(used);
                if (!used) continue;
                // Deckende Plaketten: bei 50 % Schwarz mischte sich das Duell-Feld
                // in die Schrift und die Stats waren praktisch unlesbar.
                chips[i].GetComponent<Image>().color = new Color(0.055f, 0.045f, 0.035f, 0.96f * alpha);
                chips[i].Find("Frame").GetComponent<Image>().color = Motion.Alpha(Violet, 0.85f * alpha);
                chips[i].Find("Gem").GetComponent<Image>().color = Motion.Alpha(Violet, alpha);
                var text = chips[i].Find("Label").GetComponent<TMP_Text>();
                text.text = texts[i];
                text.color = Motion.Alpha(new Color(0.953f, 0.867f, 0.643f), alpha);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(chipRow);
        }

        // ================== AUFBAU ==================

        private void Build(TcgCardView prefab)
        {
            skin = TransitionSkin.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 470;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            stage = (RectTransform)canvasGo.transform;

            dim = Make("Dim", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(dim.rectTransform);

            thread = Make("Thread", stage, new Color(0f, 0f, 0f, 0f));
            thread.sprite = skin.rule;
            thread.rectTransform.anchorMin = thread.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            thread.rectTransform.pivot = new Vector2(0f, 0.5f);

            for (int i = 0; i < 14; i++)
            {
                var mote = Make("Mote" + i, stage, new Color(0f, 0f, 0f, 0f));
                mote.sprite = skin.square;
                mote.rectTransform.anchorMin = mote.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                mote.rectTransform.sizeDelta = Vector2.one * (3f + i % 3);
                mote.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                motes.Add(mote.rectTransform);
                moteImages.Add(mote);
            }

            BuildVault();

            shaft = Make("Shaft", stage, new Color(0f, 0f, 0f, 0f));
            shaft.sprite = skin.fade;
            shaft.rectTransform.anchorMin = shaft.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shaft.rectTransform.sizeDelta = new Vector2(150f, 340f);

            cardHolder = MakeRect("Card", stage);
            cardHolder.anchorMin = cardHolder.anchorMax = new Vector2(0.5f, 1f);
            cardView = Instantiate(prefab, cardHolder);
            var cardRect = (RectTransform)cardView.transform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(HeroW, HeroH);
            cardView.enabled = false;

            flash = Make("Flash", stage, new Color(0f, 0f, 0f, 0f));
            flash.sprite = skin.flare;
            flash.rectTransform.anchorMin = flash.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            shockRing = Make("Shock", stage, new Color(0f, 0f, 0f, 0f));
            shockRing.sprite = skin.frame; shockRing.type = Image.Type.Sliced;
            shockRing.rectTransform.anchorMin = shockRing.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shockRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            BuildText();

            blackout = Make("Blackout", stage, new Color(0f, 0f, 0f, 0f));
            Stretch(blackout.rectTransform);

            gameObject.SetActive(false);
        }

        private void BuildVault()
        {
            vaultRoot = MakeRect("Vault", stage);
            vaultRoot.anchorMin = vaultRoot.anchorMax = new Vector2(0.5f, 1f);

            // Vier Quadranten der Platte, hinter den Ringen
            for (int i = 0; i < 4; i++)
            {
                float dx = i % 2 == 0 ? -1f : 1f;
                float dy = i < 2 ? -1f : 1f;
                var window = MakeRect("Plate" + i, vaultRoot);
                window.sizeDelta = new Vector2(R, R);
                window.anchoredPosition = new Vector2(dx * R * 0.5f, dy * R * 0.5f);
                var plate = window.gameObject.AddComponent<Image>();
                plate.sprite = skin.diagFade;
                plate.color = Motion.Alpha(Deep, 0.55f);
                plate.raycastTarget = false;
                plates.Add(window);
                plateImages.Add(plate);
            }

            // Drei Rautenringe — Skala 1.0 / 0.74 / 0.5
            for (int i = 0; i < 3; i++)
            {
                var ring = Make("Ring" + i, vaultRoot, Motion.Alpha(Violet, 0.35f));
                ring.sprite = skin.frame; ring.type = Image.Type.Sliced;
                ring.rectTransform.sizeDelta = new Vector2(R * 2f, R * 2f);
                rings.Add(ring.rectTransform);
                ringImages.Add(ring);
            }

            // Vier Schlösser
            for (int i = 0; i < 4; i++)
            {
                var padlock = Make("Lock" + i, vaultRoot, new Color(0f, 0f, 0f, 0f));
                padlock.sprite = skin.square;
                padlock.rectTransform.sizeDelta = new Vector2(18f, 18f);
                locks.Add(padlock.rectTransform);
                lockImages.Add(padlock);
            }
        }

        private void BuildText()
        {
            var labelHolder = MakeRect("Label", stage);
            labelHolder.anchorMin = labelHolder.anchorMax = new Vector2(0.5f, 1f);
            labelHolder.sizeDelta = new Vector2(900f, 56f);
            labelHolder.anchoredPosition = new Vector2(0f, -70f);
            label = MakeText("Top", labelHolder, skin.oswald, 14f, Violet);
            label.characterSpacing = 40f;
            label.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)label.transform, 900f, 20f, 12f);
            sublabel = MakeText("Sub", labelHolder, skin.spectral, 15f, Dim);
            sublabel.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)sublabel.transform, 900f, 20f, -14f);

            bannerBlock = MakeRect("Banner", stage);
            bannerBlock.anchorMin = bannerBlock.anchorMax = new Vector2(0.5f, 1f);
            bannerBlock.sizeDelta = new Vector2(1000f, 90f);
            bannerBlock.anchoredPosition = new Vector2(0f, -130f);
            bannerTop = MakeText("Top", bannerBlock, skin.oswald, 13f, Violet);
            bannerTop.characterSpacing = 38f;
            bannerTop.alignment = TextAlignmentOptions.Center;
            Strip((RectTransform)bannerTop.transform, 1000f, 18f, 28f);
            bannerWord = MakeText("Word", bannerBlock, skin.cinzel, 44f, Violet);
            bannerWord.alignment = TextAlignmentOptions.Center;
            bannerWord.enableAutoSizing = true;
            bannerWord.fontSizeMin = 24f; bannerWord.fontSizeMax = 44f;
            Strip((RectTransform)bannerWord.transform, 1000f, 54f, -12f);

            chipRow = MakeRect("Chips", stage);
            chipRow.anchorMin = chipRow.anchorMax = new Vector2(0.5f, 1f);
            chipRow.sizeDelta = new Vector2(900f, 40f);
            // Auf den Tresor zentriert — sonst sässen sie darauf
            chipRow.anchoredPosition = new Vector2(0f, -(VaultY + R + 46f));
            var layout = chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            layout.childControlWidth = true; layout.childControlHeight = true;
            for (int i = 0; i < 3; i++)
            {
                var chip = MakeRect("Chip" + i, chipRow);
                var element = chip.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 200f; element.preferredHeight = 38f;
                var bg = chip.gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.5f); bg.raycastTarget = false;
                var frame = Make("Frame", chip, Violet);
                frame.sprite = skin.frame; frame.type = Image.Type.Sliced;
                Stretch(frame.rectTransform);
                var gem = Make("Gem", chip, Violet);
                gem.sprite = skin.square;
                gem.rectTransform.sizeDelta = new Vector2(8f, 8f);
                gem.rectTransform.anchorMin = gem.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                gem.rectTransform.anchoredPosition = new Vector2(16f, 0f);
                gem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                var text = MakeText("Label", chip, skin.spectral, 14f, Gold);
                text.alignment = TextAlignmentOptions.Left;
                var textRect = (RectTransform)text.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(30f, 0f); textRect.offsetMax = new Vector2(-10f, 0f);
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
