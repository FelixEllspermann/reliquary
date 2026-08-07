// Rouge TCG — Lobby-, Matchmaking-, Relay- und Account-Server
// v3: Coins-Währung, Pack-Inventar (kaufen/öffnen, 8 Karten), Account-Decks, Crafting.
import http from 'http';
import net from 'net';
import fs from 'fs';
import path from 'path';
import crypto from 'crypto';
import { fileURLToPath } from 'url';
import { WebSocketServer } from 'ws';
import { openDatabase } from './db.js';
import * as ranks from './ranks.js';
import * as finishes from './finishes.js';
import * as cosmetics from './cosmetics.js';

const PORT = Number(process.env.PORT) || 7777; // Testinstanzen laufen auf einem anderen Port
const DIR = path.dirname(fileURLToPath(import.meta.url));
const DATA_DIR = path.join(DIR, 'data');
const ACCOUNTS_FILE = path.join(DATA_DIR, 'accounts.json');

const log = (...a) => console.log(new Date().toISOString(), ...a);

// ---- Ökonomie (hier balancen) ----
const ECON = {
  startCoins: 1500,        // Startguthaben neuer Accounts
  packSize: 5,             // Karten pro Pack (Slot-Anzahl, Fallback ohne slots-Eintrag)
  duelRewardCoins: 100,    // Coins pro gespieltem Duell
  winBonusCoins: 100,      // extra Coins für den Sieger
  soloRewardCoins: 50,     // Coins pro Bot-Duell (Solo)
  soloRewardCooldownMs: 60000, // Mindestabstand zwischen Solo-Belohnungen
  craftCost: [30, 30, 30, 30], // Dust-Kosten fürs Craften: immer 30 Dust der jeweiligen Rarity
  dustGain: [10, 10, 10, 10],  // Dust-Ertrag fürs Entcraften: immer 10 Dust der jeweiligen Rarity
  maxDecks: 20,            // Decks pro Account
  dailyRewardCoins: 150,   // Coins für das tägliche Siegel
  dailyCooldownMs: 20 * 3600 * 1000,     // Mindestabstand zwischen Daily-Claims (20h)
  dailyStreakBreakMs: 48 * 3600 * 1000,  // danach beginnt die Serie neu (48h)
  defaultSlots: [0, 0, 1, 2, 2]   // Fallback-Slot-Raritäten (C C U R R)
};

// ---- Spieldaten (aus Unity exportiert) ----
const cards = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'cards.json'), 'utf8'));        // Name -> Rarity 0-3
const packs = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'packs.json'), 'utf8'));        // Name -> {price, cards[]}
const starter = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'starter.json'), 'utf8'));    // Name -> Anzahl (Start-Sammlung)
const starterDeck = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'starterdeck.json'), 'utf8')); // {name, hero, cards[]}
// Die fuenf Decks zur Auswahl beim ersten Start. Erzeugt und geprueft von
// data/build-starterdecks.py — nie von Hand editieren, die Pruefung faellt sonst weg.
const starterDecks = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'starterdecks.json'), 'utf8'));
const rules = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'rules.json'), 'utf8'));        // {maxCopiesPerCard, deckMinSize, deckMaxSize}
// Spieler-Feedback liegt außerhalb von DATA_DIR in einem Verzeichnis, das sich Spiel-Server
// und Website (Gruppe 'feedback') teilen — so kommt die Website an die Meldungen, aber
// nicht an accounts.json.
const FEEDBACK_FILE = process.env.ROUGE_FEEDBACK_FILE || '/srv/reliquary-feedback/feedback.jsonl';
let reliquaries = [];                                                                        // Reliquary-Namen (Extra Deck)
try { reliquaries = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'reliquary.json'), 'utf8')); } catch { reliquaries = []; }
const reliquarySet = new Set(reliquaries);
const EXTRA_MAX = rules.extraDeckMaxSize || 20;

// Banlist: Kartenname -> erlaubte Kopien (0 = gebannt, 1 = limitiert, 2 = semi-limitiert).
// Nicht gelistete Karten dürfen maxCopiesPerCard mal ins Deck.
let banlist = {};
try { banlist = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'banlist.json'), 'utf8')); } catch { banlist = {}; }

// Chronik der Banlist — reine Anzeige, wird nie durchgesetzt.
let banlistHistory = [];
try {
  const raw = JSON.parse(fs.readFileSync(path.join(DATA_DIR, 'banlist-history.json'), 'utf8'));
  banlistHistory = Array.isArray(raw) ? raw : (Array.isArray(raw.entries) ? raw.entries : []);
} catch { banlistHistory = []; }

/** Eine Änderung als "neu|alt|Kartenname" — der Client färbt anhand des neuen Limits. */
function historyLine(change) {
  const to = typeof change.to === 'number' ? change.to : rules.maxCopiesPerCard;
  const from = typeof change.from === 'number' ? change.from : -1;
  return `${to}|${from}|${change.card || ''}`;
}

/** Wie oft diese Karte insgesamt ins Deck darf. */
function allowedCopies(cardName) {
  const limit = banlist[cardName];
  return typeof limit === 'number' ? Math.max(0, Math.min(limit, rules.maxCopiesPerCard)) : rules.maxCopiesPerCard;
}

function limitWord(limit) {
  if (limit <= 0) return 'forbidden';
  if (limit === 1) return 'limited to 1 copy';
  return `limited to ${limit} copies`;
}

// ---- Accounts ----
// Gehalten werden sie weiterhin im RAM; geschrieben wird aber nur noch der eine
// geänderte Account (siehe db.js), nicht mehr die komplette Datei.
const db = openDatabase(DATA_DIR, log);
db.importLegacyJson(ACCOUNTS_FILE);
let accounts = db.loadAll();
for (const [key, acc] of Object.entries(accounts))
  if (migrate(acc)) db.save(key, acc); // Angleichungen sofort persistieren

/** Bringt ältere Accounts aufs aktuelle Format. Liefert true, wenn etwas geändert wurde. */
function migrate(acc) {
  let changed = false;
  if (acc.coins === undefined) { acc.coins = ECON.startCoins; changed = true; }
  if (!acc.packInv) { acc.packInv = {}; changed = true; }
  // Wer noch nie gewaehlt hat, bekommt die Auswahl beim naechsten Start —
  // auch Konten, die es lange vor dieser Funktion schon gab.
  if (acc.starterPick === undefined) { acc.starterPick = null; changed = true; }
  // Das alte Fix-Deck nur noch fuer Konten, die die Auswahl hinter sich haben.
  // Sonst bekaeme ein neuer Spieler ein Deck, das er nie ausgesucht hat, und die
  // Auswahl waere eine Zierde.
  if (acc.starterPick && (!acc.decks || acc.decks.length === 0)) {
    acc.decks = [structuredClone(starterDeck)]; changed = true;
  }
  // Reparatur: der Starter-Grant vergass anfangs die Spielerkarte. Wer sein
  // Startdeck schon gewaehlt hat, aber dessen Hero nicht besitzt, sass in der
  // Solo- und Deck-Pruefung fest ("cards you do not own"). Einmal nachreichen.
  if (acc.starterPick) {
    const picked = starterDecks.find(d => d.id === acc.starterPick);
    if (picked && cards[picked.hero] !== undefined
        && finishes.total(acc.collection[picked.hero]) < 1) {
      finishes.add(acc.collection, picked.hero, finishes.PLAIN);
      changed = true;
    }
  }
  if (!acc.decks) { acc.decks = []; changed = true; }
  if (!acc.daily) { acc.daily = { streak: 0, lastClaim: 0 }; changed = true; }
  if ('packs' in acc) { delete acc.packs; changed = true; } // v2-Feld
  // Alte Pack-Namen ins eine "Relic Pack" überführen
  for (const oldName of ['Flames & Frost', 'Tomb of Ash', 'Relic Cache', 'Sealed Vault']) {
    if (acc.packInv[oldName] === undefined) continue;
    if (acc.packInv[oldName]) {
      acc.packInv['Relic Pack'] = (acc.packInv['Relic Pack'] || 0) + acc.packInv[oldName];
    }
    delete acc.packInv[oldName];
    changed = true;
  }
  return changed;
}

