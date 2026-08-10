using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Exportiert die Spieldaten für den Server nach Server/data:
    ///   cards-full.json — ALLE Gameplay-Felder jeder Karte inkl. Effekte (Enums als
    ///                     Namen) + das komplette Regelwerk. Grundlage für den
    ///                     server-autoritativen Duel-Host (Schritt 4).
    ///   cards.json      — Name -> Rarity (Legacy, nutzt der Node-Server heute).
    ///   reliquary.json  — Namen aller Extra-Deck-Karten.
    /// Nach jedem Karten-Batch einmal ausführen: Menü "Rouge TCG/Export Server Data".
    /// </summary>
    public static class ServerCardExporter
    {
        [MenuItem("Rouge TCG/Export Server Data")]
        public static void ExportMenu() => Debug.Log(ExportAll());

        public static string ExportAll()
        {
            var catalog = LoadCatalog();
            var rules = LoadRules();
            var cards = catalog.cards.Where(c => c != null)
                .OrderBy(c => c.cardName, StringComparer.Ordinal).ToList();

            string dataDir = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", "Server", "data"));
            System.IO.Directory.CreateDirectory(dataDir);

            // ---- cards-full.json ----
            var full = new StringBuilder();
            full.Append("{\n  \"rules\": ");
            WriteObjectFields(full, rules, 1);
            full.Append(",\n  \"cards\": {\n");
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                full.Append("    ").Append(Quote(card.cardName)).Append(": ");
                WriteCard(full, card);
                full.Append(i < cards.Count - 1 ? ",\n" : "\n");
            }
            full.Append("  }\n}\n");
            string fullPath = System.IO.Path.Combine(dataDir, "cards-full.json");
            System.IO.File.WriteAllText(fullPath, full.ToString(), new UTF8Encoding(false));

            // ---- cards.json (Name -> Rarity) ----
            // Tokens bleiben draußen: node speist daraus Pack-, Draft- und
            // Craft-Pools — ein Token ist nicht sammelbar. (cards-full.json
            // behält ihn, der DuelHost braucht die Definition.)
            var collectible = cards.Where(c => !c.isToken).ToList();
            var legacy = new StringBuilder("{\n");
            for (int i = 0; i < collectible.Count; i++)
                legacy.Append(' ').Append(Quote(collectible[i].cardName)).Append(": ")
                      .Append((int)collectible[i].rarity).Append(i < collectible.Count - 1 ? ",\n" : "\n");
            legacy.Append("}\n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dataDir, "cards.json"),
                legacy.ToString(), new UTF8Encoding(false));

            // ---- reliquary.json ----
            var reliquaries = cards.Where(c => c is ReliquaryCardData).Select(c => c.cardName).ToList();
            var reli = new StringBuilder("[\n");
            for (int i = 0; i < reliquaries.Count; i++)
                reli.Append(' ').Append(Quote(reliquaries[i])).Append(i < reliquaries.Count - 1 ? ",\n" : "\n");
            reli.Append("]\n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dataDir, "reliquary.json"),
                reli.ToString(), new UTF8Encoding(false));

            return $"Server-Export: {cards.Count} Karten ({reliquaries.Count} Reliquarys) nach {dataDir}";
        }

        private static CardCatalog LoadCatalog()
        {
            string guid = AssetDatabase.FindAssets("t:CardCatalog").FirstOrDefault()
                ?? throw new Exception("Kein CardCatalog-Asset gefunden.");
            return AssetDatabase.LoadAssetAtPath<CardCatalog>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static GameRules LoadRules()
        {
            string guid = AssetDatabase.FindAssets("t:GameRules").FirstOrDefault()
                ?? throw new Exception("Kein GameRules-Asset gefunden.");
            return AssetDatabase.LoadAssetAtPath<GameRules>(AssetDatabase.GUIDToAssetPath(guid));
        }

        // ================== JSON-SCHREIBER ==================

        private static void WriteCard(StringBuilder sb, CardDefinition card)
        {
            sb.Append("{ \"class\": ").Append(Quote(card.GetType().Name));
            foreach (var field in SerializableFields(card.GetType()))
            {
                sb.Append(", ").Append(Quote(field.Name)).Append(": ");
                WriteValue(sb, field.GetValue(card), 3);
            }
            sb.Append(" }");
        }

        private static void WriteObjectFields(StringBuilder sb, object obj, int indent)
        {
            sb.Append("{ ");
            bool first = true;
            foreach (var field in SerializableFields(obj.GetType()))
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(Quote(field.Name)).Append(": ");
                WriteValue(sb, field.GetValue(obj), indent + 1);
            }
            sb.Append(" }");
        }

        /// <summary>Öffentliche Instanzfelder ohne Unity-Objekte (artwork etc.).</summary>
        private static FieldInfo[] SerializableFields(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                .ToArray();

        private static void WriteValue(StringBuilder sb, object value, int indent)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append(Quote(s)); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case Enum e: sb.Append(Quote(e.ToString())); break;
                case int or long or short or byte: sb.Append(value); break;
                case float f: sb.Append(f.ToString("0.####", CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(d.ToString("0.####", CultureInfo.InvariantCulture)); break;
                case IList list:
                {
                    if (list.Count == 0) { sb.Append("[]"); break; }
                    sb.Append("[ ");
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        WriteValue(sb, list[i], indent + 1);
                    }
                    sb.Append(" ]");
                    break;
                }
                default:
                    // Verschachtelte [Serializable]-Klassen (EffectDefinition, EffectAction)
                    WriteObjectFields(sb, value, indent);
                    break;
            }
        }

        private static string Quote(string raw)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in raw ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
