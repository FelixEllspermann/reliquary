using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Zuschneide-Helfer für die Tutorial-Bilder (Resources/Tutorial): nimmt einen
    /// Vollbild-Screenshot und schreibt einen Ausschnitt als PNG ins Projekt.
    /// Koordinaten im Pixelraum des Screenshots, y von OBEN gezählt (wie im
    /// Bildbetrachter) — Unity-Texturen zählen von unten, das rechnet der Helfer um.
    /// Optional wird der Ausschnitt auf eine Zielbreite skaliert (bilinear).
    /// </summary>
    public static class TutorialShots
    {
        public static string Crop(string sourcePng, int x, int yFromTop, int width, int height, string assetPath, int targetWidth = 0)
        {
            if (!File.Exists(sourcePng)) return "missing source " + sourcePng;
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(sourcePng));

            x = Mathf.Clamp(x, 0, source.width - 1);
            yFromTop = Mathf.Clamp(yFromTop, 0, source.height - 1);
            width = Mathf.Clamp(width, 1, source.width - x);
            height = Mathf.Clamp(height, 1, source.height - yFromTop);
            int yFromBottom = source.height - yFromTop - height;

            var pixels = source.GetPixels(x, yFromBottom, width, height);
            var crop = new Texture2D(width, height, TextureFormat.RGBA32, false);
            crop.SetPixels(pixels);
            crop.Apply();

            if (targetWidth > 0 && targetWidth < width)
            {
                int targetHeight = Mathf.RoundToInt(height * (targetWidth / (float)width));
                var scaled = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
                for (int py = 0; py < targetHeight; py++)
                    for (int px = 0; px < targetWidth; px++)
                        scaled.SetPixel(px, py, crop.GetPixelBilinear((px + 0.5f) / targetWidth, (py + 0.5f) / targetHeight));
                scaled.Apply();
                Object.DestroyImmediate(crop);
                crop = scaled;
            }

            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(assetPath, crop.EncodeToPNG());
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(crop);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return $"{assetPath} {width}x{height}" + (targetWidth > 0 ? $" -> {targetWidth}px wide" : "");
        }
    }
}
