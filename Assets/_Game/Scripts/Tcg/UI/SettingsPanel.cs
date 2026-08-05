using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Einstellungs-Overlay im Reliquary-Stil — derzeit nur Sound (Musiklautstärke).
    /// Öffnet über den Settings-Button in der TopBar; Scrim-Klick oder DONE schließt.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Verdrahtung")]
        [Tooltip("Button in der TopBar, der das Menü öffnet")]
        [SerializeField] private Button openButton;

        [Tooltip("Wurzel des Overlays (Scrim + Panel) — startet inaktiv")]
        [SerializeField] private GameObject overlay;

        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button scrimButton;
        [SerializeField] private Button doneButton;

        [Header("Musik")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicPercent;

        [Header("Verhalten")]
        [Range(0.05f, 0.6f)]
        [Tooltip("Dauer des Ein-/Ausblendens")]
        [SerializeField] private float fadeDuration = 0.16f;

        private Coroutine animRoutine;
        private Slider sfxSlider;
        private TMP_Text sfxPercent;
        private Slider shakeSlider;
        private TMP_Text shakePercent;
        private TMP_Text sfxLabel;      // gemerkt, damit die dritte Zeile ihn klonen kann
        private float rowSpacing = 52f;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (doneButton != null) doneButton.onClick.AddListener(Close);
            if (scrimButton != null) scrimButton.onClick.AddListener(Close);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            BuildSfxRow();
            BuildShakeRow();
            if (overlay != null) overlay.SetActive(false);
        }

        public void Open()
        {
            if (overlay == null) return;
            overlay.SetActive(true);
            float current = MusicManager.Instance != null ? MusicManager.Instance.MusicVolume : 0.4f;
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(current);
            UpdatePercent(current);

            float effects = SfxManager.Instance != null ? SfxManager.Instance.SfxVolume : 0.7f;
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(effects);
            if (sfxPercent != null) sfxPercent.text = $"{Mathf.RoundToInt(effects * 100f)}%";

            if (shakeSlider != null) shakeSlider.SetValueWithoutNotify(ScreenShake.Strength);
            UpdateShakeLabel(ScreenShake.Strength);
            StartAnim(true);
        }

        /// <summary>
        /// Zweite Zeile für die Effektlautstärke. Kopiert werden genau die drei Teile der
        /// Musik-Zeile (Beschriftung, Regler, Prozentwert) und an die Stelle des
        /// Platzhalter-Hinweises gesetzt — der Hinweis kündigt weitere Optionen an, und
        /// diese hier ist die erste davon.
        /// </summary>
        private void BuildSfxRow()
        {
            if (musicSlider == null || musicPercent == null) return;
            var parent = musicSlider.transform.parent as RectTransform;
            if (parent == null || musicPercent.transform.parent != parent) return;

            var sliderRect = (RectTransform)musicSlider.transform;
            float rowY = sliderRect.anchoredPosition.y;

            var hint = FindHintBelow(parent, rowY);
            float targetY = hint != null ? ((RectTransform)hint.transform).anchoredPosition.y : rowY - 36f;
            if (hint != null) hint.gameObject.SetActive(false);

            sfxSlider = CopyToRow(musicSlider.gameObject, parent, "SfxSlider", targetY).GetComponent<Slider>();
            sfxPercent = CopyToRow(musicPercent.gameObject, parent, "SfxPercent", targetY).GetComponent<TMP_Text>();

            rowSpacing = Mathf.Abs(rowY - targetY);

            var label = FindRowLabel(parent, rowY);
            if (label != null)
            {
                sfxLabel = CopyToRow(label.gameObject, parent, "SfxLabel", targetY).GetComponent<TMP_Text>();
                if (sfxLabel != null) sfxLabel.text = "SOUND";
            }

            if (sfxSlider == null) return;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        /// <summary>
        /// Dritte Zeile: Stärke des Bildschirmrüttelns. Wird genauso aus der Musik-Zeile
        /// geklont wie die Sound-Zeile und darunter gesetzt. 0 % schaltet es ganz ab.
        /// </summary>
        private void BuildShakeRow()
        {
            if (sfxSlider == null || sfxPercent == null) return;
            var parent = sfxSlider.transform.parent as RectTransform;
            if (parent == null) return;

            float sfxY = ((RectTransform)sfxSlider.transform).anchoredPosition.y;
            float targetY = sfxY - rowSpacing;

            shakeSlider = CopyToRow(sfxSlider.gameObject, parent, "ShakeSlider", targetY).GetComponent<Slider>();
            shakePercent = CopyToRow(sfxPercent.gameObject, parent, "ShakePercent", targetY).GetComponent<TMP_Text>();

            // Direkt die Sound-Beschriftung klonen: die Suche würde sonst den
            // ausgeblendeten Platzhalter-Hinweis auf gleicher Höhe erwischen — und
            // eine Kopie davon wäre ebenfalls unsichtbar.
            if (sfxLabel != null)
            {
                var labelCopy = CopyToRow(sfxLabel.gameObject, parent, "ShakeLabel", targetY).GetComponent<TMP_Text>();
                if (labelCopy != null) labelCopy.text = "SCREEN SHAKE";
            }

            MakeRoomForThirdRow();

            if (shakeSlider == null) return;
            shakeSlider.minValue = 0f;
            shakeSlider.maxValue = 1.5f;
            shakeSlider.onValueChanged.RemoveAllListeners();
            shakeSlider.onValueChanged.AddListener(OnShakeChanged);
        }

        /// <summary>
        /// Das Panel war für zwei Zeilen gebaut — die dritte braucht Platz, sonst liegt
        /// der DONE-Knopf auf dem Regler. Panel wächst nach unten, alles unterhalb der
        /// neuen Zeile rutscht mit.
        /// </summary>
        private void MakeRoomForThirdRow()
        {
            if (panel == null || shakeSlider == null) return;
            float shakeY = ((RectTransform)shakeSlider.transform).anchoredPosition.y;
            float oldHeight = panel.rect.height;

            // Der sichtbare Kasten sind eigene Kinder (Hintergrund, Rahmen) mit fester
            // Höhe — die müssen mitwachsen, sonst verlängert sich nur ein unsichtbares
            // Rechteck und der DONE-Knopf landet auf der Rahmenkante.
            var chrome = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in panel)
            {
                var rect = child as RectTransform;
                if (rect != null && rect.rect.height >= oldHeight * 0.9f) chrome.Add(rect);
            }

            panel.sizeDelta = new Vector2(panel.sizeDelta.x, panel.sizeDelta.y + rowSpacing);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x,
                panel.anchoredPosition.y - rowSpacing * (1f - panel.pivot.y));

            // Durch den Pivot rutscht der Inhalt beim Wachsen mit. Oben wird das
            // zurückgenommen, unten auf genau eine Zeile aufgefüllt.
            float liftAbove = rowSpacing * (1f - panel.pivot.y);
            float dropBelow = -rowSpacing * panel.pivot.y;

            foreach (Transform child in panel)
            {
                var rect = child as RectTransform;
                if (rect == null) continue;
                if (rect.anchorMin.y < 0.01f && rect.anchorMax.y < 0.01f) continue;

                if (chrome.Contains(rect))
                {
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + rowSpacing);
                    continue;   // bleibt zentriert, wächst nach unten mit dem Panel
                }

                float shift = rect.anchoredPosition.y < shakeY - 1f ? dropBelow : liftAbove;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + shift);
            }
        }

        private bool IsShakeRow(RectTransform rect) =>
            (shakeSlider != null && rect == shakeSlider.transform) ||
            (shakePercent != null && rect == shakePercent.transform) ||
            rect.name == "ShakeLabel";

        private void OnShakeChanged(float value)
        {
            ScreenShake.SetStrength(value);
            UpdateShakeLabel(value);
            ScreenShake.Impact();   // sofort spürbare Vorschau
        }

        private void UpdateShakeLabel(float value)
        {
            if (shakePercent == null) return;
            shakePercent.text = value <= 0.001f ? "OFF" : $"{Mathf.RoundToInt(value / 1.5f * 100f)}%";
        }

        private static GameObject CopyToRow(GameObject original, RectTransform parent, string name, float y)
        {
            var copy = Instantiate(original, parent);
            copy.name = name;
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
            return copy;
        }

        /// <summary>Die Beschriftung der Musik-Zeile: Text auf gleicher Höhe wie der Regler.</summary>
        private TMP_Text FindRowLabel(RectTransform parent, float rowY)
        {
            foreach (Transform child in parent)
            {
                if (child == musicSlider.transform || child == musicPercent.transform) continue;
                var text = child.GetComponent<TMP_Text>();
                if (text == null) continue;
                if (Mathf.Abs(((RectTransform)child).anchoredPosition.y - rowY) < 6f) return text;
            }
            return null;
        }

        /// <summary>Der Platzhalter-Hinweis: der breiteste Text unterhalb der Musik-Zeile.</summary>
        private static TMP_Text FindHintBelow(RectTransform parent, float rowY)
        {
            TMP_Text widest = null;
            foreach (Transform child in parent)
            {
                var text = child.GetComponent<TMP_Text>();
                if (text == null) continue;
                var rect = (RectTransform)child;
                if (rect.anchoredPosition.y >= rowY - 6f) continue;
                if (widest == null || rect.sizeDelta.x > ((RectTransform)widest.transform).sizeDelta.x) widest = text;
            }
            return widest;
        }

        private void OnSfxChanged(float value)
        {
            if (SfxManager.Instance != null) SfxManager.Instance.SetSfxVolume(value);
            if (sfxPercent != null) sfxPercent.text = $"{Mathf.RoundToInt(value * 100f)}%";
            SfxManager.Hover();   // sofort hörbare Vorschau der neuen Lautstärke
        }

        public void Close()
        {
            PlayerPrefs.Save();
            StartAnim(false);
        }

        private void OnMusicChanged(float value)
        {
            if (MusicManager.Instance != null) MusicManager.Instance.SetMusicVolume(value);
            UpdatePercent(value);
        }

        private void UpdatePercent(float value)
        {
            if (musicPercent != null) musicPercent.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private void StartAnim(bool show)
        {
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Animate(show));
        }

        private IEnumerator Animate(bool show)
        {
            float from = overlayGroup != null ? overlayGroup.alpha : (show ? 0f : 1f);
            float to = show ? 1f : 0f;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeDuration;
                if (overlayGroup != null) overlayGroup.alpha = Mathf.Lerp(from, to, k);
                if (panel != null)
                {
                    float s = show ? Mathf.Lerp(0.96f, 1f, k) : Mathf.Lerp(1f, 0.97f, k);
                    panel.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
            if (overlayGroup != null) overlayGroup.alpha = to;
            if (panel != null) panel.localScale = Vector3.one;
            if (!show && overlay != null) overlay.SetActive(false);
            animRoutine = null;
        }
    }
}
