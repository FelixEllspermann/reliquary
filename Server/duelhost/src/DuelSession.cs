// Ein server-autoritatives Duell: hält die Engine (DuelManager), treibt ihre
// Coroutinen mit einem eigenen Pump (DuelWait/null werden übersprungen, gewartet
// wird nur auf Client-Intents) und serialisiert Zustand, Ereignisse und Requests
// PRO SPIELER-SICHT — verdeckte Karten und die Gegnerhand verlassen den Server nie.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rouge.Tcg;

namespace Rouge.DuelHost
{
    public class DuelSession
    {
        public readonly string Id;
        public bool Finished { get; private set; }

        private readonly CardLibrary library;
        private readonly Action<string, object> emit;   // (Seite "A"/"B" oder null = beide, Payload)
        private readonly ServerPresenter presenter = new ServerPresenter();
        private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();

        private DuelManager duel;
        private PlayerState playerA, playerB;

        private DuelRequest pendingRequest;
        private string pendingSide;
        private int nextRequestId = 1;
        private int sentRequestId;

        private readonly Dictionary<CardInstance, int> cardIds = new Dictionary<CardInstance, int>();
        private readonly Dictionary<int, CardInstance> cardsById = new Dictionary<int, CardInstance>();
        private int nextCardId = 1;

        private bool stateDirty;
        private readonly List<string> logLines = new List<string>();
        private DuelResult? ended;
        private bool endSent;

        public DuelSession(string id, CardLibrary library, Action<string, object> emit)
        {
            Id = id;
            this.library = library;
            this.emit = emit;
        }

        // ================== START ==================

        public void Start(JsonElement msg)
        {
            int seed = msg.GetProperty("seed").GetInt32();
            bool aStarts = !msg.TryGetProperty("aStarts", out var s) || s.GetBoolean();

            duel = new DuelManager(new DuelConfig
            {
                Rules = library.Rules,
                Catalog = library.Catalog,
                Presenter = presenter,
                RunRoutine = routine => stack.Push(routine),   // Pump treibt selbst
                BotActionDelay = 0f
            });
            // Die Brettänderung ist die Naht zwischen zwei Bildern, und genau an
            // dieser Naht müssen die Ereignisse raus.
            //
            // Die Engine ruft den Presenter mal VOR und mal NACH der Änderung:
            // Zerstörung animiert, solange die Karte noch liegt, und räumt sie
            // danach ab; eine Beschwörung stellt erst hin und animiert dann. Wer
            // erst alle Ereignisse sammelt und danach einen Zustand schickt, spielt
            // die Zerstörung auf einem Brett, auf dem die Karte längst weg ist —
            // man sieht schlicht nichts. Genau deshalb fehlten Angriffs- und
            // Zerstörungsanimation, und die Extra-Deck-Karte stand schon da, bevor
            // der Tresor sich öffnete.
            duel.OnBoardChanged += () =>
            {
                EmitEvents();       // alles, was VOR dieser Änderung geschah
                stateDirty = true;
                EmitState();        // und unmittelbar danach der neue Zustand
            };
            duel.OnLog += line => logLines.Add(line);
            duel.OnDuelEnded += result => ended = result;

            var a = ReadSide(msg.GetProperty("a"), "A");
            var b = ReadSide(msg.GetProperty("b"), "B");
            duel.StartServerDuel(seed,
                a.Name, a.Deck, a.Extra, a.Hero, a.Controller,
                b.Name, b.Deck, b.Extra, b.Hero, b.Controller, aStarts,
                a.DeckFinishes, a.ExtraFinishes, b.DeckFinishes, b.ExtraFinishes);

            playerA = duel.Player1;
            playerB = duel.Player2;
        }

        /// <summary>Eine Duellseite, wie sie aus der Start-Nachricht kommt.</summary>
        private sealed class Side
        {
            public string Name;
            public List<CardDefinition> Deck, Extra;
            public List<Rouge.Tcg.Net.CardFinish> DeckFinishes, ExtraFinishes;
            public PlayerCardData Hero;
            public DuelController Controller;
        }

