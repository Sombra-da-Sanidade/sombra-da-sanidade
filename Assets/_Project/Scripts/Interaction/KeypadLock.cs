using System;
using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SombraDaSanidade.Interaction
{
    // Trava de senha do servidor do HD-03 (GDD 7, Ala Executiva: "Senhas com sanidade baixa"). A senha é
    // sorteada a cada partida e cada dígito está numa pista em fósforo (Level.CodeClue) que só aparece com a
    // sanidade baixa ou com um Módulo Neural. E abre o teclado; números digitam, Backspace apaga, Enter confirma.
    // O jogo não pausa: o monstro continua andando. Senha errada faz barulho (bipe de erro).
    public class KeypadLock : MonoBehaviour, IInteractable
    {
        [Tooltip("Servidor que a senha libera.")]
        [SerializeField] private DriveServer server;
        [Tooltip("Quantidade de dígitos (uma pista por dígito).")]
        [SerializeField, Range(1, 6)] private int digits = 4;
        [Tooltip("Senha fixa (testes). Vazio: sorteada ao carregar a cena.")]
        [SerializeField] private string fixedCode = "";
        [Tooltip("Raio do ruído de uma senha errada (o bipe chama o monstro).")]
        [SerializeField] private float wrongNoiseRadius = 9f;
        [Tooltip("Longe disto (m), o teclado fecha sozinho.")]
        [SerializeField] private float closeDistance = 2.2f;
        [SerializeField] private SpriteRenderer screen;

        public string Prompt => "Digitar senha";
        public bool CanInteract => !IsUnlocked && !IsOpen;
        public float HoldDuration => 0f;

        public string Code { get; private set; }
        public string Entry { get; private set; } = "";
        public bool IsOpen { get; private set; }
        public bool IsUnlocked { get; private set; }
        public int WrongAttempts { get; private set; }
        public DriveServer Server => server;

        public event Action<KeypadLock> Unlocked;
        public event Action<KeypadLock> WrongCode;

        private Transform player;
        private int openedFrame;
        private float messageUntil;
        private string message = "";

        // No enum Key o Digit0 vem depois do Digit9: tabela explícita.
        private static readonly Key[] DigitKeys =
            { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] NumpadKeys =
            { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        private static readonly Color Locked =new Color(0.87f, 0.25f, 0.22f);
        private static readonly Color Open = new Color(0.33f, 0.72f, 0.44f);

        private void Awake()
        {
            Code = IsValid(fixedCode) ? fixedCode : RandomCode(digits);
            if (screen != null) screen.color = Locked;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        public char Digit(int index) => index >= 0 && index < Code.Length ? Code[index] : '?';

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            IsOpen = true;
            Entry = "";
            openedFrame = Time.frameCount;
        }

        public void Close() => IsOpen = false;

        public void Type(char c)
        {
            if (!IsOpen || IsUnlocked || c < '0' || c > '9' || Entry.Length >= Code.Length) return;
            Entry += c;
        }

        public void Erase()
        {
            if (IsOpen && Entry.Length > 0) Entry = Entry.Substring(0, Entry.Length - 1);
        }

        // true = senha certa. Errada: bipe alto (ruído), limpa o visor e conta a tentativa.
        public bool Submit()
        {
            if (!IsOpen || IsUnlocked) return false;
            if (Entry == Code)
            {
                IsUnlocked = true;
                if (server != null) server.Unlock();
                if (screen != null) screen.color = Open;
                Show("ACESSO LIBERADO");
                IsOpen = false;
                Unlocked?.Invoke(this);
                return true;
            }
            WrongAttempts++;
            Entry = "";
            Show("SENHA INVÁLIDA");
            NoiseBus.Emit(transform.position, wrongNoiseRadius);
            WrongCode?.Invoke(this);
            return false;
        }

        private void Update()
        {
            if (!IsOpen) return;
            bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            if (!playing || (player != null && Vector2.Distance(player.position, transform.position) > closeDistance))
            {
                Close();
                return;
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (Time.frameCount > openedFrame && kb.eKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            for (int d = 0; d <= 9; d++)
                if (kb[DigitKeys[d]].wasPressedThisFrame || kb[NumpadKeys[d]].wasPressedThisFrame)
                    Type((char)('0' + d));
            if (kb.backspaceKey.wasPressedThisFrame) Erase();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Submit();
        }

        private void Show(string text)
        {
            message = text;
            messageUntil = Time.time + 1.5f;
        }

        private static bool IsValid(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            foreach (char c in code) if (c < '0' || c > '9') return false;
            return true;
        }

        private static string RandomCode(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++) chars[i] = (char)('0' + Random.Range(0, 10));
            return new string(chars);
        }

        // Visor do teclado embaixo da tela (kit CRT). Sem a arte, versão de texto.
        private void OnGUI()
        {
            bool showMessage = Time.time < messageUntil;
            if (!IsOpen && !showMessage) return;
            if (Event.current.type != EventType.Repaint) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            string slots = "";
            for (int i = 0; i < Code.Length; i++) slots += (i < Entry.Length ? Entry[i].ToString() : "_") + (i < Code.Length - 1 ? " " : "");
            bool error = showMessage && !IsUnlocked;

            if (UI.PixelHud.HasArt)
            {
                UI.PixelHud.Begin();
                const int w = 150, h = 50;
                int x = (UI.PixelHud.Width - w) / 2, y = UI.PixelHud.Height - h - 12;
                UI.PixelHud.Box(x, y, w, h, UI.PixelHud.Fosforo2);
                UI.PixelHud.Box(x + 1, y + 1, w - 2, h - 2, UI.PixelHud.Vazio);
                UI.PixelHud.Text("DIRETORIA // TRAVA DO HD-03", x + 6, y + 5, UI.PixelHud.Fosforo3);
                if (showMessage)
                    UI.PixelHud.TextCentered(message, x + w / 2, y + 20, error ? UI.PixelHud.Emergencia4 : UI.PixelHud.Fosforo5, 1f, 1);
                else
                    UI.PixelHud.TextCentered(slots, x + w / 2, y + 17, UI.PixelHud.Fosforo5, 1f, 2);
                UI.PixelHud.Text("0-9 DIGITA  ENTER CONFIRMA  E SAI", x + 6, y + h - 11, UI.PixelHud.Fosforo2);
                return;
            }

            var style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = error ? new Color(0.9f, 0.3f, 0.25f) : new Color(0.45f, 1f, 0.5f);
            var r = new Rect((Screen.width - 420f) / 2f, Screen.height - 130f, 420f, 100f);
            GUI.Box(r, (showMessage ? message : $"SENHA: {slots}") + "\n0-9 digita · Enter confirma · E sai", style);
        }
    }
}
