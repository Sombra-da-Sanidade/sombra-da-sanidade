using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Valores das interações do GDD 4.3 (Ejetar Gaveta de HD).
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Sombra da Sanidade/Config/Interação")]
    public class InteractionConfig : ScriptableObject
    {
        [Header("Gaveta de HD")]
        [Tooltip("Segundos segurando E para ejetar o HD (GDD 4.3). Valor não está no GDD: ajustar no playtest.")]
        public float ejectDuration = 2f;
        [Tooltip("Raio do barulho ao ejetar o HD (GDD 4.3: ejetar faz barulho). Valor não está no GDD.")]
        public float ejectNoiseRadius = 10f;
    }
}
