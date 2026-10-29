using SombraDaSanidade.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5. Gera Prefabs/DriveServer.prefab (issue #11): bloco do servidor, LED, brilho verde
    // e trigger de interação, ligado ao InteractionConfig. As cenas só posicionam o prefab.
    // Menu: Sombra da Sanidade > Gerar prefab do Servidor de HD.
    public static class DriveServerPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/DriveServer.prefab";

        [MenuItem("Sombra da Sanidade/Gerar prefab do Servidor de HD", priority = 42)]
        public static void BuildFromMenu()
        {
            Selection.activeObject = Build();
            Debug.Log($"Prefab do servidor de HD gerado em {PrefabPath}");
        }

        public static GameObject GetOrBuild()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return existing != null ? existing : Build();
        }

        public static GameObject Build()
        {
            var square = PrototypeSceneBuilder.GetOrCreatePlaceholderSprite();
            var config = PrototypeSceneBuilder.GetOrCreateConfig<InteractionConfig>("InteractionConfig");

            var root = PrototypeSceneBuilder.CreateBlock(null, square, "DriveServer", Vector2.zero,
                new Vector2(1.2f, 1.2f), new Color(0.15f, 0.25f, 0.2f));
            try
            {
                var col = root.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.5f, 1.5f);

                var led = PrototypeSceneBuilder.CreateBlock(root.transform, square, "Indicator", Vector2.zero,
                    Vector2.one * 0.3f, Color.green);
                led.transform.localPosition = Vector3.zero;
                led.GetComponent<SpriteRenderer>().sortingOrder = 6;

                var glow = new GameObject("Glow").AddComponent<Light2D>();
                glow.transform.SetParent(root.transform, false);
                glow.lightType = Light2D.LightType.Point;
                glow.color = new Color(0.3f, 1f, 0.45f);
                glow.intensity = 0.6f;
                glow.pointLightOuterRadius = 1.6f;

                var server = root.AddComponent<DriveServer>();
                PrototypeSceneBuilder.SetRef(server, "config", config);
                PrototypeSceneBuilder.SetRef(server, "indicator", led.GetComponent<SpriteRenderer>());

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
