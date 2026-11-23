using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Apagão final da Ala de Energia (planejamento-rc 1, fase 2): ao ejetar o HD da ala, a subestação cai e
    // todas as salas apagam (Breaker.Cut, o mesmo do alarme da câmera). O Sonâmbulo corre no escuro, então a
    // volta até a porta é o pico da fase. Os disjuntores podem ser religados: o jogador escolhe onde parar.
    public class WingBlackout : MonoBehaviour
    {
        [Tooltip("Atraso entre o HD sair e as luzes caírem (segundos), para o jogador ouvir o aviso antes.")]
        [SerializeField, Min(0f)] private float delay = 1.5f;
        [SerializeField] private SoundEvent sound;

        public bool Triggered { get; private set; }

        public void Setup(SoundEvent blackoutSound) => sound = blackoutSound;

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
            Invoke(nameof(CutAll), delay);
        }

        // Público para o teste da ala.
        public void CutAll()
        {
            Triggered = true;
            if (sound != null) AudioService.Play(sound);
            foreach (var b in FindObjectsByType<Breaker>(FindObjectsSortMode.None)) b.Cut();
        }
    }
}
