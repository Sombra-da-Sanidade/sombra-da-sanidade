using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Números da câmera de segurança (#79). Valores iniciais de docs/planejamento-rc.md seção 2 ("SecurityCamera").
    // Se a câmera chama o Espécime depende da ala: DifficultyProfile.camerasCallSpecimen.
    [CreateAssetMenu(fileName = "SecurityCamera", menuName = "Sombra da Sanidade/Config/Câmera de segurança")]
    public class SecurityCameraConfig : ScriptableObject
    {
        [Header("Visão")]
        [Tooltip("Alcance do cone, em unidades.")]
        [Min(0.5f)] public float range = 7f;
        [Tooltip("Abertura do cone, em graus.")]
        [Range(10f, 180f)] public float coneAngle = 50f;
        [Tooltip("Segundos vendo o Thomas até disparar o alarme (planejamento-rc 2: 0,6 s).")]
        [Min(0.05f)] public float detectTime = 0.6f;
        [Tooltip("Quanto a detecção perde por segundo quando o Thomas sai do cone (1 = esvazia em detectTime).")]
        [Min(0.1f)] public float detectDecay = 1f;

        [Header("Varredura")]
        [Tooltip("Ângulo total varrido em volta da direção inicial, em graus.")]
        [Range(0f, 270f)] public float sweepAngle = 90f;
        [Tooltip("Graus por segundo na varredura.")]
        [Min(1f)] public float sweepSpeed = 25f;
        [Tooltip("Graus por segundo ao seguir o Thomas enquanto detecta.")]
        [Min(1f)] public float trackSpeed = 120f;

        [Header("Alarme")]
        [Tooltip("Segundos de sirene (planejamento-rc 2: 5 s).")]
        [Min(0.5f)] public float alarmDuration = 5f;
        [Tooltip("Disjuntores a até esta distância formam o setor: o alarme apaga todos até o jogador religar.")]
        [Min(0f)] public float sectorRadius = 9f;
        [Tooltip("Raio do ruído emitido onde o Thomas está, quando a ala manda a câmera chamar o Espécime (fase 3).")]
        [Min(0f)] public float callNoiseRadius = 60f;

        [Header("EMP")]
        [Tooltip("Câmeras a até esta distância do jogador desligam quando o EMP dispara.")]
        [Min(0f)] public float empRadius = 4f;
        [Tooltip("Segundos desligada pelo EMP (planejamento-rc 2: 10 s).")]
        [Min(0f)] public float empDisableSeconds = 10f;

        [Header("Visual")]
        public Color coneIdleColor = new Color(0.55f, 0.75f, 1f);
        public Color coneDetectColor = new Color(1f, 0.75f, 0.3f);
        public Color coneAlarmColor = new Color(1f, 0.2f, 0.15f);
        [Range(0f, 2f)] public float coneIntensity = 0.35f;
    }
}
