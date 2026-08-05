using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Hover-Verhalten der Menü-Kacheln: 12px-Lift in 160ms (ease-out) plus Accent-Glow.
    /// </summary>
    public class MenuTileHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private float lift = 12f;
        [SerializeField] private float duration = 0.16f;
        [SerializeField, Tooltip("Optionales Glow-Image (Accent, Alpha wird animiert)")] private Image glowImage;
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.32f;

        private RectTransform rect;
        private float baseY;
        private Coroutine animation;
        private bool hovered;
        private Graphic hitTarget;
        private Vector4 basePadding;

        private void Awake()
        {
            rect = (RectTransform)transform;
            baseY = rect.anchoredPosition.y;
            hitTarget = FindHitTarget();
            if (hitTarget != null) basePadding = hitTarget.raycastPadding;
            if (glowImage != null)
            {
                var color = glowImage.color;
                glowImage.color = new Color(color.r, color.g, color.b, 0f);
            }
        }

        /// <summary>Die Grafik, über die die Kachel angeklickt wird.</summary>
        private Graphic FindHitTarget()
        {
            var selectable = GetComponent<Selectable>();
            if (selectable != null && selectable.targetGraphic != null) return selectable.targetGraphic;
            var own = GetComponent<Graphic>();
            if (own != null && own.raycastTarget) return own;
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) return graphic;
            return null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hovered) return;
            hovered = true;
            SfxManager.Hover();
            Animate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hovered) return;
            hovered = false;
            Animate(false);
        }

        public void OnPointerClick(PointerEventData eventData) => SfxManager.Click();

        /// <summary>
        /// Die Kachel wandert beim Hovern nach oben — damit rutscht sie unter dem Zeiger
        /// weg, fällt zurück, wird wieder getroffen und das Ganze klappert endlos. Die
        /// Trefferfläche wird deshalb um genau den Hub nach unten verlängert, sodass sie
        /// über der ursprünglichen Fläche stehen bleibt.
        /// </summary>
        private void ApplyHitCompensation(bool lifted)
        {
            if (hitTarget == null) return;
            hitTarget.raycastPadding = lifted
                ? new Vector4(basePadding.x, basePadding.y - lift, basePadding.z, basePadding.w)
                : basePadding;
        }

        private void OnDisable()
        {
            hovered = false;
            ApplyHitCompensation(false);
            if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY);
        }

        private void Animate(bool lifted)
        {
            ApplyHitCompensation(lifted);
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(AnimateRoutine(lifted));
        }

        private IEnumerator AnimateRoutine(bool hovered)
        {
            float targetY = baseY + (hovered ? lift : 0f);
            float targetGlow = hovered ? glowAlpha : 0f;
            float startY = rect.anchoredPosition.y;
            float startGlow = glowImage != null ? glowImage.color.a : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                k = 1f - (1f - k) * (1f - k); // ease-out
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Lerp(startY, targetY, k));
                if (glowImage != null)
                {
                    var color = glowImage.color;
                    glowImage.color = new Color(color.r, color.g, color.b, Mathf.Lerp(startGlow, targetGlow, k));
                }
                yield return null;
            }
        }
    }
}
