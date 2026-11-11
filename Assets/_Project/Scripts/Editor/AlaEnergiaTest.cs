using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NavMeshPlus.Components;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SombraDaSanidade.EditorTools
{
    // Ala de Energia (#42): perfil de dificuldade da ala (#30), 3 disjuntores com um ao lado da entrada
    // (Proposta 4), subestação barulhenta que exige o EMP na hora certa, HD-02 (log e upgrade certos), atalho do
    // monstro e checkpoint ao concluir (#45).
    // Menu: Sombra da Sanidade > Rodar teste da Ala de Energia.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AlaEnergiaTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class AlaEnergiaTest
    {
        private const string RunningKey = "SombraAlaEnergiaTest.Running";
        private const string BatchKey = "SombraAlaEnergiaTest.Batch";

        private static int step;
        private static float stepStart;
        private static int noises;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static AlaEnergiaTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da Ala de Energia", priority = 67)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaEnergiaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Ala de Energia] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            var substation = Object.FindAnyObjectByType<Substation>();
            var server = Object.FindAnyObjectByType<DriveServer>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro, jogador, sanidade, subestação, servidor, checkpoint, objetivos e GameManager",
                        monster && player && sanity && substation && server && WingCheckpoint.Instance
                        && Object.FindAnyObjectByType<WingObjectives>() && gm);
                    if (failed) { Finish(); return; }
                    CheckProfile(gm, monster, sanity);
                    CheckMap(player);
                    Check("Porta abre com 1 HD nesta ala", gm.DrivesRequired == 1);
                    Check($"Sem checkpoint, a ala conta o HD-01 da Médica (anteriores: {gm.PriorDrives})", gm.PriorDrives == 1);
                    Check("Monstro presente desde a entrada", monster.isActiveAndEnabled
                        && Object.FindAnyObjectByType<WingIntro>() == null);
                    Check("Servidor do HD-02 sem energia antes da subestação", !server.IsPowered && !server.CanInteract);
                    Next();
                    break;

                case 1:
                    if (t < 0.5f) return;
                    var shortcut = Object.FindAnyObjectByType<MonsterShortcut>();
                    Check("Atalho do monstro existe e está ligado na Energia",
                        shortcut != null && shortcut.GetComponent<NavMeshLink>().enabled);

                    // Reparo da subestação de verdade: segura E parado em frente ao console.
                    player.enabled = false;
                    // O monstro começa na sala da subestação: sem isto ele às vezes alcança o jogador antes do EMP
                    // roteirizado. Vai para a entrada e volta pelo WarpMonster do passo seguinte.
                    WarpMonster(monster, player.transform.position);
                    Teleport(player, (Vector2)substation.transform.position + Vector2.down * 0.2f);
                    NoiseBus.NoiseEmitted += CountNoise;
                    noises = 0;
                    player.GetComponent<PlayerInteractor>().HoldOverride = true;
                    Next();
                    break;

                case 2:
                    // O monstro chega pelo barulho: aparece a 6 m e o jogador dá o EMP na hora certa.
                    if (t < 1.5f) return;
                    var interactor = player.GetComponent<PlayerInteractor>();
                    Check("Segurar E em frente ao console começa o reparo", interactor.IsChanneling && substation.IsRepairing);
                    WarpMonster(monster, (Vector2)substation.transform.position + new Vector2(6f, -0.5f));
                    monster.Stun(Object.FindAnyObjectByType<FlashlightController>().EmpStunDuration);
                    Check("EMP atordoa o monstro durante o reparo", monster.IsStunned);
                    Next();
                    break;

                case 3:
                    if (!substation.IsPowered && t < 7f && gm.IsPlaying) return;
                    NoiseBus.NoiseEmitted -= CountNoise;
                    player.GetComponent<PlayerInteractor>().HoldOverride = false;
                    Check($"Reparo leva {substation.HoldDuration:0} s e faz barulho o tempo todo ({noises} ruídos)",
                        substation.HoldDuration >= 5f && noises >= 4);
                    Check("Com o EMP na hora certa, o jogador termina vivo", gm.IsPlaying);
                    Check("Subestação religada", substation.IsPowered);
                    Check("Servidor do HD-02 ganha energia", server.IsPowered && server.CanInteract);
                    if (failed) { Finish(); return; }
                    CheckDriveAndCheckpoint(gm, server, player);
                    Finish();
                    break;
            }
        }

        private static void CheckProfile(GameManager gm, MonsterController monster, SanityController sanity)
        {
            var p = gm.ActiveProfile;
            Check("GameManager com o DifficultyProfile da Energia", p != null && p.wing == "Energia");
            if (p == null) return;
            // O perfil é lido direto pelos sistemas (DifficultyProfile.Active); os configs compartilhados não mudam.
            Check($"Monstro: velocidade 3.85 e raio auditivo 6 (veio {monster.CurrentSpeed} / {monster.HearingRadius})",
                Mathf.Approximately(monster.CurrentSpeed, 3.85f) && Mathf.Approximately(monster.HearingRadius, 6f));
            Check("Monstro: checa disjuntores e usa atalhos", monster.Has(MonsterBehaviour.BreakerCheck) && monster.Has(MonsterBehaviour.Shortcuts));
            Check($"Sanidade: dreno 1.5/s no escuro e alucinação a cada 20-30 s (veio {sanity.DarknessDrain}, {p.hallucinationInterval.x}-{p.hallucinationInterval.y})",
                Mathf.Approximately(sanity.DarknessDrain, 1.5f) && p.hallucinations
                && Mathf.Approximately(p.hallucinationInterval.x, 20f) && Mathf.Approximately(p.hallucinationInterval.y, 30f));
            Check("Assets compartilhados não mudam com o perfil",
                !Mathf.Approximately(AssetDatabase.LoadAssetAtPath<MonsterConfig>("Assets/_Project/ScriptableObjects/Config/MonsterConfig.asset").baseSpeed, 3.85f));

            var table = PrototypeSceneBuilder.LoadLootTable("Energia");
            float total = table.nothingWeight + table.batteryWeight + table.sedativeWeight + table.adrenaSynWeight;
            Check($"Tabela de loot bate com o perfil: bateria {table.batteryWeight / total:P0}",
                Mathf.Abs(table.batteryWeight / total - p.batteryChance) < 0.01f);
        }

        private static void CheckMap(PlayerController player)
        {
            var breakers = Object.FindObjectsByType<Breaker>();
            var p = GameManager.Instance.ActiveProfile;
            Check($"{breakers.Length} disjuntores (perfil: {p.breakersAvailable})", breakers.Length == p.breakersAvailable);
            float nearest = breakers.Length == 0 ? float.MaxValue
                : breakers.Min(b => Vector2.Distance(b.transform.position, player.transform.position));
            Check($"Disjuntor ao lado da entrada para o primeiro reparo sem pressão ({nearest:0.0} m)", nearest <= 4f);
            var monster = Object.FindAnyObjectByType<MonsterController>();
            Check($"Monstro começa longe da entrada ({Vector2.Distance(monster.transform.position, player.transform.position):0} m)",
                Vector2.Distance(monster.transform.position, player.transform.position) > 25f);
        }

        // HD-02: log VANCE-02 e escolha de upgrade do 2º HD; vitória salva o checkpoint para a Executiva.
        private static void CheckDriveAndCheckpoint(GameManager gm, DriveServer server, PlayerController player)
        {
            string dir = Path.Combine(Path.GetTempPath(), "sombra-checkpoint-test");
            Checkpoint.PathOverride = Path.Combine(dir, "checkpoint.json");
            try
            {
                Checkpoint.Clear();
                server.CompleteInteract();
                Check($"HD-02 coletado (partida: {gm.TotalDrives} HDs)", gm.DrivesCollected == 1 && gm.TotalDrives == 2);
                var log = AudioLogPlayer.Instance != null ? AudioLogPlayer.Instance.Current : null;
                Check($"Toca o log do 2º HD (veio: {(log != null ? log.id : "nenhum")})", log != null && log.id == "VANCE-02");
                var terminal = player.GetComponent<UpgradeTerminal>();
                Check($"Escolha de upgrade do HD-02 (veio: HD {(terminal != null ? terminal.DriveNumber : 0)})",
                    terminal != null && terminal.DriveNumber == 2);
                if (terminal != null && UpgradeTerminal.IsOpen) terminal.Choose(0);

                gm.TriggerVictory();
                var wing = WingCheckpoint.Instance;
                Check("Concluir a ala salva o checkpoint", wing.Saved && Checkpoint.TryLoad(out var data)
                    && data.lastWing == "Energia" && data.nextWing == "Executiva" && data.drives == 2);
                Check($"Tela de vitória mostra \"{wing.StatusLine}\"", wing.StatusLine == "PROGRESSO SALVO");
            }
            finally
            {
                Checkpoint.Clear();
                Checkpoint.PathOverride = null;
            }
        }

        private static void CountNoise(Vector2 position, float radius) => noises++;

        private static void WarpMonster(MonsterController monster, Vector2 position)
        {
            var agent = monster.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                monster.transform.position = position;
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
            stepStart = Time.time;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        private static void Finish()
        {
            NoiseBus.NoiseEmitted -= CountNoise;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Ala de Energia] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
