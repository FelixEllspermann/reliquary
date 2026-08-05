# Handoff: RELIQUARY — TCG Card System

> The game is called **RELIQUARY**. This bundle covers three pieces: the card system
> (this file), the duel board (`README-duel-field.md`), the app shell
> (`README-shell-screens.md`), the collection screens
> (`README-collection-screens.md`), the duel setup screen
> (`README-duel-setup.md`) and three cutscenes — the coin toss
> (`README-coin-flip.md`), the vault entrance (`README-vault-enter.md`) and the
> duel load (`README-duel-load.md`).

## Overview
Card layout system for a digital trading card game. Defines the **front face** (three card
types: Monster, Spell, Artifact) and the **shared back face**. The frame is deliberately
setting-agnostic: the same chassis must carry medieval fantasy, steampunk, futuristic and
mythological artwork without looking out of place. Card type is communicated by the metal
keyline colour; elemental attribute by a small pip; power level by a hexagonal crest.

## About the Design Files
The files in this bundle are **design references created in HTML** — prototypes that show the
intended look, proportions and field rules. They are **not production code to copy directly**.

The task is to **recreate these designs inside the target codebase's existing environment**
(Unity UI/UGUI, Godot, React, Unreal UMG, SwiftUI, …) using its established patterns, layout
system and asset pipeline. If no environment exists yet, pick the most appropriate one for the
project and implement the design there.

In an engine, most of the frame should end up as **9-slice sprites / atlas art**, not as
runtime-generated gradients. The CSS gradients in the prototype describe the *intended
appearance* of those sprites.

## Fidelity
**High fidelity.** Colours, typography, spacing and field sizes are final and should be matched
precisely. Two exceptions, which are explicitly placeholder:
- the artwork inside the frames (one supplied sample + two empty drop slots),
- the sample card names / effect texts (they exist to prove the text budget).

---

## Card geometry

Two scales are used throughout. **Screen scale** is the working/reference scale; **master scale**
is the print/high-res render target.

| | Screen | Master |
|---|---|---|
| Card | 480 × 672 | 750 × 1050 |
| Ratio | 2.5 : 3.5 (0.7143) | same |
| Physical | — | 63.5 × 88.9 mm @ 300 dpi |
| Corner radius | 12 | 19 |
| Box model | border-box (border is *inside* the 480×672) | same |
| Outer keyline border | 2 | 3 |
| Padding — sides | 39 | 61 |
| Padding — top / bottom | 12 | 19 |
| Content column width | 402 | 628 |

**Master = screen × 1.5625.** Every value below is given at screen scale; multiply by 1.5625
for master. Nothing in the layout is a percentage — the card is a fixed grid.

### Vertical stack (Monster & Artifact)
Top to bottom inside the padded content column, all 402 wide unless noted:

| # | Field | Height | Gap below |
|---|---|---|---|
| 1 | Name plate + level crest row | 51 | 5 |
| 2 | Artwork frame (362 wide, centred) | 362 | 5 |
| 3 | Card type badge + attribute/type strip | 29 | 4 |
| 4 | Effect text panel | 128 | 4 |
| 5 | DMG / DEF row | 56 | — |

2 (border) + 12 + 51 + 5 + 362 + 5 + 29 + 4 + 128 + 4 + 56 + 12 + 2 (border) = **672** ✓

### Vertical stack (Spell — no DMG/DEF)
The stat row is **removed entirely**, and the effect panel absorbs its height + its gap
(56 + 4 = 60):

| # | Field | Height | Gap below |
|---|---|---|---|
| 1 | Name plate + level crest row | 51 | 5 |
| 2 | Artwork frame (362 wide, centred) | 362 | 5 |
| 3 | Card type badge + attribute/type strip | 29 | 4 |
| 4 | Effect text panel | **188** | — |

2 (border) + 12 + 51 + 5 + 362 + 5 + 29 + 4 + 188 + 12 + 2 (border) = **672** ✓

This is the general rule: **any card without a stat row gets effect height 128 + 60 = 188.**

---

## Screens / Views

### View 1 — Card front

**Purpose:** the single unit of play. Must be readable at ~40% of reference size in a hand of
cards, and fully legible when zoomed.

**Layout:** fixed 480 × 672 box, `box-sizing: border-box`, `position: relative`, padding `12px 39px`, radius 12,
2px solid keyline border, plus `inset 0 0 0 1px rgba(0,0,0,.5)` to darken the inner edge of
the metal. Body fill is a 165° three-stop gradient (see per-type table). Content is a single
402-wide flex column.