        private Side ReadSide(JsonElement side, string key)
        {
            var deckFinishes = new List<Rouge.Tcg.Net.CardFinish>();
            var extraFinishes = new List<Rouge.Tcg.Net.CardFinish>();
            bool isBot = side.TryGetProperty("kind", out var k) && k.GetString() == "bot";
            return new Side
            {
                Name = side.GetProperty("name").GetString(),
                // Namen und Finishes zusammen auflösen: fällt eine Karte weg,
                // muss ihre Ausführung mitfallen, sonst verrutscht die ganze Liste.
                Deck = library.Catalog.ResolveList(ReadNames(side, "deck"), ReadInts(side, "deckFinishes"), deckFinishes),
                Extra = library.Catalog.ResolveList(ReadNames(side, "extra"), ReadInts(side, "extraFinishes"), extraFinishes),
                DeckFinishes = deckFinishes,
                ExtraFinishes = extraFinishes,
                Hero = side.TryGetProperty("hero", out var h)
                    ? library.Catalog.FindByName(h.GetString()) as PlayerCardData : null,
                Controller = isBot ? new BotDuelController() : (DuelController)new WireController(this, key)
            };
        }

        private static List<string> ReadNames(JsonElement parent, string field)
        {
            var names = new List<string>();
            if (parent.TryGetProperty(field, out var array) && array.ValueKind == JsonValueKind.Array)
                foreach (var item in array.EnumerateArray()) names.Add(item.GetString());
            return names;
        }

        /// <summary>Zahlenfeld aus der Nachricht — fehlt es, bleibt die Liste leer (alles schlicht).</summary>
        private static List<int> ReadInts(JsonElement parent, string field)
        {
            var values = new List<int>();
            if (parent.TryGetProperty(field, out var array) && array.ValueKind == JsonValueKind.Array)
                foreach (var item in array.EnumerateArray())
                    values.Add(item.ValueKind == JsonValueKind.Number ? item.GetInt32() : 0);
            return values;
        }

        // ================== PUMP ==================

        /// <summary>Treibt die Engine, bis sie auf einen Client-Intent wartet oder fertig ist.</summary>
        public void Pump()
        {
            int guard = 0;
            while (!Finished && pendingRequest == null)
            {
                if (stack.Count == 0) { Finished = true; break; }
                if (++guard > 5_000_000)
                {
                    logLines.Add("HOST ERROR: coroutine runaway — duel aborted.");
                    Finished = true;
                    break;
                }
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                if (top.Current is IEnumerator nested) stack.Push(nested);
                // DuelWait und null: serverseitig sofort weiter
            }
            if (ended != null) Finished = true;
        }

        // ================== REQUESTS & INTENTS ==================

        public void PostRequest(string side, DuelRequest request)
        {
            pendingRequest = request;
            pendingSide = side;
            nextRequestId++;

            // Der andere sitzt derweil vor einem Brett, auf dem sich nichts rührt,
            // und weiss nicht, ob er dran ist oder ob es hängt. Also sagen wir es ihm.
            emit(side == "A" ? "B" : "A", new
            {
                op = "waiting",
                duelId = Id,
                text = request is YesNoRequest ? "deciding whether to respond"
                     : request is TargetRequest ? "choosing targets"
                     : request is ZoneSelectRequest ? "choosing a zone"
                     : request is BattleActionRequest ? "declaring an attack"
                     : "thinking"
            });
        }

