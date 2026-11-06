using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.UI
{
    // Pausa com Esc durante a partida: continuar, configurações, reiniciar e sair. Kit CRT de art-src/UI.
    // Criada sozinha em toda cena que tem GameManager (não precisa editar cena nem prefab). Issue #64.
    public class PauseMenu : MonoBehaviour
    {
        // Cena do menu inicial (#32). Se não estiver no Build Profile, "SAIR" fecha o jogo.
        public const string MenuScene = "Menu";

        private static readonly string[] Items = { "CONTINUAR", "CONFIGURAÇÕES", "REINICIAR", "SAIR" };
        private const int PanelW = 144, PanelH = 124, ButtonW = 112;

        public bool IsOpen { get; private set; }

        private readonly SettingsPanel settings = new SettingsPanel();
        private bool inSettings;
        private int selected;
        private float previousTimeScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GameSettings.Apply();
            if (FindAnyObjectByType<GameManager>() == null || FindAnyObjectByType<PauseMenu>() != null) return;
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
        }

        private void OnDestroy()
        {
            if (IsOpen) Close();
        }

        public void Open()
        {
            if (IsOpen || UpgradeTerminal.IsOpen) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            IsOpen = true;
            inSettings = false;
            selected = 0;
            ModalScreen.Push();
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            ModalScreen.Pop();
            GameSettings.Save();
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying) Time.timeScale = previousTimeScale;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!IsOpen)
            {
                if (kb.escapeKey.wasPressedThisFrame) Open();
                return;
            }
            if (inSettings)
            {
                if (settings.HandleKeyboard(kb)) inSettings = false;
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) selected = (selected + Items.Length - 1) % Items.Length;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % Items.Length;
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Activate(selected);
        }

        private void Activate(int index)
        {
            switch (index)
            {
                case 0:
                    Close();
                    break;
                case 1:
                    inSettings = true;
                    settings.Reset();
                    break;
                case 2:
                    Close();
                    if (GameManager.Instance != null) GameManager.Instance.Restart();
                    else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    break;
                default:
                    Close();
                    Time.timeScale = 1f;
                    if (Application.CanStreamedLevelBeLoaded(MenuScene)) SceneManager.LoadScene(MenuScene);
                    else Application.Quit();
                    break;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || !PixelHud.HasArt) return;
            PixelHud.Begin();
            var e = Event.current;

            if (e.type == EventType.Repaint)
            {
                PixelHud.Dim(PixelHud.Vazio, 0.65f);
            }

            if (inSettings)
            {
                int sx = (PixelHud.Width - SettingsPanel.PanelWidth) / 2, sy = (PixelHud.Height - SettingsPanel.PanelHeight) / 2;
                if (settings.OnGUI(sx, sy)) inSettings = false;
                return;
            }

            int x = (PixelHud.Width - PanelW) / 2, y = (PixelHud.Height - PanelH) / 2;
            int bx = x + (PanelW - ButtonW) / 2;
            int ButtonY(int i) => y + 36 + i * 18;
            for (int i = 0; i < Items.Length; i++)
            {
                if (!PixelHud.MouseOver(bx, ButtonY(i) + 2, ButtonW, 11)) continue;
                selected = i;
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    e.Use();
                    Activate(i);
                    return;
                }
            }
            if (e.type != EventType.Repaint) return;

            PixelHud.Panel(x, y, PanelW, PanelH);
            PixelHud.TextCentered("PAUSADO", x + PanelW / 2, y + 14, PixelHud.Fosforo5, 1f, 2);
            for (int i = 0; i < Items.Length; i++) PixelHud.Button(Items[i], bx, ButtonY(i), ButtonW, i == selected);
        }
    }
}
