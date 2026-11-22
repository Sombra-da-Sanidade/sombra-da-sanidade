using SombraDaSanidade.Core;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Atalhos de teste no Play, para ver telas sem jogar a fase inteira. Frente 5.
    public static class DebugShortcuts
    {
        // Coleta os HDs que faltam e vence a ala aberta: mostra a tela de vitória (título, rank, PRÓXIMA ALA)
        // e salva o checkpoint, como uma vitória normal.
        [MenuItem("Sombra da Sanidade/Debug/Vencer a ala atual", priority = 200)]
        public static void WinCurrentWing()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying)
            {
                Debug.LogWarning("[Debug] Dê Play numa cena de ala (Scenes/Wings) antes de vencer.");
                return;
            }
            while (!gm.HasAllDrives) gm.CollectDrive();
            gm.TriggerVictory();
        }

        [MenuItem("Sombra da Sanidade/Debug/Vencer a ala atual", true)]
        private static bool CanWin() => EditorApplication.isPlaying;
    }
}