/**
 * Merkt einen Account zum Speichern vor. Der Schlüssel ist immer der
 * kleingeschriebene Name — genau der, unter dem er auch in `accounts` liegt.
 */
function saveAccount(acc) {
  if (acc) db.save(acc.name.toLowerCase(), acc);
}

function hashPass(pass, salt) {
  return crypto.scryptSync(String(pass), salt, 32).toString('hex');
}

// ---- Steam-Anmeldung ----
// Scharfgeschaltet über Umgebungsvariablen (siehe STEAM-SETUP.md):
//   STEAM_APP_ID        die App-ID aus Steamworks
//   STEAM_WEB_API_KEY   Publisher-Web-API-Key (NICHT der persönliche Key)
//   STEAM_DEV_MODE=1    NUR lokal: akzeptiert "dev:<steamid>" ohne Valve-Prüfung
const STEAM_APP_ID = process.env.STEAM_APP_ID || '';
const STEAM_WEB_API_KEY = process.env.STEAM_WEB_API_KEY || '';
const STEAM_DEV_MODE = process.env.STEAM_DEV_MODE === '1';
const steamConfigured = !!(STEAM_APP_ID && STEAM_WEB_API_KEY);

/**
 * Prüft ein Steam-Auth-Ticket bei Valve und liefert { steamId } oder { error }.
 * Der Client kann hier NICHT lügen: die SteamID kommt ausschliesslich aus
 * Valves Antwort, nie aus der Nachricht des Clients.
 */
async function verifySteamTicket(ticket) {
  if (typeof ticket !== 'string' || ticket.length === 0) return { error: 'No Steam ticket.' };

  if (STEAM_DEV_MODE && ticket.startsWith('dev:')) {
    const steamId = ticket.slice(4).trim();
    if (!/^\d{5,20}$/.test(steamId)) return { error: 'Invalid dev ticket.' };
    log(`!!! STEAM_DEV_MODE: Ticket UNGEPRÜFT akzeptiert (SteamID ${steamId}) — niemals produktiv nutzen!`);
    return { steamId };
  }

  if (!steamConfigured) return { error: 'Steam sign-in is not configured on this server yet.' };
  if (!/^[0-9a-fA-F]+$/.test(ticket) || ticket.length > 4096) return { error: 'Malformed Steam ticket.' };

  const url = 'https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1/'
    + `?key=${encodeURIComponent(STEAM_WEB_API_KEY)}`
    + `&appid=${encodeURIComponent(STEAM_APP_ID)}`
    + `&ticket=${ticket}`;
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(8000) });
    if (!response.ok) return { error: `Steam returned HTTP ${response.status}.` };
    const body = await response.json();
    const params = body?.response?.params;
    if (!params || params.result !== 'OK')
      return { error: 'Steam rejected the ticket: ' + (body?.response?.error?.errordesc || 'unknown reason') };
    if (params.publisherbanned) return { error: 'This Steam account is banned from the game.' };
    return { steamId: String(params.steamid) };
  } catch (error) {
    return { error: 'Could not reach Steam: ' + error.message };
  }
}

/** Findet den Account zu einer SteamID (die Verknüpfung ist eindeutig). */
function accountBySteamId(steamId) {
  for (const acc of Object.values(accounts)) if (acc.steamId === steamId) return acc;
  return null;
}

/**
 * Steam-Anmeldung. Läuft asynchron neben dem Nachrichten-Handler; währenddessen
 * sperrt c.steamPending weitere Versuche, damit ein Doppelklick nicht zwei
 * Accounts anlegt.
 */
async function handleSteamAuth(c, m) {
  if (c.account) { sendError(c, 'Already signed in.'); return; }
  if (c.steamPending) return;
  c.steamPending = true;
  try {
    const check = await verifySteamTicket(m.steamTicket);
    if (c.ws.readyState !== 1) return;
    if (check.error) { sendError(c, check.error); return; }

    let account = accountBySteamId(check.steamId);
    if (!account) {
      const name = freeAccountName(m.steamName);
      account = newAccount(name, null, check.steamId);
      accounts[name.toLowerCase()] = account;
      saveAccount(account);
      log(`Neuer Steam-Account: ${name} (SteamID ${check.steamId})`);
    } else {
      log(`Steam-Login: ${account.name}`);
    }
    c.account = account.name.toLowerCase();
    c.name = account.name;
    send(c, { t: 'auth_ok', profile: profileOf(account) });
  } finally {
    c.steamPending = false;
  }
}

/*
 * Steam-Accounts und Passwort-Accounts sind bewusst zwei getrennte Welten: eine
 * SteamID lässt sich nicht nachträglich an einen Formular-Account hängen, und ein
 * Steam-Account bekommt kein Passwort. Sonst gäbe es zwei Wege in denselben
 * Besitz — und damit doppelt so viele Wege, ihn zu verlieren.
 */

/** Freier Account-Schlüssel auf Basis eines Wunschnamens (Steam-Personas sind nicht eindeutig). */
function freeAccountName(wish) {
  let base = String(wish || '').trim().replace(/[^\w \-.]/g, '').slice(0, 16).trim();
  if (base.length < 3) base = 'Duelist';
  if (!accounts[base.toLowerCase()]) return base;
  for (let i = 2; i < 10000; i++) {
    const candidate = `${base}${i}`;
    if (!accounts[candidate.toLowerCase()]) return candidate;
  }
  return base + Date.now();
}

/**
 * Neuer Account. Ohne Passwort (Steam-Anmeldung) bleibt hash leer — ein Login
 * über das Formular ist dann unmöglich, bis der Spieler eines setzt.
 */
function newAccount(name, pass, steamId = null) {
  const salt = crypto.randomBytes(16).toString('hex');
  return {
    name,
    salt,
    hash: pass ? hashPass(pass, salt) : '',
    steamId,
    coins: ECON.startCoins,
    tokens: [0, 0, 0, 0],
    // Leer. Die Karten kommen aus dem Startdeck, das der Spieler gleich waehlt —
    // ein fixes Startpaket obendrauf wuerde die Wahl entwerten.
    collection: {},
    packInv: {},
    decks: [],
    starterPick: null,
    daily: { streak: 0, lastClaim: 0 },
    // Jeder fängt ganz unten an
    rp: 0,
    peakRank: 1,
    season: ranks.currentSeason(),
    wins: 0, losses: 0, streak: 0, bestStreak: 0, careerRp: 0,
    titles: [cosmetics.STARTER_TITLE],
    // Kosmetik: Besitz und ausgerüstete Fächer (bezahlt wird mit Coins)
    cosmetics: [],
    equipped: { title: cosmetics.STARTER_TITLE }
  };
}

