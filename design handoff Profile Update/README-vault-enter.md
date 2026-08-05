# Handoff: Vault Enter (login → main menu)

Companion document to the RELIQUARY bundle. Same visual language — **Reliquary**.
Read `README.md` (card system) first for the shared palette and type scale, and
`README-coin-flip.md` for the motion conventions, which this piece reuses exactly.

## Overview
The transition after a successful login. The player has just pressed LOG IN on the auth panel;
this plays before the Main Menu appears. A great seal fills the frame, its six locks release in
sequence, the two halves part, and the camera passes through into the vault.

Reference runs **6.6 s at 1280 × 720**, four cuts. It is a **loading mask**: it covers the real
work (session handshake, collection fetch, menu warm-up), so its length must be able to flex.

## About the Design File
`Vault Enter.dc.html` + `vault-enter.jsx` are a **motion reference built in HTML**. Do not port
the DOM. Port the timing, the easing and the seal's construction into Unity using the project's
own rendering. Scrub the timeline in the browser to read any value.

## Fidelity
**High fidelity on timing and easing.** Placeholder: the player name, the online count, the
collection numbers, and the seal's relief detail (CSS shapes standing in for a real mesh).

---

## The through-line

One scalar carries the whole piece:

| Symbol | Meaning | Range |
|---|---|---|
| `depth` | how far the camera has travelled inward | 0 … 1 |
| `r` | seal radius, px at 720p | 150 … 340 |
| `turn` | tumbler-ring rotation, degrees | 0 … 460 |
| `split` | how far the two halves have parted | 0 … 1 |
| `lit` | emissive amount on the seal | 0 … 1 |

`depth` drives the backdrop: the four concentric ornament rings scale by `1 + depth · 1.9`
about the frame centre, and the weave scales by `1 + depth · 0.5`. That parallax difference is
what reads as *distance* rather than a flat zoom. In Unity: put the rings and the weave on
separate quads at different Z and move one camera — you get the same effect for free.

---

## Timeline — four phases

| # | Phase | Dur | `depth` | `r` | `turn` | `split` |
|---|---|---|---|---|---|---|
| 1 | **Approach** | 1.5 s | 0 → 0.18 | 150 → 172 | 0 | 0 |
| 2 | **Unlock** | 2.0 s | 0.18 → 0.48 | 172 → 210 | 0 → 300 | 0 → 0.55 |
| 3 | **Open** | 1.1 s | 0.48 → 1.0 | 210 → 340 | 300 → 460 | 0.55 → 1.0 |
| 4 | **Arrive** | 2.0 s | 1.0 → 0.04 | — | — | — |

**Total 6.6 s.** Phase boundaries are pinned — every phase's first frame equals the previous
phase's last frame. Verify the seams at 1.5 s, 3.5 s and 4.6 s if you split these into separate
Unity states.

### Velocity at the seams matters here
Phase 2 ends **decelerating** (it uses `drift`, whose slope is zero at both ends). Phase 3
therefore starts with an **ease-in** (`p²`, slope zero at 0) rather than the ease-out used
elsewhere. Matching position across a cut is not enough — if phase 3 had used `enter`, the seal
would visibly jerk forward at 3.5 s. **Match velocity, not just value.**

## Motion helpers

Identical to the coin toss — the same three curves, nothing else:

```
enter(t) = 1 − (1 − t)³                       // ease-out cubic
drift(t) = 0.5 − 0.5·cos(π·t)                 // ease-in-out sine
pop(t)   = 1 + 2.9·(t−1)³ + 1.9·(t−1)²        // ease-out back, overshoot 1.9
seg(p,a,b) = clamp01((p − a)/(b − a))         // remap a sub-window onto 0…1
```

Phase 3 additionally uses plain `p²` for the reason above.

---

## Phase 1 — Approach (1.5 s)

```
depth = drift(p) · 0.18
r     = lerp(150 → 172, drift(p))
lit   = 0.1 + sin(π·p) · 0.1               // one slow breath
settle = enter(seg(p, 0, 0.40))

"WELCOME BACK / <name>"  opacity = settle · (1 − enter(seg(p, 0.66, 0.94))),  rise = lerp(20→0, settle)
status pill              opacity = enter(seg(p, 0.30, 0.62)) · (1 − enter(seg(p, 0.70, 0.96)))
```

