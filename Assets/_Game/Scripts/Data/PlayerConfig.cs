using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Data
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Rouge/Spieler-Konfiguration")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Spieler")]
        [Tooltip("Anzeigename")]
        public string playerName = "Held";

        [Tooltip("Maximale Lebenspunkte")]
        public int maxHealth = 80;

        [Header("Zug-Regeln")]
        [Range(1, 10)]
        [Tooltip("Energie zu Beginn jedes Zuges")]
        public int energyPerTurn = 3;

        [Range(1, 10)]
        [Tooltip("Karten, die zu Zugbeginn gezogen werden")]
        public int drawPerTurn = 5;

        [Range(1, 12)]
        [Tooltip("Maximale Handgröße — darüber hinaus wird nicht gezogen")]
        public int maxHandSize = 10;

        [Header("Startdeck")]
        [Tooltip("Karten, mit denen der Kampf beginnt (Duplikate erlaubt)")]
        public List<CardData> startingDeck = new List<CardData>();
    }
}
