using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "NeuerZauber", menuName = "Rouge TCG/Zauberkarte")]
    public class SpellCardData : CardDefinition
    {
        [Header("Zauber")]
        [Tooltip("Normal: nur in der eigenen Main Phase. Quick: auch als Reaktion im Gegnerzug (wenn vorher gesetzt).")]
        public SpellSpeed speed = SpellSpeed.Normal;

        [Header("Rite (Incarnates, September 2026)")]
        [Tooltip("RITE: diese Zauber-Unterart opfert das benannte Monster und beschwört das " +
                 "benannte Incarnate PERMANENT (keine Standby-Rückkehr). Aktivierbar nur, wenn " +
                 "das Monster auf dem eigenen Feld liegt und das Incarnate im Extra Deck wartet.")]
        public bool isRite;

        [Tooltip("Name des Monsters, das die Rite opfert (Namens-Enthält-Abgleich)")]
        public string riteSacrificeName = "";

        [Tooltip("Name des Incarnates, das die Rite beschwört")]
        public string riteIncarnateName = "";

        public override CardKind Kind => CardKind.Spell;

        public override Color FrameColor => isRite
            ? new Color(0.55f, 0.14f, 0.22f)   // Riten tragen das Bordeaux der Incarnates
            : speed == SpellSpeed.Quick
                ? new Color(0.25f, 0.65f, 0.70f)
                : new Color(0.25f, 0.60f, 0.40f);
    }
}
