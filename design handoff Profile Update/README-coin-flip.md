# Handoff: Coin Toss Animation → Unity

Companion document to the RELIQUARY bundle (`README.md` card system, `README-duel-field.md`,
`README-shell-screens.md`, `README-collection-screens.md`, `README-duel-setup.md`).
Same visual language — **Reliquary**.

## Overview
The coin toss that decides **who gets to choose the turn order**. It plays after both players
are matched and before the board is dealt: the coin is flicked off the table, spins through an
arc, lands, and the face that comes up names the *winner of the toss* — who then picks whether
to go first or second.

That second step matters for balance: going first sets the pace but costs the turn-one draw, so
the toss winner is making a real decision, not receiving a reward.

Reference runs **7.7 s at 1280 × 720**, authored as four cuts. It is a *cutscene*, not a UI
element — it takes the whole screen and blocks input until it resolves.

## About the Design File
`Coin Flip.dc.html` + `coin-flip.jsx` are a **motion reference built in HTML**. Do not port the
DOM. Port the **timing, the curves, and the coin's look** into Unity using the project's own
rendering (this is a UI cutscene → **Canvas + a 3D coin mesh, or a Canvas-only 2D coin using
the squash trick described below**).

Open the HTML file, scrub the timeline, and read any value off it — every visible quantity is
computed from the formulas in this document.

## Fidelity
**High fidelity on timing and easing** — the numbers below are the spec, match them.
Placeholder: the coin's relief detail (drawn with CSS shapes; in Unity it should be a real
mesh with a normal map, or authored sprite art) and the result copy.

---

## The two quantities that drive everything

The whole animation is two scalars plus a camera. Port these and the motion is correct.

| Symbol | Meaning | Range |
|---|---|---|
| `h` | altitude, 0 = resting on the table, 1 = apex | 0 … 1 |
| `spin` | rotation about the coin's horizontal axis, in **turns** | 0 … 8 |
| `tilt` | rock about the view axis, degrees | −7 … 7 |

Screen mapping in the reference (1280 × 720):
- table line `GROUND = 508` px from the top
- apex travel `RISE = 336` px → `y = GROUND − h · RISE`
- coin diameter `D = 168` px

In Unity, express `h` in world/canvas units instead: `localPosition.y = groundY + h * riseHeight`.
Keep the **ratio** `riseHeight / coinDiameter = 2.0` — that is what makes the toss read as a
real flick rather than a hop.

### Face parity — the important detail
The landed face is decided by the **fractional part of `spin`**, so the outcome is baked into
the target rotation, not chosen at the end:

```
spin lands on an integer   → cos(2π·spin) = +1 → FRONT face up  (RELIC)
spin lands on integer+0.5  → cos(2π·spin) = −1 → BACK face up   (SEAL)
```

Reference default `spin_final = 8.0` (RELIC). For SEAL use `8.5`.
**Decide the winner on the server before the animation starts**, then pick `spin_final` to match.
Never randomise inside the animation — the result must already be authoritative.

---

## Timeline — four phases

Durations are the reference values. They are **retimable**: everything below is driven by each
phase's normalised progress `p ∈ [0,1]`, so scaling a phase's length stretches its motion
rather than clipping it. In Unity that means one `AnimationCurve`/coroutine per phase evaluated
on `p`, not on absolute time.

| # | Phase | Dur | `h` in → out | `spin` in → out | Camera scale / y |
|---|---|---|---|---|---|
| 1 | **Toss** | 1.40 s | 0 → 0.62 | 0 → 3.2 | 1.00 → 1.06 / 0 → −20 |
| 2 | **Apex** | 1.60 s | 0.62 → **1.0** → 0.62 | 3.2 → 6.0 | 1.06 → 1.12 / −20 → −34 |
| 3 | **Land** | 1.30 s | 0.62 → 0 | 6.0 → 8.0 | 1.12 → 1.00 / −34 → 0 |
| 4 | **Verdict** | 3.40 s | 0 | 8.0 (held) | 1.00 → 1.05 → 1.00 |

**Total 7.70 s.** Phase 4 is the only phase that does not end in-game — it **holds on the
choice** until the winner picks or the timer expires. Camera y is in reference pixels at 720p; scale is a uniform zoom about the
point `(50%, 46%)` of the frame.

### The boundary rule (non-negotiable)
Every phase's **first rendered frame equals the previous phase's last frame**. `h`, `spin`,
`tilt` and camera are all pinned at the seams (see the table), and every entrance/exit
completes strictly *inside* its phase. If you implement each phase as a separate Unity state,
verify the seams — a mismatch shows as a visible pop at 1.4 s, 3.0 s and 4.3 s.

