using System;

namespace Rouge.Tcg.Net
{
    /// <summary>Nachricht vom/zum Relay-Server (flach gehalten für JsonUtility).</summary>
    [Serializable]
    public class NetMessage
    {
        public string t;           // welcome | queued | lobby | match | relay | peer_left | left | error
        public int id;             // welcome
        public string name;        // hello
        public string code;        // lobby / join
        public string msg;         // error
        public string youAre;      // match: "A" oder "B"
        public string opponent;    // match: Gegnername
        public string[] oppSlots;  // match: Kosmetik-Fächer des Gegners
        public string[] oppIds;    // match: was er darin trägt (parallel zu oppSlots)
        public int seed;           // match: gemeinsamer RNG-Seed
        public string startPlayer; // match: "A" oder "B"
        public NetData data;       // relay-Inhalt

        // Account & Sammlung
        public string pass;        // register/login: Passwort

        // Steam-Anmeldung: Hex-Ticket aus SteamBridge, serverseitig geprüft
        public string steamTicket;
        public string steamName;   // Vorschlag für den Duellisten-Namen (Steam-Persona)
        public string pack;        // buy_pack/open_pack: Packname
        public int packCount;      // open_pack: wie viele auf einmal (0/1 = eines)
        public string card;        // craft/dust: Kartenname
        public string starter;     // claim_starter: Id des gewählten Startdecks
        public string item;        // buy_cosmetic/equip_cosmetic: Gegenstands-Id
        public string slot;        // equip_cosmetic: Fach
        public bool won;           // duel_result
        public int floor;          // tower_progress: erstmals bezwungene Turm-Ebene

        // rank_change: kommt am Duellende, wenn sich der Rang bewegt hat
        public int rankDelta;      // RP-Änderung, kann negativ sein
        public int rankValue;      // Rang danach (1..10)
        public int rankTier;       // Unterstufe danach (1..5)
        public string rankName;
        public int rankRp;
        public int rankFromValue;  // Rang davor
        public int rankFromTier;
        public bool rankPromoted;  // Unterstufe gestiegen
        public bool rankUp;        // Hauptrang gestiegen — nur dann läuft die Animation
        public NetProfile profile;   // auth_ok / profile / pack_result / craft_result
        public string[] packCards;   // pack_result: gezogene Karten (Namen)
        public int[] packFinishes;   // pack_result: Finish je gezogener Karte
        public int finish;           // craft_result: gewürfeltes Finish der gefertigten Karte

        // Deck-Verwaltung
        public int deckIndex;      // save_deck/delete_deck/queue
        public NetDeck deck;       // save_deck

        // Deck-/Karten-Statistiken
        public string deckName;     // solo_result: Name des gespielten Decks
        public string deckHero;     // solo_result: Heldenkarte des gespielten Decks
        public string[] deckCards;  // solo_result: Main-Deck (Kartennamen)
        public string[] deckExtra;  // solo_result: Extra Deck (Kartennamen)
        public StatsDeck[] decks;   // stats_decks: Antwort des Servers
        public StatsCard[] cardStats;   // stats_cards: Karten mit Bilanz
        public StatsArchetype[] archetypeStats; // stats_archetypes: Familien mit Bilanz
        public StatsArchPair[] archetypePairs;  // stats_archetypes: die Duos dazu
        public StatsPair[] partners;    // stats_card_detail: häufigste Deck-Partner

        // Profil-Statistiken (profile_stats)
        public ProfileMatch[] matches;  // die letzten Spiele des Kontos
        public int pvpGames;
        public int pvpWins;
        public int soloGames;
        public int soloWins;
        public LiveGame[] liveGames;    // gerade laufende Server-Duelle (zum Zuschauen)
        public ShowcaseCard[] showcase; // profile_stats/set_showcase: die 3 Schaufenster-Karten

        // Freunde & Herausforderungen
        public string friendCode;    // friends: der eigene Code zum Weitergeben
        public FriendEntry[] friends;// friends: die Liste
        public string[] requests;    // friends: offene eingehende Anfragen (Namen)
        public string kind;          // friend_event: request | sent | accepted

        // Fremdes Profil (profile_view)
        public bool online;          // ist der Spieler gerade verbunden?
        public bool isFriend;
        public int rankWins;
        public int rankLosses;
        public int rankBestStreak;
        public string avatarId;      // ausgerüstete Kosmetik, Ids reichen dem Client
        public string frameId;
        public string titleId;

        // Replays
        public ReplayEntry[] replays; // replay_list / profile_view
        public long replayId;         // replay_delete/replay_watch: welches
        public string owner;          // replay_list: wessen Liste das ist
        public string a;              // replay_start: Spieler A
        public string b;              // replay_start: Spieler B

