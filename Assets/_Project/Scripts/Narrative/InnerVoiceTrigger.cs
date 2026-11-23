using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Dispara uma fala do Thomas quando ele entra no trigger ou por evento (#89).
    [RequireComponent(typeof(Collider2D))]
    public class InnerVoiceTrigger : MonoBehaviour
    {
        [SerializeField] private InnerVoiceLine line;

        [Tooltip("Se marcado, só dispara quando a sanidade estiver abaixo de sanityBelow.")]
        [SerializeField, Range(1f, 101f)] private float sanityBelow = 101f;

        private bool triggered;

        public void Setup(InnerVoiceLine voiceLine, float belowSanity = 101f)
        {
            line = voiceLine;
            sanityBelow = belowSanity;
        }

        public void Trigger()
        {
            if (triggered || line == null) return;
            triggered = true;
            InnerVoicePlayer.Play(line);
        }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (WingNarrativeDirector.IsAutoSuppressed) return;
            if (triggered || line == null || !other.CompareTag("Player")) return;
            if (sanityBelow <= 100f)
            {
                var sanity = FindAnyObjectByType<SanityController>();
                if (sanity == null || sanity.Value >= sanityBelow) return;
            }
            Trigger();
        }
    }
}