        public void ApplyIntent(string side, JsonElement intent)
        {
            if (pendingRequest == null || side != pendingSide) return;
            bool valid = ApplyAnswer(pendingRequest, intent);
            if (!valid)
            {
                // Abgelehnt: der Request geht erneut raus. Beim dritten Mal ist
                // klar, dass sich Client und Server über diese Anfrage nicht
                // einigen — dann wird sie neutral beantwortet.
                //
                // Vorher lief das ewig im Kreis: Server schickt, Client antwortet,
                // Server lehnt ab, Server schickt … Für den Spieler sieht das aus
                // wie ein eingefrorenes Spiel, und im Log stand kein Wort davon.
                rejectedAnswers++;
                logLines.Add($"[sync] Antwort auf {pendingRequest.GetType().Name} abgelehnt (Versuch {rejectedAnswers}).");
                Console.WriteLine($"[host] Intent abgelehnt: {pendingRequest.GetType().Name} (Versuch {rejectedAnswers}) — {intent}");
                if (rejectedAnswers < 3) { sentRequestId = 0; return; }

                logLines.Add("[sync] Anfrage konnte nicht beantwortet werden — neutral aufgelöst, das Duell läuft weiter.");
                Console.WriteLine($"[host] {pendingRequest.GetType().Name} dreimal abgelehnt — neutral beantwortet.");
                AnswerNeutral(pendingRequest);
                pendingRequest = null;
                pendingSide = null;
                rejectedAnswers = 0;
                return;
            }

            rejectedAnswers = 0;
            pendingRequest.Answered = true;
            pendingRequest = null;
            emit(pendingSide == "A" ? "B" : "A", new { op = "waiting", duelId = Id, text = "" });
            pendingSide = null;
        }

        /// <summary>Wie oft der Client die offene Anfrage schon ungültig beantwortet hat.</summary>
        private int rejectedAnswers;

        /// <summary>Intent gegen den offenen Request prüfen und anwenden. False = ungültig.</summary>
        private bool ApplyAnswer(DuelRequest request, JsonElement intent)
        {
            switch (request)
            {
                case StartChoiceRequest start:
                    start.Result = !intent.TryGetProperty("first", out var f) || f.GetBoolean();
                    return true;

                case MainActionRequest main:
                {
                    int chosen = ReadInt(intent, "chosen", -1);
                    if (chosen < 0 || chosen >= main.Options.Count) return false;
                    main.Chosen = chosen;
                    int zone = ReadInt(intent, "zone", -1);
                    if (zone >= 0) main.Options[chosen].PreferredZoneIndex = zone;
                    return true;
                }

                case BattleActionRequest battle:
                {
                    int chosen = ReadInt(intent, "chosen", -1);
                    if (chosen < 0 || chosen >= battle.Options.Count) return false;
                    battle.Chosen = chosen;
                    return true;
                }

                case YesNoRequest yesNo:
                    yesNo.Result = intent.TryGetProperty("result", out var r) && r.GetBoolean();
                    return true;

                case OptionRequest option:
                {
                    int chosen = ReadInt(intent, "chosen", -1);
                    if (chosen == -1 && option.AllowCancel) { option.Result = -1; return true; }
                    if (chosen < 0 || chosen >= option.Options.Count) return false;
                    option.Result = chosen;
                    return true;
                }

                case ZoneSelectRequest zoneSelect:
                {
                    int index = ReadInt(intent, "index", -1);
                    if (!zoneSelect.FreeIndices.Contains(index)) return false;
                    zoneSelect.Result = index;
                    return true;
                }

                case TargetRequest target:
                {
                    if (intent.TryGetProperty("cancelled", out var c) && c.GetBoolean())
                    {
                        if (!target.AllowCancel) return false;
                        target.Cancelled = true;
                        return true;
                    }
                    var picked = new List<CardInstance>();
                    if (intent.TryGetProperty("ids", out var ids) && ids.ValueKind == JsonValueKind.Array)
                        foreach (var idElement in ids.EnumerateArray())
                        {
                            if (!cardsById.TryGetValue(idElement.GetInt32(), out var card)) return false;
                            if (!target.Candidates.Contains(card) || picked.Contains(card)) return false;
                            picked.Add(card);
                        }
                    int required = Math.Min(target.Count, target.Candidates.Count);
                    if (target.AllowFewer ? picked.Count > target.Count : picked.Count != required) return false;
                    target.Result.Clear();
                    target.Result.AddRange(picked);
                    return true;
                }
            }
            return false;
        }

