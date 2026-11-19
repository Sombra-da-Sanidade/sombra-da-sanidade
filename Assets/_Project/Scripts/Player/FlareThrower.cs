using System;
using SombraDaSanidade.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.Player
{
    // Sinalizadores do Thomas: G joga um na direção do mouse (até throwRange, parando antes da parede).
    // Aceso, segura o Sonâmbulo parado dentro da luz (SignalFlare.Illuminates). Pega mais nos FlarePickup do mapa.
    // O contador aparece no canto da tela enquanto houver sinalizador.
    [RequireComponent(typeof(PlayerController))]
    public class FlareThrower : MonoBehaviour
    {
        [SerializeField] private int count = 1;
        [SerializeField] private int maxCount = 4;
        [SerializeField] private float throwRange = 5f;
        [SerializeField] private float flightTime = 0.45f;
        [Tooltip("Segundos acesa. O Sonâmbulo fica parado esse tempo se estiver na luz.")]
        [SerializeField] private float burnDuration = 8f;
        [Tooltip("Raio da luz que segura o Sonâmbulo.")]
        [SerializeField] private float lightRadius = 4f;
        [SerializeField] private LayerMask wallMask;

        [Header("Arte")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite[] litFrames;
        [SerializeField] private Sprite deadSprite;
        [SerializeField] private Material unlitMaterial;

        public int Count => count;
        public float BurnDuration => burnDuration;
        public float LightRadius => lightRadius;
        public SignalFlare LastFlare { get; private set; }

        public event Action<int> CountChanged;

        private PlayerController player;
        private Camera cam;
        private float flashUntil;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.gKey.wasPressedThisFrame) return;
            if (cam == null) cam = Camera.main;
            Vector2 target = (Vector2)transform.position + player.Facing * throwRange;
            var mouse = Mouse.current;
            if (mouse != null && cam != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                target = world;
            }
            TryThrow(target);
        }

        public void Add(int amount)
        {
            int before = count;
            count = Mathf.Clamp(count + amount, 0, maxCount);
            flashUntil = Time.unscaledTime + 1.5f;
            if (count != before) CountChanged?.Invoke(count);
        }

        public bool CanAdd => count < maxCount;

        // Joga em direção ao alvo, até o alcance; uma parede no caminho corta o voo antes dela.
        public bool TryThrow(Vector2 target)
        {
            if (count <= 0) { flashUntil = Time.unscaledTime + 1f; return false; }
            Vector2 from = transform.position;
            Vector2 dir = target - from;
            float dist = Mathf.Min(dir.magnitude, throwRange);
            if (dist < 0.01f) { dir = player.Facing; dist = 1f; }
            dir.Normalize();
            var hit = Physics2D.Raycast(from, dir, dist, wallMask);
            if (hit.collider != null) dist = Mathf.Max(0.3f, hit.distance - 0.35f);
            Vector2 to = from + dir * dist;

            count--;
            CountChanged?.Invoke(count);
            LastFlare = SignalFlare.Spawn(from, to, flightTime * Mathf.Max(0.4f, dist / throwRange), burnDuration, lightRadius,
                litFrames, deadSprite, unlitMaterial);
            return true;
        }

        // Ícone e quantidade no canto de baixo à direita (HUD em pixel art). Some sem sinalizador, a não ser logo
        // depois de tentar jogar sem nenhum.
        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || icon == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            bool flash = Time.unscaledTime < flashUntil;
            if (count <= 0 && !flash) return;
            UI.PixelHud.Begin();
            int px = UI.PixelHud.Px;
            int x = UI.PixelHud.Width - 34, y = UI.PixelHud.Height - 20;
            var tex = icon.texture;
            var r = icon.textureRect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, count > 0 ? 1f : 0.45f);
            GUI.DrawTextureWithTexCoords(new Rect(x * px, y * px, r.width * px, r.height * px), tex, uv);
            GUI.color = prev;
            var color = count > 0 ? (flash ? UI.PixelHud.Ambar2 : UI.PixelHud.Concreto7) : UI.PixelHud.Emergencia4;
            UI.PixelHud.Text($"x{count}", x + 13, y + 3, color);
            UI.PixelHud.Text("G", x + 1, y - 7, UI.PixelHud.Concreto5);
        }
    }
}
