using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 1. Teste do skill check do HD (#33) na cena de protótipo: great, good, falha (progresso, pausa,
    // ruído), máximo de 3 por HD, ruído baixo depois de 50%, faixa menor por HD e com o monstro perto.
    // Segura E pelo PlayerInteractor.HoldOverride (batchmode não tem input) e usa uma cópia do
    // InteractionConfig sem sorteio, para os checks começarem quando o teste quer.
    // Menu: Sombra da Sanidade > Rodar teste do skill check.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SkillCheckTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SkillCheckTest
    {
        private const string RunningKey = "SombraSkillCheckTest.Running";
        private const string BatchKey = "SombraSkillCheckTest.Batch";
        private const float TestEject = 10f;

        private static int step;
        private static float stepStart;
        private static float mark;
        private static InteractionConfig cfg;
        private static readonly List<(Vector2 pos, float radius)> noises = new List<(Vector2, float)>();
        private static readonly List<string> results = new List<string>();
        private static bool failed;
        private static DriveServer second;
        private static readonly List<float> warnedRemaining = new List<float>();

        static SkillCheckTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do skill check", priority = 74)]
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
            Debug.Log("[Skill check] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void OnNoise(Vector2 pos, float radius) => noises.Add((pos, radius));

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            var check = player != null ? player.GetComponent<SkillCheck>() : null;
            var server = Object.FindObjectsByType<DriveServer>(FindObjectsSortMode.InstanceID).FirstOrDefault();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem jogador com interactor, servidor e GameManager", player && interactor && server && gm);
                    if (failed) { Finish(); return; }
                    Check("Jogador tem SkillCheck e SkillCheckHud", check && player.GetComponent<UI.SkillCheckHud>());
                    if (failed) { Finish(); return; }
                    var monster = Object.FindAnyObjectByType<MonsterController>();
                    if (monster != null) monster.gameObject.SetActive(false);
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    player.enabled = false;

                    var original = (InteractionConfig)GetField(server, "config");
                    Check("Ejeção do HD dura 12 s no config", Mathf.Approximately(original.ejectDuration, 12f));
                    Check("Faixa good 15/13/11% por HD coletado",
                        Mathf.Approximately(SkillCheck.ZoneSizeFor(original, 0, false), 0.15f)
                        && Mathf.Approximately(SkillCheck.ZoneSizeFor(original, 1, false), 0.13f)
                        && Mathf.Approximately(SkillCheck.ZoneSizeFor(original, 2, false), 0.11f));
                    Check("Monstro perto encolhe a faixa em 40%", Mathf.Approximately(SkillCheck.ZoneSizeFor(original, 0, true), 0.09f));

                    cfg = Object.Instantiate(original);
                    cfg.ejectDuration = TestEject;
                    cfg.skillCheckChancePerSecond = 0f;
                    cfg.skillCheckMinPerDrive = 0;   // os passos 1-9 começam os checks na mão
                    cfg.upgradeTree = null;          // o terminal de upgrade pausaria o tempo entre os HDs
                    SetField(server, "config", cfg);

                    NoiseBus.NoiseEmitted += OnNoise;
                    noises.Clear();
                    Teleport(player, server.transform.position + Vector3.down * 1.1f);
                    interactor.HoldOverride = true;
                    Next();
                    break;

                case 1:   // great
                    if (!interactor.IsChanneling || t < 0.3f) return;
                    Check("Segurar E canaliza o servidor", ReferenceEquals(interactor.Current, server));
                    Check("Check começa com aviso", check.StartCheck() && check.State == SkillCheckState.Warning);
                    Next();
                    break;

                case 2:
                    if (check.State != SkillCheckState.Active) { if (t > 2f) Fail("Aviso não virou ponteiro"); return; }
                    mark = interactor.Progress;
                    var r = check.ResolveAt(check.ZoneStart + check.GreatSize * 0.5f);
                    Check($"Great no começo da faixa dá +{cfg.skillCheckGreatBonus:P0} ({mark:P0} -> {interactor.Progress:P0})",
                        r == SkillCheckResult.Great && interactor.Progress > mark + cfg.skillCheckGreatBonus - 0.01f);
                    Next();
                    break;

                case 3:   // good
                    if (check.State == SkillCheckState.Result) return;
                    check.StartCheck();
                    Next();
                    break;

                case 4:
                    if (check.State != SkillCheckState.Active) return;
                    mark = interactor.Progress;
                    r = check.ResolveAt(check.ZoneStart + check.ZoneSize - 0.005f);
                    Check("Good no fim da faixa só continua", r == SkillCheckResult.Good && Mathf.Abs(interactor.Progress - mark) < 0.01f);
                    Next();
                    break;

                case 5:   // falha
                    if (check.State == SkillCheckState.Result) return;
                    noises.Clear();
                    check.StartCheck();
                    Next();
                    break;

                case 6:
                    if (check.State != SkillCheckState.Active) return;
                    mark = interactor.Progress;
                    r = check.ResolveAt(0f);
                    Check($"Apertar fora da faixa é falha e tira {cfg.skillCheckFailPenalty:P0} ({mark:P0} -> {interactor.Progress:P0})",
                        r == SkillCheckResult.Fail && Mathf.Abs(interactor.Progress - (mark - cfg.skillCheckFailPenalty)) < 0.01f);
                    Check($"Falha faz barulho de raio {cfg.skillCheckFailNoiseRadius:0}", noises.Any(n => Mathf.Approximately(n.radius, cfg.skillCheckFailNoiseRadius)));
                    Check("Falha pausa o progresso", interactor.IsProgressPaused);
                    mark = interactor.Progress;
                    Next();
                    break;

                case 7:
                    if (t < cfg.skillCheckFailPause * 0.5f) return;
                    Check("Progresso parado durante a pausa", Mathf.Abs(interactor.Progress - mark) < 0.005f);
                    Check($"Máximo de {cfg.skillCheckMaxPerDrive} checks por HD", server.SkillChecksDone == cfg.skillCheckMaxPerDrive
                        && !server.CanSkillCheck && !check.StartCheck());
                    Check("Servidor conta 1 falha (para o rank, #49)", server.SkillCheckFailures == 1);
                    noises.Clear();
                    Next();
                    break;

                case 8:   // ruído baixo depois de 50%
                    if (interactor.Progress < 0.5f) { if (t > TestEject + 2f) Fail("Progresso não passou de 50%"); return; }
                    if (!noises.Any(n => Mathf.Approximately(n.radius, cfg.ejectLateNoiseRadius)) && t < TestEject + 2f + cfg.ejectLateNoiseInterval) return;
                    Check($"Depois de 50% o servidor faz ruído de raio {cfg.ejectLateNoiseRadius:0}",
                        noises.Any(n => Mathf.Approximately(n.radius, cfg.ejectLateNoiseRadius)));
                    Next();
                    break;

                case 9:
                    if (gm.DrivesCollected < 1 && t < TestEject + 2f) return;
                    interactor.HoldOverride = false;
                    Check($"HD ejetado ({gm.DrivesCollected})", gm.DrivesCollected == 1);

                    // Segundo HD sem sorte nenhuma (chance 0): o check garantido precisa aparecer, a tempo.
                    second = Object.FindObjectsByType<DriveServer>(FindObjectsInactive.Exclude)
                        .FirstOrDefault(s => s != server && s.CanInteract);
                    if (second == null) { Fail("Cena tem um segundo servidor energizado"); return; }
                    var lucky = Object.Instantiate(cfg);
                    lucky.skillCheckMinPerDrive = 1;
                    SetField(second, "config", lucky);
                    warnedRemaining.Clear();
                    check.Warned += OnWarned;
                    Teleport(player, second.transform.position + Vector3.down * 1.1f);
                    interactor.HoldOverride = true;
                    Next();
                    break;

                case 10:
                    if (gm.DrivesCollected < 2 && t < TestEject + 6f) return;
                    interactor.HoldOverride = false;
                    check.Warned -= OnWarned;
                    float latest = SkillCheck.LatestStart(second.SkillCheckConfig);
                    Check($"Ejeção sem sorte ainda tem 1 skill check ({second.SkillChecksDone})", second.SkillChecksDone == 1);
                    Check($"Check começa antes do corte de fim (restavam {(warnedRemaining.Count > 0 ? warnedRemaining[0] : 0f):0.0} s, mínimo {latest:0.0} s)",
                        warnedRemaining.Count == 1 && warnedRemaining[0] >= latest - 0.05f);
                    Check($"Segundo HD ejetado ({gm.DrivesCollected})", gm.DrivesCollected == 2);
                    Finish();
                    break;
            }
        }

        private static void OnWarned()
        {
            var check = Object.FindAnyObjectByType<SkillCheck>();
            if (check != null) warnedRemaining.Add(check.RemainingSeconds());
        }

        private static void Fail(string label)
        {
            Check(label, false);
            Finish();
        }

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

        private static void Teleport(PlayerController player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
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
            if (step < 0) return;
            step = -1;
            NoiseBus.NoiseEmitted -= OnNoise;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Skill check] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
