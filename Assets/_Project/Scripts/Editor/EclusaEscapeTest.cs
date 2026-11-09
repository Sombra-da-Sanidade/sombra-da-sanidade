using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Fuga da Eclusa (#44, GDD 5 e 7 fase 4): começa com os 3 HDs, a porta externa só abre depois do terminal,
    // o terminal inicia a purga e a fúria do monstro, chegar à porta vence e o fim da purga é Game Over próprio.
    // Menu: Sombra da Sanidade > Rodar teste da Eclusa.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.EclusaEscapeTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class EclusaEscapeTest
    {
        private const string RunningKey = "SombraEclusaTest.Running";
        private const string BatchKey = "SombraEclusaTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static EclusaEscapeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da Eclusa", priority = 67)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.EclusaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Eclusa] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var gm = GameManager.Instance;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var door = Object.FindAnyObjectByType<ExitDoor>();
            var escape = EscapeSequence.Instance;
            var terminal = escape != null ? escape.Terminal : null;
            // Tempo real: vitória e Game Over zeram o timeScale.
            float t = Time.realtimeSinceStartup - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem GameManager, jogador, monstro, terminal, porta externa e purga",
                        gm && player && monster && door && escape && terminal);
                    if (failed) { Finish(); return; }
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    Check($"Começa com os 3 HDs ({gm.DrivesCollected}/{gm.DrivesRequired})", gm.HasAllDrives && gm.DrivesRequired == 3);
                    Check("Porta externa ligada ao terminal", door.Terminal == terminal);
                    Check("Porta trancada antes do terminal", !door.IsUnlocked);
                    Check("Purga parada antes do terminal", !escape.Active && !monster.IsFurious);
                    Check($"Purga em config dentro de 2-3 min ({escape.Duration:0} s)", escape.Duration >= 120f && escape.Duration <= 180f);
                    Check("Terminal aceita os HDs", terminal.CanInteract);
                    // Encostar na porta trancada não vence.
                    Teleport(player, door.transform.position + Vector3.up * 0.3f);
                    Next();
                    break;

                case 1:
                    if (t < 0.5f) return;
                    Check("Porta trancada: encostar não vence", gm.IsPlaying);
                    terminal.CompleteInteract();
                    Check("Terminal inicia a purga", escape.Active && terminal.Activated && !terminal.CanInteract);
                    Check("Monstro em fúria com os comportamentos da Executiva",
                        monster.IsFurious && monster.Behaviours == MonsterWingBehaviours.Executiva);
                    var cfg = monster.Config;
                    float expected = monster.State == MonsterState.Chase
                        ? Mathf.Min(cfg.furySpeed * cfg.chaseSpeedMultiplier, Mathf.Max(cfg.chaseMaxSpeed, cfg.maxSpeed))
                        : cfg.furySpeed;
                    if (MonsterSafeMode.Enabled) expected *= cfg.safeModeSpeedMultiplier;
                    Check($"Velocidade da fúria (coluna Eclusa {cfg.furySpeed}): {monster.CurrentSpeed:0.00}",
                        Mathf.Approximately(cfg.furySpeed, 4.6f) && Mathf.Abs(monster.CurrentSpeed - expected) < 0.01f);
                    Check("Porta destravada depois do terminal", door.IsUnlocked);
                    Next();
                    break;

                case 2:
                    if (gm.IsPlaying && t < 3f) return;
                    Check("Chegar à porta destravada vence", gm.State == GameState.Victory);
                    gm.Restart();
                    Next();
                    break;

                case 3:
                    // Espera a cena recarregar (GameManager novo, purga parada).
                    if (gm == null || escape == null || escape.Active || t < 0.3f) return;
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    monster.gameObject.SetActive(false);   // a causa da morte tem que ser a purga
                    terminal.CompleteInteract();
                    escape.SetRemaining(0.5f);
                    Check("Faltando pouco, ainda em jogo", gm.IsPlaying && escape.Active);
                    Next();
                    break;

                case 4:
                    if (gm.IsPlaying && t < 5f) return;
                    Check($"Purga chega: Game Over com causa própria ({gm.State}, {gm.LastDeathCause})",
                        gm.State == GameState.GameOver && gm.LastDeathCause == DeathCause.Purge);
                    Finish();
                    break;
            }
        }

        private static void Teleport(PlayerController player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
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
            var sb = new StringBuilder($"[Eclusa] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
