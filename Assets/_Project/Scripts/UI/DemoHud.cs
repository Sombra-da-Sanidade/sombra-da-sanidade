using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using static SombraDaSanidade.UI.PixelHud;

namespace SombraDaSanidade.UI
{
    // HUD diegético (GDD 8). Frente 3 (Sanidade, HUD e narrativa). Issues #16 e #39.
    // Na maior parte do tempo a tela é só a cena. Cada informação aparece quando muda e some depois:
    // - bateria: pilha pequena ao lado do Thomas, ao mudar de segmento, ligar/desligar ou recarregar; fixa e piscando se baixa;
    // - HDs: três gavetas no canto, ao coletar um;
    // - relógio: segurando Tab o Thomas olha o pulso (anda mais devagar) e vê ECG, HDs e bateria. Sem números;
    // - legendas dos logs da Dra. Vance, sem moldura;
    // - dicas de controle uma vez por sessão, no momento em que o controle passa a importar.
    // A sanidade não tem barra: é comunicada por vinheta, batimento e alucinações. A barra MENTE só existe para depuração.
    // Tudo é desenhado em "pixels de jogo" (base 320x180, escala inteira) para casar com a Pixel Perfect Camera.
    public class DemoHud : MonoBehaviour
    {
        private const int BatterySegments = 4;
        private const int EcgWidth = 44;
        private const int EcgHeight = 20;

        [SerializeField] private FlashlightController flashlight;

        [Tooltip("Vazio: usa a do mesmo objeto da lanterna.")]
        [SerializeField] private SanityController sanity;

        [Tooltip("Vazio: valores padrão.")]
        [SerializeField] private HudConfig config;

        [Tooltip("Marca na pilha o nível que paga um EMP. Vazio: sem marca.")]
        [SerializeField] private FlashlightConfig flashlightConfig;

        // Dicas já mostradas: valem para a sessão inteira, mesmo depois de reiniciar a partida.
        private static readonly HashSet<string> hintsShown = new HashSet<string>();

        private HudConfig Config => config != null ? config : (config = ScriptableObject.CreateInstance<HudConfig>());

        private PlayerController player;
        private SanityHeartbeat heartbeat;
        private Camera cam;

        private float batteryVisibleUntil;
        private float empFailedUntil;
        private AdrenaSynEffect adrena;
        private int lastBatterySegment = -1;
        private float drivesVisibleUntil;
        private int newestDrive = -1;

        public static bool HasWatchCluster { get; private set; }
        public static int WatchClusterLeftPx { get; private set; }
        public static float WatchClusterScale { get; private set; } = 1f;

        private bool watchHeld;
        private bool watchVisible = true;
        private bool watchSlowApplied;
        private float watchRaise; // 0 = ancorado no canto, 1 = foco com zoom

        private enum RadarTargetKind { None, Drive, Substation, Terminal, Exit }

        // Bússola e Radar Forense (Chrono-Vigil GPS com pathfinding NavMesh)
        private Transform radarTarget;
        private RadarTargetKind radarKind;
        private Color radarColor = Fosforo5;
        private float radarDistance = float.MaxValue;
        private float radarAngle;
        private float nextRadarScanTime;
        private NavMeshPath radarPath;
        private ExitDoor cachedExitDoor;

        // Feedback sonoro de alertas da HUD
        private AudioSource audioSrc;
        private AudioClip bleepClip;
        private int lastBleepSecond = -1;

        private Texture2D ecg;
        private Color32[] ecgPixels;
        private readonly float[] ecgTrace = new float[EcgWidth];
        private float ecgHead;
        private int beatSamplesLeft;
        private bool showDebugBar;

        private string hintKey, hintLabel;
        private float hintUntil;

        private GUIStyle textStyle;

        private void Start()
        {
            if (sanity == null && flashlight != null) sanity = flashlight.GetComponent<SanityController>();
            player = flashlight != null ? flashlight.GetComponent<PlayerController>() : FindAnyObjectByType<PlayerController>();
            heartbeat = FindAnyObjectByType<SanityHeartbeat>();
            cam = Camera.main;
            showDebugBar = Config.showSanityDebugBar;

            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f;
            Audio.AudioService.Route(audioSrc, Audio.AudioBus.Ui);   // #101
            bleepClip = CreateBleepClip();

            if (flashlight != null)
            {
                flashlight.BatteryChanged += OnBatteryChanged;
                flashlight.LitChanged += OnLitChanged;
                flashlight.EmpFailed += OnEmpFailed;
                lastBatterySegment = Segment(flashlight.BatteryRatio);
            }
            if (heartbeat != null) heartbeat.Beat += OnBeat;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDriveCollected;

            ecg = new Texture2D(EcgWidth, EcgHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            ecgPixels = new Color32[EcgWidth * EcgHeight];
            for (int i = 0; i < EcgWidth; i++) ecgTrace[i] = float.NaN;
        }

        private void OnDestroy()
        {
            HasWatchCluster = false;
            SetWatchSlow(false);
            if (sanity != null) sanity.IsFocusing = false;
            if (flashlight != null)
            {
                flashlight.BatteryChanged -= OnBatteryChanged;
                flashlight.LitChanged -= OnLitChanged;
                flashlight.EmpFailed -= OnEmpFailed;
            }
            if (heartbeat != null) heartbeat.Beat -= OnBeat;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
            if (ecg != null) Destroy(ecg);
            if (bleepClip != null) Destroy(bleepClip);
        }

        // Para testes e para outras telas (ex.: tutorial) abrirem o relógio sem teclado.
        public bool WatchOpen => watchHeld;
        public void SetWatchHeld(bool held) => watchHeld = held;

        private void Update()
        {
            bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame) watchHeld = true;
                if (kb.tabKey.wasReleasedThisFrame) watchHeld = false;
                if (kb.hKey.wasPressedThisFrame && !ModalScreen.IsOpen && !UpgradeTerminal.IsOpen)
                {
                    watchVisible = !watchVisible;
                    Dismiss("h");
                }
                if (kb.fKey.wasPressedThisFrame) Dismiss("f");
                if (kb.f3Key.wasPressedThisFrame && (Application.isEditor || Debug.isDebugBuild)) showDebugBar = !showDebugBar;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) Dismiss("f");
            if (!playing) watchHeld = false;