function profileOf(acc) {
  // Saisonwechsel wird beim ersten Blick auf den Account nachgeholt
  ranks.rolloverIfNeeded(acc);
  const seal = ranks.rankInfo(acc);
  const names = Object.keys(acc.collection);
  const packNames = Object.keys(acc.packInv).filter(p => acc.packInv[p] > 0);
  const gap = Date.now() - (acc.daily?.lastClaim || 0);
  return {
    account: acc.name,
    coins: acc.coins,
    tokensCommon: acc.tokens[0],
    tokensUncommon: acc.tokens[1],
    tokensRare: acc.tokens[2],
    tokensLegendary: acc.tokens[3],
    // collectionCounts bleibt die Gesamtzahl (nichts Bestehendes bricht);
    // die vier Fächer kommen zusätzlich, eins je Finish.
    collectionCards: names,
    collectionCounts: names.map(n => finishes.total(acc.collection[n])),
    collectionPlain: names.map(n => finishes.normalise(acc.collection[n])[finishes.PLAIN]),
    collectionGlossy: names.map(n => finishes.normalise(acc.collection[n])[finishes.GLOSSY]),
    collectionRainbow: names.map(n => finishes.normalise(acc.collection[n])[finishes.RAINBOW]),
    collectionStatic: names.map(n => finishes.normalise(acc.collection[n])[finishes.STATIC]),
    packNames,
    packCounts: packNames.map(p => acc.packInv[p]),
    decks: acc.decks.map(d => ({
      name: d.name, hero: d.hero, cards: d.cards, extra: d.extra || [],
      cardFinishes: d.cardFinishes || new Array(d.cards.length).fill(0),
      extraFinishes: d.extraFinishes || new Array((d.extra || []).length).fill(0)
    })),
    dailyStreak: acc.daily?.streak || 0,
    dailyClaimable: gap >= ECON.dailyCooldownMs,
    dailyNextInMs: gap >= ECON.dailyCooldownMs ? 0 : ECON.dailyCooldownMs - gap,
    dailyRewardCoins: ECON.dailyRewardCoins,
    steamLinked: !!acc.steamId,
    rankValue: seal.rank,
    rankTier: seal.tier,
    rankName: seal.name,
    rankRp: seal.rp,
    rankTierFloor: seal.tierFloor,
    rankNextAt: seal.nextAt ?? -1,
    rankSeason: seal.season,
    rankWins: seal.wins,
    rankLosses: seal.losses,
    rankBestStreak: seal.bestStreak,
    titles: acc.titles && acc.titles.length ? acc.titles : [cosmetics.STARTER_TITLE],
    towerFloor: acc.towerFloor | 0,
    ...cosmetics.stateOf(acc),
    ...cosmetics.catalog(),
    banlistNames: Object.keys(banlist),
    banlistLimits: Object.keys(banlist).map(n => allowedCopies(n)),
    banlistMaxCopies: rules.maxCopiesPerCard,
    historyDates: banlistHistory.map(e => e.date || ''),
    historyTitles: banlistHistory.map(e => e.title || ''),
    historyNotes: banlistHistory.map(e => e.note || ''),
    historyChanges: banlistHistory.map(e =>
      (Array.isArray(e.changes) ? e.changes : []).map(historyLine).join('\n')),
    online: clients.size,

    // Solange nicht gewaehlt wurde, reist der ganze Katalog mit. Das sind fuenf
    // Decks a 40 Karten — einmal beim Login, danach nie wieder.
    starterPending: !acc.starterPick,
    starters: acc.starterPick ? [] : starterDecks.map(d => ({
      id: d.id, name: d.name, archetypes: d.archetypes, blurb: d.blurb,
      description: d.description, hero: d.hero, cards: d.cards, extra: d.extra
    }))
  };
}

/**
 * Vergibt ein Startdeck: Karten in die Sammlung, Deck in die Deckliste, Haken dran.
 *
 * Genau EINMAL pro Konto. Der Haken wird VOR dem Speichern gesetzt, damit zwei
 * schnell hintereinander eintreffende Anfragen nicht beide durchlaufen — sonst
 * saehe ein Doppelklick wie zwei Geschenke aus.
 */
function grantStarterDeck(acc, id) {
  if (acc.starterPick) return 'You have already chosen a starter deck.';
  const deck = starterDecks.find(d => d.id === id);
  if (!deck) return 'Unknown starter deck.';

  acc.starterPick = deck.id;
  // deck.hero gehoert dazu: die Solo- und Deck-Pruefung im Client verlangt
  // auch die Spielerkarte in der Sammlung. Ohne sie sass ein frischer Account
  // mit seinem eben gewaehlten Deck fest — "cards you do not own".
  for (const name of [...deck.cards, ...deck.extra, deck.hero]) {
    if (cards[name] === undefined) continue;   // Karte aus dem Spiel genommen
    finishes.add(acc.collection, name, finishes.PLAIN);
  }
  acc.decks.push({
    name: deck.name, hero: deck.hero,
    cards: [...deck.cards], extra: [...deck.extra],
    cardFinishes: new Array(deck.cards.length).fill(0),
    extraFinishes: new Array(deck.extra.length).fill(0)
  });
  saveAccount(acc);
  log(`${acc.name} waehlt das Startdeck "${deck.name}" (${deck.cards.length}+${deck.extra.length} Karten).`);
  return null;
}

/** Karten eines Unique-Packs, die dem Konto noch fehlen. */
function uniquePool(packDef, acc) {
  return (packDef.cards || []).filter(name =>
    cards[name] !== undefined && finishes.total(acc.collection[name]) < 1);
}

/** Slot-basierte Pack-Ziehung: pro Slot eine feste Rarity; fehlt sie im Pool,
 *  Fallback erst auf niedrigere, dann auf höhere Raritäten.
 *  Der letzte Slot wird mit legendaryChance zur Legendary aufgewertet — ohne das
 *  wären Legendaries aus Packs überhaupt nicht ziehbar. */
function drawFromPack(packDef) {
  const byRarity = [[], [], [], []];
  for (const name of packDef.cards) {
    const rarity = cards[name];
    if (rarity !== undefined) byRarity[rarity].push(name);
  }
  const base = Array.isArray(packDef.slots) && packDef.slots.length > 0 ? packDef.slots : ECON.defaultSlots;
  const slots = base.slice();

  const chance = typeof packDef.legendaryChance === 'number' ? packDef.legendaryChance : 0;
  const last = slots.length - 1;
  if (chance > 0 && last >= 0 && slots[last] !== 3 && byRarity[3].length > 0 && Math.random() < chance) {
    slots[last] = 3;
  }

  const result = [];
  for (const want of slots) {
    let pool = byRarity[want] || [];
    for (let r = want - 1; r >= 0 && pool.length === 0; r--) pool = byRarity[r];
    for (let r = want + 1; r <= 3 && pool.length === 0; r++) pool = byRarity[r];
    if (pool.length === 0) pool = packDef.cards;
    result.push(pool[Math.floor(Math.random() * pool.length)]);
  }
  return result;
}

/** Deck-Validierung beim Speichern: bekannte Karten, Besitz, Kopien-Limit, Maximalgröße, Extra Deck. */
/**
 * Prüft ein Deck gegen Regeln, Banlist und Besitz.
 *
 * Zwei Ebenen, die man nicht verwechseln darf: das Kopienlimit zählt je KARTE
 * (drei Exemplare heissen drei, egal welches Finish), der Besitz dagegen je
 * EXEMPLAR — wer zwei Static einbaut, muss auch zwei Static besitzen.
 */
