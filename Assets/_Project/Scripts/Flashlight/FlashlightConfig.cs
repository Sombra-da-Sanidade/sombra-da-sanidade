using UnityEngine;

namespace SombraDaSanidade.Flashlight
{
    // Valores do GDD 4.2, 6.2 e 6.3 (Lanterna Mod. Lux-90).
    [CreateAssetMenu(fileName = "FlashlightConfig", menuName = "Sombra da Sanidade/Config/Lanterna")]
    public class FlashlightConfig : ScriptableObject
    {
        [Header("Bateria")]
        public float maxBattery = 100f;
        [Tooltip("Dreno por segundo com a lanterna ligada (GDD: -1%/s).")]
        public float drainPerSecond = 1f;
        [Tooltip("Carga recuperada por bateria sobressalente (GDD: +30%).")]
        public float batteryPickupAmount = 30f;

        [Header("Cone de luz")]
        [Tooltip("Abertura total do cone em graus (GDD: 60).")]
        public float coneAngle = 60f;
        [Tooltip("Alcance em unidades (GDD: 8 m).")]
        public float range = 8f;
        public float intensity = 1.2f;
        [Tooltip("Fração da intensidade quando a bateria está quase no fim.")]
        [Range(0f, 1f)] public float lowBatteryIntensityRatio = 0.2f;

        [Header("Pulso EMP")]
        [Tooltip("Custo de carga por disparo (GDD: 40%).")]
        public float empCost = 40f;
        [Tooltip("Tempo de atordoamento do monstro em segundos (GDD: 4s).")]
        public float empStunDuration = 4f;
    }
}
