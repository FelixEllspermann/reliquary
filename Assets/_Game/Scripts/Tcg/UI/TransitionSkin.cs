using TMPro;
using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Die Handvoll Sprites und Schriften, die die Übergänge (Vault Enter, Duel Load)
    /// brauchen. Sie bauen ihre Oberfläche zur Laufzeit und überleben den Szenenwechsel,
    /// können also keine Szenen-Referenzen benutzen — deshalb liegt das hier in
    /// <c>Resources</c> und wird per <see cref="Load"/> geholt.
    ///
    /// Befüllt wird das Asset aus dem CardSkin über Menü: Rouge TCG/Rebuild Transition Skin.
    /// </summary>
    [CreateAssetMenu(fileName = "TransitionSkin", menuName = "Rouge TCG/Transition Skin")]
    public class TransitionSkin : ScriptableObject
    {
        [Header("Bausteine")]
        public Sprite frame;      // 9-slice 1px-Rahmen, tintbar
        public Sprite square;     // volles Quadrat (Pips, Kerne)
        public Sprite glow;       // weicher radialer Verlauf (smoothstep bis zum Rand)
        public Sprite flare;      // harter Lichtkern, aussen früh transparent
        public Sprite weave;      // 40px-Kachel mit zwei Diagonalen (Tiled einsetzen)
        public Sprite rule;       // waagerechter Verlauf für Zierstriche
        public Sprite fade;       // senkrechter Verlauf (unten transparent, oben deckend)
        public Sprite vignette;   // Rand-Abdunklung (innerer Schatten)

        [Header("Karten-Finishes (Texturen, werden gescrollt)")]
        [Tooltip("Additives Material — hellt auf, statt das Artwork zu überdecken")]
        public Material additive;
        public Texture finishGloss;
        public Texture finishRainbow;
        public Texture finishGrating;
        public Texture finishScanlines;
        public Texture finishNoise;
        public Texture finishBand;

        [Header("Rang-Embleme")]
        public Sprite diagFade;     // 135°-Verlauf, oben links deckend
        public Sprite dashedRing;   // gestrichelter Kreis (Vault Seal)

        [Header("Vault Enter")]
        public Sprite seal;         // Siegelkörper (goldene Scheibe mit Rand)
        public Sprite ring;         // dünner Kreisring (Schlossring)
        public Sprite reliefOuter;  // Quadrat-Umriss, Rand 4,1 %
        public Sprite reliefInner;  // Quadrat-Umriss, Rand 5,8 %, 20 % Füllung

        [Header("Duel Load")]
        public Sprite cardBack;       // Kartenrücken in Ladegröße
        public Sprite parchment;      // Pergament-Streifen (Banner-Notiz)
        public Sprite zoneMonster;    // leere Monsterzone (gold)
        public Sprite zoneSpell;      // leere Zauberzone (teal)
        public Sprite zoneArtifact;   // leere Artefaktzone (violett)

        [Header("Schriften")]
        public TMP_FontAsset cinzel;
        public TMP_FontAsset oswald;
        public TMP_FontAsset spectral;

        private static TransitionSkin cached;

        /// <summary>Holt das Asset aus Resources (einmal, danach gecacht).</summary>
        public static TransitionSkin Load()
        {
            if (cached == null) cached = Resources.Load<TransitionSkin>("TransitionSkin");
            return cached;
        }
    }
}
