using System;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    // Batimento cardíaco que acelera e fica mais alto conforme a sanidade cai (GDD 7 e 9).
    // Com a mente alucinando, cada batida também treme a câmera (GDD 9). Frente 3 (Sanidade e HUD). Issue #9.
    // Sem clip no Inspector, gera um batimento sintético até a pasta Audio ter um de verdade.
    [RequireComponent(typeof(AudioSource))]
    public class SanityHeartbeat : MonoBehaviour
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        [Tooltip("Som de uma batida (tum-tum). Vazio: usa um sintético.")]
        [SerializeField] private AudioClip clip;

        [Header("Ritmo (começa no estado Ansioso)")]
        [SerializeField] private float calmBpm = 65f;
        [SerializeField] private float panicBpm = 150f;
        [SerializeField, Range(0f, 1f)] private float minVolume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

        [Header("Tremor da câmera (a partir de Alucinando)")]
        [SerializeField] private float shakeDuration = 0.12f;
        [SerializeField] private float minShake = 0.03f;
        [SerializeField] private float maxShake = 0.12f;

        // Para sincronizar outros efeitos com a batida.
        public event Action Beat;

        private AudioSource source;
        private CameraFollow cameraFollow;
        private bool ownsClip;
        private float timer;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity == null)
            {
                Debug.LogError($"{name}: SanityHeartbeat sem SanityController na cena.", this);
                enabled = false;
                return;
            }

            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            if (clip == null)
            {
                clip = CreateHeartbeatClip();
                ownsClip = true;
            }
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
        }

        private void OnDestroy()
        {
            if (ownsClip && clip != null) Destroy(clip);
        }

        private void Update()
        {
            var state = sanity.State;
            bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            if (!playing || state == SanityState.Lucid || state == SanityState.Dead)
            {
                timer = 0f; // a primeira batida vem logo ao ficar Ansioso
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            var config = sanity.Config;
            float panic = Mathf.InverseLerp(config.anxiousBelow, 0f, sanity.Value);
            timer = 60f / Mathf.Lerp(calmBpm, panicBpm, panic);
            source.PlayOneShot(clip, Mathf.Lerp(minVolume, maxVolume, panic));

            if (cameraFollow != null && sanity.Value < config.hallucinatingBelow)
            {
                float shake = Mathf.InverseLerp(config.hallucinatingBelow, 0f, sanity.Value);
                cameraFollow.Shake(shakeDuration, Mathf.Lerp(minShake, maxShake, shake));
            }

            Beat?.Invoke();
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
    }
}
