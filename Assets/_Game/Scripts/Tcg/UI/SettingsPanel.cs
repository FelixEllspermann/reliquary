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
        private TMP_Text languageValue; // klickbarer Sprachwechsler (vierte Zeile)
        private string pendingLanguage; // Auswahl in der Zeile — wirkt erst mit ANWENDEN
        private Button applyButton;
        private CanvasGroup applyGroup;
        private float rowSpacing = 52f;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (doneButton != null) doneButton.onClick.AddListener(Close);
            if (scrimButton != null) scrimButton.onClick.AddListener(Close);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            BuildSfxRow();
            BuildShakeRow();
            BuildLanguageRow();
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

            pendingLanguage = Loc.Language;
            UpdateLanguageValue();
            RefreshApplyState();
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
                if (sfxLabel != null) sfxLabel.text = Loc.T("SOUND");
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
                if (labelCopy != null) labelCopy.text = Loc.T("SCREEN SHAKE");
            }

            MakeRoomForRow(targetY);

            if (shakeSlider == null) return;
            shakeSlider.minValue = 0f;
            shakeSlider.maxValue = 1.5f;
            shakeSlider.onValueChanged.RemoveAllListeners();
            shakeSlider.onValueChanged.AddListener(OnShakeChanged);
        }

        /// <summary>
        /// Vierte Zeile: die Sprache. Beschriftung links wie bei den Reglern, rechts
        /// ein Wert mit ‹/›-Pfeilen. Blättern ändert nur die AUSWAHL — gewechselt
        /// wird erst mit ANWENDEN (Knopf neben FERTIG), das lädt die Szene neu und
        /// schließt damit auch das Menü.
        /// </summary>
        private void BuildLanguageRow()
        {
            if (shakeSlider == null || shakePercent == null || sfxLabel == null) return;
            var parent = shakeSlider.transform.parent as RectTransform;
            if (parent == null) return;

            float shakeY = ((RectTransform)shakeSlider.transform).anchoredPosition.y;
            float targetY = shakeY - rowSpacing;

            var label = CopyToRow(sfxLabel.gameObject, parent, "LanguageLabel", targetY).GetComponent<TMP_Text>();
            if (label != null) label.text = Loc.T("LANGUAGE");

            languageValue = CopyToRow(shakePercent.gameObject, parent, "LanguageValue", targetY).GetComponent<TMP_Text>();
            if (languageValue != null)
            {
                // Breiter als die Prozentwerte, damit der Sprachname und der
                // Klickbereich Platz haben; die Schrift kommt über den TMP-Fallback
                // auch mit chinesischen Glyphen zurecht (LocBoot).
                var rect = (RectTransform)languageValue.transform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x + 150f, rect.sizeDelta.y + 10f);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x - 75f, rect.anchoredPosition.y);
                languageValue.alignment = TextAlignmentOptions.Center;
                pendingLanguage = Loc.Language;
                UpdateLanguageValue();
                MakeClickable(languageValue, () => CycleLanguage(+1));
                BuildArrow(rect, left: true);
                BuildArrow(rect, left: false);
            }

            BuildApplyButton();

            // Der Regler-Platz der Zeile bleibt leer — der Wert selbst ist der Knopf.
            MakeRoomForRow(targetY);
        }

        /// <summary>
        /// ANWENDEN neben FERTIG — ausgegraut, solange die Auswahl der aktiven
        /// Sprache entspricht. Der Klon übernimmt den FERTIG-Stil; seine
        /// Inspector-Listener werden durch ein frisches Event ersetzt.
        /// </summary>
        private void BuildApplyButton()
        {
            if (doneButton == null) return;
            var doneRect = (RectTransform)doneButton.transform;
            var go = Instantiate(doneButton.gameObject, doneRect.parent);
            go.name = "ApplyButton";

            applyButton = go.GetComponent<Button>();
            applyButton.onClick = new Button.ButtonClickedEvent();
            applyButton.onClick.AddListener(ApplyLanguage);
            applyGroup = go.AddComponent<CanvasGroup>();

            var applyRect = (RectTransform)go.transform;
            applyRect.sizeDelta = new Vector2(applyRect.sizeDelta.x + 36f, applyRect.sizeDelta.y);

            var labelText = go.GetComponentInChildren<TMP_Text>();
            if (labelText != null) labelText.text = Loc.T("APPLY");

            // FERTIG nach links, ANWENDEN rechts daneben — Kantenabstand = gap.
            const float gap = 12f;
            float doneW = doneRect.sizeDelta.x;
            float applyW = applyRect.sizeDelta.x;
            float baseX = doneRect.anchoredPosition.x;
            doneRect.anchoredPosition = new Vector2(baseX - (applyW + gap) * 0.5f, doneRect.anchoredPosition.y);
            applyRect.anchoredPosition = new Vector2(baseX + (doneW + gap) * 0.5f, applyRect.anchoredPosition.y);

            RefreshApplyState();
        }

        /// <summary>
        /// Macht ein TMP-Label per Maus klickbar. Die geklonten Prozent-Labels sind
        /// reine Anzeigen (raycastTarget aus) — ohne Raycast-Ziel und targetGraphic
        /// trifft der Mausklick den Button nie. Hover hellt den Text leicht auf.
        /// </summary>
        private static void MakeClickable(TMP_Text text, UnityEngine.Events.UnityAction onClick)
        {
            text.raycastTarget = true;
            var button = text.gameObject.AddComponent<Button>();
            button.targetGraphic = text;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.82f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(1f, 1f, 1f, 0.55f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
        }

        /// <summary>
        /// ‹/›-Knopf an der Kante des Sprachwerts — blättert rückwärts/vorwärts.
        /// Als Kind des Wert-Rects verankert, damit keine Pivot-Arithmetik nötig ist;
        /// der Kind-Raycast gewinnt gegen den Wert-Button darunter.
        /// </summary>
        private void BuildArrow(RectTransform valueRect, bool left)
        {
            var go = new GameObject(left ? "LanguagePrev" : "LanguageNext", typeof(RectTransform));
            go.transform.SetParent(valueRect, false);
            var arrow = go.AddComponent<TextMeshProUGUI>();
            arrow.font = languageValue.font;
            arrow.fontSharedMaterial = languageValue.fontSharedMaterial;
            arrow.fontSize = languageValue.fontSize * 1.25f;
            arrow.color = languageValue.color;
            arrow.text = left ? "‹" : "›";
            arrow.alignment = TextAlignmentOptions.Center;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(44f, valueRect.rect.height + 10f);
            rect.anchoredPosition = Vector2.zero;

            MakeClickable(arrow, () => CycleLanguage(left ? -1 : +1));
        }

        /// <summary>Reihenfolge des Sprachwechslers — neue Sprachen hier anhängen.</summary>
        private static readonly string[] LanguageCycle = { Loc.English, Loc.German, Loc.Russian, Loc.ChineseSimplified };

        private static string LanguageDisplayName(string language) => language switch
        {
            Loc.ChineseSimplified => "简体中文",
            Loc.German => "DEUTSCH",
            Loc.Russian => "РУССКИЙ",
            _ => "ENGLISH"
        };

        private void UpdateLanguageValue()
        {
            if (languageValue == null) return;
            string language = pendingLanguage ?? Loc.Language;
            // „简体中文“ braucht die Laufzeit-Schrift auch dann, wenn gerade eine
            // andere Sprache aktiv ist — sonst zeigt die Zeile nur Vierecke.
            if (language == Loc.ChineseSimplified) LocBoot.EnsureRuntimeFallback();
            languageValue.text = LanguageDisplayName(language);
        }

        /// <summary>Blättert nur die Auswahl — gewechselt wird erst mit ANWENDEN.</summary>
        private void CycleLanguage(int direction)
        {
            SfxManager.Click();
            int index = System.Array.IndexOf(LanguageCycle, pendingLanguage ?? Loc.Language);
            pendingLanguage = LanguageCycle[(index + direction + LanguageCycle.Length) % LanguageCycle.Length];
            UpdateLanguageValue();
            RefreshApplyState();
        }

        private void RefreshApplyState()
        {
            bool dirty = pendingLanguage != null && pendingLanguage != Loc.Language;
            if (applyButton != null) applyButton.interactable = dirty;
            if (applyGroup != null) applyGroup.alpha = dirty ? 1f : 0.45f;
        }

        private void ApplyLanguage()
        {
            if (pendingLanguage == null || pendingLanguage == Loc.Language) return;
            SfxManager.Click();
            // Wechsel + Neuladen der Szene: alle Menüs bauen sich in der neuen
            // Sprache auf, das Overlay ist damit zu. Erst hier wird die Wahl
            // gespeichert — blättern allein ändert nichts.
            LocBoot.Switch(pendingLanguage);
        }

        /// <summary>
        /// Das Panel war für zwei Zeilen gebaut — jede weitere braucht Platz, sonst liegt
        /// der DONE-Knopf auf dem Regler. Panel wächst nach unten, alles unterhalb der
        /// neuen Zeile rutscht mit.
        /// </summary>
        private void MakeRoomForRow(float rowY)
        {
            if (panel == null) return;
            float shakeY = rowY;
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
            shakePercent.text = value <= 0.001f ? Loc.T("OFF") : $"{Mathf.RoundToInt(value / 1.5f * 100f)}%";
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
