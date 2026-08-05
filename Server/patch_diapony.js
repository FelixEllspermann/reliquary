// Einmaliger Admin-Patch: DiaPony bekommt Max-Ressourcen und die volle Sammlung (3x je Karte).
import fs from 'fs';
const cards = JSON.parse(fs.readFileSync('/opt/rouge-tcg/data/cards.json', 'utf8'));
const accounts = JSON.parse(fs.readFileSync('/opt/rouge-tcg/data/accounts.json', 'utf8'));
const acc = accounts['diapony'];
if (!acc) {
  console.log('FEHLER: Account diapony nicht gefunden. Vorhanden:', Object.keys(accounts).join(', '));
  process.exit(1);
}
acc.coins = 999999;
acc.tokens = [999999, 999999, 999999, 999999];
acc.collection = {};
for (const name of Object.keys(cards)) acc.collection[name] = 3;
fs.writeFileSync('/opt/rouge-tcg/data/accounts.json', JSON.stringify(accounts, null, 1));
console.log(`DiaPony gepatcht: ${Object.keys(acc.collection).length} Karten x3, ${acc.coins} Coins, Tokens ${acc.tokens.join('/')}`);
