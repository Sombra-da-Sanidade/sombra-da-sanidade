using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera Resources/HudArt.asset a partir de art-src/UI/*.json (nomes dos frames, fonte e arco do multímetro)
    // e das texturas exportadas em Art/UI. Rodar depois de mudar qualquer folha de art-src/UI.
    public static class HudArtBuilder
    {
        public const string AssetPath = "Assets/_Project/Resources/HudArt.asset";
        private const string ArtFolder = "Assets/_Project/Art/UI/";
        private static readonly string[] SheetIds = { "skillcheck", "hud_icons", "hud_bars", "ui_font", "screens", "logo" };

        [Serializable] private class FrameJson { public string name; public string @char; public int advance; }
        [Serializable] private class DialJson { public int[] pivot; public int needleRadius; public float[] angles; }
        [Serializable] private class SheetJson { public int[] cell; public int columns; public FrameJson[] frames; public DialJson dial; }

        [MenuItem("Sombra da Sanidade/Arte/Atualizar HudArt")]
        public static void BuildMenu()
        {
            var art = Build();
            Selection.activeObject = art;
            Debug.Log($"[HudArt] {art.sheets.Length} folhas, {art.dialArc.Length} pixels no arco do multímetro.");
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.HudArtBuilder.BuildBatch
        public static void BuildBatch()
        {
            AssetDatabase.Refresh();
            Build();
            EditorApplication.Exit(0);
        }

        public static HudArt Build()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            var art = AssetDatabase.LoadAssetAtPath<HudArt>(AssetPath);
            bool created = art == null;
            if (created) art = ScriptableObject.CreateInstance<HudArt>();

            var sheets = new List<HudArt.Sheet>();
            foreach (var id in SheetIds)
            {
                string jsonPath = Path.Combine(root, "art-src", "UI", id + ".json");
                if (!File.Exists(jsonPath))
                {
                    Debug.LogWarning($"[HudArt] Falta {jsonPath}");
                    continue;
                }
                string text = File.ReadAllText(jsonPath);
                var json = JsonUtility.FromJson<SheetJson>(text);
                var sheet = new HudArt.Sheet
                {
                    id = id,
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + id + ".png"),
                    cell = new Vector2Int(json.cell[0], json.cell[1]),
                    columns = json.columns,
                    frames = new string[json.frames.Length],
                };
                bool isFont = json.frames.Length > 0 && !string.IsNullOrEmpty(json.frames[0].@char);
                if (isFont) sheet.advances = new int[json.frames.Length];
                var chars = new System.Text.StringBuilder();
                for (int i = 0; i < json.frames.Length; i++)
                {
                    sheet.frames[i] = json.frames[i].name;
                    if (!isFont) continue;
                    chars.Append(json.frames[i].@char);
                    sheet.advances[i] = json.frames[i].advance;
                }
                if (isFont) sheet.chars = chars.ToString();
                if (sheet.texture == null) Debug.LogWarning($"[HudArt] Textura não encontrada: {ArtFolder}{id}.png");
                sheets.Add(sheet);

                if (json.dial != null && json.dial.pivot != null && json.dial.pivot.Length == 2)
                {
                    art.dialPivot = new Vector2Int(json.dial.pivot[0], json.dial.pivot[1]);
                    art.dialNeedleRadius = json.dial.needleRadius;
                    art.dialAngles = new Vector2(json.dial.angles[0], json.dial.angles[1]);
                    art.dialArc = ParseArc(text);
                }
            }
            art.sheets = sheets.ToArray();

            if (created)
            {
                Directory.CreateDirectory(Path.Combine(root, Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(art, AssetPath);
            }
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art;
        }

        // JsonUtility não lê listas aninhadas: "arc": [[x, y, t], ...] é lido à mão.
        private static Vector3[] ParseArc(string json)
        {
            int start = json.IndexOf("\"arc\"", StringComparison.Ordinal);
            if (start < 0) return Array.Empty<Vector3>();
            int open = json.IndexOf('[', start);
            int depth = 0, end = open;
            for (; end < json.Length; end++)
            {
                if (json[end] == '[') depth++;
                else if (json[end] == ']' && --depth == 0) break;
            }
            var list = new List<Vector3>();
            foreach (Match m in Regex.Matches(json.Substring(open + 1, end - open - 1), @"\[\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*\]"))
            {
                list.Add(new Vector3(
                    float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)));
            }
            return list.ToArray();
        }
    }
}
