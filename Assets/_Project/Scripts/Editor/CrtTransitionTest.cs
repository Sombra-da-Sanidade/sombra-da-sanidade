using System.Collections.Generic;
using System.Text;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Frente 5, issue #94. Teste da transição CRT na Ala Médica: a forma da faixa (tela inteira → linha → ponto),
    // a abertura ao entrar no jogo, o apagão do Game Over que abre sobre a tela de fim, e o reinício que só
    // carrega a cena com a tela apagada e abre na cena nova com o tempo normal.
    // Menu: Sombra da Sanidade > Rodar teste da transição CRT.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.CrtTransitionTest.RunBatch (sem -quit; sai com 0 ou 1).
    [InitializeOnLoad]
    public static class CrtTransitionTest
    {
        private const string RunningKey = "SombraCrtTransitionTest.Running";
        private const string BatchKey = "SombraCrtTransitionTest.Batch";

        private static int step;
        private static float stepStart;
        private static float maxAmount;
        private static GameManager firstGm;
        private static readonly List<string> results = new List<string>();
        private static readonly List<SoundEvent> played = new List<SoundEvent>();

        private static void OnPlayed(SoundEvent sound, AudioSource source) => played.Add(sound);
        private static bool failed;

        static CrtTransitionTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Rodar teste da transição CRT", priority = 71)]
        public static void Run() => Start(batch: false);

        public static void RunBatch() => Start(batch: true);

        private static void Start(bool batch)
        {
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            results.Clear();
            failed = false;

            // Antes do Play: a forma da faixa numa tela de 640x360 com linha de 2 px.
            const float w = 640f, h = 360f, line = 2f, share = 0.6f;
            var open = CrtTransition.Band(0f, w, h, line, share);
            Check("Aberta: a faixa é a tela inteira", open == new Rect(0f, 0f, w, h));
            var half = CrtTransition.Band(0.3f, w, h, line, share);
            Check($"Fechando: a altura cai e a largura fica ({half.width}x{half.height})",
                half.height < h && half.height > line && Mathf.Approximately(half.width, w));
            var flat = CrtTransition.Band(share, w, h, line, share);
            Check($"No fim do achatamento sobra uma linha de 2 px na largura toda ({flat.width}x{flat.height})",
                Mathf.Approximately(flat.height, line) && Mathf.Approximately(flat.width, w));
            var shrinking = CrtTransition.Band(0.8f, w, h, line, share);
            Check($"Depois a linha encolhe para o centro ({shrinking.width} px)",
                shrinking.width < w && shrinking.width > 0f && Mathf.Approximately(shrinking.center.x, w / 2f));
            Check("Fechada: largura zero", CrtTransition.Band(1f, w, h, line, share).width == 0f);

            TestScenes.Remember();
            EditorSceneManager.OpenScene(AlfaSceneBuilder.AlaMedicaScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(BatchKey, batch);
            step = 0;
            stepStart = 0f;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[CRT] Iniciando teste");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (stepStart <= 0f) stepStart = Time.realtimeSinceStartup;
            var gm = GameManager.Instance;
            var config = CrtTransitionConfig.Load();
            float t = Time.realtimeSinceStartup - stepStart;

            switch (step)
            {
                case 0:   // Entrar no jogo: a cena abre da linha.
                    if (Time.frameCount < 5 || gm == null) return;
                    if (CrtTransition.Busy && t < 5f) return;
                    Check("Transição criada sozinha e mantida entre cenas", CrtTransition.Instance != null);
                    Check($"A cena abre e a tela fica livre ({CrtTransition.Current}, {CrtTransition.Amount:0.00})",
                        !CrtTransition.Busy && CrtTransition.Amount == 0f);
                    if (failed) { Finish(); return; }

                    AudioService.Played -= OnPlayed;
                    AudioService.Played += OnPlayed;
                    played.Clear();
                    gm.TriggerGameOver(DeathCause.Monster);
                    Check("Game Over começa a fechar a tela", CrtTransition.Current == CrtTransition.Phase.Closing);
                    Check("A tela de fim fica escondida enquanto fecha (fecha a imagem da morte)", CrtTransition.HidingEndScreen);
                    maxAmount = 0f;
                    Next();
                    break;

                case 1:   // Apagão do Game Over e abertura sobre a tela de fim.
                    maxAmount = Mathf.Max(maxAmount, CrtTransition.Amount);
                    if ((CrtTransition.Busy || maxAmount < 1f) && t < 5f) return;
                    Check("Game Over apaga a tela por completo", maxAmount >= 1f);
                    Check("Depois abre sobre a tela de fim (sem trocar de cena)",
                        !CrtTransition.Busy && gm != null && gm.State == GameState.GameOver);
                    Check("A tela de fim aparece quando a linha abre", !CrtTransition.HidingEndScreen);
                    var sfx = GameSfx.Load();
                    Check("Apagão do Game Over sem som de tubo (tocam o grito e o sting)",
                        !played.Contains(sfx.crtOff) && !played.Contains(sfx.crtOn));
                    played.Clear();

                    firstGm = gm;
                    gm.Restart();
                    Check("Reiniciar não troca a cena na hora: fecha a tela primeiro",
                        CrtTransition.PendingScene == firstGm.gameObject.scene.name && GameManager.Instance == firstGm);
                    Check("A imagem congela enquanto fecha", Time.timeScale == 0f);
                    Check("Trocar de cena toca o tubo desligando", played.Contains(GameSfx.Load().crtOff));
                    played.Clear();
                    gm.Restart();   // R apertado de novo durante a troca: ignorado
                    Next();
                    break;

                case 2:   // A cena nova carregou.
                    if ((gm == null || gm == firstGm) && t < 5f) return;
                    Check("A cena recarrega", gm != null && gm != firstGm);
                    Check($"A cena só carrega com a tela apagada ({CrtTransition.CoverageAtLastLoad:0.00})",
                        CrtTransition.CoverageAtLastLoad >= 1f);
                    Check($"Fechar leva o tempo do config ({t:0.00} s, config {config.closeSeconds} s)",
                        t >= config.closeSeconds * 0.8f && t < config.closeSeconds + 1.5f);
                    Check($"A cena nova começa apagada ({CrtTransition.Amount:0.00})", CrtTransition.Amount >= 1f);
                    Check("Tempo normal na cena nova", Time.timeScale == 1f);
                    Next();
                    break;

                case 3:   // Abertura termina.
                    if (CrtTransition.Busy && t < 5f) return;
                    Check($"A abertura termina e a tela fica livre ({t:0.00} s)", !CrtTransition.Busy && CrtTransition.Amount == 0f);
                    // O quadro travado do carregamento não pode engolir a abertura (bug: abria em 0 s).
                    float expected = config.loadHoldSeconds + config.openSeconds;
                    Check($"A abertura dura o tempo do config ({t:0.00} s, config {expected:0.00} s)",
                        t >= expected * 0.8f && t < expected + 1.5f);
                    Check("A cena nova abre com o tubo ligando", played.Contains(GameSfx.Load().crtOn));
                    Check("Nenhuma troca de cena sobrando (o segundo R foi ignorado)", CrtTransition.PendingScene == null
                        && GameManager.Instance != null && GameManager.Instance.IsPlaying);
                    Finish();
                    break;
            }
        }

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
            AudioService.Played -= OnPlayed;
            EditorApplication.update -= Tick;
            SessionState.SetBool(RunningKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            var sb = new StringBuilder($"[CRT] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            TestScenes.Finish(batch, failed);
        }
    }
}
