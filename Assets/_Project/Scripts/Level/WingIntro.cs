using System;
using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Introdução de uma ala (Proposta 4 do v1.1, "ensinar antes de cobrar"). O monstro começa adormecido:
    // invisível, parado e sem colisão, mas com o GameObject ativo (lanterna, sanidade e diretor de tensão o
    // procuram com FindAnyObjectByType e ignoram enquanto o controller estiver desligado). Depois de
    // calmSeconds, quando o jogador entra no gatilho de uma variante, ele acorda no início da rota e passa
    // ao fundo (MonsterController.FollowRoute). Se o jogador demorar demais, acorda na
    // variante mais longe dele; se pegar o HD antes, surge atrás dele na mesma sala. Montado pelo AlfaSceneBuilder a partir dos marcadores da planta.
    public class WingIntro : MonoBehaviour
    {
        [Serializable]
        public struct RouteVariant
        {
            public string name;
            public Vector2[] route;
            public Rect trigger;
        }

        [SerializeField] private MonsterController monster;
        [Tooltip("Tempo mínimo sem monstro (GDD v1.1, Proposta 4: ~1 min).")]
        [SerializeField, Min(0f)] private float calmSeconds = 35f;
        [Tooltip("Se o jogador não passar por nenhum gatilho até aqui, o monstro acorda mesmo assim.")]
        [SerializeField, Min(0f)] private float maxCalmSeconds = 60f;
        [SerializeField] private RouteVariant[] variants = Array.Empty<RouteVariant>();
        [Tooltip("Cutscene no motor tocada quando o monstro acorda (C1, planejamento-rc 4). Vazio: só a rota.")]
        [SerializeField] private Cutscene wakeCutscene;
        [Tooltip("O monstro nunca acorda nesta ala (fase 2, só o Sonâmbulo). Ele fica na cena adormecido para os " +
                 "sistemas que o procuram (lanterna, sanidade, tensão) seguirem funcionando.")]
        [SerializeField] private bool neverWakes;

        public bool Awakened { get; private set; }
        public string Variant { get; private set; } = "";
        public float CalmSeconds => calmSeconds;

        private Transform player;
        private float elapsed;
        private bool pendingRoute;
        private Vector2[] chosenRoute;
        private readonly List<Behaviour> hidden = new List<Behaviour>();
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<Collider2D> hiddenColliders = new List<Collider2D>();
        private readonly List<AudioSource> mutedAudio = new List<AudioSource>();

        private void Awake()
        {
            if (monster == null) monster = FindAnyObjectByType<MonsterController>();
            if (monster != null && neverWakes)
            {
                Sleep();
                Awakened = false;
                enabled = false;
                return;
            }
            if (monster == null || variants.Length == 0)
            {
                Debug.LogWarning($"{name}: WingIntro sem monstro ou sem rotas; a ala começa normal.", this);
                Awakened = true;
                enabled = false;
                return;
            }
            Sleep();
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDriveCollected;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
        }

        // Pegou o HD antes do encontro: o Espécime surge na mesma sala, no escuro atrás do Thomas, e a C1 toca.
        private void OnDriveCollected(int total) => WakeNearPlayer();

        private void WakeNearPlayer()
        {
            if (Awakened) return;
            if (player == null) { WakeFarthest(); return; }
            Vector2 at = NearPlayerSpot(player, 5.5f);
            Wake(new RouteVariant { name = "hd", route = new[] { at } });
        }

        // Ponto no NavMesh atrás do Thomas (contra o lado para onde ele olha), sem parede no meio. Tenta outras
        // direções se atrás não der.
        private static Vector2 NearPlayerSpot(Transform player, float distance)
        {
            Vector2 from = player.position;
            var pc = player.GetComponent<SombraDaSanidade.Player.PlayerController>();
            Vector2 back = pc != null && pc.Facing.sqrMagnitude > 0.01f ? -pc.Facing.normalized : Vector2.down;
            int walls = LayerMask.GetMask("Walls");
            for (int k = 0; k < 8; k++)
            {
                Vector2 dir = Quaternion.Euler(0f, 0f, (k % 2 == 0 ? 1 : -1) * (k + 1) / 2 * 45f) * back;
                for (float d = distance; d >= 2.5f; d -= 0.5f)
                {
                    Vector2 p = from + dir * d;
                    if (Physics2D.Linecast(from, p, walls)) continue;
                    if (UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 0.8f, UnityEngine.AI.NavMesh.AllAreas)) return hit.position;
                }
            }
            return from + back * 2.5f;
        }

        private void Update()
        {
            if (pendingRoute)
            {
                // Um frame depois de religar: o Start do monstro já pôs o agente no NavMesh.
                pendingRoute = false;
                monster.FollowRoute(chosenRoute);
                enabled = false;
                return;
            }
            if (Awakened) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            elapsed += Time.deltaTime;
            if (elapsed >= maxCalmSeconds)
            {
                WakeFarthest();
                return;
            }
            if (elapsed < calmSeconds || player == null) return;
            foreach (var v in variants)
            {
                if (!v.trigger.Contains(player.position)) continue;
                Wake(v);
                return;
            }
        }

        // ---------- Dormir e acordar ----------

        private void Sleep()
        {
            // O Awake do monstro já desligou o agente; com o controller desligado o Start dele espera.
            monster.enabled = false;
            foreach (var r in monster.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
            foreach (var c in monster.GetComponentsInChildren<Collider2D>())
                if (c.enabled) { c.enabled = false; hiddenColliders.Add(c); }
            foreach (var a in monster.GetComponentsInChildren<AudioSource>())
                if (!a.mute) { a.mute = true; mutedAudio.Add(a); }
            // Luzes (brilho dos olhos) e animadores também dormem.
            foreach (var b in monster.GetComponentsInChildren<Behaviour>())
            {
                if (b == monster || !b.enabled) continue;
                if (b is UnityEngine.Rendering.Universal.Light2D || b is MonsterSpriteAnimator)
                {
                    b.enabled = false;
                    hidden.Add(b);
                }
            }
        }

        public void WakeNow() => WakeFarthest();

        private void WakeFarthest()
        {
            if (Awakened) return;
            Vector2 from = player != null ? (Vector2)player.position : Vector2.zero;
            RouteVariant best = variants[0];
            float bestDist = -1f;
            foreach (var v in variants)
            {
                if (v.route == null || v.route.Length == 0) continue;
                float d = (v.route[0] - from).sqrMagnitude;
                if (d > bestDist) { bestDist = d; best = v; }
            }
            Wake(best);
        }

        private void Wake(RouteVariant v)
        {
            if (Awakened || v.route == null || v.route.Length == 0) return;
            Awakened = true;
            Variant = v.name;
            chosenRoute = v.route;

            monster.transform.position = v.route[0];
            foreach (var r in hiddenRenderers) if (r != null) r.enabled = true;
            foreach (var c in hiddenColliders) if (c != null) c.enabled = true;
            foreach (var a in mutedAudio) if (a != null) a.mute = false;
            foreach (var b in hidden) if (b != null) b.enabled = true;
            monster.enabled = true;
            pendingRoute = true;
            // C1: a câmera vai até o Espécime no começo da rota. A cutscene precisa de freezeWorld desligado
            // para ele andar enquanto ela toca.
            if (wakeCutscene != null) StartCoroutine(PlayWakeCutscene());
        }

        // Ao pegar o HD, a tela de upgrade abre (e pausa o jogo): a C1 espera ela fechar para não brigar com a pausa.
        private System.Collections.IEnumerator PlayWakeCutscene()
        {
            yield return null;
            while (UpgradeTerminal.IsOpen || ModalScreen.IsOpen || CutscenePlayer.IsPlaying) yield return null;
            if (!CutscenePlayer.HasPlayed(wakeCutscene.id)) CutscenePlayer.Play(wakeCutscene);
        }
    }
}
