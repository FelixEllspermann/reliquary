using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "GameRules", menuName = "Rouge TCG/Regelwerk")]
    public class GameRules : ScriptableObject
    {
        [Header("Starthände")]
        [Tooltip("Handkarten des Startspielers (zieht in Zug 1 nicht)")]
        [Range(1, 10)] public int startHandTurnPlayer = 5;

        [Tooltip("Handkarten des zweiten Spielers (zieht in seinem Zug normal)")]
        [Range(1, 10)] public int startHandOpponent = 6;

        [Tooltip("Überspringt der Startspieler seine erste Draw Phase?")]
        public bool turnPlayerSkipsFirstDraw = true;

        [Tooltip("Hat der Startspieler in Zug 1 keine Battle Phase?")]
        public bool turnPlayerSkipsFirstBattle = true;

        [Header("Mana")]
        [Tooltip("Mana beider Spieler zu Duellbeginn")]
        [Range(0, 10)] public int startMana = 3;

        [Tooltip("Mana-Zuwachs pro eigener Runde")]
        [Range(0, 5)] public int manaGrowthPerTurn = 1;

        [Tooltip("Obergrenze für das Rundenmana")]
        [Range(1, 20)] public int manaCap = 10;

        [Header("Beschwörung")]
        [Tooltip("Normal Summons pro Zug")]
        [Range(1, 5)] public int normalSummonsPerTurn = 1;

        [Tooltip("Tribute für ein Level-2-Monster")]
        [Range(0, 4)] public int tributesForLevel2 = 1;

        [Tooltip("Tribute für ein Level-3-Monster")]
        [Range(0, 4)] public int tributesForLevel3 = 2;

        [Header("Tokens")]
        [Tooltip("Definition des Illusion-Tokens (Gaslight). Unity: Inspector-Referenz; " +
                 "Server: wird nach dem CardLibrary-Load per Name gesetzt. " +
                 "Der Export überspringt Unity-Objekt-Referenzen automatisch.")]
        public MonsterCardData illusionToken;

        [Header("Deck & Hand")]
        [Tooltip("Minimale Deckgröße")]
        public int deckMinSize = 40;

        [Tooltip("Maximale Deckgröße")]
        public int deckMaxSize = 80;

        [Tooltip("Handkartenlimit — Überschuss wird in der End Phase abgeworfen")]
        [Range(4, 20)] public int handLimit = 8;

        [Tooltip("Maximale Kopien derselben Karte pro Deck (wird vom Deck-Editor geprüft)")]
        [Range(1, 10)] public int maxCopiesPerCard = 3;

        [Tooltip("Maximale Größe des Extra Decks (Reliquary-Karten)")]
        [Range(0, 30)] public int extraDeckMaxSize = 20;

        [Header("Kampf")]
        [Tooltip("Positionswechsel pro Monster und Zug")]
        [Range(0, 5)] public int positionChangesPerTurn = 1;

        public int TributesForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level == 2) return tributesForLevel2;
            return tributesForLevel3;
        }
    }
}
