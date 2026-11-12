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
    // Frente 2. Teste do Sonâmbulo na cena de protótipo (corredor externo da direita):
    // adormecido até ser visto, trava só com a lanterna nele, vem mesmo olhado no escuro, corre rápido,
    // trava de novo com a luz e mata (com a cena de morte) quando encosta sem a luz. O Espécime da cena é desligado.
    // Menu: Sombra da Sanidade > Rodar teste do Sonâmbulo.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.SleepwalkerTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class SleepwalkerTest
    {
        private const string RunningKey = "SombraSleepwalkerTest.Running";
        private const string BatchKey = "SombraSleepwalkerTest.Batch";
        private const string SafeKey = "SombraSleepwalkerTest.Safe";

        private static readonly Vector2 PlayerSpot = new Vector2(17.5f, -11f);
        private static readonly Vector2 WalkerSpot = new Vector2(17.5f, -3f);

        private static int step;
        private static float stepStart;
        private static Vector2 mark;
        private static float markTime;
        private static SleepwalkerController walker;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static SleepwalkerTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do Sonâmbulo", priority = 67)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SleepwalkerPrefabBuilder.Build();
            SessionState.SetBool(SafeKey, MonsterSafeMode.Enabled);
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            walker = null;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Sonâmbulo] Iniciando teste");
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
                    player.enabled = false;                         // a mira passa a ser controlada pelo teste
                    SetLight(flashlight, false);                    // só o campo de visão conta
                    Teleport(player, PlayerSpot);
                    SetFacing(player, Vector2.down);                // de costas para onde ele vai aparecer
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SleepwalkerPrefabBuilder.PrefabPath);
                    walker = Object.Instantiate(prefab, WalkerSpot, Quaternion.identity).GetComponent<SleepwalkerController>();
                    Next();
                    break;

                case 1:
                    if (t < 1f) return;
                    mark = walker.transform.position;
                    Check("Começa adormecido", walker.State == SleepwalkerState.Dormant);
                    Check($"Adormecido não sai do lugar ({Vector2.Distance(mark, WalkerSpot):0.00} m)", Vector2.Distance(mark, WalkerSpot) < 0.3f);
                    SetFacing(player, Vector2.up);                  // olha para ele, ainda no escuro
                    Next();
                    break;

                case 2:
                    if (t < 0.3f) return;
                    Check("Acorda quando o Thomas olha para ele (mesmo no escuro)", walker.IsAwake);
                    SetLight(flashlight, true);                     // agora a lanterna nele
                    mark = walker.transform.position;
                    Next();
                    break;

                case 3:
                    if (t < 2f) return;                             // passa da folga de despertar
                    float still = Vector2.Distance(mark, walker.transform.position);
                    Check("Com a lanterna nele, fica travado", walker.IsLit && walker.State == SleepwalkerState.Frozen);
                    Check($"Com a lanterna nele, não se move ({still:0.00} m em 2 s)", still < 0.05f);
                    SetLight(flashlight, false);                    // continua olhando, mas apaga a luz
                    Next();
                    break;

                case 4:
                    if (walker.State != SleepwalkerState.Moving)
                    {
                        if (t > 1f) { Check("Lanterna apagada, mesmo olhando, ele vem", false); Finish(); }
                        return;
                    }
                    Check("Lanterna apagada, mesmo olhando, ele vem", true);
                    mark = walker.transform.position;
                    markTime = Time.time;
                    Next();
                    break;

                case 5:
                    if (t < 0.3f) return;
                    float speed = Vector2.Distance(mark, walker.transform.position) / (Time.time - markTime);
                    Check($"Fora da luz é bem mais rápido que o jogador ({speed:0.0} m/s)", speed > 5.5f);
                    SetFacing(player, ((Vector2)walker.transform.position - (Vector2)player.transform.position).normalized);
                    SetLight(flashlight, true);
                    Next();
                    break;

                case 6:
                    if (t < 0.15f) return;
                    Check($"Lanterna de novo nele, trava na hora ({walker.Velocity.magnitude:0.00} m/s)",
                        walker.State == SleepwalkerState.Frozen && walker.Velocity.magnitude < 0.05f);
                    Check($"Trava numa pose sorteada (pose {walker.Pose})", walker.Pose != SleepwalkerController.DormantPose);
                    Check("Com a luz nele, não mata", gm.IsPlaying);
                    SetFacing(player, Vector2.down);                // vira as costas
                    Next();
                    break;

                case 7:
                    if (walker.IsKilling && !killSeen)
                    {
                        killSeen = true;
                        var scene = walker.GetComponent<MonsterKillScene>();
                        Check($"Encostar toca a cena de morte ({(scene != null ? (scene.LastKill == MonsterKillScene.Kill.Bite ? "engolir" : "pescoço") : "sem cena")})",
                            scene != null && scene.IsPlaying && gm.IsPlaying);
                    }
                    if (gm.IsPlaying && t < 9f) return;
                    if (!killSeen) Check("Encostar toca a cena de morte", false);
                    Check("De costas, ele alcança e mata", gm.State == GameState.GameOver && gm.LastDeathCause == DeathCause.Monster);
                    Finish();
                    break;
            }
        }

        private static bool killSeen;

        private static void Next()
        {
            step++;
            killSeen = false;
            stepStart = Time.time;
        }

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }

        private static void Teleport(PlayerController player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
        }

        private static void SetFacing(PlayerController player, Vector2 dir) => SetBackingField(player, "Facing", dir.normalized);

        private static void SetLight(FlashlightController flashlight, bool on) => SetBackingField(flashlight, "IsSwitchedOn", on);

        private static void SetBackingField(object target, string property, object value)
        {
            var field = target.GetType().GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new System.MissingFieldException(target.GetType().Name, property);
            field.SetValue(target, value);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            MonsterSafeMode.Enabled = SessionState.GetBool(SafeKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Sonâmbulo] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
