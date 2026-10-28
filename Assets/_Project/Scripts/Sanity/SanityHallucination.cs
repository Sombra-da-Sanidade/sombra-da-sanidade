using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SombraDaSanidade.Sanity
{
    // Alucinações sensoriais (GDD 4.6): abaixo de 35 de sanidade, a cada 15 a 25 s surge uma silhueta
    // parecida com o Espécime Vazio perto do jogador, avança um pouco e some. O feixe da lanterna a desfaz.
    // O jogador não sabe se é o monstro real. Frente 3 (Sanidade e HUD). Issue #9.
    public class SanityHallucination : MonoBehaviour
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        [Tooltip("Silhueta falsa. Usar material Unlit para ser vista no escuro, como o monstro de perto.")]
        [SerializeField] private SpriteRenderer phantom;

        [Tooltip("Paredes: a silhueta não nasce dentro delas nem atrás delas.")]
        [SerializeField] private LayerMask obstacles;

        [Header("Aparição")]
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 6f;
        [SerializeField] private float fadeTime = 0.4f;
        [SerializeField] private float visibleTime = 1.2f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.7f;
        [Tooltip("Velocidade com que a silhueta avança na direção do jogador.")]
        [SerializeField] private float approachSpeed = 1f;

        public bool IsShowing { get; private set; }

        // Posição da aparição. Para som ou outros efeitos.
        public event Action<Vector2> Appeared;

        private FlashlightController flashlight;
        private Color phantomColor;
        private float nextTimer = -1f;
        private float elapsed;

        private float TotalTime => fadeTime * 2f + visibleTime;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity == null || phantom == null)
            {
                Debug.LogError($"{name}: SanityHallucination sem SanityController ou silhueta.", this);
                enabled = false;
                return;
            }

            flashlight = sanity.GetComponent<FlashlightController>();
            phantomColor = phantom.color;
            phantom.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (IsShowing) UpdatePhantom();

            var config = sanity.Config;
            if (sanity.Value >= config.hallucinationsBelow || sanity.State == SanityState.Dead)
            {
                nextTimer = -1f; // desarma: ao cair de novo, espera um intervalo inteiro
                return;
            }

            if (nextTimer < 0f) nextTimer = NextInterval(config);
            nextTimer -= Time.deltaTime;
            if (nextTimer > 0f || IsShowing) return;

            nextTimer = NextInterval(config);
            TrySpawn();
        }

        private void TrySpawn()
        {
            Vector2 player = sanity.transform.position;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 point = player + Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
                if (Physics2D.OverlapCircle(point, 0.5f, obstacles)) continue;
                if (Physics2D.Linecast(player, point, obstacles)) continue;

                phantom.transform.position = point;
                phantom.gameObject.SetActive(true);
                elapsed = 0f;
                IsShowing = true;
                SetAlpha(0f);
                Appeared?.Invoke(point);
                return;
            }
        }

        private void UpdatePhantom()
        {
            elapsed += Time.deltaTime;

            // Luz desfaz a ilusão: pula direto para o fade-out.
            if (flashlight != null && flashlight.IsPointInCone(phantom.transform.position))
                elapsed = Mathf.Max(elapsed, TotalTime - fadeTime);

            if (elapsed >= TotalTime)
            {
                IsShowing = false;
                phantom.gameObject.SetActive(false);
                return;
            }

            float fadeIn = Mathf.Clamp01(elapsed / fadeTime);
            float fadeOut = Mathf.Clamp01((TotalTime - elapsed) / fadeTime);
            SetAlpha(Mathf.Min(fadeIn, fadeOut) * maxAlpha);

            phantom.transform.position = Vector2.MoveTowards(
                phantom.transform.position, sanity.transform.position, approachSpeed * Time.deltaTime);
        }

        private void SetAlpha(float a)
        {
            phantom.color = new Color(phantomColor.r, phantomColor.g, phantomColor.b, phantomColor.a * a);
        }

        private static float NextInterval(SanityConfig config) =>
            Random.Range(config.hallucinationMinInterval, config.hallucinationMaxInterval);
    }
}
