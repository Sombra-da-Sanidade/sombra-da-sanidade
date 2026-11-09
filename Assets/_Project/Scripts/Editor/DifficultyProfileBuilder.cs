using System.IO;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera os DifficultyProfile das alas (#30) com os valores da tabela da Proposta 1 do GDD v1.1, em
    // ScriptableObjects/Config/Difficulty/DifficultyProfile_<ala>.asset. Rodar de novo sobrescreve com a tabela:
    // para ajustar no playtest, mude aqui (e na tabela do docs/gdd/gdd-v1.1-propostas.md).
    // Menu: Sombra da Sanidade > Gerar perfis de dificuldade.
    public static class DifficultyProfileBuilder
    {
        public const string Dir = "Assets/_Project/ScriptableObjects/Config/Difficulty";

        private struct Row
        {
            public string wing;
            public float speed, hearing, drain, battery, delay;
            public bool hallucinations;
            public Vector2 interval;
            public int breakers;
            public MonsterBehaviour behaviours;
        }

        // Proposta 1 do v1.1. Eclusa: monstro em fúria (4.6), sem bateria nos armários, sem disjuntores.
        private static readonly Row[] Table =
        {
            new Row { wing = "Medica", speed = 3.5f, hearing = 5f, drain = 1f, battery = 0.70f, hallucinations = false,
                interval = new Vector2(0f, 0f), breakers = 3, delay = 60f, behaviours = MonsterWingBehaviours.Medica },
            new Row { wing = "Energia", speed = 3.85f, hearing = 6f, drain = 1.5f, battery = 0.60f, hallucinations = true,
                interval = new Vector2(20f, 30f), breakers = 3, delay = 0f, behaviours = MonsterWingBehaviours.Energia },
            new Row { wing = "Executiva", speed = 4.2f, hearing = 7f, drain = 2f, battery = 0.45f, hallucinations = true,
                interval = new Vector2(15f, 25f), breakers = 2, delay = 0f, behaviours = MonsterWingBehaviours.Executiva },
            new Row { wing = "Eclusa", speed = 4.6f, hearing = 9f, drain = 2f, battery = 0f, hallucinations = true,
                interval = new Vector2(10f, 15f), breakers = 0, delay = 0f, behaviours = MonsterWingBehaviours.Executiva },
        };

        [MenuItem("Sombra da Sanidade/Gerar perfis de dificuldade", priority = 30)]
        public static void BuildAll()
        {
            foreach (var row in Table) GetOrBuild(row.wing);
            AssetDatabase.SaveAssets();
            Debug.Log($"Perfis de dificuldade gerados em {Dir}");
        }

        public static string PathFor(string wing) => $"{Dir}/DifficultyProfile_{wing}.asset";

        // Cria ou atualiza o perfil da ala com os valores da tabela.
        public static DifficultyProfile GetOrBuild(string wing)
        {
            var row = System.Array.Find(Table, r => r.wing == wing);
            if (row.wing == null)
            {
                Debug.LogError($"Ala {wing} não está na tabela da Proposta 1.");
                return null;
            }

            Directory.CreateDirectory(Dir);
            string path = PathFor(wing);
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(path);
            bool created = profile == null;
            if (created) profile = ScriptableObject.CreateInstance<DifficultyProfile>();

            profile.wing = row.wing;
            profile.monsterSpeed = row.speed;
            profile.hearingRadius = row.hearing;
            profile.behaviours = row.behaviours;
            profile.monsterDelaySeconds = row.delay;
            profile.darknessDrain = row.drain;
            profile.hallucinations = row.hallucinations;
            profile.hallucinationInterval = row.interval;
            profile.batteryChance = row.battery;
            profile.lootTable = AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/ScriptableObjects/Config/Loot/LootTable_{wing}.asset");
            profile.breakers = row.breakers;
            if (profile.lootTable == null) Debug.LogWarning($"LootTable_{wing} não existe: o perfil fica sem tabela de loot.");

            if (created) AssetDatabase.CreateAsset(profile, path);
            else EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
