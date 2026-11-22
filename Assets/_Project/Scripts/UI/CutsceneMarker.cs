using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SombraDaSanidade.UI
{
    // Ponto da cena que uma cutscene pode usar (#88): alvo de câmera (shot.focus = id) e reação a um cue
    // (shot.cue = id), por exemplo a lâmpada que pisca e o Sonâmbulo que aparece mais perto na C2.
    // As alas (RC2) colocam os marcadores; o CutscenePlayer só procura pelo id.
    public class CutsceneMarker : MonoBehaviour
    {
        private static readonly List<CutsceneMarker> all = new List<CutsceneMarker>();

        [Tooltip("Nome usado em shot.focus e shot.cue.")]
        [SerializeField] private string id = "";

        [Tooltip("Chamado quando um plano com cue igual ao id começa.")]
        [SerializeField] private UnityEvent onCue = new UnityEvent();

        public string Id => id;
        public UnityEvent OnCue => onCue;

        public void Setup(string markerId) => id = markerId;

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public static CutsceneMarker Find(string markerId)
        {
            foreach (var m in all) if (m != null && m.id == markerId) return m;
            return null;
        }

        public static void Fire(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return;
            foreach (var m in all.ToArray()) if (m != null && m.id == cue) m.onCue.Invoke();
        }
    }
}
