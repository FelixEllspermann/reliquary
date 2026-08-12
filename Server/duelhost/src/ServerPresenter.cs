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
        /// <summary>Zahl ohne eigene Bedeutung — bisher nur die Nummer eines Kettenglieds.</summary>
        public int Amount;

        // Effekt-Anzeige beim Aktivierungs-Puls: der Client zeigt unter der
        // gehobenen Karte, was der Effekt macht. Text (oben) trägt das Label.
        public string EffectText;
        public int EffectCost;
        public int EffectInfused; // 0 = nein, 1 = Standalone, 2 = Coupled
    }

    public class ServerPresenter : IDuelPresenter
    {
        public readonly List<DuelEvent> Pending = new List<DuelEvent>();

        private IEnumerator Record(DuelEvent evt)
        {
            Pending.Add(evt);
            yield break;
        }

        // Diese beiden merken sich im Client, WO eine Karte gerade liegt — der Flug
        // danach geht von dort aus. Auf dem Server gibt es keine Position, also wird
        // nur der Zeitpunkt aufgezeichnet; der Client schaut dann auf seinem Brett
        // nach. Fehlen sie, hat ShowCardMoved keinen Startpunkt und tut gar nichts:
        // genau deshalb sind Karten im Online-Duell von der Hand aufs Feld gesprungen.
        public void RememberView(CardInstance card) => Pending.Add(new DuelEvent { Type = "remember", Card = card });
        public void RememberOrigin(CardInstance card) => Pending.Add(new DuelEvent { Type = "rememberorigin", Card = card });

        public IEnumerator ShowCardMoved(CardInstance card) => Record(new DuelEvent { Type = "moved", Card = card });
        public IEnumerator ShowPhaseBanner(string text, float holdOverride = -1f) => Record(new DuelEvent { Type = "banner", Text = text });
        public IEnumerator ShowCoinToss(PlayerState winner) => Record(new DuelEvent { Type = "cointoss", Player = winner });
        public IEnumerator ShowCardDrawn(PlayerState player, CardInstance card, float speed = 1f) => Record(new DuelEvent { Type = "draw", Player = player, Card = card });
        public IEnumerator ShowHandShuffle(PlayerState player) => Record(new DuelEvent { Type = "shuffle", Player = player });
        public IEnumerator ShowSummon(CardInstance monster) => Record(new DuelEvent { Type = "summon", Card = monster });
        public IEnumerator ShowReliquarySummon(CardInstance monster, PlayerState owner, int zoneIndex) =>
            Record(new DuelEvent { Type = "reliquarysummon", Card = monster, Player = owner, Zone = zoneIndex });
        public IEnumerator ShowPositionSwitch(CardInstance card) => Record(new DuelEvent { Type = "position", Card = card });
        public IEnumerator ShowMilled(PlayerState player, CardInstance card) => Record(new DuelEvent { Type = "milled", Card = card, Player = player });
        public IEnumerator ShowCardRevealed(CardInstance card, string label) => Record(new DuelEvent { Type = "reveal", Card = card, Text = label });
        public IEnumerator ShowCardActivation(CardInstance card, EffectDefinition effect) => Record(new DuelEvent { Type = "activation", Card = card, Text = effect != null ? effect.label : "" });
        public IEnumerator ShowActivationPulse(CardInstance card, bool spin, EffectDefinition effect = null) => Record(new DuelEvent
        {
            Type = "pulse",
            Card = card,
            Text = effect != null ? effect.label : null,
            EffectText = effect != null ? effect.text : null,
            EffectCost = effect != null ? effect.manaCost : 0,
            EffectInfused = effect == null || !effect.isInfused ? 0 : (effect.infusedKind == InfusedKind.Coupled ? 2 : 1)
        });

        // Kettenanzeige. Der Server rechnet, der Client zeigt — ohne diese drei
        // saehe ein Online-Spieler von der Kette genau nichts.
        public IEnumerator ShowChainLink(CardInstance card, string label, PlayerState owner, int link) =>
            Record(new DuelEvent { Type = "chainlink", Card = card, Text = label, Player = owner, Amount = link });
        public IEnumerator ShowChainResolve(CardInstance card, int link) =>
            Record(new DuelEvent { Type = "chainresolve", Card = card, Amount = link });
        public IEnumerator ShowChainEnd() => Record(new DuelEvent { Type = "chainend" });
        public IEnumerator ShowTargetsFlash(List<CardInstance> targets) => Record(new DuelEvent { Type = "targets", Card = targets != null && targets.Count > 0 ? targets[0] : null });
        public IEnumerator ShowAttackDeclared(CardInstance attacker, CardInstance target, bool direct) => Record(new DuelEvent { Type = "attack", Card = attacker, Target = target, Direct = direct });
        public IEnumerator ShowAttackImpact(CardInstance attacker, CardInstance target, bool direct) => Record(new DuelEvent { Type = "impact", Card = attacker, Target = target, Direct = direct });
        public IEnumerator ShowCardDestroyed(CardInstance card) => Record(new DuelEvent { Type = "destroyed", Card = card });
        public IEnumerator ShowCardSentToGrave(CardInstance card) => Record(new DuelEvent { Type = "tograve", Card = card });
        public IEnumerator ShowSpellToGrave(CardInstance spell) => Record(new DuelEvent { Type = "spelltograve", Card = spell });
        public IEnumerator ShowCardBanished(CardInstance card) => Record(new DuelEvent { Type = "banished", Card = card });
    }
}
