using System;
using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Flashlight
{
    // Lanterna Mod. Lux-90: cone de luz que revela o cenário e drena bateria (GDD 4.2 e 6.3).
    // Baseado na lógica de óleo/cone do PlayerController do projeto Escura.
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] private FlashlightConfig config;
        [SerializeField] private PlayerController player;

        [Tooltip("Pivô que gira com a mira. Precisa ter um Light2D do tipo Point.")]
        [SerializeField] private Transform pivot;

        [Tooltip("Camadas que bloqueiam o feixe (paredes).")]
        [SerializeField] private LayerMask obstacles;

        public float Battery { get; private set; }
        public float BatteryRatio => config.maxBattery > 0f ? Battery / config.maxBattery : 0f;
        public bool IsSwitchedOn { get; private set; } = true;
        public bool IsLit => IsSwitchedOn && Battery > 0f;

        public event Action<float> BatteryChanged;
        public event Action<bool> LitChanged;

        private Light2D light2D;
        private bool wasLit;

        private void Awake()
        {
            if (pivot != null) light2D = pivot.GetComponent<Light2D>();
            Battery = config.maxBattery;
            ApplyConeShape();
        }

        private void Update()
        {
            ReadToggle();
            Aim();
            Drain();
            UpdateLight();
        }

        // Usado pelo monstro (atração) e pela sanidade (escuridão).
        public bool IsPointInCone(Vector2 point)
        {
            if (!IsLit || pivot == null) return false;

            Vector2 origin = pivot.position;
            Vector2 toPoint = point - origin;
            if (toPoint.magnitude > config.range) return false;
            if (Vector2.Angle(player.Facing, toPoint) > config.coneAngle * 0.5f) return false;

            return !Physics2D.Linecast(origin, point, obstacles);
        }

        // Para o pulso EMP e outros gastos pontuais. Retorna false se não houver carga.
        public bool TrySpend(float amount)
        {
            if (Battery < amount) return false;
            SetBattery(Battery - amount);
            return true;
        }

        public void Recharge(float amount)
        {
            SetBattery(Battery + amount);
        }

        private void ReadToggle()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed = (kb != null && kb.fKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (pressed) IsSwitchedOn = !IsSwitchedOn;
        }

        private void Aim()
        {
            if (pivot == null || player == null) return;
            float angle = Mathf.Atan2(player.Facing.y, player.Facing.x) * Mathf.Rad2Deg;
            pivot.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        private void Drain()
        {
            if (IsSwitchedOn && Battery > 0f)
                SetBattery(Battery - config.drainPerSecond * Time.deltaTime);
        }

        private void UpdateLight()
        {
            bool lit = IsLit;
            if (lit != wasLit)
            {
                wasLit = lit;
                LitChanged?.Invoke(lit);
            }

            if (light2D == null) return;
            light2D.enabled = lit;
            if (lit)
                light2D.intensity = Mathf.Lerp(config.intensity * config.lowBatteryIntensityRatio, config.intensity, BatteryRatio);
        }

        private void SetBattery(float value)
        {
            Battery = Mathf.Clamp(value, 0f, config.maxBattery);
            BatteryChanged?.Invoke(Battery);
        }

        private void ApplyConeShape()
        {
            if (light2D == null) return;
            light2D.pointLightOuterRadius = config.range;
            light2D.pointLightOuterAngle = config.coneAngle;
            light2D.pointLightInnerAngle = config.coneAngle * 0.6f;
        }
    }
}
