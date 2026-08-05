using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Alle Audio-Clips an einer Stelle. Liegt unter Resources, damit SfxManager und
    /// MusicManager sie ohne Verdrahtung in jeder Szene finden — auch bei einem
    /// Direktstart aus dem Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Rouge TCG/Audio-Bibliothek")]
    public class AudioLibrary : ScriptableObject
    {
        public const string ResourcePath = "AudioLibrary";

        [Header("Oberfläche")]
        [Tooltip("Kurzer Ton beim Überfahren von Buttons und Menü-Kacheln")]
        public AudioClip hover;

        [Tooltip("Klick auf einen Button")]
        public AudioClip buttonPress;

        [Tooltip("Tastenanschlag in Eingabefeldern (Login)")]
        public AudioClip type;

        [Tooltip("Belohnung/Bestätigung — erfolgreicher Login, Daily Claim")]
        public AudioClip claim;

        [Header("Duell")]
        [Tooltip("Karte im Duell überfahren")]
        public AudioClip cardHover;

        [Tooltip("Karte wird gezogen")]
        public AudioClip cardDraw;

        [Tooltip("Karte wird auf dem Feld abgelegt (Beschwörung, Spell, Artefakt, Set)")]
        public AudioClip cardPlace;

        [Tooltip("Karteneffekt wird aktiviert")]
        public AudioClip cardActivate;

        [Tooltip("Treffer — Angriff auf eine Karte oder direkt auf den Spieler")]
        public AudioClip hit;

        [Tooltip("Karte wird zerstört")]
        public AudioClip destroyed;

        [Tooltip("Karte fliegt in Friedhof oder Verbannung")]
        public AudioClip cardMoving;

        [Header("Münzwurf")]
        [Tooltip("Die Münze wird geworfen")]
        public AudioClip coinToss;

        [Tooltip("Eine volle Umdrehung der Münze — klingt einmal pro Drehung")]
        public AudioClip coinTurn;

        [Tooltip("Die Münze schlägt auf")]
        public AudioClip coinHit;

        [Header("Übergänge")]
        [Tooltip("Ein Schloss des Siegels löst aus — klingt sechsmal im Login-Übergang")]
        public AudioClip sealUnlock;

        [Tooltip("Das Siegel bricht auf")]
        public AudioClip sealOpen;

        [Tooltip("Das Deck wird gemischt — Taumel-Phase des Duell-Übergangs")]
        public AudioClip cardShuffle;

        [Header("Musik")]
        [Tooltip("Login-/Titelscreen")]
        public AudioClip loginMusic;

        [Tooltip("Hauptmenü, Shop, Deck Builder, Duel-Setup")]
        public AudioClip menuMusic;

        [Tooltip("Münzwurf-Cutscene vor dem Duell")]
        public AudioClip tossMusic;

        [Tooltip("Duell — pro Duell wird zufällig einer dieser Tracks gewählt")]
        public AudioClip[] duelMusic;

        private static AudioLibrary cached;

        /// <summary>Lädt die Bibliothek aus Resources (Ergebnis wird gemerkt).</summary>
        public static AudioLibrary Load()
        {
            if (cached == null) cached = Resources.Load<AudioLibrary>(ResourcePath);
            return cached;
        }
    }
}
