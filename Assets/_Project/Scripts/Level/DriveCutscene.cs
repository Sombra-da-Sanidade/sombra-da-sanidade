using SombraDaSanidade.Core;
using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Toca uma cutscene quando o HD da ala é coletado (C3, a revelação depois do HD-03; planejamento-rc 4).
    // O CutscenePlayer enfileira: se o log da Vance ou a escolha de upgrade abrirem junto, nada se perde.
    public class DriveCutscene : MonoBehaviour
    {
        [SerializeField] private Cutscene cutscene;
        [Tooltip("Atraso depois da coleta (segundos), para o som do HD tocar antes.")]
        [SerializeField, Min(0f)] private float delay = 1f;

        public bool Triggered { get; private set; }
        public Cutscene Cutscene => cutscene;

        public void Setup(Cutscene scene) => cutscene = scene;

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDriveCollected;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
        }

        private void OnDriveCollected(int total)
        {
            if (Triggered) return;
            Triggered = true;
            if (cutscene != null) Invoke(nameof(Play), delay);
        }

        private void Play() => CutscenePlayer.Play(cutscene);
    }
}
