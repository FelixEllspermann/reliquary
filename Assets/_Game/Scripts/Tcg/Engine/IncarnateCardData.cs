using UnityEngine;

namespace Rouge.Tcg
{
    /// <summary>
    /// Incarnate-Karte (Extra Deck, ROTER Rahmen): die zweite Extra-Deck-Kartenart
    /// neben den Reliquaries — eingeführt 29.08.2026.
    ///
    /// Zwei Wege aufs Feld:
    /// 1. OPFERGABE (temporär): Monster opfern, deren Level-Summe EXAKT dem
    ///    Incarnate-Level entspricht — mindestens eines davon muss ein VESSEL
    ///    sein (MonsterCardData.isVessel). Kein Mana. In der Standby Phase des
    ///    NÄCHSTEN eigenen Zuges kehrt das Incarnate ins Extra Deck zurück
    ///    (sofern es noch auf dem Feld liegt).
    /// 2. RITE (permanent): Eine Riten-Zauberkarte (SpellCardData.isRite) opfert
    ///    das auf ihr benannte Monster und beschwört das benannte Incarnate —
    ///    ohne Rückkehr.
    ///
    /// Verlässt ein Incarnate das Feld auf andere Weise, gilt die Reliquary-Regel:
    /// Zerstörung/Verbannung laufen normal, Hand-Rückgaben leiten ins Extra Deck.
    /// </summary>
    [CreateAssetMenu(fileName = "NeuesIncarnate", menuName = "Rouge TCG/Incarnate Card")]
    public class IncarnateCardData : MonsterCardData
    {
        /// <summary>Blutroter Incarnate-Rahmen.</summary>
        public override Color FrameColor => new Color(0.72f, 0.16f, 0.14f);

        [Header("Incarnate — Beschwörung aus dem Extra Deck")]
        [Tooltip("Die Opfergabe muss EXAKT diese Level-Summe erreichen (Monster-Level sind 1-3; " +
                 "ein Level-6-Incarnate braucht z.B. 3+3 oder 2+2+1+1).")]
        [Range(2, 9)] public int incarnateLevel = 4;

        /// <summary>
        /// Beschwörungsregel + Vergänglichkeit als generierte Kartenzeilen — die
        /// UI zeigt BuildPassiveLines, keine eigene Anzeige-Stelle nötig.
        /// </summary>
        public override System.Collections.Generic.List<string> BuildPassiveLines()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                Loc.F("OFFERING: Sacrifice monsters whose Levels total exactly {0} — at least one must be a VESSEL. Summoned this way, it returns to the Extra Deck during your next Standby Phase.", incarnateLevel)
            };
            lines.AddRange(base.BuildPassiveLines());
            return lines;
        }
    }
}
