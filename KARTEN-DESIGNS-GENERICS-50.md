# Design-Vorschlag v2: 51 neue Generics — „THE SMALL PRINT"

**Status: FREIGEGEBEN und GEBAUT (2026-08-15) — Builder `Assets/_Game/Scripts/Editor/SmallPrint.cs`
(Menü „Rouge TCG/Build The Small Print (51 Generics)"), Engine-Bausteine in
DuelActions/DuelManager, Prompts in `KARTEN-PROMPTS-SMALL-PRINT.md`. Liegt auf dem
Testserver (7778/7901); Prod erst auf Go. Kleine Abweichungen beim Bau (siehe „Umsetzung" unten).**

## Umsetzung — Abweichungen vom Text

- **Heads You Lose:** Ziel wird NACH dem Wurf gewählt (Heads: 1 Monster auf dem Feld,
  Tails: 1 eigenes) — Münzwurf-Ziele werden generell erst nach dem Wurf gewählt.
- **The House Always Wins:** „Flip 3 coins" statt „je Zauber im Friedhof (max 3)" — die
  Beschwörung verlangt ohnehin 3+ Zauber, das ist dieselbe Zahl.
- **Blood for Ink:** kein „Once per turn" (die Engine kennt kein hartes OPT je Kartenname
  für Zauber; 1000 LP je 2 Karten trägt das).
- **First and Last Word:** nur „negate" — ein annullierter Zauber geht ohnehin ins Grab.
- **Lock Shields (Infused):** „cannot be destroyed this turn" (Kampf UND Effekt).
- **Volte-Face:** SS-Bedingung ist die bestehende „a face-down monster is on the field".
- **Load-Bearing Wall:** der Zerstörungs-Effekt ist ein Passiv-Text (feuert automatisch).
- **Sabine** darf im Beschwörungszug angreifen (sonst wäre „All or Nothing" tot).
- **Gift Horse:** nur der Besitzer darf verschenken (kein Ping-Pong).

Änderungen gegenüber v1 (Felix' Feedback):
- **Loaded Dice**: zweimal werfen, Ergebnis aussuchen (bei zwei Tails keine Wahl → Karte zerstört).
- **The Usurer's Terms**: Schuld ist immer aktivierbar; jedes Mana, das nächste
  Runde nicht gedeckt ist, kostet 1500 LP — steht so auf der Karte. Gilt als
  allgemeine Regel für Mana-Schulden.
- **NEU: Skimmed Off the Top** (Quick): Mana, das der Gegner sich verschaffen
  will, geht an dich. Damit 51 Karten — wenn du bei 50 bleiben willst, würde ich
  *Sworn Statement* streichen (teuerster UI-Baustein).
- **Ledger of Small Debts**: Infused kostet jetzt 1 Mana + 1500 LP für 2 Karten
  (statt 2 Mana für 1 Mana).
- **Grale**: kein Massen-Lien mehr — Infused erhöht stattdessen EIN Lien um 1;
  Stats runter auf 1300/1000.
- **Aurel**: LP-Kosten werden 0 statt halbiert; dafür SUMMON härter
  (LP ≤ 4000 · 3+ Spells im Friedhof · 3 Mana).
- **The White Elephant** 1000 LP je Standby, **Gift Horse** 800 LP.
- **Left Hand of the Hangman**: Zerstörung des Gegenübers feuert bei Summon
  UND nach jeder Bewegung — die Bewegung ist damit das wiederholbare Removal.
- **Load-Bearing Wall**: Positionswechsel-Effekte des Gegners dürfen sie
  targeten, verpuffen aber (das Passiv gewinnt).
- **Castellan**: „bewegen" ist nacheinander gemeint (jede Bewegung macht eine
  Zone frei) — Text entsprechend präzisiert.
- **High Stakes**: nur in der gegnerischen Main Phase aktivierbar, wirkt bis
  zum Ende DEINES nächsten Zuges (also beide Battle Phases).
- Deine Antworten: Lien wiederkehrend ✓ · Zonen-Bewegung ✓ · Namen ✓ ·
  Set-Name „The Small Print" ✓.

Leitidee des Sets: **Jeder Handel hat Bedingungen.** Alle 50 Karten sind
Generics (kein Archetype), und jede starke Wirkung trägt ihr Kleingedrucktes —
eine Wette, eine Schuld, ein Tausch, eine Standortfrage, einen Schwur oder eine
Kampfregel. Das erfüllt Felix' Vorgaben so:

1. **Namen**: Idiome, Charakternamen im Haus-Stil („X, Who …"), konkrete
   Gegenstände — nichts vom Schlag „Shadow Blade of the Ancients".
2. **Infused + Passives**: 38 Karten mit Infused, 26 mit Passiv.
3. **Downsides**: jede starke Wirkung hat Preis, Bedingung oder Risiko
   (steht bei jeder Karte als *Kleingedrucktes*).
4. **Special Summons**: 18 der 21 Monster haben eine SS-Bedingung
   (die drei ohne sind Lv1-Utility).
5. **Nichts Bekanntes**: gegen alle 199 bestehenden Generics und das gesamte
   Engine-Vokabular geprüft. Sechs neue Mechaniken tragen das Set:
   **Münzwurf**, **LP als Kosten**, **Pfandrecht (Lien)**, **Kontrolltausch**,
   **Zonen-Spiel (benachbart / gegenüber / bewegen)**, **Once per Duel** —
   dazu Kampfregeln, die es noch nicht gab (Piercing, doppelter Kampfschaden,
   Angriffslimit, Zaubersteuer, Parley).

Format wie in der 6er-Welle: SS: = Special-Summon-Bedingung (aus der Hand,
global once per turn je Name) · ↳ = Coupled-Infused (Upgrade des Effekts
darüber) · sonst Standalone-Infused · Normal-Effekte 0 Mana, Infused kostet,
Spells min. 1 Mana. ⚙ = braucht neuen Engine-Baustein (Liste am Ende).

Verteilung: 21 Monster · 16 Spells · 8 Artifacts · 6 Reliquaries.
Rarities: 6 Legendary · 23 Rare · 22 Uncommon.

---

## I. WAGERS — Münzwurf (7 Karten)

Neue Mechanik ⚙: **Flip a coin.** Ein Effekt wirft eine Münze; die folgenden
Aktionen sind an Heads oder Tails gebunden. Tails ist nie „nichts passiert",
sondern immer die Kehrseite — das ist der eingebaute Preis.

**Heads You Lose** — Spell · Quick · Rare
- (1M): Target 1 face-up monster on the field, then flip a coin. Heads — destroy it. Tails — destroy 1 monster you control instead (if you control none, nothing happens).
- ↳ Infused (3M): Flip two coins; if at least one lands Heads, destroy the target. If both land Tails, destroy 1 monster you control and your opponent draws 1 card.
- *Kleingedrucktes*: 50 % (bzw. 25 %) Eigentor.

**Loaded Dice** — Artifact · Player · Rare
- Passiv ⚙: Once per turn, when you flip a coin, flip it twice instead and choose which result counts.
- Ignition-Infused (2M, once per turn): Flip a coin: Heads — draw 1 card. Tails — discard 1 card.
- *Kleingedrucktes*: If both flips land Tails, there is nothing to choose — and this card is destroyed. (Das Glück ist aufgebraucht.)

**Grinner, Who Plays the Table** — Monster · DARK/Demon · Lv2 · 1600/1200 · Rare
- SS: Wenn der Gegner ein Monster kontrolliert.
- On Summon (0M): Flip a coin: Heads — your opponent discards 1 random card. Tails — you discard 1 random card.
- Ignition-Infused (2M, once per turn): Flip a coin: Heads — this card gains 900 ATK until the end of the turn. Tails — it loses 900 ATK until the end of the turn.

**Pennywhistle, Who Calls It in the Air** — Monster · WIND/Myth · Lv1 · 800/800 · Uncommon
- On Summon (0M): Flip a coin: Heads — add 1 Spell from your Deck to your hand. Tails — send the top 2 cards of your Deck to the Graveyard.
- Ignition-Infused (1M, once per turn): Flip a coin: Heads — gain 2 Mana this turn. Tails — you have 1 less Mana during your next turn ⚙.

**Nell, Who Bets the Rent** — Monster · WIND/Human · Lv1 · 700/1000 · Uncommon
- SS ⚙: Wenn du 2 oder weniger Handkarten hast.
- Ignition-Infused (1M, once per turn): Flip a coin: Heads — return 1 monster your opponent controls with 1800 or less ATK to the hand. Tails — return this card to your hand.
- *Kleingedrucktes*: Der Bounce kann dich selbst treffen — Tempoverlust statt Tempogewinn.

**The House Always Wins** — Reliquary · DARK/Human · Lv3 · 2400/2200 · Legendary
- SUMMON: 3+ Spells in deinem Friedhof · Kosten 2 Mana
- On Summon (0M): Flip a coin for each Spell in your Graveyard (max 3): for each Heads, destroy 1 monster your opponent controls; for each Tails, you take 500 damage.
- Passiv ⚙: While your LP are lower than your opponent's, your coin flips that land Tails count as Heads.
- *Kleingedrucktes*: Cannot attack the turn it is Summoned. Das Passiv gilt nur im Rückstand — wer führt, würfelt ehrlich.

**Sabine, Who Wagers the Crown** — Reliquary · FIRE/Human · Lv3 · 2700/2100 · Legendary
- SUMMON: Deine LP niedriger als die des Gegners · 4+ Spells im Friedhof · Kosten 3 Mana
- On Summon (0M): Flip a coin. Heads — destroy all monsters your opponent controls. Tails — destroy all other monsters you control.
- Ignition-Infused (2M, once per turn): Flip a coin: Heads — this card can attack twice this Battle Phase. Tails — it cannot attack this turn.
- *Kleingedrucktes*: Der Boardwipe ist ein echter Münzwurf — Alles oder Nichts.

## II. DEBTS — LP als Kosten, Mana-Schulden, Pfandrecht (11 Karten)

Neue Mechaniken ⚙: **Pay LP** als Kosten (statt oder zusätzlich zu Mana) ·
**Mana-Schuld** (nächster Zug weniger Mana, Umkehrung von „Sleep On It";
Regel: *was nicht gedeckt ist, kostet 1500 LP je Mana* — steht auf jeder Karte
mit Schuld) · **Lien (Pfandrecht)**: ein Marker mit Betrag auf einem Monster —
*in jeder Standby Phase seines Kontrolleurs zahlt er den Betrag in Mana oder es
wird zerstört.* Sichtbar als Badge wie der Totenkopf, mit Zahl.

**Blood for Ink** — Spell · Normal · Uncommon
- (1M + pay 1000 LP): Draw 2 cards.
- ↳ Infused (1M + pay 2000 LP): Draw 3 cards and gain 1 Mana this turn.
- *Kleingedrucktes*: Once per turn (beide Stufen zusammen). Karten gegen Leben.

**The Usurer's Terms** — Spell · Normal · Rare
- (1M): Gain 4 Mana this turn. During your next turn, you have 3 less Mana. If you cannot cover the full amount, you lose 1500 LP for each Mana you could not pay.
- ↳ Infused (1M): Gain 6 Mana this turn; next turn 5 less — same terms.
- *Kleingedrucktes*: Der Kredit wird nächste Runde fällig — Tempo jetzt, Loch danach; wer nicht zahlen kann, zahlt mit Leben.

**Skimmed Off the Top** — Spell · Quick · Rare *(neu, Felix' Wunsch)*
- (1M) ⚙: Activate when your opponent activates a card or effect that would give them Mana: they gain none of it — you gain that Mana instead. (Mana skimmed during your opponent's turn is added to your next turn.)
- *Kleingedrucktes*: Nur als Antwort auf einen Mana-Effekt aktivierbar; ohne Ziel tot auf der Hand.

**Pound of Flesh** — Spell · Normal · Rare
- (1M + pay 1500 LP): Destroy 1 monster your opponent controls with 1500 or less ATK.
- ↳ Infused (2M + pay 3000 LP): 3000 or less ATK.
- *Kleingedrucktes*: Das Pfund Fleisch — die LP, die du zahlst, sind der ATK, den du nehmen darfst.

**Sign in Blood** — Spell · Normal · Rare
- (2M + pay 2000 LP): Special Summon 1 Level 2 monster from your Deck. Its effects are negated until the End Phase, and it cannot attack this turn.
- *Kleingedrucktes*: Body sofort, Effekt und Angriff erst nächste Runde.

**Ledger of Small Debts** — Artifact · Field · Rare
- Ignition (0M, once per turn): Pay 800 LP: gain 1 Mana this turn.
- Ignition-Infused (1M, once per turn): Pay 1500 LP: draw 2 cards.
- *Kleingedrucktes* ⚙: When your LP are 2000 or less, this card is destroyed. (Der Eintreiber kommt.)

**Grale, Who Collects on Sundays** — Monster · DARK/Human · Lv2 · 1300/1000 · Rare
- SS ⚙: Wenn deine LP niedriger sind als die des Gegners.
- On Summon (0M): Place a Lien of 1 on 1 monster your opponent controls. (During each of its controller's Standby Phases, they pay that much Mana or it is destroyed.)
- Ignition-Infused (2M, once per turn) ⚙: Raise the Lien on 1 monster your opponent controls by 1. (Zinsen: aus 1 Mana je Runde werden 2, dann 3 …)
- *Kleingedrucktes*: Ein Lien pro Beschwörung, danach nur noch Zinsen — und ein 1300er-Body, der sich nicht selbst schützen kann.

**Vetch, Who Never Forgets a Face** — Monster · DARK/Human · Lv1 · 900/900 · Uncommon
- SS ⚙: Wenn ein Monster mit Lien auf dem Feld liegt.
- On Summon (0M): Place a Lien on 1 monster your opponent controls with 1500 or less ATK.
- Hand-Ignition-Infused (1M): Discard this card: place a Lien on 1 monster your opponent controls (any ATK).

**The Bailiff at the Door** — Monster · EARTH/Human · Lv2 · 1500/1900 · Uncommon
- SS ⚙: Wenn ein Monster mit Lien auf dem Feld liegt.
- Passiv ⚙: Monsters with a Lien lose 500 ATK.
- On Summon (0M): Destroy 1 monster with a Lien; draw 1 card.

**Aurel, Who Collects at Midnight** — Reliquary · DARK/Angel · Lv3 · 2600/2200 · Rare
- SUMMON: Deine LP 4000 oder weniger · 3+ Spells in deinem Friedhof · Kosten 3 Mana
- On Summon (0M): Gain 500 LP for each Spell in your Graveyard (max 2500).
- Passiv ⚙: LP costs you pay are reduced to 0.
- *Kleingedrucktes*: Cannot attack the turn it is Summoned. Blood for Ink, Pound of Flesh, Sign in Blood, Blood Oath werden gratis — aber erst, wenn du fast am Boden liegst und schon gezaubert hast.

**Blood Oath** — Monster · DARK/Human · Lv2 · 1700/1500 · Uncommon
- Cannot be Normal Summoned/Set ⚙.
- SS ⚙: Pay 1000 LP.
- On Destroyed (0M): Gain 1000 LP.
- Ignition-Infused (2M, once per turn): Pay 500 LP: this card permanently gains 500 ATK.
- *Kleingedrucktes*: Der einzige Weg aufs Feld kostet Leben; stirbt er, kommt es zurück.

## III. BARTER — Kontrolltausch, Danaergeschenke, Wilderei (8 Karten)

Neue Mechaniken ⚙: **Swap control** (dauerhaft, beide Richtungen) ·
**Give control** · **Fluch-Passiv** („its controller loses LP") · **cannot be
Tributed** · **Poach** (aus dem gegnerischen Friedhof beschwören; verbannt,
wenn es das Feld verlässt) · **beide Hände mischen und neu ziehen**.

**Fair Trade** — Spell · Normal · Rare
- (2M): Choose 1 monster you control and 1 monster your opponent controls: swap control of them permanently. The monster you receive cannot attack this turn.
- ↳ Infused (4M): Also draw 1 card, and the monster you gave away has its effects negated until the End Phase.
- *Kleingedrucktes*: Du gibst wirklich etwas her — mit The White Elephant oder Gift Horse wird daraus ein Danaergeschenk.

**Even Exchange** — Spell · Quick · Uncommon
- (2M): Both players shuffle their hands into their Decks, then each draws as many cards as they shuffled.
- ↳ Infused (3M): You draw 1 more.
- *Kleingedrucktes*: Symmetrisch — die Waffe ist das Timing (wenn der Gegner gerade gesucht hat).

**The White Elephant** — Monster · EARTH/Animal · Lv3 · 3000/2600 · Rare
- Cannot be Normal Summoned/Set ⚙ · cannot be Tributed ⚙.
- SS: Wenn der Gegner 2+ Monster kontrolliert.
- Passiv ⚙: During each of its controller's Standby Phases, its controller loses 1000 LP.
- *Kleingedrucktes*: 3000 ATK ohne Bedingung — aber wer ihn hält, blutet. Verschenken (Fair Trade, Broker) ist der Plan.

**Gift Horse** — Monster · WIND/Animal · Lv2 · 1900/1900 · Uncommon
- SS: Wenn 3+ Karten in deinem Friedhof liegen.
- Passiv ⚙: Cannot attack · cannot be Tributed · during each of its controller's Standby Phases, its controller loses 800 LP.
- Ignition (0M, once per turn) ⚙: Give control of this card to your opponent; draw 2 cards.
- *Kleingedrucktes*: Solange er bei dir steht, kostet er dich; erst als Geschenk wird er stark.

**The Changeling Cradle** — Monster · DARK/Myth · Lv1 · 500/500 · Rare
- SS: Wenn der Gegner ein Monster kontrolliert.
- On Summon (0M): Swap control of this card and 1 Level 1 monster your opponent controls; the monster you take cannot attack this turn.
- *Kleingedrucktes*: Nur Level 1 — ein Kuckucksei, kein Diebstahl.

**Hessel of the Crossroads** — Monster · EARTH/Human · Lv2 · 1600/1600 · Uncommon
- SS: Wenn du ein Artifact kontrollierst.
- On Summon (0M): Your opponent draws 1 card; you draw 2 cards.
- ↳ Infused (2M): You draw 3 instead, but you skip your next Draw Phase ⚙.

**Poacher's Lantern** — Artifact · Field · Rare
- Ignition-Infused (2M, once per turn) ⚙: EITHER player may activate: Special Summon 1 monster with 2000 or less ATK from the OTHER player's Graveyard to your field. It cannot attack this turn and is banished if it leaves the field.
- *Kleingedrucktes*: Die Laterne leuchtet für beide — der Gegner wildert in deinem Friedhof genauso.

**The Broker of Both Sides** — Reliquary · LIGHT/Demon · Lv3 · 2500/2500 · Rare
- SUMMON ⚙: Ein Monster auf dem Feld wird von jemand anderem kontrolliert als seinem Besitzer · Kosten 2 Mana
- On Summon (0M): Swap control of 1 monster you control and 1 monster your opponent controls.
- Passiv: Monsters you control but do not own gain 500 ATK.
- *Kleingedrucktes*: Cannot attack the turn it is Summoned. Braucht einen laufenden Handel.

## IV. GROUND — Nachbarn, Gegenüber, Bewegung (9 Karten)

Neue Mechanik ⚙: **Zonen haben Bedeutung.** *Adjacent* = die beiden
Nachbarzonen auf der eigenen Seite · *facing* = die gegnerische Zone direkt
gegenüber · *move* = ein Monster wechselt in eine leere eigene Zone. Die
Zonenwahl beim Beschwören gibt es schon — jetzt zählt sie.

**Lock Shields** — Spell · Quick · Uncommon
- (1M): Target 1 monster you control: it and the monsters adjacent to it gain 500 DEF until the end of the turn.
- ↳ Infused (2M): 700 DEF, and they cannot be destroyed by battle this turn.

**Stare Down** — Spell · Quick · Uncommon
- (1M): Target 1 monster you control: until the end of the turn it gains ATK equal to half the ATK of the monster facing it.
- ↳ Infused (2M): Equal to its full ATK.
- *Kleingedrucktes*: Ohne Gegenüber passiert nichts.

**Serjeant Halloway** — Monster · EARTH/Human · Lv2 · 1500/1800 · Rare
- SS ⚙: Wenn du 2+ Monster kontrollierst.
- Passiv ⚙: Monsters adjacent to this card gain 400 ATK. This card cannot attack.
- Ignition-Infused (1M, once per turn) ⚙: Move this card to an empty monster zone you control.

**Left Hand of the Hangman** — Monster · DARK/Demon · Lv2 · 1800/1000 · Rare
- SS: Wenn der Gegner 2+ Monster kontrolliert.
- Trigger (0M) ⚙: When this card is Summoned or moves to another zone: destroy the face-up monster facing it if its ATK is lower than this card's.
- Ignition-Infused (2M, once per turn) ⚙: Move this card to an adjacent empty zone; it cannot attack this turn.
- *Kleingedrucktes*: Jede Bewegung ist ein neuer Strang — aber nur gegen offene Monster unter 1800 ATK, nur in eine leere Nachbarzone, und der Angriff fällt aus. Der Gegner sieht kommen, wo du hinwillst.

**Rook's Gambit** — Monster · LIGHT/Mecha · Lv1 · 900/1500 · Uncommon
- Passiv ⚙: The monster facing this card loses 600 ATK. This card cannot attack.
- Ignition (0M, once per turn): Move this card to any empty monster zone you control.
- *Kleingedrucktes*: Ein wandernder Debuff, kein Angreifer.

**Volte-Face** — Monster · WIND/Human · Lv2 · 1700/1700 · Rare
- SS: Wenn du eine verdeckte Karte kontrollierst.
- Trigger ⚙: When this card changes its battle position: draw 1 card.
- Ignition-Infused (1M, once per turn) ⚙: This card may change its battle position one additional time this turn.
- *Kleingedrucktes*: If this card changed position this turn, it cannot attack.

**Load-Bearing Wall** — Monster · EARTH/Mecha · Lv3 · 2400/2600 · Rare
- Cannot be Normal Summoned/Set ⚙.
- SS ⚙: Wenn du 2+ Monster kontrollierst.
- Passiv ⚙: Monsters adjacent to this card cannot be destroyed by card effects. This card cannot change its battle position — effects that would change it can still target it, but do nothing.
- On Destroyed (0M) ⚙: The monsters adjacent to it permanently lose 500 ATK and 500 DEF.
- *Kleingedrucktes*: Fällt die Wand, fällt der Putz mit.

**The Empty Chair** — Artifact · Field · Rare
- Passiv ⚙: Your monsters with no adjacent monster gain 500 ATK. Your monsters with an adjacent monster lose 200 ATK.
- *Kleingedrucktes*: Belohnt Abstand, bestraft Klumpen — die eigene Aufstellung wird zur Entscheidung.

**Castellan of the Long Wall** — Reliquary · EARTH/Human · Lv3 · 2200/3000 · Rare
- SUMMON: 3+ Monster unter deiner Kontrolle · Kosten 2 Mana
- On Summon (0M) ⚙: Move 1 monster you control to an empty monster zone; then you may move a second one. (Nacheinander — die erste Bewegung macht eine Zone frei, also reicht die eine freie Zone neben ihm.)
- Passiv ⚙: Monsters adjacent to this card cannot be destroyed by battle. This card cannot attack.

## V. OATHS — Once per Duel, Schwüre, harte Auflagen (7 Karten)

Neue Mechaniken ⚙: **Once per Duel** (hartes Limit je Spieler und Name — erlaubt
Effekte, die once per turn zu stark wären) · **must be Special Summoned** ·
Selbstauflagen als Preis.

**The Unbroken Oath** — Spell · Quick · Legendary
- Once per Duel. (2M): Negate the effects of all cards your opponent controls until the end of this turn.
- *Kleingedrucktes*: You cannot activate other Spells for the rest of this turn ⚙.

**First and Last Word** — Spell · Quick · Legendary
- Once per Duel. (2M): Negate the activation of the Spell your opponent activated last in this chain and destroy it ⚙.
- *Kleingedrucktes*: Nur als Antwort in einer Kette; einmal pro Duell — das eine Wort, das zählt.

**Sworn Statement** — Spell · Normal · Uncommon
- (1M) ⚙: Declare Monster, Spell or Artifact, then reveal the top card of your Deck: if it is the declared type, add it to your hand; otherwise send it to the Graveyard.
- ↳ Infused (2M): Reveal the top 2 cards instead; matching cards go to your hand, the rest to the Graveyard.
- *Kleingedrucktes*: Falsch geschworen = gemillt. (Mit Cliffhanger/Weather Eye wird daraus Wissen.)

**Sworn to the Gate** — Monster · LIGHT/Human · Lv3 · 2500/2200 · Rare
- Cannot be Normal Summoned/Set ⚙.
- SS ⚙: Wenn du keine Monster kontrollierst.
- Passiv ⚙: While you control no other monsters, this card cannot be destroyed by battle or by card effects.
- *Kleingedrucktes* ⚙: While this card is on the field, you cannot Special Summon other monsters. (Der einsame Torwächter.)

**Vow of Poverty** — Artifact · Player · Rare
- Passiv: During your Standby Phase, gain 2 additional Mana this turn.
- *Kleingedrucktes* ⚙: During your End Phase, if you hold more than 2 cards, destroy this card.

**The Ascetic of the Ninth Stair** — Reliquary · LIGHT/Human · Lv3 · 2800/2800 · Legendary
- SUMMON ⚙: 0 Karten auf deiner Hand · Kosten 3 Mana
- On Summon (0M): Draw 2 cards.
- Passiv ⚙: While you have 1 or fewer cards in hand, this card cannot be targeted and cannot be destroyed by card effects.
- *Kleingedrucktes*: Cannot attack the turn it is Summoned. Wer hortet, verliert den Schutz.

**Marrow, Who Holds Every Card** — Monster · DARK/Demon · Lv2 · 1000/1000 · Rare
- SS ⚙: Wenn du 5+ Handkarten hast.
- Passiv ⚙: Gains 300 ATK for each card in your hand.
- Ignition-Infused (2M, once per turn): Draw 1 card, then your opponent draws 1 card.
- *Kleingedrucktes*: Cannot attack the turn it is Summoned; und jede Karte, die du spielst, macht ihn kleiner. Gegenstück zum Asketen.

## VI. TERMS OF BATTLE — neue Kampfregeln (9 Karten)

Neue Mechaniken ⚙: **Piercing** (Kampfschaden auch gegen Defense) · **direkter
Angriff mit halbem Schaden** · **doppelter Kampfschaden** (Zug-Flag) ·
**Zaubersteuer** · **Battle Phase beenden** · **ein Angriff pro Battle Phase** ·
**immun gegen Effektzerstörung** · **cannot attack directly**.

**Ram's Head** — Artifact · Monster · Uncommon
- +300 ATK. Passiv ⚙: The equipped monster inflicts piercing battle damage.
- *Kleingedrucktes* ⚙: If the equipped monster attacks a Defense Position monster and does not destroy it, destroy this card.

**Chimney Sweep** — Monster · FIRE/Human · Lv1 · 1000/600 · Uncommon
- SS: Wenn du ein Artifact kontrollierst.
- Passiv ⚙: Can attack directly even if your opponent controls monsters; battle damage from its direct attacks is halved.

**High Stakes** — Spell · Quick · Rare
- (2M) ⚙: Only during your opponent's Main Phase: until the end of your next turn, all battle damage to either player is doubled.
- *Kleingedrucktes*: Gilt für beide Battle Phases — erst seine, dann deine. Wer sich verrechnet, verliert doppelt.

**Guild Tariff** — Artifact · Field · Rare
- Passiv ⚙: Spells cost 1 more Mana — for both players.
- *Kleingedrucktes* ⚙: During your End Phase, if you activated a Spell this turn, destroy this card. (Die Zunft duldet keine Ausnahme — auch nicht für dich.)

**Stone That Would Not Break** — Monster · EARTH/Mecha · Lv2 · 0/2500 · Uncommon
- SS ⚙: Wenn du keine Monster kontrollierst (in Defense Position).
- Passiv ⚙: Cannot be destroyed by card effects · cannot be Tributed · cannot change its battle position.
- *Kleingedrucktes*: Eine Mauer und nichts sonst — nie Tribut, nie Angriff.

**Bristleback Aurochs** — Monster · EARTH/Beast · Lv3 · 2600/1800 · Rare
- SS ⚙: Wenn der Gegner ein Monster in Defense Position kontrolliert.
- Passiv ⚙: Piercing. Cannot attack directly.
- Ignition-Infused (2M, once per turn): Switch 1 monster your opponent controls to Defense Position.
- *Kleingedrucktes*: Trampelt durch Mauern, kommt aber nie an den Spieler.

**Trample the Line** — Spell · Normal · Uncommon
- (1M): Target 1 monster you control: it inflicts piercing battle damage this turn and gains 300 ATK until the end of the turn.
- ↳ Infused (3M): All monsters you control inflict piercing battle damage this turn (no ATK bonus).

**Parley** — Spell · Quick · Rare
- (1M) ⚙: Only during a Battle Phase: end the Battle Phase. Your opponent draws 1 card.
- *Kleingedrucktes*: Der Waffenstillstand kostet eine Karte Kartenvorteil.

**The Duelist's Code** — Artifact · Field · Legendary
- Passiv ⚙: Each player may declare only one attack per Battle Phase. Attacking monsters gain 700 ATK during the battle.
- *Kleingedrucktes*: Symmetrisch — Schwarm-Decks hassen es, Ein-Boss-Decks lieben es.

---

## Neue Engine-Bausteine (⚙) — Übersicht

Alles im bestehenden Action/Passive/Trigger-Muster, nur angehängt (Enums am
Ende). Grobe Größe: vergleichbar mit dem 115er-Set-Batch (~15 Bausteine).

| # | Baustein | Karten |
|---|----------|--------|
| 1 | **Münzwurf**: `FlipCoin`-Aktion + Aktions-Gate (nur bei Heads / nur bei Tails) · Passiv „zweimal werfen, Ergebnis wählen" (Loaded Dice, Wahl-Prompt) · Passiv „Tails zählt als Heads im Rückstand" (House) | 7 |
| 2 | **LP als Kosten**: `PayLifePoints` (isCost) · SS-Kosten in LP (Blood Oath) · Passiv LP-Kosten = 0 (Aurel) · Passiv „zerstört bei LP ≤ X" (Ledger) | 6 |
| 3 | **Mana-Schuld**: `DrainSelfManaNextTurn`; ungedeckter Rest kostet 1500 LP je Mana (Usurer, Pennywhistle) · **Mana-Umleitung**: `RedirectManaFromChainLink` — nur legal als Antwort auf ein gegnerisches Kettenglied mit Mana-Gewinn; in dessen Zug wird es zu Next-Turn-Mana (Skimmed Off the Top) | 3 |
| 4 | **Lien**: Marker MIT BETRAG auf Monster, Standby-Toll „N Mana oder zerstört" (Yes/No wie Emergency Barrier) · `PlaceLienOnTarget` · `RaiseLienOnTarget` · SS-Bedingung „Lien auf dem Feld" · Passiv „Lien-Monster −500 ATK" · Badge wie Totenkopf mit Zahl | 3 |
| 5 | **Kontrolle**: `SwapControlWithTarget` · `GiveSelfToOpponent` · Passiv „Kontrolleur verliert X LP je Standby" · `cannot be Tributed` · Reliquary-Req „fremdkontrolliertes Monster auf dem Feld" | 5 |
| 6 | **Poach**: SS aus dem GEGNER-Friedhof + Instanz-Flag „verbannt, wenn es das Feld verlässt" | 1 |
| 7 | **Zonen**: Nachbar-/Gegenüber-Helfer · Aura-Filter „nur Nachbarn" / „nur Alleinstehende" · Ziel „adjacent allies" / „facing monster" · `MoveSelfToZone` / `MoveTargetToZone` (nacheinander) · Trigger `OnMovedSelf` (Hangman) · Passiv „Nachbarn effekt-/kampfimmun" | 9 |
| 8 | **Position**: Trigger `OnPositionChangedSelf` · `ExtraPositionChangeThisTurn` · Passiv „cannot change position" (Effekte dürfen targeten, verpuffen) | 3 |
| 9 | **Once per Duel** (Effekt-Flag, je Spieler+Name) | 2 |
| 10 | **Beschwörungs-Auflagen**: `cannot be Normal Summoned/Set` · Passiv „du kannst keine anderen Monster spezialbeschwören" · neue SS-Bedingungen: keine eigenen Monster / 2+ eigene Monster / LP niedriger / Handkarten ≤ 2 bzw. ≥ 5 / Gegner-Monster in Defense · Reliquary-Req „leere Hand" | 10 |
| 11 | **Handkarten-Zähler** (`OwnHandCards` für Passiv-ATK) · Passiv-Immunität „solange Hand ≤ 1" (Ascetic) | 2 |
| 12 | **Kampf**: Passiv Piercing + `GrantPiercingThisTurn` · Passiv Direktangriff mit halbem Schaden · Flag „Kampfschaden ×2 bis Ende deines nächsten Zuges" + Aktivierungs-Gate „nur gegnerische Main Phase" (High Stakes) · Passiv „cannot attack directly" · Passiv „immun gegen Effektzerstörung" · Ram's-Head-Bruch (Trigger „hat Defense-Monster nicht zerstört") | 6 |
| 13 | **Battle-Phase-Regeln**: `EndBattlePhaseNow` (Parley) · Angriffslimit 1 je Battle Phase + Angreifer-Bonus (Duelist's Code) | 2 |
| 14 | **Zauberregeln**: Zaubersteuer +1 (beide) + End-Phase-Selbstzerstörung „wenn du gezaubert hast" · „keine weiteren Spells diesen Zug" (Unbroken Oath) · `NegatePreviousChainLink` + destroy (First and Last Word) | 3 |
| 15 | **Kleinkram**: `SkipOwnNextDrawPhase` (Hessel) · `ShuffleBothHandsRedraw` (Even Exchange) · End-Phase-Handlimit-Zerstörung (Vow of Poverty) · Typ-Deklaration + Top-Reveal (Sworn Statement) | 4 |

Wenn dir das zu viel Engine ist: am günstigsten zu streichen wären Baustein 15
(4 Karten hätten dann schlichtere Effekte), Loaded Dice/House-Passiv (Münzwurf
ohne Manipulation) und die Zonen-Bewegung (Halloway/Hangman/Rook/Castellan
bleiben mit Nachbar-/Gegenüber-Passiven trotzdem sinnvoll).

## Geklärt (v1-Fragen)

Lien wiederkehrend ✓ · Zonen-Bewegung ✓ · Namen ✓ · Set-Name „The Small Print" ✓.

Offen nur noch: **51 oder 50?** Skimmed Off the Top ist dazugekommen; wenn du
bei 50 bleiben willst, streiche ich Sworn Statement (einziger Kandidat mit
eigenem Deklarations-Prompt).

Nach Freigabe: Engine-Bausteine → Builder-Stage (partial, idempotent wie
WaveSix) → Bot-Proben + Selftest → Test-Deploy → Artwork-Prompts nach
Artstyle-Guide.
