using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Terminal com uma gravação avulsa (GDD 9, issue #39). Segurar E toca o log pelo AudioLogPlayer da cena.
    // Os logs principais tocam ao coletar os HDs; terminais servem para logs extras que o mapa quiser.
    // O jogador pode andar enquanto ouve, mas se afastar demais do terminal para a fita.
    [RequireComponent(typeof(Collider2D))]
    public class AudioLogTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private AudioLog log;
        [SerializeField] private NarrativeConfig config;

        [Tooltip("Tela do terminal: pisca enquanto a fita toca. Opcional.")]
        [SerializeField] private SpriteRenderer screen;

        public AudioLog Log => log;
        public bool IsPlaying => AudioLogPlayer.Instance != null && AudioLogPlayer.Instance.Source == transform;

        public string Prompt => AudioLogArchive.HasHeard(log) ? "Ouvir a gravação de novo" : "Ouvir a gravação";
        public bool CanInteract => log != null && AudioLogPlayer.Instance != null && !AudioLogPlayer.Instance.IsPlaying;
        public float HoldDuration => Config.holdDuration;

        private NarrativeConfig Config => config != null ? config : (config = ScriptableObject.CreateInstance<NarrativeConfig>());

        private void Start()
        {
            if (log == null) Debug.LogWarning($"{name}: AudioLogTerminal sem AudioLog no Inspector.", this);
            if (AudioLogPlayer.Instance == null) Debug.LogWarning($"{name}: cena sem AudioLogPlayer, o terminal não toca.", this);
        }

        private void Update()
        {
            if (screen != null) screen.enabled = !IsPlaying || Mathf.Repeat(Time.time * 2f, 1f) < 0.7f;
        }

        public void BeginInteract() { }
        public void TickInteract(float progress) { }
        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (AudioLogPlayer.Instance != null) AudioLogPlayer.Instance.Play(log, transform, Config.hearingRadius);
        }
    }
}
