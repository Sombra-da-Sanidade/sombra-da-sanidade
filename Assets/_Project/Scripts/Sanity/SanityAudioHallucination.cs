using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SombraDaSanidade.Sanity
{
    public enum AudioHallucinationType
    {
        FalseFootsteps,
        MetallicBang,
        InfrasoundWhisper
    }

    // Paranoia Auditiva — Alucinações Sonoras Espaciais 2D (GDD 4.6).
    // Frente 3 (Sanidade, HUD e Narrativa) — Módulo 2.
    // Abaixo de 35% de sanidade mental, gera sons falsos posicionados ao redor do jogador
    // (passos sobre chapa de aço, estrondo metálico ou sussurros de 14.8 Hz).
    // Se o feixe da lanterna iluminar a origem do som, a ilusão é dissipada imediatamente.
    [RequireComponent(typeof(AudioSource))]
    public class SanityAudioHallucination : MonoBehaviour
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        [Tooltip("Lanterna do jogador para detecção de dissipação luminosa. Vazio: busca no jogador.")]
        [SerializeField] private FlashlightController flashlight;

        [Header("Distâncias e Ângulos de Aparição")]
        [SerializeField] private float minSpawnDistance = 4f;
        [SerializeField] private float maxSpawnDistance = 7.5f;

        [Header("Áudios Pré-gravados (Opcionais; gera sintéticos se vazio)")]
        [SerializeField] private AudioClip footstepsClip;
        [SerializeField] private AudioClip metallicBangClip;
        [SerializeField] private AudioClip whisperClip;
        [SerializeField] private AudioClip dissipationSnapClip;

        public bool IsPlayingHallucination { get; private set; }
        public AudioHallucinationType CurrentType { get; private set; }
        public Vector2 CurrentPosition { get; private set; }
        public int TotalTriggersCount { get; private set; }
        public int DissipatedCount { get; private set; }

        public event Action<AudioHallucinationType, Vector2> HallucinationTriggered;
        public event Action HallucinationDissipated;

        private AudioSource audioSource;
        private float nextIntervalTimer = -1f;
        private float activeDurationTimer;
        private bool ownsFootstepsClip;
        private bool ownsBangClip;
        private bool ownsWhisperClip;
        private bool ownsSnapClip;

        private void Awake()
        {
            EnsureAudioSources();

            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity != null && flashlight == null) flashlight = sanity.GetComponent<FlashlightController>();
        }

        private void Start()
        {
            if (flashlight == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) flashlight = player.GetComponent<FlashlightController>();
            }
        }

        private void OnDestroy()
        {
            if (ownsFootstepsClip && footstepsClip != null) Destroy(footstepsClip);
            if (ownsBangClip && metallicBangClip != null) Destroy(metallicBangClip);
            if (ownsWhisperClip && whisperClip != null) Destroy(whisperClip);
            if (ownsSnapClip && dissipationSnapClip != null) Destroy(dissipationSnapClip);
        }

        private void Update()
        {
            if (sanity == null)
            {
                sanity = FindAnyObjectByType<SanityController>();
                if (sanity == null) return;
            }
            if (sanity.Config == null) return;

            EnsureAudioSources();

            bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            if (!playing || sanity.State == SanityState.Dead)
            {
                if (IsPlayingHallucination) StopHallucination();
                nextIntervalTimer = -1f;
                return;
            }

            var config = sanity.Config;
            float threshold = config.hallucinationsBelow > 0 ? config.hallucinationsBelow : 35f;

            if (sanity.Value >= threshold)
            {
                if (IsPlayingHallucination) StopHallucination();
                nextIntervalTimer = -1f;
                return;
            }

            // Se uma alucinação sonora estiver em andamento, monitora dissipação pela lanterna
            if (IsPlayingHallucination)
            {
                activeDurationTimer -= Time.deltaTime;

                // 1. Dissipação diegética pela luz: apontar o feixe desfaz o som
                if (flashlight != null && flashlight.IsLit && flashlight.IsPointInCone(CurrentPosition))
                {
                    DissipateByLight();
                    return;
                }

                if (activeDurationTimer <= 0f || !audioSource.isPlaying)
                {
                    StopHallucination();
                }
                return;
            }

            // Agendamento do próximo evento aleatório
            if (nextIntervalTimer < 0f)
            {
                nextIntervalTimer = Random.Range(config.hallucinationMinInterval, config.hallucinationMaxInterval);
            }

            nextIntervalTimer -= Time.deltaTime;
            if (nextIntervalTimer <= 0f)
            {
                TriggerRandomHallucination();
                nextIntervalTimer = Random.Range(config.hallucinationMinInterval, config.hallucinationMaxInterval);
            }
        }

        public void TriggerHallucination(AudioHallucinationType type, Vector2? forcedOrigin = null)
        {
            EnsureAudioSources();

            Vector2 playerPos = sanity != null ? (Vector2)sanity.transform.position : (Vector2)transform.position;
            Vector2 origin = forcedOrigin ?? (playerPos + Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance));

            CurrentType = type;
            CurrentPosition = origin;
            transform.position = new Vector3(origin.x, origin.y, 0f);

            // Ajuste estéreo baseado na posição relativa ao jogador (sem perder volume no 2D)
            float relX = origin.x - playerPos.x;
            audioSource.panStereo = Mathf.Clamp(relX / 6f, -0.85f, 0.85f);

            AudioClip selectedClip = null;
            float duration = 1.5f;

            switch (type)
            {
                case AudioHallucinationType.FalseFootsteps:
                    selectedClip = footstepsClip;
                    duration = 1.8f;
                    audioSource.pitch = Random.Range(0.95f, 1.15f);
                    audioSource.volume = 0.85f;
                    break;

                case AudioHallucinationType.MetallicBang:
                    selectedClip = metallicBangClip;
                    duration = 2.0f;
                    audioSource.pitch = Random.Range(0.85f, 1.05f);
                    audioSource.volume = 0.95f;
                    break;

                case AudioHallucinationType.InfrasoundWhisper:
                    selectedClip = whisperClip;
                    duration = 2.5f;
                    audioSource.pitch = Random.Range(0.9f, 1.0f);
                    audioSource.volume = 0.75f;
                    break;
            }

            if (selectedClip != null)
            {
                audioSource.clip = selectedClip;
                audioSource.Play();
            }

            IsPlayingHallucination = true;
            activeDurationTimer = duration;
            TotalTriggersCount++;

            HallucinationTriggered?.Invoke(CurrentType, CurrentPosition);
        }

        private void TriggerRandomHallucination()
        {
            int r = Random.Range(0, 3);
            TriggerHallucination((AudioHallucinationType)r);
        }

        private void DissipateByLight()
        {
            if (!IsPlayingHallucination) return;

            audioSource.Stop();
            if (dissipationSnapClip != null)
            {
                audioSource.pitch = 1.2f;
                audioSource.PlayOneShot(dissipationSnapClip, 0.6f);
            }

            IsPlayingHallucination = false;
            DissipatedCount++;
            HallucinationDissipated?.Invoke();
        }

        private void StopHallucination()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            IsPlayingHallucination = false;
        }

        private void EnsureAudioSources()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.25f; // Panning estéreo preservando volume audível no 2D
                audioSource.minDistance = 6f;
                audioSource.maxDistance = 30f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (footstepsClip == null)
            {
                footstepsClip = CreateProceduralFootstepsClip();
                ownsFootstepsClip = true;
            }

            if (metallicBangClip == null)
            {
                metallicBangClip = CreateProceduralBangClip();
                ownsBangClip = true;
            }

            if (whisperClip == null)
            {
                whisperClip = CreateProceduralWhisperClip();
                ownsWhisperClip = true;
            }

            if (dissipationSnapClip == null)
            {
                dissipationSnapClip = CreateProceduralSnapClip();
                ownsSnapClip = true;
            }
        }

        // Síntese procedural de passos de bota sobre piso de grade metálica (4 passadas rápidas)
        private static AudioClip CreateProceduralFootstepsClip()
        {
            const int rate = 22050;
            const float duration = 1.6f;
            var data = new float[(int)(rate * duration)];

            float[] stepTimes = { 0.05f, 0.38f, 0.72f, 1.05f };
            foreach (float tStart in stepTimes)
            {
                int from = (int)(tStart * rate);
                int count = Mathf.Min((int)(0.12f * rate), data.Length - from);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Exp(-t * 55f);
                    float metalClang = Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 1150f * t) * 0.25f;
                    float heelThud = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.6f;
                    data[from + i] += (metalClang + heelThud) * env * 0.8f;
                }
            }

            var clip = AudioClip.Create("FootstepsHallucination (sintético)", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Síntese procedural de estrondo metálico / choque ressonante distante
        private static AudioClip CreateProceduralBangClip()
        {
            const int rate = 22050;
            const float duration = 1.8f;
            var data = new float[(int)(rate * duration)];

            int count = data.Length;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 4.5f);
                float strike = (i < rate * 0.03f) ? (Random.value * 2f - 1f) * 0.8f : 0f;
                float resonance = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.5f
                                + Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.3f
                                + Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.15f;
                data[i] = (strike + resonance) * env;
            }

            var clip = AudioClip.Create("MetallicBangHallucination (sintético)", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Síntese procedural de infrassom / sussurros de 14.8 Hz com ruído estocástico
        private static AudioClip CreateProceduralWhisperClip()
        {
            const int rate = 22050;
            const float duration = 2.2f;
            var data = new float[(int)(rate * duration)];

            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Sin(t / duration * Mathf.PI);

                // Modulação de 14.8 Hz (infrassom diegético do GDD)
                float infraMod = (1f + Mathf.Sin(2f * Mathf.PI * 14.8f * t)) * 0.5f;

                // Ruído suave simulando sussurros e estática ambiente
                float noise = (Random.value * 2f - 1f) * 0.3f;
                float lowDrone = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.4f;

                data[i] = (noise * infraMod + lowDrone) * env * 0.75f;
            }

            var clip = AudioClip.Create("WhisperHallucination (sintético)", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Estalo estático de dissipação ao iluminar a alucinação
        private static AudioClip CreateProceduralSnapClip()
        {
            const int rate = 22050;
            const float duration = 0.08f;
            var data = new float[(int)(rate * duration)];

            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 90f);
                data[i] = (Random.value * 2f - 1f) * env * 0.85f;
            }

            var clip = AudioClip.Create("DissipationSnap (sintético)", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
