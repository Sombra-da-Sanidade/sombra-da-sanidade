using System;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    public enum SanityState { Lucid, Anxious, Hallucinating, Collapsed, Dead }

    // Sanidade do jogador (GDD 4.2 e 6.3). Dono: Marley. Issue #8.
    // Fica no Player. A API pública é o contrato usado por Zona Segura, itens e HUD.
    public class SanityController : MonoBehaviour
    {
        [SerializeField] private SanityConfig config;

        public float Value { get; private set; }
        public float Ratio => config.maxSanity > 0f ? Value / config.maxSanity : 0f;
        public SanityState State { get; private set; } = SanityState.Lucid;
        public bool InSafeZone => safeZoneCount > 0;

        public event Action<float> ValueChanged;
        public event Action<SanityState> StateChanged;

        private int safeZoneCount;

        private void Awake()
        {
            Value = config.maxSanity;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // TODO(Marley): dreno no escuro (lanterna apagada e fora de Zona Segura),
            // dreno com o monstro próximo, regeneração em Zona Segura. Usar Add(-x * Time.deltaTime).
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
