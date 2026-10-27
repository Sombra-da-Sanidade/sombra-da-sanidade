using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Eclusa de evacuação: com os 3 HDs, encostar no trigger vence a partida (GDD 4.4).
    [RequireComponent(typeof(Collider2D))]
    public class ExitDoor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Color lockedColor = new Color(0.7f, 0.15f, 0.15f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.3f);

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            if (doorRenderer == null || GameManager.Instance == null) return;
            doorRenderer.color = GameManager.Instance.HasAllDrives ? unlockedColor : lockedColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || GameManager.Instance == null) return;
            GameManager.Instance.TriggerVictory();
        }
    }
}