        private static int ReadInt(JsonElement json, string name, int fallback) =>
            json.TryGetProperty(name, out var value) && value.TryGetInt32(out int parsed) ? parsed : fallback;

        public void Forfeit(string side)
        {
            if (duel == null || Finished) return;
            duel.Forfeit(side == "A" ? playerA : playerB);
            // Ein offener Request würde den Pump ewig blockieren — neutral beantworten
            if (pendingRequest != null)
            {
                AnswerNeutral(pendingRequest);
                pendingRequest = null;
                pendingSide = null;
            }
        }

        private static void AnswerNeutral(DuelRequest request)
        {
            switch (request)
            {
                case MainActionRequest main: main.Chosen = main.Options.FindIndex(o => o.Kind == MainActionKind.EndTurn); break;
                case BattleActionRequest battle: battle.Chosen = battle.Options.FindIndex(o => o.EndBattle); break;
                case OptionRequest option: option.Result = option.AllowCancel ? -1 : 0; break;
                case TargetRequest target: if (target.AllowCancel) target.Cancelled = true; break;
                case ZoneSelectRequest zone: zone.Result = zone.FreeIndices.Count > 0 ? zone.FreeIndices[0] : -1; break;
            }
            request.Answered = true;
        }

        // ================== AUSGEHENDE NACHRICHTEN ==================

        /// <summary>Die bisher aufgezeichneten Ereignisse rausschicken, je Sicht maskiert.</summary>
        private void EmitEvents()
        {
            if (presenter.Pending.Count == 0) return;
            emit("A", new { op = "events", duelId = Id, events = presenter.Pending.Select(e => EventWire(e, playerA)).ToArray() });
            emit("B", new { op = "events", duelId = Id, events = presenter.Pending.Select(e => EventWire(e, playerB)).ToArray() });
            // Zuschauer-Fassung: viewer=null zeigt NUR Öffentliches (kein Ghosting).
            // Node verwirft to=="S", wenn niemand zuschaut.
            emit("S", new { op = "events", duelId = Id, events = presenter.Pending.Select(e => EventWire(e, null)).ToArray() });
            presenter.Pending.Clear();
        }

        /// <summary>Den aktuellen Zustand rausschicken, falls sich etwas geändert hat.</summary>
        private void EmitState()
        {
            if (!stateDirty || playerA == null || playerB == null) return;
            stateDirty = false;
            emit("A", new { op = "state", duelId = Id, view = BuildView(playerA, playerB) });
            emit("B", new { op = "state", duelId = Id, view = BuildView(playerB, playerA) });
            emit("S", new { op = "state", duelId = Id, view = BuildSpectatorView() });
        }

        /// <summary>Erzwingt beim nächsten Flush einen State — für frisch beigetretene Zuschauer.</summary>
        public void Poke() => stateDirty = true;

        /// <summary>
        /// Die Sicht eines Zuschauers: Spieler A unten, B oben, beide Hände
        /// verdeckt (viewer=null sieht nur, was BEIDE Spieler sehen).
        /// </summary>
        private object BuildSpectatorView() => new
        {
            turn = duel.TurnNumber,
            phase = duel.Phase.ToString(),
            yourTurn = false,
            you = SideView(playerA, null),
            foe = SideView(playerB, null)
        };

        public void Flush()
        {
            if (duel == null) return;

            EmitEvents();

            if (logLines.Count > 0)
            {
                emit(null, new { op = "log", duelId = Id, lines = logLines.ToArray() });
                logLines.Clear();
            }

            EmitState();

            if (pendingRequest != null && sentRequestId != nextRequestId)
            {
                sentRequestId = nextRequestId;
                emit(pendingSide, new { op = "request", duelId = Id, request = RequestWire(pendingRequest) });
            }

            if (ended != null && !endSent)
            {
                endSent = true;
                emit(null, new { op = "end", duelId = Id, winner = ended == DuelResult.Player1Wins ? "A" : "B" });
            }
        }

        // ================== SERIALISIERUNG (mit Sichtschutz) ==================

