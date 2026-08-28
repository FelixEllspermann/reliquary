# 25 weitere Generics — Design-Vorschlag (Welle 3, Paket 2)

Stand: 2026-08-28 · Status: **GEBAUT** (Welle 3, releaseVersion 0.1.8; kleine Bau-Abweichungen im Commit dokumentiert) · Fortsetzung von
[KARTEN-DESIGNS-25-GENERICS.md] (#1–25), Nummern **26–50**.

Gleiche Leitplanken, plus die Lehren aus deinem Paket-1-Review: Level nur 1–3 auf der
echten Katalog-Skala (Lv1 ≤1400 ATK/≤1900 DEF, Lv2 ≤2200/2600, Lv3 ≤3200/3400), alle
8 Monster mit SS-Bedingung, jede Karte mehrere Effekte, Infused beider Sorten —
**[Infused – N]** = Standalone, **[Or Infused +N]** = Coupled-Wahl. Kein direkter
LP-Schaden. **Paket 2 ist komplett draw-frei** — die Payoffs sind diesmal Positionen,
Exil, Token, Deck-Ordnung, Masken und Tempo.

Neue Territorien gegenüber Paket 1: Positions-Tänze, Banish als Ressource, Token,
Deck-Stapeln (beider Seiten!), Mill, Kopien/Masken, Battle-Tricks, Zugstruktur.

---

## A · Positions-Tänzer

### 26. Pirouette Duelist — Monster · Lv 2 · 1200/1200 · 2 Mana
- **SS:** Aus der Hand, wenn der Gegner ein **Monster in Verteidigung** kontrolliert. *(selfSummonRequiresOpponentDefenseMonster ✓)*
- **E1 (once/turn):** Change the battle position of **any monster on the field** — auch gegnerische. *(ChangePosition auf Gegner NEU-klein)* — reißt DEF-Wände auf oder legt Angreifer schlafen.
- **[Or Infused +1] auf E1:** …and it **cannot change position** until your next turn. *(Position-Lock ✓)*

### 27. Turnabout Waltz — Spell · 2 Mana
- **E1:** **Every face-up monster on the field changes battle position.** — der ganze Ballsaal dreht sich: Wände werden Ziele, Stürmer werden Mauern.
- **[Or Infused +2] auf E1:** Nur **eine Seite deiner Wahl** wechselt.

### 28. Stage Fright — Quick-Spell · 2 Mana *(Trapline, SummonResponse ✓)*
- **E1:** When your opponent summons a monster: it arrives in **Defense Position** and cannot change position this turn. *(Positions-Zwang NEU-klein + Lock ✓)* — Lampenfieber beim großen Auftritt.
- **[Infused – 2]:** Flip one of your face-down monsters face-up; it gains **+400 ATK** until end of turn. *(FlipFaceUp ✓)*

## B · Banish-Ökonomie (Exil als Ressource)

### 29. Exile Broker — Monster · Lv 2 · 1500/1000 · 3 Mana
- **SS:** Aus der Hand, wenn **mindestens 1 deiner Karten verbannt** ist. *(Banish-Zähl-req NEU-klein)*
- **E1 (On Summon):** **Banish the top card of your deck** face-down. — er zahlt sich selbst in die Schattenwirtschaft ein.
- **E2 (passiv):** Gains **+200 ATK for each of your banished cards**. *(CountKind Banished NEU-klein)*
- **[Infused – 2]:** Return 1 of your banished cards **to your graveyard**. *(NEU-klein)* — und füttert damit die Grab-Spitzen-Familie aus Paket 1.

### 30. Letters from Exile — Spell · 1 Mana
- **E1:** Banish up to 2 cards from your graveyard: for each, one of your monsters gains **+300 ATK** until end of turn.
- **[Or Infused +1] auf E1:** …and the banished cards **return to your graveyard** during your next Standby Phase. *(NEU-klein)* — Exil auf Zeit: der Buff jetzt, das Grab bleibt langfristig intakt.

### 31. The Unforgotten — Monster · Lv 3 · 2100/1400 · 4 Mana
- **SS:** **Aus der Verbannung!** Ist diese Karte verbannt und hast du 3+ verbannte Karten: Special Summon sie aufs Feld. *(SS-Quelle Banish NEU-mittel)* — die Karte, die aus dem Exil heimkehrt.
- **E1 (passiv):** **Cannot be banished.** *(NEU-klein)* — einmal heimgekehrt, nie wieder fort.
- **[Infused – 2]:** **Banish the top card of your opponent's graveyard.** — Anti-Grab-Tech und eigener Exil-Zähler in einem.

## C · Token & Illusionen

**Scarecrow-Token sind ein EIGENER Token-Typ** (eigenes Artwork, eigener Name):
sie zählen NICHT als Illusion-Token, triggern keine Illusion-Synergien und sind
für nichts verwendbar, was Illusion-Token ermöglichen. *(Token-Typ NEU-klein —
Spawn-Logik wie SpawnIllusionTokens, aber getrennter Typ/Sprite)*

### 32. Straw Army — Spell · 2 Mana
- **E1:** Special Summon **2 Scarecrow-Token** (Lv 1 · 0/500) in Defense.
- **[Or Infused +2] auf E1:** **3 Token**, und bis zu deinem nächsten Zug **müssen Gegner-Monster Token angreifen**, wenn möglich. *(MustBeAttacked ✓ auf Token)* — die Vogelscheuchen halten die Linie.

### 33. Puppet Parade — Artefakt · 2 Mana
- **E1 (once/turn, passiv):** Wird ein eigener **Token zerstört**: Special Summon **1 Scarecrow-Token** in Defense. — die Parade endet nie.
- **[Infused – 2]:** Ein eigener Token wird **bis Zugende zur Kopie eines Monsters auf dem Feld**. *(Temporary-Copy ✓)* — die Puppe trägt heute Abend ein echtes Gesicht.

### 34. Man of Straw — Monster · Lv 1 · 800/600 · 1 Mana
- **SS:** Aus der Hand, wenn du **keine Monster** kontrollierst. *(selfSummonRequiresNoOwnMonsters ✓)*
- **E1 (passiv):** Wird er durch Kampf zerstört: Special Summon **1 Scarecrow-Token** in Defense.
- **[Infused – 1]:** Until end of turn, this card **counts as two Tributes**. *(Tribut-Zählung NEU-klein)* — der Strohmann, der für zwei geopfert wird.

## D · Deck-Stapler (Ordnung ist Macht — auf beiden Stapeln)

### 35. Cartomancer's Eye — Artefakt · 2 Mana
- **E1 (once/turn):** Look at the top card of **your deck**; you may put it on the bottom. *(Scry — ReorderTopOfDeck-Verwandt ✓)*
- **[Or Infused +1] auf E1:** *Oder stattdessen:* look at the top card of your **opponent's deck**; you may put it on the bottom. — du mischst SEIN Schicksal; Gift für Card Sharp und alle Wager.

### 36. Stacked Deck — Spell · 1 Mana
- **E1:** Look at the top 3 cards of your deck, put them back **in any order**. *(ReorderTopOfDeck ✓)*
- **[Or Infused +2] auf E1:** …and you may send **one of them to the graveyard**. — legt gezielt die Grab-Spitze (Paket-1-Familie!).

### 37. House Dealer — Monster · Lv 2 · 1400/1100 · 2 Mana
- **SS:** Aus der Hand, wenn diese Runde bereits **eine Karte aufgedeckt** wurde. *(selfSummonRequiresRevealedThisTurn ✓)*
- **E1 (On Summon, Wager):** Reveal the top card of your deck. **Ist sie ein Monster:** House Dealer gains **+600 ATK permanently**. *(Reveal ✓ + Typ-Check wie Card Sharp #17)* — das Haus gewinnt immer… wenn du vorher gestapelt hast (Stacked Deck/Cartomancer's Eye machen die Wette zur Gewissheit).
- **[Infused – 2]:** Both players put their top deck card **on the bottom**. — der Croupier mischt neu: Anti-Stacking gegen alles aus dieser Familie.

## E · Mill

### 38. Quarry Collapse — Spell · 2 Mana
- **E1:** **Mill 3** vom eigenen Deck. *(Milled-Tracking ✓)* — Selbstmill als Motor für Grab-Spitze, Séance, Open Casket.
- **[Or Infused +2] auf E1:** …then put **1 milled monster on top of your graveyard**. *(Reorder ✓)*

### 39. Baron of the Undertow — Monster · Lv 3 · 2300/1200 · 4 Mana
- **SS:** Aus der Hand, wenn dein **Grab 8+ Karten** hält. *(Grave-Count-req NEU-klein)*
- **E1 (On Summon):** Your **opponent mills 2**.
- **[Or Infused +2] auf E1:** Your opponent mills **4** instead.
- **E2 (passiv):** Cards your opponent mills are **banished instead**. *(NEU-klein)* — der Sog gibt nichts zurück: Anti-Grab-Tech, die gegnerische Friedhofs-Decks trockenlegt.

### 40. Mudlark Scavenger — Monster · Lv 2 · 1600/900 · 3 Mana
- **SS:** Aus der Hand, wenn diese Runde **Karten gemillt** wurden. *(selfSummonRequiresMilled ✓)*
- **E1 (On Summon):** **Mill 2** vom eigenen Deck.
- **E2 (passiv):** While dein Grab **8+ Karten** hält: **+500 ATK**. *(Grave-Count ✓)*
- **[Infused – 2]:** Put **1 monster from your graveyard on top** of it. *(Reorder ✓)* — der Schlammsucher sortiert den Spülsaum.

## F · Kopien & Masken

### 41. Mirror Usher — Monster · Lv 3 · 1900/1900 · 4 Mana
- **SS:** Aus der Hand, wenn der Gegner **2+ Monster** kontrolliert. *(selfSummonRequiresOpponentMonsters ✓)*
- **E1 (On Summon):** Becomes a **copy of an opponent's monster** until end of turn (keeps its name). *(Temporary-Copy ✓)*
- **[Or Infused +2] auf E1:** Die Kopie hält **bis zum Ende deines nächsten Zuges**. — der Platzanweiser trägt die Maske eine ganze Vorstellung lang.

### 42. Borrowed Face — Quick-Spell · 2 Mana *(AttackResponse ✓)*
- **E1:** Dein angegriffenes Monster wird **bis Zugende zur Kopie des Angreifers**. *(Temporary-Copy ✓)* — Spiegelduell: gleiche Werte, beide fallen.
- **[Or Infused +1] auf E1:** …und erhält **+100 ATK** dazu. — es gewinnt das Spiegelduell. Knapp.

### 43. Prompter's Box — Artefakt · 3 Mana
- **E1 (once/turn):** Setze ein eigenes offenes Monster **face-down**. *(SetFaceDown ✓)* — zurück hinter den Vorhang: Flip-Effekte und „once per turn" laden neu.
- **[Infused – 2]:** Flip ein eigenes face-down-Monster offen: sein **On-Summon-Effekt feuert erneut**. *(Flip-Replay NEU-mittel — Balance-Fragezeichen, gern streichen/abschwächen)*

## G · Battle-Tricks

### 44. Lowball Feint — Quick-Spell · 1 Mana *(AttackResponse ✓)*
- **E1:** Das angreifende Monster verliert **-800 ATK** bis Zugende. *(EOT-Debuff ✓)*
- **[Or Infused +1] auf E1:** Überlebt es den Kampf, wechselt es am Zugende in **Verteidigung**. — erst tief pokern, dann liegt er flach.

### 45. Shield Wall Doctrine — Artefakt · 2 Mana
- **E1 (passiv):** Deine Monster in Verteidigung erhalten **+300 DEF**, während sie angegriffen werden.
- **[Infused – 2]:** Wechsle **alle deine Monster** in Verteidigung; sie erhalten **+300 DEF bis zu deinem nächsten Zug**. — Igel-Formation auf Kommando.

### 46. Overextension — Spell · 2 Mana
- **E1:** Ein Gegner-Monster, das **diese Runde angegriffen hat** *(DeclaredAttackThisTurn ✓)*: wechselt in Verteidigung und ist **position-locked** bis zum Ende seines nächsten Zuges. *(Lock ✓)* — wer sich verausgabt, kommt nicht wieder hoch.
- **[Or Infused +2] auf E1:** **Alle** Gegner-Monster, die diese Runde angegriffen haben.

## H · Tempo & Zugstruktur

### 47. Prepaid Ritual — Spell · 0 Mana
- **E1:** Dein **nächster Spell** diese Runde kostet **2 weniger**. *(Discount = Gegenstück zu NextSpellSurcharge ✓)*
- **[Or Infused +1] auf E1:** *Oder stattdessen:* deine **nächste Beschwörung** diese Runde kostet **2 weniger**. — die Anzahlung, die sich aussuchen darf, wofür.

### 48. Closing Time — Artefakt · 3 Mana
- **E1 (passiv, bindet BEIDE):** Jeder Spieler kann nur **1 Monster pro Zug beschwören**. *(Summons-pro-Zug-Cap NEU-klein; Dekret-Gefühl ohne Bylaw)* — Sperrstunde für den ganzen Saal.
- **[Infused – 2]:** **Dein Cap ist diese Runde aufgehoben — die Karte bleibt liegen.** — du kennst den Hinterausgang, und die Tür fällt hinter dir wieder zu: der Gegner bleibt gedeckelt. (Wiederholbar; das gratis Artefakt-Opfern hebt den Cap dagegen für BEIDE auf.)

## I · Reliquaries

### 49. Reliquary: The Hall of Echoes — Reliquary · 2000/1800 · req: **3+ eigene verbannte Karten** *(Banish-req NEU-klein)*
- **E1 (passiv):** While du 3+ verbannte Karten hast: deine Monster **+400 ATK**.
- **E2 (once/turn):** **Banish 1 Karte aus deinem Grab.** — die Halle füllt sich selbst.
- **[Infused – 3]:** Return 1 deiner verbannten Karten **in dein Grab**. — das Echo kehrt zurück (und legt die Grab-Spitze).

### 50. Reliquary: The Last Bow — Reliquary · 2200/1500 · req: **2+ eigene face-down-Karten** *(Facedown-req NEU-klein)*
- **E1 (once/turn):** Setze ein eigenes offenes Monster **face-down**. *(wie #43, hier gratis)*
- **E2 (passiv):** Deine face-down-Monster können **nicht von Effekten zerstört** werden. *(Effekt-Schutz ✓ + Facedown-Filter NEU-klein)*
- **[Infused – 2]:** **Flip alle deine face-down-Monster offen.** — der Schlussapplaus: alles zeigt auf einmal sein Gesicht.

---

## Bilanz Paket 2

| | |
|---|---|
| Monster / Spells / Artefakte / Reliquaries | 8 / 10 / 5 / 2 |
| Monster mit SS-Bedingung | **8 von 8** (davon neu: Banish-Count, SS aus der Verbannung, Grab-Count) |
| Karten mit 2+ Effekten | **25 von 25** |
| Standalone-Infused | #28, 29, 31, 33, 34, 37, 40, 43, 45, 48, 49, 50 (12×) |
| Coupled „Or Infused" | #26, 27, 30, 32, 35, 36, 38, 39, 41, 42, 44, 46, 47 (13×) |
| Direkter LP-Schaden | **0** |
| Draw-Effekte | **0** — Payoffs sind Positionen, Exil, Token, Deck-Ordnung, Masken, Tempo |
| Wirken im Gegnerzug | #28, 42, 44 (Traplines/Quick) + #26/27/46 drehen gegnerische Positionen, #33/48 passiv |
| NEU-mittel (größere Bausteine) | #31 SS aus der Verbannung · #43 Flip-Replay (Balance-Fragezeichen) |

**Set-übergreifende Synergien (26–50 → 1–25):** Exil-Familie (#29/30/31/49) füttert die
Grab-Spitze zurück; Deck-Stapler (#35/36/37) armieren Card Sharp (#17) und alle Wager;
Mill (#38/40) betankt Open Casket (#24) und Séance (#12); Masken (#41/42/43) und
Facedown-Werkzeuge harmonieren mit Trapdoor Stage (#6) und Masquerade Ball (#14);
Baron of the Undertow (#39) ist die Anti-Karte gegen genau diese Grab-Strategien —
das Meta streitet sich selbst.
