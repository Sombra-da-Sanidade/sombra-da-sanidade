using UnityEngine;

namespace SombraDaSanidade.Player
{
    // Valores do GDD 6.2 (Thomas Reis).
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Sombra da Sanidade/Config/Player")]
    public class PlayerConfig : ScriptableObject
    {
        [Tooltip("Velocidade base em unidades/s (GDD: 4.0 m/s).")]
        public float moveSpeed = 4f;
    }
}
