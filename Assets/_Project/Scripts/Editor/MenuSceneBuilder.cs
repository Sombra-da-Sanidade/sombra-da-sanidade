using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera Scenes/Main/Menu.unity: câmera preta e o MainMenuScreen (kit CRT de art-src/UI).
    // Não mexe no Build Profile: a ordem das cenas e a cena inicial da build WebGL são da Frente 5 (#32).
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main/Menu.unity";

        [MenuItem("Sombra da Sanidade/Criar cena de Menu", priority = 20)]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PixelHud.Vazio;
            camera.gameObject.AddComponent<AudioListener>();

            new GameObject("MainMenu").AddComponent<MainMenuScreen>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Menu] Cena criada em {ScenePath}. Adicione ao Build Profile antes da cena do jogo.");
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.MenuSceneBuilder.BuildBatch
        public static void BuildBatch()
        {
            Build();
            EditorApplication.Exit(0);
        }
    }
}
