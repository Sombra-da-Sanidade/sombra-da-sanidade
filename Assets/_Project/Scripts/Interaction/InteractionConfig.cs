using SombraDaSanidade.Flashlight;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Valores das interações do GDD 4.3 (Ejetar Gaveta de HD).
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Sombra da Sanidade/Config/Interação")]
    public class InteractionConfig : ScriptableObject
    {
        [Header("Gaveta de HD")]
        [Tooltip("Segundos segurando E para ejetar o HD (GDD 4.3; 12 s pelo plano do skill check, docs/skill-check-hd.md). Não está no GDD: ajustar no playtest.")]
        public float ejectDuration = 12f;
        [Tooltip("Raio do barulho ao ejetar o HD (GDD 4.3: ejetar faz barulho). Valor não está no GDD.")]
        public float ejectNoiseRadius = 10f;
        [Tooltip("Upgrades da Lux-90 oferecidos ao ejetar cada HD (Proposta 2 do GDD v1.1). Vazio: sem tela de upgrade.")]
        public LuxUpgradeTree upgradeTree;
        [Tooltip("A partir desta fração do progresso, o servidor faz ruído baixo enquanto ejeta. Não está no GDD: ajustar no playtest.")]
        [Range(0f, 1f)] public float ejectLateNoiseFrom = 0.5f;
        [Tooltip("Raio do ruído baixo depois da metade. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float ejectLateNoiseRadius = 6f;
        [Tooltip("Segundos entre os ruídos baixos. Não está no GDD: ajustar no playtest.")]
        [Min(0.1f)] public float ejectLateNoiseInterval = 3f;

        [Header("Skill check (GDD 4.4: calibragem no multímetro)")]
        [Tooltip("Segundos de canalização antes de o primeiro check poder aparecer. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float skillCheckStartDelay = 2f;
        [Tooltip("Chance por segundo de começar um check (0.25 = 25%/s). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 1f)] public float skillCheckChancePerSecond = 0.25f;
        [Tooltip("Máximo de checks por HD. Não está no GDD: ajustar no playtest.")]
        [Min(0)] public int skillCheckMaxPerDrive = 3;
        [Tooltip("Segundos entre o bipe de aviso e o ponteiro começar a varrer. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float skillCheckWarning = 0.6f;
        [Tooltip("Segundos para o ponteiro varrer o mostrador inteiro. Não está no GDD: ajustar no playtest.")]
        [Min(0.1f)] public float skillCheckSweep = 1.1f;
        [Tooltip("Início mínimo da faixa no mostrador (0-1), para dar tempo de reagir. Não está no GDD: ajustar no playtest.")]
        [Range(0f, 1f)] public float skillCheckZoneMin = 0.35f;
        [Tooltip("Tamanho da faixa good sem HD coletado (0.15 = 15% do mostrador). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 1f)] public float skillCheckGoodSize = 0.15f;
        [Tooltip("Quanto a faixa good encolhe por HD já coletado (15, 13, 11%). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 0.1f)] public float skillCheckShrinkPerDrive = 0.02f;
        [Tooltip("Tamanho da faixa great, no começo da good. Não está no GDD: ajustar no playtest.")]
        [Range(0f, 0.2f)] public float skillCheckGreatSize = 0.04f;
        [Tooltip("Progresso ganho no great (0.05 = +5%). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 0.5f)] public float skillCheckGreatBonus = 0.05f;
        [Tooltip("Progresso perdido na falha (0.10 = -10%). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 0.5f)] public float skillCheckFailPenalty = 0.1f;
        [Tooltip("Segundos com o progresso parado depois da falha. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float skillCheckFailPause = 1f;
        [Tooltip("Raio do ruído da falha (faísca e estalo). Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float skillCheckFailNoiseRadius = 12f;
        [Tooltip("Duração do tremor de câmera na falha. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float skillCheckFailShakeDuration = 0.25f;
        [Tooltip("Intensidade do tremor de câmera na falha. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float skillCheckFailShakeMagnitude = 0.2f;
        [Tooltip("Com o monstro perto, as faixas encolhem esta fração (0.4 = -40%). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 0.9f)] public float skillCheckMonsterNearShrink = 0.4f;
        [Tooltip("Abaixo desta sanidade o ponteiro treme (GDD 4.4: 35 é o limiar da loucura).")]
        [Range(0f, 100f)] public float skillCheckJitterBelowSanity = 35f;
        [Tooltip("Amplitude do tremor do ponteiro (fração do mostrador). Não está no GDD: ajustar no playtest.")]
        [Range(0f, 0.2f)] public float skillCheckJitterAmount = 0.04f;
    }
}
