# Archetype-Artstyle-Guide

Die Stil-Bibel für alle Karten-Artworks. **Jeder neue Artwork-Prompt wird aus
diesem Dokument gebaut** — so bleiben die Archetypes über Batches und Monate
hinweg konsistent. Wer einen Archetype ergänzt, ergänzt hier seine Sektion.

## So wird ein Prompt gebaut

```
[Motiv der Karte] + [Prompt-Baustein des Archetypes] + [Qualitäts-Block]
```

- **Motiv der Karte:** Was die Karte TUT, als Bild (aus Name + Effekt). `solo,`
  voranstellen; `no humans,` wenn kein Mensch im Bild ist.
- **Prompt-Baustein:** unten je Archetype — Palette, Licht, Requisiten. Immer
  komplett übernehmen, nicht kürzen: er IST die Konsistenz.
- **Qualitäts-Block (ans Ende, immer gleich):**
  `dark fantasy, masterpiece, best quality, very aesthetic, absurdres, newest`

**Modell & Settings:** Animagine XL 3.1 (civitai.com/models/260267) ·
Euler a · CFG 5–7 · 26–28 Steps · **1216×832 (quer)**.

**Negativ-Prompt (für alle identisch):**
```
nsfw, lowres, (bad), text, error, fewer, extra, missing, worst quality, jpeg artifacts, low quality, watermark, unfinished, displeasing, oldest, early, chromatic aberration, signature, extra digits, artistic error, username, scan, [abstract]
```

**Ablage:** PNG exakt nach Kartennamen benennen (`Tidebound Leviathan.png`) →
`Assets/_Game/Art` → Unity-Menü **Rouge → Card Design → Artworks automatisch
zuweisen**. Vorhandene Bilder werden nie überschrieben.

## Globales Fundament (gilt für JEDE Karte)

Reliquary ist gemalte Dark Fantasy: sattes, dramatisches Licht auf tiefem
Grund, ein zentriertes Hauptmotiv, ornamentale Details statt Fotorealismus.
Kein Text, kein Rahmen, kein Logo im Bild — der Kartenrahmen kommt vom Spiel.
Monster sind Porträts eines Wesens, Spells/Artefakte zeigen den MOMENT der
Wirkung oder das Objekt selbst. Humor ist erlaubt (Snugglet, Paperbound),
aber immer ernsthaft gemalt — die Welt nimmt sich selbst ernst, nicht der Witz.

---

## Apocrypha — LIGHT · Myth (8 Karten)

Mythen, die aus den Büchern gestrichen wurden. Sphinx, Hydra, Chimera
existieren nur noch zwischen den Zeilen — halb Wesen, halb Manuskript. Jedes
Wesen trägt Spuren von Papier, Tinte oder Randnotizen.

- **Palette:** warmes Licht-Gold und Elfenbein auf tiefem Blau-Schwarz
- **Motive:** schwebende Buchseiten, zerreißendes Pergament, glühende Schrift,
  die sich auflöst; Kreaturen mit Papier-Texturen in Fell und Flügeln
- **Baustein:**
```
warm golden light and ivory on deep blue-black, floating torn manuscript pages, glowing dissolving script, creature partly made of parchment and ink, mythic library ruins
```

## Archfiend — DARK · Demon (11 Karten)

Der höllische Adel: Dämonen mit Kronen, Verträgen und Hofstaat. Sie zerstören
nicht aus Wut, sondern per Dekret — jede Hinrichtung ist ein Verwaltungsakt
des Throns.

- **Palette:** Glutrot und Schwarz mit gierigem Gold
- **Motive:** Thronsäle aus Obsidian, brennende Siegel und Verträge, Kronen,
  Kettenhunde des Hofes, Lava-Adern im Boden
- **Baustein:**
```
ember red and black with greedy gold accents, obsidian throne hall, burning contracts and royal seals, infernal nobility, lava veins glowing in dark stone
```

## Barrierstruck — EARTH · Mecha (12 Karten)

Wandelnde Bollwerke: Konstrukte, die aus Mauern, Schilden und Prismen gebaut
wurden. Sie greifen selten an — sie stehen, halten und antworten.