function validateDeck(acc, deck) {
  if (!deck || typeof deck.name !== 'string' || !Array.isArray(deck.cards)) return 'Invalid deck data.';
  if (deck.name.trim().length < 1 || deck.name.length > 30) return 'Invalid deck name.';
  if (deck.cards.length > rules.deckMaxSize) return `Maximum ${rules.deckMaxSize} cards.`;

  const finishOf = (list, index) => Array.isArray(list) ? Math.min(Math.max(list[index] | 0, 0), finishes.COUNT - 1) : finishes.PLAIN;
  const counts = {};      // je Karte — für Banlist und Kopienlimit
  const used = {};        // je Karte+Finish — für den Besitz

  for (let i = 0; i < deck.cards.length; i++) {
    const cardName = deck.cards[i];
    if (cards[cardName] === undefined) return `Unknown card: ${cardName}`;
    if (reliquarySet.has(cardName)) return `${cardName} is a Reliquary — it belongs in the Extra Deck.`;
    counts[cardName] = (counts[cardName] || 0) + 1;
    const allowed = allowedCopies(cardName);
    if (counts[cardName] > allowed)
      return allowed < rules.maxCopiesPerCard
        ? `${cardName} is ${limitWord(allowed)} by the banlist.`
        : `Maximum ${rules.maxCopiesPerCard} copies per card.`;

    const finish = finishOf(deck.cardFinishes, i);
    const key = `${cardName}#${finish}`;
    used[key] = (used[key] || 0) + 1;
    if (!finishes.owns(acc.collection, cardName, finish, used[key]))
      return finish === finishes.PLAIN
        ? `Not owned: ${cardName}`
        : `Not owned: ${cardName} (${finishes.NAMES[finish]} ×${used[key]})`;
  }

  const extra = Array.isArray(deck.extra) ? deck.extra : [];
  if (extra.length > EXTRA_MAX) return `Extra Deck: maximum ${EXTRA_MAX} cards.`;
  const extraCounts = {};
  for (let i = 0; i < extra.length; i++) {
    const cardName = extra[i];
    if (!reliquarySet.has(cardName)) return `${cardName} is not a Reliquary card.`;
    extraCounts[cardName] = (extraCounts[cardName] || 0) + 1;
    const allowedExtra = allowedCopies(cardName);
    if (extraCounts[cardName] > allowedExtra)
      return allowedExtra < rules.maxCopiesPerCard
        ? `${cardName} is ${limitWord(allowedExtra)} by the banlist.`
        : `Maximum ${rules.maxCopiesPerCard} copies per Reliquary.`;

    const finish = finishOf(deck.extraFinishes, i);
    const key = `x:${cardName}#${finish}`;
    used[key] = (used[key] || 0) + 1;
    if (!finishes.owns(acc.collection, cardName, finish, used[key]))
      return finish === finishes.PLAIN
        ? `Not owned: ${cardName}`
        : `Not owned: ${cardName} (${finishes.NAMES[finish]} ×${used[key]})`;
  }

  if (deck.hero && cards[deck.hero] === undefined) return 'Unknown player card.';
  if (deck.hero && finishes.total(acc.collection[deck.hero]) < 1) return 'Player card not owned.';
  return null;
}

// ---- Admin-Schnittstelle (Spieler-Editor der Website) ----
//
// Die Website darf die Spieldaten NICHT selbst anfassen: die Konten liegen in
// SQLite, der Server hält sie im Speicher, und zwei Schreiber auf derselben Datei
// enden in verlorenen Änderungen. Also fragt die Website hier an, und dieser
// Prozess — der einzige Besitzer der Daten — führt es aus.
//
// Drei Riegel, alle drei müssen halten:
//   1. Nur über die Loopback-Schnittstelle. Website und Spielserver laufen auf
//      derselben Maschine; von aussen ist der Weg damit gar nicht erst offen.
//   2. Ein gemeinsames Geheimnis aus der Umgebung. Steht es nicht da, ist die
//      Schnittstelle KOMPLETT AUS statt offen — im Zweifel lieber unbenutzbar.
//   3. Vergleich in konstanter Zeit, damit sich das Geheimnis nicht erraten lässt.
const ADMIN_TOKEN = process.env.ADMIN_TOKEN || '';

function isLoopback(req) {
  const address = req.socket.remoteAddress || '';
  return address === '127.0.0.1' || address === '::1' || address === '::ffff:127.0.0.1';
}

function adminAllowed(req) {
  if (!ADMIN_TOKEN || !isLoopback(req)) return false;
  const given = Buffer.from(String(req.headers['x-admin-token'] || ''));
  const want = Buffer.from(ADMIN_TOKEN);
  return given.length === want.length && crypto.timingSafeEqual(given, want);
}

function adminJson(res, code, body) {
  res.writeHead(code, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(body));
}

/** Was die Website über einen Spieler zu sehen bekommt. */
function adminPlayerView(acc) {
  const collection = [];
  for (const [card, byFinish] of Object.entries(acc.collection || {})) {
    const counts = Array.isArray(byFinish) ? byFinish : [byFinish | 0];
    counts.forEach((count, finish) => {
      if (count > 0) collection.push({ card, finish, count });
    });
  }
  collection.sort((a, b) => a.card.localeCompare(b.card) || a.finish - b.finish);
  return {
    name: acc.name,
    coins: acc.coins | 0,
    dust: (acc.tokens || []).map(t => t | 0),
    packs: acc.packInv || {},
    rank: acc.rank || null,
    cosmetics: acc.cosmetics || [],
    decks: (acc.decks || []).map(d => d && d.name).filter(Boolean),
    cardCount: collection.reduce((sum, e) => sum + e.count, 0),
    collection
  };
}

/**
 * Führt eine Admin-Änderung aus. Gibt null zurück, wenn es geklappt hat, sonst
 * den Grund. Jede Änderung landet im Log — wer Spielwerte vergibt, muss das
 * nachher nachlesen können.
 */
function adminApply(acc, body) {
  const changes = [];

  if (body.coins !== undefined) {
    const value = Math.max(0, Math.round(Number(body.coins)));
    if (!Number.isFinite(value)) return 'coins ist keine Zahl.';
    changes.push(`Coins ${acc.coins | 0} -> ${value}`);
    acc.coins = value;
  }

  if (body.dust !== undefined) {
    if (!Array.isArray(body.dust) || body.dust.length !== (acc.tokens || []).length)
      return `dust muss ein Array mit ${(acc.tokens || []).length} Werten sein.`;
    const next = body.dust.map(v => Math.max(0, Math.round(Number(v))));
    if (next.some(v => !Number.isFinite(v))) return 'dust enthält keine Zahl.';
    changes.push(`Dust [${acc.tokens.join(',')}] -> [${next.join(',')}]`);
    acc.tokens = next;
  }

  if (Array.isArray(body.cards)) {
    for (const entry of body.cards) {
      const name = String(entry.card || '');
      if (cards[name] === undefined) return `Unbekannte Karte: ${name}`;
      const finish = Math.min(Math.max(entry.finish | 0, 0), finishes.COUNT - 1);
      const amount = Math.round(Number(entry.count));
      if (!Number.isFinite(amount) || amount === 0) continue;
      if (amount > 0) {
        for (let i = 0; i < amount; i++) finishes.add(acc.collection, name, finish);
      } else {
        for (let i = 0; i < -amount; i++) {
          if (!finishes.owns(acc.collection, name, finish)) break;
          finishes.remove(acc.collection, name, finish);
        }
      }
      changes.push(`${amount > 0 ? '+' : ''}${amount}x ${name} (${finishes.NAMES[finish]})`);
    }
  }

  if (changes.length === 0) return 'Nichts zu ändern.';
  saveAccount(acc);
  log(`ADMIN ${acc.name}: ${changes.join(' | ')}`);

  // Sitzt der Spieler gerade im Spiel, sieht er es sofort statt erst nach dem
  // nächsten Login — sonst wundert er sich, warum die Coins nicht ankommen.
  for (const client of clients)
    if (client.account === acc.name.toLowerCase()) sendProfile(client, acc);

  return null;
}

