using System.Collections;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Sanity
{
    // Estabilizador de Lítio (GDD 4.3 e 6.3): recupera muito de uma vez (+30), mas deixa Thomas lento
    // por alguns segundos. Usado na hora com E, sem inventário. Some depois de usado. Frente 3 (Sanidade e HUD).
    public class LithiumStabilizer : MonoBehaviour, IInteractable
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        public string Prompt => "Usar Estabilizador de Lítio";
        public bool CanInteract => !used && sanity != null;
        public float HoldDuration => 0f;

        private bool used;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
        }

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            used = true;

            var config = sanity.Config;
            sanity.Add(config.stabilizerAmount);
            Hide();

            var player = sanity.GetComponent<PlayerController>();
            if (player != null) StartCoroutine(SlowDown(player, config.stabilizerSlowMultiplier, config.stabilizerSlowDuration));
        }

        // Multiplica e divide em vez de voltar para 1, para não apagar outro efeito (ex.: Adrena-Syn).
        private static IEnumerator SlowDown(PlayerController player, float multiplier, float duration)
        {
            if (multiplier <= 0f) yield break;
            player.SpeedMultiplier *= multiplier;
            yield return new WaitForSeconds(duration);
            if (player != null) player.SpeedMultiplier /= multiplier;
        }

        // Esconde sem desativar o objeto, senão a corrotina do efeito para no meio.
        private void Hide()
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            foreach (var l in GetComponentsInChildren<Light2D>()) l.enabled = false;
        }
    }
}
