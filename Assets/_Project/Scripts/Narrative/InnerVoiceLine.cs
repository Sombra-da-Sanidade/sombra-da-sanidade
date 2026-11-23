using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Monólogo do Thomas (roteiro-rc 2, #89): uma fala com versão Estável (sanidade 40 a 100) e Abalado (abaixo de
    // 40). Assets em Resources/Narrative/Voices; quem toca é o WingNarrativeDirector (por evento) e o
    // InnerVoiceTrigger (por área, '"' na planta).
    [CreateAssetMenu(fileName = "InnerVoiceLine", menuName = "Sombra da Sanidade/Narrativa/Fala do Thomas")]
    public class InnerVoiceLine : ScriptableObject
    {
        public const float ShakenBelow = 40f;   // GDD 4: Alucinando e Colapsado (39 a 1)
        public const float ThomasPitch = 0.575f;   // metade do bipe da Vance (1.15): uma oitava abaixo

        [Tooltip("Id do roteiro (T1-1, T2-3...).")]
        public string id = "T1-1";
        [Tooltip("Ala onde a fala vale (Medica, Energia, Executiva, Eclusa).")]
        public string wing = "Medica";
        [Tooltip("Quem fala na legenda.")]
        public string speaker = "THOMAS";
        [Tooltip("Tom da voz de bipes (0.575 = uma oitava abaixo da Vance).")]
        [Range(0.35f, 1.5f)] public float babblePitch = ThomasPitch;
        [TextArea(1, 3)] public string stable = "";
        [TextArea(1, 3)] public string shaken = "";

        public string TextFor(float sanity) => sanity < ShakenBelow && shaken.Length > 0 ? shaken : stable;

        // Compatibilidade com as versões de asset antigas: o player legado usa os vetores estáveis/abalados.
        [HideInInspector] [TextArea(1, 3)] public string[] stableLines = new string[0];
        [HideInInspector] [TextArea(1, 3)] public string[] shakenLines = new string[0];

        public string[] PickLines(bool shaken) =>
            shaken && shakenLines != null && shakenLines.Length > 0 ? shakenLines
            : stableLines != null && stableLines.Length > 0 ? stableLines
            : new[] { TextFor(shaken ? 20f : 80f) };

        public AudioLog ToRuntimeLog(bool shaken)
        {
            var log = CreateInstance<AudioLog>();
            log.hideFlags = HideFlags.DontSave;
            log.id = id;
            log.speaker = speaker;
            log.babblePitch = babblePitch;
            log.lines = PickLines(shaken);
            log.isInternalThought = true;
            log.tape = false;
            return log;
        }

        // Fita de uma linha para o AudioLogPlayer: sem clique nem chiado, fora da lista de logs ouvidos.
        public AudioLog ToLog(float sanity)
        {
            var log = CreateInstance<AudioLog>();
            log.name = id;
            log.id = id;
            log.speaker = speaker;
            log.babblePitch = babblePitch;
            log.tape = false;
            log.lines = new[] { TextFor(sanity) };
            return log;
        }
    }
}
