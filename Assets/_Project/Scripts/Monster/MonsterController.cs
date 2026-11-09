using System;
using System.Collections.Generic;
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

        public MonsterConfig Config => config;

        public MonsterState State { get; private set; } = MonsterState.Patrol;
        public bool IsStunned => State == MonsterState.Stunned;

        // Grito de estática logo após a luz tocá-lo (início da Perseguição). Fica parado, mas encostar ainda mata.
        public bool IsDetecting { get; private set; }

        // Multiplicador acumulado pelos HDs coletados (GDD regra 4).
        public float SpeedMultiplier { get; private set; } = 1f;

        // Na Perseguição ele galopa (config.chaseSpeedMultiplier). O modo seguro deixa tudo mais lento.
        public float CurrentSpeed
        {
            get
            {
                if (config == null) return 0f;
                float speed = IsFurious ? config.furySpeed : Mathf.Min(config.baseSpeed * SpeedMultiplier, config.maxSpeed);
                if (State == MonsterState.Chase)
                    speed = Mathf.Min(speed * config.chaseSpeedMultiplier, Mathf.Max(config.chaseMaxSpeed, config.maxSpeed));
                return speed * (MonsterSafeMode.Enabled ? config.safeModeSpeedMultiplier : 1f);
            }
        }

        // Fúria da Eclusa (#44): ligada pela purga (Level.EscapeSequence). Velocidade e audição da coluna Eclusa
        // da Proposta 1 e todos os comportamentos da Executiva.
        public bool IsFurious { get; private set; }

        public void SetFury(bool on)
        {
            if (IsFurious == on) return;
            IsFurious = on;
            if (on) SetBehaviours(MonsterWingBehaviours.Executiva);
            ApplySpeed();
        }

        // Pegou o jogador e está tocando a cena de morte (MonsterKillScene). A IA fica parada até o Game Over.
        public bool IsKilling { get; private set; }

        // Encostou no jogador fora do modo seguro. O MonsterKillScene toca a morte e chama o Game Over no fim.
        // Sem ninguém ouvindo, o Game Over é imediato.
        public event Action<Transform> Caught;

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
        private Vector2? retreatFrom;

        // Busca da Ala Médica (#36)
        private int searchLeft;
        private Vector2 searchCenter;
        private bool atSearchPoint;
        private readonly List<int> searchVisited = new List<int>();

        // Energia e Executiva (#37)
        private readonly Dictionary<MonsterRepellent, float> zoneChecked = new Dictionary<MonsterRepellent, float>();
        private readonly List<(Vector2 pos, float time)> noiseHistory = new List<(Vector2, float)>();

        private Vector2 lastKnownPlayerPos;
        private float lastSeenTime = float.NegativeInfinity;

        private Vector2 lastNoisePos;
        private float lastNoiseTime = float.NegativeInfinity;
        private bool lastNoiseVisited = true;

        private bool chasingBeforeStun;

        // Encontro roteirizado (Proposta 4 do v1.1): anda por uma rota fixa e, se for iluminado, grita e
        // investiga em vez de perseguir. Depois dá uns segundos de folga antes de a luz valer de novo.
        private const float ScriptedLightGrace = 4f;
        private List<Vector2> route;
        private int routeIndex;
        private bool routeStarted;
        private bool scriptedReaction;
        private float lightGraceUntil = float.NegativeInfinity;

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
            MonsterSafeMode.Changed += OnSafeModeChanged;
        }

        private void OnSafeModeChanged(bool on) => ApplySpeed();

        private void OnDisable()
        {
            NoiseBus.NoiseEmitted -= OnNoise;
            MonsterSafeMode.Changed -= OnSafeModeChanged;
        }

        // Start, não OnEnable: garante que o Awake do GameManager e o bake do NavMesh já rodaram.
        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DriveCollected += OnDriveCollected;
                // Cenas que já começam com HDs (Eclusa, #44) partem da velocidade acumulada.
                if (GameManager.Instance.DrivesCollected > 0) OnDriveCollected(GameManager.Instance.DrivesCollected);
            }

            if (flashlight == null) flashlight = FindAnyObjectByType<FlashlightController>();
            if (player == null && flashlight != null) player = flashlight.transform;
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();

            home = transform.position;
            Behaviours = config.behaviours;
            BehavioursChanged?.Invoke(Behaviours);
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

            if (IsKilling)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                return;
            }

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

            if (route != null)
            {
                UpdateRoute();
                AvoidSafeZones();
                return;
            }

            // Modo econômico do Filtro Âmbar (#31) ilumina sem atrair.
            if (Time.time >= lightGraceUntil && IsLitByFlashlight() && flashlight.AttractsMonster) OnLit();

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
            if (IsKilling) return;
            route = null;
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
            if (IsOnRoute || State == MonsterState.Stunned || State == MonsterState.Chase) return;
            lastNoiseVisited = true;
            EnterInvestigate(position);
        }

        // Diretor de tensão (#35): no Relax ele patrulha os pontos mais longe deste lugar (o jogador).
        // Não é um alvo: o monstro continua achando o jogador só pela luz e pelo ruído. null = patrulha normal.
        public bool IsRetreating => retreatFrom.HasValue;

        public void SetRetreat(Vector2? awayFrom, bool redirectNow = true)
        {
            retreatFrom = awayFrom;
            if (!agentReady || !awayFrom.HasValue || !redirectNow) return;
            // Larga a investigação e já sai andando para longe; perseguição o diretor espera acabar sozinha.
            if (State == MonsterState.Patrol || State == MonsterState.Investigate) EnterPatrol();
        }

        // Comportamentos ligados nesta ala (#36). Nunca dependem de mortes do jogador.
        public MonsterBehaviour Behaviours { get; private set; } = MonsterBehaviour.Search;

        public bool Has(MonsterBehaviour behaviour) => (Behaviours & behaviour) == behaviour;

        // Avisado quando a ala troca o conjunto (o MonsterShortcut liga e desliga os atalhos com isso).
        public event Action<MonsterBehaviour> BehavioursChanged;

        public void SetBehaviours(MonsterBehaviour behaviours)
        {
            Behaviours = behaviours;
            if (!Has(MonsterBehaviour.Search)) searchLeft = 0;
            BehavioursChanged?.Invoke(behaviours);
        }

        // Encontro roteirizado: segue `points` em ordem sem ligar para ruído e, no fim, volta à patrulha.
        // Se for iluminado no caminho, grita e investiga o ponto do jogador (sem Perseguição). Pode ser chamado
        // antes do Start; a rota começa quando o agente estiver no NavMesh.
        public bool IsOnRoute => route != null;

        public void FollowRoute(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count == 0) return;
            route = new List<Vector2>(points);
            routeIndex = 0;
            routeStarted = false;
            if (State != MonsterState.Patrol) SetState(MonsterState.Patrol);
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
            ApplySpeed();
            StateChanged?.Invoke(next);
        }

        private void EnterPatrol()
        {
            SetState(MonsterState.Patrol);
            waiting = false;
            searchLeft = 0;
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
                // No recuo do diretor ele não volta atrás de ruído antigo (ruído novo ainda o atrai).
                if (!IsRetreating && HasFreshNoise() && Random.value < config.followNoiseChance)
                {
                    lastNoiseVisited = true;
                    EnterInvestigate(lastNoisePos);
                    return;
                }
                if (!IsRetreating && Has(MonsterBehaviour.BreakerCheck) && TryCheckLitZone()) return;
                if (!IsRetreating && Has(MonsterBehaviour.NoiseMemory) && TryRecallOldNoise()) return;
                GoToNextPatrolPoint();
                return;
            }

            if (Arrived())
            {
                waiting = true;
                stateTimer = config.waypointPause * MercyFactor();
            }
        }

        private void EnterInvestigate(Vector2 position, bool search = true)
        {
            investigateTarget = position;
            waiting = false;
            SetState(MonsterState.Investigate);
            agent.isStopped = false;
            BeginSearch(position);
            if (!search) searchLeft = 0;
            MoveTo(position);
        }

        private void UpdateInvestigate()
        {
            if (waiting)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer > 0f) return;
                // Busca (#36): antes de voltar à rota, checa pontos por perto.
                if (searchLeft > 0 && NextSearchPoint(out var next))
                {
                    searchLeft--;
                    waiting = false;
                    atSearchPoint = true;
                    agent.isStopped = false;
                    MoveTo(next);
                    return;
                }
                EnterPatrol();
                return;
            }

            if (Arrived())
            {
                waiting = true;
                agent.isStopped = true;
                float listen = config.listenDuration * (atSearchPoint ? config.searchListenFactor : 1f);
                stateTimer = listen * MercyFactor();
            }
        }

        // ---------- Busca (#36) ----------

        private void BeginSearch(Vector2 center)
        {
            searchVisited.Clear();
            atSearchPoint = false;
            searchCenter = center;
            // No recuo do diretor de tensão (#35) ele não fica rondando: escuta e vai embora.
            searchLeft = Has(MonsterBehaviour.Search) && !IsRetreating ? config.searchPoints : 0;
        }

        // Ordem subótima, como no Alien: Isolation: sorteia entre os pontos mais próximos ainda não vistos,
        // e às vezes volta a um que já checou. Parece procurar, não saber.
        private bool NextSearchPoint(out Vector2 point)
        {
            point = searchCenter;
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Vector2 c = searchCenter + Random.insideUnitCircle * config.searchRadius * 0.5f;
                if (!NavMesh.SamplePosition(c, out var hit, 3f, NavMesh.AllAreas)) return false;
                point = hit.position;
                return true;
            }

            if (searchVisited.Count > 1 && Random.value < config.searchRevisitChance)
            {
                // Volta a um já visitado, mas não ao último (senão ele só ficaria parado).
                int back = searchVisited[Random.Range(0, searchVisited.Count - 1)];
                point = patrolPoints[back].position;
                searchVisited.Add(back);
                return true;
            }

            var candidates = new List<int>();
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null || searchVisited.Contains(i)) continue;
                if (((Vector2)patrolPoints[i].position - searchCenter).sqrMagnitude <= config.searchRadius * config.searchRadius)
                    candidates.Add(i);
            }
            if (candidates.Count == 0) return false;
            candidates.Sort((a, b) =>
                ((Vector2)patrolPoints[a].position - searchCenter).sqrMagnitude.CompareTo(
                    ((Vector2)patrolPoints[b].position - searchCenter).sqrMagnitude));
            int pick = candidates[Random.Range(0, Mathf.Min(Mathf.Max(1, config.searchCandidates), candidates.Count))];
            searchVisited.Add(pick);
            point = patrolPoints[pick].position;
            return true;
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

        private void UpdateRoute()
        {
            if (IsLitByFlashlight() && flashlight.AttractsMonster)
            {
                // Grito igual ao da Perseguição; o UpdateChase troca a corrida por investigação.
                route = null;
                scriptedReaction = true;
                OnLit();
                return;
            }

            if (!routeStarted)
            {
                routeStarted = true;
                waiting = false;
                MoveTo(route[routeIndex]);
                return;
            }
            if (!Arrived()) return;

            routeIndex++;
            if (routeIndex < route.Count)
            {
                MoveTo(route[routeIndex]);
                return;
            }
            route = null;
            EnterPatrol();
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
                if (scriptedReaction)
                {
                    scriptedReaction = false;
                    lightGraceUntil = Time.time + ScriptedLightGrace;
                    EnterInvestigate(lastKnownPlayerPos);
                    return;
                }
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
                BeginSearch(lastKnownPlayerPos);
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

        // Consulta compartilhada pela detecção e pelo EMP: as mesmas alturas, alcance e paredes.
        public bool IsLitByFlashlight()
        {
            if (config == null || flashlight == null || !flashlight.IsLit) return false;
            Vector2 feet = transform.position;
            var heights = config.lightSampleHeights;
            if (heights == null || heights.Length == 0) return flashlight.IsPointInCone(feet);
            for (int i = 0; i < heights.Length; i++)
                if (flashlight.IsPointInCone(feet + Vector2.up * heights[i])) return true;
            return false;
        }

        private void OnNoise(Vector2 position, float radius)
        {
            float hearing = config == null ? 0f
                : IsFurious ? Mathf.Max(config.hearingRadius, config.furyHearingRadius) : config.hearingRadius;
            float hear = Mathf.Max(radius, hearing);
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
            noiseHistory.Add((position, Time.time));
            int max = Mathf.Max(1, config.noiseMemorySize);
            while (noiseHistory.Count > max) noiseHistory.RemoveAt(0);
        }

        // ---------- Energia e Executiva (#37) ----------

        // Ala de Energia: vai olhar de longe uma Zona Segura acesa. Para fora da luz (MoveTo já respeita a
        // borda) e escuta. Não sabe se o jogador está lá: a luz é só um sinal de que alguém consertou algo.
        private bool TryCheckLitZone()
        {
            var zones = MonsterRepellent.Active;
            if (zones.Count == 0 || Random.value >= config.breakerCheckChance) return false;
            MonsterRepellent pick = null;
            float best = float.MaxValue;
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (zoneChecked.TryGetValue(z, out float when) && Time.time - when < config.breakerCheckCooldown) continue;
                float d = (z.Center - (Vector2)transform.position).sqrMagnitude;
                if (d < best) { best = d; pick = z; }
            }
            if (pick == null) return false;
            zoneChecked[pick] = Time.time;
            var edge = pick.ClosestOutside(transform.position, config.safeZoneMargin + config.breakerCheckDistance);
            EnterInvestigate(edge, search: false);
            return true;
        }

        // Ala Executiva: memória curta. Volta a um ruído antigo (não o último, que já tem a regra dos 70%).
        private bool TryRecallOldNoise()
        {
            if (Random.value >= config.noiseMemoryChance) return false;
            var options = new List<Vector2>();
            for (int i = 0; i < noiseHistory.Count - 1; i++)
                if (Time.time - noiseHistory[i].time <= config.noiseMemorySeconds) options.Add(noiseHistory[i].pos);
            if (options.Count == 0) return false;
            EnterInvestigate(options[Random.Range(0, options.Count)]);
            return true;
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
            if (retreatFrom.HasValue)
            {
                MoveTo(FarthestPoint(retreatFrom.Value));
                return;
            }
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

        // Ponto de patrulha mais longe de `from`. Se já está nele, vai para o segundo mais longe,
        // para não ficar parado o recuo inteiro.
        private Vector2 FarthestPoint(Vector2 from)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                int best = -1, second = -1;
                float bestD = -1f, secondD = -1f;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;
                    float d = ((Vector2)patrolPoints[i].position - from).sqrMagnitude;
                    if (d > bestD) { second = best; secondD = bestD; best = i; bestD = d; }
                    else if (d > secondD) { second = i; secondD = d; }
                }
                int pick = best == currentWaypoint && second >= 0 ? second : best;
                if (pick >= 0)
                {
                    currentWaypoint = pick;
                    return patrolPoints[pick].position;
                }
            }
            Vector2 far = home;
            float farD = -1f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 c = home + Random.insideUnitCircle * randomPatrolRadius;
                if (!NavMesh.SamplePosition(c, out var hit, 3f, NavMesh.AllAreas)) continue;
                float d = ((Vector2)hit.position - from).sqrMagnitude;
                if (d > farD) { farD = d; far = hit.position; }
            }
            return far;
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
            if (IsStunned || IsKilling || !other.CompareTag("Player")) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (MonsterSafeMode.Enabled)
            {
                SafeHit(other);
                return;
            }
            var target = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            if (Caught == null)
            {
                if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver(DeathCause.Monster);
                return;
            }
            IsKilling = true;
            IsDetecting = false;
            if (agentReady)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            Caught.Invoke(target);
        }

        // Modo seguro (#48): no lugar da morte, empurra o jogador para longe, tira sanidade e para um pouco.
        private float nextSafeHit;

        private void SafeHit(Collider2D other)
        {
            if (Time.time < nextSafeHit) return;
            nextSafeHit = Time.time + config.safeModeHitCooldown;

            var target = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            var sanity = target.GetComponentInParent<SanityController>();
            if (sanity != null) sanity.Add(-config.safeModeSanityDamage);

            Vector2 from = transform.position;
            Vector2 at = target.position;
            Vector2 dir = at - from;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            dir.Normalize();
            // Não atravessa parede: para antes do que estiver no caminho.
            float dist = config.safeModePushDistance;
            var hit = Physics2D.CircleCast(at, 0.3f, dir, dist, LayerMask.GetMask("Walls"));
            if (hit.collider != null) dist = Mathf.Max(0f, hit.distance - 0.05f);
            Vector2 pushed = at + dir * dist;
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = pushed;
            target.position = pushed;

            Stun(config.safeModeHitPause);
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