**Components**

**1. Inner keyline (decorative)**
- Absolutely positioned, `inset: 6px`, radius 7, 1px solid `keyline @ 40% alpha`.
- `pointer-events: none`.

**2. Corner rivets (decorative) — 4×**
- 13 × 13, `transform: rotate(45deg)` (a diamond), solid keyline colour.
- Positioned 12px from each corner (top-left, top-right, bottom-left, bottom-right).

**3. Name plate**
- `flex: 1` inside the 51-high header row, gap 8 to the crest → effectively 350 × 51.
- Background: `linear-gradient(180deg, <plateTop>, <plateBottom>)`.
- 1px keyline border on **top and bottom only** (no side borders — the chamfer supplies them).
- Chamfered ends: `clip-path: polygon(0 0, 100% 0, calc(100% - 14px) 100%, 14px 100%)`.
- Padding `0 18px`, `overflow: hidden`.
- Text: **Cinzel 600, 22px / 1**, colour `<nameInk>`, single line,
  `white-space: nowrap; overflow: hidden; text-overflow: ellipsis`.
- Budget: ~24 characters before ellipsis. Longer names should be authored shorter, not shrunk.

**4. Level crest**
- 44 × 48, `flex: none`, sits at the right end of the header row.
- Shape (both layers): `clip-path: polygon(50% 0, 100% 20%, 100% 66%, 50% 100%, 0 66%, 0 20%)`.
- Outer layer = metal rim: `linear-gradient(160deg, <crestLight>, <crestDark>)`.
- Inner layer = `position: absolute; inset: 2px`, dark fill
  `linear-gradient(160deg, <crestInnerTop>, <crestInnerBottom>)`.
- Numeral: **Cinzel 700, 24px / 1**, colour `<crestInk>`, centred.
- **Allowed values: 1, 2, 3 only.** Never renders over the artwork.

**5. Artwork frame**
- Outer box 362 × 362, `align-self: center` (so it is inset 20px from the content column on
  each side), `box-sizing: border-box`, padding **9**, 2px solid keyline,
  `box-shadow: 0 3px 8px rgba(0,0,0,.55)`, fill `linear-gradient(160deg, <frameTop>, <frameBottom>)`.
- Inner image container: 100% × 100% (= **342 × 342**), `overflow: hidden`,
  1px solid `keyline @ 65% alpha`. On Monster it additionally carries
  `inset 0 0 40px rgba(0,0,0,.5)` as a vignette.
- Image: **strict 1:1**, `object-fit: cover`.
- **Artwork delivery spec: 1024 × 1024 minimum, square, no text baked in, key subject inside
  the central 85% (the 1px inner line and vignette eat the outermost pixels).**

**6. Card type badge**
- `flex: none`, height 29, padding `0 11px`, no radius.
- Background `linear-gradient(180deg, <badgeTop>, <badgeBottom>)`, ink `<badgeInk>`.
- Text: **Oswald 600, 12px / 1, letter-spacing .14em**, uppercase.
- Values: `MONSTER` · `SPELL` · `ARTIFACT`. This value drives the entire card palette.

**7. Attribute · Type strip**
- `flex: 1`, height 29, padding `0 11px`, background `rgba(0,0,0,.35)`,
  1px solid `keyline @ 45% alpha`, `justify-content: space-between`.
- Left: attribute pip (9 × 9, `rotate(45deg)`, solid attribute colour) + gap 7 + label.
  **Oswald 500, 12px / 1, letter-spacing .16em**, colour `<metaInkStrong>`.
- Right: type word. Same font, colour `<metaInkMuted>`.
- Monster type vocabulary: **Dragon · Human · Mecha · Myth · Animal**.
- Spell and Artifact reuse the slot for their own subtype word (`RITUAL`, `MECHA`, …).

**8. Effect text panel**
- 402 wide. Height **128** (Monster, Artifact) or **188** (Spell / any card without stats).
- Background: parchment `linear-gradient(180deg, #EBE1C7, #D9CCAB)` — **identical on all three
  card types**; only the 1px border takes the type colour (`#8C7440` / `#4C7B87` / `#6A5A93`).
- Padding `9px 12px` (128 variant) or `11px 13px` (188 variant). `overflow: hidden`.
- Body: **Spectral 400, 13px / 1.45**, colour `#2E2417`, `text-wrap: pretty`.
- Optional subtype eyebrow above the body (used on Spell): **Oswald 500, 11px / 1,
  letter-spacing .20em**, colour `#6B6250`, margin-bottom 9.
