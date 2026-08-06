# RELIQUARY — Official Rulebook

*Rules as implemented in build 0.1.2. Where this document and the game engine disagree, the engine is right and this document has a bug — please report it.*

---

## Contents

1. [Object of the Game](#1-object-of-the-game)
2. [The Cards](#2-the-cards)
3. [The Board](#3-the-board)
4. [Starting a Duel](#4-starting-a-duel)
5. [Turn Structure](#5-turn-structure)
6. [Mana](#6-mana)
7. [Summoning Monsters](#7-summoning-monsters)
8. [Battle Positions & Face-Down Cards](#8-battle-positions--face-down-cards)
9. [Spells](#9-spells)
10. [Artifacts](#10-artifacts)
11. [The Player Card](#11-the-player-card)
12. [Reliquaries & the Extra Deck](#12-reliquaries--the-extra-deck)
13. [Effects](#13-effects)
14. [Targeting](#14-targeting)
15. [Chains & Responses](#15-chains--responses)
16. [The Battle Phase](#16-the-battle-phase)
17. [Destruction, the Graveyard & Banishment](#17-destruction-the-graveyard--banishment)
18. [Winning and Losing](#18-winning-and-losing)
19. [Deckbuilding](#19-deckbuilding)
20. [Keyword Glossary](#20-keyword-glossary)
21. [Advanced Rulings & Edge Cases](#21-advanced-rulings--edge-cases)

---

## 1. Object of the Game

Two duelists fight with decks of 40–80 cards, each led by a **Player Card** (their hero). You win by any of these:

- Reducing your opponent's **Life Points to 0**.
- Your opponent must draw and **their deck is empty** (deck-out).
- Your opponent **surrenders**.

Your starting Life Points are printed on your Player Card — most heroes bring **8000 LP**, greedy ones bring 7500, the patient Tithekeeper brings 8500.

---

## 2. The Cards

### 2.1 Card types

| Type | Where it lives | What it does |
|---|---|---|
| **Monster** | Deck → hand → Monster Zone | Fights. Has ATK, DEF, a Level (1–3), a Type and an Attribute. |
| **Spell** | Deck → hand → Spell Zone | One-shot effects. Activated from hand or set face-down first. Goes to the Graveyard after resolving. |
| **Artifact** | Deck → hand → Artifact Zone | Permanents. Stay on the field, some carry ongoing bonuses, some equip to monsters. |
| **Reliquary** | **Extra Deck** → Monster Zone | Boss monsters with printed summon conditions. Never in your main deck, never in your hand. |
| **Player Card** | Player Card Zone | Your hero. On the field for the whole duel, brings your starting LP and one activated ability. |

### 2.2 Card anatomy (monsters)

- **Level (1–3):** determines the tribute cost to Normal Summon it (see §7.1).
- **ATK / DEF:** battle values. The value that matters is the one matching the monster's current battle position.
- **Type** (Dragon, Beast, Mecha, Demon, Animal, Myth, …) and **Attribute** (Fire, Water, Light, Dark, Earth, Wind): categories that other cards' effects search, buff or restrict by. RELIQUARY effects deliberately say *"add 1 Water Level 2 monster"* rather than naming cards — categories are the connective tissue of the game.
- **Effects:** zero or more abilities (see §13).

### 2.3 Rarity and finishes

Every card has a rarity — **Common, Uncommon, Rare, Legendary** — which matters for collecting and crafting, never for gameplay.

Every *copy* of a card additionally has a **finish**: Plain, Glossy, Rainbow or Static. Finishes are purely cosmetic. They are visible in your deck, in the deck builder and on the field during duels — a Glossy card in your deck is Glossy in the game. Face-down cards never show their finish to the opponent; sparkle would be information.

---

## 3. The Board

Each player's side of the field has:

| Zone | Slots | Notes |
|---|---|---|
| **Monster Zones** | 5 | Monsters and summoned Reliquaries. |
| **Spell Zones** | 3 | Set or resolving spells. |
| **Artifact Zones** | 2 | Artifacts in play. |
| **Player Card Zone** | 1 | Your hero. Cannot be attacked. |
| **Deck** | 1 pile | Face-down, shuffled. |
| **Extra Deck** | 1 pile | Your Reliquaries (up to 20). |
| **Graveyard** | 1 pile | Face-up, public. Destroyed and used cards. |
| **Banished Zone** | 1 pile | Face-up, public. Cards removed from play. |

If a zone row is full, nothing else can be placed there — a summon or set that finds no free slot **fizzles**.

---

## 4. Starting a Duel

1. **Coin toss.** The winner **chooses** whether to go first or second.
2. **Life Points** are set by each player's hero.
3. **Opening hands:** the player going **first draws 5** cards; the player going **second draws 6**.
4. **Mana:** both players start the duel with a **3-Mana income** (see §6).

The starting player's first turn has two restrictions:

- They **skip their first Draw Phase** (no free 6th card).
- They **have no Battle Phase on turn 1**.

Going second is compensated with the extra card and the first chance to attack.

---

## 5. Turn Structure

Each turn passes through five phases, in order:

### 5.1 Draw Phase
Draw 1 card. (Skipped on the very first turn of the duel by the starting player.) If you must draw and cannot, you lose — see §18.

### 5.2 Standby Phase
Effects that read *"during your Standby Phase"* trigger now.

### 5.3 Main Phase
In any order, as often as your resources allow:

- **Normal Summon** a monster (once per turn by default).
- **Special Summon** monsters whose own conditions allow it.
- **Reliquary Summon** from your Extra Deck (conditions + costs, §12).
- **Activate** spells from your hand, or **Set** them face-down.
- **Play** artifacts.
- **Activate** Ignition effects of your cards on the field — including your **Player Card**.
- **Activate** Hand-Ignition effects from your hand and Graveyard-Ignition effects from your graveyard.
- **Change battle positions** (limited, §8.1).
- Enter the Battle Phase, or end your turn.

### 5.4 Battle Phase
Attack with your monsters (§16). The starting player has no Battle Phase on turn 1. You may return from battle to a second Main-Phase-like state only in the sense that ending battle returns you to the Main Phase context before ending the turn.

### 5.5 End Phase
- Effects that read *"during your End Phase"* trigger.
- Temporary modifiers expire (see §20 — *until end of turn*).
- Control of stolen monsters returns (§20 — *Take Control*).
- **Hand limit:** if you hold more than **8 cards**, you choose and discard down to 8.

---

## 6. Mana

Mana pays for spells, effects, hero abilities and Reliquary summons.

- **Income:** you start the duel with an income of **3**. It grows by **+1 each of your turns**, capped at **10**.
- **Refill, not carry-over:** at the start of your turn your Mana is set to your income. Unspent Mana from last turn is **gone** — by default.
- **Gained Mana** (from effects) adds to your current pool and can exceed your income for that turn.

### 6.1 Mana across turns

Two effect families deliberately break the refill rule:

- **Mana next turn** (e.g. Otho the Vaultkeep): you bank a bonus that is **added to your next refill**.
- **Mana debt** (e.g. Ossian the Tithekeeper, Manacle cards): your opponent's next refill is **reduced** by the stated amount.

Credits and debts apply exactly once, at the next refill, then clear.

### 6.2 Draining current Mana

*Drain* effects that hit the opponent's **current pool** only matter during **their** turn (in your turn their pool is usually empty and refills anyway). That is why serious Mana attackers use *next turn* debts instead.

---

## 7. Summoning Monsters

### 7.1 Normal Summon

Once per turn, you may summon one monster from your hand into a free Monster Zone, **in Attack Position, face-up**:

| Level | Tribute cost |
|---|---|
| Level 1 | free |
| Level 2 | tribute 1 monster you control |
| Level 3 | tribute 2 monsters you control |

Tributes are sent to the graveyard as a **cost** — they are paid immediately and are not undone by anything that happens later.

Some effects grant an **extra Normal Summon** in the same turn.

### 7.2 Special Summon

Monsters with a printed self-summon condition (*"You can Special Summon this card (from your hand) if …"*) can put themselves onto the field during your Main Phase, without using your Normal Summon. Conditions are checked at the moment of summoning — common ones include: a named/categorized card on your field, the opponent controlling monsters, artifacts you control, face-down monsters you control, cards in graveyards, and more.

When a card is Special Summoned **face-up**, its controller chooses Attack or Defense Position.

Effects can also Special Summon monsters from hand, deck, graveyard or the banished zone — each effect states its source. Effects that summon **face-down** place the monster in face-down Defense Position (see §8.2).

### 7.3 Summon responses

Every summon (Normal, Special, Reliquary) opens a **response window** (§15) before the game moves on. *"When the opponent summons"* triggers fire here, as do Quick effects.

### 7.4 Summoning sickness — there is none

Monsters may attack the turn they are summoned. The game balances tempo elsewhere (no first-turn battle, tribute costs, response windows).

---

## 8. Battle Positions & Face-Down Cards

### 8.1 Positions

A face-up monster is in **Attack** or **Defense** Position. In your Main Phase you may change each monster's position **once per turn** (this uses the monster's position change, not a global budget). Position changes can be locked by effects.

- Attack Position uses **ATK**; the monster can attack.
- Defense Position uses **DEF**; the monster cannot attack.

### 8.2 Face-down cards

There is **no manual "Set" for monsters** — face-down monsters enter play only through effects (e.g. Sable the Veilkeeper, Lyria cards, *"turn target monster face-down"*). A face-down monster:

- is always in **Defense Position**;
- hides its identity, stats and finish from the opponent (you may inspect your own);
- **loses all accumulated stat modifications** the moment it is turned face-down — turning a buffed attacker face-down is a purge (§20);
- cannot attack and does not activate its effects.

### 8.3 Flipping face-up

A face-down monster is flipped face-up when:

- an effect flips it (*Flip target face-up*),
- its controller changes its position (their once-per-turn position change), or
- it is **attacked** — the attack flips it before damage is calculated.

When a face-down monster is flipped face-up by any means, its **Flip effects** trigger (*"When this card is flipped face-up: …"*). Exception: if the flip happens deep inside a chain (already at maximum chain depth), the flip effect cannot start and is lost — see §15.4.

---

## 9. Spells

### 9.1 Playing spells

From your hand in your Main Phase you can:

- **Activate** a spell directly — pay its Mana cost, choose targets, resolve.
- **Set** it face-down into a Spell Zone — costs nothing, hides it.

A **set spell cannot be activated in the same turn it was set.** From the next turn on, you can activate it from the field.

### 9.2 Spell speed

- **Normal** spells: only during your own Main Phase.
- **Quick** spells: additionally during any **response window** (§15) — including in the opponent's turn, *if the spell was already set on your field* (and not set this turn). Spells respond from the field, not from your hand — the exception is monsters with Hand-Quick abilities, see §13.2.

### 9.3 Resolution

After a spell resolves it goes to the **graveyard**, unless the spell itself moved somewhere else during resolution (banished as a cost, shuffled into the deck, etc.).

---

## 10. Artifacts

- Played from hand into a free **Artifact Zone** during your Main Phase (Mana cost as printed).
- They **stay** on the field; their printed bonuses last while they remain.
- Some artifacts **equip** to monsters (via their own effect or another card's): the equipped monster gains the artifact's bonuses (e.g. +ATK). If the equipped monster leaves the field, the equipment relationship ends.
- Some artifacts protect: a **shield artifact** may shatter in place of a card that would be destroyed (§17.3). One important limit: **a replacement destruction is final** — it cannot itself be replaced by a second shield.
- Artifacts can be set face-down onto the field by effects (e.g. Bronn the Relicwright); a face-down artifact hides its identity until used or flipped by the game's rules for that effect.

---

## 11. The Player Card

Your hero sits in its own zone for the entire duel.

- It **cannot be attacked**, targeted for combat, or destroyed by battle. Its LP value defined your starting Life Points — after that the LP live with the player, not the card.
- It has **one ability**, following a universal law: **2 Mana, once per turn.** The twelve heroes differ only in *what* the ability does and in their LP price tag.
- Hero abilities are activated in your Main Phase like any Ignition effect — with one exception: **Nix the Tidecaller's ability is Quick** and can be activated in response windows, including during the opponent's turn. The Player Card counts as a card on the field for response purposes.
- Hero abilities respect all normal effect rules: costs, targeting, once per turn, and they enter chains like any other activation.

Heroes are acquired through starter decks and the **Hero Cache** in the shop. They cannot be crafted, dusted into play, or pulled from regular packs.

---

## 12. Reliquaries & the Extra Deck

Reliquaries are the game's boss monsters. They live in your **Extra Deck** (up to 20 cards) and are summoned during your Main Phase by meeting their printed **conditions** and paying their printed **costs**.

### 12.1 Conditions (checked, not paid)

A Reliquary may require any combination of: a named or categorized card on your field (possibly several), your LP below the opponent's or below a number, the opponent controlling more monsters than you or at least N monsters, a minimum current Mana, artifacts on your field or in your graveyard, face-down monsters you control, a monster with an equipped artifact, at least N cards in your graveyard or banished zone, you controlling no monsters, or you controlling at least N monsters.

### 12.2 Costs (paid on summon)

In addition to a **Mana cost**, a Reliquary can demand: banishing monsters from your graveyard, tributing another monster you control, tributing several of your monsters — and, for the harshest ones, **tributing monsters on the opponent's field**. A summon that eats an enemy monster is itself removal; that is the design, not an accident.

Costs are paid immediately. The summon then opens a response window like any other summon.

### 12.3 Reliquaries leave differently

When a Reliquary would go to the **hand, the deck or the graveyard**, it returns to the **Extra Deck** instead. Bounce effects, grave recursion and shuffle effects all respect this. A Reliquary can still be **banished** — the banished zone is the one place that holds them.

---

## 13. Effects

### 13.1 Reading an effect

An effect line has up to four parts:

1. **Trigger** — when it can/does fire (see 13.2).
2. **Cost** — everything before the semicolon that is paid on activation (*"You can pay 2 Mana and discard 1 card;"*). Costs are paid **immediately**, before anyone can respond. Costs are never refunded.
3. **Targets** — chosen at activation (§14).
4. **Resolution** — what happens, after all responses have resolved.

### 13.2 Triggers

| Trigger | Fires |
|---|---|
| **Ignition** | Manually, in your own Main Phase, from a face-up card on the field. |
| **Quick** | Manually, in any open response window — your turn or theirs — from a face-up card on the field. |
| **Hand-Ignition** | Manually in your Main Phase, from your **hand** (monster reveals itself to act). |
| **Hand-Quick** | In any response window, from your **hand** — the effect the opponent never sees coming. Discarding the card is usually part of the cost. |
| **Graveyard-Ignition** | Manually in your Main Phase, from your **graveyard**. |
| **On Summon / On Normal Summon** | Automatically when this card is summoned (you are asked whether to use optional ones). |
| **On Destroyed** | When this card is destroyed. |
| **On Opponent Summon** | In the response window of an opponent's summon. |
| **Flip** | When this face-down card is turned face-up (§8.3). |
| **Standby / End Phase** | In the respective phase of your turn. |
| **On Activate** | The body of a spell/artifact — fires when the card itself is played. |

### 13.3 Once per turn

*"Once per turn"* is tracked **per card copy, per effect**. Two copies of the same monster each get their use. A card that leaves the field and returns is a new object and starts fresh.

### 13.4 Infused effects

Some effects are marked **INFUSED**. They come in two kinds:

- **Standalone** — an independent ability. Its INFUSED tag marks it as premium, nothing else changes mechanically.
- **Coupled** — an **upgrade of the effect printed directly above it**. A Coupled effect and its base effect share one activation: **you may use one of the pair per turn, never both.** Choosing is part of activating.

A card can carry several Infused effects; each Coupled effect pairs with its own preceding base effect.

### 13.5 Negation

- *Negate* effects mark a card's **effects as negated until end of turn**: its abilities cannot be activated and its ongoing effects are switched off.
- Negating an **activation** in a chain: the activation still resolves as "negated — nothing happens", but **its costs stay paid**. Negation punishes; it does not refund.

---

## 14. Targeting

- Targets are declared **when the effect is activated**, before responses.
- Candidates are filtered by the effect's categories (type, attribute, exact level, maximum ATK, name fragment, "mentions" text).
- **"Up to N"** effects may choose fewer targets — you can stop early.
- **Cannot be targeted** protects only against **the opponent's** effects; your own effects may still target your protected monster.
- **Fizzle rule:** between targeting and resolution, the world can change. At resolution each target is re-checked: if it changed **zone**, changed **controller**, or changed its **face-up/face-down state**, it is no longer a legal target and the effect **fizzles for that target** (other targets still resolve). This is the standard counterplay: kill, bounce, steal or flip the target in response, and the effect hits air.
- Effects without targets (draws, mills, global effects) cannot fizzle this way.

---

## 15. Chains & Responses

### 15.1 Response windows

After each of these events, the game pauses and offers the **non-acting player** (then back and forth) the chance to respond:

- a **spell or effect activation**,
- an **artifact** being played,
- a **summon** (any kind),
- an **attack declaration**,
- certain **phase transitions**.

Legal responses: Quick effects of face-up field cards (including the Player Card), set Quick spells (not set this turn), Hand-Quick monster effects, and "when the opponent summons" triggers in summon windows.

### 15.2 Building a chain

A response is itself an activation — it opens its own window. This nests: activation A → response B → response C. The chain is capped at **3 links**; at maximum depth no further responses are offered.

Costs of every link are paid **at activation time**, top to bottom as the chain builds.

### 15.3 Resolving

When both players pass, the chain resolves **backwards**: the last link first, the original activation last. Every link's targets are re-checked by the fizzle rule when its turn comes.

The on-screen **chain tracker** (top of the screen) shows exactly this: links are numbered as they are added under **BUILDING CHAIN**, then the header flips to **RESOLVING** and the panel works back down, greying out finished links. A lone effect nobody answers is not a chain and shows nothing.

### 15.4 Deep-chain exception

Triggers that would fire during resolution at **maximum chain depth** (e.g. a Flip effect from an attack deep in a chain) cannot start and are lost. The 3-link cap is absolute.

---

## 16. The Battle Phase

### 16.1 Declaring attacks

Each of your face-up Attack-Position monsters may attack **once per turn**. Choose an attacker and a target:

- an opponent's monster, or
- the opponent **directly** — only if they control **no monsters**.

Effects can grant a monster an **additional attack** in the same Battle Phase.

The declaration opens a **response window**. After responses resolve, the attack is re-validated:

- If the **attacker** left the field → the attack is cancelled.
- If the **target** left the field → the attack fizzles (it does **not** retarget or become direct).
- If a **direct attack** was declared and the opponent now controls a monster → the direct attack is no longer possible.

*Taunt* effects can force the opponent's attacks onto a specific monster this turn.

### 16.2 Damage calculation

**Against an Attack-Position monster** (compare ATK vs ATK):

| Result | Outcome |
|---|---|
| Attacker higher | Defender is destroyed; its controller takes the **difference** as battle damage. |
| Defender higher | Attacker is destroyed; **your** LP take the difference. |
| Equal | **Both** are destroyed; no damage. |

**Against a Defense-Position monster** (compare attacker's ATK vs defender's DEF):

| Result | Outcome |
|---|---|
| ATK higher | Defender is destroyed. **No damage** goes through — there is no piercing. |
| DEF higher | Nothing is destroyed; the **attacker's controller** takes the difference. |
| Equal | Nothing happens — the attack bounces off. |

**Attacking a face-down monster:** it is flipped face-up first (Flip effects fire, §8.3), then damage is calculated against its position — which is always Defense.

**Direct attacks** deal the attacker's full ATK as battle damage.

*Prevent battle damage* effects stop all battle damage to their player for the rest of the turn; effect damage is unaffected.

### 16.3 Current values

Battle always uses **current** ATK/DEF — base value plus permanent bonuses plus temporary (until end of turn) modifiers plus equipment. *Purge* strips all of it back to print. *Swap* and *Copy* effects override values until end of turn.

---

## 17. Destruction, the Graveyard & Banishment

### 17.1 Ways to leave the field

- **Destroyed** — by battle or by a destroy effect → graveyard. Triggers *On Destroyed* effects.
- **Sent to the graveyard** — tributes, costs, discards. This is **not** destruction: *On Destroyed* does not trigger, destruction protection does not help.
- **Bounced** — returned to hand (Reliquaries: to the Extra Deck).
- **Shuffled into the deck.**
- **Banished** — removed to the banished zone. Banished cards are out of the game unless an effect explicitly retrieves or summons them from there.

The distinction matters constantly: *"cannot be destroyed"* does nothing against a tribute, a bounce or a banish.

### 17.2 Destruction protection

*Cannot be destroyed this turn* protects against battle **and** effect destruction until the End Phase.

### 17.3 Replacement (shield artifacts)

If a card you control would be destroyed and you control a **shield artifact** (*"shatters in place of …"*), you may destroy the artifact instead; the original card survives. Rules:

- The artifact cannot shield **itself**.
- You are asked each time; shielding is optional.
- **A replacement destruction is final.** The shattering shield cannot be saved by another shield. (Two Bulwark Prisms cannot juggle a destruction between them forever.)

### 17.4 Public knowledge

Both graveyards and both banished zones are **public**: any player may inspect them at any time. Deck contents and hand contents are private; deck **counts** and hand **counts** are public.

---

## 18. Winning and Losing

You **lose** immediately when:

1. your **Life Points reach 0** (battle damage or effect damage);
2. you must **draw** (Draw Phase or any effect) and your **deck is empty** — the draw that cannot happen ends the duel on the spot, even mid-effect;
3. you **surrender** (the button is always available).

There are no draws by simultaneous LP loss in the current rules; damage is always dealt to one player at a time — *"both players take damage"* effects apply sequentially.

---

## 19. Deckbuilding

A legal deck:

| Rule | Value |
|---|---|
| Main deck size | **40–80** cards |
| Copies of one card | max **3** (across all finishes) |
| Extra Deck | up to **20** Reliquaries |
| Hero | exactly **1** Player Card |
| Ownership | you can only play copies you own |

### 19.1 The banlist

The banlist can tighten the copy rule per card: **0** (banned), **1** (limited), **2** (semi-limited). The current list and its history are in the game's BANLIST screen. The server enforces both the banlist and ownership when you queue — an illegal or unowned deck is rejected before the duel starts.

### 19.2 Big decks are a choice

40 cards is the floor, not the law. Self-mill strategies (Silt the Dredger, Gravemaw) genuinely want 55–60 cards — the deck is fuel, and the deck-out rule (§18) is the price.

---

## 20. Keyword Glossary

| Keyword / phrase | Meaning |
|---|---|
| **Add … to your hand** | Take from the stated zone (deck searches shuffle afterwards). |
| **Additional attack** | The monster may attack once more this Battle Phase. |
| **Banish** | Move to the banished zone (§17.1). |
| **Cannot attack this turn** | Attack declarations with this monster are impossible until End Phase. |
| **Cannot be targeted** | Only your opponent's targeting is blocked; yours is not (§14). |
| **Control … until End Phase** | You take a monster; it returns automatically in the End Phase. |
| **Copy ATK/DEF** | Your monster's values become the target's until end of turn. |
| **Discard** | From hand to graveyard. As a cost: chosen by the paying player, paid instantly. |
| **Extra Normal Summon** | Raises your Normal Summon count this turn. |
| **Flip face-up / face-down** | See §8. Face-down strips all modifications. |
| **Lock position** | The monster cannot change battle position this turn. |
| **Mill** | Send the top N cards of a deck to its graveyard. |
| **Negate** | See §13.5. Until end of turn. |
| **Prevent battle damage** | Its player takes no battle damage this turn. |
| **Protect (cannot be destroyed)** | Battle and effect destruction are both blocked this turn (§17.2). |
| **Purge** | Remove ALL temporary and permanent ATK/DEF modifications from the target. |
| **Return to hand (bounce)** | Hand for normal cards; Extra Deck for Reliquaries. |
| **Shuffle into the deck** | The card leaves entirely; the deck is shuffled. |
| **Summon lock** | The opponent cannot Special Summon this turn. |
| **Swap ATK/DEF** | The target's values trade places until end of turn. |
| **Taunt** | The opponent must attack this card this turn if they attack at all. |
| **Tribute** | Send to the graveyard as a cost (not destruction). |
| **Until end of turn** | Expires in the End Phase of the current turn — whoever's turn it is. |

---

## 21. Advanced Rulings & Edge Cases

These are the questions that come up at the table. All of them are engine-verified.

**21.1 — Costs vs. negation.** You activate *Palimpsest* (pay 2 Mana, discard 1; draw 2). Your opponent negates the activation. You do **not** draw — but the Mana and the discarded card are gone. Costs are never refunded (§13.1, §13.5). Corollary: if the discard itself was your plan (graveyard setup), negation may hurt less than it looks.

**21.2 — Fizzle by flip.** Your removal targets a face-up monster. In response, its owner turns it face-down. At resolution the target's face changed → the effect **fizzles** (§14). The same works with a bounce, a steal, or a tribute in response.

**21.3 — Attacking into a disappearing target.** You attack monster X; in the response window X is bounced. The attack fizzles — it does **not** convert into a direct attack, and the attack still counts as used (§16.1).

**21.4 — Direct attack spoiled.** You declare a direct attack; in response the opponent Special Summons a blocker from hand (Hand-Quick). The direct attack is cancelled at re-validation (§16.1). The attack is spent.

**21.5 — Face-down resets buffs.** A monster with +1000 permanent ATK is turned face-down. All modifications are stripped (§8.2). Flipping it back up does not restore them. Maeva the Pale's whole identity rests on this ruling.

**21.6 — Two shields, one destruction.** Monster M would be destroyed; shield artifact A shatters instead. Shield artifact B may **not** shatter in place of A — a replacement destruction is final (§17.3). B survives for the next, separate destruction.

**21.7 — Tribute is not destruction.** Sacrilegion tributes an opponent's monster as a summon cost. *"Cannot be destroyed"* does not save it; *On Destroyed* triggers do not fire; shield artifacts do not offer to shatter. It was sent, not slain (§17.1).

**21.8 — Reliquary recursion.** An effect says *"return 1 monster from your graveyard to your hand"*. It can never hit a Reliquary — Reliquaries are not in the graveyard; wherever they would go, they return to the Extra Deck instead (§12.3). Only banishing truly parks a Reliquary.

**21.9 — Chain cap in practice.** A activates a spell (link 1), B responds (link 2), A responds again (link 3). B would like to answer — but the chain is capped at 3 links; no window opens (§15.2). Plan your interaction for the last word.

**21.10 — Flip effect at maximum depth.** Deep in a 3-link chain, an attack flips a face-down monster. Its Flip effect **cannot start** (the chain is full) and is lost forever (§15.4). It does not "wait" for the chain to finish.

**21.11 — Deck-out mid-effect.** You control Vess and activate *Palimpsest* with 1 card left in the deck. The first draw succeeds, the second cannot — you **lose immediately**, mid-resolution (§18). Silt players: count your cards.

**21.12 — Once per turn and copies.** Two copies of the same monster with *"Once per turn: …"* each use their own effect in the same turn (§13.3). The lock is per copy, not per name.

**21.13 — Coupled effects share one slot.** A card reads: effect A, and below it INFUSED (Coupled) effect A+. You may activate A **or** A+ this turn, never both (§13.4). Next turn you choose fresh.

**21.14 — Hero timing.** Nix the Tidecaller (Quick) can bounce in the opponent's turn — but only when a response window is open (their activation, summon, artifact, or attack declaration). A hero cannot act into silence; someone must move first (§15.1).

**21.15 — Mana debt stacking.** Ossian taxes 2 next-turn Mana; a Manacle card adds 2 more. The opponent's next refill is reduced by 4 — but never below 0, and only that once (§6.1).

**21.16 — Stolen monsters and tributes.** You take control of an opponent's monster *until End Phase*. While you control it, it is yours for every purpose — including tribute costs. Tributing a borrowed monster is a clean two-for-one: it never comes back (§20, §17.1).

**21.17 — The empty-zone fizzle.** An effect summons a token or moves a card into a full zone row: no free slot → that part of the effect fizzles with a log message. Costs (as always) stay paid.

**21.18 — Public math.** Hand counts, deck counts, both graveyards, both banished zones and everything face-up are open information. If you want to count the opponent's outs, count them — nothing hidden is knowable, and nothing knowable is hidden (§17.4).

---

*RELIQUARY rulebook, v1 — written for build 0.1.2.*
