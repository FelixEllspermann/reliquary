using System.Collections;
using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>
    /// Alles, was die Duell-Engine von der Präsentation braucht — als Interface,
    /// damit die Engine keinen UI-Code kennt und später auch headless (auf dem
    /// Server) laufen kann. Im Unity-Client implementiert Rouge.Tcg.UI.DuelPresenter
    /// dieses Interface; auf dem Server gibt es schlicht keinen Presenter (null).
    ///
    /// Positions-Merken: Die Engine kennt keine Bildschirmkoordinaten. Vor einer
    /// Datenänderung ruft sie RememberView/RememberOrigin auf; der Presenter merkt
    /// sich die Position der Karte intern. ShowCardMoved fliegt anschliessend von
    /// dort zur neuen View — oder tut nichts, wenn nichts gemerkt wurde.
    /// </summary>
    public interface IDuelPresenter
    {
        /// <summary>Merkt die aktuelle View-Position (nur wenn die Karte sichtbar ist).</summary>
        void RememberView(CardInstance card);

        /// <summary>Wie RememberView, fällt aber auf den Stapel-Anker der Zone zurück.</summary>
        void RememberOrigin(CardInstance card);

        /// <summary>Flug von der gemerkten Position zur neuen View; ohne Merkeintrag ein No-op.</summary>
        IEnumerator ShowCardMoved(CardInstance card);

        IEnumerator ShowPhaseBanner(string text, float holdOverride = -1f);
        /// <summary>Der Münzwurf. <paramref name="winner"/> entscheidet, auf welche Seite sie fällt.</summary>
        IEnumerator ShowCoinToss(PlayerState winner);
        IEnumerator ShowCardDrawn(PlayerState player, CardInstance card, float speed = 1f);
        IEnumerator ShowHandShuffle(PlayerState player);
        IEnumerator ShowSummon(CardInstance monster);

        /// <summary>
        /// Beschwörung aus dem Extra Deck. Eigener Auftritt, weil ein Reliquary
        /// nicht aus der Hand kommt, sondern aus dem Tresor — die Beschwörung IST
        /// das Öffnen (Handoff „Animations", Abschnitt 6).
        /// </summary>
        IEnumerator ShowReliquarySummon(CardInstance monster, PlayerState owner, int zoneIndex);
        IEnumerator ShowPositionSwitch(CardInstance card);
        IEnumerator ShowCardActivation(CardInstance card, EffectDefinition effect);
        IEnumerator ShowActivationPulse(CardInstance card, bool spin);
        IEnumerator ShowTargetsFlash(List<CardInstance> targets);
        IEnumerator ShowAttackDeclared(CardInstance attacker, CardInstance target, bool direct);
        IEnumerator ShowAttackImpact(CardInstance attacker, CardInstance target, bool direct);
        IEnumerator ShowCardDestroyed(CardInstance card);
        IEnumerator ShowCardSentToGrave(CardInstance card);
        IEnumerator ShowSpellToGrave(CardInstance spell);
        IEnumerator ShowCardBanished(CardInstance card);
    }
}
