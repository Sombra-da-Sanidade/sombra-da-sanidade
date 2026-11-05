using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SombraDaSanidade.EditorTools
{
    // Frente 3. Batimento em 3 faixas (#38), relógio do HUD e logs de áudio (#39: HD toca log, sinal perdido, terminal).
    // Menu: Sombra da Sanidade > Rodar teste da Frente 3 (sanidade, HUD e logs).
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SanityHudTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SanityHudTest
    {
        private const string RunningKey = "SombraSanityHudTest.Running";
        private const string BatchKey = "SombraSanityHudTest.Batch";
        private const string LogPath = "Assets/_Project/ScriptableObjects/Narrative/Vance01_Triagem.asset";
        private const string TerminalLogPath = "Assets/_Project/ScriptableObjects/Narrative/Vance02_Contencao.asset";

        // Distâncias do monstro ao jogador em cada faixa (padrão: alerta < 12 m, perigo < 6 m, corte da fita < 4,5 m).
        private static readonly Vector2 WarningOffset = new Vector2(9f, 0f);
        private static readonly Vector2 DangerOffset = new Vector2(4f, 0f);
        private static readonly Vector2 InterruptOffset = new Vector2(3f, 0f);

        private static int step;
        private static float stepStart;
        private static float warningBpm;
        private static Vector2? holdMonsterAt;
        private static AudioLogTerminal terminal;
        private static AudioLogPlayer logPlayer;
        private static AudioLog driveLog;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static SanityHudTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da Frente 3 (sanidade, HUD e logs)", priority = 67)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            holdMonsterAt = null;
            terminal = null;
            logPlayer = null;
            driveLog = null;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Frente 3] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            var heartbeat = Object.FindAnyObjectByType<SanityHeartbeat>();
            var hud = Object.FindAnyObjectByType<DemoHud>();
            var monster = Object.FindAnyObjectByType<MonsterController>(FindObjectsInactive.Include);
            float t = Time.time - stepStart;

            // Segura o monstro numa distância fixa: ele anda pouco entre um tick e outro.
            if (holdMonsterAt.HasValue && monster != null && player != null)
                Place(monster, (Vector2)player.transform.position + holdMonsterAt.Value);

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem jogador, sanidade, batimento, HUD e monstro", player && sanity && heartbeat && hud && monster);
                    if (failed) { Finish(); return; }
                    player.enabled = false;
                    monster.gameObject.SetActive(false);
                    Next();
                    break;

                case 1:
                    if (t < 0.3f) return;
                    Check("Lúcido e sem monstro: sem batida", heartbeat.Tier == ProximityTier.Safe && heartbeat.Bpm == 0f);
                    monster.gameObject.SetActive(true);
                    holdMonsterAt = WarningOffset;
                    Next();
                    break;

                case 2:
                    if (t < 0.4f) return;
                    var config = sanity.Config;
                    Check($"Monstro a 9 m: faixa de alerta ({heartbeat.Tier})", heartbeat.Tier == ProximityTier.Warning);
                    Check($"Alerta: batida entre calma e pânico ({heartbeat.Bpm:0} bpm)",
                        heartbeat.Bpm >= config.calmBpm && heartbeat.Bpm < config.panicBpm);
                    warningBpm = heartbeat.Bpm;
                    holdMonsterAt = DangerOffset;
                    Next();
                    break;

                case 3:
                    if (t < 0.4f) return;
                    Check($"Monstro a 4 m: faixa de perigo ({heartbeat.Tier})", heartbeat.Tier == ProximityTier.Danger);
                    Check($"Perigo acelera a batida ({warningBpm:0} -> {heartbeat.Bpm:0} bpm)", heartbeat.Bpm > warningBpm);
                    Check("Perigo: tensão acima do piso", heartbeat.Tension >= sanity.Config.dangerTensionFloor - 0.01f);
                    holdMonsterAt = null;
                    monster.gameObject.SetActive(false);
                    sanity.Add(-80f);
                    Next();
                    break;

                case 4:
                    if (t < 0.3f) return;
                    Check($"Sem monstro e mente baixa: ainda bate ({heartbeat.Bpm:0} bpm)",
                        heartbeat.Tier == ProximityTier.Safe && heartbeat.Bpm > sanity.Config.calmBpm);
                    sanity.Add(80f);
                    hud.SetWatchHeld(true);
                    Next();
                    break;

                case 5:
                    if (t < 0.3f) return;
                    float slow = player.SpeedMultiplier;
                    Check($"Relógio aberto deixa o Thomas lento ({slow:0.00})", hud.WatchOpen && slow < 0.99f);
                    hud.SetWatchHeld(false);
                    Next();
                    break;

                case 6:
                    if (t < 0.3f) return;
                    Check($"Fechar o relógio devolve a velocidade ({player.SpeedMultiplier:0.00})", Mathf.Approximately(player.SpeedMultiplier, 1f));
                    logPlayer = PrepareLogPlayer();
                    Check("Cena tem AudioLogPlayer com log para o 1º HD", logPlayer != null);
                    if (failed) { Finish(); return; }
                    Next();
                    break;

                case 7:
                    if (t < 0.1f) return; // Start do AudioLogPlayer assina o evento de HD
                    GameManager.Instance.CollectDrive();
                    Check("Coletar o HD toca um log", logPlayer.IsPlaying && logPlayer.Source == null);
                    driveLog = logPlayer.Current;
                    Next();
                    break;

                case 8:
                    if (t < 0.05f) return;
                    Check($"Legenda sendo digitada ({logPlayer.VisibleText.Length}/{logPlayer.FullLine.Length} letras)",
                        logPlayer.VisibleText.Length > 0);
                    monster.gameObject.SetActive(true);
                    holdMonsterAt = InterruptOffset;
                    Next();
                    break;

                case 9:
                    if (t < 0.3f) return;
                    Check("Monstro perto: sinal perdido, gravação pausada", logPlayer.SignalLost && logPlayer.IsPlaying);
                    holdMonsterAt = null;
                    monster.gameObject.SetActive(false);
                    Next();
                    break;

                case 10:
                    if (t < 0.3f) return;
                    Check("Monstro longe: a gravação continua", !logPlayer.SignalLost && logPlayer.IsPlaying);
                    Next();
                    break;

                case 11:
                    if (logPlayer.IsPlaying && t < 8f) return;
                    Check("Log do HD termina sozinho", !logPlayer.IsPlaying);
                    Check("Log ouvido até o fim vai para o arquivo", AudioLogArchive.HasHeard(driveLog));
                    terminal = CreateTerminal(player.transform.position);
                    Check("Terminal de log criado", terminal != null && terminal.Log != null);
                    if (failed) { Finish(); return; }
                    Next();
                    break;

                case 12:
                    if (t < 0.1f) return;
                    terminal.CompleteInteract();
                    Check("Segurar E no terminal toca a fita", terminal.IsPlaying);
                    Teleport(player, (Vector2)player.transform.position + new Vector2(-10f, 0f));
                    Next();
                    break;

                case 13:
                    if (t < 0.3f) return;
                    Check("Longe do terminal a fita para", !terminal.IsPlaying && !logPlayer.IsPlaying);
                    Finish();
                    break;
            }
        }

        // Tempos curtos para o teste não esperar a leitura de verdade.
        private static NarrativeConfig FastConfig()
        {
            var config = ScriptableObject.CreateInstance<NarrativeConfig>();
            config.charactersPerSecond = 400f;
            config.commaPause = 0f;
            config.sentencePause = 0f;
            config.minHoldSeconds = 0.1f;
            config.holdPerCharacter = 0f;
            config.pauseBetweenLines = 0.05f;
            return config;
        }

        // Usa o AudioLogPlayer da cena ou cria um (cenas geradas antes dele não têm).
        private static AudioLogPlayer PrepareLogPlayer()
        {
            var log = AssetDatabase.LoadAssetAtPath<AudioLog>(LogPath);
            if (log == null) return null;
            var logPlayer = AudioLogPlayer.Instance;
            if (logPlayer == null) logPlayer = new GameObject("TestAudioLogPlayer").AddComponent<AudioLogPlayer>();
            SetField(logPlayer, "config", FastConfig());
            SetField(logPlayer, "driveLogs", new[] { log });
            return logPlayer;
        }

        private static void Teleport(PlayerController player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = position;
            player.transform.position = position;
        }

        private static AudioLogTerminal CreateTerminal(Vector2 position)
        {
            var log = AssetDatabase.LoadAssetAtPath<AudioLog>(TerminalLogPath);
            if (log == null) return null;
            var go = new GameObject("TestAudioLog");
            go.transform.position = position + Vector2.right;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var terminal = go.AddComponent<AudioLogTerminal>();
            SetField(terminal, "log", log);
            SetField(terminal, "config", FastConfig());
            return terminal;
        }

        private static void Place(MonsterController monster, Vector2 position)
        {
            var agent = monster.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && agent.Warp(position)) return;
            monster.transform.position = position;
        }

        private static void Next()
        {
            step++;
            stepStart = Time.time;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        private static void SetField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            f?.SetValue(target, value);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Frente 3] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
