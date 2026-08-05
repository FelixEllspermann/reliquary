using Rouge.Tcg.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Das Rang-Emblem: eine Form, zehn Zustände. Grundlage ist immer ein um 45°
    /// gedrehtes Quadrat in einem 104er-Feld; jeder Rang legt genau eine Schicht
    /// dazu. Dadurch ist der Rang ablesbar, ohne den Namen zu lesen — und das
    /// Wachstum liest sich als Aufstieg statt als zehn verschiedene Abzeichen.
    ///
    /// Wird zur Laufzeit gebaut und über <see cref="Build"/> an ein beliebiges
    /// RectTransform gehängt. Drei Größen (siehe <see cref="Size"/>): unterhalb
    /// von 48 px verschmieren Pips und Ringe, deshalb fallen sie dort weg.
    /// </summary>
    public class RankEmblem : MonoBehaviour
    {
        public enum Size
        {
            Full,     // 96 px — Profil, Aufstiegs-Bildschirm: alle Schichten
            Compact,  // 48 px — Zeilen, Duell-Intro: ohne Ring, Speichen, Seiten-Pips
            Tiny      // 24 px — Leisten: nur Aussenraute und Kern
        }

        /// <summary>
        /// Die Schichten des Emblems, in der Reihenfolge, in der die Ränge sie
        /// freischalten. Der Zahlenwert IST der Rang, ab dem die Schicht da ist —
        /// deshalb hat jedes Siegel genau eine Ebene mehr als das darunter.
        /// </summary>
        public enum Layer
        {
            Outer = 1, Core = 2, Inner = 3, Axis = 4, SidePips = 5,
            CornerPips = 6, Ring = 7, Spokes = 8, Filled = 9, Halo = 10
        }

        /// <summary>Besitzt dieser Rang diese Schicht? Einzige Quelle der Regel.</summary>
        public static bool Has(int rank, Layer layer) => rank >= (int)layer;

        /// <summary>Bezugsgröße aller Werte im Handoff — alle Maße skalieren mit px/Box.</summary>
        public const float Box = 104f;

        private TransitionSkin skin;
        private RectTransform root;
        private Image glow;

        /// <summary>Baut ein Emblem als Kind von <paramref name="parent"/>.</summary>
        public static RankEmblem Build(RectTransform parent, int rank, Size size = Size.Full)
        {
            var go = new GameObject("RankEmblem", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var emblem = go.AddComponent<RankEmblem>();
            emblem.root = rect;
            emblem.skin = TransitionSkin.Load();
            emblem.Rebuild(rank, size);
            return emblem;
        }

        /// <summary>Zeichnet das Emblem für einen Rang neu.</summary>
        public void Rebuild(int rank, Size size)
        {
            for (int i = root.childCount - 1; i >= 0; i--) DestroyImmediate(root.GetChild(i).gameObject);
            if (skin == null) skin = TransitionSkin.Load();
            if (skin == null) return;

            rank = Mathf.Clamp(rank, 1, 10);
            float px = size == Size.Full ? 96f : size == Size.Compact ? 48f : 24f;
            float k = px / Box;                       // alle Handoff-Werte skalieren mit
            root.sizeDelta = new Vector2(px, px);

            var edge = RankLadder.Edge(rank);
            var light = RankLadder.Light(rank);
            var dark = RankLadder.Dark(rank);
            bool full = size == Size.Full;
            bool compact = size != Size.Tiny;

            // Schein — erst ab Gold Seal, dann zunehmend
            float glowAlpha = RankLadder.GlowAlpha(rank);
            if (glowAlpha > 0f && compact)
            {
                // Eng am Emblem halten — ein breiter Schein frisst die Zeichnung auf
                glow = Add("Glow", skin.glow, Tint(edge, glowAlpha * 0.30f), Box * 1.15f * k, 0f);
                glow.transform.SetAsFirstSibling();
            }

            // Rang 10: rotierender gestrichelter Ring plus ein zweiter, fester
            if (Has(rank, Layer.Halo) && full)
            {
                var spinner = Add("RotatingRing", skin.dashedRing, Tint(edge, 0.7f), 104f * k, 0f);
                spinner.gameObject.AddComponent<SlowSpin>();
                Add("SolidRing", skin.ring, Tint(edge, 0.45f), 92f * k, 0f);
            }

            // Rang 7: Ring
            if (Has(rank, Layer.Ring) && full) Add("Ring", skin.ring, Tint(edge, 0.50f), 100f * k, 0f);

            // Rang 8: Speichen (ab Rang 9 zusätzlich diagonal)
            if (Has(rank, Layer.Spokes) && full)
            {
                float[] angles = Has(rank, Layer.Filled) ? new[] { 0f, 90f, 45f, 135f } : new[] { 0f, 90f };
                foreach (float angle in angles)
                {
                    var spoke = Add("Spoke", skin.square, Tint(edge, 0.22f), 0f, angle);
                    spoke.rectTransform.sizeDelta = new Vector2(96f * k, 2f * k);
                }
            }

            // Rang 4: nicht gedrehtes Quadrat — die zweite Achse
            if (Has(rank, Layer.Axis) && compact)
            {
                var axis = Add("AxisSquare", skin.frame, Tint(edge, 0.32f), 88f * k, 0f);
                axis.type = Image.Type.Sliced;
            }

            // Rang 1: Aussenraute mit Verlaufsfüllung
            var fill = Add("OuterFill", skin.diagFade, Tint(edge, 0.22f), 88f * k, 45f);
            fill.transform.SetAsFirstSibling();
            var outer = Add("OuterDiamond", skin.frame, edge, 88f * k, 45f);
            outer.type = Image.Type.Sliced;

            // Rang 3: innere Raute, ab Rang 9 gefüllt
            if (Has(rank, Layer.Inner) && compact)
            {
                if (Has(rank, Layer.Filled)) Add("InnerFill", skin.diagFade, Tint(light, 0.35f), 52f * k, 45f);
                var inner = Add("InnerDiamond", skin.frame, Tint(edge, 0.60f), 52f * k, 45f);
                inner.type = Image.Type.Sliced;
            }

            // Rang 5: Seiten-Pips (unter 96 px werden sie zu Matsch — siehe Handoff),
            // Rang 6: Eck-Pips
            if (Has(rank, Layer.SidePips) && full)
                foreach (var offset in new[] { new Vector2(-44f, 0f), new Vector2(44f, 0f) })
                    Pip(offset * k, 8f * k, edge);
            if (Has(rank, Layer.CornerPips) && compact)
                foreach (var offset in new[] { new Vector2(-31f, -31f), new Vector2(31f, -31f),
                                               new Vector2(-31f, 31f), new Vector2(31f, 31f) })
                    Pip(offset * k, 8f * k, edge);

            // Rang 2: der Kern. Dunkle Basis, heller Verlauf darüber — so entsteht
            // der Metallverlauf, den eine einzelne Tönung nicht hergibt.
            if (Has(rank, Layer.Core))
            {
                float core = (Has(rank, Layer.Filled) ? 24f : 20f) * k;
                Add("CoreBase", skin.square, dark, core, 45f);
                Add("CoreSheen", skin.diagFade, light, core, 45f);
            }
        }

        private void Pip(Vector2 position, float size, Color color)
        {
            var pip = Add("Pip", skin.square, color, size, 45f);
            pip.rectTransform.anchoredPosition = position;
        }

        private Image Add(string name, Sprite sprite, Color color, float size, float rotation)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            if (size > 0f) rect.sizeDelta = new Vector2(size, size);
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Color Tint(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        /// <summary>Der gestrichelte Ring von Vault Seal dreht sich einmal in 22 Sekunden.</summary>
        private class SlowSpin : MonoBehaviour
        {
            private void Update() =>
                transform.Rotate(0f, 0f, -360f / 22f * Time.unscaledDeltaTime);
        }
    }
}
