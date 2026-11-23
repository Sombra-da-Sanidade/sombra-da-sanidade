using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Tocador das falas internas do Thomas (#89). Respeita a hierarquia:
    // Cutscene > AudioLog (Vance) > InnerVoice (Thomas). Se houver tela modal, upgrade, cutscene ou log
    // da Vance ativo, a fala entra na fila e toca assim que o caminho ficar livre.
    public class InnerVoicePlayer : MonoBehaviour
    {
        public static InnerVoicePlayer Instance { get; private set; }

        private static readonly HashSet<string> played = new HashSet<string>();

        private readonly Queue<InnerVoiceLine> pending = new Queue<InnerVoiceLine>();
        private SanityController sanity;
        private AudioLog activeRuntimeLog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            played.Clear();
        }

        public static bool HasPlayed(string id) => !string.IsNullOrEmpty(id) && played.Contains(id);
        public static void ClearHistory() => played.Clear();

        public static void Play(InnerVoiceLine line)
        {
            if (line == null || CutscenePlayer.IsPlaying) return;
            if (!string.IsNullOrEmpty(line.id) && played.Contains(line.id)) return;
            Ensure().Enqueue(line);
        }

        private static InnerVoicePlayer Ensure()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<InnerVoicePlayer>();
            if (existing != null) return existing;
            return new GameObject("InnerVoicePlayer").AddComponent<InnerVoicePlayer>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() => CutscenePlayer.Started += OnCutsceneStarted;
        private void OnDisable() => CutscenePlayer.Started -= OnCutsceneStarted;

        private void OnCutsceneStarted(Cutscene _) => pending.Clear();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (activeRuntimeLog != null) Destroy(activeRuntimeLog);
        }

        private void Enqueue(InnerVoiceLine line)
        {
            if (!string.IsNullOrEmpty(line.id)) played.Add(line.id);
            pending.Enqueue(line);
        }

        private void Update()
        {
            if (pending.Count == 0) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (CutscenePlayer.IsPlaying || ModalScreen.IsOpen || UpgradeTerminal.IsOpen) return;

            var logPlayer = AudioLogPlayer.Instance;
            if (logPlayer == null || logPlayer.IsPlaying) return;

            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            bool shaken = sanity != null && sanity.State >= SanityState.Hallucinating;

            var next = pending.Dequeue();
            var lines = next != null ? next.PickLines(shaken) : null;
            if (next == null || lines == null || lines.Length == 0) return;

            if (activeRuntimeLog != null) Destroy(activeRuntimeLog);
            activeRuntimeLog = next.ToRuntimeLog(shaken);
            logPlayer.Play(activeRuntimeLog);
        }
    }
}