Phase 4 also has to match **phase 1's first frame**, because the reference loops. In-game it
won't loop, so phase 4 may simply hold instead of fading its banner out.

---

## Motion helpers — use exactly these three

No other easing appears anywhere in the piece. Keep it that way; it is what makes the toss feel
like one gesture.

```
enter(t) = 1 − (1 − t)³                       // ease-out cubic  — launches, entrances
drift(t) = 0.5 − 0.5·cos(π·t)                 // ease-in-out sine — camera, arcs
pop(t)   = 1 + 2.9·(t−1)³ + 1.9·(t−1)²        // ease-out back    — impact overshoot
```

Unity equivalents: `enter` → `AnimationCurve.EaseInOut` won't do, author it (or
`1-Mathf.Pow(1-t,3)`); `drift` → `Mathf.SmoothStep(0,1,t)` is close enough;
`pop` → an `OutBack` ease with overshoot **1.9** (DOTween: `Ease.OutBack, overshoot: 1.9f`).

A helper used constantly below — remap a sub-window of `p` onto 0…1:
```
seg(p, a, b) = clamp01((p − a) / (b − a))
```

---

## Phase 1 — Toss (1.40 s)

```
a    = sin(π · seg(p, 0, 0.26))          // anticipation, 0 at both ends
fly  = seg(p, 0.26, 1)
h    = enter(fly) · 0.62 − a · 0.03      // the squat dips the coin slightly
spin = enter(fly) · 3.2
tilt = a · −3                            // degrees
cam  = lerp(1.00→1.06, drift(p)),  y = lerp(0→−20, drift(p))
```

The first 26 % is **anticipation**: the coin squats into the table and tilts 3° before it
leaves. Do not skip it — without it the launch reads as a teleport.

**Caption** "Who goes first?" fades and rises in over `seg(p, 0.34, 0.74)`, rise 18 → 0 px,
and is fully settled from 0.74 to the end of the phase.

## Phase 2 — Apex (1.60 s)

```
arc  = sin(π · p)                        // 0 → 1 → 0
h    = lerp(0.62 → 1.00, arc)
spin = lerp(3.2 → 6.0, p)                // LINEAR — no easing in the air
cam  = lerp(1.06→1.12, drift(p)),  y = lerp(−20→−34, drift(p))
```

This is the held beat: the coin hangs, spinning at a constant **1.75 turns/s**, and the camera
pushes in slowly. `spin` must be linear here — easing it makes the coin look like it is being
driven rather than coasting.

**Caption** fades out over `seg(p, 0.6, 0.92)`, drifting −14 px, gone before the phase ends.

## Phase 3 — Land (1.30 s)

```
fall   = seg(p, 0, 0.52)
drop   = 0.62 · (1 − fall²)                     // accelerating fall
bounce = sin(π · seg(p, 0.52, 0.74)) · 0.075    // one small hop
settle = sin(π · seg(p, 0.74, 1))   · 0.012     // a last shiver
h      = drop + bounce + settle

spin   = lerp(6.0 → 8.0, enter(seg(p, 0, 0.62)))   // eases to a stop before touchdown
tilt   = sin(seg(p, 0.52, 1) · 5π) · (1 − enter(seg(p, 0.52, 1))) · 7
glow   = enter(seg(p, 0.52, 0.8)) · (1 − seg(p, 0.8, 1)) · 0.5
cam    = lerp(1.12→1.00, drift(p)),  y = lerp(−34→0, drift(p))
```

- `spin` finishes **before** the coin lands (at `p = 0.62` of this phase, ≈ 0.81 s in) so the
  face is already readable through the bounce.
- `tilt` is a damped **5-half-cycle** rock. Use `sin`, not `cos` — with `cos` the rock starts
  at full amplitude and pops at the phase boundary.
- **Dust ring**: an ellipse at the table line, expanding 120 → 420 px on `enter(seg(p,0.5,0.9))`,
  height 24 % of its width, 2 px border `#C8A45C` fading `(1 − seg)·0.5 → 0`.
  In Unity: a one-shot particle burst plus a scaling ring sprite.
- Camera **pulls back** on impact rather than pushing in. That inversion is the hit.

## Phase 4 — Verdict (2.40 s)

