using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.Player
{
    // Movimento WASD independente da mira pelo mouse (GDD 4.5 e 7).
    // Portado do PlayerController do projeto Escura, só com movimento e mira.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        [Header("Referências")]
        [Tooltip("Filho com o sprite. Gira para a direção da mira se rotateVisual estiver ligado.")]
        [SerializeField] private Transform visual;
        [Tooltip("Desligue com sprite em vista 3/4 (PlayerSpriteAnimator escolhe a direção pela mira).")]
        [SerializeField] private bool rotateVisual = true;
        [SerializeField] private float visualRotationOffset = -90f;

        public Vector2 Facing { get; private set; } = Vector2.down;
        public Vector2 MoveInput { get; private set; }
        public bool IsMoving => MoveInput.sqrMagnitude > 0.01f;

        // Multiplicador para efeitos temporários (Adrena-Syn, Estabilizador de Lítio).
        public float SpeedMultiplier { get; set; } = 1f;

        private Rigidbody2D rb;
        private Camera cam;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            cam = Camera.main;
        }

        private void Update()
        {
            ReadMovement();
            UpdateFacingFromMouse();
        }

        private void FixedUpdate()
        {
            float speed = (config != null ? config.moveSpeed : 4f) * SpeedMultiplier;
            rb.linearVelocity = MoveInput * speed;
        }

        private void ReadMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

            var input = new Vector2(x, y);
            MoveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void UpdateFacingFromMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || cam == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            Vector2 dir = (Vector2)world - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.0001f) return;

            Facing = dir.normalized;

            if (visual != null && rotateVisual)
            {
                float angle = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg + visualRotationOffset;
                visual.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
