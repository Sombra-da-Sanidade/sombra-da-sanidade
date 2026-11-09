using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Beta;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Teste da cena Beta. Em modo de edição: o gerador dá mapas válidos para várias seeds. Em Play na Beta:
    // tudo o que a cena deveria ter está lá, as alas trocam o comportamento do monstro e o painel F1 funciona.
    // Usa o modo seguro durante o teste (o monstro não mata) e restaura o valor salvo no fim.
    // Menu: Sombra da Sanidade > Beta > Rodar teste da Beta.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.BetaSceneTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class BetaSceneTest
    {
        private const string RunningKey = "SombraBetaTest.Running";
        private const string BatchKey = "SombraBetaTest.Batch";
        private const string PrevSafeKey = "SombraBetaTest.Safe";
        private const string ResultsKey = "SombraBetaTest.Results";
        private const string FailedKey = "SombraBetaTest.Failed";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static BetaSceneTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                // O domain reload ao entrar em Play apaga a lista: recupera o que o modo de edição já conferiu.
                results.Clear();
                results.AddRange(SessionState.GetString(ResultsKey, "").Split('\n').Where(l => l.Length > 0));
                failed = SessionState.GetBool(FailedKey, false);
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("Sombra da Sanidade/Beta/Rodar teste da Beta", priority = 7)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;
            CheckGenerator();

            TestScenes.Remember();
            if (!System.IO.File.Exists(BetaSceneBuilder.ScenePath)) BetaSceneBuilder.Build();
            EditorSceneManager.OpenScene(BetaSceneBuilder.ScenePath);
            SessionState.SetBool(PrevSafeKey, MonsterSafeMode.Enabled);
            MonsterSafeMode.Enabled = true;
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            SessionState.SetString(ResultsKey, string.Join("\n", results));
            SessionState.SetBool(FailedKey, failed);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Beta] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        // Fuga da Eclusa (#44): a porta externa fica longe do terminal do saguão, para a purga ser uma corrida.
        private static bool DoorFarFromTerminal(AlfaSceneBuilder.Layout map)
        {
            Vector2 terminal = default, door = default;
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    if (map[x, y] == 'U') terminal = new Vector2(x, y);
                    if (map[x, y] == 'E') door = new Vector2(x, y);
                }
            return Vector2.Distance(terminal, door) >= BetaMapGenerator.MinDoorDistance - 8f;
        }

        private static void CheckGenerator()
        {
            int ok = 0;
            string first = null;
            for (int seed = 1; seed <= 30; seed++)
            {
                var map = BetaMapGenerator.Generate(seed, out var info);
                bool valid = map != null && BetaMapGenerator.Count(map, 'S') == 3 && BetaMapGenerator.Count(map, 'K') >= 3
                    && BetaMapGenerator.Count(map, 'A') >= 6 && BetaMapGenerator.Count(map, 'T') >= 3
                    && BetaMapGenerator.Count(map, 'E') == 4 && BetaMapGenerator.Count(map, 'U') == 1 && BetaMapGenerator.Count(map, '@') == 1
                    && DoorFarFromTerminal(map)
                    && BetaMapGenerator.Count(map, 'M') == 1 && info.Shortcuts.Count == 1;
                if (valid) ok++;
                else first ??= $"seed {seed}";
            }
            Check($"Gerador: 30 seeds dão mapas completos e conectados ({ok}/30{(first != null ? ", falhou " + first : "")})", ok == 30);
            var a = BetaMapGenerator.ToText(BetaMapGenerator.Generate(7, out _), 7);
            var b = BetaMapGenerator.ToText(BetaMapGenerator.Generate(7, out _), 7);
            Check("Gerador: mesma seed, mesmo mapa", a == b);
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var tracker = Object.FindAnyObjectByType<BetaWingTracker>();
            var panel = Object.FindAnyObjectByType<BetaDebugPanel>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    var lockers = Object.FindObjectsByType<Locker>(FindObjectsSortMode.None);
                    Check("Beta tem jogador, monstro, painel e controle de alas", player && monster && panel && tracker);
                    if (failed) { Finish(); return; }
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    Check($"Pede 3 HDs ({gm.DrivesRequired})", gm.DrivesRequired == 3);
                    Check($"3 servidores de HD", Object.FindObjectsByType<DriveServer>(FindObjectsSortMode.None).Length == 3);
                    Check($"3+ disjuntores", Object.FindObjectsByType<Breaker>(FindObjectsSortMode.None).Length >= 3);
                    var tables = lockers.Where(l => l.Table != null).Select(l => l.Table.name).Distinct().ToList();
                    Check($"6+ armários com a tabela das 3 alas ({lockers.Length}: {string.Join(", ", tables)})", lockers.Length >= 6 && tables.Count == 3);
                    Check("Adrena-Syn e Estabilizador no mapa",
                        Object.FindAnyObjectByType<AdrenaSynModule>() && Object.FindAnyObjectByType<LithiumStabilizer>());
                    var logs = Object.FindObjectsByType<AudioLogTerminal>(FindObjectsSortMode.None);
                    Check($"3+ terminais com log ({logs.Length})", logs.Length >= 3 && logs.All(l => l.Log != null));
                    var exitDoor = Object.FindAnyObjectByType<ExitDoor>();
                    Check("Porta externa ligada ao terminal da eclusa e purga na cena (#44)",
                        exitDoor && exitDoor.Terminal && Object.FindAnyObjectByType<SombraDaSanidade.Level.EscapeSequence>());
                    Check("Eclusa, pista em fósforo e atalho do monstro",
                        exitDoor &&Object.FindAnyObjectByType<SanityClue>() && Object.FindAnyObjectByType<MonsterShortcut>());

                    // Sprite novo do armário: abre e troca para o aberto.
                    var locker = lockers[0];
                    locker.Open(LootKind.Nothing);
                    var body = (SpriteRenderer)new SerializedObject(locker).FindProperty("body").objectReferenceValue;
                    Check($"Armário aberto usa o sprite locker_open ({(body && body.sprite ? body.sprite.name : "nenhum")})",
                        body != null && body.sprite != null && body.sprite.name == "locker_open");

                    panel.TeleportTo(Below<DriveServer>("Energia"));
                    Next();
                    break;

                case 1:
                    if (t < 0.3f) return;
                    Check($"Na Energia o monstro checa disjuntores e usa atalhos ({tracker.Current}, {monster.Behaviours})",
                        tracker.Current == "Energia" && monster.Has(MonsterBehaviour.BreakerCheck) && monster.Has(MonsterBehaviour.Shortcuts));
                    var link = Object.FindAnyObjectByType<MonsterShortcut>().GetComponent<NavMeshPlus.Components.NavMeshLink>();
                    Check("Atalho do monstro ligado na Energia", link.enabled);
                    panel.TeleportTo(Below<DriveServer>("Executiva"));
                    Next();
                    break;

                case 2:
                    if (t < 0.3f) return;
                    Check($"Na Executiva ganha memória curta ({monster.Behaviours})",
                        tracker.Current == "Executiva" && monster.Has(MonsterBehaviour.NoiseMemory));

                    panel.Open();
                    Check("F1 abre o painel e pausa o jogo", panel.IsOpen && Time.timeScale == 0f && ModalScreen.IsOpen);
                    panel.ForceWing("Medica");
                    Check("Painel força os comportamentos da Médica", monster.Behaviours == MonsterWingBehaviours.Medica && !tracker.FollowPlayer);
                    panel.SetSanity(30f);
                    var sanity = player.GetComponent<SanityController>();
                    Check($"Painel põe a sanidade em 30 ({sanity.Value:0})", Mathf.Abs(sanity.Value - 30f) < 1f);
                    panel.SetBattery(10f);
                    Check("Painel põe a bateria em 10", Mathf.Abs(player.GetComponent<Flashlight.FlashlightController>().Battery - 10f) < 1f);
                    panel.Close();
                    Check("Fechar o painel retoma o jogo", !panel.IsOpen && Time.timeScale > 0f && !ModalScreen.IsOpen);
                    Finish();
                    break;
            }
        }

        // Chão logo abaixo do servidor de HD da ala.
        private static Vector2 Below<T>(string wing) where T : Component
        {
            var o = Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.name.Contains(wing));
            return (Vector2)o.transform.position + Vector2.down * 1.1f;
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

        private static void Finish()
        {
            if (step < 0) return;
            step = -1;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            MonsterSafeMode.Enabled = SessionState.GetBool(PrevSafeKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Beta] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
