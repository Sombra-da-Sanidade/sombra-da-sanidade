using System;
using System.Collections;
using SombraDaSanidade.Core;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Cela da Dra. Vance na Eclusa (planejamento-rc 3, #82). Só abre depois que o Thomas escolhe salvá-la no
    // terminal (#95). Segurar E por 6 s com 2 skill checks de multímetro (InteractionConfig próprio da cela);
    // cada segundo de abertura faz barulho, e as câmeras da sala continuam ligadas. Solta, a Vance sai pelo duto
    // sem escolta, desliga as câmeras do caminho até a eclusa e abre o atalho (ShortcutGate).
    [RequireComponent(typeof(Collider2D))]
    public class VanceCell : MonoBehaviour, IInteractable, ISkillCheckable
    {
        [SerializeField] private EscapeConfig config;
        [Tooltip("Skill check da cela: 2 checks garantidos (skillCheckMin/MaxPerDrive = 2).")]
        [SerializeField] private InteractionConfig skillCheckConfig;
        [SerializeField] private SpriteRenderer cellArt;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private SpriteRenderer vance;
        [Tooltip("Porta externa: as câmeras perto da linha cela-porta são as 'do caminho'.")]
        [SerializeField] private Transform exit;
        [Tooltip("Distância da linha cela-porta até onde uma câmera conta como 'do caminho'.")]
        [SerializeField, Min(0f)] private float pathCameraRadius = 12f;

        public static event Action<VanceCell> Opened;

        public bool IsOpen { get; private set; }
        public int SkillChecksDone { get; private set; }
        public int SkillCheckFailures { get; private set; }

        private float nextNoise;

        public void Setup(EscapeConfig escape, InteractionConfig skill, SpriteRenderer art, Sprite closed, Sprite open,
            SpriteRenderer vanceArt, Transform exitDoor)
        {
            config = escape;
            skillCheckConfig = skill;
            cellArt = art;
            closedSprite = closed;
            openSprite = open;
            vance = vanceArt;
            exit = exitDoor;
        }

        public string Prompt => IsOpen ? "" : VanceChoice.Decision == VanceDecision.Save ? "Abrir a cela"
            : VanceChoice.Decision == VanceDecision.Leave ? "A escolha foi feita" : "Cela de observação";

        public bool CanInteract => !IsOpen && VanceChoice.Decision == VanceDecision.Save
            && (GameManager.Instance == null || GameManager.Instance.IsPlaying);

        public float HoldDuration => config != null ? config.cellHoldSeconds : 6f;

        public InteractionConfig SkillCheckConfig => skillCheckConfig;
        public bool CanSkillCheck => !IsOpen && skillCheckConfig != null && SkillChecksDone < skillCheckConfig.skillCheckMaxPerDrive;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Start()
        {
            if (cellArt != null && closedSprite != null) cellArt.sprite = closedSprite;
        }

        public void BeginInteract()
        {
            // Cada tentativa recomeça os checks (soltar E zera o progresso, como no HD).
            SkillChecksDone = 0;
            nextNoise = 0f;
        }

        public void TickInteract(float progress)
        {
            if (config == null || Time.time < nextNoise) return;
            nextNoise = Time.time + 1f;
            NoiseBus.Emit(transform.position, config.cellNoiseRadius);
        }

        public void OnSkillCheckStarted() => SkillChecksDone++;

        public void OnSkillCheckResolved(SkillCheckResult result)
        {
            if (result == SkillCheckResult.Fail) SkillCheckFailures++;
        }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (cellArt != null && openSprite != null) cellArt.sprite = openSprite;
            VanceChoice.Release();
            ShortcutGate.OpenAll();
            ShutdownPathCameras();
            Opened?.Invoke(this);
            if (vance != null) StartCoroutine(VanceLeaves());
        }

        // Câmeras perto da linha entre a cela e a porta externa (o caminho de volta pelo atalho).
        public int ShutdownPathCameras()
        {
            Vector2 a = transform.position;
            Vector2 b = exit != null ? (Vector2)exit.position : a;
            int count = 0;
            foreach (var cam in SecurityCamera.All.ToArray())
            {
                if (cam == null || DistanceToSegment(cam.transform.position, a, b) > pathCameraRadius) continue;
                cam.Shutdown();
                count++;
            }
            return count;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        // Sai pelo duto do teto, sem escolta: sobe e some.
        private IEnumerator VanceLeaves()
        {
            float duration = config != null ? config.vanceExitSeconds : 1.5f;
            Vector3 start = vance.transform.position;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / Mathf.Max(0.01f, duration);
                vance.transform.position = start + Vector3.up * k * 1.2f;
                vance.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            vance.enabled = false;
        }
    }
}
