using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Baut die 115 Karten des August-2026-Batches (5 je Archetype + 25 Generics)
    /// in vier Stages und hängt sie in den Katalog.
    ///
    /// Läuft mehrfach: bestehende Assets werden überschrieben, nicht verdoppelt.
    /// Das Artwork bleibt dabei erhalten — sonst wäre jede Korrektur am Effekttext
    /// eine Runde Bildergenerieren.
    /// </summary>
    public static class Batch2026Builder
    {
        private const string MonsterDir  = "Assets/_Game/Data/Tcg/Monsters";
        private const string SpellDir    = "Assets/_Game/Data/Tcg/Spells";
        private const string ArtifactDir = "Assets/_Game/Data/Tcg/Artifacts";
        private const string RelicDir    = "Assets/_Game/Data/Tcg/Reliquary";
        private const string CatalogPath = "Assets/_Game/Data/Tcg/CardCatalog.asset";

        private static readonly List<CardDefinition> built = new List<CardDefinition>();

        [MenuItem("Rouge TCG/Build Batch 2026 — Stage 1")]
        public static void BuildStage1()
        {
            built.Clear();
            Tidebound();
            Gravemaw();
            Wyldpack();
            Hexweaver();
            Forgeheart();
            Finish("Stage 1");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Stage 2")]
        public static void BuildStage2()
        {
            built.Clear();
            Genostitched();
            Lyria();
            Archfiend();
            Barrierstruck();
            Heavenly();
            Finish("Stage 2");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Stage 3")]
        public static void BuildStage3()
        {
            built.Clear();
            Fethaerbreese();
            Lightless();
            DragonShrine();
            Kindlekin();
            Mechination2026();
            Finish("Stage 3");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Stage 4")]
        public static void BuildStage4()
        {
            built.Clear();
            Manacle2026();
            Sacrilegion2026();
            Sleightwind2026();
            Generics2026();
            Finish("Stage 4");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Loose Set (Puns)")]
        public static void BuildLooseSet()
        {
            built.Clear();
            LooseSetBodies();
            LooseSetTricks();
            LooseSetEconomy();
            LooseSetArtifacts();
            Finish("Loose Set");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — 5 Archetypes (Stun+Traps+...)")]
        public static void BuildFiveArchetypes()
        {
            built.Clear();
            Paperbound();
            Powderkeg();
            Trapline();
            Redactor();
            Snugglet();
            Finish("5 Archetypes");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Deckay (Mill)")]
        public static void BuildDeckay()
        {
            built.Clear();
            Deckay();
            Finish("Deckay");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Failsafe (Artifacts)")]
        public static void BuildFailsafe()
        {
            built.Clear();
            Failsafe();
            Finish("Failsafe");
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Deathpoem (Samurai)")]
        public static void BuildDeathpoem()
        {
            built.Clear();
            Deathpoem();
            Finish("Deathpoem");
        }

        // ---- DEATHPOEM (Fire / Samurai) · „Das Opfer IST der Effekt" ----
        //
        // Samurai schrieben vor dem Tod ihr Jisei, das Todesgedicht. Jeder
        // Deathpoem fällt von eigener Hand (Tribute als Kosten) und reißt dabei
        // etwas mit. Der Friedhof füllt sich — Vow, Duelist und die Reliquaries
        // leben davon.
        private static void Deathpoem()
        {
            Mon("Deathpoem Initiate", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Human, 800, 400,
                Fx("Opening Verse", "Pay 1 Mana and tribute this card: destroy 1 monster with 1500 or less ATK.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500)),
                Inf("Perfect Cut", "Pay 3 Mana instead: destroy 1 monster (no ATK limit).",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster)));

            Mon("Deathpoem Duelist", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Human, 1400, 200,
                Fx("Second Verse", "Pay 2 Mana and tribute this card: destroy 1 monster.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster)),
                Inf("Echo of the Blade", "Pay 2 Mana and banish this card from your Graveyard: Special Summon 1 Level 1 \"Deathpoem\" monster from your Graveyard.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        level: 1, nameFilter: "Deathpoem")));

            var calligrapher = Mon("Deathpoem Calligrapher", CardRarity.Uncommon, 1, MonsterAttribute.Fire, MonsterType.Human, 600, 600,
                Fx("Ink for the Fallen", "If this card is sent to the Graveyard: add 1 \"Deathpoem\" card from your Deck to your hand.",
                    EffectTrigger.OnSentToGraveyardSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered,
                        nameFilter: "Deathpoem")));
            calligrapher.auraAtkBonus = 200;
            calligrapher.auraNameFilter = "Deathpoem";
            calligrapher.auraExcludesSelf = true;

            Mon("Deathpoem Housebane", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Human, 2000, 1000,
                Fx("Verse of Ruin", "Pay 3 Mana and tribute this card: destroy up to 2 monsters.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster,
                        targetCount: 2, upTo: true)),
                Inf("Verse of Silence", "Pay 5 Mana instead: destroy up to 2 monsters AND return up to 1 Spell or Artifact on the field to its owner's hand.",
                    EffectTrigger.Ignition, 5, true,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster,
                        targetCount: 2, upTo: true),
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact, upTo: true)));

            Spell("Deathpoem Vow", CardRarity.Common, false,
                Fx("Recite the Verse", "Special Summon 1 \"Deathpoem\" monster from your Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Deathpoem")),
                Inf("Recite the Anthology", "Pay 3 Mana instead: Special Summon up to 2.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Deathpoem", targetCount: 2, upTo: true)));

            var finalVerse = Mon("Deathpoem, the Final Verse", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Human, 1800, 1600,
                Fx("Closing Line", "Pay 2 Mana and tribute this card: destroy 1 monster and return up to 1 Spell or Artifact to its owner's hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact, upTo: true)));
            finalVerse.canSelfSpecialSummon = true;
            finalVerse.selfSummonRequiresGraveNamedCount = 3;
            finalVerse.selfSummonRequiresGraveNamed = "Deathpoem";
            finalVerse.selfSummonPosition = BattlePosition.Attack;

            var stanza = Rel("Deathpoem, the Hundredth Stanza", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Human, 2400, 1800,
                "5+ monsters in your Graveyard — pay 3 Mana.", 3,
                Fx("Hundred Blades Falling", "If this card is Summoned: you can destroy up to 2 monsters your opponent controls.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster,
                        targetCount: 2, upTo: true)),
                Fx("The Book Closes", "Pay 3 Mana and tribute this card: destroy up to 3 monsters.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster,
                        targetCount: 3, upTo: true)));
            stanza.reqGraveyardMonstersAtLeast = 5;

            var unsigned = Rel("Deathpoem, Unsigned Verse", CardRarity.Rare, 2,
                MonsterAttribute.Fire, MonsterType.Human, 1000, 1500,
                "Your Graveyard holds 6+ cards — pay 2 Mana.", 2,
                Inf("Unwritten Ending", "Pay 2 Mana: this card cannot be destroyed this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ProtectSelfThisTurn)));
            unsigned.reqGraveyardAtLeast = 6;
            unsigned.passiveAtkPerCount = 100;
            unsigned.passiveAtkPerCountKind = EffectCountKind.OwnGraveyardCards;
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Apocrypha (Myth)")]
        public static void BuildApocrypha()
        {
            built.Clear();
            Apocrypha();
            Finish("Apocrypha");
        }

        // ---- APOCRYPHA (Light / Myth) · „Aus dem Buch gestrichen" ----
        //
        // Sphinx, Hydra, Chimera — Mythen, die die Chronisten verworfen haben.
        // Kontroll-Archetype: Bounce, Hand-Blick, Wiedergänger. Der Endboss
        // reißt mit NegateRestOfChain die komplette Kette unter sich aus dem
        // Buch — das letzte Wort hat immer die ungeschriebene Seite.
        private static void Apocrypha()
        {
            Mon("Apocrypha Roc", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Myth, 900, 500,
                Fx("Carried Off", "Pay 1 Mana: return 1 monster on the field to its owner's hand.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster)),
                Inf("Storm of Wings", "Pay 3 Mana instead: return up to 2 monsters.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster,
                        targetCount: 2, upTo: true)));

            Mon("Apocrypha Sphinx", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Myth, 1200, 1200,
                Fx("First Riddle", "When this card is Summoned: look at your opponent's hand and choose 1 card; they discard it.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.LookAndDiscardChosen, 1, TargetKind.HandCardOpponent)),
                Inf("Second Riddle", "Pay 2 Mana and banish this card from your Graveyard: look at your opponent's hand and choose 1 card; they discard it.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.LookAndDiscardChosen, 1, TargetKind.HandCardOpponent)));

            Mon("Apocrypha Chimera", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Myth, 1300, 900,
                Fx("Borrowed Shape", "Pay 1 Mana: copy the ATK and DEF of 1 monster on the field until the end of the turn.",
                    EffectTrigger.Quick, 1, false,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster)),
                Inf("Second Head", "Pay 2 Mana instead: copy, and this card can attack once more this Battle Phase.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster),
                    Act(EffectActionType.AttackAgainSelf)));

            Mon("Apocrypha Hydra", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Myth, 1800, 1200,
                Fx("Grow Back", "When this card is destroyed: you can Special Summon 1 \"Apocrypha\" monster from your Graveyard.",
                    EffectTrigger.OnDestroyedSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Apocrypha")),
                Inf("Two Heads", "Pay 2 Mana: Special Summon up to 2 instead.",
                    EffectTrigger.OnDestroyedSelf, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterSelf,
                        nameFilter: "Apocrypha", targetCount: 2, upTo: true)));

            Spell("Apocrypha Fable", CardRarity.Common, false,
                Fx("Retell the Tale", "Add 1 \"Apocrypha\" card from your Deck to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered,
                        nameFilter: "Apocrypha")));

            var cartographer = Mon("Apocrypha Cartographer", CardRarity.Rare, 2, MonsterAttribute.Light, MonsterType.Myth, 1100, 1400,
                Fx("Chart the Lost", "Pay 2 Mana and banish 1 card from your Graveyard: draw 1 card.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 1)));
            cartographer.passiveAtkPerCount = 200;
            cartographer.passiveAtkPerCountKind = EffectCountKind.OwnBanishedMonsters;

            var unwritten = Rel("Apocrypha, the Unwritten", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Myth, 2500, 2100,
                "3+ monsters in your Graveyard and 4+ Mana available — pay 2 Mana.", 2,
                Fx("The Last Word", "Pay 2 Mana: negate all previous links of the current chain.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.NegateRestOfChain)));
            unwritten.reqGraveyardMonstersAtLeast = 3;
            unwritten.reqMinMana = 4;

            var colophon = Rel("Apocrypha, Torn Colophon", CardRarity.Rare, 2,
                MonsterAttribute.Light, MonsterType.Myth, 1900, 1700,
                "Your opponent controls 2+ monsters — pay 2 Mana.", 2,
                Fx("Missing Page", "If this card is Summoned: return 1 card your opponent controls to its owner's hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemyCardOnField)).Mand());
            colophon.reqOpponentMonstersAtLeast = 2;
            colophon.passiveTaunt = true;
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Gaslight (Illusion)")]
        public static void BuildGaslight()
        {
            built.Clear();
            Gaslight();
            Finish("Gaslight");

            // Token-Referenz im Regelwerk verankern — die Engine spawnt darüber
            // (Server setzt sie nach dem CardLibrary-Load per Name).
            var rules = AssetDatabase.LoadAssetAtPath<GameRules>("Assets/_Game/Data/Tcg/GameRules.asset");
            var token = built.Find(c => c != null && c.isToken) as MonsterCardData;
            if (rules != null && token != null && rules.illusionToken != token)
            {
                rules.illusionToken = token;
                EditorUtility.SetDirty(rules);
                AssetDatabase.SaveAssets();
            }
        }

        // ---- GASLIGHT (Dark / Myth) · „Das eingeredete Feld" ----
        //
        // Illusionisten, die dem Gegner 0/0-Trugbilder aufs Feld reden: Die
        // Tokens verstopfen seine Zonen, und Gaslight-Karten ernten sie —
        // als Kartenvorteil, ATK-Skalierung oder Debuff. Tokens lösen sich
        // beim Verlassen des Feldes auf und zählen nie im Friedhof.
        private static void Gaslight()
        {
            var token = Mon("Illusion Token", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Myth, 0, 0);
            token.isToken = true;

            Mon("Gaslight Lanternist", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Myth, 700, 700,
                Fx("Remember This?", "When this card is Summoned: Summon 1 Illusion Token (0/0) to your opponent's field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)),
                Inf("Remember Both?", "Pay 2 Mana instead: Summon 2.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 2)));

            Spell("Gaslight Usher", CardRarity.Common, false,
                Fx("Right This Way", "Summon 2 Illusion Tokens (0/0) to your opponent's field.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 2)));

            Mon("Gaslight Mesmer", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Myth, 1100, 900,
                Fx("Shatter the Doubt", "Pay 1 Mana: destroy 1 Illusion Token your opponent controls; draw 1 card.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.DestroyIllusionTokensDrawPer, 1, targetCount: 1)),
                Inf("Shatter It All", "Pay 2 Mana instead: destroy up to 2; draw 1 for each.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.DestroyIllusionTokensDrawPer, 2, targetCount: 2)));

            Spell("Gaslight Mirrorwalk", CardRarity.Uncommon, true,
                Fx("And Another One", "When your opponent Summons a monster: Summon 1 Illusion Token (0/0) to their field.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)).InWindow(QuickWindow.SummonResponse));

            var charlatan = Mon("Gaslight Charlatan", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Myth, 1600, 1200,
                Inf("Nothing Up My Sleeve", "Pay 2 Mana: this card cannot be targeted by your opponent's effects this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.ImmuneTargetThisTurn, 1, TargetKind.SelfCard)));
            charlatan.passiveAtkPerCount = 300;
            charlatan.passiveAtkPerCountKind = EffectCountKind.OpponentIllusionTokens;

            Spell("Gaslight Curtain Call", CardRarity.Rare, false,
                Fx("The Reveal", "Destroy all Illusion Tokens your opponent controls; draw 1 card for each (max 3).",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DestroyIllusionTokensDrawPer, 99, targetCount: 3)));

            var premiere = Rel("Gaslight, the Grand Premiere", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Myth, 2200, 2000,
                "Your opponent controls 3+ monsters — pay 3 Mana.", 3,
                Fx("Full House", "If this card is Summoned: fill your opponent's empty Monster Zones with Illusion Tokens (0/0).",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 5)),
                Fx("Lights Down", "Pay 3 Mana: destroy all Illusion Tokens; 1 monster your opponent controls loses 400 ATK for each.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DestroyAllIllusionTokensDebuffTargetPer, 400, TargetKind.EnemyMonster)));
            premiere.reqOpponentMonstersAtLeast = 3;

            var ovation = Rel("Gaslight, Standing Ovation", CardRarity.Rare, 2,
                MonsterAttribute.Dark, MonsterType.Myth, 1700, 1600,
                "Your opponent controls an Illusion Token — pay 2 Mana.", 2,
                Fx("Encore", "If this card is Summoned: Summon 1 Illusion Token (0/0) to your opponent's field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SummonIllusionTokensToOpponent, 1)));
            ovation.reqOpponentNamedOnField = "Illusion Token";
            ovation.auraAtkBonus = 300;
            ovation.auraNameFilter = "Gaslight";
            ovation.auraExcludesSelf = true;
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Slowburn (Charged)")]
        public static void BuildSlowburn()
        {
            built.Clear();
            Slowburn();
            Finish("Slowburn");
        }

        // ---- SLOWBURN (Fire / Human) · „Die lange Lunte" ----
        //
        // Magier, die Quick-Spells SETZEN und schwelen lassen: sofort gezündet
        // schwach — liegt die Lunte eine volle Runde, zündet in der eigenen
        // Standby Phase automatisch die GELADENE Version (ChargedStandby).
        // Pyrekeeper und der Boss schließen die Lunte per Detonate kurz.
        private static void Slowburn()
        {
            Mon("Slowburn Candlewick", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Human, 600, 800,
                Fx("Lay the Fuse", "When this card is Summoned: you can set 1 \"Slowburn\" Spell from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered,
                        nameFilter: "Slowburn:")),
                Inf("Lay Both Fuses", "Pay 2 Mana instead: set up to 2 with different names.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered,
                        nameFilter: "Slowburn:", targetCount: 2, upTo: true, excludeSameName: true)));

            Mon("Slowburn Chandler", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Human, 1000, 1200,
                Fx("Rewick", "Pay 1 Mana: return 1 \"Slowburn\" card from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf,
                        nameFilter: "Slowburn")),
                Inf("Snuff the Flame", "Pay 2 Mana: your opponent cannot Special Summon this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.OpponentSummonLockThisTurn)));

            var pyrekeeper = Mon("Slowburn Pyrekeeper", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Human, 1700, 1300,
                Fx("Detonate", "Pay 3 Mana: trigger the CHARGED effect of 1 of your set \"Slowburn\" Spells that was set before this turn.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.DetonateChargedSpell, 1, TargetKind.AllySpellOrArtifact,
                        nameFilter: "Slowburn:")));
            pyrekeeper.auraDefBonus = 200;
            pyrekeeper.auraNameFilter = "Slowburn";
            pyrekeeper.auraExcludesSelf = true;

            Spell("Slowburn: Tripwire", CardRarity.Common, true,
                Fx("Fuse Lit", "Switch 1 monster your opponent controls to Defense Position.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster)),
                Fx("Charged: Firing Line", "Charged (auto in your Standby Phase): switch ALL your opponent's monsters to Defense Position.",
                    EffectTrigger.ChargedStandby, 0, false,
                    Act(EffectActionType.SwitchAllToDefense, 1)));

            Spell("Slowburn: Banked Flame", CardRarity.Uncommon, true,
                Fx("Fuse Lit", "Draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DrawCards, 1)),
                Fx("Charged: Flashover", "Charged (auto in your Standby Phase): draw 2 cards and gain 2 Mana this turn.",
                    EffectTrigger.ChargedStandby, 0, false,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainMana, 2)));

            Spell("Slowburn: Deep Coals", CardRarity.Rare, true,
                Fx("Fuse Lit", "1 monster your opponent controls loses 400 ATK.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DebuffTargetAtk, 400, TargetKind.EnemyMonster)),
                Fx("Charged: Eruption", "Charged (auto in your Standby Phase): destroy up to 2 monsters your opponent controls.",
                    EffectTrigger.ChargedStandby, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster,
                        targetCount: 2, upTo: true)));

            var patient = Rel("Slowburn, the Patient Flame", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Human, 2300, 1900,
                "2+ Spells in your Graveyard — pay 3 Mana.", 3,
                Fx("Long Game", "If this card is Summoned: you can set up to 2 \"Slowburn\" Spells from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered,
                        nameFilter: "Slowburn:", targetCount: 2, upTo: true)),
                Fx("Detonate", "Pay 2 Mana: trigger the CHARGED effect of 1 of your set \"Slowburn\" Spells that was set before this turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.DetonateChargedSpell, 1, TargetKind.AllySpellOrArtifact,
                        nameFilter: "Slowburn:")));
            patient.reqGraveyardSpellsAtLeast = 2;

            var backdraft = Rel("Slowburn, Backdraft", CardRarity.Rare, 2,
                MonsterAttribute.Fire, MonsterType.Human, 1800, 1400,
                "Your opponent controls more monsters than you — pay 2 Mana.", 2,
                Fx("Rush of Air", "If this card is Summoned: gain 2 Mana this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 2)).Mand(),
                Inf("Doorway Flare", "Pay 2 Mana: 1 monster your opponent controls loses 600 ATK.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DebuffTargetAtk, 600, TargetKind.EnemyMonster)));
            backdraft.reqOpponentMoreMonsters = true;
        }

        [MenuItem("Rouge TCG/Build Batch 2026 — Mimicrypt (Copy)")]
        public static void BuildMimicrypt()
        {
            built.Clear();
            Mimicrypt();
            Finish("Mimicrypt");
        }

        // ---- MIMICRYPT (Dark / Demon) · „Die Krypta der Nachahmer" ----
        //
        // Kopisten, die den GEGNERISCHEN Friedhof plündern: Zauber nachspielen,
        // Werte absaugen, Originale entleihen — und der König beschwört fremde
        // Tote aufs eigene Feld. Jede Karte stiehlt anders.
        private static void Mimicrypt()
        {
            Mon("Mimicrypt Ghoul", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Demon, 600, 900,
                Fx("Grave Robbery", "When this card is Summoned: banish 1 card from your opponent's Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent)),
                Inf("Double Haul", "Pay 1 Mana instead: banish up to 2.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent,
                        targetCount: 2, upTo: true)));

            Mon("Mimicrypt Understudy", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Demon, 1000, 1000,
                Fx("Steal the Scene", "Pay 2 Mana: choose 1 Spell in your opponent's Graveyard — resolve its effect as if it were yours.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.CopySpellFromOpponentGraveyard, 1, TargetKind.GraveyardSpellOpponent)));

            Spell("Mimicrypt Forgery", CardRarity.Uncommon, true,
                Fx("Perfect Fake", "1 monster you control copies the ATK and DEF of 1 monster your opponent controls until the end of the turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.AllyMonsterCopiesTargetStats, 1, TargetKind.EnemyMonster)));

            Spell("Mimicrypt Siphon", CardRarity.Common, false,
                Fx("Drain the Original", "1 monster your opponent controls loses 400 ATK; 1 monster you control gains 400 ATK.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DebuffTargetAtk, 400, TargetKind.EnemyMonster),
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.AllyMonster)));

            var archivist = Mon("Mimicrypt Archivist", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Demon, 1500, 1500,
                Fx("Borrow the Original", "Pay 3 Mana: take control of 1 monster your opponent controls until the End Phase.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.TakeControlUntilEndOfTurn, 1, TargetKind.EnemyMonster)));
            archivist.auraDefBonus = 200;
            archivist.auraNameFilter = "Mimicrypt";
            archivist.auraExcludesSelf = true;

            Spell("Mimicrypt Encore", CardRarity.Rare, false,
                Fx("Encore!", "Special Summon a copy of 1 monster your opponent controls to your field. It vanishes during the End Phase.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SummonCopyOfTarget, 1, TargetKind.EnemyMonster)));

            var king = Rel("Mimicrypt, the Borrowed King", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Demon, 1000, 1000,
                "Your opponent's Graveyard holds 8+ cards — pay 3 Mana.", 3,
                Fx("Crown of Mirrors", "If this card is Summoned: you can copy the ATK and DEF of 1 monster on the field until the end of the turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster)),
                Fx("Command the Dead", "Pay 3 Mana: Special Summon 1 monster from your OPPONENT's Graveyard to your field.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.GraveyardMonsterOpponent)));
            king.reqOpponentGraveyardAtLeast = 8;

            var palimpsest = Rel("Mimicrypt, Palimpsest", CardRarity.Rare, 2,
                MonsterAttribute.Dark, MonsterType.Demon, 1600, 1600,
                "Your opponent's Graveyard holds 6+ cards — pay 2 Mana.", 2,
                Fx("Scrape the Page", "If this card is Summoned: you can banish up to 2 cards from your opponent's Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent,
                        targetCount: 2, upTo: true)),
                Inf("Undertext", "Pay 2 Mana: copy the ATK and DEF of 1 monster on the field until the end of the turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster)));
            palimpsest.reqOpponentGraveyardAtLeast = 6;
        }

        // ---- FAILSAFE (Earth / Artefakt-Interrupts) · „Fällt eine Sicherung,
        // rastet die nächste ein" ----
        //
        // Going-first-Deck: Turn 1 werden Failsafe-Artefakte offen gelegt, jedes
        // trägt einen Quick-Interrupt für den Gegnerzug und ERSETZT sich nach
        // Gebrauch selbst durch das nächste Failsafe aus dem Deck. Zwei Slots,
        // Mana pro Interrupt und der Deck-Verbrauch balancieren die Kette.
        private static void Failsafe()
        {
            Mon("Failsafe Tinker", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Human, 500, 900,
                Fx("Install", "When this card is Summoned: you can set 1 \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered,
                        nameFilter: "Failsafe")));

            var carrier = Mon("Failsafe Carrier", CardRarity.Uncommon, 1, MonsterAttribute.Earth, MonsterType.Human, 700, 700,
                Fx("Salvage Parts", "When this card is Summoned: you can return 1 \"Failsafe\" card from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf,
                        nameFilter: "Failsafe")));
            carrier.canSelfSpecialSummon = true;
            carrier.selfSummonRequiresArtifact = true;
            carrier.selfSummonPosition = BattlePosition.Attack;
            UnityEditor.EditorUtility.SetDirty(carrier);

            var engineer = Mon("Failsafe Chief Engineer", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Human, 1600, 1400,
                Fx("Routine Maintenance", "Once per turn — pay 1 Mana: set 1 \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered,
                        nameFilter: "Failsafe")));
            engineer.passiveAtkPerCount = 200;
            engineer.passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;
            UnityEditor.EditorUtility.SetDirty(engineer);

            Artifact("Failsafe Seal", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Emergency Shutdown", "Pay 1 Mana: negate 1 monster your opponent controls until the end of the turn. Then send this card to the Graveyard and set 1 other \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.Quick, 1, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SendSelfToGraveyard),
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered,
                        nameFilter: "Failsafe", excludeSameName: true)));

            Artifact("Failsafe Damper", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Pressure Release", "Pay 1 Mana: 1 monster your opponent controls loses 500 ATK. Then send this card to the Graveyard and set 1 other \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.Quick, 1, false,
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster),
                    Act(EffectActionType.SendSelfToGraveyard),
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered,
                        nameFilter: "Failsafe", excludeSameName: true)));

            Artifact("Failsafe Bulkhead", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Seal the Breach", "Pay 1 Mana: you take no battle damage this turn. Then send this card to the Graveyard and set 1 other \"Failsafe\" Artifact from your Deck onto the field.",
                    EffectTrigger.Quick, 1, false,
                    Act(EffectActionType.PreventBattleDamageThisTurn),
                    Act(EffectActionType.SendSelfToGraveyard),
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered,
                        nameFilter: "Failsafe", excludeSameName: true)));

            Spell("Raise the Failsafes", CardRarity.Common, false,
                Fx("Bring Systems Online", "Set up to 2 \"Failsafe\" Artifacts from your Deck onto the field.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 2, TargetKind.DeckArtifactFiltered,
                        nameFilter: "Failsafe", targetCount: 2, upTo: true)));
        }

        // ---- DECKAY (Dark / Mill) · „Das Deck verfault, und genau davon lebt es" ----
        //
        // Alle Endphasen-Mills sind PFLICHT (mandatory) — der Motor läuft, ob man
        // will oder nicht. Die Mill-Trigger (OnMilledSelf & Co.) sind die Ernte.
        private static void Deckay()
        {
            Mon("Deckay Maggot", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 300, 400,
                Fx("Grave Tithe", "During either player's End Phase: mill 2 cards.",
                    EffectTrigger.EitherEndPhase, 0, false,
                    Act(EffectActionType.MillSelf, 2)).Mand(),
                Fx("Scent of Rot", "If this card is sent from the Deck to the Graveyard: you can add 1 \"Deckay\" Spell from your Deck to your hand.",
                    EffectTrigger.OnMilledSelf, 0, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckSpellFiltered, nameFilter: "Deckay")));

            Mon("Deckay Moth", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 400, 300,
                Fx("Dust Tithe", "During either player's End Phase: mill 1 card.",
                    EffectTrigger.EitherEndPhase, 0, false,
                    Act(EffectActionType.MillSelf, 1)).Mand(),
                Fx("Hatch from Ruin", "If this card is sent from the Deck to the Graveyard — pay 1 Mana: you can Special Summon 1 Level 1 \"Deckay\" monster with a different name from your Deck.",
                    EffectTrigger.OnMilledSelf, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFiltered,
                        level: 1, nameFilter: "Deckay", excludeSameName: true)),
                Inf("Cocoon Curse", "Banish this card from your Graveyard: Set 1 monster your opponent controls in face-down Defense Position.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)));

            Mon("Deckay Fiend", CardRarity.Uncommon, 1, MonsterAttribute.Dark, MonsterType.Demon, 500, 500,
                Fx("Feast of Endings", "During your opponent's End Phase: send this card from the field to the Graveyard, then mill 3 cards.",
                    EffectTrigger.OpponentEndPhase, 0, false,
                    Act(EffectActionType.SendSelfToGraveyard),
                    Act(EffectActionType.MillSelf, 3)).Mand(),
                Fx("Parting Spite", "If this card is sent to the Graveyard from anywhere — pay 1 Mana: burn 2 Mana from your opponent.",
                    EffectTrigger.OnSentToGraveyardSelf, 1, false,
                    Act(EffectActionType.DrainOpponentMana, 2)),
                Inf("Sweeter Spite", "Pay 2 Mana instead: gain 1000 LP.",
                    EffectTrigger.OnSentToGraveyardSelf, 2, true,
                    Act(EffectActionType.HealSelf, 1000)),
                Inf("Choke the Vault", "When your opponent Special Summons a Reliquary — discard this card: negate that Reliquary's effects for the rest of this turn.",
                    EffectTrigger.HandQuick, 2, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyMonster)).OnRelicSummon());

            // Die Selbstbeschwörung ist eine BEDINGUNG (kein aktivierbarer Effekt):
            // sie erscheint als Beschwörungs-Option ohne Kette und ohne Effekt-Slot.
            var leech = Mon("Deckay Leech", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Animal, 700, 900,
                Fx("Gorge Tithe", "During your End Phase: mill 4 cards.",
                    EffectTrigger.EndPhase, 0, false,
                    Act(EffectActionType.MillSelf, 4)).Mand(),
                Fx("Burrowed Loot", "If this card is sent from the Deck to the Graveyard — pay 1 Mana: add 1 \"Deckay\" Artifact from your Deck to your hand.",
                    EffectTrigger.OnMilledSelf, 1, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Deckay")),
                Inf("Unearth and Arm", "Pay 2 Mana instead: place it directly onto the field.",
                    EffectTrigger.OnMilledSelf, 2, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Deckay")));
            leech.canSelfSpecialSummon = true;
            leech.selfSummonRequiresMilled = true;
            leech.selfSummonPosition = BattlePosition.Attack;
            UnityEditor.EditorUtility.SetDirty(leech);

            var vulture = Mon("Deckay Vulture", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Animal, 900, 700,
                Fx("Wing Tithe", "During either player's End Phase: mill 3 cards.",
                    EffectTrigger.EitherEndPhase, 0, false,
                    Act(EffectActionType.MillSelf, 3)).Mand(),
                Fx("Feathered Ward", "If this card is sent from the Deck or your hand to the Graveyard: 1 monster you control cannot be targeted and is unaffected by your opponent's effects this turn.",
                    EffectTrigger.OnDiscardedOrMilledSelf, 0, false,
                    Act(EffectActionType.ImmuneTargetThisTurn, 1, TargetKind.AllyMonster, upTo: true)),
                Inf("Answer in Kind", "When your opponent Summons a Reliquary — discard this card: Special Summon 1 Reliquary from your Extra Deck (ignoring its Summon conditions, paying its Mana cost). It cannot use its On-Summon effects and is sent to the Graveyard during your next End Phase.",
                    EffectTrigger.HandQuick, 2, false,
                    Act(EffectActionType.SendSelfToGraveyard, isCost: true),
                    Act(EffectActionType.SummonReliquaryFromExtraSuppressed)).OnRelicSummon());
            vulture.canSelfSpecialSummon = true;
            vulture.selfSummonRequiresGraveNamedCount = 5;
            vulture.selfSummonRequiresGraveNamed = "Deckay";
            vulture.selfSummonPosition = BattlePosition.Attack;
            UnityEditor.EditorUtility.SetDirty(vulture);

            Spell("Deckay Rot", CardRarity.Rare, true,
                Fx("Spread the Rot", "Mill 5 cards. If a \"Deckay\" monster lies in your Graveyard afterwards, you can destroy 1 monster your opponent controls.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MillSelf, 5),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, upTo: true)),
                Inf("Bloom from Below", "Banish this card from your Graveyard: Special Summon 1 Level 2 or lower \"Deckay\" monster from your hand or Graveyard.",
                    EffectTrigger.GraveyardIgnition, 3, false,
                    Act(EffectActionType.BanishSelf, isCost: true),
                    Act(EffectActionType.SpecialSummonTargetFromHandOrGrave, 1, TargetKind.HandOrGraveMonsterFiltered,
                        nameFilter: "Deckay", maxAtk: 900)));

            Artifact("Signs of Deckay", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Reclaim the Lost", "During your Standby Phase: you can shuffle up to 2 cards from your Graveyard into your Deck.",
                    EffectTrigger.StandbyPhase, 0, false,
                    Act(EffectActionType.ShuffleGraveyardIntoDeck, 2, TargetKind.GraveyardCardSelf,
                        targetCount: 2, upTo: true)));

            var king = Rel("King of Deckay", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Demon, 2600, 2000,
                "8+ monsters in your Graveyard and 5+ Mana available — the Summon itself costs nothing.", 0,
                Fx("Coronation of Rot", "If this card is Summoned: mill 10 cards.",
                    EffectTrigger.OnSummonSelf, 0, false,
                    Act(EffectActionType.MillSelf, 10)).Mand(),
                Fx("Rot Consumes All", "Destroy all other cards on the field. Take 200 damage for each card destroyed.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.DestroyAllOthersSelfDamagePer, 200)),
                Inf("Crowned in Filth", "If you milled this or last turn: this card cannot be targeted and is unaffected by your opponent's effects this turn.",
                    EffectTrigger.Ignition, 4, false,
                    Act(EffectActionType.ImmuneTargetThisTurn, 1, TargetKind.SelfCard)).NeedsMilled());
            king.reqGraveyardMonstersAtLeast = 8;
            king.reqMinMana = 5;
            king.passiveBurnPerMill = 200;

            // ---- Erweiterung August 2026: 5 neue Deckay (approved) ----

            Mon("Deckay Worm", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Animal, 400, 200,
                Fx("Writhe Back", "Pay 1 Mana and banish 1 other \"Deckay\" card from your Graveyard: Special Summon this card from your Graveyard.",
                    EffectTrigger.GraveyardIgnition, 1, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf,
                        nameFilter: "Deckay", excludeSelf: true, isCost: true),
                    Act(EffectActionType.SpecialSummonTargetFromGraveOrBanish, 1, TargetKind.SelfCard)));

            Mon("Deckay Broodmother", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Animal, 500, 1200,
                Fx("Tend the Brood", "During your End Phase: mill 2 cards.",
                    EffectTrigger.EndPhase, 0, false,
                    Act(EffectActionType.MillSelf, 2)).Mand(),
                Fx("Renew the Nest", "If this card is sent from the Deck to the Graveyard: shuffle up to 3 cards from your Graveyard into your Deck.",
                    EffectTrigger.OnMilledSelf, 0, true,
                    Act(EffectActionType.ShuffleGraveyardIntoDeck, 3, TargetKind.GraveyardCardSelf,
                        targetCount: 3, upTo: true)));

            var glutton = Mon("Deckay Glutton", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Demon, 1200, 800,
                Fx("Endless Hunger", "During your End Phase: mill 2 cards.",
                    EffectTrigger.EndPhase, 0, false,
                    Act(EffectActionType.MillSelf, 2)).Mand());
            glutton.canSelfSpecialSummon = true;
            glutton.selfSummonRequiresGraveNamedCount = 10;   // leerer Filter = alle Karten
            glutton.selfSummonPosition = BattlePosition.Attack;
            glutton.passiveAtkPerCount = 100;
            glutton.passiveAtkPerCountKind = EffectCountKind.OwnBanishedMonsters;
            UnityEditor.EditorUtility.SetDirty(glutton);

            Spell("Deckay Swarm", CardRarity.Rare, true,
                Fx("Blot the Sky", "Mill 3 cards. Then you can have 1 monster your opponent controls lose 800 ATK.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MillSelf, 3),
                    Act(EffectActionType.DebuffTargetAtk, 800, TargetKind.EnemyMonster, upTo: true)));

            Spell("Feast of Deckay", CardRarity.Common, false,
                Fx("First Course", "Mill 2 cards. Draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MillSelf, 2),
                    Act(EffectActionType.DrawCards, 1)));
        }

        // ---- Deckay-Feinwürze für Effekt-Definitionen ----

        /// <summary>PFLICHT-Trigger: feuert ohne Nachfrage (Deckay-Endphasen-Mills).</summary>
        private static EffectDefinition Mand(this EffectDefinition effect)
        {
            effect.mandatory = true;
            return effect;
        }

        /// <summary>Reaktion NUR auf ein Reliquary-Summon des Gegners.</summary>
        private static EffectDefinition OnRelicSummon(this EffectDefinition effect)
        {
            effect.onlyReliquarySummonResponse = true;
            return effect;
        }

        /// <summary>Bedingung: in diesem oder dem vorherigen Zug gemillt.</summary>
        private static EffectDefinition NeedsMilled(this EffectDefinition effect)
        {
            effect.requireMilledLastTurn = true;
            return effect;
        }

        /// <summary>Bedingung: mindestens N Friedhofskarten mit diesem Namensteil.</summary>
        private static EffectDefinition NeedsGraveNamed(this EffectDefinition effect, int count, string filter)
        {
            effect.minOwnGraveyardNamed = count;
            effect.graveyardNamedFilter = filter;
            return effect;
        }

        /// <summary>SetDirty am Ende (CreateAsset schreibt sofort — siehe NewArchetypeBuilder) + Katalog.</summary>
        private static void Finish(string stage)
        {
            foreach (var card in built) EditorUtility.SetDirty(card);

            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            int added = 0;
            foreach (var card in built)
                if (!catalog.cards.Contains(card)) { catalog.cards.Add(card); added++; }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Batch2026] {stage}: {built.Count} Karten gebaut, {added} neu im Katalog ({catalog.cards.Count} gesamt).");
        }

        // ================== Bausteine ==================

        private static EffectAction Act(EffectActionType type, int amount = 1,
            TargetKind target = TargetKind.None, int level = 0,
            MonsterAttribute? attribute = null, MonsterType? monsterType = null,
            int targetCount = 1, bool upTo = false, int maxAtk = 0, bool isCost = false,
            bool excludeSelf = false, string nameFilter = "", string mentions = "",
            EffectCountKind countKind = EffectCountKind.OwnArtifactsOnField,
            bool excludeSameName = false)
        {
            var action = new EffectAction
            {
                type = type, amount = amount, target = target, levelFilter = level,
                targetCount = targetCount, upToTargets = upTo, maxAtkFilter = maxAtk,
                isCost = isCost, targetExcludesSelf = excludeSelf,
                nameFilter = nameFilter, mentionsFilter = mentions, countKind = countKind,
                excludeSameName = excludeSameName
            };
            if (attribute.HasValue) { action.useAttributeFilter = true; action.attributeFilter = attribute.Value; }
            if (monsterType.HasValue) { action.useTypeFilter = true; action.typeFilter = monsterType.Value; }
            return action;
        }

        /// <summary>Fenster-Beschränkung für Fallen-Zauber (AttackResponse/SummonResponse).</summary>
        private static EffectDefinition InWindow(this EffectDefinition effect, QuickWindow window)
        {
            effect.quickWindow = window;
            return effect;
        }

        /// <summary>Der "Set die nächste Falle"-Baustein: optional (upTo), anderer Name.</summary>
        private static EffectAction SetNextTrap() =>
            Act(EffectActionType.SetTargetSpellFromHand, 1, TargetKind.HandSpellFiltered,
                upTo: true, nameFilter: "Trapline", excludeSameName: true);

        private static ReliquaryCardData Rel(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            string summonText, int manaCost, params EffectDefinition[] effects)
        {
            var card = Make<ReliquaryCardData>(RelicDir, name, rarity, effects);
            card.level = level; card.attribute = attribute; card.monsterType = type;
            card.atk = atk; card.def = def;
            card.summonText = summonText; card.summonManaCost = manaCost;
            // Alles zurücksetzen, damit ein zweiter Lauf keine alten Bedingungen erbt
            card.reqNamedOnField = ""; card.reqNamedCount = 1;
            card.reqLifeBelowOpponent = false; card.reqOpponentMoreMonsters = false;
            card.reqOpponentMonstersAtLeast = 0; card.reqMinMana = 0;
            card.reqOwnArtifactsOnField = 0; card.reqOwnArtifactsInGrave = 0;
            card.reqOwnFaceDownMonsters = 0; card.reqMonsterWithEquip = false;
            card.reqGraveyardAtLeast = 0; card.reqControlNoMonsters = false;
            card.reqOwnMonstersAtLeast = 0; card.reqLifeAtMost = 0; card.reqBanishedAtLeast = 0;
            card.costBanishMonstersFromGrave = 0; card.costTributeOtherMonster = false;
            card.costTributeOwnMonsters = 0; card.costTributeOpponentMonsters = 0;
            card.canSelfSpecialSummon = false;
            return card;
        }

        private static EffectDefinition Fx(string label, string text, EffectTrigger trigger,
            int mana, bool oncePerTurn, params EffectAction[] actions)
        {
            return new EffectDefinition
            {
                label = label, text = text, trigger = trigger, manaCost = mana,
                oncePerTurn = oncePerTurn, isInfused = false,
                actions = new List<EffectAction>(actions)
            };
        }

        /// <summary>Infused-Effekt. coupled = Entweder-oder-Upgrade des Normal-Effekts darüber.</summary>
        private static EffectDefinition Inf(string label, string text, EffectTrigger trigger,
            int mana, bool coupled, params EffectAction[] actions)
        {
            var effect = Fx(label, text, trigger, mana, true, actions);
            effect.isInfused = true;
            effect.infusedKind = coupled ? InfusedKind.Coupled : InfusedKind.Standalone;
            return effect;
        }

        /// <summary>Aktivierungs-Bedingungen anhängen (minMana, Feld-/Hand-Vergleiche, Equip-Pflicht).</summary>
        private static EffectDefinition Needs(this EffectDefinition effect, int minMana = 0,
            int minOwnMonsters = 0, int minFaceDown = 0, int minGrave = 0,
            bool oppMoreHand = false, bool oppMoreMonsters = false, bool equip = false)
        {
            effect.minMana = minMana;
            effect.minOwnMonsters = minOwnMonsters;
            effect.minOwnFaceDownMonsters = minFaceDown;
            effect.minOwnGraveyardCards = minGrave;
            effect.requireOpponentMoreHandCards = oppMoreHand;
            effect.requireOpponentMoreMonsters = oppMoreMonsters;
            effect.requiresEquippedArtifact = equip;
            return effect;
        }

        // ================== Asset-Anlage ==================

        private static string FileName(string cardName) =>
            cardName.Replace(",", "").Replace("'", "").Replace(":", "").Replace(" ", "");

        private static T Make<T>(string dir, string cardName, CardRarity rarity,
            params EffectDefinition[] effects) where T : CardDefinition
        {
            Directory.CreateDirectory(dir);
            string path = $"{dir}/{FileName(cardName)}.asset";
            var card = AssetDatabase.LoadAssetAtPath<T>(path);
            bool fresh = card == null;
            if (fresh) card = ScriptableObject.CreateInstance<T>();

            card.cardName = cardName;
            card.rarity = rarity;
            card.effects = new List<EffectDefinition>(effects);
            // artwork wird bewusst NICHT angefasst — ein zweiter Lauf soll keine
            // schon zugewiesenen Bilder verlieren.

            // Alle Passiv-Felder zurücksetzen, damit ein zweiter Lauf nichts erbt
            card.isToken = false;
            card.auraAtkBonus = 0; card.auraDefBonus = 0; card.auraNameFilter = "";
            card.auraUseTypeFilter = false; card.auraLevelFilter = 0;
            card.auraOnlyFaceDown = false; card.auraExcludesSelf = false;
            card.passiveTaunt = false; card.battleShieldMinOwnArtifacts = 0;
            card.tributeWorth = 1; card.protectsNamedFromTargeting = "";
            card.conditionalDoubleAttack = false;
            card.passiveAtkPerCount = 0; card.passiveDefPerCount = 0;
            card.passiveCannotAttack = false; card.passiveNoAttackOnSummonTurn = false;
            card.passiveBurnPerMill = 0;

            if (fresh) AssetDatabase.CreateAsset(card, path);
            else EditorUtility.SetDirty(card);
            built.Add(card);
            return card;
        }

        private static MonsterCardData Mon(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = Make<MonsterCardData>(MonsterDir, name, rarity, effects);
            card.level = level; card.attribute = attribute; card.monsterType = type;
            card.atk = atk; card.def = def;
            card.canSelfSpecialSummon = false;
            card.selfSummonRequiresNameOnField = "";
            card.selfSummonRequiredNameCount = 1;
            card.selfSummonRequiresOpponentMonsters = 0;
            card.selfSummonRequiresAttribute = false;
            card.selfSummonRequiresArtifact = false;
            card.selfSummonRequiresFaceDownOnField = false;
            card.selfSummonChecksOpponentField = false;
            card.selfSummonRequiresMilled = false;
            card.selfSummonRequiresGraveNamedCount = 0;
            card.selfSummonRequiresGraveNamed = "";
            card.selfSummonPosition = BattlePosition.Defense;
            card.passiveAtkPerCount = 0;
            card.passiveDefPerCount = 0;
            return card;
        }

        private static SpellCardData Spell(string name, CardRarity rarity, bool quick,
            params EffectDefinition[] effects)
        {
            var card = Make<SpellCardData>(SpellDir, name, rarity, effects);
            card.speed = quick ? SpellSpeed.Quick : SpellSpeed.Normal;
            return card;
        }

        private static ArtifactCardData Artifact(string name, CardRarity rarity, ArtifactSlot slot,
            int atkBonus = 0, int defBonus = 0, params EffectDefinition[] effects)
        {
            var card = Make<ArtifactCardData>(ArtifactDir, name, rarity, effects);
            card.slot = slot; card.atkBonus = atkBonus; card.defBonus = defBonus;
            card.protectTypeFromEffectDestruction = false;
            card.redirectDestructionToSelf = false;
            card.countsAsNameOnField = "";
            card.firstSpellDiscountPerTurn = 0;
            card.protectsFaceDownWhileNamedFaceUp = "";
            return card;
        }

        // ================== STAGE 1 ==================

        // ---- TIDEBOUND (Water / Myth) · „Das Meer gibt alles zurück" ----
        private static void Tidebound()
        {
            Mon("Tidebound Backwash", CardRarity.Common, 1, MonsterAttribute.Water, MonsterType.Myth, 800, 1400,
                Fx("Backwash", "When this card is Summoned: You can return 1 other monster you control to your hand; if you do, draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Undertow", "Instead, pay 2 Mana: Return 1 other monster on either field to its owner's hand.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, excludeSelf: true)));

            Mon("Tidebound Beachcomber", CardRarity.Rare, 3, MonsterAttribute.Water, MonsterType.Myth, 2500, 2100,
                Fx("Combing the Shallows", "When this card is Summoned: You can pay 2 Mana; return 1 Spell or Artifact your opponent controls to the hand.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact)),
                Inf("Spring Tide", "Instead, pay 4 Mana: Return 1 Spell or Artifact AND 1 monster your opponent controls to the hand.",
                    EffectTrigger.OnSummonSelf, 4, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)),
                Fx("Finders Keepers", "Once per turn, when a card is returned from the field to your opponent's hand: Gain 1 Mana.",
                    EffectTrigger.OnEnemyCardBounced, 0, true,
                    Act(EffectActionType.GainMana, 1)));

            Spell("Tidebound Wave Goodbye", CardRarity.Uncommon, true,
                Fx("Wave Goodbye", "Pay 1 Mana: Return 1 monster with 1500 or less ATK to its owner's hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, maxAtk: 1500)),
                Inf("The Long Goodbye", "Instead, pay 3 Mana: Return 1 monster of any ATK to its owner's hand, then draw 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Tidebound Message in a Bottle", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Uncorked", "Once per turn: Pay 1 Mana; return 1 \"Tidebound\" monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Tidebound")),
                Inf("Answered Message", "Instead, pay 3 Mana: Special Summon 1 \"Tidebound\" monster from your Graveyard face-down.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Tidebound")));

            Spell("Tidebound Ebb and Flow", CardRarity.Uncommon, false,
                Fx("Ebb", "Return up to 2 monsters you control to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, targetCount: 2, upTo: true)),
                Inf("Flow", "Instead, pay 2 Mana: Return 2 monsters you control to your hand; draw 2 cards.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, targetCount: 2),
                    Act(EffectActionType.DrawCards, 2)));
        }

        // ---- GRAVEMAW (Dark / Demon) · „Wir werfen nichts weg" ----
        private static void Gravemaw()
        {
            Mon("Gravemaw Nibbler", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Demon, 700, 900,
                Fx("Grazing", "When this card is Summoned: Send the top 2 cards of your Deck to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillSelf, 2)),
                Inf("Gorging", "Instead, pay 1 Mana: Send the top 4 cards of your Deck to the Graveyard; gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.MillSelf, 4),
                    Act(EffectActionType.GainMana, 1)));

            Mon("Gravemaw Bonepicker", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Demon, 2600, 1800,
                Fx("Pick the Bones", "When this card is Summoned: Banish up to 2 monsters from your Graveyard; this card gains 300 ATK for each of your banished monsters.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.BuffSelfAtkPerCount, 300, countKind: EffectCountKind.OwnBanishedMonsters)),
                Inf("Pick Them Clean", "Instead, pay 2 Mana: Banish up to 4 monsters from your Graveyard; this card gains 300 ATK for each of your banished monsters.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardMonsterSelf, targetCount: 4, upTo: true),
                    Act(EffectActionType.BuffSelfAtkPerCount, 300, countKind: EffectCountKind.OwnBanishedMonsters)),
                Fx("A Bone to Pick", "Once per turn: Pay 2 Mana; return 1 of your banished monsters to your Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedMonsterSelf)));

            Spell("Gravemaw Stolen Supper", CardRarity.Uncommon, true,
                Fx("Stolen Supper", "Pay 1 Mana: Banish 1 card from your opponent's Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent)),
                Inf("Cleared Table", "Instead, pay 2 Mana: Banish up to 3 cards from your opponent's Graveyard; gain 300 LP.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent, targetCount: 3, upTo: true),
                    Act(EffectActionType.HealSelf, 300)));

            Artifact("Gravemaw Cold Storage", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Thaw", "Once per turn: Pay 2 Mana; Special Summon 1 \"Gravemaw\" monster from your Graveyard face-down.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Gravemaw")),
                Inf("Flash Thaw", "Instead, pay 4 Mana: Special Summon 1 \"Gravemaw\" monster from your Graveyard face-up.",
                    EffectTrigger.Ignition, 4, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Gravemaw")));

            Spell("Gravemaw Leftovers", CardRarity.Common, false,
                Fx("Scrape the Plate", "Return up to 2 of your banished \"Gravemaw\" cards to your Graveyard.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true, nameFilter: "Gravemaw")),
                Inf("Midnight Snack", "Instead, pay 1 Mana: Return up to 2 of your banished \"Gravemaw\" cards to your Graveyard; draw 1 card.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true, nameFilter: "Gravemaw"),
                    Act(EffectActionType.DrawCards, 1)));
        }

        // ---- WYLDPACK (Wind / Beast) · „Das Rudel zählt" ----
        private static void Wyldpack()
        {
            Mon("Wyldpack Underdog", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Beast, 500, 500,
                Fx("Underdog Story", "When this card is Summoned, if your opponent controls more monsters than you: Draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)).Needs(oppMoreMonsters: true),
                Inf("Against All Odds", "Instead, pay 1 Mana: Draw 2 cards instead.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.DrawCards, 2)).Needs(oppMoreMonsters: true));

            Mon("Wyldpack Fetch", CardRarity.Uncommon, 2, MonsterAttribute.Wind, MonsterType.Beast, 1400, 1600,
                Fx("Fetch!", "When this card is Summoned: Return 1 \"Wyldpack\" monster from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Wyldpack")),
                Inf("Good Dog", "Instead, pay 2 Mana: Return any 1 BEAST monster from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, monsterType: MonsterType.Beast)));

            Spell("Wyldpack Hackles", CardRarity.Uncommon, true,
                Fx("Raised Hackles", "Pay 1 Mana: 1 \"Wyldpack\" monster you control gains 700 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 700, TargetKind.AllyMonster, nameFilter: "Wyldpack")),
                Inf("The Whole Pack Bristles", "Instead, pay 2 Mana: Up to 5 \"Wyldpack\" monsters you control gain 400 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.AllyMonster, targetCount: 5, upTo: true, nameFilter: "Wyldpack")));

            var topDog = Artifact("Wyldpack Top Dog", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Alpha's Share", "Once per turn: Pay 1 Mana; 1 \"Wyldpack\" monster you control gains 300 ATK permanently.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.AllyMonster, nameFilter: "Wyldpack")));
            topDog.auraAtkBonus = 200;
            topDog.auraNameFilter = "Wyldpack";

            Spell("Wyldpack Off the Leash", CardRarity.Rare, false,
                Fx("Slip the Collar", "Pay 2 Mana: Up to 5 BEAST monsters you control gain 400 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.AllyMonster, targetCount: 5, upTo: true, monsterType: MonsterType.Beast)),
                Inf("Never Coming Back", "Instead, pay 4 Mana: Up to 5 BEAST monsters you control gain 400 ATK permanently.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.AllyMonster, targetCount: 5, upTo: true, monsterType: MonsterType.Beast)));
        }

        // ---- HEXWEAVER (Dark / Human) · „Mana ist Faden" ----
        private static void Hexweaver()
        {
            Mon("Hexweaver Loose Thread", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 600, 1200,
                Fx("Pull the Thread", "When this card is Summoned: Gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 1)),
                Inf("Keep Pulling", "Instead, pay 1 Mana: Gain 1 Mana now and 1 more during your next turn.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.GainMana, 1),
                    Act(EffectActionType.GainManaNextTurn, 1)));

            Mon("Hexweaver, Looming Large", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2300, 2100,
                Fx("Woven Wisdom", "When this card is Summoned, if you have 6 or more Mana: Draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 2)).Needs(minMana: 6),
                Inf("Force the Weave", "Instead, pay 2 Mana: Draw 2 cards regardless of your Mana.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DrawCards, 2)),
                Fx("Rethreaded Fate", "Once per turn: Pay 3 Mana; return 1 monster your opponent controls to its owner's hand.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)));

            Spell("Hexweaver Unravel", CardRarity.Uncommon, true,
                Fx("Unravel", "Pay 2 Mana: Return 1 Spell or Artifact your opponent controls to the hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact)),
                Inf("Unmade", "Instead, pay 4 Mana: Banish 1 Spell or Artifact your opponent controls.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.EnemySpellOrArtifact)));

            var bobbin = Artifact("Hexweaver Bargain Bobbin", CardRarity.Rare, ArtifactSlot.Field, 0, 0);
            bobbin.firstSpellDiscountPerTurn = 1;

            Spell("Hexweaver Rethread", CardRarity.Uncommon, false,
                Fx("Rethread", "Return 1 Spell from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, excludeSelf: true)),
                Inf("Double Stitch", "Instead, pay 1 Mana: Return up to 2 Spells from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, targetCount: 2, upTo: true, excludeSelf: true)));
        }

        // ---- FORGEHEART (Fire / Mecha) · „Nichts verlässt die Werkstatt fertig" ----
        private static void Forgeheart()
        {
            Mon("Forgeheart Apprentice-Piece", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Mecha, 900, 700,
                Fx("Journeyman's Errand", "When this card is Summoned: Add 1 \"Forgeheart\" Artifact from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Forgeheart")),
                Inf("Masterwork Delivery", "Instead, pay 2 Mana: Place 1 \"Forgeheart\" Artifact from your Deck directly into your Artifact Zone.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Forgeheart")));

            var ironclad = Mon("Forgeheart Ironclad Argument", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Mecha, 2700, 2300);
            ironclad.passiveAtkPerCount = 300;
            ironclad.passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;
            // 2 statt 3: es gibt nur zwei Artefakt-Zonen — "volles Artefakt-Brett" ist die Bedingung
            ironclad.battleShieldMinOwnArtifacts = 2;

            Spell("Forgeheart Quench", CardRarity.Common, true,
                Fx("Quench", "Pay 1 Mana: 1 monster you control gains 800 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 800, TargetKind.AllyMonster)),
                Inf("Tempered", "Instead, pay 2 Mana: 1 monster you control gains 800 DEF permanently.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BuffTargetDef, 800, TargetKind.AllyMonster)));

            Artifact("Forgeheart Spare Parts", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Spare Parts", "Once per turn: Pay 1 Mana; return 1 Artifact from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf)),
                Inf("Refitted", "Instead, pay 3 Mana: Place 1 Artifact from your Graveyard directly into your Artifact Zone.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf)));

            Spell("Forgeheart Scrap Deal", CardRarity.Uncommon, false,
                Fx("Scrap", "Destroy 1 Artifact you control; draw 2 cards.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Haggle", "Instead, pay 1 Mana: Destroy 1 Artifact you control; draw 2 cards and gain 2 Mana.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainMana, 2)));
        }

        // ================== STAGE 2 ==================

        // ---- GENOSTITCHED (Dark / Mecha) · „Fleisch ist nur die erste Schicht" ----
        private static void Genostitched()
        {
            Mon("Genostitched Hand-Me-Down", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Mecha, 600, 800,
                Fx("Hand-Me-Down", "When this card is Summoned: You can equip 1 Artifact from your Graveyard to this card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.EquipTargetArtifactToSelf, 1, TargetKind.GraveyardArtifactSelf)),
                Inf("Tailored Fit", "Instead, pay 2 Mana: Equip 1 Artifact from your Deck to this card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.EquipTargetArtifactToSelf, 1, TargetKind.DeckArtifactFiltered)));

            var dressed = Mon("Genostitched Dressed to Kill", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Mecha, 2500, 2000,
                Fx("Killing Fit", "Once per turn, if this card has an equipped Artifact: Pay 2 Mana; this card can attack an additional time this Battle Phase.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.SelfCard)).Needs(equip: true));
            dressed.passiveAtkPerCount = 400;
            dressed.passiveAtkPerCountKind = EffectCountKind.EquippedArtifactsOnSelf;

            Spell("Genostitched Quick Change", CardRarity.Uncommon, true,
                Fx("Quick Change", "Pay 1 Mana: Move 1 Artifact you control onto your strongest monster.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.MoveTargetArtifactToStrongestMonster, 0, TargetKind.AllyArtifact)),
                Inf("Showtime", "Instead, pay 2 Mana: The new bearer also gains 400 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.MoveTargetArtifactToStrongestMonster, 400, TargetKind.AllyArtifact)));

            Artifact("Genostitched Third Arm", CardRarity.Uncommon, ArtifactSlot.Monster, 500, 0,
                Fx("Extra Reach", "Once per turn, when the equipped monster destroys a monster in battle: Draw 1 card.",
                    EffectTrigger.OnBearerBattleKill, 0, true,
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Genostitched Loose Ends", CardRarity.Uncommon, false,
                Fx("Cut the Thread", "Destroy 1 Artifact you control; draw 2 cards.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Tie Off", "Instead, pay 2 Mana: Also return 1 \"Genostitched\" monster from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Genostitched")));
        }

        // ---- LYRIA (Light / Human) · „Die beste Musik hört man nicht kommen" ----
        private static void Lyria()
        {
            Mon("Lyria Hushabye", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Human, 700, 1000,
                Fx("Hushabye", "When this card is Summoned: You can Set 1 \"Lyria\" monster from your hand face-down.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered, nameFilter: "Lyria")),
                Inf("Sleep Tight", "Instead, pay 2 Mana: Set 1 monster of any name from your hand face-down, and draw 1 card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered),
                    Act(EffectActionType.DrawCards, 1)));

            Mon("Lyria Curtain Call", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Human, 2400, 2200,
                Fx("Curtain Call", "When this card is Summoned: Flip up to 2 of your face-down monsters face-up.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterSelf, targetCount: 2, upTo: true)),
                Inf("Standing Ovation", "Instead, pay 2 Mana: The flipped monsters also gain 400 ATK until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.FlipTargetFaceUp, 400, TargetKind.FaceDownMonsterSelf, targetCount: 2, upTo: true)),
                Fx("Take a Bow", "Once per turn: Pay 1 Mana; turn this card face-down into Defense Position.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.SelfCard)));

            Spell("Lyria Quiet Crescendo", CardRarity.Uncommon, true,
                Fx("Quiet Crescendo", "Pay 1 Mana: 1 monster you control gains 300 ATK for each of your face-down monsters, until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkPerCountEot, 300, TargetKind.AllyMonster, countKind: EffectCountKind.OwnFaceDownMonsters)),
                Inf("Fortissimo", "Instead, pay 3 Mana: The bonus is permanent.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.BuffTargetAtkPerCountPermanent, 300, TargetKind.AllyMonster, countKind: EffectCountKind.OwnFaceDownMonsters)));

            var greenRoom = Artifact("Lyria Green Room", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Places, Everyone", "Once per turn: Pay 2 Mana; Special Summon 1 \"Lyria\" monster from your Graveyard face-down.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Lyria")));
            greenRoom.protectsFaceDownWhileNamedFaceUp = "Lyria";

            Spell("Lyria Second Movement", CardRarity.Uncommon, false,
                Fx("Second Movement", "Set 1 monster from your Graveyard face-down on your field.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf)),
                Inf("Reprise", "Instead, pay 3 Mana: Set 2 \"Lyria\" monsters from your Graveyard face-down; draw 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, nameFilter: "Lyria"),
                    Act(EffectActionType.DrawCards, 1)));
        }

        // ---- ARCHFIEND (Dark / Demon) · „Jeder Handel hat Kleingedrucktes" ----
        private static void Archfiend()
        {
            Mon("Archfiend Matchmaker", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Demon, 900, 600,
                Fx("Strike a Match", "When this card is Summoned: Send 1 \"Archfiend\" card from your Deck to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckCardFiltered, nameFilter: "Archfiend")),
                Inf("Perfect Match", "Instead, pay 1 Mana: Send 1 \"Archfiend\" card from your Deck to the Graveyard, then add 1 other \"Archfiend\" card from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckCardFiltered, nameFilter: "Archfiend"),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Archfiend")));

            Mon("Archfiend Hatchet Man", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Demon, 2500, 2100,
                Fx("Hatchet Job", "When this card is Summoned: Destroy 1 monster your opponent controls with 1500 or less ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500)),
                Inf("Contract Work", "Instead, pay 3 Mana: Destroy 1 monster your opponent controls with 2500 or less ATK.",
                    EffectTrigger.OnSummonSelf, 3, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2500)),
                Fx("Bury the Hatchet", "Once per turn: Pay 2 Mana; banish 1 monster from your Graveyard; gain 300 LP.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardMonsterSelf),
                    Act(EffectActionType.HealSelf, 300)));

            Spell("Archfiend Devil's Advocate", CardRarity.Uncommon, true,
                Fx("Devil's Advocate", "Pay 1 Mana and discard 1 card; return 1 \"Archfiend\" monster from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true, excludeSelf: true),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend")),
                Inf("Case Won", "Instead, pay 3 Mana and discard 1 card: Special Summon 1 \"Archfiend\" monster from your Graveyard face-down.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true, excludeSelf: true),
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend")));

            var crown = Artifact("Archfiend Heavy Is the Crown", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Heavy Is the Crown", "During your Standby Phase: Return \"Archfiend Crown\" from your Graveyard to your hand.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, nameFilter: "Archfiend Crown")));
            crown.auraAtkBonus = 200;
            crown.auraUseTypeFilter = true;
            crown.auraTypeFilter = MonsterType.Demon;

            Spell("Archfiend Devil's Bargain", CardRarity.Rare, false,
                Fx("The Bargain", "Destroy 1 monster you control; draw 2 cards.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("The Fine Print", "Instead, pay 1 Mana: Also Special Summon 1 \"Archfiend\" monster from your Graveyard face-down.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Archfiend")));
        }

        // ---- BARRIERSTRUCK (Earth / Mecha) · „Gebaut wird für immer" ----
        private static void Barrierstruck()
        {
            Mon("Barrierstruck Bricklayer", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Mecha, 500, 1500,
                Fx("Lay the Foundation", "When this card is Summoned: Send 1 Artifact from your Deck to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SendTargetFromDeckToGraveyard, 1, TargetKind.DeckArtifactFiltered)),
                Inf("Measure Twice", "Instead, pay 1 Mana: Add it to your hand instead.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered)));

            var peacekeeper = Mon("Barrierstruck Peacekeeper", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Mecha, 1000, 2200,
                Fx("Final Warning", "Once per turn: Pay 2 Mana; change 1 monster your opponent controls to Defense Position.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster)));
            peacekeeper.passiveCannotAttack = true;
            peacekeeper.auraDefBonus = 300;
            peacekeeper.auraExcludesSelf = true;

            Mon("Barrierstruck, Set in Stone", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Mecha, 1800, 2800,
                Fx("Set in Stone", "When this card is Summoned: It gains 200 DEF for each Artifact in your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffSelfDefPerCount, 200, countKind: EffectCountKind.OwnGraveyardArtifacts)),
                Inf("Written in Stone", "Instead, pay 2 Mana: It also gains the same amount of ATK.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.BuffSelfDefPerCount, 200, countKind: EffectCountKind.OwnGraveyardArtifacts),
                    Act(EffectActionType.BuffSelfAtkPerCount, 200, countKind: EffectCountKind.OwnGraveyardArtifacts)));

            Spell("Barrierstruck Cold Shoulder", CardRarity.Uncommon, true,
                Fx("Cold Shoulder", "Pay 1 Mana: Change 1 monster you control to Defense Position; 1 monster you control gains 800 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 800, TargetKind.AllyMonster)),
                Inf("Stone Cold", "Instead, pay 2 Mana: Change ALL your monsters to Defense Position; up to 5 of them gain 800 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SwitchAllToDefense, 2),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 800, TargetKind.AllyMonster, targetCount: 5, upTo: true)));

            Artifact("Barrierstruck Load-Bearing Wall", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Bear the Load", "Once per turn: Pay 1 Mana; 1 monster you control gains 400 DEF permanently.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetDef, 400, TargetKind.AllyMonster)));
        }

        // ---- HEAVENLY (Light / Angel) · „Das Licht hat Personal" ----
        private static void Heavenly()
        {
            Mon("Heavenly Errand Angel", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Angel, 800, 1300,
                Fx("Small Miracles", "When this card is Summoned: Add 1 \"Heavenly\" Spell from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckSpellFiltered, nameFilter: "Heavenly")),
                Inf("Special Delivery", "Instead, pay 2 Mana: Also gain 300 LP.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckSpellFiltered, nameFilter: "Heavenly"),
                    Act(EffectActionType.HealSelf, 300)));

            var bodyguard = Mon("Heavenly Bodyguard", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Angel, 2200, 2600);
            bodyguard.protectsNamedFromTargeting = "Heavenly Seraph Sovereign";
            bodyguard.passiveTaunt = true;

            Spell("Heavenly Intervention", CardRarity.Uncommon, true,
                Fx("Intervention", "Pay 2 Mana: Return 1 monster your opponent controls to its owner's hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)),
                Inf("Divine Veto", "Instead, pay 4 Mana: Banish it instead.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.BanishTargetMonster, 1, TargetKind.EnemyMonster)));

            Artifact("Heavenly Collection Plate", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Pass the Plate", "Once per turn: Gain 300 LP.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.HealSelf, 300)),
                Inf("Generous Sunday", "Instead, pay 2 Mana, if you control 2+ monsters: Gain 300 LP and draw 1 card.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.HealSelf, 300),
                    Act(EffectActionType.DrawCards, 1)).Needs(minOwnMonsters: 2));

            Spell("Heavenly Second Coming", CardRarity.Rare, false,
                Fx("Second Coming", "Pay 3 Mana: Special Summon 1 \"Heavenly\" monster from your Graveyard.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Heavenly")),
                Inf("In Glory", "Instead, pay 5 Mana: It also gains 500 ATK and 500 DEF permanently.",
                    EffectTrigger.OnActivate, 5, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 500, TargetKind.GraveyardMonsterSelf, nameFilter: "Heavenly")));
        }

        // ================== STAGE 3 ==================

        // ---- FETHAERBREESE (Wind / Animal) · „Was fliegt, kommt wieder" ----
        private static void Fethaerbreese()
        {
            Mon("Fethaerbreese Featherweight", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Animal, 800, 800,
                Fx("Featherweight", "When this card is Summoned: You can discard 1 card; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Punching Up", "Instead, pay 1 Mana: Discard 1 card; draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 2)));

            var twoInBush = Mon("Fethaerbreese Two-in-the-Bush", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Animal, 2500, 1900,
                Fx("A Bird in the Hand", "Once per turn: Pay 1 Mana; return 1 other \"Fethaerbreese\" monster you control to your hand; draw 1 card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, excludeSelf: true, nameFilter: "Fethaerbreese"),
                    Act(EffectActionType.DrawCards, 1)));
            twoInBush.conditionalDoubleAttack = true;
            twoInBush.doubleAttackAttribute = MonsterAttribute.Wind;

            Spell("Fethaerbreese Flight Risk", CardRarity.Uncommon, true,
                Fx("Flight Risk", "Pay 1 Mana: Return 1 of your \"Fethaerbreese\" monsters to your hand; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, nameFilter: "Fethaerbreese"),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Gone by Morning", "Instead, pay 2 Mana: Return 2 of your \"Fethaerbreese\" monsters to your hand; draw 2 cards.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, targetCount: 2, nameFilter: "Fethaerbreese"),
                    Act(EffectActionType.DrawCards, 2)));

            Artifact("Fethaerbreese Nest Egg", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Nest Egg", "Once per turn, when a monster returns from your field to your hand: Draw 1 card.",
                    EffectTrigger.OnOwnMonsterBounced, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Fx("Crack It Open", "You can send this card to the Graveyard: Gain 2 Mana.",
                    EffectTrigger.Ignition, 0, false,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, isCost: true),
                    Act(EffectActionType.GainMana, 2)));

            Spell("Fethaerbreese Homing Instinct", CardRarity.Common, false,
                Fx("Homing Instinct", "Return up to 2 \"Fethaerbreese\" monsters from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, upTo: true, nameFilter: "Fethaerbreese")),
                Inf("Tailwind Home", "Instead, pay 1 Mana: Return 2 \"Fethaerbreese\" monsters from your Graveyard to your hand; gain 2 Mana.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, nameFilter: "Fethaerbreese"),
                    Act(EffectActionType.GainMana, 2)));
        }

        // ---- LIGHTLESS (Dark / Human) · „Im Dunkeln arbeitet es sich besser" ----
        private static void Lightless()
        {
            Mon("Lightless Light-Fingered", CardRarity.Uncommon, 1, MonsterAttribute.Dark, MonsterType.Human, 600, 1100,
                Fx("Light Fingers", "When this card is flipped face-up: Return 1 Spell or Artifact your opponent controls to the hand.",
                    EffectTrigger.OnFlipFaceUp, 0, false,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.EnemySpellOrArtifact)),
                Fx("Palmed", "Once per turn: Pay 1 Mana; turn this card face-down into Defense Position.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.SelfCard)));

            var lightsOut = Mon("Lightless Lights-Out", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2300, 2000,
                Fx("Lights Out", "When this card is flipped face-up: Set 1 monster your opponent controls face-down.",
                    EffectTrigger.OnFlipFaceUp, 0, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)));
            lightsOut.passiveAtkPerCount = 500;
            lightsOut.passiveAtkPerCountKind = EffectCountKind.OpponentFaceDownMonsters;

            Spell("Lightless Snuff", CardRarity.Uncommon, true,
                Fx("Snuff", "Pay 1 Mana: Turn 1 face-up monster you control face-down.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster)),
                Inf("Every Candle", "Instead, pay 2 Mana: Turn up to 2 of your monsters face-down.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster, targetCount: 2, upTo: true)));

            var curtain = Artifact("Lightless Blackout Curtain", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Draw the Curtain", "Once per turn: Pay 2 Mana; Set 1 \"Lightless\" monster from your hand face-down.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered, nameFilter: "Lightless")));
            curtain.auraDefBonus = 500;
            curtain.auraOnlyFaceDown = true;

            Spell("Lightless Closed Casket", CardRarity.Uncommon, false,
                Fx("Closed Casket", "Set up to 2 \"Lightless\" monsters from your hand face-down.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered, targetCount: 2, upTo: true, nameFilter: "Lightless")),
                Inf("Wake the Mourners", "Instead, pay 2 Mana: Set 2 \"Lightless\" monsters from your hand face-down; draw 2 cards.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered, targetCount: 2, nameFilter: "Lightless"),
                    Act(EffectActionType.DrawCards, 2)));
        }

        // ---- DRAGON SHRINE (Light / Dragon) · „Der Schrein weckt, was schläft" ----
        private static void DragonShrine()
        {
            Mon("Petitioner of the Dragon Shrine", CardRarity.Common, 1, MonsterAttribute.Light, MonsterType.Dragon, 700, 1000,
                Fx("Humble Petition", "When this card is Summoned: Add 1 \"Dragon Shrine\" card from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Dragon Shrine")),
                Inf("Answered Prayer", "Instead, pay 2 Mana: Add 1 \"Dragon Shrine\" card AND 1 Dragon monster from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Dragon Shrine"),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, monsterType: MonsterType.Dragon)));

            var doorwyrm = Mon("Doorwyrm of the Dragon Shrine", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Dragon, 1600, 1400);
            doorwyrm.auraAtkBonus = 300;
            doorwyrm.auraNameFilter = "Dragon Shrine";
            doorwyrm.auraExcludesSelf = true;

            Spell("Dragon Shrine Wakeup Call", CardRarity.Uncommon, false,
                Fx("Wakeup Call", "Pay 2 Mana: Special Summon 1 Dragon monster from your Graveyard.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, monsterType: MonsterType.Dragon)),
                Inf("Rise and Shine", "Instead, pay 4 Mana: Special Summon up to 2 Dragon monsters from your Graveyard.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, upTo: true, monsterType: MonsterType.Dragon)));

            var standIn = Artifact("Dragon Shrine Stand-In", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Break Character", "You can send this card to the Graveyard: Add 1 \"Dragon Shrine\" card from your Deck to your hand.",
                    EffectTrigger.Ignition, 0, false,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, isCost: true),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: "Dragon Shrine")));
            standIn.countsAsNameOnField = "Dragon Shrine";

            Spell("Dragon Shrine Scale Advantage", CardRarity.Uncommon, true,
                Fx("Scale Advantage", "Pay 1 Mana: 1 Dragon you control gains 500 ATK and 500 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster, monsterType: MonsterType.Dragon),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AllyMonster, monsterType: MonsterType.Dragon)),
                Inf("Economies of Scale", "Instead, pay 3 Mana: Up to 5 of your Dragon monsters gain 500 ATK and 500 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster, targetCount: 5, upTo: true, monsterType: MonsterType.Dragon),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AllyMonster, targetCount: 5, upTo: true, monsterType: MonsterType.Dragon)));
        }

        // ---- KINDLEKIN (Fire / Beast) · „Viele kleine Flammen" ----
        private static void Kindlekin()
        {
            Mon("Kindlekin Plus-One", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Beast, 400, 400,
                Fx("Plus-One", "When this card is Summoned: You can Special Summon 1 \"Kindlekin\" monster from your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, nameFilter: "Kindlekin")),
                Inf("Party of Three", "Instead, pay 1 Mana: Special Summon up to 2 \"Kindlekin\" monsters from your hand.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, targetCount: 2, upTo: true, nameFilter: "Kindlekin")));

            var marshal = Mon("Kindlekin Fire Marshal", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Beast, 1300, 1500,
                Fx("Roll Call", "Once per turn: Pay 2 Mana; return 1 Level 1 monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1)));
            marshal.auraAtkBonus = 300;
            marshal.auraLevelFilter = 1;

            Spell("Kindlekin Rekindle", CardRarity.Uncommon, true,
                Fx("Rekindle", "Pay 1 Mana: Return 1 Level 1 monster from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1)),
                Inf("From the Embers", "Instead, pay 2 Mana: Special Summon it instead.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1)));

            Artifact("Kindlekin Warm Memories", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Warm Memories", "Once per turn, when a monster you control is destroyed: Gain 300 LP.",
                    EffectTrigger.OnOwnMonsterDestroyed, 0, true,
                    Act(EffectActionType.HealSelf, 300)),
                Fx("Share the Warmth", "Once per turn: Pay 1 Mana; 1 Level 1 monster you control gains 300 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, level: 1)));

            Spell("Kindlekin Sift the Ashes", CardRarity.Common, false,
                Fx("Sift the Ashes", "Send the top 3 cards of your Deck to the Graveyard; add 1 \"Kindlekin\" card among them to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.MillAndSalvage, 3, targetCount: 1, nameFilter: "Kindlekin")),
                Inf("Every Last Spark", "Instead, pay 1 Mana: Add ALL \"Kindlekin\" cards among them to your hand.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.MillAndSalvage, 3, targetCount: 3, nameFilter: "Kindlekin")));
        }

        // ---- MECHINATION (Earth / Mecha) · „Serienfertigung mit Garantie" ----
        private static void Mechination2026()
        {
            Mon("Mechination Jumpstart", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Mecha, 800, 600,
                Fx("Jumpstart", "When this card is Summoned: 1 other MECHA monster you control gains 300 ATK until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, excludeSelf: true, monsterType: MonsterType.Mecha)),
                Inf("Full Charge", "Instead, pay 1 Mana: The bonus is permanent.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.AllyMonster, excludeSelf: true, monsterType: MonsterType.Mecha)));

            Mon("Mechination Night Shift", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Mecha, 2400, 2200,
                Fx("Night Shift", "When this card is Summoned: Return 1 MECHA monster from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, monsterType: MonsterType.Mecha)),
                Inf("Double Shift", "Instead, pay 2 Mana: Return 2 MECHA monsters from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, monsterType: MonsterType.Mecha)),
                Fx("Overtime", "Once per turn: Pay 2 Mana; this card gains 300 ATK permanently.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BuffTargetAtk, 300, TargetKind.SelfCard)));

            Spell("Mechination Recall Notice", CardRarity.Uncommon, true,
                Fx("Recall Notice", "Pay 1 Mana: Return 1 MECHA monster you control to your hand; gain 1 Mana.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, monsterType: MonsterType.Mecha),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Full Refund", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster, monsterType: MonsterType.Mecha),
                    Act(EffectActionType.GainMana, 1),
                    Act(EffectActionType.DrawCards, 1)));

            var crumple = Artifact("Mechination Crumple Zone", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Reinforced Frame", "Once per turn: Pay 2 Mana; 1 MECHA monster you control gains 400 DEF permanently.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BuffTargetDef, 400, TargetKind.AllyMonster, monsterType: MonsterType.Mecha)));
            crumple.auraDefBonus = 300;
            crumple.auraUseTypeFilter = true;
            crumple.auraTypeFilter = MonsterType.Mecha;

            Spell("Mechination Trade-In", CardRarity.Uncommon, false,
                Fx("Trade-In", "Destroy 1 Artifact you control; add 1 Artifact from your Deck to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered)),
                Inf("Loyalty Bonus", "Instead, pay 1 Mana: Also gain 2 Mana.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered),
                    Act(EffectActionType.GainMana, 2)));
        }

        // ================== STAGE 4 ==================

        // ---- MANACLE (Dark / Myth) · „Zinsen schlafen nie" ----
        private static void Manacle2026()
        {
            Mon("Manacle Silver Spoon", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Myth, 700, 1100,
                Fx("Born Lucky", "When this card is Summoned, if you have 5 or more Mana: Draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)).Needs(minMana: 5),
                Inf("Old Money", "Instead, pay 2 Mana, if you have 8 or more Mana: Draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DrawCards, 2)).Needs(minMana: 8));

            Mon("Manacle Loan Shark", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Myth, 2400, 2400,
                Fx("Predatory Terms", "When this card is Summoned: Your opponent has 1 less Mana during their next turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1)),
                Inf("Compound Cruelty", "Instead, pay 2 Mana: Your opponent has 2 less Mana during their next turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 2)),
                Fx("Collection Rounds", "Once per turn: Pay 2 Mana; your opponent has 1 less Mana during their next turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1)));

            Spell("Manacle Hidden Fees", CardRarity.Uncommon, true,
                Fx("Hidden Fees", "Pay 2 Mana: Your opponent has 2 less Mana during their next turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 2)),
                Inf("Terms and Conditions", "Instead, pay 4 Mana: They have 3 less Mana and you have 1 more during your next turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 3),
                    Act(EffectActionType.GainManaNextTurn, 1)));

            Artifact("Manacle Compound Interest", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Compound Interest", "Once per turn: Pay 2 Mana; you have 3 more Mana during your next turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.GainManaNextTurn, 3)));

            Spell("Manacle Buyout", CardRarity.Rare, false,
                Fx("Buyout", "Pay 4 Mana: Return 1 monster your opponent controls to its owner's hand; draw 1 card.",
                    EffectTrigger.OnActivate, 4, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Hostile Takeover", "Instead, pay 6 Mana: Banish it instead; draw 1 card.",
                    EffectTrigger.OnActivate, 6, true,
                    Act(EffectActionType.BanishTargetMonster, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)));
        }

        // ---- SACRILEGION (Light / Dragon) · „Geben ist das neue Nehmen" ----
        private static void Sacrilegion2026()
        {
            Mon("Sacrilegion Willing Lamb", CardRarity.Uncommon, 1, MonsterAttribute.Light, MonsterType.Dragon, 300, 1200,
                Fx("Willing", "When this card is Tributed: Draw 1 card.",
                    EffectTrigger.OnTributedSelf, 0, false,
                    Act(EffectActionType.DrawCards, 1)),
                Fx("Volunteer Again", "Once per turn, while this card is in your Graveyard: Pay 2 Mana; Set it face-down on your field.",
                    EffectTrigger.GraveyardIgnition, 2, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.SelfCard)));

            var twiceBlessed = Mon("Sacrilegion Twice-Blessed", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Dragon, 1500, 1300);
            twiceBlessed.tributeWorth = 2;

            Spell("Sacrilegion Severance", CardRarity.Uncommon, true,
                Fx("Severance", "Pay 1 Mana: Destroy 1 monster you control; gain 2 Mana.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.GainMana, 2)),
                Inf("Golden Parachute", "Instead, pay 2 Mana: Gain 3 Mana and draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster, isCost: true),
                    Act(EffectActionType.GainMana, 3),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Sacrilegion Blood Dividend", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Blood Dividend", "Once per turn, when a monster you control is Tributed: Gain 300 LP.",
                    EffectTrigger.OnOwnMonsterTributed, 0, true,
                    Act(EffectActionType.HealSelf, 300)),
                Inf("Special Dividend", "Instead, pay 1 Mana: Also draw 1 card.",
                    EffectTrigger.OnOwnMonsterTributed, 1, true,
                    Act(EffectActionType.HealSelf, 300),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Sacrilegion Cold Congregation", CardRarity.Uncommon, false,
                Fx("Cold Congregation", "Pay 2 Mana: Special Summon up to 2 \"Sacrilegion\" monsters from your Graveyard face-down.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, upTo: true, nameFilter: "Sacrilegion")),
                Inf("Full Pews", "Instead, pay 4 Mana: Up to 3.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SpecialSummonTargetFromGraveFaceDown, 1, TargetKind.GraveyardMonsterSelf, targetCount: 3, upTo: true, nameFilter: "Sacrilegion")));
        }

        // ---- SLEIGHTWIND (Wind / Demon) · „Schau auf die andere Hand" ----
        private static void Sleightwind2026()
        {
            Mon("Sleightwind Card Counter", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Demon, 900, 700,
                Fx("Counting Cards", "When this card is Summoned, if your opponent has more cards in hand than you: Draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)).Needs(oppMoreHand: true),
                Inf("The House Always Loses", "Instead, pay 2 Mana: Draw until you match their hand size (max 3).",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DrawUntilMatchOpponentHand, 3)).Needs(oppMoreHand: true));

            Mon("Sleightwind, Ace Up the Sleeve", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Demon, 2300, 2100,
                Fx("Ace Up the Sleeve", "Once per turn, during either player's turn: Pay 2 Mana and discard this card from your hand; return 1 monster your opponent controls to its owner's hand.",
                    EffectTrigger.HandQuick, 2, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, isCost: true),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)),
                Fx("Second Ace", "Once per turn: Pay 1 Mana; discard 1 card; draw 1 card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Sleightwind Bait-and-Switch", CardRarity.Uncommon, true,
                Fx("Bait-and-Switch", "Pay 1 Mana: Return 1 of your monsters to your hand; Set 1 monster from your hand face-down.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered)),
                Inf("Double Blind", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.SpecialSummonTargetFaceDown, 1, TargetKind.HandMonsterFiltered),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Sleightwind Marked Deck", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Marked Deck", "Once per turn: Discard 1 card; draw 1 card.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Stacked Deck", "Instead, pay 2 Mana: Discard 1 card; draw 2 cards.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DrawCards, 2)));

            Spell("Sleightwind Nothing to See", CardRarity.Uncommon, false,
                Fx("Nothing to See", "Return 1 of your face-down monsters to your hand; draw 2 cards.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.FaceDownMonsterSelf),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Move Along", "Instead, pay 2 Mana: Draw 3 cards instead.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.FaceDownMonsterSelf),
                    Act(EffectActionType.DrawCards, 3)));
        }

        // ---- GENERICS (25) ----
        private static void Generics2026()
        {
            // --- I · Interaktion ---
            Spell("Cold Feet", CardRarity.Uncommon, true,
                Fx("Cold Feet", "Pay 2 Mana: Return 1 monster on the field to its owner's hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster)),
                Inf("Second-Guessing", "Instead, pay 4 Mana: Return 2 monsters on the field to their owners' hands.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, targetCount: 2)));

            Spell("Put a Pin in It", CardRarity.Uncommon, true,
                Fx("Put a Pin in It", "Pay 2 Mana: Turn 1 face-up monster your opponent controls face-down.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)),
                Inf("Tabled Indefinitely", "Instead, pay 4 Mana: Up to 2.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)));

            Spell("Planned Obsolescence", CardRarity.Uncommon, true,
                Fx("Planned Obsolescence", "Pay 1 Mana: Destroy 1 Spell or Artifact your opponent controls.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemySpellOrArtifact)),
                Inf("End of Support", "Instead, pay 2 Mana: Destroy up to 2.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemySpellOrArtifact, targetCount: 2, upTo: true)));

            Spell("Ancient History", CardRarity.Uncommon, true,
                Fx("Ancient History", "Pay 1 Mana: Banish up to 2 cards from your opponent's Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent, targetCount: 2, upTo: true)),
                Inf("Lost to Time", "Instead, pay 2 Mana: Up to 4.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent, targetCount: 4, upTo: true)));

            Spell("Plot Armor", CardRarity.Rare, true,
                Fx("Plot Armor", "Pay 1 Mana: 1 monster you control cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster)),
                Inf("Main Character", "Instead, pay 3 Mana: It also gains 500 ATK until the end of this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster)));

            Spell("Past Your Bedtime", CardRarity.Rare, false,
                Fx("Past Your Bedtime", "Pay 3 Mana: Return up to 5 monsters with 1200 or less ATK to their owners' hands.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, targetCount: 5, upTo: true, maxAtk: 1200)),
                Inf("Lights Out at Nine", "Instead, pay 5 Mana: 1800 or less.",
                    EffectTrigger.OnActivate, 5, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster, targetCount: 5, upTo: true, maxAtk: 1800)));

            Spell("Cards on the Table", CardRarity.Uncommon, true,
                Fx("Cards on the Table", "Pay 1 Mana: Flip 1 face-down monster your opponent controls face-up.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterEnemy)),
                Inf("Show Your Hand", "Instead, pay 2 Mana: Flip up to 5 of them face-up.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterEnemy, targetCount: 5, upTo: true)));

            // --- II · Karten-Ökonomie ---
            Spell("Second Opinion", CardRarity.Uncommon, false,
                Fx("Second Opinion", "Pay 2 Mana: Discard 1 card; draw 2 cards.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Third Opinion", "Instead, pay 3 Mana: Discard 1 card; draw 3 cards.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 3)));

            Mon("Weather Eye", CardRarity.Common, 1, MonsterAttribute.Water, MonsterType.Human, 600, 900,
                Fx("Weather Eye", "When this card is Summoned: Reveal the top card of your Deck; you may put it on the bottom.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.RevealTopMayBottom, 1)),
                Inf("Storm Warning", "Instead, pay 1 Mana: Also draw 1 card afterwards.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.RevealTopMayBottom, 1),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Small Favors", CardRarity.Common, false,
                Fx("Small Favors", "Pay 1 Mana: Return 1 Level 1 monster from your Graveyard to your hand; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, level: 1),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Small Miracles", "Instead, pay 2 Mana: Special Summon it instead; draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, level: 1),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Spring Cleaning", CardRarity.Uncommon, false,
                Fx("Spring Cleaning", "Discard 2 cards; draw 2 cards and gain 1 Mana.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, targetCount: 2, isCost: true, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Deep Clean", "Instead, pay 1 Mana: Draw 3 cards instead.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, targetCount: 2, isCost: true, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 3),
                    Act(EffectActionType.GainMana, 1)));

            Artifact("Rainy-Day Fund", CardRarity.Common, ArtifactSlot.Field, 0, 0,
                Fx("Rainy-Day Fund", "During your Standby Phase: Gain 200 LP.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.HealSelf, 200)),
                Fx("Break the Glass", "You can send this card to the Graveyard: Gain 800 LP.",
                    EffectTrigger.Ignition, 0, false,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, isCost: true),
                    Act(EffectActionType.HealSelf, 800)));

            Spell("Long Way Home", CardRarity.Uncommon, false,
                Fx("Long Way Home", "Return up to 2 of your banished cards to your Graveyard.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true)),
                Inf("Shortcut", "Instead, pay 2 Mana: Also return 1 card from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf, targetCount: 2, upTo: true),
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf)));

            // --- III · Bodies mit Zweck ---
            Mon("Immovable Object", CardRarity.Common, 2, MonsterAttribute.Earth, MonsterType.Mecha, 0, 2400);

            Mon("Early Bird", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Animal, 1000, 600,
                Fx("Early Bird", "When this card is Summoned: Gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 1)),
                Inf("Gets the Worm", "Instead, pay 1 Mana: Gain 2 Mana.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.GainMana, 2)));

            var slow = Mon("Slow to Anger", CardRarity.Rare, 3, MonsterAttribute.Water, MonsterType.Myth, 2000, 2700,
                Fx("...to Anger", "Once per turn: Pay 2 Mana; this card gains 400 ATK permanently.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.SelfCard)));
            slow.passiveNoAttackOnSummonTurn = true;

            Mon("Bad Penny", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Demon, 1500, 1200,
                Fx("Turns Up Again", "When this card is destroyed: Return it to your hand.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.ReturnSelfFromGraveToHand, 1, TargetKind.SelfCard)));

            Mon("Second Thoughts", CardRarity.Rare, 3, MonsterAttribute.Wind, MonsterType.Animal, 2600, 1500,
                Fx("Second Thoughts", "When this card is Summoned: Return 1 Spell or Artifact you control to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.AllySpellOrArtifact)),
                Inf("On Reflection", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.ReturnTargetCardToHand, 1, TargetKind.AllySpellOrArtifact),
                    Act(EffectActionType.DrawCards, 1)));

            var hound = Mon("Attention Hound", CardRarity.Uncommon, 1, MonsterAttribute.Light, MonsterType.Beast, 800, 1200,
                Fx("Good Boy", "Once per turn: Pay 1 Mana; this card gains 300 DEF until the end of this turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 300, TargetKind.SelfCard)));
            hound.passiveTaunt = true;

            Mon("Awkward Silence", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1400, 1400,
                Fx("Awkward Silence", "When this card is Summoned: You discard 1 card and your opponent discards 1 random card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DiscardOpponentRandom, 1)),
                Inf("Painful Silence", "Instead, pay 2 Mana: Your opponent discards 2 random cards.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true),
                    Act(EffectActionType.DiscardOpponentRandom, 2)));

            // --- IV · Tech & Spielweite ---
            Spell("Second Wind", CardRarity.Uncommon, false,
                Fx("Second Wind", "Pay 2 Mana: You may Normal Summon 1 additional monster this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ExtraNormalSummon, 1)),
                Inf("Third Wind", "Instead, pay 4 Mana: 2 additional.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ExtraNormalSummon, 2)));

            var moral = Artifact("Moral Support", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Pep Talk", "Once per turn: Pay 1 Mana; 1 monster you control gains 200 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 200, TargetKind.AllyMonster)));
            moral.auraAtkBonus = 100;
            moral.auraDefBonus = 100;

            Artifact("Old Tricks", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Old Tricks", "Once per turn: Pay 1 Mana; return 1 Spell from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf)));

            Spell("Cooler Heads Prevail", CardRarity.Uncommon, false,
                Fx("Cooler Heads Prevail", "Pay 2 Mana: Change all face-up monsters on the field to Defense Position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SwitchAllToDefense, 0)),
                Inf("Talked Down", "Instead, pay 3 Mana: Only your opponent's monsters.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SwitchAllToDefense, 1)));

            Spell("Retail Therapy", CardRarity.Uncommon, false,
                Fx("Retail Therapy", "Pay 3 Mana: Add 1 Artifact from your Deck to your hand; gain 300 LP.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered),
                    Act(EffectActionType.HealSelf, 300)),
                Inf("Treat Yourself", "Instead, pay 4 Mana: Gain 600 LP and draw 1 card.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered),
                    Act(EffectActionType.HealSelf, 600),
                    Act(EffectActionType.DrawCards, 1)));
        }

        // ================== LOOSE SET · „PUNS & PURPOSE" ==================

        private static void LooseSetBodies()
        {
            // Kein Effekt — der Name ist das Regelwerk. Gegenstück zu Immovable Object.
            Mon("Glass Cannon", CardRarity.Common, 2, MonsterAttribute.Fire, MonsterType.Mecha, 2200, 0);

            var address = Fx("Address the Elephant", "Once per turn, EITHER player may pay 2 Mana: that player draws 1 card.",
                EffectTrigger.Ignition, 2, true,
                Act(EffectActionType.DrawCards, 1));
            address.eitherPlayerMayActivate = true;
            var elephant = Mon("Elephant in the Room", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Animal, 2500, 2500, address);
            elephant.passiveTaunt = true;

            var bloomer = Mon("Late Bloomer", CardRarity.Uncommon, 3, MonsterAttribute.Earth, MonsterType.Human, 2400, 2000,
                Fx("...Bloomer", "When this card is Summoned: Draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Full Bloom", "Instead, pay 2 Mana: Draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DrawCards, 2)));
            bloomer.passiveNoAttackOnSummonTurn = true;

            Mon("Method Actor", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1200, 1200,
                Fx("Method Acting", "When this card is Summoned: Its ATK and DEF become those of 1 monster on the field until the end of this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster, excludeSelf: true)),
                Inf("Scene Stealer", "Instead, pay 1 Mana: It also gains 300 ATK on top.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.AnyMonster, excludeSelf: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.SelfCard)));

            var twoBirds = Mon("Two Birds, One Stone", CardRarity.Uncommon, 2, MonsterAttribute.Wind, MonsterType.Animal, 1400, 1000);
            twoBirds.conditionalDoubleAttack = true;
            twoBirds.doubleAttackAttribute = MonsterAttribute.Earth;

            Mon("Trophy Hunter", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1700, 1200,
                Fx("Fair Game", "Once per turn, when this card destroys a monster in battle: Draw 1 card.",
                    EffectTrigger.OnBearerBattleKill, 0, true,
                    Act(EffectActionType.DrawCards, 1)));

            Mon("Grief Counselor", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Human, 1100, 1700,
                Fx("Processing Loss", "Once per turn, when a monster you control is destroyed: Draw 1 card.",
                    EffectTrigger.OnOwnMonsterDestroyed, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Fx("Counseling Session", "Once per turn: Pay 1 Mana; gain 400 LP.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.HealSelf, 400)));

            var ham = Mon("Sacrificial Ham", CardRarity.Uncommon, 1, MonsterAttribute.Light, MonsterType.Beast, 500, 900,
                Fx("Dies Beautifully", "When this card is Tributed: Gain 300 LP.",
                    EffectTrigger.OnTributedSelf, 0, false,
                    Act(EffectActionType.HealSelf, 300)));
            ham.tributeWorth = 2;

            Mon("Night Owl", CardRarity.Uncommon, 1, MonsterAttribute.Wind, MonsterType.Animal, 800, 600,
                Fx("Night Shift", "When this card is flipped face-up: Return 1 monster your opponent controls with 1500 or less ATK to the hand.",
                    EffectTrigger.OnFlipFaceUp, 0, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster, maxAtk: 1500)));

            var bench = Mon("Bench Warmer", CardRarity.Uncommon, 1, MonsterAttribute.Earth, MonsterType.Beast, 400, 1400);
            bench.passiveCannotAttack = true;
            bench.auraDefBonus = 200;
            bench.auraExcludesSelf = true;

            var manager = Mon("Middle Management", CardRarity.Uncommon, 2, MonsterAttribute.Light, MonsterType.Human, 1300, 1300,
                Fx("Delegate", "Once per turn: Pay 1 Mana; Special Summon 1 Level 1 monster from your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, level: 1)));
            manager.auraAtkBonus = 200;
            manager.auraDefBonus = 200;
            manager.auraLevelFilter = 1;
        }

        private static void LooseSetTricks()
        {
            Spell("Lost in the Shuffle", CardRarity.Uncommon, true,
                Fx("Lost in the Shuffle", "Pay 2 Mana: Shuffle 1 monster with 1500 or less ATK into its owner's Deck.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.AnyMonster, maxAtk: 1500)),
                Inf("Never Seen Again", "Instead, pay 4 Mana: Any ATK.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.AnyMonster)));

            Spell("Cease and Desist", CardRarity.Rare, true,
                Fx("Cease", "Pay 2 Mana: Negate the effects of 1 card your opponent controls until the end of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)),
                Inf("and Desist", "Instead, pay 4 Mana: Also, 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));

            Spell("Head Over Heels", CardRarity.Uncommon, true,
                Fx("Head Over Heels", "Pay 1 Mana: 1 monster on the field swaps its ATK and DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SwapAtkDefThisTurn, 1, TargetKind.AnyMonster)),
                Inf("Topsy-Turvy", "Instead, pay 2 Mana: Up to 2 monsters.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SwapAtkDefThisTurn, 1, TargetKind.AnyMonster, targetCount: 2, upTo: true)));

            Spell("Borrowed Time", CardRarity.Rare, false,
                Fx("Borrowed Time", "Pay 4 Mana: Take control of 1 monster your opponent controls until the End Phase.",
                    EffectTrigger.OnActivate, 4, false,
                    Act(EffectActionType.TakeControlUntilEndOfTurn, 1, TargetKind.EnemyMonster)),
                Inf("Interest Accrues", "Instead, pay 5 Mana: Also draw 1 card.",
                    EffectTrigger.OnActivate, 5, true,
                    Act(EffectActionType.TakeControlUntilEndOfTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Open Book Test", CardRarity.Uncommon, false,
                Fx("Open Book", "Pay 2 Mana: Look at your opponent's hand and choose 1 card; they discard it.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.LookAndDiscardChosen, 1, TargetKind.HandCardOpponent)),
                Inf("Pop Quiz", "Instead, pay 4 Mana: Choose 2 cards.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.LookAndDiscardChosen, 1, TargetKind.HandCardOpponent, targetCount: 2)));

            Spell("Five-Finger Discount", CardRarity.Rare, false,
                Fx("Sticky Fingers", "Pay 4 Mana: Your opponent discards 1 random card.",
                    EffectTrigger.OnActivate, 4, false,
                    Act(EffectActionType.DiscardOpponentRandom, 1)),
                Inf("Five-Finger Discount", "Instead, pay 6 Mana: Reveal 1 random card from their hand — if it is a monster, Special Summon it to YOUR field; otherwise they discard it.",
                    EffectTrigger.OnActivate, 6, true,
                    Act(EffectActionType.OpponentRandomToFieldOrDiscard, 1)));

            Spell("Velvet Rope", CardRarity.Uncommon, true,
                Fx("Not on the List", "Pay 2 Mana: Your opponent cannot Special Summon for the rest of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.OpponentSummonLockThisTurn, 1)),
                Inf("Dress Code", "Instead, pay 3 Mana: Also, 1 monster they control cannot attack this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.OpponentSummonLockThisTurn, 1),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));

            Spell("Pillow Fort", CardRarity.Common, true,
                Fx("Pillow Fort", "Pay 1 Mana: You take no battle damage for the rest of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.PreventBattleDamageThisTurn, 1)),
                Inf("Reinforced Cushions", "Instead, pay 2 Mana: Also, 1 monster you control gains 500 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.PreventBattleDamageThisTurn, 1),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AllyMonster)));
        }

        private static void LooseSetEconomy()
        {
            Spell("Sleep On It", CardRarity.Common, false,
                Fx("Sleep On It", "You have 2 more Mana during your next turn.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.GainManaNextTurn, 2)),
                Inf("Slept Like a Rock", "Instead, pay 1 Mana: 3 more.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.GainManaNextTurn, 3)));

            Spell("Needle in a Haystack", CardRarity.Common, false,
                Fx("Needle in a Haystack", "Send the top 3 cards of your Deck to the Graveyard; add 1 Level 1 monster among them to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.MillAndSalvage, 3, targetCount: 1, level: 1)),
                Inf("Magnet", "Instead, pay 1 Mana: Top 5 cards; add up to 2 Level 1 monsters among them.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.MillAndSalvage, 5, targetCount: 2, level: 1)));

            Spell("Back in Style", CardRarity.Uncommon, false,
                Fx("Back in Style", "Pay 1 Mana: Shuffle up to 3 cards from your Graveyard into your Deck; draw 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ShuffleGraveyardIntoDeck, 1, TargetKind.GraveyardCardSelf, targetCount: 3, upTo: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Full Comeback", "Instead, pay 2 Mana: Up to 5 cards; draw 1.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ShuffleGraveyardIntoDeck, 1, TargetKind.GraveyardCardSelf, targetCount: 5, upTo: true),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Silver Lining", CardRarity.Uncommon, false,
                Fx("Silver Lining", "If your opponent controls more monsters than you: Draw 2 cards.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DrawCards, 2)).Needs(oppMoreMonsters: true),
                Inf("Break in the Clouds", "Instead, pay 2 Mana: Also gain 2 Mana.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainMana, 2)).Needs(oppMoreMonsters: true));

            Spell("Spoiler Alert", CardRarity.Uncommon, false,
                Fx("Spoiler Alert", "Pay 2 Mana: Set 1 Spell from your Deck face-down (usable this turn).",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered)),
                Inf("Read the Ending", "Instead, pay 3 Mana: Also reveal the top card of your Deck; you may put it on the bottom.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered),
                    Act(EffectActionType.RevealTopMayBottom, 1)));
        }

        private static void LooseSetArtifacts()
        {
            var fallGuy = Artifact("Fall Guy", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Stuntman's Fee", "When this card is destroyed: Draw 1 card.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.DrawCards, 1)));
            fallGuy.redirectDestructionToSelf = true;

            Artifact("Snooze Button", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Five More Minutes", "Once per turn: Pay 2 Mana; 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Do Not Disturb", "Instead, pay 3 Mana: It also cannot change its battle position this turn.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)));

            Artifact("Cliffhanger", CardRarity.Common, ArtifactSlot.Field, 0, 0,
                Fx("To Be Continued", "During your Standby Phase: Reveal the top card of your Deck; you may put it on the bottom.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.RevealTopMayBottom, 1)),
                Fx("Skip to the Good Part", "Once per turn: Pay 1 Mana; send the top card of your Deck to the Graveyard; draw 1 card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.MillSelf, 1),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Participation Trophy", CardRarity.Common, ArtifactSlot.Monster, 300, 300,
                Fx("Celebrate Anyway", "Once per turn, when the equipped monster destroys a monster in battle: Gain 300 LP.",
                    EffectTrigger.OnBearerBattleKill, 0, true,
                    Act(EffectActionType.HealSelf, 300)));

            Artifact("Security Blanket", CardRarity.Uncommon, ArtifactSlot.Monster, 0, 600,
                Fx("Tucked In", "When played: 1 monster you control cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster)));

            Artifact("Fire Escape", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Fire Escape", "Once per turn: Pay 1 Mana; return 1 monster you control to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster)),
                Inf("Orderly Evacuation", "Instead, pay 2 Mana: Also draw 1 card.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.DrawCards, 1)));
        }

        // ================== PAPERBOUND (Dark / Human) · Stun ==================

        private static void Paperbound()
        {
            Mon("Paperbound File Clerk", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 600, 1200,
                Fx("Take a Number", "When this card is Summoned: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Wait Here, Please", "Instead, pay 2 Mana: It also cannot change its battle position this turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)));

            Mon("Paperbound Rubber Stamp", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 900, 700,
                Fx("Stamped and Filed", "When this card is Summoned: Change 1 monster your opponent controls to Defense Position.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster)),
                Inf("Filed Forever", "Instead, pay 1 Mana: It also cannot change its position this turn.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)));

            Mon("Paperbound Auditor", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1500, 1600,
                Fx("Surprise Audit", "Once per turn: Pay 2 Mana; negate the effects of 1 card your opponent controls until the end of this turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)),
                Inf("Discrepancy Found", "Instead, pay 4 Mana: It also loses 500 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 4, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -500, TargetKind.EnemyMonster)));

            Mon("Paperbound Commissioner", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2300, 2400,
                Fx("Closed for Lunch", "When this card is Summoned: Change ALL your opponent's monsters to Defense Position.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SwitchAllToDefense, 1)),
                Inf("Closed Indefinitely", "Instead, pay 3 Mana: Also turn 1 of them face-down.",
                    EffectTrigger.OnSummonSelf, 3, true,
                    Act(EffectActionType.SwitchAllToDefense, 1),
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)),
                Fx("Not My Department", "Once per turn, during either player's turn: Pay 2 Mana; 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));

            Spell("Paperbound Red Tape", CardRarity.Uncommon, true,
                Fx("Red Tape", "Pay 1 Mana: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Miles of It", "Instead, pay 2 Mana: Up to 2 monsters.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)));

            Spell("Paperbound In Triplicate", CardRarity.Uncommon, false,
                Fx("In Triplicate", "Pay 2 Mana: Change up to 3 of your opponent's monsters to Defense Position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster, targetCount: 3, upTo: true)),
                Inf("Notarized", "Instead, pay 4 Mana: They also cannot change their position this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster, targetCount: 3, upTo: true),
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster, targetCount: 3, upTo: true)));

            Spell("Paperbound Lost Form 27-B", CardRarity.Rare, false,
                Fx("Lost in Filing", "Pay 2 Mana: Turn 1 face-up monster your opponent controls face-down.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)),
                Inf("Never Existed", "Instead, pay 4 Mana: Up to 2.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)));

            Spell("Paperbound Office Hours", CardRarity.Uncommon, true,
                Fx("Office Hours", "Pay 1 Mana: Your opponent cannot Special Summon for the rest of this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.OpponentSummonLockThisTurn, 1)),
                Inf("By Appointment Only", "Instead, pay 2 Mana: Also, 1 monster they control cannot attack this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.OpponentSummonLockThisTurn, 1),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));

            Artifact("Paperbound Waiting Room", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Please Hold", "Once per turn: Pay 2 Mana; 1 monster your opponent controls cannot attack and cannot change its position this turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Estimated Wait: Forever", "Instead, pay 3 Mana: Also, your opponent cannot Special Summon this turn.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.OpponentSummonLockThisTurn, 1)));

            var rejection = Rel("Paperbound, the Final Rejection", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2600, 2600,
                "You control 2+ monsters and have 4+ cards in your Graveyard. Cost 3 Mana.", 3,
                Fx("Application Denied", "When this card is Summoned: Change all your opponent's monsters to Defense Position.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SwitchAllToDefense, 1)),
                Inf("Denied With Prejudice", "Instead, pay 2 Mana: Also, up to 2 of them cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SwitchAllToDefense, 1),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)),
                Fx("Rejected", "Once per turn, during either player's turn: Pay 2 Mana; negate the effects of 1 card on the field until the end of this turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)));
            rejection.reqOwnMonstersAtLeast = 2;
            rejection.reqGraveyardAtLeast = 4;

            // Sanity: Karten geben dem Beschwörer keinen Kartenvorteil — Paperbound
            // gewinnt über verlorene GEGNER-Züge, nicht über eigene Ressourcen.
        }

        // ================== POWDERKEG (Fire / Mecha) · Artefakt-Munition ==================

        private static void Powderkeg()
        {
            Mon("Powderkeg Loader", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Mecha, 800, 900,
                Fx("Load the Rack", "When this card is Summoned: Place 1 \"Powderkeg\" Artifact from your Deck into your Artifact Zone.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg")),
                Inf("Double Load", "Instead, pay 2 Mana: Also add 1 more to your hand.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg"),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg")));

            Mon("Powderkeg Sparkplug", CardRarity.Common, 1, MonsterAttribute.Fire, MonsterType.Mecha, 1000, 500,
                Fx("Short Fuse", "Once per turn: Pay 1 Mana; destroy 1 Artifact you control; draw 1 card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Chain Reaction", "Instead, pay 2 Mana: Draw 2 cards.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2)));

            Mon("Powderkeg Cannoneer", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Mecha, 1700, 1300,
                Fx("Return Fire", "During either player's turn: Destroy 1 Artifact you control; destroy 1 monster your opponent controls with 1000 or less ATK. (No once-per-turn limit — ammunition is the limit.)",
                    EffectTrigger.Quick, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1000)),
                Inf("Heavy Shot", "Instead, pay 3 Mana: 2000 or less.",
                    EffectTrigger.Quick, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000)));

            var quartermaster = Mon("Powderkeg Quartermaster", CardRarity.Rare, 3, MonsterAttribute.Fire, MonsterType.Mecha, 2400, 2000,
                Fx("Requisition", "When this card is Summoned: Place up to 2 \"Powderkeg\" Artifacts from your Deck into your Artifact Zone.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, targetCount: 2, upTo: true, nameFilter: "Powderkeg")),
                Inf("Full Manifest", "Instead, pay 3 Mana: Also draw 1 card.",
                    EffectTrigger.OnSummonSelf, 3, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, targetCount: 2, upTo: true, nameFilter: "Powderkeg"),
                    Act(EffectActionType.DrawCards, 1)));
            quartermaster.passiveAtkPerCount = 200;
            quartermaster.passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;

            Artifact("Powderkeg Magazine", CardRarity.Common, ArtifactSlot.Field, 0, 0,
                Fx("Stockpile", "Once per turn: Pay 1 Mana; place 1 other \"Powderkeg\" Artifact from your Deck into your Artifact Zone.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg", excludeSameName: true)),
                Fx("Cook-Off", "When this card is destroyed: Draw 1 card.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Powderkeg Shellcrate", CardRarity.Common, ArtifactSlot.Field, 0, 0,
                Fx("Propellant", "When this card is destroyed: Gain 1 Mana.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.GainMana, 1)));

            Artifact("Powderkeg Blastplate", CardRarity.Uncommon, ArtifactSlot.Monster, 400, 400,
                Fx("Shrapnel", "When this card is destroyed: 1 monster you control gains 400 ATK until the end of this turn.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.AllyMonster)));

            Spell("Powderkeg Point-Blank", CardRarity.Rare, true,
                Fx("Point-Blank", "Pay 1 Mana: Destroy 1 Artifact you control; destroy 1 card your opponent controls. (No once-per-turn limit.)",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyCardOnField)),
                Inf("Full Broadside", "Instead, pay 3 Mana: Destroy 2 of your Artifacts; destroy up to 2 of your opponent's cards.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, targetCount: 2, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyCardOnField, targetCount: 2, upTo: true)));

            Spell("Powderkeg Misfire", CardRarity.Uncommon, false,
                Fx("Misfire", "Destroy 1 Artifact you control; draw 2 cards.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Salvage the Barrel", "Instead, pay 1 Mana: Also gain 1 Mana.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.GainMana, 1)));

            Spell("Powderkeg Brass Sweep", CardRarity.Uncommon, false,
                Fx("Brass Sweep", "Pay 1 Mana: Return up to 2 \"Powderkeg\" Artifacts from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf, targetCount: 2, upTo: true, nameFilter: "Powderkeg")),
                Inf("Clean Sweep", "Instead, pay 2 Mana: Up to 3 Artifacts of ANY name.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf, targetCount: 3, upTo: true)));

            var firstSpark = Rel("Powderkeg, First Spark", CardRarity.Uncommon, 2, MonsterAttribute.Fire, MonsterType.Mecha, 2100, 1600,
                "You control 1+ Artifact and have 2+ cards in your Graveyard. Cost 2 Mana.", 2,
                Fx("Opening Shot", "When this card is Summoned: Place 1 \"Powderkeg\" Artifact from your Deck into your Artifact Zone.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetArtifactFromDeck, 1, TargetKind.DeckArtifactFiltered, nameFilter: "Powderkeg")),
                Fx("Warning Shot", "Once per turn, during either player's turn: Destroy 1 Artifact you control; negate the effects of 1 card on the field until the end of this turn.",
                    EffectTrigger.Quick, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)));
            firstSpark.reqOwnArtifactsOnField = 1;
            firstSpark.reqGraveyardAtLeast = 2;

            var lastSalvo = Rel("Powderkeg, the Last Salvo", CardRarity.Legendary, 3, MonsterAttribute.Fire, MonsterType.Mecha, 2800, 2200,
                "You control 2 Artifacts and have 3+ Artifacts in your Graveyard. Cost 3 Mana.", 3,
                Fx("Fire at Will", "During either player's turn: Destroy 1 Artifact you control; destroy 1 card your opponent controls. (No once-per-turn limit — ammunition is the limit.)",
                    EffectTrigger.Quick, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyArtifact, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyCardOnField)),
                Fx("Reload", "Once per turn: Pay 2 Mana; place 1 \"Powderkeg\" Artifact from your Graveyard back into your Artifact Zone.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.PlaceTargetArtifactFromGraveyard, 1, TargetKind.GraveyardArtifactSelf, nameFilter: "Powderkeg")));
            lastSalvo.reqOwnArtifactsOnField = 2;
            lastSalvo.reqOwnArtifactsInGrave = 3;
        }

        // ================== TRAPLINE (Earth / Human) · Fallen-Ketten ==================

        private static void Trapline()
        {
            Mon("Trapline Warden", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 1400, 1600,
                Fx("Lay the Line", "When this card is Summoned: Set 1 \"Trapline\" Spell from your Deck face-down (usable this turn).",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered, nameFilter: "Trapline")),
                Inf("Cover the Valley", "Instead, pay 2 Mana: Set 2.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered, targetCount: 2, nameFilter: "Trapline")));

            Mon("Trapline Weaver", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Human, 700, 1100,
                Fx("Spin the Line", "Once per turn: Pay 1 Mana; Set 1 \"Trapline\" Quick Spell from your hand face-down; draw 1 card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SetTargetSpellFromHand, 1, TargetKind.HandSpellFiltered, nameFilter: "Trapline"),
                    Act(EffectActionType.DrawCards, 1)));

            Spell("Trapline Tripwire", CardRarity.Common, true,
                Fx("Tripwire", "When an attack is declared: Pay 1 Mana; the attacking monster loses 800 ATK until the end of this turn; then you may Set 1 \"Trapline\" with a different name from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -800, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse),
                Inf("Tangled", "Instead, pay 2 Mana: It also cannot attack again this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -800, TargetKind.EnemyMonster),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse));

            Spell("Trapline Row of Teeth", CardRarity.Rare, true,
                Fx("Row of Teeth", "When an attack is declared: Pay 4 Mana; destroy ALL Attack Position monsters your opponent controls; then you may Set 1 \"Trapline\" with a different name from your hand.",
                    EffectTrigger.OnActivate, 4, false,
                    Act(EffectActionType.DestroyAllEnemyAttackMonsters, 1),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse),
                Inf("The Whole Row", "Instead, pay 5 Mana: Set up to 2 \"Trapline\" cards from your hand afterwards.",
                    EffectTrigger.OnActivate, 5, true,
                    Act(EffectActionType.DestroyAllEnemyAttackMonsters, 1),
                    Act(EffectActionType.SetTargetSpellFromHand, 1, TargetKind.HandSpellFiltered, targetCount: 2, upTo: true, nameFilter: "Trapline", excludeSameName: true)).InWindow(QuickWindow.AttackResponse));

            Spell("Trapline Warm Welcome", CardRarity.Rare, true,
                Fx("Warm Welcome", "When your opponent Summons a monster: Pay 3 Mana; destroy 1 monster they control, then destroy all Defense Position monsters on the field with the same Level; then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 3, false,
                    Act(EffectActionType.DestroyTargetAndSameLevelDefense, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.SummonResponse),
                Inf("Overstayed Welcome", "Instead, pay 4 Mana: Also, your opponent cannot Special Summon for the rest of this turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.DestroyTargetAndSameLevelDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.OpponentSummonLockThisTurn, 1),
                    SetNextTrap()).InWindow(QuickWindow.SummonResponse));

            Spell("Trapline Bear Hug", CardRarity.Uncommon, true,
                Fx("Bear Hug", "When an attack is declared: Pay 2 Mana; turn the attacking monster face-down (the attack is cancelled); then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse),
                Inf("Crushing Embrace", "Instead, pay 3 Mana: It also cannot change its position this turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse));

            Spell("Trapline Pitfall", CardRarity.Uncommon, true,
                Fx("Pitfall", "When your opponent Summons a monster: Pay 2 Mana; return 1 monster they control to the hand; then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.SummonResponse),
                Inf("No Bottom", "Instead, pay 4 Mana: Shuffle it into the Deck instead.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.EnemyMonster),
                    SetNextTrap()).InWindow(QuickWindow.SummonResponse));

            Spell("Trapline Decoy", CardRarity.Common, true,
                Fx("Decoy", "When an attack is declared: Pay 1 Mana; 1 monster you control cannot be destroyed this turn; then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse),
                Inf("Convincing Decoy", "Instead, pay 2 Mana: You also take no battle damage this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.PreventBattleDamageThisTurn, 1),
                    SetNextTrap()).InWindow(QuickWindow.AttackResponse));

            Spell("Trapline Double Back", CardRarity.Uncommon, true,
                Fx("Double Back", "Pay 1 Mana: Return 1 \"Trapline\" Quick Spell from your Graveyard to your hand; then you may Set 1 \"Trapline\" from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Trapline"),
                    SetNextTrap()),
                Inf("Retrace the Line", "Instead, pay 2 Mana: Return 2.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, targetCount: 2, nameFilter: "Trapline"),
                    SetNextTrap()));

            Spell("Trapline Smoke Signal", CardRarity.Common, true,
                Fx("Smoke Signal", "Draw 1 card; then you may Set 1 \"Trapline\" Quick Spell with a different name from your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DrawCards, 1),
                    SetNextTrap()),
                Inf("Signal Fire", "Instead, pay 1 Mana: Also gain 1 Mana.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.GainMana, 1),
                    SetNextTrap()));

            Artifact("Trapline Basecamp", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Restock the Line", "During your Standby Phase: Set 1 \"Trapline\" Spell from your Deck face-down.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.SetTargetSpellFromDeck, 1, TargetKind.DeckSpellFiltered, nameFilter: "Trapline")),
                Inf("Salvage Run", "Once per turn: Pay 2 Mana; return 1 \"Trapline\" Spell from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, nameFilter: "Trapline")));

            var snares = Rel("Trapline, Season of Snares", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 2000, 1800,
                "3+ cards in your Graveyard. Cost 2 Mana.", 2,
                Fx("Harvest the Line", "When this card is Summoned: Return up to 2 \"Trapline\" Spells from your Graveyard to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardSpellSelf, targetCount: 2, upTo: true, nameFilter: "Trapline")),
                Fx("Quick Snare", "Once per turn, during either player's turn: Pay 2 Mana; 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));
            snares.reqGraveyardAtLeast = 3;

            var patientJaw = Rel("Trapline, the Patient Jaw", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Human, 2500, 2300,
                "4+ cards in your Graveyard. Cost 3 Mana.", 3,
                Fx("Sprung Steel", "When this card is Summoned: Destroy 1 monster your opponent controls with 2000 or less ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 2000)),
                Fx("Reset the Trap", "Once per turn, during either player's turn: Pay 1 Mana; Set 1 \"Trapline\" Quick Spell from your hand face-down.",
                    EffectTrigger.Quick, 1, true,
                    Act(EffectActionType.SetTargetSpellFromHand, 1, TargetKind.HandSpellFiltered, nameFilter: "Trapline")));
            patientJaw.reqGraveyardAtLeast = 4;
        }

        // ================== REDACTOR (Dark / Human) · Anti-Draw ==================

        private static void Redactor()
        {
            Mon("Redactor Inkling", CardRarity.Common, 1, MonsterAttribute.Dark, MonsterType.Human, 700, 800,
                Fx("Every Word Costs", "Once per turn, when your opponent draws outside their Draw Phase: This card gains 200 ATK permanently.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.BuffTargetAtk, 200, TargetKind.SelfCard)),
                Inf("Every Letter, Too", "Instead, pay 1 Mana: 200 ATK and 200 DEF.",
                    EffectTrigger.OnOpponentDraw, 1, true,
                    Act(EffectActionType.BuffTargetAtk, 200, TargetKind.SelfCard),
                    Act(EffectActionType.BuffTargetDef, 200, TargetKind.SelfCard)));

            Mon("Redactor Censor", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1500, 1400,
                Fx("Strike That", "Once per turn, when your opponent draws outside their Draw Phase: Send the top card of their Deck to the Graveyard.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.MillOpponent, 1)),
                Inf("Strike It All", "Instead, pay 2 Mana: The top 2.",
                    EffectTrigger.OnOpponentDraw, 2, true,
                    Act(EffectActionType.MillOpponent, 2)));

            Mon("Redactor Archivist", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1200, 1800,
                Fx("Reading Over Your Shoulder", "Once per turn, when your opponent draws outside their Draw Phase: Pay 1 Mana; draw 1 card.",
                    EffectTrigger.OnOpponentDraw, 1, true,
                    Act(EffectActionType.DrawCards, 1)));

            Mon("Redactor Blackbar", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2400, 2200,
                Fx("Heavily Redacted", "Once per turn, when your opponent draws outside their Draw Phase: 1 monster they control loses 300 ATK permanently.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.DebuffTargetAtk, 300, TargetKind.EnemyMonster)),
                Inf("Nothing Left to Read", "Instead, pay 2 Mana: 500.",
                    EffectTrigger.OnOpponentDraw, 2, true,
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster)),
                Fx("Expunge", "Once per turn: Pay 2 Mana; banish 1 card from your opponent's Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent)));

            Mon("Redactor Minister of Records", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2200, 2600,
                Fx("State Secret", "Once per turn, when your opponent draws outside their Draw Phase: They discard 1 random card.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.DiscardOpponentRandom, 1)),
                Inf("Sealed by the State", "Instead, pay 2 Mana: They also have 1 less Mana during their next turn.",
                    EffectTrigger.OnOpponentDraw, 2, true,
                    Act(EffectActionType.DiscardOpponentRandom, 1),
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1)));

            Spell("Redactor Classified", CardRarity.Uncommon, false,
                Fx("Classified", "Pay 2 Mana: 1 face-up monster your opponent controls cannot change its position this turn; then turn it face-down.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)),
                Inf("Above Your Clearance", "Instead, pay 4 Mana: Up to 2.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true),
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster, targetCount: 2, upTo: true)));

            Spell("Redactor Burn Before Reading", CardRarity.Uncommon, true,
                Fx("Burn Before Reading", "Pay 2 Mana: Send the top 3 cards of your opponent's Deck to the Graveyard.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.MillOpponent, 3)),
                Inf("Ashes to Archives", "Instead, pay 3 Mana: Also banish 1 card from their Graveyard.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.MillOpponent, 3),
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent)));

            Spell("Redactor Freedom of Information", CardRarity.Uncommon, false,
                Fx("Freedom of Information", "Pay 1 Mana: Discard 1 card; draw 2 cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Full Disclosure", "Instead, pay 2 Mana: Draw 3 cards.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf, isCost: true, excludeSelf: true),
                    Act(EffectActionType.DrawCards, 3)));

            Spell("Redactor Mandatory Reading", CardRarity.Uncommon, false,
                Fx("Mandatory Reading", "Pay 1 Mana: Your opponent draws 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.OpponentDraws, 1)),
                Inf("Assigned Homework", "Instead, pay 2 Mana: You draw 1 card as well.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.OpponentDraws, 1),
                    Act(EffectActionType.DrawCards, 1)));

            Artifact("Redactor Ministry Seal", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Official Secrets", "Once per turn, when your opponent draws outside their Draw Phase: They have 1 less Mana during their next turn.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1)),
                Inf("Top Secret", "Instead, pay 2 Mana: Also send the top card of their Deck to the Graveyard.",
                    EffectTrigger.OnOpponentDraw, 2, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1),
                    Act(EffectActionType.MillOpponent, 1)));

            var finalEdition = Rel("Redactor, Final Edition", CardRarity.Rare, 3, MonsterAttribute.Dark, MonsterType.Human, 2700, 2300,
                "5+ cards in your Graveyard. Tribute 1 monster you control. Cost 3 Mana.", 3,
                Fx("Print Deadline", "Once per turn, when your opponent draws outside their Draw Phase: They discard 1 random card.",
                    EffectTrigger.OnOpponentDraw, 0, true,
                    Act(EffectActionType.DiscardOpponentRandom, 1)),
                Inf("Stop the Presses", "Instead, pay 2 Mana: Also send the top 2 cards of their Deck to the Graveyard.",
                    EffectTrigger.OnOpponentDraw, 2, true,
                    Act(EffectActionType.DiscardOpponentRandom, 1),
                    Act(EffectActionType.MillOpponent, 2)),
                Fx("Pulp the Archives", "Once per turn: Pay 2 Mana; banish up to 3 cards from your opponent's Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardOpponent, targetCount: 3, upTo: true)));
            finalEdition.reqGraveyardAtLeast = 5;
            finalEdition.costTributeOtherMonster = true;
        }

        // ================== SNUGGLET (bunt / Animal+Beast) · Kuschel-Trio ==================

        private static void Snugglet()
        {
            // Der Aura-Ring: Bumble→Mopsy→Pebble→Whiskers→Puddle→Acorn→Bumble.
            // Jedes Tierchen trägt das 3er-Feldlimit — nur drei passen aufs Sofa.
            System.Action<MonsterCardData> limit = pet =>
            {
                pet.fieldLimitName = "Snugglet";
                pet.fieldLimitCount = 3;
            };

            var bumble = Mon("Snugglet Bumble", CardRarity.Common, 1, MonsterAttribute.Wind, MonsterType.Animal, 600, 600,
                Fx("Buzz Around", "When this card is Summoned: Add 1 \"Snugglet\" monster from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, nameFilter: "Snugglet")),
                Inf("Busy Bee", "Instead, pay 2 Mana: Also add 1 \"Snugglet\" Spell.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, nameFilter: "Snugglet"),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckSpellFiltered, nameFilter: "Snugglet")));
            bumble.auraAtkBonus = 400; bumble.auraNameFilter = "Snugglet Mopsy"; limit(bumble);

            var mopsy = Mon("Snugglet Mopsy", CardRarity.Common, 1, MonsterAttribute.Earth, MonsterType.Animal, 800, 500,
                Fx("Lucky Foot", "When this card is Summoned: Gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 1)),
                Inf("Lucky Streak", "Instead, pay 1 Mana: Also 1 more during your next turn.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.GainMana, 1),
                    Act(EffectActionType.GainManaNextTurn, 1)));
            mopsy.auraDefBonus = 400; mopsy.auraNameFilter = "Snugglet Pebble"; limit(mopsy);

            var pebble = Mon("Snugglet Pebble", CardRarity.Common, 1, MonsterAttribute.Water, MonsterType.Beast, 400, 1200);
            pebble.auraAtkBonus = 400; pebble.auraNameFilter = "Snugglet Whiskers";
            pebble.passiveTaunt = true; limit(pebble);

            var whiskers = Mon("Snugglet Whiskers", CardRarity.Uncommon, 1, MonsterAttribute.Dark, MonsterType.Beast, 900, 400,
                Fx("Pounce", "Once per turn: Pay 1 Mana; 1 monster your opponent controls loses 400 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -400, TargetKind.EnemyMonster)));
            whiskers.auraAtkBonus = 300; whiskers.auraDefBonus = 300; whiskers.auraNameFilter = "Snugglet Puddle"; limit(whiskers);

            var puddle = Mon("Snugglet Puddle", CardRarity.Uncommon, 1, MonsterAttribute.Water, MonsterType.Animal, 500, 900,
                Fx("Happy Splash", "When this card is Summoned, if you control 3 monsters: Draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)).Needs(minOwnMonsters: 3),
                Inf("Cannonball!", "Instead, pay 1 Mana: Draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 1, true,
                    Act(EffectActionType.DrawCards, 2)).Needs(minOwnMonsters: 3));
            puddle.auraAtkBonus = 400; puddle.auraNameFilter = "Snugglet Acorn"; limit(puddle);

            var acorn = Mon("Snugglet Acorn", CardRarity.Uncommon, 1, MonsterAttribute.Light, MonsterType.Animal, 700, 700,
                Fx("Stash", "Once per turn: Pay 1 Mana; return 1 \"Snugglet\" monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Snugglet")));
            acorn.auraAtkBonus = 400; acorn.auraNameFilter = "Snugglet Bumble"; limit(acorn);

            Spell("Snugglet Pile-Up", CardRarity.Uncommon, false,
                Fx("Pile-Up", "Pay 1 Mana: Special Summon up to 2 \"Snugglet\" monsters from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, targetCount: 2, upTo: true, nameFilter: "Snugglet")),
                Inf("Everybody In", "Instead, pay 3 Mana: From your hand or Graveyard.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromHandOrGrave, 1, TargetKind.HandOrGraveMonsterFiltered, targetCount: 2, upTo: true, nameFilter: "Snugglet")));

            Spell("Snugglet Nap Time", CardRarity.Common, true,
                Fx("Nap Time", "Pay 1 Mana: Up to 3 of your \"Snugglet\" monsters cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, targetCount: 3, upTo: true, nameFilter: "Snugglet")),
                Inf("Deep Sleep", "Instead, pay 2 Mana: They also gain 300 DEF until the end of this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, targetCount: 3, upTo: true, nameFilter: "Snugglet"),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 300, TargetKind.AllyMonster, targetCount: 3, upTo: true, nameFilter: "Snugglet")));

            var sofa = Artifact("Snugglet Sofa", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Scooch Over", "Once per turn: Pay 2 Mana; Special Summon 1 \"Snugglet\" monster from your Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Snugglet")));
            sofa.auraAtkBonus = 200; sofa.auraDefBonus = 200; sofa.auraNameFilter = "Snugglet";

            var cuddlepile = Rel("Snugglet Cuddlepile, Three Deep", CardRarity.Rare, 2, MonsterAttribute.Light, MonsterType.Beast, 2200, 2200,
                "You control 3 \"Snugglet\" monsters. Tribute 1 monster you control. Cost 2 Mana.", 2,
                Fx("Room for One More", "When this card is Summoned: Special Summon 1 \"Snugglet\" monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, nameFilter: "Snugglet")));
            cuddlepile.reqNamedOnField = "Snugglet"; cuddlepile.reqNamedCount = 3;
            cuddlepile.costTributeOtherMonster = true;
            cuddlepile.auraAtkBonus = 300; cuddlepile.auraDefBonus = 300; cuddlepile.auraNameFilter = "Snugglet"; cuddlepile.auraExcludesSelf = true;

            var fortress = Rel("Snugglet Blanket Fortress", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Beast, 2500, 2800,
                "You control 3 \"Snugglet\" monsters and have 3+ cards in your Graveyard. Tribute 1 monster you control. Cost 3 Mana.", 3,
                Fx("Pull the Blanket Tight", "Once per turn, during either player's turn: Pay 2 Mana; 1 \"Snugglet\" monster you control cannot be destroyed this turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.AllyMonster, nameFilter: "Snugglet")));
            fortress.reqNamedOnField = "Snugglet"; fortress.reqNamedCount = 3;
            fortress.reqGraveyardAtLeast = 3;
            fortress.costTributeOtherMonster = true;
            fortress.protectsNamedFromTargeting = "Snugglet";

            var squish = Rel("Snugglet, the Whole Squish", CardRarity.Legendary, 3, MonsterAttribute.Light, MonsterType.Beast, 3000, 3000,
                "You control 3 \"Snugglet\" monsters and have 5+ cards in your Graveyard. Tribute 2 monsters you control. Cost 4 Mana.", 4,
                Fx("The Whole Family", "When this card is Summoned: Special Summon up to 2 \"Snugglet\" monsters from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, targetCount: 2, upTo: true, nameFilter: "Snugglet")),
                Inf("Group Hug", "Once per turn: Pay 2 Mana; up to 3 \"Snugglet\" monsters you control gain 400 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.AllyMonster, targetCount: 3, upTo: true, nameFilter: "Snugglet")));
            squish.reqNamedOnField = "Snugglet"; squish.reqNamedCount = 3;
            squish.reqGraveyardAtLeast = 5;
            squish.costTributeOwnMonsters = 2;
            squish.auraAtkBonus = 300; squish.auraDefBonus = 300; squish.auraNameFilter = "Snugglet"; squish.auraExcludesSelf = true;
        }
    }
}
