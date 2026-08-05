using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>An diesem Objekt hängt der UiFxInstaller kein Feedback an.</summary>
    public class UiFxIgnore : MonoBehaviour { }

    /// <summary>
    /// Hängt <see cref="UiButtonFx"/> automatisch an jeden Button im Spiel — in allen
    /// Szenen und auch an UI, die erst zur Laufzeit entsteht (Deck-Zeilen, Lobby-Listen,
    /// Dropdown-Einträge). Unity führt selbst Buch über alle aktiven Selectables; sobald
    /// sich deren Anzahl ändert, läuft ein Durchgang. Das kostet im Normalfall nichts
    /// weiter als einen Zahlenvergleich pro Frame.
    /// </summary>
    public class UiFxInstaller : MonoBehaviour
    {
        private static UiFxInstaller instance;

        private Selectable[] buffer = new Selectable[128];
        private int lastCount = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("~UiFx");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<UiFxInstaller>();
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => lastCount = -1;

        private void LateUpdate()
        {
            int count = Selectable.allSelectableCount;
            if (count == lastCount) return;
            lastCount = count;
            InstallAll(count);
        }

        private void InstallAll(int count)
        {
            if (count <= 0) return;
            if (buffer.Length < count) buffer = new Selectable[Mathf.NextPowerOfTwo(count)];
            Selectable.AllSelectablesNoAlloc(buffer);

            for (int i = 0; i < count; i++)
            {
                var selectable = buffer[i];
                if (selectable == null) continue;
                var existing = selectable.GetComponent<UiButtonFx>();
                if (!ShouldDecorate(selectable))
                {
                    // Auch nachträglich entfernen — in Szenen, die es schon gespeichert haben
                    if (existing != null) Destroy(existing);
                    continue;
                }
                if (existing != null) continue;
                selectable.gameObject.AddComponent<UiButtonFx>();
            }
        }

        /// <summary>
        /// Schieberegler und Eingabefelder bleiben außen vor — sie sollen beim Tippen
        /// bzw. Ziehen ruhig bleiben. Menü-Kacheln bringen ihre eigene Animation mit.
        /// </summary>
        private static bool ShouldDecorate(Selectable selectable)
        {
            if (selectable is Scrollbar || selectable is Slider) return false;
            if (selectable is TMP_InputField || selectable is InputField) return false;
            if (selectable.GetComponent<UiFxIgnore>() != null) return false;
            if (selectable.GetComponent<MenuTileHover>() != null) return false;
            if (IsScrim(selectable)) return false;
            return true;
        }

        /// <summary>
        /// Ein Scrim ist der klickbare Hintergrund hinter einem Overlay. Er ist zwar ein
        /// Button, aber kein Knopf im Wortsinn: bekäme er das Hover-Feedback, würde der
        /// ganze Bildschirm aufleuchten und bei jeder Mausbewegung klingeln.
        /// </summary>
        private static bool IsScrim(Selectable selectable)
        {
            string name = selectable.gameObject.name;
            if (name.IndexOf("Scrim", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Backdrop", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // Sicherheitsnetz für unbenannte Fälle: fast bildschirmfüllende Knöpfe
            var rect = selectable.transform as RectTransform;
            var canvas = selectable.GetComponentInParent<Canvas>();
            if (rect == null || canvas == null) return false;
            var canvasRect = canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : canvas.transform as RectTransform;
            if (canvasRect == null) return false;

            return rect.rect.width >= canvasRect.rect.width * 0.85f
                && rect.rect.height >= canvasRect.rect.height * 0.85f;
        }
    }
}
