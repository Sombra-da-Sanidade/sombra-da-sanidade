using System.Collections.Generic;
using System.IO;
using System.Linq;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // Gera cenas em blocos cinzas com o mapa base da mini demo (loops, 3 servidores de HD,
    // 2 disjuntores, eclusa, monstro placeholder). Usado para Sandbox pessoal, protótipo e Demo.
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ConfigDir = Root + "/ScriptableObjects/Config";
        private const string PlaceholderSpritePath = Root + "/Art/Placeholder/square.png";
        private const string ThomasSheetPath = Root + "/Art/Sprites/thomas.png";
        public const string PrototypeScenePath = Root + "/Scenes/Sandbox/Prototipo.unity";
        public const string DemoScenePath = Root + "/Scenes/Main/Demo.unity";
        internal const string WallLayerName = "Walls";
        private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private static readonly Color WallColor = new Color(0.45f, 0.45f, 0.48f);

        [MenuItem("Sombra da Sanidade/Gerar cena de protótipo", priority = 0)]
        public static void BuildPrototype() => BuildScene(PrototypeScenePath, overwrite: true);

        [MenuItem("Sombra da Sanidade/Gerar cena Demo (base)", priority = 1)]
        public static void BuildDemo() => BuildScene(DemoScenePath, overwrite: false);

        // Uma Sandbox por frente de trabalho (docs/equipe.md).
        [MenuItem("Sombra da Sanidade/Criar Sandbox/Frente 1 - Lanterna e EMP", priority = 20)]
        public static void SandboxFrente1() => BuildSandbox("Frente1");

        [MenuItem("Sombra da Sanidade/Criar Sandbox/Frente 2 - Monstro", priority = 21)]
        public static void SandboxFrente2() => BuildSandbox("Frente2");

        [MenuItem("Sombra da Sanidade/Criar Sandbox/Frente 3 - Sanidade e HUD", priority = 22)]
        public static void SandboxFrente3() => BuildSandbox("Frente3");

        [MenuItem("Sombra da Sanidade/Criar Sandbox/Frente 4 - Disjuntor e mapa", priority = 23)]
        public static void SandboxFrente4() => BuildSandbox("Frente4");

        [MenuItem("Sombra da Sanidade/Criar Sandbox/Frente 5 - Partida, HDs e build", priority = 24)]
        public static void SandboxFrente5() => BuildSandbox("Frente5");

        // Mantido para o batchmode do PR anterior.
        public static void Build() => BuildPrototype();

        private static void BuildSandbox(string member)
        {
            BuildScene($"{Root}/Scenes/Sandbox/{member}.unity", overwrite: false);
        }

        private static void BuildScene(string path, bool overwrite)
        {
            if (!overwrite && File.Exists(path))
            {
                bool replace = Application.isBatchMode || EditorUtility.DisplayDialog(
                    "Cena já existe",
                    $"{path} já existe. Substituir? Tudo que foi feito nela será perdido.",
                    "Substituir", "Cancelar");
                if (!replace)
                {
                    EditorSceneManager.OpenScene(path);
                    return;
                }
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Garante que os assets existem antes de trocar de cena.
            GetOrCreateConfig<PlayerConfig>("PlayerConfig");
            GetOrCreateConfig<FlashlightConfig>("FlashlightConfig");
            GetOrCreateConfig<SanityConfig>("SanityConfig");
            GetOrCreateConfig<MonsterConfig>("MonsterConfig");
            GetOrCreateConfig<InteractionConfig>("InteractionConfig");
            GetOrCreatePlaceholderSprite();
            DriveServerPrefabBuilder.GetOrBuild();
            int wallLayer = EnsureLayer(WallLayerName);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Carrega de novo depois do NewScene: ele descarrega assets sem uso e as referências
            // obtidas antes viram nulas na cena salva.
            var playerConfig = GetOrCreateConfig<PlayerConfig>("PlayerConfig");
            var flashlightConfig = GetOrCreateConfig<FlashlightConfig>("FlashlightConfig");
            var sanityConfig = GetOrCreateConfig<SanityConfig>("SanityConfig");
            var square = GetOrCreatePlaceholderSprite();

            new GameObject("GameManager").AddComponent<GameManager>();
            CreateGlobalLight();
            CreateMap(square, wallLayer);
            CreateNavMesh();
            var player = CreatePlayer(square, playerConfig, flashlightConfig, sanityConfig, wallLayer, new Vector2(0f, -11f));
            CreateCamera(player.transform);
            CreateHud(player.GetComponent<FlashlightController>());
            CreateSanityEffects(player.GetComponent<SanityController>(), square, wallLayer);
            CreateObjectives(square);
            CreateSanityItems(square);
            CreateMonster(new Vector2(16f, 10f));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
            AddSceneToBuild(path);
            Debug.Log($"Cena gerada em {path}");
        }

        // ---------- Mapa ----------

        // Área 40x28 com blocos internos que formam corredores em loop (GDD 4.1: sem becos cegos).
        private static void CreateMap(Sprite square, int wallLayer)
        {
            var floor = new GameObject("Floor");
            floor.transform.localScale = new Vector3(40f, 28f, 1f);
            var floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = square;
            floorSr.color = new Color(0.2f, 0.21f, 0.22f);
            floorSr.sortingOrder = -10;
            floor.AddComponent<NavMeshModifier>(); // andável no NavMesh do monstro

            var walls = new GameObject("Walls").transform;
            // Bordas
            CreateWall(walls, square, wallLayer, new Vector2(0f, 14f), new Vector2(41f, 1f));
            CreateWall(walls, square, wallLayer, new Vector2(0f, -14f), new Vector2(41f, 1f));
            CreateWall(walls, square, wallLayer, new Vector2(-20f, 0f), new Vector2(1f, 28f));
            CreateWall(walls, square, wallLayer, new Vector2(20f, 0f), new Vector2(1f, 28f));
            // Bloco central (loop principal ao redor)
            CreateWall(walls, square, wallLayer, new Vector2(0f, 1f), new Vector2(10f, 6f));
            // Alas laterais
            CreateWall(walls, square, wallLayer, new Vector2(-12f, 6f), new Vector2(6f, 5f));
            CreateWall(walls, square, wallLayer, new Vector2(-12f, -6f), new Vector2(6f, 5f));
            CreateWall(walls, square, wallLayer, new Vector2(12f, 6f), new Vector2(6f, 5f));
            CreateWall(walls, square, wallLayer, new Vector2(12f, -6f), new Vector2(6f, 5f));
            // Divisórias do saguão (perto da eclusa)
            CreateWall(walls, square, wallLayer, new Vector2(-6f, -10f), new Vector2(1f, 4f));
            CreateWall(walls, square, wallLayer, new Vector2(6f, -10f), new Vector2(1f, 4f));
            // Pilares para quebrar linha de visão
            CreateWall(walls, square, wallLayer, new Vector2(-5f, 10f), new Vector2(1.5f, 1.5f));
            CreateWall(walls, square, wallLayer, new Vector2(5f, 10f), new Vector2(1.5f, 1.5f));
            CreateWall(walls, square, wallLayer, new Vector2(-16f, 0f), new Vector2(1.5f, 1.5f));
            CreateWall(walls, square, wallLayer, new Vector2(16f, 0f), new Vector2(1.5f, 1.5f));
        }

        private static void CreateObjectives(Sprite square)
        {
            var parent = new GameObject("Objectives").transform;

            CreateDriveServer(parent, square, "DriveServer_Medica", new Vector2(-17.5f, 11.5f));
            CreateDriveServer(parent, square, "DriveServer_Energia", new Vector2(17.5f, 11.5f));
            CreateDriveServer(parent, square, "DriveServer_Executiva", new Vector2(0f, 12.5f));

            CreateBreaker(parent, square, "Breaker_Oeste", new Vector2(-7.5f, 1f));
            CreateBreaker(parent, square, "Breaker_Leste", new Vector2(7.5f, 1f));

            var door = CreateBlock(parent, square, "ExitDoor", new Vector2(0f, -13.2f), new Vector2(3f, 0.6f), new Color(0.7f, 0.15f, 0.15f));
            var col = door.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);
            var exit = door.AddComponent<ExitDoor>();
            SetRef(exit, "doorRenderer", door.GetComponent<SpriteRenderer>());
        }

        // Frente 5: instancia o prefab do servidor (Prefabs/DriveServer.prefab). Se não existir, é gerado agora.
        internal static void CreateDriveServer(Transform parent, Sprite square, string name, Vector2 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(DriveServerPrefabBuilder.GetOrBuild(), parent);
            go.name = name;
            go.transform.position = position;
        }

        internal static void CreateBreaker(Transform parent, Sprite square, string name, Vector2 position)
        {
            var go = CreateBlock(parent, square, name, position, new Vector2(0.8f, 1f), new Color(0.75f, 0.6f, 0.15f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.4f);
            go.AddComponent<Breaker>();
        }

        // Frente 2: instancia o prefab do monstro (Prefabs/Monster.prefab) e os pontos de patrulha
        // nos cruzamentos dos corredores em loop. Se o prefab não existir, é gerado agora.
        private static void CreateMonster(Vector2 position)
        {
            Vector2[] spots =
            {
                new Vector2(-17.5f, 11f), new Vector2(0f, 10.5f), new Vector2(17.5f, 11f),
                new Vector2(-7f, 10f), new Vector2(7f, 10f),
                new Vector2(-18f, 0f), new Vector2(-7f, -1f), new Vector2(7f, -1f), new Vector2(18f, 0f),
                new Vector2(-17.5f, -11f), new Vector2(-10f, -11f), new Vector2(10f, -11f), new Vector2(17.5f, -11f),
            };
            CreateMonster(position, spots);
        }

        internal static void CreateMonster(Vector2 position, IReadOnlyList<Vector2> spots)
        {
            var prefab = MonsterPrefabBuilder.GetOrBuild();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;

            var patrol = new GameObject("MonsterPatrol").transform;
            var points = new List<Transform>();
            for (int i = 0; i < spots.Count; i++)
            {
                var point = new GameObject($"Point_{i:00}").transform;
                point.SetParent(patrol, false);
                point.position = spots[i];
                points.Add(point);
            }

            var so = new SerializedObject(go.GetComponent<MonsterController>());
            var list = so.FindProperty("patrolPoints");
            list.arraySize = points.Count;
            for (int i = 0; i < points.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // NavMesh 2D (NavMeshPlus, ADR 0003). Só entram no bake objetos com NavMeshModifier:
        // o chão (andável) e as paredes (Not Walkable). O bake é feito ao carregar a cena (NavMeshRuntimeBaker).
        internal static void CreateNavMesh()
        {
            var go = new GameObject("NavMesh");
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = NavMeshPlus.Components.CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.hideEditorLogs = true;
            go.AddComponent<CollectSources2d>();
            go.AddComponent<NavMeshRuntimeBaker>();
        }

        // ---------- Player, câmera, luz, HUD ----------

        internal static void CreateGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.05f;
            light.color = new Color(0.55f, 0.6f, 0.75f);
        }

        internal static GameObject CreatePlayer(Sprite square, PlayerConfig playerConfig, FlashlightConfig flashlightConfig,
            SanityConfig sanityConfig, int wallLayer, Vector2 position)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = position;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            // Sprite do Thomas (art-src/Sprites/thomas.json). Pivô nos pés; desce um pouco para a
            // sombra desenhada ficar no centro do collider. Sem a folha, cai no quadrado placeholder.
            var thomasSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(ThomasSheetPath);
            var thomasIdle = AssetDatabase.LoadAllAssetsAtPath(ThomasSheetPath).OfType<Sprite>().FirstOrDefault(s => s.name == "idle_down_0");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            if (thomasSheet != null && thomasIdle != null)
            {
                visual.transform.localPosition = new Vector3(0f, -0.15f, 0f);
                sr.sprite = thomasIdle;
            }
            else
            {
                visual.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                sr.sprite = square;
                sr.color = new Color(0.85f, 0.8f, 0.65f);
            }

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

            // Luz fraca em volta do jogador para não ficar 100% cego com a lanterna apagada.
            var aura = new GameObject("Aura").AddComponent<Light2D>();
            aura.transform.SetParent(player.transform, false);
            aura.lightType = Light2D.LightType.Point;
            aura.color = new Color(0.6f, 0.65f, 0.8f);
            aura.intensity = 0.35f;
            aura.pointLightOuterRadius = 1.4f;

            var controller = player.AddComponent<PlayerController>();
            SetRef(controller, "config", playerConfig);
            SetRef(controller, "visual", visual.transform);
            if (thomasSheet != null && thomasIdle != null)
            {
                var controllerSo = new SerializedObject(controller);
                controllerSo.FindProperty("rotateVisual").boolValue = false;
                controllerSo.ApplyModifiedPropertiesWithoutUndo();
                var animator = player.AddComponent<PlayerSpriteAnimator>();
                SetRef(animator, "sheet", thomasSheet);
                SetRef(animator, "body", sr);
            }

            var flashlight = player.AddComponent<FlashlightController>();
            SetRef(flashlight, "config", flashlightConfig);
            SetRef(flashlight, "player", controller);
            SetRef(flashlight, "pivot", pivot.transform);
            var so = new SerializedObject(flashlight);
            so.FindProperty("obstacles").intValue = 1 << wallLayer;
            so.ApplyModifiedPropertiesWithoutUndo();

            var sanity = player.AddComponent<SanityController>();
            SetRef(sanity, "config", sanityConfig);

            player.AddComponent<PlayerInteractor>();

            return player;
        }

        internal static void CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(target.position.x, target.position.y, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            var camData = go.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true; // vinheta da sanidade
            go.AddComponent<AudioListener>();

            var follow = go.AddComponent<CameraFollow>();
            follow.Target = target;
        }

        internal static void CreateHud(FlashlightController flashlight)
        {
            var hud = new GameObject("DemoHud").AddComponent<DemoHud>();
            SetRef(hud, "flashlight", flashlight);

            SetRef(hud, "sanity", flashlight.GetComponent<SanityController>());
        }

        // ---------- Sanidade ----------

        // Vinheta + aberração, batimento + tremor e alucinações (GDD 4.6, 7 e 9).
        internal static void CreateSanityEffects(SanityController sanity, Sprite square, int wallLayer)
        {
            var vignette = new GameObject("SanityVignette").AddComponent<SanityVignette>();
            SetRef(vignette, "sanity", sanity);

            var heartbeat = new GameObject("SanityHeartbeat").AddComponent<SanityHeartbeat>();
            SetRef(heartbeat, "sanity", sanity);

            var hallucination = new GameObject("SanityHallucination").AddComponent<SanityHallucination>();
            // Escala 1: em jogo o sprite vira um quadro da folha do monstro (SanityHallucination.monsterArt), no mesmo tamanho.
            var phantom = CreateBlock(hallucination.transform, square, "Phantom", Vector2.zero, Vector2.one, new Color(0.3f, 0.1f, 0.4f));
            var phantomSr = phantom.GetComponent<SpriteRenderer>();
            phantomSr.sortingOrder = 9;
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
            if (unlit != null) phantomSr.sharedMaterial = unlit;
            else Debug.LogWarning($"Material {UnlitSpriteMaterialPath} não encontrado: a alucinação só aparece sob luz.");
            SetRef(hallucination, "sanity", sanity);
            SetRef(hallucination, "phantom", phantomSr);
            var so = new SerializedObject(hallucination);
            so.FindProperty("obstacles").intValue = 1 << wallLayer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Estabilizadores e pistas em fósforo. Textos provisórios até a Frente 4 ter os puzzles.
        private static void CreateSanityItems(Sprite square)
        {
            var parent = new GameObject("SanityItems").transform;

            CreateStabilizer(parent, square, "Stabilizer_Sudoeste", new Vector2(-17f, -11f));
            CreateStabilizer(parent, square, "Stabilizer_Leste", new Vector2(17f, -3f));

            CreateClue(parent, "Clue_FusivelOeste", new Vector2(-7.5f, 2.2f), "FUSÍVEL 3-7-1", 0.06f);
            CreateClue(parent, "Clue_FusivelLeste", new Vector2(7.5f, 2.2f), "FUSÍVEL 9-0-2", 0.06f);
            CreateClue(parent, "Clue_Aviso", new Vector2(0f, -5.5f), "ELE SEGUE A LUZ", 0.1f);
        }

        internal static void CreateStabilizer(Transform parent, Sprite square, string name, Vector2 position)
        {
            var go = CreateBlock(parent, square, name, position, new Vector2(0.4f, 0.6f), new Color(0.55f, 0.8f, 0.95f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.5f, 2f); // compensa a escala pequena do bloco

            var glow = new GameObject("Glow").AddComponent<Light2D>();
            glow.transform.SetParent(go.transform, false);
            glow.lightType = Light2D.LightType.Point;
            glow.color = new Color(0.55f, 0.8f, 0.95f);
            glow.intensity = 0.5f;
            glow.pointLightOuterRadius = 1.2f;

            go.AddComponent<LithiumStabilizer>();
        }

        // TextMesh usa material próprio (sem luz 2D): brilha no escuro como tinta de fósforo.
        internal static void CreateClue(Transform parent, string name, Vector2 position, string text, float characterSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = 48;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.45f, 1f, 0.55f, 0.85f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 8;

            go.AddComponent<SanityClue>();
        }

        // ---------- Helpers ----------

        internal static GameObject CreateBlock(Transform parent, Sprite square, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = 5;
            return go;
        }

        private static void CreateWall(Transform parent, Sprite square, int layer, Vector2 position, Vector2 size)
        {
            var go = CreateBlock(parent, square, "Wall", position, size, WallColor);
            go.layer = layer;
            go.GetComponent<SpriteRenderer>().sortingOrder = 0;
            go.AddComponent<BoxCollider2D>();
            var navBlock = go.AddComponent<NavMeshModifier>();
            navBlock.overrideArea = true;
            navBlock.area = 1; // Not Walkable
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

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static T GetOrCreateConfig<T>(string name) where T : ScriptableObject
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

        internal static Sprite GetOrCreatePlaceholderSprite()
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

        internal static int EnsureLayer(string name)
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

        internal static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool isStart = path == AlfaSceneBuilder.ScenePath;
            int index = scenes.FindIndex(s => s.path == path);
            if (index >= 0 && (!isStart || index == 0)) return;
            if (index >= 0) scenes.RemoveAt(index);

            // A Alfa sempre fica no índice 0, para ser a cena inicial da build.
            var entry = new EditorBuildSettingsScene(path, true);
            if (isStart) scenes.Insert(0, entry);
            else scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
