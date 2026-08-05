using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Generiert alle Texturen des "Reliquary"-Kartendesigns (design_handoff_tcg_card_system)
    /// pixelgenau aus den Design-Tokens sowie die TMP-FontAssets und das CardSkin-Asset.
    /// Menü: Rouge/Card Design/Generate Assets.
    /// </summary>
    public static class CardDesignGenerator
    {
        private const int W = 480;
        private const int H = 672;
        private const string ArtDir = "Assets/_Game/Art/CardFrame";
        private const string FontDir = "Assets/_Game/Fonts";
        private const string SkinPath = "Assets/_Game/Data/Tcg/CardSkin.asset";

        // ---------- Paletten (Design-Tokens, README "Card type palettes") ----------
        private class Palette
        {
            public Color keyline, bodyTop, bodyMid, bodyBottom;
            public Color plateTop, plateBottom;
            public Color crestLight, crestDark, crestInnerTop, crestInnerBottom;
            public Color frameTop, frameBottom;
            public Color badgeTop, badgeBottom;
            public Color effectBorder;
            public Color statTop, statBottom;
            public bool hasStats = true;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        private static Palette Monster() => new Palette
        {
            keyline = Hex("#C8A45C"), bodyTop = Hex("#332315"), bodyMid = Hex("#150D07"), bodyBottom = Hex("#251809"),
            plateTop = Hex("#42301C"), plateBottom = Hex("#22150A"),
            crestLight = Hex("#EBCE8A"), crestDark = Hex("#8E6A22"), crestInnerTop = Hex("#3B2A10"), crestInnerBottom = Hex("#180F04"),
            frameTop = Hex("#3E2C16"), frameBottom = Hex("#1A1108"),
            badgeTop = Hex("#E2C685"), badgeBottom = Hex("#9C7526"),
            effectBorder = Hex("#8C7440"),
            statTop = Hex("#2A1D0E"), statBottom = Hex("#140C05")
        };

        private static Palette Spell() => new Palette
        {
            keyline = Hex("#8FC6D2"), bodyTop = Hex("#17323A"), bodyMid = Hex("#07161A"), bodyBottom = Hex("#122A31"),
            plateTop = Hex("#1E3A40"), plateBottom = Hex("#0E2126"),
            crestLight = Hex("#B4E2EC"), crestDark = Hex("#3E7A88"), crestInnerTop = Hex("#132E35"), crestInnerBottom = Hex("#050F12"),
            frameTop = Hex("#20424B"), frameBottom = Hex("#0A1A1F"),
            badgeTop = Hex("#A5D8E2"), badgeBottom = Hex("#3B7C8B"),
            effectBorder = Hex("#4C7B87"),
            hasStats = false
        };

        private static Palette Artifact() => new Palette
        {
            keyline = Hex("#B9A3E0"), bodyTop = Hex("#241C3C"), bodyMid = Hex("#0D0916"), bodyBottom = Hex("#1D1633"),
            plateTop = Hex("#2E2545"), plateBottom = Hex("#171029"),
            crestLight = Hex("#D6C4F5"), crestDark = Hex("#6A4FA8"), crestInnerTop = Hex("#241C3A"), crestInnerBottom = Hex("#0C0916"),
            frameTop = Hex("#332A50"), frameBottom = Hex("#120E20"),
            badgeTop = Hex("#C2AEEC"), badgeBottom = Hex("#5F4699"),
            effectBorder = Hex("#6A5A93"),
            statTop = Hex("#221A38"), statBottom = Hex("#100B1C")
        };

        /// <summary>Player-Karten: Monster-Gold, aber Spell-Geometrie (kein Stat-Row, großes Textfeld).</summary>
        private static Palette Player()
        {
            var palette = Monster();
            palette.hasStats = false;
            return palette;
        }

        /// <summary>Reliquary (Extra Deck): weißes Ivory-Chassis mit Gold-Keyline, dunkle Tinte.</summary>
        private static Palette Reliquary() => new Palette
        {
            keyline = Hex("#C8A45C"), bodyTop = Hex("#F8F1E0"), bodyMid = Hex("#EFE5CC"), bodyBottom = Hex("#F4ECD8"),
            plateTop = Hex("#FBF6EA"), plateBottom = Hex("#E9DCBD"),
            crestLight = Hex("#F5E7C2"), crestDark = Hex("#C8A45C"), crestInnerTop = Hex("#FBF6EA"), crestInnerBottom = Hex("#E6D9B8"),
            frameTop = Hex("#EFE6CF"), frameBottom = Hex("#D9CCA9"),
            badgeTop = Hex("#8A6E35"), badgeBottom = Hex("#5A431C"),
            effectBorder = Hex("#B39D6E"),
            statTop = Hex("#EFE5CC"), statBottom = Hex("#DFD2AC")
        };

        private static readonly Color Parchment0 = Hex("#EBE1C7");
        private static readonly Color Parchment1 = Hex("#D9CCAB");

        // ---------- Zeichen-Helfer ----------

        private static float RoundedRectSdf(float x, float y, float cx, float cy, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(x - cx) - (halfW - radius);
            float qy = Mathf.Abs(y - cy) - (halfH - radius);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>CSS-linear-gradient: 0deg = nach oben, im Uhrzeigersinn; y wächst nach unten.</summary>
        private static float GradientT(float x, float y, float w, float h, float cssAngleDeg)
        {
            float rad = cssAngleDeg * Mathf.Deg2Rad;
            float dx = Mathf.Sin(rad), dy = -Mathf.Cos(rad);
            float half = 0.5f * (Mathf.Abs(w * dx) + Mathf.Abs(h * dy));
            if (half < 0.0001f) return 0.5f;
            float t = ((x - w * 0.5f) * dx + (y - h * 0.5f) * dy) / (2f * half) + 0.5f;
            return Mathf.Clamp01(t);
        }

        private static Color ThreeStop(float t, Color a, Color mid, float midPos, Color b)
        {
            return t < midPos ? Color.Lerp(a, mid, t / midPos) : Color.Lerp(mid, b, (t - midPos) / (1f - midPos));
        }

        private static void Blend(Color[] px, int w, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w) return;
            int i = y * w + x;
            if (i < 0 || i >= px.Length) return;
            var dst = px[i];
            float a = c.a + dst.a * (1f - c.a);
            if (a <= 0f) { px[i] = Color.clear; return; }
            px[i] = new Color(
                (c.r * c.a + dst.r * dst.a * (1f - c.a)) / a,
                (c.g * c.a + dst.g * dst.a * (1f - c.a)) / a,
                (c.b * c.a + dst.b * dst.a * (1f - c.a)) / a, a);
        }

        private static bool InChamfer(float lx, float ly, float w, float h, float chamfer)
        {
            float inset = chamfer * (ly / h);
            return lx >= inset && lx <= w - inset;
        }

        private static bool InHex(float lx, float ly, float w, float h)
        {
            // clip-path: 50% 0, 100% 20%, 100% 66%, 50% 100%, 0 66%, 0 20%
            var p = new Vector2(lx / w, ly / h);
            Vector2[] poly =
            {
                new Vector2(.5f, 0f), new Vector2(1f, .20f), new Vector2(1f, .66f),
                new Vector2(.5f, 1f), new Vector2(0f, .66f), new Vector2(0f, .20f)
            };
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (poly[i].y > p.y != poly[j].y > p.y &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>
        /// Rasterisiert eine Fläche mit sub×sub Unterabtastungen je Pixel. `shade` liefert
        /// die Farbe an einer Subpixel-Stelle oder Color.clear für "hier ist nichts".
        /// <para>
        /// Ohne das bekommt jede schräge Kante — Sechseck, Ellipse, Diamant — harte
        /// Treppenstufen, weil der Innen-/Außen-Test binär ist. Genau die sieht man
        /// auf der Karte als "verpixelt", und zwar unabhängig von der Auflösung.
        /// </para>
        /// flipY dreht auf Unitys Koordinaten (y wächst nach oben); wo bestehende
        /// Grafiken ohne Drehung entstanden sind, bleibt es aus, damit sich ihr
        /// Aussehen nicht ändert.
        /// </summary>
        private static Texture2D Rasterize(int w, int h, int sub, System.Func<float, float, Color> shade, bool flipY = true)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            float total = sub * sub;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = 0f, g = 0f, b = 0f, weight = 0f;
                    for (int sy = 0; sy < sub; sy++)
                        for (int sx = 0; sx < sub; sx++)
                        {
                            var c = shade(x + (sx + 0.5f) / sub, y + (sy + 0.5f) / sub);
                            if (c.a <= 0f) continue;
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a;
                            weight += c.a;
                        }
                    if (weight <= 0f) continue;
                    px[(flipY ? h - 1 - y : y) * w + x] = new Color(r / weight, g / weight, b / weight, weight / total);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---------- Chassis ----------

        private static Texture2D BuildChassis(Palette pal)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];

            int effectH = pal.hasStats ? 128 : 188;
            var effectRect = new Rect(39, 470, 402, effectH);
            var dmgRect = new Rect(39, 602, 198, 56);
            var defRect = new Rect(243, 602, 198, 56);
            var plateRect = new Rect(39, 14, 350, 51);
            var frameRect = new Rect(59, 70, 362, 362);
            var innerArtRect = new Rect(68, 79, 342, 342);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, W / 2f, H / 2f, W / 2f, H / 2f, 12f);
                    if (d > 0f) continue; // außerhalb der Karte: transparent

                    // Body-Verlauf 165°
                    Color c = ThreeStop(GradientT(fx, fy, W, H, 165f), pal.bodyTop, pal.bodyMid, 0.55f, pal.bodyBottom);

                    // Innerer dekorativer Keyline-Rahmen (inset 6, r7, 40%)
                    float dInner = RoundedRectSdf(fx, fy, W / 2f, H / 2f, W / 2f - 6f, H / 2f - 6f, 7f);
                    if (dInner > -1f && dInner <= 0f)
                        c = Color.Lerp(c, pal.keyline, 0.40f);

                    // Rivets: Diamanten 13x13 bei 12px Abstand
                    foreach (var corner in new[] { new Vector2(18.5f, 18.5f), new Vector2(W - 18.5f, 18.5f), new Vector2(18.5f, H - 18.5f), new Vector2(W - 18.5f, H - 18.5f) })
                        if (Mathf.Abs(fx - corner.x) + Mathf.Abs(fy - corner.y) <= 13f * 0.7071f)
                            c = pal.keyline;

                    // Name-Plate (Chamfer-Trapez)
                    if (plateRect.Contains(new Vector2(fx, fy)))
                    {
                        float lx = fx - plateRect.x, ly = fy - plateRect.y;
                        if (InChamfer(lx, ly, plateRect.width, plateRect.height, 14f))
                        {
                            float t = ly / plateRect.height;
                            c = Color.Lerp(pal.plateTop, pal.plateBottom, t);
                            if (ly < 1f || ly > plateRect.height - 1f) c = pal.keyline;
                        }
                    }

                    // Artwork-Rahmen (362, padding 9, 2px Keyline außen, 1px 65% innen)
                    if (frameRect.Contains(new Vector2(fx, fy)))
                    {
                        float lx = fx - frameRect.x, ly = fy - frameRect.y;
                        c = Color.Lerp(pal.frameTop, pal.frameBottom, GradientT(lx, ly, 362, 362, 160f));
                        if (lx < 2f || ly < 2f || lx > 360f || ly > 360f) c = pal.keyline;
                        if (innerArtRect.Contains(new Vector2(fx, fy)))
                        {
                            c = pal.frameBottom; // Fallback-Fläche, Artwork liegt darüber
                            float ix = fx - innerArtRect.x, iy = fy - innerArtRect.y;
                            if (ix < 1f || iy < 1f || ix > 341f || iy > 341f)
                                c = Color.Lerp(c, pal.keyline, 0.65f);
                        }
                    }

                    // Effekt-Panel (Pergament + 1px Typ-Border)
                    if (effectRect.Contains(new Vector2(fx, fy)))
                    {
                        float ly = fy - effectRect.y;
                        c = Color.Lerp(Parchment0, Parchment1, ly / effectRect.height);
                        float lx = fx - effectRect.x;
                        if (lx < 1f || ly < 1f || lx > effectRect.width - 1f || ly > effectRect.height - 1f)
                            c = pal.effectBorder;
                    }

                    // Stat-Boxen
                    if (pal.hasStats)
                    {
                        if (dmgRect.Contains(new Vector2(fx, fy)))
                        {
                            float lx = fx - dmgRect.x, ly = fy - dmgRect.y;
                            c = Color.Lerp(pal.statTop, pal.statBottom, ly / dmgRect.height);
                            if (lx < 1f || ly < 1f || lx > dmgRect.width - 1f || ly > dmgRect.height - 1f)
                                c = pal.keyline;
                        }
                        if (defRect.Contains(new Vector2(fx, fy)))
                        {
                            float lx = fx - defRect.x, ly = fy - defRect.y;
                            c = Color.Lerp(pal.statTop, pal.statBottom, ly / defRect.height);
                            if (lx < 1f || ly < 1f || lx > defRect.width - 1f || ly > defRect.height - 1f)
                                c = Color.Lerp(c, pal.keyline, 0.45f);
                        }
                    }

                    // Außenkanten zuletzt: 2px Keyline + 1px dunkle Innenkante
                    if (d > -2f) c = pal.keyline;
                    else if (d > -3f) c = Color.Lerp(c, Color.black, 0.5f);

                    px[(H - 1 - y) * W + x] = c; // Unity: y nach oben
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---------- Rückseite ----------

        private static Texture2D BuildBack()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            Color gold = Hex("#C8A45C");
            Color baseIn = Hex("#4E2A18"), baseOut = Hex("#1C0E08");
            float cx = W / 2f, cy = H / 2f;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, cx, cy, W / 2f, H / 2f, 12f);
                    if (d > 0f) continue;

                    // Radialer Grundverlauf (Ellipse, Stop bei 78%)
                    float rx = (fx - cx) / (W / 2f), ry = (fy - cy) / (H / 2f);
                    float r = Mathf.Sqrt(rx * rx + ry * ry);
                    Color c = Color.Lerp(baseIn, baseOut, Mathf.Clamp01(r / 0.78f));

                    // Webmuster ±45°
                    var weave = new Color(gold.r, gold.g, gold.b, 0.13f);
                    float m1 = ((fx + fy) % 20f + 20f) % 20f;
                    float m2 = ((fx - fy) % 20f + 20f) % 20f;
                    if (m1 < 1f) c = Color.Lerp(c, gold, weave.a);
                    if (m2 < 1f) c = Color.Lerp(c, gold, weave.a);

                    // Doppelte Keyline
                    float d10 = RoundedRectSdf(fx, fy, cx, cy, W / 2f - 10f, H / 2f - 10f, 6f);
                    if (d10 > -1f && d10 <= 0f) c = Color.Lerp(c, gold, 0.55f);
                    float d16 = RoundedRectSdf(fx, fy, cx, cy, W / 2f - 16f, H / 2f - 16f, 4f);
                    if (d16 > -1f && d16 <= 0f) c = Color.Lerp(c, gold, 0.22f);

                    // Zentrales Ornament
                    float manhattan = Mathf.Abs(fx - cx) + Mathf.Abs(fy - cy);
                    float diamond230 = 230f * 0.7071f;
                    if (manhattan <= diamond230 && manhattan > diamond230 - 2.83f) c = Color.Lerp(c, gold, 0.6f);
                    float ax = Mathf.Abs(fx - cx), ay = Mathf.Abs(fy - cy);
                    if (ax <= 115f && ay <= 115f && (ax > 114f || ay > 114f)) c = Color.Lerp(c, gold, 0.3f);
                    float diamond120 = 120f * 0.7071f;
                    if (manhattan <= diamond120)
                    {
                        float t = GradientT(fx - (cx - 60f), fy - (cy - 60f), 120f, 120f, 135f);
                        c = Color.Lerp(c, gold, Mathf.Lerp(0.35f, 0.05f, t));
                        if (manhattan > diamond120 - 1.41f) c = Color.Lerp(c, gold, 0.7f);
                    }
                    float diamond46 = 46f * 0.7071f;
                    if (manhattan <= diamond46)
                    {
                        float t = GradientT(fx - (cx - 23f), fy - (cy - 23f), 46f, 46f, 135f);
                        c = Color.Lerp(Hex("#E6CD8F"), Hex("#7A5A1E"), t);
                    }

                    // Außenrand
                    if (d > -2f) c = gold;

                    px[(H - 1 - y) * W + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---------- Kleinere Elemente ----------

        /// <summary>
        /// Level-Wappen. Die Design-Größe ist 44×48, gezeichnet wird in <see cref="CrestScale"/>×
        /// davon: das Sechseck sitzt auf der Heldenkarte und in der Münzwurf-Auswahl größer
        /// als seine Vorlage, und selbst bei 1:1 zeigen die schrägen Kanten Stufen.
        /// </summary>
        private const int CrestScale = 4;

        private static Texture2D BuildCrest(Palette pal)
        {
            const int w = 44 * CrestScale, h = 48 * CrestScale;
            const float inset = 2f * CrestScale;
            const float innerW = w - inset * 2f, innerH = h - inset * 2f;
            return Rasterize(w, h, 4, (fx, fy) =>
            {
                if (!InHex(fx, fy, w, h)) return Color.clear;
                if (InHex(fx - inset, fy - inset, innerW, innerH))
                    return Color.Lerp(pal.crestInnerTop, pal.crestInnerBottom,
                                      GradientT(fx - inset, fy - inset, innerW, innerH, 160f));
                return Color.Lerp(pal.crestLight, pal.crestDark, GradientT(fx, fy, w, h, 160f));
            });
        }

        private static Texture2D BuildBadge(Palette pal)
        {
            const int w = 8 * CrestScale, h = 29 * CrestScale;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(pal.badgeTop, pal.badgeBottom, (y + 0.5f) / h);
                for (int x = 0; x < w; x++) px[(h - 1 - y) * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildVignette()
        {
            const int s = 342;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, s - 1 - x), Mathf.Min(y, s - 1 - y));
                    float a = Mathf.Clamp01(1f - edge / 40f) * 0.5f;
                    px[y * s + x] = new Color(0f, 0f, 0f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildWhiteFrame()
        {
            const int s = 12;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = (x == 0 || y == 0 || x == s - 1 || y == s - 1) ? Color.white : Color.clear;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildWhiteSquare()
        {
            const int s = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---------- Asset-Pipeline ----------

        private static Sprite SaveSprite(Texture2D tex, string name, Vector4 border = default)
        {
            Directory.CreateDirectory(ArtDir);
            string path = $"{ArtDir}/{name}.png";
            int longestSide = Mathf.Max(tex.width, tex.height);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Keine Mipmaps: die UI zeichnet nahezu 1:1, und Unity greift trotzdem
            // eine kleinere Stufe ab — das ist die Unschärfe, die man als "verpixelt"
            // sieht. Kleine Grafiken bleiben unkomprimiert, weil DXT in 4×4-Blöcken
            // rechnet und auf feinen Verläufen und Kanten Klötzchen hinterlässt.
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = border;
            if (longestSide <= 900)
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset MakeFont(string ttfName, string assetName, int atlasSize)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{ttfName}.ttf");
            if (font == null) { Debug.LogError($"TTF fehlt: {FontDir}/{ttfName}.ttf"); return null; }
            string path = $"{FontDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, atlasSize, atlasSize);
            fontAsset.name = assetName;
            AssetDatabase.CreateAsset(fontAsset, path);
            fontAsset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        // ---------- Duel-Field-Texturen (README-duel-field) ----------

        private static Texture2D BuildTable()
        {
            const int w = 1920, h = 1080;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color baseIn = Hex("#241811"), baseOut = Hex("#0B0705");
            Color gold = Hex("#C8A45C");
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float rx = (x - w / 2f) / 1500f * 2f, ry = (y - h / 2f) / 760f * 2f;
                    float r = Mathf.Sqrt(rx * rx + ry * ry);
                    Color c = Color.Lerp(baseIn, baseOut, Mathf.Clamp01(r / 0.76f));
                    float m1 = ((x + y) % 26f + 26f) % 26f;
                    float m2 = ((x - y) % 26f + 26f) % 26f;
                    if (m1 < 1f) c = Color.Lerp(c, gold, 0.045f);
                    if (m2 < 1f) c = Color.Lerp(c, gold, 0.045f);
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildHorizontalScrim()
        {
            const int w = 64, h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color ink = new Color(10f / 255f, 7f / 255f, 5f / 255f);
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Lerp(0.92f, 0.55f, x / (float)(w - 1));
                for (int y = 0; y < h; y++) px[y * w + x] = new Color(ink.r, ink.g, ink.b, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildVerticalTint(Color color, bool fadeDown)
        {
            const int w = 8, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);              // 0 unten, 1 oben (Unity)
                float a = fadeDown ? t : 1f - t;           // fadeDown: oben voll
                var c = new Color(color.r, color.g, color.b, color.a * a);
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildParchmentPanel()
        {
            const int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color border = Hex("#8C7440");
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    Color c = Color.Lerp(Parchment0, Parchment1, 1f - y / (float)(s - 1));
                    if (x == 0 || y == 0 || x == s - 1 || y == s - 1) c = border;
                    px[y * s + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Leere Zone: gestrichelte 1px-Border + halbtransparente Füllung.</summary>
        private static Texture2D BuildZoneEmpty(Color accent, float borderAlpha, Color fill)
        {
            const int w = 112, h = 157;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            var border = new Color(accent.r, accent.g, accent.b, borderAlpha);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, w / 2f, h / 2f, w / 2f, h / 2f, 5f);
                    if (d > 0f) continue;
                    Color c = fill;
                    if (d > -1f)
                    {
                        bool horizontal = y < 3 || y > h - 4;
                        bool dash = horizontal ? (x % 8) < 5 : (y % 8) < 5;
                        bool corner = (x < 8 || x > w - 9) && (y < 8 || y > h - 9);
                        c = dash || corner ? border : fill;
                    }
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildZoneDrop()
        {
            const int w = 112, h = 157;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color border = new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.9f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, w / 2f, h / 2f, w / 2f, h / 2f, 5f);
                    if (d > 0f) continue;
                    Color c = Color.Lerp(Hex("#2A1D0E"), Hex("#120C06"), GradientT(x, y, w, h, 165f));
                    if (d > -1.5f) c = border;
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildPile(Color top, Color bottom, Color border)
        {
            const int w = 112, h = 157;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, w / 2f, h / 2f, w / 2f, h / 2f, 5f);
                    if (d > 0f) continue;
                    Color c = Color.Lerp(top, bottom, y / (float)h);
                    if (d > -1f) c = border;
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildPlayerSlot(bool self)
        {
            const int w = 112, h = 157;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color gold = Hex("#C8A45C");
            Color cool = new Color(143f / 255f, 198f / 255f, 210f / 255f, 0.6f);
            Color keyline = self ? gold : cool;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, w / 2f, h / 2f, w / 2f, h / 2f, 5f);
                    if (d > 0f) continue;
                    Color c = new Color(0f, 0f, 0f, 0.45f);
                    if (fy < 24f) c = Color.Lerp(Hex("#42301C"), Hex("#22150A"), fy / 24f);           // Namensband
                    else if (fy > h - 20f) c = new Color(0f, 0f, 0f, 0.6f);                            // Footer
                    else
                    {
                        float mid = 24f + (h - 44f) / 2f;
                        float manhattan = Mathf.Abs(fx - w / 2f) + Mathf.Abs(fy - mid);
                        float half = 52f * 0.7071f;
                        if (manhattan <= half)
                        {
                            float t = GradientT(fx - (w / 2f - 26f), fy - (mid - 26f), 52f, 52f, 135f);
                            c = Color.Lerp(new Color(keyline.r, keyline.g, keyline.b, 0.22f), new Color(0, 0, 0, 0.45f), Mathf.Clamp01(t / 0.65f));
                            if (manhattan > half - 1.41f) c = new Color(keyline.r, keyline.g, keyline.b, 0.55f);
                        }
                    }
                    float bw = self ? 2f : 1f;
                    if (d > -bw) c = keyline;
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Kompaktes Karten-Chassis 112x157 (Feld/Hand-Rendition).</summary>
        private static Texture2D BuildCompact(Palette pal, bool withStats)
        {
            const int w = 112, h = 157;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            // Bänder: Pad 3, Name 17, Art 105, dann Meta 11 + Stats 18 (Monster) bzw. Parchment-Footer 29
            var nameRect = new Rect(3, 3, w - 6, 17);
            var artRect = new Rect(3, 20, w - 6, 105);
            var metaRect = new Rect(3, 125, w - 6, 11);
            var statsRect = new Rect(3, 136, w - 6, 18);
            var footerRect = new Rect(3, 125, w - 6, 29);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, w / 2f, h / 2f, w / 2f, h / 2f, 5f);
                    if (d > 0f) continue;
                    Color c = ThreeStop(GradientT(fx, fy, w, h, 165f), pal.bodyTop, pal.bodyMid, 0.55f, pal.bodyBottom);
                    var p = new Vector2(fx, fy);
                    if (nameRect.Contains(p))
                    {
                        float ly = fy - nameRect.y;
                        c = Color.Lerp(pal.plateTop, pal.plateBottom, ly / nameRect.height);
                        if (ly < 1f || ly > nameRect.height - 1f) c = pal.keyline;
                    }
                    else if (artRect.Contains(p))
                    {
                        c = pal.frameBottom;
                        float lx = fx - artRect.x, ly = fy - artRect.y;
                        if (lx < 1f || ly < 1f || lx > artRect.width - 1f || ly > artRect.height - 1f)
                            c = Color.Lerp(c, pal.keyline, 0.45f);
                    }
                    else if (withStats && metaRect.Contains(p))
                    {
                        c = Color.Lerp(c, Color.black, 0.35f);
                    }
                    else if (withStats && statsRect.Contains(p))
                    {
                        float lx = fx - statsRect.x, ly = fy - statsRect.y;
                        float boxW = (statsRect.width - 3f) / 2f;
                        bool inDmg = lx < boxW, inDef = lx > boxW + 3f;
                        if (inDmg || inDef)
                        {
                            c = Color.Lerp(c, Color.black, 0.45f);
                            float bx = inDmg ? lx : lx - boxW - 3f;
                            bool edge = bx < 1f || bx > boxW - 1f || ly < 1f || ly > statsRect.height - 1f;
                            if (edge) c = inDmg ? pal.keyline : Color.Lerp(c, pal.keyline, 0.35f);
                        }
                    }
                    else if (!withStats && footerRect.Contains(p))
                    {
                        float lx = fx - footerRect.x, ly = fy - footerRect.y;
                        c = Color.Lerp(Parchment0, Parchment1, ly / footerRect.height);
                        if (lx < 1f || ly < 1f || lx > footerRect.width - 1f || ly > footerRect.height - 1f)
                            c = pal.effectBorder;
                    }
                    if (d > -1.5f) c = pal.keyline;
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Kartenrückseite in reduzierter Größe (Weave-Pitch/Diamant laut Handoff).</summary>
        private static Texture2D BuildBackSmall(int w, int h, float pitch, float diamond, float radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color gold = Hex("#C8A45C");
            Color baseIn = Hex("#4E2A18"), baseOut = Hex("#1C0E08");
            float cx = w / 2f, cy = h / 2f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, cx, cy, w / 2f, h / 2f, radius);
                    if (d > 0f) continue;
                    float rx = (fx - cx) / (w / 2f), ry = (fy - cy) / (h / 2f);
                    float r = Mathf.Sqrt(rx * rx + ry * ry);
                    Color c = Color.Lerp(baseIn, baseOut, Mathf.Clamp01(r / 0.78f));
                    float m1 = ((fx + fy) % pitch + pitch) % pitch;
                    float m2 = ((fx - fy) % pitch + pitch) % pitch;
                    if (m1 < 1f) c = Color.Lerp(c, gold, 0.15f);
                    if (m2 < 1f) c = Color.Lerp(c, gold, 0.15f);
                    float manhattan = Mathf.Abs(fx - cx) + Mathf.Abs(fy - cy);
                    float half = diamond * 0.7071f;
                    if (manhattan <= half && manhattan > half - 1.41f) c = Color.Lerp(c, gold, 0.55f);
                    if (d > -1f) c = new Color(gold.r, gold.g, gold.b, 0.55f);
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---------- Shell-Texturen (README-shell-screens) ----------

        private static Texture2D BuildShellBackground()
        {
            const int w = 1920, h = 1080;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color baseIn = Hex("#2A1C12"), baseOut = Hex("#0A0705");
            Color gold = Hex("#C8A45C");
            float cx = w * 0.5f, cy = h * 0.55f; // CSS "at 50% 45%" (45% von oben)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float rx = (x - cx) / 1500f * 2f, ry = (y - cy) / 820f * 2f;
                    float r = Mathf.Sqrt(rx * rx + ry * ry);
                    Color c = Color.Lerp(baseIn, baseOut, Mathf.Clamp01(r / 0.78f));
                    float m1 = ((x + y) % 28f + 28f) % 28f;
                    float m2 = ((x - y) % 28f + 28f) % 28f;
                    if (m1 < 1f) c = Color.Lerp(c, gold, 0.04f);
                    if (m2 < 1f) c = Color.Lerp(c, gold, 0.04f);
                    // Vignette: 240px weicher dunkler Rand
                    float edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    float vig = 1f - Mathf.Clamp01(edge / 240f);
                    c = Color.Lerp(c, Color.black, 0.85f * vig * vig);
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildRelicFill()
        {
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color a = Hex("#3A2818"), mid = Hex("#140C07"), b = Hex("#291A0C");
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float t = GradientT(x, y, s, s, 165f);
                    px[(s - 1 - y) * s + x] = ThreeStop(t, a, mid, 0.58f, b);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildRelicFrame()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color gold = Hex("#C8A45C");
            Color keyline = new Color(gold.r, gold.g, gold.b, 0.35f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    Color c = Color.clear;
                    float d = RoundedRectSdf(fx, fy, s / 2f, s / 2f, s / 2f, s / 2f, 12f);
                    if (d <= 0f && d > -2f) c = gold; // 2px Außenrand
                    float dInner = RoundedRectSdf(fx, fy, s / 2f, s / 2f, s / 2f - 6f, s / 2f - 6f, 7f);
                    if (dInner <= 0f && dInner > -1f) c = keyline; // innere Keyline bei inset 6
                    px[(s - 1 - y) * s + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildSweepBand()
        {
            const int w = 64, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Sin(x / (float)(w - 1) * Mathf.PI) * 0.45f;
                for (int y = 0; y < h; y++) px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildTileBg(string accentHex, string fill0, string fill1, string fill2)
        {
            const int w = 318, h = 452;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color accent = Hex(accentHex);
            Color keyline = new Color(accent.r, accent.g, accent.b, 0.35f);
            Color a = Hex(fill0), mid = Hex(fill1), b = Hex(fill2);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, w / 2f, h / 2f, w / 2f, h / 2f, 10f);
                    if (d > 0f) continue;
                    float t = GradientT(x, y, w, h, 165f);
                    Color c = ThreeStop(t, a, mid, 0.58f, b);
                    float dInner = RoundedRectSdf(fx, fy, w / 2f, h / 2f, w / 2f - 6f, h / 2f - 6f, 7f);
                    if (dInner <= 0f && dInner > -1f) c = Color.Lerp(c, keyline, keyline.a);
                    if (d > -2f) c = accent; // 2px Accent-Keyline außen
                    // Zwei Rivets an den oberen Ecken (11px Diamanten bei Inset 16)
                    float rivetHalf = 11f * 0.7071f;
                    float man1 = Mathf.Abs(fx - 16f) + Mathf.Abs(fy - 16f);
                    float man2 = Mathf.Abs(fx - (w - 16f)) + Mathf.Abs(fy - 16f);
                    if (man1 <= rivetHalf || man2 <= rivetHalf) c = accent;
                    px[(h - 1 - y) * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildGradientBadge(string topHex, string bottomHex)
        {
            const int w = 8 * CrestScale, h = 29 * CrestScale;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color top = Hex(topHex), bottom = Hex(bottomHex);
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildEmberBadge() => BuildGradientBadge("#E8B896", "#A85E3C");

        // ---------- Coin Toss (Cutscene, README-coin-flip) ----------

        /// <summary>Münz-Vorderseite RELIC (gold) bzw. Rückseite SEAL (silber), D = 336 px (2× Referenz).</summary>
        private static Texture2D BuildCoinFace(bool relic)
        {
            const int S = 336;
            float R = S / 2f;
            var lightPos = new Vector2(S * 0.34f, S * 0.28f);

            Color bodyA = relic ? Hex("#F8EED6") : Hex("#F2F5F8");
            Color bodyB = relic ? Hex("#C8A45C") : Hex("#A9B2BE");
            Color bodyC = relic ? Hex("#7A5A1E") : Hex("#5A6472");
            float midStop = relic ? 0.46f : 0.50f;
            float endStop = relic ? 0.88f : 0.90f;
            Color rimLight = relic ? Hex("#EBCE8A") : Hex("#D6DDE6");
            Color rimDeep = relic ? Hex("#3B2A10") : Hex("#262C34");
            Color ink = rimDeep;

            return Rasterize(S, S, 3, (fx, fy) =>
            {
                float dx = fx - R, dy = fy - R;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > R) return Color.clear;

                // Radialer Körper vom Lichtpunkt (34% / 28%)
                float t = Mathf.Clamp01(Vector2.Distance(new Vector2(fx, fy), lightPos) / (S * 0.94f));
                Color c = t < midStop
                    ? Color.Lerp(bodyA, bodyB, t / midStop)
                    : Color.Lerp(bodyB, bodyC, Mathf.Clamp01((t - midStop) / (endStop - midStop)));

                // Untere Abschattung (inset 0 -10px 24px)
                float shade = Mathf.Clamp01((fy - S * 0.55f) / (S * 0.45f)) * (relic ? 0.45f : 0.5f);
                c = Color.Lerp(c, Color.black, shade * 0.7f);

                // Relief (im CSS-y: +y nach unten, Mitte R/R)
                if (relic)
                {
                    // Drei genestete 45°-Quadrate (Seiten 192/104/44 bei D=336 → Manhattan-Radien /√2)
                    float m = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (Mathf.Abs(m - 136f) <= 4f) c = ink;
                    else if (Mathf.Abs(m - 74f) <= 3f) c = ink;
                    else if (m < 74f && m > 31f) c = Color.Lerp(c, ink, 0.18f);
                    else if (m <= 31f) c = ink;
                }
                else
                {
                    // SEAL: Wappen — Ring — Speichen als drei klar getrennte Bänder.
                    // Vorher liefen die Speichen quer durch das Wappen und lagen bei
                    // 60 % Deckung; dadurch war die Mitte ein grauer Brei und die
                    // Rückseite im Flug nicht von der Vorderseite zu unterscheiden.
                    if (Mathf.Abs(d - 112f) <= 3f) c = ink;              // Ring
                    if (d >= 122f && d <= 152f)                          // 8 Speichen nach außen
                    {
                        float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
                        bool spoke = ay <= 6f || ax <= 6f
                            || Mathf.Abs(dx - dy) <= 8.5f || Mathf.Abs(dx + dy) <= 8.5f;
                        if (spoke) c = ink;
                    }
                    // Hexagon-Wappen 100×108 zentriert, voll deckend
                    float hx = dx + 50f, hy = dy + 54f;
                    if (hx >= 0f && hx <= 100f && hy >= 0f && hy <= 108f && InHex(hx, hy, 100f, 108f)) c = ink;
                }

                // Rim: äußere 12 px hell, darunter 4 px dunkel
                if (d > R - 12f) c = rimLight;
                else if (d > R - 16f) c = rimDeep;
                return c;
            }, flipY: false);
        }

        /// <summary>Weiche schwarze Boden-Ellipse (transparent ab 72 %).</summary>
        private static Texture2D BuildCoinShadow()
        {
            const int W2 = 256, H2 = 96;
            var tex = new Texture2D(W2, H2, TextureFormat.RGBA32, false);
            var px = new Color[W2 * H2];
            for (int y = 0; y < H2; y++)
                for (int x = 0; x < W2; x++)
                {
                    float nx = (x + 0.5f - W2 / 2f) / (W2 / 2f);
                    float ny = (y + 0.5f - H2 / 2f) / (H2 / 2f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = r >= 1f ? 0f : (r <= 0.72f ? 1f : 1f - (r - 0.72f) / 0.28f);
                    px[y * W2 + x] = new Color(0f, 0f, 0f, a);
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        /// <summary>
        /// Dünner goldener Ellipsen-Ring für die Staublandung. Design-Größe 420×102,
        /// gezeichnet in 2× — die Cutscene zieht den Ring beim Aufschlag auf 420 × Canvas-Skala
        /// auf, und darüber lief er bisher in die Streckung.
        /// </summary>
        private static Texture2D BuildCoinDustRing()
        {
            const int W2 = 840, H2 = 204;
            var tex = new Texture2D(W2, H2, TextureFormat.RGBA32, false);
            var px = new Color[W2 * H2];
            var gold = Hex("#C8A45C");
            for (int y = 0; y < H2; y++)
                for (int x = 0; x < W2; x++)
                {
                    float nx = (x + 0.5f - W2 / 2f) / (W2 / 2f - 6f);
                    float ny = (y + 0.5f - H2 / 2f) / (H2 / 2f - 6f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(r - 1f) * 22f);
                    px[y * W2 + x] = new Color(gold.r, gold.g, gold.b, a);
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        /// <summary>Bildschirm-Vignette (Ränder dunkel, Mitte frei).</summary>
        private static Texture2D BuildScreenVignette()
        {
            const int S = 512;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f - S / 2f) / (S / 2f);
                    float ny = (y + 0.5f - S / 2f) / (S / 2f);
                    float r = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                    float a = Mathf.Pow(Mathf.Clamp01((r - 0.55f) / 0.45f), 1.6f) * 0.88f;
                    px[y * S + x] = new Color(0f, 0f, 0f, a);
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        /// <summary>Generiert die Coin-Toss-Cutscene-Sprites (Münzflächen, Schatten, Ring, Vignette).</summary>
        [MenuItem("Rouge/Card Design/Generate Coin Toss Assets")]
        public static void GenerateCoinToss()
        {
            var skin = AssetDatabase.LoadAssetAtPath<CardSkin>(SkinPath);
            if (skin == null) { Debug.LogError("CardSkin.asset fehlt."); return; }
            skin.coinRelic = SaveSprite(BuildCoinFace(true), "CoinRelic");
            skin.coinSeal = SaveSprite(BuildCoinFace(false), "CoinSeal");
            skin.coinShadow = SaveSprite(BuildCoinShadow(), "CoinShadow");
            skin.coinDustRing = SaveSprite(BuildCoinDustRing(), "CoinDustRing");
            skin.screenVignette = SaveSprite(BuildScreenVignette(), "ScreenVignette");
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log("Coin-Toss-Assets generiert.");
        }

        /// <summary>Generiert nur die neuen Reliquary-Sprites (weißes Chassis, Badge, Kompakt).</summary>
        [MenuItem("Rouge/Card Design/Generate Reliquary Assets")]
        public static void GenerateReliquary()
        {
            var skin = AssetDatabase.LoadAssetAtPath<CardSkin>(SkinPath);
            if (skin == null) { Debug.LogError("CardSkin.asset fehlt — erst 'Generate Assets' ausführen."); return; }
            skin.chassisReliquary = SaveSprite(BuildChassis(Reliquary()), "ChassisReliquary");
            skin.badgeReliquary = SaveSprite(BuildBadge(Reliquary()), "BadgeReliquary");
            skin.compactReliquary = SaveSprite(BuildCompact(Reliquary(), true), "CompactReliquary");
            skin.pileExtra = SaveSprite(BuildPile(
                new Color(244f / 255f, 236f / 255f, 216f / 255f, 0.78f),
                new Color(196f / 255f, 182f / 255f, 148f / 255f, 0.78f),
                new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.6f)), "PileExtra");
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log("Reliquary-Design-Assets generiert (Chassis, Badge, Kompakt).");
        }

        [MenuItem("Rouge/Card Design/Generate Assets")]
        public static void Generate()
        {
            var skin = AssetDatabase.LoadAssetAtPath<CardSkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<CardSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            skin.chassisMonster = SaveSprite(BuildChassis(Monster()), "ChassisMonster");
            skin.chassisSpell = SaveSprite(BuildChassis(Spell()), "ChassisSpell");
            skin.chassisArtifact = SaveSprite(BuildChassis(Artifact()), "ChassisArtifact");
            skin.chassisPlayer = SaveSprite(BuildChassis(Player()), "ChassisPlayer");
            skin.cardBack = SaveSprite(BuildBack(), "CardBack");
            skin.artworkVignette = SaveSprite(BuildVignette(), "ArtVignette");
            skin.whiteFrame = SaveSprite(BuildWhiteFrame(), "WhiteFrame", new Vector4(2, 2, 2, 2));
            skin.whiteSquare = SaveSprite(BuildWhiteSquare(), "WhiteSquare");
            skin.crestMonster = SaveSprite(BuildCrest(Monster()), "CrestMonster");
            skin.crestSpell = SaveSprite(BuildCrest(Spell()), "CrestSpell");
            skin.crestArtifact = SaveSprite(BuildCrest(Artifact()), "CrestArtifact");
            skin.badgeMonster = SaveSprite(BuildBadge(Monster()), "BadgeMonster");
            skin.badgeSpell = SaveSprite(BuildBadge(Spell()), "BadgeSpell");
            skin.badgeArtifact = SaveSprite(BuildBadge(Artifact()), "BadgeArtifact");

            skin.cinzelSemiBold = MakeFont("Cinzel-SemiBold", "Cinzel-SemiBold SDF", 512);
            skin.cinzelBold = MakeFont("Cinzel-Bold", "Cinzel-Bold SDF", 512);
            skin.oswaldMedium = MakeFont("Oswald-Medium", "Oswald-Medium SDF", 512);
            skin.oswaldSemiBold = MakeFont("Oswald-SemiBold", "Oswald-SemiBold SDF", 512);
            skin.spectral = MakeFont("Spectral-Regular", "Spectral SDF", 1024);

            // ---- Duel Field ----
            skin.tableBackground = SaveSprite(BuildTable(), "TableBackground");
            skin.railScrim = SaveSprite(BuildHorizontalScrim(), "RailScrim");
            skin.opponentTint = SaveSprite(BuildVerticalTint(new Color(40f / 255f, 62f / 255f, 86f / 255f, 0.5f), true), "OpponentTint");
            skin.playerTint = SaveSprite(BuildVerticalTint(new Color(96f / 255f, 52f / 255f, 18f / 255f, 0.34f), false), "PlayerTint");
            skin.parchmentPanel = SaveSprite(BuildParchmentPanel(), "ParchmentPanel", new Vector4(3, 3, 3, 3));
            skin.zoneEmptyMonster = SaveSprite(BuildZoneEmpty(Hex("#C8A45C"), 0.35f, new Color(18f / 255f, 11f / 255f, 6f / 255f, 0.55f)), "ZoneEmptyMonster");
            skin.zoneEmptySpell = SaveSprite(BuildZoneEmpty(Hex("#8FC6D2"), 0.4f, new Color(6f / 255f, 16f / 255f, 20f / 255f, 0.55f)), "ZoneEmptySpell");
            skin.zoneEmptyArtifact = SaveSprite(BuildZoneEmpty(Hex("#B9A3E0"), 0.4f, new Color(12f / 255f, 8f / 255f, 20f / 255f, 0.55f)), "ZoneEmptyArtifact");
            skin.zoneDropTarget = SaveSprite(BuildZoneDrop(), "ZoneDropTarget");
            skin.pileGraveyard = SaveSprite(BuildPile(new Color(28f / 255f, 32f / 255f, 42f / 255f, 0.75f), new Color(10f / 255f, 12f / 255f, 16f / 255f, 0.75f), new Color(140f / 255f, 150f / 255f, 165f / 255f, 0.4f)), "PileGraveyard");
            skin.pileBanished = SaveSprite(BuildPile(new Color(58f / 255f, 20f / 255f, 12f / 255f, 0.7f), new Color(18f / 255f, 8f / 255f, 5f / 255f, 0.7f), new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.45f)), "PileBanished");
            skin.playerSlotSelf = SaveSprite(BuildPlayerSlot(true), "PlayerSlotSelf");
            skin.playerSlotFoe = SaveSprite(BuildPlayerSlot(false), "PlayerSlotFoe");
            skin.compactMonster = SaveSprite(BuildCompact(Monster(), true), "CompactMonster");
            skin.compactSpell = SaveSprite(BuildCompact(Spell(), false), "CompactSpell");
            skin.compactArtifact = SaveSprite(BuildCompact(Artifact(), false), "CompactArtifact");
            skin.compactPlayer = SaveSprite(BuildCompact(Player(), false), "CompactPlayer");
            skin.backZone = SaveSprite(BuildBackSmall(112, 157, 13f, 46f, 5f), "BackZone");
            skin.backHand = SaveSprite(BuildBackSmall(62, 87, 9f, 26f, 4f), "BackHand");

            // ---- Shell (Login & Hauptmenü) ----
            skin.shellBackground = SaveSprite(BuildShellBackground(), "ShellBackground");
            skin.relicFill = SaveSprite(BuildRelicFill(), "RelicFill");
            skin.relicFrame = SaveSprite(BuildRelicFrame(), "RelicFrame", new Vector4(20, 20, 20, 20));
            skin.sweepBand = SaveSprite(BuildSweepBand(), "SweepBand");
            skin.tilePlay = SaveSprite(BuildTileBg("#C8A45C", "#3A2818", "#140C07", "#291A0C"), "TilePlay");
            skin.tileSolo = SaveSprite(BuildTileBg("#8FC6D2", "#1B3A43", "#07161A", "#122A31"), "TileSolo");
            skin.tileShop = SaveSprite(BuildTileBg("#E0A07A", "#3E2018", "#170A06", "#2C130C"), "TileShop");
            skin.tileDecks = SaveSprite(BuildTileBg("#B9A3E0", "#2A2148", "#0D0916", "#1D1633"), "TileDecks");
            skin.backLogin = SaveSprite(BuildBackSmall(240, 336, 12f, 116f, 10f), "BackLogin");
            skin.backThumb = SaveSprite(BuildBackSmall(44, 62, 7f, 26f, 3f), "BackThumb");
            skin.badgeEmber = SaveSprite(BuildEmberBadge(), "BadgeEmber");
            skin.badgeTeal = SaveSprite(BuildGradientBadge("#A5D8E2", "#3B7C8B"), "BadgeTeal");

            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log("Card-Design-Assets generiert: " + ArtDir);
        }
    }
}
