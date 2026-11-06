using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.UI
{
    // Tela de terminal (CRT verde) aberta ao ejetar um HD: o jogador escolhe 1 de 2 upgrades da Lux-90
    // com o jogo pausado (Proposta 2 do GDD v1.1, a recompensa concreta pedida no feedback do professor).
    // Desenhada com o kit CRT de art-src/UI (issue #64). Fica no Player, criada
    // sob demanda pelo DriveServer. Frente 1. Issue #31.
    public class UpgradeTerminal : MonoBehaviour
    {
        // Lido pela lanterna e por quem mais precisar ignorar input enquanto a tela está aberta.
        public static bool IsOpen { get; private set; }

        public LuxUpgradeTree.Choice Current { get; private set; }
        public int DriveNumber { get; private set; }

        public event Action<LuxUpgrade> Chosen;

        private FlashlightController flashlight;
        private SanityController sanity;
        private float previousTimeScale = 1f;
        private GUIStyle titleStyle, bodyStyle, buttonStyle;

        private static readonly Color Crt = new Color(0.45f, 1f, 0.5f);
        private static readonly Color CrtDim = new Color(0.25f, 0.6f, 0.3f);

        private void Awake()
        {
            flashlight = GetComponent<FlashlightController>();
            sanity = GetComponent<SanityController>();
        }

        private void OnDestroy()
        {
            if (Current != null) Close();
        }

        // Abre a escolha do HD driveNumber (1 a 3). Sem escolha configurada, não abre.
        public bool Open(LuxUpgradeTree tree, int driveNumber)
        {
            var choice = tree != null ? tree.ForDrive(driveNumber) : null;
            if (choice == null || Current != null) return false;
            Current = choice;
            DriveNumber = driveNumber;
            IsOpen = true;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            return true;
        }

        // 0 = opção A, 1 = opção B.
        public void Choose(int option)
        {
            if (Current == null) return;
            var upgrade = option == 0 ? Current.optionA : Current.optionB;
            LuxUpgrades.Apply(upgrade, flashlight, sanity);
            Close();
            Chosen?.Invoke(upgrade);
        }

        private void Close()
        {
            Current = null;
            IsOpen = false;
            // Não retoma se a partida acabou enquanto a tela estava aberta.
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying) Time.timeScale = previousTimeScale;
        }

        private void Update()
        {
            if (Current == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
        }

        // Tela de terminal Aethelgard com o kit CRT (screens.json): moldura, dois cartões com ícone, nome e efeito.
        // Clique ou 1/2 escolhem. Sem arte, cai na versão de texto. Issue #64.
        private void OnGUI()
        {
            if (Current == null) return;
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
            PixelHud.Text("AETHELGARD // TERMINAL DE SERVIÇO", px + 14, py + 14, PixelHud.Fosforo4);
            PixelHud.Text($"HD-0{DriveNumber} EXTRAÍDO. LUX-90 DETECTADA.", px + 14, py + 26, PixelHud.Fosforo3);
            PixelHud.Text("SELECIONE UMA MODIFICAÇÃO:", px + 14, py + 36, PixelHud.Fosforo3);

            bool blink = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f;
            for (int i = 0; i < 2; i++)
            {
                var upgrade = i == 0 ? Current.optionA : Current.optionB;
                bool selected = i == hover;
                PixelHud.Box(cx[i], cy, cw, ch, selected ? PixelHud.Fosforo4 : PixelHud.Fosforo2);
                PixelHud.Box(cx[i] + 1, cy + 1, cw - 2, ch - 2, selected ? PixelHud.Fosforo1 : PixelHud.Vazio);
                PixelHud.Sprite("hud_icons", i == 0 ? "key_1" : "key_2", cx[i] + 6, cy + 6);
                if (upgrade == null) continue;
                PixelHud.Sprite("hud_icons", DemoHud.UpgradeIcon(upgrade.effect), cx[i] + cw - 22, cy + 6, 1f, 2);
                PixelHud.Text(upgrade.displayName, cx[i] + 6, cy + 28, selected ? PixelHud.Fosforo5 : PixelHud.Fosforo4);
                var lines = PixelHud.Wrap(upgrade.description, cw - 12);
                for (int l = 0; l < lines.Count && l < 5; l++)
                    PixelHud.Text(lines[l], cx[i] + 6, cy + 42 + l * 9, PixelHud.Fosforo3);
                if (selected && blink) PixelHud.Sprite("screens", "cursor_0", cx[i] - 13, cy + 22);
            }
        }

        // Versão de texto, para cenas sem o HudArt.
        private void DrawFallback()
        {
            EnsureStyles();
            GUI.color = new Color(0f, 0.04f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(720f, Screen.width - 32f);
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.18f;
            GUI.Label(new Rect(x, y, w, 40), $"AETHELGARD // TERMINAL DE SERVIÇO // HD-0{DriveNumber} EXTRAÍDO", titleStyle);
            GUI.Label(new Rect(x, y + 44, w, 28), "Lux-90 detectada. Selecione uma modificação:", bodyStyle);

            float colW = (w - 16f) * 0.5f;
            DrawOption(new Rect(x, y + 90, colW, 170), Current.optionA, 0);
            DrawOption(new Rect(x + colW + 16f, y + 90, colW, 170), Current.optionB, 1);
        }

        private void DrawOption(Rect r, LuxUpgrade upgrade, int index)
        {
            string text = $"[{index + 1}] {upgrade.displayName.ToUpperInvariant()}\n\n{upgrade.description}";
            if (GUI.Button(r, text, buttonStyle)) Choose(index);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            titleStyle.normal.textColor = Crt;
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
            bodyStyle.normal.textColor = CrtDim;
            buttonStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(14, 14, 14, 14) };
            buttonStyle.normal.textColor = Crt;
            buttonStyle.hover.textColor = Color.white;
        }
    }
}
