using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Client eines server-autoritativen Duells: empfängt Sichten, Ereignisse und
    /// Entscheidungs-Anfragen vom Server, hält den Spiegel-DuelManager aktuell und
    /// leitet Anfragen an die GANZ NORMALE Duell-UI weiter — Board, Prompts,
    /// Drag &amp; Drop und Presenter arbeiten unverändert, nur die Engine sitzt
    /// auf dem Server. Antworten gehen als Intents zurück.
    ///
    /// Verarbeitung pro Server-Flush: events werden gepuffert, state sofort
    /// angewendet, danach die gepufferten Animationen abgespielt, zuletzt die
    /// Anfrage präsentiert (bis dahin schickt der Server nichts Neues).
    /// </summary>
    public class ServerDuelClient : MonoBehaviour
    {
        [SerializeField] private DuelHost duelHost;
        [SerializeField] private DuelUIController ui;

        private readonly Queue<NetMessage> inbox = new Queue<NetMessage>();
        private readonly List<SduelEvent> pendingEvents = new List<SduelEvent>();
        private DuelManager duel;
        private DuelPresenter presenter;

        /// <summary>Die gerade offene Anfrage (für Debug-Werkzeuge und Tests).</summary>
        public DuelRequest CurrentRequest { get; private set; }

        private void Start()
        {
            if (!MatchContext.IsServerMatch || duelHost == null) return;

            duel = duelHost.Duel;
            presenter = duelHost.ScenePresenter;
            duel.MirrorBegin(MatchContext.LocalName, MatchContext.RemoteName);
            StartCoroutine(Pipeline());
        }

        private void OnDestroy()
        {
            // Zuschauer melden sich beim Verlassen der Szene ab — egal auf welchem
            // Weg sie rausgehen (Surrender-Knopf, GameOver, Menü).
            if (MatchContext.SpectateMode)
            {
                var net = Rouge.Tcg.Net.NetworkManager.Instance;
                if (net != null && net.IsConnected) net.SendSpectateLeave();
                MatchContext.SpectateMode = false;
            }
        }

        /// <summary>
        /// Aufgeben wirkt SOFORT. Vorher schickte der Surrender-Knopf nur das
        /// leave und wartete auf das end des Servers — das hing aber in der
        /// Nachrichtenschleife fest, wenn gerade eine eigene Anfrage offen war
        /// (die Schleife liest nichts, solange sie auf die Antwort wartet) oder
        /// noch ein Berg Ereignis-Animationen davor lag. Jetzt: leave senden,
        /// alle eigenen Abläufe stoppen und das Duell lokal als Niederlage
        /// beenden. Das end des Servers bestätigt später nur noch; Rang,
        /// Historie und Statistik bucht der Server ohnehin unabhängig davon.
        /// </summary>
        public void SurrenderNow()
        {
            if (duel == null || duel.Result != DuelResult.None) return;
            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected) net.SendLeave();
            StopAllCoroutines();   // Pipeline, offener Request, Event-Wiedergabe
            CurrentRequest = null;
            duel.MirrorEnd(false); // feuert OnDuelEnded → Ergebnis-Bildschirm sofort
        }

        /// <summary>
        /// Holt alle inzwischen eingetroffenen Server-Duell-Nachrichten aus dem
        /// NetworkManager-Puffer. Der überlebt den Szenenwechsel — so geht die
        /// Eröffnung (Münzwurf + Startwahl) nicht mehr verloren, die der DuelHost
        /// schickt, während der Client noch im Match-Found-Popup steht.
        /// </summary>
        private void DrainNetworkInbox()
        {
            var network = NetworkManager.Instance;
            if (network == null) return;
            while (network.TryDequeueSduel(out var message)) inbox.Enqueue(message);
        }

        private IEnumerator Pipeline()
        {
            // Der Lade-Übergang liegt noch über allem — die Nachrichten sammeln
            // sich solange im Posteingang, damit die Eröffnung sichtbar bleibt.
            //
            // Freigeben muss ihn HIER jemand: bei einem Server-Duell steigt der
            // DuelHost in Start() vorher aus (er rechnet ja nichts), also käme
            // seine Freigabe nie — und der Vorhang bliebe für immer stehen.
            DuelLoadTransition.Release();
            while (DuelLoadTransition.CurtainHolding) yield return null;

            while (duel != null && duel.Result == DuelResult.None)
            {
                DrainNetworkInbox();
                if (inbox.Count == 0) { yield return null; continue; }
                var message = inbox.Dequeue();
                switch (message.op)
                {
                    case "events":
                        // Sofort abspielen, nicht puffern. Der Server schickt sie
                        // an der Naht, an der sie hingehören — vor dem Zustand, den
                        // sie herbeiführen. Wer sie sammelt und erst nach dem
                        // Zustand abspielt, animiert eine Zerstörung auf einem
                        // Brett, auf dem die Karte nicht mehr liegt.
                        if (message.events != null) pendingEvents.AddRange(message.events);
                        yield return PlayPendingEvents();
                        break;

                    case "log":
                        if (message.lines != null)
                            foreach (var line in message.lines) duel.Log(line);
                        break;

                    case "state":
                        duel.MirrorApplyState(message.view);
                        break;

                    case "request":
                        yield return PlayPendingEvents();
                        yield return HandleRequest(message.request);
                        break;

                    case "end":
                        yield return PlayPendingEvents();
                        duel.MirrorEnd(message.winner == (MatchContext.LocalIsPlayerA ? "A" : "B"));
                        yield break;

                    case "waiting":
                        // Der Gegner entscheidet gerade — ohne diesen Hinweis sieht
                        // ein stilles Brett aus wie ein hängendes.
                        if (ui != null) ui.ShowOpponentThinking(message.text);
                        break;

                    case "error":
                        duel.Log("SERVER ERROR: " + message.msg);
                        break;
                }
            }
        }

        // ================== ANFRAGEN ==================

        private IEnumerator HandleRequest(SduelRequest wire)
        {
            if (wire == null) yield break;
            var request = duel.MirrorMaterialize(wire);

            // Eine Anfrage, die dieser Client nicht bauen kann, darf das Duell nicht
            // anhalten: der Server wartet auf genau diese Antwort und schickt bis
            // dahin nichts mehr. Beide Spieler stehen dann still, und keiner kann
            // etwas dagegen tun. Also lieber eine leere Antwort und eine Zeile im
            // Log als ein Duell, das nur noch der Neustart beendet.
            if (request == null)
            {
                duel.Log($"[sync] Unbekannte Anfrage \"{wire.type}\" — übersprungen.");
                Debug.LogWarning($"ServerDuelClient: Anfrage \"{wire.type}\" nicht materialisierbar (reqId {wire.reqId}).");
                if (NetworkManager.Instance != null)
                    NetworkManager.Instance.SendSduelIntent(new SduelAnswer { reqId = wire.reqId, chosen = 0 });
                yield break;
            }

            // Dasselbe für eine Zielwahl ohne Ziele: der Server hat Kandidaten
            // angeboten, die dieser Client nicht kennt (etwa eine verdeckte Karte,
            // die nie in seinen Zustand kam). Es gibt nichts zum Anklicken, also
            // wartet die UI bis in alle Ewigkeit.
            if (request is TargetRequest emptyTargets && emptyTargets.Candidates.Count == 0)
            {
                int offered = wire.candidates != null ? wire.candidates.Length : 0;
                duel.Log(offered > 0
                    ? "[sync] Ziele konnten nicht zugeordnet werden — Auswahl übersprungen."
                    : "[sync] Keine gültigen Ziele.");
                if (offered > 0)
                    Debug.LogWarning($"ServerDuelClient: {offered} Ziele angeboten, keines im Spiegel bekannt (reqId {wire.reqId}).");
                if (NetworkManager.Instance != null)
                    NetworkManager.Instance.SendSduelIntent(duel.MirrorAnswer(emptyTargets, wire.reqId));
                yield break;
            }

            CurrentRequest = request;

            switch (request)
            {
                case StartChoiceRequest start:
                {
                    bool answered = false;
                    if (ui != null)
                        ui.AskStartChoice(first => { start.Result = first; answered = true; });
                    else answered = true;
                    while (!answered) yield return null;
                    break;
                }
                case MainActionRequest main: yield return Handle(main); break;
                case BattleActionRequest battle: yield return Handle(battle); break;
                case YesNoRequest yesNo: yield return Handle(yesNo); break;
                case OptionRequest option: yield return Handle(option); break;
                case TargetRequest target: yield return Handle(target); break;
                case ZoneSelectRequest zoneSelect: yield return Handle(zoneSelect); break;
            }

            CurrentRequest = null;
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.SendSduelIntent(duel.MirrorAnswer(request, wire.reqId));
        }

        private IEnumerator Handle(MainActionRequest r) { if (ui != null) yield return ui.Handle(r); }
        private IEnumerator Handle(BattleActionRequest r) { if (ui != null) yield return ui.Handle(r); }
        private IEnumerator Handle(YesNoRequest r) { if (ui != null) yield return ui.Handle(r); }
        private IEnumerator Handle(OptionRequest r) { if (ui != null) yield return ui.Handle(r); }
        private IEnumerator Handle(TargetRequest r) { if (ui != null) yield return ui.Handle(r); }
        private IEnumerator Handle(ZoneSelectRequest r) { if (ui != null) yield return ui.Handle(r); }

        // ================== ANIMATIONEN ==================

        private IEnumerator PlayPendingEvents()
        {
            if (pendingEvents.Count == 0) yield break;
            var events = new List<SduelEvent>(pendingEvents);
            pendingEvents.Clear();
            if (presenter == null) yield break;

            foreach (var evt in events)
            {
                var player = evt.mine ? duel.Player1 : duel.Player2;
                // Kennt der Spiegel die Karte nicht, wird sie angelegt statt
                // verworfen — sonst fällt die Animation lautlos aus, und genau das
                // liess Extra-Deck-Beschwörungen ohne Tresor erscheinen.
                var card = duel.MirrorEventCard(evt.cardId, evt.cardName, player);
                // Bei der Reliquary-Beschwörung ist targetId die ZONE, keine Karte
                var target = evt.type == "reliquarysummon" ? null : duel.MirrorCard(evt.targetId);

                switch (evt.type)
                {
                    // Merken, wo die Karte JETZT liegt — der Flug danach startet dort.
                    // Kein yield: das ist eine Momentaufnahme, keine Animation.
                    case "remember": if (card != null) presenter.RememberView(card); break;
                    case "rememberorigin": if (card != null) presenter.RememberOrigin(card); break;
                    case "moved": if (card != null) yield return presenter.ShowCardMoved(card); break;
                    // Alte Aktivierungs-Anzeige: es gibt sie nicht mehr, aber ein
                    // Server auf älterem Stand könnte sie noch schicken
                    case "activation": if (card != null) yield return presenter.ShowActivationPulse(card, false); break;

                    // Kettenanzeige. Der Kartenname kommt mit, weil ein Glied auch
                    // von einer Karte stammen kann, die der Spiegel nie gesehen
                    // hat — eine Handkarte des Gegners zum Beispiel.
                    case "chainlink":
                        yield return presenter.ShowChainLink(card, evt.text, player, evt.link);
                        break;
                    case "chainresolve": yield return presenter.ShowChainResolve(card, evt.link); break;
                    case "chainend": yield return presenter.ShowChainEnd(); break;

                    case "banner": yield return presenter.ShowPhaseBanner(evt.text ?? ""); break;
                    case "cointoss": yield return presenter.ShowCoinToss(player); break;
                    case "draw": if (card != null) yield return presenter.ShowCardDrawn(player, card); break;
                    case "shuffle": yield return presenter.ShowHandShuffle(player); break;
                    case "summon": if (card != null) yield return presenter.ShowSummon(card); break;
                    case "reliquarysummon":
                        // `mine` sagt, wessen Extra Deck sich öffnet, targetId ist die Zone
                        if (card != null)
                            yield return presenter.ShowReliquarySummon(card, player, evt.targetId);
                        break;
                    case "position": if (card != null) yield return presenter.ShowPositionSwitch(card); break;
                    case "milled": if (card != null && player != null) yield return presenter.ShowMilled(player, card); break;
                    case "reveal": if (card != null) yield return presenter.ShowCardRevealed(card, evt.text ?? ""); break;
                    case "pulse":
                        if (card != null)
                        {
                            // Effekt-Infos aus dem Event zu einer Anzeige-Definition
                            // zusammensetzen — ein alter Server schickt sie nicht,
                            // dann bleibt es beim schlichten Puls ohne Panel.
                            EffectDefinition pulseFx = null;
                            if (!string.IsNullOrEmpty(evt.effectText) || !string.IsNullOrEmpty(evt.text))
                                pulseFx = new EffectDefinition
                                {
                                    label = evt.text,
                                    text = evt.effectText,
                                    manaCost = evt.effectCost,
                                    isInfused = evt.effectInfused > 0,
                                    infusedKind = evt.effectInfused == 2 ? InfusedKind.Coupled : InfusedKind.Standalone
                                };
                            yield return presenter.ShowActivationPulse(card, false, pulseFx);
                        }
                        break;
                    case "targets": if (card != null) yield return presenter.ShowTargetsFlash(new List<CardInstance> { card }); break;
                    case "attack": if (card != null) yield return presenter.ShowAttackDeclared(card, target, evt.direct); break;
                    case "impact": if (card != null) yield return presenter.ShowAttackImpact(card, target, evt.direct); break;
                    case "destroyed": if (card != null) yield return presenter.ShowCardDestroyed(card); break;
                    case "tograve": if (card != null) yield return presenter.ShowCardSentToGrave(card); break;
                    case "spelltograve": if (card != null) yield return presenter.ShowSpellToGrave(card); break;
                    case "banished": if (card != null) yield return presenter.ShowCardBanished(card); break;
                }
            }
        }

        /// <summary>Effekt der Karte anhand des Server-Labels wiederfinden (fürs Showcase).</summary>
        private static EffectDefinition MirrorEffect(CardInstance card, string label)
        {
            if (card?.Definition?.effects == null) return null;
            foreach (var effect in card.Definition.effects)
                if (effect != null && effect.label == label) return effect;
            return null;
        }
    }
}
