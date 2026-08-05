using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Baut TcgCard.prefab im "Reliquary"-Design neu auf: fixe 480x672-Geometrie laut
    /// Handoff, uniform skaliert über TcgCardView.FitCardRoot.
    /// Menü: Rouge/Card Design/Rebuild TcgCard Prefab.
    /// </summary>
    public static class TcgCardPrefabBuilderV3
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/TcgCard.prefab";
        private const string SkinPath = "Assets/_Game/Data/Tcg/CardSkin.asset";

        [MenuItem("Rouge/Card Design/Rebuild TcgCard Prefab")]
        public static void Rebuild()
        {
            var skin = AssetDatabase.LoadAssetAtPath<CardSkin>(SkinPath);
            if (skin == null) { Debug.LogError("CardSkin fehlt — erst 'Generate Assets' ausführen."); return; }

            var root = new GameObject("TcgCard", typeof(RectTransform));
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(104f, 140f);

                var rootImage = root.AddComponent<Image>();
                rootImage.color = Color.clear;
                rootImage.raycastTarget = true;

                var view = root.AddComponent<UI.TcgCardView>();

                // ---- CardRoot: natives Design-Format ----
                var cardRoot = Child(root.transform, "CardRoot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 672f));

                var front = Child(cardRoot, "Front", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                var chassis = ImageChild(front, "Chassis", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, skin.chassisMonster);

                var artwork = ImageChild(front, "Artwork", TL(), TL(), new Vector2(68f, -79f), new Vector2(342f, 342f), null);
                artwork.GetComponent<Image>().enabled = false;
                var vignette = ImageChild(front, "Vignette", TL(), TL(), new Vector2(68f, -79f), new Vector2(342f, 342f), skin.artworkVignette);
                vignette.GetComponent<Image>().enabled = false;

                var nameGo = TextChild(front, "NameText", TL(), TL(), new Vector2(57f, -14f), new Vector2(314f, 51f),
                    skin.cinzelSemiBold, 22f, TextAlignmentOptions.MidlineLeft);
                var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
                nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
                // Lange Namen werden kleiner gesetzt statt abgeschnitten. Ellipsis
                // bleibt als letzter Notnagel, falls selbst 13pt nicht reichen.
                nameTmp.enableAutoSizing = true;
                nameTmp.fontSizeMax = 22f;
                nameTmp.fontSizeMin = 13f;
                nameTmp.overflowMode = TextOverflowModes.Ellipsis;
                nameTmp.characterSpacing = 1f;

                var crest = ImageChild(front, "Crest", TL(), TL(), new Vector2(397f, -15.5f), new Vector2(44f, 48f), skin.crestMonster);
                var crestText = TextChild(crest.transform, "CrestText", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.cinzelBold, 24f, TextAlignmentOptions.Center);

                var badge = ImageChild(front, "Badge", TL(), TL(), new Vector2(39f, -437f), new Vector2(100f, 29f), skin.badgeMonster);
                var badgeText = TextChild(badge.transform, "BadgeText", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.oswaldSemiBold, 12f, TextAlignmentOptions.Center);
                badgeText.GetComponent<TextMeshProUGUI>().characterSpacing = 14f;

                // Meta-Strip: schwarzer Grund + Keyline-Rahmen + Pip + zwei Labels
                var strip = ImageChild(front, "MetaStrip", TL(), TL(), new Vector2(143f, -437f), new Vector2(298f, 29f), null);
                strip.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
                var stripBorder = ImageChild(strip.transform, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, skin.whiteFrame);
                stripBorder.GetComponent<Image>().type = Image.Type.Sliced;
                var pip = ImageChild(strip.transform, "Pip", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15.5f, 0f), new Vector2(9f, 9f), skin.whiteSquare);
                ((RectTransform)pip.transform).localEulerAngles = new Vector3(0f, 0f, 45f);
                var attrText = TextChild(strip.transform, "AttrText", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.oswaldMedium, 12f, TextAlignmentOptions.MidlineLeft);
                var attrTmp = attrText.GetComponent<TextMeshProUGUI>();
                attrTmp.characterSpacing = 16f;
                var attrRect = (RectTransform)attrText.transform;
                attrRect.offsetMin = new Vector2(27f, 0f);
                attrRect.offsetMax = new Vector2(-11f, 0f);
                var typeTextGo = TextChild(strip.transform, "TypeText", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.oswaldMedium, 12f, TextAlignmentOptions.MidlineRight);
                typeTextGo.GetComponent<TextMeshProUGUI>().characterSpacing = 16f;
                var typeRect = (RectTransform)typeTextGo.transform;
                typeRect.offsetMin = new Vector2(11f, 0f);
                typeRect.offsetMax = new Vector2(-11f, 0f);

                var effectGo = TextChild(front, "EffectText", TL(), TL(), new Vector2(51f, -479f), new Vector2(378f, 110f),
                    skin.spectral, 13f, TextAlignmentOptions.TopLeft);
                var effectTmp = effectGo.GetComponent<TextMeshProUGUI>();
                effectTmp.lineSpacing = 20f;
                effectTmp.overflowMode = TextOverflowModes.Ellipsis;

                // Stat-Reihe
                var statsRoot = Child(front, "Stats", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var dmgLabel = TextChild(statsRoot, "DmgLabel", TL(), TL(), new Vector2(52f, -602f), new Vector2(80f, 56f),
                    skin.oswaldMedium, 11f, TextAlignmentOptions.MidlineLeft);
                dmgLabel.GetComponent<TextMeshProUGUI>().characterSpacing = 18f;
                dmgLabel.GetComponent<TextMeshProUGUI>().text = "ATK";
                var dmgValue = TextChild(statsRoot, "DmgValue", TL(), TL(), new Vector2(100f, -602f), new Vector2(124f, 56f),
                    skin.cinzelBold, 28f, TextAlignmentOptions.MidlineRight);
                var defLabel = TextChild(statsRoot, "DefLabel", TL(), TL(), new Vector2(256f, -602f), new Vector2(80f, 56f),
                    skin.oswaldMedium, 11f, TextAlignmentOptions.MidlineLeft);
                defLabel.GetComponent<TextMeshProUGUI>().characterSpacing = 18f;
                defLabel.GetComponent<TextMeshProUGUI>().text = "DEF";
                var defValue = TextChild(statsRoot, "DefValue", TL(), TL(), new Vector2(304f, -602f), new Vector2(124f, 56f),
                    skin.cinzelBold, 28f, TextAlignmentOptions.MidlineRight);

                var back = ImageChild(cardRoot, "BackOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, skin.cardBack);
                back.SetActive(false);

                var highlight = ImageChild(cardRoot, "HighlightFrame", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(20f, 20f), null);
                highlight.GetComponent<Image>().color = UI.TcgCardView.TargetHighlight;
                highlight.transform.SetAsFirstSibling(); // hinter dem Chassis: nur der Rand wirkt als Outline
                highlight.SetActive(false);

                // ---- CompactRoot: Feld-/Hand-Rendition 112x157 ----
                var compactRoot = Child(root.transform, "CompactRoot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 157f));
                compactRoot.gameObject.SetActive(false);

                var cHighlight = ImageChild(compactRoot, "cHighlight", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(8f, 8f), null);
                cHighlight.GetComponent<Image>().color = UI.TcgCardView.TargetHighlight;
                cHighlight.SetActive(false);

                var cChassis = ImageChild(compactRoot, "cChassis", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, skin.compactMonster);
                var cArt = ImageChild(compactRoot, "cArt", TL(), TL(), new Vector2(4f, -21f), new Vector2(104f, 103f), null);
                cArt.GetComponent<Image>().enabled = false;

                var cNameGo = TextChild(compactRoot, "cName", TL(), TL(), new Vector2(8f, -3f), new Vector2(96f, 17f),
                    skin.cinzelSemiBold, 8f, TextAlignmentOptions.MidlineLeft);
                var cNameTmp = cNameGo.GetComponent<TextMeshProUGUI>();
                cNameTmp.textWrappingMode = TextWrappingModes.NoWrap;
                // Wie bei der grossen Karte: lieber kleiner setzen als abschneiden
                cNameTmp.enableAutoSizing = true;
                cNameTmp.fontSizeMax = 8f;
                cNameTmp.fontSizeMin = 5.5f;
                cNameTmp.overflowMode = TextOverflowModes.Ellipsis;

                var cMeta = Child(compactRoot, "cMeta", TL(), TL(), TL(), new Vector2(3f, -125f), new Vector2(106f, 11f));
                var cPip = ImageChild(cMeta, "cPip", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(7.5f, 0f), new Vector2(5f, 5f), skin.whiteSquare);
                ((RectTransform)cPip.transform).localEulerAngles = new Vector3(0f, 0f, 45f);
                var cAttrGo = TextChild(cMeta, "cAttr", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.oswaldMedium, 7f, TextAlignmentOptions.MidlineLeft);
                var cAttrRect = (RectTransform)cAttrGo.transform;
                cAttrRect.offsetMin = new Vector2(14f, 0f);
                cAttrRect.offsetMax = new Vector2(-4f, 0f);
                var cTypeGo = TextChild(cMeta, "cType", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.oswaldMedium, 7f, TextAlignmentOptions.MidlineRight);
                var cTypeRect = (RectTransform)cTypeGo.transform;
                cTypeRect.offsetMin = new Vector2(4f, 0f);
                cTypeRect.offsetMax = new Vector2(-4f, 0f);

                var cStats = Child(compactRoot, "cStats", TL(), TL(), TL(), new Vector2(3f, -136f), new Vector2(106f, 18f));
                var cAtkGo = TextChild(cStats, "cAtk", TL(), TL(), new Vector2(0f, 0f), new Vector2(51.5f, 18f),
                    skin.cinzelBold, 10f, TextAlignmentOptions.Center);
                var cDefGo = TextChild(cStats, "cDef", TL(), TL(), new Vector2(54.5f, 0f), new Vector2(51.5f, 18f),
                    skin.cinzelBold, 10f, TextAlignmentOptions.Center);

                var cFooterGo = TextChild(compactRoot, "cFooter", TL(), TL(), new Vector2(3f, -125f), new Vector2(106f, 29f),
                    skin.oswaldSemiBold, 8f, TextAlignmentOptions.Center);
                var cFooterTmp = cFooterGo.GetComponent<TextMeshProUGUI>();
                cFooterTmp.characterSpacing = 18f;
                cFooterTmp.color = new Color32(0x6B, 0x62, 0x50, 0xFF);

                var cCrest = ImageChild(compactRoot, "cCrest", TL(), TL(), new Vector2(97f, 6f), new Vector2(20f, 22f), skin.crestMonster);
                var cCrestTextGo = TextChild(cCrest.transform, "cCrestText", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    skin.cinzelBold, 11f, TextAlignmentOptions.Center);

                var cBack = ImageChild(compactRoot, "cBack", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, skin.backZone);
                cBack.SetActive(false);

                // ---- View verdrahten ----
                var so = new SerializedObject(view);
                so.FindProperty("skin").objectReferenceValue = skin;
                so.FindProperty("cardRoot").objectReferenceValue = cardRoot;
                so.FindProperty("front").objectReferenceValue = front.gameObject;
                so.FindProperty("chassisImage").objectReferenceValue = chassis.GetComponent<Image>();
                so.FindProperty("artworkImage").objectReferenceValue = artwork.GetComponent<Image>();
                so.FindProperty("vignetteImage").objectReferenceValue = vignette.GetComponent<Image>();
                so.FindProperty("nameText").objectReferenceValue = nameTmp;
                so.FindProperty("crestImage").objectReferenceValue = crest.GetComponent<Image>();
                so.FindProperty("crestText").objectReferenceValue = crestText.GetComponent<TextMeshProUGUI>();
                so.FindProperty("badgeRect").objectReferenceValue = (RectTransform)badge.transform;
                so.FindProperty("badgeImage").objectReferenceValue = badge.GetComponent<Image>();
                so.FindProperty("badgeText").objectReferenceValue = badgeText.GetComponent<TextMeshProUGUI>();
                so.FindProperty("stripRect").objectReferenceValue = (RectTransform)strip.transform;
                so.FindProperty("stripBorder").objectReferenceValue = stripBorder.GetComponent<Image>();
                so.FindProperty("pipRect").objectReferenceValue = (RectTransform)pip.transform;
                so.FindProperty("pipImage").objectReferenceValue = pip.GetComponent<Image>();
                so.FindProperty("attributeText").objectReferenceValue = attrTmp;
                so.FindProperty("typeText").objectReferenceValue = typeTextGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("effectRect").objectReferenceValue = (RectTransform)effectGo.transform;
                so.FindProperty("effectText").objectReferenceValue = effectTmp;
                so.FindProperty("statsRoot").objectReferenceValue = statsRoot.gameObject;
                so.FindProperty("dmgLabel").objectReferenceValue = dmgLabel.GetComponent<TextMeshProUGUI>();
                so.FindProperty("dmgValue").objectReferenceValue = dmgValue.GetComponent<TextMeshProUGUI>();
                so.FindProperty("defLabel").objectReferenceValue = defLabel.GetComponent<TextMeshProUGUI>();
                so.FindProperty("defValue").objectReferenceValue = defValue.GetComponent<TextMeshProUGUI>();
                so.FindProperty("backOverlay").objectReferenceValue = back.GetComponent<Image>();
                so.FindProperty("highlightFrame").objectReferenceValue = highlight;
                so.FindProperty("compactRoot").objectReferenceValue = compactRoot;
                so.FindProperty("cChassis").objectReferenceValue = cChassis.GetComponent<Image>();
                so.FindProperty("cArt").objectReferenceValue = cArt.GetComponent<Image>();
                so.FindProperty("cName").objectReferenceValue = cNameTmp;
                so.FindProperty("cMeta").objectReferenceValue = cMeta.gameObject;
                so.FindProperty("cPip").objectReferenceValue = cPip.GetComponent<Image>();
                so.FindProperty("cAttr").objectReferenceValue = cAttrGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("cType").objectReferenceValue = cTypeGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("cStats").objectReferenceValue = cStats.gameObject;
                so.FindProperty("cAtk").objectReferenceValue = cAtkGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("cDef").objectReferenceValue = cDefGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("cFooter").objectReferenceValue = cFooterTmp;
                so.FindProperty("cCrest").objectReferenceValue = cCrest.GetComponent<Image>();
                so.FindProperty("cCrestText").objectReferenceValue = cCrestTextGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("cBack").objectReferenceValue = cBack.GetComponent<Image>();
                so.FindProperty("cHighlight").objectReferenceValue = cHighlight;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("TcgCard.prefab (Reliquary) neu gebaut.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Vector2 TL() => new Vector2(0f, 1f);

        private static Transform Child(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            if (anchorMin == anchorMax)
            {
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
            }
            else
            {
                rect.offsetMin = new Vector2(-size.x / 2f, -size.y / 2f);
                rect.offsetMax = new Vector2(size.x / 2f, size.y / 2f);
            }
            return go.transform;
        }

        private static GameObject ImageChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Sprite sprite)
        {
            Vector2 pivot = anchorMin == anchorMax && anchorMin == TL() ? TL() : new Vector2(0.5f, 0.5f);
            var t = Child(parent, name, anchorMin, anchorMax, pivot, pos, size);
            var image = t.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return t.gameObject;
        }

        private static GameObject TextChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, TMP_FontAsset font, float fontSize, TextAlignmentOptions align)
        {
            Vector2 pivot = anchorMin == anchorMax && anchorMin == TL() ? TL() : new Vector2(0.5f, 0.5f);
            var t = Child(parent, name, anchorMin, anchorMax, pivot, pos, size);
            var text = t.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.raycastTarget = false;
            text.text = "";
            return t.gameObject;
        }
    }
}