- **Palette:** Steingrau und Bernstein, dazu Kristall-Cyan aus Prismenkernen
- **Motive:** meterdicke Schildplatten, leuchtende Barriere-Felder, Prismen
  und Kristallkerne, Festungsruinen, Runen auf Stein
- **Baustein:**
```
stone grey and amber with crystal cyan barrier light, massive shield plates, glowing prism core, rune-carved fortress walls, immovable guardian construct
```

## Deathpoem — FIRE · Human (8 Karten)

Feudales Japan in Glut und Tusche. Samurai schreiben vor dem Tod ihr Jisei —
das Todesgedicht — und fallen dann von eigener Hand, um etwas mitzureißen.
Jede Karte trägt irgendwo Kalligraphie.

- **Palette:** tiefes Schwarz und Tusche-Weiß, Feuer-Orange und Glutrot
- **Motive:** brennende Schriftrollen, glühende Schriftzeichen, Asche und
  verkohlte Kirschblüten in der Luft, Katana, letzte Haltung
- **Baustein:**
```
deep black and ink white with fire orange, burning calligraphy scrolls, glowing kanji, ash and charred cherry blossoms drifting, feudal japanese samurai aesthetic
```

## Deckay — DARK · Animal/Demon (13 Karten)

Verfall als Lebensraum — modrige Grabkammern und zerfallende Kartenstapel.
Die Tiere sind keine Monster-Schrecken, sondern Aasfresser mit Würde: sie
ernten, was das Deck verliert.

- **Palette:** fahles Grün und krankes Violett auf tiefem Schwarz
- **Motive:** verrottende Karten und Pergament, Maden/Motten/Geier/Egel,
  Grabkammern, Karten, die zu Staub zerfallen
- **Baustein:**
```
sickly green and diseased violet on deep black, decaying tarot cards and rotting parchment, crypt chamber, card fragments dissolving into dust, scavenger creature dignity
```

## Dragon Shrine — LIGHT · Dragon (14 Karten)

Ein Bergschrein, in dem Drachen verehrt werden und Pilger zu Drachen werden.
Alles ist Zeremonie: Tore, Opfergaben, Wächter — und ganz oben der ewige Wyrm.

- **Palette:** Gold und Weiß auf warmem Dämmerblau, Weihrauch-Schleier
- **Motive:** Schrein-Tore (Torii), steinerne Drachenstatuen, die erwachen,
  Opferschalen, Gebetsbänder, gewundene goldene Drachenleiber
- **Baustein:**
```
white and gold sacred light on warm dusk blue, mountain dragon shrine with torii gates, incense smoke, coiled golden dragon, stone guardians and prayer ribbons
```

## Failsafe — EARTH · Human/Artefakte (7 Karten)

Eine unterirdische Sicherungs-Werkstatt. Fällt eine Sicherung, rastet mit
einem satten Klack die nächste ein. Die Menschen hier sind ruhige Ingenieure:
nichts überrascht sie, für alles liegt ein Ersatz bereit.

- **Palette:** Messing, Bronze und grauer Stein, Kontrollleuchten in kühlem Türkis-Blau
- **Motive:** Zahnräder, Siegel, Schalttafeln, Ersatzteil-Regale, Dampf,
  ineinandergreifende Sicherungsmechanismen
- **Baustein:**
```
brass and bronze machinery on grey stone, cool teal indicator lights, interlocking gears and seals, underground engineering workshop, calm preparedness
```

## Fethaerbreese — WIND · Animal (12 Karten)

Vogelwesen aus Feder und Aufwind, so leicht, dass sie kaum landen. Ihre Magie
ist das Zurückkehren: was aufsteigt, findet heim.

- **Palette:** Himmelblau und Wolkenweiß mit blassem Türkis und Sonnengold
- **Motive:** treibende Federn, Aufwinde und Wolkenbänke, hohle Knochen,
  Nester auf Felsnadeln, Schwärme im Gegenlicht
- **Baustein:**
```
sky blue and cloud white with pale teal and sun gold, drifting feathers and updrafts, bird spirits soaring over cloud banks, wind-swept nests on rock spires
```

## Forgeheart — FIRE · Mecha (12 Karten)

Konstrukte mit einer Esse als Herz. Sie schmieden sich selbst, opfern
Bauteile und geben Ersatz weiter — eine Werkstatt, die lebt.

