namespace Rouge.Tcg
{
    public enum CardKind { Monster, Spell, Artifact, Player }

    public enum CardRarity { Common, Uncommon, Rare, Legendary }

    public enum SpellSpeed
    {
        Normal, // nur in der eigenen Main Phase aktivierbar
        Quick   // auch als Reaktion / im Gegnerzug aktivierbar (aus der Hand nur im eigenen Zug)
    }

    public enum ArtifactSlot { Monster, Player, Field }

    public enum BattlePosition { Attack, Defense }

    /// <summary>Element-Attribut eines Monsters.</summary>
    public enum MonsterAttribute { Light, Dark, Water, Fire, Wind, Earth }

    /// <summary>Typ/Rasse eines Monsters.</summary>
    public enum MonsterType { Dragon, Human, Animal, Beast, Mecha, Demon, Angel, Myth }

    public enum DuelPhase { Draw, Standby, Main, Battle, End }

    public enum ZoneType { Deck, Hand, MonsterZone, SpellZone, ArtifactZone, PlayerZone, Graveyard, Banished, ExtraDeck }

    public enum EffectTrigger
    {
        Ignition,         // manuell in der eigenen Main Phase aktivierbar
        Quick,            // manuell, wann immer ein Reaktionsfenster offen ist
        OnActivate,       // beim Ausspielen der Zauber-/Artefaktkarte
        OnSummonSelf,     // wenn diese Karte beschworen wird
        OnDestroyedSelf,  // wenn diese Karte zerstört wird
        OnOpponentSummon, // wenn der Gegner ein Monster beschwört (Reaktion)
        StandbyPhase,     // Standby Phase des eigenen Zuges
        EndPhase,         // End Phase des eigenen Zuges
        OnNormalSummonSelf, // wenn diese Karte als Normalbeschwörung beschworen wird
        HandIgnition,     // aus der eigenen Hand in der Main Phase aktivierbar (Monster-Effekt)
        GraveyardIgnition, // aus dem eigenen Friedhof in der Main Phase aktivierbar
        OnFlipFaceUp,     // wenn diese verdeckte Karte aufgedeckt wird (Flip-Effekt)

        // Nur anhängen, nie dazwischen: Karten-Assets speichern den Zahlenwert.
        HandQuick,        // aus der eigenen Hand, wann immer ein Reaktionsfenster offen ist

        // --- Batch August 2026 ---
        OnTributedSelf,        // wenn DIESE Karte als Tribut gezahlt wird (Willing Lamb)
        OnOwnMonsterTributed,  // wenn irgendein eigenes Monster getributet wird (Blood Dividend)
        OnOwnMonsterBounced,   // wenn ein eigenes Monster vom Feld auf die Hand zurückkehrt (Nest Egg)
        OnEnemyCardBounced,    // wenn eine gegnerische Feldkarte auf die Hand zurückkehrt (Finders Keepers)
        OnBearerBattleKill,    // auf Karte/Ausrüstung: der (Träger) zerstört ein Monster im Kampf (Extra Reach)
        OnOwnMonsterDestroyed, // wenn irgendein eigenes Monster zerstört wird (Warm Memories)
        OnOpponentDraw,        // wenn der Gegner AUSSERHALB seiner Draw Phase zieht (Redactor; nur Standby/Main)

        // --- Deckay (Mill-Archetype) ---
        EitherEndPhase,        // End Phase JEDES Zuges (eigener und gegnerischer)
        OpponentEndPhase,      // End Phase des GEGNERISCHEN Zuges
        OnMilledSelf,          // DIESE Karte wandert vom DECK in den Friedhof (gemillt)
        OnDiscardedOrMilledSelf, // DIESE Karte wandert aus HAND oder DECK in den Friedhof
        OnSentToGraveyardSelf, // DIESE Karte landet im Friedhof — egal woher (Feld/Hand/Deck)

        // --- Slowburn (Charged-Spells) ---
        ChargedStandby,        // GELADENE Version eines gesetzten Spells: zündet automatisch in der
                               // eigenen Standby Phase, wenn die Karte VOR diesem Zug gesetzt wurde

        // --- 6er-Welle (August 2026) — NUR ANHÄNGEN, Assets speichern Zahlenwerte ---
        OnOwnArtifactDestroyed, // wenn irgendein eigenes Artefakt zerstört wird (Failsafe Dead Man's Switch)
        OnOwnMonsterFlipped,    // wenn irgendein eigenes Monster aufgedeckt wird (Lyria Orchestra Pit)

