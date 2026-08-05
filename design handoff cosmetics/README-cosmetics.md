# Handoff: Cosmetics — 30 items, five card finishes, five victory seals

Companion document to the RELIQUARY bundle (`README.md` card system,
`README-progression.md` ranks and shop economy, `README-animations.md` motion).
Same visual language — **Reliquary**. Read `README.md` first for the palette and type scale.

## What is in here

| File | What it is |
|---|---|
| `Cosmetics Catalogue.dc.html` | All 30 cosmetics, each drawn as the artifact it actually is, with its shop icon |
| `Victory Seals.dc.html` + `victory-seals.jsx` | The five victory seal stamps, animated |
| `Cosmetics Shop.dc.html` | The shop screen itself (see `README-progression.md`) |

Everything here is **cosmetic only**. No cosmetic changes a rule, a stat, or a matchmaking
weight. That has to stay true — the moment a mat or a finish touches gameplay, the shop stops
being optional.

---

# 1 · The five categories

| Category | Count | Where it is seen | Design constraint |
|---|---|---|---|
| Card back | 8 | Every draw, every face-down card, both players | The one seen most; carries the most weight |
| Duel mat | 6 | The whole field, all match long | Must stay quiet enough that cards read on top |
| Toss coin | 5 | The opening toss only, ~7 s | Two faces, and both get shown |
| Profile frame | 6 | Profile, leaderboard rows, duel intro | Reads at 44 px in a list and at 150 px on a profile |
| Victory seal | 5 | The win screen, ~2.5 s | Must land in about a second and a half |

**Each category ships with a vanilla baseline** that every player has and nobody buys. The
catalogue shows it first in every row, in a dashed frame. A cosmetic that does not clearly beat
the baseline should not ship — that is what the baseline is there to test.

## Rarity spread
8 common · 9 rare · 8 epic · 5 relic. Rarity here means **visual ambition, not power**:
a common changes one property (colour, material), a relic changes the composition.

## Pricing
Coins for common and rare, shards for most epic and relic. The split is deliberate: coins come
from playing, shards from the season track and ranked rewards, so the loudest items are gated
behind engagement rather than behind grinding. Prices are in `README-progression.md`.

---

# 2 · Card backs (8)

130 × 182 in the catalogue; ship at the card's real render size. All share the 5 px rounded
corner, the 2 px frame and the inner rule — everything else is free.

| # | Name | Rarity | The idea | What makes it distinct |
|---|---|---|---|---|
| 1 | Ashen Weave | common | The house weave, drained | Same geometry as vanilla, all gold removed — greys `#3A382F → #1A1A17`, rim `#7E7566` |
| 2 | Tomb Gilt | epic | Gold leaf over the weave | 6 px weave pitch (vs 11), a **filled** centre diamond, 3 px `#EBCE8A` frame |
| 3 | Deep Current | rare | The flooded lower vault | **No diagonal weave at all** — horizontal bands, and a horizontal lens at centre |
| 4 | Obsidian Lattice | rare | Two grids crossed | Orthogonal grid *and* 45° grid overlaid, near-black, one bright core |
| 5 | Chainbound | rare | Bound shut | A vertical column of five chain links — the only back with a repeating object |
| 6 | Cartogram | epic | The vault floor plan | **Light back** — parchment `#D9CCAB → #A8996F`, offset rectangles, a centre compass |
| 7 | Split Seal | relic | Two halves, one card | Diagonal split: gold weave one side, violet the other, a lit seam across |
| 8 | Static Bloom | relic | Interference | Four concentric **circles** (not diamonds) plus 4 px scanlines |

The three axes that keep them apart: **weave direction** (diagonal / horizontal / orthogonal /
none), **value** (dark, or light in Cartogram's case), and **centre motif** (diamond, lens,
circle, chain, seam). Two backs must never match on all three.

---

# 3 · Duel mats (6)

272 × 154 in the catalogue; ship at field aspect. Every mat keeps the same furniture so the
board stays learnable: a centre line, five zone outlines per side, and a centre marker.
**Only the treatment changes.** Zone outlines never drop below 14 % opacity.

| # | Name | Rarity | The idea | Treatment |
|---|---|---|---|---|
| 9 | Stone Table | common | The slab, cut deeper | Chiselled block joints, two nested diamonds with 3 px inset shadow |
| 10 | Ember Circle | rare | A burning ring | A glowing ellipse around the field, four embers, ember-tinted zone rules |
| 11 | Starless Vault | epic | Cold and empty | Near-black `#060609`, nine fixed cold points, no ornament |
| 12 | Tidal Floor | rare | Water over stone | Horizontal bands, two lens sheens, a lit centre line |
| 13 | Foundry Grate | epic | Standing over the furnace | Vertical bars with warm underlight rising from below |
| 14 | Cathedral Plate | relic | An arched hall | Arches top and bottom, two radiating ribs, a bright centre diamond |

Ember Circle and Foundry Grate are the two warm mats and should never be the only options in a
rotation together — a player who dislikes warm boards needs a cold choice available.

