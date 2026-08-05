using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Faden und Fadenkreuz der Zielwahl (Handoff „Animations", Abschnitt 3).
    ///
    /// Vier Eckwinkel fahren von 120 px auf 8 px zu, dazu eine Raute in der Mitte,
    /// die von 120° auf 0 dreht und von 1.6 auf 1 schrumpft. Das Fadenkreuz
    /// wandert dabei vom Zauberer zum Ziel — dadurch sieht man, WOHER die
    /// Zielwahl kommt, statt dass sie einfach auf dem Opfer erscheint.
    ///
    /// Der Faden ist zwei Pixel breit und bleibt nach dem Einrasten bei 45 %
    /// stehen: die Verbindung bleibt lesbar, drängt sich aber nicht mehr auf.
    /// </summary>
    public class TargetReticle : MonoBehaviour
    {
        private static readonly Color Ember = new Color(0.878f, 0.376f, 0.227f);      // #E0603A
        private static readonly Color EmberLit = new Color(0.953f, 0.765f, 0.651f);   // #F3C3A6

        private const float Arm = 26f, Thickness = 3f;

        public RectTransform Rect { get; private set; }

        private RectTransform thread, core;
        private Image threadImage, coreImage;
        private readonly RectTransform[] brackets = new RectTransform[8];   // je Ecke zwei Balken
        private readonly Image[] bracketImages = new Image[8];

        public static TargetReticle Build(RectTransform parent)
        {
            var skin = TransitionSkin.Load();
            var go = new GameObject("~Reticle", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var reticle = go.AddComponent<TargetReticle>();
            reticle.Rect = rect;
            reticle.Create(skin);
            return reticle;
        }

        private void Create(TransitionSkin skin)
        {
            // Der Faden hängt am Reticle-Elternteil, nicht am Fadenkreuz selbst —
            // er verbindet zwei Punkte und darf nicht mitwandern
            thread = MakeRect("Thread", (RectTransform)Rect.parent);
            thread.pivot = new Vector2(0f, 0.5f);
            threadImage = thread.gameObject.AddComponent<Image>();
            threadImage.sprite = skin != null ? skin.rule : null;
            threadImage.raycastTarget = false;

            for (int i = 0; i < 4; i++)
            {
                float dx = i % 2 == 0 ? -1f : 1f;
                float dy = i < 2 ? -1f : 1f;
                // Ein Winkel besteht aus zwei Balken; ein L, kein Rahmen
                brackets[i * 2] = MakeBar(skin, Arm, Thickness, out bracketImages[i * 2]);
                brackets[i * 2 + 1] = MakeBar(skin, Thickness, Arm, out bracketImages[i * 2 + 1]);
                brackets[i * 2].name = $"ArmH{dx}{dy}";
                brackets[i * 2 + 1].name = $"ArmV{dx}{dy}";
            }

            core = MakeRect("Core", Rect);
            core.sizeDelta = new Vector2(15f, 15f);
            coreImage = core.gameObject.AddComponent<Image>();
            coreImage.sprite = skin != null ? skin.frame : null;
            coreImage.type = Image.Type.Sliced;
            coreImage.color = EmberLit;
            coreImage.raycastTarget = false;
        }

        private RectTransform MakeBar(TransitionSkin skin, float width, float height, out Image image)
        {
            var rect = MakeRect("Arm", Rect);
            rect.sizeDelta = new Vector2(width, height);
            image = rect.gameObject.AddComponent<Image>();
            image.sprite = skin != null ? skin.square : null;
            image.color = Ember;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>
        /// Stellt Faden und Fadenkreuz.
        /// <paramref name="at"/> ist die Position des Fadenkreuzes (es wandert),
        /// <paramref name="to"/> das Ziel (dort endet der Faden).
        /// </summary>
        public void Apply(Vector2 at, Vector2 to, Vector2 targetSize,
                          float threadLength, float lockAmount, float spin, float threadAlpha)
        {
            Rect.anchoredPosition = at;

            // Der Faden startet am unteren Rand der wirkenden Karte
            var from = new Vector2(0f, -40f);
            var delta = to - from;
            float length = delta.magnitude;
            thread.gameObject.SetActive(threadLength > 0.002f && threadAlpha > 0.002f);
            if (thread.gameObject.activeSelf)
            {
                thread.anchoredPosition = from;
                thread.sizeDelta = new Vector2(length * Mathf.Clamp01(threadLength), 2f);
                thread.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                threadImage.color = new Color(EmberLit.r, EmberLit.g, EmberLit.b, 0.85f * threadAlpha);
            }

            // Winkel fahren von 120 px auf 8 px an das Ziel heran
            float reach = Motion.Mix(120f, 8f, Motion.Enter(lockAmount));
            float halfW = targetSize.x * 0.5f + reach;
            float halfH = targetSize.y * 0.5f + reach;
            for (int i = 0; i < 4; i++)
            {
                float dx = i % 2 == 0 ? -1f : 1f;
                float dy = i < 2 ? -1f : 1f;
                brackets[i * 2].anchoredPosition = new Vector2(
                    dx * (halfW - Arm * 0.5f), dy * halfH);
                brackets[i * 2 + 1].anchoredPosition = new Vector2(
                    dx * halfW, dy * (halfH - Arm * 0.5f));
                var tint = new Color(Ember.r, Ember.g, Ember.b, 1f);
                bracketImages[i * 2].color = tint;
                bracketImages[i * 2 + 1].color = tint;
            }

            core.localEulerAngles = new Vector3(0f, 0f, 45f + spin);
            core.localScale = Vector3.one * Motion.Mix(1.6f, 1f, Motion.Enter(lockAmount));
        }

        private void OnDestroy()
        {
            if (thread != null) Destroy(thread.gameObject);
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
    }
}
