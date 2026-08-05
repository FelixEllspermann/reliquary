using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "NeuesDeck", menuName = "Rouge TCG/Deck")]
    public class DeckDefinition : ScriptableObject
    {
        [Tooltip("Anzeigename des Decks")]
        public string deckName = "Neues Deck";

        [Tooltip("Die Spielerkarte (Held) dieses Decks — liegt ab Duellbeginn offen in der Spielerzone")]
        public PlayerCardData playerCard;

        [Tooltip("Alle Karten des Decks (40–80, Duplikate erlaubt)")]
        public List<CardDefinition> cards = new List<CardDefinition>();

        [Tooltip("Extra Deck: bis zu 20 Reliquary-Karten")]
        public List<CardDefinition> extraCards = new List<CardDefinition>();

        private void OnValidate()
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i] is PlayerCardData)
                {
                    Debug.LogWarning($"Deck '{deckName}': Spielerkarten gehören nicht ins Deck (Slot {i}) — bitte ins Feld 'Player Card'.", this);
                }
            }
        }
    }
}
