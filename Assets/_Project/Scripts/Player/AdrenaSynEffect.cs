using UnityEngine;

namespace SombraDaSanidade.Player
{
    // Efeito ativo do Módulo Adrena-Syn (GDD 6): +35% de velocidade por 15 s.
    // Fica no Player e é criado sob demanda. Multiplica e divide o SpeedMultiplier, para não apagar
    // a lentidão do Estabilizador de Lítio. Tomar de novo durante o efeito só renova a duração. Frente 1. Issue #34.
    [RequireComponent(typeof(PlayerController))]
    public class AdrenaSynEffect : MonoBehaviour
    {
        public bool IsActive => appliedFactor > 0f;
        public float Remaining => IsActive ? Mathf.Max(0f, endTime - Time.time) : 0f;

        private PlayerController player;
        private float appliedFactor;
        private float endTime;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        public static AdrenaSynEffect On(PlayerController player)
        {
            if (player == null) return null;
            var effect = player.GetComponent<AdrenaSynEffect>();
            return effect != null ? effect : player.gameObject.AddComponent<AdrenaSynEffect>();
        }

        public void Activate(float speedBonus, float duration)
        {
            float factor = 1f + Mathf.Max(0f, speedBonus);
            if (!IsActive)
            {
                appliedFactor = factor;
                player.SpeedMultiplier *= appliedFactor;
            }
            endTime = Time.time + duration;
        }

        private void Update()
        {
            if (IsActive && Time.time >= endTime) End();
        }

        private void OnDisable()
        {
            if (IsActive) End();
        }

        private void End()
        {
            if (player != null) player.SpeedMultiplier /= appliedFactor;
            appliedFactor = 0f;
        }
    }
}
