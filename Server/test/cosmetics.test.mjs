// Regeltest des Kosmetik-Ladens. Läuft ohne Server: node test/cosmetics.test.mjs
import * as c from '../cosmetics.js';
import * as f from '../finishes.js';

let failed = 0;
const check = (condition, label) => {
  console.log((condition ? '  ok   ' : '  FAIL ') + label);
  if (!condition) failed++;
};
const fresh = () => ({ coins: 12000, cosmetics: [], equipped: {} });

console.log('Katalog');
check(c.ITEMS.length === 20, `20 Gegenstände (${c.ITEMS.length})`);
check(c.SLOTS.length === 6, `sechs Fächer (${c.SLOTS.length})`);
for (const slot of c.SLOTS) {
  const items = c.ITEMS.filter(i => i.slot === slot);
  check(items.length >= 3, `${slot}: ${items.length} Gegenstände`);
}
// Es gibt nur noch eine Währung — nichts darf mehr etwas anderes verlangen
const nonCoin = c.ITEMS.filter(i => i.currency !== null && i.currency !== 'coins');
check(nonCoin.length === 0, `alles kostet Coins (${nonCoin.map(i => i.currency).join()})`);
check(c.ITEMS.filter(i => i.currency === null).length === 1, 'genau einer ist unverkäuflich');
check(c.ITEMS.every(i => i.currency === null || i.price > 0), 'jeder Verkaufspreis ist positiv');

// Seltenheit muss sich im Preis niederschlagen, sonst ist die Staffelung nur Behauptung
const cheapest = r => Math.min(...c.ITEMS.filter(i => i.rarity === r && i.price).map(i => i.price));
check(cheapest('relic') > cheapest('epic') && cheapest('epic') > cheapest('common'),
  `Preise steigen mit der Seltenheit (common ${cheapest('common')}, epic ${cheapest('epic')}, relic ${cheapest('relic')})`);

console.log('Kaufen');
let acc = fresh();
check(c.buy(acc, 'ashen_weave') === null, 'Kauf geht durch');
check(acc.coins === 11400, `Coins abgezogen (${acc.coins})`);
check(c.owns(acc, 'ashen_weave'), 'Gegenstand gehört danach dem Account');
check(c.buy(acc, 'ashen_weave') === 'You already own this.', 'kein Doppelkauf');

acc = fresh();
check(c.buy(acc, 'vault_ring') === null, 'teuerstes Stück ist bezahlbar');
check(acc.coins === 7000, `Coins abgezogen (${acc.coins})`);

acc = fresh(); acc.coins = 100;
check(c.buy(acc, 'tomb_gilt')?.startsWith('Not enough coins'), 'zu wenig Coins wird abgelehnt');
check(c.buy(acc, 'eclipse')?.startsWith('Not enough coins'), 'auch das teure Stück verlangt Coins');
check(acc.shards === undefined, 'es entsteht kein Shard-Feld mehr');

acc = fresh();
const bane = c.buy(acc, 'wardens_bane');
check(bane !== null && bane.includes('Not for sale'), `Warden's Bane ist nicht käuflich: "${bane}"`);
check(c.buy(acc, 'gibt_es_nicht') === 'Unknown item.', 'unbekannter Gegenstand wird abgelehnt');

console.log('Ausrüsten');
acc = fresh();
c.buy(acc, 'ashen_weave');
check(c.equip(acc, 'sleeve', 'ashen_weave') === null, 'besessenen Gegenstand ausrüsten');
check(acc.equipped.sleeve === 'ashen_weave', 'Fach ist belegt');
check(c.equip(acc, 'avatarFrame', 'ashen_weave') === 'That item does not fit this slot.', 'falsches Fach wird abgelehnt');
check(c.equip(acc, 'sleeve', 'tomb_gilt') === 'You do not own this.', 'nicht besessen wird abgelehnt');
check(c.equip(acc, 'quatsch', 'ashen_weave') === 'Unknown slot.', 'unbekanntes Fach wird abgelehnt');
check(c.equip(acc, 'sleeve', '') === null && acc.equipped.sleeve === undefined, 'leere id räumt das Fach');

acc = fresh();
check(c.owns(acc, c.STARTER_TITLE), 'Startertitel gehört jedem');
check(c.equip(acc, 'title', c.STARTER_TITLE) === null, 'Startertitel lässt sich ausrüsten');

console.log('Wire-Format');
acc = fresh(); c.buy(acc, 'ashen_weave'); c.equip(acc, 'sleeve', 'ashen_weave');
const state = c.stateOf(acc);
check(state.cosmeticsOwned.includes('ashen_weave') && state.cosmeticsOwned.includes(c.STARTER_TITLE),
  'Besitzliste enthält Kauf und Startertitel');
check(state.equippedIds[c.SLOTS.indexOf('sleeve')] === 'ashen_weave', 'Ausrüstung steht am richtigen Index');
check(state.shards === undefined, 'das Wire-Format kennt keine Shards mehr');
const cat = c.catalog();
check(cat.shopIds.length === 20 && cat.shopPrices.length === 20, 'Katalog kommt als parallele Listen');
check(cat.shopPrices[cat.shopIds.indexOf('wardens_bane')] === -1, 'unverkäuflich wird als Preis -1 gesendet');
check(cat.shopCurrencies.every(x => x === 'coins' || x === ''), 'der Katalog nennt nur Coins');

console.log('Coins aus Zerlegen');
check(f.SHARD_VALUE === undefined, 'die alte Shard-Tabelle ist weg');
check(f.COIN_VALUE[f.PLAIN] === 0, 'schlichte Exemplare geben keine Coins (die geben Staub)');
check(f.COIN_VALUE[f.GLOSSY] === 80 && f.COIN_VALUE[f.RAINBOW] === 400 && f.COIN_VALUE[f.STATIC] === 1200,
  'Zerlegewerte 80 / 400 / 1200');
check(f.COIN_VALUE[f.STATIC] > f.COIN_VALUE[f.RAINBOW] && f.COIN_VALUE[f.RAINBOW] > f.COIN_VALUE[f.GLOSSY],
  'seltener zerlegt sich wertvoller');

// Ein Static ist 1/240 — der Gegenwert soll spürbar, aber kein Freifahrtschein sein
const ringInStatics = 5000 / f.COIN_VALUE[f.STATIC];
check(ringInStatics > 3 && ringInStatics < 6,
  `Vault Ring (5000) entspricht ${ringInStatics.toFixed(1)} zerlegten Static`);

console.log(failed === 0 ? '\nAlle Prüfungen bestanden.' : `\n${failed} Prüfung(en) fehlgeschlagen.`);
process.exit(failed === 0 ? 0 : 1);
