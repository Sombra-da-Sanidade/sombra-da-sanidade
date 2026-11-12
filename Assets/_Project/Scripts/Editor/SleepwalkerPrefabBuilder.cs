using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2 (Monstro). Gera Prefabs/Sleepwalker.prefab (o Sonâmbulo) a partir do código e ajuda a colocá-lo no mapa.
    // Menus: Sombra da Sanidade > Gerar prefab do Sonâmbulo
    //        Sombra da Sanidade > Colocar Sonâmbulo na cena (no centro da Scene view)
    public static class SleepwalkerPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Sleepwalker.prefab";
        public const string ConfigPath = "Assets/_Project/ScriptableObjects/Config/SleepwalkerConfig.asset";
        private const string SheetPath = MonsterPrefabBuilder.ArtDir + "/Sonambulo_sheet.png";
        private const string GlowPath = MonsterPrefabBuilder.ArtDir + "/Sonambulo_sheet_glow.png";
        private const string KillPath = MonsterPrefabBuilder.ArtDir + "/Sonambulo_kills.png";
        private const string KillGlowPath = MonsterPrefabBuilder.ArtDir + "/Sonambulo_kills_glow.png";
        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        // Visor acima dos pés (sprite de 64 px a 16 px/unidade, cabeça pendurada na altura do peito).
        private const float VisorHeight = 2.6f;

        [MenuItem("Sombra da Sanidade/Gerar prefab do Sonâmbulo", priority = 41)]
        public static void BuildFromMenu()
        {
            Selection.activeObject = Build();
            Debug.Log($"Prefab do Sonâmbulo gerado em {PrefabPath}");
        }

        [MenuItem("Sombra da Sanidade/Colocar Sonâmbulo na cena", priority = 42)]
        public static void PlaceInScene()
        {
            var prefab = GetOrBuild();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var view = SceneView.lastActiveSceneView;
            Vector3 at = view != null ? view.pivot : Vector3.zero;
            go.transform.position = new Vector3(at.x, at.y, 0f);
            Undo.RegisterCreatedObjectUndo(go, "Colocar Sonâmbulo");
            Selection.activeObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        public static GameObject GetOrBuild()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return existing != null ? existing : Build();
        }

        public static SleepwalkerConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<SleepwalkerConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<SleepwalkerConfig>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ConfigPath));
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static GameObject Build()
        {
            MonsterPrefabBuilder.EnsureArtImport(SheetPath);
            MonsterPrefabBuilder.EnsureArtImport(GlowPath);
            MonsterPrefabBuilder.EnsureArtImport(KillPath);
            MonsterPrefabBuilder.EnsureArtImport(KillGlowPath);
            var config = GetOrCreateConfig();

            var root = new GameObject("Sleepwalker");
            try
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                var col = root.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.4f;
                agent.height = 1f;
                agent.baseOffset = 0f;
                agent.angularSpeed = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.enabled = false;

                var walker = root.AddComponent<SleepwalkerController>();
                var wso = new SerializedObject(walker);
                wso.FindProperty("config").objectReferenceValue = config;
                wso.ApplyModifiedPropertiesWithoutUndo();

                var body = CreateRenderer(root.transform, "Body", LitMaterialPath, 9);
                var glow = CreateRenderer(root.transform, "Glow", UnlitMaterialPath, 10);

                var visorGo = new GameObject("Visor");
                visorGo.transform.SetParent(root.transform, false);
                visorGo.transform.localPosition = new Vector3(0f, VisorHeight, 0f);
                var visor = visorGo.AddComponent<Light2D>();
                visor.lightType = Light2D.LightType.Point;
                visor.color = new Color(0.62f, 0.36f, 0.68f);
                visor.intensity = 0.5f;
                visor.pointLightOuterRadius = 1.2f;
                visor.pointLightInnerRadius = 0.15f;

                var anim = root.AddComponent<SleepwalkerSpriteAnimator>();
                var so = new SerializedObject(anim);
                so.FindProperty("sheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
                so.FindProperty("glowSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowPath);
                so.FindProperty("killSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(KillPath);
                so.FindProperty("killGlowSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(KillGlowPath);
                so.FindProperty("body").objectReferenceValue = body;
                so.FindProperty("glow").objectReferenceValue = glow;
                so.FindProperty("visor").objectReferenceValue = visor;
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
    }
}
