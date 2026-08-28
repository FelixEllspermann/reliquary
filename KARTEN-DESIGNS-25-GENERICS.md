# 25 Generics — Design-Vorschlag (Welle 3, „Road to 1000"-Finale)

Stand: 2026-08-28 · Status: **GEBAUT** (Welle 3, releaseVersion 0.1.8; kleine Bau-Abweichungen im Commit dokumentiert) · Überarbeitung 3
nach Felix' Review: Levels auf die echte 1–3-Skala (Katalog: Lv1 ≤1400 ATK, Lv2 ≤2200,
Lv3 ≤3200), Lurker-Infused ersetzt (kollidierte mit dem eigenen Taunt), Draw-1-Payoffs
durch interessantere Effekte ersetzt, Peephole-Infused generft, Insurance-Kern bleibt
die Ersatz-Zerstörung. Struktur: **[Infused – N]** = Standalone mit eigenen Mana-Kosten,
**[Or Infused +N]** = Coupled-Upgrade (bei Aktivierung Wahl Normal/stärker).

Leitplanken: kein direkter LP-Schaden; Effekte, die das Spielgeschehen drehen;
Interaktion im Gegnerzug. Stats/Kosten sind Startwerte fürs Inspector-Balancing.
Engine-Aufwand: ✓ = Baustein existiert, NEU-klein / NEU-mittel = neuer Baustein.

**Alle 8 Monster haben eine SS-Bedingung** (durchweg vorhandene selfSummon-Felder).

---

## A · Der Gegner entscheidet (Deals als Generic-Gewürz)

### 1. Crossroads Peddler — Monster · Lv 2 · 1400/1000 · 3 Mana
- **SS:** Aus der Hand, wenn der Gegner **mehr Monster** kontrolliert als du. *(selfSummonRequiresOpponentMoreMonsters ✓)*
- **E1 (On Summon, Deal):** Your opponent chooses — you **draw 1**, *or* this gains **+700 ATK and Piercing** until the end of the turn.
- **[Or Infused +2] auf E1:** Der Deal wird härter: you **draw 2**, *or* **+700 ATK, Piercing and one extra attack** this turn. *(BonusAttacks ✓)*
- **E2 (passiv):** While you have made a deal this turn, this gains **+300 ATK**. *(DealsThisTurn ✓)*

### 2. The Long Detour — Artefakt · 2 Mana
- **E1 (passiv):** When an opponent's monster declares its **first attack each turn**, they choose: send their top deck card to the graveyard and attack on — *or* the attack is **cancelled**. *(OfferDeal ✓ + AttackToll-Trigger-Muster ✓ + CancelAttackTarget ✓, Verdrahtung NEU-klein)*
- **[Infused – 2]:** **Return a monster that attacked this turn to its owner's hand.** *(Bounce ✓ + DeclaredAttackThisTurn-Tracking ✓)* — der lange Umweg schickt den Angreifer ganz nach Hause.

### 3. Final Offer — Spell · 5 Mana
- **E1 (Deal):** Target a monster your opponent controls. They choose: they **send it to the graveyard** — *or* **you take control of it** until the end of your next turn. *(OfferDeal ✓ + Kontroll-Wechsel ✓)*
- **[Or Infused +2] auf E1:** Beide Äste werden härter: (A) wird zu **banish**, (B) hält **einen Zug länger**. *(Banish ✓; Steal-Dauer NEU-klein)* — das letzte Angebot kennt keine Gnade.

## B · Gegnerzug & Ambush

