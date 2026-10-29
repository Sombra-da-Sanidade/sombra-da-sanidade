using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2 (Monstro). Teste do Espécime na cena de protótipo: patrulha, ruído, luz, EMP, HD, Zona Segura e contato.
    // Menu: Sombra da Sanidade > Rodar teste do monstro (resultado no Console).
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.MonsterTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class MonsterTest
    {
        private const string RunningKey = "SombraMonsterTest.Running";
        private const string BatchKey = "SombraMonsterTest.Batch";

        private static int step;
        private static float stepStart;
        private static Vector2 mark;
        private static float distMark;
        private static GameObject zone;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static MonsterTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do monstro", priority = 61)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);

            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Monstro] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;

            var monster = Object.FindAnyObjectByType<MonsterController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var gm = GameManager.Instance;
            var agent = monster != null ? monster.GetComponent<NavMeshAgent>() : null;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro, jogador, lanterna e GameManager", monster && player && flashlight && gm);
                    if (failed) { Finish(); return; }
                    Check("Monstro está no NavMesh", agent.enabled && agent.isOnNavMesh);
                    Check("Começa em Patrulha", monster.State == MonsterState.Patrol);
                    var body = monster.transform.Find("Body");
                    Check("Sprite do Espécime aplicado", body && body.GetComponent<SpriteRenderer>().sprite != null);
                    if (failed) { Finish(); return; }
                    // Sem mouse no teste: a mira fica onde o teste mandar.
                    player.enabled = false;
                    SetLight(flashlight, false);
                    mark = monster.transform.position;
                    Next();
                    break;

                case 1:
                    if (t < 2.5f) return;
                    float walked = Vector2.Distance(mark, monster.transform.position);
                    Check($"Patrulha anda pelo NavMesh ({walked:0.0} m em 2,5 s)", walked > 1f);
                    NavMesh.SamplePosition(new Vector3(7f, -1f, 0f), out var hit, 2f, NavMesh.AllAreas);
                    mark = hit.position;
                    distMark = Vector2.Distance(monster.transform.position, mark);
                    NoiseBus.Emit(mark, 100f);
                    Check("Ruído leva à Investigação", monster.State == MonsterState.Investigate);
                    Next();
                    break;

                case 2:
                    if (t < 2f) return;
                    float d = Vector2.Distance(monster.transform.position, mark);
                    Check($"Vai até o ruído ({distMark:0.0} -> {d:0.0} m)", d < distMark - 1f);
                    // Corredor externo da direita: monstro 5 m acima do jogador, lanterna apontada para ele.
                    agent.Warp(new Vector3(17.5f, -6f, 0f));
                    Teleport(player, new Vector2(17.5f, -11f));
                    SetFacing(player, Vector2.up);
                    SetLight(flashlight, false);
                    Next();
                    break;

                case 3:
                    if (t < 0.6f) return;
                    Check("Lanterna apagada apontada para ele não atrai", monster.State != MonsterState.Chase);
                    SetLight(flashlight, true);
                    Next();
                    break;

                case 4:
                    if (monster.State != MonsterState.Chase && t < 1f) return;
                    Check("Luz no monstro inicia a Perseguição", monster.State == MonsterState.Chase);
                    Check("Grito de detecção antes de correr", monster.IsDetecting);
                    distMark = Vector2.Distance(monster.transform.position, player.transform.position);
                    Next();
                    break;

                case 5:
                    // Grito (0,8 s) + um pouco de corrida, ainda longe o bastante para não encostar.
                    if (t < 1.4f) return;
                    float dc = Vector2.Distance(monster.transform.position, player.transform.position);
                    Check($"Corre na direção do jogador ({distMark:0.0} -> {dc:0.0} m)", dc < distMark - 0.3f);
                    CheckEmp(flashlight, monster, player, agent);
                    // Jogador some no escuro, longe dali.
                    SetLight(flashlight, false);
                    Teleport(player, new Vector2(0f, -11f));
                    Next();
                    break;

                case 6:
                    if (t < 0.5f) return;
                    Check("EMP deixa Atordoado", monster.State == MonsterState.Stunned);
                    Check($"Atordoado fica parado ({agent.velocity.magnitude:0.00} m/s)", agent.velocity.magnitude < 0.05f);
                    Next();
                    break;

                case 7:
                    if (t < 4f) return;
                    Check("Volta a agir depois do stun e procura onde o jogador estava", monster.State == MonsterState.Investigate);
                    gm.CollectDrive();
                    Check($"HD acelera +10% ({monster.CurrentSpeed:0.00} m/s, agente {agent.speed:0.00})",
                        Mathf.Approximately(monster.CurrentSpeed, 3.85f) && Mathf.Approximately(agent.speed, 3.85f));
                    zone = new GameObject("TestSafeZone");
                    zone.transform.position = monster.transform.position;
                    zone.AddComponent<MonsterRepellent>().Radius = 2.5f;
                    Next();
                    break;

                case 8:
                    float dz = Vector2.Distance(monster.transform.position, zone.transform.position);
                    if (dz < 2.5f && t < 3f) return;
                    Check($"Sai de uma Zona Segura que acende em cima dele ({dz:0.0} m do centro)", dz >= 2.5f);
                    Object.Destroy(zone);
                    Teleport(player, monster.transform.position);
                    Next();
                    break;

                case 9:
                    if (gm.IsPlaying && Time.realtimeSinceStartup - stepStart < 1f) return;
                    Check("Encostar no monstro é Game Over", gm.State == GameState.GameOver && gm.LastDeathCause == DeathCause.Monster);
                    Finish();
                    break;
            }
        }

        private static void Next()
        {
            step++;
            stepStart = step == 9 ? Time.realtimeSinceStartup : Time.time;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        private static void Teleport(PlayerController player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
        }

        private static void CheckEmp(FlashlightController flashlight, MonsterController monster,
            PlayerController player, NavMeshAgent agent)
        {
            // Corredor externo direito: mira acima da cabeça, deixando pés e tronco fora do cone.
            agent.Warp(new Vector3(18f, -6f, 0f));
            Teleport(player, new Vector2(16.5f, -6f));
            float angle = 80f * Mathf.Deg2Rad;
            SetFacing(player, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            Physics2D.SyncTransforms();
            Vector2 feet = monster.transform.position;
            Check("Regressão EMP: pés e tronco fora do cone, cabeça dentro",
                !flashlight.IsPointInCone(feet)
                && !flashlight.IsPointInCone(feet + Vector2.up * 0.2f)
                && !flashlight.IsPointInCone(feet + Vector2.up * 1.4f)
                && flashlight.IsPointInCone(feet + Vector2.up * 2.6f));
            Check("Detecção reconhece a cabeça iluminada", monster.IsLitByFlashlight());

            var config = (FlashlightConfig)new SerializedObject(flashlight)
                .FindProperty("config").objectReferenceValue;
            flashlight.Recharge(config.maxBattery);
            flashlight.TrySpend(flashlight.Battery - config.empCost);

            int noises = 0;
            bool stunnedAtNoise = false;
            System.Action<Vector2, float> onNoise = (_, _) =>
            {
                noises++;
                stunnedAtNoise = monster.IsStunned;
            };
            NoiseBus.NoiseEmitted += onNoise;
            try
            {
                FireEmp(flashlight);
                Check("EMP acerta cabeça com carga exata e zera a bateria",
                    monster.IsStunned && Mathf.Approximately(flashlight.Battery, 0f));
                Check("EMP atordoa antes de emitir ruído", noises == 1 && stunnedAtNoise);

                FireEmp(flashlight);
                Check("Sem carga mostra aviso e não emite outro ruído",
                    EmpFeedback(flashlight) == "Carga insuficiente para EMP" && noises == 1);
                SetLight(flashlight, false);
                FireEmp(flashlight);
                Check("Lanterna desligada mostra aviso sem ruído ou gasto",
                    EmpFeedback(flashlight) == "Lanterna desligada" && noises == 1
                    && Mathf.Approximately(flashlight.Battery, 0f));
            }
            finally
            {
                NoiseBus.NoiseEmitted -= onNoise;
            }
        }

        // Exercita o mesmo disparo do clique direito sem depender de input em batchmode.
        private static void FireEmp(FlashlightController flashlight) =>
            typeof(FlashlightController).GetMethod("TryFireEmp", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(flashlight, null);

        private static string EmpFeedback(FlashlightController flashlight) =>
            (string)typeof(FlashlightController).GetField("empFeedbackMessage", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(flashlight);

        // Propriedades com private set: o teste ajusta por reflexão para não abrir a API do jogador e da lanterna.
        private static void SetFacing(PlayerController player, Vector2 dir) =>
            SetBackingField(player, "Facing", dir.normalized);

        private static void SetLight(FlashlightController flashlight, bool on) =>
            SetBackingField(flashlight, "IsSwitchedOn", on);

        private static void SetBackingField(object target, string property, object value)
        {
            var field = target.GetType().GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new TestSetupException(target.GetType().Name, property);
            field.SetValue(target, value);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);

            var sb = new StringBuilder($"[Monstro] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());

            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.Exit(failed ? 1 : 0);
        }

        private class TestSetupException : System.Exception
        {
            public TestSetupException(string type, string property)
                : base($"Campo de {type}.{property} não encontrado (o teste usa reflexão).") { }
        }
    }
}
