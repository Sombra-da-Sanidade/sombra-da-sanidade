using System;
using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Narrative
{
    // Toca as gravações da Dra. Vance (GDD 9, issue #39). Frente 3 (Sanidade, HUD e narrativa). Um por cena.
    // - Cada HD coletado toca o próximo log de driveLogs (o HD é a recompensa narrativa da ala).
    // - Terminais (AudioLogTerminal) tocam um log avulso.
    // Sem voz gravada, a legenda é digitada letra a letra e cada letra solta um bipe com timbre de vogal,
    // como a fala de jogos antigos: o HUD (DemoHud) desenha a legenda a partir de VisibleText.
    // Monstro perto pausa a gravação com estática ("sinal perdido"); ela continua quando ele se afasta.
    [RequireComponent(typeof(AudioSource))]
    public class AudioLogPlayer : MonoBehaviour
    {
        // Formantes F1 e F2 (Hz) de a, e, i, o, u: dão a cada bipe o timbre de uma vogal.
        private static readonly Vector2[] Formants =
        {
            new Vector2(800f, 1200f), new Vector2(500f, 1900f), new Vector2(320f, 2300f),
            new Vector2(500f, 900f), new Vector2(330f, 800f)
        };

        [SerializeField] private NarrativeConfig config;

        [Tooltip("Log que toca ao coletar cada HD, na ordem: o 1º HD coletado toca o primeiro, e assim por diante.")]
        [SerializeField] private AudioLog[] driveLogs = new AudioLog[0];

        public static AudioLogPlayer Instance { get; private set; }

        public AudioLog Current { get; private set; }
        public bool IsPlaying => Current != null;
        // Monstro perto: gravação pausada, HUD mostra o sinal perdido.
        public bool SignalLost { get; private set; }
        // Linha inteira e o trecho já digitado. Vazios na pausa entre linhas.
        public string FullLine => IsPlaying && !inPause ? Current.lines[lineIndex] : "";
        public string VisibleText => IsPlaying && !inPause ? Current.lines[lineIndex].Substring(0, typed) : "";
        // Segundos que faltam para a legenda atual sumir (o HUD usa para o fade de saída).
        public float LineTimeLeft => typed < FullLine.Length ? float.MaxValue : holdLeft;
        public float LineTime { get; private set; }
        // Terminal que está tocando (null para log de HD).
        public Transform Source { get; private set; }

        public event Action<AudioLog> Started;
        public event Action<AudioLog> Completed;
        public event Action<bool> SignalLostChanged;

        private NarrativeConfig Config => config != null ? config : (config = ScriptableObject.CreateInstance<NarrativeConfig>());

        private readonly Queue<AudioLog> queue = new Queue<AudioLog>();
        private AudioSource voice;
        private AudioSource babble;
        private AudioSource hiss;
        private AudioClip[] vowels;
        private AudioClip clickClip, staticClip, hissClip;
        private SanityController listener;
        private float hearingRadius;
        private int lineIndex;
        private int typed;
        private float typeTimer;
        private float holdLeft;
        private bool inPause;
        private float pauseLeft;
        private float lastBlip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{name}: mais de um AudioLogPlayer na cena; este foi desligado.", this);
                enabled = false;
                return;
            }
            Instance = this;

            voice = GetComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            babble = gameObject.AddComponent<AudioSource>();
            babble.playOnAwake = false;
            babble.spatialBlend = 0f;
            hiss = gameObject.AddComponent<AudioSource>();
            hiss.playOnAwake = false;
            hiss.spatialBlend = 0f;
            hiss.loop = true;

            vowels = new AudioClip[Formants.Length];
            for (int i = 0; i < Formants.Length; i++) vowels[i] = CreateVowelBlip(Formants[i]);
            clickClip = CreateNoise("Clique do gravador (sintético)", 0.06f, 70f, 0.6f);
            staticClip = CreateNoise("Estática (sintética)", 0.5f, 3f, 0.5f);
            hissClip = CreateNoise("Chiado de fita (sintético)", 2f, 0f, 1f);
            hiss.clip = hissClip;
        }

        private void Start()
        {
            listener = FindAnyObjectByType<SanityController>();
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected += OnDriveCollected;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null) GameManager.Instance.DriveCollected -= OnDriveCollected;
            if (vowels != null) foreach (var clip in vowels) Destroy(clip);
            Destroy(clickClip);
            Destroy(staticClip);
            Destroy(hissClip);
        }

        private void OnDriveCollected(int count)
        {
            int index = count - 1;
            if (index < 0 || index >= driveLogs.Length || driveLogs[index] == null) return;
            // O log do HD tem prioridade sobre um terminal; dois HDs seguidos entram na fila.
            if (IsPlaying && Source != null) Stop();
            Play(driveLogs[index]);
        }

        // source e radius: só para terminais. Longe demais do terminal, o jogador deixa de ouvir.
        public void Play(AudioLog log, Transform source = null, float radius = 0f)
        {
            if (log == null || log.lines == null || log.lines.Length == 0) return;
            if (IsPlaying)
            {
                if (source == null) queue.Enqueue(log);
                return;
            }

            Current = log;
            Source = source;
            hearingRadius = radius;
            SetSignalLost(false);
            babble.PlayOneShot(clickClip);
            hiss.volume = Config.tapeHissVolume;
            hiss.Play();
            if (log.voice != null)
            {
                voice.clip = log.voice;
                voice.Play();
            }
            StartLine(0);
            Started?.Invoke(log);
        }

        public void Stop()
        {
            if (!IsPlaying) return;
            Current = null;
            Source = null;
            SetSignalLost(false);
            voice.Stop();
            hiss.Stop();
        }

        private void Update()
        {
            if (!IsPlaying) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                Stop();
                queue.Clear();
                return;
            }

            if (listener != null && Source != null
                && Vector2.Distance(listener.transform.position, Source.position) > hearingRadius)
            {
                Stop();
                return;
            }

            bool monsterNear = listener != null && listener.DistanceToMonster <= Config.interruptRadius;
            if (monsterNear != SignalLost)
            {
                SetSignalLost(monsterNear);
                babble.PlayOneShot(monsterNear ? staticClip : clickClip);
            }
            if (SignalLost) return;

            LineTime += Time.deltaTime;
            if (inPause)
            {
                pauseLeft -= Time.deltaTime;
                if (pauseLeft > 0f) return;
                if (lineIndex + 1 < Current.lines.Length) StartLine(lineIndex + 1);
                else Finish();
                return;
            }

            string line = Current.lines[lineIndex];
            if (typed < line.Length)
            {
                typeTimer -= Time.deltaTime;
                while (typeTimer <= 0f && typed < line.Length)
                {
                    char c = line[typed++];
                    typeTimer += 1f / CharactersPerSecond() + PunctuationPause(c, line, typed);
                    if (Current.voice == null) Blip(c);
                }
                if (typed == line.Length)
                    holdLeft = Config.minHoldSeconds + line.Length * Config.holdPerCharacter;
                return;
            }

            holdLeft -= Time.deltaTime;
            if (holdLeft <= 0f)
            {
                inPause = true;
                pauseLeft = Config.pauseBetweenLines;
            }
        }

        private void StartLine(int index)
        {
            lineIndex = index;
            typed = 0;
            typeTimer = 0f;
            LineTime = 0f;
            inPause = false;
        }

        private void Finish()
        {
            var log = Current;
            Stop();
            AudioLogArchive.Add(log);
            Completed?.Invoke(log);
            if (queue.Count > 0) Play(queue.Dequeue());
        }

        private void SetSignalLost(bool lost)
        {
            if (lost == SignalLost) return;
            SignalLost = lost;
            if (lost) voice.Pause();
            else if (Current != null && Current.voice != null) voice.UnPause();
            hiss.volume = Config.tapeHissVolume * (lost ? 3f : 1f);
            SignalLostChanged?.Invoke(lost);
        }

        // Com voz gravada, a digitação acompanha a duração da gravação, dividida pelo tamanho das linhas.
        private float CharactersPerSecond()
        {
            if (Current.voice == null) return Mathf.Max(1f, Config.charactersPerSecond);
            int total = 0;
            foreach (var l in Current.lines) total += l.Length;
            float speech = Current.voice.length - Config.pauseBetweenLines * (Current.lines.Length - 1);
            return Mathf.Max(1f, total / Mathf.Max(0.5f, speech * 0.85f));
        }

        // Pausa depois da pontuação, mas não no meio de "..." nem no fim da linha.
        private float PunctuationPause(char c, string line, int next)
        {
            if (Current.voice != null || next >= line.Length || line[next] == '.') return 0f;
            if (c == ',' || c == ';' || c == ':') return Config.commaPause;
            if (c == '.' || c == '!' || c == '?') return Config.sentencePause;
            return 0f;
        }

        // Um bipe por letra: vogais com o próprio timbre, consoantes com uma vogal sorteada pela letra, mais baixas.
        private void Blip(char c)
        {
            char k = Fold(char.ToLowerInvariant(c));
            if (!char.IsLetterOrDigit(k)) return;
            if (Time.time - lastBlip < Config.babbleMinInterval) return;
            lastBlip = Time.time;

            int vowel = "aeiou".IndexOf(k);
            bool isVowel = vowel >= 0;
            if (!isVowel) vowel = k % Formants.Length;
            babble.pitch = Current.babblePitch * (isVowel ? 1f : 0.92f) * (1f + ((k * 7) % 5 - 2) * 0.015f);
            babble.PlayOneShot(vowels[vowel], Config.babbleVolume * (isVowel ? 1f : 0.7f));
        }

        private static char Fold(char c)
        {
            const string from = "áàâãäéèêëíìîïóòôõöúùûüç";
            const string to = "aaaaaeeeeiiiiooooouuuuc";
            int i = from.IndexOf(c);
            return i >= 0 ? to[i] : c;
        }

        // Pulso de voz (fundamental de 190 Hz) com harmônicos realçados perto de F1 e F2,
        // e amostras quantizadas para soar como chip de som antigo.
        private static AudioClip CreateVowelBlip(Vector2 formants)
        {
            const int rate = 22050;
            const float length = 0.075f;
            const float f0 = 190f;
            var data = new float[(int)(rate * length)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float sample = 0f;
                for (int k = 1; k * f0 < 3200f; k++)
                {
                    float f = k * f0;
                    float a = Mathf.Exp(-Mathf.Pow((f - formants.x) / 160f, 2f))
                              + 0.6f * Mathf.Exp(-Mathf.Pow((f - formants.y) / 260f, 2f))
                              + 0.15f / k;
                    sample += a * Mathf.Sin(2f * Mathf.PI * f * t);
                }
                float envelope = Mathf.Clamp01(t / 0.006f) * Mathf.Clamp01((length - t) / 0.03f);
                data[i] = Mathf.Round(sample * envelope * 0.35f * 16f) / 16f;
            }
            var clip = AudioClip.Create("Bipe de fala (sintético)", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Ruído com decaimento: estalo (decay alto), estática (baixo) ou chiado contínuo (0).
        private static AudioClip CreateNoise(string clipName, float seconds, float decay, float gain)
        {
            const int rate = 22050;
            var data = new float[(int)(rate * seconds)];
            var rng = new System.Random(clipName.Length);
            float smooth = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                smooth += 0.35f * ((float)(rng.NextDouble() * 2.0 - 1.0) - smooth);
                data[i] = smooth * Mathf.Exp(-t * decay) * gain;
            }
            var clip = AudioClip.Create(clipName, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
