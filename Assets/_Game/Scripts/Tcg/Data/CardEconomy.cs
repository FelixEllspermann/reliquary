namespace Rouge.Tcg
{
    /// <summary>
    /// Client-seitige Anzeige-Werte der Karten-Ökonomie. MUSS mit Server/server.js ECON
    /// übereinstimmen (craftCost/dustGain pro Rarity) — Quelle der Wahrheit bleibt der Server.
    /// </summary>
    public static class CardEconomy
    {
        /// <summary>Entcraften gibt immer 10 Dust der jeweiligen Rarity.</summary>
        public static int DustGain(CardRarity rarity) => 10;

        /// <summary>Craften kostet immer 30 Dust der jeweiligen Rarity.</summary>
        public static int CraftCost(CardRarity rarity) => 30;
    }
}
