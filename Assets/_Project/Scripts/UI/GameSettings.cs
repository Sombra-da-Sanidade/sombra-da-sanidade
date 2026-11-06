using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.UI
{
    // Preferências do jogador (GDD 10: PlayerPrefs para áudio e brilho), mais tela cheia.
    // O brilho usa um Volume global próprio criado em runtime (mesmo padrão do SanityVignette),
    // para não mexer no profile de ninguém. Issue #64.
    public static class GameSettings
    {
        private const string VolumeKey = "sombra.volume";
        private const string BrightnessKey = "sombra.brightness";
        private const string FullscreenKey = "sombra.fullscreen";

        // Brilho 0..1, com 0,5 neutro. Os extremos somam ou tiram este tanto de exposição (EV).
        private const float BrightnessRangeEv = 1.2f;

        private static UnityEngine.Rendering.Volume brightnessVolume;
        private static ColorAdjustments colorAdjustments;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); Apply(); }
        }

        public static float Brightness
        {
            get => PlayerPrefs.GetFloat(BrightnessKey, 0.5f);
            set { PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp01(value)); Apply(); }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); Apply(); }
        }

        public static void Save() => PlayerPrefs.Save();

        // Aplica tudo. Chamado ao carregar cada cena (PauseMenu) e a cada mudança.
        public static void Apply()
        {
            AudioListener.volume = Volume;
            if (!Application.isEditor && Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
            EnsureVolume();
            if (colorAdjustments != null) colorAdjustments.postExposure.Override((Brightness - 0.5f) * 2f * BrightnessRangeEv);
        }

        private static void EnsureVolume()
        {
            // Só em jogo. O objeto some ao sair do Play Mode; a referência estática fica "nula" pela checagem da Unity.
            if (!Application.isPlaying) return;
            if (brightnessVolume != null && colorAdjustments != null) return;
            var go = new GameObject("BrightnessVolume");
            Object.DontDestroyOnLoad(go);
            var volume = brightnessVolume = go.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            colorAdjustments = profile.Add<ColorAdjustments>(true);
            volume.sharedProfile = profile;
        }
    }
}
