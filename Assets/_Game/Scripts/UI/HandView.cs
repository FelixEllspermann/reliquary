using System.Collections.Generic;
using UnityEngine;
using Rouge.Combat;

namespace Rouge.UI
{
    public class HandView : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private CombatManager combat;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private RectTransform container;

        private readonly List<CardView> views = new List<CardView>();

        private void OnEnable()
        {
            if (combat == null) return;
            if (combat.Deck != null) combat.Deck.OnPilesChanged += Rebuild;
            combat.OnCombatChanged += RefreshPlayable;
        }

        private void OnDisable()
        {
            if (combat == null) return;
            if (combat.Deck != null) combat.Deck.OnPilesChanged -= Rebuild;
            combat.OnCombatChanged -= RefreshPlayable;
        }

        private void Rebuild()
        {
            foreach (var view in views)
            {
                if (view != null) Destroy(view.gameObject);
            }
            views.Clear();

            foreach (var card in combat.Deck.Hand)
            {
                var view = Instantiate(cardPrefab, container);
                view.Show(card);
                view.Clicked += HandleCardClicked;
                views.Add(view);
            }
            RefreshPlayable();
        }

        private void RefreshPlayable()
        {
            bool playerTurn = combat.State == CombatState.PlayerTurn;
            foreach (var view in views)
            {
                if (view != null && view.Card != null)
                    view.SetPlayable(playerTurn && view.Card.cost <= combat.Energy);
            }
        }

        private void HandleCardClicked(CardView view)
        {
            combat.TryPlayCard(view.Card);
        }
    }
}
