using System;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Servidor com a Gaveta de HD (GDD 4.3). Issue #11; skill check (#33) da Frente 1.
    // Segurar E ejeta o HD (12 s com skill check de multímetro): soma no GameManager (que acelera o monstro)
    // e faz barulho. Depois da metade, o servidor faz ruído baixo enquanto ejeta. Ao ejetar, abre a escolha
    // de upgrade da Lux-90 (#31).
    public class DriveServer : MonoBehaviour, IInteractable, ISkillCheckable
    {
        [SerializeField] private InteractionConfig config;
        [Tooltip("Log de áudio tocado ao ejetar o HD (GDD 9: logs da Dra. Vance). Vazio na mini demo.")]
        [SerializeField] private AudioClip logClip;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private Color loadedColor = new Color(0.3f, 1f, 0.45f);
        [SerializeField] private Color emptyColor = new Color(0.25f, 0.25f, 0.25f);

        public string Prompt => "Ejetar HD";
        public bool CanInteract => !ejected && config != null;
        public float HoldDuration => config.ejectDuration;

        // Disparado ao ejetar: gancho para logs, legendas e efeitos que vierem depois.
        public event Action<DriveServer> Ejected;

        // Skill check (#33). Falhas ficam para o bônus do rank (#49).
        public InteractionConfig SkillCheckConfig => config;
        public bool CanSkillCheck => !ejected && config != null && SkillChecksDone < config.skillCheckMaxPerDrive;
        public int SkillChecksDone { get; private set; }
        public int SkillCheckFailures { get; private set; }
        public event Action<DriveServer, SkillCheckResult> SkillCheckResolved;

        private bool ejected;
        private float nextLateNoise;

        private void Awake()
        {
            if (config == null) Debug.LogError($"{name}: DriveServer sem InteractionConfig.", this);
        }

        private void Start()
        {
            if (indicator != null) indicator.color = loadedColor;
            EnsurePlayerComponents();
        }

        // Cenas geradas antes do #33 não têm o SkillCheck no jogador: cria aqui.
        private static void EnsurePlayerComponents()
        {
            var interactor = FindAnyObjectByType<PlayerInteractor>();
            if (interactor == null) return;
            if (interactor.GetComponent<SkillCheck>() == null) interactor.gameObject.AddComponent<SkillCheck>();
            if (interactor.GetComponent<UI.SkillCheckHud>() == null) interactor.gameObject.AddComponent<UI.SkillCheckHud>();
            if (interactor.GetComponent<UI.UpgradeTerminal>() == null) interactor.gameObject.AddComponent<UI.UpgradeTerminal>();
        }

        public void BeginInteract()
        {
            nextLateNoise = 0f;
        }

        public void TickInteract(float progress)
        {
            if (progress < config.ejectLateNoiseFrom || Time.time < nextLateNoise) return;
            nextLateNoise = Time.time + config.ejectLateNoiseInterval;
            NoiseBus.Emit(transform.position, config.ejectLateNoiseRadius);
        }

        public void OnSkillCheckStarted()
        {
            SkillChecksDone++;
        }

        public void OnSkillCheckResolved(SkillCheckResult result)
        {
            if (result == SkillCheckResult.Fail) SkillCheckFailures++;
            SkillCheckResolved?.Invoke(this, result);
        }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            ejected = true;
            if (indicator != null) indicator.color = emptyColor;
            NoiseBus.Emit(transform.position, config.ejectNoiseRadius);
            if (logClip != null) AudioSource.PlayClipAtPoint(logClip, transform.position);
            if (GameManager.Instance != null) GameManager.Instance.CollectDrive();
            Ejected?.Invoke(this);
            OpenUpgradeTerminal();
        }

        // Recompensa concreta do HD (#31): escolha de 1 de 2 upgrades da Lux-90, com o jogo pausado.
        private void OpenUpgradeTerminal()
        {
            if (config.upgradeTree == null) return;
            var terminal = FindAnyObjectByType<UI.UpgradeTerminal>();
            int drive = GameManager.Instance != null ? GameManager.Instance.DrivesCollected : 1;
            if (terminal != null) terminal.Open(config.upgradeTree, drive);
        }
    }
}
