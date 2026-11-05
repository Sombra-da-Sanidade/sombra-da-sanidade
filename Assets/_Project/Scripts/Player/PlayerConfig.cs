using UnityEngine;

namespace SombraDaSanidade.Player
{
    // Valores do GDD 6.2 (Thomas Reis) e do Módulo Adrena-Syn (GDD 6).
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Sombra da Sanidade/Config/Player")]
    public class PlayerConfig : ScriptableObject
    {
        [Tooltip("Velocidade base em unidades/s (GDD: 4.0 m/s).")]
        public float moveSpeed = 4f;

        [Header("Módulo Adrena-Syn (GDD 6)")]
        [Tooltip("Bônus de velocidade do Adrena-Syn (GDD 6: +35%). O custo de -20 de sanidade fica no SanityConfig.")]
        [Min(0f)] public float adrenaSynSpeedBonus = 0.35f;
        [Tooltip("Duração do Adrena-Syn em segundos (GDD 6: 15 s).")]
        [Min(0f)] public float adrenaSynDuration = 15f;
    }
}
