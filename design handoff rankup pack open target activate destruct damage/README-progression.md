# Handoff: Progression — Ranks, Leaderboard, Profile, Cosmetics

Companion document to the RELIQUARY bundle (`README.md` card system,
`README-duel-field.md`, `README-shell-screens.md`, `README-collection-screens.md`,
`README-duel-setup.md`, `README-coin-flip.md`).
Same visual language — **Reliquary**. Read `README.md` first for the shared palette,
type scale and panel vocabulary; this document only covers what is new.

## What is in here

| Screen | File | Purpose |
|---|---|---|
| Rank ladder | `Rank Ladder.dc.html` | The ten seals, their five sub-ranks each, emblem construction |
| Leaderboard | `Leaderboard.dc.html` | Top 50, season and all-time |
| Profile | `Profile Screen.dc.html` | Identity, record, match history, seals |
| Cosmetics shop | `Cosmetics Shop.dc.html` | 20 cosmetic items + the three card finishes |

All four are 1920 × 1080, built on the same chrome: 92 px top bar with a `◀` button on
the left and currency chips on the right, then a 34 px / 64 px padded body.

---

# 1 · The ranked system

## Ten seals, five sub-ranks each

Fifty steps in total. The metal names carry the progression; the word "Seal" is constant.

| # | Rank | RP band | Metal light | Metal dark | Edge |
|---|---|---|---|---|---|
| 1 | Ash Seal | 0 – 399 | `#6E6A62` | `#35322D` | `#8A857B` |
| 2 | Clay Seal | 400 – 799 | `#A5714A` | `#4A2F1C` | `#C08A5E` |
| 3 | Copper Seal | 800 – 1 199 | `#C57B45` | `#5A3016` | `#E09A5C` |
| 4 | Iron Seal | 1 200 – 1 599 | `#8F9AA5` | `#3A424B` | `#AEB9C4` |
| 5 | Silver Seal | 1 600 – 2 099 | `#D6DCE4` | `#6E7783` | `#F0F4F8` |
| 6 | Gold Seal | 2 100 – 2 599 | `#F6E4B4` | `#8E6A22` | `#EBCE8A` |
| 7 | Obsidian Seal | 2 600 – 3 199 | `#5A5470` | `#16131F` | `#8A82A8` |
| 8 | Amber Seal | 3 200 – 3 799 | `#F0A54A` | `#7A3D0C` | `#FFC978` |
| 9 | Relic Seal | 3 800 – 4 499 | `#F8EED6` | `#A6802F` | `#F3DDA4` |
| 10 | Vault Seal | 4 500 + | `#EFE7FA` | `#5E4E8C` | `#EFE7FA` |

Sub-ranks I–V split each band into five equal steps. Ranks 1–4 are 400 RP wide (80 per
sub-rank), 5–6 are 500 (100 each), 7–8 are 600 (120 each), 9 is 700 (140 each).

**Vault Seal is cut by leaderboard position, not RP** — there is no ceiling to divide.
V = top 100, IV = top 600, III = top 2 500, II = top 8 000, I = everyone else above 4 500 RP.

## RP per duel

| Situation | Win | Loss |
|---|---|---|
| Below Gold Seal | **+20 to +25** | **−15 to −20** |
| Gold Seal and above | **+25** | **−25** |

Below Gold the ladder is deliberately generous: climbing outpaces falling, so a new player
trends upward even at a 50 % win rate. From Gold it is symmetric and honest.

**Floors.** You cannot fall out of a rank once you enter it — Gold Seal I is a hard floor.
Sub-ranks inside a rank *can* be lost. Crossing a threshold promotes immediately; there is
no best-of series.

## Emblem construction

One shape, ten states. The emblem is always a **square rotated 45°** in a 104 px box; each
rank adds one layer, so rank is readable without reading text. Sizes below are for the
104 px box — scale proportionally.

| Layer | From rank | Geometry |
|---|---|---|
| Outer diamond | 1 | 88 px, rotated 45°, 2 px `edge` border, fill `linear-gradient(135deg, edge@22%, transparent 70%)` |
| Core | 2 | 20 px, rotated 45°, `linear-gradient(135deg, light, dark)` |
| Inner diamond | 3 | 52 px, rotated 45°, 1 px `edge` at 55–65 % |
| Axis square | 4 | 88 px, **not** rotated, 1 px `edge` at 28–35 % |
| Side pips | 5 | two 8 px diamonds at the left and right midpoints |
| Corner pips | 6 | four 8 px diamonds inset 9 px at the corners |
| Ring | 7 | 100 px circle, 1 px `edge` at 45–55 % |
| Spokes | 8 | 2 px lines at 0° and 90° (rank 9 adds 45° and 135°), `edge` at 18–26 % |
| Filled inner | 9 | the 52 px inner diamond gains a gradient fill; core grows to 24 px |
| Rotating ring | 10 | 104 px dashed circle, `rotate 360°` over 22 s, plus a second 92 px solid ring |

