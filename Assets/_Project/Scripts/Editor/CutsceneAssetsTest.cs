using System.Linq;
using System.Text;
using SombraDaSanidade.Level;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Critérios da #91 sobre os assets gerados pelo CutsceneAssetBuilder: as 11 cutscenes existem, C0/C3/C4/C5
    // têm painéis, C1/C2 rodam no motor com o mundo andando, M1-M3 usam o MemoryEffect, toda cutscene tem som,
    // e C0/I1/I2 carregam por Resources (WingOpening). Pular (Esc) é do CutscenePlayer e tem teste no CutsceneTest.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.CutsceneAssetsTest.RunBatch -quit
    public static class CutsceneAssetsTest
    {
        private static readonly StringBuilder log = new StringBuilder();
        private static bool failed;

        [MenuItem("Sombra da Sanidade/Narrativa/Rodar teste dos assets das cutscenes", priority = 41)]
        public static void Run() => Execute(batch: false);

        public static void RunBatch() => Execute(batch: true);

        private static void Execute(bool batch)
        {
            log.Clear();
            failed = false;
            CutsceneAssetBuilder.Build();

            foreach (var id in CutsceneAssetBuilder.Ids)
            {
                var c = CutsceneAssetBuilder.Load(id);
                Check($"{id} existe e tem planos", c != null && c.id == id && c.shots.Length > 0);
                if (c == null) continue;
                bool sound = c.music != null || c.shots.Any(s => s.sound != null);
                Check($"{id} tem som", sound);
                Check($"{id} tem legenda ou imagem em todo plano de painel",
                    c.shots.Where(s => s.kind == CutsceneShotKind.Panel && !s.memory)
                     .All(s => s.image != null || !string.IsNullOrEmpty(s.caption) || s.sound != null));
            }

            foreach (var id in new[] { "C0", "C3", "C4", "C5" })
                Check($"{id} usa painéis", CutsceneAssetBuilder.Load(id)?.shots.Any(s => s.kind == CutsceneShotKind.Panel) == true);
            foreach (var id in new[] { "C1", "C2" })
            {
                var c = CutsceneAssetBuilder.Load(id);
                Check($"{id} no motor com o mundo andando", c != null && !c.freezeWorld
                    && c.shots.All(s => s.kind == CutsceneShotKind.World));
            }
            Check("C2 dispara o cue da lâmpada", CutsceneAssetBuilder.Load("C2")?.shots.Any(s => s.cue == "c2_lampada") == true);
            foreach (var id in new[] { "M1", "M2", "M3" })
            {
                var c = CutsceneAssetBuilder.Load(id);
                Check($"{id} usa o MemoryEffect e termina em glitch", c != null && c.shots.Any(s => s.memory)
                    && c.shots.Last().transition == CutsceneTransition.Glitch);
            }
            foreach (var id in new[] { "C0", "I1", "I2" })
                Check($"{id} carrega por Resources", WingOpening.Load(id) != null);

            Debug.Log((failed ? "[Cutscenes RC] FALHOU\n" : "[Cutscenes RC] PASSOU\n") + log);
            if (batch) EditorApplication.Exit(failed ? 1 : 0);
        }

        private static void Check(string label, bool ok)
        {
            if (!ok) failed = true;
            log.AppendLine((ok ? "OK   " : "FAIL ") + label);
        }
    }
}
