using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Aplica o DifficultyProfile da ala na cena (Proposta 1 do v1.1, #30): velocidade, raio auditivo e
    // comportamentos do monstro, dreno no escuro, alucinações e tabela de loot dos armários.
    // Cada ala é uma cena, então o perfil ativo é o desta cena. Roda depois do Start do monstro (que lê os
    // comportamentos do MonsterConfig) para não ser sobrescrito.
    [DefaultExecutionOrder(100)]
    public class WingDifficulty : MonoBehaviour
    {
        public static DifficultyProfile Active { get; private set; }

        [SerializeField] private DifficultyProfile profile;

        public DifficultyProfile Profile => profile;
        public bool Applied { get; private set; }

        private void Awake()
        {
            if (profile == null) Debug.LogError($"{name}: WingDifficulty sem DifficultyProfile.", this);
            else Active = profile;
        }

        private void OnDestroy()
        {
            if (Active == profile) Active = null;
        }

        private void Start() => Apply();

        public void Apply()
        {
            if (profile == null) return;

            foreach (var monster in FindObjectsByType<MonsterController>(FindObjectsInactive.Include))
            {
                monster.SetBaseSpeed(profile.monsterSpeed);
                monster.SetHearingRadius(profile.hearingRadius);
                monster.SetBehaviours(profile.behaviours);
            }

            var sanity = FindAnyObjectByType<SanityController>();
            if (sanity != null) sanity.SetDarknessDrain(profile.darknessDrain);

            foreach (var hallucination in FindObjectsByType<SanityHallucination>())
                hallucination.SetSchedule(profile.hallucinations, profile.hallucinationInterval.x, profile.hallucinationInterval.y);

            if (profile.lootTable != null)
                foreach (var locker in FindObjectsByType<Locker>())
                    locker.SetTable(profile.lootTable);

            Applied = true;
        }
    }
}
