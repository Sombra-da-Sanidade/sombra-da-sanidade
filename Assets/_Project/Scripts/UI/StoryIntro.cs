using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.UI
{
    // Cutscene de abertura em texto (GDD 9, Narrativa): páginas digitadas letra a letra em TextMeshPro com a
    // fonte VT323 (font atlas em Art/Fonts/VT323). Toca uma vez por sessão, antes do menu.
    // Espaço/Enter/clique completa a página ou avança; Esc pula tudo.
    // O Canvas, as âncoras e as referências são montados pelo MenuSceneBuilder.
    public class StoryIntro : MonoBehaviour
    {
        [Tooltip("Texto da página atual (ancorado no centro da tela).")]
        [SerializeField] private TMP_Text body;

        [Tooltip("Dica de controles (ancorada no canto inferior direito).")]
        [SerializeField] private TMP_Text hint;

        [Tooltip("Raiz do Canvas da cutscene, desligada quando ela termina.")]
        [SerializeField] private GameObject root;

        [Tooltip("Letras por segundo na digitação.")]
        [SerializeField] private float charsPerSecond = 38f;

        [TextArea(2, 5)]
        [Tooltip("Páginas da história (GDD 9). Aceita rich text do TMP.")]
        [SerializeField] private string[] pages =
        {
            "<color=#55b870>ANOS 90. COMPLEXO EREBUS, SUB-NÍVEL 4.</color>\n\nA Aethelgard Dynamics envia um auditor, Thomas Reis, para revisar os dados do Projeto Hipnos.",
            "Minutos depois da chegada, a base entra em confinamento.\n\nAs luzes caem. Os rádios ficam mudos. A equipe não responde.",
            "Nos corredores, algo anda descalço.\n\n<color=#c8922c>O Voluntário 07 não enxerga. Mas sente a luz.</color>",
            "Os registros da Dra. Valerie Vance estão em <color=#a9f294>3 HDs</color>, nos servidores das alas Médica, de Energia e Executiva.",
            "Recupere os HDs, insira no terminal da eclusa e saia antes da purga.\n\n<color=#55b870>A lanterna protege a sua mente. E mostra onde você está.</color>",
        };

        // Uma vez por sessão: voltar ao menu depois de uma partida não repete a abertura.
        private static bool playedThisSession;

        // O MainMenuScreen fica escondido e sem input enquanto a abertura toca. Inclui o quadro em que ela
        // termina, para o mesmo Espaço não apertar JOGAR no menu.
        public static bool Blocking => playing || finishedFrame == Time.frameCount;
        private static bool playing;
        private static int finishedFrame = -1;

        private int page;
        private float typed;

        public int PageCount => pages.Length;

        private void Start()
        {
            if (playedThisSession || pages.Length == 0 || body == null)
            {
                Finish();
                return;
            }
            playing = true;
            ShowPage(0);
        }

        private void OnDestroy() => playing = false;

        private void Update()
        {
            if (!playing) return;
            int total = body.textInfo.characterCount;
            typed += charsPerSecond * Time.unscaledDeltaTime;
            body.maxVisibleCharacters = Mathf.FloorToInt(typed);

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Finish(); return; }
            bool next = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                        || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!next) return;
            if (typed < total) typed = total;               // primeiro toque completa a página
            else if (page + 1 < pages.Length) ShowPage(page + 1);
            else Finish();
        }

        private void ShowPage(int index)
        {
            page = index;
            typed = 0f;
            body.text = pages[index];
            body.maxVisibleCharacters = 0;
            body.ForceMeshUpdate();
            if (hint != null)
                hint.text = $"{index + 1}/{pages.Length}   ESPAÇO: CONTINUAR   ESC: PULAR";
        }

        public void Finish()
        {
            if (playing) finishedFrame = Time.frameCount;
            playedThisSession = true;
            playing = false;
            if (root != null) root.SetActive(false);
        }
    }
}
