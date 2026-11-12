using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Monster
{
    // Anima o Sonâmbulo pela folha Art/Monster/Sonambulo_sheet (48x64, 8 colunas):
    // linha 0 = poses travadas (0-7), 1 = walk_down, 2 = walk_up, 3 = walk_side, 4 = seen.
    // Travado mostra a pose sorteada pelo controlador. Andando, escolhe a direção pela velocidade.
    // A camada de brilho (visor roxo) é Unlit e às vezes falha quando ele está travado.
    [RequireComponent(typeof(SleepwalkerController))]
    public class SleepwalkerSpriteAnimator : MonoBehaviour
    {
        private const int RowPose = 0, RowDown = 1, RowUp = 2, RowSide = 3;

        [SerializeField] private Texture2D sheet;
        [SerializeField] private Texture2D glowSheet;
        [Tooltip("Mortes (80x80, 16 colunas): 0-31 engolir, 32-63 pescoço. Vazio: Game Over direto.")]
        [SerializeField] private Texture2D killSheet;
        [SerializeField] private Texture2D killGlowSheet;
        [SerializeField] private Vector2Int frameSize = new Vector2Int(48, 64);
        [SerializeField] private float pixelsPerUnit = 16f;
        [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.02f);
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer glow;
        [Tooltip("Luz roxa fraca do visor.")]
        [SerializeField] private Light2D visor;
        [SerializeField] private float walkFps = 14f;
        [SerializeField] private float visorIntensity = 0.5f;

        private SleepwalkerController walker;
        private Sprite[][] bodyFrames;
        private Sprite[][] glowFrames;
        private float walkTime;
        private Vector2 facing = Vector2.down;
        private bool flip;
        private float flickerUntil;

        private void Awake()
        {
            walker = GetComponent<SleepwalkerController>();
            if (sheet == null || body == null)
            {
                Debug.LogWarning($"{name}: SleepwalkerSpriteAnimator sem folha ou SpriteRenderer.", this);
                enabled = false;
                return;
            }
            bodyFrames = Slice(sheet);
            if (glowSheet != null && glow != null) glowFrames = Slice(glowSheet);
            if (killSheet != null)
            {
                var kill = GetComponent<MonsterKillScene>();
                if (kill == null) kill = gameObject.AddComponent<MonsterKillScene>();
                kill.Setup(killSheet, killGlowSheet, body, glow, pixelsPerUnit);
            }
            walker.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (walker != null) walker.StateChanged -= OnStateChanged;
        }

        // Ao travar, vira para o lado do jogador (a pose fica "olhando" para ele).
        private void OnStateChanged(SleepwalkerState state)
        {
            if (state != SleepwalkerState.Frozen) return;
            var player = FindAnyObjectByType<Player.PlayerController>();
            if (player != null) flip = player.transform.position.x < transform.position.x;
        }

        private void Update()
        {
            // Durante a morte quem desenha é o MonsterKillScene.
            if (walker.IsKilling)
            {
                if (visor != null) visor.intensity = 0f;
                return;
            }
            int row, col;
            Vector2 v = walker.Velocity;
            if (walker.State == SleepwalkerState.Moving && v.sqrMagnitude > 0.05f)
            {
                facing = v;
                walkTime += Time.deltaTime * walkFps * Mathf.Clamp(v.magnitude / 5f, 0.6f, 1.6f);
                col = Mathf.FloorToInt(walkTime) % 8;
                if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) { row = RowSide; flip = facing.x < 0f; }
                else { row = facing.y > 0f ? RowUp : RowDown; flip = false; }
            }
            else
            {
                row = RowPose;
                col = walker.State == SleepwalkerState.Dormant ? SleepwalkerController.DormantPose : walker.Pose;
            }

            row = Mathf.Min(row, bodyFrames.Length - 1);
            col = Mathf.Min(col, bodyFrames[row].Length - 1);
            body.sprite = bodyFrames[row][col];
            body.flipX = flip;

            // Travado e acordado, o visor falha de vez em quando (o "piscar" dele).
            if (walker.State == SleepwalkerState.Frozen && Random.value < Time.deltaTime * 0.35f)
                flickerUntil = Time.time + 0.12f;
            bool lit = Time.time >= flickerUntil;
            if (glowFrames != null)
            {
                glow.sprite = glowFrames[row][col];
                glow.flipX = flip;
                glow.enabled = lit;
            }
            if (visor != null) visor.intensity = lit ? visorIntensity * (walker.IsAwake ? 1f : 0.5f) : 0f;
        }

        private Sprite[][] Slice(Texture2D tex)
        {
            int rows = tex.height / frameSize.y;
            int cols = tex.width / frameSize.x;
            var result = new Sprite[rows][];
            for (int r = 0; r < rows; r++)
            {
                result[r] = new Sprite[cols];
                float y = tex.height - (r + 1) * frameSize.y;
                for (int c = 0; c < cols; c++)
                {
                    var sprite = Sprite.Create(tex, new Rect(c * frameSize.x, y, frameSize.x, frameSize.y), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = $"{tex.name}_{r}_{c}";
                    result[r][c] = sprite;
                }
            }
            return result;
        }
    }
}