// ---- HTTP + WebSocket ----
const server = http.createServer((req, res) => {
  if (req.url === '/healthz') { res.writeHead(200, { 'Content-Type': 'text/plain' }); res.end('ok'); return; }

  if (req.url && req.url.startsWith('/admin/')) {
    if (!adminAllowed(req)) { res.writeHead(404); res.end(); return; }
    const url = new URL(req.url, 'http://localhost');

    if (url.pathname === '/admin/players' && req.method === 'GET') {
      const list = Object.values(accounts)
        .map(a => ({ name: a.name, coins: a.coins | 0 }))
        .sort((x, y) => x.name.localeCompare(y.name));
      return adminJson(res, 200, { players: list });
    }

    if (url.pathname === '/admin/cards' && req.method === 'GET')
      return adminJson(res, 200, { cards: Object.keys(cards).sort(), finishes: finishes.NAMES });

    if (url.pathname === '/admin/player' && req.method === 'GET') {
      const acc = accounts[String(url.searchParams.get('name') || '').toLowerCase()];
      if (!acc) return adminJson(res, 404, { error: 'Unbekannter Spieler.' });
      return adminJson(res, 200, adminPlayerView(acc));
    }

    if (url.pathname === '/admin/player' && req.method === 'POST') {
      let raw = '';
      req.on('data', chunk => {
        raw += chunk;
        if (raw.length > 200_000) req.destroy();   // kein unbegrenzter Puffer
      });
      req.on('end', () => {
        let body;
        try { body = JSON.parse(raw); }
        catch { return adminJson(res, 400, { error: 'Kein gültiges JSON.' }); }
        const acc = accounts[String(body.name || '').toLowerCase()];
        if (!acc) return adminJson(res, 404, { error: 'Unbekannter Spieler.' });
        const problem = adminApply(acc, body);
        if (problem) return adminJson(res, 400, { error: problem });
        return adminJson(res, 200, adminPlayerView(acc));
      });
      return;
    }

    return adminJson(res, 404, { error: 'Unbekannter Endpunkt.' });
  }

  res.writeHead(404); res.end();
});
const wss = new WebSocketServer({ server });

let nextId = 1;
const clients = new Set();
const lobbies = new Map();
let quickQueue = [];

const send = (c, obj) => { if (c && c.ws.readyState === 1) c.ws.send(JSON.stringify(obj)); };
const sendError = (c, msg) => send(c, { t: 'error', msg });
const sendProfile = (c, acc) => send(c, { t: 'profile', profile: profileOf(acc) });

function makeCode() {
  // 6 Zeichen ohne I/O/0/1 — laut vorlesbar ohne Verwechslung
  const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let code = '';
  for (let i = 0; i < 6; i++) code += chars[Math.floor(Math.random() * chars.length)];
  return lobbies.has(code) ? makeCode() : code;
}

function leaveEverything(c, notifyPeer = true) {
  quickQueue = quickQueue.filter(x => x !== c);
  if (c.lobbyCode) { lobbies.delete(c.lobbyCode); c.lobbyCode = null; }
  if (c.peer) {
    if (notifyPeer) send(c.peer, { t: 'peer_left' });
    c.peer.peer = null;
    c.peer = null;
  }
  // Server-autoritatives Duell: der Host wertet das Verlassen als Aufgabe
  if (c.serverDuelId) {
    hostSend({ op: 'leave', duelId: c.serverDuelId, side: c.serverSide });
    c.serverDuelId = null;
    c.serverSide = null;
  }
}

// ---- DuelHost-Brücke: server-autoritative Duelle ----
// Der .NET-DuelHost rechnet die Duelle; Node bleibt der einzige öffentliche
// Endpunkt und routet Sichten/Requests/Intents zwischen Host und Clients.
let duelHost = null;
let duelHostBuffer = '';
const serverDuels = new Map();   // duelId -> { a: client, b: client }
let nextDuelId = 1;

// Testinstanzen bringen ihren eigenen DuelHost mit (7901), damit sie der
// Produktion nicht die eine Verbindung wegnehmen, die der Host annimmt.
const DUELHOST_PORT = Number(process.env.DUELHOST_PORT) || 7900;

function connectDuelHost() {
  const sock = net.createConnection({ host: '127.0.0.1', port: DUELHOST_PORT });
  sock.on('connect', () => { duelHost = sock; duelHostBuffer = ''; log('DuelHost verbunden.'); });
  sock.on('data', chunk => {
    duelHostBuffer += chunk.toString('utf8');
    let newline;
    while ((newline = duelHostBuffer.indexOf('\n')) >= 0) {
      const line = duelHostBuffer.slice(0, newline);
      duelHostBuffer = duelHostBuffer.slice(newline + 1);
      if (!line.trim()) continue;
      try { handleHostMessage(JSON.parse(line)); }
      catch (error) { log('DuelHost: kaputte Zeile —', error.message); }
    }
  });
  sock.on('error', () => { /* close folgt */ });
  sock.on('close', () => {
    if (duelHost === sock) { duelHost = null; log('DuelHost getrennt — neuer Versuch in 3s.'); }
    setTimeout(connectDuelHost, 3000);
  });
}
connectDuelHost();

function hostSend(obj) {
  if (duelHost) duelHost.write(JSON.stringify(obj) + '\n');
}

function handleHostMessage(m) {
  if (m.op === 'pong') return;
  const duel = serverDuels.get(m.duelId);
  if (!duel) return;

  const to = m.to;
  const payload = { t: 'sduel', ...m };
  delete payload.to;

  if (m.op === 'end') {
    // Autoritative Belohnung: HIER weiß der Server wirklich, wer gewonnen hat
    for (const side of ['a', 'b']) {
      const client = duel[side];
      if (!client) continue;
      send(client, payload);
      client.serverDuelId = null;
      client.serverSide = null;
      const acc = client.account ? accounts[client.account] : null;
      if (acc) {
        const won = (m.winner === 'A') === (side === 'a');
        acc.coins += ECON.duelRewardCoins + (won ? ECON.winBonusCoins : 0);

        // Rang: der Boden wird hier durchgesetzt, nie im Client
        ranks.rolloverIfNeeded(acc);
        const change = ranks.applyResult(acc, won);
        send(client, {
          t: 'rank_change',
          rankDelta: change.delta,
          rankValue: change.after.rank,
          rankTier: change.after.tier,
          rankName: change.after.name,
          rankRp: change.after.rp,
          rankFromValue: change.before.rank,
          rankFromTier: change.before.tier,
          rankPromoted: change.promoted,
          rankUp: change.rankUp
        });
        log(`${acc.name}: ${won ? 'Sieg' : 'Niederlage'} ${change.delta >= 0 ? '+' : ''}${change.delta} RP `
          + `→ ${change.after.name} ${change.after.tier} (${change.after.rp})`);

        saveAccount(acc);
        sendProfile(client, acc);
      }
    }
    serverDuels.delete(m.duelId);
    log(`Server-Duell ${m.duelId} beendet — Sieger ${m.winner}.`);
    return;
  }

  if (!to || to === 'A') { if (duel.a) send(duel.a, payload); }
  if (!to || to === 'B') { if (duel.b) send(duel.b, payload); }
}

