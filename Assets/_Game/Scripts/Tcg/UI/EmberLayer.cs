using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Atmosphärische Glut-Partikel der Shell-Screens: rotierte Diamanten steigen vom unteren
    /// Rand 320px auf und verblassen (Peak-Deckkraft .55, 9–14s, gestaffelt). Rein dekorativ.
    /// </summary>
    public class EmberLayer : MonoBehaviour
    {
        [SerializeField, Tooltip("Diamant-Sprite (whiteSquare aus dem CardSkin)")] private Sprite diamondSprite;
        [SerializeField, Range(1, 20)] private int count = 6;
        [SerializeField] private float riseDistance = 320f;

        private class Ember
        {
            public RectTransform Rect;
            public Image Image;
            public Color Color;
            public float Duration;
            public float Time;
            public float StartX;
            public float StartY;
        }

        private readonly List<Ember> embers = new List<Ember>();
        private static readonly string[] EmberColors = { "#C8A45C", "#C8A45C", "#EBCE8A", "#C8A45C", "#E0603A", "#C8A45C" };

        private void Start()
        {
            var area = (RectTransform)transform;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Ember_{i}", typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.sprite = diamondSprite;
                ColorUtility.TryParseHtmlString(EmberColors[i % EmberColors.Length], out var color);
                image.color = new Color(color.r, color.g, color.b, 0f);
                image.raycastTarget = false;
                var rect = (RectTransform)go.transform;
                float size = Random.Range(5f, 8f);
                rect.sizeDelta = new Vector2(size, size);
                rect.localRotation = Quaternion.Euler(0f, 0f, 45f);

                var ember = new Ember
                {
                    Rect = rect,
                    Image = image,
                    Color = color,
                    Duration = Random.Range(9f, 14f),
                    Time = -Random.Range(0f, 9f) // gestaffelter Start
                };
                Respawn(ember, area);
                embers.Add(ember);
            }
        }

        private void Respawn(Ember ember, RectTransform area)
        {
            float halfWidth = area.rect.width * 0.5f;
            ember.StartX = Random.Range(-halfWidth + 40f, halfWidth - 40f);
            ember.StartY = -area.rect.height * 0.5f - 10f;
            ember.Rect.anchoredPosition = new Vector2(ember.StartX, ember.StartY);
        }

        private void Update()
        {
            var area = (RectTransform)transform;
            foreach (var ember in embers)
            {
                ember.Time += Time.unscaledDeltaTime;
                if (ember.Time < 0f) continue;
                float k = ember.Time / ember.Duration;
                if (k >= 1f)
                {
                    ember.Time = 0f;
                    ember.Duration = Random.Range(9f, 14f);
                    Respawn(ember, area);
                    k = 0f;
                }
                ember.Rect.anchoredPosition = new Vector2(ember.StartX, ember.StartY + riseDistance * k);
                // Deckkraft 0 → .55 → 0 (dreieckig)
                float alpha = 0.55f * (k < 0.5f ? k * 2f : (1f - k) * 2f);
                ember.Image.color = new Color(ember.Color.r, ember.Color.g, ember.Color.b, alpha);
            }
        }
    }
}
