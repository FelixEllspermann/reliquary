using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Zeigt eine Karte dekorativ auf einem TcgCardView (Login-Hero, Menü-Deko).
    /// Mit gesetztem Katalog wird bei jedem Szenenstart eine ZUFÄLLIGE Karte gezogen —
    /// Karten mit Artwork werden bevorzugt, damit das Schaufenster immer gefüllt wirkt.
    /// </summary>
    public class CardShowcase : MonoBehaviour
    {
        [SerializeField] private TcgCardView cardView;
        [SerializeField, Tooltip("Feste Karte (Fallback, wenn kein Katalog gesetzt ist)")]
        private CardDefinition definition;
        [SerializeField, Tooltip("Wenn gesetzt: jedes Mal eine zufällige Karte aus dem Katalog (ohne Helden)")]
        private CardCatalog randomCatalog;

        private void Start()
        {
            var chosen = definition;
            if (randomCatalog != null)
            {
                var pool = new List<CardDefinition>();
                var withArt = new List<CardDefinition>();
                foreach (var card in randomCatalog.cards)
                {
                    if (card == null || card is PlayerCardData || card.isToken) continue;
                    pool.Add(card);
                    if (card.artwork != null) withArt.Add(card);
                }
                var pick = withArt.Count > 0 ? withArt : pool;
                if (pick.Count > 0) chosen = pick[Random.Range(0, pick.Count)];
            }

            if (cardView == null || chosen == null) return;
            cardView.Show(new CardInstance(chosen, null), false, upright: true);
            cardView.SetHighlight(false);
        }
    }
}
