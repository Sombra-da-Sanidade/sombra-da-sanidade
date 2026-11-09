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

        public SanityConfig Config => config;
        public float Value { get; private set; }
        public float Ratio => config.maxSanity > 0f ? Value / config.maxSanity : 0f;
        public SanityState State { get; private set; } = SanityState.Lucid;
        public bool InSafeZone => safeZoneCount > 0;
        public bool InDarkness => flashlight != null && !flashlight.IsLit && !InSafeZone;
        public bool IsMonsterNear { get; private set; }
        // Distância até o monstro (infinita sem monstro ativo). Usada pelo batimento em faixas (issue #38).
        public float DistanceToMonster { get; private set; } = float.MaxValue;
        // Limiar das pistas em fósforo (GDD regra 5: 35). O upgrade Ressonador Neural (#31) sobe para 50.
        public float RevealCluesBelow => revealOverride >= 0f ? revealOverride : config.revealCluesBelow;

        private float revealOverride = -1f;

        public event Action<float> ValueChanged;
        public event Action<SanityState> StateChanged;

        private int safeZoneCount;

        // Perfil da ala (#30): o GameManager troca o config por uma cópia ajustada antes do Awake.
        public void UseConfig(SanityConfig tuned)
        {
            if (tuned != null) config = tuned;
        }

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

            if (monster != null && monster.isActiveAndEnabled)
            {
                DistanceToMonster = Vector2.Distance(transform.position, monster.transform.position);
                IsMonsterNear = DistanceToMonster <= config.monsterProximityRadius;
            }
            else
            {
                DistanceToMonster = float.MaxValue;
                IsMonsterNear = false;
            }

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

        // Troca o limiar das pistas em tempo de execução, sem mexer no asset (upgrade da Lux-90, #31).
        public void SetRevealCluesBelow(float value)
        {
            revealOverride = value;
        }

        // Chamado pelo trigger da Zona Segura ao entrar/sair. Contador para suportar zonas sobrepostas.
        public void SetInSafeZone(bool inside)
        {
            safeZoneCount = Mathf.Max(0, safeZoneCount + (inside ? 1 : -1));
        }

        // Atalhos no menu ⋮ do componente para testar efeitos sem esperar no escuro.
        [ContextMenu("Debug: -25 de sanidade")]
        private void DebugDrain() => Add(-25f);

        [ContextMenu("Debug: +25 de sanidade")]
        private void DebugRestore() => Add(25f);

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
