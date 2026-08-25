# Road to 1000 — 50 Generics (Design-Entwurf)

*Stand: 25.08.2026. Erste Design-Runde für die Auffüllwelle Richtung 1000 Karten
(Release-Versprechen: 1. September). NUR Design — noch nichts gebaut. Regeln
beachtet: kategoriebasierte Effekte, fast kein Burn, Namen im Hausstil,
Special-Summon-Bedingungen und Infused-Effekte überall, wo sie tragen.
Neue Engine-Mechaniken sind am Ende gesammelt.*

Sechs kleine Mechanik-Familien (ohne Archetyp-Namen — alles Generics, die
Familien teilen nur ein Thema) plus Einzelstücke:

| Familie | Karten | Neue Mechanik |
|---|---|---|
| A. Der abwesende König | 5 | **Alternative Win-Condition** (Insignien sammeln) |
| B. Zugemauerte Zonen | 6 | **Siegel**: Monster-Zonen sperren |
| C. Das oberste Grab | 7 | **Friedhofs-Spitze**: die zuletzt begrabene Karte zählt |
| D. Die Stufenschmiede | 6 | **Level-Manipulation** & Tribut-Rabatte |
| E. Morgige Nachrichten | 7 | **Deck-Spitze & Countdown**: Zukunft lesen und fälschen |
| F. Schildkante voran | 4 | **Mit DEF angreifen** |
| G. Offene Hand | 4 | Hand aufdecken als Preis/Belohnung |
| H. Einzelstücke | 11 | Soft-Counter, Zonen-Rotation, LP-Waage u.a. |

---

## A. Der abwesende König (Alternative Win-Condition, 5 Karten)

*Die Legende: Der König des Gewölbes ist fort; wer Krone, Zepter und Reichsapfel
gleichzeitig an den Tisch bringt, wird gekrönt. Die Insignien sind MONSTER
(0 ATK, können nicht angreifen) — sie belegen Zonen, sie sind angreifbar, sie
sind verführerisches Tributfutter. Der Sieg prüft sich zu Beginn der eigenen
Standby Phase: Der Gegner hat also IMMER einen vollen Zug, um zu antworten,
nachdem die dritte Insignie liegt. Counterplay: jede Form von Removal, Bounce,
Banish, Kontroll-Klau — oder schlicht Tempo.*

**1. The Crown of the Absent King** — Monster · Lv 2 · LIGHT · Myth · 0/2000
- Passiv: Diese Karte kann nicht angreifen.
- SS-Bedingung: Du kannst diese Karte aus der Hand spezialbeschwören, wenn du
  keine anderen Monster kontrollierst (1×/Zug).
- Krönung: Zu Beginn deiner Standby Phase, wenn du „The Crown of the Absent
  King", „The Sceptre of the Absent King" und „The Orb of the Absent King"
  kontrollierst: **Du gewinnst das Duell.**
- INFUSED (eigenständig, 2 Mana): Diese Karte kann diesen Zug nicht durch
  Karteneffekte zerstört werden.

**2. The Sceptre of the Absent King** — Monster · Lv 2 · DARK · Myth · 0/1800
- Passiv: Kann nicht angreifen. + Krönungs-Text (wie oben).
- SS-Bedingung: … wenn dein Gegner mehr Monster kontrolliert als du (1×/Zug).
- Ignition (1×/Zug): Ein Monster des Gegners verliert 300 ATK bis Zugende —
  das Zepter duldet keine Erhebung.
- INFUSED (gekoppelt, 2 Mana): Stattdessen verlieren bis zu 2 Monster je 300.

**3. The Orb of the Absent King** — Monster · Lv 2 · WATER · Myth · 0/1900
- Passiv: Kann nicht angreifen. + Krönungs-Text.
- SS-Bedingung: … wenn du 4000 LP oder weniger hast (1×/Zug).
- Standby: Du erhältst 300 LP.
- INFUSED (eigenständig, 2 Mana): Mische 1 Karte aus deinem Friedhof in dein
  Deck.

