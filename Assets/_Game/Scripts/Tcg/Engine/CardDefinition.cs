using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Rouge.Tcg
{
    public abstract class CardDefinition : ScriptableObject
    {
        [Header("Karte")]
        [Tooltip("Anzeigename der Karte")]
        public string cardName = "Neue Karte";

        [Tooltip("Kartenbild (optional)")]
        public Sprite artwork;

        [Tooltip("Seltenheit — bestimmt Pack-Ziehungen und Craft-Kosten")]
        public CardRarity rarity = CardRarity.Common;

        [Tooltip("Token: nur von Effekten erzeugt — nicht sammelbar, nicht im Deck, löst sich beim Verlassen des Feldes auf")]
        public bool isToken;

        [Tooltip("Spielversion, in der die Karte neu dazukam (z.B. \"0.1.6\", \"0.1.6b\"). Leer = Bestand. " +
                 "Die NEW-CARDS-Szene der Patchnotes listet alle Karten der laufenden Version samt Buchstaben-Patches.")]
        public string releaseVersion = "";

        [Header("Effekte (Normal & Infused)")]
        [Tooltip("Alle Effekte dieser Karte. Infused-Effekte werden auf der Karte getrennt dargestellt und kosten Mana.")]
        public List<EffectDefinition> effects = new List<EffectDefinition>();

        // ---- Dauer-Aura: wirkt, solange die Karte offen auf dem Feld liegt,
        //      auf EIGENE Monster, die die Filter erfüllen (Batch August 2026) ----
        [Header("Dauer-Aura (0 = keine)")]
        [Tooltip("ATK-Bonus für eigene Monster, die die Aura-Filter erfüllen")]
        public int auraAtkBonus;
        [Tooltip("DEF-Bonus für eigene Monster, die die Aura-Filter erfüllen")]
        public int auraDefBonus;
        [Tooltip("Aura nur für Monster, deren Name diesen Text enthält (leer = alle)")]
        public string auraNameFilter = "";
        [Tooltip("Aura nur für Monster dieses Typs?")]
        public bool auraUseTypeFilter;
        public MonsterType auraTypeFilter = MonsterType.Beast;
        [Range(0, 3)]
        [Tooltip("Aura nur für Monster dieses Levels (0 = alle)")]
        public int auraLevelFilter;
        [Tooltip("Aura wirkt nur auf VERDECKTE eigene Monster (Blackout Curtain)")]
        public bool auraOnlyFaceDown;
        [Tooltip("Die Quellkarte selbst bekommt die Aura nicht (»deine anderen Monster«)")]
        public bool auraExcludesSelf;

        [Header("Passive Flaggen (Batch August 2026)")]
        [Tooltip("Der Gegner muss dieses Monster angreifen, solange es offen liegt (Attention Hound)")]
        public bool passiveTaunt;
        [Tooltip(">0: im Kampf unzerstörbar, solange du mindestens N Artefakte kontrollierst (Ironclad)")]
        public int battleShieldMinOwnArtifacts;
        [Tooltip("Zählt beim Reliquary-Tribut als N Monster (Twice-Blessed)")]
        public int tributeWorth = 1;
        [Tooltip("Eigene Karten mit diesem Namen sind für den Gegner kein gültiges Effekt-Ziel (Heavenly Bodyguard)")]
        public string protectsNamedFromTargeting = "";
        [Tooltip("Zweiter Angriff pro Battle Phase, solange ein ANDERES eigenes Monster dieses Attributs offen liegt")]
        public bool conditionalDoubleAttack;
        public MonsterAttribute doubleAttackAttribute = MonsterAttribute.Wind;

        [Tooltip(">0: dieses Monster hat dauerhaft +N ATK pro gezählter Karte (Weight of Evidence)")]
        public int passiveAtkPerCount;
        public EffectCountKind passiveAtkPerCountKind = EffectCountKind.OwnArtifactsOnField;
        [Tooltip(">0: dieses Monster hat dauerhaft +N DEF pro gezählter Karte")]
        public int passiveDefPerCount;
        public EffectCountKind passiveDefPerCountKind = EffectCountKind.OwnArtifactsOnField;

        [Tooltip("Dieses Monster kann nie angreifen (Barrierstruck Peacekeeper)")]
        public bool passiveCannotAttack;

        [Tooltip("Dieses Monster kann im Zug seiner Beschwörung nicht angreifen (Slow to Anger)")]
        public bool passiveNoAttackOnSummonTurn;

        [Tooltip("Dieses Monster kann im Zug seiner Beschwörung nicht DIREKT angreifen (Tidebound Leviathan)")]
        public bool passiveNoDirectAttackOnSummonTurn;

        [Tooltip("Feld-Limit (Snugglet): dieses Monster ist nicht beschwörbar/setzbar, solange du " +
                 "bereits N Monster kontrollierst, deren Name diesen Text enthält (leer = kein Limit)")]
        public string fieldLimitName = "";
        [Tooltip("Das N zum Feld-Limit (0 = aus)")]
        public int fieldLimitCount;

        [Tooltip(">0 (King of Deckay): jedes Mal, wenn der Besitzer 1+ Karten millt, erleidet " +
                 "der Gegner so viel Schaden — solange diese Karte offen auf dem Feld liegt")]
        public int passiveBurnPerMill;

        [Header("Dark-Angel-Passives (August 2026)")]
        [Tooltip("Solange offen auf dem Feld: KEIN Spieler kann Reliquaries beschwören (The Fallen One)")]
        public bool passiveBlockReliquarySummons;

        [Tooltip("Dieses Monster kann nicht durch Kampf zerstört werden")]
        public bool passiveNoBattleDestroy;

        [Tooltip("Dieses Monster kann nicht von gegnerischen Effekten als Ziel gewählt werden")]
        public bool passiveUntargetable;

        [Tooltip("Kein Spieler erleidet Kampfschaden aus Kämpfen, an denen dieses Monster beteiligt ist")]
        public bool passiveNoBattleDamageInvolving;

        [Tooltip("Solange offen auf dem Feld: der GEGNER erleidet allen Kampfschaden, den der " +
                 "Besitzer erleiden würde (The Last Asemir)")]
        public bool passiveRedirectBattleDamage;

        [Tooltip(">0: in jeder End Phase erhält diese Karte einen Death Counter; bei N Countern " +
                 "geht sie in den Friedhof (Immortal Demon)")]
        public int passiveDeathCounterLimit;

        [Tooltip(">0: jedes Mal, wenn der GEGNER Karten aus seinem Deck in seinen Friedhof schickt " +
                 "(ausser durch diesen Effekt selbst), schickt er N weitere hinterher " +
                 "(Exponential Deterioration)")]
        public int passiveOpponentMillAmplify;

        [Header("Schutz & Zwang (August 2026, Teil 2)")]
        [Tooltip("Solange offen auf dem Feld: KEINE Karte des Besitzers kann vom Gegner als " +
                 "Effekt-Ziel gewählt oder angegriffen werden — diese Karte eingeschlossen. " +
                 "Nicht-zielende Effekte (z.B. \"destroy all\") wirken weiter (Emergency Barrier)")]
        public bool passiveProtectAllFromTargetingAndAttacks;

        [Tooltip(">0: in JEDER End Phase zahlt der Besitzer N LP oder diese Karte wird zerstört " +
                 "(Emergency Barrier)")]
        public int passiveEndPhaseLpToll;

        [Tooltip("Solange offen auf dem Feld: der GEGNER kann Zauber nicht aus der Hand aktivieren — " +
                 "er muss sie erst setzen (und gesetzte Zauber zünden frühestens im Folgezug) " +
                 "(The Liberator)")]
        public bool passiveOpponentMustSetSpells;

        [Header("The Small Print (August 2026)")]
        [Tooltip("Aura wirkt nur auf die Monster in den NACHBARZONEN dieser Karte (Serjeant Halloway)")]
        public bool auraAdjacentOnly;

        [Tooltip("Aura wirkt nur auf eigene Monster OHNE Nachbarn (The Empty Chair); " +
                 "auraCrowdedAtkPenalty trifft die mit Nachbarn")]
        public bool auraAloneOnly;

        [Tooltip(">0: eigene Monster MIT Nachbarn verlieren so viel ATK (The Empty Chair)")]
        public int auraCrowdedAtkPenalty;

        [Tooltip(">0: das gegnerische Monster GEGENÜBER dieser Karte verliert so viel ATK (Rook's Gambit)")]
        public int facingAtkPenalty;

        [Tooltip("Nachbarn dieser Karte können nicht durch Karteneffekte zerstört werden (Load-Bearing Wall)")]
        public bool passiveAdjacentNoEffectDestroy;

        [Tooltip("Nachbarn dieser Karte können nicht durch Kampf zerstört werden (Castellan of the Long Wall)")]
        public bool passiveAdjacentNoBattleDestroy;

        [Tooltip(">0: wird diese Karte zerstört, verlieren ihre Nachbarn dauerhaft N ATK und DEF (Load-Bearing Wall)")]
        public int passiveAdjacentDebuffOnDestroy;

        [Tooltip("Diese Karte kann nicht durch Karteneffekte zerstört werden (Stone That Would Not Break)")]
        public bool passiveNoEffectDestroy;

        [Tooltip("Diese Karte kann nicht als Tribut gezahlt werden (White Elephant, Gift Horse, Stone)")]
        public bool passiveCannotBeTributed;

        [Tooltip("Diese Karte kann ihre Kampfposition nicht wechseln — Effekte, die es versuchen, verpuffen " +
                 "(Load-Bearing Wall, Stone That Would Not Break)")]
        public bool passiveCannotChangePosition;

        [Tooltip("Hat diese Karte in diesem Zug die Position gewechselt, kann sie nicht angreifen (Volte-Face)")]
        public bool passiveNoAttackAfterPositionChange;

        [Tooltip("Kann nicht als Normalbeschwörung beschworen oder gesetzt werden — nur per eigener " +
                 "Spezialbeschwörung (White Elephant, Blood Oath, Sworn to the Gate, Load-Bearing Wall)")]
        public bool passiveNoNormalSummon;

        [Tooltip(">0: in jeder Standby Phase des KONTROLLEURS verliert dieser N LP (White Elephant, Gift Horse)")]
        public int passiveControllerStandbyLpLoss;

        [Tooltip("Solange auf dem Feld: der Besitzer kann keine ANDEREN Monster spezialbeschwören (Sworn to the Gate)")]
        public bool passiveOwnerNoOtherSpecialSummons;

        [Tooltip("Solange der Besitzer keine anderen Monster kontrolliert: nicht durch Kampf oder Effekte " +
                 "zerstörbar (Sworn to the Gate)")]
        public bool passiveLoneImmunity;

        [Tooltip("Solange der Besitzer 1 oder weniger Handkarten hat: nicht zielbar, nicht durch Effekte " +
                 "zerstörbar (The Ascetic of the Ninth Stair)")]
        public bool passiveLowHandImmunity;

        [Tooltip("Piercing: Kampfschaden gegen Verteidigungsposition in Höhe der Differenz (Bristleback Aurochs)")]
        public bool passivePiercing;

        [Tooltip("Ausgerüstetes Monster erhält Piercing (Ram's Head)")]
        public bool passiveBearerPiercing;

        [Tooltip("Ram's Head: greift der Träger ein Verteidigungs-Monster an und zerstört es nicht, " +
                 "wird dieses Artefakt zerstört")]
        public bool passiveBreakOnFailedPierce;

        [Tooltip("Darf direkt angreifen, auch wenn der Gegner Monster kontrolliert; Kampfschaden " +
                 "direkter Angriffe halbiert (Chimney Sweep)")]
        public bool passiveDirectAttackHalved;

        [Tooltip("Kann nie direkt angreifen (Bristleback Aurochs)")]
        public bool passiveNoDirectAttack;

        [Tooltip("Field: Zauber kosten für BEIDE Spieler 1 Mana mehr; wer selbst gezaubert hat, verliert " +
                 "die Karte in seiner End Phase (Guild Tariff)")]
        public bool passiveSpellTaxBoth;

        [Tooltip("Field: jeder Spieler darf pro Battle Phase nur EINEN Angriff erklären; Angreifer +N ATK " +
                 "während des Kampfes (The Duelist's Code)")]
        public int passiveOneAttackBonus;

        [Tooltip("Player: Standby +N Mana; End Phase mit mehr als passiveHandCapForSurvival Handkarten " +
                 "zerstört die Karte (Vow of Poverty)")]
        public int passiveStandbyBonusMana;
        public int passiveHandCapForSurvival;

        [Tooltip(">0: sinken die LP des Besitzers auf N oder darunter, wird die Karte zerstört (Ledger of Small Debts)")]
        public int passiveDestroyWhenLifeAtMost;

        [Tooltip("Player: LP-Kosten des Besitzers werden 0 (Aurel, Who Collects at Midnight)")]
        public bool passiveLifeCostsFree;

        [Tooltip("Player: Münzwürfe des Besitzers zweimal werfen und Ergebnis wählen; zwei Tails zerstören " +
                 "die Karte (Loaded Dice)")]
        public bool passiveCoinChoose;

        [Tooltip("Liegen die LP des Besitzers unter denen des Gegners, zählt Tails als Heads (The House Always Wins)")]
        public bool passiveTailsAsHeadsWhenBehind;

        [Tooltip("Monster mit Pfandrecht verlieren so viel ATK (The Bailiff at the Door)")]
        public int passiveLienAtkPenalty;

        [Tooltip("Monster, die der Besitzer kontrolliert, aber nicht besitzt, erhalten so viel ATK (Broker of Both Sides)")]
        public int passiveStolenAtkBonus;

        [Header("Road to 1000 (September 2026)")]
        [Tooltip("Dieses Monster hat ATK und DEF der obersten MONSTERkarte des eigenen Friedhofs " +
                 "(Echo of the Latest Loss). Ohne Monster im Friedhof gelten die eigenen Basiswerte.")]
        public bool passiveStatsFromGraveTop;

        [Tooltip("Dieses Monster greift mit seiner DEF statt seiner ATK an (He Who Leads With His Shoulder)")]
        public bool passiveAttacksWithDef;

        [Tooltip(">0: nach jedem Angriff verliert dieses Monster dauerhaft N DEF (Doorstop Made of Dragon Bone)")]
        public int passiveDefLossAfterAttack;

        [Tooltip("Ausrüstung: die an den TRÄGER angrenzenden Zonen gelten als versiegelt, solange sie " +
                 "leer sind (The Landlord's Own Padlock)")]
        public bool passiveSealsAdjacentZones;

        [Tooltip("Ausrüstung: der Träger kann nicht in eine andere Zone ziehen (The Landlord's Own Padlock)")]
        public bool passiveBearerZoneLocked;

        [Tooltip(">0: solange die LP beider Spieler höchstens N auseinanderliegen, kann diese Karte " +
                 "nicht von gegnerischen Effekten als Ziel gewählt werden (The Even Scales)")]
        public int passiveUntargetableWhileLpClose;

        [Tooltip("Player-Artefakt: in der eigenen Draw Phase DARF der Besitzer statt zu ziehen die " +
                 "oberste Karte seines Friedhofs auf die Hand nehmen (The Standing Order)")]
        public bool passiveDrawReplacementGraveTop;

        [Tooltip(">0: kommt mit N Countdown-Markern aufs Feld; in jeder Standby Phase des Kontrolleurs " +
                 "wird einer entfernt — beim letzten feuert der CountdownZero-Effekt (The Appointed Hour)")]
        public int countdownMarkers;

        [Header("5 Archetypes (September 2026)")]
        [Tooltip("Giftwyrm: die Effekte dieser Karte werden immer ihrem BESITZER (OriginalOwner) " +
                 "angeboten und wirken für ihn — auch wenn der Gegner sie kontrolliert")]
        public bool passiveServesOriginalOwner;

        [Tooltip("Giftwyrm: kann nicht angreifen, solange sie von jemand anderem als ihrem " +
                 "Besitzer kontrolliert wird")]
        public bool passiveCannotAttackWhileDisloyal;

        [Tooltip("Giftwyrm Prettybow: solange offen auf dem Feld, kosten die Zauber ihres " +
                 "KONTROLLEURS 1 Mana mehr")]
        public bool passiveSpellTaxOnController;

        [Tooltip(">0: der erste Angriff des GEGNERS des Besitzers je Battle Phase kostet so viel " +
                 "Mana — automatisch abgezogen; ohne Mana kein Angriff (Waylay Tollgate)")]
        public int passiveAttackToll;

        [Tooltip(">0 DEKRET: der erste Angriff JEDES Spielers je Battle Phase kostet so viel Mana " +
                 "(Bylaw: Quiet Hours)")]
        public int passiveAttackTaxBoth;

        [Tooltip("DEKRET: jede gezogene Karte beider Spieler wird offen vorgezeigt (Bylaw: Show of Hands)")]
        public bool passiveDrawRevealBoth;

        [Tooltip(">0 DEKRET: jeder Spieler kann höchstens so viele Monster kontrollieren " +
                 "(Bylaw: Standing Room Only)")]
        public int passiveMonsterCapBoth;

        [Tooltip("Karten des Besitzers, deren Name diesen Text enthält, können nicht durch " +
                 "Karteneffekte zerstört werden (Bylaw Chairwoman: \"Bylaw:\")")]
        public string protectsNamedFromEffectDestroy = "";

        [Tooltip("Solange offen auf dem Feld: die \"Bylaw:\"-Dekrete des Besitzers gelten nicht " +
                 "mehr für ihn — nur noch für den Gegner (Letter of the Law)")]
        public bool passiveDecreesSpareOwner;

        [Tooltip(">0: aktiviert der NICHT-Besitzer einen Effekt dieser Karte (eitherPlayerMayActivate), " +
                 "hat der Besitzer im nächsten Zug so viel Mana zusätzlich (Splithoof Grinning Ledger)")]
        public int passiveOwnerRoyaltyManaNextTurn;

        [Header("Welle 3: 50 Generics (September 2026)")]
        [Tooltip("Countdown: beim Nullschlag bleibt die Karte auf dem Feld statt zerstört zu " +
                 "werden (Borrowed Hourglass)")]
        public bool countdownZeroKeepsCard;

        [Tooltip(">0: dieses Monster erhält so viel DEF, während es angegriffen wird (Shield Wall Doctrine)")]
        public int passiveDefWhileDefending;

        [Tooltip("Solange offen auf dem Feld: Karten, die der GEGNER des Besitzers millt, werden " +
                 "stattdessen verbannt (Baron of the Undertow)")]
        public bool passiveOpponentMillsBanished;

        [Tooltip("Diese Karte kann nicht verbannt werden (The Unforgotten)")]
        public bool passiveCannotBeBanished;

        [Tooltip(">0 DEKRET: jeder Spieler kann höchstens so viele Monster PRO ZUG beschwören — " +
                 "Normal- und Spezialbeschwörungen zusammen (Closing Time)")]
        public int passiveSummonCapBoth;

        [Tooltip("Standby-Countdowns des Besitzers ticken doppelt (Reliquary: The Eleventh Hour)")]
        public bool passiveCountdownsTickTwice;

        [Tooltip("Eigene VERDECKTE Monster können nicht durch Karteneffekte zerstört werden " +
                 "(Reliquary: The Last Bow)")]
        public bool passiveProtectFaceDownFromEffectDestroy;

        [Tooltip("Der erste Angriff des GEGNERS je Zug stellt ihn vor die Wahl: oberste Deckkarte " +
                 "ins Grab oder der Angriff wird abgebrochen (The Long Detour)")]
        public bool passiveFirstEnemyAttackDetourDeal;

        [Header("Incarnates (September 2026)")]
        [Tooltip(">0: Monster, die mit dieser Karte im Kampf waren (angreifend oder angegriffen), " +
                 "verlieren nach dem Kampf dauerhaft so viel ATK (Maw of the First Winter)")]
        public int passiveDebuffOpponentAfterCombat;

        [Tooltip("Solange offen auf dem Feld: der GEGNER des Besitzers kann keine Monster aus " +
                 "seinem Friedhof beschwören (The Hungering Demon)")]
        public bool passiveOpponentNoGraveSummons;

        [Tooltip("Solange offen auf dem Feld: KEIN Spieler kann Zauber aktivieren (Colossus of the Broken Gate)")]
        public bool passiveNoSpellsBoth;

        [Tooltip(">0: aktiviert der GEGNER des Besitzers einen Artefakt-Effekt, erhält diese Karte " +
                 "dauerhaft so viel ATK und DEF (Colossus of the Broken Gate)")]
        public int passiveGrowOnEnemyArtifactActivation;

        [Tooltip("Würde diese Karte zerstört, darf der Besitzer stattdessen 1 Handkarte abwerfen " +
                 "(She Who Outlives)")]
        public bool passiveDiscardToSurvive;

        [Tooltip("Incarnate: betritt das Feld mit den AUFSUMMIERTEN gedruckten ATK/DEF aller für " +
                 "seine Beschwörung geopferten Monster als Basiswerte (Avatar of the Thousandth Card)")]
        public bool passiveBaseStatsFromOffering;

        [Tooltip("Solange offen auf dem Feld: der Gegner kann keine Effekte als Reaktion auf " +
                 "MONSTER-Effekte des Besitzers aktivieren (Avatar of the Thousandth Card)")]
        public bool passiveNoResponseToOwnerMonsterEffects;

        [Tooltip("DEKRET: KEIN Spieler kann Monster spezialbeschwören — außer Incarnate-Beschwörungen " +
                 "(Sworn to the Gate)")]
        public bool passiveNoSpecialSummonsBothExceptIncarnates;

        public abstract CardKind Kind { get; }

        public abstract Color FrameColor { get; }

        /// <summary>
        /// Gehört diese Karte ins EXTRA DECK statt ins Hauptdeck? Reliquaries und
        /// Incarnates — die zentrale Wahrheit für Deck-Editor, Duel-Setup und UI.
        /// </summary>
        public bool IsExtraDeckCard => this is ReliquaryCardData || this is IncarnateCardData;

        /// <summary>Anzeige-Farbe der Seltenheit (grau/grün/blau/gold).</summary>
        public static Color RarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return new Color(0.45f, 0.85f, 0.45f);
                case CardRarity.Rare: return new Color(0.40f, 0.65f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.72f, 0.20f);
                default: return new Color(0.80f, 0.80f, 0.85f);
            }
        }

        public static string RarityName(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return "Uncommon";
                case CardRarity.Rare: return "Rare";
                case CardRarity.Legendary: return "Legendary";
                default: return "Common";
            }
        }

        /// <summary>
        /// Menschenlesbare Zeilen für alle dauerhaften Passiv-Fähigkeiten dieser Karte.
        /// Die UI (Kartenpanel, Detail-Ansicht) hängt sie als PASSIVE-Block vor die
        /// Effektliste — Daten-Felder wie die Aura hätten sonst keinen Kartentext.
        /// </summary>
        public virtual List<string> BuildPassiveLines()
        {
            var lines = new List<string>();

            if (auraAtkBonus != 0 || auraDefBonus != 0)
            {
                string scope = auraOnlyFaceDown ? Loc.T("face-down ") : "";
                string named = string.IsNullOrEmpty(auraNameFilter) ? "" : $" \"{Loc.CardName(auraNameFilter)}\"";
                string typed = auraUseTypeFilter ? " " + Loc.T(auraTypeFilter.ToString().ToUpperInvariant()) : "";
                string leveled = auraLevelFilter > 0 ? Loc.F(" Level {0}", auraLevelFilter) : "";
                string other = auraExcludesSelf ? Loc.T("other ") : "";
                string bonus = auraAtkBonus != 0 && auraDefBonus != 0
                    ? Loc.F("{0} ATK and {1} DEF", Signed(auraAtkBonus), Signed(auraDefBonus))
                    : auraAtkBonus != 0 ? Loc.F("{0} ATK", Signed(auraAtkBonus)) : Loc.F("{0} DEF", Signed(auraDefBonus));
                lines.Add(Loc.F("Your {0}{1}{2}{3}{4} monsters gain {5}.", other, scope, named, typed, leveled, bonus).Replace("  ", " "));
            }

            if (passiveAtkPerCount > 0)
                lines.Add(Loc.F("This card gains {0} ATK for each of {1}.", passiveAtkPerCount, CountName(passiveAtkPerCountKind)));
            if (passiveDefPerCount > 0)
                lines.Add(Loc.F("This card gains {0} DEF for each of {1}.", passiveDefPerCount, CountName(passiveDefPerCountKind)));
            if (passiveCannotAttack)
                lines.Add(Loc.T("This card cannot attack."));
            if (passiveNoAttackOnSummonTurn)
                lines.Add(Loc.T("This card cannot attack during the turn it is Summoned."));
            if (passiveNoDirectAttackOnSummonTurn)
                lines.Add(Loc.T("This card cannot attack directly during the turn it is Summoned."));
            if (passiveTaunt)
                lines.Add(Loc.T("Your opponent's attacks must target this card."));
            if (battleShieldMinOwnArtifacts > 0)
                lines.Add(Loc.F("While you control {0}+ Artifacts, this card cannot be destroyed by battle.", battleShieldMinOwnArtifacts));
            if (tributeWorth > 1)
                lines.Add(Loc.F("Counts as {0} tributes for a Reliquary Summon.", tributeWorth));
            if (!string.IsNullOrEmpty(protectsNamedFromTargeting))
                lines.Add(Loc.F("Your other \"{0}\" cards cannot be targeted by your opponent's effects.", protectsNamedFromTargeting));
            if (conditionalDoubleAttack)
                lines.Add(Loc.F("Can attack twice each Battle Phase while you control another face-up {0} monster.", Loc.T(doubleAttackAttribute.ToString().ToUpperInvariant())));
            if (fieldLimitCount > 0 && !string.IsNullOrEmpty(fieldLimitName))
                lines.Add(Loc.F("You cannot Summon or Set this card while you control {0} \"{1}\" monsters.", fieldLimitCount, fieldLimitName));
            if (passiveBurnPerMill > 0)
                lines.Add(Loc.F("Every time you mill 1 or more cards: deal {0} damage to your opponent.", passiveBurnPerMill));
            if (passiveBlockReliquarySummons)
                lines.Add(Loc.T("While this card is on the field, neither player can Special Summon Reliquaries."));
            if (passiveNoBattleDestroy)
                lines.Add(Loc.T("This card cannot be destroyed by battle."));
            if (passiveUntargetable)
                lines.Add(Loc.T("This card cannot be targeted by your opponent's effects."));
            if (passiveNoBattleDamageInvolving)
                lines.Add(Loc.T("Neither player takes battle damage from battles involving this card."));
            if (passiveRedirectBattleDamage)
                lines.Add(Loc.T("Your opponent takes all battle damage you would take instead."));
            if (passiveDeathCounterLimit > 0)
                lines.Add(Loc.F("During each End Phase: put a Death Counter on this card. With {0} Death Counters, it is sent to the Graveyard.", passiveDeathCounterLimit));
            if (passiveOpponentMillAmplify > 0)
                lines.Add(Loc.F("Every time your opponent sends cards from their Deck to the Graveyard (except by this effect): they send {0} more.", passiveOpponentMillAmplify));
            if (passiveProtectAllFromTargetingAndAttacks)
                lines.Add(Loc.T("While this card is on the field, your cards cannot be targeted by your opponent's effects or attacks."));
            if (passiveEndPhaseLpToll > 0)
                lines.Add(Loc.F("During each End Phase: pay {0} LP or destroy this card.", passiveEndPhaseLpToll));
            if (passiveOpponentMustSetSpells)
                lines.Add(Loc.T("While this card is face-up on the field, your opponent must Set Spells before activating them."));

            // --- The Small Print ---
            if (auraAdjacentOnly && (auraAtkBonus != 0 || auraDefBonus != 0))
                lines.Add(Loc.T("(The aura above applies only to monsters adjacent to this card.)"));
            if (auraAloneOnly && auraAtkBonus != 0)
                lines.Add(Loc.T("(The aura above applies only to your monsters with no adjacent monster.)"));
            if (auraCrowdedAtkPenalty > 0)
                lines.Add(Loc.F("Your monsters with an adjacent monster lose {0} ATK.", auraCrowdedAtkPenalty));
            if (facingAtkPenalty > 0)
                lines.Add(Loc.F("The monster facing this card loses {0} ATK.", facingAtkPenalty));
            if (passiveAdjacentNoEffectDestroy)
                lines.Add(Loc.T("Monsters adjacent to this card cannot be destroyed by card effects."));
            if (passiveAdjacentNoBattleDestroy)
                lines.Add(Loc.T("Monsters adjacent to this card cannot be destroyed by battle."));
            if (passiveAdjacentDebuffOnDestroy > 0)
                lines.Add(Loc.F("When this card is destroyed: the monsters adjacent to it permanently lose {0} ATK and DEF.", passiveAdjacentDebuffOnDestroy));
            if (passiveNoEffectDestroy)
                lines.Add(Loc.T("This card cannot be destroyed by card effects."));
            if (passiveCannotBeTributed)
                lines.Add(Loc.T("This card cannot be Tributed."));
            if (passiveCannotChangePosition)
                lines.Add(Loc.T("This card cannot change its battle position."));
            if (passiveNoAttackAfterPositionChange)
                lines.Add(Loc.T("If this card changed its battle position this turn, it cannot attack."));
            if (passiveNoNormalSummon)
                lines.Add(Loc.T("Cannot be Normal Summoned or Set."));
            if (passiveControllerStandbyLpLoss > 0)
                lines.Add(Loc.F("During each of its controller's Standby Phases: its controller loses {0} LP.", passiveControllerStandbyLpLoss));
            if (passiveOwnerNoOtherSpecialSummons)
                lines.Add(Loc.T("While this card is on the field, you cannot Special Summon other monsters."));
            if (passiveLoneImmunity)
                lines.Add(Loc.T("While you control no other monsters, this card cannot be destroyed by battle or by card effects."));
            if (passiveLowHandImmunity)
                lines.Add(Loc.T("While you have 1 or fewer cards in hand, this card cannot be targeted and cannot be destroyed by card effects."));
            if (passivePiercing)
                lines.Add(Loc.T("Piercing: when this card attacks a Defense Position monster with lower DEF, the difference is dealt as battle damage."));
            if (passiveBearerPiercing)
                lines.Add(Loc.T("The equipped monster inflicts piercing battle damage."));
            if (passiveBreakOnFailedPierce)
                lines.Add(Loc.T("If the equipped monster attacks a Defense Position monster and does not destroy it, destroy this card."));
            if (passiveDirectAttackHalved)
                lines.Add(Loc.T("This card can attack directly even if your opponent controls monsters; battle damage from its direct attacks is halved."));
            if (passiveNoDirectAttack)
                lines.Add(Loc.T("This card cannot attack directly."));
            if (passiveSpellTaxBoth)
                lines.Add(Loc.T("Spells cost 1 more Mana for both players. During your End Phase, if you activated a Spell this turn, destroy this card."));
            if (passiveOneAttackBonus > 0)
                lines.Add(Loc.F("Each player may declare only one attack per Battle Phase. Attacking monsters gain {0} ATK during the battle.", passiveOneAttackBonus));
            if (passiveStandbyBonusMana > 0)
                lines.Add(Loc.F("During your Standby Phase: gain {0} additional Mana this turn.", passiveStandbyBonusMana));
            if (passiveHandCapForSurvival > 0)
                lines.Add(Loc.F("During your End Phase, if you hold more than {0} cards, destroy this card.", passiveHandCapForSurvival));
            if (passiveDestroyWhenLifeAtMost > 0)
                lines.Add(Loc.F("When your LP are {0} or less, this card is destroyed.", passiveDestroyWhenLifeAtMost));
            if (passiveLifeCostsFree)
                lines.Add(Loc.T("LP costs you pay are reduced to 0."));
            if (passiveCoinChoose)
                lines.Add(Loc.T("Once per turn, when you flip a coin, flip it twice and choose which result counts. If both land Tails, destroy this card."));
            if (passiveTailsAsHeadsWhenBehind)
                lines.Add(Loc.T("While your LP are lower than your opponent's, your coin flips that land Tails count as Heads."));
            if (passiveLienAtkPenalty > 0)
                lines.Add(Loc.F("Monsters with a Lien lose {0} ATK.", passiveLienAtkPenalty));
            if (passiveStolenAtkBonus > 0)
                lines.Add(Loc.F("Monsters you control but do not own gain {0} ATK.", passiveStolenAtkBonus));

            // --- Road to 1000 ---
            if (passiveStatsFromGraveTop)
                lines.Add(Loc.T("This card's ATK and DEF equal those of the top monster card of your Graveyard."));
            if (passiveAttacksWithDef)
                lines.Add(Loc.T("This card attacks using its DEF."));
            if (passiveDefLossAfterAttack > 0)
                lines.Add(Loc.F("After this card attacks, it permanently loses {0} DEF.", passiveDefLossAfterAttack));
            if (passiveSealsAdjacentZones)
                lines.Add(Loc.T("The zones adjacent to the equipped monster count as Sealed while they are empty."));
            if (passiveBearerZoneLocked)
                lines.Add(Loc.T("The equipped monster cannot move to another zone."));
            if (passiveUntargetableWhileLpClose > 0)
                lines.Add(Loc.F("While both players' LP are within {0} of each other, this card cannot be targeted by your opponent's effects.", passiveUntargetableWhileLpClose));
            if (passiveDrawReplacementGraveTop)
                lines.Add(Loc.T("During your Draw Phase, you may add the top card of your Graveyard to your hand instead of drawing."));
            if (countdownMarkers > 0)
                lines.Add(Loc.F("Enters the field with {0} Hour Counters. During each of your Standby Phases: remove 1. When the last one is removed, its Countdown effect fires and this card is destroyed.", countdownMarkers));

            // --- 5 Archetypes ---
            if (passiveServesOriginalOwner)
                lines.Add(Loc.T("This card's effects always belong to its owner — even while the opponent controls it."));
            if (passiveCannotAttackWhileDisloyal)
                lines.Add(Loc.T("Cannot attack while controlled by anyone but its owner."));
            if (passiveSpellTaxOnController)
                lines.Add(Loc.T("Spells cost its controller 1 more Mana."));
            if (passiveAttackToll > 0)
                lines.Add(Loc.F("Your opponent's first attack each Battle Phase costs them {0} Mana — without it, the attack is not allowed.", passiveAttackToll));
            if (passiveAttackTaxBoth > 0)
                lines.Add(Loc.F("DECREE: Each player's first attack per Battle Phase costs {0} Mana.", passiveAttackTaxBoth));
            if (passiveDrawRevealBoth)
                lines.Add(Loc.T("DECREE: Every card either player draws is revealed."));
            if (passiveMonsterCapBoth > 0)
                lines.Add(Loc.F("DECREE: Each player can control at most {0} monsters.", passiveMonsterCapBoth));
            if (!string.IsNullOrEmpty(protectsNamedFromEffectDestroy))
                lines.Add(Loc.F("Your \"{0}\" cards cannot be destroyed by card effects.", protectsNamedFromEffectDestroy));
            if (passiveDecreesSpareOwner)
                lines.Add(Loc.T("Your \"Bylaw:\" Decrees no longer apply to you — only to your opponent."));
            if (passiveOwnerRoyaltyManaNextTurn > 0)
                lines.Add(Loc.F("If your opponent activates this card's effect: you have {0} additional Mana next turn.", passiveOwnerRoyaltyManaNextTurn));

            // --- Welle 3: 50 Generics ---
            if (countdownZeroKeepsCard)
                lines.Add(Loc.T("When its Countdown strikes zero, this card stays on the field."));
            if (passiveDefWhileDefending > 0)
                lines.Add(Loc.F("This card gains {0} DEF while it is being attacked.", passiveDefWhileDefending));
            if (passiveOpponentMillsBanished)
                lines.Add(Loc.T("Cards your opponent mills are banished instead."));
            if (passiveCannotBeBanished)
                lines.Add(Loc.T("This card cannot be banished."));
            if (passiveSummonCapBoth > 0)
                lines.Add(Loc.F("Each player can Summon at most {0} monster(s) per turn.", passiveSummonCapBoth));
            if (passiveCountdownsTickTwice)
                lines.Add(Loc.T("During your Standby Phase, your Countdowns tick twice."));
            if (passiveProtectFaceDownFromEffectDestroy)
                lines.Add(Loc.T("Your face-down monsters cannot be destroyed by card effects."));
            if (passiveFirstEnemyAttackDetourDeal)
                lines.Add(Loc.T("When your opponent declares their first attack each turn, they choose: send the top card of their Deck to the Graveyard, or the attack is cancelled."));

            // --- Incarnates ---
            if (passiveDebuffOpponentAfterCombat > 0)
                lines.Add(Loc.F("Monsters that battle this card permanently lose {0} ATK after the battle.", passiveDebuffOpponentAfterCombat));
            if (passiveOpponentNoGraveSummons)
                lines.Add(Loc.T("Your opponent cannot Summon monsters from their Graveyard."));
            if (passiveNoSpellsBoth)
                lines.Add(Loc.T("While this card is on the field, neither player can activate Spells."));
            if (passiveGrowOnEnemyArtifactActivation > 0)
                lines.Add(Loc.F("Each time your opponent activates an Artifact effect, this card permanently gains {0} ATK and DEF.", passiveGrowOnEnemyArtifactActivation));
            if (passiveDiscardToSurvive)
                lines.Add(Loc.T("If this card would be destroyed, you may discard 1 card from your hand instead."));
            if (passiveBaseStatsFromOffering)
                lines.Add(Loc.T("This card enters the field with the combined printed ATK and DEF of every monster sacrificed for its Summon as its base stats."));
            if (passiveNoResponseToOwnerMonsterEffects)
                lines.Add(Loc.T("Your opponent cannot activate effects in response to your monster effects."));
            if (passiveNoSpecialSummonsBothExceptIncarnates)
                lines.Add(Loc.T("Neither player can Special Summon monsters, except Incarnate Summons."));

            return lines;
        }

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();

        /// <summary>Zählbasis als Kartentext ("each of your Artifacts on the field").</summary>
        protected static string CountName(EffectCountKind kind)
        {
            switch (kind)
            {
                case EffectCountKind.OwnArtifactsOnField: return Loc.T("your Artifacts on the field");
                case EffectCountKind.OwnGraveyardArtifacts: return Loc.T("the Artifacts in your Graveyard");
                case EffectCountKind.OwnFaceDownMonsters: return Loc.T("your face-down monsters");
                case EffectCountKind.OwnBanishedMonsters: return Loc.T("your banished monsters");
                case EffectCountKind.OwnGraveyardCards: return Loc.T("the cards in your Graveyard");
                case EffectCountKind.EquippedArtifactsOnSelf: return Loc.T("its equipped Artifacts");
                case EffectCountKind.OpponentFaceDownMonsters: return Loc.T("your opponent's face-down monsters");
                case EffectCountKind.OpponentIllusionTokens: return Loc.T("the Illusion Tokens your opponent controls");
                case EffectCountKind.OwnHandCards: return Loc.T("the cards in your hand");
                case EffectCountKind.OwnGraveyardSpells: return Loc.T("the Spells in your Graveyard");
                case EffectCountKind.OwnDistinctLevels: return Loc.T("the different Levels among your monsters");
                case EffectCountKind.OwnMonstersOnOpponentField: return Loc.T("your monsters on your opponent's field");
                case EffectCountKind.AllArtifactsOnField: return Loc.T("the Artifacts on both fields");
                case EffectCountKind.OwnBanishedCards: return Loc.T("your banished cards");
                default: return Loc.T("your monsters on the field");
            }
        }

        /// <summary>Setzt den vollständigen Regeltext der Karte aus den Effekten zusammen.</summary>
        public virtual string BuildRulesText()
        {
            var sb = new StringBuilder();
            foreach (var effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.text)) continue;
                if (sb.Length > 0) sb.AppendLine();
                string infusedName = effect.infusedKind == InfusedKind.Coupled ? "Or Infused" : "Infused";
                string prefix = effect.isInfused
                    ? "[" + infusedName + (effect.manaCost > 0 ? " – " + effect.manaCost + " Mana" : "") + "] "
                    : (effect.manaCost > 0 ? "[" + effect.manaCost + " Mana] " : "");
                sb.Append(prefix).Append(effect.text);
            }
            return sb.ToString();
        }
    }
}
