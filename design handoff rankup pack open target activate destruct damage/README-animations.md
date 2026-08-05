# Handoff: Animations — Rank Up, Pack Open, Card Cast, Card Destroy, Player Defeat

Companion document to the RELIQUARY bundle (`README.md` card system,
`README-duel-field.md`, `README-shell-screens.md`, `README-collection-screens.md`,
`README-duel-setup.md`, `README-coin-flip.md`, `README-progression.md`).
Same visual language — **Reliquary**. Read `README.md` first for the palette and type scale,
and `README-coin-flip.md` for the motion conventions, which this document extends rather
than repeats.

## What is in here

| Animation | File | Runtime | What it covers |
|---|---|---|---|
| Rank up | `Rank Up.dc.html` | 9.4 s | Any of the nine promotions, 2 → 10 |
| Pack open | `Pack Open.dc.html` | 10.8 s | Sealed pack → five cards, rarity read before the flip |
| Card activation | `Card Cast.dc.html` | 8.8 s | Playing a card and targeting with it |
| Card destruction | `Card Destroy.dc.html` | 7.9 s | A monster breaking and travelling to the graveyard |
| Player defeat | `Player Defeat.dc.html` | 9.4 s | The final direct attack on a player card |

All five are **1280 × 720** and authored as five scenes each. Every scene is retimable: the
motion is a function of that scene's normalised progress `p ∈ [0,1]`, so changing a duration
stretches the beat instead of clipping it.

---

# 0 · Shared motion vocabulary

These are the same three curves used across the whole game. No other easing appears anywhere.

```
enter(t) = 1 − (1 − t)³                       // ease-out cubic  — launches, entrances
drift(t) = 0.5 − 0.5·cos(π·t)                 // ease-in-out sine — camera, arcs
pop(t)   = 1 + 2.9·(t−1)³ + 1.9·(t−1)²        // ease-out back    — impact overshoot
seg(p, a, b) = clamp01((p − a) / (b − a))      // remap a sub-window of p onto 0…1
```

Unity: `enter` → `1-Mathf.Pow(1-t,3)`; `drift` → `Mathf.SmoothStep`; `pop` → `Ease.OutBack`
with overshoot **1.9**.

### The boundary rule
Every scene's **first rendered frame equals the previous scene's last frame**. Each animation
below carries a table of the quantities that are pinned at its seams. If you implement scenes
as separate Unity states, verify the seams — a mismatch shows as a visible pop at the cut.

### Two techniques reused across animations

**The wedge shatter** (Card Destroy, Player Defeat). A card breaks into six wedges cut from
*one* card with `clip-path`, each carrying its own clipped copy of the card, so the artwork
breaks with the frame instead of a separate crack overlay sliding over it. The crack lines
drawn in the preceding beat are the same six lines the wedges separate along.

```
w1 polygon(0 0, 52% 0, 44% 46%, 0 38%)          dir (−0.95, −1.15)  spin −20°
w2 polygon(52% 0, 100% 0, 100% 30%, 44% 46%)    dir ( 1.00, −1.10)  spin  24°
w3 polygon(0 38%, 44% 46%, 30% 100%, 0 100%)    dir (−1.20,  0.50)  spin −14°
w4 polygon(44% 46%, 100% 30%, 100% 64%, 62% 100%) dir ( 1.20, 0.42) spin  18°
w5 polygon(30% 100%, 44% 46%, 62% 100%)         dir (−0.10,  1.30)  spin   7°
w6 polygon(100% 64%, 100% 100%, 62% 100%)       dir ( 1.05,  1.15)  spin  29°
```

All six radiate from the fracture origin at **(44 %, 46 %)** — slightly above and left of
centre, which reads as a struck point rather than a symmetric split.
In Unity: one quad per wedge with the card's render texture and matching UV clipping, or a
pre-authored six-piece mesh.

**Deterministic particles.** Every ash fleck, ember and spark uses a **fixed** per-particle
offset and a phase derived from scene progress — never `Random`. The animations have to be
frame-reproducible to export as video, and a random field cannot be scrubbed.

---

# 1 · Rank up (9.4 s)

Plays after the result screen when a duel pushes the player across a rank threshold.
Default shown: Gold Seal V → Obsidian Seal I.

## Scenes

