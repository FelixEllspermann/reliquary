// Regeltest der Rangleiter. Läuft ohne Server: node test/ranks.test.mjs
import * as r from '../ranks.js';

let failed = 0;
const check = (condition, label) => {
  console.log((condition ? '  ok   ' : '  FAIL ') + label);
  if (!condition) failed++;
};
const season = r.currentSeason();

console.log('Bänder');
check(r.rankFor(0).rank === 1 && r.rankFor(0).tier === 1, 'RP 0 → Ash Seal I');
check(r.rankFor(399).rank === 1 && r.rankFor(399).tier === 5, 'RP 399 → Ash Seal V');
check(r.rankFor(400).rank === 2 && r.rankFor(400).tier === 1, 'RP 400 → Clay Seal I');
check(r.rankFor(2100).name === 'Gold Seal', 'RP 2100 → Gold Seal');
check(r.rankFor(4500).rank === 10 && r.rankFor(4500).tier === 1, 'RP 4500 → Vault Seal I');
check(r.rankFor(999999).tier === 5, 'sehr hohe RP → Vault Seal V, keine Obergrenze');

console.log('Wertung');
check(r.rpDelta(1, true) === 22 && r.rpDelta(1, false) === -17, 'unter Gold +22 / −17');
check(r.rpDelta(6, true) === 25 && r.rpDelta(6, false) === -25, 'ab Gold +25 / −25');

console.log('Böden');
let acc = { rp: 400, peakRank: 2, season };
for (let i = 0; i < 20; i++) r.applyResult(acc, false);
check(acc.rp === 400 && r.rankFor(acc.rp).rank === 2, `20 Niederlagen halten Clay Seal I (RP ${acc.rp})`);

acc = { rp: 0, peakRank: 1, season };
for (let i = 0; i < 10; i++) r.applyResult(acc, false);
check(acc.rp === 0, 'Ash Seal I verliert nichts');

// Ash Seal ist 400 RP breit, eine Unterstufe also 80 — vier Niederlagen à 17
// reichen gerade über die Grenze bei 240.
acc = { rp: 300, peakRank: 1, season };
const startTier = r.rankFor(acc.rp).tier;
for (let i = 0; i < 4; i++) r.applyResult(acc, false);
check(r.rankFor(acc.rp).tier < startTier,
  `Unterstufe fällt innerhalb des Rangs (${startTier} → ${r.rankFor(acc.rp).tier}, RP ${acc.rp})`);
check(r.rankFor(acc.rp).rank === 1, 'der Hauptrang bleibt dabei stehen');

console.log('Kurve');
acc = { rp: 0, peakRank: 1, season };
for (let i = 0; i < 100; i++) r.applyResult(acc, i % 2 === 0);
check(acc.rp > 0, `50 % Siegquote unter Gold steigt (RP ${acc.rp})`);

acc = { rp: 2100, peakRank: 6, season };
for (let i = 0; i < 100; i++) r.applyResult(acc, i % 2 === 0);
check(acc.rp === 2100, `50 % Siegquote ab Gold bleibt stehen (RP ${acc.rp})`);

console.log('Aufstieg');
acc = { rp: 396, peakRank: 1, season };
const step = r.applyResult(acc, true);
check(step.rankUp && step.after.name === 'Clay Seal', 'Schwelle befördert sofort, ohne Serie');
check(acc.peakRank === 2, 'Bestenschutz wandert mit');

console.log('Saison');
acc = { rp: 2150, peakRank: 6, season: '2026-01' };
const roll = r.rolloverIfNeeded(acc, new Date(Date.UTC(2026, 7, 5)));
check(roll !== null && roll.to.rank === 4, `Saisonende: zwei Ränge zurück, Gold Seal → ${roll?.to.name} ${roll?.to.tier}`);
check(acc.peakRank === 4, 'Bestenschutz wird auf den neuen Rang gesetzt');
check(acc.season === '2026-08', 'Saison ist jetzt 2026-08');

// Die Unterstufe bleibt beim Abstieg erhalten
acc = { rp: 3980, peakRank: 9, season: '2026-01' };   // Relic Seal II
const tierBefore = r.rankFor(acc.rp).tier;
r.rolloverIfNeeded(acc, new Date(Date.UTC(2026, 7, 5)));
check(r.rankFor(acc.rp).rank === 7 && r.rankFor(acc.rp).tier === tierBefore,
  `Relic Seal ${tierBefore} → ${r.rankFor(acc.rp).name} ${r.rankFor(acc.rp).tier}, Unterstufe bleibt`);

acc = { rp: 500, peakRank: 2, season: '2026-01' };    // Clay Seal, nur ein Rang über dem Boden
r.rolloverIfNeeded(acc, new Date(Date.UTC(2026, 7, 5)));
check(r.rankFor(acc.rp).rank === 1, 'zwei Ränge unter Clay Seal wäre negativ — landet auf Ash Seal');

acc = { rp: 0, peakRank: 1, season: '2026-01' };
r.rolloverIfNeeded(acc, new Date(Date.UTC(2026, 7, 5)));
check(acc.rp === 0 && acc.peakRank === 1, 'Ash Seal fällt am Saisonende nicht tiefer');

const fresh = {};
r.rolloverIfNeeded(fresh, new Date(Date.UTC(2026, 7, 5)));
check(fresh.rp === 0 && fresh.season === '2026-08', 'neuer Account startet bei 0');

console.log(failed === 0 ? '\nAlle Prüfungen bestanden.' : `\n${failed} Prüfung(en) fehlgeschlagen.`);
process.exit(failed === 0 ? 0 : 1);
