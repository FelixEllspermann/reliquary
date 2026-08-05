# Handoff: Collection Screens — Deck Builder & Shop

Companion document to `README.md` (card system), `README-duel-field.md` (board) and
`README-shell-screens.md` (login & menu). Same visual language — **Reliquary**.
Read the card README first for the type palettes and the type scale.

## Overview
The two collection-facing screens, reached from the Main Menu's DECKS and SHOP tiles:
a **Deck Builder** (card pool → deck → live balance readout) and a **Shop** (featured set,
pack tiles, pack-opening ritual). They share one top bar and switch by tab, so a player can
buy a pack and immediately build with it without leaving the screen.

Designed at a fixed **1920 × 1080**, scaled uniformly to the viewport (letterbox, no reflow).

## About the Design Files
`Collection Screens.dc.html` is a **design reference created in HTML**. The interactivity
(filtering, add/remove, balance maths, pack opening) exists so the flow can be reviewed —
it is not an implementation. There is no persistence, no economy validation, no server.
Recreate in the target engine using its own UI and data layer.

## Fidelity
**High fidelity.** Sizes, colours, type and motion timings are final. Placeholder content:
all card names and effect text, the coin/dust balances, pack names and prices, and the
diamond glyph standing in for artwork on revealed pack cards.

---

## Shared chrome

Background is the same shell stack as the login/menu screens:
`radial-gradient(ellipse 1500px 820px at 50% 40%, #2A1C12, #0A0705 78%)` + the ±45° weave at
`rgba(200,164,92,.04)` / 28px pitch + `inset 0 0 240px rgba(0,0,0,.85)` vignette.
**No ember particles here** — the shell screens are atmospheric, the collection screens are
working surfaces.

### Top bar — 96 high
`padding: 0 48`, `border-bottom: 1px rgba(200,164,92,.25)`,
`linear-gradient(180deg, rgba(10,7,5,.9), transparent)`.

Left: 11px gold diamond + the 25px wordmark (same seamless gold gradient as the shell —
90°, identical first and last stop, `shimmer 9s linear infinite`), then the **tab control**:
`padding: 4` on `rgba(0,0,0,.45)` + 1px `rgba(200,164,92,.3)`, radius 5. Active tab uses the
accent of the screen it selects — **gold** for Deck Builder, **ember** (`#E8B896 → #A85E3C`,
1px `#E0A07A`) for Shop. Inactive `#8C7B5F`, hover `rgba(200,164,92,.1)`.

Right, gap 12:
1. **Dust wallet** — one pill holding four rarity diamonds (9 × 9, rotated 45°) each with its
   count in Cinzel 600 13px. Rarity colours below.
2. **Coins** — 13px gold diamond + Cinzel 600 17px `#F3DDA4`. Thousands separated by a
   **thin space** (`1 000 199`), never a comma.
3. **← MENU** ghost button.

### Custom scrollbar
Both list panels scroll. Track `rgba(0,0,0,.4)`; thumb `linear-gradient(180deg,#8E6A22,#4A3512)`
with a 1px `rgba(200,164,92,.35)` border, width 10; hover thumb brightens to
`#EBCE8A → #8E6A22`. Never the OS default — it breaks the frame.

---

## Screen 1 — Deck Builder

Content area 984 high, `padding: 24px 48`, three columns, **gap 24**:

| Column | Width |
|---|---|
| Card Pool | 600 |
| Your Deck | 600 |
| Detail rail | remainder (~552) |

All three are **936 high** — the columns bottom-align, so the screen reads as one slab.

### Card Pool panel
radius 9, 1px `rgba(200,164,92,.35)`, fill `rgba(30,20,12,.75) → rgba(10,7,5,.75)`,
`overflow: hidden` with the list scrolling inside.

**Header** (`padding: 16px 18px 14`, gap 12, `rgba(0,0,0,.3)`, 1px bottom rule):
- Title `Card Pool` — Cinzel 600 21px `#F1DFB8`; right: `N of M cards` in Spectral 13px `#8C7B5F`.
- **Search field** — height 40, `rgba(0,0,0,.5)`, 1px `rgba(200,164,92,.35)`, radius 4,
  Spectral 14px. Focus: border `#EBCE8A` + `0 0 0 3px rgba(235,206,138,.14)`.
  Matches name, attribute, subtype and card type in one query.
- **Type filter row** — 4 equal chips: `ALL · MONSTER · SPELL · ARTIFACT`.
- **Attribute filter row** — 7 equal chips: `ALL` + two-letter codes `FI WA LI DA EA WI`
  (full name in the tooltip). The active chip borders in that attribute's own colour.

