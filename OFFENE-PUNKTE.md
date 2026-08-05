# Offene Punkte — Stand 2026-08-06

Der letzte Build ist **0.1.0f9** (`Build/Reliquary-0.1.0f9.zip`). Die
Projektversion steht bereits auf **0.1.0h** — der Build dazu fehlt noch.

Server (`server.js`, `cosmetics.js`) und DuelHost laufen auf 217.154.212.82 mit
den passenden Gegenstücken; die Website (`mcweb`) trägt den Spieler-Editor.

Alles bis `fcec5ec` ist auf `github.com/FelixEllspermann/reliquary` gepusht.

---

## Offen

### 0 · Artworks fuer 61 neue Karten
Alle 61 Karten der fuenf neuen Archetypes haben kein Bild. Prompts liegen
fertig in `AmusePrompts-5-Archetypes.md` (Universal-Negativ + 61 Positiv).
Danach `Rouge → Card Design → Artworks automatisch zuweisen`.

### 1 · Build fehlt (Stand 0.1.0i gebaut, danach kam noch mehr)
Die Finishes im Duell und die Deck-Builder-Vorschau sind gebaut und geprüft,
aber noch in keinem Build. Der Server ist schon aktualisiert — ein alter Client
verträgt das, das Feld `finish` ist ihm nur unbekannt.

### 2 · Nichts davon ist gespielt
Aktivierung, Zielwahl, Zerstörung, Tresor, Niederlage, die fünf Siegessiegel,
der Münzwurf und jetzt die Finishes sind einzeln gerendert geprüft — aber **nie
im laufenden Duell**. Ein Solo-Duell gegen einen Bot zeigt fast alles auf einmal.

Nur mit zwei Clients am selben Server prüfbar: geteilte Spielmatte, Kartenrücken
des Gegners, Ereignis-Reihenfolge, Deck-Suche, Finishes des Gegners.

Falls der Multiplayer schlechter läuft als vorher, liegen die Vorgänger daneben:
`/opt/rouge-duelhost/DuelHost.dll.bak-finishes` und
`/opt/rouge-tcg/server.js.bak-finishes`.

### 3 · Finish-Stärke im Duell ist Geschmackssache
Auf einer Karte in Zonengröße (112 px) sind Regenbogen und Static deutlich
sichtbar, aber ATK/DEF bleiben lesbar. Wenn es im Spiel zu laut wirkt, sitzen
die Deckkräfte alle in `CardFinishOverlay.Rebuild()` — eine Zahl je Ebene.

### 4 · Reliquary-Kartenrahmen  ← FREIGEGEBEN
Violette Akzente sind abgesegnet: violette Namensplatte, Typband „RELIQUARY",
violettes Wappen, passender Kartenrücken.

Das Chassis existiert bereits in Elfenbein/Gold — `CardDesignGenerator.Reliquary()`
ist die Palette, `GenerateReliquary()` erzeugt Chassis, Badge, Kompaktkarte und
Extra-Deck-Ablage. Zu tun ist also nur die Palette auf Violett zu ziehen und ein
eigenes Wappen (`BuildCrest`) plus Kartenrücken zu ergänzen — kein neuer
Generator, ein Durchgang mit `Rouge/Card Design/Generate Reliquary Assets`.

### 5 · Rune Blade
Die Ursache des Hängers ist weiterhin unbekannt. Der Dreier-Schutz in
`DuelSession` (dreimal abgelehnte Antwort → neutrale Antwort, mit Logzeile)
fängt es ab und macht den nächsten Fall nachlesbar.

---

## Erledigt in dieser Sitzung

