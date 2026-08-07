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
        OnOwnMonsterDestroyed  // wenn irgendein eigenes Monster zerstört wird (Warm Memories)
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
        BuffTargetAtkPerCountPermanent // Ziel: +amount ATK dauerhaft pro gezählter Karte (countKind)
    }

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
        OpponentFaceDownMonsters // verdeckte Monster des Gegners (Night Terror)
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
        AllySpellOrArtifact        // eigener Zauber (auch gesetzt) oder eigenes Artefakt auf dem Feld
    }

    /// <summary>
    /// Art eines Infused-Effekts: Standalone = eigenständige Fähigkeit, unabhängig nutzbar.
    /// Coupled = Upgrade des vorangehenden Normal-Effekts — pro Zug nur einer von beiden.
    /// </summary>
    public enum InfusedKind { Standalone, Coupled }
}
