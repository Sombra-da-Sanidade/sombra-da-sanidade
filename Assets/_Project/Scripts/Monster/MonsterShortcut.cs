using NavMeshPlus.Components;
using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Atalho só do monstro (#37, Ala de Energia): um NavMeshLink entre dois pontos de chão, por exemplo
    // através de um duto na parede. O jogador não usa NavMesh, então só o monstro passa por ele.
    // O link fica ligado apenas quando a ala atual dá o comportamento Shortcuts ao monstro.
    // Frente 4: coloque este componente no mapa e ajuste Start Point / End Point do NavMeshLink (locais).
    [RequireComponent(typeof(NavMeshLink))]
    public class MonsterShortcut : MonoBehaviour
    {
        private NavMeshLink link;
        private MonsterController monster;

        private void Awake()
        {
            link = GetComponent<NavMeshLink>();
            link.bidirectional = true;
            link.enabled = false;
        }

        private void Start()
        {
            monster = FindAnyObjectByType<MonsterController>();
            if (monster != null) monster.BehavioursChanged += Refresh;
            Refresh(monster != null ? monster.Behaviours : MonsterBehaviour.None);
        }

        private void OnDestroy()
        {
            if (monster != null) monster.BehavioursChanged -= Refresh;
        }

        // Para montar em código (gerador de cena ou teste): pontos em coordenadas do mundo.
        public void Setup(Vector2 from, Vector2 to)
        {
            if (link == null) link = GetComponent<NavMeshLink>();
            // Os setters do NavMeshLink recriam o link mesmo com o componente desligado.
            // Liga durante a troca e volta ao estado de antes, para o OnDisable remover o link se preciso.
            bool was = link.enabled;
            link.enabled = true;
            link.startPoint = transform.InverseTransformPoint(from);
            link.endPoint = transform.InverseTransformPoint(to);
            link.enabled = was;
        }

        private void Refresh(MonsterBehaviour behaviours)
        {
            bool on = (behaviours & MonsterBehaviour.Shortcuts) != 0;
            if (link.enabled != on) link.enabled = on;
        }

        private void OnDrawGizmos()
        {
            var l = link != null ? link : GetComponent<NavMeshLink>();
            if (l == null) return;
            Gizmos.color = new Color(0.8f, 0.3f, 1f, 0.9f);
            Gizmos.DrawLine(transform.TransformPoint(l.startPoint), transform.TransformPoint(l.endPoint));
        }
    }
}
