using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // „5 Archetypes" (Design v2 freigegeben 26.08.2026): Giftwyrm (Zustellung
    // aufs Gegnerfeld), Splithoof (Deals — der Gegner wählt), Waylay (Ambush im
    // Gegnerzug), Bylaw (Dekrete für beide Seiten), Chimekeep (Countdowns).
    // Nutzt die Batch2026Builder-Helfer (partial), idempotent wie alle Stages.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build 5 Archetypes (Giftwyrm, Splithoof, Waylay, Bylaw, Chimekeep)")]
        public static void BuildArchetypes5()
        {
            built.Clear();
            A5Giftwyrm(); A5Splithoof(); A5Waylay(); A5Bylaw(); A5Chimekeep();
            Finish("5 Archetypes");
        }

        // ================== Helfer nur für dieses Set ==================

        private const string A5Version = "0.1.7";

        private static void A5Reset(CardDefinition card)
        {
            RtReset(card);
            card.releaseVersion = A5Version;
            card.passiveServesOriginalOwner = false;
            card.passiveCannotAttackWhileDisloyal = false;
            card.passiveSpellTaxOnController = false;
            card.passiveAttackToll = 0;
            card.passiveAttackTaxBoth = 0;
            card.passiveDrawRevealBoth = false;
            card.passiveMonsterCapBoth = 0;
            card.protectsNamedFromEffectDestroy = "";
            card.passiveDecreesSpareOwner = false;
            card.passiveOwnerRoyaltyManaNextTurn = 0;
            if (card is MonsterCardData monster)
            {
                monster.selfSummonToOpponentField = false;
                monster.selfSummonRequiresOpponentMonsterDestroyedThisTurn = false;
                monster.selfSummonRequiresOwnCountdown = false;
            }
            if (card is ReliquaryCardData reliquary)
            {
                reliquary.reqDealsThisTurn = 0;
                reliquary.reqDealsThisDuel = 0;
                reliquary.reqOpponentNamedCount = 0;
                reliquary.reqOpponentAttackedRecently = false;
                reliquary.reqOwnCountdownCards = 0;
                reliquary.reqGraveyardNamedCount = 0;
                reliquary.reqGraveyardNamed = "";
            }
        }

        private static MonsterCardData A5Mon(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = SpMon(name, rarity, level, attribute, type, atk, def, effects);
            A5Reset(card);
            return card;
        }

        private static SpellCardData A5Spell(string name, CardRarity rarity, bool quick, params EffectDefinition[] effects)
        {
            var card = SpSpell(name, rarity, quick, effects);
            A5Reset(card);
            return card;
        }

        private static ArtifactCardData A5Artifact(string name, CardRarity rarity, ArtifactSlot slot,
            int atkBonus = 0, int defBonus = 0, params EffectDefinition[] effects)
        {
            var card = SpArtifact(name, rarity, slot, atkBonus, defBonus, effects);
            A5Reset(card);
            return card;
        }

        private static ReliquaryCardData A5Rel(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            string summonText, int manaCost, params EffectDefinition[] effects)
        {
            var card = SpRel(name, rarity, level, attribute, type, atk, def, summonText, manaCost, effects);
            A5Reset(card);
            card.passiveNoAttackOnSummonTurn = true; // Standard der neueren Reliquaries
            return card;
        }

        // ---- Feinwürze für die neuen Engine-Felder ----

        private static EffectAction A5Deal(this EffectAction action, string optionA, string optionB)
        { action.dealOptionA = optionA; action.dealOptionB = optionB; return action; }
        private static EffectAction A5A(this EffectAction action) { action.dealGate = DealGate.OptionA; return action; }
        private static EffectAction A5B(this EffectAction action) { action.dealGate = DealGate.OptionB; return action; }

        private static EffectDefinition A5WhileDelivered(this EffectDefinition effect)
        { effect.onlyWhileControlledByOpponent = true; return effect; }
        private static EffectDefinition A5NeedsOppAttack(this EffectDefinition effect)
        { effect.requireOpponentAttackedThisTurn = true; return effect; }
        private static EffectDefinition A5NeedsStrike(this EffectDefinition effect)
        { effect.requireStruckThisTurn = true; return effect; }

        // ================== 1. GIFTWYRM (DARK / Animal) — Zustellung ==================
        private static void A5Giftwyrm()
        {
            var ribbon = A5Mon("Giftwyrm Ribboncoil", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 0, 900,
                Fx("Return to Sender", "If this card is sent to the Graveyard while your opponent controls it: draw 1 card.",
                    EffectTrigger.OnSentToGraveyardSelf, 0, false,
                    Act(EffectActionType.DrawCards, 1)).A5WhileDelivered().Mand());
            ribbon.canSelfSpecialSummon = true;
            ribbon.selfSummonToOpponentField = true;
            ribbon.selfSummonPosition = BattlePosition.Defense;
            ribbon.passiveServesOriginalOwner = true;
            ribbon.passiveCannotAttackWhileDisloyal = true;

            var sweet = A5Mon("Giftwyrm Sweettooth", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 400, 600,
                Fx("Nibbling Sounds", "During the End Phase of this card's controller, while your opponent controls it: you have 1 additional Mana next turn.",
                    EffectTrigger.EndPhase, 0, false,
                    Act(EffectActionType.GainManaNextTurn, 1)).A5WhileDelivered().Mand());
            sweet.canSelfSpecialSummon = true;
            sweet.selfSummonToOpponentField = true;
            sweet.selfSummonPosition = BattlePosition.Defense;
            sweet.passiveServesOriginalOwner = true;
            sweet.passiveCannotAttackWhileDisloyal = true;

            var bow = A5Mon("Giftwyrm Prettybow", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 0, 1200);
            bow.canSelfSpecialSummon = true;
            bow.selfSummonToOpponentField = true;
            bow.selfSummonPosition = BattlePosition.Defense;
            bow.passiveServesOriginalOwner = true;
            bow.passiveCannotAttackWhileDisloyal = true;
            bow.passiveSpellTaxOnController = true;

            var unboxed = A5Mon("Giftwyrm Unboxed", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Animal, 1800, 300,
                Fx("It Hatches", "If this card is sent to the Graveyard while your opponent controls it: you can Special Summon it to YOUR field.",
                    EffectTrigger.OnSentToGraveyardSelf, 0, false,
                    Act(EffectActionType.SpecialSummonSelfFromGrave)).A5WhileDelivered());
            unboxed.canSelfSpecialSummon = true;
            unboxed.selfSummonToOpponentField = true;
            unboxed.selfSummonPosition = BattlePosition.Defense;
            unboxed.passiveServesOriginalOwner = true;
            unboxed.passiveCannotAttackWhileDisloyal = true;

            A5Spell("Giftwyrm Registry", CardRarity.Uncommon, false,
                Fx("On the List", "Pay 2 Mana: Special Summon 1 \"Giftwyrm\" monster from your Deck to your OPPONENT's field.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonTargetToOpponentField, 1, TargetKind.DeckMonsterFiltered,
                        nameFilter: "Giftwyrm").RtInDefense()),
                Inf("The Full Registry", "Instead, pay 4 Mana: 2 with different names.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonTargetToOpponentField, 1, TargetKind.DeckMonsterFiltered,
                        nameFilter: "Giftwyrm", targetCount: 2, excludeSameName: true).RtInDefense()));

            A5Spell("Giftwyrm Thank-You Note", CardRarity.Uncommon, true,
                Fx("Warmest Regards", "Pay 1 Mana: draw 1 card for each \"Giftwyrm\" monster your opponent controls (max. 3).",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DrawPerCount, 1, targetCount: 3, countKind: EffectCountKind.OwnMonstersOnOpponentField)),
                Inf("With Deepest Thanks", "Instead, pay 2 Mana: also add 1 \"Giftwyrm\" card from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DrawPerCount, 1, targetCount: 3, countKind: EffectCountKind.OwnMonstersOnOpponentField),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Giftwyrm")));

            A5Spell("Giftwyrm Molting", CardRarity.Rare, false,
                Fx("The Molting", "Pay 2 Mana: return up to 2 of your \"Giftwyrm\" monsters your opponent controls to YOUR field; they can attack this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReclaimOwnFromOpponentField, 0, targetCount: 2, nameFilter: "Giftwyrm")),
                Inf("Hungry Molting", "Instead, pay 4 Mana: they also gain 500 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ReclaimOwnFromOpponentField, 500, targetCount: 2, nameFilter: "Giftwyrm")));

            var shell = A5Mon("Giftwyrm Hollowshell", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Animal, 1500, 1500,
                Fx("What Was Inside", "When this card is Summoned: 1 monster your opponent controls loses 400 ATK until the end of the turn for each \"Giftwyrm\" he controls.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtkPerCountEot, -400, TargetKind.EnemyMonster,
                        countKind: EffectCountKind.OwnMonstersOnOpponentField)));
            shell.canSelfSpecialSummon = true;
            shell.selfSummonChecksOpponentField = true;
            shell.selfSummonRequiresNameOnField = "Giftwyrm";
            shell.selfSummonPosition = BattlePosition.Attack;

            var guest = A5Rel("Giftwyrm, Ungrateful Guest", CardRarity.Rare, 2,
                MonsterAttribute.Dark, MonsterType.Animal, 1900, 1600,
                "Your opponent controls a \"Giftwyrm\" monster — pay 2 Mana.", 2,
                Fx("A Gift for the Host", "When this card is Summoned: Special Summon 1 \"Giftwyrm\" monster from your Graveyard to your OPPONENT's field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetToOpponentField, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Giftwyrm").RtInDefense()),
                Inf("Overstaying", "Pay 2 Mana: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));
            guest.reqOpponentNamedOnField = "Giftwyrm";

            var hamper = A5Rel("Giftwyrm, the Whole Hamper", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Animal, 2400, 2000,
                "Your opponent controls 2+ \"Giftwyrm\" monsters — pay 3 Mana.", 3,
                Fx("The Hamper Bursts", "When this card is Summoned: return ALL your \"Giftwyrm\" monsters from your opponent's field to YOUR field; they gain 300 ATK until the end of the turn and can attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReclaimOwnFromOpponentField, 300, targetCount: 99, nameFilter: "Giftwyrm")));
            hamper.reqOpponentNamedOnField = "Giftwyrm";
            hamper.reqOpponentNamedCount = 2;
            hamper.passiveNoAttackOnSummonTurn = false; // der Korb platzt — der Boss stürmt mit

            // Balance-Haken: der Boss selbst darf im Beschwörungszug ran, die
            // Rückkehrer sowieso (loyal = kein Disloyal-Verbot mehr).
        }

        // ================== 2. SPLITHOOF (FIRE / Demon) — Deals ==================
        private static void A5Splithoof()
        {
            A5Mon("Splithoof Doorknocker", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Demon, 800, 700,
                Fx("A Knock at the Door", "When this card is Summoned — DEAL, your opponent chooses: they discard 1 random card, OR you draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OfferDeal).A5Deal("You discard 1 random card", "The dealer draws 1 card"),
                    Act(EffectActionType.DiscardOpponentRandom, 1).A5A(),
                    Act(EffectActionType.DrawCards, 1).A5B()));

            A5Mon("Splithoof Pennyweight", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Demon, 600, 1000,
                Fx("Thumb on the Scale", "Once per turn — DEAL, your opponent chooses: you gain 1 Mana this turn, OR you draw 1 card.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.OfferDeal).A5Deal("The dealer gains 1 Mana", "The dealer draws 1 card"),
                    Act(EffectActionType.GainMana, 1).A5A(),
                    Act(EffectActionType.DrawCards, 1).A5B()));

            var ledger = A5Artifact("Splithoof Grinning Ledger", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Open for Business", "EITHER player may activate this once per turn — pay 1 Mana: draw 1 card. If your OPPONENT activates it, you have 1 additional Mana next turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.DrawCards, 1)).EitherSide());
            ledger.passiveOwnerRoyaltyManaNextTurn = 1;

            A5Mon("Splithoof Hagglehorn", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Demon, 1400, 1200,
                Fx("Haggling Hour", "When this card is Summoned — DEAL, your opponent chooses: 1 monster of the DEALER's choice they control loses 500 ATK permanently, OR you Special Summon 1 Level 1 \"Splithoof\" monster from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OfferDeal).A5Deal("A monster of the dealer's choice you control loses 500 ATK permanently", "The dealer summons a Level 1 \"Splithoof\" from their Deck"),
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster).A5A(),
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFiltered,
                        level: 1, nameFilter: "Splithoof").A5B()));

            A5Spell("Splithoof Sign Here", CardRarity.Uncommon, true,
                Fx("Sign Here, Please", "Pay 2 Mana when an opponent's monster attacks — DEAL, your opponent chooses: the attack is called off, OR the attack proceeds and they discard 2 random cards afterwards.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.OfferDeal).A5Deal("The attack is called off", "The attack proceeds — discard 2 random cards"),
                    Act(EffectActionType.CancelAttackTarget, 1, TargetKind.EnemyMonster).A5A(),
                    Act(EffectActionType.DiscardOpponentRandom, 2).A5B()).InWindow(QuickWindow.AttackResponse));

            A5Spell("Splithoof Repossession", CardRarity.Rare, false,
                Fx("Repossession", "Pay 2 Mana: place a Lien of 2 on 1 monster your opponent controls (each of their Standby Phases: pay 2 Mana or it is destroyed); draw 1 card.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.PlaceLienOnTarget, 2, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Compound Interest", "Instead, pay 3 Mana: a Lien of 3.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.PlaceLienOnTarget, 3, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            A5Spell("Splithoof Fiddle Wager", CardRarity.Rare, false,
                Fx("The Fiddle Wager", "Pay 1 Mana: both players reveal the top card of their Deck. The player whose card has the higher Level (non-monsters count as 0) adds theirs to the hand; the other card goes to its owner's Graveyard. On a tie, both stay on top.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.TopDeckWager, 0)),
                Inf("Devil's Own Tune", "Instead, pay 2 Mana: if YOUR card wins, also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.TopDeckWager, 1)));

            var imp = A5Mon("Splithoof Collections Imp", CardRarity.Rare, 2, MonsterAttribute.Fire, MonsterType.Demon, 1600, 900,
                Fx("Payment Overdue", "When this card is Summoned: raise the Lien on 1 monster your opponent controls by 1.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.RaiseLienOnTarget, 1, TargetKind.EnemyMonsterWithLien)));
            imp.canSelfSpecialSummon = true;
            imp.selfSummonRequiresLienOnField = true;
            imp.selfSummonPosition = BattlePosition.Attack;

            var fair = A5Rel("Splithoof, Fair and Square", CardRarity.Rare, 2,
                MonsterAttribute.Fire, MonsterType.Demon, 2000, 1500,
                "A Deal was struck this turn — pay 2 Mana.", 2,
                Fx("Read the Fine Print", "When this card is Summoned — DEAL, your opponent chooses: YOU draw 1 card, OR the strongest monsters of both players permanently swap control.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OfferDeal).A5Deal("The dealer draws 1 card", "The strongest monsters of both players swap control"),
                    Act(EffectActionType.DrawCards, 1).A5A(),
                    Act(EffectActionType.SwapStrongestMonsters).A5B()));
            fair.reqDealsThisTurn = 1;

            var bargain = A5Rel("Splithoof, the Better Bargain", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Demon, 2500, 2100,
                "3+ Deals were struck this Duel — pay 3 Mana.", 3,
                Fx("The Closing Offer", "When this card is Summoned — DEAL, your opponent chooses: you draw 3 cards, OR they send the strongest monster they control to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OfferDeal).A5Deal("The dealer draws 3 cards", "You send your strongest monster to the Graveyard"),
                    Act(EffectActionType.DrawCards, 3).A5A(),
                    Act(EffectActionType.OpponentSendsStrongestToGrave).A5B()),
                Inf("Sweeten the Pot", "Once per turn — pay 2 Mana (Quick) — DEAL, your opponent chooses: 1 monster of your choice gains 700 ATK until the end of the turn, OR you draw 1 card.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.OfferDeal).A5Deal("A monster of the dealer's choice gains 700 ATK this turn", "The dealer draws 1 card"),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 700, TargetKind.AllyMonster).A5A(),
                    Act(EffectActionType.DrawCards, 1).A5B()));
            bargain.reqDealsThisDuel = 3;
        }

        // ================== 3. WAYLAY (WIND / Human) — Ambush ==================
        private static void A5Waylay()
        {
            A5Mon("Waylay Hedgeknife", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Human, 900, 600,
                Fx("Out of the Hedge", "AMBUSH — when your opponent Summons a monster: you can Special Summon this card from your hand. Then 1 monster your opponent controls loses 300 ATK until the end of the turn.",
                    EffectTrigger.HandQuick, 0, false,
                    Act(EffectActionType.SpecialSummonSelfFromHand),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -300, TargetKind.EnemyMonster)).InWindow(QuickWindow.SummonResponse));

            A5Mon("Waylay Roadthorn", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Human, 700, 1100,
                Fx("Thorn in the Road", "AMBUSH — when an opponent's monster attacks: you can Special Summon this card from your hand. Then the attacker loses 500 ATK until the end of the turn.",
                    EffectTrigger.HandQuick, 0, false,
                    Act(EffectActionType.SpecialSummonSelfFromHand),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -500, TargetKind.EnemyMonster)).InWindow(QuickWindow.AttackResponse));

            var tollgate = A5Artifact("Waylay Tollgate", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Inf("Count the Take", "Pay 1 Mana (once per turn): draw 1 card, if your opponent attacked this turn.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.DrawCards, 1)).A5NeedsOppAttack());
            tollgate.passiveAttackToll = 1;

            A5Mon("Waylay Highwayman", CardRarity.Uncommon, 2, MonsterAttribute.Wind, MonsterType.Human, 1500, 1000,
                Fx("Your Purse or Your Pride", "AMBUSH — when an opponent's monster attacks: you can Special Summon this card from your hand. Then draw 1 card.",
                    EffectTrigger.HandQuick, 0, false,
                    Act(EffectActionType.SpecialSummonSelfFromHand),
                    Act(EffectActionType.DrawCards, 1)).InWindow(QuickWindow.AttackResponse),
                Inf("Blocked Road", "Pay 2 Mana: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));

            A5Spell("Stand and Deliver!", CardRarity.Rare, true,
                Fx("Stand and Deliver!", "Pay 2 Mana when an opponent's monster attacks: the attack is called off; draw 1 card.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.CancelAttackTarget, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)).InWindow(QuickWindow.AttackResponse),
                Inf("And the Hat Too", "Instead, pay 3 Mana: the attacker also permanently loses 300 ATK.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.CancelAttackTarget, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DebuffTargetAtk, 300, TargetKind.SameAsPrevious),
                    Act(EffectActionType.DrawCards, 1)).InWindow(QuickWindow.AttackResponse));

            A5Spell("Waylay Mislead", CardRarity.Uncommon, true,
                Fx("False Trail", "Pay 1 Mana: move 1 monster your opponent controls to another empty Monster Zone on their side (your choice); it cannot attack this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MoveEnemyTargetToZone, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.SameAsPrevious)),
                Inf("Deep Woods", "Instead, pay 2 Mana: up to 2 — they cannot attack this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.MoveEnemyTargetToZone, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.SameAsPrevious)));

            var fence = A5Mon("Waylay Campfire Fence", CardRarity.Uncommon, 2, MonsterAttribute.Wind, MonsterType.Human, 1200, 1400,
                Fx("Spoils to Share", "When this card is Summoned: draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)));
            fence.canSelfSpecialSummon = true;
            fence.selfSummonRequiresOpponentMonsterDestroyedThisTurn = true;
            fence.selfSummonPosition = BattlePosition.Attack;

            A5Mon("Waylay Nightparcel", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Human, 2200, 1600,
                Fx("Unscheduled Delivery", "AMBUSH — during your OPPONENT's turn, pay 2 Mana in any response window: Special Summon this card from your hand.",
                    EffectTrigger.HandQuick, 2, false,
                    Act(EffectActionType.SpecialSummonSelfFromHand)).OpponentTurnOnly());

            var bushes = A5Rel("Waylay, First Out of the Bushes", CardRarity.Rare, 2,
                MonsterAttribute.Wind, MonsterType.Human, 1800, 1500,
                "Your opponent attacked this or last turn — pay 2 Mana.", 2,
                Fx("Divide the Spoils", "When this card is Summoned: add 1 \"Waylay\" monster from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Waylay")));
            bushes.reqOpponentAttackedRecently = true;
            bushes.auraAtkBonus = 300;
            bushes.auraNameFilter = "Waylay";
            bushes.auraExcludesSelf = true;

            var king = A5Rel("Waylay, King of the Crossroads", CardRarity.Legendary, 3,
                MonsterAttribute.Wind, MonsterType.Human, 2400, 2200,
                "3+ \"Waylay\" cards are in your Graveyard — pay 3 Mana.", 3,
                Fx("Every Road Is Mine", "When this card is Summoned: Special Summon up to 2 \"Waylay\" monsters from your Graveyard; they cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Waylay", targetCount: 2, upTo: true).RtNoAttack()),
                Inf("Ambush at the Crossing", "Pay 2 Mana (Quick, only during your opponent's turn): all face-up monsters your opponent controls lose 400 ATK until the end of the turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DebuffAllEnemyAtkEot, 400)).OpponentTurnOnly());
            king.reqGraveyardNamedCount = 3;
            king.reqGraveyardNamed = "Waylay";
        }

        // ================== 4. BYLAW (EARTH / Human) — Dekrete ==================
        private static void A5Bylaw()
        {
            A5Mon("Bylaw Filing Clerk", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Human, 700, 1000,
                Fx("Proper Channels", "When this card is Summoned: add 1 \"Bylaw:\" Artifact from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Bylaw:")),
                Inf("Expedited Filing", "Pay 2 Mana: place it directly onto the field instead.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Bylaw:")));

            var quiet = A5Artifact("Bylaw: Quiet Hours", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0);
            quiet.passiveAttackTaxBoth = 1;

            var hands = A5Artifact("Bylaw: Show of Hands", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0);
            hands.passiveDrawRevealBoth = true;

            var standing = A5Artifact("Bylaw: Standing Room Only", CardRarity.Rare, ArtifactSlot.Field, 0, 0);
            standing.passiveMonsterCapBoth = 3;

            var enforcer = A5Mon("Bylaw Enforcer", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 1300, 1300);
            enforcer.canSelfSpecialSummon = true;
            enforcer.selfSummonRequiresArtifact = true;
            enforcer.selfSummonPosition = BattlePosition.Attack;
            enforcer.passiveAtkPerCount = 400;
            enforcer.passiveAtkPerCountKind = EffectCountKind.AllArtifactsOnField;

            A5Mon("Bylaw Ombudsman", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 1100, 1600,
                Fx("File a Complaint", "Once per turn — pay 2 Mana (Quick): negate the effects of 1 Artifact on either field until the end of the turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.AnyArtifactOnField)));

            A5Spell("Bylaw Loophole", CardRarity.Rare, true,
                Fx("The Loophole", "Pay 2 Mana: choose 1 \"Bylaw:\" Decree on the field — its rule does not apply to YOU this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ExemptFromDecree, 1, TargetKind.AnyArtifactOnField, nameFilter: "Bylaw:")),
                Inf("Subparagraph 12b", "Instead, pay 3 Mana: up to 2 Decrees.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ExemptFromDecree, 1, TargetKind.AnyArtifactOnField,
                        nameFilter: "Bylaw:", targetCount: 2, upTo: true)));

            A5Spell("Bylaw Red Tape", CardRarity.Uncommon, true,
                Fx("Form B-27", "Pay 1 Mana: the next Spell your opponent activates this turn costs 2 more Mana.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.TaxOpponentNextSpellThisTurn, 2)),
                Inf("In Triplicate", "Instead, pay 2 Mana: … and you draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.TaxOpponentNextSpellThisTurn, 2),
                    Act(EffectActionType.DrawCards, 1)));

            var chairwoman = A5Rel("Bylaw, Acting Chairwoman", CardRarity.Rare, 2,
                MonsterAttribute.Earth, MonsterType.Human, 1700, 1900,
                "You control an Artifact — pay 2 Mana.", 2,
                Fx("Session in Order", "When this card is Summoned: place 1 \"Bylaw:\" Decree from your Deck onto the field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Bylaw:")));
            chairwoman.reqOwnArtifactsOnField = 1;
            chairwoman.protectsNamedFromEffectDestroy = "Bylaw:";

            var letter = A5Rel("Bylaw, the Letter of the Law", CardRarity.Legendary, 3,
                MonsterAttribute.Earth, MonsterType.Human, 2300, 2300,
                "You control 2+ Artifacts — pay 3 Mana.", 3,
                Inf("Motion Carried", "Pay 2 Mana: place 1 \"Bylaw:\" Decree from your Deck onto the field.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Bylaw:")));
            letter.reqOwnArtifactsOnField = 2;
            letter.passiveDecreesSpareOwner = true;
        }

        // ================== 5. CHIMEKEEP (LIGHT / Mecha) — Countdown ==================
        private static void A5Chimekeep()
        {
            var chorister = A5Mon("Chimekeep Windup Chorister", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Mecha, 800, 800,
                Fx("First Verse at Last", "When the last Hour Counter is removed: draw 1 card; this card permanently gains 400 ATK.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.SelfCard)).Mand());
            chorister.countdownMarkers = 2;

            A5Mon("Chimekeep Bellringer", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Mecha, 900, 1000,
                Fx("Ring Ahead", "When this card is Summoned: remove 1 Hour Counter from 1 of your cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.TickCountdownTarget, 1, TargetKind.AllyCountdownCard)),
                Inf("Ring the Changes", "Pay 2 Mana: from up to 2 of your cards, 1 each.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.TickCountdownTarget, 1, TargetKind.AllyCountdownCard,
                        targetCount: 2, upTo: true)));

            var pendulum = A5Mon("Chimekeep Pendulum", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Mecha, 1100, 1400,
                Fx("Keep the Beat", "Once per turn — pay 1 Mana: remove 1 Hour Counter from 1 of your cards.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.TickCountdownTarget, 1, TargetKind.AllyCountdownCard)));
            pendulum.canSelfSpecialSummon = true;
            pendulum.selfSummonRequiresOwnCountdown = true;
            pendulum.selfSummonPosition = BattlePosition.Attack;

            var escapement = A5Artifact("Chimekeep Escapement", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Measured Release", "When the last Hour Counter is removed: add 1 \"Chimekeep\" card from your Deck to your hand; then draw 1 card. Then destroy this card.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Chimekeep"),
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.SendSelfToGraveyard)).Mand(),
                Inf("Hold the Spring", "Once per turn — pay 1 Mana: remove 1 Hour Counter from this card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.TickCountdownSelf, 1)));
            escapement.countdownMarkers = 2;

            var curfew = A5Artifact("Chimekeep Curfew Bell", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Everyone Goes Home", "When the last Hour Counter is removed: return up to 2 cards your opponent controls to their hand. Then destroy this card.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemyCardOnField,
                        targetCount: 2, upTo: true),
                    Act(EffectActionType.SendSelfToGraveyard)).Mand(),
                Inf("Toll Early", "Once per turn — pay 2 Mana: remove 1 additional Hour Counter.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.TickCountdownSelf, 1)));
            curfew.countdownMarkers = 3;

            A5Spell("Chimekeep Overwind", CardRarity.Uncommon, true,
                Fx("Overwind", "Pay 1 Mana: remove 1 Hour Counter from 1 of your cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.TickCountdownTarget, 1, TargetKind.AllyCountdownCard)),
                Inf("Strip the Gears", "Instead, pay 3 Mana: from up to 2 of your cards, 1 each.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.TickCountdownTarget, 1, TargetKind.AllyCountdownCard,
                        targetCount: 2, upTo: true)));

            A5Spell("Chimekeep Chime In", CardRarity.Rare, true,
                Fx("Chime In", "Pay 2 Mana. If one of your cards struck zero this turn: draw 2 cards.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DrawCards, 2)).A5NeedsStrike(),
                Inf("Chime Along", "Instead, pay 3 Mana: … and 1 monster you control gains 500 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster)).A5NeedsStrike());

            var nightround = A5Mon("Chimekeep Night Round", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Mecha, 1500, 1200,
                Fx("Company on the Round", "When the last Hour Counter is removed: Special Summon 1 \"Chimekeep\" monster from your Graveyard; it cannot attack this turn.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Chimekeep").RtNoAttack()).Mand());
            nightround.countdownMarkers = 3;

            var quarter = A5Rel("Chimekeep, Quarter Past Doom", CardRarity.Rare, 2,
                MonsterAttribute.Light, MonsterType.Mecha, 1700, 1700,
                "You control a card with an Hour Counter — pay 2 Mana.", 2,
                Fx("The Quarter Strikes", "When the last Hour Counter is removed: this card permanently gains 800 ATK and can attack twice this Battle Phase.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.BuffTargetAtk, 800, TargetKind.SelfCard),
                    Act(EffectActionType.AttackAgainSelf)).Mand());
            quarter.reqOwnCountdownCards = 1;
            quarter.countdownMarkers = 2;

            var carillon = A5Rel("Chimekeep, the Midnight Carillon", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Mecha, 2300, 2100,
                "You control 2+ cards with Hour Counters — pay 3 Mana.", 3,
                Fx("Midnight, All at Once", "When this card is Summoned: ALL your cards with Hour Counters strike immediately (all counters go to 0; their effects fire in field order).",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.StrikeAllOwnCountdowns)));
            carillon.reqOwnCountdownCards = 2;
        }
    }
}
