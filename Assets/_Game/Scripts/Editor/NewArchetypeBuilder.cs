using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Legt die 61 Karten der fünf Archetypes aus KARTEN-DESIGN-5-ARCHETYPES.md an
    /// und hängt sie in den Katalog.
    ///
    /// Läuft mehrfach: bestehende Assets werden überschrieben, nicht verdoppelt.
    /// Das Artwork bleibt dabei erhalten — sonst wäre jede Korrektur am Effekttext
    /// eine Runde Bildergenerieren.
    /// </summary>
    public static class NewArchetypeBuilder
    {
        private const string MonsterDir  = "Assets/_Game/Data/Tcg/Monsters";
        private const string RelicDir    = "Assets/_Game/Data/Tcg/Reliquary";
        private const string SpellDir    = "Assets/_Game/Data/Tcg/Spells";
        private const string ArtifactDir = "Assets/_Game/Data/Tcg/Artifacts";
        private const string CatalogPath = "Assets/_Game/Data/Tcg/CardCatalog.asset";

        private static readonly List<CardDefinition> built = new List<CardDefinition>();

        [MenuItem("Rouge TCG/Build 5 New Archetypes")]
        public static void Build()
        {
            built.Clear();
            Mechination();
            Sleightwind();
            Kindlekin();
            Manacle();
            Sacrilegion();

            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            int added = 0;
            foreach (var card in built)
                if (!catalog.cards.Contains(card)) { catalog.cards.Add(card); added++; }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Archetypes] {built.Count} Karten gebaut, {added} neu im Katalog " +
                      $"({catalog.cards.Count} gesamt).");
        }

        // ================== Bausteine ==================

        private static EffectAction Act(EffectActionType type, int amount = 1,
            TargetKind target = TargetKind.None, int level = 0,
            MonsterAttribute? attribute = null, MonsterType? monsterType = null,
            int targetCount = 1, bool upTo = false, int maxAtk = 0, bool isCost = false,
            bool excludeSelf = false)
        {
            var action = new EffectAction
            {
                type = type, amount = amount, target = target, levelFilter = level,
                targetCount = targetCount, upToTargets = upTo, maxAtkFilter = maxAtk,
                isCost = isCost, targetExcludesSelf = excludeSelf
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

        /// <summary>Infused-Effekt. coupled = Upgrade des Normal-Effekts darüber.</summary>
        private static EffectDefinition Inf(string label, string text, EffectTrigger trigger,
            int mana, bool coupled, params EffectAction[] actions)
        {
            var effect = Fx(label, text, trigger, mana, true, actions);
            effect.isInfused = true;
            effect.infusedKind = coupled ? InfusedKind.Coupled : InfusedKind.Standalone;
            return effect;
        }

        // ================== Asset-Anlage ==================

        private static string FileName(string cardName)
        {
            var clean = cardName.Replace(",", "").Replace("'", "").Replace(" ", "");
            return clean;
        }

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

            if (fresh) AssetDatabase.CreateAsset(card, path);
            else EditorUtility.SetDirty(card);
            built.Add(card);
            return card;
        }

        private static MonsterCardData Mon(string dir, string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = Make<MonsterCardData>(dir, name, rarity, effects);
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

        /// <summary>Selbst-Spezialbeschwörung anhängen (Kette am Monster).</summary>
        private static MonsterCardData SelfSummon(this MonsterCardData card, string requiredName = "",
            int count = 1, int opponentMonsters = 0, BattlePosition position = BattlePosition.Defense)
        {
            card.canSelfSpecialSummon = true;
            card.selfSummonRequiresNameOnField = requiredName;
            card.selfSummonRequiredNameCount = count;
            card.selfSummonRequiresOpponentMonsters = opponentMonsters;
            card.selfSummonPosition = position;
            return card;
        }

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

        private static SpellCardData Spell(string name, CardRarity rarity, bool quick,
            params EffectDefinition[] effects)
        {
            var card = Make<SpellCardData>(SpellDir, name, rarity, effects);
            card.speed = quick ? SpellSpeed.Quick : SpellSpeed.Normal;
            return card;
        }

        private static ArtifactCardData Artifact(string name, CardRarity rarity, ArtifactSlot slot,
            int atkBonus, int defBonus, MonsterType? protect, params EffectDefinition[] effects)
        {
            var card = Make<ArtifactCardData>(ArtifactDir, name, rarity, effects);
            card.slot = slot; card.atkBonus = atkBonus; card.defBonus = defBonus;
            card.protectTypeFromEffectDestruction = protect.HasValue;
            if (protect.HasValue) card.protectedType = protect.Value;
            card.redirectDestructionToSelf = false;
            return card;
        }

        // ================== 1 · MECHINATION (Earth / Mecha) ==================

        private static void Mechination()
        {
            const MonsterAttribute E = MonsterAttribute.Earth;
            const MonsterType M = MonsterType.Mecha;

            Mon(MonsterDir, "Mechination Cogwright", CardRarity.Common, 1, E, M, 500, 900,
                Fx("Read the Plan", "When this card is Normal Summoned: Add 1 Level 1 EARTH monster from your Deck to your hand.",
                    EffectTrigger.OnNormalSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, E)),
                Inf("Fit the Part", "You can pay 2 Mana: Special Summon 1 Level 1 EARTH monster from your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, 1, E)));

            Mon(MonsterDir, "Mechination Spindle", CardRarity.Common, 1, E, M, 800, 400,
                Fx("Unwind", "Once per turn: Send this card from your hand to the Graveyard; Special Summon 1 Level 1 MECHA monster from your Graveyard.",
                    EffectTrigger.HandIgnition, 0, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, null, M)));

            Mon(MonsterDir, "Mechination Ratchet", CardRarity.Common, 1, E, M, 400, 1200,
                Fx("One Notch Tighter", "When this card is Summoned: Gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 1)))
                .SelfSummon("Mechination");

            Mon(MonsterDir, "Mechination Boltling", CardRarity.Uncommon, 1, E, M, 900, 300,
                Fx("Haul It Up", "Once per turn: You can pay 1 Mana; Special Summon 1 Level 1 monster from your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, 1)),
                Inf("Haul It Back", "Instead, pay 3 Mana: Special Summon 1 Level 1 monster from your Graveyard.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1)));

            Mon(MonsterDir, "Mechination Hammerhand", CardRarity.Common, 2, E, M, 1700, 1200,
                Fx("Call the Heavy", "When this card is Summoned: Add 1 Level 2 EARTH monster from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 2, E)),
                Inf("Call the Small", "You can pay 2 Mana: Add 1 Level 1 EARTH monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, E)));

            Mon(MonsterDir, "Mechination Gearmaw", CardRarity.Uncommon, 2, E, M, 1500, 1600,
                Inf("Jam the Works", "You can pay 2 Mana: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)))
                .SelfSummon("Mechination", 2);

            Mon(MonsterDir, "Mechination Kilnwarden", CardRarity.Uncommon, 2, E, M, 1300, 2000,
                Fx("Reforge", "Once per turn: Special Summon 1 Level 1 EARTH monster from your Graveyard.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, E)),
                Inf("Reforge Twice", "Instead, pay 2 Mana: Special Summon up to 2 Level 1 EARTH monsters from your Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, E, null, 2, true)));

            Mon(MonsterDir, "Mechination Pistonlord", CardRarity.Rare, 3, E, M, 2400, 1800,
                Fx("Drive Through", "When this card is Summoned: Destroy 1 monster your opponent controls with 1500 or less ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, 0, null, null, 1, false, 1500)),
                Inf("Drive Harder", "You can pay 3 Mana: Destroy 1 monster your opponent controls with 2500 or less ATK.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, 0, null, null, 1, false, 2500)));

            Mon(MonsterDir, "Mechination Overseer", CardRarity.Rare, 3, E, M, 2200, 2200,
                Fx("Second Shift", "Once per turn: You can pay 2 Mana; 1 MECHA monster you control can attack twice this turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.AllyMonster, 0, null, M)),
                Inf("Double Shift", "Instead, pay 4 Mana: Up to 2 MECHA monsters you control can each attack twice this turn.",
                    EffectTrigger.Ignition, 4, true,
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.AllyMonster, 0, null, M, 2, true)),
                Inf("Overclock", "You can pay 2 Mana: 1 MECHA monster you control gains 600 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 600, TargetKind.AllyMonster, 0, null, M)));

            Rel("Mechination Assemblage", CardRarity.Rare, 3, E, M, 2500, 2000,
                "You control 2+ monsters. Cost 2 Mana.", 2,
                Fx("Fit Together", "When this card is Summoned: Add 1 Level 2 EARTH monster from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 2, E)),
                Inf("Salvage", "You can pay 2 Mana: Special Summon 1 Level 1 EARTH monster from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, E)))
                .reqOwnMonstersAtLeast = 2;

            var worldgear = Rel("Mechination Worldgear", CardRarity.Legendary, 3, E, M, 3000, 2600,
                "You control 3+ monsters and 5+ cards in your Graveyard. Cost 3 Mana. Destroy 1 other monster you control.", 3,
                Fx("The Great Turn", "When this card is Summoned: Special Summon up to 2 Level 1 EARTH monsters from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, E, null, 2, true)),
                Inf("The Greater Turn", "Instead, pay 3 Mana: Special Summon up to 2 Level 2 EARTH monsters from your Graveyard.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 2, E, null, 2, true)),
                Inf("Strip the Gears", "You can pay 3 Mana: Destroy 1 card your opponent controls.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyCardOnField)));
            worldgear.reqOwnMonstersAtLeast = 3;
            worldgear.reqGraveyardAtLeast = 5;
            worldgear.costTributeOtherMonster = true;

            Artifact("Mechination Assembly Line", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0, M,
                Fx("Night Shift", "During your Standby Phase: Add 1 Level 1 EARTH monster from your Graveyard to your hand.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, 1, E)),
                Inf("Rush Order", "You can pay 2 Mana: Add 1 Level 1 EARTH monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, E)));

            Spell("Mechination Blueprint", CardRarity.Common, false,
                Fx("Blueprint", "Add 1 Level 1 EARTH monster from your Deck to your hand.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, E)));

            Spell("Mechination Recast", CardRarity.Common, false,
                Fx("Recast", "Pay 1 Mana: Special Summon 1 Level 1 monster from your Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1)));

            Spell("Mechination Overdrive", CardRarity.Uncommon, true,
                Fx("Overdrive", "Pay 2 Mana: Special Summon 1 MECHA monster from your hand.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, 0, null, M)));
        }

        // ================== 2 · SLEIGHTWIND (Wind / Demon) ==================

        private static void Sleightwind()
        {
            const MonsterAttribute W = MonsterAttribute.Wind;
            const MonsterType D = MonsterType.Demon;

            Mon(MonsterDir, "Sleightwind Whisperer", CardRarity.Uncommon, 1, W, D, 700, 700,
                Fx("Hush", "Once per turn, during either player's turn: Discard this card from your hand; 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.HandQuick, 0, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Hush Entirely", "Instead, pay 2 Mana: That monster also cannot change its battle position this turn.",
                    EffectTrigger.HandQuick, 2, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockPositionThisTurn, 1, TargetKind.EnemyMonster)));

            Mon(MonsterDir, "Sleightwind Doubtbringer", CardRarity.Rare, 1, W, D, 1000, 500,
                Fx("Second Thoughts", "Once per turn, during either player's turn: Pay 1 Mana and discard this card from your hand; negate the effects of 1 card on the field until the end of this turn.",
                    EffectTrigger.HandQuick, 1, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)),
                Inf("Lasting Doubt", "Instead, pay 3 Mana: Also make that card lose 500 ATK permanently.",
                    EffectTrigger.HandQuick, 3, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField),
                    Act(EffectActionType.DebuffTargetAtk, 500, TargetKind.EnemyMonster)),
                Inf("Recall a Whisper", "You can pay 2 Mana: Add 1 Level 1 WIND monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, 1, W)));

            Mon(MonsterDir, "Sleightwind Maskbearer", CardRarity.Rare, 2, W, D, 1600, 1400,
                Fx("Take the Face", "Once per turn, during either player's turn: Pay 2 Mana and discard this card from your hand; return 1 monster on the field to its owner's hand.",
                    EffectTrigger.HandQuick, 2, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.AnyMonster)),
                Inf("Take the Memory", "You can pay 3 Mana: Banish 1 card from your opponent's Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardAny)));

            Mon(MonsterDir, "Sleightwind Thornmother", CardRarity.Uncommon, 2, W, D, 1400, 1800,
                Fx("Creeping Briar", "Once per turn, during either player's turn: Discard this card from your hand; 1 monster your opponent controls loses 600 ATK permanently.",
                    EffectTrigger.HandQuick, 0, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.DebuffTargetAtk, 600, TargetKind.EnemyMonster)));

            Spell("Sleightwind Hush", CardRarity.Uncommon, true,
                Fx("Hush", "Pay 1 Mana: Add 1 Level 1 WIND monster from your Graveyard to your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, 1, W)));

            Spell("Sleightwind Second Face", CardRarity.Rare, false,
                Fx("Second Face", "Add 1 Level 1 WIND monster from your Deck to your hand; gain 1 Mana.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, W),
                    Act(EffectActionType.GainMana, 1)));

            var choir = Rel("Sleightwind Choir of Two", CardRarity.Rare, 2, W, D, 2100, 1700,
                "3+ cards in your Graveyard. Cost 2 Mana.", 2,
                Fx("Sing It Back", "Once per turn: You can pay 1 Mana; add 1 card from your Graveyard to your hand.",
                    EffectTrigger.Quick, 1, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf)),
                Inf("Sing It All Back", "Instead, pay 3 Mana: Add up to 2 cards from your Graveyard to your hand.",
                    EffectTrigger.Quick, 3, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardCardSelf, 0, null, null, 2, true)),
                Inf("Still the Air", "You can pay 2 Mana: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));
            choir.reqGraveyardAtLeast = 3;

            var unwitnessed = Rel("Sleightwind the Unwitnessed", CardRarity.Legendary, 3, W, D, 2800, 2400,
                "6+ cards in your Graveyard and you control no monsters. Cost 3 Mana.", 3,
                Fx("Unmake", "Once per turn: You can pay 2 Mana; negate the effects of 1 card on the field until the end of this turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField)),
                Inf("Unmake Entirely", "Instead, pay 4 Mana: Also return that card to its owner's hand.",
                    EffectTrigger.Quick, 4, true,
                    Act(EffectActionType.NegateTargetCard, 1, TargetKind.EnemyCardOnField),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)),
                Inf("Nothing Was Lost", "You can pay 2 Mana: Add 1 Level 1 WIND monster from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, 1, W)));
            unwitnessed.reqGraveyardAtLeast = 6;
            unwitnessed.reqControlNoMonsters = true;
        }

        // ================== 3 · KINDLEKIN (Fire / Beast) ==================

        private static void Kindlekin()
        {
            const MonsterAttribute F = MonsterAttribute.Fire;
            const MonsterType B = MonsterType.Beast;

            Mon(MonsterDir, "Kindlekin Spark", CardRarity.Common, 1, F, B, 800, 200,
                Fx("Catch", "When this card is Summoned: Gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 1)))
                .SelfSummon("Kindlekin");

            Mon(MonsterDir, "Kindlekin Ashling", CardRarity.Common, 1, F, B, 500, 1000)
                .SelfSummon("", 1, 1);

            Mon(MonsterDir, "Kindlekin Flickerpaw", CardRarity.Uncommon, 1, F, B, 1000, 400,
                Fx("Scent the Litter", "When this card is Normal Summoned: Add 1 Level 1 FIRE monster from your Deck to your hand.",
                    EffectTrigger.OnNormalSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, F)),
                Inf("Call the Litter", "You can pay 2 Mana: Special Summon 1 Level 1 FIRE monster from your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, 1, F)));

            Mon(MonsterDir, "Kindlekin Emberwing", CardRarity.Uncommon, 1, F, B, 900, 900,
                Fx("Fan the Ashes", "Once per turn: Special Summon 1 Level 1 FIRE monster from your Graveyard.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F)),
                Inf("Fan Them All", "Instead, pay 2 Mana: Special Summon up to 2 Level 1 FIRE monsters from your Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F, null, 2, true)))
                .SelfSummon("Kindlekin", 2);

            Mon(MonsterDir, "Kindlekin Hearthnurse", CardRarity.Uncommon, 1, F, B, 300, 1400,
                Fx("From the Den", "Once per turn: You can pay 1 Mana; Special Summon 1 Level 1 FIRE monster from your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, 1, F)),
                Inf("From the Deep Den", "Instead, pay 3 Mana: Special Summon 1 Level 1 FIRE monster from your Deck.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFilteredSelf, 1, F)));

            Mon(MonsterDir, "Kindlekin Pyrewhelp", CardRarity.Rare, 1, F, B, 1200, 300,
                Fx("Burst of Cinders", "When this card is destroyed: Special Summon 1 Level 1 FIRE monster from your Graveyard.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F)),
                Inf("Stoke", "You can pay 2 Mana: 1 FIRE monster you control gains 500 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 500, TargetKind.AllyMonster, 0, F)))
                .SelfSummon("Kindlekin");

            Spell("Kindlekin Tinderfall", CardRarity.Common, false,
                Fx("Tinderfall", "Add 1 Level 1 FIRE monster from your Deck to your hand; gain 1 Mana.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, F),
                    Act(EffectActionType.GainMana, 1)));

            Rel("Kindlekin Pyre Warden", CardRarity.Uncommon, 2, F, B, 2000, 1200,
                "You control 2+ monsters. Cost 1 Mana.", 1,
                Fx("Guard the Fire", "When this card is Summoned: Special Summon 1 Level 1 FIRE monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F)),
                Inf("Find the Kindling", "You can pay 2 Mana: Add 1 Level 1 FIRE monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, F)))
                .reqOwnMonstersAtLeast = 2;

            Rel("Kindlekin Emberthrone", CardRarity.Rare, 3, F, B, 2400, 2000,
                "You control 3+ monsters. Cost 2 Mana.", 2,
                Fx("Call to the Coals", "Once per turn: You can pay 1 Mana; Special Summon 1 Level 1 FIRE monster from your Graveyard.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F)),
                Inf("Call Them All", "Instead, pay 3 Mana: Special Summon up to 2 Level 1 FIRE monsters from your Graveyard.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F, null, 2, true)),
                Inf("Warm the Pack", "You can pay 2 Mana: Up to 3 BEAST monsters you control gain 300 ATK until the end of this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 300, TargetKind.AllyMonster, 0, null, B, 3, true)))
                .reqOwnMonstersAtLeast = 3;

            var lastEmber = Rel("Kindlekin, the Last Ember", CardRarity.Legendary, 3, F, B, 3000, 2200,
                "You control 4+ monsters and 6+ cards in your Graveyard. Cost 4 Mana. Banish 2 monsters from your Graveyard.", 4,
                Fx("Everything Burns", "When this card is Summoned: Destroy all monsters on the field except BEAST monsters.",
                    EffectTrigger.OnSummonSelf, 0, false,
                    Act(EffectActionType.DestroyAllMonstersExceptType, 1, TargetKind.None, 0, null, B)),
                Inf("From the Ashes", "You can pay 3 Mana: Special Summon up to 2 Level 1 FIRE monsters from your Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, F, null, 2, true)));
            lastEmber.reqOwnMonstersAtLeast = 4;
            lastEmber.reqGraveyardAtLeast = 6;
            lastEmber.costBanishMonstersFromGrave = 2;
        }

        // ================== 4 · MANACLE (Dark / Myth) ==================

        private static void Manacle()
        {
            const MonsterAttribute K = MonsterAttribute.Dark;
            const MonsterType Y = MonsterType.Myth;

            Mon(MonsterDir, "Manacle Tollkeeper", CardRarity.Common, 1, K, Y, 600, 900,
                Fx("The Toll", "When this card is Normal Summoned: Your opponent has 1 less Mana during their next turn.",
                    EffectTrigger.OnNormalSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1)));

            Mon(MonsterDir, "Manacle Gleaner", CardRarity.Common, 1, K, Y, 900, 500,
                Fx("Glean", "Once per turn: Gain 1 Mana.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.GainMana, 1)));

            Mon(MonsterDir, "Manacle Coinbiter", CardRarity.Uncommon, 1, K, Y, 400, 1100,
                Fx("Bite the Coin", "Once per turn, during either player's turn: Your opponent loses 1 Mana.",
                    EffectTrigger.Quick, 0, true,
                    Act(EffectActionType.DrainOpponentMana, 1)),
                Inf("Bite Deeper", "Instead, pay 2 Mana: Your opponent has 2 less Mana during their next turn.",
                    EffectTrigger.Quick, 2, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 2)));

            Mon(MonsterDir, "Manacle Debtwarden", CardRarity.Uncommon, 2, K, Y, 1600, 1300,
                Fx("Enter the Debt", "When this card is Summoned: Your opponent has 1 less Mana during their next turn, and you have 1 more during yours.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 1),
                    Act(EffectActionType.GainManaNextTurn, 1)));

            Mon(MonsterDir, "Manacle Ledgerkeeper", CardRarity.Uncommon, 2, K, Y, 1200, 1900,
                Fx("Consult the Ledger", "Once per turn: You can pay 1 Mana; add 1 Level 1 DARK monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, K)),
                Inf("Skim the Top", "You can pay 2 Mana: Your opponent loses 1 Mana and you gain 1 Mana.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DrainOpponentMana, 1),
                    Act(EffectActionType.GainMana, 1)));

            Mon(MonsterDir, "Manacle Usurer", CardRarity.Rare, 2, K, Y, 1800, 1000,
                Fx("Call the Loan", "Once per turn, during either player's turn: You can pay 1 Mana; your opponent loses 2 Mana.",
                    EffectTrigger.Quick, 1, true,
                    Act(EffectActionType.DrainOpponentMana, 2)),
                Inf("Compound It", "Instead, pay 3 Mana: Your opponent has 3 less Mana during their next turn.",
                    EffectTrigger.Quick, 3, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 3)),
                Inf("Find the Debtor", "You can pay 2 Mana: Add 1 Level 1 DARK monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, K)));

            Mon(MonsterDir, "Manacle Assessor", CardRarity.Rare, 3, K, Y, 2300, 1700,
                Fx("Assess", "When this card is Summoned: Your opponent loses 2 Mana; you gain 1 Mana.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentMana, 2),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Reassess", "You can pay 3 Mana: Your opponent loses 2 Mana, and you have 2 more Mana during your next turn.",
                    EffectTrigger.Quick, 3, false,
                    Act(EffectActionType.DrainOpponentMana, 2),
                    Act(EffectActionType.GainManaNextTurn, 2)));

            Mon(MonsterDir, "Manacle Bailiff", CardRarity.Rare, 3, K, Y, 2000, 2400,
                Fx("Seal the Vault", "When this card is Summoned: Your opponent has 2 less Mana during their next turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 2)),
                Inf("Seal It Shut", "Instead, pay 3 Mana: Your opponent has 3 less Mana during their next turn, and you have 1 more during yours.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 3),
                    Act(EffectActionType.GainManaNextTurn, 1)));

            Spell("Manacle Levy", CardRarity.Uncommon, true,
                Fx("Levy", "Pay 1 Mana: Your opponent loses 2 Mana.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DrainOpponentMana, 2)));

            Spell("Manacle Reckoning", CardRarity.Rare, false,
                Fx("Reckoning", "Pay 2 Mana: Your opponent has 3 less Mana during their next turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 3)));

            Artifact("Manacle Countinghouse", CardRarity.Rare, ArtifactSlot.Field, 0, 0, Y,
                Fx("The House Always Counts", "During your Standby Phase: Gain 1 Mana.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.GainMana, 1)),
                Inf("Collect", "You can pay 2 Mana: Your opponent loses 1 Mana.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DrainOpponentMana, 1)));

            Rel("Manacle Debt Collector", CardRarity.Rare, 2, K, Y, 2100, 1600,
                "You have 5+ Mana available. Cost 2 Mana.", 2,
                Fx("Come to Collect", "When this card is Summoned: Your opponent has 2 less Mana during their next turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 2)),
                Inf("Take It Now", "You can pay 2 Mana: Your opponent loses 2 Mana.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.DrainOpponentMana, 2)))
                .reqMinMana = 5;

            var ledger = Rel("Manacle, the Final Ledger", CardRarity.Legendary, 3, K, Y, 2900, 2500,
                "You have 7+ Mana available and 6+ cards in your Graveyard. Cost 4 Mana.", 4,
                Fx("Close the Book", "When this card is Summoned: Your opponent has 3 less Mana during their next turn, and you have 2 more during yours.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 3),
                    Act(EffectActionType.GainManaNextTurn, 2)),
                Inf("Foreclose", "Instead, pay 5 Mana: Your opponent has 5 less Mana during their next turn.",
                    EffectTrigger.Ignition, 5, true,
                    Act(EffectActionType.DrainOpponentManaNextTurn, 5)),
                Inf("Read the Names", "You can pay 2 Mana: Add 1 Level 1 DARK monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, K)));
            ledger.reqMinMana = 7;
            ledger.reqGraveyardAtLeast = 6;
        }

        // ================== 5 · SACRILEGION (Light / Dragon) ==================

        private static void Sacrilegion()
        {
            const MonsterAttribute L = MonsterAttribute.Light;
            const MonsterType G = MonsterType.Dragon;

            Mon(MonsterDir, "Sacrilegion Acolyte", CardRarity.Common, 1, L, G, 600, 1000,
                Fx("Read the Rite", "When this card is Normal Summoned: Add 1 Level 1 LIGHT monster from your Deck to your hand.",
                    EffectTrigger.OnNormalSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, L)),
                Inf("Raise the Fallen", "You can pay 2 Mana: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)));

            Mon(MonsterDir, "Sacrilegion Oathling", CardRarity.Common, 1, L, G, 1000, 600)
                .SelfSummon("Sacrilegion");

            Mon(MonsterDir, "Sacrilegion Pledgebearer", CardRarity.Uncommon, 1, L, G, 800, 800,
                Fx("Give the Pledge", "Once per turn: Send this card from your hand to the Graveyard; Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.HandIgnition, 0, true,
                    Act(EffectActionType.SendSelfToGraveyard, 1, TargetKind.SelfCard, 0, null, null, 1, false, 0, true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)));

            Mon(MonsterDir, "Sacrilegion Herald", CardRarity.Uncommon, 2, L, G, 1700, 1300,
                Fx("Sound the Oath", "When this card is Summoned: Add 1 Level 2 LIGHT monster from your Deck to your hand.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 2, L)),
                Inf("Sound It Lower", "You can pay 2 Mana: Add 1 Level 1 LIGHT monster from your Deck to your hand.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckMonsterFiltered, 1, L)));

            Mon(MonsterDir, "Sacrilegion Vowkeeper", CardRarity.Rare, 2, L, G, 1500, 1700,
                Fx("Keep the Vow", "Once per turn: You can pay 1 Mana; Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)),
                Inf("Break the Vow", "Instead, pay 3 Mana: Special Summon 1 Level 1 LIGHT monster from your Deck.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFilteredSelf, 1, L)));

            Mon(MonsterDir, "Sacrilegion Sanctifier", CardRarity.Rare, 3, L, G, 2400, 2000,
                Fx("Pin in Place", "When this card is Summoned: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)),
                Inf("Send It Away", "Instead, pay 3 Mana: Return that monster to its owner's hand.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)),
                Inf("Raise the Fallen", "You can pay 2 Mana: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)));

            Artifact("Sacrilegion Covenant Stone", CardRarity.Rare, ArtifactSlot.Field, 0, 0, G,
                Fx("The Stone Remembers", "During your Standby Phase: Add 1 Level 1 LIGHT monster from your Graveyard to your hand.",
                    EffectTrigger.StandbyPhase, 0, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardMonsterSelf, 1, L)),
                Inf("The Stone Answers", "You can pay 2 Mana: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)));

            Artifact("Sacrilegion Binding Chain", CardRarity.Uncommon, ArtifactSlot.Monster, 500, 500, null,
                Fx("Bind", "When this card is equipped: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));

            Spell("Sacrilegion Rite of Return", CardRarity.Uncommon, false,
                Fx("Rite of Return", "Special Summon 1 Level 1 LIGHT monster from your Graveyard; gain 1 Mana.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L),
                    Act(EffectActionType.GainMana, 1)));

            Spell("Sacrilegion Sworn Oath", CardRarity.Rare, true,
                Fx("Sworn Oath", "Pay 1 Mana: Special Summon 1 Level 1 LIGHT monster from your hand.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SpecialSummonTargetFromHand, 1, TargetKind.HandMonsterFiltered, 1, L)));

            var first = Rel("Sacrilegion First Sacrament", CardRarity.Uncommon, 2, L, G, 2000, 1600,
                "Tribute 1 monster you control and 1 monster your opponent controls. Cost 2 Mana.", 2);
            first.costTributeOwnMonsters = 1;
            first.costTributeOpponentMonsters = 1;

            var second = Rel("Sacrilegion Second Sacrament", CardRarity.Rare, 3, L, G, 2500, 2000,
                "Tribute 2 monsters you control and 1 monster your opponent controls. Cost 3 Mana.", 3,
                Fx("Return the Least", "When this card is Summoned: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)),
                Inf("Return Two", "Instead, pay 2 Mana: Special Summon up to 2 Level 1 LIGHT monsters from your Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L, null, 2, true)));
            second.costTributeOwnMonsters = 2;
            second.costTributeOpponentMonsters = 1;

            var third = Rel("Sacrilegion Third Sacrament", CardRarity.Rare, 3, L, G, 2700, 2300,
                "Tribute 1 monster you control and 2 monsters your opponent controls. Cost 4 Mana.", 4);
            third.costTributeOwnMonsters = 1;
            third.costTributeOpponentMonsters = 2;

            var broken = Rel("Sacrilegion Broken Vow", CardRarity.Rare, 3, L, G, 2600, 2600,
                "You control no monsters and 5+ cards in your Graveyard. Cost 3 Mana.", 3,
                Fx("Begin Again", "When this card is Summoned: Special Summon 1 Level 1 LIGHT monster from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L)),
                Inf("Unmake the Oath", "You can pay 3 Mana: Banish 1 card from either Graveyard.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardAny)));
            broken.reqControlNoMonsters = true;
            broken.reqGraveyardAtLeast = 5;

            var last = Rel("Sacrilegion, the Last Oath", CardRarity.Legendary, 3, L, G, 3200, 2800,
                "Tribute 2 monsters you control and 1 monster your opponent controls. 8+ cards in your Graveyard. Cost 5 Mana.", 5,
                Fx("The Oath Is Kept", "When this card is Summoned: Special Summon up to 2 Level 1 LIGHT monsters from your Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 1, L, null, 2, true)),
                Inf("The Oath Is Raised", "Instead, pay 4 Mana: Special Summon up to 2 Level 2 LIGHT monsters from your Graveyard.",
                    EffectTrigger.Ignition, 4, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf, 2, L, null, 2, true)),
                Inf("Hold Them Down", "You can pay 3 Mana: 1 monster your opponent controls cannot attack this turn.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.EnemyMonster)));
            last.costTributeOwnMonsters = 2;
            last.costTributeOpponentMonsters = 1;
            last.reqGraveyardAtLeast = 8;
        }
    }
}