| # | Scene | Dur | `fill` in → out | RP | old seal | new seal |
|---|---|---|---|---|---|---|
| 1 | **Result** | 1.6 s | .95 → .95 | rp₀ | intact | — |
| 2 | **Award** | 1.8 s | .95 → 1.0 | rp₀ → rp₁ | intact | — |
| 3 | **Break** | 1.4 s | 1.0, then out | rp₁ | 1 → 0 | — |
| 4 | **Forge** | 1.8 s | hidden | rp₁ | gone | 0 → 1 |
| 5 | **Reveal** | 2.8 s | new bar | rp₁ | gone | complete |

`rp₀` = the new rank's floor − 20; `rp₁` = `rp₀` + gain. So the win always lands 5 RP past the
threshold, which is the case worth animating.

## One generic seal, ten ranks
The emblem is **not** ten assets. One component takes the rank number, pulls its metals from
the rank table (`README-progression.md`), and gates its layers by rank — so the forged seal
always has exactly one layer more than the shattered one. That is the whole point of the beat:
the player sees the emblem gain a piece.

- **Break** uses the wedge idea on the emblem's outer diamond: four quadrant clips fly along
  their diagonals, corner pips shoot outward, the core flashes and scales, each layer dying on
  its own threshold (`dieOut(0.5)` for the core through `dieOut(0.85)` for the rings).
- **Forge** assembles inward-out on staggered windows of `forge`:
  core .00–.26 → outer .16–.50 → inner .32–.62 → axis square .44–.70 → pips .56–.82 →
  ring .66–.94 → spokes .74–1.0. Each uses `pop` so it snaps rather than fades.
- The stage tint crosses from the old rank's dark tone to the new one's during Break.

## Layout constraints (learned the hard way)
- Emblem centre **y 336**, radius **182**. The rotated outer diamond's bounding box is
  `out × √2 = 1.196 r`, i.e. wider and taller than the r box — the banner must clear that,
  not the box.
- The sub-rank pips get their **own band at y 462**, above the RP bar. They must never sit
  inside the emblem: they are the same colour as the emblem's corner pips and the two sets
  become indistinguishable.
- Pips are anchored **from the centre in px**, never inset from the r-box edge. Centre them
  with margins, not `translate(-50%)` — a `rotate(45deg)` earlier in the transform list sends
  a subsequent translate off diagonally.

## Rewards
Pack + coins (`100 + rank × 25`), plus a cosmetic unlock at ranks 6, 8 and 10 only. Vault Seal
shows `top 8 000 · ranked by placement` where the RP cap would be, because rank 10 has none.

---

# 2 · Pack open (10.8 s)

## Scenes

| # | Scene | Dur | pack | spread | flip |
|---|---|---|---|---|---|
| 1 | **Seal** | 1.8 s | intact, shaking | stacked | 0 |
| 2 | **Tear** | 1.3 s | 0 → split | stacked | 0 |
| 3 | **Fan** | 1.9 s | gone | 0 → 1 | 0 |
| 4 | **Flip** | 2.6 s | gone | 1 | 0 → 1 |
| 5 | **Hold** | 3.2 s | gone | 1 | 1 |

Five slots at `CX + (i − 2) × (176 + 22)`, card 176 × 246, row centre y 336.

## The rarity tell comes BEFORE the flip
This is the design decision the whole animation is built around. Each face-down card sits in a
**flame field** whose size, count and reach are set by rarity, so the player knows what is
coming while the card is still face down. The anticipation is in the burn, not the reveal.

```
tongues:  common 5 · rare 7 · epic 9 · relic 13
spread:   card width × 1.24, rooted 8px above the card's lower edge
height:   CH × lerp(0.52, 1.06, rarityWeight) × centreFalloff × flicker
centre:   max(0.2, 1 − |u − 0.5| × 1.55)        // middle tongues tallest
flicker:  0.6 + 0.4·sin(2π(t·1.7 + i·0.29))     // fixed per-tongue phase
sway:     skewX( sin(2π(t·1.1 + i·0.41)) × 6deg )
shape:    clip-path polygon(50% 0, 82% 34%, 100% 72%, 74% 100%, 26% 100%, 0 72%, 18% 34%)
fill:     linear-gradient(180deg, transparent, c@16% 20%, c@58% 60%, c@92%)
blur:     lerp(1.5, 4.5, 1 − centre) px          // edge tongues softer
core:     an inner tongue at 44% width, 52% height, pale, only strong on epic/relic
embers:   epic and relic only — 5 diamonds rising on fixed phases
```

Rarity weight: common 0, rare 0.25, epic 0.5, relic 1. A relic burns **past the top of the
card**; a common barely licks its lower edge.