/** Startet ein server-autoritatives Duell für zwei eingeloggte, sduel-fähige Clients. */
function startServerDuel(a, b) {
  const accA = accounts[a.account];
  const accB = accounts[b.account];
  const deckA = accA.decks[a.deckIndex] || accA.decks[0];
  const deckB = accB.decks[b.deckIndex] || accB.decks[0];
  if (validateDeck(accA, deckA) || validateDeck(accB, deckB)) return false;

  const duelId = 'd' + (nextDuelId++);
  serverDuels.set(duelId, { a, b });
  a.serverDuelId = duelId; a.serverSide = 'A';
  b.serverDuelId = duelId; b.serverSide = 'B';

  hostSend({
    op: 'start', duelId,
    seed: Math.floor(Math.random() * 2147483647),
    aStarts: Math.random() < 0.5,
    // Die Finishes gehören zum Exemplar, nicht zur Karte: ohne sie läge im
    // Duell überall die schlichte Fassung, auch bei wem, der die glänzende
    // eingebaut hat.
    a: { name: a.name, deck: deckA.cards, extra: deckA.extra || [], hero: deckA.hero, kind: 'human',
         deckFinishes: deckA.cardFinishes || [], extraFinishes: deckA.extraFinishes || [] },
    b: { name: b.name, deck: deckB.cards, extra: deckB.extra || [], hero: deckB.hero, kind: 'human',
         deckFinishes: deckB.cardFinishes || [], extraFinishes: deckB.extraFinishes || [] }
  });
  send(a, { t: 'sduel_start', duelId, youAre: 'A', opponent: b.name, ...equippedOf(b) });
  send(b, { t: 'sduel_start', duelId, youAre: 'B', opponent: a.name, ...equippedOf(a) });
  log(`Server-Duell ${duelId}: ${a.name} vs ${b.name}`);
  return true;
}

/**
 * Die ausgerüstete Kosmetik eines Spielers, wie der Gegner sie zu sehen bekommt.
 * Geht mit der Match-Nachricht raus, also VOR dem ersten Bild des Duells — der
 * Client soll nie mitten im Spiel nachladen müssen. Kennt er einen Gegenstand
 * nicht, fällt er still auf das Standardaussehen zurück.
 */
function equippedOf(client) {
  const acc = client.account ? accounts[client.account] : null;
  const eq = acc && acc.equipped && typeof acc.equipped === 'object' ? acc.equipped : {};
  return { oppSlots: cosmetics.SLOTS, oppIds: cosmetics.SLOTS.map(slot => eq[slot] || '') };
}

/**
 * Ein Duell aufsetzen. Es gibt nur noch **einen** Weg: der DuelHost rechnet, die
 * Clients zeigen an.
 *
 * Früher fiel diese Funktion auf ein Lockstep-Duell zurück, wenn der Host nicht
 * erreichbar oder ein Spieler nicht eingeloggt war — beide Clients rechneten dann
 * selbst mit geteiltem Seed. Dieser zweite Weg ist raus: er wurde nie
 * mitgepflegt, und ein Spieler landete darin, ohne es zu merken. Klappt das
 * Server-Duell nicht, sagen wir das lieber, als heimlich etwas Schlechteres zu
 * liefern.
 */
function startMatch(a, b) {
  if (duelHost && a.sduel && b.sduel && a.account && b.account && startServerDuel(a, b)) return;

  const why = !duelHost ? 'Duel server is not available right now.'
    : !a.account || !b.account ? 'Both duelists have to be signed in.'
    : !a.sduel || !b.sduel ? 'One client is too old for online duels — please update.'
    : 'The duel could not be started.';
  sendError(a, why);
  sendError(b, why);
  log(`Match abgelehnt: ${a.name}(#${a.id}) vs ${b.name}(#${b.id}) — ${why}`);
}

