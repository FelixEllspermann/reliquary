using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // 6er-Welle Teil 3 — Paperbound bis Wyldpack.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build Wave Six — 3 (Paperbound–Wyldpack)")]
        public static void BuildWaveSix3()
        {
            built.Clear();
            W6Paperbound(); W6Powderkeg(); W6Redactor(); W6Sacrilegion(); W6Sleightwind();
            W6Slowburn(); W6Snugglet(); W6Tidebound(); W6Trapline(); W6Wyldpack();
            Finish("WaveSix 3");
        }

        private static void W6Paperbound()
        {
            Mon("Paperbound Notary", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 700, 1300,
                Fx("Notarized Delay", "When this card is Summoned: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Sealed in Triplicate", "Instead, pay 1 Mana: It also cannot change its battle position this turn.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)));

            var department = Mon("Paperbound Head of Department", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2200, 2500,
                Fx("Application Denied", "When this card is Summoned: Your opponent cannot Special Summon for the rest of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OpponentSummonLockThisTurn)),
                Inf("Sit Down and Wait", "Pay 2 Mana: Change 1 monster your opponent controls to Defense Position; it cannot change its position this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)));
            department.canSelfSpecialSummon = true;
            department.selfSummonChecksOpponentField = true;
            department.selfSummonRequiresOpponentMonsters = 2;

            Spell("Paperbound Filing Deadline", CardRarity.Uncommon, true,
                Fx("Missed the Deadline", "When your opponent Summons a monster: it cannot attack this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)).InWindow(QuickWindow.SummonResponse),
                Inf("Rejected Outright", "Instead, pay 3 Mana: Also change it to Defense Position; it cannot change its position this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)).InWindow(QuickWindow.SummonResponse));

            Spell("Paperbound Cease and Desist", CardRarity.Rare, false,
                Fx("Cease and Desist", "Negate the effects of 1 card your opponent controls until the end of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)),
                Inf("Blanket Injunction", "Instead, pay 4 Mana: Up to 2 cards.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField, targetCount: 2, upTo: true)));

            Artifact("Paperbound Records Office", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("File Under D", "Once per turn: Change 1 monster your opponent controls to Defense Position.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster)),
                Inf("Lost in the Archive", "Pay 3 Mana: 1 monster your opponent controls cannot attack this turn; draw 1 card.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            var bureau = Rel("Paperbound, Supreme Bureau", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Human, 2500, 2800,
                "Your opponent controls 3+ monsters. Cost 3 Mana.", 3,
                Fx("Bureau Ruling", "When this card is Summoned: All your opponent's monsters cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster, targetCount: 5, upTo: true)),
                Inf("Permit Revoked", "Pay 2 Mana: Your opponent cannot Special Summon this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.OpponentSummonLockThisTurn)));
            bureau.reqOpponentMonstersAtLeast = 3;
        }

        private static void W6Powderkeg()
        {
            Mon("Powderkeg Fusewright", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Mecha, 800, 800,
                Fx("Rewire the Charge", "When this card is Summoned: Place 1 \"Powderkeg\" Artifact from your Graveyard into your Artifact Zone.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Powderkeg")),
                Inf("Fresh Fuse", "Instead, pay 2 Mana: From your Deck; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg"),
                    Act(EffectActionType.DrawCards, 1)));

            var demolitionist = Mon("Powderkeg Demolitionist", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Mecha, 2300, 1800,
                Inf("Controlled Demolition", "Pay 2 Mana: Destroy 1 Artifact you control; destroy 1 Spell or Artifact your opponent controls.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemySpellOrArtifact)));
            demolitionist.canSelfSpecialSummon = true;
            demolitionist.selfSummonRequiresArtifact = true;
            demolitionist.passiveAtkPerCount = 200;
            demolitionist.passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;

            Spell("Powderkeg Chain Reaction", CardRarity.Rare, false,
                Fx("Chain Reaction", "Destroy up to 2 Artifacts you control; destroy up to 2 monsters your opponent controls with 1000 or less ATK.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, targetCount: 2, upTo: true, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1000, targetCount: 2, upTo: true)),
                Inf("Sympathetic Detonation", "Instead, pay 3 Mana: 1600 or less.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, targetCount: 2, upTo: true, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1600, targetCount: 2, upTo: true)));

            Spell("Powderkeg Duck and Cover", CardRarity.Uncommon, true,
                Fx("Duck and Cover", "Destroy 1 Artifact you control; 1 monster you control cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster)),
                Inf("Behind the Sandbags", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Powderkeg Munitions Dump", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Stocked Deep", "When this card is destroyed: Place 1 \"Powderkeg\" Artifact from your Deck into your Artifact Zone.",
                    EffectTrigger.OnDestroyedSelf, 0, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg", upTo: true)),
                Inf("Crack a Crate", "Once per turn — pay 1 Mana: Destroy 1 other Artifact you control; draw 1 card.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, excludeSelf: true, isCost: true),
                    Act(EffectActionType.DrawCards, 1)));

            var zeroHour = Rel("Powderkeg, Zero Hour", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Mecha, 2900, 2000,
                "4+ Artifacts in your Graveyard. Cost 3 Mana.", 3,
                Fx("Load Everything", "When this card is Summoned: Place up to 2 \"Powderkeg\" Artifacts from your Graveyard into your Artifact Zone.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Powderkeg", targetCount: 2, upTo: true)),
                Fx("Fire at Will", "Destroy 1 Artifact you control; destroy 1 monster your opponent controls with 1500 or less ATK.",
                    EffectTrigger.Quick, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500)));
            zeroHour.reqOwnArtifactsInGrave = 4;
        }

        private static void W6Redactor()
        {
            Mon("Redactor Proofreader", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 800, 1000,
                Fx("Every Word Costs", "Once per turn, when your opponent draws outside their Draw Phase: Gain 1 Mana.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.GainMana, 1)),
                Inf("Line-Item Fee", "Instead, pay 1 Mana: Also draw 1 card.",
                    EffectTrigger.OnOpponentDraw, 1, true,
                    Act(EffectActionType.GainMana, 1),
                    Act(EffectActionType.DrawCards, 1)));

            var censor = Mon("Redactor Chief Censor", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2400, 2000,
                Fx("Struck Passages", "Once per turn, when your opponent draws outside their Draw Phase: Send the top 2 cards of their Deck to the Graveyard.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.MillOpponent, 2)),
                Inf("Complimentary Copy", "Pay 2 Mana: Your opponent draws 1 card.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.OpponentDraws, 1)));
            censor.canSelfSpecialSummon = true;
            censor.selfSummonChecksOpponentField = true;
            censor.selfSummonRequiresOpponentMonsters = 2;

            Spell("Redactor Errata Slip", CardRarity.Uncommon, false,
                Fx("Errata Slip", "Your opponent draws 1 card; you draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.OpponentDraws, 1),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Extended Correction", "Instead, pay 2 Mana: You draw 2 instead.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.OpponentDraws, 1),
                    Act(EffectActionType.DrawCards, 2)));

            Spell("Redactor Struck from Record", CardRarity.Rare, true,
                Fx("Struck from Record", "Banish up to 2 cards from your opponent's Graveyard; draw 1 card.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent, targetCount: 2, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Purged Edition", "Instead, pay 3 Mana: Up to 4.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent, targetCount: 4, upTo: true),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Redactor Black Vault", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Into the Vault", "Once per turn, when your opponent draws outside their Draw Phase: They discard 1 random card.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.DiscardOpponentRandom, 1)),
                Inf("Vault Overflow", "Pay 2 Mana: Send the top 2 cards of your opponent's Deck to the Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.MillOpponent, 2)));

            var unprinted = Rel("Redactor, the Unprinted Truth", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Human, 2800, 2400,
                "Your opponent's Graveyard holds 6+ cards. Cost 3 Mana.", 3,
                Fx("Forced Reading", "When this card is Summoned: Your opponent draws 2 cards, then discards 2 random cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OpponentDraws, 2),
                    Act(EffectActionType.DiscardOpponentRandom, 2)),
                Inf("Ink Runs Dry", "Pay 2 Mana, when your opponent draws outside their Draw Phase: 1 monster they control permanently loses 400 ATK.",
                    EffectTrigger.OnOpponentDraw, 2, true,
                    Act(EffectActionType.DebuffTargetAtk, 400, TargetKind.EnemyMonster)));
            unprinted.reqOpponentGraveyardAtLeast = 6;
        }

        private static void W6Sacrilegion()
        {
            Mon("Sacrilegion Altar Boy", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Dragon, 600, 900,
                Fx("Willing Service", "When this card is Tributed: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.OnTributedSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Light, upTo: true)),
                Inf("Errand of Faith", "Pay 1 Mana and send this card from your hand to the Graveyard: add 1 \"Sacrilegion\" card from your Deck to your hand.",
                    EffectTrigger.HandIgnition, 1, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Sacrilegion")));

            var sacrist = Mon("Sacrilegion Grand Sacrist", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Dragon, 2300, 2100,
                Fx("Gathered Relics", "When this card is Summoned: Add 1 \"Sacrilegion\" card from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Sacrilegion", upTo: true)));
            sacrist.canSelfSpecialSummon = true;
            sacrist.selfSummonRequiresGraveNamedCount = 4;
            sacrist.tributeWorth = 2;

            Spell("Sacrilegion Mass Offering", CardRarity.Uncommon, false,
                Fx("Mass Offering", "Special Summon up to 2 Level 1 LIGHT monsters from your Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Light, targetCount: 2, upTo: true)),
                Inf("High Mass", "Instead, pay 3 Mana: Up to 3.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Light, targetCount: 3, upTo: true)));

            Spell("Sacrilegion Divine Exchange", CardRarity.Rare, true,
                Fx("Divine Exchange", "Destroy 1 monster you control; Special Summon 1 Level 2 LIGHT monster from your Graveyard.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 2, attribute: MonsterAttribute.Light)),
                Inf("Favorable Terms", "Instead, pay 3 Mana: Also gain 2 Mana.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 2, attribute: MonsterAttribute.Light),
                    Act(EffectActionType.GainMana, 2)));

            Artifact("Sacrilegion Ossuary Altar", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Every Bone Counted", "Once per turn, when a monster you control is Tributed: Draw 1 card.",
                    EffectTrigger.OnOwnMonsterTributed, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Raise the Faithful", "Pay 2 Mana: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Light)));

            var communion = Rel("Sacrilegion, Communion of Bone", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Dragon, 2900, 2500,
                "Tribute 1 monster you control and 1 your opponent controls. 6+ cards in your Graveyard. Cost 4 Mana.", 4,
                Fx("Communion", "When this card is Summoned: Special Summon up to 2 Level 1 LIGHT monsters from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Light, targetCount: 2, upTo: true)),
                Inf("Final Rite", "Pay 3 Mana and tribute 1 other monster you control: destroy 1 monster your opponent controls.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, excludeSelf: true, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster)));
            communion.costTributeOwnMonsters = 1;
            communion.costTributeOpponentMonsters = 1;
            communion.reqGraveyardAtLeast = 6;
        }

        private static void W6Sleightwind()
        {
            Mon("Sleightwind Palmist", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Demon, 800, 800,
                Fx("Palm and Switch", "Once per turn, during either player's turn: Discard this card from your hand; draw 1 card.",
                    EffectTrigger.HandQuick, 0, true,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DrawCards, 1)),
                Fx("Read the Palm", "When this card is Summoned: Add 1 Level 1 WIND monster from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Wind, upTo: true)));

            var curtainjumper = Mon("Sleightwind Curtainjumper", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Demon, 2200, 1900,
                Fx("Jump Scare", "Once per turn, during either player's turn: Discard this card from your hand; 1 monster your opponent controls loses 500 ATK until the end of this turn.",
                    EffectTrigger.HandQuick, 0, true,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -500, TargetKind.EnemyMonster)),
                Inf("Vanish Them Instead", "Pay 2 Mana: Return 1 monster your opponent controls with 1500 or less ATK to its owner's hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster, maxAtk: 1500)));
            curtainjumper.canSelfSpecialSummon = true;
            curtainjumper.selfSummonChecksOpponentField = true;
            curtainjumper.selfSummonRequiresOpponentMonsters = 2;

            Spell("Sleightwind Misdirection", CardRarity.Rare, true,
                Fx("Misdirection", "Negate the effects of 1 card on the field until the end of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)),
                Inf("Look Over There", "Instead, pay 3 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Sleightwind Rigged Deck", CardRarity.Uncommon, false,
                Fx("Rigged Deck", "Discard 2 cards; draw 3.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DiscardFromHandCost, 2, isCost: true),
                    Act(EffectActionType.DrawCards, 3)),
                Inf("Stacked Deeper", "Instead, pay 2 Mana: Also gain 1 Mana.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DiscardFromHandCost, 2, isCost: true),
                    Act(EffectActionType.DrawCards, 3),
                    Act(EffectActionType.GainMana, 1)));

            var falseBottom = Artifact("Sleightwind False Bottom", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Double Lining", "Once per turn — pay 1 Mana: Discard 1 card; draw 1 card.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.DiscardFromHandCost, 1, isCost: true),
                    Act(EffectActionType.DrawCards, 1)));
            falseBottom.auraAtkBonus = 200;
            falseBottom.auraNameFilter = "Sleightwind";

            var perfectTrick = Rel("Sleightwind, the Perfect Trick", CardRarity.Legendary, 3,
                MonsterAttribute.Wind, MonsterType.Demon, 2700, 2300,
                "5+ cards in your Graveyard and your opponent controls more monsters than you. Cost 3 Mana.", 3,
                Fx("The Prestige", "When this card is Summoned: Return up to 2 monsters your opponent controls with 2000 or less ATK to their owner's hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster, maxAtk: 2000, targetCount: 2, upTo: true)),
                Inf("Encore from the Sleeve", "Pay 2 Mana: Add 1 monster from your Graveyard to your hand.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf)));
            perfectTrick.reqGraveyardAtLeast = 5;
            perfectTrick.reqOpponentMoreMonsters = true;
        }

        private static void W6Slowburn()
        {
            Mon("Slowburn Matchseller", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Human, 700, 900,
                Fx("A Match for Everyone", "When this card is Summoned: Set 1 \"Slowburn\" Spell from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered, nameFilter: "Slowburn")),
                Inf("Last Matchstick", "Pay 2 Mana and banish this card from your Graveyard: return 1 \"Slowburn\" Spell from your Graveyard to your hand.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Slowburn")));

            var emberlord = Mon("Slowburn Emberlord", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Human, 2200, 1800,
                Inf("Light It Early", "Once per turn — pay 3 Mana: Trigger the CHARGED effect of 1 of your set \"Slowburn\" Spells that was set before this turn.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DetonateChargedSpell)));
            emberlord.canSelfSpecialSummon = true;
            emberlord.selfSummonRequiresFaceDownOnField = true;
            emberlord.auraDefBonus = 300;
            emberlord.auraNameFilter = "Slowburn";
            emberlord.auraExcludesSelf = true;

            Spell("Slowburn: Long Fuse", CardRarity.Uncommon, true,
                Fx("Short End", "Draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DrawCards, 1)),
                Fx("Long End (Charged)", "CHARGED (auto in your Standby Phase): Add 1 \"Slowburn\" card from your Deck to your hand; gain 2 Mana.",
                    EffectTrigger.ChargedStandby, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Slowburn"),
                    Act(EffectActionType.GainMana, 2)));

            Spell("Slowburn: Powder Trail", CardRarity.Rare, true,
                Fx("Thin Trail", "1 monster your opponent controls loses 300 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -300, TargetKind.EnemyMonster)),
                Fx("Trail's End (Charged)", "CHARGED (auto in your Standby Phase): Destroy 1 monster your opponent controls; 1 other monster they control permanently loses 500 ATK.",
                    EffectTrigger.ChargedStandby, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster, upTo: true)));

            Artifact("Slowburn Fire Watch", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Night Watch", "During your Standby Phase: You can Set 1 \"Slowburn\" Spell from your hand.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.SetTargetSpellFromHand, 1, TargetKind.HandSpellFiltered, nameFilter: "Slowburn", upTo: true)),
                Inf("Stir the Coals", "Pay 2 Mana: Return 1 \"Slowburn\" card from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Slowburn")));

            var spark = Rel("Slowburn, the Inevitable Spark", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Human, 2500, 2100,
                "3+ Spells in your Graveyard. Cost 3 Mana.", 3,
                Fx("Lay the Lines", "When this card is Summoned: Set up to 2 \"Slowburn\" Spells from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered, nameFilter: "Slowburn", targetCount: 2, upTo: true)),
                Inf("It Was Always Lit", "Once per turn — pay 2 Mana: Trigger the CHARGED effect of 1 of your set \"Slowburn\" Spells that was set before this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DetonateChargedSpell)));
            spark.reqGraveyardSpellsAtLeast = 3;
        }

        private static void W6Snugglet()
        {
            var nightlight = Mon("Snugglet Nightlight", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Beast, 500, 1000,
                Fx("Glow for Friends", "When this card is Summoned: Add 1 \"Snugglet\" monster from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, nameFilter: "Snugglet")));
            nightlight.auraDefBonus = 400;
            nightlight.auraNameFilter = "Snugglet Nightcap";
            nightlight.fieldLimitName = "Snugglet";
            nightlight.fieldLimitCount = 3;

            var nightcap = Mon("Snugglet Nightcap", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 900, 500,
                Fx("Room for One More", "When this card is Summoned: You can Special Summon 1 \"Snugglet\" monster from your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, nameFilter: "Snugglet", upTo: true)));
            nightcap.auraAtkBonus = 400;
            nightcap.auraNameFilter = "Snugglet Nightlight";
            nightcap.fieldLimitName = "Snugglet";
            nightcap.fieldLimitCount = 3;

            Spell("Snugglet Story Time", CardRarity.Uncommon, false,
                Fx("Story Time", "Special Summon up to 2 \"Snugglet\" monsters from your Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Snugglet", targetCount: 2, upTo: true)),
                Inf("One More Chapter", "Instead, pay 2 Mana: They gain 300 DEF permanently.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Snugglet", targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffTargetDef, 300, TargetKind.AllyMonster, nameFilter: "Snugglet", targetCount: 2, upTo: true)));

            Spell("Snugglet Group Hug", CardRarity.Uncommon, true,
                Fx("Group Hug", "Up to 3 \"Snugglet\" monsters you control cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Snugglet", targetCount: 3, upTo: true)),
                Inf("Squeeze Tighter", "Instead, pay 2 Mana: They also gain 400 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Snugglet", targetCount: 3, upTo: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.AllyMonster, nameFilter: "Snugglet", targetCount: 3, upTo: true)));

            var throne = Artifact("Snugglet Beanbag Throne", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Royal Summons", "Once per turn — pay 2 Mana: Add 1 \"Snugglet\" card from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Snugglet")));
            throne.auraAtkBonus = 200;
            throne.auraDefBonus = 200;
            throne.auraNameFilter = "Snugglet";

            var bigSpoon = Rel("Snugglet, Big Spoon", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Beast, 2800, 2900,
                "You control 3 \"Snugglet\" monsters. Cost 3 Mana; Tribute 1 monster you control.", 3,
                Fx("Everyone In", "When this card is Summoned: Special Summon 1 \"Snugglet\" monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Snugglet", upTo: true)),
                Inf("Hold Them Close", "Pay 2 Mana: 1 \"Snugglet\" monster you control cannot be destroyed this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Snugglet")));
            bigSpoon.reqNamedOnField = "Snugglet";
            bigSpoon.reqNamedCount = 3;
            bigSpoon.costTributeOwnMonsters = 1;
            bigSpoon.protectsNamedFromTargeting = "Snugglet";
        }

        private static void W6Tidebound()
        {
            Mon("Tidebound Tidepooler", CardRarity.Common, 1, MonsterAttribute.Water, MonsterType.Myth, 800, 1000,
                Fx("Pool the Catch", "When this card is Summoned: You can return 1 other monster you control to your hand; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, excludeSelf: true, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Slip Away", "Pay 2 Mana: Return this card to your hand; 1 monster your opponent controls loses 400 ATK until the end of this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.SelfCard),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -400, TargetKind.EnemyMonster)));

            var deepcaller = Mon("Tidebound Deepcaller", CardRarity.Rare, 3, MonsterAttribute.Water, MonsterType.Myth, 2200, 2100,
                Fx("Call It Under", "When this card is Summoned: Return 1 Spell or Artifact your opponent controls to the hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact)),
                Inf("Deeper Still", "Instead, pay 3 Mana: Also return 1 monster with 1500 or less ATK.",
                    EffectTrigger.OnSummonSelf, 3, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster, maxAtk: 1500)));
            deepcaller.canSelfSpecialSummon = true;
            deepcaller.selfSummonChecksOpponentField = true;
            deepcaller.selfSummonRequiresOpponentMonsters = 2;

            Spell("Tidebound Riptide", CardRarity.Rare, true,
                Fx("Riptide", "Return up to 2 monsters with 1200 or less ATK to their owners' hands.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, maxAtk: 1200, targetCount: 2, upTo: true)),
                Inf("Undertow Rising", "Instead, pay 4 Mana: Any ATK.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, targetCount: 2, upTo: true)));

            Spell("Tidebound Beachhead", CardRarity.Uncommon, false,
                Fx("Beachhead", "Special Summon 1 \"Tidebound\" monster from your hand; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, nameFilter: "Tidebound"),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Second Landing", "Instead, pay 2 Mana: From your Graveyard instead.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Tidebound"),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Tidebound Moon Pull", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Pull of the Moon", "Once per turn, when a card is returned from the field to your opponent's hand: Draw 1 card.",
                    EffectTrigger.OnEnemyCardBounced, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Tide Coming In", "Pay 2 Mana: Return 1 \"Tidebound\" monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Tidebound")));

            var kingTide = Rel("Tidebound, the King Tide", CardRarity.Legendary, 3,
                MonsterAttribute.Water, MonsterType.Myth, 2800, 2600,
                "5+ cards in your Graveyard. Cost 3 Mana.", 3,
                Fx("The King Tide", "When this card is Summoned: Return 1 card your opponent controls to its owner's hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemyCardOnField)),
                Inf("Sweep the Shore", "Pay 3 Mana: Return 1 monster on the field to its owner's hand.",
                    EffectTrigger.Quick, 3, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster)));
            kingTide.reqGraveyardAtLeast = 5;
        }

        private static void W6Trapline()
        {
            Mon("Trapline Scout", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Human, 800, 1200,
                Fx("Walk the Line", "When this card is Summoned: Add 1 \"Trapline\" Spell from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Trapline", upTo: true)),
                Inf("Re-Arm It", "Instead, pay 2 Mana: Set it directly instead (usable this turn).",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetSpellFromGraveyard, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Trapline")));

            var jawsmith = Mon("Trapline Jawsmith", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Human, 2100, 2200,
                Inf("Fresh Teeth", "Once per turn — pay 1 Mana: Set 1 \"Trapline\" Quick Spell from your hand face-down; draw 1 card.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.SetTargetSpellFromHand, 1, TargetKind.HandSpellFiltered, nameFilter: "Trapline"),
                    Act(EffectActionType.DrawCards, 1)));
            jawsmith.canSelfSpecialSummon = true;
            jawsmith.selfSummonRequiresGraveNamedCount = 4;
            jawsmith.passiveDefPerCount = 200;
            jawsmith.passiveDefPerCountKind = EffectCountKind.OwnGraveyardCards;

            Spell("Trapline Snare Wire", CardRarity.Uncommon, true,
                Fx("Snare Wire", "When an attack is declared: the attacking monster loses 600 ATK until the end of this turn; then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -600, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse),
                Inf("Hoisted High", "Instead, pay 3 Mana: Turn the attacker face-down (the attack is cancelled); then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse));

            Spell("Trapline Springloaded", CardRarity.Rare, true,
                Fx("Springloaded", "When your opponent Summons a monster: it permanently loses 800 ATK; then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DebuffTargetAtk, 800, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.SummonResponse),
                Inf("Snapped Shut", "Instead, pay 4 Mana: Destroy it (2000 or less ATK); then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000),
                    SetNextTrap()).InWindow(QuickWindow.SummonResponse));

            Artifact("Trapline Toolshed", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Oil the Springs", "During your Standby Phase: Set 1 \"Trapline\" Quick Spell from your Graveyard face-down.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.SetTargetSpellFromGraveyard, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Trapline", upTo: true)),
                Inf("Check the Lines", "Pay 2 Mana: Draw 1 card.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DrawCards, 1)));

            var silentSeason = Rel("Trapline, the Silent Season", CardRarity.Legendary, 3,
                MonsterAttribute.Earth, MonsterType.Human, 2400, 2600,
                "3+ Spells in your Graveyard. Cost 3 Mana.", 3,
                Fx("Reset Every Snare", "When this card is Summoned: Set up to 2 \"Trapline\" Spells from your Graveyard face-down.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromGraveyard, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Trapline", targetCount: 2, upTo: true)),
                Inf("Season of Silence", "Pay 2 Mana: 1 monster your opponent controls loses 500 ATK until the end of this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -500, TargetKind.EnemyMonster)));
            silentSeason.reqGraveyardSpellsAtLeast = 3;
        }

        private static void W6Wyldpack()
        {
            var denmother = Mon("Wyldpack Denmother", CardRarity.Uncommon, 2, MonsterAttribute.Wind, MonsterType.Beast, 1200, 1700,
                Fx("Gather the Litter", "When this card is Summoned: You can Special Summon 1 Level 1 \"Wyldpack\" monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, nameFilter: "Wyldpack", upTo: true)),
                Inf("The Whole Litter", "Instead, pay 2 Mana: Up to 2.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, nameFilter: "Wyldpack", targetCount: 2, upTo: true)));
            denmother.canSelfSpecialSummon = true;
            denmother.selfSummonRequiresAttribute = true;
            denmother.selfSummonRequiredAttribute = MonsterAttribute.Wind;

            var loneshadow = Mon("Wyldpack Loneshadow", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Beast, 2400, 1600,
                Fx("Running with the Pack", "When this card is Summoned, if you control 3+ monsters: this card gains 400 ATK until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.SelfCard)));
            loneshadow.canSelfSpecialSummon = true;
            loneshadow.selfSummonRequiresNameOnField = "Wyldpack";
            loneshadow.conditionalDoubleAttack = true;
            loneshadow.doubleAttackAttribute = MonsterAttribute.Wind;
            loneshadow.effects[0].minOwnMonsters = 3;

            Spell("Wyldpack Full Moon", CardRarity.Rare, false,
                Fx("Full Moon", "Special Summon up to 2 \"Wyldpack\" monsters from your Graveyard.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Wyldpack", targetCount: 2, upTo: true)),
                Inf("Blood Moon", "Instead, pay 4 Mana: They gain 300 ATK permanently.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Wyldpack", targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.AllyMonster, nameFilter: "Wyldpack", targetCount: 2, upTo: true)));

            Spell("Wyldpack Bare Fangs", CardRarity.Uncommon, true,
                Fx("Bare Fangs", "1 \"Wyldpack\" monster you control gains 500 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster, nameFilter: "Wyldpack")),
                Inf("Every Fang Out", "Instead, pay 2 Mana: Up to 3 gain 400 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.AllyMonster, nameFilter: "Wyldpack", targetCount: 3, upTo: true)));

            var grounds = Artifact("Wyldpack Hunting Grounds", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Track the Fallen", "Once per turn — pay 2 Mana: Add 1 \"Wyldpack\" monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Wyldpack")));
            grounds.auraAtkBonus = 200;
            grounds.auraUseTypeFilter = true;
            grounds.auraTypeFilter = MonsterType.Beast;

            var firstHowl = Rel("Wyldpack, the First Howl", CardRarity.Legendary, 3,
                MonsterAttribute.Wind, MonsterType.Beast, 2900, 2200,
                "You control 3+ \"Wyldpack\" monsters. Cost 3 Mana.", 3,
                Fx("The First Howl", "When this card is Summoned: Special Summon up to 2 Level 1 \"Wyldpack\" monsters from your Graveyard; all \"Wyldpack\" monsters you control gain 300 ATK until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, nameFilter: "Wyldpack", targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, nameFilter: "Wyldpack", targetCount: 5, upTo: true)),
                Inf("Answer the Call", "Pay 3 Mana: 1 \"Wyldpack\" monster you control can attack twice this turn.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.AllyMonster, nameFilter: "Wyldpack")));
            firstHowl.reqNamedOnField = "Wyldpack";
            firstHowl.reqNamedCount = 3;
        }
    }
}