        // Server-autoritatives Duell
        public bool sduel;          // hello: Client beherrscht Server-Duelle
        public string op;           // sduel: state | request | events | log | waiting | end
        public string duelId;
        public string winner;       // sduel end: "A" | "B"
        public SduelView view;      // sduel state
        public SduelRequest request;// sduel request
        public SduelEvent[] events; // sduel events
        public string[] lines;      // sduel log
        public string text;         // sduel waiting: was der Gegner gerade tut (leer = fertig)
        public SduelAnswer answer;  // sduel_intent (Client -> Server)
    }

    /// <summary>Ein Eintrag der Deck-Statistik (stats_decks): ein Deck über alle Spieler.</summary>
    [Serializable]
    public class StatsDeck
    {
        public string name;         // zuletzt benutzter Deckname
        public string hero;
        public int games;           // alle Matches (PvP + Solo)
        public int wins;
        public int pvpGames;        // davon server-autoritative PvP-Matches
        public int pvpWins;
        public StatsCardCount[] cards;
        public StatsCardCount[] extra;
    }

    /// <summary>Kartenname + Kopienzahl in einem Statistik-Deck.</summary>
    [Serializable]
    public class StatsCardCount
    {
        public string n;
        public int c;
    }

    /// <summary>Bilanz einer Karte über alle Matches (stats_cards); ein Match zählt einmal.</summary>
    [Serializable]
    public class StatsCard
    {
        public string n;
        public int games;
        public int wins;
        public int pvpGames;
        public int pvpWins;
    }

    /// <summary>Ein häufiger Deck-Partner einer Karte (stats_card_detail).</summary>
    [Serializable]
    public class StatsPair
    {
        public string n;
        public int games;   // gemeinsame Matches
        public int wins;    // davon gewonnen
    }

    /// <summary>Bilanz eines Archetypes über Online-Matches (stats_archetypes).</summary>
    [Serializable]
    public class StatsArchetype
    {
        public string n;
        public int games;
        public int wins;
    }

    /// <summary>Ein Archetype-Duo, das gemeinsam in Decks stand (stats_archetypes).</summary>
    [Serializable]
    public class StatsArchPair
    {
        public string a;
        public string b;
        public int games;
        public int wins;
    }

    /// <summary>Ein Eintrag der Match-Historie (profile_stats).</summary>
    [Serializable]
    public class ProfileMatch
    {
        public long ts;
        public string mode;      // "pvp" | "solo"
        public string opponent;
        public string deckName;
        public bool won;
    }

    /// <summary>Ein gerade laufendes Server-Duell (profile_stats: zum Zuschauen).</summary>
    [Serializable]
    public class LiveGame
    {
        public string duelId;
        public string a;
        public string b;
    }

    /// <summary>Eine Schaufenster-Karte des Profils (Name + Finish).</summary>
    [Serializable]
    public class ShowcaseCard
    {
        public string n;
        public int f;
    }

    /// <summary>Ein Freund in der Liste (friends).</summary>
    [Serializable]
    public class FriendEntry
    {
        public string name;
        public bool online;
        public bool inDuel;
    }

    /// <summary>Ein gespeichertes Replay (replay_list / profile_view).</summary>
    [Serializable]
    public class ReplayEntry
    {
        public long replayId;
        public string a;
        public string b;
        public string winner;   // "A" | "B"
        public long endedAt;
    }

    // ================== SERVER-DUELL (Wire-Formate des DuelHost) ==================

    /// <summary>Eine Karte aus Server-Sicht — name null = für diesen Spieler verdeckt.</summary>
    [Serializable]
    public class SduelCard
    {
        public int id;             // 0 = leerer Zonen-Slot
        public string name;
        public bool faceDown;
        public string position;    // "atk" | "def"
        public int atk;
        public int def;
        public bool negated;
        public int deathCounters;
        public int lienAmount;     // The Small Print: Pfandrecht-Betrag (0 = keins)

        // Ausführung des Exemplars (CardFinish). Nur bei sichtbaren Karten
        // gefüllt — eine verdeckte Karte am Funkeln zu erkennen wäre verraten,
        // was der Gegner nicht wissen soll.
        public int finish;
    }

    /// <summary>Die Seite eines Spielers; hand/extra sind nur für den Besitzer gefüllt.</summary>
    [Serializable]
    public class SduelSide
    {
        public string name;
        public int lp;
        public int mana;
        public int manaPerTurn;
        // Für die Mana-Anzeige: dauerhafter Bonus (gehört zur Runden-Basis) und
        // der Übertrag in die nächste Runde (Credit/Debt aus Mana-Effekten)
        public int bonusManaPerTurn;
        public int manaCredit;
        public int manaDebt;
        public int deckCount;
        public int extraCount;
        public int handCount;
        public SduelCard[] hand;
        public SduelCard[] extra;
        public SduelCard[] monsters;
        public SduelCard[] spells;
        public SduelCard[] artifacts;
        public SduelCard player;
        public SduelCard[] grave;
        public SduelCard[] banished;
    }

