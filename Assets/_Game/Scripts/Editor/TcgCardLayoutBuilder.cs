using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Baut das Layout des TcgCard-Prefabs um: Name-Balken oben, Artwork-Box in der Mitte,
    /// Werte-Balken unten — klar getrennte, besser lesbare Bereiche.
    /// Läuft nach einem Layout-Versionssprung automatisch einmal; jederzeit manuell über
    /// das Menü "Rouge/TCG-Karten-Prefab neu aufbauen".
    /// </summary>
    public static class TcgCardLayoutBuilder
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/TcgCard.prefab";
        private const string VersionKey = "Rouge.TcgCardLayoutVersion";
        private const int LayoutVersion = 2;

        [InitializeOnLoadMethod]
        private static void AutoUpgrade()
        {
            if (EditorPrefs.GetInt(VersionKey, 1) >= LayoutVersion) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Rebuild();
            };
        }

        [MenuItem("Rouge/TCG-Karten-Prefab neu aufbauen")]
        public static void Rebuild()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[Rouge] Prefab nicht gefunden: {PrefabPath}");
                return;
            }

            try
            {
                BuildLayout(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                EditorPrefs.SetInt(VersionKey, LayoutVersion);
                Debug.Log($"[Rouge] TcgCard-Prefab-Layout v{LayoutVersion} aufgebaut.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildLayout(GameObject root)
        {
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var t = root.transform;

            var nameText = FindText(t, "NameText");
            var levelText = FindText(t, "LevelText");
            var statsText = FindText(t, "StatsText");
            var artworkTf = FindDeep(t, "Artwork");
            var backOverlay = t.Find("BackOverlay");
            var highlightFrame = t.Find("HighlightFrame");
            if (nameText == null || levelText == null || statsText == null || artworkTf == null)
            {
                Debug.LogError("[Rouge] TcgCard-Prefab: erwartete Kinder fehlen — Umbau abgebrochen.");
                return;
            }

            // Idempotenz: Texte zurück an die Wurzel, alte Balken entfernen
            nameText.transform.SetParent(t, false);
            statsText.transform.SetParent(t, false);
            artworkTf.SetParent(t, false);
            foreach (var barName in new[] { "NameBar", "ArtBox", "StatsBar" })
            {
                var old = t.Find(barName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // ---- Name-Balken (oben) ----
            var nameBar = MakeBar("NameBar", t, uiSprite, new Color(0f, 0f, 0f, 0.45f));
            SetRect(nameBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(-12, 30));
            nameText.transform.SetParent(nameBar, false);
            Stretch((RectTransform)nameText.transform, new Vector2(-6, -2));
            nameText.fontSize = 12;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 8;
            nameText.fontSizeMax = 12;
            nameText.alignment = TextAlignmentOptions.Center;

            // ---- Level-Zeile ----
            var levelRect = (RectTransform)levelText.transform;
            levelRect.SetParent(t, false);
            SetRect(levelRect, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(-12, 13));
            levelText.fontSize = 10;
            levelText.alignment = TextAlignmentOptions.Center;

            // ---- Artwork-Box (Mitte) ----
            var artBox = MakeBar("ArtBox", t, uiSprite, new Color(0f, 0f, 0f, 0.30f));
            SetRect(artBox, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(-16, 52));
            artworkTf.SetParent(artBox, false);
            Stretch((RectTransform)artworkTf, new Vector2(-4, -4));
            var artImage = artworkTf.GetComponent<Image>();
            if (artImage != null) artImage.preserveAspect = true;

            // ---- Werte-Balken (unten) ----
            var statsBar = MakeBar("StatsBar", t, uiSprite, new Color(0f, 0f, 0f, 0.45f));
            SetRect(statsBar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(-12, 24));
            statsText.transform.SetParent(statsBar, false);
            Stretch((RectTransform)statsText.transform, new Vector2(-4, 0));
            statsText.fontSize = 13;
            statsText.enableAutoSizing = true;
            statsText.fontSizeMin = 8;
            statsText.fontSizeMax = 13;
            statsText.alignment = TextAlignmentOptions.Center;

            // Render-Reihenfolge: Balken unter Rücken & Highlight
            if (backOverlay != null) backOverlay.SetAsLastSibling();
            if (highlightFrame != null) highlightFrame.SetAsLastSibling();
        }

        private static RectTransform MakeBar(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 sizeDelta)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sizeDelta;
        }

        private static TextMeshProUGUI FindText(Transform root, string name)
        {
            var child = FindDeep(root, name);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>Sucht ein Kind beliebiger Tiefe (die Texte können bereits in Balken hängen).</summary>
        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
