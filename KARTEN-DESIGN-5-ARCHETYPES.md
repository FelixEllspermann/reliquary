# 5 neue Archetypes — Entwurf

61 Karten. Noch nichts gebaut, noch nichts entschieden.

| # | Archetype | Attribut / Typ | Karten | Thema |
|---|---|---|---|---|
| 1 | **Mechination** | Earth / Mecha | 15 | Kombo über Kategorie-Suchen |
| 2 | **Sleightwind** | Wind / Demon | 8 | Handkarten, die im Gegnerzug zuschlagen |
| 3 | **Kindlekin** | Fire / Beast | 10 | Level-1-Schwarm, der sich selbst beschwört |
| 4 | **Manacle** | Dark / Myth | 13 | Mana rauben und verbrennen |
| 5 | **Sacrilegion** | Light / Dragon | 15 | Reliquarys, deren Beschwörung das Feld leert |

### Woher die Namen kommen

Alle fünf sind Wortspiele auf die eigene Mechanik — im Stil dessen, was schon im
Spiel steht (Wyldpack, Fethaerbreese, Barrierstruck).

- **Mechination** — *Mech* + *machination*. Eine Maschine, die eine Intrige
  spinnt. Genau das tut ein Kombo-Deck.
- **Sleightwind** — *sleight of hand* (Fingerfertigkeit, Taschenspielertrick)
  trifft *slight wind*. Der Archetyp spielt aus der Hand, und niemand sieht ihn
  kommen. Attribut WIND ist im Namen mitgeliefert.
- **Kindlekin** — *to kindle* heisst entzünden, und **ein „kindle" ist zugleich
  das englische Sammelwort für einen Wurf Kätzchen.** Feuer, Rudel und
  Kleinvieh in einem echten Wort — deshalb ist der Typ BEAST.
- **Manacle** — eine Fussfessel. Und das Wort trägt *Mana* in sich. Der
  Archetyp legt dem Gegner sein Mana in Ketten. (Der Prefix ist „Manacle",
  nicht „Mana" — es gibt heute keinen einzigen Namensfilter auf „Mana",
  Kollision ausgeschlossen. Falls je einer dazukommt, träfe er auch diese
  Karten.)
- **Sacrilegion** — *sacrilege* + *legion*. Eine Legion, die opfert, was ihr
  nicht gehört. Die Beschwörungen fressen Monster von beiden Feldern.

---

## Zuerst: drei Sachen kann die Engine noch nicht

Ich habe das Effekt-Vokabular durchgesehen, bevor ich entworfen habe. Drei der
fünf Archetypen brauchen jeweils **ein** neues Engine-Stück. Ohne die geht die
Kernidee nicht — mit Notlösungen würde ich dir Archetypen bauen, die etwas
anderes tun als das, was du bestellt hast.

### Lücke A — Handkarten im Gegnerzug (Archetype 2)

`EffectTrigger.Quick` wird ausschliesslich von Karten **auf dem Feld**
eingesammelt (`DuelActions.cs:1878`, `responder.FieldCards()`).
`HandIgnition` gibt es, aber das ist nur die eigene Main Phase.

Es fehlt: **`HandQuick`** — ein Trigger, der im Reaktionsfenster auch die Hand
des Antwortenden durchsucht. Eine Enum-Zeile plus eine Schleife an derselben
Stelle. Ohne das ist Archetype 2 nur ein weiterer Main-Phase-Archetype.

### Lücke B — Mana wird jede Runde zurückgesetzt (Archetype 4)

`DuelManager.cs:471` setzt zu Beginn **jedes** Zuges `player.Mana = player.ManaPerTurn`.
Daraus folgen zwei Dinge, die dem Archetyp den Boden wegziehen:

- **Mana klauen in deinem Zug ist wirkungslos.** Der Gegner füllt zu Zugbeginn
  ohnehin komplett auf.
- **Mana gewinnen im Gegnerzug ist wirkungslos.** Dein Zugbeginn überschreibt es.

Ein „Steal" aus `DrainOpponentMana` + `GainMana` in einem Effekt tut also
schlicht nichts — je nachdem, wessen Zug läuft, verpufft die eine oder die
andere Hälfte.

Es fehlt: **eine Übertragung in die nächste Runde.** Zwei Zahlen auf
`PlayerState` (`ManaDebt`, `ManaCredit`), die beim Auffüllen verrechnet werden,
plus zwei Aktionen (`DrainOpponentManaNextTurn`, `GainManaNextTurn`). Erst damit
ist Mana-Denial überhaupt spürbar.

Was **heute schon** geht: `Quick`-Effekte von Karten auf dem Feld, die im
Gegnerzug Mana abziehen. Das ist echtes Denial und braucht nichts Neues — ich
habe den Archetyp so gebaut, dass er auch ohne Lücke B zur Hälfte funktioniert.

### Lücke C — Gegnermonster als Beschwörungsmaterial (Archetype 5)

`ReliquaryCardData` kennt an Kosten nur `costBanishMonstersFromGrave` und
`costTributeOtherMonster` (genau 1 eigenes Monster). Es gibt **keine**
Möglichkeit, gegnerische Monster als Material zu verlangen.

Es fehlt: **`costTributeOwnMonsters` (0–3)** und **`costTributeOpponentMonsters` (0–2)**.
Das ist genau die Idee aus deiner Nachricht — und es ist zugleich das Removal.
Ohne diese zwei Felder ist Archetype 5 nur ein weiterer Reliquary-Stapel.

**Alles andere im Entwurf läuft mit dem, was heute da ist.**

---

## Was die Engine kann, und woran ich mich gehalten habe

