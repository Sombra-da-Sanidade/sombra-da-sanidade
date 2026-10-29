using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Aplica as regras de importação de pixel art (docs/arte/guia-de-estilo.md) a tudo em
    // Art/Tiles, Art/Sprites, Art/Monster, Art/Portraits e Art/Palette, e fatia as folhas usando o JSON de art-src/
    // (mesmo arquivo que tools/art/export_sprites.py usa para gerar o PNG).
    public class PixelArtImporter : AssetPostprocessor
    {
        // Tile de 16px = 1 unidade do mundo (guia de estilo, "Escala").
        public const int PixelsPerUnit = 16;
        private const string ArtRoot = "Assets/_Project/Art/";
        private static readonly string[] Categories = { "Tiles", "Sprites", "Monster", "Portraits", "Palette" };

        [Serializable]
        private class Sheet
        {
            public int[] cell;
            public int columns;
            public Frame[] frames;
        }

        [Serializable]
        private class Frame
        {
            public string name;
        }

        private void OnPreprocessTexture()
        {
            string category = GetCategory(assetPath);
            if (category == null) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            // Sem isso, folhas com proporção 6:1 ou 4:3 podem ser importadas como Cubemap.
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            // O PlayerSpriteAnimator lê a folha do personagem para achar a lente da lanterna em cada quadro.
            importer.isReadable = category == "Sprites";

            var sheet = LoadSheet(category, assetPath);
            if (sheet == null || sheet.frames == null || sheet.frames.Length == 0)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            Slice(importer, sheet, category);
        }

        private static string GetCategory(string path)
        {
            if (!path.StartsWith(ArtRoot) || !path.EndsWith(".png")) return null;
            string rest = path.Substring(ArtRoot.Length);
            return Categories.FirstOrDefault(c => rest.StartsWith(c + "/"));
        }

        private static Sheet LoadSheet(string category, string pngPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string json = Path.Combine(projectRoot, "art-src", category, Path.GetFileNameWithoutExtension(pngPath) + ".json");
            if (!File.Exists(json)) return null;
            return JsonUtility.FromJson<Sheet>(File.ReadAllText(json));
        }

        private static void Slice(TextureImporter importer, Sheet sheet, string category)
        {
            int cw = sheet.cell[0];
            int ch = sheet.cell[1];
            int count = sheet.frames.Length;
            // Mesmo cálculo de layout do export_sprites.py.
            int columns = Mathf.Clamp(sheet.columns > 0 ? sheet.columns : count, 1, count);
            int rows = (count + columns - 1) / columns;
            int height = rows * ch;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Reaproveita o ID dos sprites que já existem para não quebrar referências em cenas/prefabs.
            var existing = provider.GetSpriteRects()
                .GroupBy(r => r.name)
                .ToDictionary(g => g.Key, g => g.First());

            // Personagens e monstro pisam no chão: pivô nos pés. Tiles e retratos: pivô no centro.
            var alignment = category == "Tiles" || category == "Portraits" ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;

            // Cópia do layout atual antes de mexer (SpriteRect é referência).
            var current = provider.GetSpriteRects().Select(r => (r.name, r.rect, r.alignment)).ToList();
            var rects = new List<SpriteRect>(count);
            for (int i = 0; i < count; i++)
            {
                string name = sheet.frames[i].name;
                if (!existing.TryGetValue(name, out var rect))
                    rect = new SpriteRect { name = name, spriteID = StableId(importer.assetPath, name) };

                int x = (i % columns) * cw;
                int yFromTop = (i / columns) * ch;
                rect.rect = new Rect(x, height - yFromTop - ch, cw, ch);
                rect.alignment = alignment;
                rect.pivot = alignment == SpriteAlignment.BottomCenter ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
                rects.Add(rect);
            }

            // Só grava quando o layout mudou: aplicar sem necessidade durante a importação
            // descarta os sprites gerados nesta passada.
            if (SameLayout(current, rects)) return;

            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
        }

        private static bool SameLayout(List<(string name, Rect rect, SpriteAlignment alignment)> current, List<SpriteRect> wanted)
        {
            if (current.Count != wanted.Count) return false;
            for (int i = 0; i < current.Count; i++)
            {
                var c = current[i];
                var w = wanted[i];
                if (c.name != w.name || c.rect != w.rect || c.alignment != w.alignment) return false;
            }
            return true;
        }

        // ID derivado de caminho + nome: quem importar primeiro gera o mesmo ID que os outros.
        private static GUID StableId(string path, string name)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(path + "#" + name));
            return new GUID(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
        }
    }
}
