using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 1. Teste do ajuste dinâmico de bateria (#47) na cena de protótipo: bateria baixa por um tempo
    // sobe a chance de bateria do armário; achar bateria desliga. Usa uma cópia do InteractionConfig com
    // atraso curto, para não alterar o asset.
    // Menu: Sombra da Sanidade > Rodar teste do ajuste de bateria.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.BatteryPityTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class BatteryPityTest
    {
        private const string RunningKey = "SombraBatteryPityTest.Running";
        private const string BatchKey = "SombraBatteryPityTest.Batch";
        private const float TestDelay = 1f;
        private const int Rolls = 4000;

        private static int step;
        private static float stepStart;
        private static float baseRate;
        private static InteractionConfig testConfig;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static BatteryPityTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do ajuste de bateria", priority = 72)]
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
            Debug.Log("[Ajuste de bateria] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var locker = Object.FindAnyObjectByType<Locker>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var pity = flashlight != null ? flashlight.GetComponent<BatteryPity>() : null;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem armário e lanterna, e o armário criou o BatteryPity", locker && flashlight && pity);
                    if (failed) { Finish(); return; }
                    var monster = Object.FindAnyObjectByType<MonsterController>();
                    if (monster != null) monster.gameObject.SetActive(false);
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;

                    var original = (InteractionConfig)GetField(locker, "config");
                    testConfig = Object.Instantiate(original);
                    testConfig.pityDelay = TestDelay;
                    SetField(locker, "config", testConfig);
                    SetField(pity, "config", testConfig);

                    baseRate = BatteryRate(locker);
                    float expectedBase = testConfig.lockerLootChance * testConfig.lockerBatteryShare;
                    Check($"Chance normal de bateria perto de {expectedBase:P0} ({baseRate:P1})", Mathf.Abs(baseRate - expectedBase) < 0.03f);

                    flashlight.TrySpend(flashlight.Battery - 10f);
                    Check("Ajuste começa desligado", !pity.IsActive);
                    Next();
                    break;

                case 1:
                    if (t < TestDelay + 0.3f) return;
                    Check($"Liga depois de {TestDelay:0} s com bateria < {testConfig.pityBatteryBelow:0}", pity.IsActive);
                    float boosted = BatteryRate(locker);
                    float expected = baseRate + testConfig.pityBonus;
                    Check($"Chance de bateria sobe {testConfig.pityBonus * 100f:0} pontos ({boosted:P1})", Mathf.Abs(boosted - expected) < 0.03f);

                    locker.Open(LootKind.Battery);
                    Check("Achar bateria desliga o ajuste", !pity.IsActive && pity.BonusChance == 0f);
                    Next();
                    break;

                case 2:
                    if (t < TestDelay + 0.3f) return;
                    Check($"Bateria acima do limiar não religa ({flashlight.Battery:0})", !pity.IsActive);
                    Finish();
                    break;
            }
        }

        private static float BatteryRate(Locker locker)
        {
            Random.InitState(47);
            int battery = 0;
            for (int i = 0; i < Rolls; i++)
                if (locker.RollLoot() == LootKind.Battery) battery++;
            return battery / (float)Rolls;
        }

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

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
            var sb = new StringBuilder($"[Ajuste de bateria] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
