using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    public enum InnerVoiceTrigger
    {
        WingStart,          // começo da ala, depois da cutscene de abertura
        FirstDarkness,      // primeira vez no escuro total
        Area,               // entrar no LevelMarker de id = arg
        FirstBreaker,       // primeiro disjuntor consertado
        AfterCutscene,      // fim da cutscene de id = arg
        DriveCollected,     // HD da ala ejetado
        CameraSeen,         // primeira câmera de segurança vista
        CameraCall,         // primeira câmera que chama o Espécime
        SubstationPowered,  // subestação religada
        NeuralModule,       // Módulo Neural usado
        ClueRead,           // primeira pista da senha revelada perto do Thomas
        ChoiceSave,         // escolha na Eclusa: salvar a Vance
        ChoiceLeave,        // escolha na Eclusa: deixar a Vance
    }

    // Monólogo do Thomas (roteiro-rc 2, #89): uma fala com versão Estável (sanidade 40 a 100) e Abalado (abaixo de
    // 40). Toca uma vez por ala, com a legenda e a voz de bipes do AudioLogPlayer uma oitava abaixo da Vance.
    // Gerado pelo InnerVoiceAssetBuilder a partir do roteiro; não editar o asset à mão.
    [CreateAssetMenu(fileName = "InnerVoiceLine", menuName = "Sombra da Sanidade/Narrativa/Fala do Thomas")]
    public class InnerVoiceLine : ScriptableObject
    {
        public const float ShakenBelow = 40f;   // GDD 4: Alucinando e Colapsado (39 a 1)
        public const float ThomasPitch = 0.575f;   // metade do bipe da Vance (1.15): uma oitava abaixo

        [Tooltip("Id do roteiro (T1-1, T2-3...).")]
        public string id = "T1-1";
        [Tooltip("Ala onde a fala vale (Medica, Energia, Executiva, Eclusa).")]
        public string wing = "Medica";
        public InnerVoiceTrigger trigger;
        [Tooltip("Argumento do gatilho: id do LevelMarker (Area) ou da cutscene (AfterCutscene).")]
        public string arg = "";
        [TextArea(1, 3)] public string stable = "";
        [TextArea(1, 3)] public string shaken = "";

        public string TextFor(float sanity) => sanity < ShakenBelow && shaken.Length > 0 ? shaken : stable;

        // Fita de uma linha para o AudioLogPlayer: sem clique nem chiado, fora da lista de logs ouvidos.
        public AudioLog ToLog(float sanity)
        {
            var log = CreateInstance<AudioLog>();
            log.name = id;
            log.id = id;
            log.speaker = "THOMAS";
            log.babblePitch = ThomasPitch;
            log.tape = false;
            log.lines = new[] { TextFor(sanity) };
            return log;
        }
    }
}
