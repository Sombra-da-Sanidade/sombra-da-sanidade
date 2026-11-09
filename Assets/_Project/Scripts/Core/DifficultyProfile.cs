using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Dificuldade de uma ala (Proposta 1 do v1.1, issue #30). Um asset por ala; o GameManager da cena aplica o
    // perfil no Awake em cópias dos configs (os assets compartilhados nunca são alterados).
    [CreateAssetMenu(menuName = "Sombra da Sanidade/Difficulty Profile", fileName = "Difficulty_")]
    public class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Nome da ala (Medica, Energia, Executiva, Eclusa). Casa com o checkpoint.")]
        public string wing = "Medica";

        [Header("Monstro")]
        [Tooltip("Velocidade base da ala. Já inclui os +10% dos HDs de alas anteriores.")]
        public float monsterSpeed = 3.5f;
        public float hearingRadius = 5f;
        [Tooltip("Comportamentos da ala (Proposta 8 / #37).")]
        public MonsterBehaviour monsterBehaviours = MonsterBehaviour.Search;
        [Tooltip("Segundos até o monstro aparecer. 0 = desde a entrada.")]
        public float monsterDelaySeconds;

        [Header("Sanidade")]
        [Tooltip("Dreno de sanidade no escuro, por segundo.")]
        public float darknessDrain = 1f;
        public bool hallucinationsEnabled;
        public float hallucinationMinInterval = 20f;
        public float hallucinationMaxInterval = 30f;

        [Header("Mapa")]
        [Tooltip("Chance de bateria por armário. A tabela de loot da ala deve bater com este valor.")]
        [Range(0f, 1f)] public float batteryChance = 0.7f;
        [Tooltip("Disjuntores no mapa da ala. O builder confere.")]
        public int breakersAvailable = 3;

        public MonsterConfig ApplyTo(MonsterConfig source)
        {
            if (source == null) return null;
            var copy = Instantiate(source);
            copy.name = source.name + " (" + wing + ")";
            copy.baseSpeed = monsterSpeed;
            copy.maxSpeed = Mathf.Max(copy.maxSpeed, monsterSpeed);
            copy.hearingRadius = hearingRadius;
            copy.behaviours = monsterBehaviours;
            return copy;
        }

        public SanityConfig ApplyTo(SanityConfig source)
        {
            if (source == null) return null;
            var copy = Instantiate(source);
            copy.name = source.name + " (" + wing + ")";
            copy.darknessDrain = darknessDrain;
            if (hallucinationsEnabled)
            {
                copy.hallucinationMinInterval = hallucinationMinInterval;
                copy.hallucinationMaxInterval = Mathf.Max(hallucinationMinInterval, hallucinationMaxInterval);
            }
            else
            {
                // Sem alucinação na ala: o intervalo nunca vence.
                copy.hallucinationMinInterval = copy.hallucinationMaxInterval = float.MaxValue;
            }
            return copy;
        }
    }
}
