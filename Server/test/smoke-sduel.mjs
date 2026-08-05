// E2E-Test des server-autoritativen Duells: zwei WS-Clients (smoke1/smoke2)
// queuen sich, spielen mit einer simplen Politik ein komplettes Duell über den
// DuelHost und prüfen dabei das Wichtigste — dass verdeckte Information den
// Server NIE verlässt (Gegnerhand ohne Namen, verdeckte Karten maskiert).
//   node smoke-sduel.mjs [ws://127.0.0.1:7777]
import WebSocket from 'ws';

const URL = process.argv[2] || 'ws://127.0.0.1:7777';
const fail = msg => { console.error('FAIL:', msg); process.exit(1); };
setTimeout(() => fail('Timeout (120s)'), 120_000).unref?.();

let winners = [];
let statesSeen = 0;
let requestsAnswered = 0;

function startClient(name, pass) {
  const ws = new WebSocket(URL);
  const send = obj => ws.send(JSON.stringify(obj));
  let registered = false;

  ws.on('open', () => {
    send({ t: 'hello', name, sduel: true });
    send({ t: 'register', name, pass });
  });

  ws.on('message', raw => {
    const m = JSON.parse(raw);
    switch (m.t) {
      case 'error':
        if (!registered) { registered = true; send({ t: 'login', name, pass }); return; }
        // Deck-/Spielfehler sind im Smoke fatal
        if (!String(m.msg).includes('claim')) fail(`${name}: ${m.msg}`);
        break;

      case 'auth_ok':
        registered = true;
        send({ t: 'hello', name, sduel: true });   // sduel nach Login erneut markieren
        send({ t: 'queue', deckIndex: 0 });
        break;

      case 'sduel_start':
        console.log(`  ${name}: Duell ${m.duelId} als ${m.youAre} gegen ${m.opponent}`);
        break;

      case 'sduel':
        handleDuel(m);
        break;
    }
  });

  function handleDuel(m) {
    switch (m.op) {
      case 'state': {
        statesSeen++;
        const view = m.view;
        // ---- Sichtschutz-Prüfungen ----
        // Gegnerhand kommt als ID-Liste — aber NIE mit Namen
        for (const card of view.foe.hand ?? [])
          if (card && card.name) fail(`${name}: Gegnerhand-Karte mit Namen (${card.name})`);
        for (const card of view.foe.extra ?? [])
          if (card && card.name) fail(`${name}: Gegner-Extra-Karte mit Namen (${card.name})`);
        for (const card of view.you.hand ?? [])
          if (!card.name) fail(`${name}: eigene Handkarte ohne Namen`);
        for (const card of view.foe.monsters ?? [])
          if (card && card.faceDown && card.name)
            fail(`${name}: verdecktes Gegner-Monster mit Namen (${card.name})`);
        for (const card of view.foe.spells ?? [])
          if (card && card.faceDown && card.name)
            fail(`${name}: verdeckter Gegner-Zauber mit Namen (${card.name})`);
        break;
      }

      case 'events':
        for (const evt of m.events ?? [])
          if (evt.type === 'draw' && !evt.mine && evt.cardName)
            fail(`${name}: Gegner-Draw mit Kartennamen (${evt.cardName})`);
        break;

      case 'request':
        send({ t: 'sduel_intent', answer: answerFor(m.request) });
        requestsAnswered++;
        break;

      case 'end':
        console.log(`  ${name}: Duell vorbei — Sieger ${m.winner}`);
        winners.push(m.winner);
        if (winners.length === 2) finish();
        break;
    }
  }

  function answerFor(request) {
    switch (request.type) {
      case 'start': return { first: true };
      case 'main': {
        const options = request.mainOptions ?? [];
        const summon = options.find(o => o.kind === 'SummonMonster');
        if (summon) return { chosen: summon.i };
        const battle = options.find(o => o.kind === 'ToBattlePhase');
        if (battle) return { chosen: battle.i };
        const end = options.find(o => o.kind === 'EndTurn');
        return { chosen: end ? end.i : 0 };
      }
      case 'battle': {
        const options = request.battleOptions ?? [];
        const attack = options.find(o => !o.endBattle);
        return { chosen: attack ? options.indexOf(attack) : options.findIndex(o => o.endBattle) };
      }
      case 'yesno': return { result: false };
      // Nie abbrechen: ein Cancel bei der Positionswahl würde die Beschwörung
      // abbrechen und die Hauptphase böte sie sofort wieder an — Endlosschleife.
      case 'option': return { chosen: 0 };
      case 'target': {
        // Auch abbrechbare Targets beantworten (Tribute!) — Cancel würde die
        // Beschwörung abbrechen und die Hauptphase böte sie erneut an.
        const need = Math.min(request.count, (request.candidates ?? []).length);
        return { ids: (request.candidates ?? []).slice(0, need).map(c => c.id) };
      }
      case 'zone': return { index: (request.freeIndices ?? [0])[0] };
      default: return {};
    }
  }

  return ws;
}

function finish() {
  if (winners[0] !== winners[1]) fail(`Sieger uneins: ${winners.join(' vs ')}`);
  console.log(`SMOKE OK — Sieger ${winners[0]}, ${statesSeen} Sichten, ${requestsAnswered} beantwortete Requests, Gegnerhand nie übertragen.`);
  process.exit(0);
}

console.log('Server-Duell-Smoke gegen', URL);
startClient('smoke1', 'smoketest1');
setTimeout(() => startClient('smoke2', 'smoketest2'), 400);
