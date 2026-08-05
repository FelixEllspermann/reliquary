// Testet den Steam-Anmeldeweg gegen eine Instanz mit STEAM_DEV_MODE=1.
// Prüft: (1) Erstanmeldung legt einen Account an, (2) dieselbe SteamID landet
// wieder im GLEICHEN Account, (3) eine zweite SteamID bekommt einen eigenen,
// (4) Passwort-Login auf einen Steam-Account wird abgelehnt, (5) Verknüpfen
// einer SteamID, die schon vergeben ist, scheitert sauber.
//   node smoke-steam.mjs [ws://127.0.0.1:7778]
import WebSocket from 'ws';

const URL = process.argv[2] || 'ws://127.0.0.1:7778';
const fail = msg => { console.error('FAIL:', msg); process.exit(1); };
setTimeout(() => fail('Timeout'), 25_000).unref?.();

/** Öffnet eine Verbindung und liefert send/next-Helfer. */
function connect() {
  const ws = new WebSocket(URL);
  const queue = [];
  const waiters = [];
  ws.on('message', raw => {
    const m = JSON.parse(raw);
    if (waiters.length) waiters.shift()(m); else queue.push(m);
  });
  const next = () => new Promise(res => queue.length ? res(queue.shift()) : waiters.push(res));
  return {
    ws,
    open: () => new Promise(res => ws.on('open', res)),
    send: obj => ws.send(JSON.stringify(obj)),
    next,
    /** Wartet auf die nächste Nachricht, überspringt aber Begrüssungen/Status. */
    reply: async () => {
      for (let i = 0; i < 10; i++) {
        const m = await next();
        if (m.t !== 'welcome' && m.t !== 'online') return m;
      }
      throw new Error('nur Begrüssungen empfangen');
    },
    close: () => ws.close()
  };
}

const run = async () => {
  // --- 1) Erstanmeldung über Steam ---
  const a = connect();
  await a.open();
  a.send({ t: 'hello', name: 'SteamTester', sduel: true });
  a.send({ t: 'steam_auth', steamTicket: 'dev:76561198000000001', steamName: 'Vault Hunter' });
  let m = await a.reply();
  if (m.t !== 'auth_ok') fail('Erstanmeldung: ' + JSON.stringify(m));
  const firstName = m.profile.account;
  if (!m.profile.steamLinked) fail('steamLinked ist false nach Steam-Login');
  if (m.profile.coins !== 1500) fail('Startcoins falsch: ' + m.profile.coins);
  console.log(`  1) Neuer Steam-Account: "${firstName}", steamLinked=${m.profile.steamLinked}, ${m.profile.coins} Coins`);
  a.close();

  // --- 2) Gleiche SteamID -> gleicher Account ---
  const b = connect();
  await b.open();
  b.send({ t: 'hello', name: 'x', sduel: true });
  b.send({ t: 'steam_auth', steamTicket: 'dev:76561198000000001', steamName: 'Ganz Anderer Name' });
  m = await b.reply();
  if (m.t !== 'auth_ok') fail('Wiederanmeldung: ' + JSON.stringify(m));
  if (m.profile.account !== firstName) fail(`Gleiche SteamID -> anderer Account (${m.profile.account} statt ${firstName})`);
  console.log(`  2) Gleiche SteamID landet wieder in "${m.profile.account}" — kein Duplikat`);
  b.close();

  // --- 3) Andere SteamID -> eigener Account ---
  const c = connect();
  await c.open();
  c.send({ t: 'hello', name: 'y', sduel: true });
  c.send({ t: 'steam_auth', steamTicket: 'dev:76561198000000002', steamName: 'Vault Hunter' });
  m = await c.reply();
  if (m.t !== 'auth_ok') fail('Zweite SteamID: ' + JSON.stringify(m));
  if (m.profile.account === firstName) fail('Zweite SteamID teilt sich den Account!');
  const secondName = m.profile.account;
  console.log(`  3) Zweite SteamID (gleicher Persona-Name) -> eigener Account "${secondName}"`);
  c.close();

  // --- 4) Passwort-Login auf einen Steam-Account wird abgelehnt ---
  const d = connect();
  await d.open();
  d.send({ t: 'hello', name: 'z', sduel: true });
  d.send({ t: 'login', name: firstName, pass: 'irgendwas' });
  m = await d.reply();
  if (m.t !== 'error') fail('Passwort-Login auf Steam-Account wurde NICHT abgelehnt: ' + JSON.stringify(m));
  console.log(`  4) Passwort-Login abgelehnt: "${m.msg}"`);

  // --- 5) Fremde SteamID verknüpfen scheitert ---
  d.send({ t: 'register', name: 'PassAccount', pass: 'geheim123' });
  m = await d.reply();
  if (m.t !== 'auth_ok') fail('Registrierung: ' + JSON.stringify(m));
  if (m.profile.steamLinked) fail('Frischer Passwort-Account meldet steamLinked=true');
  d.send({ t: 'steam_link', steamTicket: 'dev:76561198000000001' });   // gehört schon firstName
  m = await d.reply();
  if (m.t !== 'error') fail('Doppelte Verknüpfung wurde erlaubt: ' + JSON.stringify(m));
  console.log(`  5) Bereits vergebene SteamID abgelehnt: "${m.msg}"`);

  // --- 6) Freie SteamID verknüpfen klappt ---
  d.send({ t: 'steam_link', steamTicket: 'dev:76561198000000003' });
  m = await d.reply();
  if (m.t !== 'profile' || !m.profile.steamLinked) fail('Verknüpfen schlug fehl: ' + JSON.stringify(m));
  console.log(`  6) Freie SteamID verknüpft — "${m.profile.account}" steamLinked=${m.profile.steamLinked}`);
  d.close();

  console.log('STEAM-SMOKE OK');
  process.exit(0);
};

console.log('Steam-Anmeldung gegen', URL);
run().catch(e => fail(e.message));
