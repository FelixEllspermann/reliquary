using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Ein Karten-Pack: Name, Beschreibung und der Pool an Karten, die darin gezogen
    /// werden können. Die Ziehungs-Wahrscheinlichkeiten pro Rarity setzt der Server.
    /// </summary>
    [CreateAssetMenu(fileName = "NeuesPack", menuName = "Rouge TCG/Karten-Pack")]
    public class CardPackDefinition : ScriptableObject
    {
        [Tooltip("Anzeigename des Packs")]
        public string packName = "Neues Pack";

        [TextArea(2, 4)]
        [Tooltip("Beschreibung (im Pack-Shop angezeigt)")]
        public string description = "";

        [Tooltip("Akzentfarbe des Packs")]
        public Color packColor = new Color(0.85f, 0.65f, 0.25f);

        [Tooltip("Preis in Coins (Ingame-Währung)")]
        public int price = 150;

        [Tooltip("Unique-Pack (Hero Cache): zieht genau EINE Karte aus dem Pool, die dem " +
                 "Konto noch fehlt. Muss zum Flag \"unique\" in Server/data/packs.json passen. " +
                 "raritySlots und legendaryUpgradeChance sind dann bedeutungslos.")]
        public bool uniqueDraw;

        [Tooltip("Alle Karten, die in diesem Pack gezogen werden können. LEER lassen = das Pack " +
                 "enthält automatisch IMMER alle Karten des Katalogs außer Helden (Relic Pack).")]
        public List<CardDefinition> cardPool = new List<CardDefinition>();

        [System.NonSerialized] private List<CardDefinition> resolvedAllCache;

        /// <summary>
        /// Der effektive Karten-Pool. Ein LEERER cardPool bedeutet: dieses Pack enthält
        /// immer ALLE Karten des Katalogs (außer Helden-Karten) — neue Sets landen damit
        /// automatisch im Pack, ohne dass die Liste je wieder gepflegt werden muss.
        /// Muss zur "cards": "all"-Auflösung in Server/server.js passen.
        /// </summary>
        public List<CardDefinition> ResolvePool(CardCatalog catalog)
        {
            if (cardPool != null && cardPool.Count > 0) return cardPool;
            if (resolvedAllCache == null && catalog != null)
            {
                resolvedAllCache = new List<CardDefinition>();
                foreach (var card in catalog.cards)
                    if (card != null && !(card is PlayerCardData) && !card.isToken) resolvedAllCache.Add(card);
            }
            return resolvedAllCache ?? cardPool;
        }

        [Tooltip("Rarity pro Karten-Slot einer Öffnung (Reihenfolge = Aufdeck-Reihenfolge). " +
                 "Fehlt eine Rarity im Pool, fällt der Server auf die nächste verfügbare zurück.")]
        public List<CardRarity> raritySlots = new List<CardRarity>
        {
            CardRarity.Common, CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare, CardRarity.Rare
        };

        [Range(0f, 1f)]
        [Tooltip("Chance, dass der letzte Slot statt seiner normalen Rarity eine Legendary wird. " +
                 "0 = Legendaries sind aus Packs nicht ziehbar.\n\n" +
                 "ACHTUNG: Gezogen wird auf dem Server — dieser Wert muss mit legendaryChance " +
                 "in Server/data/packs.json übereinstimmen, sonst zeigt die Odds-Ansicht etwas anderes an, " +
                 "als tatsächlich passiert.")]
        public float legendaryUpgradeChance = 0.15f;

        [TextArea(1, 2)]
        [Tooltip("Kurzer Verkaufs-Text für die Shop-Kachel (Parchment-Strip)")]
        public string tagline = "";
    }
}
