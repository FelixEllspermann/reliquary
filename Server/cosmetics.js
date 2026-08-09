/*
 * Kosmetik. Zweiundvierzig Gegenstände in sieben Fächern — nichts davon berührt das Spiel.
 *
 * Eine einzige Währung: COINS. Die verdient man durch Spielen, und wer ein
 * Sonderexemplar zerlegt, bekommt ebenfalls Coins — es gibt also genau einen
 * Topf, aus dem alles bezahlt wird.
 *
 * Der Preis staffelt sich nach Seltenheit: die auffälligen Stücke kosten ein
 * Vielfaches der schlichten, statt in einer eigenen Währung zu stehen.
 *
 * Pro Fach bleibt mindestens ein Gegenstand unverkäuflich — das macht den Rest
 * des Ladens glaubwürdig.
 */

export const SLOTS = ['sleeve', 'avatarFrame', 'avatar', 'tossCoin', 'duelMat', 'title', 'victorySeal'];

/** Anzeigenamen der Fächer, in der Reihenfolge von SLOTS. */
export const SLOT_NAMES = {
  sleeve: 'Card sleeve',
  avatarFrame: 'Avatar frame',
  avatar: 'Profile picture',
  tossCoin: 'Toss coin',
  duelMat: 'Duel mat',
  title: 'Profile title',
  victorySeal: 'Victory seal',
};

/**
 * price/currency: 'coins' oder null (nicht käuflich).
 * unlock beschreibt, wie man einen unverkäuflichen Gegenstand bekommt.
 */
