using System;
using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    public enum LootKind { Nothing, Battery, Sedative, AdrenaSyn }

    // Armário da base (GDD 6, Economia): segurar E abre, faz barulho e pode ter bateria sobressalente (+30)
    // ou sedativo (mesmo efeito do Estabilizador de Lítio), e Adrena-Syn (#34). Abre uma vez só. Frente 1. Issue #29.
    public class Locker : MonoBehaviour, IInteractable
    {
        [SerializeField] private InteractionConfig config;
        [Tooltip("Sprite que escurece depois de saqueado. Vazio: usa o SpriteRenderer do próprio objeto.")]
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Fração da cor original depois de aberto (marca visual de armário já saqueado).")]
        [SerializeField, Range(0f, 1f)] private float openedTint = 0.45f;
        [Tooltip("Sprite do armário aberto (Art/Props). Vazio: só escurece o sprite atual.")]
        [SerializeField] private Sprite openedSprite;
        [Tooltip("Tabela de loot da ala (extra #46). Vazio: usa as chances fixas do InteractionConfig (GDD 6).")]
        [SerializeField] private LootTable table;

        public string Prompt => "Abrir armário";
        public bool CanInteract => !opened && config != null;
        public float HoldDuration => config.lockerOpenDuration;

        public bool IsOpened => opened;
        public LootKind Loot { get; private set; }
        public LootTable Table => table;

        // Disparado ao abrir, com o que saiu: gancho para som, HUD e o ajuste dinâmico de bateria.
        public event Action<Locker, LootKind> Opened;

        private bool opened;
        private FlashlightController flashlight;
        private SanityController sanity;
        private BatteryPity pity;
        private float feedbackUntil;
        private string feedbackText;
        private GUIStyle feedbackStyle;

        private const float FeedbackDuration = 1.8f;

        // Armários por tabela, para a garantia de uma bateria por ala.
        private static readonly Dictionary<LootTable, HashSet<Locker>> byTable = new Dictionary<LootTable, HashSet<Locker>>();
        private LootTable registeredTable;

        private void OnEnable() => Register();

        private void OnDisable() => Unregister();

        private void Register()
        {
            if (table == null) return;
            if (!byTable.TryGetValue(table, out var set)) byTable[table] = set = new HashSet<Locker>();
            set.Add(this);
            registeredTable = table;
        }

        private void Unregister()
        {
            if (registeredTable != null && byTable.TryGetValue(registeredTable, out var set))
            {
                set.Remove(this);
                if (set.Count == 0) byTable.Remove(registeredTable);
            }
            registeredTable = null;
        }

        // Troca a tabela em tempo de execução (perfil de dificuldade, testes).
        public void SetTable(LootTable value)
        {
            Unregister();
            table = value;
            if (isActiveAndEnabled) Register();
        }

        // Último armário fechado da ala sem nenhuma bateria ter saído: precisa dar bateria.
        private bool MustGiveBattery()
        {
            if (table == null || !table.guaranteeBattery || !byTable.TryGetValue(table, out var set)) return false;
            foreach (var other in set)
            {
                if (other == this) continue;
                if (!other.opened || other.Loot == LootKind.Battery) return false;
            }
            return true;
        }

        private void Awake()
        {
            if (config == null) Debug.LogError($"{name}: Locker sem InteractionConfig.", this);
            if (body == null) body = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            flashlight = FindAnyObjectByType<FlashlightController>();
            sanity = FindAnyObjectByType<SanityController>();
            pity = BatteryPity.On(flashlight, config);
        }

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            Open(RollLoot());
        }

        // Com tabela da ala (#46): sorteia pelos pesos dela e garante uma bateria por ala.
        // Sem tabela, sorteio do GDD 6: lootChance de ter algo; dentro disso, batteryShare de ser bateria,
        // adrenaSynShare de ser Adrena-Syn e o resto sedativo. O ajuste dinâmico (#47) soma pontos
        // à chance de bateria, tirados da chance de vir vazio.
        public LootKind RollLoot()
        {
            float bonus = pity != null ? pity.BonusChance : 0f;
            if (table != null)
            {
                if (MustGiveBattery()) return LootKind.Battery;
                return Pick(table.Chance(LootKind.Battery) + bonus, table.Chance(LootKind.Sedative), table.Chance(LootKind.AdrenaSyn));
            }

            float battery = config.lockerLootChance * config.lockerBatteryShare;
            float adrena = config.lockerLootChance * Mathf.Min(config.lockerAdrenaSynShare, 1f - config.lockerBatteryShare);
            float sedative = config.lockerLootChance - battery - adrena;
            return Pick(battery + bonus, sedative, adrena);
        }

        // Escolhe pelas chances absolutas. Se passarem de 1, os itens que não são bateria encolhem.
        private static LootKind Pick(float battery, float sedative, float adrena)
        {
            battery = Mathf.Clamp01(battery);
            float others = sedative + adrena;
            if (battery + others > 1f && others > 0f)
            {
                float scale = (1f - battery) / others;
                sedative *= scale;
                adrena *= scale;
            }
            float r = UnityEngine.Random.value;
            if (r < battery) return LootKind.Battery;
            if (r < battery + sedative) return LootKind.Sedative;
            if (r < battery + sedative + adrena) return LootKind.AdrenaSyn;
            return LootKind.Nothing;
        }

        // Separado do sorteio para os testes escolherem o conteúdo.
        public void Open(LootKind loot)
        {
            if (opened) return;
            opened = true;
            Loot = loot;

            if (body != null && openedSprite != null) body.sprite = openedSprite;
            else if (body != null) body.color = new Color(body.color.r * openedTint, body.color.g * openedTint, body.color.b * openedTint, body.color.a);
            // Porta de metal: o monstro pode ouvir (GDD 4.3, interações fazem barulho).
            NoiseBus.Emit(transform.position, config.lockerNoiseRadius);

            switch (loot)
            {
                case LootKind.Battery:
                    float amount = flashlight != null && flashlight.Config != null ? flashlight.Config.batteryPickupAmount : 0f;
                    if (flashlight != null) flashlight.Recharge(amount);
                    if (pity != null) pity.NotifyBatteryFound();
                    ShowFeedback($"Bateria +{amount:0}");
                    break;
                case LootKind.Sedative:
                    // A corrotina da lentidão roda na sanidade do jogador, que continua ativa até o fim do efeito.
                    LithiumStabilizer.ApplyEffect(sanity, sanity);
                    ShowFeedback("Sedativo: +sanidade, passos lentos");
                    break;
                case LootKind.AdrenaSyn:
                    AdrenaSynModule.Apply(sanity != null ? sanity.GetComponent<PlayerController>() : null);
                    ShowFeedback("Adrena-Syn: corre, mas a mente paga");
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
