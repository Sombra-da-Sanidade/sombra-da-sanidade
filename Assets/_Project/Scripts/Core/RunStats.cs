using UnityEngine;

namespace SombraDaSanidade.Core
{
    // O que precisa sobreviver ao reinício da cena (o Restart recarrega tudo): as mortes da partida, para o rank (#32).
    // Zera ao começar uma partida pelo Menu e ao entrar no Play (o projeto não recarrega o domínio no Play).
    public static class RunStats
    {
        public static int Deaths { get; private set; }

        public static void StartRun() => Deaths = 0;

        public static void AddDeath() => Deaths++;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => StartRun();
    }
}
