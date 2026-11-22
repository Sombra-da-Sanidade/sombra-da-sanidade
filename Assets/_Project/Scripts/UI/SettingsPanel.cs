using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.UI
{
    // Página de configurações com o kit CRT, usada pela pausa e pelo menu inicial.
    // Teclado: W/S ou setas escolhem, A/D ou setas ajustam, Enter/Espaço confirmam, Esc volta. Mouse: clique. Issue #64.
    // Volumes de música, efeitos e voz por grupo do mixer (#101).
    public class SettingsPanel
    {
        private const int Sliders = 5;              // volume, música, efeitos, voz, brilho
        private const int FullscreenRow = 5, BackRow = 6, Rows = 7;
        private const float Step = 0.1f;
        private const int RowHeight = 18;
        public const int PanelWidth = 256, PanelHeight = 160;

        private static readonly string[] Labels = { "VOLUME", "MÚSICA", "EFEITOS", "VOZ", "BRILHO" };

        private int selected;

        public void Reset() => selected = 0;

        private static float Get(int row) => row switch
        {
            0 => GameSettings.Volume,
            1 => GameSettings.MusicVolume,
            2 => GameSettings.EffectsVolume,
            3 => GameSettings.VoiceVolume,
            _ => GameSettings.Brightness
        };

        private static void Set(int row, float value)
        {
            switch (row)
            {
                case 0: GameSettings.Volume = value; break;
                case 1: GameSettings.MusicVolume = value; break;
                case 2: GameSettings.EffectsVolume = value; break;
                case 3: GameSettings.VoiceVolume = value; break;
                default: GameSettings.Brightness = value; break;
            }
        }

        // Retorna true quando o jogador pede para voltar.
        public bool HandleKeyboard(Keyboard kb)
        {
            if (kb == null) return false;
            if (kb.escapeKey.wasPressedThisFrame) { Audio.UiSfx.Back(); return Close(); }
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { selected = (selected + Rows - 1) % Rows; Audio.UiSfx.Move(); }
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { selected = (selected + 1) % Rows; Audio.UiSfx.Move(); }
            int dir = (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ? 1 : 0)
                - (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame ? 1 : 0);
            if (dir != 0) { Adjust(dir); Audio.UiSfx.Move(); }
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                if (selected == BackRow) Audio.UiSfx.Back();
                else Audio.UiSfx.Confirm();
                if (selected == FullscreenRow) GameSettings.Fullscreen = !GameSettings.Fullscreen;
                if (selected == BackRow) return Close();
            }
            return false;
        }

        private void Adjust(int dir)
        {
            if (selected < Sliders) Set(selected, Get(selected) + dir * Step);
            else if (selected == FullscreenRow) GameSettings.Fullscreen = !GameSettings.Fullscreen;
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
            int RowY(int i) => y + 24 + i * RowHeight;
            int backX = x + (PanelWidth - 96) / 2;

            for (int i = 0; i < BackRow; i++)
                if (PixelHud.MouseOver(x + 8, RowY(i), PanelWidth - 16, RowHeight - 2)) selected = i;
            if (PixelHud.MouseOver(backX, RowY(BackRow) + 4, 96, 11)) selected = BackRow;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (selected < Sliders && PixelHud.MouseOver(sliderX - 4, RowY(selected), sliderW + 8, RowHeight - 2))
                {
                    Set(selected, Mathf.Clamp01((e.mousePosition.x / PixelHud.Px - sliderX) / sliderW));
                    Audio.UiSfx.Move();
                    e.Use();
                }
                else if (selected == FullscreenRow && PixelHud.MouseOver(x + 8, RowY(FullscreenRow), PanelWidth - 16, RowHeight - 2))
                {
                    GameSettings.Fullscreen = !GameSettings.Fullscreen;
                    Audio.UiSfx.Confirm();
                    e.Use();
                }
                else if (selected == BackRow && PixelHud.MouseOver(backX, RowY(BackRow) + 4, 96, 11))
                {
                    e.Use();
                    Audio.UiSfx.Back();
                    return Close();
                }
            }
            if (e.type != EventType.Repaint) return false;

            PixelHud.Panel(x, y, PanelWidth, PanelHeight);
            PixelHud.Text("CONFIGURAÇÕES", x + 16, y + 10, PixelHud.Fosforo5);
            for (int i = 0; i < Sliders; i++)
            {
                Row(Labels[i], i, x, RowY(i));
                PixelHud.Slider(sliderX, RowY(i), sliderW, Get(i), selected == i);
            }
            Row("TELA CHEIA", FullscreenRow, x, RowY(FullscreenRow));
            PixelHud.Sprite("screens", GameSettings.Fullscreen ? "check_on" : "check_off", sliderX - 3, RowY(FullscreenRow));
            PixelHud.Button("VOLTAR", backX, RowY(BackRow) + 2, 96, selected == BackRow);
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