**4. The Regent Who Keeps the Throne Warm** — Monster · Lv 1 · EARTH · Human · 700/1400
- Beim Beschwören: Nimm 1 Monster mit 0 ATK aus deinem Deck auf die Hand.
- INFUSED (gekoppelt, 2 Mana): Beschwöre es stattdessen verdeckt.
- (Kategorial gehalten: sucht JEDES 0-ATK-Monster — die Insignien, aber auch
  künftige 0-ATK-Karten. Der Regent hält den Stuhl warm, mehr nicht.)

**5. Long Live the King** — Quick Spell · 2 Mana
- Beschwöre 1 Monster mit 0 ATK aus deinem Friedhof zurück (in Verteidigung).
- INFUSED (gekoppelt, +2 Mana): Es kann diesen Zug zusätzlich nicht zerstört
  werden.
- (Die Schutz- und Recovery-Karte des Plans — und nebenbei Tech für alle
  0-ATK-Mauern im Bestand.)

*Balance-Gedanke: Drei tote Karten im Beatdown, alle suchbar nur über den
Regenten und generische Lv2-Sucher. Ein reines Krönungs-Deck muss ~12 Slots
opfern und zwei volle Züge überleben. Fühlt sich wie Exodia light an — laut,
selten, denkwürdig.*

---

## B. Zugemauerte Zonen (Siegel, 6 Karten)

*Neue Mechanik SIEGEL: Eine LEERE Monster-Zone wird versiegelt — dort kann
nichts beschworen, bewegt oder platziert werden, solange das Siegel hält.
Siegel sind sichtbar (Backstein-Overlay auf der Zone). Sie laufen ab oder
hängen an ihrer Quelle. Drückt auf Reliquare (die brauchen eine freie Zone!)
und auf Schwarm-Decks — und die eigene Seite zumauern ist mit den
Alone/Adjacent-Karten aus 0.1.6b plötzlich auch eine Idee.*

**6. Bricklayer of the Eleventh Hour** — Monster · Lv 1 · EARTH · Human · 900/1300
- SS-Bedingung: … wenn dein Gegner 3+ Monster kontrolliert (1×/Zug).
- Beim Beschwören: Versiegle 1 leere Monster-Zone des Gegners, solange diese
  Karte offen liegt.
- INFUSED (gekoppelt, 2 Mana): Versiegle stattdessen bis zu 2.

**7. The Squatter, Uninvited** — Monster · Lv 2 · WIND · Demon · 1500/1200
- SS-Bedingung: Du kannst diese Karte aus der Hand IN EINE VERSIEGELTE ZONE
  (auch des Gegners Seite? nein — deiner Seite) spezialbeschwören; das Siegel
  bricht dabei (1×/Zug).
- Beim Beschwören: Ziehe 1 Karte, wenn diese Karte ein Siegel gebrochen hat.
- (Das Anti-Tech IN der Familie: Siegel sind stark, also wohnt jemand drin.)

**8. No Room at the Inn** — Spell · 2 Mana
- Versiegle bis zu 2 leere Monster-Zonen des Gegners bis zu deiner nächsten
  End Phase.
- INFUSED (gekoppelt, +2 Mana): Zusätzlich 1 weitere, und ziehe 1 Karte.

**9. Condemned Premises** — Quick Spell · 2 Mana
- Wähle 1 leere Monster-Zone (egal welche Seite); versiegle sie bis zu deiner
  nächsten Standby Phase.
