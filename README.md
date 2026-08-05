# RELIQUARY

Ein Online-Sammelkartenspiel in Unity 6.4 (2D-URP) mit server-autoritativen
Duellen. Der Client zeigt an, gerechnet wird auf dem Server.

## Aufbau

| Teil | Wo | Was |
|---|---|---|
| Client | `Assets/_Game/` | Unity-Projekt: Szenen, Skripte, Karten, Grafiken, Sounds |
| Duell-Engine | `Assets/_Game/Scripts/Tcg/Engine/` | Regeln, Phasen, Effekte — **kein MonoBehaviour** |
| Lobby-Server | `Server/` | Node.js: Accounts, Sammlung, Shop, Matchmaking (Port 7777) |
| Duell-Host | `Server/duelhost/` | .NET 8, rechnet die Duelle (Port 7900, nur lokal) |
| Design-Vorlagen | `design handoff */` | Die Handoffs, nach denen gebaut wird |

**Die Engine liegt nur einmal vor.** Der DuelHost kompiliert dieselben Quellen
direkt aus dem Unity-Projekt (`<Compile Include="..\..\Assets\_Game\Scripts\Tcg\Engine\**\*.cs" />`).
Wer dort etwas ändert, muss den DuelHost neu bauen und ausrollen — sonst laufen
Client und Server auf verschiedenen Regeln.

## Bauen

**Client** — im Editor über `File ▸ Build Settings`, Ziel `Build/RougeTCG/`.

**DuelHost**

```bash
cd Server/duelhost && dotnet build -c Release
```

Es gibt einen Selbsttest, der ein vollständiges Bot-gegen-Bot-Duell rechnet.
Nach jedem Eingriff in die Engine lohnt er sich:

```bash
cd Server/duelhost && dotnet run -c Release -- --selftest --data ../data
```

## Was nicht im Repo liegt

- `Library/`, `Temp/`, `Logs/`, `obj/` — stellt Unity selbst wieder her
- `Build/` — die fertigen Zips
- `sdk/` — das Steamworks SDK, bei Valve zu holen (siehe `Server/STEAM-SETUP.md`)
- `*.env`, `Server/data/*.db` — Zugangsdaten und Live-Daten

**Der Steam-Web-API-Key steht an keiner Stelle im Baum.** Er wird ausschliesslich
über `process.env` gelesen und liegt auf dem Server in `/etc/rouge-tcg.env`
(root:root, `chmod 600`). Ein Key im Client-Build könnte jeden Spieler
imitieren — das darf nie passieren.

## Generierte Grafiken

Kartenrahmen, Münzen, Wappen und die komplette Kosmetik sind **prozedural**, nicht
gemalt. Sie entstehen über zwei Menüpunkte im Editor:

- `Rouge/Card Design/…` → `Assets/_Game/Scripts/Editor/CardDesignGenerator.cs`
- `Rouge/Cosmetics/Generate Art` → `Assets/_Game/Scripts/Editor/CosmeticArtGenerator.cs`

Beim Ändern zu beachten: `SaveSprite` setzt bewusst `mipmapEnabled = false`,
Bilinear und unkomprimiert bis 900 px. Mipmaps machen die Oberfläche selbst bei
1:1-Darstellung unscharf, DXT setzt Klötzchen in feine Verläufe.

## Stand und offene Punkte

Siehe [OFFENE-PUNKTE.md](OFFENE-PUNKTE.md). Die Patchnotes des Spiels stehen in
`Assets/_Game/Resources/PatchNotes.txt` und sind im Hauptmenü unter NEWS zu lesen.
