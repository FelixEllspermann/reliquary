# Steam-Anmeldung — Aufbau und Betrieb

RELIQUARY kennt **zwei Anmeldewege**, die auf demselben Account-System sitzen:

1. **Duellisten-Name + Passwort** (wie bisher, funktioniert überall)
2. **Steam** — ein Klick, kein Passwort; nur in der Steam-Version sichtbar

Ein Account kann beides haben. Der Login-Screen blendet den Steam-Knopf nur ein,
wenn Steam wirklich läuft — in einer Version ohne Steam gibt es keinen toten Knopf.

---

## Wie die Anmeldung abläuft

```
Client                        Node-Server                      Valve
  |  SteamUser.GetAuthSessionTicket()
  |  -> Hex-Ticket
  |---- steam_auth(ticket) ----->|
  |                              |--- AuthenticateUserTicket --->|
  |                              |<-- SteamID + Ban-Status ------|
  |                              | Account suchen/anlegen
  |<--- auth_ok(profile) --------|
```

**Der Client kann dabei nicht lügen.** Die SteamID kommt ausschliesslich aus
Valves Antwort, nie aus der Nachricht des Clients. Ein gefälschtes Ticket
scheitert bei Valve, ein fremdes Ticket gehört zu einem anderen Account.

Der Web-API-Key liegt **nur auf dem Server** und darf niemals in einen
Client-Build geraten — mit ihm könnte man sich sonst als beliebiger Spieler
ausgeben.

---

## Serverseite (ist eingerichtet)

| Was | Wo |
|---|---|
| App-ID + Web-API-Key | `/etc/rouge-tcg.env` (root:root, `chmod 600`) |
| Einbindung | `/etc/systemd/system/rouge-tcg.service.d/20-steam.conf` → `EnvironmentFile=` |
| Prüfung | `verifySteamTicket()` in `server.js` |
| Speicherung | Spalte `accounts.steam_id`, eindeutiger Index |

Beim Start meldet der Server:

```
Steam-Anmeldung: aktiv (App-ID 4775350)
```

Steht dort *nicht konfiguriert*, fehlen die Umgebungsvariablen.

### Nachrichten

| Nachricht | Richtung | Wirkung |
|---|---|---|
| `steam_auth`  | Client → Server | Anmelden; legt beim ersten Mal einen Account an |
| `steam_link`  | Client → Server | Steam mit dem eingeloggten Account verknüpfen |
| `set_password`| Client → Server | Passwort für einen Steam-Account setzen |

Regeln, die der Server durchsetzt:

* Eine SteamID gehört zu **höchstens einem** Account (Datenbank-Index).
* Ein Account ohne Passwort (`hash` leer) kann sich **nicht** über das Formular
  anmelden — die Antwort lautet dann „This account signs in through Steam."
* Ein Doppelklick auf den Steam-Knopf legt keine zwei Accounts an
  (`c.steamPending`).

### Namensvergabe

Neue Steam-Accounts bekommen den Steam-Anzeigenamen, bereinigt und auf 16 Zeichen
gekürzt. Ist er vergeben, hängt der Server eine Zahl an (`Vault Hunter`,
`Vault Hunter2`, …). Der Spieler kann später einen eigenen Namen bekommen —
dafür gibt es bisher **keine** Umbenennen-Funktion, das wäre der nächste Schritt.

---

## Clientseite

* `Assets/_Game/Scripts/Tcg/Net/SteamBridge.cs` — kapselt alles Steam-spezifische
* Der Steam-Code steckt hinter `#if ROUGE_STEAM`; **ohne** dieses Define
  kompiliert und läuft das Projekt ganz normal weiter, nur ohne Steam.
* `steam_appid.txt` (Inhalt: `4775350`) muss liegen:
  * neben der Unity-Projektwurzel — für den Editor
  * **neben der gebauten `RougeTCG.exe`** — sonst startet die Steam-API nicht
  * Nach der Veröffentlichung auf Steam ist die Datei nicht mehr nötig (Steam
    liefert die App-ID dann selbst) und sollte entfernt werden.

---

## Testen ohne Steam-Client

Für Tests gibt es einen Entwicklungs-Modus, der Tickets **ungeprüft** annimmt:

```bash
STEAM_DEV_MODE=1 PORT=7778 node server.js
node test/smoke-steam.mjs ws://127.0.0.1:7778
```

Ein Ticket sieht dann so aus: `dev:76561198000000001`.

> **Niemals produktiv einschalten.** Mit `STEAM_DEV_MODE=1` kann sich jeder als
> beliebige SteamID ausgeben. Der Server schreibt bei jeder Nutzung eine
> Warnung ins Log, und beim Start steht die Zeile
> „ACHTUNG: STEAM_DEV_MODE ist an" — auf dem Produktivserver ist er **aus**.

---

## Was noch offen ist (vor der Veröffentlichung)

- [ ] **Store-Seite und Depots** in Steamworks anlegen, Build hochladen
- [ ] **Umbenennen-Funktion** für Steam-Accounts (aktuell nur der Steam-Name)
- [ ] **Steam entkoppeln** aus den Einstellungen (aktuell nur verknüpfen)
- [ ] **Steam-Overlay** testen — bei Vollbild kann es sich mit dem eigenen
      Cursor beissen
- [ ] Optional: Steam-Achievements, Rich Presence, Cloud-Saves
- [ ] `steam_appid.txt` aus dem Release-Build entfernen
