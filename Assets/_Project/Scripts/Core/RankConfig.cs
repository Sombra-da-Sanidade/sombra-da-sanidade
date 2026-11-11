using UnityEngine;

namespace SombraDaSanidade.Core
{
    public enum Rank { C, B, A, S }

    // Avaliação do auditor na vitória (#32, Proposta 3 do GDD v1.1): tempo 30%, sanidade média 30%,
    // EMPs 20% e mortes 20%. Cada critério vira uma nota de 0 a 1 e a soma ponderada dá o rank.
    // Fica em Resources/RankConfig.asset para valer em todas as cenas sem editar nenhuma, como o HudArt.
    [CreateAssetMenu(fileName = "RankConfig", menuName = "Sombra da Sanidade/Config/Rank")]
    public class RankConfig : ScriptableObject
    {
        public const string ResourcePath = "RankConfig";

        [Header("Pesos (Proposta 3)")]
        [Range(0f, 1f)] public float timeWeight = 0.3f;
        [Range(0f, 1f)] public float sanityWeight = 0.3f;
        [Range(0f, 1f)] public float empWeight = 0.2f;
        [Range(0f, 1f)] public float deathWeight = 0.2f;

        [Header("Notas de cada critério")]
        [Tooltip("Tempo ideal por HD da cena, em segundos: até aqui a nota de tempo é cheia. " +
                 "240 s = os 3 a 5 min por ala do GDD 7. Não está no GDD: ajustar no playtest.")]
        [Min(1f)] public float parSecondsPerDrive = 240f;
        [Tooltip("A nota de tempo chega a zero em (tempo ideal x este fator). Não está no GDD.")]
        [Min(1f)] public float zeroTimeFactor = 2.5f;
        [Tooltip("Com tantos EMPs a nota de EMP chega a zero. Não está no GDD.")]
        [Min(1)] public int empsForZero = 6;
        [Tooltip("Com tantas mortes a nota de mortes chega a zero. Não está no GDD.")]
        [Min(1)] public int deathsForZero = 3;

        [Header("Corte de cada rank (nota final de 0 a 1)")]
        [Range(0f, 1f)] public float sAbove = 0.85f;
        [Range(0f, 1f)] public float aAbove = 0.70f;
        [Range(0f, 1f)] public float bAbove = 0.50f;

        private static RankConfig cached;

        // Asset de Resources ou, sem ele, os valores padrão acima.
        public static RankConfig Load()
        {
            if (cached == null) cached = Resources.Load<RankConfig>(ResourcePath);
            if (cached == null) cached = CreateInstance<RankConfig>();
            return cached;
        }

        // sanityRatio: sanidade média de 0 a 1.
        public float Score(float seconds, int drivesRequired, float sanityRatio, int emps, int deaths)
        {
            float par = parSecondsPerDrive * Mathf.Max(1, drivesRequired);
            float time = 1f - Mathf.InverseLerp(par, par * zeroTimeFactor, seconds);
            float sanity = Mathf.Clamp01(sanityRatio);
            float emp = 1f - Mathf.Clamp01((float)emps / empsForZero);
            float death = 1f - Mathf.Clamp01((float)deaths / deathsForZero);
            float total = timeWeight + sanityWeight + empWeight + deathWeight;
            if (total <= 0f) return 0f;
            return (time * timeWeight + sanity * sanityWeight + emp * empWeight + death * deathWeight) / total;
        }

        public Rank RankFor(float score) =>
            score >= sAbove ? Rank.S : score >= aAbove ? Rank.A : score >= bAbove ? Rank.B : Rank.C;
    }
}
