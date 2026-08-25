using System;

namespace Rouge.Tcg
{
    /// <summary>
    /// Sichtbare Karten-Status als Bitmaske — die EINE Wahrheit für Server-Wire
    /// und Client-Badges. Der DuelHost packt sie in SduelCard.status, der
    /// DuelMirror entpackt sie zurück in die CardInstance-Felder; lokale Duelle
    /// (Solo) lesen die Felder direkt. Reihenfolge = Anzeige-Reihenfolge der
    /// Badge-Spalte (Design-Handoff "Card Status Icons", Roster 1-16; Death
    /// Counter und Lien laufen weiter über ihre Zahlenfelder).
    /// </summary>
    [Flags]
    public enum CardStatusFlags
    {
        None             = 0,
        Indestructible   = 1 << 0,   // CannotBeDestroyedThisTurn
        Immune           = 1 << 1,   // ImmuneToOpponentThisTurn
        Untargetable     = 1 << 2,   // CannotBeTargetedThisTurn
        Negated          = 1 << 3,   // EffectsNegated
        CannotAttack     = 1 << 4,   // CannotAttackThisTurn
        PositionLocked   = 1 << 5,   // PositionLockedThisTurn
        Taunt            = 1 << 6,   // MustBeAttackedThisTurn
        Piercing         = 1 << 7,   // PiercingThisTurn (nur der Zug-Status)
        MultiAttack      = 1 << 8,   // BonusAttacks > 0 (Zahl separat im Wire)
        BanishOnLeave    = 1 << 9,   // BanishWhenLeavingField (gewildert)
        TempCopy         = 1 << 10,  // IsTemporaryCopy (Mirror Hour)
        Stolen           = 1 << 11,  // Owner != OriginalOwner
        EndphaseDoom     = 1 << 12,  // TempReliquaryUntilEndPhase
        SpecialSummoned  = 1 << 13,  // WasSpecialSummoned
    }

    public static class CardStatus
    {
        /// <summary>Maske aus dem Laufzeit-Zustand einer Karte (Server-Seite).</summary>
        public static int MaskOf(CardInstance card)
        {
            if (card == null) return 0;
            var flags = CardStatusFlags.None;
            if (card.CannotBeDestroyedThisTurn) flags |= CardStatusFlags.Indestructible;
            if (card.ImmuneToOpponentThisTurn) flags |= CardStatusFlags.Immune;
            if (card.CannotBeTargetedThisTurn) flags |= CardStatusFlags.Untargetable;
            if (card.EffectsNegated) flags |= CardStatusFlags.Negated;
            if (card.CannotAttackThisTurn) flags |= CardStatusFlags.CannotAttack;
            if (card.PositionLockedThisTurn) flags |= CardStatusFlags.PositionLocked;
            if (card.MustBeAttackedThisTurn) flags |= CardStatusFlags.Taunt;
            if (card.PiercingThisTurn) flags |= CardStatusFlags.Piercing;
            if (card.BonusAttacks > 0) flags |= CardStatusFlags.MultiAttack;
            if (card.BanishWhenLeavingField) flags |= CardStatusFlags.BanishOnLeave;
            if (card.IsTemporaryCopy) flags |= CardStatusFlags.TempCopy;
            if (card.Owner != null && card.OriginalOwner != null && card.Owner != card.OriginalOwner)
                flags |= CardStatusFlags.Stolen;
            if (card.TempReliquaryUntilEndPhase) flags |= CardStatusFlags.EndphaseDoom;
            if (card.WasSpecialSummoned) flags |= CardStatusFlags.SpecialSummoned;
            return (int)flags;
        }

        /// <summary>
        /// Maske zurück in die Spiegel-Instanz (Client-Seite). Owner-Wechsel
        /// bildet der Mirror über die Seitenzuordnung ab — Stolen wird deshalb
        /// hier NICHT zurückgeschrieben, sondern nur für die Anzeige gelesen.
        /// </summary>
        public static void Apply(CardInstance card, int mask, int bonusAttacks)
        {
            if (card == null) return;
            var flags = (CardStatusFlags)mask;
            card.CannotBeDestroyedThisTurn = (flags & CardStatusFlags.Indestructible) != 0;
            card.ImmuneToOpponentThisTurn = (flags & CardStatusFlags.Immune) != 0;
            card.CannotBeTargetedThisTurn = (flags & CardStatusFlags.Untargetable) != 0;
            card.EffectsNegated = (flags & CardStatusFlags.Negated) != 0;
            card.CannotAttackThisTurn = (flags & CardStatusFlags.CannotAttack) != 0;
            card.PositionLockedThisTurn = (flags & CardStatusFlags.PositionLocked) != 0;
            card.MustBeAttackedThisTurn = (flags & CardStatusFlags.Taunt) != 0;
            card.PiercingThisTurn = (flags & CardStatusFlags.Piercing) != 0;
            card.BonusAttacks = bonusAttacks;
            card.BanishWhenLeavingField = (flags & CardStatusFlags.BanishOnLeave) != 0;
            card.IsTemporaryCopy = (flags & CardStatusFlags.TempCopy) != 0;
            card.TempReliquaryUntilEndPhase = (flags & CardStatusFlags.EndphaseDoom) != 0;
            card.WasSpecialSummoned = (flags & CardStatusFlags.SpecialSummoned) != 0;
            card.MirroredStatusMask = mask;
        }

        /// <summary>
        /// Maske für die ANZEIGE: lokal berechnet, vereint mit der Server-Maske.
        /// Lokale Duelle liefern MaskOf, Server-Duelle zusätzlich Stolen & Co.
        /// </summary>
        public static int DisplayMask(CardInstance card)
            => card == null ? 0 : MaskOf(card) | card.MirroredStatusMask;
    }
}
