using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // #89 na Ala Médica: as falas do Thomas tocam uma vez com a versão da faixa de sanidade, legenda "THOMAS" e
    // bipe uma oitava abaixo, sem fita; os objetos trocam de sprite abaixo de 35; o crachá da pausa existe.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.InnerVoiceTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class InnerVoiceTest
    {
        private const string RunningKey = "SombraInnerVoiceTest.Running";
        private const string BatchKey = "SombraInnerVoiceTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static InnerVoiceTest()
        {
            if (SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Narrativa/Rodar teste das falas do Thomas", priority = 43)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;
            InnerVoiceAssetBuilder.Build();
            CheckAssets();
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void CheckAssets()
        {
            Check($"{InnerVoiceAssetBuilder.Count} falas do roteiro (18 + 2 da Eclusa)", InnerVoiceAssetBuilder.Count == 20);
            foreach (var wing in new[] { "Medica", "Energia", "Executiva" })
            {
                var lines = InnerVoiceAssetBuilder.ForWing(wing);
                Check($"{wing}: 6 falas com versão Estável e Abalado", lines.Length == 6
                    && lines.All(l => l.stable.Length > 0 && l.shaken.Length > 0));
            }
            var line = InnerVoiceAssetBuilder.ForWing("Medica")[0];
            var log = line.ToLog(20f);
            Check("Fala vira fita sem chiado, THOMAS, uma oitava abaixo da Vance", !log.tape && log.speaker == "THOMAS"
                && Mathf.Approximately(log.babblePitch * 2f, 1.15f));
            Check("Sanidade < 40 usa a versão Abalado", log.lines[0] == line.shaken && line.ToLog(80f).lines[0] == line.stable);
            var badge = Resources.Load<ThomasBadge>(ThomasBadge.ResourcePath);
            Check("Crachá da pausa com as duas versões", badge != null && badge.normal != null && badge.shaken != null);
            foreach (var n in new[] { "espelho", "foto", "pulseira", "prontuario", "cracha" })
                Check($"Par de sprites sanity_{n}", InnerVoiceAssetBuilder.SanitySprites(n).All(s => s != null));
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var director = InnerVoiceDirector.Instance;
            var logs = AudioLogPlayer.Instance;
            var sanity = Object.FindAnyObjectByType<SanityController>();

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    // A ala abre com a C0: pula para a fala de início tocar.
                    if (CutscenePlayer.IsPlaying) { CutscenePlayer.Skip(); return; }
                    if (t < 1f) return;
                    Check("Ala tem o diretor com as 6 falas da Médica", director != null && director.Lines.Count == 6);
                    Check("Ala tem o player de falas", logs != null);
                    if (failed) { Finish(); return; }
                    Check($"T1-1 toca no começo da ala ({logs.Current?.id})", director.HasPlayed("T1-1")
                        && (logs.Current == null || logs.Current.id == "T1-1"));
                    Check("A fala do Thomas não entra no arquivo de logs", logs.Current == null || !AudioLogArchive.HasHeard(logs.Current));
                    logs.Stop();
                    sanity.Add(-sanity.Value + 20f);
                    director.Fire(InnerVoiceTrigger.FirstBreaker);
                    Check($"T1-4 com sanidade 20 usa a versão Abalado (\"{logs.Current?.lines[0]}\")",
                        logs.Current != null && logs.Current.lines[0] == InnerVoiceAssetBuilder.ForWing("Medica")[3].shaken);
                    logs.Stop();
                    director.Fire(InnerVoiceTrigger.FirstBreaker);
                    Check("Cada fala toca uma vez", logs.Current == null);
                    Next();
                    break;

                case 1:
                    if (t < 1.2f) return;
                    var objects = Object.FindObjectsByType<SanityObject>();
                    Check($"Objetos da Médica (pulseira e prontuário): {objects.Length}", objects.Length == 2);
                    Check("Com sanidade 20 os objetos mostram a versão abalada", objects.All(o => o.IsShaken));
                    Check("Crachá abalado com sanidade baixa", ThomasBadge.IsShaken());
                    sanity.Add(100f);
                    Next();
                    break;

                case 2:
                    if (t < 1.2f) return;
                    Check("Sanidade de volta: objetos normais", Object.FindObjectsByType<SanityObject>().All(o => !o.IsShaken));
                    Finish();
                    break;
            }
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
            var sb = new StringBuilder($"[Falas do Thomas] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
