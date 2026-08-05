#!/bin/bash
# Ende-zu-Ende: Account anlegen, viele Packs öffnen, Finishes prüfen.
set -u
STAGE=/tmp/rouge-packs
cleanup() { [ -n "${PID:-}" ] && kill "$PID" 2>/dev/null; wait "${PID:-}" 2>/dev/null; }
trap cleanup EXIT

# Reste eines Fehllaufs auf DIESEM Port beenden — niemals pauschal nach
# 'node server.js' suchen, das würde die Produktion mit erwischen.
# Erkannt wird der Rest am Arbeitsverzeichnis, nicht an der Kommandozeile —
# "node server.js" sieht bei Produktion und Staging identisch aus.
for pid in $(fuser -n tcp 7779 2>/dev/null); do
  if [ "$(readlink -f /proc/$pid/cwd 2>/dev/null)" = "$STAGE" ]; then
    echo "beende Rest-Prozess $pid auf Port 7779"; kill "$pid" 2>/dev/null; sleep 1
  fi
done

rm -rf "$STAGE"; mkdir -p "$STAGE/data"
ln -s /opt/rouge-tcg/node_modules "$STAGE/node_modules"
cp /opt/rouge-tcg/package.json "$STAGE/"
cp /opt/rouge-tcg/data/*.json "$STAGE/data/"
for f in server db ranks finishes; do cp "/tmp/stage-$f.js" "$STAGE/$f.js"; done
# Nur für den Test: Startguthaben hochsetzen, damit 30 Packs bezahlbar sind
sed -i 's/startCoins: 1500,/startCoins: 99000,/' "$STAGE/server.js"

cd "$STAGE" && PORT=7779 DATA_DIR="$STAGE/data" node server.js > "$STAGE/log" 2>&1 &
PID=$!
sleep 3
kill -0 "$PID" 2>/dev/null || { echo "Server tot:"; cat "$STAGE/log"; exit 1; }

cd "$STAGE" && node --input-type=module -e "
import { WebSocket } from 'ws';
const ws = new WebSocket('ws://127.0.0.1:7779');
const send = o => ws.send(JSON.stringify(o));
const PACKS = 30;
let opened = 0, drawn = 0;
const seen = [0, 0, 0, 0];
const names = ['Plain', 'Glossy', 'Rainbow', 'Static'];
let packName = null, failed = 0;
const check = (c, m) => { console.log((c ? '  ok   ' : '  FAIL ') + m); if (!c) failed++; };
const done = () => { clearTimeout(timer); ws.close(); process.exit(failed ? 1 : 0); };
const timer = setTimeout(() => { console.log('  FAIL Zeitüberschreitung'); process.exit(1); }, 40000);

ws.on('open', () => send({ t: 'register', name: 'FinishProbe', pass: 'test1234' }));
ws.on('message', raw => {
  const m = JSON.parse(raw);
  if (m.t === 'welcome') return;
  if (m.t === 'error') { console.log('  FAIL ' + m.msg); return done(); }

  if (m.t === 'auth_ok') {
    const p = m.profile;
    check(Array.isArray(p.collectionPlain), 'Profil liefert die Finish-Fächer');
    const idx = p.collectionCards.indexOf(p.collectionCards[0]);
    check(p.collectionCounts[idx] === p.collectionPlain[idx], 'Startsammlung ist vollständig schlicht');
    packName = 'Relic Pack';
    return send({ t: 'buy_pack', pack: packName });
  }

  // Kaufen und Öffnen wechseln sich ab; beide antworten mit einem Profil.
  if (m.t === 'profile' && packName && opened < PACKS) {
    if ((m.profile.packCounts || []).some(c => c > 0)) return send({ t: 'open_pack', pack: packName });
    return send({ t: 'buy_pack', pack: packName });
  }

  if (m.t === 'pack_result') {
    opened++;
    if (opened === 1)
      check(Array.isArray(m.packFinishes) && m.packFinishes.length === m.packCards.length,
        'pack_result trägt ein Finish je Karte');
    for (const f of m.packFinishes) { seen[f]++; drawn++; }
    if (opened < PACKS) return send({ t: 'buy_pack', pack: packName });

    console.log('');
    console.log('  ' + drawn + ' Karten aus ' + PACKS + ' Packs:');
    for (let i = 0; i < 4; i++)
      console.log('    ' + names[i].padEnd(8) + String(seen[i]).padStart(4) + '  ' + (seen[i] / drawn * 100).toFixed(1) + '%');
    check(seen[0] > drawn * 0.7, 'die grosse Mehrheit bleibt schlicht');
    check(seen[1] > 0, 'Glossy kommt vor');
    // pack_result bringt das aktuelle Profil gleich mit
    return verify(m.profile);
  }
});

function verify(p) {
  {
    let plain = 0, glossy = 0, rainbow = 0, stat = 0;
    for (let i = 0; i < p.collectionCards.length; i++) {
      plain += p.collectionPlain[i]; glossy += p.collectionGlossy[i];
      rainbow += p.collectionRainbow[i]; stat += p.collectionStatic[i];
    }
    console.log('');
    console.log('  Sammlung: ' + plain + ' schlicht, ' + glossy + ' glossy, ' + rainbow + ' rainbow, ' + stat + ' static');
    check(glossy + rainbow + stat > 0, 'Finishes landen dauerhaft in der Sammlung');
    const totals = p.collectionCounts.reduce((a, b) => a + b, 0);
    check(totals === plain + glossy + rainbow + stat, 'Gesamtzahl deckt sich mit der Summe der Fächer');
    return done();
  }
}
"
RESULT=${PIPESTATUS[0]}
echo ""
echo "=== Serverlog (Auszug) ==="
grep -i "öffnet" "$STAGE/log" | tail -4
exit $RESULT
