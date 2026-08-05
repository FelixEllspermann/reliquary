using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>Kompletter Zustand eines Spielers im Duell (Zonen, LP, Mana).</summary>
    public class PlayerState
    {
        public string Name = "Player";
        public DeckDefinition DeckSource;
        public PlayerState Opponent;
        public DuelController Controller;

        /// <summary>
        /// Sitzt dieser Spieler an diesem Client? Wird von
        /// <c>DuelManager.LocalPlayer</c> gepflegt. Die Darstellung braucht das,
        /// um zu wissen, wessen Kartenrücken eine Karte bekommt — die Engine
        /// selbst schaut nie darauf.
        /// </summary>
        public bool IsLocal;

        public int LifePoints;
        public int Mana;
        public int ManaPerTurn;
        public int BonusManaPerTurn;   // z.B. Solo-Schwierigkeit "Sealed": Bot +2
        public int NormalSummonsUsed;
        public int TurnsTaken;

        // --- Zug-Zustände, in ClearTempModifiers() zurückgesetzt ---
        public int ExtraNormalSummons;          // zusätzlich erlaubte Normalbeschwörungen
        public bool NoBattleDamageThisTurn;     // erleidet diesen Zug keinen Kampfschaden
        public bool CannotSpecialSummonThisTurn;

        public readonly List<CardInstance> DeckPile = new List<CardInstance>();
        public readonly List<CardInstance> ExtraDeckPile = new List<CardInstance>();
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly List<CardInstance> Graveyard = new List<CardInstance>();
        public readonly List<CardInstance> Banished = new List<CardInstance>();
        public readonly CardInstance[] MonsterZones = new CardInstance[5];
        public readonly CardInstance[] SpellZones = new CardInstance[3];
        public readonly CardInstance[] ArtifactZones = new CardInstance[2];
        public CardInstance PlayerCard;

        public IEnumerable<CardInstance> Monsters()
        {
            foreach (var monster in MonsterZones) if (monster != null) yield return monster;
        }

        public IEnumerable<CardInstance> SpellsOnField()
        {
            foreach (var spell in SpellZones) if (spell != null) yield return spell;
        }

        public IEnumerable<CardInstance> ArtifactsOnField()
        {
            foreach (var artifact in ArtifactZones) if (artifact != null) yield return artifact;
        }

        /// <summary>Alle offenen Karten des Spielers auf dem Feld (inkl. Spielerkarte).</summary>
        public IEnumerable<CardInstance> FieldCards()
        {
            foreach (var monster in Monsters()) yield return monster;
            foreach (var spell in SpellsOnField()) yield return spell;
            foreach (var artifact in ArtifactsOnField()) yield return artifact;
            if (PlayerCard != null) yield return PlayerCard;
        }

        public int MonsterCount()
        {
            int count = 0;
            foreach (var monster in MonsterZones) if (monster != null) count++;
            return count;
        }

        public int FreeMonsterZones()
        {
            int count = 0;
            foreach (var monster in MonsterZones) if (monster == null) count++;
            return count;
        }

        public int FirstFreeZoneIndex(CardInstance[] zones)
        {
            for (int i = 0; i < zones.Length; i++) if (zones[i] == null) return i;
            return -1;
        }
    }
}
