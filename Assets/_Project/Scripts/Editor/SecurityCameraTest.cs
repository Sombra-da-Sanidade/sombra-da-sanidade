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
using Object = UnityEngine.Object;

namespace SombraDaSanidade.EditorTools
{
    // RC1, issue #79. Na Ala Médica, instancia uma câmera ao lado do Thomas e confere: varredura sem vê-lo,
    // detecção em 0,6 s, alarme com apagão do setor (disjuntor ligado corta), chamado do Espécime só com
    // DifficultyProfile.camerasCallSpecimen, EMP por perto desliga e volta, disjuntor dela desliga de vez, e os sons.
    // Usa uma cópia do config com tempos curtos e sem paredes no caminho (a linha de visão é a do Linecast).
    // Menu: Sombra da Sanidade > Rodar teste da câmera de segurança.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SecurityCameraTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SecurityCameraTest
    {
        private const string RunningKey = "SombraSecurityCameraTest.Running";
        private const string BatchKey = "SombraSecurityCameraTest.Batch";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static readonly List<SoundEvent> played = new List<SoundEvent>();
        private static readonly List<float> noises = new List<float>();
        private static bool failed;

        private static SecurityCamera cam;
        private static SecurityCameraConfig cfg;
        private static Breaker sector, own;

        static SecurityCameraTest()
        {
            if (SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da câmera de segurança", priority = 75)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            var prefab = SecurityCameraPrefabBuilder.Build();
            Check("Prefab da câmera gerado", prefab != null && prefab.GetComponent<SecurityCamera>() != null);
            var so = new SerializedObject(prefab.GetComponent<SecurityCamera>());
            Check("Prefab com os 4 sprites", new[] { "idleSprite", "detectSprite", "alarmSprite", "offSprite" }
                .All(f => so.FindProperty(f).objectReferenceValue != null));
            Check("Prefab com cone e LED", so.FindProperty("cone").objectReferenceValue != null
                && so.FindProperty("led").objectReferenceValue != null);

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Câmera] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayed(SoundEvent sound, AudioSource source) => played.Add(sound);
        private static void OnNoise(Vector2 at, float radius) => noises.Add(radius);

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sfx = GameSfx.Load();

            switch (step)
            {
                case 0:
                {
                    if (Time.frameCount < 5 || GameManager.Instance == null) return;
                    if (!GameManager.Instance.IsPlaying)
                    {
                        if (t > 20f) { Check("Partida começou", false); Finish(); }
                        return;
                    }
                    var breakers = Object.FindObjectsByType<Breaker>(FindObjectsSortMode.None);
                    Check($"Ala Médica tem 2+ disjuntores ({breakers.Length})", breakers.Length >= 2);
                    if (player == null || breakers.Length < 2) { Finish(); return; }

                    // Thomas à esquerda da câmera; a câmera começa olhando para a direita (varre ±45°, não o vê).
                    Vector2 p = player.transform.position;
                    Vector2 at = p + new Vector2(2.5f, 0f);
                    own = breakers[0];
                    sector = breakers[1];
                    sector.transform.position = at + new Vector2(0f, 3f);
                    own.transform.position = at + new Vector2(30f, 0f);   // fora do setor: não corta no apagão

                    cfg = Object.Instantiate(SecurityCameraPrefabBuilder.GetOrCreateConfig());
                    cfg.alarmDuration = 0.5f;
                    cfg.empDisableSeconds = 0.5f;

                    var go = (GameObject)Object.Instantiate(SecurityCameraPrefabBuilder.GetOrBuild());
                    go.SetActive(false);
                    go.transform.position = at;
                    cam = go.GetComponent<SecurityCamera>();
                    SetField(cam, "config", cfg);
                    SetField(cam, "obstacles", (LayerMask)0);
                    SetField(cam, "findNearestBreaker", false);
                    SetField(cam, "shutdownBreaker", own);
                    var head = (Transform)GetField(cam, "head");
                    head.rotation = Quaternion.identity;
                    go.SetActive(true);

                    sector.CompleteInteract();
                    // Perfil temporário (cópia do da cena, ou um novo): o teste liga o chamado sem tocar no asset.
                    var current = DifficultyProfile.Active;
                    var profile = current != null ? Object.Instantiate(current) : ScriptableObject.CreateInstance<DifficultyProfile>();
                    profile.camerasCallSpecimen = false;
                    GameManager.Instance.SetProfile(profile);
                    played.Clear();
                    noises.Clear();
                    AudioService.Played -= OnPlayed;
                    AudioService.Played += OnPlayed;
                    NoiseBus.NoiseEmitted -= OnNoise;
                    NoiseBus.NoiseEmitted += OnNoise;
                    Next();
                    break;
                }

                case 1:   // varrendo de costas para o Thomas
                    if (t < 1f) return;
                    Check($"Varre sem ver o Thomas atrás ({cam.State})", cam.State == SecurityCameraState.Sweeping && !cam.SeesPlayer);
                    Check("Disjuntor do setor ligado antes do alarme", sector.IsActive);
                    // Thomas passa para a frente da câmera.
                    MovePlayer(player, (Vector2)cam.transform.position + new Vector2(2.5f, 0f));
                    SetAngle(cam, 0f);
                    Next();
                    break;

                case 2:   // detectando: antes de 0,6 s ainda não é alarme
                    if (t < 0.25f) return;
                    Check($"Vê o Thomas e começa a detectar ({cam.State}, {cam.Detection:0.00})",
                        cam.State == SecurityCameraState.Detecting && cam.Detection > 0f && cam.Detection < 1f);
                    Next();
                    break;

                case 3:
                    if (cam.State != SecurityCameraState.Alarm)
                    {
                        if (t > 2f) { Check("Alarme em até 0,6 s vendo o Thomas", false); Finish(); }
                        return;
                    }
                    Check($"Alarme depois de ~0,6 s vendo o Thomas ({t + 0.25f:0.00} s)", t + 0.25f < 1.2f);
                    Check("Apagão: o disjuntor do setor cortou", !sector.IsActive && !sector.IsBurnt);
                    Check($"Sem camerasCallSpecimen não chama o Espécime (ruídos: {string.Join(", ", noises)})",
                        !noises.Any(r => Mathf.Approximately(r, cfg.callNoiseRadius)));
                    Check("Sons: detecção e alarme", played.Contains(sfx.cameraDetect) && played.Contains(sfx.cameraAlarm));
                    Next();
                    break;

                case 4:   // fim do alarme: volta a vigiar (o Thomas sai da frente)
                    if (t < 0.2f) return;
                    MovePlayer(player, (Vector2)cam.transform.position + new Vector2(-2.5f, 0f));
                    if (t < 0.9f) return;
                    Check($"Depois do alarme volta a varrer ({cam.State})", cam.State == SecurityCameraState.Sweeping);
                    // Fase 3: a câmera chama o Espécime.
                    DifficultyProfile.Active.camerasCallSpecimen = true;
                    noises.Clear();
                    MovePlayer(player, (Vector2)cam.transform.position + new Vector2(2f, 0f));
                    SetAngle(cam, 0f);
                    Next();
                    break;

                case 5:
                    if (cam.State != SecurityCameraState.Alarm)
                    {
                        if (t > 3f) { Check("Segundo alarme", false); Finish(); }
                        return;
                    }
                    Check("Com camerasCallSpecimen o alarme faz ruído chamando o Espécime",
                        noises.Any(r => Mathf.Approximately(r, cfg.callNoiseRadius)));
                    DifficultyProfile.Active.camerasCallSpecimen = false;
                    Next();
                    break;

                case 6:   // EMP com o Thomas perto
                    if (t < 0.7f) return;
                    played.Clear();
                    var flashlight = Object.FindAnyObjectByType<FlashlightController>();
                    Raise(flashlight, "EmpFired");
                    Check($"EMP por perto desliga a câmera ({cam.State})", cam.State == SecurityCameraState.Disabled);
                    Check("Som de curto no EMP", played.Contains(sfx.cameraShort));
                    MovePlayer(player, (Vector2)cam.transform.position + new Vector2(-2.5f, 0f));
                    Next();
                    break;

                case 7:
                    if (t < 0.8f) return;
                    Check($"Volta depois do tempo do EMP ({cam.State})", cam.State == SecurityCameraState.Sweeping);
                    own.CompleteInteract();
                    Check($"Religar o disjuntor dela desliga de vez ({cam.State})", cam.State == SecurityCameraState.Off);
                    MovePlayer(player, (Vector2)cam.transform.position + new Vector2(2f, 0f));
                    SetAngle(cam, 0f);
                    Next();
                    break;

                case 8:
                    if (t < 1f) return;
                    Check($"Desligada não detecta mais ({cam.State})", cam.State == SecurityCameraState.Off && !cam.SeesPlayer);
                    Finish();
                    break;
            }
        }

        private static void MovePlayer(PlayerController player, Vector2 to)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = to;
            player.transform.position = new Vector3(to.x, to.y, player.transform.position.z);
        }

        private static void SetAngle(SecurityCamera c, float degrees)
        {
            SetField(c, "angle", degrees);
            ((Transform)GetField(c, "head")).rotation = Quaternion.Euler(0f, 0f, degrees);
        }

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

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
            NoiseBus.NoiseEmitted -= OnNoise;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Câmera] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
