using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Sicht- und hörbares Feedback für Buttons: Hover vergrößert leicht, legt einen
    /// Schein hinter den Knopf und hellt Rahmen und Beschriftung auf; ein Klick drückt
    /// ihn kurz ein. Wird vom <see cref="UiFxInstaller"/> automatisch an jeden Button
    /// gehängt — dadurch reagiert auch neu erzeugte UI (Deck-Zeilen, Lobby-Listen).
    ///
    /// Die Farben werden erst beim Betreten gesichert und beim Verlassen exakt
    /// zurückgesetzt: so bleiben Zustandsfarben erhalten, die andere Skripte setzen
    /// (z.B. „nicht genug Coins“ am Kaufen-Knopf).
    /// </summary>
    [DisallowMultipleComponent]
    public class UiButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Skalierung beim Überfahren")]
        [SerializeField] private float hoverScale = 1.05f;

        [Tooltip("Skalierung während des Drückens")]
        [SerializeField] private float pressScale = 0.955f;

        [Tooltip("Deckkraft des Aufleuchtens über dem Knopf")]
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.18f;

        [Tooltip("Wie schnell Skalierung und Schein nachziehen")]
        [SerializeField] private float speed = 15f;

        [SerializeField] private bool playSounds = true;

        private static readonly Color DefaultAccent = new Color(200f / 255f, 164f / 255f, 92f / 255f, 1f);

        private Selectable selectable;
        private RectTransform rect;
        private Image glow;
        private Vector3 baseScale = Vector3.one;
        private bool hovered;
        private bool pressed;

        private readonly List<Graphic> tintTargets = new List<Graphic>();
        private readonly List<Color> tintBackup = new List<Color>();
        private bool tinted;

        /// <summary>Stärkeres Feedback für besonders wichtige Knöpfe (End Turn, Battle …).</summary>
        public void SetStrength(float scale, float glowStrength)
        {
            hoverScale = scale;
            glowAlpha = glowStrength;
            if (glow != null) glow.gameObject.SetActive(glowStrength > 0.001f);
        }

        /// <summary>Hängt das Feedback an einen Knopf und gibt die Komponente zurück.</summary>
        public static UiButtonFx Attach(Component target)
        {
            if (target == null) return null;
            var existing = target.GetComponent<UiButtonFx>();
            return existing != null ? existing : target.gameObject.AddComponent<UiButtonFx>();
        }

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            rect = transform as RectTransform;
            baseScale = transform.localScale;
            CollectTintTargets();
            BuildGlow();
        }

        private void OnDisable()
        {
            RestoreTint();
            hovered = false;
            pressed = false;
            transform.localScale = baseScale;
            if (glow != null) SetGlowAlpha(0f);
        }

        private void Update()
        {
            // Wird der Knopf während des Hoverns gesperrt, sofort in den Ruhezustand
            if (hovered && selectable != null && !selectable.IsInteractable()) Leave();

            float target = pressed ? pressScale : (hovered ? hoverScale : 1f);
            float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, k);

            if (glow != null)
            {
                float wanted = hovered ? (pressed ? glowAlpha * 1.35f : glowAlpha) : 0f;
                SetGlowAlpha(Mathf.Lerp(glow.color.a, wanted, k));
            }
        }

        // ================== ZEIGER ==================

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            hovered = true;
            ApplyTint();
            if (playSounds) SfxManager.Hover(SfxManager.ButtonHoverGain);
        }

        public void OnPointerExit(PointerEventData eventData) => Leave();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            pressed = true;
            if (playSounds) SfxManager.Click();
        }

        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        private void Leave()
        {
            hovered = false;
            pressed = false;
            RestoreTint();
        }

        // ================== DARSTELLUNG ==================

        /// <summary>
        /// Aufzuhellen sind Rahmen/Keylines und Beschriftungen — nicht die Grafik, die
        /// Unity selbst über den ColorTint des Selectable steuert.
        /// </summary>
        private void CollectTintTargets()
        {
            var skip = selectable != null ? selectable.targetGraphic : null;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text != skip) tintTargets.Add(text);
            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                if (image == skip || image == glow) continue;
                string n = image.gameObject.name;
                if (n == "Frame" || n == "Keyline" || n == "Border" || n == "Outline" || n == "Stripe" || n == "Icon")
                    tintTargets.Add(image);
            }
        }

        private void ApplyTint()
        {
            if (tinted) return;
            tinted = true;
            tintBackup.Clear();
            foreach (var graphic in tintTargets)
            {
                if (graphic == null) { tintBackup.Add(Color.white); continue; }
                var original = graphic.color;
                tintBackup.Add(original);
                var brighter = Color.Lerp(original, Color.white, 0.45f);
                // Schwach sichtbare Rahmen zusätzlich in der Deckkraft anheben
                brighter.a = Mathf.Clamp01(original.a < 0.9f ? original.a * 1.7f + 0.12f : original.a);
                graphic.color = brighter;
            }
        }

        private void RestoreTint()
        {
            if (!tinted) return;
            tinted = false;
            for (int i = 0; i < tintTargets.Count && i < tintBackup.Count; i++)
                if (tintTargets[i] != null) tintTargets[i].color = tintBackup[i];
        }

        /// <summary>
        /// Aufleuchten über der gesamten Knopffläche. Liegt bewusst als letztes Kind
        /// obenauf: so wirkt es unabhängig davon, ob der Hintergrund auf dem Knopf
        /// selbst oder in einem Kind-Objekt liegt.
        /// </summary>
        private void BuildGlow()
        {
            if (rect == null || glowAlpha <= 0.001f) return;
            // Knöpfe, die ihre Größe aus ihren Kindern ableiten, würden davon wachsen
            if (GetComponent<LayoutGroup>() != null || GetComponent<ContentSizeFitter>() != null) return;

            var go = new GameObject("~FxGlow", typeof(RectTransform));
            go.layer = gameObject.layer;
            var glowRect = (RectTransform)go.transform;
            glowRect.SetParent(rect, false);
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-3f, -3f);
            glowRect.offsetMax = new Vector2(3f, 3f);
            glowRect.SetAsLastSibling();

            var ignore = go.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;

            var accent = Accent();
            glow = go.AddComponent<Image>();
            glow.raycastTarget = false;
            glow.color = new Color(accent.r, accent.g, accent.b, 0f);
        }

        /// <summary>Akzentfarbe aus einem vorhandenen Rahmen ableiten, sonst das Reliquary-Gold.</summary>
        private Color Accent()
        {
            foreach (var graphic in tintTargets)
            {
                if (graphic is Image image && image.color.a > 0.05f)
                    return new Color(image.color.r, image.color.g, image.color.b, 1f);
            }
            return DefaultAccent;
        }

        private void SetGlowAlpha(float alpha)
        {
            var color = glow.color;
            color.a = alpha;
            glow.color = color;
        }
    }
}
