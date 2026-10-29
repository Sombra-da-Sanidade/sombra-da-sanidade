using SombraDaSanidade.Interaction;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 4. Gera Prefabs/Breaker.prefab (issue #10): painel com trigger de interação.
    // A Zona Segura, o LED e as faíscas são criados pelo próprio Breaker no Awake, então o prefab
    // só precisa ser posicionado na frente do tile do disjuntor.
    // Menu: Sombra da Sanidade > Gerar prefab do Disjuntor.
    public static class BreakerPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Breaker.prefab";

        [MenuItem("Sombra da Sanidade/Gerar prefab do Disjuntor", priority = 41)]
        public static void BuildFromMenu()
        {
            Selection.activeObject = Build();
            Debug.Log($"Prefab do disjuntor gerado em {PrefabPath}");
        }

        public static GameObject Build()
        {
            var root = new GameObject("Breaker");
            try
            {
                var col = root.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.3f, 1.4f);   // alcance do E em frente ao painel
                root.AddComponent<Breaker>();
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
