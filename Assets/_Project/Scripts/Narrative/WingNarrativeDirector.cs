using System.Collections;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Level;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.Narrative
{
    // Coordena os gatilhos de evento do roteiro "Voluntário 08" por ala (#89 e #91):
    // - Dispara C0 na entrada da Fase 1, I1/I2 nas transições e C3 após VANCE-03/Upgrade do HD-03.
    // - Dispara C1 assim que o WingIntro acorda o Espécime (evitando focar monstro invisível).
    // - Liga o cue 'c2_lampada' na Ala de Energia (pisca luz e avança o Sonâmbulo 2 passos).
    // - Dispara C4/C5 ao vencer a Eclusa e as falas do Thomas por evento de gameplay.
    public class WingNarrativeDirector : MonoBehaviour
    {
        public static bool VanceSaved { get; set; }
        public static bool SuppressAutoTriggers { get; set; }
        public static bool IsAutoSuppressed => SuppressAutoTriggers || Checkpoint.PathOverride != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDirectorStatics()
        {
            VanceSaved = false;
            SuppressAutoTriggers = false;
        }

        private SanityController sanity;
        private WingIntro wingIntro;
        private CodeClue[] codeClues;
        private Breaker[] breakers;
        private Substation[] substations;
        private SecurityCamera[] cameras;

        private string wing;
        private float darknessTimer;
        private bool darknessTriggered;
        private bool neuralTriggered;
        private bool codeClueVoiceTriggered;
        private bool c1Triggered;
        private bool pendingC3;
        private bool abandonVoiceTriggered;
        private ExitDoor cachedExitDoor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(WingFlow.WingForScene(scene.name))) return;
            if (FindAnyObjectByType<WingNarrativeDirector>() != null) return;
            new GameObject("WingNarrativeDirector").AddComponent<WingNarrativeDirector>();
        }

        private void OnEnable()
        {
            CutscenePlayer.Finished += OnCutsceneFinished;
            VanceChoice.Decided += OnVanceDecided;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DriveCollected += OnDriveCollected;
                GameManager.Instance.VictoryTriggered += OnVictoryTriggered;
            }
        }

        private void OnDisable()
        {
            CutscenePlayer.Finished -= OnCutsceneFinished;
            VanceChoice.Decided -= OnVanceDecided;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DriveCollected -= OnDriveCollected;
                GameManager.Instance.VictoryTriggered -= OnVictoryTriggered;
            }
            UnsubscribeSceneEvents();
        }

        private void Start()
        {
            wing = WingFlow.WingForScene(SceneManager.GetActiveScene().name);
            if (string.IsNullOrEmpty(wing) && DifficultyProfile.Active != null) wing = DifficultyProfile.Active.wing;

            EnsureC2LampCue();
            EnsureRuntimeReactiveProps();
            EnsureVanceRescueChoice();
            if (IsAutoSuppressed) return;

            sanity = FindAnyObjectByType<SanityController>();
            wingIntro = FindAnyObjectByType<WingIntro>();
            codeClues = FindObjectsByType<CodeClue>();
            cachedExitDoor = FindAnyObjectByType<ExitDoor>();
            SubscribeSceneEvents();
            StartCoroutine(BeginWingRoutine());
        }

        private IEnumerator BeginWingRoutine()
        {
            // Aguarda 1 frame para GameManager/WingIntro inicializarem.
            yield return null;
            if (IsAutoSuppressed) yield break;

            if (wing == "Medica")
            {
                var c0 = LoadCutscene("C0_Abertura");
                if (c0 != null && !CutscenePlayer.HasPlayed(c0.id))
                    CutscenePlayer.Play(c0, () => PlayVoice("T1-1"));
                else
                    PlayVoice("T1-1");
            }
            else if (wing == "Energia")
            {
                var i1 = LoadCutscene("I1_AlaEnergia");
                if (i1 != null && !CutscenePlayer.HasPlayed(i1.id))
                    CutscenePlayer.Play(i1, () => PlayVoice("T2-1"));
                else
                {
                    yield return new WaitForSeconds(1.2f);
                    PlayVoice("T2-1");
                }
            }
            else if (wing == "Executiva")
            {
                var i2 = LoadCutscene("I2_AlaExecutiva");
                if (i2 != null && !CutscenePlayer.HasPlayed(i2.id))
                    CutscenePlayer.Play(i2, () => PlayVoice("T3-1"));
                else
                {
                    yield return new WaitForSeconds(1.2f);
                    PlayVoice("T3-1");
                }
            }
        }

        private void Update()
        {
            if (IsAutoSuppressed) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (CutscenePlayer.IsPlaying) return;

            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();

            // C1 dispara assim que o Espécime acorda pelo WingIntro para não focar monstro invisível.
            if (wing == "Medica" && !c1Triggered && wingIntro != null && wingIntro.Awakened)
            {
                c1Triggered = true;
                StartCoroutine(PlayC1Delayed());
            }

            if (wing == "Eclusa" && !abandonVoiceTriggered && !VanceSaved
                && EscapeSequence.Instance != null && EscapeSequence.Instance.Active && cachedExitDoor != null)
            {
                var p = FindAnyObjectByType<PlayerController>();
                if (p != null && Vector2.Distance(p.transform.position, cachedExitDoor.transform.position) <= 5.5f)
                {
                    abandonVoiceTriggered = true;
                    PlayVoice("T4-2");
                }
            }

            if (sanity == null) return;

            if (!darknessTriggered && wing == "Medica")
            {
                if (sanity.InDarkness)
                {
                    darknessTimer += Time.deltaTime;
                    if (darknessTimer >= 1.5f)
                    {
                        darknessTriggered = true;
                        PlayVoice("T1-2");
                    }
                }
                else
                {
                    darknessTimer = 0f;
                }
            }

            if (wing == "Executiva")
            {
                if (!neuralTriggered && sanity.IsRevealForced)
                {
                    neuralTriggered = true;
                    PlayVoice("T3-5");
                }

                if (!codeClueVoiceTriggered && sanity.Value < sanity.RevealCluesBelow && IsNearAnyCodeClue())
                {
                    codeClueVoiceTriggered = true;
                    PlayVoice("T3-4");
                }
            }
        }

        private IEnumerator PlayC1Delayed()
        {
            yield return new WaitForSeconds(0.5f);
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) yield break;
            var c1 = LoadCutscene("C1_Especime");
            if (c1 != null && !CutscenePlayer.HasPlayed(c1.id)) CutscenePlayer.Play(c1);
        }

        private bool IsNearAnyCodeClue()
        {
            if (codeClues == null || codeClues.Length == 0) return false;
            var p = FindAnyObjectByType<PlayerController>();
            if (p == null) return false;
            foreach (var clue in codeClues)
                if (clue != null && Vector2.Distance(p.transform.position, clue.transform.position) <= 4.5f)
                    return true;
            return false;
        }

        private void EnsureC2LampCue()
        {
            if (wing != "Energia") return;
            var marker = CutsceneMarker.Find("c2_lampada");
            if (marker == null)
            {
                var sw = FindAnyObjectByType<SleepwalkerController>();
                var lamp = FindClosestCeilingLamp(sw != null ? sw.transform.position : Vector3.zero);
                var go = new GameObject("Cue_C2_Lampada");
                go.transform.position = lamp != null ? lamp.transform.position : (sw != null ? sw.transform.position : Vector3.zero);
                marker = go.AddComponent<CutsceneMarker>();
                marker.Setup("c2_lampada");
            }
            marker.OnCue.RemoveListener(OnC2LampCue);
            marker.OnCue.AddListener(OnC2LampCue);
        }

        private void OnC2LampCue() => StartCoroutine(C2LampFlickerRoutine());

        private IEnumerator C2LampFlickerRoutine()
        {
            var sw = FindAnyObjectByType<SleepwalkerController>();
            var player = FindAnyObjectByType<PlayerController>();
            var lamp = FindClosestCeilingLamp(sw != null ? sw.transform.position : Vector3.zero);

            bool wasEnabled = lamp != null && lamp.enabled;
            if (lamp != null) lamp.enabled = false;

            if (sw != null && player != null)
            {
                Vector3 dir = (player.transform.position - sw.transform.position).normalized;
                sw.transform.position += dir * 2.0f;
            }

            yield return new WaitForSeconds(0.45f);
            if (lamp != null) lamp.enabled = wasEnabled;
        }

        private static Light2D FindClosestCeilingLamp(Vector3 near)
        {
            Light2D best = null;
            float bestDist = float.MaxValue;
            foreach (var sz in FindObjectsByType<SafeZone>())
            {
                var l = sz.GetComponentInChildren<Light2D>();
                if (l == null) continue;
                float d = (l.transform.position - near).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = l; }
            }
            return best;
        }

        private void SubscribeSceneEvents()
        {
            breakers = FindObjectsByType<Breaker>();
            foreach (var b in breakers) b.StateChanged += OnBreakerChanged;

            substations = FindObjectsByType<Substation>();
            foreach (var s in substations) s.Powered += OnSubstationPowered;

            cameras = FindObjectsByType<SecurityCamera>();
            foreach (var c in cameras)
            {
                c.StateChanged += OnCameraStateChanged;
                c.AlarmRaised += OnCameraAlarm;
            }
        }

        private void UnsubscribeSceneEvents()
        {
            if (breakers != null) foreach (var b in breakers) if (b != null) b.StateChanged -= OnBreakerChanged;
            if (substations != null) foreach (var s in substations) if (s != null) s.Powered -= OnSubstationPowered;
            if (cameras != null)
            {
                foreach (var c in cameras)
                {
                    if (c == null) continue;
                    c.StateChanged -= OnCameraStateChanged;
                    c.AlarmRaised -= OnCameraAlarm;
                }
            }
        }

        private void OnBreakerChanged(bool lit)
        {
            if (lit && wing == "Medica") PlayVoice("T1-4");
        }

        private void OnSubstationPowered()
        {
            if (wing == "Energia") PlayVoice("T2-5");
        }

        private void OnCameraStateChanged(SecurityCameraState state)
        {
            if (wing == "Energia" && state == SecurityCameraState.Detecting) PlayVoice("T2-3");
        }

        private void OnCameraAlarm(Vector2 pos)
        {
            if (wing == "Energia") PlayVoice("T2-3");
            else if (wing == "Executiva") PlayVoice("T3-2");
        }

        // Escolha no terminal da Eclusa (#95): fala do Thomas na hora da decisão.
        private void OnVanceDecided(VanceDecision d)
        {
            if (IsAutoSuppressed) return;
            if (d == VanceDecision.Save) PlayVoice("T4-1");
            else
            {
                abandonVoiceTriggered = true;
                PlayVoice("T4-2");
            }
        }

        private void OnCutsceneFinished(Cutscene scene)
        {
            if (IsAutoSuppressed || scene == null) return;
            if (scene.id == "C1") PlayVoice("T1-5");
            else if (scene.id == "C2") PlayVoice("T2-2");
        }

        private void OnDriveCollected(int count)
        {
            if (IsAutoSuppressed) return;
            int total = GameManager.Instance != null ? GameManager.Instance.TotalDrives : count;
            if (wing == "Medica" && total >= 1) PlayVoice("T1-6");
            else if (wing == "Energia") PlayVoice("T2-6");
            else if (wing == "Executiva" && !pendingC3)
            {
                pendingC3 = true;
                StartCoroutine(PlayC3AfterVanceRoutine());
            }
        }

        // Garante que UpgradeTerminal e VANCE-03 terminem antes da cutscene C3 Revelação (#91).
        private IEnumerator PlayC3AfterVanceRoutine()
        {
            yield return null;
            while (UpgradeTerminal.IsOpen || ModalScreen.IsOpen) yield return null;
            while (AudioLogPlayer.Instance != null && AudioLogPlayer.Instance.IsPlaying) yield return null;
            yield return new WaitForSeconds(0.5f);

            var c3 = LoadCutscene("C3_Revelacao");
            if (c3 != null && !CutscenePlayer.HasPlayed(c3.id))
                CutscenePlayer.Play(c3);
            else
                PlayVoice("T3-6");
        }

        private void OnVictoryTriggered()
        {
            if (IsAutoSuppressed || wing != "Eclusa") return;
            var ending = LoadCutscene(VanceSaved ? "C4_Testemunha" : "C5_AmostraEntregue");
            if (ending != null && !CutscenePlayer.HasPlayed(ending.id))
                CutscenePlayer.Play(ending);
        }

        public static void PlayVoice(string id)
        {
            var line = Resources.Load<InnerVoiceLine>($"Narrative/Voices/{id}");
            if (line != null) InnerVoicePlayer.Play(line);
        }

        public static Cutscene LoadCutscene(string assetName) =>
            Resources.Load<Cutscene>($"Narrative/Cutscenes/{assetName}");

        // Garante os 4 objetos reativos à sanidade em runtime caso a cena não tenha sido regerada após a adição.
        private void EnsureRuntimeReactiveProps()
        {
            if (FindAnyObjectByType<SanitySpriteSwap>() != null) return;
            var safeZones = FindObjectsByType<SafeZone>();
            Vector2 safe = safeZones != null && safeZones.Length > 0 ? (Vector2)safeZones[0].transform.position : new Vector2(-4f, 2f);

            if (wing == "Medica")
            {
                SpawnReactiveProp("Prop_Pulseira_08", safe + new Vector2(-1.2f, -0.8f), "pulseira_normal", "pulseira_08");
                SpawnReactiveProp("Prop_Prontuario_Maca", safe + new Vector2(1.2f, 0.5f), "prontuario_normal", "prontuario_08");
            }
            else if (wing == "Energia")
            {
                SpawnReactiveProp("Prop_Foto_Vance", safe + new Vector2(1.1f, 0.7f), "foto_normal", "foto_08");
            }
            else if (wing == "Executiva")
            {
                Vector2 safeExec = safeZones != null && safeZones.Length > 1 ? (Vector2)safeZones[1].transform.position : safe;
                SpawnReactiveProp("Prop_Espelho_08", safeExec + new Vector2(0f, 0.9f), "espelho_normal", "espelho_08", "M3", "espelho_rachado");
            }
        }

        private void SpawnReactiveProp(string name, Vector2 pos, string normalName, string shakenName,
            string afterCutsceneId = "", string postName = "")
        {
            var normal = LoadPropSprite(normalName);
            var shaken = LoadPropSprite(shakenName);
            if (normal == null || shaken == null) return;
            var post = !string.IsNullOrEmpty(postName) ? LoadPropSprite(postName) : null;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = normal;
            sr.sortingOrder = 5;
            go.AddComponent<SanitySpriteSwap>().Setup(normal, shaken, 35f, afterCutsceneId, post);
        }

        private static Sprite LoadPropSprite(string fileName)
        {
            var sp = Resources.Load<Sprite>($"Narrative/Props/{fileName}");
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>($"Narrative/Props/{fileName}");
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 16f) : null;
        }

        // Na Saída de Emergência (Eclusa), cria o intercomunicador/trava da Sala de Observação da Dra. Vance
        // para permitir a escolha entre C4 (Testemunha, T4-1) e C5 (Amostra entregue, T4-2).
        private void EnsureVanceRescueChoice()
        {
            // A Eclusa gerada já tem a cela da Vance com a escolha no terminal (#82, #95).
            if (wing != "Eclusa" || FindAnyObjectByType<VanceRescueInteractable>() != null
                || FindAnyObjectByType<VanceCell>() != null) return;

            var terminal = FindAnyObjectByType<EclusaTerminal>();
            var exit = cachedExitDoor != null ? cachedExitDoor : FindAnyObjectByType<ExitDoor>();
            Vector2 pos = new Vector2(2f, 2f);
            if (terminal != null && exit != null)
                pos = Vector2.Lerp(terminal.transform.position, exit.transform.position, 0.45f) + new Vector2(2.2f, 0f);
            else if (terminal != null)
                pos = (Vector2)terminal.transform.position + new Vector2(3.5f, 1.5f);

            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                pos = hit.position;

            var go = new GameObject("VanceRescue_Observacao");
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadPropSprite("prontuario_08");
            sr.sortingOrder = 6;
            go.AddComponent<VanceRescueInteractable>();
        }
    }

    // Ponto de decisão na Saída de Emergência (#91): destrancar a sala de observação resgata a Dra. Vance (final C4 + fala T4-1).
    [RequireComponent(typeof(Collider2D))]
    public class VanceRescueInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt => "Destrancar Observação (Resgatar Dra. Vance)";
        public bool CanInteract => !WingNarrativeDirector.VanceSaved
            && (GameManager.Instance == null || GameManager.Instance.IsPlaying);
        public float HoldDuration => 1.6f;

        public void BeginInteract() { }
        public void TickInteract(float progress) { }
        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (WingNarrativeDirector.VanceSaved) return;
            WingNarrativeDirector.VanceSaved = true;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.45f, 0.95f, 0.55f, 1f);
            WingNarrativeDirector.PlayVoice("T4-1");
        }
    }
}
