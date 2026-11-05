using System;
using UnityEngine;

namespace SombraDaSanidade.Flashlight
{
    // As escolhas de upgrade da Lux-90, uma por HD (Proposta 2 do GDD v1.1): HD-01, HD-02, HD-03.
    // Referenciado pelo InteractionConfig. Frente 1. Issue #31.
    [CreateAssetMenu(fileName = "LuxUpgradeTree", menuName = "Sombra da Sanidade/Config/Árvore de upgrades da Lux-90")]
    public class LuxUpgradeTree : ScriptableObject
    {
        [Serializable]
        public class Choice
        {
            public LuxUpgrade optionA;
            public LuxUpgrade optionB;
        }

        [Tooltip("Índice 0 = depois do HD-01, 1 = HD-02, 2 = HD-03.")]
        public Choice[] choices = new Choice[3];

        // driveNumber começa em 1. Null se não houver escolha para esse HD.
        public Choice ForDrive(int driveNumber)
        {
            int i = driveNumber - 1;
            if (choices == null || i < 0 || i >= choices.Length) return null;
            var c = choices[i];
            return c != null && c.optionA != null && c.optionB != null ? c : null;
        }
    }
}
