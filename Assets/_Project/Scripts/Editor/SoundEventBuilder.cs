using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SombraDaSanidade.Audio;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Cria/atualiza os SoundEvent e MusicSet a partir do áudio renderizado (#102, docs/audio/pipeline.md):
    // - Assets/_Project/Audio/<Cat>/<nome>_NN.ogg (+ _muffled) → ScriptableObjects/Audio/<Cat>/<nome>.asset (SoundEvent);
    // - música com "music_set" e "layer" na receita → ScriptableObjects/Audio/Music/<set>.asset (MusicSet).
    // Grupo, pitch, volume, espacialização, raio de ruído e cooldown vêm do bloco "event" da receita em
    // audio-src/<Cat>/<nome>.json; sem receita (foley importado à mão), o grupo vem da pasta.
    // Menu: Sombra da Sanidade > Áudio > Gerar SoundEvents. Batchmode: -executeMethod ...SoundEventBuilder.BuildBatch -quit
    public static class SoundEventBuilder
    {
        public const string EventsRoot = "Assets/_Project/ScriptableObjects/Audio/";
        private static readonly Regex TakeName = new Regex(@"^(?<name>.+)_(?<take>\d{2})(_(?<variant>muffled|far|memory))?$");

        private static string RecipeRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "audio-src"));

        [MenuItem("Sombra da Sanidade/Áudio/Gerar SoundEvents", priority = 202)]
        public static void Build()
        {
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioImportRules.AudioRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath).ToList();

            int events = 0, sets = 0;
            var groups = new Dictionary<string, List<string>>();
            foreach (var path in clips)
            {
                var m = TakeName.Match(Path.GetFileNameWithoutExtension(path));
                if (!m.Success) continue;
                string key = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + m.Groups["name"].Value;
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<string>();
                list.Add(path);
            }

            foreach (var pair in groups.OrderBy(p => p.Key))
            {
                BuildEvent(pair.Key, pair.Value);
                events++;
            }
            sets = BuildMusicSets(clips);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SoundEvents] {events} SoundEvent(s) e {sets} MusicSet(s) atualizados.");
        }

        public static void BuildBatch()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogError("[SoundEvents] Falha: " + e); EditorApplication.Exit(1); }
        }

        // Caminho do asset do evento para um áudio em Assets/_Project/Audio/<Cat>/<nome>.
        public static string EventPath(string audioKey)
        {
            string rel = audioKey.Substring(AudioImportRules.AudioRoot.Length);
            return EventsRoot + rel + ".asset";
        }

        private static void BuildEvent(string key, List<string> files)
        {
            string category = Path.GetDirectoryName(key.Substring(AudioImportRules.AudioRoot.Length)).Replace('\\', '/');
            string name = Path.GetFileName(key);
            var recipe = LoadRecipe(category, name);

            AudioClip[] Variant(string variant) => files
                .Select(f => (f, m: TakeName.Match(Path.GetFileNameWithoutExtension(f))))
                .Where(x => x.m.Groups["variant"].Value == variant)
                .OrderBy(x => x.m.Groups["take"].Value)
                .Select(x => AssetDatabase.LoadAssetAtPath<AudioClip>(x.f)).ToArray();

            string path = EventPath(key);
            var sound = AssetDatabase.LoadAssetAtPath<SoundEvent>(path);
            if (sound == null)
            {
                EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                sound = ScriptableObject.CreateInstance<SoundEvent>();
                AssetDatabase.CreateAsset(sound, path);
            }

            sound.clips = Variant("");
            var muffled = Variant("muffled");
            sound.muffledClips = muffled.Length == sound.clips.Length ? muffled : new AudioClip[0];
            sound.bus = ParseBus(recipe?.bus, category);
            var e = recipe?.@event;
            if (e != null)
            {
                if (e.volume != null && e.volume.Length == 2) sound.volume = new Vector2(e.volume[0], e.volume[1]);
                if (e.pitch != null && e.pitch.Length == 2) sound.pitch = new Vector2(e.pitch[0], e.pitch[1]);
                sound.spatial = e.spatial;
                sound.maxDistance = e.maxDistance;
                sound.noiseRadius = e.noiseRadius;
                sound.cooldown = e.cooldown;
            }
            EditorUtility.SetDirty(sound);
        }

        private static int BuildMusicSets(List<string> clips)
        {
            var touched = new HashSet<MusicSet>();
            foreach (var path in clips)
            {
                string rel = path.Substring(AudioImportRules.AudioRoot.Length);
                string category = Path.GetDirectoryName(rel).Replace('\\', '/');
                var recipe = LoadRecipe(category, Path.GetFileNameWithoutExtension(path));
                if (recipe == null || string.IsNullOrEmpty(recipe.music_set) || recipe.layer < 0) continue;

                string setPath = EventsRoot + "Music/" + recipe.music_set + ".asset";
                var set = AssetDatabase.LoadAssetAtPath<MusicSet>(setPath);
                if (set == null)
                {
                    EnsureFolder(EventsRoot + "Music");
                    set = ScriptableObject.CreateInstance<MusicSet>();
                    AssetDatabase.CreateAsset(set, setPath);
                }
                if (set.layers == null || set.layers.Length < 4) Array.Resize(ref set.layers, 4);
                if (recipe.layer < set.layers.Length) set.layers[recipe.layer] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                EditorUtility.SetDirty(set);
                touched.Add(set);
            }
            return touched.Count;
        }

        private static AudioBus ParseBus(string bus, string category)
        {
            if (!string.IsNullOrEmpty(bus) && Enum.TryParse(bus, out AudioBus parsed)) return parsed;
            string folder = category.Split('/')[0];
            return folder switch
            {
                "Music" => AudioBus.Music,
                "Ambience" => AudioBus.Ambience,
                "Monster" => AudioBus.Monster,
                "UI" => AudioBus.Ui,
                "Voice" => AudioBus.Voice,
                "Narrative" => AudioBus.Voice,
                _ => AudioBus.Sfx
            };
        }

        // Campos lidos da receita (o resto é só do render_audio.py).
        [Serializable]
        private class Recipe
        {
            public string bus;
            public string music_set;
            public int layer = -1;
            public EventBlock @event;
        }

        [Serializable]
        private class EventBlock
        {
            public float[] volume;
            public float[] pitch;
            public bool spatial = true;
            public float maxDistance;
            public float noiseRadius;
            public float cooldown;
        }

        private static Recipe LoadRecipe(string category, string name)
        {
            string file = Path.Combine(RecipeRoot, category, name + ".json");
            if (!File.Exists(file)) return null;
            try
            {
                var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(file));
                // JsonUtility não aplica o padrão "spatial = true" quando o bloco existe sem o campo.
                if (recipe.@event != null && !File.ReadAllText(file).Contains("\"spatial\"")) recipe.@event.spatial = true;
                return recipe;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SoundEvents] Receita ilegível {file}: {e.Message}");
                return null;
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
