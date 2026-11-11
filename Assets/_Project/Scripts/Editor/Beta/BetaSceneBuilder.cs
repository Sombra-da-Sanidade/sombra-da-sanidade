using System.Collections.Generic;
using System.IO;
using System.Linq;
using SombraDaSanidade.Beta;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera a cena Beta: protótipo para testar tudo o que já existe num mapa gerado por seed (BetaMapGenerator),
    // com os tiles do laboratório, o Thomas, o Espécime, armários com a tabela de cada ala, itens, servidores com
    // skill check e upgrade, disjuntores, terminais com os logs da Dra. Vance, pistas em fósforo, o atalho do
    // monstro na Energia, a troca de comportamento por ala e o painel de teste (F1).
    // Menu: Sombra da Sanidade > Beta. Batchmode: -executeMethod SombraDaSanidade.EditorTools.BetaSceneBuilder.Build
    public static class BetaSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main/Beta.unity";
        public const string LayoutTextPath = "art-src/Maps/beta-gerado.txt";
        // Seed padrão: a cena versionada e o build publicado usam esta. Trocar = mapa novo para todo mundo.
        public const int DefaultSeed = 2026;

        private static readonly string[] LogNames = { "Vance01_Triagem", "Vance02_Contencao", "Vance03_Auditoria" };

        [MenuItem("Sombra da Sanidade/Beta/Gerar cena Beta (seed padrão)", priority = 4)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(DefaultSeed);
        }

        [MenuItem("Sombra da Sanidade/Beta/Gerar cena Beta (seed nova)", priority = 5)]
        public static void BuildRandomFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(new System.Random().Next(1, 100000));
        }

        [MenuItem("Sombra da Sanidade/Beta/Abrir cena Beta", priority = 6)]
        public static void OpenFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build(DefaultSeed);
            else EditorSceneManager.OpenScene(ScenePath);
        }

        // Batchmode (seed padrão).
        public static void Build() => Build(DefaultSeed);

        public static void Build(int seed)
        {
            var map = BetaMapGenerator.Generate(seed, out var info);
            if (map == null) return;

            // Assets antes de trocar de cena.
            foreach (var n in LogNames) PrototypeSceneBuilder.LoadAudioLog(n);

            AlfaSceneBuilder.BuildFromLayout(map, ScenePath, drivesRequired: 3, info.WingAt, ctx => AddExtras(ctx, info));
            string root = Path.GetDirectoryName(Application.dataPath);
            File.WriteAllText(Path.Combine(root, LayoutTextPath), BetaMapGenerator.ToText(map, info.Seed));
            Debug.Log($"[Beta] cena gerada com a seed {info.Seed} ({LayoutTextPath} atualizado)");
        }

        private static void AddExtras(AlfaSceneBuilder.BuildContext ctx, BetaMapGenerator.Info info)
        {
            var map = ctx.Map;
            var beta = new GameObject("Beta").transform;

            // Terminais de log em frente a cada tile de terminal na parede.
            var logs = LogNames.Select(PrototypeSceneBuilder.LoadAudioLog).ToArray();
            int t = 0;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map[x, y] == 'T')
                    {
                        var log = logs[t % logs.Length];
                        PrototypeSceneBuilder.CreateAudioLog(ctx.Items, ctx.Square, $"AudioLog_{info.WingAt(x, y)}_{t}",
                            new Vector2(x + 0.5f, y - 0.5f), log);
                        t++;
                    }

            // Atalho só do monstro (liga quando a ala dá o comportamento Shortcuts).
            var markers = new List<(string, Transform)>();
            int s = 0;
            foreach (var (from, to) in info.Shortcuts)
            {
                PrototypeSceneBuilder.CreateMonsterShortcut(beta, $"MonsterShortcut_{s++}", from, to);
                markers.Add(("Atalho do monstro", Marker(beta, "ShortcutEnd", from + Vector2.down * 0.6f)));
            }

            // Alas: troca o comportamento do monstro quando o jogador entra.
            var tracker = beta.gameObject.AddComponent<BetaWingTracker>();
            var wings = new[] { BetaMapGenerator.Medica, BetaMapGenerator.Energia, BetaMapGenerator.Executiva };
            var so = new SerializedObject(tracker);
            var arr = so.FindProperty("wings");
            arr.arraySize = wings.Length;
            for (int i = 0; i < wings.Length; i++)
            {
                var r = BetaMapGenerator.Info.RegionOf(wings[i]);
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("name").stringValue = wings[i];
                // Margem de 1 tile para pegar a entrada dos corredores da ala.
                e.FindPropertyRelative("area").rectValue = new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Painel de teste (F1) com os pontos de teleporte.
            var panel = beta.gameObject.AddComponent<BetaDebugPanel>();
            var points = new List<(string, Transform)> { ("Saguão (início)", Marker(beta, "Spawn", ctx.Player.transform.position)) };
            points.AddRange(Named<DriveServer>(o => $"HD {Wing(o.name)}", 1.1f));
            points.AddRange(Named<Breaker>(o => $"Disjuntor {Wing(o.name)}", 1.1f));
            points.AddRange(Named<Locker>(o => $"Armário {Wing(o.name)}", 1f).GroupBy(p => p.Item1).Select(g => g.First()));
            points.AddRange(Named<AdrenaSynModule>(_ => "Adrena-Syn", 1f));
            points.AddRange(Named<LithiumStabilizer>(_ => "Estabilizador", 1f));
            points.AddRange(Named<AudioLogTerminal>(o => $"Log {Wing(o.name)}", 1f));
            points.AddRange(Named<ExitDoor>(_ => "Eclusa", 1.5f));
            points.AddRange(markers);
            var monster = Object.FindAnyObjectByType<MonsterController>();
            if (monster != null) points.Add(("Perto do monstro", Marker(beta, "MonsterArea", (Vector2)monster.transform.position + Vector2.down * 4f)));

            var pso = new SerializedObject(panel);
            pso.FindProperty("seed").intValue = info.Seed;
            var parr = pso.FindProperty("points");
            parr.arraySize = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                var e = parr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("label").stringValue = points[i].Item1;
                e.FindPropertyRelative("target").objectReferenceValue = points[i].Item2;
            }
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Ponto de teleporte logo abaixo de cada objeto (no chão, fora do trigger de interação do vizinho).
            IEnumerable<(string, Transform)> Named<T>(System.Func<T, string> label, float below) where T : Component =>
                Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID)
                    .OrderBy(o => o.name)
                    .Select(o => (label(o), Marker(beta, $"Go_{o.name}", (Vector2)o.transform.position + Vector2.down * below)));
        }

        private static Transform Marker(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        // "DriveServer_Energia" → "Energia"; o primeiro trecho depois do "_".
        private static string Wing(string objectName)
        {
            var parts = objectName.Split('_');
            return parts.Length > 1 ? parts[1] switch { "Medica" => "Médica", "Saguao" => "Saguão", var w => w } : objectName;
        }
    }
}
