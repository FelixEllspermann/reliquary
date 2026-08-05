#!/bin/bash
# Zeigt den Exemplar-Bestand des Staging-Testaccounts.
cd /tmp/rouge-stage7778 || { echo "Staging fehlt"; exit 1; }
node --input-type=module -e "
import { openDatabase } from './db.js';
import * as f from './finishes.js';
const db = openDatabase('/tmp/rouge-stage7778/data', () => {});
const acc = db.loadAll()['finishtest'];
if (!acc) { console.log('Account fehlt'); process.exit(1); }
let p = 0, g = 0, r = 0, s = 0;
const mixed = [];
for (const [name, entry] of Object.entries(acc.collection)) {
  const x = f.normalise(entry);
  p += x[0]; g += x[1]; r += x[2]; s += x[3];
  if (x[1] + x[2] + x[3] > 0) mixed.push(name.padEnd(28) + '[' + x.join(', ') + ']');
}
console.log('FinishTest: ' + p + ' plain, ' + g + ' glossy, ' + r + ' rainbow, ' + s + ' static');
console.log('Karten mit besonderen Exemplaren (plain, glossy, rainbow, static):');
mixed.slice(0, 10).forEach(m => console.log('  ' + m));
db.close();
" 2>&1 | grep -v ExperimentalWarning | grep -v trace-warnings
