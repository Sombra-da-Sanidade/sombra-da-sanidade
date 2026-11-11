using System;
using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace SombraDaSanidade.Interaction
{
    // Console da subestação (Ala de Energia, GDD 7: "religar subestação; HD-02". Issue #42).
    // Segurar E por 6 s religa a energia do servidor do HD-02. O reparo é barulhento: um estalo forte no começo e
    // descargas a cada 1,5 s chamam o monstro, que costuma chegar antes do fim. O jogador precisa dar o EMP na
    // hora certa (stun de 4 s) para terminar. Soltar ou se mexer zera o progresso. Depois de ligada, fica ligada.
    public class Substation : MonoBehaviour, IInteractable
    {
        [Header("Reparo")]
        [SerializeField] private float repairDuration = 6f;

        [Header("Ruído (ajustar no playtest para a ala durar 5-7 min)")]
        [Tooltip("Estalo ao encostar no console.")]
        [SerializeField] private float startNoiseRadius = 10f;
        [Tooltip("Descargas durante o reparo.")]
        [SerializeField] private float pulseNoiseRadius = 10f;
        [SerializeField] private float pulseInterval = 1.5f;
        [Tooltip("A subestação volta com um zumbido alto.")]
        [SerializeField] private float poweredNoiseRadius = 12f;

        [Header("Visual (vazios: cria a luz; sem tilemap, só a luz muda)")]
        [SerializeField] private Light2D statusLight;
        [SerializeField] private Light2D sparks;
        [SerializeField] private Tilemap wallTiles;
        [SerializeField] private Vector3Int tileCell;
        [SerializeField] private TileBase poweredTile;
        [SerializeField] private Color offColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color onColor = new Color(0.36f, 0.7f, 1f);   // azul = eletricidade (guia de estilo)

        public string Prompt => "Religar subestação";
        public bool CanInteract => !IsPowered;
        public float HoldDuration => repairDuration;
        public bool IsPowered { get; private set; }
        public bool IsRepairing { get; private set; }

        public event Action Powered;

        private float nextPulse;

        private void Awake()
        {
            if (statusLight == null) statusLight = CreateLight("StatusLight", offColor, 0.7f, 1.2f);
            if (sparks == null) sparks = CreateLight("Sparks", onColor, 0f, 2.4f);
            sparks.enabled = false;
            statusLight.color = offColor;
        }

        private void Update()
        {
            if (!IsRepairing || sparks == null) return;
            sparks.enabled = true;
            sparks.intensity = UnityEngine.Random.value < 0.4f ? UnityEngine.Random.Range(1f, 2.2f) : 0f;
        }

        public void BeginInteract()
        {
            IsRepairing = true;
            NoiseBus.Emit(transform.position, startNoiseRadius);
            nextPulse = Time.time + pulseInterval;
        }

        public void TickInteract(float progress)
        {
            if (Time.time < nextPulse) return;
            nextPulse = Time.time + pulseInterval;
            NoiseBus.Emit(transform.position, pulseNoiseRadius);
        }

        public void CancelInteract() => StopSparks();

        public void CompleteInteract()
        {
            if (IsPowered) return;
            StopSparks();
            IsPowered = true;
            statusLight.color = onColor;
            statusLight.intensity = 1.2f;
            if (wallTiles != null && poweredTile != null) wallTiles.SetTile(tileCell, poweredTile);
            NoiseBus.Emit(transform.position, poweredNoiseRadius);
            Powered?.Invoke();
        }

        private void StopSparks()
        {
            IsRepairing = false;
            if (sparks != null) sparks.enabled = false;
        }

        private Light2D CreateLight(string name, Color color, float intensity, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = (Vector2)transform.position + Vector2.up * 0.9f;
            var s = transform.lossyScale;
            go.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, s.x), 1f / Mathf.Max(0.0001f, s.y), 1f);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            return light;
        }
    }
}
