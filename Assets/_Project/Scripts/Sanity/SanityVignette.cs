using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Sanity
{
    // Vinheta que fecha a tela e aberração cromática que distorce as cores conforme a sanidade cai
    // (GDD 4.2, 7 e 10). Frente 3 (Sanidade e HUD). Issue #9.
    // Cria o próprio Volume global em runtime: não depende de profile salvo nem de editar a cena.
    public class SanityVignette : MonoBehaviour
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        [Header("Intensidade")]
        [Tooltip("Vinheta com sanidade cheia.")]
        [SerializeField, Range(0f, 1f)] private float calmIntensity = 0.2f;
        [Tooltip("Vinheta com sanidade zerada.")]
        [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.65f;
        [Tooltip("Velocidade com que a vinheta acompanha a sanidade.")]
        [SerializeField] private float followSpeed = 1.5f;

        [Header("Cor")]
        [SerializeField] private Color calmColor = Color.black;
        [Tooltip("Cor perto do colapso.")]
        [SerializeField] private Color panicColor = new Color(0.35f, 0f, 0.02f);

        [Header("Aberração cromática")]
        [Tooltip("Distorção com sanidade zerada. Começa a aparecer no estado Ansioso.")]
        [SerializeField, Range(0f, 1f)] private float maxAberration = 0.8f;

        [Header("Pulso (Alucinando e Colapso)")]
        [SerializeField] private float pulseAmount = 0.06f;
        [SerializeField] private float pulseSpeed = 2.5f;

        private Volume volume;
        private VolumeProfile profile;
        private Vignette vignette;
        private ChromaticAberration aberration;
        private float shownRatio = 1f;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity == null || sanity.Config == null)
            {
                Debug.LogError($"{name}: SanityVignette sem SanityController (ou sem SanityConfig) na cena.", this);
                enabled = false;
                return;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vignette = profile.Add<Vignette>(true);
            vignette.smoothness.Override(0.45f);
            vignette.rounded.Override(false);
            aberration = profile.Add<ChromaticAberration>(true);

            volume = GetComponent<Volume>();
            if (volume == null) volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;

            EnablePostProcessing();
            Apply(1f);
        }

        private void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }

        private void Update()
        {
            shownRatio = Mathf.MoveTowards(shownRatio, sanity.Ratio, followSpeed * Time.deltaTime);
            Apply(shownRatio);
        }

        private void Apply(float ratio)
        {
            float danger = 1f - ratio;
            float intensity = Mathf.Lerp(calmIntensity, maxIntensity, danger);

            var config = sanity.Config;
            float distortion = Mathf.InverseLerp(config.anxiousBelow, 0f, ratio * config.maxSanity) * maxAberration;

            // Pulso lembra batimento: mais rápido no colapso.
            var state = sanity.State;
            if (state == SanityState.Hallucinating || state == SanityState.Collapsed)
            {
                float speed = state == SanityState.Collapsed ? pulseSpeed * 1.8f : pulseSpeed;
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * speed));
                intensity += pulse * pulseAmount;
                distortion += pulse * pulseAmount * 2f;
            }

            vignette.intensity.Override(Mathf.Clamp01(intensity));
            aberration.intensity.Override(Mathf.Clamp01(distortion));
            vignette.color.Override(Color.Lerp(calmColor, panicColor, Mathf.InverseLerp(0.5f, 0.1f, ratio)));
        }

        // A câmera gerada pelo menu vem com pós-processamento desligado.
        private static void EnablePostProcessing()
        {
            var cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData data))
                data.renderPostProcessing = true;
        }
    }
}
