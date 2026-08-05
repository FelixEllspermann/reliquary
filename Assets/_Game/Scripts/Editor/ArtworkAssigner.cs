using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Ordnet PNGs/JPGs aus Assets/_Game/Art automatisch den Karten im Katalog zu.
    /// Toleriert lockere Dateinamen ("FlameWhelp Art Work.png", "Nimble GOblin.png"):
    /// verglichen wird nur Kleinbuchstaben+Ziffern, Füllwörter (art/work/of/the) fliegen raus.
    /// Karten, die schon ein Artwork haben, werden nie angefasst.
    /// </summary>
    public static class ArtworkAssigner
    {
        private const string ArtFolder = "Assets/_Game/Art";
        private const string CatalogPath = "Assets/_Game/Data/Tcg/CardCatalog.asset";

        [MenuItem("Rouge/Card Design/Artworks automatisch zuweisen")]
        public static void AssignAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            if (catalog == null) { Debug.LogError($"Katalog nicht gefunden: {CatalogPath}"); return; }

            var files = Directory.GetFiles(ArtFolder)
                .Where(p => p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".jpeg"))
                .Select(p => p.Replace('\\', '/'))
                .ToList();
            var byKey = new Dictionary<string, string>();
            foreach (var path in files)
            {
                string key = Normalize(Path.GetFileNameWithoutExtension(path));
                if (!string.IsNullOrEmpty(key)) byKey[key] = path;
            }

            int assigned = 0;
            var missing = new List<string>();
            foreach (var card in catalog.cards)
            {
                if (card == null || card.artwork != null) continue;

                string cardKey = Normalize(card.cardName);
                string match = byKey.TryGetValue(cardKey, out var exact) ? exact : null;
                if (match == null)
                {
                    var candidates = byKey.Where(kv => kv.Key.Contains(cardKey) || cardKey.Contains(kv.Key))
                                          .Select(kv => kv.Value).ToList();
                    if (candidates.Count == 1) match = candidates[0];
                    else if (candidates.Count > 1)
                    {
                        Debug.LogWarning($"Mehrdeutig für '{card.cardName}': {string.Join(", ", candidates.Select(Path.GetFileName))} — übersprungen.");
                        continue;
                    }
                }
                if (match == null) { missing.Add(card.cardName); continue; }

                var sprite = LoadAsSprite(match);
                if (sprite == null) { Debug.LogWarning($"Kein Sprite ladbar: {match}"); continue; }
                card.artwork = sprite;
                EditorUtility.SetDirty(card);
                assigned++;
                Debug.Log($"Artwork zugewiesen: {card.cardName} ← {Path.GetFileName(match)}");
            }

            AssetDatabase.SaveAssets();
            string missingInfo = missing.Count > 0 ? $" Ohne Artwork-Datei ({missing.Count}): {string.Join(", ", missing)}" : "";
            Debug.Log($"ArtworkAssigner: {assigned} zugewiesen.{missingInfo}");
        }

        /// <summary>Stellt sicher, dass die Textur als Sprite importiert ist, und lädt sie.</summary>
        private static Sprite LoadAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static string Normalize(string s)
        {
            string lowered = new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            foreach (var filler in new[] { "artwork", "art", "work", "ofthe", "the" })
                lowered = lowered.Replace(filler, "");
            return lowered;
        }
    }
}
