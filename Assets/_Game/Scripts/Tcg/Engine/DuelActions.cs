using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rouge.Tcg
{
    public partial class DuelManager
    {
        private class TargetCollection
        {
            public readonly Dictionary<int, List<CardInstance>> PerAction = new Dictionary<int, List<CardInstance>>();
            public bool Cancelled;

            // Zustand der Ziele zum Zeitpunkt des Targetings (YuGiOh-Regel: bei der Auflösung
            // muss ein Ziel noch in derselben Zone, beim selben Besitzer und im selben
            // Face-up/Face-down-Zustand sein — sonst verpufft der Effekt für dieses Ziel).
            private readonly Dictionary<CardInstance, (ZoneType zone, PlayerState owner, bool faceDown)> snapshots =
                new Dictionary<CardInstance, (ZoneType, PlayerState, bool)>();

            public void RecordSnapshot(CardInstance card)
            {
                if (card != null && !snapshots.ContainsKey(card))
                    snapshots[card] = (card.Zone, card.Owner, card.FaceDown);
            }

            public bool StillValid(CardInstance card)
            {
                if (card == null) return false;
                if (!snapshots.TryGetValue(card, out var s)) return true; // nie getargetet (z.B. SelfCard)
                return card.Zone == s.zone && card.Owner == s.owner && card.FaceDown == s.faceDown;
            }
        }

        // ================== MAIN PHASE: legale Aktionen ==================

        private MainActionRequest BuildMainActions(PlayerState player)
        {
            var request = new MainActionRequest { Title = $"Main Phase — {player.Name}" };

            foreach (var card in player.Hand)
            {
                if (card.MonsterData != null)
                {
                    int tributes = rules.TributesForLevel(card.MonsterData.level);
                    bool canSummon = player.NormalSummonsUsed < rules.normalSummonsPerTurn + player.ExtraNormalSummons
                                     && player.MonsterCount() >= tributes
                                     && (tributes > 0 || player.FreeMonsterZones() > 0);
                    if (canSummon)
                    {
                        string tributeInfo = tributes > 0 ? $" ({tributes} tribute{(tributes > 1 ? "s" : "")})" : "";
                        request.Options.Add(new MainActionOption
                        {
                            Kind = MainActionKind.SummonMonster,
                            Card = card,
                            Label = $"Summon {card.Name}{tributeInfo}"
                        });
                    }

                    // Selbst-Spezialbeschwörung (z.B. "wenn du/der Gegner ein bestimmtes Monster kontrolliert)
                    var monsterData = card.MonsterData;
                    if (monsterData.canSelfSpecialSummon && player.FreeMonsterZones() > 0)
                    {
                        var pool = (monsterData.selfSummonChecksOpponentField ? player.Opponent.Monsters() : player.Monsters())
                            .Where(m => !m.FaceDown).ToList();
                        bool nameOk = string.IsNullOrEmpty(monsterData.selfSummonRequiresNameOnField)
                            || pool.Count(m => m.Name.Contains(monsterData.selfSummonRequiresNameOnField))
                               >= Math.Max(1, monsterData.selfSummonRequiredNameCount);
                        bool attributeOk = !monsterData.selfSummonRequiresAttribute
                            || pool.Any(m => m.MonsterData != null && m.MonsterData.attribute == monsterData.selfSummonRequiredAttribute);
                        bool faceDownOk = !monsterData.selfSummonRequiresFaceDownOnField || AnyFaceDownOnField();
                        bool artifactOk = !monsterData.selfSummonRequiresArtifact
                            || player.ArtifactZones.Any(a => a != null);
                        bool foeCountOk = monsterData.selfSummonRequiresOpponentMonsters <= 0
                            || player.Opponent.MonsterCount() >= monsterData.selfSummonRequiresOpponentMonsters;
                        if (nameOk && attributeOk && faceDownOk && artifactOk && foeCountOk)
                            request.Options.Add(new MainActionOption
                            {
                                Kind = MainActionKind.SpecialSummonSelf,
                                Card = card,
                                Label = $"Special Summon {card.Name}"
                            });
                    }

                    // Hand-Effekte (z.B. "schicke diese Karte aus der Hand in den Friedhof: ...")
                    foreach (int index in ActivatableEffects(card, player, EffectTrigger.HandIgnition))
                    {
                        request.Options.Add(new MainActionOption
                        {
                            Kind = MainActionKind.ActivateFieldEffect,
                            Card = card,
                            EffectIndex = index,
                            Label = $"{card.Name} (hand): {EffectChoiceLabel(card, index)}"
                        });
                    }
                }
                else if (card.SpellData != null)
                {
                    foreach (int index in ActivatableEffects(card, player, EffectTrigger.OnActivate))
                    {
                        request.Options.Add(new MainActionOption
                        {
                            Kind = MainActionKind.ActivateSpellFromHand,
                            Card = card,
                            EffectIndex = index,
                            Label = $"Activate {card.Name} — {EffectChoiceLabel(card, index)}"
                        });
                    }
                    if (player.FirstFreeZoneIndex(player.SpellZones) >= 0)
                    {
                        request.Options.Add(new MainActionOption
                        {
                            Kind = MainActionKind.SetSpell,
                            Card = card,
                            Label = $"Set {card.Name} (face-down)"
                        });
                    }
                }
                else if (card.ArtifactData != null)
                {
                    bool zoneFree = player.FirstFreeZoneIndex(player.ArtifactZones) >= 0;
                    bool targetOk = card.ArtifactData.slot != ArtifactSlot.Monster || player.MonsterCount() > 0;
                    if (zoneFree && targetOk)
                    {
                        var activatable = ActivatableEffects(card, player, EffectTrigger.OnActivate);
                        if (activatable.Count == 0)
                        {
                            request.Options.Add(new MainActionOption
                            {
                                Kind = MainActionKind.PlayArtifact,
                                Card = card,
                                EffectIndex = -1,
                                Label = $"Play {card.Name}"
                            });
                        }
                        else
                        {
                            foreach (int index in activatable)
                            {
                                request.Options.Add(new MainActionOption
                                {
                                    Kind = MainActionKind.PlayArtifact,
                                    Card = card,
                                    EffectIndex = index,
                                    Label = $"Play {card.Name} — {EffectChoiceLabel(card, index)}"
                                });
                            }
                        }
                    }
                }
            }

            // Reliquary-Beschwörungen aus dem Extra Deck (eine Option pro Karten-Definition)
            if (player.FreeMonsterZones() > 0)
            {
                var offered = new HashSet<CardDefinition>();
                foreach (var reliquary in player.ExtraDeckPile)
                {
                    var data = reliquary.Definition as ReliquaryCardData;
                    if (data == null || offered.Contains(data)) continue;
                    if (!ReliquaryRequirementsMet(player, data)) continue;
                    offered.Add(data);
                    request.Options.Add(new MainActionOption
                    {
                        Kind = MainActionKind.SummonReliquary,
                        Card = reliquary,
                        Label = $"Reliquary Summon {reliquary.Name} ({data.summonManaCost} Mana)"
                    });
                }
            }

            foreach (var spell in player.SpellsOnField())
            {
                if (spell.SetThisTurn) continue;
                foreach (int index in ActivatableEffects(spell, player, EffectTrigger.OnActivate))
                {
                    request.Options.Add(new MainActionOption
                    {
                        Kind = MainActionKind.ActivateSetSpell,
                        Card = spell,
                        EffectIndex = index,
                        Label = $"Activate set {spell.Name} — {EffectChoiceLabel(spell, index)}"
                    });
                }
            }

            foreach (var card in player.FieldCards())
            {
                if (card.SpellData != null) continue;
                if (card.FaceDown) continue; // verdeckte Monster haben keine aktiven Effekte
                foreach (int index in ActivatableEffects(card, player, EffectTrigger.Ignition))
                {
                    request.Options.Add(new MainActionOption
                    {
                        Kind = MainActionKind.ActivateFieldEffect,
                        Card = card,
                        EffectIndex = index,
                        Label = $"{card.Name}: {EffectChoiceLabel(card, index)}"
                    });
                }
            }

            // Friedhof-Effekte (z.B. "verbannen: ...")
            foreach (var card in player.Graveyard.ToArray())
            {
                foreach (int index in ActivatableEffects(card, player, EffectTrigger.GraveyardIgnition))
                {
                    request.Options.Add(new MainActionOption
                    {
                        Kind = MainActionKind.ActivateFieldEffect,
                        Card = card,
                        EffectIndex = index,
                        Label = $"{card.Name} (graveyard): {EffectChoiceLabel(card, index)}"
                    });
                }
            }

            foreach (var monster in player.Monsters())
            {
                if (monster.SummonedThisTurn || monster.HasAttackedThisTurn) continue;
                if (monster.PositionLockedThisTurn) continue;
                if (monster.PositionChangesUsed >= rules.positionChangesPerTurn) continue;
                string label = monster.FaceDown
                    ? "Face-down monster: flip face-up into Attack Position"
                    : $"{monster.Name}: switch to {(monster.Position == BattlePosition.Attack ? "Defense" : "Attack")}";
                request.Options.Add(new MainActionOption
                {
                    Kind = MainActionKind.ChangePosition,
                    Card = monster,
                    Label = label
                });
            }

            bool battleAllowed = !(TurnNumber == 1 && rules.turnPlayerSkipsFirstBattle);
            if (battleAllowed)
                request.Options.Add(new MainActionOption { Kind = MainActionKind.ToBattlePhase, Label = "To Battle Phase" });
            request.Options.Add(new MainActionOption { Kind = MainActionKind.EndTurn, Label = "End Turn" });

            return request;
        }

        private IEnumerator ExecuteMainAction(PlayerState player, MainActionOption option)
        {
            switch (option.Kind)
            {
                case MainActionKind.SummonMonster:
                    yield return ExecuteSummon(player, option.Card, option.PreferredZoneIndex);
                    break;
                case MainActionKind.SetSpell:
                {
                    presenter?.RememberView(option.Card);
                    yield return ExecuteSetSpell(player, option.Card, option.PreferredZoneIndex);
                    if (presenter != null && option.Card.Zone == ZoneType.SpellZone)
                        yield return presenter.ShowCardMoved(option.Card);
                    break;
                }
                case MainActionKind.ActivateSpellFromHand:
                    yield return ActivateSpell(player, option.Card, option.EffectIndex, true);
                    break;
                case MainActionKind.ActivateSetSpell:
                    yield return ActivateSpell(player, option.Card, option.EffectIndex, false);
                    break;
                case MainActionKind.PlayArtifact:
                    yield return ExecutePlayArtifact(player, option.Card, option.EffectIndex, option.PreferredZoneIndex);
                    break;
                case MainActionKind.ActivateFieldEffect:
                    yield return ActivateEffect(player, option.Card, option.EffectIndex);
                    break;
                case MainActionKind.ChangePosition:
                {
                    bool wasFaceDown = option.Card != null && option.Card.FaceDown;
                    ExecuteChangePosition(player, option.Card);
                    if (presenter != null) yield return presenter.ShowPositionSwitch(option.Card);
                    if (wasFaceDown && responseDepth < 2) // Flip-Effekte (Lyria)
                        yield return OfferTriggeredEffects(player, option.Card, EffectTrigger.OnFlipFaceUp);
                    break;
                }
                case MainActionKind.SpecialSummonSelf:
                    yield return ExecuteSelfSpecialSummon(player, option.Card, option.PreferredZoneIndex);
                    break;
                case MainActionKind.SummonReliquary:
                    yield return ExecuteReliquarySummon(player, option.Card, option.PreferredZoneIndex);
                    break;
            }

            // Kurzer Beat nach jeder Aktion, damit das Duell lesbar bleibt (nicht headless)
            if (presenter != null && option.Kind != MainActionKind.ToBattlePhase && option.Kind != MainActionKind.EndTurn)
                yield return DuelWait.For(0.2f);
        }

        // ================== RELIQUARY (EXTRA DECK) ==================

        /// <summary>Prüft alle Beschwörungs-Voraussetzungen einer Reliquary-Karte (inkl. Bezahlbarkeit der Kosten).</summary>
        private bool ReliquaryRequirementsMet(PlayerState player, ReliquaryCardData data)
        {
            var opponent = player.Opponent;
            if (player.Mana < Math.Max(data.summonManaCost, data.reqMinMana)) return false;

            if (!string.IsNullOrEmpty(data.reqNamedOnField))
            {
                int named = player.Monsters().Count(m => !m.FaceDown && m.Name.Contains(data.reqNamedOnField));
                if (named < Math.Max(1, data.reqNamedCount)) return false;
            }
            if (data.reqLifeBelowOpponent && player.LifePoints >= opponent.LifePoints) return false;
            if (data.reqOpponentMoreMonsters && opponent.MonsterCount() <= player.MonsterCount()) return false;
            if (data.reqOpponentMonstersAtLeast > 0 && opponent.MonsterCount() < data.reqOpponentMonstersAtLeast) return false;
            if (data.reqOwnArtifactsOnField > 0
                && player.ArtifactZones.Count(a => a != null) < data.reqOwnArtifactsOnField) return false;
            if (data.reqOwnArtifactsInGrave > 0
                && player.Graveyard.Count(c => c.ArtifactData != null) < data.reqOwnArtifactsInGrave) return false;
            if (data.reqOwnFaceDownMonsters > 0
                && player.Monsters().Count(m => m.FaceDown) < data.reqOwnFaceDownMonsters) return false;
            if (data.reqMonsterWithEquip && !player.Monsters().Any(m => m.EquippedArtifacts.Count > 0)) return false;
            if (data.reqGraveyardAtLeast > 0 && player.Graveyard.Count < data.reqGraveyardAtLeast) return false;
            if (data.reqControlNoMonsters && player.MonsterCount() > 0) return false;
            if (data.reqOwnMonstersAtLeast > 0 && player.MonsterCount() < data.reqOwnMonstersAtLeast) return false;
            if (data.reqLifeAtMost > 0 && player.LifePoints > data.reqLifeAtMost) return false;
            if (data.reqBanishedAtLeast > 0 && player.Banished.Count < data.reqBanishedAtLeast) return false;

            // Zusatzkosten müssen bezahlbar sein
            if (data.costBanishMonstersFromGrave > 0
                && player.Graveyard.Count(c => c.MonsterData != null) < data.costBanishMonstersFromGrave) return false;
            if (data.costTributeOtherMonster && player.MonsterCount() < 1) return false;

            // Tribute von beiden Feldern. Zerstörungs-Immunität zählt nicht mit —
            // sonst böte die Engine eine Beschwörung an, die beim Bezahlen scheitert.
            int ownNeeded = data.costTributeOwnMonsters + (data.costTributeOtherMonster ? 1 : 0);
            if (ownNeeded > 0 && player.Monsters().Count(m => !m.CannotBeDestroyedThisTurn) < ownNeeded) return false;
            if (data.costTributeOpponentMonsters > 0
                && player.Opponent.Monsters().Count(m => !m.CannotBeDestroyedThisTurn && !m.CannotBeTargetedThisTurn)
                   < data.costTributeOpponentMonsters) return false;

            return true;
        }

        /// <summary>Beschwört eine Reliquary aus dem Extra Deck: Kosten wählen und zahlen, dann aufs Feld.</summary>
        private IEnumerator ExecuteReliquarySummon(PlayerState player, CardInstance monster, int preferredZone = -1)
        {
            var data = monster.Definition as ReliquaryCardData;
            if (data == null || !player.ExtraDeckPile.Contains(monster)) yield break;
            if (!ReliquaryRequirementsMet(player, data)) yield break;

            // Tribut zuerst wählen (kann abgebrochen werden)
            CardInstance tributePick = null;
            if (data.costTributeOtherMonster)
            {
                var tributeRequest = new TargetRequest
                {
                    Title = $"Destroy 1 monster you control for {monster.Name}",
                    Kind = TargetKind.AllyMonster,
                    Count = 1,
                    AllowCancel = true
                };
                tributeRequest.Candidates.AddRange(player.Monsters().Where(m => !m.CannotBeDestroyedThisTurn));
                if (tributeRequest.Candidates.Count == 0) yield break;
                yield return DecideRouted(player, tributeRequest);
                if (tributeRequest.Cancelled || tributeRequest.Result.Count < 1) yield break;
                tributePick = tributeRequest.Result[0];
            }

            // Tribute von beiden Feldern — erst wählen, gezahlt wird weiter unten.
            // Getrennte Abfragen, damit klar bleibt, wessen Monster gerade stirbt:
            // ein gemeinsamer Dialog liesse einen versehentlich das eigene opfern.
            var ownTributes = new List<CardInstance>();
            if (data.costTributeOwnMonsters > 0)
            {
                var request = new TargetRequest
                {
                    Title = $"Offer {data.costTributeOwnMonsters} of your monsters to {monster.Name}",
                    Kind = TargetKind.AllyMonster,
                    Count = data.costTributeOwnMonsters,
                    AllowCancel = true
                };
                request.Candidates.AddRange(player.Monsters()
                    .Where(m => !m.CannotBeDestroyedThisTurn && m != tributePick));
                yield return DecideRouted(player, request);
                if (request.Cancelled || request.Result.Count < data.costTributeOwnMonsters) yield break;
                ownTributes.AddRange(request.Result);
            }

            var foeTributes = new List<CardInstance>();
            if (data.costTributeOpponentMonsters > 0)
            {
                var request = new TargetRequest
                {
                    Title = $"Claim {data.costTributeOpponentMonsters} of your opponent's monsters for {monster.Name}",
                    Kind = TargetKind.EnemyMonster,
                    Count = data.costTributeOpponentMonsters,
                    AllowCancel = true
                };
                request.Candidates.AddRange(player.Opponent.Monsters()
                    .Where(m => !m.CannotBeDestroyedThisTurn && !m.CannotBeTargetedThisTurn));
                yield return DecideRouted(player, request);
                if (request.Cancelled || request.Result.Count < data.costTributeOpponentMonsters) yield break;
                foeTributes.AddRange(request.Result);
            }

            var banishPicks = new List<CardInstance>();
            if (data.costBanishMonstersFromGrave > 0)
            {
                var banishRequest = new TargetRequest
                {
                    Title = $"Banish {data.costBanishMonstersFromGrave} monsters from your Graveyard for {monster.Name}",
                    Kind = TargetKind.GraveyardMonsterSelf,
                    Count = data.costBanishMonstersFromGrave,
                    AllowCancel = true
                };
                banishRequest.Candidates.AddRange(player.Graveyard.Where(c => c.MonsterData != null));
                yield return DecideRouted(player, banishRequest);
                if (banishRequest.Cancelled || banishRequest.Result.Count < data.costBanishMonstersFromGrave) yield break;
                banishPicks.AddRange(banishRequest.Result);
            }

            // Position wählen (Extra-Deck-Beschwörungen sind immer offen — kein verdecktes Setzen)
            var positionRequest = new OptionRequest
            {
                Title = $"{monster.Name}: choose position",
                Card = monster,
                AllowCancel = true
            };
            positionRequest.Options.Add("Attack Position");
            positionRequest.Options.Add("Defense Position");
            yield return DecideRouted(player, positionRequest);
            if (positionRequest.Result < 0) yield break;
            var summonPosition = positionRequest.Result == 1 ? BattlePosition.Defense : BattlePosition.Attack;

            int zoneIndex = -1;
            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                $"Choose a zone for {monster.Name}", preferredZone, index => zoneIndex = index);
            if (zoneIndex < 0) yield break;

            // ---- Kosten zahlen ----
            player.Mana -= data.summonManaCost;
            if (data.summonManaCost > 0) Log($"{player.Name} pays {data.summonManaCost} Mana ({player.Mana} Mana left).");
            if (tributePick != null)
            {
                Log($"{player.Name} offers {tributePick.Name} to the Reliquary.");
                yield return DestroyCard(tributePick);
                if (Result != DuelResult.None) yield break;
                if (IsOnField(tributePick)) { Log("The tribute was not paid — the summon is aborted."); yield break; }
            }
            // Die Tribute von beiden Feldern. Jede Zerstörung kann Effekte
            // auslösen, die das Duell beenden — deshalb nach jeder einzelnen
            // prüfen, statt am Ende einmal.
            foreach (var pick in ownTributes.Concat(foeTributes))
            {
                if (!IsOnField(pick)) continue;   // ein vorheriger Trigger hat sie schon geholt
                Log($"{monster.Name} claims {pick.Name} from {pick.Owner.Name}.");
                yield return DestroyCard(pick);
                if (Result != DuelResult.None) yield break;
            }

            foreach (var pick in banishPicks)
            {
                MoveToBanished(pick);
                Log($"{pick.Name} is banished from the graveyard.");
            }

            // Tribut-Trigger können Zonen gefüllt haben — Zone erneut absichern
            if (player.MonsterZones[zoneIndex] != null)
            {
                zoneIndex = player.FirstFreeZoneIndex(player.MonsterZones);
                if (zoneIndex < 0) { Log("No free monster zone — the Reliquary stays in the Extra Deck."); yield break; }
            }

            // Die Animation läuft VOR dem Beschwören: der Tresor öffnet sich, die
            // Karte steigt heraus und fährt zu ihrer Zone — erst dann taucht sie
            // dort wirklich auf. Läge sie schon im Feld, sähe man sie doppelt.
            if (presenter != null) yield return presenter.ShowReliquarySummon(monster, player, zoneIndex);

            player.ExtraDeckPile.Remove(monster);
            player.MonsterZones[zoneIndex] = monster;
            monster.Owner = player;
            monster.Zone = ZoneType.MonsterZone;
            monster.Position = summonPosition;
            monster.FaceDown = false;
            monster.SummonedThisTurn = true;
            monster.WasSpecialSummoned = true;
            Log($"{player.Name} Reliquary Summons {monster.Name} ({monster.CurrentAtk}/{monster.CurrentDef}) " +
                $"in {(summonPosition == BattlePosition.Attack ? "Attack" : "Defense")} Position!");
            BoardChanged();
            yield return RunSummonEvents(monster);
        }

        /// <summary>
        /// Reliquary-Karten haben keine Hand: Wird eine auf die Hand zurückgegeben, kehrt sie
        /// stattdessen ins Extra Deck zurück. Zerstörung und Verbannung laufen normal
        /// (Friedhof bzw. Banishment). True, wenn die Karte umgeleitet wurde.
        /// </summary>
        private bool ReturnToExtraDeck(CardInstance card)
        {
            if (!(card.Definition is ReliquaryCardData)) return false;
            DetachEquipsToGraveyard(card);
            RemoveFromCurrentZone(card);
            if (card.OriginalOwner != null) card.Owner = card.OriginalOwner;
            card.FaceDown = false;
            card.Zone = ZoneType.ExtraDeck;
            card.PermanentAtkBonus = 0;
            card.PermanentDefBonus = 0;
            card.TempAtkBonus = 0;
            card.TempDefBonus = 0;
            card.WasSpecialSummoned = false;
            card.Owner.ExtraDeckPile.Add(card);
            Log($"{card.Name} returns to the Extra Deck.");
            return true;
        }

        /// <summary>Spezialbeschwörung eines Handmonsters über seine eigene Bedingung (kostet keinen Normal Summon).</summary>
        private IEnumerator ExecuteSelfSpecialSummon(PlayerState player, CardInstance monster, int preferredZone = -1)
        {
            if (monster.MonsterData == null || !player.Hand.Contains(monster)) yield break;

            int zoneIndex = -1;
            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                $"Choose a zone for {monster.Name}", preferredZone, index => zoneIndex = index);
            if (zoneIndex < 0) yield break;

            // Schreibt die Karte eine Position vor, gilt sie — der Kartentext nennt sie
            // ausdrücklich ("… in Defense Position"). Sonst wählt der Spieler; beide
            // Möglichkeiten sind offen, verdeckt gesetzt wird bei Spezialbeschwörungen nie.
            var forcedPosition = monster.MonsterData.selfSummonPosition;
            var summonPosition = forcedPosition;
            if (forcedPosition != BattlePosition.Defense)
            {
                var positionRequest = new OptionRequest
                {
                    Title = $"{monster.Name}: choose position",
                    Card = monster,
                    AllowCancel = true
                };
                positionRequest.Options.Add("Attack Position");
                positionRequest.Options.Add("Defense Position");
                yield return DecideRouted(player, positionRequest);
                if (positionRequest.Result < 0) yield break;
                summonPosition = positionRequest.Result == 0 ? BattlePosition.Attack : BattlePosition.Defense;
            }

            presenter?.RememberView(monster);
            player.Hand.Remove(monster);
            player.MonsterZones[zoneIndex] = monster;
            monster.Zone = ZoneType.MonsterZone;
            monster.FaceDown = false;
            monster.Position = summonPosition;
            monster.SummonedThisTurn = true;
            monster.WasSpecialSummoned = true;
            Log($"{player.Name} special summons {monster.Name} ({monster.CurrentAtk}/{monster.CurrentDef}) in {(monster.Position == BattlePosition.Attack ? "Attack" : "Defense")} Position.");
            BoardChanged();

            if (presenter != null)
            {
                yield return presenter.ShowCardMoved(monster);
                yield return presenter.ShowSummon(monster);
            }
            yield return RunSummonEvents(monster);
        }

        // ================== BESCHWÖRUNG ==================

        /// <summary>Wunsch-Zone nutzen, wenn frei — sonst die erste freie Zone.</summary>
        private static int ResolveZoneIndex(PlayerState player, CardInstance[] zones, int preferred)
        {
            if (preferred >= 0 && preferred < zones.Length && zones[preferred] == null) return preferred;
            return player.FirstFreeZoneIndex(zones);
        }

        /// <summary>
        /// Lässt den Spieler eine freie Zone wählen (lockstep-fähig). Eine per Drag gewählte
        /// Wunsch-Zone wird direkt übernommen; bei nur einer freien Zone entfällt die Frage.
        /// </summary>
        private IEnumerator ChooseZone(PlayerState player, CardInstance[] zones, ZoneType zoneType, string title, int preferred, System.Action<int> apply)
        {
            if (preferred >= 0 && preferred < zones.Length && zones[preferred] == null)
            {
                apply(preferred);
                yield break;
            }

            var free = new List<int>();
            for (int i = 0; i < zones.Length; i++)
                if (zones[i] == null) free.Add(i);

            if (free.Count == 0) { apply(-1); yield break; }
            if (free.Count == 1) { apply(free[0]); yield break; }

            var request = new ZoneSelectRequest { Title = title, ForPlayer = player, Zone = zoneType };
            request.FreeIndices.AddRange(free);
            yield return DecideRouted(player, request);
            apply(request.Result >= 0 && request.Result < zones.Length && zones[request.Result] == null
                ? request.Result
                : free[0]);
        }

        private IEnumerator ExecuteSummon(PlayerState player, CardInstance monster, int preferredZone = -1)
        {
            if (monster.MonsterData == null || !player.Hand.Contains(monster)) yield break;
            int tributes = rules.TributesForLevel(monster.MonsterData.level);

            var tributeCards = new List<CardInstance>();
            if (tributes > 0)
            {
                var targetRequest = new TargetRequest
                {
                    Title = $"Choose {tributes} tribute{(tributes > 1 ? "s" : "")} for {monster.Name}",
                    Kind = TargetKind.AllyMonster,
                    Count = tributes,
                    AllowCancel = true
                };
                targetRequest.Candidates.AddRange(player.Monsters());
                yield return DecideRouted(player, targetRequest);
                if (targetRequest.Cancelled || targetRequest.Result.Count < tributes) yield break;
                tributeCards.AddRange(targetRequest.Result);
            }

            var positionRequest = new OptionRequest
            {
                Title = $"{monster.Name}: choose position",
                Card = monster,
                AllowCancel = true
            };
            positionRequest.Options.Add("Attack Position");
            positionRequest.Options.Add("Set face-down (Defense)");
            yield return DecideRouted(player, positionRequest);
            if (positionRequest.Result < 0) yield break;
            bool setFaceDown = positionRequest.Result == 1; // Verteidigung = verdeckt legen (Set)

            foreach (var tribute in tributeCards)
            {
                Log($"{player.Name} tributes {tribute.Name}.");
                if (presenter != null) yield return presenter.ShowCardSentToGrave(tribute);
                MoveToGraveyardWithEquips(tribute);
            }
            if (tributeCards.Count > 0) BoardChanged();

            int zoneIndex = -1;
            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                $"Choose a zone for {monster.Name}", preferredZone, index => zoneIndex = index);
            if (zoneIndex < 0) yield break;

            presenter?.RememberView(monster);
            player.Hand.Remove(monster);
            player.MonsterZones[zoneIndex] = monster;
            monster.Zone = ZoneType.MonsterZone;
            monster.Position = setFaceDown ? BattlePosition.Defense : BattlePosition.Attack;
            monster.FaceDown = setFaceDown;
            monster.SummonedThisTurn = true;
            player.NormalSummonsUsed++;

            if (setFaceDown)
            {
                // Kein Name im Log und kein Showcase — sonst verrät die UI die verdeckte Karte
                Log($"{player.Name} sets a monster face-down in Defense Position.");
                BoardChanged();
                if (presenter != null) yield return presenter.ShowCardMoved(monster);
                // Verdeckt gelegt = kein offenes Beschwören: keine Summon-Trigger, kein Response-Fenster
            }
            else
            {
                Log($"{player.Name} summons {monster.Name} ({monster.CurrentAtk}/{monster.CurrentDef}) in Attack Position.");
                BoardChanged();
                if (presenter != null) yield return presenter.ShowCardMoved(monster);
                if (presenter != null) yield return presenter.ShowSummon(monster);
                yield return RunSummonEvents(monster, true);
            }
        }

        private IEnumerator RunSummonEvents(CardInstance monster, bool wasNormalSummon = false)
        {
            if (responseDepth < 2)
            {
                if (wasNormalSummon)
                {
                    yield return OfferTriggeredEffects(monster.Owner, monster, EffectTrigger.OnNormalSummonSelf);
                    if (Result != DuelResult.None) yield break;
                }
                yield return OfferTriggeredEffects(monster.Owner, monster, EffectTrigger.OnSummonSelf);
                if (Result != DuelResult.None) yield break;
                yield return OpenResponseWindow(monster.Owner.Opponent, "summon", monster);
            }
        }

        /// <summary>Gemeinsamer Pfad aller Effekt-Spezialbeschwörungen: Zone wählen, aufs Feld, Events.</summary>
        private IEnumerator SpecialSummonToField(PlayerState player, CardInstance monster, string sourceDescription)
        {
            if (player.CannotSpecialSummonThisTurn)
            {
                Log($"{player.Name} cannot Special Summon this turn — {monster.Name} stays where it is.");
                yield break;
            }

            int zoneIndex = -1;
            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                $"Choose a zone for {monster.Name}", -1, index => zoneIndex = index);
            if (zoneIndex < 0)
            {
                Log("No free monster zone — the special summon fizzles.");
                yield break;
            }

            presenter?.RememberOrigin(monster);
            RemoveFromCurrentZone(monster);
            player.MonsterZones[zoneIndex] = monster;
            monster.Owner = player;
            monster.Zone = ZoneType.MonsterZone;
            monster.Position = BattlePosition.Attack;
            monster.SummonedThisTurn = true;
            monster.WasSpecialSummoned = true;
            monster.PermanentAtkBonus = 0;
            monster.PermanentDefBonus = 0;
            monster.TempAtkBonus = 0;
            monster.TempDefBonus = 0;
            Log($"{player.Name} special summons {monster.Name} {sourceDescription}.");
            BoardChanged();
            if (presenter != null)
            {
                yield return presenter.ShowCardMoved(monster);
                yield return presenter.ShowSummon(monster);
            }
            yield return RunSummonEvents(monster);
        }

        // ================== ZAUBER & ARTEFAKTE ==================

        private IEnumerator ExecuteSetSpell(PlayerState player, CardInstance spell, int preferredZone = -1)
        {
            if (!player.Hand.Contains(spell)) yield break;

            int zoneIndex = -1;
            yield return ChooseZone(player, player.SpellZones, ZoneType.SpellZone,
                $"Choose a zone for {spell.Name}", preferredZone, index => zoneIndex = index);
            if (zoneIndex < 0) yield break;

            player.Hand.Remove(spell);
            player.SpellZones[zoneIndex] = spell;
            spell.Zone = ZoneType.SpellZone;
            spell.FaceDown = true;
            spell.SetThisTurn = true;
            Log($"{player.Name} sets a card face-down.");
            BoardChanged();
        }

        private IEnumerator ActivateSpell(PlayerState player, CardInstance spell, int effectIndex, bool fromHand)
        {
            var effect = GetEffect(spell, effectIndex);
            if (effect == null || player.Mana < effect.manaCost) yield break;

            var targets = new TargetCollection();
            yield return CollectTargets(player, effect, targets, true, spell);
            if (targets.Cancelled) yield break;

            // Aufdecken kommt VOR dem Puls: eine Karte wird gezeigt und aktiviert
            // dann — nicht umgekehrt. Sonst spielt die ganze Animation auf einem
            // Kartenrücken, und der Gegner sieht erst hinterher, was ihn traf.
            spell.FaceDown = false;
            BoardChanged();

            // Aktivierungs-Puls auf der Karte selbst (Hand: mit Dreh, Feld: Blink+Pop)
            if (presenter != null) yield return presenter.ShowActivationPulse(spell, fromHand);

            player.Mana -= effect.manaCost;
            LockEffectForTurn(spell, effectIndex, effect);

            if (fromHand) player.Hand.Remove(spell);
            else RemoveFromZoneArray(player.SpellZones, spell);

            activationSerial++;
            Log($"{player.Name} activates {spell.Name}{ActivationLogSuffix(effect)}.");
            BoardChanged();

            // Kosten-Aktionen fallen sofort — noch bevor der Gegner reagieren kann
            yield return ResolveEffectActions(spell, effect, player, targets, costsPhase: true);
            if (Result != DuelResult.None) yield break;


            int chainBefore = activationSerial;
            yield return OpenResponseWindow(player.Opponent, "activation", spell);
            if (Result != DuelResult.None) yield break;

            if (spell.EffectsNegated)
            {
                Log($"{spell.Name}'s effect is negated — nothing happens.");
            }
            else
            {
                if (activationSerial != chainBefore) Log($"{spell.Name} resolves.");
                yield return ResolveEffectActions(spell, effect, player, targets);
            }
            if (spell.Zone != ZoneType.Graveyard && spell.Zone != ZoneType.Banished && presenter != null)
                yield return presenter.ShowSpellToGrave(spell);
            MoveToGraveyard(spell);
            BoardChanged();
        }

        private IEnumerator ExecutePlayArtifact(PlayerState player, CardInstance artifact, int effectIndex, int preferredZone = -1)
        {
            if (artifact.ArtifactData == null || !player.Hand.Contains(artifact)) yield break;

            int zoneIndex = -1;
            yield return ChooseZone(player, player.ArtifactZones, ZoneType.ArtifactZone,
                $"Choose a zone for {artifact.Name}", preferredZone, index => zoneIndex = index);
            if (zoneIndex < 0) yield break;

            CardInstance equipTarget = null;
            if (artifact.ArtifactData.slot == ArtifactSlot.Monster)
            {
                var targetRequest = new TargetRequest
                {
                    Title = $"{artifact.Name}: choose a monster to equip",
                    Kind = TargetKind.AllyMonster,
                    Count = 1,
                    AllowCancel = true
                };
                targetRequest.Candidates.AddRange(player.Monsters());
                yield return DecideRouted(player, targetRequest);
                if (targetRequest.Cancelled || targetRequest.Result.Count == 0) yield break;
                equipTarget = targetRequest.Result[0];
            }

            presenter?.RememberView(artifact);
            player.Hand.Remove(artifact);
            player.ArtifactZones[zoneIndex] = artifact;
            artifact.Zone = ZoneType.ArtifactZone;
            if (equipTarget != null)
            {
                artifact.EquipTarget = equipTarget;
                equipTarget.EquippedArtifacts.Add(artifact);
            }

            string suffix = equipTarget != null ? $" onto {equipTarget.Name}" : "";
            Log($"{player.Name} plays artifact {artifact.Name}{suffix}.");
            BoardChanged();
            if (presenter != null) yield return presenter.ShowCardMoved(artifact);

            if (effectIndex >= 0)
                yield return ActivateEffect(player, artifact, effectIndex);
            else
                yield return OpenResponseWindow(player.Opponent, "artifact", artifact);
        }

        /// <summary>Aktiviert einen Effekt einer liegenden Karte (Ignition/Quick/Trigger).</summary>
        private IEnumerator ActivateEffect(PlayerState player, CardInstance card, int effectIndex)
        {
            var effect = GetEffect(card, effectIndex);
            if (effect == null || player.Mana < effect.manaCost) yield break;

            var targets = new TargetCollection();
            yield return CollectTargets(player, effect, targets, true, card);
            if (targets.Cancelled) yield break;

            // Auch hier zuerst aufdecken — ein verdecktes Artefakt, das zündet,
            // muss sichtbar sein, bevor es wirkt
            if (card.FaceDown)
            {
                card.FaceDown = false;
                BoardChanged();
            }

            // Aktivierungs-Puls auf der Karte (Hand-Ignition mit Dreh, Feldkarten mit Blink+Pop)
            if (presenter != null) yield return presenter.ShowActivationPulse(card, card.Zone == ZoneType.Hand);

            player.Mana -= effect.manaCost;
            LockEffectForTurn(card, effectIndex, effect);

            activationSerial++;
            Log($"{player.Name} activates {card.Name}: \"{effect.label}\"{ActivationLogSuffix(effect)}.");
            BoardChanged();

            // Kosten-Aktionen fallen sofort — noch bevor der Gegner reagieren kann
            yield return ResolveEffectActions(card, effect, player, targets, costsPhase: true);
            if (Result != DuelResult.None) yield break;


            int chainBefore = activationSerial;
            yield return OpenResponseWindow(player.Opponent, "activation", card);
            if (Result != DuelResult.None) yield break;

            if (card.EffectsNegated)
            {
                Log($"{card.Name}'s effect is negated — nothing happens.");
            }
            else
            {
                if (activationSerial != chainBefore) Log($"{card.Name} resolves.");
                yield return ResolveEffectActions(card, effect, player, targets);
            }
            BoardChanged();
        }

        /// <summary>Deckt ein verdecktes Monster auf und löst seinen Flip-Effekt aus.</summary>
        private IEnumerator FlipFaceUp(CardInstance monster, BattlePosition position = BattlePosition.Attack)
        {
            if (monster == null || monster.Zone != ZoneType.MonsterZone || !monster.FaceDown) yield break;
            monster.FaceDown = false;
            monster.Position = position;
            Log($"{monster.Name} is flipped face-up!");
            BoardChanged();
            if (responseDepth < 2)
                yield return OfferTriggeredEffects(monster.Owner, monster, EffectTrigger.OnFlipFaceUp);
        }

        private void ExecuteChangePosition(PlayerState player, CardInstance monster)
        {
            if (monster.Zone != ZoneType.MonsterZone) return;
            if (monster.FaceDown)
            {
                monster.FaceDown = false;
                monster.Position = BattlePosition.Attack;
                monster.PositionChangesUsed++;
                Log($"{monster.Name} is flipped face-up into Attack Position!");
                BoardChanged();
                return;
            }
            monster.Position = monster.Position == BattlePosition.Attack ? BattlePosition.Defense : BattlePosition.Attack;
            monster.PositionChangesUsed++;
            Log($"{monster.Name} switches to {(monster.Position == BattlePosition.Attack ? "Attack" : "Defense")} Position.");
            BoardChanged();
        }

        // ================== EFFEKTE ==================

        private EffectDefinition GetEffect(CardInstance card, int index)
        {
            if (card?.Definition == null) return null;
            if (index < 0 || index >= card.Definition.effects.Count) return null;
            return card.Definition.effects[index];
        }

        /// <summary>
        /// Alle Effekte, die sich EINE Nutzung pro Zug teilen: ein Normal-Effekt
        /// und jeder Coupled-Infused-Effekt, der ihm folgt — bis zum nächsten
        /// Normal-Effekt, der eine neue Gruppe eröffnet.
        ///
        /// <para>
        /// Standalone-Infused-Effekte stehen bewusst NICHT drin. Sie sind eigene
        /// Fähigkeiten und dürfen neben allem anderen laufen; sie unterbrechen
        /// die Gruppe aber auch nicht, sondern stehen einfach daneben.
        /// </para>
        /// <para>
        /// <b>Warum eine Liste und kein einzelner Partner.</b> Vorher gab diese
        /// Methode genau einen Index zurück. Bei zwei Coupled-Effekten unter
        /// demselben Normal-Effekt sperrte eine Aktivierung nur einen von beiden
        /// — der zweite blieb nutzbar, und die Karte tat pro Zug zweimal das,
        /// was sie laut Text nur einmal darf. Solange es je Karte höchstens einen
        /// Infused-Effekt gab, fiel das nicht auf.
        /// </para>
        /// Leere Liste heisst: dieser Effekt teilt sich mit niemandem.
        /// </summary>
        public List<int> CoupledGroup(CardInstance card, int index)
        {
            var group = new List<int>();
            var effects = card?.Definition?.effects;
            if (effects == null || index < 0 || index >= effects.Count) return group;

            var effect = effects[index];
            if (effect.isInfused && effect.infusedKind != InfusedKind.Coupled) return group;

            // Gruppenanfang: der Normal-Effekt, unter dem dieser Effekt hängt
            int start = index;
            while (start >= 0 && effects[start].isInfused) start--;
            if (start < 0) return group;   // Coupled ohne Normal-Effekt davor — koppelt an nichts

            group.Add(start);
            for (int i = start + 1; i < effects.Count; i++)
            {
                if (!effects[i].isInfused) break;
                if (effects[i].infusedKind == InfusedKind.Coupled) group.Add(i);
            }

            // Ein Normal-Effekt ohne Coupled-Partner teilt sich mit niemandem
            return group.Count > 1 ? group : new List<int>();
        }

        /// <summary>Sperrt Once-per-Turn-Index und die ganze gekoppelte Gruppe für diesen Zug.</summary>
        private void LockEffectForTurn(CardInstance card, int effectIndex, EffectDefinition effect)
        {
            if (effect.oncePerTurn) card.OncePerTurnUsed.Add(effectIndex);
            // Wer einen aus der Gruppe nutzt, verbraucht die ganze Gruppe.
            foreach (int i in CoupledGroup(card, effectIndex)) card.OncePerTurnUsed.Add(i);
        }

        public List<int> ActivatableEffects(CardInstance card, PlayerState player, EffectTrigger trigger)
        {
            var result = new List<int>();
            if (card?.Definition == null) return result;
            for (int i = 0; i < card.Definition.effects.Count; i++)
            {
                var effect = card.Definition.effects[i];
                if (effect.trigger != trigger) continue;
                if (player.Mana < effect.manaCost) continue;
                if (card.OncePerTurnUsed.Contains(i)) continue;
                if (effect.onlyIfSpecialSummoned && !card.WasSpecialSummoned) continue;
                if (effect.requiresEquippedArtifact && card.EquippedArtifacts.Count == 0) continue;
                if (card.EffectsNegated) continue; // annullierte Karte kann nichts aktivieren
                if (!HasValidTargets(effect, player, card)) continue;
                result.Add(i);
            }
            return result;
        }

        private bool HasValidTargets(EffectDefinition effect, PlayerState player, CardInstance source = null)
        {
            foreach (var action in effect.actions)
            {
                if (action.target == TargetKind.None || action.target == TargetKind.SelfCard) continue;
                if (BuildTargetCandidates(action, player, source).Count == 0) return false;
            }
            return true;
        }

        /// <summary>Liegt irgendwo auf dem Feld eine verdeckte Karte (Monster oder gesetzter Zauber)?</summary>
        private bool AnyFaceDownOnField()
        {
            foreach (var player in new[] { Player1, Player2 })
            {
                foreach (var monster in player.MonsterZones) if (monster != null && monster.FaceDown) return true;
                foreach (var spell in player.SpellZones) if (spell != null && spell.FaceDown) return true;
            }
            return false;
        }

        /// <summary>Prüft Typ-/Level-/Namens-Filter einer Action gegen eine Karte.</summary>
        private static bool MatchesFilter(EffectAction action, CardInstance card)
        {
            var monster = card.MonsterData;
            if (action.useTypeFilter && (monster == null || monster.monsterType != action.typeFilter)) return false;
            if (action.useAttributeFilter && (monster == null || monster.attribute != action.attributeFilter)) return false;
            if (action.levelFilter > 0 && (monster == null || monster.level != action.levelFilter)) return false;
            if (action.maxAtkFilter > 0 && (monster == null || card.CurrentAtk > action.maxAtkFilter)) return false;
            if (!string.IsNullOrEmpty(action.nameFilter) && !card.Name.Contains(action.nameFilter)) return false;
            if (!string.IsNullOrEmpty(action.mentionsFilter) && !CardMentions(card, action.mentionsFilter)) return false;
            return true;
        }

        /// <summary>True, wenn der Name ODER ein Effekttext der Karte den Begriff enthält.</summary>
        private static bool CardMentions(CardInstance card, string term)
        {
            if (card.Name.Contains(term)) return true;
            if (card.Definition?.effects == null) return false;
            foreach (var effect in card.Definition.effects)
            {
                if (effect == null) continue;
                if (!string.IsNullOrEmpty(effect.text) && effect.text.Contains(term)) return true;
                if (!string.IsNullOrEmpty(effect.label) && effect.label.Contains(term)) return true;
            }
            return false;
        }

        private static bool ActionHasFilter(EffectAction action) =>
            action.useTypeFilter || action.useAttributeFilter || action.levelFilter > 0
            || action.maxAtkFilter > 0
            || !string.IsNullOrEmpty(action.nameFilter) || !string.IsNullOrEmpty(action.mentionsFilter);

        private List<CardInstance> BuildTargetCandidates(EffectAction action, PlayerState player, CardInstance source = null)
        {
            var candidates = new List<CardInstance>();
            switch (action.target)
            {
                case TargetKind.EnemyMonster: candidates.AddRange(player.Opponent.Monsters()); break;
                case TargetKind.AllyMonster: candidates.AddRange(player.Monsters()); break;
                case TargetKind.AnyMonster:
                    candidates.AddRange(player.Monsters());
                    candidates.AddRange(player.Opponent.Monsters());
                    break;
                case TargetKind.GraveyardMonsterSelf:
                    candidates.AddRange(player.Graveyard.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.GraveyardCardSelf:
                    candidates.AddRange(player.Graveyard);
                    break;
                case TargetKind.DeckMonsterFiltered:
                    candidates.AddRange(player.DeckPile.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.HandMonsterFiltered:
                    candidates.AddRange(player.Hand.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.HandOrGraveMonsterFiltered:
                    candidates.AddRange(player.Hand.Where(c => c.MonsterData != null));
                    candidates.AddRange(player.Graveyard.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.GraveyardCardAny:
                    candidates.AddRange(player.Graveyard);
                    candidates.AddRange(player.Opponent.Graveyard);
                    break;
                case TargetKind.BanishedMonsterAny:
                    candidates.AddRange(player.Banished.Where(c => c.MonsterData != null));
                    candidates.AddRange(player.Opponent.Banished.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.DeckSpellFiltered:
                    candidates.AddRange(player.DeckPile.Where(c => c.SpellData != null));
                    break;
                case TargetKind.DeckCardFiltered:
                    candidates.AddRange(player.DeckPile);
                    break;
                case TargetKind.GraveOrBanishedMonsterSelf:
                    candidates.AddRange(player.Graveyard.Where(c => c.MonsterData != null));
                    candidates.AddRange(player.Banished.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.FaceDownMonsterAny:
                    candidates.AddRange(player.Monsters().Where(m => m.FaceDown));
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.FaceDown));
                    break;
                case TargetKind.FaceDownMonsterEnemy:
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.FaceDown));
                    break;
                case TargetKind.GraveyardSpellSelf:
                    candidates.AddRange(player.Graveyard.Where(c => c.SpellData != null));
                    break;
                case TargetKind.DeckArtifactFiltered:
                    candidates.AddRange(player.DeckPile.Where(c => c.ArtifactData != null));
                    break;
                case TargetKind.GraveyardArtifactSelf:
                    candidates.AddRange(player.Graveyard.Where(c => c.ArtifactData != null));
                    break;
                case TargetKind.HandArtifactFiltered:
                    candidates.AddRange(player.Hand.Where(c => c.ArtifactData != null));
                    break;
                case TargetKind.FaceDownMonsterSelf:
                    candidates.AddRange(player.Monsters().Where(m => m.FaceDown));
                    break;
                case TargetKind.EnemyCardOnField:
                    candidates.AddRange(player.Opponent.FieldCards());
                    break;
                case TargetKind.AllyArtifact:
                    candidates.AddRange(player.ArtifactZones.Where(a => a != null));
                    break;
                case TargetKind.HandCardSelf:
                    candidates.AddRange(player.Hand);
                    break;
                case TargetKind.HandCardOpponent:
                    candidates.AddRange(player.Opponent.Hand);
                    break;
                case TargetKind.DeckMonsterFilteredSelf:
                    candidates.AddRange(player.DeckPile.Where(c => c.MonsterData != null));
                    break;
                // TargetKind.SelfCard: kein Auswahl-Dialog — wird in ResolveEffectActions direkt zur Quellkarte.
            }
            if (ActionHasFilter(action)) candidates.RemoveAll(c => !MatchesFilter(action, c));
            if (action.targetExcludesSelf && source != null) candidates.Remove(source);
            // Ziel-Immunität gilt nur gegen den Gegner — eigene Effekte dürfen weiter anvisieren
            candidates.RemoveAll(c => c != null && c.CannotBeTargetedThisTurn && c.Owner != player);
            return candidates;
        }

        private IEnumerator CollectTargets(PlayerState player, EffectDefinition effect, TargetCollection result, bool canCancel, CardInstance source = null)
        {
            for (int i = 0; i < effect.actions.Count; i++)
            {
                var action = effect.actions[i];
                if (action.target == TargetKind.None || action.target == TargetKind.SelfCard) continue;

                var candidates = BuildTargetCandidates(action, player, source);
                if (candidates.Count == 0) continue;

                int targetCount = Math.Clamp(action.targetCount, 1, candidates.Count);
                bool upTo = action.upToTargets && targetCount > 1;
                var request = new TargetRequest
                {
                    Title = targetCount > 1
                        ? (upTo ? $"\"{effect.label}\" — choose up to {targetCount} targets"
                                : $"\"{effect.label}\" — choose {targetCount} targets")
                        : $"\"{effect.label}\" — choose target",
                    Kind = action.target,
                    Count = targetCount,
                    AllowFewer = upTo,
                    AllowCancel = canCancel
                };
                request.Candidates.AddRange(candidates);
                yield return DecideRouted(player, request);
                if (request.Cancelled) { result.Cancelled = true; yield break; }
                result.PerAction[i] = new List<CardInstance>(request.Result);
                foreach (var chosen in request.Result) result.RecordSnapshot(chosen);
            }

            // Gewählte Ziele blinken rot auf (nur sichtbare Feld-/Handkarten)
            if (presenter != null && result.PerAction.Count > 0)
            {
                var flashTargets = new List<CardInstance>();
                foreach (var pair in result.PerAction) flashTargets.AddRange(pair.Value);
                if (flashTargets.Count > 0) yield return presenter.ShowTargetsFlash(flashTargets);
            }
        }

        /// <summary>
        /// Führt die Aktionen eines Effekts aus. costsPhase=true führt nur Kosten-Aktionen aus
        /// (sofort bei Aktivierung, vor Reaktionen), false nur die eigentlichen Effekt-Aktionen.
        /// </summary>
        private IEnumerator ResolveEffectActions(CardInstance source, EffectDefinition effect, PlayerState player, TargetCollection targets, bool costsPhase = false)
        {
            for (int i = 0; i < effect.actions.Count; i++)
            {
                if (Result != DuelResult.None) yield break;
                var action = effect.actions[i];
                if (action.isCost != costsPhase) continue;
                targets.PerAction.TryGetValue(i, out var chosen);
                if (chosen != null && chosen.Count > 0)
                {
                    // YuGiOh-Regel: Ziele, die Zone/Besitzer/Face-Zustand gewechselt haben, verpuffen
                    var valid = chosen.FindAll(targets.StillValid);
                    if (valid.Count < chosen.Count)
                        foreach (var lost in chosen)
                            if (!valid.Contains(lost))
                                Log($"{lost.Name} is no longer a valid target — the effect fizzles.");
                    chosen = valid;
                }
                var target = chosen != null && chosen.Count > 0 ? chosen[0] : null;
                if (action.target == TargetKind.SelfCard) target = source;

                // Alle gewählten Ziele einer Aktion ("bis zu N …" wirkt auf jedes davon)
                var affected = new List<CardInstance>();
                if (action.target == TargetKind.SelfCard) affected.Add(source);
                else if (chosen != null) affected.AddRange(chosen);

                switch (action.type)
                {
                    case EffectActionType.DamageOpponent:
                        DealDamage(player.Opponent, action.amount, source.Name);
                        break;
                    case EffectActionType.HealSelf:
                        player.LifePoints += action.amount;
                        Log($"{player.Name} gains {action.amount} LP ({player.LifePoints} LP).");
                        OnLifeChanged?.Invoke(player, action.amount);
                        break;
                    case EffectActionType.DrawCards:
                        if (!TryDraw(player, action.amount)) yield break;
                        yield return PresentDraws(player);
                        break;
                    case EffectActionType.GainMana:
                        player.Mana += action.amount;
                        Log($"{player.Name} gains {action.amount} Mana ({player.Mana} Mana).");
                        break;
                    case EffectActionType.DestroyTargetMonster:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            if (IsProtectedFromEffectDestruction(hit, player))
                            {
                                Log($"{hit.Name} is protected and cannot be destroyed by card effects.");
                                continue;
                            }
                            Log($"{source.Name} destroys {hit.Name}.");
                            yield return DestroyCard(hit);
                        }
                        break;
                    case EffectActionType.BanishTargetMonster:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            Log($"{source.Name} banishes {hit.Name}.");
                            if (presenter != null) yield return presenter.ShowCardBanished(hit);
                            MoveToBanished(hit);
                        }
                        break;
                    case EffectActionType.BuffTargetAtk:
                        foreach (var hit in affected)
                            if (IsOnField(hit)) { hit.PermanentAtkBonus += action.amount; Log($"{hit.Name} permanently gains +{action.amount} ATK ({hit.CurrentAtk})."); }
                        break;
                    case EffectActionType.BuffTargetAtkUntilEndOfTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.TempAtkBonus += action.amount;
                            Log(action.amount >= 0
                                ? $"{hit.Name} gains +{action.amount} ATK until end of turn ({hit.CurrentAtk})."
                                : $"{hit.Name} loses {-action.amount} ATK until end of turn ({hit.CurrentAtk}).");
                        }
                        break;
                    case EffectActionType.BuffTargetDefUntilEndOfTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.TempDefBonus += action.amount;
                            Log(action.amount >= 0
                                ? $"{hit.Name} gains +{action.amount} DEF until end of turn ({hit.CurrentDef})."
                                : $"{hit.Name} loses {-action.amount} DEF until end of turn ({hit.CurrentDef}).");
                        }
                        break;
                    case EffectActionType.EquipTargetArtifactToSelf:
                        if (target != null && target.ArtifactData != null && IsOnField(source) && source.MonsterData != null)
                        {
                            int equipZone = player.FirstFreeZoneIndex(player.ArtifactZones);
                            if (equipZone < 0) { Log("No free artifact zone — the equip fizzles."); break; }
                            bool fromDeck = target.Zone == ZoneType.Deck;
                            RemoveFromCurrentZone(target);
                            player.Hand.Remove(target);
                            player.DeckPile.Remove(target);
                            player.Graveyard.Remove(target);
                            player.ArtifactZones[equipZone] = target;
                            target.Owner = player;
                            target.Zone = ZoneType.ArtifactZone;
                            target.EquipTarget = source;
                            source.EquippedArtifacts.Add(target);
                            if (fromDeck) Shuffle(player.DeckPile);
                            Log($"{source.Name} equips {target.Name}" + (fromDeck ? " from the deck." : "."));
                        }
                        break;
                    case EffectActionType.FlipTargetFaceUp:
                        if (chosen != null)
                            foreach (var pick in chosen)
                            {
                                if (pick == null || !IsOnField(pick) || !pick.FaceDown) continue;
                                yield return FlipFaceUp(pick);
                                if (Result != DuelResult.None) yield break;
                            }
                        break;
                    case EffectActionType.SpecialSummonTargetFaceDown:
                        if (target != null && target.MonsterData != null
                            && (target.Zone == ZoneType.Hand || target.Zone == ZoneType.Deck))
                        {
                            int fdZone = player.FirstFreeZoneIndex(player.MonsterZones);
                            if (fdZone < 0) { Log("No free monster zone — the set fizzles."); break; }
                            bool fromDeckSet = target.Zone == ZoneType.Deck;
                            player.Hand.Remove(target);
                            player.DeckPile.Remove(target);
                            player.MonsterZones[fdZone] = target;
                            target.Owner = player;
                            target.Zone = ZoneType.MonsterZone;
                            target.Position = BattlePosition.Defense;
                            target.FaceDown = true;
                            target.SummonedThisTurn = true;
                            target.WasSpecialSummoned = true;
                            if (fromDeckSet) Shuffle(player.DeckPile);
                            Log($"{player.Name} sets a monster face-down in Defense Position.");
                            BoardChanged();
                        }
                        break;
                    case EffectActionType.SetTargetArtifactFromDeck:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.ArtifactData == null || hit.Zone != ZoneType.Deck) continue;
                            int artZone = player.FirstFreeZoneIndex(player.ArtifactZones);
                            if (artZone < 0) { Log("No free artifact zone — the set fizzles."); break; }
                            player.DeckPile.Remove(hit);
                            player.ArtifactZones[artZone] = hit;
                            hit.Owner = player;
                            hit.Zone = ZoneType.ArtifactZone;
                            Shuffle(player.DeckPile);
                            Log($"{player.Name} places {hit.Name} from the deck and shuffles.");
                            BoardChanged();
                        }
                        break;
                    case EffectActionType.SpecialSummonTargetFromDeck:
                        if (target != null && target.MonsterData != null && target.Zone == ZoneType.Deck)
                        {
                            player.DeckPile.Remove(target);
                            Shuffle(player.DeckPile);
                            yield return SpecialSummonToField(player, target, "from the deck");
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.NegateTargetCard:
                        foreach (var hit in affected)
                        {
                            if (hit == null || !IsOnField(hit)) continue;
                            hit.EffectsNegated = true;
                            Log($"{hit.Name}'s effects are negated until the end of the turn!");
                            BoardChanged();
                        }
                        break;
                    case EffectActionType.ProtectSelfThisTurn:
                        if (IsOnField(source))
                        {
                            source.CannotBeDestroyedThisTurn = true;
                            Log($"{source.Name} cannot be destroyed this turn.");
                        }
                        break;
                    case EffectActionType.PurgeTargetBuffs:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.PermanentAtkBonus = 0;
                            hit.PermanentDefBonus = 0;
                            hit.TempAtkBonus = 0;
                            hit.TempDefBonus = 0;
                            Log($"{hit.Name} is purged — all modifications removed ({hit.CurrentAtk}/{hit.CurrentDef}).");
                        }
                        break;
                    case EffectActionType.DebuffTargetAtk:
                        foreach (var hit in affected)
                            if (IsOnField(hit)) { hit.PermanentAtkBonus -= action.amount; Log($"{hit.Name} loses {action.amount} ATK ({hit.CurrentAtk})."); }
                        break;
                    case EffectActionType.SpecialSummonFromGraveyard:
                        if (target != null && target.Zone == ZoneType.Graveyard && target.MonsterData != null)
                        {
                            int zoneIndex = -1;
                            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                                $"Choose a zone for {target.Name}", -1, index => zoneIndex = index);
                            if (zoneIndex >= 0)
                            {
                                target.Owner.Graveyard.Remove(target);
                                player.MonsterZones[zoneIndex] = target;
                                target.Owner = player;
                                target.Zone = ZoneType.MonsterZone;
                                target.Position = BattlePosition.Attack;
                                target.SummonedThisTurn = true;
                                target.WasSpecialSummoned = true;
                                target.PermanentAtkBonus = 0;
                                target.PermanentDefBonus = 0;
                                target.TempAtkBonus = 0;
                                target.TempDefBonus = 0;
                                Log($"{player.Name} special summons {target.Name} from the graveyard.");
                                BoardChanged();
                                if (presenter != null) yield return presenter.ShowSummon(target);
                                
yield return RunSummonEvents(target);
                            }
                            else Log("No free monster zone — the special summon fizzles.");
                        }
                        break;
                    case EffectActionType.ReturnTargetToHand:
                        foreach (var hit in affected)
                        {
                            var target2 = hit;
                            if (!IsOnField(target2)) continue;
                            if (ReturnToExtraDeck(target2)) continue; // Reliquarys kehren ins Extra Deck zurück
                            Log($"{target2.Name} is returned to the hand.");
                            DetachEquipsToGraveyard(target2);
                            RemoveFromZoneArray(target2.Owner.MonsterZones, target2);
                            if (target2.OriginalOwner != null) target2.Owner = target2.OriginalOwner; // zurück zum Besitzer
                            target2.Zone = ZoneType.Hand;
                            target2.WasSpecialSummoned = false;
                            target2.Owner.Hand.Add(target2);
                        }
                        break;
                    case EffectActionType.BuffTargetDef:
                        foreach (var hit in affected)
                            if (IsOnField(hit)) { hit.PermanentDefBonus += action.amount; Log($"{hit.Name} permanently gains +{action.amount} DEF ({hit.CurrentDef})."); }
                        break;
                    case EffectActionType.DebuffTargetDef:
                        foreach (var hit in affected)
                            if (IsOnField(hit)) { hit.PermanentDefBonus -= action.amount; Log($"{hit.Name} loses {action.amount} DEF ({hit.CurrentDef})."); }
                        break;
                    case EffectActionType.DamageBothPlayers:
                        DealDamage(player, action.amount, source.Name);
                        if (Result == DuelResult.None) DealDamage(player.Opponent, action.amount, source.Name);
                        break;
                    case EffectActionType.DiscardOpponentRandom:
                        for (int d = 0; d < action.amount && player.Opponent.Hand.Count > 0; d++)
                        {
                            var discarded = player.Opponent.Hand[rng.Next(player.Opponent.Hand.Count)];
                            if (presenter != null) yield return presenter.ShowCardSentToGrave(discarded);
                            MoveToGraveyard(discarded);
                            Log($"{player.Opponent.Name} discards {discarded.Name}.");
                        }
                        break;
                    case EffectActionType.DrainOpponentMana:
                        int drained = Math.Min(player.Opponent.Mana, action.amount);
                        player.Opponent.Mana -= drained;
                        Log($"{player.Opponent.Name} loses {drained} Mana ({player.Opponent.Mana} Mana).");
                        break;
                    case EffectActionType.DrainOpponentManaNextTurn:
                        player.Opponent.ManaDebt += action.amount;
                        Log($"{player.Opponent.Name} will have {player.Opponent.ManaDebt} less Mana next turn.");
                        break;
                    case EffectActionType.GainManaNextTurn:
                        player.ManaCredit += action.amount;
                        Log($"{player.Name} will have {player.ManaCredit} more Mana next turn.");
                        break;
                    case EffectActionType.ReturnFromGraveyardToHand:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Graveyard) continue;
                            if (ReturnToExtraDeck(hit)) continue; // Reliquarys kehren ins Extra Deck zurück
                            RemoveFromCurrentZone(hit);
                            hit.Zone = ZoneType.Hand;
                            hit.Owner.Hand.Add(hit);
                            Log($"{player.Name} returns {hit.Name} from the graveyard to their hand.");
                        }
                        break;
                    case EffectActionType.AddTargetFromDeckToHand:
                        if (target != null && target.Zone == ZoneType.Deck)
                        {
                            player.DeckPile.Remove(target);
                            target.Zone = ZoneType.Hand;
                            player.Hand.Add(target);
                            Shuffle(player.DeckPile);
                            Log($"{player.Name} adds {target.Name} from the deck to their hand and shuffles.");
                        }
                        break;
                    case EffectActionType.SpecialSummonTargetFromHand:
                        // targetCount > 1 erlaubt "beschwöre bis zu N Monster von der Hand"
                        foreach (var pick in (chosen ?? new List<CardInstance>()))
                        {
                            if (pick == null || pick.Zone != ZoneType.Hand || pick.MonsterData == null) continue;
                            if (player.FreeMonsterZones() <= 0) { Log("No free monster zone — no further summons."); break; }
                            yield return SpecialSummonToField(player, pick, "from the hand");
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.TributeSelfSpecialSummonTarget:
                        if (target != null && target.MonsterData != null && IsOnField(source))
                        {
                            Log($"{player.Name} sends {source.Name} to the graveyard.");
                            if (presenter != null) yield return presenter.ShowCardSentToGrave(source);
                            MoveToGraveyardWithEquips(source);
                            string from = target.Zone == ZoneType.Graveyard ? "from the graveyard" : "from the hand";
                            yield return SpecialSummonToField(player, target, from);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.SpecialSummonTargetFromHandOrGrave:
                        if (target != null && target.MonsterData != null
                            && (target.Zone == ZoneType.Hand || target.Zone == ZoneType.Graveyard))
                        {
                            string summonOrigin = target.Zone == ZoneType.Graveyard ? "from the graveyard" : "from the hand";
                            yield return SpecialSummonToField(player, target, summonOrigin);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.GrantAdditionalAttack:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.BonusAttacks++;
                            Log($"{hit.Name} can attack an additional time this turn!");
                        }
                        break;
                    case EffectActionType.BanishTarget:
                        if (chosen != null)
                            foreach (var pick in chosen)
                                if (pick != null && pick.Zone != ZoneType.Banished)
                                {
                                    Log($"{pick.Name} is banished.");
                                    if (presenter != null) yield return presenter.ShowCardBanished(pick);
                                    MoveToBanished(pick);
                                }
                        break;
                    case EffectActionType.SpecialSummonTargetFromBanished:
                        if (target != null && target.Zone == ZoneType.Banished && target.MonsterData != null)
                        {
                            yield return SpecialSummonToField(player, target, "from the banishment");
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.SetTargetSpellFromDeck:
                        if (target != null && target.Zone == ZoneType.Deck && target.SpellData != null)
                        {
                            int spellZone = player.FirstFreeZoneIndex(player.SpellZones);
                            if (spellZone < 0) { Log("No free spell zone — the set fizzles."); break; }
                            player.DeckPile.Remove(target);
                            player.SpellZones[spellZone] = target;
                            target.Zone = ZoneType.SpellZone;
                            target.FaceDown = true;
                            target.SetThisTurn = false; // darf noch in diesem Zug aktiviert werden
                            Shuffle(player.DeckPile);
                            Log($"{player.Name} sets a spell from the deck (usable this turn) and shuffles.");
                        }
                        break;
                    case EffectActionType.SendSelfToGraveyard:
                        if (IsOnField(source) || source.Zone == ZoneType.Hand)
                        {
                            Log($"{player.Name} sends {source.Name} to the graveyard.");
                            if (presenter != null) yield return presenter.ShowCardSentToGrave(source);
                            MoveToGraveyardWithEquips(source);
                        }
                        break;
                    case EffectActionType.SetTargetFaceDownDefense:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null || hit.FaceDown) continue;
                            hit.Position = BattlePosition.Defense;
                            hit.FaceDown = true;
                            Log($"{hit.Name} is turned face-down into Defense Position.");
                        }
                        break;
                    case EffectActionType.BanishSelf:
                        if (source.Zone != ZoneType.Banished)
                        {
                            Log($"{source.Name} is banished.");
                            if (presenter != null) yield return presenter.ShowCardBanished(source);
                            MoveToBanished(source);
                        }
                        break;
                    case EffectActionType.SpecialSummonTargetFromGraveOrBanish:
                        if (target != null && (target.Zone == ZoneType.Graveyard || target.Zone == ZoneType.Banished) && target.MonsterData != null)
                        {
                            string origin = target.Zone == ZoneType.Graveyard ? "from the graveyard" : "from the banishment";
                            yield return SpecialSummonToField(player, target, origin);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.OpponentRandomToFieldOrDiscard:
                        var oppHand = player.Opponent.Hand;
                        if (oppHand.Count > 0)
                        {
                            var randomCard = oppHand[rng.Next(oppHand.Count)];
                            if (randomCard.MonsterData != null && player.FreeMonsterZones() > 0)
                            {
                                Log($"{player.Name} seizes {randomCard.Name} from the opponent's hand!");
                                yield return SpecialSummonToField(player, randomCard, "from the opponent's hand");
                                if (Result != DuelResult.None) yield break;
                            }
                            else
                            {
                                if (presenter != null) yield return presenter.ShowCardSentToGrave(randomCard);
                                MoveToGraveyard(randomCard);
                                Log($"{player.Opponent.Name} discards {randomCard.Name}.");
                            }
                        }
                        break;
                    case EffectActionType.DestroyAllMonstersExceptType:
                        foreach (var duelist in new[] { player, player.Opponent })
                        {
                            foreach (var monster in duelist.Monsters().ToArray())
                            {
                                if (Result != DuelResult.None) yield break;
                                if (action.useTypeFilter && monster.MonsterData.monsterType == action.typeFilter) continue;
                                if (IsProtectedFromEffectDestruction(monster, player))
                                {
                                    Log($"{monster.Name} is protected and cannot be destroyed by card effects.");
                                    continue;
                                }
                                Log($"{source.Name} destroys {monster.Name}.");
                                yield return DestroyCard(monster);
                            }
                        }
                        break;

                    // ================== NEUE BAUSTEINE ==================

                    case EffectActionType.MillSelf:
                        MillDeck(player, Math.Max(1, action.amount));
                        break;
                    case EffectActionType.MillOpponent:
                        MillDeck(player.Opponent, Math.Max(1, action.amount));
                        break;

                    case EffectActionType.ShuffleTargetIntoDeck:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone == ZoneType.Deck) continue;
                            Log($"{hit.Name} is shuffled into {hit.Owner.Name}'s Deck.");
                            if (presenter != null) yield return presenter.ShowCardSentToGrave(hit);
                            RemoveCardFromItsZone(hit);
                            hit.Zone = ZoneType.Deck;
                            hit.Owner.DeckPile.Add(hit);
                            Shuffle(hit.Owner.DeckPile);
                        }
                        break;

                    case EffectActionType.ShuffleGraveyardIntoDeck:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Graveyard) continue;
                            player.Graveyard.Remove(hit);
                            hit.Zone = ZoneType.Deck;
                            player.DeckPile.Add(hit);
                            Log($"{hit.Name} is shuffled back into the Deck.");
                        }
                        Shuffle(player.DeckPile);
                        break;

                    case EffectActionType.CannotAttackThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.CannotAttackThisTurn = true;
                            Log($"{hit.Name} cannot attack this turn.");
                        }
                        break;

                    case EffectActionType.LockPositionThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.PositionLockedThisTurn = true;
                            Log($"{hit.Name} cannot change its battle position this turn.");
                        }
                        break;

                    case EffectActionType.CannotBeTargetedThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.CannotBeTargetedThisTurn = true;
                            Log($"{hit.Name} cannot be targeted this turn.");
                        }
                        break;

                    case EffectActionType.SwapAtkDefThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.StatsSwappedThisTurn = !hit.StatsSwappedThisTurn;
                            Log($"{hit.Name} swaps ATK and DEF ({hit.CurrentAtk}/{hit.CurrentDef}).");
                        }
                        break;

                    case EffectActionType.TauntThisTurn:
                        if (IsOnField(source))
                        {
                            source.MustBeAttackedThisTurn = true;
                            Log($"{player.Opponent.Name}'s monsters must attack {source.Name} this turn.");
                        }
                        break;

                    case EffectActionType.PreventBattleDamageThisTurn:
                        player.NoBattleDamageThisTurn = true;
                        Log($"{player.Name} takes no battle damage for the rest of this turn.");
                        break;

                    case EffectActionType.ExtraNormalSummon:
                        player.ExtraNormalSummons += Math.Max(1, action.amount);
                        Log($"{player.Name} may Normal Summon {Math.Max(1, action.amount)} additional time(s) this turn.");
                        break;

                    case EffectActionType.OpponentSummonLockThisTurn:
                        player.Opponent.CannotSpecialSummonThisTurn = true;
                        Log($"{player.Opponent.Name} cannot Special Summon for the rest of this turn.");
                        break;

                    case EffectActionType.DiscardFromHandCost:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Hand) continue;
                            Log($"{player.Name} discards {hit.Name}.");
                            player.Hand.Remove(hit);
                            MoveToGraveyard(hit);
                        }
                        break;

                    case EffectActionType.LookAndDiscardChosen:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Hand) continue;
                            Log($"{hit.Owner.Name} is forced to discard {hit.Name}.");
                            hit.Owner.Hand.Remove(hit);
                            MoveToGraveyard(hit);
                        }
                        break;

                    case EffectActionType.CopyTargetStatsThisTurn:
                        if (target != null && target.MonsterData != null && IsOnField(source) && source.MonsterData != null)
                        {
                            source.StatsOverriddenThisTurn = true;
                            source.OverriddenAtk = target.CurrentAtk;
                            source.OverriddenDef = target.CurrentDef;
                            Log($"{source.Name} copies {target.Name}'s stats ({source.CurrentAtk}/{source.CurrentDef}).");
                        }
                        break;

                    case EffectActionType.TakeControlUntilEndOfTurn:
                        yield return TakeControl(player, target);
                        break;

                    case EffectActionType.SummonCopyOfTarget:
                        yield return SummonCopy(player, target);
                        break;
                }
            }
            BoardChanged();
        }

        // ================== HELFER FÜR DIE NEUEN BAUSTEINE ==================

        /// <summary>Oberste Karten eines Decks in den Friedhof. Ein leeres Deck verliert
        /// erst beim nächsten Ziehen — Millen allein beendet das Duell nicht.</summary>
        private void MillDeck(PlayerState player, int count)
        {
            int actual = Math.Min(count, player.DeckPile.Count);
            for (int i = 0; i < actual; i++)
            {
                var card = player.DeckPile[0];
                player.DeckPile.RemoveAt(0);
                MoveToGraveyard(card);
            }
            Log(actual == 0
                ? $"{player.Name}'s Deck is already empty."
                : $"{player.Name} sends the top {actual} card(s) of the Deck to the Graveyard ({player.DeckPile.Count} left).");
        }

        /// <summary>Nimmt eine Karte aus der Zone, in der sie gerade liegt.</summary>
        private void RemoveCardFromItsZone(CardInstance card)
        {
            var owner = card.Owner;
            switch (card.Zone)
            {
                case ZoneType.MonsterZone: RemoveFromZoneArray(owner.MonsterZones, card); break;
                case ZoneType.SpellZone: RemoveFromZoneArray(owner.SpellZones, card); break;
                case ZoneType.ArtifactZone: RemoveFromZoneArray(owner.ArtifactZones, card); break;
                case ZoneType.Hand: owner.Hand.Remove(card); break;
                case ZoneType.Graveyard: owner.Graveyard.Remove(card); break;
                case ZoneType.Banished: owner.Banished.Remove(card); break;
            }
        }

        /// <summary>
        /// Kontrolle über ein gegnerisches Monster bis zur End Phase. Die Rückgabe erledigt
        /// ClearTempModifiers über ControlReturnsTo — auch wenn das Duell vorher endet.
        /// </summary>
        private IEnumerator TakeControl(PlayerState player, CardInstance monster)
        {
            if (monster == null || monster.MonsterData == null || !IsOnField(monster)) yield break;
            if (monster.Owner == player) yield break;
            int free = player.FirstFreeZoneIndex(player.MonsterZones);
            if (free < 0) { Log($"{player.Name} has no free Monster Zone — nothing is taken."); yield break; }

            var from = monster.Owner;
            presenter?.RememberView(monster);

            RemoveFromZoneArray(from.MonsterZones, monster);
            player.MonsterZones[free] = monster;
            monster.Owner = player;
            monster.Zone = ZoneType.MonsterZone;
            monster.ControlReturnsTo = from;
            monster.HasAttackedThisTurn = false;
            Log($"{player.Name} takes control of {monster.Name} until the End Phase.");
            BoardChanged();
            if (presenter != null) yield return presenter.ShowCardMoved(monster);
        }

        /// <summary>Laufzeit-Kopie eines Monsters; verschwindet in der End Phase.</summary>
        private IEnumerator SummonCopy(PlayerState player, CardInstance original)
        {
            if (original == null || original.MonsterData == null) yield break;
            int free = player.FirstFreeZoneIndex(player.MonsterZones);
            if (free < 0) { Log($"{player.Name} has no free Monster Zone — no copy is made."); yield break; }

            var copy = new CardInstance(original.Definition, player)
            {
                Zone = ZoneType.MonsterZone,
                Position = BattlePosition.Attack,
                SummonedThisTurn = true,
                WasSpecialSummoned = true,
                IsTemporaryCopy = true
            };
            player.MonsterZones[free] = copy;
            Log($"{player.Name} Special Summons a copy of {original.Name} ({copy.CurrentAtk}/{copy.CurrentDef}).");
            BoardChanged();
            if (presenter != null) yield return presenter.ShowSummon(copy);
            yield return RunSummonEvents(copy);
        }

        private static string InfusedTag(EffectDefinition effect)
        {
            if (!effect.isInfused) return "";
            return effect.infusedKind == InfusedKind.Coupled ? " [Infused Upgrade]" : " [Infused]";
        }

        private string DescribeActivation(EffectDefinition effect)
        {
            string cost = effect.manaCost > 0 ? $" ({effect.manaCost} Mana)" : "";
            return InfusedTag(effect) + cost;
        }

        /// <summary>Log-Zusatz einer Aktivierung: [Infused]-Tag, explizite Kosten, Chain-Link-Nummer.</summary>
        private string ActivationLogSuffix(EffectDefinition effect)
        {
            string cost = effect.manaCost > 0 ? $" — pays {effect.manaCost} Mana" : "";
            int chainLink = responseDepth + 1;
            string chain = chainLink > 1 ? $" (Chain Link {chainLink})" : "";
            return InfusedTag(effect) + cost + chain;
        }

        public string EffectChoiceLabel(CardInstance card, int index)
        {
            var effect = GetEffect(card, index);
            if (effect == null) return "?";
            string kind = effect.isInfused
                ? (effect.infusedKind == InfusedKind.Coupled ? "Infused Upgrade" : "Infused")
                : "Normal";
            string cost = effect.manaCost > 0 ? $", {effect.manaCost} Mana" : "";
            return $"{effect.label} ({kind}{cost})";
        }

        // ================== TRIGGER & REAKTIONEN ==================

        private IEnumerator OfferTriggeredEffects(PlayerState owner, CardInstance card, EffectTrigger trigger)
        {
            var activatable = ActivatableEffects(card, owner, trigger);
            if (activatable.Count == 0) yield break;

            if (activatable.Count == 1)
            {
                var effect = GetEffect(card, activatable[0]);
                var request = new YesNoRequest
                {
                    Title = "Activate effect?",
                    Card = card,
                    Question = $"{card.Name}: Activate \"{effect.label}\"?{DescribeActivation(effect)}"
                };
                yield return DecideRouted(owner, request);
                if (request.Result) yield return ActivateEffect(owner, card, activatable[0]);
            }
            else
            {
                var request = new OptionRequest
                {
                    Title = $"{card.Name}: choose effect",
                    Card = card,
                    AllowCancel = true
                };
                foreach (int index in activatable) request.Options.Add(EffectChoiceLabel(card, index));
                yield return DecideRouted(owner, request);
                if (request.Result >= 0 && request.Result < activatable.Count)
                    yield return ActivateEffect(owner, card, activatable[request.Result]);
            }
        }

        private IEnumerator ResolvePhaseTriggers(PlayerState player, EffectTrigger trigger)
        {
            foreach (var card in player.FieldCards().ToArray())
            {
                if (Result != DuelResult.None) yield break;
                if (card.Zone == ZoneType.Graveyard || card.Zone == ZoneType.Banished) continue;
                if (card.FaceDown) continue;
                yield return OfferTriggeredEffects(player, card, trigger);
            }
        }

        private IEnumerator OpenResponseWindow(PlayerState firstPriority, string context, CardInstance contextCard, bool isPhaseWindow = false)
        {
            if (responseDepth >= 2) yield break;
            responseDepth++;

            foreach (var responder in new[] { firstPriority, firstPriority.Opponent })
            {
                if (Result != DuelResult.None) break;

                foreach (var (card, effectIndex) in BuildResponseCandidates(responder, context, contextCard))
                {
                    if (Result != DuelResult.None) break;

                    var effect = GetEffect(card, effectIndex);
                    if (effect == null || responder.Mana < effect.manaCost) continue;
                    if (card.OncePerTurnUsed.Contains(effectIndex)) continue;
                    if (!HasValidTargets(effect, responder, card)) continue;

                    var request = new YesNoRequest
                    {
                        Title = isPhaseWindow ? context : $"Response to {context}",
                        Card = card,
                        IsPhaseWindow = isPhaseWindow,
                        Question = $"{card.Name}: Activate \"{effect.label}\" {(isPhaseWindow ? "now?" : "in response?")}{DescribeActivation(effect)}"
                    };
                    yield return DecideRouted(responder, request);
                    if (!request.Result) continue;

                    if (card.SpellData != null && card.Zone == ZoneType.SpellZone)
                        yield return ActivateSpell(responder, card, effectIndex, false);
                    else
                        yield return ActivateEffect(responder, card, effectIndex);
                }
            }

            responseDepth--;
        }

        private List<(CardInstance, int)> BuildResponseCandidates(PlayerState responder, string context, CardInstance contextCard)
        {
            var candidates = new List<(CardInstance, int)>();

            foreach (var spell in responder.SpellsOnField())
            {
                if (spell.SetThisTurn || spell.SpellData == null) continue;
                if (spell.SpellData.speed != SpellSpeed.Quick) continue;
                foreach (int index in ActivatableEffects(spell, responder, EffectTrigger.OnActivate))
                    candidates.Add((spell, index));
            }

            foreach (var card in responder.FieldCards())
            {
                if (card.FaceDown) continue;
                foreach (int index in ActivatableEffects(card, responder, EffectTrigger.Quick))
                    candidates.Add((card, index));

                if (context == "summon" && contextCard != null && contextCard.Owner != responder)
                {
                    foreach (int index in ActivatableEffects(card, responder, EffectTrigger.OnOpponentSummon))
                        candidates.Add((card, index));
                }
            }

            // Karten, die aus der HAND antworten. Bis hierher konnte nur mitreden,
            // was schon auf dem Feld lag — wer stören wollte, musste eine Runde
            // vorher etwas hinlegen und es überleben lassen. HandQuick erlaubt
            // Karten, die ihren ganzen Wert daraus ziehen, dass der Gegner sie
            // nicht kommen sieht.
            //
            // Verdeckte Information bleibt verdeckt: die Kandidaten werden nur
            // für den Antwortenden selbst gebaut, und der Server maskiert die
            // Hand des Gegners ohnehin.
            foreach (var card in responder.Hand)
                foreach (int index in ActivatableEffects(card, responder, EffectTrigger.HandQuick))
                    candidates.Add((card, index));

            return candidates;
        }

        // ================== BATTLE PHASE ==================

        private IEnumerator RunBattlePhase(PlayerState player)
        {
            int safety = 0;
            while (Result == DuelResult.None && safety++ < 50)
            {
                var request = BuildBattleActions(player);
                if (request.Options.Count == 1) break; // nur noch "beenden"

                yield return DecideRouted(player, request);
                if (request.Chosen < 0 || request.Chosen >= request.Options.Count) continue;

                var option = request.Options[request.Chosen];
                if (option.EndBattle) break;

                yield return ResolveAttack(player, option);
                if (CheckWin()) yield break;
                if (presenter != null) yield return DuelWait.For(0.2f); // Beat zwischen Angriffen
            }
        }

        private BattleActionRequest BuildBattleActions(PlayerState player)
        {
            var request = new BattleActionRequest { Title = $"Battle Phase — {player.Name}" };

            // Spott: gibt es Monster, die angegriffen werden MÜSSEN, sind nur die wählbar
            var forcedTargets = player.Opponent.Monsters().Where(m => m.MustBeAttackedThisTurn).ToList();

            foreach (var attacker in player.Monsters())
            {
                if (attacker.Position != BattlePosition.Attack) continue;
                if (attacker.CannotAttackThisTurn) continue;
                if (attacker.HasAttackedThisTurn && attacker.BonusAttacks <= 0) continue;

                if (player.Opponent.MonsterCount() == 0)
                {
                    request.Options.Add(new BattleOption
                    {
                        Attacker = attacker,
                        Direct = true,
                        Label = $"{attacker.Name} ({attacker.CurrentAtk}) attacks directly"
                    });
                }
                else
                {
                    foreach (var target in forcedTargets.Count > 0 ? forcedTargets : player.Opponent.Monsters().ToList())
                    {
                        string targetInfo = target.FaceDown
                            ? "a face-down monster"
                            : $"{target.Name} ({(target.Position == BattlePosition.Attack ? $"ATK {target.CurrentAtk}" : $"DEF {target.CurrentDef}")})";
                        request.Options.Add(new BattleOption
                        {
                            Attacker = attacker,
                            Target = target,
                            Label = $"{attacker.Name} ({attacker.CurrentAtk}) attacks {targetInfo}"
                        });
                    }
                }
            }

            request.Options.Add(new BattleOption { EndBattle = true, Label = "End Battle Phase" });
            return request;
        }

        private IEnumerator ResolveAttack(PlayerState player, BattleOption option)
        {
            var attacker = option.Attacker;
            var target = option.Target;
            if (attacker == null || attacker.Zone != ZoneType.MonsterZone) yield break;
            if (attacker.HasAttackedThisTurn && attacker.BonusAttacks <= 0) yield break;

            if (attacker.HasAttackedThisTurn) attacker.BonusAttacks--;   // Bonus-Angriff verbrauchen
            else attacker.HasAttackedThisTurn = true;
            Log(option.Direct
                ? $"{attacker.Name} declares a direct attack!"
                : $"{attacker.Name} attacks {target.Name}!");
            BoardChanged();

            if (presenter != null) yield return presenter.ShowAttackDeclared(attacker, target, option.Direct);

            
yield return OpenResponseWindow(player.Opponent, "attack", attacker);
            if (Result != DuelResult.None) yield break;
            if (attacker.Zone != ZoneType.MonsterZone)
            {
                Log("The attacker left the field — attack cancelled.");
                yield break;
            }

            if (option.Direct)
            {
                if (player.Opponent.MonsterCount() > 0)
                {
                    Log("Direct attack no longer possible — the opponent controls a monster.");
                    yield break;
                }
                if (presenter != null) yield return presenter.ShowAttackImpact(attacker, null, true);
                DealDamage(player.Opponent, attacker.CurrentAtk, attacker.Name, isBattleDamage: true);
            }
            else
            {
                if (target == null || target.Zone != ZoneType.MonsterZone)
                {
                    Log("The attack target is gone — the attack fizzles.");
                    yield break;
                }
                if (target.FaceDown)
                {
                    target.FaceDown = false;
                    Log($"The face-down monster is flipped face-up: {target.Name}!");
                    BoardChanged();
                    if (responseDepth < 2) // Flip-Effekte feuern auch beim Aufdecken durch einen Angriff
                        yield return OfferTriggeredEffects(target.Owner, target, EffectTrigger.OnFlipFaceUp);
                    if (Result != DuelResult.None) yield break;
                    if (target.Zone != ZoneType.MonsterZone) { Log("The attack target is gone — the attack fizzles."); yield break; }
                }
                if (presenter != null) yield return presenter.ShowAttackImpact(attacker, target, false);

                int attackValue = attacker.CurrentAtk;
                if (target.Position == BattlePosition.Attack)
                {
                    int defenderAtk = target.CurrentAtk;
                    if (attackValue > defenderAtk)
                    {
                        DealDamage(player.Opponent, attackValue - defenderAtk, attacker.Name, isBattleDamage: true);
                        yield return DestroyCard(target);
                    }
                    else if (attackValue < defenderAtk)
                    {
                        DealDamage(player, defenderAtk - attackValue, target.Name, isBattleDamage: true);
                        yield return DestroyCard(attacker);
                    }
                    else
                    {
                        Log("Both monsters destroy each other!");
                        yield return DestroyCard(target);
                        yield return DestroyCard(attacker);
                    }
                }
                else
                {
                    int defenderDef = target.CurrentDef;
                    if (attackValue > defenderDef)
                    {
                        Log($"{target.Name}'s defense is broken.");
                        yield return DestroyCard(target);
                    }
                    else if (attackValue < defenderDef)
                    {
                        DealDamage(player, defenderDef - attackValue, target.Name, isBattleDamage: true);
                        Log($"{attacker.Name} bounces off the defense.");
                    }
                    else
                    {
                        Log("The attack bounces off harmlessly.");
                    }
                }
            }
            BoardChanged();
        }

        private void DealDamage(PlayerState player, int amount, string sourceName, bool isBattleDamage = false)
        {
            if (amount <= 0) return;
            if (isBattleDamage && player.NoBattleDamageThisTurn)
            {
                Log($"{player.Name} takes no battle damage this turn — {amount} damage is prevented.");
                return;
            }
            int before = player.LifePoints;
            player.LifePoints = Math.Max(0, player.LifePoints - amount);
            Log($"{player.Name} takes {amount} damage from {sourceName} ({player.LifePoints} LP).");
            OnLifeChanged?.Invoke(player, player.LifePoints - before);
            BoardChanged();
        }

        // ================== ZONEN-VERSCHIEBUNGEN ==================

        private bool IsOnField(CardInstance card)
        {
            return card != null && (card.Zone == ZoneType.MonsterZone || card.Zone == ZoneType.SpellZone || card.Zone == ZoneType.ArtifactZone);
        }

        private void RemoveFromZoneArray(CardInstance[] zones, CardInstance card)
        {
            for (int i = 0; i < zones.Length; i++)
                if (zones[i] == card) zones[i] = null;
        }

        private void RemoveFromCurrentZone(CardInstance card)
        {
            var owner = card.Owner;
            switch (card.Zone)
            {
                case ZoneType.Hand: owner.Hand.Remove(card); break;
                case ZoneType.Deck: owner.DeckPile.Remove(card); break;
                case ZoneType.Graveyard: owner.Graveyard.Remove(card); break;
                case ZoneType.Banished: owner.Banished.Remove(card); break;
                case ZoneType.MonsterZone: RemoveFromZoneArray(owner.MonsterZones, card); break;
                case ZoneType.SpellZone: RemoveFromZoneArray(owner.SpellZones, card); break;
                case ZoneType.ArtifactZone: RemoveFromZoneArray(owner.ArtifactZones, card); break;
                case ZoneType.ExtraDeck: owner.ExtraDeckPile.Remove(card); break;
            }

            if (card.EquipTarget != null)
            {
                card.EquipTarget.EquippedArtifacts.Remove(card);
                card.EquipTarget = null;
            }
        }

        private void DetachEquipsToGraveyard(CardInstance monster)
        {
            foreach (var artifact in monster.EquippedArtifacts.ToArray())
            {
                Log($"{artifact.Name} is destroyed along with its bearer.");
                MoveToGraveyard(artifact);
            }
            monster.EquippedArtifacts.Clear();
        }

        public void MoveToGraveyard(CardInstance card)
        {
            // Reliquarys landen wie jede andere Karte im Friedhof — nur Hand-Rückgaben
            // schicken sie zurück ins Extra Deck (siehe ReturnToExtraDeck).
            RemoveFromCurrentZone(card);
            if (card.OriginalOwner != null) card.Owner = card.OriginalOwner; // Kontrolle endet — zurück zum Besitzer
            card.FaceDown = false;
            card.Zone = ZoneType.Graveyard;
            card.PermanentAtkBonus = 0;
            card.PermanentDefBonus = 0;
            card.TempAtkBonus = 0;
            card.TempDefBonus = 0;
            card.WasSpecialSummoned = false;
            card.Owner.Graveyard.Add(card);
        }

        private void MoveToGraveyardWithEquips(CardInstance monster)
        {
            DetachEquipsToGraveyard(monster);
            MoveToGraveyard(monster);
        }

        /// <summary>
        /// True, wenn ein Feld-Artefakt des Besitzers dieses Monster vor Zerstörung
        /// durch gegnerische Karteneffekte schützt (z.B. Dragon Claw).
        /// </summary>
        private bool IsProtectedFromEffectDestruction(CardInstance target, PlayerState effectOwner)
        {
            if (target?.MonsterData == null) return false;
            if (target.Owner == effectOwner) return false;
            foreach (var artifact in target.Owner.ArtifactZones)
            {
                var data = artifact?.ArtifactData;
                if (data != null && data.slot == ArtifactSlot.Field && data.protectTypeFromEffectDestruction
                    && target.MonsterData.monsterType == data.protectedType)
                    return true;
            }
            return false;
        }

        private void MoveToBanished(CardInstance card)
        {
            DetachEquipsToGraveyard(card);
            RemoveFromCurrentZone(card);
            if (card.OriginalOwner != null) card.Owner = card.OriginalOwner; // zurück zum Besitzer
            card.FaceDown = false;
            card.Zone = ZoneType.Banished;
            card.PermanentAtkBonus = 0;
            card.PermanentDefBonus = 0;
            card.TempAtkBonus = 0;
            card.TempDefBonus = 0;
            card.WasSpecialSummoned = false;
            card.Owner.Banished.Add(card);
        }

        /// <summary>
        /// Barrierstruck: Ein Schutz-Artefakt des Besitzers kann sich anstelle der Karte zerstören.
        /// Fragt den Besitzer; bei Zustimmung stirbt das Artefakt und die Karte bleibt stehen.
        /// </summary>
        private IEnumerator TryRedirectDestruction(CardInstance card, System.Action<bool> redirected)
        {
            redirected(false);
            if (card == null || card.Owner == null) yield break;

            foreach (var artifact in card.Owner.ArtifactZones.ToArray())
            {
                var data = artifact?.ArtifactData;
                if (data == null || !data.redirectDestructionToSelf) continue;
                if (artifact == card) continue; // schützt nicht sich selbst

                var request = new YesNoRequest
                {
                    Title = "Shield?",
                    Card = artifact,
                    Question = $"Destroy {artifact.Name} instead of {card.Name}?"
                };
                yield return DecideRouted(card.Owner, request);
                if (!request.Result) continue;

                Log($"{artifact.Name} shatters in place of {card.Name}!");
                redirected(true);
                yield return DestroyCard(artifact);
                yield break;
            }
        }

        private IEnumerator DestroyCard(CardInstance card)
        {
            if (card == null || card.Zone == ZoneType.Graveyard) yield break;
            if (card.CannotBeDestroyedThisTurn)
            {
                Log($"{card.Name} cannot be destroyed this turn.");
                yield break;
            }

            bool shielded = false;
            yield return TryRedirectDestruction(card, value => shielded = value);
            if (shielded) yield break;

            bool wasMonster = card.MonsterData != null;
            if (presenter != null) yield return presenter.ShowCardDestroyed(card); // Zersplittern + Flug zum Friedhof
            if (wasMonster) DetachEquipsToGraveyard(card);
            MoveToGraveyard(card);
            Log($"{card.Name} is destroyed.");
            BoardChanged();

            if (wasMonster && responseDepth < 2)
                yield return OfferTriggeredEffects(card.Owner, card, EffectTrigger.OnDestroyedSelf);
        }
    }
}
