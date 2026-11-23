using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Sanity
{
    // Troca o sprite de um objeto do cenário quando a sanidade cai abaixo do limiar (docs/narrativa/roteiro-rc.md seção 4, #89).
    // Cobre os objetos reativos: espelho, foto da Vance, pulseira "08" e prontuário na maca.
    public class SanitySpriteSwap : MonoBehaviour
    {
        [SerializeField] private SanityController sanity;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite shakenSprite;

        [Tooltip("Sanidade abaixo da qual o objeto revela a versão abalada.")]
        [SerializeField] private float sanityBelow = 35f;

        [Tooltip("Opcional: após tocar esta cutscene (ex.: M3), troca para afterCutsceneSprite (ou esconde se vazio).")]
        [SerializeField] private string afterCutsceneId = "";
        [SerializeField] private Sprite afterCutsceneSprite;

        public void Setup(Sprite normal, Sprite shaken, float threshold = 35f, string cutsceneId = "", Sprite postCutscene = null)
        {
            normalSprite = normal;
            shakenSprite = shaken;
            sanityBelow = threshold;
            afterCutsceneId = cutsceneId;
            afterCutsceneSprite = postCutscene;
            Refresh();
        }

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (target != null && normalSprite == null) normalSprite = target.sprite;
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            if (target == null) return;

            if (!string.IsNullOrEmpty(afterCutsceneId) && CutscenePlayer.HasPlayed(afterCutsceneId))
            {
                if (afterCutsceneSprite != null)
                {
                    target.enabled = true;
                    target.sprite = afterCutsceneSprite;
                }
                else
                {
                    target.enabled = false;
                }
                return;
            }

            bool shaken = sanity != null && (sanity.Value < sanityBelow || sanity.IsRevealForced);
            var desired = shaken && shakenSprite != null ? shakenSprite : normalSprite;
            if (desired != null && target.sprite != desired)
                target.sprite = desired;
        }
    }
}
