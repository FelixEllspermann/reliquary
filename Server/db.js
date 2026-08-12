// Persistenz der Accounts in SQLite.
//
// Vorher lag alles in einer accounts.json, die bei *jeder* Änderung komplett neu
// geschrieben wurde — bei n Accounts also O(n) Arbeit pro Pack-Öffnung. Hier wird
// nur noch die eine geänderte Zeile geschrieben.
//
// Das Speichermodell im Server bleibt unverändert: ein Account ist weiterhin ein
// schlichtes Objekt im RAM. Diese Datei kümmert sich ausschliesslich darum, es
// zu laden und geändert wieder wegzuschreiben.
//
// node:sqlite ist ab Node 22 eingebaut — kein natives Modul, kein Compiler nötig.

import { DatabaseSync } from 'node:sqlite';
import crypto from 'crypto';
import fs from 'fs';
import path from 'path';
import * as finishes from './finishes.js';

const SCHEMA_VERSION = 1;

/**
 * Öffnet (und erstellt bei Bedarf) die Datenbank und liefert die Zugriffe darauf.
 * @param {string} dataDir Verzeichnis mit accounts.db
 * @param {(...args:any[])=>void} log
 */
export function openDatabase(dataDir, log = console.log) {
  const file = path.join(dataDir, 'accounts.db');
  const db = new DatabaseSync(file);

  // WAL: Lesen blockiert Schreiben nicht, und ein Absturz kostet keine Datenbank.
  db.exec('PRAGMA journal_mode = WAL');
  db.exec('PRAGMA synchronous = NORMAL');
  db.exec('PRAGMA foreign_keys = ON');

  db.exec(`
    CREATE TABLE IF NOT EXISTS meta (
      key   TEXT PRIMARY KEY,
      value TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS accounts (
      key      TEXT PRIMARY KEY,
      name     TEXT NOT NULL,
      salt     TEXT NOT NULL,
      hash     TEXT NOT NULL,
      coins    INTEGER NOT NULL DEFAULT 0,
      tokens   TEXT NOT NULL DEFAULT '[0,0,0,0]',
      daily    TEXT NOT NULL DEFAULT '{}',
      decks    TEXT NOT NULL DEFAULT '[]',
      pack_inv TEXT NOT NULL DEFAULT '{}',
      created  INTEGER NOT NULL,
      updated  INTEGER NOT NULL
    );
    CREATE TABLE IF NOT EXISTS collection (
      account TEXT NOT NULL REFERENCES accounts(key) ON DELETE CASCADE,
      card    TEXT NOT NULL,
      count   INTEGER NOT NULL,
      PRIMARY KEY (account, card)
    );
    CREATE INDEX IF NOT EXISTS collection_card ON collection(card);
  `);

  // --- Migrationen (bestehende Datenbanken nachziehen) ---
  const columns = db.prepare('PRAGMA table_info(accounts)').all().map(c => c.name);
  if (!columns.includes('steam_id')) {
    // Steam-Anmeldung: eine SteamID gehört zu höchstens einem Account.
    db.exec('ALTER TABLE accounts ADD COLUMN steam_id TEXT');
    db.exec('CREATE UNIQUE INDEX IF NOT EXISTS accounts_steam ON accounts(steam_id) WHERE steam_id IS NOT NULL');
    log('DB-Migration: Spalte steam_id ergänzt.');
  }
  if (!columns.includes('rank')) {
    // Rangleiter. `rank` ist ein JSON-Block, damit spätere Felder (Saisonpass,
    // Statistiken) keine weitere Migration brauchen.
    db.exec("ALTER TABLE accounts ADD COLUMN rank TEXT NOT NULL DEFAULT '{}'");
    log('DB-Migration: Spalte rank ergänzt.');
  }
  if (!columns.includes('cosmetics')) {
    // Kosmetik: Shards, Besitz und ausgerüstete Fächer — wieder als JSON-Block,
    // damit neue Fächer keine Migration brauchen.
    db.exec("ALTER TABLE accounts ADD COLUMN cosmetics TEXT NOT NULL DEFAULT '{}'");
    log('DB-Migration: Spalte cosmetics ergänzt.');
  }
  if (!columns.includes('starter_pick')) {
    // Welches Startdeck der Spieler gewaehlt hat, oder leer.
    //
    // Das MUSS eine eigene Spalte sein: loadAll baut das Konto aus benannten
    // Spalten, nicht aus einem Blob. Ein Feld, das hier fehlt, existiert nach
    // dem naechsten Serverstart nicht mehr — der Spieler bekaeme die Auswahl
    // wieder vorgelegt und koennte ein zweites Deck kassieren.
    db.exec("ALTER TABLE accounts ADD COLUMN starter_pick TEXT NOT NULL DEFAULT ''");
    log('DB-Migration: Spalte starter_pick ergaenzt.');
  }
  if (!columns.includes('progress')) {
    // Spielfortschritt jenseits der Sammlung: Turm-Ebene, laufender Draft,
    // Draft-Abschluesse — als JSON-Block, damit neue Modi keine Migration
    // brauchen. Bis hierher lebte towerFloor NUR im Speicher: jeder Neustart
    // warf alle Spieler zurueck auf Ebene 0 (und oeffnete die Erstsieg-Packs
    // erneut). Ab jetzt ueberlebt der Fortschritt.
    db.exec("ALTER TABLE accounts ADD COLUMN progress TEXT NOT NULL DEFAULT '{}'");
    log('DB-Migration: Spalte progress ergaenzt (Turm & Draft ueberleben Neustarts).');
  }

  // Karten-Finishes: ein Finish gehört dem Exemplar, also gehört es in den
  // Primärschlüssel. SQLite kann keinen Schlüssel ändern — die Tabelle wird
  // deshalb neu gebaut und der Bestand als „schlicht" übernommen.
  const collectionColumns = db.prepare('PRAGMA table_info(collection)').all().map(c => c.name);
  if (!collectionColumns.includes('finish')) {
    db.exec(`
      BEGIN;
      CREATE TABLE collection_new (
        account TEXT NOT NULL REFERENCES accounts(key) ON DELETE CASCADE,
        card    TEXT NOT NULL,
        finish  INTEGER NOT NULL DEFAULT 0,
        count   INTEGER NOT NULL,
        PRIMARY KEY (account, card, finish)
      );
      INSERT INTO collection_new (account, card, finish, count)
        SELECT account, card, 0, count FROM collection;
      DROP TABLE collection;
      ALTER TABLE collection_new RENAME TO collection;
      CREATE INDEX IF NOT EXISTS collection_card ON collection(card);
      COMMIT;
    `);
    log('DB-Migration: Sammlung um Finishes erweitert (Bestand = schlicht).');
  }

  // Deck-Statistiken: EIN Aggregat je Deck-Zusammensetzung (Hash über Held +
  // sortierte Kartenlisten). Die Anzeige braucht keine Einzel-Matches, und ein
  // Aggregat kann bei Regeländerungen nicht "falsch nachberechnet" werden.
  db.exec(`
    CREATE TABLE IF NOT EXISTS deck_stats (
      hash      TEXT PRIMARY KEY,
      name      TEXT NOT NULL DEFAULT '',
      hero      TEXT NOT NULL DEFAULT '',
      cards     TEXT NOT NULL DEFAULT '[]',
      extra     TEXT NOT NULL DEFAULT '[]',
      games     INTEGER NOT NULL DEFAULT 0,
      wins      INTEGER NOT NULL DEFAULT 0,
      pvp_games INTEGER NOT NULL DEFAULT 0,
      pvp_wins  INTEGER NOT NULL DEFAULT 0,
      updated   INTEGER NOT NULL DEFAULT 0
    );
  `);

  // Karten-Statistiken: je Karte zählt ein Match EINMAL (egal wie viele Kopien
  // im Deck lagen). card_pairs hält, wie oft zwei Karten zusammen in einem
  // Deck ein Match bestritten haben — Grundlage für "often paired with".
  // Paare sind alphabetisch normiert (a < b), damit jede Kombination genau
  // eine Zeile hat.
  db.exec(`
    CREATE TABLE IF NOT EXISTS card_stats (
      card      TEXT PRIMARY KEY,
      games     INTEGER NOT NULL DEFAULT 0,
      wins      INTEGER NOT NULL DEFAULT 0,
      pvp_games INTEGER NOT NULL DEFAULT 0,
      pvp_wins  INTEGER NOT NULL DEFAULT 0,
      updated   INTEGER NOT NULL DEFAULT 0
    );
    CREATE TABLE IF NOT EXISTS card_pairs (
      a     TEXT NOT NULL,
      b     TEXT NOT NULL,
      games INTEGER NOT NULL DEFAULT 0,
      wins  INTEGER NOT NULL DEFAULT 0,
      PRIMARY KEY (a, b)
    );
    CREATE INDEX IF NOT EXISTS card_pairs_b ON card_pairs(b);
  `);

  // Match-Historie je Konto: die letzten Spiele für die Profil-Seite.
  db.exec(`
    CREATE TABLE IF NOT EXISTS match_log (
      id        INTEGER PRIMARY KEY AUTOINCREMENT,
      account   TEXT NOT NULL,
      ts        INTEGER NOT NULL,
      mode      TEXT NOT NULL,
      opponent  TEXT NOT NULL DEFAULT '',
      deck_name TEXT NOT NULL DEFAULT '',
      won       INTEGER NOT NULL DEFAULT 0
    );
    CREATE INDEX IF NOT EXISTS match_log_account ON match_log(account, ts);
  `);

  db.prepare('INSERT OR REPLACE INTO meta(key, value) VALUES(?, ?)')
    .run('schema_version', String(SCHEMA_VERSION));

  const statements = {
    selectAccounts: db.prepare('SELECT * FROM accounts'),
    selectCollection: db.prepare('SELECT account, card, finish, count FROM collection'),
    upsertAccount: db.prepare(`
      INSERT INTO accounts (key, name, salt, hash, coins, tokens, daily, decks, pack_inv, steam_id, rank, cosmetics, starter_pick, progress, created, updated)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      ON CONFLICT(key) DO UPDATE SET
        name = excluded.name, salt = excluded.salt, hash = excluded.hash,
        coins = excluded.coins, tokens = excluded.tokens, daily = excluded.daily,
        decks = excluded.decks, pack_inv = excluded.pack_inv, steam_id = excluded.steam_id,
        rank = excluded.rank, cosmetics = excluded.cosmetics,
        starter_pick = excluded.starter_pick, progress = excluded.progress, updated = excluded.updated
    `),
    clearCollection: db.prepare('DELETE FROM collection WHERE account = ?'),
    insertCard: db.prepare('INSERT INTO collection (account, card, finish, count) VALUES (?, ?, ?, ?)'),
    deleteAccount: db.prepare('DELETE FROM accounts WHERE key = ?'),
    upsertDeckStat: db.prepare(`
      INSERT INTO deck_stats (hash, name, hero, cards, extra, games, wins, pvp_games, pvp_wins, updated)
      VALUES (?, ?, ?, ?, ?, 1, ?, ?, ?, ?)
      ON CONFLICT(hash) DO UPDATE SET
        name = excluded.name, hero = excluded.hero,
        games = games + 1, wins = wins + excluded.wins,
        pvp_games = pvp_games + excluded.pvp_games, pvp_wins = pvp_wins + excluded.pvp_wins,
        updated = excluded.updated
    `),
    selectTopDecks: db.prepare('SELECT * FROM deck_stats ORDER BY games DESC, updated DESC LIMIT ?'),
    upsertCardStat: db.prepare(`
      INSERT INTO card_stats (card, games, wins, pvp_games, pvp_wins, updated)
      VALUES (?, 1, ?, ?, ?, ?)
      ON CONFLICT(card) DO UPDATE SET
        games = games + 1, wins = wins + excluded.wins,
        pvp_games = pvp_games + excluded.pvp_games, pvp_wins = pvp_wins + excluded.pvp_wins,
        updated = excluded.updated
    `),
    // Die Karten-Statistik wertet NUR Online-Matches — Solo verzerrt (Bots).
    selectTopCards: db.prepare('SELECT * FROM card_stats WHERE pvp_games > 0 ORDER BY pvp_games DESC, card ASC LIMIT ?'),
    upsertCardPair: db.prepare(`
      INSERT INTO card_pairs (a, b, games, wins) VALUES (?, ?, 1, ?)
      ON CONFLICT(a, b) DO UPDATE SET games = games + 1, wins = wins + excluded.wins
    `),
    selectPartners: db.prepare(`
      SELECT CASE WHEN a = ? THEN b ELSE a END AS partner, games, wins
      FROM card_pairs WHERE a = ? OR b = ?
      ORDER BY games DESC, partner ASC LIMIT ?
    `),
    insertMatchLog: db.prepare(`
      INSERT INTO match_log (account, ts, mode, opponent, deck_name, won)
      VALUES (?, ?, ?, ?, ?, ?)
    `),
    selectRecentMatches: db.prepare(`
      SELECT ts, mode, opponent, deck_name, won FROM match_log
      WHERE account = ? ORDER BY ts DESC, id DESC LIMIT ?
    `),
    countMatchStats: db.prepare(`
      SELECT mode, COUNT(*) AS games, SUM(won) AS wins FROM match_log
      WHERE account = ? GROUP BY mode
    `),
    countAccounts: db.prepare('SELECT COUNT(*) AS n FROM accounts'),
    countCards: db.prepare('SELECT COUNT(*) AS n FROM collection')
  };

  const parse = (text, fallback) => {
    try { return JSON.parse(text); } catch { return fallback; }
  };

  /** Alle Accounts als { key: accountObjekt } — das Format, das der Server erwartet. */
  function loadAll() {
    const result = {};
    for (const row of statements.selectAccounts.all()) {
      result[row.key] = {
        name: row.name,
        salt: row.salt,
        hash: row.hash,
        coins: row.coins,
        tokens: parse(row.tokens, [0, 0, 0, 0]),
        daily: parse(row.daily, { streak: 0, lastClaim: 0 }),
        decks: parse(row.decks, []),
        packInv: parse(row.pack_inv, {}),
        steamId: row.steam_id || null,
        collection: {},
        // Leerer String heisst "noch nicht gewaehlt" — als null zurueck, damit
        // der Server nur an einer Stelle auf Wahrheit pruefen muss.
        starterPick: row.starter_pick || null,
        ...parse(row.rank, {}),      // rp, peakRank, season, wins, losses, streak, bestStreak, careerRp
        ...parse(row.cosmetics, {}), // cosmetics, equipped (ein altes shards-Feld wird ignoriert)
        ...parse(row.progress, {})   // towerFloor, draft, draftClears
      };
    }
    for (const row of statements.selectCollection.all()) {
      const account = result[row.account];
      if (!account) continue;
      const entry = account.collection[row.card] || finishes.emptyEntry();
      entry[Math.min(Math.max(row.finish | 0, 0), finishes.COUNT - 1)] = row.count;
      account.collection[row.card] = entry;
    }
    return result;
  }

  /**
   * Schreibt genau einen Account. Die Sammlung wird als Ganzes ersetzt — bei
   * höchstens ein paar hundert Zeilen in einer Transaktion ist das schneller
   * und deutlich weniger fehleranfällig als ein Diff.
   */
  function writeAccount(key, account) {
    const now = Date.now();
    db.exec('BEGIN');
    try {
      statements.upsertAccount.run(
        key,
        account.name,
        account.salt,
        account.hash,
        account.coins | 0,
        JSON.stringify(account.tokens || [0, 0, 0, 0]),
        JSON.stringify(account.daily || { streak: 0, lastClaim: 0 }),
        JSON.stringify(account.decks || []),
        JSON.stringify(account.packInv || {}),
        account.steamId || null,
        JSON.stringify({
          rp: account.rp | 0,
          peakRank: account.peakRank || 1,
          season: account.season || null,
          wins: account.wins | 0,
          losses: account.losses | 0,
          streak: account.streak | 0,
          bestStreak: account.bestStreak | 0,
          careerRp: account.careerRp | 0
        }),
        JSON.stringify({
          cosmetics: Array.isArray(account.cosmetics) ? account.cosmetics : [],
          equipped: account.equipped && typeof account.equipped === 'object' ? account.equipped : {}
        }),
        account.starterPick || '',
        JSON.stringify({
          towerFloor: account.towerFloor | 0,
          draft: account.draft || null,
          draftClears: account.draftClears | 0,
          // Profil-Schaufenster und NEW-Badges: gleiche Falle wie einst der
          // Turm — was hier fehlt, existiert nach dem nächsten Neustart nicht.
          showcase: Array.isArray(account.showcase) ? account.showcase : [],
          newCards: Array.isArray(account.newCards) ? account.newCards : []
        }),
        now,
        now
      );
      statements.clearCollection.run(key);
      for (const [card, entry] of Object.entries(account.collection || {})) {
        const counts = finishes.normalise(entry);
        for (let finish = 0; finish < finishes.COUNT; finish++)
          if (counts[finish] > 0) statements.insertCard.run(key, card, finish, counts[finish]);
      }
      db.exec('COMMIT');
    } catch (error) {
      db.exec('ROLLBACK');
      throw error;
    }
  }

  // ---- Verzögertes Schreiben ----
  // Mehrere Änderungen am selben Account in derselben Sekunde sollen eine
  // Schreiboperation ergeben, nicht fünf.
  const dirty = new Map();   // key -> accountObjekt
  let timer = null;
  let closed = false;

  function flush() {
    clearTimeout(timer);
    timer = null;
    if (closed || dirty.size === 0) return;
    const pending = [...dirty.entries()];
    dirty.clear();
    for (const [key, account] of pending) {
      try { writeAccount(key, account); }
      catch (error) { log(`DB-Fehler beim Speichern von ${key}:`, error.message); }
    }
  }

  function save(key, account) {
    if (closed || !key || !account) return;
    dirty.set(key, account);
    if (timer === null) timer = setTimeout(flush, 250);
  }

  function remove(key) {
    dirty.delete(key);
    statements.deleteAccount.run(key);
  }

  function stats() {
    let bytes = 0;
    try { bytes = fs.statSync(file).size; } catch { /* neu angelegt */ }
    return {
      accounts: statements.countAccounts.get().n,
      cards: statements.countCards.get().n,
      bytes
    };
  }

  function close() {
    flush();
    db.close();
  }

  /**
   * Verbucht ein Match für eine Deck-Zusammensetzung. Die Identität ist der
   * Hash über Held + sortierte Kartenlisten — gleiche Zusammensetzung heisst
   * gleiches Deck, egal wie es benannt ist oder wem es gehört. Der zuletzt
   * benutzte Name gewinnt die Anzeige.
   */
  function recordDeckResult({ name, hero, cards, extra, won, pvp }) {
    const main = [...(cards || [])].sort();
    const side = [...(extra || [])].sort();
    if (main.length === 0) return;
    const hash = crypto.createHash('sha1')
      .update((hero || '') + '|' + main.join(',') + '|' + side.join(','))
      .digest('hex');
    const grouped = list => {
      const counts = new Map();
      for (const card of list) counts.set(card, (counts.get(card) || 0) + 1);
      return [...counts.entries()].map(([n, c]) => ({ n, c }));
    };
    statements.upsertDeckStat.run(hash, name || '', hero || '',
      JSON.stringify(grouped(main)), JSON.stringify(grouped(side)),
      won ? 1 : 0, pvp ? 1 : 0, pvp && won ? 1 : 0, Date.now());

    // Karten-Statistik: jede Karte zählt pro Match einmal, Kopien egal.
    // Paare (alphabetisch normiert) tragen "often paired with" — und zählen
    // NUR Online-Matches, genau wie die Anzeige (Solo gegen Bots verzerrt).
    const now = Date.now();
    const distinct = [...new Set([...main, ...side])].sort();
    const winFlag = won ? 1 : 0;
    for (const card of distinct)
      statements.upsertCardStat.run(card, winFlag, pvp ? 1 : 0, pvp && won ? 1 : 0, now);
    if (pvp)
      for (let i = 0; i < distinct.length; i++)
        for (let j = i + 1; j < distinct.length; j++)
          statements.upsertCardPair.run(distinct[i], distinct[j], winFlag);
  }

  /** Die meistgespielten Karten (ein Match zählt je Karte einmal). */
  function topCards(limit = 100) {
    return statements.selectTopCards.all(limit).map(row => ({
      n: row.card, games: row.games, wins: row.wins,
      pvpGames: row.pvp_games, pvpWins: row.pvp_wins
    }));
  }

  /** Die häufigsten Deck-Partner einer Karte, samt gemeinsamer Bilanz. */
  function cardPartners(card, limit = 12) {
    return statements.selectPartners.all(card, card, card, limit).map(row => ({
      n: row.partner, games: row.games, wins: row.wins
    }));
  }

  /** Schreibt ein Spiel in die Historie eines Kontos (Profil: "Recent Matches"). */
  function recordMatch(account, { mode, opponent, deckName, won }) {
    statements.insertMatchLog.run(account, Date.now(), mode || 'solo',
      opponent || '', deckName || '', won ? 1 : 0);
  }

  /** Die letzten Spiele + Modus-Bilanzen eines Kontos. */
  function profileStats(account, limit = 20) {
    const matches = statements.selectRecentMatches.all(account, limit).map(row => ({
      ts: row.ts, mode: row.mode, opponent: row.opponent,
      deckName: row.deck_name, won: row.won === 1
    }));
    let pvpGames = 0, pvpWins = 0, soloGames = 0, soloWins = 0;
    for (const row of statements.countMatchStats.all(account)) {
      if (row.mode === 'pvp') { pvpGames = row.games; pvpWins = row.wins || 0; }
      else { soloGames += row.games; soloWins += row.wins || 0; }
    }
    return { matches, pvpGames, pvpWins, soloGames, soloWins };
  }

  /** Die meistgespielten Decks samt Kartenlisten, fürs Statistik-Panel im Client. */
  function topDecks(limit = 50) {
    return statements.selectTopDecks.all(limit).map(row => ({
      name: row.name, hero: row.hero,
      games: row.games, wins: row.wins,
      pvpGames: row.pvp_games, pvpWins: row.pvp_wins,
      cards: JSON.parse(row.cards), extra: JSON.parse(row.extra)
    }));
  }

  /**
   * Einmalige Übernahme aus der alten accounts.json. Läuft nur, solange die
   * Datenbank leer ist; danach wird die JSON aus dem Weg umbenannt.
   */
  function importLegacyJson(jsonFile) {
    if (statements.countAccounts.get().n > 0) return 0;
    if (!fs.existsSync(jsonFile)) return 0;

    let legacy;
    try { legacy = JSON.parse(fs.readFileSync(jsonFile, 'utf8')); }
    catch (error) { log('accounts.json unlesbar, Übernahme übersprungen:', error.message); return 0; }

    let count = 0;
    for (const [key, account] of Object.entries(legacy)) {
      if (!account || !account.name) continue;
      writeAccount(key, {
        name: account.name,
        salt: account.salt || '',
        hash: account.hash || '',
        coins: account.coins || 0,
        tokens: account.tokens || [0, 0, 0, 0],
        daily: account.daily || { streak: 0, lastClaim: 0 },
        decks: account.decks || [],
        packInv: account.packInv || {},
        steamId: account.steamId || null,
        collection: account.collection || {}
      });
      count++;
    }
    if (count > 0) {
      fs.renameSync(jsonFile, jsonFile + '.migrated');
      log(`accounts.json -> SQLite übernommen: ${count} Accounts (Original als .migrated gesichert)`);
    }
    return count;
  }

  return { loadAll, save, remove, flush, close, stats, importLegacyJson, file,
    recordDeckResult, topDecks, topCards, cardPartners, recordMatch, profileStats };
}