Glow: `drop-shadow(0 0 Npx edge@alpha)` where N and alpha climb with rank —
none below 6, 14 px/.40 at 6–7, 18 px/.45 at 8, 22 px/.50 at 9, 26 px/.60 at 10.

### Sizes
- **96 px** — profile, rank-up screen. Full construction.
- **48 px** — leaderboard rows, duel intro. Drop ring, spokes, side pips.
- **24 px** — HUD, chat, friend list. Outer diamond + core only, 1.5 px border.

Below 48 px the pips and rings turn to mud. Do not scale the 96 px asset down; swap it.

### Sub-rank display
Five diamond pips, 10 px, gap 6. Filled = `edge`; empty = 1 px `edge` at 45 %.
Tier I fills one, tier V fills all five. Roman numerals accompany them in Cinzel 700.

---

# 2 · Leaderboard

Top 50 on one screen, no scrolling.

## Layout
- **Left, 560 px** — podium. Rank 1 gets a full-width card with the Vault Seal emblem at
  112 px and its rotating ring; ranks 2 and 3 sit side by side at 56 px. Then your own
  standing, and the gap to 50th expressed in wins (`1 700 RP … at +25 per win that is 68
  clean wins`) — the single most motivating line on the screen.
- **Right, remaining** — ranks 4–50 in **two columns of 24 grid rows**. Column A holds
  4–27, column B holds 28–50 (23 items in a 24-row track, so both columns share row height).
  Row height ≈ 32 px, gap 5.

## Row columns
`# (40) · seal diamond (8) · name (flex) · RP (80, right) · W–L (76, right) · WR (44, right) · Δ (40, right)`

The seal diamond left of the name is the rank tell: violet-gold
`linear-gradient(135deg,#EFE7FA,#5E4E8C)` for Vault Seal, gold
`linear-gradient(135deg,#F8EED6,#A6802F)` for Relic Seal. Because every player in the top 50
is Vault or Relic, two colours cover the whole board — and the boundary between them is
visible as a colour change partway down column B.

## The Δ column
Position change since the last update. `▲n` in `#7ACD96`, `▼n` in `#C05A44`, `—` in
`#5C513F`. Never colour the whole row by movement; only the arrow.

## Two boards
**Season 3** sorts by seasonal RP, **All Time** by career RP. Same fifty people, renumbered.
The Δ arrows are derived from the difference between the two orderings, which keeps them
consistent when you switch tabs instead of inventing new noise.

Refresh line in the top bar: `updated 4 minutes ago` with a pulsing green dot. Real cadence
is up to the backend — five minutes is a reasonable floor for a board this small.

---

# 3 · Profile

## Layout
- **Left, 520 px** — identity card (456 px) over a showcase panel.
- **Right, remaining** — a 54 px tab strip over the active tab's content.

## Identity card
Portrait 148 × 148 in the card-artwork frame (7 px padding,
`linear-gradient(160deg,#3E2C16,#1A1108)`, 2 px `#C8A45C`, inner 1 px `#C8A45C` at 65 %).
The **level crest** overlaps its bottom-right corner by 14 × 10 px — the same hexagon
`clip-path: polygon(50% 0,100% 20%,100% 66%,50% 100%,0 66%,0 20%)` as the card's level badge,
48 × 52 px, 2 px inset dark core.

Below the portrait: eyebrow `DUELIST`, name in Cinzel 700/40, `#4417 · joined March 2026`,
then the **title chip** — a button that opens a 300 px popover with the player's unlocked
titles, each with its unlock condition as a subtitle.

Then a divider, the rank block (66 px emblem + name + RP bar + `180 RP to Relic Seal I`),
another divider, and three small facts: hours, seals, decks.

Dividers are `linear-gradient(90deg,transparent,rgba(200,164,92,.4),transparent)`, 1 px.

## Showcase
Three cards the player pins to their profile, at whatever finish they own. In the mock they
are card backs in the three type colours (gold / teal `#8FC6D2` / violet `#B9A3E0`) with a
gradient name bar rather than a name — deliberately blank, because these are the player's
cards, not ours.

## Tabs

