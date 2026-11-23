using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Toca as falas do Thomas de uma ala (roteiro-rc 2, #89) nos gatilhos de cada InnerVoiceLine. Cada fala toca
    // uma vez; durante uma cutscene ela espera o fim. Montado pelo builder das alas com as falas da ala.
    public class InnerVoiceDirector : MonoBehaviour
    {
        [SerializeField] private InnerVoiceLine[] lines = new InnerVoiceLine[0];
        [Tooltip("Segundos contínuos no escuro para a fala do primeiro escuro.")]
        [SerializeField, Min(0f)] private float darknessSeconds = 1.5f;
        [Tooltip("Distância (unidades) para a câmera contar como vista sem estar no cone dela.")]
        [SerializeField, Min(0f)] private float cameraSeenDistance = 5f;
        [Tooltip("Distância (unidades) da pista da senha revelada para contar como lida.")]
        [SerializeField, Min(0f)] private float clueReadDistance = 4f;

        private readonly HashSet<string> played = new HashSet<string>();
        private readonly Queue<InnerVoiceLine> pending = new Queue<InnerVoiceLine>();
        private SanityController sanity;
        private Transform player;
        private float darkTime;
        private bool started;

        public IReadOnlyList<InnerVoiceLine> Lines => lines;
        public bool HasPlayed(string id) => played.Contains(id);
        public static InnerVoiceDirector Instance { get; private set; }

        public void Setup(InnerVoiceLine[] wingLines) => lines = wingLines;

        private void Awake() => Instance = this;

        private void Start()
        {
            sanity = FindAnyObjectByType<SanityController>();
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDrive;
            foreach (var b in FindObjectsByType<Breaker>(FindObjectsSortMode.None))
                b.StateChanged += on => { if (on) Fire(InnerVoiceTrigger.FirstBreaker); };
            foreach (var s in FindObjectsByType<Substation>(FindObjectsSortMode.None))
                s.Powered += () => Fire(InnerVoiceTrigger.SubstationPowered);
            foreach (var c in SecurityCamera.All)
                c.AlarmRaised += _ =>
                {
                    var profile = DifficultyProfile.Active;
                    if (profile != null && profile.camerasCallSpecimen) Fire(InnerVoiceTrigger.CameraCall);
                };
            CutscenePlayer.Finished += OnCutscene;
            NeuralModule.Applied += OnNeural;
            VanceChoice.Decided += OnChoice;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDrive;
            CutscenePlayer.Finished -= OnCutscene;
            NeuralModule.Applied -= OnNeural;
            VanceChoice.Decided -= OnChoice;
        }

        // Um quadro depois: o log da Vance do HD começa primeiro e a fala do Thomas entra na fila atrás dele.
        private void OnDrive(int total) => Fire(InnerVoiceTrigger.DriveCollected, "", defer: true);
        private void OnNeural(Vector2 at) => Fire(InnerVoiceTrigger.NeuralModule);
        private void OnCutscene(Cutscene c) { if (c != null) Fire(InnerVoiceTrigger.AfterCutscene, c.id); }
        private void OnChoice(VanceDecision d) =>
            Fire(d == VanceDecision.Save ? InnerVoiceTrigger.ChoiceSave : InnerVoiceTrigger.ChoiceLeave);

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (!CutscenePlayer.IsPlaying)
            {
                // Começo da ala: depois da cutscene de abertura (C0, I1, I2).
                if (!started) { started = true; Fire(InnerVoiceTrigger.WingStart); }
                while (pending.Count > 0) Play(pending.Dequeue());
            }
            if (player == null) return;

            darkTime = sanity != null && sanity.InDarkness ? darkTime + Time.deltaTime : 0f;
            if (darkTime >= darknessSeconds) Fire(InnerVoiceTrigger.FirstDarkness);

            foreach (var line in lines)
            {
                if (line == null || line.trigger != InnerVoiceTrigger.Area || played.Contains(line.id)) continue;
                var marker = LevelMarker.Find(line.arg);
                if (marker == null) continue;
                float reach = Mathf.Max(1.5f, marker.Size.magnitude * 0.5f);
                if (Vector2.Distance(marker.transform.position, player.position) <= reach) Fire(line);
            }

            foreach (var cam in SecurityCamera.All)
                if (cam != null && (cam.SeesPlayer || Vector2.Distance(cam.transform.position, player.position) <= cameraSeenDistance))
                {
                    Fire(InnerVoiceTrigger.CameraSeen);
                    break;
                }

            foreach (var clue in FindObjectsByType<CodeClue>(FindObjectsSortMode.None))
            {
                var sc = clue.GetComponent<SanityClue>();
                if (sc != null && sc.IsRevealed && Vector2.Distance(clue.transform.position, player.position) <= clueReadDistance)
                {
                    Fire(InnerVoiceTrigger.ClueRead);
                    break;
                }
            }
        }

        public void Fire(InnerVoiceTrigger trigger, string arg = "", bool defer = false)
        {
            foreach (var line in lines)
                if (line != null && line.trigger == trigger && (arg.Length == 0 || line.arg == arg)) Fire(line, defer);
        }

        private void Fire(InnerVoiceLine line, bool defer = false)
        {
            if (!played.Add(line.id)) return;
            if (defer || CutscenePlayer.IsPlaying) pending.Enqueue(line);
            else Play(line);
        }

        private void Play(InnerVoiceLine line)
        {
            var log = line.ToLog(sanity != null ? sanity.Value : 100f);
            if (AudioLogPlayer.Instance != null) AudioLogPlayer.Instance.Play(log);
        }
    }
}
