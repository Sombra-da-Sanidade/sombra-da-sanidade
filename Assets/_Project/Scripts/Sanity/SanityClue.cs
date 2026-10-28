using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    // Pista escrita em fósforo que só aparece com a mente abalada (GDD 1 "Revelação pela Loucura" e 4.4 regra 5).
    // Colocar em qualquer objeto com SpriteRenderer ou TextMesh (filhos incluídos). Frente 3 (Sanidade e HUD).
    public class SanityClue : MonoBehaviour
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        [Tooltip("Velocidade com que a pista surge e some.")]
        [SerializeField] private float fadeSpeed = 1.2f;

        [Tooltip("Tremulação do brilho, para parecer que a mente está inventando o texto.")]
        [SerializeField, Range(0f, 1f)] private float flicker = 0.25f;

        public bool IsRevealed => alpha >= 0.5f;

        private SpriteRenderer[] sprites;
        private TextMesh[] texts;
        private Color[] spriteColors;
        private Color[] textColors;
        private float alpha;
        private float noiseSeed;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity == null)
            {
                Debug.LogError($"{name}: SanityClue sem SanityController na cena.", this);
                enabled = false;
                return;
            }

            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            texts = GetComponentsInChildren<TextMesh>(true);
            spriteColors = new Color[sprites.Length];
            textColors = new Color[texts.Length];
            for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
            for (int i = 0; i < texts.Length; i++) textColors[i] = texts[i].color;

            noiseSeed = Random.value * 100f;
            Apply();
        }

        private void Update()
        {
            float target = sanity.Value < sanity.Config.revealCluesBelow ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            float glow = alpha * (1f - flicker * Mathf.PerlinNoise(noiseSeed, Time.time * 3f));
            bool visible = alpha > 0f;

            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].enabled = visible;
                sprites[i].color = WithAlpha(spriteColors[i], glow);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                var r = texts[i].GetComponent<MeshRenderer>();
                if (r != null) r.enabled = visible;
                texts[i].color = WithAlpha(textColors[i], glow);
            }
        }

        private static Color WithAlpha(Color c, float factor) => new Color(c.r, c.g, c.b, c.a * factor);
    }
}
