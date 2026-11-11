using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.Core
{
    public enum GameState { Playing, GameOver, Victory }

    public enum DeathCause { Monster, Sanity, Purge }

    // Estado da partida, HDs coletados e condições de vitória/derrota (GDD 4.4).
    // Um por cena. As telas de fim ficam em UI.ResultScreen (issue #64).
    // Ordem cedo: o perfil da ala (#30) e os HDs anteriores (#45) ficam prontos antes do Awake dos outros sistemas.
    [DefaultExecutionOrder(-400)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int drivesRequired = 3;

        [Tooltip("Dificuldade desta ala (Proposta 1). Vazio: os sistemas usam os próprios configs.")]
        [SerializeField] private DifficultyProfile difficulty;

        public DifficultyProfile ActiveProfile => difficulty;

        public event Action<DifficultyProfile> ProfileChanged;

        [Tooltip("HDs entregues em alas anteriores quando não há checkpoint (cena aberta direto). " +
                 "Com um checkpoint que aponta para esta ala, vale o número salvo.")]
        [SerializeField] private int priorDrives;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsPlaying => State == GameState.Playing;
        // HDs desta ala (porta, HUD e velocidade do monstro, que já sobe de ala em ala pelo perfil).
        public int DrivesCollected { get; private set; }
        // HDs entregues em alas anteriores (checkpoint #45).
        public int PriorDrives { get; private set; }
        // HDs da partida inteira: escolhe o log da Dra. Vance, a escolha de upgrade e o skill check (HD-01, 02, 03).
        public int TotalDrives => PriorDrives + DrivesCollected;
        // HDs da partida ao fim desta ala (HUD e tela de resultado mostram a partida inteira).
        public int TotalDrivesRequired => PriorDrives + drivesRequired;
        public int DrivesRequired => drivesRequired;
        public bool HasAllDrives => DrivesCollected >= drivesRequired;
        public DeathCause LastDeathCause { get; private set; }
        public float ElapsedTime { get; private set; }

        // Estatísticas do rank (#32, Proposta 3). EmpsUsed e ElapsedTime contam desde o último reinício da cena;
        // os Run* somam as alas anteriores trazidas pelo checkpoint (#45). Mortes valem a partida toda.
        public int EmpsUsed { get; private set; }
        public int Deaths => RunStats.Deaths;
        public float RunTime => RunStats.CarriedSeconds + ElapsedTime;
        public int RunEmps => RunStats.CarriedEmps + EmpsUsed;
        public float SanitySum => RunStats.CarriedSanitySum + sanitySum;
        public float SanitySeconds => RunStats.CarriedSanitySeconds + sanityTime;
        public float AverageSanity => SanitySeconds > 0f ? SanitySum / SanitySeconds : 1f;   // 0 a 1

        // Falhas no multímetro da partida: alas anteriores mais os servidores desta cena. Percorre a cena: não chamar por frame.
        public int CountSkillCheckFailures()
        {
            int total = RunStats.CarriedSkillCheckFailures;
            foreach (var server in FindObjectsByType<Interaction.DriveServer>(FindObjectsSortMode.None))
                total += server.SkillCheckFailures;
            return total;
        }

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
            PriorDrives = LoadPriorDrives();
        }

        private int LoadPriorDrives()
        {
            // A ala vem do perfil ou, sem perfil (Ala Médica), do WingCheckpoint da cena.
            string wing = difficulty != null ? difficulty.wing : FindAnyObjectByType<Level.WingCheckpoint>()?.Wing;
            if (!string.IsNullOrEmpty(wing) && Checkpoint.TryLoad(out var data) && data.nextWing == wing)
                return Mathf.Max(0, data.drives);
            return Mathf.Max(0, priorDrives);
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
            // Tempo ideal pelos HDs da partida inteira: as alas anteriores também contam.
            RankScore = config.Score(RunTime, TotalDrivesRequired, AverageSanity, RunEmps, Deaths);
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
                body = $"Rank {FinalRank}   HDs: {DrivesCollected}/{drivesRequired}   Tempo: {FormatTime(RunTime)}";
                if (wing != null && wing.StatusLine.Length > 0) body += "   " + wing.StatusLine;
            }
            else
            {
                title = "FIM DA LINHA";
                body = LastDeathCause switch
                {
                    DeathCause.Monster => "O Espécime Vazio te alcançou.",
                    DeathCause.Purge => "A purga da eclusa te alcançou.",
                    _ => "Sua mente colapsou no escuro."
                };
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
