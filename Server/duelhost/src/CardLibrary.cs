// Lädt cards-full.json (Export aus Unity: "Rouge TCG/Export Server Data") und baut
// daraus dieselben Datenobjekte, mit denen auch der Client rechnet: MonsterCardData,
// SpellCardData usw. — dank ScriptableObject-Shim per `new` erzeugbar. Felder werden
// per Reflection nach Namen befüllt, Enums über ihre Namen; unbekannte JSON-Felder
// werden ignoriert (alte Hosts überleben neue Exporte).

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Rouge.Tcg;

namespace Rouge.DuelHost
{
    public class CardLibrary
    {
        public readonly CardCatalog Catalog = new CardCatalog();
        public readonly GameRules Rules = new GameRules();

        public static CardLibrary Load(string dataDir)
        {
            var library = new CardLibrary();
            string path = Path.Combine(dataDir, "cards-full.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));

            PopulateFields(library.Rules, doc.RootElement.GetProperty("rules"));

            foreach (var entry in doc.RootElement.GetProperty("cards").EnumerateObject())
            {
                var card = CreateCard(entry.Value.GetProperty("class").GetString());
                if (card == null) continue;
                PopulateFields(card, entry.Value);
                if (string.IsNullOrEmpty(card.cardName)) card.cardName = entry.Name;
                library.Catalog.cards.Add(card);
            }

            // Karten-Referenzen überleben den JSON-Weg nicht (der Export überspringt
            // Unity-Objekte) — nach dem Laden per Name nachziehen.
            library.Rules.illusionToken = library.Catalog.cards
                .Find(c => c.cardName == "Illusion Token") as MonsterCardData;
            library.Rules.scarecrowToken = library.Catalog.cards
                .Find(c => c.cardName == "Scarecrow Token") as MonsterCardData;

            return library;
        }

        private static CardDefinition CreateCard(string className) => className switch
        {
            "MonsterCardData" => new MonsterCardData(),
            "ReliquaryCardData" => new ReliquaryCardData(),
            "IncarnateCardData" => new IncarnateCardData(),
            "SpellCardData" => new SpellCardData(),
            "ArtifactCardData" => new ArtifactCardData(),
            "PlayerCardData" => new PlayerCardData(),
            _ => null
        };

        /// <summary>Öffentliche Felder eines Objekts aus einem JSON-Objekt befüllen.</summary>
        private static void PopulateFields(object target, JsonElement json)
        {
            var type = target.GetType();
            foreach (var property in json.EnumerateObject())
            {
                var field = type.GetField(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) continue;
                object value = ReadValue(field.FieldType, property.Value);
                if (value != null || !field.FieldType.IsValueType) field.SetValue(target, value);
            }
        }

        private static object ReadValue(Type type, JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Null) return null;
            if (type == typeof(string)) return json.GetString();
            if (type == typeof(bool)) return json.GetBoolean();
            if (type == typeof(int)) return json.GetInt32();
            if (type == typeof(long)) return json.GetInt64();
            if (type == typeof(float)) return json.GetSingle();
            if (type == typeof(double)) return json.GetDouble();
            if (type.IsEnum)
                return Enum.TryParse(type, json.GetString(), out var parsed) ? parsed : Activator.CreateInstance(type);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(type);
                foreach (var item in json.EnumerateArray())
                    list.Add(ReadValue(itemType, item));
                return list;
            }

            // Verschachtelte [Serializable]-Klassen (EffectDefinition, EffectAction)
            var nested = Activator.CreateInstance(type);
            PopulateFields(nested, json);
            return nested;
        }
    }
}
