using UnityEngine;

namespace SombraDaSanidade.Audio
{
    // Números do sistema de áudio (#101). Fica em Resources/AudioConfig, porque o AudioDirector é criado em
    // runtime em toda cena de jogo. Valores de docs/audio/sound-design.md.
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Sombra da Sanidade/Config/Áudio")]
    public class AudioConfig : ScriptableObject
    {
        public const string ResourcePath = "AudioConfig";

        [Header("Música em camadas (seção 4)")]
        [Tooltip("Segundos para uma camada entrar (L3 entra rápido no susto). sound-design.md 4.")]
        public float layerFadeIn = 0.3f;
        [Tooltip("Segundos para uma camada sair (recuo lento, curva dente-de-serra do GDD 7).")]
        public float layerFadeOut = 4f;
        [Tooltip("Tensão (0 a 1, TensionDirector) a partir da qual entra a camada L2 (pulso).")]
        [Range(0f, 1f)] public float pulseTension = 0.35f;
        [Tooltip("Volume de cada camada quando ativa.")]
        [Range(0f, 1f)] public float layerVolume = 0.8f;

        [Header("Ducking (seção 6)")]
        [Tooltip("dB a menos no Ambiente com o monstro em perseguição.")]
        public float chaseAmbienceDuckDb = -6f;
        [Tooltip("dB a menos na Música enquanto a Vance fala (log de áudio).")]
        public float voiceMusicDuckDb = -8f;
        [Tooltip("Segundos para entrar e sair do ducking.")]
        public float duckFade = 0.5f;

        [Header("Oclusão (seção 3)")]
        [Tooltip("Intervalo do raycast até o ouvinte (s).")]
        public float occlusionInterval = 0.1f;
        [Tooltip("dB a menos com parede entre a fonte e o ouvinte.")]
        public float occlusionDb = -6f;
        [Tooltip("Segundos do crossfade entre a versão limpa e a abafada.")]
        public float occlusionFade = 0.25f;

        [Header("Espacialização 2D (seção 3)")]
        [Range(0f, 1f)] public float spatialBlend = 0.8f;
        [Tooltip("Distância máxima padrão de sons do mundo (unidades).")]
        public float worldMaxDistance = 8f;
        [Tooltip("Distância máxima dos monstros (unidades).")]
        public float monsterMaxDistance = 14f;

        private static AudioConfig cached;

        // Sem asset (cena de teste antiga), usa os valores padrão acima.
        public static AudioConfig Load()
        {
            if (cached == null) cached = Resources.Load<AudioConfig>(ResourcePath);
            if (cached == null) cached = CreateInstance<AudioConfig>();
            return cached;
        }
    }
}
