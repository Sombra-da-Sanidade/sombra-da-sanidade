using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    // Objeto que muda com a sanidade (roteiro-rc 4, #89), variação do SanityClue: em vez de aparecer, troca de
    // sprite abaixo do limite (espelho, foto, pulseira, prontuário). A troca pisca algumas vezes antes de firmar,
    // para o jogador perceber que não é o mesmo objeto. Volta ao normal quando a sanidade sobe.
    public class SanityObject : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite normal;
        [SerializeField] private Sprite shaken;
        [Tooltip("Abaixo desta sanidade o objeto mostra a versão abalada (roteiro-rc 4: 35, o mesmo das pistas).")]
        [SerializeField, Range(1f, 100f)] private float threshold = 35f;
        [Tooltip("Segundos piscando entre as duas versões na troca.")]
        [SerializeField, Min(0f)] private float flickerSeconds = 0.6f;

        private SanityController sanity;
        private bool isShaken;
        private float flickerUntil;

        public bool IsShaken => isShaken;
        public float Threshold => threshold;

        public void Setup(SpriteRenderer renderer, Sprite normalSprite, Sprite shakenSprite)
        {
            art = renderer;
            normal = normalSprite;
            shaken = shakenSprite;
        }

        private void Start()
        {
            sanity = FindAnyObjectByType<SanityController>();
            if (art != null) art.sprite = normal;
        }

        private void Update()
        {
            if (art == null || sanity == null) return;
            bool want = sanity.Value < threshold || sanity.IsRevealForced;
            if (want != isShaken)
            {
                isShaken = want;
                flickerUntil = Time.time + flickerSeconds;
            }
            bool flicker = Time.time < flickerUntil && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
            art.sprite = isShaken != flicker ? shaken : normal;
        }
    }
}
