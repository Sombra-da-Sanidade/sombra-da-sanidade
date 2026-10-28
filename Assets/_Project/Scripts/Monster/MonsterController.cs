using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Monster
{
    public enum MonsterState { Patrol, Investigate, Chase, Stunned }

    // Espécime Vazio (GDD 4.2). Frente 2 (Monstro). Issues #4 e #5.
    // A API pública abaixo é o contrato usado por Lanterna (EMP), Sanidade e HDs: não mudar sem avisar o grupo.
    public class MonsterController : MonoBehaviour
    {
        [SerializeField] private MonsterConfig config;

        public MonsterState State { get; private set; } = MonsterState.Patrol;
        public bool IsStunned => State == MonsterState.Stunned;

        // Multiplicador acumulado pelos HDs coletados (GDD regra 4).
        public float SpeedMultiplier { get; private set; } = 1f;

        public float CurrentSpeed => config != null
            ? Mathf.Min(config.baseSpeed * SpeedMultiplier, config.maxSpeed)
            : 0f;

        private void OnEnable()
        {
            NoiseBus.NoiseEmitted += OnNoise;
        }

        private void OnDisable()
        {
            NoiseBus.NoiseEmitted -= OnNoise;
        }

        // Start, não OnEnable: garante que o Awake do GameManager já rodou.
        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDriveCollected;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
        }

        // Chamado pelo pulso EMP.
        public void Stun(float duration)
        {
            // TODO(Frente 2): parar o agente por `duration` segundos e voltar para Patrol ou Chase.
            State = MonsterState.Stunned;
        }

        // Chamado quando o cone da lanterna toca o monstro ou um ruído é ouvido.
        public void Alert(Vector2 position)
        {
            // TODO(Frente 2): ir até `position` (Investigate) ou perseguir o jogador (Chase).
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = multiplier;
            // TODO(Frente 2): aplicar CurrentSpeed no NavMeshAgent.
        }

        private void OnNoise(Vector2 position, float radius)
        {
            if (((Vector2)transform.position - position).sqrMagnitude <= radius * radius)
                Alert(position);
        }

        private void OnDriveCollected(int total)
        {
            if (config != null) SetSpeedMultiplier(1f + config.speedBonusPerDrive * total);
        }

        // TODO(Frente 2): patrulha por waypoints (NavMeshPlus), perseguição, atração pela luz
        // via FlashlightController.IsPointInCone e GameManager.TriggerGameOver(DeathCause.Monster) ao encostar.
    }
}
