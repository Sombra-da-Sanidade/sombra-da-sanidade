using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 1. Teste dos upgrades da Lux-90 (#31) na cena de protótipo: cada HD abre a escolha com o jogo
    // pausado, a escolha retoma o jogo e cada um dos 6 efeitos muda o valor certo (sem alterar os assets).
    // Menu: Sombra da Sanidade > Rodar teste dos upgrades da Lux-90.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.UpgradeTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class UpgradeTest
    {
        private const string RunningKey = "SombraUpgradeTest.Running";
        private const string BatchKey = "SombraUpgradeTest.Batch";

        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static UpgradeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste dos upgrades da Lux-90", priority = 75)]
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
            Debug.Log("[Upgrades] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || Time.frameCount < 5) return;
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            var terminal = Object.FindAnyObjectByType<UpgradeTerminal>();
            var servers = Object.FindObjectsByType<DriveServer>(FindObjectsSortMode.InstanceID);
            Check("Cena tem lanterna, sanidade, terminal e 3 servidores", flashlight && sanity && terminal && servers.Length >= 3);
            if (failed) { Finish(); return; }
            var monster = Object.FindAnyObjectByType<MonsterController>();
            if (monster != null) monster.gameObject.SetActive(false);
            if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;

            var config = (InteractionConfig)new SerializedObject(servers[0]).FindProperty("config").objectReferenceValue;
            var flashlightConfig = (FlashlightConfig)new SerializedObject(flashlight).FindProperty("config").objectReferenceValue;
            var tree = config.upgradeTree;
            Check("InteractionConfig aponta para a árvore com 3 escolhas", tree && tree.ForDrive(1) != null && tree.ForDrive(2) != null && tree.ForDrive(3) != null);
            if (failed) { Finish(); return; }
            float baseRange = flashlight.Range;
            float baseStun = flashlight.EmpStunDuration;

            // HD-01: Célula Extra (opção B).
            servers[0].CompleteInteract();
            Check("Ejetar o HD-01 abre o terminal e pausa o jogo", UpgradeTerminal.IsOpen && Time.timeScale == 0f && terminal.DriveNumber == 1);
            Check("Terminal oferece Lente Focal ou Célula Extra",
                terminal.Current != null && terminal.Current.optionA.effect == LuxUpgradeEffect.RangeMultiplier
                && terminal.Current.optionB.effect == LuxUpgradeEffect.MaxBattery);
            flashlight.TrySpend(50f);
            float before = flashlight.Battery;
            terminal.Choose(1);
            Check("Escolher fecha o terminal e retoma o jogo", !UpgradeTerminal.IsOpen && Time.timeScale > 0f);
            Check($"Célula Extra: bateria máx. 130 e +30 de carga ({flashlight.MaxBattery:0}, {before:0} -> {flashlight.Battery:0})",
                Mathf.Approximately(flashlight.MaxBattery, 130f) && flashlight.Battery > before + 29f);
            flashlight.Recharge(200f);
            Check("Recarga vai até 130", Mathf.Approximately(flashlight.Battery, 130f));

            // HD-02: Capacitor Duplo (opção A).
            servers[1].CompleteInteract();
            Check("HD-02 oferece Capacitor Duplo ou Bobina Longa", terminal.DriveNumber == 2 && terminal.Current != null
                && terminal.Current.optionA.effect == LuxUpgradeEffect.EmpCost && terminal.Current.optionB.effect == LuxUpgradeEffect.StunDuration);
            terminal.Choose(0);
            Check($"Capacitor Duplo: EMP custa 30 ({flashlight.EmpCost:0})", Mathf.Approximately(flashlight.EmpCost, 30f));

            // HD-03: Filtro Âmbar (opção A).
            servers[2].CompleteInteract();
            Check("HD-03 oferece Filtro Âmbar ou Ressonador Neural", terminal.DriveNumber == 3 && terminal.Current != null
                && terminal.Current.optionA.effect == LuxUpgradeEffect.AmberFilter && terminal.Current.optionB.effect == LuxUpgradeEffect.NeuralResonator);
            terminal.Choose(0);
            Check("Com o filtro, fora do modo econômico a luz ainda atrai", flashlight.HasAmberFilter && flashlight.AttractsMonster);
            flashlight.SetEcoMode(true);
            Check("Filtro Âmbar: modo econômico ilumina sem atrair o monstro", flashlight.IsEcoMode && flashlight.IsLit && !flashlight.AttractsMonster);
            flashlight.SetEcoMode(false);
            Check("Três upgrades registrados", flashlight.Installed.Count == 3);
            Check("Sem escolha para um 4º HD", !terminal.Open(tree, 4));

            // As outras três opções, aplicadas direto.
            LuxUpgrades.Apply(tree.ForDrive(1).optionA, flashlight, sanity);
            Check($"Lente Focal: alcance +25% ({baseRange:0.0} -> {flashlight.Range:0.0})", Mathf.Approximately(flashlight.Range, baseRange * 1.25f));
            LuxUpgrades.Apply(tree.ForDrive(2).optionB, flashlight, sanity);
            Check($"Bobina Longa: stun 5,5 s ({baseStun:0.0} -> {flashlight.EmpStunDuration:0.0})", Mathf.Approximately(flashlight.EmpStunDuration, 5.5f));
            LuxUpgrades.Apply(tree.ForDrive(3).optionB, flashlight, sanity);
            Check($"Ressonador Neural: pistas abaixo de 50 ({sanity.RevealCluesBelow:0})", Mathf.Approximately(sanity.RevealCluesBelow, 50f));
            Check("Assets de config intactos (EMP 40, bateria 100, alcance 8)",
                Mathf.Approximately(flashlightConfig.empCost, 40f) && Mathf.Approximately(flashlightConfig.maxBattery, 100f)
                && Mathf.Approximately(flashlightConfig.range, 8f) && Mathf.Approximately(sanity.Config.revealCluesBelow, 35f));
            Finish();
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
            var sb = new StringBuilder($"[Upgrades] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
