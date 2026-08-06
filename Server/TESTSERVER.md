# Testserver — Änderungen ausprobieren, ohne die Produktion anzufassen

Auf dem vServer laufen zwei vollständig getrennte Instanzen:

| | Produktion | Test |
|---|---|---|
| Relay (Node) | `rouge-tcg`, Port **7777** | `rouge-tcg-test`, Port **7778** |
| DuelHost (.NET) | `rouge-duelhost`, Port 7900 (lokal) | `rouge-duelhost-test`, Port 7901 (lokal) |
| Verzeichnis | `/opt/rouge-tcg` | `/opt/rouge-tcg-test` |
| DuelHost-Verzeichnis | `/opt/rouge-duelhost` | `/opt/rouge-duelhost-test` |
| Datenbank | echte Accounts | eigene, wegwerfbar |

Die Testinstanz hat ihre eigene `accounts.db` (leer gestartet), eigene
Spieldaten-JSONs und schreibt Feedback in eine eigene Datei statt ins Panel
der Website. Steam-Login funktioniert auch dort (gleiche Ticket-Prüfung).
Der Port ist über `PORT`, der DuelHost über `DUELHOST_PORT` in der
Service-Unit gesetzt — `server.js` liest beide aus der Umgebung.

## Warum

Wer `server.js` oder den DuelHost direkt in Produktion aktualisiert, zwingt
alle Spieler auf den neuen Stand, bevor der passende Build überhaupt
hochgeladen ist — wer gerade im Spiel ist, kann dann nichts mehr machen.
Deshalb: **erst auf Test ausprobieren, Produktion erst zusammen mit dem
Build-Upload aktualisieren.**

## Unity gegen den Testserver richten

Login-Szene → `NetworkManager` → Haken **Use Test Server**.

Der Haken wirkt nur im Editor (`Application.isEditor`) — ein Build ignoriert
ihn immer und redet grundsätzlich mit Produktion. Man kann ihn also nicht
versehentlich ausliefern. Im Editor-Log steht beim Start eine Warnung
`TESTSERVER aktiv`, damit man weiss, wo man gerade spielt.

## Server-Änderungen auf Test ausrollen

```bash
scp Server/server.js root@217.154.212.82:/tmp/server.js
ssh root@217.154.212.82 "install -o rouge -g rouge -m 644 /tmp/server.js /opt/rouge-tcg-test/server.js && systemctl restart rouge-tcg-test"
```

Dasselbe Muster für `db.js`, `ranks.js`, `finishes.js`, `cosmetics.js` und
Daten-JSONs (Ziel `/opt/rouge-tcg-test/data/`).

## DuelHost-Änderungen auf Test ausrollen

```bash
dotnet build Server/duelhost/DuelHost.csproj -c Release
scp Server/duelhost/bin/Release/net8.0/DuelHost.dll root@217.154.212.82:/tmp/DuelHost.dll
ssh root@217.154.212.82 "install -o rouge -g rouge -m 644 /tmp/DuelHost.dll /opt/rouge-duelhost-test/DuelHost.dll && systemctl restart rouge-duelhost-test"
```

## Wenn alles passt: Produktion

Erst den Build hochladen bzw. verteilen, dann dieselben Dateien nach
`/opt/rouge-tcg` bzw. `/opt/rouge-duelhost` und die Produktionsdienste
neu starten. Vorher prüfen, ob jemand online ist:

```bash
ssh root@217.154.212.82 "ss -tn state established '( sport = :7777 )' | tail -n +2 | wc -l"
```

## Test-Datenbank zurücksetzen

```bash
ssh root@217.154.212.82 "systemctl stop rouge-tcg-test && rm -f /opt/rouge-tcg-test/data/accounts.db* && systemctl start rouge-tcg-test"
```

Die Instanz legt beim Start eine frische DB an. Test-Accounts sind
Wegwerfware — nichts davon zählt.

## Wichtige Regeln

- **Niemals** `pkill -f 'node server.js'` — das trifft auch die Produktion.
  Immer `systemctl restart rouge-tcg-test` (bzw. `rouge-tcg`).
- Der DuelHost nimmt genau EINE Verbindung an. Die Testinstanz hat ihren
  eigenen auf 7901 — nie einen Client direkt auf 7900 richten, das wirft
  die Produktion raus.
- `STEAM_DEV_MODE` bleibt überall aus, auch auf Test.
