using TMPro;
using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Alle Assets des "Reliquary"-Kartendesigns (Handoff): Chassis-Sprites je Kartentyp,
    /// Rückseite, Wappen, Badges und die Design-Schriften. Wird vom Generator befüllt
    /// (Menü: Rouge/Card Design) und vom TcgCardView gelesen.
    /// </summary>
    [CreateAssetMenu(fileName = "CardSkin", menuName = "Rouge TCG/Card Skin")]
    public class CardSkin : ScriptableObject
    {
        [Header("Chassis (kompletter Rahmen, 480x672)")]
        public Sprite chassisMonster;
        public Sprite chassisSpell;
        public Sprite chassisArtifact;
        public Sprite chassisPlayer;
        public Sprite chassisReliquary;  // weißes Extra-Deck-Chassis
        [Tooltip("Incarnates (rot) — fehlt das Sprite, wird das Monster-Chassis rot getönt")]
        public Sprite chassisIncarnate;  // rotes Extra-Deck-Chassis

        [Header("Rückseite & Overlays")]
        public Sprite cardBack;
        public Sprite artworkVignette;
        public Sprite whiteFrame;   // 9-slice 1px-Rahmen, tintbar
        public Sprite whiteSquare;  // Pip-Basis

        [Header("Level-Wappen (44x48)")]
        public Sprite crestMonster;
        public Sprite crestSpell;
        public Sprite crestArtifact;

        [Header("Typ-Badges (vertikaler Verlauf, horizontal streckbar)")]
        public Sprite badgeMonster;
        public Sprite badgeSpell;
        public Sprite badgeArtifact;
        public Sprite badgeReliquary;

        [Header("Coin Toss (Cutscene)")]
        public Sprite coinRelic;      // Gold-Vorderseite (RELIC)
        public Sprite coinSeal;       // Silber-Rückseite (SEAL)
        public Sprite coinShadow;     // weiche Boden-Ellipse
        public Sprite coinDustRing;   // Landungs-Staubring
        public Sprite screenVignette; // Rand-Vignette für Cutscenes

        [Header("Schriften (Design-Typografie)")]
        public TMP_FontAsset cinzelSemiBold; // Kartenname
        public TMP_FontAsset cinzelBold;     // Crest-Zahl, Stat-Werte
        public TMP_FontAsset oswaldMedium;   // Attribut/Typ, Stat-Label
        public TMP_FontAsset oswaldSemiBold; // Badge
        public TMP_FontAsset spectral;       // Effekt-Text

        [Header("Duel Field — Board")]
        public Sprite tableBackground;   // 1920x1080 Tisch mit Webmuster
        public Sprite railScrim;         // horizontaler Scrim-Verlauf (rechts gespiegelt einsetzen)
        public Sprite opponentTint;      // kühler Verlauf oben
        public Sprite playerTint;        // warmer Verlauf unten
        public Sprite parchmentPanel;    // 9-slice Pergament mit Border (Log/Ability)

        [Header("Duel Field — Zonen & Piles (112x157)")]
        public Sprite zoneEmptyMonster;
        public Sprite zoneEmptySpell;
        public Sprite zoneEmptyArtifact;
        public Sprite zoneDropTarget;
        public Sprite pileGraveyard;
        public Sprite pileBanished;
        public Sprite pileExtra;         // Extra-Deck-Stapel (Ivory/Gold)
        public Sprite playerSlotSelf;
        public Sprite playerSlotFoe;

        [Header("Kompakte Karten (112x157)")]
        public Sprite compactMonster;
        public Sprite compactSpell;
        public Sprite compactArtifact;
        public Sprite compactPlayer;
        public Sprite compactReliquary;
        [Tooltip("Incarnates (rot) — fehlt das Sprite, wird das Monster-Chassis rot getönt")]
        public Sprite compactIncarnate;
        public Sprite backZone;          // Rückseite in Zonengröße (Weave 13, Diamant 46)
        public Sprite backHand;          // Gegner-Handrücken (62x87, Weave 9, Diamant 26)

        [Header("Shell (Login & Hauptmenü)")]
        public Sprite shellBackground;   // 1920x1080 Radial + Weave + Vignette
        public Sprite relicFill;         // 165°-Panel-Verlauf (Auth-Panel, Overlays)
        public Sprite relicFrame;        // 9-slice: 2px Gold-Rand + innere Keyline (r12)
        public Sprite sweepBand;         // weißes Sweep-Band (Busy-Zustand)
        public Sprite tilePlay;          // Menü-Kacheln 318x452
        public Sprite tileSolo;
        public Sprite tileShop;
        public Sprite tileDecks;
        public Sprite backLogin;         // Kartenrücken 240x336 (Login-Trio)
        public Sprite backThumb;         // Kartenrücken 44x62 (Aktives-Deck-Thumbnail)
        public Sprite badgeEmber;        // Ember-Gradient (#E8B896→#A85E3C) für Shop-CTAs
        public Sprite badgeTeal;         // Teal-Gradient (#A5D8E2→#3B7C8B) für Solo-CTAs

        /// <summary>Kompaktes Chassis für einen Kartentyp (Reliquary VOR Monster prüfen — erbt davon).</summary>
        public Sprite CompactChassisFor(CardDefinition definition)
        {
            switch (definition)
            {
                case ReliquaryCardData _: return compactReliquary != null ? compactReliquary : compactMonster;
                case IncarnateCardData _: return compactIncarnate != null ? compactIncarnate : compactMonster;
                case MonsterCardData _: return compactMonster;
                case SpellCardData _: return compactSpell;
                case ArtifactCardData _: return compactArtifact;
                case PlayerCardData _: return compactPlayer;
                default: return compactMonster;
            }
        }

        /// <summary>Chassis für einen Kartentyp (Player-Karten haben ein eigenes Gold-Chassis ohne Stats).</summary>
        public Sprite ChassisFor(CardDefinition definition)
        {
            switch (definition)
            {
                case ReliquaryCardData _: return chassisReliquary != null ? chassisReliquary : chassisMonster;
                case IncarnateCardData _: return chassisIncarnate != null ? chassisIncarnate : chassisMonster;
                case MonsterCardData _: return chassisMonster;
                case SpellCardData _: return chassisSpell;
                case ArtifactCardData _: return chassisArtifact;
                case PlayerCardData _: return chassisPlayer;
                default: return chassisMonster;
            }
        }

        /// <summary>
        /// Incarnates ohne eigenes Chassis-Sprite bekommen das Monster-Chassis mit
        /// diesem Rot-Tint — so sind sie sofort als „rote Karten" lesbar, bis ein
        /// echtes Incarnate-Chassis eingehängt wird.
        /// </summary>
        public Color ChassisTintFor(CardDefinition definition)
        {
            if (definition is IncarnateCardData && chassisIncarnate == null)
                return new Color(1f, 0.66f, 0.62f);
            return Color.white;
        }
    }
}
