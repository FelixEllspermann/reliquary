using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // 6er-Welle (Design v2, freigegeben): Teil 1 — Apocrypha bis Gaslight.
    // Nutzt die Batch2026Builder-Helfer (partial), idempotent wie alle Stages.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build Wave Six — 1 (Apocrypha–Gaslight)")]
        public static void BuildWaveSix1()
        {
            built.Clear();
            W6Apocrypha(); W6Archfiend(); W6Barrierstruck(); W6Deathpoem(); W6Deckay();
            W6DragonShrine(); W6Failsafe(); W6Fethaerbreese(); W6Forgeheart(); W6Gaslight();
            Finish("WaveSix 1");
        }

        private static void W6Apocrypha()
        {
            var manticore = Mon("Apocrypha Manticore", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Myth, 1400, 1100,
                Fx("Torn from the Index", "When this card is Summoned: Banish 1 card from your Graveyard; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Torn from Every Index", "Instead, pay 2 Mana: Banish up to 2; draw 2, then reveal the top card of your Deck — you may put it on the bottom.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, targetCount: 2, upTo: true, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.RevealTopMayBottom)));
            manticore.canSelfSpecialSummon = true;
            manticore.selfSummonRequiresGraveNamedCount = 2;
            manticore.selfSummonRequiresGraveNamed = "Apocrypha";

            var basilisk = Mon("Apocrypha Basilisk", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Myth, 2000, 1700,
                Inf("Petrifying Footnote", "Pay 2 Mana: Negate the effects of 1 card on the field until the end of this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)));
            basilisk.canSelfSpecialSummon = true;
            basilisk.selfSummonChecksOpponentField = true;
            basilisk.selfSummonRequiresOpponentMonsters = 2;
            basilisk.passiveAtkPerCount = 200;
            basilisk.passiveAtkPerCountKind = EffectCountKind.OwnBanishedMonsters;

            Spell("Apocrypha Errata", CardRarity.Uncommon, false,
                Fx("Correct the Record", "Shuffle up to 3 cards from your Graveyard into your Deck; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.GraveyardCardSelf, targetCount: 3, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Strike the Record", "Instead, pay 2 Mana: Banish them instead; draw 2.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, targetCount: 3, upTo: true),
                    Act(EffectActionType.DrawCards, 2)));

            Spell("Apocrypha Missing Page", CardRarity.Rare, true,
                Fx("Recovered Fragment", "Return 1 of your banished cards to your Graveyard; 1 monster you control swaps its ATK and DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf),
                    Act(EffectActionType.SwapAtkDefThisTurn, 1, TargetKind.AllyMonster)),
                Inf("Recovered Chapter", "Instead, pay 3 Mana: Return up to 2; the monster also cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.SwapAtkDefThisTurn, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster)));

            Artifact("Apocrypha Bestiary", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Catalogue of Beasts", "Once per turn: Banish 1 card from your Graveyard; add 1 \"Apocrypha\" card from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, isCost: true),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Apocrypha")),
                Inf("Hostile Citation", "Pay 2 Mana: Banish 1 card from your opponent's Graveyard; 1 monster you control gains 300 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster)));

            var chapter = Rel("Apocrypha, the Lost Chapter", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Myth, 2600, 2200,
                "3+ of your cards are banished — pay 2 Mana.", 2,
                Fx("Rebinding", "When this card is Summoned: Return up to 2 of your banished cards to your Graveyard; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Stricken Entirely", "Pay 3 Mana: Banish 1 monster your opponent controls.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.BanishTargetMonster, 1, TargetKind.EnemyMonster)),
                Inf("Between the Lines", "Pay 2 Mana: This card cannot be targeted this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CannotBeTargetedThisTurn, 1, TargetKind.SelfCard)));
            chapter.reqBanishedAtLeast = 3;
        }

        private static void W6Archfiend()
        {
            var executioner = Mon("Archfiend Executioner", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Demon, 1700, 900,
                Fx("Carry Out the Sentence", "When this card is Summoned: Destroy 1 monster your opponent controls with 1200 or less ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1200)),
                Inf("Broaden the Writ", "Instead, pay 2 Mana: 1800 or less.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1800)));
            executioner.canSelfSpecialSummon = true;
            executioner.selfSummonChecksOpponentField = true;
            executioner.selfSummonRequiresOpponentMonsters = 2;

            Mon("Archfiend Court Jester", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Demon, 700, 700,
                Fx("Dying Joke", "When this card is destroyed: Draw 1 card.",
                    EffectTrigger.OnDestroyedSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Cutting Punchline", "Pay 1 Mana and send this card from your hand to the Graveyard: 1 monster your opponent controls permanently loses 400 ATK.",
                    EffectTrigger.HandIgnition, 1, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DebuffTargetAtk, 400, TargetKind.EnemyMonster)));

            Spell("Archfiend Death Warrant", CardRarity.Rare, false,
                Fx("Signed and Sealed", "Destroy 1 monster your opponent controls with 2000 or less ATK.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000)),
                Inf("Royal Decree", "Instead, pay 4 Mana: No ATK limit; then Special Summon 1 \"Archfiend\" monster from your Graveyard face-down.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend", upTo: true)));

            Spell("Archfiend Last Laugh", CardRarity.Uncommon, true,
                Fx("Last Laugh", "Destroy 1 monster you control; draw 2 cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("The Joke's On You", "Instead, pay 2 Mana: Also Special Summon 1 \"Archfiend\" monster from your Graveyard face-down.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend", upTo: true)));

            var gallows = Artifact("Archfiend Gallows", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("March to the Scaffold", "Once per turn — pay 2 Mana: Destroy 1 monster you control; Special Summon 1 \"Archfiend\" monster from your Graveyard face-down.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend")));
            gallows.effects[0].oncePerTurn = true;
            gallows.auraAtkBonus = 200;
            gallows.auraUseTypeFilter = true;
            gallows.auraTypeFilter = MonsterType.Demon;

            var kingmaker = Rel("Archfiend Kingmaker", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Demon, 2800, 2000,
                "3+ monsters in your Graveyard. Cost 3 Mana; Tribute 1 monster you control.", 3,
                Fx("Coronation in Blood", "When this card is Summoned: Destroy up to 2 monsters your opponent controls with 1500 or less ATK; this card permanently gains 200 ATK for each.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500, targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffSelfAtkPerCount, 0)),
                Inf("Raise the Court", "Pay 3 Mana: Special Summon 1 \"Archfiend\" monster from your Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend")));
            kingmaker.reqGraveyardMonstersAtLeast = 3;
            kingmaker.costTributeOwnMonsters = 1;
            // BuffSelfAtkPerCount hier ohne sauberen Zähler — ersetze durch festen Buff:
            kingmaker.effects[0].actions[1] = Act(EffectActionType.BuffTargetAtk, 200, TargetKind.SelfCard);
        }

        private static void W6Barrierstruck()
        {
            Mon("Barrierstruck Mason", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Mecha, 400, 1600,
                Fx("Rebuild the Wall", "When this card is Summoned: Add 1 \"Barrierstruck\" Artifact from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Barrierstruck")),
                Inf("Raise It Higher", "Instead, pay 2 Mana: Place it directly onto the field; 1 monster you control gains 300 DEF until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Barrierstruck"),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 300, TargetKind.AllyMonster)));

            var gatekeeper = Mon("Barrierstruck Gatekeeper", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Mecha, 1600, 3000,
                Inf("The Gate Answers", "Pay 2 Mana: This card cannot be destroyed this turn; it swaps ATK and DEF until the end of this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectSelfThisTurn),
                    Act(EffectActionType.SwapAtkDefThisTurn, 1, TargetKind.SelfCard)));
            gatekeeper.canSelfSpecialSummon = true;
            gatekeeper.selfSummonRequiresArtifact = true;
            gatekeeper.passiveTaunt = true;
            gatekeeper.passiveDefPerCount = 200;
            gatekeeper.passiveDefPerCountKind = EffectCountKind.OwnArtifactsOnField;

            Spell("Barrierstruck Hold the Line", CardRarity.Uncommon, true,
                Fx("Hold the Line", "Up to 3 of your monsters gain 600 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 600, TargetKind.AllyMonster, targetCount: 3, upTo: true)),
                Inf("Not One Step Back", "Instead, pay 3 Mana: They also cannot be destroyed this turn, and 1 of them draws every attack.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 600, TargetKind.AllyMonster, targetCount: 3, upTo: true),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, targetCount: 3, upTo: true),
                    Act(EffectActionType.TauntThisTurn, 1, TargetKind.AllyMonster)));

            Spell("Barrierstruck Demolition Refund", CardRarity.Uncommon, false,
                Fx("Demolition Refund", "Destroy 1 Artifact you control; draw 2 cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Insurance Payout", "Instead, pay 2 Mana: Also place 1 \"Barrierstruck\" Artifact from your Deck onto the field.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Barrierstruck")));

            var keystone = Artifact("Barrierstruck Keystone", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("The Arch Holds", "When this card is destroyed: Place up to 2 \"Barrierstruck\" Artifacts from your Graveyard onto the field.",
                    EffectTrigger.OnDestroyedSelf, 0, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Barrierstruck", targetCount: 2, upTo: true)));
            keystone.redirectDestructionToSelf = true;

            var unbreached = Rel("Barrierstruck, the Unbreached", CardRarity.Legendary, 3,
                MonsterAttribute.Earth, MonsterType.Mecha, 2200, 3400,
                "You control 3+ Artifacts. Cost 3 Mana.", 3,
                Fx("Fortify the Living", "When this card is Summoned: Up to 2 of your monsters gain 500 DEF permanently.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetDef, 500, TargetKind.AllyMonster, targetCount: 2, upTo: true)),
                Inf("The Wall Walks", "Pay 3 Mana: This card swaps ATK and DEF until the end of this turn; it can attack twice this Battle Phase.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SwapAtkDefThisTurn, 1, TargetKind.SelfCard),
                    Act(EffectActionType.AttackAgainSelf)));
            unbreached.reqOwnArtifactsOnField = 3;
            unbreached.battleShieldMinOwnArtifacts = 1;
        }

        private static void W6Deathpoem()
        {
            var elegist = Mon("Deathpoem Elegist", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Human, 1500, 600,
                Fx("Elegy for Steel", "Tribute this card: destroy 1 Spell or Artifact your opponent controls.",
                    EffectTrigger.Ignition, 0, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemySpellOrArtifact)),
                Inf("Verse Remembered", "Pay 2 Mana and banish this card from your Graveyard: add 1 \"Deathpoem\" card from your Deck to your hand.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Deathpoem")));
            elegist.canSelfSpecialSummon = true;
            elegist.selfSummonRequiresGraveNamedCount = 4;

            var warpoet = Mon("Deathpoem Warpoet", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Human, 2200, 1400,
                Fx("Stanza of Slaughter", "Tribute this card: destroy up to 2 monsters with 1500 or less ATK.",
                    EffectTrigger.Ignition, 0, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500, targetCount: 2, upTo: true)));
            warpoet.canSelfSpecialSummon = true;
            warpoet.selfSummonRequiresGraveNamedCount = 6;
            warpoet.passiveAtkPerCount = 100;
            warpoet.passiveAtkPerCountKind = EffectCountKind.OwnGraveyardCards;

            Spell("Deathpoem Final Draft", CardRarity.Uncommon, false,
                Fx("Final Draft", "Send 1 \"Deathpoem\" monster from your Deck to the Graveyard; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckMonsterFilteredSelf, nameFilter: "Deathpoem"),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Collected Works", "Instead, pay 2 Mana: Send 2.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckMonsterFilteredSelf, nameFilter: "Deathpoem", targetCount: 2),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Deathpoem Recital", CardRarity.Rare, true,
                Fx("Midnight Recital", "Special Summon 1 \"Deathpoem\" monster from your Graveyard.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Deathpoem")),
                Inf("Recital for Two", "Instead, pay 4 Mana: Special Summon 2; they cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Deathpoem", targetCount: 2, upTo: true),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, targetCount: 2, upTo: true)));

            Artifact("Deathpoem Inkstone", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Ink from Ashes", "Once per turn, when a monster you control is Tributed: Draw 1 card.",
                    EffectTrigger.OnOwnMonsterTributed, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Sharpen the Verse", "Pay 2 Mana: Return 1 \"Deathpoem\" monster from your Graveyard to your hand; it gains 300 ATK permanently.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Deathpoem"),
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.AllyMonster, upTo: true)));

            var author = Rel("Deathpoem, Death of the Author", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Human, 2500, 2000,
                "Your Graveyard holds 7+ cards — pay 3 Mana.", 3,
                Fx("The Author Falls", "When this card is Summoned: Destroy 1 monster your opponent controls; then you can Special Summon 1 Level 1 \"Deathpoem\" monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, nameFilter: "Deathpoem", upTo: true)),
                Inf("The Book Burns", "Pay 4 Mana and tribute this card: destroy ALL monsters your opponent controls with 2000 or less ATK.",
                    EffectTrigger.Ignition, 4, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000, targetCount: 5, upTo: true)));
            author.reqGraveyardAtLeast = 7;
        }

        private static void W6Deckay()
        {
            Mon("Deckay Burrower", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 500, 700,
                Fx("Tunnel Through", "During either player's End Phase: mill 2 cards.",
                    EffectTrigger.EitherEndPhase, 0, true,
                    Act(EffectActionType.MillSelf, 2)),
                Fx("Never Truly Gone", "If this card is sent from the Deck to the Graveyard: Special Summon it.",
                    EffectTrigger.OnMilledSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.SelfCard)));

            var prince = Mon("Deckay Carrion Prince", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Demon, 2100, 1500,
                Fx("Court of Rot", "When this card is Summoned: Mill 3 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillSelf, 3)),
                Inf("Tithe of Decay", "Pay 2 Mana: Shuffle up to 3 cards from your Graveyard into your Deck; 1 monster your opponent controls permanently loses 500 ATK.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.GraveyardCardSelf, targetCount: 3, upTo: true),
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster)));
            prince.canSelfSpecialSummon = true;
            prince.selfSummonRequiresMilled = true;
            prince.passiveAtkPerCount = 100;
            prince.passiveAtkPerCountKind = EffectCountKind.OwnGraveyardCards;

            Spell("Deckay Decomposition", CardRarity.Uncommon, false,
                Fx("Decomposition", "Mill 4 cards; then you can add 1 \"Deckay\" card from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MillSelf, 4),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Deckay", upTo: true)),
                Inf("Full Breakdown", "Instead, pay 2 Mana: Mill 6; add up to 2.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.MillSelf, 6),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Deckay", targetCount: 2, upTo: true)));

            Spell("Deckay Spoilage", CardRarity.Rare, true,
                Fx("Spoilage", "Mill 2 cards; 1 monster your opponent controls permanently loses 300 ATK.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MillSelf, 2),
                    Act(EffectActionType.DebuffTargetAtk, 300, TargetKind.EnemyMonster)),
                Inf("Deep Rot", "Instead, pay 3 Mana: Mill 4; it permanently loses 500 ATK.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.MillSelf, 4),
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster)));

            Artifact("Deckay Compost Heap", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Compost", "During your Standby Phase: mill 2 cards.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.MillSelf, 2)),
                Inf("Rich Soil", "Once per turn — pay 1 Mana, if you milled this or last turn: You have 2 more Mana during your next turn.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.GainManaNextTurn, 2)));
            built[built.Count - 1].effects[1].oncePerTurn = true;
            built[built.Count - 1].effects[1].requireMilledLastTurn = true;

            var under = Rel("Deckay, the Ravenous Under", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Demon, 2700, 1900,
                "Your Graveyard holds 10+ cards — pay 2 Mana.", 2,
                Fx("The Under Feeds", "When this card is Summoned: Mill 5 cards; then Special Summon up to 2 \"Deckay\" monsters from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillSelf, 5),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Deckay", targetCount: 2, upTo: true)),
                Inf("Fed and Patient", "Pay 2 Mana, if you milled this or last turn: this card cannot be destroyed this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectSelfThisTurn)));
            under.reqGraveyardAtLeast = 10;
            under.effects[1].requireMilledLastTurn = true;
        }

        private static void W6DragonShrine()
        {
            Mon("Novice of the Dragon Shrine", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Human, 600, 900,
                Fx("First Offering", "When this card is Normal Summoned: Special Summon 1 Level 1 Dragon-Type monster from your hand.",
                    EffectTrigger.OnNormalSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, level: 1, monsterType: MonsterType.Dragon)),
                Inf("Grand Offering", "Instead, pay 2 Mana: From your Deck instead.",
                    EffectTrigger.OnNormalSummonSelf, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFilteredSelf, level: 1, monsterType: MonsterType.Dragon)));

            var twins = Mon("Twin Wyrms of the Dragon Shrine", CardRarity.Rare, 2, MonsterAttribute.Light, MonsterType.Dragon, 1500, 1500,
                Fx("Two at the Gate", "When this card is Summoned, if you control another monster: draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Wake the Second", "Once per turn — pay 2 Mana: Special Summon 1 Level 1 Dragon-Type monster from your Graveyard; it gains 200 DEF permanently.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, monsterType: MonsterType.Dragon),
                    Act(EffectActionType.BuffTargetDef, 200, TargetKind.AllyMonster, upTo: true)));
            twins.canSelfSpecialSummon = true;
            twins.selfSummonRequiresAttribute = true;
            twins.selfSummonRequiredAttribute = MonsterAttribute.Light;
            twins.effects[0].minOwnMonsters = 1;
            twins.effects[1].oncePerTurn = true;

            Spell("Dragon Shrine Morning Rites", CardRarity.Uncommon, false,
                Fx("Morning Rites", "Add 1 Level 2 Dragon-Type monster from your Deck to your hand; reveal the top card of your Deck — you may put it on the bottom.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, level: 2, monsterType: MonsterType.Dragon),
                    Act(EffectActionType.RevealTopMayBottom)),
                Inf("Full Procession", "Instead, pay 3 Mana: Add 1 Level 2 AND 1 Level 1 Dragon-Type monster.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, level: 2, monsterType: MonsterType.Dragon),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, level: 1, monsterType: MonsterType.Dragon)));

            Spell("Dragon Shrine Guardian's Breath", CardRarity.Uncommon, true,
                Fx("Guardian's Breath", "1 Dragon-Type monster you control cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, monsterType: MonsterType.Dragon)),
                Inf("Twin Breath", "Instead, pay 2 Mana: Up to 2; they also gain 400 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, monsterType: MonsterType.Dragon, targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 400, TargetKind.AllyMonster, monsterType: MonsterType.Dragon, targetCount: 2, upTo: true)));

            Artifact("Dragon Shrine Prayer Wheel", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Turning Prayers", "During your Standby Phase: You can Special Summon 1 Level 1 Dragon-Type monster from your hand.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, level: 1, monsterType: MonsterType.Dragon, upTo: true)),
                Inf("Answered Prayer", "Pay 3 Mana: Add 1 \"Dragon Shrine\" card from your Deck to your hand.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Dragon Shrine")));

            var firstFlame = Rel("Dragon Shrine, First Flame of Worship", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Dragon, 2900, 2400,
                "You control 3+ \"Dragon\" monsters. Cost 3 Mana.", 3,
                Fx("The Shrine Ignites", "When this card is Summoned: All Dragon-Type monsters you control gain 400 ATK permanently.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.AllyMonster, monsterType: MonsterType.Dragon, targetCount: 5, upTo: true)),
                Inf("Pilgrims Return", "Pay 3 Mana: Special Summon up to 2 Level 1 Dragon-Type monsters from your Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1, monsterType: MonsterType.Dragon, targetCount: 2, upTo: true)));
            firstFlame.reqNamedOnField = "Dragon";
            firstFlame.reqNamedCount = 3;
        }

        private static void W6Failsafe()
        {
            var foreman = Mon("Failsafe Foreman", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 1200, 1600,
                Fx("Back on the Grid", "When this card is Summoned: Set 1 \"Failsafe\" Artifact from your Graveyard onto the field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Failsafe")),
                Inf("Fresh Installation", "Instead, pay 2 Mana: From your Deck instead, then draw 1 card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Failsafe"),
                    Act(EffectActionType.DrawCards, 1)));
            foreman.canSelfSpecialSummon = true;
            foreman.selfSummonRequiresArtifact = true;

            var officer = Mon("Failsafe Redundancy Officer", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Human, 1800, 2200,
                Inf("Backup Protocol", "Pay 2 Mana: 1 of your Artifacts cannot be destroyed this turn; draw 1 card.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyArtifact),
                    Act(EffectActionType.DrawCards, 1)));
            officer.canSelfSpecialSummon = true;
            officer.selfSummonRequiresArtifact = true;
            officer.passiveDefPerCount = 200;
            officer.passiveDefPerCountKind = EffectCountKind.OwnArtifactsOnField;

            Spell("Failsafe Protocol", CardRarity.Uncommon, false,
                Fx("Recovery Protocol", "Add up to 2 \"Failsafe\" Artifacts from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Failsafe", targetCount: 2, upTo: true)),
                Inf("Hot Swap", "Instead, pay 2 Mana: Also set 1 \"Failsafe\" Artifact from your Graveyard directly onto the field.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Failsafe", targetCount: 2, upTo: true),
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Failsafe", upTo: true)));

            Spell("Failsafe Manual Override", CardRarity.Rare, true,
                Fx("Manual Override", "Negate the effects of 1 monster your opponent controls until the end of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyMonster)),
                Inf("Override and Rearm", "Instead, pay 3 Mana: Also set 1 \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Failsafe")));

            Artifact("Failsafe Circuit Breaker", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Trip the Breaker", "1 monster your opponent controls cannot attack this turn. Then send this card to the Graveyard and set 1 other \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.Quick, 0, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SendSelfToGraveyard),
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Failsafe", upTo: true, excludeSameName: true)));

            var deadman = Rel("Failsafe, the Dead Man's Switch", CardRarity.Legendary, 3,
                MonsterAttribute.Earth, MonsterType.Mecha, 2400, 2800,
                "3+ Artifacts in your Graveyard. Cost 3 Mana.", 3,
                Fx("Everything Reboots", "When this card is Summoned: Set up to 2 \"Failsafe\" Artifacts from your Graveyard onto the field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Failsafe", targetCount: 2, upTo: true)),
                Fx("Dead Man's Dividend", "Once per turn, when an Artifact you control is destroyed: Draw 1 card.",
                    EffectTrigger.OnOwnArtifactDestroyed, 0, true,
                    Act(EffectActionType.DrawCards, 1)));
            deadman.reqOwnArtifactsInGrave = 3;
        }

        private static void W6Fethaerbreese()
        {
            Mon("Fethaerbreese Windreader", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Animal, 900, 1100,
                Fx("Read the Currents", "When this card is Summoned: You can return 1 other \"Fethaerbreese\" monster you control to your hand; if you do, draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, nameFilter: "Fethaerbreese", excludeSelf: true, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Favorable Winds", "Instead, pay 1 Mana: Also gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, nameFilter: "Fethaerbreese", excludeSelf: true, upTo: true),
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.GainMana, 1)));

            var stormfront = Mon("Fethaerbreese Stormfront", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Animal, 2300, 2000,
                Fx("Leading Edge", "When this card is Summoned: Return 1 monster your opponent controls with 1500 or less ATK to its owner's hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster, maxAtk: 1500)),
                Inf("Full Gale", "Instead, pay 3 Mana: No ATK limit.",
                    EffectTrigger.OnSummonSelf, 3, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)));
            stormfront.canSelfSpecialSummon = true;
            stormfront.selfSummonRequiresAttribute = true;
            stormfront.selfSummonRequiredAttribute = MonsterAttribute.Wind;
            stormfront.conditionalDoubleAttack = true;
            stormfront.doubleAttackAttribute = MonsterAttribute.Wind;

            Spell("Fethaerbreese Tailwind", CardRarity.Uncommon, false,
                Fx("Tailwind", "Special Summon up to 2 \"Fethaerbreese\" monsters from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, nameFilter: "Fethaerbreese", targetCount: 2, upTo: true)),
                Inf("Storm Surge", "Instead, pay 2 Mana: They gain 300 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, nameFilter: "Fethaerbreese", targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, nameFilter: "Fethaerbreese", targetCount: 2, upTo: true)));

            Spell("Fethaerbreese Gust Shield", CardRarity.Uncommon, true,
                Fx("Gust Shield", "Return 1 of your monsters to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster)),
                Inf("Cyclone Shelter", "Instead, pay 2 Mana: Also draw 1 card and gain 1 Mana.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.GainMana, 1)));

            Artifact("Fethaerbreese High Roost", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Watch from Above", "Once per turn, when a monster returns from your field to your hand: 1 monster you control gains 300 ATK until the end of this turn.",
                    EffectTrigger.OnOwnMonsterBounced, 0, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, upTo: true)),
                Inf("Dive from the Roost", "Pay 2 Mana: Special Summon 1 \"Fethaerbreese\" monster from your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, nameFilter: "Fethaerbreese")));

            var homingSky = Rel("Fethaerbreese, the Homing Sky", CardRarity.Legendary, 3,
                MonsterAttribute.Wind, MonsterType.Animal, 2800, 2500,
                "4+ monsters in your Graveyard. Cost 3 Mana.", 3,
                Fx("The Sky Calls Home", "When this card is Summoned: Return 1 monster your opponent controls to its owner's hand, then you can return 1 of YOUR monsters to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, excludeSelf: true, upTo: true)),
                Inf("Carried Home", "Pay 2 Mana: Return 1 of your monsters to your hand; draw 1 card.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 1)));
            homingSky.reqGraveyardMonstersAtLeast = 4;
        }

        private static void W6Forgeheart()
        {
            Mon("Forgeheart Smelter", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Mecha, 700, 1000,
                Fx("Feed the Furnace", "When this card is Summoned: Send 1 \"Forgeheart\" Artifact from your Deck to the Graveyard; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckCardFiltered, nameFilter: "Forgeheart"),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Reclaim the Slag", "Pay 2 Mana and banish this card from your Graveyard: add 1 Artifact from your Graveyard to your hand.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf)));

            var masterwork = Mon("Forgeheart Masterwork", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Mecha, 2600, 2200,
                Fx("Signature Piece", "When this card is Summoned: 1 monster you control permanently gains 300 ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.AllyMonster)),
                Inf("Master's Series", "Instead, pay 3 Mana: Up to 2 monsters; 400 each.",
                    EffectTrigger.OnSummonSelf, 3, true,
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.AllyMonster, targetCount: 2, upTo: true)));
            masterwork.canSelfSpecialSummon = true;
            masterwork.selfSummonRequiresArtifact = true;
            masterwork.battleShieldMinOwnArtifacts = 2;

            Spell("Forgeheart Temper", CardRarity.Uncommon, true,
                Fx("Temper", "1 monster you control gains 500 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster)),
                Inf("Perfect Temper", "Instead, pay 3 Mana: The bonus is permanent, and the monster gains 300 DEF permanently.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.BuffTargetAtk, 500, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetDef, 300, TargetKind.AllyMonster)));

            Spell("Forgeheart Recast Order", CardRarity.Uncommon, false,
                Fx("Recast Order", "Place 1 Artifact from your Graveyard directly into your Artifact Zone.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf)),
                Inf("Priority Order", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf),
                    Act(EffectActionType.DrawCards, 1)));

            var crucible = Artifact("Forgeheart Crucible", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Sacrificial Alloy", "Once per turn — pay 2 Mana: Destroy 1 other Artifact you control; 1 monster you control permanently gains 400 ATK.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, excludeSelf: true, isCost: true),
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.AllyMonster)));
            crucible.effects[0].oncePerTurn = true;
            crucible.auraAtkBonus = 200;
            crucible.auraUseTypeFilter = true;
            crucible.auraTypeFilter = MonsterType.Mecha;

            var crown = Rel("Forgeheart, the Molten Crown", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Mecha, 3100, 2500,
                "You control 2+ Artifacts and your Graveyard holds 5+ cards. Cost 4 Mana.", 4,
                Fx("Crowned in Slag", "When this card is Summoned: Place up to 2 Artifacts from your Graveyard onto the field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, targetCount: 2, upTo: true)),
                Inf("Forge Royal", "Pay 3 Mana: 1 monster you control permanently gains 500 ATK.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.BuffTargetAtk, 500, TargetKind.AllyMonster)));
            crown.reqOwnArtifactsOnField = 2;
            crown.reqGraveyardAtLeast = 5;
        }

        private static void W6Gaslight()
        {
            Mon("Gaslight Understudy", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Myth, 800, 600,
                Fx("Learn the Part", "When this card is Summoned: Summon 1 Illusion Token (0/0) to your opponent's field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)),
                Inf("Posthumous Performance", "Pay 2 Mana and banish this card from your Graveyard: summon 1 Illusion Token to your opponent's field.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)));

            var hypnotist = Mon("Gaslight Stage Hypnotist", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Myth, 1900, 1500,
                Fx("You Are Getting Sleepy", "Once per turn: Destroy 1 Illusion Token your opponent controls; 1 monster they control permanently loses 300 ATK.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, nameFilter: "Illusion", isCost: true),
                    Act(EffectActionType.DebuffTargetAtk, 300, TargetKind.EnemyMonster)));
            hypnotist.canSelfSpecialSummon = true;
            hypnotist.selfSummonChecksOpponentField = true;
            hypnotist.selfSummonRequiresOpponentMonsters = 2;
            hypnotist.passiveAtkPerCount = 300;
            hypnotist.passiveAtkPerCountKind = EffectCountKind.OpponentIllusionTokens;

            Spell("Gaslight House Lights", CardRarity.Uncommon, false,
                Fx("House Lights Up", "Summon 1 Illusion Token (0/0) to your opponent's field; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Blinding Spotlights", "Instead, pay 3 Mana: Summon 2 Tokens; draw 2.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 2),
                    Act(EffectActionType.DrawCards, 2)));

            Spell("Gaslight Now You See Me", CardRarity.Rare, true,
                Fx("Now You See Me", "1 \"Gaslight\" monster you control cannot be targeted this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.CannotBeTargetedThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Gaslight")),
                Inf("Now You Don't", "Instead, pay 2 Mana: Also summon 1 Illusion Token (0/0) to your opponent's field.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.CannotBeTargetedThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Gaslight"),
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)));

            Artifact("Gaslight Smoke Machine", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Fog the Entrance", "Once per turn, when your opponent Summons a monster: Summon 1 Illusion Token (0/0) to their field.",
                    EffectTrigger.OnOpponentSummon, 0, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)),
                Inf("Clear the Fog", "Pay 2 Mana: Destroy 1 Illusion Token; draw 1 card.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DestroyIllusionTokensDrawPer, 1, targetCount: 1)));

            var vanishing = Rel("Gaslight, the Vanishing Act", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Myth, 2500, 2100,
                "Your opponent controls 2+ monsters, one of them an Illusion Token. Cost 3 Mana.", 3,
                Fx("The Grand Vanish", "When this card is Summoned: Destroy all Illusion Tokens your opponent controls; draw 1 card for each (max 3).",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyIllusionTokensDrawPer, 5, targetCount: 3)),
                Inf("Encore of Nothing", "Pay 2 Mana: Summon 1 Illusion Token (0/0) to your opponent's field; 1 monster they control loses 400 ATK until the end of this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1),
                    Act(EffectActionType.DebuffTargetAtk, 400, TargetKind.EnemyMonster)));
            vanishing.reqOpponentNamedOnField = "Illusion";
            vanishing.reqOpponentMonstersAtLeast = 2;
        }
    }
}