        // --- The Small Print (August 2026) ---
        OnPositionChangedSelf,  // DIESE Karte wechselt offen die Kampfposition (Volte-Face)
        OnMovedSelf,            // DIESE Karte ist in eine andere Zone gezogen (Left Hand of the Hangman)

        // --- Road to 1000 (September 2026) ---
        CountdownZero           // der letzte Countdown-Marker dieser Karte wurde entfernt (The Appointed Hour)
    }

    public enum EffectActionType
    {
        DamageOpponent,             // Schaden an gegnerische LP
        HealSelf,                   // eigene LP heilen
        DrawCards,                  // Karten ziehen
        GainMana,                   // Mana in diesem Zug erhalten
        DestroyTargetMonster,       // Zielmonster zerstören
        BanishTargetMonster,        // Zielmonster verbannen
        BuffTargetAtk,              // Ziel-ATK dauerhaft erhöhen
        BuffTargetAtkUntilEndOfTurn,// Ziel-ATK bis Zugende erhöhen
        DebuffTargetAtk,            // Ziel-ATK dauerhaft senken
        SpecialSummonFromGraveyard, // Monster aus eigenem Friedhof beschwören
        ReturnTargetToHand,         // Zielmonster auf die Hand zurück
        BuffTargetDef,              // Ziel-DEF dauerhaft erhöhen
        DebuffTargetDef,            // Ziel-DEF dauerhaft senken
        DamageBothPlayers,          // beide Spieler erleiden Schaden
        DiscardOpponentRandom,      // Gegner wirft zufällige Handkarte(n) ab
        DrainOpponentMana,          // Gegner verliert Mana
        ReturnFromGraveyardToHand,  // Karte aus dem eigenen Friedhof auf die Hand
        AddTargetFromDeckToHand,    // Ziel aus dem eigenen Deck auf die Hand (Suche); danach wird gemischt
        SpecialSummonTargetFromHand,// Ziel aus der eigenen Hand als Spezialbeschwörung
        TributeSelfSpecialSummonTarget, // Quellkarte in den Friedhof schicken, dann Ziel spezialbeschwören
        GrantAdditionalAttack,      // Zielmonster darf in dieser Battle Phase erneut angreifen
        BanishTarget,               // Zielkarte verbannen (auch aus dem Friedhof)
        SpecialSummonTargetFromBanished, // verbanntes Ziel als Spezialbeschwörung aufs eigene Feld
        SetTargetSpellFromDeck,     // Ziel-Zauber aus dem Deck verdeckt setzen (sofort aktivierbar)
        SendSelfToGraveyard,        // Quellkarte in den Friedhof schicken (auch aus der Hand)
        DestroyAllMonstersExceptType, // alle Monster beider Felder zerstören, außer dem Filter-Typ
        SetTargetFaceDownDefense,   // Zielmonster verdeckt in Verteidigungsposition legen
        BanishSelf,                 // Quellkarte verbannen (z.B. aus dem Friedhof als Kosten)
        SpecialSummonTargetFromGraveOrBanish, // Ziel aus Friedhof ODER Verbannung spezialbeschwören
        OpponentRandomToFieldOrDiscard, // zufällige gegnerische Handkarte: Monster -> auf DEIN Feld, sonst Friedhof
        BuffTargetDefUntilEndOfTurn, // Ziel-DEF bis Zugende erhöhen
        SpecialSummonTargetFromHandOrGrave, // Ziel aus Hand ODER Friedhof spezialbeschwören (ohne Selbst-Tribut)
        PurgeTargetBuffs, // alle temporären UND permanenten ATK/DEF-Modifikationen des Ziels entfernen
        EquipTargetArtifactToSelf,  // Ziel-Artefakt (Hand/Deck/Friedhof) an die Quellkarte anlegen
        FlipTargetFaceUp,           // verdecktes Zielmonster aufdecken (löst Flip-Effekte aus)
        SpecialSummonTargetFaceDown,// Ziel aus Hand/Deck verdeckt in Verteidigung spezialbeschwören
        SetTargetArtifactFromDeck,  // Artefakt aus dem Deck ins Artefakt-Feld legen
        NegateTargetCard,           // Zielkarte auf dem Feld: ihre Effekte sind bis Zugende annulliert
        ProtectSelfThisTurn,        // Quellkarte kann diesen Zug nicht zerstört werden
        SpecialSummonTargetFromDeck, // Ziel aus dem eigenen Deck spezialbeschwören

