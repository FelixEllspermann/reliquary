using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>
    /// Lokalisierung — bewusst als reines C# ohne Unity-Abhängigkeiten, weil der
    /// DuelHost diese Datei mitkompiliert (Engine-Ordner). Der Server füllt nie
    /// eine Tabelle und bleibt damit automatisch englisch; der Client lädt die
    /// Tabellen beim Start (LocBoot) aus Resources/Localization.
    ///
    /// Zwei Tabellen:
    ///  - UI: englischer Text → Übersetzung. Auch Format-VORLAGEN mit {0}/{1}
    ///    (BuildPassiveLines, SelfSummonConditionText) laufen hierüber — deshalb
    ///    nie string-interpolieren, sondern Loc.F(vorlage, werte) rufen.
    ///  - Karten: je englischem Kartennamen die übersetzten Felder (Name,
    ///    Beschwörungstext, Effekt-Label/-Text je INDEX). Der englische Name
    ///    bleibt überall der Schlüssel — Server, Decks, Suche und Links arbeiten
    ///    weiter mit ihm, nur die ANZEIGE wechselt die Sprache.
    /// </summary>
    public static class Loc
    {
        public const string English = "en";
        public const string ChineseSimplified = "zh-Hans";
        public const string German = "de";

        /// <summary>Aktive Sprache (Sprachcode). Setzt nur der Client (LocBoot).</summary>
        public static string Language = English;

        public static bool Active => Language != English && ui != null;

        public class CardEntry
        {
            public string name;
            public string summon;
            public readonly Dictionary<int, string> labels = new Dictionary<int, string>();
            public readonly Dictionary<int, string> texts = new Dictionary<int, string>();
        }

        private static Dictionary<string, string> ui;
        private static Dictionary<string, CardEntry> cards;

        public static void SetTables(Dictionary<string, string> uiTable, Dictionary<string, CardEntry> cardTable)
        {
            ui = uiTable;
            cards = cardTable;
        }

        /// <summary>UI-Text übersetzen; ohne Eintrag bleibt es beim Englischen.</summary>
        public static string T(string english)
        {
            if (!Active || string.IsNullOrEmpty(english)) return english;
            return ui.TryGetValue(english, out var translated) && translated.Length > 0 ? translated : english;
        }

        /// <summary>Format-Vorlage übersetzen und füllen ("{0} gains {1} ATK …").</summary>
        public static string F(string english, params object[] args)
        {
            var template = T(english);
            try { return string.Format(template, args); }
            catch (System.FormatException) { return string.Format(english, args); }
        }

        private static CardEntry Entry(string cardName)
        {
            if (!Active || cards == null || string.IsNullOrEmpty(cardName)) return null;
            return cards.TryGetValue(cardName, out var entry) ? entry : null;
        }

        /// <summary>Anzeigename einer Karte (englischer Name bleibt der Schlüssel).</summary>
        public static string CardName(string englishName)
        {
            var entry = Entry(englishName);
            return entry != null && !string.IsNullOrEmpty(entry.name) ? entry.name : englishName;
        }

        /// <summary>Übersetzter Kartenname oder null, wenn es keinen gibt (für Such-Aliase).</summary>
        public static string CardNameOrNull(string englishName)
        {
            var entry = Entry(englishName);
            return entry != null && !string.IsNullOrEmpty(entry.name) ? entry.name : null;
        }

        public static string CardSummon(string cardName, string english)
        {
            var entry = Entry(cardName);
            return entry != null && !string.IsNullOrEmpty(entry.summon) ? entry.summon : english;
        }

        public static string CardLabel(string cardName, int effectIndex, string english)
        {
            var entry = Entry(cardName);
            return entry != null && entry.labels.TryGetValue(effectIndex, out var t) && t.Length > 0 ? t : english;
        }

        public static string CardText(string cardName, int effectIndex, string english)
        {
            var entry = Entry(cardName);
            return entry != null && entry.texts.TryGetValue(effectIndex, out var t) && t.Length > 0 ? t : english;
        }
    }
}
