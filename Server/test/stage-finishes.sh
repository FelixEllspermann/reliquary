#!/bin/bash
# Prüft die Finish-Migration an einer KOPIE der Produktionsdatenbank.
set -u
STAGE=/tmp/rouge-finish
rm -rf "$STAGE"
mkdir -p "$STAGE/data"
ln -s /opt/rouge-tcg/node_modules "$STAGE/node_modules"
cp /opt/rouge-tcg/package.json "$STAGE/"
cp /tmp/stage-db.js       "$STAGE/db.js"
cp /tmp/stage-ranks.js    "$STAGE/ranks.js"
cp /tmp/stage-finishes.js "$STAGE/finishes.js"

cp /opt/rouge-tcg/data/accounts.db     "$STAGE/data/"
cp /opt/rouge-tcg/data/accounts.db-wal "$STAGE/data/" 2>/dev/null
cp /opt/rouge-tcg/data/accounts.db-shm "$STAGE/data/" 2>/dev/null

echo "=== Zustand VOR der Migration ==="
sqlite3 "$STAGE/data/accounts.db" "SELECT COUNT(*) || ' Sammlungszeilen, ' || SUM(count) || ' Karten' FROM collection;"

cd "$STAGE" && node --input-type=module -e "
import { openDatabase } from './db.js';
import * as f from './finishes.js';

const db = openDatabase('$STAGE/data', (m) => console.log('  [db] ' + m));
const all = db.loadAll();
const names = Object.keys(all);
let bad = 0, cards = 0;

console.log('');
console.log('=== Nach der Migration ===');
for (const key of names) {
  const acc = all[key];
  let total = 0, entries = 0, shaped = true;
  for (const [name, entry] of Object.entries(acc.collection)) {
    if (!Array.isArray(entry) || entry.length !== 4) shaped = false;
    total += f.total(entry); entries++;
  }
  cards += total;
  console.log((shaped ? '  ok   ' : '  FAIL ') + acc.name.padEnd(12)
    + entries + ' Karten, ' + total + ' Exemplare, Coins ' + acc.coins + ', Decks ' + (acc.decks||[]).length);
  if (!shaped) bad++;
}
console.log('  Exemplare gesamt: ' + cards);

// Ein Finish hinzufügen, schreiben, wieder lesen
const key = names[0];
const probe = all[key];
const card = Object.keys(probe.collection)[0];
const before = f.total(probe.collection[card]);
f.add(probe.collection, card, f.STATIC, 2);
f.add(probe.collection, card, f.RAINBOW, 1);
db.save(key, probe);
db.flush();

const again = db.loadAll()[key];
const entry = again.collection[card];
const ok = entry[f.STATIC] === 2 && entry[f.RAINBOW] === 1 && f.total(entry) === before + 3;
console.log('');
console.log((ok ? '  ok   ' : '  FAIL ') + card + ' -> ' + JSON.stringify(entry)
  + ' (vorher ' + before + ' schlicht, jetzt +2 Static +1 Rainbow)');
if (!ok) bad++;

// Besitzprüfung wie beim Deckspeichern
console.log((f.owns(again.collection, card, f.STATIC, 2) ? '  ok   ' : '  FAIL ') + 'zwei Static im Deck erlaubt');
console.log((!f.owns(again.collection, card, f.STATIC, 3) ? '  ok   ' : '  FAIL ') + 'drei Static abgelehnt');

// Sammlung darf sonst unverändert sein
const sameCoins = again.coins === probe.coins;
const sameCards = Object.keys(again.collection).length === Object.keys(probe.collection).length;
console.log(((sameCoins && sameCards) ? '  ok   ' : '  FAIL ') + 'Coins und Kartenzahl unverändert');
if (!sameCoins || !sameCards) bad++;

db.close();
console.log(bad === 0 ? '\n  Migration unbedenklich.' : '\n  ' + bad + ' Problem(e).');
process.exit(bad === 0 ? 0 : 1);
"
RESULT=$?

echo ""
echo "=== Tabellenform danach ==="
sqlite3 "$STAGE/data/accounts.db" ".schema collection"
exit $RESULT