| Punkt | Wo |
|---|---|
| **Finish reist ins Duell** | Deckliste → `server.js` (`deckFinishes`) → `DuelSession.ReadSide` → `CardInstance.Finish` → `CardWire` → `DuelMirror` → `TcgCardView.Show` |
| Aufzählungstyp verschoben | `Engine/CardFinishKind.cs` — den Ordner kompiliert der DuelHost; die Anzeige-Helfer bleiben in `Net/CardFinish.cs` |
| Gleichschritt beim Auflösen | `CardCatalog.ResolveList(names, finishes, kept)` — ein unbekannter Name nimmt sein Finish mit raus |
| Verdeckt bleibt verdeckt | `CardWire` maskiert das Finish; eine funkelnde Rückseite verriete die Karte |
| Solo-Duell | `MatchContext.LocalDeckFinishes` aus `SoloController` und `DuelSetupController` |
| `RuntimeDeck.Clone()` | kopierte die Finishes nicht mit — aus einer Kopie wurde ein Deck aus lauter schlichten Karten |
| Ein einziger Zeichenort | `TcgCardView.Show`; `PackOpenSequence` ruft das Overlay nicht mehr selbst |
| **Deck-Builder-Vorschau** | `Select(card, finish)` statt `Select(card)`; die Zeile gibt ihr Exemplar weiter |
| Finish-Leiste | `DeckBuilderController.BuildFinishStrip()` unter der Vorschaukarte, alle vier Ausführungen, besessene hell mit Stückzahl |
| Zeilen-Markierung | leuchtet nur noch für das angezeigte Exemplar, nicht für jede Zeile der Karte |
| **Spieler-Editor neu gebaut** | Kachel sitzt jetzt IM Gitter (`tile wide`) statt hinter `</div></div>`; Sicherung: `admin.html.bak-ui2` |

### Wie geprüft
- DuelHost-Selftest: `Player1Wins`, 10 Züge — Engine unverändert gesund.
- Vier Karten im Editor gerendert, groß und in Zonengröße: die drei Overlays
  legen 1 / 2 / 3 Ebenen an, schlicht keine.
- Detail-Rail über Reflexion mit dem echten Controller aufgerufen: 4 Chips,
  300 px breit, richtiges Finish markiert.
- Probe-Duell gegen einen lokalen DuelHost (Port 7901, **nicht** der Server —
  eine zweite Verbindung würde die von Node ersetzen): dieselbe Karte einmal
  als Glossy und einmal als Static, also hängt das Finish am Exemplar und
  überlebt das Mischen. Der Bot mit schlichtem Deck zeigt durchweg 0.
- `admin.html` sauber verschachtelt, API ohne Login weiterhin 401.

---

## Wo was liegt

| Was | Wo |
|---|---|
| Finish zeichnen | `Assets/_Game/Scripts/Tcg/UI/CardFinishOverlay.cs` (Deckkräfte in `Rebuild()`) |
| Finish-Typ | `Assets/_Game/Scripts/Tcg/Engine/CardFinishKind.cs`, Helfer in `Net/CardFinish.cs` |
| Kosmetik-Grafiken erzeugen | Menü `Rouge/Cosmetics/Generate Art` → `CosmeticArtGenerator.cs` |
| Karten-/Münz-Sprites erzeugen | Menü `Rouge/Card Design/…` → `CardDesignGenerator.cs` |
| Grafik zur Kosmetik-Id finden | `Assets/_Game/Scripts/Tcg/Net/CosmeticArt.cs` |
| Siegessiegel | `Assets/_Game/Scripts/Tcg/UI/VictorySealSequence.cs` |
| Katalog und Preise | `Server/cosmetics.js` (deployt nach `/opt/rouge-tcg/`) |
| Spieler-Editor (Website) | `/opt/mcweb/public/admin.html` + Proxy in `/opt/mcweb/server.js` |
| Spieler-Editor (Spielserver) | `Server/server.js`, Abschnitt „Admin-Schnittstelle" |
| Patchnotes | `Assets/_Game/Resources/PatchNotes.txt` |

**Beim Sprite-Erzeugen:** `SaveSprite` setzt `mipmapEnabled = false`, Bilinear und
unkomprimiert bis 900 px. Vorher stand dort `mipmapEnabled = true` plus
Trilinear — das war die eigentliche Ursache der Unschärfe, und es hätte jede
Import-Korrektur bei der nächsten Neugenerierung wieder überschrieben.

**Nach `Generate Art`:** die vier kopierten Vanilla-Münz-Sprites
(`coin_vanilla_relic/_seal`, `coin_shadow`, `coin_dustring`) unter
`Resources/Cosmetics` prüfen — der Presenter findet die Standardmünze nur dort.

**Der DuelHost nimmt nur eine Verbindung.** Wer sich zum Prüfen auf Port 7900
verbindet, verdrängt Node und schneidet laufende Duelle ab. Immer einen eigenen
Host starten: `dotnet run -c Release -- --serve --data ../data --port 7901`.
