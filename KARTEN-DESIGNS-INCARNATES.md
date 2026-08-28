# Incarnates — die ersten 5 (Designs von Felix, GEBAUT)

Stand: 2026-08-29 · Status: **GEBAUT** (releaseVersion 0.1.8, Katalog 961) · Felix'
Designs ersetzen den ersten Entwurf; Bau-Details im Commit dokumentiert.

**System (gebaut):** Incarnates = rote Extra-Deck-Karten mit eigenem Level.
- **Opfergabe (temporär):** Level-Summe der Opfer EXAKT = Incarnate-Level, mind. 1 VESSEL,
  kein Mana; Rückkehr ins Extra Deck in der Standby Phase des nächsten eigenen Zuges.
  **114 Vessels** markiert: je Archetyp 2 niedrige + 1 Lv3, 10 Generics, plus die 5
  Riten-Opfer (Ice Warden, Archfiend Overlord, Sworn to the Gate, Wenna, The Thousandth Card).
- **Rite (permanent):** rote Zauber mit Mana-Kosten, opfern ihr benanntes Monster.
  Jede Rite sucht per [Infused – 1] ihr Opfer aus dem Deck.

---

## 1. Maw of the First Winter — Lv 7 · 2000/2000 · Water/Myth
- **E1 (On Summon):** Alle Gegner-Monster → Verteidigung + Position-Lock bis zum Ende
  des nächsten gegnerischen Zuges.
- **E2 (passiv):** Monster, die mit ihr im Kampf waren (beide Richtungen), verlieren
  danach dauerhaft 500 ATK.
- **E3 (passiv):** Kann nicht im Kampf zerstört werden.
- **E4 [Infused – 4, Quick, beide Züge, 1×/Zug]:** Ziel-Monster verliert dauerhaft
  500 ATK; fällt es dadurch auf 0, wird es zerstört.
- **Rite of the First Winter (3 Mana)** — Opfer: **Ice Warden** (Vessel).

## 2. The Hungering Demon — Lv 7 · 3000/2500 · Dark/Demon
- **E1 (On Summon):** Verbanne die obersten 3 Karten des GEGNER-DECKS; +200 ATK
  permanent je Karte.
- **E2 (passiv):** Der Gegner kann keine Monster aus dem Friedhof beschwören.
- **E3 (passiv):** Kann nicht vom Feld verbannt werden.
- **E4 [Infused – 3, Quick, beide Züge, 1×/Zug]:** Verbanne ALLE Monster aus deinem
  Friedhof; +100 ATK bis Zugende je Karte.
- **Rite of Unending Hunger (3 Mana)** — Opfer: **Archfiend Overlord** (Vessel).

## 3. Colossus of the Broken Gate — Lv 8 · 2400/2500 · Earth/Demon
- **E1 (passiv):** Solange er auf dem Feld liegt, kann KEIN Spieler Zauber aktivieren.
- **E2 (passiv):** Kann nicht von Effekten als Ziel gewählt werden.
- **E3 (passiv):** Jede gegnerische Artefakt-Effekt-Aktivierung: +200 ATK und DEF permanent.
- **Rite of the Broken Gate (4 Mana)** — Opfer: **Sworn to the Gate** (Vessel; die Karte
  wurde zugleich umgebaut: „Kein Spieler kann Monster spezialbeschwören — außer
  Incarnate-Beschwörungen" statt der alten Besitzer-Sperre).

## 4. She Who Outlives — Lv 8 · 3000/3000 · Light/Myth
- **E1 (On Summon):** Wähle 1 Monster auf dem Feld; es kann permanent nicht mehr durch
  Kampf zerstört werden.
- **E2 (passiv):** Würde sie zerstört, darf ihr Besitzer stattdessen 1 Handkarte abwerfen.
- **E3 [Infused – 2, Quick, 1×/Zug]:** Verbanne sie bis Zugende (Rückkehr in freie Zone,
  sonst Friedhof; die Temporär-Uhr einer Opfergabe läuft weiter); beschwöre 1
  LICHT-Monster ≤2000 ATK aus dem Friedhof.
- **E4 [Or Infused +3]:** …oder aus dem Deck.
- **Rite of Eternal Life (4 Mana)** — Opfer: **Wenna, Who Waits Outside** (Vessel).

## 5. Avatar of the Thousandth Card — Lv 9 · ?/? · Dark/Angel
- **E1 (passiv):** Betritt das Feld mit den AUFSUMMIERTEN gedruckten ATK/DEF aller
  Opfer als Basiswerte (per Rite: nur The Thousandth Card — 1000/1000-Basis).
- **E2 (passiv):** Der Gegner kann keine Effekte als Reaktion auf deine Monster-Effekte
  aktivieren (das Reaktionsfenster öffnet gar nicht erst).
- **E3 [Infused – 2, Quick, 1×/Zug, nur auf ein Gegner-Kettenglied mit Draw]:** Negiere
  die Aktivierung, zerstöre die Karte; war es ein Monster, erleidet der Gegner Schaden
  in Höhe seiner ATK.
- **Rite of the Thousandth (5 Mana)** — Opfer: **The Thousandth Card** (Vessel).

---

## Nachweise (Selftest, 300 Duelle, 0 Fehler)

55 Opfergaben · 10 Standby-Rückkehren · 214 Riten-Opferungen · 88 Deck-Verschlingungen ·
5 Frostbisse · 33 Segen · 64 Schleier-Blinks · 212 Outlive-Rettungen · 1 Gier-Bestrafung.
Regression (Zufall + Welle-3-Decks): 420 weitere Duelle, 0 Fehler.
