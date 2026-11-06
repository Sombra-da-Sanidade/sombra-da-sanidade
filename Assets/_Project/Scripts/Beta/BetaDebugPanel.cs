using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Monster;
using SombraDaSanidade.Sanity;
using SombraDaSanidade.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.Beta
{
    // Painel de teste da cena Beta (F1). Pausa o jogo enquanto aberto. Serve para testar o que ainda não tem
    // como ser alcançado jogando: comportamentos de cada ala, modo seguro, sanidade e bateria em valores
    // específicos, pular para cada objeto do mapa. F2 força um skill check enquanto o jogador ejeta um HD.
    // O valor do diretor de tensão aparece só aqui, nunca no HUD (GDD 8).
    public class BetaDebugPanel : MonoBehaviour
    {
        [Serializable]
        public struct Point
        {
            public string label;
            public Transform target;
        }

        [SerializeField] private int seed;
        [SerializeField] private Point[] points = Array.Empty<Point>();

        public bool IsOpen { get; private set; }
        public int Seed => seed;

        private BetaWingTracker wings;
        private GameObject player;
        private FlashlightController flashlight;
        private SanityController sanity;
        private PlayerInteractor interactor;
        private SkillCheck skillCheck;
        private MonsterController monster;
        private Vector2 scroll;
        private float previousTimeScale = 1f;
        private GUIStyle hintStyle, headerStyle;

        private void Start()
        {
            wings = FindAnyObjectByType<BetaWingTracker>();
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                flashlight = player.GetComponent<FlashlightController>();
                sanity = player.GetComponent<SanityController>();
                interactor = player.GetComponent<PlayerInteractor>();
            }
            monster = FindAnyObjectByType<MonsterController>(FindObjectsInactive.Include);
        }

        private void OnDisable()
        {
            if (IsOpen) Close();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame && !UpgradeTerminal.IsOpen)
            {
                if (IsOpen) Close();
                else Open();
            }
            if (kb.f2Key.wasPressedThisFrame && !IsOpen) ForceSkillCheck();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            ModalScreen.Push();
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            ModalScreen.Pop();
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying) Time.timeScale = previousTimeScale;
        }

        // ---------- Ações (públicas para o BetaSceneTest) ----------

        public void ForceWing(string wing)
        {
            if (wings == null) return;
            wings.FollowPlayer = false;
            wings.Apply(wing);
        }

        public void FollowPlayerWing()
        {
            if (wings == null) return;
            wings.FollowPlayer = true;
            wings.Apply(wings.Current);
        }

        public void SetSanity(float value)
        {
            if (sanity != null) sanity.Add(value - sanity.Value);
        }

        public void SetBattery(float value)
        {
            if (flashlight == null) return;
            if (value >= flashlight.Battery) flashlight.Recharge(value - flashlight.Battery);
            else flashlight.TrySpend(flashlight.Battery - value);
        }

        public void TeleportTo(Vector2 position)
        {
            if (player == null) return;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = position;
            player.transform.position = position;
        }

        public bool ForceSkillCheck()
        {
            if (interactor == null) return false;
            if (skillCheck == null) skillCheck = interactor.GetComponent<SkillCheck>();
            return skillCheck != null && interactor.IsChanneling && skillCheck.StartCheck();
        }

        // ---------- Tela ----------

        private void OnGUI()
        {
            hintStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontSize = 13 };
            hintStyle.normal.textColor = new Color(0.55f, 0.75f, 0.55f, 0.8f);
            if (!IsOpen)
            {
                GUI.Label(new Rect(Screen.width - 330f, Screen.height - 26f, 320f, 22f), $"BETA seed {seed}  ·  F1 painel de teste  ·  F2 skill check", hintStyle);
                return;
            }

            headerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = new Color(0.6f, 1f, 0.6f);

            float w = Mathf.Min(560f, Screen.width - 32f);
            float h = Mathf.Min(620f, Screen.height - 32f);
            GUILayout.BeginArea(new Rect(16f, 16f, w, h), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label($"PAINEL DE TESTE (Beta, seed {seed})   F1 fecha", headerStyle);
            Status();

            GUILayout.Label("Comportamentos do monstro", headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Seguir ala do jogador")) FollowPlayerWing();
            if (GUILayout.Button("Médica")) ForceWing("Medica");
            if (GUILayout.Button("Energia")) ForceWing("Energia");
            if (GUILayout.Button("Executiva")) ForceWing("Executiva");
            GUILayout.EndHorizontal();

            GUILayout.Label("Monstro", headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(MonsterSafeMode.Enabled ? "Modo seguro: LIGADO" : "Modo seguro: desligado"))
                MonsterSafeMode.Enabled = !MonsterSafeMode.Enabled;
            if (monster != null)
            {
                if (GUILayout.Button("Atordoar 4 s")) monster.Stun(4f);
                if (GUILayout.Button(monster.gameObject.activeSelf ? "Desligar monstro" : "Ligar monstro"))
                    monster.gameObject.SetActive(!monster.gameObject.activeSelf);
                if (GUILayout.Button("Ruído no jogador") && player != null)
                    NoiseBus.Emit(player.transform.position, 40f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Jogador", headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sanidade 100")) SetSanity(100f);
            if (GUILayout.Button("Sanidade 30")) SetSanity(30f);
            if (GUILayout.Button("Sanidade 10")) SetSanity(10f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bateria cheia")) SetBattery(9999f);
            if (GUILayout.Button("Bateria 10")) SetBattery(10f);
            if (GUILayout.Button("+1 HD") && GameManager.Instance != null) GameManager.Instance.CollectDrive();
            GUILayout.EndHorizontal();

            GUILayout.Label("Ir para", headerStyle);
            int col = 0;
            GUILayout.BeginHorizontal();
            foreach (var p in points)
            {
                if (p.target == null) continue;
                if (GUILayout.Button(p.label, GUILayout.Width((w - 40f) / 3f)))
                {
                    TeleportTo(p.target.position);
                    Close();
                }
                if (++col % 3 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Status()
        {
            string wing = wings != null ? BetaWingTracker.DisplayName(wings.Current) : "?";
            string follow = wings != null && !wings.FollowPlayer ? " (forçada no painel)" : "";
            GUILayout.Label($"Ala do jogador: {wing}{follow}");
            if (monster != null)
                GUILayout.Label($"Monstro: {monster.State}, comportamentos {monster.Behaviours}, velocidade {monster.CurrentSpeed:0.00}");
            var tension = TensionDirector.Instance;
            if (tension != null) GUILayout.Label($"Tensão: {tension.Value:0.00} ({tension.Phase})");
            if (sanity != null) GUILayout.Label($"Sanidade: {sanity.Value:0} ({sanity.State})");
            if (flashlight != null) GUILayout.Label($"Bateria: {flashlight.Battery:0}/{flashlight.MaxBattery:0}, upgrades: {flashlight.Installed.Count}");
        }
    }
}
