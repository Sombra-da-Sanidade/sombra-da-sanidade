using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.Interaction
{
    public enum SkillCheckState { Idle, Warning, Active, Result }

    // Skill check de multímetro na ejeção de HD (GDD 4.4; plano em docs/skill-check-hd.md). Fica no Player.
    // Enquanto o jogador canaliza um ISkillCheckable: bipe de aviso, o ponteiro varre o mostrador e
    // Espaço precisa cair na faixa. Errar (ou não apertar) tira progresso, pausa, faz barulho e treme a tela.
    // O DriveServer cria este componente sob demanda se a cena não tiver. Frente 1. Issue #33.
    [RequireComponent(typeof(PlayerInteractor))]
    public class SkillCheck : MonoBehaviour
    {
        public SkillCheckState State { get; private set; } = SkillCheckState.Idle;
        // Ponteiro de 0 a 1 (já com o tremor de sanidade baixa).
        public float NeedlePosition { get; private set; }
        // Faixa good: começa em ZoneStart e tem ZoneSize. A great é o começo da faixa (GreatSize).
        public float ZoneStart { get; private set; }
        public float ZoneSize { get; private set; }
        public float GreatSize { get; private set; }
        public SkillCheckResult LastResult { get; private set; }
        public bool IsPaused => Time.time < pauseUntil;
        // Ponteiro tremendo pela sanidade baixa: o mostrador puxa para o roxo de Hipnos (SkillCheckHud).
        public bool IsShaky { get; private set; }

        public event Action Warned;
        public event Action<SkillCheckResult> Resolved;

        private PlayerInteractor interactor;
        private SanityController sanity;
        private CameraFollow cameraFollow;
        private ISkillCheckable target;
        private float channelTime;
        private float stateTime;
        private float pauseUntil;
        private float jitterSeed;

        // Tempo que o resultado fica no mostrador antes de sumir. Só visual.
        private const float ResultDisplay = 0.5f;

        private void Awake()
        {
            interactor = GetComponent<PlayerInteractor>();
            sanity = GetComponent<SanityController>();
            jitterSeed = UnityEngine.Random.value * 100f;
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
        }

        private void Update()
        {
            var current = interactor.IsChanneling ? interactor.Current as ISkillCheckable : null;
            if (current != target)
            {
                target = current;
                channelTime = 0f;
                if (State != SkillCheckState.Result) State = SkillCheckState.Idle;
            }
            if (target == null)
            {
                // Soltar E no meio do check só cancela, sem penalidade (o progresso já zera).
                if (State == SkillCheckState.Warning || State == SkillCheckState.Active) State = SkillCheckState.Idle;
                TickResult();
                return;
            }

            var config = target.SkillCheckConfig;
            if (config == null) return;
            channelTime += Time.deltaTime;
            stateTime += Time.deltaTime;

            switch (State)
            {
                case SkillCheckState.Idle:
                    if (channelTime >= config.skillCheckStartDelay && !IsPaused
                        && UnityEngine.Random.value < config.skillCheckChancePerSecond * Time.deltaTime)
                        StartCheck();
                    break;

                case SkillCheckState.Warning:
                    if (stateTime >= config.skillCheckWarning) Enter(SkillCheckState.Active);
                    break;

                case SkillCheckState.Active:
                    float raw = config.skillCheckSweep > 0f ? stateTime / config.skillCheckSweep : 1f;
                    NeedlePosition = Mathf.Clamp01(raw + Jitter(config));
                    var kb = Keyboard.current;
                    if (kb != null && kb.spaceKey.wasPressedThisFrame) ResolveAt(NeedlePosition);
                    else if (raw >= 1f) ResolveAt(1f);   // passou do fim sem apertar: falha
                    break;

                case SkillCheckState.Result:
                    TickResult();
                    break;
            }
        }

        // Começa um check agora (o sorteio por segundo chama isto; testes também). false se não der.
        public bool StartCheck()
        {
            if (target == null || !target.CanSkillCheck || target.SkillCheckConfig == null) return false;
            var config = target.SkillCheckConfig;
            int drives = GameManager.Instance != null ? GameManager.Instance.DrivesCollected : 0;
            bool monsterNear = sanity != null && sanity.IsMonsterNear;
            float factor = monsterNear ? 1f - config.skillCheckMonsterNearShrink : 1f;
            ZoneSize = ZoneSizeFor(config, drives, monsterNear);
            GreatSize = Mathf.Min(config.skillCheckGreatSize * factor, ZoneSize);
            // A faixa nunca nasce no comecinho: o jogador precisa de tempo para reagir.
            ZoneStart = UnityEngine.Random.Range(config.skillCheckZoneMin, Mathf.Max(config.skillCheckZoneMin, 1f - ZoneSize - 0.05f));
            NeedlePosition = 0f;
            target.OnSkillCheckStarted();
            Enter(SkillCheckState.Warning);
            Warned?.Invoke();
            return true;
        }

        // Faixa good: encolhe por HD já coletado (15, 13, 11%) e com o monstro perto (-40%).
        public static float ZoneSizeFor(InteractionConfig config, int drivesCollected, bool monsterNear)
        {
            float size = config.skillCheckGoodSize - config.skillCheckShrinkPerDrive * Mathf.Max(0, drivesCollected);
            if (monsterNear) size *= 1f - config.skillCheckMonsterNearShrink;
            return Mathf.Max(0.02f, size);
        }

        // Julga o aperto com o ponteiro em needle e aplica o efeito. Público para os testes.
        public SkillCheckResult ResolveAt(float needle)
        {
            if (State != SkillCheckState.Active || target == null) return LastResult;
            var config = target.SkillCheckConfig;

            SkillCheckResult result;
            if (needle >= ZoneStart && needle <= ZoneStart + GreatSize) result = SkillCheckResult.Great;
            else if (needle >= ZoneStart && needle <= ZoneStart + ZoneSize) result = SkillCheckResult.Good;
            else result = SkillCheckResult.Fail;

            switch (result)
            {
                case SkillCheckResult.Great:
                    interactor.AddProgress(config.skillCheckGreatBonus);
                    break;
                case SkillCheckResult.Fail:
                    interactor.AddProgress(-config.skillCheckFailPenalty);
                    pauseUntil = Time.time + config.skillCheckFailPause;
                    interactor.PauseProgress(config.skillCheckFailPause);
                    // Faísca e estalo do multímetro: o monstro ouve de longe.
                    NoiseBus.Emit(((MonoBehaviour)target).transform.position, config.skillCheckFailNoiseRadius);
                    if (cameraFollow != null) cameraFollow.Shake(config.skillCheckFailShakeDuration, config.skillCheckFailShakeMagnitude);
                    break;
            }

            LastResult = result;
            Enter(SkillCheckState.Result);
            target.OnSkillCheckResolved(result);
            Resolved?.Invoke(result);
            return result;
        }

        // Sanidade baixa: o ponteiro treme (GDD 4.2, a mente atrapalha as mãos).
        private float Jitter(InteractionConfig config)
        {
            IsShaky = sanity != null && sanity.Value < config.skillCheckJitterBelowSanity;
            if (!IsShaky) return 0f;
            return (Mathf.PerlinNoise(jitterSeed, Time.time * 12f) - 0.5f) * 2f * config.skillCheckJitterAmount;
        }

        private void TickResult()
        {
            if (State != SkillCheckState.Result) return;
            stateTime += target == null ? Time.deltaTime : 0f;
            if (stateTime >= ResultDisplay) State = SkillCheckState.Idle;
        }

        private void Enter(SkillCheckState state)
        {
            State = state;
            stateTime = 0f;
        }
    }
}
