using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Rouge.Tcg
{
    public abstract class CardDefinition : ScriptableObject
    {
        [Header("Karte")]
        [Tooltip("Anzeigename der Karte")]
        public string cardName = "Neue Karte";

        [Tooltip("Kartenbild (optional)")]
        public Sprite artwork;

        [Tooltip("Seltenheit — bestimmt Pack-Ziehungen und Craft-Kosten")]
        public CardRarity rarity = CardRarity.Common;

        [Tooltip("Token: nur von Effekten erzeugt — nicht sammelbar, nicht im Deck, löst sich beim Verlassen des Feldes auf")]
        public bool isToken;

        [Header("Effekte (Normal & Infused)")]
        [Tooltip("Alle Effekte dieser Karte. Infused-Effekte werden auf der Karte getrennt dargestellt und kosten Mana.")]
        public List<EffectDefinition> effects = new List<EffectDefinition>();

        // ---- Dauer-Aura: wirkt, solange die Karte offen auf dem Feld liegt,
        //      auf EIGENE Monster, die die Filter erfüllen (Batch August 2026) ----
        [Header("Dauer-Aura (0 = keine)")]
        [Tooltip("ATK-Bonus für eigene Monster, die die Aura-Filter erfüllen")]
        public int auraAtkBonus;
        [Tooltip("DEF-Bonus für eigene Monster, die die Aura-Filter erfüllen")]
        public int auraDefBonus;
        [Tooltip("Aura nur für Monster, deren Name diesen Text enthält (leer = alle)")]
        public string auraNameFilter = "";
        [Tooltip("Aura nur für Monster dieses Typs?")]
        public bool auraUseTypeFilter;
        public MonsterType auraTypeFilter = MonsterType.Beast;
        [Range(0, 3)]
        [Tooltip("Aura nur für Monster dieses Levels (0 = alle)")]
        public int auraLevelFilter;
        [Tooltip("Aura wirkt nur auf VERDECKTE eigene Monster (Blackout Curtain)")]
        public bool auraOnlyFaceDown;
        [Tooltip("Die Quellkarte selbst bekommt die Aura nicht (»deine anderen Monster«)")]
        public bool auraExcludesSelf;

        [Header("Passive Flaggen (Batch August 2026)")]
        [Tooltip("Der Gegner muss dieses Monster angreifen, solange es offen liegt (Attention Hound)")]
        public bool passiveTaunt;
        [Tooltip(">0: im Kampf unzerstörbar, solange du mindestens N Artefakte kontrollierst (Ironclad)")]
        public int battleShieldMinOwnArtifacts;
        [Tooltip("Zählt beim Reliquary-Tribut als N Monster (Twice-Blessed)")]
        public int tributeWorth = 1;
        [Tooltip("Eigene Karten mit diesem Namen sind für den Gegner kein gültiges Effekt-Ziel (Heavenly Bodyguard)")]
        public string protectsNamedFromTargeting = "";
        [Tooltip("Zweiter Angriff pro Battle Phase, solange ein ANDERES eigenes Monster dieses Attributs offen liegt")]
        public bool conditionalDoubleAttack;
        public MonsterAttribute doubleAttackAttribute = MonsterAttribute.Wind;

        [Tooltip(">0: dieses Monster hat dauerhaft +N ATK pro gezählter Karte (Weight of Evidence)")]
        public int passiveAtkPerCount;
        public EffectCountKind passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;
        [Tooltip(">0: dieses Monster hat dauerhaft +N DEF pro gezählter Karte")]
        public int passiveDefPerCount;
        public EffectCountKind passiveDefPerCountKind = EffectCountKind.OwnArtifactsOnField;

        [Tooltip("Dieses Monster kann nie angreifen (Barrierstruck Peacekeeper)")]
        public bool passiveCannotAttack;

        [Tooltip("Dieses Monster kann im Zug seiner Beschwörung nicht angreifen (Slow to Anger)")]
        public bool passiveNoAttackOnSummonTurn;

        [Tooltip("Dieses Monster kann im Zug seiner Beschwörung nicht DIREKT angreifen (Tidebound Leviathan)")]
        public bool passiveNoDirectAttackOnSummonTurn;

        [Tooltip("Feld-Limit (Snugglet): dieses Monster ist nicht beschwörbar/setzbar, solange du " +
                 "bereits N Monster kontrollierst, deren Name diesen Text enthält (leer = kein Limit)")]
        public string fieldLimitName = "";
        [Tooltip("Das N zum Feld-Limit (0 = aus)")]
        public int fieldLimitCount;

        [Tooltip(">0 (King of Deckay): jedes Mal, wenn der Besitzer 1+ Karten millt, erleidet " +
                 "der Gegner so viel Schaden — solange diese Karte offen auf dem Feld liegt")]
        public int passiveBurnPerMill;

        [Header("Dark-Angel-Passives (August 2026)")]
        [Tooltip("Solange offen auf dem Feld: KEIN Spieler kann Reliquaries beschwören (The Fallen One)")]
        public bool passiveBlockReliquarySummons;

        [Tooltip("Dieses Monster kann nicht durch Kampf zerstört werden")]
        public bool passiveNoBattleDestroy;

        [Tooltip("Dieses Monster kann nicht von gegnerischen Effekten als Ziel gewählt werden")]
        public bool passiveUntargetable;

        [Tooltip("Kein Spieler erleidet Kampfschaden aus Kämpfen, an denen dieses Monster beteiligt ist")]
        public bool passiveNoBattleDamageInvolving;

        [Tooltip("Solange offen auf dem Feld: der GEGNER erleidet allen Kampfschaden, den der " +
                 "Besitzer erleiden würde (The Last Asemir)")]
        public bool passiveRedirectBattleDamage;

        [Tooltip(">0: in jeder End Phase erhält diese Karte einen Death Counter; bei N Countern " +
                 "geht sie in den Friedhof (Immortal Demon)")]
        public int passiveDeathCounterLimit;

        [Tooltip(">0: jedes Mal, wenn der GEGNER Karten aus seinem Deck in seinen Friedhof schickt " +
                 "(ausser durch diesen Effekt selbst), schickt er N weitere hinterher " +
                 "(Exponential Deterioration)")]
        public int passiveOpponentMillAmplify;

        [Header("Schutz & Zwang (August 2026, Teil 2)")]
        [Tooltip("Solange offen auf dem Feld: KEINE Karte des Besitzers kann vom Gegner als " +
                 "Effekt-Ziel gewählt oder angegriffen werden — diese Karte eingeschlossen. " +
                 "Nicht-zielende Effekte (z.B. \"destroy all\") wirken weiter (Emergency Barrier)")]
        public bool passiveProtectAllFromTargetingAndAttacks;

        [Tooltip(">0: in JEDER End Phase zahlt der Besitzer N LP oder diese Karte wird zerstört " +
                 "(Emergency Barrier)")]
        public int passiveEndPhaseLpToll;

        [Tooltip("Solange offen auf dem Feld: der GEGNER kann Zauber nicht aus der Hand aktivieren — " +
                 "er muss sie erst setzen (und gesetzte Zauber zünden frühestens im Folgezug) " +
                 "(The Liberator)")]
        public bool passiveOpponentMustSetSpells;

        [Header("The Small Print (August 2026)")]
        [Tooltip("Aura wirkt nur auf die Monster in den NACHBARZONEN dieser Karte (Serjeant Halloway)")]
        public bool auraAdjacentOnly;

        [Tooltip("Aura wirkt nur auf eigene Monster OHNE Nachbarn (The Empty Chair); " +
                 "auraCrowdedAtkPenalty trifft die mit Nachbarn")]
        public bool auraAloneOnly;

        [Tooltip(">0: eigene Monster MIT Nachbarn verlieren so viel ATK (The Empty Chair)")]
        public int auraCrowdedAtkPenalty;

        [Tooltip(">0: das gegnerische Monster GEGENÜBER dieser Karte verliert so viel ATK (Rook's Gambit)")]
        public int facingAtkPenalty;

        [Tooltip("Nachbarn dieser Karte können nicht durch Karteneffekte zerstört werden (Load-Bearing Wall)")]
        public bool passiveAdjacentNoEffectDestroy;

        [Tooltip("Nachbarn dieser Karte können nicht durch Kampf zerstört werden (Castellan of the Long Wall)")]
        public bool passiveAdjacentNoBattleDestroy;

        [Tooltip(">0: wird diese Karte zerstört, verlieren ihre Nachbarn dauerhaft N ATK und DEF (Load-Bearing Wall)")]
        public int passiveAdjacentDebuffOnDestroy;

        [Tooltip("Diese Karte kann nicht durch Karteneffekte zerstört werden (Stone That Would Not Break)")]
        public bool passiveNoEffectDestroy;

        [Tooltip("Diese Karte kann nicht als Tribut gezahlt werden (White Elephant, Gift Horse, Stone)")]
        public bool passiveCannotBeTributed;

        [Tooltip("Diese Karte kann ihre Kampfposition nicht wechseln — Effekte, die es versuchen, verpuffen " +
                 "(Load-Bearing Wall, Stone That Would Not Break)")]
        public bool passiveCannotChangePosition;

        [Tooltip("Hat diese Karte in diesem Zug die Position gewechselt, kann sie nicht angreifen (Volte-Face)")]
        public bool passiveNoAttackAfterPositionChange;

        [Tooltip("Kann nicht als Normalbeschwörung beschworen oder gesetzt werden — nur per eigener " +
                 "Spezialbeschwörung (White Elephant, Blood Oath, Sworn to the Gate, Load-Bearing Wall)")]
        public bool passiveNoNormalSummon;

        [Tooltip(">0: in jeder Standby Phase des KONTROLLEURS verliert dieser N LP (White Elephant, Gift Horse)")]
        public int passiveControllerStandbyLpLoss;

        [Tooltip("Solange auf dem Feld: der Besitzer kann keine ANDEREN Monster spezialbeschwören (Sworn to the Gate)")]
        public bool passiveOwnerNoOtherSpecialSummons;

        [Tooltip("Solange der Besitzer keine anderen Monster kontrolliert: nicht durch Kampf oder Effekte " +
                 "zerstörbar (Sworn to the Gate)")]
        public bool passiveLoneImmunity;

        [Tooltip("Solange der Besitzer 1 oder weniger Handkarten hat: nicht zielbar, nicht durch Effekte " +
                 "zerstörbar (The Ascetic of the Ninth Stair)")]
        public bool passiveLowHandImmunity;

        [Tooltip("Piercing: Kampfschaden gegen Verteidigungsposition in Höhe der Differenz (Bristleback Aurochs)")]
        public bool passivePiercing;

        [Tooltip("Ausgerüstetes Monster erhält Piercing (Ram's Head)")]
        public bool passiveBearerPiercing;

        [Tooltip("Ram's Head: greift der Träger ein Verteidigungs-Monster an und zerstört es nicht, " +
                 "wird dieses Artefakt zerstört")]
        public bool passiveBreakOnFailedPierce;

        [Tooltip("Darf direkt angreifen, auch wenn der Gegner Monster kontrolliert; Kampfschaden " +
                 "direkter Angriffe halbiert (Chimney Sweep)")]
        public bool passiveDirectAttackHalved;

        [Tooltip("Kann nie direkt angreifen (Bristleback Aurochs)")]
        public bool passiveNoDirectAttack;

        [Tooltip("Field: Zauber kosten für BEIDE Spieler 1 Mana mehr; wer selbst gezaubert hat, verliert " +
                 "die Karte in seiner End Phase (Guild Tariff)")]
        public bool passiveSpellTaxBoth;

        [Tooltip("Field: jeder Spieler darf pro Battle Phase nur EINEN Angriff erklären; Angreifer +N ATK " +
                 "während des Kampfes (The Duelist's Code)")]
        public int passiveOneAttackBonus;

        [Tooltip("Player: Standby +N Mana; End Phase mit mehr als passiveHandCapForSurvival Handkarten " +
                 "zerstört die Karte (Vow of Poverty)")]
        public int passiveStandbyBonusMana;
        public int passiveHandCapForSurvival;

        [Tooltip(">0: sinken die LP des Besitzers auf N oder darunter, wird die Karte zerstört (Ledger of Small Debts)")]
        public int passiveDestroyWhenLifeAtMost;

        [Tooltip("Player: LP-Kosten des Besitzers werden 0 (Aurel, Who Collects at Midnight)")]
        public bool passiveLifeCostsFree;

        [Tooltip("Player: Münzwürfe des Besitzers zweimal werfen und Ergebnis wählen; zwei Tails zerstören " +
                 "die Karte (Loaded Dice)")]
        public bool passiveCoinChoose;

        [Tooltip("Liegen die LP des Besitzers unter denen des Gegners, zählt Tails als Heads (The House Always Wins)")]
        public bool passiveTailsAsHeadsWhenBehind;

        [Tooltip("Monster mit Pfandrecht verlieren so viel ATK (The Bailiff at the Door)")]
        public int passiveLienAtkPenalty;

        [Tooltip("Monster, die der Besitzer kontrolliert, aber nicht besitzt, erhalten so viel ATK (Broker of Both Sides)")]
        public int passiveStolenAtkBonus;

        public abstract CardKind Kind { get; }

        public abstract Color FrameColor { get; }

        /// <summary>Anzeige-Farbe der Seltenheit (grau/grün/blau/gold).</summary>
        public static Color RarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return new Color(0.45f, 0.85f, 0.45f);
                case CardRarity.Rare: return new Color(0.40f, 0.65f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.72f, 0.20f);
                default: return new Color(0.80f, 0.80f, 0.85f);
            }
        }

        public static string RarityName(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return "Uncommon";
                case CardRarity.Rare: return "Rare";
                case CardRarity.Legendary: return "Legendary";
                default: return "Common";
            }
        }

        /// <summary>
        /// Menschenlesbare Zeilen für alle dauerhaften Passiv-Fähigkeiten dieser Karte.
        /// Die UI (Kartenpanel, Detail-Ansicht) hängt sie als PASSIVE-Block vor die
        /// Effektliste — Daten-Felder wie die Aura hätten sonst keinen Kartentext.
        /// </summary>
        public virtual List<string> BuildPassiveLines()
        {
            var lines = new List<string>();

            if (auraAtkBonus != 0 || auraDefBonus != 0)
            {
                string scope = auraOnlyFaceDown ? "face-down " : "";
                string named = string.IsNullOrEmpty(auraNameFilter) ? "" : $" \"{auraNameFilter}\"";
                string typed = auraUseTypeFilter ? $" {auraTypeFilter.ToString().ToUpperInvariant()}" : "";
                string leveled = auraLevelFilter > 0 ? $" Level {auraLevelFilter}" : "";
                string other = auraExcludesSelf ? "other " : "";
                string bonus = auraAtkBonus != 0 && auraDefBonus != 0
                    ? $"{Signed(auraAtkBonus)} ATK and {Signed(auraDefBonus)} DEF"
                    : auraAtkBonus != 0 ? $"{Signed(auraAtkBonus)} ATK" : $"{Signed(auraDefBonus)} DEF";
                lines.Add($"Your {other}{scope}{named}{typed}{leveled} monsters gain {bonus}.".Replace("  ", " "));
            }

            if (passiveAtkPerCount > 0)
                lines.Add($"This card gains {passiveAtkPerCount} ATK for each of {CountName(passiveAtkPerCountKind)}.");
            if (passiveDefPerCount > 0)
                lines.Add($"This card gains {passiveDefPerCount} DEF for each of {CountName(passiveDefPerCountKind)}.");
            if (passiveCannotAttack)
                lines.Add("This card cannot attack.");
            if (passiveNoAttackOnSummonTurn)
                lines.Add("This card cannot attack during the turn it is Summoned.");
            if (passiveNoDirectAttackOnSummonTurn)
                lines.Add("This card cannot attack directly during the turn it is Summoned.");
            if (passiveTaunt)
                lines.Add("Your opponent's attacks must target this card.");
            if (battleShieldMinOwnArtifacts > 0)
                lines.Add($"While you control {battleShieldMinOwnArtifacts}+ Artifacts, this card cannot be destroyed by battle.");
            if (tributeWorth > 1)
                lines.Add($"Counts as {tributeWorth} tributes for a Reliquary Summon.");
            if (!string.IsNullOrEmpty(protectsNamedFromTargeting))
                lines.Add($"Your other \"{protectsNamedFromTargeting}\" cards cannot be targeted by your opponent's effects.");
            if (conditionalDoubleAttack)
                lines.Add($"Can attack twice each Battle Phase while you control another face-up {doubleAttackAttribute.ToString().ToUpperInvariant()} monster.");
            if (fieldLimitCount > 0 && !string.IsNullOrEmpty(fieldLimitName))
                lines.Add($"You cannot Summon or Set this card while you control {fieldLimitCount} \"{fieldLimitName}\" monsters.");
            if (passiveBurnPerMill > 0)
                lines.Add($"Every time you mill 1 or more cards: deal {passiveBurnPerMill} damage to your opponent.");
            if (passiveBlockReliquarySummons)
                lines.Add("While this card is on the field, neither player can Special Summon Reliquaries.");
            if (passiveNoBattleDestroy)
                lines.Add("This card cannot be destroyed by battle.");
            if (passiveUntargetable)
                lines.Add("This card cannot be targeted by your opponent's effects.");
            if (passiveNoBattleDamageInvolving)
                lines.Add("Neither player takes battle damage from battles involving this card.");
            if (passiveRedirectBattleDamage)
                lines.Add("Your opponent takes all battle damage you would take instead.");
            if (passiveDeathCounterLimit > 0)
                lines.Add($"During each End Phase: put a Death Counter on this card. With {passiveDeathCounterLimit} Death Counters, it is sent to the Graveyard.");
            if (passiveOpponentMillAmplify > 0)
                lines.Add($"Every time your opponent sends cards from their Deck to the Graveyard (except by this effect): they send {passiveOpponentMillAmplify} more.");
            if (passiveProtectAllFromTargetingAndAttacks)
                lines.Add("While this card is on the field, your cards cannot be targeted by your opponent's effects or attacks.");
            if (passiveEndPhaseLpToll > 0)
                lines.Add($"During each End Phase: pay {passiveEndPhaseLpToll} LP or destroy this card.");
            if (passiveOpponentMustSetSpells)
                lines.Add("While this card is face-up on the field, your opponent must Set Spells before activating them.");

            // --- The Small Print ---
            if (auraAdjacentOnly && (auraAtkBonus != 0 || auraDefBonus != 0))
                lines.Add("(The aura above applies only to monsters adjacent to this card.)");
            if (auraAloneOnly && auraAtkBonus != 0)
                lines.Add("(The aura above applies only to your monsters with no adjacent monster.)");
            if (auraCrowdedAtkPenalty > 0)
                lines.Add($"Your monsters with an adjacent monster lose {auraCrowdedAtkPenalty} ATK.");
            if (facingAtkPenalty > 0)
                lines.Add($"The monster facing this card loses {facingAtkPenalty} ATK.");
            if (passiveAdjacentNoEffectDestroy)
                lines.Add("Monsters adjacent to this card cannot be destroyed by card effects.");
            if (passiveAdjacentNoBattleDestroy)
                lines.Add("Monsters adjacent to this card cannot be destroyed by battle.");
            if (passiveAdjacentDebuffOnDestroy > 0)
                lines.Add($"When this card is destroyed: the monsters adjacent to it permanently lose {passiveAdjacentDebuffOnDestroy} ATK and DEF.");
            if (passiveNoEffectDestroy)
                lines.Add("This card cannot be destroyed by card effects.");
            if (passiveCannotBeTributed)
                lines.Add("This card cannot be Tributed.");
            if (passiveCannotChangePosition)
                lines.Add("This card cannot change its battle position.");
            if (passiveNoAttackAfterPositionChange)
                lines.Add("If this card changed its battle position this turn, it cannot attack.");
            if (passiveNoNormalSummon)
                lines.Add("Cannot be Normal Summoned or Set.");
            if (passiveControllerStandbyLpLoss > 0)
                lines.Add($"During each of its controller's Standby Phases: its controller loses {passiveControllerStandbyLpLoss} LP.");
            if (passiveOwnerNoOtherSpecialSummons)
                lines.Add("While this card is on the field, you cannot Special Summon other monsters.");
            if (passiveLoneImmunity)
                lines.Add("While you control no other monsters, this card cannot be destroyed by battle or by card effects.");
            if (passiveLowHandImmunity)
                lines.Add("While you have 1 or fewer cards in hand, this card cannot be targeted and cannot be destroyed by card effects.");
            if (passivePiercing)
                lines.Add("Piercing: when this card attacks a Defense Position monster with lower DEF, the difference is dealt as battle damage.");
            if (passiveBearerPiercing)
                lines.Add("The equipped monster inflicts piercing battle damage.");
            if (passiveBreakOnFailedPierce)
                lines.Add("If the equipped monster attacks a Defense Position monster and does not destroy it, destroy this card.");
            if (passiveDirectAttackHalved)
                lines.Add("This card can attack directly even if your opponent controls monsters; battle damage from its direct attacks is halved.");
            if (passiveNoDirectAttack)
                lines.Add("This card cannot attack directly.");
            if (passiveSpellTaxBoth)
                lines.Add("Spells cost 1 more Mana for both players. During your End Phase, if you activated a Spell this turn, destroy this card.");
            if (passiveOneAttackBonus > 0)
                lines.Add($"Each player may declare only one attack per Battle Phase. Attacking monsters gain {passiveOneAttackBonus} ATK during the battle.");
            if (passiveStandbyBonusMana > 0)
                lines.Add($"During your Standby Phase: gain {passiveStandbyBonusMana} additional Mana this turn.");
            if (passiveHandCapForSurvival > 0)
                lines.Add($"During your End Phase, if you hold more than {passiveHandCapForSurvival} cards, destroy this card.");
            if (passiveDestroyWhenLifeAtMost > 0)
                lines.Add($"When your LP are {passiveDestroyWhenLifeAtMost} or less, this card is destroyed.");
            if (passiveLifeCostsFree)
                lines.Add("LP costs you pay are reduced to 0.");
            if (passiveCoinChoose)
                lines.Add("Once per turn, when you flip a coin, flip it twice and choose which result counts. If both land Tails, destroy this card.");
            if (passiveTailsAsHeadsWhenBehind)
                lines.Add("While your LP are lower than your opponent's, your coin flips that land Tails count as Heads.");
            if (passiveLienAtkPenalty > 0)
                lines.Add($"Monsters with a Lien lose {passiveLienAtkPenalty} ATK.");
            if (passiveStolenAtkBonus > 0)
                lines.Add($"Monsters you control but do not own gain {passiveStolenAtkBonus} ATK.");

            return lines;
        }

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();

        /// <summary>Zählbasis als Kartentext ("each of your Artifacts on the field").</summary>
        protected static string CountName(EffectCountKind kind)
        {
            switch (kind)
            {
                case EffectCountKind.OwnArtifactsOnField: return "your Artifacts on the field";
                case EffectCountKind.OwnGraveyardArtifacts: return "the Artifacts in your Graveyard";
                case EffectCountKind.OwnFaceDownMonsters: return "your face-down monsters";
                case EffectCountKind.OwnBanishedMonsters: return "your banished monsters";
                case EffectCountKind.OwnGraveyardCards: return "the cards in your Graveyard";
                case EffectCountKind.EquippedArtifactsOnSelf: return "its equipped Artifacts";
                case EffectCountKind.OpponentFaceDownMonsters: return "your opponent's face-down monsters";
                case EffectCountKind.OpponentIllusionTokens: return "the Illusion Tokens your opponent controls";
                case EffectCountKind.OwnHandCards: return "the cards in your hand";
                case EffectCountKind.OwnGraveyardSpells: return "the Spells in your Graveyard";
                default: return "your monsters on the field";
            }
        }

        /// <summary>Setzt den vollständigen Regeltext der Karte aus den Effekten zusammen.</summary>
        public virtual string BuildRulesText()
        {
            var sb = new StringBuilder();
            foreach (var effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.text)) continue;
                if (sb.Length > 0) sb.AppendLine();
                string infusedName = effect.infusedKind == InfusedKind.Coupled ? "Or Infused" : "Infused";
                string prefix = effect.isInfused
                    ? "[" + infusedName + (effect.manaCost > 0 ? " – " + effect.manaCost + " Mana" : "") + "] "
                    : (effect.manaCost > 0 ? "[" + effect.manaCost + " Mana] " : "");
                sb.Append(prefix).Append(effect.text);
            }
            return sb.ToString();
        }
    }
}
