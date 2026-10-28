using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // HUD provisório da mini demo: bateria, sanidade e HDs no canto superior esquerdo (GDD 7).
    // Frente 3 (Sanidade e HUD). Issue #9. Trocar por UI diegética (referência: PixelHud do Escura).
    public class DemoHud : MonoBehaviour
    {
        private const float BarX = 110f;
        private const float BarWidth = 160f;

        private static readonly Color TextColor = new Color(0.75f, 0.95f, 0.75f);
        private static readonly Color EmptyColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color DangerColor = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color MindColor = new Color(0.6f, 0.75f, 0.95f);
        private static readonly Color SafeColor = new Color(0.95f, 0.85f, 0.4f);

        [SerializeField] private FlashlightController flashlight;

        [Tooltip("Vazio: usa a do mesmo objeto da lanterna.")]
        [SerializeField] private SanityController sanity;

        private GUIStyle style;

        private void Start()
        {
            if (sanity == null && flashlight != null) sanity = flashlight.GetComponent<SanityController>();
        }

        private void OnGUI()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            style ??= new GUIStyle(GUI.skin.label) { fontSize = 18 };
            style.normal.textColor = TextColor;

            float y = 12f;

            if (flashlight != null && flashlight.enabled)
            {
                DrawBar(y, "BATERIA", flashlight.BatteryRatio, Color.Lerp(DangerColor, TextColor, flashlight.BatteryRatio));
                y += 28f;
            }

            if (sanity != null && sanity.enabled)
            {
                DrawBar(y, "MENTE", sanity.Ratio, Color.Lerp(DangerColor, MindColor, sanity.Ratio));
                DrawStatus(y);
                y += 28f;
            }

            if (GameManager.Instance != null)
                GUI.Label(new Rect(16, y, 200, 24), $"HDs {GameManager.Instance.DrivesCollected}/{GameManager.Instance.DrivesRequired}", style);
        }

        private void DrawBar(float y, string label, float ratio, Color fill)
        {
            GUI.Label(new Rect(16, y, 120, 24), label, style);
            GUI.color = EmptyColor;
            GUI.DrawTexture(new Rect(BarX, y + 6, BarWidth, 12), Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(BarX, y + 6, BarWidth * Mathf.Clamp01(ratio), 12), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Estado ao lado da barra; Zona Segura tem prioridade porque é a informação acionável.
        private void DrawStatus(float y)
        {
            string text;
            Color color;
            if (sanity.InSafeZone)
            {
                text = "ZONA SEGURA";
                color = SafeColor;
            }
            else
            {
                text = sanity.State switch
                {
                    SanityState.Anxious => "ansioso",
                    SanityState.Hallucinating => "alucinando",
                    SanityState.Collapsed => "COLAPSO",
                    _ => "lúcido"
                };
                color = sanity.State >= SanityState.Hallucinating ? DangerColor : TextColor;
            }

            var previous = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(new Rect(BarX + BarWidth + 10, y, 160, 24), text, style);
            style.normal.textColor = previous;
        }
    }
}
