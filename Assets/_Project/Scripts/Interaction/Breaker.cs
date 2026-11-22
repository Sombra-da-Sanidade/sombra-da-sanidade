using System;
using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Interaction
{
    // Disjuntor / quadro elétrico (GDD 4.2, 4.3 e regra 6). Frente 4 (Disjuntor e mapa). Issue #10.
    // Segurar E por 4s acende as arandelas da sala (RoomLamp) e a Zona Segura por 60s. No fim, o painel
    // queima (fogo, faíscas e fumaça) e não pode ser reparado de novo: cada disjuntor é uma proteção de uso único.
    // Se a cena não tiver SafeZone/LED/faísca configurados, o disjuntor cria os seus no Awake, então funciona
    // nas cenas geradas (protótipo, Alfa) sem regenerar.
    public class Breaker : MonoBehaviour, IInteractable
    {
        [Header("Tempos (GDD 4.2 e 6.4)")]
        [SerializeField] private float repairDuration = 4f;
        [SerializeField] private float lightDuration = 60f;
        [Tooltip("Nos últimos segundos a lâmpada falha para avisar que vai apagar. Não está no GDD.")]
        [SerializeField] private float warningTime = 5f;

        [Header("Ruído (GDD 4.3 e 6.4)")]
        [SerializeField] private float repairNoiseRadius = 7f;
        [Tooltip("Ruído elétrico no acionamento (GDD 6.4). Não está no GDD o raio: ajustar no playtest.")]
        [SerializeField] private float activationNoiseRadius = 9f;

        [Header("Zona Segura")]
        [SerializeField] private SafeZone safeZone;
        [Tooltip("Onde fica a lâmpada em relação ao painel. Os disjuntores ficam na parede norte: a luz cai na sala, abaixo.")]
        [SerializeField] private Vector2 zoneOffset = new Vector2(0f, -2f);
        [SerializeField] private float zoneRadius = 3.5f;

        [Header("Feedback (vazios: cria)")]
        [Tooltip("LED do painel: vermelho queimado, verde ativo (guia de estilo: cor é informação).")]
        [SerializeField] private Light2D statusLed;
        [Tooltip("Faíscas azuis durante o reparo.")]
        [SerializeField] private Light2D sparks;
        [SerializeField] private Color brokenColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color activeColor = new Color(0.35f, 1f, 0.45f);

        [Header("Arte do painel e da sala (vazios: só luz)")]
        [SerializeField] private SpriteRenderer panel;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite burntSprite;
        [Tooltip("Quadros da queima (fogo e faíscas), tocados uma vez no fim do tempo.")]
        [SerializeField] private Sprite[] burnFrames;
        [SerializeField] private SpriteRenderer smoke;
        [SerializeField] private Sprite[] smokeFrames;
        [Tooltip("Arandelas da sala: acendem com o reparo e queimam junto com o painel.")]
        [SerializeField] private RoomLamp[] lamps;

        [Header("Queima")]
        [Tooltip("Desligado: volta ao estado de antes e pode ser reparado de novo (comportamento antigo).")]
        [SerializeField] private bool burnsOut = true;
        [SerializeField] private float burnDuration = 1.6f;
        [SerializeField] private float burnNoiseRadius = 6f;

        public string Prompt => isBurnt ? "Disjuntor queimado" : "Reparar disjuntor";
        public bool CanInteract => !isActive && !isBurnt && !burning;
        public bool IsBurnt => isBurnt;
        public RoomLamp[] Lamps => lamps;
        public SpriteRenderer Panel => panel;
        public float HoldDuration => repairDuration;
        public bool IsActive => isActive;
        public SafeZone Zone => safeZone;

        // true ao acender a Zona Segura, false ao apagar. Bom gancho para som e HUD.
        public event Action<bool> StateChanged;
        // Disparado quando o painel termina de queimar.
        public event Action Burnt;

        private bool isActive;
        private float remaining;
        private bool repairing;
        private bool isBurnt;
        private bool burning;
        private float burnStart;

        private void Awake()
        {
            if (safeZone == null)
            {
                var go = CreateChild("SafeZone", zoneOffset);
                go.AddComponent<CircleCollider2D>();
                safeZone = go.AddComponent<SafeZone>();
                safeZone.Radius = zoneRadius;
            }
            if (statusLed == null) statusLed = CreateLight("StatusLed", new Vector2(0f, 0.9f), brokenColor, 0.6f, 0.7f);
            if (sparks == null) sparks = CreateLight("Sparks", new Vector2(0f, 0.9f), new Color(0.36f, 0.7f, 1f), 0f, 1.6f);
            sparks.enabled = false;
            statusLed.color = brokenColor;
            if (panel != null && offSprite != null) panel.sprite = offSprite;
            if (smoke != null) smoke.enabled = false;
        }

        private void Update()
        {
            if (repairing && sparks != null)
            {
                // Faísca: piscadas curtas e irregulares.
                sparks.enabled = true;
                sparks.intensity = UnityEngine.Random.value < 0.35f ? UnityEngine.Random.Range(0.8f, 1.6f) : 0f;
            }
            if (burning) TickBurn();
            if (isBurnt) TickSmoke();
            if (!isActive) return;
            remaining -= Time.deltaTime;
            bool warn = remaining <= warningTime;
            safeZone.SetWarning(warn);
            if (lamps != null) foreach (var l in lamps) if (l != null) l.SetWarning(warn);
            if (remaining <= 0f) Deactivate();
        }

        private void TickBurn()
        {
            float t = Time.time - burnStart;
            if (sparks != null)
            {
                sparks.enabled = true;
                sparks.color = new Color(1f, 0.55f, 0.3f);
                sparks.intensity = UnityEngine.Random.value < 0.6f ? UnityEngine.Random.Range(0.9f, 1.8f) : 0.3f;
            }
            if (panel != null && burnFrames != null && burnFrames.Length > 0)
                panel.sprite = burnFrames[Mathf.Min(burnFrames.Length - 1, (int)(t / burnDuration * burnFrames.Length * 2f) % burnFrames.Length)];
            if (t >= burnDuration) FinishBurn();
        }

        private void FinishBurn()
        {
            burning = false;
            isBurnt = true;
            if (sparks != null) { sparks.enabled = false; sparks.color = new Color(0.36f, 0.7f, 1f); }
            if (panel != null && burntSprite != null) panel.sprite = burntSprite;
            if (statusLed != null) statusLed.enabled = false;
            if (smoke != null) smoke.enabled = true;
            if (lamps != null) foreach (var l in lamps) if (l != null) l.Burn();
            Burnt?.Invoke();
        }

        private void TickSmoke()
        {
            if (smoke == null || smokeFrames == null || smokeFrames.Length == 0) return;
            smoke.sprite = smokeFrames[(int)(Time.time * 6f) % smokeFrames.Length];
        }

        public void BeginInteract()
        {
            // O ruído do reparo atrai o monstro (GDD 4.3).
            NoiseBus.Emit(transform.position, repairNoiseRadius);
            repairing = true;
        }

        public void TickInteract(float progress) { }

        public void CancelInteract() => StopSparks();

        public void CompleteInteract()
        {
            StopSparks();
            isActive = true;
            remaining = lightDuration;
            safeZone.TurnOn();
            if (statusLed != null) statusLed.color = activeColor;
            if (panel != null && onSprite != null) panel.sprite = onSprite;
            if (lamps != null)
                for (int i = 0; i < lamps.Length; i++)
                    if (lamps[i] != null) lamps[i].TurnOn(i * 0.12f);
            NoiseBus.Emit(transform.position, activationNoiseRadius);
            StateChanged?.Invoke(true);
        }

        private void Deactivate()
        {
            isActive = false;
            safeZone.TurnOff();
            if (statusLed != null) statusLed.color = brokenColor;
            if (burnsOut)
            {
                // Sobrecarga: o painel pega fogo e as arandelas estouram. Não dá para religar.
                burning = true;
                burnStart = Time.time;
                if (lamps != null) foreach (var l in lamps) if (l != null) l.TurnOff();
                NoiseBus.Emit(transform.position, burnNoiseRadius);
            }
            else
            {
                if (panel != null && offSprite != null) panel.sprite = offSprite;
                if (lamps != null) foreach (var l in lamps) if (l != null) l.TurnOff();
            }
            StateChanged?.Invoke(false);
        }

        // Apagão do alarme da câmera de segurança (#79): a sala apaga até o jogador consertar de novo.
        // Nunca queima o painel, mesmo com burnsOut, para o jogador sempre poder religar.
        public void Cut()
        {
            if (!isActive) return;
            isActive = false;
            safeZone.TurnOff();
            if (statusLed != null) statusLed.color = brokenColor;
            if (panel != null && offSprite != null) panel.sprite = offSprite;
            if (lamps != null) foreach (var l in lamps) if (l != null) l.TurnOff();
            StateChanged?.Invoke(false);
        }

        private void StopSparks()
        {
            repairing = false;
            if (sparks != null) sparks.enabled = false;
        }

        // Filho sem herdar a escala do painel (nas cenas geradas o disjuntor é um bloco 0.8x1).
        private GameObject CreateChild(string name, Vector2 offset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = (Vector2)transform.position + offset;
            var s = transform.lossyScale;
            go.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, s.x), 1f / Mathf.Max(0.0001f, s.y), 1f);
            return go;
        }

        private Light2D CreateLight(string name, Vector2 offset, Color color, float intensity, float radius)
        {
            var go = CreateChild(name, offset);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            return light;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere((Vector2)transform.position + zoneOffset, zoneRadius);
        }
    }
}
