using System.Collections.Generic;

namespace Rouge.Tcg.Net
{
    /// <summary>Übergibt die Match-Daten von der Lobby an die Duel-Szene (ein Match pro Prozess).</summary>
    public static class MatchContext
    {

        /// <summary>Server-autoritatives Duell: der DuelHost auf dem Server rechnet, der Client spiegelt nur.</summary>
        public static bool IsServerMatch;

        /// <summary>Solo-Duell mit einem Account-Deck (LocalDeckCards/LocalHero gefüllt, Gegner = Bot).</summary>
        public static bool UseCustomLocalDeck;

        /// <summary>Solo-Schwierigkeit: 0 = Novice (Bot ohne Reaktionen), 1 = Warden, 2 = Sealed (+2 Mana, 12000 LP).</summary>
        public static int SoloDifficulty = 1;

        // Gewählter Solo-Gegner (leer = Legacy-Verhalten über SoloDifficulty)
        public static string BotName = "";
        public static List<string> BotDeckCards = new List<string>();
        public static List<string> BotExtraCards = new List<string>();
        public static string BotHero = "";
        public static int BotLifePoints;     // 0 = Standard der Heldenkarte
        public static int BotBonusMana;
        public static bool BotNovice;
        public static bool LocalIsPlayerA;

        public static bool LocalStarts = true;

        public static string LocalName = "Du";
        public static string RemoteName = "Opponent";

        public static List<string> LocalDeckCards = new List<string>();
        public static List<string> LocalExtraCards = new List<string>();
        public static string LocalHero;
        public static string LocalDeckName;

        // Ausführung je Deck-Platz, gleiche Reihenfolge wie oben. Leer heisst
        // schlicht — Decks aus der Zeit vor den Finishes bleiben so gültig.
        public static List<int> LocalDeckFinishes = new List<int>();
        public static List<int> LocalExtraFinishes = new List<int>();


        /// <summary>
        /// Was der Gegner an Kosmetik trägt, Fach → Id. Kommt mit der
        /// Match-Nachricht, also vor dem ersten Bild des Duells — mitten im Spiel
        /// nachzuladen wäre sichtbar. Leer heisst Standardaussehen.
        /// </summary>
        public static readonly Dictionary<string, string> RemoteCosmetics = new Dictionary<string, string>();

        public static string RemoteEquipped(string slot) =>
            slot != null && RemoteCosmetics.TryGetValue(slot, out var id) ? id : "";

        /// <summary>Übernimmt die beiden Parallel-Listen aus der Match-Nachricht.</summary>
        public static void SetRemoteCosmetics(string[] slots, string[] ids)
        {
            RemoteCosmetics.Clear();
            if (slots == null || ids == null) return;
            for (int i = 0; i < slots.Length && i < ids.Length; i++)
                if (!string.IsNullOrEmpty(slots[i]) && !string.IsNullOrEmpty(ids[i]))
                    RemoteCosmetics[slots[i]] = ids[i];
        }

        public static void Clear()
        {
            IsServerMatch = false;
            UseCustomLocalDeck = false;
            SoloDifficulty = 1;
            BotName = "";
            BotDeckCards = new List<string>();
            BotExtraCards = new List<string>();
            BotHero = "";
            BotLifePoints = 0;
            BotBonusMana = 0;
            BotNovice = false;
            LocalIsPlayerA = false;
            LocalStarts = true;
            LocalName = "Du";
            RemoteName = "Opponent";
            RemoteCosmetics.Clear();
            LocalDeckCards = new List<string>();
            LocalExtraCards = new List<string>();
            LocalDeckFinishes = new List<int>();
            LocalExtraFinishes = new List<int>();
            LocalHero = null;
            LocalDeckName = null;
        }
    }
}
