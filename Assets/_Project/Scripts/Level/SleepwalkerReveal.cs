using System.Collections;
using SombraDaSanidade.Monster;
using SombraDaSanidade.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Level
{
    // C2 (roteiro-rc 5): o Sonâmbulo parado sob uma luz; a luz pisca e, quando volta, ele está dois passos mais
    // perto do Thomas. Fica no mesmo GameObject do CutsceneMarker "c2_lampada": a luz acende quando a C2 começa,
    // o cue do 2º plano faz o apagão e o passo, e a luz se apaga devagar quando a cutscene termina.
    [RequireComponent(typeof(CutsceneMarker))]
    public class SleepwalkerReveal : MonoBehaviour
    {
        [SerializeField] private Light2D lamp;
        [Tooltip("Quanto o Sonâmbulo avança no escuro (unidades).")]
        [SerializeField, Min(0f)] private float step = 2f;
        [SerializeField, Min(0.05f)] private float darkSeconds = 0.5f;
        [SerializeField] private string cutsceneId = "C2";

        private float baseIntensity;

        public void Setup(Light2D light, string id)
        {
            lamp = light;
            cutsceneId = id;
        }

        private void Awake()
        {
            if (lamp != null)
            {
                baseIntensity = lamp.intensity;
                lamp.enabled = false;
            }
            GetComponent<CutsceneMarker>().OnCue.AddListener(Reveal);
            CutscenePlayer.Started += OnStarted;
            CutscenePlayer.Finished += OnFinished;
        }

        private void OnDestroy()
        {
            CutscenePlayer.Started -= OnStarted;
            CutscenePlayer.Finished -= OnFinished;
        }

        private void OnStarted(Cutscene c)
        {
            if (c == null || c.id != cutsceneId || lamp == null) return;
            lamp.intensity = baseIntensity;
            lamp.enabled = true;
        }

        private void OnFinished(Cutscene c)
        {
            if (c != null && c.id == cutsceneId && lamp != null) StartCoroutine(FadeOut());
        }

        private void Reveal() => StartCoroutine(BlinkAndStep());

        private IEnumerator BlinkAndStep()
        {
            if (lamp != null) lamp.enabled = false;
            yield return new WaitForSecondsRealtime(darkSeconds);
            var walker = FindAnyObjectByType<SleepwalkerController>();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (walker != null && player != null)
            {
                Vector2 from = walker.transform.position;
                Vector2 to = Vector2.MoveTowards(from, player.transform.position, step);
                var agent = walker.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh) agent.Warp(to);
                else walker.transform.position = to;
                transform.position = to;
            }
            if (lamp != null) lamp.enabled = true;
        }

        private IEnumerator FadeOut()
        {
            for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                lamp.intensity = Mathf.Lerp(baseIntensity, 0f, t / 1.5f);
                yield return null;
            }
            lamp.enabled = false;
        }
    }
}
