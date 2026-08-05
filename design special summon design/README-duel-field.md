# Handoff: Duel Field (game board)

Companion document to `README.md` (the card system). Same visual language — **Reliquary**.
Read the card README first for the type palettes, typography and the card anatomy; this
document only covers the board and the reduced card renditions that live on it.

## Overview
The in-game duel board: two mirrored player halves, an inspector rail on the left, a status
rail on the right. Designed at a fixed **1920 × 1080** and intended to scale uniformly to the
viewport (letterbox, do not reflow).

## About the Design Files
`Duel Field.dc.html` is a **design reference created in HTML** — a static prototype of one
game state. It is not production code. Recreate it in the target engine (Unity UGUI, Godot,
Unreal UMG, React, …) with that engine's layout system. Everything drawn here with CSS
gradients and `clip-path` should become sprites / 9-slices in an engine.

## Fidelity
**High fidelity.** All sizes, colours and type are final. Placeholder content: the diamond
glyphs standing in for card artwork on the field and in hand, and all sample card names,
stats and log lines.

---

## Global geometry

| | Value |
|---|---|
| Board | 1920 × 1080, `overflow: hidden` |
| Left rail (Inspector) | 344 wide, padding `26px 24px`, gap 14, `border-right: 1px rgba(200,164,92,.25)` |
| Right rail (Status) | 344 wide, padding `26px 24px`, gap 16, `border-left: 1px rgba(200,164,92,.25)` |
| Centre column | 1232 wide (flex), padding `24px 0`, `flex-direction: column; justify-content: space-between` |

### Background stack (bottom → top)
1. `radial-gradient(ellipse 1500px 760px at 50% 50%, #241811, #0B0705 76%)` — the table.
2. Weave overlay: two `repeating-linear-gradient`s at ±45°,
   `rgba(200,164,92,.045) 0 1px, transparent 1px 26px` — same motif as the card back, 3×
   coarser. `pointer-events: none`.
3. **Opponent tint** — top 470px, `linear-gradient(180deg, rgba(40,62,86,.5), transparent)` (cool).
4. **Player tint** — bottom 430px, `linear-gradient(0deg, rgba(96,52,18,.34), transparent)` (warm).

Steps 3 and 4 are the primary orientation cue: **your half is warm, the enemy half is cold.**
Never remove them; colour-blind users still get the split from the value difference.

Both rails sit on `linear-gradient(90deg, rgba(10,7,5,.92), rgba(10,7,5,.55))` (mirrored
to `270deg` on the right) so the table reads as continuing underneath them.

### Centre column stack (top → bottom)
| Block | Height |
|---|---|
| Opponent hand | 88 |
| Opponent field block | 324 |
| Phase divider | auto (~40) |
| Player field block | 324 |
| Player hand | 172 |

Distributed with `justify-content: space-between` inside the 1032px padded column.

---

## The field block

One per side. `display: flex; gap: 18; align-items: center` — **868 wide** total.

```
[ player-card slot 112 ] 18 [ zone grid 608 ] 18 [ pile column 112 ]
```

### Zone grid
Two rows of five, `gap: 12` horizontally, `gap: 10` between rows.
Every zone is **112 × 157** (2.5 : 3.5 — the card ratio), `border-radius: 5`, `box-sizing: border-box`.

Grid width = 5 × 112 + 4 × 12 = **608**. Block height = 157 + 10 + 157 = **324**.

**Row order is mirrored.** The monster row is always the row *closest to the centre line*:

| Side | Outer row | Inner row (at the centre line) |
|---|---|---|
| Opponent | Support | Monster |
| Player | Monster | Support |

**Support row composition (both sides, left → right):** `Spell · Spell · Spell · Artifact · Artifact`.

### Zone states

