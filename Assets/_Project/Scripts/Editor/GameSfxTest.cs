using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // RC1/RC4, issue #105. Confere os sons de gameplay na Ala Médica: GameSfx completo (3+ tomadas nos sons
    // repetidos, nenhum ruído novo no NoiseBus) e os ganchos do GameSfxHooks (passos, lanterna, bateria, EMP,
    // estados do Espécime e multímetro). Os eventos privados (EMP, multímetro) são disparados por reflexão.
    // Menu: Sombra da Sanidade > Rodar teste dos sons de gameplay.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.GameSfxTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class GameSfxTest
    {
        private const string RunningKey = "SombraGameSfxTest.Running";
        private const string BatchKey = "SombraGameSfxTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<SoundEvent> played = new List<SoundEvent>();
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static GameSfxTest()
        {
            if (SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste dos sons de gameplay", priority = 74)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            SoundEventBuilder.Build();
            var sfx = AssetDatabase.LoadAssetAtPath<GameSfx>(SoundEventBuilder.GameSfxPath);
            Check("GameSfx existe em Resources", sfx != null);
            if (sfx != null)
            {
                var fields = typeof(GameSfx).GetFields().Where(f => f.FieldType == typeof(SoundEvent)).ToArray();
                var sounds = fields.Select(f => (f.Name, s: (SoundEvent)f.GetValue(sfx))).ToArray();
                var missing = sounds.Where(x => x.s == null || !x.s.HasClips).Select(x => x.Name).ToArray();
                Check($"Todos os {fields.Length} sons ligados e com áudio ({string.Join(", ", missing)})", missing.Length == 0);
                Check("Sons repetidos com 3+ tomadas (passos, passos do Espécime, tique)",
                    sfx.footstepWalk.clips.Length >= 3 && sfx.footstepRun.clips.Length >= 3
                    && sfx.specimenStep.clips.Length >= 3 && sfx.skillTick.clips.Length >= 3);
                Check("Nenhum som novo emite ruído no NoiseBus", sounds.All(x => x.s == null || x.s.noiseRadius <= 0f));
                Check("Respiração do Espécime tem versão abafada", sfx.specimenBreath.muffledClips.Length == sfx.specimenBreath.clips.Length);
            }

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Sons] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayed(SoundEvent sound, AudioSource source) => played.Add(sound);

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var sfx = GameSfx.Load();
            var hooks = GameSfxHooks.Instance;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var flashlight = UnityEngine.Object.FindAnyObjectByType<FlashlightController>();
            var monster = UnityEngine.Object.FindAnyObjectByType<MonsterController>(FindObjectsInactive.Include);

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5 || GameManager.Instance == null || t < 1f) return;
                    Check("GameSfxHooks criado na cena de jogo", hooks != null);
                    if (hooks == null) { Finish(); return; }
                    Check("Espécime ganhou respiração em loop", hooks.SpecimenCount > 0 && hooks.SpecimenBreath(0) != null);
                    played.Clear();
                    AudioService.Played -= OnPlayed;
                    AudioService.Played += OnPlayed;
                    Next();
                    break;

                case 1:   // passos: anda 0,1 por quadro durante 0,6 s
                    if (player != null) player.transform.position += new Vector3(0.1f, 0f, 0f);
                    if (t < 0.6f) return;
                    Check($"Passos tocam ao andar ({played.Count(s => s == sfx.footstepWalk)})", played.Count(s => s == sfx.footstepWalk) >= 2);
                    played.Clear();
                    flashlight.TrySpend(flashlight.Battery);
                    Next();
                    break;

                case 2:
                    if (t < 0.2f) return;
                    Check("Bateria acabando toca 'esgotada'", played.Contains(sfx.batteryDead));
                    played.Clear();
                    flashlight.Recharge(10f);   // fica abaixo de 20%: também deve avisar bateria fraca
                    Next();
                    break;

                case 3:
                    if (t < 0.3f) return;
                    Check("Lanterna volta a acender: 'liga'", played.Contains(sfx.flashlightOn));
                    Check("Bateria abaixo de 20%: aviso de bateria fraca", played.Contains(sfx.batteryLow));
                    Check("Zumbido da lanterna acesa", hooks.FlashlightHum != null && hooks.FlashlightHum.isPlaying && hooks.FlashlightHum.volume > 0f);
                    played.Clear();
                    Raise(flashlight, "EmpFired");
                    Check("EMP toca o disparo", played.Contains(sfx.empFire));
                    var skill = UnityEngine.Object.FindAnyObjectByType<SkillCheck>();
                    if (skill != null)
                    {
                        Raise(skill, "Warned");
                        Raise(skill, "Resolved", SkillCheckResult.Great);
                        Raise(skill, "Resolved", SkillCheckResult.Fail);
                        Check("Multímetro: aviso, ótimo e falha", played.Contains(sfx.skillWarn) && played.Contains(sfx.skillGreat) && played.Contains(sfx.skillFail));
                    }
                    played.Clear();
                    if (monster != null)
                    {
                        monster.gameObject.SetActive(true);
                        monster.Stun(1f);
                    }
                    Next();
                    break;

                case 4:
                    if (t < 0.3f) return;
                    Check("Espécime atordoado toca o som de atordoado", monster != null && played.Contains(sfx.specimenStunned));
                    Finish();
                    break;
            }
        }

        // Dispara um evento C# de outro componente (o campo de apoio tem o mesmo nome do evento).
        private static void Raise(object target, string evt, params object[] args)
        {
            var field = target.GetType().GetField(evt, BindingFlags.Instance | BindingFlags.NonPublic);
            (field?.GetValue(target) as Delegate)?.DynamicInvoke(args);
        }

        private static void Next()
        {
            step++;
            stepStart = Time.realtimeSinceStartup;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        private static void Finish()
        {
            AudioService.Played -= OnPlayed;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Sons] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
