using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.UI
{
    // Linguagem das memórias (#88, planejamento-rc 4): o passado é verde fósforo estourado, com linhas de TV e
    // quadros repetidos como fita gasta. O presente é frio. O jogador percebe sozinho quando está no passado.
    // - Volume próprio em runtime (como o SanityVignette): dessatura, puxa para o verde e estoura a exposição.
    //   A paleta de 4 cores exata fica nos painéis de memória (ADR da #99); no motor o filtro só aproxima.
    // - Linhas de TV e faixa de rolagem em IMGUI por cima de tudo.
    // - Quadros repetidos: o CutscenePlayer congela o painel em saltos (Stutter).
    // - Áudio abafado: a cutscene troca a faixa pela versão _memory (o WebGL não aplica filtro, ADR 0005).
    // Ligar com MemoryEffect.Active = true; a transição leva fadeSeconds.
    public class MemoryEffect : MonoBehaviour
    {
        public static MemoryEffect Instance { get; private set; }

        // Liga e desliga o efeito; cria o objeto na primeira vez.
        public static bool Active
        {
            get => Instance != null && Instance.target > 0f;
            set
            {
                if (value) Ensure().target = 1f;
                else if (Instance != null) Instance.target = 0f;
            }
        }

        public static float Amount => Instance != null ? Instance.amount : 0f;

        [SerializeField] private float fadeSeconds = 0.35f;
        [SerializeField] private Color tint = new Color(0.72f, 1f, 0.62f);
        [SerializeField] private float exposure = 0.8f;
        [SerializeField, Range(0f, 1f)] private float scanlineAlpha = 0.22f;

        private Volume volume;
        private VolumeProfile profile;
        private ColorAdjustments color;
        private float target, amount;
        private static Texture2D white;

        public static MemoryEffect Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("MemoryEffect");
            return go.AddComponent<MemoryEffect>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = profile.Add<ColorAdjustments>(true);
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;   // acima do Volume da sanidade
            volume.weight = 0f;
            volume.sharedProfile = profile;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (profile != null) Destroy(profile);
        }

        private void Update()
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
            amount = Mathf.MoveTowards(amount, target, step);
            volume.weight = amount;
            color.saturation.Override(-100f);
            color.contrast.Override(35f);
            color.postExposure.Override(exposure);
            color.colorFilter.Override(tint);
        }

        private void OnGUI()
        {
            if (amount <= 0.01f) return;
            if (white == null) white = Texture2D.whiteTexture;
            GUI.depth = -100;
            int px = Mathf.Max(2, Mathf.FloorToInt(Screen.height / 180f));
            float a = scanlineAlpha * amount;
            GUI.color = new Color(0.02f, 0.08f, 0.05f, a);
            for (int y = 0; y < Screen.height; y += px * 2)
                GUI.DrawTexture(new Rect(0, y, Screen.width, px), white);
            // Faixa de rolagem da fita, descendo devagar.
            float band = Mathf.Repeat(Time.unscaledTime * 0.25f, 1.2f) - 0.1f;
            GUI.color = new Color(0.66f, 0.95f, 0.58f, 0.08f * amount);
            GUI.DrawTexture(new Rect(0, band * Screen.height, Screen.width, px * 6), white);
            GUI.color = Color.white;
        }
    }
}
