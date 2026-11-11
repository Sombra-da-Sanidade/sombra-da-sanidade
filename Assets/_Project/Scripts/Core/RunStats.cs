using UnityEngine;

namespace SombraDaSanidade.Core
{
    // O que precisa sobreviver ao reinício da cena (o Restart recarrega tudo) e à troca de ala: as mortes da
    // partida e as estatísticas trazidas das alas anteriores pelo checkpoint (#32 e #45).
    // Zera ao começar uma partida pelo Menu e ao entrar no Play (o projeto não recarrega o domínio no Play).
    public static class RunStats
    {
        public static int Deaths { get; private set; }

        // Alas anteriores, vindas do checkpoint. O GameManager soma com a ala atual.
        public static float CarriedSeconds { get; private set; }
        public static int CarriedEmps { get; private set; }
        public static float CarriedSanitySum { get; private set; }
        public static float CarriedSanitySeconds { get; private set; }
        public static int CarriedSkillCheckFailures { get; private set; }

        // savedAtUtc do checkpoint já carregado: ao reiniciar a ala depois de um Game Over, o estado do Thomas
        // volta ao do checkpoint, mas as mortes desta tentativa não podem voltar.
        public static string LoadedCheckpoint { get; private set; }

        public static void StartRun()
        {
            Deaths = 0;
            CarriedSeconds = 0f;
            CarriedEmps = 0;
            CarriedSanitySum = 0f;
            CarriedSanitySeconds = 0f;
            CarriedSkillCheckFailures = 0;
            LoadedCheckpoint = null;
        }

        public static void AddDeath() => Deaths++;

        // Só na primeira vez que o checkpoint é carregado na partida.
        public static void LoadFrom(CheckpointData data)
        {
            if (data == null || data.savedAtUtc == LoadedCheckpoint) return;
            LoadedCheckpoint = data.savedAtUtc;
            Deaths = data.deaths;
            CarriedSeconds = data.totalSeconds;
            CarriedEmps = data.emps;
            CarriedSanitySum = data.sanitySum;
            CarriedSanitySeconds = data.sanitySeconds;
            CarriedSkillCheckFailures = data.skillCheckFailures;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => StartRun();
    }
}
