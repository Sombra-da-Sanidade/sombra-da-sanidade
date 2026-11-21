using UnityEngine;

namespace SombraDaSanidade.Audio
{
    // Um som do jogo (#101): várias tomadas, variação de pitch e volume, grupo do mixer, distância e, se o som
    // também é ouvido pelo Espécime, o raio no NoiseBus. Criado pelo builder a partir de "nome_NN.wav" (#102).
    // A versão abafada (_muffled) é usada pela AudioOcclusion em sons contínuos; ADR 0005.
    [CreateAssetMenu(fileName = "SoundEvent", menuName = "Sombra da Sanidade/Áudio/Som")]
    public class SoundEvent : ScriptableObject
    {
        [Tooltip("Tomadas do som (3 a 8 para sons repetidos, sound-design.md seção 5).")]
        public AudioClip[] clips = new AudioClip[0];
        [Tooltip("Mesmas tomadas, abafadas (opcional; mesmo tamanho de clips).")]
        public AudioClip[] muffledClips = new AudioClip[0];
        public AudioBus bus = AudioBus.Sfx;

        [Tooltip("Volume sorteado entre x e y.")]
        public Vector2 volume = new Vector2(0.9f, 1f);
        [Tooltip("Pitch sorteado entre x e y.")]
        public Vector2 pitch = new Vector2(0.95f, 1.05f);

        [Tooltip("Posicional no mundo (pan e distância). Desligado: toca direto, como UI e voz.")]
        public bool spatial = true;
        [Tooltip("Distância máxima (unidades). 0: padrão do AudioConfig para o grupo.")]
        public float maxDistance;

        [Tooltip("Raio de ruído no NoiseBus quando o som toca (0: o Espécime não ouve). GDD 4 e 6.")]
        public float noiseRadius;
        [Tooltip("Intervalo mínimo entre duas execuções (s).")]
        public float cooldown;

        [System.NonSerialized] private int lastIndex = -1;
        [System.NonSerialized] private float lastPlayed = float.NegativeInfinity;

        public bool HasClips => clips != null && clips.Length > 0;

        // Sorteia uma tomada diferente da anterior (evita o "efeito metralhadora").
        public int NextIndex()
        {
            if (!HasClips) return -1;
            if (clips.Length == 1) return lastIndex = 0;
            int i = Random.Range(0, clips.Length - 1);
            if (i >= lastIndex && lastIndex >= 0) i++;
            return lastIndex = i;
        }

        public AudioClip MuffledFor(int index) =>
            muffledClips != null && index >= 0 && index < muffledClips.Length ? muffledClips[index] : null;

        public bool TryConsumeCooldown(float now)
        {
            if (now < lastPlayed) lastPlayed = float.NegativeInfinity;   // Play novo no Editor: o relógio voltou a zero
            if (now - lastPlayed < cooldown) return false;
            lastPlayed = now;
            return true;
        }

        public void ResetRuntimeState()
        {
            lastIndex = -1;
            lastPlayed = float.NegativeInfinity;
        }
    }
}
