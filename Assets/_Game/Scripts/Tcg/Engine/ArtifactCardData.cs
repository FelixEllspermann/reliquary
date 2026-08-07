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

        [Header("Batch August 2026")]
        [Tooltip("Zählt auf dem Feld als Monster mit diesem Namen für Zähl-Bedingungen (Dragon Shrine Stand-In)")]
        public string countsAsNameOnField = "";

        [Tooltip(">0: der erste Zauber des Besitzers pro Zug kostet so viel weniger Mana (Bargain Bobbin)")]
        public int firstSpellDiscountPerTurn;

        [Tooltip("Eigene verdeckte Monster sind kein Angriffsziel, solange ein offenes eigenes Monster " +
                 "mit diesem Namensteil liegt (Lyria Green Room; leer = aus)")]
        public string protectsFaceDownWhileNamedFaceUp = "";

        public override CardKind Kind => CardKind.Artifact;

        public override System.Collections.Generic.List<string> BuildPassiveLines()
        {
            var lines = base.BuildPassiveLines();
            if (protectTypeFromEffectDestruction)
                lines.Add($"{protectedType}-Type monsters you control cannot be destroyed by your opponent's card effects.");
            if (redirectDestructionToSelf)
                lines.Add("If a card you control would be destroyed, you can destroy this card instead.");
            if (!string.IsNullOrEmpty(countsAsNameOnField))
                lines.Add($"While on the field, this card counts as a monster named \"{countsAsNameOnField}\".");
            if (firstSpellDiscountPerTurn > 0)
                lines.Add($"The first Spell you play each turn costs {firstSpellDiscountPerTurn} less Mana.");
            if (!string.IsNullOrEmpty(protectsFaceDownWhileNamedFaceUp))
                lines.Add($"While you control a face-up \"{protectsFaceDownWhileNamedFaceUp}\" monster, your face-down monsters cannot be attacked.");
            return lines;
        }

        public override Color FrameColor => new Color(0.55f, 0.40f, 0.70f);
    }
}
