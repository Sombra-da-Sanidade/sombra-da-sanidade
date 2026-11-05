using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    public enum LootKind { Nothing, Battery, Sedative }

    // Armário da base (GDD 6, Economia): segurar E abre, faz barulho e pode ter bateria sobressalente (+30)
    // ou sedativo (mesmo efeito do Estabilizador de Lítio). Abre uma vez só. Frente 1. Issue #29.
    public class Locker : MonoBehaviour, IInteractable
    {
        [SerializeField] private InteractionConfig config;
        [Tooltip("Sprite que escurece depois de saqueado. Vazio: usa o SpriteRenderer do próprio objeto.")]
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Fração da cor original depois de aberto (marca visual de armário já saqueado).")]
        [SerializeField, Range(0f, 1f)] private float openedTint = 0.45f;

        public string Prompt => "Abrir armário";
        public bool CanInteract => !opened && config != null;
        public float HoldDuration => config.lockerOpenDuration;

        public bool IsOpened => opened;
        public LootKind Loot { get; private set; }

        // Disparado ao abrir, com o que saiu: gancho para som, HUD e o ajuste dinâmico de bateria.
        public event Action<Locker, LootKind> Opened;

        private bool opened;
        private FlashlightController flashlight;
        private SanityController sanity;
        private float feedbackUntil;
        private string feedbackText;
        private GUIStyle feedbackStyle;

        private const float FeedbackDuration = 1.8f;

        private void Awake()
        {
            if (config == null) Debug.LogError($"{name}: Locker sem InteractionConfig.", this);
            if (body == null) body = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            flashlight = FindAnyObjectByType<FlashlightController>();
            sanity = FindAnyObjectByType<SanityController>();
        }

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            Open(RollLoot());
        }

        // Sorteio do GDD 6: lootChance de ter algo; dentro disso, batteryShare de ser bateria.
        public LootKind RollLoot()
        {
            if (UnityEngine.Random.value >= config.lockerLootChance) return LootKind.Nothing;
            return UnityEngine.Random.value < config.lockerBatteryShare ? LootKind.Battery : LootKind.Sedative;
        }

        // Separado do sorteio para os testes escolherem o conteúdo.
        public void Open(LootKind loot)
        {
            if (opened) return;
            opened = true;
            Loot = loot;

            if (body != null) body.color = new Color(body.color.r * openedTint, body.color.g * openedTint, body.color.b * openedTint, body.color.a);
            // Porta de metal: o monstro pode ouvir (GDD 4.3, interações fazem barulho).
            NoiseBus.Emit(transform.position, config.lockerNoiseRadius);

            switch (loot)
            {
                case LootKind.Battery:
                    float amount = flashlight != null && flashlight.Config != null ? flashlight.Config.batteryPickupAmount : 0f;
                    if (flashlight != null) flashlight.Recharge(amount);
                    ShowFeedback($"Bateria +{amount:0}");
                    break;
                case LootKind.Sedative:
                    // A corrotina da lentidão roda na sanidade do jogador, que continua ativa até o fim do efeito.
                    LithiumStabilizer.ApplyEffect(sanity, sanity);
                    ShowFeedback("Sedativo: +sanidade, passos lentos");
                    break;
                default:
                    ShowFeedback("Vazio");
                    break;
            }

            Opened?.Invoke(this, loot);
        }

        private void ShowFeedback(string text)
        {
            feedbackText = text;
            feedbackUntil = Time.unscaledTime + FeedbackDuration;
        }

        // Texto provisório sobre o armário, no padrão do prompt do PlayerInteractor.
        private void OnGUI()
        {
            if (Time.unscaledTime >= feedbackUntil) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            var cam = Camera.main;
            if (cam == null) return;

            feedbackStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            feedbackStyle.normal.textColor = Loot == LootKind.Nothing ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.95f, 0.85f, 0.5f);

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
            GUI.Label(new Rect(screen.x - 160, Screen.height - screen.y - 30, 320, 24), feedbackText, feedbackStyle);
        }
    }
}
