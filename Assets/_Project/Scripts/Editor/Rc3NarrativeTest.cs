using System.Collections.Generic;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Validação abrangente da frente RC3 (Cutscenes, Monólogo e Sanidade, #89/#91).
    // Menus:
    // - Sombra da Sanidade > Narrativa > Validar assets RC3
    // - Sombra da Sanidade > Narrativa > Rodar teste Playmode RC3
    [InitializeOnLoad]
    public static class Rc3NarrativeTest
    {
        private const string RunningKey = "SombraRc3Test.Running";

        private static readonly string[] VoiceIds =
        {
            "T1-1", "T1-2", "T1-3", "T1-4", "T1-5", "T1-6",
            "T2-1", "T2-2", "T2-3", "T2-4", "T2-5", "T2-6",
            "T3-1", "T3-2", "T3-3", "T3-4", "T3-5", "T3-6",
            "T4-1", "T4-2"
        };

        private static readonly string[] CutsceneAssets =
        {
            "C0_Abertura", "C1_Especime", "M1_SalaBranca", "I1_AlaEnergia",
            "C2_Sonambulo", "M2_Assinatura", "I2_AlaExecutiva", "M3_Espelho",
            "C3_Revelacao", "C4_Testemunha", "C5_AmostraEntregue"
        };

        private static int step;
        private static float stepStart;
        private static readonly List<string> results = new List<string>();
        private static bool failed;
        private static GameObject testSwapGo;
        private static SpriteRenderer testSwapSr;
        private static Sprite sprA, sprB;

        static Rc3NarrativeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Sombra da Sanidade/Narrativa/Validar assets RC3", priority = 73)]
        public static void ValidateAssets()
        {
            int errors = 0;

            foreach (var id in VoiceIds)
            {
                var v = Resources.Load<InnerVoiceLine>($"Narrative/Voices/{id}");
                if (v == null || v.stableLines.Length == 0 || v.shakenLines.Length == 0)
                {
                    Debug.LogError($"[RC3] Fala ausente ou incompleta em Resources/Narrative/Voices/{id}");
                    errors++;
                    continue;
                }
                if (v.PickLines(false)[0] == v.PickLines(true)[0])
                    Debug.LogWarning($"[RC3] {id} possui textos idênticos para Estável e Abalado.");

                var rt = v.ToRuntimeLog(true);
                if (!rt.isInternalThought || rt.babblePitch >= 1f)
                {
                    Debug.LogError($"[RC3] {id}: configuração de pensamento interno/pitch incorreta.");
                    errors++;
                }
                Object.DestroyImmediate(rt);
            }

            foreach (var name in CutsceneAssets)
            {
                var c = Resources.Load<Cutscene>($"Narrative/Cutscenes/{name}");
                if (c == null || c.shots == null || c.shots.Length == 0)
                {
                    Debug.LogError($"[RC3] Cutscene ausente ou sem planos em Resources/Narrative/Cutscenes/{name}");
                    errors++;
                    continue;
                }
                if (name.StartsWith("M") && c.music == null)
                    Debug.LogWarning($"[RC3] Memória {name} sem trilha musical vinculada.");

                foreach (var s in c.shots)
                {
                    if (s == null || s.duration <= 0f)
                    {
                        Debug.LogError($"[RC3] Plano inválido em {name}.");
                        errors++;
                    }
                }
            }

            // Som das cutscenes (#107): elevador na C0, glitch das memórias, cela da Vance e Thomas uma oitava abaixo.
            var c0 = Resources.Load<Cutscene>("Narrative/Cutscenes/C0_Abertura");
            var sfx = SombraDaSanidade.Audio.GameSfx.Load();
            bool soundOk = c0 != null && c0.shots.Length > 0 && c0.shots[0].sound != null
                && c0.shots[0].sound.name == "elevador"
                && sfx != null && sfx.memoryGlitch != null && sfx.memoryGlitch.HasClips
                && sfx.vanceCellOpen != null && sfx.vanceCellOpen.HasClips
                && Mathf.Approximately(InnerVoiceLine.ThomasPitch * 2f, 1.15f);
            if (!soundOk)
            {
                Debug.LogError("[RC3] Som das cutscenes incompleto (C0 sem elevador, GameSfx sem glitch ou cela, ou pitch do Thomas).");
                errors++;
            }

            if (errors == 0)
                Debug.Log("[RC3] SUCESSO: Todas as 20 falas do Thomas e as 11 cutscenes (planos, trilhas e flags) estão válidas!");
            else
                Debug.LogError($"[RC3] FALHA: {errors} problema(s) encontrado(s). Rode os geradores no menu Narrativa.");
        }

        [MenuItem("Sombra da Sanidade/Narrativa/Rodar teste Playmode RC3", priority = 74)]
        public static void RunPlaymodeTest()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestScenes.Remember();
            EditorSceneManager.OpenScene(PrototypeSceneBuilder.PrototypeScenePath);
            SessionState.SetBool(RunningKey, true);
            step = 0;
            stepStart = 0f;
            results.Clear();
            failed = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float t = Time.time - stepStart;

            switch (step)
            {
                case 0:
                    if (Time.frameCount < 5) return;
                    InnerVoicePlayer.ClearHistory();
                    AudioLogArchive.Clear();

                    var sanity = Object.FindAnyObjectByType<SanityController>();
                    var logPlayer = Object.FindAnyObjectByType<AudioLogPlayer>();
                    Check("Cena tem SanityController e AudioLogPlayer", sanity != null && logPlayer != null);
                    if (failed) { Finish(); return; }

                    // Testa SanitySpriteSwap (< 35)
                    var texA = new Texture2D(4, 4);
                    var texB = new Texture2D(4, 4);
                    sprA = Sprite.Create(texA, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                    sprB = Sprite.Create(texB, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                    testSwapGo = new GameObject("Test_SanitySwap");
                    testSwapSr = testSwapGo.AddComponent<SpriteRenderer>();
                    testSwapSr.sprite = sprA;
                    testSwapGo.AddComponent<SanitySpriteSwap>().Setup(sprA, sprB, 35f);

                    Check("SanitySpriteSwap mantém sprite normal com sanidade cheia", testSwapSr.sprite == sprA);

                    // Testa InnerVoicePlayer fila após log da Vance
                    var vance = AssetDatabase.LoadAssetAtPath<AudioLog>("Assets/_Project/ScriptableObjects/Narrative/Vance01_Triagem.asset");
                    var thomas = Resources.Load<InnerVoiceLine>("Narrative/Voices/T1-1");
                    Check("Assets Vance01 e T1-1 carregados", vance != null && thomas != null);
                    if (failed) { Finish(); return; }

                    logPlayer.Play(vance);
                    InnerVoicePlayer.Play(thomas);
                    Check("InnerVoice aguarda na fila enquanto Vance fala", logPlayer.Current == vance);
                    Next();
                    break;

                case 1:
                    if (t < 0.25f) return;
                    var s = Object.FindAnyObjectByType<SanityController>();
                    s.Add(-75f); // vai para 25 (< 35)
                    Next();
                    break;

                case 2:
                    if (t < 0.1f) return;
                    Check("SanitySpriteSwap troca para sprite abalado abaixo de 35", testSwapSr.sprite == sprB);
                    var lp = Object.FindAnyObjectByType<AudioLogPlayer>();
                    lp.Stop();
                    var thomas2 = Resources.Load<InnerVoiceLine>("Narrative/Voices/T1-2");
                    InnerVoicePlayer.Play(thomas2);
                    Next();
                    break;

                case 3:
                    if (t < 0.15f) return;
                    var lp2 = Object.FindAnyObjectByType<AudioLogPlayer>();
                    Check("InnerVoice toca após log encerrar e usa variante abalada",
                        lp2.IsPlaying && lp2.Current != null && lp2.Current.isInternalThought
                        && lp2.Current.lines[0] == Resources.Load<InnerVoiceLine>("Narrative/Voices/T1-2").shakenLines[0]);
                    Check("Fala interna não polui AudioLogArchive da Vance", !AudioLogArchive.HasHeard(lp2.Current));

                    WingNarrativeDirector.VanceSaved = false;
                    var rescueGo = new GameObject("Test_VanceRescue");
                    rescueGo.AddComponent<BoxCollider2D>().isTrigger = true;
                    var rescue = rescueGo.AddComponent<VanceRescueInteractable>();
                    rescue.CompleteInteract();
                    Check("Interação da Observação resgata Dra. Vance (final C4)", WingNarrativeDirector.VanceSaved && !rescue.CanInteract);
                    WingNarrativeDirector.VanceSaved = false;

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
            results.Add((ok ? "OK   " : "FALHA ") + label);
            if (!ok) failed = true;
        }

        private static void Finish()
        {
            SessionState.SetBool(RunningKey, false);
            EditorApplication.update -= Tick;
            string report = (failed ? "[RC3 Playmode] FALHOU\n" : "[RC3 Playmode] PASSOU\n") + string.Join("\n", results);
            if (failed) Debug.LogError(report);
            else Debug.Log(report);
            TestScenes.Finish(batch: false, failed: failed);
        }
    }
}
