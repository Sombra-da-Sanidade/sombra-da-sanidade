using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.Interaction
{
    // Fica no Player. Encontra o IInteractable mais próximo e conduz a canalização segurando E.
    // Mover-se cancela a canalização (GDD 4.4: "fica parado e exposto durante o reparo").
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 1.2f;
        [SerializeField] private float cancelMoveThreshold = 0.05f;

        public IInteractable Current { get; private set; }
        public bool IsChanneling { get; private set; }
        public float Progress { get; private set; }

        private readonly Collider2D[] hits = new Collider2D[16];
        private Vector2 channelStartPos;
        private GUIStyle promptStyle;

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                Cancel();
                return;
            }

            var kb = Keyboard.current;
            bool held = kb != null && kb.eKey.isPressed;

            if (!IsChanneling) Current = FindNearest();
            if (Current == null || !Current.CanInteract)
            {
                Cancel();
                return;
            }

            if (!held)
            {
                Cancel();
                return;
            }

            if (!IsChanneling)
            {
                IsChanneling = true;
                Progress = 0f;
                channelStartPos = transform.position;
                Current.BeginInteract();
            }

            if (((Vector2)transform.position - channelStartPos).sqrMagnitude > cancelMoveThreshold * cancelMoveThreshold)
            {
                Cancel();
                return;
            }

            float duration = Current.HoldDuration;
            Progress = duration <= 0f ? 1f : Mathf.Clamp01(Progress + Time.deltaTime / duration);
            Current.TickInteract(Progress);

            if (Progress >= 1f)
            {
                var done = Current;
                IsChanneling = false;
                Progress = 0f;
                done.CompleteInteract();
            }
        }

        private void Cancel()
        {
            if (IsChanneling && Current != null) Current.CancelInteract();
            IsChanneling = false;
            Progress = 0f;
        }

        private IInteractable FindNearest()
        {
            int count = Physics2D.OverlapCircle(transform.position, reach, ContactFilter2D.noFilter, hits);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;
                float d = ((Vector2)hits[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = candidate;
                }
            }
            return best;
        }

        // Prompt provisório para a mini demo. A UI final fica com o sistema de UI.
        private void OnGUI()
        {
            if (Current == null || !Current.CanInteract) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            var cam = Camera.main;
            if (cam == null) return;

            promptStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            promptStyle.normal.textColor = new Color(0.75f, 0.95f, 0.75f);

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
            var rect = new Rect(screen.x - 150, Screen.height - screen.y - 30, 300, 24);
            string text = IsChanneling
                ? $"{Current.Prompt}... {Mathf.RoundToInt(Progress * 100)}%"
                : $"[E] {Current.Prompt}";
            GUI.Label(rect, text, promptStyle);
        }
    }
}
