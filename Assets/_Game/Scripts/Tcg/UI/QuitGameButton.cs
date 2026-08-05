using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Beendet das Spiel (Hauptmenü-Leiste und Login-Screen). Im Editor wird
    /// stattdessen der Play Mode gestoppt, damit sich der Knopf testen lässt.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuitGameButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Quit);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
