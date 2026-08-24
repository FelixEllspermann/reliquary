// Geselliger Dauerläufer für Editor-Tests des Freunde-Systems: loggt sich ein,
// nennt seinen Freundescode, nimmt Anfragen und Herausforderungen automatisch
// an und spielt Duelle mit der Smoke-Politik. Läuft, bis man ihn beendet.
//   node friendbot.mjs [ws://127.0.0.1:7777] [name] [pass]
import WebSocket from 'ws';

const URL = process.argv[2] || 'ws://127.0.0.1:7777';
const NAME = process.argv[3] || 'Robin';
const PASS = process.argv[4] || 'friendbot';
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

const ws = new WebSocket(URL);
const send = obj => ws.send(JSON.stringify(obj));
let authed = false;
const accepted = new Set();   // wen wir schon angenommen haben (kein Accept-Sturm)

ws.on('open', () => {
  send({ t: 'hello', name: NAME, sduel: true });
  send({ t: 'register', name: NAME, pass: PASS });
});

ws.on('close', () => { log('Verbindung zu — Ende.'); process.exit(0); });

ws.on('message', raw => {
  const m = JSON.parse(raw);
  switch (m.t) {
    case 'error':
      if (!authed) { send({ t: 'login', name: NAME, pass: PASS }); return; }
      log('Server:', m.msg);
      break;

    case 'auth_ok':
      authed = true;
      send({ t: 'hello', name: NAME, sduel: true });
      if (m.profile && m.profile.starterPending && m.profile.starters?.length)
        send({ t: 'claim_starter', starter: m.profile.starters[0].id });
      send({ t: 'friends_get' });
      log(`eingeloggt als ${NAME}`);
      break;

    case 'friends':
      log(`FREUNDESCODE: ${m.friendCode}  (${(m.friends ?? []).length} Freunde)`);
      for (const who of m.requests ?? []) {
        if (accepted.has(who)) continue;
        accepted.add(who);
        log(`nehme Freundschaftsanfrage von ${who} an`);
        send({ t: 'friend_accept', name: who });
      }
      break;

    case 'friend_event':
      log(`friend_event: ${m.kind} — ${m.name}`);
      if (m.kind === 'request') send({ t: 'friends_get' });
      break;

    case 'challenge_incoming':
      log(`Herausforderung von ${m.name} — nehme in 1s an`);
      setTimeout(() => send({ t: 'challenge_accept', name: m.name, deckIndex: 0 }), 1000);
      break;

    case 'sduel_start':
      log(`Duell ${m.duelId} als ${m.youAre} gegen ${m.opponent}`);
      break;

    case 'sduel':
      if (m.op === 'request') send({ t: 'sduel_intent', answer: answerFor(m.request) });
      else if (m.op === 'end') log(`Duell vorbei — Sieger ${m.winner}`);
      break;
  }
});

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
    case 'option': return { chosen: 0 };
    case 'target': {
      const need = Math.min(request.count, (request.candidates ?? []).length);
      return { ids: (request.candidates ?? []).slice(0, need).map(x => x.id) };
    }
    case 'zone': return { index: (request.freeIndices ?? [0])[0] };
    default: return {};
  }
}

log(`Friendbot ${NAME} verbindet zu ${URL}`);
setInterval(() => {}, 60000);   // wach bleiben
