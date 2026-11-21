using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace SombraDaSanidade.EditorTools
{
    // RC4, issue #101. Testa o sistema de áudio na Ala Médica: mixer com os grupos e volumes expostos, volume do
    // jogador em dB, SoundEvent (tomada sem repetir, pitch, grupo, ruído no NoiseBus), fontes dos sistemas ligadas
    // ao mixer, MusicLayers (camadas juntas e por intensidade), AudioDirector (Zona Segura zera a música) e
    // AudioOcclusion (parede troca para a versão abafada).
    // Menu: Sombra da Sanidade > Rodar teste do sistema de áudio.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AudioSystemTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class AudioSystemTest
    {
        private const string RunningKey = "SombraAudioSystemTest.Running";
        private const string BatchKey = "SombraAudioSystemTest.Batch";

        private static int step;
        private static float stepStart;
        private static MusicLayers music;
        private static AudioOcclusion occlusion;
        private static GameObject wall;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static AudioSystemTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do sistema de áudio", priority = 71)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerBuilder.MixerPath);
            Check("SombraMixer existe em Resources", mixer != null);
            if (mixer != null)
                foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
                    Check($"Grupo {bus} com volume exposto", mixer.FindMatchingGroups(bus.ToString()).Length > 0
                        && mixer.GetFloat(AudioService.VolumeParameter(bus), out _));
            Check("AudioConfig existe em Resources", AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioMixerBuilder.ConfigPath) != null);

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Áudio] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var sanity = Object.FindAnyObjectByType<SanityController>();

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5 || GameManager.Instance == null) return;
                    Check("AudioDirector criado na cena de jogo", AudioDirector.Instance != null);

                    AudioService.SetUserVolume(AudioBus.Music, 0.5f);
                    bool got = AudioService.Mixer.GetFloat(AudioService.VolumeParameter(AudioBus.Music), out float db);
                    Check($"Volume 50% da música vira -6 dB no mixer ({db:0.0})", got && Mathf.Abs(db + 6.02f) < 0.1f);
                    AudioService.SetUserVolume(AudioBus.Music, 1f);
                    AudioService.SetDuck(AudioBus.Ambience, -6f);
                    AudioService.Mixer.GetFloat(AudioService.VolumeParameter(AudioBus.Ambience), out db);
                    Check($"Ducking de -6 dB no ambiente ({db:0.0})", Mathf.Abs(db + 6f) < 0.1f);
                    AudioService.SetDuck(AudioBus.Ambience, 0f);

                    // SoundEvent com 3 tomadas.
                    var sound = ScriptableObject.CreateInstance<SoundEvent>();
                    sound.clips = new[] { Tone("a", 330f), Tone("b", 440f), Tone("c", 550f) };
                    sound.pitch = new Vector2(0.9f, 1.1f);
                    sound.noiseRadius = 5f;
                    Vector2 heard = Vector2.zero;
                    float heardRadius = 0f;
                    void OnNoise(Vector2 p, float r) { heard = p; heardRadius = r; }
                    NoiseBus.NoiseEmitted += OnNoise;
                    AudioClip last = null;
                    bool repeated = false, pitchOk = true, groupOk = true;
                    for (int i = 0; i < 12; i++)
                    {
                        var s = AudioService.Play(sound, new Vector2(3f, 4f));
                        if (s == null) { groupOk = false; break; }
                        if (s.clip == last) repeated = true;
                        last = s.clip;
                        pitchOk &= s.pitch >= 0.9f && s.pitch <= 1.1f;
                        groupOk &= s.outputAudioMixerGroup == AudioService.Group(AudioBus.Sfx);
                    }
                    NoiseBus.NoiseEmitted -= OnNoise;
                    Check("SoundEvent não repete a mesma tomada seguida", !repeated);
                    Check("Pitch sorteado dentro da faixa", pitchOk);
                    Check("Som toca no grupo SFX", groupOk);
                    Check("Som com raio de ruído avisa o NoiseBus", heard == new Vector2(3f, 4f) && Mathf.Approximately(heardRadius, 5f));

                    // Fontes dos sistemas (batimento, logs, sirene, HUD) ligadas ao mixer.
                    var loose = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
                        .Where(a => a.outputAudioMixerGroup == null && a.transform.root.name != "AudioService").ToArray();
                    Check($"Toda fonte da cena passa pelo mixer ({string.Join(", ", loose.Select(a => a.name))})", loose.Length == 0);

                    // Música em camadas.
                    var set = ScriptableObject.CreateInstance<MusicSet>();
                    set.layers = new[] { Tone("L0", 110f, 2f), Tone("L1", 165f, 2f), Tone("L2", 220f, 2f), Tone("L3", 275f, 2f) };
                    var go = new GameObject("TestMusic");
                    music = go.AddComponent<MusicLayers>();
                    music.SetMusic(set);
                    music.Begin();
                    music.SetIntensity(1);
                    Next();
                    break;

                case 1:
                    if (t < 1.5f) return;
                    Check("4 camadas tocando", music.LayerCount == 4 && Enumerable.Range(0, 4).All(i => music.LayerSource(i).isPlaying));
                    float span = Enumerable.Range(0, 4).Max(i => music.LayerSource(i).time) - Enumerable.Range(0, 4).Min(i => music.LayerSource(i).time);
                    Check($"Camadas juntas ({span * 1000f:0.0} ms)", span < 0.05f);
                    Check("Intensidade 1: L0 e L1 soam, L2 e L3 não",
                        music.LayerVolume(0) > 0.1f && music.LayerVolume(1) > 0.1f && music.LayerVolume(2) == 0f && music.LayerVolume(3) == 0f);
                    Check("Camadas no grupo Música", music.LayerSource(0).outputAudioMixerGroup == AudioService.Group(AudioBus.Music));
                    music.SetIntensity(3);
                    Next();
                    break;

                case 2:
                    if (t < 0.5f) return;
                    Check($"Intensidade 3: L3 entra rápido ({music.LayerVolume(3):0.00})", music.LayerVolume(3) > 0.7f);
                    if (sanity != null) sanity.SetInSafeZone(true);
                    Next();
                    break;

                case 3:
                    if (t < 0.3f) return;
                    Check("Zona Segura: AudioDirector pede música só com L0", AudioDirector.Instance.TargetIntensity == 0);
                    if (sanity != null) sanity.SetInSafeZone(false);

                    // Oclusão: fonte no ouvinte (livre) e depois atrás de uma parede de teste.
                    var listener = Object.FindAnyObjectByType<AudioListener>();
                    Vector2 at = listener.transform.position;
                    var loop = ScriptableObject.CreateInstance<SoundEvent>();
                    loop.clips = new[] { Tone("dry", 200f, 2f) };
                    loop.muffledClips = new[] { Tone("muffled", 100f, 2f) };
                    loop.bus = AudioBus.Monster;
                    var src = new GameObject("TestOcclusion");
                    src.transform.position = at;
                    occlusion = src.AddComponent<AudioOcclusion>();
                    occlusion.Setup(loop, 1f);
                    wall = new GameObject("TestWall") { layer = LayerMask.NameToLayer("Walls") };
                    wall.transform.position = at + new Vector2(30f, 0f);
                    wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 6f);
                    Next();
                    break;

                case 4:
                    if (t < 0.6f) return;
                    Check("Sem parede: versão limpa", !occlusion.Occluded && occlusion.Dry.volume > 0.9f);
                    var at2 = (Vector2)Object.FindAnyObjectByType<AudioListener>().transform.position;
                    wall.transform.position = at2 + new Vector2(1.5f, 0f);
                    occlusion.transform.position = at2 + new Vector2(3f, 0f);
                    Next();
                    break;

                case 5:
                    if (t < 0.8f) return;
                    Check("Com parede: troca para a versão abafada", occlusion.Occluded && occlusion.Mix > 0.95f
                        && occlusion.Dry.volume < 0.05f && occlusion.Muffled.volume > 0.3f);
                    Check("Fonte do monstro no grupo Monster", occlusion.Dry.outputAudioMixerGroup == AudioService.Group(AudioBus.Monster));
                    Finish();
                    break;
            }
        }

        private static AudioClip Tone(string name, float freq, float seconds = 0.2f)
        {
            int rate = 44100, n = (int)(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * 0.05f;
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
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
            var sb = new StringBuilder($"[Áudio] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