    [Serializable]
    public class SduelView
    {
        public int turn;
        public string phase;       // DuelPhase-Name
        public bool yourTurn;
        public SduelSide you;
        public SduelSide foe;
    }

    /// <summary>Präsentations-Ereignis (maskiert: cardName null, wenn nicht sichtbar).</summary>
    [Serializable]
    public class SduelEvent
    {
        public string type;        // banner|cointoss|draw|shuffle|summon|moved|position|activation|pulse|targets|attack|impact|destroyed|tograve|spelltograve|banished
        public int cardId;
        public string cardName;
        public int targetId;
        public bool mine;
        public string text;
        public bool direct;

        /// <summary>Nummer des Kettenglieds (1-basiert), nur bei chain*-Ereignissen.</summary>
        public int link;

        // Effekt-Anzeige beim "pulse": text = Label, dazu Kartentext + Kosten,
        // damit der Client das Panel unter der gehobenen Karte füllen kann.
        public string effectText;
        public int effectCost;
        public int effectInfused; // 0 = nein, 1 = Standalone, 2 = Coupled
    }

    [Serializable]
    public class SduelMainOption
    {
        public int i;
        public string kind;        // MainActionKind-Name
        public string label;
        public int cardId;
    }

    [Serializable]
    public class SduelBattleOption
    {
        public int i;
        public string label;
        public int attackerId;
        public int targetId;
        public bool direct;
        public bool endBattle;
    }

    /// <summary>Eine Entscheidungs-Anfrage des Servers (Felder je nach type).</summary>
    [Serializable]
    public class SduelRequest
    {
        public int reqId;
        public string type;        // start|main|battle|yesno|option|target|zone
        public string title;
        public string question;    // yesno
        public int cardId;         // yesno/option: Kontext-Karte
        public bool isPhaseWindow; // yesno
        public bool isResponse;    // yesno: Reaktionsangebot (Toggle darf ablehnen)
        public SduelMainOption[] mainOptions;
        public SduelBattleOption[] battleOptions;
        public string[] choices;   // option
        public bool allowCancel;   // option/target
        public int[] choiceCardIds;   // option: Karte je Option (0 = keine) — Master-Duel-Reaktionsliste
        public bool isResponseList;   // option: Reaktions-Angebot (Toggle darf pauschal passen)
        public bool searchable;       // option: Namenssuche mit Filterfeld (The Forbidden Name)
        public SduelCard[] candidates; // target
        public int count;          // target
        public bool allowFewer;    // target
        public string zone;        // zone: ZoneType-Name
        public int[] freeIndices;  // zone
    }

    /// <summary>Antwort des Clients auf einen Request (nur die passenden Felder zählen).</summary>
    [Serializable]
    public class SduelAnswer
    {
        public int reqId;
        public bool first;         // start
        public int chosen = -1;    // main/battle/option
        public int zone = -1;      // main: Wunsch-Zone
        public bool result;        // yesno
        public bool cancelled;     // target
        public int[] ids;          // target: Karten-IDs
        public int index = -1;     // zone
    }

    /// <summary>Ein Account-Deck (auf dem Server gespeichert).</summary>
    [Serializable]
    public class NetDeck
    {
        public string name;
        public string hero;
        public string[] cards;
        public string[] extra;  // Extra Deck (Reliquary-Karten)

        // Finish je Exemplar, gleiche Reihenfolge wie cards/extra.
        // Fehlt das Feld, sind alle Karten schlicht — alte Decks bleiben gültig.
        public int[] cardFinishes;
        public int[] extraFinishes;
    }

    /// <summary>
    /// Ein Startdeck zur Auswahl. Kommt komplett mit Kartenliste, damit der
    /// Auswahl-Bildschirm jede Karte anzeigen kann, ohne nachzufragen — der
    /// Spieler soll vor der Entscheidung lesen können, was er bekommt.
    /// </summary>
    [Serializable]
    public class NetStarterDeck
    {
        public string id;
        public string name;
        public string archetypes;   // "Mechination · Sacrilegion"
        public string blurb;        // eine Zeile für die Kachel
        public string description;  // was das Deck tut, mehrere Absätze
        public string hero;
        public string[] cards;
        public string[] extra;
    }

    /// <summary>Konto-Zustand vom Server.</summary>
    [Serializable]
    public class NetProfile
    {
        /// <summary>Das Konto hat noch kein Startdeck gewählt.</summary>
        public bool starterPending;

        /// <summary>Die Auswahl — nur gefüllt, solange sie offen ist.</summary>
        public NetStarterDeck[] starters;

        public string account;
        public int coins;
        public int tokensCommon;
        public int tokensUncommon;
        public int tokensRare;
        public int tokensLegendary;
        public string[] collectionCards;
        public int[] collectionCounts;   // Gesamtzahl je Karte, über alle Finishes

