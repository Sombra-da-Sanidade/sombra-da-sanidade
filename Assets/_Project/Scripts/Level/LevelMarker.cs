using System.Collections.Generic;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Ponto da ala reservado para um sistema de outra frente (planejamento-rc 2 e 7). A Frente de níveis
    // posiciona o marcador pela planta; quem faz o sistema (falas do Thomas, objetos de sanidade, cela da Vance,
    // atalho da Eclusa) acha o ponto com LevelMarker.Find(id) ou encaixa o componente no mesmo GameObject.
    // O id sai do builder: "<tipo>_<ala>_<n>", numerado de cima para baixo e da esquerda para a direita.
    public class LevelMarker : MonoBehaviour
    {
        public enum Kind { InnerVoice, SanityObject, VanceCell, Shortcut }

        private static readonly List<LevelMarker> all = new List<LevelMarker>();

        [SerializeField] private string id = "";
        [SerializeField] private Kind kind;
        [Tooltip("Área do marcador em unidades (gatilho de fala, cela). Zero = só o ponto.")]
        [SerializeField] private Vector2 size;

        public string Id => id;
        public Kind Type => kind;
        public Vector2 Size => size;
        public static IReadOnlyList<LevelMarker> All => all;

        public void Setup(string markerId, Kind markerKind, Vector2 area)
        {
            id = markerId;
            kind = markerKind;
            size = area;
        }

        public static LevelMarker Find(string markerId)
        {
            foreach (var m in all) if (m != null && m.id == markerId) return m;
            return null;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                Kind.InnerVoice => Color.cyan,
                Kind.SanityObject => Color.magenta,
                Kind.VanceCell => Color.yellow,
                _ => Color.green
            };
            if (size.sqrMagnitude > 0f) Gizmos.DrawWireCube(transform.position, size);
            else Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
