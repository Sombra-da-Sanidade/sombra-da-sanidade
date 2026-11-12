using NavMeshPlus.Components;
using SombraDaSanidade.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SombraDaSanidade.EditorTools
{
    // Perspectiva 3/4 dos props das alas:
    //   - Lâmpada de teto: tilemap Ceiling, desenhado por cima de jogador e monstro, 2 tiles acima do ponto de luz
    //     (altura do teto = as 2 fileiras de face da parede). Antes era um decal no chão.
    //   - Armário: a base encosta no pé da parede (a arte sobe pela face, como servidor e disjuntor) e ganha
    //     uma sombra de contato no piso. Antes avançava 12 px sobre o chão e parecia afundado.
    // Usado pelo AlfaSceneBuilder e pela migração das cenas já montadas.
    public static class PerspectiveFix
    {
        public const string CeilingName = "Ceiling";
        public const int CeilingOrder = 20;   // acima de jogador (10), monstro (9/10) e fantasmas (9)
        public const int CeilingHeight = 2;   // tiles

        // Quanto a base do armário fica à frente do pé da parede (profundidade do móvel na vista 3/4).
        private const float LockerDepth = 0.15f;

        public static Tilemap CreateCeiling(Transform grid)
        {
            var go = new GameObject(CeilingName);
            go.transform.SetParent(grid, false);
            var map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = CeilingOrder;
            // Só visual: fora do NavMesh (o CollectObjects.All o pegaria como andável).
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            return map;
        }

        public static Vector3Int CeilingCell(Vector3Int floorCell) => floorCell + new Vector3Int(0, CeilingHeight, 0);

        // Armário na primeira fileira de chão abaixo da parede: o pé da parede é a borda de cima da célula.
        public static void StandLocker(Locker locker, Sprite square)
        {
            var so = new SerializedObject(locker);
            var body = so.FindProperty("body").objectReferenceValue as SpriteRenderer;
            if (body == null || body.gameObject == locker.gameObject) return;
            var root = locker.transform;
            float foot = Mathf.Floor(root.position.y) + 1f;
            float baseY = foot - LockerDepth;
            body.transform.position = new Vector3(root.position.x, baseY, 0f);

            var shadow = root.Find("ContactShadow");
            if (shadow == null)
            {
                shadow = new GameObject("ContactShadow").transform;
                shadow.SetParent(root, false);
                shadow.gameObject.AddComponent<SpriteRenderer>();
            }
            var sr = shadow.GetComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0f, 0f, 0f, 0.45f);
            sr.sortingOrder = -6;   // acima do chão e dos decals, abaixo do armário (-4)
            shadow.position = new Vector3(root.position.x, baseY - 0.05f, 0f);
            shadow.localScale = new Vector3(1.05f, 0.18f, 1f);
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

        [MenuItem("Sombra da Sanidade/Arte/Corrigir perspectiva (lâmpadas e armários)", priority = 44)]
        public static void MigrateAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var square = PrototypeSceneBuilder.GetOrCreatePlaceholderSprite();
            foreach (var path in Scenes) Migrate(path, square);
        }

        private static void Migrate(string path, Sprite square)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var grid = GameObject.Find("Grid");
            var decalsGo = GameObject.Find("Grid/Decals");
            if (grid == null || decalsGo == null || grid.transform.Find(CeilingName) != null)
            {
                Debug.Log($"[Perspectiva] {path}: nada a corrigir.");
                return;
            }

            var decals = decalsGo.GetComponent<Tilemap>();
            var ceiling = CreateCeiling(grid.transform);
            decals.CompressBounds();
            int lamps = 0;
            foreach (var cell in decals.cellBounds.allPositionsWithin)
            {
                var tile = decals.GetTile(cell);
                if (tile == null || tile.name != "ceiling_lamp") continue;
                decals.SetTile(cell, null);
                ceiling.SetTile(CeilingCell(cell), tile);
                lamps++;
            }

            int lockers = 0;
            foreach (var locker in Object.FindObjectsByType<Locker>(FindObjectsInactive.Include))
            {
                StandLocker(locker, square);
                lockers++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Perspectiva] {path}: {lamps} lâmpadas no teto, {lockers} armários encostados.");
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.PerspectiveFix.MigrateBatch
        public static void MigrateBatch()
        {
            MigrateAll();
            EditorApplication.Exit(0);
        }
    }
}
