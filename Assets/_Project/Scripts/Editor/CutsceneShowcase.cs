using SombraDaSanidade.Level;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.EditorTools
{
    // Reproduz todas as 11 cutscenes da campanha em ordem cronológica, independente de progresso ou ações do jogador.
    // Troca automaticamente para a ala correspondente quando a cutscene usa planos WorldShot ("especime", "sonambulo", "player").
    [InitializeOnLoad]
    public static class CutsceneShowcase
    {
        private const string RunningKey = "SombraDaSanidade.CutsceneShowcase.Running";
        private const string IndexKey = "SombraDaSanidade.CutsceneShowcase.Index";
        private const string PhaseKey = "SombraDaSanidade.CutsceneShowcase.Phase";
        private const string PendingSceneKey = "SombraDaSanidade.CutsceneShowcase.PendingScene";

        private readonly struct ShowcaseEntry
        {
            public readonly string AssetName;
            public readonly string ScenePath;
            public readonly bool WakeIntro;

            public ShowcaseEntry(string assetName, string scenePath = null, bool wakeIntro = false)
            {
                AssetName = assetName;
                ScenePath = scenePath;
                WakeIntro = wakeIntro;
            }
        }

        private static readonly ShowcaseEntry[] Sequence =
        {
            new ShowcaseEntry("C0_Abertura", AlfaSceneBuilder.AlaMedicaScenePath),
            new ShowcaseEntry("C1_Especime", AlfaSceneBuilder.AlaMedicaScenePath, wakeIntro: true),
            new ShowcaseEntry("M1_SalaBranca"),
            new ShowcaseEntry("I1_AlaEnergia"),
            new ShowcaseEntry("C2_Sonambulo", AlfaSceneBuilder.AlaEnergiaScenePath),
            new ShowcaseEntry("M2_Assinatura"),
            new ShowcaseEntry("I2_AlaExecutiva"),
            new ShowcaseEntry("M3_Espelho", AlfaSceneBuilder.AlaExecutivaScenePath),
            new ShowcaseEntry("C3_Revelacao"),
            new ShowcaseEntry("C4_Testemunha"),
            new ShowcaseEntry("C5_AmostraEntregue"),
        };

        private enum Phase { Prepare = 0, WaitingScene = 1, Settling = 2, Playing = 3 }

        private static int settleFrames;
        private static float cooldownUntil;

        static CutsceneShowcase()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (SessionState.GetBool(RunningKey, false))
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("Sombra da Sanidade/Narrativa/Assistir todas as 11 Cutscenes em ordem", priority = 70)]
        public static void RunAll()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[CutsceneShowcase] Saia do Playmode antes de iniciar a sequência.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);

            SessionState.SetBool(RunningKey, true);
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetInt(PhaseKey, (int)Phase.Prepare);
            SessionState.SetString(PendingSceneKey, string.Empty);

            settleFrames = 3;
            cooldownUntil = 0f;
            WingNarrativeDirector.SuppressAutoTriggers = true;

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            Debug.Log("[CutsceneShowcase] Iniciando reprodução sequencial das 11 cutscenes (Espaço/Clique avança, segure ESC pula a atual).");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                WingNarrativeDirector.SuppressAutoTriggers = true;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                CleanupSession();
                TestScenes.Finish(batch: false, failed: false);
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }
            if (!EditorApplication.isPlaying) return;

            WingNarrativeDirector.SuppressAutoTriggers = true;

            int index = SessionState.GetInt(IndexKey, 0);
            if (index < 0 || index >= Sequence.Length)
            {
                Finish();
                return;
            }

            var entry = Sequence[index];
            var phase = (Phase)SessionState.GetInt(PhaseKey, 0);

            switch (phase)
            {
                case Phase.Prepare:
                {
                    if (Time.realtimeSinceStartup < cooldownUntil) return;

                    if (!string.IsNullOrEmpty(entry.ScenePath) && SceneManager.GetActiveScene().path != entry.ScenePath)
                    {
                        SessionState.SetString(PendingSceneKey, entry.ScenePath);
                        SessionState.SetInt(PhaseKey, (int)Phase.WaitingScene);
                        EditorSceneManager.LoadSceneInPlayMode(entry.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                        return;
                    }

                    settleFrames = 3;
                    SessionState.SetInt(PhaseKey, (int)Phase.Settling);
                    break;
                }

                case Phase.WaitingScene:
                {
                    string pending = SessionState.GetString(PendingSceneKey, string.Empty);
                    if (!string.IsNullOrEmpty(pending) && SceneManager.GetActiveScene().path != pending) return;

                    SessionState.SetString(PendingSceneKey, string.Empty);
                    settleFrames = 3;
                    SessionState.SetInt(PhaseKey, (int)Phase.Settling);
                    break;
                }

                case Phase.Settling:
                {
                    if (entry.WakeIntro && settleFrames == 3)
                        Object.FindAnyObjectByType<WingIntro>()?.WakeNow();

                    settleFrames--;
                    if (settleFrames > 0) return;

                    if (AudioLogPlayer.Instance != null && AudioLogPlayer.Instance.IsPlaying)
                        AudioLogPlayer.Instance.Stop();

                    var scene = WingNarrativeDirector.LoadCutscene(entry.AssetName);
                    if (scene == null)
                    {
                        Debug.LogWarning($"[CutsceneShowcase] Cutscene '{entry.AssetName}' não encontrada em Resources/Narrative/Cutscenes.");
                        AdvanceToNext(index);
                        return;
                    }

                    Debug.Log($"[CutsceneShowcase] ({index + 1}/{Sequence.Length}) Tocando {entry.AssetName}");
                    CutscenePlayer.Play(scene);
                    SessionState.SetInt(PhaseKey, (int)Phase.Playing);
                    break;
                }

                case Phase.Playing:
                {
                    if (CutscenePlayer.IsPlaying) return;
                    AdvanceToNext(index);
                    break;
                }
            }
        }

        private static void AdvanceToNext(int currentIndex)
        {
            int next = currentIndex + 1;
            if (next >= Sequence.Length)
            {
                Finish();
                return;
            }

            SessionState.SetInt(IndexKey, next);
            SessionState.SetInt(PhaseKey, (int)Phase.Prepare);
            cooldownUntil = Time.realtimeSinceStartup + 0.25f;
        }

        private static void Finish()
        {
            Debug.Log("[CutsceneShowcase] Sequência das 11 cutscenes concluída.");
            CleanupSession();
            TestScenes.Finish(batch: false, failed: false);
        }

        private static void CleanupSession()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetString(PendingSceneKey, string.Empty);
            WingNarrativeDirector.SuppressAutoTriggers = false;
        }
    }
}
