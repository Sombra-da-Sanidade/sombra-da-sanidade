using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.Audio
{
    // Liga os eventos dos sistemas aos sons do GameSfx (#105). Criado em runtime em toda cena com GameManager,
    // como o AudioDirector: nenhuma cena ou prefab de outra frente precisa mudar.
    // - Thomas: passos pela distância andada, no piso da ala (corrida com Adrena-Syn).
    // - Lux-90: liga, desliga, zumbido aceso, bateria fraca, esgotada, EMP e EMP falho.
    // - Espécime: respiração em loop com oclusão, passos pela distância, estalo ao investigar, grito na
    //   perseguição, atordoado, ataque.
    // - Sonâmbulo: rangido só enquanto se move (corta em seco ao congelar), respiração parado, despertar, ataque.
    // - Multímetro: aviso, ticks do ponteiro, bom, ótimo, falha.
    // - Mundo e itens (#106): disjuntor (liga, desliga, queima), HD (ejeção, coleta), armário, pilha, sinalizador
    //   (pegar e acender), injeção (Adrena-Syn, Estabilizador, sedativo), teclado da senha, subestação,
    //   escolha de upgrade e "progresso salvo" na vitória de uma ala. Enquanto o jogador segura E, um loop
    //   acompanha a ação (consertar o disjuntor, forçar o armário, abrir a caixa de ferramentas, religar a
    //   subestação, ejetar o HD)
    //   e para ao soltar ou andar.
    // Nada aqui emite ruído no NoiseBus (balanceamento do monstro inalterado).
    public class GameSfxHooks : MonoBehaviour
    {
        public static GameSfxHooks Instance { get; private set; }

        private GameSfx sfx;
        private AudioConfig config;
        private PlayerController player;
        private FlashlightController flashlight;
        private AdrenaSynEffect adrena;
        private SkillCheck skillCheck;
        private AudioSource hum;
        private Vector2 lastPlayerPos;
        private float playerWalked, nextBatteryLow;
        private int lastTick = -1;
        private bool adrenaWasActive;
        private FlareThrower flares;
        private int lastFlares = -1;
        private PlayerInteractor interactor;
        private AudioSource channelLoop;
        private SoundEvent channelSound;
        // Fade de entrada e saída do loop de canalização (por segundo): evita estalo ao soltar o E.
        private const float ChannelFade = 10f;

        public SoundEvent ChannelSound => channelSound;
        public float ChannelVolume => channelLoop != null ? channelLoop.volume : 0f;
        private readonly List<System.Action> unhook = new List<System.Action>();
        private readonly List<KeypadLock> keypads = new List<KeypadLock>();
        private class Flare { public SignalFlare f; public AudioOcclusion burn; }
        private readonly List<Flare> flaresBurning = new List<Flare>();
        // Segundos finais em que o chiado do sinalizador vai sumindo junto com a luz.
        private const float FlareFadeSeconds = 1.5f;
        private readonly List<int> keypadEntry = new List<int>();

        private class Specimen { public MonsterController m; public AudioOcclusion breath; public Vector2 last; public float walked; }
        private class Sleeper { public SleepwalkerController s; public AudioOcclusion creak, breath; }
        private class Watcher { public SecurityCamera c; public AudioOcclusion servo; }

        private readonly List<Specimen> specimens = new List<Specimen>();
        private readonly List<Sleeper> sleepers = new List<Sleeper>();
        private readonly List<Watcher> watchers = new List<Watcher>();

        public int SpecimenCount => specimens.Count;
        public int SleeperCount => sleepers.Count;
        public AudioSource FlashlightHum => hum;
        public AudioOcclusion SleeperCreak(int i) => i < sleepers.Count ? sleepers[i].creak : null;
        public AudioOcclusion SpecimenBreath(int i) => i < specimens.Count ? specimens[i].breath : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= Ensure;
            SceneManager.sceneLoaded += Ensure;
        }

        private static void Ensure(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<GameSfxHooks>() != null || FindAnyObjectByType<GameManager>() == null) return;
            if (GameSfx.Load() == null) return;
            new GameObject("GameSfxHooks").AddComponent<GameSfxHooks>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            sfx = GameSfx.Load();
            config = AudioConfig.Load();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                lastPlayerPos = player.transform.position;
            }

            flashlight = FindAnyObjectByType<FlashlightController>();
            if (flashlight != null)
            {
                flashlight.LitChanged += OnLitChanged;
                flashlight.EmpFired += OnEmpFired;
                flashlight.EmpFailed += OnEmpFailed;
                if (sfx.flashlightHum != null && sfx.flashlightHum.HasClips)
                {
                    hum = flashlight.gameObject.AddComponent<AudioSource>();
                    hum.clip = sfx.flashlightHum.clips[0];
                    hum.loop = true;
                    hum.playOnAwake = false;
                    hum.spatialBlend = 0f;
                    hum.volume = 0f;
                    AudioService.Route(hum, sfx.flashlightHum.bus);
                    hum.Play();
                }
            }

            foreach (var m in FindObjectsByType<MonsterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var entry = new Specimen { m = m, last = m.transform.position };
                entry.breath = Loop(m.gameObject, sfx.specimenBreath, config.specimenBreathVolume.x);
                m.StateChanged += state => OnSpecimenState(entry, state);
                m.Caught += _ => Play(sfx.specimenAttack, m.transform.position);
                specimens.Add(entry);
            }

            flares = FindAnyObjectByType<FlareThrower>();
            interactor = FindAnyObjectByType<PlayerInteractor>();
            HookWorld();

            foreach (var s in FindObjectsByType<SleepwalkerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var entry = new Sleeper { s = s };
                entry.creak = Loop(s.gameObject, sfx.sleepwalkerCreak, 0f);
                entry.breath = Loop(s.gameObject, sfx.sleepwalkerBreath, 0f);
                s.Woke += () => Play(sfx.sleepwalkerWake, s.transform.position);
                s.Caught += _ => Play(sfx.sleepwalkerAttack, s.transform.position);
                sleepers.Add(entry);
            }
        }

        private void OnDestroy()
        {
            if (flashlight != null)
            {
                flashlight.LitChanged -= OnLitChanged;
                flashlight.EmpFired -= OnEmpFired;
                flashlight.EmpFailed -= OnEmpFailed;
            }
            UnhookSkillCheck();
            foreach (var u in unhook) u();
            unhook.Clear();
            if (Instance == this) Instance = null;
        }

        private static AudioOcclusion Loop(GameObject owner, SoundEvent sound, float volume)
        {
            if (sound == null || !sound.HasClips) return null;
            var occlusion = owner.AddComponent<AudioOcclusion>();
            occlusion.Setup(sound, volume);
            return occlusion;
        }

        private static void Play(SoundEvent sound, Vector2? position) => AudioService.Play(sound, position);

        private void Update()
        {
            // Fim da partida: só o som de morte ou de vitória. Os loops (respiração do monstro, rangido, servo,
            // zumbido da lanterna, segurar E) continuavam tocando por baixo da tela de fim.
            var manager = GameManager.Instance;
            if (manager != null && !manager.IsPlaying)
            {
                SilenceLoops();
                return;
            }
            UpdateFootsteps();
            UpdateFlashlight();
            UpdateSpecimens();
            UpdateSleepers();
            UpdateCameras();
            UpdateSkillCheck();
            UpdateItems();
            UpdateChannel();
        }

        private void SilenceLoops()
        {
            foreach (var e in specimens) if (e.breath != null) e.breath.Volume = 0f;
            foreach (var e in sleepers)
            {
                if (e.creak != null) e.creak.Volume = 0f;
                if (e.breath != null) e.breath.Volume = 0f;
            }
            foreach (var w in watchers) if (w.servo != null) w.servo.Volume = 0f;
            foreach (var e in flaresBurning) if (e.burn != null) e.burn.Volume = 0f;
            if (hum != null) hum.volume = 0f;
            if (channelLoop != null && channelLoop.isPlaying)
            {
                channelLoop.Stop();
                channelSound = null;
            }
        }

        // ---------- Thomas ----------

        private void UpdateFootsteps()
        {
            if (player == null) return;
            Vector2 pos = player.transform.position;
            float moved = Vector2.Distance(pos, lastPlayerPos);
            lastPlayerPos = pos;
            if (moved > 2f) { playerWalked = 0f; return; }   // teleporte (checkpoint, cutscene) não é passo
            playerWalked += moved;
            if (playerWalked < config.playerStepDistance) return;
            playerWalked = 0f;
            // O efeito só é adicionado ao jogador na primeira injeção (AdrenaSynEffect.On).
            if (adrena == null) adrena = player.GetComponent<AdrenaSynEffect>();
            bool running = adrena != null && adrena.IsActive;
            var profile = Core.DifficultyProfile.Active;
            Play(sfx.Footstep(profile != null ? profile.floor : Core.FloorType.Tile, running), pos);
        }

        // ---------- Lux-90 ----------

        private void OnLitChanged(bool lit)
        {
            if (lit) Play(sfx.flashlightOn, flashlight.transform.position);
            else Play(flashlight.Battery <= 0f ? sfx.batteryDead : sfx.flashlightOff, flashlight.transform.position);
        }

        private void OnEmpFired() => Play(sfx.empFire, flashlight.transform.position);
        private void OnEmpFailed(EmpFailReason reason) => Play(sfx.empFail, flashlight.transform.position);

        private void UpdateFlashlight()
        {
            if (flashlight == null) return;
            if (hum != null) hum.volume = flashlight.IsLit ? config.flashlightHumVolume : 0f;
            if (flashlight.IsLit && flashlight.BatteryRatio < config.batteryLowRatio && Time.time >= nextBatteryLow)
            {
                nextBatteryLow = Time.time + config.batteryLowInterval;
                Play(sfx.batteryLow, flashlight.transform.position);
            }
        }

        // ---------- Espécime ----------

        private void OnSpecimenState(Specimen e, MonsterState state)
        {
            Vector2 at = e.m.transform.position;
            switch (state)
            {
                case MonsterState.Investigate: Play(sfx.specimenHear, at); break;
                case MonsterState.Chase: Play(sfx.specimenScream, at); break;
                case MonsterState.Stunned: Play(sfx.specimenStunned, at); break;
            }
        }

        private void UpdateSpecimens()
        {
            foreach (var e in specimens)
            {
                if (e.m == null || !e.m.isActiveAndEnabled) continue;
                if (e.breath != null)
                    e.breath.Volume = e.m.IsStunned ? 0f
                        : e.m.State == MonsterState.Chase ? config.specimenBreathVolume.y : config.specimenBreathVolume.x;

                Vector2 pos = e.m.transform.position;
                float moved = Vector2.Distance(pos, e.last);
                e.last = pos;
                if (moved > 2f) { e.walked = 0f; continue; }
                e.walked += moved;
                if (e.walked < config.specimenStepDistance) continue;
                e.walked = 0f;
                Play(sfx.specimenStep, pos);
            }
        }

        // ---------- Sonâmbulo ----------

        private void UpdateSleepers()
        {
            foreach (var e in sleepers)
            {
                if (e.s == null) continue;
                bool active = e.s.isActiveAndEnabled;
                // Só se move no escuro: o som corta no mesmo quadro em que a luz o congela (sound-design.md 5).
                if (e.creak != null) e.creak.Volume = active && e.s.State == SleepwalkerState.Moving ? 1f : 0f;
                if (e.breath != null) e.breath.Volume = active && e.s.State == SleepwalkerState.Frozen ? 0.8f : 0f;
            }
        }

        // ---------- Câmeras de segurança (#79) ----------

        private void OnCameraState(SecurityCamera c, SecurityCameraState state)
        {
            Vector2 at = c.transform.position;
            switch (state)
            {
                case SecurityCameraState.Detecting: Play(sfx.cameraDetect, at); break;
                case SecurityCameraState.Alarm: Play(sfx.cameraAlarm, at); Play(sfx.cameraBlackout, at); break;
                case SecurityCameraState.Disabled:
                case SecurityCameraState.Off: Play(sfx.cameraShort, at); break;
            }
        }

        // O motor só soa enquanto a câmera gira.
        private void UpdateCameras()
        {
            // Registra na primeira vez que a câmera aparece (cena carregada ou instanciada depois).
            foreach (var c in SecurityCamera.All)
            {
                if (watchers.Exists(w => w.c == c)) continue;
                watchers.Add(new Watcher { c = c, servo = Loop(c.gameObject, sfx.cameraServo, 0f) });
                c.StateChanged += state => OnCameraState(c, state);
            }
            foreach (var w in watchers)
            {
                if (w.c == null || w.servo == null) continue;
                bool moving = w.c.isActiveAndEnabled
                    && (w.c.State == SecurityCameraState.Sweeping || w.c.State == SecurityCameraState.Detecting);
                w.servo.Volume = moving ? 0.6f : 0f;
            }
        }

        // ---------- Mundo e itens (#106) ----------

        private void HookWorld()
        {
            foreach (var b in FindObjectsByType<Breaker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var breaker = b;
                System.Action<bool> state = on => Play(on ? sfx.breakerOn : sfx.breakerOff, breaker.transform.position);
                System.Action burnt = () => Play(sfx.breakerBurnt, breaker.transform.position);
                breaker.StateChanged += state;
                breaker.Burnt += burnt;
                unhook.Add(() => { if (breaker != null) { breaker.StateChanged -= state; breaker.Burnt -= burnt; } });
            }
            foreach (var d in FindObjectsByType<DriveServer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var server = d;
                System.Action<DriveServer> ejected = s => Play(sfx.driveEject, s.transform.position);
                server.Ejected += ejected;
                unhook.Add(() => { if (server != null) server.Ejected -= ejected; });
            }
            foreach (var l in FindObjectsByType<Locker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var locker = l;
                System.Action<Locker, LootKind> opened = OnLockerOpened;
                locker.Opened += opened;
                unhook.Add(() => { if (locker != null) locker.Opened -= opened; });
            }
            foreach (var k in FindObjectsByType<KeypadLock>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var keypad = k;
                keypads.Add(keypad);
                keypadEntry.Add(keypad.Entry.Length);
                System.Action<KeypadLock> ok = x => Play(sfx.keypadOk, x.transform.position);
                System.Action<KeypadLock> wrong = x => Play(sfx.keypadWrong, x.transform.position);
                keypad.Unlocked += ok;
                keypad.WrongCode += wrong;
                unhook.Add(() => { if (keypad != null) { keypad.Unlocked -= ok; keypad.WrongCode -= wrong; } });
            }
            foreach (var st in FindObjectsByType<Substation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var substation = st;
                System.Action powered = () => Play(sfx.substationOn, substation.transform.position);
                substation.Powered += powered;
                unhook.Add(() => { if (substation != null) substation.Powered -= powered; });
            }
            foreach (var t in FindObjectsByType<UI.UpgradeTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var terminal = t;
                System.Action<LuxUpgrade> chosen = _ => Play(sfx.uiUpgrade, null);
                terminal.Chosen += chosen;
                unhook.Add(() => { if (terminal != null) terminal.Chosen -= chosen; });
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                System.Action<int> collected = _ => Play(sfx.driveCollect, null);
                System.Action victory = OnVictory;
                gm.DriveCollected += collected;
                gm.VictoryTriggered += victory;
                unhook.Add(() => { if (gm != null) { gm.DriveCollected -= collected; gm.VictoryTriggered -= victory; } });
            }

            foreach (var et in FindObjectsByType<EclusaTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var eclusa = et;
                System.Action<EclusaTerminal> inserted = x => Play(sfx.eclusaOn, x.transform.position);
                eclusa.ActivatedEvent += inserted;
                unhook.Add(() => { if (eclusa != null) eclusa.ActivatedEvent -= inserted; });
            }

            System.Action<Vector2> neural = at => Play(sfx.neuralModule, at);
            Sanity.NeuralModule.Applied += neural;
            unhook.Add(() => Sanity.NeuralModule.Applied -= neural);

            System.Action<Vector2> dose = at => Play(sfx.injection, at);
            Sanity.LithiumStabilizer.Applied += dose;
            unhook.Add(() => Sanity.LithiumStabilizer.Applied -= dose);
        }

        // Armário: a porta sempre; a pilha tem o som de pegar. O sedativo toca a injeção pelo Estabilizador e o
        // Adrena-Syn pela ativação do efeito.
        private void OnLockerOpened(Locker locker, LootKind loot)
        {
            Play(sfx.lockerOpen, locker.transform.position);
            if (loot == LootKind.Battery) Play(sfx.itemPickup, locker.transform.position);
        }

        // Ala concluída: o checkpoint é salvo na vitória ("PROGRESSO SALVO" na tela). Na última ala (ou cena sem
        // encadeamento), a fuga: eclusa despressurizando e o sinal de homologação, antes da música do laudo.
        private void OnVictory()
        {
            var wing = Level.WingCheckpoint.Instance;
            if (wing != null && !wing.IsFinal) Play(sfx.uiSaved, null);
            else Play(sfx.victory, null);
        }

        private void UpdateItems()
        {
            if (player == null) return;
            // Adrena-Syn: a injeção toca quando o efeito liga (módulo do mapa ou armário).
            if (adrena == null) adrena = player.GetComponent<AdrenaSynEffect>();
            bool active = adrena != null && adrena.IsActive;
            if (active && !adrenaWasActive) Play(sfx.injection, player.transform.position);
            adrenaWasActive = active;

            // Painel da senha: cada número digitado ou apagado (o visor muda de tamanho). O certo e o errado têm som
            // próprio; a senha errada limpa o visor e não conta como tecla.
            for (int i = 0; i < keypads.Count; i++)
            {
                var keypad = keypads[i];
                if (keypad == null) continue;
                int length = keypad.Entry.Length;
                if (length != keypadEntry[i] && keypad.IsOpen && !keypad.IsUnlocked && (length > 0 || keypadEntry[i] == 1))
                    Play(sfx.keypadKey, keypad.transform.position);
                keypadEntry[i] = length;
            }

            // Sinalizadores: pegar soma, acender (lançar) tira.
            // O lançador é procurado uma vez no Start (pode não estar no objeto do jogador).
            if (flares == null) return;
            int count = flares.Count;
            if (lastFlares >= 0 && count != lastFlares)
            {
                Play(count > lastFlares ? sfx.itemPickup : sfx.flareIgnite, player.transform.position);
                if (count < lastFlares) TrackFlares();
            }
            lastFlares = count;
            UpdateFlareBurn();
        }

        // Loop enquanto o jogador segura E. Some com o progresso pausado (falha no multímetro).
        private void UpdateChannel()
        {
            if (interactor == null) return;
            SoundEvent want = null;
            if (interactor.IsChanneling && !interactor.IsProgressPaused && interactor.Current is Component target)
                want = target is Breaker ? sfx.breakerRepair
                    : target is Locker ? sfx.lockerForce
                    : target is DriveServer ? sfx.driveEjecting
                    : target is FlarePickup pickup && pickup.IsToolbox ? sfx.toolboxOpen
                    : target is Substation ? sfx.substationRepair
                    : target is EclusaTerminal ? sfx.eclusaInsert
                    : null;
            if (want != null && !want.HasClips) want = null;

            if (want != null && want != channelSound)
            {
                if (channelLoop == null)
                {
                    channelLoop = gameObject.AddComponent<AudioSource>();
                    channelLoop.loop = true;
                    channelLoop.playOnAwake = false;
                    channelLoop.spatialBlend = 0f;
                }
                channelSound = want;
                // Armário (1 s) e disjuntor (4 s): um som com começo, meio e fim, do tamanho do segurar E,
                // tocado uma vez (em loop soava mecânico). Só a ejeção do HD (12 s, pausa no multímetro) fica em loop.
                channelLoop.loop = want == sfx.driveEjecting;
                channelLoop.clip = want.clips[Random.Range(0, want.clips.Length)];
                channelLoop.volume = 0f;
                AudioService.Route(channelLoop, want.bus);
                channelLoop.Play();
            }
            if (channelLoop == null || channelSound == null) return;

            float target01 = want != null ? channelSound.volume.y : 0f;
            channelLoop.volume = Mathf.MoveTowards(channelLoop.volume, target01, ChannelFade * Time.unscaledDeltaTime);
            if (want == null && channelLoop.volume <= 0f)
            {
                channelLoop.Stop();
                channelSound = null;
            }
        }

        // Sinalizador lançado: ganha o loop de queima, que toca do chão enquanto ele arde e some com a luz.
        private void TrackFlares()
        {
            if (sfx.flareBurn == null || !sfx.flareBurn.HasClips) return;
            foreach (var f in FindObjectsByType<SignalFlare>(FindObjectsSortMode.None))
            {
                if (f.IsSpent || flaresBurning.Exists(x => x.f == f)) continue;
                flaresBurning.Add(new Flare { f = f, burn = Loop(f.gameObject, sfx.flareBurn, 0f) });
            }
        }

        private void UpdateFlareBurn()
        {
            for (int i = flaresBurning.Count - 1; i >= 0; i--)
            {
                var e = flaresBurning[i];
                if (e.f == null || e.burn == null || e.f.IsSpent) { flaresBurning.RemoveAt(i); continue; }
                e.burn.Volume = e.f.IsBurning
                    ? sfx.flareBurn.volume.y * Mathf.Clamp01(e.f.Remaining / FlareFadeSeconds)
                    : 0f;
            }
        }

        // ---------- Multímetro ----------

        private void UpdateSkillCheck()
        {
            // O SkillCheck pode ser criado depois (DriveServer cria no jogador em cenas antigas).
            if (skillCheck == null)
            {
                skillCheck = FindAnyObjectByType<SkillCheck>();
                if (skillCheck == null) return;
                skillCheck.Warned += OnSkillWarned;
                skillCheck.Resolved += OnSkillResolved;
            }
            if (skillCheck.State != SkillCheckState.Active) { lastTick = -1; return; }
            int tick = Mathf.FloorToInt(skillCheck.NeedlePosition * Mathf.Max(1, config.skillTicksPerSweep));
            if (tick != lastTick && lastTick >= 0) Play(sfx.skillTick, skillCheck.transform.position);
            lastTick = tick;
        }

        private void OnSkillWarned() => Play(sfx.skillWarn, skillCheck.transform.position);

        private void OnSkillResolved(SkillCheckResult result) => Play(result switch
        {
            SkillCheckResult.Great => sfx.skillGreat,
            SkillCheckResult.Good => sfx.skillGood,
            _ => sfx.skillFail
        }, skillCheck.transform.position);

        private void UnhookSkillCheck()
        {
            if (skillCheck == null) return;
            skillCheck.Warned -= OnSkillWarned;
            skillCheck.Resolved -= OnSkillResolved;
        }
    }
}
