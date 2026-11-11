using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5. Teste do DifficultyProfile (#30) na cena de protótipo: sem perfil nada muda, com perfil monstro
    // e sanidade leem da ala, o bônus por HD não soma, as alucinações seguem o perfil e armários sem tabela usam a da ala.
    // Menu: Sombra da Sanidade > Rodar teste de dificuldade.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.DifficultyProfileTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class DifficultyProfileTest
    {
        private const string RunningKey = "SombraDifficultyTest.Running";
        private const string BatchKey = "SombraDifficultyTest.Batch";
        private const string PrevSafeModeKey = "SombraDifficultyTest.SafeMode";
        private const string ConfigFolder = "Assets/_Project/ScriptableObjects/Config/";

        private static int step;
        private static float stepStart;
        private static int profileEvents;
        private static DifficultyProfile testProfile;
        private static LootTable ownTable;
        private static Locker plainLocker;
        private static Locker ownLocker;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        static DifficultyProfileTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste de dificuldade", priority = 67)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(PrevSafeModeKey, MonsterSafeMode.Enabled);
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Dificuldade] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var monster = Object.FindAnyObjectByType<MonsterController>();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            var hallucination = Object.FindAnyObjectByType<SanityHallucination>();
            var gm = GameManager.Instance;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    Check("Cena tem monstro, sanidade, alucinação e GameManager", monster && sanity && hallucination && gm);
                    if (failed) { Finish(); return; }
                    MonsterSafeMode.Enabled = false;
                    if (TensionDirector.Instance != null) TensionDirector.Instance.enabled = false;
                    monster.enabled = false;   // parado e sem perseguir: os testes não dependem de onde ele anda

                    CheckAssets();

                    // Sem perfil: valores do config.
                    Check("Sem perfil, ActiveProfile é vazio", gm.ActiveProfile == null);
                    Check("Sem perfil, a velocidade é a do config",
                        Mathf.Approximately(monster.CurrentSpeed, monster.Config.baseSpeed));
                    Check("Sem perfil, o raio auditivo é o do config",
                        Mathf.Approximately(monster.HearingRadius, monster.Config.hearingRadius));
                    Check("Sem perfil, o dreno é o do config",
                        Mathf.Approximately(sanity.DarknessDrain, sanity.Config.darknessDrain));

                    // Com perfil: valores da ala e evento de troca.
                    var energia = LoadAsset("DifficultyEnergia");
                    profileEvents = 0;
                    gm.ProfileChanged += CountProfileEvent;
                    gm.SetProfile(energia);
                    gm.SetProfile(energia);   // repetido não avisa de novo
                    Check("Evento de troca dispara uma vez", profileEvents == 1 && gm.ActiveProfile == energia);
                    Check($"Monstro usa a velocidade da ala ({monster.CurrentSpeed:0.00})",
                        Mathf.Approximately(monster.CurrentSpeed, energia.monsterSpeed));
                    Check("Monstro usa o raio auditivo da ala",
                        Mathf.Approximately(monster.HearingRadius, energia.monsterHearingRadius));
                    Check("Monstro usa os comportamentos da ala", monster.Behaviours == MonsterWingBehaviours.Energia);
                    Check("Sanidade usa o dreno da ala", Mathf.Approximately(sanity.DarknessDrain, energia.darknessDrain));

                    // O aumento por HD já está na tabela: não pode somar por cima.
                    monster.SetSpeedMultiplier(1.5f);
                    Check("Bônus por HD não soma com o perfil",
                        Mathf.Approximately(monster.CurrentSpeed, energia.monsterSpeed));
                    monster.SetSpeedMultiplier(1f);

                    // Voltar para vazio restaura os configs.
                    gm.SetProfile(null);
                    gm.ProfileChanged -= CountProfileEvent;
                    Check("Sem perfil de novo, volta ao config",
                        Mathf.Approximately(monster.CurrentSpeed, monster.Config.baseSpeed)
                        && monster.Behaviours == monster.Config.behaviours);

                    // Alucinações: perfil desligado, com sanidade baixa.
                    testProfile = ScriptableObject.CreateInstance<DifficultyProfile>();
                    testProfile.hallucinations = false;
                    testProfile.hallucinationInterval = new Vector2(0.1f, 0.2f);
                    testProfile.darknessDrain = 0f;
                    testProfile.monsterSpeed = 3.5f;
                    testProfile.monsterHearingRadius = 5f;
                    testProfile.monsterBehaviours = MonsterWingBehaviours.Medica;
                    gm.SetProfile(testProfile);
                    sanity.Add(-(sanity.Value - (sanity.Config.hallucinationsBelow - 15f)));
                    Next();
                    break;

                case 1:
                    if (hallucination.IsShowing)
                    {
                        Check("Perfil sem alucinações não mostra a silhueta", false);
                        testProfile.hallucinations = true;
                        Next();
                        return;
                    }
                    if (t < 1f) return;
                    Check("Perfil sem alucinações não mostra a silhueta", true);
                    testProfile.hallucinations = true;
                    Next();
                    break;

                case 2:
                    // Intervalo de 0.1-0.2 s do perfil; o do config é de 15 a 25 s, então só passa lendo do perfil.
                    if (!hallucination.IsShowing && t < 2f) return;
                    Check($"Perfil com alucinações usa o intervalo da ala ({t:0.0} s)", hallucination.IsShowing);
                    testProfile.hallucinations = false;

                    // Armários: um armário novo, sem tabela própria, lê a do perfil no Start (como ao entrar na ala).
                    var locker = Object.FindAnyObjectByType<Locker>();
                    Check("Cena tem armário", locker);
                    if (failed) { Finish(); return; }
                    testProfile.lootTable = LoadAsset("DifficultyExecutiva").lootTable;
                    ownTable = ScriptableObject.CreateInstance<LootTable>();
                    plainLocker = Object.Instantiate(locker);
                    plainLocker.SetTable(null);
                    ownLocker = Object.Instantiate(locker);
                    ownLocker.SetTable(ownTable);
                    Next();
                    break;

                case 3:
                    if (t < 0.2f) return;
                    Check("Armário sem tabela usa a tabela da ala", plainLocker.Table == testProfile.lootTable);
                    Check("Armário com tabela própria mantém a dele", ownLocker.Table == ownTable);
                    Object.Destroy(plainLocker.gameObject);
                    Object.Destroy(ownLocker.gameObject);
                    Object.Destroy(ownTable);
                    Object.Destroy(testProfile);
                    Finish();
                    break;
            }
        }

        // Os 4 assets com a tabela da Proposta 1. A chance de bateria vem da tabela de loot da ala.
        private static void CheckAssets()
        {
            CheckAsset("Medica", 3.5f, 5f, 1f, 0.70f, false, Vector2.zero, MonsterWingBehaviours.Medica);
            CheckAsset("Energia", 3.85f, 6f, 1.5f, 0.60f, true, new Vector2(20f, 30f), MonsterWingBehaviours.Energia);
            CheckAsset("Executiva", 4.2f, 7f, 2f, 0.45f, true, new Vector2(15f, 25f), MonsterWingBehaviours.Executiva);
            CheckAsset("Eclusa", 4.6f, 9f, 2f, 0f, true, new Vector2(10f, 15f), MonsterWingBehaviours.Executiva);
        }

        private static void CheckAsset(string wing, float speed, float hearing, float drain, float battery,
            bool hallucinations, Vector2 interval, MonsterBehaviour behaviours)
        {
            var p = LoadAsset("Difficulty" + wing);
            var loot = AssetDatabase.LoadAssetAtPath<LootTable>(ConfigFolder + "Loot/LootTable_" + wing + ".asset");
            bool ok = p != null
                && Mathf.Approximately(p.monsterSpeed, speed)
                && Mathf.Approximately(p.monsterHearingRadius, hearing)
                && Mathf.Approximately(p.darknessDrain, drain)
                && p.hallucinations == hallucinations
                && (!hallucinations || p.hallucinationInterval == interval)
                && p.monsterBehaviours == behaviours
                && p.lootTable != null && p.lootTable == loot
                && Mathf.Abs(p.lootTable.Chance(LootKind.Battery) - battery) < 0.01f;
            Check($"Difficulty{wing} tem os valores da Proposta 1 e a tabela de loot da ala", ok);
        }

        private static DifficultyProfile LoadAsset(string name) =>
            AssetDatabase.LoadAssetAtPath<DifficultyProfile>(ConfigFolder + name + ".asset");

        private static void CountProfileEvent(DifficultyProfile profile) => profileEvents++;

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
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            MonsterSafeMode.Enabled = SessionState.GetBool(PrevSafeModeKey, false);   // não muda a preferência de quem testou
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Dificuldade] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
