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
    // Provisória em OnGUI; a arte final segue o estilo das telas da Frente 5. Fica no Player, criada
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

        private void OnGUI()
        {
            if (Current == null) return;
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