        // --- ab hier neu; NUR ANHÄNGEN, nie dazwischen einfügen: die Karten-Assets
        //     speichern den Zahlenwert, ein Verschieben würde alle Effekte vertauschen ---
        MillSelf,                   // oberste N Karten des eigenen Decks in den Friedhof
        MillOpponent,               // oberste N Karten des Gegnerdecks in dessen Friedhof
        ShuffleTargetIntoDeck,      // Zielkarte ins Deck ihres Besitzers mischen
        ShuffleGraveyardIntoDeck,   // N Karten aus dem eigenen Friedhof zurück ins Deck
        CannotAttackThisTurn,       // Ziel kann diesen Zug nicht angreifen
        LockPositionThisTurn,       // Ziel kann diesen Zug die Position nicht wechseln
        CannotBeTargetedThisTurn,   // Ziel ist diesen Zug kein gültiges Ziel für den Gegner
        SwapAtkDefThisTurn,         // Ziel tauscht bis Zugende ATK und DEF
        TauntThisTurn,              // Gegner muss diesen Zug die Quellkarte angreifen
        PreventBattleDamageThisTurn,// Aktivierender Spieler erleidet diesen Zug keinen Kampfschaden
        ExtraNormalSummon,          // eine zusätzliche Normalbeschwörung in diesem Zug
        OpponentSummonLockThisTurn, // Gegner kann diesen Zug nicht spezialbeschwören
        DiscardFromHandCost,        // N Handkarten abwerfen (als Kosten gedacht)
        LookAndDiscardChosen,       // Gegnerhand ansehen, 1 Karte wählen, die abgeworfen wird
        CopyTargetStatsThisTurn,    // ATK/DEF der Quellkarte werden bis Zugende die des Ziels
        TakeControlUntilEndOfTurn,  // Kontrolle über ein gegnerisches Monster bis zur End Phase
        SummonCopyOfTarget,         // Kopie des Ziels auf das eigene Feld bis zur End Phase

        // Mana über die Rundengrenze. DrainOpponentMana trifft nur den aktuellen
        // Vorrat und ist im eigenen Zug wirkungslos — der Gegner füllt zu
        // Zugbeginn ohnehin auf. Diese beiden wirken auf das nächste Auffüllen.
        DrainOpponentManaNextTurn,  // dem Gegner fehlt im nächsten Zug so viel Mana
        GainManaNextTurn,           // du hast in deinem nächsten Zug so viel Mana mehr

        // --- Batch August 2026 ---
        ReturnTargetCardToHand,     // beliebige Feldkarte (Monster/Zauber/Artefakt) auf die Hand des Besitzers
        ProtectTargetThisTurn,      // Ziel kann diesen Zug nicht zerstört werden
        SwitchTargetToDefense,      // Zielmonster in Verteidigungsposition drehen
        SwitchAllToDefense,         // amount 0 = alle Monster beider Felder, 1 = nur gegnerische
        DrawUntilMatchOpponentHand, // ziehen bis Handkarten-Gleichstand (amount = Obergrenze)
        ReturnSelfFromGraveToHand,  // Quellkarte aus dem Friedhof auf die Hand (Bad Penny)
        SpecialSummonTargetFromGraveFaceDown, // Ziel aus dem eigenen Friedhof verdeckt beschwören
        MillAndSalvage,             // amount Karten millen; Treffer (nameFilter) bis targetCount auf die Hand
        BuffSelfAtkPerCount,        // Quellkarte: +amount ATK dauerhaft pro gezählter Karte (countKind)
        BuffSelfDefPerCount,        // Quellkarte: +amount DEF dauerhaft pro gezählter Karte (countKind)
        RevealTopMayBottom,         // oberste Deckkarte zeigen; Aktivierender darf sie nach unten legen
        ReturnBanishedToGraveyard,  // verbannte Zielkarte(n) zurück in den Friedhof ihres Besitzers (Gravemaw)
        PlaceTargetArtifactFromGraveyard, // Ziel-Artefakt aus dem eigenen Friedhof in die Artefakt-Zone legen
        MoveTargetArtifactToStrongestMonster, // Ziel-Artefakt ans eigene Monster mit höchstem ATK; amount>0 = EOT-ATK-Bonus für den neuen Träger
        SendTargetFromDeckToGraveyard, // Zielkarte aus dem eigenen Deck in den Friedhof (Foolish Burial)
        BuffTargetAtkPerCountEot,   // Ziel: +amount ATK bis Zugende pro gezählter Karte (countKind)
        BuffTargetAtkPerCountPermanent, // Ziel: +amount ATK dauerhaft pro gezählter Karte (countKind)
        OpponentDraws,              // der GEGNER zieht amount Karten (Redactor Mandatory Reading)
        DestroyAllEnemyAttackMonsters, // alle Monster des Gegners in Angriffsposition zerstören (Row of Teeth)
        DestroyTargetAndSameLevelDefense, // Ziel zerstören + alle DEF-Monster gleichen Levels beider Felder (Warm Welcome)
        SetTargetSpellFromHand,     // Ziel-Zauber aus der EIGENEN Hand verdeckt setzen (sofort aktivierbar; Trapline)

