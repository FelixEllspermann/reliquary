using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Import-Preset für Karten-Artworks: 512er-Limit + Crunch-Kompression.
    /// Die Quell-PNGs (1216×832, ~1.5 MB) sind für Kartengröße weit überdimensioniert —
    /// unkomprimiert luden ~750 Artworks beim Öffnen des Deck Builders rund 1 GB
    /// Texturdaten in einem Rutsch. Mit 512+Crunch schrumpft das auf einen Bruchteil,
    /// und Unity cached das Ergebnis in der Library (einmalig importieren, danach schnell).
    /// OnPreprocessTexture greift automatisch für jedes NEUE Artwork im Art-Ordner.
    /// </summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/_Game/Art";
        private const int MaxSize = 512;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;
            var importer = (TextureImporter)assetImporter;
            Apply(importer);
        }

        private static void Apply(TextureImporter importer)
        {
            importer.maxTextureSize = MaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = 50;
            importer.mipmapEnabled = false; // reine UI-Sprites, nie in 3D-Distanz
        }

        [MenuItem("Rouge/Card Design/Artwork-Import optimieren (512 + Crunch)")]
        public static void RetuneExistingArtworks()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
            int changed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    if (importer.maxTextureSize == MaxSize && importer.crunchedCompression) continue;
                    Apply(importer);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
            Debug.Log($"[ArtImport] {changed} von {guids.Length} Artworks auf {MaxSize}px + Crunch umgestellt.");
        }
    }
}
