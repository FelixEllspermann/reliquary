using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rouge.Tcg
{
    [Serializable]
    public class EffectAction
    {
        [Tooltip("Was passiert")]
        public EffectActionType type = EffectActionType.DamageOpponent;

        [Tooltip("Kosten (Textteil VOR dem Semikolon): wird sofort bei der Aktivierung ausgeführt, " +
                 "noch bevor der Gegner reagieren kann — nicht erst bei der Auflösung.")]
        public bool isCost;

        [Tooltip("Wert (Schaden, Heilung, Kartenanzahl, Buff-Höhe ...)")]
        public int amount = 1;

        [Tooltip("Welche Art Ziel dafür gewählt werden muss")]
        public TargetKind target = TargetKind.None;

        [Tooltip("Die Quellkarte selbst ist KEIN gültiges Ziel (»1 anderes Monster«)")]
        public bool targetExcludesSelf;

        [Header("Filter (für Deck-/Hand-/Friedhof-Ziele)")]
        [Tooltip("Nur Monster dieses Typs als Ziel zulassen?")]
        public bool useTypeFilter;

        [Tooltip("Erlaubter Monster-Typ (wenn Filter aktiv)")]
        public MonsterType typeFilter = MonsterType.Dragon;

        [Range(0, 3)]
        [Tooltip("Erlaubtes Monster-Level (0 = beliebig)")]
        public int levelFilter;

        [Range(1, 5)]
        [Tooltip("Wie viele Ziele gewählt werden (bei weniger Kandidaten: so viele wie möglich)")]
        public int targetCount = 1;

        [Tooltip("\"Bis zu\": targetCount ist nur die Obergrenze — der Spieler darf früher fertig sein. " +
                 "Passend für Kartentexte mit \"up to N\".")]
        public bool upToTargets;

        [Tooltip("Nur Karten, deren NAME diesen Text enthält (leer = egal), z.B. \"Dragon Shrine\"")]
        public string nameFilter = "";

        [Tooltip("Nur Karten, deren Name ODER Effekttext diesen Text enthält (leer = egal), z.B. \"Dragon\"")]
        public string mentionsFilter = "";

        [Tooltip("Nur Monster mit HÖCHSTENS so viel ATK als Ziel zulassen (0 = egal)")]
        public int maxAtkFilter;

        [Tooltip("Nur Monster dieses Attributs als Ziel zulassen?")]
        public bool useAttributeFilter;

        [Tooltip("Erlaubtes Attribut (wenn Filter aktiv)")]
        public MonsterAttribute attributeFilter = MonsterAttribute.Light;

        [Tooltip("Zählbasis für ...PerCount-Aktionen")]
        public EffectCountKind countKind = EffectCountKind.OwnArtifactsOnField;

        [Tooltip("Karten mit demselben NAMEN wie die Quellkarte sind kein gültiges Ziel " +
                 "(Trapline: \"mit anderem Namen\")")]
        public bool excludeSameName;

        [Tooltip("Nur Monster OHNE Effekte als Ziel zulassen (Rally the Weak)")]
        public bool onlyWithoutEffects;

        [Tooltip("Spezialbeschwörungen dieser Aktion kommen in Verteidigungsposition")]
        public bool summonInDefense;

        [Tooltip("The Small Print: diese Aktion läuft nur, wenn der letzte Münzwurf des Effekts " +
                 "(FlipCoin) so fiel — Heads oder Tails. None = immer.")]
        public CoinGate coinGate = CoinGate.None;

        [Header("Road to 1000 (September 2026)")]
        [Tooltip("Von dieser Aktion beschworene Monster können diesen Zug nicht angreifen")]
        public bool summonCannotAttack;

        [Tooltip("Nur Monster als Ziel, deren Level NIEDRIGER ist als die Monsterzahl " +
                 "ihres Besitzers (Cut Down to Size)")]
        public bool requireLevelBelowControllerCount;

        [Tooltip("Nur Monster als Ziel, die nicht angreifen können — per Passiv oder " +
                 "diesen Zug (Eviction Notice)")]
        public bool onlyCannotAttack;

        [Tooltip("Nur Monster mit 0 Basis-ATK als Ziel (Regent, Long Live the King)")]
        public bool zeroAtkOnly;

        [Header("5 Archetypes (September 2026)")]
        [Tooltip("Splithoof: diese Aktion läuft nur, wenn der Gegner beim letzten " +
                 "OfferDeal des Effekts diese Option gewählt hat. None = immer.")]
        public DealGate dealGate = DealGate.None;

        [Tooltip("OfferDeal: Text der Option A, wie der Gegner sie im Dialog liest")]
        public string dealOptionA = "";

        [Tooltip("OfferDeal: Text der Option B")]
        public string dealOptionB = "";
    }

    [Serializable]
    public class EffectDefinition
    {
        [Tooltip("Kurzname für Prompts und Log (z.B. 'Flammenstoß')")]
        public string label = "Effekt";

        [TextArea(2, 6)]
        [Tooltip("Effekttext, wie er auf der Karte steht (YuGiOh-Stil)")]
        public string text = "";

        [Tooltip("Ist das der Infused-Effekt der Karte? (Wird getrennt dargestellt)")]
        public bool isInfused;

        [Tooltip("Standalone = eigenständige Fähigkeit. Coupled = Upgrade des vorangehenden " +
                 "Normal-Effekts: pro Zug nur einer von beiden nutzbar.")]
        public InfusedKind infusedKind = InfusedKind.Standalone;

        [Range(0, 10)]
        [Tooltip("Manakosten der Aktivierung (0 = gratis)")]
        public int manaCost;

        [Tooltip("Wann bzw. wie dieser Effekt aktiviert wird")]
        public EffectTrigger trigger = EffectTrigger.Ignition;

        [Tooltip("Nur einmal pro Zug aktivierbar?")]
        public bool oncePerTurn;

        [Tooltip("Nur aktivierbar, wenn diese Karte spezialbeschworen wurde?")]
        public bool onlyIfSpecialSummoned;

        [Tooltip("Nur aktivierbar, wenn diese Karte ein Artefakt ausgerüstet hat (Genostitched)")]
        public bool requiresEquippedArtifact;

        [Tooltip("AUCH der Gegner darf diesen Ignition-Effekt in seiner Main Phase aktivieren " +
                 "(Elephant in the Room). Der Aktivierende zahlt und profitiert; einmal pro Zug " +
                 "gilt für die Karte insgesamt, egal wer sie anspricht.")]
        public bool eitherPlayerMayActivate;

        [Tooltip("Fenster-Beschränkung für gesetzte Quick-Zauber (Trapline-Fallen): " +
                 "AttackResponse/SummonResponse zünden NUR im jeweiligen Reaktionsfenster " +
                 "und sind nicht offen aus der Hand spielbar.")]
        public QuickWindow quickWindow = QuickWindow.Any;

        [Tooltip("PFLICHT-Trigger (Deckay): feuert ohne Nachfrage, sobald der Auslöser eintritt. " +
                 "Gilt nur für Ereignis-/Phasen-Trigger — Ignition-Effekte bleiben freiwillig.")]
        public bool mandatory;

        [Tooltip("Dieser Reaktions-Effekt ist NUR anwählbar, wenn das auslösende Summon ein " +
                 "Reliquary war (Deckay Fiend/Vulture).")]
        public bool onlyReliquarySummonResponse;

        [Tooltip("Quick-Effekt nur im EIGENEN Zug anwählbar (The Forbidden Name: der Normal-Effekt " +
                 "bleibt daheim, erst das Infused-Upgrade reagiert im Gegnerzug).")]
        public bool onlyDuringYourTurn;

        [Tooltip("Effekt nur im GEGNERISCHEN Zug anwählbar (Emergency Barrier: der Notfall-Einsatz " +
                 "aus der Hand lohnt nur, wenn der Gegner am Zug ist — im eigenen spielt man normal).")]
        public bool onlyDuringOpponentTurn;

        [Tooltip("Friedhofs-Trigger zündet NUR, wenn die Karte aus dem EXTRA DECK in den Friedhof " +
                 "ging (The Last Asemir).")]
        public bool onlyFromExtraDeck;

        [Tooltip("The Small Print: einmal pro DUELL je Spieler und Kartenname (The Unbroken Oath, " +
                 "First and Last Word).")]
        public bool oncePerDuel;

        [Tooltip("The Small Print: nur in einer MAIN PHASE anwählbar (High Stakes: die gegnerische).")]
        public bool onlyDuringMainPhase;

        [Tooltip("The Small Print: nur während einer BATTLE PHASE anwählbar (Parley).")]
        public bool onlyDuringBattlePhase;

        [Header("Aktivierungs-Bedingungen (0/false = keine Bedingung)")]
        [Tooltip("Nur aktivierbar mit mindestens so viel verfügbarem Mana (zusätzlich zu den Kosten)")]
        public int minMana;

        [Tooltip("Nur aktivierbar mit mindestens so vielen eigenen Monstern auf dem Feld")]
        public int minOwnMonsters;

        [Tooltip("Nur aktivierbar mit mindestens so vielen eigenen verdeckten Monstern")]
        public int minOwnFaceDownMonsters;

        [Tooltip("Nur aktivierbar mit mindestens so vielen Karten im eigenen Friedhof")]
        public int minOwnGraveyardCards;

        [Tooltip("Nur aktivierbar, wenn der Gegner mehr Handkarten hat als du")]
        public bool requireOpponentMoreHandCards;

        [Tooltip("Nur aktivierbar, wenn der Gegner mehr Monster kontrolliert als du")]
        public bool requireOpponentMoreMonsters;

        [Tooltip("Nur aktivierbar, wenn du in DIESEM oder dem VORHERIGEN Zug gemillt hast (Deckay)")]
        public bool requireMilledLastTurn;

        [Tooltip(">0: nur aktivierbar mit mindestens so vielen Friedhofskarten, deren Name den " +
                 "Namensfilter unten enthält (Deckay Vulture: 5+ \"Deckay\")")]
        public int minOwnGraveyardNamed;

        [Tooltip("Namensfilter für die Bedingung darüber")]
        public string graveyardNamedFilter = "";

        [Header("Road to 1000: weitere Bedingungen")]
        [Tooltip("Nur aktivierbar/auslösbar, wenn du ALLE hier gelisteten Karten offen " +
                 "kontrollierst — Namen mit ';' getrennt (Krönung des abwesenden Königs)")]
        public string requiresControlNamed = "";

        [Tooltip("Friedhofs-Effekt nur anwählbar, solange die Karte die OBERSTE Karte " +
                 "des Friedhofs ist (He Sleeps Lightly)")]
        public bool onlyWhileGraveTop;

        [Tooltip("Nur aktivierbar, wenn diesen Zug ein eigenes Monster zerstört wurde " +
                 "(Buried With His Boots On)")]
        public bool requireOwnMonsterDestroyedThisTurn;

        [Tooltip("Nur im ERSTEN eigenen Zug des Duells aktivierbar (First Mover's Advantage)")]
        public bool onlyOnFirstOwnTurn;

        [Tooltip("Nur aktivierbar mit HÖCHSTENS so vielen eigenen Monstern (0 = keine " +
                 "Bedingung; Making Ends Meet: 1). Alte Assets ohne dieses Feld laden als 0 = aus.")]
        public int maxOwnMonsters;

        [Header("5 Archetypes: weitere Bedingungen")]
        [Tooltip("Giftwyrm: dieser Trigger zündet nur, wenn die Karte (beim Verlassen des " +
                 "Feldes) vom GEGNER kontrolliert war bzw. gerade kontrolliert wird")]
        public bool onlyWhileControlledByOpponent;

        [Tooltip("Waylay: nur aktivierbar, wenn dein Gegner diesen Zug angegriffen hat")]
        public bool requireOpponentAttackedThisTurn;

        [Tooltip("Chimekeep: nur aktivierbar, wenn diesen Zug eine deiner Countdown-Karten " +
                 "ihren Nullschlag hatte (Chime In)")]
        public bool requireStruckThisTurn;

        [Header("Welle 3: 50 Generics")]
        [Tooltip("Nur aktivierbar mit mindestens so vielen Karten in der EIGENEN Verbannung " +
                 "(0 = keine Bedingung; The Unforgotten: 3)")]
        public int minOwnBanishedCards;

        [Header("Incarnates")]
        [Tooltip("Avatar: nur aktivierbar, wenn das letzte Kettenglied ein GEGNER-Effekt ist, " +
                 "der ihn Karten ziehen ließe (DrawCards & Co.)")]
        public bool requiresOpponentDrawChainLink;

        [Tooltip("Aktionen, die bei der Auflösung in Reihenfolge ausgeführt werden")]
        public List<EffectAction> actions = new List<EffectAction>();
    }
}
