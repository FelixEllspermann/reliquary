using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>Eine Dialogzeile im Turm: wer spricht, was gesagt wird.</summary>
    [System.Serializable]
    public class TowerLine
    {
        [Tooltip("Sprechername in Grossbuchstaben (z.B. GATEKEEPER, YOU)")]
        public string speaker = "";
        [TextArea(2, 4)] public string text = "";
    }

    /// <summary>Der ganze Turm: die Ebenen in Reihenfolge von unten nach oben.</summary>
    [CreateAssetMenu(fileName = "Tower", menuName = "Rouge TCG/Tower")]
    public class TowerDefinition : ScriptableObject
    {
        [Tooltip("Ebenen von unten (Index 0 = Ebene 1) nach oben")]
        public List<TowerFloorDefinition> floors = new List<TowerFloorDefinition>();
    }
}
