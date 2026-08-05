using System.Collections.Generic;
using Rouge.Tcg.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Das Rang-Siegel als animierbare Fassung von <see cref="RankEmblem"/>:
    /// dieselbe Geometrie und dieselben Schichtregeln, aber einmal gebaut und
    /// danach je Bild neu gestellt.
    ///
    /// Zwei Bewegungen, die sich gegenseitig ausschliessen:
    /// <b>scatter</b> sprengt das Siegel (das alte im Bruch), <b>forge</b> baut es
    /// von innen nach aussen auf (das neue im Schmieden).
    ///
    /// Wichtig: nicht jede Schicht bewegt sich gleich. Nur die Quadranten der
    /// Aussenraute und die Pips <i>fliegen</i>; Ring, Achse, Speichen und Kern
    /// <i>dehnen</i> sich stattdessen. Und beim Schmieden erscheinen Ring, Achse
    /// und Speichen allein über ihre Deckkraft — sie skalieren nicht aus dem
    /// Nichts heraus, sondern der Ring fährt von aussen auf seine Größe zu.
    /// </summary>
    public class RankSealView : MonoBehaviour
    {
        // Fensterlage je Schicht beim Schmieden (Anteile von `forge`)
        private static readonly Vector2 WCore = new Vector2(0.00f, 0.26f);
        private static readonly Vector2 WOuter = new Vector2(0.16f, 0.50f);
        private static readonly Vector2 WInner = new Vector2(0.32f, 0.62f);
        private static readonly Vector2 WAxis = new Vector2(0.44f, 0.70f);
        private static readonly Vector2 WPips = new Vector2(0.56f, 0.82f);
        private static readonly Vector2 WRing = new Vector2(0.66f, 0.94f);
        private static readonly Vector2 WSpoke = new Vector2(0.74f, 1.00f);

        /// <summary>Wie sich eine Schicht bewegt — jede hat ihre eigene Formel.</summary>
        private enum Kind { Quadrant, Pip, Ring, Halo, Spoke, Axis, Inner, Core }

        private class Piece
        {
            public Kind Kind;
            public RectTransform Rect;
            public Image Image;
            public Vector2 Home;
            public Vector2 FlyDirection;
            public float FlySpin;
            public float BaseSize;        // Ruhegröße (Kern, Pips, Speichenlänge)
            public float BaseAlpha;
            public float HomeRotation;
        }

        private readonly List<Piece> pieces = new List<Piece>();
        private RectTransform root;
        private Image halo;
        private TransitionSkin skin;
        private int rank = 1;
        private float box = 182f;

        public RectTransform Rect => root;

        public static RankSealView Build(RectTransform parent, int rank, float box = 182f)
        {
            var go = new GameObject("RankSeal", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(box, box);

            var view = go.AddComponent<RankSealView>();
            view.root = rect;
            view.skin = TransitionSkin.Load();
            view.box = box;
            view.Rebuild(rank);
            return view;
        }

        public void Rebuild(int newRank)
        {
            for (int i = root.childCount - 1; i >= 0; i--) DestroyImmediate(root.GetChild(i).gameObject);
            pieces.Clear();
            halo = null;
            if (skin == null) skin = TransitionSkin.Load();
            if (skin == null) return;

            rank = Mathf.Clamp(newRank, 1, 10);
            float k = box / RankEmblem.Box;
            root.sizeDelta = new Vector2(box, box);

            var edge = RankLadder.Edge(rank);
            var light = RankLadder.Light(rank);
            var dark = RankLadder.Dark(rank);

            // Der weiche Schein sitzt ganz hinten und wird nur ausgeblendet
            halo = Raw("Glow", skin.glow, Tint(edge, 0.3f), box * 1.05f, 0f);

            if (RankEmblem.Has(rank, RankEmblem.Layer.Halo))
            {
                var dashed = Add(Kind.Halo, "RotatingRing", skin.dashedRing, Tint(edge, 0.5f), box, 0f);
                dashed.Rect.gameObject.AddComponent<SlowSpin>();
            }
            if (RankEmblem.Has(rank, RankEmblem.Layer.Ring))
                Add(Kind.Ring, "Ring", skin.ring, Tint(edge, 0.5f), box * 0.96f, 0f);

            if (RankEmblem.Has(rank, RankEmblem.Layer.Spokes))
            {
                float[] angles = RankEmblem.Has(rank, RankEmblem.Layer.Filled)
                    ? new[] { 0f, 90f, 45f, 135f } : new[] { 0f, 90f };
                foreach (float angle in angles)
                {
                    var spoke = Add(Kind.Spoke, "Spoke", skin.square, Tint(edge, 0.24f), 0f, angle);
                    spoke.Rect.sizeDelta = new Vector2(box, 2f);
                    spoke.BaseSize = box;
                }
            }

            if (RankEmblem.Has(rank, RankEmblem.Layer.Axis))
            {
                var axis = Add(Kind.Axis, "AxisSquare", skin.frame, Tint(edge, 0.32f), 88f * k, 0f);
                axis.Image.type = Image.Type.Sliced;
            }

            BuildOuterQuadrants(k, edge);

            if (RankEmblem.Has(rank, RankEmblem.Layer.Inner))
            {
                if (RankEmblem.Has(rank, RankEmblem.Layer.Filled))
                    Add(Kind.Inner, "InnerFill", skin.diagFade, Tint(light, 0.32f), 52f * k, 45f);
                var inner = Add(Kind.Inner, "InnerDiamond", skin.frame, Tint(edge, 0.65f), 52f * k, 45f);
                inner.Image.type = Image.Type.Sliced;
            }

            if (RankEmblem.Has(rank, RankEmblem.Layer.SidePips))
                foreach (var offset in new[] { new Vector2(-44f, 0f), new Vector2(44f, 0f) })
                    Pip(offset * k, 8f * k, edge);
            if (RankEmblem.Has(rank, RankEmblem.Layer.CornerPips))
                foreach (var offset in new[] { new Vector2(-31f, -31f), new Vector2(31f, -31f),
                                               new Vector2(-31f, 31f), new Vector2(31f, 31f) })
                    Pip(offset * k, 8f * k, edge);

            if (RankEmblem.Has(rank, RankEmblem.Layer.Core))
            {
                float core = (RankEmblem.Has(rank, RankEmblem.Layer.Filled) ? 24f : 20f) * k;
                var baseCore = Add(Kind.Core, "CoreBase", skin.square, dark, core, 45f);
                baseCore.BaseSize = core;
                var sheen = Add(Kind.Core, "CoreSheen", skin.diagFade, light, core, 45f);
                sheen.BaseSize = core;
            }

            Apply(0f, 1f, 0.4f, 1f);
        }

        private void BuildOuterQuadrants(float k, Color edge)
        {
            float outer = 88f * k;
            float half = outer * 0.75f;   // Fenstergröße: out * 1.5 / 2

            for (int i = 0; i < 4; i++)
            {
                float dx = i % 2 == 0 ? -1f : 1f;
                float dy = i < 2 ? -1f : 1f;

                var window = new GameObject("OuterQuad" + i, typeof(RectTransform));
                var windowRect = (RectTransform)window.transform;
                windowRect.SetParent(root, false);
                windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
                windowRect.pivot = new Vector2(0.5f, 0.5f);
                windowRect.sizeDelta = new Vector2(half, half);
                windowRect.anchoredPosition = new Vector2(dx * half * 0.5f, dy * half * 0.5f);
                window.AddComponent<RectMask2D>();

                // Die Raute sitzt im Fenster gegenläufig versetzt und bleibt so mittig
                var offset = new Vector2(-dx * half * 0.5f, -dy * half * 0.5f);
                var fill = Raw("Fill", skin.diagFade, Tint(edge, 0.22f), outer, 45f);
                fill.rectTransform.SetParent(windowRect, false);
                fill.rectTransform.anchoredPosition = offset;
                var line = Raw("Line", skin.frame, edge, outer, 45f);
                line.type = Image.Type.Sliced;
                line.rectTransform.SetParent(windowRect, false);
                line.rectTransform.anchoredPosition = offset;

                pieces.Add(new Piece
                {
                    Kind = Kind.Quadrant,
                    Rect = windowRect,
                    Image = null,
                    Home = windowRect.anchoredPosition,
                    FlyDirection = new Vector2(dx, dy) * 0.7f,
                    FlySpin = dx * 16f,
                    BaseAlpha = 1f,
                    HomeRotation = 0f,
                });
            }
        }

        /// <summary>Stellt das Siegel auf einen Zustand.</summary>
        public void Apply(float scatter, float forge, float glow, float fade)
        {
            if (root == null) return;
            root.gameObject.SetActive(fade > 0.001f && forge > 0.001f);
            if (!root.gameObject.activeSelf) return;

            float s = Motion.Enter(scatter);
            float fly = s * box * 1.15f;

            float wCore = Motion.Pop(Motion.Seg(forge, WCore.x, WCore.y));
            float wOuter = Motion.Pop(Motion.Seg(forge, WOuter.x, WOuter.y));
            float wInner = Motion.Pop(Motion.Seg(forge, WInner.x, WInner.y));
            float wAxis = Motion.Seg(forge, WAxis.x, WAxis.y);
            float wPips = Motion.Pop(Motion.Seg(forge, WPips.x, WPips.y));
            float wRing = Motion.Seg(forge, WRing.x, WRing.y);
            float wSpoke = Motion.Seg(forge, WSpoke.x, WSpoke.y);

            foreach (var piece in pieces)
            {
                switch (piece.Kind)
                {
                    case Kind.Quadrant:
                        piece.Rect.anchoredPosition = piece.Home + piece.FlyDirection * fly;
                        piece.Rect.localEulerAngles = new Vector3(0f, 0f, s * piece.FlySpin);
                        SetQuadrant(piece, wOuter, DieOut(scatter, 0.8f) * fade);
                        break;

                    case Kind.Pip:
                    {
                        float size = piece.BaseSize * Mathf.Max(0.001f, wPips);
                        piece.Rect.sizeDelta = new Vector2(size, size);
                        piece.Rect.anchoredPosition = piece.Home + piece.FlyDirection * fly;
                        SetAlpha(piece, Mathf.Clamp01(wPips) * DieOut(scatter, 0.7f) * fade);
                        break;
                    }

                    case Kind.Ring:
                        // Fährt von 1.45 auf 1 zu und dehnt sich beim Bruch
                        piece.Rect.localScale = Vector3.one
                            * (Motion.Mix(1.45f, 1f, Motion.Enter(wRing)) * (1f + s * 0.6f));
                        SetAlpha(piece, wRing * DieOut(scatter, 0.85f) * fade);
                        break;

                    case Kind.Halo:
                        piece.Rect.localScale = Vector3.one * Motion.Mix(1.4f, 1f, Motion.Enter(wRing));
                        SetAlpha(piece, wRing * DieOut(scatter, 0.85f) * fade);
                        break;

                    case Kind.Spoke:
                        // Speichen fliegen nicht, sie werden länger
                        piece.Rect.sizeDelta = new Vector2(piece.BaseSize * (1f + s * 0.8f), 2f);
                        SetAlpha(piece, wSpoke * DieOut(scatter, 0.75f) * fade);
                        break;

                    case Kind.Axis:
                        piece.Rect.localScale = Vector3.one * (1f + s * 0.5f);
                        SetAlpha(piece, wAxis * DieOut(scatter, 0.8f) * fade);
                        break;

                    case Kind.Inner:
                        piece.Rect.localScale = Vector3.one
                            * Mathf.Max(0.001f, wInner * (1f + s * 0.9f));
                        SetAlpha(piece, Mathf.Clamp01(wInner) * DieOut(scatter, 0.6f) * fade);
                        break;

                    case Kind.Core:
                    {
                        float size = piece.BaseSize * Mathf.Max(0.001f, wCore) * (1f + s * 2.6f);
                        piece.Rect.sizeDelta = new Vector2(size, size);
                        SetAlpha(piece, Mathf.Clamp01(wCore) * DieOut(scatter, 0.5f) * fade);
                        break;
                    }
                }
            }

            if (halo != null)
            {
                var edge = RankLadder.Edge(rank);
                halo.color = Tint(edge, (0.1f + glow * 0.24f) * fade * Mathf.Clamp01(forge));
                halo.rectTransform.localScale = Vector3.one * (1f + glow * 0.2f + s * 0.6f);
            }
        }

        /// <summary>1 solange die Schicht lebt, 0 ab ihrer eigenen Schwelle.</summary>
        private static float DieOut(float scatter, float at) => 1f - Mathf.Clamp01(scatter / at);

        private static void SetQuadrant(Piece piece, float grow, float alpha)
        {
            for (int i = 0; i < piece.Rect.childCount; i++)
            {
                var child = piece.Rect.GetChild(i);
                var image = child.GetComponent<Image>();
                if (image == null) continue;
                child.localScale = Vector3.one * Mathf.Max(0.001f, grow);
                float baseAlpha = child.name == "Fill" ? 0.22f : 1f;
                var color = image.color;
                image.color = new Color(color.r, color.g, color.b, alpha * baseAlpha);
            }
        }

        private static void SetAlpha(Piece piece, float alpha)
        {
            if (piece.Image == null) return;
            var color = piece.Image.color;
            piece.Image.color = new Color(color.r, color.g, color.b, alpha * piece.BaseAlpha);
        }

        private void Pip(Vector2 position, float size, Color color)
        {
            var direction = position.sqrMagnitude > 0.001f ? position.normalized : Vector2.up;
            var piece = Add(Kind.Pip, "Pip", skin.square, color, size, 45f);
            piece.Rect.anchoredPosition = position;
            piece.Home = position;
            piece.FlyDirection = direction * 0.9f;
            piece.BaseSize = size;
        }

        private Piece Add(Kind kind, string name, Sprite sprite, Color color, float size, float rotation)
        {
            var image = Raw(name, sprite, color, size, rotation);
            var piece = new Piece
            {
                Kind = kind,
                Rect = image.rectTransform,
                Image = image,
                Home = Vector2.zero,
                FlyDirection = Vector2.zero,
                FlySpin = 0f,
                BaseSize = size,
                BaseAlpha = color.a,
                HomeRotation = rotation,
            };
            pieces.Add(piece);
            return piece;
        }

        private Image Raw(string name, Sprite sprite, Color color, float size, float rotation)
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
            private void Update() => transform.Rotate(0f, 0f, -360f / 22f * Time.unscaledDeltaTime);
        }
    }
}
