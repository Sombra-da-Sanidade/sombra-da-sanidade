using System.Collections.Generic;
using NavMeshPlus.Components;
using SombraDaSanidade.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SombraDaSanidade.EditorTools
{
    // Rule Tile das paredes em vista 3/4 (Art/Tiles/Lab/wall_rule.asset). Pinta-se a parede inteira com ele no
    // tilemap Walls e o próprio tile escolhe o sprite olhando os vizinhos, sem script:
    //   - chão logo abaixo ou dois abaixo: wall_face (as duas fileiras de face);
    //   - chão em volta (1 para os lados, até 3 abaixo e 1 acima): wall_top;
    //   - miolo: sem sprite (fica o preto do fundo da câmera).
    // "Chão" = célula vazia no Walls. Peças especiais (servidor, disjuntor, porta, terminal, canos) ficam no
    // tilemap WallDetails, por cima da parede, para não contarem como chão.
    // Novos níveis: pintar wall_rule no Walls e as peças no WallDetails pela Tile Palette.
    public static class WallRuleTile
    {
        public const string AssetPath = "Assets/_Project/Art/Tiles/Lab/wall_rule.asset";
        public const string DetailsName = "WallDetails";

        // Folga de parede em volta do mapa: o Rule Tile trata fora do tilemap como chão, então a borda
        // falsa fica longe da câmera.
        public const int Margin = 20;

        private const int NotThis = RuleTile.TilingRuleOutput.Neighbor.NotThis;

        // Tiles que o Rule Tile substitui. O resto do Walls vai para o WallDetails.
        private static readonly HashSet<string> PlainWalls = new HashSet<string> { "wall_face", "wall_top" };

        public static RuleTile GetOrCreate(Dictionary<string, Tile> tiles)
        {
            var rule = AssetDatabase.LoadAssetAtPath<RuleTile>(AssetPath);
            bool create = rule == null;
            if (create) rule = ScriptableObject.CreateInstance<RuleTile>();

            var face = tiles["wall_face"].sprite;
            var top = tiles["wall_top"].sprite;
            rule.m_DefaultSprite = null;
            // Sprite com sprite nulo: o NavMeshPlus ignora o miolo (com None ele viraria uma caixa por célula).
            rule.m_DefaultColliderType = Tile.ColliderType.Sprite;
            rule.m_TilingRules = new List<RuleTile.TilingRule>
            {
                Rule(face, new Vector3Int(0, -1, 0)),
                Rule(face, new Vector3Int(0, -2, 0)),
            };
            for (int dy = 1; dy >= -3; dy--)
                for (int dx = -1; dx <= 1; dx++)
                    if (dx != 0 || dy != 0) rule.m_TilingRules.Add(Rule(top, new Vector3Int(dx, dy, 0)));

            if (create) AssetDatabase.CreateAsset(rule, AssetPath);
            EditorUtility.SetDirty(rule);
            AssetDatabase.SaveAssets();
            return rule;
        }

        private static RuleTile.TilingRule Rule(Sprite sprite, Vector3Int floorAt) => new RuleTile.TilingRule
        {
            m_Sprites = new[] { sprite },
            m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_ColliderType = Tile.ColliderType.Sprite,
            m_NeighborPositions = new List<Vector3Int> { floorAt },
            m_Neighbors = new List<int> { NotThis },
        };

        // Pinta o Rule Tile em todas as células sem chão de [0, width) x [0, height), mais a folga.
        public static void Fill(Tilemap walls, RuleTile rule, int width, int height, System.Func<int, int, bool> isFloor)
        {
            var cells = new List<Vector3Int>();
            for (int y = -Margin; y < height + Margin; y++)
                for (int x = -Margin; x < width + Margin; x++)
                    if (!isFloor(x, y)) cells.Add(new Vector3Int(x, y, 0));
            var tilesArray = new TileBase[cells.Count];
            for (int i = 0; i < tilesArray.Length; i++) tilesArray[i] = rule;
            walls.SetTiles(cells.ToArray(), tilesArray);
        }

        // Cria o WallDetails irmão do Walls, por cima dele e também bloqueando o NavMesh.
        public static Tilemap CreateDetails(Tilemap walls)
        {
            var go = new GameObject(DetailsName);
            go.transform.SetParent(walls.transform.parent, false);
            var details = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = walls.GetComponent<TilemapRenderer>().sortingOrder + 1;
            var block = go.AddComponent<NavMeshModifier>();
            block.overrideArea = true;
            block.area = 1; // Not Walkable
            return details;
        }

        // ---------- Migração das cenas já montadas ----------

        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Main/Alfa.unity",
            "Assets/_Project/Scenes/Main/Beta.unity",
            "Assets/_Project/Scenes/Wings/AlaMedica.unity",
            "Assets/_Project/Scenes/Wings/AlaEnergia.unity",
            "Assets/_Project/Scenes/Wings/AlaExecutiva.unity",
            "Assets/_Project/Scenes/Wings/Eclusa.unity",
        };

        // Converte o Walls de cada cena sem regerar a cena (mantém o que foi ajustado à mão): as peças especiais
        // vão para o WallDetails, e a parede vira Rule Tile. O resultado visual é o mesmo do builder antigo.
        [MenuItem("Sombra da Sanidade/Arte/Converter paredes para Rule Tile", priority = 43)]
        public static void MigrateAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var rule = GetOrCreate(ArtTestSceneBuilder.LoadOrCreateTiles());
            foreach (var path in Scenes) Migrate(path, rule);
        }

        private static void Migrate(string path, RuleTile rule)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var wallsGo = GameObject.Find("Grid/Walls");
            var floorGo = GameObject.Find("Grid/Floor");
            if (wallsGo == null || floorGo == null || GameObject.Find("Grid/" + DetailsName) != null)
            {
                Debug.Log($"[RuleTile] {path}: nada a converter.");
                return;
            }
            var walls = wallsGo.GetComponent<Tilemap>();
            var floor = floorGo.GetComponent<Tilemap>();
            var details = CreateDetails(walls);

            walls.CompressBounds();
            floor.CompressBounds();
            int moved = 0;
            foreach (var cell in walls.cellBounds.allPositionsWithin)
            {
                var tile = walls.GetTile(cell);
                if (tile == null || PlainWalls.Contains(tile.name)) continue;
                details.SetTile(cell, tile);
                moved++;
            }
            walls.ClearAllTiles();

            var b = floor.cellBounds;
            Fill(walls, rule, b.xMax, b.yMax, (x, y) => floor.HasTile(new Vector3Int(x, y, 0)));

            // A subestação troca o próprio tile ao ser religada: agora ele mora no WallDetails.
            foreach (var substation in Object.FindObjectsByType<Substation>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(substation);
                var prop = so.FindProperty("wallTiles");
                if (prop.objectReferenceValue == walls) prop.objectReferenceValue = details;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RuleTile] {path}: paredes em Rule Tile, {moved} peças no {DetailsName}.");
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.WallRuleTile.MigrateBatch
        public static void MigrateBatch()
        {
            MigrateAll();
            EditorApplication.Exit(0);
        }
    }
}
