using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Client-Seite der Lokalisierung: lädt beim Start die Sprachwahl aus den
    /// PlayerPrefs und die Tabellen aus Resources/Localization, baut zur Laufzeit
    /// eine CJK-Schrift aus einer System-Schrift (nichts wird mitgeliefert, kein
    /// Font-Asset im Projekt) und hängt sie als globalen TMP-Fallback ein — jede
    /// bestehende Schrift (Cinzel/Oswald/Spectral) kann damit chinesische Glyphen
    /// zeichnen, ohne dass ein Label seine Schrift wechselt.
    ///
    /// Wichtig (Projekt-Erfahrung): TMP zeichnet Glyphen auf Atlas-Textur 2 leer.
    /// Deshalb EIN dynamischer 4096er-Atlas, Multi-Atlas AUS — der reicht für
    /// mehrere tausend Zeichen pro Sitzung.
    ///
    /// Dazu der Szenen-Sweep: nach jedem Szenenladen werden alle TMP-Labels,
    /// deren Text EXAKT einem UI-Tabellen-Eintrag entspricht, übersetzt. Das
    /// erwischt die statischen Szenen-Beschriftungen (Zonen, Topbar, Piles),
    /// ohne jede Szene anzufassen; zur Laufzeit gesetzte Texte laufen über
    /// Loc.T/F an ihrer Quelle.
    /// </summary>
    public static class LocBoot
    {
        public const string PrefKey = "rouge.language";

        private static TMP_FontAsset cjkFont;
        private static bool hooked;

        public static TMP_FontAsset CjkFont => cjkFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            // Manuelle Wahl (Settings) gewinnt; ohne sie folgt das Spiel bei jedem
            // Start der Steam-Sprache (bzw. der OS-Sprache, wenn Steam nicht läuft).
            // Die Auto-Erkennung wird bewusst NICHT gespeichert — erst der erste
            // Wechsel im Settings-Menü legt die Wahl fest.
            Apply(PlayerPrefs.HasKey(PrefKey)
                    ? PlayerPrefs.GetString(PrefKey, Loc.English)
                    : DetectDefaultLanguage(),
                persist: false);
            if (!hooked)
            {
                hooked = true;
                // Zweimal fegen: sofort (statische Szenen-Labels) und kurz danach —
                // manche Texte setzt erst ein Start()/Refresh nach dem Laden.
                SceneManager.sceneLoaded += (_, _) => { SweepSceneLabels(); ScheduleLateSweep(); };
                Application.quitting += RemoveFallback;   // Editor-Play-Ende & Build-Quit
            }
        }

        private static void ScheduleLateSweep()
        {
            if (!Loc.Active) return;
            var go = new GameObject("~LocLateSweep");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<LateSweep>();
        }

        /// <summary>Fegt in den ersten Bildern nach dem Szenenladen noch zweimal nach.</summary>
        private class LateSweep : MonoBehaviour
        {
            private System.Collections.IEnumerator Start()
            {
                yield return null;
                SweepSceneLabels();
                yield return new WaitForSecondsRealtime(0.6f);
                SweepSceneLabels();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Ohne gespeicherte Wahl: die Sprache, in der Steam das Spiel ausliefert,
        /// sonst die OS-Sprache, sonst Englisch. Initialise ist idempotent — so ist
        /// die Reihenfolge der Boot-Hooks (LocBoot vs. SteamRuntime) egal.
        /// </summary>
        private static string DetectDefaultLanguage()
        {
            Net.SteamBridge.Initialise();
            switch (Net.SteamBridge.GameLanguage)
            {
                case "german": return Loc.German;
                case "schinese": return Loc.ChineseSimplified;
                case "russian": return Loc.Russian;
                case "french": return Loc.French;
                case "spanish":                 // Spanien …
                case "latam": return Loc.Spanish;   // … und Lateinamerika: eine Fassung
                case "": break;                 // kein Steam — das OS fragen
                default: return Loc.English;    // Steam-Sprache, die wir (noch) nicht haben
            }
            switch (Application.systemLanguage)
            {
                case SystemLanguage.German: return Loc.German;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified: return Loc.ChineseSimplified;
                case SystemLanguage.Russian: return Loc.Russian;
                case SystemLanguage.French: return Loc.French;
                case SystemLanguage.Spanish: return Loc.Spanish;
                default: return Loc.English;
            }
        }

        /// <summary>Sprache setzen + Tabellen (neu) laden. Persistiert nur auf Wunsch.</summary>
        public static void Apply(string language, bool persist = true)
        {
            if (persist) PlayerPrefs.SetString(PrefKey, language);
            Loc.Language = language;
            if (language == Loc.English)
            {
                Loc.SetTables(null, null);
                RemoveFallback();
                return;
            }

            Loc.SetTables(LoadUiTable(language), LoadCardTable(language));
            // Chinesisch und Russisch brauchen die Laufzeit-Schrift: CJK fehlt allen
            // Projekt-Fonts, Kyrillisch fehlt Cinzel (Überschriften/Kartennamen).
            // Deutsch und Französisch kommen ohne aus — lateinische Glyphen (Umlaute,
            // Akzente, œ, « ») tragen die Dynamic-Fonts selbst.
            if (language == Loc.ChineseSimplified || language == Loc.Russian) EnsureRuntimeFallback();
            else RemoveFallback();
        }

        /// <summary>Sprache wechseln und die aktive Szene neu laden (Menüs bauen sich neu auf).</summary>
        public static void Switch(string language)
        {
            Apply(language);
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ================== Tabellen ==================

        /// <summary>ui.&lt;lang&gt;.txt: „englisch<TAB>übersetzt" je Zeile, # = Kommentar.</summary>
        private static Dictionary<string, string> LoadUiTable(string language)
        {
            var table = new Dictionary<string, string>();
            var asset = Resources.Load<TextAsset>($"Localization/ui.{language}");
            if (asset == null) { Debug.LogWarning($"Loc: Localization/ui.{language} fehlt."); return table; }
            foreach (var raw in asset.text.Replace("\r", "").Split('\n'))
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                int tab = raw.IndexOf('\t');
                if (tab <= 0) continue;
                string key = raw.Substring(0, tab);
                // NICHT trimmen: Vorlagen-Bausteine („anderen “, „ Stufe {0}“)
                // tragen ihre Leerzeichen selbst.
                string value = raw.Substring(tab + 1);
                if (key.Length > 0 && value.Length > 0) table[key] = value;
            }
            return table;
        }

        /// <summary>
        /// cards.&lt;lang&gt;.txt: Blöcke je Karte —
        ///   ## Englischer Name
        ///   n Übersetzter Name
        ///   s Beschwörungstext (Reliquary)
        ///   l0/t0, l1/t1 … Label/Text je Effekt-INDEX (Reihenfolge der Builder).
        /// </summary>
        private static Dictionary<string, Loc.CardEntry> LoadCardTable(string language)
        {
            var table = new Dictionary<string, Loc.CardEntry>();
            var asset = Resources.Load<TextAsset>($"Localization/cards.{language}");
            if (asset == null) { Debug.LogWarning($"Loc: Localization/cards.{language} fehlt."); return table; }
            Loc.CardEntry entry = null;
            foreach (var raw in asset.text.Replace("\r", "").Split('\n'))
            {
                if (raw.Length == 0 || raw[0] == '#' && !raw.StartsWith("## ")) continue;
                if (raw.StartsWith("## "))
                {
                    entry = new Loc.CardEntry();
                    table[raw.Substring(3).Trim()] = entry;
                    continue;
                }
                if (entry == null) continue;
                int space = raw.IndexOf(' ');
                if (space <= 0) continue;
                string field = raw.Substring(0, space);
                string value = raw.Substring(space + 1).Trim();
                if (value.Length == 0) continue;
                if (field == "n") entry.name = value;
                else if (field == "s") entry.summon = value;
                else if (field[0] == 'l' && int.TryParse(field.Substring(1), out int li)) entry.labels[li] = value;
                else if (field[0] == 't' && int.TryParse(field.Substring(1), out int ti)) entry.texts[ti] = value;
            }
            return table;
        }

        // ================== CJK-Schrift ==================

        /// <summary>
        /// Baut die Laufzeit-Schrift aus einer installierten System-Schrift (YaHei
        /// deckt CJK UND Kyrillisch ab) und hängt sie als globalen TMP-Fallback ein.
        /// Nichts wird ins Projekt geschrieben; im Editor wird der Eintrag beim
        /// Play-Ende wieder entfernt (Application.quitting). Public, damit die
        /// Sprachzeile im Settings-Menü „简体中文“ auch dann zeichnen kann, wenn
        /// gerade eine andere Sprache aktiv ist (sonst nur Ersatz-Vierecke).
        /// </summary>
        public static void EnsureRuntimeFallback()
        {
            if (cjkFont == null)
            {
                cjkFont = CreateCjkFontAsset();
                if (cjkFont == null) { Debug.LogWarning("Loc: keine CJK-Systemschrift gefunden — chinesischer Text zeigt Ersatzzeichen."); return; }
                // Projekt-Erfahrung: Glyphen auf Atlas-Textur 2 werden leer gezeichnet —
                // ein einzelner 4096er-Atlas reicht für tausende Zeichen pro Sitzung.
                cjkFont.isMultiAtlasTexturesEnabled = false;
                cjkFont.name = "CJK Dynamic (runtime)";
            }
            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks != null && !fallbacks.Contains(cjkFont)) fallbacks.Add(cjkFont);
        }

        private static void RemoveFallback()
        {
            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (cjkFont != null && fallbacks != null) fallbacks.Remove(cjkFont);
        }

        /// <summary>
        /// CJK-Schrift direkt aus der Font-DATEI des Systems bauen (die Font-Objekt-
        /// Überladung kann die Face-Daten von OS-Fonts nicht laden). YaHei liegt auf
        /// jedem Windows seit Vista; danach ein paar Ausweichkandidaten.
        /// </summary>
        private static TMP_FontAsset CreateCjkFontAsset()
        {
            string windir = System.Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
            string[] candidates =
            {
                windir + @"\Fonts\msyh.ttc",     // Microsoft YaHei (Win 10/11)
                windir + @"\Fonts\msyh.ttf",     // ältere Windows-Versionen
                windir + @"\Fonts\simhei.ttf",
                windir + @"\Fonts\simsun.ttc",
                windir + @"\Fonts\Deng.ttf",     // DengXian
                "/System/Library/Fonts/PingFang.ttc",                       // macOS
                "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",   // Linux
            };
            foreach (var path in candidates)
            {
                if (!System.IO.File.Exists(path)) continue;
                var asset = TMP_FontAsset.CreateFontAsset(path, 0, 48, 6,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 4096, 4096);
                if (asset != null) return asset;
            }
            // Letzter Versuch über den Familiennamen (kleinerer Standard-Atlas)
            foreach (var family in new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimSun" })
            {
                var asset = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);
                if (asset != null) return asset;
            }
            return null;
        }

        // ================== Szenen-Sweep ==================

        /// <summary>
        /// Übersetzt alle statischen TMP-Labels der frisch geladenen Szene, deren
        /// Text exakt einem Tabellen-Eintrag entspricht. Läuft nur bei aktiver
        /// Fremdsprache; zur Laufzeit neu gesetzte Texte gehen über Loc.T an der
        /// Quelle.
        /// </summary>
        public static void SweepSceneLabels()
        {
            if (!Loc.Active) return;
            foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Der Schriftzug des Spiels bleibt in jeder Sprache RELIQUARY
                if (label.name.Contains("Wordmark") || label.name.Contains("Logo")) continue;
                var text = label.text;
                if (string.IsNullOrEmpty(text)) continue;
                var translated = Loc.T(text.Trim());
                if (!ReferenceEquals(translated, text.Trim()) && translated != text.Trim())
                    label.text = translated;
            }
        }
    }
}
