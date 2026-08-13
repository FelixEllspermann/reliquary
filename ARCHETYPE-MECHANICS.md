# Archetype-Mechanics — Überblick

Was jeder Archetype SPIELT: Gameplan und Kern-Mechaniken, gezogen aus den
echten Kartendaten (cards-full.json, Stand 569 Karten). Das Gegenstück zum
ARCHETYPE-ARTSTYLE-GUIDE — der beschreibt, wie die Familien aussehen, dieses
Dokument beschreibt, wie sie funktionieren.

**Wiederkehrende System-Begriffe:**
- **Infused** — Mana-Upgrade eines Effekts (stärkere Version desselben Bausteins).
- **Charged** — gesetzter Quick-Spell zündet automatisch in der eigenen Standby
  Phase mit verstärktem Effekt (Slowburn-Signatur).
- **HandQuick** — Karte antwortet direkt aus der Hand im Reaktionsfenster.
- **Mill** — Karten vom eigenen Deck in den Friedhof.
- **Bounce** — Karte zurück auf die Hand.
- **Facedown/Flip** — verdeckt legen bzw. aufdecken (mit FLIP-Effekten).
- **Mana-Übertrag** — „X more/less Mana during the next turn".

---

## Apocrypha — LIGHT · Myth (8 Karten)
**Gameplan:** Mythen-Toolbox — für jede Lage eine Antwort, gekrönt von Ketten-Kontrolle.
- Flexible Einzel-Antworten: Bounce (Roc), Stat-Kopie (Chimera), Hand-Peek + gezielter Discard (Sphinx)
- Wiederkehr: Hydra revived beim Tod „Apocrypha"-Monster aus dem Grab
- Banish als Ressource: Cartographer zieht über Grab-Banish, ATK pro verbannter Karte
- Krone: **the Unwritten negiert alle bisherigen Ketten-Glieder** (NegateRestOfChain)

