#!/bin/bash
# Staging-Test der Rangleiter: eigene Instanz auf Port 7778 mit eigener Datenbank,
# damit die Produktion unberührt bleibt.
set -u
STAGE=/tmp/rouge-stage
cleanup() {
  [ -n "${SERVER_PID:-}" ] && kill "$SERVER_PID" 2>/dev/null
  wait "${SERVER_PID:-}" 2>/dev/null
}
trap cleanup EXIT

rm -rf "$STAGE"
mkdir -p "$STAGE/data"
cp /opt/rouge-tcg/package.json "$STAGE/"
ln -s /opt/rouge-tcg/node_modules "$STAGE/node_modules"
cp /opt/rouge-tcg/data/*.json "$STAGE/data/" 2>/dev/null
cp /tmp/stage-server.js "$STAGE/server.js"
cp /tmp/stage-db.js "$STAGE/db.js"
cp /tmp/stage-ranks.js "$STAGE/ranks.js"
mkdir -p "$STAGE/test"
cp /tmp/stage-ranks.test.mjs "$STAGE/test/ranks.test.mjs"

echo "=== Regeltest ==="
cd "$STAGE" && node test/ranks.test.mjs || exit 1

echo ""
echo "=== Server startet auf 7778 (eigene DB) ==="
cd "$STAGE" && PORT=7778 DATA_DIR="$STAGE/data" node server.js > "$STAGE/server.log" 2>&1 &
SERVER_PID=$!
sleep 3

if ! kill -0 "$SERVER_PID" 2>/dev/null; then
  echo "Server ist nicht gestartet:"
  cat "$STAGE/server.log"
  exit 1
fi

echo "=== Login und Profil prüfen ==="
cd "$STAGE" && node --input-type=module -e "
import { WebSocket } from 'ws';
const ws = new WebSocket('ws://127.0.0.1:7778');
const send = o => ws.send(JSON.stringify(o));
let step = 0;
const timer = setTimeout(() => { console.log('FAIL: Zeitüberschreitung'); process.exit(1); }, 12000);
ws.on('open', () => send({ t: 'register', name: 'RankProbe', pass: 'test1234' }));
ws.on('message', raw => {
  const m = JSON.parse(raw);
  if (m.t === 'welcome') return;
  if (m.t === 'error') { console.log('FAIL:', m.msg); clearTimeout(timer); process.exit(1); }
  if (m.t === 'auth_ok' || m.t === 'profile') {
    const p = m.profile || m;
    if (step++ > 0) return;
    const ok = p.rankValue === 1 && p.rankTier === 1 && p.rankRp === 0
            && p.rankName === 'Ash Seal' && Array.isArray(p.titles) && p.titles.includes('early_vault_hunter');
    console.log(ok ? '  ok   neuer Account startet auf Ash Seal I mit Titel' : '  FAIL Profil: ' + JSON.stringify({
      rankValue: p.rankValue, rankTier: p.rankTier, rankRp: p.rankRp, rankName: p.rankName, titles: p.titles }));
    console.log('  Saison: ' + p.rankSeason + ', nächste Stufe bei ' + p.rankNextAt + ' RP');
    clearTimeout(timer);
    ws.close();
    process.exit(ok ? 0 : 1);
  }
});
" || exit 1

echo ""
echo "=== Migration einer bestehenden Datenbank ==="
cp /opt/rouge-tcg/data/accounts.db "$STAGE/data/migrate-test.db" 2>/dev/null && \
cd "$STAGE" && node --input-type=module -e "
import { openDatabase } from './db.js';
const db = openDatabase('$STAGE/data', () => {});
const all = db.loadAll();
const names = Object.keys(all);
console.log('  Accounts gelesen: ' + names.length);
const sample = all[names[0]];
console.log('  Beispiel ' + (sample ? sample.name : '-') + ': rp=' + (sample?.rp ?? 'undefined') + ' peakRank=' + (sample?.peakRank ?? 'undefined'));
db.close();
" || echo "  (keine Produktions-DB zum Prüfen)"

echo ""
echo "=== Serverlog ==="
tail -15 "$STAGE/server.log"