- **Palette:** Schmiedeorange und geschmolzenes Gold auf Eisen-Grau und Ruß
- **Motive:** glühende Brustkerne, Amboss und Hammer, Funkenregen,
  geschmolzenes Metall in Gussrinnen, dampfende Werkhallen
- **Baustein:**
```
forge orange and molten gold on iron grey, glowing furnace heart core, anvil sparks flying, molten metal channels, living construct blacksmith
```

## Gaslight — DARK · Myth (8 Karten)

Ein viktorianisches Illusions-Varieté nach Mitternacht. Die Illusionisten
reden dem Gegner ein volles Feld ein — ihre Trugbilder sind halbtransparente
Gestalten aus Gaslicht, die sich auflösen, sobald jemand genauer hinsieht.

- **Palette:** kaltes Grün-Türkis der Gaslaternen vor tiefem Violett-Schwarz
- **Motive:** Samtvorhänge, Spiegel, Rauch, Bühnenlicht, halbtransparente
  Trugbilder, Varieté-Architektur
- **Baustein:**
```
cold green-teal gaslight on deep violet black, victorian illusion theatre, velvet curtains and mirrors, semi-transparent light phantoms dissolving, midnight vaudeville
```

## Genostitched — DARK · Mecha (11 Karten)

Chirurgie trifft Maschinenbau: zusammengenähte Wesen aus Fleisch, Stahl und
angelegten Artefakten. Das Labor ist stolz auf jede Naht.

- **Palette:** krankes Cyan-Grün und Chirurgen-Weiß auf dunklem Violett-Grau
- **Motive:** grobe Nähte und Klammern, implantierte Maschinenteile,
  Bernsteinglas-Tanks, OP-Lampenlicht, Skalpelle und Schläuche
- **Baustein:**
```
sickly cyan-green and surgical white on dark violet grey, stitched flesh fused with machine parts, specimen tanks, operating lamp glow, proud laboratory horror
```

## Gravemaw — DARK · Demon/Beast (11 Karten)

Gräber mit Kiefern: Wesen, die Friedhöfe fressen und Eingefrorenes wieder
auftauen. Ihr Reich ist die Kühlkammer unter dem Boden — nichts bleibt
begraben, alles wird Nahrung oder kehrt zurück.

- **Palette:** Erdbraun und Schwarz mit kaltem Blaugrün (Frost) und Knochenweiß
- **Motive:** riesige aufgerissene Mäuler im Boden, Grabsteine, Frost auf
  Särgen, ausgegrabene Knochen, Kälte-Nebel
- **Baustein:**
```
earthen brown and black with cold blue-green frost light, giant jaws opening in graveyard soil, frosted coffins and unearthed bones, freezing mist crypt
```

## Heavenly — LIGHT · Angel (12 Karten)

Der geordnete Himmel: Engel-Chöre, Leibwachen, Herolde. Marmor, Gold und
warmes Licht — Autorität, die beschützt.

- **Palette:** Weiß und Gold mit warmem Morgenlicht auf blassblauem Himmel
- **Motive:** ausgebreitete Flügel, Heiligenscheine, Marmortore und Säulen,
  Wolkenböden, Posaunen und Banner
- **Baustein:**
```
white and gold with warm dawn light on pale blue sky, spread angel wings and halos, marble gates and columns, cloud floors, choir of protective authority
```

## Hexweaver — DARK · Human (12 Karten)

Weberinnen, die Flüche als Gewebe spannen. Jeder Faden ist ein Schicksal,
jeder Webstuhl eine Falle — wer hindurchgeht, ist eingesponnen.

- **Palette:** tiefes Violett und Indigo mit silbern glühenden Fäden
- **Motive:** Webstühle, gespannte Leuchtfäden, Spinnweben-Muster, Nadeln
  und Spindeln, verhüllte Weberinnen mit vielen Händen
- **Baustein:**
```
deep violet and indigo with silver glowing threads, cursed loom, luminous woven strands stretched like webs, veiled witch weavers, needle and spindle motifs
```

## Kindlekin — FIRE · Beast (15 Karten)

Kleine Feuergeschöpfe am Herd: Glut-Welpen und Aschenkinder, die sich
gegenseitig aus Herd, Hand und Grab nachlegen. Klein, warm, unzählbar.

