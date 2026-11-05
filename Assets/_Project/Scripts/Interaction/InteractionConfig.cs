using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Valores das interações do GDD 4.3 (Ejetar Gaveta de HD) e dos armários (GDD 6).
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Sombra da Sanidade/Config/Interação")]
    public class InteractionConfig : ScriptableObject
    {
        [Header("Gaveta de HD")]
        [Tooltip("Segundos segurando E para ejetar o HD (GDD 4.3). Valor não está no GDD: ajustar no playtest.")]
        public float ejectDuration = 2f;
        [Tooltip("Raio do barulho ao ejetar o HD (GDD 4.3: ejetar faz barulho). Valor não está no GDD.")]
        public float ejectNoiseRadius = 10f;

        [Header("Armário")]
        [Tooltip("Segundos segurando E para abrir um armário. Valor não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float lockerOpenDuration = 1f;
        [Tooltip("Raio do barulho ao abrir o armário (porta de metal rangendo). Valor não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float lockerNoiseRadius = 6f;
        [Tooltip("Chance de o armário ter bateria ou sedativo (GDD 6: 60% por armário).")]
        [Range(0f, 1f)] public float lockerLootChance = 0.6f;
        [Tooltip("Dos armários com loot, fração que tem bateria (o resto tem sedativo). GDD 6 não separa: ajustar no playtest.")]
        [Range(0f, 1f)] public float lockerBatteryShare = 0.5f;
    }
}
