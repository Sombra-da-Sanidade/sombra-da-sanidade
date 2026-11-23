using System;
using System.Collections.Generic;
using SombraDaSanidade.Audio;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.UI
{
    // Toca uma Cutscene (#88, planejamento-rc 2 e 4). Criado sob demanda: CutscenePlayer.Play(cutscene).
    // - Trava o controle (PlayerController.InputLocked + ModalScreen, que esconde a HUD e a lanterna ignora).
    // - freezeWorld: Time.timeScale = 0 (o player roda em tempo real). Planos no motor sem congelar deixam o mundo
    //   andar, para a C1 e a C2.
    // - Painel: imagem 320x180 com zoom e pan. Motor: a câmera do jogo vai até o alvo, com faixas pretas.
    // - Legenda digitada; Espaço ou clique completa e avança. Segurar Esc pula a cutscene inteira.
    // - Transições: corte, fade ou glitch. Memória liga o MemoryEffect. Cue dispara um CutsceneMarker.
    // - AudioDirector: abaixa a música da fase e toca a faixa da cutscene.
    public class CutscenePlayer : MonoBehaviour
    {
        public static CutscenePlayer Instance { get; private set; }
        public static bool IsPlaying => Instance != null && Instance.current != null;

        public static event Action<Cutscene> Started;
        public static event Action<Cutscene> Finished;

        // Ids tocados nesta sessão (para não repetir uma memória depois de morrer, por exemplo).
        private static readonly HashSet<string> played = new HashSet<string>();
        public static bool HasPlayed(string id) => played.Contains(id);

        public const float SkipHoldSeconds = 0.8f;
        private const float FadeSeconds = 0.45f;
        private const float ExitFadeOutSeconds = 0.40f;
        private const float ExitFadeInSeconds = 0.70f;
        private const float GlitchSeconds = 0.35f;
        private const float CharsPerSecond = 75f;
        private const float LetterboxSpeed = 3f;

        public Cutscene Current => current;
        public int ShotIndex => index;
        public CutsceneShot Shot => current != null && index < current.shots.Length ? current.shots[index] : null;
        public float SkipProgress => skipHeld / SkipHoldSeconds;

        private readonly Queue<(Cutscene scene, Action done)> queue = new Queue<(Cutscene, Action)>();
        private Cutscene current;
        private Action onDone;
        private int index;
        private float shotTime, typed, skipHeld, letterbox, exitFadeAlpha, previousTimeScale = 1f, baseSize;
        private bool froze;
        private Camera cam;
        private CameraFollow follow;
        private Vector3 cameraStart;
        private static Texture2D white;

        public static void Play(Cutscene cutscene, Action onDone = null)
        {
            if (cutscene == null || cutscene.shots == null || cutscene.shots.Length == 0) { onDone?.Invoke(); return; }
            Ensure().Enqueue(cutscene, onDone);
        }

        // Pula a cutscene atual (o mesmo que segurar Esc).
        public static void Skip()
        {
            if (IsPlaying) Instance.End();
        }

        private static CutscenePlayer Ensure()
        {
            if (Instance != null) return Instance;
            return new GameObject("CutscenePlayer").AddComponent<CutscenePlayer>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            played.Clear();
            Started = null;
            Finished = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (current != null) Restore();
            if (Instance == this) Instance = null;
        }

        private void Enqueue(Cutscene cutscene, Action done)
        {
            queue.Enqueue((cutscene, done));
            if (current == null) Next();
        }

        private void Next()
        {
            if (queue.Count == 0) return;
            var (scene, done) = queue.Dequeue();
            current = scene;
            onDone = done;
            played.Add(scene.id);

            PlayerController.InputLocked = true;
            ModalScreen.Push();
            AudioDirector.CutsceneActive = true;
            AudioDirector.CutsceneTrack = scene.music;
            froze = scene.freezeWorld;
            if (froze)
            {
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }

            cam = Camera.main;
            follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow == null) follow = FindAnyObjectByType<CameraFollow>();
            if (cam != null)
            {
                baseSize = cam.orthographicSize;
                cameraStart = cam.transform.position;
            }
            // O player move a câmera em tempo real; o CameraFollow volta no fim.
            if (follow != null) follow.enabled = false;

            Started?.Invoke(scene);
            StartShot(0);
        }

        private void StartShot(int i)
        {
            index = i;
            shotTime = 0f;
            typed = 0f;
            var shot = Shot;
            if (shot == null) return;
            MemoryEffect.Active = shot.memory;
            // Glitch de fita ao entrar na memória ou trocar de plano com glitch (#107).
            if (shot.memory && shot.transition == CutsceneTransition.Glitch) PlayGlitch();
            if (shot.sound != null) AudioService.Play(shot.sound);
            CutsceneMarker.Fire(shot.cue);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (current == null)
            {
                if (exitFadeAlpha > 0f) exitFadeAlpha = Mathf.MoveTowards(exitFadeAlpha, 0f, dt / ExitFadeInSeconds);
                return;
            }
            var shot = Shot;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.escapeKey.isPressed)
            {
                skipHeld += dt;
                if (skipHeld >= SkipHoldSeconds) { End(); return; }
            }
            else skipHeld = 0f;

            shotTime += dt;
            typed += CharsPerSecond * dt;
            bool advance = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                           || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (advance)
            {
                int length = shot != null && shot.caption != null ? shot.caption.Length : 0;
                if (typed < length) typed = length;   // primeiro toque completa a legenda
                else shotTime = float.MaxValue;
            }

            letterbox = Mathf.MoveTowards(letterbox, shot != null && shot.kind == CutsceneShotKind.World ? 1f : 0f,
                LetterboxSpeed * dt);
            if (shot != null && shot.kind == CutsceneShotKind.World) DriveCamera(shot, dt);

            if (shot == null || shotTime >= shot.duration)
            {
                if (index + 1 < current.shots.Length) StartShot(index + 1);
                else End();
            }
        }

        private float Progress(CutsceneShot shot)
        {
            float k = Mathf.Clamp01(shotTime / Mathf.Max(0.01f, shot.duration));
            if (shot.memory) k = Mathf.Floor(k * 6f) / 6f;   // quadros repetidos, como fita gasta
            return k;
        }

        private void DriveCamera(CutsceneShot shot, float dt)
        {
            if (cam == null) return;
            var target = ResolveFocus(shot.focus);
            if (target == null) return;
            float k = Mathf.SmoothStep(0f, 1f, Progress(shot));
            Vector2 at = (Vector2)target.position + Vector2.Lerp(shot.panFrom, shot.panTo, k);
            var pos = cam.transform.position;
            Vector3 goal = new Vector3(at.x, at.y, pos.z);
            // Corte: salta no primeiro quadro. Fade/glitch: escondem o salto do mesmo jeito.
            cam.transform.position = shotTime <= dt ? goal : Vector3.Lerp(pos, goal, 1f - Mathf.Exp(-5f * dt));
            cam.orthographicSize = baseSize * Mathf.Lerp(shot.zoom.x, shot.zoom.y, k);
        }

        private static Transform ResolveFocus(string focus)
        {
            switch ((focus ?? "").Trim().ToLowerInvariant())
            {
                case "":
                case "player": return FindAnyObjectByType<PlayerController>()?.transform;
                case "especime": return FindAnyObjectByType<MonsterController>()?.transform;
                case "sonambulo": return FindAnyObjectByType<SleepwalkerController>()?.transform;
                default: return CutsceneMarker.Find(focus)?.transform;
            }
        }

        private void End()
        {
            if (current == null) return;
            var scene = current;
            var done = onDone;
            if (Shot != null && Shot.memory) PlayGlitch();   // glitch de saída da memória (#107)
            Restore();
            Finished?.Invoke(scene);
            done?.Invoke();
            Next();
        }

        private static void PlayGlitch()
        {
            var sfx = GameSfx.Load();
            if (sfx != null) AudioService.Play(sfx.memoryGlitch);
        }

        private void Restore()
        {
            current = null;
            onDone = null;
            skipHeld = 0f;
            letterbox = 0f;
            exitFadeAlpha = queue.Count == 0 ? 1f : 0f; // Fade suave ao voltar para a gameplay
            MemoryEffect.Active = false;
            PlayerController.InputLocked = false;
            ModalScreen.Pop();
            AudioDirector.CutsceneActive = false;
            AudioDirector.CutsceneTrack = null;
            // Se o jogo acabou durante a cutscene, o GameManager manda no timeScale.
            if (froze && (GameManager.Instance == null || GameManager.Instance.IsPlaying)) Time.timeScale = previousTimeScale;
            froze = false;
            if (cam != null)
            {
                cam.orthographicSize = baseSize;
                cam.transform.position = cameraStart;
            }
            if (follow != null) follow.enabled = true;
        }

        // ---------- Desenho ----------

        private void OnGUI()
        {
            if (white == null) white = Texture2D.whiteTexture;
            if (current == null)
            {
                if (exitFadeAlpha > 0f)
                {
                    GUI.depth = -50;
                    Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black, exitFadeAlpha);
                }
                return;
            }
            var shot = Shot;
            if (shot == null) return;
            GUI.depth = -50;
            PixelHud.Begin();
            int W = Screen.width, H = Screen.height;

            if (shot.kind == CutsceneShotKind.Panel)
            {
                Fill(new Rect(0, 0, W, H), Color.black, 1f);
                if (shot.image != null) DrawPanel(shot, PanelRect());
            }
            else if (letterbox > 0f)
            {
                float bar = H * 0.12f * letterbox;
                Fill(new Rect(0, 0, W, bar), Color.black, 1f);
                Fill(new Rect(0, H - bar, W, bar), Color.black, 1f);
            }

            DrawCaption(shot);
            DrawTransition(shot);
            DrawSkipHint();
        }

        // Área 16:9 centrada (o painel tem 320x180).
        private static Rect PanelRect()
        {
            float scale = Mathf.Min(Screen.width / 320f, Screen.height / 180f);
            float w = 320f * scale, h = 180f * scale;
            return new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        }

        private void DrawPanel(CutsceneShot shot, Rect rect)
        {
            var sprite = shot.image;
            var tex = sprite.texture;
            Rect r = sprite.textureRect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);

            float k = Mathf.SmoothStep(0f, 1f, Progress(shot));
            float zoom = Mathf.Max(1f, Mathf.Lerp(shot.zoom.x, shot.zoom.y, k));
            Vector2 pan = Vector2.Lerp(shot.panFrom, shot.panTo, k);
            float w = uv.width / zoom, h = uv.height / zoom;
            float cx = uv.x + uv.width * (0.5f + Mathf.Clamp(pan.x, -0.5f, 0.5f) * (1f - 1f / zoom));
            float cy = uv.y + uv.height * (0.5f + Mathf.Clamp(pan.y, -0.5f, 0.5f) * (1f - 1f / zoom));
            var crop = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);

            // Fita gasta: a imagem pula um pixel de vez em quando.
            if (shot.memory && Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.06f) rect.y += PixelHud.Px;
            GUI.DrawTextureWithTexCoords(rect, tex, crop, true);
        }

        private void DrawCaption(CutsceneShot shot)
        {
            if (string.IsNullOrEmpty(shot.caption)) return;
            string text = shot.caption.Substring(0, Mathf.Clamp(Mathf.FloorToInt(typed), 0, shot.caption.Length));
            bool intertitle = shot.kind == CutsceneShotKind.Panel && shot.image == null;
            int maxW = PixelHud.Width - 40;
            var lines = Wrap(string.IsNullOrEmpty(shot.speaker) ? text : shot.speaker + ": " + text, maxW);
            int lineH = 10;
            int y = intertitle ? PixelHud.Height / 2 - lines.Count * lineH / 2 : PixelHud.Height - 18 - lines.Count * lineH;

            if (!PixelHud.HasArt)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = PixelHud.Px * 8 };
                GUI.Label(new Rect(0, y * PixelHud.Px, Screen.width, lines.Count * lineH * PixelHud.Px), string.Join("\n", lines), style);
                return;
            }
            if (!intertitle)
                PixelHud.Box(10, y - 4, PixelHud.Width - 20, lines.Count * lineH + 6, PixelHud.Vazio, 0.78f);
            for (int i = 0; i < lines.Count; i++)
            {
                int x = (PixelHud.Width - PixelHud.TextWidth(lines[i])) / 2;
                var color = intertitle ? PixelHud.Fosforo4 : shot.memory ? PixelHud.Fosforo5 : PixelHud.Concreto8;
                if (i == 0 && !string.IsNullOrEmpty(shot.speaker) && lines[i].StartsWith(shot.speaker + ":"))
                {
                    int sw = PixelHud.Text(shot.speaker + ":", x, y, PixelHud.Ambar2);
                    PixelHud.Text(lines[i].Substring(shot.speaker.Length + 1), x + sw + 1, y, color);
                }
                else PixelHud.Text(lines[i], x, y, color);
                y += lineH;
            }
        }

        private static List<string> Wrap(string text, int maxWidth)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                string line = "";
                foreach (var word in paragraph.Split(' '))
                {
                    string next = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && PixelHud.HasArt && PixelHud.TextWidth(next) > maxWidth)
                    {
                        lines.Add(line);
                        line = word;
                    }
                    else line = next;
                }
                lines.Add(line);
            }
            return lines;
        }

        private void DrawTransition(CutsceneShot shot)
        {
            if (shot.transition == CutsceneTransition.Fade && shotTime < FadeSeconds)
                Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black, 1f - shotTime / FadeSeconds);
            else if (current != null && index == current.shots.Length - 1)
            {
                float remaining = shot.duration - shotTime;
                if (remaining < ExitFadeOutSeconds && shotTime > FadeSeconds)
                    Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black, Mathf.Clamp01(1f - remaining / ExitFadeOutSeconds));
            }
            if (shot.transition == CutsceneTransition.Glitch && shotTime < GlitchSeconds)
            {
                // Faixas horizontais com ruído, no ritmo de quadros de fita.
                var rng = new System.Random(Mathf.FloorToInt(Time.unscaledTime * 24f));
                int bars = 10;
                for (int i = 0; i < bars; i++)
                {
                    float y = (float)rng.NextDouble() * Screen.height;
                    float h = PixelHud.Px * (1 + rng.Next(6));
                    float x = (float)(rng.NextDouble() - 0.5) * Screen.width * 0.2f;
                    var c = rng.Next(3) switch { 0 => PixelHud.Fosforo4, 1 => PixelHud.Concreto7, _ => Color.black };
                    Fill(new Rect(x, y, Screen.width, h), c, 0.35f + 0.5f * (float)rng.NextDouble());
                }
            }
        }

        private void DrawSkipHint()
        {
            if (!PixelHud.HasArt) return;
            const string label = "SEGURE ESC PARA PULAR";
            int x = PixelHud.Width - PixelHud.TextWidth(label) - 6;
            int y = PixelHud.Height - 8;
            PixelHud.Text(label, x, y, PixelHud.Concreto6, 0.7f);
            if (skipHeld > 0f)
                PixelHud.Box(x, y + 6, Mathf.RoundToInt(PixelHud.TextWidth(label) * SkipProgress), 1, PixelHud.Ambar2);
        }

        private static void Fill(Rect rect, Color color, float alpha)
        {
            GUI.color = new Color(color.r, color.g, color.b, alpha);
            GUI.DrawTexture(rect, white);
            GUI.color = Color.white;
        }
    }
}
