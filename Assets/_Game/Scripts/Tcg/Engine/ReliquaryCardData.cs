using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Reliquary-Karte (Extra Deck): ein Monster mit weißem Rahmen, das jederzeit in der
    /// eigenen Main Phase beschworen werden kann, solange die Voraussetzungen erfüllt sind
    /// und die Kosten bezahlt werden. Verlässt es das Feld, kehrt es ins Extra Deck zurück.
    /// </summary>
    [CreateAssetMenu(fileName = "NeueReliquary", menuName = "Rouge TCG/Reliquary Card")]
    public class ReliquaryCardData : MonsterCardData
    {
        /// <summary>Weißer Reliquary-Rahmen.</summary>
        public override Color FrameColor => new Color(0.94f, 0.91f, 0.83f);

        [Header("Reliquary — Beschwörung aus dem Extra Deck")]
        [Tooltip("Anzeigetext der Beschwörungs-Bedingung und -Kosten (steht auf der Karte)")]
        [TextArea] public string summonText = "";

        [Tooltip("Mana-Kosten der Beschwörung")]
        [Range(0, 9)] public int summonManaCost;

        [Header("Voraussetzungen (alle gesetzten müssen erfüllt sein)")]
        [Tooltip("Du kontrollierst offene Monster, deren Name diesen Text enthält")]
        public string reqNamedOnField = "";
        [Tooltip("Wie viele solcher Monster nötig sind")]
        [Range(1, 5)] public int reqNamedCount = 1;

        [Tooltip("Deine LP müssen niedriger sein als die des Gegners")]
        public bool reqLifeBelowOpponent;

        [Tooltip("Der Gegner kontrolliert mehr Monster als du")]
        public bool reqOpponentMoreMonsters;

        [Tooltip("Der Gegner kontrolliert mindestens so viele Monster (0 = aus)")]
        [Range(0, 5)] public int reqOpponentMonstersAtLeast;

        [Tooltip("Der Gegner kontrolliert mindestens 1 Monster, dessen Name diesen Text enthält (leer = aus)")]
        public string reqOpponentNamedOnField = "";

        [Tooltip("Du hast mindestens so viel Mana verfügbar (Bedingung, keine Kosten; 0 = aus)")]
        [Range(0, 10)] public int reqMinMana;

        [Tooltip("Du kontrollierst mindestens so viele Artefakte (0 = aus)")]
        [Range(0, 2)] public int reqOwnArtifactsOnField;

        [Tooltip("Mindestens so viele Artefakte in deinem Friedhof (0 = aus)")]
        [Range(0, 10)] public int reqOwnArtifactsInGrave;

        [Tooltip("Du kontrollierst mindestens so viele verdeckte Monster (0 = aus)")]
        [Range(0, 5)] public int reqOwnFaceDownMonsters;

        [Tooltip("Du kontrollierst mindestens 1 Monster mit ausgerüstetem Artefakt")]
        public bool reqMonsterWithEquip;

        [Tooltip("Mindestens so viele Karten in deinem Friedhof (0 = aus)")]
        [Range(0, 20)] public int reqGraveyardAtLeast;

        [Tooltip("Nur beschwörbar mit mindestens so vielen MONSTERN im eigenen Friedhof (King of Deckay)")]
        [Range(0, 20)] public int reqGraveyardMonstersAtLeast;

        [Tooltip("Mindestzahl an Zauberkarten im eigenen Friedhof")]
        [Range(0, 20)] public int reqGraveyardSpellsAtLeast;

        [Tooltip("Mindestzahl an Karten im GEGNERISCHEN Friedhof")]
        [Range(0, 20)] public int reqOpponentGraveyardAtLeast;

        [Tooltip("Bedingung: Du kontrollierst KEIN Monster")]
        public bool reqControlNoMonsters;

        [Tooltip("Du kontrollierst mindestens so viele Monster (0 = aus)")]
        [Range(0, 5)] public int reqOwnMonstersAtLeast;

        [Tooltip("Deine LP sind höchstens so hoch (0 = aus)")]
        public int reqLifeAtMost;

        [Tooltip("Mindestens so viele Karten in deinem Banishment (0 = aus)")]
        [Range(0, 20)] public int reqBanishedAtLeast;

        [Tooltip("KEINE Reliquary auf einem der Felder oder in einer der Verbannungen (Immortal Demon)")]
        public bool reqNoReliquariesOnFieldOrBanish;

        [Tooltip("Mindestens so viele Reliquaries in DEINEM Friedhof (0 = aus)")]
        [Range(0, 10)] public int reqReliquariesInGraveAtLeast;

        [Tooltip("Der Gegner kontrolliert ein Monster mit mindestens so viel ATK (0 = aus)")]
        public int reqOpponentMonsterAtkAtLeast;

        [Header("The Small Print")]
        [Tooltip("Du hast KEINE Handkarten (The Ascetic of the Ninth Stair)")]
        public bool reqHandEmpty;

        [Tooltip("Ein Monster auf dem Feld wird von jemand anderem kontrolliert als seinem Besitzer (Broker of Both Sides)")]
        public bool reqControlChangedOnField;

        [Header("5 Archetypes (September 2026)")]
        [Tooltip("Diesen Zug wurden mindestens so viele Deals geschlossen (0 = aus; Splithoof)")]
        public int reqDealsThisTurn;

        [Tooltip("In diesem Duell wurden mindestens so viele Deals geschlossen (0 = aus; Splithoof)")]
        public int reqDealsThisDuel;

        [Tooltip("Wie viele Monster die reqOpponentNamedOnField-Bedingung erfüllen müssen (0 = 1; Giftwyrm-Hamper: 2)")]
        public int reqOpponentNamedCount;

        [Tooltip("Der Gegner hat in diesem oder im letzten Zug angegriffen (Waylay)")]
        public bool reqOpponentAttackedRecently;

        [Tooltip("Du kontrollierst mindestens so viele Karten mit Countdown-Markern (0 = aus; Chimekeep)")]
        public int reqOwnCountdownCards;

        [Tooltip("Mindestens so viele Karten mit diesem Namensteil in deinem Friedhof (0 = aus; Waylay-König)")]
        public int reqGraveyardNamedCount;

        [Tooltip("Namensfilter für die Friedhofs-Bedingung darüber")]
        public string reqGraveyardNamed = "";

        // =====================================================================
        // VERALTET seit 28.08.2026 (Design-Regel): Reliquary-Beschwörungen
        // verlangen KEINE Aktionen mehr — nur Board-State-Voraussetzungen (reqs)
        // + Mana. Aktionskosten (Tribute, Grab-Banish, Zerstören) sind für eine
        // KÜNFTIGE neue Kartenart reserviert. Diese Felder bleiben nur, weil
        // Assets Zahlenwerte speichern — auf KEINER Karte mehr setzen!
        // =====================================================================
        [Header("Zusatzkosten — VERALTET, nicht mehr benutzen (siehe Kommentar)")]
        [Tooltip("VERALTET: Banishe so viele Monster aus deinem Friedhof (0 = aus)")]
        [Range(0, 10)] public int costBanishMonstersFromGrave;

        [Tooltip("Zerstöre 1 anderes Monster, das du kontrollierst")]
        public bool costTributeOtherMonster;

        /// <summary>
        /// Tribute von BEIDEN Feldern. Damit ist die Beschwörung selbst das
        /// Removal: sie kostet nicht nur eigene Karten, sie räumt auch drüben ab.
        ///
        /// Wer hier etwas einträgt, sollte wissen, was er dem Archetyp antut —
        /// verlangt eine Reliquary gegnerische Monster, ist sie eine tote Karte,
        /// solange das andere Feld leer ist. Das ist gewollt, aber es gehört auf
        /// die Karte geschrieben.
        /// </summary>
        [Tooltip("Opfere so viele eigene Monster (zusätzlich zu costTributeOtherMonster; 0 = aus)")]
        [Range(0, 3)] public int costTributeOwnMonsters;

        [Tooltip("Opfere so viele GEGNERISCHE Monster — die Beschwörung ist damit auch Removal (0 = aus)")]
        [Range(0, 2)] public int costTributeOpponentMonsters;
    }
}