- **Text budget: ~6 lines ≈ 330 characters at 128px; ~9 lines ≈ 500 characters at 188px.**
- Overflow policy: **do not auto-shrink below 12px.** Prefer authoring shorter text or an
  in-engine scroll/expand on tap.

**9. DMG box** (Monster, Artifact — omitted on Spell)
- `flex: 1` (≈198 × 56), padding `0 13px`, `justify-content: space-between`,
  `align-items: center`, gap 6 to the DEF box.
- Background `linear-gradient(180deg, <statTop>, <statBottom>)`, 1px solid **full keyline**.
- Label: **Oswald 500, 11px / 1, letter-spacing .18em**, colour `<statLabelStrong>`.
- Value: **Cinzel 700, 28px / 1**, colour `<statInkStrong>`. Range 0–9999; Artifact may show a
  modifier form (`+400`).

**10. DEF box**
- Identical geometry. Border is `keyline @ 40–50% alpha` instead of full keyline, so the pair
  reads as primary/secondary at a glance.
- Label colour `<statLabelMuted>`, value colour `<statInkMuted>`.
- When a value does not apply, render an em dash `—` in `<statInkDisabled>`.

### View 2 — Card back

**Purpose:** face-down state, deck, hand of the opponent. **One single back for the entire
game** — it must be identical for every card, otherwise the deck is marked.

- 480 × 672, `box-sizing: border-box` (the back MUST measure exactly the same as a front — a
  mismatched back marks the deck), radius 12, 2px solid `#C8A45C`, `overflow: hidden`.
- Base: `radial-gradient(ellipse at 50% 50%, #4E2A18, #1C0E08 78%)`.
- Weave: two stacked `repeating-linear-gradient`s at **+45°** and **−45°**,
  `rgba(200,164,92,.13) 0 1px, transparent 1px 20px`.
- Double keyline: `inset: 10px` → 1px `rgba(200,164,92,.55)`, radius 6;
  `inset: 16px` → 1px `rgba(200,164,92,.22)`, radius 4.
- Centre ornament, all centred with `translate(-50%,-50%)`:
  - 230 × 230 rotated 45°, 2px `rgba(200,164,92,.6)`
  - 230 × 230 unrotated, 1px `rgba(200,164,92,.3)`
  - 120 × 120 rotated 45°, fill `linear-gradient(135deg, rgba(200,164,92,.35), rgba(200,164,92,.05))`, 1px `rgba(200,164,92,.7)`
  - 46 × 46 rotated 45°, fill `linear-gradient(135deg, #E6CD8F, #7A5A1E)`
- **No logo, no wordmark** — deliberate, so it survives a later rebrand.
- In engine this should be **one texture**, not layered quads.

---

## Interactions & Behavior

The prototype is static; the following is the intended behaviour to implement.

- **Flip (back → front):** 3D Y-rotation, 320 ms, `cubic-bezier(.4, 0, .2, 1)`. Swap faces at
  the 50% mark. Backface culled.
- **Hover / focus (desktop or controller):** lift `translateY(-8px)` + scale 1.03, 140 ms
  `ease-out`; drop shadow deepens from `0 24px 60px rgba(0,0,0,.65)` to
  `0 34px 80px rgba(0,0,0,.75)`.
- **Selected:** keyline colour goes to its light stop (e.g. `#C8A45C` → `#EBCE8A`) plus an
  outer glow `0 0 24px <keyline @ 45%>`.
- **Disabled / unplayable:** card desaturates to ~35%, opacity .7, no hover lift.
- **Long effect text:** on tap/hover, expand the card to a detail view rather than scaling the
  type down. Never render effect text below 12px.
- **Missing artwork:** show the frame with an empty inner panel at `#1A1108` (or the type's
  `<frameBottom>`) — never collapse the artwork box; the layout is fixed-height.
- **Level:** value is clamped to 1–3 at the data layer. There is no visual state for 0 or 4+.
- **Responsive:** the card does not reflow. Scale the whole 480 × 672 box uniformly; below
  ~55% only the name, level, badge, DMG and DEF stay legible — that is the intended
  "hand" reading, effect text is read in the detail view.

## State Management

Per-card data model:

