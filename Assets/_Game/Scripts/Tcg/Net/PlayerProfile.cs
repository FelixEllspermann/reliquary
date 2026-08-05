using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg.Net
{
    /// <summary>Ein Account-Deck als Laufzeit-Daten (Kartennamen; Auflösung über den CardCatalog).</summary>
    [System.Serializable]
    public class RuntimeDeck
    {
        public string Name = "Neues Deck";
        public string Hero = "";
        public List<string> Cards = new List<string>();
        public List<string> Extra = new List<string>();  // Extra Deck (Reliquarys)

        // Finish je Exemplar, gleiche Reihenfolge wie Cards/Extra. Leer = alles schlicht.
        public List<CardFinish> CardFinishes = new List<CardFinish>();
        public List<CardFinish> ExtraFinishes = new List<CardFinish>();

        /// <summary>Finish des Exemplars an dieser Stelle (schlicht, wenn nichts hinterlegt).</summary>
        public CardFinish FinishAt(int index) =>
            index >= 0 && index < CardFinishes.Count ? CardFinishes[index] : CardFinish.Plain;

        public CardFinish ExtraFinishAt(int index) =>
            index >= 0 && index < ExtraFinishes.Count ? ExtraFinishes[index] : CardFinish.Plain;

        public RuntimeDeck Clone()
        {
            return new RuntimeDeck { Name = Name, Hero = Hero, Cards = new List<string>(Cards), Extra = new List<string>(Extra) };
        }
    }

    /// <summary>Das eingeloggte Konto: Sammlung, Tokens, Coins, Packs, Decks (Quelle: Server).</summary>
    public static class PlayerProfile
    {
        public static bool LoggedIn;
        public static string AccountName = "";
        public static int Coins;
        public static int TokensCommon, TokensUncommon, TokensRare, TokensLegendary;
        public static int DailyStreak;
        public static bool DailyClaimable;
        public static long DailyNextInMs;
        public static int DailyRewardCoins = 150;
        public static int OnlineCount;
        public static System.DateTime ProfileReceivedAt; // für Client-seitigen Countdown
        /// <summary>Gesamtzahl je Karte, über alle Finishes.</summary>
        public static readonly Dictionary<string, int> Collection = new Dictionary<string, int>();

        /// <summary>Aufschlüsselung je Karte nach Finish — für Deck Builder und Sammlung.</summary>
        public static readonly Dictionary<string, CardStock> Stock = new Dictionary<string, CardStock>();
        public static readonly Dictionary<string, int> PackInventory = new Dictionary<string, int>();
        public static readonly List<RuntimeDeck> Decks = new List<RuntimeDeck>();

        /// <summary>Rangstand dieser Saison. Wird ausschliesslich vom Server gesetzt.</summary>
        public static readonly RankState Rank = new RankState();

        /// <summary>Ein Aufstieg, der noch gezeigt werden muss.</summary>
        public class RankUp
        {
            public int From;        // Hauptrang davor
            public int Into;        // Hauptrang danach
            public int Gain;        // gewonnene RP
            public string Opponent;
        }

        /// <summary>
        /// Der Aufstieg wartet, bis der Ergebnis-Bildschirm durch ist — sonst
        /// liefen zwei Bildschirme gegeneinander. Wer ihn zeigt, holt ihn mit
        /// <see cref="TakeRankUp"/> ab; damit ist er verbraucht.
        /// </summary>
        private static RankUp pendingRankUp;

        public static bool HasRankUp => pendingRankUp != null;

        public static void QueueRankUp(RankUp rankUp) => pendingRankUp = rankUp;

        /// <summary>
        /// Die RP-Änderung des letzten Duells. Sie kommt mit `rank_change` an,
        /// gebraucht wird sie erst auf dem Niederlage-Bildschirm — dazwischen
        /// liegen ein paar Sekunden.
        /// </summary>
        private static int pendingRpDelta;

        public static void QueueRpDelta(int delta) => pendingRpDelta = delta;

        public static int TakeRpDelta()
        {
            int delta = pendingRpDelta;
            pendingRpDelta = 0;
            return delta;
        }

        public static RankUp TakeRankUp()
        {
            var rankUp = pendingRankUp;
            pendingRankUp = null;
            return rankUp;
        }

        /// <summary>Freigeschaltete Profiltitel (Schlüssel, nicht Anzeigetext).</summary>
        public static readonly List<string> Titles = new List<string>();

        /// <summary>Kartenname -> erlaubte Kopien (0 gebannt, 1 limitiert, 2 semi-limitiert).</summary>
        public static readonly Dictionary<string, int> Banlist = new Dictionary<string, int>();

        /// <summary>Normales Kopienlimit, wenn eine Karte nicht auf der Banlist steht.</summary>
        public static int BanlistMaxCopies = 3;

        /// <summary>Eine Banlist-Änderung: Karte, neues und altes Limit (-1 = unbekannt).</summary>
        public class BanlistChange
        {
            public string Card;
            public int To;
            public int From = -1;
        }

        /// <summary>Ein datierter Banlist-Stand mit seinen Änderungen.</summary>
        public class BanlistRevision
        {
            public string Date = "";
            public string Title = "";
            public string Note = "";
            public readonly List<BanlistChange> Changes = new List<BanlistChange>();
        }

        /// <summary>Chronik der Banlist, älteste zuerst (Reihenfolge wie in der Datei).</summary>
        public static readonly List<BanlistRevision> BanlistHistory = new List<BanlistRevision>();

        public static void Apply(NetProfile profile)
        {
            if (profile == null) return;
            LoggedIn = true;
            AccountName = profile.account ?? "";
            Coins = profile.coins;
            TokensCommon = profile.tokensCommon;
            TokensUncommon = profile.tokensUncommon;
            TokensRare = profile.tokensRare;
            TokensLegendary = profile.tokensLegendary;
            DailyStreak = profile.dailyStreak;
            DailyClaimable = profile.dailyClaimable;
            DailyNextInMs = profile.dailyNextInMs;
            if (profile.dailyRewardCoins > 0) DailyRewardCoins = profile.dailyRewardCoins;
            if (profile.online > 0) OnlineCount = profile.online;
            ProfileReceivedAt = System.DateTime.UtcNow;

            if (profile.rankValue > 0)
            {
                Rank.Rank = profile.rankValue;
                Rank.Tier = profile.rankTier;
                Rank.Name = string.IsNullOrEmpty(profile.rankName)
                    ? RankLadder.Names[Mathf.Clamp(profile.rankValue, 1, 10) - 1]
                    : profile.rankName;
                Rank.Rp = profile.rankRp;
                Rank.TierFloor = profile.rankTierFloor;
                Rank.NextAt = profile.rankNextAt;
                Rank.Season = profile.rankSeason ?? "";
                Rank.Wins = profile.rankWins;
                Rank.Losses = profile.rankLosses;
                Rank.BestStreak = profile.rankBestStreak;
            }
            Titles.Clear();
            if (profile.titles != null) Titles.AddRange(profile.titles);
            Cosmetics.Apply(profile);

            Banlist.Clear();
            if (profile.banlistNames != null && profile.banlistLimits != null)
                for (int i = 0; i < profile.banlistNames.Length && i < profile.banlistLimits.Length; i++)
                    Banlist[profile.banlistNames[i]] = profile.banlistLimits[i];
            if (profile.banlistMaxCopies > 0) BanlistMaxCopies = profile.banlistMaxCopies;

            BanlistHistory.Clear();
            if (profile.historyDates != null)
                for (int i = 0; i < profile.historyDates.Length; i++)
                {
                    var revision = new BanlistRevision
                    {
                        Date = At(profile.historyDates, i),
                        Title = At(profile.historyTitles, i),
                        Note = At(profile.historyNotes, i)
                    };
                    foreach (var line in At(profile.historyChanges, i)
                                 .Split('\n', System.StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = line.Split('|');
                        if (parts.Length < 3) continue;
                        revision.Changes.Add(new BanlistChange
                        {
                            To = ParseInt(parts[0], BanlistMaxCopies),
                            From = ParseInt(parts[1], -1),
                            Card = parts[2]
                        });
                    }
                    BanlistHistory.Add(revision);
                }

            // Collection bleibt die Gesamtzahl je Karte; Stock schlüsselt nach Finish auf
            Collection.Clear();
            Stock.Clear();
            if (profile.collectionCards != null && profile.collectionCounts != null)
                for (int i = 0; i < profile.collectionCards.Length && i < profile.collectionCounts.Length; i++)
                {
                    string card = profile.collectionCards[i];
                    Collection[card] = profile.collectionCounts[i];

                    var stock = new CardStock();
                    stock[CardFinish.Plain] = At(profile.collectionPlain, i, profile.collectionCounts[i]);
                    stock[CardFinish.Glossy] = At(profile.collectionGlossy, i, 0);
                    stock[CardFinish.Rainbow] = At(profile.collectionRainbow, i, 0);
                    stock[CardFinish.Static] = At(profile.collectionStatic, i, 0);
                    Stock[card] = stock;
                }

            PackInventory.Clear();
            if (profile.packNames != null && profile.packCounts != null)
                for (int i = 0; i < profile.packNames.Length && i < profile.packCounts.Length; i++)
                    PackInventory[profile.packNames[i]] = profile.packCounts[i];

            Decks.Clear();
            if (profile.decks != null)
                foreach (var deck in profile.decks)
                {
                    if (deck == null) continue;
                    var runtime = new RuntimeDeck { Name = deck.name ?? "Deck", Hero = deck.hero ?? "" };
                    if (deck.cards != null) runtime.Cards.AddRange(deck.cards);
                    if (deck.extra != null) runtime.Extra.AddRange(deck.extra);
                    FillFinishes(runtime.CardFinishes, deck.cardFinishes, runtime.Cards.Count);
                    FillFinishes(runtime.ExtraFinishes, deck.extraFinishes, runtime.Extra.Count);
                    Decks.Add(runtime);
                }
        }

        private static string At(string[] array, int index) =>
            array != null && index < array.Length && array[index] != null ? array[index] : "";

        private static int At(int[] array, int index, int fallback) =>
            array != null && index < array.Length ? array[index] : fallback;

        /// <summary>
        /// Füllt die Finish-Liste eines Decks auf. Fehlende Werte sind „schlicht" —
        /// so bleiben Decks gültig, die vor den Finishes gespeichert wurden.
        /// </summary>
        private static void FillFinishes(List<CardFinish> target, int[] source, int count)
        {
            target.Clear();
            for (int i = 0; i < count; i++)
            {
                int value = source != null && i < source.Length ? source[i] : 0;
                target.Add((CardFinish)Mathf.Clamp(value, 0, CardFinishInfo.Count - 1));
            }
        }

        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, out int parsed) ? parsed : fallback;

        public static void Clear()
        {
            LoggedIn = false;
            AccountName = "";
            Coins = 0;
            TokensCommon = TokensUncommon = TokensRare = TokensLegendary = 0;
            Collection.Clear();
            PackInventory.Clear();
            Decks.Clear();
        }

        public static int Owned(string cardName) =>
            cardName != null && Collection.TryGetValue(cardName, out int count) ? count : 0;

        /// <summary>Wie viele Exemplare dieses Finishes besitzt der Spieler?</summary>
        public static int Owned(string cardName, CardFinish finish) =>
            cardName != null && Stock.TryGetValue(cardName, out var stock) ? stock[finish] : 0;

        /// <summary>Der Bestand einer Karte nach Finish (nie null).</summary>
        public static CardStock StockOf(string cardName) =>
            cardName != null && Stock.TryGetValue(cardName, out var stock) ? stock : EmptyStock;

        private static readonly CardStock EmptyStock = new CardStock();

        /// <summary>
        /// Wie oft diese Karte laut Banlist ins Deck darf. Nicht gelistete Karten haben
        /// kein Limit — dann gilt die normale Regel aus GameRules.
        /// </summary>
        public static int AllowedCopies(string cardName, int defaultMax)
        {
            if (cardName != null && Banlist.TryGetValue(cardName, out int limit))
                return Mathf.Clamp(limit, 0, defaultMax);
            return defaultMax;
        }

        /// <summary>True, wenn die Karte überhaupt auf der Banlist steht.</summary>
        public static bool IsRestricted(string cardName) =>
            cardName != null && Banlist.ContainsKey(cardName);

        public static int PacksOf(string packName) =>
            packName != null && PackInventory.TryGetValue(packName, out int count) ? count : 0;

        public static int Tokens(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return TokensUncommon;
                case CardRarity.Rare: return TokensRare;
                case CardRarity.Legendary: return TokensLegendary;
                default: return TokensCommon;
            }
        }
    }
}
