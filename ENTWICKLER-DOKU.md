# RELIQUARY — Entwickler-Dokumentation

*Stand: Build 0.1.2. Diese Doku beschreibt, wie das Spiel technisch funktioniert und wie man daran arbeitet, ohne sich die gesammelten Narben neu zu verdienen. Spielregeln stehen in `RULEBOOK.md`; hier steht, warum der Code so aussieht, wie er aussieht.*

---

## Inhalt

1. [Architektur-Überblick](#1-architektur-überblick)
2. [Repository-Layout](#2-repository-layout)
3. [Die Engine](#3-die-engine)
4. [Der Unity-Client](#4-der-unity-client)
5. [Der Node-Server](#5-der-node-server)
6. [Der DuelHost](#6-der-duelhost)
7. [Deployment & Betrieb](#7-deployment--betrieb)
8. [Kochbuch: wiederkehrende Arbeiten](#8-kochbuch-wiederkehrende-arbeiten)
9. [Invarianten & Fallstricke](#9-invarianten--fallstricke)
10. [Sicherheit](#10-sicherheit)

---

## 1. Architektur-Überblick

Das Spiel besteht aus **drei Prozessen**:

```
┌─────────────────┐   WebSocket    ┌──────────────────┐   TCP (lokal)   ┌─────────────────┐
│  Unity-Client   │◄──────────────►│  Node-Relay      │◄───────────────►│  .NET DuelHost  │
│  (Windows-Build │   Port 7777    │  server.js       │   Port 7900     │  (dieselbe      │
│   oder Editor)  │                │  Accounts, Shop, │                 │   Engine wie    │
│                 │                │  Matchmaking,    │                 │   der Client!)  │
│                 │                │  sduel-Relay     │                 │                 │
└─────────────────┘                └──────────────────┘                 └─────────────────┘
```

- **Der Unity-Client** rendert, sammelt Eingaben und rechnet **Solo-Duelle lokal** (gegen Bots). Für Online-Duelle ist er nur noch ein **Spiegel**: er zeigt an, was der Server sagt, und schickt Entscheidungen zurück.
- **Der Node-Relay** (`Server/server.js`) ist die Autorität über alles Persistente: Accounts, Sammlung, Decks, Coins, Packs, Kosmetik, Rang. Er spricht WebSocket mit den Clients und reicht Duell-Nachrichten an den DuelHost weiter.
- **Der DuelHost** (`Server/duelhost/`) rechnet Online-Duelle server-autoritativ. Er kompiliert **dieselben Engine-Quellen** wie der Client (`Assets/_Game/Scripts/Tcg/Engine/**`) — es gibt genau eine Regelimplementierung im ganzen Projekt.

Der zentrale Satz der Architektur: **die Engine kennt kein Unity-UI und kein Netzwerk.** Sie spricht mit der Außenwelt über drei Schnittstellen: `DuelController` (wer entscheidet?), `IDuelPresenter` (wer zeigt an?) und `runRoutine` (wer pumpt die Coroutinen?). Dadurch läuft identischer Code im Editor, im Build und headless auf dem Server.

---

## 2. Repository-Layout

```
Rouge/
├── Assets/_Game/
│   ├── Art/                    Karten-Artworks (PNG, exakt nach Kartennamen benannt)
│   │   └── Cosmetics/          Roh-Bilder für Kosmetik (Rahmen, Profile Pics)
│   ├── Data/
│   │   ├── Tcg/                CardCatalog.asset + alle Karten-Assets (je Unterordner)
│   │   │   ├── PlayerCards/    die 12 Helden
│   │   │   └── Packs/          CardPackDefinition-Assets (RelicPack, HeroCache)
│   │   └── Player/             (Alt-Prototyp)
│   ├── Fonts/                  TMP-Fontassets (Cinzel, Oswald, Spectral)
│   ├── Resources/              ZUR LAUFZEIT ladbar: PatchNotes.txt, TransitionSkin,
│   │   └── Cosmetics/          Kosmetik-Sprites nach Namenskonvention (frame_<id> …)
│   ├── Scenes/                 Login, StarterPick, MainMenu, Play, Shop, DeckEditor, Duel
│   └── Scripts/
│       ├── Tcg/Engine/         DIE ENGINE (Unity-frei kompilierbar, eigenes asmdef)
│       ├── Tcg/Data/           Unity-seitige Datentypen (CardSkin, CardPackDefinition)
│       ├── Tcg/Net/            NetworkManager, PlayerProfile, Cosmetics, CosmeticArt
│       ├── Tcg/UI/             alle Szenen-Controller und die Duell-Präsentation
│       └── Editor/             Exporter, ArtworkAssigner, Prefab-Builder, Batch-Tools
├── Build/                      Build-Ausgabe + Release-Zips (nicht committet)
├── Server/
│   ├── server.js               der Relay (Accounts, Shop, Matchmaking, Admin-API)
│   ├── db.js                   SQLite-Schicht (benannte Spalten + Migrationen)
│   ├── finishes.js             Finish-Würfel & Sammlungseinträge [plain,glossy,rainbow,static]
│   ├── cosmetics.js            Kosmetik-Katalog (42 Items, 7 Fächer) + Kauf/Ausrüsten
│   ├── ranks.js                Rangleiter (10 Siegel × 5 Stufen, RP, Monats-Seasons)
│   ├── data/                   die Wahrheit über Karten & Ökonomie (siehe 5.4)
│   └── duelhost/               .NET-Projekt; kompiliert ../../Assets/.../Engine/**
├── RULEBOOK.md                 Spielregeln (englisch, spielerseitig)
└── ENTWICKLER-DOKU.md          diese Datei
```

---

## 3. Die Engine

Namespace `Rouge.Tcg`, Ordner `Assets/_Game/Scripts/Tcg/Engine/`, eigenes Assembly (`Rouge.Tcg.Engine.asmdef`). **Jede Datei hier wird vom DuelHost mitkompiliert** — wer hier `UnityEngine.UI` importiert, bricht den Server-Build.

### 3.1 Datenmodell

```
CardDefinition (ScriptableObject)          Basisklasse: cardName, artwork, rarity, effects[]
├── MonsterCardData                        level, attribute, monsterType, atk, def,
│                                          canSelfSpecialSummon + selfSummonRequires*-Bedingungen
│   └── ReliquaryCardData                  summonText, summonManaCost, req*-Bedingungen,
│                                          cost*-Kosten (inkl. Tribute von BEIDEN Feldern)
├── SpellCardData                          speed (Normal/Quick), manaCost
├── ArtifactCardData                       manaCost, atkBonus, redirectDestructionToSelf …
└── PlayerCardData                         startLifePoints (7500–8500)
```

- **`EffectDefinition`**: label, text, isInfused, infusedKind (Standalone/Coupled), manaCost, trigger, oncePerTurn, onlyIfSpecialSummoned, requiresEquippedArtifact, `List<EffectAction> actions`.
- **`EffectAction`**: type (das große Enum), isCost, amount, target (TargetKind), targetCount, upToTargets, Filter (useTypeFilter/typeFilter, useAttributeFilter/attributeFilter, levelFilter **= exakter Level**, maxAtkFilter **= „ATK ≤ X"**, nameFilter, mentionsFilter), targetExcludesSelf.
- **`CardCatalog`**: die eine Liste aller Karten-Assets. `FindByName`, `ResolveList` (hält Finish-Listen im Gleichschritt, auch wenn ein Name fehlt).
- **`GameRules`**: alle Zahlwerte des Regelwerks (Starthände, Mana, Tribute, Limits). Ein Asset, im Inspector balancierbar — **editor-first** ist Projektprinzip.

### 3.2 Die Enums — NUR ANHÄNGEN

`TcgEnums.cs` enthält `EffectActionType` (~60 Actions), `EffectTrigger`, `TargetKind` u. a. Karten-Assets speichern **Zahlenwerte**. Wer mitten im Enum einfügt oder umsortiert, **vertauscht die Effekte aller existierenden Karten**. Neue Werte kommen ausschließlich ans Ende, unter den Warnkommentar. Das gilt für alle drei Enums.

### 3.3 DuelManager & DuelActions

`DuelManager` (partial, Fortsetzung in `DuelActions.cs`) ist die Regelmaschine. Sie ist **komplett coroutinen-basiert**: jede Handlung ist ein `IEnumerator`, der `yield return`t, wann immer eine Entscheidung oder eine Animation ansteht.

- **Start-Wege:** `StartDuel()` (Solo/Lokal, Decks aus Inspector oder `MatchContext`), `StartServerDuel(seed, …)` (headless, beide Controller übergeben), `MirrorBegin(...)` (Client-Spiegel eines Server-Duells, rechnet selbst NICHTS).
- **Pump:** `runRoutine` treibt die Coroutinen. Unity gibt eine MonoBehaviour-StartCoroutine hinein; der DuelHost einen eigenen Stack; Default ist `DriveImmediately` (synchron bis zum Ende — für Selftests). `DuelWait`/`null`-Yields sind reine Wartezeichen und werden headless übersprungen.
- **Entscheidungen** laufen über Request-Objekte (`DuelRequests.cs`): `MainActionRequest`, `BattleActionRequest`, `YesNoRequest`, `OptionRequest`, `TargetRequest`, `ZoneSelectRequest`, `StartChoiceRequest`. `DecideRouted(player, request)` reicht sie an den **`DuelController`** des Spielers:
  - `HumanDuelController` → UI fragt den Menschen,
  - `BotDuelController` → Heuristiken (Kosten-Auswahlen nehmen das Schwächste, alles andere das Stärkste),
  - serverseitig ein Wire-Controller, der Requests zum echten Client schickt.
- **Effekt-Auflösung** (`ResolveEffectActions`): läuft zweimal — `costsPhase:true` (nur `isCost`-Actions, sofort bei Aktivierung) und danach die eigentliche Wirkung. Dazwischen liegt das Reaktionsfenster. **Kosten werden nie erstattet.**
- **Ziel-Verfall:** `TargetCollection` macht bei der Zielwahl einen Snapshot (Zone, Besitzer, FaceDown). Bei der Auflösung filtert `StillValid` alles heraus, was sich verändert hat („Fizzle-Regel").
- **Ketten:** Es gibt **keine Chain-Liste** — `ActivateSpell`/`ActivateEffect` öffnen `OpenResponseWindow`, darin läuft die nächste Aktivierung vollständig; die Reihenfolge steckt im **Aufrufstapel**. Zwei Zähler, die man nicht verwechseln darf:
  - `responseDepth` zählt nur **Reaktionsfenster** (Cap: `>= 2` → keine weiteren Antworten ⇒ max. 3 Glieder).
  - `chainDepth` zählt **Aktivierungs-Verschachtelung** — auch Trigger, die mitten in einer Auflösung feuern (OnDestroyedSelf etc.) und nie durch ein Fenster gehen. Die Kettenanzeige und die Log-Nummern hängen an `chainDepth`; wer sie an `responseDepth` hängt, bekommt Phantom-Ketten (gelernt am 06.08.).
- **Ersatz-Zerstörung:** `DestroyCard(card, asReplacement)` — ein Schild-Artefakt kann eine Zerstörung umleiten, aber eine Umleitung ist endgültig (`asReplacement:true` überspringt `TryRedirectDestruction`). Ohne diese Flagge retten sich zwei Bulwark Prisms gegenseitig bis in alle Ewigkeit und frieren den Prozess ein.
- **Reaktionsfenster-Kontexte:** `"activation"`, `"artifact"`, `"summon"`, `"attack"`. Kandidaten: Quick-Effekte von Feldkarten (inkl. **Spielerkarte** — `FieldCards()` enthält sie), gesetzte Quick-Spells (nicht im Setz-Zug), `HandQuick` aus der Hand, `OnOpponentSummon` in Summon-Fenstern.

### 3.4 IDuelPresenter

Alles Sichtbare läuft über dieses Interface: Draw-Flüge, Summons, Angriffe, Zerstörung, Münzwurf, Phasenbanner — und die drei Ketten-Meldungen `ShowChainLink` / `ShowChainResolve` / `ShowChainEnd`. Der Unity-Client implementiert es in `DuelPresenter`; der DuelHost in `ServerPresenter` (zeichnet nichts, **protokolliert Events** für die Clients). Positions-Gedächtnis: die Engine kennt keine Bildschirmkoordinaten — `RememberView`/`RememberOrigin` vor der Datenänderung, `ShowCardMoved` fliegt danach von der gemerkten Position.

Wichtig für Signaturen: der Presenter darf **nie** über `player.IsLocal` argumentieren — auf dem Server ist `LocalPlayer` null. Besitzer immer als `PlayerState` übergeben; die Wire-Schicht rechnet `mine = evt.Player == viewer`.

### 3.5 DuelMirror (Client-Seite eines Server-Duells)

`MirrorBegin` baut zwei leere `PlayerState`s, `MirrorApplyState` übernimmt Server-Sichten (`SduelView`), `MirrorMaterialize` verwandelt Wire-Requests in echte Request-Objekte, `MirrorAnswer` serialisiert die Antwort, `MirrorEventCard` legt unbekannte Karten an (z. B. eine erstmals gezeigte Handkarte des Gegners), statt Animationen stumm ausfallen zu lassen.

---

## 4. Der Unity-Client

### 4.1 Szenenfluss

```
Login ──(neuer Account / starterPending)──► StarterPick ──► MainMenu
  │                                                            │
  └────────────(bestehender Account)───────────────────────────┤
                                                               ▼
                    ┌──────────┬────────────┬──────────────────┐
                    ▼          ▼            ▼                  ▼
                  Play       Shop       DeckEditor         (Profil/News/
                    │      (Packs +      (Deck-Builder      Banlist sind
                    │      Kosmetik)     + Helden-Wähler)   Overlays im Menü)
                    ▼
                CoinToss ──► Duel ──► zurück ins MainMenu
```

Solo-Duelle: `MatchContext.UseCustomLocalDeck` + Bot-Roster; der `DuelHost`-Component in der Duel-Szene rechnet lokal. Online: `MatchContext.IsServerMatch` — `ServerDuelClient` übernimmt, der lokale DuelHost steigt in `Start()` aus.

### 4.2 UI-Schichten der Duel-Szene

`DuelCanvas` (Kinder in Zeichenreihenfolge): Background → Board → PhaseDivider → Hände → RightRail (LP/Buttons/Log) → CardDetailPanel (links) → PresentationLayer (Flüge/Showcase) → PromptPanel → PilePanel → GameOverPanel. Die **Kettenanzeige** (`ChainTracker`) hängt sich zur Laufzeit als letztes Kind an die **Canvas-Wurzel** (oben mittig, aufklappbar). Overlays, die garantiert über allem liegen müssen, bekommen einen **eigenen Canvas mit `overrideSorting`** (Muster: Helden-Wähler, sortingOrder 300) — Geschwister-Reihenfolge allein ist zerbrechlich.

Wichtige UI-Klassen: `DuelUIController` (Prompts/Buttons), `DuelBoardRenderer` (Zonen → `TcgCardView`s, Hover → `CardDetailPanel`), `DuelPresenter` (alle Animationen), `TcgCardView` (eine Karte; **Vollbild ab 200 px Breite, darunter Kompaktmodus** — `CompactThreshold`), `CardShowcase`, `PackOpenSequence` (1–5 Karten), Szenen-Controller je Szene (`ShopController`, `DeckBuilderController`, `ProfilePanel`, `StarterPickController`, …).

**Laufzeit-UI-Muster:** Viele Panels werden komplett im Code gebaut (StarterPick, ChainTracker, Helden-Wähler, PackInfo-Overlay). Regeln dabei: bei gestreckten Rects (`anchorMin != anchorMax`) ist `sizeDelta` ein **Inset** — Ränder über `offsetMin/offsetMax` setzen; ScrollRects brauchen eine Raycast-Graphic **im Viewport**, sonst greift das Mausrad ins Leere; `TcgCardView`s brauchen **feste Maße vor `Show()`**, wenn sie in ein Layout kommen (die Zelle ist beim Aufbau noch 0 breit → Karte spränge in den Kompaktmodus).

### 4.3 Net-Schicht

- **`NetworkManager`** (persistenter Singleton ab Login): WebSocket zu `ws://217.154.212.82:7777`, `useTestServer`-Haken im Inspector schaltet auf `:7778` (nur Editor-Feld — **vor Builds ausschalten**). Versendet/empfängt `NetMessage` (JsonUtility, flach).
- **`NetMessages.cs`** (liegt in Engine/, weil der DuelHost die `Sduel*`-Typen braucht): ein großes flaches Nachrichtenformat für alles — Auth, Profile, Packs, Decks, Kosmetik, Rang, `sduel`-Duellverkehr. Seit 2026-08-10 trägt `SduelPlayer` zusätzlich `bonusManaPerTurn`/`manaCredit`/`manaDebt` (Mana-Anzeige: Basis vs. temporär vs. Übertrag) und `SduelEvent` beim `pulse` die Felder `effectText`/`effectCost`/`effectInfused` (Effekt-Panel unter der gehobenen Karte). Beide Seiten (DuelHost `DuelSession.EventWire`/Side-Wire ↔ `DuelMirror`/`ServerDuelClient`) müssen solche Felder synchron führen — fehlende Felder sind bei JsonUtility still 0/null, alte Gegenseiten fallen also sanft zurück.
- **`PlayerProfile`** (statisch): der Spiegel des Server-Profils (Sammlung mit Finish-Aufschlüsselung, Decks, Coins, Dust, Banlist, Rang, Kosmetik-Besitz). `Owned(name)` ist die eine Besitz-Frage im Client.
- **`Cosmetics` / `CosmeticArt`**: Katalog kommt vom Server (parallele Arrays), Grafiken liegen im Client unter `Resources/Cosmetics/` nach Konvention `back_<id>`, `mat_<id>`, `coin_<id>_relic/_seal`, `frame_<id>`, `avatar_<id>`, `icon_<id>`. **Fehlende Grafik = stiller Vanilla-Fallback**, nie ein Platzhalter. `IsPlaque(id)` unterscheidet gemalte Bilderrahmen (skalieren aufs ausgemessene **Fenster**, `PlaqueScale`) von Ring-Rahmen.
- **`MatchContext`** (statisch): transportiert Match-Infos zwischen Szenen (Seed, Namen, Kosmetik des Gegners, Server-Match-Flagge).

### 4.4 Editor-Tooling (`Assets/_Game/Scripts/Editor/`)

| Werkzeug | Menü / Zweck |
|---|---|
| `ServerCardExporter` | **„Rouge TCG/Export Server Data"** — schreibt `cards-full.json` (alle Gameplay-Felder, Enums als Namen), `cards.json` (Name→Rarity), `reliquary.json`. Nach JEDEM Karten-Batch ausführen. |
| `ArtworkAssigner` | „Rouge/Card Design/Artworks automatisch zuweisen" — matcht PNGs in `Art/` per Namen (ignoriert Satzzeichen) auf Karten-Assets. |
| `TcgCardPrefabBuilderV3` | baut/aktualisiert das TcgCard-Prefab (Design-Geometrie 480×672, Kompakt 112×157). |
| `NewArchetypeBuilder` u. ä. | Batch-Erzeugung von Karten-Assets per Code. |
| `SteamBuildPostProcessor` | legt `steam_appid.txt` neben die gebaute exe. |

**Asset-Erzeugung per Code — die 61-Karten-Lektion:** `AssetDatabase.CreateAsset` serialisiert **sofort**; Felder, die danach gesetzt werden, leben nur im Speicher. Deshalb: erst alle Felder setzen, dann `CreateAsset`, und am Ende trotzdem für **jedes** Asset `EditorUtility.SetDirty` + `SaveAssets` — und die Werte **auf der Platte** verifizieren (Python über die .asset-YAMLs), nie nur im Speicher.

**Import-Preset-Falle:** neue PNGs bekommen vom Projekt-Preset `Sprite Mode: Multiple` (beschneidet Rects auf die Alpha-Box). Für UI-Sprites per `TextureImporter` auf `Single` zwingen.

---

## 5. Der Node-Server

### 5.1 Prozessbild

`server.js` ist EIN Node-Prozess je Instanz: WebSocket-Server (Port 7777 bzw. 7778), HTTP nur für die Loopback-Admin-API. Er hält alle Accounts **im Speicher** und schreibt sie mit **250 ms Debounce** in SQLite — ein Kill direkt nach einer Änderung kann die letzte Schreibung verlieren (bewusst dokumentierter Trade-off; deshalb Dienste sauber `systemctl restart`en, nie killen).

### 5.2 Nachrichten-Protokoll (Client → Server `m.t`, Auswahl)

`register`/`login`/`steam_login` → `auth_ok`+`profile` · `buy_pack`/`open_pack` (→ `pack_result`) · `craft`/`dust` · `save_deck`/`delete_deck` · `claim_starter` · `buy_cosmetic`/`equip_cosmetic` · `daily` · `queue`/`create`/`join`/`leave` (Matchmaking) · `sduel_intent` (Duell-Antworten) · `feedback`. Antworten tragen immer das volle `profile` mit, wenn sich etwas geändert hat — der Client ersetzt seinen Spiegel komplett statt zu patchen.

### 5.3 Persistenz (`db.js`)

SQLite `/opt/rouge-tcg/data/accounts.db`. **Benannte Spalten, kein JSON-Blob** — jedes neue Account-Feld braucht Spalte + Migration in `db.js` UND ggf. `migrate(acc)` in `server.js` (läuft bei jedem Login; idempotent halten!). Beispiele: `starter_pick`-Spalte; Hero-Nachreichung für Alt-Accounts. Testinstanz hat ihre **eigene DB** unter `/opt/rouge-tcg-test/data/`.

### 5.4 Datenlage (`Server/data/`)

| Datei | Wahrheit über | Erzeuger |
|---|---|---|
| `cards-full.json` | alle Gameplay-Daten (DuelHost liest NUR das) | Unity-Exporter |
| `cards.json` | Name → Rarity (Craft/Dust/Collection-Checks im Relay) | Unity-Exporter |
| `reliquary.json` | Extra-Deck-Namen | Unity-Exporter |
| `packs.json` | Pack-Ökonomie: price, `slots` (Rarity-Array als Zahlen), `legendaryChance`, `cards`-Pool; Unique-Packs: `unique:true` (Hero Cache — zieht nur Fehlendes, Kauf abgelehnt wenn nichts fehlt, Refund beim Öffnen ins Leere) | Hand/Skript |
| `starterdecks.json` | die 5 Startdecks inkl. Hero (Grant vergibt Karten+Extra+**Hero**) | `build-starterdecks.py` |
| `banlist.json` / `banlist-history.json` | Limits 0/1/2 + Chronik | Hand |
| `rules.json`, `starter.json`, `starterdeck.json` | Regeln-Export, Alt-Startsammlung, Selftest-Deck | gemischt |

**Sync-Pflichten** (Anzeige im Client muss zur Server-Wahrheit passen): `packs.json.slots` ↔ `RelicPack.asset.raritySlots`, `legendaryChance` ↔ `legendaryUpgradeChance`, `unique` ↔ `uniqueDraw`, `finishes.js`-Raten (1/12, 1/60, 1/240) ↔ `ShopController.FinishOddsText`, `cosmetics.js`-Katalog ↔ Sprites in `Resources/Cosmetics`.

### 5.5 Admin-API

Nur **Loopback** + Header `x-admin-token` (Token aus `/etc/rouge-tcg.env`). `GET /admin/players`, `GET/POST /admin/player` (coins setzen, dust, cards ±N mit Finish), `GET /admin/cards`. Das Admin-Panel auf TeamTycoon.de (mcweb) und die Wartungs-Skripte laufen darüber — **Änderungen an laufenden Accounts immer über diese API**, nie an der DB vorbei (der Prozess würde sie überschreiben).

### 5.6 DuelHost-Bridge

Der Relay hält **eine** TCP-Verbindung zum DuelHost (`127.0.0.1:7900`). `sduel`-Nachrichten der Clients werden durchgereicht; der Host schickt `state`/`request`/`events`/`log`/`end` zurück, der Relay routet an die beiden Sitze. **Wer sich direkt auf 7900 verbindet, verdrängt Node und killt laufende Duelle** — private Tests immer gegen einen eigenen Host auf einem anderen Port.

---

## 6. Der DuelHost

`Server/duelhost/DuelHost.csproj` (net8.0) kompiliert die Engine-Quellen **per Compile-Include direkt aus dem Unity-Ordner**. Konsequenzen:

- Jede Engine-Änderung ⇒ `dotnet build -c Release` ⇒ neue `DuelHost.dll` deployen.
- `CardLibrary` lädt `cards-full.json` und befüllt die Karten-Objekte **per Reflection über Feldnamen** — neue serialisierte Felder fließen ohne Host-Änderung mit, solange der Exporter sie schreibt.
- Kein Unity-Typ in der Engine (s. o.).

Bausteine: `Program.cs` (Args: `--serve --data <dir> --port <p>`, `--selftest [--log]`), `DuelSession.cs` (eine Partie: Wire-Serialisierung, **Maskierung** verdeckter Information je Sicht — Handkarten, verdeckte Karten, Finishes verdeckter Karten), `ServerPresenter.cs` (IDuelPresenter → Event-Liste: `draw`, `summon`, `attack`, `chainlink` mit `link`-Nummer, …).

**Selftest & Proben:** `--selftest` spielt Bot vs Bot mit `starterdeck.json` aus dem Datenordner (fester Seed ⇒ deterministisch) und gibt mit `--log` das ganze Duell-Protokoll aus. Bewährtes Muster für gezielte Tests: einen **Probe-Datenordner** bauen (Kopie von `cards-full.json` + manipulierte `starterdeck.json`, z. B. anderer Hero oder editierte Effekte zum Bisektieren) und mit `--data` darauf zeigen. Immer mit `timeout` laufen lassen — eine Endlosschleife im Regelwerk sieht sonst wie ein Hänger aus (und `time` misst unter Git-Bash nur den Wrapper, nicht dotnet!).

---

## 7. Deployment & Betrieb

### 7.1 Der vServer (217.154.212.82)

| Dienst | Port | Verzeichnis | Zweck |
|---|---|---|---|
| `rouge-tcg` | **7777** | `/opt/rouge-tcg/` | Produktions-Relay |
| `rouge-duelhost` | 7900 (lokal) | `/opt/rouge-duelhost/` | Produktions-Duelle |
| `rouge-tcg-test` | **7778** | `/opt/rouge-tcg-test/` | Test-Relay (eigene DB!) |
| `rouge-duelhost-test` | 7901 (lokal) | `/opt/rouge-duelhost-test/` | Test-Duelle |
| `mcweb` | 3000 | `/opt/mcweb/` | TeamTycoon-Website inkl. Admin-Panel & Feedback |

Env-Dateien: `/etc/rouge-tcg.env` (root:root 600 — ADMIN_TOKEN, Steam-Key, DUELHOST_PORT je Instanz), `/etc/reliquary/admin.env` (root:mcweb 640). **Nichts davon je ins Repo.**

### 7.2 Deploy-Muster

```
scp <dateien> root@217.154.212.82:/tmp/
ssh root@… "install -o rouge -g rouge -m 644 /tmp/<datei> /opt/<ziel> && systemctl restart <dienst>"
```

Regeln:
1. **Erst Test (7778/7901), selbst spielen, dann Produktion.** Der Testserver existiert genau dafür — Spieler auf 7777 merken vom Basteln nichts.
2. Vor jedem Produktions-Restart: `ss -tn state established '( sport = :7777 )'` — **niemand online?** Dann restart (dauert ~3 s).
3. Vor dem Überschreiben Sicherungskopien anlegen (`*.bak-<tag>` liegen schon dort als Vorbild).
4. SSH nur aus **PowerShell** (Git-Bash-SSH scheitert am Publickey). Mehrzeiler als lokale `.sh` per `Write` (ohne BOM) erzeugen und scp'en.
5. **NIEMALS `pkill -f 'node server.js'`** — das Muster trifft die Produktion.

### 7.3 Was gehört bei einem Release zusammen?

Client-Build und Serverdaten sind ein Paar. Checkliste in Reihenfolge:

1. Engine geändert? → DuelHost bauen + Selftest grün.
2. Karten geändert? → **Export Server Data** ausführen.
3. `bundleVersion` setzen (Shop liest `Application.version` selbst), Changelog-Block oben in `Resources/PatchNotes.txt`.
4. `useTestServer`-Haken in der Login-Szene **aus**.
5. Build (BuildPipeline; die exe/UnityPlayer.dll behalten alte Zeitstempel — das ist normal, es zählen `RougeTCG_Data/*`), Zip ohne `*_BurstDebugInformation_DoNotShip`.
6. Version-String im Build verifizieren (`globalgamemanagers` enthält `0.x.y`).
7. Server-Sync: `server.js`, `cosmetics.js`, `data/*.json`, `DuelHost.dll` — erst Test, dann Produktion (Duelhost vor Relay restarten, dann verbindet sich Node sauber neu).
8. Commit + Push; Zip verteilen; Steam-Post.

### 7.4 Test-Rezepte

- **Solo-Duell** rechnet lokal — für Engine-/UI-Tests reicht der Editor komplett ohne Server.
- **Play-Mode ist über MCP nicht verifizierbar** (Bridge stirbt ohne Fensterfokus). Stattdessen: UI in Edit-Mode über RenderTexture screenshotten (Canvas → ScreenSpaceCamera zwingen, rendern, zurückstellen; Canvas füllt IMMER die RenderTexture — Ausschnitte nur im Pixelraum schneiden) und Spiellogik über den DuelHost-Selftest fahren.
- **Konto-Manipulation zum Testen** (Coins, Karten geben/nehmen): Admin-API-Skripte auf dem Server (Muster in dieser Session: give-coins/give-heroes).

---

## 8. Kochbuch: wiederkehrende Arbeiten

### 8.1 Neue Karte(n)

1. Design **zuerst dem Designer zeigen** (Projektregel), Effekte kategoriebasiert („ein Wasser-Level-2-Monster"), Burn sparsam.
2. Asset anlegen (Editor-Skript-Batch oder CreateAssetMenu) — Felder VOR `CreateAsset`, danach SetDirty-Schleife + Disk-Verifikation.
3. In den `CardCatalog` hängen.
4. Artwork: PNG exakt nach Kartennamen → `Art/` → ArtworkAssigner.
5. **Export Server Data**; wenn die Karte in Packs ziehbar sein soll: Pool in `packs.json` ergänzen (Heroes NICHT — die sind Cache-only).
6. DuelHost-Selftest; bei neuen Mechaniken eine Bot-Probe mit passendem Probedeck.
7. Test-Server bestücken → spielen → Produktion.

### 8.2 Neue Effekt-Action

1. `EffectActionType` **ans Ende** anhängen (+ ggf. `TargetKind` ans Ende).
2. Implementierung als `case` in `ResolveEffectActions` (DuelActions), Log-Zeile auf Englisch.
3. Braucht sie Kandidaten: `BuildTargetCandidates` erweitern; Filter über `MatchesFilter`.
4. DuelHost neu bauen; Probe schreiben, die die Action wirklich feuert (Bots aktivieren nur, was `ActivatableEffects`+`HasValidTargets` anbietet — leere Kandidaten = nie getestet).

### 8.3 Neuer Held

Asset in `Data/Tcg/PlayerCards/` (Familienregel: 2 Mana, once per turn; LP als Preis), Katalog, Export, **Hero-Cache-Pool** in `packs.json` ergänzen, Bot-Probe je Held (starterdeck-Probe mit `hero`-Feld), Artwork + Amuse-Prompt (epische Untersicht: „gigantic/titanic, view from below, epic composition").

### 8.4 Neue Kosmetik

`cosmetics.js`: Item mit id/slot/name/rarity/price (Preis-Staffel des Fachs respektieren; Kommentar-Zählung oben pflegen). Sprites nach Namenskonvention in `Resources/Cosmetics/` (Import: Sprite/Single!). Neuer Slot? → SLOTS+SLOT_NAMES (Server), SlotNames (Client `Cosmetics.cs`), Sektion in `CosmeticsPanel`, Loader in `CosmeticArt`, Anzeige-Ort. Katalog erst mit dem **Build** in die Produktion — alte Clients zeigen sonst leere Kacheln.

### 8.5 Balancing

Zahlen leben in Assets (`GameRules`, Karten, `packs.json`) — Balancing braucht **keinen** Codechange, aber immer: Export → DuelHost-Daten aktualisieren → beide Seiten der Sync-Pflichten (5.4) prüfen.

---

## 9. Invarianten & Fallstricke

Die Liste der Dinge, die genau einmal wehgetan haben:

1. **Enums nur anhängen** (3.2) — sonst vertauschen alle Assets ihre Effekte.
2. **`CreateAsset` serialisiert sofort** — SetDirty-Schleife + Disk-Verifikation nach jedem Batch (die 61-Karten-Lektion: alles sah im Speicher richtig aus und war auf der Platte leer).
3. **`responseDepth` ≠ `chainDepth`** — Trigger mitten in Auflösungen gehen an Fenstern vorbei; UI-Ketten-Logik immer an `chainDepth`.
4. **Ersatz-Zerstörung ist endgültig** — sonst Prisma-Endlosschleife, Host friert ein.
5. **Kosten fallen vor Reaktionen und werden nie erstattet** — `isCost` ist Semantik, nicht Deko.
6. **Server-Presenter darf nicht auf `IsLocal` schauen** — `LocalPlayer` ist dort null; Besitzer als `PlayerState` durchreichen.
7. **Accounts-DB hat benannte Spalten** — neues Feld = Spalte + Migration + idempotente `migrate()`; Reparaturen dürfen bei jedem Login laufen, aber nur einmal wirken.
8. **250-ms-Schreibdebounce** — Prozess nie hart killen; Tests nicht direkt nach der letzten Aktion beenden.
9. **Ein-Verbindungs-Regel des DuelHost** — nie direkt auf 7900 verbinden; Testinstanz nutzen.
10. **`useTestServer` vor jedem Build aus** — und der Testserver hat eine eigene, leere DB (Accounts existieren dort separat).
11. **Grants müssen ALLES vergeben, was die Client-Prüfung verlangt** — der Starterdeck-Grant ohne Hero hat jeden neuen Account blockiert; `DeckIsOwned` prüft Deck **und** Hero.
12. **Angebotene UI muss Besitz respektieren** — der Deck-Builder bot alle 12 Helden an und Solo lehnte dann kryptisch ab; Besitzfilter gehören an die Quelle der Auswahl.
13. **Layout-Zellen sind beim Aufbau 0 breit** — `TcgCardView` vor `Show()` feste Maße geben, sonst Kompaktmodus-Roulette.
14. **Overlays brauchen eigene Canvas-Sortierung** (`overrideSorting`), Geschwister-Reihenfolge reicht nicht.
15. **Import-Preset setzt neue Sprites auf Multiple** — Rects werden beschnitten; auf Single zwingen.
16. **TMP-Sonderzeichen** nur aus „Symbols SDF" — Glyphen auf Atlas-Textur 2 zeichnen leer.
17. **AssetDatabase ist Editor-only** — Laufzeit lädt über `Resources` (TransitionSkin-Muster); `CardSkin` liegt bewusst NICHT in Resources.
18. **Unique-Packs**: Kauf ablehnen, wenn der Pool leer ist, und beim Öffnen ins Leere erstatten — der Randfall „zwei gekauft, nach dem ersten ist alles da" existiert wirklich.
19. **Odds-Anzeigen sind Verträge** — jede Zahl im Client (Slots, Legendary-Chance, Finish-Raten) hat eine Server-Quelle; Änderungen immer paarweise.
20. **Timeout um jeden Selftest** — und Misstrauen gegen `time`-Werte von Wrappern.

---

## 10. Sicherheit

- **Steam Publisher-Key**: nur in `/etc/rouge-tcg.env` (root:root 600, via systemd `EnvironmentFile`). Nie im Repo, nie im Client. Ticket-Prüfung ausschließlich serverseitig.
- **`STEAM_DEV_MODE=1`** akzeptiert ungeprüfte `dev:<steamid>`-Tickets — niemals in Produktion setzen.
- **Steam- und Passwort-Accounts sind strikt getrennt** — kein Linking, kein nachträgliches Passwort auf Steam-Accounts.
- **`ADMIN_TOKEN`**: nur in den Env-Dateien; API nur über Loopback; Token nie in Logs oder Kommandozeilen echoen (Skripte lesen ihn auf dem Server aus der Datei).
- `.gitignore` deckt `*.env`; Backups der DB bleiben auf dem Server.

---

*Ende. Wenn dich diese Doku angelogen hat, hat sich der Code geändert — dann bitte hier nachziehen, die Datei ist Teil des Repos.*
