using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Level;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Terminal da eclusa (GDD 5: inserir os 3 HDs no terminal; issue #44). Evolução do ExitDoor: a porta
    // externa só abre depois que os HDs entram aqui, e isso dispara a purga (Level.EscapeSequence).
    [RequireComponent(typeof(Collider2D))]
    public class EclusaTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private EscapeConfig config;
        [Tooltip("Gavetas acesas uma por HD inserido (opcional).")]
        [SerializeField] private SpriteRenderer[] slots = Array.Empty<SpriteRenderer>();
        [SerializeField] private Color slotEmpty = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color slotReady = new Color(0.85f, 0.65f, 0.15f);
        [SerializeField] private Color slotInserted = new Color(0.3f, 1f, 0.45f);

        public bool Activated { get; private set; }
        public event Action<EclusaTerminal> ActivatedEvent;

        private static GameManager Gm => GameManager.Instance;
        private bool HasDrives => Gm == null || Gm.HasAllDrives;

        // Sem número: quantos HDs faltam o relógio do HUD já mostra.
        public string Prompt => HasDrives ? "Inserir HDs na eclusa" : "Faltam HDs";
        public bool CanInteract => !Activated && HasDrives && (Gm == null || Gm.IsPlaying);
        public float HoldDuration => config != null ? config.insertHoldSeconds : 3f;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            int have = Gm != null ? Gm.DrivesCollected : 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].color = Activated ? slotInserted : i < have ? slotReady : slotEmpty;
            }
        }

        public void BeginInteract() { }
        public void TickInteract(float progress) { }
        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (Activated) return;
            Activated = true;
            if (config != null) NoiseBus.Emit(transform.position, config.activationNoiseRadius);
            ActivatedEvent?.Invoke(this);
        }
    }
}
