using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Build WebGL em Builds/WebGL (pasta ignorada pelo git): cenas habilitadas do Build Settings.
    // Também roda em batchmode: -executeMethod SombraDaSanidade.EditorTools.WebGLBuilder.Build
    public static class WebGLBuilder
    {
        private const string OutputDir = "Builds/WebGL";

        [MenuItem("Sombra da Sanidade/Build WebGL", priority = 40)]
        public static void Build()
        {
            // Cenas habilitadas no Build Settings, na ordem: Menu primeiro (#32), depois as 4 alas do encadeamento
            // (WingFlow) e as cenas de reserva. O GameManager só carrega o que estiver no build.
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Nenhuma cena habilitada no Build Settings.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // Gzip com fallback: funciona em hosts que não mandam o header Content-Encoding (itch.io, Unity Play).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build WebGL ok: {OutputDir} ({summary.totalSize / (1024 * 1024)} MB, cenas {string.Join(", ", scenes)})");
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
