using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Valores do GDD 4.2, 4.4 e 4.6 (Espécime Vazio).
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

        [Header("Patrulha")]
        [Tooltip("Chance de seguir o último ruído em um cruzamento (GDD: 70%).")]
        [Range(0f, 1f)] public float followNoiseChance = 0.7f;
        public float hearingRadius = 6f;

        [Header("Ajuste de sanidade baixa (GDD 5)")]
        [Tooltip("Abaixo desta sanidade o monstro demora mais para procurar.")]
        public float mercySanityThreshold = 15f;
        [Tooltip("Atraso extra na busca (GDD: 5%).")]
        public float mercySearchDelay = 0.05f;
    }
}
