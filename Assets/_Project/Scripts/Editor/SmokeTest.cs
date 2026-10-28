using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SombraDaSanidade.EditorTools
{
    // Teste de fumaça da mini demo: entra em Play, simula mouse/teclado e confere o loop básico.
    // Menu: Sombra da Sanidade > Rodar smoke test (resultado no Console).
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SmokeTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SmokeTest
    {
        private const string RunningKey = "SombraSmokeTest.Running";
        private const string BatchKey = "SombraSmokeTest.Batch";

        private static int step;
        private static int waitFrames;
        private static float stepStart;
        private static float batteryAtStart;
        private static float sanityAtDark;
        private static Mouse mouse;
        private static Keyboard keyboard;
        private static readonly List<string> results = new List<string>();
        private static bool failed;
        private static InputSettings.EditorInputBehaviorInPlayMode previousInputBehavior;

        static SmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar smoke test", priority = 60)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            string scene = System.IO.File.Exists(PrototypeSceneBuilder.DemoScenePath)
                ? PrototypeSceneBuilder.DemoScenePath
                : PrototypeSceneBuilder.PrototypeScenePath;

            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scene);

            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            ResetState();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log($"[Smoke] Iniciando em {scene}");
            EditorApplication.EnterPlaymode();
        }

        private static void ResetState()
        {
            step = 0;
            waitFrames = 0;
            results.Clear();
            failed = false;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (waitFrames-- > 0) return;

            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var gm = GameManager.Instance;
            var cam = Camera.main;

            switch (step)
            {
                case 0:
                    // Sem janela Game em batchmode: força o input a ir para o jogo.
                    previousInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
                    InputSystem.settings.editorInputBehaviorInPlayMode =
                        InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    mouse = InputSystem.AddDevice<Mouse>();
                    keyboard = InputSystem.AddDevice<Keyboard>();
                    Check("Cena tem Player, Lanterna, GameManager e câmera", player && flashlight && gm && cam);
                    Check("Lanterna habilitada (config presente)", flashlight && flashlight.enabled);
                    var sanity = Object.FindAnyObjectByType<SanityController>();
                    Check("Sanidade habilitada (config presente)", sanity && sanity.enabled);
                    Check("Cena tem efeitos de sanidade (vinheta, batimento, alucinação, pistas)",
                        Object.FindAnyObjectByType<SanityVignette>() && Object.FindAnyObjectByType<SanityHeartbeat>()
                        && Object.FindAnyObjectByType<SanityHallucination>() && Object.FindAnyObjectByType<SanityClue>());
                    var clue = Object.FindAnyObjectByType<SanityClue>();
                    Check("Pista em fósforo escondida com a sanidade cheia", clue && !clue.IsRevealed);
                    if (failed) { Finish(); return; }
                    // O monstro tem teste próprio (MonsterTest); aqui ele sairia patrulhando e poderia
                    // encostar no jogador teleportado, deixando o teste do loop instável.
                    var monster = Object.FindAnyObjectByType<MonsterController>();
                    if (monster != null) monster.gameObject.SetActive(false);
                    batteryAtStart = flashlight.Battery;
                    stepStart = Time.time;
                    Next(10);
                    break;

                case 1: // Mira para a direita
                    PointMouse(cam, player.transform.position + Vector3.right * 3f);
                    Next(5);
                    break;

                case 2:
                    LogAim(player, cam);
                    CheckInput("Mira segue o mouse (direita)", player.Facing.x > 0.9f);
                    PointMouse(cam, player.transform.position + Vector3.left * 3f);
                    Next(5);
                    break;

                case 3:
                    LogAim(player, cam);
                    CheckInput("Mira segue o mouse (esquerda)", player.Facing.x < -0.9f);
                    Next(0);
                    break;

                case 4:
                    if (Time.time - stepStart < 1.5f) { waitFrames = 5; return; }
                    Check($"Bateria drena com a luz ligada ({batteryAtStart:0.0} -> {flashlight.Battery:0.0})", flashlight.Battery < batteryAtStart);
                    Check("Lanterna começa ligada", flashlight.IsSwitchedOn);
                    Press(Key.F);
                    Next(3);
                    break;

                case 5:
                    Release();
                    CheckInput("F desliga a lanterna", !flashlight.IsSwitchedOn);
                    sanityAtDark = Object.FindAnyObjectByType<SanityController>().Value;
                    Next(30);
                    break;

                case 6:
                    float sanityNow = Object.FindAnyObjectByType<SanityController>().Value;
                    CheckInput($"Sanidade drena no escuro ({sanityAtDark:0.0} -> {sanityNow:0.0})", sanityNow < sanityAtDark);
                    Press(Key.F);
                    Next(3);
                    break;

                case 7:
                    Release();
                    CheckInput("F liga a lanterna de novo", flashlight.IsSwitchedOn);
                    // Leva o jogador ao primeiro servidor e segura E de verdade.
                    var server = Object.FindAnyObjectByType<DriveServer>();
                    Teleport(player, server.transform.position + Vector3.down * 1.1f);
                    Press(Key.E);
                    stepStart = Time.time;
                    Next(5);
                    break;

                case 8:
                    if (gm.DrivesCollected < 1 && Time.time - stepStart < 4f) { waitFrames = 5; return; }
                    Release();
                    CheckInput($"Segurar E coleta um HD ({gm.DrivesCollected}/3)", gm.DrivesCollected == 1);
                    foreach (var s in Object.FindObjectsByType<DriveServer>())
                        if (s.CanInteract) s.CompleteInteract();
                    Check($"3 HDs coletados ({gm.DrivesCollected}/3)", gm.HasAllDrives);
                    var door = Object.FindAnyObjectByType<ExitDoor>();
                    Teleport(player, door.transform.position + Vector3.up * 0.3f);
                    stepStart = Time.realtimeSinceStartup;
                    Next(10);
                    break;

                case 9:
                    if (gm.State != GameState.Victory && Time.realtimeSinceStartup - stepStart < 2f) { waitFrames = 5; return; }
                    Check("Encostar na porta com 3 HDs dá vitória", gm.State == GameState.Victory);
                    Finish();
                    break;
            }
        }

        private static void LogAim(PlayerController player, Camera cam)
        {
            var current = Mouse.current;
            Debug.Log($"[Smoke] mouse.current==teste:{current == mouse} pos:{(current != null ? current.position.ReadValue() : Vector2.zero)} " +
                      $"screen:{Screen.width}x{Screen.height} cam:{cam.pixelRect} facing:{player.Facing}");
        }

        private static void Next(int frames)
        {
            step++;
            waitFrames = frames;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        // Em batchmode não há janela Game e o Input System não entrega eventos ao jogo.
        // Esses checks só valem rodando pelo menu, com o Editor aberto.
        private static void CheckInput(string label, bool ok)
        {
            if (Application.isBatchMode) results.Add($"SKIP {label} (batchmode sem input)");
            else Check(label, ok);
        }

        private static void PointMouse(Camera cam, Vector3 world)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(screen.x, screen.y) });
            InputSystem.Update();
        }

        private static void Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
        }

        private static void Release()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
        }

        private static void Teleport(PlayerController player, Vector3 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);

            var sb = new StringBuilder($"[Smoke] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());

            InputSystem.settings.editorInputBehaviorInPlayMode = previousInputBehavior;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
