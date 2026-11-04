using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2. Teste dos comportamentos das Alas de Energia e Executiva (#37) na cena de protótipo:
    // checar de longe uma Zona Segura acesa, atalho só do monstro e memória curta de ruídos.
    // Menu: Sombra da Sanidade > Rodar teste das alas do monstro.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.MonsterWingTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class MonsterWingTest
    {
        private const string RunningKey = "SombraWingTest.Running";
        private const string BatchKey = "SombraWingTest.Batch";
        private const float ZoneRadius = 2.5f;

        private static int step;
        private static float stepStart;
        private static GameObject zone;
        private static float closest;
        private static Vector2 oldNoise;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static MonsterWingTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste das alas do monstro", priority = 65)]
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
            Debug.Log("[Alas] Iniciando teste");
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
                    Check("Presets por ala somam comportamentos",
                        (MonsterWingBehaviours.Executiva & MonsterWingBehaviours.Energia) == MonsterWingBehaviours.Energia
                        && (MonsterWingBehaviours.Energia & MonsterWingBehaviours.Medica) == MonsterWingBehaviours.Medica);
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    player.enabled = false;
                    var cfg = Object.Instantiate(monster.Config);   // clone: o asset não muda
                    cfg.waypointPause = 0.2f;
                    cfg.listenDuration = 0.4f;
                    cfg.followNoiseChance = 0f;
                    cfg.breakerCheckChance = 1f;
                    cfg.breakerCheckCooldown = 999f;
                    cfg.noiseMemoryChance = 1f;
                    SetField(monster, "config", cfg);

                    // Ala de Energia: uma Zona Segura acesa no ponto de patrulha mais longe do monstro.
                    monster.SetBehaviours(MonsterBehaviour.BreakerCheck);
                    zone = new GameObject("TestLitZone");
                    zone.transform.position = FarthestPatrolPoint(monster, monster.transform.position);
                    zone.AddComponent<MonsterRepellent>().Radius = ZoneRadius;
                    closest = float.MaxValue;
                    Next();
                    break;

                case 1:
                {
                    float d = Vector2.Distance(monster.transform.position, zone.transform.position);
                    closest = Mathf.Min(closest, d);
                    var agent = monster.GetComponent<NavMeshAgent>();
                    bool watching = monster.State == MonsterState.Investigate && agent.isStopped;
                    if (!(watching && d < ZoneRadius + 4f) && t < 30f) return;
                    Check($"Energia: vai olhar a Zona Segura acesa de longe ({d:0.0} m do centro)", watching && d < ZoneRadius + 4f);
                    Check($"Energia: nunca entra na luz (mais perto: {closest:0.0} m, raio {ZoneRadius})", closest >= ZoneRadius - 0.1f);
                    Object.Destroy(zone);

                    // Ala Executiva: dois ruídos longe demais para ouvir na hora; ele só lembra deles.
                    monster.SetBehaviours(MonsterBehaviour.NoiseMemory);
                    oldNoise = FarthestPatrolPoint(monster, monster.transform.position);
                    Vector2 recent = SecondFarthestPatrolPoint(monster, monster.transform.position, oldNoise);
                    NoiseBus.Emit(oldNoise, 0.1f);
                    NoiseBus.Emit(recent, 0.1f);
                    Next();
                    break;
                }

                case 2:
                {
                    float d = Vector2.Distance(monster.transform.position, oldNoise);
                    if (d > 1.5f && t < 35f) return;
                    Check($"Executiva: volta a um ruído antigo, não só ao último ({d:0.0} m dele)", d <= 1.5f);

                    // Ala de Energia: atalho só do monstro atravessando o bloco central.
                    var a = new Vector2(0f, -3f);
                    var b = new Vector2(0f, 5f);
                    var go = new GameObject("TestShortcut");
                    go.AddComponent<NavMeshLink>();
                    var shortcut = go.AddComponent<MonsterShortcut>();
                    shortcut.Setup(a, b);
                    monster.SetBehaviours(MonsterBehaviour.Shortcuts);
                    Next();
                    break;
                }

                case 3:
                {
                    if (t < 0.3f) return;
                    var a = new Vector2(0f, -3f);
                    var b = new Vector2(0f, 5f);
                    float with = PathLength(a, b);
                    monster.SetBehaviours(MonsterBehaviour.None);
                    Next();
                    SessionState.SetFloat("SombraWingTest.With", with);
                    break;
                }

                case 4:
                {
                    if (t < 0.3f) return;
                    float with = SessionState.GetFloat("SombraWingTest.With", -1f);
                    float without = PathLength(new Vector2(0f, -3f), new Vector2(0f, 5f));
                    Check($"Energia: com atalho o caminho encurta ({without:0.0} -> {with:0.0} m)", with > 0f && with < without - 3f);
                    Finish();
                    break;
                }
            }
        }

        private static float PathLength(Vector2 a, Vector2 b)
        {
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return -1f;
            float len = 0f;
            for (int i = 1; i < path.corners.Length; i++) len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return len;
        }

        private static Transform[] Points(MonsterController monster) =>
            (Transform[])monster.GetType().GetField("patrolPoints", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(monster);

        private static Vector2 FarthestPatrolPoint(MonsterController monster, Vector2 from)
        {
            Vector2 best = from;
            float bestD = -1f;
            foreach (var p in Points(monster))
            {
                float d = Vector2.Distance(p.position, from);
                if (d > bestD) { bestD = d; best = p.position; }
            }
            return best;
        }

        private static Vector2 SecondFarthestPatrolPoint(MonsterController monster, Vector2 from, Vector2 exclude)
        {
            Vector2 best = from;
            float bestD = -1f;
            foreach (var p in Points(monster))
            {
                if (Vector2.Distance(p.position, exclude) < 4f) continue;
                float d = Vector2.Distance(p.position, from);
                if (d > bestD) { bestD = d; best = p.position; }
            }
            return best;
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

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Alas] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
