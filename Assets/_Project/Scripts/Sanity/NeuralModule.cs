using SombraDaSanidade.Interaction;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.Sanity
{
    // Módulo Neural (GDD 7, Ala Executiva: "Módulos neurais, Revelação pela Loucura"). Pilar 2: tecnologia
    // experimental com custo psíquico. Conectar com E tira sanidade na hora e, por alguns segundos, mostra as
    // pistas em fósforo com qualquer sanidade. É o atalho para ler as senhas sem precisar enlouquecer no escuro.
    // Usado uma vez, no padrão do Adrena-Syn.
    public class NeuralModule : MonoBehaviour, IInteractable
    {
        [Tooltip("Sanidade do jogador. Vazio: procura uma na cena.")]
        [SerializeField] private SanityController sanity;

        public string Prompt => "Conectar módulo neural (-sanidade)";
        public bool CanInteract => !used && sanity != null && sanity.Config != null;
        public float HoldDuration => 0f;
        public bool Used => used;

        private bool used;

        private void Awake()
        {
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
        }

        public void BeginInteract() { }

        public void TickInteract(float progress) { }

        public void CancelInteract() { }

        public void CompleteInteract()
        {
            if (!CanInteract) return;
            used = true;
            Apply(sanity);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            foreach (var l in GetComponentsInChildren<Light2D>()) l.enabled = false;
        }

        // O custo pode levar a sanidade a 0: é o risco do pilar 2, sem trava (igual ao Adrena-Syn).
        public static void Apply(SanityController sanity)
        {
            if (sanity == null || sanity.Config == null) return;
            sanity.ForceRevealClues(sanity.Config.neuralModuleRevealSeconds);
            sanity.Add(-sanity.Config.neuralModuleCost);
        }
    }
}
