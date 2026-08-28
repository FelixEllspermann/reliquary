using System.Linq;
using UnityEditor;
using UnityEngine;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // Die ersten 5 Incarnates + ihre Riten (Designs von Felix, 29.08.2026).
    // Level-Treppe 7/7/8/8/9; Riten opfern namentlich Ice Warden, Archfiend
    // Overlord, Sworn to the Gate, Wenna und The Thousandth Card — alle fünf
    // werden Vessels. Sworn to the Gate wird dabei zum beidseitigen
    // Spezialbeschwörungs-Dekret mit Incarnate-Ausnahme. Idempotent.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build Incarnates (5 + Riten)")]
        public static void BuildIncarnates5()
        {
            built.Clear();
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);

            // ---- 1. Maw of the First Winter (Lv 7) ----
            var maw = InMake("Maw of the First Winter", CardRarity.Rare, 7,
                MonsterAttribute.Water, MonsterType.Myth, 2000, 2000,
                Fx("The First Frost", "When this card is Summoned: every monster your opponent controls is switched to Defense Position and cannot change its position until the end of your opponent's next turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SwitchAllToDefense, 1),
                    Act(EffectActionType.LockTargetPositionTurns, 2, TargetKind.EnemyMonster, targetCount: 5, upTo: true)),
                Inf("Deep Freeze", "QUICK, once per turn — pay 4 Mana: target 1 monster on the field; it permanently loses 500 ATK. If this brings it to 0 ATK, destroy it.",
                    EffectTrigger.Quick, 4, false,
                    Act(EffectActionType.DebuffTargetAtkPermanentDestroyIfZero, 500, TargetKind.AnyMonster)));
            maw.passiveDebuffOpponentAfterCombat = 500;
            maw.passiveNoBattleDestroy = true;

            // ---- 2. The Hungering Demon (Lv 7) ----
            var hunger = InMake("The Hungering Demon", CardRarity.Rare, 7,
                MonsterAttribute.Dark, MonsterType.Demon, 3000, 2500,
                Fx("First Course", "When this card is Summoned: banish the top 3 cards of your opponent's Deck; this card permanently gains 200 ATK for each card banished this way.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishOpponentDeckTopBuffSelfPer, 200, targetCount: 3)),
                Inf("Empty the Larder", "QUICK, once per turn — pay 3 Mana: banish every monster in your Graveyard; this card gains 100 ATK until the end of the turn for each card banished this way.",
                    EffectTrigger.Quick, 3, false,
                    Act(EffectActionType.BanishAllOwnGraveMonstersBuffSelfEotPer, 100)));
            hunger.passiveOpponentNoGraveSummons = true;
            hunger.passiveCannotBeBanished = true;

            // ---- 3. Colossus of the Broken Gate (Lv 8) ----
            var colossus = InMake("Colossus of the Broken Gate", CardRarity.Legendary, 8,
                MonsterAttribute.Earth, MonsterType.Demon, 2400, 2500);
            colossus.passiveNoSpellsBoth = true;
            colossus.passiveUntargetable = true;
            colossus.passiveGrowOnEnemyArtifactActivation = 200;

            // ---- 4. She Who Outlives (Lv 8) ----
            var she = InMake("She Who Outlives", CardRarity.Legendary, 8,
                MonsterAttribute.Light, MonsterType.Myth, 3000, 3000,
                Fx("Her Blessing", "When this card is Summoned: you can choose 1 monster on the field; it can no longer be destroyed by battle.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GrantNoBattleDestroyPermanent, 1, TargetKind.AnyMonster, upTo: true)),
                Inf("Step Beyond the Veil", "QUICK, once per turn — pay 2 Mana: banish this card until the end of the turn (it returns to a free zone, or to the Graveyard if none is free); Special Summon 1 LIGHT monster with 2000 or less ATK from your Graveyard.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.BlinkSelfUntilEot),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf,
                        attribute: MonsterAttribute.Light, maxAtk: 2000).RtInDefense()),
                Inf("Step Further Still", "Instead, pay 5 Mana: Special Summon it from your Deck.",
                    EffectTrigger.Quick, 5, true,
                    Act(EffectActionType.BlinkSelfUntilEot),
                    Act(EffectActionType.SpecialSummonTargetFromDeck, 1, TargetKind.DeckMonsterFiltered,
                        attribute: MonsterAttribute.Light, maxAtk: 2000).RtInDefense()));
            she.passiveDiscardToSurvive = true;

            // ---- 5. Avatar of the Thousandth Card (Lv 9) ----
            var avatar = InMake("Avatar of the Thousandth Card", CardRarity.Legendary, 9,
                MonsterAttribute.Dark, MonsterType.Angel, 0, 0,
                Inf("The Price of Greed", "QUICK, once per turn — pay 2 Mana, when your opponent activates an effect that would let them draw: negate the activation and destroy the card. If it was a monster, inflict damage to your opponent equal to its ATK.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.NegateDrawEffectPunish)).InAvatarWindow());
            avatar.passiveBaseStatsFromOffering = true;
            avatar.passiveNoResponseToOwnerMonsterEffects = true;

            // ---- Die 5 Riten ----
            InRite("Rite of the First Winter", 3, "Ice Warden", "Maw of the First Winter");
            InRite("Rite of Unending Hunger", 3, "Archfiend Overlord", "The Hungering Demon");
            InRite("Rite of the Broken Gate", 4, "Sworn to the Gate", "Colossus of the Broken Gate");
            InRite("Rite of Eternal Life", 4, "Wenna, Who Waits Outside", "She Who Outlives");
            InRite("Rite of the Thousandth", 5, "The Thousandth Card", "Avatar of the Thousandth Card");

            // ---- Die Riten-Opfer werden Vessels; Sworn to the Gate wird zum Dekret ----
            string[] chosenVessels = { "Ice Warden", "Archfiend Overlord", "Sworn to the Gate",
                "Wenna, Who Waits Outside", "The Thousandth Card" };
            foreach (var name in chosenVessels)
            {
                if (catalog.FindByName(name) is MonsterCardData vessel)
                {
                    vessel.isVessel = true;
                    EditorUtility.SetDirty(vessel);
                }
                else Debug.LogError($"[Incarnates] Vessel fehlt: {name}");
            }
            if (catalog.FindByName("Sworn to the Gate") is MonsterCardData sworn)
            {
                sworn.passiveOwnerNoOtherSpecialSummons = false;
                sworn.passiveNoSpecialSummonsBothExceptIncarnates = true;
                EditorUtility.SetDirty(sworn);
            }

            Finish("Incarnates 5 + Riten");
        }

        // ================== Helfer nur für dieses Set ==================

        private static IncarnateCardData InMake(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = Make<IncarnateCardData>(RelicDir, name, rarity, effects);
            W3Reset(card);
            card.releaseVersion = W3Version;
            card.level = 3;                    // Monster-Levelfeld bleibt neutral — das echte Level:
            card.incarnateLevel = level;       // die Opfergabe-Summe
            card.attribute = attribute; card.monsterType = type;
            card.atk = atk; card.def = def;
            card.canSelfSpecialSummon = false;
            card.passiveNoNormalSummon = false;
            return card;
        }

        private static SpellCardData InRite(string name, int mana, string sacrifice, string incarnate)
        {
            var card = W3Spell(name, CardRarity.Rare, false,
                Fx("The Rite", $"Pay {mana} Mana and sacrifice \"{sacrifice}\": Special Summon \"{incarnate}\" from your Extra Deck — permanently.",
                    EffectTrigger.OnActivate, mana, false,
                    Act(EffectActionType.RiteSummonIncarnate)),
                Inf("Prepare the Vessel", $"Pay 1 Mana: add 1 \"{sacrifice}\" from your Deck to your hand.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckCardFiltered, nameFilter: sacrifice)));
            card.isRite = true;
            card.riteSacrificeName = sacrifice;
            card.riteIncarnateName = incarnate;
            return card;
        }

        /// <summary>Avatar-Konter: nur zündbar, wenn das letzte Kettenglied ein Gegner-Draw wäre.</summary>
        private static EffectDefinition InAvatarWindow(this EffectDefinition effect)
        { effect.requiresOpponentDrawChainLink = true; return effect; }

        // ================== Vessel-Nachschlag: Lv-3-Archetypen + Generics ==================

        [MenuItem("Rouge TCG/Mark Vessels II (Lv3 je Archetyp + Generics)")]
        public static void MarkVesselsRound2()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            var families = ArchetypeCatalog.Names
                .Concat(new[] { "Giftwyrm", "Splithoof", "Waylay", "Bylaw", "Chimekeep" })
                .Distinct().ToArray();
            int marked = 0;
            var log = new System.Text.StringBuilder();

            bool InFamily(MonsterCardData m, string family) =>
                m.cardName.StartsWith(family)
                || (ArchetypeCatalog.Exceptions.TryGetValue(m.cardName, out var home) && home == family);

            // Je Archetyp zusätzlich das schwächste Level-3-Monster
            foreach (var family in families)
            {
                var third = catalog.cards.OfType<MonsterCardData>()
                    .Where(m => !(m is ReliquaryCardData) && !(m is IncarnateCardData) && !m.isToken
                        && !m.isVessel && m.level == 3 && InFamily(m, family))
                    .OrderBy(m => m.atk).ThenBy(m => m.cardName)
                    .FirstOrDefault();
                if (third == null) continue;
                third.isVessel = true;
                EditorUtility.SetDirty(third);
                marked++;
                log.Append($"{third.cardName} (Lv3), ");
            }

            // Generics: je Level die ATK-schwächsten familienlosen Monster (4×Lv1, 3×Lv2, 3×Lv3)
            bool IsGeneric(MonsterCardData m) => families.All(f => !InFamily(m, f));
            foreach (var (level, take) in new[] { (1, 4), (2, 3), (3, 3) })
            {
                var picks = catalog.cards.OfType<MonsterCardData>()
                    .Where(m => !(m is ReliquaryCardData) && !(m is IncarnateCardData) && !m.isToken
                        && !m.isVessel && m.level == level && IsGeneric(m))
                    .OrderBy(m => m.atk).ThenBy(m => m.cardName)
                    .Take(take).ToList();
                foreach (var vessel in picks)
                {
                    vessel.isVessel = true;
                    EditorUtility.SetDirty(vessel);
                    marked++;
                    log.Append($"{vessel.cardName} (Lv{level} Generic), ");
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vessels II] {marked} weitere Vessels: {log}");
        }
    }
}
