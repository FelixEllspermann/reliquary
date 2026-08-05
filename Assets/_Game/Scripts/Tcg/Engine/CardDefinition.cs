using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Rouge.Tcg
{
    public abstract class CardDefinition : ScriptableObject
    {
        [Header("Karte")]
        [Tooltip("Anzeigename der Karte")]
        public string cardName = "Neue Karte";

        [Tooltip("Kartenbild (optional)")]
        public Sprite artwork;

        [Tooltip("Seltenheit — bestimmt Pack-Ziehungen und Craft-Kosten")]
        public CardRarity rarity = CardRarity.Common;

        [Header("Effekte (Normal & Infused)")]
        [Tooltip("Alle Effekte dieser Karte. Infused-Effekte werden auf der Karte getrennt dargestellt und kosten Mana.")]
        public List<EffectDefinition> effects = new List<EffectDefinition>();

        public abstract CardKind Kind { get; }

        public abstract Color FrameColor { get; }

        /// <summary>Anzeige-Farbe der Seltenheit (grau/grün/blau/gold).</summary>
        public static Color RarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return new Color(0.45f, 0.85f, 0.45f);
                case CardRarity.Rare: return new Color(0.40f, 0.65f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.72f, 0.20f);
                default: return new Color(0.80f, 0.80f, 0.85f);
            }
        }

        public static string RarityName(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return "Uncommon";
                case CardRarity.Rare: return "Rare";
                case CardRarity.Legendary: return "Legendary";
                default: return "Common";
            }
        }

        /// <summary>Setzt den vollständigen Regeltext der Karte aus den Effekten zusammen.</summary>
        public virtual string BuildRulesText()
        {
            var sb = new StringBuilder();
            foreach (var effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.text)) continue;
                if (sb.Length > 0) sb.AppendLine();
                string infusedName = effect.infusedKind == InfusedKind.Coupled ? "Or Infused" : "Infused";
                string prefix = effect.isInfused
                    ? "[" + infusedName + (effect.manaCost > 0 ? " – " + effect.manaCost + " Mana" : "") + "] "
                    : (effect.manaCost > 0 ? "[" + effect.manaCost + " Mana] " : "");
                sb.Append(prefix).Append(effect.text);
            }
            return sb.ToString();
        }
    }
}
