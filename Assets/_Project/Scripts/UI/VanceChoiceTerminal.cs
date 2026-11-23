using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.UI
{
    // Escolha da Vance no terminal da Eclusa (planejamento-rc 3, #95): depois dos 3 HDs, a C3 já disse que ela
    // está presa. 1 = deixar (caminho direto), 2 = salvar (desvio até a cela, atalho na volta). O jogo pausa até
    // escolher, como o terminal de upgrade, para a purga não correr enquanto o jogador lê.
    public class VanceChoiceTerminal : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private float previousTimeScale = 1f;
        private GUIStyle titleStyle, bodyStyle, buttonStyle;

        private static readonly string[] Titles = { "SEGUIR PARA A ECLUSA", "LIBERTAR A DRA. VANCE" };
        private static readonly string[] Lines =
        {
            "PORTA EXTERNA DESTRAVADA. CAMINHO DIRETO. A DRA. VANCE FICA NA OBSERVAÇÃO.",
            "CELA DE OBSERVAÇÃO DO OUTRO LADO DO SETOR. 6 S PARA ABRIR. ELA ABRE O ATALHO E DESLIGA AS CÂMERAS.",
        };

        public void Open()
        {
            if (IsOpen || VanceChoice.Decision != VanceDecision.None) return;
            IsOpen = true;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        public void Choose(int option)
        {
            if (!IsOpen) return;
            VanceChoice.Decide(option == 1 ? VanceDecision.Save : VanceDecision.Leave);
            IsOpen = false;
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying) Time.timeScale = previousTimeScale;
        }

        private void OnDestroy()
        {
            if (IsOpen) Time.timeScale = previousTimeScale;
            IsOpen = false;
        }

        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            if (!PixelHud.HasArt)
            {
                DrawFallback();
                return;
            }

            PixelHud.Begin();
            const int pw = 304, ph = 164;
            int px = (PixelHud.Width - pw) / 2, py = (PixelHud.Height - ph) / 2;
            int cw = 134, ch = 92, cy = py + 52;
            int[] cx = { px + 14, px + 14 + cw + 8 };
            int hover = -1;
            for (int i = 0; i < 2; i++) if (PixelHud.MouseOver(cx[i], cy, cw, ch)) hover = i;

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && hover >= 0)
            {
                e.Use();
                Choose(hover);
                return;
            }
            if (e.type != EventType.Repaint) return;

            PixelHud.Dim(PixelHud.Vazio, 0.92f);
            PixelHud.Panel(px, py, pw, ph);
            PixelHud.Text("AETHELGARD // ECLUSA // PURGA ARMADA", px + 14, py + 14, PixelHud.Emergencia4);
            PixelHud.Text("HDS 01, 02 E 03 RECEBIDOS.", px + 14, py + 26, PixelHud.Fosforo3);
            PixelHud.Text("SINAL VITAL NA OBSERVAÇÃO: DRA. V. VANCE.", px + 14, py + 36, PixelHud.Ambar2);

            bool blink = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f;
            for (int i = 0; i < 2; i++)
            {
                bool selected = i == hover;
                PixelHud.Box(cx[i], cy, cw, ch, selected ? PixelHud.Fosforo4 : PixelHud.Fosforo2);
                PixelHud.Box(cx[i] + 1, cy + 1, cw - 2, ch - 2, selected ? PixelHud.Fosforo1 : PixelHud.Vazio);
                PixelHud.Sprite("hud_icons", i == 0 ? "key_1" : "key_2", cx[i] + 6, cy + 6);
                var title = PixelHud.Wrap(Titles[i], cw - 30);
                for (int l = 0; l < title.Count && l < 2; l++)
                    PixelHud.Text(title[l], cx[i] + 24, cy + 7 + l * 9, selected ? PixelHud.Fosforo5 : PixelHud.Fosforo4);
                var lines = PixelHud.Wrap(Lines[i], cw - 12);
                for (int l = 0; l < lines.Count && l < 6; l++)
                    PixelHud.Text(lines[l], cx[i] + 6, cy + 30 + l * 9, PixelHud.Fosforo3);
                if (selected && blink) PixelHud.Sprite("screens", "cursor_0", cx[i] - 13, cy + 22);
            }
        }

        // Versão de texto, para cenas sem o HudArt.
        private void DrawFallback()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
                titleStyle.normal.textColor = new Color(0.66f, 0.95f, 0.58f);
                bodyStyle = new GUIStyle(titleStyle) { fontSize = 16 };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true, alignment = TextAnchor.UpperLeft };
            }
            GUI.color = new Color(0f, 0.04f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float w = Mathf.Min(720f, Screen.width - 32f), x = (Screen.width - w) * 0.5f, y = Screen.height * 0.18f;
            GUI.Label(new Rect(x, y, w, 40), "AETHELGARD // ECLUSA // PURGA ARMADA", titleStyle);
            GUI.Label(new Rect(x, y + 44, w, 28), "Sinal vital na observação: Dra. V. Vance.", bodyStyle);
            float colW = (w - 16f) * 0.5f;
            for (int i = 0; i < 2; i++)
                if (GUI.Button(new Rect(x + i * (colW + 16f), y + 90, colW, 170), $"[{i + 1}] {Titles[i]}\n\n{Lines[i]}", buttonStyle))
                    Choose(i);
        }
    }
}
