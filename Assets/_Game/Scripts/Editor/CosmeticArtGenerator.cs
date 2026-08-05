using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Erzeugt die Grafiken aller Kosmetik-Gegenstände (Handoff „Cosmetics") —
    /// acht Kartenrücken, sechs Spielmatten, fünf Wurfmünzen mit je zwei Seiten,
    /// sechs Profilrahmen, und für jeden Gegenstand ein Shop-Icon.
    ///
    /// Alles ist prozedural: die Gegenstände sind geometrisch beschrieben, nicht
    /// gemalt, und lassen sich damit in jeder Größe scharf ausgeben. Die Kanten
    /// laufen über <see cref="Rasterize"/> mit Unterabtastung, sonst treppen
    /// Diagonalen und Kreise sichtbar.
    ///
    /// Menü: Rouge/Cosmetics/Generate Art.
    ///
    /// Die Regeln aus dem Handoff, die man beim Ändern nicht brechen darf:
    ///   • Kartenrücken unterscheiden sich in Webrichtung, Helligkeit UND
    ///     Mittelmotiv — zwei dürfen nie in allen dreien übereinstimmen.
    ///   • Jede Münze trägt zwei VERSCHIEDENE Zeichen, sonst ist der Flug nicht
    ///     zu lesen.
    ///   • Profilrahmen müssen auf 44 px lesbar bleiben: das Merkmal sitzt in der
    ///     Silhouette oder im Rand, nie im Detail.
    ///   • Ein Icon ist die Miniatur der eigenen Geometrie, kein Kategoriesymbol.
    ///     Beim Verkleinern fällt Beiwerk weg, das Kennzeichen bleibt.
    /// </summary>
    public static class CosmeticArtGenerator
    {
        // Unter Resources, damit der Client sie zur Laufzeit über die Id findet —
        // der Katalog kommt vom Server, die Grafik muss ohne Inspector-Verdrahtung
        // dazu passen.
        private const string Dir = "Assets/_Game/Resources/Cosmetics";

        private const int BackW = 240, BackH = 336;   // Kartenrücken
        private const int MatW = 960, MatH = 540;     // Spielmatte
        private const int CoinS = 336;                // Münzseite
        private const int FrameS = 300;               // Profilrahmen
        private const int IconS = 68;                 // Shop-Icon (2× von 34)

        // ================== MENÜ ==================

        [MenuItem("Rouge/Cosmetics/Generate Art")]
        public static void Generate()
        {
            // Bewusst ohne StartAssetEditing: der Importer muss je Datei sofort
            // greifen, und innerhalb einer Sammelbearbeitung gibt es ihn noch nicht.
            Directory.CreateDirectory(Dir);
            foreach (var back in Backs) Save(back.Draw(BackW, BackH), "back_" + back.Id);
            foreach (var mat in Mats) Save(mat.Draw(MatW, MatH), "mat_" + mat.Id);
            foreach (var coin in Coins)
            {
                Save(coin.DrawRelic(CoinS), "coin_" + coin.Id + "_relic");
                Save(coin.DrawSeal(CoinS), "coin_" + coin.Id + "_seal");
            }
            foreach (var frame in Frames) Save(frame.Draw(FrameS), "frame_" + frame.Id);
            foreach (var icon in Icons) Save(icon.Draw(IconS), "icon_" + icon.Id);
            AssetDatabase.Refresh();
            Debug.Log($"Kosmetik-Grafiken erzeugt: {Backs.Length} Rücken, {Mats.Length} Matten, "
                      + $"{Coins.Length} Münzen (je 2 Seiten), {Frames.Length} Rahmen, {Icons.Length} Icons → {Dir}");
        }

        // ================== KARTENRÜCKEN (8) ==================

        private class Back
        {
            public string Id;
            public Func<int, int, Texture2D> Draw;
        }

        private static readonly Back[] Backs =
        {
            new Back { Id = "ashen_weave",      Draw = AshenWeave },
            new Back { Id = "tomb_gilt",        Draw = TombGilt },
            new Back { Id = "deep_current",     Draw = DeepCurrent },
            new Back { Id = "obsidian_lattice", Draw = ObsidianLattice },
            new Back { Id = "chainbound",       Draw = Chainbound },
            new Back { Id = "cartogram",        Draw = Cartogram },
            new Back { Id = "split_seal",       Draw = SplitSeal },
            new Back { Id = "static_bloom",     Draw = StaticBloom },
        };

        /// <summary>Das Hausgewebe, entfärbt: gleiche Geometrie wie Vanilla, alles Gold heraus.</summary>
        private static Texture2D AshenWeave(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#3A382F"), Hex("#1A1A17"), y / (float)h);
            if (Weave(x, y, 26f, 1.6f)) c = Lift(c, Hex("#7E7566"), 0.30f);
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, Hex("#7E7566"), edge);
            if (DiamondRing(x, y, w * 0.5f, h * 0.5f, 46f, 2.4f)) c = Hex("#7E7566");
            return c;
        });

        /// <summary>Blattgold über dem Gewebe: enge Webung, GEFÜLLTE Mittelraute, dicker Rand.</summary>
        private static Texture2D TombGilt(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#2C2110"), Hex("#120C05"), y / (float)h);
            if (Weave(x, y, 14f, 1.4f)) c = Lift(c, Hex("#EBCE8A"), 0.26f);
            if (Diamond(x, y, w * 0.5f, h * 0.5f, 44f)) c = Hex("#EBCE8A");
            else if (DiamondRing(x, y, w * 0.5f, h * 0.5f, 62f, 2f)) c = Lift(c, Hex("#EBCE8A"), 0.7f);
            if (CardEdge(x, y, w, h, out float edge, 7f)) c = Color.Lerp(c, Hex("#EBCE8A"), edge);
            return c;
        });

        /// <summary>Das geflutete untere Gewölbe: KEINE Diagonale, waagerechte Bänder, Linse in der Mitte.</summary>
        private static Texture2D DeepCurrent(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#0E2B33"), Hex("#04141A"), y / (float)h);
            float band = Mathf.Repeat(y, 22f);
            if (band < 1.8f) c = Lift(c, Hex("#8FC6D2"), 0.24f);
            else if (band < 9f) c = Lift(c, Hex("#8FC6D2"), 0.05f);
            // waagerechte Linse
            float lx = (x - w * 0.5f) / 74f, ly = (y - h * 0.5f) / 20f;
            float lens = lx * lx + ly * ly;
            if (lens < 1f) c = Lift(c, Hex("#DFF4F8"), 0.55f * (1f - lens));
            if (Mathf.Abs(lens - 1f) < 0.07f) c = Lift(c, Hex("#DFF4F8"), 0.8f);
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, Hex("#4C7B87"), edge);
            return c;
        });

        /// <summary>Zwei gekreuzte Gitter: orthogonal UND 45°, fast schwarz, ein heller Kern.</summary>
        private static Texture2D ObsidianLattice(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#131318"), Hex("#08080B"), y / (float)h);
            if (Mathf.Repeat(x, 24f) < 1.4f || Mathf.Repeat(y, 24f) < 1.4f) c = Lift(c, Hex("#6E7482"), 0.30f);
            if (Weave(x, y, 34f, 1.2f)) c = Lift(c, Hex("#6E7482"), 0.20f);
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.5f, h * 0.5f));
            if (d < 15f) c = Color.Lerp(Hex("#DDE3EE"), c, d / 15f);
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, Hex("#6E7482"), edge);
            return c;
        });

        /// <summary>Zugebunden: eine senkrechte Säule aus fünf Kettengliedern — der einzige Rücken mit Objektwiederholung.</summary>
        private static Texture2D Chainbound(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#241D14"), Hex("#0D0A06"), y / (float)h);
            if (Weave(x, y, 30f, 1.2f)) c = Lift(c, Hex("#8A7A5E"), 0.10f);
            for (int i = 0; i < 5; i++)
            {
                float cy = h * (0.17f + i * 0.165f);
                float ex = (x - w * 0.5f) / 26f, ey = (y - cy) / 34f;
                float r = Mathf.Sqrt(ex * ex + ey * ey);
                if (Mathf.Abs(r - 1f) < 0.13f) c = Lift(c, Hex("#C8A45C"), 0.9f);
                else if (Mathf.Abs(r - 1f) < 0.22f) c = Lift(c, Hex("#7A5A1E"), 0.5f);
            }
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, Hex("#C8A45C"), edge);
            return c;
        });

        /// <summary>Der Grundriss des Gewölbes. Der EINZIGE helle Rücken — Pergament, versetzte Rechtecke, Kompass.</summary>
        private static Texture2D Cartogram(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#D9CCAB"), Hex("#A8996F"), y / (float)h);
            var ink = Hex("#4A3B20");
            // versetzte Rechtecke: der Grundriss
            var plans = new[]
            {
                new Rect(w * 0.14f, h * 0.12f, w * 0.44f, h * 0.24f),
                new Rect(w * 0.34f, h * 0.30f, w * 0.50f, h * 0.22f),
                new Rect(w * 0.18f, h * 0.56f, w * 0.38f, h * 0.30f),
                new Rect(w * 0.52f, h * 0.62f, w * 0.30f, h * 0.20f),
            };
            foreach (var plan in plans)
                if (RectRing(x, y, plan, 1.6f)) c = Lift(c, ink, 0.85f);
            // Kompass in der Mitte
            float dx = x - w * 0.5f, dy = y - h * 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (Mathf.Abs(d - 21f) < 1.4f) c = Lift(c, ink, 0.9f);
            if (d < 20f && (Mathf.Abs(dx) < 1.4f || Mathf.Abs(dy) < 1.4f)) c = Lift(c, ink, 0.9f);
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, ink, edge * 0.8f);
            return c;
        });

        /// <summary>Zwei Hälften, eine Karte: diagonal geteilt, Gold gegen Violett, eine erleuchtete Naht.</summary>
        private static Texture2D SplitSeal(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            // Trennlinie von oben links nach unten rechts
            float t = x / (float)w + y / (float)h - 1f;
            bool gold = t < 0f;
            var c = gold
                ? Color.Lerp(Hex("#332315"), Hex("#150D07"), y / (float)h)
                : Color.Lerp(Hex("#241C3C"), Hex("#0D0916"), y / (float)h);
            var thread = gold ? Hex("#EBCE8A") : Hex("#D6C4F5");
            if (Weave(x, y, 24f, 1.4f, gold)) c = Lift(c, thread, 0.22f);
            float seam = Mathf.Abs(t) * Mathf.Min(w, h) * 0.5f;
            if (seam < 1.6f) c = Hex("#F8EED6");
            else if (seam < 7f) c = Lift(c, Hex("#F8EED6"), 0.45f * (1f - seam / 7f));
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, gold ? Hex("#C8A45C") : Hex("#B9A3E0"), edge);
            return c;
        });

        /// <summary>Interferenz: vier konzentrische KREISE — keine Rauten — plus Bildzeilen.</summary>
        private static Texture2D StaticBloom(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            if (!InCard(x, y, w, h)) return Color.clear;
            var c = Color.Lerp(Hex("#191A22"), Hex("#08090D"), y / (float)h);
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.5f, h * 0.5f));
            float[] radii = { 26f, 50f, 74f, 98f };
            float[] alpha = { 0.95f, 0.62f, 0.38f, 0.20f };
            for (int i = 0; i < radii.Length; i++)
                if (Mathf.Abs(d - radii[i]) < 1.8f) c = Lift(c, Hex("#DFF4F8"), alpha[i]);
            if (Mathf.Repeat(y, 9f) < 4f) c = Lift(c, Hex("#8FC6D2"), 0.07f);
            if (d < 9f) c = Hex("#F8EED6");
            if (CardEdge(x, y, w, h, out float edge)) c = Color.Lerp(c, Hex("#8FC6D2"), edge);
            return c;
        });

        // ================== SPIELMATTEN (6) ==================

        private class Mat
        {
            public string Id;
            public Func<int, int, Texture2D> Draw;
        }

        private static readonly Mat[] Mats =
        {
            new Mat { Id = "stone_table",     Draw = StoneTable },
            new Mat { Id = "tidal_floor",     Draw = TidalFloor },
            new Mat { Id = "ember_circle",    Draw = EmberCircle },
            new Mat { Id = "starless_vault",  Draw = StarlessVault },
            new Mat { Id = "foundry_grate",   Draw = FoundryGrate },
            new Mat { Id = "cathedral_plate", Draw = CathedralPlate },
        };

        /// <summary>Die Platte, tiefer geschlagen: Steinfugen und zwei genestete Rauten.</summary>
        private static Texture2D StoneTable(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            var c = Color.Lerp(Hex("#241A10"), Hex("#0D0805"), Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
            int row = Mathf.FloorToInt(y / 108f);
            float ox = row % 2 == 0 ? 0f : 84f;
            if (Mathf.Repeat(y, 108f) < 2.5f) c = Color.Lerp(c, Color.black, 0.55f);
            if (Mathf.Repeat(x + ox, 168f) < 2.5f) c = Color.Lerp(c, Color.black, 0.45f);
            if (DiamondRing(x, y, w * 0.5f, h * 0.5f, 132f, 3f)) c = Lift(c, Hex("#C8A45C"), 0.18f);
            if (DiamondRing(x, y, w * 0.5f, h * 0.5f, 88f, 3f)) c = Lift(c, Hex("#C8A45C"), 0.13f);
            return c;
        }, alpha: false);

        /// <summary>Wasser über Stein: waagerechte Bänder, zwei Linsen, erleuchtete Mittellinie.</summary>
        private static Texture2D TidalFloor(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            var c = Color.Lerp(Hex("#0C2830"), Hex("#04121A"), Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
            if (Mathf.Repeat(y, 26f) < 2f) c = Lift(c, Hex("#8FC6D2"), 0.09f);
            foreach (var cx in new[] { w * 0.28f, w * 0.72f })
            {
                float lx = (x - cx) / 180f, ly = (y - h * 0.5f) / 52f;
                float lens = lx * lx + ly * ly;
                if (lens < 1f) c = Lift(c, Hex("#DFF4F8"), 0.12f * (1f - lens));
            }
            if (Mathf.Abs(y - h * 0.5f) < 1.6f) c = Lift(c, Hex("#DFF4F8"), 0.5f);
            return c;
        }, alpha: false);

        /// <summary>Ein brennender Ring um das Feld, vier Glutpunkte.</summary>
        private static Texture2D EmberCircle(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            var c = Color.Lerp(Hex("#2A140C"), Hex("#0C0503"), Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
            float ex = (x - w * 0.5f) / (w * 0.44f), ey = (y - h * 0.5f) / (h * 0.42f);
            float r = Mathf.Sqrt(ex * ex + ey * ey);
            c = Lift(c, Hex("#E0603A"), Mathf.Clamp01(1f - Mathf.Abs(r - 1f) * 9f) * 0.5f);
            if (Mathf.Abs(r - 1f) < 0.012f) c = Lift(c, Hex("#F3C3A6"), 0.8f);
            foreach (var p in new[] { new Vector2(0.16f, 0.24f), new Vector2(0.84f, 0.24f),
                                      new Vector2(0.16f, 0.76f), new Vector2(0.84f, 0.76f) })
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(w * p.x, h * p.y));
                c = Lift(c, Hex("#F3C3A6"), Mathf.Clamp01(1f - d / 34f) * 0.55f);
            }
            return c;
        }, alpha: false);

        /// <summary>Kalt und leer: fast schwarz, neun feste kalte Punkte, kein Zierrat.</summary>
        private static Texture2D StarlessVault(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            var c = Hex("#060609");
            c = Color.Lerp(Hex("#0C0C12"), c, Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
            for (int i = 0; i < 9; i++)
            {
                // fest verdrahtet, nicht zufällig — ein Duell sieht wie das nächste aus
                float px = w * (0.12f + (i % 3) * 0.38f) + (i / 3) * 22f;
                float py = h * (0.18f + (i / 3) * 0.32f);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(px, py));
                c = Lift(c, Hex("#AFC4E0"), Mathf.Clamp01(1f - d / 16f) * 0.5f);
            }
            return c;
        }, alpha: false);

        /// <summary>Über dem Schmelzofen: senkrechte Streben, warmes Licht von unten.</summary>
        private static Texture2D FoundryGrate(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            float up = Mathf.Clamp01(y / (float)h);
            var c = Color.Lerp(Hex("#140C08"), Hex("#3A1C0C"), up * up);
            if (Mathf.Repeat(x, 46f) < 22f) c = Color.Lerp(c, Hex("#0A0604"), 0.72f);
            else c = Lift(c, Hex("#E0603A"), 0.10f + up * 0.28f);
            if (Mathf.Repeat(x, 46f) < 2f) c = Lift(c, Hex("#F3C3A6"), 0.16f);
            return c;
        }, alpha: false);

        /// <summary>Ein Bogensaal: Bögen oben und unten, zwei ausstrahlende Rippen, helle Mittelraute.</summary>
        private static Texture2D CathedralPlate(int w, int h) => Rasterize(w, h, (x, y) =>
        {
            var c = Color.Lerp(Hex("#1C1830"), Hex("#08060F"), Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
            var stone = Hex("#B9A3E0");
            for (int i = 0; i < 5; i++)
            {
                float cx = w * (0.1f + i * 0.2f);
                foreach (var cy in new[] { 0f, (float)h })
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (Mathf.Abs(d - 96f) < 2f) c = Lift(c, stone, 0.30f);
                    if (Mathf.Abs(d - 74f) < 1.5f) c = Lift(c, stone, 0.18f);
                }
            }
            float rib = Mathf.Abs(Mathf.Abs(x - w * 0.5f) - Mathf.Abs(y - h * 0.5f) * 1.6f);
            if (rib < 2f) c = Lift(c, stone, 0.22f);
            if (Diamond(x, y, w * 0.5f, h * 0.5f, 30f)) c = Lift(c, Hex("#EFE7FA"), 0.5f);
            if (DiamondRing(x, y, w * 0.5f, h * 0.5f, 54f, 2f)) c = Lift(c, stone, 0.4f);
            return c;
        }, alpha: false);

        // ================== WURFMÜNZEN (5) ==================

        private class Coin
        {
            public string Id;
            public Func<int, Texture2D> DrawRelic, DrawSeal;
        }

        private static readonly Coin[] Coins =
        {
            new Coin { Id = "copper_trial",  DrawRelic = s => CopperTrial(s, true),  DrawSeal = s => CopperTrial(s, false) },
            new Coin { Id = "silver_warden", DrawRelic = s => SilverWarden(s, true), DrawSeal = s => SilverWarden(s, false) },
            new Coin { Id = "bone_token",    DrawRelic = s => BoneToken(s, true),    DrawSeal = s => BoneToken(s, false) },
            new Coin { Id = "molten_bit",    DrawRelic = s => MoltenBit(s, true),    DrawSeal = s => MoltenBit(s, false) },
            new Coin { Id = "vault_coin",    DrawRelic = s => VaultCoin(s, true),    DrawSeal = s => VaultCoin(s, false) },
        };

        /// <summary>Kupfer: RELIC eine fette gefüllte Raute mit gestanzter Mitte, SEAL ein geviertes Feld.</summary>
        private static Texture2D CopperTrial(int s, bool relic) =>
            CoinBody(s, Hex("#F6D9B4"), Hex("#B87333"), Hex("#5E3512"), Hex("#EFC79A"), Hex("#3A1F0A"),
                (dx, dy, d, ink, c) =>
                {
                    float m = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (relic)
                    {
                        if (m <= 118f && m > 34f) return ink;
                        if (m <= 34f) return c;                       // gestanzte Mitte
                    }
                    else
                    {
                        if (Mathf.Abs(dx) <= 7f || Mathf.Abs(dy) <= 7f) { if (d < 128f) return ink; }
                        foreach (var p in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
                            if (Vector2.Distance(new Vector2(dx, dy), p * 62f) < 16f) return ink;
                    }
                    return c;
                });

        /// <summary>Silber: RELIC Kreis mit Speichen und Wappen, SEAL ein Schlüsselloch.</summary>
        private static Texture2D SilverWarden(int s, bool relic) =>
            CoinBody(s, Hex("#F2F5F8"), Hex("#A9B2BE"), Hex("#5A6472"), Hex("#D6DDE6"), Hex("#262C34"),
                (dx, dy, d, ink, c) =>
                {
                    if (relic)
                    {
                        if (Mathf.Abs(d - 112f) <= 3f) return ink;
                        if (d >= 122f && d <= 152f)
                        {
                            float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
                            if (ay <= 6f || ax <= 6f || Mathf.Abs(dx - dy) <= 8.5f || Mathf.Abs(dx + dy) <= 8.5f) return ink;
                        }
                        if (InHexLocal(dx + 50f, dy + 54f, 100f, 108f)) return ink;
                    }
                    else
                    {
                        // Schlüsselloch: Kreis über einem sich verjüngenden Schlitz
                        if (Vector2.Distance(new Vector2(dx, dy + 42f), Vector2.zero) < 42f) return ink;
                        float taper = Mathf.Lerp(26f, 9f, Mathf.InverseLerp(-6f, 108f, dy));
                        if (dy > -6f && dy < 108f && Mathf.Abs(dx) < taper) return ink;
                        if (Mathf.Abs(d - 148f) <= 3f) return ink;
                    }
                    return c;
                });

        /// <summary>Knochen: die einzige nicht runde Münze — abgeplatzt, und auf beiden Seiten anders.</summary>
        private static Texture2D BoneToken(int s, bool relic)
        {
            float R = s / 2f;
            var light = Hex("#F4EEDC"); var mid = Hex("#CFC3A4"); var deep = Hex("#8A7C5C");
            var ink = Hex("#4A3F28");
            return Rasterize(s, s, (x, y) =>
            {
                float dx = x - R, dy = y - R;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                // unregelmäßige Silhouette: der Radius schwankt mit dem Winkel,
                // auf beiden Seiten mit anderer Phase — Abplatzer, keine Scheibe
                float a = Mathf.Atan2(dy, dx);
                float wobble = Mathf.Sin(a * 5f + (relic ? 0.4f : 2.3f)) * 7f
                             + Mathf.Sin(a * 9f + (relic ? 1.9f : 0.6f)) * 4f;
                float edgeR = R - 6f + wobble;
                if (d > edgeR) return Color.clear;
                float t = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(s * 0.34f, s * 0.28f)) / (s * 0.94f));
                var c = t < 0.5f ? Color.Lerp(light, mid, t / 0.5f) : Color.Lerp(mid, deep, (t - 0.5f) / 0.5f);
                c = Color.Lerp(c, Color.black, Mathf.Clamp01((y - s * 0.55f) / (s * 0.45f)) * 0.3f);
                if (relic)
                {
                    // eingeritztes Kreuz
                    if ((Mathf.Abs(dx) <= 9f && Mathf.Abs(dy) <= 96f) || (Mathf.Abs(dy) <= 9f && Mathf.Abs(dx) <= 96f)) c = ink;
                }
                else
                {
                    // drei Krallenspuren, schräg
                    for (int i = 0; i < 3; i++)
                    {
                        float off = (i - 1) * 42f;
                        float line = Mathf.Abs((dx - dy) * 0.7071f - off);
                        float along = (dx + dy) * 0.7071f;
                        if (line < 5.5f && Mathf.Abs(along) < 88f - Mathf.Abs(off) * 0.4f) c = ink;
                    }
                }
                if (d > edgeR - 5f) c = Color.Lerp(c, deep, 0.75f);
                return c;
            });
        }

        /// <summary>Geschmolzen: RELIC eine glühende Naht mit Raute, SEAL ein strahlender Rissstern.</summary>
        private static Texture2D MoltenBit(int s, bool relic) =>
            CoinBody(s, Hex("#6E6A66"), Hex("#3A3634"), Hex("#151312"), Hex("#8A8480"), Hex("#0A0908"),
                (dx, dy, d, ink, c) =>
                {
                    var glow = Hex("#F0713A"); var hot = Hex("#FFD9A8");
                    if (relic)
                    {
                        float m = Mathf.Abs(dx) + Mathf.Abs(dy);
                        if (Mathf.Abs(m - 96f) <= 5f) return ink;
                        if (Mathf.Abs(dy) < 26f)   // die Naht: das einzige leuchtende Element im Satz
                        {
                            float k = 1f - Mathf.Abs(dy) / 26f;
                            if (Mathf.Abs(dy) < 5f) return hot;
                            return Color.Lerp(c, glow, k * 0.9f);
                        }
                        if (m <= 40f) return ink;
                    }
                    else
                    {
                        if (d < 34f) return ink;                       // dunkler Kern
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * Mathf.PI / 4f + 0.19f;
                            float px = Mathf.Cos(a), py = Mathf.Sin(a);
                            float along = dx * px + dy * py;
                            float across = Mathf.Abs(-dx * py + dy * px);
                            if (along > 30f && along < 150f && across < Mathf.Lerp(9f, 2f, Mathf.InverseLerp(30f, 150f, along)))
                                return Color.Lerp(glow, hot, Mathf.InverseLerp(30f, 150f, along));
                        }
                    }
                    return c;
                });

        /// <summary>Tresor: RELIC Rautenumriss mit goldenem Kern, SEAL Ring mit vier Pips und Wappen.</summary>
        private static Texture2D VaultCoin(int s, bool relic) =>
            CoinBody(s, Hex("#F8EED6"), Hex("#C8A45C"), Hex("#7A5A1E"), Hex("#EBCE8A"), Hex("#3B2A10"),
                (dx, dy, d, ink, c) =>
                {
                    var gold = Hex("#EBCE8A");
                    float m = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (relic)
                    {
                        if (Mathf.Abs(m - 128f) <= 5f) return ink;
                        if (Mathf.Abs(m - 96f) <= 3f) return ink;
                        if (m <= 46f) return gold;
                    }
                    else
                    {
                        if (Mathf.Abs(d - 130f) <= 4f) return ink;
                        for (int i = 0; i < 4; i++)
                        {
                            float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
                            if (Vector2.Distance(new Vector2(dx, dy), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 100f) < 13f) return ink;
                        }
                        if (InHexLocal(dx + 46f, dy + 50f, 92f, 100f)) return gold;
                    }
                    return c;
                });

        /// <summary>
        /// Der gemeinsame Münzkörper: radialer Verlauf vom Lichtpunkt, Abschattung
        /// unten, Rand hell mit dunklem Absatz. Das Relief liefert der Aufrufer.
        /// </summary>
        private static Texture2D CoinBody(int s, Color bodyA, Color bodyB, Color bodyC,
                                          Color rimLight, Color rimDeep,
                                          Func<float, float, float, Color, Color, Color> relief)
        {
            float R = s / 2f;
            var lightPos = new Vector2(s * 0.34f, s * 0.28f);
            return Rasterize(s, s, (x, y) =>
            {
                float dx = x - R, dy = y - R;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > R) return Color.clear;
                float t = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), lightPos) / (s * 0.94f));
                var c = t < 0.48f ? Color.Lerp(bodyA, bodyB, t / 0.48f)
                                  : Color.Lerp(bodyB, bodyC, Mathf.Clamp01((t - 0.48f) / 0.42f));
                c = Color.Lerp(c, Color.black, Mathf.Clamp01((y - s * 0.55f) / (s * 0.45f)) * 0.34f);
                c = relief(dx, dy, d, rimDeep, c);
                if (d > R - 12f) c = rimLight;
                else if (d > R - 16f) c = rimDeep;
                return c;
            }, flipY: false);
        }

        // ================== PROFILRAHMEN (6) ==================

        private class Frame
        {
            public string Id;
            public Func<int, Texture2D> Draw;
        }

        private static readonly Frame[] Frames =
        {
            new Frame { Id = "iron_bracket",     Draw = IronBracket },
            new Frame { Id = "amber_halo",       Draw = AmberHalo },
            new Frame { Id = "thorn_setting",    Draw = ThornSetting },
            new Frame { Id = "gilded_reliquary", Draw = GildedReliquary },
            new Frame { Id = "prism_mount",      Draw = PrismMount },
            new Frame { Id = "vault_ring",       Draw = VaultRing },
        };

        // Die Mitte bleibt frei — dort liegt das Portrait.
        private const float Port = 0.80f;

        /// <summary>Genietete Platte: vier runde Nieten auf dickem grauem Rand.</summary>
        private static Texture2D IronBracket(int s) => Rasterize(s, s, (x, y) =>
        {
            float inset = s * (1f - Port) * 0.5f;
            bool inside = x > inset && y > inset && x < s - inset && y < s - inset;
            if (inside) return Color.clear;
            var c = Color.Lerp(Hex("#6A6A72"), Hex("#2E2E34"), y / (float)s);
            if (x < 3f || y < 3f || x > s - 3f || y > s - 3f) c = Hex("#8A8A94");
            foreach (var p in new[] { new Vector2(0.09f, 0.09f), new Vector2(0.91f, 0.09f),
                                      new Vector2(0.09f, 0.91f), new Vector2(0.91f, 0.91f) })
                if (Vector2.Distance(new Vector2(x, y), new Vector2(s * p.x, s * p.y)) < s * 0.035f)
                    c = Hex("#C6C6CE");
            return c;
        });

        /// <summary>Von hinten erleuchtet: ein warmer Schein, breiter als das Portrait.</summary>
        private static Texture2D AmberHalo(int s) => Rasterize(s, s, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(s * 0.5f, s * 0.5f));
            float outer = s * 0.5f, inner = s * Port * 0.5f;
            if (d < inner * 0.98f) return Color.clear;
            float k = Mathf.Clamp01(1f - (d - inner) / (outer - inner));
            var c = Color.Lerp(Hex("#E0A05C"), Hex("#F3DDA4"), k);
            return new Color(c.r, c.g, c.b, Mathf.Clamp01(k * 1.15f) * 0.95f);
        });

        /// <summary>
        /// Gedornte Fassung: acht Dreiecke, die den quadratischen Umriss
        /// <b>nach aussen</b> durchbrechen. Das ist der ganze Zweck des Rahmens —
        /// auf 44 px in einer Bestenliste erkennt man ihn nur an der Silhouette,
        /// und eine nach innen gekerbte bleibt ein Quadrat wie jedes andere.
        /// Der Rahmen selbst sitzt darum eingerückt, damit die Dornen Platz haben.
        /// </summary>
        private static Texture2D ThornSetting(int s) => Rasterize(s, s, (x, y) =>
        {
            float margin = s * 0.14f;                       // Luft für die Dornenspitzen
            float inner = s * 0.24f;                        // Innenkante des Rahmens
            var iron = Color.Lerp(Hex("#6A6276"), Hex("#241F2A"), y / (float)s);

            float edgeDistance = Mathf.Min(Mathf.Min(x, s - x), Mathf.Min(y, s - y));
            if (edgeDistance >= margin && edgeDistance <= inner) return iron;   // der Rahmen
            if (edgeDistance > inner) return Color.clear;                       // das Portrait

            // Acht Dornen, je zwei pro Kante, die aus dem Rahmen nach aussen laufen
            for (int edge = 0; edge < 4; edge++)
            {
                // u = Position entlang der Kante, v = Abstand von der Aussenkante
                float u = edge == 0 ? x : edge == 1 ? y : edge == 2 ? x : y;
                float v = edge == 0 ? y : edge == 1 ? s - x : edge == 2 ? s - y : x;
                if (v > margin) continue;
                for (int k = 0; k < 2; k++)
                {
                    float centre = s * (0.34f + k * 0.32f);
                    float half = Mathf.Lerp(s * 0.075f, 0f, 1f - v / margin);   // spitz nach aussen
                    if (Mathf.Abs(u - centre) <= half) return iron;
                }
            }
            return Color.clear;
        });

        /// <summary>Die Tresortür, geschrumpft: doppelter Goldrand und ein Wappen über der Oberkante.</summary>
        private static Texture2D GildedReliquary(int s) => Rasterize(s, s, (x, y) =>
        {
            float inset = s * 0.11f;
            bool inside = x > inset && y > inset && x < s - inset && y < s - inset;
            // Wappen sitzt oben und ragt über den Rahmen hinaus
            if (InHexLocal(x - (s * 0.5f - s * 0.09f), y + s * 0.03f, s * 0.18f, s * 0.20f))
                return Color.Lerp(Hex("#EBCE8A"), Hex("#8E6A22"), y / (s * 0.2f));
            if (inside) return Color.clear;
            var c = Color.Lerp(Hex("#C8A45C"), Hex("#6A4E1C"), y / (float)s);
            float ring = Mathf.Min(Mathf.Min(x, s - x), Mathf.Min(y, s - y));
            if (ring > s * 0.045f && ring < s * 0.065f) c = Hex("#1E1405");   // Trennfuge = doppelter Rand
            if (ring < 2.5f) c = Hex("#EBCE8A");
            return c;
        });

        /// <summary>Facettiert: vier große Eckdreiecke schneiden in das Portrait hinein.</summary>
        private static Texture2D PrismMount(int s) => Rasterize(s, s, (x, y) =>
        {
            float inset = s * 0.055f;
            bool border = x < inset || y < inset || x > s - inset || y > s - inset;
            float cut = s * 0.42f;
            bool corner = x + y < cut || (s - x) + y < cut || x + (s - y) < cut || (s - x) + (s - y) < cut;
            if (!border && !corner) return Color.clear;
            var c = Color.Lerp(Hex("#D8CAF6"), Hex("#4A3A78"), (x + y) / (2f * s));
            if (corner && !border)
            {
                float edge = Mathf.Min(Mathf.Min(cut - (x + y), cut - ((s - x) + y)),
                                       Mathf.Min(cut - (x + (s - y)), cut - ((s - x) + (s - y))));
                if (edge < 3f) c = Hex("#EFE7FA");
            }
            return c;
        });

        /// <summary>Nie in Ruhe: ein gestrichelter Kreis AUSSERHALB des Quadrats.</summary>
        private static Texture2D VaultRing(int s) => Rasterize(s, s, (x, y) =>
        {
            float inset = s * 0.145f;
            bool square = !(x > inset && y > inset && x < s - inset && y < s - inset);
            var c = Color.clear;
            if (square)
            {
                c = Color.Lerp(Hex("#C8A45C"), Hex("#6A4E1C"), y / (float)s);
                float ring = Mathf.Min(Mathf.Min(x, s - x), Mathf.Min(y, s - y));
                if (ring > inset - 3f) c = Hex("#EBCE8A");
            }
            float dx = x - s * 0.5f, dy = y - s * 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (Mathf.Abs(d - s * 0.46f) < s * 0.012f)
            {
                float a = Mathf.Atan2(dy, dx) + Mathf.PI;
                if (Mathf.Repeat(a, Mathf.PI / 9f) < Mathf.PI / 15f) c = Hex("#F3DDA4");
            }
            return c;
        });

        // ================== SHOP-ICONS (30) ==================

        private class Icon
        {
            public string Id;
            public Func<int, Texture2D> Draw;
        }

        /// <summary>
        /// Jedes Icon ist die Miniatur der EIGENEN Geometrie, kein Kategoriesymbol —
        /// ein Raster aus 30 Kategoriesymbolen ist unlesbar, ein Raster aus 30
        /// verschiedenen Formen scannbar. Beim Verkleinern fällt Beiwerk weg.
        /// </summary>
        private static readonly Icon[] Icons = BuildIcons();

        private static Icon[] BuildIcons()
        {
            var list = new List<Icon>();

            void Add(string id, string plate, string accent, Action<IconPen> content) =>
                list.Add(new Icon { Id = id, Draw = s => IconTile(s, Hex(plate), Hex(accent), content) });

            // --- Kartenrücken ---
            Add("ashen_weave", "#1A1A17", "#7E7566", p => { p.Weave(9f); p.DiamondRing(0.34f); });
            Add("tomb_gilt", "#120C05", "#EBCE8A", p => { p.Weave(5f); p.Diamond(0.30f); });
            Add("deep_current", "#04141A", "#8FC6D2", p => { p.Bands(8f); p.Lens(0.44f, 0.16f); });
            Add("obsidian_lattice", "#08080B", "#6E7482", p => { p.Grid(9f); p.Weave(13f); p.Dot(0, 0, 0.09f); });
            Add("chainbound", "#0D0A06", "#C8A45C", p => { p.Link(0f, -0.20f); p.Link(0f, 0.20f); });
            Add("cartogram", "#A8996F", "#4A3B20", p => { p.RectRing(-0.14f, -0.14f, 0.42f, 0.30f); p.RectRing(0.12f, 0.14f, 0.44f, 0.32f); });
            Add("split_seal", "#150D07", "#EBCE8A", p => { p.SplitHalf(Hex("#241C3C"), Hex("#D6C4F5")); p.Seam(); });
            Add("static_bloom", "#08090D", "#DFF4F8", p => { p.Circle(0.24f); p.Circle(0.42f); p.Scanlines(4f); });

            // --- Spielmatten ---
            Add("stone_table", "#0D0805", "#C8A45C", p => { p.Joints(); p.DiamondRing(0.36f); });
            Add("tidal_floor", "#04121A", "#8FC6D2", p => { p.Bands(7f); p.CentreRule(); });
            Add("ember_circle", "#0C0503", "#E0603A", p => { p.Ellipse(0.42f, 0.34f); p.Dot(-0.30f, -0.22f, 0.06f); p.Dot(0.30f, 0.22f, 0.06f); });
            Add("starless_vault", "#060609", "#AFC4E0", p => { p.Dot(-0.24f, -0.18f, 0.05f); p.Dot(0.10f, 0.02f, 0.05f); p.Dot(0.28f, 0.26f, 0.05f); });
            Add("foundry_grate", "#140C08", "#E0603A", p => { p.Bars(7f); p.Underlight(); });
            Add("cathedral_plate", "#08060F", "#B9A3E0", p => { p.Arch(-0.5f); p.Arch(0.5f); p.Diamond(0.14f); });

            // --- Wurfmünzen ---
            Add("copper_trial", "#5E3512", "#EFC79A", p => { p.Disc(); p.Diamond(0.34f); p.Punch(0.12f); });
            Add("silver_warden", "#5A6472", "#D6DDE6", p => { p.Disc(); p.Keyhole(); });
            Add("bone_token", "#8A7C5C", "#F4EEDC", p => { p.Chip(); p.Claw(); });
            Add("molten_bit", "#151312", "#F0713A", p => { p.Disc(); p.Seam(); p.Punch(0.10f); });
            Add("vault_coin", "#7A5A1E", "#EBCE8A", p => { p.Disc(); p.DiamondRing(0.34f); p.Diamond(0.13f); });

            // --- Profilrahmen ---
            Add("iron_bracket", "#2E2E34", "#C6C6CE", p => { p.Border(0.16f); p.Dot(-0.30f, -0.30f, 0.06f); p.Dot(0.30f, 0.30f, 0.06f); });
            Add("amber_halo", "#2A1A0A", "#F3DDA4", p => { p.Halo(); });
            Add("thorn_setting", "#241F2A", "#8A8296", p => { p.Border(0.12f); p.Spike(0f, -1f); p.Spike(0f, 1f); p.Spike(-1f, 0f); p.Spike(1f, 0f); });
            Add("gilded_reliquary", "#1E1405", "#EBCE8A", p => { p.Border(0.12f); p.Border(0.24f); p.Crest(-0.36f); });
            Add("prism_mount", "#241C3C", "#D8CAF6", p => { p.CornerCut(); });
            Add("vault_ring", "#1E1405", "#F3DDA4", p => { p.Border(0.20f); p.DashedRing(0.44f); });

            // --- Siegessiegel ---
            Add("brand", "#1A0F06", "#C8894E", p => { p.DiamondRing(0.36f); p.Diamond(0.14f); });
            Add("shatter", "#07161A", "#DFF4F8", p => { p.DiamondRing(0.36f); p.Crack(28f); p.Crack(-52f); });
            Add("bloom", "#0D0916", "#EFE7FA", p => { p.DiamondRing(0.20f); p.DiamondRing(0.34f); p.DiamondRing(0.46f); });
            Add("verdict", "#1A1206", "#F8EED6", p => { p.Diamond(0.38f); p.Punch(0.20f); p.Diamond(0.08f); });
            Add("eclipse", "#0A0705", "#F8EED6", p => { p.Disc(); p.DarkDisc(0.10f); });

            // --- Titel: die drei sind Text, kein Gegenstand — ein schlichtes Band ---
            Add("sealbreaker", "#1A1207", "#C8A45C", p => { p.CentreRule(); p.Diamond(0.12f); });
            Add("ash_collector", "#101418", "#8FC6D2", p => { p.CentreRule(); p.Dot(-0.22f, 0f, 0.06f); p.Dot(0.22f, 0f, 0.06f); });
            Add("wardens_bane", "#1A0A0A", "#E0603A", p => { p.CentreRule(); p.Crack(38f); });

            return list.ToArray();
        }

        /// <summary>
        /// Ein Icon-Kachel: Platte in der dunkelsten Farbe des Gegenstands, 1 px
        /// Rand in seiner Akzentfarbe, darin höchstens vier Elemente.
        /// </summary>
        private static Texture2D IconTile(int s, Color plate, Color accent, Action<IconPen> content)
        {
            var pen = new IconPen(s, plate, accent);
            content(pen);
            return pen.Bake();
        }

        /// <summary>
        /// Zeichenstift für die Icons. Sammelt Prüfungen und rastert sie am Ende in
        /// einem Durchgang mit Unterabtastung — so bleiben Kreise und Diagonalen
        /// auch bei 34 px sauber.
        /// </summary>
        private class IconPen
        {
            private readonly int size;
            private readonly Color plate, accent;
            private readonly List<Func<float, float, Color>> layers = new List<Func<float, float, Color>>();

            public IconPen(int size, Color plate, Color accent)
            {
                this.size = size; this.plate = plate; this.accent = accent;
            }

            // Alle Maße sind relativ zur Kantenlänge, Ursprung in der Mitte.
            private float N(float v) => v * size;
            private Color A(float alpha) => new Color(accent.r, accent.g, accent.b, alpha);

            public void Weave(float pitch) => layers.Add((u, v) =>
                Mathf.Repeat(u + v, pitch) < 1.3f || Mathf.Repeat(u - v, pitch) < 1.3f ? A(0.5f) : Color.clear);
            public void Grid(float pitch) => layers.Add((u, v) =>
                Mathf.Repeat(u, pitch) < 1.2f || Mathf.Repeat(v, pitch) < 1.2f ? A(0.45f) : Color.clear);
            public void Bands(float pitch) => layers.Add((u, v) => Mathf.Repeat(v, pitch) < 1.6f ? A(0.5f) : Color.clear);
            public void Bars(float pitch) => layers.Add((u, v) => Mathf.Repeat(u, pitch) < pitch * 0.45f ? A(0.42f) : Color.clear);
            public void Scanlines(float pitch) => layers.Add((u, v) => Mathf.Repeat(v, pitch) < pitch * 0.5f ? A(0.16f) : Color.clear);
            public void Joints() => layers.Add((u, v) =>
                Mathf.Repeat(v, N(0.34f)) < 1.4f || Mathf.Repeat(u, N(0.5f)) < 1.4f ? A(0.35f) : Color.clear);

            public void Diamond(float r) => layers.Add((u, v) => Mathf.Abs(u) + Mathf.Abs(v) <= N(r) ? A(1f) : Color.clear);
            public void DiamondRing(float r) => layers.Add((u, v) =>
                Mathf.Abs(Mathf.Abs(u) + Mathf.Abs(v) - N(r)) < 1.5f ? A(1f) : Color.clear);
            public void Circle(float r) => layers.Add((u, v) =>
                Mathf.Abs(Mathf.Sqrt(u * u + v * v) - N(r)) < 1.4f ? A(0.9f) : Color.clear);
            public void Disc() => layers.Add((u, v) => Mathf.Sqrt(u * u + v * v) <= N(0.42f) ? A(0.9f) : Color.clear);
            public void DarkDisc(float offset) => layers.Add((u, v) =>
                Vector2.Distance(new Vector2(u, v), new Vector2(N(offset), 0f)) <= N(0.40f)
                    ? new Color(plate.r * 0.4f, plate.g * 0.4f, plate.b * 0.4f, 1f) : Color.clear);
            public void Punch(float r) => layers.Add((u, v) =>
                Mathf.Abs(u) + Mathf.Abs(v) <= N(r) ? plate : Color.clear);
            public void Dot(float x, float y, float r) => layers.Add((u, v) =>
                Vector2.Distance(new Vector2(u, v), new Vector2(N(x), N(y))) <= N(r) ? A(1f) : Color.clear);
            public void Ellipse(float rx, float ry) => layers.Add((u, v) =>
            {
                float e = Mathf.Sqrt(Sq(u / N(rx)) + Sq(v / N(ry)));
                return Mathf.Abs(e - 1f) < 0.10f ? A(0.9f) : Color.clear;
            });
            public void Lens(float rx, float ry) => layers.Add((u, v) =>
                Sq(u / N(rx)) + Sq(v / N(ry)) <= 1f ? A(0.85f) : Color.clear);
            public void CentreRule() => layers.Add((u, v) => Mathf.Abs(v) < 1.4f ? A(0.9f) : Color.clear);
            public void Seam() => layers.Add((u, v) => Mathf.Abs(v) < 2.2f ? A(1f) : Color.clear);
            public void Underlight() => layers.Add((u, v) =>
                A(Mathf.Clamp01((v + N(0.5f)) / N(1f)) * 0.55f));

            public void Border(float inset) => layers.Add((u, v) =>
            {
                float ring = N(0.5f) - Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                return ring >= N(inset) - 1.5f && ring <= N(inset) ? A(1f) : Color.clear;
            });
            public void RectRing(float cx, float cy, float w, float h) => layers.Add((u, v) =>
            {
                float ax = Mathf.Abs(u - N(cx)), ay = Mathf.Abs(v - N(cy));
                bool inBox = ax <= N(w) * 0.5f && ay <= N(h) * 0.5f;
                bool inCore = ax <= N(w) * 0.5f - 1.4f && ay <= N(h) * 0.5f - 1.4f;
                return inBox && !inCore ? A(0.95f) : Color.clear;
            });
            public void Link(float x, float y) => layers.Add((u, v) =>
            {
                float e = Mathf.Sqrt(Sq((u - N(x)) / N(0.16f)) + Sq((v - N(y)) / N(0.20f)));
                return Mathf.Abs(e - 1f) < 0.22f ? A(1f) : Color.clear;
            });
            public void Halo() => layers.Add((u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v) / N(0.46f);
                return d <= 1f ? A(Mathf.Clamp01(Mathf.Pow(d, 2.2f)) * 0.95f) : Color.clear;
            });
            public void Spike(float dx, float dy) => layers.Add((u, v) =>
            {
                float along = u * dx + v * dy, across = Mathf.Abs(-u * dy + v * dx);
                return along > N(0.16f) && across < N(0.5f) - along * 0.85f ? A(1f) : Color.clear;
            });
            public void Crest(float y) => layers.Add((u, v) =>
                InHexLocal(u + N(0.11f), v - N(y) + N(0.12f), N(0.22f), N(0.24f)) ? A(1f) : Color.clear);
            public void CornerCut() => layers.Add((u, v) =>
                Mathf.Abs(u) + Mathf.Abs(v) >= N(0.62f) ? A(0.95f) : Color.clear);
            public void DashedRing(float r) => layers.Add((u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (Mathf.Abs(d - N(r)) > 1.5f) return Color.clear;
                float a = Mathf.Atan2(v, u) + Mathf.PI;
                return Mathf.Repeat(a, Mathf.PI / 6f) < Mathf.PI / 10f ? A(1f) : Color.clear;
            });
            public void Keyhole() => layers.Add((u, v) =>
            {
                if (Vector2.Distance(new Vector2(u, v), new Vector2(0f, -N(0.12f))) < N(0.13f)) return A(1f);
                float taper = Mathf.Lerp(N(0.08f), N(0.03f), Mathf.InverseLerp(-N(0.02f), N(0.30f), v));
                return v > -N(0.02f) && v < N(0.30f) && Mathf.Abs(u) < taper ? A(1f) : Color.clear;
            });
            public void Chip() => layers.Add((u, v) =>
            {
                float a = Mathf.Atan2(v, u);
                float edge = N(0.40f) + Mathf.Sin(a * 5f + 0.4f) * N(0.03f);
                return Mathf.Sqrt(u * u + v * v) <= edge ? A(0.85f) : Color.clear;
            });
            public void Claw() => layers.Add((u, v) =>
            {
                for (int i = -1; i <= 1; i++)
                {
                    float line = Mathf.Abs((u - v) * 0.7071f - i * N(0.14f));
                    if (line < N(0.022f) && Mathf.Abs((u + v) * 0.7071f) < N(0.28f)) return plate;
                }
                return Color.clear;
            });
            public void Crack(float degrees) => layers.Add((u, v) =>
            {
                float rad = degrees * Mathf.Deg2Rad;
                float px = Mathf.Cos(rad), py = Mathf.Sin(rad);
                float across = Mathf.Abs(-u * py + v * px), along = Mathf.Abs(u * px + v * py);
                return across < 1.2f && along < N(0.44f) ? A(1f) : Color.clear;
            });
            public void Arch(float side) => layers.Add((u, v) =>
            {
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0f, N(0.5f) * side));
                return Mathf.Abs(d - N(0.34f)) < 1.4f ? A(0.85f) : Color.clear;
            });
            public void SplitHalf(Color otherPlate, Color otherAccent) => layers.Add((u, v) =>
                u + v > 0f ? otherPlate : Color.clear);

            public Texture2D Bake()
            {
                float half = size * 0.5f;
                float radius = size * 0.147f;   // 5 px bei 34 px Kantenlänge
                return Rasterize(size, size, (x, y) =>
                {
                    // abgerundete Platte
                    float qx = Mathf.Abs(x - half) - (half - radius);
                    float qy = Mathf.Abs(y - half) - (half - radius);
                    float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
                    float sdf = Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    if (sdf > 0f) return Color.clear;

                    var c = plate;
                    float u = x - half, v = y - half;
                    foreach (var layer in layers)
                    {
                        var over = layer(u, v);
                        if (over.a > 0f) c = Color.Lerp(c, new Color(over.r, over.g, over.b, 1f), over.a);
                    }
                    if (sdf > -2f) c = accent;   // 1 px Rand in der Akzentfarbe (2 px bei 2×)
                    return c;
                });
            }
        }

        // ================== ZEICHEN-WERKZEUG ==================

        private static float Sq(float v) => v * v;

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            return colour;
        }

        /// <summary>Farbe aufhellen, ohne die Deckkraft anzufassen.</summary>
        private static Color Lift(Color baseColour, Color over, float amount) =>
            Color.Lerp(baseColour, over, Mathf.Clamp01(amount));

        /// <summary>Kartenkontur mit 12 px Radius (5 px im Handoff, hier auf 240 px Breite).</summary>
        private static bool InCard(float x, float y, float w, float h)
        {
            const float radius = 12f;
            float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - radius);
            float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - radius);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius <= 0f;
        }

        /// <summary>Randstärke der Karte: liefert wie stark die Randfarbe durchschlägt.</summary>
        private static bool CardEdge(float x, float y, float w, float h, out float strength, float thickness = 5f)
        {
            float edge = Mathf.Min(Mathf.Min(x, w - x), Mathf.Min(y, h - y));
            if (edge <= thickness) { strength = 1f; return true; }
            if (edge <= thickness + 9f) { strength = 0.22f; return true; }   // innere Zierlinie
            strength = 0f;
            return false;
        }

        private static bool Weave(float x, float y, float pitch, float width, bool bothWays = true) =>
            Mathf.Repeat(x + y, pitch) < width || (bothWays && Mathf.Repeat(x - y, pitch) < width);

        private static bool Diamond(float x, float y, float cx, float cy, float r) =>
            Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= r;

        private static bool DiamondRing(float x, float y, float cx, float cy, float r, float width) =>
            Mathf.Abs(Mathf.Abs(x - cx) + Mathf.Abs(y - cy) - r) < width;

        private static bool RectRing(float x, float y, Rect rect, float width) =>
            rect.Contains(new Vector2(x, y))
            && !new Rect(rect.x + width, rect.y + width, rect.width - width * 2f, rect.height - width * 2f)
                    .Contains(new Vector2(x, y));

        /// <summary>Sechseck wie auf der Karte, lokale Koordinaten ab der linken oberen Ecke.</summary>
        private static bool InHexLocal(float lx, float ly, float w, float h)
        {
            if (lx < 0f || ly < 0f || lx > w || ly > h) return false;
            var p = new Vector2(lx / w, ly / h);
            Vector2[] poly =
            {
                new Vector2(.5f, 0f), new Vector2(1f, .20f), new Vector2(1f, .66f),
                new Vector2(.5f, 1f), new Vector2(0f, .66f), new Vector2(0f, .20f)
            };
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (poly[i].y > p.y != poly[j].y > p.y &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        /// <summary>
        /// Rastert mit 3×3 Unterabtastung. Ohne das treppen Sechsecke, Ellipsen und
        /// jede Diagonale sichtbar — dasselbe, was die Kartenwappen verpixelt aussehen liess.
        /// </summary>
        private static Texture2D Rasterize(int w, int h, Func<float, float, Color> shade,
                                           bool flipY = true, bool alpha = true, int sub = 3)
        {
            var tex = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
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
                    px[(flipY ? h - 1 - y : y) * w + x] =
                        new Color(r / weight, g / weight, b / weight, weight / total);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static void Save(Texture2D tex, string name)
        {
            string path = $"{Dir}/{name}.png";
            int longestSide = Mathf.Max(tex.width, tex.height);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            if (longestSide <= 900) importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
