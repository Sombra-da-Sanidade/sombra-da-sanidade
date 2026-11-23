using System.Collections.Generic;
using System.IO;
using SombraDaSanidade.Audio;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera as 11 cutscenes do RC (#91) a partir de docs/narrativa/roteiro-rc.md seção 5. Os assets ficam em
    // Resources/Cutscenes/<id>.asset (C0, I1 e I2 são carregados em runtime pelo WingOpening); as alas acham
    // as outras pelo id quando são geradas. Painéis (#98): Art/Cutscenes/<id>_<n>.png (n a partir de 1) entra
    // sozinho; sem o PNG o plano vira intertítulo (tela preta com a legenda). Mudar texto = editar aqui e gerar
    // de novo. Menu: Sombra da Sanidade > Narrativa > Gerar cutscenes.
    public static class CutsceneAssetBuilder
    {
        public const string Folder = "Assets/_Project/Resources/Cutscenes";
        private const string PanelFolder = "Assets/_Project/Art/Cutscenes";
        private const string Sfx = "Assets/_Project/ScriptableObjects/Audio/";
        private const string Music = "Assets/_Project/Audio/Music/";

        public static readonly string[] Ids = { "C0", "C1", "M1", "I1", "C2", "M2", "I2", "M3", "C3", "C4", "C5" };

        // Um plano do roteiro. kind: P painel, W motor (focus), M memória (painel com MemoryEffect).
        private struct S
        {
            public char kind; public string focus, speaker, caption, sound, cue;
            public float duration; public Vector2 zoom, panFrom, panTo; public CutsceneTransition transition;
        }

        private static S P(string caption, float d = 4f, string sound = null, CutsceneTransition t = CutsceneTransition.Fade) =>
            new S { kind = 'P', caption = caption, duration = d, sound = sound, transition = t, zoom = new Vector2(1f, 1.08f),
                    panFrom = new Vector2(-0.03f, 0f), panTo = new Vector2(0.03f, 0f) };

        private static S M(string speaker, string caption, float d = 3.5f, string sound = null,
            CutsceneTransition t = CutsceneTransition.Cut) =>
            new S { kind = 'M', speaker = speaker, caption = caption, duration = d, sound = sound, transition = t,
                    zoom = new Vector2(1.1f, 1.25f) };

        private static S W(string focus, string speaker, string caption, float d, float zoomFrom, float zoomTo,
            string sound = null, string cue = "", CutsceneTransition t = CutsceneTransition.Cut) =>
            new S { kind = 'W', focus = focus, speaker = speaker, caption = caption, duration = d, sound = sound,
                    cue = cue, transition = t, zoom = new Vector2(zoomFrom, zoomTo) };

        private class Def
        {
            public string id, music; public bool freeze = true; public S[] shots;
        }

        private static IEnumerable<Def> Script()
        {
            const string crt = "UI/crt_liga", whisper = "Sanity/alucinacao_sussurro";
            yield return new Def { id = "C0", shots = new[] {
                P("SUB-NÍVEL 4. COMPLEXO EREBUS. 1994.", 4f, crt),
                P("AUDITOR T. REIS: RECUPERAR OS HDS 01, 02 E 03 DO PROJETO HIPNOS.", 4.5f),
                P("A BASE ESTÁ EM CONFINAMENTO. NENHUMA EQUIPE RESPONDE.", 4f),
                P("A AETHELGARD AGRADECE A SUA COLABORAÇÃO.", 4f, "World/eclusa_liga"),
            }};
            yield return new Def { id = "C1", freeze = false, shots = new[] {
                W("especime", "", "", 2.5f, 1f, 0.85f, "Monster/especime_respiracao", t: CutsceneTransition.Fade),
                W("especime", "", "", 2f, 0.85f, 0.75f, "Monster/especime_estalo"),
                W("especime", "", "", 1.6f, 0.75f, 0.55f, "Monster/especime_grito"),
                W("player", "THOMAS", "Ele não me viu.", 2.5f, 1f, 1f),
            }};
            yield return new Def { id = "M1", music = "memoria_memory", shots = new[] {
                M("", "", 2f, whisper, CutsceneTransition.Fade),
                M("VANCE", "Vai doer menos se você não lutar.", 3.5f),
                M("VANCE", "Conte de 10 para trás, Thomas.", 3.5f),
                M("THOMAS", "Dez. Nove. Oito.", 3f, t: CutsceneTransition.Glitch),
            }};
            yield return new Def { id = "I1", shots = new[] {
                P("A ALA DE ENERGIA FICA ABAIXO. ALGO ANDA NO ESCURO DE LÁ.", 4.5f, crt),
            }};
            yield return new Def { id = "C2", freeze = false, shots = new[] {
                W("c2_lampada", "", "", 2.5f, 0.9f, 0.8f, "Monster/sonambulo_respiracao", t: CutsceneTransition.Fade),
                W("c2_lampada", "", "", 1.2f, 0.8f, 0.8f, "Camera/camera_apagao", "c2_lampada"),
                W("c2_lampada", "THOMAS", "Ele se mexeu. Quando a luz apagou, ele se mexeu.", 3.5f, 0.8f, 0.7f,
                    "Monster/sonambulo_rangido"),
            }};
            yield return new Def { id = "M2", music = "memoria_memory", shots = new[] {
                M("", "TERMO DE PARTICIPAÇÃO VOLUNTÁRIA. PROJETO HIPNOS.", 3.5f, whisper, CutsceneTransition.Fade),
                M("VANCE", "Você leu tudo? Pode desistir agora.", 3.5f),
                M("THOMAS", "Eu preciso do dinheiro.", 3f),
                M("", "THOMAS REIS", 2.5f, t: CutsceneTransition.Glitch),
            }};
            yield return new Def { id = "I2", shots = new[] {
                P("ÚLTIMO HD: ALA EXECUTIVA. OS DOIS ESTÃO SOLTOS.", 4.5f, crt),
            }};
            yield return new Def { id = "M3", music = "memoria_memory", shots = new[] {
                W("player", "", "", 2.5f, 1f, 0.7f, whisper, t: CutsceneTransition.Fade),
                M("", "", 2f),
                M("VANCE", "Sessão final, Voluntário 08.", 3.5f, t: CutsceneTransition.Glitch),
            }};
            yield return new Def { id = "C3", music = "revelacao", shots = new[] {
                P("VOLUNTÁRIO 08. NOME: THOMAS REIS. STATUS: EM TESTE.", 4.5f, crt),
                P("RESPONSÁVEL: DRA. V. VANCE.", 3.5f),
                W("player", "VANCE", "Thomas. Não é gravação. Estou viva.", 3.5f, 1f, 0.9f, whisper, t: CutsceneTransition.Glitch),
                W("player", "VANCE", "Você não é auditor. A auditoria é o teste. Eu apaguei sua memória.", 4.5f, 0.9f, 0.85f),
                W("player", "VANCE", "Estou presa na observação. A Aethelgard vai purgar a base.", 4f, 0.85f, 0.8f),
                W("player", "VANCE", "Vá para a eclusa. Ou venha me buscar. A escolha é sua.", 4f, 0.8f, 0.75f, "World/eclusa_liga"),
            }};
            yield return new Def { id = "C4", music = "final_testemunha", shots = new[] {
                P("", 3f, "World/vitoria_eclusa"),
                P("Com isto, a Aethelgard não apaga mais ninguém.", 4f).Speaker("VANCE"),
                P("AETHELGARD DYNAMICS SOB INVESTIGAÇÃO. TESTEMUNHA SOBREVIVENTE.", 4.5f),
            }};
            yield return new Def { id = "C5", music = "final_amostra", shots = new[] {
                P("", 3f, "World/vitoria_eclusa"),
                P("Voluntário 08. Bom trabalho.", 3.5f).Speaker("AGENTE"),
                P("TESTE CONCLUÍDO. INICIAR VOLUNTÁRIO 09.", 4.5f),
            }};
        }

        private static S Speaker(this S s, string who) { s.speaker = who; return s; }

        [MenuItem("Sombra da Sanidade/Narrativa/Gerar cutscenes", priority = 40)]
        public static void BuildFromMenu() => Build();

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.CutsceneAssetBuilder.Build
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            foreach (var def in Script()) Write(def);
            AssetDatabase.SaveAssets();
            Debug.Log($"Cutscenes geradas em {Folder} ({Ids.Length}).");
        }

        public static Cutscene Load(string id) => AssetDatabase.LoadAssetAtPath<Cutscene>($"{Folder}/{id}.asset");

        private static void Write(Def def)
        {
            string path = $"{Folder}/{def.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<Cutscene>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<Cutscene>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = def.id;
            asset.freezeWorld = def.freeze;
            asset.music = def.music == null ? null : LoadMusic(def.music);
            var shots = new List<CutsceneShot>();
            int panel = 0;
            foreach (var s in def.shots)
            {
                var shot = new CutsceneShot
                {
                    kind = s.kind == 'W' ? CutsceneShotKind.World : CutsceneShotKind.Panel,
                    focus = s.kind == 'W' ? s.focus : "player",
                    zoom = s.zoom == Vector2.zero ? Vector2.one : s.zoom,
                    panFrom = s.panFrom,
                    panTo = s.panTo,
                    duration = s.duration,
                    speaker = s.speaker ?? "",
                    caption = s.caption ?? "",
                    sound = s.sound == null ? null : LoadSound(s.sound),
                    transition = s.transition,
                    memory = s.kind == 'M',
                    cue = s.cue ?? "",
                };
                if (shot.kind == CutsceneShotKind.Panel)
                    shot.image = AssetDatabase.LoadAssetAtPath<Sprite>($"{PanelFolder}/{def.id}_{++panel}.png");
                shots.Add(shot);
            }
            asset.shots = shots.ToArray();
            EditorUtility.SetDirty(asset);
        }

        private static SoundEvent LoadSound(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<SoundEvent>($"{Sfx}{name}.asset");
            if (s == null) Debug.LogWarning($"Cutscenes: som {name} não encontrado.");
            return s;
        }

        private static AudioClip LoadMusic(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Music}{name}.ogg");
            if (c == null) Debug.LogWarning($"Cutscenes: música {name} não encontrada.");
            return c;
        }
    }
}
