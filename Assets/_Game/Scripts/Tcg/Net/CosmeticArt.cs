using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>
    /// Die Grafiken zu den Kosmetik-Ids. Der Katalog kommt vom Server, die Bilder
    /// liegen im Client unter <c>Resources/Cosmetics</c> — die Id ist das Bindeglied.
    ///
    /// <b>Eine fehlende Grafik fällt still auf Vanilla zurück</b> — nie ein
    /// Platzhalter, nie ein Ladekringel. Ein Server, der einen Gegenstand kennt,
    /// den dieser Client noch nicht hat, sieht damit einfach aus wie vorher.
    /// Erzeugt werden die Bilder von <c>Rouge/Cosmetics/Generate Art</c>.
    /// </summary>
    public static class CosmeticArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>Kartenrücken (Fach „sleeve").</summary>
        public static Sprite CardBack(string id) => Load("back_", id);

        /// <summary>Spielmatte (Fach „duelMat").</summary>
        public static Sprite Mat(string id) => Load("mat_", id);

        /// <summary>Wurfmünze, RELIC-Seite (Fach „tossCoin").</summary>
        public static Sprite CoinRelic(string id) => Load("coin_", id, "_relic");

        /// <summary>Wurfmünze, SEAL-Seite.</summary>
        public static Sprite CoinSeal(string id) => Load("coin_", id, "_seal");

        /// <summary>Profilrahmen (Fach „avatarFrame").</summary>
        public static Sprite Frame(string id) => Load("frame_", id);

        /// <summary>Shop-Icon — jeder Gegenstand hat eines, auch die Titel.</summary>
        public static Sprite Icon(string id) => Load("icon_", id);

        // ---- Kurzformen auf das, was der Spieler gerade trägt ----

        public static Sprite EquippedCardBack() => CardBack(Cosmetics.EquippedIn("sleeve"));
        public static Sprite EquippedMat() => Mat(Cosmetics.EquippedIn("duelMat"));
        public static Sprite EquippedCoinRelic() => CoinRelic(Cosmetics.EquippedIn("tossCoin"));
        public static Sprite EquippedCoinSeal() => CoinSeal(Cosmetics.EquippedIn("tossCoin"));
        public static Sprite EquippedFrame() => Frame(Cosmetics.EquippedIn("avatarFrame"));

        /// <summary>Die Matte des Gegners — sie liegt auf seiner Bretthälfte.</summary>
        public static Sprite RemoteMat() => Mat(MatchContext.RemoteEquipped("duelMat"));

        // ---- Die Wurfmünze gehört beiden ----
        //
        // Anders als die Matte lässt sich die Münze nicht teilen: es fliegt genau
        // eine, und beide sehen dieselbe. Also entscheidet eine feste Regel —
        // <b>Spieler A gewinnt</b>, immer. Eine Regel, die man einmal lernt, ist
        // besser als eine, die mal so und mal anders ausgeht; und beide Clients
        // kommen so ohne Absprache auf dasselbe Bild.

        private static string HostSide(string slot)
        {
            bool network = MatchContext.IsServerMatch;
            if (!network) return Cosmetics.EquippedIn(slot);
            return MatchContext.LocalIsPlayerA
                ? Cosmetics.EquippedIn(slot)
                : MatchContext.RemoteEquipped(slot);
        }

        public static Sprite MatchCoinRelic() => CoinRelic(HostSide("tossCoin"));
        public static Sprite MatchCoinSeal() => CoinSeal(HostSide("tossCoin"));

        private static Sprite Load(string prefix, string id, string suffix = "")
        {
            if (string.IsNullOrEmpty(id)) return null;
            string key = prefix + id + suffix;
            if (cache.TryGetValue(key, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>("Cosmetics/" + key);
            cache[key] = sprite;   // auch null merken: nicht bei jedem Frame neu suchen
            return sprite;
        }
    }
}
