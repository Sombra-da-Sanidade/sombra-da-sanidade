using System;
using System.Linq;
using System.Reflection;
using SombraDaSanidade.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SombraDaSanidade.EditorTools
{
    // Gera Resources/SombraMixer.mixer (#101): Master com os grupos do AudioBus e o volume de cada grupo exposto
    // como "<Grupo>Volume" (dB). No WebGL o mixer só muda volume (ADR 0005), então não há efeitos nem snapshots extras.
    // A Unity não tem API pública para criar mixer: usa a API interna do Editor por reflexão.
    // Menu: Sombra da Sanidade > Áudio > Criar mixer. Batchmode: -executeMethod ...AudioMixerBuilder.BuildBatch -quit
    public static class AudioMixerBuilder
    {
        public const string MixerPath = "Assets/_Project/Resources/" + AudioService.MixerResource + ".mixer";

        private static readonly Assembly EditorAssembly = typeof(Editor).Assembly;
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public const string ConfigPath = "Assets/_Project/Resources/" + AudioConfig.ResourcePath + ".asset";

        [MenuItem("Sombra da Sanidade/Áudio/Criar mixer", priority = 201)]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AudioConfig>(), ConfigPath);

            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null) AssetDatabase.DeleteAsset(MixerPath);

            var controllerType = EditorAssembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            var controller = controllerType.GetMethod("CreateMixerControllerAtPath", All).Invoke(null, new object[] { MixerPath });
            var master = controllerType.GetProperty("masterGroup", All).GetValue(controller);

            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
            {
                var group = controllerType.GetMethod("CreateNewGroup", All).Invoke(controller, new object[] { bus.ToString(), false });
                controllerType.GetMethod("AddChildToParent", All).Invoke(controller, new[] { group, master });
                Expose(controllerType, controller, group, AudioService.VolumeParameter(bus));
            }

            EditorUtility.SetDirty((UnityEngine.Object)controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var missing = Enum.GetValues(typeof(AudioBus)).Cast<AudioBus>()
                .Where(b => mixer == null || mixer.FindMatchingGroups(b.ToString()).Length == 0).ToArray();
            if (missing.Length > 0) Debug.LogError("[Mixer] Grupos faltando: " + string.Join(", ", missing));
            else Debug.Log($"[Mixer] Criado em {MixerPath} com {Enum.GetValues(typeof(AudioBus)).Length} grupos.");
        }

        public static void BuildBatch()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogError("[Mixer] Falha: " + e); EditorApplication.Exit(1); }
        }

        // Expõe o volume do grupo e renomeia o parâmetro (o nome padrão é "MyExposedParam").
        private static void Expose(Type controllerType, object controller, object group, string name)
        {
            var groupType = group.GetType();
            var guid = groupType.GetMethod("GetGUIDForVolume", All).Invoke(group, null);
            var pathType = EditorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath", true);
            var path = Activator.CreateInstance(pathType, All, null, new[] { group, guid }, null);
            controllerType.GetMethod("AddExposedParameter", All).Invoke(controller, new[] { path });

            var exposedProp = controllerType.GetProperty("exposedParameters", All);
            var array = (Array)exposedProp.GetValue(controller);
            var elementType = array.GetType().GetElementType();
            var guidField = elementType.GetField("guid", All);
            var nameField = elementType.GetField("name", All);
            for (int i = 0; i < array.Length; i++)
            {
                var item = array.GetValue(i);
                if (!guidField.GetValue(item).Equals(guid)) continue;
                nameField.SetValue(item, name);
                array.SetValue(item, i);
            }
            exposedProp.SetValue(controller, array);
        }
    }
}