- **Palette:** Glut-Orange und warmes Gold auf Asche-Grau
- **Motive:** Herdfeuer und Kohlebetten, kleine flammende Tierwesen,
  Funken-Schwärme, gemauerte Kamine, Wärme im Dunkeln
- **Baustein:**
```
ember orange and warm gold on ash grey, small flame beast cubs, hearth fire and coal beds, spark swarms, cozy warmth against darkness
```

## Lightless — DARK · Human/Demon (13 Karten)

Der Kult der gelöschten Lichter: Priester und Schatten, die verdeckt liegen
und aus dem Dunkel aufstehen. Ihr Licht ist das violette Glimmen der Kerzen,
die niemand angezündet hat.

- **Palette:** violett-schwarze Schatten, fahles violettes Glimmen
- **Motive:** verhüllte Gestalten aus lebender Dunkelheit, schwebende schwarze
  Kerzenflammen, zerstörte Schreine bei Nacht, Schleier und geschlossene Särge
- **Baustein:**
```
violet-black living shadows, faint purple ember glow, hooded wraith cultists, floating black candle flames, ruined night shrine, veiled and face-down secrecy
```

## Lyria — LIGHT · Human (11 Karten)

Die große Oper: Lyria dirigiert, die Bühne gehorcht. Auftritte, Abgänge,
Zugaben — Beschwörungen sind Inszenierungen, der Tod nur ein Vorhang.

- **Palette:** Elfenbein und Gold mit Bordeaux-Rot, warmes Rampenlicht
- **Motive:** Bühnen und Samtvorhänge, Dirigierstab und Notenlinien im Licht,
  Kronleuchter, Garderoben, Scheinwerferkegel
- **Baustein:**
```
ivory and gold with bordeaux red velvet, grand opera stage, warm limelight beams, conductor's baton drawing glowing music lines, chandeliers and curtains
```

## Manacle — DARK · Myth (18 Karten)

Die Schuldeneintreiber: Geister mit Ketten und Kontobüchern. Mana ist
Währung, jeder Zug ein Kredit — und die Zinsen kommen mit blauem Feuer.

- **Palette:** Eisengrau und Schwarz mit kaltem Blau-Feuer und Messing
- **Motive:** Ketten und Fesseln, aufgeschlagene Hauptbücher, Waagen,
  Siegellack, Geisterhände, die Schuldscheine reichen
- **Baustein:**
```
iron grey and black with cold blue spectral fire, heavy chains and manacles, open debt ledgers and scales, brass seals, ghostly debt collector myth
```

## Mechination — EARTH · Mecha (20 Karten)

Die Fabrik, die sich selbst baut: Schichtarbeit, Vorarbeiter, Weltgetriebe.
Messing-Kolosse, die einander aus Hand und Halde nachschieben.

- **Palette:** Messing, oxidiertes Kupfer und warmes Ocker
- **Motive:** Zahnrad-Kolosse, Fließbänder, Dampfkessel, Schichtpläne an
  Werkstoren, Kranarme, Fabrikhallen mit Oberlicht
- **Baustein:**
```
brass and oxidised copper with warm ochre, colossal gear-driven constructs, factory halls with skylight beams, conveyor belts and steam boilers, industrious machine city
```

## Mimicrypt — DARK · Demon (8 Karten)

Eine Krypta unter dem Friedhof des Gegners, bewohnt von Kopisten-Dämonen.
Sie pausen fremde Zauber ab, tragen fremde Gesichter und beschwören fremde
Tote — nie ist klar, welche Silhouette das Original ist.

- **Palette:** kaltes Violett und Grau-Schwarz, fahles grünliches Kerzenlicht
- **Motive:** Spiegel, Wachsmasken, abgepauste Manuskripte, doppelte
  Silhouetten, Garderoben voller geliehener Roben
- **Baustein:**
```
cold violet and grey-black, pale greenish candlelight, cracked mirrors and wax masks, copied manuscripts, uncanny double silhouettes, crypt beneath a graveyard
```

## Paperbound — DARK · Human (10 Karten)

Die Höllen-Bürokratie: Dämonen-Beamte, die mit Formularen töten. Anträge,
Wartezimmer, endgültige Ablehnungen — der Schrecken ist der Amtsweg.