The seal is shut and still. The player's name appears, the status pill confirms the session,
and both are **fully gone before the cut** — a caption caught mid-fade at a hard cut reads as
a glitch.

Status pill: `rgba(0,0,0,.45)` + 1px `rgba(200,164,92,.32)`, a 7px `#7ACD96` dot, Spectral 14px
`#9C8A6A` — `Seal verified · N duelists inside`.

## Phase 2 — Unlock (2.0 s)

```
depth    = lerp(0.18 → 0.48, drift(p))
r        = lerp(172 → 210, drift(p))
turn     = enter(p) · 300
tumblers = seg(p, 0.12, 0.86)              // 0…1, drives how many locks are lit
split    = enter(seg(p, 0.66, 1)) · 0.55   // halves only start parting at the end
lit      = 0.2 + tumblers · 0.4 + split · 0.5
```

The ring turns and **six tumblers light in sequence** — lock `i` lights when
`tumblers > i/6`. Spread across 0.12 → 0.86 of the phase, that is one lock roughly every
0.25 s: fast enough to feel mechanical, slow enough to count. Counting is the point; the
caption says `Six locks`.

Lit tumbler: `#EBCE8A` with `0 0 (r·0.14)px` glow. Unlit: `rgba(200,164,92,.28)`, no glow.

The two halves stay shut for the first two thirds. All the tension is in the ring.

## Phase 3 — Open (1.1 s)

```
rush  = p²                                  // ease-in — see the seam note above
depth = lerp(0.48 → 1.0, rush)
r     = lerp(210 → 340, rush)
turn  = lerp(300 → 460, drift(p))
split = clamp01(rush / 0.72²)               // fully parted at p ≈ 0.72
flare = sin(π · seg(p, 0.10, 0.70))
lit   = 0.7 + flare · 0.3

flare disc  = 200 + flare·1500 px, radial #F8EED6 at flare·0.5 → transparent 62%
white-out   = #F8EED6 at enter(seg(p, 0.76, 1)) · 0.9
```

The shortest phase and the loudest. The halves fling apart, light floods out from behind them,
and the frame **whites out to `#F8EED6`** — that white-out is the actual cut point. Everything
that has to finish loading should be done by the time it peaks.

The core (`r · 0.26`) shrinks with `1 − split`: the last thing to give way is the centre.

## Phase 4 — Arrive (2.0 s)

```
wash  = 1 − enter(seg(p, 0, 0.36))          // the white-out receding
depth = lerp(1 → 0.04, enter(seg(p, 0, 0.50)))
in    = enter(seg(p, 0.20, 0.56))
out   = 1 − enter(seg(p, 0.90, 1))          // reference loop only

lockup opacity = in · out
lockup         = translateY(lerp(28→0, in)), scale(lerp(1.06→1, in))
```

The white recedes, the camera settles back to almost nothing, and the **logo lockup** rises
into the frame: the 120px mark (three nested rotated squares, the app-icon build) over the
RELIQUARY wordmark with its gold gradient, over one Spectral line of the player's actual
collection state (`Your vault holds 218 cards and 4 decks.`).

In-game this phase **holds** on the lockup until the menu is ready, then crossfades to it over
200 ms. Only the reference fades out to loop.

---

## The seal

Radius `r`, centred in frame.

**Body** — a circle, `radial-gradient(circle at 36% 30%, #F8EED6, #C8A45C 46%, #3B2A10 92%)`,
with `inset 0 0 0 (r·0.05)px #EBCE8A` then `inset 0 0 0 (r·0.07)px #3B2A10` as the rim.
All internal dimensions are **proportional to `r`**, so the seal is resolution-independent.

**Relief** — two squares rotated 45° in `#3B2A10`: `r·1.1` with a `r·0.045` stroke, and
`r·0.6` with a `r·0.035` stroke over `rgba(59,42,16,.2)`. Same nested-diamond motif as the
card back and the logo.

**Split** — the body is drawn twice and clipped to its left and right halves
(`inset(0 50% 0 0)` / `inset(0 0 0 50%)`), each translated outward by `split · r · 1.5`.
In Unity: two halves of one mesh, or one mesh with a shader-driven separation.

