using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Linha de objetivo da Ala de Energia (#42): subestação, depois o HD-02, depois a porta. Aparece no topo da
    // tela por alguns segundos quando o passo muda (GDD 8: HUD mínimo, sem números).
    public class WingObjectives : MonoBehaviour
    {
        public enum Step { PowerSubstation, EjectDrive, ReachExit, Done }

        [SerializeField] private Substation substation;
        [SerializeField] private DriveServer server;
        [SerializeField] private string driveLabel = "HD-02";
        [SerializeField] private float showSeconds = 8f;

        public Step Current { get; private set; } = (Step)(-1);
        public string Text { get; private set; } = "";

        private float visibleUntil;

        private void Update()
        {
            var gm = GameManager.Instance;
            Step next;
            if (gm != null && gm.State == GameState.Victory) next = Step.Done;
            else if (substation != null && !substation.IsPowered) next = Step.PowerSubstation;
            else if (gm != null && !gm.HasAllDrives) next = Step.EjectDrive;
            else next = Step.ReachExit;
            if (next == Current) return;

            Current = next;
            Text = next switch
            {
                Step.PowerSubstation => "RELIGUE A SUBESTAÇÃO",
                Step.EjectDrive => "EJETE O " + driveLabel,
                Step.ReachExit => "VOLTE À PORTA DO SAGUÃO",
                _ => ""
            };
            visibleUntil = Time.time + showSeconds;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || Text.Length == 0) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (UI.UpgradeTerminal.IsOpen || UI.ModalScreen.IsOpen || !UI.PixelHud.HasArt) return;
            float left = visibleUntil - Time.time;
            if (left <= 0f) return;

            UI.PixelHud.Begin();
            float alpha = Mathf.Clamp01(left / 0.8f);
            int w = UI.PixelHud.TextWidth(Text) + 8;
            int x = UI.PixelHud.Width / 2 - w / 2;
            UI.PixelHud.Box(x, 6, w, 11, UI.PixelHud.Vazio, 0.6f * alpha);
            UI.PixelHud.Box(x, 16, w, 1, UI.PixelHud.Eletrico3, alpha);
            UI.PixelHud.TextCentered(Text, UI.PixelHud.Width / 2, 8, UI.PixelHud.Eletrico4, alpha);
        }
    }
}
