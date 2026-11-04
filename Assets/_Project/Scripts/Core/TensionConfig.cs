using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Números do diretor de tensão (#35). Base: Proposta 7 (docs/pesquisa/propostas.md) e o ciclo do
    // Left 4 Dead (docs/pesquisa/ritmo-e-tensao.md). Valores iniciais para playtest; podem mudar com o GDD v1.1 (#3).
    [CreateAssetMenu(fileName = "TensionConfig", menuName = "Sombra da Sanidade/Config/Tensão")]
    public class TensionConfig : ScriptableObject
    {
        [Header("O que faz a tensão subir (por segundo, escala 0 a 1)")]
        [Tooltip("Monstro a menos desta distância do jogador conta como perto.")]
        public float proximityRadius = 7f;
        [Tooltip("Ganho com o monstro colado no jogador; cai até 0 na borda do raio.")]
        public float proximityGain = 0.08f;
        [Tooltip("Com parede no meio (outro cômodo) a proximidade pesa menos, como no Alien: Isolation.")]
        [Range(0f, 1f)] public float outOfSightFactor = 0.4f;
        [Tooltip("Cone da lanterna tocando o monstro.")]
        public float litGain = 0.12f;
        [Tooltip("Perseguição ativa.")]
        public float chaseGain = 0.08f;
        [Tooltip("Pulo instantâneo numa falha de skill check (#33).")]
        public float skillCheckFailGain = 0.2f;

        [Header("O que faz a tensão cair")]
        [Tooltip("Queda por segundo fora da perseguição (não cai enquanto ele persegue, como no Left 4 Dead).")]
        public float decayPerSecond = 0.03f;
        [Tooltip("Queda por segundo no Relax.")]
        public float relaxDecayPerSecond = 0.08f;

        [Header("Ciclo (Proposta 7)")]
        [Tooltip("Sustain Peak: segundos no pico antes de recuar (sorteado entre os dois).")]
        public Vector2 sustainPeakSeconds = new Vector2(3f, 5f);
        [Tooltip("Relax: segundos com o monstro patrulhando longe do jogador (sorteado entre os dois).")]
        public Vector2 relaxSeconds = new Vector2(30f, 45f);
        [Tooltip("No Relax, de quanto em quanto tempo o 'longe do jogador' é recalculado.")]
        public float retreatRefreshSeconds = 3f;
    }
}