export const ITEMS = [
  // --- Kartenrücken (8) ---
  // Drei Achsen halten sie auseinander: Webrichtung (diagonal / waagerecht /
  // orthogonal / keine), Helligkeit und Mittelmotiv. Zwei Rücken dürfen nie in
  // allen dreien übereinstimmen.
  { id: 'ashen_weave',      slot: 'sleeve',      name: 'Ashen Weave',      rarity: 'common', price: 600,  currency: 'coins' },
  { id: 'deep_current',     slot: 'sleeve',      name: 'Deep Current',     rarity: 'rare',   price: 1600, currency: 'coins' },
  { id: 'chainbound',       slot: 'sleeve',      name: 'Chainbound',       rarity: 'rare',   price: 1700, currency: 'coins' },
  { id: 'obsidian_lattice', slot: 'sleeve',      name: 'Obsidian Lattice', rarity: 'rare',   price: 1800, currency: 'coins' },
  { id: 'tomb_gilt',        slot: 'sleeve',      name: 'Tomb Gilt',        rarity: 'epic',   price: 2800, currency: 'coins' },
  { id: 'cartogram',        slot: 'sleeve',      name: 'Cartogram',        rarity: 'epic',   price: 3000, currency: 'coins' },
  { id: 'split_seal',       slot: 'sleeve',      name: 'Split Seal',       rarity: 'relic',  price: 4600, currency: 'coins' },
  { id: 'static_bloom',     slot: 'sleeve',      name: 'Static Bloom',     rarity: 'relic',  price: 4800, currency: 'coins' },

  // --- Spielmatten (6) ---
  // Jede Matte behält dieselben Möbel — Mittellinie, fünf Zonen je Seite,
  // Mittelmarke — damit das Brett lesbar bleibt. Nur die Behandlung ändert sich.
  { id: 'stone_table',      slot: 'duelMat',     name: 'Stone Table',      rarity: 'common', price: 800,  currency: 'coins' },
  { id: 'tidal_floor',      slot: 'duelMat',     name: 'Tidal Floor',      rarity: 'rare',   price: 1700, currency: 'coins' },
  { id: 'ember_circle',     slot: 'duelMat',     name: 'Ember Circle',     rarity: 'rare',   price: 1800, currency: 'coins' },
  { id: 'starless_vault',   slot: 'duelMat',     name: 'Starless Vault',   rarity: 'epic',   price: 2900, currency: 'coins' },
  { id: 'foundry_grate',    slot: 'duelMat',     name: 'Foundry Grate',    rarity: 'epic',   price: 3100, currency: 'coins' },
  { id: 'cathedral_plate',  slot: 'duelMat',     name: 'Cathedral Plate',  rarity: 'relic',  price: 4700, currency: 'coins' },

  // --- Wurfmünzen (5) ---
  // Jede Münze trägt zwei VERSCHIEDENE Zeichen. Der Wurf zeigt beide Seiten,
  // bevor er landet — wer sie nicht auseinanderhält, kann den Flug nicht lesen.
  { id: 'bone_token',       slot: 'tossCoin',    name: 'Bone Token',       rarity: 'common', price: 650,  currency: 'coins' },
  { id: 'copper_trial',     slot: 'tossCoin',    name: 'Copper Trial',     rarity: 'common', price: 700,  currency: 'coins' },
  { id: 'silver_warden',    slot: 'tossCoin',    name: 'Silver Warden',    rarity: 'rare',   price: 1600, currency: 'coins' },
  { id: 'molten_bit',       slot: 'tossCoin',    name: 'Molten Bit',       rarity: 'epic',   price: 2900, currency: 'coins' },
  { id: 'vault_coin',       slot: 'tossCoin',    name: 'Vault Coin',       rarity: 'relic',  price: 4400, currency: 'coins' },

  // --- Profilrahmen (11) ---
  // Müssen auf 44 px in einer Bestenliste überleben: das Erkennungsmerkmal sitzt
  // in der Silhouette oder im Rand, nie im Detail.
  { id: 'iron_bracket',     slot: 'avatarFrame', name: 'Iron Bracket',     rarity: 'common', price: 700,  currency: 'coins' },
  { id: 'amber_halo',       slot: 'avatarFrame', name: 'Amber Halo',       rarity: 'rare',   price: 1700, currency: 'coins' },
  { id: 'thorn_setting',    slot: 'avatarFrame', name: 'Thorn Setting',    rarity: 'rare',   price: 1800, currency: 'coins' },
  { id: 'gilded_reliquary', slot: 'avatarFrame', name: 'Gilded Reliquary', rarity: 'epic',   price: 2800, currency: 'coins' },
  { id: 'prism_mount',      slot: 'avatarFrame', name: 'Prism Mount',      rarity: 'epic',   price: 3000, currency: 'coins' },
  { id: 'vault_ring',       slot: 'avatarFrame', name: 'Vault Ring',       rarity: 'relic',  price: 4600, currency: 'coins' },

  // Die gemalte Reihe: gerahmte Bilder statt Ringe. Von Efeu gehalten, von
  // Flammen gefressen, von Blitzen gespannt, von Panzerhandschuhen getragen,
  // von Schwingen entfuehrt — die Steigerung sitzt in der Dramatik.
  { id: 'rootbound',        slot: 'avatarFrame', name: 'Rootbound',        rarity: 'rare',   price: 1650, currency: 'coins' },
  { id: 'gilded_grasp',     slot: 'avatarFrame', name: 'Gilded Grasp',     rarity: 'rare',   price: 1750, currency: 'coins' },
  { id: 'stormlace',        slot: 'avatarFrame', name: 'Stormlace',        rarity: 'epic',   price: 2850, currency: 'coins' },
  { id: 'pyre_mantle',      slot: 'avatarFrame', name: 'Pyre Mantle',      rarity: 'epic',   price: 3100, currency: 'coins' },
  { id: 'fiendwing',        slot: 'avatarFrame', name: 'Fiendwing',        rarity: 'relic',  price: 4900, currency: 'coins' },

  // --- Profilbilder (4) ---
  // Monster aus dem Spiel als Portraits — wer den Sumpf spielt, zeigt die
  // Kroete. Ohne Profilbild bleibt die Initiale auf dunkler Kachel.
  { id: 'mireback_toad',    slot: 'avatar',      name: 'Mireback Toad',    rarity: 'common', price: 750,  currency: 'coins' },
  { id: 'ember_imp',        slot: 'avatar',      name: 'Ember Imp',        rarity: 'rare',   price: 1600, currency: 'coins' },
  { id: 'gravemaw_whelp',   slot: 'avatar',      name: 'Gravemaw Whelp',   rarity: 'rare',   price: 1800, currency: 'coins' },
  { id: 'bone_colossus',    slot: 'avatar',      name: 'Bone Colossus',    rarity: 'epic',   price: 2900, currency: 'coins' },

  // --- Siegessiegel (5) ---
  // Die sozialste Kosmetik im Spiel: beide Spieler sehen sie. Sie unterscheiden
  // sich darin, WIE sie ankommen — gebrannt, gebrochen, geöffnet, geschlagen,
  // verfinstert.
  { id: 'brand',            slot: 'victorySeal', name: 'Brand',            rarity: 'common', price: 850,  currency: 'coins' },
  { id: 'shatter',          slot: 'victorySeal', name: 'Shatter',          rarity: 'rare',   price: 1600, currency: 'coins' },
  { id: 'bloom',            slot: 'victorySeal', name: 'Bloom',            rarity: 'epic',   price: 3000, currency: 'coins' },
  { id: 'verdict',          slot: 'victorySeal', name: 'Verdict',          rarity: 'epic',   price: 3200, currency: 'coins' },
  { id: 'eclipse',          slot: 'victorySeal', name: 'Eclipse',          rarity: 'relic',  price: 4400, currency: 'coins' },

  // --- Titel ---
  { id: 'sealbreaker',      slot: 'title',       name: 'Sealbreaker',      rarity: 'common', price: 900,  currency: 'coins' },
  { id: 'ash_collector',    slot: 'title',       name: 'Ash Collector',    rarity: 'rare',   price: 1400, currency: 'coins' },
  { id: 'wardens_bane',     slot: 'title',       name: "Warden's Bane",    rarity: 'relic',  price: null, currency: null,
    unlock: 'Beat the Warden without losing a Reliquary.' },

  // --- Turm-Titel (nur ueber The Tower, Ebenen 1/10/15) ---
  { id: 'tower_initiate',   slot: 'title',       name: 'Tower Initiate',   rarity: 'common', price: null, currency: null,
    unlock: 'Renew the first seal of the Tower.' },
  { id: 'renewer_of_seals', slot: 'title',       name: 'Renewer of Seals', rarity: 'rare',   price: null, currency: null,
    unlock: 'Renew ten seals of the Tower.' },
  { id: 'towers_answer',    slot: 'title',       name: "The Tower's Answer", rarity: 'relic', price: null, currency: null,
    unlock: 'Reach the top of the Tower.' },

  // --- Draft-Titel (nur ueber den Draft-Turm der Challenges) ---
  { id: 'draft_sovereign',  slot: 'title',       name: 'Draft Sovereign',  rarity: 'relic',  price: null, currency: null,
    unlock: 'Conquer the Tower with a drafted deck.' },
];