wss.on('connection', (ws, req) => {
  const c = { id: nextId++, name: 'Player', ws, peer: null, lobbyCode: null, alive: true, account: null, rewardPending: false };
  clients.add(c);
  log(`Verbunden: #${c.id} (${req.socket.remoteAddress}) — ${clients.size} online`);
  send(c, { t: 'welcome', id: c.id });

  ws.on('pong', () => { c.alive = true; });

  ws.on('message', raw => {
    if (raw.length > 128 * 1024) return;
    let m;
    try { m = JSON.parse(raw); } catch { return; }
    const acc = c.account ? accounts[c.account] : null;

    switch (m.t) {
      case 'hello':
        c.name = String(m.name || 'Player').slice(0, 20).trim() || 'Player';
        c.sduel = !!m.sduel;   // Client beherrscht server-autoritative Duelle
        break;

      // ---- Account ----
      case 'register': {
        const name = String(m.name || '').trim().slice(0, 20);
        const key = name.toLowerCase();
        if (name.length < 3) { sendError(c, 'Name too short (min. 3 characters).'); break; }
        if (String(m.pass || '').length < 4) { sendError(c, 'Password too short (min. 4 characters).'); break; }
        if (accounts[key]) { sendError(c, 'Name is already taken.'); break; }
        accounts[key] = newAccount(name, m.pass);
        saveAccount(accounts[key]);
        c.account = key;
        c.name = name;
        send(c, { t: 'auth_ok', profile: profileOf(accounts[key]) });
        log(`Neuer Account: ${name}`);
        break;
      }

      case 'login': {
        const key = String(m.name || '').trim().toLowerCase();
        const found = accounts[key];
        // Steam-only-Accounts haben kein Passwort (hash ''), hashPass liefert nie ''
        if (found && !found.hash) { sendError(c, 'This account signs in through Steam.'); break; }
        if (!found || found.hash !== hashPass(m.pass || '', found.salt)) { sendError(c, 'Wrong name or password.'); break; }
        c.account = key;
        c.name = found.name;
        send(c, { t: 'auth_ok', profile: profileOf(found) });
        log(`Login: ${found.name}`);
        break;
      }

      // Anmeldung über Steam: Ticket bei Valve prüfen, dann Account finden/anlegen
      case 'steam_auth':
        handleSteamAuth(c, m);
        break;

      // steam_link und set_password gibt es bewusst nicht mehr — siehe oben.

      // ---- Shop ----
      case 'buy_pack': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const packDef = packs[String(m.pack || '')];
        if (!packDef) { sendError(c, 'Pack not found.'); break; }
        // Unique-Packs (Hero Cache) ziehen nur Karten, die dem Konto fehlen.
        // Wer schon alles hat, soll gar nicht erst zahlen duerfen.
        if (packDef.unique && uniquePool(packDef, acc).length === 0) {
          sendError(c, 'You already own every card in this pack.');
          break;
        }
        const toBuy = Math.min(10, Math.max(1, m.packCount | 0 || 1));
        const totalPrice = packDef.price * toBuy;
        if (acc.coins < totalPrice) { sendError(c, `Not enough coins (${totalPrice} needed).`); break; }
        acc.coins -= totalPrice;
        acc.packInv[m.pack] = (acc.packInv[m.pack] || 0) + toBuy;
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name} kauft '${m.pack}' (${packDef.price} Coins)`);
        break;
      }

      case 'open_pack': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const packDef = packs[String(m.pack || '')];
        if (!packDef) { sendError(c, 'Pack not found.'); break; }
        const ownedPacks = acc.packInv[m.pack] | 0;
        // Bis zu zehn auf einmal — begrenzt durch Besitz. packCount fehlt bei
        // alten Clients: dann wie immer eines.
        const toOpen = Math.min(10, Math.max(1, m.packCount | 0 || 1), ownedPacks);
        if (toOpen < 1) { sendError(c, 'You do not own this pack.'); break; }

        const drawn = [];
        const drawnFinishes = [];
        let refundedPacks = 0;
        for (let n = 0; n < toOpen; n++) {
          acc.packInv[m.pack] -= 1;
          if (packDef.unique) {
            // Hero Cache: EINE Karte, die dem Konto fehlt. Der Pool schrumpft
            // waehrend einer Mehrfach-Oeffnung mit — laeuft er leer, wird der
            // Rest erstattet statt leere Packs aufzureissen.
            const pool = uniquePool(packDef, acc);
            if (pool.length === 0) { acc.coins += packDef.price; refundedPacks += 1; continue; }
            const pick = pool[Math.floor(Math.random() * pool.length)];
            const fin = finishes.roll();
            finishes.add(acc.collection, pick, fin);
            drawn.push(pick); drawnFinishes.push(fin);
          } else {
            for (const name of drawFromPack(packDef)) {
              const fin = finishes.roll();
              finishes.add(acc.collection, name, fin);
              drawn.push(name); drawnFinishes.push(fin);
            }
          }
        }
        saveAccount(acc);
        if (drawn.length === 0) {
          sendError(c, `Every card in this pack is already yours — ${refundedPacks * packDef.price} coins refunded.`);
          sendProfile(c, acc);
          break;
        }
        send(c, { t: 'pack_result', packCards: drawn, packFinishes: drawnFinishes, profile: profileOf(acc) });
        const shiny = drawn
          .map((name, i) => drawnFinishes[i] ? `${name} (${finishes.NAMES[drawnFinishes[i]]})` : name)
          .join(', ');
        log(`${acc.name} öffnet ${toOpen}x '${m.pack}': ${shiny}`);
        break;
      }

      // ---- Crafting ----
      case 'craft': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const rarity = cards[String(m.card || '')];
        if (rarity === undefined) { sendError(c, 'Unknown card.'); break; }
        // Helden kommen aus dem Hero Cache, nicht aus der Werkbank — sonst
        // waere das 5000-Coin-Pack neben 30 Legendary-Dust eine Zierde.
        const heroPack = packs['Hero Cache'];
        if (heroPack && Array.isArray(heroPack.cards) && heroPack.cards.includes(String(m.card))) {
          sendError(c, 'Player Cards cannot be crafted — open a Hero Cache in the shop.');
          break;
        }
        const cost = ECON.craftCost[rarity];
        if (acc.tokens[rarity] < cost) { sendError(c, `Not enough dust (${cost} needed).`); break; }
        acc.tokens[rarity] -= cost;
        // Auch beim Fertigen wird das Finish gewürfelt — gleiche Raten wie im Pack.
        // Gezielt herstellen kann man es nicht.
        const craftedFinish = finishes.roll();
        finishes.add(acc.collection, m.card, craftedFinish);
        saveAccount(acc);
        send(c, { t: 'craft_result', card: String(m.card), finish: craftedFinish, profile: profileOf(acc) });
        if (craftedFinish !== finishes.PLAIN)
          log(`${acc.name} fertigt '${m.card}' — ${finishes.NAMES[craftedFinish]}!`);
        break;
      }

      /*
       * Zerlegen. Schlichte Exemplare geben Staub wie bisher; Sonderexemplare
       * geben stattdessen Coins — und zwar deutlich mehr, je seltener das
       * Finish. Das Finish muss ausdrücklich mitgeschickt werden, damit
       * niemand versehentlich ein Static für Staub verliert.
       */
      case 'dust': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const rarity = cards[String(m.card || '')];
        if (rarity === undefined) { sendError(c, 'Unknown card.'); break; }
        const finish = Math.min(Math.max(m.finish | 0, 0), finishes.COUNT - 1);
        if (!finishes.owns(acc.collection, m.card, finish)) {
          sendError(c, finish === finishes.PLAIN ? 'Card not owned.'
            : `No ${finishes.NAMES[finish]} copy owned.`);
          break;
        }
        finishes.remove(acc.collection, m.card, finish);
        if (finish === finishes.PLAIN) {
          acc.tokens[rarity] += ECON.dustGain[rarity];
        } else {
          acc.coins = (acc.coins | 0) + finishes.COIN_VALUE[finish];
          log(`${acc.name} zerlegt '${m.card}' (${finishes.NAMES[finish]}) → +${finishes.COIN_VALUE[finish]} Coins`);
        }
        saveAccount(acc);
        sendProfile(c, acc);
        break;
      }

      // ---- Kosmetik ----
      case 'buy_cosmetic': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const problem = cosmetics.buy(acc, String(m.item || ''));
        if (problem) { sendError(c, problem); break; }
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name} kauft Kosmetik '${m.item}'`);
        break;
      }

      case 'equip_cosmetic': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const problem = cosmetics.equip(acc, String(m.slot || ''), String(m.item || ''));
        if (problem) { sendError(c, problem); break; }
        saveAccount(acc);
        sendProfile(c, acc);
        break;
      }

      // ---- Decks ----
      case 'claim_starter': {
        if (!acc) { sendError(c, 'Sign in first.'); break; }
        const problem = grantStarterDeck(acc, String(m.starter || ''));
        if (problem) { sendError(c, problem); break; }
        sendProfile(c, acc);
        break;
      }

      case 'save_deck': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const index = Number(m.deckIndex);
        if (!Number.isInteger(index) || index < 0 || index > acc.decks.length || index >= ECON.maxDecks) { sendError(c, 'Invalid deck index.'); break; }
        const toFinishes = (list, length) => {
          const out = new Array(length).fill(0);
          if (Array.isArray(list))
            for (let i = 0; i < length && i < list.length; i++)
              out[i] = Math.min(Math.max(list[i] | 0, 0), finishes.COUNT - 1);
          return out;
        };
        const deck = m.deck ? {
          name: String(m.deck.name || 'Deck').slice(0, 30),
          hero: String(m.deck.hero || ''),
          cards: (m.deck.cards || []).map(String),
          extra: (m.deck.extra || []).map(String),
          cardFinishes: toFinishes(m.deck.cardFinishes, (m.deck.cards || []).length),
          extraFinishes: toFinishes(m.deck.extraFinishes, (m.deck.extra || []).length)
        } : null;
        const problem = validateDeck(acc, deck);
        if (problem) { sendError(c, problem); break; }
        if (index === acc.decks.length) acc.decks.push(deck);
        else acc.decks[index] = deck;
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name} speichert Deck '${deck.name}' (${deck.cards.length} Karten)`);
        break;
      }

      case 'delete_deck': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const index = Number(m.deckIndex);
        if (!Number.isInteger(index) || index < 0 || index >= acc.decks.length) { sendError(c, 'Invalid deck index.'); break; }
        if (acc.decks.length <= 1) { sendError(c, 'The last deck cannot be deleted.'); break; }
        const removed = acc.decks.splice(index, 1)[0];
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name} löscht Deck '${removed.name}'`);
        break;
      }

      // ---- Feedback ----
      case 'feedback': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const text = String(m.msg || '').trim().slice(0, 1000);
        if (text.length < 3) { sendError(c, 'Please write a little more.'); break; }
        const now = Date.now();
        if (c.lastFeedback && now - c.lastFeedback < 30000) { sendError(c, 'Please wait a moment before sending again.'); break; }
        c.lastFeedback = now;
        const entry = { at: new Date(now).toISOString(), account: acc.name, version: String(m.card || ''), text };
        try {
          fs.appendFileSync(FEEDBACK_FILE, JSON.stringify(entry) + '\n');
          send(c, { t: 'feedback_ok' });
          log(`FEEDBACK von ${acc.name}: ${text.replace(/\s+/g, ' ').slice(0, 120)}`);
        } catch (err) {
          sendError(c, 'Could not store feedback.');
          log(`Feedback-Fehler: ${err.message}`);
        }
        break;
      }

      // ---- Belohnung ----
      case 'duel_result': {
        if (!acc || !c.rewardPending) break;
        c.rewardPending = false;
        acc.coins += ECON.duelRewardCoins + (m.won ? ECON.winBonusCoins : 0);
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name}: Duell-Belohnung (${m.won ? 'Sieg' : 'Niederlage'}) → ${acc.coins} Coins`);
        break;
      }

      case 'claim_daily': {
        if (!acc) { sendError(c, 'Not logged in.'); break; }
        const gapDaily = Date.now() - (acc.daily.lastClaim || 0);
        if (gapDaily < ECON.dailyCooldownMs) { sendError(c, 'Daily seal not ready yet.'); break; }
        acc.daily.streak = acc.daily.lastClaim > 0 && gapDaily <= ECON.dailyStreakBreakMs
          ? acc.daily.streak + 1 : 1;
        acc.daily.lastClaim = Date.now();
        acc.coins += ECON.dailyRewardCoins;
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name}: Daily-Siegel Tag ${acc.daily.streak} → ${acc.coins} Coins`);
        break;
      }

      // Turm-Fortschritt: monoton, eine Ebene nach der anderen. Dadurch gibt es
      // die Erstsieg-Belohnung (5 Relic Packs + Meilenstein-Titel) automatisch
      // nur einmal pro Ebene — Wiederholungssiege laufen ins Leere.
      case 'tower_progress': {
        if (!acc) break;
        const floor = m.floor | 0;
        const cleared = acc.towerFloor | 0;
        if (floor !== cleared + 1 || floor < 1 || floor > 15) { sendProfile(c, acc); break; }
        acc.towerFloor = floor;
        acc.packInv['Relic Pack'] = (acc.packInv['Relic Pack'] || 0) + 5;
        acc.cosmetics = Array.isArray(acc.cosmetics) ? acc.cosmetics : [];
        const towerTitle = floor === 1 ? 'tower_initiate'
          : floor === 10 ? 'renewer_of_seals'
          : floor === 15 ? 'towers_answer' : null;
        if (towerTitle && !acc.cosmetics.includes(towerTitle)) acc.cosmetics.push(towerTitle);
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name}: Turm-Ebene ${floor} versiegelt (+5 Relic Packs${towerTitle ? `, Titel ${towerTitle}` : ''})`);
        break;
      }

      case 'solo_result': {
        if (!acc) break;
        const now = Date.now();
        if (c.lastSoloReward && now - c.lastSoloReward < ECON.soloRewardCooldownMs) break; // Spam-Schutz
        c.lastSoloReward = now;
        acc.coins += ECON.soloRewardCoins;
        saveAccount(acc);
        sendProfile(c, acc);
        log(`${acc.name}: Solo-Belohnung → ${acc.coins} Coins`);
        break;
      }

      // ---- Lobby & Matchmaking ----
      case 'queue': {
        c.deckIndex = Number.isInteger(m.deckIndex) ? m.deckIndex : 0;
        leaveEverything(c);
        quickQueue = quickQueue.filter(x => x.ws.readyState === 1);
        const partner = quickQueue.shift();
        if (partner && partner !== c) startMatch(partner, c);
        else { quickQueue.push(c); send(c, { t: 'queued' }); }
        break;
      }

      case 'create': {
        leaveEverything(c);
        const code = makeCode();
        c.lobbyCode = code;
        lobbies.set(code, c);
        send(c, { t: 'lobby', code });
        log(`Lobby ${code} von ${c.name}(#${c.id})`);
        break;
      }

      case 'join': {
        leaveEverything(c);
        const host = lobbies.get(String(m.code || '').toUpperCase().trim());
        if (!host || host.ws.readyState !== 1 || host === c) { sendError(c, 'Lobby not found.'); break; }
        lobbies.delete(host.lobbyCode);
        host.lobbyCode = null;
        startMatch(host, c);
        break;
      }

      case 'sduel_intent':
        if (c.serverDuelId)
          hostSend({ op: 'intent', duelId: c.serverDuelId, side: c.serverSide, answer: m.answer || {} });
        break;

      case 'leave':
        leaveEverything(c);
        send(c, { t: 'left' });
        break;

      case 'relay':
        if (c.peer) send(c.peer, { t: 'relay', data: m.data });
        break;
    }
  });

  ws.on('close', () => {
    leaveEverything(c);
    clients.delete(c);
    log(`Getrennt: #${c.id} — ${clients.size} online`);
  });
  ws.on('error', () => { /* close folgt */ });
});

