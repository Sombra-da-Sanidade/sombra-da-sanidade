using System;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Servidor com a Gaveta de HD (GDD 4.3). Frente 5 (Partida, HDs e build). Issue #11.
    // Segurar E ejeta o HD: soma no GameManager (que acelera o monstro) e faz barulho.
    public class DriveServer : MonoBehaviour, IInteractable
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

        private bool ejected;

        private void Awake()
        {
            if (config == null) Debug.LogError($"{name}: DriveServer sem InteractionConfig.", this);
        }

        private void Start()
        {
            if (indicator != null) indicator.color = loadedColor;
        }

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            ejected = true;
            if (indicator != null) indicator.color = emptyColor;
            NoiseBus.Emit(transform.position, config.ejectNoiseRadius);
            if (logClip != null) AudioSource.PlayClipAtPoint(logClip, transform.position);
            if (GameManager.Instance != null) GameManager.Instance.CollectDrive();
            Ejected?.Invoke(this);
        }
    }
}
