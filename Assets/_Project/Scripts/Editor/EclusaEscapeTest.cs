using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.UI;
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
            endingSeen = false;
            PlayerPrefs.DeleteKey(VanceChoice.AchievementKey);   // a conquista precisa ser liberada pelo teste
            EditorSceneManager.OpenScene(AlfaSceneBuilder.EclusaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            furyChecked = false;
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
                    Check($"Começa com os 3 HDs ({gm.TotalDrives}/{gm.TotalDrivesRequired})", gm.HasAllDrives && gm.TotalDrives == 3);
                    Check("Porta externa ligada ao terminal", door.Terminal == terminal);
                    Check("Porta trancada antes do terminal", !door.IsUnlocked);
                    Check("Purga parada antes do terminal", !escape.Active && !monster.IsFurious);
                    Check($"Purga em config dentro de 2-3 min ({escape.Duration:0} s)", escape.Duration >= 120f && escape.Duration <= 180f);
                    Check("Terminal aceita os HDs", terminal.CanInteract);
                    CheckRc(player, door);
                    // Encostar na porta trancada não vence.
                    Teleport(player, door.transform.position + Vector3.up * 0.3f);
                    Next();
                    break;

                case 1:
                    if (t < 0.5f) return;
                    Check("Porta trancada: encostar não vence", gm.IsPlaying);
                    // Lanterna apagada: na purga ele tem que vir mesmo assim.
                    var fl = Object.FindAnyObjectByType<SombraDaSanidade.Flashlight.FlashlightController>();
                    if (fl != null)
                        typeof(SombraDaSanidade.Flashlight.FlashlightController)
                            .GetField("<IsSwitchedOn>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            ?.SetValue(fl, false);
                    terminal.CompleteInteract();
                    Check("Terminal inicia a purga", escape.Active && terminal.Activated && !terminal.CanInteract);
                    var lamps = Object.FindObjectsByType<RoomLamp>();
                    Check($"Na purga, arandelas e sirenes viram alarme vermelho ({lamps.Length})",
                        lamps.Length > 0 && System.Array.TrueForAll(lamps, l => l.IsAlarm));
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
                    CheckSaveVance(terminal);
                    Next();
                    break;

                case 2:
                    if (!furyChecked)
                    {
                        furyChecked = true;
                        Check($"Na purga persegue o jogador mesmo com a lanterna apagada ({monster.State})", monster.State == MonsterState.Chase);
                    }
                    if (CutscenePlayer.IsPlaying)
                    {
                        if (!endingSeen) Check($"Porta toca o final antes do laudo ({CutscenePlayer.Instance.Current?.id})",
                            CutscenePlayer.Instance.Current != null && CutscenePlayer.Instance.Current.id == "C4");
                        endingSeen = true;
                        CutscenePlayer.Skip();
                        return;
                    }
                    if (gm.IsPlaying && t < 3f) return;
                    Check("Chegar à porta destravada vence", gm.State == GameState.Victory);
                    Check($"Final Testemunha no laudo ({VanceChoice.Title(VanceChoice.Ending)})", VanceChoice.Ending == Ending.Witness
                        && VanceChoice.Title(VanceChoice.Ending) == "TESTEMUNHA");
                    Check("Conquista A Verdade Corporativa liberada", VanceChoice.HasAchievement && EndingDirector.AchievementJustUnlocked);
                    var rank = RankConfig.Load();
                    float baseScore = rank.Score(gm.RunTime, gm.TotalDrivesRequired, gm.AverageSanity, gm.RunEmps, gm.Deaths);
                    Check($"Bônus no rank ({baseScore:0.00} -> {gm.RankScore:0.00})",
                        gm.RankScore >= Mathf.Min(1f, baseScore + rank.witnessBonus) - 0.001f);
                    PlayerPrefs.DeleteKey(VanceChoice.AchievementKey);
                    gm.Restart();
                    Next();
                    break;

                case 3:
                    // Espera a cena recarregar (GameManager novo, purga parada).
                    if (gm == null || escape == null || escape.Active || t < 0.3f) return;
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    monster.gameObject.SetActive(false);   // a causa da morte tem que ser a purga
                    terminal.CompleteInteract();
                    Check("Reiniciar pede a escolha de novo", VanceChoiceTerminal.IsOpen && VanceChoice.Decision == VanceDecision.None);
                    terminal.GetComponent<VanceChoiceTerminal>().Choose(0);
                    Check("Deixar a Vance: final Amostra e cela trancada", VanceChoice.Ending == Ending.Sample
                        && !Object.FindAnyObjectByType<VanceCell>().CanInteract && Time.timeScale > 0f);
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

        private static bool endingSeen;

        // Escolha "salvar" (#95) e a cela (#82): 6 s, 2 skill checks, câmeras ligadas na abertura; solta, abre o
        // atalho e desliga as câmeras do caminho.
        private static void CheckSaveVance(EclusaTerminal terminal)
        {
            Check("Os HDs abrem a escolha e pausam o jogo", VanceChoiceTerminal.IsOpen && Time.timeScale == 0f);
            terminal.GetComponent<VanceChoiceTerminal>().Choose(1);
            Check("Escolher 2 salva a Vance e despausa", VanceChoice.Decision == VanceDecision.Save
                && !VanceChoiceTerminal.IsOpen && Time.timeScale > 0f);
            var cell = Object.FindAnyObjectByType<VanceCell>();
            var skill = cell.SkillCheckConfig;
            Check($"Cela: segurar E por 6 s com 2 skill checks ({cell.HoldDuration} s, {skill?.skillCheckMinPerDrive}-{skill?.skillCheckMaxPerDrive})",
                cell.CanInteract && Mathf.Approximately(cell.HoldDuration, 6f)
                && skill != null && skill.skillCheckMinPerDrive == 2 && skill.skillCheckMaxPerDrive == 2);
            int near = 0;
            foreach (var c in SecurityCamera.All)
                if (Vector2.Distance(c.transform.position, cell.transform.position) < 15f && c.State != SecurityCameraState.Off) near++;
            Check($"Câmeras da cela ligadas durante a abertura ({near})", near >= 1);
            cell.CompleteInteract();
            var gates = Object.FindObjectsByType<ShortcutGate>();
            Check("Vance solta abre o atalho", VanceChoice.Released && gates.Length == 1 && gates[0].IsOpen
                && !gates[0].GetComponent<BoxCollider2D>().enabled);
            int off = 0;
            foreach (var c in SecurityCamera.All) if (c.State == SecurityCameraState.Off) off++;
            Check($"Vance desliga as câmeras do caminho ({off} de {SecurityCamera.All.Count})", off >= 1 && off < SecurityCamera.All.Count);
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

        private static bool furyChecked;

        // Dois caminhos da escolha (planejamento-rc 3, #86): cela da Vance do outro lado, atalho fechado até ela ser
        // solta, câmeras e os dois monstros. O Sonâmbulo sai da cena depois: o teste da purga é sobre o Espécime.
        private static void CheckRc(PlayerController player, ExitDoor door)
        {
            var cell = LevelMarker.Find("cela_eclusa_1");
            float fromStart = cell == null ? 0f : Vector2.Distance(cell.transform.position, player.transform.position);
            Check($"Cela da Vance do outro lado do mapa ({fromStart:0} m do início)", cell != null && fromStart >= 60f);
            var gates = Object.FindObjectsByType<ShortcutGate>();
            Check("Atalho fechado no começo", gates.Length == 1 && !gates[0].IsOpen
                && gates[0].GetComponent<BoxCollider2D>().enabled);
            var vanceCell = Object.FindAnyObjectByType<VanceCell>();
            Check("Cela da Vance trancada antes da escolha", vanceCell != null && !vanceCell.CanInteract);
            int cams = Object.FindObjectsByType<SecurityCamera>().Length;
            Check($"Câmeras ativas na Eclusa (veio {cams})", cams >= 3);
            var walkers = Object.FindObjectsByType<SleepwalkerController>();
            Check("Sonâmbulo na Eclusa", walkers.Length == 1);
            foreach (var w in walkers) w.gameObject.SetActive(false);
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
