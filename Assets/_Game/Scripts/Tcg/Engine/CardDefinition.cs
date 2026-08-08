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

        // ---- Dauer-Aura: wirkt, solange die Karte offen auf dem Feld liegt,
        //      auf EIGENE Monster, die die Filter erfüllen (Batch August 2026) ----
        [Header("Dauer-Aura (0 = keine)")]
        [Tooltip("ATK-Bonus für eigene Monster, die die Aura-Filter erfüllen")]
        public int auraAtkBonus;
        [Tooltip("DEF-Bonus für eigene Monster, die die Aura-Filter erfüllen")]
        public int auraDefBonus;
        [Tooltip("Aura nur für Monster, deren Name diesen Text enthält (leer = alle)")]
        public string auraNameFilter = "";
        [Tooltip("Aura nur für Monster dieses Typs?")]
        public bool auraUseTypeFilter;
        public MonsterType auraTypeFilter = MonsterType.Beast;
        [Range(0, 3)]
        [Tooltip("Aura nur für Monster dieses Levels (0 = alle)")]
        public int auraLevelFilter;
        [Tooltip("Aura wirkt nur auf VERDECKTE eigene Monster (Blackout Curtain)")]
        public bool auraOnlyFaceDown;
        [Tooltip("Die Quellkarte selbst bekommt die Aura nicht (»deine anderen Monster«)")]
        public bool auraExcludesSelf;

        [Header("Passive Flaggen (Batch August 2026)")]
        [Tooltip("Der Gegner muss dieses Monster angreifen, solange es offen liegt (Attention Hound)")]
        public bool passiveTaunt;
        [Tooltip(">0: im Kampf unzerstörbar, solange du mindestens N Artefakte kontrollierst (Ironclad)")]
        public int battleShieldMinOwnArtifacts;
        [Tooltip("Zählt beim Reliquary-Tribut als N Monster (Twice-Blessed)")]
        public int tributeWorth = 1;
        [Tooltip("Eigene Karten mit diesem Namen sind für den Gegner kein gültiges Effekt-Ziel (Heavenly Bodyguard)")]
        public string protectsNamedFromTargeting = "";
        [Tooltip("Zweiter Angriff pro Battle Phase, solange ein ANDERES eigenes Monster dieses Attributs offen liegt")]
        public bool conditionalDoubleAttack;
        public MonsterAttribute doubleAttackAttribute = MonsterAttribute.Wind;

        [Tooltip(">0: dieses Monster hat dauerhaft +N ATK pro gezählter Karte (Weight of Evidence)")]
        public int passiveAtkPerCount;
        public EffectCountKind passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;
        [Tooltip(">0: dieses Monster hat dauerhaft +N DEF pro gezählter Karte")]
        public int passiveDefPerCount;
        public EffectCountKind passiveDefPerCountKind = EffectCountKind.OwnArtifactsOnField;

        [Tooltip("Dieses Monster kann nie angreifen (Barrierstruck Peacekeeper)")]
        public bool passiveCannotAttack;

        [Tooltip("Dieses Monster kann im Zug seiner Beschwörung nicht angreifen (Slow to Anger)")]
        public bool passiveNoAttackOnSummonTurn;

        [Tooltip("Feld-Limit (Snugglet): dieses Monster ist nicht beschwörbar/setzbar, solange du " +
                 "bereits N Monster kontrollierst, deren Name diesen Text enthält (leer = kein Limit)")]
        public string fieldLimitName = "";
        [Tooltip("Das N zum Feld-Limit (0 = aus)")]
        public int fieldLimitCount;

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

        /// <summary>
        /// Menschenlesbare Zeilen für alle dauerhaften Passiv-Fähigkeiten dieser Karte.
        /// Die UI (Kartenpanel, Detail-Ansicht) hängt sie als PASSIVE-Block vor die
        /// Effektliste — Daten-Felder wie die Aura hätten sonst keinen Kartentext.
        /// </summary>
        public virtual List<string> BuildPassiveLines()
        {
            var lines = new List<string>();

            if (auraAtkBonus != 0 || auraDefBonus != 0)
            {
                string scope = auraOnlyFaceDown ? "face-down " : "";
                string named = string.IsNullOrEmpty(auraNameFilter) ? "" : $" \"{auraNameFilter}\"";
                string typed = auraUseTypeFilter ? $" {auraTypeFilter.ToString().ToUpperInvariant()}" : "";
                string leveled = auraLevelFilter > 0 ? $" Level {auraLevelFilter}" : "";
                string other = auraExcludesSelf ? "other " : "";
                string bonus = auraAtkBonus != 0 && auraDefBonus != 0
                    ? $"{Signed(auraAtkBonus)} ATK and {Signed(auraDefBonus)} DEF"
                    : auraAtkBonus != 0 ? $"{Signed(auraAtkBonus)} ATK" : $"{Signed(auraDefBonus)} DEF";
                lines.Add($"Your {other}{scope}{named}{typed}{leveled} monsters gain {bonus}.".Replace("  ", " "));
            }

            if (passiveAtkPerCount > 0)
                lines.Add($"This card gains {passiveAtkPerCount} ATK for each of {CountName(passiveAtkPerCountKind)}.");
            if (passiveDefPerCount > 0)
                lines.Add($"This card gains {passiveDefPerCount} DEF for each of {CountName(passiveDefPerCountKind)}.");
            if (passiveCannotAttack)
                lines.Add("This card cannot attack.");
            if (passiveNoAttackOnSummonTurn)
                lines.Add("This card cannot attack during the turn it is Summoned.");
            if (passiveTaunt)
                lines.Add("Your opponent's attacks must target this card.");
            if (battleShieldMinOwnArtifacts > 0)
                lines.Add($"While you control {battleShieldMinOwnArtifacts}+ Artifacts, this card cannot be destroyed by battle.");
            if (tributeWorth > 1)
                lines.Add($"Counts as {tributeWorth} tributes for a Reliquary Summon.");
            if (!string.IsNullOrEmpty(protectsNamedFromTargeting))
                lines.Add($"Your other \"{protectsNamedFromTargeting}\" cards cannot be targeted by your opponent's effects.");
            if (conditionalDoubleAttack)
                lines.Add($"Can attack twice each Battle Phase while you control another face-up {doubleAttackAttribute.ToString().ToUpperInvariant()} monster.");
            if (fieldLimitCount > 0 && !string.IsNullOrEmpty(fieldLimitName))
                lines.Add($"You cannot Summon or Set this card while you control {fieldLimitCount} \"{fieldLimitName}\" monsters.");

            return lines;
        }

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();

        /// <summary>Zählbasis als Kartentext ("each of your Artifacts on the field").</summary>
        protected static string CountName(EffectCountKind kind)
        {
            switch (kind)
            {
                case EffectCountKind.OwnArtifactsOnField: return "your Artifacts on the field";
                case EffectCountKind.OwnGraveyardArtifacts: return "the Artifacts in your Graveyard";
                case EffectCountKind.OwnFaceDownMonsters: return "your face-down monsters";
                case EffectCountKind.OwnBanishedMonsters: return "your banished monsters";
                case EffectCountKind.OwnGraveyardCards: return "the cards in your Graveyard";
                case EffectCountKind.EquippedArtifactsOnSelf: return "its equipped Artifacts";
                case EffectCountKind.OpponentFaceDownMonsters: return "your opponent's face-down monsters";
                default: return "your monsters on the field";
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