        // Aufschlüsselung nach Finish — gleiche Reihenfolge wie collectionCards
        public int[] collectionPlain;
        public int[] collectionGlossy;
        public int[] collectionRainbow;
        public int[] collectionStatic;

        /// <summary>Frisch erhaltene Karten (Erstbesitz) — NEW-Badge im Deck Builder, bis sie angeklickt werden.</summary>
        public string[] newCards;
        public string[] packNames;   // Pack-Inventar (ungeöffnete Packs)
        public int[] packCounts;
        public NetDeck[] decks;      // Account-Decks

        /// <summary>Höchste erstmals bezwungene Turm-Ebene (0 = noch keine).</summary>
        public int towerFloor;

        // Draft-Modus (Challenges): der laufende Draft reist komplett im Profil
        // mit, damit ein Relog genau dort weitermacht. Pool als parallele Arrays
        // (Name + gezogene Anzahl), das Deck als einfache Kartenlisten — Finishes
        // gibt es im Draft nicht, alles ist schlicht und temporär.
        public bool draftActive;
        public int draftFloor;       // versiegelte Draft-Ebenen (0 = noch keine)
        public int draftClears;      // wie oft der Draft-Turm je abgeschlossen wurde
        public string[] draftPoolNames;
        public int[] draftPoolCounts;
        public string[] draftDeckCards;
        public string[] draftDeckExtra;
        public string draftDeckHero;

        // Daily-Siegel + Server-Status (Shell-Screens)
        public int dailyStreak;      // aktuelle Serien-Länge (0 = nie geclaimt)
        public bool dailyClaimable;  // Siegel bereit?
        public long dailyNextInMs;   // Restzeit bis zum nächsten Claim
        public int dailyRewardCoins; // Belohnung pro Claim

        // Banlist: parallele Arrays, Limit = erlaubte Kopien (0 gebannt, 1 limitiert, 2 semi)
        public string[] banlistNames;
        public int[] banlistLimits;
        public int banlistMaxCopies; // normales Kopienlimit ohne Banlist

        // Banlist-Chronik: parallele Arrays; historyChanges enthält je Eintrag
        // Zeilen der Form "neu|alt|Kartenname", getrennt durch \n
        public string[] historyDates;
        public string[] historyTitles;
        public string[] historyNotes;
        public string[] historyChanges;

        /// <summary>Wurde dieser Account über Steam angelegt? (Anzeige in den Einstellungen)</summary>
        public bool steamLinked;

        // Rangleiter — vollständig serverseitig gerechnet, hier nur zur Anzeige
        public int rankValue;        // 1..10
        public int rankTier;         // 1..5
        public string rankName;
        public int rankRp;
        public int rankTierFloor;    // RP-Untergrenze der aktuellen Unterstufe
        public int rankNextAt;       // RP für die nächste Stufe, -1 an der Spitze
        public string rankSeason;
        public int rankWins;
        public int rankLosses;
        public int rankBestStreak;
        public string[] titles;      // freigeschaltete Profiltitel

        // Kosmetik — Besitz, Ausrüstung und der Ladenkatalog (Preise in Coins)
        public string[] cosmeticsOwned;
        public string[] equippedSlots;   // Fachnamen in fester Reihenfolge
        public string[] equippedIds;     // dazu passend, leer = nichts ausgerüstet
        public string[] shopIds;
        public string[] shopNames;
        public string[] shopSlots;
        public string[] shopRarities;
        public int[] shopPrices;         // -1 = nicht käuflich
        public string[] shopCurrencies;  // "coins" | "shards" | ""
        public string[] shopUnlocks;     // wie man einen unverkäuflichen bekommt

        public int online;           // verbundene Spieler
    }

    /// <summary>Inhalt einer Relay-Nachricht zwischen den beiden Spielern.</summary>
    [Serializable]
    public class NetData
    {
        public string t;        // "deck" | "answer"

        // Deck-Austausch
        public string[] cards;  // Kartennamen in Deck-Reihenfolge
        public string[] extra;  // Extra Deck (Reliquary-Karten)
        public string hero;     // Name der Spielerkarte
        public string deckName;

        // Entscheidungs-Antwort (Lockstep)
        public string kind;     // main | battle | yesno | option | target
        public int seq;         // fortlaufende Nummer der Antworten dieses Spielers
        public int chosen;      // main/battle/option: gewählter Index (-1 = Abbruch)
        public int zone;        // main: Wunsch-Zone (PreferredZoneIndex)
        public bool result;     // yesno
        public bool cancelled;  // target: abgebrochen
        public int[] indices;   // target: gewählte Kandidaten-Indizes
        public string check;    // Desync-Prüfwert (Zug:LP:LP)
    }
}