## Archfiend — DARK · Demon (11 Karten)
**Gameplan:** Removal-Aggro — der Hof richtet hin, und der eigene Hofstaat ist verbrauchbar.
- Zerstörung mit ATK-Obergrenzen auf Summon und Ignition (Hatchet Man, Overlord, Pistonlord-artig gestaffelt)
- Permanente ATK-Debuffs (Torturer, Crown-Standby)
- Eigenes Opfern als Ressource: Devil's Bargain (destroy own → Draw 2 + Mana)
- Grab-Recursion, teils **face-down** zurück aufs Feld (Devil's Advocate)
- Antwort-Removal auf gegnerische Summons (Warden)

## Barrierstruck — EARTH · Mecha (12 Karten)
**Gameplan:** Schildwall — Artefakte aus dem Deck aufs Feld stapeln und dahinter überleben.
- Artefakt-Platzierung direkt aus dem Deck (Shieldbearer, Aegis Colossus, Sanctum Colossus bis zu 2)
- **Schutz-Artefakte sterben stellvertretend** (redirectDestructionToSelf: Bulwark Prism, Aegis Fragment) und bringen beim Tod Nachschub/Summons
- Hohe DEF-Statlines + DEF-Buffs (permanent via Load-Bearing Wall, temporär via Reactive Plating/Cold Shoulder)
- Peacekeeper: kann nie angreifen, bufft die anderen, dreht Gegner in Defense

## Deathpoem — FIRE · Human (8 Karten)
**Gameplan:** Selbstmord-Removal — die eigenen Monster opfern sich für Zerstörung und füllen das Grab.
- Signatur: **„Tribute this card: destroy …"** in Stufen (Initiate ≤1200, Duelist ≤2000, Housebane frei + S/A-Bounce)
- Sterben lohnt: Calligrapher sucht beim Grab-Gang nach
- Grab-Recursion (Vow, Duelist aus dem Grab)
- Reliquaries skalieren mit vollem Friedhof (Unsigned Verse: +100 ATK pro Grab-Karte; Hundredth Stanza räumt bis zu 3 ab)

## Deckay — DARK · Animal/Demon (13 Karten)
**Gameplan:** Selbst-Mill als Motor — das eigene Deck verrottet planvoll, jeder gemillte Körper arbeitet.
- End-Phase-Mills auf fast jedem Monster (teils in JEDER End Phase)
- **OnMilledSelf-Trigger**: gemillt = Suche/Summon/Recycle (Maggot, Leech, Moth, Broodmother)
- Grab-Banish als Kosten (Worm-Selbstrevive, Moth-Facedown-Dreher)
- Anti-Reliquary-Tech aus der Hand: Fiend negiert Reliquary-Effekte, Vulture beschwört ein KONTER-Reliquary aus dem Extra Deck
- King of Deckay: Burn pro Mill, 10er-Mill beim Summon, Feld-Nuke

## Dragon Shrine — LIGHT · Dragon (14 Karten)
**Gameplan:** Such- und Leiter-Schwarm — kleine Drachen rufen größere, der Schrein liefert nach.
- Dichte Suche (Petitioner, Diactor, Dragon Sceptre, Elder Wyrm nach Spells)
- Leitern: Baby Dragon tauscht sich in der End Phase gegen Lv2-Drachen; Standby-Nachschub (Heart of the Shrine, Shrinekeeper)
- Stand-In-Artefakt **zählt als „Dragon Shrine"-Monster** für Bedingungen
- Maiden setzt Drachen-Spells aus dem Deck (sofort aktivierbar) und kann das Feld von Nicht-Drachen säubern
- Wyrm Eternal: Revive + Rudel-Buff als Abschluss

## Failsafe — EARTH · Human/Artefakte (7 Karten)
**Gameplan:** Antwort-Kette mit Auto-Nachschub — fällt eine Sicherung, rastet die nächste ein.
- Feld-Artefakte als einmalige **Quick-Antworten, die sich selbst ersetzen**: Effekt → ab ins Grab → nächstes Failsafe aus dem Deck setzen (Bulkhead: kein Kampfschaden, Damper: −500 ATK, Seal: Monster-Negate)
- Setter: Tinker/Chief Engineer/Raise the Failsafes holen Artefakte aus dem Deck
- Carrier recycelt aus dem Grab; Chief Engineer skaliert mit Artefakt-Zahl

## Fethaerbreese — WIND · Animal (12 Karten)
**Gameplan:** Bounce-Tempo mit den eigenen Vögeln — was heimkehrt, zieht Karten und triggert erneut.
- **Selbst-Bounce als Ressource**: Flight Risk/Two-in-the-Bush (zurück auf Hand → Draw), Nest Egg belohnt jeden Heimflug mit einem Draw
- On-Summon-Trigger werden durch Re-Summons wiederverwertet (Fledgling/Hollowbone suchen)
- Grab-Rückholung in die Hand (Homing Instinct, +Mana-Variante)
- Störung: Nightjar dreht Gegner facedown, Nightmother/Held Breath negieren per Quick
- Two-in-the-Bush: Doppelangriff bei zweitem WIND-Monster

## Forgeheart — FIRE · Mecha (12 Karten)
**Gameplan:** Artefakt-Ökonomie — die Schmiede macht Stats permanent und Schrott zu Karten.
- Artefakt-Suche und Direkt-Platzierung (Apprentice-Piece, Bellows, Worldanvil)
- **Permanente Buffs** als Signatur (Quench-Infused, Anvilborn, Heart of the Forge)
- Schrott-Verwertung: Scrap Deal (Artefakt zerstören → Draw 2 + Mana), Spare Parts recycelt aus dem Grab
- Hammer als Equip: opfern für permanenten −500-Debuff
- Ironclad Argument: unzerstörbar im Kampf bei 2+ Artefakten, ATK pro Artefakt

## Gaslight — DARK · Myth (8 Karten)
**Gameplan:** Feld-Sabotage — dem Gegner 0/0-Trugbilder ins Feld stellen und sie dann kassieren.
- Signatur: **Illusion Tokens (0/0) aufs GEGNER-Feld** (Lanternist, Usher, Mirrorwalk als Summon-Antwort)
- Grand Premiere füllt ALLE freien Gegner-Zonen — komplette Zonen-Blockade
- Token-Verwertung: Mesmer/Curtain Call fressen Tokens für Draws, Premiere-Debuff pro Token
- Charlatan skaliert ATK mit Token-Zahl; Standing Ovation braucht ein Token als Summon-Bedingung

## Genostitched — DARK · Mecha (11 Karten)
**Gameplan:** Ausrüstungs-Voltron — Artefakte annähen und nur ausgerüstet ist man jemand.
- Beim Summon equippen aus Hand/Grab/Deck (Grafter, Hand-Me-Down, Prime Specimen)
- **„While equipped"-Gating**: Extra-Angriff (Dressed to Kill), Removal (Vivisector, Prime Specimen), Draws (Scrapling), permanente Buffs
- Dressed to Kill: +400 ATK pro eigenem Equip; Third Arm zieht bei Battle-Kills des Trägers
- Quick Change verschiebt Equips auf das stärkste Monster mitten im Zug
- Apex Chimera equippt sich beim Summon selbst aus dem Grab

## Gravemaw — DARK · Demon/Beast (11 Karten)
**Gameplan:** Grab/Banish-Kreislauf — erst füttern, dann fressen, nichts bleibt liegen.
- Selbst-Mill als Auftakt (Nibbler), Selbstfraß für Value (Butcher/Tyrant: eigenes Monster zerstören → Draw/Burn)
- **Banish als Kosten und Skalierung**: Bonepicker +300 ATK pro verbannter Karte, Bottomless verlangt 4 Banishes
- Rückführung Banish→Grab (Leftovers, Bonepicker-Ignition) — der Kreis schließt sich
- Revives aus dem Grab, auch face-down (Cold Storage, Ossuary)
- Gegner-Grab-Hate: Stolen Supper banisht bis zu 3

## Heavenly — LIGHT · Angel (12 Karten)
**Gameplan:** Wächter-Midrange — suchen, beschützen, wiederauferstehen.
- Dichte Suche (Acolyte, Errand Angel, Heavenly Reliquary-Spell)
- **Protection-Paket**: Bodyguard hat Taunt UND macht Seraph Sovereign untargetbar
- Schwarm aus der Hand (Ascension, Choirmaid aus dem Deck)
- Grab-Recursion (Herald, Second Coming mit permanentem Buff, Empyrean beim Summon)
- Seraph Sovereign: Quick-Negate; Intervention: Bounce oder Banish als Removal; LP-Nadeln (Collection Plate)

## Hexweaver — DARK · Human (12 Karten)
**Gameplan:** Zauber-Weberei — Spells aus dem Deck setzen, aus dem Grab zurückweben, mit Mana-Vorteil bezahlen.
- Signatur: **„Set 1 Hexweaver Spell from your Deck"** (Apprentice, Grand Magus, Loomguard als Summon-Antwort, Woven Fate)
- Spell-Recursion aus dem Grab (Rethread, Scribe, Loom of Fate bis zu 2)
- Mana-Rampe: Loose Thread (+Mana, auch übertragen), Bargain Bobbin (erster Spell pro Zug −1)
- Störung: Woven Fate (−600 ATK), Unravel (S/A-Bounce oder -Banish), Looming Large (Bounce + Draws bei 6+ Mana)

## Kindlekin — FIRE · Beast (15 Karten)
**Gameplan:** Weenie-Schwarm — endloser Lv1-FIRE-Nachschub aus Hand, Deck und Grab.
- Summon-Ketten: Plus-One (bis zu 2 aus der Hand), Hearthnurse (aus Hand/Deck), Emberwing/Pyre Warden/Emberthrone (aus dem Grab)
- Suche + Mana: Flickerpaw, Tinderfall (+2 Mana), Spark (+1 Mana), Sift the Ashes (Mill-4-Suche)
- Sterben ist Nachschub: Pyrewhelp revived beim Tod, Warm Memories heilt pro Tod
- Rudel-Pumpen (Fire Marshal-Aura auf Lv1, Emberthrone-Massen-Buff)
- the Last Ember: zerstört alles außer BEAST — der Schwarm überlebt den eigenen Sturm

## Lightless — DARK · Human (13 Karten)
**Gameplan:** Verdeckt-Spiel auf BEIDEN Feldern — eigene Karten schlafen sicher, gegnerische werden schlafen gelegt.
- Eigene facedown legen als Value (Closed Casket bis zu 3, Veil/Snuff mit Draw/Mana-Drain)
- Flip-Payoffs: Light-Fingered bounct S/A beim Flip, Lights-Out setzt Gegner facedown + ATK pro Gegner-Facedown
- **Gegner facedown drehen** als Removal-Ersatz (Matriarch, Prophet, Acolyte-Grab-Effekt) und dann Facedowns zerstören (Shade, Matriarch + Burn)
- Blackout Curtain: DEF-Aura nur für Verdeckte + setzt aus der Hand nach
- Ritual: Revive aus Grab/Banish + Random-Discard oder sogar Monster-KLAU aus der Gegner-Hand; Umbra braucht LP-Rückstand und banisht

## Lyria — LIGHT · Human (11 Karten)
**Gameplan:** Flip-Theater — die eigene Truppe geht hinter den Vorhang und tritt mit Effekt wieder auf.
- FLIP-Effekte als Kern: Chimewisp sucht, Drummer debufft permanent, Flautist dreht Gegner um, Harpist kettensummont verdeckt
- Selbst wieder verdecken (Curtain Call, Grand Conductor) — Flips sind wiederholbar
- Setzen aus Hand und Grab (Hushabye, Second Movement)
- **Green Room**: eigene Facedowns sind kein Angriffsziel, solange eine Lyria offen liegt + revived verdeckt
- Quiet Crescendo: ATK pro eigenem Facedown (Infused permanent); Final Overture flippt alles auf einmal

## Manacle — DARK · Myth (18 Karten)
**Gameplan:** Steuer-Kontrolle — die Ressource des Gegners besteuern, die eigene verzinsen.
- Signatur doppelgleisig: **„loses X Mana" sofort** (Levy, Coinbiter, Usurer) und **„X less Mana next turn"** (Bailiff, Reckoning, Hidden Fees)
- Eigener Zins: Compound Interest (+3 nächsten Zug), Countinghouse (Standby +1), Debtwarden/Assessor nehmen und geben
- Payoffs bei vollem Konto: Silver Spoon zieht ab 5+/8+ Mana, Reliquaries verlangen 5+/7+ Mana
- Teures Premium-Removal (Buyout: Bounce/Banish + Draw)
- Final Ledger: −3/−5 Mana-Schläge — das Endspiel der Pfändung

## Mechination — EARTH · Mecha (20 Karten)
**Gameplan:** Fabrik-Value — Suche nach Bauteil-Level, Wiederverwertung, Doppelschichten (größter Archetype).
- Level-sortierte Suche (Lv1/Lv2 EARTH: Cogwright, Hammerhand, Blueprint, Assembly Line)
- Schwarm aus Hand und Grab (Boltling, Kilnwarden bis zu 2, Overdrive, Recast, Spindle)
- **Doppelangriffe** (Overseer: bis zu 2 MECHA schlagen zweimal) + permanente Stat-Schrauben (Jumpstart, Night Shift, Crumple Zone)
- Removal (Pistonlord gestaffelt), Selbst-Bounce für Value (Recall Notice), Artefakt-Trade-In
- Worldgear: Massen-Revive + „destroy 1 card" — die Fabrik als Endboss

## Mimicrypt — DARK · Demon (8 Karten)
**Gameplan:** Alles geliehen — Stats, Karten und Monster des Gegners benutzen.
- Stat-Kopie (Forgery, Palimpsest, Borrowed King — 1000/1000-Boss, der sich verkleidet)
- **Kontrolle stehlen**: Archivist übernimmt ein Gegner-Monster bis zur End Phase; Borrowed King revived direkt aus dem GEGNER-Grab aufs eigene Feld
- Understudy spielt Spells aus dem Gegner-Grab nach
- Encore: temporäre Kopie eines Gegner-Monsters
- Summon-Bedingungen zählen das GEGNER-Grab (6+/8+); Ghoul/Palimpsest banishen es leer

## Paperbound — DARK · Human (10 Karten)
**Gameplan:** Verwaltungs-Lockdown — niemand greift an, niemand wird beschworen, alles liegt in Defense.
- **Angriffssperren** überall („cannot attack": Red Tape, File Clerk, Waiting Room, Commissioner-Quick)
- Positionszwang: alles in Defense drehen + Position einfrieren (In Triplicate, Rubber Stamp, Commissioner)
- Facedown-Drehen als Entwaffnung (Lost Form 27-B)
- **Special-Summon-Verbote** (Office Hours, Waiting Room-Infused)
- Negates: Auditor gratis einmal pro Zug, Final Rejection als Quick — der Antrag ist abgelehnt

## Powderkeg — FIRE · Mecha (12 Karten)
**Gameplan:** Artefakte sind Munition — laden, zünden, nachladen.
- Signatur: **eigene Artefakte zerstören als Kosten für Removal** — teils OHNE Once-per-turn-Limit („ammunition is the limit": Cannoneer, Point-Blank, Last Salvo)
- Munitions-Artefakte mit Explosions-Payoffs (Magazine: Draw beim Tod, Shellcrate: Mana, Blastplate: Buff)
- Nachschub aus Deck und Grab (Loader, Quartermaster bis zu 2, Brass Sweep, Last Salvo-Ignition)
- Sparkplug/Misfire: Artefakt → Draws; First Spark: Artefakt → Negate
- Quartermaster: ATK pro Artefakt

## Redactor — DARK · Human (11 Karten)
**Gameplan:** Zensur-Bestrafung — jeder Extra-Draw des Gegners kostet ihn etwas.
- Signatur-Trigger **OnOpponentDraw** (Draws außerhalb der Draw Phase): Debuff (Blackbar), Deck-Mill (Censor), Random-Discard (Minister, Final Edition), Mana-Entzug (Ministry Seal), eigene Draws (Archivist), ATK-Wachstum (Inkling)
- **Mandatory Reading schenkt dem Gegner einen Draw** — und zündet damit das eigene Straf-Netz
- Gegner-Deck direkt millen (Burn Before Reading) + Gegner-Grab banishen (Blackbar, Final Edition bis zu 3)
- Facedown-Dreher mit Positionssperre (Classified); eigener Draw-Filter (Freedom of Information)

## Sacrilegion — LIGHT · Dragon (20 Karten)
**Gameplan:** Tribut-Maschine — Opfer sind Währung, und auch der Gegner zahlt mit.
- Signatur: Reliquaries mit **Tributen von BEIDEN Feldern** als Summon-Kosten (First/Second/Third Sacrament, Last Oath) — die Beschwörung selbst ist das Removal
- Tribut-Payoffs: Willing Lamb zieht beim Geopfert-Werden (und legt sich aus dem Grab wieder hin), Blood Dividend heilt/zieht, Twice-Blessed zählt als 2 Tribute
- Lv1-LIGHT-Revive-Loop (Vowkeeper, Covenant Stone, Rite of Return mit +2 Mana, Pledgebearer)
- Severance: eigenes Monster → +Mana/Draw; Broken Vow spielt aus dem leeren Feld heraus
- Removal über Cannot-Attack + Bounce (Sanctifier)

## Sleightwind — WIND · Demon (13 Karten)
**Gameplan:** Ärmel-Tricks — Monster aus der HAND abwerfen und als Quick-Antwort verschwinden lassen.
- Signatur: **HandQuick-Discards** — Doubtbringer (Negate), Maskbearer (Bounce), Thornmother (−600 permanent), Whisperer (Angriffssperre), Ace Up the Sleeve (Gegner-Bounce)
- Hand-Vergleich: Card Counter zieht bis zur Gegner-Handgröße auf
- Grab-Recursion in die Hand (Hush, Choir of Two, the Unwitnessed)
- Loot-Filter (Marked Deck, Nothing to See mit Selbst-Bounce eines Facedowns)
- the Unwitnessed: Quick-Negate + Bounce, spielt aus dem leeren Feld

## Slowburn — FIRE · Human (8 Karten)
**Gameplan:** Zünder legen — schwach sofort oder stark mit Verzögerung.
- Signatur **Charged**: gesetzte Quick-Spells zünden in der eigenen Standby Phase automatisch verstärkt — Banked Flame (Draw 1 → Draw 2 + 2 Mana), Deep Coals (−400 ATK → Destroy 2), Tripwire (1 in Defense → ALLE in Defense)
- Vorzünden: Pyrekeeper/Patient Flame lösen Charged-Effekte gegen Mana früher aus
- Setter aus dem Deck (Candlewick bis zu 2, Patient Flame beim Summon)
- Recursion (Chandler) + Anti-Special-Summon-Quick; Backdraft belohnt Unterzahl mit Mana

## Snugglet — bunt · Beast/Animal (12 Karten)
**Gameplan:** Kuschel-Kreis — jedes Snugglet stärkt ein bestimmtes anderes, zu dritt sind sie eine Festung.
- Signatur: **Buddy-Auren im Kreis** (Acorn→Bumble→Mopsy→Pebble→Whiskers→Puddle→Acorn) — die richtige Sitzordnung ist das Deckbau-Puzzle
- **Feld-Limit 3**: mehr als 3 Snugglets sind nicht legbar; die Reliquaries verlangen genau diese 3 + Tribute
- Zähigkeit: Pebble-Taunt, Nap Time/Blanket Fortress (unzerstörbar + Targeting-Schutz)
- Schwarm + Revive (Pile-Up, Sofa, Cuddlepile, Whole Squish bis zu 2 aus dem Grab)
- Kleine Nadeln: Whiskers-Debuff, Puddle-Draws, Mopsy-Mana

## Tidebound — WATER · Myth (12 Karten)
**Gameplan:** Die Flut nimmt alles zurück — Bounce als Removal UND als eigener Antrieb.
- Gegner bouncen statt zerstören (Undertow, Wave Goodbye, Leviathan, Returning Sea)
- **Selbst-Bounce für Value**: Ebb and Flow (+Draws), Backwash (+Draw), Skimmer nimmt sich selbst mit — On-Summon-Trigger laufen erneut
- Bounce-Payoffs: Beachcomber gewinnt Mana pro Gegner-Bounce
- Grab-Rückholung (Message in a Bottle, auch als Facedown-Revive)
- Mana-Nadeln gegen den Gegner (Skimmer, Current-Caller); Mirrorshell bestraft Summons

## Trapline — EARTH · Human (13 Karten)
**Gameplan:** Fallen-Ketten — gesetzte Quick-Spells zünden NUR im richtigen Fenster, und jede Falle legt die nächste.
- Signatur-Fenster: **AttackResponse** (Bear Hug cancelt den Angriff per Facedown-Dreher, Tripwire −800, Decoy-Schutz, Row of Teeth zerstört ALLE Angriffs-Monster) und **SummonResponse** (Pitfall bounct, Warm Welcome zerstört + Level-AoE)
- Ketten-Signatur: fast jede Falle endet mit **„then you may Set 1 Trapline from your hand"**
- Setter aus dem Deck (Basecamp-Standby, Warden — sofort scharf)
- Recycling (Double Back, Season of Snares); Patient Jaw setzt als Quick nach und removed beim Summon

## Wyldpack — WIND · Beast (12 Karten)
**Gameplan:** Rudel-Schwarm — einer ruft den nächsten, dann beißen alle gleichzeitig.
- Kettensummons aus der Hand (Howler, Matriarch, Alpha holt bis zu 2 wenn selbst special summoned)
- Suche (Cub, Call of the Wyld mit Sofort-Summon, Matriarch-Ignition)
- **Rudel-weite ATK-Pumps** (Hackles bis zu 5, Off the Leash permanent!, Top Dog permanent einzeln)
- Extra-Angriffe (Alpha-Ignition, Ur-Alpha beim Summon)
- Underdog belohnt Unterzahl mit Draws; Fetch recycelt aus dem Grab

---

## Dark Angel — DARK · Angel (Familie, kein Namens-Archetype)
**Gameplan:** Gefallene Boss-Engel mit Anti-Reliquary-Kontrolle (3 Monster + 5 Support-Karten aus demselben Paket).
- The Fallen One: blockt ALLE Reliquary-Summons solange er liegt; schickt Reliquaries zurück ins Extra Deck; Infused-Lock auf Special-Summon-Effekte
- Immortal Demon: unzerstörbar/untargetbar/kein Kampfschaden — aber Death Counter in jeder End Phase (bei 4 ins Grab); beim Grab-Gang Draw 2 + Negate
- The Last Asemir: leitet allen eigenen Kampfschaden auf den Gegner um; zwingt beim Summon alle Monster in den Angriff; bringt beim Extra-Deck→Grab-Gang alle verbannten Karten zurück
- Support: The Forbidden Name (Namens-Sperre), Yard Sentence (Deck→Grab), Implosion (Banish-AoE), Exponential Deterioration (Gegner-Mill-Verdopplung), Rally the Weak (Vanilla-Schwarm)
