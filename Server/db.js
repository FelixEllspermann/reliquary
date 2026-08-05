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

  db.prepare('INSERT OR REPLACE INTO meta(key, value) VALUES(?, ?)')
    .run('schema_version', String(SCHEMA_VERSION));

  const statements = {
    selectAccounts: db.prepare('SELECT * FROM accounts'),
    selectCollection: db.prepare('SELECT account, card, finish, count FROM collection'),
    upsertAccount: db.prepare(`
      INSERT INTO accounts (key, name, salt, hash, coins, tokens, daily, decks, pack_inv, steam_id, rank, cosmetics, created, updated)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      ON CONFLICT(key) DO UPDATE SET
        name = excluded.name, salt = excluded.salt, hash = excluded.hash,
        coins = excluded.coins, tokens = excluded.tokens, daily = excluded.daily,
        decks = excluded.decks, pack_inv = excluded.pack_inv, steam_id = excluded.steam_id,
        rank = excluded.rank, cosmetics = excluded.cosmetics, updated = excluded.updated
    `),
    clearCollection: db.prepare('DELETE FROM collection WHERE account = ?'),
    insertCard: db.prepare('INSERT INTO collection (account, card, finish, count) VALUES (?, ?, ?, ?)'),
    deleteAccount: db.prepare('DELETE FROM accounts WHERE key = ?'),
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
        ...parse(row.rank, {}),      // rp, peakRank, season, wins, losses, streak, bestStreak, careerRp
        ...parse(row.cosmetics, {})  // cosmetics, equipped (ein altes shards-Feld wird ignoriert)
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

  return { loadAll, save, remove, flush, close, stats, importLegacyJson, file };
}
