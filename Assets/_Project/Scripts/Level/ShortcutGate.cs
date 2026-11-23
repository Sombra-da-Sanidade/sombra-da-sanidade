using System;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Atalho da Eclusa (planejamento-rc 3, #86): grade que fecha o corredor norte entre a cela da Vance e a porta
    // externa. Fechada, quem vai salvar a Vance precisa voltar pelo caminho longo; solta, a Vance abre a grade e
    // o caminho até a eclusa fica reto. Quem abre é a escolha da Vance (#82/#95): ShortcutGate.OpenAll() ou
    // Open() no portão achado pelo LevelMarker "atalho_eclusa_1".
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShortcutGate : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Color closedColor = new Color(0.55f, 0.42f, 0.12f);

        public bool IsOpen { get; private set; }
        public event Action Opened;

        public void Setup(SpriteRenderer renderer) => art = renderer;

        private void Awake()
        {
            if (art != null) art.color = closedColor;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            GetComponent<BoxCollider2D>().enabled = false;
            if (art != null) art.enabled = false;
            Opened?.Invoke();
        }

        public static void OpenAll()
        {
            foreach (var g in FindObjectsByType<ShortcutGate>(FindObjectsSortMode.None)) g.Open();
        }
    }
}
