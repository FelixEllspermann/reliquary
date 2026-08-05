using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Data
{
    public enum IntentType { Attack, Defend }

    [Serializable]
    public class EnemyIntent
    {
        [Tooltip("Was der Gegner in diesem Zug tut")]
        public IntentType type = IntentType.Attack;

        [Tooltip("Stärke (Schaden bzw. Block)")]
        public int value = 5;
    }

    [CreateAssetMenu(fileName = "NeuerGegner", menuName = "Rouge/Gegner")]
    public class EnemyData : ScriptableObject
    {
        [Header("Darstellung")]
        [Tooltip("Anzeigename des Gegners")]
        public string enemyName = "Gegner";

        [Tooltip("Sprite (optional, leer = Platzhalter aus der Szene)")]
        public Sprite sprite;

        [Tooltip("Einfärbung des Sprites")]
        public Color tint = Color.white;

        [Header("Spielwerte")]
        [Tooltip("Maximale Lebenspunkte")]
        public int maxHealth = 40;

        [Tooltip("Zugmuster — wird von oben nach unten abgearbeitet und dann wiederholt")]
        public List<EnemyIntent> pattern = new List<EnemyIntent>();
    }
}
