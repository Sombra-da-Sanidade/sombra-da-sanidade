using UnityEngine;

namespace SombraDaSanidade.Audio
{
    // Música de uma fase em camadas verticais (docs/audio/sound-design.md, seção 4): L0 ambiente, L1 drone,
    // L2 pulso, L3 perseguição. Os stems têm a mesma duração e tocam juntos com PlayScheduled (ADR 0005).
    // Stems de música: import CompressedInMemory.
    [CreateAssetMenu(fileName = "MusicSet", menuName = "Sombra da Sanidade/Áudio/Música em camadas")]
    public class MusicSet : ScriptableObject
    {
        [Tooltip("L0 Ambiente, L1 Drone, L2 Pulso, L3 Perseguição. Mesma duração.")]
        public AudioClip[] layers = new AudioClip[4];

        public int Count => layers != null ? layers.Length : 0;
    }
}
