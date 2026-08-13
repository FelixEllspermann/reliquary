using System;
using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>
    /// Die kuratierten Archetypes des Spiels. Zugehörigkeit steckt normalerweise
    /// im Namens-Präfix ("Tidebound Leviathan", "Dragon Shrine Offering") — deshalb
    /// reicht dort StartsWith, und "Dragon Claw" (Generic) fällt nicht auf
    /// "Dragon Shrine". Karten, deren Familie woanders im Namen steht oder gar
    /// nicht ("King of Deckay", "Rising Tide"), stehen namentlich in Exceptions.
    /// Der Server führt beide Listen in server.js (ARCHETYPES,
    /// ARCHETYPE_EXCEPTIONS) — wer hier ergänzt, ergänzt sie auch dort.
    /// </summary>
    public static class ArchetypeCatalog
    {
        public static readonly string[] Names =
        {
            "Apocrypha", "Archfiend", "Barrierstruck", "Deathpoem", "Deckay",
            "Dragon Shrine", "Failsafe", "Fethaerbreese", "Forgeheart", "Gaslight",
            "Genostitched", "Gravemaw", "Heavenly", "Hexweaver", "Kindlekin",
            "Lightless", "Lyria", "Manacle", "Mechination", "Mimicrypt",
            "Paperbound", "Powderkeg", "Redactor", "Sacrilegion", "Sleightwind",
            "Slowburn", "Snugglet", "Tidebound", "Trapline", "Wyldpack"
        };

        /// <summary>
        /// Karten, die zu einem Archetype gehören, ohne ihn als Namens-Präfix zu
        /// tragen. Exakte Kartennamen — "Dragon Sceptre" zählt zum Shrine,
        /// "Dragon Claw" bleibt ein Generic.
        /// </summary>
        public static readonly Dictionary<string, string> Exceptions = new Dictionary<string, string>
        {
            { "King of Deckay", "Deckay" },
            { "Signs of Deckay", "Deckay" },
            { "Feast of Deckay", "Deckay" },

            { "Maiden of the Dragon Shrine", "Dragon Shrine" },
            { "Baby Dragon of the Dragon Shrine", "Dragon Shrine" },
            { "Doorwyrm of the Dragon Shrine", "Dragon Shrine" },
            { "Elder Wyrm of the Dragon Shrine", "Dragon Shrine" },
            { "Diactor of the Dragon Shrine", "Dragon Shrine" },
            { "Petitioner of the Dragon Shrine", "Dragon Shrine" },
            { "Wyrm Eternal, Shrine Ascendant", "Dragon Shrine" },
            { "Shrinekeeper Dragon", "Dragon Shrine" },
            { "Heart of the Shrine", "Dragon Shrine" },
            { "Dragon Sceptre", "Dragon Shrine" },

            { "Bulwark Prism", "Barrierstruck" },
            { "Aegis Fragment", "Barrierstruck" },
            { "Reactive Plating", "Barrierstruck" },
            { "Woven Fate", "Hexweaver" },
            { "Herald of the Lightless", "Lightless" },
            { "Call of the Wyld", "Wyldpack" },
            { "Heart of the Forge", "Forgeheart" },
            { "Rising Tide", "Tidebound" },
            { "Raise the Failsafes", "Failsafe" },
        };

        /// <summary>Der Archetype dieser Karte — oder null für Generics.</summary>
        public static string Of(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return null;
            // Namentlich geführte Karten zuerst: ihr Präfix verrät nichts.
            if (Exceptions.TryGetValue(cardName, out string named)) return named;
            foreach (var archetype in Names)
                if (cardName.StartsWith(archetype, StringComparison.Ordinal))
                    return archetype;
            return null;
        }
    }
}