```
in    = enter(seg(p, 0.06, 0.40))
out   = 1 − enter(seg(p, 0.88, 1))       // reference loop only; hold at 1 in-game
ring  = seg(p, 0.02, 0.40)
glow  = (0.45 + sin(2π·p)·0.25) · out    // slow breathing pulse on the coin
cam   = 1 + sin(π·p)·0.05,  y = 0

header opacity   = in · out,  rise = lerp(26 → 0, in)
ring opacity     = (1 − ring)·0.55 · out
ring scale       = lerp(0.8 → 1.9, pop(ring)·0.55 + ring·0.45)
GO FIRST  card   = enter(seg(p, 0.30, 0.56)) · out,  rise = lerp(24 → 0, that)
GO SECOND card   = enter(seg(p, 0.38, 0.64)) · out,  rise = lerp(24 → 0, that)
hint bar         = enter(seg(p, 0.58, 0.78)) · out
```

The coin holds still and **breathes** (`glow` oscillates once per second). One expanding gold
ring leaves the coin on impact-plus-one. The header lands first, then the two choice cards
**stagger in 0.27 s apart** (left before right), then the hint bar — so the eye is led
header → options → deadline rather than being handed the whole panel at once.

### The choice — this is the point of the scene

The coin does **not** announce a starting player. It announces who *chooses*.

Layout on a 1280 × 720 frame: the coin stays dead centre on the table line; the two option
cards flank it at `x = 96` and `x = 884`, `y = 372`, each **300 px wide**. The coin occupies
x 556–724, so there is ~160 px of clearance on each side — the composition reads as
"the coin decided, now pick a side".

**Option card** — the card frame grammar, reused:
- radius 9, 2px `#C8A45C` keyline, body `linear-gradient(165deg,#3A2818,#140C07 58%,#291A0C)`,
  `0 20px 44px rgba(0,0,0,.7)`
- inner keyline at `inset: 6`, 1px `rgba(200,164,92,.32)`
- a **38 × 42 hexagon crest** breaking the top-right corner at `right: −11, top: −13` —
  the same crest as a card's level badge — carrying the roman numeral `I` / `II`
- title **Cinzel 700 30px**, letter-spacing .05em, `#F5EBD4`, padding `22px 22px 16px`
- parchment consequence strip at the bottom (`margin: 0 8px 8px`, `padding: 11px 13`,
  `#EBE1C7 → #D9CCAB`, 1px `#8C7440`), Spectral 400 13/1.4 `#2E2417`

| Card | Numeral | Consequence copy |
|---|---|---|
| GO FIRST | I | `You open the duel and set the pace — but you draw no card on turn one.` |
| GO SECOND | II | `<Opponent> opens and shows her hand first — you draw one extra card.` |

**Neither card is styled as the recommended one.** They are visually identical because the
trade-off is genuinely even; highlighting one would be design telling the player what to think.

**Header copy**, by who won the toss:

| Winner | Chip | Headline | Sub |
|---|---|---|---|
| You (RELIC) | `RELIC · YOU CALLED IT` | `YOUR CHOICE` | `Take the first turn, or hand it over.` |
| Opponent (SEAL) | `SEAL · LYRA CALLED IT` | `LYRA'S CHOICE` | `Lyra decides who opens.` |

**Hint bar**, bottom-centre at `bottom: 44`, `rgba(0,0,0,.45)` + 1px `rgba(200,164,92,.3)`,
a 7px dot + Spectral 14px `#9C8A6A`:
- you won → `Choose within 15 s — GO FIRST is taken by default.`
- opponent won → `Waiting for Lyra to choose…` (dot in `#8FC6D2`)

**When the opponent won the toss**, both cards render at **opacity .42** and are not
interactive — the player still sees the two options so they know what is being decided *to*
them, then the chosen one resolves. Do not hide the cards; a blank wait is worse than a
visible one.

### Interactive states (in-game only — the reference is static)

| State | Treatment |
|---|---|
| Hover / focus | `translateY(−10px)`, keyline → `#EBCE8A`, add `0 0 34px rgba(235,206,138,.35)`, 140 ms ease-out |
| Pressed | `translateY(−4px)`, no glow, 80 ms |
| Chosen | the picked card holds its hover state and its crest fills gold; the other fades to opacity .2 and drops 8 px, 220 ms — then a 500 ms beat before the board loads |
| Timeout | at 3 s remaining the hint bar's dot turns `#E0603A` and a thin progress rule drains under it; at 0 s `GO FIRST` auto-resolves through the *Chosen* treatment, so the player sees what happened |

Keyboard / controller: `←`/`→` moves focus, `Enter`/`A` confirms; `1` and `2` pick directly.

---

## The coin

