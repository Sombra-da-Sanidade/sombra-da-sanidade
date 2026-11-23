using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Elementos do Release Candidate nas plantas das alas (planejamento-rc 1 a 4, issues #83 a #87):
    //  Z Sonâmbulo (adormecido)        C câmera de segurança (na parede; olha para o chão vizinho)
    //  ! ? & gatilhos de cutscene: a área que cobre todas as células do caractere. Qual cutscene cada um toca
    //        vem de uma diretiva na planta: "# @cutscene ? M1" ou "# @cutscene ? M3 35" (só com sanidade < 35).
    //  ~ som do Espécime atrás da parede (WallNoise; vai numa célula de parede)
    //  = vira também a grade do atalho (ShortcutGate), fechada até a Vance abrir
    //  * pista em fósforo (SanityClue, sanidade < 35). O texto vem das diretivas "# @pista <TEXTO>", na ordem de
    //    leitura da planta (de cima para baixo, da esquerda para a direita), como no roteiro (roteiro-rc 3).
    //  # @fala <id>: cada ", na ordem de leitura, vira o InnerVoiceTrigger da fala Resources/Narrative/Voices/<id>
    //  # @objeto <nome>: cada %, na ordem de leitura, vira o SanitySpriteSwap com Resources/Narrative/Props/<nome>_*
    //  C0, I1, I2, C1, C3, C4/C5 e as falas por evento são do WingNarrativeDirector (Frente 3, criado em runtime).
    //  " fala do Thomas   % objeto que muda com a sanidade   V cela da Vance   = atalho da Eclusa
    //        (LevelMarker com id "<tipo>_<ala>_<n>"; o sistema de outra frente encaixa no marcador).
    // A cutscene é achada pelo campo id (C1, M1...) em qualquer pasta. Sem o asset, o gatilho fica vazio e não
    // faz nada: basta gerar a cena de novo depois que a #91 criar as cutscenes.
    public static partial class AlfaSceneBuilder
    {
        // São chão (o desenho é o do vizinho da esquerda, como os marcadores da introdução).
        private const string RcFloorMarkers = "Z!?&\"%V=*";
        private const string CutsceneChars = "!?&";

        private static readonly (char c, LevelMarker.Kind kind, string prefix)[] MarkerChars =
        {
            ('"', LevelMarker.Kind.InnerVoice, "fala"),
            ('%', LevelMarker.Kind.SanityObject, "objeto"),
            ('V', LevelMarker.Kind.VanceCell, "cela"),
            ('=', LevelMarker.Kind.Shortcut, "atalho"),
        };

        // Diretiva da planta: linha "# @<nome> <args...>" (comentário, ignorada pelo desenho do mapa).
        internal static List<string[]> ParseDirectives(IEnumerable<string> lines) =>
            lines.Where(l => l.StartsWith("# @"))
                 .Select(l => l.Substring(3).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                 .Where(a => a.Length > 0)
                 .ToList();

        private static void AddRcElements(Layout map, Transform objectives, Func<int, int, string> wingAt)
        {
            var root = new GameObject("RC").transform;
            AddSleepwalkers(map, root);
            AddCameras(map, root);
            AddWallNoises(map, root);
            AddPhosphorClues(map, root);
            AddCutsceneTriggers(map, root);
            AddMarkers(map, root, wingAt);
            if (root.childCount == 0) UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        // Diretiva "# @sonambulo ronda": já começa rondando no escuro. "# @sonambulo revelacao": dorme até a cutscene
        // (WingNarrativeDirector o acorda). Sem diretiva: adormecido até o Thomas o ver (config).
        private static void AddSleepwalkers(Layout map, Transform root)
        {
            var mode = map.Directives.FirstOrDefault(d => d[0] == "sonambulo" && d.Length > 1)?[1];
            foreach (var cell in Find(map, 'Z'))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(SleepwalkerPrefabBuilder.GetOrBuild());
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                var sw = go.GetComponent<SleepwalkerController>();
                var so = new SerializedObject(sw);
                if (mode == "ronda") so.FindProperty("startMode").enumValueIndex = (int)SleepwalkerStart.Roaming;
                else if (mode == "revelacao")
                {
                    so.FindProperty("startMode").enumValueIndex = (int)SleepwalkerStart.Dormant;
                    so.FindProperty("wakeOnlyByScript").boolValue = true;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // A câmera fica presa na parede e olha para o primeiro vizinho de chão (sul, leste, oeste, norte).
        // Desliga no disjuntor mais perto do setor (SecurityCamera.findNearestBreaker, resolvido em jogo).
        private static void AddCameras(Layout map, Transform root)
        {
            var cells = Find(map, 'C');
            if (cells.Count == 0) return;
            var parent = new GameObject("SecurityCameras").transform;
            parent.SetParent(root, false);
            var dirs = new[] { Vector2Int.down, Vector2Int.right, Vector2Int.left, Vector2Int.up };
            foreach (var cell in cells)
            {
                var dir = dirs.FirstOrDefault(d => map.IsFloor(cell.x + d.x, cell.y + d.y));
                if (dir == Vector2Int.zero)
                {
                    Debug.LogError($"Câmera em {cell} não encosta em chão.");
                    continue;
                }
                var pos = new Vector2(cell.x + 0.5f, cell.y + 0.5f) + (Vector2)dir * 0.6f;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                var go = SecurityCameraPrefabBuilder.Place(parent, pos, angle);
                go.name = $"SecurityCamera_{cell.x}_{cell.y}";
            }
        }

        private const string MonsterAudio = Root + "/ScriptableObjects/Audio/Monster/";

        private static void AddWallNoises(Layout map, Transform root)
        {
            var cells = Find(map, '~');
            if (cells.Count == 0) return;
            var clips = new[] { "especime_respiracao", "especime_passo", "especime_estalo" }
                .Select(n => AssetDatabase.LoadAssetAtPath<SoundEvent>(MonsterAudio + n + ".asset"))
                .Where(s => s != null).ToArray();
            if (clips.Length == 0) Debug.LogWarning("WallNoise: sons do Espécime não encontrados; gere os SoundEvents.");
            var parent = new GameObject("WallNoises").transform;
            parent.SetParent(root, false);
            foreach (var cell in cells)
            {
                var go = new GameObject($"WallNoise_{cell.x}_{cell.y}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                go.AddComponent<WallNoise>().Setup(clips);
            }
        }

        private static void AddPhosphorClues(Layout map, Transform root)
        {
            var cells = Find(map, '*').OrderByDescending(p => p.y).ThenBy(p => p.x).ToList();
            var texts = map.Directives.Where(d => d[0] == "pista").Select(d => string.Join(" ", d.Skip(1))).ToList();
            if (cells.Count != texts.Count)
                Debug.LogError($"Pistas: a planta tem {cells.Count} '*' e {texts.Count} diretivas \"# @pista\".");
            if (cells.Count == 0) return;
            var parent = new GameObject("PhosphorClues").transform;
            parent.SetParent(root, false);
            for (int i = 0; i < cells.Count && i < texts.Count; i++)
                PrototypeSceneBuilder.CreateClue(parent, $"Clue_P{i + 1}",
                    new Vector2(cells[i].x + 0.5f, cells[i].y + 0.5f), texts[i].Replace("\\n", "\n"), 0.05f);
        }

        private static void AddCutsceneTriggers(Layout map, Transform root)
        {
            foreach (var d in map.Directives.Where(d => d[0] == "cutscene"))
            {
                if (d.Length < 3 || d[1].Length != 1 || CutsceneChars.IndexOf(d[1][0]) < 0)
                {
                    Debug.LogError($"Diretiva inválida: @{string.Join(" ", d)} (use \"# @cutscene ? M1 [sanidade]\").");
                    continue;
                }
                char c = d[1][0];
                string id = d[2];
                float sanityBelow = d.Length > 3 ? float.Parse(d[3], CultureInfo.InvariantCulture) : 101f;
                var cells = Find(map, c);
                if (cells.Count == 0)
                {
                    Debug.LogError($"Cutscene {id}: gatilho {c} não está na planta.");
                    continue;
                }
                var go = new GameObject($"CutsceneTrigger_{id}");
                go.transform.SetParent(root, false);
                go.transform.position = (Vector2)RectOf(cells).center;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = RectOf(cells).size;
                var trigger = go.AddComponent<CutsceneTrigger>();
                var scene = FindCutscene(id);
                if (scene == null) Debug.LogWarning($"Cutscene {id} ainda não existe; gatilho criado vazio.");
                trigger.Setup(scene, sanityBelow);
                EditorUtility.SetDirty(trigger);
            }
        }

        private static void AddMarkers(Layout map, Transform root, Func<int, int, string> wingAt)
        {
            foreach (var (c, kind, prefix) in MarkerChars)
            {
                // Ordem de leitura: de cima para baixo, da esquerda para a direita (y = 0 é o sul).
                var cells = Find(map, c).OrderByDescending(p => p.y).ThenBy(p => p.x).ToList();
                // Células vizinhas do mesmo caractere formam um marcador só (ex.: a cela ocupa várias).
                var groups = new List<List<Vector2Int>>();
                foreach (var cell in cells)
                {
                    var g = groups.FirstOrDefault(gr => gr.Any(o => Mathf.Abs(o.x - cell.x) + Mathf.Abs(o.y - cell.y) == 1));
                    if (g != null) g.Add(cell); else groups.Add(new List<Vector2Int> { cell });
                }
                var counters = new Dictionary<string, int>();
                int objectIndex = 0, voiceIndex = 0;
                foreach (var g in groups)
                {
                    string wing = wingAt(g[0].x, g[0].y).ToLowerInvariant();
                    counters[wing] = counters.TryGetValue(wing, out int n) ? n + 1 : 1;
                    var rect = RectOf(g);
                    var go = new GameObject($"Marker_{prefix}_{wing}_{counters[wing]}");
                    go.transform.SetParent(root, false);
                    go.transform.position = (Vector2)rect.center;
                    go.AddComponent<LevelMarker>().Setup($"{prefix}_{wing}_{counters[wing]}", kind,
                        g.Count > 1 ? rect.size : Vector2.zero);
                    if (kind == LevelMarker.Kind.Shortcut) AddShortcutGate(go, rect.size);
                    if (kind == LevelMarker.Kind.SanityObject) AddSanityObject(go, map, objectIndex++);
                    if (kind == LevelMarker.Kind.InnerVoice) AddVoiceArea(go, map, voiceIndex++, rect.size);
                }
            }
        }

        // Cutscene pelo campo id (C1, M1...), em qualquer pasta do projeto. Null se a #91 ainda não criou.
        internal static Cutscene FindCutscene(string id) =>
            AssetDatabase.FindAssets("t:Cutscene")
                .Select(g => AssetDatabase.LoadAssetAtPath<Cutscene>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(c => c != null && c.id == id);

        // Grade fechada do atalho: bloqueia o corredor até a Vance abrir (ShortcutGate.Open).
        private static void AddShortcutGate(GameObject go, Vector2 size)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            var art = new GameObject("Grade").AddComponent<SpriteRenderer>();
            art.transform.SetParent(go.transform, false);
            art.transform.localScale = new Vector3(size.x, size.y, 1f);
            art.sprite = PrototypeSceneBuilder.GetOrCreatePlaceholderSprite();
            art.sortingOrder = 5;
            go.AddComponent<ShortcutGate>().Setup(art);
        }

        private const string VanceArt = Root + "/Art/Sprites/vance.png";
        private const string VanceCellArt = Root + "/Art/Props/vance_cell.png";
        private const string CellSkillConfig = "InteractionConfigCelaVance";

        // Cela da Vance (#82) no marcador "cela_eclusa_1" (V na planta) e o diretor dos finais (#95).
        private static void AddVanceCell(Transform objectives)
        {
            new GameObject("EndingDirector").AddComponent<EndingDirector>();
            var marker = LevelMarker.Find("cela_eclusa_1")
                ?? UnityEngine.Object.FindObjectsByType<LevelMarker>(FindObjectsSortMode.None).FirstOrDefault(m => m.Id == "cela_eclusa_1");
            if (marker == null)
            {
                Debug.LogError("Eclusa: falta a cela da Vance (V) na planta.");
                return;
            }
            MonsterPrefabBuilder.EnsureArtImport(VanceArt);
            MonsterPrefabBuilder.EnsureArtImport(VanceCellArt);
            var cellSprites = AssetDatabase.LoadAllAssetsAtPath(VanceCellArt).OfType<Sprite>().ToArray();
            var vanceSprite = AssetDatabase.LoadAllAssetsAtPath(VanceArt).OfType<Sprite>().FirstOrDefault();
            if (cellSprites.Length < 2 || vanceSprite == null)
                Debug.LogWarning("Sprites da Vance/cela não encontrados. Rode python3 tools/art/export_sprites.py.");

            var go = new GameObject("VanceCell");
            go.transform.SetParent(objectives, false);
            go.transform.position = marker.transform.position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3.6f, 2.8f);

            var vance = new GameObject("Vance").AddComponent<SpriteRenderer>();
            vance.transform.SetParent(go.transform, false);
            vance.sprite = vanceSprite;
            vance.sortingOrder = 8;
            var art = new GameObject("Grade").AddComponent<SpriteRenderer>();
            art.transform.SetParent(go.transform, false);
            art.sortingOrder = 9;
            Sprite closed = cellSprites.FirstOrDefault(s => s.name.EndsWith("closed")) ?? cellSprites.FirstOrDefault();
            Sprite open = cellSprites.FirstOrDefault(s => s.name.EndsWith("open"));
            art.sprite = closed;

            var skill = AssetDatabase.LoadAssetAtPath<SombraDaSanidade.Interaction.InteractionConfig>($"{Root}/ScriptableObjects/Config/{CellSkillConfig}.asset");
            if (skill == null)
            {
                // Planejamento-rc 3: 2 skill checks em 6 s. Começa cedo e sempre faz os 2.
                skill = PrototypeSceneBuilder.GetOrCreateConfig<SombraDaSanidade.Interaction.InteractionConfig>(CellSkillConfig);
                skill.skillCheckStartDelay = 0.6f;
                skill.skillCheckChancePerSecond = 0.8f;
                skill.skillCheckMinPerDrive = 2;
                skill.skillCheckMaxPerDrive = 2;
                skill.skillCheckWarning = 0.4f;
                EditorUtility.SetDirty(skill);
                AssetDatabase.SaveAssets();
            }
            var escape = PrototypeSceneBuilder.GetOrCreateConfig<EscapeConfig>("EscapeConfig");
            var exit = UnityEngine.Object.FindAnyObjectByType<SombraDaSanidade.Interaction.ExitDoor>();
            go.AddComponent<SombraDaSanidade.Interaction.VanceCell>()
                .Setup(escape, skill, art, closed, open, vance, exit != null ? exit.transform : null);
        }

        // Fala por área (#89): InnerVoiceTrigger da Frente 3 com a fala de Resources/Narrative/Voices.
        private static void AddVoiceArea(GameObject go, Layout map, int index, Vector2 size)
        {
            var ids = map.Directives.Where(a => a[0] == "fala" && a.Length > 1).Select(a => a[1]).ToList();
            if (index >= ids.Count)
            {
                Debug.LogError($"Fala {index + 1}: falta a diretiva \"# @fala <id>\" na planta.");
                return;
            }
            var line = AssetDatabase.LoadAssetAtPath<SombraDaSanidade.Narrative.InnerVoiceLine>(
                $"{Root}/Resources/Narrative/Voices/{ids[index]}.asset");
            if (line == null) Debug.LogError($"Fala {ids[index]} não encontrada em Resources/Narrative/Voices.");
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = Vector2.Max(size, new Vector2(4.5f, 4.5f));
            go.AddComponent<SombraDaSanidade.Narrative.InnerVoiceTrigger>().Setup(line);
        }

        // Objeto que muda com a sanidade (#89): SanitySpriteSwap da Frente 3 com os sprites de
        // Resources/Narrative/Props (<nome>_normal e <nome>_08; o espelho racha depois da M3).
        private static void AddSanityObject(GameObject go, Layout map, int index)
        {
            var names = map.Directives.Where(a => a[0] == "objeto" && a.Length > 1).Select(a => a[1]).ToList();
            if (index >= names.Count)
            {
                Debug.LogError($"Objeto de sanidade {index + 1}: falta a diretiva \"# @objeto <nome>\" na planta.");
                return;
            }
            string n = names[index];
            Sprite Load(string f) => AssetDatabase.LoadAllAssetsAtPath($"{Root}/Resources/Narrative/Props/{f}.png").OfType<Sprite>().FirstOrDefault();
            var normal = Load($"{n}_normal");
            var shaken = Load($"{n}_08");
            if (normal == null || shaken == null)
            {
                Debug.LogError($"Sprites de {n} não encontrados em Resources/Narrative/Props.");
                return;
            }
            go.name = $"Prop_{n}";
            var art = go.AddComponent<SpriteRenderer>();
            art.sprite = normal;
            art.sortingOrder = 5;
            bool mirror = n == "espelho";
            go.AddComponent<SombraDaSanidade.Sanity.SanitySpriteSwap>().Setup(normal, shaken, 35f,
                mirror ? "M3" : "", mirror ? Load("espelho_rachado") : null);
        }

        private static Rect RectOf(List<Vector2Int> cells)
        {
            int x0 = cells.Min(c => c.x), y0 = cells.Min(c => c.y);
            int x1 = cells.Max(c => c.x) + 1, y1 = cells.Max(c => c.y) + 1;
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }
    }
}