In Unity: a small additive particle system per card with the rarity colour, plus a scrolling
noise-masked quad for the tongues. Keep the per-tongue phases fixed if the sequence is
recorded for marketing.

## Flip
Cards flip on a 0.09 s stagger, each over a 0.44 window, `rotateY 0 → 180°` eased with
`drift`. A `perspective: 1400px` wrapper with `backface-visibility: hidden` on both faces.
The kick — `scale +8 %` and a lift of 10–26 px scaled by rarity — peaks mid-turn.

**The flames fade as the card turns** (`1 − clamp(f / 0.55)`): the reveal consumes them, and
the frame colour takes over as the rarity tell. Do not let both run at once.

## Hold
The relic lifts 22 px, breathes on a 1 Hz glow, and carries a `RELIC · NEW` chip above it.
The epic card runs the **Glossy** sweep from `README-progression.md` — a pull is a good place
to show a finish off.

## Duplicates
```
1 new card · 4 duplicates stored · 1 Glossy pull
Turn duplicates into crafting material in the Deck Builder.
```
**Duplicates are never auto-converted and give no shards.** They stay in the collection; the
player converts them into crafting material in the Deck Builder, at their own pace.

---

# 3 · Card activation and targeting (8.8 s)

## Scenes

| # | Scene | Dur | card position | `charge` | thread | reticle |
|---|---|---|---|---|---|---|
| 1 | **Idle** | 1.5 s | in hand | 0 | 0 | 0 |
| 2 | **Lift** | 1.4 s | hand → y 356, scale 1.62 | 0 → .4 | 0 | 0 |
| 3 | **Activate** | 1.6 s | → field slot, scale 1 | .4 → 1 | 0 | 0 |
| 4 | **Target** | 2.1 s | slot | 1 → .6 | 0 → 1 | 0 → 1 |
| 5 | **Resolve** | 2.2 s | slot | .6 → 0 | 1 → 0 | 1 → 0 |

Field geometry: five zones per side at x = 372, 470, 568, 666, 764; opponent row y 178, player
row y 452; card 132 × 185 at field scale; centre line y 316.

## `charge` is the activation tell
A single scalar drives three things at once, which is why activation reads instantly:
the frame goes from the card's own edge colour to `#F8EED6`, an inner glow of `20 × charge` px
appears, and the **parchment effect box brightens** to `#F8EED6 → #EBE1C7` with a light border.
The card's text panel lighting up is the clearest possible "this effect is happening now".

## Lift
The card leaves the hand, travels to screen centre at **1.62 ×** scale, and the rest of the
board dims to 55 %. Full read time on the card before anything happens to it — this is where
the player confirms what they are playing.

## Activate
Slam on `enter(seg(p, 0.16, 0.54))`. On impact: a flat ellipse ring (0.42 aspect, reads as
ground-plane), a second 45° diamond ring on an 0.18 delay, a white flash, and a **0.7-amplitude
screen shake**. The board un-dims by 45 % as the spell takes hold.

## Target
- **Thread** — a 2 px line from the caster's upper edge to the target, `length × t`, gradient
  from `#E0603A@10%` to `#F3C3A6@85%`, 12 px glow. It stays at 45 % opacity once locked.
- **Reticle** — four corner brackets closing from **120 px out to 8 px** on `enter`, plus a
  centre diamond spinning 120° → 0° and scaling 1.6 → 1. Brackets are 26 px arms, 3 px,
  `#E0603A`, with an 8–24 px glow that grows with the lock.
- The reticle travels from the caster to the target over `seg(p, 0.12, 0.52)`, so the player
  sees *where the targeting came from*.

Label: `SELECT TARGET` with the effect text as subtitle — the reason the target is legal.

## Resolve
Target flashes ember at 50 % screen-blend, shrinks 6 %, a diamond shock ring expands to 520 px,
`DESTROYED` rises 74 px and fades, the reticle and thread release over `seg(p, 0.2, 0.5)`, and
the board returns to full brightness. Then the destruction animation (§4) takes over.

---

# 4 · Card destruction and the graveyard (7.9 s)

## Scenes

| # | Scene | Dur | wedges | drain | courier | graveyard |
|---|---|---|---|---|---|---|
| 1 | **Struck** | 1.4 s | intact | 0 | — | 7 |
| 2 | **Shatter** | 1.4 s | 0 → 150 px out | 0 → 1 | — | 7 |
| 3 | **Gather** | 1.3 s | back → 0, shrinking | 1 | appears | 7 |
| 4 | **Flight** | 1.6 s | gone | 1 | arcs across | 7 |
| 5 | **Land** | 2.2 s | gone | 1 | on the pile | 7 → 8 |

