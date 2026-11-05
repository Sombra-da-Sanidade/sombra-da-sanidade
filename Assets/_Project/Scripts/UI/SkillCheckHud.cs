using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Mostrador provisório do skill check (multímetro), em OnGUI no padrão do prompt do PlayerInteractor.
    // A arte final segue o estilo do HUD da Frente 3. Frente 1. Issue #33.
    [RequireComponent(typeof(SkillCheck))]
    public class SkillCheckHud : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(260f, 22f);
        [SerializeField] private float heightAbovePlayer = 1.6f;

        private SkillCheck check;
        private GUIStyle labelStyle;

        private static readonly Color Back = new Color(0.05f, 0.08f, 0.05f, 0.9f);
        private static readonly Color Good = new Color(0.3f, 0.8f, 0.35f);
        private static readonly Color Great = new Color(0.75f, 1f, 0.6f);
        private static readonly Color Needle = new Color(1f, 0.92f, 0.75f);
        private static readonly Color Fail = new Color(0.9f, 0.25f, 0.2f);

        private void Awake()
        {
            check = GetComponent<SkillCheck>();
        }

        private void OnGUI()
        {
            if (check == null || check.State == SkillCheckState.Idle) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            var cam = Camera.main;
            if (cam == null) return;

            labelStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * heightAbovePlayer);
            var rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);

            if (check.State == SkillCheckState.Warning)
            {
                labelStyle.normal.textColor = Needle;
                GUI.Label(new Rect(rect.x, rect.y - 4f, rect.width, rect.height), "! CALIBRAR [ESPAÇO]", labelStyle);
                return;
            }

            Fill(rect, Back);
            Fill(Part(rect, check.ZoneStart, check.ZoneSize), Good);
            Fill(Part(rect, check.ZoneStart, check.GreatSize), Great);
            Fill(new Rect(rect.x + rect.width * check.NeedlePosition - 1.5f, rect.y - 4f, 3f, rect.height + 8f), Needle);

            if (check.State == SkillCheckState.Result)
            {
                labelStyle.normal.textColor = check.LastResult == SkillCheckResult.Fail ? Fail : Great;
                string text = check.LastResult == SkillCheckResult.Great ? "PERFEITO"
                    : check.LastResult == SkillCheckResult.Good ? "OK" : "CURTO-CIRCUITO";
                GUI.Label(new Rect(rect.x, rect.y - rect.height - 4f, rect.width, rect.height), text, labelStyle);
            }
        }

        private static Rect Part(Rect r, float start, float length) =>
            new Rect(r.x + r.width * start, r.y, r.width * length, r.height);

        private static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
