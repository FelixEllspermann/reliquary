using UnityEditor;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // „Welle 3" (Design freigegeben 28.08.2026): 50 Generics in neun Familien —
    // Deals für alle, Ambush & Gegnerzug, Countdown, Grab-Spiele, Feld-Chaos,
    // Wetten & Information, Ressourcen & Tempo, Positions-Tänze, Exil-Ökonomie,
    // Token, Deck-Stapler, Mill, Masken, Battle-Tricks und 4 Reliquaries.
    // Nutzt die Batch2026Builder-Helfer (partial), idempotent wie alle Stages.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Build Wave 3 (50 Generics)")]
        public static void BuildWave3()
        {
            built.Clear();
            W3Token();
            W3Deals(); W3Ambush(); W3Countdown(); W3Grave(); W3Chaos();
            W3Wagers(); W3Tempo(); W3Dancers(); W3Exile(); W3Straw();
            W3Stackers(); W3Mill(); W3Masks(); W3Battle(); W3Relics();
            Finish("Wave 3");
        }

        // ================== Helfer nur für dieses Set ==================

        private const string W3Version = "0.1.8";

        private static void W3Reset(CardDefinition card)
        {
            A5Reset(card);
            card.releaseVersion = W3Version;
            card.countdownZeroKeepsCard = false;
            card.passiveDefWhileDefending = 0;
            card.passiveOpponentMillsBanished = false;
            card.passiveCannotBeBanished = false;
            card.passiveSummonCapBoth = 0;
            card.passiveCountdownsTickTwice = false;
            card.passiveProtectFaceDownFromEffectDestroy = false;
            card.passiveFirstEnemyAttackDetourDeal = false;
            if (card is MonsterCardData monster)
                monster.selfSummonRequiresBanishedCards = 0;
            if (card is ArtifactCardData artifact)
                artifact.redirectDestructionToSelf = false;
        }

        private static MonsterCardData W3Mon(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            params EffectDefinition[] effects)
        {
            var card = A5Mon(name, rarity, level, attribute, type, atk, def, effects);
            W3Reset(card);
            return card;
        }

        private static SpellCardData W3Spell(string name, CardRarity rarity, bool quick, params EffectDefinition[] effects)
        {
            var card = A5Spell(name, rarity, quick, effects);
            W3Reset(card);
            return card;
        }

        private static ArtifactCardData W3Artifact(string name, CardRarity rarity, ArtifactSlot slot,
            int atkBonus = 0, int defBonus = 0, params EffectDefinition[] effects)
        {
            var card = A5Artifact(name, rarity, slot, atkBonus, defBonus, effects);
            W3Reset(card);
            return card;
        }

        private static ReliquaryCardData W3Rel(string name, CardRarity rarity, int level,
            MonsterAttribute attribute, MonsterType type, int atk, int def,
            string summonText, int manaCost, params EffectDefinition[] effects)
        {
            var card = A5Rel(name, rarity, level, attribute, type, atk, def, summonText, manaCost, effects);
            W3Reset(card);
            return card;
        }

        // ================== 0. SCARECROW-TOKEN (eigener Typ, KEIN Illusion-Token) ==================
        private static void W3Token()
        {
            var token = W3Mon("Scarecrow Token", CardRarity.Common, 1,
                MonsterAttribute.Earth, MonsterType.Beast, 0, 500);
            token.isToken = true;
            token.releaseVersion = ""; // Tokens sind kein Sammelgut und keine NEW-CARDS-Zeile

            // In den GameRules verankern — SpawnScarecrowTokens liest rules.scarecrowToken
            foreach (var guid in AssetDatabase.FindAssets("t:GameRules"))
            {
                var rulesAsset = AssetDatabase.LoadAssetAtPath<GameRules>(AssetDatabase.GUIDToAssetPath(guid));
                if (rulesAsset == null) continue;
                rulesAsset.scarecrowToken = token;
                EditorUtility.SetDirty(rulesAsset);
            }
        }

        // ================== A. DER GEGNER ENTSCHEIDET ==================
        private static void W3Deals()
        {
            var peddler = W3Mon("Crossroads Peddler", CardRarity.Common, 2,
                MonsterAttribute.Earth, MonsterType.Human, 1400, 1000,
                Fx("Take It or Leave It", "When this card is Summoned, your opponent must choose: you draw 1 card — or this card gains 700 ATK and Piercing until the end of the turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.OfferDeal).A5Deal("They draw 1 card", "The Peddler gains 700 ATK and Piercing this turn"),
                    Act(EffectActionType.DrawCards, 1).A5A(),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 700, TargetKind.SelfCard).A5B(),
                    Act(EffectActionType.GrantPiercingThisTurn, 1, TargetKind.SelfCard).A5B()),
                Inf("The Hard Sell", "Instead, pay 2 Mana — the deal grows: you draw 2 — or 700 ATK, Piercing and one extra attack.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.OfferDeal).A5Deal("They draw 2 cards", "The Peddler gains 700 ATK, Piercing and an extra attack"),
                    Act(EffectActionType.DrawCards, 2).A5A(),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 700, TargetKind.SelfCard).A5B(),
                    Act(EffectActionType.GrantPiercingThisTurn, 1, TargetKind.SelfCard).A5B(),
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.SelfCard).A5B()));
            peddler.canSelfSpecialSummon = true;
            peddler.selfSummonRequiresOpponentMoreMonsters = true;
            peddler.selfSummonPosition = BattlePosition.Attack;

            var detour = W3Artifact("The Long Detour", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Scenic Route", "If your opponent attacked this turn, pay 2 Mana: return 1 monster they control to its owner's hand.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ReturnTargetToHand, 1, TargetKind.EnemyMonster)).A5NeedsOppAttack());
            detour.passiveFirstEnemyAttackDetourDeal = true;

            W3Spell("Final Offer", CardRarity.Rare, false,
                Fx("Sign or Suffer", "Pay 5 Mana. Target a monster your opponent controls — they choose: they send it to the Graveyard, or you take control of it until the end of the turn.",
                    EffectTrigger.OnActivate, 5, false,
                    Act(EffectActionType.PickTargetOnly, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.OfferDeal).A5Deal("They send the monster to the Graveyard", "You take control of it this turn"),
                    Act(EffectActionType.SendTargetToGraveyard, 1, TargetKind.SameAsPrevious).A5A(),
                    Act(EffectActionType.TakeControlUntilEndOfTurn, 1, TargetKind.SameAsPrevious).A5B()),
                Inf("No Mercy Clause", "Instead, pay 7 Mana — both roads harden: they banish it, or you take control AND it gains 400 ATK this turn.",
                    EffectTrigger.OnActivate, 7, true,
                    Act(EffectActionType.PickTargetOnly, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.OfferDeal).A5Deal("They banish the monster", "You take control of it and it gains 400 ATK this turn"),
                    Act(EffectActionType.BanishTargetMonster, 1, TargetKind.SameAsPrevious).A5A(),
                    Act(EffectActionType.TakeControlUntilEndOfTurn, 1, TargetKind.SameAsPrevious).A5B(),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.SameAsPrevious).A5B()));
        }

        // ================== B. GEGNERZUG & AMBUSH ==================
        private static void W3Ambush()
        {
            var lurker = W3Mon("Cellar Lurker", CardRarity.Uncommon, 2,
                MonsterAttribute.Dark, MonsterType.Demon, 0, 2200,
                Fx("Out of the Dark", "When an opponent's monster declares an attack: you can Special Summon this card from your hand in Defense Position.",
                    EffectTrigger.HandQuick, 0, true,
                    Act(EffectActionType.SpecialSummonSelfFromHand).RtInDefense()).InWindow(QuickWindow.AttackResponse),
                Inf("Swallowed by the Cellar", "Pay 2 Mana when an opponent's monster attacks: the attack is cancelled.",
                    EffectTrigger.Quick, 2, false,
                    Act(EffectActionType.CancelAttackTarget, 1, TargetKind.EnemyMonster)).InWindow(QuickWindow.AttackResponse));
            lurker.passiveTaunt = true;

            W3Artifact("Widow's Ledger", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Every Name Is Written", "Whenever one of your monsters is destroyed during your opponent's turn: the monster that destroyed it permanently loses 400 ATK.",
                    EffectTrigger.OnOwnMonsterDestroyed, 0, false,
                    Act(EffectActionType.DebuffDestroyerAtkPermanent, 400)).Mand().W3OppTurnOnly(),
                Inf("The Ledger Closes", "If one of your monsters was destroyed this turn, pay 3 Mana and send this Artifact to the Graveyard: Special Summon 1 monster from your Graveyard in Defense Position.",
                    EffectTrigger.Ignition, 3, false,
                    Act(EffectActionType.SendSelfToGraveyard, 1, isCost: true),
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf).RtInDefense()).RtAfterOwnLoss());

            W3Artifact("Trapdoor Stage", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Through the Floor", "Once per turn, when your opponent Special Summons a monster: flip it face-down.",
                    EffectTrigger.OnOpponentSummon, 0, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster)),
                Inf("Locked Below", "Instead, pay 2 Mana: it also cannot change position until your next turn.",
                    EffectTrigger.OnOpponentSummon, 2, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockTargetPositionTurns, 1, TargetKind.SameAsPrevious)));

            W3Spell("Second Guess", CardRarity.Rare, true,
                Fx("Not Him — Him!", "Pay 2 Mana when an opponent's monster attacks: change the attack target to another of your monsters.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.RedirectAttackToOwnMonster, 1, TargetKind.AllyMonster)).InWindow(QuickWindow.AttackResponse),
                Inf("Braced for It", "Instead, pay 4 Mana: the new target also gains 500 DEF until the end of the turn.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.RedirectAttackToOwnMonster, 1, TargetKind.AllyMonster),
                    Act(EffectActionType.BuffTargetDefUntilEndOfTurn, 500, TargetKind.SameAsPrevious)).InWindow(QuickWindow.AttackResponse));
        }

        // ================== C. COUNTDOWN FÜR ALLE ==================
        private static void W3Countdown()
        {
            var bell = W3Artifact("Doomsday Bell", CardRarity.Legendary, ArtifactSlot.Field, 0, 0,
                Fx("The Bell Tolls", "When this card's Countdown strikes zero: send every monster on the field to the Graveyard, then send this card there too.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.SendAllMonstersToGraveyard),
                    Act(EffectActionType.SendSelfToGraveyard)).Mand(),
                Inf("Ring Ahead", "Pay 1 Mana: remove 1 Hour Counter from this card.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.TickCountdownSelf, 1)));
            bell.countdownMarkers = 3;

            W3Spell("Sand in the Gears", CardRarity.Uncommon, false,
                Fx("Slow the Hour", "Pay 1 Mana: add 2 Hour Counters to any card on the field.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.AddCountdownTarget, 2, TargetKind.AnyCountdownCard)),
                Inf("Hurry the Hour", "Instead, pay 2 Mana: remove 1 Hour Counter from any card on the field.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.TickCountdownTarget, 1, TargetKind.AnyCountdownCard)));

            var hourglass = W3Mon("Borrowed Hourglass", CardRarity.Uncommon, 2,
                MonsterAttribute.Light, MonsterType.Mecha, 800, 800,
                Fx("When the Sand Runs Out", "When this card's Countdown strikes zero: draw 2 cards and this card permanently gains 800 ATK. It stays on the field.",
                    EffectTrigger.CountdownZero, 0, false,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.BuffTargetAtk, 800, TargetKind.SelfCard)).Mand(),
                Inf("Turn It Over", "Pay 2 Mana: set this card's Countdown back to 2 Hour Counters.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ResetCountdownSelf, 2)));
            hourglass.countdownMarkers = 2;
            hourglass.countdownZeroKeepsCard = true;
            hourglass.canSelfSpecialSummon = true;
            hourglass.selfSummonRequiresOwnCountdown = true;
            hourglass.selfSummonPosition = BattlePosition.Defense;
        }

        // ================== D. GRAB-SPIELE ==================
        private static void W3Grave()
        {
            W3Spell("Gravedigger's Dispute", CardRarity.Rare, false,
                Fx("Whose Grave Is It Anyway", "Pay 2 Mana: swap the top cards of both Graveyards.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SwapGraveTops)),
                Inf("Dig It Right Up", "Instead, pay 4 Mana: then Special Summon the new top monster of your Graveyard in Defense Position.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SwapGraveTops),
                    Act(EffectActionType.SpecialSummonGraveTop).RtInDefense()));

            W3Spell("Seance Circle", CardRarity.Rare, false,
                Fx("Join Hands", "Pay 4 Mana: Special Summon a monster from your opponent's Graveyard to your side of the field. When it leaves the field, banish it.",
                    EffectTrigger.OnActivate, 4, false,
                    Act(EffectActionType.SpecialSummonFromOpponentGraveyard, 1, TargetKind.GraveyardMonsterOpponent)),
                Inf("Restless Spirit", "Instead, pay 6 Mana: this turn, it can attack twice.",
                    EffectTrigger.OnActivate, 6, true,
                    Act(EffectActionType.SpecialSummonFromOpponentGraveyard, 1, TargetKind.GraveyardMonsterOpponent),
                    Act(EffectActionType.GrantAdditionalAttack, 1, TargetKind.SameAsPrevious)));

            var business = W3Mon("Unfinished Business", CardRarity.Uncommon, 3,
                MonsterAttribute.Dark, MonsterType.Myth, 2000, 1500,
                Fx("It Is Not Done", "While this card is in your Graveyard: put it on top of your Graveyard.",
                    EffectTrigger.GraveyardIgnition, 0, true,
                    Act(EffectActionType.MoveSelfToGraveTop)),
                Fx("Set My Affairs in Order", "When this card is Summoned: you can put 1 monster in your Graveyard on top of it.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MoveGraveTargetToTop, 1, TargetKind.GraveyardMonsterSelf, upTo: true)),
                Inf("One Last Errand", "While this card is the top card of your Graveyard, pay 2 Mana: Special Summon it in Defense Position.",
                    EffectTrigger.GraveyardIgnition, 2, false,
                    Act(EffectActionType.SpecialSummonSelfFromGrave).RtInDefense()).RtWhileGraveTop());
            business.canSelfSpecialSummon = true;
            business.selfSummonRequiresGraveTopMonster = true;
            business.selfSummonPosition = BattlePosition.Attack;
        }

        // ================== E. FELD-CHAOS ==================
        private static void W3Chaos()
        {
            W3Spell("Masquerade Ball", CardRarity.Legendary, false,
                Fx("Masks On", "Pay 5 Mana: flip every face-up monster on the field face-down.",
                    EffectTrigger.OnActivate, 5, false,
                    Act(EffectActionType.SetAllMonstersFaceDown)),
                Inf("The Host Goes Bare", "Instead, pay 7 Mana: … except 1 monster you control.",
                    EffectTrigger.OnActivate, 7, true,
                    Act(EffectActionType.SetAllMonstersFaceDown, 1, TargetKind.AllyMonster, upTo: true)));

            W3Spell("Eminent Domain", CardRarity.Uncommon, false,
                Fx("Public Works", "Pay 1 Mana and seal one of your own empty Monster Zones: draw 2 cards.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.SealAnyZones, 1, isCost: true),
                    Act(EffectActionType.DrawCards, 2)),
                Inf("Land Grab", "Instead, pay 2 Mana: seal one of your OPPONENT's empty Monster Zones and draw 1 card.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.SealEnemyZones, 1),
                    Act(EffectActionType.DrawCards, 1)));

            var guest = W3Mon("The Unwelcome Guest", CardRarity.Rare, 1,
                MonsterAttribute.Wind, MonsterType.Demon, 100, 1800,
                Inf("Collecting Rent", "While this card squats on your opponent's field, pay 2 Mana: you gain 2 Mana this turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.GainMana, 2)).A5WhileDelivered());
            guest.canSelfSpecialSummon = true;
            guest.selfSummonToOpponentField = true;
            guest.selfSummonPosition = BattlePosition.Defense;
            guest.passiveServesOriginalOwner = true;
            guest.passiveCannotAttack = true;
            guest.passiveCannotBeTributed = true;
            guest.passiveSpellTaxOnController = true;
        }

        // ================== F. WETTEN & INFORMATION ==================
        private static void W3Wagers()
        {
            var sharp = W3Mon("Card Sharp", CardRarity.Uncommon, 2,
                MonsterAttribute.Dark, MonsterType.Human, 1300, 900,
                Fx("Call the Card", "When this card is Summoned: declare Monster, Spell or Artifact, then reveal the top card of your Deck. If it matches, add it to your hand; otherwise it goes to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.DeclareTypeRevealTop, 1)),
                Inf("Double Down", "Instead, pay 2 Mana: reveal the top 2 cards — matches to your hand, the rest to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.DeclareTypeRevealTop, 2)));
            sharp.canSelfSpecialSummon = true;
            sharp.selfSummonRequiresRevealedThisTurn = true;
            sharp.selfSummonPosition = BattlePosition.Attack;

            W3Spell("Peephole", CardRarity.Uncommon, false,
                Fx("A Look Through the Wall", "Pay 1 Mana: look at your opponent's hand — you can shuffle 1 Spell from it into their Deck.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.HandSpellOpponent, upTo: true)),
                Inf("The Wider Crack", "Instead, pay 2 Mana: you can shuffle 1 card of ANY type instead.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.ShuffleTargetIntoDeck, 1, TargetKind.HandCardOpponent, upTo: true)));

            var policy = W3Artifact("Insurance Policy", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Inf("Personal Coverage", "Pay 2 Mana: choose a monster you control — the next time it would be destroyed this Duel, it is not.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ShieldTargetNextDestruction, 1, TargetKind.AllyMonster)));
            policy.redirectDestructionToSelf = true;
        }

        // ================== G. RESSOURCEN & TEMPO ==================
        private static void W3Tempo()
        {
            var creditor = W3Mon("Silver-Tongued Creditor", CardRarity.Rare, 3,
                MonsterAttribute.Dark, MonsterType.Human, 2200, 800,
                Fx("Easy Terms", "When this card is Summoned: gain 2 Mana this turn. During your next Standby Phase: pay 2 Mana or send this card to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.GainMana, 2),
                    Act(EffectActionType.ManaDebtNextStandby, 2)).Mand(),
                Inf("The Bigger Loan", "Instead, pay 2 Mana: gain 3 Mana — and the debt becomes 3.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.GainMana, 3),
                    Act(EffectActionType.ManaDebtNextStandby, 3)));
            creditor.canSelfSpecialSummon = true;
            creditor.selfSummonRequiresHandAtMost = 2;
            creditor.selfSummonPosition = BattlePosition.Attack;

            W3Spell("Tomorrow's Bread", CardRarity.Uncommon, false,
                Fx("Eat Today", "Pay 1 Mana: draw 2 cards. Skip your next normal draw.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DrawCards, 2),
                    Act(EffectActionType.SkipOwnNextDrawPhase)),
                Inf("Feast Today", "Instead, pay 3 Mana: draw 3 cards. Skip your next normal draw.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.DrawCards, 3),
                    Act(EffectActionType.SkipOwnNextDrawPhase)));

            W3Artifact("Scrap Broker", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Everything Has a Price", "Once per turn, send another Artifact you control to the Graveyard: draw 1 card.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.SendTargetToGraveyard, 1, TargetKind.AllyArtifact, excludeSelf: true, isCost: true),
                    Act(EffectActionType.DrawCards, 1)),
                Inf("Buy It Back", "Pay 2 Mana: add 1 Artifact from your Graveyard to your hand.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ReturnFromGraveyardToHand, 1, TargetKind.GraveyardArtifactSelf)));

            var prodigy = W3Mon("Posthumous Prodigy", CardRarity.Rare, 1,
                MonsterAttribute.Light, MonsterType.Myth, 0, 0,
                Inf("Choose the Idol", "Pay 2 Mana: put any monster in your Graveyard on top of it.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.MoveGraveTargetToTop, 1, TargetKind.GraveyardMonsterSelf)));
            prodigy.passiveStatsFromGraveTop = true;
            prodigy.canSelfSpecialSummon = true;
            prodigy.selfSummonRequiresGraveTopMonster = true;
            prodigy.selfSummonPosition = BattlePosition.Attack;
        }

        // ================== H. POSITIONS-TÄNZER ==================
        private static void W3Dancers()
        {
            var duelist = W3Mon("Pirouette Duelist", CardRarity.Uncommon, 2,
                MonsterAttribute.Wind, MonsterType.Human, 1200, 1200,
                Fx("Lead the Dance", "Once per turn: change the battle position of any monster on the field.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.ToggleTargetPosition, 1, TargetKind.AnyMonster)),
                Inf("Hold the Pose", "Instead, pay 1 Mana: it also cannot change position until your next turn.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.ToggleTargetPosition, 1, TargetKind.AnyMonster),
                    Act(EffectActionType.LockTargetPositionTurns, 1, TargetKind.SameAsPrevious)));
            duelist.canSelfSpecialSummon = true;
            duelist.selfSummonRequiresOpponentDefenseMonster = true;
            duelist.selfSummonPosition = BattlePosition.Attack;

            W3Spell("Turnabout Waltz", CardRarity.Rare, false,
                Fx("Everyone Turns", "Pay 2 Mana: every face-up monster on the field changes its battle position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.ToggleAllPositions)),
                Inf("Half the Ballroom", "Instead, pay 4 Mana: only one side of your choice turns.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.ToggleAllPositions, 1)));

            W3Spell("Stage Fright", CardRarity.Uncommon, true,
                Fx("Frozen in the Lights", "Pay 2 Mana when your opponent Summons a monster: it is turned to Defense Position and cannot change position this turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockTargetPositionTurns, 1, TargetKind.SameAsPrevious)).InWindow(QuickWindow.SummonResponse),
                Inf("Curtain Call", "Pay 2 Mana: flip 1 of your face-down monsters face-up; it gains 400 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterSelf),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 400, TargetKind.SameAsPrevious)));
        }

        // ================== I. EXIL-ÖKONOMIE ==================
        private static void W3Exile()
        {
            var broker = W3Mon("Exile Broker", CardRarity.Uncommon, 2,
                MonsterAttribute.Dark, MonsterType.Human, 1500, 1000,
                Fx("Pay Into the Shadows", "When this card is Summoned: banish the top card of your Deck face-down.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.BanishOwnDeckTop, 1)).Mand(),
                Inf("Call One Home", "Pay 2 Mana: return 1 of your banished cards to your Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf)));
            broker.canSelfSpecialSummon = true;
            broker.selfSummonRequiresBanishedCards = 1;
            broker.selfSummonPosition = BattlePosition.Attack;
            broker.passiveAtkPerCount = 200;
            broker.passiveAtkPerCountKind = EffectCountKind.OwnBanishedCards;

            W3Spell("Letters from Exile", CardRarity.Uncommon, false,
                Fx("Words That Strengthen", "Pay 1 Mana and banish 2 cards from your Graveyard: 1 of your monsters gains 600 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf, targetCount: 2, isCost: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 600, TargetKind.AllyMonster)),
                Inf("Letters Sent Home", "Instead, pay 2 Mana: the banished cards return to your Graveyard during your next Standby Phase.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.BanishGraveTargetsReturnLater, 1, TargetKind.GraveyardCardSelf, targetCount: 2, isCost: true),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 600, TargetKind.AllyMonster)));

            var unforgotten = W3Mon("The Unforgotten", CardRarity.Rare, 3,
                MonsterAttribute.Light, MonsterType.Myth, 2100, 1400,
                Fx("Into the Long Dark", "Banish this card from your hand: banish also the top card of your Deck face-down.",
                    EffectTrigger.HandIgnition, 0, true,
                    Act(EffectActionType.BanishSelf, 1, isCost: true),
                    Act(EffectActionType.BanishOwnDeckTop, 1)),
                Fx("The Long Road Back", "While you have 3+ banished cards, pay 2 Mana: Special Summon this card from exile.",
                    EffectTrigger.BanishedIgnition, 2, true,
                    Act(EffectActionType.SpecialSummonSelfFromBanished)).W3NeedsBanished(3),
                Inf("Take One With Me", "Pay 2 Mana: banish the top card of your opponent's Graveyard.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BanishOpponentGraveTop, 1)));
            unforgotten.selfSummonPosition = BattlePosition.Attack;
        }

        // ================== J. TOKEN & STROH ==================
        private static void W3Straw()
        {
            W3Spell("Straw Army", CardRarity.Uncommon, false,
                Fx("Raise the Field", "Pay 2 Mana: Special Summon 2 Scarecrow Tokens (Level 1, 0/500) in Defense Position.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SummonScarecrowTokens, 2)),
                Inf("Hold the Line", "Instead, pay 4 Mana: 3 Tokens — and until your next turn, your opponent's monsters must attack Scarecrows if able.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SummonScarecrowTokens, 3, targetCount: 2)));

            W3Artifact("Puppet Parade", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("The Parade Goes On", "Once per turn, when one of your monsters is destroyed: Special Summon 1 Scarecrow Token in Defense Position.",
                    EffectTrigger.OnOwnMonsterDestroyed, 0, true,
                    Act(EffectActionType.SummonScarecrowTokens, 1)).Mand(),
                Inf("A Real Face Tonight", "Pay 2 Mana: 1 of your monsters copies the ATK and DEF of any monster on the field until the end of the turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.AllyMonsterCopiesTargetStats, 0, TargetKind.AnyMonster)));

            var strawman = W3Mon("Man of Straw", CardRarity.Common, 1,
                MonsterAttribute.Earth, MonsterType.Beast, 800, 600,
                Fx("Stuffing Spills", "When this card is destroyed: Special Summon 1 Scarecrow Token in Defense Position.",
                    EffectTrigger.OnDestroyedSelf, 0, false,
                    Act(EffectActionType.SummonScarecrowTokens, 1)).Mand(),
                Inf("Made to Burn", "Pay 1 Mana and send this card to the Graveyard: Special Summon 1 Scarecrow Token in Defense Position.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.SendSelfToGraveyard, 1, isCost: true),
                    Act(EffectActionType.SummonScarecrowTokens, 1)));
            strawman.canSelfSpecialSummon = true;
            strawman.selfSummonRequiresNoOwnMonsters = true;
            strawman.selfSummonPosition = BattlePosition.Attack;
            strawman.tributeWorth = 2;
        }

        // ================== K. DECK-STAPLER ==================
        private static void W3Stackers()
        {
            W3Artifact("Cartomancer's Eye", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Fx("Read Your Fate", "Once per turn: look at the top card of your Deck — you may put it on the bottom.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.RevealTopMayBottom, 1)),
                Inf("Read Theirs", "Instead, pay 1 Mana: look at the top card of your OPPONENT's Deck — you may put it on the bottom.",
                    EffectTrigger.Ignition, 1, true,
                    Act(EffectActionType.RevealOpponentTopDeckMayBottom, 1)));

            W3Spell("Stacked Deck", CardRarity.Uncommon, false,
                Fx("House Rules", "Pay 1 Mana: look at the top 3 cards of your Deck and put them back in any order.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.LookReorderTopDeck, 3)),
                Inf("One Slides Under", "Instead, pay 3 Mana: then send the new top card of your Deck to the Graveyard.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.LookReorderTopDeck, 3),
                    Act(EffectActionType.MillSelf, 1)));

            var dealer = W3Mon("House Dealer", CardRarity.Uncommon, 2,
                MonsterAttribute.Dark, MonsterType.Human, 1400, 1100,
                Fx("The House Wager", "When this card is Summoned: reveal the top card of your Deck. If it is a Monster, this card permanently gains 600 ATK.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.RevealTopBuffSelfIfMonster, 600)),
                Inf("Fresh Shoe", "Pay 2 Mana: both players put their top Deck card on the bottom.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.BothTopToBottom)));
            dealer.canSelfSpecialSummon = true;
            dealer.selfSummonRequiresRevealedThisTurn = true;
            dealer.selfSummonPosition = BattlePosition.Attack;
        }

        // ================== L. MILL ==================
        private static void W3Mill()
        {
            W3Spell("Quarry Collapse", CardRarity.Uncommon, false,
                Fx("Bring It Down", "Pay 2 Mana: mill 3 cards from your Deck.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.MillSelf, 3)),
                Inf("Salvage the Rubble", "Instead, pay 4 Mana: then put 1 milled monster on top of your Graveyard.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.MillSelf, 3),
                    Act(EffectActionType.MoveGraveTargetToTop, 1, TargetKind.GraveyardMonsterSelf, upTo: true)));

            var baron = W3Mon("Baron of the Undertow", CardRarity.Legendary, 3,
                MonsterAttribute.Water, MonsterType.Demon, 2300, 1200,
                Fx("Dragged Under", "When this card is Summoned: your opponent mills 2 cards.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillOpponent, 2)),
                Inf("The Deep Current", "Instead, pay 2 Mana: your opponent mills 4 cards.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.MillOpponent, 4)));
            baron.canSelfSpecialSummon = true;
            baron.selfSummonRequiresGraveNamedCount = 8;   // leerer Filter = alle Karten
            baron.selfSummonPosition = BattlePosition.Attack;
            baron.passiveOpponentMillsBanished = true;

            var mudlark = W3Mon("Mudlark Scavenger", CardRarity.Uncommon, 2,
                MonsterAttribute.Water, MonsterType.Animal, 1600, 900,
                Fx("Wade In", "When this card is Summoned: mill 2 cards from your Deck.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.MillSelf, 2)).Mand(),
                Inf("Sort the Tideline", "Pay 2 Mana: put 1 monster in your Graveyard on top of it.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.MoveGraveTargetToTop, 1, TargetKind.GraveyardMonsterSelf)));
            mudlark.canSelfSpecialSummon = true;
            mudlark.selfSummonRequiresMilled = true;
            mudlark.selfSummonPosition = BattlePosition.Attack;
            mudlark.passiveAtkPerCount = 50;
            mudlark.passiveAtkPerCountKind = EffectCountKind.OwnGraveyardCards;
        }

        // ================== M. KOPIEN & MASKEN ==================
        private static void W3Masks()
        {
            var usher = W3Mon("Mirror Usher", CardRarity.Rare, 3,
                MonsterAttribute.Light, MonsterType.Myth, 1900, 1900,
                Fx("Seat Them Personally", "When this card is Summoned: it copies the ATK and DEF of a monster your opponent controls until the end of the turn.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 0, TargetKind.EnemyMonster)),
                Inf("A Longer Performance", "Instead, pay 2 Mana: the copy lasts until the start of your next turn.",
                    EffectTrigger.OnSummonSelf, 2, true,
                    Act(EffectActionType.CopyTargetStatsThisTurn, 1, TargetKind.EnemyMonster)));
            usher.canSelfSpecialSummon = true;
            usher.selfSummonRequiresOpponentMonsters = 2;
            usher.selfSummonPosition = BattlePosition.Attack;

            W3Spell("Borrowed Face", CardRarity.Rare, true,
                Fx("Mirror Match", "Pay 2 Mana when an opponent's monster attacks: 1 of your monsters copies the attacker's ATK and DEF until the end of the turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.AllyMonsterCopiesTargetStats, 0, TargetKind.EnemyMonster)).InWindow(QuickWindow.AttackResponse),
                Inf("The Better Likeness", "Instead, pay 3 Mana: it also gains 100 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 3, true,
                    Act(EffectActionType.AllyMonsterCopiesTargetStats, 0, TargetKind.EnemyMonster),
                    Act(EffectActionType.BuffTargetAtkUntilEndOfTurn, 100, TargetKind.AllyMonster)).InWindow(QuickWindow.AttackResponse));

            W3Artifact("Prompter's Box", CardRarity.Rare, ArtifactSlot.Field, 0, 0,
                Fx("Back Behind the Curtain", "Once per turn: set 1 face-up monster you control face-down.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster)),
                Inf("Cue the Entrance", "Pay 2 Mana: flip 1 of your face-down monsters face-up — its Flip effects trigger.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.FlipTargetFaceUp, 1, TargetKind.FaceDownMonsterSelf)));
        }

        // ================== N. BATTLE-TRICKS ==================
        private static void W3Battle()
        {
            W3Spell("Lowball Feint", CardRarity.Uncommon, true,
                Fx("Undersell It", "Pay 1 Mana when an opponent's monster attacks: it loses 800 ATK until the end of the turn.",
                    EffectTrigger.OnActivate, 1, false,
                    Act(EffectActionType.DebuffTargetAtkEot, 800, TargetKind.EnemyMonster)).InWindow(QuickWindow.AttackResponse),
                Inf("Left Reeling", "Instead, pay 2 Mana: if it survives the battle, it switches to Defense Position at the end of the turn.",
                    EffectTrigger.OnActivate, 2, true,
                    Act(EffectActionType.DebuffTargetAtkEot, 800, TargetKind.EnemyMonster),
                    Act(EffectActionType.SwitchTargetToDefenseAtEot, 1, TargetKind.SameAsPrevious)).InWindow(QuickWindow.AttackResponse));

            var doctrine = W3Artifact("Shield Wall Doctrine", CardRarity.Uncommon, ArtifactSlot.Field, 0, 0,
                Inf("Hedgehog Formation", "Pay 2 Mana: all your monsters switch to Defense Position and gain 300 DEF until your next turn.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.SwitchAllOwnToDefenseBuffDef, 300)));
            doctrine.passiveDefWhileDefending = 300;

            W3Spell("Overextension", CardRarity.Uncommon, false,
                Fx("Caught Leaning", "If your opponent attacked this turn, pay 2 Mana: 1 monster they control switches to Defense Position and cannot change position until the end of their next turn.",
                    EffectTrigger.OnActivate, 2, false,
                    Act(EffectActionType.SwitchTargetToDefense, 1, TargetKind.EnemyMonster),
                    Act(EffectActionType.LockTargetPositionTurns, 2, TargetKind.SameAsPrevious)).A5NeedsOppAttack(),
                Inf("The Whole Line Buckles", "Instead, pay 4 Mana: ALL your opponent's monsters switch to Defense Position.",
                    EffectTrigger.OnActivate, 4, true,
                    Act(EffectActionType.SwitchAllToDefense, 1)).A5NeedsOppAttack());
        }

        // ================== O. TEMPO & SPERRSTUNDE ==================
        private static void W3TempoLate()
        {
            // (in W3Tempo integriert — Methode bleibt für die Übersicht ungenutzt)
        }

        // ================== P. RELIQUARIES ==================
        private static void W3Relics()
        {
            W3Spell("Prepaid Ritual", CardRarity.Uncommon, false,
                Fx("Paid in Advance", "Your next Spell this turn costs 2 less Mana.",
                    EffectTrigger.OnActivate, 0, false,
                    Act(EffectActionType.DiscountNextSpellThisTurn, 2)),
                Inf("Prepaid Tribute", "Instead, pay 1 Mana: your next Normal Summon this turn needs 1 fewer Tribute.",
                    EffectTrigger.OnActivate, 1, true,
                    Act(EffectActionType.DiscountNextNormalSummon, 1)));

            var closing = W3Artifact("Closing Time", CardRarity.Legendary, ArtifactSlot.Field, 0, 0,
                Inf("The Back Door", "Pay 2 Mana: the summon curfew does not apply to you this turn — the card stays on the field.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.LiftOwnSummonCapThisTurn)));
            closing.passiveSummonCapBoth = 1;

            var casket = W3Rel("Reliquary: The Open Casket", CardRarity.Rare, 3,
                MonsterAttribute.Dark, MonsterType.Myth, 2100, 1600,
                "You have 8+ cards in your Graveyard — pay 2 Mana.", 2,
                Fx("Arrange the Viewing", "Once per turn: move any card in your Graveyard to its top.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.MoveGraveTargetToTop, 1, TargetKind.GraveyardCardSelf)),
                Inf("One Rises From It", "Pay 3 Mana: Special Summon 1 monster from your Graveyard in Defense Position.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.SpecialSummonFromGraveyard, 1, TargetKind.GraveyardMonsterSelf).RtInDefense()));
            casket.reqGraveyardAtLeast = 8;
            casket.auraAtkBonus = 300;

            var eleventh = W3Rel("Reliquary: The Eleventh Hour", CardRarity.Legendary, 3,
                MonsterAttribute.Dark, MonsterType.Mecha, 1900, 2200,
                "You control 2+ cards with Hour Counters — pay 3 Mana.", 3,
                Inf("Strike the Hour", "Pay 2 Mana: one of your Countdowns strikes immediately.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.TickCountdownTarget, 99, TargetKind.AllyCountdownCard)));
            eleventh.reqOwnCountdownCards = 2;
            eleventh.passiveCountdownsTickTwice = true;

            var echoes = W3Rel("Reliquary: The Hall of Echoes", CardRarity.Rare, 3,
                MonsterAttribute.Light, MonsterType.Myth, 2000, 1800,
                "You have 3+ banished cards — pay 2 Mana.", 2,
                Fx("Feed the Hall", "Once per turn: banish 1 card from your Graveyard.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.BanishTarget, 1, TargetKind.GraveyardCardSelf)),
                Inf("An Echo Returns", "Pay 3 Mana: return 1 of your banished cards to your Graveyard.",
                    EffectTrigger.Ignition, 3, true,
                    Act(EffectActionType.ReturnBanishedToGraveyard, 1, TargetKind.BanishedCardSelf)));
            echoes.reqBanishedAtLeast = 3;
            echoes.auraAtkBonus = 400;

            var bow = W3Rel("Reliquary: The Last Bow", CardRarity.Rare, 3,
                MonsterAttribute.Wind, MonsterType.Human, 2200, 1500,
                "You control 2+ face-down monsters — pay 2 Mana.", 2,
                Fx("Exit Stage Left", "Once per turn: set 1 face-up monster you control face-down.",
                    EffectTrigger.Ignition, 0, true,
                    Act(EffectActionType.SetTargetFaceDownDefense, 1, TargetKind.AllyMonster)),
                Inf("The Final Reveal", "Pay 2 Mana: flip ALL your face-down monsters face-up.",
                    EffectTrigger.Ignition, 2, true,
                    Act(EffectActionType.FlipAllOwnFaceUp)));
            bow.reqOwnFaceDownMonsters = 2;
            bow.passiveProtectFaceDownFromEffectDestroy = true;
        }

        // ---- Feinwürze für die neuen Engine-Felder ----

        /// <summary>Trigger/Effekt nur im GEGNERISCHEN Zug (Widow's Ledger).</summary>
        private static EffectDefinition W3OppTurnOnly(this EffectDefinition effect)
        { effect.onlyDuringOpponentTurn = true; return effect; }

        /// <summary>Bedingung: mindestens N Karten in der eigenen Verbannung.</summary>
        private static EffectDefinition W3NeedsBanished(this EffectDefinition effect, int count)
        { effect.minOwnBanishedCards = count; return effect; }
    }
}