**Overview.** Four stat tiles (Duels, Win rate, Best streak, Avg duel), each bordered in its
own accent and lifting its border on hover. Then attribute affinity as six bars in the
attribute colours, collection progress by rarity, and the season track.

Attribute colours (matching the card system): Fire `#E0603A`, Water `#8FC6D2`,
Light `#F3DDA4`, Dark `#B9A3E0`, Earth `#B98A4E`, Wind `#8FD2A8`.
Rarity colours: Common `#A2917A`, Rare `#8FC6D2`, Epic `#B9A3E0`, Relic `#EBCE8A`.

Season track: eight diamond nodes on a 2 px rail, filled nodes 22 px with the gold gradient,
the current node 26 px with a 2 px `#F3DDA4` border and a 22 px glow, future nodes hollow.

**Match history.** Rows of result · opponent · your deck · length · mode · RP. The result
block is a 4 px colour bar plus the word: `#7ACD96` / `A8E4BE` for a win, `#C05A44` /
`D89282` for a loss. Row count is a tweak (4–10).

**Seals.** A 4-column grid. Earned seals get the warm card gradient, a gold-bordered
diamond mark and the month earned in green. Locked seals get a flat `rgba(0,0,0,.4)` panel,
a dim mark and a progress bar with the count underneath.

---

# 4 · Cosmetics

Twenty items across six slots. Nothing here touches gameplay.

| Slot | Items | Where it shows |
|---|---|---|
| Card sleeve | Ashen Weave, Tomb Gilt, Deep Current, Obsidian Lattice | The back of your cards in every duel |
| Avatar frame | Iron Bracket, Gilded Reliquary, Amber Halo, Vault Ring | Around your portrait on profile, duel intro, leaderboard hover |
| Toss coin | Copper Trial, Silver Warden, Vault Coin | The coin in the opening toss (see `README-coin-flip.md`) |
| Duel mat | Stone Table, Ember Circle, Starless Vault | The field surface — your half only |
| Profile title | Sealbreaker, Ash Collector, Warden's Bane | Under your name on profile and duel intro |
| Victory seal | Shatter, Bloom, Eclipse | The animation on your win screen |

## Pricing

| Item | Rarity | Price |
|---|---|---|
| Ashen Weave | Common | 600 coins |
| Iron Bracket | Common | 800 coins |
| Copper Trial | Common | 700 coins |
| Stone Table | Common | 1 000 coins |
| Sealbreaker | Common | 900 coins |
| Deep Current | Rare | 1 800 coins |
| Silver Warden | Rare | 1 500 coins |
| Ember Circle | Rare | 2 200 coins |
| Ash Collector | Rare | 1 400 coins |
| Shatter | Rare | 1 600 coins |
| Amber Halo | Rare | 180 shards |
| Tomb Gilt | Epic | 2 400 coins |
| Gilded Reliquary | Epic | 2 000 coins |
| Obsidian Lattice | Epic | 240 shards |
| Starless Vault | Epic | 280 shards |
| Bloom | Epic | 200 shards |
| Vault Ring | Relic | 400 shards |
| Vault Coin | Relic | 320 shards |
| Eclipse | Relic | 360 shards |
| Warden's Bane | Relic | **not for sale** |

Two currencies, and the split is the point: **coins** are earned by playing, **shards** come
from the season track and ranked rewards. Anything animated (Vault Ring, Bloom, Eclipse, Amber
Halo) or violet costs shards. That way the flashiest items cannot be bought with grind alone.

Warden's Bane is earned only — beat the Warden without losing a relic. Keep at least one
item per slot unpurchasable; it makes the rest of the shop credible.

## Shop screen
Grid of 5 × 4 tiles (≈ 256 × 199 each) with a 420 px detail column. Each tile: a 96 px
swatch, the name in Cinzel 600/16, the slot in Oswald 9 letterspaced .2em, and the price
with its currency diamond. Tile border and swatch border carry the rarity colour; hover
lifts 5 px. Selection draws a 2 px rarity-coloured outline at `inset:-1px`.

The detail column composes its ornament from the selected item's rarity colour through two
CSS custom properties (`--acc`, `--soft`) — the one place in these screens where a value is
injected rather than written literally, because it genuinely changes at runtime.

---

# 5 · Card finishes

Three finishes on top of the normal card. **A finish belongs to the copy you pull, not to the
card** — the same card can sit in your vault plain, glossy and rainbow at once, and each
counts as its own collection entry. Nothing about effect, level, DMG or DEF changes.

