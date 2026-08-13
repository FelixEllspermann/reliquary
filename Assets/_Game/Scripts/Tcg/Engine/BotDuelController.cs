using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rouge.Tcg
{
    /// <summary>Einfacher Bot: spielt regelkonform und grob sinnvoll (v1-Heuristik).</summary>
    public class BotDuelController : DuelController
    {
        private int actionsThisTurn;
        private int lastSeenTurn = -1;

        private IEnumerator ThinkDelay()
        {
            if (Duel.BotActionDelay > 0f) yield return DuelWait.For(Duel.BotActionDelay);
        }

        public override IEnumerator Decide(MainActionRequest request)
        {
            yield return ThinkDelay();
            if (Duel.TurnNumber != lastSeenTurn)
            {
                lastSeenTurn = Duel.TurnNumber;
                actionsThisTurn = 0;
            }
            actionsThisTurn++;
            request.Chosen = actionsThisTurn > 30 ? FindExit(request) : PickMainAction(request);
            request.Answered = true;
        }

        private int FindExit(MainActionRequest request)
        {
            bool canAttack = Player.Monsters().Any(m =>
                m.Position == BattlePosition.Attack && (!m.HasAttackedThisTurn || m.BonusAttacks > 0));
            int battleIndex = request.Options.FindIndex(o => o.Kind == MainActionKind.ToBattlePhase);
            if (battleIndex >= 0 && canAttack) return battleIndex;
            return request.Options.FindIndex(o => o.Kind == MainActionKind.EndTurn);
        }

        private int PickMainAction(MainActionRequest request)
        {
            // 0) Reliquary aus dem Extra Deck (Bosse zuerst — Bedingungen sind bereits geprüft)
            int reliquaryIndex = request.Options.FindIndex(o => o.Kind == MainActionKind.SummonReliquary);
            if (reliquaryIndex >= 0) return reliquaryIndex;

            // 0b) Eigenbedingte Spezialbeschwörung (kostet keinen Normal Summon)
            int selfSummonIndex = request.Options.FindIndex(o => o.Kind == MainActionKind.SpecialSummonSelf);
            if (selfSummonIndex >= 0) return selfSummonIndex;

            // 1) Bestes Monster beschwören (Tribute nur, wenn es sich lohnt)
            int bestSummon = -1, bestSummonAtk = -1;
            for (int i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                if (option.Kind != MainActionKind.SummonMonster) continue;
                var data = option.Card.MonsterData;
                int tributes = Duel.Rules.TributesForLevel(data.level);
                if (tributes > 0)
                {
                    int sacrificedAtk = Player.Monsters().OrderBy(m => m.CurrentAtk).Take(tributes).Sum(m => m.CurrentAtk);
                    if (data.atk <= sacrificedAtk + 200) continue;
                }
                if (data.atk > bestSummonAtk) { bestSummonAtk = data.atk; bestSummon = i; }
            }
            if (bestSummon >= 0) return bestSummon;

            // 1b) Eigene verdeckte Monster aufdecken (löst FLIP-Effekte aus, Feldpräsenz)
            int flipIndex = request.Options.FindIndex(o =>
                o.Kind == MainActionKind.ChangePosition && o.Card != null && o.Card.FaceDown);
            if (flipIndex >= 0) return flipIndex;

            // 2) Artefakt ausspielen
            int artifactIndex = request.Options.FindIndex(o => o.Kind == MainActionKind.PlayArtifact);
            if (artifactIndex >= 0) return artifactIndex;

            // 3) Nützlichste (teuerste bezahlbare) Aktivierung
            int bestActivation = -1, bestCost = -1;
            for (int i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                bool isActivation = option.Kind == MainActionKind.ActivateSpellFromHand
                                    || option.Kind == MainActionKind.ActivateSetSpell
                                    || option.Kind == MainActionKind.ActivateFieldEffect;
                if (!isActivation || option.EffectIndex < 0) continue;
                var effect = option.Card.Definition.effects[option.EffectIndex];
                if (!IsEffectUseful(effect)) continue;
                if (effect.manaCost > bestCost) { bestCost = effect.manaCost; bestActivation = i; }
            }
            if (bestActivation >= 0) return bestActivation;

            // 4) Quick-Zauber verdeckt setzen (für Reaktionen im Gegnerzug)
            for (int i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                if (option.Kind == MainActionKind.SetSpell && option.Card.SpellData != null
                    && option.Card.SpellData.speed == SpellSpeed.Quick) return i;
            }

            return FindExit(request);
        }

        /// <summary>Feldzustand-Check pro Baustein: kein Mana für Effekte ohne sinnvolle Ziele verbrennen.</summary>
        private bool IsEffectUseful(EffectDefinition effect)
        {
            var foe = Player.Opponent;
            foreach (var action in effect.actions)
            {
                switch (action.type)
                {
                    case EffectActionType.HealSelf:
                        if (Player.LifePoints <= StartLifePoints() - action.amount) return true;
                        break;
                    case EffectActionType.GainMana:
                    case EffectActionType.DamageOpponent:
                    case EffectActionType.DamageBothPlayers:
                        return true;
                    case EffectActionType.DrawCards:
                        if (Player.DeckPile.Count > action.amount) return true;
                        break;
                    case EffectActionType.DestroyTargetMonster:
                    case EffectActionType.BanishTargetMonster:
                    case EffectActionType.DebuffTargetAtk:
                    case EffectActionType.DebuffTargetDef:
                    case EffectActionType.ReturnTargetToHand:
                    case EffectActionType.SetTargetFaceDownDefense:
                    case EffectActionType.PurgeTargetBuffs:
                        if (foe.MonsterCount() > 0) return true;
                        break;
                    case EffectActionType.DestroyAllMonstersExceptType:
                        if (foe.MonsterCount() >= 2) return true;
                        break;
                    case EffectActionType.NegateTargetCard:
                        if (foe.FieldCards().Any(c => c.MonsterData != null || c.ArtifactData != null)) return true;
                        break;
                    case EffectActionType.DiscardOpponentRandom:
                        if (foe.Hand.Count > 0) return true;
                        break;
                    case EffectActionType.DrainOpponentMana:
                        if (foe.Mana > 0) return true;
                        break;
                    // Der Übertrag lohnt sich immer: er trifft das nächste
                    // Auffüllen, nicht den aktuellen Vorrat — der Zustand jetzt
                    // sagt darüber nichts aus.
                    case EffectActionType.DrainOpponentManaNextTurn:
                    case EffectActionType.GainManaNextTurn:
                        return true;
                    case EffectActionType.BuffTargetAtk:
                    case EffectActionType.BuffTargetAtkUntilEndOfTurn:
                    case EffectActionType.BuffTargetDef:
                    case EffectActionType.BuffTargetDefUntilEndOfTurn:
                    case EffectActionType.GrantAdditionalAttack:
                    case EffectActionType.ProtectSelfThisTurn:
                        if (Player.MonsterCount() > 0) return true;
                        break;
                    case EffectActionType.SpecialSummonFromGraveyard:
                    case EffectActionType.SpecialSummonTargetFromGraveOrBanish:
                        if (Player.Graveyard.Any(c => c.MonsterData != null) && Player.FreeMonsterZones() > 0) return true;
                        break;
                    case EffectActionType.ReturnFromGraveyardToHand:
                        if (Player.Graveyard.Count > 0) return true;
                        break;
                    case EffectActionType.SpecialSummonTargetFromHand:
                    case EffectActionType.SpecialSummonTargetFromHandOrGrave:
                        if (Player.Hand.Any(c => c.MonsterData != null) && Player.FreeMonsterZones() > 0) return true;
                        break;
                    case EffectActionType.SpecialSummonTargetFromBanished:
                        if (Player.Banished.Any(c => c.MonsterData != null) && Player.FreeMonsterZones() > 0) return true;
                        break;
                    case EffectActionType.FlipTargetFaceUp:
                        if (Player.Monsters().Any(m => m.FaceDown)) return true;
                        break;
                    case EffectActionType.EquipTargetArtifactToSelf:
                        if (Player.Hand.Any(c => c.ArtifactData != null) || Player.Graveyard.Any(c => c.ArtifactData != null)) return true;
                        break;
                    default:
                        return true; // Suchen/Setzen aus dem Deck u.ä. sind praktisch immer sinnvoll
                }
            }
            return false;
        }

        private int StartLifePoints()
        {
            var playerCard = Player.PlayerCard?.Definition as PlayerCardData;
            return playerCard != null ? playerCard.startLifePoints : 8000;
        }

        public override IEnumerator Decide(BattleActionRequest request)
        {
            yield return ThinkDelay();
            int best = -1, bestScore = 0;
            for (int i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                if (option.EndBattle) continue;
                int score = ScoreBattleOption(option);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            request.Chosen = best >= 0 ? best : request.Options.FindIndex(o => o.EndBattle);
            request.Answered = true;
        }

        private int ScoreBattleOption(BattleOption option)
        {
            int attack = option.Attacker.CurrentAtk;
            if (option.Direct)
            {
                // Lethal geht immer vor
                if (attack >= Player.Opponent.LifePoints) return 100000;
                return attack + 1000;
            }

            var target = option.Target;
            if (target.FaceDown)
                return attack >= 1500 ? 500 : 200; // verdeckt: nur mit ordentlich ATK riskieren

            if (target.Position == BattlePosition.Attack)
            {
                if (attack > target.CurrentAtk)
                    return (attack - target.CurrentAtk) + 800 + target.CurrentAtk / 10; // große Bedrohungen zuerst
                if (attack == target.CurrentAtk) return target.CurrentAtk >= 2000 ? 100 : 0;
                return 0;
            }
            return attack > target.CurrentDef ? 400 + target.CurrentDef / 10 : 0;
        }

        /// <summary>Novice-Schwierigkeit: der Bot reagiert nie (keine Quick-Effekte, keine Fallen).</summary>
        public bool NoviceMode;

        public override IEnumerator Decide(YesNoRequest request)
        {
            yield return ThinkDelay();
            if (NoviceMode)
            {
                request.Result = false;
                request.Answered = true;
                yield break;
            }
            // Phasen-Priority-Fenster: Quick-Effekte lieber für echte Reaktionen aufheben
            request.Result = !request.IsPhaseWindow;
            request.Answered = true;
        }

        public override IEnumerator Decide(OptionRequest request)
        {
            yield return ThinkDelay();

            // Master-Duel-Reaktionsliste: gleiche Politik wie die alten Ja/Nein-
            // Fragen — echte Reaktionen nimmt der Bot, Phasenfenster passt er.
            if (request.IsResponseList)
            {
                request.Result = request.IsPhaseWindow || request.Options.Count == 0 ? -1 : 0;
                request.Answered = true;
                yield break;
            }
            // Namenssuche (The Forbidden Name): deterministisch den ersten Namen
            if (request.Searchable)
            {
                request.Result = request.Options.Count > 0 ? 0 : -1;
                request.Answered = true;
                yield break;
            }

            int chosen = request.Options.Count > 0 ? 0 : -1;

            // Beschwörungs-Position: defensive Monster (DEF > ATK) verdeckt in Verteidigung legen
            int defenseIndex = request.Options.FindIndex(o => o.Contains("Defense"));
            if (defenseIndex >= 0 && request.Card != null && request.Card.MonsterData != null
                && request.Card.MonsterData.def > request.Card.MonsterData.atk)
                chosen = defenseIndex;

            request.Result = chosen;
            request.Answered = true;
        }

        public override IEnumerator Decide(ZoneSelectRequest request)
        {
            yield return ThinkDelay();
            request.Result = request.FreeIndices.Count > 0 ? request.FreeIndices[0] : -1;
            request.Answered = true;
        }

        public override IEnumerator Decide(TargetRequest request)
        {
            yield return ThinkDelay();
            request.Result.Clear();

            // Kosten-Auswahlen (Tribute, Friedhof-Banish, Opfer) geben das Schwächste her —
            // alles andere (Buffs, Revives, Suchen, Removal) nimmt das Stärkste.
            string title = request.Title ?? "";
            bool sacrifice = title.Contains("tribute")
                || title.Contains("you control for")
                || (title.Contains("Banish") && title.Contains("from your Graveyard for"));

            IEnumerable<CardInstance> ordered;
            if (sacrifice || request.Kind == TargetKind.None)
                ordered = request.Candidates.OrderBy(c => c.CurrentAtk);
            else
                ordered = request.Candidates.OrderByDescending(c => c.CurrentAtk);

            request.Result.AddRange(ordered.Take(request.Count));
            request.Answered = true;
        }
    }
}
