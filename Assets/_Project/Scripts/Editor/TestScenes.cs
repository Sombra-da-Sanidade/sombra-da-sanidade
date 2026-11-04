using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.EditorTools
{
    // Os testes abrem a cena de protótipo. Sem isto, o Unity reabria o protótipo da próxima vez,
    // em vez do mapa do jogo. Ao terminar, cada teste volta para a cena que estava aberta antes
    // (pelo menu) ou deixa a Alfa aberta (em batch, que é a cena que o Unity reabre depois).
    // Também dá um atalho de menu para abrir a Alfa.
    [InitializeOnLoad]
    public static class TestScenes
    {
        private const string ReturnKey = "SombraTests.ReturnScene";
        private const string PendingKey = "SombraTests.Pending";
        private const string ExitCodeKey = "SombraTests.ExitCode";
        private const string BatchKey = "SombraTests.Batch";

        static TestScenes()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Sombra da Sanidade/Abrir cena Alfa", priority = -100)]
        public static void OpenAlfa()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(AlfaSceneBuilder.ScenePath);
        }

        // Chamar antes de o teste abrir a cena dele.
        public static void Remember()
        {
            string path = SceneManager.GetActiveScene().path;
            SessionState.SetString(ReturnKey, string.IsNullOrEmpty(path) ? AlfaSceneBuilder.ScenePath : path);
        }

        // Chamar no fim do teste, no lugar de ExitPlaymode/Exit: sai do Play, reabre a cena e, em batch, fecha o Unity.
        public static void Finish(bool batch, bool failed)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(BatchKey, batch);
            SessionState.SetInt(ExitCodeKey, failed ? 1 : 0);
            EditorApplication.ExitPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            string scene = batch ? AlfaSceneBuilder.ScenePath : SessionState.GetString(ReturnKey, AlfaSceneBuilder.ScenePath);
            if (System.IO.File.Exists(scene)) EditorSceneManager.OpenScene(scene);
            if (batch) EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 0));
        }
    }
}
