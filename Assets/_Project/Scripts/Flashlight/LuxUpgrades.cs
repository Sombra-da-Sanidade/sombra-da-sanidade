using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Flashlight
{
    // Aplica um LuxUpgrade em tempo de execução, sem escrever nos assets de config. Frente 1. Issue #31.
    public static class LuxUpgrades
    {
        public static void Apply(LuxUpgrade upgrade, FlashlightController flashlight, SanityController sanity)
        {
            if (upgrade == null) return;
            switch (upgrade.effect)
            {
                case LuxUpgradeEffect.NeuralResonator:
                    if (sanity != null) sanity.SetRevealCluesBelow(upgrade.value);
                    break;
                default:
                    if (flashlight != null) flashlight.ApplyUpgrade(upgrade);
                    break;
            }
            Debug.Log($"[Lux-90] Upgrade instalado: {upgrade.displayName}");
        }
    }
}
