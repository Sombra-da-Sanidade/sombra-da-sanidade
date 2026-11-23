using System.Linq;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // RC1, issue #79. Gera Prefabs/SecurityCamera.prefab e o config, e ajuda as alas (RC2) a posicioná-la.
    // Menus: Sombra da Sanidade > Gerar prefab da Câmera de segurança
    //        Sombra da Sanidade > Colocar Câmera de segurança na cena (no centro da Scene view, olhando para baixo)
    // Para os builders das alas: SecurityCameraPrefabBuilder.Place(pai, posição, ânguloEmGraus, disjuntor).
    public static class SecurityCameraPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/SecurityCamera.prefab";
        public const string ConfigPath = "Assets/_Project/ScriptableObjects/Config/SecurityCamera.asset";
        public const string ArtPath = "Assets/_Project/Art/Props/security_camera.png";
        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        [MenuItem("Sombra da Sanidade/Gerar prefab da Câmera de segurança", priority = 43)]
        public static void BuildFromMenu()
        {
            Selection.activeObject = Build();
            Debug.Log($"Prefab da câmera de segurança gerado em {PrefabPath}");
        }

        [MenuItem("Sombra da Sanidade/Colocar Câmera de segurança na cena", priority = 44)]
        public static void PlaceInScene()
        {
            var view = SceneView.lastActiveSceneView;
            Vector3 at = view != null ? view.pivot : Vector3.zero;
            var go = Place(null, at, -90f);
            Undo.RegisterCreatedObjectUndo(go, "Colocar câmera de segurança");
            Selection.activeObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        // facingDegrees: 0 olha para a direita, 90 para cima, -90 para baixo. shutdown: disjuntor que a desliga de vez
        // (vazio: o mais perto dentro do setor, resolvido em jogo).
        public static GameObject Place(Transform parent, Vector2 position, float facingDegrees, Breaker shutdown = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(GetOrBuild());
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var cam = go.GetComponent<SecurityCamera>();
            var so = new SerializedObject(cam);
            var head = (Transform)so.FindProperty("head").objectReferenceValue;
            if (head != null) head.rotation = Quaternion.Euler(0f, 0f, facingDegrees);
            MountOnWall(so, go.transform, facingDegrees);
            if (shutdown != null) so.FindProperty("shutdownBreaker").objectReferenceValue = shutdown;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // O desenho vai para a parede de onde a câmera olha: olhando para baixo, pendurada na faixa de cima da
        // parede norte (de frente); para os lados, de perfil na parede lateral (a da direita espelhada).
        // position (raiz) fica 0,6 tile à frente da célula da parede, que é de onde sai o cone.
        private static void MountOnWall(SerializedObject so, Transform root, float facingDegrees)
        {
            var art = (SpriteRenderer)so.FindProperty("art").objectReferenceValue;
            if (art == null) return;
            var dir = Quaternion.Euler(0f, 0f, facingDegrees) * Vector3.right;
            bool sideways = Mathf.Abs(dir.x) > 0.7f;
            if (sideways)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().ToArray();
                Sprite S(string n) => sprites.FirstOrDefault(s => s.name == "security_camera_side_" + n);
                if (S("idle") != null)
                {
                    so.FindProperty("idleSprite").objectReferenceValue = S("idle");
                    so.FindProperty("detectSprite").objectReferenceValue = S("detect");
                    so.FindProperty("alarmSprite").objectReferenceValue = S("alarm");
                    so.FindProperty("offSprite").objectReferenceValue = S("off");
                    so.ApplyModifiedPropertiesWithoutUndo();
                    art.sprite = S("idle");
                }
                art.flipX = dir.x < 0f;
                art.transform.position = root.position + new Vector3(dir.x > 0f ? 0.4f : -0.4f, -0.3f, 0f);
            }
            else if (dir.y < 0f)
                art.transform.position = root.position + new Vector3(0f, 1.1f, 0f);   // parede norte
            else
                art.transform.position = root.position + new Vector3(0f, -0.6f, 0f);  // parede sul
            var led = art.transform.Find("Led");
            if (led != null && art.flipX) led.localPosition = new Vector3(-led.localPosition.x, led.localPosition.y, 0f);

            // A luz sai da lente do desenho. A detecção continua na raiz (SecurityCamera usa transform.position),
            // então só o cone sobe; o alcance cresce na mesma medida para a luz chegar até onde ela detecta.
            var head = (Transform)so.FindProperty("head").objectReferenceValue;
            var cone = (UnityEngine.Rendering.Universal.Light2D)so.FindProperty("cone").objectReferenceValue;
            if (head == null) return;
            Vector3 lens = sideways
                ? art.transform.position + new Vector3(art.flipX ? -0.375f : 0.375f, 0.53f, 0f)
                : art.transform.position + new Vector3(0f, dir.y < 0f ? 0.3f : 0.5f, 0f);
            float lift = Vector2.Distance(lens, root.position);
            head.position = lens;
            if (cone != null) cone.pointLightOuterRadius += lift;
            so.FindProperty("coneExtraRange").floatValue = lift;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject GetOrBuild()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return existing != null ? existing : Build();
        }

        public static SecurityCameraConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<SecurityCameraConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<SecurityCameraConfig>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ConfigPath));
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static GameObject Build()
        {
            MonsterPrefabBuilder.EnsureArtImport(ArtPath);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().ToArray();
            Sprite S(string n) => sprites.FirstOrDefault(s => s.name == "security_camera_" + n);
            var config = GetOrCreateConfig();

            var root = new GameObject("SecurityCamera");
            try
            {
                var head = new GameObject("Head").transform;
                head.SetParent(root.transform, false);

                // O desenho fica preso na parede (não gira com a cabeça): Place o encosta na parede certa.
                var artGo = new GameObject("Art");
                artGo.transform.SetParent(root.transform, false);
                var art = artGo.AddComponent<SpriteRenderer>();
                art.sprite = S("idle");
                art.sortingOrder = 8;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
                if (mat != null) art.sharedMaterial = mat;

                // O cone do Light2D abre em volta do eixo Y local; girado -90° para seguir o X da cabeça.
                var coneGo = new GameObject("Cone");
                coneGo.transform.SetParent(head, false);
                coneGo.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                var cone = coneGo.AddComponent<Light2D>();
                cone.lightType = Light2D.LightType.Point;
                cone.pointLightInnerRadius = 0.2f;
                cone.pointLightOuterRadius = config.range;
                cone.pointLightOuterAngle = config.coneAngle;
                cone.pointLightInnerAngle = config.coneAngle * 0.6f;
                cone.intensity = config.coneIntensity;
                cone.color = config.coneIdleColor;
                cone.falloffIntensity = 0.7f;

                var ledGo = new GameObject("Led");
                ledGo.transform.SetParent(artGo.transform, false);
                ledGo.transform.localPosition = new Vector3(0.2f, 0.45f, 0f);
                var led = ledGo.AddComponent<Light2D>();
                led.lightType = Light2D.LightType.Point;
                led.pointLightOuterRadius = 0.6f;
                led.pointLightInnerRadius = 0f;
                led.intensity = 1f;

                var cam = root.AddComponent<SecurityCamera>();
                var so = new SerializedObject(cam);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("head").objectReferenceValue = head;
                so.FindProperty("art").objectReferenceValue = art;
                so.FindProperty("idleSprite").objectReferenceValue = S("idle");
                so.FindProperty("detectSprite").objectReferenceValue = S("detect");
                so.FindProperty("alarmSprite").objectReferenceValue = S("alarm");
                so.FindProperty("offSprite").objectReferenceValue = S("off");
                so.FindProperty("cone").objectReferenceValue = cone;
                so.FindProperty("led").objectReferenceValue = led;
                so.FindProperty("obstacles").intValue = LayerMask.GetMask("Walls");
                so.ApplyModifiedPropertiesWithoutUndo();

                if (S("idle") == null)
                    Debug.LogWarning($"Sprites da câmera não encontrados em {ArtPath}. Rode python3 tools/art/export_sprites.py.");

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
