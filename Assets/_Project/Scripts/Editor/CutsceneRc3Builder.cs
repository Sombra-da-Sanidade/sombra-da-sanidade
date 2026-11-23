using System.IO;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera os painéis 320x180, as 11 Cutscenes (#91) e posiciona gatilhos/pistas nas 3 alas (#89, #91).
    // Menu: Sombra da Sanidade > Narrativa > ...
    public static class CutsceneRc3Builder
    {
        public const string PanelsDir = "Assets/_Project/Art/Cutscenes";
        public const string SanityPropsDir = "Assets/_Project/Art/Props/Sanity";
        public const string CutscenesDir = "Assets/_Project/Resources/Narrative/Cutscenes";

        private const int W = 320, H = 180;

        // Paleta fria do presente (art-src/palette.json)
        private static readonly Color32 Vazio = Hex(0x0b0c10);
        private static readonly Color32 Concreto2 = Hex(0x222630);
        private static readonly Color32 Concreto4 = Hex(0x444b59);
        private static readonly Color32 Concreto6 = Hex(0x7b8390);
        private static readonly Color32 Concreto8 = Hex(0xd3d6da);
        private static readonly Color32 Fosforo2 = Hex(0x1a4d3a);
        private static readonly Color32 Fosforo4 = Hex(0x55b870);
        private static readonly Color32 Fosforo5 = Hex(0xa9f294);
        private static readonly Color32 Emergencia4 = Hex(0xdf4139);
        private static readonly Color32 Ambar2 = Hex(0xf6d878);

        // Paleta própria de 4 cores das memórias (docs/planejamento-rc.md seção 5)
        private static readonly Color32 MemDark = Hex(0x0d1915);
        private static readonly Color32 MemMid = Hex(0x1e4d3b);
        private static readonly Color32 MemGreen = Hex(0x74d482);
        private static readonly Color32 MemWhite = Hex(0xecf7ee);

        [MenuItem("Sombra da Sanidade/Narrativa/Gerar todas as 11 Cutscenes e Painéis (RC3)", priority = 71)]
        public static void BuildAllCutscenes()
        {
            Directory.CreateDirectory(PanelsDir);
            Directory.CreateDirectory(CutscenesDir);

            // Painéis 320x180
            var pC0_1 = BuildPanel("C0_01_elevador", DrawC0Elevator);
            var pC0_2 = BuildPanel("C0_02_monitor", DrawC0Monitor);
            var pC0_3 = BuildPanel("C0_03_lanterna", DrawC0Hand);
            var pC0_4 = BuildPanel("C0_04_porta", DrawC0Door);

            var pM1_1 = BuildPanel("M1_01_luz", DrawM1Light);
            var pM1_2 = BuildPanel("M1_02_seringa", DrawM1Syringe);
            var pM1_3 = BuildPanel("M1_03_vance", DrawM1Vance);
            var pM1_4 = BuildPanel("M1_04_vance_escuro", DrawM1VanceDark);

            var pM2_1 = BuildPanel("M2_01_termo", DrawM2Term);
            var pM2_2 = BuildPanel("M2_02_caneta", DrawM2Pen);
            var pM2_3 = BuildPanel("M2_03_assinatura", DrawM2Sign);
            var pM2_4 = BuildPanel("M2_04_nome", DrawM2Name);

            var pM3_2 = BuildPanel("M3_02_reflexo", DrawM3Reflection);
            var pM3_3 = BuildPanel("M3_03_pulseira", DrawM3Wristband);

            var pC3_1 = BuildPanel("C3_01_ficha", DrawC3Dossier);
            var pC3_2 = BuildPanel("C3_02_vance_assinatura", DrawC3Signature);

            var pC4_1 = BuildPanel("C4_01_neve", DrawC4Snow);
            var pC4_2 = BuildPanel("C4_02_hds", DrawC4Drives);
            var pC4_3 = BuildPanel("C4_03_jornal", DrawC4Paper);

            var pC5_1 = BuildPanel("C5_01_sozinho", DrawC5Alone);
            var pC5_2 = BuildPanel("C5_02_furgao", DrawC5Van);
            var pC5_3 = BuildPanel("C5_03_aprovado", DrawC5Approved);

            AssetDatabase.Refresh();

            var memMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/memoria_memory.ogg");
            var revMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/revelacao.ogg");
            var winWitnessMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/final_testemunha.ogg");
            var winSampleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/final_amostra.ogg");

            // 1. C0 Abertura
            SaveCutscene("C0_Abertura", "C0", true, null,
                PanelShot(pC0_1, 4.2f, "", "SUB-NÍVEL 4. COMPLEXO EREBUS. 1994.", CutsceneTransition.Fade, new Vector2(1f, 1.08f)),
                PanelShot(pC0_2, 4.8f, "", "AUDITOR T. REIS: RECUPERAR OS HDS 01, 02 E 03 DO PROJETO HIPNOS."),
                PanelShot(pC0_3, 4.5f, "", "A BASE ESTÁ EM CONFINAMENTO. NENHUMA EQUIPE RESPONDE."),
                PanelShot(pC0_4, 4.2f, "", "A AETHELGARD AGRADECE A SUA COLABORAÇÃO.", CutsceneTransition.Fade, new Vector2(1f, 1.15f))
            );

            // 2. C1 Espécime (World, freezeWorld = false)
            SaveCutscene("C1_Especime", "C1", false, null,
                WorldShot("especime", 2.2f, "", "", CutsceneTransition.Fade, new Vector2(1f, 0.85f)),
                WorldShot("especime", 1.8f, "", "", CutsceneTransition.Cut, new Vector2(0.85f, 0.72f)),
                WorldShot("especime", 2.4f, "THOMAS", "Ele não me viu.", CutsceneTransition.Cut, new Vector2(0.72f, 0.62f))
            );

            // 3. M1 Sala Branca
            SaveCutscene("M1_SalaBranca", "M1", true, memMusic,
                MemShot(pM1_1, 2.0f, "", "", CutsceneTransition.Glitch),
                MemShot(pM1_2, 3.0f, "VANCE", "Vai doer menos se você não lutar."),
                MemShot(pM1_3, 3.2f, "VANCE", "Conte de 10 para trás, Thomas."),
                MemShot(pM1_4, 2.8f, "THOMAS", "Dez. Nove. Oito.", CutsceneTransition.Glitch)
            );

            // 4. I1 Intertítulo
            SaveCutscene("I1_AlaEnergia", "I1", true, null,
                PanelShot(null, 3.2f, "", "A ALA DE ENERGIA FICA ABAIXO. ALGO ANDA NO ESCURO DE LÁ.", CutsceneTransition.Fade)
            );

            // 5. C2 Sonâmbulo (World, cue = c2_lampada)
            SaveCutscene("C2_Sonambulo", "C2", false, null,
                WorldShot("sonambulo", 2.0f, "", "", CutsceneTransition.Fade, new Vector2(1f, 0.9f)),
                WorldShot("sonambulo", 1.2f, "", "", CutsceneTransition.Cut, new Vector2(0.9f, 0.8f), cue: "c2_lampada"),
                WorldShot("sonambulo", 2.8f, "THOMAS", "Ele se mexeu. Quando a luz apagou, ele se mexeu.", CutsceneTransition.Cut, new Vector2(0.8f, 0.65f))
            );

            // 6. M2 Assinatura
            SaveCutscene("M2_Assinatura", "M2", true, memMusic,
                MemShot(pM2_1, 2.8f, "", "TERMO DE PARTICIPAÇÃO VOLUNTÁRIA. PROJETO HIPNOS.", CutsceneTransition.Glitch),
                MemShot(pM2_2, 3.0f, "VANCE", "Você leu tudo? Pode desistir agora."),
                MemShot(pM2_3, 2.8f, "THOMAS", "Eu preciso do dinheiro."),
                MemShot(pM2_4, 2.4f, "", "", CutsceneTransition.Glitch, new Vector2(1f, 1.2f))
            );

            // 7. I2 Intertítulo
            SaveCutscene("I2_AlaExecutiva", "I2", true, null,
                PanelShot(null, 3.2f, "", "ÚLTIMO HD: ALA EXECUTIVA. OS DOIS ESTÃO SOLTOS.", CutsceneTransition.Fade)
            );

            // 8. M3 Espelho
            SaveCutscene("M3_Espelho", "M3", true, memMusic,
                WorldShot("player", 2.0f, "", "", CutsceneTransition.Cut, new Vector2(1f, 0.75f)),
                MemShot(pM3_2, 2.5f, "", "", CutsceneTransition.Glitch),
                MemShot(pM3_3, 3.0f, "VANCE", "Sessão final, Voluntário 08.", CutsceneTransition.Glitch)
            );

            // 9. C3 Revelação
            SaveCutscene("C3_Revelacao", "C3", true, revMusic,
                PanelShot(pC3_1, 3.8f, "", "VOLUNTÁRIO 08. NOME: THOMAS REIS. STATUS: EM TESTE.", CutsceneTransition.Fade),
                PanelShot(pC3_2, 3.2f, "", "RESPONSÁVEL: DRA. V. VANCE."),
                WorldShot("player", 3.2f, "VANCE", "Thomas. Não é gravação. Estou viva.", CutsceneTransition.Cut, new Vector2(1f, 0.85f)),
                WorldShot("player", 3.8f, "VANCE", "Você não é auditor. A auditoria é o teste. Eu apaguei sua memória."),
                WorldShot("player", 3.6f, "VANCE", "Estou presa na observação. A Aethelgard ativou a autodestruição da base."),
                WorldShot("player", 3.8f, "VANCE", "Vá para a saída. Ou venha me buscar. A escolha é sua.")
            );

            // 10. C4 Final Testemunha
            SaveCutscene("C4_Testemunha", "C4", true, winWitnessMusic,
                PanelShot(pC4_1, 3.0f, "", "", CutsceneTransition.Fade),
                PanelShot(pC4_2, 3.6f, "VANCE", "Com isto, a Aethelgard não apaga mais ninguém."),
                PanelShot(pC4_3, 4.0f, "", "AETHELGARD DYNAMICS SOB INVESTIGAÇÃO. TESTEMUNHA SOBREVIVENTE.", CutsceneTransition.Fade)
            );

            // 11. C5 Final Amostra entregue
            SaveCutscene("C5_AmostraEntregue", "C5", true, winSampleMusic,
                PanelShot(pC5_1, 3.0f, "", "", CutsceneTransition.Fade),
                PanelShot(pC5_2, 3.4f, "AGENTE", "Voluntário 08. Bom trabalho."),
                PanelShot(pC5_3, 4.0f, "", "TESTE CONCLUÍDO. INICIAR VOLUNTÁRIO 09.", CutsceneTransition.Fade)
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RC3] 11 Cutscenes geradas com sucesso em {CutscenesDir}");
        }

        [MenuItem("Sombra da Sanidade/Narrativa/Posicionar Gatilhos e Pistas nas Alas (RC3)", priority = 72)]
        public static void PlaceTriggersAndCluesInWings()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previousScene = EditorSceneManager.GetActiveScene().path;

            Directory.CreateDirectory(SanityPropsDir);
            var wristNormal = BuildSmallSprite("pulseira_normal", 12, 8, (b, w, h) => { FillRect(b, w, h, 1, 2, 10, 4, Concreto8); });
            var wristShaken = BuildSmallSprite("pulseira_08", 12, 8, (b, w, h) => { FillRect(b, w, h, 1, 2, 10, 4, Concreto8); FillRect(b, w, h, 4, 3, 4, 2, Fosforo4); });
            var chartNormal = BuildSmallSprite("prontuario_normal", 14, 16, (b, w, h) => { FillRect(b, w, h, 2, 1, 10, 14, Concreto8); FillRect(b, w, h, 4, 10, 6, 2, Concreto4); });
            var chartShaken = BuildSmallSprite("prontuario_08", 14, 16, (b, w, h) => { FillRect(b, w, h, 2, 1, 10, 14, Concreto8); FillRect(b, w, h, 4, 10, 6, 2, Emergencia4); FillRect(b, w, h, 4, 5, 6, 2, Fosforo4); });
            var photoNormal = BuildSmallSprite("foto_normal", 16, 12, (b, w, h) => { FillRect(b, w, h, 1, 1, 14, 10, Concreto6); FillRect(b, w, h, 3, 3, 4, 6, Concreto8); FillRect(b, w, h, 9, 3, 4, 6, Concreto8); });
            var photoShaken = BuildSmallSprite("foto_08", 16, 12, (b, w, h) => { FillRect(b, w, h, 1, 1, 14, 10, Fosforo2); FillRect(b, w, h, 3, 3, 4, 6, MemWhite); FillRect(b, w, h, 9, 3, 4, 6, Fosforo5); });
            var mirrorNormal = BuildSmallSprite("espelho_normal", 16, 20, (b, w, h) => { FillRect(b, w, h, 1, 1, 14, 18, Concreto4); FillRect(b, w, h, 3, 3, 10, 14, Concreto6); FillRect(b, w, h, 6, 4, 4, 10, Concreto2); });
            var mirrorShaken = BuildSmallSprite("espelho_08", 16, 20, (b, w, h) => { FillRect(b, w, h, 1, 1, 14, 18, Fosforo2); FillRect(b, w, h, 3, 3, 10, 14, MemWhite); FillRect(b, w, h, 6, 4, 4, 10, Fosforo4); });
            var mirrorCracked = BuildSmallSprite("espelho_rachado", 16, 20, (b, w, h) => { FillRect(b, w, h, 1, 1, 14, 18, Concreto4); FillRect(b, w, h, 3, 3, 10, 14, Concreto2); FillRect(b, w, h, 4, 9, 8, 1, Vazio); FillRect(b, w, h, 7, 4, 1, 12, Vazio); });
            AssetDatabase.Refresh();

            SetupWing("Assets/_Project/Scenes/Wings/AlaMedica.unity", root =>
            {
                var safe = FindSafeAnchor(0, new Vector2(-4f, 2f));
                var locker = FindAnchor<Locker>(0, safe + new Vector2(3f, -1f));
                var drive = FindAnchor<DriveServer>(0, safe + new Vector2(0f, 3f));

                if (Object.FindAnyObjectByType<WingIntro>() == null)
                    AddCutsceneTrigger(root, "Trigger_C1_Especime", "C1_Especime", drive + new Vector2(0f, -2f), new Vector2(6f, 4f));

                AddCutsceneTrigger(root, "Trigger_M1_SalaBranca", "M1_SalaBranca", safe, new Vector2(4.5f, 4.5f));
                AddVoiceTrigger(root, "Voice_T1_3_Enfermaria", "T1-3", safe + new Vector2(0f, -1.5f), new Vector2(4.5f, 4.5f));
                AddPhosporClue(root, "Clue_P1_Enfermaria", "VOCÊ JÁ ESTEVE AQUI", safe + new Vector2(0f, 1.4f));
                AddPhosporClue(root, "Clue_P2_Vestiario", "ARMÁRIO 08", locker + new Vector2(0f, 1.2f));
                AddReactiveProp(root, "Prop_Pulseira_08", safe + new Vector2(-1.2f, -0.8f), wristNormal, wristShaken);
                AddReactiveProp(root, "Prop_Prontuario_Maca", safe + new Vector2(1.2f, 0.5f), chartNormal, chartShaken);
            });

            SetupWing("Assets/_Project/Scenes/Wings/AlaEnergia.unity", root =>
            {
                var sw = FindAnchor<SleepwalkerController>(0, new Vector2(0f, 3f));
                var player = FindAnchor<PlayerController>(0, Vector2.zero);
                var sub = FindAnchor<Substation>(0, new Vector2(-4f, 3f));
                var safe = FindSafeAnchor(0, new Vector2(4f, 2f));
                var cam = FindAnchor<SecurityCamera>(0, new Vector2(2f, -2f));

                Vector2 c2Pos = Vector2.Lerp(player, sw, 0.45f);
                AddCutsceneTrigger(root, "Trigger_C2_Sonambulo", "C2_Sonambulo", c2Pos, new Vector2(6f, 5f));
                AddCutsceneTrigger(root, "Trigger_M2_Assinatura", "M2_Assinatura", safe, new Vector2(4.5f, 4.5f));
                AddVoiceTrigger(root, "Voice_T2_4_Escritorio", "T2-4", safe + new Vector2(0f, -1.5f), new Vector2(4.5f, 4.5f));
                AddPhosporClue(root, "Clue_P3_Subestacao", "03 NÃO DORME. 08 NÃO LEMBRA.", sub + new Vector2(0f, 1.4f));
                AddPhosporClue(root, "Clue_P4_Escritorio", "T. REIS", safe + new Vector2(0f, 1.2f));
                AddPhosporClue(root, "Clue_P5_Camera", "ELES ESTÃO MEDINDO", cam + new Vector2(0f, -1.1f));
                AddReactiveProp(root, "Prop_Foto_Vance", safe + new Vector2(1.1f, 0.7f), photoNormal, photoShaken);
            });

            SetupWing("Assets/_Project/Scenes/Wings/AlaExecutiva.unity", root =>
            {
                var safe = FindSafeAnchor(1, FindSafeAnchor(0, new Vector2(-4f, 4f)));
                var keypad = FindAnchor<KeypadLock>(0, new Vector2(4f, 3f));
                var drive = FindAnchor<DriveServer>(0, new Vector2(0f, -3f));

                AddCutsceneTrigger(root, "Trigger_M3_Espelho", "M3_Espelho", safe, new Vector2(4.5f, 4.5f), sanityBelow: 35f);
                AddVoiceTrigger(root, "Voice_T3_3_Espelho", "T3-3", safe + new Vector2(0f, -1.2f), new Vector2(4.5f, 4.5f));
                AddPhosporClue(root, "Clue_P6_Espelho", "08", safe + new Vector2(0f, 1.3f));
                AddPhosporClue(root, "Clue_P7_Senha", "VOCÊ ESCREVEU ISTO", keypad + new Vector2(0f, 1.3f));
                AddPhosporClue(root, "Clue_P8_Observacao", "ELA AINDA ESTÁ AQUI", drive + new Vector2(0f, -1.4f));
                AddReactiveProp(root, "Prop_Espelho_08", safe + new Vector2(0f, 0.9f), mirrorNormal, mirrorShaken, "M3", mirrorCracked);
            });

            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene);
            Debug.Log("[RC3] Gatilhos, falas, objetos reativos (4 pares) e pistas P1-P8 ancorados nas 3 alas.");
        }

        private static void AddReactiveProp(Transform parent, string name, Vector2 pos, Sprite normal, Sprite shaken,
            string afterCutsceneId = "", Sprite postCutscene = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = normal;
            sr.sortingOrder = 5;
            go.AddComponent<SanitySpriteSwap>().Setup(normal, shaken, 35f, afterCutsceneId, postCutscene);
        }

        private static Sprite BuildSmallSprite(string fileName, int w, int h, System.Action<Color32[], int, int> draw)
        {
            string path = $"{SanityPropsDir}/{fileName}.png";
            if (!File.Exists(path))
            {
                var buf = new Color32[w * h];
                draw(buf, w, h);

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels32(buf);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            ConfigureSpriteAsset(path);
            string resPath = $"Assets/_Project/Resources/Narrative/Props/{fileName}.png";
            if (File.Exists(resPath)) ConfigureSpriteAsset(resPath);

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ConfigureSpriteAsset(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 16f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }

        private static void FillRect(Color32[] b, int bw, int bh, int x, int y, int w, int h, Color32 c)
        {
            for (int yy = Mathf.Max(0, y); yy < Mathf.Min(bh, y + h); yy++)
                for (int xx = Mathf.Max(0, x); xx < Mathf.Min(bw, x + w); xx++)
                    b[yy * bw + xx] = c;
        }

        private static Vector2 FindAnchor<T>(int index, Vector2 fallback) where T : Component
        {
            var all = Object.FindObjectsByType<T>();
            return all != null && index >= 0 && index < all.Length ? (Vector2)all[index].transform.position : fallback;
        }

        private static Vector2 FindSafeAnchor(int index, Vector2 fallback)
        {
            var all = Object.FindObjectsByType<SafeZone>();
            return all != null && index >= 0 && index < all.Length ? (Vector2)all[index].transform.position : fallback;
        }

        private static void SetupWing(string scenePath, System.Action<Transform> populate)
        {
            if (!File.Exists(scenePath)) return;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var existing = GameObject.Find("RC3_Narrative");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("RC3_Narrative").transform;
            populate(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void AddCutsceneTrigger(Transform parent, string name, string cutsceneAsset, Vector2 pos, Vector2 size, float sanityBelow = 101f)
        {
            var cutscene = Resources.Load<Cutscene>($"Narrative/Cutscenes/{cutsceneAsset}");
            if (cutscene == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            go.AddComponent<CutsceneTrigger>().Setup(cutscene, sanityBelow);
        }

        private static void AddVoiceTrigger(Transform parent, string name, string voiceId, Vector2 pos, Vector2 size)
        {
            var voice = Resources.Load<InnerVoiceLine>($"Narrative/Voices/{voiceId}");
            if (voice == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            go.AddComponent<InnerVoiceTrigger>().Setup(voice);
        }

        private static void AddPhosporClue(Transform parent, string name, string text, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = 0.14f;
            tm.fontSize = 36;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Fosforo5;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 8;
            go.AddComponent<SanityClue>();
        }

        private static void SaveCutscene(string assetName, string id, bool freezeWorld, AudioClip music, params CutsceneShot[] shots)
        {
            string path = $"{CutscenesDir}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<Cutscene>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<Cutscene>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = id;
            asset.freezeWorld = freezeWorld;
            asset.music = music;
            asset.shots = shots;
            EditorUtility.SetDirty(asset);
        }

        private static CutsceneShot PanelShot(Sprite img, float duration, string speaker, string caption,
            CutsceneTransition transition = CutsceneTransition.Cut, Vector2? zoom = null) => new CutsceneShot
        {
            kind = CutsceneShotKind.Panel,
            image = img,
            duration = duration,
            speaker = speaker,
            caption = caption,
            transition = transition,
            zoom = zoom ?? Vector2.one
        };

        private static CutsceneShot MemShot(Sprite img, float duration, string speaker, string caption,
            CutsceneTransition transition = CutsceneTransition.Cut, Vector2? zoom = null) => new CutsceneShot
        {
            kind = CutsceneShotKind.Panel,
            image = img,
            duration = duration,
            speaker = speaker,
            caption = caption,
            transition = transition,
            memory = true,
            zoom = zoom ?? Vector2.one
        };

        private static CutsceneShot WorldShot(string focus, float duration, string speaker, string caption,
            CutsceneTransition transition = CutsceneTransition.Cut, Vector2? zoom = null, string cue = "") => new CutsceneShot
        {
            kind = CutsceneShotKind.World,
            focus = focus,
            duration = duration,
            speaker = speaker,
            caption = caption,
            transition = transition,
            zoom = zoom ?? Vector2.one,
            cue = cue
        };

        // ---------- Gerador de painéis pixel art 320x180 ----------

        private static Sprite BuildPanel(string fileName, System.Action<Color32[]> draw)
        {
            string path = $"{PanelsDir}/{fileName}.png";
            if (!File.Exists(path))
            {
                var buf = new Color32[W * H];
                draw(buf);

                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.SetPixels32(buf);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 16f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Fill(Color32[] b, Color32 c) { for (int i = 0; i < b.Length; i++) b[i] = c; }

        private static void Rect(Color32[] b, int x, int y, int w, int h, Color32 c)
        {
            int x0 = Mathf.Max(0, x), y0 = Mathf.Max(0, y);
            int x1 = Mathf.Min(W, x + w), y1 = Mathf.Min(H, y + h);
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                    b[yy * W + xx] = c;
        }

        private static void DrawC0Elevator(Color32[] b) { Fill(b, Vazio); Rect(b, 90, 20, 140, 140, Concreto2); Rect(b, 158, 20, 4, 140, Vazio); Rect(b, 145, 145, 30, 8, Emergencia4); }
        private static void DrawC0Monitor(Color32[] b) { Fill(b, Vazio); Rect(b, 60, 25, 200, 130, Concreto4); Rect(b, 70, 35, 180, 110, Fosforo2); Rect(b, 120, 75, 80, 30, Fosforo4); Rect(b, 145, 85, 30, 10, Fosforo5); }
        private static void DrawC0Hand(Color32[] b) { Fill(b, Vazio); Rect(b, 40, 40, 90, 100, Concreto2); Rect(b, 110, 65, 55, 24, Concreto6); Rect(b, 165, 62, 18, 30, Ambar2); Rect(b, 183, 45, 120, 64, Fosforo2); }
        private static void DrawC0Door(Color32[] b) { Fill(b, Vazio); Rect(b, 40, 10, 80, 160, Concreto4); Rect(b, 200, 10, 80, 160, Concreto4); Rect(b, 120, 10, 80, 160, Vazio); }

        private static void DrawM1Light(Color32[] b) { Fill(b, MemDark); Rect(b, 90, 70, 140, 40, MemMid); Rect(b, 110, 78, 100, 24, MemGreen); Rect(b, 125, 84, 70, 12, MemWhite); }
        private static void DrawM1Syringe(Color32[] b) { Fill(b, MemDark); Rect(b, 80, 80, 90, 22, MemMid); Rect(b, 170, 88, 50, 6, MemGreen); Rect(b, 220, 90, 35, 2, MemWhite); }
        private static void DrawM1Vance(Color32[] b) { Fill(b, MemMid); Rect(b, 120, 35, 80, 105, MemGreen); Rect(b, 130, 60, 60, 40, MemWhite); Rect(b, 136, 98, 16, 10, MemDark); Rect(b, 168, 98, 16, 10, MemDark); }
        private static void DrawM1VanceDark(Color32[] b) { Fill(b, MemDark); Rect(b, 120, 35, 80, 105, MemMid); Rect(b, 130, 60, 60, 40, MemGreen); Rect(b, 136, 98, 16, 10, MemDark); Rect(b, 168, 98, 16, 10, MemDark); }

        private static void DrawM2Term(Color32[] b) { Fill(b, MemDark); Rect(b, 80, 20, 160, 140, MemWhite); Rect(b, 95, 130, 130, 10, MemMid); Rect(b, 95, 110, 110, 6, MemMid); Rect(b, 95, 90, 120, 6, MemMid); }
        private static void DrawM2Pen(Color32[] b) { Fill(b, MemDark); Rect(b, 70, 25, 180, 130, MemGreen); Rect(b, 145, 70, 65, 14, MemDark); Rect(b, 135, 74, 12, 6, MemWhite); }
        private static void DrawM2Sign(Color32[] b) { Fill(b, MemDark); Rect(b, 70, 25, 180, 130, MemWhite); Rect(b, 100, 60, 120, 4, MemDark); Rect(b, 115, 68, 90, 6, MemMid); }
        private static void DrawM2Name(Color32[] b) { Fill(b, MemMid); Rect(b, 60, 45, 200, 90, MemWhite); Rect(b, 85, 82, 150, 14, MemDark); }

        private static void DrawM3Reflection(Color32[] b) { Fill(b, MemDark); Rect(b, 100, 20, 120, 140, MemMid); Rect(b, 130, 45, 60, 95, MemWhite); Rect(b, 145, 110, 30, 25, MemGreen); }
        private static void DrawM3Wristband(Color32[] b) { Fill(b, MemDark); Rect(b, 60, 60, 200, 60, MemMid); Rect(b, 120, 56, 80, 68, MemWhite); Rect(b, 140, 74, 40, 30, MemGreen); }

        private static void DrawC3Dossier(Color32[] b) { Fill(b, Vazio); Rect(b, 60, 20, 200, 140, Concreto8); Rect(b, 75, 85, 50, 60, Concreto4); Rect(b, 140, 120, 100, 12, Emergencia4); Rect(b, 140, 95, 90, 8, Concreto2); }
        private static void DrawC3Signature(Color32[] b) { Fill(b, Vazio); Rect(b, 60, 20, 200, 140, Concreto8); Rect(b, 85, 65, 150, 8, Concreto2); Rect(b, 95, 80, 120, 14, Fosforo2); }

        private static void DrawC4Snow(Color32[] b) { Fill(b, Concreto8); Rect(b, 0, 0, 320, 55, Concreto6); Rect(b, 125, 45, 26, 65, Concreto2); Rect(b, 165, 48, 24, 62, Concreto2); }
        private static void DrawC4Drives(Color32[] b) { Fill(b, Vazio); Rect(b, 80, 45, 45, 70, Concreto4); Rect(b, 138, 45, 45, 70, Concreto4); Rect(b, 196, 45, 45, 70, Concreto4); Rect(b, 95, 95, 15, 6, Fosforo5); Rect(b, 153, 95, 15, 6, Fosforo5); Rect(b, 211, 95, 15, 6, Fosforo5); }
        private static void DrawC4Paper(Color32[] b) { Fill(b, Concreto2); Rect(b, 70, 15, 180, 150, Concreto8); Rect(b, 85, 125, 150, 18, Concreto2); Rect(b, 85, 40, 70, 60, Concreto4); }

        private static void DrawC5Alone(Color32[] b) { Fill(b, Concreto6); Rect(b, 0, 0, 320, 50, Concreto4); Rect(b, 148, 45, 24, 65, Vazio); }
        private static void DrawC5Van(Color32[] b) { Fill(b, Vazio); Rect(b, 70, 40, 180, 80, Concreto4); Rect(b, 80, 85, 50, 25, Concreto6); Rect(b, 245, 55, 20, 15, Emergencia4); }
        private static void DrawC5Approved(Color32[] b) { Fill(b, Vazio); Rect(b, 55, 25, 210, 130, Fosforo2); Rect(b, 75, 85, 170, 6, Fosforo5); Rect(b, 100, 45, 120, 26, Emergencia4); }

        private static Color32 Hex(int rgb) => new Color32((byte)((rgb >> 16) & 0xff), (byte)((rgb >> 8) & 0xff), (byte)(rgb & 0xff), 255);
    }
}
