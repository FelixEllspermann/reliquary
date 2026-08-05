# Offene Punkte — Stand 2026-08-05

Der Build **0.1.0f9** liegt unter `Build/Reliquary-0.1.0f9.zip`.
Server (`cosmetics.js`, `server.js`) und DuelHost laufen auf 217.154.212.82 mit
den passenden Gegenstücken.

---

## Offen

### 1 · #96 Deck-Builder-Finishes im laufenden Client prüfen
Gebaut, kompiliert, aber nie in einem laufenden Client gesehen.

### 2 · Nichts davon ist gespielt
Aktivierung, Zielwahl, Zerstörung, Tresor, Niederlage, die fünf Siegessiegel und
der neue Münzwurf sind einzeln gerendert geprüft — aber **nie im laufenden
Duell**. Ein Solo-Duell gegen einen Bot zeigt fast alles auf einmal.

Nur mit zwei Clients am selben Server prüfbar: geteilte Spielmatte, Kartenrücken
des Gegners, Ereignis-Reihenfolge, Deck-Suche.

Falls der Multiplayer schlechter läuft als vorher: die vorletzte DuelHost-DLL
liegt als `/opt/rouge-duelhost/DuelHost.dll.bak-mpfix`.

### 3 · Reliquary-Kartenrahmen  ← FREIGEGEBEN, als Nächstes
Violette Akzente sind vom User abgesegnet: violette Namensplatte, Typband
„RELIQUARY", violettes Wappen, passender Kartenrücken.

Das Chassis existiert bereits in Elfenbein/Gold — `CardDesignGenerator.Reliquary()`
ist die Palette, `GenerateReliquary()` erzeugt Chassis, Badge, Kompaktkarte und
Extra-Deck-Ablage. Zu tun ist also nur die Palette auf Violett zu ziehen und ein
eigenes Wappen (`BuildCrest`) plus Kartenrücken zu ergänzen — kein neuer
Generator, ein Durchgang mit `Rouge/Card Design/Generate Reliquary Assets`.

---

## Erledigt in dieser Sitzung

| Punkt | Wo |
|---|---|
| Münzwurf im Multiplayer | `DuelPresenter.ShowCoinToss(winner)` als Overlay, Gewinner im `cointoss`-Ereignis |
| Unbekannte Karten in Anfragen | `DuelMirror.MirrorCandidate` — war die leere Auswahl beim Suchzauber |
| Unbekannte Karten in Ereignissen | `DuelMirror.MirrorEventCard` — fehlende Animationen bei Extra-Deck-Karten |
| Ereignis-/Zustands-Reihenfolge | `DuelSession.OnBoardChanged` schickt an der Naht |
| Kein Duell-Hänger mehr | `ServerDuelClient.HandleRequest` beantwortet, was es nicht lesen kann |
| Alt-Tab | `runInBackground: 1` |
| Extra-Deck-Anker | `p1ExtraAnchor` → BottomExtraPile, `p2ExtraAnchor` → TopExtraPile |
| Thorn Setting | Dornen durchbrechen den Umriss jetzt nach aussen |
| Siegel für beide | `VictorySealSequence.PlayForLoser` am Niederlage-Bildschirm |
| **Münzwurf vereinheitlicht** | Overlay bekam Vignette, Glut, Caption, Kamerafahrt und Ergebnis-Banner; CoinToss-Szene und -Controller gelöscht |
| **Lockstep raus** | `startMatch` lehnt ab statt zurückzufallen; `RemoteDuelController`, `INetSession`, `NetworkLoopbackTest`, `StartNetworkDuel` und die Lockstep-Felder in `MatchContext` gelöscht |
| Shop | scrollt, filtert, zeigt alle 33 statt 20 |
| News | scrollt (dem Scrollbereich fehlte ein Raycast-Empfänger) |

---

## Wo was liegt

| Was | Wo |
|---|---|
| Kosmetik-Grafiken erzeugen | Menü `Rouge/Cosmetics/Generate Art` → `CosmeticArtGenerator.cs` |
| Karten-/Münz-Sprites erzeugen | Menü `Rouge/Card Design/…` → `CardDesignGenerator.cs` |
| Grafik zur Kosmetik-Id finden | `Assets/_Game/Scripts/Tcg/Net/CosmeticArt.cs` |
| Siegessiegel | `Assets/_Game/Scripts/Tcg/UI/VictorySealSequence.cs` |
| Katalog und Preise | `Server/cosmetics.js` (deployt nach `/opt/rouge-tcg/`) |
| Patchnotes | `Assets/_Game/Resources/PatchNotes.txt` |

**Beim Sprite-Erzeugen:** `SaveSprite` setzt `mipmapEnabled = false`, Bilinear und
unkomprimiert bis 900 px. Vorher stand dort `mipmapEnabled = true` plus
Trilinear — das war die eigentliche Ursache der Unschärfe, und es hätte jede
Import-Korrektur bei der nächsten Neugenerierung wieder überschrieben.

**Nach `Generate Art`:** die vier kopierten Vanilla-Münz-Sprites
(`coin_vanilla_relic/_seal`, `coin_shadow`, `coin_dustring`) unter
`Resources/Cosmetics` prüfen — der Presenter findet die Standardmünze nur dort.
