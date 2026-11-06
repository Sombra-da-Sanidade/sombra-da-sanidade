using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Números do HUD diegético (GDD 8). Frente 3. Nenhum está no GDD: ajustar no playtest.
    [CreateAssetMenu(fileName = "HudConfig", menuName = "Sombra da Sanidade/Config/HUD")]
    public class HudConfig : ScriptableObject
    {
        [Header("Relógio (segurar Tab)")]
        [Tooltip("Velocidade do Thomas enquanto olha o relógio. Consultar custa tempo.")]
        [Range(0.1f, 1f)] public float watchSlowMultiplier = 0.55f;
        [Tooltip("Segundos para levantar ou baixar o pulso.")]
        public float watchRaiseSeconds = 0.18f;
        [Tooltip("Segundos que o traço do ECG leva para cruzar a tela do relógio.")]
        public float ecgSweepSeconds = 2.4f;

        [Header("Bateria ao lado do Thomas")]
        [Tooltip("Segundos visível depois de mudar de segmento, ligar/desligar ou recarregar.")]
        public float batteryShowSeconds = 2.5f;
        [Tooltip("Abaixo desta fração a bateria fica sempre visível, piscando em vermelho.")]
        [Range(0f, 1f)] public float batteryLowRatio = 0.25f;

        [Tooltip("Segundos que o raio cortado pisca ao lado da pilha depois de um clique direito sem EMP.")]
        public float empFailShowSeconds = 1.5f;

        [Header("Efeitos no relógio")]
        [Tooltip("Nos últimos segundos do Adrena-Syn o ícone pisca no relógio.")]
        public float effectEndingSeconds = 3f;

        [Header("HDs")]
        [Tooltip("Segundos visível depois de coletar um HD.")]
        public float driveShowSeconds = 4f;

        [Header("Dicas de controle (uma vez por sessão)")]
        [Tooltip("Segundos que cada dica fica na tela se o jogador não fizer a ação.")]
        public float hintSeconds = 6f;
        [Tooltip("Segundos de jogo até a dica da lanterna aparecer, se o jogador ainda não usou F.")]
        public float flashlightHintDelay = 1.5f;

        [Header("Legendas")]
        [Tooltip("Segundos de fade de entrada e saída de cada legenda.")]
        public float subtitleFadeSeconds = 0.25f;

        [Header("Depuração (issue #16)")]
        [Tooltip("Mostra a barra MENTE. O GDD 8 pede sanidade só por efeitos; F3 alterna no editor e em builds de desenvolvimento.")]
        public bool showSanityDebugBar;
    }
}
