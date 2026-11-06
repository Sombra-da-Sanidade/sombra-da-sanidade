using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Multímetro do skill check (art-src/UI/skillcheck.json), ao lado do Thomas durante a ejeção do HD.
    // O sprite é a caixa e o mostrador; a faixa good/great e o ponteiro são pintados por cima a partir do
    // arco guardado no HudArt. Aviso: LED e borda piscando. Resultado: mostrador aceso (great) ou faísca (falha).
    // Sem arte (HudArt ausente) não desenha nada. Frente 1, estilo da Frente 3. Issues #33 e #64.
    [RequireComponent(typeof(SkillCheck))]
    public class SkillCheckHud : MonoBehaviour
    {
        [Tooltip("Posição do canto superior esquerdo do multímetro em relação ao Thomas, em unidades do mundo.")]
        [SerializeField] private Vector2 worldOffset = new Vector2(-3.4f, 1.6f);

        private SkillCheck check;
        private float resultStart;
        private SkillCheckState lastState;

        private void Awake()
        {
            check = GetComponent<SkillCheck>();
        }

        private void Update()
        {
            if (check == null) return;
            if (check.State == SkillCheckState.Result && lastState != SkillCheckState.Result) resultStart = Time.time;
            lastState = check.State;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (check == null || check.State == SkillCheckState.Idle) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            var art = HudArt.Instance;
            var cam = Camera.main;
            if (art == null || cam == null) return;

            PixelHud.Begin();
            var origin = PixelHud.WorldToGame(cam, transform.position + (Vector3)worldOffset);
            int x = origin.x, y = origin.y;
            bool blink = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f;
            bool insane = check.IsShaky;

            string frame = insane ? "dial_hipnos" : "dial_body";
            if (check.State == SkillCheckState.Warning) frame = blink ? "dial_warn_0" : "dial_warn_1";
            else if (check.State == SkillCheckState.Result)
            {
                if (check.LastResult == SkillCheckResult.Fail)
                    frame = "dial_fail_" + Mathf.Min(2, Mathf.FloorToInt((Time.time - resultStart) * 6f));
                else if (check.LastResult == SkillCheckResult.Great)
                    frame = blink ? "dial_great_0" : "dial_great_1";
            }
            if (!PixelHud.Sprite("skillcheck", frame, x, y)) return;

            bool showZone = check.State == SkillCheckState.Active
                || (check.State == SkillCheckState.Result && check.LastResult == SkillCheckResult.Good);
            if (showZone)
            {
                float start = check.ZoneStart, good = start + check.ZoneSize, great = start + check.GreatSize;
                foreach (var p in art.dialArc)
                {
                    if (p.z < start || p.z > good) continue;
                    PixelHud.Box(x + (int)p.x, y + (int)p.y, 1, 1, p.z <= great ? PixelHud.Fosforo5 : PixelHud.Fosforo4);
                }
            }

            if (check.State == SkillCheckState.Active || check.State == SkillCheckState.Result)
            {
                // NeedlePosition já traz o tremor da sanidade baixa (SkillCheck).
                float a = Mathf.Lerp(art.dialAngles.x, art.dialAngles.y, Mathf.Clamp01(check.NeedlePosition)) * Mathf.Deg2Rad;
                int x1 = Mathf.RoundToInt(art.dialPivot.x + art.dialNeedleRadius * Mathf.Cos(a));
                int y1 = Mathf.RoundToInt(art.dialPivot.y - art.dialNeedleRadius * Mathf.Sin(a));
                PixelHud.Line(x + art.dialPivot.x, y + art.dialPivot.y, x + x1, y + y1, PixelHud.Concreto8);
            }
        }
    }
}
