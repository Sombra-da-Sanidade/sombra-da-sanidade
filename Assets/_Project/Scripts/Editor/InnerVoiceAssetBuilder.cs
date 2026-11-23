using System.Collections.Generic;
using System.IO;
using System.Linq;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera as falas do Thomas (#89) a partir de docs/narrativa/roteiro-rc.md seção 2 e o crachá da pausa
    // (Resources/ThomasBadge). Mudar uma fala = editar aqui e gerar de novo. As alas pegam as falas pela ala
    // quando são geradas. Menu: Sombra da Sanidade > Narrativa > Gerar falas do Thomas.
    public static class InnerVoiceAssetBuilder
    {
        public const string Folder = "Assets/_Project/ScriptableObjects/Narrative/InnerVoice";
        private const string BadgePath = "Assets/_Project/Resources/ThomasBadge.asset";
        private const string BadgeArt = "Assets/_Project/Art/Props/sanity_cracha.png";

        private static readonly (string id, string wing, InnerVoiceTrigger trigger, string arg, string stable, string shaken)[] Script =
        {
            ("T1-1", "Medica", InnerVoiceTrigger.WingStart, "", "Sub-Nível 4. Três HDs e eu volto pra casa.", "Três HDs. Três. Eu consigo contar até três."),
            ("T1-2", "Medica", InnerVoiceTrigger.FirstDarkness, "", "Escuro demais. A Lux-90 tem que durar.", "Tem alguém respirando no mesmo ritmo que eu."),
            ("T1-3", "Medica", InnerVoiceTrigger.Area, "fala_medica_1", "Eu sabia que a enfermaria ficava à esquerda?", "Eu já deitei nessa maca. Não. Não deitei."),
            ("T1-4", "Medica", InnerVoiceTrigger.FirstBreaker, "", "Luz. Ele não entra na luz do teto.", "A luz zumbe igual àquela sala branca."),
            ("T1-5", "Medica", InnerVoiceTrigger.AfterCutscene, "C1", "Ele não me viu. Ele me ouviu.", "Ele virou pra mim como se soubesse meu nome."),
            ("T1-6", "Medica", InnerVoiceTrigger.DriveCollected, "", "Um. A Vance sabia que eu viria.", "Ela disse 'o 08'. Por que eu ouvi meu nome?"),
            ("T2-1", "Energia", InnerVoiceTrigger.WingStart, "", "Ele está do outro lado da parede. Andando junto.", "Ele está do outro lado da parede. Esperando por mim."),
            ("T2-2", "Energia", InnerVoiceTrigger.AfterCutscene, "C2", "Não tirar a luz dele. Não tirar a luz dele.", "Ele chegou mais perto. Eu não pisquei. Eu juro."),
            ("T2-3", "Energia", InnerVoiceTrigger.CameraSeen, "", "Câmeras ligadas num prédio sem ninguém?", "Alguém está anotando cada passo meu."),
            ("T2-4", "Energia", InnerVoiceTrigger.Area, "fala_energia_1", "Termos de consentimento. Centenas.", "Essa assinatura é minha. Essa é minha também."),
            ("T2-5", "Energia", InnerVoiceTrigger.SubstationPowered, "", "Energia de volta. Agora o HD.", "O zumbido parou. Eu continuo ouvindo."),
            ("T2-6", "Energia", InnerVoiceTrigger.DriveCollected, "", "Dois. O 03 foi o primeiro. Quem foi o 08?", "Oito. Oito. Por que esse número não sai da cabeça?"),
            ("T3-1", "Executiva", InnerVoiceTrigger.WingStart, "", "Os dois aqui. Luz para um, escuro para o outro.", "Os dois aqui. Os dois sabem o meu cheiro."),
            ("T3-2", "Executiva", InnerVoiceTrigger.CameraCall, "", "A câmera chamou ele. Eles estão ajudando o monstro.", "Não é o monstro que me caça. É o prédio."),
            ("T3-3", "Executiva", InnerVoiceTrigger.Area, "fala_executiva_1", "Só um espelho sujo.", "Esse não sou eu. Esse sou eu."),
            ("T3-4", "Executiva", InnerVoiceTrigger.ClueRead, "", "Os números só aparecem quando eu perco o controle.", "Eu já sabia essa senha. Eu escrevi essa senha."),
            ("T3-5", "Executiva", InnerVoiceTrigger.NeuralModule, "", "Dói. Mas eu enxergo o que estava escondido.", "Mais uma dose. Eles sempre davam mais uma dose."),
            ("T3-6", "Executiva", InnerVoiceTrigger.DriveCollected, "", "Três. Acabou. Só falta a eclusa.", "Três. Não acabou. Nunca acaba no três."),
            ("T4-1", "Eclusa", InnerVoiceTrigger.ChoiceSave, "", "Ela me apagou. E é a única que pode contar.", "Eu vou te tirar daí. Depois você me devolve quem eu era."),
            ("T4-2", "Eclusa", InnerVoiceTrigger.ChoiceLeave, "", "Ela fez isso comigo. Que fique.", "Que ela conte de 10 para trás sozinha."),
        };

        public static int Count => Script.Length;

        [MenuItem("Sombra da Sanidade/Narrativa/Gerar falas do Thomas", priority = 42)]
        public static void BuildFromMenu() => Build();

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.InnerVoiceAssetBuilder.Build
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            foreach (var s in Script)
            {
                string path = $"{Folder}/{s.id}.asset";
                var line = AssetDatabase.LoadAssetAtPath<InnerVoiceLine>(path);
                if (line == null)
                {
                    line = ScriptableObject.CreateInstance<InnerVoiceLine>();
                    AssetDatabase.CreateAsset(line, path);
                }
                line.id = s.id;
                line.wing = s.wing;
                line.trigger = s.trigger;
                line.arg = s.arg;
                line.stable = s.stable;
                line.shaken = s.shaken;
                EditorUtility.SetDirty(line);
            }
            BuildBadge();
            AssetDatabase.SaveAssets();
            Debug.Log($"Falas do Thomas geradas em {Folder} ({Script.Length}).");
        }

        public static InnerVoiceLine[] ForWing(string wing)
        {
            if (AssetDatabase.LoadAssetAtPath<InnerVoiceLine>($"{Folder}/T1-1.asset") == null) Build();
            return Script.Where(s => s.wing == wing)
                .Select(s => AssetDatabase.LoadAssetAtPath<InnerVoiceLine>($"{Folder}/{s.id}.asset"))
                .Where(l => l != null).ToArray();
        }

        public static Sprite[] SanitySprites(string name)
        {
            string path = $"Assets/_Project/Art/Props/sanity_{name}.png";
            MonsterPrefabBuilder.EnsureArtImport(path);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var normal = sprites.FirstOrDefault(s => s.name.EndsWith("_normal"));
            var shaken = sprites.FirstOrDefault(s => s.name.EndsWith("_abalado"));
            if (normal == null || shaken == null)
                Debug.LogWarning($"Sprites de sanity_{name} não encontrados. Rode python3 tools/art/export_sprites.py.");
            return new[] { normal, shaken };
        }

        private static void BuildBadge()
        {
            var badge = AssetDatabase.LoadAssetAtPath<ThomasBadge>(BadgePath);
            if (badge == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BadgePath));
                badge = ScriptableObject.CreateInstance<ThomasBadge>();
                AssetDatabase.CreateAsset(badge, BadgePath);
            }
            var sprites = SanitySprites("cracha");
            badge.normal = sprites[0];
            badge.shaken = sprites[1];
            EditorUtility.SetDirty(badge);
        }
    }
}
