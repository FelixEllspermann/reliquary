using System.Collections.Generic;

namespace Rouge.Tcg
{
    /// <summary>Basisklasse für alle Entscheidungs-Anfragen an einen Controller (Mensch oder Bot).</summary>
    public abstract class DuelRequest
    {
        public string Title = "";
        public bool Answered;
    }

    /// <summary>Münzwurf gewonnen: beginnt der Gewinner (true) oder der Gegner (false)?</summary>
    public class StartChoiceRequest : DuelRequest
    {
        public bool Result = true;
    }

    public class YesNoRequest : DuelRequest
    {
        public CardInstance Card;
        public string Question = "";
        public bool Result;

        /// <summary>True, wenn die Frage aus einem Phasenwechsel-Priority-Fenster stammt (kein konkretes Ereignis).</summary>
        public bool IsPhaseWindow;
    }

    public class OptionRequest : DuelRequest
    {
        public CardInstance Card;
        public List<string> Options = new List<string>();
        public bool AllowCancel;
        public int Result = -1; // -1 = abgebrochen
    }

    public class TargetRequest : DuelRequest
    {
        public TargetKind Kind = TargetKind.AnyMonster;
        public List<CardInstance> Candidates = new List<CardInstance>();
        public int Count = 1;

        /// <summary>"Bis zu": Count ist die Obergrenze, weniger Ziele sind erlaubt.</summary>
        public bool AllowFewer;

        public bool AllowCancel;
        public bool Cancelled;
        public List<CardInstance> Result = new List<CardInstance>();
    }

    /// <summary>Fragt den Spieler, in welche freie Zone eine Karte gelegt werden soll.</summary>
    public class ZoneSelectRequest : DuelRequest
    {
        public PlayerState ForPlayer;
        public ZoneType Zone = ZoneType.MonsterZone;
        public List<int> FreeIndices = new List<int>();
        public int Result = -1;
    }

    public enum MainActionKind
    {
        SummonMonster,          // Monster aus der Hand beschwören (inkl. Tribute)
        SetSpell,               // Zauber verdeckt in eine Zauberzone legen
        ActivateSpellFromHand,  // Zauber direkt aus der Hand aktivieren
        ActivateSetSpell,       // gesetzten Zauber aktivieren
        PlayArtifact,           // Artefakt ausspielen/ausrüsten
        ActivateFieldEffect,    // Ignition-Effekt einer Feldkarte (auch Spielerkarte)
        ChangePosition,         // Kampfposition eines Monsters wechseln
        ToBattlePhase,
        EndTurn,
        SpecialSummonSelf,      // Monster erfüllt seine eigene Spezialbeschwörungs-Bedingung
        SummonReliquary         // Reliquary aus dem Extra Deck beschwören (Bedingungen + Kosten)
    }

    public class MainActionOption
    {
        public MainActionKind Kind;
        public CardInstance Card;
        public int EffectIndex = -1;

        /// <summary>Wunsch-Zone (z.B. per Drag & Drop gewählt); -1 = erste freie Zone.</summary>
        public int PreferredZoneIndex = -1;

        public string Label = "";
    }

    public class MainActionRequest : DuelRequest
    {
        public List<MainActionOption> Options = new List<MainActionOption>();
        public int Chosen = -1;
    }

    public class BattleOption
    {
        public CardInstance Attacker;
        public CardInstance Target;
        public bool Direct;
        public bool EndBattle;
        public string Label = "";
    }

    public class BattleActionRequest : DuelRequest
    {
        public List<BattleOption> Options = new List<BattleOption>();
        public int Chosen = -1;
    }
}