---

# 4 · Toss coins (5)

94 px in the catalogue. Used only in the opening toss (`README-coin-flip.md`), where the flip
shows **both** faces before landing on one.

**Every coin carries two different devices.** Front is the RELIC face, back is the SEAL face —
the same convention the coin-flip animation reads for its parity. Two faces that look alike
make the flip unreadable, because the player cannot tell mid-spin which side is coming up.

| # | Name | Rarity | RELIC face | SEAL face |
|---|---|---|---|---|
| — | Relic & Seal *(vanilla)* | — | Three nested diamonds | Circle + 4 spokes + hexagon crest |
| 15 | Copper Trial | common | One bold filled diamond, punched centre | **Quartered field** — crossed bars + 4 dots |
| 16 | Silver Warden | rare | Circle + 4 spokes + hexagon crest | **A keyhole** — circle over a tapered slot |
| 17 | Bone Token | common | Incised cross | **Three claw scratches** at a diagonal |
| 18 | Molten Bit | epic | Horizontal glowing seam + diamond | **Radiating crack star**, dark core |
| 19 | Vault Coin | relic | Diamond outline + gold diamond core | Ring + 4 pips + gold hexagon crest |

Bone Token is the only non-circular coin — an irregular `clip-path` silhouette, chipped on both
faces but not identically. Molten Bit's seam is the only emissive element in the set.

---

# 5 · Profile frames (6)

150 × 150 around a portrait. These have to survive being shrunk to a 44 px leaderboard avatar,
so the distinguishing feature must be at the **silhouette or the border**, never in fine detail.

| # | Name | Rarity | The idea | At 44 px it still reads as |
|---|---|---|---|---|
| 20 | Iron Bracket | common | Riveted plate | Four round rivets on a thick grey border |
| 21 | Amber Halo | rare | Lit from behind | A warm glow ring wider than the portrait |
| 22 | Thorn Setting | rare | Spiked mount | Eight triangles breaking the square outline |
| 23 | Gilded Reliquary | epic | The vault door, shrunk | Double gold border + a crest above the top edge |
| 24 | Prism Mount | epic | Faceted | Four large corner triangles cutting into the portrait |
| 25 | Vault Ring | relic | Never at rest | A dashed circle **outside** the square, slowly turning |

Thorn Setting and Vault Ring both break the square boundary, which is what makes them read in a
list; they should not appear in the same rotation slot.

---

# 6 · Victory seals (5)

## What they are
The stamp that lands on the win screen when a player takes a duel. It replaces the default
diamond seal. **Both players see it**, so it is the most social cosmetic in the game and the
only one with real motion design behind it.

Purely cosmetic. It changes nothing about rewards, RP, or the result.

## Timing
Each seal completes by **72 % of its scene** and holds on its final frame for the rest — the
still frame is what the player screenshots, so it has to be worth holding. Scene lengths:
Brand 2.6 s · Shatter 2.4 s · Bloom 2.6 s · Verdict 2.4 s · Eclipse 2.8 s.

The five differ in **how they arrive**, which is the whole point — a player should recognise an
opponent's seal from the first few frames.

### 26 · Brand — common
Burned in. Nothing travels; the heat does the work.
```
spread = enter(seg(t, 0, 0.44))      // scorch halo grows to 1.44r
heat   = sin(π · seg(t, 0.06, 0.72)) // glow bloom, peaks mid-beat
cool   = enter(seg(t, 0.5, 1))       // rim: pale → ember → iron #7E4A20
mark scale = lerp(0.7, 1, enter(seg(t, 0.04, 0.5)))
4 embers rise 0.38r on mirrored x offsets (±0.30, ±0.14), fixed phases
```
The only seal with no expansion and no impact — it is the cheapest one and reads that way on
purpose.

### 27 · Shatter — rare
Lands whole, then breaks in three.
```
land = pop(seg(t, 0, 0.3))          // arrives from scale 1.5, overshoots
cracks: 28° at t 0.30 · −52° at t 0.40 · 78° at t 0.52, each drawing over 0.3
        lengths 1.56r / 1.56r / 1.25r, scaleX from transform-origin 50% 50%
part = enter(seg(t, 0.46, 1)) · 0.045r   // halves push apart, alternating sign
6 shards fly to 0.68r max on fixed 61° increments
```
The crack lines are drawn, not animated as pieces — the parting is the 0.045r offset only. That
keeps it cheap enough to run on the win screen without a mesh.

### 28 · Bloom — epic
Four diamonds open outward on a stagger. The quiet one.
```
rings: size 0.267r / 0.507r / 0.747r / 0.987r
       delays 0.00 / 0.12 / 0.24 / 0.36, each easing over 0.5 with drift()
       opacity 0.95 / 0.62 / 0.36 / 0.16
       each scales from 0.3 to 1 of its size — they open, they do not translate
core scale = drift(seg(t, 0, 0.4))
```
**No impact anywhere in it**, no overshoot, no flash. Every other seal has a hit; this one is
the option for players who do not want one.

