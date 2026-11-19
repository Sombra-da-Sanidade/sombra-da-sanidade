using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Animação em loop simples para props (bolhas do tanque, ondas do EEG). Começa num quadro sorteado
    // para objetos iguais não piscarem juntos.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 4f;

        private SpriteRenderer sr;
        private float offset;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            offset = Random.value * 10f;
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            sr.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
        }
    }
}
