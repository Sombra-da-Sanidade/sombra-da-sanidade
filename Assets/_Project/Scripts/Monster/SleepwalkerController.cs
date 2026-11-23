using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace SombraDaSanidade.Monster
{
    public enum SleepwalkerState { Dormant, Frozen, Moving }

    // Como ele começa na cena: pelo config (startsDormant), adormecido (revelado por cutscene), já rondando ou caçando.
    public enum SleepwalkerStart { FromConfig, Dormant, Roaming, Hunting }

    // Voluntário 03, o Sonâmbulo. Frente 2 (Monstro). Detalhes em docs/sonambulo.md.
    // Começa adormecido no mapa e só acorda na primeira vez que o Thomas o vê (campo de visão ou lanterna).
    // Acordado, só trava com a lanterna ligada apontada para ele; no escuro, mesmo olhando, ele vem,
    // e muito rápido. Encostar sem a luz nele mata, com uma das duas cenas de morte (MonsterKillScene).
    // Ronda: acordado e antes de encontrar o Thomas, anda devagar pelo escuro em volta da origem. Uma câmera de
    // segurança em alarme o chama até o ponto; se não achar o Thomas lá, volta a rondar. Quando encontra o Thomas
    // (vê de perto ou é notado por ele), persegue para sempre, como antes: não volta mais a rondar.
    // É o contrário do Espécime: a luz atrai o Espécime e prende o Sonâmbulo.
    [RequireComponent(typeof(NavMeshAgent))]
    public class SleepwalkerController : MonoBehaviour, IMonsterKiller
    {
        public const int PoseCount = 8;
        public const int LungePose = 5;
        public const int DormantPose = 7;

        [SerializeField] private SleepwalkerConfig config;

        [Tooltip("Como começa nesta cena. FromConfig: adormecido se o config disser, senão caçando.")]
        [SerializeField] private SleepwalkerStart startMode = SleepwalkerStart.FromConfig;
        [Tooltip("Adormecido, ignora a lanterna e o olhar do Thomas: só acorda por Wake() (revelação por cutscene).")]
        [SerializeField] private bool wakeOnlyByScript;

        [Header("Referências (vazias: procura na cena)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private FlashlightController flashlight;

        public SleepwalkerConfig Config => config;
        public SleepwalkerState State { get; private set; } = SleepwalkerState.Dormant;
        public bool IsAwake => State != SleepwalkerState.Dormant;
        // Achou o Thomas (ou foi chamado por uma câmera) e vai atrás dele. Falso enquanto ronda.
        public bool IsHunting { get; private set; }
        public bool IsRoaming => IsAwake && !IsHunting;
        public SleepwalkerStart StartMode { get => startMode; set => startMode = value; }
        public bool WakeOnlyByScript { get => wakeOnlyByScript; set => wakeOnlyByScript = value; }
        // A lanterna ligada está apontada para ele agora (atualizado a cada quadro). É o que o trava.
        public bool IsLit { get; private set; }
        // Parado pela luz de um sinalizador (e não pela lanterna).
        public bool IsLitByFlare { get; private set; }
        // Pegou o jogador e está tocando a cena de morte.
        public bool IsKilling { get; private set; }
        public float KillFps => config.killFps;
        public float KillHold => config.killHold;
        // Pose em que travou (0-7, linha "pose" da folha). Muda cada vez que ele volta a travar.
        public int Pose { get; private set; } = DormantPose;
        public Vector2 Velocity => agentReady ? (Vector2)agent.velocity : Vector2.zero;

        // Visto pela primeira vez: gancho para som e tremor de câmera.
        public event Action Woke;
        // Encontrou o Thomas de verdade, com ele na tela: a fase 2 usa para a revelação (C2). Uma vez só.
        public event Action Encountered;
        // Já encontrou o Thomas: persegue até o fim.
        public bool HasFoundPlayer { get; private set; }
        public event Action<SleepwalkerState> StateChanged;
        // Encostou no jogador fora do modo seguro: o MonsterKillScene toca a morte e chama o Game Over.
        public event Action<Transform> Caught;

        private NavMeshAgent agent;
        private bool agentReady;
        private int wallMask;
        private float holdUntil;
        private float unseenSince;
        private float repathTimer;
        private float nextSafeHit;
        private Vector2 home;
        private Vector2? roamTarget;
        private float roamWaitUntil;
        private Vector2? lure;
        private float lastNear;
        private bool huntOnWake = true;
        private bool encounterSent;
        private SecurityCamera[] cameras = Array.Empty<SecurityCamera>();

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.enabled = false;
            wallMask = LayerMask.GetMask("Walls");
            if (config == null)
            {
                Debug.LogError($"{name}: SleepwalkerController sem SleepwalkerConfig no Inspector.", this);
                enabled = false;
            }
        }

        // Velocidade da ala (DifficultyProfile, #80) ou, sem perfil, a do config.
        public float Speed
        {
            get
            {
                var p = SombraDaSanidade.Core.DifficultyProfile.Active;
                return p != null && p.sleepwalkerSpeed > 0f ? p.sleepwalkerSpeed : config.speed;
            }
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (flashlight == null && player != null) flashlight = player.GetComponent<FlashlightController>();
            if (flashlight == null) flashlight = FindAnyObjectByType<FlashlightController>();

            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.speed = Speed;
                // Prioridade diferente do Espécime (50): na fase 3 um desvia do outro em vez de travarem (#80).
                agent.avoidancePriority = 60;
                agent.acceleration = config.acceleration;
                agent.angularSpeed = 0f;
                agent.autoBraking = false;
                agent.stoppingDistance = 0f;
                agent.isStopped = true;
                agentReady = true;
            }
            else
            {
                Debug.LogError($"{name}: sem NavMesh por perto. A cena precisa de um NavMeshSurface.", this);
            }

            home = transform.position;
            var mode = startMode == SleepwalkerStart.FromConfig
                ? (config.startsDormant ? SleepwalkerStart.Dormant : SleepwalkerStart.Hunting) : startMode;
            if (mode == SleepwalkerStart.Roaming) { huntOnWake = false; Wake(); }
            else if (mode == SleepwalkerStart.Hunting) Wake();

            // Alarme de câmera chama o Sonâmbulo para o ponto onde o Thomas foi visto.
            cameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
            foreach (var cam in cameras) cam.AlarmRaised += OnCameraAlarm;
        }

        private void OnDestroy()
        {
            foreach (var cam in cameras) if (cam != null) cam.AlarmRaised -= OnCameraAlarm;
        }

        private void OnCameraAlarm(Vector2 seenAt)
        {
            if (!IsAwake || HasFoundPlayer || Vector2.Distance(transform.position, seenAt) > config.cameraLureDistance) return;
            Lure(seenAt);
        }

        // Vai até um ponto (alarme, ruído roteirizado) já caçando. Chegando lá sem ver o Thomas, volta a rondar dali.
        public void Lure(Vector2 point)
        {
            lure = point;
            IsHunting = true;
            lastNear = Time.time;
            repathTimer = 0f;
        }

        // Aparece num ponto (roteiro): já acordado e caçando, parado o tempo da folga de despertar.
        public void Ambush(Vector2 at)
        {
            if (agentReady) agent.Warp(at);
            else transform.position = at;
            if (State == SleepwalkerState.Dormant) Wake();
            holdUntil = Time.time + config.wakeGrace;
            lure = null;
            FoundPlayer();
        }

        private void Update()
        {
            if (!agentReady || player == null) return;

            if (IsKilling || (GameManager.Instance != null && !GameManager.Instance.IsPlaying) || SombraDaSanidade.UI.CutscenePlayer.IsPlaying)
            {
                Stop();
                return;
            }

            bool byFlashlight = ComputeLit();
            IsLitByFlare = ComputeFlareLit();
            IsLit = byFlashlight || IsLitByFlare;

            if (State == SleepwalkerState.Dormant)
            {
                // O sinalizador não acorda: só a lanterna ou o Thomas olhando.
                if (!wakeOnlyByScript && (byFlashlight || ComputeNoticed())) Wake();
                return;
            }

            if (IsLit)
            {
                unseenSince = float.PositiveInfinity;
                Freeze();
                return;
            }

            if (float.IsPositiveInfinity(unseenSince)) unseenSince = Time.time;
            if (Time.time < holdUntil || Time.time - unseenSince < config.unseenDelay)
            {
                Freeze();
                return;
            }

            // Encontro: vê o Thomas de perto ou é notado por ele. Daí em diante, persegue para sempre.
            if (!HasFoundPlayer && (SeesPlayer() || ComputeNoticed())) FoundPlayer();

            if (!IsHunting)
            {
                Roam();
                return;
            }

            if (lure.HasValue && !HasFoundPlayer)
            {
                GoTo(lure.Value, Speed);
                if (Vector2.Distance(transform.position, lure.Value) < 1.2f || (agent.hasPath && agent.remainingDistance < 1.2f && !agent.pathPending))
                    StopHunting();   // chamado pela câmera, chegou e não achou ninguém: volta a rondar dali
                return;
            }
            lure = null;
            Chase();
        }

        private void FoundPlayer()
        {
            HasFoundPlayer = true;
            StartHunting();
        }

        // Revelação: a primeira vez que ele está caçando o Thomas e aparece na tela.
        private void LateUpdate()
        {
            if (encounterSent || !HasFoundPlayer || !IsAwake) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 v = cam.WorldToViewportPoint(transform.position + Vector3.up * 1.2f);
            if (v.x < 0.05f || v.x > 0.95f || v.y < 0.05f || v.y > 0.95f) return;
            encounterSent = true;
            Encountered?.Invoke();
        }

        private void StartHunting()
        {
            IsHunting = true;
            lastNear = Time.time;
            roamTarget = null;
            repathTimer = 0f;
        }

        private void StopHunting()
        {
            IsHunting = false;
            lure = null;
            home = transform.position;
            roamTarget = null;
            roamWaitUntil = Time.time + Random.Range(config.roamPause.x, config.roamPause.y);
        }

        // Thomas perto e sem parede no meio.
        private bool SeesPlayer()
        {
            Vector2 a = transform.position, b = player.transform.position;
            return (b - a).sqrMagnitude <= config.detectDistance * config.detectDistance
                && !Physics2D.Linecast(a + Vector2.up * 0.4f, b, wallMask);
        }

        // Anda devagar entre pontos sorteados em volta da origem, parando um pouco em cada um. Evita Zonas Seguras acesas.
        private void Roam()
        {
            if (roamTarget.HasValue && Vector2.Distance(transform.position, roamTarget.Value) > 0.6f
                && !(agent.hasPath && !agent.pathPending && agent.remainingDistance < 0.6f))
            {
                GoTo(roamTarget.Value, config.roamSpeed);
                return;
            }
            if (roamTarget.HasValue)
            {
                roamTarget = null;
                roamWaitUntil = Time.time + Random.Range(config.roamPause.x, config.roamPause.y);
            }
            if (Time.time < roamWaitUntil)
            {
                if (State == SleepwalkerState.Moving) Freeze();
                return;
            }
            for (int i = 0; i < 8; i++)
            {
                Vector2 p = home + Random.insideUnitCircle * config.roamRadius;
                if (MonsterRepellent.Find(p, config.safeZoneMargin) != null) continue;
                if (!NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                roamTarget = hit.position;
                repathTimer = 0f;
                GoTo(roamTarget.Value, config.roamSpeed);
                return;
            }
            roamWaitUntil = Time.time + 1f;
        }

        private void GoTo(Vector2 target, float speed)
        {
            SetState(SleepwalkerState.Moving);
            agent.speed = speed * (MonsterSafeMode.Enabled ? 0.7f : 1f);
            agent.isStopped = false;
            repathTimer -= Time.deltaTime;
            if (repathTimer > 0f) return;
            repathTimer = config.repathInterval;
            var zone = MonsterRepellent.Find(target, config.safeZoneMargin);
            if (zone != null) target = zone.ClosestOutside(transform.position, config.safeZoneMargin);
            if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }

        // ---------- Contrato ----------

        // Acorda sem precisar ser visto (para eventos roteirizados).
        public void Wake()
        {
            if (State != SleepwalkerState.Dormant) return;
            holdUntil = Time.time + config.wakeGrace;
            unseenSince = float.PositiveInfinity;
            IsHunting = huntOnWake;
            HasFoundPlayer = huntOnWake;
            lastNear = Time.time;
            home = transform.position;
            SetState(SleepwalkerState.Frozen);
            Woke?.Invoke();
        }

        // ---------- Luz e visão ----------

        // Lanterna ligada, com o cone nele e sem parede no meio.
        // Sinalizador aceso no chão (SignalFlare): segura parado enquanto ele estiver dentro da luz.
        private bool ComputeFlareLit() => SignalFlare.Illuminates(transform.position);

        private bool ComputeLit()
        {
            if (flashlight == null) return false;
            foreach (float h in config.sampleHeights)
                if (flashlight.IsPointInCone((Vector2)transform.position + Vector2.up * h)) return true;
            return false;
        }

        // Só para acordar: o Thomas olhou para ele, mesmo no escuro.
        private bool ComputeNoticed()
        {
            Vector2 eye = player.transform.position;
            Vector2 facing = player.Facing;
            float half = config.viewAngle * 0.5f;
            foreach (float h in config.sampleHeights)
            {
                Vector2 point = (Vector2)transform.position + Vector2.up * h;
                Vector2 to = point - eye;
                if (to.sqrMagnitude > config.viewDistance * config.viewDistance) continue;
                if (Vector2.Angle(facing, to) > half) continue;
                if (!Physics2D.Linecast(eye, point, wallMask)) return true;
            }
            return false;
        }

        // ---------- Estados ----------

        private void Freeze()
        {
            if (State == SleepwalkerState.Moving)
            {
                // Sorteia a próxima pose; perto do jogador, é sempre o bote.
                float d = Vector2.Distance(transform.position, player.transform.position);
                if (d < config.lungePoseDistance) Pose = LungePose;
                else
                {
                    int next;
                    do next = Random.Range(0, PoseCount - 1); while (next == LungePose || next == Pose);
                    Pose = next;
                }
            }
            SetState(SleepwalkerState.Frozen);
            Stop();
        }

        private void Chase()
        {
            SetState(SleepwalkerState.Moving);
            agent.speed = Speed * (MonsterSafeMode.Enabled ? 0.7f : 1f);
            agent.isStopped = false;
            repathTimer -= Time.deltaTime;
            if (repathTimer > 0f) return;
            repathTimer = config.repathInterval;

            Vector2 target = player.transform.position;
            // Nunca entra numa Zona Segura acesa: vai até a borda e espera.
            var zone = MonsterRepellent.Find(target, config.safeZoneMargin);
            if (zone != null) target = zone.ClosestOutside(transform.position, config.safeZoneMargin);
            if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }

        private void Stop()
        {
            if (!agentReady) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        private void SetState(SleepwalkerState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        // ---------- Contato ----------

        private void OnTriggerEnter2D(Collider2D other) => TryKill(other);

        private void OnTriggerStay2D(Collider2D other) => TryKill(other);

        private void TryKill(Collider2D other)
        {
            // Com a luz nele, não ataca: só pega quem apaga ou desvia a lanterna.
            if (!IsAwake || IsLit || IsKilling || !other.CompareTag("Player")) return;
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
            Stop();
            Caught.Invoke(target);
        }

        private void SafeHit(Collider2D other)
        {
            if (Time.time < nextSafeHit) return;
            nextSafeHit = Time.time + config.safeModeHitPause;
            var target = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            var sanity = target.GetComponentInParent<SanityController>();
            if (sanity != null) sanity.Add(-config.safeModeSanityDamage);

            Vector2 at = target.position;
            Vector2 dir = at - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            dir.Normalize();
            float dist = config.safeModePushDistance;
            var wall = Physics2D.CircleCast(at, 0.3f, dir, dist, wallMask);
            if (wall.collider != null) dist = Mathf.Max(0f, wall.distance - 0.05f);
            Vector2 pushed = at + dir * dist;
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = pushed;
            target.position = pushed;
            holdUntil = Time.time + config.safeModeHitPause;
            Freeze();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsAwake ? new Color(0.5f, 0.3f, 0.55f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.6f);
            foreach (float h in config != null ? config.sampleHeights : new[] { 0.3f, 1.6f, 3f })
                Gizmos.DrawWireSphere(transform.position + Vector3.up * h, 0.2f);
        }
    }
}
