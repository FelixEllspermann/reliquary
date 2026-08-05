// Ein einzelner scripted Gegner für manuelle/Editor-Tests: loggt sich als smoke2
// ein, stellt sich in die Queue und spielt ein Server-Duell aggressiv zu Ende
// (beschwören → Battle Phase → angreifen). Beendet sich nach dem Duell.
//   node opp-sduel.mjs [ws://127.0.0.1:7777]
import WebSocket from 'ws';

const URL = process.argv[2] || 'ws://127.0.0.1:7777';
const ws = new WebSocket(URL);
const send = obj => ws.send(JSON.stringify(obj));
let registered = false;
setTimeout(() => { console.error('TIMEOUT'); process.exit(1); }, 300_000);

ws.on('open', () => {
  send({ t: 'hello', name: 'smoke2', sduel: true });
  send({ t: 'register', name: 'smoke2', pass: 'smoketest2' });
});

ws.on('message', raw => {
  const m = JSON.parse(raw);
  if (m.t === 'error' && !registered) { registered = true; send({ t: 'login', name: 'smoke2', pass: 'smoketest2' }); return; }
  if (m.t === 'auth_ok') {
    registered = true;
    send({ t: 'hello', name: 'smoke2', sduel: true });
    send({ t: 'queue', deckIndex: 0 });
    console.log('Gegner wartet in der Queue…');
    return;
  }
  if (m.t === 'sduel_start') { console.log(`Duell ${m.duelId} als ${m.youAre} gegen ${m.opponent}`); return; }
  if (m.t !== 'sduel') return;

  if (m.op === 'request') {
    const r = m.request;
    const answer =
      r.type === 'start' ? { first: true } :
      r.type === 'main' ? mainAnswer(r) :
      r.type === 'battle' ? battleAnswer(r) :
      r.type === 'yesno' ? { result: false } :
      r.type === 'option' ? { chosen: 0 } :
      r.type === 'target' ? { ids: (r.candidates ?? []).slice(0, Math.min(r.count, (r.candidates ?? []).length)).map(c => c.id) } :
      r.type === 'zone' ? { index: (r.freeIndices ?? [0])[0] } : {};
    send({ t: 'sduel_intent', answer });
  }
  if (m.op === 'end') { console.log('Duell vorbei — Sieger', m.winner); process.exit(0); }
});

function mainAnswer(r) {
  const options = r.mainOptions ?? [];
  const summon = options.find(o => o.kind === 'SummonMonster');
  if (summon) return { chosen: summon.i };
  const battle = options.find(o => o.kind === 'ToBattlePhase');
  if (battle) return { chosen: battle.i };
  const end = options.find(o => o.kind === 'EndTurn');
  return { chosen: end ? end.i : 0 };
}

function battleAnswer(r) {
  const options = r.battleOptions ?? [];
  const attack = options.findIndex(o => !o.endBattle);
  return { chosen: attack >= 0 ? attack : options.findIndex(o => o.endBattle) };
}
