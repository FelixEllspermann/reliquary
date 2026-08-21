using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>
    /// Schmale Brücke zu Steamworks. Das Projekt kompiliert und läuft OHNE das
    /// Steam-SDK — dann meldet <see cref="Available"/> einfach false und der
    /// Steam-Knopf im Login bleibt aus.
    ///
    /// Scharfgeschaltet wird über das Define STEAMWORKS_NET, das das Paket
    /// Steamworks.NET selbst setzt — es genügt also, das Paket zu installieren.
    /// Zusätzlich muss steam_appid.txt (App-ID) neben der Projektwurzel und
    /// neben der gebauten .exe liegen. Siehe Server/STEAM-SETUP.md.
    ///
    /// Sicherheitsmodell: Das Ticket ist NICHT der Nachweis. Der Client holt es
    /// nur ab; geprüft wird es serverseitig über die Steam-Web-API — nur der
    /// Server erfährt daraus die SteamID.
    /// </summary>
    public static class SteamBridge
    {
        /// <summary>Läuft Steam, ist das SDK da und die API initialisiert?</summary>
        public static bool Available { get; private set; }

        /// <summary>Steam-Anzeigename (leer, wenn nicht verfügbar).</summary>
        public static string PersonaName { get; private set; } = "";

        /// <summary>
        /// Die Sprache, in der Steam dieses Spiel ausliefert (API-Code wie
        /// "english", "german", "schinese") — leer, wenn Steam nicht läuft.
        /// </summary>
        public static string GameLanguage
        {
            get
            {
#if STEAMWORKS_NET
                if (Available) return Steamworks.SteamApps.GetCurrentGameLanguage() ?? "";
#endif
                return "";
            }
        }

        /// <summary>Warum Steam nicht bereit ist — für die Statuszeile im Login.</summary>
        public static string UnavailableReason { get; private set; } = "Steam support is not built into this version.";

        private static bool initialised;

#if STEAMWORKS_NET
        private static Steamworks.HAuthTicket ticketHandle;
        private static Steamworks.Callback<Steamworks.GetAuthSessionTicketResponse_t> ticketCallback;
        private static System.Action<string> ticketReady;
        private static System.Action<string> ticketFailed;
        private static string pendingHex;
#endif

        /// <summary>
        /// Einmalige Initialisierung. Mehrfach aufrufbar; scheitert leise, wenn
        /// Steam nicht läuft (dann startet das Spiel ganz normal ohne Steam).
        /// </summary>
        public static void Initialise()
        {
            if (initialised) return;
            initialised = true;

#if STEAMWORKS_NET
            try
            {
                // InitEx liefert im Fehlerfall eine Klartext-Begründung von Valve —
                // deutlich hilfreicher als das blosse false von Init().
                var result = Steamworks.SteamAPI.InitEx(out string steamError);
                if (result != Steamworks.ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    UnavailableReason = $"Steam could not start ({result}): {steamError}";
                    Debug.LogWarning("[Steam] " + UnavailableReason
                        + $"  |  Arbeitsverzeichnis: {System.IO.Directory.GetCurrentDirectory()}"
                        + $"  |  steam_appid.txt: {System.IO.File.Exists("steam_appid.txt")}");
                    return;
                }
                Available = true;
                PersonaName = Steamworks.SteamFriends.GetPersonaName();
                UnavailableReason = "";
                ticketCallback = Steamworks.Callback<Steamworks.GetAuthSessionTicketResponse_t>
                    .Create(OnTicketResponse);
                Debug.Log($"[Steam] Verbunden als {PersonaName} (AppID {Steamworks.SteamUtils.GetAppID()})");
            }
            catch (System.Exception error)
            {
                UnavailableReason = "Steam could not be reached: " + error.Message;
                Available = false;
            }
#endif
        }

        /// <summary>
        /// Fordert ein Auth-Ticket an. Das Ergebnis kommt ASYNCHRON: ein frisch
        /// erzeugtes Ticket ist erst gültig, wenn Steam es per
        /// GetAuthSessionTicketResponse_t bestätigt hat — schickt man es vorher
        /// an den Server, antwortet Valve mit "Invalid ticket".
        /// </summary>
        /// <param name="onReady">Bekommt das Ticket als Hex-String.</param>
        /// <param name="onError">Bekommt eine Begründung im Fehlerfall.</param>
        public static void RequestAuthTicket(System.Action<string> onReady, System.Action<string> onError)
        {
            if (!Available) { onError?.Invoke(UnavailableReason); return; }

#if STEAMWORKS_NET
            if (ticketReady != null) { onError?.Invoke("A Steam sign-in is already in progress."); return; }

            var buffer = new byte[1024];
            uint written;
            // Das Identity-Argument ist seit SDK 1.57 Pflicht. Eine geleerte
            // Identität heisst "an keinen bestimmten Dienst gebunden" — der
            // Server prüft ohnehin App-ID und Ban-Status bei Valve.
            var identity = new Steamworks.SteamNetworkingIdentity();
            identity.Clear();
            ticketHandle = Steamworks.SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out written, ref identity);
            if (ticketHandle == Steamworks.HAuthTicket.Invalid || written == 0)
            {
                onError?.Invoke("Steam did not hand out a ticket.");
                return;
            }

            var hex = new System.Text.StringBuilder((int)written * 2);
            for (int i = 0; i < written; i++) hex.Append(buffer[i].ToString("x2"));
            pendingHex = hex.ToString();
            ticketReady = onReady;
            ticketFailed = onError;
#else
            onError?.Invoke(UnavailableReason);
#endif
        }

#if STEAMWORKS_NET
        /// <summary>Steam hat das Ticket freigegeben (oder abgelehnt).</summary>
        private static void OnTicketResponse(Steamworks.GetAuthSessionTicketResponse_t response)
        {
            var ready = ticketReady;
            var failed = ticketFailed;
            string hex = pendingHex;
            ticketReady = null;
            ticketFailed = null;
            pendingHex = null;

            if (response.m_eResult == Steamworks.EResult.k_EResultOK) ready?.Invoke(hex);
            else failed?.Invoke("Steam refused the ticket: " + response.m_eResult);
        }
#endif

        /// <summary>Muss regelmäßig laufen, solange Steam aktiv ist (Callbacks).</summary>
        public static void Pump()
        {
#if STEAMWORKS_NET
            if (Available) Steamworks.SteamAPI.RunCallbacks();
#endif
        }

        public static void Shutdown()
        {
#if STEAMWORKS_NET
            if (!Available) return;
            if (ticketHandle != Steamworks.HAuthTicket.Invalid)
                Steamworks.SteamUser.CancelAuthTicket(ticketHandle);
            Steamworks.SteamAPI.Shutdown();
            Available = false;
#endif
        }
    }

    /// <summary>
    /// Hält Steam am Leben: initialisiert beim Spielstart und pumpt die Callbacks.
    /// Ohne SDK ein reiner No-op.
    /// </summary>
    public class SteamRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SteamBridge.Initialise();
            if (!SteamBridge.Available) return;
            var host = new GameObject("~SteamRuntime");
            host.hideFlags = HideFlags.HideInHierarchy;
            host.AddComponent<SteamRuntime>();
            DontDestroyOnLoad(host);
        }

        private void Update() => SteamBridge.Pump();
        private void OnApplicationQuit() => SteamBridge.Shutdown();
    }
}
