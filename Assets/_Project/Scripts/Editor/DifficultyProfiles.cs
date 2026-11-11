using SombraDaSanidade.Core;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Acesso aos assets de DifficultyProfile (#30) para os builders das alas. Os assets e os valores da
    // Proposta 1 são da Frente 5 (ver docs/dificuldade.md); aqui só carrega.
    public static class DifficultyProfiles
    {
        public const string Dir = "Assets/_Project/ScriptableObjects/Config";

        public static string PathFor(string wing) => $"{Dir}/Difficulty{wing}.asset";

        public static DifficultyProfile GetOrCreate(string wing)
        {
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(PathFor(wing));
            if (profile == null) Debug.LogError($"Perfil de dificuldade não encontrado: {PathFor(wing)}");
            return profile;
        }
    }
}
