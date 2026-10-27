using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Disjuntor / quadro elétrico (GDD 4.2, 4.3 e regra 6). Dono: Thiago. Issue #10.
    // Segurar E por 4s acende a lâmpada de teto (Zona Segura) por 60s.
    public class Breaker : MonoBehaviour, IInteractable
    {
        [SerializeField] private float repairDuration = 4f;
        [SerializeField] private float lightDuration = 60f;
        [SerializeField] private float repairNoiseRadius = 7f;

        public string Prompt => "Reparar disjuntor";
        public bool CanInteract => !isActive;
        public float HoldDuration => repairDuration;
        public bool IsActive => isActive;

        private bool isActive;

        public void BeginInteract()
        {
            // O ruído do reparo atrai o monstro (GDD 4.3).
            NoiseBus.Emit(transform.position, repairNoiseRadius);
            // TODO(Thiago): faíscas / som de reparo.
        }

        public void TickInteract(float progress)
        {
            // TODO(Thiago): feedback visual do progresso, se quiser.
        }

        public void CancelInteract()
        {
            // TODO(Thiago): parar faíscas / som.
        }

        public void CompleteInteract()
        {
            isActive = true;
            // TODO(Thiago): ligar a SafeZone (Light2D de teto + trigger que chama
            // SanityController.SetInSafeZone e bloqueia o monstro) por lightDuration segundos,
            // depois isActive = false para poder reparar de novo.
        }
    }
}
