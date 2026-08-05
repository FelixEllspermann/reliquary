# Handoff: Shell Screens — Login & Main Menu

Companion document to `README.md` (card system) and `README-duel-field.md` (board).
Same visual language — **Reliquary**. Read the card README first for the type palettes and
type scale; this document covers the two out-of-game screens.

## Overview
The application shell: the **Login screen** a player sees on launch, and the **Main Menu**
they land in afterwards. Both live in one prototype file and are wired together — the
prototype really switches screens, so the flow can be clicked through end to end.

Designed at a fixed **1920 × 1080**, scaled uniformly to the viewport (letterbox, no reflow).

## About the Design Files
`Shell Screens.dc.html` is a **design reference created in HTML**. The interactivity exists
so the flow can be reviewed, not as an implementation — there is no real auth, no network,
no persistence. Recreate the screens in the target engine / framework using its own UI and
navigation stack.

## Fidelity
**High fidelity.** Sizes, colours, type and motion timings are final. Placeholder content:
the player name, coin balance, online counts, season/trial copy, and the artwork on the
floating card.

---

## Brand

The game is called **RELIQUARY**.

**Wordmark** — `RELIQUARY`, **Cinzel 700**, letter-spacing **.09em**, all caps.
- Login (hero): **112px**, line-height .92
- Main Menu (top bar): **30px**, letter-spacing .14em

Gold-leaf fill (applied as a clipped gradient on the text):
```css
/* 90deg and identical first/last stops — otherwise the looping sweep
   shows a hard seam every cycle. */
background: linear-gradient(90deg, #A6802F 0%, #F6E4B4 14%, #C8A45C 28%,
                            #F8EED6 42%, #C8A45C 58%, #F6E4B4 76%, #A6802F 100%);
background-size: 200% 100%;
-webkit-background-clip: text; background-clip: text; color: transparent;
animation: shimmer 9s linear infinite;   /* background-position 0% → 200% */
text-shadow: 0 0 60px rgba(200,164,92,.18);
```
In an engine this is a **gold-foil material with a scrolling highlight mask**, not per-glyph
code. A flat `#C8A45C` wordmark is the acceptable fallback; never a plain yellow like the
old build.

Eyebrow above the wordmark: `TRADING CARD GAME`, **Oswald 500, 13px, letter-spacing .42em**,
`#9C8A6A`, preceded by a 10px gold diamond.

---

## Shared shell chrome

Both screens sit on the same background stack:

1. `radial-gradient(ellipse 1500px 820px at 50% 45%, #2A1C12, #0A0705 78%)`
2. Weave overlay — ±45° `repeating-linear-gradient`, `rgba(200,164,92,.04) 0 1px, transparent 1px 28px`
3. Vignette — `inset 0 0 240px rgba(0,0,0,.85)`
4. **Ember particles** — 6 rotated 5–8px diamonds (`#C8A45C`, `#EBCE8A`, one `#E0603A`) rising
   320px over 9–14s with staggered delays, peak opacity .55, then fading out. `pointer-events: none`.
   In an engine: one small particle emitter along the bottom edge. Purely atmospheric —
   never put information in it.

### Keyframes used across both screens
| Name | Definition | Used for |
|---|---|---|
| `float` | `translateY(0 → -16px → 0)`, 6.5s ease-in-out | hero card |
| `floatSlow` | `translateY(0 → -26px → 0)`, 7.5–8.5s ease-in-out | flanking card backs |
| `pulse` | opacity 1 → .55, `box-shadow 0 0 0 0 → 0 0 0 8px rgba(122,205,150,0)`, 2.4s | online status dot |
| `spin` | `rotate(360deg)`, 1.5s linear | matchmaking ring |
| `shimmer` | `background-position 0% → 200%`, 9s linear | wordmark gold sweep |
| `ember` | `translateY(0 → -320px)`, opacity 0 → .55 → 0, 9–14s linear | particles |
| `sweep` | `translateX(-120% → 320%)`, 1–1.6s | button busy state, matchmaking bar |

---

## Screen 1 — Login

Two columns, vertically centred.

| | Width |
|---|---|
| Brand column | 1090, `padding-left: 120` |
| Auth column | remainder, panel centred, `padding-right: 60` |

