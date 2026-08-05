using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Data
{
    public enum CardType { Attack, Skill, Power }

    public enum TargetType { Enemy, Self }

    public enum EffectType { Damage, Block, Heal, DrawCards, GainEnergy }

    [Serializable]
    public class CardEffect
    {
        [Tooltip("Was dieser Effekt tut")]
        public EffectType type = EffectType.Damage;

        [Tooltip("Stärke des Effekts (Schaden, Block, Heilung, Kartenanzahl, Energie)")]
        public int value = 1;
    }

    [CreateAssetMenu(fileName = "NeueKarte", menuName = "Rouge/Karte")]
    public class CardData : ScriptableObject
    {
        [Header("Darstellung")]
        [Tooltip("Anzeigename der Karte")]
        public string cardName = "Neue Karte";

        [TextArea]
        [Tooltip("Beschreibung. Platzhalter {0}, {1}, ... werden durch die Effekt-Werte ersetzt.")]
        public string description = "Füge {0} Schaden zu.";

        [Tooltip("Kartenbild (optional, leer = nur Rahmenfarbe)")]
        public Sprite artwork;

        [Tooltip("Rahmenfarbe der Karte")]
        public Color frameColor = Color.white;

        [Header("Spielwerte")]
        [Range(0, 10)]
        [Tooltip("Energiekosten beim Ausspielen")]
        public int cost = 1;

        [Tooltip("Kartentyp (aktuell rein informativ)")]
        public CardType cardType = CardType.Attack;

        [Tooltip("Ziel der Karte")]
        public TargetType target = TargetType.Enemy;

        [Tooltip("Effekte, die beim Ausspielen der Reihe nach ausgeführt werden")]
        public List<CardEffect> effects = new List<CardEffect>();

        public string GetFormattedDescription()
        {
            if (effects.Count == 0) return description;

            object[] values = new object[effects.Count];
            for (int i = 0; i < effects.Count; i++) values[i] = effects[i].value;

            try { return string.Format(description, values); }
            catch (FormatException) { return description; }
        }
    }
}
