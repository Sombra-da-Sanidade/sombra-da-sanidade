using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.UI
{
    // Página de configurações com o kit CRT, usada pela pausa e pelo menu inicial.
    // Teclado: W/S ou setas escolhem, A/D ou setas ajustam, Enter/Espaço confirmam, Esc volta. Mouse: clique. Issue #64.
    public class SettingsPanel
    {
        private const int Rows = 4; // volume, brilho, tela cheia, voltar
        private const float Step = 0.1f;
        public const int PanelWidth = 256, PanelHeight = 132;

        private int selected;

        public void Reset() => selected = 0;

        // Retorna true quando o jogador pede para voltar.
        public bool HandleKeyboard(Keyboard kb)
        {
            if (kb == null) return false;
            if (kb.escapeKey.wasPressedThisFrame) return Close();
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) selected = (selected + Rows - 1) % Rows;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % Rows;
            int dir = (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ? 1 : 0)
                - (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame ? 1 : 0);
            if (dir != 0) Adjust(dir);
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                if (selected == 2) GameSettings.Fullscreen = !GameSettings.Fullscreen;
                if (selected == 3) return Close();
            }
            return false;
        }

        private void Adjust(int dir)
        {
            if (selected == 0) GameSettings.Volume += dir * Step;
            else if (selected == 1) GameSettings.Brightness += dir * Step;
            else if (selected == 2) GameSettings.Fullscreen = !GameSettings.Fullscreen;
        }

        private bool Close()
        {
            GameSettings.Save();
            return true;
        }

        // Desenha e trata o mouse. Retorna true quando "VOLTAR" é clicado.
        public bool OnGUI(int x, int y)
        {
            var e = Event.current;
            int sliderX = x + 118, sliderW = 112;
            int RowY(int i) => y + 26 + i * 22;

            for (int i = 0; i < 3; i++)
                if (PixelHud.MouseOver(x + 8, RowY(i), PanelWidth - 16, 16)) selected = i;
            if (PixelHud.MouseOver(x + (PanelWidth - 96) / 2, RowY(3) + 4, 96, 11)) selected = 3;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (selected < 2 && PixelHud.MouseOver(sliderX - 4, RowY(selected), sliderW + 8, 16))
                {
                    float v = Mathf.Clamp01((e.mousePosition.x / PixelHud.Px - sliderX) / sliderW);
                    if (selected == 0) GameSettings.Volume = v; else GameSettings.Brightness = v;
                    e.Use();
                }
                else if (selected == 2 && PixelHud.MouseOver(x + 8, RowY(2), PanelWidth - 16, 16))
                {
                    GameSettings.Fullscreen = !GameSettings.Fullscreen;
                    e.Use();
                }
                else if (selected == 3 && PixelHud.MouseOver(x + (PanelWidth - 96) / 2, RowY(3) + 4, 96, 11))
                {
                    e.Use();
                    return Close();
                }
            }
            if (e.type != EventType.Repaint) return false;

            PixelHud.Panel(x, y, PanelWidth, PanelHeight);
            PixelHud.Text("CONFIGURAÇÕES", x + 16, y + 12, PixelHud.Fosforo5);
            Row("VOLUME", 0, x, RowY(0));
            PixelHud.Slider(sliderX, RowY(0), sliderW, GameSettings.Volume, selected == 0);
            Row("BRILHO", 1, x, RowY(1));
            PixelHud.Slider(sliderX, RowY(1), sliderW, GameSettings.Brightness, selected == 1);
            Row("TELA CHEIA", 2, x, RowY(2));
            PixelHud.Sprite("screens", GameSettings.Fullscreen ? "check_on" : "check_off", sliderX - 3, RowY(2));
            PixelHud.Button("VOLTAR", x + (PanelWidth - 96) / 2, RowY(3) + 2, 96, selected == 3);
            return false;
        }

        private void Row(string label, int index, int x, int y)
        {
            bool sel = selected == index;
            if (sel) PixelHud.Sprite("screens", "cursor_0", x + 2, y + 1);
            PixelHud.Text(label, x + 16, y + 6, sel ? PixelHud.Fosforo5 : PixelHud.Fosforo3);
        }
    }
}
