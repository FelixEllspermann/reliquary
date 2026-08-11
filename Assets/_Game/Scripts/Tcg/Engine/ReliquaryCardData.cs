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

        [Header("Zusatzkosten (bei der Beschwörung bezahlt)")]
        [Tooltip("Banishe so viele Monster aus deinem Friedhof (0 = aus)")]
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
