using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Eine Ebene des Turms: Keeper, Deck (über den Solo-Gegner), optionale
    /// Modifikatoren, der Dialog davor und die Siegzeile danach. Das Portrait
    /// ist ein freies Sprite-Feld — die Bilder kommen später und werden hier
    /// im Inspector zugewiesen; bis dahin greift das Artwork der Heldenkarte
    /// des Gegner-Decks als Platzhalter.
    ///
    /// WICHTIG: Die Klasse MUSS in einer gleichnamigen Datei liegen — sonst
    /// verlieren die .asset-Dateien beim Laden ihre Script-Bindung (gelernt
    /// auf die harte Tour: 15 Ebenen, alle null).
    /// </summary>
    [CreateAssetMenu(fileName = "TowerFloor", menuName = "Rouge TCG/Tower Floor")]
    public class TowerFloorDefinition : ScriptableObject
    {
        [Header("Keeper")]
        [Tooltip("Name des Keepers, wie er im Turm und im Duell steht")]
        public string keeperName = "The Keeper";
        [Tooltip("Kleine Zeile über dem Namen (z.B. 'FLOOR III · THE PACK')")]
        public string eyebrow = "";
        [Tooltip("Kurzbeschreibung im Banner (Taktik/Charakter)")]
        [TextArea] public string blurb = "";
        [Tooltip("Portrait für Dialog + Banner — leer = Artwork der Heldenkarte des Decks")]
        public Sprite portrait;

        [Header("Duell")]
        [Tooltip("Gegner (Deck + Grundwerte) — Modifikatoren unten überschreiben")]
        public BotOpponentDefinition opponent;
        [Tooltip("LP-Override für diese Ebene (0 = Wert des Gegners/Helden)")]
        public int lifePointsOverride;
        [Tooltip("Zusätzliches Mana pro Zug NUR auf dieser Ebene (0 = Wert des Gegners)")]
        [Range(0, 5)] public int bonusManaPerTurn;

        [Header("Erzählung")]
        [Tooltip("Dialog vor dem Duell, Zeile für Zeile durchklickbar")]
        public List<TowerLine> dialog = new List<TowerLine>();
        [Tooltip("Eine Zeile des Keepers nach dem ersten Sieg")]
        [TextArea(2, 4)] public string victoryLine = "";
    }
}
