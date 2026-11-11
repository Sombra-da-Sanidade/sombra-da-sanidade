using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    // Valores do GDD 4.2, 4.4 e 6.3 (Sanidade Mental).
    [CreateAssetMenu(fileName = "SanityConfig", menuName = "Sombra da Sanidade/Config/Sanidade")]
    public class SanityConfig : ScriptableObject
    {
        public float maxSanity = 100f;

        [Header("Ralos (por segundo)")]
        [Tooltip("Escuridão total (GDD: -1.5%/s; playtest: 1,5x mais rápido).")]
        public float darknessDrain = 2.25f;
        [Tooltip("Proximidade do monstro (GDD: -3%/s; playtest: 1,5x mais rápido).")]
        public float monsterProximityDrain = 4.5f;
        [Tooltip("Distância em que o monstro conta como próximo. Não está no GDD: ajustar no playtest.")]
        public float monsterProximityRadius = 5f;
        [Tooltip("Custo imediato do Módulo Adrena-Syn (GDD: -20).")]
        public float adrenaSynCost = 20f;
        [Tooltip("Custo imediato do Módulo Neural (Ala Executiva, GDD 7). Não está no GDD: ajustar no playtest.")]
        public float neuralModuleCost = 15f;
        [Tooltip("Segundos em que o Módulo Neural revela as pistas com qualquer sanidade. Não está no GDD.")]
        public float neuralModuleRevealSeconds = 20f;

        [Header("Fontes")]
        [Tooltip("Sob Zona Segura (GDD: +2%/s).")]
        public float safeZoneRegen = 2f;
        [Tooltip("Estabilizador de Lítio (GDD: +30).")]
        public float stabilizerAmount = 30f;
        [Tooltip("Multiplicador de velocidade depois do Estabilizador (GDD 4.3: \"deixa Thomas lento\"). Valor não está no GDD.")]
        public float stabilizerSlowMultiplier = 0.6f;
        [Tooltip("Segundos lento depois do Estabilizador (GDD 4.3: \"alguns segundos\"). Valor não está no GDD.")]
        public float stabilizerSlowDuration = 4f;

        [Header("Limites dos estados (GDD 4.2)")]
        public float anxiousBelow = 75f;
        public float hallucinatingBelow = 40f;
        public float collapsedBelow = 15f;
        [Tooltip("Pistas de enigma aparecem abaixo deste valor (GDD 4.4: 35%).")]
        public float revealCluesBelow = 35f;

        [Header("Alucinações (GDD 4.6)")]
        [Tooltip("Alucinações começam abaixo deste valor (GDD: 35%).")]
        public float hallucinationsBelow = 35f;
        [Tooltip("Intervalo mínimo entre alucinações em segundos (GDD: 15).")]
        public float hallucinationMinInterval = 15f;
        [Tooltip("Intervalo máximo entre alucinações em segundos (GDD: 25).")]
        public float hallucinationMaxInterval = 25f;

        [Header("Batimento em 3 faixas de distância do monstro (issue #38)")]
        [Tooltip("Abaixo desta distância (m) o monstro está na faixa de alerta. Valor não está no GDD: ajustar no playtest.")]
        public float heartbeatWarningRadius = 12f;
        [Tooltip("Abaixo desta distância (m) o monstro está na faixa de perigo. Valor não está no GDD: ajustar no playtest.")]
        public float heartbeatDangerRadius = 6f;
        [Tooltip("Distância (m) em que a tensão por proximidade chega ao máximo.")]
        public float heartbeatContactRadius = 1.5f;
        [Tooltip("Tensão (0 a 1) ao cruzar a borda da faixa de perigo. Entre alerta e perigo ela sobe de 0 até este valor.")]
        [Range(0f, 1f)] public float dangerTensionFloor = 0.6f;
        [Tooltip("Batimentos por minuto com tensão baixa (primeira batida ao ficar Ansioso ou entrar na faixa de alerta).")]
        public float calmBpm = 65f;
        [Tooltip("Batimentos por minuto com tensão média (meio da escala).")]
        public float warningBpm = 105f;
        [Tooltip("Batimentos por minuto com tensão máxima (monstro colado ou sanidade perto de zero).")]
        public float panicBpm = 160f;
        [Tooltip("Volume da batida com tensão mínima e máxima.")]
        [Range(0f, 1f)] public float heartbeatMinVolume = 0.35f;
        [Range(0f, 1f)] public float heartbeatMaxVolume = 1f;
        [Tooltip("Pitch da batida com tensão máxima (1 = normal).")]
        public float heartbeatMaxPitch = 1.15f;

        [Header("Respiração ofegante")]
        [Tooltip("A respiração começa abaixo desta sanidade, ou sempre na faixa de perigo.")]
        public float breathingBelow = 35f;
        [Tooltip("Volume da respiração com tensão mínima e máxima.")]
        [Range(0f, 1f)] public float breathingMinVolume = 0.15f;
        [Range(0f, 1f)] public float breathingMaxVolume = 0.7f;
    }
}
