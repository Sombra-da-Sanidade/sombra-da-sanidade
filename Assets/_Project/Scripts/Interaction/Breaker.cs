using System;
using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Interaction
{
    // Disjuntor / quadro elétrico (GDD 4.2, 4.3 e regra 6). Frente 4 (Disjuntor e mapa). Issue #10.
    // Segurar E por 4s acende a lâmpada de teto (Zona Segura) por 60s; depois dá para reparar de novo.
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

        public string Prompt => "Reparar disjuntor";
        public bool CanInteract => !isActive;
        public float HoldDuration => repairDuration;
        public bool IsActive => isActive;
        public SafeZone Zone => safeZone;

        // true ao acender a Zona Segura, false ao apagar. Bom gancho para som e HUD.
        public event Action<bool> StateChanged;

        private bool isActive;
        private float remaining;
        private bool repairing;

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
        }

        private void Update()
        {
            if (repairing && sparks != null)
            {
                // Faísca: piscadas curtas e irregulares.
                sparks.enabled = true;
                sparks.intensity = UnityEngine.Random.value < 0.35f ? UnityEngine.Random.Range(0.8f, 1.6f) : 0f;
            }
            if (!isActive) return;
            remaining -= Time.deltaTime;
            safeZone.SetWarning(remaining <= warningTime);
            if (remaining <= 0f) Deactivate();
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
            NoiseBus.Emit(transform.position, activationNoiseRadius);
            StateChanged?.Invoke(true);
        }

        private void Deactivate()
        {
            isActive = false;
            safeZone.TurnOff();
            if (statusLed != null) statusLed.color = brokenColor;
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
