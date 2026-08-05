using Rouge.Tcg;
using Rouge.Tcg.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Rouge.Editor
{
    /// <summary>
    /// Legt das TransitionSkin-Asset in Resources an und befüllt es aus dem CardSkin.
    /// Nach neuen Sprites im CardSkin einfach erneut ausführen.
    /// </summary>
    public static class TransitionSkinBuilder
    {
        private const string AssetPath = "Assets/_Game/Resources/TransitionSkin.asset";
        private const string CardSkinPath = "Assets/_Game/Data/Tcg/CardSkin.asset";

        [MenuItem("Rouge TCG/Rebuild Transition Skin")]
        public static void Rebuild()
        {
            var skin = AssetDatabase.LoadAssetAtPath<TransitionSkin>(AssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<TransitionSkin>();
                AssetDatabase.CreateAsset(skin, AssetPath);
            }

            var card = AssetDatabase.LoadAssetAtPath<CardSkin>(CardSkinPath);
            if (card == null)
            {
                Debug.LogError("[TransitionSkin] CardSkin nicht gefunden: " + CardSkinPath);
                return;
            }

            skin.frame = card.whiteFrame;
            skin.square = card.whiteSquare;
            skin.cardBack = card.backZone;
            skin.parchment = card.parchmentPanel;
            skin.zoneMonster = card.zoneEmptyMonster;
            skin.zoneSpell = card.zoneEmptySpell;
            skin.zoneArtifact = card.zoneEmptyArtifact;
            skin.vignette = Art("EdgeVignette");
            skin.glow = Art("RadialSoft");
            skin.flare = Art("RadialFlare");
            skin.weave = Art("WeaveTile");
            skin.rule = Art("RuleFade");
            skin.fade = Art("LinearFade");
            skin.seal = Art("SealBody");
            skin.ring = Art("SealRing");
            skin.additive = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/CardFrame/UIAdditive.mat");
            skin.finishGloss = Tex("FinishGloss");
            skin.finishRainbow = Tex("FinishRainbow");
            skin.finishGrating = Tex("FinishGrating");
            skin.finishScanlines = Tex("FinishScanlines");
            skin.finishNoise = Tex("FinishNoise");
            skin.finishBand = Tex("FinishBand");
            skin.diagFade = Art("DiagFade");
            skin.dashedRing = Art("DashedRing");
            skin.reliefOuter = Art("ReliefOuter");
            skin.reliefInner = Art("ReliefInner");
            skin.cinzel = card.cinzelBold;
            skin.oswald = card.oswaldMedium;
            skin.spectral = card.spectral;

            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();

            foreach (var field in typeof(TransitionSkin).GetFields())
            {
                var value = field.GetValue(skin) as Object;
                if (value == null) Debug.LogWarning("[TransitionSkin] leer: " + field.Name);
            }
            Debug.Log("[TransitionSkin] aktualisiert: " + AssetPath);
        }

        private static Sprite Art(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/CardFrame/" + name + ".png");

        /// <summary>Finish-Ebenen werden gescrollt, brauchen also die Textur statt eines Sprites.</summary>
        private static Texture Tex(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture>("Assets/_Game/Art/CardFrame/" + name + ".png");
    }
}
