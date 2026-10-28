using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Monster
{
    // Anima o Espécime a partir da folha de sprites (Art/Monster), sem Animator Controller:
    // escolhe a animação pelo estado da IA e a direção pela velocidade do NavMeshAgent.
    // Folha: uma linha por animação, quadros da esquerda para a direita. Lado = direita; esquerda usa flipX.
    // A camada de brilho (LEDs, brasa, faíscas) usa a mesma grade num SpriteRenderer Unlit:
    // é o que deixa o monstro visível no escuro como "silhueta" (GDD 4.2).
    [RequireComponent(typeof(MonsterController))]
    public class MonsterSpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public struct Clip
        {
            public string name;
            [Tooltip("Linha na folha, de cima para baixo, começando em 0.")]
            public int row;
            public int frames;
            public float fps;
            public bool loop;

            public Clip(string name, int row, int frames, float fps, bool loop)
            {
                this.name = name; this.row = row; this.frames = frames; this.fps = fps; this.loop = loop;
            }
        }

        [Header("Folha de sprites")]
        [SerializeField] private Texture2D sheet;
        [SerializeField] private Texture2D glowSheet;
        [SerializeField] private Vector2Int frameSize = new Vector2Int(96, 128);
        [SerializeField] private float pixelsPerUnit = 32f;
        [Tooltip("Pivô do quadro (0-1). Embaixo no centro: o transform fica nos pés.")]
        [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.02f);

        [Header("Renderers")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer glow;
        [Tooltip("Luz pequena dos eletrodos. A cor e a intensidade seguem o estado.")]
        [SerializeField] private Light2D led;

        [Header("Animações (ordem da folha EspecimeB_sheet.png)")]
        [SerializeField] private Clip[] clips =
        {
            new Clip("idle", 0, 8, 6f, true),
            new Clip("walk_down", 1, 8, 8f, true),
            new Clip("walk_up", 2, 8, 8f, true),
            new Clip("walk_side", 3, 8, 8f, true),
            new Clip("listen", 4, 8, 6f, true),
            new Clip("scream", 5, 8, 10f, false),
            new Clip("run_down", 6, 8, 12f, true),
            new Clip("run_up", 7, 8, 12f, true),
            new Clip("run_side", 8, 8, 12f, true),
            new Clip("stun", 9, 8, 12f, true),
        };

        [Header("LED por estado")]
        [SerializeField] private Color patrolColor = new Color(0.17f, 0.41f, 0.57f);
        [SerializeField] private Color alertColor = new Color(0.31f, 0.72f, 1f);
        [SerializeField] private Color chaseColor = new Color(0.67f, 0.93f, 1f);
        [SerializeField] private float patrolIntensity = 0.35f;
        [SerializeField] private float alertIntensity = 0.7f;
        [SerializeField] private float chaseIntensity = 1.1f;

        private MonsterController monster;
        private Sprite[][] bodyFrames;
        private Sprite[][] glowFrames;
        private int currentClip = -1;
        private float clipTime;
        private Vector2 facing = Vector2.down;

        private void Awake()
        {
            monster = GetComponent<MonsterController>();
            if (sheet == null || body == null)
            {
                Debug.LogWarning($"{name}: MonsterSpriteAnimator sem folha ou SpriteRenderer; o monstro fica sem animação.", this);
                enabled = false;
                return;
            }
            bodyFrames = Slice(sheet);
            if (glowSheet != null && glow != null) glowFrames = Slice(glowSheet);
            Play(IndexOf("idle"));
            Apply();
        }

        private void Update()
        {
            Vector2 v = monster.Velocity;
            bool moving = v.sqrMagnitude > 0.04f;
            if (moving) facing = v;

            Play(IndexOf(PickClip(moving)));
            clipTime += Time.deltaTime * PlaybackRate();
            Apply();
            UpdateLed();
        }

        private string PickClip(bool moving)
        {
            switch (monster.State)
            {
                case MonsterState.Stunned:
                    return "stun";
                case MonsterState.Chase:
                    if (monster.IsDetecting) return "scream";
                    return moving ? "run_" + DirectionSuffix() : "listen";
                case MonsterState.Investigate:
                    return moving ? "walk_" + DirectionSuffix() : "listen";
                default:
                    return moving ? "walk_" + DirectionSuffix() : "idle";
            }
        }

        private string DirectionSuffix()
        {
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) return "side";
            return facing.y > 0f ? "up" : "down";
        }

        // A corrida acompanha a velocidade: com mais HDs ele corre mais rápido e a passada acelera junto.
        private float PlaybackRate()
        {
            if (monster.State != MonsterState.Chase || monster.IsDetecting) return 1f;
            return Mathf.Max(0.5f, monster.SpeedMultiplier);
        }

        private void Play(int index)
        {
            if (index < 0 || index == currentClip) return;
            currentClip = index;
            clipTime = 0f;
        }

        private void Apply()
        {
            if (currentClip < 0) return;
            var clip = clips[currentClip];
            int count = Mathf.Max(1, Mathf.Min(clip.frames, bodyFrames[clip.row].Length));
            int frame = Mathf.FloorToInt(clipTime * clip.fps);
            frame = clip.loop ? frame % count : Mathf.Min(frame, count - 1);

            bool flip = clip.name.EndsWith("_side") && facing.x < 0f;
            body.sprite = bodyFrames[clip.row][frame];
            body.flipX = flip;
            if (glowFrames != null)
            {
                glow.sprite = glowFrames[clip.row][frame];
                glow.flipX = flip;
            }
        }

        private void UpdateLed()
        {
            if (led == null) return;
            switch (monster.State)
            {
                case MonsterState.Stunned:
                    // Sobrecarga do EMP: pisca branco.
                    led.color = Color.white;
                    led.intensity = UnityEngine.Random.value < 0.5f ? chaseIntensity * 1.4f : 0.1f;
                    break;
                case MonsterState.Chase:
                    led.color = chaseColor;
                    led.intensity = chaseIntensity;
                    break;
                case MonsterState.Investigate:
                    led.color = alertColor;
                    led.intensity = alertIntensity;
                    break;
                default:
                    led.color = patrolColor;
                    led.intensity = patrolIntensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 2f));
                    break;
            }
        }

        private int IndexOf(string clipName)
        {
            for (int i = 0; i < clips.Length; i++)
                if (clips[i].name == clipName) return i;
            return -1;
        }

        private Sprite[][] Slice(Texture2D tex)
        {
            int rows = tex.height / frameSize.y;
            int cols = tex.width / frameSize.x;
            var result = new Sprite[rows][];
            for (int r = 0; r < rows; r++)
            {
                result[r] = new Sprite[cols];
                // A textura começa embaixo; a linha 0 da folha é a de cima.
                float y = tex.height - (r + 1) * frameSize.y;
                for (int c = 0; c < cols; c++)
                {
                    var rect = new Rect(c * frameSize.x, y, frameSize.x, frameSize.y);
                    var sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = $"{tex.name}_{r}_{c}";
                    result[r][c] = sprite;
                }
            }
            return result;
        }
    }
}
