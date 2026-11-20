using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Level;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.EditorTools
{
    // RC4, issue #93. Joga a partida encadeada sem jogar: vence cada fase pelo GameManager e segue pelo botão
    // PRÓXIMA ALA (GameManager.Proceed). Confere que cada ala abre a seguinte, que o estado do Thomas (upgrade,
    // bateria, estatísticas) atravessa as alas até a Eclusa, que a vitória na Eclusa apaga o checkpoint e volta
    // ao Menu, e que o Menu aponta JOGAR e CONTINUAR para as cenas do WingFlow.
    // Menu: Sombra da Sanidade > Rodar teste do encadeamento das fases.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.WingFlowTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class WingFlowTest
    {
        private const string RunningKey = "SombraWingFlowTest.Running";
        private const string BatchKey = "SombraWingFlowTest.Batch";
        private const string Lens = "Upgrade_LenteFocal";

        private static int step;
        private static float stepStart;
        private static Scene lastScene;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static WingFlowTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do encadeamento das fases", priority = 70)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static string TestPath => Path.Combine(Path.GetTempPath(), "sombra-wingflow-test-93", "checkpoint.json");

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            // Antes do Play: WingFlow e Build Profile.
            Check("WingFlow: Médica → Energia → Executiva → Eclusa → fim",
                WingFlow.NextOf("Medica") == "Energia" && WingFlow.NextOf("Energia") == "Executiva"
                && WingFlow.NextOf("Executiva") == "Eclusa" && WingFlow.NextOf("Eclusa") == "" && WingFlow.IsFinal("Eclusa"));
            Check("WingFlow: cena da Eclusa é \"Eclusa\" (não \"AlaEclusa\")", WingFlow.SceneFor("Eclusa") == "Eclusa");
            Check("WingFlow: ala desconhecida não tem cena", WingFlow.SceneFor("Inexistente") == null && WingFlow.SceneFor(null) == null);
            var build = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            var expected = WingFlowBuildProfile.OrderedPaths();
            Check("Build Profile começa por Menu e as 4 fases, na ordem",
                build.Length >= expected.Length && expected.SequenceEqual(build.Take(expected.Length)));

            Checkpoint.PathOverride = TestPath;
            Checkpoint.Clear();
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[WingFlow] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            Checkpoint.PathOverride = TestPath;
            var scene = SceneManager.GetActiveScene();
            var gm = GameManager.Instance;
            var wing = WingCheckpoint.Instance;
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            float t = Time.realtimeSinceStartup - stepStart;

            switch (step)
            {
                case 0:   // Ala Médica: escolhe um upgrade, vence e segue.
                    if (!Ready(scene, "AlaMedica", gm, t)) return;
                    Check("Ala Médica tem WingCheckpoint apontando para a Energia", wing != null && wing.NextWing == "Energia");
                    if (wing == null || flashlight == null) { Finish(); return; }
                    var lens = Resources.Load<LuxUpgradeTree>("LuxUpgradeTree")?.Find(Lens);
                    Check("Árvore de upgrades carrega de Resources", lens != null);
                    if (lens != null) LuxUpgrades.Apply(lens, flashlight, Object.FindAnyObjectByType<Sanity.SanityController>());
                    WinAndProceed(gm, wing, "AlaEnergia");
                    break;

                case 1:   // Energia: o estado veio da Médica.
                    if (!Ready(scene, "AlaEnergia", gm, t)) return;
                    Check("Energia restaura o checkpoint da Médica", wing != null && wing.Restored);
                    Check("Upgrade da Médica continua instalado", flashlight != null && flashlight.Installed.Any(u => u.name == Lens));
                    Check($"HDs anteriores contam ({gm.PriorDrives})", gm.PriorDrives >= 1);
                    WinAndProceed(gm, wing, "AlaExecutiva");
                    break;

                case 2:
                    if (!Ready(scene, "AlaExecutiva", gm, t)) return;
                    Check("Executiva restaura o checkpoint da Energia", wing != null && wing.Restored);
                    WinAndProceed(gm, wing, "Eclusa");
                    break;

                case 3:   // Eclusa: sem WingCheckpoint na cena, um é criado em runtime.
                    if (!Ready(scene, "Eclusa", gm, t)) return;
                    Check("Eclusa ganha WingCheckpoint em runtime, marcado como final", wing != null && wing.Wing == "Eclusa" && wing.IsFinal);
                    Check("Eclusa restaura o estado (upgrade da Médica)", wing != null && wing.Restored
                        && flashlight != null && flashlight.Installed.Any(u => u.name == Lens));
                    Check($"Eclusa recebe os 3 HDs ({gm.PriorDrives})", gm.PriorDrives == 3);
                    for (int i = 0; i < gm.DrivesRequired; i++) gm.CollectDrive();
                    gm.TriggerVictory();
                    Check("Vitória na Eclusa encerra a partida", gm.State == GameState.Victory && GameManager.ProceedLabel == "MENU");
                    Check("Vitória na Eclusa apaga o checkpoint", !Checkpoint.Exists);
                    lastScene = scene;
                    gm.Proceed();
                    Next();
                    break;

                case 4:   // Menu: sem checkpoint, JOGAR abre a Médica.
                    if (!Ready(scene, WingFlow.MenuScene, null, t)) return;
                    var menu = Object.FindAnyObjectByType<MainMenuScreen>();
                    Check("Depois da Eclusa o jogo volta ao Menu", menu != null);
                    if (menu != null)
                    {
                        Check("JOGAR abre a Ala Médica", menu.GameScene == "AlaMedica");
                        Check("Sem checkpoint não há CONTINUAR", !menu.HasContinue);
                        Checkpoint.Save(new CheckpointData { lastWing = "Executiva", nextWing = "Eclusa" });
                        menu.RefreshContinue();
                        Check("CONTINUAR com checkpoint da Executiva abre a Eclusa", menu.HasContinue && menu.ContinueScene == "Eclusa");
                    }
                    Finish();
                    break;
            }
        }

        // A cena esperada carregou (e não é a anterior) e passou um instante para os Start rodarem.
        private static bool Ready(Scene scene, string name, GameManager gm, float t)
        {
            if (t > 20f)
            {
                Check($"Cena {name} carregou (ativa: {scene.name})", false);
                Finish();
                return false;
            }
            if (scene.name != name || scene == lastScene || t < 0.5f) return false;
            return name == WingFlow.MenuScene || gm != null;
        }

        private static void WinAndProceed(GameManager gm, WingCheckpoint wing, string expectedNext)
        {
            for (int i = 0; i < gm.DrivesRequired; i++) gm.CollectDrive();
            gm.TriggerVictory();
            Check($"Vitória em {wing.Wing} salva o checkpoint", wing.Saved);
            Check($"Botão da vitória é PRÓXIMA ALA e leva a {expectedNext}",
                GameManager.ProceedLabel == "PRÓXIMA ALA" && wing.NextScene == expectedNext);
            lastScene = SceneManager.GetActiveScene();
            gm.Proceed();
            Next();
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
            var sb = new StringBuilder($"[WingFlow] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
