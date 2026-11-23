using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Sanity;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Atalhos de teste no Play, para ver telas e ouvir sons sem jogar a fase inteira. Frente 5.
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

        // Bateria cheia: testar EMP e lanterna à vontade.
        [MenuItem("Sombra da Sanidade/Debug/Encher bateria", priority = 211)]
        public static void FillBattery() => SetBattery(1f);

        // 15%: abaixo dos 20% do aviso de bateria fraca (AudioConfig.batteryLowRatio).
        [MenuItem("Sombra da Sanidade/Debug/Bateria em 15%", priority = 212)]
        public static void LowBattery() => SetBattery(0.15f);

        [MenuItem("Sombra da Sanidade/Debug/Encher sanidade", priority = 221)]
        public static void FillSanity() => SetSanity(1f);

        // 25%: abaixo dos 35% em que entram a respiração ofegante, as alucinações e as pistas em fósforo.
        [MenuItem("Sombra da Sanidade/Debug/Sanidade em 25%", priority = 222)]
        public static void LowSanity() => SetSanity(0.25f);

        private static void SetBattery(float ratio)
        {
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            if (flashlight == null) { Debug.LogWarning("[Debug] Cena sem lanterna."); return; }
            float delta = flashlight.MaxBattery * ratio - flashlight.Battery;
            if (delta > 0f) flashlight.Recharge(delta);
            else flashlight.TrySpend(-delta);
        }

        private static void SetSanity(float ratio)
        {
            var sanity = Object.FindAnyObjectByType<SanityController>();
            if (sanity == null || sanity.Config == null) { Debug.LogWarning("[Debug] Cena sem sanidade."); return; }
            sanity.Add(sanity.Config.maxSanity * ratio - sanity.Value);
        }

        [MenuItem("Sombra da Sanidade/Debug/Vencer a ala atual", true)]
        [MenuItem("Sombra da Sanidade/Debug/Encher bateria", true)]
        [MenuItem("Sombra da Sanidade/Debug/Bateria em 15%", true)]
        [MenuItem("Sombra da Sanidade/Debug/Encher sanidade", true)]
        [MenuItem("Sombra da Sanidade/Debug/Sanidade em 25%", true)]
        private static bool InPlay() => EditorApplication.isPlaying;
    }
}