Diameter `D`. Two faces, one rim.

### Rendering approach
Two options, both valid:

**A · 3D mesh (preferred).** A short cylinder, `rotation.x = spin · 360°`, two materials for
the faces plus a rim material. Physically correct, gets real specular travel across the gold.

**B · Canvas 2D squash (what the reference does).** No 3D needed, and it is exact:
```
θ  = spin · 2π
c  = cos(θ)
scaleY   = |c|                      // squash the sprite vertically
faceShown = c ≥ 0 ? FRONT : BACK    // swap sprite when the sign flips
edgeVisible = |c| < 0.10            // draw the rim sliver, alpha = 1 − |c|/0.10
```
Apply as `rotate(tilt) · scaleY(|c|)` about the coin's centre. Clamp `scaleY` to a floor of
**0.02** so the sprite never collapses to nothing between frames.

### Faces

**FRONT — RELIC** (the winning face; the game's logo mark)
- body `radial-gradient(circle at 34% 28%, #F8EED6, #C8A45C 46%, #7A5A1E 88%)`
- rim `inset 0 0 0 6px #EBCE8A`, then `inset 0 0 0 8px #3B2A10`
- lower shading `inset 0 −10px 24px rgba(0,0,0,.45)`
- relief: three nested squares rotated 45°, all in `#3B2A10` —
  96 px / 4 px stroke, 52 px / 3 px stroke (fill `rgba(59,42,16,.18)`), 22 px solid core
  *(sizes at D = 168; scale proportionally)*

**BACK — SEAL** — **silver, not gold.** The two faces have to be tellable apart in a single
frame of a coin spinning at 1.75 turns/s, so they differ in *metal*, not just in relief.
- body `radial-gradient(circle at 34% 28%, #F2F5F8, #A9B2BE 50%, #5A6472 90%)`
- rim `inset 0 0 0 6px #D6DDE6`, then `inset 0 0 0 8px #262C34`
- lower shading `inset 0 −10px 24px rgba(0,0,0,.5)`
- relief in `#262C34`: a 104 px circle (4 px stroke), four 128 × 3 px spokes at
  0° / 45° / 90° / 135° at 60 % alpha, and a 46 × 50 px hexagon crest at the centre
  (the same hexagon `clip-path` as the card's level crest)

**RIM** — a 10 px band, radius 5, matching whichever face is currently front:
gold `linear-gradient(180deg, #F8EED6, #C8A45C 45%, #3B2A10)` /
silver `linear-gradient(180deg, #F2F5F8, #A9B2BE 45%, #262C34)`.

### Ground shadow
Always present, directly under the coin at `GROUND + 8`:
```
width   = lerp(D·1.02 → D·0.34, h)     // shrinks as the coin rises
height  = width · 0.20
opacity = lerp(0.50 → 0.06, h)
```
A radial-gradient ellipse, transparent past 72 %. This single element does most of the work of
selling the altitude — do not drop it.

### Glow
`drop-shadow(0 0 (18 + glow·34) px rgba(235,206,138, 0.28 + glow·0.42))`.
In Unity: a bloom-contributing emissive term, or an additive sprite behind the coin.

---

## Stage (persistent — present in every phase)

Built once and never replaced; phases only add to it.

1. **Table** `radial-gradient(ellipse 1080×620 at 50% 46%, #2A1C12, #0A0705 76%)`
2. **Weave** ±45° hairlines, `rgba(200,164,92,.045)`, 28 px pitch — the card-back motif
3. **Table plane** below `GROUND`: `linear-gradient(180deg, rgba(96,52,18,.34), transparent 70%)`
   with a 1 px `rgba(200,164,92,.22)` top edge
4. **Two ornament diamonds** centred at `(640, GROUND − 96)`, rotated 45°, 520 px @ 10 % and
   320 px @ 7 % alpha
5. **Embers** — six 5–7 px diamonds rising 330 px on a shared normalised clock with fixed
   per-particle offsets, peak alpha 0.5, fading in over the first 18 % of their travel.
   **Fixed offsets, not random** — the animation has to be frame-deterministic to export.
6. **Vignette** `inset 0 0 220px rgba(0,0,0,.88)`

---

## Typography

Fonts are Google Fonts, all SIL OFL — safe to ship. Sizes at 720p; scale with the canvas.

| Role | Font | Size / line | Weight | Tracking | Colour |
|---|---|---|---|---|---|
| Caption eyebrow `THE TOSS` | Oswald | 14 / 1 | 500 | .38em | `#9C8A6A` |
| Caption | Cinzel | 54 / 1.2 | 700 | .05em | `#F1DFB8` |
| Result chip | Oswald | 13 / 1 | 500 | .30em | `#7ACD96` win / `#8FC6D2` loss |
| Result headline | Cinzel | 74 / 1.2 | 700 | .06em | `#F8EED6` |
| Result note (parchment) | Spectral | 17 / 1 | 400 | 0 | `#2E2417` |

**Line-height 1.2 minimum on Cinzel** — at 1.0 the `Q` descender clips.

Result copy, by winning face:

| Face | Chip | Headline | Note |
|---|---|---|---|
| RELIC | `RELIC · YOU CALLED IT` | `YOU GO FIRST` | `<Player> draws no card on turn one.` |
| SEAL | `SEAL · THE WARDEN CALLED IT` | `<Opponent> GOES FIRST` | `<Opponent> draws no card on turn one.` |

The parchment note panel: `linear-gradient(180deg,#EBE1C7,#D9CCAB)`, 1 px `#8C7440`,
padding 13 × 22 — the same panel as the card's effect box.

## Colour tokens

| Token | Hex |
|---|---|
| gold | `#C8A45C` |
| gold light | `#EBCE8A` |
| gold pale | `#F8EED6` |
| gold dark | `#7A5A1E` |
| gold deep | `#3B2A10` |
| silver pale | `#F2F5F8` |
| silver light | `#D6DDE6` |
| silver | `#A9B2BE` |
| silver dark | `#5A6472` |
| silver deep | `#262C34` |
| table warm / dark | `#2A1C12` / `#0A0705` |
| parchment | `#EBE1C7` → `#D9CCAB`, ink `#2E2417` |
| win | `#7ACD96` |
| opponent | `#8FC6D2` |
| ember | `#E0603A` |

---

## Integration

```csharp
public enum CoinFace { Relic, Seal }
public enum TurnOrder { First, Second }

// Called once, after the server has already decided WHO WON THE TOSS.
// spinFinal: Relic -> 8f, Seal -> 8.5f
public IEnumerator PlayToss(CoinFace winner, string opponentName);

// Fires when phase 3 ends (t = 4.30 s) — the face is readable, the board may start
// dealing in behind the choice panel.
public event Action<CoinFace> OnFaceSettled;

// Fires when the toss winner has picked (or the 15 s default fired).
// This is the value the duel actually needs — OnTossComplete alone is not enough.
public event Action<TurnOrder> OnTurnOrderChosen;

// Fires after the chosen-card resolve beat — hand control to the duel.
public event Action OnTossComplete;
```

**Rules**
- Input is blocked for phases 1–3 except a **skip**. Skipping jumps to `t = 4.30 s`
  (the start of Verdict) — never past the reveal, and never past the choice.
- The choice is **not** part of the cutscene's fixed length: phase 4 plays its 3.4 s in, then
  **holds** until `OnTurnOrderChosen`. Only the reference loops.
- The coin result is authoritative before frame one. `spin_final` is chosen from it.
- When the *opponent* won the toss, the client shows the dimmed cards and waits for the
  server's `TurnOrder`; it must not guess or pre-select.
- The 15 s default is server-authoritative too — the client's countdown is display only.
- Deterministic: no `Random` calls inside the animation. Ember offsets are constants.
- Audio cues: a metallic *flick* at `t = 0.36` (end of anticipation), a rising *whirr* through
  Apex, a *clack* at `t = 3.68` (first touchdown), a soft *chime* at `t = 4.50` (header in),
  two quiet *ticks* at `t = 5.35` and `t = 5.63` as the choice cards land, and a single
  low *seal* thud when a card is chosen.
- If the player's connection drops mid-toss, hold the last frame — do not restart.

## Tweakable knobs (exposed in the reference)

| Knob | Values | Effect |
|---|---|---|
| Winning face | `relic` / `seal` | Sets `spin_final` parity and swaps all result copy |
| Spin | 5 – 14 turns | Total rotations; the *duration* is unchanged, so this is how fast it spins |

## Files

| File | What it is |
|---|---|
| `Coin Flip.dc.html` | The animation. Scene list and defaults live in inline scripts at the top. |
| `coin-flip.jsx` | Scene components — every formula in this document, as code. **Read this next.** |
| `animations-v2.jsx` | Timeline engine (starter, unmodified). Not part of the design. |
| `tweaks-panel.jsx` | Tweak controls (starter, unmodified). Not part of the design. |
| `README.md` | Card system handoff — read first for the shared palette and type scale. |
