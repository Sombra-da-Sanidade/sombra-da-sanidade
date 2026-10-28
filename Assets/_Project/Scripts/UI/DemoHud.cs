using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // HUD provisório da mini demo: bateria e HDs no canto superior esquerdo (GDD 7).
    // Frente 3 (Sanidade e HUD). Issue #9. Trocar por UI diegética (referência: PixelHud do Escura).
    public class DemoHud : MonoBehaviour
    {
        [SerializeField] private FlashlightController flashlight;

        private GUIStyle style;

        private void OnGUI()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            style ??= new GUIStyle(GUI.skin.label) { fontSize = 18 };
            style.normal.textColor = new Color(0.75f, 0.95f, 0.75f);

            if (flashlight != null)
            {
                const float barWidth = 160f;
                GUI.Label(new Rect(16, 12, 120, 24), "BATERIA", style);
                GUI.color = new Color(0.15f, 0.15f, 0.15f);
                GUI.DrawTexture(new Rect(110, 18, barWidth, 12), Texture2D.whiteTexture);
                GUI.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.75f, 0.95f, 0.75f), flashlight.BatteryRatio);
                GUI.DrawTexture(new Rect(110, 18, barWidth * flashlight.BatteryRatio, 12), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (GameManager.Instance != null)
                GUI.Label(new Rect(16, 40, 200, 24), $"HDs {GameManager.Instance.DrivesCollected}/{GameManager.Instance.DrivesRequired}", style);
        }
    }
}
