using System.Collections;
using System.Linq;

namespace Rouge.Tcg
{
    /// <summary>Leitet alle Entscheidungen an die Duell-UI weiter. Ohne UI antwortet er passiv (Failsafe).</summary>
    public class HumanDuelController : DuelController
    {
        private readonly IDuelUi ui;

        public HumanDuelController(IDuelUi ui)
        {
            this.ui = ui;
        }

        public override IEnumerator Decide(StartChoiceRequest request)
        {
            if (ui == null) { request.Result = true; request.Answered = true; yield break; }
            bool answered = false;
            ui.AskStartChoice(first => { request.Result = first; answered = true; });
            while (!answered) yield return null;
            request.Answered = true;
        }

        public override IEnumerator Decide(MainActionRequest request)
        {
            if (ui == null) { request.Chosen = request.Options.FindIndex(o => o.Kind == MainActionKind.EndTurn); request.Answered = true; yield break; }
            yield return ui.Handle(request);
        }

        public override IEnumerator Decide(BattleActionRequest request)
        {
            if (ui == null) { request.Chosen = request.Options.FindIndex(o => o.EndBattle); request.Answered = true; yield break; }
            yield return ui.Handle(request);
        }

        public override IEnumerator Decide(YesNoRequest request)
        {
            if (ui == null) { request.Result = false; request.Answered = true; yield break; }
            yield return ui.Handle(request);
        }

        public override IEnumerator Decide(OptionRequest request)
        {
            if (ui == null) { request.Result = request.AllowCancel ? -1 : (request.Options.Count > 0 ? 0 : -1); request.Answered = true; yield break; }
            yield return ui.Handle(request);
        }

        public override IEnumerator Decide(TargetRequest request)
        {
            if (ui == null)
            {
                if (request.AllowCancel) request.Cancelled = true;
                else request.Result.AddRange(request.Candidates.Take(request.Count));
                request.Answered = true;
                yield break;
            }
            yield return ui.Handle(request);
        }

        public override IEnumerator Decide(ZoneSelectRequest request)
        {
            if (ui == null)
            {
                request.Result = request.FreeIndices.Count > 0 ? request.FreeIndices[0] : -1;
                request.Answered = true;
                yield break;
            }
            yield return ui.Handle(request);
        }
    }
}
