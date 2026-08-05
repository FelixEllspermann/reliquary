# Handoff: Duel Load (turn choice → board)

Companion document to the RELIQUARY bundle. Same visual language — **Reliquary**.
Read `README.md` (card system) first, `README-coin-flip.md` for the scene this follows, and
`README-duel-field.md` for the board it resolves into.

## Overview
The transition after the coin-toss winner has picked first or second. The deck gathers out of
the dark, **tumbles as one mass** — the shuffle — then flings out across the board as the zone
grid resolves underneath, and the turn banner lands.

Reference runs **6.2 s at 1280 × 720**, four cuts. Like the vault entrance this is a **loading
mask** over the real match setup, so its tail must flex.

## About the Design File
`Duel Load.dc.html` + `duel-load.jsx` are a **motion reference built in HTML**. Port the timing
and the choreography, not the DOM. Scrub the timeline to read any value.

## Fidelity
**High fidelity on timing and easing.** Placeholder: the deck name and card count, and the
board grid (a simplified stand-in — the real board is specified in `README-duel-field.md`).

---

## The card model

Seven cards carry the whole animation. Each has **fixed constants** — never `Random`, the
animation has to be frame-identical every play so it can be exported and QA'd:

| Field | Meaning |
|---|---|
| `a0` | fan angle in the gathered stack, degrees |
| `a1` | resting angle once dealt |
| `n` | **whole** orbit turns during the tumble |
| `fl` | **whole** flips during the tumble |
| `dx, dy` | final offset from frame centre |
| `d` | stagger delay, 0 … 0.36 |

```
  a0     a1     n  fl    dx    dy    d
 −34   −13     2   3   −420   118  0.00
 −17    −6.5   3   2   −212   118  0.06
   4     0     2   4      0   118  0.12
  21     6.5   3   3    212   118  0.18
  38    13     2   2    420   118  0.24
 −26     0     3   4   −108  −126  0.30
  29     0     2   3    108  −126  0.36
```

### Why `n` and `fl` must be integers
This is the one non-obvious constraint in the piece. During the tumble a card's orbit angle is
`a = a0·0.35 + swirl·360·n` and its flip is `spin = p·fl`. Because both counts are whole
numbers, at `p = 1` the card is back at **exactly** the angle and the face-parity it started
with — so the next phase can pick it up from the gathered pose with no jump. Fractional counts
produce a visible snap at the 2.8 s cut. (This is the same parity trick the coin toss uses to
decide its landed face.)

---

## Timeline — four phases

| # | Phase | Dur | What moves |
|---|---|---|---|
| 1 | **Gather** | 1.2 s | cards fly in from off-frame and stack |
| 2 | **Tumble** | 1.6 s | the stack orbits and flips as one mass |
| 3 | **Deal** | 1.4 s | cards arc out to their zones; the board fades up |
| 4 | **Settle** | 2.0 s | cards fade into the real board; turn banner lands |

**Total 6.2 s.** Boundaries pinned at 1.2 s, 2.8 s and 4.2 s.

## Motion helpers

The same three curves as the rest of the game, nothing else:

```
enter(t) = 1 − (1 − t)³
drift(t) = 0.5 − 0.5·cos(π·t)
pop(t)   = 1 + 2.9·(t−1)³ + 1.9·(t−1)²
seg(p,a,b) = clamp01((p − a)/(b − a))
```

## Phase 1 — Gather (1.2 s)

Per card, with its own stagger window:
```
t   = enter(seg(p, d·0.5, 0.5 + d·0.5))
x   = lerp(dx·1.9 → 0, t)
y   = lerp(dy·2.4 + 300 → 0, t)
rot = lerp(a0·2.4 → a0·0.35, t)
scale = lerp(0.8 → 1, t)
opacity = t
spin = 0                                    // faces never flip yet

caption "Shuffling / 40 cards · <deck>"  opacity = enter(seg(p, 0.24, 0.60))
```

Cards come in from **beyond the frame and below**, over-rotated, and converge. The stagger
(0.06 per card) is what makes it read as a deck being collected rather than a group animation.

## Phase 2 — Tumble (1.6 s)

```
swirl = drift(p)
wob   = sin(2π·p) · 26

per card:
  a      = a0·0.35 + swirl·360·n
  rad    = sin(π·p) · 96                    // 0 at both ends
  x      = cos(a) · rad
  y      = sin(a) · rad·0.5 + wob·0.2       // ·0.5 = the orbit is elliptical, seen from above
  rot    = a
  spin   = p · fl
  scale  = lerp(1 → 1.06, sin(π·p))
  lit    = sin(π·p) · 0.35

backdrop weave rotates  swirl · 22°
one ornament square:    380 → 620 px, rotating 45 + swirl·90°
caption fades out       1 − enter(seg(p, 0.60, 0.94))
```

The **trudel**: the stack blooms outward into an elliptical orbit, every card flipping at its
own whole-number rate, then collapses back to the centre. Because `rad` and `scale` are both
`sin(π·p)`, the mass returns exactly to the gathered pose. The whole backdrop rotates 22° with
it — that is what sells it as the *table* spinning, not just the cards.

## Phase 3 — Deal (1.4 s)

```
board opacity = enter(seg(p, 0.30, 0.90))

per card:
  t     = enter(seg(p, d·0.9, 0.52 + d·0.9))
  arc   = sin(π·t) · −54                    // cards lift as they travel
  x     = lerp(0 → dx, t)
  y     = lerp(0 → dy, t) + arc
  rot   = lerp(a0·0.35 → a1, t)
  scale = lerp(1 → 0.79, t)
  lit   = (1 − t) · 0.3
  spin  = 0                                 // flipping is done; faces stay down
```

