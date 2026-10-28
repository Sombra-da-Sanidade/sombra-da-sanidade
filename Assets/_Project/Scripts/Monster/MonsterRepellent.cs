using System.Collections.Generic;
using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Área circular onde o monstro não entra (GDD regra 6: lâmpada de teto acesa).
    // A Frente 4 coloca este componente na Zona Segura do disjuntor e liga/desliga com o timer de 60s
    // (enabled = true/false). O monstro consulta as áreas ativas pelo registro estático abaixo.
    public class MonsterRepellent : MonoBehaviour
    {
        [Tooltip("Raio da área em unidades. Combine com o alcance da Light2D do teto.")]
        [SerializeField] private float radius = 3f;

        private static readonly List<MonsterRepellent> active = new List<MonsterRepellent>();

        public static IReadOnlyList<MonsterRepellent> Active => active;

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public Vector2 Center => transform.position;

        private void OnEnable() => active.Add(this);

        private void OnDisable() => active.Remove(this);

        public bool Contains(Vector2 point, float margin = 0f)
        {
            float r = radius + margin;
            return (point - Center).sqrMagnitude < r * r;
        }

        // Ponto na borda (mais a margem) na direção de `from`. Usado para fugir ou parar na beira da luz.
        public Vector2 ClosestOutside(Vector2 from, float margin)
        {
            Vector2 dir = from - Center;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            return Center + dir.normalized * (radius + margin);
        }

        // Primeira área ativa que contém o ponto, ou null.
        public static MonsterRepellent Find(Vector2 point, float margin = 0f)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].Contains(point, margin)) return active[i];
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = enabled ? new Color(1f, 0.85f, 0.3f, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.4f);
            const int segments = 32;
            Vector3 prev = transform.position + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
