using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2. Teste da ronda do Sonâmbulo na cena de protótipo: começa rondando no escuro, uma câmera de
    // segurança em alarme o chama até o ponto (sem achar ninguém, volta a rondar), encontra o Thomas por perto e
    // daí persegue para sempre. Também confere que, só por script, ele não acorda ao ser visto.
    // Menu: Sombra da Sanidade > Rodar teste da ronda do Sonâmbulo.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SleepwalkerRoamTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SleepwalkerRoamTest
    {
        private const string RunningKey = "SombraSleepwalkerRoamTest.Running";
        private const string BatchKey = "SombraSleepwalkerRoamTest.Batch";
        private static readonly Vector2 PlayerSpot = new Vector2(17.5f, -11f);
        private static readonly Vector2 WalkerSpot = new Vector2(17.5f, 6f);
        private static readonly Vector2 AlarmSpot = new Vector2(17.5f, 0f);
        private static readonly Vector2 FarSpot = new Vector2(-17.5f, 11f);

        private static int step;
        private static float stepStart;
        private static Vector2 mark, alarmAt;
        private static SleepwalkerController walker, sleeper;
        private static SecurityCamera camera;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static SleepwalkerRoamTest()
        {
            if (SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da ronda do Sonâmbulo", priority = 68)]
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
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Ronda] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem jogador, lanterna e GameManager", player && flashlight && gm);
                    if (failed) { Finish(); return; }
                    MonsterSafeMode.Enabled = false;
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    var specimen = Object.FindAnyObjectByType<MonsterController>();
                    if (specimen != null) specimen.gameObject.SetActive(false);
                    player.enabled = false;
                    SetBacking(flashlight, "IsSwitchedOn", false);
                    Teleport(player, PlayerSpot);
                    SetBacking(player, "Facing", Vector2.up);

                    // Câmera antes dos Sonâmbulos: eles se inscrevem no alarme no Start.
                    camera = ((GameObject)Object.Instantiate(SecurityCameraPrefabBuilder.GetOrBuild(),
                        new Vector3(AlarmSpot.x, AlarmSpot.y, 0f), Quaternion.identity)).GetComponent<SecurityCamera>();
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SleepwalkerPrefabBuilder.PrefabPath);
                    walker = Object.Instantiate(prefab, WalkerSpot, Quaternion.identity).GetComponent<SleepwalkerController>();
                    walker.StartMode = SleepwalkerStart.Roaming;
                    // Config próprio do teste: acorda rápido.
                    var cfg = Object.Instantiate(walker.Config);
                    cfg.wakeGrace = 0.2f;
                    typeof(SleepwalkerController).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(walker, cfg);

                    // Outro, só acorda pela cutscene: fica na frente do Thomas, visível.
                    sleeper = Object.Instantiate(prefab, PlayerSpot + Vector2.up * 3f, Quaternion.identity).GetComponent<SleepwalkerController>();
                    sleeper.StartMode = SleepwalkerStart.Dormant;
                    sleeper.WakeOnlyByScript = true;
                    mark = WalkerSpot;
                    Next();
                    break;

                case 1:
                    if (t < 4f) return;
                    float moved = Vector2.Distance(mark, walker.transform.position);
                    Check($"Começa acordado e rondando, sem caçar (andou {moved:0.0} m em 4 s)", walker.IsAwake && walker.IsRoaming && moved > 0.5f);
                    Check($"Rondando, anda devagar ({walker.Config.roamSpeed} m/s)", walker.Config.roamSpeed < 4f);
                    Check("Visto pelo Thomas, o da revelação continua dormindo", !sleeper.IsAwake);
                    sleeper.gameObject.SetActive(false);
                    // Alarme da câmera (como se ela tivesse visto o Thomas).
                    // Ponto do alarme a 6 m de onde ele está (no mesmo corredor), para dar tempo de conferir que foi chamado.
                    alarmAt = new Vector2(AlarmSpot.x, walker.transform.position.y + (walker.transform.position.y > 0f ? -6f : 6f));
                    typeof(SecurityCamera).GetMethod("RaiseAlarm", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(camera, new object[] { alarmAt });
                    Next();
                    break;

                case 2:
                    if (t < 0.2f) return;
                    Check("Alarme da câmera o chama: passa a caçar", walker.IsHunting);
                    mark = walker.transform.position;
                    Next();
                    break;

                case 3:
                    if (Vector2.Distance(walker.transform.position, alarmAt) > 1.6f && t < 6f) return;
                    Check($"Vai até o ponto do alarme ({Vector2.Distance(walker.transform.position, alarmAt):0.0} m)",
                        Vector2.Distance(walker.transform.position, alarmAt) <= 1.6f);
                    // Thomas longe e fora de vista: chegando ao alarme sem vê-lo, volta a rondar.
                    Next();
                    break;

                case 4:
                    if (walker.IsHunting && t < 3f) return;
                    Check("Sem ver o Thomas no ponto do alarme, volta a rondar", walker.IsRoaming);
                    // Thomas aparece perto dele, no escuro.
                    Teleport(player, (Vector2)walker.transform.position + Vector2.down * 3f);
                    Next();
                    break;

                case 5:
                    if (!walker.IsHunting && t < 2f) return;
                    Check("Thomas perto e sem parede no meio: caça", walker.IsHunting);
                    Teleport(player, FarSpot);   // do outro lado do mapa, bem além do loseDistance
                    Next();
                    break;

                case 6:
                    if (t < 4f) return;
                    Check("Depois de encontrar o Thomas, persegue para sempre (longe, não volta a rondar)",
                        walker.IsHunting && walker.HasFoundPlayer && !walker.IsRoaming);
                    Finish();
                    break;
            }
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

        private static void Teleport(PlayerController player, Vector2 position)
        {
            player.GetComponent<Rigidbody2D>().position = position;
            player.transform.position = position;
        }

        private static void SetBacking(object target, string property, object value)
        {
            var field = target.GetType().GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new System.MissingFieldException(target.GetType().Name, property);
            field.SetValue(target, value);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Ronda] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