**Chip spec** — `flex: 1`, `padding: 8px 0`, radius 3, Oswald 10px / .14em.
Active: 1px accent border, `rgba(200,164,92,.2)`, ink `#F1DFB8`, weight 600.
Inactive: 1px `rgba(200,164,92,.2)`, `rgba(0,0,0,.4)`, ink `#7E7059`, weight 500.

**Card row** — the core component, **62 high**, radius 5, `padding-left: 12` on the text block:

| Part | Spec |
|---|---|
| Attribute stripe | absolute, left 0, width **4**, full height, solid attribute colour |
| Name | **Cinzel 600 16px**, coloured by **rarity** (not by attribute), single line + ellipsis |
| Meta line | Oswald 500 10px / .10em `#8C7B5F` — 7px attribute pip · attribute (in its colour) · `/` · subtype · `·` · `DMG / DEF` (or the rarity word for non-monsters) |
| Level crest | 26 × 29 hexagon, same `clip-path` and two-layer build as the card, numeral Cinzel 700 13px |
| Count block | 56 wide, centred — `inDeck`/`owned` in Cinzel 700 19px (`#F3DDA4` when > 0, else `#5C513F`) over an `IN DECK` caption (Oswald 8px / .14em `#5C513F`) |
| −/+ buttons | 30 × 30, radius 3. Enabled −: 1px `rgba(224,96,58,.5)` on `rgba(224,96,58,.15)`, ink `#E9A183`. Enabled +: 1px `rgba(200,164,92,.55)` on `rgba(200,164,92,.18)`, ink `#F3DDA4`. Disabled: 1px `rgba(200,164,92,.15)`, `rgba(0,0,0,.3)`, ink `#4A4235` |

Row default: 1px `rgba(200,164,92,.18)` on `rgba(0,0,0,.38)`.
Row selected: 1px in the **card type's keyline** on `rgba(200,164,92,.14)`. 120 ms transition.

**Row interaction rules**
- Hover anywhere on the row → it becomes the selected card in the detail rail. No click needed.
- Click the row body → **add one copy** (pool) / **remove one copy** (deck list).
- `−` / `+` stop propagation so they never double-fire.
- Copies are clamped to `min(3, owned)`. A copy count of 0 removes the row from the deck list.

### Your Deck panel
Same shell, warmer: 1px `rgba(200,164,92,.45)`, fill `rgba(46,32,18,.7) → rgba(12,8,5,.8)`,
`box-shadow: 0 0 40px rgba(200,164,92,.07)`.

**Header** (gap 12):
1. **Deck name input** — height 44, **Cinzel 600 19px** (the name is typed straight into its
   own display type), plus `+ NEW` (gold ghost) and `DELETE` (ember ghost) buttons.
2. **Hero row** — `HERO` label + three chips carrying the hero's first name. The hero is the
   Player Card that sits on the flank of the duel board.
3. **Count bar** — a 9px track (`rgba(0,0,0,.55)`, 1px `rgba(200,164,92,.3)`) filled
   `#8E6A22 → #F3DDA4` at `count / 80`, followed by the count in **Cinzel 700 22px**:
   **`#7ACD96` when 40 ≤ n ≤ 80, `#E9A183` otherwise**, with a `/ 40–80` suffix in Oswald 12px.
   This is the single legality signal on the screen.

Rows are the pool row with two changes: the count block shows `×N` over a `COPIES` caption,
and clicking the body removes instead of adds.

**Empty state** — a dashed `rgba(200,164,92,.35)` panel with a 22px diamond,
`This vault is empty` (Cinzel 600 17px `#A2917A`) and one Spectral line of instruction.
Never an empty scroll area.

### Detail rail
Four stacked blocks, gap 14.

1. **Card preview** — a 298 × 417 viewport containing the **real 480 × 672 card** at
   `transform: scale(.62)`. Same rule as the duel-field inspector and the login hero:
   **never redraw the card at a second size.** Body fill, plate, badge and keyline all swap
   by card type; DMG shows `—` for Spells, DEF for anything but Monsters.
2. **Dust / Craft** — two equal buttons. `DUST · +N` is a steel ghost
   (1px `rgba(140,150,165,.45)`, ink `#AEB7C2`); `CRAFT · −N` is the gold primary.
   Values come from the selected card's rarity, so the player never has to look them up.
