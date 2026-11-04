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
    // Frente 2. Teste do modo seguro (#48) na cena de protótipo: mais lento, encostar empurra e drena sanidade
    // sem Game Over; desligado, encostar volta a matar. O valor salvo em PlayerPrefs é restaurado no fim.
    // Menu: Sombra da Sanidade > Rodar teste do modo seguro.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.MonsterSafeModeTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class MonsterSafeModeTest
    {
        private const string RunningKey = "SombraSafeModeTest.Running";
        private const string BatchKey = "SombraSafeModeTest.Batch";
        private const string PrevKey = "SombraSafeModeTest.Previous";

        private static int step;
        private static float stepStart;
        private static float sanityMark;
        private static Vector2 contactPoint;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static MonsterSafeModeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do modo seguro", priority = 66)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(PrevKey, MonsterSafeMode.Enabled);
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Modo seguro] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro, jogador, sanidade e GameManager", monster && player && sanity && gm);
                    if (failed) { Finish(); return; }
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    player.enabled = false;
                    float normal = monster.CurrentSpeed;
                    MonsterSafeMode.Enabled = true;
                    var agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    Check($"Modo seguro deixa o monstro mais lento ({normal:0.00} -> {monster.CurrentSpeed:0.00} m/s)",
                        monster.CurrentSpeed < normal - 0.1f && Mathf.Approximately(agent.speed, monster.CurrentSpeed));
                    sanityMark = sanity.Value;
                    contactPoint = (Vector2)monster.transform.position + Vector2.right * 0.2f;
                    Teleport(player, contactPoint);
                    Next();
                    break;

                case 1:
                    if (t < 0.5f) return;
                    Check("Encostar não dá Game Over", gm.IsPlaying);
                    Check($"Encostar drena sanidade ({sanityMark:0} -> {sanity.Value:0})", sanity.Value < sanityMark - 5f);
                    float pushed = Vector2.Distance(player.transform.position, contactPoint);
                    Check($"Jogador é empurrado para longe ({pushed:0.0} m)", pushed > 0.5f);
                    Check("Monstro para um pouco depois do encontrão", monster.IsStunned);

                    MonsterSafeMode.Enabled = false;
                    Next();
                    break;

                case 2:
                    if (monster.IsStunned && t < 4f) return;
                    Teleport(player, monster.transform.position);
                    Next();
                    break;

                case 3:
                    if (gm.IsPlaying && Time.realtimeSinceStartup - stepStart < 1f) return;
                    Check("Desligado, encostar volta a matar", gm.State == GameState.GameOver && gm.LastDeathCause == DeathCause.Monster);
                    Finish();
                    break;
            }
        }

        private static void Next()
        {
            step++;
            stepStart = step == 3 ? Time.realtimeSinceStartup : Time.time;
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

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            MonsterSafeMode.Enabled = SessionState.GetBool(PrevKey, false);   // não muda a preferência de quem testou
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Modo seguro] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
