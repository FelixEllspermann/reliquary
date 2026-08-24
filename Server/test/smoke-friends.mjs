// E2E-Test des Freunde-Systems: zwei WS-Clients befreunden sich über den
// Freundescode, fordern sich heraus, spielen das Duell über den DuelHost zu
// Ende, speichern das Replay, schauen es an und lesen das Fremdprofil.
//   node smoke-friends.mjs [ws://127.0.0.1:7777]
import WebSocket from 'ws';

const URL = process.argv[2] || 'ws://127.0.0.1:7777';
const fail = msg => { console.error('FAIL:', msg); process.exit(1); };
const ok = msg => console.log('  ok —', msg);
setTimeout(() => fail('Timeout (180s)'), 180_000).unref?.();

// Frische Namen je Lauf: alte Freundschaften/Replays verfälschen sonst die Zählungen.
const SUFFIX = Math.random().toString(36).slice(2, 6);
const NAME_A = `smokeFA${SUFFIX}`;
const NAME_B = `smokeFB${SUFFIX}`;

// Der Testablauf als Schrittfolge; jeder Schritt wird durch eine erwartete
// Nachricht abgeschlossen. Reihenfolge siehe unten bei run().
const state = {
  codeA: null, codeB: null,
  duelsEnded: 0,
  replayId: null,
  replayStates: 0, replayEnd: false
};

function connect(name) {
  const c = { name, ws: new WebSocket(URL), handlers: new Map() };
  c.send = obj => c.ws.send(JSON.stringify(obj));
  c.expect = (type, fn) => c.handlers.set(type, fn);
  c.ws.on('open', () => {
    c.send({ t: 'hello', name, sduel: true });
    c.send({ t: 'register', name, pass: 'smokepass' });
  });
  c.ws.on('message', raw => {
    const m = JSON.parse(raw);
    if (m.t === 'error' && !c.authed) { c.send({ t: 'login', name, pass: 'smokepass' }); return; }
    if (m.t === 'auth_ok') {
      c.authed = true;
      c.send({ t: 'hello', name, sduel: true });
      // Frisches Konto: Starterdeck ziehen, sonst gibt es kein Duell-Deck.
      if (m.profile && m.profile.starterPending && m.profile.starters?.length)
        c.send({ t: 'claim_starter', starter: m.profile.starters[0].id });
      c.onAuth?.();
      return;
    }
    if (m.t === 'sduel') { handleDuel(c, m); return; }
    const h = c.handlers.get(m.t);
    if (h) h(m);
    else if (m.t === 'error') fail(`${name}: unerwarteter Fehler "${m.msg}"`);
  });
  return c;
}

// ---- simple Duell-Politik (aus smoke-sduel.mjs) ----
function handleDuel(c, m) {
  if (m.op === 'request') { c.send({ t: 'sduel_intent', answer: answerFor(m.request) }); return; }
  if (m.op === 'state' && c.replayWatching) state.replayStates++;
  if (m.op === 'end') {
    if (c.replayWatching) { state.replayEnd = true; c.onReplayEnd?.(); return; }
    state.duelsEnded++;
    c.onDuelEnd?.(m.winner);
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
    case 'option': return { chosen: 0 };
    case 'target': {
      const need = Math.min(request.count, (request.candidates ?? []).length);
      return { ids: (request.candidates ?? []).slice(0, need).map(x => x.id) };
    }
    case 'zone': return { index: (request.freeIndices ?? [0])[0] };
    default: return {};
  }
}

// ---- der eigentliche Ablauf ----
const A = connect(NAME_A);
const B = connect(NAME_B);

let authCount = 0;
A.onAuth = B.onAuth = () => { if (++authCount === 2) step1(); };

// 1) Beide holen ihre Freundesliste — Codes müssen vergeben sein.
function step1() {
  A.expect('friends', m => {
    if (!m.friendCode || m.friendCode.length !== 8) fail('A ohne 8-stelligen Freundescode');
    state.codeA = m.friendCode;
    ok(`A hat Freundescode ${m.friendCode}`);
    step2();
  });
  A.send({ t: 'friends_get' });
}

