using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Valores do GDD 4.2, 4.4 e 4.6 (Espécime Vazio).
    // Os marcados "não está no GDD" são chutes para o playtest (docs/monstro.md).
    [CreateAssetMenu(fileName = "MonsterConfig", menuName = "Sombra da Sanidade/Config/Monstro")]
    public class MonsterConfig : ScriptableObject
    {
        [Header("Velocidade")]
        [Tooltip("Velocidade inicial (GDD: 3.5 m/s).")]
        public float baseSpeed = 3.5f;
        [Tooltip("Teto de velocidade (GDD: 4.8 m/s).")]
        public float maxSpeed = 4.8f;
        [Tooltip("Aumento por HD coletado (GDD: +10%).")]
        public float speedBonusPerDrive = 0.10f;
        [Tooltip("Aceleração do NavMeshAgent. Alta para ele não derrapar nas curvas. Não está no GDD.")]
        public float acceleration = 30f;

        [Header("Patrulha")]
        [Tooltip("Chance de seguir o último ruído em um cruzamento (GDD: 70%).")]
        [Range(0f, 1f)] public float followNoiseChance = 0.7f;
        [Tooltip("Escuta qualquer ruído a até esta distância, mesmo fora do raio do emissor.")]
        public float hearingRadius = 6f;
        [Tooltip("Pausa em cada ponto de patrulha antes de decidir o próximo. Não está no GDD.")]
        public float waypointPause = 1f;
        [Tooltip("Por quanto tempo o último ruído vale para a regra dos 70%. Não está no GDD.")]
        public float noiseMemory = 20f;

        [Header("Investigação auditiva")]
        [Tooltip("Tempo parado escutando no ponto do ruído antes de voltar a patrulhar. Não está no GDD.")]
        public float listenDuration = 3f;

        [Header("Perseguição (GDD regra 2)")]
        [Tooltip("Grito de estática ao ser tocado pela luz, parado, antes de correr. " +
                 "Não está no GDD: é o aviso para o jogador apagar a lanterna.")]
        public float detectionDelay = 0.8f;
        [Tooltip("Depois que a luz sai dele, continua sabendo onde o jogador está por este tempo. " +
                 "Evita fuga de graça apagando a lanterna no último instante. Não está no GDD.")]
        public float chaseMemory = 1.5f;
        [Tooltip("Alturas acima dos pés testadas contra o cone de luz (o sprite tem ~3 m).")]
        public float[] lightSampleHeights = { 0.2f, 1.4f, 2.6f };

        [Header("Zona Segura (GDD regra 6)")]
        [Tooltip("Distância que ele mantém da borda de uma Zona Segura ativa.")]
        public float safeZoneMargin = 0.6f;

        [Header("Ajuste de sanidade baixa (GDD 5)")]
        [Tooltip("Abaixo desta sanidade o monstro demora mais para procurar.")]
        public float mercySanityThreshold = 15f;
        [Tooltip("Atraso extra na busca (GDD: 5%). Vale para o grito de detecção, a escuta e as pausas.")]
        public float mercySearchDelay = 0.05f;
    }
}
