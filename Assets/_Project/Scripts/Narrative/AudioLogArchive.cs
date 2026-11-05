using System;
using System.Collections.Generic;

namespace SombraDaSanidade.Narrative
{
    // Logs ouvidos até o fim nesta sessão do jogo. A prancheta do menu de pausa (Frente 5) pode listar daqui.
    // Ainda não persiste entre partidas: entra junto com o checkpoint JSON (#45).
    public static class AudioLogArchive
    {
        private static readonly List<AudioLog> heard = new List<AudioLog>();

        public static IReadOnlyList<AudioLog> Heard => heard;

        public static event Action<AudioLog> Added;

        public static bool HasHeard(AudioLog log) => log != null && heard.Exists(l => l.id == log.id);

        public static void Add(AudioLog log)
        {
            if (log == null || HasHeard(log)) return;
            heard.Add(log);
            Added?.Invoke(log);
        }

        public static void Clear() => heard.Clear();
    }
}
