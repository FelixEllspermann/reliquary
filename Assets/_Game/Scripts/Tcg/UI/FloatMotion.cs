using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>Sanftes Auf-und-Ab-Schweben (Shell-Keyframes float/floatSlow), unscaled time.</summary>
    public class FloatMotion : MonoBehaviour
    {
        [SerializeField] private float amplitude = 16f;
        [SerializeField] private float cycleSeconds = 6.5f;
        [SerializeField] private float delay;

        private RectTransform rect;
        private Vector2 basePos;

        private void Awake()
        {
            rect = (RectTransform)transform;
            basePos = rect.anchoredPosition;
        }

        public void Configure(float amp, float cycle, float startDelay)
        {
            amplitude = amp; cycleSeconds = cycle; delay = startDelay;
        }

        private void Update()
        {
            float t = Time.unscaledTime - delay;
            if (t < 0f) return;
            // ease-in-out-Sinus: 0 → -amplitude → 0
            float k = (1f - Mathf.Cos(t / cycleSeconds * Mathf.PI * 2f)) * 0.5f;
            rect.anchoredPosition = basePos + new Vector2(0f, amplitude * k);
        }
    }
}
