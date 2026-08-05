using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Legt steam_appid.txt neben die gebaute .exe. Ohne diese Datei findet die
    /// Steam-API die App-ID nicht und der Steam-Anmeldeweg bleibt im Build aus —
    /// ein Fehler, den man beim ersten Release garantiert einmal macht.
    ///
    /// Nach der Veröffentlichung auf Steam wird die Datei nicht mehr gebraucht
    /// (Steam liefert die App-ID dann selbst) und sollte aus dem Release-Paket
    /// entfernt werden.
    /// </summary>
    public class SteamBuildPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            string source = Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "steam_appid.txt");
            if (!File.Exists(source))
            {
                Debug.LogWarning("[Steam] steam_appid.txt fehlt in der Projektwurzel — "
                    + "der Build startet die Steam-API nicht.");
                return;
            }

            string outputDir = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(outputDir)) return;

            string destination = Path.Combine(outputDir, "steam_appid.txt");
            File.Copy(source, destination, true);
            Debug.Log($"[Steam] steam_appid.txt (App-ID {File.ReadAllText(source).Trim()}) "
                + "neben die .exe kopiert.");
        }
    }
}