```ts
type CardType   = 'monster' | 'spell' | 'artifact';
type Attribute  = 'fire' | 'water' | 'light' | 'dark' | 'earth' | 'wind';
type MonsterType = 'dragon' | 'human' | 'mecha' | 'myth' | 'animal';

interface Card {
  id: string;
  name: string;          // ~24 chars before ellipsis
  level: 1 | 2 | 3;
  artworkUrl: string;    // square, >= 1024x1024
  cardType: CardType;    // drives the whole palette
  attribute: Attribute;  // drives the pip colour
  subtype: MonsterType | string; // right cell of the meta strip
  effect: string;        // <= ~330 chars with stats, <= ~500 without
  dmg?: number | string; // omit on spell
  def?: number | string; // omit on spell
}
```

Derived at render time:
- `hasStats = cardType !== 'spell'`
- `effectHeight = hasStats ? 128 : 188`
- `palette = TYPE_PALETTE[cardType]`

UI state: `faceUp`, `hovered`, `selected`, `playable`. No data fetching is part of this design.

---

## Design Tokens

### Card type palettes

| Token | Monster | Spell | Artifact |
|---|---|---|---|
| keyline | `#C8A45C` | `#8FC6D2` | `#B9A3E0` |
| bodyTop | `#332315` | `#17323A` | `#241C3C` |
| bodyMid (55%) | `#150D07` | `#07161A` | `#0D0916` |
| bodyBottom | `#251809` | `#122A31` | `#1D1633` |
| plateTop | `#42301C` | `#1E3A40` | `#2E2545` |
| plateBottom | `#22150A` | `#0E2126` | `#171029` |
| nameInk | `#F1DFB8` | `#DCF0F4` | `#E9E0F8` |
| crestLight | `#EBCE8A` | `#B4E2EC` | `#D6C4F5` |
| crestDark | `#8E6A22` | `#3E7A88` | `#6A4FA8` |
| crestInnerTop | `#3B2A10` | `#132E35` | `#241C3A` |
| crestInnerBottom | `#180F04` | `#050F12` | `#0C0916` |
| crestInk | `#F3DDA4` | `#B9E6F0` | `#D8CAF6` |
| frameTop | `#3E2C16` | `#20424B` | `#332A50` |
| frameBottom | `#1A1108` | `#0A1A1F` | `#120E20` |
| badgeTop | `#E2C685` | `#A5D8E2` | `#C2AEEC` |
| badgeBottom | `#9C7526` | `#3B7C8B` | `#5F4699` |
| badgeInk | `#1E1405` | `#04191D` | `#100A1E` |
| metaInkStrong | `#E4D3AE` | `#CDE6EB` | `#DDD3F0` |
| metaInkMuted | `#B5A484` | `#89A8B0` | `#9A8FB8` |
| effectBorder | `#8C7440` | `#4C7B87` | `#6A5A93` |
| statTop | `#2A1D0E` | — | `#221A38` |
| statBottom | `#140C05` | — | `#100B1C` |
| statLabelStrong | `#B79A62` | — | `#9A8AC4` |
| statLabelMuted | `#8D8570` | — | `#6D6684` |
| statInkStrong | `#F3DDA4` | — | `#D8CAF6` |
| statInkMuted | `#DCD3BC` | — | `#D8CAF6` |
| statInkDisabled | — | — | `#8C81AE` |

### Shared
- Parchment panel: `#EBE1C7` → `#D9CCAB`, ink `#2E2417`
- Card shadow: `0 24px 60px rgba(0,0,0,.65)`
- Artwork frame shadow: `0 3px 8px rgba(0,0,0,.55)`
- Inner edge darkening: `inset 0 0 0 1px rgba(0,0,0,.5)`
- Back base: `#4E2A18` → `#1C0E08`; back gold `#C8A45C`, highlight `#E6CD8F`, shadow `#7A5A1E`

### Attribute pips (9 × 9, rotated 45°)
| Attribute | Hex |
|---|---|
| Fire | `#E0603A` |
| Water | `#4B92D6` |
| Light | `#E8D08A` |
| Dark | `#8B6BC4` |
| Earth | `#A8894F` |
| Wind | `#6FBF9A` |

### Spacing scale (screen)
`4 · 5 · 6 · 8 · 9 · 11 · 12 · 13 · 18 · 20 · 39`

### Typography

| Role | Font | Size / line | Weight | Letter-spacing |
|---|---|---|---|---|
| Card name | Cinzel | 22 / 1 | 600 | .01em |
| Level numeral | Cinzel | 24 / 1 | 700 | 0 |
| Stat value | Cinzel | 28 / 1 | 700 | 0 |
| Card type badge | Oswald | 12 / 1 | 600 | .14em |
| Attribute / type | Oswald | 12 / 1 | 500 | .16em |
| Stat label | Oswald | 11 / 1 | 500 | .18em |
| Effect eyebrow | Oswald | 11 / 1 | 500 | .20em |
| Effect body | Spectral | 13 / 1.45 | 400 | 0 |