        // --- Deckay (Mill-Archetype) ---
        ImmuneTargetThisTurn,       // Ziel ist bis Zugende unberührbar für gegnerische Effekt-AKTIONEN (und kein Ziel)
        SummonReliquaryFromExtraSuppressed, // Reliquary aus dem Extra Deck ohne Bedingungen (Mana fällig), ohne On-Summon; stirbt in der eigenen End Phase
        DestroyAllOthersSelfDamagePer, // ALLE anderen Feldkarten zerstören; amount Selbstschaden je zerstörter Karte

        // --- Apocrypha (Chain-Negate) ---
        NegateRestOfChain,          // annulliert alle FRÜHEREN Glieder der laufenden Kette (Apocrypha, the Unwritten)
        AttackAgainSelf,            // Quellkarte darf diese Battle Phase erneut angreifen (Chimera Infused)

        // --- Gaslight (Illusion-Tokens) ---
        SummonIllusionTokensToOpponent,   // amount Illusion-Tokens (0/0, DEF) auf FREIE Gegner-Zonen; keine Summon-Trigger
        DestroyIllusionTokensDrawPer,     // bis zu amount gegnerische Illusion-Tokens zerstören; je 1 Karte ziehen (Cap: targetCount)
        DestroyAllIllusionTokensDebuffTargetPer, // ALLE Illusion-Tokens zerstören; Ziel-Monster verliert amount ATK je Token

        // --- Slowburn (Charged-Spells) ---
        DetonateChargedSpell,       // zündet SOFORT den Charged-Effekt eines eigenen gesetzten Spells (vor diesem Zug gesetzt)

        // --- Mimicrypt (Nachahmer) ---
        CopySpellFromOpponentGraveyard, // Ziel-Zauber im GEGNER-Friedhof: dessen ersten Effekt als eigenen auflösen
        AllyMonsterCopiesTargetStats,   // ein selbst gewähltes EIGENES Monster kopiert ATK/DEF des Ziels bis Zugende

        // --- Dark-Angel-Paket (August 2026) ---
        ForbidChosenNameTwoTurns,       // Spieler wählt per Suche einen Kartennamen; dessen Effekte sind diesen und den nächsten Zug gesperrt
        SkipOwnBattlePhaseNextTurn,     // der Aktivierende überspringt seine nächste Battle Phase
        BanishAllOpponentMonsters,      // alle gegnerischen Monster verbannen
        NoDirectAttacksThisTurnSelf,    // der Aktivierende darf diesen Zug nicht direkt angreifen
        BanishFromExtraDeckCost,        // Ziel aus dem EIGENEN Extra Deck verbannen (als Kosten gedacht)
        ReturnTargetReliquaryToExtraDeck, // gegnerisches Reliquary auf dem Feld kehrt ins Extra Deck zurück
        LockOpponentSpecialSummonedEffects, // Gegner kann diesen Zug keine Effekte spezialbeschworener Feldmonster aktivieren
        SwitchAllToAttack,              // alle offenen Monster beider Felder in Angriffsposition
        ReturnAllBanishedToOwners,      // JEDE verbannte Karte kehrt zurück: Reliquaries ins Extra Deck, Rest ins Deck
        SimultaneousDeckCull,           // Cull the Weak: beide decken je 1 Deck-Monster auf — schwächeres stirbt, stärkeres kommt (Besitzer nimmt Differenz als Schaden)
        PlaySelfFromHand,               // Emergency Barrier: die Quellkarte (Artefakt) wird aus der Hand aufs Feld gespielt
        SetTargetSpellFromGraveyard,    // Ziel-Zauber aus dem EIGENEN Friedhof verdeckt setzen (sofort aktivierbar)