### Brand column (gap 34)
1. Eyebrow row (diamond + `TRADING CARD GAME`).
2. Wordmark, 112px.
3. **Rule** — 720 wide: two 1px gradient rules fading out from a central 9px gold diamond, gap 16.
4. Tagline — Spectral 400 **21/1.5**, `#A2917A`, max-width 640, `text-wrap: pretty`.
5. **Card trio** — 760 × 400 relative block:
   - left card back, 240 × 336, `rotate(-13deg)`, `floatSlow 7.5s`
   - right card back, 240 × 336, `rotate(13deg)`, `floatSlow 8.5s` (delay 1.2s)
   - centre: the **real 480 × 672 card** at `transform: scale(.5)` in a 240 × 336 viewport,
     no rotation, `float 6.5s` (delay .6s), `filter: drop-shadow(0 30px 60px rgba(0,0,0,.85))`
   Same rule as the duel field inspector: **render the actual card scaled, never a redraw.**
   Card backs use the printed back at 12px weave pitch, 116px centre diamond.

### Auth panel
492 wide, `padding: 32px 36px 28px`, radius 12, **2px solid `#C8A45C`**,
fill `linear-gradient(165deg,#3A2818,#140C07 58%,#291A0C)`,
`box-shadow: 0 40px 90px rgba(0,0,0,.8), inset 0 0 0 1px rgba(0,0,0,.5), 0 0 60px rgba(200,164,92,.08)`.
Inner keyline at `inset: 6px` (1px `rgba(200,164,92,.35)`, radius 7) and four 12px gold corner
rivets at 13px — **the same frame grammar as a card**, so the panel reads as a relic.

Internal gap **19**. Contents top → bottom:

| Element | Spec |
|---|---|
| Panel eyebrow | `ENTER THE VAULT`, Oswald 500 11px / .34em, `#9C8A6A`, centred |
| **Mode tabs** | Segmented control, `padding: 4`, `rgba(0,0,0,.45)` + 1px `rgba(200,164,92,.3)`, radius 5. Active tab: gold gradient `#E2C685 → #9C7526`, 1px `#EBCE8A`, ink `#1E1405`, Oswald 600 12px / .20em. Inactive: transparent, `#8C7B5F`; hover `rgba(200,164,92,.1)` + `#D9C089` |
| Field label | Oswald 500 10px / .22em, `#9C8A6A`, gap 7 to the input |
| **Input** | height 48, `padding: 0 15`, radius 4, `rgba(0,0,0,.5)`, 1px `rgba(200,164,92,.4)`, ink `#F1DFB8`, **Spectral 16px**. Placeholder `#6E6046` italic |
| Input :focus | border `#EBCE8A` + `box-shadow: 0 0 0 3px rgba(235,206,138,.16)`. No browser outline |
| Password reveal | Inline button, right 7 / top 9, height 30, `padding: 0 11`, `rgba(200,164,92,.14)` + 1px `rgba(200,164,92,.4)`, Oswald 500 10px / .16em, label `SHOW` ⇄ `HIDE`. Input gets `padding-right: 74` |
| Remember row | Diamond checkbox — 17 × 17 rotated 45°, 1px `rgba(200,164,92,.6)` on `rgba(0,0,0,.5)`; checked = 8px gold gradient core with `0 0 9px` glow. Label Spectral 14px `#A2917A`. Right: `Lost your seal?` link, `#8C7B5F` → `#EBCE8A` on hover |
| **Primary CTA** | full width, `padding: 17px 0`, radius 5, 2px `#EBCE8A`, gold gradient, **Cinzel 600 18px / .12em**, ink `#1E1405`, `0 10px 26px rgba(0,0,0,.6), inset 0 1px 0 rgba(255,255,255,.35)`. Hover adds `0 0 30px rgba(235,206,138,.45)`. Label `LOG IN` / `CREATE ACCOUNT` by mode |
| CTA busy state | Label swaps to `UNSEALING…`; a 34%-wide white gradient band sweeps across the button (`sweep 1s linear infinite`) |
| OR divider | two 1px rules `rgba(200,164,92,.22)` + `OR`, Oswald 500 10px / .24em, `#6E6046` |
| Secondary CTA | `CONTINUE OFFLINE`, `padding: 14px 0`, radius 5, `rgba(0,0,0,.4)` + 1px `rgba(200,164,92,.4)`, Oswald 500 13px / .16em, `#C8B189` |
| Status line | 8px `#7ACD96` dot with `pulse`, Spectral 13px `#7F9E8A`: `Connected to the vault · N duelists online` |

**Register mode** inserts an `EMAIL` field between name and password. Nothing else moves;
the panel grows downward.

### Login states to implement
- **Offline** — status dot `#C97A5C`, text "Vault unreachable — offline play only", primary
  CTA disabled, `CONTINUE OFFLINE` promoted to the gold treatment.
- **Invalid credentials** — the two inputs' borders go `#E0603A`, a Spectral 13px `#E9A183`
  message appears under the password field; panel shakes ±6px, 220 ms.