        private int IdOf(CardInstance card)
        {
            if (card == null) return 0;
            if (!cardIds.TryGetValue(card, out int id))
            {
                id = nextCardId++;
                cardIds[card] = id;
                cardsById[id] = card;
            }
            return id;
        }

        /// <summary>Sieht dieser Spieler die Vorderseite der Karte?</summary>
        private static bool VisibleTo(CardInstance card, PlayerState viewer)
        {
            if (card == null) return false;
            switch (card.Zone)
            {
                case ZoneType.Hand:
                case ZoneType.Deck:
                case ZoneType.ExtraDeck:
                    return card.Owner == viewer;
                case ZoneType.Graveyard:
                case ZoneType.Banished:
                    return true;
                default:
                    return !card.FaceDown || card.Owner == viewer;
            }
        }

        private object CardWire(CardInstance card, PlayerState viewer)
        {
            if (card == null) return null;
            bool visible = VisibleTo(card, viewer);
            return new
            {
                id = IdOf(card),
                name = visible ? card.Name : null,
                faceDown = card.FaceDown,
                position = card.Position == BattlePosition.Defense ? "def" : "atk",
                atk = visible ? card.CurrentAtk : 0,
                def = visible ? card.CurrentDef : 0,
                negated = visible && card.EffectsNegated,
                deathCounters = visible ? card.DeathCounters : 0,
                lienAmount = visible ? card.LienAmount : 0,
                status = visible ? CardStatus.MaskOf(card) : 0,
                bonusAttacks = visible ? card.BonusAttacks : 0,
                // Auch das Aussehen ist verdeckte Information: eine funkelnde
                // Rückseite verriete, welche Karte dort liegt.
                finish = visible ? (int)card.Finish : 0
            };
        }

        private object SideView(PlayerState player, PlayerState viewer)
        {
            // Hand/Extra gehen IMMER als ID-Listen raus — CardWire maskiert die Namen
            // für den Gegner. So kann der Client Zieh-Animationen abspielen, ohne
            // dass verdeckte Information den Server verlässt.
            return new
            {
                name = player.Name,
                lp = player.LifePoints,
                mana = player.Mana,
                manaPerTurn = player.ManaPerTurn,
                bonusManaPerTurn = player.BonusManaPerTurn,
                manaCredit = player.ManaCredit,
                manaDebt = player.ManaDebt,
                deckCount = player.DeckPile.Count,
                extraCount = player.ExtraDeckPile.Count,
                handCount = player.Hand.Count,
                hand = player.Hand.Select(c => CardWire(c, viewer)).ToArray(),
                extra = player.ExtraDeckPile.Select(c => CardWire(c, viewer)).ToArray(),
                monsters = player.MonsterZones.Select(c => CardWire(c, viewer)).ToArray(),
                spells = player.SpellZones.Select(c => CardWire(c, viewer)).ToArray(),
                artifacts = player.ArtifactZones.Select(c => CardWire(c, viewer)).ToArray(),
                player = CardWire(player.PlayerCard, viewer),
                grave = player.Graveyard.Select(c => CardWire(c, viewer)).ToArray(),
                banished = player.Banished.Select(c => CardWire(c, viewer)).ToArray()
            };
        }

        private object BuildView(PlayerState viewer, PlayerState foe) => new
        {
            turn = duel.TurnNumber,
            phase = duel.Phase.ToString(),
            yourTurn = duel.TurnPlayer == viewer,
            you = SideView(viewer, viewer),
            foe = SideView(foe, viewer)
        };

        /// <summary>
        /// Manche Ereignisse SIND die Enthüllung: wer aus der Hand aktiviert oder
        /// aus dem Extra Deck beschwört, zeigt die Karte damit beiden Spielern —
        /// auch wenn sie im Moment des Events noch in einer verdeckten Zone
        /// liegt. Ohne diese Regel sah der Gegner nur "?" mit Kartenrücken.
        /// </summary>
        private static bool EventReveals(DuelEvent evt)
        {
            switch (evt.Type)
            {
                case "activation":
                case "pulse":
                case "chainlink":
                case "chainresolve":
                case "reliquarysummon":
                case "milled":   // gemillte Karten werden aufgedeckt gezeigt
                case "reveal":   // Suche/Rückholung: die bewegte Karte zeigt sich beiden
                    return true;
                case "summon":
                    // Face-up-Beschwörungen zeigen sich; verdeckte bleiben verdeckt
                    return evt.Card != null && !evt.Card.FaceDown;
                default:
                    return false;
            }
        }

