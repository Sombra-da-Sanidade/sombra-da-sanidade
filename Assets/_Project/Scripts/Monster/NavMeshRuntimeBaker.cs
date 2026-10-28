using NavMeshPlus.Components;
using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Gera o NavMesh 2D ao carregar a cena. Assim a Frente 4 pode mexer nas paredes da Demo
    // sem precisar rebakear e commitar um asset de NavMesh (menos conflito de merge).
    // Start, não Awake: o CollectSources2d só se registra na superfície no Awake dele.
    // A ordem de execução baixa faz este Start rodar antes do Start do monstro.
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
