namespace Rouge.Tcg
{
    /// <summary>
    /// Engine-eigene Warteanweisung. Die Engine yieldet nie Unity-Typen — der
    /// jeweilige Host übersetzt: der DuelHost in WaitForSeconds, ein Server-Host
    /// in seinen eigenen Timer (oder überspringt die Wartezeit komplett).
    /// </summary>
    public sealed class DuelWait
    {
        public readonly float Seconds;
        private DuelWait(float seconds) { Seconds = seconds; }
        public static DuelWait For(float seconds) => new DuelWait(seconds);
    }
}