| State | Border | Fill | Content |
|---|---|---|---|
| Empty — Monster | 1px dashed `rgba(200,164,92,.35)` | `rgba(18,11,6,.55)` | caption `MONSTER` |
| Empty — Spell | 1px dashed `rgba(143,198,210,.4)` | `rgba(6,16,20,.55)` | caption `SPELL` |
| Empty — Artifact | 1px dashed `rgba(185,163,224,.4)` | `rgba(12,8,20,.55)` | caption `ARTIFACT` |
| **Valid drop target** | 1.5px solid `rgba(200,164,92,.9)` | `linear-gradient(165deg,#2A1D0E,#120C06)` | 16px gold diamond (`#EBCE8A`, glow `0 0 18px`) + `DROP HERE`; zone glow `0 0 26px rgba(200,164,92,.35)` |
| Occupied | — | — | field card (below) |
| Set / face-down | 1px solid `rgba(200,164,92,.5)` | card-back weave | `SET` caption, 8px, bottom 7 |

Zone captions: **Oswald 500, 9px, letter-spacing .20em**, centred, colour = the zone's accent
at ~.60 alpha. They are a hint layer — hide them once the player knows the board
(`zoneLabels` toggle in the prototype).

Face-down / set card: the card-back weave at zone scale — base
`radial-gradient(ellipse at 50% 50%, #4E2A18, #1C0E08 78%)`, ±45° hairlines
`rgba(200,164,92,.15) 0 1px, transparent 1px 13px`, centre diamond 46 × 46 rotated 45°,
1px `rgba(200,164,92,.55)`.

### Pile column
Two stacked 112 × 157 piles, `gap: 10` — so the column matches the two-row grid exactly.

**Order is mirrored so both graveyards sit next to the centre line:**
opponent = `BANISHED` above `GRAVEYARD`; player = `GRAVEYARD` above `BANISHED`.

| Pile | Border | Fill | Label | Count |
|---|---|---|---|---|
| Graveyard | 1px `rgba(140,150,165,.4)` | `rgba(28,32,42,.75)` → `rgba(10,12,16,.75)` | `#8F99A8` | Cinzel 700 30px `#C3CBD6` |
| Banished | 1px `rgba(224,96,58,.45)` | `rgba(58,20,12,.7)` → `rgba(18,8,5,.7)` | `#C97A5C` | Cinzel 700 30px `#E9A183` |

Labels: **Oswald 500, 9px, letter-spacing .24em**, above the count, gap 6.

### Player-card slot
112 × 157, on the outer flank. Three bands:
- name band **24** high, `linear-gradient(180deg,#42301C,#22150A)`, Cinzel 600 10px;
- body: 52 × 52 diamond, 1px keyline @55%, fill `linear-gradient(135deg, keyline@22%, transparent 65%)`;
- footer band **20** high, `rgba(0,0,0,.4)`, `PLAYER CARD`, Oswald 500 9px / .20em.

Player's own slot: **2px solid `#C8A45C`** plus `0 0 22px rgba(200,164,92,.18)`.
Opponent's: **1px solid `rgba(143,198,210,.6)`**, cool palette throughout.

---

## Reduced card renditions

Three sizes exist. All keep the same reading order as the full card — **name → art → meta →
stats** — and all use the card type's keyline colour.

### A · Field card — 112 × 157
`padding: 3`, 1.5px solid keyline, radius 5.

| Band | Height | Type |
|---|---|---|
| Name plate | 17 | Cinzel 600 8px, ink = `nameInk`, 1px keyline top+bottom, padding `0 5`, ellipsis |
| Artwork | 105 | 1px keyline @ 40–50%, `object-fit: cover` |
| Meta | 11 | Oswald 500 7px / .10em — 5 × 5 attribute pip + attribute left, type right |
| Stats | 18 | two boxes, gap 3, `rgba(0,0,0,.45)`, Cinzel 700 10px; DMG box full keyline, DEF box keyline @ 30–40% |

Level crest: 20 × 22, absolutely positioned `right: -5; top: -6` (deliberately breaks the
frame so it stays readable when zones are adjacent). Same hexagon `clip-path` and two-layer
build as the full card, 1.5px inlay, numeral Cinzel 700 11px.

