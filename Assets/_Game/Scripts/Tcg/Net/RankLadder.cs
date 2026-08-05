using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>
    /// Die Rangleiter auf Client-Seite: Namen, Farben und Fortschritt zur nächsten
    /// Stufe. Gerechnet wird hier NICHTS, was den Rang verändert — RP und Boden
    /// bestimmt allein der Server. Diese Klasse darf nur darstellen.
    /// </summary>
    public static class RankLadder
    {
        public readonly struct Seal
        {
            public readonly int Rank;      // 1..10
            public readonly int Tier;      // 1..5, 5 ist die höchste
            public readonly string Name;

            public Seal(int rank, int tier, string name)
            {
                Rank = Mathf.Clamp(rank, 1, 10);
                Tier = Mathf.Clamp(tier, 1, 5);
                Name = string.IsNullOrEmpty(name) ? Names[Rank - 1] : name;
            }

            /// <summary>„Gold Seal III" — Stufe als römische Zahl wie im Handoff.</summary>
            public string Label => $"{Name} {Roman[Tier - 1]}";
        }

        public static readonly string[] Names =
        {
            "Ash Seal", "Clay Seal", "Copper Seal", "Iron Seal", "Silver Seal",
            "Gold Seal", "Obsidian Seal", "Amber Seal", "Relic Seal", "Vault Seal"
        };

        private static readonly string[] Roman = { "I", "II", "III", "IV", "V" };

        // Metall je Rang: hell, dunkel, Kante — direkt aus dem Handoff
        private static readonly string[,] Palette =
        {
            { "#6E6A62", "#35322D", "#8A857B" },   // Ash
            { "#A5714A", "#4A2F1C", "#C08A5E" },   // Clay
            { "#C57B45", "#5A3016", "#E09A5C" },   // Copper
            { "#8F9AA5", "#3A424B", "#AEB9C4" },   // Iron
            { "#D6DCE4", "#6E7783", "#F0F4F8" },   // Silver
            { "#F6E4B4", "#8E6A22", "#EBCE8A" },   // Gold
            { "#5A5470", "#16131F", "#8A82A8" },   // Obsidian
            { "#F0A54A", "#7A3D0C", "#FFC978" },   // Amber
            { "#F8EED6", "#A6802F", "#F3DDA4" },   // Relic
            { "#EFE7FA", "#5E4E8C", "#EFE7FA" },   // Vault
        };

        // Bühnenton und Schriftfarbe je Rang — die Aufstiegs-Animation kreuzt
        // während des Bruchs vom alten Ton zum neuen.
        private static readonly string[,] Tones =
        {
            { "#2A2823", "#CFCAC0" },   // Ash
            { "#2E1D10", "#E6C4A6" },   // Clay
            { "#341D0E", "#F2CBA6" },   // Copper
            { "#232A31", "#DCE3EA" },   // Iron
            { "#2A2E33", "#F4F7FA" },   // Silver
            { "#3A2818", "#F5EBD4" },   // Gold
            { "#241F36", "#D6D0EA" },   // Obsidian
            { "#3A2410", "#FFDCA8" },   // Amber
            { "#3E2C16", "#F8EED6" },   // Relic
            { "#2A2148", "#F6F1FE" },   // Vault
        };

        public static Color Light(int rank) => Hex(Palette[Clamp(rank), 0]);
        public static Color Dark(int rank) => Hex(Palette[Clamp(rank), 1]);
        public static Color Edge(int rank) => Hex(Palette[Clamp(rank), 2]);

        /// <summary>Der dunkle Bühnenton, gegen den dieser Rang gezeigt wird.</summary>
        public static Color Stage(int rank) => Hex(Tones[Clamp(rank), 0]);

        /// <summary>Schriftfarbe für den Rangnamen auf diesem Bühnenton.</summary>
        public static Color Text(int rank) => Hex(Tones[Clamp(rank), 1]);

        /// <summary>Schein um das Emblem — erst ab Gold Seal, dann zunehmend.</summary>
        public static float GlowAlpha(int rank)
        {
            switch (Clamp(rank) + 1)
            {
                case 6: case 7: return 0.40f;
                case 8: return 0.45f;
                case 9: return 0.50f;
                case 10: return 0.60f;
                default: return 0f;
            }
        }

        private static int Clamp(int rank) => Mathf.Clamp(rank, 1, 10) - 1;

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }

    /// <summary>Rangstand des eigenen Accounts, wie ihn der Server geschickt hat.</summary>
    public class RankState
    {
        public int Rank = 1;
        public int Tier = 1;
        public string Name = "Ash Seal";
        public int Rp;
        public int TierFloor;
        public int NextAt = -1;      // -1 = keine weitere Stufe (Vault Seal V)
        public string Season = "";
        public int Wins;
        public int Losses;
        public int BestStreak;

        public RankLadder.Seal Seal => new RankLadder.Seal(Rank, Tier, Name);

        /// <summary>Fortschritt innerhalb der aktuellen Unterstufe, 0..1.</summary>
        public float TierProgress
        {
            get
            {
                if (NextAt <= TierFloor) return 1f;
                return Mathf.Clamp01((Rp - TierFloor) / (float)(NextAt - TierFloor));
            }
        }

        /// <summary>„180 RP to Relic Seal I" — oder null an der Spitze.</summary>
        public string NextStepLine
        {
            get
            {
                if (NextAt < 0) return null;
                int missing = Mathf.Max(0, NextAt - Rp);
                var next = NextSeal();
                return $"{missing} RP to {next.Label}";
            }
        }

        private RankLadder.Seal NextSeal()
        {
            if (Tier < 5) return new RankLadder.Seal(Rank, Tier + 1, null);
            return new RankLadder.Seal(Rank + 1, 1, null);
        }

        public int Duels => Wins + Losses;
        public float WinRate => Duels == 0 ? 0f : Wins / (float)Duels;
    }
}
