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

        // Zwei Bauarten von Rahmen: die alten sind RINGE, die sich über die
        // Portrait-Kachel legen; die gemalte Reihe sind BILDERRAHMEN mit
        // eigenem Fenster und eigener Silhouette. Ein Bilderrahmen ersetzt
        // die Kachel, statt auf ihr zu liegen — sonst lugt sie an den Ecken
        // hervor, und breite Motive (Schwingen, Panzerhandschuhe) würden in
        // das quadratische Feld gequetscht.
        // Fensterhöhe je Bilderrahmen, in Pixeln der 1024er-Leinwand (per Skript
        // ausgemessen). Skaliert wird aufs FENSTER, nicht auf die Leinwand:
        // bei Fiendwing fressen die Schwingen die Breite — wer die Leinwand
        // normiert, bekommt ein winziges Fenster, und das Portrait dahinter
        // wirkt je nach Rahmen verschieden gross.
        private static readonly Dictionary<string, float> plaqueWindow = new Dictionary<string, float>
        {
            { "rootbound", 444f }, { "pyre_mantle", 431f }, { "stormlace", 398f },
            { "gilded_grasp", 311f }, { "fiendwing", 261f },
        };

        /// <summary>Ist dieser Rahmen ein Bilderrahmen (statt eines Rings)?</summary>
        public static bool IsPlaque(string id) => !string.IsNullOrEmpty(id) && plaqueWindow.ContainsKey(id);

        /// <summary>
        /// Faktor, mit dem die Leinwand gezeichnet werden muss, damit das
        /// Fenster <paramref name="targetWindow"/> Pixel hoch erscheint.
        /// </summary>
        public static float PlaqueScale(string id, float targetWindow)
            => plaqueWindow.TryGetValue(id ?? "", out var window) ? targetWindow / window : 1f;

        /// <summary>Profilbild (Fach „avatar") — ersetzt die Initiale auf der Kachel.</summary>
        public static Sprite Avatar(string id) => Load("avatar_", id);

        /// <summary>Shop-Icon — jeder Gegenstand hat eines, auch die Titel.</summary>
        public static Sprite Icon(string id) => Load("icon_", id);

        // ---- Kurzformen auf das, was der Spieler gerade trägt ----

        public static Sprite EquippedCardBack() => CardBack(Cosmetics.EquippedIn("sleeve"));
        public static Sprite EquippedMat() => Mat(Cosmetics.EquippedIn("duelMat"));
        public static Sprite EquippedCoinRelic() => CoinRelic(Cosmetics.EquippedIn("tossCoin"));
        public static Sprite EquippedCoinSeal() => CoinSeal(Cosmetics.EquippedIn("tossCoin"));
        public static Sprite EquippedFrame() => Frame(Cosmetics.EquippedIn("avatarFrame"));
        public static Sprite EquippedAvatar() => Avatar(Cosmetics.EquippedIn("avatar"));

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
