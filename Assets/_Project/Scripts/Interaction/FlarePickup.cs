using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Interaction
{
    // Caixa de ferramentas em cima de uma bancada: segurar E abre e pega o sinalizador que está dentro
    // (soma no FlareThrower). A caixa fica aberta e vazia. Sem a arte da bancada (art vazio), funciona como
    // um sinalizador solto no chão: some ao pegar.
    public class FlarePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private int amount = 1;
        [Tooltip("Jogador. Vazio: procura um na cena.")]
        [SerializeField] private FlareThrower thrower;

        [Header("Bancada com caixa de ferramentas (vazio: sinalizador solto)")]
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite openedSprite;
        [SerializeField] private float openDuration = 0.6f;

        public string Prompt => taken ? "Caixa vazia"
            : thrower != null && !thrower.CanAdd ? "Sinalizadores cheios"
            : art != null ? "Abrir caixa de ferramentas" : "Pegar sinalizador";
        public bool CanInteract => !taken && thrower != null && thrower.CanAdd;
        public float HoldDuration => art != null ? openDuration : 0f;
        public bool Taken => taken;
        public bool IsToolbox => art != null;

        private bool taken;

        private void Start()
        {
            if (thrower == null) thrower = FindAnyObjectByType<FlareThrower>();
        }

        public void BeginInteract() { }
        public void TickInteract(float progress) { }
        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            taken = true;
            thrower.Add(amount);
            foreach (var l in GetComponentsInChildren<Light2D>()) l.enabled = false;
            if (art != null)
            {
                if (openedSprite != null) art.sprite = openedSprite;
                return;
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        }
    }
}
