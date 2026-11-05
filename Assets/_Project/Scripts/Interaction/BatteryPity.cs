using SombraDaSanidade.Flashlight;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Ajuste dinâmico leve (extra #47, pesquisa: Hamlet em docs/pesquisa/ritmo-e-tensao.md):
    // bateria abaixo de 20 por mais de 30 s faz o próximo armário ter +15 pontos de chance de bateria.
    // Desliga quando sai uma bateria. Sem aviso ao jogador; só log no Console para o playtest.
    // Fica no Player, ao lado da lanterna. O Locker cria sob demanda se a cena não tiver. Frente 1.
    [RequireComponent(typeof(FlashlightController))]
    public class BatteryPity : MonoBehaviour
    {
        [SerializeField] private InteractionConfig config;

        public bool IsActive { get; private set; }
        public float BonusChance => IsActive && config != null ? config.pityBonus : 0f;
        public float LowBatteryTime => lowTime;

        private FlashlightController flashlight;
        private float lowTime;

        public static BatteryPity On(FlashlightController flashlight, InteractionConfig config)
        {
            if (flashlight == null) return null;
            var pity = flashlight.GetComponent<BatteryPity>();
            if (pity == null) pity = flashlight.gameObject.AddComponent<BatteryPity>();
            if (pity.config == null) pity.config = config;
            return pity;
        }

        private void Awake()
        {
            flashlight = GetComponent<FlashlightController>();
        }

        private void Update()
        {
            if (config == null || IsActive) return;
            if (flashlight.Battery < config.pityBatteryBelow)
            {
                lowTime += Time.deltaTime;
                if (lowTime >= config.pityDelay)
                {
                    IsActive = true;
                    Log($"ligado: bateria abaixo de {config.pityBatteryBelow:0} por {config.pityDelay:0} s, próximo armário +{config.pityBonus * 100f:0} pontos de bateria");
                }
            }
            else
            {
                lowTime = 0f;
            }
        }

        // Chamado pelo Locker quando sai uma bateria: o ajuste volta ao normal.
        public void NotifyBatteryFound()
        {
            if (IsActive) Log("desligado: bateria encontrada");
            IsActive = false;
            lowTime = 0f;
        }

        private void Log(string message)
        {
            if (config != null && config.pityLog) Debug.Log($"[Ajuste de bateria] {message}", this);
        }
    }
}