| Finish | Rate | Crafting material |
|---|---|---|
| Glossy | 1 in 12 (8.3 %) | 8 |
| Rainbow | 1 in 60 (1.7 %) | 40 |
| Static | 1 in 240 (0.4 %) | 120 |

**Duplicates are never auto-converted.** They stay in the collection as duplicates; the player
turns them into crafting material in the Deck Builder, at their own pace. Duplicates give no
shards. The numbers above are what a duplicate of that finish is worth in material.

Static cannot be crafted. It only comes out of a pack.

## Glossy
A single specular bar sweeping across the whole card.

```
overlay: 46% wide, 160% tall, rotate(18deg), top:-30%
fill:    linear-gradient(90deg, transparent, #fff@50% 46%, #fff@72% 52%, transparent)
blend:   screen
motion:  translateX(-160% → 260%), 3.4s ease-in-out, infinite
```

In Unity: one additive quad masked to the card, animated across UV, or a scrolling
`_MainTex` offset on an unlit additive material.

## Rainbow
A hue band under a fine diffraction grating.

```
layer 1: linear-gradient(115deg, #E0603A, #F3DDA4 14%, #8FD2A8 28%, #8FC6D2 42%,
                         #B9A3E0 58%, #E0603A 72%, #F3DDA4 86%, #8FC6D2 100%)
         background-size: 300% 100%; blend: color-dodge; opacity: .55
         motion: background-position 0% → 300%, 6s linear, infinite
layer 2: repeating-linear-gradient(115deg, #fff@14% 0 2px, transparent 2px 9px)   /* static */
```

The second layer is what makes it read as foil rather than a rainbow gradient. Keep it still
while layer 1 moves.

In Unity: sample a 1D hue ramp by `dot(viewDir, normal)` so the colour shifts with the
card's tilt instead of on a timer — much better on a card the player can rotate.

## Static
Scanlines, a noise grid and a rolling band. The rarest, and the only one that flickers.

```
layer 1: repeating-linear-gradient(0deg, #C8DCEB@30% 0 1px, transparent 1px 4px)
         motion: translateY(0 → 8px), .55s steps(4), infinite
layer 2: repeating 1px grid, both axes, #fff@50% / #fff@28%; blend: screen
         motion: opacity flicker .30 → .62 → .18 → .55 → .24 → .48 → .20, 1.1s steps(1)
layer 3: 16px horizontal band, linear-gradient(180deg, transparent, #C8E6FF@34%, transparent)
         motion: translateY 2.2s linear, infinite   /* roll down the card */
```

The flicker uses `steps(1)` on purpose — smooth interpolation reads as a fade, not
interference. If the game has an accessibility setting for reduced motion, Static must fall
back to a still frame at 30 % opacity; it is the one effect that can bother people.

---

# 6 · Integration notes

```csharp
// Rank
public struct Seal { public int Rank;      // 1..10
                     public int Tier; }    // 1..5
public static Seal SealFor(int rp, int leaderboardPos);
public static int  RpDelta(int rank, bool won);   // rank < 6 -> 20..25 / -15..-20
                                                 // rank >= 6 -> +25 / -25
public event Action<Seal, Seal> OnRankChanged;    // from, to — drives the rank-up screen

// Cosmetics
public enum Slot { Sleeve, AvatarFrame, TossCoin, DuelMat, Title, VictorySeal }
public void Equip(Slot slot, string cosmeticId);

// Finishes
public enum Finish { Plain, Glossy, Rainbow, Static }
// A collection entry is (cardId, finish). Count them separately.
```

**Rules**
- The rank floor is server-side. Never let the client compute a demotion past a rank boundary.
- Cosmetics are cosmetic. No slot may affect card visibility, readability of an opponent's
  board, or animation timing that the opponent waits on.
- The opponent sees your sleeve, mat (your half only), coin and title. They do **not** see
  your victory seal until you win.
- Finishes must be legible: no finish may reduce contrast on the effect box or the DMG/DEF
  numbers. Test Rainbow and Static against the parchment panel specifically.

## Files

| File | What it is |
|---|---|
| `Rank Ladder.dc.html` | The ten seals, emblem construction, sub-rank anatomy, promotion rules |
| `Leaderboard.dc.html` | Top 50, season / all-time |
| `Profile Screen.dc.html` | Identity, overview, match history, seals |
| `Cosmetics Shop.dc.html` | 20 cosmetics + the three finishes with live overlays |
| `README.md` | Card system handoff — read first for palette and type scale |
| `README-coin-flip.md` | Coin toss, relevant to the toss-coin cosmetics |