            if (sanity != null) sanity.IsFocusing = watchHeld;
            SetWatchSlow(watchHeld);
            float speed = Time.deltaTime / Mathf.Max(0.01f, Config.watchRaiseSeconds);
            watchRaise = Mathf.MoveTowards(watchRaise, watchHeld ? 1f : 0f, speed);
            if (watchHeld) Dismiss("tab");

            UpdateHints();
            if (playing)
            {
                UpdateEcg();
                UpdateRadar();
                CheckBuffAudioWarning();
            }
        }

        private void UpdateRadar()
        {
            if (player == null) return;
            if (radarPath == null) radarPath = new NavMeshPath();

            if (Time.time >= nextRadarScanTime || radarTarget == null)
            {
                nextRadarScanTime = Time.time + 0.2f;
                ResolveRadarTarget();
                ComputeRadarPath();
            }
            else
            {
                // Mantém a direção responsiva para o próximo canto do caminho a cada frame
                ComputeRadarDirectionFromPath();
            }
        }

        private void ResolveRadarTarget()
        {
            radarTarget = null;
            radarKind = RadarTargetKind.None;
            radarColor = Fosforo5;

            var gm = GameManager.Instance;
            Vector2 pPos = player.transform.position;

            if (gm == null || !gm.HasAllDrives)
            {
                DriveServer bestDrive = null;
                float bestDist = float.MaxValue;
                var list = DriveServer.All;
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || s.IsEjected) continue;
                    float dist = Vector2.Distance(pPos, s.transform.position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestDrive = s;
                    }
                }

