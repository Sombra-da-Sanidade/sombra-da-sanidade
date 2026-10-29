using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Player
{
    // Anima o Thomas a partir da folha Art/Sprites/thomas.png (fonte: art-src/Sprites/thomas.json),
    // sem Animator Controller, no mesmo esquema do MonsterSpriteAnimator.
    // 8 direções (GDD 9): 5 desenhadas (baixo, baixo-lado, lado, cima-lado, cima); as da esquerda usam flipX.
    // A direção vem da mira (PlayerController.Facing), então ele anda de costas se mirar para trás.
    //
    // O que decide a animação, nesta ordem:
    //   sanidade 0 → collapse (uma vez) · reparo → repair · ligou/desligou a lanterna parado → stow_light
    //   andando → walk_<dir> (run_<dir> com velocidade acima do normal, ex.: Adrena-Syn)
    //   parado → losing_mind (Alucinando/Colapsado), scared (Ansioso ou monstro perto) ou idle_<dir>
    // Com a lanterna desligada, ela vai para o cinto e as variantes "_free" têm as mãos livres.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        private readonly struct Clip
        {
            public readonly string Name;
            public readonly int Start;
            public readonly int Frames;
            public readonly float Fps;
            public readonly bool Loop;

            public Clip(string name, int start, int frames, float fps, bool loop = true)
            {
                Name = name; Start = start; Frames = frames; Fps = fps; Loop = loop;
            }
        }

        [Header("Folha de sprites")]
        [SerializeField] private Texture2D sheet;
        // 24x32 a 16 px/unidade: mesma densidade de pixel dos tiles (docs/arte/guia-de-estilo.md).
        [SerializeField] private Vector2Int frameSize = new Vector2Int(24, 32);
        [SerializeField] private float pixelsPerUnit = 16f;
        [Tooltip("Pivô do quadro (0-1). Embaixo no centro: o transform fica nos pés.")]
        [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0f);

        [Header("Renderer")]
        [SerializeField] private SpriteRenderer body;

        [Header("Limiares")]
        [Tooltip("Acima deste multiplicador de velocidade ele corre em vez de andar (Adrena-Syn: 1.35).")]
        [SerializeField] private float runSpeedMultiplier = 1.05f;

        // Ordem de art-src/Sprites/thomas.json. Fora do Inspector de propósito: cenas antigas não podem
        // guardar uma tabela desatualizada. Se a folha mudar, atualize aqui junto.
        private static readonly Clip[] Clips =
        {
            new Clip("idle_down", 0, 8, 6f),
            new Clip("idle_down_side", 8, 8, 6f),
            new Clip("idle_side", 16, 8, 6f),
            new Clip("idle_up_side", 24, 8, 6f),
            new Clip("idle_up", 32, 8, 6f),
            new Clip("walk_down", 40, 8, 10f),
            new Clip("walk_down_side", 48, 8, 10f),
            new Clip("walk_side", 56, 8, 10f),
            new Clip("walk_up_side", 64, 8, 10f),
            new Clip("walk_up", 72, 8, 10f),
            new Clip("idle_down_free", 80, 8, 6f),
            new Clip("idle_down_side_free", 88, 8, 6f),
            new Clip("idle_side_free", 96, 8, 6f),
            new Clip("idle_up_side_free", 104, 8, 6f),
            new Clip("idle_up_free", 112, 8, 6f),
            new Clip("walk_down_free", 120, 8, 10f),
            new Clip("walk_down_side_free", 128, 8, 10f),
            new Clip("walk_side_free", 136, 8, 10f),
            new Clip("walk_up_side_free", 144, 8, 10f),
            new Clip("walk_up_free", 152, 8, 10f),
            new Clip("run_down", 160, 8, 14f),
            new Clip("run_down_side", 168, 8, 14f),
            new Clip("run_side", 176, 8, 14f),
            new Clip("run_up_side", 184, 8, 14f),
            new Clip("run_up", 192, 8, 14f),
            new Clip("run_down_free", 200, 8, 14f),
            new Clip("run_down_side_free", 208, 8, 14f),
            new Clip("run_side_free", 216, 8, 14f),
            new Clip("run_up_side_free", 224, 8, 14f),
            new Clip("run_up_free", 232, 8, 14f),
            new Clip("repair", 240, 8, 8f),
            new Clip("scared", 248, 8, 8f),
            new Clip("stow_light", 256, 8, 10f, loop: false),
            new Clip("losing_mind", 264, 8, 10f),
            new Clip("collapse", 272, 8, 8f, loop: false),
        };

        // Setor de 45° (de -4 a 4, 0 = direita) → direção desenhada e se espelha.
        private static readonly string[] SectorDirection = { "side", "down_side", "down", "down_side", "side", "up_side", "up", "up_side", "side" };
        private static readonly bool[] SectorFlip = { true, true, false, false, false, false, false, true, true };

        private PlayerController player;
        private PlayerInteractor interactor;
        private FlashlightController flashlight;
        private SanityController sanity;
        private Sprite[] frames;
        private int currentClip = -1;
        private float clipTime;
        private bool reverse;

        private bool lightWasOn = true;
        private bool stowing;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            interactor = GetComponent<PlayerInteractor>();
            flashlight = GetComponent<FlashlightController>();
            sanity = GetComponent<SanityController>();
            if (sheet == null || body == null)
            {
                Debug.LogWarning($"{name}: PlayerSpriteAnimator sem folha ou SpriteRenderer; o Thomas fica sem animação.", this);
                enabled = false;
                return;
            }
            frames = Slice(sheet);
            if (flashlight != null) lightWasOn = flashlight.IsSwitchedOn;
            Play(IndexOf("idle_down"), false);
            Apply(false);
        }

        private void Update()
        {
            int sector = Mathf.RoundToInt(Mathf.Atan2(player.Facing.y, player.Facing.x) * Mathf.Rad2Deg / 45f) + 4;
            sector = Mathf.Clamp(sector, 0, SectorDirection.Length - 1);
            string dir = SectorDirection[sector];
            bool flip = SectorFlip[sector];

            bool lightOn = flashlight == null || flashlight.IsSwitchedOn;
            if (lightOn != lightWasOn)
            {
                lightWasOn = lightOn;
                // Guardar/sacar a lanterna só aparece parado; andando, a troca é instantânea.
                stowing = !player.IsMoving;
                if (stowing) Play(IndexOf("stow_light"), reverse: lightOn, force: true);
            }
            string free = lightOn ? "" : "_free";

            string clip;
            bool frontOnly = true;
            bool unscaled = false;
            var gm = GameManager.Instance;
            if (sanity != null && sanity.State == SanityState.Dead
                || gm != null && gm.State == GameState.GameOver && gm.LastDeathCause == DeathCause.Sanity)
            {
                // Game Over congela o tempo (timeScale 0): o colapso roda em tempo real.
                clip = "collapse";
                unscaled = true;
            }
            else if (interactor != null && interactor.IsChanneling)
            {
                // Reparo: de costas, mãos no painel (disjuntores ficam na parede).
                clip = "repair";
            }
            else if (player.IsMoving)
            {
                stowing = false;
                bool running = player.SpeedMultiplier > runSpeedMultiplier;
                clip = (running ? "run_" : "walk_") + dir + free;
                frontOnly = false;
            }
            else if (stowing)
            {
                clip = "stow_light";
            }
            else if (sanity != null && (sanity.State == SanityState.Hallucinating || sanity.State == SanityState.Collapsed))
            {
                clip = "losing_mind";
            }
            else if (sanity != null && (sanity.State == SanityState.Anxious || sanity.IsMonsterNear))
            {
                clip = "scared";
            }
            else
            {
                clip = "idle_" + dir + free;
                frontOnly = false;
            }
            if (frontOnly) flip = false;

            Play(IndexOf(clip), reverse && clip == "stow_light");
            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            // Adrena-Syn e Estabilizador mudam a velocidade; a passada acompanha.
            clipTime += dt * (player.IsMoving ? Mathf.Max(0.5f, player.SpeedMultiplier) : 1f);
            if (stowing && IsFinished()) stowing = false;
            Apply(flip);
        }

        private void Play(int index, bool reverse, bool force = false)
        {
            if (index < 0 || (index == currentClip && !force)) return;
            currentClip = index;
            this.reverse = reverse;
            clipTime = 0f;
        }

        private bool IsFinished()
        {
            if (currentClip < 0) return true;
            var clip = Clips[currentClip];
            return !clip.Loop && clipTime * clip.Fps >= clip.Frames;
        }

        private void Apply(bool flip)
        {
            if (currentClip < 0) return;
            var clip = Clips[currentClip];
            int count = Mathf.Max(1, clip.Frames);
            int step = Mathf.FloorToInt(clipTime * clip.Fps);
            step = clip.Loop ? step % count : Mathf.Min(step, count - 1);
            if (reverse) step = count - 1 - step;
            int frame = clip.Start + step;
            if (frame < 0 || frame >= frames.Length) return;
            body.sprite = frames[frame];
            body.flipX = flip;
        }

        private static int IndexOf(string clipName)
        {
            for (int i = 0; i < Clips.Length; i++)
                if (Clips[i].Name == clipName) return i;
            return -1;
        }

        private Sprite[] Slice(Texture2D tex)
        {
            int cols = tex.width / frameSize.x;
            int rows = tex.height / frameSize.y;
            var result = new Sprite[cols * rows];
            for (int i = 0; i < result.Length; i++)
            {
                int c = i % cols, r = i / cols;
                // A textura começa embaixo; a linha 0 da folha é a de cima.
                var rect = new Rect(c * frameSize.x, tex.height - (r + 1) * frameSize.y, frameSize.x, frameSize.y);
                var sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = $"{tex.name}_{i}";
                result[i] = sprite;
            }
            return result;
        }
    }
}
