// Lokale Brücke zum Node-Server: TCP auf 127.0.0.1, ein JSON-Objekt pro Zeile
// (NDJSON). Node verbindet sich, schickt start/intent/leave und bekommt
// state/request/events/log/end zurück. Reißt die Verbindung ab, nimmt der
// Listener einfach die nächste an.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Rouge.DuelHost
{
    public class HostServer
    {
        private readonly TcpListener listener;
        private readonly ConcurrentQueue<JsonDocument> inbound = new ConcurrentQueue<JsonDocument>();
        private StreamWriter writer;
        private readonly object writeLock = new object();

        public HostServer(int port)
        {
            listener = new TcpListener(IPAddress.Loopback, port);
        }

        public void Start()
        {
            listener.Start();
            var thread = new Thread(AcceptLoop) { IsBackground = true };
            thread.Start();
        }

        private void AcceptLoop()
        {
            while (true)
            {
                TcpClient client;
                try { client = listener.AcceptTcpClient(); }
                catch { break; }

                Console.WriteLine("[host] Node verbunden.");
                var stream = client.GetStream();
                lock (writeLock)
                    writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                try
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try { inbound.Enqueue(JsonDocument.Parse(line)); }
                        catch (Exception e) { Console.WriteLine($"[host] Kaputte Zeile: {e.Message}"); }
                    }
                }
                catch { /* Verbindung weg — auf die nächste warten */ }
                Console.WriteLine("[host] Node getrennt.");
                lock (writeLock) writer = null;
            }
        }

        public bool TryDequeue(out JsonDocument doc) => inbound.TryDequeue(out doc);

        public void Send(object payload)
        {
            string line = JsonSerializer.Serialize(payload);
            lock (writeLock)
            {
                try { writer?.WriteLine(line); }
                catch { writer = null; }
            }
        }
    }
}
