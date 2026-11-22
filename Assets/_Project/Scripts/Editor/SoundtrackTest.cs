using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Level;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.EditorTools
{
    // RC4/RC3, issue #103. Confere a trilha fixa ligada no jogo: AudioConfig com as faixas, purga com 150 s,
    // menu tocando a faixa nova no grupo Música, sala segura entrando e saindo com a Zona Segura, Game Over em
    // corte seco, purga acompanhando o tempo da EscapeSequence (inclusive SetRemaining) e laudo na vitória final.
    // Menu: Sombra da Sanidade > Rodar teste da trilha sonora.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SoundtrackTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SoundtrackTest
    {
        private const string RunningKey = "SombraSoundtrackTest.Running";
        private const string BatchKey = "SombraSoundtrackTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static SoundtrackTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da trilha sonora", priority = 73)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            SoundEventBuilder.Build();
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioMixerBuilder.ConfigPath);
            Check("AudioConfig com as 5 faixas fixas", config != null && config.menuMusic && config.safeRoomMusic
                && config.purgeMusic && config.gameOverSting && config.laudoMusic);
            if (config != null && config.purgeMusic != null)
                Check($"Purga com 150 s ({config.purgeMusic.length:0.000})", Mathf.Abs(config.purgeMusic.length - 150f) < 0.05f);

            TestScenes.Remember();
            EditorSceneManager.OpenScene(MenuSceneBuilder.ScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Trilha] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var config = AudioConfig.Load();
            var director = AudioDirector.Instance;
            var gm = GameManager.Instance;

            if (t > 25f) { Check($"Passo {step} terminou a tempo", false); Finish(); return; }

            switch (step)
            {
                case 0:   // Menu
                    if (Time.frameCount < 5 || t < 0.5f) return;
                    AudioSource menu = null;
                    foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                        if (s.loop && s.clip == config.menuMusic) menu = s;
                    Check("Menu toca a faixa nova", menu != null && menu.isPlaying);
                    Check("Faixa do menu no grupo Música", menu != null && menu.outputAudioMixerGroup == AudioService.Group(AudioBus.Music));
                    SceneManager.LoadScene("AlaMedica");
                    Next();
                    break;

                case 1:   // Sala segura
                    if (SceneManager.GetActiveScene().name != "AlaMedica" || director == null || t < 0.5f) return;
                    Object.FindAnyObjectByType<SanityController>().SetInSafeZone(true);
                    Next();
                    break;

                case 2:
                    if (director.FixedTrack != config.safeRoomMusic || director.FixedVolume < 0.95f) return;
                    Check($"Zona Segura: sala segura entra ({t:0.0} s)", director.FixedSource.isPlaying);
                    Object.FindAnyObjectByType<SanityController>().SetInSafeZone(false);
                    Next();
                    break;

                case 3:
                    if (director.FixedTrack != null) return;
                    Check($"Fora da Zona Segura a faixa sai ({t:0.0} s)", true);
                    gm.TriggerGameOver(DeathCause.Monster);
                    Check("Game Over: sting no mesmo quadro (corte seco)", director.FixedTrack == config.gameOverSting
                        && director.FixedSource.isPlaying && Mathf.Approximately(director.FixedVolume, 1f));
                    SceneManager.LoadScene("Eclusa");
                    Next();
                    break;

                case 4:   // Purga
                    if (SceneManager.GetActiveScene().name != "Eclusa" || EscapeSequence.Instance == null || t < 0.5f) return;
                    EscapeSequence.Instance.Begin();
                    Next();
                    break;

                case 5:
                    if (director.FixedTrack != config.purgeMusic || !director.FixedSource.isPlaying) return;
                    Check("Purga começa com a sequência", director.FixedSource.time < 3f);
                    EscapeSequence.Instance.SetRemaining(60f);
                    Next();
                    break;

                case 6:
                    if (t < 0.5f) return;
                    float elapsed = EscapeSequence.Instance.Elapsed, music = director.FixedSource.time;
                    Check($"Purga acompanha o salto de tempo (sequência {elapsed:0.0} s, música {music:0.0} s)", Mathf.Abs(elapsed - music) < 0.8f);
                    gm = GameManager.Instance;
                    for (int i = 0; i < gm.DrivesRequired; i++) gm.CollectDrive();
                    gm.TriggerVictory();
                    Next();
                    break;

                case 7:
                    if (director.FixedTrack != config.laudoMusic || director.FixedVolume < 0.95f) return;
                    Check($"Vitória final: laudo ({t:0.0} s)", director.FixedSource.isPlaying && director.FixedSource.loop);
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
            var sb = new StringBuilder($"[Trilha] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
