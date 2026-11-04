using System;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace SombraDaSanidade.Core
{
    public enum TensionPhase { BuildUp, SustainPeak, PeakFade, Relax }

    // Diretor de tensão (#35, Frente 2). É o "dente-de-serra" do GDD 7 virando código:
    // um medidor de 0 a 1 que sobe quando o monstro está perto, iluminado ou perseguindo, e decai com o tempo.
    // No pico ele segura de 3 a 5 s, espera a perseguição acabar sozinha e manda o monstro patrulhar
    // longe do jogador por 30 a 45 s. Depois recomeça.
    //
    // Como o Director do Alien: Isolation, ele sabe onde o jogador está, mas só dá DIREÇÃO ao monstro
    // (MonsterController.SetRetreat). O monstro continua cego e só acha o jogador pela luz e pelo ruído.
    // A tensão nunca aparece no HUD (GDD 8). Se a cena não tiver um diretor, ele é criado sozinho.
    public class TensionDirector : MonoBehaviour
    {
        [SerializeField] private TensionConfig config;
        [Tooltip("Vazios: procura na cena.")]
        [SerializeField] private MonsterController monster;
        [SerializeField] private PlayerController player;
        [Tooltip("Camadas que contam como parede para a proximidade 'em outro cômodo'.")]
        [SerializeField] private LayerMask walls;

        public static TensionDirector Instance { get; private set; }

        public float Value { get; private set; }
        public TensionPhase Phase { get; private set; } = TensionPhase.BuildUp;

        public event Action<TensionPhase> PhaseChanged;

        private float phaseTimer;
        private float retreatTimer;

        // Cena sem diretor (protótipo, Alfa, alas): cria um com os valores padrão do TensionConfig.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= EnsureDirector;
            SceneManager.sceneLoaded += EnsureDirector;
        }

        private static void EnsureDirector(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<TensionDirector>() != null) return;
            if (FindAnyObjectByType<MonsterController>() == null) return;
            new GameObject("TensionDirector").AddComponent<TensionDirector>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (walls == 0) walls = LayerMask.GetMask("Walls");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (monster == null) monster = FindAnyObjectByType<MonsterController>();
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            // Sem config na cena: usa o do MonsterConfig (assim o asset vale em todas as cenas), senão o padrão.
            if (config == null && monster != null && monster.Config != null) config = monster.Config.tension;
            if (config == null) config = ScriptableObject.CreateInstance<TensionConfig>();
        }

        // Para a Frente 1: chamar quando o jogador errar o skill check do HD (#33).
        public void ReportSkillCheckFailure()
        {
            if (config != null) AddSpike(config.skillCheckFailGain);
        }

        public void AddSpike(float amount)
        {
            if (Phase != TensionPhase.BuildUp || config == null) return;
            Value = Mathf.Clamp01(Value + amount);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (monster == null || player == null || config == null || !monster.isActiveAndEnabled) return;

            float dt = Time.deltaTime;
            bool chasing = monster.State == MonsterState.Chase;

            switch (Phase)
            {
                case TensionPhase.BuildUp:
                    Value += Gain(chasing) * dt;
                    if (!chasing) Value -= config.decayPerSecond * dt;
                    Value = Mathf.Clamp01(Value);
                    if (Value >= 1f)
                    {
                        phaseTimer = RandomIn(config.sustainPeakSeconds);
                        SetPhase(TensionPhase.SustainPeak);
                    }
                    break;

                case TensionPhase.SustainPeak:
                    Value = 1f;
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) SetPhase(TensionPhase.PeakFade);
                    break;

                case TensionPhase.PeakFade:
                    // Espera a ação acabar naturalmente: sem tirar o monstro do meio de uma perseguição.
                    if (!chasing && !monster.IsDetecting)
                    {
                        phaseTimer = RandomIn(config.relaxSeconds);
                        retreatTimer = config.retreatRefreshSeconds;
                        monster.SetRetreat(player.transform.position);
                        SetPhase(TensionPhase.Relax);
                    }
                    break;

                case TensionPhase.Relax:
                    Value = Mathf.Max(0f, Value - config.relaxDecayPerSecond * dt);
                    retreatTimer -= dt;
                    if (retreatTimer <= 0f)
                    {
                        // O jogador andou: o "longe" muda, mas sem interromper o trajeto atual.
                        retreatTimer = config.retreatRefreshSeconds;
                        monster.SetRetreat(player.transform.position, redirectNow: false);
                    }
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                    {
                        monster.SetRetreat(null);
                        SetPhase(TensionPhase.BuildUp);
                    }
                    break;
            }
        }

        private float Gain(bool chasing)
        {
            float gain = 0f;
            Vector2 p = player.transform.position;
            Vector2 m = monster.transform.position;
            float d = Vector2.Distance(p, m);
            if (d < config.proximityRadius)
            {
                float g = config.proximityGain * (1f - d / config.proximityRadius);
                if (Physics2D.Linecast(p, m, walls)) g *= config.outOfSightFactor;
                gain += g;
            }
            if (monster.IsLitByFlashlight()) gain += config.litGain;
            if (chasing) gain += config.chaseGain;
            return gain;
        }

        private void SetPhase(TensionPhase next)
        {
            if (Phase == next) return;
            Phase = next;
            PhaseChanged?.Invoke(next);
        }

        private static float RandomIn(Vector2 range) => Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    }
}
