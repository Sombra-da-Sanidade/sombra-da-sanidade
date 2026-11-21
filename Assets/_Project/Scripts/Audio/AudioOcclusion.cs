using UnityEngine;

namespace SombraDaSanidade.Audio
{
    // Oclusão 2D por troca de versão (ADR 0005: no WebGL não há filtro em runtime). Um raycast até o ouvinte a
    // cada occlusionInterval: com parede no meio, crossfade da fonte limpa para a abafada e -6 dB.
    // Para sons contínuos (respiração do monstro, máquinas). #101, docs/audio/sound-design.md seção 3.
    public class AudioOcclusion : MonoBehaviour
    {
        [SerializeField] private SoundEvent sound;
        [SerializeField] private float baseVolume = 1f;
        [SerializeField] private bool playOnStart = true;

        private AudioSource dry, muffled;
        private AudioConfig config;
        private int wallMask;
        private float nextCheck;
        private float mix;   // 0 = limpa, 1 = abafada

        public bool Occluded { get; private set; }
        public float Mix => mix;
        public AudioSource Dry => dry;
        public AudioSource Muffled => muffled;

        public void Setup(SoundEvent loop, float volume)
        {
            sound = loop;
            baseVolume = volume;
        }

        private void Start()
        {
            config = AudioConfig.Load();
            wallMask = LayerMask.GetMask("Walls");
            if (sound == null || !sound.HasClips) return;
            int index = sound.NextIndex();
            dry = MakeSource(sound.clips[index]);
            var muffledClip = sound.MuffledFor(index);
            if (muffledClip != null) muffled = MakeSource(muffledClip);
            if (playOnStart) Play();
        }

        public void Play()
        {
            if (dry == null) return;
            // Mesmo dspTime: as duas versões andam juntas e o crossfade não "pula".
            double at = AudioSettings.dspTime + 0.05;
            dry.PlayScheduled(at);
            if (muffled != null) muffled.PlayScheduled(at);
        }

        public void Stop()
        {
            if (dry != null) dry.Stop();
            if (muffled != null) muffled.Stop();
        }

        private AudioSource MakeSource(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            AudioService.Route(s, sound.bus);
            // A fonte fica no próprio objeto (anda com o monstro ou a máquina).
            s.spatialBlend = sound.spatial ? config.spatialBlend : 0f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.maxDistance = sound.maxDistance > 0f ? sound.maxDistance
                : sound.bus == AudioBus.Monster ? config.monsterMaxDistance : config.worldMaxDistance;
            s.volume = 0f;
            return s;
        }

        private void Update()
        {
            if (dry == null) return;
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + config.occlusionInterval;
                Occluded = IsBlocked();
            }
            float target = Occluded ? 1f : 0f;
            mix = Mathf.MoveTowards(mix, target, Time.deltaTime / Mathf.Max(0.01f, config.occlusionFade));
            float occludedGain = Mathf.Pow(10f, config.occlusionDb / 20f);
            if (muffled != null)
            {
                dry.volume = baseVolume * (1f - mix);
                muffled.volume = baseVolume * mix * occludedGain;
            }
            else
            {
                dry.volume = baseVolume * Mathf.Lerp(1f, occludedGain, mix);
            }
        }

        private bool IsBlocked()
        {
            var listener = AudioListenerPosition();
            if (!listener.HasValue) return false;
            Vector2 from = transform.position;
            Vector2 to = listener.Value;
            return Physics2D.Linecast(from, to, wallMask).collider != null;
        }

        private static AudioListener cachedListener;

        private static Vector2? AudioListenerPosition()
        {
            if (cachedListener == null || !cachedListener.isActiveAndEnabled)
                cachedListener = FindAnyObjectByType<AudioListener>();
            return cachedListener != null ? (Vector2)cachedListener.transform.position : (Vector2?)null;
        }
    }
}