// 2) B fügt A per Code hinzu; A muss die Anfrage live sehen.
function step2() {
  A.expect('friend_event', m => {
    if (m.kind !== 'request' || m.name !== NAME_B) return;
    ok(`A sieht Anfrage von ${m.name}`);
    step3();
  });
  B.expect('friend_event', () => {});
  B.send({ t: 'friend_add', code: state.codeA.toLowerCase() }); // Normalisierung mittesten
}

// 3) A nimmt an; beide Listen müssen den Freund online zeigen.
function step3() {
  let confirmed = 0;
  const check = (who, m) => {
    if (!m.friends?.length) return;
    if (m.friends.length !== 1) fail(`${who}: ${m.friends.length} Freunde statt 1`);
    if (!m.friends[0].online) fail(`${who}: Freund gilt als offline`);
    if (++confirmed === 2) { ok('beidseitig befreundet, beide online'); step4(); }
  };
  A.expect('friends', m => check('A', m));
  B.expect('friends', m => check('B', m));
  A.expect('friend_event', () => {});
  A.send({ t: 'friend_accept', name: NAME_B });
}

// 4) A fordert B heraus; B nimmt an; das Duell läuft bis zum Ende.
function step4() {
  B.expect('challenge_incoming', m => {
    if (m.name !== NAME_A) fail(`Challenge von ${m.name} statt ${NAME_A}`);
    ok('B sieht die Herausforderung');
    B.send({ t: 'challenge_accept', name: NAME_A, deckIndex: 0 });
  });
  A.expect('challenge_sent', () => {});
  A.expect('sduel_start', m => ok(`Duell gestartet: A ist ${m.youAre} gegen ${m.opponent}`));
  B.expect('sduel_start', () => {});
  A.onDuelEnd = B.onDuelEnd = winner => {
    if (state.duelsEnded === 2) { ok(`Duell beendet, Sieger ${winner}`); step5(); }
  };
  A.send({ t: 'friend_challenge', name: NAME_B, deckIndex: 0 });
}

// 5) A speichert das Replay.
function step5() {
  A.expect('replay_saved', () => ok('Replay gespeichert'));
  A.expect('replay_list', m => {
    if (!m.replays?.length) fail('Replay-Liste leer nach dem Speichern');
    state.replayId = m.replays[0].replayId;
    ok(`Replay-Liste: ${m.replays.length} Eintrag (id ${state.replayId})`);
    step6();
  });
  A.send({ t: 'replay_save' });
}

// 6) B liest As Profil: Freund-Flag und Replay müssen drinstehen.
function step6() {
  B.expect('profile_view', m => {
    if (m.name !== NAME_A) fail('profile_view: falscher Name');
    if (!m.isFriend) fail('profile_view: isFriend fehlt');
    if (!m.online) fail('profile_view: A gilt als offline');
    if (!m.replays?.length) fail('profile_view: Replays fehlen');
    ok(`B sieht As Profil (Rang ${m.rankName} ${m.rankTier}, ${m.replays.length} Replay)`);
    step7();
  });
  B.send({ t: 'profile_view', name: NAME_A });
}

// 7) B schaut As Replay an — es muss Zustände liefern und mit end schließen.
function step7() {
  B.replayWatching = true;
  B.expect('replay_start', m => ok(`Replay startet: ${m.a} vs ${m.b}`));
  B.onReplayEnd = () => {
    if (state.replayStates < 1) fail('Replay ohne einen einzigen Spielzustand');
    ok(`Replay abgespielt: ${state.replayStates} Zustände bis zum Ende`);
    step8();
  };
  B.send({ t: 'replay_watch', name: NAME_A, replayId: state.replayId });
}

// 8) Aufräumen: Replay löschen, entfreunden — beide Listen leer.
function step8() {
  B.replayWatching = false;
  A.expect('replay_list', m => {
    if (m.replays?.length) fail('Replay-Liste nach dem Löschen nicht leer');
    ok('Replay gelöscht');
    A.expect('friends', m2 => {
      if (m2.friends?.length) fail('A: Freundesliste nach remove nicht leer');
      ok('entfreundet — Listen leer');
      console.log('SMOKE FRIENDS OK');
      process.exit(0);
    });
    A.send({ t: 'friend_remove', name: NAME_B });
  });
  A.send({ t: 'replay_delete', replayId: state.replayId });
}

console.log(`Freunde-Smoke gegen ${URL} (${NAME_A} / ${NAME_B})`);
