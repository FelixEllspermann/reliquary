using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rouge.Data;

namespace Rouge.Combat
{
    public class DeckManager : MonoBehaviour
    {
        [Header("Laufzeit-Info (nur Anzeige)")]
        [SerializeField] private int drawPileCount;
        [SerializeField] private int discardPileCount;

        private readonly List<CardData> drawPile = new List<CardData>();
        private readonly List<CardData> discardPile = new List<CardData>();
        private readonly List<CardData> hand = new List<CardData>();
        private readonly System.Random rng = new System.Random();

        private int maxHandSize = 10;

        public IReadOnlyList<CardData> Hand => hand;
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;

        public event Action OnPilesChanged;

        public void Initialize(IEnumerable<CardData> startingDeck, int maxHand)
        {
            maxHandSize = maxHand;
            drawPile.Clear();
            discardPile.Clear();
            hand.Clear();
            drawPile.AddRange(startingDeck.Where(card => card != null));
            Shuffle(drawPile);
            NotifyChanged();
        }

        public void Draw(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (hand.Count >= maxHandSize) break;
                if (drawPile.Count == 0) Reshuffle();
                if (drawPile.Count == 0) break;

                var card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
            }
            NotifyChanged();
        }

        public void DiscardFromHand(CardData card)
        {
            if (hand.Remove(card)) discardPile.Add(card);
            NotifyChanged();
        }

        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
            NotifyChanged();
        }

        private void Reshuffle()
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        private void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void NotifyChanged()
        {
            drawPileCount = drawPile.Count;
            discardPileCount = discardPile.Count;
            OnPilesChanged?.Invoke();
        }
    }
}
