using System.Collections.Generic;
using System.Linq;
using SombraDaSanidade.Core;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Ordem do Build Profile para a partida encadeada (#93): Menu, as 4 fases do WingFlow e depois o resto
    // (Alfa, Beta, protótipo, sandboxes), que continua carregável pelos testes.
    // Menu: Sombra da Sanidade > Ordenar Build Profile das fases.
    public static class WingFlowBuildProfile
    {
        private const string WingsFolder = "Assets/_Project/Scenes/Wings/";

        public static string[] OrderedPaths()
        {
            var paths = new List<string> { MenuSceneBuilder.ScenePath };
            for (int i = 0; i < WingFlow.Count; i++) paths.Add(WingsFolder + WingFlow.SceneAt(i) + ".unity");
            return paths.ToArray();
        }

        [MenuItem("Sombra da Sanidade/Ordenar Build Profile das fases", priority = 21)]
        public static void Apply()
        {
            var ordered = OrderedPaths();
            var current = EditorBuildSettings.scenes.ToList();
            var result = new List<EditorBuildSettingsScene>();
            foreach (var path in ordered)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogError($"[Build Profile] Cena não encontrada: {path}");
                    continue;
                }
                result.Add(new EditorBuildSettingsScene(path, true));
            }
            result.AddRange(current.Where(s => !ordered.Contains(s.path)));
            EditorBuildSettings.scenes = result.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Build Profile] Ordem: " + string.Join(", ", result.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))));
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.WingFlowBuildProfile.ApplyBatch -quit
        public static void ApplyBatch() => Apply();
    }
}
