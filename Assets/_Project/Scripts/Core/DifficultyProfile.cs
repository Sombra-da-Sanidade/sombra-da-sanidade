using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Dificuldade de uma ala (#30, Proposta 1 do GDD v1.1, tabela em docs/gdd/gdd-v1.1-propostas.md).
    // O GameManager expõe o perfil ativo e os sistemas leem daqui. Sem perfil na cena, cada sistema
    // continua usando o próprio config. Detalhes em docs/dificuldade.md.
    [CreateAssetMenu(fileName = "Difficulty", menuName = "Sombra da Sanidade/Config/Dificuldade da ala")]
    public class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Nome da ala (Medica, Energia, Executiva, Eclusa). Casa com o checkpoint (#45).")]
        public string wing = "Medica";

        // Atalho para os sistemas: null quando não há GameManager ou perfil na cena.
        public static DifficultyProfile Active =>
            GameManager.Instance != null ? GameManager.Instance.ActiveProfile : null;

        [Header("Monstro")]
        [Tooltip("Velocidade ao andar, em m/s (Proposta 1: 3.5 / 3.85 / 4.2 / 4.6). Substitui o bônus por HD do " +
                 "MonsterConfig, que somaria duas vezes o mesmo aumento.")]
        public float monsterSpeed = 3.5f;
        [Tooltip("Escuta qualquer ruído a até esta distância (Proposta 1: 5 / 6 / 7 / 9).")]
        public float monsterHearingRadius = 5f;
        [Tooltip("Comportamentos ligados nesta ala (#36 e #37, Proposta 8).")]
        public MonsterBehaviour monsterBehaviours = MonsterWingBehaviours.Medica;
        [Tooltip("Segundos até o monstro aparecer. 0 = presente desde a entrada. A Médica usa o WingIntro.")]
        [Min(0f)] public float monsterDelaySeconds;

        [Header("Sanidade")]
        [Tooltip("Dreno por segundo no escuro (Proposta 1: 1.0 / 1.5 / 2.0 / 2.0).")]
        public float darknessDrain = 1f;
        [Tooltip("Desligado: esta ala não tem alucinações (Proposta 1: não ocorrem na Médica).")]
        public bool hallucinations;
        [Tooltip("Segundos entre alucinações, mínimo (x) e máximo (y) (Proposta 1: 20-30 / 15-25 / 10-15).")]
        public Vector2 hallucinationInterval = new Vector2(20f, 30f);

        [Header("Armários")]
        [Tooltip("Tabela de loot da ala (Config/Loot/), com a chance de bateria da Proposta 1 (70% / 60% / 45% / 0%). " +
                 "Armários sem tabela própria usam esta.")]
        public LootTable lootTable;
        [Tooltip("Chance de bateria por armário (0 a 1). Quem sorteia é a tabela de loot; o teste da ala confere os dois.")]
        [Range(0f, 1f)] public float batteryChance = 0.7f;

        [Header("Mapa")]
        [Tooltip("Disjuntores no mapa da ala (Proposta 1: 3 / 3 / 2 / 0). O builder da ala confere.")]
        [Min(0)] public int breakersAvailable = 3;
    }
}
