using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 1. Teste do loot por ala (#46) na cena de protótipo: sorteio pela tabela da Ala Médica e garantia
    // de uma bateria por ala. Usa cópias das tabelas, para não alterar os assets.
    // Menu: Sombra da Sanidade > Rodar teste do loot por ala.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.LootTableTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class LootTableTest
    {
        private const string RunningKey = "SombraLootTableTest.Running";
        private const string BatchKey = "SombraLootTableTest.Batch";
        private const string LootDir = "Assets/_Project/ScriptableObjects/Config/Loot/";
        private const int Rolls = 4000;

        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static LootTableTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do loot por ala", priority = 73)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Loot por ala] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var lockers = Object.FindObjectsByType<Locker>(FindObjectsSortMode.InstanceID);

            if (Time.frameCount < 5) return;
            Check("Cena tem 2 armários", lockers.Length >= 2);
            if (failed) { Finish(); return; }
            var monster = Object.FindAnyObjectByType<MonsterController>();
            if (monster != null) monster.gameObject.SetActive(false);
            if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
            var a = lockers[0];
            var b = lockers[1];

            var medica = AssetDatabase.LoadAssetAtPath<LootTable>(LootDir + "LootTable_Medica.asset");
            var eclusa = AssetDatabase.LoadAssetAtPath<LootTable>(LootDir + "LootTable_Eclusa.asset");
            Check("Tabelas das 4 alas existem", medica && eclusa
                && AssetDatabase.LoadAssetAtPath<LootTable>(LootDir + "LootTable_Energia.asset")
                && AssetDatabase.LoadAssetAtPath<LootTable>(LootDir + "LootTable_Executiva.asset"));
            if (failed) { Finish(); return; }
            Check("Eclusa sem bateria e sem garantia", eclusa.Chance(LootKind.Battery) == 0f && !eclusa.guaranteeBattery);

            // Sorteio pela tabela (b fechado: a garantia não entra).
            var table = Object.Instantiate(medica);
            a.SetTable(table);
            b.SetTable(table);
            float rate = BatteryRate(a);
            float expected = table.Chance(LootKind.Battery);
            Check($"Bateria sai perto de {expected:P0} na Ala Médica ({rate:P1})", Mathf.Abs(rate - expected) < 0.03f);

            // Garantia: tabela que nunca dá bateria ainda entrega uma no último armário da ala.
            var never = Object.Instantiate(medica);
            never.batteryWeight = 0f;
            never.sedativeWeight = 0f;
            never.adrenaSynWeight = 0f;
            never.nothingWeight = 1f;
            never.guaranteeBattery = true;
            a.SetTable(never);
            b.SetTable(never);
            a.CompleteInteract();
            Check($"Primeiro armário sai vazio ({a.Loot})", a.Loot == LootKind.Nothing);
            b.CompleteInteract();
            Check($"Último armário da ala garante bateria ({b.Loot})", b.Loot == LootKind.Battery);
            Finish();
        }

        private static float BatteryRate(Locker locker)
        {
            Random.InitState(46);
            int battery = 0;
            for (int i = 0; i < Rolls; i++)
                if (locker.RollLoot() == LootKind.Battery) battery++;
            return battery / (float)Rolls;
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
            var sb = new StringBuilder($"[Loot por ala] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
