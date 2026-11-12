using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Números do Sonâmbulo (Voluntário 03). Nenhum está no GDD: são chutes para o playtest (docs/sonambulo.md).
    [CreateAssetMenu(fileName = "SleepwalkerConfig", menuName = "Sombra da Sanidade/Config/Sonâmbulo")]
    public class SleepwalkerConfig : ScriptableObject
    {
        [Header("Movimento (só quando ninguém olha)")]
        [Tooltip("Velocidade fora da vista. O jogador anda a 4 m/s: virar as costas por um segundo custa caro.")]
        public float speed = 7.5f;
        [Tooltip("Aceleração do NavMeshAgent. Alta para ele arrancar e parar de uma vez.")]
        public float acceleration = 80f;
        [Tooltip("Intervalo entre recálculos do caminho até o jogador.")]
        public float repathInterval = 0.15f;

        [Header("Luz e visão")]
        [Tooltip("Alturas acima dos pés testadas (o sprite tem 4 m). Basta uma estar no cone da lanterna para ele travar.")]
        public float[] sampleHeights = { 0.3f, 1.6f, 3f };
        [Tooltip("Despertar: ângulo do campo de visão do Thomas, centrado na mira. Só serve para acordá-lo; " +
                 "depois disso, o que o trava é a lanterna ligada apontada para ele.")]
        public float viewAngle = 110f;
        [Tooltip("Despertar: até onde o Thomas o nota sem a lanterna.")]
        public float viewDistance = 9f;

        [Header("Despertar")]
        [Tooltip("Começa adormecido e só passa a agir na primeira vez que o Thomas o vê.")]
        public bool startsDormant = true;
        [Tooltip("Depois de acordar, fica este tempo parado mesmo sem ser visto (dá tempo de entender o que é).")]
        public float wakeGrace = 1.5f;
        [Tooltip("Depois de sair da vista, espera isto antes de se mover (evita andar na borda do campo de visão).")]
        public float unseenDelay = 0.12f;

        [Header("Poses")]
        [Tooltip("Travado a menos desta distância do jogador, usa a pose do bote.")]
        public float lungePoseDistance = 3f;

        [Header("Morte (cena de 80x80 antes do Game Over)")]
        public float killFps = 12f;
        public float killHold = 0.8f;

        [Header("Zona Segura (GDD regra 6)")]
        public float safeZoneMargin = 0.6f;

        [Header("Modo seguro (#48)")]
        public float safeModeSanityDamage = 20f;
        public float safeModePushDistance = 2.5f;
        [Tooltip("Depois do encontrão, fica travado este tempo mesmo sem ser visto.")]
        public float safeModeHitPause = 2f;
    }
}
