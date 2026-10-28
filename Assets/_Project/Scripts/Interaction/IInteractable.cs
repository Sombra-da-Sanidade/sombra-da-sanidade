using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Qualquer objeto que o jogador usa segurando E (disjuntor, servidor de HD, armário).
    // O objeto precisa de um Collider2D para ser encontrado pelo PlayerInteractor.
    public interface IInteractable
    {
        // Texto curto mostrado perto do jogador, ex.: "Reparar disjuntor".
        string Prompt { get; }

        // false quando já foi usado ou está bloqueado.
        bool CanInteract { get; }

        // Segundos segurando E até concluir. 0 = instantâneo.
        float HoldDuration { get; }

        Transform transform { get; }

        void BeginInteract();

        // Progresso de 0 a 1 enquanto o jogador segura E.
        void TickInteract(float progress);

        void CancelInteract();

        void CompleteInteract();
    }
}
