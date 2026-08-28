# Incarnates — die ersten 5 (Design zur Review)

Stand: 2026-08-29 · Status: **Design zur Review** (Groundworks sind gebaut, diese
Karten NICHT) · Neue Extra-Deck-Kartenart in Rot + Riten-Zauber + Vessel-System.

**Die Regeln (gebaut):** Ein Incarnate hat ein eigenes Level (4–9). Zwei Wege aufs Feld:
- **Opfergabe (temporär):** Monster opfern, deren Level-Summe EXAKT das Incarnate-Level
  ergibt — mindestens eines ein **Vessel** (70 sind markiert, 2 je Archetyp, alle Lv 1–2).
  Kein Mana. In der Standby Phase deines nächsten Zuges kehrt es ins Extra Deck zurück.
- **Rite (permanent):** Roter Zauber, benennt EIN Opfer-Monster und EIN Incarnate —
  so gerufen bleibt es dauerhaft.

Design-Spannung: Die Opfergabe ist der günstige Blitz (großer Auftritt, eine Runde),
die Rite die teure Bindung (das benannte Opfer muss ins Deck und aufs Feld). Effekte
sind so gebaut, dass BEIDE Wege sich lohnen: On-Summon-Impact für die Opfergabe,
Dauer-Passiva für die Rite.

---

## 1. Maw of the First Winter — Incarnate · Lv 4 · 2400/1800 · Water/Myth
- **E1 (On Summon):** Der Atem des ersten Winters: ALLE Monster deines Gegners können
  diesen Zug nicht angreifen und nicht die Position wechseln. *(Massen-Freeze — Cannot
  Attack + Position-Lock auf alle; Massen-Variante NEU-klein)*
- **E2 (passiv):** Monster, die dieses Incarnate angreifen, verlieren nach dem Kampf
  dauerhaft 300 ATK. *(Frostbiss — Widow-Kontext-Baustein ✓)*
- **[Infused – 2]:** Ein Monster deines Gegners wird verdeckt gelegt. *(SetTargetFaceDownDefense ✓)*
- **Rite: „Rite of the First Winter"** — Opfer: **Tidebound Skimmer** (Vessel). Der
  Gezeiten-Läufer friert im ersten Frost ein.
- Rolle: Der Tempo-Dieb — eine Opfergabe (z.B. Lv2+Lv2 oder Lv3+Lv1) kauft dir eine
  komplette Runde Ruhe.

## 2. The Hungering Choir — Incarnate · Lv 5 · 2700/2000 · Dark/Demon
- **E1 (On Summon):** Verbanne die obersten 3 Karten des GEGNER-Friedhofs; dieses
  Incarnate erhält dauerhaft 300 ATK je so verbannter Karte. *(BanishOpponentGraveTop ✓
  + Zähl-Kopplung NEU-klein)*
- **E2 (passiv):** Dein Gegner kann keine Monster aus seinem Friedhof beschwören.
  *(Anti-Recursion-Aura NEU-klein)* — der Chor singt, und die Toten bleiben liegen.
- **[Or Infused +2] auf E1:** Verbanne 5 statt 3.
- **Rite: „Rite of the Hungering Choir"** — Opfer: **Sacrilegion Willing Lamb** (Vessel).
  Das willige Lamm, verschlungen vom Chor — die Sakrilegion-Brücke.
- Rolle: Der Grab-Henker — gegen Friedhofs-Decks (GraveTop-Familie, Séance, Deckay)
  brutal, sonst solide.

## 3. Colossus of the Broken Gate — Incarnate · Lv 6 · 3000/2600 · Earth/Mecha
- **E1 (On Summon):** Zerstöre bis zu 2 Zauber/Artefakte deines Gegners.
  *(EnemySpellOrArtifact ✓, targetCount 2)*
- **E2 (passiv):** Durchstoß. *(passivePiercing ✓)*
- **[Infused – 2]:** Dieses Incarnate darf diese Battle Phase zweimal angreifen.
  *(GrantAdditionalAttack ✓)*
- **Rite: „Rite of the Broken Gate"** — Opfer: **Barrierstruck Mason** (Vessel). Der
  Maurer, der das Tor errichtete, reißt es nieder.
- Rolle: Der Belagerungsbrecher — Backrow-Removal + Piercing-Druck; via Rite die
  permanente Abrissbirne.