- **Palette:** Sepia und Pergament mit Amtsgrün und schwarzer Tinte
- **Motive:** Aktenberge, Stempel und Siegel, Schreibpulte als Richterpodest,
  Wartezimmer-Bänke, fliegende Formulare, Paragraphen-Ornamente
- **Baustein:**
```
sepia and parchment with bureaucratic green and black ink, towering stacks of files, giant rubber stamps and seals, demonic clerk at a judge's desk, flying forms
```

## Powderkeg — FIRE · Mecha (12 Karten)

Sprengmeister-Konstrukte mit Fässern statt Bäuchen. Alles ist Vorbereitung
auf den einen kontrollierten Knall — Lunten legen, Magazin füllen, feuern.

- **Palette:** Rostrot und Schwarzpulver-Grau mit Funkengold
- **Motive:** Pulverfässer, brennende Lunten, Munitionsregale, Explosions-
  wolken im Hintergrund, Sprenggruben, Zündschlüssel
- **Baustein:**
```
rust red and gunpowder grey with spark gold, powder kegs and burning fuses, ammunition racks, controlled demolition site, distant blast cloud
```

## Redactor — DARK · Human (11 Karten)

Der Zensoren-Orden: sie schwärzen, was nicht existieren darf. Ihre Magie
sind schwarze Balken, gelöschte Seiten und Archive, die niemand betritt.

- **Palette:** harter Schwarz-Weiß-Kontrast mit Tintenblau
- **Motive:** schwarze Balken, die über Text und Gesichter fallen, zensierte
  Dokumente, Druckpressen, Archivregale, Tintenfässer, verhüllte Zensoren
- **Baustein:**
```
stark black and white with ink blue, black redaction bars floating over documents and faces, censored archives, printing press, hooded censor order
```

## Sacrilegion — LIGHT · Dragon (20 Karten)

Die entweihte Kathedrale, in der Knochendrachen heilig sind. Gold auf
Gebein: Schwüre, Opferlämmer und Auferstehung als Liturgie.

- **Palette:** Gold und Elfenbein, Knochenweiß auf dunklem Kirchenraum
- **Motive:** skelettierte Drachen mit Goldschmuck, Kirchenfenster,
  Kerzenmeere, Reliquienschreine, Weihrauch, zerbrochene Altäre
- **Baustein:**
```
gold and ivory with bone white on dark cathedral gloom, gilded skeletal dragons, stained glass windows, seas of candles, desecrated altars and reliquaries
```

## Sleightwind — WIND · Demon (13 Karten)

Taschenspieler-Dämonen aus Rauch und Zugluft. Karten verschwinden im Ärmel,
Erinnerungen gleich mit — ihre Magie ist die Handbewegung, die keiner sah.

- **Palette:** blasses Teal und Rauchgrau mit stumpfem Violett
- **Motive:** wirbelnde Spielkarten, Ärmel und Handschuhe, Rauchschwaden in
  Gestaltform, gestohlene Gesichter, Windböen in Innenräumen
- **Baustein:**
```
pale teal and smoke grey with dull violet, swirling playing cards, sleight-of-hand demon in drifting smoke, indoor wind gusts, vanishing trick mid-motion
```

## Slowburn — FIRE · Human (8 Karten)

Eine Gilde geduldiger Feuermagier. Nichts explodiert sofort — alles schwelt:
Zündschnüre kriechen über den Boden, Kerzen brennen auf Termin, Glut wartet
unter Asche. Wer hier zündet, hat es lange vorher gelegt.

- **Palette:** warmes Glut-Orange und Kerzengold auf tiefem Braun-Schwarz
- **Motive:** kriechende Zündschnüre, Kerzen mit Stundenmarken, glimmende
  Linien am Boden, Sanduhren, wartende Magier mit verschränkten Armen
- **Baustein:**
```
warm ember orange and candle gold on deep brown-black, creeping lit fuses, timed candles, smoldering glow lines under ash, patient fire mage guild
```

## Snugglet — gemischt · Beast/Animal (12 Karten)

Kuscheltier-Wesen mit Kissenburg-Befestigung. Flauschig, verschlafen und
erstaunlich schwer zu töten — Gemütlichkeit als Verteidigungsdoktrin.

