using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rouge.Tcg;
using Rouge.Tcg.UI;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Bot-gegen-Bot-Probe in der ECHTEN Duel-Szene, mit Präsentation und UI:
    /// Menü „Rouge TCG/Bot Probe/…" schreibt die Wunsch-Decks in ein Deck-Asset,
    /// öffnet die Duel-Szene und startet den Play Mode. Unmittelbar VOR dem
    /// Eintritt (ExitingEditMode — Start() des DuelHost läuft schon im ersten
    /// Bild, EnteredPlayMode käme zu spät) werden beide Seiten auf Bot gestellt
    /// und die Decks getauscht; nach dem Play Mode werden die Originalwerte
    /// zurückgeschrieben und die Szene wieder als sauber markiert — auf Platte
    /// ändert sich nichts.
    ///
    /// Zweck: Präsentations-Pfade (Badges, Flüge, Banner, Prompts) mit neuen
    /// Karten durchspielen, ohne selbst zu klicken. Der DuelHost-Selftest deckt
    /// nur die Engine ab; hier läuft alles, was der Spieler wirklich sieht.
    /// </summary>
    [InitializeOnLoad]
    public static class DuelBotProbe
    {
        private const string FlagKey = "Rouge.BotProbe.Active";
        private const string SnapshotKey = "Rouge.BotProbe.Snapshot";   // Originalwerte des DuelHost
        private const string DuelScene = "Assets/_Game/Scenes/Duel.unity";
        private const string DeckAsset = "Assets/_Game/Data/Tcg/Decks/BotProbe.asset";

        static DuelBotProbe()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Rouge TCG/Bot Probe/The Small Print (Duel-Szene, Bot vs Bot)")]
        public static void ProbeSmallPrint()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/_Game/Data/Tcg/CardCatalog.asset");
            var main = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.6b" && !(c is ReliquaryCardData) && !(c is PlayerCardData)).Select(c => c.cardName).ToList();
            var extra = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.6b" && c is ReliquaryCardData).Select(c => c.cardName).ToList();
            // Monster doppelt, damit das Deck 40+ Karten hat und Beschwörungen nicht ausgehen
            var doubled = new List<string>(main);
            doubled.AddRange(main.Where(n => catalog.FindByName(n) is MonsterCardData));
            Start(doubled, extra);
        }

        [MenuItem("Rouge TCG/Bot Probe/Road to 1000 (Duel-Szene, Bot vs Bot)")]
        public static void ProbeRoadTo1000()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/_Game/Data/Tcg/CardCatalog.asset");
            var main = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.7" && !(c is ReliquaryCardData) && !(c is PlayerCardData)).Select(c => c.cardName).ToList();
            var extra = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.7" && c is ReliquaryCardData).Select(c => c.cardName).ToList();
            var doubled = new List<string>(main);
            doubled.AddRange(main.Where(n => catalog.FindByName(n) is MonsterCardData));
            Start(doubled, extra);
        }

        [MenuItem("Rouge TCG/Bot Probe/5 Archetypes (Duel-Szene, Bot vs Bot)")]
        public static void ProbeArchetypes5()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/_Game/Data/Tcg/CardCatalog.asset");
            string[] families = { "Giftwyrm", "Splithoof", "Waylay", "Bylaw", "Chimekeep" };
            var main = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.7"
                && families.Any(f => c.cardName.Contains(f) || c.cardName == "Stand and Deliver!")
                && !(c is ReliquaryCardData) && !(c is PlayerCardData)).Select(c => c.cardName).ToList();
            var extra = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.7"
                && families.Any(f => c.cardName.Contains(f)) && c is ReliquaryCardData).Select(c => c.cardName).ToList();
            var doubled = new List<string>(main);
            doubled.AddRange(main.Where(n => catalog.FindByName(n) is MonsterCardData));
            Start(doubled, extra);
        }

        [MenuItem("Rouge TCG/Bot Probe/Wave 3 (Duel-Szene, Bot vs Bot)")]
        public static void ProbeWave3()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/_Game/Data/Tcg/CardCatalog.asset");
            var main = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.8" && !(c is ReliquaryCardData) && !(c is PlayerCardData)).Select(c => c.cardName).ToList();
            var extra = catalog.cards.Where(c => c != null && c.releaseVersion == "0.1.8" && c is ReliquaryCardData).Select(c => c.cardName).ToList();
            var doubled = new List<string>(main);
            doubled.AddRange(main.Where(n => catalog.FindByName(n) is MonsterCardData));
            Start(doubled, extra);
        }

        [MenuItem("Rouge TCG/Bot Probe/Abbrechen (Flag löschen)")]
        public static void Cancel() { EditorPrefs.DeleteKey(FlagKey); EditorPrefs.DeleteKey(SnapshotKey); }

        /// <summary>Probe mit beliebigen Kartennamen starten (auch aus execute_code aufrufbar).</summary>
        public static void Start(List<string> cardNames, List<string> extraNames)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("BotProbe: erst den Play Mode beenden."); return; }
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/_Game/Data/Tcg/CardCatalog.asset");
            WriteDeckAsset(catalog, cardNames, extraNames ?? new List<string>());
            EditorPrefs.SetBool(FlagKey, true);
            EditorSceneManager.OpenScene(DuelScene, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(FlagKey, false)) return;
            if (state == PlayModeStateChange.ExitingEditMode) ApplyProbe();
            else if (state == PlayModeStateChange.EnteredEditMode) RestoreScene();
        }

        /// <summary>Kurz vor dem Play Mode: DuelHost auf Bot vs Bot mit dem Probe-Deck stellen.</summary>
        private static void ApplyProbe()
        {
            var host = Object.FindAnyObjectByType<DuelHost>();
            var deck = AssetDatabase.LoadAssetAtPath<DeckDefinition>(DeckAsset);
            if (host == null || deck == null)
            {
                Debug.LogWarning("BotProbe: kein DuelHost oder kein Probe-Deck — Probe abgebrochen.");
                EditorPrefs.DeleteKey(FlagKey);
                return;
            }
            var so = new SerializedObject(host);
            var snapshot = new Snapshot
            {
                deck1 = AssetDatabase.GetAssetPath(so.FindProperty("player1Deck").objectReferenceValue),
                deck2 = AssetDatabase.GetAssetPath(so.FindProperty("player2Deck").objectReferenceValue),
                ctrl1 = so.FindProperty("player1Controller").enumValueIndex,
                ctrl2 = so.FindProperty("player2Controller").enumValueIndex,
                coinToss = so.FindProperty("enableCoinToss").boolValue,
                delay = so.FindProperty("botActionDelay").floatValue
            };
            EditorPrefs.SetString(SnapshotKey, JsonUtility.ToJson(snapshot));

            so.FindProperty("player1Deck").objectReferenceValue = deck;
            so.FindProperty("player2Deck").objectReferenceValue = deck;
            so.FindProperty("player1Controller").enumValueIndex = (int)ControllerKind.Bot;
            so.FindProperty("player2Controller").enumValueIndex = (int)ControllerKind.Bot;
            so.FindProperty("enableCoinToss").boolValue = false;
            so.FindProperty("botActionDelay").floatValue = 0.15f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"BotProbe: Bot vs Bot mit {deck.cards.Count} Karten + {deck.extraCards.Count} Extra.");
        }

        /// <summary>Nach dem Play Mode: Originalwerte zurück, Szene wieder sauber.</summary>
        private static void RestoreScene()
        {
            EditorPrefs.DeleteKey(FlagKey);
            var host = Object.FindAnyObjectByType<DuelHost>();
            var json = EditorPrefs.GetString(SnapshotKey, "");
            EditorPrefs.DeleteKey(SnapshotKey);
            if (host == null || string.IsNullOrEmpty(json)) return;
            var snapshot = JsonUtility.FromJson<Snapshot>(json);
            var so = new SerializedObject(host);
            so.FindProperty("player1Deck").objectReferenceValue = string.IsNullOrEmpty(snapshot.deck1) ? null : AssetDatabase.LoadAssetAtPath<DeckDefinition>(snapshot.deck1);
            so.FindProperty("player2Deck").objectReferenceValue = string.IsNullOrEmpty(snapshot.deck2) ? null : AssetDatabase.LoadAssetAtPath<DeckDefinition>(snapshot.deck2);
            so.FindProperty("player1Controller").enumValueIndex = snapshot.ctrl1;
            so.FindProperty("player2Controller").enumValueIndex = snapshot.ctrl2;
            so.FindProperty("enableCoinToss").boolValue = snapshot.coinToss;
            so.FindProperty("botActionDelay").floatValue = snapshot.delay;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Inhalt ist wieder identisch — den Dirty-Haken wegnehmen, damit niemand
            // zum Speichern gefragt wird (interne API, per Reflection; fehlt sie, bleibt
            // die Szene halt „geändert", aber inhaltsgleich).
            var clear = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var scene = host.gameObject.scene;
            if (clear != null) clear.Invoke(null, new object[] { scene });
            Debug.Log("BotProbe: DuelHost zurückgesetzt, Szene unverändert.");
        }

        private static void WriteDeckAsset(CardCatalog catalog, List<string> cards, List<string> extra)
        {
            var deck = AssetDatabase.LoadAssetAtPath<DeckDefinition>(DeckAsset);
            bool fresh = deck == null;
            if (fresh) deck = ScriptableObject.CreateInstance<DeckDefinition>();
            deck.deckName = "Bot Probe";
            deck.cards.Clear();
            deck.extraCards.Clear();
            foreach (var name in cards) { var card = catalog.FindByName(name); if (card != null) deck.cards.Add(card); }
            foreach (var name in extra) { var card = catalog.FindByName(name); if (card != null) deck.extraCards.Add(card); }
            if (deck.playerCard == null)
                foreach (var card in catalog.cards) if (card is PlayerCardData hero) { deck.playerCard = hero; break; }
            if (fresh) AssetDatabase.CreateAsset(deck, DeckAsset);
            else EditorUtility.SetDirty(deck);
            AssetDatabase.SaveAssets();
        }

        [System.Serializable]
        private class Snapshot
        {
            public string deck1, deck2;
            public int ctrl1, ctrl2;
            public bool coinToss;
            public float delay;
        }
    }
}
