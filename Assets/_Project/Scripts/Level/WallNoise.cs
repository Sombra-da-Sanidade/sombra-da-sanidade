using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Sons do Espécime atrás das paredes na Ala de Energia (planejamento-rc 1, fase 2): ele não está no mapa,
    // mas o jogador ouve que continua lá. Ponto fixo dentro da parede ('~' na planta) que toca um som do
    // Espécime em intervalo aleatório, só com o Thomas perto o bastante para ouvir. Não emite ruído no
    // NoiseBus: é ambiente, não atrai nada.
    public class WallNoise : MonoBehaviour
    {
        [SerializeField] private SoundEvent[] sounds = new SoundEvent[0];
        [Tooltip("Intervalo entre sons, em segundos (sorteado entre os dois).")]
        [SerializeField] private Vector2 interval = new Vector2(9f, 18f);
        [Tooltip("Só toca com o Thomas a esta distância (unidades).")]
        [SerializeField, Min(1f)] private float hearRadius = 12f;

        private Transform player;
        private float next;

        public void Setup(SoundEvent[] clips) => sounds = clips;

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            // Primeiro som mais cedo, para o jogador perceber logo na entrada.
            next = Time.time + Random.Range(interval.x * 0.3f, interval.x);
        }

        private void Update()
        {
            if (Time.time < next || sounds.Length == 0 || player == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            next = Time.time + Random.Range(interval.x, interval.y);
            if (Vector2.Distance(player.position, transform.position) > hearRadius) return;
            var sound = sounds[Random.Range(0, sounds.Length)];
            if (sound != null) AudioService.Play(sound, transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, hearRadius);
        }
    }
}