Destroyed card at (568, 452); graveyard at (1128, 556). Shown for the player's own monster —
the opponent's side is the same motion mirrored across the centre line.

## Struck
Ember flash at 50 % screen-blend, 0.8-amplitude shake, a diamond shock ring, then the **six
crack lines** draw outward from (44 %, 46 %) with `scaleX` on a `transform-origin: 0 50%` —
pale core, ember tail, 8 px glow. The cracks are the promise the next scene keeps.

## Shatter
The six wedges burst along their directions. Two things happen to the colour on the way out:
**`drain`** takes the frame from its rarity/type edge to ash `#8A857B` and desaturates the
whole piece (`saturate(1 − drain × 0.9)`), so the card visibly stops being a live card. Nine
ash flecks fly on fixed angles, alternating ember and ash.

## Gather
The interesting inversion: the wedges fly **back in** (`fly: 118 → 0`), un-spin, shrink to
72 % and fade, while a violet ring contracts from 420 px to 90 px and a face-down card **pops**
into being at the convergence point. A destroyed card becomes an anonymous card back — that is
what the graveyard holds.

## Flight
One eased arc, no keyframed path:
```
t = drift(p)
x = lerp(from.x, grave.x, t)
y = lerp(from.y, grave.y, t) − sin(π·t) × 168 × arcHeight
scale = lerp(0.72, 0.62, t) × (1 + sin(π·t) × 0.16)
spin  = enter(p) × 382°          // just past one full turn, so it lands square
```
The trail is **four fixed samples of the same arc** at lags 0.10 / 0.19 / 0.29 / 0.40 —
diamonds of 16 → 7 px, violet, fading. Sampling the same function means the trail can never
drift off the path.

## Land
Settle over `seg(p, 0, 0.26)` — the card drops the last 26 px and rotates 22° → 1.5° onto the
stack. Then: a diamond ring to 340 px, the counter **7 → 8** popping from 22 to 27 px with an
18 px violet glow, the pile's ambient glow up to 0.62, and a chip reading
`1 card sent · 8 in graveyard`.

The pile itself is up to six card backs at 62 % scale, each offset 3 px up and rotated
`±(1.2 + i × 0.5)°` — a stack that looks handled, not stacked by a machine.

---

# 5 · Player defeat (9.4 s)

**Player cards** are the design premise here: the player is represented by a card, and a direct
attack hits that card. So losing uses the same grammar as any other destruction — struck,
cracked, shattered — only slower, and the board goes with it.

## The player card
Same frame language as a monster, with three substitutions:

| Monster card | Player card |
|---|---|
| card name plate | **player name** (Cinzel 600, `h × 0.05`) |
| level crest | account level |
| Type / Attribute row | `DUELIST` + **rank chip** (`GOLD SEAL III`) |
| effect box + DMG/DEF | **LIFE block**: label, big total, and a fraction bar |

Size 200 × 280, 3 px border, four corner diamonds at 10 px inset.

**The frame colour is the health bar.** `critical = 1 − clamp(lp / 1200)` drives the border from
gold `#C8A45C` toward ember `#E0603A`, adds an inner ember glow to the LIFE block, and shifts
the number from `#F8EED6` to `#F3C3A6`. At 400 life the card is unmistakably ember from across
the table without reading a digit.

**Layout constraint:** the content column must be a **definite-height flex container**
(`height: 100%`) with the artwork on `flex: 1 1 auto; min-height: 0; aspect-ratio: 1`. Sizing
the artwork from the card's *width* makes the column ~21 px taller than the card's content box
and the LIFE bar falls out of `overflow: hidden`. The artwork absorbs the leftover space; every
other child is `flex: none`.

## Scenes

| # | Scene | Dur | LP | attacker | card | ash |
|---|---|---|---|---|---|---|
| 1 | **Brink** | 1.8 s | 400 | raised, y 181 | intact, pulsing | 0 |
| 2 | **Strike** | 1.7 s | 400 → 0 | lunges to y 234 | cracking | 0 |
| 3 | **Break** | 1.5 s | 0 | fading out | shatters | 0 → .5 |
| 4 | **Collapse** | 1.2 s | 0 | gone | falls away | 1 |
| 5 | **Defeat** | 3.2 s | 0 | gone | gone | 1 |

Player card centre (640, 476); attacker 120 × 168 at rest y 181.

