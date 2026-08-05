using Rouge.Tcg.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Legt das Finish eines Kartenexemplars über die Karte. Wird an jede
    /// Kartenansicht gehängt — Hand, Feld, Deck Builder, Pack-Reveal — und
    /// beschneidet sich selbst auf die Kartenfläche.
    ///
    /// Alle drei Effekte laufen auf gescrollten Texturen statt auf Shadern:
    /// das kostet keinen eigenen Materialdurchgang und funktioniert überall,
    /// wo uGUI funktioniert.
    ///
    /// Die Lesbarkeit hat Vorrang — die Deckkraft ist bewusst so gewählt, dass
    /// Effekttext und die DMG/DEF-Zahlen darunter lesbar bleiben.
    /// </summary>
    [DisallowMultipleComponent]
    public class CardFinishOverlay : MonoBehaviour
    {
        private CardFinish finish = CardFinish.Plain;
        private RectTransform clip;

        private RawImage gloss;
        private RawImage rainbow, grating;
        private RawImage scanlines, noise, band;

        private float clock;

        /// <summary>Setzt (oder entfernt) das Finish. Mehrfaches Setzen ist billig.</summary>
        public static void Apply(RectTransform card, CardFinish finish)
        {
            if (card == null) return;
            var overlay = card.GetComponent<CardFinishOverlay>();
            if (finish == CardFinish.Plain)
            {
                if (overlay != null) overlay.Clear();
                return;
            }
            if (overlay == null) overlay = card.gameObject.AddComponent<CardFinishOverlay>();
            overlay.Set(finish);
        }

        private void Set(CardFinish value)
        {
            if (finish == value && clip != null) return;
            finish = value;
            Rebuild();
        }

        private void Clear()
        {
            finish = CardFinish.Plain;
            if (clip != null) DestroyImmediate(clip.gameObject);
            clip = null;
        }

        private void Rebuild()
        {
            if (clip != null) DestroyImmediate(clip.gameObject);
            gloss = rainbow = grating = scanlines = noise = band = null;

            var skin = TransitionSkin.Load();
            if (skin == null) return;

            // Eigener Beschnitt-Container: die Effekte laufen über den Rand hinaus
            var go = new GameObject("~Finish", typeof(RectTransform));
            clip = (RectTransform)go.transform;
            clip.SetParent((RectTransform)transform, false);
            clip.anchorMin = Vector2.zero; clip.anchorMax = Vector2.one;
            clip.offsetMin = Vector2.zero; clip.offsetMax = Vector2.zero;
            clip.SetAsLastSibling();
            go.AddComponent<RectMask2D>();

            switch (finish)
            {
                case CardFinish.Glossy:
                    gloss = AdditiveLayer("Gloss", skin.finishGloss, new Color(1f, 1f, 1f, 0f));
                    // Schmal und schräg — ein Streiflicht, kein Farbverlauf
                    gloss.rectTransform.anchorMin = new Vector2(0f, -0.3f);
                    gloss.rectTransform.anchorMax = new Vector2(0.46f, 1.3f);
                    gloss.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);
                    break;

                case CardFinish.Rainbow:
                    // Deutlich dezenter als im Web-Entwurf: dort liegt ein color-dodge
                    // darüber, das aufhellt statt zu überdecken. uGUI kann das nicht,
                    // also muss die Deckkraft die Arbeit machen — sonst verschwindet
                    // das Artwork unter einer Pastellschicht.
                    // Additiv wirkt viel kräftiger als Alpha — hier reicht wenig
                    rainbow = AdditiveLayer("Hue", skin.finishRainbow, new Color(1f, 1f, 1f, 0.13f));
                    rainbow.uvRect = new Rect(0f, 0f, 0.34f, 1f);   // Ausschnitt wandert
                    rainbow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -25f);
                    Grow(rainbow.rectTransform, 0.45f);
                    // Das feine Gitter steht still — genau das macht daraus Folie
                    grating = AdditiveLayer("Grating", skin.finishGrating, new Color(1f, 1f, 1f, 0.16f));
                    grating.rectTransform.localEulerAngles = new Vector3(0f, 0f, -25f);
                    Grow(grating.rectTransform, 0.45f);
                    // Dicht genug, dass es als Beugungsgitter liest und nicht als Streifen
                    grating.uvRect = new Rect(0f, 0f, 46f, 46f);
                    break;

                case CardFinish.Static:
                    // Feine Raster statt grober Maschen — sonst liegt ein Gitter auf
                    // der Karte statt einer Störung darin.
                    scanlines = AdditiveLayer("Scanlines", skin.finishScanlines, new Color(1f, 1f, 1f, 0.30f));
                    scanlines.uvRect = new Rect(0f, 0f, 1f, 119f);
                    noise = AdditiveLayer("Noise", skin.finishNoise, new Color(1f, 1f, 1f, 0.14f));
                    noise.uvRect = new Rect(0f, 0f, 113f, 158f);
                    band = AdditiveLayer("Band", skin.finishBand, new Color(1f, 1f, 1f, 1f));
                    band.rectTransform.anchorMin = new Vector2(0f, 0f);
                    band.rectTransform.anchorMax = new Vector2(1f, 0.12f);
                    break;
            }
        }

        /// <summary>
        /// Additive Ebene: sie fügt Licht hinzu, statt die Karte zu überdecken.
        /// Genau das macht color-dodge im Entwurf — dunkle Stellen bleiben dunkel.
        /// </summary>
        private RawImage AdditiveLayer(string name, Texture texture, Color color)
        {
            var layer = Layer(name, texture, color);
            var skin = TransitionSkin.Load();
            if (skin != null && skin.additive != null) layer.material = skin.additive;
            return layer;
        }

        private RawImage Layer(string name, Texture texture, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(clip, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Schräge Schichten müssen über die Ecken hinausragen, sonst klaffen sie.</summary>
        private static void Grow(RectTransform rect, float amount)
        {
            rect.anchorMin = new Vector2(-amount, -amount);
            rect.anchorMax = new Vector2(1f + amount, 1f + amount);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (finish == CardFinish.Plain || clip == null) return;
            clock += Time.unscaledDeltaTime;

            switch (finish)
            {
                case CardFinish.Glossy: TickGloss(); break;
                case CardFinish.Rainbow: TickRainbow(); break;
                case CardFinish.Static: TickStatic(); break;
            }
        }

        /// <summary>Ein Streiflicht wandert in 3,4 s einmal über die Karte.</summary>
        private void TickGloss()
        {
            if (gloss == null) return;
            const float period = 3.4f;
            float p = (clock % period) / period;
            float eased = p < 0.5f ? 2f * p * p : 1f - Mathf.Pow(-2f * p + 2f, 2f) * 0.5f;
            float x = Mathf.Lerp(-1.6f, 2.6f, eased);
            gloss.rectTransform.anchorMin = new Vector2(x, -0.3f);
            gloss.rectTransform.anchorMax = new Vector2(x + 0.46f, 1.3f);
            gloss.rectTransform.offsetMin = Vector2.zero;
            gloss.rectTransform.offsetMax = Vector2.zero;
            // An den Rändern ausblenden, damit es nicht abrupt erscheint
            float fade = Mathf.Clamp01(Mathf.Sin(Mathf.PI * p) * 1.6f);
            gloss.color = new Color(1f, 1f, 1f, 0.5f * fade);
        }

        /// <summary>Das Farbband wandert, das Gitter bleibt stehen.</summary>
        private void TickRainbow()
        {
            if (rainbow == null) return;
            var uv = rainbow.uvRect;
            uv.x = (clock / 6f) % 1f;
            rainbow.uvRect = uv;
        }

        /// <summary>
        /// Scanlines springen (nicht gleiten), das Rauschen flackert in Stufen und
        /// ein Band rollt herunter. Weiche Interpolation läse sich als Blende statt
        /// als Störung — deshalb sind Sprung und Flackern absichtlich gerastert.
        /// </summary>
        private void TickStatic()
        {
            if (scanlines != null)
            {
                var uv = scanlines.uvRect;
                int step = Mathf.FloorToInt((clock % 0.55f) / 0.55f * 4f);
                uv.y = step * 0.5f;
                scanlines.uvRect = uv;
            }
            if (noise != null)
            {
                float[] pattern = { 0.30f, 0.62f, 0.18f, 0.55f, 0.24f, 0.48f, 0.20f };
                int index = Mathf.FloorToInt((clock % 1.1f) / 1.1f * pattern.Length);
                noise.color = new Color(1f, 1f, 1f, pattern[Mathf.Clamp(index, 0, pattern.Length - 1)] * 0.22f);
            }
            if (band != null)
            {
                float p = (clock % 2.2f) / 2.2f;
                float y = Mathf.Lerp(1f, -0.12f, p);
                band.rectTransform.anchorMin = new Vector2(0f, y);
                band.rectTransform.anchorMax = new Vector2(1f, y + 0.12f);
                band.rectTransform.offsetMin = Vector2.zero;
                band.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