        private object EventWire(DuelEvent evt, PlayerState viewer) => new
        {
            type = evt.Type,
            cardId = evt.Card != null ? IdOf(evt.Card) : 0,
            cardName = evt.Card != null && (EventReveals(evt) || VisibleTo(evt.Card, viewer))
                ? evt.Card.Name : null,
            // Bei der Reliquary-Beschwörung trägt targetId die ZONE, nicht eine
            // Karte — die Karte liegt zu dem Zeitpunkt noch gar nicht im Feld.
            targetId = evt.Zone >= 0 ? evt.Zone : (evt.Target != null ? IdOf(evt.Target) : 0),
            mine = evt.Player != null && evt.Player == viewer,
            text = evt.Text,
            direct = evt.Direct,
            link = evt.Amount,
            effectText = evt.EffectText,
            effectCost = evt.EffectCost,
            effectInfused = evt.EffectInfused
        };

        private object RequestWire(DuelRequest request)
        {
            var common = new Dictionary<string, object>
            {
                ["reqId"] = nextRequestId,
                ["title"] = request.Title
            };
            switch (request)
            {
                case StartChoiceRequest:
                    common["type"] = "start";
                    break;
                case MainActionRequest main:
                    common["type"] = "main";
                    common["mainOptions"] = main.Options.Select((o, i) => new
                    { i, kind = o.Kind.ToString(), label = o.Label, cardId = IdOf(o.Card) }).ToArray();
                    break;
                case BattleActionRequest battle:
                    common["type"] = "battle";
                    common["battleOptions"] = battle.Options.Select((o, i) => new
                    { i, label = o.Label, attackerId = IdOf(o.Attacker), targetId = IdOf(o.Target), direct = o.Direct, endBattle = o.EndBattle }).ToArray();
                    break;
                case YesNoRequest yesNo:
                    common["type"] = "yesno";
                    common["question"] = yesNo.Question;
                    common["cardId"] = IdOf(yesNo.Card);
                    common["isPhaseWindow"] = yesNo.IsPhaseWindow;
                    common["isResponse"] = yesNo.IsResponse;
                    break;
                case OptionRequest option:
                    common["type"] = "option";
                    common["choices"] = option.Options.ToArray();
                    common["allowCancel"] = option.AllowCancel;
                    common["cardId"] = IdOf(option.Card);
                    // Master-Duel-Reaktionsliste: Karte je Option (0 = keine) + Flags;
                    // searchable = Namenssuche (The Forbidden Name), dort reisen nur Strings.
                    common["choiceCardIds"] = option.Options
                        .Select((_, i) => i < option.OptionCards.Count ? IdOf(option.OptionCards[i]) : 0).ToArray();
                    common["isResponseList"] = option.IsResponseList;
                    common["isPhaseWindow"] = option.IsPhaseWindow;
                    common["searchable"] = option.Searchable;
                    break;
                case TargetRequest target:
                    common["type"] = "target";
                    common["candidates"] = target.Candidates
                        .Select(c => CardWire(c, pendingSide == "A" ? playerA : playerB)).ToArray();
                    common["count"] = target.Count;
                    common["allowFewer"] = target.AllowFewer;
                    common["allowCancel"] = target.AllowCancel;
                    break;
                case ZoneSelectRequest zoneSelect:
                    common["type"] = "zone";
                    common["zone"] = zoneSelect.Zone.ToString();
                    common["freeIndices"] = zoneSelect.FreeIndices.ToArray();
                    break;
            }
            return common;
        }
    }
}
