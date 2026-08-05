using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>
    /// Ein Finish gehört dem EXEMPLAR, nicht der Karte. Dieselbe Karte kann
    /// gleichzeitig schlicht, glänzend und regenbogen im Tresor liegen, und jedes
    /// Exemplar zählt einzeln — im Deck Builder wie in der Sammlung.
    /// </summary>
    public enum CardFinish { Plain = 0, Glossy = 1, Rainbow = 2, Static = 3 }

    public static class CardFinishInfo
    {
        public const int Count = 4;

        private static readonly string[] Labels = { "Plain", "Glossy", "Rainbow", "Static" };

        /// <summary>Ziehungsrate als Klartext — für Shop und Tooltips.</summary>
        private static readonly string[] Odds = { "", "1 in 12", "1 in 60", "1 in 240" };

        // Akzentfarbe je Finish. Schlicht bekommt keine.
        private static readonly string[] Accents = { "#A2917A", "#F8EED6", "#8FD2A8", "#C8DCEB" };

        public static string Label(CardFinish finish) => Labels[Index(finish)];
        public static string OddsText(CardFinish finish) => Odds[Index(finish)];

        public static Color Accent(CardFinish finish)
        {
            ColorUtility.TryParseHtmlString(Accents[Index(finish)], out var color);
            return color;
        }

        /// <summary>
        /// Kurzzeichen für enge Stellen: ● glänzend, ◆ regenbogen, ■ static —
        /// je seltener, desto kantiger. Alle drei liegen in „Symbols SDF"; Zeichen
        /// ausserhalb dieser Schrift würden als leeres Kästchen erscheinen.
        /// </summary>
        public static string Glyph(CardFinish finish)
        {
            switch (finish)
            {
                case CardFinish.Glossy: return "●";
                case CardFinish.Rainbow: return "◆";
                case CardFinish.Static: return "■";
                default: return "";
            }
        }

        private static int Index(CardFinish finish) => Mathf.Clamp((int)finish, 0, Count - 1);
    }

    /// <summary>Wie viele Exemplare einer Karte man je Finish besitzt.</summary>
    public class CardStock
    {
        private readonly int[] counts = new int[CardFinishInfo.Count];

        public int this[CardFinish finish]
        {
            get => counts[Mathf.Clamp((int)finish, 0, CardFinishInfo.Count - 1)];
            set => counts[Mathf.Clamp((int)finish, 0, CardFinishInfo.Count - 1)] = Mathf.Max(0, value);
        }

        public int Total
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < counts.Length; i++) sum += counts[i];
                return sum;
            }
        }

        /// <summary>Besitzt der Spieler überhaupt ein Exemplar mit Finish?</summary>
        public bool HasAnySpecial =>
            counts[(int)CardFinish.Glossy] + counts[(int)CardFinish.Rainbow] + counts[(int)CardFinish.Static] > 0;
    }
}
