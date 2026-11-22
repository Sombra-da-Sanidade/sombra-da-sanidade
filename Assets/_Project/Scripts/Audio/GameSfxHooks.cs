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
    // - Thomas: passos pela distância andada (corrida com Adrena-Syn).
    // - Lux-90: liga, desliga, zumbido aceso, bateria fraca, esgotada, EMP e EMP falho.
    // - Espécime: respiração em loop com oclusão, passos pela distância, estalo ao investigar, grito na
    //   perseguição, atordoado, ataque.
    // - Sonâmbulo: rangido só enquanto se move (corta em seco ao congelar), respiração parado, despertar, ataque.
    // - Multímetro: aviso, ticks do ponteiro, bom, ótimo, falha.
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

        private class Specimen { public MonsterController m; public AudioOcclusion breath; public Vector2 last; public float walked; }
        private class Sleeper { public SleepwalkerController s; public AudioOcclusion creak, breath; }

        private readonly List<Specimen> specimens = new List<Specimen>();
        private readonly List<Sleeper> sleepers = new List<Sleeper>();

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
            if (Instance == this) Instance = null;
        }

        private static AudioOcclusion Loop(GameObject owner, SoundEvent sound, float volume)
        {
            if (sound == null || !sound.HasClips) return null;
            var occlusion = owner.AddComponent<AudioOcclusion>();
            occlusion.Setup(sound, volume);
            return occlusion;
        }

        private static void Play(SoundEvent sound, Vector2 position) => AudioService.Play(sound, position);

        private void Update()
        {
            UpdateFootsteps();
            UpdateFlashlight();
            UpdateSpecimens();
            UpdateSleepers();
            UpdateSkillCheck();
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
            Play(running && sfx.footstepRun != null ? sfx.footstepRun : sfx.footstepWalk, pos);
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