Cards fling out along an arc — the `−54px` lift is what stops it looking like a slide — and
shrink as they reach the zones, reading as settling onto the table. The **board grid fades up
underneath while they are still in flight**, so the player never sees an empty board.

Board stand-in: the warm/cool half tints, the gold centre line, and four rows of five
112 × 157-proportioned dashed zones in the zone accents (see `README-duel-field.md` for the
real thing).

## Phase 4 — Settle (2.0 s)

```
fade = 1 − enter(seg(p, 0.10, 0.62))        // the animated cards hand off to the real board
in   = enter(seg(p, 0.16, 0.50))
out  = 1 − enter(seg(p, 0.90, 1))           // reference loop only

per card: rot = lerp(a1 → a1·0.3, enter(p)), scale = lerp(0.79 → 0.76, enter(p)), opacity = fade
banner:   opacity = in · out, rise = lerp(20 → 0, in)
```

The animated cards straighten slightly and **cross-fade out** — the real board's own card
objects fade in underneath during the same window, so the swap is invisible. Then the turn
banner lands at frame centre: two gold diamonds flanking **Cinzel 700 56px** `YOUR TURN`, over
a parchment strip carrying the consequence of the choice the player just made
(`Draw phase skipped — you chose to go first.`).

Banner copy follows the turn order chosen in the coin toss:

| Chosen | Headline | Parchment note |
|---|---|---|
| First | `YOUR TURN` | `Draw phase skipped — you chose to go first.` |
| Second | `LYRA OPENS` | `You drew one extra card — you chose to go second.` |

---

## Card back

116 × 162 at scale 1, radius 7, 2px keyline. The printed card back, at load scale:
- body `radial-gradient(ellipse at 50% 50%, #4E2A18, #1C0E08 78%)`
- weave ±45° `rgba(200,164,92,.15)` at 10px pitch
- inner keyline `inset: 6`, 1px `rgba(200,164,92,.45)`, radius 3
- centre: a 58px rotated square (1px `rgba(200,164,92,.55)`) and a 24px gem
  `linear-gradient(135deg,#EBCE8A,#7A5A1E)`
- shadow `0 18px 40px rgba(0,0,0,.75)`, plus `0 0 (18+lit·30)px rgba(235,206,138,lit·0.5)`

**Flip rendering** — the same 2D squash trick as the coin, on the other axis:
```
c = cos(spin · 2π)
scaleX     = max(|c|, 0.06)
keyline    = c ≥ 0 ? #C8A45C : #EBCE8A      // a subtle tell that it turned over
```
In Unity, prefer a real 3D card quad with `rotation.y = spin·360°` — the squash is only there
because the reference is DOM.

## Typography

| Role | Font | Size / line | Weight | Colour |
|---|---|---|---|---|
| Caption | Cinzel | 46 / 1.2 | 700 | `#F1DFB8` |
| Caption sub | Spectral | 17 / 1 | 400 | `#A2917A` |
| Turn headline | Cinzel | 56 / 1.2 | 700 | `#F8EED6` |
| Parchment note | Spectral | 16 / 1 | 400 | `#2E2417` |

**Line-height 1.2 minimum on Cinzel.**

---

## Integration

```csharp
public IEnumerator PlayDuelLoad(TurnOrder chosen, DeckSummary deck);

// Fires when the board grid has reached full opacity (t ≈ 3.95 s) — the real board's
// objects should fade in from here so phase 4's cross-fade is invisible.
public event Action OnBoardVisible;

// Fires once the turn banner has settled (t ≈ 5.2 s). Phase 4 HOLDS here.
public event Action OnBannerSettled;
public void ReleaseToDuel();
```

**Rules**
- Phases 1–3 are **fixed length** (4.2 s). Phase 4 is elastic — hold it until the match is
  actually ready.
- `OnBoardVisible` is the important hook: the animation's cards and the board's real cards
  overlap for ~1.2 s. If the handoff is late the player sees cards pop out of existence.
- The seven cards are **decorative** — they are not the player's actual opening hand. The real
  hand is dealt by the board after `ReleaseToDuel()`. Do not try to make them match; seven
  cards read as a shuffle, five would read as a hand and set a false expectation.
- Deterministic: the constants table above is the whole state. No `Random`.
- A **skip** is allowed here (unlike the vault entrance — this plays every match). Skipping
  jumps to `t = 4.20 s`, the start of Settle, so the turn banner is never missed.
- Audio: a soft *gather* whoosh in phase 1, a continuous *riffle* through the tumble rising in
  pitch with `sin(π·p)`, seven staggered *snaps* as the cards land in phase 3, and a low
  *seal* chord under the banner.

## Colour tokens

Inherited. Used here: gold `#C8A45C`, light `#EBCE8A`, pale `#F8EED6`, dark `#7A5A1E`,
table `#241811` → `#0B0705`, opponent tint `rgba(40,62,86,.42)`, player tint
`rgba(96,52,18,.3)`, zone accents `rgba(200,164,92,.4)` / `rgba(143,198,210,.42)` /
`rgba(185,163,224,.42)`, parchment `#EBE1C7` → `#D9CCAB` with ink `#2E2417`.

## Files

| File | What it is |
|---|---|
| `Duel Load.dc.html` | The animation. Scene list lives in an inline script at the top. |
| `duel-load.jsx` | Scene components — every formula above, as code. **Read this next.** |
| `animations-v2.jsx` | Timeline engine (starter, unmodified). Not part of the design. |
| `README-coin-flip.md` | The scene immediately before this one. |
| `README-duel-field.md` | The board this resolves into. |
| `README.md` | Card system handoff — read first. |
