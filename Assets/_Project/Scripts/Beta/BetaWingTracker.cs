using System;
using SombraDaSanidade.Core;
using SombraDaSanidade.Monster;
using UnityEngine;

namespace SombraDaSanidade.Beta
{
    // Só na cena Beta. Sabe em que ala o jogador está (retângulos gravados pelo BetaSceneBuilder) e, ao trocar,
    // liga no monstro os comportamentos da ala (MonsterWingBehaviours, #36/#37) e mostra o nome da ala.
    // TODO(Frente 5): quando o DifficultyProfile (#30) existir, a troca de perfil substitui isto.
    public class BetaWingTracker : MonoBehaviour
    {
        [Serializable]
        public struct Wing
        {
            public string name;
            public Rect area;
        }

        [SerializeField] private Wing[] wings = Array.Empty<Wing>();
        [Tooltip("Se ligado, a ala do jogador decide os comportamentos do monstro. O painel F1 desliga ao forçar uma ala.")]
        [SerializeField] private bool followPlayer = true;

        public string Current { get; private set; } = "";
        public bool FollowPlayer { get => followPlayer; set => followPlayer = value; }

        private Transform player;
        private MonsterController monster;
        private float toastUntil;
        private GUIStyle toastStyle;

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            monster = FindAnyObjectByType<MonsterController>();
        }

        private void Update()
        {
            if (player == null) return;
            string wing = WingAt(player.position);
            if (wing == Current) return;
            Current = wing;
            toastUntil = Time.unscaledTime + 2.5f;
            if (followPlayer) Apply(wing);
        }

        public string WingAt(Vector2 position)
        {
            foreach (var w in wings)
                if (w.area.Contains(position)) return w.name;
            return "Saguao";
        }

        // Comportamentos da ala no monstro. O saguão usa os da Médica (o começo do jogo).
        public void Apply(string wing)
        {
            if (monster == null) monster = FindAnyObjectByType<MonsterController>();
            if (monster != null) monster.SetBehaviours(Behaviours(wing));
        }

        public static MonsterBehaviour Behaviours(string wing) => wing switch
        {
            "Energia" => MonsterWingBehaviours.Energia,
            "Executiva" => MonsterWingBehaviours.Executiva,
            _ => MonsterWingBehaviours.Medica
        };

        public static string DisplayName(string wing) => Level.WingCheckpoint.DisplayName(wing);

        private void OnGUI()
        {
            if (Time.unscaledTime >= toastUntil) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            toastStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            toastStyle.normal.textColor = new Color(0.75f, 0.95f, 0.75f, Mathf.Clamp01(toastUntil - Time.unscaledTime));
            GUI.Label(new Rect(0f, Screen.height * 0.12f, Screen.width, 36f), DisplayName(Current), toastStyle);
        }
    }
}
