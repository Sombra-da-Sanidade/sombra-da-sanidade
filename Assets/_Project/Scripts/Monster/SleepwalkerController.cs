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

    // Voluntário 03, o Sonâmbulo. Frente 2 (Monstro). Detalhes em docs/sonambulo.md.
    // Começa adormecido no mapa e só acorda na primeira vez que o Thomas o vê (campo de visão ou lanterna).
    // Acordado, só trava com a lanterna ligada apontada para ele; no escuro, mesmo olhando, ele vem,
    // e muito rápido. Encostar sem a luz nele mata, com uma das duas cenas de morte (MonsterKillScene).
    // É o contrário do Espécime: a luz atrai o Espécime e prende o Sonâmbulo.
    [RequireComponent(typeof(NavMeshAgent))]
    public class SleepwalkerController : MonoBehaviour, IMonsterKiller
    {
        public const int PoseCount = 8;
        public const int LungePose = 5;
        public const int DormantPose = 7;

        [SerializeField] private SleepwalkerConfig config;

        [Header("Referências (vazias: procura na cena)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private FlashlightController flashlight;

        public SleepwalkerConfig Config => config;
        public SleepwalkerState State { get; private set; } = SleepwalkerState.Dormant;
        public bool IsAwake => State != SleepwalkerState.Dormant;
        // A lanterna ligada está apontada para ele agora (atualizado a cada quadro). É o que o trava.
        public bool IsLit { get; private set; }
        // Pegou o jogador e está tocando a cena de morte.
        public bool IsKilling { get; private set; }
        public float KillFps => config.killFps;
        public float KillHold => config.killHold;
        // Pose em que travou (0-7, linha "pose" da folha). Muda cada vez que ele volta a travar.
        public int Pose { get; private set; } = DormantPose;
        public Vector2 Velocity => agentReady ? (Vector2)agent.velocity : Vector2.zero;

        // Visto pela primeira vez: gancho para som e tremor de câmera.
        public event Action Woke;
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

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (flashlight == null && player != null) flashlight = player.GetComponent<FlashlightController>();
            if (flashlight == null) flashlight = FindAnyObjectByType<FlashlightController>();

            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.speed = config.speed;
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

            if (!config.startsDormant) Wake();
        }

        private void Update()
        {
            if (!agentReady || player == null) return;

            if (IsKilling || (GameManager.Instance != null && !GameManager.Instance.IsPlaying))
            {
                Stop();
                return;
            }

            IsLit = ComputeLit();

            if (State == SleepwalkerState.Dormant)
            {
                if (IsLit || ComputeNoticed()) Wake();
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
            Chase();
        }

        // ---------- Contrato ----------

        // Acorda sem precisar ser visto (para eventos roteirizados).
        public void Wake()
        {
            if (State != SleepwalkerState.Dormant) return;
            holdUntil = Time.time + config.wakeGrace;
            unseenSince = float.PositiveInfinity;
            SetState(SleepwalkerState.Frozen);
            Woke?.Invoke();
        }

        // ---------- Luz e visão ----------

        // Lanterna ligada, com o cone nele e sem parede no meio.
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
            agent.speed = config.speed * (MonsterSafeMode.Enabled ? 0.7f : 1f);
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
