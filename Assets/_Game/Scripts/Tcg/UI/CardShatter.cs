using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Der Keilbruch (Handoff „Animations", Abschnitt 0 und 4).
    ///
    /// Die Karte zerbricht in sechs Keile, die aus EINER Karte geschnitten sind:
    /// jeder Keil trägt seine eigene beschnittene Kopie, damit das Artwork
    /// mitbricht, statt dass ein Riss darüberschiebt. Die sechs Risslinien, die
    /// die Szene davor zeichnet, sind genau die Linien, an denen sich die Keile
    /// trennen.
    ///
    /// Alle sechs strahlen vom Bruchpunkt (44 %, 46 %) aus — leicht über und
    /// links der Mitte. Das liest sich als Einschlagstelle, nicht als
    /// symmetrische Teilung.
    ///
    /// <b>Gather kehrt die Bewegung um</b>: die Keile fliegen zurück, entdrehen
    /// sich und schrumpfen, während am Treffpunkt ein Kartenrücken aufpoppt. Eine
    /// zerstörte Karte wird zu einer anonymen Karte — das ist, was der Friedhof hält.
    /// </summary>
    public class CardShatter : MonoBehaviour
    {
        /// <summary>Bruchpunkt in Kartenkoordinaten (0..1, y von oben).</summary>
        public static readonly Vector2 Origin = new Vector2(0.44f, 0.46f);

        /// <summary>Die sechs Keile: Umriss, Flugrichtung, Drehung.</summary>
        private static readonly Vector2[][] Shapes =
        {
            new[] { V(0f, 0f), V(0.52f, 0f), V(0.44f, 0.46f), V(0f, 0.38f) },
            new[] { V(0.52f, 0f), V(1f, 0f), V(1f, 0.30f), V(0.44f, 0.46f) },
            new[] { V(0f, 0.38f), V(0.44f, 0.46f), V(0.30f, 1f), V(0f, 1f) },
            new[] { V(0.44f, 0.46f), V(1f, 0.30f), V(1f, 0.64f), V(0.62f, 1f) },
            new[] { V(0.30f, 1f), V(0.44f, 0.46f), V(0.62f, 1f) },
            new[] { V(1f, 0.64f), V(1f, 1f), V(0.62f, 1f) },
        };

        private static readonly Vector2[] Directions =
        {
            new Vector2(-0.95f, -1.15f), new Vector2(1.00f, -1.10f), new Vector2(-1.20f, 0.50f),
            new Vector2(1.20f, 0.42f), new Vector2(-0.10f, 1.30f), new Vector2(1.05f, 1.15f),
        };

        private static readonly float[] Spins = { -20f, 24f, -14f, 18f, 7f, 29f };

        /// <summary>Asche, in die der Rahmen beim Zerfallen übergeht.</summary>
        private static readonly Color Ash = new Color(0.541f, 0.522f, 0.482f);   // #8A857B

        private static Sprite[] maskSprites;

        private readonly List<RectTransform> wedges = new List<RectTransform>();
        private readonly List<TcgCardView> faces = new List<TcgCardView>();
        private readonly List<Image> veils = new List<Image>();
        private Vector2 cardSize;

        /// <summary>Der gemeinsame Ursprung aller Keile — hier sitzt die Karte.</summary>
        public RectTransform Rect { get; private set; }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        /// <summary>
        /// Baut die sechs Keile über einer Karte. Die Kopien stammen aus dem
        /// gelieferten Prefab und zeigen dieselbe Karte wie das Original.
        /// </summary>
        /// <summary>
        /// Wie <see cref="Build(RectTransform,TcgCardView,CardInstance,Vector2)"/>,
        /// aber mit einer Bau-Funktion für das Kartengesicht. So lässt sich auch
        /// eine zur Laufzeit gesetzte Karte zerlegen — etwa die Spielerkarte, die
        /// kein Prefab ist.
        /// </summary>
        public static CardShatter Build(RectTransform parent,
                                        System.Func<RectTransform, RectTransform> makeFace,
                                        Vector2 size)
        {
            EnsureMasks();
            var go = new GameObject("~Shatter", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var shatter = go.AddComponent<CardShatter>();
            shatter.Rect = rect;
            shatter.cardSize = size;
            shatter.CreateFrom(rect, makeFace);
            return shatter;
        }

        private void CreateFrom(RectTransform root, System.Func<RectTransform, RectTransform> makeFace)
        {
            for (int i = 0; i < Shapes.Length; i++)
            {
                var wedgeRect = MakeWedge(root, i);
                var face = makeFace(wedgeRect);
                face.anchorMin = face.anchorMax = new Vector2(0.5f, 0.5f);
                face.pivot = new Vector2(0.5f, 0.5f);
                face.anchoredPosition = Vector2.zero;
                face.sizeDelta = cardSize;
                AddVeil(root, wedgeRect);
                wedges.Add(wedgeRect);
                faces.Add(null);          // kein TcgCardView — die Asche macht der Schleier
            }
        }

        public static CardShatter Build(RectTransform parent, TcgCardView source,
                                        CardInstance card, Vector2 size)
        {
            EnsureMasks();
            var go = new GameObject("~Shatter", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var shatter = go.AddComponent<CardShatter>();
            shatter.Rect = rect;
            shatter.cardSize = size;
            shatter.Create(rect, source, card);
            return shatter;
        }

        private void Create(RectTransform root, TcgCardView source, CardInstance card)
        {
            for (int i = 0; i < Shapes.Length; i++)
            {
                var wedgeRect = MakeWedge(root, i);

                var copy = Instantiate(source, wedgeRect);
                var copyRect = (RectTransform)copy.transform;
                copyRect.anchorMin = copyRect.anchorMax = new Vector2(0.5f, 0.5f);
                copyRect.pivot = new Vector2(0.5f, 0.5f);
                copyRect.anchoredPosition = Vector2.zero;
                copyRect.sizeDelta = cardSize;
                copyRect.localScale = Vector3.one;
                copy.gameObject.SetActive(true);
                copy.Show(card, false, upright: true);
                copy.SetHighlight(false);
                copy.enabled = false;                       // keine Eingaben auf Trümmern
                var group = copy.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;

                AddVeil(root, wedgeRect);
                wedges.Add(wedgeRect);
                faces.Add(copy);
            }
        }

        /// <summary>
        /// Ein Keilfenster: so gross wie die ganze Karte, die Maske schneidet die
        /// Form heraus. Die Kopie darin sitzt deckungsgleich zum Original —
        /// dadurch passen alle sechs Ausschnitte zusammen.
        /// </summary>
        private RectTransform MakeWedge(RectTransform root, int index)
        {
            var wedge = new GameObject("Wedge" + index, typeof(RectTransform));
            wedge.layer = root.gameObject.layer;
            var wedgeRect = (RectTransform)wedge.transform;
            wedgeRect.SetParent(root, false);
            wedgeRect.anchorMin = wedgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            wedgeRect.pivot = new Vector2(0.5f, 0.5f);
            wedgeRect.sizeDelta = cardSize;

            var maskImage = wedge.AddComponent<Image>();
            maskImage.sprite = maskSprites[index];
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
            var mask = wedge.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            return wedgeRect;
        }

        /// <summary>
        /// Ascheschleier über der Kopie. Eine uGUI-Tönung multipliziert nur die
        /// Eckfarbe — sie kann ein Artwork-Sprite nicht entsättigen. Der Schleier
        /// liegt in derselben Maske und greift damit genau auf der Keilfläche.
        /// </summary>
        private void AddVeil(RectTransform root, RectTransform wedgeRect)
        {
            var veil = new GameObject("Ash", typeof(RectTransform));
            veil.layer = root.gameObject.layer;
            var veilRect = (RectTransform)veil.transform;
            veilRect.SetParent(wedgeRect, false);
            veilRect.anchorMin = Vector2.zero; veilRect.anchorMax = Vector2.one;
            veilRect.offsetMin = Vector2.zero; veilRect.offsetMax = Vector2.zero;
            var veilImage = veil.AddComponent<Image>();
            veilImage.color = new Color(Ash.r, Ash.g, Ash.b, 0f);
            veilImage.raycastTarget = false;
            veils.Add(veilImage);
        }

        /// <summary>
        /// Stellt den Bruch.
        /// <paramref name="fly"/> ist der Ausschlag in Pixeln (negativ zieht die
        /// Keile wieder zusammen), <paramref name="drain"/> zieht Farbe und
        /// Sättigung heraus — die Karte hört sichtbar auf, eine lebende Karte zu sein.
        /// </summary>
        public void Apply(float fly, float spinAmount, float scale, float drain, float fade)
        {
            gameObject.SetActive(fade > 0.002f);
            if (!gameObject.activeSelf) return;

            for (int i = 0; i < wedges.Count; i++)
            {
                wedges[i].anchoredPosition = Directions[i] * fly;
                wedges[i].localEulerAngles = new Vector3(0f, 0f, Spins[i] * spinAmount);
                wedges[i].localScale = Vector3.one * Mathf.Max(0.001f, scale);

                // Der Rahmen wandert nach Asche und entsättigt: eine graue Karte
                // liest sich nicht mehr als etwas, das noch wirkt
                var tint = Color.Lerp(Color.white, Ash, drain);
                if (faces[i] != null) faces[i].SetTint(new Color(tint.r, tint.g, tint.b, fade), drain);
                veils[i].color = new Color(Ash.r, Ash.g, Ash.b, 0.72f * drain * fade);
            }
        }

        // ---- Keilformen ----

        /// <summary>
        /// Rastert die sechs Umrisse einmalig in Masken-Sprites. Reine Geometrie
        /// aus dem Handoff, deshalb gerechnet statt als Grafikdatei gepflegt.
        /// </summary>
        private static void EnsureMasks()
        {
            if (maskSprites != null) return;
            maskSprites = new Sprite[Shapes.Length];
            for (int i = 0; i < Shapes.Length; i++) maskSprites[i] = Rasterise(Shapes[i]);
        }

        private static Sprite Rasterise(Vector2[] polygon)
        {
            const int width = 132, height = 185;    // Kartenmass am Feld
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                // Texturzeile 0 liegt unten, die Umrisse sind von oben beschrieben
                float v = 1f - (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    bool inside = Contains(polygon, new Vector2(u, v));
                    pixels[y * width + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

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
