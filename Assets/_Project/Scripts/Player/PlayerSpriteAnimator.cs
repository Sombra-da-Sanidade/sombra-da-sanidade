using System;
using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.Player
{
    // Anima o Thomas a partir da folha Art/Sprites/thomas.png (fonte: art-src/Sprites/thomas.json),
    // sem Animator Controller, no mesmo esquema do MonsterSpriteAnimator.
    // 8 direções (GDD 9): 5 desenhadas (baixo, baixo-lado, lado, cima-lado, cima); as da esquerda usam flipX.
    // A direção vem da mira (PlayerController.Facing), então ele anda de costas se mirar para trás.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public struct Clip
        {
            public string name;
            [Tooltip("Índice do primeiro quadro na folha (da esquerda para a direita, de cima para baixo).")]
            public int start;
            public int frames;
            public float fps;

            public Clip(string name, int start, int frames, float fps)
            {
                this.name = name; this.start = start; this.frames = frames; this.fps = fps;
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

        [Header("Animações (ordem de art-src/Sprites/thomas.json)")]
        [SerializeField] private Clip[] clips =
        {
            new Clip("idle_down", 0, 4, 6f),
            new Clip("idle_down_side", 4, 4, 6f),
            new Clip("idle_side", 8, 4, 6f),
            new Clip("idle_up_side", 12, 4, 6f),
            new Clip("idle_up", 16, 4, 6f),
            new Clip("walk_down", 20, 6, 10f),
            new Clip("walk_down_side", 26, 6, 10f),
            new Clip("walk_side", 32, 6, 10f),
            new Clip("walk_up_side", 38, 6, 10f),
            new Clip("walk_up", 44, 6, 10f),
            new Clip("repair", 50, 4, 8f),
        };

        // Setor de 45° (de -4 a 4, 0 = direita) → direção desenhada e se espelha.
        private static readonly string[] SectorDirection = { "side", "down_side", "down", "down_side", "side", "up_side", "up", "up_side", "side" };
        private static readonly bool[] SectorFlip = { true, true, false, false, false, false, false, true, true };

        private PlayerController player;
        private PlayerInteractor interactor;
        private Sprite[] frames;
        private int currentClip = -1;
        private float clipTime;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            interactor = GetComponent<PlayerInteractor>();
            if (sheet == null || body == null)
            {
                Debug.LogWarning($"{name}: PlayerSpriteAnimator sem folha ou SpriteRenderer; o Thomas fica sem animação.", this);
                enabled = false;
                return;
            }
            frames = Slice(sheet);
            Play(IndexOf("idle_down"));
            Apply(false);
        }

        private void Update()
        {
            int sector = Mathf.RoundToInt(Mathf.Atan2(player.Facing.y, player.Facing.x) * Mathf.Rad2Deg / 45f) + 4;
            sector = Mathf.Clamp(sector, 0, SectorDirection.Length - 1);
            bool flip = SectorFlip[sector];

            string clip;
            if (interactor != null && interactor.IsChanneling)
            {
                // Reparo: de costas, mãos no painel (disjuntores ficam na parede).
                clip = "repair";
                flip = false;
            }
            else
            {
                clip = (player.IsMoving ? "walk_" : "idle_") + SectorDirection[sector];
            }

            Play(IndexOf(clip));
            // Adrena-Syn e Estabilizador mudam a velocidade; a passada acompanha.
            clipTime += Time.deltaTime * (player.IsMoving ? Mathf.Max(0.5f, player.SpeedMultiplier) : 1f);
            Apply(flip);
        }

        private void Play(int index)
        {
            if (index < 0 || index == currentClip) return;
            currentClip = index;
            clipTime = 0f;
        }

        private void Apply(bool flip)
        {
            if (currentClip < 0) return;
            var clip = clips[currentClip];
            int count = Mathf.Max(1, clip.frames);
            int frame = clip.start + Mathf.FloorToInt(clipTime * clip.fps) % count;
            if (frame < 0 || frame >= frames.Length) return;
            body.sprite = frames[frame];
            body.flipX = flip;
        }

        private int IndexOf(string clipName)
        {
            for (int i = 0; i < clips.Length; i++)
                if (clips[i].name == clipName) return i;
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
