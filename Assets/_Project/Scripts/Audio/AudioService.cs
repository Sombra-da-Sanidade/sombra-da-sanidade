using System.Collections.Generic;
using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace SombraDaSanidade.Audio
{
    // Ponto único de áudio (#101). Liga AudioSources aos grupos do SombraMixer, toca SoundEvents com um pool de
    // fontes e controla o volume de cada grupo (preferências do jogador + ducking do AudioDirector).
    // No WebGL o mixer só muda volume (ADR 0005): por isso tudo aqui é volume, e efeitos vêm pré-renderizados.
    public static class AudioService
    {
        public const string MixerResource = "SombraMixer";
        private const int PoolSize = 16;
        private const float MinDb = -80f;

        private static AudioMixer mixer;
        private static readonly Dictionary<AudioBus, AudioMixerGroup> groups = new Dictionary<AudioBus, AudioMixerGroup>();
        private static readonly float[] userVolume = { 1f, 1f, 1f, 1f, 1f, 1f };
        private static readonly float[] duckDb = new float[6];
        private static readonly List<AudioSource> pool = new List<AudioSource>();
        private static int nextSource;
        private static Transform root;

        public static AudioMixer Mixer => EnsureMixer();
        public static bool HasMixer => EnsureMixer() != null;

        public static string VolumeParameter(AudioBus bus) => bus + "Volume";

        public static AudioMixerGroup Group(AudioBus bus)
        {
            if (EnsureMixer() == null) return null;
            if (!groups.TryGetValue(bus, out var group) || group == null)
            {
                var found = mixer.FindMatchingGroups(bus.ToString());
                group = found.Length > 0 ? found[0] : null;
                groups[bus] = group;
            }
            return group;
        }

        // Liga uma fonte criada por um sistema (batimento, logs, sirene...) ao grupo. Sem mixer, não faz nada.
        public static AudioSource Route(AudioSource source, AudioBus bus)
        {
            if (source != null) source.outputAudioMixerGroup = Group(bus);
            return source;
        }

        // ---------- volume por grupo ----------

        // Preferência do jogador (0 a 1), aplicada por cima do ducking.
        public static void SetUserVolume(AudioBus bus, float linear)
        {
            userVolume[(int)bus] = Mathf.Clamp01(linear);
            ApplyVolume(bus);
        }

        public static float UserVolume(AudioBus bus) => userVolume[(int)bus];

        // Atenuação temporária em dB (0 = nenhuma), usada pelo AudioDirector.
        public static void SetDuck(AudioBus bus, float db)
        {
            duckDb[(int)bus] = Mathf.Min(0f, db);
            ApplyVolume(bus);
        }

        public static float Duck(AudioBus bus) => duckDb[(int)bus];

        // dB efetivo do grupo no mixer.
        public static float GroupDb(AudioBus bus) => Mathf.Max(MinDb, LinearToDb(userVolume[(int)bus]) + duckDb[(int)bus]);

        public static float LinearToDb(float linear) => linear <= 0.0001f ? MinDb : 20f * Mathf.Log10(linear);

        public static void ApplyAll()
        {
            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus))) ApplyVolume(bus);
        }

        private static void ApplyVolume(AudioBus bus)
        {
            if (EnsureMixer() != null) mixer.SetFloat(VolumeParameter(bus), GroupDb(bus));
        }

        // ---------- tocar sons ----------

        // Toca uma tomada do evento. position null: som "na cabeça" (UI, voz). Retorna a fonte usada ou null.
        public static AudioSource Play(SoundEvent sound, Vector2? position = null)
        {
            if (sound == null || !sound.HasClips || !sound.TryConsumeCooldown(Time.unscaledTime)) return null;
            int index = sound.NextIndex();
            var clip = sound.clips[index];
            if (clip == null) return null;

            var source = NextSource();
            Configure(source, sound, position);
            source.clip = clip;
            source.volume = Random.Range(sound.volume.x, sound.volume.y);
            source.pitch = Random.Range(sound.pitch.x, sound.pitch.y);
            source.Play();

            if (sound.noiseRadius > 0f && position.HasValue) NoiseBus.Emit(position.Value, sound.noiseRadius);
            return source;
        }

        // Clip solto num ponto (substitui AudioSource.PlayClipAtPoint, que não passa pelo mixer).
        public static AudioSource PlayClip(AudioClip clip, AudioBus bus, Vector2? position = null, float volume = 1f)
        {
            if (clip == null) return null;
            var source = NextSource();
            source.outputAudioMixerGroup = Group(bus);
            SetSpatial(source, position, bus, 0f);
            source.clip = clip;
            source.volume = volume;
            source.pitch = 1f;
            source.Play();
            return source;
        }

        private static void Configure(AudioSource source, SoundEvent sound, Vector2? position)
        {
            source.outputAudioMixerGroup = Group(sound.bus);
            SetSpatial(source, sound.spatial ? position : null, sound.bus, sound.maxDistance);
        }

        public static void SetSpatial(AudioSource source, Vector2? position, AudioBus bus, float maxDistance)
        {
            var config = AudioConfig.Load();
            if (position.HasValue)
            {
                source.transform.position = new Vector3(position.Value.x, position.Value.y, 0f);
                source.spatialBlend = config.spatialBlend;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = maxDistance > 0f ? maxDistance
                    : bus == AudioBus.Monster ? config.monsterMaxDistance : config.worldMaxDistance;
                source.dopplerLevel = 0f;
            }
            else
            {
                source.spatialBlend = 0f;
            }
        }

        private static AudioSource NextSource()
        {
            EnsureRoot();
            // Reaproveita uma fonte livre; se todas tocam, rouba a mais antiga (round-robin).
            for (int n = 0; n < pool.Count; n++)
            {
                int i = (nextSource + n) % pool.Count;
                if (pool[i] != null && !pool[i].isPlaying)
                {
                    nextSource = (i + 1) % pool.Count;
                    return pool[i];
                }
            }
            var s = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Count;
            s.Stop();
            return s;
        }

        private static void EnsureRoot()
        {
            if (root != null) return;
            pool.Clear();
            nextSource = 0;
            root = new GameObject("AudioService").transform;
            Object.DontDestroyOnLoad(root.gameObject);
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("OneShot " + i);
                go.transform.SetParent(root, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                pool.Add(s);
            }
        }

        private static AudioMixer EnsureMixer()
        {
            if (mixer == null)
            {
                mixer = Resources.Load<AudioMixer>(MixerResource);
                groups.Clear();
            }
            return mixer;
        }

        // O projeto não recarrega o domínio no Play: zera o estado estático a cada Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            root = null;
            pool.Clear();
            for (int i = 0; i < duckDb.Length; i++) duckDb[i] = 0f;
        }
    }
}
