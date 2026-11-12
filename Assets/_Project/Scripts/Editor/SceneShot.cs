using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace SombraDaSanidade.EditorTools
{
    // Prints de conferência das cenas (perspectiva de props, prints de PR): abre cada cena, centraliza uma câmera
    // num armário e numa lâmpada de teto, acende uma luz global de teste e salva PNGs.
    // Precisa de GPU: no batchmode, rodar SEM -nographics. Pasta de saída: variável SHOT_DIR (padrão Temp/Shots),
    // prefixo: SHOT_TAG (ex.: "antes", "depois").
    public static class SceneShot
    {
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Wings/AlaMedica.unity",
            "Assets/_Project/Scenes/Main/Beta.unity",
        };

        [MenuItem("Sombra da Sanidade/Arte/Prints de conferência (armário e lâmpada)", priority = 45)]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string dir = System.Environment.GetEnvironmentVariable("SHOT_DIR");
            if (string.IsNullOrEmpty(dir)) dir = "Temp/Shots";
            string tag = System.Environment.GetEnvironmentVariable("SHOT_TAG") ?? "shot";
            Directory.CreateDirectory(dir);

            foreach (var path in Scenes)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                string scene = Path.GetFileNameWithoutExtension(path);
                var light = new GameObject("ShotLight").AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.intensity = 0.9f;

                var locker = Object.FindAnyObjectByType<Interaction.Locker>(FindObjectsInactive.Include);
                if (locker != null) Shot(locker.transform.position, $"{dir}/{tag}-{scene}-armario.png");
                if (FindLamp(out var lamp)) Shot(lamp, $"{dir}/{tag}-{scene}-lampada.png");
            }
        }

        private static bool FindLamp(out Vector3 position)
        {
            foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
            {
                map.CompressBounds();
                foreach (var cell in map.cellBounds.allPositionsWithin)
                {
                    var tile = map.GetTile(cell);
                    if (tile == null || tile.name != "ceiling_lamp") continue;
                    position = map.GetCellCenterWorld(cell);
                    return true;
                }
            }
            position = default;
            return false;
        }

        private static void Shot(Vector3 at, string file)
        {
            const int w = 640, h = 360;
            var go = new GameObject("ShotCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.transform.position = new Vector3(at.x, at.y, -10f);
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Debug.Log($"[Shot] {file}");
        }

        // Batchmode (sem -nographics): -executeMethod SombraDaSanidade.EditorTools.SceneShot.RunBatch
        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }
    }
}
