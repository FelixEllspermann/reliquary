using System;

namespace Rouge.Tcg
{
    /// <summary>
    /// Die kuratierten Archetypes des Spiels. Zugehörigkeit steckt im Namens-
    /// Präfix ("Tidebound Leviathan", "Dragon Shrine Offering") — deshalb reicht
    /// StartsWith, und "Dragon Claw" (Generic) fällt nicht auf "Dragon Shrine".
    /// Der Server führt dieselbe Liste in server.js (ARCHETYPES) — wer hier
    /// einen Archetype ergänzt, ergänzt ihn auch dort.
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

        /// <summary>Der Archetype dieser Karte — oder null für Generics.</summary>
        public static string Of(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return null;
            foreach (var archetype in Names)
                if (cardName.StartsWith(archetype, StringComparison.Ordinal))
                    return archetype;
            return null;
        }
    }
}
