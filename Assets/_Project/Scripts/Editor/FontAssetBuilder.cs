using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SombraDaSanidade.EditorTools
{
    // Gera o font atlas do TextMeshPro para a VT323 (fonte de terminal, licença SIL OFL, em Art/Fonts/VT323).
    // Atlas estático com ASCII + acentos do português: vira um asset comum, usado no menu e na cutscene.
    // Também importa o TMP Essential Resources (shaders e TMP Settings) se ainda não estiver no projeto.
    public static class FontAssetBuilder
    {
        public const string FontPath = "Assets/_Project/Art/Fonts/VT323/VT323-Regular.ttf";
        public const string AssetPath = "Assets/_Project/Art/Fonts/VT323/VT323 SDF.asset";

        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüç°ºª·…";

        [MenuItem("Sombra da Sanidade/Arte/Gerar fonte TMP (VT323)", priority = 42)]
        public static TMP_FontAsset Build()
        {
            EnsureEssentials();
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError($"[Fonte] {FontPath} não encontrada.");
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 48, 6, GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic, false);
            asset.name = "VT323 SDF";
            if (!asset.TryAddCharacters(Charset, out string missing))
                Debug.LogWarning($"[Fonte] Caracteres fora da VT323: {missing}");
            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(asset, AssetPath);
            var atlas = asset.atlasTextures[0];
            atlas.name = "VT323 Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
            asset.material.name = "VT323 Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fonte] Font atlas criado em {AssetPath} ({asset.characterTable.Count} caracteres).");
            return asset;
        }

        // TMP Essential Resources: shaders SDF e TMP Settings. Sem eles o texto não renderiza.
        public static void EnsureEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
            Debug.Log("[Fonte] TMP Essential Resources importado.");
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.FontAssetBuilder.BuildBatch
        public static void BuildBatch()
        {
            Build();
            EditorApplication.Exit(0);
        }
    }
}
