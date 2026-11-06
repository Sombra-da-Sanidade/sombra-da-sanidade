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
        public bool IsProgressPaused => Time.time < pausedUntil;

        // Segura E por código (testes em batchmode, que não têm input). Não usar no jogo.
        public bool HoldOverride { get; set; }

        private readonly Collider2D[] hits = new Collider2D[16];
        private Vector2 channelStartPos;
        private float pausedUntil;

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                Cancel();
                return;
            }

            var kb = Keyboard.current;
            bool held = HoldOverride || (kb != null && kb.eKey.isPressed);

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
            if (!IsProgressPaused)
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

        // Soma ou tira progresso da canalização atual (skill check, #33). Sem canalização, não faz nada.
        public void AddProgress(float delta)
        {
            if (!IsChanneling) return;
            Progress = Mathf.Clamp01(Progress + delta);
        }

        // Congela o progresso por alguns segundos (falha no skill check). Segurar E continua valendo.
        public void PauseProgress(float seconds)
        {
            pausedUntil = Mathf.Max(pausedUntil, Time.time + seconds);
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
        // Prompt sem número (GDD 8): tecla E e o nome da ação; segurando, a barra enche (hud_bars.json).
        // Fica sob os pés do Thomas para não brigar com a pilha e o multímetro. Issue #64.
        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (Current == null || !Current.CanInteract) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (UI.UpgradeTerminal.IsOpen || UI.ModalScreen.IsOpen) return;
            var cam = Camera.main;
            if (cam == null || !UI.PixelHud.HasArt) return;

            UI.PixelHud.Begin();
            var p = UI.PixelHud.WorldToGame(cam, transform.position + Vector3.down * 0.35f);
            if (IsChanneling)
            {
                string bar = IsProgressPaused ? "hold_paused" : "hold_" + Mathf.Clamp(Mathf.RoundToInt(Progress * 8f), 0, 8);
                UI.PixelHud.Sprite("hud_icons", "key_e", p.x - 13, p.y);
                UI.PixelHud.Sprite("hud_bars", bar, p.x - 3, p.y);
                return;
            }
            string label = Current.Prompt;
            int width = 8 + 3 + UI.PixelHud.TextWidth(label);
            int left = p.x - width / 2;
            UI.PixelHud.Sprite("hud_icons", "key_e", left, p.y);
            UI.PixelHud.Box(left + 11, p.y + 1, width - 11 + 2, 7, UI.PixelHud.Vazio, 0.55f);
            UI.PixelHud.Text(label, left + 12, p.y + 2, UI.PixelHud.Concreto7);
        }
    }
}
