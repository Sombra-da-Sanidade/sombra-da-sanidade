using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Porta de saída. Com terminal (Fuga da Eclusa, GDD 5 e 7, #44): é a porta externa e só destrava depois
    // que os HDs entram no EclusaTerminal. Sem terminal (porta da ala, protótipo): abre com os HDs da cena.
    // Encostar no trigger destravado vence a partida (GDD 4.4).
    [RequireComponent(typeof(Collider2D))]
    public class ExitDoor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Color lockedColor = new Color(0.7f, 0.15f, 0.15f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.3f);
        [Tooltip("Terminal da eclusa. Vazio: a porta abre só com os HDs (comportamento antigo).")]
        [SerializeField] private EclusaTerminal terminal;

        public EclusaTerminal Terminal => terminal;

        public bool IsUnlocked
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || !gm.HasAllDrives) return false;
                return terminal == null || terminal.Activated;
            }
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private int playersInside;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            bool unlocked = IsUnlocked;
            if (doorRenderer != null) doorRenderer.color = unlocked ? unlockedColor : lockedColor;
            // Conferido aqui e não no OnTriggerStay2D: parado na porta, o Rigidbody2D dorme e o Stay para de vir.
            // Assim, se o jogador já estiver na porta quando ela destravar, vence do mesmo jeito.
            if (unlocked && playersInside > 0) gm.TriggerVictory();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) playersInside++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player")) playersInside = Mathf.Max(0, playersInside - 1);
        }
    }
}
