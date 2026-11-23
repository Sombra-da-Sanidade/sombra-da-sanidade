using SombraDaSanidade.Core;
using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Final da partida na Eclusa (planejamento-rc 3, #95). A porta externa chama Reach em vez de vencer direto:
    // toca C5 "Amostra entregue" ou C4 "Testemunha" (Vance solta), libera a conquista "A Verdade Corporativa" no
    // final Testemunha e só então abre o laudo (GameManager.TriggerVictory). Sem escolha feita (Alfa, Beta), vence
    // direto como antes.
    public class EndingDirector : MonoBehaviour
    {
        public bool Reached { get; private set; }
        public Ending Ending { get; private set; }
        // A conquista foi liberada agora (o laudo mostra "CONQUISTA DESBLOQUEADA" só nesta vez).
        public static bool AchievementJustUnlocked { get; private set; }

        public static EndingDirector Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            AchievementJustUnlocked = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Reach()
        {
            if (Reached) return;
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            Reached = true;
            Ending = VanceChoice.Ending;
            if (Ending == Ending.None)
            {
                gm.TriggerVictory();
                return;
            }
            var scene = WingOpening.Load(Ending == Ending.Witness ? "C4" : "C5");
            CutscenePlayer.Play(scene, Finish);
        }

        private void Finish()
        {
            if (Ending == Ending.Witness) AchievementJustUnlocked = VanceChoice.UnlockAchievement();
            if (GameManager.Instance != null) GameManager.Instance.TriggerVictory();
        }
    }
}