- **Busy** — CTA sweep, inputs and tabs disabled at 60% opacity.

---

## Screen 2 — Main Menu

Vertical: **top bar 104** · tiles (flex 1, centred) · **bottom rail 132**.

### Top bar
`padding: 0 64`, `border-bottom: 1px rgba(200,164,92,.25)`,
background `linear-gradient(180deg, rgba(10,7,5,.85), transparent)`.

Left: 12px gold diamond + 30px wordmark + a `TCG` chip (`padding: 4px 8`, 1px
`rgba(200,164,92,.45)`, Oswald 500 9px / .20em).

Right, gap 14 — four pills, each `rgba(0,0,0,.45)` + 1px tinted border, radius 6:
1. **Player plate** — 38 × 42 hexagon crest (same `clip-path` and two-layer build as the card
   level crest) with the initial in Cinzel 700 17px; name Cinzel 600 16px `#F1DFB8`; rank
   Oswald 500 9px / .18em `#9C8A6A`.
2. **Coins** — 13px gold diamond + Cinzel 600 17px `#F3DDA4`. Thousands are separated by a
   **thin space**, not a comma (`1 000 049`).
3. **Decks** — violet diamond + count + `DECKS` label.
4. **Log out** — `padding: 13px 18`, 1px `rgba(224,96,58,.45)`, Oswald 500 11px / .18em `#C97A5C`.

### Menu tiles
Four tiles in a row, **gap 28**, each **318 × 452** — a card-shaped target, not a bar.
radius 10, 2px accent keyline, inner keyline at `inset: 6px`, two 11px accent rivets at the
top corners, `box-shadow: 0 22px 50px rgba(0,0,0,.7)`.

| Tile | Keyline | Body fill | Emblem glyph |
|---|---|---|---|
| PLAY | `#C8A45C` | `#3A2818 → #140C07 58% → #291A0C` | ⚔ |
| SOLO | `#8FC6D2` | `#1B3A43 → #07161A 58% → #122A31` | ◈ |
| SHOP | `#E0A07A` | `#3E2018 → #170A06 58% → #2C130C` | ✦ |
| DECKS | `#B9A3E0` | `#2A2148 → #0D0916 58% → #1D1633` | ❖ |

Tile anatomy top → bottom:
1. **Emblem** (flex-centred) — 132 × 132: a rotated-45° square, 2px accent @55% with
   `linear-gradient(135deg, accent@16%, transparent 68%)`; a second rotated square at
   `inset: 26`, 1px accent @35%; the glyph on top, Cinzel 700 40–42px.
   *These glyphs are placeholders — replace with real icon art.*
2. **Title** — Cinzel 700 **30px**, letter-spacing .06em.
3. **Kicker** — Oswald 500 10px / .22em in the accent's muted tone.
4. **Parchment strip** — `margin: 0 8px 8px`, `padding: 11px 14`, `#EBE1C7 → #D9CCAB`,
   1px in the tile's dark accent. Spectral 400 13/1.35 `#2E2417` + a Cinzel `→` on the right.
   **This strip always carries live context, never a static tagline** — season countdown,
   trial progress, new-set name, collection completion. It is the reason the menu is worth
   looking at.

Hover: `translateY(-12px)` + `0 34px 70px rgba(0,0,0,.78), 0 0 40px accent@32%`, 160 ms ease-out.
Keyboard focus must use the same treatment plus a 2px `#EBCE8A` outer ring.

### Bottom rail
`padding: 0 64px 26px`, gap 20. Two flexible panels (`rgba(0,0,0,.42)`, 1px
`rgba(200,164,92,.3)`, radius 7, `padding: 16px 20`) and a right-aligned meta stack.

**Active deck** — 44 × 62 card-back thumbnail (7px weave pitch), then label
(Oswald 500 10px / .22em), deck name (Cinzel 600 19px), composition line
(Spectral 13px `#8C7B5F`), and a `SWITCH` ghost button.

**Daily seal** — label + reset countdown; a **7-segment bar** (gap 5, height 7 — filled
segments `#8E6A22 → #F3DDA4`, empty `rgba(200,164,92,.16)`); reward line; then either a gold
`CLAIM` button (`0 0 24px rgba(235,206,138,.28)`) or, once claimed, a static
`CLAIMED` chip in `#7ACD96` on 1px `rgba(122,205,150,.5)`.
Claiming increments the coin counter in the top bar — animate that count (~600 ms).

**Meta stack** — version string (Oswald 500 10px / .20em `#5C513F`) and the pulsing online
counter.

---

## Matchmaking overlay

