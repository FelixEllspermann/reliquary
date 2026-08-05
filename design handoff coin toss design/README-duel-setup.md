# Handoff: Duel Setup — Online Duel & Solo Trial

Companion document to `README.md` (card system), `README-duel-field.md` (board),
`README-shell-screens.md` (login & menu) and `README-collection-screens.md`
(deck builder & shop). Same visual language — **Reliquary**.

## Overview
The screen between the Main Menu and the duel board: pick a deck, pick how you want to play,
queue. It replaces two separate legacy dialogs (an "Online Duel" panel and a "Solo — gegen den
Bot" panel) with **one screen and a mode tab**, because both did the same job — choose a deck,
press start — and only differed in the opponent.

Designed at a fixed **1920 × 1080**, scaled uniformly to the viewport (letterbox, no reflow).

## About the Design Files
`Duel Setup.dc.html` is a **design reference created in HTML**. The interactivity (deck
selection, legality gating, lobby-code generation, difficulty, search overlay) exists so the
flow can be reviewed — it is not an implementation. No networking, no matchmaking, no
persistence. Recreate in the target engine.

## Fidelity
**High fidelity.** Sizes, colours, type and timings are final. Placeholder content: deck
names and compositions, MMR/rank numbers, trial copy, reward amounts, the generated lobby code.

---

## What changed from the old screens, and why

| Old | New | Reason |
|---|---|---|
| Native `<select>` for the deck | A list of **deck cards** with thumbnail, hero, composition, count bar and attribute spread | The deck is the most consequential choice on the screen; a one-line dropdown gave it the least weight and hid whether the deck was even legal |
| No legality feedback | Green `LEGAL` / red `TOO FEW CARDS` badge, dimmed row, disabled start, plain-language error strip | Players found out their deck was illegal *after* pressing start |
| Two separate screens | One screen, two tabs | Identical job; switching modes no longer costs a back-navigation |
| Nothing about the opponent | Mode banner with MMR band, season record / trial briefing, bot attributes and reward | Deciding to queue needs to know what you are queueing into |
| Bare `CODE` box + `GO` | Code field that unlocks at exactly 6 characters; lobby code rendered on parchment with Copy/Close and a live waiting state | The old pair gave no feedback about validity or lobby status |
| Generic browser buttons | Reliquary keyline / parchment / gold-primary grammar | Matched nothing else in the game |

---

## Shared chrome

Background: the shell stack —
`radial-gradient(ellipse 1500px 820px at 50% 45%, #2A1C12, #0A0705 78%)`, ±45° weave at
`rgba(200,164,92,.04)` / 28px, `inset 0 0 240px rgba(0,0,0,.85)` vignette, plus **three ember
particles** (5–7px rotated diamonds, 11–13s, staggered) — fewer than the login screen, since
this is a decision surface rather than a title screen.

**Top bar — 96 high**, `padding: 0 48`, 1px bottom rule `rgba(200,164,92,.25)`.
Left: diamond + 25px wordmark (seamless 90° gold gradient, `shimmer 9s`) + the **mode tabs**.
Right: the player plate (36 × 40 hexagon crest, name Cinzel 600 15px, `RANK 12 · 1 480 MMR`
in Oswald 9px / .18em) and a `← MENU` ghost button.

**Mode tabs** — same segmented control as the collection screens, but the active tab takes the
colour of the mode it selects: **gold** `#E2C685 → #9C7526` / 1px `#EBCE8A` for Online,
**teal** `#A5D8E2 → #3B7C8B` / 1px `#8FC6D2` for Solo. That accent then propagates through the
entire right column, so the screen's temperature tells you which mode you are in.

Content area 984 high, `padding: 26px 48`, two columns, **gap 26**:
deck picker **640** fixed, mode panel takes the rest. Both **932 high**.

---

## Deck picker (left, both modes)

Panel: radius 9, 1px `rgba(200,164,92,.35)`, `rgba(30,20,12,.75) → rgba(10,7,5,.75)`.

**Header** — `Choose your deck` (Cinzel 600 21px) + `N decks · only legal decks can duel`
(Spectral 13px `#8C7B5F`).

**Deck row** — `padding: 14px 16`, radius 7, gap 16:

| Part | Spec |
|---|---|
| Thumbnail | 74 × 104 card back, radius 4, 1px `rgba(200,164,92,.55)`, weave 9px pitch, 44px rotated frame, and a **20px gem in the deck's own colour** — the only per-deck identity mark |
| Name | Cinzel 600 22px `#F1DFB8`, ellipsis |
| Legality badge | `LEGAL`: `rgba(122,205,150,.14)` + 1px `rgba(122,205,150,.5)`, ink `#7ACD96`. Illegal: the ember equivalent, ink `#E9A183`, label states the fault (`TOO FEW CARDS`) |
| Meta line | `Hero: … · N Monster · N Spell · N Artifact`, Spectral 13px `#8C7B5F` |
| Count bar | 7px track, fill `#8E6A22 → #F3DDA4` (legal) or `#7A3218 → #E9A183` (illegal) at `count / 80`; count in Cinzel 700 18px, `#7ACD96` / `#E9A183` |
| Attribute spread | a 6px stacked bar, 3px gaps, one segment per attribute in its own colour |
| Select mark | 34px circle — selected: gold gradient + 1px `#EBCE8A` with a `✓`; else 1px `rgba(200,164,92,.25)` on `rgba(0,0,0,.4)`, empty |

Row default 1px `rgba(200,164,92,.2)` on `rgba(0,0,0,.38)`; **selected** 2px `#C8A45C` on
`rgba(200,164,92,.13)` + `0 0 26px rgba(200,164,92,.16)`. Illegal rows render at
**opacity .62** but stay selectable — the player must be able to inspect why.

**Footer** — one Spectral line ("you cannot swap mid-duel") plus a `DECK BUILDER` ghost button,
so a broken deck is one click from being fixed.

---

## Mode panel (right)

### Mode banner — 250 high
radius 10, **2px** in the mode accent, `padding: 0 34`, gap 32, `overflow: hidden`.
Fill `linear-gradient(110deg, …)` — gold `#3A2818 → #140C07 46% → #291A0C`, teal
`#1B3A43 → #07161A 46% → #122A31`. Inner keyline at `inset: 6`; one 360px rotated square
bleeding off the top-right at 40% opacity.

Left: a **132 × 172 opponent card** (recoloured card back, 46px glowing gem) on
`float 6.5s ease-in-out infinite`.
Middle: eyebrow (Oswald 500 10px / .26em) · title **Cinzel 700 40px** · Spectral 15/1.5 blurb
(max-width 560) which **restates the current difficulty in Solo mode**.
Right: two stat readouts — Online `YOUR MMR` / `SEASON W/L`; Solo `TRIAL` / `BEST STREAK`.
Label Oswald 9px / .18em, value Cinzel 700 24px.

### Primary start button — 104 high
Full width, radius 8, `padding: 0 30`, `justify-content: space-between`.
Enabled: mode gradient + 2px light keyline, ink `#1E1405` / `#04191D`,
`0 12px 30px rgba(0,0,0,.6), inset 0 1px 0 rgba(255,255,255,.35)`.
Inside: a 16px rotated diamond, a two-line label (**Cinzel 600 26px** title over an
Oswald 10px / .20em subline carrying the real parameters — MMR band and average wait, or
difficulty + opponent + LP) and a Cinzel `→`.
**Disabled** (illegal deck): 2px `rgba(200,164,92,.2)`, `rgba(0,0,0,.4)`, ink `#5C513F`,
`cursor: not-allowed`, no shadow.

### Online — two option cards
Each `flex: 1`, `padding: 22px 24`, radius 8, 1px `rgba(200,164,92,.35)` on `rgba(0,0,0,.42)`,
gap 14, headed by an 8px diamond + Oswald 10px / .24em label.

**Private lobby** — one Spectral paragraph, then either:
- `CREATE LOBBY` (gold ghost, `margin-top: auto`), or
- the **generated code**: a parchment strip (`#EBE1C7 → #D9CCAB`, 1px `#8C7440`) with
  `LOBBY CODE` left and the code in **Cinzel 700 26px, letter-spacing .22em** right; below it
  `COPY` and `CLOSE` (ember) side by side, and a `#7ACD96` pulsing dot with
  "Waiting for your opponent to join…".
  Codes are 6 characters from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` — **I, O, 0 and 1 are
  excluded** so a code can be read aloud without ambiguity.

**Join with a code** — a 56-high input in **Cinzel 700 24px, letter-spacing .22em**,
`text-transform: uppercase`, `maxlength 6`, placeholder `ABC123`. The `JOIN DUEL` button is
gold only at **exactly 6 characters and a legal deck**; otherwise the disabled treatment.

### Solo — difficulty + briefing
**Difficulty** card: three equal buttons, radius 5, each a name (Cinzel 600 17px) over a note
(Oswald 9px / .16em at 75% opacity). Active: 2px `#8FC6D2` on `rgba(143,198,210,.16)`,
ink `#E4F4F8`; inactive 1px `rgba(143,198,210,.25)` on `rgba(0,0,0,.4)`, ink `#7E8E94`.

| Difficulty | Note |
|---|---|
| Novice | Plays openly · no traps |
| Warden | Full deck · real AI |
| Sealed | +2 mana · 12 000 LP |

**Trial briefing** card: the bot's attributes as pill chips in their own colours
(`WATER`, `LIGHT`, plus a neutral subtype chip), a Spectral paragraph of **actual tactical
advice** ("stalls behind high-DEF walls… bring removal for Level 2"), and a 250-wide parchment
reward panel (`FIRST CLEAR REWARD`, coin diamond + Cinzel 700 22px, one line on what it
unlocks). Pinned at the bottom: a trial progress bar (teal fill `#2E7381 → #B4E2EC`) with
`6 / 20`.

### Illegal-deck strip
When the selected deck is illegal, a strip appears under the options:
`rgba(224,96,58,.12)`, 1px `rgba(224,96,58,.5)`, radius 5, a 9px `#E0603A` diamond and one
Spectral 14px `#F0A98C` sentence naming the deck, its count, the legal range and where to fix
it. **Never a bare disabled button with no explanation.**

---

## Search overlay

Shared by both modes; identical to the Main Menu's matchmaking overlay except the panel
keyline and the spinner ring take the **current mode's accent**.

`inset: 0; z-index: 30`, scrim `rgba(7,5,3,.9)`, 600-wide relic panel (`padding: 44px 46`, gap 26).
- **Spinner** 120 × 120 — static rotated square (2px `rgba(200,164,92,.2)`), a ring with only
  `border-top` / `border-right` coloured spinning at 1.5s linear, 26px gold core with
  `0 0 26px` glow.
- Title Cinzel 600 26px; note Spectral 15/1.5 centred, max-width 420, stating the rank band
  and wait, or the trial and what the bot plays.
- Indeterminate bar — height 6, a 40% `transparent → #F3DDA4 → transparent` band sweeping 1.6s.
- **Queue receipt** — a `#7ACD96` pulsing dot with "Queued with **\<deck>** · N cards", so the
  player can confirm the right deck went in without cancelling.
- `CANCEL` (ember ghost), always reachable.

On match found: swap to a "Duelist found — \<name>, Rank N" state for ~1.2 s, then load the board.

---

## State

```ts
type Mode = 'online' | 'solo';
type Difficulty = 'novice' | 'warden' | 'sealed';

interface DeckSummary {
  id: string; name: string; hero: string;
  count: number; monsters: number; spells: number; artifacts: number;
  spread: Partial<Record<Attribute, number>>;
  gem: string;                        // deck identity colour
}

interface SetupState {
  mode: Mode;
  deckId: string;
  difficulty: Difficulty;
  lobby: string | null;               // generated 6-char code
  joinCode: string;                   // uppercase, max 6
  searching: null | 'online' | 'solo';
}
```

Derived, never stored: `legal = 40 <= deck.count <= 80`, every button's enabled state, the
banner copy, and the search-overlay copy.

**Gating rules:** an illegal deck disables Quick Match, Join and Start Trial — but never
disables deck selection or the deck-builder link. `JOIN DUEL` additionally requires
`joinCode.length === 6`. Creating a lobby does not queue; it waits.

## Design tokens (setup-specific)

| Token | Value |
|---|---|
| Online accent | `#C8A45C` keyline · `#E2C685 → #9C7526` fill · `#EBCE8A` highlight |
| Solo accent | `#8FC6D2` keyline · `#A5D8E2 → #3B7C8B` fill · `#B4E2EC` highlight |
| Banner — online | `linear-gradient(110deg,#3A2818,#140C07 46%,#291A0C)` |
| Banner — solo | `linear-gradient(110deg,#1B3A43,#07161A 46%,#122A31)` |
| Option card | `rgba(0,0,0,.42)`, 1px accent @ .35 |
| Deck row selected | `rgba(200,164,92,.13)`, 2px `#C8A45C`, `0 0 26px rgba(200,164,92,.16)` |
| Deck row illegal | opacity .62 |
| Legal / illegal | `#7ACD96` / `#E9A183` |
| Disabled primary | `rgba(0,0,0,.4)`, 2px `rgba(200,164,92,.2)`, ink `#5C513F` |
| Code type | Cinzel 700, letter-spacing .22em (input 24px, parchment 26px) |

### Keyframes
| Name | Definition | Used for |
|---|---|---|
| `shimmer` | `background-position 0% → 200%`, 9s linear | wordmark |
| `float` | `translateY(0 → -14px → 0)`, 6.5s | opponent card in the banner |
| `spin` | `rotate(360deg)`, 1.5s linear | search spinner |
| `sweep` | `translateX(-120% → 320%)`, 1.6s | indeterminate bar |
| `pulse` | opacity 1 → .5, 2s | live status dots |
| `ember` | `translateY(0 → -300px)`, opacity 0 → .5 → 0, 11–13s | background particles |

### Spacing scale
`3 · 4 · 7 · 8 · 9 · 10 · 11 · 14 · 16 · 18 · 20 · 22 · 24 · 26 · 30 · 32 · 34 · 48`

## Assets

| Asset | Source | Notes |
|---|---|---|
| Deck thumbnails | none — CSS only | recoloured card backs; real deck cover art would strengthen the picker |
| Opponent card | none — CSS only | placeholder for a hero portrait or opponent avatar |
| Hero portraits | **missing** | the top-bar crest shows an initial; heroes are named but never pictured |

## Files

| File | What it is |
|---|---|
| `Duel Setup.dc.html` | Online Duel + Solo Trial, tabbed and wired. **Primary reference for this document.** |
| `Shell Screens.dc.html` | Login + Main Menu (the screen before this one). |
| `Duel Field.dc.html` | The board (the screen after this one). |
| `Collection Screens.dc.html` | Deck Builder + Shop. |
| `TCG Card System.dc.html` | Card design, annotated template, tokens. |
| `README.md` | Card system handoff — read first. |
