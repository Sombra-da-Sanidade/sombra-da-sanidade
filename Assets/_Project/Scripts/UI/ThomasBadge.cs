using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    // Crachá do Thomas no menu de pausa (roteiro-rc 4, #89): "T. REIS / AUDITORIA" e, abaixo de 35 de sanidade,
    // "VOL-08 / HIPNOS". Fica em Resources para valer em todas as cenas sem editar nenhuma (como o RankConfig).
    // O asset Resources/ThomasBadge.asset aponta para Art/Props/sanity_cracha.png.
    public class ThomasBadge : ScriptableObject
    {
        public const string ResourcePath = "ThomasBadge";

        public Sprite normal;
        public Sprite shaken;
        [Range(1f, 100f)] public float threshold = 35f;

        private static ThomasBadge cached;

        public static ThomasBadge Load()
        {
            if (cached == null) cached = Resources.Load<ThomasBadge>(ResourcePath);
            return cached;
        }

        public static bool IsShaken()
        {
            var badge = Load();
            var sanity = Object.FindAnyObjectByType<SanityController>();
            return badge != null && sanity != null && sanity.Value < badge.threshold;
        }

        // x, y em pixels do HUD (PixelHud), escala inteira.
        public static void Draw(int x, int y, int scale)
        {
            var badge = Load();
            if (badge == null) return;
            var sprite = IsShaken() ? badge.shaken : badge.normal;
            if (sprite == null) return;
            var tex = sprite.texture;
            var r = sprite.textureRect;
            int px = PixelHud.Px;
            var screen = new Rect(x * px, y * px, r.width * scale * px, r.height * scale * px);
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            GUI.DrawTextureWithTexCoords(screen, tex, uv);
        }
    }
}