3. **Deck Balance** — `rgba(0,0,0,.42)`, 1px `rgba(200,164,92,.3)`, radius 7, gap 15:
   - header `DECK BALANCE` (8px diamond + Oswald 10px / .24em + fading rule);
   - **level curve** — three rows `LV 1/2/3`, each a 14px track with a fill scaled to the
     *largest* bucket (not to the deck size, so short decks still read), count right in
     Cinzel 600 13px. Lv3 fills ember (`#8E4A1E → #F3C3A6`) to flag top-heaviness;
   - **type split** — three equal cards, each bordered in its card-type accent, label Oswald
     9px / .16em + count Cinzel 700 20px;
   - **attribute spread** — a single 11px stacked bar, 2px gaps, one segment per attribute in
     its own colour, widths proportional. Tooltip carries the name;
   - **advice strip** — parchment (`#EBE1C7 → #D9CCAB`, 1px `#8C7440`), Spectral 12/1.45
     `#2E2417`, pinned to the bottom with `margin-top: auto`.
     **The advice is derived, never static.** Priority order: under 40 cards → how many more;
     fewer than 14 monsters → field-presence warning; more than 6 Level 3 → top-heavy warning;
     zero artifacts → suggestion; otherwise "legal and balanced".
     Widths and bar fills transition 250 ms ease-out so edits are visibly felt.
4. **Save / Back** — `SAVE DECK` (flex 2, gold primary, Cinzel 600 16px) and `BACK` (flex 1,
   ghost). After a successful save the label becomes `SAVED ✓`; **any subsequent edit resets
   it** — name change, hero change, or any copy count change.

---

## Screen 2 — Shop

Content area 984, `padding: 26px 48`, column layout, gap 22.

### Featured banner — 296 high
radius 11, **2px `#E0A07A`**, `linear-gradient(110deg,#3E2018,#170A06 46%,#2C130C)`,
`padding: 0 44`, gap 44, `overflow: hidden`.
Decor: inner keyline at `inset: 6`; two large rotated squares bleeding off the right edge
(420 and 260, 1px `rgba(224,160,122,.16)` / `.1`) — depth without an illustration.

Left: a **186 × 246 pack** — the card back recoloured ember (weave 11px pitch, 96px rotated
frame, 34px glowing core), `float 6s ease-in-out infinite`.

Right: `NEW SET` badge + season kicker · title **Cinzel 700 46px** `#F8E2D6` ·
Spectral 16/1.5 `#B79582` blurb (max-width 620) · then `BUY PACK · 150` (ember primary,
Cinzel 600 16px) and a `2 FREE PACKS WAITING` status pill with a `#7ACD96` dot.

### Pack tiles
Four equal tiles filling the remaining height, gap 22. Same grammar as the menu tiles:
radius 10, 2px accent keyline, inner keyline at `inset: 6`, two 11px accent rivets at the top.

| Pack | Keyline | Body | Price | Contents |
|---|---|---|---|---|
| Tomb of Ash | `#E0A07A` | `#3E2018 → #170A06 → #2C130C` | 150 | Rare or better guaranteed |
| Flames & Frost | `#8FC6D2` | `#1B3A43 → #07161A → #122A31` | 100 | Base set, 120 cards |
| Relic Cache | `#B9A3E0` | `#2A2148 → #0D0916 → #1D1633` | 200 | Artifacts only, 40 cards |
| Sealed Vault | `#EBCE8A` | `#41301A → #150E06 → #2E2010` | 450 | One Legendary guaranteed |

Tile anatomy: **pack art** (152 × 206, radius 6, 2px keyline, radial body, white hairline
weave, 56px rotated gem with a `0 0 26px` accent glow) → **title** Cinzel 700 25px →
**kicker** Oswald 500 10px / .20em → **parchment strip** (blurb left, `×owned` right) →
**two buttons**: `BUY · price` (ghost in the tile's accent) and `OPEN`.

`OPEN` has three states: **owned > 0** → gold primary with `colGlow 2.6s` (a breathing
`0 0 24px → 0 0 46px` glow — the only animated affordance on the screen, so the eye goes
straight to what can be opened); **owned = 0** → `NONE OWNED`, `cursor: not-allowed`, ink
`#4A4235`; disabled tiles never lose their frame.

Footer: signed-in line (Spectral 13px) left, version string right.

### Pack opening overlay
`position: absolute; inset: 0; z-index: 30`, scrim
`radial-gradient(ellipse at 50% 45%, rgba(60,30,14,.9), rgba(7,5,3,.96) 70%)`, column, gap 34.

- Header: `UNSEALING` (Oswald 500 11px / .34em `#9C8A6A`) over the pack name (Cinzel 700 40px).
- **Five cards, 200 × 280**, gap 20, each individually clickable.
  - **Sealed**: the card back — weave at 11px pitch, 88px rotated frame, 32px gold core,
    and `CLICK TO UNSEAL` at the bottom in Oswald 9px / .22em.
  - **Revealed**: a mini card (name plate 26 · art 148 · meta 16 · rarity bar) animating in
    with `colPop .45s cubic-bezier(.2,.7,.3,1)` — a `scale(.7) rotateY(90deg)` flip that
    overshoots to 1.06 before settling. Border takes the card type's keyline; **Legendary
    pulls override it to `#EBCE8A` plus `0 0 40px rgba(235,206,138,.55)`**, which is the whole
    point of the ritual.
- `REVEAL ALL` (ghost) for players who don't want the ceremony, and `ADD TO VAULT` (gold
  primary) to close. Both always reachable — never trap the player in an animation.

