using System.Collections.Generic;
using System.IO;
using System.Linq;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace SombraDaSanidade.EditorTools
{
    // Monta a cena do alfa com os tiles do laboratório a partir da planta em texto art-src/Maps/alfa.txt
    // (1 caractere = 1 tile; legenda no próprio arquivo). Saguão com a eclusa, Ala Médica, Ala de Energia e
    // Ala Executiva ligadas por corredores em loop (GDD 4.1). Jogador, monstro, HUD e sanidade vêm do
    // PrototypeSceneBuilder, então a cena se comporta igual ao protótipo.
    // Menu: Sombra da Sanidade > Gerar cena Alfa (tiles). Mudar o mapa = editar o .txt e gerar de novo.
    public static class AlfaSceneBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Main/Alfa.unity";
        private const string LayoutFile = "art-src/Maps/alfa.txt";

        // Caracteres que são chão (o resto é parede/teto).
        private const string FloorChars = ".,hwpbL@Mmi";

        [MenuItem("Sombra da Sanidade/Gerar cena Alfa (tiles)", priority = 2)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AlfaSceneBuilder.Build
        public static void Build() => Build(LayoutFile, ScenePath, drivesRequired: 3, wing: null);

        // wing nulo = nome da ala pela posição no mapa (Alfa tem as 3 alas lado a lado).
        private static void Build(string layoutFile, string scenePath, int drivesRequired, string wing)
        {
            var map = LoadLayout(layoutFile);
            if (map == null) return;

            // Assets antes de trocar de cena (mesmo motivo do PrototypeSceneBuilder).
            ArtTestSceneBuilder.LoadOrCreateTiles();
            PrototypeSceneBuilder.GetOrCreatePlaceholderSprite();
            DriveServerPrefabBuilder.GetOrBuild();
            int wallLayer = PrototypeSceneBuilder.EnsureLayer(PrototypeSceneBuilder.WallLayerName);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tiles = ArtTestSceneBuilder.LoadOrCreateTiles();
            var square = PrototypeSceneBuilder.GetOrCreatePlaceholderSprite();
            var playerConfig = PrototypeSceneBuilder.GetOrCreateConfig<PlayerConfig>("PlayerConfig");
            var flashlightConfig = PrototypeSceneBuilder.GetOrCreateConfig<FlashlightConfig>("FlashlightConfig");
            var sanityConfig = PrototypeSceneBuilder.GetOrCreateConfig<SanityConfig>("SanityConfig");

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            var gmSo = new SerializedObject(gm);
            gmSo.FindProperty("drivesRequired").intValue = drivesRequired;
            gmSo.ApplyModifiedPropertiesWithoutUndo();
            PrototypeSceneBuilder.CreateGlobalLight();
            BuildTilemaps(map, tiles);
            BuildWallColliders(map, wallLayer);
            PrototypeSceneBuilder.CreateNavMesh();

            var objectives = new GameObject("Objectives").transform;
            var items = new GameObject("SanityItems").transform;
            Vector2 playerSpawn = new Vector2(map.Width / 2f, 3.5f);
            Vector2 monsterSpawn = playerSpawn + Vector2.up * 18f;
            var patrol = new List<Vector2>();
            var exitCells = new List<Vector2Int>();
            int breakers = 0;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    char c = map[x, y];
                    var center = new Vector2(x + 0.5f, y + 0.5f);
                    var inFront = center + Vector2.down;   // célula de chão logo abaixo da face da parede
                    switch (c)
                    {
                        case 'S': CreateServer(objectives, square, $"DriveServer_{wing ?? Wing(x, map.Width)}", inFront, center); break;
                        case 'K': CreateBreaker(objectives, square, $"Breaker_{wing ?? Wing(x, map.Width)}_{++breakers}", inFront); break;
                        case 'E': exitCells.Add(new Vector2Int(x, y)); break;
                        case '@': playerSpawn = center; break;
                        case 'M': monsterSpawn = center; break;
                        case 'm': patrol.Add(center); break;
                        case 'i': PrototypeSceneBuilder.CreateStabilizer(items, square, $"Stabilizer_{x}_{y}", center); break;
                    }
                }
            }
            if (exitCells.Count > 0) CreateExit(objectives, square, exitCells);
            CreateClues(items, objectives);

            var player = PrototypeSceneBuilder.CreatePlayer(square, playerConfig, flashlightConfig, sanityConfig, wallLayer, playerSpawn);
            PrototypeSceneBuilder.CreateCamera(player.transform);
            PrototypeSceneBuilder.CreateHud(player.GetComponent<FlashlightController>());
            PrototypeSceneBuilder.CreateSanityEffects(player.GetComponent<SanityController>(), square, wallLayer);
            PrototypeSceneBuilder.CreateMonster(monsterSpawn, patrol);

            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            PrototypeSceneBuilder.AddSceneToBuild(scenePath);
            Debug.Log($"Cena gerada em {scenePath} ({map.Width}x{map.Height} tiles)");
        }

        // ---------- Planta ----------

        private class Layout
        {
            public int Width, Height;
            public char[,] Cells;   // [x, y], y = 0 embaixo
            public char this[int x, int y] => x < 0 || y < 0 || x >= Width || y >= Height ? '#' : Cells[x, y];
            public bool IsFloor(int x, int y) => FloorChars.IndexOf(this[x, y]) >= 0;
        }

        private static Layout LoadLayout(string layoutFile)
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), layoutFile);
            if (!File.Exists(path))
            {
                Debug.LogError($"Planta do mapa não encontrada: {layoutFile}");
                return null;
            }
            // Linhas de comentário começam com "# " (com espaço); as do mapa não têm espaço.
            var rows = File.ReadAllLines(path).Where(l => l.Length > 0 && !l.Contains(' ')).ToList();
            int w = rows.Max(r => r.Length), h = rows.Count;
            var cells = new char[w, h];
            for (int row = 0; row < h; row++)
                for (int x = 0; x < w; x++)
                    cells[x, h - 1 - row] = x < rows[row].Length ? rows[row][x] : '#';
            return new Layout { Width = w, Height = h, Cells = cells };
        }

        private static string Wing(int x, int width)
        {
            if (x < width / 3) return "Medica";
            if (x > width * 2 / 3) return "Energia";
            return "Executiva";
        }

        // ---------- Tiles ----------

        private static void BuildTilemaps(Layout map, Dictionary<string, Tile> tiles)
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            var floor = CreateTilemap(grid.transform, "Floor", -10);
            var decals = CreateTilemap(grid.transform, "Decals", -8);
            var walls = CreateTilemap(grid.transform, "Walls", -5);
            // NavMesh: o chão é andável e as paredes não (NavMeshPlus coleta tilemaps com NavMeshModifier).
            floor.gameObject.AddComponent<NavMeshModifier>();
            var block = walls.gameObject.AddComponent<NavMeshModifier>();
            block.overrideArea = true;
            block.area = 1; // Not Walkable

            // Chão: maioria liso, juntas e variações espalhadas (guia de estilo, regra 7).
            var rng = new System.Random(4);
            string[] pool = { "floor_a", "floor_a", "floor_a", "floor_a", "floor_a", "floor_a", "floor_seam", "floor_seam", "floor_b", "floor_c" };

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    char c = map[x, y];
                    if (map.IsFloor(x, y))
                    {
                        string f = c == ',' ? "floor_grate" : c == 'h' ? "floor_hazard" : pool[rng.Next(pool.Length)];
                        Set(floor, tiles, cell, f);
                        string decal = c switch { 'w' => "puddle", 'p' => "papers", 'b' => "blood_smear", 'L' => "ceiling_lamp", _ => null };
                        if (decal != null) Set(decals, tiles, cell, decal);
                        continue;
                    }

                    // Parede em vista 3/4: duas fileiras de face acima do chão, depois o topo escuro.
                    string t;
                    if (map.IsFloor(x, y - 1))
                        t = c switch { 'S' => "server_bottom", 'K' => "breaker_off", 'T' => "wall_terminal", 'P' => "wall_face_pipes", 'E' => "door_bottom", _ => "wall_face" };
                    else if (map.IsFloor(x, y - 2))
                        t = map[x, y - 1] switch { 'S' => "server_top", 'E' => "door_top", _ => "wall_face" };
                    else if (NearFloor(map, x, y))
                        t = "wall_top";
                    else
                        continue; // miolo das paredes fica preto (fundo da câmera)
                    Set(walls, tiles, cell, t);
                }
            }
        }

        private static bool NearFloor(Layout map, int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -3; dy <= 1; dy++)
                    if (map.IsFloor(x + dx, y + dy)) return true;
            return false;
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            return tilemap;
        }

        private static void Set(Tilemap map, Dictionary<string, Tile> tiles, Vector3Int cell, string name)
        {
            if (tiles.TryGetValue(name, out var tile)) map.SetTile(cell, tile);
            else Debug.LogWarning($"Tile {name} não existe em Art/Tiles/lab_tiles.");
        }

        // ---------- Colisão e sombra ----------

        // Junta as células de parede em retângulos (linhas iguais empilhadas viram um bloco só).
        // Cada bloco tem BoxCollider2D (física, Linecast da lanterna e da alucinação) e ShadowCaster2D (luz 2D).
        private static void BuildWallColliders(Layout map, int wallLayer)
        {
            var parent = new GameObject("WallColliders").transform;
            var used = new bool[map.Width, map.Height];
            int count = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (used[x, y] || map.IsFloor(x, y)) continue;
                    int x1 = x;
                    while (x1 + 1 < map.Width && !used[x1 + 1, y] && !map.IsFloor(x1 + 1, y)) x1++;
                    int y1 = y;
                    while (y1 + 1 < map.Height && RowFree(map, used, x, x1, y1 + 1)) y1++;
                    for (int yy = y; yy <= y1; yy++)
                        for (int xx = x; xx <= x1; xx++)
                            used[xx, yy] = true;

                    var go = new GameObject($"Wall_{count++:000}");
                    go.transform.SetParent(parent, false);
                    go.layer = wallLayer;
                    go.transform.position = new Vector3((x + x1 + 1) / 2f, (y + y1 + 1) / 2f, 0f);
                    go.transform.localScale = new Vector3(x1 - x + 1, y1 - y + 1, 1f);
                    go.AddComponent<BoxCollider2D>();
                    var caster = go.AddComponent<ShadowCaster2D>();
                    caster.selfShadows = false;
                }
            }
        }

        private static bool RowFree(Layout map, bool[,] used, int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++)
                if (used[x, y] || map.IsFloor(x, y)) return false;
            return true;
        }

        // ---------- Objetos de jogo ----------

        // O servidor é o tile na parede; o HD (trigger de interação) fica no chão logo à frente.
        private static void CreateServer(Transform parent, Sprite square, string name, Vector2 floorCell, Vector2 wallCell)
        {
            PrototypeSceneBuilder.CreateDriveServer(parent, square, name, floorCell);
            var go = parent.Find(name).gameObject;
            go.GetComponent<SpriteRenderer>().enabled = false;   // o desenho é o tile server_bottom
            var led = go.transform.Find("Indicator");
            led.position = new Vector3(wallCell.x, wallCell.y - 0.25f, 0f);
            led.localScale = new Vector3(0.12f / go.transform.localScale.x, 0.12f / go.transform.localScale.y, 1f);
            var glow = go.transform.Find("Glow");
            if (glow != null) glow.position = wallCell;
        }

        private static void CreateBreaker(Transform parent, Sprite square, string name, Vector2 floorCell)
        {
            PrototypeSceneBuilder.CreateBreaker(parent, square, name, floorCell);
            parent.Find(name).GetComponent<SpriteRenderer>().enabled = false;   // o desenho é o tile breaker_off
        }

        // Eclusa: as portas são tiles; o trigger fica no chão em frente e uma faixa de luz mostra o estado
        // (vermelha trancada, verde com os 3 HDs).
        private static void CreateExit(Transform parent, Sprite square, List<Vector2Int> cells)
        {
            float cx = (float)cells.Average(c => c.x) + 0.5f;
            int wallY = cells[0].y;
            var go = new GameObject("ExitDoor");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cx, wallY - 0.5f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(cells.Count, 1.2f);
            var status = PrototypeSceneBuilder.CreateBlock(go.transform, square, "StatusLight",
                new Vector2(cx, wallY + 0.15f), new Vector2(cells.Count * 0.8f, 0.12f), new Color(0.7f, 0.15f, 0.15f));
            var exit = go.AddComponent<ExitDoor>();
            PrototypeSceneBuilder.SetRef(exit, "doorRenderer", status.GetComponent<SpriteRenderer>());
        }

        // Pistas em fósforo provisórias (Revelação pela Loucura) até a Frente 4 ter os puzzles.
        private static void CreateClues(Transform items, Transform objectives)
        {
            var breaker = objectives.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Breaker_Medica"));
            if (breaker != null)
                PrototypeSceneBuilder.CreateClue(items, "Clue_Fusivel", (Vector2)breaker.position + Vector2.down * 1.6f, "FUSÍVEL 3-7-1", 0.05f);
            var exit = objectives.Find("ExitDoor");
            if (exit != null)
                PrototypeSceneBuilder.CreateClue(items, "Clue_Aviso", (Vector2)exit.position + Vector2.down * 3.5f, "ELE SEGUE A LUZ", 0.08f);
        }
    }
}
