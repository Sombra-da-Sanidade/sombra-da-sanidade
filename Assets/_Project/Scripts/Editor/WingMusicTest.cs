using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5, issue #104. Confere a música por fase: as 3 alas e a Eclusa com MusicSet de 4 stems do mesmo tamanho no
    // AudioConfig; no Play da Ala Médica, a MusicLayers criada pelo AudioDirector tocando em sincronia, com
    // L0 e L1 na exploração e corte seco no Game Over; na Eclusa, a vitória final com o jingle sozinho e o laudo depois.
    // Menu: Sombra da Sanidade > Rodar teste da música por fase.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.WingMusicTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class WingMusicTest
    {
        private const string RunningKey = "SombraWingMusicTest.Running";
        private const string BatchKey = "SombraWingMusicTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static WingMusicTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da música por fase", priority = 76)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioMixerBuilder.ConfigPath);
            Check("AudioConfig existe", config != null);
            if (config != null)
            {
                foreach (var wing in new[] { "Medica", "Energia", "Executiva", "Eclusa" })
                {
                    var set = config.WingMusic(wing);
                    bool four = set != null && set.Count == 4 && set.layers.All(c => c != null);
                    Check($"{wing}: MusicSet com 4 stems", four);
                    if (four)
                        Check($"{wing}: stems com o mesmo tamanho (loop em sincronia)",
                            set.layers.Select(c => c.samples).Distinct().Count() == 1);
                }
                var all = new[] { config.medicaMusic, config.energiaMusic, config.executivaMusic, config.eclusaMusic };
                Check("As 4 fases (3 alas e a Eclusa) têm músicas diferentes", all.Distinct().Count() == 4);
            }

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            stepStart = 0f;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Música por fase] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            // Segunda parte: saiu do Play da Ala Médica, abre a Eclusa (última fase) e dá Play de novo.
            if (step == 2 && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene(AlfaSceneBuilder.EclusaScenePath);
                Next();
                EditorApplication.EnterPlaymode();
                return;
            }
            if (!EditorApplication.isPlaying) return;
            if (stepStart <= 0f) stepStart = Time.realtimeSinceStartup;
            float t = Time.realtimeSinceStartup - stepStart;
            var gm = GameManager.Instance;

            switch (step)
            {
                case 0:   // Começo da ala: exploração (L0 + L1).
                {
                    var music = Object.FindAnyObjectByType<MusicLayers>();
                    bool ready = gm != null && music != null && music.Started && music.LayerVolume(1) > 0.5f;
                    if (!ready && t < 10f) return;
                    var config = AudioConfig.Load();
                    Check("Ala Médica cria a MusicLayers com a música da ala",
                        music != null && music.Set == config.medicaMusic);
                    if (music == null || !music.Started) { Finish(); return; }
                    Check("4 camadas tocando no grupo Música",
                        music.LayerCount == 4 && Enumerable.Range(0, 4).All(i => music.LayerSource(i).isPlaying
                            && music.LayerSource(i).outputAudioMixerGroup == AudioService.Group(AudioBus.Music)));
                    int[] at = Enumerable.Range(0, 4).Select(i => music.LayerSource(i).timeSamples).ToArray();
                    Check($"Camadas em sincronia ({at.Max() - at.Min()} amostras)", at.Max() - at.Min() < 2048);
                    Check("Exploração: L0 e L1 ligadas, L2 e L3 mudas",
                        music.LayerVolume(0) > 0.5f && music.LayerVolume(1) > 0.5f
                        && music.LayerVolume(2) == 0f && music.LayerVolume(3) == 0f);
                    gm.TriggerGameOver(DeathCause.Monster);
                    Next();
                    break;
                }

                case 1:   // Game Over: corte seco.
                {
                    if (t < 0.2f) return;
                    var music = Object.FindAnyObjectByType<MusicLayers>();
                    Check("Game Over silencia a música da fase",
                        music.Muted && Enumerable.Range(0, 4).All(i => music.LayerSource(i).volume == 0f));
                    Next();
                    EditorApplication.ExitPlaymode();
                    break;
                }

                case 3:   // Eclusa: música própria; vitória final com o jingle sozinho e o laudo depois.
                {
                    var music = Object.FindAnyObjectByType<MusicLayers>();
                    if ((gm == null || music == null || !music.Started || AudioDirector.Instance == null) && t < 10f) return;
                    if (t < 1f) return;
                    Check("Eclusa toca a música dela", music != null && music.Set == AudioConfig.Load().eclusaMusic);
                    if (gm == null) { Finish(); return; }
                    while (!gm.HasAllDrives) gm.CollectDrive();
                    gm.TriggerVictory();
                    Next();
                    break;
                }

                case 4:   // Durante o jingle de vitória: sem laudo e sem música da fase.
                {
                    if (t < 1f) return;
                    var director = AudioDirector.Instance;
                    var music = Object.FindAnyObjectByType<MusicLayers>();
                    var config = AudioConfig.Load();
                    Check("Vitória: a música cai para só o ambiente (intensidade 0)", director.TargetIntensity == 0);
                    Check("Vitória final: o jingle toca sozinho (sem laudo e música da fase muda)",
                        director.FixedTrack != config.laudoMusic && music != null && music.Muted);
                    Next();
                    break;
                }

                case 5:   // Depois do jingle entra o laudo.
                {
                    var director = AudioDirector.Instance;
                    var config = AudioConfig.Load();
                    bool laudo = director.FixedTrack == config.laudoMusic && director.FixedVolume > 0.3f;
                    if (!laudo && t < config.laudoDelay + 3f) return;
                    Check("Depois do jingle entra o laudo", laudo);
                    Finish();
                    break;
                }
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
            var sb = new StringBuilder($"[Música por fase] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
