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
    }
}
