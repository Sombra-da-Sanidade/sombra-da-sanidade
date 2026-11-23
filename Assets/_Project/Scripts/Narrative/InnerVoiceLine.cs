using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Fala interna do Thomas por faixa de sanidade (docs/narrativa/roteiro-rc.md seção 2, issue #89).
    // Estável: sanidade 100 a 40 (Lúcido/Ansioso). Abalado: sanidade 39 a 1 (Alucinando/Colapsado).
    [CreateAssetMenu(fileName = "InnerVoiceLine", menuName = "Sombra da Sanidade/Narrativa/Fala do Thomas")]
    public class InnerVoiceLine : ScriptableObject
    {
        [Tooltip("Identificador estável (T1-1, T2-4...). Cada fala toca uma única vez por partida.")]
        public string id = "T1-1";

        [Tooltip("Nome exibido na legenda.")]
        public string speaker = "THOMAS";

        [Tooltip("Tom dos bipes: uma oitava abaixo do da Vance (1.15 -> 0.575).")]
        [Range(0.35f, 1.5f)] public float babblePitch = 0.575f;

        [Tooltip("Texto quando a sanidade está Estável (40 a 100).")]
        [TextArea(1, 3)]
        public string[] stableLines = new string[0];

        [Tooltip("Texto quando a sanidade está Abalada (abaixo de 40).")]
        [TextArea(1, 3)]
        public string[] shakenLines = new string[0];

        public string[] PickLines(bool shaken)
        {
            if (shaken && shakenLines != null && shakenLines.Length > 0) return shakenLines;
            return stableLines != null && stableLines.Length > 0 ? stableLines : shakenLines;
        }

        public AudioLog ToRuntimeLog(bool shaken)
        {
            var log = CreateInstance<AudioLog>();
            log.hideFlags = HideFlags.DontSave;
            log.id = id;
            log.speaker = speaker;
            log.babblePitch = babblePitch;
            log.lines = PickLines(shaken);
            log.isInternalThought = true;
            return log;
        }
    }
}
