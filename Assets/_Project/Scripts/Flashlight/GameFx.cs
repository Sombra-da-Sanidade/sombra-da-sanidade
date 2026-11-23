using UnityEngine;

namespace SombraDaSanidade.Flashlight
{
    // Efeitos de partícula de gameplay (#81, planejamento-rc 2). Fica em Resources/GameFx; os prefabs saem de
    // Prefabs/FX/ e são gerados por Sombra da Sanidade > Arte > Gerar efeitos de partícula (FxPrefabBuilder).
    // Quem toca é o GameFxHooks, ouvindo os eventos da Lux-90 e do multímetro. Só visual: nada aqui faz ruído.
    [CreateAssetMenu(fileName = "GameFx", menuName = "Sombra da Sanidade/Arte/Efeitos de gameplay")]
    public class GameFx : ScriptableObject
    {
        public const string ResourcePath = "GameFx";

        [Tooltip("Onda de choque azul do EMP (FlashlightController.EmpFired), centrada no Thomas. Some em 0,4 s.")]
        public GameObject empWave;
        [Tooltip("Partículas verdes no mostrador do multímetro quando o skill check dá great.")]
        public GameObject skillGreat;
        [Tooltip("Faíscas e fumaça no servidor quando o skill check falha (o tremor de tela é do SkillCheck).")]
        public GameObject skillFail;

        private static GameFx cached;

        public static GameFx Load()
        {
            if (cached == null) cached = Resources.Load<GameFx>(ResourcePath);
            return cached;
        }
    }
}
