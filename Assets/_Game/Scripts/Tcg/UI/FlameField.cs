using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Das Flammenfeld unter einer verdeckten Pack-Karte (Handoff „Animations",
    /// Abschnitt 2). Es ist die Entscheidung, um die herum die ganze Animation
    /// gebaut ist: <b>die Seltenheit ist zu sehen, bevor die Karte umdreht</b>.
    /// Zungenzahl, Höhe und Reichweite hängen an der Seltenheit — ein Relic
    /// brennt über die Karte hinaus, ein Common leckt gerade an ihre Unterkante.
    ///
    /// Deshalb ist das hier auch keine Deko, sondern ein Spielhinweis: bei
    /// reduzierter Bewegung wird das Feld eingefroren, nicht entfernt.
    ///
    /// Jede Zunge flackert auf ihrer eigenen, festen Phase — kein Zufall. Die
    /// Sequenz muss Bild für Bild reproduzierbar bleiben.
    /// </summary>
    public class FlameField : MonoBehaviour
    {
        private const float CardWidth = 176f, CardHeight = 246f;

        private static Sprite tongueSprite, coreSprite;

        private readonly List<RectTransform> tongues = new List<RectTransform>();
        private readonly List<Image> tongueImages = new List<Image>();
        private readonly List<Image> coreImages = new List<Image>();
        private readonly List<RectTransform> embers = new List<RectTransform>();
        private readonly List<Image> emberImages = new List<Image>();

        private RectTransform root;
        private Image bed;            // die glimmende Ellipse am Fuss
        private Color colour;
        private float pulse;          // 0 Common … 1 Relic
        private float spread, baseHeight;

        /// <summary>Zungenzahl je Seltenheitsstufe — der auffälligste Unterschied.</summary>
        private static int TongueCount(float pulse) =>
            pulse >= 0.99f ? 13 : pulse >= 0.5f ? 9 : pulse >= 0.25f ? 7 : 5;

        public static FlameField Build(RectTransform parent, Color colour, float pulse, Sprite bedSprite = null)
        {
            var go = new GameObject("Flames", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var field = go.AddComponent<FlameField>();
            field.root = rect;
            field.colour = colour;
            field.pulse = Mathf.Clamp01(pulse);
            field.bedSprite = bedSprite;
            field.Create();
            return field;
        }

        private Sprite bedSprite;

        private void Create()
        {
            EnsureSprites();
            spread = CardWidth * 1.24f;
            baseHeight = CardHeight * Mathf.Lerp(0.52f, 1.06f, pulse);

            // Glut am Fuss: eine weiche Ellipse, die die Zungenwurzeln verbindet.
            // Ohne Sprite zeichnet uGUI hier ein hartes Rechteck — das sähe aus
            // wie ein Farbklotz unter der Karte, nicht wie Glut.
            bed = Make("Bed", root, bedSprite, colour);
            bed.rectTransform.sizeDelta = new Vector2(spread, 52f);
            bed.rectTransform.anchoredPosition = new Vector2(0f, 26f);

            int count = TongueCount(pulse);
            for (int i = 0; i < count; i++)
            {
                var tongue = Make("Tongue" + i, root, tongueSprite, colour);
                // Pivot unten: die Zunge wächst nach oben und neigt sich um ihre Wurzel
                tongue.rectTransform.pivot = new Vector2(0.5f, 0f);
                tongues.Add(tongue.rectTransform);
                tongueImages.Add(tongue);

                var core = Make("Core", tongue.rectTransform, coreSprite, Color.white);
                core.rectTransform.anchorMin = new Vector2(0.28f, 0f);
                core.rectTransform.anchorMax = new Vector2(0.72f, 0.52f);
                core.rectTransform.offsetMin = Vector2.zero;
                core.rectTransform.offsetMax = Vector2.zero;
                core.rectTransform.pivot = new Vector2(0.5f, 0f);
                coreImages.Add(core);
            }

            // Funken gibt es erst ab Rare — sie sind der Hinweis „hier kommt etwas"
            if (pulse >= 0.5f)
                for (int i = 0; i < 5; i++)
                {
                    var ember = Make("Ember" + i, root, bedSprite, colour);
                    ember.rectTransform.sizeDelta = Vector2.one * (4f + i % 3);
                    ember.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                    embers.Add(ember.rectTransform);
                    emberImages.Add(ember);
                }
        }

        /// <summary>
        /// Stellt das Feld. <paramref name="amount"/> ist die Stärke (0 = aus),
        /// <paramref name="t"/> die Phase — beim Aufruf immer der Szenenfortschritt,
        /// nie eine eigene Uhr, sonst ist die Sequenz nicht mehr exportierbar.
        /// </summary>
        public void Apply(float amount, float t)
        {
            root.gameObject.SetActive(amount > 0.01f);
            if (!root.gameObject.activeSelf) return;

            // Die Wurzel sitzt 8 px über der Unterkante der Karte
            root.anchoredPosition = new Vector2(0f, -CardHeight * 0.5f + 8f);
            bed.color = Alpha(colour, 0.28f * amount);

            int count = tongues.Count;
            for (int i = 0; i < count; i++)
            {
                float u = count == 1 ? 0.5f : i / (float)(count - 1);
                float dx = (u - 0.5f) * spread;
                float centre = Mathf.Max(0.2f, 1f - Mathf.Abs(u - 0.5f) * 1.55f);
                float flicker = 0.6f + 0.4f * Mathf.Sin(Mathf.PI * 2f * (t * 1.7f + i * 0.29f));
                float height = baseHeight * centre * flicker * amount;
                float width = Mathf.Lerp(15f, 31f, centre) * Mathf.Lerp(0.82f, 1.16f, flicker);
                float sway = Mathf.Sin(Mathf.PI * 2f * (t * 1.1f + i * 0.41f)) * 6f;

                var rect = tongues[i];
                rect.sizeDelta = new Vector2(width, Mathf.Max(1f, height));
                rect.anchoredPosition = new Vector2(dx, 0f);
                // uGUI kennt kein skewX — die Neigung um die Wurzel kommt dem am nächsten
                rect.localEulerAngles = new Vector3(0f, 0f, -sway);
                tongueImages[i].color = Alpha(colour, 0.9f * amount);
                coreImages[i].color = Alpha(new Color(0.973f, 0.933f, 0.839f), // #F8EED6
                    (0.5f * pulse + 0.18f) * amount);
            }

            for (int i = 0; i < embers.Count; i++)
            {
                float phase = Mathf.Repeat(t * 0.85f + i * 0.21f, 1f);
                float x = (i / 4f - 0.5f) * spread * 0.72f + Mathf.Sin(Mathf.PI * 2f * (phase + i)) * 12f;
                embers[i].anchoredPosition = new Vector2(x, baseHeight * (0.55f + phase * 1.05f));
                emberImages[i].color = Alpha(colour, Mathf.Sin(Mathf.PI * phase) * 0.75f * amount);
            }
        }

        private static Color Alpha(Color colour, float alpha) =>
            new Color(colour.r, colour.g, colour.b, Mathf.Clamp01(alpha));

        private static Image Make(string name, RectTransform parent, Sprite sprite, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        // ---- Zungenform ----

        /// <summary>
        /// Zungen- und Kernform werden einmal gerechnet und danach geteilt. Als
        /// Textur statt als Asset, damit die Animation ohne neue Grafikdateien
        /// auskommt — die Form ist reine Geometrie aus dem Handoff.
        /// </summary>
        private static void EnsureSprites()
        {
            if (tongueSprite != null && coreSprite != null) return;

            // polygon(50% 0, 82% 34%, 100% 72%, 74% 100%, 26% 100%, 0 72%, 18% 34%)
            var tongue = new[]
            {
                new Vector2(0.50f, 0.00f), new Vector2(0.82f, 0.34f), new Vector2(1.00f, 0.72f),
                new Vector2(0.74f, 1.00f), new Vector2(0.26f, 1.00f), new Vector2(0.00f, 0.72f),
                new Vector2(0.18f, 0.34f),
            };
            // polygon(50% 0, 100% 60%, 72% 100%, 28% 100%, 0 60%)
            var core = new[]
            {
                new Vector2(0.50f, 0.00f), new Vector2(1.00f, 0.60f), new Vector2(0.72f, 1.00f),
                new Vector2(0.28f, 1.00f), new Vector2(0.00f, 0.60f),
            };

            // Der senkrechte Verlauf steckt in der Alpha: oben durchsichtig,
            // unten fast deckend (16 % bei 20 %, 58 % bei 60 %, 92 % unten)
            tongueSprite = Rasterise(tongue, cssY =>
                cssY < 0.2f ? Mathf.Lerp(0f, 0.16f, cssY / 0.2f)
                : cssY < 0.6f ? Mathf.Lerp(0.16f, 0.58f, (cssY - 0.2f) / 0.4f)
                : Mathf.Lerp(0.58f, 0.92f, (cssY - 0.6f) / 0.4f));
            coreSprite = Rasterise(core, cssY => cssY);
        }

        /// <summary>
        /// Zeichnet ein Polygon in eine Textur. <paramref name="alphaAt"/> bekommt
        /// die Höhe von OBEN (0) nach UNTEN (1) — so steht es im Handoff.
        /// </summary>
        private static Sprite Rasterise(Vector2[] polygon, System.Func<float, float> alphaAt)
        {
            const int width = 64, height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                // Texturzeile 0 ist unten, das Polygon ist von oben beschrieben
                float cssY = 1f - (y + 0.5f) / height;
                byte alphaRow = (byte)(Mathf.Clamp01(alphaAt(cssY)) * 255f);
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    bool inside = Contains(polygon, new Vector2(u, cssY));
                    pixels[y * width + x] = new Color32(255, 255, 255, inside ? alphaRow : (byte)0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Punkt-in-Polygon per Strahlenschnitt.</summary>
        private static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].y > point.y == polygon[j].y > point.y) continue;
                float cross = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                              / (polygon[j].y - polygon[i].y) + polygon[i].x;
                if (point.x < cross) inside = !inside;
            }
            return inside;
        }
    }
}
