using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Ala Executiva (issue #43): perfil de dificuldade da Proposta 1, monstro presente desde a entrada,
    // pistas de senha só com a sanidade baixa (ou com o Módulo Neural), trava de senha do HD-03 e checkpoint.
    // Menu: Sombra da Sanidade > Rodar teste da Ala Executiva.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AlaExecutivaTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class AlaExecutivaTest
    {
        private const string RunningKey = "SombraAlaExecutivaTest.Running";
        private const string BatchKey = "SombraAlaExecutivaTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;
        private static float sanityBefore;
        private static int noises;

        static AlaExecutivaTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da Ala Executiva", priority = 67)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaExecutivaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Ala Executiva] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            // Include: o teste desativa o monstro no meio e ainda lê a velocidade dele no fim.
            var monster = Object.FindAnyObjectByType<MonsterController>(FindObjectsInactive.Include);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            var difficulty = Object.FindAnyObjectByType<WingDifficulty>();
            var keypad = Object.FindAnyObjectByType<KeypadLock>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro, jogador, sanidade, perfil, teclado, checkpoint e GameManager",
                        monster && player && sanity && difficulty && keypad && WingCheckpoint.Instance && gm);
                    if (failed) { Finish(); return; }
                    CheckProfile(difficulty, monster, sanity);
                    CheckLayout(gm, keypad);
                    Next();
                    break;

                case 1:
                    // As pistas começam escondidas: sanidade cheia. O Módulo Neural revela por alguns segundos.
                    var clues = Object.FindObjectsByType<CodeClue>();
                    Check("Pistas de senha escondidas com a sanidade cheia",
                        clues.All(c => !c.GetComponent<SanityClue>().IsRevealed) && !EntranceClue().IsRevealed);
                    Check("Cada pista mostra a posição do seu dígito (\"SENHA _ 7 _ _\")", clues.All(c => c.Text == Expected(keypad, c.Index)));
                    player.enabled = false;
                    monster.gameObject.SetActive(false);   // comportamento do monstro tem teste próprio; aqui ele atrapalharia
                    sanityBefore = sanity.Value;
                    var module = Object.FindObjectsByType<NeuralModule>().First();
                    module.CompleteInteract();
                    Check($"Módulo Neural custa sanidade ({sanityBefore:0} -> {sanity.Value:0})",
                        Mathf.Approximately(sanityBefore - sanity.Value, sanity.Config.neuralModuleCost) && module.Used);
                    Next();
                    break;

                case 2:
                    if (t < 1.2f) return;
                    Check("Com o Módulo Neural as pistas aparecem com qualquer sanidade",
                        sanity.IsRevealForced && Object.FindObjectsByType<CodeClue>().All(c => c.GetComponent<SanityClue>().IsRevealed));
                    Check("A pista da entrada também aparece", EntranceClue().IsRevealed);

                    // Teclado: senha errada faz barulho, a certa libera o servidor.
                    Teleport(player, keypad.transform.position);
                    noises = 0;
                    NoiseBus.NoiseEmitted += CountNoise;
                    keypad.CompleteInteract();
                    Check("E abre o teclado", keypad.IsOpen);
                    string wrong = new string(keypad.Code.Select(c => c == '9' ? '0' : (char)(c + 1)).ToArray());
                    foreach (char c in wrong) keypad.Type(c);
                    bool ok = keypad.Submit();
                    Check("Senha errada não libera e faz barulho", !ok && keypad.WrongAttempts == 1 && noises > 0 && keypad.Server.IsLocked);
                    foreach (char c in keypad.Code) keypad.Type(c);
                    keypad.Erase();
                    keypad.Type(keypad.Code[keypad.Code.Length - 1]);
                    ok = keypad.Submit();
                    NoiseBus.NoiseEmitted -= CountNoise;
                    Check("Senha certa libera o servidor do HD-03", ok && keypad.IsUnlocked && !keypad.Server.IsLocked && keypad.Server.CanInteract);
                    Next();
                    break;

                case 3:
                    if (t < 0.2f) return;
                    keypad.Server.CompleteInteract();
                    Check($"HD-03 completa os 3 HDs ({gm.DrivesCollected}/{gm.DrivesRequired})", gm.DrivesCollected == 3 && gm.HasAllDrives);
                    Check($"HD desta ala acelera +10% sobre a velocidade da ala (x{monster.SpeedMultiplier:0.00})",
                        Mathf.Approximately(monster.SpeedMultiplier, 1f + monster.Config.speedBonusPerDrive));
                    var terminal = player.GetComponent<UpgradeTerminal>();
                    Check("Escolha de upgrade é a do HD-03", terminal != null && terminal.DriveNumber == 3);
                    if (terminal != null) terminal.Choose(0);
                    CheckCheckpoint(gm);
                    Finish();
                    break;
            }
        }

        // Proposta 1 do v1.1, coluna da Ala Executiva.
        private static void CheckProfile(WingDifficulty difficulty, MonsterController monster, SanityController sanity)
        {
            var p = difficulty.Profile;
            Check("Perfil da Executiva aplicado", p != null && p.wing == "Executiva" && difficulty.Applied && WingDifficulty.Active == p);
            if (p == null) return;
            Check($"Tabela: velocidade 4.2, raio 7, dreno 2.0, bateria 45%, alucinação 15-25 s, 2 disjuntores",
                Mathf.Approximately(p.monsterSpeed, 4.2f) && Mathf.Approximately(p.hearingRadius, 7f)
                && Mathf.Approximately(p.darknessDrain, 2f) && Mathf.Approximately(p.batteryChance, 0.45f)
                && p.hallucinations && p.hallucinationInterval == new Vector2(15f, 25f) && p.breakers == 2
                && p.monsterDelaySeconds == 0f);
            Check($"Monstro com a velocidade e o raio da ala ({monster.BaseSpeed} / {monster.HearingRadius})",
                Mathf.Approximately(monster.BaseSpeed, 4.2f) && Mathf.Approximately(monster.HearingRadius, 7f));
            Check($"Monstro com os comportamentos da Executiva ({monster.Behaviours})", monster.Behaviours == MonsterWingBehaviours.Executiva);
            Check("Monstro presente desde a entrada (ativo e visível)", monster.isActiveAndEnabled
                && monster.GetComponentsInChildren<Renderer>().Any(r => r.enabled));
            Check($"Dreno no escuro da ala ({sanity.DarknessDrain}/s)", Mathf.Approximately(sanity.DarknessDrain, 2f));
            var hallucination = Object.FindAnyObjectByType<SanityHallucination>();
            Check("Alucinações a cada 15-25 s", hallucination && hallucination.Allowed && hallucination.Interval == new Vector2(15f, 25f));
            var lockers = Object.FindObjectsByType<Locker>();
            Check($"Armários ({lockers.Length}) com a tabela da ala e 45% de bateria", lockers.Length > 0 && p.lootTable != null
                && lockers.All(l => l.Table == p.lootTable) && Mathf.Approximately(p.lootTable.Chance(LootKind.Battery), p.batteryChance));
            int breakers = Object.FindObjectsByType<Breaker>().Length;
            Check($"Disjuntores na planta = perfil ({breakers})", breakers == p.breakers);
        }

        private static void CheckLayout(GameManager gm, KeypadLock keypad)
        {
            Check($"Começa com os 2 HDs das alas anteriores ({gm.DrivesCollected}/{gm.DrivesRequired})",
                gm.StartingDrives == 2 && gm.DrivesCollected == 2 && gm.DrivesRequired == 3);
            var servers = Object.FindObjectsByType<DriveServer>();
            Check("Um servidor (HD-03), travado por senha", servers.Length == 1 && servers[0].IsLocked && !servers[0].CanInteract
                && keypad.Server == servers[0]);
            int clues = Object.FindObjectsByType<CodeClue>().Length;
            Check($"Senha de {keypad.Code.Length} dígitos com uma pista por dígito ({clues})", keypad.Code.Length == 4 && clues == 4);
            Check("Pista em fósforo perto da entrada (Proposta 4)", EntranceClue() != null);
            Check("Módulos Neurais na ala", Object.FindObjectsByType<NeuralModule>().Length >= 2);
            Check("Porta de saída presente", Object.FindAnyObjectByType<ExitDoor>() != null);
        }

        // #45: concluir a ala salva o checkpoint (pasta temporária, não mexe no save de quem joga).
        private static void CheckCheckpoint(GameManager gm)
        {
            string dir = Path.Combine(Path.GetTempPath(), "sombra-checkpoint-test");
            Checkpoint.PathOverride = Path.Combine(dir, "checkpoint.json");
            try
            {
                Checkpoint.Clear();
                gm.TriggerVictory();
                var wing = WingCheckpoint.Instance;
                Check("Concluir a ala salva o checkpoint (Executiva -> Eclusa, 3 HDs)", gm.State == GameState.Victory && wing.Saved
                    && Checkpoint.TryLoad(out var data) && data.lastWing == "Executiva" && data.nextWing == "Eclusa" && data.drives == 3);
                Check($"Tela de vitória mostra \"{wing.StatusLine}\"", wing.StatusLine == "PROGRESSO SALVO");
            }
            finally
            {
                Checkpoint.Clear();
                Checkpoint.PathOverride = null;
            }
        }

        private static SanityClue EntranceClue()
        {
            var go = GameObject.Find("Clue_Entrada");
            return go != null ? go.GetComponent<SanityClue>() : null;
        }

        private static string Expected(KeypadLock keypad, int index)
        {
            var parts = new List<string>();
            for (int i = 0; i < keypad.Code.Length; i++) parts.Add(i == index ? keypad.Code[i].ToString() : "_");
            return "SENHA " + string.Join(" ", parts);
        }

        private static void CountNoise(Vector2 position, float radius) => noises++;

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
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Ala Executiva] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
