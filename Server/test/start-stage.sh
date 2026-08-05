#!/bin/bash
# Startet eine Staging-Instanz auf 7778 mit eigener Datenbank und lässt sie laufen.
# Die Produktion auf 7777 bleibt unberührt.
set -u
STAGE=/tmp/rouge-stage7778

# Rest eines früheren Laufs beenden (nur Staging, niemals /opt/rouge-tcg)
for pid in $(ls /proc | grep -E '^[0-9]+$'); do
  cwd=$(readlink /proc/$pid/cwd 2>/dev/null) || continue
  case "$cwd" in /tmp/rouge-stage7778*) kill "$pid" 2>/dev/null ;; esac
done
sleep 1

rm -rf "$STAGE"; mkdir -p "$STAGE/data"
ln -s /opt/rouge-tcg/node_modules "$STAGE/node_modules"
cp /opt/rouge-tcg/package.json "$STAGE/"
cp /opt/rouge-tcg/data/*.json "$STAGE/data/"
for f in server db ranks finishes; do cp "/opt/rouge-tcg/$f.js" "$STAGE/$f.js"; done
sed -i 's/startCoins: 1500,/startCoins: 99000,/' "$STAGE/server.js"

cd "$STAGE" && PORT=7778 DATA_DIR="$STAGE/data" setsid nohup node server.js > "$STAGE/log" 2>&1 &
sleep 3
if grep -q "läuft auf Port 7778" "$STAGE/log"; then
  echo "Staging läuft auf 7778"
  # Testaccount mit gemischten Exemplaren vorbereiten
  cd "$STAGE" && node --input-type=module -e "
  import { WebSocket } from 'ws';
  const ws = new WebSocket('ws://127.0.0.1:7778');
  const send = o => ws.send(JSON.stringify(o));
  let opened = 0;
  ws.on('open', () => send({ t: 'register', name: 'FinishTest', pass: 'test1234' }));
  ws.on('message', raw => {
    const m = JSON.parse(raw);
    if (m.t === 'error') { console.log('  Fehler: ' + m.msg); process.exit(0); }
    if (m.t === 'auth_ok' || m.t === 'profile') {
      if (opened >= 40) {
        const p = m.profile;
        let g = 0, r = 0, s = 0;
        for (let i = 0; i < p.collectionCards.length; i++) { g += p.collectionGlossy[i]; r += p.collectionRainbow[i]; s += p.collectionStatic[i]; }
        console.log('  Testaccount FinishTest / test1234 — ' + g + ' glossy, ' + r + ' rainbow, ' + s + ' static');
        process.exit(0);
      }
      if ((m.profile.packCounts || []).some(c => c > 0)) return send({ t: 'open_pack', pack: 'Relic Pack' });
      return send({ t: 'buy_pack', pack: 'Relic Pack' });
    }
    if (m.t === 'pack_result') { opened++; send({ t: 'buy_pack', pack: 'Relic Pack' }); }
  });
  setTimeout(() => { console.log('  Zeitüberschreitung'); process.exit(0); }, 30000);
  "
else
  echo "Staging nicht gestartet:"; cat "$STAGE/log"
fi