- **Palette:** weiche Pastelltöne mit warmem Lampenlicht
- **Motive:** Deckenburgen, Sofas und Kissen, Plüschfell, Schlafmützen,
  Milch und Kekse, ein Auge halb offen — zur Sicherheit
- **Baustein:**
```
soft pastel colors with warm lamplight, plush fluffy creature, blanket fort and pillows, cozy living room den, sleepy but watchful, wholesome and sturdy
```

## Tidebound — WATER · Myth (12 Karten)

Gezeiten-Geister und das Meer, das alles zurückbringt. Versunkene Ruinen,
Flaschenpost, der Leviathan unter der Oberfläche — die Flut nimmt und gibt.

- **Palette:** Tiefblau und Türkis mit Schaumweiß und fahlem Mondlicht
- **Motive:** Wellenkämme und Strudel, versunkene Säulen, Flaschenpost,
  leuchtende Quallen-Schleier, aufsteigende Blasen, Muschelpanzer
- **Baustein:**
```
deep sea blue and teal with foam white and pale moonlight, tidal spirits, sunken ruin columns, cresting waves and whirlpools, bioluminescent glow beneath the surface
```

## Trapline — EARTH · Human (13 Karten)

Fallensteller im Herbstwald: Schlingen, Tellereisen und gespannte Leinen.
Ihre Zauber liegen gesetzt und warten — der Wald selbst ist die Falle.

- **Palette:** Moosgrün und Erdbraun mit Kupfer und Herbstlaub-Orange
- **Motive:** gespannte Seile und Schlingen, Tellereisen, Rauchzeichen,
  Jagdhütten, Laubhaufen mit Metallglanz darunter, Stolperdrähte
- **Baustein:**
```
moss green and earthen brown with copper and autumn leaf orange, taut rope snares and steel traps hidden under leaves, hunter's forest, tripwires and smoke signals
```

## Wyldpack — WIND · Beast (12 Karten)

Das Wolfsrudel im Sturm: sie jagen gestaffelt, rufen einander und kommen
immer zu mehreren. Der Wind trägt das Heulen voraus.

- **Palette:** Sturmgrau und Silber mit Waldgrün und kaltem Mondlicht
- **Motive:** Wölfe im Sprung, gesträubtes Fell im Wind, Rudel-Silhouetten
  am Hang, zerklüftete Wildnis, Vollmond hinter Wolkenfetzen
- **Baustein:**
```
storm grey and silver with forest green and cold moonlight, leaping wolves with wind-blown fur, pack silhouettes on a ridge, rugged wilderness, full moon behind torn clouds
```

---

## Dark Angel — DARK · Angel (visuelle Familie, kein Namens-Archetype)

Gefallene Engel: The Fallen One, Immortal Demon, The Last Asemir. Himmlische
Anatomie mit gebrochener Heiligkeit — schwarze Flügel, matte Goldreste,
zersprungene Heiligenscheine. Würde ohne Gnade.

- **Palette:** Obsidian-Schwarz und Asche mit mattem Alt-Gold und blutrotem Licht
- **Motive:** zerzauste schwarze Flügel, geborstene Halos, gefallene Statuen,
  dunkler Himmel mit letztem Goldstreif, Federn, die zu Asche werden
- **Baustein:**
```
obsidian black and ash with tarnished gold and blood-red light, fallen angel with ragged black wings, broken halo fragments, feathers turning to ash, ruined celestial statuary
```

## Generics (kein Archetype)

Generics tragen KEINEN Familien-Look — sie folgen nur dem globalen Fundament.
Palette und Motiv frei nach Karte; einzige Regeln: gemalte Dark Fantasy,
dramatisches Licht, zentriertes Motiv, kein Text/Rahmen. Bei elementaren
Karten die Attributfarbe anklingen lassen (FIRE warm-rot, WATER blau-türkis,
WIND hellgrau-grün, EARTH ocker-braun, LIGHT gold-weiß, DARK violett-schwarz).

## Heroes (PlayerCards)

Eigenes Dokument: `AmusePrompts-10-Heroes.md` — Porträt-Kompositionen,
Charakter im Zentrum, Element-Aura passend zum Deck.
