using System.IO;
using SombraDaSanidade.Narrative;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera os 20 assets InnerVoiceLine do roteiro "Voluntário 08" em Resources/Narrative/Voices/ (#89).
    // Menu: Sombra da Sanidade > Gerar falas do Thomas (RC3).
    public static class NarrativeRc3Builder
    {
        public const string VoicesDir = "Assets/_Project/Resources/Narrative/Voices";

        [MenuItem("Sombra da Sanidade/Narrativa/Gerar falas do Thomas (RC3)", priority = 70)]
        public static void BuildAllVoices()
        {
            Directory.CreateDirectory(VoicesDir);

            // Fase 1: Ala Médica
            SaveVoice("T1-1",
                "Sub-Nível 4. Três HDs e eu volto pra casa.",
                "Três HDs. Três. Eu consigo contar até três.");
            SaveVoice("T1-2",
                "Escuro demais. A Lux-90 tem que durar.",
                "Tem alguém respirando no mesmo ritmo que eu.");
            SaveVoice("T1-3",
                "Eu sabia que a enfermaria ficava à esquerda?",
                "Eu já deitei nessa maca. Não. Não deitei.");
            SaveVoice("T1-4",
                "Luz. Ele não entra na luz do teto.",
                "A luz zumbe igual àquela sala branca.");
            SaveVoice("T1-5",
                "Ele não me viu. Ele me ouviu.",
                "Ele virou pra mim como se soubesse meu nome.");
            SaveVoice("T1-6",
                "Um. A Vance sabia que eu viria.",
                "Ela disse 'o 08'. Por que eu ouvi meu nome?");

            // Fase 2: Ala de Energia
            SaveVoice("T2-1",
                "Ele está do outro lado da parede. Andando junto.",
                "Ele está do outro lado da parede. Esperando por mim.");
            SaveVoice("T2-2",
                "Não tirar a luz dele. Não tirar a luz dele.",
                "Ele chegou mais perto. Eu não pisquei. Eu juro.");
            SaveVoice("T2-3",
                "Câmeras ligadas num prédio sem ninguém?",
                "Alguém está anotando cada passo meu.");
            SaveVoice("T2-4",
                "Termos de consentimento. Centenas.",
                "Essa assinatura é minha. Essa é minha também.");
            SaveVoice("T2-5",
                "Energia de volta. Agora o HD.",
                "O zumbido parou. Eu continuo ouvindo.");
            SaveVoice("T2-6",
                "Dois. O 03 foi o primeiro. Quem foi o 08?",
                "Oito. Oito. Por que esse número não sai da cabeça?");

            // Fase 3: Ala Executiva
            SaveVoice("T3-1",
                "Os dois aqui. Luz para um, escuro para o outro.",
                "Os dois aqui. Os dois sabem o meu cheiro.");
            SaveVoice("T3-2",
                "A câmera chamou ele. Eles estão ajudando o monstro.",
                "Não é o monstro que me caça. É o prédio.");
            SaveVoice("T3-3",
                "Só um espelho sujo.",
                "Esse não sou eu. Esse sou eu.");
            SaveVoice("T3-4",
                "Os números só aparecem quando eu perco o controle.",
                "Eu já sabia essa senha. Eu escrevi essa senha.");
            SaveVoice("T3-5",
                "Dói. Mas eu enxergo o que estava escondido.",
                "Mais uma dose. Eles sempre davam mais uma dose.");
            SaveVoice("T3-6",
                "Três. Acabou. Só falta a saída.",
                "Três. Não acabou. Nunca acaba no três.");

            // Eclusa (escolha da Vance)
            SaveVoice("T4-1",
                "Ela me apagou. E é a única que pode contar.",
                "Eu vou te tirar daí. Depois você me devolve quem eu era.");
            SaveVoice("T4-2",
                "Ela fez isso comigo. Que fique.",
                "Que ela conte de 10 para trás sozinha.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RC3] 20 falas do Thomas geradas em {VoicesDir}");
        }

        private static void SaveVoice(string id, string stable, string shaken)
        {
            string path = $"{VoicesDir}/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<InnerVoiceLine>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<InnerVoiceLine>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = id;
            asset.speaker = "THOMAS";
            asset.babblePitch = 0.575f;
            asset.stableLines = new[] { stable };
            asset.shakenLines = new[] { shaken };
            EditorUtility.SetDirty(asset);
        }
    }
}
