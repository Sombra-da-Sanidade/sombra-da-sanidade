using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Import do áudio de Assets/_Project/Audio/ (#102, ADR 0005):
    // - sem normalização do Unity: o volume vem do render_audio.py (alvo de LUFS por grupo, sound-design.md seção 6);
    // - música e ambiente (pastas Music e Ambience, ou arquivo sem tomada _NN): CompressedInMemory, estéreo;
    // - efeitos (arquivos nome_NN): DecompressOnLoad e mono (fonte posicional 2D).
    public class AudioImportRules : AssetPostprocessor
    {
        public const string AudioRoot = "Assets/_Project/Audio/";
        public const string NormalizeProperty = "m_Normalize";
        private static readonly Regex Take = new Regex(@"_\d{2}(_(muffled|far|memory))?$");

        public static bool IsStreamLike(string assetPath)
        {
            string p = assetPath.Replace('\\', '/');
            if (p.Contains("/Music/") || p.Contains("/Ambience/")) return true;
            return !Take.IsMatch(Path.GetFileNameWithoutExtension(p));
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot)) return;
            var importer = (AudioImporter)assetImporter;
            bool stream = IsStreamLike(assetPath);

            var settings = importer.defaultSampleSettings;
            settings.loadType = stream ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !stream;
            importer.loadInBackground = false;

            // "normalize" não tem API pública; no SerializedObject o campo é m_Normalize.
            var so = new SerializedObject(importer);
            var normalize = so.FindProperty(NormalizeProperty);
            if (normalize != null && normalize.boolValue)
            {
                normalize.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
