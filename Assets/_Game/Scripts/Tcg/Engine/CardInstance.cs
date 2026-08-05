using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>Laufzeit-Instanz einer Karte innerhalb eines Duells.</summary>
    public class CardInstance
    {
        public CardDefinition Definition;
        public PlayerState Owner;
        public ZoneType Zone = ZoneType.Deck;
        public bool FaceDown;                       // gesetzte Zauber liegen verdeckt
        public BattlePosition Position = BattlePosition.Attack;
        public bool SetThisTurn;                    // in diesem Zug gesetzt (noch nicht aktivierbar)
        public bool SummonedThisTurn;
        public bool WasSpecialSummoned;             // solange auf dem Feld: kam per Spezialbeschwörung
        public bool HasAttackedThisTurn;
        public int BonusAttacks;   // zusätzliche Angriffe in dieser Battle Phase (z.B. Dragon Tail)
        public int PositionChangesUsed;
        public int PermanentAtkBonus;
        public int PermanentDefBonus;
        public int TempAtkBonus;                    // bis zum Ende des laufenden Zuges
        public int TempDefBonus;                    // bis zum Ende des laufenden Zuges
        public bool EffectsNegated;                 // bis Zugende annulliert (Heavenly Seraph Sovereign)
        public bool CannotBeDestroyedThisTurn;      // Zerstörungs-Immunität bis Zugende

        // --- Zug-Zustände, alle in ClearTurnFlags() zurückgesetzt ---
        public bool CannotAttackThisTurn;           // darf diesen Zug nicht angreifen
        public bool PositionLockedThisTurn;         // darf diesen Zug die Position nicht wechseln
        public bool CannotBeTargetedThisTurn;       // kein gültiges Ziel für gegnerische Effekte
        public bool MustBeAttackedThisTurn;         // Spott: Angriffe müssen hierhin
        public bool StatsSwappedThisTurn;           // ATK und DEF sind vertauscht
        public bool StatsOverriddenThisTurn;        // ATK/DEF von einer anderen Karte kopiert
        public int OverriddenAtk;
        public int OverriddenDef;

        /// <summary>Bei Kontrollübernahme: Spieler, an den die Karte in der End Phase zurückgeht.</summary>
        public PlayerState ControlReturnsTo;

        /// <summary>Laufzeit-Kopie (The Mirror Hour): verschwindet in der End Phase.</summary>
        public bool IsTemporaryCopy;

        public CardInstance EquipTarget;            // Artefakt: das ausgerüstete Monster
        public readonly List<CardInstance> EquippedArtifacts = new List<CardInstance>();
        public readonly HashSet<int> OncePerTurnUsed = new HashSet<int>();

        public MonsterCardData MonsterData => Definition as MonsterCardData;
        public SpellCardData SpellData => Definition as SpellCardData;
        public ArtifactCardData ArtifactData => Definition as ArtifactCardData;
        public string Name => Definition != null ? Definition.cardName : "?";

        /// <summary>Grundwert vor Boni — bei kopierten oder vertauschten Werten die Ersatzzahl.</summary>
        private int BaseAtk => StatsOverriddenThisTurn ? OverriddenAtk
            : StatsSwappedThisTurn ? (MonsterData != null ? MonsterData.def : 0)
            : (MonsterData != null ? MonsterData.atk : 0);

        private int BaseDef => StatsOverriddenThisTurn ? OverriddenDef
            : StatsSwappedThisTurn ? (MonsterData != null ? MonsterData.atk : 0)
            : (MonsterData != null ? MonsterData.def : 0);

        public int CurrentAtk
        {
            get
            {
                int value = BaseAtk + PermanentAtkBonus + TempAtkBonus;
                foreach (var artifact in EquippedArtifacts)
                    if (artifact.ArtifactData != null) value += artifact.ArtifactData.atkBonus;
                return value < 0 ? 0 : value;
            }
        }

        public int CurrentDef
        {
            get
            {
                int value = BaseDef + PermanentDefBonus + TempDefBonus;
                foreach (var artifact in EquippedArtifacts)
                    if (artifact.ArtifactData != null) value += artifact.ArtifactData.defBonus;
                return value < 0 ? 0 : value;
            }
        }

        /// <summary>Der ursprüngliche Besitzer — dahin kehrt die Karte zurück (Friedhof/Hand/Verbannung), auch wenn sie gerade kontrolliert wird.</summary>
        public PlayerState OriginalOwner { get; private set; }

        public CardInstance(CardDefinition definition, PlayerState owner)
        {
            Definition = definition;
            Owner = owner;
            OriginalOwner = owner;
        }
    }
}
