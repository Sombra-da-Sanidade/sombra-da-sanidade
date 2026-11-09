using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Dificuldade de uma ala (Proposta 1 do GDD v1.1, issue #30): vários eixos sobem juntos, de forma visível
    // e previsível. Um asset por ala em ScriptableObjects/Config/Difficulty/, gerado com os valores da tabela
    // por DifficultyProfileBuilder. Quem aplica na cena é o Level.WingDifficulty; os sistemas leem dele.
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Sombra da Sanidade/Config/Perfil de dificuldade")]
    public class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Nome da ala (Medica, Energia, Executiva, Eclusa).")]
        public string wing = "Medica";

        [Header("Monstro")]
        [Tooltip("Velocidade base na ala. Os HDs desta ala somam +10% cada em cima dela (GDD regra 4).")]
        public float monsterSpeed = 3.5f;
        [Tooltip("Raio em que ele ouve qualquer ruído, mesmo baixo.")]
        public float hearingRadius = 5f;
        [Tooltip("Comportamentos ligados (MonsterWingBehaviours, #36/#37).")]
        public MonsterBehaviour behaviours = MonsterWingBehaviours.Medica;
        [Tooltip("Segundos até o monstro aparecer. 0 = presente desde a entrada. A Médica usa o WingIntro.")]
        [Min(0f)] public float monsterDelaySeconds;

        [Header("Sanidade")]
        [Tooltip("Dreno por segundo no escuro.")]
        public float darknessDrain = 1f;
        [Tooltip("Desligado: não há alucinações na ala.")]
        public bool hallucinations;
        [Tooltip("Intervalo entre alucinações (s).")]
        public Vector2 hallucinationInterval = new Vector2(20f, 30f);

        [Header("Recursos")]
        [Tooltip("Chance de bateria por armário (0 a 1). Quem sorteia é a tabela de loot abaixo; o teste confere os dois.")]
        [Range(0f, 1f)] public float batteryChance = 0.7f;
        [Tooltip("Tabela de loot dos armários da ala.")]
        public LootTable lootTable;
        [Tooltip("Disjuntores na ala (a planta tem que bater, o teste confere).")]
        [Min(0)] public int breakers = 3;
    }
}
