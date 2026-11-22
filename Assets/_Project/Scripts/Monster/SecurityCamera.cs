using System;
using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Monster
{
    public enum SecurityCameraState { Sweeping, Detecting, Alarm, Disabled, Off }

    // Terceira ameaça do RC (#79, planejamento-rc 2 e 3): os olhos da Aethelgard. Varre um cone; vendo o Thomas
    // por detectTime, dispara um alarme que apaga o setor (todo disjuntor no raio corta até ser religado) e, nas alas
    // com DifficultyProfile.camerasCallSpecimen, faz um ruído onde ele está, chamando o Espécime.
    // Desliga de vez quando o disjuntor dela é religado, ou por alguns segundos com o EMP por perto.
    // A câmera não depende de luz: enxerga no escuro (infravermelho).
    public class SecurityCamera : MonoBehaviour
    {
        public static readonly List<SecurityCamera> All = new List<SecurityCamera>();

        [SerializeField] private SecurityCameraConfig config;
        [Tooltip("Parte que gira. O eixo X local aponta para onde a câmera olha.")]
        [SerializeField] private Transform head;
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite detectSprite;
        [SerializeField] private Sprite alarmSprite;
        [SerializeField] private Sprite offSprite;
        [Tooltip("Cone de luz que mostra o campo de visão.")]
        [SerializeField] private Light2D cone;
        [SerializeField] private Light2D led;
        [SerializeField] private LayerMask obstacles;

        [Header("Disjuntor")]
        [Tooltip("Religar este disjuntor desliga a câmera até o fim da ala. Vazio: o mais perto dentro do setor.")]
        [SerializeField] private Breaker shutdownBreaker;
        [Tooltip("Desligado: só o disjuntor indicado acima desliga a câmera (ou nenhum, se vazio).")]
        [SerializeField] private bool findNearestBreaker = true;

        public SecurityCameraConfig Config => config;
        public SecurityCameraState State { get; private set; } = SecurityCameraState.Sweeping;
        // 0 a 1: quanto falta para o alarme.
        public float Detection => config != null ? Mathf.Clamp01(seen / config.detectTime) : 0f;
        public bool SeesPlayer { get; private set; }
        public Vector2 Facing => head != null ? (Vector2)head.right : (Vector2)transform.right;
        public Breaker ShutdownBreaker => shutdownBreaker;

        public event Action<SecurityCameraState> StateChanged;
        // Alarme disparado: posição do Thomas visto. Gancho para som, HUD e rank.
        public event Action<Vector2> AlarmRaised;

        private PlayerController player;
        private FlashlightController flashlight;
        private float baseAngle, angle, sweepT, seen, stateTimer;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            if (head == null) head = transform;
            if (obstacles.value == 0) obstacles = LayerMask.GetMask("Walls");
            baseAngle = angle = head.eulerAngles.z;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            flashlight = FindAnyObjectByType<FlashlightController>();
            if (flashlight != null) flashlight.EmpFired += OnEmpFired;

            if (shutdownBreaker == null && findNearestBreaker && config != null)
                shutdownBreaker = NearestBreaker(config.sectorRadius);
            if (shutdownBreaker != null)
            {
                shutdownBreaker.StateChanged += OnShutdownBreaker;
                if (shutdownBreaker.IsActive) Shutdown();
            }
            Apply();
        }

        private void OnDestroy()
        {
            if (flashlight != null) flashlight.EmpFired -= OnEmpFired;
            if (shutdownBreaker != null) shutdownBreaker.StateChanged -= OnShutdownBreaker;
        }

        private void OnShutdownBreaker(bool on)
        {
            if (on) Shutdown();
        }

        private void OnEmpFired()
        {
            if (config == null || flashlight == null || State == SecurityCameraState.Off) return;
            if (Vector2.Distance(flashlight.transform.position, transform.position) <= config.empRadius)
                DisableFor(config.empDisableSeconds);
        }

        // Desliga até o fim da ala (disjuntor religado).
        public void Shutdown()
        {
            seen = 0f;
            SetState(SecurityCameraState.Off);
        }

        // Curto do EMP: volta a varrer depois de alguns segundos.
        public void DisableFor(float seconds)
        {
            if (State == SecurityCameraState.Off) return;
            seen = 0f;
            stateTimer = seconds;
            SetState(SecurityCameraState.Disabled);
        }

        private void Update()
        {
            if (config == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            float dt = Time.deltaTime;

            switch (State)
            {
                case SecurityCameraState.Off:
                    return;
                case SecurityCameraState.Disabled:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) SetState(SecurityCameraState.Sweeping);
                    break;
                case SecurityCameraState.Alarm:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) { seen = 0f; SetState(SecurityCameraState.Sweeping); }
                    break;
                case SecurityCameraState.Sweeping:
                case SecurityCameraState.Detecting:
                    UpdateWatch(dt);
                    break;
            }
            UpdateVisual();
        }

        private void UpdateWatch(float dt)
        {
            SeesPlayer = CanSee();
            if (SeesPlayer)
            {
                seen += dt;
                // Segue o Thomas enquanto confirma, sem largar a borda do cone.
                Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
                float target = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                angle = Mathf.MoveTowardsAngle(angle, target, config.trackSpeed * dt);
                if (State != SecurityCameraState.Detecting) SetState(SecurityCameraState.Detecting);
                if (seen >= config.detectTime) RaiseAlarm(player.transform.position);
            }
            else
            {
                seen = Mathf.Max(0f, seen - config.detectTime * config.detectDecay * dt);
                if (State == SecurityCameraState.Detecting && seen <= 0f) SetState(SecurityCameraState.Sweeping);
                if (State == SecurityCameraState.Sweeping) Sweep(dt);
            }
            head.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Sweep(float dt)
        {
            if (config.sweepAngle <= 0f) { angle = Mathf.MoveTowardsAngle(angle, baseAngle, config.sweepSpeed * dt); return; }
            // Vai e volta em volta da direção inicial; depois de seguir o Thomas, retoma de onde parou.
            sweepT += dt * config.sweepSpeed / config.sweepAngle;
            float target = baseAngle + (Mathf.PingPong(sweepT, 1f) - 0.5f) * config.sweepAngle;
            angle = Mathf.MoveTowardsAngle(angle, target, config.sweepSpeed * 2f * dt);
        }

        private bool CanSee()
        {
            if (player == null || !player.isActiveAndEnabled) return false;
            Vector2 from = transform.position;
            Vector2 to = (Vector2)player.transform.position - from;
            if (to.sqrMagnitude > config.range * config.range) return false;
            if (Vector2.Angle(Facing, to) > config.coneAngle * 0.5f) return false;
            return !Physics2D.Linecast(from, player.transform.position, obstacles);
        }

        private void RaiseAlarm(Vector2 seenAt)
        {
            stateTimer = config.alarmDuration;
            SetState(SecurityCameraState.Alarm);

            // Apagão do setor: as salas apagam até o jogador religar (decisão de 22/11, comentário da #79).
            foreach (var b in FindObjectsByType<Breaker>(FindObjectsSortMode.None))
                if (b != shutdownBreaker && Vector2.Distance(b.transform.position, transform.position) <= config.sectorRadius)
                    b.Cut();

            // Fase 3 em diante: a câmera entrega o Thomas ao Espécime (planejamento-rc 3, "Câmeras").
            var profile = DifficultyProfile.Active;
            if (profile != null && profile.camerasCallSpecimen) NoiseBus.Emit(seenAt, config.callNoiseRadius);

            AlarmRaised?.Invoke(seenAt);
        }

        private Breaker NearestBreaker(float radius)
        {
            Breaker best = null;
            float bestD = radius;
            foreach (var b in FindObjectsByType<Breaker>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(b.transform.position, transform.position);
                if (d <= bestD) { best = b; bestD = d; }
            }
            return best;
        }

        private void SetState(SecurityCameraState state)
        {
            if (State == state) return;
            State = state;
            if (state != SecurityCameraState.Detecting) SeesPlayer = false;
            Apply();
            StateChanged?.Invoke(state);
        }

        private void Apply()
        {
            if (art != null)
            {
                var sprite = State switch
                {
                    SecurityCameraState.Detecting => detectSprite,
                    SecurityCameraState.Alarm => alarmSprite,
                    SecurityCameraState.Disabled or SecurityCameraState.Off => offSprite,
                    _ => idleSprite,
                };
                if (sprite != null) art.sprite = sprite;
            }
            bool watching = State == SecurityCameraState.Sweeping || State == SecurityCameraState.Detecting
                || State == SecurityCameraState.Alarm;
            if (cone != null)
            {
                cone.enabled = watching;
                if (config != null)
                {
                    cone.pointLightOuterRadius = config.range;
                    cone.pointLightOuterAngle = config.coneAngle;
                    cone.pointLightInnerAngle = config.coneAngle * 0.6f;
                    cone.intensity = config.coneIntensity;
                    cone.color = State switch
                    {
                        SecurityCameraState.Detecting => config.coneDetectColor,
                        SecurityCameraState.Alarm => config.coneAlarmColor,
                        _ => config.coneIdleColor,
                    };
                }
            }
            if (led != null)
            {
                led.enabled = watching;
                led.intensity = 1f;
                led.color = State switch
                {
                    SecurityCameraState.Detecting => new Color(1f, 0.7f, 0.2f),
                    SecurityCameraState.Alarm => new Color(1f, 0.15f, 0.1f),
                    _ => new Color(0.3f, 0.8f, 0.45f),
                };
            }
        }

        // LED pisca no alarme; o cone pulsa junto.
        private void UpdateVisual()
        {
            if (State != SecurityCameraState.Alarm) return;
            bool on = Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
            if (led != null) led.intensity = on ? 1.2f : 0.1f;
            if (art != null && alarmSprite != null && detectSprite != null) art.sprite = on ? alarmSprite : detectSprite;
            if (cone != null && config != null) cone.intensity = config.coneIntensity * (on ? 1.3f : 0.7f);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, config.sectorRadius);
            Vector2 f = Facing;
            float half = config.coneAngle * 0.5f;
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Quaternion.Euler(0, 0, half) * f * config.range));
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Quaternion.Euler(0, 0, -half) * f * config.range));
        }
    }
}
