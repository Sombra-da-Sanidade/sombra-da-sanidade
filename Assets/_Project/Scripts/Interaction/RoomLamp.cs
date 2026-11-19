using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Interaction
{
    // Arandela de parede de uma sala. O Breaker da sala liga todas juntas: acendem piscando como fluorescente
    // velha, falham nos últimos segundos e, quando o disjuntor queima, estouram e ficam com o vidro trincado.
    // Só visual: a proteção (Zona Segura) continua sendo a do disjuntor.
    // Na purga da eclusa (EscapeSequence.Started, depois de entregar os HDs) todas viram alarme: luz vermelha
    // pulsando no ritmo da sirene, mesmo as apagadas ou queimadas (circuito de emergência). As sirenes de parede
    // usam este mesmo componente, só com alarme.
    public class RoomLamp : MonoBehaviour
    {
        public enum LampState { Off, On, Burnt }

        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Light2D glow;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite burntSprite;
        [SerializeField] private float intensity = 0.9f;
        [Tooltip("Piscada de partida, como lâmpada fluorescente.")]
        [SerializeField] private float startupFlicker = 0.7f;

        [Header("Alarme da purga")]
        [Tooltip("Quadros em alarme (lente vermelha acesa/apagada, ou a sirene girando).")]
        [SerializeField] private Sprite[] alarmFrames;
        [SerializeField] private Color alarmColor = new Color(1f, 0.15f, 0.12f);
        [SerializeField] private float alarmIntensity = 1.1f;
        [SerializeField] private float alarmFps = 8f;

        public LampState State { get; private set; } = LampState.Off;
        public bool IsAlarm { get; private set; }

        private float onAt;
        private bool warning;
        private float seed;

        private Color normalColor;
        private Level.EscapeSequence escape;

        private void Awake()
        {
            seed = Random.value * 100f;
            if (glow != null) normalColor = glow.color;
            Apply(LampState.Off);
        }

        private void Start()
        {
            escape = Level.EscapeSequence.Instance;
            if (escape == null) return;
            escape.Started += OnPurge;
            if (escape.Active) OnPurge();
        }

        private void OnDestroy()
        {
            if (escape != null) escape.Started -= OnPurge;
        }

        private void OnPurge() => SetAlarm(true);

        public void SetAlarm(bool on)
        {
            IsAlarm = on;
            if (glow == null) return;
            glow.color = on ? alarmColor : normalColor;
            glow.enabled = on || State == LampState.On;
        }

        // delay: as lâmpadas da sala acendem em sequência, não todas no mesmo quadro.
        public void TurnOn(float delay = 0f)
        {
            if (State == LampState.Burnt) return;
            warning = false;
            onAt = Time.time + delay;
            Apply(LampState.On);
        }

        public void TurnOff() => Apply(LampState.Off);

        public void SetWarning(bool on) => warning = on;

        public void Burn() => Apply(LampState.Burnt);

        private void Apply(LampState state)
        {
            State = state;
            if (art != null)
            {
                var sprite = state switch { LampState.On => onSprite, LampState.Burnt => burntSprite, _ => offSprite };
                if (sprite != null) art.sprite = sprite;
            }
            if (glow != null)
            {
                glow.enabled = state == LampState.On;
                glow.intensity = 0f;
            }
        }

        private void Update()
        {
            if (IsAlarm)
            {
                UpdateAlarm();
                return;
            }
            if (State != LampState.On) return;
            float t = Time.time - onAt;
            float k;
            if (t < 0f) k = 0f;
            else if (t < startupFlicker) k = Random.value < 0.55f ? 1f : 0.1f;
            else if (warning) k = Mathf.PerlinNoise(Time.time * 9f, seed) < 0.38f ? 0.15f : 1f;
            else k = 1f;
            if (glow != null) glow.intensity = intensity * k;
            if (art != null && onSprite != null && offSprite != null) art.sprite = k > 0.5f ? onSprite : offSprite;
        }

        // Pulso vermelho no ritmo da sirene da purga (acelera com o tempo, como o EscapeSequence).
        private void UpdateAlarm()
        {
            float period = 1.2f;
            if (escape != null && escape.Config != null)
                period = Mathf.Lerp(escape.Config.sirenPeriodStart, escape.Config.sirenPeriodEnd, escape.Progress);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period));
            if (glow != null)
            {
                glow.enabled = true;
                glow.intensity = alarmIntensity * (0.25f + 0.75f * wave);
            }
            if (art == null || alarmFrames == null || alarmFrames.Length == 0) return;
            art.sprite = alarmFrames.Length == 2
                ? alarmFrames[wave > 0.5f ? 0 : 1]
                : alarmFrames[(int)((Time.time + seed) * alarmFps) % alarmFrames.Length];
        }
    }
}
