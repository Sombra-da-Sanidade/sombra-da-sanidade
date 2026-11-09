using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Level
{
    // Fuga da Eclusa (GDD 5 e 7, fase 4; issue #44). Quando o EclusaTerminal recebe os HDs, começa a purga:
    // o monstro entra em fúria e o jogador tem purgeSeconds para chegar à porta externa (ExitDoor).
    // O tempo restante nunca aparece em número (GDD 8): a sirene acelera e a luz fica vermelha.
    // Se o tempo acabar, Game Over com causa própria (DeathCause.Purge).
    public class EscapeSequence : MonoBehaviour
    {
        public static EscapeSequence Instance { get; private set; }

        [SerializeField] private EscapeConfig config;
        [SerializeField] private EclusaTerminal terminal;
        [Tooltip("Luzes giratórias de emergência (ficam apagadas até a purga).")]
        [SerializeField] private Light2D[] beacons = Array.Empty<Light2D>();
        [Tooltip("Luz global da cena (vazia: procura a Global Light 2D).")]
        [SerializeField] private Light2D globalLight;
        [Tooltip("Clipe de um ciclo da sirene. Vazio: gera um lamento sintético até a pasta Audio ter o de verdade.")]
        [SerializeField] private AudioClip sirenClip;

        public EscapeConfig Config => config;
        public EclusaTerminal Terminal => terminal;
        public bool Active { get; private set; }
        public float Elapsed { get; private set; }
        public float Duration => config != null ? config.purgeSeconds : 150f;
        public float Progress => Active ? Mathf.Clamp01(Elapsed / Duration) : 0f;
        // Só para o painel de teste e os testes. Nunca mostrar no HUD (GDD 8).
        public float Remaining => Mathf.Max(0f, Duration - Elapsed);
        public bool InFinalPhase => Active && config != null && Progress >= config.finalPhaseFrom;

        public event Action Started;

        private AudioSource siren;
        private bool ownsClip;
        private float nextSiren;
        private float baseGlobalIntensity;
        private Color baseGlobalColor;
        private float beaconAngle;

        private void Awake()
        {
            Instance = this;
            if (config == null) Debug.LogError($"{name}: EscapeSequence sem EscapeConfig.", this);
            siren = gameObject.AddComponent<AudioSource>();
            siren.playOnAwake = false;
            siren.spatialBlend = 0f;
            if (sirenClip == null)
            {
                sirenClip = CreateSirenClip();
                ownsClip = true;
            }
            foreach (var b in beacons)
                if (b != null) b.enabled = false;
        }

        private void Start()
        {
            if (terminal == null) terminal = FindAnyObjectByType<EclusaTerminal>();
            if (terminal != null) terminal.ActivatedEvent += OnTerminalActivated;
            if (globalLight == null)
            {
                foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                    if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
            }
            if (globalLight != null)
            {
                baseGlobalIntensity = globalLight.intensity;
                baseGlobalColor = globalLight.color;
            }
        }

        private void OnDestroy()
        {
            if (terminal != null) terminal.ActivatedEvent -= OnTerminalActivated;
            if (ownsClip && sirenClip != null) Destroy(sirenClip);
            if (Instance == this) Instance = null;
        }

        private void OnTerminalActivated(EclusaTerminal _) => Begin();

        // Público para o painel de teste e os testes.
        public void Begin()
        {
            if (Active) return;
            Active = true;
            Elapsed = 0f;
            nextSiren = 0f;
            foreach (var b in beacons)
            {
                if (b == null) continue;
                b.enabled = true;
                b.color = config != null ? config.emergencyColor : Color.red;
            }
            foreach (var monster in FindObjectsByType<MonsterController>(FindObjectsSortMode.None))
                monster.SetFury(true);
            Started?.Invoke();
        }

        // Painel de teste: deixa só `seconds` de purga.
        public void SetRemaining(float seconds)
        {
            if (!Active) Begin();
            Elapsed = Mathf.Max(0f, Duration - seconds);
        }

        private void Update()
        {
            if (!Active || config == null) return;
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying)
            {
                if (siren.isPlaying) siren.Stop();
                return;
            }

            Elapsed += Time.deltaTime;
            float p = Progress;
            UpdateSiren(p);
            UpdateLights(p);

            if (Elapsed >= Duration && gm != null) gm.TriggerGameOver(DeathCause.Purge);
        }

        private void UpdateSiren(float p)
        {
            if (Time.time < nextSiren) return;
            float period = Mathf.Lerp(config.sirenPeriodStart, config.sirenPeriodEnd, p);
            nextSiren = Time.time + period;
            siren.pitch = sirenClip.length / period;   // um lamento inteiro por ciclo
            siren.volume = Mathf.Lerp(config.sirenVolumeStart, config.sirenVolumeEnd, p);
            siren.Stop();
            siren.clip = sirenClip;
            siren.Play();
        }

        private void UpdateLights(float p)
        {
            float spin = Mathf.Lerp(config.beaconSpinStart, config.beaconSpinEnd, p);
            beaconAngle = Mathf.Repeat(beaconAngle + spin * 360f * Time.deltaTime, 360f);
            // Giroflex: cada luz é um cone que gira; acende mais à medida que o tempo acaba.
            float intensity = Mathf.Lerp(config.beaconIntensityStart, config.beaconIntensityEnd, p);
            bool strobeOff = InFinalPhase && Mathf.Repeat(Time.time * config.strobeRate, 1f) < 0.25f;
            for (int i = 0; i < beacons.Length; i++)
            {
                var b = beacons[i];
                if (b == null) continue;
                b.transform.rotation = Quaternion.Euler(0f, 0f, beaconAngle + i * 137f);
                b.intensity = strobeOff ? 0f : intensity;
            }

            if (globalLight == null) return;
            // A base escura da cena pulsa para vermelho no ritmo da sirene, mais forte no fim.
            float period = Mathf.Lerp(config.sirenPeriodStart, config.sirenPeriodEnd, p);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / period);
            float amount = Mathf.Lerp(0.3f, 1f, p) * wave;
            globalLight.color = Color.Lerp(baseGlobalColor, config.emergencyColor, Mathf.Lerp(0.4f, 1f, p));
            globalLight.intensity = Mathf.Lerp(baseGlobalIntensity, config.globalPulseIntensity, amount);
        }

        // Um ciclo de sirene de 1 s: tom sobe e desce (lamento), com harmônico para soar em alto-falante pequeno.
        private static AudioClip CreateSirenClip()
        {
            const int rate = 22050;
            var data = new float[rate];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float freq = Mathf.Lerp(420f, 880f, Mathf.Sin(t * Mathf.PI));
                phase += 2f * Mathf.PI * freq / rate;
                float envelope = Mathf.Clamp01(t * 30f) * Mathf.Clamp01((1f - t) * 30f);
                data[i] = (Mathf.Sin(phase) * 0.6f + Mathf.Sign(Mathf.Sin(phase)) * 0.15f) * envelope * 0.7f;
            }
            var clip = AudioClip.Create("Sirene da purga (sintética)", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
