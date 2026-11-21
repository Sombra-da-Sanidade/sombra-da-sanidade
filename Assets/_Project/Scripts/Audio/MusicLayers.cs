using UnityEngine;

namespace SombraDaSanidade.Audio
{
    // Toca um MusicSet com uma fonte por camada, todas agendadas no mesmo dspTime (ADR 0005: deriva de 3,7 ms em
    // 60 s no navegador). Só o volume de cada camada muda: Intensity 0 = só L0, 1 = até L1, 2 = até L2, 3 = todas.
    // O AudioDirector define a intensidade. #101, docs/audio/sound-design.md seção 4.
    public class MusicLayers : MonoBehaviour
    {
        [SerializeField] private MusicSet set;
        [Tooltip("Camadas ativas no começo (0 a 3).")]
        [SerializeField] private int startIntensity = 1;

        private AudioSource[] sources = new AudioSource[0];
        private float[] current = new float[0];
        private AudioConfig config;

        public MusicSet Set => set;
        public int Intensity { get; private set; }
        public int LayerCount => sources.Length;
        public bool Started { get; private set; }

        public void SetMusic(MusicSet music) => set = music;

        public float LayerVolume(int layer) => layer >= 0 && layer < current.Length ? current[layer] : 0f;
        public AudioSource LayerSource(int layer) => layer >= 0 && layer < sources.Length ? sources[layer] : null;

        public void SetIntensity(int intensity) => Intensity = Mathf.Clamp(intensity, 0, Mathf.Max(0, sources.Length - 1));

        private void Start()
        {
            config = AudioConfig.Load();
            Begin();
        }

        public void Begin()
        {
            if (Started || set == null || set.Count == 0) return;
            Started = true;
            int n = set.Count;
            sources = new AudioSource[n];
            current = new float[n];
            double at = AudioSettings.dspTime + 0.2;
            for (int i = 0; i < n; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                AudioService.Route(s, AudioBus.Music);
                s.clip = set.layers[i];
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.volume = 0f;
                if (s.clip != null) s.PlayScheduled(at);
                sources[i] = s;
            }
            SetIntensity(startIntensity);
        }

        private void Update()
        {
            if (!Started) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < sources.Length; i++)
            {
                float target = i <= Intensity ? config.layerVolume : 0f;
                // Entra rápido (susto) e sai devagar (recuo).
                float speed = target > current[i] ? 1f / Mathf.Max(0.01f, config.layerFadeIn) : 1f / Mathf.Max(0.01f, config.layerFadeOut);
                current[i] = Mathf.MoveTowards(current[i], target, speed * config.layerVolume * dt);
                sources[i].volume = current[i];
            }
        }
    }
}
