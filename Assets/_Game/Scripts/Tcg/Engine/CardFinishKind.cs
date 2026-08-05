namespace Rouge.Tcg.Net
{
    /// <summary>
    /// Ein Finish gehört dem EXEMPLAR, nicht der Karte. Dieselbe Karte kann
    /// gleichzeitig schlicht, glänzend und regenbogen im Tresor liegen, und jedes
    /// Exemplar zählt einzeln — im Deck Builder wie in der Sammlung.
    ///
    /// Warum liegt der Aufzählungstyp hier in Engine/ und nicht bei seinen
    /// Helfern in Net/? Weil der DuelHost genau diesen Ordner kompiliert. Die
    /// Engine trägt das Finish von der Deckliste bis auf den Tisch — rendern tut
    /// sie nichts, aber ohne den Typ könnte sie es nicht einmal weiterreichen.
    /// Die Anzeige-Helfer (Farben, Kürzel, Bestand) bleiben in Net/CardFinish.cs,
    /// sie brauchen Unity.
    /// </summary>
    public enum CardFinish { Plain = 0, Glossy = 1, Rainbow = 2, Static = 3 }

    /// <summary>
    /// Über die Leitung ist ein Finish nur eine Zahl. Hier wird daraus wieder
    /// eine Ausführung — und alles Unbekannte wird schlicht statt ungültig: ein
    /// Client mit einer neueren Fassung soll eine unbekannte Zahl nicht in eine
    /// kaputte Karte verwandeln.
    /// </summary>
    public static class CardFinishWire
    {
        public const int Count = 4;

        public static CardFinish From(int value) =>
            value >= 0 && value < Count ? (CardFinish)value : CardFinish.Plain;
    }
}