Rarity weighting per pack (design intent, tune in the economy layer):
Sealed Vault `R R R L L` · Tomb of Ash `C U R R L` · others `C C U R R`.

---

## State

```ts
type Screen = 'decks' | 'shop';
type Rarity = 'C' | 'U' | 'R' | 'L';

interface CollectionState {
  screen: Screen;
  filters: { search: string; type: 'All'|'Monster'|'Spell'|'Artifact'; attribute: 'All'|Attribute };
  deck: { name: string; hero: string; counts: Record<string, number>; saved: boolean };
  selected?: Card;                       // drives the detail rail
  wallet: { coins: number; dust: Record<Rarity, number> };
  packs: Record<string, number>;         // packId → owned
  opening?: { packId: string; cards: Card[]; revealed: boolean[] };
}
```

Derived per render, never stored: the filtered pool, the deck list, the count, the level
curve, the type split, the attribute spread and the advice string.

**Rules to enforce in the data layer, not the view:** max 3 copies per card and never more
than `owned`; deck legal at 40–80; craft debits the matching rarity's dust and increments
`owned`; dust credits it and decrements `owned`; buying debits coins and increments the pack
count; opening decrements it and adds five cards to the collection.

## Design tokens (collection-specific)

| Token | Value |
|---|---|
| Panel — pool | `rgba(30,20,12,.75)` → `rgba(10,7,5,.75)`, 1px `rgba(200,164,92,.35)` |
| Panel — deck | `rgba(46,32,18,.7)` → `rgba(12,8,5,.8)`, 1px `rgba(200,164,92,.45)` |
| Row default | `rgba(0,0,0,.38)`, 1px `rgba(200,164,92,.18)` |
| Row selected | `rgba(200,164,92,.14)`, 1px = card-type keyline |
| Legal count | `#7ACD96` · illegal `#E9A183` |
| Add control | `rgba(200,164,92,.18)` / `#F3DDA4` |
| Remove control | `rgba(224,96,58,.15)` / `#E9A183` |
| Disabled control | `rgba(0,0,0,.3)` / `#4A4235` |
| Scrollbar thumb | `#8E6A22 → #4A3512`, hover `#EBCE8A → #8E6A22` |
| Shop accent | `#E0A07A` keyline, `#E8B896 → #A85E3C` fill, `#F3C3A6` highlight |

### Rarity
| Rarity | Ink | Dust gained | Craft cost |
|---|---|---|---|
| Common | `#C6CCD4` | 20 | 40 |
| Uncommon | `#9FDCBE` | 50 | 110 |
| Rare | `#A6CCEA` | 150 | 340 |
| Legendary | `#F3DDA4` | 500 | 1200 |

Rarity colours the **card name** in lists and the wallet diamonds. Attribute colours the
**row stripe and pip**. Card type colours the **frame**. Three separate channels — do not
merge them.

### Keyframes
| Name | Definition | Used for |
|---|---|---|
| `shimmer` | `background-position 0% → 200%`, 9s linear | wordmark |
| `float` | `translateY(0 → -12px → 0)`, 6s ease-in-out | featured pack |
| `glow` | `0 0 24px → 0 0 46px rgba(235,206,138,…)`, 2.6s | openable pack button |
| `pop` | `scale(.7) rotateY(90deg) → scale(1.06) → scale(1)`, .45s | pack card reveal |

### Spacing scale
`3 · 4 · 5 · 6 · 8 · 9 · 10 · 12 · 13 · 14 · 15 · 18 · 20 · 22 · 24 · 26 · 44 · 48`

## Assets

| Asset | Source | Notes |
|---|---|---|
| `uploads/artwork-1785612438938.png` | supplied by the user | shown on the detail-rail preview card |
| Card artwork | **missing** | every card needs square art; the preview currently reuses one image |
| Pack art | none — CSS only | the four packs are recoloured card backs; real pack illustrations would carry the shop |
| Revealed pack cards | placeholder | rotated diamond stands in for artwork |

## Files

| File | What it is |
|---|---|
| `Collection Screens.dc.html` | Deck Builder + Shop, tabbed and wired. **Primary reference for this document.** |
| `Shell Screens.dc.html` | Login + Main Menu. |
| `Duel Field.dc.html` | The duel board. |
| `TCG Card System.dc.html` | Card design, annotated template, tokens. |
| `README.md` | Card system handoff — read first. |