        // --- The Small Print (August 2026) — NUR ANHÄNGEN ---
        FlipCoin,                       // Münze werfen; folgende Aktionen mit coinGate Heads/Tails laufen nur bei passendem Ergebnis
        PayLifePoints,                  // amount LP zahlen (als Kosten gedacht; Aurel setzt LP-Kosten auf 0)
        DrainSelfManaNextTurn,          // eigene Mana-Schuld: nächster Zug amount weniger; ungedeckter Rest kostet 1500 LP je Mana
        PlaceLienOnTarget,              // Pfandrecht amount auf Zielmonster (Standby des Kontrolleurs: zahlen oder zerstört)
        RaiseLienOnTarget,              // bestehendes Pfandrecht des Ziels um amount erhöhen
        SwapControlWithTarget,          // dauerhafter Kontrolltausch: erstes Ziel (eigenes) gegen zweites Ziel (gegnerisches)
        GiveSelfToOpponent,             // Quellkarte wechselt dauerhaft zum Gegner (Gift Horse)
        SpecialSummonFromOpponentGraveyard, // Ziel aus dem GEGNER-Friedhof aufs eigene Feld; verbannt, wenn es das Feld verlässt
        MoveSelfToZone,                 // Quellkarte in eine leere eigene Monsterzone ziehen (amount 1 = nur Nachbarzone)
        MoveTargetToZone,               // Zielmonster (eigenes) in eine leere Zone ziehen
        ExtraPositionChangeThisTurn,    // Quellkarte darf diesen Zug noch einmal die Position wechseln
        SkipOwnNextDrawPhase,           // der Aktivierende zieht in seiner nächsten Draw Phase nicht
        ShuffleBothHandsRedraw,         // beide mischen die Hand ins Deck und ziehen gleich viele; amount = Extrakarten für den Aktivierenden
        DeclareTypeRevealTop,           // Kartenart deklarieren, oberste amount Karten aufdecken: Treffer auf die Hand, Rest ins Grab
        RedirectManaFromChainLink,      // Mana-Gewinn des vorigen (gegnerischen) Kettenglieds geht an den Aktivierenden
        NegatePreviousChainLink,        // annulliert NUR das direkt vorige Kettenglied (gegnerischer Zauber)
        EndBattlePhaseNow,              // die laufende Battle Phase endet sofort (Parley)
        DoubleBattleDamageUntilNextTurnEnd, // Kampfschaden ×2 bis zum Ende des NÄCHSTEN eigenen Zuges (High Stakes)
        GrantPiercingThisTurn,          // Ziel(e) fügen diesen Zug Piercing-Kampfschaden zu
        LockOwnSpellsThisTurn,          // der Aktivierende kann diesen Zug keine weiteren Zauber aktivieren (Unbroken Oath)
        DebuffAdjacentPermanent,        // Nachbarn der Quellkarte verlieren dauerhaft amount ATK und DEF (Load-Bearing Wall)
        DamageSelf,                     // Schaden an die EIGENEN LP — kein Kostenzahlen, Aurel hilft nicht (House Always Wins: Tails)
        DestroyAllEnemyMonsters,        // alle gegnerischen Monster zerstören (Sabine: Heads)
        DestroyAllOtherOwnMonsters,     // alle ANDEREN eigenen Monster zerstören (Sabine: Tails)
        SpecialSummonTargetFromDeckSuppressed, // Ziel aus dem Deck beschwören: Effekte bis Zugende annulliert, kein Angriff diesen Zug (Sign in Blood)
        PickTargetOnly,                 // nur Zielwahl, die Aktion selbst tut nichts — die NÄCHSTE Aktion nutzt das Ziel (Fair Trade: eigenes Monster)
        NegateAllOpponentCards,         // alle offenen gegnerischen Feldkarten sind bis Zugende annulliert (The Unbroken Oath)
        GainAtkOfFacingMonsterEot,      // Ziel erhält bis Zugende amount % des ATK des Monsters GEGENÜBER (Stare Down)
        DiscardSelfRandom,              // der Aktivierende wirft amount zufällige Handkarten ab (Grinner: Tails)
        HealSelfPerCount,               // amount LP je gezählter Karte (countKind), höchstens targetCount Zählungen (Aurel)