- INFUSED (gekoppelt, +1 Mana): Wähle stattdessen 2 Zonen.
- (Quick = im gegnerischen Zug die Reliquary-Zone wegschnappen, oder die
  eigene Lücke gegen „move"-Angriffe verrammeln.)

**10. The Bricked-Up Door** — Field Artifact
- 1×/Zug — 1 Mana: Versiegle 1 leere Monster-Zone bis zu deiner nächsten
  Standby Phase.
- INFUSED (eigenständig, 2 Mana): Ein Siegel, das du kontrollierst, hält
  stattdessen bis zum Ende des NÄCHSTEN Zuges.

**11. The Landlord's Own Padlock** — Artifact (Monster-Artefakt)
- Ausrüstung: Der Träger kann nicht die Zone wechseln; die beiden an den
  Träger ANGRENZENDEN Zonen gelten, solange sie leer sind, als versiegelt.
- INFUSED (eigenständig, 2 Mana): Der Träger erhält permanent +300 DEF.
- (Ein Monster als wandelnde Mauer — bewusst Anti-Synergie mit den eigenen
  Adjacent-Auren: Wer sperrt, steht allein.)

---

## C. Das oberste Grab (Friedhofs-Spitze, 7 Karten)

*Neue Mechanik: „die oberste Karte deines Friedhofs" = die ZULETZT dort
gelandete Karte. Plötzlich ist die Reihenfolge, in der man Kosten zahlt,
discardet und Monster verliert, ein Skill-Element. (Engine: Friedhof ist
bereits eine geordnete Liste; UI zeigt die oberste ohnehin auf dem Stapel.)*

**12. Gravedigger's First Shift** — Monster · Lv 1 · EARTH · Human · 800/1200
- Beim Beschwören: Schicke 1 Karte aus deinem Deck in den Friedhof.
- INFUSED (gekoppelt, 1 Mana): Suche sie dir aus (statt der obersten
  Deck-Karte).
- (Der Handwerker der Familie: legt gezielt das „oberste Grab".)

**13. Echo of the Latest Loss** — Monster · Lv 2 · DARK · Myth · ?/?  (Basis 500/500)
- SS-Bedingung: … wenn die oberste Karte deines Friedhofs ein Monster ist
  (1×/Zug).
- Passiv: Diese Karte hat permanent ATK/DEF gleich der obersten Monsterkarte
  deines Friedhofs (aktualisiert sich, wenn sich die Spitze ändert).
- INFUSED (eigenständig, 2 Mana): Banne die oberste Friedhofskarte des
  Gegners.

**14. He Sleeps Lightly** — Monster · Lv 2 · DARK · Demon · 1600/1100
- FRIEDHOF · Solange diese Karte die OBERSTE Karte deines Friedhofs ist —
  2 Mana: Spezialbeschwöre sie.
- Beim Beschwören (nur wenn aus dem Friedhof): Sie kann diesen Zug nicht
  angreifen.
- (Ein Monster, das immer wieder aufsteht — solange man es zuoberst hält.
  Jeder weitere Todesfall „begräbt" ihn tiefer. Herrliches Sequencing.)

**15. Last In, First Out** — Spell · 1 Mana
- Nimm die oberste Karte deines Friedhofs auf die Hand.
- INFUSED (gekoppelt, +2 Mana): Die obersten 2.
- (Buchhalter-Poesie: LIFO als Kartenname.)

**16. The Fresh Grave** — Spell · 2 Mana
- Spezialbeschwöre die oberste Karte deines Friedhofs, wenn sie ein Monster
  der Stufe 2 oder niedriger ist.
- INFUSED (gekoppelt, +2 Mana): Beliebige Stufe; es kann diesen Zug nicht
  angreifen.

**17. Buried With His Boots On** — Quick Spell · 2 Mana
- Wenn diesen Zug ein Monster von dir zerstört wurde: Beschwöre die oberste
  Monsterkarte deines Friedhofs VERDECKT.
- INFUSED (gekoppelt, +1 Mana): Ziehe zusätzlich 1 Karte.

**18. The Unquiet Topsoil** — Field Artifact
- 1×/Zug: Lege die oberste Karte deines Friedhofs UNTER den Friedhof.
- 1×/Zug — 2 Mana: Ein „Friedhofs-Spitze"-Effekt von dir darf diesen Zug die
  obersten ZWEI Karten als Spitze behandeln.
- (Der Stellhebel der Familie: sortieren, ohne zu schummeln.)

---

## D. Die Stufenschmiede (Level-Spiele, 6 Karten)

*Level war bisher statisch. Jetzt: Tribut-Rabatte und Level-Änderungen AUF DEM
FELD — mit echten Trade-offs, weil Level-1-Support, Level-Filter und
Tribut-Wert längst existieren.*

**19. A Foot in the Door** — Spell · 1 Mana
- Deine nächste Normalbeschwörung in diesem Zug kostet 1 Tribut weniger.
- INFUSED (gekoppelt, +2 Mana): … kostet keine Tribute.
- (Der Lv3-Beschleuniger. Bewusst 1×: keine zwei Rabatte stapelbar, die Karte
  verbraucht sich mit der Beschwörung.)

**20. The Promotion Board** — Artifact (Feld)
- 1×/Zug — 2 Mana: Ein Monster, das du kontrollierst, wird permanent 1 Stufe
  höher (max. Stufe 3).
- INFUSED (eigenständig, 1 Mana): Ziehe 1 Karte, wenn du diesen Zug ein
  Monster befördert hast.
- (Beförderung klingt gut — aber Level-1-Support lässt den Beförderten
  fallen, und „destroy all Level X"-Karten lesen die neue Stufe. Aufstieg hat
  seinen Preis.)

**21. Demoted for Cause** — Quick Spell · 2 Mana
- Ein Monster auf dem Feld wird bis zur End Phase Stufe 1.
- INFUSED (gekoppelt, +1 Mana): Bis zu 2 Monster.
- (Macht gegnerische Bosse für einen Zug zu „Level-1"-Zielen — die halbe
  Removal-Suite des Spiels wird plötzlich kreativ.)

**22. Cut Down to Size** — Spell · 2 Mana
- Zerstöre 1 Monster, dessen Stufe NIEDRIGER ist als die Anzahl der Monster,
  die sein Besitzer kontrolliert.
- INFUSED (gekoppelt, +2 Mana): Zerstöre bis zu 2 solche Monster.
- (Anti-Schwarm mit Denksport: Ein einzelnes Lv1 ist safe; vier Lv2 in einer
  Reihe sind es nicht.)

**23. Stuck on the Middle Rung** — Monster · Lv 2 · WIND · Human · 1400/1400
- SS-Bedingung: … wenn du ein Monster der Stufe 1 UND eines der Stufe 3
  kontrollierst (1×/Zug).
- Passiv: +300 ATK für jede VERSCHIEDENE Stufe unter deinen Monstern.
- INFUSED (eigenständig, 2 Mana): Diese Karte wird bis Zugende die Stufe
  deiner Wahl (1–3).

**24. The Overqualified Doorman** — Monster · Lv 3 · LIGHT · Human · 2100/2100
- SS-Bedingung: … wenn dein Gegner ein Monster der Stufe 3 kontrolliert und
  du keines (1×/Zug).
- Beim Beschwören: Ein gegnerisches Monster wird bis zur End Phase Stufe 1.
- INFUSED (eigenständig, 2 Mana): 1×/Zug: Diese Karte kann diesen Zug nicht
  durch Kampf zerstört werden.

---

## E. Morgige Nachrichten (Deck-Spitze & Countdown, 7 Karten)

*Die Zukunft lesen, fälschen — und auf sie warten. Kombiniert Top-Deck-Wissen
(die Gamble-Karten werden durch Manipulation zu Skill-Karten) mit einem
Countdown-Artefakt, das der Gegner tickken sieht.*

**25. The Ink Still Wet** — Spell · 1 Mana
- Sieh dir die obersten 3 Karten deines Decks an, lege sie in beliebiger
  Reihenfolge zurück.
- INFUSED (gekoppelt, +1 Mana): Lege zusätzlich bis zu 1 davon unter das
  Deck und ziehe 1 Karte.

**26. The Day After Tomorrow's News** — Spell · 2 Mana
- Sieh dir die obersten 2 Karten des GEGNERISCHEN Decks an und lege sie in
  beliebiger Reihenfolge zurück.
- INFUSED (gekoppelt, +2 Mana): Lege stattdessen bis zu 1 davon unter sein
  Deck.
- (Kein Mill, kein Burn — pure Sabotage der gegnerischen Zukunft. Gemein auf
  die leise Art.)

**27. The Self-Fulfilling Prophecy** — Quick Spell · 2 Mana
- Decke die oberste Karte deines Decks auf: Ist sie ein Monster der Stufe 2
  oder niedriger, spezialbeschwöre es; sonst lege sie in den Friedhof.
- INFUSED (gekoppelt, +2 Mana): Beliebige Stufe; ein beschworenes Monster
  kann diesen Zug nicht angreifen.
- (Mit #25 vorbereitet ist das kein Glücksspiel mehr — das ist der Punkt.)

**28. She Reads the Weather in Entrails** — Monster · Lv 1 · DARK · Human · 900/1200
- SS-Bedingung: … wenn diesen Zug eine Karte aufgedeckt wurde (1×/Zug).
- Beim Beschwören: Decke die oberste Karte deines Decks auf; du darfst sie
  unter das Deck legen.
- INFUSED (eigenständig, 2 Mana): Dasselbe für das Deck des Gegners.

**29. The Calendar's Last Page** — Monster · Lv 3 · DARK · Myth · 2200/1800
- SS-Bedingung: … ab deinem 7. Zug (1×/Duell).
- Beim Beschwören: Ziehe 2 Karten, dann lege 1 Handkarte unter dein Deck.
- INFUSED (eigenständig, 3 Mana): Bis Zugende können deine aufgedeckten
  Deck-Spitzen-Monster wie Handkarten normalbeschworen werden. *(ambitioniert
  — Engine-Notiz unten; zur Not ersatzweise: „Decke die obersten 2 auf, nimm
  alle Monster darunter auf die Hand.")*

**30. The Appointed Hour** — Artifact (Feld) · Countdown
- Kommt mit 3 „Stunden"-Markern ins Spiel. In jeder deiner Standby Phases:
  entferne 1 Marker.
- Beim letzten Marker: Ziehe 2 Karten, erhalte diesen Zug 2 Mana, und gib bis
  zu 1 Karte, die dein Gegner kontrolliert, auf seine Hand zurück. Zerstöre
  dann diese Karte.
- INFUSED (eigenständig, 2 Mana, 1×/Zug): Entferne 1 zusätzlichen Marker.
- (Drei Züge sichtbare Vorfreude — der Gegner MUSS sich um ein tickendes
  Artefakt kümmern. Verwandt mit Slowburns „Charged", aber öffentlich und
  generisch.)

**31. Ink for the Third Edition** — Quick Spell · 1 Mana
- Lege 1 Karte aus deiner Hand oben AUF dein Deck; ziehe 1 Karte.
- INFUSED (gekoppelt, +1 Mana): Danach darfst du die oberste Karte deines
  Decks aufdecken.
- (Zyklus-Karte der Familie; legt gezielt die eigene „Prophezeiung" — und
  triggert #28.)

---

## F. Schildkante voran (DEF-Angriff, 4 Karten)

*Neuer Passiv-Flag: „Diese Karte greift mit ihrer DEF an." Die Mauern des
Spiels — und davon gibt es RICHTIG viele im Bestand — bekommen eine
Offensiv-Schule. Kein neues Zahlenmodell: Beim Angriff zählt schlicht DEF
statt ATK, alles andere (Positionen, Piercing, Auren) bleibt.*

**32. He Who Leads With His Shoulder** — Monster · Lv 2 · EARTH · Human · 400/1900
- Passiv: Diese Karte greift mit ihrer DEF an.
- INFUSED (eigenständig, 2 Mana): +300 DEF bis Zugende.

**33. The Vault's Own Doorframe** — Monster · Lv 3 · EARTH · Mecha · 0/2600
- SS-Bedingung: … wenn du 2+ Artefakte kontrollierst (1×/Zug).
- Passiv: Greift mit DEF an. Kann im Beschwörungszug nicht angreifen.
- INFUSED (eigenständig, 3 Mana): Bis Zugende erhalten deine anderen Monster
  in ANGRENZENDEN Zonen +400 DEF.

**34. Doorstop Made of Dragon Bone** — Monster · Lv 1 · FIRE · Dragon · 0/1500
- Passiv: Greift mit DEF an. Verliert nach einem Angriff 300 DEF permanent
  (der Türstopper splittert).
- INFUSED (eigenständig, 1 Mana): Repariere ihn — +300 DEF permanent
  (1×/Zug).

**35. Lead With the Shield** — Spell · 1 Mana
- 1 Monster, das du kontrollierst, greift bis Zugende mit seiner DEF an.
- INFUSED (gekoppelt, +2 Mana): Bis zu 3 Monster; sie erhalten zusätzlich
  je +200 DEF bis Zugende.
- (Verwandelt jedes Stall-Board für einen Zug in eine Schildwall-Offensive.)

---

## G. Offene Hand (Reveal, 4 Karten)

*Information als Währung: die eigene Hand herzeigen ist der Preis, Wissen über
den Gegner der Lohn. Nur Momentaufnahmen (kein Dauer-Offenlegen — das bleibt
UI-schonend).*

**36. An Honest Man's Bluff** — Spell · 1 Mana
- Zeige deine Hand vor: Enthält sie kein Spell, ziehe 2 Karten; sonst ziehe 1.
- INFUSED (gekoppelt, +1 Mana): Danach darfst du 1 Handkarte unter dein Deck
  legen.
- (Der Witz: Die Karte selbst war eben noch ein Spell auf der Hand — direkt
  nachgelegt wird der „ehrliche Mann" wahr.)

**37. The Beggar Who Shows His Purse** — Monster · Lv 1 · WIND · Human · 1000/1000
- SS-Bedingung: … wenn du 2 oder weniger Handkarten hast (1×/Zug).
- Beim Beschwören: Zeige deine Hand vor; ist sie leer, ziehe 2 Karten.
- INFUSED (eigenständig, 1 Mana): Dein Gegner zeigt dir eine zufällige
  Handkarte.

**38. The Transparent Man** — Monster · Lv 2 · LIGHT · Myth · 1200/1600
- Beim Beschwören: Zeige deine Hand vor; +200 ATK permanent für jedes
  vorgezeigte Monster.
- INFUSED (eigenständig, 2 Mana): Diese Karte kann diesen Zug nicht Ziel
  gegnerischer Effekte werden.
- (Er hat nichts zu verbergen — und wird genau daraus stark.)

**39. Everything Above Board** — Quick Spell · 2 Mana
- Beide Spieler zeigen ihre Hände vor. Du darfst danach 1 Karte ziehen, wenn
  dein Gegner mehr Handkarten hat als du.
- INFUSED (gekoppelt, +1 Mana): Nur DEIN Gegner zeigt vor.
- (Der Scouting-Klassiker — vor der eigenen Battle Phase Gold wert.)

---

## H. Einzelstücke (11 Karten)

**40. The Thousandth Card** — Monster · Lv 1 · LIGHT · Myth · 1000/1000
- SS-Bedingung: … wenn dein Deck 40+ Karten enthält (1×/Zug).
- Beim Beschwören: Ziehe 1 Karte.
- INFUSED (eigenständig, 1 Mana, 1×/Duell): Mische diese Karte in dein Deck;
  ziehe 2 Karten.
- (Die Jubiläumskarte des Sets. Eine unter tausend — und sie hat zu dir
  gefunden. Stats natürlich 1000/1000.)

**41. Countersign** — Quick Spell · 2 Mana
- Das nächste Spell, das dein Gegner in diesem Zug aktiviert, kostet 2 Mana
  mehr.
- INFUSED (gekoppelt, +1 Mana): … kostet 3 mehr, und du ziehst 1 Karte, wenn
  er diesen Zug kein Spell mehr aktiviert.
- (Der Soft-Counter: kein Negate, sondern eine Steuer im gegnerischen Zug —
  neues Interaktions-Gefühl für den Response-Slot.)

**42. Eviction Notice** — Spell · 1 Mana
- Gib 1 Monster, das nicht angreifen kann, auf die Hand seines Besitzers
  zurück.
- INFUSED (gekoppelt, +2 Mana): Bis zu 2 solche Monster.
- (Meta-Pflege in Kartenform: Mauern, Insignien (!), Token-Verwandtes —
  alles, was nur rumsteht, bekommt die Kündigung. Hält u. a. die eigene
  Familie A ehrlich.)

**43. Wrong Queue, Sir** — Quick Spell · 2 Mana
- BEWEGE 1 gegnerisches Monster in eine andere leere Monster-Zone seiner
  Seite (deiner Wahl).
- INFUSED (gekoppelt, +1 Mana): Bewege bis zu 2.
- (Erstmals Gegner-Movement: Facing-Sniper verschieben, Adjacent-Auren
  zerreißen, den Hangman ins Leere schicken.)

**44. The Turntable** — Spell · 2 Mana
- Verschiebe ALLE deine Monster um eine Zone in dieselbe Richtung (links
  oder rechts; das Ende bleibt stehen, wenn es nicht passt — dann rückt nur,
  was Platz hat).
- INFUSED (gekoppelt, +1 Mana): Ziehe 1 Karte, wenn dabei 3+ Monster bewegt
  wurden.
- (Ein Griff, und die ganze Formation dreht: Facing- und Adjacent-Decks
  bekommen ihr Manöver.)

**45. Settle the Difference** — Quick Spell · 3 Mana · **1×/Duell**
- Die LP beider Spieler werden auf den NIEDRIGEREN der beiden Werte gesetzt.
- INFUSED (gekoppelt, +2 Mana): Danach erhältst du 1000 LP.
- (Die Waage bricht Stall-Zustände und belohnt aggressive Linien, ohne Burn
  zu sein. 1×/Duell und teuer, weil der Effekt Spiele dreht.)

**46. The Even Scales** — Monster · Lv 2 · LIGHT · Myth · 1500/1500
- SS-Bedingung: … wenn die LP beider Spieler höchstens 500 auseinanderliegen
  (1×/Zug).
- Passiv: Solange die LP beider Spieler höchstens 500 auseinanderliegen, kann
  diese Karte nicht Ziel von Effekten werden.
- INFUSED (eigenständig, 2 Mana): Du erhältst LP in Höhe der halben Differenz
  beider LP-Stände (max. 1000).

**47. First Mover's Advantage** — Spell · 1 Mana
- Nur in deinem ERSTEN Zug des Duells aktivierbar: Erhalte diesen Zug 2 Mana.
- INFUSED (gekoppelt, +1 Mana): Ziehe zusätzlich 1 Karte, banne dann „First
  Mover's Advantage" (diese Kopie).
- (Eröffnungstheorie als Karte. Später gezogen ist sie tot — der Preis für
  den Blitzstart.)

**48. The Standing Order** — Artifact (Spieler)
- Deine Draw Phase: Du DARFST statt zu ziehen die oberste Karte deines
  Friedhofs auf die Hand nehmen.
- INFUSED (eigenständig, 2 Mana, 1×/Zug): Lege die oberste Karte deines
  Decks in den Friedhof.
- (Dauerauftrag bei der Bank des Todes; Brücke in Familie C. Draw-Ersatz ist
  neu und herrlich unheimlich.)

**49. Two Truths and a Lie** — Spell · 2 Mana
- Beschwöre bis zu 3 Monster aus deiner Hand VERDECKT (in Verteidigung).
- INFUSED (gekoppelt, +1 Mana): Ziehe danach 1 Karte für jedes so gesetzte
  Monster über das erste hinaus.
- (Drei Rücken, eine Lüge — Flip-, Lyria- und Bluff-Spielern läuft das
  Wasser im Mund zusammen, ganz ohne Archetyp-Stempel.)

**50. Making Ends Meet** — Spell · 1 Mana
- Wenn du 0 oder 1 Monster kontrollierst: Erhalte diesen Zug 2 Mana.
- INFUSED (gekoppelt, +1 Mana): Ziehe zusätzlich 1 Karte, wenn deine Hand
  danach 3 oder weniger Karten enthält.
- (Das Armuts-Stipendium: Comeback-Mana, das Board-Führende nicht nutzen
  können.)

---

## Engine-Anforderungen (neue EffectActionTypes / Flags — ans ENDE der Enums!)

1. **WinCondition-Check** (A): Standby-Trigger „kontrolliere Karten X+Y+Z →
   Duell gewonnen". Sauberster Weg: neue Action `WinTheDuel` + bestehende
   Bedingungsprüfung; Log-Zeile + eigener GameOver-Grund („Coronation!").
2. **Zone-Siegel** (B): Zonen-Status `sealed` (+ Ablauf-Timing), Prüfung in
   Summon/Set/Move/Reliquary-Platzierung; UI-Overlay auf der Zone.
3. **Friedhofs-Spitze** (C): Referenz `GraveyardTop` als Zielkategorie (Liste
   ist schon geordnet); #13 braucht dynamische Stat-Bindung, #14 einen
   Graveyard-Ignition mit Positionsprüfung.
4. **Level-Änderung & Tribut-Rabatt** (D): temporäre/permanente Level-Deltas
   (Filter lesen das effektive Level) + Zähler `nextNormalSummonDiscount`.
5. **Countdown-Marker** (E, #30): Marker-Typ analog Death Counter, Trigger
   „letzter Marker entfernt".
6. **Deck-Spitze aufgedeckt lassen** (E, #29-Infused): aufwendig — zur Not
   die notierte Ersatzfassung nehmen.
7. **DEF-Angriff** (F): Kampfrechnung nutzt DEF des Angreifers, Flag
   `attacksWithDef`; Anzeige im Inspect („greift mit DEF an").
8. **Gegner-Movement** (H, #43/#44): Move-Action auf gegnerische Monster
   erweitern + „alle eigenen rotieren".
9. **Spell-Steuer auf Zeit** (H, #41): temporärer Kostenaufschlag für den
   nächsten gegnerischen Spell (verwandt mit Guild Tariff, aber einmalig und
   getimt).
10. **LP-Angleichung** (H, #45): `SetLPToLower` — symmetrisch, kein Damage
    (umgeht bewusst alle Burn-/Damage-Interaktionen).
11. **Draw-Ersatz** (H, #48): Draw-Phase-Hook „ersetze Zug durch
    Friedhofs-Spitze" mit Spielerwahl.

*Burn-Bilanz des Sets: 0 direkte Schadenskarten — Removal, Bewegung, Siegel,
Information und Ressourcenspiele tragen die Welle. (Regel eingehalten.)*

*Nächste Schritte nach dem Design-Go: Engine-Actions (Kochbuch 8.2) →
Selftest-Proben je Mechanik → Assets/Katalog → Export → Bot-Proben →
Übersetzung in alle 8 Tabellen → Testserver.*
