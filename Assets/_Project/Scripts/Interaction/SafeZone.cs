using System;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Interaction
{
    // Zona Segura (GDD 4.2, 4.3 e regra 6): lâmpada de teto acesa depois do reparo de um disjuntor.
    // Dentro dela a sanidade regenera (SanityController.SetInSafeZone) e o monstro não entra (MonsterRepellent).
    // Frente 4. Quem liga e desliga é o Breaker; a zona só cuida da luz, do trigger e do bloqueio.
    [RequireComponent(typeof(CircleCollider2D))]
    public class SafeZone : MonoBehaviour
    {
        [Tooltip("Raio da área em unidades. O alcance da luz acompanha.")]
        [SerializeField] private float radius = 3.5f;

        [Header("Lâmpada de teto (vazia: cria uma)")]
        [SerializeField] private Light2D lamp;
        // Âmbar: luz é segurança (guia de estilo, pilares visuais).
        [SerializeField] private Color lampColor = new Color(1f, 0.85f, 0.55f);
        [SerializeField] private float lampIntensity = 1.1f;
        [Tooltip("Duração da piscada de partida, como lâmpada fluorescente.")]
        [SerializeField] private float startupFlicker = 0.6f;

        public bool IsOn { get; private set; }
        public bool PlayerInside => playerSanity != null;
        public float Radius
        {
            get => radius;
            set { radius = Mathf.Max(0.1f, value); Apply(); }
        }

        public event Action<bool> Toggled;

        private CircleCollider2D area;
        private MonsterRepellent repellent;
        private SanityController playerSanity;
        private float onTime;
        private bool warning;

        private void Awake()
        {
            area = GetComponent<CircleCollider2D>();
            area.isTrigger = true;
            area.enabled = false;

            repellent = GetComponent<MonsterRepellent>();
            if (repellent == null) repellent = gameObject.AddComponent<MonsterRepellent>();
            repellent.enabled = false;

            if (lamp == null) lamp = CreateLamp();
            lamp.enabled = false;
            Apply();
        }

        // Collider, área do monstro e alcance da luz seguem o mesmo raio.
        private void Apply()
        {
            if (area == null) return;
            area.radius = radius;
            repellent.Radius = radius;
            lamp.pointLightInnerRadius = radius * 0.35f;
            lamp.pointLightOuterRadius = radius + 0.5f;
        }

        public void TurnOn()
        {
            if (IsOn) return;
            IsOn = true;
            warning = false;
            onTime = Time.time;
            repellent.enabled = true;
            lamp.enabled = true;
            // Ligar o collider dispara OnTriggerEnter se o jogador já estiver embaixo da lâmpada.
            area.enabled = true;
            Toggled?.Invoke(true);
        }

        public void TurnOff()
        {
            if (!IsOn) return;
            IsOn = false;
            if (playerSanity != null) playerSanity.SetInSafeZone(false);
            playerSanity = null;
            area.enabled = false;
            repellent.enabled = false;
            lamp.enabled = false;
            Toggled?.Invoke(false);
        }

        // Últimos segundos: a lâmpada falha para avisar que a proteção vai acabar (timer nunca aparece em número, GDD 8).
        public void SetWarning(bool on) => warning = on;

        private void Update()
        {
            if (!IsOn) return;
            float k = 1f;
            float t = Time.time - onTime;
            if (t < startupFlicker)
                k = UnityEngine.Random.value < 0.55f ? 1f : 0.15f;
            else if (warning)
                k = Mathf.PerlinNoise(Time.time * 9f, 0.3f) < 0.35f ? 0.2f : 1f;
            lamp.intensity = lampIntensity * k;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOn || playerSanity != null || !other.CompareTag("Player")) return;
            var sanity = other.GetComponentInParent<SanityController>();
            if (sanity == null) return;
            playerSanity = sanity;
            sanity.SetInSafeZone(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (playerSanity == null || !other.CompareTag("Player")) return;
            playerSanity.SetInSafeZone(false);
            playerSanity = null;
        }

        private Light2D CreateLamp()
        {
            var go = new GameObject("CeilingLamp");
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = lampColor;
            light.intensity = lampIntensity;
            light.falloffIntensity = 0.6f;
            light.shadowsEnabled = true;
            light.shadowIntensity = 0.9f;
            return light;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            const int segments = 32;
            Vector3 prev = transform.position + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
