using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // „The Small Print" (Design v2, freigegeben): 51 Generics — Wetten, Schulden,
    // Tausch, Zonen, Schwüre und Kampfregeln. Nutzt die Batch2026Builder-Helfer
    // (partial) und ist idempotent wie alle Stages: ein zweiter Lauf setzt alle
    // Small-Print-Felder neu, Artworks bleiben unangetastet.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build The Small Print (51 Generics)")]
        public static void BuildSmallPrint()
        {
            built.Clear();
            SpWagers(); SpDebts(); SpBarter(); SpGround(); SpOaths(); SpTermsOfBattle();
            Finish("The Small Print");
        }

        // ================== Kleine Helfer nur für dieses Set ==================

        private static EffectAction Heads(this EffectAction action) { action.coinGate = CoinGate.Heads; return action; }
        private static EffectAction Tails(this EffectAction action) { action.coinGate = CoinGate.Tails; return action; }

        private static EffectDefinition OncePerDuel(this EffectDefinition effect) { effect.oncePerDuel = true; return effect; }
        private static EffectDefinition MainPhaseOnly(this EffectDefinition effect) { effect.onlyDuringMainPhase = true; return effect; }
        private static EffectDefinition BattlePhaseOnly(this EffectDefinition effect) { effect.onlyDuringBattlePhase = true; return effect; }
        private static EffectDefinition OpponentTurnOnly(this EffectDefinition effect) { effect.onlyDuringOpponentTurn = true; return effect; }
        private static EffectDefinition EitherSide(this EffectDefinition effect) { effect.eitherPlayerMayActivate = true; return effect; }

        /// <summary>Set-Version für die NEW-CARDS-Szene der Patchnotes.</summary>
        private const string SmallPrintVersion = "0.1.6b";

        /// <summary>Alle Small-Print-Passives zurücksetzen (Make() kennt nur die alten) + Set-Version stempeln.</summary>
        private static void ResetSmallPrint(CardDefinition card)
        {
            card.releaseVersion = SmallPrintVersion;
            card.auraAdjacentOnly = false; card.auraAloneOnly = false; card.auraCrowdedAtkPenalty = 0;
            card.facingAtkPenalty = 0; card.passiveAdjacentNoEffectDestroy = false;
            card.passiveAdjacentNoBattleDestroy = false; card.passiveAdjacentDebuffOnDestroy = 0;
            card.passiveNoEffectDestroy = false; card.passiveCannotBeTributed = false;
            card.passiveCannotChangePosition = false; card.passiveNoAttackAfterPositionChange = false;
            card.passiveNoNormalSummon = false; card.passiveControllerStandbyLpLoss = 0;
            card.passiveOwnerNoOtherSpecialSummons = false; card.passiveLoneImmunity = false;
            card.passiveLowHandImmunity = false; card.passivePiercing = false;
            card.passiveBearerPiercing = false; card.passiveBreakOnFailedPierce = false;
            card.passiveDirectAttackHalved = false; card.passiveNoDirectAttack = false;
            card.passiveSpellTaxBoth = false; card.passiveOneAttackBonus = 0;
            card.passiveStandbyBonusMana = 0; card.passiveHandCapForSurvival = 0;
            card.passiveDestroyWhenLifeAtMost = 0; card.passiveLifeCostsFree = false;
            card.passiveCoinChoose = false; card.passiveTailsAsHeadsWhenBehind = false;
            card.passiveLienAtkPenalty = 0; card.passiveStolenAtkBonus = 0;
        }

        /// <summary>Monster dieses Sets: Spieler wählt die Position der Selbst-Beschwörung (außer Stone).</summary>
        private static MonsterCardData SpMon(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = Mon(name, rarity, level, attribute, type, atk, def, effects);
            ResetSmallPrint(card);
            card.selfSummonRequiresNoOwnMonsters = false; card.selfSummonRequiresOwnMonsters = 0;
            card.selfSummonRequiresLifeBelowOpponent = false; card.selfSummonRequiresHandAtMost = 0;
            card.selfSummonRequiresHandAtLeast = 0; card.selfSummonRequiresOpponentDefenseMonster = false;
            card.selfSummonRequiresLienOnField = false; card.selfSummonLifeCost = 0;
            card.selfSummonPosition = BattlePosition.Attack;
            card.passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;
            return card;
        }

        private static SpellCardData SpSpell(string name, CardRarity rarity, bool quick, params EffectDefinition[] effects)
        {
            var card = Spell(name, rarity, quick, effects);
            ResetSmallPrint(card);
            return card;
        }

        private static ArtifactCardData SpArtifact(string name, CardRarity rarity, ArtifactSlot slot,
            int atkBonus = 0, int defBonus = 0, params EffectDefinition[] effects)
        {
            var card = Artifact(name, rarity, slot, atkBonus, defBonus, effects);
            ResetSmallPrint(card);
            return card;
        }

        private static ReliquaryCardData SpRel(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            string summonText, int manaCost, params EffectDefinition[] effects)
        {
            var card = Rel(name, rarity, level, attribute, type, atk, def, summonText, manaCost, effects);
            ResetSmallPrint(card);
            card.reqGraveyardSpellsAtLeast = 0; card.reqHandEmpty = false; card.reqControlChangedOnField = false;
            card.passiveNoAttackOnSummonTurn = true; // Small-Print-Reliquaries greifen im Beschwörungszug nicht an
            return card;
        }

        // ================== I. WAGERS — Münzwurf ==================
        private static void SpWagers()
        {
            SpSpell("Heads You Lose", CardRarity.Rare, true,
                Fx("Heads You Lose", "Pay 1 Mana: Flip a coin. Heads — destroy 1 monster on the field. Tails — destroy 1 monster you control instead (if you control none, nothing happens).",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AnyMonster).Heads(),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster).Tails()),
                Inf("Double or Nothing", "Instead, pay 3 Mana: Flip two coins. If at least one lands Heads, destroy 1 monster on the field. If both land Tails, destroy 1 monster you control and your opponent draws 1 card.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.FlipCoin).Tails(),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AnyMonster).Heads(),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AllyMonster).Tails(),
                    Act(EffectActionType.OpponentDraws, 1).Tails()));

            var dice = SpArtifact("Loaded Dice", CardRarity.Rare, ArtifactSlot.Player, 0, 0,
                Inf("Roll Again", "Pay 2 Mana: Flip a coin. Heads — draw 1 card. Tails — discard 1 card.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DrawCards, 1).Heads(),
                    Act(EffectActionType.DiscardFromHandCost, 1, TargetKind.HandCardSelf).Tails()));
            dice.passiveCoinChoose = true;

            var grinner = SpMon("Grinner, Who Plays the Table", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Demon, 1600, 1200,
                Fx("Plays the Table", "When this card is Summoned: Flip a coin. Heads — your opponent discards 1 random card. Tails — you discard 1 random card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DiscardOpponentRandom, 1).Heads(),
                    Act(EffectActionType.DiscardSelfRandom, 1).Tails()),
                Inf("Raise the Stakes", "Pay 2 Mana: Flip a coin. Heads — this card gains 900 ATK until the end of the turn. Tails — it loses 900 ATK until the end of the turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 900, TargetKind.SelfCard).Heads(),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, -900, TargetKind.SelfCard).Tails()));
            grinner.canSelfSpecialSummon = true;
            grinner.selfSummonChecksOpponentField = true;
            grinner.selfSummonRequiresOpponentMonsters = 1;

            SpMon("Pennywhistle, Who Calls It in the Air", CardRarity.Uncommon, 1, MonsterAttribute.Wind, MonsterType.Myth, 800, 800,
                Fx("Calls It in the Air", "When this card is Summoned: Flip a coin. Heads — add 1 Spell from your Deck to your hand. Tails — send the top 2 cards of your Deck to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.AddTargetFromDeckToHand, 1, TargetKind.DeckSpellFiltered).Heads(),
                    Act(EffectActionType.MillSelf, 2).Tails()),
                Inf("Double Down", "Pay 1 Mana: Flip a coin. Heads — gain 2 Mana this turn. Tails — you have 1 less Mana during your next turn.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.GainMana, 2).Heads(),
                    Act(EffectActionType.DrainSelfManaNextTurn, 1).Tails()));

            var nell = SpMon("Nell, Who Bets the Rent", CardRarity.Uncommon, 1, MonsterAttribute.Wind, MonsterType.Human, 700, 1000,
                Inf("Bets the Rent", "Pay 1 Mana: Flip a coin. Heads — return 1 monster your opponent controls with 1800 or less ATK to the hand. Tails — return this card to your hand.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster, maxAtk: 1800).Heads(),
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.SelfCard).Tails()));
            nell.canSelfSpecialSummon = true;
            nell.selfSummonRequiresHandAtMost = 2;

            var house = SpRel("The House Always Wins", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Human, 2400, 2200,
                "3+ Spells in your Graveyard — pay 2 Mana.", 2,
                Fx("The House Always Wins", "When this card is Summoned: Flip 3 coins. For each Heads, destroy 1 monster your opponent controls; for each Tails, you take 500 damage.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster).Heads(),
                    Act(EffectActionType.DamageSelf, 500).Tails(),
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster).Heads(),
                    Act(EffectActionType.DamageSelf, 500).Tails(),
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster).Heads(),
                    Act(EffectActionType.DamageSelf, 500).Tails()));
            house.reqGraveyardSpellsAtLeast = 3;
            house.passiveTailsAsHeadsWhenBehind = true;

            var sabine = SpRel("Sabine, Who Wagers the Crown", CardRarity.Legendary, 3,
                MonsterAttribute.Fire, MonsterType.Human, 2700, 2100,
                "Your LP are lower than your opponent's and 4+ Spells are in your Graveyard — pay 3 Mana.", 3,
                Fx("Wagers the Crown", "When this card is Summoned: Flip a coin. Heads — destroy all monsters your opponent controls. Tails — destroy all other monsters you control.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.DestroyAllEnemyMonsters).Heads(),
                    Act(EffectActionType.DestroyAllOtherOwnMonsters).Tails()),
                Inf("All or Nothing", "Pay 2 Mana: Flip a coin. Heads — this card can attack twice this Battle Phase. Tails — it cannot attack this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.FlipCoin),
                    Act(EffectActionType.AttackAgainSelf).Heads(),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.SelfCard).Tails()));
            sabine.reqLifeBelowOpponent = true;
            sabine.reqGraveyardSpellsAtLeast = 4;
            sabine.passiveNoAttackOnSummonTurn = false; // Alles oder Nichts — sie darf sofort ran
        }

        // ================== II. DEBTS — LP als Kosten, Mana-Schulden, Pfandrecht ==================
        private static void SpDebts()
        {
            SpSpell("Blood for Ink", CardRarity.Uncommon, false,
                Fx("Blood for Ink", "Pay 1 Mana and 1000 LP: Draw 2 cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.PayLifePoints, 1000, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Deeper Cut", "Instead, pay 1 Mana and 2000 LP: Draw 3 cards and gain 1 Mana this turn.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.PayLifePoints, 2000, isCost: true),
                    Act(EffectActionType.DrawCards, 3),
                    Act(EffectActionType.GainMana, 1)));

            SpSpell("The Usurer's Terms", CardRarity.Rare, false,
                Fx("The Usurer's Terms", "Pay 1 Mana: Gain 4 Mana this turn. During your next turn, you have 3 less Mana. If you cannot cover the full amount, you lose 1500 LP for each Mana you could not pay.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.GainMana, 4),
                    Act(EffectActionType.DrainSelfManaNextTurn, 3)),
                Inf("The Usurer's Fine Print", "Instead, pay 1 Mana: Gain 6 Mana this turn; during your next turn, you have 5 less Mana — same terms.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.GainMana, 6),
                    Act(EffectActionType.DrainSelfManaNextTurn, 5)));

            SpSpell("Skimmed Off the Top", CardRarity.Rare, true,
                Fx("Skimmed Off the Top", "Pay 1 Mana: Activate when your opponent activates a card or effect that would give them Mana: they gain none of it — you gain that Mana instead (Mana skimmed during your opponent's turn is added to your next turn).",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.RedirectManaFromChainLink)));

            SpSpell("Pound of Flesh", CardRarity.Rare, false,
                Fx("Pound of Flesh", "Pay 1 Mana and 1500 LP: Destroy 1 monster your opponent controls with 1500 or less ATK.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.PayLifePoints, 1500, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 1500)),
                Inf("Two Pounds of Flesh", "Instead, pay 2 Mana and 3000 LP: Destroy 1 monster your opponent controls with 3000 or less ATK.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.PayLifePoints, 3000, isCost: true),
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.EnemyMonster, maxAtk: 3000)));

            SpSpell("Sign in Blood", CardRarity.Rare, false,
                Fx("Sign in Blood", "Pay 2 Mana and 2000 LP: Special Summon 1 Level 2 monster from your Deck. Its effects are negated until the End Phase, and it cannot attack this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.PayLifePoints, 2000, isCost: true),
                    Act(EffectActionType.SpecialSummonTargetFromDeckSuppressed, 1, TargetKind.DeckMonsterFiltered, level: 2)));

            var ledger = SpArtifact("Ledger of Small Debts", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Small Debts", "Once per turn: Pay 800 LP; gain 1 Mana this turn.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.PayLifePoints, 800, isCost: true),
                    Act(EffectActionType.GainMana, 1)),
                Inf("Larger Debts", "Pay 1 Mana and 1500 LP: Draw 2 cards.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.PayLifePoints, 1500, isCost: true),
                    Act(EffectActionType.DrawCards, 2)));
            ledger.passiveDestroyWhenLifeAtMost = 2000;

            var grale = SpMon("Grale, Who Collects on Sundays", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Human, 1300, 1000,
                Fx("Collects on Sundays", "When this card is Summoned: Place a Lien of 1 on 1 monster your opponent controls (during each of its controller's Standby Phases, they pay that much Mana or it is destroyed).",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceLienOnTarget, 1, TargetKind.EnemyMonster)),
                Inf("Interest", "Pay 2 Mana: Raise the Lien on 1 monster your opponent controls by 1.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.RaiseLienOnTarget, 1, TargetKind.EnemyMonsterWithLien)));
            grale.canSelfSpecialSummon = true;
            grale.selfSummonRequiresLifeBelowOpponent = true;

            var vetch = SpMon("Vetch, Who Never Forgets a Face", CardRarity.Uncommon, 1, MonsterAttribute.Dark, MonsterType.Human, 900, 900,
                Fx("Never Forgets a Face", "When this card is Summoned: Place a Lien of 1 on 1 monster your opponent controls with 1500 or less ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PlaceLienOnTarget, 1, TargetKind.EnemyMonster, maxAtk: 1500)),
                Inf("A Face in the Crowd", "Pay 1 Mana and discard this card: Place a Lien of 1 on 1 monster your opponent controls.",
                    EffectTrigger.HandIgnition, 1, false,
                    Act(EffectActionType.SendSelfToGraveyard, 1, isCost: true),
                    Act(EffectActionType.PlaceLienOnTarget, 1, TargetKind.EnemyMonster)));
            vetch.canSelfSpecialSummon = true;
            vetch.selfSummonRequiresLienOnField = true;

            var bailiff = SpMon("The Bailiff at the Door", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 1500, 1900,
                Fx("Knock, Knock", "When this card is Summoned: Destroy 1 monster with a Lien; draw 1 card.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.AnyMonsterWithLien),
                    Act(EffectActionType.DrawCards, 1)));
            bailiff.canSelfSpecialSummon = true;
            bailiff.selfSummonRequiresLienOnField = true;
            bailiff.passiveLienAtkPenalty = 500;

            var aurel = SpRel("Aurel, Who Collects at Midnight", CardRarity.Rare, 3,
                MonsterAttribute.Dark, MonsterType.Angel, 2600, 2200,
                "Your LP are 4000 or less and 3+ Spells are in your Graveyard — pay 3 Mana.", 3,
                Fx("Collects at Midnight", "When this card is Summoned: Gain 500 LP for each Spell in your Graveyard (max 2500).",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.HealSelfPerCount, 500, targetCount: 5, countKind: EffectCountKind.OwnGraveyardSpells)));
            aurel.reqLifeAtMost = 4000;
            aurel.reqGraveyardSpellsAtLeast = 3;
            aurel.passiveLifeCostsFree = true;

            var oath = SpMon("Blood Oath", CardRarity.Uncommon, 2, MonsterAttribute.Dark, MonsterType.Human, 1700, 1500,
                Fx("Debt Repaid", "When this card is destroyed: Gain 1000 LP.",
                    EffectTrigger.OnDestroyedSelf, 0, true,
                    Act(EffectActionType.HealSelf, 1000)),
                Inf("Sworn in Blood", "Pay 2 Mana and 500 LP: This card permanently gains 500 ATK.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.PayLifePoints, 500, isCost: true),
                    Act(EffectActionType.BuffTargetAtk, 500, TargetKind.SelfCard)));
            oath.passiveNoNormalSummon = true;
            oath.canSelfSpecialSummon = true;
            oath.selfSummonLifeCost = 1000;
        }

        // ================== III. BARTER — Kontrolltausch, Danaergeschenke, Wilderei ==================
        private static void SpBarter()
        {
            SpSpell("Fair Trade", CardRarity.Rare, false,
                Fx("Fair Trade", "Pay 2 Mana: Choose 1 monster you control and 1 monster your opponent controls: swap control of them permanently. The monster you receive cannot attack this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.PickTargetOnly, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.SwapControlWithTarget, 1, TargetKind.EnemyMonster)),
                Inf("Unfair Trade", "Instead, pay 4 Mana: Swap them, draw 1 card, and the monster you gave away has its effects negated until the End Phase.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.PickTargetOnly, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.SwapControlWithTarget, 2, TargetKind.EnemyMonster),
                    Act(EffectActionType.DrawCards, 1)));

            SpSpell("Even Exchange", CardRarity.Uncommon, true,
                Fx("Even Exchange", "Pay 2 Mana: Both players shuffle their hands into their Decks, then each draws as many cards as they shuffled.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ShuffleBothHandsRedraw, 0)),
                Inf("Uneven Exchange", "Instead, pay 3 Mana: The same — but you draw 1 more.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.ShuffleBothHandsRedraw, 1)));

            var elephant = SpMon("The White Elephant", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Animal, 3000, 2600);
            elephant.passiveNoNormalSummon = true;
            elephant.passiveCannotBeTributed = true;
            elephant.passiveControllerStandbyLpLoss = 1000;
            elephant.canSelfSpecialSummon = true;
            elephant.selfSummonChecksOpponentField = true;
            elephant.selfSummonRequiresOpponentMonsters = 2;

            var horse = SpMon("Gift Horse", CardRarity.Uncommon, 2, MonsterAttribute.Wind, MonsterType.Animal, 1900, 1900,
                Fx("Don't Look It in the Mouth", "Once per turn, if you own this card: Give control of it to your opponent; draw 2 cards.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.GiveSelfToOpponent),
                    Act(EffectActionType.DrawCards, 2)));
            horse.passiveCannotAttack = true;
            horse.passiveCannotBeTributed = true;
            horse.passiveControllerStandbyLpLoss = 800;
            horse.canSelfSpecialSummon = true;
            horse.selfSummonRequiresGraveNamedCount = 3;   // leerer Filter = beliebige Karten

            var cradle = SpMon("The Changeling Cradle", CardRarity.Rare, 1, MonsterAttribute.Dark, MonsterType.Myth, 500, 500,
                Fx("Cuckoo's Egg", "When this card is Summoned: Swap control of this card and 1 Level 1 monster your opponent controls; the monster you take cannot attack this turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SwapControlWithTarget, 1, TargetKind.EnemyLevel1Monster)));
            cradle.canSelfSpecialSummon = true;
            cradle.selfSummonChecksOpponentField = true;
            cradle.selfSummonRequiresOpponentMonsters = 1;

            var hessel = SpMon("Hessel of the Crossroads", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Human, 1600, 1600,
                Fx("Crossroads Deal", "When this card is Summoned: Your opponent draws 1 card; you draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OpponentDraws, 1),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Devil's Bargain", "Instead, pay 2 Mana: Your opponent draws 1 card; you draw 3 cards, but you skip your next Draw Phase.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.OpponentDraws, 1),
                    Act(EffectActionType.DrawCards, 3),
                    Act(EffectActionType.SkipOwnNextDrawPhase)));
            hessel.canSelfSpecialSummon = true;
            hessel.selfSummonRequiresArtifact = true;

            SpArtifact("Poacher's Lantern", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Poacher's Lantern", "Pay 2 Mana (either player may activate this): Special Summon 1 monster with 2000 or less ATK from the other player's Graveyard to your field. It cannot attack this turn and is banished if it leaves the field.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SpecialSummonFromOpponentGraveyard, 1, TargetKind.GraveyardMonsterOpponent, maxAtk: 2000)).EitherSide());

            var broker = SpRel("The Broker of Both Sides", CardRarity.Rare, 3,
                MonsterAttribute.Light, MonsterType.Demon, 2500, 2500,
                "A monster on the field is controlled by someone other than its owner — pay 2 Mana.", 2,
                Fx("Both Sides of the Deal", "When this card is Summoned: Swap control of 1 other monster you control and 1 monster your opponent controls.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.PickTargetOnly, 1, TargetKind.AllyMonster, excludeSelf: true),
                    Act(EffectActionType.SwapControlWithTarget, 1, TargetKind.EnemyMonster)));
            broker.reqControlChangedOnField = true;
            broker.passiveStolenAtkBonus = 500;
        }

        // ================== IV. GROUND — Nachbarn, Gegenüber, Bewegung ==================
        private static void SpGround()
        {
            SpSpell("Lock Shields", CardRarity.Uncommon, true,
                Fx("Lock Shields", "Pay 1 Mana: Target 1 monster you control: it and the monsters adjacent to it gain 500 DEF until the end of the turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.AdjacentAllyMonsters)),
                Inf("Shield Wall", "Instead, pay 2 Mana: They gain 700 DEF and cannot be destroyed this turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 700, TargetKind.AllyMonster),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.SameAsPrevious),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 700, TargetKind.AdjacentAllyMonsters),
                    Act(EffectActionType.ProtectTargetThisTurn, 1, TargetKind.SameAsPrevious)));

            SpSpell("Stare Down", CardRarity.Uncommon, true,
                Fx("Stare Down", "Pay 1 Mana: Target 1 monster you control: until the end of the turn it gains ATK equal to half the ATK of the monster facing it.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.GainAtkOfFacingMonsterEot, 50, TargetKind.AllyMonster)),
                Inf("Death Stare", "Instead, pay 2 Mana: It gains ATK equal to the full ATK of the monster facing it.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.GainAtkOfFacingMonsterEot, 100, TargetKind.AllyMonster)));

            var halloway = SpMon("Serjeant Halloway", CardRarity.Rare, 2, MonsterAttribute.Earth, MonsterType.Human, 1500, 1800,
                Inf("Fall In", "Pay 1 Mana: Move this card to an empty monster zone you control.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.MoveSelfToZone, 0)));
            halloway.canSelfSpecialSummon = true;
            halloway.selfSummonRequiresOwnMonsters = 2;
            halloway.auraAtkBonus = 400;
            halloway.auraAdjacentOnly = true;
            halloway.passiveCannotAttack = true;

            var hangman = SpMon("Left Hand of the Hangman", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Demon, 1800, 1000,
                Fx("The Drop", "When this card is Summoned: Destroy the face-up monster facing it if that monster's ATK is lower than this card's.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.FacingEnemyMonster, maxAtk: -1)).Mand(),
                Fx("The Drop", "When this card moves to another zone: Destroy the face-up monster facing it if that monster's ATK is lower than this card's.",
                    EffectTrigger.OnMovedSelf, 0, false,
                    Act(EffectActionType.DestroyTargetMonster, 1, TargetKind.FacingEnemyMonster, maxAtk: -1)).Mand(),
                Inf("Walk the Gallows", "Pay 2 Mana: Move this card to an adjacent empty zone; it cannot attack this turn.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.MoveSelfToZone, 1),
                    Act(EffectActionType.CannotAttackThisTurn, 1, TargetKind.SelfCard)));
            hangman.canSelfSpecialSummon = true;
            hangman.selfSummonChecksOpponentField = true;
            hangman.selfSummonRequiresOpponentMonsters = 2;

            var rook = SpMon("Rook's Gambit", CardRarity.Uncommon, 1, MonsterAttribute.Light, MonsterType.Mecha, 900, 1500,
                Fx("Castle", "Once per turn: Move this card to any empty monster zone you control.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.MoveSelfToZone, 0)));
            rook.facingAtkPenalty = 600;
            rook.passiveCannotAttack = true;

            var volte = SpMon("Volte-Face", CardRarity.Rare, 2, MonsterAttribute.Wind, MonsterType.Human, 1700, 1700,
                Fx("About Turn", "When this card changes its battle position: Draw 1 card.",
                    EffectTrigger.OnPositionChangedSelf, 0, true,
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Double Turn", "Pay 1 Mana: This card may change its battle position one additional time this turn.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.ExtraPositionChangeThisTurn, 1)));
            volte.canSelfSpecialSummon = true;
            volte.selfSummonRequiresFaceDownOnField = true;
            volte.passiveNoAttackAfterPositionChange = true;

            var wall = SpMon("Load-Bearing Wall", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Mecha, 2400, 2600);
            wall.passiveNoNormalSummon = true;
            wall.canSelfSpecialSummon = true;
            wall.selfSummonRequiresOwnMonsters = 2;
            wall.passiveAdjacentNoEffectDestroy = true;
            wall.passiveCannotChangePosition = true;
            wall.passiveAdjacentDebuffOnDestroy = 500;

            var chair = SpArtifact("The Empty Chair", CardRarity.Rare, ArtifactSlot.Field, 0, 0);
            chair.auraAtkBonus = 500;
            chair.auraAloneOnly = true;
            chair.auraCrowdedAtkPenalty = 200;

            var castellan = SpRel("Castellan of the Long Wall", CardRarity.Rare, 3,
                MonsterAttribute.Earth, MonsterType.Human, 2200, 3000,
                "You control 3+ monsters — pay 2 Mana.", 2,
                Fx("Man the Wall", "When this card is Summoned: Move 1 other monster you control to an empty monster zone; then you may move a second one.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MoveTargetToZone, 1, TargetKind.AllyMonster, excludeSelf: true),
                    Act(EffectActionType.MoveTargetToZone, 1, TargetKind.AllyMonster, upTo: true, excludeSelf: true)));
            castellan.reqOwnMonstersAtLeast = 3;
            castellan.passiveAdjacentNoBattleDestroy = true;
            castellan.passiveCannotAttack = true;
            castellan.passiveNoAttackOnSummonTurn = false; // greift ohnehin nie an
        }

        // ================== V. OATHS — Once per Duel, Schwüre, Auflagen ==================
        private static void SpOaths()
        {
            SpSpell("The Unbroken Oath", CardRarity.Legendary, true,
                Fx("The Unbroken Oath", "Once per Duel. Pay 2 Mana: Negate the effects of all cards your opponent controls until the end of this turn. You cannot activate other Spells for the rest of this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.NegateAllOpponentCards),
                    Act(EffectActionType.LockOwnSpellsThisTurn)).OncePerDuel());

            SpSpell("First and Last Word", CardRarity.Legendary, true,
                Fx("First and Last Word", "Once per Duel. Pay 2 Mana: Negate the activation of the Spell your opponent activated last in this chain.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.NegatePreviousChainLink)).OncePerDuel());

            SpSpell("Sworn Statement", CardRarity.Uncommon, false,
                Fx("Sworn Statement", "Pay 1 Mana: Declare Monster, Spell or Artifact, then reveal the top card of your Deck: if it is the declared type, add it to your hand; otherwise send it to the Graveyard.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DeclareTypeRevealTop, 1)),
                Inf("Sworn Twice", "Instead, pay 2 Mana: Reveal the top 2 cards instead — matching cards go to your hand, the rest to the Graveyard.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DeclareTypeRevealTop, 2)));

            var gate = SpMon("Sworn to the Gate", CardRarity.Rare, 3, MonsterAttribute.Light, MonsterType.Human, 2500, 2200);
            gate.passiveNoNormalSummon = true;
            gate.canSelfSpecialSummon = true;
            gate.selfSummonRequiresNoOwnMonsters = true;
            gate.passiveLoneImmunity = true;
            gate.passiveOwnerNoOtherSpecialSummons = true;

            var vow = SpArtifact("Vow of Poverty", CardRarity.Rare, ArtifactSlot.Player, 0, 0);
            vow.passiveStandbyBonusMana = 2;
            vow.passiveHandCapForSurvival = 2;

            var ascetic = SpRel("The Ascetic of the Ninth Stair", CardRarity.Legendary, 3,
                MonsterAttribute.Light, MonsterType.Human, 2800, 2800,
                "You have no cards in your hand — pay 3 Mana.", 3,
                Fx("The Ninth Stair", "When this card is Summoned: Draw 2 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DrawCards, 2)));
            ascetic.reqHandEmpty = true;
            ascetic.passiveLowHandImmunity = true;

            var marrow = SpMon("Marrow, Who Holds Every Card", CardRarity.Rare, 2, MonsterAttribute.Dark, MonsterType.Demon, 1000, 1000,
                Inf("Deal Me In", "Pay 2 Mana: Draw 1 card, then your opponent draws 1 card.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.DrawCards, 1),
                    Act(EffectActionType.OpponentDraws, 1)));
            marrow.canSelfSpecialSummon = true;
            marrow.selfSummonRequiresHandAtLeast = 5;
            marrow.passiveAtkPerCount = 300;
            marrow.passiveAtkPerCountKind = EffectCountKind.OwnHandCards;
            marrow.passiveNoAttackOnSummonTurn = true;
        }

        // ================== VI. TERMS OF BATTLE — neue Kampfregeln ==================
        private static void SpTermsOfBattle()
        {
            var ram = SpArtifact("Ram's Head", CardRarity.Uncommon, ArtifactSlot.Monster, 300, 0);
            ram.passiveBearerPiercing = true;
            ram.passiveBreakOnFailedPierce = true;

            var sweep = SpMon("Chimney Sweep", CardRarity.Uncommon, 1, MonsterAttribute.Fire, MonsterType.Human, 1000, 600);
            sweep.canSelfSpecialSummon = true;
            sweep.selfSummonRequiresArtifact = true;
            sweep.passiveDirectAttackHalved = true;

            SpSpell("High Stakes", CardRarity.Rare, true,
                Fx("High Stakes", "Pay 2 Mana: Only during your opponent's Main Phase: until the end of your next turn, all battle damage to either player is doubled.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.DoubleBattleDamageUntilNextTurnEnd)).MainPhaseOnly().OpponentTurnOnly());

            var tariff = SpArtifact("Guild Tariff", CardRarity.Rare, ArtifactSlot.Field, 0, 0);
            tariff.passiveSpellTaxBoth = true;

            var stone = SpMon("Stone That Would Not Break", CardRarity.Uncommon, 2, MonsterAttribute.Earth, MonsterType.Mecha, 0, 2500);
            stone.canSelfSpecialSummon = true;
            stone.selfSummonRequiresNoOwnMonsters = true;
            stone.selfSummonPosition = BattlePosition.Defense;
            stone.passiveNoEffectDestroy = true;
            stone.passiveCannotBeTributed = true;
            stone.passiveCannotChangePosition = true;
            stone.passiveCannotAttack = true;

            var aurochs = SpMon("Bristleback Aurochs", CardRarity.Rare, 3, MonsterAttribute.Earth, MonsterType.Beast, 2600, 1800,
                Inf("Lower the Horns", "Pay 2 Mana: Switch 1 monster your opponent controls to Defense Position.",
                    EffectTrigger.Ignition, 2, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster)));
            aurochs.canSelfSpecialSummon = true;
            aurochs.selfSummonRequiresOpponentDefenseMonster = true;
            aurochs.passivePiercing = true;
            aurochs.passiveNoDirectAttack = true;

            SpSpell("Trample the Line", CardRarity.Uncommon, false,
                Fx("Trample the Line", "Pay 1 Mana: Target 1 monster you control: it inflicts piercing battle damage this turn and gains 300 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.GrantPiercingThisTurn, 300, TargetKind.AllyMonster)),
                Inf("Trample the Whole Line", "Instead, pay 3 Mana: All monsters you control inflict piercing battle damage this turn (no ATK bonus).",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.GrantPiercingThisTurn, 1)));

            SpSpell("Parley", CardRarity.Rare, true,
                Fx("Parley", "Pay 1 Mana: Only during a Battle Phase: end the Battle Phase. Your opponent draws 1 card.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.EndBattlePhaseNow),
                    Act(EffectActionType.OpponentDraws, 1)).BattlePhaseOnly());

            var code = SpArtifact("The Duelist's Code", CardRarity.Legendary, ArtifactSlot.Field, 0, 0);
            code.passiveOneAttackBonus = 700;
        }
    }
}
