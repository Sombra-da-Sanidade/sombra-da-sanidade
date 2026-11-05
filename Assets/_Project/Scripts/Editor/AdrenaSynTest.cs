using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 1. Teste do Módulo Adrena-Syn (#34) na cena de protótipo: -20 de sanidade, +35% de velocidade,
    // renovar não acumula, convive com a lentidão do Estabilizador e volta ao normal no fim.
    // Menu: Sombra da Sanidade > Rodar teste do Adrena-Syn.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AdrenaSynTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class AdrenaSynTest
    {
        private const string RunningKey = "SombraAdrenaSynTest.Running";
        private const string BatchKey = "SombraAdrenaSynTest.Batch";

        private static int step;
        private static float stepStart;
        private static float boosted;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static AdrenaSynTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do Adrena-Syn", priority = 71)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Adrena-Syn] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var module = Object.FindAnyObjectByType<AdrenaSynModule>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem módulo, jogador e sanidade", module && player && sanity);
                    if (failed) { Finish(); return; }
                    var monster = Object.FindAnyObjectByType<MonsterController>();
                    if (monster != null) monster.gameObject.SetActive(false);
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;

                    var cfg = player.Config;
                    float expected = 1f + cfg.adrenaSynSpeedBonus;
                    Check("Módulo pode ser usado", module.CanInteract);
                    float before = sanity.Value;
                    module.CompleteInteract();
                    float cost = before - sanity.Value;
                    Check($"Custa {sanity.Config.adrenaSynCost:0} de sanidade ({cost:0.0})", Mathf.Abs(cost - sanity.Config.adrenaSynCost) < 0.5f);
                    Check($"Velocidade x{expected:0.00} ({player.SpeedMultiplier:0.00})", Mathf.Approximately(player.SpeedMultiplier, expected));
                    Check("Módulo some depois de usado", !module.CanInteract);

                    // Acima do limiar de corrida do PlayerSpriteAnimator: anda com run_<dir>.
                    var animator = player.GetComponent<PlayerSpriteAnimator>();
                    float runAbove = animator != null
                        ? new SerializedObject(animator).FindProperty("runSpeedMultiplier").floatValue : 1.05f;
                    Check("Multiplicador passa do limiar de corrida do sprite", player.SpeedMultiplier > runAbove);

                    // Renovar durante o efeito não multiplica de novo.
                    AdrenaSynModule.Apply(player);
                    Check($"Tomar de novo não acumula ({player.SpeedMultiplier:0.00})", Mathf.Approximately(player.SpeedMultiplier, expected));

                    // Estabilizador junto: os dois multiplicadores convivem.
                    boosted = player.SpeedMultiplier;
                    LithiumStabilizer.ApplyEffect(sanity, sanity);
                    float both = expected * sanity.Config.stabilizerSlowMultiplier;
                    Check($"Com o Estabilizador fica x{both:0.00} ({player.SpeedMultiplier:0.00})", Mathf.Approximately(player.SpeedMultiplier, both));
                    Next();
                    break;

                case 1:
                    if (t < sanity.Config.stabilizerSlowDuration + 0.3f) return;
                    Check($"Fim da lentidão volta ao bônus ({player.SpeedMultiplier:0.00})", Mathf.Approximately(player.SpeedMultiplier, boosted));
                    Next();
                    break;

                case 2:
                    var effect = player.GetComponent<AdrenaSynEffect>();
                    if (effect != null && effect.IsActive && t < player.Config.adrenaSynDuration) return;
                    Check($"Efeito acaba e a velocidade volta ao normal ({player.SpeedMultiplier:0.00})",
                        effect != null && !effect.IsActive && Mathf.Approximately(player.SpeedMultiplier, 1f));
                    Finish();
                    break;
            }
        }

        private static void Next()
        {
            step++;
            stepStart = Time.time;
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
            var sb = new StringBuilder($"[Adrena-Syn] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