        // --- Road to 1000 (September 2026) — NUR ANHÄNGEN ---
        WinTheDuel,                     // der Aktivierende gewinnt das Duell sofort (Krönung des abwesenden Königs)
        SealEnemyZones,                 // amount leere GEGNER-Monsterzonen versiegeln — bis zum Ende des nächsten eigenen Zuges
        SealEnemyZonesWhileSourceFaceUp,// amount leere Gegner-Zonen versiegeln, solange die QUELLKARTE offen liegt (Bricklayer)
        SealAnyZones,                   // amount leere Monsterzonen BELIEBIGER Seite versiegeln — bis Ende des nächsten eigenen Zuges
        SpecialSummonGraveTop,          // oberste Friedhofskarte beschwören, wenn Monster mit Level <= levelFilter (0 = egal)
        SpecialSummonGraveTopMonsterFaceDown, // oberste MONSTERkarte des Friedhofs verdeckt beschwören (Buried With His Boots On)
        ReturnGraveTopToHand,           // die obersten amount Karten des eigenen Friedhofs auf die Hand (Last In, First Out)
        BanishOpponentGraveTop,         // die obersten amount Karten des GEGNER-Friedhofs verbannen (Echo Infused)
        MoveGraveTopToBottom,           // die oberste Karte des eigenen Friedhofs UNTER den Stapel legen (Unquiet Topsoil)
        SpecialSummonSelfFromGrave,     // die Quellkarte aus dem eigenen Friedhof beschwören (He Sleeps Lightly)
        ChangeTargetLevelPermanent,     // Ziel-Level dauerhaft um amount ändern (geklemmt auf 1..3; Promotion Board)
        SetTargetLevelThisTurn,         // Ziel-Level bis Zugende auf amount setzen (Demoted for Cause)
        ChooseSelfLevelThisTurn,        // Quellkarte: Spieler wählt ihr Level (1-3) bis Zugende (Stuck on the Middle Rung)
        DiscountNextNormalSummon,       // nächste Normalbeschwörung diesen Zug kostet amount Tribute weniger (99 = keine)
        TickCountdownSelf,              // amount Countdown-Marker der Quellkarte entfernen (Appointed Hour Infused)
        LookReorderTopDeck,             // oberste amount Karten des EIGENEN Decks ansehen und in Wunschreihenfolge zurücklegen
        LookReorderOpponentTopDeck,     // dasselbe für das GEGNER-Deck (The Day After Tomorrow's News)
        RevealTopDeckSummonIfLowLevel,  // oberste Deckkarte aufdecken: Monster mit Level <= levelFilter (0 = egal) wird beschworen, sonst Friedhof
        RevealOpponentTopDeckMayBottom, // oberste GEGNER-Deckkarte aufdecken; der Aktivierende darf sie nach unten legen
        RevealTopDeckTakeMonsters,      // oberste amount Karten aufdecken: Monster auf die Hand, Rest bleibt in Reihenfolge oben
        PutTargetHandCardToDeckBottom,  // Ziel-Handkarte(n) UNTER das eigene Deck legen
        PutTargetHandCardOnTopOfDeck,   // Ziel-Handkarte oben AUF das eigene Deck legen (Ink for the Third Edition)
        RevealOwnHandDrawByContent,     // eigene Hand vorzeigen: ohne Zauber amount+1 ziehen, sonst amount (Honest Man's Bluff)
        RevealOwnHandDrawIfEmpty,       // eigene Hand vorzeigen: ist sie leer, amount Karten ziehen (The Beggar)
        RevealOwnHandBuffPerMonster,    // eigene Hand vorzeigen: Quellkarte +amount ATK dauerhaft je vorgezeigtem Monster
        OpponentRevealsRandomHandCard,  // der Gegner zeigt amount zufällige Handkarten vor
        BothRevealHandsDrawIfOpponentMore, // beide zeigen die Hand; amount ziehen, wenn der Gegner mehr Handkarten hat
        OpponentRevealsHandDrawIfMore,  // nur der GEGNER zeigt seine Hand; amount ziehen, wenn er mehr Handkarten hat
        GrantAttacksWithDefThisTurn,    // Ziel(e) greifen diesen Zug mit DEF an; zusätzlich +amount DEF bis Zugende
        TaxOpponentNextSpellThisTurn,   // der nächste gegnerische Zauber diesen Zug kostet amount Mana mehr (Countersign)
        MoveEnemyTargetToZone,          // gegnerisches Ziel-Monster in eine leere Zone SEINER Seite schieben (Wrong Queue, Sir)
        RotateOwnMonsters,              // alle eigenen Monster eine Zone nach links/rechts (Wahl); amount 1 = +1 Karte ziehen bei 3+ bewegten
        SetBothLifeToLower,             // die LP beider Spieler werden auf den niedrigeren Wert gesetzt (Settle the Difference)
        HealHalfLpDifference,           // halbe LP-Differenz als Heilung, höchstens amount (The Even Scales)
        SetTargetMonstersFromHandFaceDown, // Ziel-Monster aus der Hand verdeckt in Verteidigung legen; amount 1 = je Karte über der ersten 1 ziehen
        DrawIfHandAtMost,               // 1 Karte ziehen, wenn die Hand danach höchstens amount Karten hält (Making Ends Meet)

