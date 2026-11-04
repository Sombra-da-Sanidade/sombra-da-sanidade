using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 2. Teste do diretor de tensão (#35) na cena de protótipo: sobe com a proximidade,
    // segura o pico, recua o monstro para longe do jogador, volta ao Build Up, decai e reage a falha de skill check.
    // Menu: Sombra da Sanidade > Rodar teste da tensão.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.TensionTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class TensionTest
    {
        private const string RunningKey = "SombraTensionTest.Running";
        private const string BatchKey = "SombraTensionTest.Batch";

        private static int step;
        private static float stepStart;
        private static float mark;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static TensionTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        // Cria Config/TensionConfig.asset e liga no MonsterConfig, para o diretor usar o asset em todas as cenas.
        [MenuItem("Sombra da Sanidade/Criar config do diretor de tensão", priority = 42)]
        public static void EnsureConfigAsset()
        {
            var tension = PrototypeSceneBuilder.GetOrCreateConfig<TensionConfig>("TensionConfig");
            var monster = PrototypeSceneBuilder.GetOrCreateConfig<MonsterConfig>("MonsterConfig");
            if (monster.tension != tension)
            {
                monster.tension = tension;
                EditorUtility.SetDirty(monster);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("TensionConfig ligado no MonsterConfig.");
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da tensão", priority = 63)]
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
            Debug.Log("[Tensão] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var director = TensionDirector.Instance;
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena sem diretor ganha um sozinho", director != null);
                    Check("Cena tem monstro e jogador", monster && player);
                    if (failed) { Finish(); return; }
                    Check("Começa em Build Up com tensão zero", director.Phase == TensionPhase.BuildUp && director.Value < 0.01f);
                    player.enabled = false;
                    // Números rápidos só para o teste (instância nova: o asset não é alterado).
                    var fast = ScriptableObject.CreateInstance<TensionConfig>();
                    fast.proximityGain = 2f;
                    fast.litGain = 0f;
                    fast.chaseGain = 0f;
                    fast.decayPerSecond = 0.5f;
                    fast.relaxDecayPerSecond = 0.5f;
                    fast.sustainPeakSeconds = new Vector2(0.5f, 0.5f);
                    fast.relaxSeconds = new Vector2(3f, 3f);
                    fast.retreatRefreshSeconds = 0.5f;
                    SetField(director, "config", fast);
                    // Atordoado ele fica parado e não mata: dá para medir a proximidade com calma.
                    monster.Stun(60f);
                    Teleport(player, (Vector2)monster.transform.position + Vector2.right * 1.2f);
                    Next();
                    break;

                case 1:
                    if (director.Phase == TensionPhase.BuildUp && t < 3f) return;
                    Check("Monstro perto enche a tensão até o pico", director.Phase == TensionPhase.SustainPeak && director.Value >= 0.99f);
                    Next();
                    break;

                case 2:
                    if (director.Phase != TensionPhase.Relax && t < 2f) return;
                    Check("Segura o pico e recua quando não há perseguição", director.Phase == TensionPhase.Relax);
                    Check("Relax manda o monstro patrulhar longe do jogador", monster.IsRetreating);
                    monster.Stun(0.05f);   // solta o atordoamento: ele volta a patrulhar, agora recuando
                    mark = Vector2.Distance(monster.transform.position, player.transform.position);
                    Next();
                    break;

                case 3:
                    if (t < 1.8f) return;
                    float d = Vector2.Distance(monster.transform.position, player.transform.position);
                    Check($"Monstro se afasta do jogador no Relax ({mark:0.0} -> {d:0.0} m)", d > mark + 1f);
                    Check("Tensão cai no Relax", director.Value < 0.6f);
                    Next();
                    break;

                case 4:
                    if (director.Phase == TensionPhase.Relax && t < 3f) return;
                    Check("Depois do Relax volta ao Build Up", director.Phase == TensionPhase.BuildUp);
                    Check("Monstro volta à patrulha normal", !monster.IsRetreating);
                    // Longe do monstro: a tensão só decai.
                    Vector2 m = monster.transform.position;
                    Teleport(player, new Vector2(m.x > 0f ? -17.5f : 17.5f, m.y > 0f ? -11f : 11f));
                    director.AddSpike(0.6f);
                    mark = director.Value;
                    Next();
                    break;

                case 5:
                    if (t < 0.5f) return;
                    Check($"Longe do monstro a tensão decai ({mark:0.00} -> {director.Value:0.00})", director.Value < mark - 0.1f);
                    mark = director.Value;
                    director.ReportSkillCheckFailure();
                    Check($"Falha de skill check dá um pulo ({mark:0.00} -> {director.Value:0.00})", director.Value > mark + 0.1f);
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
            var sb = new StringBuilder($"[Tensão] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
