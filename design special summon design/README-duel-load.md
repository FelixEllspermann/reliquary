# Handoff: Duel Load (turn choice → board)

Companion document to the RELIQUARY bundle. Same visual language — **Reliquary**.
Read `README.md` (card system) first, `README-coin-flip.md` for the scene this follows, and
`README-duel-field.md` for the board that takes over once this clears.

## Overview
The transition after the coin-toss winner has picked first or second. The deck gathers out of
the dark, **tumbles as one mass** — the shuffle — then the whole deck **flies off to the right**
and clears the frame.

Reference runs **4.4 s at 1280 × 720**, three cuts. It is a **loading mask** over the real match
setup. It deliberately does not deal cards onto a board: the board's own opening animation owns
that, and playing a fake deal first made the same cards appear twice.

## About the Design File
`Duel Load.dc.html` + `duel-load.jsx` are a **motion reference built in HTML**. Port the timing
and the choreography, not the DOM. Scrub the timeline to read any value.

## Fidelity
**High fidelity on timing and easing.** Placeholder: the deck name and card count.

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
| 3 | **Exit** | 1.6 s | the deck launches right and clears the frame |

**Total 4.4 s.** Boundaries pinned at 1.2 s and 2.8 s.

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

Caption: `Shuffling` (Cinzel 700 46px) over `40 cards · <deck name>` (Spectral 17px `#A2917A`).

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

## Phase 3 — Exit (1.6 s)

```
per card:
  t     = enter(seg(p, d·0.75, 0.60 + d·0.75))
  arc   = sin(π·t) · −46                    // a lift, so it is a throw and not a slide
  x     = lerp(0 → 1180, t²)                // t² — the launch accelerates
  y     = lerp(0 → a1·5, t) + arc           // the fan spreads vertically as it goes
  rot   = lerp(a0·0.35 → a0·0.35 + 26, t)
  scale = lerp(1 → 0.82, t)
  lit   = (1 − t) · 0.3
  spin  = 0                                 // flipping is done; faces stay down

  opacity = 1 − seg(p, 0.46 + d·0.75, 0.60 + d·0.75)   // fades over its last 0.14

caption "Deck sealed"  opacity = enter(seg(p, 0.24, 0.56)) · (1 − enter(seg(p, 0.86, 1)))
```

The stagger is **0.06 per card scaled by 0.75**, so the seven cards leave in a ripple rather
than as a block, and the last one still completes its travel *and* its fade before the phase
ends. That matters: a card still visible at the cut pops.

Two details that carry the throw:
- `x` uses `t²`, not a linear ramp — the deck is being *flung*, so it should still be
  accelerating when it leaves frame.
- `y` spreads by `a1·5`, fanning the deck out vertically as it travels. Without it, seven
  cards on one horizontal line read as a single object.

`x = 1180` puts a card's centre 540 px past the right edge — comfortably clear at scale 0.82.

The caption `Deck sealed` (Cinzel 700 46px `#F1DFB8`) lands as the cards leave and fades with
them, so the frame is empty at the end and whatever comes next has a clean slate.

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

**Line-height 1.2 minimum on Cinzel.**

---

## Integration

```csharp
public IEnumerator PlayDuelLoad(TurnOrder chosen, DeckSummary deck);

// Fires when the last card has left frame (t ≈ 4.40 s) — the frame is empty,
// hand straight over to the board.
public event Action OnDeckCleared;
```

**Rules**
- The whole piece is **fixed length** (4.4 s) — it is choreography, do not retime it at runtime.
  If the match is not ready when it ends, hold on the empty frame; the backdrop is the same
  table the board uses, so a hold is invisible.
- It ends on an **empty frame**. The board's own entrance starts from there — do not overlap the
  two, and do not have this animation deal cards the board will deal again.
- The seven cards are **decorative** — they are not the player's opening hand. Do not try to
  make them match; seven cards read as a shuffle, five would read as a hand and set a false
  expectation.
- Deterministic: the constants table above is the whole state. No `Random`.
- A **skip** is allowed here (unlike the vault entrance — this plays every match). Skipping cuts
  straight to the empty frame and fires `OnDeckCleared`.
- Audio: a soft *gather* whoosh in phase 1, a continuous *riffle* through the tumble rising in
  pitch with `sin(π·p)`, then a single *sweep* as the deck launches right, panning L→R.

## Colour tokens

Inherited. Used here: gold `#C8A45C`, light `#EBCE8A`, pale `#F8EED6`, dark `#7A5A1E`,
table `#2A1C12` → `#0A0705`, card back `#4E2A18` → `#1C0E08`.

## Files

| File | What it is |
|---|---|
| `Duel Load.dc.html` | The animation. Scene list lives in an inline script at the top. |
| `duel-load.jsx` | Scene components — every formula above, as code. **Read this next.** |
| `animations-v2.jsx` | Timeline engine (starter, unmodified). Not part of the design. |
| `README-coin-flip.md` | The scene immediately before this one. |
| `README-duel-field.md` | The board that takes over on the empty frame. |
| `README.md` | Card system handoff — read first. |