### B · Hand card — 120 × 168
`padding: 4`, 1.5px solid keyline, radius 6, `box-shadow: 0 12px 28px rgba(0,0,0,.7)`.

| Band | Height |
|---|---|
| Name plate | 19 (Cinzel 600 9px) |
| Artwork | 110 (margin-top 3) |
| Meta | 12 |
| Stats | 19 |

Crest 22 × 24 at `right: -6; top: -7`, numeral 12px.

**Spell / Artifact hand cards have no meta or stat band.** Artwork is followed by a
**parchment footer** (`#EBE1C7` → `#D9CCAB`, 1px in the type's `effectBorder`) filling the
remaining height, carrying the subtype in **Oswald 600 8px / .18em** — the same rule as the
full card, where Spell drops the stat row.

### C · Focused hand card — 120 × 184
The hovered / selected card. Same build, raised: `margin-bottom: 14`, 2px solid `#EBCE8A`,
`box-shadow: 0 18px 36px rgba(0,0,0,.8), 0 0 28px rgba(235,206,138,.32)`.
Bands grow to 21 / 120 / 13 / rest; crest 24 × 26, numeral 13px.

### D · Card back
Hand backs **62 × 87**, radius 4, 1px `rgba(200,164,92,.55)`. Same back artwork as the
printed card, weave pitch scaled to **9px**, centre diamond 26 × 26.
At zone size (112 × 157) the weave pitch is **13px** and the diamond 46 × 46.

---

## Hands

| | Card | Pitch | Wrapper |
|---|---|---|---|
| Opponent | 62 × 87 back | **48** (`margin-left: -14`, wrapper `padding-left: 14`) | height 88, `align-items: flex-start` |
| Player | 120 × 168 | **100** (`margin-left: -20`, wrapper `padding-left: 20`) | height 172, `align-items: flex-end` |

**Overlap rule: never cover a hand card's name plate.** The visible strip must stay wider
than the longest name at that size — that is what sets the 100px player pitch. If the hand
grows past ~9 cards, reduce the pitch to a floor of 76px and then scroll/fan-arc rather than
overlapping further.

---

## Phase divider

Full-width row, `padding: 0 60`, `gap: 20`, sitting on the centre line between the halves.
- Left / right: 1px rules fading to `rgba(200,164,92,.55)` at the centre.
- Centre group, gap 12: 10px gold diamond · **Cinzel 600 15px, letter-spacing .14em, `#EBCE8A`**
  (`YOUR MAIN PHASE`) · **Spectral 400 12px, `#8E8371`** (instruction) · 10px gold diamond.

This is the only place the current phase is stated on the board itself; the rail repeats it
as chips.

---

## Left rail — Inspector

1. **Header** — 9px gold diamond + `INSPECT` (Oswald 500 11px / .26em, `#9C8A6A`) + a 1px rule
   fading out.
2. **Card viewport** — 288 × 404, `overflow: hidden`. Inside it the **full 480 × 672 card**
   at `transform: scale(.6); transform-origin: top left`.
   **Do not build a second card layout for the inspector** — render the real card scaled, so
   the inspector can never drift from the printed card. 0.6 is the only scale in use.
3. **Tag chips** — gap 6, padding `4px 9px`, Oswald 10px / .16em. Rarity chip uses the gold
   badge gradient; the rest are `rgba(0,0,0,.45)` with a 1px tinted border.
4. **Ability block** — parchment panel (`#EBE1C7` → `#D9CCAB`, 1px `#8C7440`), padding
   `11px 13px`. Header row: ability name (Oswald 600 12px / .20em, `#5C4A1E`) and the mana
   cost right (8px diamond `#B8442A` + Oswald 600 11px `#7A5A1E`). Body Spectral 400 12/1.45
   `#2E2417`.
5. **Zone legend** — pinned to the bottom (`margin-top: auto`). 16 × 11 swatches in the four
   zone accents + Spectral 12px labels.

Empty state: keep the 288 × 404 viewport reserved and show the card back at 0.6 with a
`SELECT A CARD` caption — the rail must not change width or the board will jump.

---

## Right rail — Status

Rail gap 16. Panels: padding `14px 16px`, radius 6, internal gap 11.

### Player / opponent status panel
| Element | Spec |
|---|---|
| Panel (opponent) | `rgba(24,32,42,.85)` → `rgba(8,11,15,.85)`, 1px `rgba(143,198,210,.4)` |
| Panel (player) | `rgba(46,32,18,.9)` → `rgba(14,9,5,.9)`, 1px `#C8A45C`, `0 0 24px rgba(200,164,92,.12)` |
| Deck name | Cinzel 600 17px |
| Side badge | `FOE` / `YOU`, padding `3px 7px`, Oswald 500 9px / .16em, tinted fill + 1px border |
| LP | label Oswald 500 10px / .22em; value **Cinzel 700 36px** |
| Mana | `MANA` label + 11 × 11 diamonds rotated 45°, gap 6. Spent pips drop to `rgba(…, .22)` with no glow; the player's available pips carry `0 0 9px` |
| LP bar | height 5, `rgba(0,0,0,.5)` + 1px tinted border, fill `linear-gradient(90deg, dark, light)` |
| Counters | 4-column grid, gap 8 — `DECK · HAND · GY · BAN`; label Oswald 500 9px / .16em, value Cinzel 600 16px |

### Turn panel
`rgba(0,0,0,.4)`, 1px `rgba(200,164,92,.35)`. `Turn N` in Cinzel 600 18px `#EBCE8A`, active
player's name right in Spectral 13px `#9C8A6A`.
Phase chips: 4 equal cells, gap 5, padding `6px 0`, Oswald 9px / .12em.
Inactive `rgba(0,0,0,.4)` + 1px `rgba(200,164,92,.2)`, ink `#6A5E4A`.
Active: gold gradient `#E2C685` → `#9C7526`, 1px `#EBCE8A`, ink `#1E1405`.
Order: `DRAW · MAIN · BATTLE · END`.

### End Turn button
Full width, padding `15px 0`, radius 5, 2px `#EBCE8A`, fill `#E2C685` → `#9C7526`,
**Cinzel 600 17px, letter-spacing .10em**, ink `#1E1405`,
`box-shadow: 0 8px 22px rgba(0,0,0,.55), inset 0 1px 0 rgba(255,255,255,.35)`.
Hover: fill `#F3DDA4` → `#B08829` plus `0 0 26px rgba(235,206,138,.4)`.
Disabled (not your turn): fill `rgba(0,0,0,.4)`, 2px `rgba(200,164,92,.25)`, ink `#6A5E4A`,
no shadow.

### Duel log
Parchment panel, `flex: 1; min-height: 0`, padding `13px 15px`, radius 4, 1px `#8C7440`.
- Header `DUEL LOG` — Oswald 600 10px / .24em `#5C4A1E` + a 1px rule.
- Entries — Spectral 400 12/1.45 `#4A3E2A`, gap 7. Card and player names inside an entry are
  weight 600 `#2E2417`.
- Turn separator — Oswald 600 11px / .14em `#7A5A1E`, e.g. `— TURN 1 · PYRO STARTER · MANA 3 —`.
- Newest entry at the bottom; auto-scroll to bottom; keep the last ~50 entries.

---

## Interactions & Behavior

- **Hover a hand card** → the focused rendition (C): rises 14px, grows to 184, gold keyline
  and glow. 120 ms `ease-out`. The inspector updates to that card at the same time.
- **Pick up / drag** → the card follows the cursor at hand-card size, opacity .92, tilt
  `rotate(-3deg)`; every legal target zone switches to the drop-target state simultaneously.
- **Illegal drop** → zone flashes `rgba(224,96,58,.5)` for 180 ms, card returns to hand with
  a 200 ms ease-out.
- **Summon** → the card scales from hand size to field size over 240 ms
  `cubic-bezier(.2,.7,.3,1)` and lands with a one-shot gold ring at the zone.
- **Attack** → attacker lunges 18px toward the target and back, 260 ms; damage floats up from
  the defending LP counter in Cinzel 700 28px.
- **LP change** → the numeral counts, not cuts (~600 ms); the LP bar eases over the same
  duration; damage tints the numeral `#E0603A` for 400 ms.
- **Phase change** → the active chip crossfades (140 ms) and the centre divider text swaps.
- **Not your turn** → the whole player half loses its warm tint, End Turn is disabled, hand
  cards do not lift.
- **Inspector** follows hover; on click it *locks* to the clicked card (add a small
  `PINNED` chip) until another click or Escape.
- **Responsive:** uniform scale of the whole 1920 × 1080 board. Do not reflow the rails; below
  ~1280px logical width, collapse the left rail to a hover-only overlay rather than shrinking
  the field.

## State Management

```ts
type ZoneKind = 'monster' | 'spell' | 'artifact';
type Phase    = 'draw' | 'main' | 'battle' | 'end';

interface Zone {
  kind: ZoneKind;
  card?: Card;             // see the card README
  faceDown: boolean;       // renders the SET back
  position: 'attack' | 'defense';
}

interface Side {
  deckName: string;
  lp: number; lpMax: number;
  mana: number; manaMax: number;   // 0-3, drawn as diamonds
  deck: number; hand: Card[];
  graveyard: Card[]; banished: Card[];
  playerCard?: Card;
  zones: Zone[];           // 10: 5 monster + 3 spell + 2 artifact
}

interface Duel {
  you: Side; foe: Side;
  turn: number; activeSide: 'you' | 'foe'; phase: Phase;
  log: LogEntry[];
  ui: { inspected?: Card; pinned: boolean; dragging?: Card; legalZones: string[] };
}
```

`ui.legalZones` is what drives the drop-target state; it should be computed by the rules
engine when a drag starts, never by the view.

## Design Tokens (board-specific)

Card-type palettes, attribute pips and the type scale all come from the card README. New here:

| Token | Value |
|---|---|
| Table base | `#241811` → `#0B0705` |
| Table weave | `rgba(200,164,92,.045)`, 26px pitch |
| Opponent tint | `rgba(40,62,86,.5)` → transparent, 470px |
| Player tint | `rgba(96,52,18,.34)` → transparent, 430px |
| Rail scrim | `rgba(10,7,5,.92)` → `rgba(10,7,5,.55)` |
| Rail divider | `rgba(200,164,92,.25)` |
| Empty zone fill | `rgba(18,11,6,.55)` / `rgba(6,16,20,.55)` / `rgba(12,8,20,.55)` |
| Drop-target glow | `0 0 26px rgba(200,164,92,.35)` |
| Graveyard | `#8F99A8` label, `#C3CBD6` count, border `rgba(140,150,165,.4)` |
| Banished | `#C97A5C` label, `#E9A183` count, border `rgba(224,96,58,.45)` |
| Mana pip | `#EBCE8A` (you) / `#8FC6D2` (foe), 11 × 11 rotated 45° |
| Log ink | `#4A3E2A` body, `#2E2417` emphasis, `#7A5A1E` separator |

### Board spacing scale
`3 · 4 · 5 · 6 · 7 · 8 · 10 · 11 · 12 · 14 · 16 · 18 · 20 · 24 · 26 · 60`

## Assets

| Asset | Source | Notes |
|---|---|---|
| `uploads/artwork-1785612438938.png` | supplied by the user | used on the inspector card and one field card |
| Field / hand artwork | **missing** | stood in by a rotated-diamond placeholder; every card needs its square art |
| Board surface | none — CSS only | should become one texture with the weave baked in |
| Zone frames, piles, crests | none — CSS only | 9-slice sprites in an engine |

## Files

| File | What it is |
|---|---|
| `Duel Field.dc.html` | The board design. **Primary reference for this document.** |
| `TCG Card System.dc.html` | The card design + annotated template + tokens. |
| `README.md` | Card system handoff — read first. |
