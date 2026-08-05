using System.Collections;

namespace Rouge.Tcg
{
    /// <summary>
    /// Entscheidungs-Schnittstelle eines Spielers. Die Engine stellt Anfragen (Requests),
    /// der Controller beantwortet sie — ein Mensch über die UI, ein Bot per Heuristik.
    /// </summary>
    public abstract class DuelController
    {
        public PlayerState Player;
        public DuelManager Duel;

        /// <summary>Münzwurf gewonnen: First/Second wählen. Standard: der Gewinner beginnt.</summary>
        public virtual IEnumerator Decide(StartChoiceRequest request)
        {
            request.Result = true;
            request.Answered = true;
            yield break;
        }

        public abstract IEnumerator Decide(MainActionRequest request);
        public abstract IEnumerator Decide(BattleActionRequest request);
        public abstract IEnumerator Decide(YesNoRequest request);
        public abstract IEnumerator Decide(OptionRequest request);
        public abstract IEnumerator Decide(TargetRequest request);
        public abstract IEnumerator Decide(ZoneSelectRequest request);
    }
}
