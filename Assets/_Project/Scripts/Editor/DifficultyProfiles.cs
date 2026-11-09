using System.IO;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Assets de DifficultyProfile (#30) com os valores iniciais da tabela da Proposta 1 do v1.1.
    // Só cria o que falta: um asset que já existe mantém os valores ajustados no playtest.
    public static class DifficultyProfiles
    {
        public const string Dir = "Assets/_Project/ScriptableObjects/Config/Difficulty";

        public static string PathFor(string wing) => $"{Dir}/Difficulty_{wing}.asset";

        [MenuItem("Sombra da Sanidade/Criar perfis de dificuldade (Proposta 1)", priority = 30)]
        public static void CreateAll()
        {
            foreach (var wing in new[] { "Medica", "Energia", "Executiva", "Eclusa" }) GetOrCreate(wing);
            Debug.Log($"Perfis de dificuldade em {Dir}");
        }

        public static DifficultyProfile GetOrCreate(string wing)
        {
            var path = PathFor(wing);
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(path);
            if (profile != null) return profile;

            Directory.CreateDirectory(Dir);
            profile = ScriptableObject.CreateInstance<DifficultyProfile>();
            Fill(profile, wing);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // | Parâmetro              | Médica | Energia | Executiva | Eclusa |
        // | Velocidade do monstro  | 3.5    | 3.85    | 4.2       | 4.6    |
        // | Raio auditivo          | 5      | 6       | 7         | 9      |
        // | Dreno no escuro (/s)   | 1.0    | 1.5     | 2.0       | 2.0    |
        // | Bateria por armário    | 70%    | 60%     | 45%       | 0%     |
        // | Alucinação (s)         | não    | 20-30   | 15-25     | 10-15  |
        // | Disjuntores            | 3      | 3       | 2         | 0      |
        // | Monstro desde          | 1 min  | entrada | entrada   | entrada|
        private static void Fill(DifficultyProfile p, string wing)
        {
            p.wing = wing;
            switch (wing)
            {
                case "Medica":
                    Set(p, 3.5f, 5f, 1f, 0.7f, false, 0f, 0f, 3, 60f, MonsterWingBehaviours.Medica);
                    break;
                case "Energia":
                    Set(p, 3.85f, 6f, 1.5f, 0.6f, true, 20f, 30f, 3, 0f, MonsterWingBehaviours.Energia);
                    break;
                case "Executiva":
                    Set(p, 4.2f, 7f, 2f, 0.45f, true, 15f, 25f, 2, 0f, MonsterWingBehaviours.Executiva);
                    break;
                default:
                    Set(p, 4.6f, 9f, 2f, 0f, true, 10f, 15f, 0, 0f, MonsterWingBehaviours.Executiva);
                    break;
            }
        }

        private static void Set(DifficultyProfile p, float speed, float hearing, float drain, float battery,
            bool hallucinations, float hMin, float hMax, int breakers, float delay, MonsterBehaviour behaviours)
        {
            p.monsterSpeed = speed;
            p.hearingRadius = hearing;
            p.darknessDrain = drain;
            p.batteryChance = battery;
            p.hallucinationsEnabled = hallucinations;
            p.hallucinationMinInterval = hMin;
            p.hallucinationMaxInterval = hMax;
            p.breakersAvailable = breakers;
            p.monsterDelaySeconds = delay;
            p.monsterBehaviours = behaviours;
        }
    }
}