                if (bestDrive != null)
                {
                    if (!bestDrive.IsPowered)
                    {
                        var sub = FindUnpoweredSubstation(pPos);
                        if (sub != null)
                        {
                            radarTarget = sub.transform;
                            radarKind = RadarTargetKind.Substation;
                            radarColor = Ambar2;
                            return;
                        }
                    }

                    radarTarget = bestDrive.transform;
                    radarKind = RadarTargetKind.Drive;
                    radarColor = !bestDrive.IsPowered ? Emergencia4 : bestDrive.IsLocked ? Ambar2 : Fosforo5;
                    return;
                }
            }

            if (cachedExitDoor == null) cachedExitDoor = FindAnyObjectByType<ExitDoor>();
            if (cachedExitDoor != null)
            {
                if (cachedExitDoor.Terminal != null && !cachedExitDoor.Terminal.Activated)
                {
                    radarTarget = cachedExitDoor.Terminal.transform;
                    radarKind = RadarTargetKind.Terminal;
                    radarColor = Ambar2;
                }
                else
                {
                    radarTarget = cachedExitDoor.transform;
                    radarKind = RadarTargetKind.Exit;
                    radarColor = Eletrico4;
                }
            }
        }

        private static Substation FindUnpoweredSubstation(Vector2 from)
        {
            Substation best = null;
            float bestDist = float.MaxValue;
            foreach (var s in FindObjectsByType<Substation>())
            {
                if (s == null || s.IsPowered) continue;
                float d = Vector2.Distance(from, s.transform.position);
                if (d < bestDist) { bestDist = d; best = s; }
            }
            return best;
        }

        private void ComputeRadarPath()
        {
            if (radarTarget == null)
            {
                radarDistance = float.MaxValue;
                return;
            }

            Vector2 pPos = player.transform.position;
            Vector2 tPos = radarTarget.position;

            if (NavMesh.SamplePosition(pPos, out var fromHit, 3f, NavMesh.AllAreas)
                && NavMesh.SamplePosition(tPos, out var toHit, 3f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, radarPath)
                && radarPath.status != NavMeshPathStatus.PathInvalid
                && radarPath.corners.Length >= 2)
            {
                float pathLen = 0f;
                for (int i = 1; i < radarPath.corners.Length; i++)
                    pathLen += Vector2.Distance(radarPath.corners[i - 1], radarPath.corners[i]);
                radarDistance = pathLen;
                ComputeRadarDirectionFromPath();
            }
            else
            {
                radarPath.ClearCorners();
                Vector2 diff = tPos - pPos;
                radarDistance = diff.magnitude;
                radarAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            }
        }

        private void ComputeRadarDirectionFromPath()
        {
            if (radarTarget == null || player == null) return;
            Vector2 pPos = player.transform.position;
            Vector2 nextWaypoint = radarTarget.position;

            if (radarPath != null && radarPath.corners.Length >= 2)
            {
                var corners = radarPath.corners;
                nextWaypoint = corners[corners.Length - 1];
                for (int i = 1; i < corners.Length; i++)
                {
                    if (Vector2.Distance(pPos, corners[i]) >= 0.7f)
                    {
                        nextWaypoint = corners[i];
                        break;
                    }
                }
            }

            Vector2 diff = nextWaypoint - pPos;
            radarAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        }

        private void CheckBuffAudioWarning()
        {
            if (player == null) return;
            if (adrena == null) adrena = player.GetComponent<AdrenaSynEffect>();

            float minRemaining = float.MaxValue;
            if (adrena != null && adrena.IsActive) minRemaining = Mathf.Min(minRemaining, adrena.Remaining);
            if (sanity != null && sanity.IsRevealForced) minRemaining = Mathf.Min(minRemaining, sanity.ForcedRevealRemaining);

            if (minRemaining <= Config.effectEndingSeconds)
            {
                int sec = Mathf.CeilToInt(minRemaining);
                if (sec > 0 && sec != lastBleepSecond)
                {
                    lastBleepSecond = sec;
                    if (audioSrc != null && bleepClip != null) audioSrc.PlayOneShot(bleepClip, 0.35f);
                }
            }
            else
            {
                lastBleepSecond = -1;
            }
        }

        private static AudioClip CreateBleepClip()
        {
            const int rate = 22050;
            const float duration = 0.08f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Sin(i / (float)samples * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.35f;
            }
            var clip = AudioClip.Create("HudBleep", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------- Eventos ----------

        private void OnBatteryChanged(float battery)
        {
            int segment = Segment(flashlight.BatteryRatio);
            if (segment != lastBatterySegment) batteryVisibleUntil = Time.time + Config.batteryShowSeconds;
            lastBatterySegment = segment;
        }

        private void OnLitChanged(bool lit)
        {
            batteryVisibleUntil = Time.time + Config.batteryShowSeconds;
        }

        // Clique direito sem EMP: a pilha aparece com o raio cortado piscando.
        private void OnEmpFailed(EmpFailReason reason)
        {
            empFailedUntil = Time.time + Config.empFailShowSeconds;
            batteryVisibleUntil = Mathf.Max(batteryVisibleUntil, empFailedUntil);
        }

        private void OnDriveCollected(int count)
        {
            newestDrive = (GameManager.Instance != null ? GameManager.Instance.PriorDrives : 0) + count - 1;
            drivesVisibleUntil = Time.time + Config.driveShowSeconds;
            nextRadarScanTime = 0f;
        }

        // Cada batida desenha um complexo QRS no ECG a partir da cabeça do traço.
        private void OnBeat() => beatSamplesLeft = 6;

        private static int Segment(float ratio) => Mathf.CeilToInt(Mathf.Clamp01(ratio) * BatterySegments);

        private void SetWatchSlow(bool slow)
        {
            if (player == null || slow == watchSlowApplied) return;
            float k = Mathf.Max(0.01f, Config.watchSlowMultiplier);
            player.SpeedMultiplier = slow ? player.SpeedMultiplier * k : player.SpeedMultiplier / k;
            watchSlowApplied = slow;
        }

        // ---------- Dicas ----------

        private void UpdateHints()
        {
            if (hintKey != null && Time.time > hintUntil) hintKey = null;
            if (hintKey != null) return;

            float played = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : Time.timeSinceLevelLoad;
            if (flashlight != null && played > Config.flashlightHintDelay) Show("f", "F", "lanterna");
            if (played > 8f && hintsShown.Contains("f")) Show("h", "H", "esconder relógio");
            if (sanity != null && (sanity.State != SanityState.Lucid || (heartbeat != null && heartbeat.Tier != ProximityTier.Safe)))
                Show("tab", "TAB", "focar pulso");
            if (heartbeat != null && heartbeat.Tier == ProximityTier.Danger && flashlight != null && flashlight.IsLit
                && (flashlightConfig == null || flashlight.Battery >= flashlightConfig.empCost))
                Show("emp", "BOTÃO DIREITO", "pulso EMP");
        }

        private void Show(string id, string key, string label)
        {
            if (hintKey != null || hintsShown.Contains(id)) return;
            hintsShown.Add(id);
            hintKey = key;
            hintLabel = label;
            hintUntil = Time.time + Config.hintSeconds;
        }

        // A ação foi feita: não precisa mais da dica.
        private void Dismiss(string id)
        {
            if (!hintsShown.Contains(id)) { hintsShown.Add(id); return; }
            if (hintKey != null && hintKey.ToLowerInvariant() == id) hintKey = null;
        }

        // ---------- ECG ----------

        private void UpdateEcg()
        {
            int previous = Mathf.FloorToInt(ecgHead);
            ecgHead = Mathf.Repeat(ecgHead + Time.deltaTime * EcgWidth / Mathf.Max(0.2f, Config.ecgSweepSeconds), EcgWidth);
            int current = Mathf.FloorToInt(ecgHead);

            for (int x = previous; x != current; x = (x + 1) % EcgWidth)
            {
                int next = (x + 1) % EcgWidth;
                ecgTrace[next] = SampleEcg();
            }

            Color32 trace = EcgColor();
            Color32 back = Fosforo1;
            Color32 grid = Fosforo2;
            for (int i = 0; i < ecgPixels.Length; i++) ecgPixels[i] = back;
            for (int x = 0; x < EcgWidth; x += 6)
                for (int y = 0; y < EcgHeight; y += 2) ecgPixels[y * EcgWidth + x] = grid;

            for (int x = 0; x < EcgWidth; x++)
            {
                // Lacuna logo à frente da cabeça, como num monitor de verdade.
                int ahead = (x - current + EcgWidth) % EcgWidth;
                if (ahead > 0 && ahead <= 3) continue;
                if (float.IsNaN(ecgTrace[x])) continue;

                int y = Mathf.Clamp(Mathf.RoundToInt(EcgHeight / 2f + ecgTrace[x]), 0, EcgHeight - 1);
                int prevX = (x - 1 + EcgWidth) % EcgWidth;
                int y0 = float.IsNaN(ecgTrace[prevX]) || (prevX - current + EcgWidth) % EcgWidth <= 3
                    ? y : Mathf.Clamp(Mathf.RoundToInt(EcgHeight / 2f + ecgTrace[prevX]), 0, EcgHeight - 1);
                for (int yy = Mathf.Min(y, y0); yy <= Mathf.Max(y, y0); yy++) ecgPixels[yy * EcgWidth + x] = trace;
            }
            ecg.SetPixels32(ecgPixels);
            ecg.Apply(false);
        }

        // Valor de uma coluna do ECG: linha de base com ruído conforme a mente, e o QRS quando há batida.
        private float SampleEcg()
        {
            float noise = 0f;
            if (sanity != null)
            {
                if (sanity.State >= SanityState.Hallucinating) noise = Random.Range(-1.2f, 1.2f);
                else if (sanity.State == SanityState.Anxious) noise = Random.Range(-0.4f, 0.4f);
            }

            if (beatSamplesLeft <= 0) return noise;
            float[] qrs = { 1f, -2f, 7f, -4f, 1f, 0f };
            float amplitude = heartbeat != null ? Mathf.Lerp(0.8f, 1.2f, heartbeat.Tension) : 1f;
            float v = qrs[qrs.Length - beatSamplesLeft] * amplitude;
            beatSamplesLeft--;
            return v + noise;
        }

        private Color EcgColor()
        {
            if (sanity != null && sanity.IsFocusRecovering) return Fosforo5; // Modo Foco sob a luz: verde vibrante
            if (sanity != null && sanity.State >= SanityState.Hallucinating && Random.value < 0.15f) return Hipnos2;
            if (heartbeat == null) return Fosforo5;
            return heartbeat.Tier switch
            {
                ProximityTier.Danger => Emergencia4,
                ProximityTier.Warning => Ambar2,
                _ => (sanity != null && sanity.IsFocusing ? Fosforo4 : Fosforo5)
            };
        }

        // ---------- Desenho ----------

        private void OnGUI()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) { HasWatchCluster = false; return; }
            // Pausa, terminal de upgrade ou outra tela modal aberta: a HUD não pode ficar por cima do painel.
            if (ModalScreen.IsOpen || UpgradeTerminal.IsOpen) { HasWatchCluster = false; return; }

            Begin();
            bool showWatch = watchVisible || watchRaise > 0.01f;
            UpdateWatchClusterBounds(showWatch);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (HandleWatchToggleClick(showWatch))
                {
                    Event.current.Use();
                    return;
                }
            }

            if (Event.current.type != EventType.Repaint) return;

            textStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = false, richText = false };

            DrawBatteryNearPlayer();
            DrawDrives(8, 8, watch: false);
            DrawSubtitles();
            DrawHint();
            if (showWatch) DrawWatch();
            else DrawWatchCollapsedButton();
            if (showDebugBar) DrawDebugBar();
            GUI.color = Color.white;
        }

        private void UpdateWatchClusterBounds(bool showWatch)
        {
            HasWatchCluster = true;
            if (showWatch)
            {
                const int w = 64;
                float ease = 1f - (1f - watchRaise) * (1f - watchRaise);
                float scale = Mathf.Lerp(1f, 1.45f, ease);
                int rightEdge = Screen.width / Px - 8;
                WatchClusterLeftPx = rightEdge - Mathf.RoundToInt(w * scale);
                WatchClusterScale = scale;
            }
            else
            {
                WatchClusterLeftPx = Screen.width / Px - 46;
                WatchClusterScale = 1f;
            }
        }

        private bool HandleWatchToggleClick(bool showWatch)
        {
            if (showWatch)
            {
                int w = 64, h = HasArt ? 66 : 56;
                int x = Screen.width / Px - w - 8;
                int y = Screen.height / Px - h - 6;
                if (PixelHud.MouseOver(x, y, 12, 9))
                {
                    watchVisible = false;
                    Dismiss("h");
                    return true;
                }
            }
            else
            {
                int bx = Screen.width / Px - 46, by = Screen.height / Px - 15;
                if (PixelHud.MouseOver(bx, by, 38, 11))
                {
                    watchVisible = true;
                    Dismiss("h");
                    return true;
                }
            }
            return false;
        }

        // Relógio escondido: mostra só a seta 4 vias do caminho, distância curta e botão [H].
        private void DrawWatchCollapsedButton()
        {
            int bx = Screen.width / Px - 46, by = Screen.height / Px - 15;
            bool hover = PixelHud.MouseOver(bx, by, 38, 11);
            Box(bx, by, 38, 11, hover ? Concreto4 : Concreto3, 0.92f);
            Box(bx + 1, by + 1, 36, 9, Vazio, 0.92f);

            if (radarTarget != null && radarKind != RadarTargetKind.None)
            {
                DrawCardinalArrow(bx + 2, by + 2, radarAngle, radarColor);
                string shortDist = radarDistance < 2.5f
                    ? (radarKind == RadarTargetKind.Exit ? "SAI" : "AQ")
                    : $"{Mathf.RoundToInt(radarDistance)}M";
                Color dCol = radarKind == RadarTargetKind.Exit ? Eletrico4 : Concreto7;
                PixelHud.Text(shortDist, bx + 11, by + 3, dCol);
            }
            else
            {
                Box(bx + 4, by + 5, 3, 1, Concreto5, 1f);
            }

            Box(bx + 27, by + 2, 9, 7, hover ? Concreto5 : Concreto2, 1f);
            PixelHud.Text("H", bx + 29, by + 3, hover ? Fosforo5 : Fosforo4);
        }

        // Pilha de 4 segmentos a um palmo do Thomas, com a marca do EMP.
        private void DrawBatteryNearPlayer()
        {
            if (flashlight == null || !flashlight.enabled || cam == null) return;
            float ratio = flashlight.BatteryRatio;
            bool low = ratio <= Config.batteryLowRatio;
            float alpha = Fade(batteryVisibleUntil, 0.5f);
            if (low) alpha = Mathf.Max(alpha, 0.55f + 0.45f * Mathf.Sign(Mathf.Sin(Time.time * 6f)));
            if (alpha <= 0f) return;

            Vector3 screen = cam.WorldToScreenPoint(flashlight.transform.position + new Vector3(0.9f, 1.9f, 0f));
            int x = Mathf.RoundToInt(screen.x / Px), y = Mathf.RoundToInt((Screen.height - screen.y) / Px);
            DrawBattery(x, y, alpha);
        }

        // 14x5 pixels: 4 segmentos de 2 com 1 de folga, mais o polo.
        private void DrawBattery(int x, int y, float alpha)
        {
            float ratio = flashlight.BatteryRatio;
            bool low = ratio <= Config.batteryLowRatio;
            int filled = Segment(ratio);

            Box(x, y, 14, 5, Concreto5, alpha);
            Box(x + 14, y + 1, 1, 3, Concreto5, alpha);
            Box(x + 1, y + 1, 12, 3, Vazio, alpha);
            for (int i = 0; i < BatterySegments; i++)
            {
                Color c = i < filled ? (low ? Emergencia4 : (flashlight.IsLit ? Ambar2 : Ambar1)) : Concreto2;
                Box(x + 2 + i * 3, y + 1, 2, 3, c, alpha);
            }
            // Marca do EMP em cima da pilha: azul se a carga paga um pulso, vermelha se não.
            // Usa os valores da lanterna, que já incluem o Capacitor Duplo e a Célula Extra.
            if (flashlightConfig != null && flashlight.MaxBattery > 0f)
            {
                int mark = x + 1 + Mathf.RoundToInt(12f * Mathf.Clamp01(flashlight.EmpCost / flashlight.MaxBattery));
                bool canEmp = flashlight.Battery >= flashlight.EmpCost;
                Box(mark, y - 2, 1, 2, canEmp ? Eletrico4 : Emergencia3, alpha);
            }
            // Raio cortado piscando ao lado do polo depois de um clique direito sem EMP.
            if (Time.time < empFailedUntil && Mathf.Repeat(Time.time * 6f, 1f) < 0.6f)
                PixelHud.Sprite("hud_icons", "emp_empty", x + 16, y - 2, alpha);
        }

        // Três gavetas de HD. No canto, só depois de coletar; no relógio, sempre.
        private void DrawDrives(int x, int y, bool watch)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float alpha = watch ? 1f : Fade(drivesVisibleUntil, 0.8f);
            if (alpha <= 0f) return;

            for (int i = 0; i < gm.TotalDrivesRequired; i++)
            {
                bool has = i < gm.TotalDrives;
                bool blink = !watch && i == newestDrive && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
                int dx = x + i * 8;
                Box(dx, y, 6, 8, has ? Concreto4 : Concreto2, alpha);
                Box(dx + 1, y + 1, 4, 6, has ? Concreto3 : Vazio, alpha);
                Box(dx + 2, y + 5, 2, 1, has ? (blink ? Fosforo5 : Fosforo4) : Concreto2, alpha);
            }
        }

        // Relógio no pulso: permanentemente ancorado no canto inferior direito; aumenta de tamanho em Modo Foco (Tab).
        private void DrawWatch()
        {
            bool row = HasArt;
            int w = 64, h = row ? 66 : 56;
            float ease = 1f - (1f - watchRaise) * (1f - watchRaise);
            int x = Screen.width / Px - w - 8;
            int y = Screen.height / Px - h - 6;

            float scale = Mathf.Lerp(1f, 1.45f, ease);
            Matrix4x4 prevMatrix = GUI.matrix;
            if (scale > 1.001f)
            {
                Vector2 pivot = new Vector2((x + w) * Px, (y + h) * Px);
                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            }

            try
            {
                // Pulseira, caixa e bisel
                Color caseColor = watchHeld ? Concreto4 : Concreto3;
                Box(x + 15, y - 6, 34, 6, Concreto2, 1f);
                Box(x + 15, y + h, 34, 8, Concreto2, 1f);
                Box(x, y, w, h, caseColor, 1f);
                Box(x, y, w, 1, Concreto5, 1f);
                Box(x, y + h - 1, w, 1, Concreto1, 1f);
                Box(x + w, y + 12, 2, 6, Concreto4, 1f); // coroa
                Box(x + 4, y + 4, w - 8, h - 8, Vazio, 1f);

                // Botão visível para minimizar/esconder o relógio (clique ou tecla H)
                bool minHover = !watchHeld && PixelHud.MouseOver(x, y, 12, 9);
                Box(x + 1, y + 1, 9, 4, minHover ? Concreto5 : Concreto2, 1f);
                Box(x + 3, y + 2, 5, 1, minHover ? Concreto8 : Fosforo4, 1f);

                // Borda interna destaca suavemente em modo foco
                if (watchHeld)
                {
                    Color focusGlow = sanity != null && sanity.IsFocusRecovering ? Fosforo3 : Ambar1;
                    Box(x + 4, y + 4, w - 8, 1, focusGlow, 0.7f);
                    Box(x + 4, y + h - 5, w - 8, 1, focusGlow, 0.7f);
                }

                // Tela: ECG em cima, HDs e pilha no meio, Radar forense e efeitos embaixo
                int sx = x + 8, sy = y + 6;
                if (ecg != null)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(sx * Px, sy * Px, EcgWidth * Px, EcgHeight * Px), ecg);
                }
                DrawDrives(sx, sy + EcgHeight + 2, watch: true);
                if (flashlight != null) DrawBattery(sx + EcgWidth - 15, sy + EcgHeight + 3, 1f);

                // Linha do Radar Forense do HD (seta direcional 4 vias e sinal RF)
                DrawRadarRow(sx, sy + EcgHeight + 11);

                // LED âmbar de Zona Segura e modo econômico
                if (sanity != null && sanity.InSafeZone) Box(x + w - 9, y + 2, 2, 1, Ambar2, 1f);
                if (flashlight != null && flashlight.HasAmberFilter) Box(x + w - 13, y + 2, 2, 1, flashlight.IsEcoMode ? Ambar1 : Concreto2, 1f);

                if (row) DrawWatchRow(sx, sy + EcgHeight + 20);
            }
            finally
            {
                GUI.matrix = prevMatrix;
            }
        }

        // Radar de proximidade inteligente (NavMesh): guia até o HD/Subestação e depois até a Saída.
        private void DrawRadarRow(int x, int y)
        {
            if (radarTarget == null || radarKind == RadarTargetKind.None)
            {
                var gm = GameManager.Instance;
                bool allDone = gm != null && gm.HasAllDrives;
                string msg = allDone ? "SAÍDA" : "SEM SINAL";
                Color col = allDone ? Eletrico4 : Concreto5;
                PixelHud.Text(msg, x, y, col);
                return;
            }

            // Seta cardeal 7x7 apontando para o próximo corredor/porta do caminho
            DrawCardinalArrow(x, y, radarAngle, radarColor);

            // Barras de sinal RF (4 barras)
            float maxDist = Config.radarMaxDistance;
            float prox = 1f - Mathf.Clamp01(radarDistance / maxDist);
            int bars = Mathf.Clamp(Mathf.CeilToInt(prox * 4), 1, 4);
            int bx = x + 9;
            for (int b = 0; b < 4; b++)
            {
                int barH = 2 + b;
                Color barCol = b < bars
                    ? (radarKind == RadarTargetKind.Exit ? Eletrico4 : (bars >= 3 ? Fosforo4 : Ambar2))
                    : Concreto2;
                Box(bx + b * 3, y + 6 - barH, 2, barH, barCol, 1f);
            }

            // Distância em metros ou aviso de chegada
            int tx = bx + 13;
            if (radarDistance < 2.5f)
            {
                bool blink = Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
                string arrive = radarKind == RadarTargetKind.Exit ? "SAIR" : "AQUI";
                PixelHud.Text(arrive, tx, y, blink ? radarColor : Fosforo4);
            }
            else
            {
                string distStr = radarDistance < 10f ? $"{radarDistance:0.0}M" : $"{radarDistance:0}M";
                Color distCol = radarKind == RadarTargetKind.Exit ? Eletrico4 : Concreto7;
                PixelHud.Text(distStr, tx, y, distCol);
            }
        }

        private static void DrawCardinalArrow(int x, int y, float angleDeg, Color col)
        {
            if (angleDeg >= -45f && angleDeg < 45f)
            {
                // Direita (→)
                Box(x, y + 2, 3, 3, col);
                Box(x + 3, y, 1, 7, col);
                Box(x + 4, y + 1, 1, 5, col);
                Box(x + 5, y + 2, 1, 3, col);
                Box(x + 6, y + 3, 1, 1, col);
            }
            else if (angleDeg >= 45f && angleDeg < 135f)
            {
                // Cima (↑)
                Box(x + 3, y, 1, 1, col);
                Box(x + 2, y + 1, 3, 1, col);
                Box(x + 1, y + 2, 5, 1, col);
                Box(x, y + 3, 7, 1, col);
                Box(x + 2, y + 4, 3, 3, col);
            }
            else if (angleDeg >= -135f && angleDeg < -45f)
            {
                // Baixo (↓)
                Box(x + 2, y, 3, 3, col);
                Box(x, y + 3, 7, 1, col);
                Box(x + 1, y + 4, 5, 1, col);
                Box(x + 2, y + 5, 3, 1, col);
                Box(x + 3, y + 6, 1, 1, col);
            }
            else
            {
                // Esquerda (←)
                Box(x, y + 3, 1, 1, col);
                Box(x + 1, y + 2, 1, 3, col);
                Box(x + 2, y + 1, 1, 5, col);
                Box(x + 3, y, 1, 7, col);
                Box(x + 4, y + 2, 3, 3, col);
            }
        }

        // Linha de baixo do relógio: timers de efeitos ativos à esquerda, upgrades da Lux-90 à direita.
        private void DrawWatchRow(int x, int y)
        {
            int left = x;
            if (player != null)
            {
                if (adrena == null) adrena = player.GetComponent<AdrenaSynEffect>();
                if (adrena != null && adrena.IsActive)
                {
                    left = DrawBuffBadge(left, y, "fx_adrena", adrena.Remaining, adrena.RemainingRatio, Fosforo4);
                }
                if (sanity != null && sanity.IsRevealForced)
                {
                    left = DrawBuffBadge(left, y, "up_resonator", sanity.ForcedRevealRemaining, sanity.ForcedRevealRatio, Eletrico4);
                }
                if (Time.time < player.SlowedUntil)
                {
                    float slowRem = player.SlowedUntil - Time.time;
                    float slowDur = sanity != null && sanity.Config != null ? Mathf.Max(0.01f, sanity.Config.stabilizerSlowDuration) : 4f;
                    left = DrawBuffBadge(left, y, "fx_slow", slowRem, Mathf.Clamp01(slowRem / slowDur), Ambar1, warn: false);
                }
            }

            if (flashlight == null) return;
            var installed = flashlight.Installed;
            int right = x + EcgWidth - 8;
            for (int i = installed.Count - 1; i >= 0 && right >= left; i--)
            {
                if (installed[i] == null) continue;
                PixelHud.Sprite("hud_icons", UpgradeIcon(installed[i].effect), right, y);
                right -= 9;
            }
        }

        private int DrawBuffBadge(int x, int y, string icon, float remaining, float ratio, Color normalColor, bool warn = true)
        {
            bool ending = warn && remaining <= Config.effectEndingSeconds;
            float blink = ending && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? 0.35f : 1f;
            Color activeCol = ending ? Emergencia4 : normalColor;

            PixelHud.Sprite("hud_icons", icon, x, y, blink);
            // Micro-barra regressiva (8x2) logo abaixo do ícone para leitura periférica imediata
            Box(x, y + 9, 8, 2, Concreto2, 0.9f);
            int fillW = Mathf.Clamp(Mathf.CeilToInt(8f * Mathf.Clamp01(ratio)), 0, 8);
            if (fillW > 0) Box(x, y + 9, fillW, 2, activeCol, 1f);

            int sec = Mathf.CeilToInt(remaining);
            int textW = PixelHud.Text($"{sec:0}S", x + 9, y + 1, activeCol);
            return x + 9 + textW + 3;
        }

        // Ícone de cada upgrade em hud_icons.json (docs/upgrades-lux90.md).
        public static string UpgradeIcon(LuxUpgradeEffect effect) => effect switch
        {
            LuxUpgradeEffect.RangeMultiplier => "up_lens",
            LuxUpgradeEffect.MaxBattery => "up_cell",
            LuxUpgradeEffect.EmpCost => "up_capacitor",
            LuxUpgradeEffect.StunDuration => "up_coil",
            LuxUpgradeEffect.AmberFilter => "up_amber",
            _ => "up_resonator"
        };

        // Legenda confinada à área livre à esquerda do conjunto [Flare + Relógio] para nunca ficar coberta pelo HUD.
        private void DrawSubtitles()
        {
            var logPlayer = AudioLogPlayer.Instance;
            if (logPlayer == null || !logPlayer.IsPlaying) return;

            var flare = this.player != null ? this.player.GetComponent<FlareThrower>() : null;
            int flareReserve = (flare != null && flare.Count > 0) ? Mathf.RoundToInt(38 * WatchClusterScale) : 0;
            float rightLimitPx = HasWatchCluster ? (WatchClusterLeftPx - flareReserve - 8) * Px : Screen.width - Px * 14;
            float leftMarginPx = Px * 14f;
            float availW = Mathf.Max(Px * 110f, rightLimitPx - leftMarginPx);

            textStyle.fontSize = Px * 5;
            bool prevWrap = textStyle.wordWrap;
            textStyle.wordWrap = true;

            try
            {
                if (logPlayer.SignalLost)
                {
                    const string lost = "— sinal perdido —";
                    var lostSize = textStyle.CalcSize(new GUIContent(lost));
                    float jitter = Random.Range(-1, 2) * Px;
                    float lx = leftMarginPx + Mathf.Max(0f, (availW - lostSize.x) * 0.5f) + jitter;
                    float ly = Screen.height - Px * 20 - lostSize.y;
                    Label(new UnityEngine.Rect(lx, ly, lostSize.x, lostSize.y), lost, Emergencia4, 0.9f);
                    return;
                }

                string full = logPlayer.FullLine;
                if (full.Length == 0) return;
                float fade = Mathf.Max(0.01f, Config.subtitleFadeSeconds);
                float alpha = Mathf.Min(Mathf.Clamp01(logPlayer.LineTime / fade), Mathf.Clamp01(logPlayer.LineTimeLeft / fade));

                string speaker = logPlayer.Current.speaker + ":  ";
                string fullCombined = speaker + full;
                var speakerSize = textStyle.CalcSize(new GUIContent(speaker));
                float singleW = textStyle.CalcSize(new GUIContent(fullCombined)).x;

                if (singleW <= availW)
                {
                    float totalH = textStyle.CalcHeight(new GUIContent(fullCombined), availW);
                    float boxW = Mathf.Min(availW, singleW);
                    float boxX = leftMarginPx + (availW - boxW) * 0.5f;
                    float boxY = Screen.height - Px * 12f - totalH;

                    Fill(new UnityEngine.Rect(boxX - Px * 3f, boxY - Px * 2f, boxW + Px * 6f, totalH + Px * 4f), Vazio, 0.62f * alpha);
                    Label(new UnityEngine.Rect(boxX, boxY, speakerSize.x, speakerSize.y), speaker, Fosforo4, alpha);
                    Label(new UnityEngine.Rect(boxX + speakerSize.x, boxY, boxW - speakerSize.x + Px * 4f, totalH), logPlayer.VisibleText, Concreto8, alpha);
                }
                else
                {
                    float bodyH = textStyle.CalcHeight(new GUIContent(full), availW);
                    float headerH = speakerSize.y + Px;
                    float boxX = leftMarginPx;
                    float bodyY = Screen.height - Px * 12f - bodyH;
                    float topY = bodyY - headerH;

                    Fill(new UnityEngine.Rect(boxX - Px * 3f, topY - Px * 2f, availW + Px * 6f, headerH + bodyH + Px * 4f), Vazio, 0.62f * alpha);
                    Label(new UnityEngine.Rect(boxX, topY, speakerSize.x, speakerSize.y), logPlayer.Current.speaker + ":", Fosforo4, alpha);
                    Label(new UnityEngine.Rect(boxX, bodyY, availW, bodyH + Px * 2f), logPlayer.VisibleText, Concreto8, alpha);
                }
            }
            finally
            {
                textStyle.wordWrap = prevWrap;
            }
        }

        // Tecla em relevo acima do Thomas, com o nome da ação ao lado.
        private void DrawHint()
        {
            if (hintKey == null || cam == null || flashlight == null) return;
            float alpha = Mathf.Clamp01((hintUntil - Time.time) / 0.5f);
            textStyle.fontSize = Px * 4;

            Vector3 screen = cam.WorldToScreenPoint(flashlight.transform.position + new Vector3(0f, 2.6f, 0f));
            var keySize = textStyle.CalcSize(new GUIContent(hintKey));
            var labelSize = textStyle.CalcSize(new GUIContent(hintLabel));
            float total = keySize.x + Px * 4 + Px * 3 + labelSize.x;
            float left = screen.x - total / 2f, top = Screen.height - screen.y;

            var key = new UnityEngine.Rect(left, top, keySize.x + Px * 4, keySize.y + Px);
            Fill(key, Concreto5, alpha);
            Fill(new UnityEngine.Rect(key.x + Px, key.y + Px, key.width - 2 * Px, key.height - 3 * Px), Concreto1, alpha);
            Label(new UnityEngine.Rect(key.x + Px * 2, key.y, keySize.x, keySize.y), hintKey, Concreto8, alpha);
            Label(new UnityEngine.Rect(key.xMax + Px * 3, top, labelSize.x, labelSize.y), hintLabel, Concreto7, alpha);
        }

        // Barra MENTE antiga, só para depuração (F3 ou HudConfig.showSanityDebugBar). Issue #16.
        private void DrawDebugBar()
        {
            if (sanity == null || !sanity.enabled) return;
            int x = Screen.width / Px - 58, y = 6;
            Box(x, y, 52, 4, Concreto2, 0.9f);
            Box(x, y, Mathf.RoundToInt(52 * sanity.Ratio), 4, Color.Lerp(Emergencia4, Fosforo4, sanity.Ratio), 0.9f);
            textStyle.fontSize = Px * 3;
            string text = $"MENTE {sanity.Value:0}  {sanity.State}" + (heartbeat != null ? $"  {heartbeat.Tier} {heartbeat.Bpm:0}bpm" : "");
            Label(new UnityEngine.Rect((x - 2) * Px - 200, (y + 5) * Px, 200 + 54 * Px, Px * 5), text, Concreto7, 0.9f);
        }

        private void Label(UnityEngine.Rect rect, string text, Color color, float alpha)
        {
            var shadow = rect;
            shadow.position += new Vector2(Px * 0.5f, Px * 0.5f);
            textStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.85f);
            GUI.color = Color.white;
            GUI.Label(shadow, text, textStyle);
            textStyle.normal.textColor = new Color(color.r, color.g, color.b, alpha);
            GUI.Label(rect, text, textStyle);
        }
    }
}
