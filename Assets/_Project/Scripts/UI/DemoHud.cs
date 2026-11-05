using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Narrative;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEngine;
using UnityEngine.InputSystem;

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
        // Paleta do jogo (art-src/palette.json).
        private static readonly Color Vazio = Hex(0x0b0c10);
        private static readonly Color Concreto1 = Hex(0x161920);
        private static readonly Color Concreto2 = Hex(0x222630);
        private static readonly Color Concreto3 = Hex(0x313744);
        private static readonly Color Concreto4 = Hex(0x444b59);
        private static readonly Color Concreto5 = Hex(0x5d6573);
        private static readonly Color Concreto7 = Hex(0xa1a8b2);
        private static readonly Color Concreto8 = Hex(0xd3d6da);
        private static readonly Color Fosforo1 = Hex(0x0e2a22);
        private static readonly Color Fosforo2 = Hex(0x1a4d3a);
        private static readonly Color Fosforo4 = Hex(0x55b870);
        private static readonly Color Fosforo5 = Hex(0xa9f294);
        private static readonly Color Eletrico4 = Hex(0x5bb2ff);
        private static readonly Color Emergencia3 = Hex(0xa1222c);
        private static readonly Color Emergencia4 = Hex(0xdf4139);
        private static readonly Color Ambar1 = Hex(0xc8922c);
        private static readonly Color Ambar2 = Hex(0xf6d878);
        private static readonly Color Hipnos2 = Hex(0x7e4a8a);

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
        private int lastBatterySegment = -1;
        private float drivesVisibleUntil;
        private int newestDrive = -1;

        private bool watchHeld;
        private bool watchSlowApplied;
        private float watchRaise; // 0 = abaixado, 1 = no rosto

        private Texture2D ecg;
        private Color32[] ecgPixels;
        private readonly float[] ecgTrace = new float[EcgWidth];
        private float ecgHead;
        private int beatSamplesLeft;
        private bool showDebugBar;

        private string hintKey, hintLabel;
        private float hintUntil;

        private GUIStyle textStyle;
        private int px; // tamanho de um pixel de jogo na tela

        private void Start()
        {
            if (sanity == null && flashlight != null) sanity = flashlight.GetComponent<SanityController>();
            player = flashlight != null ? flashlight.GetComponent<PlayerController>() : FindAnyObjectByType<PlayerController>();
            heartbeat = FindAnyObjectByType<SanityHeartbeat>();
            cam = Camera.main;
            showDebugBar = Config.showSanityDebugBar;

            if (flashlight != null)
            {
                flashlight.BatteryChanged += OnBatteryChanged;
                flashlight.LitChanged += OnLitChanged;
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
            SetWatchSlow(false);
            if (flashlight != null)
            {
                flashlight.BatteryChanged -= OnBatteryChanged;
                flashlight.LitChanged -= OnLitChanged;
            }
            if (heartbeat != null) heartbeat.Beat -= OnBeat;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
            if (ecg != null) Destroy(ecg);
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
                if (kb.fKey.wasPressedThisFrame) Dismiss("f");
                if (kb.f3Key.wasPressedThisFrame && (Application.isEditor || Debug.isDebugBuild)) showDebugBar = !showDebugBar;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) Dismiss("f");
            if (!playing) watchHeld = false;

            SetWatchSlow(watchHeld);
            float speed = Time.deltaTime / Mathf.Max(0.01f, Config.watchRaiseSeconds);
            watchRaise = Mathf.MoveTowards(watchRaise, watchHeld ? 1f : 0f, speed);
            if (watchHeld) Dismiss("tab");

            UpdateHints();
            if (watchRaise > 0f) UpdateEcg();
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

        private void OnDriveCollected(int count)
        {
            newestDrive = count - 1;
            drivesVisibleUntil = Time.time + Config.driveShowSeconds;
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
            if (sanity != null && (sanity.State != SanityState.Lucid || (heartbeat != null && heartbeat.Tier != ProximityTier.Safe)))
                Show("tab", "TAB", "relógio");
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
            if (sanity != null && sanity.State >= SanityState.Hallucinating && Random.value < 0.15f) return Hipnos2;
            if (heartbeat == null) return Fosforo5;
            return heartbeat.Tier switch
            {
                ProximityTier.Danger => Emergencia4,
                ProximityTier.Warning => Ambar2,
                _ => Fosforo5
            };
        }

        // ---------- Desenho ----------

        private void OnGUI()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (Event.current.type != EventType.Repaint) return;

            px = Mathf.Max(2, Mathf.FloorToInt(Screen.height / 180f));
            textStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = false, richText = false };

            DrawBatteryNearPlayer();
            DrawDrives(8, 8, watch: false);
            DrawSubtitles();
            DrawHint();
            if (watchRaise > 0f) DrawWatch();
            if (showDebugBar) DrawDebugBar();
            GUI.color = Color.white;
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
            int x = Mathf.RoundToInt(screen.x / px), y = Mathf.RoundToInt((Screen.height - screen.y) / px);
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
            if (flashlightConfig != null && flashlightConfig.maxBattery > 0f)
            {
                int mark = x + 1 + Mathf.RoundToInt(12f * flashlightConfig.empCost / flashlightConfig.maxBattery);
                bool canEmp = flashlight.Battery >= flashlightConfig.empCost;
                Box(mark, y - 2, 1, 2, canEmp ? Eletrico4 : Emergencia3, alpha);
            }
        }

        // Três gavetas de HD. No canto, só depois de coletar; no relógio, sempre.
        private void DrawDrives(int x, int y, bool watch)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float alpha = watch ? 1f : Fade(drivesVisibleUntil, 0.8f);
            if (alpha <= 0f) return;

            for (int i = 0; i < gm.DrivesRequired; i++)
            {
                bool has = i < gm.DrivesCollected;
                bool blink = !watch && i == newestDrive && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
                int dx = x + i * 8;
                Box(dx, y, 6, 8, has ? Concreto4 : Concreto2, alpha);
                Box(dx + 1, y + 1, 4, 6, has ? Concreto3 : Vazio, alpha);
                Box(dx + 2, y + 5, 2, 1, has ? (blink ? Fosforo5 : Fosforo4) : Concreto2, alpha);
            }
        }

        // Relógio no pulso: sobe do canto inferior direito. ECG, HDs e bateria, sem números.
        private void DrawWatch()
        {
            int w = 62, h = 46;
            float ease = 1f - (1f - watchRaise) * (1f - watchRaise);
            int x = Screen.width / px - w - 8;
            int y = Mathf.RoundToInt(Mathf.Lerp(Screen.height / (float)px + 4, Screen.height / (float)px - h - 8, ease));

            // Pulseira, caixa e bisel
            Box(x + 14, y - 6, 34, 6, Concreto2, 1f);
            Box(x + 14, y + h, 34, 8, Concreto2, 1f);
            Box(x, y, w, h, Concreto3, 1f);
            Box(x, y, w, 1, Concreto5, 1f);
            Box(x, y + h - 1, w, 1, Concreto1, 1f);
            Box(x + w, y + 12, 2, 6, Concreto4, 1f); // coroa
            Box(x + 4, y + 4, w - 8, h - 8, Vazio, 1f);

            // Tela: ECG em cima, HDs e pilha embaixo
            int sx = x + 7, sy = y + 6;
            if (ecg != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(sx * px, sy * px, EcgWidth * px, EcgHeight * px), ecg);
            }
            DrawDrives(sx, sy + EcgHeight + 3, watch: true);
            if (flashlight != null) DrawBattery(sx + EcgWidth - 15, sy + EcgHeight + 4, 1f);

            // LED âmbar quando está sob a luz de uma Zona Segura
            if (sanity != null && sanity.InSafeZone) Box(x + w - 9, y + 2, 2, 1, Ambar2, 1f);
        }

        // Legenda sem moldura, centrada embaixo: nome em fósforo e fala em branco, com sombra para ler sobre a cena.
        // A fala é digitada letra a letra (AudioLogPlayer); a posição usa a linha inteira para o texto não andar.
        private void DrawSubtitles()
        {
            var player = AudioLogPlayer.Instance;
            if (player == null || !player.IsPlaying) return;

            textStyle.fontSize = px * 5;
            float bottom = Screen.height - px * 26;

            if (player.SignalLost)
            {
                const string lost = "— sinal perdido —";
                var lostSize = textStyle.CalcSize(new GUIContent(lost));
                float jitter = Random.Range(-1, 2) * px;
                Label(new UnityEngine.Rect((Screen.width - lostSize.x) / 2f + jitter, bottom, lostSize.x, lostSize.y), lost, Emergencia4, 0.9f);
                return;
            }

            string full = player.FullLine;
            if (full.Length == 0) return;
            float fade = Mathf.Max(0.01f, Config.subtitleFadeSeconds);
            float alpha = Mathf.Min(Mathf.Clamp01(player.LineTime / fade), Mathf.Clamp01(player.LineTimeLeft / fade));

            string speaker = player.Current.speaker + ":  ";
            var speakerSize = textStyle.CalcSize(new GUIContent(speaker));
            var fullSize = textStyle.CalcSize(new GUIContent(full));
            float left = (Screen.width - speakerSize.x - fullSize.x) / 2f;
            Label(new UnityEngine.Rect(left, bottom, speakerSize.x, speakerSize.y), speaker, Fosforo4, alpha);
            Label(new UnityEngine.Rect(left + speakerSize.x, bottom, fullSize.x, fullSize.y), player.VisibleText, Concreto8, alpha);
        }

        // Tecla em relevo acima do Thomas, com o nome da ação ao lado.
        private void DrawHint()
        {
            if (hintKey == null || cam == null || flashlight == null) return;
            float alpha = Mathf.Clamp01((hintUntil - Time.time) / 0.5f);
            textStyle.fontSize = px * 4;

            Vector3 screen = cam.WorldToScreenPoint(flashlight.transform.position + new Vector3(0f, 2.6f, 0f));
            var keySize = textStyle.CalcSize(new GUIContent(hintKey));
            var labelSize = textStyle.CalcSize(new GUIContent(hintLabel));
            float total = keySize.x + px * 4 + px * 3 + labelSize.x;
            float left = screen.x - total / 2f, top = Screen.height - screen.y;

            var key = new UnityEngine.Rect(left, top, keySize.x + px * 4, keySize.y + px);
            Fill(key, Concreto5, alpha);
            Fill(new UnityEngine.Rect(key.x + px, key.y + px, key.width - 2 * px, key.height - 3 * px), Concreto1, alpha);
            Label(new UnityEngine.Rect(key.x + px * 2, key.y, keySize.x, keySize.y), hintKey, Concreto8, alpha);
            Label(new UnityEngine.Rect(key.xMax + px * 3, top, labelSize.x, labelSize.y), hintLabel, Concreto7, alpha);
        }

        // Barra MENTE antiga, só para depuração (F3 ou HudConfig.showSanityDebugBar). Issue #16.
        private void DrawDebugBar()
        {
            if (sanity == null || !sanity.enabled) return;
            int x = Screen.width / px - 58, y = 6;
            Box(x, y, 52, 4, Concreto2, 0.9f);
            Box(x, y, Mathf.RoundToInt(52 * sanity.Ratio), 4, Color.Lerp(Emergencia4, Fosforo4, sanity.Ratio), 0.9f);
            textStyle.fontSize = px * 3;
            string text = $"MENTE {sanity.Value:0}  {sanity.State}" + (heartbeat != null ? $"  {heartbeat.Tier} {heartbeat.Bpm:0}bpm" : "");
            Label(new UnityEngine.Rect((x - 2) * px - 200, (y + 5) * px, 200 + 54 * px, px * 5), text, Concreto7, 0.9f);
        }

        // ---------- Primitivas ----------

        // Retângulo em pixels de jogo.
        private void Box(int x, int y, int w, int h, Color color, float alpha) =>
            Fill(new UnityEngine.Rect(x * px, y * px, w * px, h * px), color, alpha);

        private static void Fill(UnityEngine.Rect rect, Color color, float alpha)
        {
            GUI.color = new Color(color.r, color.g, color.b, alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private void Label(UnityEngine.Rect rect, string text, Color color, float alpha)
        {
            var shadow = rect;
            shadow.position += new Vector2(px * 0.5f, px * 0.5f);
            textStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.85f);
            GUI.color = Color.white;
            GUI.Label(shadow, text, textStyle);
            textStyle.normal.textColor = new Color(color.r, color.g, color.b, alpha);
            GUI.Label(rect, text, textStyle);
        }

        // 1 até "until", depois cai até 0 em "fadeSeconds". 0 se nunca foi mostrado.
        private static float Fade(float until, float fadeSeconds)
        {
            if (until <= 0f) return 0f;
            if (Time.time < until) return 1f;
            return Mathf.Clamp01(1f - (Time.time - until) / fadeSeconds);
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);
    }
}
