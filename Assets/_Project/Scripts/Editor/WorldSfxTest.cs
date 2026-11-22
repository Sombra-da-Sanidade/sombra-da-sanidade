using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5, issue #106. Confere os sons de mundo, itens e interface na Ala Médica: dispara cada sistema pelo
    // método público (disjuntor, armário com pilha, coleta de HD, Estabilizador, Adrena-Syn, menus, vitória) e
    // verifica se o som certo passou pelo AudioService.
    // Menu: Sombra da Sanidade > Rodar teste dos sons de mundo.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.WorldSfxTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class WorldSfxTest
    {
        private const string RunningKey = "SombraWorldSfxTest.Running";
        private const string BatchKey = "SombraWorldSfxTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<SoundEvent> played = new List<SoundEvent>();
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static WorldSfxTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste dos sons de mundo", priority = 75)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            stepStart = 0f;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Sons de mundo] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayed(SoundEvent sound, AudioSource source) => played.Add(sound);

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (stepStart <= 0f) stepStart = Time.realtimeSinceStartup;
            float t = Time.realtimeSinceStartup - stepStart;
            var gm = GameManager.Instance;
            var sfx = GameSfx.Load();

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5 || gm == null || GameSfxHooks.Instance == null)
                    {
                        if (t > 15f) { Check("Cena com GameManager e GameSfxHooks", false); Finish(); }
                        return;
                    }
                    if (t < 0.5f) return;   // Start dos ganchos
                    AudioService.Played -= OnPlayed;
                    AudioService.Played += OnPlayed;

                    var breaker = Object.FindObjectsByType<Breaker>(FindObjectsSortMode.None).FirstOrDefault(b => b.CanInteract);
                    Check("A ala tem disjuntor", breaker != null);
                    if (breaker != null)
                    {
                        played.Clear();
                        breaker.CompleteInteract();
                        Check("Religar o disjuntor toca o estalo com as lâmpadas", played.Contains(sfx.breakerOn));
                    }

                    var locker = Object.FindObjectsByType<Locker>(FindObjectsSortMode.None).FirstOrDefault(l => l.CanInteract);
                    Check("A ala tem armário", locker != null);
                    if (locker != null)
                    {
                        played.Clear();
                        locker.Open(LootKind.Battery);
                        Check("Abrir armário toca a porta", played.Contains(sfx.lockerOpen));
                        Check("Pilha no armário toca o som de pegar", played.Contains(sfx.itemPickup));
                    }

                    played.Clear();
                    gm.CollectDrive();
                    Check("Coletar HD toca a coleta", played.Contains(sfx.driveCollect));

                    var sanity = Object.FindAnyObjectByType<SanityController>();
                    played.Clear();
                    LithiumStabilizer.ApplyEffect(sanity, sanity);
                    Check("Estabilizador (e sedativo) toca a injeção", played.Contains(sfx.injection));

                    played.Clear();
                    NeuralModule.Apply(sanity);
                    Check("Conectar o Módulo Neural toca o choque", played.Contains(sfx.neuralModule));

                    played.Clear();
                    UiSfx.Move();
                    UiSfx.Confirm();
                    UiSfx.Back();
                    Check("Menus: navegar, confirmar e voltar",
                        played.Contains(sfx.uiMove) && played.Contains(sfx.uiConfirm) && played.Contains(sfx.uiBack));

                    played.Clear();
                    var player = Object.FindAnyObjectByType<PlayerController>();
                    AdrenaSynEffect.On(player).Activate(0.3f, 3f);
                    Next();
                    break;

                case 1:   // A injeção do Adrena-Syn sai no Update dos ganchos.
                    if (!played.Contains(sfx.injection) && t < 1f) return;
                    Check("Adrena-Syn toca a injeção quando o efeito liga", played.Contains(sfx.injection));

                    // Segurar E num armário fechado: o jogador vai até ele e segura por código.
                    var closed = Object.FindObjectsByType<Locker>(FindObjectsSortMode.None).FirstOrDefault(l => l.CanInteract);
                    var interactor = Object.FindAnyObjectByType<PlayerInteractor>();
                    Check("Há outro armário fechado e o PlayerInteractor", closed != null && interactor != null);
                    if (closed == null || interactor == null) { Finish(); return; }
                    var body = interactor.GetComponent<Rigidbody2D>();
                    if (body != null) body.position = closed.transform.position;
                    interactor.transform.position = closed.transform.position;
                    Physics2D.SyncTransforms();
                    interactor.HoldOverride = true;
                    Next();
                    break;

                case 2:   // Segurando: o loop de forçar o armário toca.
                {
                    var hooks = GameSfxHooks.Instance;
                    bool holding = hooks.ChannelSound == sfx.lockerForce && hooks.ChannelVolume > 0f;
                    if (!holding && t < 0.8f) return;
                    Check("Segurar E no armário toca o loop de forçar o trinco", holding);
                    Object.FindAnyObjectByType<PlayerInteractor>().HoldOverride = false;
                    Next();
                    break;
                }

                case 3:   // Soltou: o loop some.
                {
                    var hooks = GameSfxHooks.Instance;
                    if (hooks.ChannelVolume > 0f && t < 1f) return;
                    Check("Soltar o E para o loop", hooks.ChannelVolume <= 0f);

                    played.Clear();
                    while (!gm.HasAllDrives) gm.CollectDrive();
                    played.Clear();
                    gm.TriggerVictory();
                    Check("Vitória de uma ala toca o \"progresso salvo\"", played.Contains(sfx.uiSaved));
                    Check("Nenhum som de mundo emite ruído no NoiseBus",
                        new[] { sfx.breakerOn, sfx.breakerOff, sfx.breakerBurnt, sfx.driveEject, sfx.lockerOpen, sfx.itemPickup,
                                sfx.injection, sfx.flareIgnite, sfx.keypadOk, sfx.keypadWrong, sfx.substationOn }
                            .All(s => s != null && s.noiseRadius <= 0f));
                    Next();
                    break;
                }

                case 4:   // Fim da partida: os loops ficam mudos (antes tocavam por baixo da tela de fim).
                {
                    if (t < 0.2f) return;
                    var hooks = GameSfxHooks.Instance;
                    var breath = hooks.SpecimenBreath(0);
                    Check("Na tela de fim o zumbido da lanterna e a respiração do monstro ficam mudos",
                        (hooks.FlashlightHum == null || hooks.FlashlightHum.volume == 0f) && (breath == null || breath.Volume == 0f));
                    var heart = Object.FindAnyObjectByType<SanityHeartbeat>();
                    var sources = heart != null ? heart.GetComponents<AudioSource>() : new AudioSource[0];
                    Check("Na tela de fim a respiração do Thomas para", sources.All(s => !s.loop || !s.isPlaying));
                    Finish();
                    break;
                }
            }
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
            var sb = new StringBuilder($"[Sons de mundo] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
