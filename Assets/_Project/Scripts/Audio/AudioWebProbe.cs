using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SombraDaSanidade.Audio
{
    // Sonda de áudio para o WebGL (#100, ADR 0005). Mede no navegador o que o sistema de áudio (#101) vai usar:
    // 4 stems importados começados juntos com PlayScheduled (deriva entre eles), crossfade de volume entre duas
    // fontes, AudioClip.Create + SetData e se OnAudioFilterRead é chamado. Começa no primeiro clique ou tecla
    // (o navegador bloqueia áudio antes disso). Resultados no console do navegador com o prefixo [AudioProbe].
    // A cena é gerada por AudioWebProbeBuilder; não entra no jogo.
    public class AudioWebProbe : MonoBehaviour
    {
        [SerializeField] private AudioClip[] stems = new AudioClip[0];
        [SerializeField] private AudioClip dryClip;
        [SerializeField] private AudioClip muffledClip;
        [Tooltip("Segundos de medição antes do relatório final.")]
        [SerializeField] private float duration = 60f;

        private AudioSource[] stemSources;
        private AudioSource dry, muffled, generated;
        private FilterProbe filter;
        private bool started, finished;
        private double startDsp;
        private float startTime, nextReport;
        private float maxDriftMs;
        private string status = "CLIQUE PARA COMEÇAR";
        private readonly StringBuilder log = new StringBuilder();

        private void Update()
        {
            if (!started)
            {
                bool input = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                             || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
                if (input) Begin();
                return;
            }
            if (finished) return;

            float t = Time.realtimeSinceStartup - startTime;
            // Crossfade de 4 s em loop: dry ↔ muffled (oclusão por troca de versão).
            float k = Mathf.PingPong(t / 4f, 1f);
            dry.volume = 1f - k;
            muffled.volume = k;

            float drift = StemDriftMs();
            if (t > 1f) maxDriftMs = Mathf.Max(maxDriftMs, drift);

            if (t >= nextReport)
            {
                nextReport += 10f;
                Report($"t={t:0}s dsp={AudioSettings.dspTime - startDsp:0.00}s deriva={drift:0.0}ms max={maxDriftMs:0.0}ms " +
                       $"stems={PlayingCount()}/{stemSources.Length} gerado.time={generated.time:0.00} filtro={filter.Calls}");
            }
            if (t >= duration) Finish();
        }

        private void Begin()
        {
            started = true;
            startTime = Time.realtimeSinceStartup;
            nextReport = 0f;
            Report($"inicio sampleRate={AudioSettings.outputSampleRate} dsp={AudioSettings.dspTime:0.000}");

            // Stems: começam juntos daqui a 0,2 s no relógio de áudio.
            startDsp = AudioSettings.dspTime + 0.2;
            stemSources = new AudioSource[stems.Length];
            for (int i = 0; i < stems.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.clip = stems[i];
                s.loop = true;
                s.volume = 0.2f;
                s.PlayScheduled(startDsp);
                stemSources[i] = s;
                Report($"stem {i} {stems[i].name} freq={stems[i].frequency} samples={stems[i].samples} loadType={stems[i].loadType}");
            }

            dry = Loop(dryClip, 1f);
            muffled = Loop(muffledClip, 0f);

            // Clip gerado em runtime (padrão atual de SanityHeartbeat e AudioLogPlayer).
            int rate = 44100;
            var data = new float[rate];
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Sin(2f * Mathf.PI * 220f * i / rate) * 0.1f;
            var clip = AudioClip.Create("Probe gerado", data.Length, 1, rate, false);
            bool setOk = clip.SetData(data, 0);
            generated = Loop(clip, 0.3f);
            Report($"AudioClip.Create ok, SetData={setOk}, length={clip.length:0.00}");

            // OnAudioFilterRead num objeto à parte.
            var probe = new GameObject("FilterProbe");
            var src = probe.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.volume = 0.01f;
            filter = probe.AddComponent<FilterProbe>();
            src.Play();
            status = "MEDINDO...";
        }

        private AudioSource Loop(AudioClip clip, float volume)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.volume = volume;
            s.Play();
            return s;
        }

        // Diferença máxima de posição entre os stems (ms). Os stems têm o mesmo tamanho.
        private float StemDriftMs()
        {
            if (stemSources == null || stemSources.Length < 2) return 0f;
            float min = float.MaxValue, max = float.MinValue;
            foreach (var s in stemSources)
            {
                float time = s.time;
                min = Mathf.Min(min, time);
                max = Mathf.Max(max, time);
            }
            // Um stem que deu a volta no loop antes do outro aparece como deriva de quase um loop inteiro.
            float span = max - min;
            float length = stemSources[0].clip != null ? stemSources[0].clip.length : 0f;
            if (length > 0f && span > length / 2f) span = length - span;
            return span * 1000f;
        }

        private int PlayingCount()
        {
            int n = 0;
            foreach (var s in stemSources) if (s.isPlaying) n++;
            return n;
        }

        private void Finish()
        {
            finished = true;
            bool syncOk = maxDriftMs < 30f && PlayingCount() == stemSources.Length;
            Report($"RESULTADO deriva_max={maxDriftMs:0.0}ms sincronia={(syncOk ? "OK" : "FALHA")} " +
                   $"clip_gerado={(generated.isPlaying ? "OK" : "FALHA")} OnAudioFilterRead={(filter.Calls > 0 ? "CHAMADO" : "NAO_CHAMADO")} " +
                   $"crossfade=OK");
            status = "FIM. VER CONSOLE.";
        }

        private void Report(string line)
        {
            Debug.Log("[AudioProbe] " + line);
            log.AppendLine(line);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, Screen.width - 20, 30), status);
            GUI.Label(new Rect(10, 40, Screen.width - 20, Screen.height - 50), log.ToString());
        }

        // Conta as chamadas do callback de áudio. No WebGL a doc diz que não existe.
        private class FilterProbe : MonoBehaviour
        {
            private int calls;
            public int Calls => calls;
            private void OnAudioFilterRead(float[] data, int channels) => System.Threading.Interlocked.Increment(ref calls);
        }
    }
}