Triggered by PLAY and SOLO. `position: absolute; inset: 0; z-index: 20`, scrim
`rgba(7,5,3,.88)`, centred 560-wide panel with the same relic frame as the auth panel
(`padding: 44px 46`, gap 26).

- **Spinner** — 120 × 120: a static rotated-45° square (2px `rgba(200,164,92,.2)`), a
  circular ring with only `border-top`/`border-right` coloured (`#EBCE8A` / `#C8A45C`)
  spinning at 1.5s linear, and a 26px gold diamond core with `0 0 26px` glow.
- **Title** — Cinzel 600 26px `#F1DFB8`. Copy differs by mode:
  `Searching for a duelist` / `Waking the Warden`.
- **Note** — Spectral 15/1.5 `#A2917A`, centred. Tells the player what to expect
  (rank band and average wait, or the trial's deck) rather than just spinning.
- **Progress bar** — full width, height 6, `rgba(0,0,0,.5)` + 1px `rgba(200,164,92,.3)`;
  a 40%-wide `transparent → #F3DDA4 → transparent` band sweeping (1.6s ease-in-out).
  Indeterminate on purpose.
- **CANCEL** — ember-bordered ghost button, always reachable.

On match found: the panel should snap to a "Duelist found — \<name>, Rank 12" state for
~1.2s before the board loads.

---

## Navigation & state

```ts
type Screen = 'login' | 'menu';
type AuthMode = 'signin' | 'register';
type Matching = null | 'online' | 'solo';

interface ShellState {
  screen: Screen;
  auth: { mode: AuthMode; name: string; showPassword: boolean; remember: boolean;
          busy: boolean; error?: string; connected: boolean };
  player: { name: string; rank: number; coins: number; decks: number; activeDeck: string };
  daily: { day: number; total: 7; reward: number; claimed: boolean; resetsInMs: number };
  matching: Matching;
}
```

Flow: `login` —(submit, ~950 ms busy)→ `menu`; `login` —(continue offline)→ `menu`;
`menu` —(log out)→ `login` (clears `matching`); `menu` —(PLAY / SOLO)→ overlay → board.

Screen transition: crossfade 200 ms plus a 12px upward drift on the incoming screen. The
ember layer and the background do **not** transition — they persist across the cut, which is
what makes the shell feel like one place.

---

## Design tokens (shell-specific)

Card palettes, attribute pips and the type scale come from the card README. New here:

| Token | Value |
|---|---|
| Shell background | `#2A1C12` → `#0A0705` (radial) |
| Vignette | `inset 0 0 240px rgba(0,0,0,.85)` |
| Panel fill | `#3A2818` → `#140C07` 58% → `#291A0C` |
| Panel shadow | `0 40px 90px rgba(0,0,0,.8)` + `0 0 60px rgba(200,164,92,.08)` |
| Input fill / border | `rgba(0,0,0,.5)` / `rgba(200,164,92,.4)` |
| Input focus | `#EBCE8A` + `0 0 0 3px rgba(235,206,138,.16)` |
| Muted ink | `#9C8A6A` labels · `#A2917A` body · `#8C7B5F` tertiary · `#6E6046` placeholder |
| Online / success | `#7ACD96` |
| Danger / logout | `#E0603A` border, `#C97A5C` ink, `#F0A98C` hover |
| Shop accent | `#E0A07A` (light tint of the Fire pip `#E0603A`) |
| Tile hover lift | `translateY(-12px)`, 160 ms ease-out |

### Shell spacing scale
`4 · 6 · 7 · 9 · 10 · 12 · 14 · 16 · 18 · 19 · 20 · 22 · 26 · 28 · 34 · 44 · 64 · 120`

## Assets

| Asset | Source | Notes |
|---|---|---|
| `uploads/artwork-1785612438938.png` | supplied by the user | on the login hero card |
| Wordmark | type only (Cinzel 700) | no logotype file yet; the gold gradient is the identity |
| Tile emblems | **missing** | ⚔ ◈ ✦ ❖ are placeholders — commission four icons that read at 132px and at 32px |
| Player avatar | **missing** | the hexagon crest currently shows an initial |
| Background, panels, rivets | none — CSS only | in-engine: one background texture + 9-slice panel sprites |

## Files

| File | What it is |
|---|---|
| `Shell Screens.dc.html` | Login + Main Menu, wired together. **Primary reference for this document.** |
| `Duel Field.dc.html` | The board the menu leads into. |
| `TCG Card System.dc.html` | Card design, annotated template, tokens. |
| `README.md` | Card system handoff — read first. |
| `README-duel-field.md` | Board handoff. |
