using System.IO;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // Gera a cena Sandbox/Prototipo com blocos cinzas: player, lanterna, câmera e paredes.
    // Menu: Sombra da Sanidade > Gerar cena de protótipo.
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ConfigDir = Root + "/ScriptableObjects/Config";
        private const string PlaceholderSpritePath = Root + "/Art/Placeholder/square.png";
        private const string ScenePath = Root + "/Scenes/Sandbox/Prototipo.unity";
        private const string WallLayerName = "Walls";

        [MenuItem("Sombra da Sanidade/Gerar cena de protótipo")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var playerConfig = GetOrCreateConfig<PlayerConfig>("PlayerConfig");
            var flashlightConfig = GetOrCreateConfig<FlashlightConfig>("FlashlightConfig");
            GetOrCreateConfig<SanityConfig>("SanityConfig");
            GetOrCreateConfig<MonsterConfig>("MonsterConfig");
            var square = GetOrCreatePlaceholderSprite();
            int wallLayer = EnsureLayer(WallLayerName);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGlobalLight();
            var player = CreatePlayer(square, playerConfig, flashlightConfig, wallLayer);
            CreateCamera(player.transform);
            CreateWalls(square, wallLayer);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Debug.Log($"Cena gerada em {ScenePath}");
        }

        private static void CreateGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.05f;
            light.color = new Color(0.55f, 0.6f, 0.75f);
        }

        private static GameObject CreatePlayer(Sprite square, PlayerConfig playerConfig, FlashlightConfig flashlightConfig, int wallLayer)
        {
            var player = new GameObject("Player");
            player.tag = "Player";

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.85f, 0.8f, 0.65f);
            sr.sortingOrder = 10;

            var pivot = new GameObject("FlashlightPivot");
            pivot.transform.SetParent(player.transform, false);
            var cone = pivot.AddComponent<Light2D>();
            cone.lightType = Light2D.LightType.Point;
            cone.color = new Color(1f, 0.93f, 0.78f);
            cone.intensity = flashlightConfig.intensity;
            cone.pointLightInnerRadius = 0.2f;
            cone.pointLightOuterRadius = flashlightConfig.range;
            cone.pointLightOuterAngle = flashlightConfig.coneAngle;
            cone.pointLightInnerAngle = flashlightConfig.coneAngle * 0.6f;
            EnableShadows(cone);

            var controller = player.AddComponent<PlayerController>();
            SetRef(controller, "config", playerConfig);
            SetRef(controller, "visual", visual.transform);

            var flashlight = player.AddComponent<FlashlightController>();
            SetRef(flashlight, "config", flashlightConfig);
            SetRef(flashlight, "player", controller);
            SetRef(flashlight, "pivot", pivot.transform);
            var so = new SerializedObject(flashlight);
            so.FindProperty("obstacles").intValue = 1 << wallLayer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static void CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<AudioListener>();

            var follow = go.AddComponent<CameraFollow>();
            follow.Target = target;
        }

        // Sala 24x16 com um pilar central, para testar o cone e as sombras.
        private static void CreateWalls(Sprite square, int wallLayer)
        {
            var parent = new GameObject("Walls");
            CreateWall(parent.transform, square, wallLayer, new Vector2(0f, 8f), new Vector2(24f, 1f));
            CreateWall(parent.transform, square, wallLayer, new Vector2(0f, -8f), new Vector2(24f, 1f));
            CreateWall(parent.transform, square, wallLayer, new Vector2(-12f, 0f), new Vector2(1f, 16f));
            CreateWall(parent.transform, square, wallLayer, new Vector2(12f, 0f), new Vector2(1f, 16f));
            CreateWall(parent.transform, square, wallLayer, new Vector2(4f, 2f), new Vector2(2f, 2f));
            CreateWall(parent.transform, square, wallLayer, new Vector2(-5f, -3f), new Vector2(1f, 6f));
        }

        private static void CreateWall(Transform parent, Sprite square, int layer, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Wall");
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.45f, 0.45f, 0.48f);

            go.AddComponent<BoxCollider2D>();
            var caster = go.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false;
        }

        // Liga sombras via SerializedObject para não depender da API pública do URP.
        private static void EnableShadows(Light2D light)
        {
            var so = new SerializedObject(light);
            var enabled = so.FindProperty("m_ShadowsEnabled");
            if (enabled != null) enabled.boolValue = true;
            var intensity = so.FindProperty("m_ShadowIntensity");
            if (intensity != null) intensity.floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrCreateConfig<T>(string name) where T : ScriptableObject
        {
            string path = $"{ConfigDir}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            Directory.CreateDirectory(ConfigDir);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static Sprite GetOrCreatePlaceholderSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (sprite != null) return sprite;

            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(PlaceholderSpritePath));
            File.WriteAllBytes(PlaceholderSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PlaceholderSpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 4;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        }

        private static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }

            Debug.LogWarning($"Sem slot livre para a layer {name}. Usando Default.");
            return 0;
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == path) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(path, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
