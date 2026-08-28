using System.Linq;
using UnityEditor;
using UnityEngine;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // Reliquary-Regel seit 28.08.2026 (Felix): Beschwörungs-VORAUSSETZUNGEN
    // verlangen KEINE Aktionen mehr (kein Tribut, kein Grab-Banish, kein
    // Zerstören) — nur noch Board-State + Mana. Aktionskosten sind für eine
    // künftige neue Kartenart reserviert. Effekt-Kosten (Infused „tribute 1: …")
    // bleiben erlaubt — die Regel betrifft nur die Beschwörung.
    //
    // Dieser Batch räumt die 18 Bestands-Reliquaries mit Aktions-Kosten um:
    // Kosten raus, Board-State/Mana als Ausgleich, und wo die Kosten die
    // Identität WAREN (Sacrilegion First/Third, Grunn), wandert die Opferung
    // in Summon-/Infused-EFFEKTE. Idempotent.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Rework Reliquaries (keine Aktions-Kosten mehr)")]
        public static void ReworkReliquaryCosts()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            int touched = 0;

            ReliquaryCardData Rel(string name)
            {
                var card = catalog.FindByName(name) as ReliquaryCardData;
                if (card == null) { Debug.LogError($"[RelRework] fehlt: {name}"); return null; }
                // Die eigentliche Regel: alle Aktions-Kosten der Beschwörung fallen
                card.costBanishMonstersFromGrave = 0;
                card.costTributeOtherMonster = false;
                card.costTributeOwnMonsters = 0;
                card.costTributeOpponentMonsters = 0;
                touched++;
                EditorUtility.SetDirty(card);
                return card;
            }

            // ---- Archfiends: Comeback-Removal braucht ein dominiertes Board ----
            var dominus = Rel("Archfiend Dominus");
            dominus.summonManaCost = 4;
            dominus.reqOpponentMonstersAtLeast = 3;
            dominus.summonText = "Your opponent controls 3+ monsters — pay 4 Mana.";

            var kingmaker = Rel("Archfiend Kingmaker");
            kingmaker.summonManaCost = 4;
            kingmaker.reqGraveyardMonstersAtLeast = 5;
            kingmaker.summonText = "5+ monsters in your Graveyard — pay 4 Mana.";

            var breath = Rel("Fethaerbreese, the Held Breath");
            breath.summonManaCost = 4;
            breath.summonText = "You control 2+ \"Fethaerbreese\" monsters and have 5+ cards in your Graveyard — pay 4 Mana.";

            var gravemaw = Rel("Gravemaw, the Bottomless");
            gravemaw.summonManaCost = 3;
            gravemaw.reqGraveyardMonstersAtLeast = 6;
            gravemaw.summonText = "6+ monsters in your Graveyard — pay 3 Mana.";

            // ---- Grunn: das Fressen wird zum EFFEKT statt zur Beschwörungs-Kost ----
            var grunn = Rel("Grunn, Who Eats His Own");
            grunn.summonManaCost = 3;
            grunn.reqOwnMonstersAtLeast = 3;
            grunn.summonText = "You control 3+ monsters — pay 3 Mana.";
            if (!grunn.effects.Any(e => e != null && e.label == "Still Eating"))
                grunn.effects.Add(Inf("Still Eating",
                    "Pay 1 Mana and send 1 other monster you control to the Graveyard: this card permanently gains 400 ATK.",
                    EffectTrigger.Ignition, 1, false,
                    Act(EffectActionType.SendTargetToGraveyard, 1, TargetKind.AllyMonster, excludeSelf: true, isCost: true),
                    Act(EffectActionType.BuffTargetAtk, 400, TargetKind.SelfCard)));

            var kindlekin = Rel("Kindlekin, the Last Ember");
            kindlekin.reqBanishedAtLeast = 3;
            kindlekin.summonText = "You control 4+ monsters, 6+ cards in your Graveyard and 3+ banished cards — pay 4 Mana.";

            var worldgear = Rel("Mechination Worldgear");
            worldgear.summonManaCost = 4;
            worldgear.summonText = "You control 3+ monsters and have 5+ cards in your Graveyard — pay 4 Mana.";

            var redactor = Rel("Redactor, Final Edition");
            redactor.summonManaCost = 4;
            redactor.reqGraveyardAtLeast = 6;
            redactor.summonText = "6+ cards in your Graveyard — pay 4 Mana.";

            // ---- Sacrilegion: die Opferung wandert in die Summon-EFFEKTE ----
            var first = Rel("Sacrilegion First Sacrament");
            first.summonManaCost = 2;
            first.reqOwnMonstersAtLeast = 1;
            first.reqOpponentMonstersAtLeast = 1;
            first.summonText = "You and your opponent each control 1+ monsters — pay 2 Mana.";
            if (!first.effects.Any(e => e != null && e.label == "The First Rite"))
                first.effects.Add(Fx("The First Rite",
                    "When this card is Summoned: send 1 other monster you control and your opponent's strongest monster to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SendTargetToGraveyard, 1, TargetKind.AllyMonster, excludeSelf: true),
                    Act(EffectActionType.OpponentSendsStrongestToGrave)).Mand());

            var second = Rel("Sacrilegion Second Sacrament");
            second.summonManaCost = 3;
            second.reqOwnMonstersAtLeast = 2;
            second.reqGraveyardAtLeast = 4;
            second.summonText = "You control 2+ monsters and have 4+ cards in your Graveyard — pay 3 Mana.";

            var third = Rel("Sacrilegion Third Sacrament");
            third.summonManaCost = 5;
            third.reqOwnMonstersAtLeast = 1;
            third.reqOpponentMonstersAtLeast = 2;
            third.reqGraveyardAtLeast = 5;
            third.summonText = "You control 1+ monsters, your opponent controls 2+ and you have 5+ cards in your Graveyard — pay 5 Mana.";
            if (!third.effects.Any(e => e != null && e.label == "The Third Rite"))
                third.effects.Add(Fx("The Third Rite",
                    "When this card is Summoned: send 1 other monster you control and your opponent's 2 strongest monsters to the Graveyard.",
                    EffectTrigger.OnSummonSelf, 0, true,
                    Act(EffectActionType.SendTargetToGraveyard, 1, TargetKind.AllyMonster, excludeSelf: true),
                    Act(EffectActionType.OpponentSendsStrongestToGrave),
                    Act(EffectActionType.OpponentSendsStrongestToGrave)).Mand());

            var communion = Rel("Sacrilegion, Communion of Bone");
            communion.summonManaCost = 5;
            communion.reqOwnMonstersAtLeast = 2;
            communion.summonText = "You control 2+ monsters and have 6+ cards in your Graveyard — pay 5 Mana.";

            var oath = Rel("Sacrilegion, the Last Oath");
            oath.summonManaCost = 6;
            oath.reqOwnMonstersAtLeast = 3;
            oath.reqOpponentMonstersAtLeast = 1;
            oath.summonText = "You control 3+ monsters, your opponent controls 1+ and you have 8+ cards in your Graveyard — pay 6 Mana.";

            // ---- Snugglets: keiner muss mehr gehen — dafür kostet das Kuscheln mehr ----
            var fortress = Rel("Snugglet Blanket Fortress");
            fortress.summonManaCost = 4;
            fortress.summonText = "You control 3 \"Snugglet\" monsters and have 3+ cards in your Graveyard — pay 4 Mana.";

            var cuddle = Rel("Snugglet Cuddlepile, Three Deep");
            cuddle.summonManaCost = 3;
            cuddle.summonText = "You control 3 \"Snugglet\" monsters — pay 3 Mana.";

            var spoon = Rel("Snugglet, Big Spoon");
            spoon.summonManaCost = 4;
            spoon.summonText = "You control 3 \"Snugglet\" monsters — pay 4 Mana.";

            var squish = Rel("Snugglet, the Whole Squish");
            squish.summonManaCost = 5;
            squish.reqOwnMonstersAtLeast = 4;
            squish.summonText = "You control 4+ monsters (3 of them \"Snugglet\") and have 5+ cards in your Graveyard — pay 5 Mana.";

            var debt = Rel("The Debt Made Flesh");
            debt.summonManaCost = 4;
            debt.reqGraveyardAtLeast = 4;
            debt.summonText = "Your LP are 4000 or less and you have 4+ cards in your Graveyard — pay 4 Mana.";

            AssetDatabase.SaveAssets();
            Debug.Log($"[RelRework] {touched} Reliquaries umgestellt — Beschwörungen verlangen nur noch Board-State + Mana.");
        }
    }
}
