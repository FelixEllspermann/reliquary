using TMPro;
using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Gold-Folien-Shimmer für das RELIQUARY-Wordmark: ein scrollender Gold-Leaf-Verlauf
    /// über die Glyphen (TMP-Vertexfarben). Erste = letzte Verlaufsstufe, damit die Schleife
    /// keine Naht zeigt. Fallback bei deaktivierter Komponente: flaches #C8A45C.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class GoldShimmerText : MonoBehaviour
    {
        [SerializeField, Range(2f, 30f), Tooltip("Dauer eines kompletten Shimmer-Durchlaufs")]
        private float cycleSeconds = 9f;
        [SerializeField, Range(1f, 4f), Tooltip("Breite des Verlaufs relativ zur Textbreite (2 = CSS background-size 200%)")]
        private float gradientScale = 2f;

        private TMP_Text text;
        private Gradient gradient;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                Key("#A6802F", 0f), Key("#F6E4B4", 0.14f), Key("#C8A45C", 0.28f),
                Key("#F8EED6", 0.42f), Key("#C8A45C", 0.58f), Key("#F6E4B4", 0.76f), Key("#A6802F", 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        }

        private static GradientColorKey Key(string hex, float time)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return new GradientColorKey(color, time);
        }

        private void LateUpdate()
        {
            if (text == null || !text.enabled) return;
            text.ForceMeshUpdate();
            var info = text.textInfo;
            if (info == null || info.characterCount == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                minX = Mathf.Min(minX, ch.bottomLeft.x);
                maxX = Mathf.Max(maxX, ch.topRight.x);
            }
            if (maxX <= minX) return;

            float width = (maxX - minX) * gradientScale;
            float offset = (Time.unscaledTime / cycleSeconds) % 1f;

            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                var mesh = info.meshInfo[ch.materialReferenceIndex];
                for (int v = 0; v < 4; v++)
                {
                    int vi = ch.vertexIndex + v;
                    float t = Mathf.Repeat((mesh.vertices[vi].x - minX) / width - offset, 1f);
                    mesh.colors32[vi] = gradient.Evaluate(t);
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
