/*
 * Karten-Finishes. Ein Finish gehört dem EXEMPLAR, nicht der Karte: dieselbe
 * Karte kann gleichzeitig schlicht, glänzend und regenbogen im Tresor liegen,
 * und jedes Exemplar zählt einzeln.
 *
 * Deshalb ist die Sammlung serverseitig `collection[kartenname] = [n0,n1,n2,n3]`
 * statt einer einzelnen Zahl — der Kartenname bleibt der Schlüssel, damit
 * Banlist, Kopienlimit und „einzigartige Karten" unverändert funktionieren.
 */

export const PLAIN = 0;
export const GLOSSY = 1;
export const RAINBOW = 2;
export const STATIC = 3;
export const COUNT = 4;

export const NAMES = ['Plain', 'Glossy', 'Rainbow', 'Static'];

/** Ziehungsraten laut Handoff. Der Rest bleibt schlicht. */
const RATES = [
  { finish: STATIC,  chance: 1 / 240 },
  { finish: RAINBOW, chance: 1 / 60 },
  { finish: GLOSSY,  chance: 1 / 12 },
];

/**
 * Zerlegewert eines Sonderexemplars in Coins. Schlichte Karten geben stattdessen
 * Staub wie eh und je — sie sind Baumaterial, Sonderexemplare sind Geld.
 */
export const COIN_VALUE = [0, 80, 400, 1200];

/*
 * Ein Finish lässt sich nicht gezielt herstellen. Auch beim Fertigen wird
 * gewürfelt — mit denselben Raten wie beim Pack. Sonst wäre jedes Glossy
 * einfach eine Frage von genug Staub, und die Seltenheit wäre keine.
 */

/** Würfelt das Finish für eine frisch gezogene Karte. */
export function roll(random = Math.random) {
  const r = random();
  let threshold = 0;
  for (const entry of RATES) {
    threshold += entry.chance;
    if (r < threshold) return entry.finish;
  }
  return PLAIN;
}

/** Leerer Eintrag — vier Fächer, eins je Finish. */
export function emptyEntry() {
  return [0, 0, 0, 0];
}

/**
 * Macht aus einem Sammlungseintrag verlässlich ein Vier-Felder-Array.
 * Alte Datenbestände hielten dort eine blanke Zahl; die wandert nach Plain.
 */
export function normalise(entry) {
  if (Array.isArray(entry)) {
    const out = emptyEntry();
    for (let i = 0; i < COUNT; i++) out[i] = Math.max(0, entry[i] | 0);
    return out;
  }
  const out = emptyEntry();
  out[PLAIN] = Math.max(0, entry | 0);
  return out;
}

/** Wie viele Exemplare dieser Karte insgesamt? */
export function total(entry) {
  const e = normalise(entry);
  return e[0] + e[1] + e[2] + e[3];
}

/** Legt ein Exemplar in die Sammlung. */
export function add(collection, cardName, finish = PLAIN, amount = 1) {
  const entry = normalise(collection[cardName]);
  entry[Math.min(Math.max(finish | 0, 0), COUNT - 1)] += amount;
  collection[cardName] = entry;
  return entry;
}

/** Nimmt ein Exemplar heraus. Gibt false zurück, wenn es keines gibt. */
export function remove(collection, cardName, finish = PLAIN, amount = 1) {
  const entry = normalise(collection[cardName]);
  const slot = Math.min(Math.max(finish | 0, 0), COUNT - 1);
  if (entry[slot] < amount) return false;
  entry[slot] -= amount;
  if (total(entry) === 0) delete collection[cardName];
  else collection[cardName] = entry;
  return true;
}

/** Besitzt der Account mindestens `amount` Exemplare dieses Finishes? */
export function owns(collection, cardName, finish = PLAIN, amount = 1) {
  return normalise(collection[cardName])[Math.min(Math.max(finish | 0, 0), COUNT - 1)] >= amount;
}
