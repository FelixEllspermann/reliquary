using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>
    /// Spiegel-Modus für server-autoritative Duelle: der Server rechnet, dieser
    /// DuelManager hält nur den ANZEIGE-Zustand aus den state-Snapshots. Konvention:
    /// Player1 ist immer der lokale Spieler. Karten, die der Server nicht verrät
    /// (Gegnerhand, verdeckte Karten), existieren als Platzhalter ohne Definition —
    /// die Kartenansicht zeigt dafür Rückseiten.
    ///
    /// Als partial-Teil des DuelManager darf dieser Code die privaten Setter
    /// (Phase, TurnNumber, …) benutzen; UI und GameOverScreen funktionieren dadurch
    /// unverändert gegen denselben Typ.
    /// </summary>
    public partial class DuelManager
    {
        /// <summary>True, wenn dieses Duell nur ein Spiegel eines Server-Duells ist.</summary>
        public bool IsMirror { get; private set; }

        private readonly Dictionary<int, CardInstance> mirrorCards = new Dictionary<int, CardInstance>();
        private readonly Dictionary<CardInstance, int> mirrorIds = new Dictionary<CardInstance, int>();
        private bool mirrorFirstStateApplied;

        public void MirrorBegin(string localName, string foeName)
        {
            IsMirror = true;
            DuelRunning = true;
            Result = DuelResult.None;
            TurnNumber = 0;
            Player1 = new PlayerState { Name = string.IsNullOrEmpty(localName) ? "You" : localName };
            Player2 = new PlayerState { Name = string.IsNullOrEmpty(foeName) ? "Opponent" : foeName };
            Player1.Opponent = Player2;
            Player2.Opponent = Player1;
            LocalPlayer = Player1;
            TurnPlayer = Player1;
            Log($"Duel: {Player1.Name} vs {Player2.Name}.");
            BoardChanged();
        }

        public CardInstance MirrorCard(int id) =>
            id != 0 && mirrorCards.TryGetValue(id, out var card) ? card : null;

        public int MirrorIdOf(CardInstance card) =>
            card != null && mirrorIds.TryGetValue(card, out int id) ? id : 0;

        // ================== ZUSTAND ==================

        public void MirrorApplyState(Net.SduelView view)
        {
            if (view == null || Player1 == null) return;
            TurnNumber = view.turn;
            if (System.Enum.TryParse<DuelPhase>(view.phase, out var parsedPhase)) SetPhase(parsedPhase);
            TurnPlayer = view.yourTurn ? Player1 : Player2;
            MirrorSide(view.you, Player1);
            MirrorSide(view.foe, Player2);
            mirrorFirstStateApplied = true;
            BoardChanged();
        }

        private void MirrorSide(Net.SduelSide side, PlayerState player)
        {
            if (side == null) return;
            if (!string.IsNullOrEmpty(side.name)) player.Name = side.name;

            int lifeDelta = side.lp - player.LifePoints;
            player.LifePoints = side.lp;
            if (lifeDelta != 0 && mirrorFirstStateApplied) OnLifeChanged?.Invoke(player, lifeDelta);

            player.Mana = side.mana;
            player.ManaPerTurn = side.manaPerTurn;
            player.BonusManaPerTurn = side.bonusManaPerTurn;
            player.ManaCredit = side.manaCredit;
            player.ManaDebt = side.manaDebt;

            // Hand/Extra kommen immer als ID-Listen — beim Gegner ohne Namen
            // (Platzhalter ohne Definition = Rückseiten), aber mit stabilen IDs,
            // damit Zieh- und Ausspiel-Animationen funktionieren.
            MirrorFillVisible(player.Hand, side.hand, player, ZoneType.Hand);
            MirrorFillVisible(player.ExtraDeckPile, side.extra, player, ZoneType.ExtraDeck);
            MirrorFillHidden(player.DeckPile, side.deckCount, player, ZoneType.Deck);
            MirrorFillVisible(player.Graveyard, side.grave, player, ZoneType.Graveyard);
            MirrorFillVisible(player.Banished, side.banished, player, ZoneType.Banished);

            for (int i = 0; i < player.MonsterZones.Length; i++)
                player.MonsterZones[i] = MirrorResolve(At(side.monsters, i), player, ZoneType.MonsterZone);
            for (int i = 0; i < player.SpellZones.Length; i++)
                player.SpellZones[i] = MirrorResolve(At(side.spells, i), player, ZoneType.SpellZone);
            for (int i = 0; i < player.ArtifactZones.Length; i++)
                player.ArtifactZones[i] = MirrorResolve(At(side.artifacts, i), player, ZoneType.ArtifactZone);
            player.PlayerCard = MirrorResolve(side.player, player, ZoneType.PlayerZone);
        }

        private static Net.SduelCard At(Net.SduelCard[] array, int index) =>
            array != null && index < array.Length ? array[index] : null;

        private void MirrorFillVisible(List<CardInstance> list, Net.SduelCard[] wire, PlayerState owner, ZoneType zone)
        {
            list.Clear();
            if (wire == null) return;
            foreach (var entry in wire)
            {
                var card = MirrorResolve(entry, owner, zone);
                if (card != null) list.Add(card);
            }
        }

        /// <summary>Unsichtbare Stapel (Deck, Gegnerhand): nur die Anzahl zählt.</summary>
        private void MirrorFillHidden(List<CardInstance> list, int count, PlayerState owner, ZoneType zone)
        {
            while (list.Count > count) list.RemoveAt(list.Count - 1);
            while (list.Count < count) list.Add(new CardInstance(null, owner) { Zone = zone });
            foreach (var card in list) { card.Owner = owner; card.Zone = zone; }
        }

        private CardInstance MirrorResolve(Net.SduelCard wire, PlayerState owner, ZoneType zone)
        {
            if (wire == null || wire.id == 0) return null;
            if (!mirrorCards.TryGetValue(wire.id, out var card))
            {
                card = new CardInstance(null, owner);
                mirrorCards[wire.id] = card;
                mirrorIds[card] = wire.id;
            }
            card.Owner = owner;
            card.Zone = zone;
            card.FaceDown = wire.faceDown;
            card.Position = wire.position == "def" ? BattlePosition.Defense : BattlePosition.Attack;
            card.EffectsNegated = wire.negated;
            card.Finish = Net.CardFinishWire.From(wire.finish);

            if (!string.IsNullOrEmpty(wire.name))
            {
                if (card.Definition == null || card.Definition.cardName != wire.name)
                    card.Definition = catalog != null ? catalog.FindByName(wire.name) : null;
                // Kampfwerte exakt so anzeigen, wie der Server sie rechnet
                card.StatsOverriddenThisTurn = true;
                card.OverriddenAtk = wire.atk;
                card.OverriddenDef = wire.def;
            }
            return card;
        }

        /// <summary>
        /// Eine Karte aus einer Anfrage auflösen. Bekannte Karten kommen unverändert
        /// zurück — an denen darf nichts umgeschrieben werden, sie stehen ja auf dem
        /// Brett. Alles Unbekannte wird angelegt.
        /// <para>
        /// <b>Das ist der Grund, warum eine Suche ins Leere lief.</b> Ein Zauber, der
        /// ein Monster aus dem <i>Deck</i> holt, bietet Karten an, die nie in einem
        /// Zustand standen — das Deck ist verdeckte Information und wird nie Karte für
        /// Karte übertragen. Wer sie hier verwirft, zeigt ein leeres Auswahlfenster,
        /// und der Spieler kann nichts anklicken. Der Server schickt Name und Werte
        /// mit; daraus lässt sich die Karte vollständig bauen.
        /// </para>
        /// </summary>
        private CardInstance MirrorCandidate(Net.SduelCard wire)
        {
            if (wire == null || wire.id == 0) return null;
            if (mirrorCards.TryGetValue(wire.id, out var known)) return known;
            return MirrorResolve(wire, Player1, ZoneType.Deck);
        }

        /// <summary>
        /// Eine Karte aus einem Ereignis auflösen. Wie <see cref="MirrorCandidate"/>,
        /// nur für Animationen statt für Anfragen: eine Extra-Deck-Karte taucht im
        /// Beschwörungs-Ereignis auf, <i>bevor</i> sie je in einem Zustand stand.
        /// Ohne Platzhalter fiel die ganze Animation lautlos aus.
        /// <para>
        /// Der Name kommt nur mit, wenn der Server ihn diesem Spieler zeigen darf —
        /// ohne ihn bleibt die Definition leer, und <c>TcgCardView</c> zeichnet
        /// genau dafür einen Kartenrücken.
        /// </para>
        /// </summary>
        public CardInstance MirrorEventCard(int id, string cardName, PlayerState owner)
        {
            if (id == 0) return null;
            if (mirrorCards.TryGetValue(id, out var known))
            {
                // Der Spiegel kennt die Karte vielleicht nur als Rueckseite —
                // eine Handkarte des Gegners etwa. Bringt das Ereignis jetzt
                // einen Namen mit (Aktivierung = Enthuellung), wird er
                // nachgereicht, sonst zeigte die Animation weiter nur "?".
                if (known.Definition == null && !string.IsNullOrEmpty(cardName) && catalog != null)
                    known.Definition = catalog.FindByName(cardName);
                return known;
            }
            return MirrorResolve(
                new Net.SduelCard { id = id, name = cardName },
                owner ?? Player1,
                ZoneType.Deck);
        }

        // ================== REQUESTS ==================

        /// <summary>Baut aus dem Wire-Request einen echten DuelRequest mit Spiegel-Karten.</summary>
        public DuelRequest MirrorMaterialize(Net.SduelRequest wire)
        {
            switch (wire.type)
            {
                case "start":
                    return new StartChoiceRequest { Title = wire.title ?? "" };

                case "main":
                {
                    var request = new MainActionRequest { Title = wire.title ?? "" };
                    foreach (var option in wire.mainOptions ?? new Net.SduelMainOption[0])
                        request.Options.Add(new MainActionOption
                        {
                            Kind = System.Enum.TryParse<MainActionKind>(option.kind, out var kind) ? kind : MainActionKind.EndTurn,
                            Card = MirrorCard(option.cardId),
                            Label = option.label ?? ""
                        });
                    return request;
                }

                case "battle":
                {
                    var request = new BattleActionRequest { Title = wire.title ?? "" };
                    foreach (var option in wire.battleOptions ?? new Net.SduelBattleOption[0])
                        request.Options.Add(new BattleOption
                        {
                            Attacker = MirrorCard(option.attackerId),
                            Target = MirrorCard(option.targetId),
                            Direct = option.direct,
                            EndBattle = option.endBattle,
                            Label = option.label ?? ""
                        });
                    return request;
                }

                case "yesno":
                    return new YesNoRequest
                    {
                        Title = wire.title ?? "",
                        Question = wire.question ?? "",
                        Card = MirrorCard(wire.cardId),
                        IsPhaseWindow = wire.isPhaseWindow,
                        IsResponse = wire.isResponse
                    };

                case "option":
                {
                    var request = new OptionRequest
                    {
                        Title = wire.title ?? "",
                        AllowCancel = wire.allowCancel,
                        Card = MirrorCard(wire.cardId),
                        IsResponseList = wire.isResponseList,
                        IsPhaseWindow = wire.isPhaseWindow,
                        Searchable = wire.searchable
                    };
                    if (wire.choices != null) request.Options.AddRange(wire.choices);
                    // Master-Duel-Reaktionsliste: die Karte hinter jeder Option
                    if (wire.choiceCardIds != null)
                        foreach (var id in wire.choiceCardIds)
                            request.OptionCards.Add(id > 0 ? MirrorCard(id) : null);
                    return request;
                }

                case "target":
                {
                    var request = new TargetRequest
                    {
                        Title = wire.title ?? "",
                        Count = wire.count,
                        AllowFewer = wire.allowFewer,
                        AllowCancel = wire.allowCancel
                    };
                    foreach (var candidate in wire.candidates ?? new Net.SduelCard[0])
                    {
                        var card = MirrorCandidate(candidate);
                        if (card != null) request.Candidates.Add(card);
                    }
                    return request;
                }

                case "zone":
                {
                    var request = new ZoneSelectRequest
                    {
                        Title = wire.title ?? "",
                        ForPlayer = Player1,
                        Zone = System.Enum.TryParse<ZoneType>(wire.zone, out var zone) ? zone : ZoneType.MonsterZone
                    };
                    if (wire.freeIndices != null) request.FreeIndices.AddRange(wire.freeIndices);
                    return request;
                }
            }
            return null;
        }

        /// <summary>Verpackt die beantwortete Anfrage als Intent für den Server.</summary>
        public Net.SduelAnswer MirrorAnswer(DuelRequest request, int reqId)
        {
            var answer = new Net.SduelAnswer { reqId = reqId };
            switch (request)
            {
                case StartChoiceRequest start:
                    answer.first = start.Result;
                    break;
                case MainActionRequest main:
                    answer.chosen = main.Chosen;
                    if (main.Chosen >= 0 && main.Chosen < main.Options.Count)
                        answer.zone = main.Options[main.Chosen].PreferredZoneIndex;
                    break;
                case BattleActionRequest battle:
                    answer.chosen = battle.Chosen;
                    break;
                case YesNoRequest yesNo:
                    answer.result = yesNo.Result;
                    break;
                case OptionRequest option:
                    answer.chosen = option.Result;
                    break;
                case TargetRequest target:
                    answer.cancelled = target.Cancelled;
                    var ids = new List<int>();
                    foreach (var card in target.Result)
                    {
                        int id = MirrorIdOf(card);
                        if (id != 0) ids.Add(id);
                    }
                    answer.ids = ids.ToArray();
                    break;
                case ZoneSelectRequest zoneSelect:
                    answer.index = zoneSelect.Result;
                    break;
            }
            return answer;
        }

        // ================== ENDE ==================

        public void MirrorEnd(bool localWon)
        {
            if (!IsMirror || Result != DuelResult.None) return;
            Result = localWon ? DuelResult.Player1Wins : DuelResult.Player2Wins;
            DuelRunning = false;
            // Kein eigener Log — die "DUEL OVER"-Zeile kommt bereits aus dem Server-Log
            BoardChanged();
            OnDuelEnded?.Invoke(Result);
        }
    }
}
