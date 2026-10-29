using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Build WebGL da mini demo em Builds/WebGL (pasta ignorada pelo git).
    // Também roda em batchmode: -executeMethod SombraDaSanidade.EditorTools.WebGLBuilder.Build
    public static class WebGLBuilder
    {
        private const string OutputDir = "Builds/WebGL";

        [MenuItem("Sombra da Sanidade/Build WebGL", priority = 40)]
        public static void Build()
        {
            // A Alfa (mapa com tiles) é a cena da mini demo; Demo e Protótipo ficam de reserva.
            string scene = new[]
            {
                AlfaSceneBuilder.ScenePath,
                PrototypeSceneBuilder.DemoScenePath,
                PrototypeSceneBuilder.PrototypeScenePath
            }.FirstOrDefault(File.Exists);

            if (scene == null)
            {
                Debug.LogError("Nenhuma cena para build. Use Sombra da Sanidade > Gerar cena Alfa (tiles).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // Gzip com fallback: funciona em hosts que não mandam o header Content-Encoding (itch.io, Unity Play).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build WebGL ok: {OutputDir} ({summary.totalSize / (1024 * 1024)} MB, cena {scene})");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(OutputDir);
            }
            else
            {
                Debug.LogError($"Build WebGL falhou: {summary.result}, {summary.totalErrors} erro(s)");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
