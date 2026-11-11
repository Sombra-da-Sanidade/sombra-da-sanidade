using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Telas de fim de partida com o kit CRT (screens.json). Chamado pelo OnGUI do GameManager.
    // Vitória: gavetas de HD, rank S/A/B/C e estatísticas (#32). Game over: selo da causa
    // (garras vermelhas para o Espécime, espiral roxa para a mente). Fora do jogo número pode aparecer. Issue #64.
    public static class ResultScreen
    {
        private static int cachedFailures = -1;

        // Retorna false se não houver arte; o GameManager mostra a versão de texto.
        public static bool Draw(GameManager gm)
        {
            if (!PixelHud.HasArt) return false;
            PixelHud.Begin();
            var e = Event.current;

            const int pw = 288, ph = 164;
            int x = (PixelHud.Width - pw) / 2, y = (PixelHud.Height - ph) / 2;
            int bx = x + (pw - 112) / 2, by = y + ph - 26;
            if (e.type == EventType.MouseDown && e.button == 0 && PixelHud.MouseOver(bx, by + 2, 112, 11))
            {
                e.Use();
                cachedFailures = -1;
                gm.Restart();
                return true;
            }
            if (e.type != EventType.Repaint) return true;

            bool victory = gm.State == GameState.Victory;
            bool blink = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f;
            if (victory) DrawVictory(gm, x, y, pw, ph);
            else DrawGameOver(gm, x, y, pw, ph);
            PixelHud.Button(victory ? "JOGAR DE NOVO" : "REINICIAR", bx, by, 112, blink);
            PixelHud.Sprite("hud_icons", "key_r", bx + 112 + 6, by + 4);
            return true;
        }

        private static void DrawVictory(GameManager gm, int x, int y, int w, int h)
        {
            PixelHud.Dim(PixelHud.Vazio, 0.9f);
            PixelHud.Panel(x, y, w, h);
            PixelHud.Sprite("screens", "stamp_escaped", x + 14, y + 12, 1f, 2);
            // Fim de ala (Ala Médica no vertical slice): o título é a ala e o checkpoint é a recompensa.
            var wing = Level.WingCheckpoint.Instance;
            if (wing != null)
            {
                PixelHud.Text(Level.WingCheckpoint.DisplayName(wing.Wing) + " CONCLUÍDA", x + 52, y + 18, PixelHud.Fosforo5, 1f, 2);
                PixelHud.Text("PORTA PARA O SAGUÃO DESTRAVADA.", x + 52, y + 36, PixelHud.Fosforo3);
                if (wing.StatusLine.Length > 0)
                    PixelHud.TextCentered(wing.StatusLine, x + w / 2, y + 123, wing.Saved ? PixelHud.Fosforo5 : PixelHud.Emergencia4);
            }
            else
            {
                PixelHud.Text("VOCÊ ESCAPOU", x + 52, y + 18, PixelHud.Fosforo5, 1f, 2);
                PixelHud.Text("ECLUSA ABERTA. HDS ENTREGUES À SUPERFÍCIE.", x + 52, y + 36, PixelHud.Fosforo3);
            }

            for (int i = 0; i < gm.DrivesRequired; i++)
                PixelHud.Sprite("screens", i < gm.DrivesCollected ? "hd_slot_full" : "hd_slot_empty", x + 14 + i * 18, y + 56);

            if (cachedFailures < 0)
            {
                cachedFailures = 0;
                foreach (var server in Object.FindObjectsByType<DriveServer>(FindObjectsSortMode.None))
                    cachedFailures += server.SkillCheckFailures;
            }
            // Avaliação do auditor (#32, Proposta 3): selo do rank e o melhor já tirado nesta cena.
            int rankX = x + w - 46;
            PixelHud.TextCentered("RANK", rankX + 16, y + 44, PixelHud.Fosforo3);
            PixelHud.Sprite("screens", "rank_" + gm.FinalRank, rankX, y + 52, 1f, 2);
            var best = GameManager.BestRank(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            if (gm.IsNewBestRank) PixelHud.Text("NOVO MELHOR RANK", x + 14, y + 76, PixelHud.Ambar2);
            else if (best != null) PixelHud.Text("MELHOR RANK: " + best.Value, x + 14, y + 76, PixelHud.Fosforo3);

            int left = x + 14, leftEnd = x + 136, right = x + 150, rightEnd = x + w - 14;
            Stat("TEMPO", FormatTime(gm.ElapsedTime), left, y + 88, leftEnd);
            Stat("HDS", $"{gm.DrivesCollected}/{gm.DrivesRequired}", left, y + 99, leftEnd);
            Stat("FALHAS MULTÍMETRO", cachedFailures.ToString(), left, y + 110, leftEnd);
            Stat("SANIDADE MÉDIA", Mathf.RoundToInt(gm.AverageSanity * 100f) + "%", right, y + 88, rightEnd);
            Stat("EMPS", gm.EmpsUsed.ToString(), right, y + 99, rightEnd);
            Stat("MORTES", gm.Deaths.ToString(), right, y + 110, rightEnd);
        }

        private static void DrawGameOver(GameManager gm, int x, int y, int w, int h)
        {
            bool mind = gm.LastDeathCause == DeathCause.Sanity;
            PixelHud.Dim(PixelHud.Emergencia1, 1f);
            PixelHud.Dim(PixelHud.Vazio, 0.75f);
            int cx = PixelHud.Width / 2;
            PixelHud.Sprite("screens", mind ? "stamp_dead_mind" : "stamp_dead_monster", cx - 24, y + 4, 1f, 3);
            PixelHud.TextCentered("FIM DA LINHA", cx, y + 64, mind ? PixelHud.Hipnos2 : PixelHud.Emergencia4, 1f, 2);
            PixelHud.TextCentered(mind ? "SUA MENTE COLAPSOU NO ESCURO." : "O ESPÉCIME VAZIO TE ALCANÇOU.", cx, y + 84, PixelHud.Concreto7);
            cachedFailures = -1;
        }

        private static void Stat(string label, string value, int x, int y, int right)
        {
            PixelHud.Text(label, x, y, PixelHud.Fosforo3);
            PixelHud.Text(value, right - PixelHud.TextWidth(value), y, PixelHud.Fosforo5);
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
