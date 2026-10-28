using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace SombraDaSanidade.EditorTools
{
    // Gera Tile assets a partir de Art/Tiles/lab_tiles.png e uma sala de teste com Light2D
    // (issue #13: testar a leitura do tileset no escuro e sob a lanterna).
    public static class ArtTestSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string SheetPath = Root + "/Art/Tiles/lab_tiles.png";
        private const string TileAssetDir = Root + "/Art/Tiles/Lab";
        public const string ScenePath = Root + "/Scenes/Sandbox/ArteTeste.unity";

        private const int Width = 14;
        private const int FloorRows = 7;

        [MenuItem("Sombra da Sanidade/Arte/Gerar Tile assets do laboratório", priority = 40)]
        public static void BuildTiles() => LoadOrCreateTiles();

        [MenuItem("Sombra da Sanidade/Arte/Gerar cena de teste de arte", priority = 41)]
        public static void BuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LoadOrCreateTiles();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Recarrega depois do NewScene (mesmo motivo do PrototypeSceneBuilder).
            var tiles = LoadOrCreateTiles();
            var sprites = LoadSprites();

            var grid = new GameObject("Grid").AddComponent<Grid>();
            var floor = CreateTilemap(grid.transform, "Floor", -10);
            var walls = CreateTilemap(grid.transform, "Walls", -5);
            var decals = CreateTilemap(grid.transform, "Decals", -8);

            // Chão: maioria liso, juntas e variações espalhadas (repetição sem padrão visível).
            var rng = new System.Random(4);
            string[] floorPool = { "floor_a", "floor_a", "floor_a", "floor_a", "floor_a", "floor_a", "floor_seam", "floor_seam", "floor_b", "floor_c" };
            for (int y = 0; y < FloorRows; y++)
                for (int x = 0; x < Width; x++)
                    floor.SetTile(new Vector3Int(x, y, 0), tiles[floorPool[rng.Next(floorPool.Length)]]);
            for (int x = 4; x <= 8; x++)
                floor.SetTile(new Vector3Int(x, 1, 0), tiles["floor_grate"]);
            floor.SetTile(new Vector3Int(6, FloorRows - 1, 0), tiles["floor_hazard"]);

            // Parede em vista 3/4: duas fileiras de face + topo escuro.
            string[] faceRow = { "wall_face", "wall_face_pipes", "wall_terminal", "wall_face", "breaker_off", "wall_face", null,
                "wall_face", "wall_face_pipes", "breaker_on", "wall_face", null, null, "wall_face" };
            for (int x = 0; x < Width; x++)
            {
                walls.SetTile(new Vector3Int(x, FloorRows + 2, 0), tiles["wall_top"]);
                walls.SetTile(new Vector3Int(x, FloorRows + 1, 0), tiles["wall_face"]);
                if (faceRow[x] != null) walls.SetTile(new Vector3Int(x, FloorRows, 0), tiles[faceRow[x]]);
            }
            walls.SetTile(new Vector3Int(6, FloorRows + 1, 0), tiles["door_top"]);
            walls.SetTile(new Vector3Int(6, FloorRows, 0), tiles["door_bottom"]);
            walls.SetTile(new Vector3Int(11, FloorRows + 1, 0), tiles["server_top"]);
            walls.SetTile(new Vector3Int(11, FloorRows, 0), tiles["server_bottom"]);
            walls.SetTile(new Vector3Int(12, FloorRows + 1, 0), tiles["server_top"]);
            walls.SetTile(new Vector3Int(12, FloorRows, 0), tiles["server_bottom_empty"]);

            decals.SetTile(new Vector3Int(2, 4, 0), tiles["puddle"]);
            decals.SetTile(new Vector3Int(9, 5, 0), tiles["blood_smear"]);
            decals.SetTile(new Vector3Int(4, 5, 0), tiles["papers"]);

            CreateLights(sprites);
            CreateCamera();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Cena de teste de arte gerada em {ScenePath}");
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            return map;
        }

        private static void CreateLights(Dictionary<string, Sprite> sprites)
        {
            var global = new GameObject("Global Light 2D").AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.08f;
            global.color = new Color(0.55f, 0.6f, 0.75f);

            // Lanterna: mesmos valores padrão do FlashlightConfig (GDD 4: cone 60°, alcance 8).
            var cone = new GameObject("Lanterna (teste)").AddComponent<Light2D>();
            cone.transform.position = new Vector3(3.5f, 1.5f, 0f);
            cone.transform.rotation = Quaternion.Euler(0f, 0f, -40f);
            cone.lightType = Light2D.LightType.Point;
            cone.color = new Color(1f, 0.93f, 0.78f);
            cone.intensity = 1.1f;
            cone.pointLightInnerRadius = 0.2f;
            cone.pointLightOuterRadius = 8f;
            cone.pointLightOuterAngle = 60f;
            cone.pointLightInnerAngle = 36f;

            // Lâmpada de teto: Zona Segura (luz quente, âmbar da paleta).
            var lamp = new GameObject("Lampada de teto");
            lamp.transform.position = new Vector3(11.5f, 2.5f, 0f);
            var sr = lamp.AddComponent<SpriteRenderer>();
            sr.sprite = sprites["ceiling_lamp"];
            sr.sortingOrder = 5;
            var lampLight = new GameObject("Luz").AddComponent<Light2D>();
            lampLight.transform.SetParent(lamp.transform, false);
            lampLight.lightType = Light2D.LightType.Point;
            lampLight.color = new Color(0.96f, 0.85f, 0.47f);
            lampLight.intensity = 0.9f;
            lampLight.pointLightOuterRadius = 3f;

            // Brilho dos LEDs dos servidores (verde-fósforo).
            foreach (float x in new[] { 11.5f, 12.5f })
            {
                var led = new GameObject("LED").AddComponent<Light2D>();
                led.transform.position = new Vector3(x, FloorRows + 0.5f, 0f);
                led.lightType = Light2D.LightType.Point;
                led.color = new Color(0.33f, 0.72f, 0.44f);
                led.intensity = 0.5f;
                led.pointLightOuterRadius = 1.2f;
            }
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(Width / 2f, (FloorRows + 3) / 2f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.AddComponent<UniversalAdditionalCameraData>();
            // Pixel perfect: 16 PPU numa base 320x180 (guia de estilo, "Escala").
            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PixelArtImporter.PixelsPerUnit;
            ppc.refResolutionX = 320;
            ppc.refResolutionY = 180;
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            return AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }

        private static Dictionary<string, Tile> LoadOrCreateTiles()
        {
            var sprites = LoadSprites();
            if (sprites.Count == 0)
            {
                Debug.LogError($"Nenhum sprite em {SheetPath}. Rode python3 tools/art/export_sprites.py.");
                return new Dictionary<string, Tile>();
            }

            Directory.CreateDirectory(TileAssetDir);
            var tiles = new Dictionary<string, Tile>();
            foreach (var pair in sprites)
            {
                string path = $"{TileAssetDir}/{pair.Key}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.sprite = pair.Value;
                EditorUtility.SetDirty(tile);
                tiles[pair.Key] = tile;
            }
            AssetDatabase.SaveAssets();
            return tiles;
        }
    }
}
