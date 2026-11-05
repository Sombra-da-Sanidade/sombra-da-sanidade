using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Uma gravação da Dra. Vance (GDD 9, issue #39). O texto fica no asset para ser revisado sem mexer em código;
    // a versão de leitura dos roteiros está em docs/narrativa/logs.md.
    [CreateAssetMenu(fileName = "AudioLog", menuName = "Sombra da Sanidade/Narrativa/Log de áudio")]
    public class AudioLog : ScriptableObject
    {
        [Tooltip("Identificador estável, ex.: VANCE-01. Usado para lembrar quais logs o jogador já ouviu.")]
        public string id = "VANCE-01";

        [Tooltip("Quem fala, como aparece na legenda.")]
        public string speaker = "VANCE";

        [Tooltip("Título curto da fita, para a lista de logs ouvidos.")]
        public string title = "";

        [Tooltip("Voz gravada. Vazio: a legenda é digitada letra a letra com a voz de bipes.")]
        public AudioClip voice;

        [Tooltip("Tom da voz de bipes: 1 é neutro, acima é mais agudo. Cada personagem pode ter o seu.")]
        [Range(0.5f, 2f)] public float babblePitch = 1.15f;

        [Tooltip("Uma legenda por linha, na ordem em que são faladas.")]
        [TextArea(2, 4)]
        public string[] lines = new string[0];
    }
}
