using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Primitivas de desenho do HUD em "pixels de jogo": a tela é dividida pela base 320x180 e cada
    // pixel vira um quadrado inteiro, como a Pixel Perfect Camera. Usado em OnGUI (só no Repaint).
    // Cores só da paleta (art-src/palette.json); sprites e fonte vêm do HudArt (art-src/UI).
    public static class PixelHud
    {
        // Paleta do jogo, pelos nomes de art-src/palette.json.
        public static readonly Color Vazio = Hex(0x0b0c10);
        public static readonly Color Concreto1 = Hex(0x161920);
        public static readonly Color Concreto2 = Hex(0x222630);
        public static readonly Color Concreto3 = Hex(0x313744);
        public static readonly Color Concreto4 = Hex(0x444b59);
        public static readonly Color Concreto5 = Hex(0x5d6573);
        public static readonly Color Concreto6 = Hex(0x7b8390);
        public static readonly Color Concreto7 = Hex(0xa1a8b2);
        public static readonly Color Concreto8 = Hex(0xd3d6da);
        public static readonly Color Fosforo1 = Hex(0x0e2a22);
        public static readonly Color Fosforo2 = Hex(0x1a4d3a);
        public static readonly Color Fosforo3 = Hex(0x2c7d52);
        public static readonly Color Fosforo4 = Hex(0x55b870);
        public static readonly Color Fosforo5 = Hex(0xa9f294);
        public static readonly Color Eletrico3 = Hex(0x2d6ad0);
        public static readonly Color Eletrico4 = Hex(0x5bb2ff);
        public static readonly Color Emergencia1 = Hex(0x2b0a12);
        public static readonly Color Emergencia3 = Hex(0xa1222c);
        public static readonly Color Emergencia4 = Hex(0xdf4139);
        public static readonly Color Ambar1 = Hex(0xc8922c);
        public static readonly Color Ambar2 = Hex(0xf6d878);
        public static readonly Color Hipnos1 = Hex(0x3e2048);
        public static readonly Color Hipnos2 = Hex(0x7e4a8a);

        // Cor da tinta da fonte (chave 8). O tint é dividido por ela para a letra sair na cor pedida.
        private static readonly Color FontInk = Concreto8;

        // Tamanho de um pixel de jogo na tela. Atualizado por Begin() no começo de cada OnGUI.
        public static int Px { get; private set; } = 2;
        public static int Width => Screen.width / Px;
        public static int Height => Screen.height / Px;
        public static HudArt Art => HudArt.Instance;

        public static void Begin() => Px = Mathf.Max(2, Mathf.FloorToInt(Screen.height / 180f));

        // ---------- formas ----------

        public static void Box(int x, int y, int w, int h, Color color, float alpha = 1f) =>
            Fill(new Rect(x * Px, y * Px, w * Px, h * Px), color, alpha);

        public static void Fill(Rect rect, Color color, float alpha = 1f)
        {
            GUI.color = new Color(color.r, color.g, color.b, color.a * alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Tela inteira escurecida (pausa, fim de partida).
        public static void Dim(Color color, float alpha) => Fill(new Rect(0, 0, Screen.width, Screen.height), color, alpha);

        // Reta em pixels de jogo (ponteiro do multímetro).
        public static void Line(int x0, int y0, int x1, int y1, Color color, float alpha = 1f)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                Box(x0, y0, 1, 1, color, alpha);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // ---------- sprites ----------

        // Desenha um frame de art-src/UI. Retorna false se a arte não estiver disponível.
        public static bool Sprite(string sheet, string frame, int x, int y, float alpha = 1f, int scale = 1)
        {
            var art = Art;
            if (art == null || !art.TryGet(sheet, frame, out var tex, out var uv, out var size)) return false;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(new Rect(x * Px, y * Px, size.x * scale * Px, size.y * scale * Px), tex, uv, true);
            GUI.color = Color.white;
            return true;
        }

        public static bool HasArt => Art != null;

        // Moldura CRT 9-slice (screens.json, panel_*). Cantos de 16 px; w e h a partir de 32.
        public static void Panel(int x, int y, int w, int h)
        {
            const int T = 16;
            for (int yy = y; yy < y + h; yy += T)
            {
                int dy = Mathf.Min(yy, y + h - T);
                bool top = yy == y, bottom = yy + T >= y + h;
                for (int xx = x; xx < x + w; xx += T)
                {
                    int dx = Mathf.Min(xx, x + w - T);
                    bool left = xx == x, right = xx + T >= x + w;
                    string v = top ? "t" : bottom ? "b" : "";
                    string hz = left ? "l" : right ? "r" : "";
                    string pos = v + hz;
                    Sprite("screens", "panel_" + (pos.Length == 0 ? "c" : pos), dx, dy);
                }
            }
        }

        // Botão de terminal: btn_l + btn_c repetido + btn_r. Altura útil 11 px (linhas 2 a 12 do tile).
        public static void Button(string label, int x, int y, int w, bool selected)
        {
            string p = selected ? "btn_sel_" : "btn_";
            Sprite("screens", p + "l", x, y);
            for (int xx = x + 16; xx < x + w - 16; xx += 16) Sprite("screens", p + "c", Mathf.Min(xx, x + w - 32), y);
            Sprite("screens", p + "r", x + w - 16, y);
            TextCentered(label, x + w / 2 + (selected ? 3 : 0), y + 5, selected ? Fosforo5 : Fosforo3);
        }

        public static void Slider(int x, int y, int w, float value, bool selected)
        {
            for (int xx = 0; xx < w; xx += 16) Sprite("screens", "slider_track", x + Mathf.Min(xx, w - 16), y);
            int fill = Mathf.RoundToInt(w * Mathf.Clamp01(value));
            for (int xx = 0; xx + 16 <= fill; xx += 16) Sprite("screens", "slider_fill", x + xx, y);
            if (fill % 16 != 0)
            {
                // Pedaço final do preenchimento, igual às duas linhas de slider_fill.
                Box(x + fill - fill % 16, y + 7, fill % 16, 1, Fosforo4);
                Box(x + fill - fill % 16, y + 8, fill % 16, 1, Fosforo3);
            }
            Sprite("screens", selected ? "slider_knob_sel" : "slider_knob", x + fill - 8, y);
        }

        // ---------- texto (fonte bitmap, só maiúsculas) ----------

        // y é o topo da letra maiúscula; acentos sobem 2 px. Retorna a largura escrita.
        public static int Text(string text, int x, int y, Color color, float alpha = 1f, int scale = 1)
        {
            var art = Art;
            if (art == null || string.IsNullOrEmpty(text)) return 0;
            var cell = art.GlyphCell;
            var tint = new Color(
                Mathf.Clamp01(color.r / FontInk.r), Mathf.Clamp01(color.g / FontInk.g), Mathf.Clamp01(color.b / FontInk.b), alpha);
            int cx = x;
            foreach (char raw in text.ToUpperInvariant())
            {
                if (art.TryGlyph(raw, out var tex, out var uv, out int advance))
                {
                    GUI.color = tint;
                    GUI.DrawTextureWithTexCoords(
                        new Rect(cx * Px, (y - 2 * scale) * Px, cell.x * scale * Px, cell.y * scale * Px), tex, uv, true);
                }
                cx += advance * scale;
            }
            GUI.color = Color.white;
            return cx - x - scale;
        }

        public static int TextWidth(string text, int scale = 1)
        {
            var art = Art;
            if (art == null || string.IsNullOrEmpty(text)) return 0;
            int w = 0;
            foreach (char c in text.ToUpperInvariant())
            {
                art.TryGlyph(c, out _, out _, out int advance);
                w += advance * scale;
            }
            return w - scale;
        }

        public static void TextCentered(string text, int centerX, int y, Color color, float alpha = 1f, int scale = 1) =>
            Text(text, centerX - TextWidth(text, scale) / 2, y, color, alpha, scale);

        // Quebra em linhas que cabem em maxWidth pixels de jogo.
        public static System.Collections.Generic.List<string> Wrap(string text, int maxWidth)
        {
            var lines = new System.Collections.Generic.List<string>();
            string line = "";
            foreach (var word in text.Split(' '))
            {
                string attempt = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && TextWidth(attempt) > maxWidth)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = attempt;
            }
            if (line.Length > 0) lines.Add(line);
            return lines;
        }

        // ---------- utilidades ----------

        // Ponto do mundo em pixels de jogo (origem no canto superior esquerdo).
        public static Vector2Int WorldToGame(Camera cam, Vector3 world)
        {
            Vector3 s = cam.WorldToScreenPoint(world);
            return new Vector2Int(Mathf.RoundToInt(s.x / Px), Mathf.RoundToInt((Screen.height - s.y) / Px));
        }

        // Retângulo em pixels de jogo contém o mouse? (Event.current.mousePosition, origem em cima.)
        public static bool MouseOver(int x, int y, int w, int h)
        {
            if (Event.current == null) return false;
            var m = Event.current.mousePosition / Px;
            return m.x >= x && m.x < x + w && m.y >= y && m.y < y + h;
        }

        // 1 até "until", depois cai até 0 em "fadeSeconds". 0 se nunca foi mostrado.
        public static float Fade(float until, float fadeSeconds, float now)
        {
            if (until <= 0f) return 0f;
            if (now < until) return 1f;
            return Mathf.Clamp01(1f - (now - until) / Mathf.Max(0.01f, fadeSeconds));
        }

        public static float Fade(float until, float fadeSeconds) => Fade(until, fadeSeconds, Time.time);

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);
    }
}