- **Level 1–3.** Level 1 gratis, Level 2 = 1 Tribut, Level 3 = 2 Tribute.
- **Mana:** Start 3, +1 pro Zug, Deckel 10.
- **Kategorie-Suchen** sind da: `AddTargetFromDeckToHand` mit Level-, Attribut-
  und Typ-Filter. Genau dein „füge ein Wasser-Level-2-Monster hinzu".
  **Keine einzige Suche in diesem Entwurf nennt einen Kartennamen.**
- **Beschwörungs-Bedingungen** dürfen dagegen Namen nennen („während du ein
  ‚Kindlekin'-Monster kontrollierst") — das ist keine Suche, sondern eine
  Bedingung, und ohne sie wären Selbst-Beschwörer nicht eingrenzbar.
- **Board Wipe:** `DestroyAllMonstersExceptType` räumt beide Felder ab und
  verschont einen Typ. Setzt man den auf den eigenen Typ, ist es einseitig —
  ausser der Gegner spielt zufällig denselben Typ. Das ist ehrlich so und steht
  auf der Karte.
- **Wenig Burn.** Kein einziger direkter LP-Schaden in allen 61 Karten.

---

# 1 · MECHINATION — Earth / Mecha

**15 Karten:** 9 Monster, 2 Reliquary, 1 Feld-Artefakt, 3 Zauber

Der Kombo-Archetyp. Jedes Teil sucht das nächste **über seine Kategorie**, nie
über den Namen — dadurch gibt es keine feste Linie, sondern eine, die sich aus
dem baut, was gerade da ist. Zwei Suchen führen nie zwingend zur selben Karte.

### Monster

| Karte | Lv | ATK/DEF | Rar | Effekt |
|---|---|---|---|---|
| **Mechination Cogwright** | 1 | 500/900 | C | **Bei Normalbeschwörung:** Füge 1 Level-1-EARTH-Monster aus deinem Deck deiner Hand hinzu. |
| **Mechination Spindle** | 1 | 800/400 | C | **Aus der Hand, 1×/Zug:** Schicke diese Karte auf den Friedhof; beschwöre 1 Level-1-MECHA-Monster aus deinem Friedhof. |
| **Mechination Ratchet** | 1 | 400/1200 | C | **Spezialbeschwörung:** Während du ein „Mechination"-Monster kontrollierst, aus der Hand in Verteidigung. **Bei Beschwörung:** Erhalte 1 Mana. |
| **Mechination Boltling** | 1 | 900/300 | U | **Zündung, 1×/Zug, 1 Mana:** Beschwöre 1 Level-1-Monster aus deiner Hand. |
| **Mechination Hammerhand** | 2 | 1700/1200 | C | **Bei Beschwörung:** Füge 1 Level-2-EARTH-Monster aus deinem Deck deiner Hand hinzu. |
| **Mechination Gearmaw** | 2 | 1500/1600 | U | **Spezialbeschwörung:** Während du 2+ „Mechination"-Monster kontrollierst, aus der Hand. |
| **Mechination Kilnwarden** | 2 | 1300/2000 | U | **Zündung, 1×/Zug:** Beschwöre 1 Level-1-EARTH-Monster aus deinem Friedhof. |
| **Mechination Pistonlord** | 3 | 2400/1800 | R | **Bei Beschwörung:** Zerstöre 1 gegnerisches Monster mit höchstens 1500 ATK. |
| **Mechination Overseer** | 3 | 2200/2200 | R | **Zündung, 1×/Zug, 2 Mana:** 1 MECHA-Monster, das du kontrollierst, darf diesen Zug ein zweites Mal angreifen. |

### Reliquarys

| Karte | Lv | ATK/DEF | Rar | Beschwörung | Effekt |
|---|---|---|---|---|---|
| **Mechination Assemblage** | 3 | 2500/2000 | R | Du kontrollierst 2+ Monster. **2 Mana.** | **Bei Beschwörung:** Füge 1 Level-2-EARTH-Monster aus dem Deck deiner Hand hinzu. |
| **Mechination Worldgear** | 3 | 3000/2600 | L | Du kontrollierst 3+ Monster, 5+ Karten im Friedhof. **3 Mana.** Zerstöre 1 anderes eigenes Monster. | **Bei Beschwörung:** Beschwöre bis zu 2 Level-1-EARTH-Monster aus deinem Friedhof. |

### Feld-Artefakt

| Karte | Rar | Effekt |
|---|---|---|
| **Mechination Assembly Line** | U | **Feld-Aura:** Deine MECHA-Monster können nicht durch gegnerische Karteneffekte zerstört werden. **Standby Phase:** Gib 1 Level-1-EARTH-Monster aus deinem Friedhof auf die Hand zurück. |

### Zauber

| Karte | Rar | Effekt |
|---|---|---|
| **Mechination Blueprint** | C | Füge 1 Level-1-EARTH-Monster aus deinem Deck deiner Hand hinzu. |
| **Mechination Recast** | C | **1 Mana:** Beschwöre 1 Level-1-Monster aus deinem Friedhof. |
| **Mechination Overdrive** | U | **Schnellzauber, 2 Mana:** Beschwöre 1 MECHA-Monster aus deiner Hand. |

### Kombo-Guide

**Linie 1 — „Zwei Karten werden fünf" (Zug 1, 3 Mana)**

1. **Cogwright** normal beschwören → sucht **Boltling** (Level 1 EARTH).
2. **Boltling** mit 1 Mana per Effekt aus der Hand → beschwört **Ratchet** aus der Hand.
   *Moment: Boltling muss erst selbst aufs Feld.* Also andersherum:
2. **Boltling** ist auf der Hand; **Ratchet** spezialbeschwören (Bedingung: du kontrollierst Cogwright).
3. **Ratchet** bei Beschwörung: **+1 Mana** (jetzt 4 verfügbar).
4. **Boltling** ist noch auf der Hand — **Spindle** aus der Hand abwerfen? Friedhof ist leer, geht nicht.
5. **Assemblage** beschwören (2+ Monster ✓, 2 Mana) → sucht **Hammerhand**.

Ergebnis Zug 1: Cogwright + Ratchet + Assemblage auf dem Feld, Hammerhand und
Boltling auf der Hand, 2 Mana übrig. **Kein Kartennachteil**, weil jede Suche
eine neue Karte nachliefert.

**Linie 2 — „Der Friedhof arbeitet" (ab Zug 3, 5 Mana)**

1. **Hammerhand** normal beschwören → sucht **Gearmaw** (Level 2 EARTH).
2. **Gearmaw** spezialbeschwören (2+ „Mechination" ✓ — Hammerhand plus was schon steht).
3. **Kilnwarden** liegt im Friedhof? Dann **Spindle** aus der Hand abwerfen →
   holt ein Level-1-MECHA zurück.
4. Jetzt 3+ Monster und genug Friedhof: **Worldgear** (3 Mana, zerstört 1 eigenes
   Monster als Kosten — nimm das schwächste) → **holt 2 Level-1 aus dem Friedhof zurück**.
   Die Kosten zahlen sich selbst zurück.

Ergebnis: 4 Monster, davon ein 3000er, aus einer Normalbeschwörung heraus.

**Linie 3 — „Zwei Angriffe" (Finisher)**

**Overseer** auf dem Feld + 2 Mana → ein MECHA greift zweimal an. Zusammen mit
**Worldgear** (3000 ATK) sind das 6000 Schaden in einer Battle Phase, wenn das
Feld leer ist. Deshalb kostet Overseer 2 Mana und ist auf MECHA beschränkt.

**Die Bremse:** Alles hängt an Level-1-Monstern im Friedhof. Zug 1 ist der
schwächste Zug des Decks — es gibt nichts zu recyceln. Wer die ersten beiden
Züge übersteht, gewinnt das Ressourcenspiel.

---

# 2 · SLEIGHTWIND — Wind / Demon

**8 Karten:** 4 Monster, 2 Zauber, 2 Reliquary
**Braucht Lücke A (`HandQuick`).**

Der Archetyp spielt fast ausschliesslich im **Zug des Gegners**. Die Monster
sind Handkarten, die man abwirft, um zu stören — und die Reliquarys holen sie
zurück. Der Friedhof ist hier kein Abfall, sondern das Lager.

### Monster

| Karte | Lv | ATK/DEF | Rar | Effekt |
|---|---|---|---|---|
| **Sleightwind Whisperer** | 1 | 700/700 | U | **Hand-Schnelleffekt, 1×/Zug:** Wirf diese Karte ab; 1 gegnerisches Monster kann diesen Zug nicht angreifen. |
| **Sleightwind Doubtbringer** | 1 | 1000/500 | R | **Hand-Schnelleffekt, 1×/Zug, 1 Mana:** Wirf diese Karte ab; annulliere die Effekte 1 Karte auf dem Feld bis Zugende. |
| **Sleightwind Maskbearer** | 2 | 1600/1400 | R | **Hand-Schnelleffekt, 1×/Zug, 2 Mana:** Wirf diese Karte ab; gib 1 Monster auf dem Feld auf die Hand seines Besitzers zurück. |
| **Sleightwind Thornmother** | 2 | 1400/1800 | U | **Hand-Schnelleffekt, 1×/Zug:** Wirf diese Karte ab; 1 gegnerisches Monster verliert 600 ATK. |

### Zauber

| Karte | Rar | Effekt |
|---|---|---|
| **Sleightwind Hush** | U | **Schnellzauber, 1 Mana:** Gib 1 Level-1-WIND-Monster aus deinem Friedhof auf die Hand zurück. |
| **Sleightwind Second Face** | R | Füge 1 Level-1-WIND-Monster aus deinem Deck deiner Hand hinzu; erhalte 1 Mana. |

### Reliquarys

| Karte | Lv | ATK/DEF | Rar | Beschwörung | Effekt |
|---|---|---|---|---|---|
| **Sleightwind Choir of Two** | 2 | 2100/1700 | R | 3+ Karten im Friedhof. **2 Mana.** | **Schnelleffekt, 1×/Zug, 1 Mana:** Gib 1 Karte aus deinem Friedhof auf die Hand zurück. |
| **Sleightwind the Unwitnessed** | 3 | 2800/2400 | L | 6+ Karten im Friedhof **und du kontrollierst kein Monster**. **3 Mana.** | **Bei Beschwörung:** Gib bis zu 2 Level-1-WIND-Monster aus deinem Friedhof auf die Hand zurück. **Schnelleffekt, 1×/Zug, 2 Mana:** Annulliere die Effekte 1 Karte auf dem Feld bis Zugende. |

### Kombo-Guide

**Der Kreislauf.** Du wirfst im Gegnerzug ab, um zu stören. In deinem Zug holt
**Choir of Two** eine der abgeworfenen Karten zurück. Netto verlierst du pro
Runde eine halbe Karte statt einer ganzen — dafür hat der Gegner zweimal nicht
das getan, was er wollte.

**Die Umkehr.** *The Unwitnessed* verlangt ein **leeres eigenes Feld**. Das ist
kein Nachteil, sondern die Pointe: Der Archetyp spielt ohnehin aus der Hand.
Wenn der Gegner dein Feld abgeräumt hat, ist die Bedingung erfüllt — er hat
sich die Karte selbst freigeschaltet, die zwei Handkarten zurückgibt und
danach jede Runde etwas annulliert.

**Reihenfolge im Gegnerzug:** erst *Whisperer* (gratis) auf das grösste Monster,
damit es nicht angreift. *Doubtbringer* erst, wenn wirklich ein Effekt kommt —
1 Mana im Gegnerzug ist Mana, das du in deinem Zug nicht mehr hast.

**Warum nur 8 Karten:** Handkarten-Störung skaliert schlecht. Mehr als vier
verschiedene Abwerfer, und man hat immer die falsche auf der Hand.

---

# 3 · KINDLEKIN — Fire / Beast

**10 Karten:** 6 Monster, 3 Reliquary, 1 Zauber

Sechs Level-1-Monster, die sich gegenseitig aufs Feld ziehen. Kein einziges
davon ist für sich stark — der Wert entsteht aus der Anzahl. Und die Anzahl ist
zugleich die Beschwörungs-Bedingung der drei Reliquarys.

### Monster (alle Level 1)

| Karte | ATK/DEF | Rar | Effekt |
|---|---|---|---|
| **Kindlekin Spark** | 800/200 | C | **Spezialbeschwörung:** Während du ein „Kindlekin"-Monster kontrollierst, aus der Hand. **Bei Beschwörung:** Erhalte 1 Mana. |
| **Kindlekin Ashling** | 500/1000 | C | **Spezialbeschwörung:** Während der Gegner 1+ Monster kontrolliert, aus der Hand in Verteidigung. |
| **Kindlekin Flickerpaw** | 1000/400 | U | **Bei Normalbeschwörung:** Füge 1 Level-1-FIRE-Monster aus deinem Deck deiner Hand hinzu. |
| **Kindlekin Emberwing** | 900/900 | U | **Spezialbeschwörung:** Während du 2+ „Kindlekin"-Monster kontrollierst, aus der Hand. **Zündung, 1×/Zug:** Beschwöre 1 Level-1-FIRE-Monster aus deinem Friedhof. |
| **Kindlekin Hearthnurse** | 300/1400 | U | **Zündung, 1×/Zug, 1 Mana:** Beschwöre 1 Level-1-FIRE-Monster aus deiner Hand. |
| **Kindlekin Pyrewhelp** | 1200/300 | R | **Spezialbeschwörung:** Während du ein „Kindlekin"-Monster kontrollierst, aus der Hand. **Wenn diese Karte zerstört wird:** Beschwöre 1 Level-1-FIRE-Monster aus deinem Friedhof. |

### Zauber

| Karte | Rar | Effekt |
|---|---|---|
| **Kindlekin Tinderfall** | C | Füge 1 Level-1-FIRE-Monster aus deinem Deck deiner Hand hinzu; erhalte 1 Mana. |

### Reliquarys — aufsteigender Aufwand

| Karte | Lv | ATK/DEF | Rar | Beschwörung | Effekt |
|---|---|---|---|---|---|
| **Kindlekin Pyre Warden** | 2 | 2000/1200 | U | Du kontrollierst 2+ Monster. **1 Mana.** | **Bei Beschwörung:** Beschwöre 1 Level-1-FIRE-Monster aus deinem Friedhof. |
| **Kindlekin Emberthrone** | 3 | 2400/2000 | R | Du kontrollierst 3+ Monster. **2 Mana.** | **Zündung, 1×/Zug, 1 Mana:** Beschwöre 1 Level-1-FIRE-Monster aus deinem Friedhof. |
| **Kindlekin, the Last Ember** | 3 | 3000/2200 | L | Du kontrollierst **4+ Monster** und 6+ Karten im Friedhof. **4 Mana.** Verbanne 2 Monster aus deinem Friedhof. | **Bei Beschwörung: Zerstöre alle Monster auf dem Feld ausser BEAST-Monstern.** |

### Kombo-Guide

**Der Schwarm (Zug 2, 4 Mana):**

1. **Flickerpaw** normal beschwören → sucht **Spark**.
2. **Spark** spezialbeschwören (Flickerpaw steht ✓) → **+1 Mana** (5 verfügbar).
3. **Emberwing** spezialbeschwören (2 „Kindlekin" ✓).
4. **Pyrewhelp** spezialbeschwören (Bedingung ✓).
   → **4 Monster ohne einen einzigen Tribut.**
5. **Hearthnurse** hätte noch 1 Mana gekostet — halte sie, du brauchst das Mana.

**Der Board Wipe (Zug 3–4):**

*The Last Ember* ist absichtlich die teuerste Karte im Set: 4 eigene Monster auf
dem Feld, 6 Karten im Friedhof, 4 Mana, plus 2 Monster aus dem Friedhof
verbannt. Das ist **die Belohnung dafür, dass der Schwarm schon steht** — nicht
ein Knopf, den man drückt, wenn es schlecht läuft.

Und der Clou: sie verschont **BEAST**. Dein ganzer Schwarm bleibt stehen, das
gegnerische Feld ist leer. Wenn du direkt danach angreifst, sind das vier bis
fünf Angriffe auf einen leeren Tisch.

**Der ehrliche Haken:** Spielt der Gegner selbst BEAST-Monster, überleben seine
auch. Das ist die eingebaute Schwäche und steht im Kartentext.

**Pyrewhelp als Versicherung:** Sie holt beim eigenen Tod ein Level 1 zurück.
Der Schwarm schrumpft dadurch nicht unter die Schwelle von 4 Monstern, wenn der
Gegner einzeln abräumt.

---

# 4 · MANACLE — Dark / Myth

**13 Karten:** 8 Monster, 2 Reliquary, 2 Zauber, 1 Artefakt
**Hälfte funktioniert heute, die andere braucht Lücke B.**

Der Archetyp gewinnt nicht über Schaden, sondern darüber, dass der Gegner seine
Karten nicht bezahlen kann. Jede Karte, die im Gegnerzug feuert, ist echtes
Denial. Jede Karte, die auf „nächste Runde" wirkt, braucht Lücke B.

**Ohne Lücke B** bleiben 7 der 13 Karten voll funktionsfähig — der Archetyp wäre
spielbar, aber halb so scharf.

### Monster

| Karte | Lv | ATK/DEF | Rar | Effekt | Braucht B? |
|---|---|---|---|---|---|
| **Manacle Tollkeeper** | 1 | 600/900 | C | **Bei Normalbeschwörung:** Der Gegner hat in seinem nächsten Zug 1 Mana weniger. | ja |
| **Manacle Gleaner** | 1 | 900/500 | C | **Zündung, 1×/Zug:** Erhalte 1 Mana. | nein |
| **Manacle Coinbiter** | 1 | 400/1100 | U | **Schnelleffekt, 1×/Zug:** Der Gegner verliert 1 Mana. | nein |
| **Manacle Debtwarden** | 2 | 1600/1300 | U | **Bei Beschwörung:** Der Gegner hat nächsten Zug 1 Mana weniger, du 1 Mana mehr. | ja |
| **Manacle Ledgerkeeper** | 2 | 1200/1900 | U | **Zündung, 1×/Zug, 1 Mana:** Füge 1 Level-1-DARK-Monster aus deinem Deck deiner Hand hinzu. | nein |
| **Manacle Usurer** | 2 | 1800/1000 | R | **Schnelleffekt, 1×/Zug, 1 Mana:** Der Gegner verliert 2 Mana. | nein |
| **Manacle Assessor** | 3 | 2300/1700 | R | **Bei Beschwörung:** Der Gegner verliert 2 Mana; du erhältst 1 Mana. | nein |
| **Manacle Bailiff** | 3 | 2000/2400 | R | **Bei Beschwörung:** Der Gegner hat nächsten Zug 2 Mana weniger. | ja |

### Zauber & Artefakt

| Karte | Rar | Effekt | Braucht B? |
|---|---|---|---|
| **Manacle Levy** | U | **Schnellzauber, 1 Mana:** Der Gegner verliert 2 Mana. | nein |
| **Manacle Reckoning** | R | **2 Mana:** Der Gegner hat in seinem nächsten Zug 3 Mana weniger. | ja |
| **Manacle Countinghouse** | R | **Feld-Artefakt.** Feld-Aura: Deine MYTH-Monster können nicht durch gegnerische Karteneffekte zerstört werden. **Standby Phase:** Erhalte 1 Mana. | nein |

### Reliquarys

| Karte | Lv | ATK/DEF | Rar | Beschwörung | Effekt | Braucht B? |
|---|---|---|---|---|---|---|
| **Manacle Debt Collector** | 2 | 2100/1600 | R | Du hast 5+ Mana verfügbar. **2 Mana.** | **Bei Beschwörung:** Der Gegner hat nächsten Zug 2 Mana weniger. | ja |
| **Manacle, the Final Ledger** | 3 | 2900/2500 | L | Du hast 7+ Mana verfügbar und 6+ Karten im Friedhof. **4 Mana.** | **Bei Beschwörung:** Gegner nächsten Zug 3 Mana weniger, du 2 mehr. **Schnelleffekt, 1×/Zug, 1 Mana:** Der Gegner verliert 1 Mana. | ja |

### Kombo-Guide

**Der Grundgedanke:** Der Gegner bekommt zu Zugbeginn sein volles Mana. Alles,
was du in **deinem** Zug abziehst, ist weg. Also feuerst du in **seinem** Zug —
nachdem er beschworen hat, bevor er den teuren Effekt bezahlt.

**Linie — „Er kann nicht antworten" (Zug 4, 6 Mana)**

1. In deinem Zug: **Ledgerkeeper** sucht ein Level 1, **Gleaner** gibt +1 Mana.
2. **Coinbiter** und **Usurer** stehen auf dem Feld — du gibst 3 Mana **nicht** aus.
3. Sein Zug, er hat 6 Mana: **Coinbiter** (−1), **Usurer** (1 Mana, −2),
   **Levy** aus der Hand (1 Mana, −2). Er steht bei **1 Mana**.
4. Sein 3-Mana-Reliquary liegt tot auf der Hand.

Du hast dafür 2 Mana bezahlt und drei Karten investiert. Das ist der Preis —
der Archetyp tauscht Kartenvorteil gegen Tempo-Verweigerung.

**Mit Lücke B kommt die zweite Ebene:** *Reckoning* und *the Final Ledger*
schneiden schon **vor** seinem Zug ab. Er beginnt mit 3 statt 6 Mana, und deine
Schnelleffekte nehmen ihm den Rest.

**Warum 7+ Mana als Bedingung für das Boss-Reliquary:** Es soll die Karte sein,
die man spielt, wenn man das Manaspiel bereits gewonnen hat — nicht die, mit der
man es gewinnt.

---

# 5 · SACRILEGION — Light / Dragon

**15 Karten:** 6 Monster, 5 Reliquary, 2 Zauber, 2 Artefakte
**Braucht Lücke C.**

Fünf Reliquarys, deren Beschwörung Monster von **beiden** Feldern verlangt. Die
Beschwörung *ist* das Removal — es gibt in diesem Archetyp keine einzige Karte,
die „zerstöre 1 Monster" sagt. Dafür kostet jede Beschwörung dich selbst etwas,
und die Monster sind darauf gebaut, dieses Material nachzuliefern.

### Monster

| Karte | Lv | ATK/DEF | Rar | Effekt |
|---|---|---|---|---|
| **Sacrilegion Acolyte** | 1 | 600/1000 | C | **Bei Normalbeschwörung:** Füge 1 Level-1-LIGHT-Monster aus deinem Deck deiner Hand hinzu. |
| **Sacrilegion Oathling** | 1 | 1000/600 | C | **Spezialbeschwörung:** Während du ein „Sacrilegion"-Monster kontrollierst, aus der Hand. |
| **Sacrilegion Pledgebearer** | 1 | 800/800 | U | **Aus der Hand, 1×/Zug:** Schicke diese Karte auf den Friedhof; beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Herald** | 2 | 1700/1300 | U | **Bei Beschwörung:** Füge 1 Level-2-LIGHT-Monster aus deinem Deck deiner Hand hinzu. |
| **Sacrilegion Vowkeeper** | 2 | 1500/1700 | R | **Zündung, 1×/Zug, 1 Mana:** Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Sanctifier** | 3 | 2400/2000 | R | **Bei Beschwörung:** 1 gegnerisches Monster kann diesen Zug nicht angreifen. |

### Artefakte

| Karte | Rar | Effekt |
|---|---|---|
| **Sacrilegion Covenant Stone** | R | **Feld-Artefakt.** Feld-Aura: Deine DRAGON-Monster können nicht durch gegnerische Karteneffekte zerstört werden. **Standby Phase:** Gib 1 Level-1-LIGHT-Monster aus deinem Friedhof auf die Hand zurück. |
| **Sacrilegion Binding Chain** | U | **Monster-Artefakt, +500 ATK / +500 DEF.** **Beim Anlegen:** 1 gegnerisches Monster kann diesen Zug nicht angreifen. |

### Zauber

| Karte | Rar | Effekt |
|---|---|---|
| **Sacrilegion Rite of Return** | U | Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof; erhalte 1 Mana. |
| **Sacrilegion Sworn Oath** | R | **Schnellzauber, 1 Mana:** Beschwöre 1 Level-1-LIGHT-Monster aus deiner Hand. |

### Reliquarys — die Sakramente

| Karte | Lv | ATK/DEF | Rar | Beschwörung | Effekt |
|---|---|---|---|---|---|
| **Sacrilegion First Sacrament** | 2 | 2000/1600 | U | **Tribut 1 eigenes + 1 gegnerisches Monster.** 2 Mana. | — |
| **Sacrilegion Second Sacrament** | 3 | 2500/2000 | R | **Tribut 2 eigene + 1 gegnerisches Monster.** 3 Mana. | **Bei Beschwörung:** Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Third Sacrament** | 3 | 2700/2300 | R | **Tribut 1 eigenes + 2 gegnerische Monster.** 4 Mana. | — |
| **Sacrilegion Broken Vow** | 3 | 2600/2600 | R | Du kontrollierst **kein** Monster, 5+ Karten im Friedhof. 3 Mana. | **Bei Beschwörung:** Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion, the Last Oath** | 3 | 3200/2800 | L | **Tribut 2 eigene + 1 gegnerisches Monster**, 8+ Karten im Friedhof. 5 Mana. | **Bei Beschwörung:** Beschwöre bis zu 2 Level-1-LIGHT-Monster aus deinem Friedhof. |

### Kombo-Guide

**Die Rechnung.** *Second Sacrament* frisst 2 eigene und 1 gegnerisches Monster
und gibt dir sofort 1 Level 1 zurück. Netto: du verlierst 2 Karten vom Feld,
bekommst 1 zurück und einen 2500er — der Gegner verliert 1 Monster ohne
Gegenleistung. Der Tausch geht auf, **wenn deine Tribute billig waren.**

Genau dafür sind die Level-1-Monster da: Oathling beschwört sich gratis,
Pledgebearer holt aus dem Friedhof zurück, Vowkeeper macht das jede Runde.
**Du fütterst nie etwas Wertvolles.**

**Linie — „Zwei weg, einer steht" (Zug 3, 5 Mana)**

1. **Acolyte** normal beschwören → sucht **Oathling**.
2. **Oathling** spezialbeschwören (Acolyte steht ✓). → 2 Monster.
3. **Second Sacrament** (3 Mana): Tribut Acolyte + Oathling + **1 gegnerisches Monster**.
4. Bei Beschwörung: hol **Oathling** aus dem Friedwof zurück.
   → Feld: 2500er Reliquary + Oathling. Gegnerfeld: **1 Monster weniger**.
5. Mit den letzten 2 Mana: **Sworn Oath** beschwört ein weiteres Level 1 aus der Hand
   → schon wieder Material für die nächste Runde.

**Linie — „Der doppelte Schnitt" (5 Mana, gegen ein volles Gegnerfeld)**

*Third Sacrament* nimmt **2 gegnerische** Monster und nur 1 eigenes. Das ist die
Karte gegen Schwarm-Decks — teuer (4 Mana) und ohne Zusatzeffekt, weil das
Removal selbst schon der Effekt ist.

**Linie — „Wenn alles weg ist"**

*Broken Vow* ist der einzige Sakrament ohne Tribut: leeres eigenes Feld, 5 Karten
im Friedhof. Sie ist die Antwort darauf, dass die anderen vier tote Karten sind,
wenn du kein Material hast — und sie stellt sofort ein Level 1 mit auf, sodass
der nächste Sakrament wieder bezahlbar wird.

**Die eingebaute Bremse:** Kein Sakrament kann beschworen werden, wenn der
Gegner **kein** Monster kontrolliert (ausser Broken Vow). Der Archetyp kann ein
leeres Feld nicht bestrafen — er ist eine Antwort, kein Anfang.

---

---

# Die Infused-Ebene

Die Tabellen oben zeigen, was eine Karte **umsonst** tut. Hier steht, was sie
zusätzlich kann, wenn du Mana dafür übrig hast. 38 der 61 Karten bekommen eine
Infused-Ebene, zusammen 48 Effekte.

**Zwei Arten, und der Unterschied ist wichtig:**

- **Standalone** — eine eigene Fähigkeit. Läuft neben allem anderen auf der
  Karte, kostet nur Mana.
- **Coupled** — ein *Upgrade* des Normal-Effekts darüber. Pro Zug nur **einer
  aus der Gruppe**: entweder das Original umsonst oder die teurere Fassung.
  Das ist die eigentliche Entscheidung, die diese Karten stellen.

**Neu und in diesem Set zum ersten Mal:** zehn Karten tragen **zwei**
Infused-Effekte — meist ein Coupled-Upgrade plus eine unabhängige Standalone.
Bis eben konnte die Engine das nicht: sie sperrte bei einer Aktivierung nur
*einen* gekoppelten Partner, ein zweiter wäre im selben Zug noch nutzbar
gewesen. Im bestehenden Set trägt keine einzige Karte mehr als einen
Infused-Effekt, deshalb ist es nie aufgefallen. Ist repariert.

Die betroffenen zehn sind unten mit **▲▲** markiert.

## 1 · Mechination

| Karte | Art | Mana | Infused-Effekt |
|---|---|---|---|
| **Mechination Cogwright** | Standalone | 2 | Beschwöre 1 Level-1-EARTH-Monster aus deiner Hand. |
| **Mechination Boltling** | Coupled | 3 | *Statt aus der Hand:* Beschwöre 1 Level-1-Monster aus deinem **Friedhof**. |
| **Mechination Hammerhand** | Standalone | 2 | Füge 1 Level-1-EARTH-Monster aus deinem Deck deiner Hand hinzu. |
| **Mechination Gearmaw** | Standalone | 2 | 1 gegnerisches Monster kann diesen Zug nicht angreifen. |
| **Mechination Kilnwarden** | Coupled | 2 | *Statt einem:* Beschwöre bis zu **2** Level-1-EARTH-Monster aus deinem Friedhof. |
| **Mechination Pistonlord** | Standalone | 3 | Zerstöre 1 gegnerisches Monster mit höchstens 2500 ATK. |
| **Mechination Overseer** ▲▲ | Coupled | 4 | *Statt einem:* **Zwei** MECHA-Monster dürfen je ein zweites Mal angreifen. |
| | Standalone | 2 | 1 MECHA-Monster erhält bis Zugende 600 ATK. |
| **Mechination Assemblage** | Standalone | 2 | Beschwöre 1 Level-1-EARTH-Monster aus deinem Friedhof. |
| **Mechination Worldgear** ▲▲ | Coupled | 3 | *Statt Level 1:* Beschwöre bis zu 2 **Level-2**-EARTH-Monster aus deinem Friedhof. |
| | Standalone | 3 | Zerstöre 1 gegnerische Karte auf dem Feld. |
| **Mechination Assembly Line** | Standalone | 2 | Füge 1 Level-1-EARTH-Monster aus deinem Deck deiner Hand hinzu. |

*Overseer ist die Karte, an der das Gruppen-Prinzip sichtbar wird: Du kannst
denselben Zug den Buff für 2 Mana zünden UND den Doppelangriff — aber nicht
den einfachen und den doppelten Angriff zusammen.*

## 2 · Sleightwind

| Karte | Art | Mana | Infused-Effekt |
|---|---|---|---|
| **Sleightwind Whisperer** | Coupled | 2 | *Statt „kann nicht angreifen":* Das Monster kann diesen Zug **weder angreifen noch die Position wechseln**. |
| **Sleightwind Doubtbringer** ▲▲ | Coupled | 3 | *Statt annullieren:* Annulliere die Effekte **und** senke die ATK des Ziels dauerhaft um 500. |
| | Standalone | 2 | Gib 1 Level-1-WIND-Monster aus deinem Friedhof auf die Hand zurück. |
| **Sleightwind Maskbearer** | Standalone | 3 | Verbanne 1 Karte aus dem gegnerischen Friedhof. |
| **Sleightwind Choir of Two** ▲▲ | Coupled | 3 | *Statt einer:* Gib **2** Karten aus deinem Friedhof auf die Hand zurück. |
| | Standalone | 2 | 1 gegnerisches Monster kann diesen Zug nicht angreifen. |
| **Sleightwind the Unwitnessed** ▲▲ | Coupled | 4 | *Statt annullieren:* Annulliere die Effekte **und** gib die Karte auf die Hand ihres Besitzers zurück. |
| | Standalone | 2 | Gib 1 Level-1-WIND-Monster aus deinem Friedhof auf die Hand zurück. |

*Bei den Handkarten hängt das Upgrade an derselben Abwurf-Kosten — du wirfst
dieselbe Karte, entscheidest aber, wie hart sie zuschlägt.*

## 3 · Kindlekin

| Karte | Art | Mana | Infused-Effekt |
|---|---|---|---|
| **Kindlekin Flickerpaw** | Standalone | 2 | Beschwöre 1 Level-1-FIRE-Monster aus deiner Hand. |
| **Kindlekin Emberwing** | Coupled | 2 | *Statt einem:* Beschwöre bis zu **2** Level-1-FIRE-Monster aus deinem Friedhof. |
| **Kindlekin Hearthnurse** | Coupled | 3 | *Statt aus der Hand:* Beschwöre 1 Level-1-FIRE-Monster aus deinem **Deck**. |
| **Kindlekin Pyrewhelp** | Standalone | 2 | 1 FIRE-Monster erhält bis Zugende 500 ATK. |
| **Kindlekin Pyre Warden** | Standalone | 2 | Füge 1 Level-1-FIRE-Monster aus deinem Deck deiner Hand hinzu. |
| **Kindlekin Emberthrone** ▲▲ | Coupled | 3 | *Statt einem:* Beschwöre bis zu **2** Level-1-FIRE-Monster aus deinem Friedhof. |
| | Standalone | 2 | Alle BEAST-Monster, die du kontrollierst, erhalten bis Zugende 300 ATK. |
| **Kindlekin, the Last Ember** | Standalone | 3 | Beschwöre bis zu 2 Level-1-FIRE-Monster aus deinem Friedhof. |

*Hearthnurse ist die wichtigste: das Coupled-Upgrade holt aus dem **Deck** statt
aus der Hand. Ein Mana mehr, und aus einem Extender wird ein Sucher.*

## 4 · Manacle

| Karte | Art | Mana | Infused-Effekt |
|---|---|---|---|
| **Manacle Coinbiter** | Coupled | 2 | *Statt 1 Mana jetzt:* Dem Gegner fehlen im **nächsten Zug 2** Mana. |
| **Manacle Ledgerkeeper** | Standalone | 2 | Der Gegner verliert 1 Mana; du erhältst 1 Mana. |
| **Manacle Usurer** ▲▲ | Coupled | 3 | *Statt 2 Mana jetzt:* Dem Gegner fehlen im **nächsten Zug 3** Mana. |
| | Standalone | 2 | Füge 1 Level-1-DARK-Monster aus deinem Deck deiner Hand hinzu. |
| **Manacle Assessor** | Standalone | 3 | Der Gegner verliert 2 Mana; du hast im nächsten Zug 2 Mana mehr. |
| **Manacle Bailiff** | Coupled | 3 | *Statt 2:* Dem Gegner fehlen im nächsten Zug **3** Mana, und du hast 1 mehr. |
| **Manacle Countinghouse** | Standalone | 2 | Der Gegner verliert 1 Mana. |
| **Manacle Debt Collector** | Standalone | 2 | Der Gegner verliert 2 Mana. |
| **Manacle, the Final Ledger** ▲▲ | Coupled | 5 | *Statt 3 und 2:* Dem Gegner fehlen im nächsten Zug **5** Mana. |
| | Standalone | 2 | Füge 1 Level-1-DARK-Monster aus deinem Deck deiner Hand hinzu. |

*Hier trägt das Coupled-Prinzip die ganze Fraktion: **jetzt** wenig klauen oder
**nächste Runde** viel. Sofort wirkt im Gegnerzug, der Übertrag schneidet ihm
den Zug ab, bevor er anfängt. Nie beides in derselben Runde.*

## 5 · Sacrilegion

| Karte | Art | Mana | Infused-Effekt |
|---|---|---|---|
| **Sacrilegion Acolyte** | Standalone | 2 | Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Herald** | Standalone | 2 | Füge 1 Level-1-LIGHT-Monster aus deinem Deck deiner Hand hinzu. |
| **Sacrilegion Vowkeeper** | Coupled | 3 | *Statt aus dem Friedhof:* Beschwöre 1 Level-1-LIGHT-Monster aus deinem **Deck**. |
| **Sacrilegion Sanctifier** ▲▲ | Coupled | 3 | *Statt „kann nicht angreifen":* Gib das Monster auf die Hand seines Besitzers zurück. |
| | Standalone | 2 | Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Covenant Stone** | Standalone | 2 | Beschwöre 1 Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Second Sacrament** | Coupled | 2 | *Statt einem:* Beschwöre bis zu **2** Level-1-LIGHT-Monster aus deinem Friedhof. |
| **Sacrilegion Broken Vow** | Standalone | 3 | Verbanne 1 Karte aus einem der beiden Friedhöfe. |
| **Sacrilegion, the Last Oath** ▲▲ | Coupled | 4 | *Statt Level 1:* Beschwöre bis zu 2 **Level-2**-LIGHT-Monster aus deinem Friedhof. |
| | Standalone | 3 | 1 gegnerisches Monster kann diesen Zug nicht angreifen. |

*Sanctifier ist das schärfste Upgrade im Set: aus „darf nicht angreifen" wird
echtes Removal auf die Hand — und weil es gekoppelt ist, verzichtest du dafür
auf den Gratis-Effekt.*

---

## Zusammenfassung Seltenheiten

| Archetype | Common | Uncommon | Rare | Legendary |
|---|---|---|---|---|
| Mechination | 4 | 5 | 5 | 1 |
| Sleightwind | 0 | 3 | 4 | 1 |
| Kindlekin | 3 | 4 | 2 | 1 |
| Manacle | 2 | 5 | 5 | 1 |
| Sacrilegion | 2 | 5 | 7 | 1 |
| **Summe** | **11** | **22** | **23** | **5** |

## Was als Nächstes entschieden werden muss

1. **Lücken A, B, C bauen — ja oder nein?** Ohne A ist Archetype 2 kein
   Hand-Archetyp. Ohne C ist Archetype 5 kein Removal-Archetyp. Ohne B ist
   Archetype 4 halb so scharf, aber spielbar.
2. **Namen und Zahlen** — alles oben ist Vorschlag, nichts ist gesetzt.
3. **Artworks:** 61 Stück. Prompts kommen von mir, sobald das Design steht.
