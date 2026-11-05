using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 1. Teste dos armários (#29) na cena de protótipo: sorteio perto dos 60% do GDD 6,
    // bateria +30, sedativo com o efeito do Estabilizador, ruído ao abrir e sem saque duplo.
    // Menu: Sombra da Sanidade > Rodar teste dos armários.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.LockerTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class LockerTest
    {
        private const string RunningKey = "SombraLockerTest.Running";
        private const string BatchKey = "SombraLockerTest.Batch";
        private const int Rolls = 4000;

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static readonly List<(Vector2 pos, float radius)> noises = new List<(Vector2, float)>();
        private static bool failed;

        static LockerTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste dos armários", priority = 70)]
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
            Debug.Log("[Armários] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void OnNoise(Vector2 pos, float radius) => noises.Add((pos, radius));

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var lockers = Object.FindObjectsByType<Locker>(FindObjectsSortMode.InstanceID);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem 2 armários, jogador, lanterna e sanidade", lockers.Length >= 2 && player && flashlight && sanity);
                    if (failed) { Finish(); return; }
                    var monster = Object.FindAnyObjectByType<MonsterController>();
                    if (monster != null) monster.gameObject.SetActive(false);
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    player.enabled = false;
                    NoiseBus.NoiseEmitted += OnNoise;
                    noises.Clear();

                    var a = lockers[0];
                    var b = lockers[1];
                    var config = GetConfig(a);
                    Check("Armário fechado pode ser aberto", a.CanInteract && !a.IsOpened);

                    // Sorteio: frequências perto da chance configurada.
                    Random.InitState(29);
                    int nothing = 0, battery = 0, sedative = 0;
                    for (int i = 0; i < Rolls; i++)
                    {
                        switch (a.RollLoot())
                        {
                            case LootKind.Battery: battery++; break;
                            case LootKind.Sedative: sedative++; break;
                            default: nothing++; break;
                        }
                    }
                    float lootRate = (battery + sedative) / (float)Rolls;
                    float batteryRate = battery / (float)Mathf.Max(1, battery + sedative);
                    Check($"Chance de loot perto de {config.lockerLootChance:P0} ({lootRate:P1})", Mathf.Abs(lootRate - config.lockerLootChance) < 0.03f);
                    Check($"Fração de bateria perto de {config.lockerBatteryShare:P0} ({batteryRate:P1})", Mathf.Abs(batteryRate - config.lockerBatteryShare) < 0.04f);

                    // Bateria.
                    flashlight.TrySpend(50f);
                    float before = flashlight.Battery;
                    a.Open(LootKind.Battery);
                    float gained = flashlight.Battery - before;
                    Check($"Bateria +{flashlight.Config.batteryPickupAmount:0} ({gained:0.0})", Mathf.Abs(gained - flashlight.Config.batteryPickupAmount) < 0.5f);
                    Check("Abrir faz barulho no raio do config",
                        noises.Any(n => Vector2.Distance(n.pos, a.transform.position) < 0.01f && Mathf.Approximately(n.radius, config.lockerNoiseRadius)));
                    Check("Armário aberto fica marcado e não pode ser saqueado de novo", a.IsOpened && !a.CanInteract);
                    float again = flashlight.Battery;
                    a.Open(LootKind.Battery);
                    Check("Abrir de novo não dá mais bateria", flashlight.Battery <= again + 0.01f);

                    // Sedativo.
                    var copy = Object.Instantiate(b.gameObject).GetComponent<Locker>();
                    sanity.Add(-50f);
                    float sBefore = sanity.Value;
                    b.Open(LootKind.Sedative);
                    float sGain = sanity.Value - sBefore;
                    Check($"Sedativo dá +{sanity.Config.stabilizerAmount:0} de sanidade ({sGain:0.0})", sGain > sanity.Config.stabilizerAmount - 1f);
                    Check("Sedativo deixa o jogador lento", player.SpeedMultiplier < 0.99f);

                    // Fluxo pelo IInteractable (o que o PlayerInteractor chama ao concluir).
                    copy.CompleteInteract();
                    Check("CompleteInteract abre o armário", copy.IsOpened && !copy.CanInteract);
                    Next();
                    break;

                case 1:
                    if (t < sanity.Config.stabilizerSlowDuration + 0.4f) return;
                    Check($"Lentidão do sedativo acaba ({player.SpeedMultiplier:0.00})", Mathf.Approximately(player.SpeedMultiplier, 1f));
                    Finish();
                    break;
            }
        }

        private static InteractionConfig GetConfig(Locker locker)
        {
            var so = new SerializedObject(locker);
            return (InteractionConfig)so.FindProperty("config").objectReferenceValue;
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
            NoiseBus.NoiseEmitted -= OnNoise;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Armários] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
