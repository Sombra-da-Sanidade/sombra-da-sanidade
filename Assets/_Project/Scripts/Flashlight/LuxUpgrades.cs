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
            if (upgrade.effect == LuxUpgradeEffect.NeuralResonator && sanity != null)
                sanity.SetRevealCluesBelow(upgrade.value);
            // Todo upgrade entra na lista da lanterna (Installed), que o relógio e o checkpoint (#45) leem.
            // O Ressonador não tem efeito na lanterna: só é registrado.
            if (flashlight != null) flashlight.ApplyUpgrade(upgrade);
            Debug.Log($"[Lux-90] Upgrade instalado: {upgrade.displayName}");
        }
    }
}
