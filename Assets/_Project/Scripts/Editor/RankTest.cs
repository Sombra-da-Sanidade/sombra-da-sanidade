using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5. Teste das telas e do rank (#32) e do CONTINUAR (#45): conta do rank, Menu como cena 0,
    // EMPs, mortes que sobrevivem ao reinício, sanidade média, melhor rank em PlayerPrefs e o menu com checkpoint.
    // O melhor rank e o checkpoint de quem testa são guardados e restaurados.
    // Menu: Sombra da Sanidade > Rodar teste de rank e telas.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.RankTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class RankTest
    {
        private const string RunningKey = "SombraRankTest.Running";
        private const string BatchKey = "SombraRankTest.Batch";
        private const string PrevBestKey = "SombraRankTest.PrevBest";

        private static int step;
        private static float stepStart;
        private static GameManager firstGm;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static RankTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste de rank e telas", priority = 68)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;
            CheckRankMath();
            CheckBuildOrder();

            var best = GameManager.BestRank(SceneName);
            SessionState.SetInt(PrevBestKey, best.HasValue ? (int)best.Value : -1);
            GameManager.ClearBestRank(SceneName);

            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Rank] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static string SceneName => Path.GetFileNameWithoutExtension(PrototypeSceneBuilder.PrototypeScenePath);

        // Sem Play: a conta da Proposta 3 com os valores do asset.
        private static void CheckRankMath()
        {
            var config = Resources.Load<RankConfig>(RankConfig.ResourcePath);
            Check("RankConfig em Resources com os pesos 30/30/20/20", config != null
                && Mathf.Approximately(config.timeWeight, 0.3f) && Mathf.Approximately(config.sanityWeight, 0.3f)
                && Mathf.Approximately(config.empWeight, 0.2f) && Mathf.Approximately(config.deathWeight, 0.2f));
            if (config == null) return;

            float par = config.parSecondsPerDrive;
            Check("Partida perfeita dá S", config.RankFor(config.Score(par, 1, 1f, 0, 0)) == Rank.S);
            Check("Partida péssima dá C", config.RankFor(config.Score(par * 10f, 1, 0f, 20, 10)) == Rank.C);
            Check("Mais tempo nunca melhora a nota", config.Score(par * 2f, 1, 1f, 0, 0) < config.Score(par, 1, 1f, 0, 0));
            Check("Tempo ideal cresce com os HDs da cena", config.Score(par * 2f, 3, 1f, 0, 0) > config.Score(par * 2f, 1, 1f, 0, 0));
            Check("Cortes S/A/B na ordem certa", config.RankFor(config.aAbove) == Rank.A && config.RankFor(config.bAbove) == Rank.B
                && config.RankFor(config.sAbove) == Rank.S && config.RankFor(config.bAbove - 0.01f) == Rank.C);
        }

        private static void CheckBuildOrder()
        {
            var scenes = EditorBuildSettings.scenes;
            Check("Menu é a cena 0 do Build Profile", scenes.Length > 0 && scenes[0].path == MenuSceneBuilder.ScenePath && scenes[0].enabled);
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var gm = GameManager.Instance;
            float t = Time.realtimeSinceStartup - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5 || gm == null) return;
                    var flashlight = Object.FindAnyObjectByType<FlashlightController>();
                    var sanity = Object.FindAnyObjectByType<SanityController>();
                    Check("Cena tem GameManager, lanterna e sanidade", flashlight && sanity);
                    if (failed) { Finish(); return; }
                    Calm();
                    Check("Começa sem EMP e sem morte", gm.EmpsUsed == 0 && gm.Deaths == 0);

                    // EMP de verdade, pelo mesmo caminho do clique direito.
                    typeof(FlashlightController).GetProperty("IsSwitchedOn").SetValue(flashlight, true);
                    typeof(FlashlightController).GetMethod("TryFireEmp", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(flashlight, null);
                    Check("EMP disparado conta 1", gm.EmpsUsed == 1);

                    gm.TriggerGameOver(DeathCause.Monster);
                    Check("Game Over conta 1 morte", gm.Deaths == 1);
                    firstGm = gm;
                    gm.Restart();
                    Next();
                    break;

                case 1:
                    if (gm == null || gm == firstGm || t < 0.3f) return;
                    Calm();
                    Check("A morte sobrevive ao reinício da cena", gm.Deaths == 1);
                    Check("EMPs recomeçam no reinício", gm.EmpsUsed == 0);
                    var s = Object.FindAnyObjectByType<SanityController>();
                    s.Add(-s.Value * 0.5f);
                    Next();
                    break;

                case 2:
                    if (t < 1f) return;
                    for (int i = 0; i < gm.DrivesRequired; i++) gm.CollectDrive();
                    gm.TriggerVictory();
                    float avg = gm.AverageSanity;
                    Check($"Sanidade média entre a cheia e a metade ({avg:P0})", avg > 0.45f && avg < 0.95f);
                    var config = RankConfig.Load();
                    var expected = config.RankFor(config.Score(gm.ElapsedTime, gm.DrivesRequired, avg, gm.EmpsUsed, gm.Deaths));
                    Check($"Vitória calcula o rank ({gm.FinalRank}, nota {gm.RankScore:0.00})",
                        gm.State == GameState.Victory && gm.FinalRank == expected);
                    Check("Primeiro rank da cena é o melhor e fica salvo",
                        gm.IsNewBestRank && GameManager.BestRank(SceneName) == gm.FinalRank);

                    // Menu com checkpoint: CONTINUAR leva à cena da próxima ala.
                    Checkpoint.PathOverride = Path.Combine(Path.GetTempPath(), "sombra-rank-test", "checkpoint.json");
                    Checkpoint.Save(new CheckpointData { lastWing = "Saguao", nextWing = "Medica" });
                    SceneManager.LoadScene(Path.GetFileNameWithoutExtension(MenuSceneBuilder.ScenePath));
                    Next();
                    break;

                case 3:
                    var menu = Object.FindAnyObjectByType<MainMenuScreen>();
                    if (menu == null && t < 3f) return;
                    if (t < 0.3f) return;
                    Check("Menu carrega e destrava o tempo", menu != null && Mathf.Approximately(Time.timeScale, 1f));
                    if (menu != null)
                    {
                        Check("Com checkpoint, o menu oferece CONTINUAR para a próxima ala",
                            menu.HasContinue && menu.ContinueScene == "AlaMedica");
                        Checkpoint.Save(new CheckpointData { lastWing = "Medica", nextWing = "Inexistente" });
                        menu.RefreshContinue();
                        Check("Sem a cena da próxima ala, não oferece CONTINUAR", !menu.HasContinue);
                        Checkpoint.Clear();
                        menu.RefreshContinue();
                        Check("Sem checkpoint, não oferece CONTINUAR", !menu.HasContinue);
                    }
                    Checkpoint.PathOverride = null;
                    Finish();
                    break;
            }
        }

        // Monstro e diretor parados: o teste não depende de onde o monstro anda.
        private static void Calm()
        {
            var monster = Object.FindAnyObjectByType<MonsterController>();
            if (monster != null) monster.enabled = false;
            if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
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
            Checkpoint.PathOverride = null;
            // Devolve o melhor rank de quem testou.
            GameManager.ClearBestRank(SceneName);
            int prev = SessionState.GetInt(PrevBestKey, -1);
            if (prev >= 0) GameManager.SaveBestRank(SceneName, (Rank)prev);

            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Rank] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
