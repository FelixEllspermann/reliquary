using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>
    /// Kosmetik-Katalog und Besitzstand, wie der Server sie schickt. Nichts davon
    /// berührt das Spiel — Ausrüstung ändert nur, wie Dinge aussehen.
    ///
    /// Bezahlt wird ausschliesslich mit Coins. Wer ein Sonderexemplar zerlegt,
    /// bekommt ebenfalls Coins — es gibt nur einen Topf.
    ///
    /// Der Katalog kommt mit jedem Profil mit, damit der Laden keinen eigenen
    /// Abruf braucht und nach einem Serverupdate sofort aktuell ist.
    /// </summary>
    public class CosmeticItem
    {
        public string Id;
        public string Name;
        public string Slot;
        public string Rarity;      // common | rare | epic | relic
        public int Price;          // in Coins; -1 = nicht käuflich
        public string Currency;    // "coins" oder "" (unverkäuflich)
        public string Unlock;      // wie man ihn sonst bekommt

        public bool ForSale => Price >= 0 && Currency == "coins";

        /// <summary>Farbe der Seltenheit — dieselbe Skala wie im Profil.</summary>
        public Color Accent
        {
            get
            {
                string hex;
                switch (Rarity)
                {
                    case "rare": hex = "#8FC6D2"; break;
                    case "epic": hex = "#B9A3E0"; break;
                    case "relic": hex = "#EBCE8A"; break;
                    default: hex = "#A2917A"; break;
                }
                ColorUtility.TryParseHtmlString(hex, out var color);
                return color;
            }
        }
    }

    public static class Cosmetics
    {
        /// <summary>Anzeigenamen der Fächer, Schlüssel wie beim Server.</summary>
        private static readonly Dictionary<string, string> SlotNames = new Dictionary<string, string>
        {
            { "sleeve", "Card sleeve" },
            { "avatarFrame", "Avatar frame" },
            { "avatar", "Profile picture" },
            { "tossCoin", "Toss coin" },
            { "duelMat", "Duel mat" },
            { "title", "Profile title" },
            { "victorySeal", "Victory seal" },
        };

        /// <summary>Der gesamte Laden, in Server-Reihenfolge.</summary>
        public static readonly List<CosmeticItem> Catalog = new List<CosmeticItem>();

        /// <summary>Ids, die dem Spieler gehören.</summary>
        public static readonly HashSet<string> Owned = new HashSet<string>();

        /// <summary>Fach -> ausgerüstete Id (leer = nichts).</summary>
        public static readonly Dictionary<string, string> Equipped = new Dictionary<string, string>();

        public static string SlotName(string slot) =>
            slot != null && SlotNames.TryGetValue(slot, out var name) ? name : slot;

        public static CosmeticItem Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var item in Catalog) if (item.Id == id) return item;
            return null;
        }

        public static bool Owns(string id) => !string.IsNullOrEmpty(id) && Owned.Contains(id);

        public static string EquippedIn(string slot) =>
            slot != null && Equipped.TryGetValue(slot, out var id) ? id : "";

        /// <summary>Alle Gegenstände eines Fachs, in Katalogreihenfolge.</summary>
        public static List<CosmeticItem> InSlot(string slot)
        {
            var result = new List<CosmeticItem>();
            foreach (var item in Catalog) if (item.Slot == slot) result.Add(item);
            return result;
        }

        /// <summary>Kann sich der Spieler das gerade leisten?</summary>
        public static bool CanAfford(CosmeticItem item) =>
            item != null && item.ForSale && PlayerProfile.Coins >= item.Price;

        internal static void Apply(NetProfile profile)
        {
            if (profile.shopIds != null)
            {
                Catalog.Clear();
                for (int i = 0; i < profile.shopIds.Length; i++)
                    Catalog.Add(new CosmeticItem
                    {
                        Id = profile.shopIds[i],
                        Name = At(profile.shopNames, i),
                        Slot = At(profile.shopSlots, i),
                        Rarity = At(profile.shopRarities, i),
                        Price = profile.shopPrices != null && i < profile.shopPrices.Length ? profile.shopPrices[i] : -1,
                        Currency = At(profile.shopCurrencies, i),
                        Unlock = At(profile.shopUnlocks, i),
                    });
            }

            Owned.Clear();
            if (profile.cosmeticsOwned != null)
                foreach (var id in profile.cosmeticsOwned) Owned.Add(id);

            Equipped.Clear();
            if (profile.equippedSlots != null && profile.equippedIds != null)
                for (int i = 0; i < profile.equippedSlots.Length && i < profile.equippedIds.Length; i++)
                    if (!string.IsNullOrEmpty(profile.equippedIds[i]))
                        Equipped[profile.equippedSlots[i]] = profile.equippedIds[i];
        }

        private static string At(string[] array, int index) =>
            array != null && index < array.Length && array[index] != null ? array[index] : "";
    }
}
