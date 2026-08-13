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

        /// <summary>
        /// Mana, das beim NÄCHSTEN Auffüllen fehlt bzw. dazukommt, danach wieder 0.
        ///
        /// Ohne diese zwei Zahlen kann es kein Mana-Denial geben: zu Beginn jedes
        /// Zuges wird der Vorrat komplett neu gesetzt. Wer dem Gegner in seinem
        /// eigenen Zug Mana abzieht, nimmt ihm nichts — er füllt ohnehin gleich
        /// wieder auf. Und wer im Gegnerzug Mana gewinnt, verliert es beim eigenen
        /// Zugbeginn wieder. Erst der Übertrag macht beides spürbar.
        /// </summary>
        public int ManaDebt;
        public int ManaCredit;
        public int NormalSummonsUsed;
        public int TurnsTaken;

        // --- Zug-Zustände, in ClearTempModifiers() zurückgesetzt ---
        public int ExtraNormalSummons;          // zusätzlich erlaubte Normalbeschwörungen
        public bool NoBattleDamageThisTurn;     // erleidet diesen Zug keinen Kampfschaden
        public bool CannotSpecialSummonThisTurn;
        public bool NoDirectAttacksThisTurn;    // darf diesen Zug nicht direkt angreifen (Implosion)
        public bool SpecialSummonedEffectsLockedThisTurn; // kann diesen Zug keine Effekte spezialbeschworener Feldmonster aktivieren

        // The Forbidden Name (Infused): die NÄCHSTE eigene Battle Phase entfällt.
        // Gemerkt wird der Aktivierungszug — verbraucht wird in einem späteren
        // Zug, damit eine Aktivierung in der eigenen Main Phase nicht schon die
        // eigene Battle Phase desselben Zuges frisst. -1 = keine Schuld offen.
        public int SkipBattlePhaseAfterTurn = -1;

        /// <summary>Ausgespielte Zauber in diesem Zug — für den Erster-Zauber-Rabatt
        /// (Bargain Bobbin). Reset in ResetTurnFlags(), für beide Spieler.</summary>
        public int SpellsCastThisTurn;

        // Deckay: hat dieser Spieler in diesem/dem vorherigen Zug-Zyklus gemillt?
        // "ThisTurn" zählt ab dem eigenen Zugbeginn (auch Mills im Gegnerzug);
        // beim nächsten eigenen Zugbeginn rutscht der Wert nach "LastTurn".
        public bool MilledThisTurn;
        public bool MilledLastTurn;

        // Tidebound Leviathan: Selbst-Spezialbeschwörungen mit "einmal pro Zug"
        // werden je KARTENNAME gemerkt (zwei Exemplare teilen sich das Limit).
        public readonly System.Collections.Generic.HashSet<string> SelfSummonedNamesThisTurn
            = new System.Collections.Generic.HashSet<string>();

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
