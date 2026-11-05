using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Números dos logs de áudio (issue #39). Nenhum está no GDD: ajustar no playtest.
    [CreateAssetMenu(fileName = "NarrativeConfig", menuName = "Sombra da Sanidade/Config/Narrativa")]
    public class NarrativeConfig : ScriptableObject
    {
        [Header("Terminais")]
        [Tooltip("Segundos segurando E para começar a ouvir.")]
        public float holdDuration = 0.6f;
        [Tooltip("Jogador a mais desta distância (m) do terminal deixa de ouvir: a fita para e pode ser ouvida de novo.")]
        public float hearingRadius = 7f;

        [Header("Sinal perdido")]
        [Tooltip("Monstro a menos desta distância (m) do jogador pausa a gravação com estática. Ela volta quando ele se afasta.")]
        public float interruptRadius = 4.5f;

        [Header("Legenda digitada")]
        [Tooltip("Letras por segundo ao digitar a legenda (sem voz gravada).")]
        public float charactersPerSecond = 26f;
        [Tooltip("Pausa extra depois de vírgula.")]
        public float commaPause = 0.15f;
        [Tooltip("Pausa extra depois de ponto, exclamação, interrogação ou reticências.")]
        public float sentencePause = 0.35f;
        [Tooltip("Segundos que a legenda fica na tela depois de digitada, no mínimo.")]
        public float minHoldSeconds = 1.2f;
        [Tooltip("Segundos a mais de leitura por letra, somados ao mínimo.")]
        public float holdPerCharacter = 0.02f;
        [Tooltip("Pausa entre uma legenda e a próxima.")]
        public float pauseBetweenLines = 0.35f;

        [Header("Voz de bipes (sem voz gravada)")]
        [Tooltip("Volume dos bipes de fala.")]
        [Range(0f, 1f)] public float babbleVolume = 0.35f;
        [Tooltip("Intervalo mínimo entre dois bipes: com digitação rápida, nem toda letra soa.")]
        public float babbleMinInterval = 0.055f;
        [Tooltip("Volume do chiado de fita enquanto a gravação toca.")]
        [Range(0f, 1f)] public float tapeHissVolume = 0.06f;
    }
}