/** Jeder Early-Access-Spieler startet mit diesem Titel. */
export const STARTER_TITLE = 'early_vault_hunter';

const BY_ID = new Map(ITEMS.map(item => [item.id, item]));

export function byId(id) {
  return BY_ID.get(String(id || '')) || null;
}

/** Gehört der Gegenstand dem Account? Startgegenstände zählen immer. */
export function owns(acc, id) {
  if (id === STARTER_TITLE) return true;
  return Array.isArray(acc.cosmetics) && acc.cosmetics.includes(id);
}

/**
 * Kauft einen Gegenstand. Gibt null zurück, wenn es geklappt hat, sonst den
 * Grund als Text — der Server schickt ihn unverändert an den Client.
 */
export function buy(acc, id) {
  const item = byId(id);
  if (!item) return 'Unknown item.';
  if (owns(acc, id)) return 'You already own this.';
  if (item.currency !== 'coins') return item.unlock ? `Not for sale — ${item.unlock}` : 'Not for sale.';

  if ((acc.coins | 0) < item.price) return `Not enough coins (${item.price} needed).`;
  acc.coins -= item.price;

  if (!Array.isArray(acc.cosmetics)) acc.cosmetics = [];
  acc.cosmetics.push(id);
  return null;
}

/** Rüstet einen Gegenstand aus. Leere id räumt das Fach. */
export function equip(acc, slot, id) {
  if (!SLOTS.includes(slot)) return 'Unknown slot.';
  if (!acc.equipped || typeof acc.equipped !== 'object') acc.equipped = {};

  if (!id) { delete acc.equipped[slot]; return null; }

  // Der Startertitel liegt in keinem Katalogfach, gehört aber ins Titelfach
  if (id === STARTER_TITLE) {
    if (slot !== 'title') return 'That item does not fit this slot.';
    acc.equipped[slot] = id;
    return null;
  }

  const item = byId(id);
  if (!item) return 'Unknown item.';
  if (item.slot !== slot) return 'That item does not fit this slot.';
  if (!owns(acc, id)) return 'You do not own this.';
  acc.equipped[slot] = id;
  return null;
}

/** Alles, was der Client über die Kosmetik eines Accounts wissen muss. */
export function stateOf(acc) {
  const owned = [STARTER_TITLE, ...(Array.isArray(acc.cosmetics) ? acc.cosmetics : [])];
  const equipped = acc.equipped && typeof acc.equipped === 'object' ? acc.equipped : {};
  return {
    cosmeticsOwned: owned,
    equippedSlots: SLOTS,
    equippedIds: SLOTS.map(slot => equipped[slot] || ''),
  };
}

/** Der Katalog für den Shop — einmal beim Login mitgeschickt. */
export function catalog() {
  return {
    shopIds: ITEMS.map(i => i.id),
    shopNames: ITEMS.map(i => i.name),
    shopSlots: ITEMS.map(i => i.slot),
    shopRarities: ITEMS.map(i => i.rarity),
    shopPrices: ITEMS.map(i => i.price ?? -1),
    shopCurrencies: ITEMS.map(i => i.currency || ''),
    shopUnlocks: ITEMS.map(i => i.unlock || ''),
  };
}
