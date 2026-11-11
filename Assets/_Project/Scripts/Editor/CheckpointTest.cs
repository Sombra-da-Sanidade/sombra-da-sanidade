using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Level;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5. Teste do checkpoint completo (#45) na Ala Médica, com um save numa pasta temporária (não mexe no
    // save de quem testa): a ala começa com bateria, sanidade, upgrades e estatísticas do save; o reinício depois
    // de um Game Over volta ao checkpoint sem apagar a morte; a vitória salva tudo para a próxima ala.
    // Menu: Sombra da Sanidade > Rodar teste do checkpoint.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.CheckpointTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class CheckpointTest
    {
        private const string RunningKey = "SombraCheckpointTest.Running";
        private const string BatchKey = "SombraCheckpointTest.Batch";
        private const string Lens = "Upgrade_LenteFocal", Resonator = "Upgrade_RessonadorNeural";

        private static int step;
        private static float stepStart;
        private static GameManager firstGm;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static CheckpointTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do checkpoint", priority = 69)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static string TestPath => Path.Combine(Path.GetTempPath(), "sombra-checkpoint-test-45", "checkpoint.json");

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            // Como se o jogador tivesse concluído o Saguão e fosse entrar na Ala Médica.
            Checkpoint.PathOverride = TestPath;
            Checkpoint.Save(new CheckpointData
            {
                lastWing = "Saguao", nextWing = "Medica", drives = 1, elapsedSeconds = 90f,
                battery = 40f, sanity = 60f, upgrades = new[] { Lens, Resonator },
                totalSeconds = 100f, deaths = 2, emps = 3, sanitySum = 50f, sanitySeconds = 100f, skillCheckFailures = 4,
                savedAtUtc = "2026-01-01T00:00:00Z"
            });

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Checkpoint] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            // Sem recarregar o domínio, o caminho de teste continua; com recarga, é preciso apontar de novo.
            Checkpoint.PathOverride = TestPath;
            var gm = GameManager.Instance;
            var wing = WingCheckpoint.Instance;
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            float t = Time.realtimeSinceStartup - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5 || gm == null) return;
                    Check("Cena tem GameManager, WingCheckpoint, lanterna e sanidade", wing && flashlight && sanity);
                    if (failed) { Finish(); return; }
                    Check("Entrar na ala do checkpoint restaura o estado", wing.Restored);
                    Check($"Bateria do save ({flashlight.Battery:0})", Mathf.Abs(flashlight.Battery - 40f) < 1f);
                    Check($"Sanidade do save ({sanity.Value:0.0})", Mathf.Abs(sanity.Value - 60f) < 3f);
                    var names = flashlight.Installed.Select(u => u.name).ToArray();
                    Check($"Upgrades reinstalados ({string.Join(", ", names)})",
                        names.Length == 2 && names.Contains(Lens) && names.Contains(Resonator));
                    Check("Ressonador vale de novo (pistas abaixo de 50)", Mathf.Approximately(sanity.RevealCluesBelow, 50f));
                    Check("Mortes, EMPs, HDs e falhas das alas anteriores contam",
                        gm.Deaths == 2 && gm.RunEmps == 3 && gm.PriorDrives == 1 && gm.CountSkillCheckFailures() >= 4);
                    Check($"Tempo da partida soma as alas anteriores ({gm.RunTime:0.0} s)", gm.RunTime >= 100f && gm.RunTime < 110f);

                    flashlight.TrySpend(20f);
                    gm.TriggerGameOver(DeathCause.Monster);
                    firstGm = gm;
                    gm.Restart();
                    Next();
                    break;

                case 1:
                    if (gm == null || gm == firstGm || t < 0.5f) return;
                    Check($"Reinício volta à bateria do checkpoint ({flashlight.Battery:0})", Mathf.Abs(flashlight.Battery - 40f) < 1f);
                    Check("Reinício reinstala os upgrades uma vez só", flashlight.Installed.Count == 2);
                    Check($"A morte desta tentativa não é apagada ({gm.Deaths})", gm.Deaths == 3);

                    for (int i = 0; i < gm.DrivesRequired; i++) gm.CollectDrive();
                    gm.TriggerVictory();
                    bool loaded = Checkpoint.TryLoad(out var data);
                    Check("Vitória salva o checkpoint da próxima ala", wing.Saved && loaded
                        && data.lastWing == "Medica" && data.nextWing == "Energia");
                    if (loaded)
                    {
                        Check($"Salva HDs da partida ({data.drives}) e o estado do Thomas",
                            data.drives == 1 + gm.DrivesRequired && Mathf.Abs(data.battery - flashlight.Battery) < 0.5f
                            && Mathf.Abs(data.sanity - sanity.Value) < 0.5f
                            && data.upgrades.Length == 2 && data.upgrades.Contains(Lens) && data.upgrades.Contains(Resonator));
                        Check("Salva as estatísticas do rank da partida",
                            data.deaths == 3 && data.emps == 3 && data.totalSeconds >= 100f
                            && data.sanitySeconds > 100f && data.skillCheckFailures >= 4);
                    }
                    Finish();
                    break;
            }
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
            Checkpoint.Clear();
            Checkpoint.PathOverride = null;
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Checkpoint] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
