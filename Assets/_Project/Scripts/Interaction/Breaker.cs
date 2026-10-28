using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Disjuntor / quadro elétrico (GDD 4.2, 4.3 e regra 6). Frente 4 (Disjuntor e mapa). Issue #10.
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
            // TODO(Frente 4): faíscas / som de reparo.
        }

        public void TickInteract(float progress)
        {
            // TODO(Frente 4): feedback visual do progresso, se quiser.
        }

        public void CancelInteract()
        {
            // TODO(Frente 4): parar faíscas / som.
        }

        public void CompleteInteract()
        {
            isActive = true;
            // TODO(Frente 4): ligar a SafeZone (Light2D de teto + trigger que chama
            // SanityController.SetInSafeZone e bloqueia o monstro) por lightDuration segundos,
            // depois isActive = false para poder reparar de novo.
        }
    }
}
