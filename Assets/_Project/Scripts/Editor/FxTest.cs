using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Teste dos efeitos de partícula (#81): material sem luz do URP 2D, prefabs em Prefabs/FX/ com cores da paleta,
    // e os ganchos do GameFxHooks (EMP, great e fail do multímetro) criando e destruindo os efeitos.
    // Menu: Sombra da Sanidade > Arte > Rodar teste dos efeitos de partícula.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.FxTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class FxTest
    {
        private const string RunningKey = "SombraFxTest.Running";
        private const string BatchKey = "SombraFxTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static FxTest()
        {
            if (SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Arte/Rodar teste dos efeitos de partícula", priority = 121)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            FxPrefabBuilder.Build();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FxPrefabBuilder.MaterialPath);
            Check("Material sem luz do URP 2D", mat != null && mat.shader != null && mat.shader.name == FxPrefabBuilder.ShaderName);
            var fx = AssetDatabase.LoadAssetAtPath<GameFx>(FxPrefabBuilder.GameFxPath);
            Check("GameFx em Resources com os 3 prefabs", fx != null && fx.empWave && fx.skillGreat && fx.skillFail);
            if (fx != null)
            {
                var prefabs = new[] { fx.empWave, fx.skillGreat, fx.skillFail }.Where(p => p != null).ToArray();
                Check("Prefabs em Prefabs/FX/", prefabs.All(p => AssetDatabase.GetAssetPath(p).StartsWith(FxPrefabBuilder.PrefabDir)));
                var systems = prefabs.SelectMany(p => p.GetComponentsInChildren<ParticleSystem>()).ToArray();
                Check("Todos os sistemas usam o material e não repetem", systems.Length >= 5
                    && systems.All(s => s.GetComponent<ParticleSystemRenderer>().sharedMaterial == mat && !s.main.loop));
                Check("Raiz se destrói sozinha", prefabs.All(p => p.GetComponent<ParticleSystem>().main.stopAction == ParticleSystemStopAction.Destroy));
                Check("Onda do EMP some em 0,4 s", Mathf.Approximately(fx.empWave.GetComponent<ParticleSystem>().main.startLifetime.constant, 0.4f));
                Check("Cores só da paleta (início branco, gradiente em degraus)", systems.All(InPalette));
            }

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[FX] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var hooks = GameFxHooks.Instance;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5 || GameManager.Instance == null || t < 1f) return;
                    Check("GameFxHooks criado na cena de jogo", hooks != null);
                    if (hooks == null) { Finish(); return; }
                    // Na Médica o multímetro pode não existir ainda: o DriveServer cria no jogador sob demanda.
                    var player = UnityEngine.Object.FindAnyObjectByType<Player.PlayerController>();
                    if (UnityEngine.Object.FindAnyObjectByType<SkillCheck>() == null && player != null)
                        player.gameObject.AddComponent<SkillCheck>();
                    Next();
                    break;

                case 1:
                    if (t < 0.2f) return;   // um Update para o gancho achar o SkillCheck
                    var flashlight = UnityEngine.Object.FindAnyObjectByType<FlashlightController>();
                    var skill = UnityEngine.Object.FindAnyObjectByType<SkillCheck>();
                    Check("Lanterna e multímetro na cena", flashlight != null && skill != null);
                    if (flashlight == null || skill == null) { Finish(); return; }
                    Raise(flashlight, "EmpFired");
                    Raise(skill, "Resolved", SkillCheckResult.Great);
                    Raise(skill, "Resolved", SkillCheckResult.Fail);
                    Raise(skill, "Resolved", SkillCheckResult.Good);
                    Check("EMP cria a onda de choque", Count("FX_EmpWave") == 1);
                    Check("Great cria as partículas verdes", Count("FX_SkillGreat") == 1);
                    Check("Fail cria faíscas e fumaça", Count("FX_SkillFail") == 1 && Find("FX_SkillFail").transform.Find("Fumaca") != null);
                    Check("Good não cria efeito", Count("FX_") == 3);
                    Next();
                    break;

                case 2:
                    if (t < 2f) return;
                    Check("Efeitos somem sozinhos", Count("FX_") == 0);
                    Finish();
                    break;
            }
        }

        private static int Count(string prefix) =>
            UnityEngine.Object.FindObjectsByType<ParticleSystem>().Count(p => p.transform.parent == null && p.name.StartsWith(prefix));

        private static GameObject Find(string prefix) =>
            UnityEngine.Object.FindObjectsByType<ParticleSystem>().First(p => p.name.StartsWith(prefix)).gameObject;

        private static bool InPalette(ParticleSystem s)
        {
            var main = s.main;
            if (main.startColor.mode != ParticleSystemGradientMode.Color || main.startColor.color != Color.white) return false;
            var col = s.colorOverLifetime;
            if (!col.enabled || col.color.mode != ParticleSystemGradientMode.Gradient) return false;
            var g = col.color.gradient;
            var palette = Palette();
            return g.mode == GradientMode.Fixed && g.colorKeys.All(k => palette.Contains(ColorUtility.ToHtmlStringRGB(k.color)));
        }

        private static HashSet<string> Palette()
        {
            var json = System.IO.File.ReadAllText("art-src/palette.json");
            var set = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(json, "\"hex\": \"#([0-9a-fA-F]{6})\""))
                set.Add(m.Groups[1].Value.ToUpperInvariant());
            return set;
        }

        // Dispara um evento C# de outro componente (o campo de apoio tem o mesmo nome do evento).
        private static void Raise(object target, string evt, params object[] args)
        {
            var field = target.GetType().GetField(evt, BindingFlags.Instance | BindingFlags.NonPublic);
            (field?.GetValue(target) as Delegate)?.DynamicInvoke(args);
        }

        private static void Next()
        {
            step++;
            stepStart = Time.realtimeSinceStartup;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[FX] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
