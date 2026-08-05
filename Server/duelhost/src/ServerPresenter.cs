// IDuelPresenter des Servers: statt zu animieren, zeichnet er semantische Ereignisse
// auf. Die DuelSession serialisiert sie pro Spieler-Sicht (verdeckte Informationen
// werden dort maskiert) und der Client spielt sie mit seinem echten Presenter ab.

using System.Collections;
using System.Collections.Generic;
using Rouge.Tcg;

namespace Rouge.DuelHost
{
    public class DuelEvent
    {
        public string Type;
        public CardInstance Card;
        public CardInstance Target;
        public PlayerState Player;
        public string Text;
        public bool Direct;
        /// <summary>Zonenindex — die Reliquary-Beschwörung braucht ihn als Ziel.</summary>
        public int Zone = -1;
    }

    public class ServerPresenter : IDuelPresenter
    {
        public readonly List<DuelEvent> Pending = new List<DuelEvent>();

        private IEnumerator Record(DuelEvent evt)
        {
            Pending.Add(evt);
            yield break;
        }

        public void RememberView(CardInstance card) { }
        public void RememberOrigin(CardInstance card) { }

        public IEnumerator ShowCardMoved(CardInstance card) => Record(new DuelEvent { Type = "moved", Card = card });
        public IEnumerator ShowPhaseBanner(string text, float holdOverride = -1f) => Record(new DuelEvent { Type = "banner", Text = text });
        public IEnumerator ShowCoinToss(PlayerState winner) => Record(new DuelEvent { Type = "cointoss", Player = winner });
        public IEnumerator ShowCardDrawn(PlayerState player, CardInstance card, float speed = 1f) => Record(new DuelEvent { Type = "draw", Player = player, Card = card });
        public IEnumerator ShowHandShuffle(PlayerState player) => Record(new DuelEvent { Type = "shuffle", Player = player });
        public IEnumerator ShowSummon(CardInstance monster) => Record(new DuelEvent { Type = "summon", Card = monster });
        public IEnumerator ShowReliquarySummon(CardInstance monster, PlayerState owner, int zoneIndex) =>
            Record(new DuelEvent { Type = "reliquarysummon", Card = monster, Player = owner, Zone = zoneIndex });
        public IEnumerator ShowPositionSwitch(CardInstance card) => Record(new DuelEvent { Type = "position", Card = card });
        public IEnumerator ShowCardActivation(CardInstance card, EffectDefinition effect) => Record(new DuelEvent { Type = "activation", Card = card, Text = effect != null ? effect.label : "" });
        public IEnumerator ShowActivationPulse(CardInstance card, bool spin) => Record(new DuelEvent { Type = "pulse", Card = card });
        public IEnumerator ShowTargetsFlash(List<CardInstance> targets) => Record(new DuelEvent { Type = "targets", Card = targets != null && targets.Count > 0 ? targets[0] : null });
        public IEnumerator ShowAttackDeclared(CardInstance attacker, CardInstance target, bool direct) => Record(new DuelEvent { Type = "attack", Card = attacker, Target = target, Direct = direct });
        public IEnumerator ShowAttackImpact(CardInstance attacker, CardInstance target, bool direct) => Record(new DuelEvent { Type = "impact", Card = attacker, Target = target, Direct = direct });
        public IEnumerator ShowCardDestroyed(CardInstance card) => Record(new DuelEvent { Type = "destroyed", Card = card });
        public IEnumerator ShowCardSentToGrave(CardInstance card) => Record(new DuelEvent { Type = "tograve", Card = card });
        public IEnumerator ShowSpellToGrave(CardInstance spell) => Record(new DuelEvent { Type = "spelltograve", Card = spell });
        public IEnumerator ShowCardBanished(CardInstance card) => Record(new DuelEvent { Type = "banished", Card = card });
    }
}