## Brink
A 2 Hz pulse on everything at once: the attacker's glow, an ember halo behind the player card,
and a diamond ring expanding and fading around it. Label: `DIRECT ATTACK` /
`400 life left · no monsters to block` — it names *why* this is lethal.

## Strike
**Geometry that matters.** The attacker renders at `scale(1.14)` at full lunge, so its stop is
derived from the **scaled** height:
```
LUNGE_Y = PC.y − PC.h/2 − (FOE.h × 1.14)/2 − 6
```
Its bottom edge lands 6 px above the player card's top edge, which keeps the **name plate**
readable through the blow — the plate is what makes it a player card rather than a monster, and
covering it during the kill loses the point of the beat. The attacker is also emitted **after**
the player card so it paints in front; behind it, it reads as a stray fragment.

LP drains over `seg(p, 0.42, 0.82)`, the `−2 600` figure rises 70 px to the right of the card
(clear of its border), and the crack lines draw from 0.52 onward.

## Break and Collapse
Wedges of the player card, at the same six shapes. The **field itself** desaturates and dims
(`saturate(1 − ash × 0.9) brightness(1 − ash × 0.42)`), twenty-two ash flecks start falling on
fixed offsets, and in Collapse the wedges keep falling (`fall` to `p = 0.92`) while staying
visible to `p = 0.9`. The blackout only starts at `p = 0.8` and reaches 50 % — a near-black
frame in the middle of the sequence reads as the animation having stopped.

## Defeat
`THE SEAL HOLDS` / **DEFEAT** (Cinzel 700, 104 px) / `Kestrel_09 wins on turn 11`, then the
consequences as chips: `−25 RP · Gold Seal III`, `Gold Seal I is your floor`,
`Daily Seal 4 of 7 kept`. Naming what was *not* lost is the difference between a defeat screen
and a punishment screen.

---

# 6 · Integration

```csharp
// Rank up — see README-progression.md for the rank table
public IEnumerator PlayRankUp(int intoRank, int rpGain);   // intoRank 2..10

// Pack open
public enum Rarity { Common, Rare, Epic, Relic }
public enum Finish { Plain, Glossy, Rainbow, Static }
public IEnumerator PlayPackOpen(PullResult[] five);        // decided server-side, in order
public event Action OnCardsRevealed;                       // end of Flip — hand control back

// In duel
public IEnumerator PlayActivation(CardId card, int zone);
public IEnumerator PlayTargeting(CardId source, CardId target);
public IEnumerator PlayDestruction(CardId card, bool mine);
public event Action<CardId> OnEnteredGraveyard;             // end of Land — update the counter

// Defeat
public IEnumerator PlayDefeat(string winner, int turn, int rpDelta);
```

**Rules**
- Every outcome is authoritative **before frame one**: the five pack cards, the destruction,
  the lethal damage. Nothing is decided inside an animation.
- Deterministic — no `Random` in any of these sequences. Particle offsets are constants.
- Skips: pack open may skip to the start of **Hold**, never past the flip. Defeat may skip to
  the start of **Defeat**. Destruction and activation are short enough to play through.
- The rarity flames are a **gameplay-relevant tell**, not decoration. If a reduced-motion
  setting is on, hold them at a still frame — do not remove them.
- Audio cues (ms into each animation): pack `tear` 1.9 s, five `flips` from 3.2 s on the same
  0.09 s stagger, `relic chime` 5.8 s · activation `slam` 2.9 s, `lock` 4.6 s, `hit` 5.2 s ·
  destruction `crack` 0.5 s, `shatter` 1.5 s, `land` 5.8 s · defeat `impact` 2.5 s,
  `shatter` 3.5 s, `toll` 6.2 s.

## Files

| File | What it is |
|---|---|
| `Rank Up.dc.html` + `rank-up.jsx` | Promotion, all nine transitions via one generic seal |
| `Pack Open.dc.html` + `pack-open.jsx` | Pack opening with the rarity flame field |
| `Card Cast.dc.html` + `card-cast.jsx` | Activation and targeting |
| `Card Destroy.dc.html` + `card-destroy.jsx` | Wedge shatter and the graveyard arc |
| `Player Defeat.dc.html` + `player-defeat.jsx` | Player card, direct attack, defeat screen |
| `animations-v2.jsx` | Timeline engine (starter, unmodified) — not part of the design |
| `tweaks-panel.jsx` | Tweak controls (starter, unmodified) — not part of the design |

Each `.dc.html` declares its scene list and durations in inline scripts at the top; the
`.jsx` beside it holds every formula in this document, as code. **Read the `.jsx` next.**
