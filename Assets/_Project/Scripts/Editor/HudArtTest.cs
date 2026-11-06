using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.UI;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Issue #64. Confere o HudArt (texturas, frames usados pelo código, fonte e arco do multímetro)
    // e a pausa (abre, congela o tempo, fecha e devolve). Roda em edit mode, sem cena.
    // Menu: Sombra da Sanidade > Rodar teste do HUD em pixel art.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.HudArtTest.RunBatch (sai com 0 ou 1).
    public static class HudArtTest
    {
        // Todo frame que o código desenha. Nome faltando = sprite invisível no jogo.
        private static readonly (string sheet, string frame)[] UsedFrames =
        {
            ("skillcheck", "dial_body"), ("skillcheck", "dial_hipnos"), ("skillcheck", "dial_warn_0"), ("skillcheck", "dial_warn_1"),
            ("skillcheck", "dial_great_0"), ("skillcheck", "dial_great_1"),
            ("skillcheck", "dial_fail_0"), ("skillcheck", "dial_fail_1"), ("skillcheck", "dial_fail_2"),
            ("hud_icons", "key_e"), ("hud_icons", "key_f"), ("hud_icons", "key_r"), ("hud_icons", "key_1"), ("hud_icons", "key_2"),
            ("hud_icons", "mouse_r"), ("hud_icons", "loot_battery"), ("hud_icons", "loot_sedative"), ("hud_icons", "loot_adrena"),
            ("hud_icons", "loot_empty"), ("hud_icons", "loot_plus"), ("hud_icons", "fx_adrena"), ("hud_icons", "fx_slow"),
            ("hud_icons", "emp_empty"),
            ("hud_bars", "hold_paused"), ("hud_bars", "key_space"), ("hud_bars", "key_tab"), ("hud_bars", "key_esc"),
            ("screens", "cursor_0"), ("screens", "slider_track"), ("screens", "slider_fill"), ("screens", "slider_knob"),
            ("screens", "slider_knob_sel"), ("screens", "check_on"), ("screens", "check_off"),
            ("screens", "btn_l"), ("screens", "btn_c"), ("screens", "btn_r"), ("screens", "btn_sel_l"), ("screens", "btn_sel_c"), ("screens", "btn_sel_r"),
            ("screens", "stamp_escaped"), ("screens", "stamp_dead_monster"), ("screens", "stamp_dead_mind"),
            ("screens", "hd_slot_full"), ("screens", "hd_slot_empty"),
            ("logo", "logo"), ("logo", "logo_glitch"),
        };

        // Textos fixos das telas: toda letra precisa existir na fonte.
        private static readonly string[] UsedText =
        {
            "AETHELGARD // TERMINAL DE SERVIÇO", "HD-01 EXTRAÍDO. LUX-90 DETECTADA.", "SELECIONE UMA MODIFICAÇÃO:",
            "VOCÊ ESCAPOU", "ECLUSA ABERTA. HDS ENTREGUES À SUPERFÍCIE.", "TEMPO 12:34", "HDS 3/3", "FALHAS NO MULTÍMETRO",
            "FIM DA LINHA", "SUA MENTE COLAPSOU NO ESCURO.", "O ESPÉCIME VAZIO TE ALCANÇOU.", "JOGAR DE NOVO", "REINICIAR",
            "PAUSADO", "CONTINUAR", "CONFIGURAÇÕES", "SAIR", "VOLUME", "BRILHO", "TELA CHEIA", "VOLTAR", "JOGAR",
            "AETHELGARD DYNAMICS // ORDEM DE SERVIÇO", "COMPLEXO EREBUS, SUB-NÍVEL 4. APAGÃO TOTAL.",
            "SEGURAR: INTERAGIR", "LANTERNA", "PULSO EMP", "RELÓGIO", "MULTÍMETRO", "PAUSA", "INICIAR TURNO", "CONFIRMAR",
            "Abrir armário", "Ejetar HD", "Reparar disjuntor", "Usar Estabilizador de Lítio", "Injetar Adrena-Syn (-sanidade)",
            "Ouvir a gravação de novo",
        };

        private static readonly List<string> results = new List<string>();
        private static bool failed;

        [MenuItem("Sombra da Sanidade/Rodar teste do HUD em pixel art", priority = 69)]
        public static void Run()
        {
            bool ok = Execute();
            Debug.Log(Report(ok));
        }

        public static void RunBatch()
        {
            bool ok = Execute();
            Debug.Log(Report(ok));
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Execute()
        {
            results.Clear();
            failed = false;

            var art = Resources.Load<HudArt>(HudArt.ResourcePath);
            Check("HudArt existe em Resources", art != null);
            if (art == null) return false;

            foreach (var sheet in art.sheets)
            {
                bool hasTexture = sheet.texture != null;
                Check($"{sheet.id}: textura ligada", hasTexture);
                if (!hasTexture) continue;
                int rows = (sheet.frames.Length + sheet.columns - 1) / sheet.columns;
                Check($"{sheet.id}: tamanho da textura bate com o JSON",
                    sheet.texture.width == sheet.cell.x * Mathf.Min(sheet.columns, sheet.frames.Length) && sheet.texture.height == sheet.cell.y * rows);
            }

            var missing = UsedFrames.Where(f => !art.Has(f.sheet, f.frame)).Select(f => f.sheet + "/" + f.frame).ToList();
            for (int i = 0; i <= 8; i++) if (!art.Has("hud_bars", "hold_" + i)) missing.Add("hud_bars/hold_" + i);
            foreach (LuxUpgradeEffect effect in System.Enum.GetValues(typeof(LuxUpgradeEffect)))
                if (!art.Has("hud_icons", DemoHud.UpgradeIcon(effect))) missing.Add("hud_icons/" + DemoHud.UpgradeIcon(effect));
            foreach (var pos in new[] { "tl", "t", "tr", "l", "c", "r", "bl", "b", "br" })
                if (!art.Has("screens", "panel_" + pos)) missing.Add("screens/panel_" + pos);
            Check("Todo frame usado pelo código existe" + (missing.Count > 0 ? ": falta " + string.Join(", ", missing) : ""), missing.Count == 0);

            var texts = new List<string>(UsedText);
            foreach (var guid in AssetDatabase.FindAssets("t:LuxUpgrade"))
            {
                var up = AssetDatabase.LoadAssetAtPath<LuxUpgrade>(AssetDatabase.GUIDToAssetPath(guid));
                if (up != null) { texts.Add(up.displayName); texts.Add(up.description); }
            }
            var noGlyph = new HashSet<char>();
            foreach (var t in texts)
                foreach (char c in t.ToUpperInvariant())
                    if (!art.TryGlyph(c, out _, out _, out _)) noGlyph.Add(c);
            Check("Fonte cobre os textos das telas e dos upgrades" + (noGlyph.Count > 0 ? ": falta '" + new string(noGlyph.ToArray()) + "'" : ""), noGlyph.Count == 0);

            string json = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "art-src", "UI", "skillcheck.json"));
            int arcInJson = System.Text.RegularExpressions.Regex.Matches(json, @"\[\s*\d+,\s*\d+,\s*[\d.]+\s*\]").Count;
            Check("Arco do multímetro carregado (pixels de 0 a 1)", art.dialArc.Length > 20
                && art.dialArc.Min(p => p.z) <= 0.05f && art.dialArc.Max(p => p.z) >= 0.95f);
            Check($"Arco do HudArt igual ao do JSON ({art.dialArc.Length} de {arcInJson} pixels)", arcInJson == art.dialArc.Length);

            // Pausa: em edit mode não há GameManager, então abre direto.
            var go = new GameObject("PauseMenuTest");
            try
            {
                Time.timeScale = 1f;
                var pause = go.AddComponent<PauseMenu>();
                pause.Open();
                Check("Pausa abre, congela o tempo e marca tela modal", pause.IsOpen && Time.timeScale == 0f && ModalScreen.IsOpen);
                pause.Close();
                Check("Pausa fecha e devolve o tempo", !pause.IsOpen && Time.timeScale == 1f && !ModalScreen.IsOpen);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Time.timeScale = 1f;
            }

            float volume = GameSettings.Volume;
            GameSettings.Volume = 2f;
            Check("Volume fica entre 0 e 1", Mathf.Approximately(GameSettings.Volume, 1f));
            GameSettings.Volume = volume;

            return !failed;
        }

        private static void Check(string label, bool ok)
        {
            results.Add((ok ? "OK    " : "FALHOU ") + label);
            if (!ok) failed = true;
        }

        private static string Report(bool ok)
        {
            var sb = new StringBuilder($"[HudArtTest] {(ok ? "PASSOU" : "FALHOU")}\n");
            foreach (var r in results) sb.AppendLine(r);
            return sb.ToString();
        }
    }
}
