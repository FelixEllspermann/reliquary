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
        private static TMP_FontAsset koreanFont;
        private static TMP_FontAsset japaneseFont;
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
                SceneManager.sceneLoaded += (_, _) => { HardenSceneFonts(); SweepSceneLabels(); ScheduleLateSweep(); };
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
                case "portuguese":              // Portugal …
                case "brazilian": return Loc.Portuguese;   // … und Brasilien: eine Fassung
                case "koreana": return Loc.Korean;   // so heißt Koreanisch bei Steam wirklich
                case "japanese": return Loc.Japanese;
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
                case SystemLanguage.Portuguese: return Loc.Portuguese;
                case SystemLanguage.Korean: return Loc.Korean;
                case SystemLanguage.Japanese: return Loc.Japanese;
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
            // Chinesisch/Russisch, Koreanisch und Japanisch brauchen je ihre
            // Laufzeit-Schrift: CJK fehlt allen Projekt-Fonts, Kyrillisch fehlt
            // Cinzel, Hangul trägt nicht einmal YaHei (Malgun Gothic springt ein),
            // und Japanisch bekommt Meiryo/Yu Gothic — YaHei würde Kanji nur in
            // chinesischen Glyphenformen zeichnen. Die lateinischen Sprachen
            // kommen ohne aus (Umlaute, Akzente, œ, « » tragen die Dynamic-Fonts).
            RemoveFallback();
            if (language == Loc.ChineseSimplified || language == Loc.Russian) EnsureRuntimeFallback();
            else if (language == Loc.Korean) EnsureKoreanFallback();
            else if (language == Loc.Japanese) EnsureJapaneseFallback();
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
                cjkFont = CreateRuntimeFontAsset(CjkFontFiles, CjkFontFamilies, "CJK Dynamic (runtime)");
                if (cjkFont == null) { Debug.LogWarning("Loc: keine CJK-Systemschrift gefunden — chinesischer Text zeigt Ersatzzeichen."); return; }
            }
            AddFallback(cjkFont);
        }

        /// <summary>
        /// Hangul trägt weder eine Projekt-Schrift noch YaHei — Koreanisch bekommt
        /// seinen eigenen Laufzeit-Fallback aus Malgun Gothic. Public aus demselben
        /// Grund wie EnsureRuntimeFallback: die Sprachzeile zeigt „한국어“ auch,
        /// wenn gerade eine andere Sprache aktiv ist.
        /// </summary>
        public static void EnsureKoreanFallback()
        {
            if (koreanFont == null)
            {
                koreanFont = CreateRuntimeFontAsset(KoreanFontFiles, KoreanFontFamilies, "Hangul Dynamic (runtime)");
                if (koreanFont == null) { Debug.LogWarning("Loc: keine Hangul-Systemschrift gefunden — koreanischer Text zeigt Ersatzzeichen."); return; }
            }
            AddFallback(koreanFont);
        }

        /// <summary>
        /// Japanisch bekommt seine eigene Laufzeit-Schrift aus Meiryo/Yu Gothic —
        /// YaHei hätte zwar alle Kanji, aber in chinesischen Glyphenformen.
        /// Public aus demselben Grund wie die anderen: die Sprachzeile zeigt
        /// „日本語“ auch, wenn gerade eine andere Sprache aktiv ist.
        /// </summary>
        public static void EnsureJapaneseFallback()
        {
            if (japaneseFont == null)
            {
                japaneseFont = CreateRuntimeFontAsset(JapaneseFontFiles, JapaneseFontFamilies, "Japanese Dynamic (runtime)");
                if (japaneseFont == null) { Debug.LogWarning("Loc: keine japanische Systemschrift gefunden — japanischer Text zeigt Ersatzzeichen."); return; }
            }
            AddFallback(japaneseFont);
        }

        private static void AddFallback(TMP_FontAsset font)
        {
            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks != null && !fallbacks.Contains(font)) fallbacks.Add(font);
        }

        private static void RemoveFallback()
        {
            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null) return;
            if (cjkFont != null) fallbacks.Remove(cjkFont);
            if (koreanFont != null) fallbacks.Remove(koreanFont);
            if (japaneseFont != null) fallbacks.Remove(japaneseFont);
        }

        // Dateinamen ohne Pfad werden unter %WINDIR%\Fonts gesucht; absolute Pfade
        // (macOS/Linux) bleiben wie sie sind. NotoSansCJK deckt beide Fälle ab.
        private static readonly string[] CjkFontFiles =
        {
            "msyh.ttc",     // Microsoft YaHei (Win 10/11)
            "msyh.ttf",     // ältere Windows-Versionen
            "simhei.ttf",
            "simsun.ttc",
            "Deng.ttf",     // DengXian
            "/System/Library/Fonts/PingFang.ttc",                       // macOS
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",   // Linux
        };
        private static readonly string[] CjkFontFamilies = { "Microsoft YaHei", "Microsoft YaHei UI", "SimSun" };

        private static readonly string[] KoreanFontFiles =
        {
            "malgun.ttf",   // Malgun Gothic (Windows-Standard für Koreanisch)
            "gulim.ttc",
            "batang.ttc",
            "/System/Library/Fonts/AppleSDGothicNeo.ttc",               // macOS
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",   // Linux
        };
        private static readonly string[] KoreanFontFamilies = { "Malgun Gothic", "Gulim", "Batang" };

        private static readonly string[] JapaneseFontFiles =
        {
            "YuGothM.ttc",  // Yu Gothic Medium (Win 10/11)
            "meiryo.ttc",   // Meiryo (Vista+)
            "msgothic.ttc",
            "/System/Library/Fonts/Hiragino Sans GB.ttc",               // macOS
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",   // Linux
        };
        private static readonly string[] JapaneseFontFamilies = { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic" };

        /// <summary>
        /// Laufzeit-Schrift direkt aus der Font-DATEI des Systems bauen (die
        /// Font-Objekt-Überladung kann die Face-Daten von OS-Fonts nicht laden).
        /// Projekt-Erfahrung: Glyphen auf Atlas-Textur 2 werden leer gezeichnet —
        /// deshalb EIN dynamischer 4096er-Atlas, Multi-Atlas AUS; der reicht für
        /// tausende Zeichen pro Sitzung.
        /// </summary>
        private static TMP_FontAsset CreateRuntimeFontAsset(string[] files, string[] families, string assetName)
        {
            string windir = System.Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
            foreach (var file in files)
            {
                string path = file.IndexOf('/') >= 0 ? file : windir + @"\Fonts\" + file;
                if (!System.IO.File.Exists(path)) continue;
                var asset = TMP_FontAsset.CreateFontAsset(path, 0, 48, 6,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 4096, 4096);
                if (asset == null) continue;
                asset.isMultiAtlasTexturesEnabled = false;
                asset.name = assetName;
                return asset;
            }
            // Letzter Versuch über den Familiennamen (kleinerer Standard-Atlas)
            foreach (var family in families)
            {
                var asset = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);
                if (asset == null) continue;
                asset.isMultiAtlasTexturesEnabled = false;
                asset.name = assetName;
                return asset;
            }
            return null;
        }

        // ================== Atlas-Härtung ==================

        private static readonly HashSet<int> hardenedFonts = new HashSet<int>();

        /// <summary>
        /// Multi-Atlas für jede dynamische Schrift der Szene abschalten (samt
        /// Fallback-Ketten). Grund ist dieselbe Projekt-Erfahrung wie oben: TMP
        /// zeichnet Glyphen auf Atlas-Textur 2 leer — Menü-Knöpfe wie CLOSE/NEW
        /// CARDS blieben sporadisch ohne Beschriftung, sobald ihre spät
        /// angefragten Glyphen erst auf der zweiten Seite landeten. Die
        /// Projekt-Fonts sind dafür auf 2048er-Atlanten vergrößert.
        /// </summary>
        private static void HardenSceneFonts()
        {
            foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                HardenFont(label.font);
            if (TMP_Settings.defaultFontAsset != null)
                HardenFont(TMP_Settings.defaultFontAsset);
        }

        private static void HardenFont(TMP_FontAsset font)
        {
            if (font == null || !hardenedFonts.Add(font.GetInstanceID())) return;
            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                font.isMultiAtlasTexturesEnabled = false;
            var fallbacks = font.fallbackFontAssetTable;
            if (fallbacks == null) return;
            foreach (var fallback in fallbacks) HardenFont(fallback);
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
