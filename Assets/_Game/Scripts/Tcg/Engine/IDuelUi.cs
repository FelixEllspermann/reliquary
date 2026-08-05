using System.Collections;

namespace Rouge.Tcg
{
    /// <summary>
    /// Alles, was die Engine von der Duell-UI braucht, um einen Menschen entscheiden
    /// zu lassen. Im Client implementiert Rouge.Tcg.UI.DuelUIController dieses
    /// Interface; auf dem Server spielt kein Mensch — dort bleibt es null.
    /// </summary>
    public interface IDuelUi
    {
        IEnumerator Handle(MainActionRequest request);
        IEnumerator Handle(BattleActionRequest request);
        IEnumerator Handle(YesNoRequest request);
        IEnumerator Handle(OptionRequest request);
        IEnumerator Handle(TargetRequest request);
        IEnumerator Handle(ZoneSelectRequest request);

        /// <summary>Münzwurf gewonnen: First oder Second? (callback true = first)</summary>
        void AskStartChoice(System.Action<bool> callback);
    }
}
