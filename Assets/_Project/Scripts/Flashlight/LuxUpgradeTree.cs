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

        // Upgrade pelo nome do asset (o checkpoint, #45, salva os nomes). Null se não estiver na árvore.
        public LuxUpgrade Find(string assetName)
        {
            if (choices == null || string.IsNullOrEmpty(assetName)) return null;
            foreach (var c in choices)
            {
                if (c == null) continue;
                if (c.optionA != null && c.optionA.name == assetName) return c.optionA;
                if (c.optionB != null && c.optionB.name == assetName) return c.optionB;
            }
            return null;
        }

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
