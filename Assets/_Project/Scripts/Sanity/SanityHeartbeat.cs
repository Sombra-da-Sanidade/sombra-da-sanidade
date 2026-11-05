using System;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    public enum ProximityTier { Safe, Warning, Danger }

    // Batimento cardíaco em 3 faixas de distância do monstro (GDD 7 e 9). Frente 3 (Sanidade e HUD). Issues #9 e #38.
    // A tensão é o maior valor entre o pânico da sanidade e a proximidade do monstro: longe e lúcido não há batida;
    // na faixa de alerta ela começa e acelera; na faixa de perigo dispara, treme a câmera e traz a respiração ofegante.
    // Sem clips no Inspector, gera sons sintéticos até a pasta Audio ter os de verdade.
    [RequireComponent(typeof(AudioSource))]
    public class SanityHeartbeat : MonoBehaviour
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        [Tooltip("Som de uma batida (tum-tum). Vazio: usa um sintético.")]
        [SerializeField] private AudioClip clip;

        [Tooltip("Respiração ofegante em loop. Vazio: usa uma sintética.")]
        [SerializeField] private AudioClip breathingClip;

        [Header("Tremor da câmera (Alucinando ou faixa de perigo)")]
        [SerializeField] private float shakeDuration = 0.12f;
        [SerializeField] private float minShake = 0.03f;
        [SerializeField] private float maxShake = 0.12f;

        public ProximityTier Tier { get; private set; } = ProximityTier.Safe;
        // 0 a 1: o que move ritmo, volume, tremor e respiração. O relógio do HUD desenha o ECG com isto.
        public float Tension { get; private set; }
        // 0 quando não há batida (calmo e longe do monstro).
        public float Bpm { get; private set; }

        // Para sincronizar outros efeitos com a batida.
        public event Action Beat;

        private AudioSource source;
        private AudioSource breathing;
        private CameraFollow cameraFollow;
        private bool ownsClip;
        private bool ownsBreathingClip;
        private float timer;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity == null || sanity.Config == null)
            {
                Debug.LogError($"{name}: SanityHeartbeat sem SanityController (ou sem SanityConfig) na cena.", this);
                enabled = false;
                return;
            }

            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            breathing = gameObject.AddComponent<AudioSource>();
            breathing.playOnAwake = false;
            breathing.spatialBlend = 0f;
            breathing.loop = true;
            breathing.volume = 0f;

            if (clip == null)
            {
                clip = CreateHeartbeatClip();
                ownsClip = true;
            }
            if (breathingClip == null)
            {
                breathingClip = CreateBreathingClip();
                ownsBreathingClip = true;
            }
            breathing.clip = breathingClip;
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
        }

        private void OnDestroy()
        {
            if (ownsClip && clip != null) Destroy(clip);
            if (ownsBreathingClip && breathingClip != null) Destroy(breathingClip);
        }

        private void Update()
        {
            var config = sanity.Config;
            bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            if (!playing || sanity.State == SanityState.Dead)
            {
                Calm();
                FadeBreathing(0f);
                return;
            }

            float distance = sanity.DistanceToMonster;
            Tier = distance <= config.heartbeatDangerRadius ? ProximityTier.Danger
                : distance <= config.heartbeatWarningRadius ? ProximityTier.Warning
                : ProximityTier.Safe;

            float sanityPanic = sanity.State == SanityState.Lucid ? 0f : Mathf.InverseLerp(config.anxiousBelow, 0f, sanity.Value);
            float proximityPanic = Tier switch
            {
                ProximityTier.Danger => Mathf.Lerp(config.dangerTensionFloor, 1f,
                    Mathf.InverseLerp(config.heartbeatDangerRadius, config.heartbeatContactRadius, distance)),
                ProximityTier.Warning => config.dangerTensionFloor
                    * Mathf.InverseLerp(config.heartbeatWarningRadius, config.heartbeatDangerRadius, distance),
                _ => 0f
            };

            // Lúcido e longe do monstro: silêncio. A primeira batida vem logo que um dos dois muda.
            if (sanity.State == SanityState.Lucid && Tier == ProximityTier.Safe)
            {
                Calm();
                FadeBreathing(0f);
                return;
            }

            Tension = Mathf.Max(sanityPanic, proximityPanic);
            Bpm = Tension < 0.5f
                ? Mathf.Lerp(config.calmBpm, config.warningBpm, Tension * 2f)
                : Mathf.Lerp(config.warningBpm, config.panicBpm, (Tension - 0.5f) * 2f);

            bool breathe = sanity.Value < config.breathingBelow || Tier == ProximityTier.Danger;
            FadeBreathing(breathe ? Mathf.Lerp(config.breathingMinVolume, config.breathingMaxVolume, Tension) : 0f);
            breathing.pitch = Mathf.Lerp(0.95f, 1.2f, Tension);

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 60f / Mathf.Max(1f, Bpm);

            source.pitch = Mathf.Lerp(1f, config.heartbeatMaxPitch, Tension);
            source.PlayOneShot(clip, Mathf.Lerp(config.heartbeatMinVolume, config.heartbeatMaxVolume, Tension));

            if (cameraFollow != null && (sanity.Value < config.hallucinatingBelow || Tier == ProximityTier.Danger))
                cameraFollow.Shake(shakeDuration, Mathf.Lerp(minShake, maxShake, Tension));

            Beat?.Invoke();
        }

        private void Calm()
        {
            timer = 0f;
            Tension = 0f;
            Bpm = 0f;
            Tier = ProximityTier.Safe;
        }

        // Sobe e desce o volume aos poucos para a respiração não cortar seco.
        private void FadeBreathing(float target)
        {
            breathing.volume = Mathf.MoveTowards(breathing.volume, target, Time.deltaTime);
            if (breathing.volume > 0f && !breathing.isPlaying) breathing.Play();
            else if (breathing.volume <= 0f && breathing.isPlaying) breathing.Stop();
        }

        // "Tum-tum": duas pancadas graves com queda de tom, a segunda mais fraca.
        private static AudioClip CreateHeartbeatClip()
        {
            const int rate = 22050;
            var data = new float[(int)(rate * 0.45f)];
            AddThump(data, rate, 0f, 0.7f);
            AddThump(data, rate, 0.17f, 0.45f);

            var result = AudioClip.Create("Heartbeat (sintético)", data.Length, 1, rate, false);
            result.SetData(data, 0);
            return result;
        }

        private static void AddThump(float[] data, int rate, float start, float gain)
        {
            const float length = 0.14f;
            int from = (int)(start * rate);
            int count = Mathf.Min((int)(length * rate), data.Length - from);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(95f, 50f, t / length) / rate;
                float envelope = Mathf.Exp(-t * 28f) * Mathf.Clamp01(t * 400f);
                // Harmônico para o grave ser ouvido em caixinhas de notebook.
                float wave = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f);
                data[from + i] += wave * envelope * gain;
            }
        }

        // Inspira e expira com ruído filtrado (ar nas vias aéreas), em loop de 2 s.
        private static AudioClip CreateBreathingClip()
        {
            const int rate = 22050;
            var data = new float[rate * 2];
            var rng = new System.Random(1337);
            AddBreath(data, rate, rng, 0.05f, 0.7f, 0.22f, 0.55f);
            AddBreath(data, rate, rng, 0.9f, 0.95f, 0.16f, 0.75f);

            var result = AudioClip.Create("Breathing (sintético)", data.Length, 1, rate, false);
            result.SetData(data, 0);
            return result;
        }

        private static void AddBreath(float[] data, int rate, System.Random rng, float start, float length, float smoothing, float gain)
        {
            int from = (int)(start * rate);
            int count = Mathf.Min((int)(length * rate), data.Length - from);
            float filtered = 0f;
            for (int i = 0; i < count; i++)
            {
                float envelope = Mathf.Sin(i / (float)count * Mathf.PI);
                filtered += smoothing * ((float)(rng.NextDouble() * 2.0 - 1.0) - filtered);
                data[from + i] += filtered * envelope * gain;
            }
        }
    }
}
