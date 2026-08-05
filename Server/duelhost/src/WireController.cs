// Controller eines menschlichen Spielers im server-autoritativen Duell: jede
// Entscheidung geht als Request über die Wire-Verbindung an den Client; die
// Coroutine wartet, bis dessen Intent die Antwort setzt. Validiert wird in der
// DuelSession — der Client kann nur wählen, was der Server ihm angeboten hat.

using System.Collections;
using Rouge.Tcg;

namespace Rouge.DuelHost
{
    public class WireController : DuelController
    {
        private readonly DuelSession session;
        public readonly string Side;

        public WireController(DuelSession session, string side)
        {
            this.session = session;
            Side = side;
        }

        private IEnumerator Route(DuelRequest request)
        {
            session.PostRequest(Side, request);
            while (!request.Answered) yield return null;
        }

        public override IEnumerator Decide(StartChoiceRequest request) => Route(request);
        public override IEnumerator Decide(MainActionRequest request) => Route(request);
        public override IEnumerator Decide(BattleActionRequest request) => Route(request);
        public override IEnumerator Decide(YesNoRequest request) => Route(request);
        public override IEnumerator Decide(OptionRequest request) => Route(request);
        public override IEnumerator Decide(TargetRequest request) => Route(request);
        public override IEnumerator Decide(ZoneSelectRequest request) => Route(request);
    }
}
