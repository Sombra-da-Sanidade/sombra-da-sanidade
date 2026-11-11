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

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsPlaying => State == GameState.Playing;
        public int DrivesCollected { get; private set; }
        public int DrivesRequired => drivesRequired;
        public bool HasAllDrives => DrivesCollected >= drivesRequired;
        public DeathCause LastDeathCause { get; private set; }
        public float ElapsedTime { get; private set; }

        // Estatísticas do rank (#32, Proposta 3). Tempo e EMPs contam desde o último reinício; mortes, a partida toda.
        public float AverageSanity => sanityTime > 0f ? sanitySum / sanityTime : 1f;   // 0 a 1
        public int EmpsUsed { get; private set; }
        public int Deaths => RunStats.Deaths;

        // Calculados na vitória.
        public float RankScore { get; private set; }
        public Rank FinalRank { get; private set; }
        public bool IsNewBestRank { get; private set; }

        public event Action<int> DriveCollected;
        public event Action<DeathCause> GameOverTriggered;
        public event Action VictoryTriggered;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Sanity.SanityController sanity;
        private Flashlight.FlashlightController flashlight;
        private float sanitySum;
        private float sanityTime;

        private const string BestRankKey = "Sombra.BestRank.";

        // Melhor rank já tirado na cena, ou null. Salvo em PlayerPrefs (Proposta 3: incentivar a rejogar).
        public static Rank? BestRank(string scene)
        {
            int value = PlayerPrefs.GetInt(BestRankKey + scene, -1);
            return value < 0 ? (Rank?)null : (Rank)value;
        }

        public static void SaveBestRank(string scene, Rank rank)
        {
            PlayerPrefs.SetInt(BestRankKey + scene, (int)rank);
            PlayerPrefs.Save();
        }

        public static void ClearBestRank(string scene) => PlayerPrefs.DeleteKey(BestRankKey + scene);

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

        private void Start()
        {
            sanity = FindAnyObjectByType<Sanity.SanityController>();
            flashlight = FindAnyObjectByType<Flashlight.FlashlightController>();
            if (flashlight != null) flashlight.EmpFired += OnEmpFired;
        }

        private void OnDestroy()
        {
            if (flashlight != null) flashlight.EmpFired -= OnEmpFired;
            if (Instance == this) Instance = null;
        }

        private void OnEmpFired()
        {
            if (IsPlaying) EmpsUsed++;
        }

        private void Update()
        {
            if (IsPlaying)
            {
                ElapsedTime += Time.deltaTime;
                // Média no tempo, não por frame: a taxa de quadros não muda a nota.
                if (sanity != null && sanity.isActiveAndEnabled)
                {
                    sanitySum += sanity.Ratio * Time.deltaTime;
                    sanityTime += Time.deltaTime;
                }
                return;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
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
            RunStats.AddDeath();
            Time.timeScale = 0f;
            GameOverTriggered?.Invoke(cause);
        }

        public void TriggerVictory()
        {
            if (!IsPlaying || !HasAllDrives) return;
            State = GameState.Victory;
            Time.timeScale = 0f;
            ScoreRank();
            VictoryTriggered?.Invoke();
        }

        private void ScoreRank()
        {
            var config = RankConfig.Load();
            RankScore = config.Score(ElapsedTime, drivesRequired, AverageSanity, EmpsUsed, Deaths);
            FinalRank = config.RankFor(RankScore);
            string scene = SceneManager.GetActiveScene().name;
            var best = BestRank(scene);
            IsNewBestRank = best == null || FinalRank > best.Value;
            if (IsNewBestRank) SaveBestRank(scene, FinalRank);
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
                body = $"Rank {FinalRank}   HDs: {DrivesCollected}/{drivesRequired}   Tempo: {FormatTime(ElapsedTime)}";
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
