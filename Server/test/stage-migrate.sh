#!/bin/bash
# Prüft die Migration an einer KOPIE der Produktionsdatenbank.
# Die Produktion wird dabei nicht angefasst.
set -u
STAGE=/tmp/rouge-migrate
rm -rf "$STAGE"
mkdir -p "$STAGE/data"
ln -s /opt/rouge-tcg/node_modules "$STAGE/node_modules"
cp /opt/rouge-tcg/package.json "$STAGE/"
cp /tmp/stage-db.js "$STAGE/db.js"
cp /tmp/stage-ranks.js "$STAGE/ranks.js"

# WAL mitkopieren, sonst fehlen die zuletzt geschriebenen Daten
cp /opt/rouge-tcg/data/accounts.db     "$STAGE/data/"
cp /opt/rouge-tcg/data/accounts.db-wal "$STAGE/data/" 2>/dev/null
cp /opt/rouge-tcg/data/accounts.db-shm "$STAGE/data/" 2>/dev/null

cd "$STAGE" && node --input-type=module -e "
import { openDatabase } from './db.js';
import * as ranks from './ranks.js';

const db = openDatabase('$STAGE/data', (m) => console.log('  [db] ' + m));
const all = db.loadAll();
const names = Object.keys(all);
console.log('  Accounts gelesen: ' + names.length);

let bad = 0;
for (const key of names) {
  const acc = all[key];
  // Bestandsaccounts haben noch keine Rangdaten — die Leiter muss damit umgehen
  const seal = ranks.rankFor(acc.rp);
  const info = ranks.rankInfo(acc);
  const cards = Object.keys(acc.collection || {}).length;
  if (!seal || !info || seal.rank !== 1) { bad++; console.log('  FAIL ' + acc.name + ': ' + JSON.stringify(seal)); continue; }
  console.log('  ok   ' + acc.name.padEnd(14) + ' ' + info.name + ' ' + info.tier
    + '  RP ' + info.rp + '  Coins ' + acc.coins + '  Karten ' + cards + '  Decks ' + (acc.decks||[]).length);
}

// Saisonwechsel auf einem Bestandsaccount
if (names.length) {
  const probe = all[names[0]];
  ranks.rolloverIfNeeded(probe);
  console.log('  Nach Saison-Abgleich: ' + probe.name + ' Saison=' + probe.season + ' rp=' + probe.rp + ' peakRank=' + probe.peakRank);
}

// Schreiben und erneut lesen — überlebt der Rangblock den Umlauf?
if (names.length) {
  const key = names[0];
  const probe = all[key];
  probe.rp = 850; probe.peakRank = 3; probe.wins = 7; probe.losses = 3; probe.bestStreak = 4;
  db.save(key, probe);
  db.flush();
  const again = db.loadAll()[key];
  const ok = again.rp === 850 && again.peakRank === 3 && again.wins === 7 && again.bestStreak === 4
          && again.coins === probe.coins && Object.keys(again.collection).length === Object.keys(probe.collection).length;
  console.log((ok ? '  ok   ' : '  FAIL ') + 'Rangblock übersteht Schreiben/Lesen — '
    + ranks.rankInfo(again).name + ' ' + ranks.rankInfo(again).tier
    + ', Sammlung ' + Object.keys(again.collection).length + ' Zeilen erhalten');
  if (!ok) bad++;
}

db.close();
console.log(bad === 0 ? '\n  Migration unbedenklich.' : '\n  ' + bad + ' Problem(e).');
process.exit(bad === 0 ? 0 : 1);
"
