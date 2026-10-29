using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 4. Teste do disjuntor e da Zona Segura na cena de protótipo (issue #10).
    // Menu: Sombra da Sanidade > Rodar teste do disjuntor.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.BreakerTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class BreakerTest
    {
        private const string RunningKey = "SombraBreakerTest.Running";
        private const string BatchKey = "SombraBreakerTest.Batch";
        // Encurta os 60 s do GDD para o teste não demorar.
        private const float TestLightDuration = 2.5f;

        private static int step;
        private static float stepStart;
        private static float sanityMark;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static BreakerTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste do disjuntor", priority = 62)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Disjuntor] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var breaker = Object.FindAnyObjectByType<Breaker>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem disjuntor, jogador e sanidade", breaker && player && sanity);
                    if (failed) { Finish(); return; }
                    var monster = Object.FindAnyObjectByType<MonsterController>();
                    if (monster != null) monster.gameObject.SetActive(false);   // não pode encostar no jogador
                    player.enabled = false;
                    Check("Zona Segura criada e apagada no início", breaker.Zone != null && !breaker.Zone.IsOn);
                    Check("Dá para reparar", breaker.CanInteract);

                    breaker.BeginInteract();
                    Next();
                    break;

                case 1:
                    if (t < 0.2f) return;
                    var sp = breaker.transform.Find("Sparks").GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                    Check("Faíscas durante o reparo", sp.enabled);
                    breaker.CancelInteract();
                    Check("Cancelar apaga as faíscas", !sp.enabled);

                    SetField(breaker, "lightDuration", TestLightDuration);
                    SetField(breaker, "warningTime", 1f);
                    sanity.Add(-40f);
                    breaker.CompleteInteract();
                    Check("Reparo acende a Zona Segura", breaker.Zone.IsOn && breaker.IsActive && !breaker.CanInteract);
                    Check("Monstro bloqueado na zona", MonsterRepellent.Find(breaker.Zone.transform.position) != null);
                    Teleport(player, breaker.Zone.transform.position);
                    Next();
                    break;

                case 2:
                    if (t < 0.4f) return;
                    Check("Jogador dentro conta como Zona Segura", sanity.InSafeZone);
                    sanityMark = sanity.Value;
                    Next();
                    break;

                case 3:
                    if (t < 0.8f) return;
                    Check($"Sanidade regenera na zona ({sanityMark:0.0} -> {sanity.Value:0.0})", sanity.Value > sanityMark);
                    Next();
                    break;

                case 4:
                    if (breaker.IsActive && t < TestLightDuration + 1f) return;
                    Check("Zona apaga no fim do tempo", !breaker.Zone.IsOn && !breaker.IsActive);
                    Check("Jogador sai da Zona Segura quando apaga", !sanity.InSafeZone);
                    Check("Monstro liberado", MonsterRepellent.Find(breaker.Zone.transform.position) == null);
                    Check("Dá para reparar de novo", breaker.CanInteract);
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
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = position;
            player.transform.position = position;
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
            var sb = new StringBuilder($"[Disjuntor] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
