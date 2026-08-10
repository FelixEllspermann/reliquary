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
                                     && (tributes > 0 || player.FreeMonsterZones() > 0)
                                     && !FieldLimitReached(player, card.Definition);
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
                    if (monsterData.canSelfSpecialSummon && player.FreeMonsterZones() > 0
                        && !FieldLimitReached(player, card.Definition))
                    {
                        var checkedSide = monsterData.selfSummonChecksOpponentField ? player.Opponent : player;
                        var pool = checkedSide.Monsters().Where(m => !m.FaceDown).ToList();
                        bool nameOk = string.IsNullOrEmpty(monsterData.selfSummonRequiresNameOnField)
                            || NamedOnFieldCount(checkedSide, monsterData.selfSummonRequiresNameOnField)
                               >= Math.Max(1, monsterData.selfSummonRequiredNameCount);
                        bool attributeOk = !monsterData.selfSummonRequiresAttribute
                            || pool.Any(m => m.MonsterData != null && m.MonsterData.attribute == monsterData.selfSummonRequiredAttribute);
                        bool faceDownOk = !monsterData.selfSummonRequiresFaceDownOnField || AnyFaceDownOnField();
                        bool artifactOk = !monsterData.selfSummonRequiresArtifact
                            || player.ArtifactZones.Any(a => a != null);
                        bool foeCountOk = monsterData.selfSummonRequiresOpponentMonsters <= 0
                            || player.Opponent.MonsterCount() >= monsterData.selfSummonRequiresOpponentMonsters;
                        // Deckay: "gemillt diesen oder letzten Zug" (Leech) und
                        // "N+ benannte Karten im Friedhof" (Vulture) als BEDINGUNG
                        bool milledOk = !monsterData.selfSummonRequiresMilled
                            || player.MilledThisTurn || player.MilledLastTurn;
                        // Leerer Namensfilter = ALLE Friedhofskarten zählen (Deckay Glutton)
                        bool graveNamedOk = monsterData.selfSummonRequiresGraveNamedCount <= 0
                            || player.Graveyard.Count(c =>
                                   string.IsNullOrEmpty(monsterData.selfSummonRequiresGraveNamed)
                                   || c.Name.Contains(monsterData.selfSummonRequiresGraveNamed))
                               >= monsterData.selfSummonRequiresGraveNamedCount;
                        if (nameOk && attributeOk && faceDownOk && artifactOk && foeCountOk && milledOk && graveNamedOk)
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
                        // Fallen (AttackResponse/SummonResponse) sind nie offen aus der Hand spielbar
                        if (card.Definition.effects[index].quickWindow != QuickWindow.Any) continue;
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
                    // Fallen warten auf ihr Fenster — in der Main Phase bleiben sie liegen
                    if (spell.Definition.effects[index].quickWindow != QuickWindow.Any) continue;
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

            // Elephant in the Room: Ignition-Effekte auf GEGNERISCHEN Karten, die
            // ausdrücklich beide Spieler ansprechen dürfen. Der Aktivierende zahlt
            // sein Mana und zieht seine Karten — die Once-per-Turn-Sperre liegt
            // auf der Karte selbst und gilt damit für beide gemeinsam.
            foreach (var card in player.Opponent.FieldCards())
            {
                if (card.SpellData != null || card.FaceDown) continue;
                foreach (int index in ActivatableEffects(card, player, EffectTrigger.Ignition))
                {
                    if (!card.Definition.effects[index].eitherPlayerMayActivate) continue;
                    request.Options.Add(new MainActionOption
                    {
                        Kind = MainActionKind.ActivateFieldEffect,
                        Card = card,
                        EffectIndex = index,
                        Label = $"{card.Name} (opponent's): {EffectChoiceLabel(card, index)}"
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
                // Stand-Ins zählen mit (countsAsNameOnField, z.B. Dragon Shrine Replica)
                if (NamedOnFieldCount(player, data.reqNamedOnField) < Math.Max(1, data.reqNamedCount)) return false;
            }
            if (data.reqLifeBelowOpponent && player.LifePoints >= opponent.LifePoints) return false;
            if (data.reqOpponentMoreMonsters && opponent.MonsterCount() <= player.MonsterCount()) return false;
            if (data.reqOpponentMonstersAtLeast > 0 && opponent.MonsterCount() < data.reqOpponentMonstersAtLeast) return false;
            if (!string.IsNullOrEmpty(data.reqOpponentNamedOnField)
                && !opponent.Monsters().Any(m => !m.FaceDown && m.Name.Contains(data.reqOpponentNamedOnField))) return false;
            if (data.reqOwnArtifactsOnField > 0
                && player.ArtifactZones.Count(a => a != null) < data.reqOwnArtifactsOnField) return false;
            if (data.reqOwnArtifactsInGrave > 0
                && player.Graveyard.Count(c => c.ArtifactData != null) < data.reqOwnArtifactsInGrave) return false;
            if (data.reqOwnFaceDownMonsters > 0
                && player.Monsters().Count(m => m.FaceDown) < data.reqOwnFaceDownMonsters) return false;
            if (data.reqMonsterWithEquip && !player.Monsters().Any(m => m.EquippedArtifacts.Count > 0)) return false;
            if (data.reqGraveyardAtLeast > 0 && player.Graveyard.Count < data.reqGraveyardAtLeast) return false;
            if (data.reqGraveyardMonstersAtLeast > 0
                && player.Graveyard.Count(c => c.MonsterData != null) < data.reqGraveyardMonstersAtLeast) return false;
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
            // Tribut-Wert (Twice-Blessed): ein Monster kann als mehrere zählen. Für die
            // Machbarkeit gilt das beste Szenario — der Einzel-Tribut nimmt das geringwertigste.
            var tributeWorths = player.Monsters().Where(m => !m.CannotBeDestroyedThisTurn)
                .Select(TributeWorthOf).OrderBy(w => w).ToList();
            if (data.costTributeOtherMonster)
            {
                if (tributeWorths.Count == 0) return false;
                tributeWorths.RemoveAt(0);
            }
            if (data.costTributeOwnMonsters > 0 && tributeWorths.Sum() < data.costTributeOwnMonsters) return false;
            if (data.costTributeOpponentMonsters > 0
                && player.Opponent.Monsters().Count(m => !m.CannotBeDestroyedThisTurn && !m.CannotBeTargetedThisTurn
                                                         && !IsGuardedFromTargeting(m))
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
                var pool = player.Monsters()
                    .Where(m => !m.CannotBeDestroyedThisTurn && m != tributePick).ToList();
                // Twice-Blessed: zählt ein Kandidat als mehrere Tribute, darf man früher aufhören
                bool worthMatters = pool.Any(m => TributeWorthOf(m) > 1);
                var request = new TargetRequest
                {
                    Title = $"Offer {data.costTributeOwnMonsters} of your monsters to {monster.Name}",
                    Kind = TargetKind.AllyMonster,
                    Count = data.costTributeOwnMonsters,
                    AllowFewer = worthMatters,
                    AllowCancel = true
                };
                request.Candidates.AddRange(pool);
                yield return DecideRouted(player, request);
                if (request.Cancelled) yield break;
                if (request.Result.Sum(TributeWorthOf) < data.costTributeOwnMonsters) yield break;
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
                yield return FireTributeTriggers(tributePick);
                if (Result != DuelResult.None) yield break;
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
                if (!IsOnField(pick)) yield return FireTributeTriggers(pick);
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
                yield return FireTributeTriggers(tribute);
                if (Result != DuelResult.None) yield break;
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
                // ERST das Reaktionsfenster auf die Beschwörung, DANN die Summon-
                // Trigger: wer aufs Summon kontert, tut es, bevor z.B. ein Nibbler
                // seinen Mill auflöst — sonst wirkte der Trigger wie Kosten.
                yield return OpenResponseWindow(monster.Owner.Opponent, "summon", monster);
                if (Result != DuelResult.None) yield break;

                // Hat die Reaktion das Monster entfernt oder verdeckt, verpufft
                // sein Beschwörungs-Trigger.
                if (monster.Zone != ZoneType.MonsterZone || monster.FaceDown) yield break;

                if (wasNormalSummon)
                {
                    yield return OfferTriggeredEffects(monster.Owner, monster, EffectTrigger.OnNormalSummonSelf);
                    if (Result != DuelResult.None) yield break;
                }
                yield return OfferTriggeredEffects(monster.Owner, monster, EffectTrigger.OnSummonSelf);
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
            if (FieldLimitReached(player, monster.Definition))
            {
                Log($"{player.Name} already controls the maximum number of \"{monster.Definition.fieldLimitName}\" monsters — {monster.Name} stays where it is.");
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
            if (effect == null) yield break;
            int manaCost = EffectiveManaCost(player, spell, effect);
            if (player.Mana < manaCost) yield break;

            var targets = new TargetCollection();
            yield return CollectTargets(player, effect, targets, true, spell);
            if (targets.Cancelled) yield break;

            // Aufdecken kommt VOR dem Puls: eine Karte wird gezeigt und aktiviert
            // dann — nicht umgekehrt. Sonst spielt die ganze Animation auf einem
            // Kartenrücken, und der Gegner sieht erst hinterher, was ihn traf.
            spell.FaceDown = false;
            BoardChanged();

            // Aktivierungs-Puls auf der Karte selbst (Hand: mit Dreh, Feld: Blink+Pop),
            // mit Effekt-Panel: das Showcase erklärt, was gerade aktiviert wird
            if (presenter != null) yield return presenter.ShowActivationPulse(spell, fromHand, effect);

            if (manaCost < effect.manaCost)
                Log($"{player.Name}'s first spell this turn is discounted — {manaCost} instead of {effect.manaCost} Mana.");
            player.Mana -= manaCost;
            player.SpellsCastThisTurn++;
            LockEffectForTurn(spell, effectIndex, effect);

            if (fromHand) player.Hand.Remove(spell);
            else RemoveFromZoneArray(player.SpellZones, spell);

            activationSerial++;
            int chainLink = ++chainDepth;
            chainCards.Add(spell);
            Log($"{player.Name} activates {spell.Name}{ActivationLogSuffix(effect)}.");
            if (presenter != null)
                yield return presenter.ShowChainLink(spell, effect.label, player, chainLink);
            BoardChanged();

            // Kosten-Aktionen fallen sofort — noch bevor der Gegner reagieren kann
            yield return ResolveEffectActions(spell, effect, player, targets, costsPhase: true);
            if (Result != DuelResult.None) { yield return CloseChainLink(); yield break; }


            int chainBefore = activationSerial;
            yield return OpenResponseWindow(player.Opponent, "activation", spell);
            if (Result != DuelResult.None) { yield return CloseChainLink(); yield break; }

            if (presenter != null) yield return presenter.ShowChainResolve(spell, chainLink);

            if (spell.EffectsNegated)
            {
                Log($"{spell.Name}'s effect is negated — nothing happens.");
            }
            else
            {
                if (activationSerial != chainBefore) Log($"{spell.Name} resolves.");
                resolvingChain++;
                yield return ResolveEffectActions(spell, effect, player, targets);
                resolvingChain--;
            }
            yield return CloseChainLink();
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

            // Aktivierungs-Puls auf der Karte (Hand-Ignition mit Dreh, Feldkarten mit
            // Blink+Pop) — samt Panel, das den aktivierten Effekt erklärt
            if (presenter != null) yield return presenter.ShowActivationPulse(card, card.Zone == ZoneType.Hand, effect);

            player.Mana -= effect.manaCost;
            LockEffectForTurn(card, effectIndex, effect);

            activationSerial++;
            int chainLink = ++chainDepth;
            chainCards.Add(card);
            Log($"{player.Name} activates {card.Name}: \"{effect.label}\"{ActivationLogSuffix(effect)}.");
            if (presenter != null)
                yield return presenter.ShowChainLink(card, effect.label, player, chainLink);
            BoardChanged();

            // Kosten-Aktionen fallen sofort — noch bevor der Gegner reagieren kann
            yield return ResolveEffectActions(card, effect, player, targets, costsPhase: true);
            if (Result != DuelResult.None) { yield return CloseChainLink(); yield break; }


            int chainBefore = activationSerial;
            yield return OpenResponseWindow(player.Opponent, "activation", card);
            if (Result != DuelResult.None) { yield return CloseChainLink(); yield break; }

            if (presenter != null) yield return presenter.ShowChainResolve(card, chainLink);

            if (card.EffectsNegated)
            {
                Log($"{card.Name}'s effect is negated — nothing happens.");
            }
            else
            {
                if (activationSerial != chainBefore) Log($"{card.Name} resolves.");
                resolvingChain++;
                yield return ResolveEffectActions(card, effect, player, targets);
                resolvingChain--;
            }
            yield return CloseChainLink();
            BoardChanged();
            // Deckay: was diese Aktivierung ins Grab geschickt hat (Discards,
            // Kosten, Feld-Abgänge), meldet sich jetzt — nicht mitten in der Kette.
            yield return FirePendingGraveTriggers();
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
                if (player.Mana < EffectiveManaCost(player, card, effect)) continue;
                if (card.OncePerTurnUsed.Contains(i)) continue;
                if (effect.onlyIfSpecialSummoned && !card.WasSpecialSummoned) continue;
                if (effect.requiresEquippedArtifact && card.EquippedArtifacts.Count == 0) continue;
                if (card.EffectsNegated) continue; // annullierte Karte kann nichts aktivieren
                if (RequiresOpenChain(effect) && chainCards.Count == 0) continue;
                if (!MeetsConditions(effect, player)) continue;
                if (!HasValidTargets(effect, player, card)) continue;
                result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// NegateRestOfChain braucht eine offene Kette: als Glied 1 (etwa im
        /// Beschwörungs-Fenster) gäbe es nichts zu annullieren — der Effekt
        /// wird dann gar nicht erst angeboten.
        /// </summary>
        private static bool RequiresOpenChain(EffectDefinition effect)
        {
            foreach (var action in effect.actions)
                if (action.type == EffectActionType.NegateRestOfChain) return true;
            return false;
        }

        /// <summary>Aktivierungs-Bedingungen des Effekts (minMana, Feld-/Hand-Vergleiche).</summary>
        private static bool MeetsConditions(EffectDefinition effect, PlayerState player)
        {
            if (effect.minMana > 0 && player.Mana < effect.minMana) return false;
            if (effect.minOwnMonsters > 0 && player.MonsterCount() < effect.minOwnMonsters) return false;
            if (effect.minOwnFaceDownMonsters > 0)
            {
                int faceDown = 0;
                foreach (var m in player.MonsterZones) if (m != null && m.FaceDown) faceDown++;
                if (faceDown < effect.minOwnFaceDownMonsters) return false;
            }
            if (effect.minOwnGraveyardCards > 0 && player.Graveyard.Count < effect.minOwnGraveyardCards) return false;
            if (effect.requireOpponentMoreHandCards && player.Opponent.Hand.Count <= player.Hand.Count) return false;
            if (effect.requireOpponentMoreMonsters && player.Opponent.MonsterCount() <= player.MonsterCount()) return false;
            if (effect.requireMilledLastTurn && !player.MilledThisTurn && !player.MilledLastTurn) return false;
            if (effect.minOwnGraveyardNamed > 0)
            {
                int named = 0;
                foreach (var card in player.Graveyard)
                    if (!string.IsNullOrEmpty(effect.graveyardNamedFilter)
                        && card.Name.Contains(effect.graveyardNamedFilter)) named++;
                if (named < effect.minOwnGraveyardNamed) return false;
            }
            return true;
        }

        private bool HasValidTargets(EffectDefinition effect, PlayerState player, CardInstance source = null)
        {
            foreach (var action in effect.actions)
            {
                if (action.target == TargetKind.None || action.target == TargetKind.SelfCard) continue;
                // "Bis zu"-Aktionen sind optional: null Kandidaten blockieren die
                // Aktivierung nicht (Trapline: "then Set 1 ... from your hand").
                if (action.upToTargets) continue;
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
                case TargetKind.EnemySpellOrArtifact:
                    candidates.AddRange(player.Opponent.SpellsOnField());
                    candidates.AddRange(player.Opponent.ArtifactsOnField());
                    break;
                case TargetKind.BanishedMonsterSelf:
                    candidates.AddRange(player.Banished.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.BanishedCardSelf:
                    candidates.AddRange(player.Banished);
                    break;
                case TargetKind.GraveyardCardOpponent:
                    candidates.AddRange(player.Opponent.Graveyard);
                    break;
                case TargetKind.AllySpellOrArtifact:
                    candidates.AddRange(player.SpellsOnField());
                    candidates.AddRange(player.ArtifactsOnField());
                    break;
                case TargetKind.HandSpellFiltered:
                    candidates.AddRange(player.Hand.Where(c => c.SpellData != null));
                    break;
                // TargetKind.SelfCard: kein Auswahl-Dialog — wird in ResolveEffectActions direkt zur Quellkarte.
            }
            if (ActionHasFilter(action)) candidates.RemoveAll(c => !MatchesFilter(action, c));
            if (action.targetExcludesSelf && source != null) candidates.Remove(source);
            // Trapline: "mit anderem Namen" — gleichnamige Karten sind kein Ziel
            if (action.excludeSameName && source != null)
                candidates.RemoveAll(c => c != null && c.Name == source.Name);
            // Ziel-Immunität gilt nur gegen den Gegner — eigene Effekte dürfen weiter anvisieren
            candidates.RemoveAll(c => c != null && (c.CannotBeTargetedThisTurn || c.ImmuneToOpponentThisTurn) && c.Owner != player);
            // Heavenly Bodyguard: benannte Karten sind für den Gegner kein gültiges Ziel
            candidates.RemoveAll(c => c != null && c.Owner != player && IsGuardedFromTargeting(c));
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
                // upTo gilt auch bei Einzelzielen: eine optionale Zusatz-Aktion
                // ("then Set 1 from your hand") darf mit 0 Zielen enden.
                bool upTo = action.upToTargets;
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

                // Deckay-Immunität: gegnerische Effekt-Aktionen prallen bis Zugende
                // ab — zentral HIER, damit jede Aktionsart abgedeckt ist (auch
                // solche, die NACH der Zielwahl markiert wurden).
                int shrugged = affected.RemoveAll(hit =>
                    hit != null && hit.ImmuneToOpponentThisTurn && hit.Owner != player);
                if (shrugged > 0)
                    Log($"{shrugged} card(s) shrug off {source.Name}'s effect (immune this turn).");

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
                        yield return FireOpponentDrawTriggers(player);
                        if (Result != DuelResult.None) yield break;
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
                                // amount > 1 = EOT-ATK-Bonus für die aufgedeckte Karte (Standing Ovation);
                                // amount 1 ist der historische Default alter Assets und heißt "kein Bonus".
                                if (action.amount > 1 && pick.Zone == ZoneType.MonsterZone)
                                {
                                    pick.TempAtkBonus += action.amount;
                                    Log($"{pick.Name} gains +{action.amount} ATK until end of turn ({pick.CurrentAtk}).");
                                }
                            }
                        break;
                    case EffectActionType.SpecialSummonTargetFaceDown:
                        foreach (var pick in affected)
                        {
                            if (pick == null || pick.MonsterData == null
                                || (pick.Zone != ZoneType.Hand && pick.Zone != ZoneType.Deck)) continue;
                            if (FieldLimitReached(player, pick.Definition)) continue; // Snugglet: Sofa voll
                            int fdZone = player.FirstFreeZoneIndex(player.MonsterZones);
                            if (fdZone < 0) { Log("No free monster zone — the set fizzles."); break; }
                            bool fromDeckSet = pick.Zone == ZoneType.Deck;
                            player.Hand.Remove(pick);
                            player.DeckPile.Remove(pick);
                            player.MonsterZones[fdZone] = pick;
                            pick.Owner = player;
                            pick.Zone = ZoneType.MonsterZone;
                            pick.Position = BattlePosition.Defense;
                            pick.FaceDown = true;
                            pick.SummonedThisTurn = true;
                            pick.WasSpecialSummoned = true;
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
                    case EffectActionType.NegateRestOfChain:
                    {
                        // Die äußeren Glieder warten im Aufrufstapel und prüfen
                        // EffectsNegated erst, wenn sie selbst auflösen — markieren
                        // reicht. Das letzte Element ist die Quelle dieses Effekts.
                        int torn = 0;
                        for (int li = 0; li < chainCards.Count - 1; li++)
                        {
                            var link = chainCards[li];
                            if (link == null || link.EffectsNegated) continue;
                            link.EffectsNegated = true;
                            chainNegatedCards.Add(link);
                            torn++;
                            Log($"{source.Name} negates {link.Name}'s activation!");
                        }
                        if (torn == 0) Log($"{source.Name}: no earlier chain links to negate.");
                        BoardChanged();
                        break;
                    }
                    case EffectActionType.AttackAgainSelf:
                        // BonusAttacks ist die native Schiene für Zweitangriffe
                        // (Battle-Checks und Bot-KI kennen sie bereits).
                        if (IsOnField(source) && source.MonsterData != null)
                        {
                            source.BonusAttacks++;
                            Log($"{source.Name} readies another attack!");
                            BoardChanged();
                        }
                        break;

                    case EffectActionType.SummonIllusionTokensToOpponent:
                        yield return SpawnIllusionTokens(player.Opponent, Math.Max(1, action.amount));
                        break;

                    case EffectActionType.DestroyIllusionTokensDrawPer:
                    {
                        // Bis zu amount gegnerische Illusion-Tokens zerstören,
                        // je 1 Karte ziehen — gedeckelt auf targetCount Draws.
                        int destroyed = 0;
                        int maxDestroy = Math.Max(1, action.amount);
                        foreach (var zone in player.Opponent.MonsterZones.ToArray())
                        {
                            if (destroyed >= maxDestroy) break;
                            if (zone == null || zone.Definition == null || !zone.Definition.isToken) continue;
                            yield return DestroyCard(zone);
                            if (Result != DuelResult.None) yield break;
                            destroyed++;
                        }
                        int draws = Math.Min(destroyed, Math.Max(1, action.targetCount));
                        if (destroyed == 0) Log($"{player.Name} finds no Illusion Token to shatter.");
                        else if (draws > 0)
                        {
                            if (!TryDraw(player, draws)) yield break;
                            yield return PresentDraws(player);
                            yield return FireOpponentDrawTriggers(player);
                        }
                        if (Result != DuelResult.None) yield break;
                        break;
                    }

                    case EffectActionType.DestroyAllIllusionTokensDebuffTargetPer:
                    {
                        int shattered = 0;
                        foreach (var duelist in new[] { player, player.Opponent })
                            foreach (var zone in duelist.MonsterZones.ToArray())
                            {
                                if (zone == null || zone.Definition == null || !zone.Definition.isToken) continue;
                                yield return DestroyCard(zone);
                                if (Result != DuelResult.None) yield break;
                                shattered++;
                            }
                        int debuff = shattered * action.amount;
                        if (shattered == 0) Log($"{source.Name}: no Illusion Tokens on the field — nothing happens.");
                        else if (target != null && IsOnField(target) && target.MonsterData != null && debuff > 0)
                        {
                            target.PermanentAtkBonus -= debuff;
                            Log($"{target.Name} loses {debuff} ATK ({shattered} Illusion Token{(shattered == 1 ? "" : "s")} shattered).");
                            BoardChanged();
                        }
                        break;
                    }
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
                        // Schleife statt Einzelziel: "beschwöre bis zu 2 aus dem Friedhof"
                        // belebte vorher stillschweigend nur das erste Ziel wieder.
                        foreach (var pick in affected)
                        {
                            if (pick == null || pick.Zone != ZoneType.Graveyard || pick.MonsterData == null) continue;
                            if (player.CannotSpecialSummonThisTurn)
                            {
                                Log($"{player.Name} cannot Special Summon this turn — {pick.Name} stays in the graveyard.");
                                break;
                            }
                            if (FieldLimitReached(player, pick.Definition)) continue; // Snugglet: Sofa voll
                            int zoneIndex = -1;
                            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                                $"Choose a zone for {pick.Name}", -1, index => zoneIndex = index);
                            if (zoneIndex < 0) { Log("No free monster zone — the special summon fizzles."); break; }
                            pick.Owner.Graveyard.Remove(pick);
                            player.MonsterZones[zoneIndex] = pick;
                            pick.Owner = player;
                            pick.Zone = ZoneType.MonsterZone;
                            pick.Position = BattlePosition.Attack;
                            pick.SummonedThisTurn = true;
                            pick.WasSpecialSummoned = true;
                            pick.PermanentAtkBonus = 0;
                            pick.PermanentDefBonus = 0;
                            pick.TempAtkBonus = 0;
                            pick.TempDefBonus = 0;
                            // amount > 1 = dauerhafter ATK/DEF-Bonus für die Wiederkehr (In Glory);
                            // amount 1 ist der historische Default und heißt "kein Bonus".
                            if (action.amount > 1)
                            {
                                pick.PermanentAtkBonus += action.amount;
                                pick.PermanentDefBonus += action.amount;
                                Log($"{pick.Name} returns in glory (+{action.amount} ATK/DEF).");
                            }
                            Log($"{player.Name} special summons {pick.Name} from the graveyard.");
                            BoardChanged();
                            if (presenter != null) yield return presenter.ShowSummon(pick);
                            yield return RunSummonEvents(pick);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.ReturnTargetToHand:
                        foreach (var hit in affected)
                        {
                            var target2 = hit;
                            if (!IsOnField(target2)) continue;
                            if (DissolveIfToken(target2)) continue; // Illusionen kehren nirgendwohin zurück
                            if (ReturnToExtraDeck(target2)) // Reliquarys kehren ins Extra Deck zurück
                            {
                                yield return FireBounceTriggers(target2, wasMonster: true);
                                if (Result != DuelResult.None) yield break;
                                continue;
                            }
                            Log($"{target2.Name} is returned to the hand.");
                            DetachEquipsToGraveyard(target2);
                            RemoveFromZoneArray(target2.Owner.MonsterZones, target2);
                            if (target2.OriginalOwner != null) target2.Owner = target2.OriginalOwner; // zurück zum Besitzer
                            target2.Zone = ZoneType.Hand;
                            target2.FaceDown = false;
                            target2.WasSpecialSummoned = false;
                            target2.PermanentAtkBonus = 0;
                            target2.PermanentDefBonus = 0;
                            target2.TempAtkBonus = 0;
                            target2.TempDefBonus = 0;
                            target2.Owner.Hand.Add(target2);
                            yield return FireBounceTriggers(target2, wasMonster: true);
                            if (Result != DuelResult.None) yield break;
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
                            yield return FireTributeTriggers(source);
                            if (Result != DuelResult.None) yield break;
                            string from = target.Zone == ZoneType.Graveyard ? "from the graveyard" : "from the hand";
                            yield return SpecialSummonToField(player, target, from);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.SpecialSummonTargetFromHandOrGrave:
                        // Schleife statt Einzelziel — "bis zu 2 aus Hand oder Friedhof" (Snugglet Pile-Up)
                        foreach (var pick in affected)
                        {
                            if (pick == null || pick.MonsterData == null
                                || (pick.Zone != ZoneType.Hand && pick.Zone != ZoneType.Graveyard)) continue;
                            if (player.FreeMonsterZones() <= 0) { Log("No free monster zone — no further summons."); break; }
                            string summonOrigin = pick.Zone == ZoneType.Graveyard ? "from the graveyard" : "from the hand";
                            yield return SpecialSummonToField(player, pick, summonOrigin);
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
                    {
                        int milled = 0;
                        yield return MillDeck(player, Math.Max(1, action.amount), n => milled = n);
                        yield return ApplyMillBurn(player, milled);
                        yield return FirePendingGraveTriggers();
                        break;
                    }
                    case EffectActionType.MillOpponent:
                    {
                        int milled = 0;
                        yield return MillDeck(player.Opponent, Math.Max(1, action.amount), n => milled = n);
                        yield return ApplyMillBurn(player.Opponent, milled);
                        yield return FirePendingGraveTriggers();
                        break;
                    }

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

                    // ================== BATCH AUGUST 2026 ==================

                    case EffectActionType.ReturnTargetCardToHand:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            if (DissolveIfToken(hit)) continue; // Illusionen kehren nirgendwohin zurück
                            bool bouncedMonster = hit.MonsterData != null;
                            if (ReturnToExtraDeck(hit)) // Reliquarys kehren ins Extra Deck zurück
                            {
                                yield return FireBounceTriggers(hit, wasMonster: true);
                                if (Result != DuelResult.None) yield break;
                                continue;
                            }
                            Log($"{hit.Name} is returned to the hand.");
                            if (bouncedMonster) DetachEquipsToGraveyard(hit);
                            RemoveFromCurrentZone(hit);
                            if (hit.OriginalOwner != null) hit.Owner = hit.OriginalOwner; // zurück zum Besitzer
                            hit.Zone = ZoneType.Hand;
                            hit.FaceDown = false;
                            hit.WasSpecialSummoned = false;
                            hit.PermanentAtkBonus = 0;
                            hit.PermanentDefBonus = 0;
                            hit.TempAtkBonus = 0;
                            hit.TempDefBonus = 0;
                            hit.Owner.Hand.Add(hit);
                            yield return FireBounceTriggers(hit, bouncedMonster);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;

                    case EffectActionType.ProtectTargetThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.CannotBeDestroyedThisTurn = true;
                            Log($"{hit.Name} cannot be destroyed this turn.");
                        }
                        break;

                    case EffectActionType.SwitchTargetToDefense:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null || hit.FaceDown) continue;
                            if (hit.Position == BattlePosition.Defense) continue;
                            hit.Position = BattlePosition.Defense;
                            Log($"{hit.Name} is switched to Defense Position.");
                        }
                        break;

                    case EffectActionType.SwitchAllToDefense:
                    {
                        // amount 0 = beide Felder, 1 = nur gegnerische, 2 = nur eigene Monster
                        var sides = action.amount == 1 ? new[] { player.Opponent }
                            : action.amount == 2 ? new[] { player }
                            : new[] { player, player.Opponent };
                        foreach (var duelist in sides)
                            foreach (var m in duelist.Monsters())
                            {
                                if (m.FaceDown || m.Position == BattlePosition.Defense) continue;
                                m.Position = BattlePosition.Defense;
                                Log($"{m.Name} is switched to Defense Position.");
                            }
                        break;
                    }

                    case EffectActionType.DrawUntilMatchOpponentHand:
                    {
                        int deficit = player.Opponent.Hand.Count - player.Hand.Count;
                        int cap = action.amount > 0 ? action.amount : int.MaxValue;
                        int toDraw = Math.Min(deficit, cap);
                        if (toDraw > 0)
                        {
                            if (!TryDraw(player, toDraw)) yield break;
                            yield return PresentDraws(player);
                            yield return FireOpponentDrawTriggers(player);
                            if (Result != DuelResult.None) yield break;
                        }
                        else Log($"{player.Name} already holds at least as many cards — nothing is drawn.");
                        break;
                    }

                    case EffectActionType.ReturnSelfFromGraveToHand:
                        if (source.Zone == ZoneType.Graveyard && !ReturnToExtraDeck(source))
                        {
                            RemoveFromCurrentZone(source);
                            source.Zone = ZoneType.Hand;
                            source.FaceDown = false;
                            source.Owner.Hand.Add(source);
                            Log($"{source.Name} climbs back from the graveyard into {source.Owner.Name}'s hand.");
                        }
                        break;

                    case EffectActionType.SpecialSummonTargetFromGraveFaceDown:
                        foreach (var pick in affected)
                        {
                            if (pick == null || pick.MonsterData == null || pick.Zone != ZoneType.Graveyard) continue;
                            if (player.CannotSpecialSummonThisTurn)
                            {
                                Log($"{player.Name} cannot Special Summon this turn — the monster stays in the graveyard.");
                                break;
                            }
                            if (FieldLimitReached(player, pick.Definition)) continue; // Snugglet: Sofa voll
                            int fdGraveZone = player.FirstFreeZoneIndex(player.MonsterZones);
                            if (fdGraveZone < 0) { Log("No free monster zone — the summon fizzles."); break; }
                            pick.Owner.Graveyard.Remove(pick);
                            player.MonsterZones[fdGraveZone] = pick;
                            pick.Owner = player;
                            pick.Zone = ZoneType.MonsterZone;
                            pick.Position = BattlePosition.Defense;
                            pick.FaceDown = true;
                            pick.SummonedThisTurn = true;
                            pick.WasSpecialSummoned = true;
                            // Kein Name im Log — die Karte liegt wieder verdeckt
                            Log($"{player.Name} Special Summons a monster face-down from the graveyard.");
                            BoardChanged();
                            if (presenter != null) yield return presenter.ShowCardMoved(pick);
                            // Verdeckt = keine offene Beschwörung: keine Summon-Trigger
                        }
                        break;

                    case EffectActionType.MillAndSalvage:
                    {
                        int millCount = Math.Min(Math.Max(1, action.amount), player.DeckPile.Count);
                        var milledCards = new List<CardInstance>();
                        for (int m = 0; m < millCount; m++)
                        {
                            var top = player.DeckPile[0];
                            player.DeckPile.RemoveAt(0);
                            if (presenter != null) yield return presenter.ShowMilled(player, top);
                            MoveToGraveyard(top);
                            milledCards.Add(top);
                        }
                        Log(millCount == 0
                            ? $"{player.Name}'s Deck is already empty."
                            : $"{player.Name} sends the top {millCount} card(s) of the Deck to the Graveyard.");
                        int salvaged = 0;
                        foreach (var milled in milledCards)
                        {
                            if (salvaged >= Math.Max(1, action.targetCount)) break;
                            if (milled.Zone != ZoneType.Graveyard) continue;
                            if (ActionHasFilter(action) && !MatchesFilter(action, milled)) continue;
                            RemoveFromCurrentZone(milled);
                            milled.Zone = ZoneType.Hand;
                            milled.Owner.Hand.Add(milled);
                            Log($"{player.Name} salvages {milled.Name} from the milled cards.");
                            salvaged++;
                        }
                        if (millCount > 0)
                        {
                            player.MilledThisTurn = true;
                            BoardChanged();
                            yield return ApplyMillBurn(player, millCount);
                            yield return FirePendingGraveTriggers();
                        }
                        break;
                    }

                    case EffectActionType.BuffSelfAtkPerCount:
                        if (IsOnField(source) && source.MonsterData != null)
                        {
                            int counted = CountFor(action.countKind, player);
                            int gain = action.amount * counted;
                            if (gain != 0)
                            {
                                source.PermanentAtkBonus += gain;
                                Log($"{source.Name} gains +{gain} ATK ({counted} × {action.amount}) — now {source.CurrentAtk}.");
                            }
                            else Log($"{source.Name} finds nothing to count — no ATK gained.");
                        }
                        break;

                    case EffectActionType.BuffSelfDefPerCount:
                        if (IsOnField(source) && source.MonsterData != null)
                        {
                            int counted = CountFor(action.countKind, player);
                            int gain = action.amount * counted;
                            if (gain != 0)
                            {
                                source.PermanentDefBonus += gain;
                                Log($"{source.Name} gains +{gain} DEF ({counted} × {action.amount}) — now {source.CurrentDef}.");
                            }
                            else Log($"{source.Name} finds nothing to count — no DEF gained.");
                        }
                        break;

                    case EffectActionType.ReturnBanishedToGraveyard:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Banished) continue;
                            RemoveFromCurrentZone(hit);
                            hit.Zone = ZoneType.Graveyard;
                            hit.Owner.Graveyard.Add(hit);
                            Log($"{hit.Name} is returned from banishment to the Graveyard.");
                        }
                        break;

                    case EffectActionType.PlaceTargetArtifactFromGraveyard:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.ArtifactData == null || hit.Zone != ZoneType.Graveyard) continue;
                            int placeZone = player.FirstFreeZoneIndex(player.ArtifactZones);
                            if (placeZone < 0) { Log("No free artifact zone — the placement fizzles."); break; }
                            RemoveFromCurrentZone(hit);
                            player.ArtifactZones[placeZone] = hit;
                            hit.Owner = player;
                            hit.Zone = ZoneType.ArtifactZone;
                            hit.FaceDown = false;
                            Log($"{player.Name} places {hit.Name} from the Graveyard onto the field.");
                            BoardChanged();
                        }
                        break;

                    case EffectActionType.OpponentDraws:
                        // Mandatory Reading: der GEGNER zieht — und füttert damit
                        // jeden OnOpponentDraw-Lauscher des Aktivierenden.
                        if (!TryDraw(player.Opponent, Math.Max(1, action.amount)))
                        {
                            if (Result != DuelResult.None) yield break; // Deckout durch Zwangslektüre
                        }
                        else
                        {
                            Log($"{player.Opponent.Name} is forced to draw {Math.Max(1, action.amount)} card(s).");
                            yield return PresentDraws(player.Opponent);
                            yield return FireOpponentDrawTriggers(player.Opponent);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;

                    case EffectActionType.DestroyAllEnemyAttackMonsters:
                        foreach (var monster in player.Opponent.Monsters().ToArray())
                        {
                            if (Result != DuelResult.None) yield break;
                            if (monster.FaceDown || monster.Position != BattlePosition.Attack) continue;
                            if (IsProtectedFromEffectDestruction(monster, player))
                            {
                                Log($"{monster.Name} is protected and cannot be destroyed by card effects.");
                                continue;
                            }
                            Log($"{source.Name} snaps shut on {monster.Name}!");
                            yield return DestroyCard(monster);
                        }
                        break;

                    case EffectActionType.DestroyTargetAndSameLevelDefense:
                        if (target != null && target.MonsterData != null && IsOnField(target))
                        {
                            int trapLevel = target.MonsterData.level;
                            if (IsProtectedFromEffectDestruction(target, player))
                            {
                                Log($"{target.Name} is protected and cannot be destroyed by card effects.");
                            }
                            else
                            {
                                Log($"{source.Name} destroys {target.Name}.");
                                yield return DestroyCard(target);
                                if (Result != DuelResult.None) yield break;
                                // ... und alle Verteidiger desselben Levels auf BEIDEN Feldern
                                foreach (var duelist in new[] { player, player.Opponent })
                                    foreach (var monster in duelist.Monsters().ToArray())
                                    {
                                        if (Result != DuelResult.None) yield break;
                                        if (monster.Position != BattlePosition.Defense) continue;
                                        if (monster.MonsterData == null || monster.MonsterData.level != trapLevel) continue;
                                        if (IsProtectedFromEffectDestruction(monster, player))
                                        {
                                            Log($"{monster.Name} is protected and cannot be destroyed by card effects.");
                                            continue;
                                        }
                                        Log($"{source.Name} drags {monster.Name} down as well.");
                                        yield return DestroyCard(monster);
                                    }
                            }
                        }
                        break;

                    case EffectActionType.SetTargetSpellFromHand:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.SpellData == null || hit.Zone != ZoneType.Hand) continue;
                            int handSetZone = player.FirstFreeZoneIndex(player.SpellZones);
                            if (handSetZone < 0) { Log("No free spell zone — the set fizzles."); break; }
                            player.Hand.Remove(hit);
                            player.SpellZones[handSetZone] = hit;
                            hit.Owner = player;
                            hit.Zone = ZoneType.SpellZone;
                            hit.FaceDown = true;
                            hit.SetThisTurn = false; // die Falle ist sofort scharf
                            Log($"{player.Name} sets a card from the hand face-down.");
                            BoardChanged();
                        }
                        break;

                    // ================== DECKAY ==================

                    case EffectActionType.ImmuneTargetThisTurn:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone == ZoneType.Graveyard) continue;
                            hit.ImmuneToOpponentThisTurn = true;
                            hit.CannotBeTargetedThisTurn = true;
                            Log($"{hit.Name} cannot be targeted and is unaffected by the opponent's effects this turn.");
                        }
                        break;

                    case EffectActionType.SummonReliquaryFromExtraSuppressed:
                    {
                        // Vulture-Konter: Reliquary aus dem Extra Deck OHNE Bedingungen,
                        // aber die Mana-Beschwörungskosten fallen an. Kein On-Summon-
                        // Effekt; die Karte fällt in der eigenen End Phase ins Grab.
                        var options = player.ExtraDeckPile
                            .Where(r => r?.MonsterData is ReliquaryCardData rd && player.Mana >= rd.summonManaCost)
                            .ToList();
                        if (options.Count == 0) { Log("No Reliquary in the Extra Deck can answer the call."); break; }
                        int freeZone = player.FirstFreeZoneIndex(player.MonsterZones);
                        if (freeZone < 0) { Log("No free Monster Zone — the call fizzles."); break; }

                        var pick = new OptionRequest { Title = "Summon which Reliquary?", Card = source, AllowCancel = true };
                        foreach (var r in options)
                            pick.Options.Add($"{r.Name} ({((ReliquaryCardData)r.MonsterData).summonManaCost} Mana)");
                        yield return DecideRouted(player, pick);
                        if (pick.Result < 0 || pick.Result >= options.Count) break;

                        var reliquary = options[pick.Result];
                        var relData = (ReliquaryCardData)reliquary.MonsterData;
                        player.Mana -= relData.summonManaCost;
                        player.ExtraDeckPile.Remove(reliquary);
                        player.MonsterZones[freeZone] = reliquary;
                        reliquary.Zone = ZoneType.MonsterZone;
                        reliquary.Owner = player;
                        reliquary.FaceDown = false;
                        reliquary.Position = BattlePosition.Attack;
                        reliquary.WasSpecialSummoned = true;
                        reliquary.SummonedThisTurn = true;
                        reliquary.TempReliquaryUntilEndPhase = true;
                        Log($"{player.Name} calls {reliquary.Name} from the Extra Deck in response — it will not survive the next End Phase.");
                        if (presenter != null) yield return presenter.ShowSummon(reliquary);
                        BoardChanged();
                        break;
                    }

                    case EffectActionType.DestroyAllOthersSelfDamagePer:
                    {
                        // King of Deckay: alles außer der Quellkarte fällt; jeder
                        // Fall kostet den Aktivierenden amount LP.
                        var doomed = new List<CardInstance>();
                        foreach (var side in new[] { player, player.Opponent })
                        {
                            foreach (var m in side.MonsterZones) if (m != null && m != source) doomed.Add(m);
                            foreach (var s in side.SpellZones) if (s != null && s != source) doomed.Add(s);
                            foreach (var a in side.ArtifactZones) if (a != null && a != source) doomed.Add(a);
                        }
                        int fallen = 0;
                        foreach (var victim in doomed)
                        {
                            if (Result != DuelResult.None) yield break;
                            if (victim.Zone == ZoneType.Graveyard) continue;   // schon mitgerissen (Equips)
                            if (victim.ImmuneToOpponentThisTurn && victim.Owner != player) continue;
                            yield return DestroyCard(victim);
                            fallen++;
                        }
                        if (fallen > 0 && action.amount > 0)
                        {
                            int selfDamage = fallen * action.amount;
                            player.LifePoints -= selfDamage;
                            Log($"{player.Name} takes {selfDamage} damage ({fallen} × {action.amount}) for the devastation.");
                            OnLifeChanged?.Invoke(player, -selfDamage);
                            BoardChanged();
                        }
                        break;
                    }

                    case EffectActionType.MoveTargetArtifactToStrongestMonster:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.ArtifactData == null || hit.Zone != ZoneType.ArtifactZone) continue;
                            // Neuer Träger: das eigene Monster mit dem höchsten ATK (deterministisch,
                            // ohne zweiten Ziel-Dialog — der Star bekommt das Kostüm).
                            CardInstance bearer = null;
                            foreach (var m in player.Monsters())
                            {
                                if (m.FaceDown || m == hit.EquipTarget) continue;
                                if (bearer == null || m.CurrentAtk > bearer.CurrentAtk) bearer = m;
                            }
                            if (bearer == null) { Log("No other monster to carry the artifact — nothing moves."); break; }
                            if (hit.EquipTarget != null) hit.EquipTarget.EquippedArtifacts.Remove(hit);
                            hit.EquipTarget = bearer;
                            bearer.EquippedArtifacts.Add(hit);
                            Log($"{hit.Name} is refitted onto {bearer.Name}.");
                            if (action.amount > 0)
                            {
                                bearer.TempAtkBonus += action.amount;
                                Log($"{bearer.Name} gains +{action.amount} ATK until end of turn ({bearer.CurrentAtk}).");
                            }
                            BoardChanged();
                        }
                        break;

                    case EffectActionType.SendTargetFromDeckToGraveyard:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Deck) continue;
                            player.DeckPile.Remove(hit);
                            MoveToGraveyard(hit);
                            Log($"{player.Name} sends {hit.Name} from the Deck to the Graveyard.");
                        }
                        Shuffle(player.DeckPile);
                        break;

                    case EffectActionType.BuffTargetAtkPerCountEot:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            int countedEot = CountFor(action.countKind, player);
                            int gainEot = action.amount * countedEot;
                            if (gainEot == 0) { Log($"{hit.Name} finds nothing to count — no ATK gained."); continue; }
                            hit.TempAtkBonus += gainEot;
                            Log($"{hit.Name} gains +{gainEot} ATK ({countedEot} × {action.amount}) until end of turn ({hit.CurrentAtk}).");
                        }
                        break;

                    case EffectActionType.BuffTargetAtkPerCountPermanent:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            int countedPerm = CountFor(action.countKind, player);
                            int gainPerm = action.amount * countedPerm;
                            if (gainPerm == 0) { Log($"{hit.Name} finds nothing to count — no ATK gained."); continue; }
                            hit.PermanentAtkBonus += gainPerm;
                            Log($"{hit.Name} permanently gains +{gainPerm} ATK ({countedPerm} × {action.amount}) — now {hit.CurrentAtk}.");
                        }
                        break;

                    case EffectActionType.RevealTopMayBottom:
                    {
                        if (player.DeckPile.Count == 0) { Log($"{player.Name}'s Deck is empty."); break; }
                        var topCard = player.DeckPile[0];
                        Log($"{player.Name} reveals the top card of the Deck: {topCard.Name}.");
                        var peek = new YesNoRequest
                        {
                            Title = "Top of the Deck",
                            Card = topCard,
                            Question = $"Top card: {topCard.Name}. Put it on the bottom of the Deck?"
                        };
                        yield return DecideRouted(player, peek);
                        if (peek.Result)
                        {
                            player.DeckPile.RemoveAt(0);
                            player.DeckPile.Add(topCard);
                            Log($"{player.Name} moves the revealed card to the bottom of the Deck.");
                        }
                        else Log("The revealed card stays on top of the Deck.");
                        break;
                    }
                }
            }
            BoardChanged();
        }

        // ================== HELFER FÜR DIE NEUEN BAUSTEINE ==================

        /// <summary>Oberste Karten eines Decks in den Friedhof. Ein leeres Deck verliert
        /// erst beim nächsten Ziehen — Millen allein beendet das Duell nicht.</summary>
        private IEnumerator MillDeck(PlayerState player, int count, Action<int> onMilled = null)
        {
            int actual = Math.Min(count, player.DeckPile.Count);
            for (int i = 0; i < actual; i++)
            {
                var card = player.DeckPile[0];
                player.DeckPile.RemoveAt(0);
                // Aufdecken auf der Deck-Zone, kurz liegen lassen, dann der Flug —
                // erst danach wandert die Karte wirklich in den Friedhof
                if (presenter != null) yield return presenter.ShowMilled(player, card);
                MoveToGraveyard(card);
                BoardChanged();
            }
            if (actual > 0) player.MilledThisTurn = true;   // Deckay: "hast du gemillt?"
            Log(actual == 0
                ? $"{player.Name}'s Deck is already empty."
                : $"{player.Name} sends the top {actual} card(s) of the Deck to the Graveyard ({player.DeckPile.Count} left).");
            onMilled?.Invoke(actual);
        }

        /// <summary>
        /// King of Deckay: liegt beim Besitzer des gemillten Decks ein offenes
        /// Monster mit Mill-Brand, brennt jeder Mill-VORGANG (nicht jede Karte)
        /// den Gegner. Nach dem Schaden entscheidet CheckWin der Aufrufer.
        /// </summary>
        private IEnumerator ApplyMillBurn(PlayerState miller, int milledCount)
        {
            if (milledCount <= 0) yield break;
            foreach (var monster in miller.Monsters().ToArray())
            {
                if (monster.FaceDown || monster.Definition == null) continue;
                int burn = monster.Definition.passiveBurnPerMill;
                if (burn <= 0 || monster.EffectsNegated) continue;
                miller.Opponent.LifePoints -= burn;
                Log($"{monster.Name} sears {miller.Opponent.Name} for {burn} (mill).");
                if (presenter != null) yield return presenter.ShowActivationPulse(monster, false);
                BoardChanged();
            }
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

        // ================== HELFER: BATCH AUGUST 2026 ==================

        /// <summary>
        /// Manakosten unter Berücksichtigung des Erster-Zauber-Rabatts (Bargain Bobbin):
        /// Der erste ausgespielte Zauber des Spielers pro Zug wird um den höchsten
        /// firstSpellDiscountPerTurn-Wert seiner Feld-Artefakte billiger.
        /// </summary>
        private int EffectiveManaCost(PlayerState player, CardInstance card, EffectDefinition effect)
        {
            int cost = effect.manaCost;
            if (cost <= 0) return cost;
            if (card?.SpellData == null || effect.trigger != EffectTrigger.OnActivate) return cost;
            if (player.SpellsCastThisTurn > 0) return cost;
            int discount = 0;
            foreach (var artifact in player.ArtifactZones)
            {
                var data = artifact?.ArtifactData;
                if (data != null && data.firstSpellDiscountPerTurn > discount)
                    discount = data.firstSpellDiscountPerTurn;
            }
            return Math.Max(0, cost - discount);
        }

        /// <summary>Wie viele Tribute dieses Monster wert ist (Twice-Blessed: 2).</summary>
        private static int TributeWorthOf(CardInstance monster) =>
            monster?.Definition != null ? Math.Max(1, monster.Definition.tributeWorth) : 1;

        /// <summary>
        /// Feld-Limit (Snugglet): true, wenn diese Karte wegen "max N gleichnamige"
        /// gerade NICHT beschworen/gesetzt werden darf.
        /// </summary>
        private static bool FieldLimitReached(PlayerState player, CardDefinition definition)
        {
            if (definition == null || definition.fieldLimitCount <= 0
                || string.IsNullOrEmpty(definition.fieldLimitName)) return false;
            int named = 0;
            foreach (var monster in player.Monsters())
                if (monster.Name.Contains(definition.fieldLimitName)) named++;
            return named >= definition.fieldLimitCount;
        }

        /// <summary>
        /// Offene Monster mit passendem Namen auf einem Feld — plus Artefakte, die per
        /// countsAsNameOnField als solches Monster gelten (Dragon Shrine Replica).
        /// </summary>
        private static int NamedOnFieldCount(PlayerState side, string namePart)
        {
            int count = 0;
            foreach (var monster in side.Monsters())
                if (!monster.FaceDown && monster.Name.Contains(namePart)) count++;
            foreach (var artifact in side.ArtifactZones)
            {
                var data = artifact?.ArtifactData;
                if (data != null && !string.IsNullOrEmpty(data.countsAsNameOnField)
                    && data.countsAsNameOnField.Contains(namePart)) count++;
            }
            return count;
        }

        /// <summary>Zählbasis der ...PerCount-Aktionen — geteilt mit CardInstance.PerCountBonus.</summary>
        private static int CountFor(EffectCountKind kind, PlayerState player) => CardInstance.CountOn(player, kind);

        /// <summary>Ironclad: im Kampf unzerstörbar, solange der Besitzer genug Artefakte kontrolliert.</summary>
        private static bool BattleShieldHolds(CardInstance monster)
        {
            int needed = monster?.Definition != null ? monster.Definition.battleShieldMinOwnArtifacts : 0;
            if (needed <= 0 || monster.Owner == null) return false;
            int artifacts = 0;
            foreach (var artifact in monster.Owner.ArtifactZones) if (artifact != null) artifacts++;
            return artifacts >= needed;
        }

        /// <summary>
        /// Bedingter Zweitangriff (conditionalDoubleAttack): bereit, wenn der erste
        /// Angriff verbraucht ist, kein Bonus-Angriff mehr offen ist (== 0; nach dem
        /// Zweitangriff steht der Zähler auf -1) und ein ANDERES offenes eigenes
        /// Monster des geforderten Attributs liegt.
        /// </summary>
        private static bool ConditionalSecondAttackReady(PlayerState player, CardInstance attacker)
        {
            var definition = attacker?.Definition;
            if (definition == null || !definition.conditionalDoubleAttack) return false;
            if (!attacker.HasAttackedThisTurn || attacker.BonusAttacks != 0) return false;
            foreach (var ally in player.Monsters())
                if (ally != attacker && !ally.FaceDown && ally.MonsterData != null
                    && ally.MonsterData.attribute == definition.doubleAttackAttribute) return true;
            return false;
        }

        /// <summary>
        /// Lyria Green Room: eigene verdeckte Monster sind kein Angriffsziel, solange ein
        /// offenes eigenes Monster mit dem eingestellten Namensteil auf dem Feld liegt.
        /// </summary>
        private static bool FaceDownShieldedFromAttack(CardInstance target)
        {
            if (target == null || !target.FaceDown || target.Owner == null) return false;
            foreach (var artifact in target.Owner.ArtifactZones)
            {
                var data = artifact?.ArtifactData;
                if (data == null || string.IsNullOrEmpty(data.protectsFaceDownWhileNamedFaceUp)) continue;
                foreach (var monster in target.Owner.Monsters())
                    if (!monster.FaceDown && monster.Name.Contains(data.protectsFaceDownWhileNamedFaceUp))
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Heavenly Bodyguard: eine offene Feldkarte des Besitzers mit gesetztem
        /// protectsNamedFromTargeting macht benannte Karten für den Gegner unanvisierbar.
        /// Der Beschützer schützt dabei nie sich selbst — sonst wäre er unangreifbar.
        /// </summary>
        private static bool IsGuardedFromTargeting(CardInstance card)
        {
            if (card?.Owner == null) return false;
            foreach (var guard in card.Owner.FieldCards())
            {
                if (guard == card || guard.FaceDown || guard.Definition == null) continue;
                string guarded = guard.Definition.protectsNamedFromTargeting;
                if (!string.IsNullOrEmpty(guarded) && card.Name.Contains(guarded)) return true;
            }
            return false;
        }

        /// <summary>
        /// Kandidaten für Ereignis-Trigger (Tribut/Bounce): offene Feldkarten, gesetzte
        /// aktivierbare Zauber und die Handkarten des Spielers.
        /// </summary>
        private List<CardInstance> TriggerScanCandidates(PlayerState player)
        {
            var list = new List<CardInstance>();
            foreach (var card in player.FieldCards())
                if (!card.FaceDown || (card.SpellData != null && card.Zone == ZoneType.SpellZone && !card.SetThisTurn))
                    list.Add(card);
            list.AddRange(player.Hand);
            return list;
        }

        /// <summary>
        /// Tribut-Trigger: erst die getributete Karte selbst (Willing Lamb, OnTributedSelf),
        /// dann Feld/Hand ihres Besitzers (Blood Dividend, OnOwnMonsterTributed).
        /// </summary>
        // ---- Deckay: Friedhofs-Ankunfts-Trigger ----
        // MoveToGraveyard ist synchron und läuft mitten in Auflösungen — die
        // Trigger sammeln sich deshalb hier und feuern an der nächsten Naht
        // (Ende einer Aktivierung, nach Zerstörungen, nach dem Handlimit).
        private readonly List<(CardInstance card, ZoneType fromZone)> pendingGraveTriggers
            = new List<(CardInstance, ZoneType)>();
        private bool firingGraveTriggers;

        private static bool HasGraveArrivalTrigger(CardInstance card)
        {
            if (card?.Definition?.effects == null) return false;
            foreach (var effect in card.Definition.effects)
                if (effect != null && (effect.trigger == EffectTrigger.OnMilledSelf
                    || effect.trigger == EffectTrigger.OnDiscardedOrMilledSelf
                    || effect.trigger == EffectTrigger.OnSentToGraveyardSelf))
                    return true;
            return false;
        }

        /// <summary>
        /// Arbeitet alle gesammelten Friedhofs-Trigger ab. Effekte können dabei
        /// weitere Karten ins Grab schicken — die Schleife läuft, bis nichts
        /// mehr ansteht; der Guard verhindert eine Verschachtelung in sich selbst.
        /// </summary>
        private IEnumerator FirePendingGraveTriggers()
        {
            if (firingGraveTriggers || pendingGraveTriggers.Count == 0) yield break;
            firingGraveTriggers = true;
            int safety = 40;   // gegen pathologische Endlos-Ketten
            while (pendingGraveTriggers.Count > 0 && safety-- > 0 && Result == DuelResult.None)
            {
                var (card, fromZone) = pendingGraveTriggers[0];
                pendingGraveTriggers.RemoveAt(0);
                if (card == null || card.Zone != ZoneType.Graveyard) continue;   // schon weitergewandert
                var owner = card.Owner;

                if (fromZone == ZoneType.Deck)
                    yield return OfferTriggeredEffects(owner, card, EffectTrigger.OnMilledSelf);
                if (fromZone == ZoneType.Deck || fromZone == ZoneType.Hand)
                    yield return OfferTriggeredEffects(owner, card, EffectTrigger.OnDiscardedOrMilledSelf);
                yield return OfferTriggeredEffects(owner, card, EffectTrigger.OnSentToGraveyardSelf);
            }
            pendingGraveTriggers.Clear();
            firingGraveTriggers = false;
        }

        private IEnumerator FireTributeTriggers(CardInstance tributed)
        {
            if (tributed == null || responseDepth >= 2) yield break;
            var owner = tributed.Owner; // nach der Zahlung wieder der ursprüngliche Besitzer
            if (owner == null) yield break;

            yield return OfferTriggeredEffects(owner, tributed, EffectTrigger.OnTributedSelf);
            if (Result != DuelResult.None) yield break;

            foreach (var card in TriggerScanCandidates(owner).ToArray())
            {
                if (Result != DuelResult.None) yield break;
                yield return OfferTriggeredEffects(owner, card, EffectTrigger.OnOwnMonsterTributed);
            }
        }

        /// <summary>
        /// Redactor: der Gegner des Ziehenden hört mit, wenn AUSSERHALB der Draw Phase
        /// gezogen wird (nur Standby/Main — der normale Rundenzug bleibt straffrei).
        /// Der Reentry-Guard verhindert, dass Draw-Trigger, die selbst ziehen
        /// (Archivist), eine Endloskette aus gegenseitigen Triggern starten.
        /// </summary>
        private bool firingDrawTriggers;
        private IEnumerator FireOpponentDrawTriggers(PlayerState drawer)
        {
            if (firingDrawTriggers || drawer == null || responseDepth >= 2) yield break;
            if (Phase != DuelPhase.Standby && Phase != DuelPhase.Main) yield break;
            var listener = drawer.Opponent;
            if (listener == null) yield break;

            firingDrawTriggers = true;
            foreach (var card in TriggerScanCandidates(listener).ToArray())
            {
                if (Result != DuelResult.None) break;
                yield return OfferTriggeredEffects(listener, card, EffectTrigger.OnOpponentDraw);
            }
            firingDrawTriggers = false;
        }

        /// <summary>
        /// Extra Reach: der Angreifer hat ein Monster im Kampf zerstört — Trigger auf
        /// ihm selbst und auf seinen ausgerüsteten Artefakten anbieten.
        /// </summary>
        private IEnumerator FireBearerKillTriggers(CardInstance attacker)
        {
            if (attacker == null || responseDepth >= 2 || attacker.Zone != ZoneType.MonsterZone) yield break;
            var owner = attacker.Owner;
            yield return OfferTriggeredEffects(owner, attacker, EffectTrigger.OnBearerBattleKill);
            if (Result != DuelResult.None) yield break;
            foreach (var equip in attacker.EquippedArtifacts.ToArray())
            {
                if (Result != DuelResult.None) yield break;
                yield return OfferTriggeredEffects(owner, equip, EffectTrigger.OnBearerBattleKill);
            }
        }

        /// <summary>
        /// Bounce-Trigger: kehrt eine Feldkarte auf die Hand (oder ins Extra Deck) zurück,
        /// hört ihr Besitzer OnOwnMonsterBounced (Nest Egg) und dessen Gegner
        /// OnEnemyCardBounced (Finders Keepers) — Feld, gesetzte Zauber und Hand.
        /// </summary>
        private IEnumerator FireBounceTriggers(CardInstance bounced, bool wasMonster)
        {
            if (bounced == null || responseDepth >= 2) yield break;
            var owner = bounced.Owner;
            if (owner == null || owner.Opponent == null) yield break;

            if (wasMonster)
            {
                foreach (var card in TriggerScanCandidates(owner).ToArray())
                {
                    if (Result != DuelResult.None) yield break;
                    yield return OfferTriggeredEffects(owner, card, EffectTrigger.OnOwnMonsterBounced);
                }
            }

            foreach (var card in TriggerScanCandidates(owner.Opponent).ToArray())
            {
                if (Result != DuelResult.None) yield break;
                yield return OfferTriggeredEffects(owner.Opponent, card, EffectTrigger.OnEnemyCardBounced);
            }
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

        /// <summary>
        /// Verlaesst ein Glied und schliesst die Anzeige, wenn es das AEUSSERSTE war.
        ///
        /// Die Engine fuehrt keine Kette als Liste: eine Aktivierung ruft ueber
        /// das Reaktionsfenster die naechste auf, und die Reihenfolge steckt
        /// allein im Aufrufstapel. Gezaehlt wird deshalb chainDepth und NICHT
        /// responseDepth — ein Trigger, der mitten in einer Auflösung feuert,
        /// ruft ActivateEffect erneut auf, ohne je durch ein Fenster zu gehen.
        /// Wer auf responseDepth schaut, haelt so eine Verschachtelung faelsch-
        /// licherweise fuer die unterste Ebene und schliesst die Anzeige,
        /// waehrend die aeussere Aktivierung noch laeuft.
        /// </summary>
        private IEnumerator CloseChainLink()
        {
            if (chainDepth > 0) chainDepth--;
            if (chainCards.Count > 0) chainCards.RemoveAt(chainCards.Count - 1);
            if (chainDepth == 0)
            {
                // NegateRestOfChain traf die GLIEDER, nicht die Karten: nach der
                // Kette ist eine so annullierte Karte wieder voll funktionsfähig.
                // Muss VOR FlushPendingOffers passieren, sonst starten die
                // nachgeholten Trigger mit fälschlich annullierten Karten.
                foreach (var negated in chainNegatedCards)
                    if (negated != null) negated.EffectsNegated = false;
                chainNegatedCards.Clear();
                chainCards.Clear();
                if (presenter != null) yield return presenter.ShowChainEnd();
                yield return FlushPendingOffers();
            }
        }

        /// <summary>
        /// Stellt die während der Auflösung zurückgestellten Trigger-Fragen.
        /// Antworten können neue Ketten starten — deren CloseChainLink flusht
        /// dann erneut, der Guard verhindert nur die Selbst-Verschachtelung.
        /// </summary>
        private IEnumerator FlushPendingOffers()
        {
            if (flushingOffers || pendingOffers.Count == 0) yield break;
            flushingOffers = true;
            int safety = 0;
            try
            {
                while (pendingOffers.Count > 0 && safety++ < 40 && Result == DuelResult.None)
                {
                    var (owner, card, trigger) = pendingOffers[0];
                    pendingOffers.RemoveAt(0);
                    if (card == null) continue;
                    // Friedhofs-Trigger (OnMilledSelf & Co.) ERWARTEN die Karte im
                    // Friedhof — nur Feld-Trigger verfallen, wenn die Karte weg ist.
                    // Auch Zerstörungs- und Tribut-Trigger gehören dazu: die Karte
                    // liegt beim Nachholen zwangsläufig im Grab (Apocrypha Hydra).
                    bool graveTrigger = trigger == EffectTrigger.OnMilledSelf
                        || trigger == EffectTrigger.OnDiscardedOrMilledSelf
                        || trigger == EffectTrigger.OnSentToGraveyardSelf
                        || trigger == EffectTrigger.OnDestroyedSelf
                        || trigger == EffectTrigger.OnTributedSelf;
                    if (graveTrigger && card.Zone != ZoneType.Graveyard) continue;
                    if (!graveTrigger && (card.Zone == ZoneType.Graveyard || card.Zone == ZoneType.Banished))
                        continue;
                    yield return OfferTriggeredEffects(owner, card, trigger);
                }
            }
            finally
            {
                flushingOffers = false;
                pendingOffers.Clear();
            }
        }

        /// <summary>Log-Zusatz einer Aktivierung: [Infused]-Tag, explizite Kosten, Chain-Link-Nummer.</summary>
        private string ActivationLogSuffix(EffectDefinition effect)
        {
            string cost = effect.manaCost > 0 ? $" — pays {effect.manaCost} Mana" : "";
            string chain = chainDepth > 1 ? $" (Chain Link {chainDepth})" : "";
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

        /// <summary>
        /// Trigger-Fragen, die WÄHREND einer Ketten-Auflösung anfielen. In den
        /// Abbau grätscht niemand — auch nicht mit einer "Aktivieren?"-Frage.
        /// Die Naht (Kette komplett aufgelöst, chainDepth == 0) holt sie nach.
        /// </summary>
        private readonly List<(PlayerState owner, CardInstance card, EffectTrigger trigger)> pendingOffers
            = new List<(PlayerState, CardInstance, EffectTrigger)>();
        private bool flushingOffers;

        private IEnumerator OfferTriggeredEffects(PlayerState owner, CardInstance card, EffectTrigger trigger)
        {
            var activatable = ActivatableEffects(card, owner, trigger);
            if (activatable.Count == 0) yield break;

            // PFLICHT-Effekte (Deckay) feuern ohne Nachfrage — der Reihe nach,
            // bevor die freiwilligen ihre Frage stellen.
            for (int i = activatable.Count - 1; i >= 0; i--)
            {
                var forced = GetEffect(card, activatable[i]);
                if (forced == null || !forced.mandatory) continue;
                int index = activatable[i];
                activatable.RemoveAt(i);
                Log($"{card.Name}: \"{forced.label}\" activates (mandatory).");
                yield return ActivateTriggered(owner, card, index);
                if (Result != DuelResult.None) yield break;
            }
            if (activatable.Count == 0) yield break;

            // Freiwillige Angebote unterbrechen keinen Ketten-Abbau: vormerken,
            // FlushPendingOffers stellt die Frage nach der Auflösung.
            if (resolvingChain > 0)
            {
                if (!pendingOffers.Exists(p => p.card == card && p.trigger == trigger))
                    pendingOffers.Add((owner, card, trigger));
                yield break;
            }

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
                if (request.Result) yield return ActivateTriggered(owner, card, activatable[0]);
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
                    yield return ActivateTriggered(owner, card, activatable[request.Result]);
            }
        }

        /// <summary>
        /// Führt einen getriggerten Effekt aus. Zauber in der Zauberzone laufen über
        /// ActivateSpell (Aufdecken + Friedhof danach) — alles andere über ActivateEffect.
        /// Bisher konnte kein gesetzter Zauber per Ereignis-Trigger feuern; mit den
        /// Tribut-/Bounce-Triggern können sie es, und ohne diese Weiche bliebe die
        /// aufgelöste Karte für immer offen auf dem Feld liegen.
        /// </summary>
        private IEnumerator ActivateTriggered(PlayerState owner, CardInstance card, int effectIndex)
        {
            if (card.SpellData != null && card.Zone == ZoneType.SpellZone)
                yield return ActivateSpell(owner, card, effectIndex, false);
            else
                yield return ActivateEffect(owner, card, effectIndex);
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
            // Waehrend eine Kette sich ABBAUT, geht kein neues Fenster auf: was
            // ein aufloesender Effekt anstoesst (Beschwoerungen, Artefakte),
            // laeuft durch, ohne dass jemand hineingraetschen kann. Reagiert
            // wird auf Aktivierungen, nicht in deren Aufloesung.
            if (resolvingChain > 0) yield break;
            responseDepth++;

            foreach (var responder in new[] { firstPriority, firstPriority.Opponent })
            {
                if (Result != DuelResult.None) break;

                foreach (var (card, effectIndex) in BuildResponseCandidates(responder, context, contextCard))
                {
                    if (Result != DuelResult.None) break;

                    var effect = GetEffect(card, effectIndex);
                    if (effect == null || responder.Mana < EffectiveManaCost(responder, card, effect)) continue;
                    if (card.OncePerTurnUsed.Contains(effectIndex)) continue;
                    if (!HasValidTargets(effect, responder, card)) continue;

                    var request = new YesNoRequest
                    {
                        Title = isPhaseWindow ? context : $"Response to {context}",
                        Card = card,
                        IsPhaseWindow = isPhaseWindow,
                        IsResponse = !isPhaseWindow,
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
                {
                    // Trapline: Angriffs-/Beschwörungs-Fallen zünden NUR im passenden Fenster
                    var window = spell.Definition.effects[index].quickWindow;
                    if (window == QuickWindow.AttackResponse && context != "attack") continue;
                    if (window == QuickWindow.SummonResponse && context != "summon") continue;
                    candidates.Add((spell, index));
                }
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

            // Deckay Fiend/Vulture: Antworten, die NUR auf ein Reliquary-Summon
            // zünden — zentral gefiltert, damit jeder Sammelweg oben es erbt.
            bool reliquarySummon = context == "summon" && contextCard?.Definition is ReliquaryCardData;
            candidates.RemoveAll(pair =>
            {
                var fx = GetEffect(pair.Item1, pair.Item2);
                return fx != null && fx.onlyReliquarySummonResponse && !reliquarySummon;
            });

            return candidates;
        }

        // ================== BATTLE PHASE ==================

        private IEnumerator RunBattlePhase(PlayerState player)
        {
            int safety = 0;
            while (Result == DuelResult.None && safety++ < 50)
            {
                var request = BuildBattleActions(player);
                // Kein Auto-Ende, wenn nur noch "End Battle Phase" übrig ist: die Phase
                // gehört dem Spieler, bis er sie selbst schliesst. Bots wählen die
                // einzige Option sofort — und weil JEDER die Entscheidung trifft,
                // laufen Server und Client-Spiegel identisch.

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
            // (per Effekt für diesen Zug — oder dauerhaft per Attention Hound)
            var forcedTargets = player.Opponent.Monsters()
                .Where(m => m.MustBeAttackedThisTurn
                            || (!m.FaceDown && m.Definition != null && m.Definition.passiveTaunt))
                .ToList();

            foreach (var attacker in player.Monsters())
            {
                if (attacker.Position != BattlePosition.Attack) continue;
                if (attacker.CannotAttackThisTurn) continue;
                if (attacker.Definition != null && attacker.Definition.passiveCannotAttack) continue;
                if (attacker.SummonedThisTurn && attacker.Definition != null
                    && attacker.Definition.passiveNoAttackOnSummonTurn) continue;
                if (attacker.HasAttackedThisTurn && attacker.BonusAttacks <= 0
                    && !ConditionalSecondAttackReady(player, attacker)) continue;

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
                    // Lyria Green Room: geschützte verdeckte Monster sind kein Angriffsziel
                    var openTargets = (forcedTargets.Count > 0 ? forcedTargets : player.Opponent.Monsters().ToList())
                        .Where(t => !FaceDownShieldedFromAttack(t)).ToList();
                    foreach (var target in openTargets)
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
            if (attacker.Definition != null && attacker.Definition.passiveCannotAttack) yield break;
            if (attacker.SummonedThisTurn && attacker.Definition != null
                && attacker.Definition.passiveNoAttackOnSummonTurn) yield break;
            if (attacker.HasAttackedThisTurn && attacker.BonusAttacks <= 0
                && !ConditionalSecondAttackReady(player, attacker)) yield break;

            // Bonus- bzw. bedingten Zweitangriff verbrauchen: der Zweitangriff drückt
            // BonusAttacks auf -1, womit ConditionalSecondAttackReady (== 0) erlischt.
            if (attacker.HasAttackedThisTurn) attacker.BonusAttacks--;
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
            if (attacker.FaceDown)
            {
                // Bear Hug: eine Falle hat den Angreifer verdeckt gelegt — der Angriff verpufft
                Log("The attacker was turned face-down — attack cancelled.");
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
                        if (BattleShieldHolds(target)) Log($"{target.Name} stands firm — its artifacts hold the line.");
                        else yield return DestroyCard(target);
                        if (Result == DuelResult.None && target.Zone != ZoneType.MonsterZone)
                            yield return FireBearerKillTriggers(attacker);
                    }
                    else if (attackValue < defenderAtk)
                    {
                        DealDamage(player, defenderAtk - attackValue, target.Name, isBattleDamage: true);
                        if (BattleShieldHolds(attacker)) Log($"{attacker.Name} stands firm — its artifacts hold the line.");
                        else yield return DestroyCard(attacker);
                    }
                    else
                    {
                        Log("Both monsters clash with equal force!");
                        if (BattleShieldHolds(target)) Log($"{target.Name} stands firm — its artifacts hold the line.");
                        else yield return DestroyCard(target);
                        if (BattleShieldHolds(attacker)) Log($"{attacker.Name} stands firm — its artifacts hold the line.");
                        else yield return DestroyCard(attacker);
                    }
                }
                else
                {
                    int defenderDef = target.CurrentDef;
                    if (attackValue > defenderDef)
                    {
                        if (BattleShieldHolds(target))
                        {
                            Log($"{target.Name}'s defense bends but its artifacts hold the line.");
                        }
                        else
                        {
                            Log($"{target.Name}'s defense is broken.");
                            yield return DestroyCard(target);
                            if (Result == DuelResult.None && target.Zone != ZoneType.MonsterZone)
                                yield return FireBearerKillTriggers(attacker);
                        }
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

        /// <summary>
        /// Löst ein Token auf, statt es in eine Ablage zu bewegen. Danach liegt es
        /// in KEINER Liste mehr (Zone-Tag Banished nur als "weg"-Markierung,
        /// ohne Banished-Eintrag) — Friedhofs-/Banish-Bedingungen sehen es nie.
        /// </summary>
        private bool DissolveIfToken(CardInstance card)
        {
            if (card?.Definition == null || !card.Definition.isToken) return false;
            RemoveFromCurrentZone(card);
            card.Zone = ZoneType.Banished;
            Log($"{card.Name} dissolves into nothing.");
            BoardChanged();
            return true;
        }

        /// <summary>
        /// Gaslight: Platziert bis zu count Illusion-Tokens auf FREIE Monster-Zonen
        /// des Empfängers — offen in Verteidigung. Bewusst KEINE Summon-Trigger und
        /// kein Reaktionsfenster: die Platzierung ist Teil einer Effekt-Auflösung
        /// und gilt nicht als Beschwörung (sonst Mirrorwalk-Endlosschleife).
        /// </summary>
        private IEnumerator SpawnIllusionTokens(PlayerState receiver, int count)
        {
            var tokenDef = rules != null ? rules.illusionToken : null;
            if (tokenDef == null)
            {
                Log("No Illusion Token definition configured — nothing happens.");
                yield break;
            }
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                int free = receiver.FirstFreeZoneIndex(receiver.MonsterZones);
                if (free < 0) break;
                var token = new CardInstance(tokenDef, receiver)
                {
                    Zone = ZoneType.MonsterZone,
                    Position = BattlePosition.Defense,
                    SummonedThisTurn = true,
                    WasSpecialSummoned = true
                };
                receiver.MonsterZones[free] = token;
                placed++;
                BoardChanged();
                if (presenter != null) yield return presenter.ShowSummon(token);
            }
            if (placed > 0)
                Log($"{placed} Illusion Token{(placed == 1 ? "" : "s")} appear{(placed == 1 ? "s" : "")} on {receiver.Name}'s field.");
            else
                Log($"{receiver.Name} has no free Monster Zone — no Illusion Token appears.");
        }

        public void MoveToGraveyard(CardInstance card)
        {
            // Tokens sind Illusionen: statt in den Friedhof zu wandern, lösen sie
            // sich auf — sie liegen danach in KEINER Liste und zählen für keine
            // Friedhofs-Bedingung.
            if (DissolveIfToken(card)) return;

            // Deckay: Karten mit Friedhofs-Triggern merken sich ihre Herkunft —
            // die Trigger feuern gesammelt an der nächsten Naht (FirePendingGraveTriggers),
            // denn dieser Umzug hier ist synchron und kann keine Kette starten.
            var fromZone = card.Zone;
            if (HasGraveArrivalTrigger(card))
                pendingGraveTriggers.Add((card, fromZone));

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
            if (DissolveIfToken(card)) return;
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
                // Das Ersatz-Opfer ist ENDGÜLTIG: asReplacement verhindert, dass
                // ein zweites Schutz-Artefakt auch diese Zerstörung umleitet.
                // Sonst retten sich zwei Bulwark Prisms gegenseitig bis in alle
                // Ewigkeit — A zerbricht für X, B für A, A für B, und keines
                // stirbt je. Zwei Bots, die immer Ja sagen, haben damit den
                // DuelHost eingefroren.
                yield return DestroyCard(artifact, asReplacement: true);
                yield break;
            }
        }

        private IEnumerator DestroyCard(CardInstance card, bool asReplacement = false)
        {
            if (card == null || card.Zone == ZoneType.Graveyard) yield break;
            if (card.CannotBeDestroyedThisTurn)
            {
                Log($"{card.Name} cannot be destroyed this turn.");
                yield break;
            }

            if (!asReplacement)
            {
                bool shielded = false;
                yield return TryRedirectDestruction(card, value => shielded = value);
                if (shielded) yield break;
            }

            bool wasMonster = card.MonsterData != null;
            if (presenter != null) yield return presenter.ShowCardDestroyed(card); // Zersplittern + Flug zum Friedhof
            if (wasMonster) DetachEquipsToGraveyard(card);
            MoveToGraveyard(card);
            Log($"{card.Name} is destroyed.");
            BoardChanged();

            if (responseDepth < 2)
            {
                // OnDestroyedSelf gilt für JEDE Karte — auch Artefakte (Fall Guy,
                // Bulwark Prism). Vorher feuerte er nur für Monster, womit
                // Artefakt-Sterbenseffekte stillschweigend nie liefen.
                yield return OfferTriggeredEffects(card.Owner, card, EffectTrigger.OnDestroyedSelf);
                if (Result != DuelResult.None) yield break;
                if (wasMonster)
                {
                    // Warm Memories: Feld/Hand des Besitzers hört mit, wenn ein eigenes Monster fällt
                    foreach (var listener in TriggerScanCandidates(card.Owner).ToArray())
                    {
                        if (Result != DuelResult.None) yield break;
                        yield return OfferTriggeredEffects(card.Owner, listener, EffectTrigger.OnOwnMonsterDestroyed);
                    }
                }
                // Deckay: der Friedhofs-Ankunfts-Trigger der zerstörten Karte
                yield return FirePendingGraveTriggers();
            }
        }
    }
}
