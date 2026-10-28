using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace SombraDaSanidade.Monster
{
    public enum MonsterState { Patrol, Investigate, Chase, Stunned }

    // Espécime Vazio (GDD 4.2). Frente 2 (Monstro). Issues #4 e #5.
    // A API pública abaixo é o contrato usado por Lanterna (EMP), Sanidade e HDs: não mudar sem avisar o grupo.
    //
    // Estados (GDD 4.2): Patrulha Cega -> Investigação Auditiva (ruído) -> Perseguição Furiosa (luz) -> Atordoado (EMP).
    // Ele é cego: só encontra o jogador pela luz da lanterna ou pelo som. Comportamento detalhado em docs/monstro.md.
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterController : MonoBehaviour
    {
        [SerializeField] private MonsterConfig config;

        [Tooltip("Pontos de patrulha (cruzamentos dos corredores). Vazio: sorteia pontos no NavMesh em volta do início.")]
        [SerializeField] private Transform[] patrolPoints;
        [Tooltip("Raio do sorteio quando não há pontos de patrulha.")]
        [SerializeField] private float randomPatrolRadius = 12f;

        [Header("Referências (vazias: procura na cena)")]
        [SerializeField] private FlashlightController flashlight;
        [SerializeField] private Transform player;
        [SerializeField] private SanityController sanity;

        public MonsterState State { get; private set; } = MonsterState.Patrol;
        public bool IsStunned => State == MonsterState.Stunned;

        // Grito de estática logo após a luz tocá-lo (início da Perseguição). Fica parado, mas encostar ainda mata.
        public bool IsDetecting { get; private set; }

        // Multiplicador acumulado pelos HDs coletados (GDD regra 4).
        public float SpeedMultiplier { get; private set; } = 1f;

        public float CurrentSpeed => config != null
            ? Mathf.Min(config.baseSpeed * SpeedMultiplier, config.maxSpeed)
            : 0f;

        // Velocidade real no NavMesh, para animação e áudio.
        public Vector2 Velocity => agentReady ? (Vector2)agent.velocity : Vector2.zero;

        public event Action<MonsterState> StateChanged;
        // A luz o tocou e ele vai perseguir: bom gancho para o grito de estática e tremor de câmera.
        public event Action Detected;

        private const float RepathInterval = 0.2f;
        private const float ArriveDistance = 0.35f;

        private NavMeshAgent agent;
        private bool agentReady;
        private Vector2 home;

        private Vector2 investigateTarget;
        private float stateTimer;      // pausa na patrulha, escuta na investigação, grito, atordoamento
        private bool waiting;           // parado no ponto de patrulha / escutando
        private float repathTimer;
        private int currentWaypoint = -1;

        private Vector2 lastKnownPlayerPos;
        private float lastSeenTime = float.NegativeInfinity;

        private Vector2 lastNoisePos;
        private float lastNoiseTime = float.NegativeInfinity;
        private bool lastNoiseVisited = true;

        private bool chasingBeforeStun;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            // NavMeshPlus: o agente anda no plano XY, sem girar o transform.
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            // Só liga depois de achar o NavMesh no Start (o baker roda no Awake).
            agent.enabled = false;

            if (config == null)
            {
                Debug.LogError($"{name}: MonsterController sem MonsterConfig no Inspector.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            NoiseBus.NoiseEmitted += OnNoise;
        }

        private void OnDisable()
        {
            NoiseBus.NoiseEmitted -= OnNoise;
        }

        // Start, não OnEnable: garante que o Awake do GameManager e o bake do NavMesh já rodaram.
        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDriveCollected;

            if (flashlight == null) flashlight = FindAnyObjectByType<FlashlightController>();
            if (player == null && flashlight != null) player = flashlight.transform;
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();

            home = transform.position;
            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.acceleration = config.acceleration;
                agent.stoppingDistance = 0.1f;
                agentReady = true;
                ApplySpeed();
                EnterPatrol();
            }
            else
            {
                Debug.LogError($"{name}: sem NavMesh por perto. A cena precisa de um NavMeshSurface " +
                               "(menu Sombra da Sanidade > Gerar cena de protótipo cria um).", this);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
        }

        private void Update()
        {
            if (!agentReady) return;

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                agent.isStopped = true;
                return;
            }

            if (State == MonsterState.Stunned)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) EndStun();
                return;
            }

            if (IsLitByFlashlight()) OnLit();

            switch (State)
            {
                case MonsterState.Patrol: UpdatePatrol(); break;
                case MonsterState.Investigate: UpdateInvestigate(); break;
                case MonsterState.Chase: UpdateChase(); break;
            }

            AvoidSafeZones();
        }

        // ---------- Contrato ----------

        // Chamado pelo pulso EMP.
        public void Stun(float duration)
        {
            if (State != MonsterState.Stunned) chasingBeforeStun = State == MonsterState.Chase;
            IsDetecting = false;
            stateTimer = duration;
            SetState(MonsterState.Stunned);
            if (agentReady)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        // Ruído ouvido: vai até `position` e escuta (Investigação Auditiva).
        // Quem quiser fazer o monstro perseguir de verdade não chama isto: a Perseguição só começa pela luz (GDD regra 2).
        public void Alert(Vector2 position)
        {
            RememberNoise(position);
            if (State == MonsterState.Stunned || State == MonsterState.Chase) return;
            lastNoiseVisited = true;
            EnterInvestigate(position);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = multiplier;
            ApplySpeed();
        }

        // ---------- Estados ----------

        private void SetState(MonsterState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        private void EnterPatrol()
        {
            SetState(MonsterState.Patrol);
            waiting = false;
            GoToNextPatrolPoint();
        }

        private void UpdatePatrol()
        {
            if (waiting)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer > 0f) return;
                waiting = false;

                // Nó de decisão (GDD 4.6): 70% vai ao último ruído, 30% segue a rota.
                if (HasFreshNoise() && Random.value < config.followNoiseChance)
                {
                    lastNoiseVisited = true;
                    EnterInvestigate(lastNoisePos);
                    return;
                }
                GoToNextPatrolPoint();
                return;
            }

            if (Arrived())
            {
                waiting = true;
                stateTimer = config.waypointPause * MercyFactor();
            }
        }

        private void EnterInvestigate(Vector2 position)
        {
            investigateTarget = position;
            waiting = false;
            SetState(MonsterState.Investigate);
            agent.isStopped = false;
            MoveTo(position);
        }

        private void UpdateInvestigate()
        {
            if (waiting)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) EnterPatrol();
                return;
            }

            if (Arrived())
            {
                waiting = true;
                agent.isStopped = true;
                stateTimer = config.listenDuration * MercyFactor();
            }
        }

        private void OnLit()
        {
            if (player == null) return;
            lastSeenTime = Time.time;
            lastKnownPlayerPos = player.position;

            if (State == MonsterState.Chase) return;

            // Perseguição Furiosa: primeiro o grito de estática, parado, depois corre.
            waiting = false;
            IsDetecting = true;
            stateTimer = config.detectionDelay * MercyFactor();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            SetState(MonsterState.Chase);
            Detected?.Invoke();
        }

        private void UpdateChase()
        {
            if (IsDetecting)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer > 0f) return;
                IsDetecting = false;
                agent.isStopped = false;
                repathTimer = 0f;
            }

            bool tracking = player != null && Time.time - lastSeenTime <= config.chaseMemory;
            if (tracking) lastKnownPlayerPos = player.position;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = RepathInterval;
                MoveTo(lastKnownPlayerPos);
            }

            // Perdeu o rastro (luz apagada): vai até onde o viu por último e escuta.
            if (!tracking && Arrived())
            {
                waiting = true;
                agent.isStopped = true;
                investigateTarget = lastKnownPlayerPos;
                stateTimer = config.listenDuration * MercyFactor();
                SetState(MonsterState.Investigate);
            }
        }

        private void EndStun()
        {
            agent.isStopped = false;
            // Se estava caçando, procura onde o jogador estava; senão volta à rota.
            if (chasingBeforeStun) EnterInvestigate(lastKnownPlayerPos);
            else EnterPatrol();
        }

        // ---------- Sentidos ----------

        private bool IsLitByFlashlight()
        {
            if (flashlight == null || !flashlight.IsLit) return false;
            Vector2 feet = transform.position;
            var heights = config.lightSampleHeights;
            if (heights == null || heights.Length == 0) return flashlight.IsPointInCone(feet);
            for (int i = 0; i < heights.Length; i++)
                if (flashlight.IsPointInCone(feet + Vector2.up * heights[i])) return true;
            return false;
        }

        private void OnNoise(Vector2 position, float radius)
        {
            float hear = Mathf.Max(radius, config != null ? config.hearingRadius : 0f);
            if (((Vector2)transform.position - position).sqrMagnitude <= hear * hear)
                Alert(position);
            else
                RememberNoise(position);
        }

        private void RememberNoise(Vector2 position)
        {
            lastNoisePos = position;
            lastNoiseTime = Time.time;
            lastNoiseVisited = false;
        }

        private bool HasFreshNoise() =>
            !lastNoiseVisited && Time.time - lastNoiseTime <= config.noiseMemory;

        // GDD 5: com a sanidade abaixo de 15 ele demora 5% a mais para procurar.
        private float MercyFactor()
        {
            if (sanity == null || sanity.Value >= config.mercySanityThreshold) return 1f;
            return 1f + config.mercySearchDelay;
        }

        private void OnDriveCollected(int total)
        {
            if (config != null) SetSpeedMultiplier(1f + config.speedBonusPerDrive * total);
        }

        // ---------- Movimento ----------

        private void ApplySpeed()
        {
            if (agentReady) agent.speed = CurrentSpeed;
        }

        private void MoveTo(Vector2 target)
        {
            if (!agentReady || !agent.isOnNavMesh) return;
            float margin = config.safeZoneMargin;
            var zone = MonsterRepellent.Find(target, margin);
            if (zone != null) target = zone.ClosestOutside(transform.position, margin);
            agent.isStopped = false;
            agent.SetDestination(target);
        }

        private bool Arrived()
        {
            if (agent.pathPending) return false;
            return agent.remainingDistance <= agent.stoppingDistance + ArriveDistance;
        }

        private void GoToNextPatrolPoint()
        {
            Vector2 target;
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                int next = currentWaypoint;
                if (patrolPoints.Length > 1)
                    while (next == currentWaypoint) next = Random.Range(0, patrolPoints.Length);
                else next = 0;
                currentWaypoint = next;
                target = patrolPoints[next] != null ? (Vector2)patrolPoints[next].position : home;
            }
            else
            {
                target = home + Random.insideUnitCircle * randomPatrolRadius;
                if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas)) target = hit.position;
            }
            MoveTo(target);
        }

        // GDD regra 6: nunca entra numa lâmpada de teto acesa.
        private void AvoidSafeZones()
        {
            if (MonsterRepellent.Active.Count == 0) return;
            Vector2 pos = transform.position;
            float margin = config.safeZoneMargin;

            // A luz acendeu em cima dele: sai pela borda mais próxima.
            var inside = MonsterRepellent.Find(pos);
            if (inside != null)
            {
                agent.isStopped = false;
                agent.SetDestination(inside.ClosestOutside(pos, margin));
                return;
            }

            // Prestes a entrar: para na borda. Na patrulha escolhe outra rota.
            Vector2 vel = agent.velocity;
            if (vel.sqrMagnitude < 0.01f) return;
            Vector2 ahead = pos + vel.normalized * margin;
            if (MonsterRepellent.Find(ahead) == null) return;

            agent.velocity = Vector3.zero;
            agent.ResetPath();
            if (State == MonsterState.Patrol && !waiting)
            {
                waiting = true;
                stateTimer = config.waypointPause;
            }
            else if (State == MonsterState.Investigate && !waiting)
            {
                waiting = true;
                stateTimer = config.listenDuration * MercyFactor();
            }
            // Na Perseguição ele espera na borda; o próximo repath já mira o ponto fora da luz.
        }

        // ---------- Contato ----------

        private void OnTriggerEnter2D(Collider2D other) => TryKill(other);

        private void OnTriggerStay2D(Collider2D other) => TryKill(other);

        private void TryKill(Collider2D other)
        {
            if (IsStunned || !other.CompareTag("Player")) return;
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver(DeathCause.Monster);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.5f);
            DrawCircle(transform.position, config.hearingRadius);
            if (patrolPoints == null) return;
            Gizmos.color = new Color(0.8f, 0.3f, 1f, 0.8f);
            foreach (var p in patrolPoints)
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.4f);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 32;
            Vector3 prev = center + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
