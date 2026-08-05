using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "NeuesArtefakt", menuName = "Rouge TCG/Artefaktkarte")]
    public class ArtifactCardData : CardDefinition
    {
        [Header("Artefakt")]
        [Tooltip("Wofür ist diese Ausrüstung: ein Monster, der Spieler selbst oder das Feld")]
        public ArtifactSlot slot = ArtifactSlot.Monster;

        [Tooltip("ATK-Bonus für das ausgerüstete Monster (nur bei Slot = Monster)")]
        public int atkBonus;

        [Tooltip("DEF-Bonus für das ausgerüstete Monster (nur bei Slot = Monster)")]
        public int defBonus;

        [Header("Feld-Aura (nur bei Slot = Field)")]
        [Tooltip("Schützt eigene Monster des gewählten Typs vor Zerstörung durch gegnerische Karteneffekte")]
        public bool protectTypeFromEffectDestruction;

        [Tooltip("Geschützter Monster-Typ")]
        public MonsterType protectedType = MonsterType.Dragon;

        [Header("Schutzschild (Barrierstruck)")]
        [Tooltip("Würde eine eigene Karte zerstört, kann stattdessen dieses Artefakt zerstört werden")]
        public bool redirectDestructionToSelf;

        public override CardKind Kind => CardKind.Artifact;

        public override Color FrameColor => new Color(0.55f, 0.40f, 0.70f);
    }
}
