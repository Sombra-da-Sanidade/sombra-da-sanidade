using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.Core
{
    public enum GameState { Playing, GameOver, Victory }

    public enum DeathCause { Monster, Sanity }

    // Estado da partida, HDs coletados e condições de vitória/derrota (GDD 4.4).
    // Um por cena. As telas de fim ficam em UI.ResultScreen (issue #64).
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int drivesRequired = 3;

        [Tooltip("Dificuldade desta ala (Proposta 1). Vazio: os sistemas usam os próprios configs.")]
        [SerializeField] private DifficultyProfile difficulty;

        public DifficultyProfile ActiveProfile => difficulty;

        public event Action<DifficultyProfile> ProfileChanged;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsPlaying => State == GameState.Playing;
        public int DrivesCollected { get; private set; }
        public int DrivesRequired => drivesRequired;
        public bool HasAllDrives => DrivesCollected >= drivesRequired;
        public DeathCause LastDeathCause { get; private set; }
        public float ElapsedTime { get; private set; }

        public event Action<int> DriveCollected;
        public event Action<DeathCause> GameOverTriggered;
        public event Action VictoryTriggered;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (IsPlaying)
            {
                ElapsedTime += Time.deltaTime;
                return;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
        }

        public void SetProfile(DifficultyProfile profile)
        {
            if (profile == difficulty) return;
            difficulty = profile;
            ProfileChanged?.Invoke(profile);
        }

        public void CollectDrive()
        {
            if (!IsPlaying || HasAllDrives) return;
            DrivesCollected++;
            DriveCollected?.Invoke(DrivesCollected);
        }

        public void TriggerGameOver(DeathCause cause)
        {
            if (!IsPlaying) return;
            State = GameState.GameOver;
            LastDeathCause = cause;
            Time.timeScale = 0f;
            GameOverTriggered?.Invoke(cause);
        }

        public void TriggerVictory()
        {
            if (!IsPlaying || !HasAllDrives) return;
            State = GameState.Victory;
            Time.timeScale = 0f;
            VictoryTriggered?.Invoke();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            // A cena precisa estar no Build Profile (os menus de Sandbox e protótipo já adicionam).
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnGUI()
        {
            if (IsPlaying) return;
            // Tela com o kit CRT (UI.ResultScreen); sem a arte, a versão de texto abaixo.
            if (UI.ResultScreen.Draw(this)) return;
            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string title;
            string body;
            if (State == GameState.Victory)
            {
                var wing = Level.WingCheckpoint.Instance;
                title = wing != null ? Level.WingCheckpoint.DisplayName(wing.Wing) + " CONCLUÍDA" : "VOCÊ ESCAPOU";
                body = $"HDs: {DrivesCollected}/{drivesRequired}   Tempo: {FormatTime(ElapsedTime)}";
                if (wing != null && wing.StatusLine.Length > 0) body += "   " + wing.StatusLine;
            }
            else
            {
                title = "FIM DA LINHA";
                body = LastDeathCause == DeathCause.Monster
                    ? "O Espécime Vazio te alcançou."
                    : "Sua mente colapsou no escuro.";
            }

            float w = Screen.width;
            float h = Screen.height;
            GUI.Label(new Rect(0, h * 0.35f, w, 60), title, titleStyle);
            GUI.Label(new Rect(0, h * 0.35f + 70, w, 30), body, bodyStyle);
            GUI.Label(new Rect(0, h * 0.35f + 110, w, 30), "Pressione R para reiniciar", bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 42, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.85f, 0.2f, 0.2f);
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            bodyStyle.normal.textColor = new Color(0.75f, 0.95f, 0.75f);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
