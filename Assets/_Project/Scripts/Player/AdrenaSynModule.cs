using SombraDaSanidade.Interaction;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Player
{
    // Módulo Adrena-Syn (GDD 6 e Pilar 2, Tecnologia Experimental e Risco): +35% de velocidade por 15 s
    // ao custo de -20 de sanidade na hora. Usado com E, sem inventário, no padrão do LithiumStabilizer.
    // Também sai dos armários (Locker). Frente 1. Issue #34.
    public class AdrenaSynModule : MonoBehaviour, IInteractable
    {
        [Tooltip("Jogador. Vazio: procura um na cena.")]
        [SerializeField] private PlayerController player;

        public string Prompt => "Injetar Adrena-Syn (-sanidade)";
        public bool CanInteract => !used && player != null && player.Config != null;
        public float HoldDuration => 0f;

        private bool used;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
        }

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            used = true;
            Apply(player);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            foreach (var l in GetComponentsInChildren<Light2D>()) l.enabled = false;
        }

        // Efeito do módulo, também usado pelo loot dos armários. O custo pode levar a sanidade a 0:
        // é o risco do pilar 2, sem trava.
        public static void Apply(PlayerController player)
        {
            if (player == null || player.Config == null) return;
            var sanity = player.GetComponent<SanityController>();
            if (sanity != null && sanity.Config != null) sanity.Add(-sanity.Config.adrenaSynCost);
            AdrenaSynEffect.On(player).Activate(player.Config.adrenaSynSpeedBonus, player.Config.adrenaSynDuration);
        }
    }
}
