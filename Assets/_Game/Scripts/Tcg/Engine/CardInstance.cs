using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>Laufzeit-Instanz einer Karte innerhalb eines Duells.</summary>
    public class CardInstance
    {
        public CardDefinition Definition;
        public PlayerState Owner;
        public ZoneType Zone = ZoneType.Deck;

        /// <summary>
        /// Welche Ausführung dieses Exemplars auf dem Tisch liegt. Regeltechnisch
        /// bedeutungslos — die Engine trägt sie nur mit, damit der Client die
        /// glänzende Kopie auch als glänzende Kopie zeichnet. Wer drei schlichte
        /// und eine Static im Deck hat, sieht genau eine leuchten.
        /// </summary>
        public Net.CardFinish Finish = Net.CardFinish.Plain;

        public bool FaceDown;                       // gesetzte Zauber liegen verdeckt
        public BattlePosition Position = BattlePosition.Attack;
        public bool SetThisTurn;                    // in diesem Zug gesetzt (noch nicht aktivierbar)
        public bool SummonedThisTurn;
        public bool WasSpecialSummoned;             // solange auf dem Feld: kam per Spezialbeschwörung
        public int DeathCounters;                   // Immortal Demon: End-Phase-Zähler bis zum Grab
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
        public bool ImmuneToOpponentThisTurn;       // gegnerische Effekt-Aktionen prallen bis Zugende ab (Deckay)
        public bool TempReliquaryUntilEndPhase;     // Vulture-Konter-Summon: stirbt in der End Phase des Besitzers
        public bool MustBeAttackedThisTurn;         // Spott: Angriffe müssen hierhin
        public bool StatsSwappedThisTurn;           // ATK und DEF sind vertauscht
        public bool StatsOverriddenThisTurn;        // ATK/DEF von einer anderen Karte kopiert
        public int OverriddenAtk;
        public int OverriddenDef;

        /// <summary>Bei Kontrollübernahme: Spieler, an den die Karte in der End Phase zurückgeht.</summary>
        public PlayerState ControlReturnsTo;

        // --- The Small Print ---
        /// <summary>Pfandrecht: in jeder Standby Phase des Kontrolleurs zahlt er so viel Mana oder die Karte wird zerstört (0 = keins).</summary>
        public int LienAmount;
        /// <summary>Gewildert (Poacher's Lantern): verlässt die Karte das Feld, wird sie verbannt statt ins Grab zu gehen.</summary>
        public bool BanishWhenLeavingField;
        /// <summary>Piercing bis Zugende (Trample the Line) — dauerhaft über passivePiercing/Ram's Head.</summary>
        public bool PiercingThisTurn;
        /// <summary>Skimmed Off the Top: der Mana-Gewinn dieses Kettenglieds geht an diesen Spieler.</summary>
        public PlayerState ManaRedirectedTo;
        /// <summary>Loaded Dice: der Re-Flip ist einmal pro Zug — auf der Karte gemerkt.</summary>
        public bool CoinChooseUsedThisTurn;

        /// <summary>Laufzeit-Kopie (The Mirror Hour): verschwindet in der End Phase.</summary>
        public bool IsTemporaryCopy;

        // --- Road to 1000 ---
        /// <summary>Dauerhafte Level-Änderung (The Promotion Board). Wirkt geklemmt auf 1..3.</summary>
        public int PermanentLevelBonus;
        /// <summary>Bis Zugende gilt DIESES Level (0 = aus; Demoted for Cause).</summary>
        public int TempLevelThisTurn;
        /// <summary>Greift diesen Zug mit DEF an (Lead With the Shield) — dauerhaft über passiveAttacksWithDef.</summary>
        public bool AttacksWithDefThisTurn;
        /// <summary>Countdown-Marker (The Appointed Hour): tickt in der Standby Phase des Kontrolleurs.</summary>
        public int CountdownMarkers;

        // --- 5 Archetypes ---
        /// <summary>Bylaw Loophole: dieses Dekret wirkt bis Zugende NICHT auf diesen Spieler.</summary>
        public PlayerState DecreeExemptFor;
        /// <summary>Giftwyrm: war die Karte beim Verlassen des Feldes unter Fremdkontrolle?
        /// (MoveToGraveyard setzt den Owner zurück — der Trigger braucht die Vorgeschichte.)</summary>
        public bool WasDisloyalWhenLeftField;

        // --- Welle 3: 50 Generics ---
        /// <summary>Stage Fright/Overextension: Position gesperrt, bis der KONTROLLEUR so viele eigene Züge begonnen hat.</summary>
        public int PositionLockTurns;
        /// <summary>Insurance Policy: so viele kommende Zerstörungen verpuffen (Einmal-Schilde).</summary>
        public int DestructionShields;
        /// <summary>Silver-Tongued Creditor: in der nächsten Standby des Kontrolleurs so viel Mana zahlen, sonst Friedhof (0 = keine Schuld).</summary>
        public int PendingManaDebt;
        /// <summary>Lowball Feint Infused: wechselt in der End Phase in Verteidigungsposition.</summary>
        public bool SwitchToDefenseAtEot;
        /// <summary>Straw Army Infused: Dauer-Spott — Angriffe müssen hierhin, solange die Karte offen liegt.</summary>
        public bool PersistentTaunt;
        /// <summary>Mirror Usher Infused: die kopierten Werte (StatsOverridden) halten bis zum Beginn des nächsten eigenen Zuges.</summary>
        public bool CopyStatsUntilOwnersNextTurn;
        /// <summary>Shield Wall Infused: DEF-Bonus bis zum Beginn des nächsten eigenen Zuges des Kontrolleurs.</summary>
        public int DefBuffUntilOwnersNextTurn;

        // --- Incarnates ---
        /// <summary>Temporäre Opfergabe-Beschwörung: Zugnummer der Beschwörung — in der Standby
        /// Phase des nächsten EIGENEN Zuges (TurnNumber größer) kehrt die Karte ins Extra Deck
        /// zurück. -1 = permanent (Rite oder kein Incarnate).</summary>
        public int IncarnateReturnTurn = -1;

        /// <summary>
        /// Das Level, mit dem diese Karte gerade spielt: temporäre Setzung schlägt
        /// den permanenten Bonus, beides klemmt auf 1..3. Alle Level-Prüfungen der
        /// Engine (Filter, Bedingungen, Anzeige) lesen HIER — nur die Beschwörungs-
        /// kosten aus der Hand lesen weiterhin das gedruckte Level.
        /// </summary>
        public int EffectiveLevel
        {
            get
            {
                if (MonsterData == null) return 0;
                if (TempLevelThisTurn > 0) return System.Math.Min(3, System.Math.Max(1, TempLevelThisTurn));
                int level = MonsterData.level + PermanentLevelBonus;
                return System.Math.Min(3, System.Math.Max(1, level));
            }
        }

        /// <summary>
        /// Im Server-Duell: die zuletzt vom DuelHost übertragene Status-Maske.
        /// Deckt Zustände ab, die der Spiegel lokal nicht ableiten kann (Stolen —
        /// der Mirror kennt keine Besitzwechsel-Historie). 0 in lokalen Duellen.
        /// </summary>
        public int MirroredStatusMask;

        public CardInstance EquipTarget;            // Artefakt: das ausgerüstete Monster
        public readonly List<CardInstance> EquippedArtifacts = new List<CardInstance>();
        public readonly HashSet<int> OncePerTurnUsed = new HashSet<int>();

        public MonsterCardData MonsterData => Definition as MonsterCardData;
        public SpellCardData SpellData => Definition as SpellCardData;
        public ArtifactCardData ArtifactData => Definition as ArtifactCardData;
        public string Name => Definition != null ? Definition.cardName : "?";

        /// <summary>
        /// Echo of the Latest Loss: die gedruckten Werte kommen live von der obersten
        /// MONSTERkarte des eigenen Friedhofs. Liegt dort keine, gelten die eigenen.
        /// </summary>
        private MonsterCardData StatSource
        {
            get
            {
                if (Definition == null || !Definition.passiveStatsFromGraveTop || Owner == null) return MonsterData;
                for (int i = Owner.Graveyard.Count - 1; i >= 0; i--)
                    if (Owner.Graveyard[i].MonsterData != null) return Owner.Graveyard[i].MonsterData;
                return MonsterData;
            }
        }

        /// <summary>Grundwert vor Boni — bei kopierten oder vertauschten Werten die Ersatzzahl.</summary>
        private int BaseAtk => StatsOverriddenThisTurn ? OverriddenAtk
            : StatsSwappedThisTurn ? (StatSource != null ? StatSource.def : 0)
            : (StatSource != null ? StatSource.atk : 0);

        private int BaseDef => StatsOverriddenThisTurn ? OverriddenDef
            : StatsSwappedThisTurn ? (StatSource != null ? StatSource.atk : 0)
            : (StatSource != null ? StatSource.def : 0);

        public int CurrentAtk
        {
            get
            {
                int value = BaseAtk + PermanentAtkBonus + TempAtkBonus + AuraBonus(atk: true) + PerCountBonus(atk: true);
                foreach (var artifact in EquippedArtifacts)
                    if (artifact.ArtifactData != null) value += artifact.ArtifactData.atkBonus;
                return value < 0 ? 0 : value;
            }
        }

        public int CurrentDef
        {
            get
            {
                int value = BaseDef + PermanentDefBonus + TempDefBonus + DefBuffUntilOwnersNextTurn
                    + AuraBonus(atk: false) + PerCountBonus(atk: false);
                foreach (var artifact in EquippedArtifacts)
                    if (artifact.ArtifactData != null) value += artifact.ArtifactData.defBonus;
                return value < 0 ? 0 : value;
            }
        }

        /// <summary>Zählt der Countdown OwnBanishedCards? Zentrale Zählbasis der Welle 3.</summary>
        public static int BanishedCount(PlayerState player) => player != null ? player.Banished.Count : 0;

        /// <summary>
        /// Selbst-Skalierung (Weight of Evidence): dauerhaft +N ATK/DEF pro gezählter
        /// Karte — wie die Aura ein Live-Scan statt Buchhaltung.
        /// </summary>
        private int PerCountBonus(bool atk)
        {
            if (Definition == null || Owner == null || Zone != ZoneType.MonsterZone) return 0;
            int per = atk ? Definition.passiveAtkPerCount : Definition.passiveDefPerCount;
            if (per <= 0) return 0;
            var kind = atk ? Definition.passiveAtkPerCountKind : Definition.passiveDefPerCountKind;
            if (kind == EffectCountKind.EquippedArtifactsOnSelf) return per * EquippedArtifacts.Count;
            return per * CountOn(Owner, kind);
        }

        /// <summary>Zählbasis der PerCount-Fähigkeiten — geteilt mit den ...PerCount-Aktionen der Engine.</summary>
        public static int CountOn(PlayerState player, EffectCountKind kind)
        {
            switch (kind)
            {
                case EffectCountKind.OwnArtifactsOnField:
                {
                    int artifacts = 0;
                    foreach (var a in player.ArtifactZones) if (a != null) artifacts++;
                    return artifacts;
                }
                case EffectCountKind.OwnGraveyardArtifacts:
                {
                    int graveArtifacts = 0;
                    foreach (var c in player.Graveyard) if (c.ArtifactData != null) graveArtifacts++;
                    return graveArtifacts;
                }
                case EffectCountKind.OwnFaceDownMonsters:
                {
                    int faceDown = 0;
                    foreach (var m in player.MonsterZones) if (m != null && m.FaceDown) faceDown++;
                    return faceDown;
                }
                case EffectCountKind.OwnBanishedMonsters:
                {
                    int banished = 0;
                    foreach (var c in player.Banished) if (c.MonsterData != null) banished++;
                    return banished;
                }
                case EffectCountKind.OwnGraveyardCards: return player.Graveyard.Count;
                case EffectCountKind.OwnMonstersOnField: return player.MonsterCount();
                case EffectCountKind.OpponentFaceDownMonsters:
                {
                    if (player.Opponent == null) return 0;
                    int foeFaceDown = 0;
                    foreach (var m in player.Opponent.MonsterZones) if (m != null && m.FaceDown) foeFaceDown++;
                    return foeFaceDown;
                }
                case EffectCountKind.OpponentIllusionTokens:
                {
                    if (player.Opponent == null) return 0;
                    int tokens = 0;
                    foreach (var m in player.Opponent.MonsterZones)
                        if (m != null && m.Definition != null && m.Definition.isToken) tokens++;
                    return tokens;
                }
                case EffectCountKind.OwnHandCards: return player.Hand.Count;
                case EffectCountKind.OwnGraveyardSpells:
                {
                    int spells = 0;
                    foreach (var c in player.Graveyard) if (c.SpellData != null) spells++;
                    return spells;
                }
                case EffectCountKind.OwnDistinctLevels:
                {
                    // Stuck on the Middle Rung: verschiedene Level unter den eigenen OFFENEN Monstern
                    var seen = new HashSet<int>();
                    foreach (var m in player.MonsterZones)
                        if (m != null && !m.FaceDown && m.MonsterData != null) seen.Add(m.EffectiveLevel);
                    return seen.Count;
                }
                case EffectCountKind.OwnMonstersOnOpponentField:
                {
                    // Giftwyrm: eigene (OriginalOwner) Monster in den Zonen des Gegners
                    if (player.Opponent == null) return 0;
                    int delivered = 0;
                    foreach (var m in player.Opponent.MonsterZones)
                        if (m != null && m.OriginalOwner == player) delivered++;
                    return delivered;
                }
                case EffectCountKind.AllArtifactsOnField:
                {
                    int artifactsBoth = 0;
                    foreach (var a in player.ArtifactZones) if (a != null) artifactsBoth++;
                    if (player.Opponent != null)
                        foreach (var a in player.Opponent.ArtifactZones) if (a != null) artifactsBoth++;
                    return artifactsBoth;
                }
                case EffectCountKind.OwnBanishedCards: return player.Banished.Count;
                default: return 0;
            }
        }

        /// <summary>
        /// Summe aller Dauer-Auren, die auf DIESES Monster wirken: eigene offene
        /// Feldkarten (Monster + Artefakte) mit Aura-Werten, deren Filter passen.
        /// Bewusst als Live-Scan statt Buchhaltung — Auren erscheinen und
        /// verschwinden mit ihrer Quelle, ohne Auf-/Abbau-Hooks.
        /// </summary>
        private int AuraBonus(bool atk)
        {
            if (Owner == null || Zone != ZoneType.MonsterZone || MonsterData == null) return 0;
            int total = 0;
            for (int zone = 0; zone < 2; zone++)
            {
                var sources = zone == 0 ? Owner.MonsterZones : Owner.ArtifactZones;
                foreach (var source in sources)
                {
                    var def = source != null ? source.Definition : null;
                    if (def == null) continue;
                    if (source.FaceDown) continue;                      // verdeckte Quellen strahlen nicht

                    // The Small Print: Zonen-Auren ohne klassischen Aura-Wert
                    if (atk && def.auraCrowdedAtkPenalty > 0 && source != this && HasAdjacentMonster()) total -= def.auraCrowdedAtkPenalty;
                    if (atk && def.passiveStolenAtkBonus > 0 && Owner != OriginalOwner) total += def.passiveStolenAtkBonus;
                    if (atk && def.passiveLienAtkPenalty > 0 && LienAmount > 0) total -= def.passiveLienAtkPenalty;

                    if (def.auraExcludesSelf && source == this) continue;
                    int bonus = atk ? def.auraAtkBonus : def.auraDefBonus;
                    if (bonus == 0) continue;
                    if (!string.IsNullOrEmpty(def.auraNameFilter)
                        && (Definition == null || !Definition.cardName.Contains(def.auraNameFilter))) continue;
                    if (def.auraUseTypeFilter && MonsterData.monsterType != def.auraTypeFilter) continue;
                    if (def.auraLevelFilter > 0 && EffectiveLevel != def.auraLevelFilter) continue;
                    if (def.auraOnlyFaceDown && !FaceDown) continue;
                    if (def.auraAdjacentOnly && !IsAdjacentTo(source)) continue;
                    if (def.auraAloneOnly && HasAdjacentMonster()) continue;
                    total += bonus;
                }
            }

            // Gegnerische Quellen: das Monster GEGENÜBER (Rook's Gambit) und Pfandrecht-Strafen (Bailiff)
            if (Owner.Opponent != null)
            {
                int index = ZoneIndex;
                foreach (var source in Owner.Opponent.MonsterZones)
                {
                    var def = source != null ? source.Definition : null;
                    if (def == null || source.FaceDown) continue;
                    if (atk && def.facingAtkPenalty > 0 && index >= 0 && source.ZoneIndex == index) total -= def.facingAtkPenalty;
                    if (atk && def.passiveLienAtkPenalty > 0 && LienAmount > 0) total -= def.passiveLienAtkPenalty;
                }
            }
            return total;
        }

        // ---------- The Small Print: Zonen-Geometrie ----------

        /// <summary>Index dieser Karte in den Monsterzonen ihres Kontrolleurs (-1 = nicht auf dem Feld).</summary>
        public int ZoneIndex => Owner != null && Zone == ZoneType.MonsterZone
            ? System.Array.IndexOf(Owner.MonsterZones, this) : -1;

        /// <summary>Liegen beide auf derselben Seite direkt nebeneinander?</summary>
        public bool IsAdjacentTo(CardInstance other)
        {
            if (other == null || other == this || other.Owner != Owner) return false;
            int a = ZoneIndex, b = other.ZoneIndex;
            return a >= 0 && b >= 0 && System.Math.Abs(a - b) == 1;
        }

        /// <summary>Hat diese Karte links oder rechts ein Monster (gleiche Seite)?</summary>
        public bool HasAdjacentMonster()
        {
            int index = ZoneIndex;
            if (index < 0) return false;
            var zones = Owner.MonsterZones;
            return (index > 0 && zones[index - 1] != null) || (index < zones.Length - 1 && zones[index + 1] != null);
        }

        /// <summary>Die eigenen Nachbarn (0–2 Karten).</summary>
        public List<CardInstance> AdjacentMonsters()
        {
            var result = new List<CardInstance>();
            int index = ZoneIndex;
            if (index < 0) return result;
            var zones = Owner.MonsterZones;
            if (index > 0 && zones[index - 1] != null) result.Add(zones[index - 1]);
            if (index < zones.Length - 1 && zones[index + 1] != null) result.Add(zones[index + 1]);
            return result;
        }

        /// <summary>Das gegnerische Monster in der Zone gegenüber (null = keins).</summary>
        public CardInstance FacingMonster()
        {
            int index = ZoneIndex;
            if (index < 0 || Owner.Opponent == null) return null;
            var zones = Owner.Opponent.MonsterZones;
            return index < zones.Length ? zones[index] : null;
        }

        /// <summary>Piercing dauerhaft (Passiv, Ram's Head) oder bis Zugende (Trample the Line)?</summary>
        public bool HasPiercing
        {
            get
            {
                if (PiercingThisTurn) return true;
                if (Definition != null && Definition.passivePiercing) return true;
                foreach (var artifact in EquippedArtifacts)
                    if (artifact.Definition != null && artifact.Definition.passiveBearerPiercing) return true;
                return false;
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