### 29 · Verdict — epic
Struck from above and driven into the table.
```
5 strike lines at 0° / ±26° / ±48°, drawn downward from −0.62r on a 0.03 stagger
snap  = pop(seg(t, 0.24, 0.56))     // the plate lands with overshoot
hit   = seg(t, 0.26, 0.72)          // flat ellipse ring, 2.0r wide × 0.52r tall
lines fade to 30 % after t 0.6
```
The ring is flat (0.26 aspect) so it reads as running along the table rather than out through
the air — the same trick the activation slam uses in `README-animations.md`.

### 30 · Eclipse — relic
A dark disc crosses a bright one and stops off-centre.
```
born  = enter(seg(t, 0, 0.24))
slide = drift(seg(t, 0.2, 0.74))     // dark disc: −1.5d → +0.10d
rim   = enter(seg(t, 0.5, 0.9))      // outer rim + one spark on the lit edge
bright disc glow falls as the dark disc covers it
```
The only seal that ends **deliberately asymmetric** — the crescent and the spark sit left of
centre. Do not "fix" that; it is the composition.

## The centering rule (this bit matters)
Every seal is positioned from its centre with `left: −size/2`, which only lands on the true
centre if the border is **inside** the box. Without `box-sizing: border-box` the rim is added
outside and the whole mark shifts down-right by exactly its own border width — 13 px on Brand's
13 px rim, 7 px on Shatter's. Set `border-box` on every bordered element.

Sizing is derived, not chosen: the band left free by the win-screen text runs **y 169 to y 526**,
so its centre is **348** and its half-height is **178**. A 45°-rotated diamond of side r has a
half-height of `0.707r`; Bloom's widest ring (0.987r) gives `0.698r ≤ 178`, i.e. `r ≤ 254`.
**r = 236** leaves a margin. Verified: all five sit at cy 348, spanning 178–518 at the widest.

---

# 7 · Shop icons

Every item has a **34 px icon** for the shop grid. Each icon is a **miniature of that item's own
geometry** — the chain links, the water bands, the keyhole, the dashed ring — not a category
glyph. A grid of 30 category glyphs is unreadable; a grid of 30 distinct shapes is scannable.

```
size            34 × 34, 5 px radius
plate           the item's own darkest tone
border          1 px in the item's accent colour
content         2–4 elements maximum
```

Detail drops out the same way the app icons do (`unity-icons/README.txt`): keep the one element
that identifies the item and delete the rest. Cartogram keeps two offset rectangles and loses
the compass; Chainbound keeps two links and loses three.

---

# 8 · Card finishes (separate from the 30)

Finishes are pulled from packs, not bought. They are covered in `README-progression.md`:
**Glossy** 1 in 12 · **Rainbow** 1 in 60 · **Static** 1 in 240. Static cannot be crafted.

**Duplicates are never auto-converted and give no shards.** They stay in the collection; the
player turns them into crafting material in the Deck Builder, at their own pace. A duplicate of
a finished card is worth more material: Glossy 8, Rainbow 40, Static 120.

The Glossy sweep is specified in `README-animations.md` §2 (Pack Open → Hold).

---

# 9 · Integration

```csharp
public enum CosmeticSlot { CardBack, DuelMat, TossCoin, ProfileFrame, VictorySeal }

// One equipped item per slot, per account. No stacking, no loadouts.
public void Equip(CosmeticSlot slot, CosmeticId id);
public CosmeticId GetEquipped(CosmeticSlot slot);   // never null — falls back to vanilla

// Victory seal plays on the win screen, after the result is final
public IEnumerator PlayVictorySeal(CosmeticId seal);
public event Action OnSealSettled;                  // 72% of the beat — screenshot point
```

**Rules**
- Vanilla is always owned and can always be re-equipped. There is no state in which a slot is
  empty.
- The **opponent's** card back, mat and seal must download before the duel starts, not during.
  A missing cosmetic falls back to vanilla silently — never a placeholder, never a spinner.
- Whose mat is shown when both players own one: the **host's**, and the client is told which.
  Pick one rule and never surprise the player with it.
- Seals are deterministic — no `Random`. Ember and shard offsets are constants, so a recorded
  win screen replays identically.
- Reduced motion: hold each seal at its final frame instead of removing it. The seal is how a
  player signs a win; taking it away is worse than not animating it.

## Files

| File | What it is |
|---|---|
| `Cosmetics Catalogue.dc.html` | All 30, drawn as artifacts, with icons. Static — no logic class. |
| `Victory Seals.dc.html` | The five stamps. Scene list and defaults in inline scripts at the top. |
| `victory-seals.jsx` | Every formula in §6, as code. **Read this next.** |
| `animations-v2.jsx` | Timeline engine (starter, unmodified). Not part of the design. |
| `tweaks-panel.jsx` | Tweak controls (starter, unmodified). Not part of the design. |

Tweaks on `Victory Seals`: **Compare all five** puts them in one row at their own offsets, which
is the fastest way to check they still read as five different things; **Win screen text** hides
the banner to see a seal on its own.
