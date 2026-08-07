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
            EffectCountKind countKind = EffectCountKind.OwnArtifactsOnField)
        {
            var action = new EffectAction
            {
                type = type, amount = amount, target = target, levelFilter = level,
                targetCount = targetCount, upToTargets = upTo, maxAtkFilter = maxAtk,
                isCost = isCost, targetExcludesSelf = excludeSelf,
                nameFilter = nameFilter, mentionsFilter = mentions, countKind = countKind
            };
            if (attribute.HasValue) { action.useAttributeFilter = true; action.attributeFilter = attribute.Value; }
            if (monsterType.HasValue) { action.useTypeFilter = true; action.typeFilter = monsterType.Value; }
            return action;
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
            cardName.Replace(",", "").Replace("'", "").Replace(" ", "");

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
            card.auraAtkBonus = 0; card.auraDefBonus = 0; card.auraNameFilter = "";
            card.auraUseTypeFilter = false; card.auraLevelFilter = 0;
            card.auraOnlyFaceDown = false; card.auraExcludesSelf = false;
            card.passiveTaunt = false; card.battleShieldMinOwnArtifacts = 0;
            card.tributeWorth = 1; card.protectsNamedFromTargeting = "";
            card.conditionalDoubleAttack = false;
            card.passiveAtkPerCount = 0; card.passiveDefPerCount = 0;
            card.passiveCannotAttack = false; card.passiveNoAttackOnSummonTurn = false;

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
            card.selfSummonPosition = BattlePosition.Defense;
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
    }
}