### 4. Cellar Lurker — Monster · Lv 2 · 0/2200 · 3 Mana
- **SS (Ambush):** When an opponent's monster declares an attack: Special Summon this **from your hand** in Defense. *(SpecialSummonSelfFromHand + quickWindow AttackResponse ✓)*
- **E1 (passiv):** While this is in Defense Position, your opponent's monsters **must attack this card** if able. *(MustBeAttacked ✓)*
- **[Infused – 2]:** Until your next turn: monsters that battle this card are flipped **face-down** after damage calculation. *(Facedown-Flip NEU-klein, wie #6/#14)* — der Keller lockt sie an UND schluckt sie ins Dunkel: Taunt zieht die Angriffe, der Flip macht die Angreifer stumm.

### 5. Widow's Ledger — Artefakt · 2 Mana
- **E1 (passiv):** Whenever one of your monsters is destroyed **during your opponent's turn**: the monster that destroyed it **loses 400 ATK permanently**. *(Permanent-Debuff ✓, Zugseiten-Bedingung NEU-klein)* — die Witwe vergisst nicht: jeder Mord steht im Buch, und die Mörder verfallen.
- **[Infused – 3]:** Send this artifact to the graveyard: **Special Summon 1 monster from your graveyard that was destroyed this turn**, in Defense. *(Revive ✓ + Filter NEU-klein)*

### 6. Trapdoor Stage — Artefakt · 3 Mana
- **E1 (once/turn):** When your opponent **Special Summons** a monster: flip it **face-down**. *(SummonResponse ✓ + Facedown-Flip NEU-klein)*
- **[Or Infused +2] auf E1:** …and it **cannot be flipped face-up** until your next turn. *(Position-Lock ✓)*

### 7. Second Guess — Quick-Spell · 2 Mana
- **E1:** When an opponent's monster declares an attack: **change the attack target** to another of your monsters. *(Umlenkung NEU-mittel)*
- **[Or Infused +2] auf E1:** The new target gains **+500 DEF** until end of turn.

## C · Countdown für alle

### 8. Doomsday Bell — Artefakt · 4 Mana · **Countdown 3**
- **E1 (Countdown 0):** **Send every monster on the field to the graveyard.** *(ArmCountdown auf Artefakt ✓ + CountdownZero ✓ + Massen-Send ✓)*
- **[Infused – 1]:** **Tick** this card's countdown by 1. *(TickCountdownTarget ✓)* — du kannst die Glocke selbst beschleunigen.

### 9. Sand in the Gears — Spell · 1 Mana
- **E1:** **Add 2** to the countdown of any card on the field. *(Marker addieren NEU-klein)*
- **[Or Infused +1] auf E1:** *Oder stattdessen:* **tick** a countdown by 1. *(TickCountdownTarget ✓)* — eine Karte, beide Richtungen der Uhr.

### 10. Borrowed Hourglass — Monster · Lv 2 · 800/800 · 2 Mana · **Countdown 2**
- **SS:** Aus der Hand, wenn du bereits **eine Countdown-Karte kontrollierst**. *(selfSummonRequiresOwnCountdown ✓)*
- **E1 (Countdown 0):** Draw 2 and this gains **+800 ATK** permanently.
- **[Infused – 2]:** **Reset** this card's countdown to 2. *(Re-Arm NEU-klein)* — der Motor läuft nochmal.

## D · Grab-Spiele (Synergie mit der Grab-Spitzen-Familie aus Welle 1)

### 11. Gravedigger's Dispute — Spell · 2 Mana
- **E1:** **Swap the top cards** of both graveyards. *(NEU-klein)*
- **[Or Infused +2] auf E1:** …then you may **Special Summon the new top monster of your graveyard** in Defense; banish it when it leaves the field. *(Revive ✓ + Banish-Klausel wie #12)* — nach dem Tausch liegt SEINE Karte auf deinem Grab: du gräbst den Streitgegenstand gleich selbst aus.

### 12. Séance Circle — Spell · 4 Mana
- **E1:** Special Summon a monster **from your opponent's graveyard** to your side. When it leaves the field, banish it. *(NEU-mittel — Owner-Logik = Zustellung invers)*
- **[Or Infused +2] auf E1:** This turn it may **attack twice**. *(BonusAttacks ✓)* — der Geist will Rache.

### 13. Unfinished Business — Monster · Lv 3 · 2000/1500 · 4 Mana
- **SS:** Aus der Hand, wenn **oben auf deinem Grab ein Monster** liegt. *(selfSummonRequiresGraveTopMonster ✓)*
- **E1 (Standby, im Grab):** Liegt diese Karte im Grab, aber **nicht obenauf**: lege sie nach oben. *(Grab-Reihenfolge NEU-klein)*
- **E2 (While GraveTop):** Solange diese Karte oben auf deinem Grab liegt: **deine Monster +200 ATK**. *(onlyWhileGraveTop ✓)*
- **[Infused – 2, aus dem Grab]:** Liegt sie obenauf: **Special Summon sie in Defense**; banish beim Verlassen des Felds. *(GraveTop-Actions ✓ + Revive ✓)*

## E · Feld-Chaos

### 14. Masquerade Ball — Spell · 5 Mana
- **E1:** Flip **every face-up monster on the field** face-down. *(Facedown-Flip wie #6, Massen-Variante)*
- **[Or Infused +2] auf E1:** …**except one monster you control.** — der Gastgeber trägt keine Maske.

### 15. Eminent Domain — Spell · 1 Mana
- **E1:** **Seal one of your own empty monster zones**: draw 2. *(SealZones ✓, Eigenseite NEU-klein)*
- **[Or Infused +2] auf E1:** *Oder stattdessen:* seal one of your **opponent's** empty zones; draw 1. *(SealZones Gegnerseite ✓)*

### 16. The Unwelcome Guest — Monster · Lv 1 · 100/1800 · 2 Mana
- **SS (Zustellung):** You may Special Summon this **to your opponent's side of the field**. *(selfSummonToOpponentField ✓)*
- **E1 (passiv):** Cannot attack. While it squats on your opponent's field, their **spells cost 1 more**. *(passiveCannotAttackWhileDisloyal ✓ + passiveSpellTaxOnController ✓)*
- **E2 (passiv):** Its controller **cannot Tribute** this card. *(NEU-klein)* — rauswerfen muss man ihn schon selbst verprügeln.
- **[Infused – 2, nur solange er beim Gegner steht]:** **Gain 2 Mana this turn.** *(onlyWhileControlledByOpponent ✓ + ManaCredit ✓)* — Miete kassieren, in Münzen.

## F · Wetten & Information

### 17. Card Sharp — Monster · Lv 2 · 1300/900 · 3 Mana
- **SS:** Aus der Hand, wenn **diese Runde bereits eine Karte aufgedeckt** wurde. *(selfSummonRequiresRevealedThisTurn ✓)*
- **E1 (On Summon):** **Name Monster, Spell or Artifact**, then reveal your top deck card — if it matches, add it to your hand. *(OptionRequest ✓ + Reveal ✓ + Abgleich NEU-klein)*
- **[Or Infused +2] auf E1:** On a correct call this also gains **+400 ATK permanently**. *(PermanentAtkBonus ✓)* — der Hai wächst mit jedem Coup.

### 18. Peephole — Spell · 1 Mana
- **E1:** Look at your opponent's hand. You may **shuffle 1 spell from it into their deck**. *(RevealHand ✓ + Rückmischen NEU-klein)*
- **[Or Infused +1] auf E1:** You may shuffle **1 card of any type** instead. — gleiche Menge, freie Zielwahl; der Blick durchs Guckloch bleibt bezahlbar.

### 19. Insurance Policy — Artefakt · 2 Mana
- **E1 (passiv):** When one of your monsters would be **destroyed (by battle or effect)**: you may send this artifact to the graveyard instead. *(Ersatz-Zerstörung NEU-mittel — die Police deckt beide Schadensfälle)*
- **[Infused – 2]:** Choose a monster you control: the **next time** it would be destroyed this duel, it isn't. *(Einmal-Schild NEU-klein, Variante des CannotBeDestroyed-Flags)* — die persönliche Police fürs Lieblingsmonster; kein Duplikat des Passivs, sondern Schutz, der die Karte überdauert.

## G · Ressourcen & Tempo

### 20. Silver-Tongued Creditor — Monster · Lv 3 · 2200/800 · 3 Mana
- **SS:** Aus der Hand, wenn du **höchstens 2 Handkarten** hast. *(selfSummonRequiresHandAtMost ✓)* — der Kredithai riecht klamme Kunden.
- **E1 (On Summon):** Gain **2 Mana** this turn.
- **[Or Infused +2] auf E1:** Gain **3 Mana** instead — and the debt below becomes 3.
- **E2 (deine nächste Standby):** **Pay 2 Mana** or send this card to the graveyard. *(ManaCredit ✓ + Fälligkeit NEU-klein)*

### 21. Tomorrow's Bread — Spell · 1 Mana
- **E1:** Draw 2. **Skip your next normal draw.** *(Draw-Ersatz ✓)*
- **[Or Infused +2] auf E1:** Draw **3** instead; skip your next **two** normal draws. — der Zins steigt mit.

### 22. Scrap Broker — Artefakt · 2 Mana
- **E1 (once/turn):** Send **another artifact you control** to the graveyard: draw 1. *(Send + Grave-Trigger ✓)*
- **[Infused – 2]:** **Add 1 artifact from your graveyard to your hand.** *(Grab-Recursion ✓)* — Kreislaufwirtschaft.

### 23. Posthumous Prodigy — Monster · Lv 1 · ?/? · 2 Mana
- **SS:** Aus der Hand, wenn **oben auf deinem Grab ein Monster** liegt. *(selfSummonRequiresGraveTopMonster ✓)*
- **E1 (passiv):** ATK/DEF sind immer gleich den **gedruckten Werten des obersten Grab-Monsters**. *(dynamische Stats NEU-mittel)*
- **[Infused – 2]:** Put **any monster in your graveyard on top** of it. *(Grab-Reorder NEU-klein)* — das Wunderkind sucht sich sein Idol.

## H · Reliquaries

### 24. Reliquary: The Open Casket — Reliquary · 2100/1600 · req: **8+ Karten im eigenen Grab**
- **E1 (passiv):** Your monsters gain **+300 ATK**.
- **E2 (once/turn):** Move **any card in your graveyard to its top**. *(Grab-Reorder NEU-klein)*
- **[Infused – 3]:** **Special Summon 1 monster from your graveyard** in Defense. *(Revive ✓)*

### 25. Reliquary: The Eleventh Hour — Reliquary · 1900/2200 · req: **2+ eigene Countdown-Karten** *(reqOwnCountdownCards ✓)*
- **E1 (passiv):** During your Standby Phase, your countdowns **tick twice**. *(Doppel-Tick NEU-klein)*
- **[Infused – 2]:** **Strike** one of your countdowns. *(StrikeCountdown ✓)* — das Carillon für alle.

---

## Bilanz

| | |
|---|---|
| Monster / Spells / Artefakte / Reliquaries | 8 / 9 / 6 / 2 |
| Monster mit SS-Bedingung | **8 von 8** (alles vorhandene selfSummon-Felder) |
| Karten mit 2+ Effekten | **25 von 25** |
| Standalone-Infused | #4, 5, 8, 10, 13, 16, 19, 22, 23, 24, 25 (11×) |
| Coupled „Or Infused" | #1, 3, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 21 (13×, #1 hat beides im Set-Schnitt) |
| Direkter LP-Schaden | **0** |
| Wirken im Gegnerzug | #2, 4, 5, 6, 7, 19 (+ Deals #1/#3 lassen den Gegner entscheiden) |
| NEU-mittel (größere Bausteine) | #7 Angriffs-Umlenkung · #12 Grab-Poach · #19 Ersatz-Zerstörung · #23 dynamische Stats |
