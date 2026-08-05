using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "NeuesMonster", menuName = "Rouge TCG/Monsterkarte")]
    public class MonsterCardData : CardDefinition
    {
        [Header("Monster")]
        [Range(1, 3)]
        [Tooltip("Level bestimmt die Beschwörungskosten: Level 1 = frei, Level 2 = 1 Tribut, Level 3 = 2 Tribute")]
        public int level = 1;

        [Tooltip("Element-Attribut des Monsters")]
        public MonsterAttribute attribute = MonsterAttribute.Earth;

        [Tooltip("Typ/Rasse des Monsters")]
        public MonsterType monsterType = MonsterType.Beast;

        [Tooltip("Angriffspunkte")]
        public int atk = 1000;

        [Tooltip("Verteidigungspunkte")]
        public int def = 1000;

        [Header("Selbst-Spezialbeschwörung (optional)")]
        [Tooltip("Darf sich diese Karte unter einer Bedingung selbst aus der Hand spezialbeschwören?")]
        public bool canSelfSpecialSummon;

        [Tooltip("Bedingung: Feld braucht ein Monster, dessen Name diesen Text enthält (leer = keine Namensbedingung)")]
        public string selfSummonRequiresNameOnField = "";

        [Tooltip("Bedingung prüft das GEGNERISCHE Feld statt des eigenen")]
        public bool selfSummonChecksOpponentField;

        [Tooltip("Bedingung: Es muss eine verdeckte Karte auf einem der Felder liegen")]
        public bool selfSummonRequiresFaceDownOnField;

        [Tooltip("Bedingung: Du musst ein Artefakt kontrollieren")]
        public bool selfSummonRequiresArtifact;

        [Range(1, 5)]
        [Tooltip("Wie viele Monster die Namensbedingung erfüllen müssen (1 = Standard)")]
        public int selfSummonRequiredNameCount = 1;

        [Range(0, 5)]
        [Tooltip("Bedingung: Der Gegner muss mindestens so viele Monster kontrollieren (0 = egal)")]
        public int selfSummonRequiresOpponentMonsters;

        [Tooltip("Bedingung: Feld braucht ein Monster mit diesem Attribut?")]
        public bool selfSummonRequiresAttribute;

        [Tooltip("Gefordertes Attribut (wenn aktiviert)")]
        public MonsterAttribute selfSummonRequiredAttribute = MonsterAttribute.Light;

        [Tooltip("Kampfposition der Selbst-Spezialbeschwörung")]
        public BattlePosition selfSummonPosition = BattlePosition.Defense;

        public override CardKind Kind => CardKind.Monster;

        public override Color FrameColor => new Color(0.80f, 0.55f, 0.25f);

        /// <summary>
        /// Lesbarer Beschwörungs-Bedingungstext für die Karte (aus den selfSummon-Feldern generiert).
        /// Leer, wenn die Karte keine Selbst-Spezialbeschwörung hat.
        /// </summary>
        public string SelfSummonConditionText()
        {
            if (!canSelfSpecialSummon) return "";
            var parts = new System.Collections.Generic.List<string>();
            string side = selfSummonChecksOpponentField ? "your opponent controls" : "you control";
            if (!string.IsNullOrEmpty(selfSummonRequiresNameOnField))
            {
                int needed = Mathf.Max(1, selfSummonRequiredNameCount);
                parts.Add(needed > 1
                    ? $"{side} {needed}+ \"{selfSummonRequiresNameOnField}\" monsters"
                    : $"{side} a \"{selfSummonRequiresNameOnField}\" monster");
            }
            if (selfSummonRequiresAttribute)
                parts.Add($"{side} a {selfSummonRequiredAttribute.ToString().ToUpperInvariant()} monster");
            if (selfSummonRequiresFaceDownOnField)
                parts.Add("there is a face-down monster on the field");
            if (selfSummonRequiresArtifact)
                parts.Add("you control an Artifact");
            if (selfSummonRequiresOpponentMonsters > 0)
                parts.Add($"your opponent controls {selfSummonRequiresOpponentMonsters}+ monsters");

            string position = selfSummonPosition == BattlePosition.Defense ? " in Defense Position" : "";
            if (parts.Count == 0)
                return $"You can Special Summon this card from your hand{position}.";
            return $"While {string.Join(" and ", parts)}: You can Special Summon this card from your hand{position}.";
        }

        /// <summary>Anzeige-Farbe des Attributs — Pip-Farben aus dem Reliquary-Design-Handoff.</summary>
        public static Color AttributeColor(MonsterAttribute attribute)
        {
            switch (attribute)
            {
                case MonsterAttribute.Fire: return new Color32(0xE0, 0x60, 0x3A, 0xFF);
                case MonsterAttribute.Water: return new Color32(0x4B, 0x92, 0xD6, 0xFF);
                case MonsterAttribute.Light: return new Color32(0xE8, 0xD0, 0x8A, 0xFF);
                case MonsterAttribute.Dark: return new Color32(0x8B, 0x6B, 0xC4, 0xFF);
                case MonsterAttribute.Earth: return new Color32(0xA8, 0x89, 0x4F, 0xFF);
                default: return new Color32(0x6F, 0xBF, 0x9A, 0xFF); // Wind
            }
        }

        /// <summary>"Fire Beast"-Kurzform mit eingefärbtem Attribut (Rich Text).</summary>
        public string AttributeTypeRichText()
        {
            string hex = ColorUtility.ToHtmlStringRGB(AttributeColor(attribute));
            return $"<color=#{hex}>{attribute}</color> {monsterType}";
        }
    }
}
