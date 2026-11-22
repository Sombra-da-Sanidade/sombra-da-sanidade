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

        [Header("Faixas fixas (seção 4, #103). Ligadas por Sombra da Sanidade > Áudio > Gerar SoundEvents")]
        [Tooltip("Menu: drone com a contagem invertida.")]
        public AudioClip menuMusic;
        [Tooltip("Zona Segura: única faixa calma.")]
        public AudioClip safeRoomMusic;
        [Tooltip("Purga da Eclusa: 150 s exatos, o próprio cronômetro (GDD 8: o tempo não aparece em número).")]
        public AudioClip purgeMusic;
        [Tooltip("Game Over: corte seco e zumbido.")]
        public AudioClip gameOverSting;
        [Tooltip("Laudo (vitória na última fase).")]
        public AudioClip laudoMusic;
        [Tooltip("Segundos de crossfade entre a música da fase e uma faixa fixa.")]
        public float fixedFade = 1.5f;

        [Header("Efeitos de gameplay (#105)")]
        [Tooltip("Distância (unidades) entre dois passos do Thomas andando.")]
        public float playerStepDistance = 0.85f;
        [Tooltip("Distância entre dois passos do Espécime (mais pesado, passada maior).")]
        public float specimenStepDistance = 1.1f;
        [Tooltip("Abaixo desta fração de bateria a Lux-90 avisa que está fraca (sound-design.md seção 5: 20%).")]
        [Range(0f, 1f)] public float batteryLowRatio = 0.2f;
        [Tooltip("Segundos entre dois avisos de bateria fraca.")]
        public float batteryLowInterval = 6f;
        [Tooltip("Volume do zumbido contínuo da lanterna acesa.")]
        [Range(0f, 1f)] public float flashlightHumVolume = 0.12f;
        [Tooltip("Volume da respiração do Espécime patrulhando / em perseguição.")]
        public Vector2 specimenBreathVolume = new Vector2(0.55f, 1f);
        [Tooltip("Ticks do ponteiro do multímetro por varredura.")]
        public int skillTicksPerSweep = 8;

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
