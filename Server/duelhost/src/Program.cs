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

        private static int SelfTest(CardLibrary library, string dataDir)
        {
            var starter = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataDir, "starterdeck.json"))).RootElement;
            var deckNames = starter.GetProperty("cards").EnumerateArray().Select(c => c.GetString()).ToList();
            string heroName = starter.TryGetProperty("hero", out var h) ? h.GetString() : null;

            var deckA = library.Catalog.ResolveList(deckNames);
            var deckB = library.Catalog.ResolveList(deckNames);
            var hero = library.Catalog.FindByName(heroName) as PlayerCardData;
            var none = new List<CardDefinition>();

            var duel = new DuelManager(new DuelConfig { Rules = library.Rules, BotActionDelay = 0f });
            var watch = System.Diagnostics.Stopwatch.StartNew();
            duel.StartServerDuel(7777,
                "Selftest-A", deckA, none, hero, new BotDuelController(),
                "Selftest-B", deckB, none, hero, new BotDuelController(),
                true);
            watch.Stop();

            Console.WriteLine($"[selftest] Ergebnis: {duel.Result} nach {duel.TurnNumber} Zügen in {watch.ElapsedMilliseconds} ms");
            Console.WriteLine($"[selftest] Logzeilen: {duel.LogHistory.Count}");
            foreach (var line in duel.LogHistory.Skip(Math.Max(0, duel.LogHistory.Count - 4)))
                Console.WriteLine($"[selftest]   {line}");

            return duel.Result == DuelResult.None ? 1 : 0;
        }

        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
