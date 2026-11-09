using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Fuga da Eclusa (GDD 5 e 7, fase 4; issue #44): terminal que recebe os 3 HDs e a purga cronometrada.
    // O tempo nunca aparece em número (GDD 8): só sirene e luz contam quanto falta.
    [CreateAssetMenu(fileName = "EscapeConfig", menuName = "Sombra da Sanidade/Config/Fuga da Eclusa")]
    public class EscapeConfig : ScriptableObject
    {
        [Header("Terminal da eclusa")]
        [Tooltip("Segundos segurando E para inserir os HDs. Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float insertHoldSeconds = 3f;
        [Tooltip("Raio do barulho da eclusa abrindo ao inserir os HDs (chama o monstro). Não está no GDD.")]
        [Min(0f)] public float activationNoiseRadius = 40f;

        [Header("Purga")]
        [Tooltip("Segundos entre inserir os HDs e a purga (GDD 7: corrida de 2-3 min).")]
        [Min(1f)] public float purgeSeconds = 150f;
        [Tooltip("A partir desta fração do tempo, fase final: sirene mais rápida e estrobo. Não está no GDD.")]
        [Range(0f, 1f)] public float finalPhaseFrom = 0.8f;

        [Header("Sirene (acelera com o tempo)")]
        [Tooltip("Segundos por ciclo do lamento no começo da purga.")]
        [Min(0.2f)] public float sirenPeriodStart = 2.4f;
        [Tooltip("Segundos por ciclo do lamento no fim da purga.")]
        [Min(0.2f)] public float sirenPeriodEnd = 0.8f;
        [Range(0f, 1f)] public float sirenVolumeStart = 0.35f;
        [Range(0f, 1f)] public float sirenVolumeEnd = 0.8f;

        [Header("Luz de emergência")]
        public Color emergencyColor = new Color(0.9f, 0.12f, 0.1f);
        [Tooltip("Intensidade das luzes giratórias no começo e no fim da purga.")]
        [Min(0f)] public float beaconIntensityStart = 0.6f;
        [Min(0f)] public float beaconIntensityEnd = 1.2f;
        [Tooltip("Voltas por segundo das luzes giratórias no começo e no fim.")]
        [Min(0f)] public float beaconSpinStart = 0.5f;
        [Min(0f)] public float beaconSpinEnd = 1.6f;
        [Tooltip("Intensidade máxima da luz global no pulso vermelho (no fim da purga). A base é a da cena.")]
        [Min(0f)] public float globalPulseIntensity = 0.18f;
        [Tooltip("Estrobos por segundo na fase final.")]
        [Min(0f)] public float strobeRate = 4f;
    }
}
