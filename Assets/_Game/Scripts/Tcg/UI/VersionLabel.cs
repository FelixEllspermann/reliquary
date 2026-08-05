using TMPro;
using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Schreibt die Build-Nummer in das Textfeld, an dem dieses Skript hängt.
    ///
    /// Vorher stand die Zahl als Text in der Szene — und blieb dort stehen,
    /// während das Spiel weiterzog. Der Shop zeigte „BUILD 1.0", längst nachdem
    /// der Build 0.1.0h hiess. Eine Versionsangabe, die man von Hand pflegen
    /// muss, ist keine: sie lügt beim ersten Mal, an das niemand denkt.
    ///
    /// <see cref="Application.version"/> kommt aus den Player Settings, also aus
    /// derselben Zahl, mit der auch gebaut wird.
    /// </summary>
    [DisallowMultipleComponent]
    public class VersionLabel : MonoBehaviour
    {
        [Tooltip("Leer lassen — dann nimmt das Skript das Textfeld an diesem Objekt.")]
        [SerializeField] private TMP_Text target;

        private void Awake() => Apply();

        // Im Editor sofort sichtbar, ohne das Spiel zu starten
        private void OnValidate() => Apply();

        private void Apply()
        {
            if (target == null) target = GetComponent<TMP_Text>();
            if (target == null) target = GetComponentInChildren<TMP_Text>(true);
            if (target != null) target.text = $"RELIQUARY · BUILD {Application.version}";
        }
    }
}