## 4. She Who Outlives — Incarnate · Lv 7 · 2900/2900 · Light/Myth
- **E1 (On Summon):** Deine anderen Monster können diesen Zug nicht zerstört werden
  (Kampf und Effekte). *(CannotBeDestroyed-EOT, Massen-Variante NEU-klein)*
- **E2 (passiv, die Vergänglichkeits-Pointe):** Kehrt dieses Incarnate ins Extra Deck
  zurück ODER verlässt es das Feld: Beschwöre 1 Level-1-Monster aus deinem Friedhof in
  Verteidigungsposition. *(Abschiedsgeschenk — Rückkehr-Trigger NEU-klein)* — sie geht,
  aber sie lässt Leben zurück.
- **[Infused – 3]:** Ein Monster deines Gegners kann diesen Zug nicht angreifen.
  *(CannotAttackThisTurn ✓)*
- **Rite: „Rite of Her Outliving"** — Opfer: **Heavenly Acolyte** (Vessel). Die Novizin
  gibt ihr Leben — und überdauert in IHR.
- Rolle: Der Schutzwall mit Nachlass — die Opfergabe schützt einen Angriffs-Zug, die
  Rite macht sie zur dauerhaften Lebensversicherung.

## 5. Avatar of the Thousandth Card — Incarnate · Lv 8 · ?/? · Dark/Myth
- **E1 (passiv, die Opfergabe-Fantasie):** Dieses Incarnate betritt das Feld mit den
  AUFSUMMIERTEN gedruckten ATK und DEF aller Monster, die für seine Beschwörung geopfert
  wurden. *(Opfer-Summen-Stats NEU-mittel)* — drei 2000er geopfert = ein 6000er-Koloss
  für eine Runde; via Rite nur vom benannten Einzelopfer gespeist (bewusst schwächer,
  dafür permanent — der Tradeoff der Kartenart in einer Karte).
- **E2 (On Summon):** Dein Gegner kann diesen Zug keine Karten als Reaktion auf
  Angriffe dieses Incarnates aktivieren?? — GESTRICHEN, zu komplex. Stattdessen:
  **E2 (passiv):** Kann nicht zum Ziel gegnerischer Effekte werden. *(passiveUntargetable ✓)*
- **[Infused – 3]:** Alle anderen offenen Monster verlieren bis Zugende 500 ATK.
  *(DebuffAllEnemyAtkEot ✓ + eigene? Nur gegnerische — DebuffAllEnemyAtkEot ✓)*
- **Rite: „Rite of the Thousandth"** — Opfer: **The Thousandth Card**. Die tausendste
  Karte wird zum Avatar ihrer selbst — das Jubiläums-Easter-Egg zur 1000er-Marke.
- Rolle: Das Finisher-Monument — die Level-8-Opfergabe frisst fast dein Board und
  gebiert einen Titanen auf Zeit.

---

## Die 5 Riten (rote Zauber, isRite)

| Rite | Opfer (steht auf der Karte) | Incarnate | Mana |
|---|---|---|---|
| Rite of the First Winter | Tidebound Skimmer | Maw of the First Winter | 2 |
| Rite of the Hungering Choir | Sacrilegion Willing Lamb | The Hungering Choir | 2 |
| Rite of the Broken Gate | Barrierstruck Mason | Colossus of the Broken Gate | 3 |
| Rite of Her Outliving | Heavenly Acolyte | She Who Outlives | 3 |
| Rite of the Thousandth | The Thousandth Card | Avatar of the Thousandth Card | 4 |

Jede Rite trägt zusätzlich einen kleinen Zweitnutzen als **[Infused – 1]**:
„Füge das benannte Opfer-Monster aus deinem Deck deiner Hand hinzu" — die Rite SUCHT
ihr eigenes Opfer, wenn es noch fehlt. *(AddTargetFromDeckToHand ✓ mit nameFilter)*

## Bilanz

| | |
|---|---|
| Incarnates | 5 (Level 4/5/6/7/8 — eine Treppe) |
| Riten | 5 (je 1, mit Such-Infused) |
| Direkter LP-Schaden | 0 |
| Neue Engine-Bausteine | Massen-Freeze, Massen-Schutz, Anti-Grab-Aura, Rückkehr-Trigger (je NEU-klein) · Opfer-Summen-Stats (NEU-mittel) |
| Katalog nach Bau | 951 + 10 = 961 |
