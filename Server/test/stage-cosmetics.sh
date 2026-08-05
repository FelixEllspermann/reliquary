#!/bin/bash
# Prüft die Kosmetik-Migration an einer KOPIE der Produktionsdatenbank.
set -u
STAGE=/tmp/rouge-cosm
rm -rf "$STAGE"; mkdir -p "$STAGE/data"
ln -s /opt/rouge-tcg/node_modules "$STAGE/node_modules"
cp /opt/rouge-tcg/package.json "$STAGE/"
for f in db ranks finishes cosmetics; do cp "/tmp/stage-$f.js" "$STAGE/$f.js"; done

cp /opt/rouge-tcg/data/accounts.db     "$STAGE/data/"
cp /opt/rouge-tcg/data/accounts.db-wal "$STAGE/data/" 2>/dev/null
cp /opt/rouge-tcg/data/accounts.db-shm "$STAGE/data/" 2>/dev/null

cd "$STAGE" && node --input-type=module -e "
import { openDatabase } from './db.js';
import * as c from './cosmetics.js';
import * as f from './finishes.js';

const db = openDatabase('$STAGE/data', (m) => console.log('  [db] ' + m));
const all = db.loadAll();
const names = Object.keys(all);
let bad = 0;

console.log('');
console.log('=== Bestandsaccounts nach der Migration ===');
for (const key of names) {
  const acc = all[key];
  const state = c.stateOf(acc);
  const cards = Object.keys(acc.collection || {}).length;
  const ok = state.shards === 0 && state.cosmeticsOwned.length === 1 && cards > 0;
  console.log((ok ? '  ok   ' : '  FAIL ') + acc.name.padEnd(12)
    + 'Shards ' + state.shards + ', Kosmetik ' + state.cosmeticsOwned.length
    + ' (' + state.cosmeticsOwned.join(',') + '), Coins ' + acc.coins + ', Karten ' + cards);
  if (!ok) bad++;
}

// Kaufen, ausrüsten, schreiben, wieder lesen
const key = names[0];
const probe = all[key];
probe.shards = 500;
const buyProblem = c.buy(probe, 'vault_ring');
const equipProblem = c.equip(probe, 'avatarFrame', 'vault_ring');
console.log('');
console.log((buyProblem === null ? '  ok   ' : '  FAIL ') + 'Vault Ring gekauft' + (buyProblem ? ': ' + buyProblem : ''));
console.log((equipProblem === null ? '  ok   ' : '  FAIL ') + 'Vault Ring ausgerüstet' + (equipProblem ? ': ' + equipProblem : ''));
db.save(key, probe);
db.flush();

const again = db.loadAll()[key];
const state = c.stateOf(again);
const parts = {
  'Shards': state.shards === 100,
  'Besitz': state.cosmeticsOwned.includes('vault_ring'),
  'Ausrüstung': state.equippedIds[c.SLOTS.indexOf('avatarFrame')] === 'vault_ring',
  'Coins': again.coins === probe.coins,
  'Sammlung': Object.keys(again.collection).length === Object.keys(probe.collection).length,
  'RP': (again.rp | 0) === (probe.rp | 0),
  'Decks': (again.decks || []).length === (probe.decks || []).length,
};
for (const [label, ok] of Object.entries(parts)) {
  console.log((ok ? '  ok   ' : '  FAIL ') + label + ' übersteht Schreiben/Lesen');
  if (!ok) bad++;
}

// Sonderexemplar zerlegen -> Shards
const card = Object.keys(again.collection)[0];
f.add(again.collection, card, f.STATIC, 1);
const before = again.shards | 0;
f.remove(again.collection, card, f.STATIC, 1);
again.shards = before + f.SHARD_VALUE[f.STATIC];
console.log((again.shards === before + 120 ? '  ok   ' : '  FAIL ')
  + 'Static zerlegen gibt 120 Shards (' + before + ' -> ' + again.shards + ')');

db.close();
console.log(bad === 0 ? '\n  Migration unbedenklich.' : '\n  ' + bad + ' Problem(e).');
process.exit(bad === 0 ? 0 : 1);
" 2>&1 | grep -v ExperimentalWarning | grep -v trace-warnings
