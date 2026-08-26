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
                    // A Foot in the Door: der Rabatt drückt die Tributkosten dieses Zuges
                    int tributes = Math.Max(0, rules.TributesForLevel(card.MonsterData.level) - player.NextNormalSummonTributeDiscount);
                    // Tribute räumen ihre eigene (unversiegelte) Zone — ohne Tribut braucht
                    // es eine freie Zone, die NICHT zugemauert ist (Road to 1000: Siegel).
                    // Bylaw (Standing Room Only): die Monster-Obergrenze zählt NACH Tributen.
                    int monsterCap = MonsterCapFor(player);
                    bool canSummon = player.NormalSummonsUsed < rules.normalSummonsPerTurn + player.ExtraNormalSummons
                                     && TributableCount(player) >= tributes
                                     && (tributes > 0 || UnsealedFreeMonsterZones(player) > 0)
                                     && player.MonsterCount() - tributes < monsterCap
                                     && !FieldLimitReached(player, card.Definition)
                                     && !card.Definition.passiveNoNormalSummon;   // The Small Print: nur per eigener SS
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
                    // The Squatter zieht in eine VERSIEGELTE eigene Zone — alle anderen
                    // brauchen eine freie, unversiegelte. Giftwyrms werden dem GEGNER
                    // zugestellt und brauchen dessen Platz (und dessen Monster-Obergrenze).
                    var monsterData = card.MonsterData;
                    bool zoneForSelfSummon =
                        monsterData != null && monsterData.selfSummonToOpponentField
                            ? UnsealedFreeMonsterZones(player.Opponent) > 0
                              && player.Opponent.MonsterCount() < MonsterCapFor(player.Opponent)
                        : monsterData != null && monsterData.selfSummonIntoSealedZone
                            ? AnyOwnSealedEmptyZone(player)
                            : UnsealedFreeMonsterZones(player) > 0
                              && player.MonsterCount() < MonsterCapFor(player);
                    if (monsterData.canSelfSpecialSummon && zoneForSelfSummon
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
                        // REGEL: Selbst-Spezialbeschwörungen aus dem Main Deck gehen
                        // grundsätzlich nur EINMAL pro Zug (je Kartenname) — sonst
                        // leert eine Hand voller Kopien das ganze Feld auf einmal.
                        bool oncePerTurnOk = !player.SelfSummonedNamesThisTurn.Contains(card.Name);
                        // The Small Print: weitere Bedingungen + LP-Kosten + Sworn-to-the-Gate-Sperre
                        int otherHand = player.Hand.Count - 1;
                        bool smallPrintOk = (!monsterData.selfSummonRequiresNoOwnMonsters || player.MonsterCount() == 0)
                            && (monsterData.selfSummonRequiresOwnMonsters <= 0 || player.MonsterCount() >= monsterData.selfSummonRequiresOwnMonsters)
                            && (!monsterData.selfSummonRequiresLifeBelowOpponent || player.LifePoints < player.Opponent.LifePoints)
                            && (monsterData.selfSummonRequiresHandAtMost <= 0 || otherHand <= monsterData.selfSummonRequiresHandAtMost)
                            && (monsterData.selfSummonRequiresHandAtLeast <= 0 || otherHand >= monsterData.selfSummonRequiresHandAtLeast)
                            && (!monsterData.selfSummonRequiresOpponentDefenseMonster
                                || player.Opponent.Monsters().Any(m => m.Position == BattlePosition.Defense))
                            && (!monsterData.selfSummonRequiresLienOnField || AnyLienOnField())
                            && CanPayLife(player, monsterData.selfSummonLifeCost)
                            && !SpecialSummonsLockedFor(player);
                        // Road to 1000: die neuen Bedingungsfamilien
                        bool roadOk = (!monsterData.selfSummonRequiresOpponentMoreMonsters
                                || player.Opponent.MonsterCount() > player.MonsterCount())
                            && (monsterData.selfSummonRequiresLifeAtMost <= 0
                                || player.LifePoints <= monsterData.selfSummonRequiresLifeAtMost)
                            && (!monsterData.selfSummonRequiresGraveTopMonster
                                || (player.Graveyard.Count > 0 && player.Graveyard[player.Graveyard.Count - 1].MonsterData != null))
                            && (!monsterData.selfSummonRequiresRevealedThisTurn || player.RevealedCardThisTurn || player.Opponent.RevealedCardThisTurn)
                            && (!monsterData.selfSummonRequiresLevels1And3
                                || (player.Monsters().Any(m => !m.FaceDown && m.EffectiveLevel == 1)
                                    && player.Monsters().Any(m => !m.FaceDown && m.EffectiveLevel == 3)))
                            && (!monsterData.selfSummonRequiresOpponentLevel3AndNoneSelf
                                || (player.Opponent.Monsters().Any(m => !m.FaceDown && m.EffectiveLevel == 3)
                                    && !player.Monsters().Any(m => !m.FaceDown && m.EffectiveLevel == 3)))
                            && (monsterData.selfSummonRequiresTurnAtLeast <= 0
                                || player.TurnsTaken >= monsterData.selfSummonRequiresTurnAtLeast)
                            && (monsterData.selfSummonRequiresDeckAtLeast <= 0
                                || player.DeckPile.Count >= monsterData.selfSummonRequiresDeckAtLeast)
                            && (monsterData.selfSummonRequiresLpWithin <= 0
                                || Math.Abs(player.LifePoints - player.Opponent.LifePoints) <= monsterData.selfSummonRequiresLpWithin)
                            && (monsterData.selfSummonRequiresArtifacts <= 0
                                || player.ArtifactZones.Count(a => a != null) >= monsterData.selfSummonRequiresArtifacts);
                        if (nameOk && attributeOk && faceDownOk && artifactOk && foeCountOk && milledOk && graveNamedOk && oncePerTurnOk && smallPrintOk && roadOk)
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
                    // The Liberator: der Gegner erzwingt den Umweg über das Setzen —
                    // direkte Hand-Aktivierungen entfallen, die Set-Option bleibt.
                    if (!MustSetSpellsFirst(player))
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
            // Siegel drücken hier doppelt: das Reliquary braucht eine unversiegelte Zone —
            // oder Tribute, die ihre eigene räumen (die Tributrechnung prüft der Summon selbst)
            if ((UnsealedFreeMonsterZones(player) > 0 || player.MonsterCount() > 0)
                && player.MonsterCount() < MonsterCapFor(player) && !SpecialSummonsLockedFor(player))
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

            // Eigene Artefakte dürfen freiwillig ins Grab — macht volle Artefakt-Zonen
            // wieder frei (Klick aufs Artefakt zeigt die Option).
            foreach (var artifact in player.ArtifactZones)
            {
                if (artifact == null) continue;
                request.Options.Add(new MainActionOption
                {
                    Kind = MainActionKind.SacrificeArtifact,
                    Card = artifact,
                    Label = $"Send {artifact.Name} to the Graveyard"
                });
            }

            foreach (var monster in player.Monsters())
            {
                if (monster.SummonedThisTurn || monster.HasAttackedThisTurn) continue;
                if (monster.PositionLockedThisTurn) continue;
                if (monster.PositionChangesUsed >= rules.positionChangesPerTurn) continue;
                if (monster.Definition != null && monster.Definition.passiveCannotChangePosition) continue; // Small Print
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
                    {
                        yield return OfferTriggeredEffects(player, option.Card, EffectTrigger.OnFlipFaceUp);
                        // Orchestra Pit hört auch auf manuelle Flips
                        foreach (var listener in TriggerScanCandidates(player).ToArray())
                        {
                            if (Result != DuelResult.None) yield break;
                            yield return OfferTriggeredEffects(player, listener, EffectTrigger.OnOwnMonsterFlipped);
                        }
                    }
                    else if (!wasFaceDown && responseDepth < 2 && option.Card != null && IsOnField(option.Card))
                    {
                        // Volte-Face: offener Positionswechsel — mit Kleingedrucktem kein Angriff mehr
                        if (option.Card.Definition != null && option.Card.Definition.passiveNoAttackAfterPositionChange)
                            option.Card.CannotAttackThisTurn = true;
                        yield return OfferTriggeredEffects(player, option.Card, EffectTrigger.OnPositionChangedSelf);
                    }
                    break;
                }
                case MainActionKind.SpecialSummonSelf:
                    yield return ExecuteSelfSpecialSummon(player, option.Card, option.PreferredZoneIndex);
                    break;
                case MainActionKind.SummonReliquary:
                    yield return ExecuteReliquarySummon(player, option.Card, option.PreferredZoneIndex);
                    break;
                case MainActionKind.SacrificeArtifact:
                    // Freiwillige Entsorgung: schafft Platz in den Artefakt-Zonen.
                    // Bewusst SEND, nicht destroy — Zerstörungs-Trigger bleiben stumm,
                    // Friedhofs-Ankunfts-Trigger feuern normal.
                    if (option.Card != null && option.Card.Zone == ZoneType.ArtifactZone
                        && option.Card.Owner == player)
                    {
                        Log($"{player.Name} sends {option.Card.Name} to the Graveyard.");
                        if (presenter != null) yield return presenter.ShowCardSentToGrave(option.Card);
                        MoveToGraveyard(option.Card);
                        BoardChanged();
                        yield return FirePendingGraveTriggers();
                    }
                    break;
            }

            // Kurzer Beat nach jeder Aktion, damit das Duell lesbar bleibt (nicht headless)
            if (presenter != null && option.Kind != MainActionKind.ToBattlePhase && option.Kind != MainActionKind.EndTurn)
                yield return DuelWait.For(0.2f);
        }

        // ================== RELIQUARY (EXTRA DECK) ==================

        /// <summary>The Fallen One: liegt irgendwo ein offener Reliquary-Blocker?</summary>
        private bool ReliquarySummonsBlocked()
        {
            foreach (var side in new[] { Player1, Player2 })
                foreach (var monster in side.Monsters())
                    if (!monster.FaceDown && !monster.EffectsNegated
                        && monster.Definition != null && monster.Definition.passiveBlockReliquarySummons)
                        return true;
            return false;
        }

        /// <summary>Prüft alle Beschwörungs-Voraussetzungen einer Reliquary-Karte (inkl. Bezahlbarkeit der Kosten).</summary>
        private bool ReliquaryRequirementsMet(PlayerState player, ReliquaryCardData data)
        {
            var opponent = player.Opponent;
            if (ReliquarySummonsBlocked()) return false; // The Fallen One sperrt beide Spieler
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
                && opponent.Monsters().Count(m => !m.FaceDown && m.Name.Contains(data.reqOpponentNamedOnField))
                   < Math.Max(1, data.reqOpponentNamedCount)) return false;
            // --- 5 Archetypes ---
            if (data.reqDealsThisTurn > 0 && player.DealsThisTurn < data.reqDealsThisTurn) return false;
            if (data.reqDealsThisDuel > 0 && player.DealsThisDuel < data.reqDealsThisDuel) return false;
            if (data.reqOpponentAttackedRecently
                && !opponent.DeclaredAttackThisTurn && !opponent.DeclaredAttackLastTurn) return false;
            if (data.reqOwnCountdownCards > 0
                && player.FieldCards().Count(c => c.CountdownMarkers > 0) < data.reqOwnCountdownCards) return false;
            if (data.reqGraveyardNamedCount > 0
                && player.Graveyard.Count(c => string.IsNullOrEmpty(data.reqGraveyardNamed)
                       || c.Name.Contains(data.reqGraveyardNamed)) < data.reqGraveyardNamedCount) return false;
            if (data.reqOwnArtifactsOnField > 0
                && player.ArtifactZones.Count(a => a != null) < data.reqOwnArtifactsOnField) return false;
            if (data.reqOwnArtifactsInGrave > 0
                && player.Graveyard.Count(c => c.ArtifactData != null) < data.reqOwnArtifactsInGrave) return false;
            if (data.reqOwnFaceDownMonsters > 0
                && player.Monsters().Count(m => m.FaceDown) < data.reqOwnFaceDownMonsters) return false;
            if (data.reqMonsterWithEquip && !player.Monsters().Any(m => m.EquippedArtifacts.Count > 0)) return false;
            if (data.reqGraveyardAtLeast > 0 && player.Graveyard.Count < data.reqGraveyardAtLeast) return false;
            if (data.reqGraveyardSpellsAtLeast > 0
                && player.Graveyard.Count(c => c.SpellData != null) < data.reqGraveyardSpellsAtLeast) return false;
            if (data.reqOpponentGraveyardAtLeast > 0
                && opponent.Graveyard.Count < data.reqOpponentGraveyardAtLeast) return false;
            if (data.reqGraveyardMonstersAtLeast > 0
                && player.Graveyard.Count(c => c.MonsterData != null) < data.reqGraveyardMonstersAtLeast) return false;
            if (data.reqControlNoMonsters && player.MonsterCount() > 0) return false;
            if (data.reqOwnMonstersAtLeast > 0 && player.MonsterCount() < data.reqOwnMonstersAtLeast) return false;
            if (data.reqLifeAtMost > 0 && player.LifePoints > data.reqLifeAtMost) return false;
            if (data.reqBanishedAtLeast > 0 && player.Banished.Count < data.reqBanishedAtLeast) return false;
            // The Small Print
            if (data.reqHandEmpty && player.Hand.Count > 0) return false;
            if (data.reqControlChangedOnField
                && !player.Monsters().Concat(opponent.Monsters()).Any(m => m.Owner != m.OriginalOwner)) return false;
            // Immortal Demon: nirgends Reliquaries — weder auf den Feldern noch in den Verbannungen
            if (data.reqNoReliquariesOnFieldOrBanish)
            {
                foreach (var side in new[] { player, opponent })
                {
                    if (side.Monsters().Any(m => m.Definition is ReliquaryCardData)) return false;
                    if (side.Banished.Any(c => c.Definition is ReliquaryCardData)) return false;
                }
            }
            if (data.reqReliquariesInGraveAtLeast > 0
                && player.Graveyard.Count(c => c.Definition is ReliquaryCardData) < data.reqReliquariesInGraveAtLeast) return false;
            // The Last Asemir: der Gegner muss eine echte Bedrohung kontrollieren
            if (data.reqOpponentMonsterAtkAtLeast > 0
                && !opponent.Monsters().Any(m => !m.FaceDown && m.CurrentAtk >= data.reqOpponentMonsterAtkAtLeast)) return false;

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
                tributeRequest.Candidates.AddRange(player.Monsters().Where(m => !m.CannotBeDestroyedThisTurn && !CannotBeTributed(m)));
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
                    .Where(m => !m.CannotBeDestroyedThisTurn && m != tributePick && !CannotBeTributed(m)).ToList();
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
                    .Where(m => !m.CannotBeDestroyedThisTurn && !m.CannotBeTargetedThisTurn && !CannotBeTributed(m)));
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
        /// <summary>Hat der Spieler eine LEERE, versiegelte Monster-Zone? (The Squatter)</summary>
        private bool AnyOwnSealedEmptyZone(PlayerState player)
        {
            for (int i = 0; i < player.MonsterZones.Length; i++)
                if (player.MonsterZones[i] == null && IsZoneSealed(player, i)) return true;
            return false;
        }

        private IEnumerator ExecuteSelfSpecialSummon(PlayerState player, CardInstance monster, int preferredZone = -1)
        {
            if (monster.MonsterData == null || !player.Hand.Contains(monster)) yield break;

            // Giftwyrm: das Geschenk wird dem GEGNER zugestellt — er kontrolliert es,
            // es bleibt aber die Karte des Zustellers (OriginalOwner). Bewusst KEINE
            // Summon-Trigger und kein Reaktionsfenster: eine Zustellung ist keine
            // Beschwörung, auf die man antwortet (Gaslight-Token-Schule).
            if (monster.MonsterData.selfSummonToOpponentField)
            {
                var receiver = player.Opponent;
                var free = new List<int>();
                for (int i = 0; i < receiver.MonsterZones.Length; i++)
                    if (receiver.MonsterZones[i] == null && !IsZoneSealed(receiver, i)) free.Add(i);
                if (free.Count == 0 || receiver.MonsterCount() >= MonsterCapFor(receiver)) yield break;

                int deliverZone = free[0];
                if (free.Count > 1)
                {
                    var pick = new ZoneSelectRequest
                    {
                        Title = $"Choose a zone on {receiver.Name}'s field for {monster.Name}",
                        ForPlayer = receiver,
                        Zone = ZoneType.MonsterZone
                    };
                    pick.FreeIndices.AddRange(free);
                    yield return DecideRouted(player, pick);
                    if (free.Contains(pick.Result)) deliverZone = pick.Result;
                }

                presenter?.RememberView(monster);
                player.Hand.Remove(monster);
                receiver.MonsterZones[deliverZone] = monster;
                monster.Owner = receiver;
                monster.Zone = ZoneType.MonsterZone;
                monster.FaceDown = false;
                monster.Position = monster.MonsterData.selfSummonPosition;
                monster.SummonedThisTurn = true;
                monster.WasSpecialSummoned = true;
                monster.WasDisloyalWhenLeftField = false;
                player.SelfSummonedNamesThisTurn.Add(monster.Name);
                ArmCountdown(monster);
                Log($"{player.Name} delivers {monster.Name} to {receiver.Name}'s field — what a generous gift.");
                BoardChanged();
                if (presenter != null) yield return presenter.ShowCardMoved(monster);
                yield break;
            }

            int zoneIndex = -1;
            if (monster.MonsterData.selfSummonIntoSealedZone)
            {
                // The Squatter, Uninvited: zieht ausgerechnet in eine VERSIEGELTE eigene
                // Zone ein — und bricht das Siegel dabei (auch Padlock-Nachbarschaften
                // sind dann schlicht besetzt).
                var sealed_ = new List<int>();
                for (int i = 0; i < player.MonsterZones.Length; i++)
                    if (player.MonsterZones[i] == null && IsZoneSealed(player, i)) sealed_.Add(i);
                if (sealed_.Count == 0) yield break;
                zoneIndex = sealed_[0];
                if (sealed_.Count > 1)
                {
                    var request = new ZoneSelectRequest
                    {
                        Title = $"Choose a sealed zone for {monster.Name}",
                        ForPlayer = player,
                        Zone = ZoneType.MonsterZone
                    };
                    request.FreeIndices.AddRange(sealed_);
                    yield return DecideRouted(player, request);
                    if (sealed_.Contains(request.Result)) zoneIndex = request.Result;
                }
                player.ZoneSeals.RemoveAll(seal => seal.Index == zoneIndex);
                Log($"{monster.Name} moves into the sealed zone — the seal breaks!");
            }
            else
            {
                yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                    $"Choose a zone for {monster.Name}", preferredZone, index => zoneIndex = index);
            }
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

            // The Small Print (Blood Oath): die Beschwörung kostet Leben — Aurel macht sie gratis
            if (monster.MonsterData.selfSummonLifeCost > 0)
            {
                if (!CanPayLife(player, monster.MonsterData.selfSummonLifeCost)) yield break;
                PayLife(player, monster.MonsterData.selfSummonLifeCost, monster.Name);
            }

            presenter?.RememberView(monster);
            player.Hand.Remove(monster);
            player.MonsterZones[zoneIndex] = monster;
            monster.Zone = ZoneType.MonsterZone;
            monster.FaceDown = false;
            monster.Position = summonPosition;
            monster.SummonedThisTurn = true;
            monster.WasSpecialSummoned = true;
            ArmCountdown(monster); // Chimekeep
            player.SelfSummonedNamesThisTurn.Add(monster.Name); // "einmal pro Zug"-Gedächtnis
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
            // Road to 1000: versiegelte Monster-Zonen sind kein Bauplatz — weder für
            // Beschwörungen noch fürs Setzen. Der Wunsch-Slot wird genauso geprüft.
            bool sealable = zoneType == ZoneType.MonsterZone && zones == player.MonsterZones;
            if (preferred >= 0 && preferred < zones.Length && zones[preferred] == null
                && !(sealable && IsZoneSealed(player, preferred)))
            {
                apply(preferred);
                yield break;
            }

            var free = new List<int>();
            for (int i = 0; i < zones.Length; i++)
                if (zones[i] == null && !(sealable && IsZoneSealed(player, i))) free.Add(i);

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
            // A Foot in the Door: der Rabatt gilt für die NÄCHSTE Normalbeschwörung dieses Zuges
            int tributes = Math.Max(0, rules.TributesForLevel(monster.MonsterData.level) - player.NextNormalSummonTributeDiscount);

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
                // The Small Print: "cannot be Tributed" (White Elephant, Gift Horse, Stone) bleibt außen vor
                targetRequest.Candidates.AddRange(player.Monsters().Where(m => !CannotBeTributed(m)));
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
            player.NextNormalSummonTributeDiscount = 0; // A Foot in the Door: verbraucht
            if (!setFaceDown) ArmCountdown(monster);    // Chimekeep (verdeckt: erst beim Flip)

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
        private IEnumerator SpecialSummonToField(PlayerState player, CardInstance monster, string sourceDescription, bool inDefense = false)
        {
            if (player.CannotSpecialSummonThisTurn)
            {
                Log($"{player.Name} cannot Special Summon this turn — {monster.Name} stays where it is.");
                yield break;
            }
            // Bylaw (Standing Room Only): die Obergrenze gilt auch für Effekt-Beschwörungen
            if (player.MonsterCount() >= MonsterCapFor(player))
            {
                Log($"{player.Name} already controls the decreed maximum of monsters — {monster.Name} stays where it is.");
                yield break;
            }
            // Sworn to the Gate: der einsame Torwächter duldet keine weiteren Spezialbeschwörungen
            if (SpecialSummonsLockedFor(player) && monster.Owner == player)
            {
                Log($"{player.Name}'s oath forbids other Special Summons — {monster.Name} stays where it is.");
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
            monster.Position = inDefense ? BattlePosition.Defense : BattlePosition.Attack;
            monster.SummonedThisTurn = true;
            monster.WasSpecialSummoned = true;
            monster.PermanentAtkBonus = 0;
            monster.PermanentDefBonus = 0;
            monster.TempAtkBonus = 0;
            monster.TempDefBonus = 0;
            ArmCountdown(monster); // Chimekeep: die Uhr zieht sich beim Feld-Betreten auf
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

        /// <summary>
        /// Slowburn: Gesetzte Spells mit Charged-Effekt zünden in der EIGENEN
        /// Standby Phase automatisch — sofern sie VOR diesem Zug gesetzt wurden
        /// (SetThisTurn ist beim Zugwechsel gefallen). Bricht der Spieler eine
        /// Zielwahl ab, bleibt die Lunte liegen und fragt nächste Standby erneut.
        /// </summary>
        private IEnumerator ResolveChargedSpells(PlayerState player)
        {
            foreach (var spell in player.SpellZones.ToArray())
            {
                if (spell == null || spell.SpellData == null || !spell.FaceDown || spell.SetThisTurn) continue;
                int charged = ChargedEffectIndex(spell);
                if (charged < 0) continue;
                Log($"{spell.Name}'s fuse burns down — the charged effect triggers!");
                yield return ActivateSpell(player, spell, charged, fromHand: false);
                if (Result != DuelResult.None) yield break;
            }
        }

        /// <summary>Index des Charged-Effekts eines Spells, -1 wenn keiner existiert.</summary>
        private static int ChargedEffectIndex(CardInstance spell)
        {
            if (spell?.Definition == null) return -1;
            for (int i = 0; i < spell.Definition.effects.Count; i++)
                if (spell.Definition.effects[i].trigger == EffectTrigger.ChargedStandby) return i;
            return -1;
        }

        private IEnumerator ActivateSpell(PlayerState player, CardInstance spell, int effectIndex, bool fromHand)
        {
            var effect = GetEffect(spell, effectIndex);
            if (effect == null) yield break;
            if (IsNameForbidden(spell.Name)) yield break; // The Forbidden Name
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
            // Countersign: die Steuer trifft genau EINEN Zauber, dann ist sie abgegolten
            if (player.NextSpellSurcharge > 0)
            {
                Log($"{player.Name} pays the Countersign surcharge of {player.NextSpellSurcharge} Mana.");
                player.NextSpellSurcharge = 0;
            }
            LockEffectForTurn(spell, effectIndex, effect);

            if (fromHand) player.Hand.Remove(spell);
            else RemoveFromZoneArray(player.SpellZones, spell);

            activationSerial++;
            int chainLink = ++chainDepth;
            chainCards.Add(spell);
            chainEffects.Add(effect);
            // "(set)" macht im Protokoll sichtbar, dass der Zauber vom Feld kam —
            // und macht die Liberator-Sperre (Hand-Aktivierungen verboten) prüfbar.
            Log($"{player.Name} activates {spell.Name}{(fromHand ? "" : " (set)")}{ActivationLogSuffix(effect)}.");
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
            // The Appointed Hour: die Uhr wird frisch aufgezogen
            if (artifact.Definition != null && artifact.Definition.countdownMarkers > 0)
            {
                artifact.CountdownMarkers = artifact.Definition.countdownMarkers;
                Log($"{artifact.Name} enters with {artifact.CountdownMarkers} Hour Counters.");
            }
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
            if (IsNameForbidden(card.Name)) yield break; // The Forbidden Name

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

            // Splithoof Grinning Ledger: das Haus verdient mit — aktiviert der
            // NICHT-Besitzer, bekommt der Besitzer seine Provision (nächster Zug).
            if (card.Owner != null && card.Owner != player && card.Definition != null
                && card.Definition.passiveOwnerRoyaltyManaNextTurn > 0)
            {
                card.Owner.ManaCredit += card.Definition.passiveOwnerRoyaltyManaNextTurn;
                Log($"{card.Owner.Name} collects the house's cut — {card.Definition.passiveOwnerRoyaltyManaNextTurn} Mana next turn.");
            }

            activationSerial++;
            int chainLink = ++chainDepth;
            chainCards.Add(card);
            chainEffects.Add(effect);
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
            ArmCountdown(monster); // Chimekeep: verdeckte Uhren ziehen sich beim Aufdecken auf
            Log($"{monster.Name} is flipped face-up!");
            BoardChanged();
            if (responseDepth < 2)
            {
                yield return OfferTriggeredEffects(monster.Owner, monster, EffectTrigger.OnFlipFaceUp);
                // Lyria Orchestra Pit: Feld/Hand des Besitzers hört mit, wenn ein eigenes Monster aufgedeckt wird
                foreach (var listener in TriggerScanCandidates(monster.Owner).ToArray())
                {
                    if (Result != DuelResult.None) yield break;
                    yield return OfferTriggeredEffects(monster.Owner, listener, EffectTrigger.OnOwnMonsterFlipped);
                }
            }
        }

        private void ExecuteChangePosition(PlayerState player, CardInstance monster)
        {
            if (monster.Zone != ZoneType.MonsterZone) return;
            if (monster.FaceDown)
            {
                monster.FaceDown = false;
                monster.Position = BattlePosition.Attack;
                ArmCountdown(monster); // Chimekeep
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
            // The Small Print: einmal pro DUELL — je Spieler und Kartenname, alle Kopien teilen
            if (effect.oncePerDuel && card.Owner != null)
                card.Owner.OncePerDuelUsed.Add(OncePerDuelKey(card, effectIndex));
        }

        private static string OncePerDuelKey(CardInstance card, int effectIndex) => card.Name + "#" + effectIndex;

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
                if (IsNameForbidden(card.Name)) continue; // The Forbidden Name: Name ist gesperrt
                // The Fallen One (Infused): keine Effekte spezialbeschworener Feldmonster
                if (player.SpecialSummonedEffectsLockedThisTurn
                    && card.Zone == ZoneType.MonsterZone && card.WasSpecialSummoned) continue;
                // The Forbidden Name: der Normal-Effekt bleibt im eigenen Zug daheim
                if (effect.onlyDuringYourTurn && TurnPlayer != player) continue;
                // Emergency Barrier: der Notfall-Einsatz zündet nur im Gegnerzug
                if (effect.onlyDuringOpponentTurn && TurnPlayer == player) continue;
                if (RequiresOpenChain(effect) && chainCards.Count == 0) continue;
                // --- The Small Print ---
                if (effect.oncePerDuel && player.OncePerDuelUsed.Contains(OncePerDuelKey(card, i))) continue;
                if (effect.onlyDuringMainPhase && Phase != DuelPhase.Main) continue;
                if (effect.onlyDuringBattlePhase && Phase != DuelPhase.Battle) continue;
                // He Sleeps Lightly: nur solange die Karte die OBERSTE Friedhofskarte ist
                if (effect.onlyWhileGraveTop
                    && (card.Zone != ZoneType.Graveyard || card.Owner == null
                        || card.Owner.Graveyard.Count == 0
                        || card.Owner.Graveyard[card.Owner.Graveyard.Count - 1] != card)) continue;
                // --- 5 Archetypes ---
                // Giftwyrm: der Trigger verlangt Fremdkontrolle (auf dem Feld: jetzt;
                // nach dem Abgang: beim Verlassen des Feldes)
                if (effect.onlyWhileControlledByOpponent
                    && !(IsOnField(card) ? card.Owner != card.OriginalOwner : card.WasDisloyalWhenLeftField)) continue;
                if (effect.requireOpponentAttackedThisTurn && !player.Opponent.DeclaredAttackThisTurn) continue;
                if (effect.requireStruckThisTurn && !player.CountdownStruckThisTurn) continue;
                if (card.SpellData != null && effect.trigger == EffectTrigger.OnActivate && player.SpellsLockedThisTurn) continue;
                if (!CanPayLifeCosts(effect, player)) continue;
                if (!ChainContextAllows(effect, player)) continue;
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
            // --- Road to 1000 ---
            // Krönung: ALLE gelisteten Namen müssen offen auf dem eigenen Feld liegen
            if (!string.IsNullOrEmpty(effect.requiresControlNamed))
            {
                foreach (var wanted in effect.requiresControlNamed.Split(';'))
                {
                    string name = wanted.Trim();
                    if (name.Length == 0) continue;
                    bool found = false;
                    foreach (var card in player.FieldCards())
                        if (card != null && !card.FaceDown && card.Name.Contains(name)) { found = true; break; }
                    if (!found) return false;
                }
            }
            if (effect.requireOwnMonsterDestroyedThisTurn && player.OwnMonstersDestroyedThisTurn <= 0) return false;
            if (effect.onlyOnFirstOwnTurn && player.TurnsTaken != 1) return false;
            if (effect.maxOwnMonsters > 0 && player.MonsterCount() > effect.maxOwnMonsters) return false;
            return true;
        }

        private bool HasValidTargets(EffectDefinition effect, PlayerState player, CardInstance source = null)
        {
            // YuGiOh-Aktivierungslegalität: Aktionen, die Karten aufs Feld bringen,
            // sind ohne freie Zone gar nicht erst aktivierbar — auch "bis zu"-
            // Fassungen. Räumt eine FRÜHERE Aktion desselben Effekts womöglich
            // selbst Platz (Opfer/Bounce/eigene Zerstörung als Kosten), gilt die
            // Sperre nicht — die Auflösung schafft sich ihren Platz.
            bool earlierMayFree = false;
            bool sourceIsFieldMonster = source != null && source.MonsterData != null && IsOnField(source);
            // Münzwurf-Ziele: erst nach dem Wurf gewählt — sind ALLE zielenden Aktionen
            // münzgebunden und keine hat Kandidaten, gibt es nichts zu wetten.
            bool anyGatedTargets = false, anyGatedCandidates = false, onlyCoinBusiness = true;
            foreach (var action in effect.actions)
            {
                // Cull the Weak: ohne Monster im eigenen Deck gibt es nichts aufzudecken
                if (action.type == EffectActionType.SimultaneousDeckCull
                    && !player.DeckPile.Any(c => c.MonsterData != null)) return false;
                if (!earlierMayFree && FreeCapacityFor(action.type, player) <= 0) return false;
                if (ActionMayFreeOwnZone(action.type)) earlierMayFree = true;
                // Gift Horse: nur der Besitzer verschenkt — der Beschenkte kann es nicht zurückreichen
                if (action.type == EffectActionType.GiveSelfToOpponent && source != null
                    && source.OriginalOwner != null && source.OriginalOwner != player) return false;
                bool targeted = action.target != TargetKind.None && action.target != TargetKind.SelfCard
                    && action.target != TargetKind.SameAsPrevious;
                if ((action.coinGate != CoinGate.None || action.dealGate != DealGate.None) && targeted)
                {
                    anyGatedTargets = true;
                    if (BuildTargetCandidates(action, player, source).Count > 0) anyGatedCandidates = true;
                    continue;
                }
                if (action.type != EffectActionType.FlipCoin
                    && action.type != EffectActionType.OfferDeal) onlyCoinBusiness = false;
                if (!targeted) continue;
                // "Bis zu"-Aktionen sind optional: null Kandidaten blockieren die
                // Aktivierung nicht (Trapline: "then Set 1 ... from your hand").
                if (action.upToTargets) continue;
                // Zauber-Anker (Lock Shields/Stare Down): Nachbarn/Gegenüber hängen am gewählten Ziel
                if (IsAutoTarget(action.target) && !sourceIsFieldMonster) continue;
                if (BuildTargetCandidates(action, player, source).Count == 0) return false;
            }
            if (anyGatedTargets && !anyGatedCandidates && onlyCoinBusiness) return false;
            return true;
        }

        /// <summary>
        /// Wie viele Karten diese Aktion mangels Zonen-Platz überhaupt aufs Feld
        /// bringen könnte (int.MaxValue = kein Zonenbedarf). Grundlage der
        /// Aktivierungslegalität UND der Zielwahl-Klammer: "bis zu 2 setzen"
        /// bietet bei nur einer freien Zone auch nur ein Ziel an. Ändert sich
        /// das Brett NACH der Aktivierung, fizzlet die Auflösung ganz normal.
        /// </summary>
        private int FreeCapacityFor(EffectActionType type, PlayerState player)
        {
            switch (type)
            {
                case EffectActionType.SpecialSummonFromGraveyard:
                case EffectActionType.SpecialSummonTargetFromHand:
                case EffectActionType.SpecialSummonTargetFromBanished:
                case EffectActionType.SpecialSummonTargetFromGraveOrBanish:
                case EffectActionType.SpecialSummonTargetFromHandOrGrave:
                case EffectActionType.SpecialSummonTargetFaceDown:
                case EffectActionType.SpecialSummonTargetFromDeck:
                case EffectActionType.SpecialSummonTargetFromGraveFaceDown:
                case EffectActionType.SummonCopyOfTarget:
                case EffectActionType.SummonReliquaryFromExtraSuppressed:
                case EffectActionType.SpecialSummonFromOpponentGraveyard:
                case EffectActionType.SpecialSummonTargetFromDeckSuppressed:
                case EffectActionType.SpecialSummonGraveTop:
                case EffectActionType.SpecialSummonGraveTopMonsterFaceDown:
                case EffectActionType.SpecialSummonSelfFromGrave:
                case EffectActionType.RevealTopDeckSummonIfLowLevel:
                case EffectActionType.SetTargetMonstersFromHandFaceDown:
                case EffectActionType.SpecialSummonSelfFromHand:
                    return player.FreeMonsterZones();
                case EffectActionType.SummonIllusionTokensToOpponent:
                case EffectActionType.SpecialSummonTargetToOpponentField:
                    return player.Opponent.FreeMonsterZones();
                case EffectActionType.SetTargetSpellFromDeck:
                case EffectActionType.SetTargetSpellFromHand:
                case EffectActionType.SetTargetSpellFromGraveyard:
                    return FreeZoneCount(player.SpellZones);
                case EffectActionType.SetTargetArtifactFromDeck:
                case EffectActionType.PlaceTargetArtifactFromGraveyard:
                    return FreeZoneCount(player.ArtifactZones);
                default:
                    return int.MaxValue;
            }
        }

        /// <summary>Kann diese Aktion eine EIGENE Zone räumen (Opferkosten, Bounce, Selbst-Abgang)?</summary>
        private static bool ActionMayFreeOwnZone(EffectActionType type)
        {
            switch (type)
            {
                case EffectActionType.DestroyTargetMonster:
                case EffectActionType.BanishTargetMonster:
                case EffectActionType.BanishTarget:
                case EffectActionType.BanishSelf:
                case EffectActionType.ReturnTargetToHand:
                case EffectActionType.ReturnTargetCardToHand:
                case EffectActionType.SendSelfToGraveyard:
                case EffectActionType.TributeSelfSpecialSummonTarget:
                case EffectActionType.ShuffleTargetIntoDeck:
                    return true;
                default:
                    return false;
            }
        }

        private static int FreeZoneCount(CardInstance[] zones)
        {
            int free = 0;
            for (int i = 0; i < zones.Length; i++)
                if (zones[i] == null) free++;
            return free;
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
            // Road to 1000: Feldkarten spielen mit ihrem EFFEKTIVEN Level (Demoted for
            // Cause, Promotion Board) — außerhalb des Feldes ist das das gedruckte.
            if (action.levelFilter > 0 && (monster == null
                || (card.Zone == ZoneType.MonsterZone ? card.EffectiveLevel : monster.level) != action.levelFilter)) return false;
            if (action.maxAtkFilter > 0 && (monster == null || card.CurrentAtk > action.maxAtkFilter)) return false;
            if (!string.IsNullOrEmpty(action.nameFilter) && !card.Name.Contains(action.nameFilter)) return false;
            if (!string.IsNullOrEmpty(action.mentionsFilter) && !CardMentions(card, action.mentionsFilter)) return false;
            // Rally the Weak: nur Vanillas — Karten ohne jeden Effekt-Eintrag
            if (action.onlyWithoutEffects && card.Definition != null
                && (card.Definition.effects.Count > 0
                    || (card.MonsterData != null && card.MonsterData.canSelfSpecialSummon))) return false;
            // --- Road to 1000 ---
            // Cut Down to Size: Level unter der Monsterzahl des eigenen Kontrolleurs
            if (action.requireLevelBelowControllerCount && (monster == null || card.Owner == null
                || card.EffectiveLevel >= card.Owner.MonsterCount())) return false;
            // Eviction Notice: nur Monster, die (gerade) nicht angreifen können
            if (action.onlyCannotAttack && (monster == null
                || !(card.CannotAttackThisTurn
                     || (card.Definition != null && card.Definition.passiveCannotAttack)))) return false;
            // Regent / Long Live the King: nur Monster mit 0 gedruckter ATK
            if (action.zeroAtkOnly && (monster == null || monster.atk != 0)) return false;
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
            || action.maxAtkFilter > 0 || action.onlyWithoutEffects
            || action.requireLevelBelowControllerCount || action.onlyCannotAttack || action.zeroAtkOnly
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
                case TargetKind.GraveyardSpellOpponent:
                    candidates.AddRange(player.Opponent.Graveyard.Where(c => c.SpellData != null));
                    break;
                case TargetKind.GraveyardMonsterOpponent:
                    candidates.AddRange(player.Opponent.Graveyard.Where(c => c.MonsterData != null));
                    break;
                case TargetKind.AllySpellOrArtifact:
                    candidates.AddRange(player.SpellsOnField());
                    candidates.AddRange(player.ArtifactsOnField());
                    break;
                case TargetKind.HandSpellFiltered:
                    candidates.AddRange(player.Hand.Where(c => c.SpellData != null));
                    break;
                case TargetKind.ExtraDeckReliquarySelf:
                    candidates.AddRange(player.ExtraDeckPile.Where(c => c.Definition is ReliquaryCardData));
                    break;
                case TargetKind.EnemyReliquaryOnField:
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.Definition is ReliquaryCardData));
                    break;
                // --- The Small Print: Zonen-Ziele (Auto-Wahl, kein Dialog) ---
                case TargetKind.AdjacentAllyMonsters:
                    if (source != null) candidates.AddRange(source.AdjacentMonsters());
                    break;
                case TargetKind.FacingEnemyMonster:
                {
                    // Nur offene Gegenüber; maxAtkFilter -1 = "mit weniger ATK als die Quellkarte" (Hangman)
                    var facing = source?.FacingMonster();
                    if (facing != null && !facing.FaceDown
                        && (action.maxAtkFilter >= 0 || facing.CurrentAtk < source.CurrentAtk)) candidates.Add(facing);
                    break;
                }
                case TargetKind.EnemyMonsterWithLien:
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.LienAmount > 0));
                    break;
                case TargetKind.AnyMonsterWithLien:
                    candidates.AddRange(player.Monsters().Where(m => m.LienAmount > 0));
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.LienAmount > 0));
                    break;
                case TargetKind.EnemyLevel1Monster:
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.MonsterData != null && m.EffectiveLevel == 1));
                    break;
                case TargetKind.EnemyDefenseMonster:
                    candidates.AddRange(player.Opponent.Monsters().Where(m => m.Position == BattlePosition.Defense));
                    break;
                // --- 5 Archetypes ---
                case TargetKind.AllyCountdownCard:
                    candidates.AddRange(player.FieldCards().Where(c => c.CountdownMarkers > 0));
                    break;
                case TargetKind.AnyArtifactOnField:
                    candidates.AddRange(player.ArtifactZones.Where(a => a != null));
                    candidates.AddRange(player.Opponent.ArtifactZones.Where(a => a != null));
                    break;
                // TargetKind.SelfCard: kein Auswahl-Dialog — wird in ResolveEffectActions direkt zur Quellkarte.
            }
            if (ActionHasFilter(action)) candidates.RemoveAll(c => !MatchesFilter(action, c));
            // The Ascetic: bei 1 oder weniger Handkarten des Besitzers für den Gegner unanvisierbar
            candidates.RemoveAll(c => c != null && c.Owner != player && !c.FaceDown && IsOnField(c)
                && c.Definition != null && c.Definition.passiveLowHandImmunity && c.Owner.Hand.Count <= 1);
            if (action.targetExcludesSelf && source != null) candidates.Remove(source);
            // Trapline: "mit anderem Namen" — gleichnamige Karten sind kein Ziel
            if (action.excludeSameName && source != null)
                candidates.RemoveAll(c => c != null && c.Name == source.Name);
            // Ziel-Immunität gilt nur gegen den Gegner — eigene Effekte dürfen weiter anvisieren
            candidates.RemoveAll(c => c != null && (c.CannotBeTargetedThisTurn || c.ImmuneToOpponentThisTurn) && c.Owner != player);
            // Immortal Demon: dauerhaft unanvisierbar für den Gegner (Feldkarten)
            candidates.RemoveAll(c => c != null && c.Owner != player && !c.FaceDown
                && c.Definition != null && c.Definition.passiveUntargetable && IsOnField(c));
            // Heavenly Bodyguard: benannte Karten sind für den Gegner kein gültiges Ziel
            candidates.RemoveAll(c => c != null && c.Owner != player && IsGuardedFromTargeting(c));
            // The Even Scales: solange die LP-Waage im Lot ist, kein gegnerisches Ziel
            candidates.RemoveAll(c => c != null && c.Owner != player && !c.FaceDown && IsOnField(c)
                && c.Definition != null && c.Definition.passiveUntargetableWhileLpClose > 0
                && Math.Abs(c.Owner.LifePoints - c.Owner.Opponent.LifePoints) <= c.Definition.passiveUntargetableWhileLpClose);
            // Emergency Barrier: ein offener Feld-Beschützer macht ALLE Karten seines
            // Besitzers (sich selbst eingeschlossen) für gegnerische Effekte unanvisierbar.
            // Nicht-zielende Effekte (destroy all, Mill) bleiben davon unberührt.
            candidates.RemoveAll(c => c != null && c.Owner != player && IsOnField(c)
                && HasAllCardsProtection(c.Owner));
            return candidates;
        }

        private IEnumerator CollectTargets(PlayerState player, EffectDefinition effect, TargetCollection result, bool canCancel, CardInstance source = null)
        {
            bool earlierMayFree = false;
            // Anker für Nachbar-/Gegenüber-Ziele: die Quellkarte, wenn sie ein Feldmonster
            // ist — sonst (Zauber wie Lock Shields/Stare Down) das zuletzt gewählte Ziel.
            CardInstance anchor = source != null && source.MonsterData != null && IsOnField(source) ? source : null;
            for (int i = 0; i < effect.actions.Count; i++)
            {
                var action = effect.actions[i];
                bool mayFreeAfter = ActionMayFreeOwnZone(action.type);
                if (action.target == TargetKind.None || action.target == TargetKind.SelfCard)
                {
                    if (mayFreeAfter) earlierMayFree = true;
                    continue;
                }
                // Münzwurf-/Deal-Ziele werden erst nach Wurf bzw. Wahl gewählt (ResolveEffectActions)
                if (action.coinGate != CoinGate.None || action.dealGate != DealGate.None)
                { if (mayFreeAfter) earlierMayFree = true; continue; }
                // "Dieselben Ziele wie eben": die letzte zielende Aktion davor kopieren
                if (action.target == TargetKind.SameAsPrevious)
                {
                    for (int back = i - 1; back >= 0; back--)
                        if (result.PerAction.TryGetValue(back, out var earlier) && earlier.Count > 0)
                        { result.PerAction[i] = new List<CardInstance>(earlier); break; }
                    if (mayFreeAfter) earlierMayFree = true;
                    continue;
                }

                var candidates = BuildTargetCandidates(action, player, IsAutoTarget(action.target) ? anchor : source);
                if (candidates.Count == 0) { if (mayFreeAfter) earlierMayFree = true; continue; }

                // The Small Print: Nachbarn/Gegenüber sind durch die Geometrie bestimmt — kein Dialog
                if (IsAutoTarget(action.target))
                {
                    result.PerAction[i] = new List<CardInstance>(candidates);
                    foreach (var chosen in candidates) result.RecordSnapshot(chosen);
                    if (mayFreeAfter) earlierMayFree = true;
                    continue;
                }

                // Zonen-Klammer: mehr Ziele als freie Plätze gibt es nicht zu wählen —
                // außer eine frühere Aktion desselben Effekts schafft selbst Platz.
                int capacity = earlierMayFree ? int.MaxValue : FreeCapacityFor(action.type, player);
                int targetCount = Math.Clamp(Math.Min(action.targetCount, capacity), 1, candidates.Count);
                if (mayFreeAfter) earlierMayFree = true;
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
                // Zauber ohne eigene Zone: das gewählte Monster wird zum Anker für "adjacent"/"facing"
                bool sourceIsFieldMonster = source != null && source.MonsterData != null && IsOnField(source);
                if (!sourceIsFieldMonster && request.Result.Count > 0 && request.Result[0] != null
                    && request.Result[0].MonsterData != null && IsOnField(request.Result[0]))
                    anchor = request.Result[0];
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

                // The Small Print: Münzwurf-Gate — nur bei passendem letzten Wurf
                if (action.coinGate == CoinGate.Heads && !lastCoinHeads) continue;
                if (action.coinGate == CoinGate.Tails && lastCoinHeads) continue;
                // Splithoof: Deal-Gate — nur die Option, die der Gegner gewählt hat
                if (action.dealGate == DealGate.OptionA && !lastDealChoseA) continue;
                if (action.dealGate == DealGate.OptionB && lastDealChoseA) continue;

                // Münzwurf-/Deal-Ziele werden erst NACH dem Wurf bzw. der Wahl gewählt
                // (YuGiOh: "flip a coin — Heads: destroy 1 monster"): CollectTargets hat
                // sie ausgelassen, hier holt der Aktivierende sie nach — Gate offen.
                if ((action.coinGate != CoinGate.None || action.dealGate != DealGate.None)
                    && action.target != TargetKind.None
                    && action.target != TargetKind.SelfCard && action.target != TargetKind.SameAsPrevious
                    && (chosen == null || chosen.Count == 0))
                {
                    var late = BuildTargetCandidates(action, player, source);
                    if (late.Count == 0) { Log($"{source.Name}: nothing to choose — this part does nothing."); continue; }
                    List<CardInstance> picked;
                    if (IsAutoTarget(action.target)) picked = late;
                    else
                    {
                        int lateCount = Math.Clamp(action.targetCount, 1, late.Count);
                        var lateRequest = new TargetRequest
                        {
                            Title = lateCount > 1
                                ? $"\"{effect.label}\" — choose {lateCount} targets"
                                : $"\"{effect.label}\" — choose target",
                            Kind = action.target,
                            Count = lateCount,
                            AllowFewer = action.upToTargets,
                            AllowCancel = false
                        };
                        lateRequest.Candidates.AddRange(late);
                        yield return DecideRouted(player, lateRequest);
                        picked = new List<CardInstance>(lateRequest.Result);
                        if (presenter != null && picked.Count > 0) yield return presenter.ShowTargetsFlash(picked);
                    }
                    chosen = picked;
                    targets.PerAction[i] = picked;
                    target = picked.Count > 0 ? picked[0] : null;
                    affected.Clear();
                    affected.AddRange(picked);
                }

                switch (action.type)
                {
                    // ================== THE SMALL PRINT (Nachzügler) ==================
                    case EffectActionType.DamageSelf:
                        DealDamage(player, action.amount, source.Name);
                        if (CheckWin()) yield break;
                        break;
                    case EffectActionType.DestroyAllEnemyMonsters:
                        foreach (var foe in new List<CardInstance>(player.Opponent.Monsters()))
                        {
                            if (!IsOnField(foe)) continue;
                            if (IsProtectedFromEffectDestruction(foe, player)) { Log($"{foe.Name} is protected and cannot be destroyed by card effects."); continue; }
                            Log($"{source.Name} destroys {foe.Name}.");
                            yield return DestroyCard(foe);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.DestroyAllOtherOwnMonsters:
                        foreach (var own in new List<CardInstance>(player.Monsters()))
                        {
                            if (own == source || !IsOnField(own)) continue;
                            if (IsProtectedFromEffectDestruction(own, player)) { Log($"{own.Name} is protected and cannot be destroyed by card effects."); continue; }
                            Log($"{source.Name} destroys {own.Name}.");
                            yield return DestroyCard(own);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.SpecialSummonTargetFromDeckSuppressed:
                        foreach (var pick in affected)
                        {
                            if (pick == null || pick.MonsterData == null || pick.Zone != ZoneType.Deck) continue;
                            if (player.FreeMonsterZones() <= 0) { Log("No free monster zone — no further summons."); break; }
                            player.DeckPile.Remove(pick);
                            pick.EffectsNegated = true;
                            pick.CannotAttackThisTurn = true;
                            yield return SpecialSummonToField(player, pick, "from the deck — bound and silent", action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                            if (IsOnField(pick)) Log($"{pick.Name}'s effects are negated until the End Phase, and it cannot attack this turn.");
                        }
                        Shuffle(player.DeckPile);
                        break;
                    case EffectActionType.PickTargetOnly:
                        break; // reine Zielwahl — die nächste Aktion greift auf targets.PerAction[i] zu
                    case EffectActionType.NegateAllOpponentCards:
                    {
                        int silenced = 0;
                        foreach (var foeCard in new List<CardInstance>(player.Opponent.FieldCards()))
                        {
                            if (foeCard == null || foeCard.FaceDown || foeCard.EffectsNegated || !IsOnField(foeCard)) continue;
                            foeCard.EffectsNegated = true;
                            silenced++;
                        }
                        Log(silenced > 0
                            ? $"{source.Name}: the effects of {silenced} card(s) {player.Opponent.Name} controls are negated until the end of the turn!"
                            : $"{source.Name}: {player.Opponent.Name} controls nothing to silence.");
                        BoardChanged();
                        break;
                    }
                    case EffectActionType.GainAtkOfFacingMonsterEot:
                        foreach (var hit in affected)
                        {
                            if (hit == null || !IsOnField(hit) || hit.MonsterData == null) continue;
                            var across = hit.FacingMonster();
                            if (across == null || across.FaceDown) { Log($"{hit.Name} stares at an empty zone — nothing happens."); continue; }
                            int gain = across.CurrentAtk * Math.Max(1, action.amount) / 100;
                            hit.TempAtkBonus += gain;
                            Log($"{hit.Name} stares down {across.Name} and gains +{gain} ATK until end of turn ({hit.CurrentAtk}).");
                        }
                        break;
                    case EffectActionType.DiscardSelfRandom:
                        for (int d = 0; d < Math.Max(1, action.amount) && player.Hand.Count > 0; d++)
                        {
                            var lost = player.Hand[rng.Next(player.Hand.Count)];
                            if (presenter != null) yield return presenter.ShowCardSentToGrave(lost);
                            MoveToGraveyard(lost);
                            Log($"{player.Name} discards {lost.Name} at random.");
                        }
                        break;
                    case EffectActionType.HealSelfPerCount:
                    {
                        int tallied = Math.Min(CountFor(action.countKind, player), Math.Max(1, action.targetCount));
                        int healed = tallied * action.amount;
                        if (healed <= 0) { Log($"{source.Name}: nothing to count — no LP gained."); break; }
                        player.LifePoints += healed;
                        Log($"{player.Name} gains {healed} LP ({tallied} × {action.amount}) ({player.LifePoints} LP).");
                        OnLifeChanged?.Invoke(player, healed);
                        break;
                    }

                    // ================== THE SMALL PRINT ==================
                    case EffectActionType.FlipCoin:
                    {
                        bool heads = false;
                        yield return FlipCoin(player, source, h => heads = h);
                        lastCoinHeads = heads;
                        if (Result != DuelResult.None) yield break;
                        break;
                    }
                    case EffectActionType.PayLifePoints:
                        PayLife(player, action.amount, source.Name);
                        if (CheckWin()) yield break;
                        break;
                    case EffectActionType.DrainSelfManaNextTurn:
                        player.LoanDebt += action.amount;
                        Log($"{player.Name} owes {action.amount} Mana next turn (unpaid Mana costs 1500 LP each).");
                        break;
                    case EffectActionType.PlaceLienOnTarget:
                        foreach (var hit in affected)
                        {
                            if (hit == null || !IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.LienAmount += Math.Max(1, action.amount);
                            Log($"A Lien of {hit.LienAmount} Mana is placed on {hit.Name} — its controller pays each Standby Phase or loses it.");
                        }
                        BoardChanged();
                        break;
                    case EffectActionType.RaiseLienOnTarget:
                        foreach (var hit in affected)
                        {
                            if (hit == null || !IsOnField(hit) || hit.LienAmount <= 0) continue;
                            hit.LienAmount += Math.Max(1, action.amount);
                            Log($"The Lien on {hit.Name} rises to {hit.LienAmount} Mana.");
                        }
                        BoardChanged();
                        break;
                    case EffectActionType.SwapControlWithTarget:
                    {
                        // Ziel-Aktion i = das GEGNERISCHE Monster; das eigene steckt in der
                        // vorigen Aktion (AllyMonster) — oder ist die Quellkarte selbst (Changeling Cradle).
                        CardInstance mine = null;
                        if (i > 0 && targets.PerAction.TryGetValue(i - 1, out var ownPick) && ownPick.Count > 0
                            && ownPick[0] != null && ownPick[0].Owner == player && IsOnField(ownPick[0])) mine = ownPick[0];
                        if (mine == null && source.MonsterData != null && IsOnField(source) && source.Owner == player) mine = source;
                        var theirs = affected.Count > 0 ? affected[0] : null;
                        if (mine == null || theirs == null || !IsOnField(theirs) || theirs.Owner == player)
                        {
                            Log($"{source.Name}: no trade possible — the effect fizzles.");
                            break;
                        }
                        // Beide Zonen räumen, dann kreuzweise einsetzen — so braucht niemand eine freie Zone
                        var mineOwner = mine.Owner; var theirOwner = theirs.Owner;
                        int mineIndex = mine.ZoneIndex, theirIndex = theirs.ZoneIndex;
                        mineOwner.MonsterZones[mineIndex] = theirs; theirs.Owner = mineOwner; theirs.ControlReturnsTo = null;
                        theirOwner.MonsterZones[theirIndex] = mine; mine.Owner = theirOwner; mine.ControlReturnsTo = null;
                        theirs.SummonedThisTurn = true; theirs.CannotAttackThisTurn = true;
                        mine.SummonedThisTurn = true;
                        Log($"{player.Name} trades {mine.Name} for {theirs.Name} — control of both changes hands for good.");
                        // amount 2 = Fair Trade (Infused): das hergegebene Monster ist bis zur End Phase annulliert
                        if (action.amount >= 2 && !mine.EffectsNegated)
                        {
                            mine.EffectsNegated = true;
                            Log($"{mine.Name}'s effects are negated until the End Phase.");
                        }
                        BoardChanged();
                        break;
                    }
                    case EffectActionType.GiveSelfToOpponent:
                        if (source.MonsterData != null && IsOnField(source) && source.Owner == player)
                        {
                            TransferControlPermanently(source, player.Opponent);
                            BoardChanged();
                        }
                        break;
                    case EffectActionType.SpecialSummonFromOpponentGraveyard:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.MonsterData == null || hit.Zone != ZoneType.Graveyard) continue;
                            if (player.FreeMonsterZones() <= 0) { Log($"{player.Name} has no free zone — {hit.Name} stays in the Graveyard."); break; }
                            string robbed = hit.Owner.Name;
                            hit.Owner.Graveyard.Remove(hit);
                            hit.Owner = player;
                            hit.BanishWhenLeavingField = true;
                            hit.CannotAttackThisTurn = true;
                            yield return SpecialSummonToField(player, hit, $"from {robbed}'s Graveyard — poached with {source.Name}", action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.MoveSelfToZone:
                        yield return MoveMonsterToZone(player, source, action.amount == 1, source.Name);
                        if (Result != DuelResult.None) yield break;
                        break;
                    case EffectActionType.MoveTargetToZone:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Owner != player) continue;
                            yield return MoveMonsterToZone(player, hit, false, source.Name);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    case EffectActionType.ExtraPositionChangeThisTurn:
                        if (IsOnField(source))
                        {
                            source.PositionChangesUsed = Math.Max(0, source.PositionChangesUsed - Math.Max(1, action.amount));
                            Log($"{source.Name} may change its battle position again this turn.");
                        }
                        break;
                    case EffectActionType.SkipOwnNextDrawPhase:
                        player.SkipNextDrawPhase = true;
                        Log($"{player.Name} will skip the next Draw Phase.");
                        break;
                    case EffectActionType.ShuffleBothHandsRedraw:
                    {
                        foreach (var side in new[] { player, player.Opponent })
                        {
                            int count = side.Hand.Count;
                            if (count == 0) continue;
                            foreach (var card in new List<CardInstance>(side.Hand))
                            {
                                side.Hand.Remove(card);
                                card.Zone = ZoneType.Deck;
                                side.DeckPile.Add(card);
                            }
                            Shuffle(side.DeckPile);
                            Log($"{side.Name} shuffles {count} card(s) back and draws {count}.");
                            if (!TryDraw(side, count)) yield break;
                            yield return PresentDraws(side);
                        }
                        if (action.amount > 0)
                        {
                            if (!TryDraw(player, action.amount)) yield break;
                            yield return PresentDraws(player);
                        }
                        break;
                    }
                    case EffectActionType.DeclareTypeRevealTop:
                    {
                        var declare = new OptionRequest { Title = $"{source.Name}: declare a card type", Card = source, AllowCancel = false };
                        declare.Options.Add("Monster");
                        declare.Options.Add("Spell");
                        declare.Options.Add("Artifact");
                        yield return DecideRouted(player, declare);
                        int pick = Math.Clamp(declare.Result, 0, 2);
                        string picked = declare.Options[pick];
                        Log($"{player.Name} declares: {picked}.");
                        player.RevealedCardThisTurn = true; // Road to 1000: She Reads the Weather
                        int reveal = Math.Max(1, action.amount);
                        for (int r = 0; r < reveal && player.DeckPile.Count > 0; r++)
                        {
                            var top = player.DeckPile[0];
                            player.DeckPile.RemoveAt(0);
                            bool match = (pick == 0 && top.MonsterData != null)
                                      || (pick == 1 && top.SpellData != null)
                                      || (pick == 2 && top.ArtifactData != null);
                            if (match)
                            {
                                top.Zone = ZoneType.Hand;
                                player.Hand.Add(top);
                                Log($"{player.Name} reveals {top.Name} — sworn true, it goes to the hand.");
                            }
                            else
                            {
                                Log($"{player.Name} reveals {top.Name} — sworn false, it goes to the Graveyard.");
                                MoveToGraveyard(top);
                            }
                            if (presenter != null) yield return presenter.ShowCardMoved(top);
                        }
                        BoardChanged();
                        break;
                    }
                    case EffectActionType.RedirectManaFromChainLink:
                    {
                        // Das Glied VOR diesem: chainCards[^1] ist die Quellkarte selbst
                        if (chainCards.Count >= 2)
                        {
                            var link = chainCards[chainCards.Count - 2];
                            if (link != null && link.Owner != player)
                            {
                                link.ManaRedirectedTo = player;
                                Log($"{source.Name}: whatever Mana {link.Name} yields goes to {player.Name} instead.");
                            }
                        }
                        break;
                    }
                    case EffectActionType.NegatePreviousChainLink:
                    {
                        if (chainCards.Count >= 2)
                        {
                            var link = chainCards[chainCards.Count - 2];
                            if (link != null && !link.EffectsNegated && link.Owner != player)
                            {
                                link.EffectsNegated = true;
                                chainNegatedCards.Add(link);
                                Log($"{source.Name} negates {link.Name}'s activation!");
                                BoardChanged();
                            }
                        }
                        break;
                    }
                    case EffectActionType.EndBattlePhaseNow:
                        endBattlePhaseRequested = true;
                        Log($"{source.Name}: the Battle Phase ends.");
                        break;
                    case EffectActionType.DoubleBattleDamageUntilNextTurnEnd:
                        // Bis zum Ende des NÄCHSTEN Zuges des Aktivierenden = die
                        // laufende (gegnerische) Battle Phase und die eigene danach.
                        doubleBattleDamageUntilTurn = TurnPlayer == player ? TurnNumber + 2 : TurnNumber + 1;
                        Log($"{source.Name}: all battle damage is doubled until the end of {player.Name}'s next turn.");
                        break;
                    case EffectActionType.GrantPiercingThisTurn:
                    {
                        // Ohne Ziel: alle eigenen Monster (Trample the Line Infused); amount > 1 = EOT-ATK-Bonus dazu
                        var pierced = action.target == TargetKind.None ? new List<CardInstance>(player.Monsters()) : affected;
                        foreach (var hit in pierced)
                        {
                            if (hit == null || !IsOnField(hit)) continue;
                            hit.PiercingThisTurn = true;
                            if (action.amount > 1) hit.TempAtkBonus += action.amount;
                            Log(action.amount > 1
                                ? $"{hit.Name} gains piercing and +{action.amount} ATK until end of turn ({hit.CurrentAtk})."
                                : $"{hit.Name} gains piercing this turn.");
                        }
                        break;
                    }
                    case EffectActionType.LockOwnSpellsThisTurn:
                        player.SpellsLockedThisTurn = true;
                        Log($"{player.Name} cannot activate other Spells this turn.");
                        break;
                    case EffectActionType.DebuffAdjacentPermanent:
                        foreach (var hit in source.AdjacentMonsters())
                        {
                            hit.PermanentAtkBonus -= action.amount;
                            hit.PermanentDefBonus -= action.amount;
                            Log($"{hit.Name} loses {action.amount} ATK and DEF as {source.Name} falls.");
                        }
                        BoardChanged();
                        break;

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
                        // Skimmed Off the Top: der Gewinn geht an den Skimmer — in dessen
                        // Zug sofort, sonst als Guthaben für seinen nächsten Zug
                        if (source.ManaRedirectedTo != null && source.ManaRedirectedTo != player)
                        {
                            var thief = source.ManaRedirectedTo;
                            if (TurnPlayer == thief) { thief.Mana += action.amount; Log($"{thief.Name} skims {action.amount} Mana from {source.Name} ({thief.Mana} Mana)."); }
                            else { thief.ManaCredit += action.amount; Log($"{thief.Name} skims {action.amount} Mana from {source.Name} — banked for next turn."); }
                            break;
                        }
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
                        // Schleife statt Einzelziel — "bis zu 3 aus dem Deck" (Rally the Weak)
                        foreach (var pick in affected)
                        {
                            if (pick == null || pick.MonsterData == null || pick.Zone != ZoneType.Deck) continue;
                            if (player.FreeMonsterZones() <= 0) { Log("No free monster zone — no further summons."); break; }
                            player.DeckPile.Remove(pick);
                            yield return SpecialSummonToField(player, pick, "from the deck", action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                        }
                        Shuffle(player.DeckPile);
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

                    case EffectActionType.DetonateChargedSpell:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.SpellData == null || !hit.FaceDown
                                || hit.Zone != ZoneType.SpellZone) continue;
                            if (hit.SetThisTurn)
                            {
                                Log($"{hit.Name} was set this turn — the fuse is too fresh.");
                                continue;
                            }
                            int chargedIdx = ChargedEffectIndex(hit);
                            if (chargedIdx < 0) continue;
                            Log($"{source.Name} shortcuts the fuse — {hit.Name} detonates!");
                            yield return ActivateSpell(hit.Owner, hit, chargedIdx, fromHand: false);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;

                    case EffectActionType.CopySpellFromOpponentGraveyard:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.SpellData == null || hit.Zone != ZoneType.Graveyard) continue;
                            var copied = hit.Definition != null && hit.Definition.effects.Count > 0
                                ? hit.Definition.effects[0] : null;
                            if (copied == null) continue;
                            Log($"{player.Name} re-enacts {hit.Name} from the opponent's Graveyard!");
                            // Quelle bleibt die Grave-Karte: Selbstbezüge (BanishSelf & Co.)
                            // treffen das fremde Original, nie den Nachahmer. Kosten-Aktionen
                            // des kopierten Effekts fallen bewusst NICHT an.
                            var copyTargets = new TargetCollection();
                            yield return CollectTargets(player, copied, copyTargets, true, hit);
                            if (copyTargets.Cancelled) continue;
                            yield return ResolveEffectActions(hit, copied, player, copyTargets);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;

                    case EffectActionType.AllyMonsterCopiesTargetStats:
                        if (target != null && target.MonsterData != null && IsOnField(target))
                        {
                            var recipientRequest = new TargetRequest
                            {
                                Title = $"Choose your monster to copy {target.Name}'s stats",
                                Kind = TargetKind.AllyMonster,
                                Count = 1,
                                AllowCancel = false
                            };
                            recipientRequest.Candidates.AddRange(player.Monsters().Where(m => !m.FaceDown && m != target));
                            if (recipientRequest.Candidates.Count == 0)
                            {
                                Log($"{player.Name} controls no monster to receive the copied stats.");
                                break;
                            }
                            yield return DecideRouted(player, recipientRequest);
                            if (recipientRequest.Result.Count == 0) break;
                            var recipient = recipientRequest.Result[0];
                            recipient.StatsOverriddenThisTurn = true;
                            recipient.OverriddenAtk = target.CurrentAtk;
                            recipient.OverriddenDef = target.CurrentDef;
                            Log($"{recipient.Name} copies {target.Name}'s stats ({recipient.CurrentAtk}/{recipient.CurrentDef}).");
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

                    // ================== DARK-ANGEL-PAKET ==================

                    case EffectActionType.ForbidChosenNameTwoTurns:
                    {
                        // Namenswahl über das Suchfeld: der Pool sind alle Karten-
                        // namen des Spiels (DeclarableNames wird beim Aufsetzen des
                        // Prozesses gefüllt), das UI filtert beim Tippen.
                        var namePick = new OptionRequest
                        {
                            Title = "Declare a card name",
                            Card = source,
                            AllowCancel = false,
                            Searchable = true
                        };
                        namePick.Options.AddRange(DeclarableNames);
                        if (namePick.Options.Count == 0) { Log("No card names to declare."); break; }
                        yield return DecideRouted(player, namePick);
                        if (namePick.Result < 0 || namePick.Result >= namePick.Options.Count) break;
                        string forbidden = namePick.Options[namePick.Result];
                        ForbiddenNames[forbidden] = TurnNumber + 1; // dieser + nächster Zug
                        Log($"{player.Name} declares \"{forbidden}\" — its effects are forbidden this turn and the next.");
                        break;
                    }

                    case EffectActionType.SkipOwnBattlePhaseNextTurn:
                        player.SkipBattlePhaseAfterTurn = TurnNumber;
                        Log($"{player.Name} will skip their next Battle Phase.");
                        break;

                    case EffectActionType.BanishAllOpponentMonsters:
                        foreach (var enemy in new List<CardInstance>(player.Opponent.Monsters()))
                        {
                            if (DissolveIfToken(enemy)) continue; // Illusionen lösen sich auf
                            RemoveFromCurrentZone(enemy);
                            DetachEquipsToGraveyard(enemy);
                            enemy.FaceDown = false;
                            enemy.Zone = ZoneType.Banished;
                            enemy.Owner.Banished.Add(enemy);
                            Log($"{enemy.Name} is banished.");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.NoDirectAttacksThisTurnSelf:
                        player.NoDirectAttacksThisTurn = true;
                        Log($"{player.Name} cannot attack directly this turn.");
                        break;

                    case EffectActionType.BanishFromExtraDeckCost:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.ExtraDeck) continue;
                            hit.Owner.ExtraDeckPile.Remove(hit);
                            hit.Zone = ZoneType.Banished;
                            hit.Owner.Banished.Add(hit);
                            Log($"{player.Name} banishes {hit.Name} from the Extra Deck.");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.ReturnTargetReliquaryToExtraDeck:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            if (ReturnToExtraDeck(hit))
                                Log($"{hit.Name} returns to the Extra Deck.");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.LockOpponentSpecialSummonedEffects:
                        player.Opponent.SpecialSummonedEffectsLockedThisTurn = true;
                        Log($"{player.Opponent.Name} cannot activate effects of Special Summoned monsters for the rest of this turn.");
                        break;

                    case EffectActionType.SwitchAllToAttack:
                        // "Every monster" heisst jedes: verdeckte werden dabei
                        // aufgedeckt (ohne Flip-Trigger — der Ruck kommt von aussen).
                        foreach (var side in new[] { player, player.Opponent })
                        {
                            foreach (var monster in side.Monsters())
                            {
                                if (!monster.FaceDown && monster.Position == BattlePosition.Attack) continue;
                                if (monster.Definition != null && monster.Definition.passiveCannotChangePosition) continue;
                                bool wasHidden = monster.FaceDown;
                                monster.FaceDown = false;
                                monster.Position = BattlePosition.Attack;
                                Log(wasHidden
                                    ? $"{monster.Name} is flipped face-up and switches to Attack Position."
                                    : $"{monster.Name} switches to Attack Position.");
                            }
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.ReturnAllBanishedToOwners:
                    {
                        int returned = 0;
                        foreach (var side in new[] { player, player.Opponent })
                        {
                            foreach (var exiled in new List<CardInstance>(side.Banished))
                            {
                                side.Banished.Remove(exiled);
                                if (exiled.OriginalOwner != null) exiled.Owner = exiled.OriginalOwner;
                                exiled.FaceDown = false;
                                if (exiled.Definition is ReliquaryCardData)
                                {
                                    exiled.Zone = ZoneType.ExtraDeck;
                                    exiled.Owner.ExtraDeckPile.Add(exiled);
                                }
                                else
                                {
                                    exiled.Zone = ZoneType.Deck;
                                    exiled.Owner.DeckPile.Add(exiled);
                                }
                                returned++;
                            }
                        }
                        if (returned > 0)
                        {
                            // Beide Decks mischen — die Rückkehrer sollen nicht unten aufliegen
                            Shuffle(player.DeckPile);
                            Shuffle(player.Opponent.DeckPile);
                            Log($"{returned} banished card(s) return to their owners' Decks and Extra Decks.");
                        }
                        BoardChanged();
                        break;
                    }
                    case EffectActionType.SimultaneousDeckCull:
                        yield return ResolveSimultaneousDeckCull(player, source);
                        if (Result != DuelResult.None) yield break;
                        break;
                    case EffectActionType.PlaySelfFromHand:
                        // Emergency Barrier: die Quellkarte selbst wandert aus der Hand aufs Feld
                        if (source != null && source.ArtifactData != null && player.Hand.Contains(source))
                            yield return ExecutePlayArtifact(player, source, -1);
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
                        if (source.ManaRedirectedTo != null && source.ManaRedirectedTo != player)
                        {
                            source.ManaRedirectedTo.ManaCredit += action.amount;
                            Log($"{source.ManaRedirectedTo.Name} skims {action.amount} Mana from {source.Name} — banked for next turn.");
                            break;
                        }
                        player.ManaCredit += action.amount;
                        Log($"{player.Name} will have {player.ManaCredit} more Mana next turn.");
                        break;
                    case EffectActionType.ReturnFromGraveyardToHand:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Graveyard) continue;
                            if (ReturnToExtraDeck(hit)) continue; // Reliquarys kehren ins Extra Deck zurück
                            if (presenter != null) yield return presenter.ShowCardRevealed(hit, "RETURNED TO HAND");
                            RemoveFromCurrentZone(hit);
                            hit.Zone = ZoneType.Hand;
                            hit.Owner.Hand.Add(hit);
                            Log($"{player.Name} returns {hit.Name} from the graveyard to their hand.");
                        }
                        break;
                    case EffectActionType.AddTargetFromDeckToHand:
                        if (target != null && target.Zone == ZoneType.Deck)
                        {
                            if (presenter != null) yield return presenter.ShowCardRevealed(target, "ADDED FROM THE DECK");
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
                    case EffectActionType.SetTargetSpellFromGraveyard:
                        if (target != null && target.Zone == ZoneType.Graveyard && target.SpellData != null)
                        {
                            int graveSetZone = player.FirstFreeZoneIndex(player.SpellZones);
                            if (graveSetZone < 0) { Log("No free spell zone — the set fizzles."); break; }
                            target.Owner.Graveyard.Remove(target);
                            player.SpellZones[graveSetZone] = target;
                            target.Owner = player;
                            target.Zone = ZoneType.SpellZone;
                            target.FaceDown = true;
                            target.SetThisTurn = false; // darf noch in diesem Zug aktiviert werden
                            target.EffectsNegated = false;
                            Log($"{player.Name} sets a spell from the graveyard (usable this turn).");
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
                            yield return SpecialSummonToField(player, target, origin, action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                            // Long Live the King: der Rückkehrer bleibt diesen Zug friedlich
                            if (action.summonCannotAttack && IsOnField(target))
                            {
                                target.CannotAttackThisTurn = true;
                                Log($"{target.Name} cannot attack this turn.");
                            }
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
                            // Aus Stapeln (Friedhof/Verbannung) zeigt sich die Karte kurz —
                            // vom Feld war sie ohnehin sichtbar, da reicht der Flug.
                            if (presenter != null && (hit.Zone == ZoneType.Graveyard || hit.Zone == ZoneType.Banished))
                                yield return presenter.ShowCardRevealed(hit, "SHUFFLED INTO THE DECK");
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
                            if (presenter != null) yield return presenter.ShowCardRevealed(hit, "SHUFFLED INTO THE DECK");
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
                        // Mit Zielwahl tauntet das ZIEL (Hold the Line); ohne wie bisher die Quellkarte
                        if (affected.Count > 0)
                        {
                            foreach (var baited in affected)
                            {
                                if (!IsOnField(baited) || baited.MonsterData == null) continue;
                                baited.MustBeAttackedThisTurn = true;
                                Log($"{player.Opponent.Name}'s monsters must attack {baited.Name} this turn.");
                            }
                        }
                        else if (IsOnField(source))
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
                            if (hit.Definition != null && hit.Definition.passiveCannotChangePosition) { Log($"{hit.Name} does not budge."); continue; }
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
                                if (m.Definition != null && m.Definition.passiveCannotChangePosition) continue;
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
                            if (presenter != null) yield return presenter.ShowCardRevealed(source, "RETURNED TO HAND");
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
                            if (presenter != null) yield return presenter.ShowCardRevealed(milled, "SALVAGED TO HAND");
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
                            yield return AmplifyOpponentMill(player, millCount);
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
                        if (ReliquarySummonsBlocked()) { Log("Reliquary Summons are sealed — the call fizzles."); break; }
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
                    {
                        int sent = 0;
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Deck) continue;
                            player.DeckPile.Remove(hit);
                            MoveToGraveyard(hit);
                            sent++;
                            Log($"{player.Name} sends {hit.Name} from the Deck to the Graveyard.");
                        }
                        Shuffle(player.DeckPile);
                        if (sent > 0) player.MilledThisTurn = true;
                        yield return AmplifyOpponentMill(player, sent);
                        break;
                    }

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
                        player.RevealedCardThisTurn = true; // Road to 1000: She Reads the Weather
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

                    // ================== ROAD TO 1000 (SEPTEMBER 2026) ==================

                    case EffectActionType.WinTheDuel:
                        Log($"CORONATION! {player.Name} controls the full regalia of the Absent King — {player.Name} wins the Duel!");
                        if (presenter != null) yield return presenter.ShowPhaseBanner("CORONATION!", 1.6f);
                        EndDuelByLoss(player.Opponent);
                        yield break;

                    case EffectActionType.SealEnemyZones:
                        yield return SealZones(player, player.Opponent, Math.Max(1, action.amount), null, source.Name);
                        break;

                    case EffectActionType.SealEnemyZonesWhileSourceFaceUp:
                        yield return SealZones(player, player.Opponent, Math.Max(1, action.amount),
                            IsOnField(source) ? source : null, source.Name);
                        break;

                    case EffectActionType.SealAnyZones:
                        for (int sealed_ = 0; sealed_ < Math.Max(1, action.amount); sealed_++)
                        {
                            bool ownFree = UnsealedFreeMonsterZones(player) > 0;
                            bool foeFree = UnsealedFreeMonsterZones(player.Opponent) > 0;
                            if (!ownFree && !foeFree)
                            {
                                if (sealed_ == 0) Log($"{source.Name}: no empty zone to seal.");
                                break;
                            }
                            var sideToSeal = player.Opponent;
                            if (ownFree && foeFree)
                            {
                                var sideAsk = new OptionRequest { Title = $"{source.Name}: seal a zone on which side?", Card = source };
                                sideAsk.Options.Add("Opponent's side");
                                sideAsk.Options.Add("Your side");
                                yield return DecideRouted(player, sideAsk);
                                if (sideAsk.Result == 1) sideToSeal = player;
                            }
                            else if (ownFree) sideToSeal = player;
                            yield return SealZones(player, sideToSeal, 1, null, source.Name);
                        }
                        break;

                    case EffectActionType.SpecialSummonGraveTop:
                    {
                        var top = player.Graveyard.Count > 0 ? player.Graveyard[player.Graveyard.Count - 1] : null;
                        if (top == null) { Log($"{player.Name}'s Graveyard is empty."); break; }
                        if (top.MonsterData == null
                            || (action.levelFilter > 0 && top.MonsterData.level > action.levelFilter))
                        {
                            Log($"The top card of the Graveyard is {top.Name} — it does not fit; nothing happens.");
                            break;
                        }
                        yield return SpecialSummonToField(player, top, "from the top of the graveyard", action.summonInDefense);
                        if (Result != DuelResult.None) yield break;
                        if (action.summonCannotAttack && IsOnField(top))
                        {
                            top.CannotAttackThisTurn = true;
                            Log($"{top.Name} cannot attack this turn.");
                        }
                        break;
                    }

                    case EffectActionType.SpecialSummonGraveTopMonsterFaceDown:
                    {
                        CardInstance topMonster = null;
                        for (int g = player.Graveyard.Count - 1; g >= 0; g--)
                            if (player.Graveyard[g].MonsterData != null) { topMonster = player.Graveyard[g]; break; }
                        if (topMonster == null) { Log($"{player.Name} has no monster in the Graveyard."); break; }
                        if (player.CannotSpecialSummonThisTurn) { Log($"{player.Name} cannot Special Summon this turn."); break; }
                        if (FieldLimitReached(player, topMonster.Definition)) break;
                        int bootsZone = FirstUnsealedFreeZone(player);
                        if (bootsZone < 0) { Log("No free monster zone — the summon fizzles."); break; }
                        topMonster.Owner.Graveyard.Remove(topMonster);
                        player.MonsterZones[bootsZone] = topMonster;
                        topMonster.Owner = player;
                        topMonster.Zone = ZoneType.MonsterZone;
                        topMonster.Position = BattlePosition.Defense;
                        topMonster.FaceDown = true;
                        topMonster.SummonedThisTurn = true;
                        topMonster.WasSpecialSummoned = true;
                        Log($"{player.Name} Special Summons the top monster of the Graveyard face-down.");
                        BoardChanged();
                        if (presenter != null) yield return presenter.ShowCardMoved(topMonster);
                        break;
                    }

                    case EffectActionType.ReturnGraveTopToHand:
                        for (int taken = 0; taken < Math.Max(1, action.amount) && player.Graveyard.Count > 0; taken++)
                        {
                            var top = player.Graveyard[player.Graveyard.Count - 1];
                            player.Graveyard.Remove(top);
                            top.Zone = ZoneType.Hand;
                            top.FaceDown = false;
                            player.Hand.Add(top);
                            Log($"{player.Name} takes {top.Name} from the top of the Graveyard ({player.Hand.Count} in hand).");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.BanishOpponentGraveTop:
                        for (int taken = 0; taken < Math.Max(1, action.amount) && player.Opponent.Graveyard.Count > 0; taken++)
                        {
                            var top = player.Opponent.Graveyard[player.Opponent.Graveyard.Count - 1];
                            Log($"{source.Name} banishes {top.Name} from the top of {player.Opponent.Name}'s Graveyard.");
                            if (presenter != null) yield return presenter.ShowCardBanished(top);
                            MoveToBanished(top);
                        }
                        break;

                    case EffectActionType.MoveGraveTopToBottom:
                        if (player.Graveyard.Count <= 1) { Log($"{player.Name}'s Graveyard has nothing to reorder."); break; }
                        else
                        {
                            var top = player.Graveyard[player.Graveyard.Count - 1];
                            player.Graveyard.RemoveAt(player.Graveyard.Count - 1);
                            player.Graveyard.Insert(0, top);
                            Log($"{player.Name} slides {top.Name} to the bottom of the Graveyard.");
                            BoardChanged();
                        }
                        break;

                    case EffectActionType.SpecialSummonSelfFromGrave:
                        if (source.Zone == ZoneType.Graveyard && source.MonsterData != null)
                        {
                            yield return SpecialSummonToField(player, source, "from the top of the graveyard", action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                            if (action.summonCannotAttack && IsOnField(source))
                            {
                                source.CannotAttackThisTurn = true;
                                Log($"{source.Name} cannot attack this turn.");
                            }
                        }
                        break;

                    case EffectActionType.ChangeTargetLevelPermanent:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.PermanentLevelBonus += action.amount;
                            Log($"{hit.Name} is now Level {hit.EffectiveLevel} (permanently).");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.SetTargetLevelThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.TempLevelThisTurn = Math.Clamp(action.amount, 1, 3);
                            Log($"{hit.Name} is Level {hit.EffectiveLevel} until the end of the turn.");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.ChooseSelfLevelThisTurn:
                    {
                        var levelAsk = new OptionRequest { Title = $"{source.Name}: choose its Level until the end of the turn", Card = source };
                        levelAsk.Options.Add("Level 1");
                        levelAsk.Options.Add("Level 2");
                        levelAsk.Options.Add("Level 3");
                        yield return DecideRouted(player, levelAsk);
                        int chosenLevel = Math.Clamp(levelAsk.Result + 1, 1, 3);
                        source.TempLevelThisTurn = chosenLevel;
                        Log($"{source.Name} is Level {chosenLevel} until the end of the turn.");
                        BoardChanged();
                        break;
                    }

                    case EffectActionType.DiscountNextNormalSummon:
                        player.NextNormalSummonTributeDiscount = Math.Max(player.NextNormalSummonTributeDiscount, action.amount);
                        Log(action.amount >= 99
                            ? $"{player.Name}'s next Normal Summon this turn requires no tributes."
                            : $"{player.Name}'s next Normal Summon this turn requires {action.amount} fewer tribute(s).");
                        break;

                    case EffectActionType.TickCountdownSelf:
                        if (source.CountdownMarkers > 0)
                        {
                            source.CountdownMarkers = Math.Max(0, source.CountdownMarkers - Math.Max(1, action.amount));
                            Log(source.CountdownMarkers > 0
                                ? $"{source.Name}: an Hour Counter is removed ({source.CountdownMarkers} left)."
                                : $"{source.Name}: the last Hour Counter is removed — the appointed hour has come!");
                            BoardChanged();
                            if (source.CountdownMarkers <= 0)
                            {
                                yield return OfferTriggeredEffects(player, source, EffectTrigger.CountdownZero);
                                if (Result != DuelResult.None) yield break;
                            }
                        }
                        break;

                    case EffectActionType.LookReorderTopDeck:
                        yield return ReorderTopOfDeck(player, player, Math.Max(1, action.amount), source.Name);
                        break;

                    case EffectActionType.LookReorderOpponentTopDeck:
                        yield return ReorderTopOfDeck(player, player.Opponent, Math.Max(1, action.amount), source.Name);
                        break;

                    case EffectActionType.RevealTopDeckSummonIfLowLevel:
                    {
                        if (player.DeckPile.Count == 0) { Log($"{player.Name}'s Deck is empty."); break; }
                        var top = player.DeckPile[0];
                        player.RevealedCardThisTurn = true;
                        Log($"{player.Name} reveals the top card of the Deck: {top.Name}.");
                        if (presenter != null) yield return presenter.ShowCardRevealed(top, "PROPHECY");
                        bool fits = top.MonsterData != null
                            && (action.levelFilter <= 0 || top.MonsterData.level <= action.levelFilter);
                        player.DeckPile.RemoveAt(0);
                        if (fits)
                        {
                            yield return SpecialSummonToField(player, top, "as foretold", action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                            if (IsOnField(top))
                            {
                                if (action.summonCannotAttack)
                                {
                                    top.CannotAttackThisTurn = true;
                                    Log($"{top.Name} cannot attack this turn.");
                                }
                            }
                            else { player.DeckPile.Insert(0, top); Shuffle(player.DeckPile); } // Beschwörung geplatzt: zurück und mischen
                        }
                        else
                        {
                            MoveToGraveyard(top);
                            Log($"{top.Name} does not match the prophecy — it is sent to the Graveyard.");
                            yield return FirePendingGraveTriggers();
                        }
                        BoardChanged();
                        break;
                    }

                    case EffectActionType.RevealOpponentTopDeckMayBottom:
                    {
                        if (player.Opponent.DeckPile.Count == 0) { Log($"{player.Opponent.Name}'s Deck is empty."); break; }
                        var top = player.Opponent.DeckPile[0];
                        player.RevealedCardThisTurn = true;
                        Log($"{player.Opponent.Name}'s top card is revealed: {top.Name}.");
                        if (presenter != null) yield return presenter.ShowCardRevealed(top, "REVEALED");
                        var peek = new YesNoRequest
                        {
                            Title = "Opponent's Deck",
                            Card = top,
                            Question = $"Top card: {top.Name}. Put it on the bottom of their Deck?"
                        };
                        yield return DecideRouted(player, peek);
                        if (peek.Result)
                        {
                            player.Opponent.DeckPile.RemoveAt(0);
                            player.Opponent.DeckPile.Add(top);
                            Log($"{player.Name} slides the revealed card to the bottom of {player.Opponent.Name}'s Deck.");
                        }
                        else Log("The revealed card stays on top.");
                        break;
                    }

                    case EffectActionType.RevealTopDeckTakeMonsters:
                    {
                        int shown = Math.Min(Math.Max(1, action.amount), player.DeckPile.Count);
                        if (shown == 0) { Log($"{player.Name}'s Deck is empty."); break; }
                        player.RevealedCardThisTurn = true;
                        var kept = new List<CardInstance>();
                        for (int r = 0; r < shown; r++)
                        {
                            var card = player.DeckPile[0];
                            player.DeckPile.RemoveAt(0);
                            Log($"{player.Name} reveals {card.Name}.");
                            if (card.MonsterData != null)
                            {
                                card.Zone = ZoneType.Hand;
                                player.Hand.Add(card);
                                Log($"{card.Name} is a monster — it goes to the hand.");
                            }
                            else kept.Add(card);
                        }
                        // Nicht-Monster kehren in ihrer Reihenfolge nach oben zurück
                        for (int back = kept.Count - 1; back >= 0; back--)
                            player.DeckPile.Insert(0, kept[back]);
                        BoardChanged();
                        break;
                    }

                    case EffectActionType.PutTargetHandCardToDeckBottom:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Hand || !player.Hand.Contains(hit)) continue;
                            player.Hand.Remove(hit);
                            hit.Zone = ZoneType.Deck;
                            player.DeckPile.Add(hit);
                            Log($"{player.Name} puts a card on the bottom of the Deck.");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.PutTargetHandCardOnTopOfDeck:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.Hand || !player.Hand.Contains(hit)) continue;
                            player.Hand.Remove(hit);
                            hit.Zone = ZoneType.Deck;
                            player.DeckPile.Insert(0, hit);
                            Log($"{player.Name} puts a card on top of the Deck.");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.RevealOwnHandDrawByContent:
                    {
                        player.RevealedCardThisTurn = true;
                        Log($"{player.Name} reveals their hand: {(player.Hand.Count == 0 ? "(empty)" : string.Join(", ", player.Hand.Select(c => c.Name)))}.");
                        bool anySpell = player.Hand.Any(c => c.SpellData != null);
                        int honest = Math.Max(1, action.amount) + (anySpell ? 0 : 1);
                        Log(anySpell
                            ? $"A Spell among them — {player.Name} draws {honest}."
                            : $"Not a single Spell — {player.Name} draws {honest}.");
                        if (!TryDraw(player, honest)) yield break;
                        yield return PresentDraws(player);
                        break;
                    }

                    case EffectActionType.RevealOwnHandDrawIfEmpty:
                        player.RevealedCardThisTurn = true;
                        Log($"{player.Name} reveals their hand: {(player.Hand.Count == 0 ? "(empty)" : string.Join(", ", player.Hand.Select(c => c.Name)))}.");
                        if (player.Hand.Count == 0)
                        {
                            Log($"An empty purse — {player.Name} draws {Math.Max(1, action.amount)}.");
                            if (!TryDraw(player, Math.Max(1, action.amount))) yield break;
                            yield return PresentDraws(player);
                        }
                        else Log("The purse is not empty — no cards are drawn.");
                        break;

                    case EffectActionType.RevealOwnHandBuffPerMonster:
                    {
                        player.RevealedCardThisTurn = true;
                        Log($"{player.Name} reveals their hand: {(player.Hand.Count == 0 ? "(empty)" : string.Join(", ", player.Hand.Select(c => c.Name)))}.");
                        int shownMonsters = player.Hand.Count(c => c.MonsterData != null);
                        if (shownMonsters > 0 && IsOnField(source) && source.MonsterData != null)
                        {
                            source.PermanentAtkBonus += action.amount * shownMonsters;
                            Log($"{source.Name} permanently gains +{action.amount * shownMonsters} ATK ({shownMonsters} monsters shown) — now {source.CurrentAtk}.");
                        }
                        else if (shownMonsters == 0) Log("No monsters to show — no ATK gained.");
                        BoardChanged();
                        break;
                    }

                    case EffectActionType.OpponentRevealsRandomHandCard:
                        if (player.Opponent.Hand.Count == 0) Log($"{player.Opponent.Name}'s hand is empty.");
                        else
                        {
                            player.RevealedCardThisTurn = true;
                            for (int r = 0; r < Math.Max(1, action.amount) && player.Opponent.Hand.Count > 0; r++)
                            {
                                var shownCard = player.Opponent.Hand[rng.Next(player.Opponent.Hand.Count)];
                                Log($"{player.Opponent.Name} reveals {shownCard.Name} at random.");
                                if (presenter != null) yield return presenter.ShowCardRevealed(shownCard, "REVEALED");
                            }
                        }
                        break;

                    case EffectActionType.BothRevealHandsDrawIfOpponentMore:
                        player.RevealedCardThisTurn = true;
                        player.Opponent.RevealedCardThisTurn = true;
                        Log($"{player.Name} reveals their hand: {(player.Hand.Count == 0 ? "(empty)" : string.Join(", ", player.Hand.Select(c => c.Name)))}.");
                        Log($"{player.Opponent.Name} reveals their hand: {(player.Opponent.Hand.Count == 0 ? "(empty)" : string.Join(", ", player.Opponent.Hand.Select(c => c.Name)))}.");
                        if (player.Opponent.Hand.Count > player.Hand.Count)
                        {
                            Log($"{player.Opponent.Name} holds more — {player.Name} draws {Math.Max(1, action.amount)}.");
                            if (!TryDraw(player, Math.Max(1, action.amount))) yield break;
                            yield return PresentDraws(player);
                        }
                        break;

                    case EffectActionType.OpponentRevealsHandDrawIfMore:
                        player.RevealedCardThisTurn = true;
                        Log($"{player.Opponent.Name} reveals their hand: {(player.Opponent.Hand.Count == 0 ? "(empty)" : string.Join(", ", player.Opponent.Hand.Select(c => c.Name)))}.");
                        if (player.Opponent.Hand.Count > player.Hand.Count)
                        {
                            Log($"{player.Opponent.Name} holds more — {player.Name} draws {Math.Max(1, action.amount)}.");
                            if (!TryDraw(player, Math.Max(1, action.amount))) yield break;
                            yield return PresentDraws(player);
                        }
                        break;

                    case EffectActionType.GrantAttacksWithDefThisTurn:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit) || hit.MonsterData == null) continue;
                            hit.AttacksWithDefThisTurn = true;
                            if (action.amount > 0) hit.TempDefBonus += action.amount;
                            Log(action.amount > 0
                                ? $"{hit.Name} leads with the shield — it attacks with its DEF this turn and gains +{action.amount} DEF ({hit.CurrentDef})."
                                : $"{hit.Name} leads with the shield — it attacks with its DEF this turn ({hit.CurrentDef}).");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.TaxOpponentNextSpellThisTurn:
                        player.Opponent.NextSpellSurcharge += Math.Max(1, action.amount);
                        Log($"The next Spell {player.Opponent.Name} activates this turn costs {player.Opponent.NextSpellSurcharge} more Mana.");
                        break;

                    case EffectActionType.MoveEnemyTargetToZone:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.MonsterZone || hit.Owner == player) continue;
                            if (BearerZoneLocked(hit)) { Log($"{hit.Name} is locked in place and cannot be moved."); continue; }
                            var hitSide = hit.Owner;
                            int fromZone = hit.ZoneIndex;
                            var freeFoe = new List<int>();
                            for (int z = 0; z < hitSide.MonsterZones.Length; z++)
                                if (hitSide.MonsterZones[z] == null && !IsZoneSealed(hitSide, z)) freeFoe.Add(z);
                            if (freeFoe.Count == 0) { Log($"{hit.Name} has nowhere to be moved."); continue; }
                            int destination = freeFoe[0];
                            if (freeFoe.Count > 1)
                            {
                                var queueRequest = new ZoneSelectRequest
                                {
                                    Title = $"{source.Name}: choose the new zone for {hit.Name}",
                                    ForPlayer = hitSide,
                                    Zone = ZoneType.MonsterZone
                                };
                                queueRequest.FreeIndices.AddRange(freeFoe);
                                yield return DecideRouted(player, queueRequest);
                                if (freeFoe.Contains(queueRequest.Result)) destination = queueRequest.Result;
                            }
                            presenter?.RememberOrigin(hit);
                            hitSide.MonsterZones[fromZone] = null;
                            hitSide.MonsterZones[destination] = hit;
                            Log($"{hit.Name} is shoved to zone {destination + 1} — wrong queue, sir.");
                            BoardChanged();
                            if (presenter != null) yield return presenter.ShowCardMoved(hit);
                            yield return OfferTriggeredEffects(hitSide, hit, EffectTrigger.OnMovedSelf);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;

                    case EffectActionType.RotateOwnMonsters:
                    {
                        if (player.MonsterCount() == 0) { Log($"{player.Name} controls no monsters to rotate."); break; }
                        var directionAsk = new OptionRequest { Title = $"{source.Name}: rotate your monsters which way?", Card = source };
                        directionAsk.Options.Add("Left");
                        directionAsk.Options.Add("Right");
                        yield return DecideRouted(player, directionAsk);
                        bool right = directionAsk.Result == 1;
                        int moved = 0;
                        var movedCards = new List<CardInstance>();
                        if (right)
                        {
                            for (int z = player.MonsterZones.Length - 2; z >= 0; z--)
                            {
                                var mover = player.MonsterZones[z];
                                if (mover == null || BearerZoneLocked(mover)) continue;
                                if (player.MonsterZones[z + 1] != null || IsZoneSealed(player, z + 1)) continue;
                                player.MonsterZones[z] = null;
                                player.MonsterZones[z + 1] = mover;
                                moved++; movedCards.Add(mover);
                            }
                        }
                        else
                        {
                            for (int z = 1; z < player.MonsterZones.Length; z++)
                            {
                                var mover = player.MonsterZones[z];
                                if (mover == null || BearerZoneLocked(mover)) continue;
                                if (player.MonsterZones[z - 1] != null || IsZoneSealed(player, z - 1)) continue;
                                player.MonsterZones[z] = null;
                                player.MonsterZones[z - 1] = mover;
                                moved++; movedCards.Add(mover);
                            }
                        }
                        Log(moved > 0
                            ? $"{player.Name} turns the table — {moved} monster(s) slide one zone to the {(right ? "right" : "left")}."
                            : "The table does not budge — no monster could move.");
                        BoardChanged();
                        foreach (var mover in movedCards)
                        {
                            yield return OfferTriggeredEffects(player, mover, EffectTrigger.OnMovedSelf);
                            if (Result != DuelResult.None) yield break;
                        }
                        if (action.amount == 1 && moved >= 3)
                        {
                            Log($"Three or more moved — {player.Name} draws 1 card.");
                            if (!TryDraw(player, 1)) yield break;
                            yield return PresentDraws(player);
                        }
                        break;
                    }

                    case EffectActionType.SetBothLifeToLower:
                    {
                        int lower = Math.Min(player.LifePoints, player.Opponent.LifePoints);
                        foreach (var duelist in new[] { player, player.Opponent })
                        {
                            int delta = lower - duelist.LifePoints;
                            if (delta == 0) continue;
                            duelist.LifePoints = lower;
                            Log($"{duelist.Name}'s LP are set to {lower}.");
                            OnLifeChanged?.Invoke(duelist, delta);
                        }
                        Log($"{source.Name}: the scales are settled at {lower} LP.");
                        BoardChanged();
                        if (CheckWin()) yield break;
                        break;
                    }

                    case EffectActionType.HealHalfLpDifference:
                    {
                        int gap = Math.Abs(player.LifePoints - player.Opponent.LifePoints);
                        int healGain = Math.Min(gap / 2, Math.Max(1, action.amount));
                        if (healGain <= 0) { Log("The scales are already even — no LP gained."); break; }
                        player.LifePoints += healGain;
                        Log($"{player.Name} gains {healGain} LP ({player.LifePoints} LP).");
                        OnLifeChanged?.Invoke(player, healGain);
                        break;
                    }

                    case EffectActionType.SetTargetMonstersFromHandFaceDown:
                    {
                        int placed = 0;
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.MonsterData == null || !player.Hand.Contains(hit)) continue;
                            if (FieldLimitReached(player, hit.Definition)) continue;
                            int lieZone = -1;
                            yield return ChooseZone(player, player.MonsterZones, ZoneType.MonsterZone,
                                "Choose a zone for the face-down monster", -1, z => lieZone = z);
                            if (lieZone < 0) { Log("No free monster zone — no further cards can be set."); break; }
                            presenter?.RememberView(hit);
                            player.Hand.Remove(hit);
                            player.MonsterZones[lieZone] = hit;
                            hit.Zone = ZoneType.MonsterZone;
                            hit.Position = BattlePosition.Defense;
                            hit.FaceDown = true;
                            hit.SummonedThisTurn = true;
                            placed++;
                            Log($"{player.Name} sets a monster face-down in Defense Position.");
                            BoardChanged();
                            if (presenter != null) yield return presenter.ShowCardMoved(hit);
                        }
                        if (action.amount == 1 && placed > 1)
                        {
                            Log($"{placed - 1} beyond the first — {player.Name} draws {placed - 1}.");
                            if (!TryDraw(player, placed - 1)) yield break;
                            yield return PresentDraws(player);
                        }
                        break;
                    }

                    case EffectActionType.DrawIfHandAtMost:
                        if (player.Hand.Count <= Math.Max(0, action.amount))
                        {
                            Log($"{player.Name}'s hand is light enough — 1 card is drawn.");
                            if (!TryDraw(player, 1)) yield break;
                            yield return PresentDraws(player);
                        }
                        else Log($"{player.Name} holds more than {action.amount} cards — no draw.");
                        break;

                    // ================== 5 ARCHETYPES (SEPTEMBER 2026) ==================

                    case EffectActionType.OfferDeal:
                    {
                        // Splithoof: der GEGNER wählt — beide Optionen stehen auf der Karte
                        var contract = new OptionRequest
                        {
                            Title = $"{source.Name}: {player.Opponent.Name} must choose",
                            Card = source,
                            AllowCancel = false
                        };
                        contract.Options.Add(string.IsNullOrEmpty(action.dealOptionA) ? "Option A" : action.dealOptionA);
                        contract.Options.Add(string.IsNullOrEmpty(action.dealOptionB) ? "Option B" : action.dealOptionB);
                        yield return DecideRouted(player.Opponent, contract);
                        lastDealChoseA = contract.Result != 1;
                        player.DealsThisTurn++;
                        player.DealsThisDuel++;
                        Log($"{player.Opponent.Name} signs the deal of {source.Name}: \"{contract.Options[lastDealChoseA ? 0 : 1]}\".");
                        break;
                    }

                    case EffectActionType.SwapStrongestMonsters:
                    {
                        CardInstance Strongest(PlayerState side) => side.Monsters()
                            .Where(m => !m.FaceDown).OrderByDescending(m => m.CurrentAtk).FirstOrDefault();
                        var mine = Strongest(player);
                        var theirs = Strongest(player.Opponent);
                        if (mine == null || theirs == null)
                        {
                            Log($"{source.Name}: both sides need a face-up monster — the trade falls through.");
                            break;
                        }
                        int myZone = mine.ZoneIndex, theirZone = theirs.ZoneIndex;
                        player.MonsterZones[myZone] = theirs;
                        player.Opponent.MonsterZones[theirZone] = mine;
                        mine.Owner = player.Opponent;
                        theirs.Owner = player;
                        mine.ControlReturnsTo = null;
                        theirs.ControlReturnsTo = null;
                        mine.SummonedThisTurn = true;   // frisch beim neuen Kontrolleur
                        theirs.SummonedThisTurn = true;
                        Log($"{source.Name}: {mine.Name} and {theirs.Name} swap control — fair and square.");
                        BoardChanged();
                        break;
                    }

                    case EffectActionType.OpponentSendsStrongestToGrave:
                    {
                        var doomed = player.Opponent.Monsters()
                            .Where(m => !m.FaceDown).OrderByDescending(m => m.CurrentAtk).FirstOrDefault();
                        if (doomed == null) { Log($"{player.Opponent.Name} controls no face-up monster."); break; }
                        Log($"{player.Opponent.Name} sends {doomed.Name} to the Graveyard — the contract is explicit.");
                        if (presenter != null) yield return presenter.ShowCardSentToGrave(doomed);
                        MoveToGraveyardWithEquips(doomed);
                        BoardChanged();
                        yield return FirePendingGraveTriggers();
                        break;
                    }

                    case EffectActionType.DrawPerCount:
                    {
                        int tally = Math.Min(CountFor(action.countKind, player), Math.Max(1, action.targetCount));
                        if (tally <= 0) { Log($"{source.Name}: nothing to count — no cards drawn."); break; }
                        Log($"{player.Name} draws {tally} card(s).");
                        if (!TryDraw(player, tally)) yield break;
                        yield return PresentDraws(player);
                        break;
                    }

                    case EffectActionType.TopDeckWager:
                    {
                        if (player.DeckPile.Count == 0 || player.Opponent.DeckPile.Count == 0)
                        { Log("Both players need a card in the Deck to wager."); break; }
                        var myBet = player.DeckPile[0];
                        var theirBet = player.Opponent.DeckPile[0];
                        player.RevealedCardThisTurn = true;
                        int myLevel = myBet.MonsterData != null ? myBet.MonsterData.level : 0;
                        int theirLevel = theirBet.MonsterData != null ? theirBet.MonsterData.level : 0;
                        Log($"{player.Name} reveals {myBet.Name} (Level {myLevel}), {player.Opponent.Name} reveals {theirBet.Name} (Level {theirLevel}).");
                        if (myLevel == theirLevel)
                        {
                            Log("A draw — both cards stay on top.");
                            break;
                        }
                        var winner = myLevel > theirLevel ? player : player.Opponent;
                        var winCard = myLevel > theirLevel ? myBet : theirBet;
                        var loseCard = myLevel > theirLevel ? theirBet : myBet;
                        winner.DeckPile.Remove(winCard);
                        winCard.Zone = ZoneType.Hand;
                        winner.Hand.Add(winCard);
                        loseCard.Owner.DeckPile.Remove(loseCard);
                        MoveToGraveyard(loseCard);
                        Log($"{winner.Name} wins the wager and takes {winCard.Name}; {loseCard.Name} goes to the Graveyard.");
                        BoardChanged();
                        yield return FirePendingGraveTriggers();
                        if (Result != DuelResult.None) yield break;
                        if (action.amount >= 1 && winner == player)
                        {
                            Log($"{player.Name} draws 1 for winning the wager.");
                            if (!TryDraw(player, 1)) yield break;
                            yield return PresentDraws(player);
                        }
                        break;
                    }

                    case EffectActionType.SpecialSummonTargetToOpponentField:
                        // Giftwyrm: Zustellung frei Haus — keine Summon-Trigger (siehe
                        // ExecuteSelfSpecialSummon), der Empfänger kontrolliert, der
                        // Zusteller bleibt Besitzer.
                        foreach (var parcel in affected)
                        {
                            if (parcel == null || parcel.MonsterData == null) continue;
                            var receiver = player.Opponent;
                            if (player.CannotSpecialSummonThisTurn)
                            { Log($"{player.Name} cannot Special Summon this turn."); break; }
                            if (receiver.MonsterCount() >= MonsterCapFor(receiver))
                            { Log($"{receiver.Name}'s field is at its decreed limit — no delivery."); break; }
                            int slot = FirstUnsealedFreeZone(receiver);
                            if (slot < 0) { Log($"{receiver.Name} has no free zone — no delivery."); break; }
                            bool fromDeck = parcel.Zone == ZoneType.Deck;
                            RemoveFromCurrentZone(parcel);
                            receiver.MonsterZones[slot] = parcel;
                            parcel.Owner = receiver;
                            parcel.Zone = ZoneType.MonsterZone;
                            parcel.FaceDown = false;
                            parcel.Position = action.summonInDefense ? BattlePosition.Defense
                                : parcel.MonsterData.selfSummonPosition;
                            parcel.SummonedThisTurn = true;
                            parcel.WasSpecialSummoned = true;
                            parcel.WasDisloyalWhenLeftField = false;
                            ArmCountdown(parcel);
                            Log($"{player.Name} delivers {parcel.Name} to {receiver.Name}'s field.");
                            BoardChanged();
                            if (presenter != null) yield return presenter.ShowCardMoved(parcel);
                            if (fromDeck) Shuffle(player.DeckPile);
                        }
                        break;

                    case EffectActionType.ReclaimOwnFromOpponentField:
                    {
                        // Giftwyrm Molting/Hamper: die Geschenke schlüpfen und kommen heim
                        var delivered = player.Opponent.Monsters()
                            .Where(m => m.OriginalOwner == player
                                && (string.IsNullOrEmpty(action.nameFilter) || m.Name.Contains(action.nameFilter)))
                            .ToList();
                        if (delivered.Count == 0) { Log($"{source.Name}: nothing of yours lives over there."); break; }

                        List<CardInstance> reclaiming;
                        int want = Math.Min(Math.Max(1, action.targetCount), delivered.Count);
                        if (want >= delivered.Count) reclaiming = delivered;
                        else
                        {
                            var pick = new TargetRequest
                            {
                                Title = $"\"{effect.label}\" — choose up to {want} to reclaim",
                                Kind = TargetKind.EnemyMonster,
                                Count = want,
                                AllowFewer = true,
                                AllowCancel = false
                            };
                            pick.Candidates.AddRange(delivered);
                            yield return DecideRouted(player, pick);
                            reclaiming = new List<CardInstance>(pick.Result);
                        }

                        int reclaimed = 0;
                        foreach (var gift in reclaiming)
                        {
                            if (gift == null || gift.Zone != ZoneType.MonsterZone) continue;
                            if (player.MonsterCount() >= MonsterCapFor(player))
                            { Log($"{player.Name}'s field is at its decreed limit — the rest stays."); break; }
                            int home = FirstUnsealedFreeZone(player);
                            if (home < 0) { Log($"{player.Name} has no free zone — the rest stays."); break; }
                            RemoveFromZoneArray(player.Opponent.MonsterZones, gift);
                            player.MonsterZones[home] = gift;
                            gift.Owner = player;
                            gift.SummonedThisTurn = true;
                            gift.WasSpecialSummoned = true;
                            if (action.amount > 0) gift.TempAtkBonus += action.amount;
                            reclaimed++;
                            Log(action.amount > 0
                                ? $"{gift.Name} hatches and returns to {player.Name} (+{action.amount} ATK until end of turn)."
                                : $"{gift.Name} hatches and returns to {player.Name}.");
                        }
                        if (reclaimed > 0)
                        {
                            BoardChanged();
                            if (presenter != null) yield return DuelWait.For(0.2f);
                        }
                        break;
                    }

                    case EffectActionType.SpecialSummonSelfFromHand:
                        // Waylay-Ambush: die Quellkarte springt aus der Hand aufs Feld
                        if (source.Zone == ZoneType.Hand && player.Hand.Contains(source))
                        {
                            player.Hand.Remove(source);
                            yield return SpecialSummonToField(player, source, "from the bushes", action.summonInDefense);
                            if (Result != DuelResult.None) yield break;
                            if (!IsOnField(source))
                            {
                                // Beschwörung geplatzt (kein Platz/Cap) — zurück in die Hand
                                source.Zone = ZoneType.Hand;
                                player.Hand.Add(source);
                            }
                        }
                        break;

                    case EffectActionType.CancelAttackTarget:
                        foreach (var hit in affected)
                        {
                            if (!IsOnField(hit)) continue;
                            hit.CannotAttackThisTurn = true;
                            Log($"{hit.Name} cannot attack this turn — the attack is called off.");
                        }
                        break;

                    case EffectActionType.DebuffAllEnemyAtkEot:
                        foreach (var foe in player.Opponent.Monsters())
                        {
                            if (foe.FaceDown) continue;
                            foe.TempAtkBonus -= Math.Max(0, action.amount);
                            Log($"{foe.Name} loses {action.amount} ATK until the end of the turn ({foe.CurrentAtk}).");
                        }
                        BoardChanged();
                        break;

                    case EffectActionType.ExemptFromDecree:
                        foreach (var hit in affected)
                        {
                            if (hit == null || hit.Zone != ZoneType.ArtifactZone) continue;
                            hit.DecreeExemptFor = player;
                            Log($"{player.Name} slips through a loophole — {hit.Name} does not apply to them this turn.");
                        }
                        break;

                    case EffectActionType.TickCountdownTarget:
                        foreach (var clock in affected)
                        {
                            if (clock == null || clock.CountdownMarkers <= 0) continue;
                            clock.CountdownMarkers = Math.Max(0, clock.CountdownMarkers - Math.Max(1, action.amount));
                            Log(clock.CountdownMarkers > 0
                                ? $"{clock.Name}: an Hour Counter is removed ({clock.CountdownMarkers} left)."
                                : $"{clock.Name}: the last Hour Counter is removed!");
                            BoardChanged();
                            if (clock.CountdownMarkers <= 0)
                            {
                                yield return StrikeCountdown(clock.Owner, clock);
                                if (Result != DuelResult.None) yield break;
                            }
                        }
                        break;

                    case EffectActionType.StrikeAllOwnCountdowns:
                    {
                        var clocks = player.FieldCards().Where(c => c.CountdownMarkers > 0).ToArray();
                        if (clocks.Length == 0) { Log($"{source.Name}: no clock is ticking."); break; }
                        Log($"{source.Name}: MIDNIGHT — every bell rings at once!");
                        foreach (var clock in clocks)
                        {
                            if (!IsOnField(clock) || clock.CountdownMarkers <= 0) continue;
                            yield return StrikeCountdown(player, clock);
                            if (Result != DuelResult.None) yield break;
                        }
                        break;
                    }
                }
            }
            BoardChanged();
        }

        /// <summary>
        /// Road to 1000: die obersten Karten eines Decks ansehen und in Wunschreihenfolge
        /// zurücklegen. Der Wählende bestimmt Karte für Karte, was zuoberst liegt.
        /// </summary>
        private IEnumerator ReorderTopOfDeck(PlayerState chooser, PlayerState deckOwner, int count, string sourceName)
        {
            int seen = Math.Min(count, deckOwner.DeckPile.Count);
            if (seen == 0) { Log($"{deckOwner.Name}'s Deck is empty."); yield break; }
            chooser.RevealedCardThisTurn = true;

            var pool = new List<CardInstance>();
            for (int i = 0; i < seen; i++) { pool.Add(deckOwner.DeckPile[0]); deckOwner.DeckPile.RemoveAt(0); }
            Log(chooser == deckOwner
                ? $"{chooser.Name} looks at the top {seen} card(s) of the Deck."
                : $"{chooser.Name} looks at the top {seen} card(s) of {deckOwner.Name}'s Deck.");

            var newOrder = new List<CardInstance>();
            while (pool.Count > 1)
            {
                var pick = new TargetRequest
                {
                    Title = $"{sourceName}: choose the card that goes ON TOP next",
                    Kind = TargetKind.None,
                    Count = 1,
                    AllowCancel = false
                };
                pick.Candidates.AddRange(pool);
                yield return DecideRouted(chooser, pick);
                var chosen = pick.Result.Count > 0 && pool.Contains(pick.Result[0]) ? pick.Result[0] : pool[0];
                newOrder.Add(chosen);
                pool.Remove(chosen);
            }
            newOrder.AddRange(pool);

            for (int back = newOrder.Count - 1; back >= 0; back--)
                deckOwner.DeckPile.Insert(0, newOrder[back]);
            Log($"The top {seen} card(s) return in the chosen order.");
        }

        /// <summary>Erste freie UND unversiegelte Monster-Zone (-1 = keine).</summary>
        private int FirstUnsealedFreeZone(PlayerState player)
        {
            for (int i = 0; i < player.MonsterZones.Length; i++)
                if (player.MonsterZones[i] == null && !IsZoneSealed(player, i)) return i;
            return -1;
        }

        // ================== HELFER: 5 ARCHETYPES (SEPTEMBER 2026) ==================

        /// <summary>
        /// Bylaw: Wirkt dieses Dekret auf diesen Spieler? Nein, wenn es verdeckt,
        /// annulliert, per Loophole ausgesetzt ist — oder sein Besitzer den
        /// Letter of the Law kontrolliert und selbst der Geprüfte ist.
        /// </summary>
        public bool DecreeApplies(CardInstance decree, PlayerState side)
        {
            if (decree == null || side == null || decree.FaceDown || decree.EffectsNegated) return false;
            if (decree.DecreeExemptFor == side) return false;
            if (decree.Owner == side && decree.Name.StartsWith("Bylaw:"))
                foreach (var card in side.Monsters())
                    if (!card.FaceDown && card.Definition != null && card.Definition.passiveDecreesSpareOwner)
                        return false;
            return true;
        }

        /// <summary>Bylaw (Standing Room Only): Monster-Obergrenze für diese Seite (Standard: Zonenzahl).</summary>
        private int MonsterCapFor(PlayerState side)
        {
            int cap = side.MonsterZones.Length;
            foreach (var half in new[] { Player1, Player2 })
            {
                if (half == null) continue;
                foreach (var decree in half.ArtifactZones)
                {
                    int limit = decree?.Definition != null ? decree.Definition.passiveMonsterCapBoth : 0;
                    if (limit > 0 && limit < cap && DecreeApplies(decree, side)) cap = limit;
                }
            }
            return cap;
        }

        /// <summary>
        /// Waylay Tollgate + Bylaw Quiet Hours: Mana-Zoll auf den ERSTEN Angriff
        /// dieses Spielers in der laufenden Battle Phase (0 = frei).
        /// </summary>
        private int AttackTollFor(PlayerState attackerSide)
        {
            if (attackerSide.AttacksDeclaredThisBattle > 0) return 0;
            int toll = 0;
            // Tollgate: kassiert nur Angriffe des GEGNERS seines Besitzers
            foreach (var gate in attackerSide.Opponent.ArtifactZones)
                if (gate != null && !gate.FaceDown && !gate.EffectsNegated && gate.Definition != null)
                    toll += gate.Definition.passiveAttackToll;
            // Quiet Hours: kassiert beide Seiten — sofern das Dekret hier greift
            foreach (var half in new[] { Player1, Player2 })
            {
                if (half == null) continue;
                foreach (var decree in half.ArtifactZones)
                    if (decree?.Definition != null && decree.Definition.passiveAttackTaxBoth > 0
                        && DecreeApplies(decree, attackerSide))
                        toll += decree.Definition.passiveAttackTaxBoth;
            }
            return toll;
        }

        /// <summary>Chimekeep: Countdown-Marker beim Betreten des Feldes aufziehen.</summary>
        private void ArmCountdown(CardInstance card)
        {
            if (card?.Definition != null && card.Definition.countdownMarkers > 0)
                card.CountdownMarkers = card.Definition.countdownMarkers;
        }

        /// <summary>Chimekeep: der Nullschlag einer Karte — Marker weg, Effekt feuert.</summary>
        private IEnumerator StrikeCountdown(PlayerState owner, CardInstance clock)
        {
            clock.CountdownMarkers = 0;
            owner.CountdownStruckThisTurn = true;
            Log($"{clock.Name}: the last Hour Counter is removed — the appointed hour has come!");
            BoardChanged();
            yield return OfferTriggeredEffects(owner, clock, EffectTrigger.CountdownZero);
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
            yield return AmplifyOpponentMill(player, actual);
        }

        // Exponential Deterioration millt gerade nach — der Nachschlag darf sich
        // nicht selbst wieder auslösen ("except by the effect from this card").
        private bool amplifyingMill;

        /// <summary>
        /// Exponential Deterioration: hat der GEGNER des Millers ein offenes
        /// Verstärker-Artefakt liegen, schickt der Miller je Vorgang N Karten
        /// hinterher. Ein Vorgang, ein Nachschlag — auch bei mehreren Karten.
        /// </summary>
        private IEnumerator AmplifyOpponentMill(PlayerState miller, int milledCount)
        {
            if (milledCount <= 0 || amplifyingMill) yield break;
            int amplify = 0;
            foreach (var artifact in miller.Opponent.ArtifactZones)
            {
                if (artifact == null || artifact.FaceDown || artifact.Definition == null) continue;
                if (artifact.EffectsNegated) continue;
                amplify += artifact.Definition.passiveOpponentMillAmplify;
            }
            if (amplify <= 0) yield break;

            amplifyingMill = true;
            Log($"{miller.Opponent.Name}'s Exponential Deterioration bites — {miller.Name} sends {amplify} more card(s).");
            yield return MillDeck(miller, amplify);
            amplifyingMill = false;
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
            bool isSpellCast = card?.SpellData != null && effect.trigger == EffectTrigger.OnActivate;
            // Guild Tariff: Zauber kosten für BEIDE 1 mehr — auch die sonst gratis wären
            if (isSpellCast && SpellTaxActive()) cost += 1;
            // Countersign: der nächste Zauber dieses Spielers trägt den Aufschlag
            if (isSpellCast && player.NextSpellSurcharge > 0) cost += player.NextSpellSurcharge;
            // Giftwyrm Prettybow: jedes offene Steuer-Monster auf der EIGENEN Seite
            // verteuert die eigenen Zauber — das Geschenk kassiert seinen Wirt
            if (isSpellCast)
                foreach (var leech in player.Monsters())
                    if (!leech.FaceDown && !leech.EffectsNegated && leech.Definition != null
                        && leech.Definition.passiveSpellTaxOnController) cost += 1;
            if (cost <= 0) return cost;
            if (!isSpellCast) return cost;
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

        // ================== HELFER: THE SMALL PRINT (AUGUST 2026) ==================

        /// <summary>Liegt irgendwo offen ein Guild Tariff?</summary>
        private bool SpellTaxActive()
        {
            foreach (var side in new[] { Player1, Player2 })
            {
                if (side == null) continue;
                foreach (var artifact in side.ArtifactZones)
                    if (artifact != null && !artifact.FaceDown && !artifact.EffectsNegated
                        && artifact.Definition != null
                        && artifact.Definition.passiveSpellTaxBoth) return true;
            }
            return false;
        }

        /// <summary>The Duelist's Code: ATK-Bonus des Angreifers (0 = kein Kodex auf dem Feld).</summary>
        private int OneAttackBonus()
        {
            int best = 0;
            foreach (var side in new[] { Player1, Player2 })
            {
                if (side == null) continue;
                foreach (var artifact in side.ArtifactZones)
                    if (artifact != null && !artifact.FaceDown && artifact.Definition != null
                        && artifact.Definition.passiveOneAttackBonus > best) best = artifact.Definition.passiveOneAttackBonus;
            }
            return best;
        }

        /// <summary>Zonen-Ziele, die die Geometrie bestimmt — kein Auswahl-Dialog.</summary>
        private static bool IsAutoTarget(TargetKind kind) =>
            kind == TargetKind.AdjacentAllyMonsters || kind == TargetKind.FacingEnemyMonster;

        /// <summary>Anzahl Monster, die als Tribut zur Verfügung stehen (ohne "cannot be Tributed").</summary>
        private static int TributableCount(PlayerState player)
        {
            int count = 0;
            foreach (var m in player.Monsters()) if (!CannotBeTributed(m)) count++;
            return count;
        }

        private static bool CannotBeTributed(CardInstance monster) =>
            monster?.Definition != null && monster.Definition.passiveCannotBeTributed;

        /// <summary>Sworn to the Gate: kontrolliert der Spieler eine offene Karte, die ANDERE Spezialbeschwörungen sperrt?</summary>
        private static bool SpecialSummonsLockedFor(PlayerState player)
        {
            foreach (var m in player.Monsters())
                if (!m.FaceDown && m.Definition != null && m.Definition.passiveOwnerNoOtherSpecialSummons) return true;
            return false;
        }

        private bool AnyLienOnField()
        {
            foreach (var side in new[] { Player1, Player2 })
                if (side != null) foreach (var m in side.Monsters()) if (m.LienAmount > 0) return true;
            return false;
        }

        // ================== HELFER: ROAD TO 1000 (ZONEN-SIEGEL) ==================

        /// <summary>
        /// Ist diese Monster-Zone versiegelt? Zählt gelistete Siegel (befristet oder
        /// quellgebunden) UND die virtuellen Padlock-Siegel: die leeren Nachbarzonen
        /// eines Trägers von The Landlord's Own Padlock.
        /// </summary>
        public bool IsZoneSealed(PlayerState side, int index)
        {
            if (side == null || index < 0 || index >= side.MonsterZones.Length) return false;
            foreach (var seal in side.ZoneSeals)
            {
                if (seal.Index != index) continue;
                if (seal.UntilTurn >= 0)
                {
                    if (TurnNumber <= seal.UntilTurn) return true;
                }
                else if (seal.Source != null && !seal.Source.FaceDown
                         && seal.Source.Zone == ZoneType.MonsterZone) return true;
            }
            // The Landlord's Own Padlock: der Träger mauert seine leeren Nachbarzonen zu
            if (side.MonsterZones[index] == null)
                foreach (var bearer in side.Monsters())
                {
                    if (bearer.FaceDown || System.Math.Abs(bearer.ZoneIndex - index) != 1) continue;
                    foreach (var equip in bearer.EquippedArtifacts)
                        if (equip.Definition != null && equip.Definition.passiveSealsAdjacentZones) return true;
                }
            return false;
        }

        /// <summary>Freie UND unversiegelte Monster-Zonen eines Spielers.</summary>
        private int UnsealedFreeMonsterZones(PlayerState side)
        {
            int count = 0;
            for (int i = 0; i < side.MonsterZones.Length; i++)
                if (side.MonsterZones[i] == null && !IsZoneSealed(side, i)) count++;
            return count;
        }

        /// <summary>
        /// Versiegelt bis zu <paramref name="count"/> leere Monster-Zonen. Der
        /// Aktivierende wählt die Zonen; boundTo bindet das Siegel an eine offene
        /// Quellkarte, sonst hält es bis zum Ende seines NÄCHSTEN Zuges.
        /// </summary>
        private IEnumerator SealZones(PlayerState chooser, PlayerState side, int count, CardInstance boundTo, string sourceName)
        {
            // Bis zum Ende des nächsten EIGENEN Zuges des Aktivierenden: im eigenen
            // Zug versiegelt heißt +2 (dieser + der nächste eigene), im Gegnerzug +1.
            int until = TurnNumber + (TurnPlayer == chooser ? 2 : 1);
            for (int placed = 0; placed < count; placed++)
            {
                var free = new List<int>();
                for (int i = 0; i < side.MonsterZones.Length; i++)
                    if (side.MonsterZones[i] == null && !IsZoneSealed(side, i)) free.Add(i);
                if (free.Count == 0)
                {
                    if (placed == 0) Log($"{sourceName}: no empty zone to seal.");
                    yield break;
                }

                int chosen = free[0];
                if (free.Count > 1)
                {
                    var request = new ZoneSelectRequest
                    {
                        Title = $"{sourceName}: choose a zone to seal",
                        ForPlayer = side,
                        Zone = ZoneType.MonsterZone
                    };
                    request.FreeIndices.AddRange(free);
                    yield return DecideRouted(chooser, request);
                    if (free.Contains(request.Result)) chosen = request.Result;
                }

                side.ZoneSeals.Add(new ZoneSeal
                {
                    Index = chosen,
                    UntilTurn = boundTo != null ? -1 : until,
                    Source = boundTo
                });
                Log(boundTo != null
                    ? $"{sourceName} bricks up {side.Name}'s zone {chosen + 1} — sealed while {boundTo.Name} remains face-up."
                    : $"{sourceName} bricks up {side.Name}'s zone {chosen + 1} — sealed until the end of {chooser.Name}'s next turn.");
                BoardChanged();
            }
        }

        /// <summary>Aurel: LP-Kosten des Besitzers sind 0.</summary>
        private static bool LifeCostsFree(PlayerState player)
        {
            foreach (var card in player.FieldCards())
                if (card != null && !card.FaceDown && card.Definition != null && card.Definition.passiveLifeCostsFree) return true;
            return false;
        }

        /// <summary>LP-Kosten sind zahlbar, wenn danach noch Leben übrig bleibt (auf 0 zahlen heißt verlieren).</summary>
        private static bool CanPayLife(PlayerState player, int amount)
        {
            if (amount <= 0 || LifeCostsFree(player)) return true;
            return player.LifePoints > amount;
        }

        private void PayLife(PlayerState player, int amount, string sourceName)
        {
            if (amount <= 0) return;
            if (LifeCostsFree(player))
            {
                Log($"{player.Name} pays no LP for {sourceName} — the debt is collected elsewhere.");
                return;
            }
            int before = player.LifePoints;
            player.LifePoints -= amount;
            Log($"{player.Name} pays {amount} LP for {sourceName} ({player.LifePoints} LP).");
            OnLifeChanged?.Invoke(player, player.LifePoints - before);
        }

        /// <summary>Alle PayLifePoints-Kosten eines Effekts zusammen zahlbar?</summary>
        private static bool CanPayLifeCosts(EffectDefinition effect, PlayerState player)
        {
            int total = 0;
            foreach (var action in effect.actions)
                if (action.type == EffectActionType.PayLifePoints) total += action.amount;
            return CanPayLife(player, total);
        }

        /// <summary>
        /// Ketten-Kontext-Aktionen: NegatePreviousChainLink braucht direkt davor
        /// einen gegnerischen Zauber, RedirectManaFromChainLink ein gegnerisches
        /// Glied mit Mana-Gewinn, EndBattlePhaseNow eine laufende Battle Phase.
        /// </summary>
        private bool ChainContextAllows(EffectDefinition effect, PlayerState player)
        {
            foreach (var action in effect.actions)
            {
                switch (action.type)
                {
                    case EffectActionType.NegatePreviousChainLink:
                    {
                        if (chainCards.Count == 0) return false;
                        var last = chainCards[chainCards.Count - 1];
                        if (last == null || last.Owner == player || last.SpellData == null || last.EffectsNegated) return false;
                        break;
                    }
                    case EffectActionType.RedirectManaFromChainLink:
                    {
                        if (chainCards.Count == 0 || chainEffects.Count == 0) return false;
                        var last = chainCards[chainCards.Count - 1];
                        var lastEffect = chainEffects[chainEffects.Count - 1];
                        if (last == null || last.Owner == player || lastEffect == null) return false;
                        if (last.ManaRedirectedTo != null) return false;
                        bool givesMana = false;
                        foreach (var a in lastEffect.actions)
                            if (!a.isCost && (a.type == EffectActionType.GainMana || a.type == EffectActionType.GainManaNextTurn)) givesMana = true;
                        if (!givesMana) return false;
                        break;
                    }
                    case EffectActionType.EndBattlePhaseNow:
                        if (Phase != DuelPhase.Battle) return false;
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Münzwurf mit allen Kleingedruckten: Loaded Dice (zweimal werfen, wählen —
        /// zwei Tails zerstören die Würfel), House Always Wins (Tails zählt als Heads
        /// im Rückstand). true = Heads.
        /// </summary>
        private IEnumerator FlipCoin(PlayerState player, CardInstance source, System.Action<bool> apply)
        {
            bool heads = rng.Next(2) == 0;

            // Loaded Dice: einmal pro Zug zweimal werfen und aussuchen
            CardInstance dice = null;
            foreach (var card in player.FieldCards())
                if (card != null && !card.FaceDown && card.Definition != null && card.Definition.passiveCoinChoose
                    && !card.CoinChooseUsedThisTurn) { dice = card; break; }
            if (dice != null)
            {
                dice.CoinChooseUsedThisTurn = true;
                bool second = rng.Next(2) == 0;
                Log($"{dice.Name}: {player.Name} flips twice — {(heads ? "Heads" : "Tails")} and {(second ? "Heads" : "Tails")}.");
                if (heads != second)
                {
                    var ask = new YesNoRequest
                    {
                        Title = "Loaded Dice",
                        Card = dice,
                        Question = "The coins disagree — take Heads?"
                    };
                    yield return DecideRouted(player, ask);
                    heads = ask.Result;
                    Log($"{dice.Name}: {player.Name} takes {(heads ? "Heads" : "Tails")}.");
                }
                else if (!heads)
                {
                    Log($"Both flips land Tails — {dice.Name} is destroyed.");
                    yield return DestroyCard(dice);
                }
            }

            // The House Always Wins: im Rückstand zählt Tails als Heads
            if (!heads && player.LifePoints < player.Opponent.LifePoints)
            {
                foreach (var card in player.FieldCards())
                    if (card != null && !card.FaceDown && card.Definition != null && card.Definition.passiveTailsAsHeadsWhenBehind)
                    {
                        Log($"{card.Name}: the house calls it Heads.");
                        heads = true;
                        break;
                    }
            }

            Log($"{player.Name} flips a coin for {source.Name}: {(heads ? "HEADS" : "TAILS")}.");
            if (presenter != null) yield return presenter.ShowPhaseBanner(heads ? "HEADS" : "TAILS", 0.9f);
            apply(heads);
        }

        /// <summary>Zieht ein Monster in eine andere eigene Zone (Small Print: adjacent/facing wird lebendig).</summary>
        private IEnumerator MoveMonsterToZone(PlayerState player, CardInstance monster, bool adjacentOnly, string reason)
        {
            if (monster == null || monster.Zone != ZoneType.MonsterZone || monster.Owner != player) yield break;
            // The Landlord's Own Padlock: der Träger ist festgeschraubt
            if (BearerZoneLocked(monster)) { Log($"{monster.Name} is locked in place and cannot move."); yield break; }
            int from = monster.ZoneIndex;
            var free = new List<int>();
            for (int i = 0; i < player.MonsterZones.Length; i++)
            {
                if (player.MonsterZones[i] != null) continue;
                if (adjacentOnly && Math.Abs(i - from) != 1) continue;
                if (IsZoneSealed(player, i)) continue; // Road to 1000: zugemauerte Zonen
                free.Add(i);
            }
            if (free.Count == 0) { Log($"{monster.Name} has nowhere to move."); yield break; }

            int chosen = free[0];
            if (free.Count > 1)
            {
                var request = new ZoneSelectRequest { Title = $"{reason}: choose a zone for {monster.Name}", ForPlayer = player, Zone = ZoneType.MonsterZone };
                request.FreeIndices.AddRange(free);
                yield return DecideRouted(player, request);
                if (free.Contains(request.Result)) chosen = request.Result;
            }

            presenter?.RememberOrigin(monster);   // Flug von der alten in die neue Zone
            player.MonsterZones[from] = null;
            player.MonsterZones[chosen] = monster;
            Log($"{monster.Name} moves to zone {chosen + 1}.");
            BoardChanged();
            if (presenter != null) yield return presenter.ShowCardMoved(monster);
            yield return OfferTriggeredEffects(player, monster, EffectTrigger.OnMovedSelf);
        }

        /// <summary>The Landlord's Own Padlock: trägt dieses Monster ein zonensperrendes Artefakt?</summary>
        private static bool BearerZoneLocked(CardInstance monster)
        {
            foreach (var equip in monster.EquippedArtifacts)
                if (equip.Definition != null && equip.Definition.passiveBearerZoneLocked) return true;
            return false;
        }

        /// <summary>Dauerhafter Kontrollwechsel — kein Rückfall in der End Phase.</summary>
        private void TransferControlPermanently(CardInstance monster, PlayerState to)
        {
            var from = monster.Owner;
            if (from == to || monster.Zone != ZoneType.MonsterZone) return;
            int freeIndex = to.FirstFreeZoneIndex(to.MonsterZones);
            if (freeIndex < 0) { Log($"{to.Name} has no free zone — {monster.Name} stays put."); return; }
            RemoveFromZoneArray(from.MonsterZones, monster);
            to.MonsterZones[freeIndex] = monster;
            monster.Owner = to;
            monster.ControlReturnsTo = null;
            monster.SummonedThisTurn = true;   // frisch beim neuen Kontrolleur: kein Angriff, keine Positionsänderung diesen Zug
            Log($"{to.Name} takes control of {monster.Name}.");
        }

        /// <summary>Ledger of Small Debts: Karten, deren LP-Schwelle unterschritten ist, fallen.</summary>
        private IEnumerator EnforceLifeThresholds()
        {
            foreach (var side in new[] { Player1, Player2 })
            {
                if (side == null) continue;
                foreach (var card in new List<CardInstance>(side.FieldCards()))
                {
                    int limit = card?.Definition != null ? card.Definition.passiveDestroyWhenLifeAtMost : 0;
                    if (limit <= 0 || side.LifePoints > limit || !IsOnField(card)) continue;
                    Log($"{side.Name}'s LP are at {limit} or less — {card.Name} is destroyed.");
                    yield return DestroyCard(card);
                }
            }
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

        /// <summary>
        /// Im Kampf unzerstörbar: hart (passiveNoBattleDestroy, Immortal Demon)
        /// oder bedingt über Artefakte (Ironclad).
        /// </summary>
        private static bool BattleShieldHolds(CardInstance monster) => BattleShieldReason(monster) != null;

        /// <summary>
        /// Warum ein Monster den Kampf überlebt (null = kein Schutz). Der Text
        /// wandert ins Log, damit "stands firm" nicht immer die Artefakte lobt.
        /// </summary>
        private static string BattleShieldReason(CardInstance monster)
        {
            if (monster?.Definition != null && monster.Definition.passiveNoBattleDestroy)
                return $"{monster.Name} stands firm — it cannot be destroyed by battle.";
            // The Small Print: Sworn to the Gate (allein), Castellan-Nachbarn
            if (monster?.Definition != null && monster.Owner != null && monster.Zone == ZoneType.MonsterZone)
            {
                if (monster.Definition.passiveLoneImmunity && monster.Owner.MonsterCount() <= 1)
                    return $"{monster.Name} stands firm — alone at the gate, its oath holds.";
                foreach (var neighbour in monster.AdjacentMonsters())
                    if (!neighbour.FaceDown && neighbour.Definition != null && neighbour.Definition.passiveAdjacentNoBattleDestroy)
                        return $"{monster.Name} stands firm — {neighbour.Name}'s wall shelters it.";
            }
            int needed = monster?.Definition != null ? monster.Definition.battleShieldMinOwnArtifacts : 0;
            if (needed <= 0 || monster.Owner == null) return null;
            int artifacts = 0;
            foreach (var artifact in monster.Owner.ArtifactZones) if (artifact != null) artifacts++;
            return artifacts >= needed ? $"{monster.Name} stands firm — its artifacts hold the line." : null;
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
                yield return OfferTriggeredEffects(owner, card, EffectTrigger.OnSentToGraveyardSelf, fromZone);
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
            if (chainCards.Count > 0)
            {
                var closing = chainCards[chainCards.Count - 1];
                if (closing != null) closing.ManaRedirectedTo = null;   // Skimmed Off the Top gilt nur diesem Glied
                chainCards.RemoveAt(chainCards.Count - 1);
            }
            if (chainEffects.Count > 0) chainEffects.RemoveAt(chainEffects.Count - 1);
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
                chainEffects.Clear();
                if (presenter != null) yield return presenter.ShowChainEnd();
                // Ledger of Small Debts: LP-Schwellen greifen, sobald die Kette steht
                yield return EnforceLifeThresholds();
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
                    var (owner, card, trigger, graveFromZone) = pendingOffers[0];
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
                    yield return OfferTriggeredEffects(owner, card, trigger, graveFromZone);
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
        private readonly List<(PlayerState owner, CardInstance card, EffectTrigger trigger, ZoneType? graveFromZone)> pendingOffers
            = new List<(PlayerState, CardInstance, EffectTrigger, ZoneType?)>();
        private bool flushingOffers;

        private IEnumerator OfferTriggeredEffects(PlayerState owner, CardInstance card, EffectTrigger trigger, ZoneType? graveFromZone = null)
        {
            // Giftwyrm: Karten, die ihrem Besitzer dienen, triggern IMMER für den
            // OriginalOwner — egal, wer sie gerade kontrolliert (oder kontrollierte).
            if (card?.Definition != null && card.Definition.passiveServesOriginalOwner
                && card.OriginalOwner != null)
                owner = card.OriginalOwner;

            // YuGiOh-Regel: WÄHREND eine Kette sich auflöst, startet nichts Neues —
            // auch PFLICHT-Trigger warten und feuern erst an der Naht danach.
            // graveFromZone wandert mit, sonst verlöre Asemirs "nur aus dem Extra
            // Deck"-Filter beim Nachholen seine Herkunftsinfo.
            if (resolvingChain > 0)
            {
                if (!pendingOffers.Exists(p => p.card == card && p.trigger == trigger))
                    pendingOffers.Add((owner, card, trigger, graveFromZone));
                yield break;
            }

            var activatable = ActivatableEffects(card, owner, trigger);
            // The Last Asemir: der Trigger verlangt die Reise Extra Deck → Friedhof
            if (graveFromZone.HasValue)
                activatable.RemoveAll(i =>
                {
                    var fx = GetEffect(card, i);
                    return fx != null && fx.onlyFromExtraDeck && graveFromZone.Value != ZoneType.ExtraDeck;
                });
            if (activatable.Count == 0)
            {
                // Sichtbares Fizzle statt stillem Nichts: der Trigger WÄRE da,
                // aber die Karte ist annulliert.
                if (card.EffectsNegated && HasEffectWithTrigger(card, trigger))
                {
                    Log($"{card.Name} tries to activate — but its effects are negated.");
                    if (presenter != null)
                        yield return presenter.ShowTargetsFlash(new List<CardInstance> { card });
                }
                yield break;
            }

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

            // Einheitlich als Karten-Liste (Master-Duel-Stil): eine Zeile je
            // Effekt mit der Karte dahinter, CANCEL passt — ob nun ein Trigger
            // fragt oder zwischen Normal und Infused gewählt wird.
            {
                var request = new OptionRequest
                {
                    Title = activatable.Count == 1 ? "Activate effect?" : $"{card.Name}: choose effect",
                    Card = card,
                    AllowCancel = true
                };
                foreach (int index in activatable)
                {
                    request.Options.Add(EffectChoiceLabel(card, index));
                    request.OptionCards.Add(card);
                }
                yield return DecideRouted(owner, request);
                if (request.Result >= 0 && request.Result < activatable.Count)
                    yield return ActivateTriggered(owner, card, activatable[request.Result]);
            }
        }

        /// <summary>Hat die Karte überhaupt einen Effekt mit diesem Trigger? (Für das Negated-Fizzle-Feedback.)</summary>
        private static bool HasEffectWithTrigger(CardInstance card, EffectTrigger trigger)
        {
            if (card?.Definition == null) return false;
            foreach (var fx in card.Definition.effects)
                if (fx != null && fx.trigger == trigger) return true;
            return false;
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

            // Wer beschwört, reagiert nicht auf die eigene Beschwörung — seine
            // On-Summon-Trigger folgen ohnehin direkt (Master-Duel-Reihenfolge:
            // erst der Konter des Gegners, dann sofort der eigene Trigger).
            bool onlyOpponentResponds = context == "summon";

            foreach (var responder in new[] { firstPriority, firstPriority.Opponent })
            {
                if (Result != DuelResult.None) break;
                if (onlyOpponentResponds && contextCard != null && responder == contextCard.Owner) continue;

                // Master-Duel-Liste statt Einzelfragen: ALLE aktivierbaren
                // Reaktionen auf einmal zeigen, der Spieler klickt eine an oder
                // passt. Nach jeder Aktivierung wird neu gesammelt — was die
                // Auflösung verändert hat, verschwindet von selbst aus der Liste.
                int safety = 0;
                while (Result == DuelResult.None && safety++ < 12)
                {
                    var candidates = new List<(CardInstance card, int effectIndex)>();
                    foreach (var (card, effectIndex) in BuildResponseCandidates(responder, context, contextCard))
                    {
                        var effect = GetEffect(card, effectIndex);
                        if (effect == null || responder.Mana < EffectiveManaCost(responder, card, effect)) continue;
                        if (card.OncePerTurnUsed.Contains(effectIndex)) continue;
                        if (!HasValidTargets(effect, responder, card)) continue;
                        candidates.Add((card, effectIndex));
                    }
                    if (candidates.Count == 0) break;

                    var request = new OptionRequest
                    {
                        Title = isPhaseWindow ? context : $"Response to {context}",
                        Card = contextCard,
                        AllowCancel = true,       // Cancel = Pass
                        IsResponseList = true,
                        IsPhaseWindow = isPhaseWindow
                    };
                    foreach (var (card, effectIndex) in candidates)
                    {
                        var effect = GetEffect(card, effectIndex);
                        request.Options.Add($"{card.Name} — \"{effect.label}\"{DescribeActivation(effect)}");
                        request.OptionCards.Add(card);
                    }
                    yield return DecideRouted(responder, request);
                    if (request.Result < 0 || request.Result >= candidates.Count) break; // Pass

                    var chosen = candidates[request.Result];
                    if (chosen.card.SpellData != null && chosen.card.Zone == ZoneType.SpellZone)
                        yield return ActivateSpell(responder, chosen.card, chosen.effectIndex, false);
                    else
                        yield return ActivateEffect(responder, chosen.card, chosen.effectIndex);
                    if (Result != DuelResult.None) break;
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
            // The Liberator: Zauber dürfen nicht direkt aus der Hand antworten,
            // solange der Gegner den Set-Zwang auf dem Feld hat.
            bool spellsLocked = MustSetSpellsFirst(responder);
            foreach (var card in responder.Hand)
            {
                if (spellsLocked && card.SpellData != null) continue;
                foreach (int index in ActivatableEffects(card, responder, EffectTrigger.HandQuick))
                {
                    // Waylay-Ambush: Hand-Reaktionen mit Fenster-Beschränkung zünden
                    // NUR im passenden Fenster (wie die Trapline-Fallen vom Feld)
                    var window = card.Definition.effects[index].quickWindow;
                    if (window == QuickWindow.AttackResponse && context != "attack") continue;
                    if (window == QuickWindow.SummonResponse && context != "summon") continue;
                    candidates.Add((card, index));
                }
            }

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

        /// <summary>
        /// The Liberator: liegt beim GEGNER dieses Spielers ein offenes Feld-Monster
        /// mit Set-Zwang, muss dieser Spieler Zauber erst setzen — Aktivierungen
        /// direkt aus der Hand (Main Phase wie HandQuick) entfallen.
        /// </summary>
        public bool MustSetSpellsFirst(PlayerState player)
        {
            foreach (var card in player.Opponent.FieldCards())
                if (card != null && !card.FaceDown && card.Definition != null
                    && card.Definition.passiveOpponentMustSetSpells) return true;
            return false;
        }

        /// <summary>
        /// Emergency Barrier: liegt bei dieser Seite ein offener Feld-Beschützer,
        /// sind ALLE ihre Karten für den Gegner weder Effekt-Ziel noch Angriffsziel.
        /// </summary>
        public bool HasAllCardsProtection(PlayerState side)
        {
            foreach (var card in side.FieldCards())
                if (card != null && !card.FaceDown && card.Definition != null
                    && card.Definition.passiveProtectAllFromTargetingAndAttacks) return true;
            return false;
        }

        /// <summary>
        /// Cull the Weak: beide Spieler wählen VERDECKT ein Monster aus ihrem Deck —
        /// erst nach beiden Wahlen wird gleichzeitig aufgedeckt. Das niedrigere ATK
        /// geht ins Grab, das höhere wird spezialbeschworen (ohne Platz: oben aufs
        /// Deck) und sein Besitzer nimmt die ATK-Differenz als Schaden.
        /// Gleichstand: beide werden zurückgemischt. Fehlt einer Seite das Monster,
        /// gewinnt die andere kampflos — die Differenz ist dann das volle ATK.
        /// </summary>
        private IEnumerator ResolveSimultaneousDeckCull(PlayerState player, CardInstance source)
        {
            var sides = new[] { player, player.Opponent };
            var picks = new CardInstance[2];
            for (int s = 0; s < sides.Length; s++)
            {
                var side = sides[s];
                var pool = side.DeckPile.Where(c => c.MonsterData != null)
                    .OrderByDescending(c => c.MonsterData.atk).ToList();
                if (pool.Count == 0)
                {
                    Log($"{side.Name} has no monster in the Deck to reveal.");
                    continue;
                }
                var request = new TargetRequest
                {
                    Title = $"{source.Name}: choose a monster from your Deck to reveal",
                    Kind = TargetKind.DeckMonsterFilteredSelf,
                    Count = 1,
                    AllowCancel = false
                };
                request.Candidates.AddRange(pool);
                yield return DecideRouted(side, request);
                picks[s] = request.Result.Count > 0 ? request.Result[0] : pool[0];
            }

            if (picks[0] == null && picks[1] == null) yield break;

            foreach (var pick in picks)
            {
                if (pick == null) continue;
                Log($"{pick.Owner.Name} reveals {pick.Name} (ATK {pick.MonsterData.atk}).");
                if (presenter != null) yield return presenter.ShowCardRevealed(pick, "REVEALED FROM THE DECK");
            }

            if (picks[0] != null && picks[1] != null
                && picks[0].MonsterData.atk == picks[1].MonsterData.atk)
            {
                Log("Both monsters have equal ATK — they are shuffled back into the Decks.");
                Shuffle(player.DeckPile);
                Shuffle(player.Opponent.DeckPile);
                BoardChanged();
                yield break;
            }

            int atkFirst = picks[0]?.MonsterData.atk ?? -1;
            int atkSecond = picks[1]?.MonsterData.atk ?? -1;
            var winner = atkFirst > atkSecond ? picks[0] : picks[1];
            var loser = atkFirst > atkSecond ? picks[1] : picks[0];

            if (loser != null)
            {
                Log($"{loser.Name} has the lower ATK — it is destroyed.");
                MoveToGraveyard(loser);
                BoardChanged();
            }

            var owner = winner.Owner;
            bool canSummon = owner.FreeMonsterZones() > 0
                && !owner.CannotSpecialSummonThisTurn
                && !FieldLimitReached(owner, winner.Definition);
            if (canSummon)
            {
                yield return SpecialSummonToField(owner, winner, $"with {source.Name}");
            }
            else
            {
                owner.DeckPile.Remove(winner);
                owner.DeckPile.Insert(0, winner);
                winner.Zone = ZoneType.Deck;
                Log($"{winner.Name} cannot be Summoned — it is placed on top of the Deck.");
                BoardChanged();
            }

            int difference = winner.MonsterData.atk - Math.Max(0, loser?.MonsterData.atk ?? 0);
            if (difference > 0)
            {
                Log($"{owner.Name} pays the price of strength.");
                DealDamage(owner, difference, source.Name);
                if (CheckWin()) yield break;
            }

            yield return FirePendingGraveTriggers();
        }

        // ================== BATTLE PHASE ==================

        private IEnumerator RunBattlePhase(PlayerState player)
        {
            int safety = 0;
            player.AttacksDeclaredThisBattle = 0;
            endBattlePhaseRequested = false;
            while (Result == DuelResult.None && safety++ < 50)
            {
                // Parley: die Phase endet, sobald der laufende Kampf durch ist
                if (endBattlePhaseRequested) { endBattlePhaseRequested = false; break; }
                var request = BuildBattleActions(player);
                // Kein Auto-Ende, wenn nur noch "End Battle Phase" übrig ist: die Phase
                // gehört dem Spieler, bis er sie selbst schliesst. Bots wählen die
                // einzige Option sofort — und weil JEDER die Entscheidung trifft,
                // laufen Server und Client-Spiegel identisch.

                yield return DecideRouted(player, request);
                if (request.Chosen < 0 || request.Chosen >= request.Options.Count) continue;

                var option = request.Options[request.Chosen];
                if (option.EndBattle) break;

                // Waylay Tollgate / Bylaw Quiet Hours: der erste Angriff kostet Wegzoll —
                // automatisch abgezogen (BuildBattleActions bietet ohne Deckung nichts an)
                int toll = AttackTollFor(player);
                if (toll > 0)
                {
                    player.Mana = Math.Max(0, player.Mana - toll);
                    Log($"{player.Name} pays a toll of {toll} Mana to attack ({player.Mana} Mana left).");
                }
                player.AttacksDeclaredThisBattle++;
                player.DeclaredAttackThisTurn = true; // Waylay: "hat diesen Zug angegriffen"
                yield return ResolveAttack(player, option);
                if (CheckWin()) yield break;
                yield return EnforceLifeThresholds();
                if (CheckWin()) yield break;
                if (presenter != null) yield return DuelWait.For(0.2f); // Beat zwischen Angriffen
            }
            endBattlePhaseRequested = false;
        }

        /// <summary>
        /// Road to 1000 (Schildkante voran): der Angriffswert eines Monsters — normal
        /// die ATK, mit passiveAttacksWithDef oder Lead With the Shield die DEF.
        /// </summary>
        private static int AttackValueOf(CardInstance attacker) =>
            attacker.AttacksWithDefThisTurn
            || (attacker.Definition != null && attacker.Definition.passiveAttacksWithDef)
                ? attacker.CurrentDef
                : attacker.CurrentAtk;

        private BattleActionRequest BuildBattleActions(PlayerState player)
        {
            var request = new BattleActionRequest { Title = $"Battle Phase — {player.Name}" };

            // Spott: gibt es Monster, die angegriffen werden MÜSSEN, sind nur die wählbar
            // (per Effekt für diesen Zug — oder dauerhaft per Attention Hound)
            var forcedTargets = player.Opponent.Monsters()
                .Where(m => m.MustBeAttackedThisTurn
                            || (!m.FaceDown && m.Definition != null && m.Definition.passiveTaunt))
                .ToList();

            // The Duelist's Code: ein Angriff je Battle Phase — danach nur noch das Ende
            bool attackCapReached = OneAttackBonus() > 0 && player.AttacksDeclaredThisBattle >= 1;
            // Waylay Tollgate / Bylaw Quiet Hours: reicht das Mana nicht für den
            // fälligen Wegzoll, gibt es diesen Zug keinen ersten Angriff.
            bool tollUnpayable = AttackTollFor(player) > player.Mana;

            foreach (var attacker in player.Monsters())
            {
                if (tollUnpayable) break;
                if (attackCapReached) break;
                if (attacker.Position != BattlePosition.Attack) continue;
                if (attacker.CannotAttackThisTurn) continue;
                if (attacker.Definition != null && attacker.Definition.passiveCannotAttack) continue;
                // Giftwyrm: Geschenke kämpfen nicht für ihren Empfänger
                if (attacker.Definition != null && attacker.Definition.passiveCannotAttackWhileDisloyal
                    && attacker.Owner != attacker.OriginalOwner) continue;
                if (attacker.SummonedThisTurn && attacker.Definition != null
                    && attacker.Definition.passiveNoAttackOnSummonTurn) continue;
                if (attacker.HasAttackedThisTurn && attacker.BonusAttacks <= 0
                    && !ConditionalSecondAttackReady(player, attacker)) continue;

                // Chimney Sweep: darf am Feld vorbei direkt angreifen (halber Schaden)
                bool sweepDirect = attacker.Definition != null && attacker.Definition.passiveDirectAttackHalved
                    && player.Opponent.MonsterCount() > 0 && !player.NoDirectAttacksThisTurn
                    && !HasAllCardsProtection(player.Opponent);
                if (sweepDirect)
                    request.Options.Add(new BattleOption
                    {
                        Attacker = attacker,
                        Direct = true,
                        Label = $"{attacker.Name} ({AttackValueOf(attacker)}) slips past and attacks directly (half damage)"
                    });

                if (player.Opponent.MonsterCount() == 0)
                {
                    // Bristleback Aurochs: kommt nie an den Spieler
                    if (attacker.Definition != null && attacker.Definition.passiveNoDirectAttack) continue;
                    // Implosion: wer das Feld wegsprengt, stürmt nicht im selben Zug hinterher
                    if (player.NoDirectAttacksThisTurn) continue;
                    // Tidebound Leviathan: im Beschwörungszug kein Direktangriff —
                    // der Summon-Bounce soll das Feld nicht für den Todesstoß räumen
                    if (attacker.SummonedThisTurn && attacker.Definition != null
                        && attacker.Definition.passiveNoDirectAttackOnSummonTurn) continue;
                    request.Options.Add(new BattleOption
                    {
                        Attacker = attacker,
                        Direct = true,
                        Label = $"{attacker.Name} ({AttackValueOf(attacker)}) attacks directly"
                    });
                }
                else
                {
                    // Emergency Barrier: kein Monster des geschützten Spielers ist Angriffsziel —
                    // und weil er Monster kontrolliert, gibt es auch keinen Direktangriff.
                    if (HasAllCardsProtection(player.Opponent)) continue;
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
                            Label = $"{attacker.Name} ({AttackValueOf(attacker)}) attacks {targetInfo}"
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
            if (attacker.Definition != null && attacker.Definition.passiveCannotAttackWhileDisloyal
                && attacker.Owner != attacker.OriginalOwner) yield break;
            if (attacker.SummonedThisTurn && attacker.Definition != null
                && attacker.Definition.passiveNoAttackOnSummonTurn) yield break;
            if (option.Direct && attacker.SummonedThisTurn && attacker.Definition != null
                && attacker.Definition.passiveNoDirectAttackOnSummonTurn) yield break;
            if (option.Direct && player.NoDirectAttacksThisTurn) yield break; // Implosion
            if (attacker.HasAttackedThisTurn && attacker.BonusAttacks <= 0
                && !ConditionalSecondAttackReady(player, attacker)) yield break;

            // Immortal Demon: Kämpfe MIT dieser Karte verursachen keinen Kampfschaden
            bool noBattleDamage =
                (attacker.Definition != null && attacker.Definition.passiveNoBattleDamageInvolving)
                || (!option.Direct && target != null && target.Definition != null
                    && target.Definition.passiveNoBattleDamageInvolving);

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
            // Stand and Deliver / Splithoof Sign Here: wer im Reaktionsfenster das
            // Angriffsverbot kassiert, bricht auch den LAUFENDEN Angriff ab.
            if (attacker.CannotAttackThisTurn)
            {
                Log("The attack is called off.");
                yield break;
            }

            // The Duelist's Code: der Angreifer trägt den Bonus nur während des Kampfes
            int codeBonus = OneAttackBonus();

            if (option.Direct)
            {
                // Chimney Sweep: darf am Feld vorbei — dafür nur der halbe Schaden
                bool sweep = attacker.Definition != null && attacker.Definition.passiveDirectAttackHalved;
                if (player.Opponent.MonsterCount() > 0 && !sweep)
                {
                    Log("Direct attack no longer possible — the opponent controls a monster.");
                    yield break;
                }
                if (presenter != null) yield return presenter.ShowAttackImpact(attacker, null, true);
                int direct = AttackValueOf(attacker) + codeBonus;
                if (sweep && player.Opponent.MonsterCount() > 0) direct /= 2;
                if (!noBattleDamage) DealDamage(player.Opponent, direct, attacker.Name, isBattleDamage: true);
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
                    ArmCountdown(target); // Chimekeep
                    Log($"The face-down monster is flipped face-up: {target.Name}!");
                    BoardChanged();
                    if (responseDepth < 2) // Flip-Effekte feuern auch beim Aufdecken durch einen Angriff
                        yield return OfferTriggeredEffects(target.Owner, target, EffectTrigger.OnFlipFaceUp);
                    if (Result != DuelResult.None) yield break;
                    if (target.Zone != ZoneType.MonsterZone) { Log("The attack target is gone — the attack fizzles."); yield break; }
                }
                if (presenter != null) yield return presenter.ShowAttackImpact(attacker, target, false);

                int attackValue = AttackValueOf(attacker) + codeBonus;
                if (target.Position == BattlePosition.Attack)
                {
                    int defenderAtk = target.CurrentAtk;
                    if (attackValue > defenderAtk)
                    {
                        if (!noBattleDamage) DealDamage(player.Opponent, attackValue - defenderAtk, attacker.Name, isBattleDamage: true);
                        if (BattleShieldHolds(target)) Log(BattleShieldReason(target));
                        else yield return DestroyCard(target);
                        if (Result == DuelResult.None && target.Zone != ZoneType.MonsterZone)
                            yield return FireBearerKillTriggers(attacker);
                    }
                    else if (attackValue < defenderAtk)
                    {
                        if (!noBattleDamage) DealDamage(player, defenderAtk - attackValue, target.Name, isBattleDamage: true);
                        if (BattleShieldHolds(attacker)) Log(BattleShieldReason(attacker));
                        else yield return DestroyCard(attacker);
                    }
                    else
                    {
                        Log("Both monsters clash with equal force!");
                        if (BattleShieldHolds(target)) Log(BattleShieldReason(target));
                        else yield return DestroyCard(target);
                        if (BattleShieldHolds(attacker)) Log(BattleShieldReason(attacker));
                        else yield return DestroyCard(attacker);
                    }
                }
                else
                {
                    int defenderDef = target.CurrentDef;
                    bool defenderSurvived = true;
                    if (attackValue > defenderDef)
                    {
                        // The Small Print: Piercing — die Differenz trifft den Kontrolleur trotzdem
                        if (attacker.HasPiercing && !noBattleDamage)
                        {
                            Log($"{attacker.Name} pierces through {target.Name}'s defense!");
                            DealDamage(player.Opponent, attackValue - defenderDef, attacker.Name, isBattleDamage: true);
                        }
                        if (BattleShieldHolds(target))
                        {
                            Log(BattleShieldReason(target).Replace(" stands firm — ", "'s defense bends but "));
                        }
                        else
                        {
                            Log($"{target.Name}'s defense is broken.");
                            yield return DestroyCard(target);
                            defenderSurvived = target.Zone == ZoneType.MonsterZone;
                            if (Result == DuelResult.None && !defenderSurvived)
                                yield return FireBearerKillTriggers(attacker);
                        }
                    }
                    else if (attackValue < defenderDef)
                    {
                        if (!noBattleDamage) DealDamage(player, defenderDef - attackValue, target.Name, isBattleDamage: true);
                        Log($"{attacker.Name} bounces off the defense.");
                    }
                    else
                    {
                        Log("The attack bounces off harmlessly.");
                    }

                    // Ram's Head: wer an einer Mauer abprallt, verliert das Horn
                    if (defenderSurvived && Result == DuelResult.None && attacker.Zone == ZoneType.MonsterZone)
                        foreach (var horn in attacker.EquippedArtifacts.ToArray())
                            if (horn.Definition != null && horn.Definition.passiveBreakOnFailedPierce)
                            {
                                Log($"{horn.Name} shatters against {target.Name}.");
                                yield return DestroyCard(horn);
                                if (Result != DuelResult.None) yield break;
                            }
                }
            }

            // Doorstop Made of Dragon Bone: der Türstopper splittert nach jedem Angriff
            if (attacker.Definition != null && attacker.Definition.passiveDefLossAfterAttack > 0
                && attacker.Zone == ZoneType.MonsterZone)
            {
                attacker.PermanentDefBonus -= attacker.Definition.passiveDefLossAfterAttack;
                Log($"{attacker.Name} chips from the impact — it permanently loses {attacker.Definition.passiveDefLossAfterAttack} DEF ({attacker.CurrentDef} DEF).");
            }
            BoardChanged();
        }

        private void DealDamage(PlayerState player, int amount, string sourceName, bool isBattleDamage = false)
        {
            if (amount <= 0) return;
            // The Last Asemir: der Gegner erleidet allen Kampfschaden des Besitzers.
            // Nur EINE Umleitung — zwei Asemirs werfen den Schaden nicht ewig hin und her.
            if (isBattleDamage && player.Monsters().Any(m => !m.FaceDown
                && m.Definition != null && m.Definition.passiveRedirectBattleDamage))
            {
                Log($"{player.Name}'s battle damage is redirected to {player.Opponent.Name}.");
                player = player.Opponent;
            }
            if (isBattleDamage && player.NoBattleDamageThisTurn)
            {
                Log($"{player.Name} takes no battle damage this turn — {amount} damage is prevented.");
                return;
            }
            // High Stakes: bis zum Ende des nächsten Zuges des Aktivierenden zählt Kampfschaden doppelt
            if (isBattleDamage && doubleBattleDamageUntilTurn >= TurnNumber && doubleBattleDamageUntilTurn > 0)
            {
                amount *= 2;
                Log("High Stakes — the battle damage is doubled!");
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
                if (receiver.MonsterCount() >= MonsterCapFor(receiver)) break; // Bylaw: Obergrenze
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

            // Poacher's Lantern: gewilderte Karten kehren nie in ein Grab zurück
            if (card.BanishWhenLeavingField && card.Zone == ZoneType.MonsterZone)
            {
                card.BanishWhenLeavingField = false;
                Log($"{card.Name} was poached — it is banished instead.");
                MoveToBanished(card);
                return;
            }
            card.LienAmount = 0;
            card.BanishWhenLeavingField = false;
            card.PiercingThisTurn = false;

            // Deckay: Karten mit Friedhofs-Triggern merken sich ihre Herkunft —
            // die Trigger feuern gesammelt an der nächsten Naht (FirePendingGraveTriggers),
            // denn dieser Umzug hier ist synchron und kann keine Kette starten.
            var fromZone = card.Zone;
            if (HasGraveArrivalTrigger(card))
                pendingGraveTriggers.Add((card, fromZone));

            // Reliquarys landen wie jede andere Karte im Friedhof — nur Hand-Rückgaben
            // schicken sie zurück ins Extra Deck (siehe ReturnToExtraDeck).
            // Giftwyrm: die Fremdkontrolle wird VOR dem Besitzer-Reset festgehalten,
            // damit "während dein Gegner sie kontrolliert"-Trigger sie noch sehen.
            card.WasDisloyalWhenLeftField = IsOnField(card)
                && card.OriginalOwner != null && card.Owner != card.OriginalOwner;
            RemoveFromCurrentZone(card);
            if (card.OriginalOwner != null) card.Owner = card.OriginalOwner; // Kontrolle endet — zurück zum Besitzer
            card.FaceDown = false;
            card.Zone = ZoneType.Graveyard;
            card.PermanentAtkBonus = 0;
            card.PermanentDefBonus = 0;
            card.TempAtkBonus = 0;
            card.TempDefBonus = 0;
            card.WasSpecialSummoned = false;
            // Road to 1000 / 5 Archetypes: auch Level, Schild-Haltung und Uhrwerk enden am Grab
            card.PermanentLevelBonus = 0;
            card.TempLevelThisTurn = 0;
            card.AttacksWithDefThisTurn = false;
            card.CountdownMarkers = 0;
            card.DecreeExemptFor = null;
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
            if (target?.Definition == null) return false;
            // --- The Small Print: Effekt-Immunitäten (auch gegen eigene Effekte) ---
            if (IsOnField(target) && !target.FaceDown)
            {
                if (target.Definition.passiveNoEffectDestroy) return true;
                if (target.Definition.passiveLoneImmunity && target.Owner.MonsterCount() <= 1) return true;
                if (target.Definition.passiveLowHandImmunity && target.Owner.Hand.Count <= 1) return true;
                // Bylaw Chairwoman: benannte Karten des Besitzers stehen unter Amtsschutz
                foreach (var guardian in target.Owner.FieldCards())
                    if (guardian != null && guardian != target && !guardian.FaceDown && !guardian.EffectsNegated
                        && guardian.Definition != null
                        && !string.IsNullOrEmpty(guardian.Definition.protectsNamedFromEffectDestroy)
                        && target.Name.Contains(guardian.Definition.protectsNamedFromEffectDestroy)) return true;
                foreach (var neighbour in target.AdjacentMonsters())
                    if (!neighbour.FaceDown && neighbour.Definition != null && neighbour.Definition.passiveAdjacentNoEffectDestroy) return true;
            }
            if (target.MonsterData == null) return false;
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
            card.LienAmount = 0;
            card.BanishWhenLeavingField = false;
            card.PiercingThisTurn = false;
            card.WasDisloyalWhenLeftField = IsOnField(card)
                && card.OriginalOwner != null && card.Owner != card.OriginalOwner;
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
            card.PermanentLevelBonus = 0;
            card.TempLevelThisTurn = 0;
            card.AttacksWithDefThisTurn = false;
            card.CountdownMarkers = 0;
            card.DecreeExemptFor = null;
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
            // Buried With His Boots On: eigene Monster-Verluste dieses Zuges zählen
            if (wasMonster && card.Zone == ZoneType.MonsterZone && card.Owner != null)
                card.Owner.OwnMonstersDestroyedThisTurn++;
            // Load-Bearing Wall: fällt die Wand, fällt der Putz mit — Nachbarn merken
            // sich VOR dem Umzug, danach hat die Karte keine Zone mehr
            int wallDebuff = card.Definition != null ? card.Definition.passiveAdjacentDebuffOnDestroy : 0;
            var wallNeighbours = wallDebuff > 0 && wasMonster ? card.AdjacentMonsters() : null;
            if (presenter != null) yield return presenter.ShowCardDestroyed(card); // Zersplittern + Flug zum Friedhof
            if (wasMonster) DetachEquipsToGraveyard(card);
            MoveToGraveyard(card);
            Log($"{card.Name} is destroyed.");
            if (wallNeighbours != null)
                foreach (var neighbour in wallNeighbours)
                {
                    if (!IsOnField(neighbour)) continue;
                    neighbour.PermanentAtkBonus -= wallDebuff;
                    neighbour.PermanentDefBonus -= wallDebuff;
                    Log($"{neighbour.Name} loses {wallDebuff} ATK and DEF as {card.Name} falls.");
                }
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
                else if (card.ArtifactData != null)
                {
                    // Dead Man's Switch: dasselbe Ohr für fallende ARTEFAKTE
                    foreach (var listener in TriggerScanCandidates(card.Owner).ToArray())
                    {
                        if (Result != DuelResult.None) yield break;
                        yield return OfferTriggeredEffects(card.Owner, listener, EffectTrigger.OnOwnArtifactDestroyed);
                    }
                }
                // Deckay: der Friedhofs-Ankunfts-Trigger der zerstörten Karte
                yield return FirePendingGraveTriggers();
            }
        }
    }
}
