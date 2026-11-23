using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SombraDaSanidade.Flashlight
{
    // Liga os eventos da Lux-90 e do multímetro às partículas do GameFx (#81). Criado em runtime em toda cena
    // com GameManager, como o GameSfxHooks. Cada efeito é um prefab que se destrói sozinho (stopAction).
    public class GameFxHooks : MonoBehaviour
    {
        public static GameFxHooks Instance { get; private set; }

        private GameFx fx;
        private FlashlightController flashlight;
        private SkillCheck skillCheck;
        private SkillCheckHud skillHud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= Ensure;
            SceneManager.sceneLoaded += Ensure;
        }

        private static void Ensure(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<GameFxHooks>() != null || FindAnyObjectByType<GameManager>() == null) return;
            if (GameFx.Load() == null) return;
            new GameObject("GameFxHooks").AddComponent<GameFxHooks>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            fx = GameFx.Load();
        }

        private void Start()
        {
            flashlight = FindAnyObjectByType<FlashlightController>();
            if (flashlight != null) flashlight.EmpFired += OnEmpFired;
        }

        private void Update()
        {
            // O SkillCheck pode ser criado depois (o DriveServer cria no jogador sob demanda).
            if (skillCheck != null) return;
            skillCheck = FindAnyObjectByType<SkillCheck>();
            if (skillCheck == null) return;
            skillHud = skillCheck.GetComponent<SkillCheckHud>();
            skillCheck.Resolved += OnSkillResolved;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (flashlight != null) flashlight.EmpFired -= OnEmpFired;
            if (skillCheck != null) skillCheck.Resolved -= OnSkillResolved;
        }

        private void OnEmpFired() => Spawn(fx.empWave, flashlight.transform.position);

        private void OnSkillResolved(SkillCheckResult result)
        {
            if (result == SkillCheckResult.Great)
                Spawn(fx.skillGreat, skillHud != null ? skillHud.DialWorldPosition : skillCheck.transform.position);
            else if (result == SkillCheckResult.Fail)
                Spawn(fx.skillFail, skillCheck.TargetPosition);
        }

        // Público para os testes.
        public static GameObject Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;
            position.z = 0f;
            return Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
