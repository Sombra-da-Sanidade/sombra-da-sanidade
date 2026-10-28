using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Monster;
using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    public enum SanityState { Lucid, Anxious, Hallucinating, Collapsed, Dead }

    // Sanidade do jogador (GDD 4.2 e 6.3). Frente 3 (Sanidade e HUD). Issue #8.
    // Fica no Player. A API pública é o contrato usado por Zona Segura, itens e HUD.
    public class SanityController : MonoBehaviour
    {
        [SerializeField] private SanityConfig config;

        [Tooltip("Lanterna do jogador. Vazio: usa a do mesmo objeto.")]
        [SerializeField] private FlashlightController flashlight;

        [Tooltip("Espécime Vazio. Vazio: procura um na cena ao iniciar.")]
        [SerializeField] private MonsterController monster;

        public float Value { get; private set; }
        public float Ratio => config.maxSanity > 0f ? Value / config.maxSanity : 0f;
        public SanityState State { get; private set; } = SanityState.Lucid;
        public bool InSafeZone => safeZoneCount > 0;
        public bool InDarkness => flashlight != null && !flashlight.IsLit && !InSafeZone;
        public bool IsMonsterNear { get; private set; }

        public event Action<float> ValueChanged;
        public event Action<SanityState> StateChanged;

        private int safeZoneCount;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{name}: SanityController sem SanityConfig no Inspector.", this);
                enabled = false;
                return;
            }

            Value = config.maxSanity;
            if (flashlight == null) flashlight = GetComponent<FlashlightController>();
        }

        // Start: o monstro pode ser criado depois do jogador na cena.
        private void Start()
        {
            if (monster == null) monster = FindAnyObjectByType<MonsterController>();
            if (flashlight == null) Debug.LogWarning($"{name}: SanityController sem lanterna, dreno no escuro desligado.", this);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            IsMonsterNear = monster != null && monster.isActiveAndEnabled
                && ((Vector2)(monster.transform.position - transform.position)).sqrMagnitude
                   <= config.monsterProximityRadius * config.monsterProximityRadius;

            // Zona Segura protege a mente por completo: o monstro não entra nela (GDD 4.2).
            float rate;
            if (InSafeZone)
            {
                rate = config.safeZoneRegen;
            }
            else
            {
                rate = 0f;
                if (InDarkness) rate -= config.darknessDrain;
                if (IsMonsterNear) rate -= config.monsterProximityDrain;
            }

            if (rate > 0f && Value >= config.maxSanity) return;
            if (rate != 0f) Add(rate * Time.deltaTime);
        }

        // Positivo recupera, negativo drena. Ex.: Estabilizador = Add(config.stabilizerAmount).
        public void Add(float amount)
        {
            if (State == SanityState.Dead) return;

            Value = Mathf.Clamp(Value + amount, 0f, config.maxSanity);
            ValueChanged?.Invoke(Value);
            UpdateState();

            if (Value <= 0f && GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver(DeathCause.Sanity);
        }

        // Chamado pelo trigger da Zona Segura ao entrar/sair. Contador para suportar zonas sobrepostas.
        public void SetInSafeZone(bool inside)
        {
            safeZoneCount = Mathf.Max(0, safeZoneCount + (inside ? 1 : -1));
        }

        private void UpdateState()
        {
            SanityState next;
            if (Value <= 0f) next = SanityState.Dead;
            else if (Value < config.collapsedBelow) next = SanityState.Collapsed;
            else if (Value < config.hallucinatingBelow) next = SanityState.Hallucinating;
            else if (Value < config.anxiousBelow) next = SanityState.Anxious;
            else next = SanityState.Lucid;

            if (next == State) return;
            State = next;
            StateChanged?.Invoke(State);
        }
    }
}
