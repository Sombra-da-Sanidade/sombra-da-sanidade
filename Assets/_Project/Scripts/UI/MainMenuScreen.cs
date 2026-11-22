using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.UI
{
    // Menu inicial com o kit CRT: logo, CONTINUAR (com checkpoint, #45), JOGAR (abre o briefing), CONFIGURAÇÕES,
    // SAIR e o melhor rank da cena do jogo (#32).
    // A cena é gerada por Sombra da Sanidade > Criar cena de Menu (MenuSceneBuilder). Frente 5, issues #32 e #64.
    public class MainMenuScreen : MonoBehaviour
    {
        // Partida nova abre a primeira fase do WingFlow (#93). Antes era um campo serializado ("Beta" na cena);
        // o valor antigo que ficou no Menu.unity é ignorado e some na próxima vez que a cena for salva.
        private string gameScene = WingFlow.FirstScene;

        [Tooltip("Segundos entre as falhas roxas do logo (Revelação pela Loucura).")]
        [SerializeField] private float glitchEvery = 3.2f;

        private enum Page { Main, Briefing, Settings }

        private const string Continue = "CONTINUAR", Play = "JOGAR", Options = "CONFIGURAÇÕES", Quit = "SAIR";
        private string[] items = { Play, Options, Quit };
        private string continueScene;
        private const string Briefing =
            "COMPLEXO EREBUS, SUB-NÍVEL 4. APAGÃO TOTAL. RECUPERE OS 3 HDS DOS SERVIDORES E SAIA PELA ECLUSA.";

        private readonly SettingsPanel settings = new SettingsPanel();
        private Page page;
        private int selected;

        public string GameScene { get => gameScene; set => gameScene = value; }
        public bool HasContinue => continueScene != null;
        public string ContinueScene => continueScene;

        private void Start()
        {
            Time.timeScale = 1f;
            GameSettings.Apply();
            RefreshContinue();
            // A música do menu foi criada pelo MenuSceneBuilder antes do mixer (#101): liga ao grupo Música aqui.
            // A faixa do menu vem do AudioConfig (#103): troca o drone antigo da cena sem precisar regerar a cena.
            var menuMusic = Audio.AudioConfig.Load().menuMusic;
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source.outputAudioMixerGroup == null) Audio.AudioService.Route(source, Audio.AudioBus.Music);
                if (menuMusic == null || !source.loop || source.clip == menuMusic) continue;
                source.clip = menuMusic;
                source.volume = 1f;   // a faixa já sai no volume certo do render (-24 LUFS)
                source.Play();
            }
        }

        // #45: com checkpoint salvo e a cena da próxima ala no Build Profile, o menu oferece CONTINUAR.
        // A cena vem do WingFlow (#93): a Eclusa não segue o padrão "Ala" + nome.
        public void RefreshContinue()
        {
            continueScene = null;
            if (Checkpoint.TryLoad(out var data))
            {
                string scene = WingFlow.SceneFor(data.nextWing);
                if (scene != null && Application.CanStreamedLevelBeLoaded(scene)) continueScene = scene;
            }
            items = continueScene != null ? new[] { Continue, Play, Options, Quit } : new[] { Play, Options, Quit };
            selected = 0;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || StoryIntro.Blocking) return;
            switch (page)
            {
                case Page.Settings:
                    if (settings.HandleKeyboard(kb)) page = Page.Main;
                    break;
                case Page.Briefing:
                    if (kb.escapeKey.wasPressedThisFrame) { Audio.UiSfx.Back(); page = Page.Main; }
                    else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) { Audio.UiSfx.Confirm(); StartGame(); }
                    break;
                default:
                    if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { selected = (selected + items.Length - 1) % items.Length; Audio.UiSfx.Move(); }
                    if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { selected = (selected + 1) % items.Length; Audio.UiSfx.Move(); }
                    if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) { Audio.UiSfx.Confirm(); Activate(selected); }
                    break;
            }
        }

        private void Activate(int index)
        {
            switch (items[index])
            {
                case Continue: Load(continueScene); break;
                case Play: page = Page.Briefing; break;
                case Options: page = Page.Settings; settings.Reset(); break;
                default: Application.Quit(); break;
            }
        }

        // Partida nova: zera as mortes do rank (#32) e o checkpoint antigo (#45).
        public void StartGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                Debug.LogWarning($"[Menu] A cena '{gameScene}' não está no Build Profile.");
                return;
            }
            Checkpoint.Clear();
            Load(gameScene);
        }

        private static void Load(string scene)
        {
            RunStats.StartRun();
            CrtTransition.LoadScene(scene);
        }

        private void OnGUI()
        {
            if (!PixelHud.HasArt || StoryIntro.Blocking) return;
            PixelHud.Begin();
            var e = Event.current;
            int w = PixelHud.Width, h = PixelHud.Height, cx = w / 2;

            if (e.type == EventType.Repaint)
            {
                PixelHud.Dim(PixelHud.Vazio, 1f);
                for (int y = 3; y < h; y += 4) PixelHud.Box(0, y, w, 1, PixelHud.Fosforo1);
            }

            if (page == Page.Settings)
            {
                if (settings.OnGUI(cx - SettingsPanel.PanelWidth / 2, (h - SettingsPanel.PanelHeight) / 2)) page = Page.Main;
                return;
            }
            if (page == Page.Briefing)
            {
                DrawBriefing(cx, h);
                return;
            }

            const int bw = 104;
            int bx = cx - bw / 2;
            int top = h / 2 - 16 - (items.Length - 3) * 9;   // com CONTINUAR, a lista sobe meio botão
            int ButtonY(int i) => top + i * 18;
            for (int i = 0; i < items.Length; i++)
            {
                if (!PixelHud.MouseOver(bx, ButtonY(i) + 2, bw, 11)) continue;
                selected = i;
                if (e.type == EventType.MouseDown && e.button == 0) { e.Use(); Audio.UiSfx.Confirm(); Activate(i); return; }
            }
            if (e.type != EventType.Repaint) return;

            bool glitch = Mathf.Repeat(Time.unscaledTime, glitchEvery) < 0.14f;
            PixelHud.Sprite("logo", glitch ? "logo_glitch" : "logo", cx - 80, h / 2 - 76);
            for (int i = 0; i < items.Length; i++) PixelHud.Button(items[i], bx, ButtonY(i), bw, i == selected);
            // Rank da partida completa: o calculado ao escapar pela Eclusa.
            var best = GameManager.BestRank(WingFlow.SceneAt(WingFlow.Count - 1));
            if (best != null) PixelHud.TextCentered("MELHOR RANK: " + best.Value, cx, ButtonY(items.Length) + 4, PixelHud.Fosforo3);
            PixelHud.Sprite("hud_bars", "key_space", cx - 68, h - 20);
            PixelHud.Text("CONFIRMAR", cx - 48, h - 18, PixelHud.Fosforo3);
            PixelHud.Sprite("hud_bars", "key_esc", cx + 8, h - 20);
            PixelHud.Text("VOLTAR", cx + 28, h - 18, PixelHud.Fosforo3);
        }

        private void DrawBriefing(int cx, int h)
        {
            var e = Event.current;
            const int pw = 288, ph = 156;
            int x = cx - pw / 2, y = (h - ph) / 2;
            int bx = cx - 56, by = y + ph - 28;
            bool over = PixelHud.MouseOver(bx, by + 2, 112, 11);
            if (over && e.type == EventType.MouseDown && e.button == 0) { e.Use(); Audio.UiSfx.Confirm(); StartGame(); return; }
            if (e.type != EventType.Repaint) return;

            PixelHud.Panel(x, y, pw, ph);
            PixelHud.Text("AETHELGARD DYNAMICS // ORDEM DE SERVIÇO", x + 14, y + 14, PixelHud.Fosforo4);
            var lines = PixelHud.Wrap(Briefing, pw - 28);
            for (int i = 0; i < lines.Count; i++) PixelHud.Text(lines[i], x + 14, y + 30 + i * 10, PixelHud.Fosforo3);

            int cy = y + 72;
            Control("hud_icons", "key_e", "SEGURAR: INTERAGIR", x + 14, cy);
            Control("hud_icons", "key_f", "LANTERNA", x + 14, cy + 12);
            Control("hud_icons", "mouse_r", "PULSO EMP", x + 14, cy + 24);
            Control("hud_bars", "key_tab", "RELÓGIO", x + 150, cy);
            Control("hud_bars", "key_space", "MULTÍMETRO", x + 150, cy + 12);
            Control("hud_bars", "key_esc", "PAUSA", x + 150, cy + 24);
            PixelHud.Button("INICIAR TURNO", bx, by, 112, true);
        }

        private static void Control(string sheet, string key, string label, int x, int y)
        {
            PixelHud.Sprite(sheet, key, x, y);
            PixelHud.Text(label, x + (sheet == "hud_bars" ? 20 : 12), y + 1, PixelHud.Fosforo3);
        }
    }
}
