using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // „Road to 1000" (Design freigegeben 25.08.2026): 50 Generics in sechs
    // Mechanik-Familien plus Einzelstücke — alternative Win-Condition (Krönung),
    // Zonen-Siegel, Friedhofs-Spitze, Level-Spiele, Deck-Spitze & Countdown,
    // DEF-Angriff, Reveal. Nutzt die Batch2026Builder-Helfer (partial) und ist
    // idempotent: ein zweiter Lauf setzt alle Felder neu, Artworks bleiben.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build Road to 1000 (50 Generics)")]
        public static void BuildRoadTo1000()
        {
            built.Clear();
            RtAbsentKing(); RtSealedZones(); RtFreshGrave(); RtLevelForge();
            RtTomorrowsNews(); RtShieldFirst(); RtOpenHand(); RtSingles();
            Finish("Road to 1000");
        }

        // ================== Helfer nur für dieses Set ==================

        private const string RoadVersion = "0.1.7";

        /// <summary>Alle Road-to-1000-Felder zurücksetzen + Set-Version stempeln.</summary>
        private static void RtReset(CardDefinition card)
        {
            card.releaseVersion = RoadVersion;
            card.passiveStatsFromGraveTop = false;
            card.passiveAttacksWithDef = false;
            card.passiveDefLossAfterAttack = 0;
            card.passiveSealsAdjacentZones = false;
            card.passiveBearerZoneLocked = false;
            card.passiveUntargetableWhileLpClose = 0;
            card.passiveDrawReplacementGraveTop = false;
            card.countdownMarkers = 0;
            if (card is MonsterCardData monster)
            {
                monster.selfSummonRequiresOpponentMoreMonsters = false;
                monster.selfSummonRequiresLifeAtMost = 0;
                monster.selfSummonIntoSealedZone = false;
                monster.selfSummonRequiresGraveTopMonster = false;
                monster.selfSummonRequiresRevealedThisTurn = false;
                monster.selfSummonRequiresLevels1And3 = false;
                monster.selfSummonRequiresOpponentLevel3AndNoneSelf = false;
                monster.selfSummonRequiresTurnAtLeast = 0;
                monster.selfSummonRequiresDeckAtLeast = 0;
                monster.selfSummonRequiresLpWithin = 0;
                monster.selfSummonRequiresArtifacts = 0;
            }
        }

        private static MonsterCardData RtMon(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = SpMon(name, rarity, level, attribute, type, atk, def, effects);
            RtReset(card);
            return card;
        }

        private static SpellCardData RtSpell(string name, CardRarity rarity, bool quick, params EffectDefinition[] effects)
        {
            var card = SpSpell(name, rarity, quick, effects);
            RtReset(card);
            return card;
        }

        private static ArtifactCardData RtArtifact(string name, CardRarity rarity, ArtifactSlot slot,
            int atkBonus = 0, int defBonus = 0, params EffectDefinition[] effects)
        {
            var card = SpArtifact(name, rarity, slot, atkBonus, defBonus, effects);
            RtReset(card);
            return card;
        }

        // ---- Feinwürze für die neuen Engine-Felder ----

        private static EffectAction RtNoAttack(this EffectAction action) { action.summonCannotAttack = true; return action; }
        private static EffectAction RtInDefense(this EffectAction action) { action.summonInDefense = true; return action; }
        private static EffectAction RtZeroAtk(this EffectAction action) { action.zeroAtkOnly = true; return action; }
        private static EffectAction RtUnderleveled(this EffectAction action) { action.requireLevelBelowControllerCount = true; return action; }
        private static EffectAction RtEvictable(this EffectAction action) { action.onlyCannotAttack = true; return action; }

        private static EffectDefinition RtRegalia(this EffectDefinition effect)
        {
            effect.requiresControlNamed = "The Crown of the Absent King;The Sceptre of the Absent King;The Orb of the Absent King";
            effect.mandatory = true;
            return effect;
        }
        private static EffectDefinition RtWhileGraveTop(this EffectDefinition effect) { effect.onlyWhileGraveTop = true; return effect; }
        private static EffectDefinition RtAfterOwnLoss(this EffectDefinition effect) { effect.requireOwnMonsterDestroyedThisTurn = true; return effect; }
        private static EffectDefinition RtFirstTurnOnly(this EffectDefinition effect) { effect.onlyOnFirstOwnTurn = true; return effect; }
        private static EffectDefinition RtMaxMonsters(this EffectDefinition effect, int most) { effect.maxOwnMonsters = most; return effect; }

        private const string CoronationText =
            "At the start of your Standby Phase, if you control \"The Crown of the Absent King\", " +
            "\"The Sceptre of the Absent King\" and \"The Orb of the Absent King\": you win the Duel.";

        // ================== A. DER ABWESENDE KÖNIG (Alt-Win, 5) ==================
        private static void RtAbsentKing()
        {
            var crown = RtMon("The Crown of the Absent King", CardRarity.Rare, 2, MonsterAttribute.Light, MonsterType.Myth, 0, 2000,
                Fx("Coronation", CoronationText,
                    EffectTrigger.StandbyPhase, 0, false,
                    Act(EffectActionType.WinTheDuel)).RtRegalia(),
                Inf("Hold the Crown", "Pay 2 Mana: this card cannot be destroyed this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectSelfThisTurn)));
            crown.passiveCannotAttack = true;
            crown.canSelfSpecialSummon = true;
            crown.selfSummonRequiresNoOwnMonsters = true;
            crown.selfSummonPosition = BattlePosition.Defense;

            var sceptre = RtMon("The Sceptre of the Absent King", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Myth, 0, 1800,
                Fx("Coronation", CoronationText,
                    EffectTrigger.StandbyPhase, 0, false,
                    Act(EffectActionType.WinTheDuel)).RtRegalia(),
                Fx("No One Rises", "Once per turn: 1 monster your opponent controls loses 300 ATK until the end of the turn.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -300, TargetKind.EnemyMonster)),
                Inf("No One At All", "Pay 2 Mana: up to 2 monsters your opponent controls lose 300 ATK until the end of the turn instead.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -300, TargetKind.EnemyMonster,
                        targetCount: 2, upTo: true)));
            sceptre.passiveCannotAttack = true;
            sceptre.canSelfSpecialSummon = true;
            sceptre.selfSummonRequiresOpponentMoreMonsters = true;
            sceptre.selfSummonPosition = BattlePosition.Defense;

            var orb = RtMon("The Orb of the Absent King", CardRarity.Rare, 2, MonsterAttribute.Water, MonsterType.Myth, 0, 1900,
                Fx("Coronation", CoronationText,
                    EffectTrigger.StandbyPhase, 0, false,
                    Act(EffectActionType.WinTheDuel)).RtRegalia(),
                Fx("The Realm Endures", "During your Standby Phase: gain 300 LP.",
                    EffectTrigger.StandbyPhase, 0, false,
                    Act(EffectActionType.HealSelf, 300)),
                Inf("Kept in Trust", "Pay 2 Mana: shuffle 1 card from your Graveyard into your Deck.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ShuffleGraveyardIntoDeck, 1, TargetKind.GraveyardCardSelf)));
            orb.passiveCannotAttack = true;
            orb.canSelfSpecialSummon = true;
            orb.selfSummonRequiresLifeAtMost = 4000;
            orb.selfSummonPosition = BattlePosition.Defense;

            RtMon("The Regent Who Keeps the Throne Warm", CardRarity.Uncommon, 1, MonsterAttribute.Earth, MonsterType.Human, 700, 1400,
                Fx("Keep the Seat", "When this card is Summoned: add 1 monster with 0 ATK from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered).RtZeroAtk()),
                Inf("Warm the Seat", "Pay 2 Mana: Special Summon it instead — its effects are negated until the End Phase, and it cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromDeckSuppressed, 1, TargetKind.DeckMonsterFiltered).RtZeroAtk()));

            RtSpell("Long Live the King", CardRarity.Uncommon, true,
                Fx("Long Live the King", "Pay 2 Mana: Special Summon 1 monster with 0 ATK from your Graveyard in Defense Position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf).RtZeroAtk().RtInDefense()),
                Inf("Long May He Reign", "Instead, pay 4 Mana: it also cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf).RtZeroAtk().RtInDefense(),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.SameAsPrevious)));
        }

        // ================== B. ZUGEMAUERTE ZONEN (Siegel, 6) ==================
        private static void RtSealedZones()
        {
            var bricklayer = RtMon("Bricklayer of the Eleventh Hour", CardRarity.Uncommon, 1, MonsterAttribute.Earth, MonsterType.Human, 900, 1300,
                Fx("One More Course", "When this card is Summoned: seal 1 empty Monster Zone your opponent controls while this card remains face-up.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SealEnemyZonesWhileSourceFaceUp, 1)),
                Inf("Wall Them In", "Pay 2 Mana: seal up to 2 instead.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SealEnemyZonesWhileSourceFaceUp, 2)));
            bricklayer.canSelfSpecialSummon = true;
            bricklayer.selfSummonChecksOpponentField = true;
            bricklayer.selfSummonRequiresOpponentMonsters = 3;
            bricklayer.selfSummonPosition = BattlePosition.Attack;

            var squatter = RtMon("The Squatter, Uninvited", CardRarity.Rare, 2, MonsterAttribute.Wind, MonsterType.Demon, 1500, 1200,
                Fx("Moving In", "If this card was Special Summoned: draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)));
            squatter.effects[0].onlyIfSpecialSummoned = true;
            squatter.canSelfSpecialSummon = true;
            squatter.selfSummonIntoSealedZone = true;
            squatter.selfSummonPosition = BattlePosition.Attack;

            RtSpell("No Room at the Inn", CardRarity.Uncommon, false,
                Fx("No Room at the Inn", "Pay 2 Mana: seal up to 2 empty Monster Zones your opponent controls until the end of your next turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SealEnemyZones, 2)),
                Inf("Not Even the Stable", "Instead, pay 4 Mana: seal up to 3, and draw 1 card.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SealEnemyZones, 3),
                    Act(EffectActionType.DrawCards, 1)));

            RtSpell("Condemned Premises", CardRarity.Rare, true,
                Fx("Condemned Premises", "Pay 2 Mana: seal 1 empty Monster Zone on either side of the field until the end of your next turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SealAnyZones, 1)),
                Inf("Condemned Block", "Instead, pay 3 Mana: seal 2 zones.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SealAnyZones, 2)));

            RtArtifact("The Bricked-Up Door", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Brick by Brick", "Once per turn — pay 1 Mana: seal 1 empty Monster Zone on either side until the end of your next turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SealAnyZones, 1)),
                Inf("Wall Off the Wing", "Pay 2 Mana: seal 2 zones instead.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SealAnyZones, 2)));

            var padlock = RtArtifact("The Landlord's Own Padlock", CardRarity.Rare, ArtifactSlot.Monster, 0, 300,
                Inf("One More Lock", "Pay 2 Mana: seal 1 empty Monster Zone on either side until the end of your next turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SealAnyZones, 1)));
            padlock.passiveSealsAdjacentZones = true;
            padlock.passiveBearerZoneLocked = true;
        }

        // ================== C. DAS OBERSTE GRAB (Friedhofs-Spitze, 7) ==================
        private static void RtFreshGrave()
        {
            RtMon("Gravedigger's First Shift", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Human, 800, 1200,
                Fx("Break Ground", "When this card is Summoned: send the top card of your Deck to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillSelf, 1)),
                Inf("Dig Where Told", "Pay 1 Mana: choose the card yourself — send 1 card from your Deck to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckCardFiltered)));

            var echo = RtMon("Echo of the Latest Loss", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Myth, 500, 500,
                Inf("Silence the Other Grave", "Pay 2 Mana: banish the top card of your opponent's Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BanishOpponentGraveTop, 1)));
            echo.passiveStatsFromGraveTop = true;
            echo.canSelfSpecialSummon = true;
            echo.selfSummonRequiresGraveTopMonster = true;
            echo.selfSummonPosition = BattlePosition.Attack;

            RtMon("He Sleeps Lightly", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Demon, 1600, 1100,
                Fx("A Light Sleeper", "While this card is the top card of your Graveyard — pay 2 Mana: Special Summon it. It cannot attack this turn.",
                    EffectTrigger.GraveyardIgnition, 2, true,
                    Act(EffectActionType.SpecialSummonSelfFromGrave).RtNoAttack()).RtWhileGraveTop());

            RtSpell("Last In, First Out", CardRarity.Uncommon, false,
                Fx("Last In, First Out", "Pay 1 Mana: add the top card of your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnGraveTopToHand, 1)),
                Inf("Double Entry", "Instead, pay 3 Mana: the top 2.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ReturnGraveTopToHand, 2)));

            RtSpell("The Fresh Grave", CardRarity.Rare, false,
                Fx("The Fresh Grave", "Pay 2 Mana: Special Summon the top card of your Graveyard if it is a Level 2 or lower monster.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonGraveTop, 1, level: 2)),
                Inf("The Freshest Grave", "Instead, pay 4 Mana: any Level — it cannot attack this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonGraveTop, 1).RtNoAttack()));

            RtSpell("Buried With His Boots On", CardRarity.Rare, true,
                Fx("Buried With His Boots On", "Pay 2 Mana. If one of your monsters was destroyed this turn: Special Summon the top monster card of your Graveyard face-down.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonGraveTopMonsterFaceDown)).RtAfterOwnLoss(),
                Inf("And His Hat Besides", "Instead, pay 3 Mana: also draw 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonGraveTopMonsterFaceDown),
                    Act(EffectActionType.DrawCards, 1)).RtAfterOwnLoss());

            RtArtifact("The Unquiet Topsoil", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Turn the Soil", "Once per turn: put the top card of your Graveyard on the bottom of your Graveyard.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.MoveGraveTopToBottom)),
                Inf("Fresh Soil", "Pay 2 Mana: send the top card of your Deck to the Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.MillSelf, 1)));
        }

        // ================== D. DIE STUFENSCHMIEDE (Level, 6) ==================
        private static void RtLevelForge()
        {
            RtSpell("A Foot in the Door", CardRarity.Uncommon, false,
                Fx("A Foot in the Door", "Pay 1 Mana: your next Normal Summon this turn requires 1 fewer tribute.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DiscountNextNormalSummon, 1)),
                Inf("Both Feet", "Instead, pay 3 Mana: it requires no tributes.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DiscountNextNormalSummon, 99)));

            RtArtifact("The Promotion Board", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Promoted", "Once per turn — pay 2 Mana: 1 monster you control permanently becomes 1 Level higher (max. Level 3).",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ChangeTargetLevelPermanent, 1, TargetKind.AllyMonster)),
                Inf("Promoted With Honors", "Pay 3 Mana: promote 1 monster, then draw 1 card.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.ChangeTargetLevelPermanent, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            RtSpell("Demoted for Cause", CardRarity.Rare, true,
                Fx("Demoted for Cause", "Pay 2 Mana: 1 monster on the field becomes Level 1 until the End Phase.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetLevelThisTurn, 1, TargetKind.AnyMonster)),
                Inf("Mass Demotion", "Instead, pay 3 Mana: up to 2 monsters.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SetTargetLevelThisTurn, 1, TargetKind.AnyMonster,
                        targetCount: 2, upTo: true)));

            RtSpell("Cut Down to Size", CardRarity.Rare, false,
                Fx("Cut Down to Size", "Pay 2 Mana: destroy 1 monster whose Level is LOWER than the number of monsters its controller controls.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AnyMonster).RtUnderleveled()),
                Inf("Cut Them All Down", "Instead, pay 4 Mana: destroy up to 2 such monsters.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AnyMonster,
                        targetCount: 2, upTo: true).RtUnderleveled()));

            var rung = RtMon("Stuck on the Middle Rung", CardRarity.Rare, 2, MonsterAttribute.Wind, MonsterType.Human, 1400, 1400,
                Inf("Any Rung Will Do", "Pay 2 Mana: this card becomes the Level of your choice (1-3) until the End Phase.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ChooseSelfLevelThisTurn)));
            rung.canSelfSpecialSummon = true;
            rung.selfSummonRequiresLevels1And3 = true;
            rung.selfSummonPosition = BattlePosition.Attack;
            rung.passiveAtkPerCount = 300;
            rung.passiveAtkPerCountKind = EffectCountKind.OwnDistinctLevels;

            var doorman = RtMon("The Overqualified Doorman", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Human, 2100, 2100,
                Fx("Not on the List", "When this card is Summoned: 1 monster your opponent controls becomes Level 1 until the End Phase.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetLevelThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Seen Worse Doors", "Once per turn — pay 2 Mana: this card cannot be destroyed this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectSelfThisTurn)));
            doorman.canSelfSpecialSummon = true;
            doorman.selfSummonRequiresOpponentLevel3AndNoneSelf = true;
            doorman.selfSummonPosition = BattlePosition.Attack;
        }

        // ================== E. MORGIGE NACHRICHTEN (Deck-Spitze & Countdown, 7) ==================
        private static void RtTomorrowsNews()
        {
            RtSpell("The Ink Still Wet", CardRarity.Uncommon, false,
                Fx("The Ink Still Wet", "Pay 1 Mana: look at the top 3 cards of your Deck and put them back in any order.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.LookReorderTopDeck, 3)),
                Inf("Read It Twice", "Instead, pay 2 Mana: reorder them, then draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.LookReorderTopDeck, 3),
                    Act(EffectActionType.DrawCards, 1)));

            RtSpell("The Day After Tomorrow's News", CardRarity.Rare, false,
                Fx("Tomorrow's News", "Pay 2 Mana: look at the top 2 cards of your OPPONENT's Deck and put them back in any order.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.LookReorderOpponentTopDeck, 2)),
                Inf("The Whole Edition", "Instead, pay 4 Mana: the top 3.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.LookReorderOpponentTopDeck, 3)));

            RtSpell("The Self-Fulfilling Prophecy", CardRarity.Rare, true,
                Fx("The Prophecy", "Pay 2 Mana: reveal the top card of your Deck. If it is a Level 2 or lower monster, Special Summon it; otherwise send it to the Graveyard.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.RevealTopDeckSummonIfLowLevel, 1, level: 2)),
                Inf("The Greater Prophecy", "Instead, pay 4 Mana: any Level — a Summoned monster cannot attack this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.RevealTopDeckSummonIfLowLevel, 1).RtNoAttack()));

            var reader = RtMon("She Reads the Weather in Entrails", CardRarity.Uncommon, 1, MonsterAttribute.Dark, MonsterType.Human, 900, 1200,
                Fx("Read the Signs", "When this card is Summoned: reveal the top card of your Deck; you may put it on the bottom.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.RevealTopMayBottom)),
                Inf("Read Their Signs", "Pay 2 Mana: the same for your opponent's Deck.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.RevealOpponentTopDeckMayBottom)));
            reader.canSelfSpecialSummon = true;
            reader.selfSummonRequiresRevealedThisTurn = true;
            reader.selfSummonPosition = BattlePosition.Attack;

            var calendar = RtMon("The Calendar's Last Page", CardRarity.Legendary, 3, MonsterAttribute.Dark, MonsterType.Myth, 2200, 1800,
                Fx("The Last Page Turns", "When this card is Summoned: draw 2 cards, then put 1 card from your hand on the bottom of your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.PutTargetHandCardToDeckBottom, 1, TargetKind.HandCardSelf)),
                Inf("Tear Out Tomorrow", "Pay 3 Mana: reveal the top 2 cards of your Deck; add all monsters among them to your hand.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.RevealTopDeckTakeMonsters, 2)));
            calendar.canSelfSpecialSummon = true;
            calendar.selfSummonRequiresTurnAtLeast = 7;
            calendar.selfSummonPosition = BattlePosition.Attack;

            var hour = RtArtifact("The Appointed Hour", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("The Hour Strikes", "When the last Hour Counter is removed: draw 2 cards, gain 2 Mana this turn, and return up to 1 card your opponent controls to their hand. Then destroy this card.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainMana, 2),
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemyCardOnField, upTo: true),
                    Act(EffectActionType.SendSelfToGraveyard)).Mand(),
                Inf("Wind the Clock Forward", "Once per turn — pay 2 Mana: remove 1 additional Hour Counter.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.TickCountdownSelf, 1)));
            hour.countdownMarkers = 3;

            RtSpell("Ink for the Third Edition", CardRarity.Uncommon, true,
                Fx("Ink for the Third Edition", "Pay 1 Mana: put 1 card from your hand on top of your Deck; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.PutTargetHandCardOnTopOfDeck, 1, TargetKind.HandCardSelf),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Proofread", "Instead, pay 2 Mana: afterwards reveal the top card of your Deck; you may put it on the bottom.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.PutTargetHandCardOnTopOfDeck, 1, TargetKind.HandCardSelf),
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.RevealTopMayBottom)));
        }

        // ================== F. SCHILDKANTE VORAN (DEF-Angriff, 4) ==================
        private static void RtShieldFirst()
        {
            var shoulder = RtMon("He Who Leads With His Shoulder", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 400, 1900,
                Inf("Put the Weight In", "Pay 2 Mana: this card gains 300 DEF until the end of the turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 300, TargetKind.SelfCard)));
            shoulder.passiveAttacksWithDef = true;

            var doorframe = RtMon("The Vault's Own Doorframe", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Mecha, 0, 2600,
                Inf("Brace the Wing", "Pay 3 Mana: your monsters adjacent to this card gain 400 DEF until the end of the turn.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 400, TargetKind.AdjacentAllyMonsters)));
            doorframe.passiveAttacksWithDef = true;
            doorframe.passiveNoAttackOnSummonTurn = true;
            doorframe.canSelfSpecialSummon = true;
            doorframe.selfSummonRequiresArtifacts = 2;
            doorframe.selfSummonPosition = BattlePosition.Defense;

            var doorstop = RtMon("Doorstop Made of Dragon Bone", CardRarity.Uncommon, 1, MonsterAttribute.Fire, MonsterType.Dragon, 0, 1500,
                Inf("Splint the Crack", "Once per turn — pay 1 Mana: this card permanently gains 300 DEF.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetDef, 300, TargetKind.SelfCard)));
            doorstop.passiveAttacksWithDef = true;
            doorstop.passiveDefLossAfterAttack = 300;

            RtSpell("Lead With the Shield", CardRarity.Rare, false,
                Fx("Lead With the Shield", "Pay 1 Mana: 1 monster you control attacks using its DEF until the end of the turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.GrantAttacksWithDefThisTurn, 0, TargetKind.AllyMonster)),
                Inf("Shield Wall Advance", "Instead, pay 3 Mana: up to 3 monsters — they also gain 200 DEF until the end of the turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.GrantAttacksWithDefThisTurn, 200, TargetKind.AllyMonster,
                        targetCount: 3, upTo: true)));
        }

        // ================== G. OFFENE HAND (Reveal, 4) ==================
        private static void RtOpenHand()
        {
            RtSpell("An Honest Man's Bluff", CardRarity.Uncommon, false,
                Fx("An Honest Man's Bluff", "Pay 1 Mana: reveal your hand. If it holds no Spell, draw 2 cards; otherwise draw 1.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.RevealOwnHandDrawByContent, 1)),
                Inf("Nothing Up the Sleeve", "Instead, pay 2 Mana: afterwards you may put 1 card from your hand on the bottom of your Deck.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.RevealOwnHandDrawByContent, 1),
                    Act(EffectActionType.PutTargetHandCardToDeckBottom, 1, TargetKind.HandCardSelf, upTo: true)));

            var beggar = RtMon("The Beggar Who Shows His Purse", CardRarity.Uncommon, 1, MonsterAttribute.Wind, MonsterType.Human, 1000, 1000,
                Fx("An Empty Purse", "When this card is Summoned: reveal your hand. If it is empty, draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.RevealOwnHandDrawIfEmpty, 2)),
                Inf("Show Me Yours", "Pay 1 Mana: your opponent reveals 1 random card from their hand.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.OpponentRevealsRandomHandCard, 1)));
            beggar.canSelfSpecialSummon = true;
            beggar.selfSummonRequiresHandAtMost = 2;
            beggar.selfSummonPosition = BattlePosition.Attack;

            RtMon("The Transparent Man", CardRarity.Rare, 2, MonsterAttribute.Light, MonsterType.Myth, 1200, 1600,
                Fx("Nothing to Hide", "When this card is Summoned: reveal your hand; this card permanently gains 200 ATK for each monster revealed.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.RevealOwnHandBuffPerMonster, 200)),
                Inf("Look Right Through", "Pay 2 Mana: this card cannot be targeted by your opponent's effects this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CannotBeTargetedThisTurn, 1, TargetKind.SelfCard)));

            RtSpell("Everything Above Board", CardRarity.Rare, true,
                Fx("Everything Above Board", "Pay 2 Mana: both players reveal their hands. Draw 1 card if your opponent holds more cards than you.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.BothRevealHandsDrawIfOpponentMore, 1)),
                Inf("Your Side of the Table", "Instead, pay 3 Mana: only your OPPONENT reveals — same draw.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.OpponentRevealsHandDrawIfMore, 1)));
        }

        // ================== H. EINZELSTÜCKE (11) ==================
        private static void RtSingles()
        {
            var thousandth = RtMon("The Thousandth Card", CardRarity.Legendary, 1, MonsterAttribute.Light, MonsterType.Myth, 1000, 1000,
                Fx("One Among a Thousand", "When this card is Summoned: draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Back Into the Thousand", "Once per Duel — pay 1 Mana: shuffle this card into your Deck; draw 2 cards.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.SelfCard),
                    Act(EffectActionType.DrawCards, 2)).OncePerDuel());
            thousandth.canSelfSpecialSummon = true;
            thousandth.selfSummonRequiresDeckAtLeast = 40;
            thousandth.selfSummonPosition = BattlePosition.Attack;

            RtSpell("Countersign", CardRarity.Rare, true,
                Fx("Countersign", "Pay 2 Mana: the next Spell your opponent activates this turn costs 2 more Mana.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.TaxOpponentNextSpellThisTurn, 2)),
                Inf("Notarized", "Instead, pay 3 Mana: it costs 3 more, and you draw 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.TaxOpponentNextSpellThisTurn, 3),
                    Act(EffectActionType.DrawCards, 1)));

            RtSpell("Eviction Notice", CardRarity.Uncommon, false,
                Fx("Eviction Notice", "Pay 1 Mana: return 1 monster that cannot attack to its owner's hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster).RtEvictable()),
                Inf("Clear the Building", "Instead, pay 3 Mana: up to 2 such monsters.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster,
                        targetCount: 2, upTo: true).RtEvictable()));

            RtSpell("Wrong Queue, Sir", CardRarity.Rare, true,
                Fx("Wrong Queue, Sir", "Pay 2 Mana: move 1 monster your opponent controls to another empty Monster Zone on their side (your choice).",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.MoveEnemyTargetToZone, 1, TargetKind.EnemyMonster)),
                Inf("Everyone Out of Line", "Instead, pay 3 Mana: move up to 2.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.MoveEnemyTargetToZone, 1, TargetKind.EnemyMonster,
                        targetCount: 2, upTo: true)));

            RtSpell("The Turntable", CardRarity.Rare, false,
                Fx("The Turntable", "Pay 2 Mana: shift ALL your monsters one zone to the left or right (monsters with no room stay).",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.RotateOwnMonsters, 0)),
                Inf("Spin It Again", "Instead, pay 3 Mana: draw 1 card if 3 or more monsters moved.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.RotateOwnMonsters, 1)));

            RtSpell("Settle the Difference", CardRarity.Legendary, true,
                Fx("Settle the Difference", "Once per Duel. Pay 3 Mana: both players' LP become the LOWER of the two totals.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.SetBothLifeToLower)).OncePerDuel(),
                Inf("Keep the Change", "Instead, pay 5 Mana: afterwards you gain 1000 LP.",
                    EffectTrigger.OnActivate, 5, true,
                    Act(EffectActionType.SetBothLifeToLower),
                    Act(EffectActionType.HealSelf, 1000)).OncePerDuel());

            var scales = RtMon("The Even Scales", CardRarity.Rare, 2, MonsterAttribute.Light, MonsterType.Myth, 1500, 1500,
                Inf("Tip Toward Me", "Pay 2 Mana: gain LP equal to half the difference between both players' LP (max. 1000).",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.HealHalfLpDifference, 1000)));
            scales.canSelfSpecialSummon = true;
            scales.selfSummonRequiresLpWithin = 500;
            scales.selfSummonPosition = BattlePosition.Attack;
            scales.passiveUntargetableWhileLpClose = 500;

            RtSpell("First Mover's Advantage", CardRarity.Uncommon, false,
                Fx("First Mover's Advantage", "Only during your FIRST turn of the Duel — pay 1 Mana: gain 2 Mana this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.GainMana, 2)).RtFirstTurnOnly(),
                Inf("Tempo Thief", "Instead, pay 2 Mana: also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.GainMana, 2),
                    Act(EffectActionType.DrawCards, 1)).RtFirstTurnOnly());

            var order = RtArtifact("The Standing Order", CardRarity.Rare, ArtifactSlot.Player, 0, 0,
                Inf("Overdraft", "Once per turn — pay 2 Mana: send the top card of your Deck to the Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.MillSelf, 1)));
            order.passiveDrawReplacementGraveTop = true;

            RtSpell("Two Truths and a Lie", CardRarity.Rare, false,
                Fx("Two Truths and a Lie", "Pay 2 Mana: Set up to 3 monsters from your hand face-down in Defense Position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetMonstersFromHandFaceDown, 0, TargetKind.HandMonsterFiltered,
                        targetCount: 3, upTo: true)),
                Inf("And a Wink", "Instead, pay 3 Mana: draw 1 card for each monster Set beyond the first.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SetTargetMonstersFromHandFaceDown, 1, TargetKind.HandMonsterFiltered,
                        targetCount: 3, upTo: true)));

            RtSpell("Making Ends Meet", CardRarity.Uncommon, false,
                Fx("Making Ends Meet", "Pay 1 Mana. If you control 1 or fewer monsters: gain 2 Mana this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.GainMana, 2)).RtMaxMonsters(1),
                Inf("Stretch It Further", "Instead, pay 2 Mana: also draw 1 card if your hand then holds 3 or fewer cards.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.GainMana, 2),
                    Act(EffectActionType.DrawIfHandAtMost, 3)).RtMaxMonsters(1));
        }
    }
}
