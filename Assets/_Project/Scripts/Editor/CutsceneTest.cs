using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Player;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SombraDaSanidade.EditorTools
{
    // RC3, issue #88. Na Ala Médica toca uma cutscene montada em memória com 3 planos (painel com fade, motor
    // focando um CutsceneMarker com cue e glitch, memória com faixa própria) e confere: controle travado, HUD
    // escondida (ModalScreen), mundo congelado, pausa bloqueada, câmera indo ao alvo com zoom, cue disparado,
    // MemoryEffect e faixa da cutscene só na memória, e tudo restaurado no fim. Depois confere o pulo (Skip).
    // Segurar Esc chama o mesmo Skip; conferir à mão no menu de teste.
    // Menu: Sombra da Sanidade > Rodar teste das cutscenes.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.CutsceneTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class CutsceneTest
    {
        private const string RunningKey = "SombraCutsceneTest.Running";
        private const string BatchKey = "SombraCutsceneTest.Batch";
        private const string MusicPath = "Assets/_Project/Audio/Music/memoria_memory.ogg";
        private const string PanelPath = "Assets/_Project/Art/Props/security_camera.png";

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        private static Cutscene scene;
        private static CutsceneMarker marker;
        private static int cues, finished, done;
        private static float baseSize;

        static CutsceneTest()
        {
            if (SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste das cutscenes", priority = 76)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Cutscene] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static Cutscene Build()
        {
            var panel = AssetDatabase.LoadAllAssetsAtPath(PanelPath).OfType<Sprite>().FirstOrDefault();
            var c = ScriptableObject.CreateInstance<Cutscene>();
            c.id = "TESTE";
            c.freezeWorld = true;
            c.music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            c.shots = new[]
            {
                new CutsceneShot { kind = CutsceneShotKind.Panel, image = panel, duration = 0.8f, caption = "SUB-NÍVEL 4.",
                    transition = CutsceneTransition.Fade, zoom = new Vector2(1f, 1.4f) },
                new CutsceneShot { kind = CutsceneShotKind.World, focus = "alvo_teste", cue = "alvo_teste", duration = 1.2f,
                    zoom = new Vector2(1f, 0.7f), speaker = "THOMAS", caption = "Ele não me viu.",
                    transition = CutsceneTransition.Glitch },
                new CutsceneShot { kind = CutsceneShotKind.Panel, memory = true, duration = 0.8f, speaker = "VANCE",
                    caption = "Conte de 10 para trás, Thomas." },
            };
            return c;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.realtimeSinceStartup - stepStart;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var cam = Camera.main;

            switch (step)
            {
                case 0:
                {
                    if (Time.frameCount < 5 || GameManager.Instance == null) return;
                    if (!GameManager.Instance.IsPlaying) { if (t > 20f) { Check("Partida começou", false); Finish(); } return; }
                    if (player == null || cam == null) { Check("Thomas e câmera na cena", false); Finish(); return; }

                    var go = new GameObject("AlvoTeste");
                    go.transform.position = player.transform.position + new Vector3(4f, 2f, 0f);
                    marker = go.AddComponent<CutsceneMarker>();
                    marker.Setup("alvo_teste");
                    cues = finished = done = 0;
                    marker.OnCue.AddListener(() => cues++);
                    CutscenePlayer.Finished -= OnFinished;
                    CutscenePlayer.Finished += OnFinished;

                    baseSize = cam.orthographicSize;
                    scene = Build();
                    Check("Cena de teste com painel e faixa", scene.shots[0].image != null && scene.music != null);
                    CutscenePlayer.Play(scene, () => done++);
                    Next();
                    break;
                }

                case 1:   // painel
                {
                    if (t < 0.3f) return;
                    Check("Cutscene tocando", CutscenePlayer.IsPlaying && CutscenePlayer.Instance.ShotIndex == 0);
                    Check("Controle travado e HUD escondida", PlayerController.InputLocked && ModalScreen.IsOpen);
                    Check("Mundo congelado (timeScale 0)", Mathf.Approximately(Time.timeScale, 0f));
                    Check("Música da fase em modo cutscene", AudioDirector.CutsceneActive);
                    var pause = Object.FindAnyObjectByType<PauseMenu>();
                    if (pause != null)
                    {
                        pause.Open();
                        Check("Esc não abre a pausa durante a cutscene", !pause.IsOpen);
                        if (pause.IsOpen) pause.Close();
                    }
                    Check("Sem efeito de memória no painel do presente", !MemoryEffect.Active);
                    Next();
                    break;
                }

                case 2:   // motor
                {
                    if (CutscenePlayer.Instance.ShotIndex < 1) { if (t > 2f) { Check("Avança para o plano 2", false); Finish(); } return; }
                    if (t < 1.4f) return;
                    var shot = CutscenePlayer.Instance.Shot;
                    Check($"Plano no motor ({CutscenePlayer.Instance.ShotIndex})", shot != null && shot.kind == CutsceneShotKind.World);
                    Check($"Cue disparado no marcador ({cues})", cues == 1);
                    float d = Vector2.Distance(cam.transform.position, marker.transform.position);
                    Check($"Câmera foi até o alvo ({d:0.00} u)", d < 1.5f);
                    Check($"Zoom aproximou ({cam.orthographicSize:0.00} < {baseSize:0.00})", cam.orthographicSize < baseSize);
                    Next();
                    break;
                }

                case 3:   // memória
                {
                    if (CutscenePlayer.Instance.ShotIndex < 2) { if (t > 2f) { Check("Avança para a memória", false); Finish(); } return; }
                    if (t < 0.3f) return;
                    Check("Memória liga o MemoryEffect", MemoryEffect.Active);
                    Check("Faixa da cutscene pedida ao AudioDirector", AudioDirector.CutsceneTrack == scene.music);
                    Next();
                    break;
                }

                case 4:   // fim
                {
                    if (CutscenePlayer.IsPlaying) { if (t > 3f) { Check("Cutscene termina sozinha", false); Finish(); } return; }
                    if (t < 0.1f) return;
                    Check($"Fim avisado ({finished}) e callback chamado ({done})", finished == 1 && done == 1);
                    Check("Controle e HUD de volta", !PlayerController.InputLocked && !ModalScreen.IsOpen);
                    Check($"Tempo volta ao normal ({Time.timeScale})", Mathf.Approximately(Time.timeScale, 1f));
                    Check("Música e memória de volta", !AudioDirector.CutsceneActive && AudioDirector.CutsceneTrack == null && !MemoryEffect.Active);
                    Check($"Zoom restaurado ({cam.orthographicSize:0.00})", Mathf.Approximately(cam.orthographicSize, baseSize));
                    Check("Id marcado como tocado", CutscenePlayer.HasPlayed("TESTE"));
                    CutscenePlayer.Play(scene, () => done++);
                    Next();
                    break;
                }

                case 5:   // pular
                {
                    if (t < 0.2f) return;
                    CutscenePlayer.Skip();
                    Check("Pular encerra na hora", !CutscenePlayer.IsPlaying && done == 2);
                    Check("Pular também restaura o controle e o tempo",
                        !PlayerController.InputLocked && Mathf.Approximately(Time.timeScale, 1f));
                    Finish();
                    break;
                }
            }
        }

        private static void OnFinished(Cutscene c) => finished++;

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
            CutscenePlayer.Finished -= OnFinished;
            if (CutscenePlayer.IsPlaying) CutscenePlayer.Skip();
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[Cutscene] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
