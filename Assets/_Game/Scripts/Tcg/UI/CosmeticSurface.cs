using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Legt die ausgerüstete Kosmetik über das Bild, an dem dieses Skript hängt.
    /// Ohne Ausrüstung — oder wenn der Client die Grafik nicht kennt — bleibt
    /// schlicht stehen, was im Inspector verdrahtet ist. Das ist die
    /// Vanilla-Rückfallebene, und sie ist absichtlich stumm: ein Platzhalter wäre
    /// schlimmer als das Grundbild.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CosmeticSurface : MonoBehaviour
    {
        public enum Surface { DuelMat, CardBack, AvatarFrame }

        [SerializeField] private Surface surface = Surface.DuelMat;

        private RectTransform builtHalves;

        private void OnEnable()
        {
            // Nur im Spiel. Im Editor läuft OnEnable beim Laden der Szene mit, und
            // die erzeugten Hälften landeten sonst als echte Objekte in der Szene.
            if (!Application.isPlaying) return;
            Apply();
        }

        /// <summary>Neu anwenden, wenn sich die Ausrüstung im laufenden Betrieb ändert.</summary>
        public void Apply()
        {
            var image = GetComponent<Image>();
            if (image == null) return;

            if (surface == Surface.DuelMat) { ApplySplitMat(image); return; }

            Sprite sprite = surface == Surface.CardBack
                ? Net.CosmeticArt.EquippedCardBack()
                : Net.CosmeticArt.EquippedFrame();
            if (sprite == null) return;
            image.sprite = sprite;
        }

        /// <summary>
        /// Die Matte ist zweigeteilt: <b>jeder liegt auf seiner eigenen</b>. Unten
        /// die eigene, oben die des Gegners — genau so, wie die Bretthälften auch
        /// sonst aufgeteilt sind.
        /// <para>
        /// Beide Hälften zeigen ihre Matte in <i>voller</i> Feldgrösse und werden
        /// nur beschnitten. Würde man je eine ganze Matte in eine halbe Höhe
        /// quetschen, wäre sie gestaucht und die Muster passten nicht mehr zur
        /// Vorlage — Cathedral Plates Bögen etwa gehören an den oberen und unteren
        /// Rand, nicht in die Mitte.
        /// </para>
        /// Das Grundbild bleibt liegen: wer keine Matte trägt, zeigt weiter den
        /// normalen Tisch, und das gilt für jede Hälfte einzeln.
        /// </summary>
        private void ApplySplitMat(Image baseImage)
        {
            var own = Net.CosmeticArt.EquippedMat();
            var foe = Net.CosmeticArt.RemoteMat();

            if (builtHalves != null)
            {
                if (Application.isPlaying) Destroy(builtHalves.gameObject);
                else DestroyImmediate(builtHalves.gameObject);
                builtHalves = null;
            }
            if (own == null && foe == null) return;

            var root = new GameObject("~Mats", typeof(RectTransform)) { hideFlags = HideFlags.DontSave };
            root.layer = gameObject.layer;
            builtHalves = (RectTransform)root.transform;
            builtHalves.SetParent(baseImage.rectTransform, false);
            builtHalves.anchorMin = Vector2.zero;
            builtHalves.anchorMax = Vector2.one;
            builtHalves.offsetMin = Vector2.zero;
            builtHalves.offsetMax = Vector2.zero;

            // Untere Hälfte = eigene Seite, obere = die des Gegners
            if (own != null) BuildHalf("Own", own, false);
            if (foe != null) BuildHalf("Foe", foe, true);

            // Eine feine Naht macht die Teilung zur Absicht statt zum Fehler
            var seam = MakeImage("Seam", builtHalves, new Color(0.784f, 0.643f, 0.361f, 0.22f));
            seam.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            seam.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            seam.rectTransform.offsetMin = new Vector2(0f, -1f);
            seam.rectTransform.offsetMax = new Vector2(0f, 1f);
        }

        private void BuildHalf(string name, Sprite mat, bool top)
        {
            var clip = new GameObject(name, typeof(RectTransform));
            clip.layer = gameObject.layer;
            var clipRect = (RectTransform)clip.transform;
            clipRect.SetParent(builtHalves, false);
            clipRect.anchorMin = new Vector2(0f, top ? 0.5f : 0f);
            clipRect.anchorMax = new Vector2(1f, top ? 1f : 0.5f);
            clipRect.offsetMin = Vector2.zero;
            clipRect.offsetMax = Vector2.zero;
            clip.AddComponent<RectMask2D>();

            // Das Bild spannt sich über das GANZE Feld, obwohl sein Elternteil nur
            // die halbe Höhe hat: für die obere Hälfte reicht das Feld von −1 bis 1
            // in Elternkoordinaten, für die untere von 0 bis 2.
            var image = MakeImage("Mat", clipRect, Color.white);
            image.sprite = mat;
            image.type = Image.Type.Simple;
            image.rectTransform.anchorMin = new Vector2(0f, top ? -1f : 0f);
            image.rectTransform.anchorMax = new Vector2(1f, top ? 1f : 2f);
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
        }

        private static Image MakeImage(string name, RectTransform parent, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }
    }
}
