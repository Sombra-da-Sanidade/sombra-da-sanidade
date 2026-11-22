using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Tempos da transição CRT entre cenas (docs/planejamento-rc.md, seção 2; issue #94). Frente 5.
    // Fica em Resources/CrtTransitionConfig.asset para valer em todas as cenas sem editar nenhuma, como o RankConfig.
    [CreateAssetMenu(fileName = "CrtTransitionConfig", menuName = "Sombra da Sanidade/Config/Transição CRT")]
    public class CrtTransitionConfig : ScriptableObject
    {
        public const string ResourcePath = "CrtTransitionConfig";

        [Header("Troca de cena")]
        [Tooltip("Segundos para a tela fechar numa linha horizontal e apagar, antes de carregar a cena.")]
        [Min(0.01f)] public float closeSeconds = 0.35f;
        [Tooltip("Segundos para a linha abrir de novo na cena nova.")]
        [Min(0.01f)] public float openSeconds = 0.45f;
        [Tooltip("Segundos no preto na cena nova antes de abrir. Esconde a câmera se ajeitando no spawn.")]
        [Min(0f)] public float loadHoldSeconds = 0.2f;

        [Header("Game Over")]
        [Tooltip("Segundos com a tela apagada no Game Over antes de abrir sobre a tela de fim.")]
        [Min(0f)] public float gameOverHoldSeconds = 0.15f;

        [Header("Forma")]
        [Tooltip("Parte da animação em que a tela achata até virar linha. O resto é a linha encolhendo até o ponto.")]
        [Range(0.1f, 0.9f)] public float collapseShare = 0.6f;
        [Tooltip("Ao abrir o jogo (primeira cena), a tela abre da linha de fósforo.")]
        public bool openOnBoot = true;

        private static CrtTransitionConfig cached;

        // Asset de Resources ou, sem ele, os valores padrão acima.
        public static CrtTransitionConfig Load()
        {
            if (cached == null) cached = Resources.Load<CrtTransitionConfig>(ResourcePath);
            if (cached == null) cached = CreateInstance<CrtTransitionConfig>();
            return cached;
        }
    }
}
