using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // 6er-Welle Teil 2 — Genostitched bis Mimicrypt.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build Wave Six — 2 (Genostitched–Mimicrypt)")]
        public static void BuildWaveSix2()
        {
            built.Clear();
            W6Genostitched(); W6Gravemaw(); W6Heavenly(); W6Hexweaver(); W6Kindlekin();
            W6Lightless(); W6Lyria(); W6Manacle(); W6Mechination(); W6Mimicrypt();
            Finish("WaveSix 2");
        }

        private static void W6Genostitched()
        {
            Mon("Genostitched Seamstress", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Mecha, 500, 1200,
                Fx("Thread the Needle", "When this card is Summoned: Add 1 Artifact from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf)),
                Inf("Stitch It On", "Instead, pay 2 Mana: Equip 1 Artifact from your Graveyard to this card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.EquipTargetArtifactToSelf, 1, TargetKind.GraveyardArtifactSelf)));

            var opus = Mon("Genostitched Magnum Opus", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Mecha, 2300, 1900,
                Fx("Self-Assembly", "When this card is Summoned: Equip 1 Artifact from your Graveyard to this card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.EquipTargetArtifactToSelf, 1, TargetKind.GraveyardArtifactSelf, upTo: true)),
                Inf("Untouchable Design", "While equipped with an Artifact, pay 2 Mana: This card cannot be targeted this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.CannotBeTargetedThisTurn, 1, TargetKind.SelfCard)));
            opus.canSelfSpecialSummon = true;
            opus.selfSummonRequiresArtifact = true;
            opus.passiveAtkPerCount = 300;
            opus.passiveAtkPerCountKind = EffectCountKind.EquippedArtifactsOnSelf;
            opus.effects[1].requiresEquippedArtifact = true;

            Spell("Genostitched Field Surgery", CardRarity.Uncommon, true,
                Fx("Field Surgery", "Equip 1 Artifact from your hand to 1 monster you control; it gains 300 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MoveTargetArtifactToStrongestMonster, 0, TargetKind.HandArtifactFiltered),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 300, TargetKind.AllyMonster)),
                Inf("Deep Graft", "Instead, pay 2 Mana: The bonus is permanent.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.MoveTargetArtifactToStrongestMonster, 0, TargetKind.HandArtifactFiltered),
                    Act(EffectActionType.BuffTargetDef, 300, TargetKind.AllyMonster)));

            Spell("Genostitched Donor List", CardRarity.Uncommon, false,
                Fx("Donor List", "Add 1 Artifact from your Deck to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered)),
                Inf("Priority Donor", "Instead, pay 3 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered),
                    Act(EffectActionType.DrawCards, 1)));

            var fourthArm = Artifact("Genostitched Fourth Arm", CardRarity.Rare, ArtifactSlot.Monster, 300, 0,
                Fx("Extra Reach", "Once per turn, when the equipped monster destroys a monster in battle: 1 monster your opponent controls permanently loses 300 ATK.",
                    EffectTrigger.OnBearerBattleKill, 0, true,
                    Act(EffectActionType.DebuffTargetAtk, 300, TargetKind.EnemyMonster)));

            var composite = Rel("Genostitched, the Composite God", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Mecha, 2600, 2400,
                "You control a monster with an equipped Artifact and have 2+ Artifacts in your Graveyard. Cost 3 Mana.", 3,
                Fx("Assembled Divinity", "When this card is Summoned: Equip up to 2 Artifacts from your Graveyard to this card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.EquipTargetArtifactToSelf, 1, TargetKind.GraveyardArtifactSelf, targetCount: 2, upTo: true)),
                Inf("Divine Dissection", "While equipped with an Artifact, pay 3 Mana: Destroy 1 monster your opponent controls with 2000 or less ATK; this card permanently gains 300 ATK.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000),
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.SelfCard)));
            composite.reqMonsterWithEquip = true;
            composite.reqOwnArtifactsInGrave = 2;
            composite.effects[1].requiresEquippedArtifact = true;
        }

        private static void W6Gravemaw()
        {
            Mon("Gravemaw Gnawer", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Beast, 600, 800,
                Fx("First Bite", "When this card is Summoned: Mill 2 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillSelf, 2)),
                Inf("Gnaw Back Up", "Pay 1 Mana and banish 1 other card from your Graveyard: Special Summon this card from your Graveyard.",
                    EffectTrigger.GraveyardIgnition, 1, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, excludeSelf: true, isCost: true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.SelfCard)));

            var sepulcher = Mon("Gravemaw Sepulcher Beast", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Beast, 2400, 1700,
                Fx("Sepulcher Feast", "When this card is Summoned: Banish up to 2 cards from your Graveyard; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Gorging Feast", "Instead, pay 2 Mana: Banish up to 3; draw 1.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, targetCount: 3, upTo: true),
                    Act(EffectActionType.DrawCards, 1)));
            sepulcher.canSelfSpecialSummon = true;
            sepulcher.selfSummonRequiresMilled = true;
            sepulcher.passiveAtkPerCount = 200;
            sepulcher.passiveAtkPerCountKind = EffectCountKind.OwnBanishedMonsters;

            Spell("Gravemaw Second Helping", CardRarity.Uncommon, false,
                Fx("Second Helping", "Return up to 2 of your banished cards to your Graveyard; mill 2 cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.MillSelf, 2)),
                Inf("Third Helping", "Instead, pay 2 Mana: Also Special Summon 1 Level 1 \"Gravemaw\" monster from your Graveyard.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.MillSelf, 2),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, nameFilter: "Gravemaw", upTo: true)));

            Spell("Gravemaw Gulp", CardRarity.Rare, true,
                Fx("Gulp", "Banish 1 monster your opponent controls with 1500 or less ATK.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.BanishTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500)),
                Inf("Swallow Whole", "Instead, pay 4 Mana: No ATK limit.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.BanishTargetMonster, 1, TargetKind.EnemyMonster)));

            Artifact("Gravemaw Feeding Trough", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Keep It Full", "During your Standby Phase: mill 2 cards.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.MillSelf, 2)),
                Inf("Scraps Return", "Once per turn — pay 2 Mana: Return 1 of your banished cards to your Graveyard; gain 1 Mana.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf),
                    Act(EffectActionType.GainMana, 1)));

            var maw = Rel("Gravemaw, Maw of the World", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Beast, 3000, 2100,
                "5+ of your cards are banished. Cost 3 Mana.", 3,
                Fx("The World Returns", "When this card is Summoned: Return up to 3 of your banished cards to your Graveyard; this card permanently gains 200 ATK for each returned card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 3, upTo: true),
                    Act(EffectActionType.BuffTargetAtk, 200, TargetKind.SelfCard)),
                Inf("Bite the World", "Once per turn — pay 3 Mana: Banish 1 card your opponent controls.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.EnemyCardOnField)));
            maw.reqBanishedAtLeast = 5;
        }

        private static void W6Heavenly()
        {
            var vindicator = Mon("Heavenly Vindicator", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Angel, 1600, 1400,
                Fx("Under My Wing", "When this card is Summoned: 1 \"Heavenly\" monster you control cannot be destroyed this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Heavenly")),
                Inf("Lasting Ward", "Instead, pay 2 Mana: It also gains 400 DEF permanently.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Heavenly"),
                    Act(EffectActionType.BuffTargetDef, 400, TargetKind.AllyMonster, nameFilter: "Heavenly")));
            vindicator.canSelfSpecialSummon = true;
            vindicator.selfSummonRequiresAttribute = true;
            vindicator.selfSummonRequiredAttribute = MonsterAttribute.Light;

            var archon = Mon("Heavenly Archon", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Angel, 2500, 2300,
                Fx("Host Descends", "When this card is Summoned: Special Summon 1 Level 1 \"Heavenly\" monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, nameFilter: "Heavenly", upTo: true)),
                Inf("Silencing Light", "Pay 3 Mana: Negate the effects of 1 monster your opponent controls until the end of this turn.",
                    EffectTrigger.Quick, 3, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyMonster)));
            archon.canSelfSpecialSummon = true;
            archon.selfSummonRequiresNameOnField = "Heavenly";
            archon.auraDefBonus = 200;
            archon.auraNameFilter = "Heavenly";
            archon.auraExcludesSelf = true;

            Spell("Heavenly Benediction", CardRarity.Uncommon, true,
                Fx("Benediction", "1 monster you control cannot be destroyed this turn; gain 300 LP.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.HealSelf, 300)),
                Inf("Twin Blessing", "Instead, pay 2 Mana: Up to 2 monsters; gain 500 LP.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, targetCount: 2, upTo: true),
                    Act(EffectActionType.HealSelf, 500)));

            Spell("Heavenly Summons", CardRarity.Rare, false,
                Fx("Heavenly Summons", "Special Summon 1 \"Heavenly\" monster from your Deck with 1400 or less ATK.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFilteredSelf, nameFilter: "Heavenly", maxAtk: 1400)),
                Inf("Highest Summons", "Instead, pay 4 Mana: No ATK limit; it cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFilteredSelf, nameFilter: "Heavenly"),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Heavenly", upTo: true)));

            var altarpiece = Artifact("Heavenly Altarpiece", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Morning Offering", "Pay 1 Mana: Gain 300 LP.",
                    EffectTrigger.StandbyPhase, 1, false,
                    Act(EffectActionType.HealSelf, 300)));
            altarpiece.auraAtkBonus = 200;
            altarpiece.auraDefBonus = 200;
            altarpiece.auraUseTypeFilter = true;
            altarpiece.auraTypeFilter = MonsterType.Angel;

            var choir = Rel("Heavenly Highest Choir", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Angel, 3000, 2600,
                "You control 2+ \"Heavenly\" monsters and have 5+ Mana available. Cost 4 Mana.", 4,
                Fx("The Choir Assembles", "When this card is Summoned: Special Summon up to 2 \"Heavenly\" monsters from your Graveyard; they cannot be destroyed this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Heavenly", targetCount: 2, upTo: true),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Heavenly", targetCount: 2, upTo: true)),
                Inf("Veil of Song", "Pay 2 Mana: 1 \"Heavenly\" monster you control cannot be targeted this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CannotBeTargetedThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Heavenly")));
            choir.reqNamedOnField = "Heavenly";
            choir.reqNamedCount = 2;
            choir.reqMinMana = 5;
        }

        private static void W6Hexweaver()
        {
            Mon("Hexweaver Spindlewitch", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 700, 1000,
                Fx("Spin the First Thread", "When this card is Summoned: Set 1 \"Hexweaver\" Spell from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered, nameFilter: "Hexweaver")),
                Inf("Rewind the Spool", "Pay 2 Mana and banish this card from your Graveyard: return 1 Spell from your Graveyard to your hand.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf)));

            var broker = Mon("Hexweaver Curse Broker", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2000, 2200,
                Fx("Broker's Fee", "When this card is Summoned, if you have 5+ Mana: draw 1 card and gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Sold Short", "Pay 2 Mana: 1 monster your opponent controls permanently loses 400 ATK.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DebuffTargetAtk, 400, TargetKind.EnemyMonster)));
            broker.canSelfSpecialSummon = true;
            broker.selfSummonRequiresFaceDownOnField = true;
            broker.effects[0].minMana = 5;

            Spell("Hexweaver Second Stitch", CardRarity.Uncommon, false,
                Fx("Second Stitch", "Return 1 Quick Spell from your Graveyard to your hand; gain 1 Mana.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Double Stitch", "Instead, pay 2 Mana: Return 2.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.GainMana, 1)));

            Spell("Hexweaver Snip", CardRarity.Rare, true,
                Fx("Snip", "Return 1 Spell or Artifact your opponent controls to the hand; draw 1 card.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Cut from the Weave", "Instead, pay 4 Mana: Banish it; draw 1 card.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.EnemySpellOrArtifact),
                    Act(EffectActionType.DrawCards, 1)));

            var tapestry = Artifact("Hexweaver Tapestry of Debts", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Weave It Back In", "Once per turn — pay 2 Mana: Set 1 \"Hexweaver\" Spell from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SetTargetSpellFromGraveyard, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Hexweaver")));
            tapestry.firstSpellDiscountPerTurn = 1;

            var pattern = Rel("Hexweaver, the Pattern Beneath", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Human, 2700, 2500,
                "4+ Spells in your Graveyard. Cost 3 Mana.", 3,
                Fx("Read the Pattern", "When this card is Summoned: Add up to 2 Spells from your Graveyard to your hand; gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Unravel Their Work", "Pay 2 Mana: Negate the effects of 1 Spell or Artifact your opponent controls until the end of this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemySpellOrArtifact)));
            pattern.reqGraveyardSpellsAtLeast = 4;
        }

        private static void W6Kindlekin()
        {
            Mon("Kindlekin Cindertail", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Beast, 700, 600,
                Fx("Tag Along", "When this card is Summoned: You can Special Summon 1 Level 1 FIRE monster from your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, level: 1, attribute: MonsterAttribute.Fire, upTo: true)),
                Fx("Warm Trail", "When this card is destroyed: Add 1 Level 1 FIRE monster from your Graveyard to your hand.",
                    EffectTrigger.OnDestroyedSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Fire, upTo: true)));

            var chief = Mon("Kindlekin Bonfire Chief", CardRarity.Rare, 2, MonsterAttribute.Fire, MonsterType.Beast, 1400, 1300,
                Inf("Everyone Back to the Fire", "Once per turn — pay 2 Mana: Special Summon up to 2 Level 1 FIRE monsters from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Fire, targetCount: 2, upTo: true)));
            chief.canSelfSpecialSummon = true;
            chief.selfSummonRequiresNameOnField = "Kindlekin";
            chief.auraAtkBonus = 200;
            chief.auraDefBonus = 200;
            chief.auraLevelFilter = 1;
            chief.auraExcludesSelf = true;

            Spell("Kindlekin Pile On", CardRarity.Uncommon, false,
                Fx("Pile On", "Special Summon up to 3 Level 1 FIRE monsters with 1000 or less ATK from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, level: 1, attribute: MonsterAttribute.Fire, maxAtk: 1000, targetCount: 3, upTo: true)),
                Inf("Everyone Piles On", "Instead, pay 3 Mana: From your hand or Graveyard.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromHandOrGrave, 1, TargetKind.HandOrGraveMonsterFiltered, level: 1, attribute: MonsterAttribute.Fire, maxAtk: 1000, targetCount: 3, upTo: true)));

            Spell("Kindlekin Warm Glow", CardRarity.Uncommon, true,
                Fx("Warm Glow", "Up to 3 of your Level 1 monsters gain 300 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, level: 1, targetCount: 3, upTo: true)),
                Inf("Hearthfire Halo", "Instead, pay 2 Mana: Up to 5; they also cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, level: 1, targetCount: 5, upTo: true),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, level: 1, targetCount: 5, upTo: true)));

            Artifact("Kindlekin Hearthstone", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Kept Warm", "Once per turn, when a monster you control is destroyed: Add 1 Level 1 FIRE monster from your Graveyard to your hand.",
                    EffectTrigger.OnOwnMonsterDestroyed, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Fire, upTo: true)),
                Inf("Stoke the Stone", "Pay 2 Mana: Special Summon 1 Level 1 FIRE monster from your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, level: 1, attribute: MonsterAttribute.Fire)));

            var thousandth = Rel("Kindlekin, the Thousandth Flame", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Beast, 2600, 2000,
                "You control 3+ \"Kindlekin\" monsters. Cost 3 Mana.", 3,
                Fx("A Thousand Sparks", "When this card is Summoned: All Level 1 monsters you control gain 500 ATK until the end of this turn; this card can attack twice this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster, level: 1, targetCount: 5, upTo: true),
                    Act(EffectActionType.AttackAgainSelf)),
                Inf("Rekindled Legion", "Pay 3 Mana: Special Summon up to 2 Level 1 FIRE monsters from your Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Fire, targetCount: 2, upTo: true)));
            thousandth.reqNamedOnField = "Kindlekin";
            thousandth.reqNamedCount = 3;
        }

        private static void W6Lightless()
        {
            Mon("Lightless Pallbearer", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 600, 1300,
                Fx("Carry Them Back", "When this card is Summoned: Set 1 \"Lightless\" monster from your Graveyard face-down on your field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Lightless", upTo: true)),
                Inf("Quiet Procession", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Lightless", upTo: true),
                    Act(EffectActionType.DrawCards, 1)));

            var gravewatcher = Mon("Lightless Gravewatcher", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2100, 1800,
                Fx("Watch Turn", "When this card is flipped face-up: turn 1 monster your opponent controls face-down into Defense Position.",
                    EffectTrigger.OnFlipFaceUp, 0, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)));
            gravewatcher.canSelfSpecialSummon = true;
            gravewatcher.selfSummonRequiresFaceDownOnField = true;
            gravewatcher.passiveAtkPerCount = 300;
            gravewatcher.passiveAtkPerCountKind = EffectCountKind.OwnFaceDownMonsters;

            Spell("Lightless Vigil", CardRarity.Uncommon, false,
                Fx("Keep the Vigil", "Set 1 \"Lightless\" monster from your Deck face-down on your field.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.DeckMonsterFilteredSelf, nameFilter: "Lightless")),
                Inf("Double Watch", "Instead, pay 3 Mana: Set 2.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.DeckMonsterFilteredSelf, nameFilter: "Lightless", targetCount: 2)));

            Spell("Lightless Sudden Dark", CardRarity.Rare, true,
                Fx("Sudden Dark", "Turn 1 monster your opponent controls face-down into Defense Position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)),
                Inf("Total Dark", "Instead, pay 3 Mana: Up to 2.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)));

            var snuffer = Artifact("Lightless Candle Snuffer", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Snuff It Out", "Once per turn — pay 2 Mana: Destroy 1 face-down monster your opponent controls.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.FaceDownMonsterEnemy)));
            snuffer.auraDefBonus = 300;
            snuffer.auraOnlyFaceDown = true;

            var hourOfNone = Rel("Lightless, Hour of None", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Demon, 2700, 2300,
                "You control 2+ face-down monsters. Cost 3 Mana.", 3,
                Fx("The Hour Strikes", "When this card is Summoned: Turn up to 2 monsters your opponent controls face-down into Defense Position.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)),
                Inf("Nothing Left Lit", "Pay 3 Mana: Destroy 1 face-down monster on the field; draw 1 card.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.FaceDownMonsterAny),
                    Act(EffectActionType.DrawCards, 1)));
            hourOfNone.reqOwnFaceDownMonsters = 2;
        }

        private static void W6Lyria()
        {
            Mon("Lyria Understage", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Human, 500, 1200,
                Fx("Trapdoor Entrance", "FLIP: Special Summon 1 Level 1 \"Lyria\" monster from your Deck face-down.",
                    EffectTrigger.OnFlipFaceUp, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.DeckMonsterFilteredSelf, level: 1, nameFilter: "Lyria", upTo: true)),
                Inf("Back Below", "Pay 1 Mana: Turn this card face-down into Defense Position.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.SelfCard)));

            var primaVoce = Mon("Lyria Prima Voce", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Human, 2300, 2000,
                Fx("Shattering Note", "FLIP: 1 monster your opponent controls permanently loses 500 ATK.",
                    EffectTrigger.OnFlipFaceUp, 0, true,
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster)),
                Inf("Cue the Soloist", "Pay 2 Mana: Flip 1 of your face-down monsters face-up.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterSelf)));
            primaVoce.canSelfSpecialSummon = true;
            primaVoce.selfSummonRequiresFaceDownOnField = true;

            Spell("Lyria Intermission", CardRarity.Uncommon, true,
                Fx("Intermission", "Turn 1 monster you control face-down into Defense Position; it gains 500 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AllyMonster)),
                Inf("Extended Break", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AllyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Lyria Standing Room Only", CardRarity.Uncommon, false,
                Fx("Standing Room Only", "Set up to 2 \"Lyria\" monsters from your hand face-down.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered, nameFilter: "Lyria", targetCount: 2, upTo: true)),
                Inf("Sold-Out Show", "Instead, pay 3 Mana: From your hand or Graveyard.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandOrGraveMonsterFiltered, nameFilter: "Lyria", targetCount: 2, upTo: true)));

            Artifact("Lyria Orchestra Pit", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Strike Up the Band", "Once per turn, when one of your monsters is flipped face-up: Draw 1 card.",
                    EffectTrigger.OnOwnMonsterFlipped, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Conductor's Cue", "Pay 2 Mana: Flip 1 of your face-down monsters face-up.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterSelf)));

            var encore = Rel("Lyria, the Encore Eternal", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Human, 2600, 2600,
                "You control 3+ face-down monsters. Cost 3 Mana.", 3,
                Fx("Curtain Up", "When this card is Summoned: Flip all your face-down monsters face-up; they gain 300 ATK until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterSelf, targetCount: 5, upTo: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, targetCount: 5, upTo: true)),
                Inf("Reset the Stage", "Pay 2 Mana: Turn up to 2 of your monsters face-down.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster, targetCount: 2, upTo: true)));
            encore.reqOwnFaceDownMonsters = 3;
        }

        private static void W6Manacle()
        {
            Mon("Manacle Pawnbroker", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Myth, 800, 900,
                Fx("Opening Offer", "When this card is Summoned: Your opponent loses 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentMana, 1)),
                Inf("Compound Terms", "Instead, pay 2 Mana: They also have 1 less Mana during their next turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DrainOpponentMana, 1),
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1)));

            var chancellor = Mon("Manacle Chancellor of Coin", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Myth, 2500, 2000,
                Fx("Audit the Books", "When this card is Summoned, if you have 6+ Mana: draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Seize Assets", "Pay 3 Mana: Your opponent loses 2 Mana; you gain 1 Mana.",
                    EffectTrigger.Quick, 3, false,
                    Act(EffectActionType.DrainOpponentMana, 2),
                    Act(EffectActionType.GainMana, 1)));
            chancellor.canSelfSpecialSummon = true;
            chancellor.selfSummonChecksOpponentField = true;
            chancellor.selfSummonRequiresOpponentMonsters = 2;
            chancellor.effects[0].minMana = 6;

            Spell("Manacle Foreclosure", CardRarity.Rare, false,
                Fx("Foreclosure", "Destroy 1 monster your opponent controls with 2000 or less ATK; your opponent loses 1 Mana.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000),
                    Act(EffectActionType.DrainOpponentMana, 1)),
                Inf("Total Repossession", "Instead, pay 5 Mana: No ATK limit.",
                    EffectTrigger.OnActivate, 5, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrainOpponentMana, 1)));

            Spell("Manacle Wage Garnish", CardRarity.Uncommon, true,
                Fx("Wage Garnish", "Your opponent has 1 less Mana during their next turn; you have 1 more during yours.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1),
                    Act(EffectActionType.GainManaNextTurn, 1)),
                Inf("Full Garnishment", "Instead, pay 3 Mana: 2 less / 2 more.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 2),
                    Act(EffectActionType.GainManaNextTurn, 2)));

            Artifact("Manacle Vault Door", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Interest Accrues", "During your Standby Phase: Gain 1 Mana.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.GainMana, 1)),
                Inf("Slam the Vault", "Once per turn — pay 2 Mana: Your opponent loses 1 Mana.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DrainOpponentMana, 1)));

            var crownDebt = Rel("Manacle, the Crown Debt", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Myth, 3000, 2400,
                "You have 8+ Mana available. Cost 5 Mana.", 5,
                Fx("The Crown Collects", "When this card is Summoned: Your opponent loses 3 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentMana, 3)),
                Inf("Royal Dividend", "Pay 4 Mana: Draw 2 cards; you have 2 more Mana during your next turn.",
                    EffectTrigger.Ignition, 4, false,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainManaNextTurn, 2)));
            crownDebt.reqMinMana = 8;
        }

        private static void W6Mechination()
        {
            Mon("Mechination Rivetrunner", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Mecha, 900, 700,
                Fx("Sprint the Line", "When this card is Normal Summoned: Special Summon 1 Level 1 EARTH monster from your Graveyard.",
                    EffectTrigger.OnNormalSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Earth, upTo: true)),
                Inf("Fresh from the Line", "Instead, pay 2 Mana: From your Deck instead (with 1000 or less ATK).",
                    EffectTrigger.OnNormalSummonSelf, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFilteredSelf, level: 1, attribute: MonsterAttribute.Earth, maxAtk: 1000)));

            var supervisor = Mon("Mechination Shift Supervisor", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Mecha, 2300, 2100,
                Inf("Double Shift", "Once per turn — pay 2 Mana: 1 MECHA monster you control can attack twice this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.AllyMonster, monsterType: MonsterType.Mecha)));
            supervisor.canSelfSpecialSummon = true;
            supervisor.selfSummonRequiresNameOnField = "Mechination";
            supervisor.auraAtkBonus = 200;
            supervisor.auraUseTypeFilter = true;
            supervisor.auraTypeFilter = MonsterType.Mecha;
            supervisor.auraExcludesSelf = true;

            Spell("Mechination Production Quota", CardRarity.Uncommon, false,
                Fx("Meet the Quota", "Add up to 2 Level 1 EARTH monsters from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Earth, targetCount: 2, upTo: true)),
                Inf("Exceed the Quota", "Instead, pay 2 Mana: Special Summon 1 of them.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Earth, upTo: true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Earth, upTo: true)));

            Spell("Mechination Emergency Shift", CardRarity.Rare, true,
                Fx("Emergency Shift", "Special Summon up to 2 MECHA monsters from your hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, monsterType: MonsterType.Mecha, targetCount: 2, upTo: true)),
                Inf("All Hands", "Instead, pay 3 Mana: They gain 300 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, monsterType: MonsterType.Mecha, targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 300, TargetKind.AllyMonster, monsterType: MonsterType.Mecha, targetCount: 2, upTo: true)));

            var gearVault = Artifact("Mechination Gear Vault", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Requisition Parts", "Once per turn — pay 2 Mana: Add 1 Level 2 EARTH monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, level: 2, attribute: MonsterAttribute.Earth)));
            gearVault.auraDefBonus = 200;
            gearVault.auraUseTypeFilter = true;
            gearVault.auraTypeFilter = MonsterType.Mecha;

            var primeMotor = Rel("Mechination, Prime Motor", CardRarity.Legendary, 3,
                MonsterAttribute.Earth, MonsterType.Mecha, 2900, 2700,
                "You control 3+ \"Mechination\" monsters. Cost 3 Mana.", 3,
                Fx("Torque of Ages", "When this card is Summoned: Up to 2 MECHA monsters you control gain 400 ATK permanently.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.AllyMonster, monsterType: MonsterType.Mecha, targetCount: 2, upTo: true)),
                Inf("Night Shift Rally", "Pay 3 Mana: Special Summon up to 2 Level 1 EARTH monsters from your Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, attribute: MonsterAttribute.Earth, targetCount: 2, upTo: true)));
            primeMotor.reqNamedOnField = "Mechination";
            primeMotor.reqNamedCount = 3;
        }

        private static void W6Mimicrypt()
        {
            Mon("Mimicrypt Waxchild", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Demon, 500, 800,
                Fx("Borrowed Face", "When this card is Summoned: This card copies the ATK and DEF of 1 monster on the field until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster, excludeSelf: true)),
                Inf("Melt Into Memory", "Pay 2 Mana and banish this card from your Graveyard: banish 1 card from your opponent's Graveyard.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent)));

            var mirrorborn = Mon("Mimicrypt Mirrorborn", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Demon, 1200, 1200,
                Fx("Mirror Stance", "Once per turn: This card copies the ATK and DEF of 1 monster on the field until the end of this turn.",
                    EffectTrigger.Quick, 0, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster, excludeSelf: true)),
                Inf("Perfect Reflection", "Instead, pay 2 Mana: It also cannot be destroyed by battle this turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster, excludeSelf: true),
                    Act(EffectActionType.ProtectSelfThisTurn)));
            mirrorborn.canSelfSpecialSummon = true;
            mirrorborn.selfSummonChecksOpponentField = true;
            mirrorborn.selfSummonRequiresOpponentMonsters = 2;

            Spell("Mimicrypt Borrowed Words", CardRarity.Rare, false,
                Fx("Borrowed Words", "Choose 1 Spell in your opponent's Graveyard; resolve its effect as if it were yours.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.CopySpellFromOpponentGraveyard, 1, TargetKind.GraveyardSpellOpponent)),
                Inf("Words Made Mine", "Instead, pay 3 Mana: Then banish it.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.CopySpellFromOpponentGraveyard, 1, TargetKind.GraveyardSpellOpponent),
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardSpellOpponent)));

            Spell("Mimicrypt Cold Read", CardRarity.Uncommon, true,
                Fx("Cold Read", "Look at your opponent's hand and choose 1 card; they discard it.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.LookAndDiscardChosen, 1)),
                Inf("Deep Read", "Instead, pay 4 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.LookAndDiscardChosen, 1),
                    Act(EffectActionType.DrawCards, 1)));

            var waxMuseum = Artifact("Mimicrypt Wax Museum", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("New Exhibit", "Once per turn — pay 3 Mana: Special Summon 1 monster from your OPPONENT's Graveyard to your field.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterOpponent)));
            waxMuseum.auraDefBonus = 300;
            waxMuseum.auraNameFilter = "Mimicrypt";

            var court = Rel("Mimicrypt, the Faceless Court", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Demon, 1500, 1500,
                "Your opponent's Graveyard holds 10+ cards. Cost 3 Mana.", 3,
                Fx("Court in Session", "When this card is Summoned: This card copies the ATK and DEF of 1 monster on the field until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster, excludeSelf: true)),
                Inf("Rule in Absentia", "Pay 3 Mana: Take control of 1 monster your opponent controls until the End Phase.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.TakeControlUntilEndOfTurn, 1, TargetKind.EnemyMonster)));
            court.reqOpponentGraveyardAtLeast = 10;
        }
    }
}
