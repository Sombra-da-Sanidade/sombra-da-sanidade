using System.Collections.Generic;
using System.Linq;
using System.Text;
using SombraDaSanidade.Audio;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // RC4, issue #102. Confere o lado Unity do pipeline com as receitas de audio-src/_Exemplos (renderizadas por
    // tools/audio/render_audio.py): regras de import (sem normalizar, efeitos mono e DecompressOnLoad, música
    // CompressedInMemory) e os SoundEvent gerados (tomadas, versões abafadas, grupo e bloco "event" da receita).
    // Menu: Sombra da Sanidade > Rodar teste do pipeline de áudio.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.AudioPipelineTest.RunBatch (sai com 0 ou 1).
    public static class AudioPipelineTest
    {
        private const string Examples = AudioImportRules.AudioRoot + "_Exemplos/";
        private static readonly List<string> results = new List<string>();
        private static bool failed;

        [MenuItem("Sombra da Sanidade/Rodar teste do pipeline de áudio", priority = 72)]
        public static void Run() => Execute(batch: false);

        public static void RunBatch() => Execute(batch: true);

        private static void Execute(bool batch)
        {
            results.Clear();
            failed = false;

            var breath = Importer("monstro_respiracao_01.ogg");
            var music = Importer("motivo_contagem.ogg");
            Check("Exemplos renderizados e importados", breath != null && music != null);
            if (breath != null && music != null)
            {
                Check("Efeito: DecompressOnLoad e mono", breath.defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad && breath.forceToMono);
                Check("Música: CompressedInMemory e estéreo", music.defaultSampleSettings.loadType == AudioClipLoadType.CompressedInMemory && !music.forceToMono);
                Check("Unity não normaliza (volume vem do render)", !new SerializedObject(breath).FindProperty(AudioImportRules.NormalizeProperty).boolValue
                    && !new SerializedObject(music).FindProperty(AudioImportRules.NormalizeProperty).boolValue);
            }

            SoundEventBuilder.Build();
            var monster = AssetDatabase.LoadAssetAtPath<SoundEvent>(SoundEventBuilder.EventPath(Examples + "monstro_respiracao"));
            Check("SoundEvent da respiração criado", monster != null);
            if (monster != null)
            {
                Check($"2 tomadas em ordem ({string.Join(", ", monster.clips.Select(c => c != null ? c.name : "null"))})",
                    monster.clips.Length == 2 && monster.clips[0].name == "monstro_respiracao_01" && monster.clips[1].name == "monstro_respiracao_02");
                Check("Versões abafadas ligadas às tomadas", monster.muffledClips.Length == 2 && monster.muffledClips[1].name == "monstro_respiracao_02_muffled");
                Check("Grupo Monster e posicional", monster.bus == AudioBus.Monster && monster.spatial);
                Check("Pitch do bloco event", Mathf.Approximately(monster.pitch.x, 0.97f) && Mathf.Approximately(monster.pitch.y, 1.03f));
            }

            var bip = AssetDatabase.LoadAssetAtPath<SoundEvent>(SoundEventBuilder.EventPath(Examples + "ui_bipe"));
            Check("SoundEvent do bipe: 3 tomadas, grupo Ui, não posicional",
                bip != null && bip.clips.Length == 3 && bip.bus == AudioBus.Ui && !bip.spatial && bip.muffledClips.Length == 0);
            Check("Música (sem _NN) não vira SoundEvent",
                AssetDatabase.LoadAssetAtPath<SoundEvent>(SoundEventBuilder.EventPath(Examples + "motivo_contagem")) == null);

            var sb = new StringBuilder($"[Pipeline de áudio] {(failed ? "FALHOU" : "PASSOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            if (failed) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
            if (batch) EditorApplication.Exit(failed ? 1 : 0);
        }

        private static AudioImporter Importer(string file) => AssetImporter.GetAtPath(Examples + file) as AudioImporter;

        private static void Check(string label, bool ok)
        {
            results.Add($"{(ok ? "OK  " : "FAIL")} {label}");
            if (!ok) failed = true;
        }
    }
}
