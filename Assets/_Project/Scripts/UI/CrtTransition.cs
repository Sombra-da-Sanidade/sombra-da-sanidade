using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.UI
{
    // Transição analógica de tubo CRT (docs/planejamento-rc.md, seção 2; issue #94). Frente 5.
    // Ao sair de uma cena a imagem achata numa linha horizontal de fósforo, a linha encolhe até o ponto e apaga;
    // a cena nova abre ao contrário. No Game Over a tela dá o mesmo apagão e abre sobre a tela de fim.
    // Ao abrir o jogo, a primeira cena abre da linha.
    // Criada sozinha na primeira cena e mantida entre cenas (não precisa editar cena nem prefab).
    // Quem troca de cena chama CrtTransition.LoadScene no lugar de SceneManager.LoadScene.
    // Tempo sem escala: funciona com o jogo pausado (Game Over e vitória param o Time.timeScale).
    public class CrtTransition : MonoBehaviour
    {
        public enum Phase { Idle, Closing, Closed, Opening }

        private static CrtTransition instance;
        private const float MaxStep = 1f / 30f;

        public static CrtTransition Instance => instance;
        public static Phase Current => instance != null ? instance.phase : Phase.Idle;
        // 0 = tela aberta, 1 = apagada.
        public static float Amount => instance != null ? instance.amount : 0f;
        public static bool Busy => Current != Phase.Idle;
        // Cena que vai carregar quando a tela apagar, ou null.
        public static string PendingScene => instance != null ? instance.pendingScene : null;
        // Quanto a tela estava fechada quando a última cena foi carregada (1 = apagada). Usado pelo teste.
        public static float CoverageAtLastLoad { get; private set; } = -1f;
        // Enquanto o apagão do Game Over fecha, a tela de fim fica escondida: quem fecha é a imagem da morte,
        // e a tela "FIM DA LINHA" só aparece quando a linha abre. Sem isso a tela de fim fechava e reabria igual.
        public static bool HidingEndScreen => instance != null && instance.gameOverPulse
            && (instance.phase == Phase.Closing || instance.phase == Phase.Closed);

        private CrtTransitionConfig config;
        private Phase phase;
        private float amount;
        private string pendingScene;
        private bool loading;
        private bool gameOverPulse;
        private float holdLeft;
        private GameManager watched;

        // Sem recarga de domínio ao entrar no Play, o estático da partida anterior não pode sobrar.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            CoverageAtLastLoad = -1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var t = Ensure();
            if (!t.config.openOnBoot) return;
            t.amount = 1f;
            t.phase = Phase.Opening;
            Audio.UiSfx.CrtOn();
        }

        private static CrtTransition Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("CrtTransition");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CrtTransition>();
            return instance;
        }

        // Fecha a tela, carrega a cena e abre na cena nova. Um segundo pedido durante a troca é ignorado
        // (Enter apertado duas vezes no menu, R durante a troca). Fora do Play, carrega direto.
        public static void LoadScene(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return;
            if (!Application.isPlaying || !Application.CanStreamedLevelBeLoaded(scene))
            {
                // Cena fora do Build Profile: deixa o erro da Unity aparecer como antes, sem prender a tela no preto.
                SceneManager.LoadScene(scene);
                return;
            }
            var t = Ensure();
            if (t.pendingScene != null) return;
            t.pendingScene = scene;
            // A imagem congela enquanto o tubo desliga: o Thomas e os monstros não andam durante o fechamento.
            // A cena nova volta ao tempo normal (o timeScale é restaurado antes de carregar).
            Time.timeScale = 0f;
            // Continua de onde estava: se a tela já estava abrindo (ou no apagão do Game Over), não pisca.
            if (t.phase != Phase.Closed) t.phase = Phase.Closing;
            // O tubo desliga. Durante o apagão do Game Over fica mudo (tocam o grito e o sting).
            if (!t.gameOverPulse) Audio.UiSfx.CrtOff();
        }

        // Faixa da imagem que ainda aparece, em pixels de tela. amount 0: a tela inteira; até collapseShare a
        // altura cai até virar uma linha da espessura de line; depois a linha encolhe até o centro; 1: largura 0.
        public static Rect Band(float amount, float width, float height, float line, float collapseShare)
        {
            amount = Mathf.Clamp01(amount);
            line = Mathf.Max(1f, line);
            collapseShare = Mathf.Clamp(collapseShare, 0.01f, 0.99f);
            if (amount <= 0f) return new Rect(0f, 0f, width, height);

            float v = Mathf.Clamp01(amount / collapseShare);
            float s = Mathf.Clamp01((amount - collapseShare) / (1f - collapseShare));
            // Começa devagar e despenca no fim, como o feixe do tubo ao desligar.
            float bandH = Mathf.Max(line, Snap(Mathf.Lerp(height, line, v * v), line));
            float bandW = amount >= 1f ? 0f : Snap(Mathf.Lerp(width, 0f, s * s), line);
            return new Rect((width - bandW) * 0.5f, (height - bandH) * 0.5f, bandW, bandH);
        }

        private static float Snap(float value, float step) => Mathf.Round(value / step) * step;

        private void Awake()
        {
            config = CrtTransitionConfig.Load();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Watch(null);
            if (instance == this) instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || !loading) return;
            loading = false;
            pendingScene = null;
            gameOverPulse = false;
            // Segura no preto um instante na cena nova: esconde a câmera se ajeitando no spawn e marca a troca.
            phase = Phase.Closed;
            holdLeft = config.loadHoldSeconds;
        }

        private void Update()
        {
            // Cada cena tem o próprio GameManager: troca a inscrição quando ele muda.
            if (GameManager.Instance != watched) Watch(GameManager.Instance);

            // Passo limitado: o quadro logo depois de carregar a cena chega com o tempo do carregamento inteiro e
            // pulava a abertura de uma vez (a cena nova aparecia sem transição).
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            switch (phase)
            {
                case Phase.Closing:
                    amount = Mathf.MoveTowards(amount, 1f, dt / config.closeSeconds);
                    if (amount < 1f) break;
                    phase = Phase.Closed;
                    holdLeft = config.gameOverHoldSeconds;
                    break;
                case Phase.Closed:
                    if (pendingScene != null)
                    {
                        if (!loading) BeginLoad();
                    }
                    else
                    {
                        holdLeft -= dt;
                        if (holdLeft > 0f) break;
                        phase = Phase.Opening;
                        // O tubo liga na cena nova. Abrir sobre a tela de Game Over fica mudo (sting tocando).
                        if (!gameOverPulse) Audio.UiSfx.CrtOn();
                        gameOverPulse = false;
                    }
                    break;
                case Phase.Opening:
                    amount = Mathf.MoveTowards(amount, 0f, dt / config.openSeconds);
                    if (amount <= 0f) phase = Phase.Idle;
                    break;
            }
        }

        private void BeginLoad()
        {
            loading = true;
            CoverageAtLastLoad = amount;
            Time.timeScale = 1f;
            SceneManager.LoadScene(pendingScene);
        }

        private void Watch(GameManager gm)
        {
            // ReferenceEquals: o GameManager da cena anterior já foi destruído, mas a inscrição precisa sair.
            if (!ReferenceEquals(watched, null)) watched.GameOverTriggered -= OnGameOver;
            watched = gm;
            if (gm != null) gm.GameOverTriggered += OnGameOver;
        }

        // Apagão do Game Over: fecha, segura um instante no preto e abre sobre a tela de fim.
        private void OnGameOver(DeathCause cause)
        {
            if (pendingScene != null) return;
            gameOverPulse = true;
            phase = Phase.Closing;
        }

        private void OnGUI()
        {
            // Acima de todas as telas (HUD, resultado, pausa, cutscene).
            GUI.depth = -1000;
            if (amount <= 0f || Event.current.type != EventType.Repaint) return;

            PixelHud.Begin();
            float w = Screen.width, h = Screen.height, line = PixelHud.Px;
            var band = Band(amount, w, h, line, config.collapseShare);

            // Preto em volta da faixa.
            PixelHud.Fill(new Rect(0f, 0f, w, band.yMin), PixelHud.Vazio);
            PixelHud.Fill(new Rect(0f, band.yMax, w, h - band.yMax), PixelHud.Vazio);
            PixelHud.Fill(new Rect(0f, band.yMin, band.xMin, band.height), PixelHud.Vazio);
            PixelHud.Fill(new Rect(band.xMax, band.yMin, w - band.xMax, band.height), PixelHud.Vazio);
            if (band.width <= 0f) return;

            float v = Mathf.Clamp01(amount / Mathf.Max(0.01f, config.collapseShare));
            if (v < 1f)
            {
                // A imagem que achata fica mais clara, como o fósforo concentrando o feixe.
                PixelHud.Fill(band, PixelHud.Fosforo5, 0.5f * v * v);
                return;
            }
            // Só a linha: brilho em volta e o traço de fósforo.
            PixelHud.Fill(new Rect(band.x, band.y - line * 2f, band.width, band.height + line * 4f), PixelHud.Fosforo4, 0.35f);
            PixelHud.Fill(band, PixelHud.Fosforo5);
        }
    }
}
