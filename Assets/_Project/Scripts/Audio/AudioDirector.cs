using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.Audio
{
    // Mistura do jogo por estado (#101, docs/audio/sound-design.md seções 4 e 6). Criado em runtime em toda cena
    // com GameManager, como o TensionDirector. Decide:
    // - a intensidade da MusicLayers da cena: Zona Segura ou cutscene = 0, exploração = 1, tensão ou monstro
    //   investigando = 2, perseguição com o monstro na tela = 3. Monstro pelas costas (fora da tela) não sobe
    //   para 3: a música não entrega a posição (regra do Alien: Isolation);
    // - o ducking: Ambiente baixa na perseguição, Música baixa enquanto a Vance fala.
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        // Ligado pelo CutscenePlayer (#88) enquanto uma cutscene toca.
        public static bool CutsceneActive { get; set; }
        // Faixa fixa pedida pela cutscene (memória, revelação, finais). null: só abaixa a música da fase.
        public static AudioClip CutsceneTrack { get; set; }

        private AudioConfig config;
        private MusicLayers music;
        private SanityController sanity;
        private MonsterController monster;
        private SleepwalkerController sleepwalker;
        private Camera cam;
        private float ambienceDuck, musicDuck;

        public int TargetIntensity { get; private set; } = 1;
        public bool Chasing { get; private set; }

        // Faixa fixa tocando por cima (ou no lugar) da música da fase (#103): sala segura, purga, game over, laudo.
        public AudioClip FixedTrack => fixedSource != null ? fixedSource.clip : null;
        public float FixedVolume => fixedSource != null ? fixedSource.volume : 0f;
        public AudioSource FixedSource => fixedSource;

        private AudioSource fixedSource;
        private AudioClip pendingClip;
        private GameManager gm;
        private float victoryAt = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= EnsureDirector;
            SceneManager.sceneLoaded += EnsureDirector;
        }

        private static void EnsureDirector(Scene scene, LoadSceneMode mode)
        {
            AudioService.ApplyAll();
            if (FindAnyObjectByType<AudioDirector>() != null) return;
            if (FindAnyObjectByType<GameManager>() == null) return;
            new GameObject("AudioDirector").AddComponent<AudioDirector>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            config = AudioConfig.Load();
        }

        private void OnDestroy()
        {
            if (gm != null)
            {
                gm.GameOverTriggered -= OnGameOver;
                gm.VictoryTriggered -= OnVictory;
            }
            if (Instance != this) return;
            Instance = null;
            AudioService.SetDuck(AudioBus.Ambience, 0f);
            AudioService.SetDuck(AudioBus.Music, 0f);
        }

        private void Start()
        {
            music = FindAnyObjectByType<MusicLayers>();
            if (music == null) music = CreateWingMusic();
            sanity = FindAnyObjectByType<SanityController>();
            monster = FindAnyObjectByType<MonsterController>();
            sleepwalker = FindAnyObjectByType<SleepwalkerController>();
            cam = Camera.main;
            gm = GameManager.Instance;
            if (gm != null)
            {
                gm.GameOverTriggered += OnGameOver;
                gm.VictoryTriggered += OnVictory;
            }

            fixedSource = gameObject.AddComponent<AudioSource>();
            fixedSource.playOnAwake = false;
            fixedSource.spatialBlend = 0f;
            fixedSource.ignoreListenerPause = true;
            AudioService.Route(fixedSource, AudioBus.Music);
        }

        // Nenhuma cena de ala tem MusicLayers: a música da fase (#104) é criada aqui, pela ala do WingCheckpoint
        // (ou pelo nome da cena), com o MusicSet ligado no AudioConfig. Cena sem ala (Alfa, sandboxes): sem música.
        private MusicLayers CreateWingMusic()
        {
            var checkpoint = Level.WingCheckpoint.Instance;
            string wing = checkpoint != null ? checkpoint.Wing : WingFlow.WingForScene(gameObject.scene.name);
            var set = config.WingMusic(wing);
            if (set == null || set.Count == 0) return null;
            var layers = new GameObject("WingMusic (" + wing + ")").AddComponent<MusicLayers>();
            layers.SetMusic(set);
            return layers;
        }

        // Game Over é corte seco (sound-design.md seção 4): sem fade, a música da fase some no mesmo quadro.
        private void OnGameOver(DeathCause cause)
        {
            if (music != null) music.Cut();
            if (fixedSource == null || config.gameOverSting == null) return;
            pendingClip = null;
            fixedSource.Stop();
            fixedSource.clip = config.gameOverSting;
            fixedSource.loop = false;
            fixedSource.volume = 1f;
            fixedSource.Play();
        }

        private void OnVictory() => victoryAt = Time.unscaledTime;

        // Vitória da última fase: o jingle de vitória toca sozinho, depois entra o laudo.
        private bool FinalVictory
        {
            get
            {
                if (gm == null || gm.State != GameState.Victory) return false;
                var wing = Level.WingCheckpoint.Instance;
                return wing == null || wing.IsFinal;
            }
        }

        // Qual faixa fixa deveria tocar agora. null: só a música da fase.
        private AudioClip DesiredFixed(out bool loop)
        {
            loop = true;
            if (gm != null && gm.State == GameState.GameOver) { loop = false; return config.gameOverSting; }
            if (FinalVictory)
                return victoryAt >= 0f && Time.unscaledTime - victoryAt < config.laudoDelay ? null : config.laudoMusic;
            if (CutsceneActive && CutsceneTrack != null) return CutsceneTrack;
            var escape = Level.EscapeSequence.Instance;
            if (escape != null && escape.Active) { loop = false; return config.purgeMusic; }
            if (sanity != null && sanity.InSafeZone && !CutsceneActive) return config.safeRoomMusic;
            return null;
        }

        private void UpdateFixedTrack()
        {
            if (fixedSource == null) return;
            var desired = DesiredFixed(out bool loop);
            float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, config.fixedFade);

            if (desired != fixedSource.clip || !fixedSource.isPlaying && desired != null && loop)
            {
                // O sting do Game Over não volta a tocar depois de terminar.
                if (desired != null && desired == config.gameOverSting && fixedSource.clip == desired) return;
                pendingClip = desired;
                fixedSource.volume = Mathf.MoveTowards(fixedSource.volume, 0f, step);
                if (fixedSource.volume > 0f && fixedSource.isPlaying) return;
                fixedSource.Stop();
                fixedSource.clip = pendingClip;
                fixedSource.loop = loop;
                if (pendingClip != null) fixedSource.Play();
            }
            else if (desired != null)
            {
                fixedSource.volume = Mathf.MoveTowards(fixedSource.volume, 1f, step);
            }

            // A purga é o cronômetro: a música acompanha o tempo real da sequência (inclusive SetRemaining).
            var escape = Level.EscapeSequence.Instance;
            if (desired != null && desired == config.purgeMusic && escape != null && fixedSource.isPlaying
                && Mathf.Abs(fixedSource.time - escape.Elapsed) > 0.5f && escape.Elapsed < desired.length)
                fixedSource.time = escape.Elapsed;

            // Faixa que substitui a música da fase (purga, game over, laudo) silencia as camadas; a da sala segura
            // toca por cima do L0 (o AudioDirector já põe a intensidade em 0 na Zona Segura).
            // Na vitória final a música da fase também fica muda durante o jingle.
            if (music != null) music.Muted = desired != null && desired != config.safeRoomMusic || FinalVictory;
        }

        private void Update()
        {
            bool monsterChasing = monster != null && monster.isActiveAndEnabled && monster.State == MonsterState.Chase;
            bool sleeperMoving = sleepwalker != null && sleepwalker.isActiveAndEnabled && sleepwalker.State == SleepwalkerState.Moving;
            Chasing = monsterChasing || sleeperMoving;

            TargetIntensity = ComputeIntensity(monsterChasing, sleeperMoving);
            if (music != null) music.SetIntensity(TargetIntensity);
            UpdateFixedTrack();

            // Ducking suave em dB.
            float speed = 80f / Mathf.Max(0.01f, config.duckFade) * Time.unscaledDeltaTime;
            ambienceDuck = Mathf.MoveTowards(ambienceDuck, Chasing ? config.chaseAmbienceDuckDb : 0f, speed);
            bool voice = AudioLogPlayer.Instance != null && AudioLogPlayer.Instance.IsPlaying;
            musicDuck = Mathf.MoveTowards(musicDuck, voice ? config.voiceMusicDuckDb : 0f, speed);
            AudioService.SetDuck(AudioBus.Ambience, ambienceDuck);
            AudioService.SetDuck(AudioBus.Music, musicDuck);
        }

        private int ComputeIntensity(bool monsterChasing, bool sleeperMoving)
        {
            // Fim da partida (tela "Ala concluída"): só o ambiente, mesmo se o monstro estava perseguindo.
            if (gm != null && gm.State != GameState.Playing) return 0;
            if (CutsceneActive || (sanity != null && sanity.InSafeZone)) return 0;
            bool onScreen = (monsterChasing && OnScreen(monster.transform.position))
                            || (sleeperMoving && OnScreen(sleepwalker.transform.position));
            if ((monsterChasing || sleeperMoving) && onScreen) return 3;
            float tension = TensionDirector.Instance != null ? TensionDirector.Instance.Value : 0f;
            bool investigating = monster != null && monster.State == MonsterState.Investigate;
            if (monsterChasing || sleeperMoving || investigating || tension >= config.pulseTension) return 2;
            return 1;
        }

        private bool OnScreen(Vector3 world)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return true;
            var v = cam.WorldToViewportPoint(world);
            return v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            CutsceneActive = false;
            CutsceneTrack = null;
        }
    }
}