**Tumbler ring** — at `inset: −r·0.18`, a `r·0.035` stroke in `rgba(200,164,92,.55)`, rotating
by `turn`. Six `r·0.11` diamonds placed at 0/60/120/180/240/300°, each pushed out `r·1.18`.

**Core** — `r·0.26`, rotated `45 + turn`, `linear-gradient(135deg,#F8EED6,#7A5A1E)`, glow
`0 0 (r·0.3)px`. Scales and fades with `1 − split`.

**Glow** — `drop-shadow(0 0 (20 + lit·60)px rgba(235,206,138, 0.2 + lit·0.5))`.
In Unity: emissive + bloom.

## Backdrop

1. `radial-gradient(ellipse 1100×700 at 50% 48%, #2A1C12, #0A0705 76%)`
2. Weave, ±45° hairlines `rgba(200,164,92,.045)` at 28px pitch, scaled by `1 + depth·0.5`
3. Four ornament squares rotated 45°, base sizes **980 / 700 / 470 / 300** px, all scaled by
   `1 + depth·1.9`, alpha `0.16 − i·0.025 + glow·0.14`
4. A centre bloom disc, 300·scale px, `rgba(235,206,138, 0.05 + glow·0.3)` → transparent at 66%
5. Vignette `inset 0 0 240px rgba(0,0,0,.88)`

## Typography

| Role | Font | Size / line | Weight | Tracking | Colour |
|---|---|---|---|---|---|
| Eyebrow | Oswald | 13 / 1 | 500 | .38em | `#9C8A6A` |
| Headline | Cinzel | 50 / 1.2 | 700 | .05em | `#F1DFB8` |
| Wordmark (Arrive) | Cinzel | 78 / 1.2 | 700 | .09em | gold gradient |
| Body / status | Spectral | 14–20 / 1 | 400 | 0 | `#A2917A` / `#9C8A6A` |

**Line-height 1.2 minimum on Cinzel** — at 1.0 the `Q` descender clips.

---

## Integration

```csharp
// Plays as a loading mask. The caller starts real work in parallel.
public IEnumerator PlayVaultEnter(string playerName, VaultSummary summary);

// Fires at the white-out peak (t ≈ 4.55 s) — safe point to swap the underlying scene.
public event Action OnCurtainPeak;

// Fires once the lockup has settled (t ≈ 5.7 s). Phase 4 HOLDS here until
// the caller signals the menu is ready.
public event Action OnArriveSettled;
public void ReleaseToMenu();     // crossfade 200 ms into the Main Menu
```

**Rules**
- Phases 1–3 are **fixed length** (4.6 s) — they are choreography, do not retime them at
  runtime. Phase 4 is the elastic one; hold it as long as loading needs.
- If loading finishes early, still play phase 4's 2.0 s in full. Cutting the arrival short
  wastes the build-up.
- If loading takes longer than ~6 s past the hold, add a small Spectral line under the lockup
  (`Still opening…`) rather than looping the seal.
- Input is blocked throughout. There is **no skip** — this only plays once per session, and it
  is short. If telemetry shows returning players skipping, add one on a second viewing only.
- Deterministic: no `Random` anywhere.
- Audio: a low *stone* rumble under phase 1, six ascending *click*s tracking the tumblers
  through phase 2, a heavy *release* at `t = 3.50`, a rising *swell* peaking with the white-out
  at `t = 4.55`, then room tone.
- On login failure this never plays — the auth panel handles its own error state.

## Colour tokens

Inherited from the card system. Used here: gold `#C8A45C`, light `#EBCE8A`, pale `#F8EED6`,
dark `#7A5A1E`, deep `#3B2A10`, table `#2A1C12` → `#0A0705`, success `#7ACD96`,
muted ink `#A2917A` / `#9C8A6A`.

## Files

| File | What it is |
|---|---|
| `Vault Enter.dc.html` | The animation. Scene list lives in an inline script at the top. |
| `vault-enter.jsx` | Scene components — every formula above, as code. **Read this next.** |
| `animations-v2.jsx` | Timeline engine (starter, unmodified). Not part of the design. |
| `README-coin-flip.md` | The coin toss — same motion conventions, read for context. |
| `README.md` | Card system handoff — read first. |
