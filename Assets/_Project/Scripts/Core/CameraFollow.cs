using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Câmera top-down com lerp suave e tremor (GDD 9). Portado do projeto Escura.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private Vector3 smoothedPos;
        private Vector3 velocity;
        private float shakeTimer;
        private float shakeMagnitude;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            smoothedPos = transform.position;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            smoothedPos = Vector3.SmoothDamp(smoothedPos, target.position + offset, ref velocity, smoothTime);

            Vector3 shakeOffset = Vector3.zero;
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            }

            transform.position = smoothedPos + shakeOffset;
        }

        // Usar em EMP, aproximação do monstro e batimentos acelerados.
        public void Shake(float duration, float magnitude)
        {
            shakeTimer = duration;
            shakeMagnitude = magnitude;
        }
    }
}
