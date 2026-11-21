using System;
using System.IO;
using SombraDaSanidade.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Build WebGL da sonda de áudio (#100, ADR 0005). Gera WAVs de teste, importa como no jogo (stems de música
    // CompressedInMemory, SFX DecompressOnLoad), monta uma cena só com a AudioWebProbe, compila para a pasta
    // pedida e apaga a cena e os WAVs. Não mexe no Build Profile do jogo.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AudioWebProbeBuilder.BuildBatch -probeOut <pasta> -quit
    public static class AudioWebProbeBuilder
    {
        private const string TempFolder = "Assets/_Project/Audio/_Probe";
        private const string ScenePath = TempFolder + "/AudioWebProbe.unity";
        private const int Rate = 44100;

        [MenuItem("Sombra da Sanidade/Áudio/Build WebGL da sonda de áudio (#100)", priority = 200)]
        public static void BuildMenu()
        {
            string output = EditorUtility.SaveFolderPanel("Pasta da build da sonda", "", "AudioWebProbe");
            if (!string.IsNullOrEmpty(output)) Build(output);
        }

        public static void BuildBatch()
        {
            string output = ArgAfter("-probeOut") ?? Path.Combine(Path.GetTempPath(), "sombra-audio-probe");
            bool ok = Build(output);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build(string output)
        {
            try
            {
                Directory.CreateDirectory(TempFolder);
                // 4 stems de 8 s com um clique no início de cada loop e um tom próprio (a deriva fica audível).
                var stemPaths = new string[4];
                float[] tones = { 55f, 110f, 165f, 220f };
                for (int i = 0; i < 4; i++)
                {
                    stemPaths[i] = $"{TempFolder}/Probe_Stem_L{i}.wav";
                    WriteWav(stemPaths[i], Stem(tones[i], 8f));
                }
                string dryPath = TempFolder + "/Probe_Sfx_Dry.wav", muffledPath = TempFolder + "/Probe_Sfx_Muffled.wav";
                WriteWav(dryPath, Noise(2f, smooth: 1));
                WriteWav(muffledPath, Noise(2f, smooth: 24));   // média móvel longa = passa-baixa pré-renderado
                AssetDatabase.Refresh();

                foreach (var p in stemPaths) SetLoadType(p, AudioClipLoadType.CompressedInMemory);
                SetLoadType(dryPath, AudioClipLoadType.DecompressOnLoad);
                SetLoadType(muffledPath, AudioClipLoadType.DecompressOnLoad);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.gameObject.tag = "MainCamera";
                cam.gameObject.AddComponent<AudioListener>();
                var probe = new GameObject("AudioWebProbe").AddComponent<AudioWebProbe>();
                var so = new SerializedObject(probe);
                var stems = so.FindProperty("stems");
                stems.arraySize = stemPaths.Length;
                for (int i = 0; i < stemPaths.Length; i++)
                    stems.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(stemPaths[i]);
                so.FindProperty("dryClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(dryPath);
                so.FindProperty("muffledClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(muffledPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);

                // Sem compressão: a build abre em qualquer servidor estático (python -m http.server).
                var compression = PlayerSettings.WebGL.compressionFormat;
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                try
                {
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { ScenePath },
                        locationPathName = output,
                        target = BuildTarget.WebGL,
                        options = BuildOptions.None
                    });
                    Debug.Log($"[AudioProbe] Build {report.summary.result} em {output} ({report.summary.totalSize / 1024} KB)");
                    return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
                }
                finally
                {
                    PlayerSettings.WebGL.compressionFormat = compression;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[AudioProbe] Falha no build: " + e);
                return false;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }

        private static float[] Stem(float freq, float seconds)
        {
            var d = new float[(int)(Rate * seconds)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float click = i < Rate / 50 ? 0.8f * (1f - i / (Rate / 50f)) : 0f;
                d[i] = 0.25f * Mathf.Sin(2f * Mathf.PI * freq * t) + click * Mathf.Sin(2f * Mathf.PI * 1000f * t);
            }
            return d;
        }

        private static float[] Noise(float seconds, int smooth)
        {
            var rng = new System.Random(7);
            var raw = new float[(int)(Rate * seconds)];
            for (int i = 0; i < raw.Length; i++) raw[i] = (float)(rng.NextDouble() * 2 - 1) * 0.5f;
            var d = new float[raw.Length];
            float sum = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                sum += raw[i];
                if (i >= smooth) sum -= raw[i - smooth];
                d[i] = sum / smooth * Mathf.Sqrt(smooth);
            }
            return d;
        }

        private static void WriteWav(string path, float[] samples)
        {
            using var w = new BinaryWriter(File.Create(path));
            int bytes = samples.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (var s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
        }

        private static void SetLoadType(string path, AudioClipLoadType loadType)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static string ArgAfter(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
