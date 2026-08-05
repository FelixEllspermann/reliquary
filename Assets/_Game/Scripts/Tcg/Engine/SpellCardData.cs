using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "NeuerZauber", menuName = "Rouge TCG/Zauberkarte")]
    public class SpellCardData : CardDefinition
    {
        [Header("Zauber")]
        [Tooltip("Normal: nur in der eigenen Main Phase. Quick: auch als Reaktion im Gegnerzug (wenn vorher gesetzt).")]
        public SpellSpeed speed = SpellSpeed.Normal;

        public override CardKind Kind => CardKind.Spell;

        public override Color FrameColor => speed == SpellSpeed.Quick
            ? new Color(0.25f, 0.65f, 0.70f)
            : new Color(0.25f, 0.60f, 0.40f);
    }
}