        // --- 5 Archetypes (September 2026) — NUR ANHÄNGEN ---
        OfferDeal,                      // Splithoof: der GEGNER wählt Option A oder B (dealOptionA/B); folgende Aktionen mit dealGate laufen entsprechend
        SwapStrongestMonsters,          // die offenen Monster mit dem höchsten ATK beider Spieler tauschen dauerhaft die Kontrolle
        OpponentSendsStrongestToGrave,  // das offene Monster mit dem höchsten ATK des Gegners geht in den Friedhof (kein "zerstören")
        DrawPerCount,                   // amount unbenutzt: 1 Karte je gezählter Karte (countKind) ziehen, höchstens targetCount
        TopDeckWager,                   // beide decken die oberste Deckkarte auf: höheres Level → Hand, Verlierer-Karte → Grab; amount 1 = bei eigenem Sieg 1 ziehen
        SpecialSummonTargetToOpponentField, // Giftwyrm: Ziel (Hand/Deck/Friedhof) auf das Feld des GEGNERS spezialbeschwören — keine Summon-Trigger
        ReclaimOwnFromOpponentField,    // Giftwyrm: bis zu targetCount eigene (OriginalOwner) Monster mit nameFilter vom Gegnerfeld aufs eigene zurückholen; amount = ATK-Bonus bis Zugende
        SpecialSummonSelfFromHand,      // Waylay-Ambush: die Quellkarte aus der Hand spezialbeschwören
        CancelAttackTarget,             // der laufende Angriff des Ziels wird abgebrochen (setzt CannotAttackThisTurn; ResolveAttack bricht ab)
        DebuffAllEnemyAtkEot,           // alle offenen gegnerischen Monster verlieren amount ATK bis Zugende
        ExemptFromDecree,               // Bylaw Loophole: das Ziel-Dekret wirkt bis Zugende nicht auf den Aktivierenden
        TickCountdownTarget,            // Chimekeep: amount Countdown-Marker vom ZIEL entfernen (eigene Karte); bei 0 feuert dessen Nullschlag
        StrikeAllOwnCountdowns          // Chimekeep-Carillon: ALLE eigenen Countdown-Karten schlagen sofort (Marker auf 0, Effekte feuern)
    }

    /// <summary>Münzwurf-Gate einer Aktion: läuft nur, wenn der letzte Wurf des Effekts so fiel.</summary>
    public enum CoinGate { None, Heads, Tails }

    /// <summary>
    /// Deal-Gate einer Aktion (Splithoof): läuft nur, wenn der Gegner beim letzten
    /// OfferDeal dieses Effekts die entsprechende Option gewählt hat.
    /// </summary>
    public enum DealGate { None, OptionA, OptionB }

    /// <summary>Was BuffSelfPerCount / ähnliche Zähl-Aktionen zählen.</summary>
    public enum EffectCountKind
    {
        OwnArtifactsOnField,    // eigene Artefakte auf dem Feld
        OwnGraveyardArtifacts,  // Artefakte im eigenen Friedhof
        OwnFaceDownMonsters,    // eigene verdeckte Monster
        OwnBanishedMonsters,    // eigene verbannte Monster
        OwnGraveyardCards,      // Karten im eigenen Friedhof
        OwnMonstersOnField,     // eigene Monster auf dem Feld
        EquippedArtifactsOnSelf, // an DIESER Karte ausgerüstete Artefakte (nur Passiv-Skalierung)
        OpponentFaceDownMonsters, // verdeckte Monster des Gegners (Night Terror)
        OpponentIllusionTokens, // Illusion-Tokens auf dem gegnerischen Feld (Gaslight Charlatan)
        OwnHandCards,           // eigene Handkarten (Marrow, Who Holds Every Card)
        OwnGraveyardSpells,     // Zauber im eigenen Friedhof (The House Always Wins)

        // --- Road to 1000 ---
        OwnDistinctLevels,      // Anzahl VERSCHIEDENER Level unter den eigenen offenen Monstern (Stuck on the Middle Rung)

        // --- 5 Archetypes ---
        OwnMonstersOnOpponentField, // eigene (OriginalOwner) Monster auf dem GEGNERFELD (Giftwyrm)
        AllArtifactsOnField     // Artefakte auf BEIDEN Feldern (Bylaw Enforcer)
    }

