using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Ein wählbarer Solo-Gegner: Anzeigename, Deck (inkl. Extra Deck über DeckDefinition),
    /// optionale LP-/Mana-Modifikatoren und Novice-Verhalten. Wird im Duel-Setup gewählt
    /// und über den MatchContext an die Duel-Szene übergeben.
    /// </summary>
    [CreateAssetMenu(fileName = "NeuerBotGegner", menuName = "Rouge TCG/Bot Opponent")]
    public class BotOpponentDefinition : ScriptableObject
    {
        [Tooltip("Angezeigter Name des Gegners")]
        public string displayName = "The Warden";

        [Tooltip("Kurzzeile im Auswahl-Chip (z.B. 'GRAVE RECURSION · REAL AI')")]
        public string chipNote = "";

        [Tooltip("Beschreibung im Mode-Banner (Taktik-Hinweis für den Spieler)")]
        [TextArea] public string blurb = "";

        [Tooltip("Deck des Gegners (Extra Deck über das extraCards-Feld des Decks)")]
        public DeckDefinition deck;

        [Tooltip("LP-Override (0 = Standard der Heldenkarte)")]
        public int lifePointsOverride;

        [Tooltip("Zusätzliches Mana pro Zug")]
        [Range(0, 5)] public int bonusManaPerTurn;

        [Tooltip("Novice: reagiert nie im Gegnerzug (keine Quick-Effekte)")]
        public bool noviceMode;
    }
}
