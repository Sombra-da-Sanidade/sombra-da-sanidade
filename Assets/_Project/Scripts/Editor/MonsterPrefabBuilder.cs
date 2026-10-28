using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2 (Monstro). Gera o prefab Prefabs/Monster.prefab a partir do código, para todo mundo
    // ter o mesmo monstro e o prefab poder ser refeito sem editar YAML na mão.
    // Menu: Sombra da Sanidade > Gerar prefab do Monstro. O builder de cenas chama isto se o prefab não existir.
    public static class MonsterPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Monster.prefab";
        public const string ArtDir = "Assets/_Project/Art/Monster";
        private const string SheetPath = ArtDir + "/EspecimeB_sheet.png";
        private const string GlowPath = ArtDir + "/EspecimeB_sheet_glow.png";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Config/MonsterConfig.asset";
        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // Altura dos eletrodos acima dos pés (sprite de 128 px a 32 px/unidade, cérebro perto do topo).
        private const float LedHeight = 3.1f;

        [MenuItem("Sombra da Sanidade/Gerar prefab do Monstro", priority = 40)]
        public static void BuildFromMenu()
        {
            var prefab = Build();
            Selection.activeObject = prefab;
            Debug.Log($"Prefab do monstro gerado em {PrefabPath}");
        }

        public static GameObject GetOrBuild()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return existing != null ? existing : Build();
        }

        public static GameObject Build()
        {
            EnsureArtImport(SheetPath);
            EnsureArtImport(GlowPath);
            var config = AssetDatabase.LoadAssetAtPath<MonsterConfig>(ConfigPath);
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            var glowSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowPath);

            var root = new GameObject("Monster");
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                // Contato com o jogador (trigger). Raio ~0.5 m nos pés: o corpo é largo, mas os braços não matam.
                var col = root.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.5f;

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.45f;
                agent.height = 1f;
                agent.baseOffset = 0f;
                agent.angularSpeed = 0f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.enabled = false;

                var monster = root.AddComponent<MonsterController>();
                Set(monster, "config", config);

                var body = CreateRenderer(root.transform, "Body", LitMaterialPath, 9);
                var glow = CreateRenderer(root.transform, "Glow", UnlitMaterialPath, 10);

                var ledGo = new GameObject("Led");
                ledGo.transform.SetParent(root.transform, false);
                ledGo.transform.localPosition = new Vector3(0f, LedHeight, 0f);
                var led = ledGo.AddComponent<Light2D>();
                led.lightType = Light2D.LightType.Point;
                led.color = new Color(0.17f, 0.41f, 0.57f);
                led.intensity = 0.35f;
                led.pointLightOuterRadius = 1.4f;
                led.pointLightInnerRadius = 0.2f;

                var anim = root.AddComponent<MonsterSpriteAnimator>();
                var so = new SerializedObject(anim);
                so.FindProperty("sheet").objectReferenceValue = sheet;
                so.FindProperty("glowSheet").objectReferenceValue = glowSheet;
                so.FindProperty("body").objectReferenceValue = body;
                so.FindProperty("glow").objectReferenceValue = glow;
                so.FindProperty("led").objectReferenceValue = led;
                so.ApplyModifiedPropertiesWithoutUndo();

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, string materialPath, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        // Pixel art: sem filtro, sem compressão e sem redimensionar a folha (768x1280 não é potência de 2).
        public static void EnsureArtImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Arte do monstro não encontrada em {path}.");
                return;
            }

            bool dirty = importer.textureType != TextureImporterType.Sprite
                         || importer.filterMode != FilterMode.Point
                         || importer.textureCompression != TextureImporterCompression.Uncompressed
                         || importer.mipmapEnabled
                         || importer.spritePixelsPerUnit != 32f
                         || importer.maxTextureSize < 2048;
            if (!dirty) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