setInterval(() => {
  for (const c of clients) {
    if (!c.alive) { c.ws.terminate(); continue; }
    c.alive = false;
    try { c.ws.ping(); } catch { /* ignorieren */ }
  }
}, 30000);

// ---- Sauberes Herunterfahren: offene Änderungen noch wegschreiben ----
let shuttingDown = false;
function shutdown(signal) {
  if (shuttingDown) return;
  shuttingDown = true;
  log(`${signal} — fahre herunter, schreibe offene Änderungen`);
  try { db.close(); } catch (error) { log('DB-Close-Fehler:', error.message); }
  server.close(() => process.exit(0));
  setTimeout(() => process.exit(0), 1500).unref();
}
process.on('SIGTERM', () => shutdown('SIGTERM'));
process.on('SIGINT', () => shutdown('SIGINT'));

server.listen(PORT, () => {
  const s = db.stats();
  log(`Rouge-TCG-Server v3 läuft auf Port ${PORT} — ${Object.keys(cards).length} Karten, ${Object.keys(packs).length} Packs, ${s.accounts} Accounts (DB ${Math.round(s.bytes / 1024)} KB, ${s.cards} Sammlungszeilen)`);
  log(`Steam-Anmeldung: ${steamConfigured ? `aktiv (App-ID ${STEAM_APP_ID})` : 'nicht konfiguriert'}`
    + (STEAM_DEV_MODE ? ' — ACHTUNG: STEAM_DEV_MODE ist an, Tickets werden NICHT geprüft!' : ''));
});
