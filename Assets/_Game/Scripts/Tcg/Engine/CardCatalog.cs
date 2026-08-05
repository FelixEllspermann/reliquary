using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Katalog aller Karten des Spiels — löst Kartennamen aus Netzwerk-Nachrichten
    /// in lokale Karten-Assets auf. Beide Clients müssen denselben Kartenstand haben.
    /// </summary>
    [CreateAssetMenu(fileName = "CardCatalog", menuName = "Rouge TCG/Karten-Katalog")]
    public class CardCatalog : ScriptableObject
    {
        [Tooltip("Alle Karten (inklusive Spielerkarten)")]
        public List<CardDefinition> cards = new List<CardDefinition>();

        private Dictionary<string, CardDefinition> lookup;

        public CardDefinition FindByName(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return null;
            if (lookup == null || lookup.Count != cards.Count)
            {
                lookup = new Dictionary<string, CardDefinition>();
                foreach (var card in cards)
                    if (card != null && !lookup.ContainsKey(card.cardName)) lookup[card.cardName] = card;
            }
            lookup.TryGetValue(cardName, out var found);
            return found;
        }

        public List<CardDefinition> ResolveList(IEnumerable<string> names)
        {
            var result = new List<CardDefinition>();
            if (names == null) return result;
            foreach (var name in names)
            {
                var card = FindByName(name);
                if (card != null) result.Add(card);
                else Debug.LogWarning($"CardCatalog: Karte '{name}' nicht gefunden!");
            }
            return result;
        }

        /// <summary>
        /// Wie <see cref="ResolveList(IEnumerable{string})"/>, führt aber die
        /// Finish-Liste im Gleichschritt mit.
        ///
        /// Der Gleichschritt ist der ganze Punkt: ein unbekannter Kartenname fällt
        /// aus der Liste, und ohne diese Methode bliebe sein Finish stehen. Ab da
        /// trüge jede folgende Karte die Ausführung ihrer Vorgängerin — aus einem
        /// fehlenden Namen würde ein Deck voll falscher Effekte.
        /// </summary>
        /// <param name="finishes">Zahlenwerte parallel zu <paramref name="names"/>; darf kürzer oder null sein.</param>
        /// <param name="kept">Nimmt die Finishes der tatsächlich gefundenen Karten auf.</param>
        public List<CardDefinition> ResolveList(IList<string> names, IList<int> finishes, List<Net.CardFinish> kept)
        {
            var result = new List<CardDefinition>();
            kept?.Clear();
            if (names == null) return result;
            for (int i = 0; i < names.Count; i++)
            {
                var card = FindByName(names[i]);
                if (card == null) { Debug.LogWarning($"CardCatalog: Karte '{names[i]}' nicht gefunden!"); continue; }
                result.Add(card);
                kept?.Add(FinishAt(finishes, i));
            }
            return result;
        }

        /// <summary>Ein Finish aus einer Zahlenliste — fehlt der Eintrag, ist er schlicht.</summary>
        private static Net.CardFinish FinishAt(IList<int> finishes, int index) =>
            finishes != null && index >= 0 && index < finishes.Count
                ? Net.CardFinishWire.From(finishes[index])
                : Net.CardFinish.Plain;
    }
}
