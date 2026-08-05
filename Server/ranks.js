/*
 * Rangleiter: zehn Siegel zu je fünf Unterstufen, insgesamt fünfzig Schritte.
 *
 * Zwei Regeln machen das Ganze aus:
 *  - Unter Gold Seal steigt man schneller als man fällt. Wer bei 50 % Siegquote
 *    spielt, kommt trotzdem voran; ab Gold ist es symmetrisch und ehrlich.
 *  - Aus einem einmal erreichten Hauptrang fällt man nicht wieder heraus.
 *    Unterstufen darf man verlieren, den Rang nicht — bis zum Saisonende.
 *
 * Das Handoff schneidet Vault Seal nach Leaderboard-Platz. Es gibt (noch) kein
 * Leaderboard, also läuft die Stufe hier in RP weiter: Vault Seal V ist die
 * Spitze und hat nach oben keine Grenze.
 */

export const RANKS = [
  { rank: 1,  name: 'Ash Seal',      from: 0,    width: 400 },
  { rank: 2,  name: 'Clay Seal',     from: 400,  width: 400 },
  { rank: 3,  name: 'Copper Seal',   from: 800,  width: 400 },
  { rank: 4,  name: 'Iron Seal',     from: 1200, width: 400 },
  { rank: 5,  name: 'Silver Seal',   from: 1600, width: 500 },
  { rank: 6,  name: 'Gold Seal',     from: 2100, width: 500 },
  { rank: 7,  name: 'Obsidian Seal', from: 2600, width: 600 },
  { rank: 8,  name: 'Amber Seal',    from: 3200, width: 600 },
  { rank: 9,  name: 'Relic Seal',    from: 3800, width: 700 },
  { rank: 10, name: 'Vault Seal',    from: 4500, width: 700 },   // offen nach oben
];

const GOLD = 6;                 // ab hier symmetrische Wertung
const SEASON_DROP_RANKS = 2;    // weicher Reset am Saisonende: zwei volle Ränge

/** Untergrenze eines Rangs in RP. */
export function floorOf(rank) {
  const entry = RANKS[Math.min(Math.max(rank, 1), RANKS.length) - 1];
  return entry.from;
}

/** Rang und Unterstufe zu einem RP-Stand. Stufe 1 ist die unterste, 5 die höchste. */
export function rankFor(rp) {
  const points = Math.max(0, Math.floor(rp || 0));
  let entry = RANKS[0];
  for (const candidate of RANKS) if (points >= candidate.from) entry = candidate;

  const step = entry.width / 5;
  let tier = Math.floor((points - entry.from) / step) + 1;
  if (tier > 5) tier = 5;       // Vault Seal V hat keine Obergrenze
  if (tier < 1) tier = 1;
  return { rank: entry.rank, name: entry.name, tier, rp: points };
}

/** RP-Änderung für ein beendetes Duell. */
export function rpDelta(rank, won) {
  if (rank >= GOLD) return won ? 25 : -25;
  // Unter Gold: Aufstieg schlägt Abstieg. Deterministisch statt zufällig, damit
  // zwei Spieler mit gleicher Bilanz auch gleich weit kommen.
  return won ? 22 : -17;
}

/**
 * Wendet ein Duellergebnis auf einen Account an und gibt an, was passiert ist.
 * Der Boden wird hier durchgesetzt — niemals im Client.
 */
export function applyResult(acc, won) {
  const before = rankFor(acc.rp);
  const delta = rpDelta(before.rank, won);

  let next = Math.max(0, (acc.rp || 0) + delta);

  // Boden: nicht unter den höchsten je erreichten Hauptrang dieser Saison
  const guard = floorOf(acc.peakRank || 1);
  if (next < guard) next = guard;

  acc.rp = next;
  const after = rankFor(next);
  if (after.rank > (acc.peakRank || 1)) acc.peakRank = after.rank;

  acc.careerRp = Math.max(0, (acc.careerRp || 0) + Math.max(0, delta));
  if (won) { acc.wins = (acc.wins || 0) + 1; acc.streak = (acc.streak || 0) + 1; }
  else { acc.losses = (acc.losses || 0) + 1; acc.streak = 0; }
  if ((acc.streak || 0) > (acc.bestStreak || 0)) acc.bestStreak = acc.streak;

  return {
    delta: next - (before.rp),
    before,
    after,
    promoted: after.rank > before.rank || (after.rank === before.rank && after.tier > before.tier),
    rankUp: after.rank > before.rank,
  };
}

/** Kennung der laufenden Saison — ein Monat, z.B. "2026-08". */
export function currentSeason(now = new Date()) {
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, '0')}`;
}

/**
 * Setzt den Account auf die laufende Saison. Beim Wechsel fällt man um einen
 * vollen Rang zurück und die Bestenschutz-Marke wird neu gesetzt — das ist der
 * einzige Moment, in dem man einen Hauptrang verlieren kann.
 */
export function rolloverIfNeeded(acc, now = new Date()) {
  const season = currentSeason(now);
  if (acc.season === season) return null;

  const previous = acc.season ? rankFor(acc.rp) : null;
  if (previous) {
    const droppedRank = Math.max(1, previous.rank - SEASON_DROP_RANKS);
    acc.rp = floorOf(droppedRank) + Math.round((RANKS[droppedRank - 1].width / 5) * (previous.tier - 1));
    acc.peakRank = droppedRank;
  } else {
    acc.rp = 0;
    acc.peakRank = 1;
  }
  acc.season = season;
  return previous ? { from: previous, to: rankFor(acc.rp) } : null;
}

/** Rang-Block für das Profil. */
export function rankInfo(acc) {
  const seal = rankFor(acc.rp);
  const entry = RANKS[seal.rank - 1];
  const step = entry.width / 5;
  const tierFloor = entry.from + step * (seal.tier - 1);
  const nextAt = seal.rank === 10 && seal.tier === 5 ? null : tierFloor + step;
  return {
    rank: seal.rank,
    tier: seal.tier,
    name: seal.name,
    rp: seal.rp,
    tierFloor,
    nextAt,
    season: acc.season || currentSeason(),
    wins: acc.wins || 0,
    losses: acc.losses || 0,
    bestStreak: acc.bestStreak || 0,
  };
}

