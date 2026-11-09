using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Ala Médica do vertical slice: primeiro minuto sem monstro, encontro roteirizado que reage à luz
    // (grito + investigação, sem Perseguição) e checkpoint "Progresso salvo" ao concluir (#45).
    // Menu: Sombra da Sanidade > Rodar teste da Ala Médica.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AlaMedicaIntroTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class AlaMedicaIntroTest
    {
        private const string RunningKey = "SombraAlaMedicaTest.Running";
        private const string BatchKey = "SombraAlaMedicaTest.Batch";

        // Dentro do gatilho oeste (x da planta) e no topo da Enfermaria, de frente para o corredor norte.
        private static readonly Vector2 WestTrigger = new Vector2(10.5f, 19.5f);
        private static readonly Vector2 BelowNorthCorridor = new Vector2(10.5f, 30.5f);
        private static readonly Vector2 Antessala = new Vector2(35.5f, 6.5f);

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static AlaMedicaIntroTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da Ala Médica", priority = 66)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Ala Médica] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var intro = Object.FindAnyObjectByType<WingIntro>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro, jogador, lanterna, introdução, checkpoint e GameManager",
                        monster && player && flashlight && intro && WingCheckpoint.Instance && gm);
                    if (failed) { Finish(); return; }
                    Check($"Introdução começa com ~1 min de calma ({intro.CalmSeconds:0} s)", intro.CalmSeconds >= 55f);
                    Check("Monstro adormecido: controller desligado", !monster.isActiveAndEnabled);
                    Check("Monstro adormecido: invisível", !AnyEnabled(monster.GetComponentsInChildren<Renderer>()));
                    Check("Monstro adormecido: sem colisão", !AnyEnabled(monster.GetComponentsInChildren<Collider2D>()));
                    Check("Ala começa com 1 HD para abrir a porta", gm.DrivesRequired == 1);

                    // Encurta a calma para o teste e espera dentro do gatilho oeste antes do tempo.
                    SetField(intro, "calmSeconds", 2f);
                    SetField(intro, "maxCalmSeconds", 999f);
                    player.enabled = false;
                    SetLight(flashlight, false);
                    Teleport(player, WestTrigger);
                    Next();
                    break;

                case 1:
                    if (t < 1f) return;
                    Check("No gatilho antes do tempo mínimo o monstro não acorda", !intro.Awakened);
                    Next();
                    break;

                case 2:
                    if (!intro.Awakened && t < 5f) return;
                    Check("Depois do tempo mínimo, entrar no gatilho acorda o monstro", intro.Awakened);
                    Check($"Variante da Enfermaria (veio: {intro.Variant})", intro.Variant == "Enfermaria");
                    Next();
                    break;

                case 3:
                    if (!monster.IsOnRoute && t < 1f) return;
                    Check("Monstro acordado está ativo e visível", monster.isActiveAndEnabled
                        && AnyEnabled(monster.GetComponentsInChildren<Renderer>()));
                    Check("Segue a rota roteirizada", monster.IsOnRoute);
                    Check($"Começa longe do jogador ({Vector2.Distance(monster.transform.position, player.transform.position):0.0} m)",
                        Vector2.Distance(monster.transform.position, player.transform.position) > 10f);
                    NoiseBus.Emit(player.transform.position, 100f);
                    Check("Na rota, ruído não o desvia", monster.IsOnRoute && monster.State == MonsterState.Patrol);

                    // Lanterna acesa apontada para o corredor norte, de onde ele desce.
                    Teleport(player, BelowNorthCorridor);
                    SetFacing(player, Vector2.up);
                    SetLight(flashlight, true);
                    Next();
                    break;

                case 4:
                    if (!monster.IsDetecting && t < 4f) return;
                    Check("Iluminado na rota: grita (detecção)", monster.IsDetecting);
                    Check("Iluminado na rota: larga a rota", !monster.IsOnRoute);
                    Next();
                    break;

                case 5:
                    if (monster.IsDetecting && t < 3f) return;
                    Check($"Depois do grito investiga, sem Perseguição (estado: {monster.State})", monster.State == MonsterState.Investigate);
                    Next();
                    break;

                case 6:
                    if (t < 0.3f) return;
                    Check($"Folga logo depois: a luz ainda não vira Perseguição (estado: {monster.State})", monster.State != MonsterState.Chase);
                    SetLight(flashlight, false);
                    Teleport(player, Antessala);
                    // Rota curta dentro da Enfermaria: no fim ele volta à patrulha.
                    monster.FollowRoute(new[] { new Vector2(10.5f, 28.5f), new Vector2(14.5f, 25.5f) });
                    Next();
                    break;

                case 7:
                    if (monster.IsOnRoute && t < 15f) return;
                    Check($"Termina a rota e volta à patrulha (estado: {monster.State})", !monster.IsOnRoute && monster.State == MonsterState.Patrol);
                    CheckCheckpoint(gm);
                    Finish();
                    break;
            }
        }

        // #45: round-trip do JSON e salvamento ao vencer, numa pasta temporária (não mexe no save de quem joga).
        private static void CheckCheckpoint(GameManager gm)
        {
            string dir = Path.Combine(Path.GetTempPath(), "sombra-checkpoint-test");
            Checkpoint.PathOverride = Path.Combine(dir, "checkpoint.json");
            try
            {
                Checkpoint.Clear();
                Check("Sem checkpoint, TryLoad devolve false", !Checkpoint.TryLoad(out _));
                Checkpoint.Save(new CheckpointData { lastWing = "Teste", nextWing = "X", drives = 2, elapsedSeconds = 12.5f });
                bool loaded = Checkpoint.TryLoad(out var data);
                Check("Checkpoint salva e carrega o mesmo conteúdo",
                    loaded && data.lastWing == "Teste" && data.nextWing == "X" && data.drives == 2
                    && Mathf.Approximately(data.elapsedSeconds, 12.5f) && !string.IsNullOrEmpty(data.savedAtUtc));
                File.WriteAllText(Checkpoint.FilePath, "{ quebrado");
                Check("JSON corrompido não quebra o jogo", !Checkpoint.TryLoad(out _));
                Checkpoint.Clear();

                gm.CollectDrive();
                gm.TriggerVictory();
                var wing = WingCheckpoint.Instance;
                Check("Concluir a ala salva o checkpoint", wing.Saved && Checkpoint.TryLoad(out data)
                    && data.lastWing == "Medica" && data.nextWing == "Energia" && data.drives == 1);
                Check($"Tela de vitória mostra \"{wing.StatusLine}\"", wing.StatusLine == "PROGRESSO SALVO");
            }
            finally
            {
                Checkpoint.Clear();
                Checkpoint.PathOverride = null;
            }
        }

        private static bool AnyEnabled<T>(T[] items) where T : Component
        {
            foreach (var item in items)
            {
                if (item is Renderer r && r.enabled) return true;
                if (item is Collider2D c && c.enabled) return true;
            }
            return false;
        }

        private static void Teleport(PlayerController player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
        }

        private static void SetFacing(PlayerController player, Vector2 dir) =>
            SetBackingField(player, "Facing", dir.normalized);

        private static void SetLight(FlashlightController flashlight, bool on) =>
            SetBackingField(flashlight, "IsSwitchedOn", on);

        private static void SetBackingField(object target, string property, object value) =>
            target.GetType().GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

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
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Ala Médica] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
