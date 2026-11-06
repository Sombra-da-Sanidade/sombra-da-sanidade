using System;
using System.Collections.Generic;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Índice das folhas de art-src/UI (HUD e telas). Gerado pelo menu
    // Sombra da Sanidade > Arte > Atualizar HudArt (HudArtBuilder), que lê os JSON e aponta as texturas.
    // Fica em Resources para qualquer HUD achar sem referência na cena (cenas antigas continuam funcionando).
    public class HudArt : ScriptableObject
    {
        public const string ResourcePath = "HudArt";

        [Serializable]
        public class Sheet
        {
            public string id;
            public Texture2D texture;
            public Vector2Int cell;
            public int columns;
            public string[] frames;
            // Só na fonte: caractere e avanço de cada frame.
            public string chars;
            public int[] advances;
        }

        public Sheet[] sheets = Array.Empty<Sheet>();

        [Header("Multímetro (skillcheck.json, bloco dial)")]
        [Tooltip("Pixels da faixa do mostrador: x, y e t (0 = esquerda, 1 = direita).")]
        public Vector3[] dialArc = Array.Empty<Vector3>();
        public Vector2Int dialPivot = new Vector2Int(23, 19);
        public int dialNeedleRadius = 11;
        [Tooltip("Ângulo do ponteiro em graus para t = 0 (x) e t = 1 (y).")]
        public Vector2 dialAngles = new Vector2(158f, 22f);

        private static HudArt instance;
        private static bool loaded;

        private Dictionary<string, (Sheet sheet, int index)> frames;
        private Dictionary<char, int> glyphs;
        private Sheet font;

        public static HudArt Instance
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    instance = Resources.Load<HudArt>(ResourcePath);
                }
                return instance;
            }
        }

        // Com "Enter Play Mode" sem recarregar o domínio, o cache estático precisa zerar.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            instance = null;
            loaded = false;
        }

        private void OnEnable() => frames = null;

        public bool Has(string sheet, string frame) => TryGet(sheet, frame, out _, out _, out _);

        public bool TryGet(string sheet, string frame, out Texture2D texture, out Rect uv, out Vector2Int size)
        {
            BuildIndex();
            texture = null;
            uv = default;
            size = default;
            if (!frames.TryGetValue(sheet + "/" + frame, out var entry) || entry.sheet.texture == null) return false;
            Frame(entry.sheet, entry.index, out uv);
            texture = entry.sheet.texture;
            size = entry.sheet.cell;
            return true;
        }

        public bool TryGlyph(char c, out Texture2D texture, out Rect uv, out int advance)
        {
            BuildIndex();
            texture = null;
            uv = default;
            advance = 4;
            if (font == null || font.texture == null || !glyphs.TryGetValue(c, out int index)) return false;
            Frame(font, index, out uv);
            texture = font.texture;
            advance = font.advances != null && index < font.advances.Length ? font.advances[index] : font.cell.x;
            return true;
        }

        public Vector2Int GlyphCell => font != null ? font.cell : new Vector2Int(6, 8);

        private static void Frame(Sheet sheet, int index, out Rect uv)
        {
            float tw = sheet.texture.width, th = sheet.texture.height;
            int col = index % Mathf.Max(1, sheet.columns), row = index / Mathf.Max(1, sheet.columns);
            // UV começa embaixo; a folha é lida de cima para baixo.
            uv = new Rect(col * sheet.cell.x / tw, 1f - (row + 1) * sheet.cell.y / th, sheet.cell.x / tw, sheet.cell.y / th);
        }

        private void BuildIndex()
        {
            if (frames != null) return;
            frames = new Dictionary<string, (Sheet, int)>();
            glyphs = new Dictionary<char, int>();
            font = null;
            foreach (var sheet in sheets)
            {
                if (sheet == null || sheet.frames == null) continue;
                for (int i = 0; i < sheet.frames.Length; i++) frames[sheet.id + "/" + sheet.frames[i]] = (sheet, i);
                if (!string.IsNullOrEmpty(sheet.chars))
                {
                    font = sheet;
                    for (int i = 0; i < sheet.chars.Length; i++) glyphs[sheet.chars[i]] = i;
                }
            }
        }
    }
}
