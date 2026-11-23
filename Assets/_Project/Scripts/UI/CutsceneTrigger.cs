using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Toca uma cutscene quando o Thomas entra no trigger (#88). Usado pelas alas (RC2) nos gatilhos de C1, C2,
    // M1, M2 e M3 (planejamento-rc 4). Toca uma vez por partida.
    [RequireComponent(typeof(Collider2D))]
    public class CutsceneTrigger : MonoBehaviour
    {
        [SerializeField] private Cutscene cutscene;

        [Tooltip("Só dispara com a sanidade abaixo deste valor (M3, o espelho: 35). 101 = sempre.")]
        [SerializeField, Range(1f, 101f)] private float sanityBelow = 101f;

        private bool played;

        public Cutscene Cutscene => cutscene;

        public void Setup(Cutscene scene, float belowSanity = 101f)
        {
            cutscene = scene;
            sanityBelow = belowSanity;
        }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Core.Checkpoint.PathOverride != null) return;
            if (played || cutscene == null || !other.CompareTag("Player") || CutscenePlayer.IsPlaying) return;
            if (sanityBelow <= 100f)
            {
                var sanity = FindAnyObjectByType<SanityController>();
                if (sanity == null || sanity.Value >= sanityBelow) return;
            }
            played = true;
            CutscenePlayer.Play(cutscene);
        }
    }
}