Fonts are Google Fonts: **Cinzel** (500/600/700), **Oswald** (300–700), **Spectral**
(400/500/600 + italic). All three are SIL OFL — safe to embed in a shipped game.

### Radii & borders
- Card radius 12 (master 19); inner keyline radius 7; back inner keylines 6 / 4
- Name plate, badge, meta strip, effect panel, stat boxes: **radius 0** (deliberately hard-edged)
- Keyline border 2px; hairlines 1px

---

## Assets

| Asset | Source | Notes |
|---|---|---|
| `uploads/artwork-1785612438938.png` | supplied by the user | 1024 × 1024 sample artwork, used on the Monster card |
| Spell artwork | **missing** | empty 1:1 drop slot in the prototype |
| Artifact artwork | **missing** | empty 1:1 drop slot in the prototype |
| Frame art | none — CSS only | the frame is drawn entirely in CSS; in-engine it should become 9-slice sprites |
| Card back | none — CSS only | should become one texture |
| Icons | none | attribute pips are plain rotated squares, not icons. If icon pips are wanted later, they must fit 9 × 9 at screen scale (14 × 14 master) |

No logo or wordmark exists yet — the back is intentionally logo-free.

## Files

| File | What it is |
|---|---|
| `README-duel-field.md` | Handoff for the in-game duel board (companion document). |
| `README-shell-screens.md` | Handoff for the Login and Main Menu screens (companion document). |
| `README-collection-screens.md` | Handoff for the Deck Builder and Shop screens (companion document). |
| `README-duel-setup.md` | Handoff for the Online Duel / Solo Trial setup screen (companion document). |
| `README-coin-flip.md` | Handoff for the coin-toss cutscene, written for a Unity port (companion document). |
| `README-progression.md` | Handoff for ranks, leaderboard, profile and cosmetics (companion document). |
| `README-animations.md` | Handoff for the five game animations, written for a Unity port (companion document). |
| `Rank Up.dc.html` | Promotion animation, all nine rank transitions. |
| `Pack Open.dc.html` | Pack opening with the rarity flame field. |
| `Card Cast.dc.html` | Card activation and targeting. |
| `Card Destroy.dc.html` | Destruction and the trip to the graveyard. |
| `Player Defeat.dc.html` | The final direct attack on a player card. |
| `Extra Summon.dc.html` | The vault opens and a Reliquary monster is summoned. |
| `Rank Ladder.dc.html` | The ten seals and their sub-ranks. |
| `Leaderboard.dc.html` | Top 50, season and all-time. |
| `Profile Screen.dc.html` | Duelist record. |
| `Cosmetics Shop.dc.html` | 20 cosmetics + Glossy / Rainbow / Static finishes. |
| `Coin Flip.dc.html` + `coin-flip.jsx` | The coin-toss animation, 7.7 s at 1280 × 720. |
| `README-vault-enter.md` | Handoff for the login → main menu transition (companion document). |
| `Vault Enter.dc.html` + `vault-enter.jsx` | The vault-entrance animation, 6.6 s at 1280 × 720. |
| `README-duel-load.md` | Handoff for the turn-choice → board transition (companion document). |
| `Duel Load.dc.html` + `duel-load.jsx` | The shuffle loading animation, 4.4 s at 1280 × 720. |
| `Duel Setup.dc.html` | Duel setup — deck choice, matchmaking, lobby codes, bot difficulty. |
| `Collection Screens.dc.html` | Deck Builder + Shop, tabbed and wired as a clickable flow. |
| `Shell Screens.dc.html` | Login + Main Menu, wired together as a clickable flow. |
| `Duel Field.dc.html` | The duel board design — 1920 × 1080, uses the reduced card renditions. |
| `TCG Card System.dc.html` | The final design. Monster / Spell / Artifact fronts, shared back, annotated anatomy template, token sheet. **Primary reference.** |
| `TCG Card System v1 (3 directions).dc.html` | Earlier exploration — three frame directions (Bastion / Circuit / Reliquary). Kept for context only; **Reliquary is the chosen direction**. |
| `image-slot.js` | Prototype-only helper that renders the empty artwork drop targets. Not part of the design. |
| `support.js` | Prototype runtime. Not part of the design. |
| `uploads/artwork-1785612438938.png` | Sample artwork. |

Open `TCG Card System.dc.html` in a browser to inspect any value with dev tools — every field
is a plain element with inline styles, so computed sizes can be read directly.
