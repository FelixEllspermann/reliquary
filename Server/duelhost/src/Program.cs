// Rouge DuelHost — rechnet Duelle server-autoritativ mit der geteilten Engine.
//   --selftest          Bot-vs-Bot-Duell aus cards-full.json, synchron, mit Ausgabe
//   --serve             TCP-Brücke für den Node-Server (Standard)
//   --data <dir>        Datenverzeichnis (cards-full.json, starterdeck.json)
//   --port <port>       Brücken-Port (Standard 7900, nur 127.0.0.1)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Rouge.Tcg;

namespace Rouge.DuelHost
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string dataDir = ReadArg(args, "--data") ?? "/opt/rouge-tcg/data";
            int port = int.TryParse(ReadArg(args, "--port"), out int p) ? p : 7900;

            var library = CardLibrary.Load(dataDir);
            Console.WriteLine($"[host] {library.Catalog.cards.Count} Karten geladen, Regeln: startMana {library.Rules.startMana}, Deck {library.Rules.deckMinSize}-{library.Rules.deckMaxSize}");

            // The Forbidden Name: Namenspool für "declare a card name" — gleiche
            // Liste und Sortierung wie der Client (CardLinkText.Configure).
            DuelManager.DeclarableNames.Clear();
            foreach (var card in library.Catalog.cards)
            {
                if (card == null || card is PlayerCardData || card.isToken) continue;
                DuelManager.DeclarableNames.Add(card.cardName);
            }
            DuelManager.DeclarableNames.Sort(StringComparer.Ordinal);

            if (args.Contains("--selftest")) return SelfTest(library, dataDir);

            var server = new HostServer(port);
            server.Start();
            Console.WriteLine($"[host] DuelHost lauscht auf 127.0.0.1:{port}");

            var sessions = new Dictionary<string, DuelSession>();
            while (true)
            {
                bool worked = false;
                while (server.TryDequeue(out var doc))
                {
                    worked = true;
                    using (doc) Handle(doc.RootElement, library, sessions, server);
                }

                foreach (var session in sessions.Values.ToList())
                {
                    session.Pump();
                    session.Flush();
                    if (session.Finished)
                    {
                        sessions.Remove(session.Id);
                        Console.WriteLine($"[host] Duell {session.Id} beendet — {sessions.Count} aktiv.");
                    }
                }

                if (!worked) Thread.Sleep(10);
            }
        }

        private static void Handle(JsonElement msg, CardLibrary library,
            Dictionary<string, DuelSession> sessions, HostServer server)
        {
            string op = msg.GetProperty("op").GetString();
            string duelId = msg.TryGetProperty("duelId", out var d) ? d.GetString() : null;

            switch (op)
            {
                case "start":
                {
                    var session = new DuelSession(duelId, library,
                        (side, payload) => server.Send(WithSide(payload, side)));
                    sessions[duelId] = session;
                    try
                    {
                        session.Start(msg);
                        Console.WriteLine($"[host] Duell {duelId} gestartet — {sessions.Count} aktiv.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[host] Start von {duelId} fehlgeschlagen: {e.Message}");
                        sessions.Remove(duelId);
                        server.Send(new { op = "error", duelId, message = e.Message });
                    }
                    break;
                }
                case "intent":
                    if (duelId != null && sessions.TryGetValue(duelId, out var forIntent))
                        forIntent.ApplyIntent(msg.GetProperty("side").GetString(), msg.GetProperty("answer"));
                    break;
                case "leave":
                    if (duelId != null && sessions.TryGetValue(duelId, out var forLeave))
                        forLeave.Forfeit(msg.GetProperty("side").GetString());
                    break;
                case "poke":
                    // Ein Zuschauer ist beigetreten — beim nächsten Flush geht ein
                    // frischer State raus (auch an A/B, das ist idempotent).
                    if (duelId != null && sessions.TryGetValue(duelId, out var forPoke))
                        forPoke.Poke();
                    break;
                case "ping":
                    server.Send(new { op = "pong" });
                    break;
            }
        }

        /// <summary>Payload um die Ziel-Seite ergänzen, damit Node weiß, an wen es geht.</summary>
        private static object WithSide(object payload, string side)
        {
            var json = JsonSerializer.SerializeToNode(payload);
            json["to"] = side;   // null = beide Spieler
            return json;
        }

        // ================== SELBSTTEST ==================

        // Zusatz-Schalter für Karten-Proben:
        //   --deck <file>   Deckdatei statt starterdeck.json (gleiches Format: cards/extra/hero)
        //   --games <n>     n Duelle hintereinander mit fortlaufenden Seeds (Standard 1)
        //   --seed <n>      Start-Seed (Standard 7777)
        //   --log           komplettes Protokoll des LETZTEN Duells ausgeben
        //   --grep <text>   nur Logzeilen mit diesem Text ausgeben (aller Duelle)
        private static int SelfTest(CardLibrary library, string dataDir)
        {
            var args = Environment.GetCommandLineArgs();
            string deckPath = ReadArg(args, "--deck") ?? Path.Combine(dataDir, "starterdeck.json");
            int games = int.TryParse(ReadArg(args, "--games"), out int g) ? Math.Max(1, g) : 1;
            int seed = int.TryParse(ReadArg(args, "--seed"), out int s) ? s : 7777;
            string grep = ReadArg(args, "--grep");

            var starter = JsonDocument.Parse(File.ReadAllText(deckPath)).RootElement;
            var deckNames = starter.GetProperty("cards").EnumerateArray().Select(c => c.GetString()).ToList();
            string heroName = starter.TryGetProperty("hero", out var h) ? h.GetString() : null;

            // Extra Deck aus derselben Datei, wenn eines drinsteht. Ohne das kann
            // der Selftest keine einzige Reliquary erreichen — und damit auch
            // keine Beschwörungs-Bedingung und keine Beschwörungs-Kosten.
            var extraNames = starter.TryGetProperty("extra", out var e) && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Select(c => c.GetString()).ToList()
                : new List<string>();

            foreach (var name in deckNames.Concat(extraNames))
                if (library.Catalog.FindByName(name) == null)
                    Console.WriteLine($"[selftest] WARNUNG: Karte \"{name}\" nicht im Katalog.");

            int failures = 0;
            var totalWatch = System.Diagnostics.Stopwatch.StartNew();
            for (int game = 0; game < games; game++)
            {
                var deckA = library.Catalog.ResolveList(deckNames);
                var deckB = library.Catalog.ResolveList(deckNames);
                var extraA = library.Catalog.ResolveList(extraNames);
                var extraB = library.Catalog.ResolveList(extraNames);
                var hero = library.Catalog.FindByName(heroName) as PlayerCardData;

                var duel = new DuelManager(new DuelConfig { Rules = library.Rules, BotActionDelay = 0f });
                var watch = System.Diagnostics.Stopwatch.StartNew();
                string crash = null;
                try
                {
                    duel.StartServerDuel(seed + game,
                        "Selftest-A", deckA, extraA, hero, new BotDuelController(),
                        "Selftest-B", deckB, extraB, hero, new BotDuelController(),
                        true);
                }
                catch (Exception ex)
                {
                    crash = ex.ToString();
                }
                watch.Stop();

                bool ok = crash == null && duel.Result != DuelResult.None;
                if (!ok) failures++;
                Console.WriteLine($"[selftest] #{game + 1} seed {seed + game}: {(crash != null ? "EXCEPTION" : duel.Result.ToString())} nach {duel.TurnNumber} Zügen, {duel.LogHistory.Count} Logzeilen, {watch.ElapsedMilliseconds} ms");
                if (crash != null)
                {
                    Console.WriteLine($"[selftest]   {crash}");
                    foreach (var line in duel.LogHistory.Skip(Math.Max(0, duel.LogHistory.Count - 12)))
                        Console.WriteLine($"[selftest]   {line}");
                }

                if (grep != null)
                    foreach (var line in duel.LogHistory)
                        if (line.IndexOf(grep, StringComparison.OrdinalIgnoreCase) >= 0)
                            Console.WriteLine($"[selftest]   #{game + 1}: {line}");

                // --log gibt das ganze Protokoll des letzten Duells aus. Ohne das sieht
                // man nur das Ende, und ob ein Effekt je gefeuert hat, bleibt unsichtbar.
                if (game == games - 1)
                {
                    var shown = args.Contains("--log")
                        ? duel.LogHistory
                        : duel.LogHistory.Skip(Math.Max(0, duel.LogHistory.Count - 4));
                    foreach (var line in shown)
                        Console.WriteLine($"[selftest]   {line}");
                }
            }
            totalWatch.Stop();
            Console.WriteLine($"[selftest] {games} Duell(e), {failures} Fehler, {totalWatch.ElapsedMilliseconds} ms gesamt");
            return failures == 0 ? 0 : 1;
        }

        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
