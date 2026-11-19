using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Player
{
    // Sinalizador aceso no chão. Voa em arco até o alvo, cai, queima por alguns segundos com luz vermelha e apaga.
    // Enquanto queima, segura o Sonâmbulo parado dentro do raio da luz (SleepwalkerController consulta Illuminates):
    // é o tempo de reação para o Thomas fugir ou lidar com o Espécime. Criado pelo FlareThrower.
    public class SignalFlare : MonoBehaviour
    {
        private static readonly List<SignalFlare> burning = new List<SignalFlare>();

        // Algum sinalizador aceso ilumina este ponto?
        public static bool Illuminates(Vector2 point)
        {
            for (int i = burning.Count - 1; i >= 0; i--)
            {
                var f = burning[i];
                if (f == null) { burning.RemoveAt(i); continue; }
                if (((Vector2)f.transform.position - point).sqrMagnitude <= f.radius * f.radius) return true;
            }
            return false;
        }

        public static int BurningCount => burning.Count;

        public bool IsFlying { get; private set; }
        public bool IsBurning { get; private set; }
        public bool IsSpent { get; private set; }
        public float Radius => radius;
        public float Remaining => IsBurning ? Mathf.Max(0f, burnEnd - Time.time) : 0f;

        private SpriteRenderer art;
        private Light2D glow;
        private Sprite[] litFrames;
        private Sprite deadSprite;
        private float duration;
        private float radius;
        private Vector2 from, to;
        private float flightTime, flightStart, burnEnd;
        private float baseIntensity = 1.3f;

        public static SignalFlare Spawn(Vector2 from, Vector2 to, float flightTime, float duration, float radius,
            Sprite[] litFrames, Sprite deadSprite, Material unlit = null)
        {
            var go = new GameObject("SignalFlare");
            go.transform.position = from;
            var f = go.AddComponent<SignalFlare>();
            f.from = from; f.to = to;
            f.flightTime = Mathf.Max(0.05f, flightTime);
            f.duration = duration; f.radius = radius;
            f.litFrames = litFrames; f.deadSprite = deadSprite;

            var artGo = new GameObject("Art");
            artGo.transform.SetParent(go.transform, false);
            f.art = artGo.AddComponent<SpriteRenderer>();
            f.art.sortingOrder = 8;
            if (litFrames != null && litFrames.Length > 0) f.art.sprite = litFrames[0];
            if (unlit != null) f.art.sharedMaterial = unlit;   // a chama brilha mesmo no escuro

            f.glow = go.AddComponent<Light2D>();
            f.glow.lightType = Light2D.LightType.Point;
            f.glow.color = new Color(1f, 0.32f, 0.25f);
            f.glow.pointLightInnerRadius = radius * 0.2f;
            f.glow.pointLightOuterRadius = radius + 0.6f;
            f.glow.intensity = 0.6f;
            f.glow.shadowsEnabled = true;
            f.glow.shadowIntensity = 0.85f;

            f.IsFlying = true;
            f.flightStart = Time.time;
            return f;
        }

        private void Update()
        {
            if (IsFlying)
            {
                float t = Mathf.Clamp01((Time.time - flightStart) / flightTime);
                transform.position = Vector2.Lerp(from, to, t);
                // Arco: o desenho sobe e desce acima da sombra, girando.
                art.transform.localPosition = new Vector3(0f, 4f * 0.7f * t * (1f - t), 0f);
                art.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 540f);
                Animate();
                if (t >= 1f) Land();
                return;
            }
            if (!IsBurning) return;
            Animate();
            float left = burnEnd - Time.time;
            // Chama tremida; no último segundo e meio ela vai morrendo.
            float fade = Mathf.Clamp01(left / 1.5f);
            glow.intensity = baseIntensity * (0.8f + 0.25f * Mathf.PerlinNoise(Time.time * 12f, 0.5f)) * Mathf.Max(0.25f, fade);
            if (left <= 0f) BurnOut();
        }

        private void Animate()
        {
            if (litFrames == null || litFrames.Length == 0) return;
            art.sprite = litFrames[(int)(Time.time * 12f) % litFrames.Length];
        }

        private void Land()
        {
            IsFlying = false;
            IsBurning = true;
            transform.position = to;
            art.transform.localPosition = Vector3.zero;
            art.transform.localRotation = Quaternion.identity;
            art.sortingOrder = -6;   // no chão, debaixo dos personagens
            burnEnd = Time.time + duration;
            burning.Add(this);
        }

        private void BurnOut()
        {
            IsBurning = false;
            IsSpent = true;
            burning.Remove(this);
            glow.enabled = false;
            if (deadSprite != null) art.sprite = deadSprite;
            Destroy(gameObject, 20f);
        }

        private void OnDestroy() => burning.Remove(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.25f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
