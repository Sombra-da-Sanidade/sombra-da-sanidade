using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    // Valores do GDD 4.2, 4.4 e 6.3 (Sanidade Mental).
    [CreateAssetMenu(fileName = "SanityConfig", menuName = "Sombra da Sanidade/Config/Sanidade")]
    public class SanityConfig : ScriptableObject
    {
        public float maxSanity = 100f;

        [Header("Ralos (por segundo)")]
        [Tooltip("Escuridão total (GDD: -1.5%/s).")]
        public float darknessDrain = 1.5f;
        [Tooltip("Proximidade do monstro (GDD: -3%/s).")]
        public float monsterProximityDrain = 3f;
        [Tooltip("Distância em que o monstro conta como próximo. Não está no GDD: ajustar no playtest.")]
        public float monsterProximityRadius = 5f;
        [Tooltip("Custo imediato do Módulo Adrena-Syn (GDD: -20).")]
        public float adrenaSynCost = 20f;

        [Header("Fontes")]
        [Tooltip("Sob Zona Segura (GDD: +2%/s).")]
        public float safeZoneRegen = 2f;
        [Tooltip("Estabilizador de Lítio (GDD: +30).")]
        public float stabilizerAmount = 30f;

        [Header("Limites dos estados (GDD 4.2)")]
        public float anxiousBelow = 75f;
        public float hallucinatingBelow = 40f;
        public float collapsedBelow = 15f;
        [Tooltip("Pistas de enigma aparecem abaixo deste valor (GDD 4.4: 35%).")]
        public float revealCluesBelow = 35f;
    }
}
