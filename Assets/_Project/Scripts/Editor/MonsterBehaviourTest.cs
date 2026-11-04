using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2. Teste dos comportamentos por ala (#36) na cena de protótipo: a busca da Ala Médica
    // (pontos por perto em ordem subótima, com retorno a pontos já vistos) e o liga/desliga por SetBehaviours.
    // Menu: Sombra da Sanidade > Rodar teste dos comportamentos do monstro.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.MonsterBehaviourTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class MonsterBehaviourTest
    {
        private const string RunningKey = "SombraBehaviourTest.Running";
        private const string BatchKey = "SombraBehaviourTest.Batch";

        private static int step;
        private static float stepStart;
        private static float arrivedAt;
        private static readonly List<int> visits = new List<int>();
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static MonsterBehaviourTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste dos comportamentos do monstro", priority = 64)]
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
            Debug.Log("[Comportamentos] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro e jogador", monster && player);
                    if (failed) { Finish(); return; }
                    Check("Ala Médica começa com a busca ligada", monster.Has(MonsterBehaviour.Search));
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    player.enabled = false;
                    // Jogador longe e parado: o teste é só sobre a busca. Config clonado: o asset não muda.
                    var cfg = Object.Instantiate(monster.Config);
                    cfg.listenDuration = 0.4f;
                    cfg.waypointPause = 0.2f;
                    cfg.searchPoints = 3;
                    cfg.searchRevisitChance = 0f;
                    cfg.searchRadius = 14f;
                    SetField(monster, "config", cfg);
                    StartNoise(monster);
                    Next();
                    break;

                case 1:
                    Track(monster);
                    if (monster.State != MonsterState.Patrol && t < 25f) return;
                    int distinct = new HashSet<int>(visits).Count;
                    Check($"Busca checa pontos perto do ruído antes de voltar à rota ({distinct} pontos)", distinct >= 2);
                    Check("Termina a busca e volta a patrulhar", monster.State == MonsterState.Patrol);

                    monster.SetBehaviours(MonsterBehaviour.None);
                    StartNoise(monster);
                    arrivedAt = -1f;
                    Next();
                    break;

                case 2:
                    var agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (arrivedAt < 0f && monster.State == MonsterState.Investigate && agent.isStopped) arrivedAt = Time.time;
                    if (monster.State != MonsterState.Patrol && t < 20f) return;
                    float afterListen = arrivedAt < 0f ? 99f : Time.time - arrivedAt;
                    Check($"Sem a busca, volta à rota logo depois de escutar ({afterListen:0.0} s)", afterListen < 1.2f);

                    monster.SetBehaviours(MonsterBehaviour.Search);
                    var cfg2 = monster.Config;
                    cfg2.searchRevisitChance = 1f;
                    cfg2.searchPoints = 4;
                    StartNoise(monster);
                    Next();
                    break;

                case 3:
                    Track(monster);
                    if (monster.State != MonsterState.Patrol && t < 30f) return;
                    bool repeated = new HashSet<int>(visits).Count < visits.Count;
                    Check($"Às vezes volta a um ponto já visto (sequência {string.Join(",", visits)})", repeated);
                    Finish();
                    break;
            }
        }

        // Ruído num ponto de patrulha perto do monstro, e zera o registro de visitas.
        private static void StartNoise(MonsterController monster)
        {
            var points = (Transform[])GetField(monster, "patrolPoints");
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (var p in points)
            {
                float d = Vector2.Distance(p.position, monster.transform.position);
                if (d > 2f && d < bestD) { bestD = d; best = p; }
            }
            visits.Clear();
            NoiseBus.Emit(best.position, 100f);
        }

        private static void Track(MonsterController monster)
        {
            if (monster.State != MonsterState.Investigate) return;
            var points = (Transform[])GetField(monster, "patrolPoints");
            for (int i = 0; i < points.Length; i++)
            {
                if (Vector2.Distance(points[i].position, monster.transform.position) > 0.8f) continue;
                if (visits.Count == 0 || visits[visits.Count - 1] != i) visits.Add(i);
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

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Comportamentos] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