    public enum TargetKind
    {
        None,                 // kein Ziel nötig
        EnemyMonster,         // gegnerisches Monster
        AllyMonster,          // eigenes Monster
        AnyMonster,           // beliebiges Monster
        GraveyardMonsterSelf, // Monster im eigenen Friedhof
        GraveyardCardSelf,    // beliebige Karte im eigenen Friedhof
        DeckMonsterFiltered,       // Monster im eigenen Deck (Filter der Action beachten)
        HandMonsterFiltered,       // Monster in der eigenen Hand (Filter der Action beachten)
        HandOrGraveMonsterFiltered, // Monster in Hand oder Friedhof (Filter der Action beachten)
        GraveyardCardAny,          // beliebige Karte in einem der beiden Friedhöfe
        BanishedMonsterAny,        // Monster in einer der beiden Verbannungszonen
        DeckSpellFiltered,         // Zauber im eigenen Deck (Filter der Action beachten)
        DeckCardFiltered,          // beliebige Karte im eigenen Deck (Filter der Action beachten)
        GraveOrBanishedMonsterSelf, // Monster im eigenen Friedhof ODER der eigenen Verbannung
        FaceDownMonsterAny,        // verdecktes Monster auf einem der beiden Felder
        SelfCard,                  // die Quellkarte selbst (kein Auswahl-Dialog)
        FaceDownMonsterEnemy,      // verdecktes Monster auf dem gegnerischen Feld
        GraveyardSpellSelf,        // Zauberkarte im eigenen Friedhof
        DeckArtifactFiltered,      // Artefakt im eigenen Deck (Filter der Action beachten)
        GraveyardArtifactSelf,     // Artefakt im eigenen Friedhof
        HandArtifactFiltered,      // Artefakt in der eigenen Hand
        FaceDownMonsterSelf,       // eigenes verdecktes Monster
        EnemyCardOnField,          // beliebige gegnerische Karte auf dem Feld (Monster/Zauber/Artefakt)
        AllyArtifact,              // eigenes Artefakt auf dem Feld
        DeckMonsterFilteredSelf,   // Monster im eigenen Deck (Alias für Deck-Beschwörungen)

        // --- neu; nur anhängen, siehe Hinweis bei EffectActionType ---
        HandCardSelf,              // beliebige Karte in der eigenen Hand
        HandCardOpponent,          // beliebige Karte in der gegnerischen Hand (aufgedeckt gewählt)

        // --- Batch August 2026 ---
        EnemySpellOrArtifact,      // gegnerischer Zauber oder gegnerisches Artefakt auf dem Feld
        BanishedMonsterSelf,       // Monster in der eigenen Verbannung
        BanishedCardSelf,          // beliebige Karte in der eigenen Verbannung
        GraveyardCardOpponent,     // beliebige Karte im gegnerischen Friedhof
        AllySpellOrArtifact,       // eigener Zauber (auch gesetzt) oder eigenes Artefakt auf dem Feld
        HandSpellFiltered,         // Zauber in der eigenen Hand (Filter der Action beachten)

        // --- Mimicrypt (Gegner-Friedhof) ---
        GraveyardSpellOpponent,    // Zauberkarte im gegnerischen Friedhof
        GraveyardMonsterOpponent,  // Monster im gegnerischen Friedhof

        // --- Dark-Angel-Paket ---
        ExtraDeckReliquarySelf,    // Reliquary im EIGENEN Extra Deck (z.B. als Verbannungs-Kosten)
        EnemyReliquaryOnField,     // gegnerisches Reliquary auf dem Feld

        // --- The Small Print: Zonen-Ziele (werden ohne Dialog automatisch gewählt) ---
        AdjacentAllyMonsters,      // die eigenen Monster links und rechts der Quellkarte
        FacingEnemyMonster,        // das gegnerische Monster in der Zone gegenüber der Quellkarte
        EnemyMonsterWithLien,      // gegnerisches Monster mit Pfandrecht
        AnyMonsterWithLien,        // beliebiges Monster mit Pfandrecht
        EnemyLevel1Monster,        // gegnerisches Level-1-Monster (Changeling Cradle)
        EnemyDefenseMonster,       // gegnerisches Monster in Verteidigungsposition
        SameAsPrevious,            // dieselben Ziele wie die letzte zielende Aktion davor — kein zweiter Dialog (Lock Shields)

        // --- 5 Archetypes (September 2026) ---
        AllyCountdownCard,         // eigene Feldkarte mit Countdown-Markern (Chimekeep)
        AnyArtifactOnField         // Artefakt in einer Artefakt-Zone BEIDER Seiten (Bylaw Ombudsman/Loophole)
    }

    /// <summary>
    /// Art eines Infused-Effekts: Standalone = eigenständige Fähigkeit, unabhängig nutzbar.
    /// Coupled = Upgrade des vorangehenden Normal-Effekts — pro Zug nur einer von beiden.
    /// </summary>
    public enum InfusedKind { Standalone, Coupled }

    /// <summary>
    /// In welchem Reaktionsfenster ein gesetzter Quick-Zauber zünden darf (Trapline).
    /// Any = überall (auch offen in der Main Phase spielbar). AttackResponse/SummonResponse =
    /// NUR im jeweiligen Fenster — solche Fallen müssen gesetzt sein und warten.
    /// </summary>
    public enum QuickWindow { Any, AttackResponse, SummonResponse }
}
