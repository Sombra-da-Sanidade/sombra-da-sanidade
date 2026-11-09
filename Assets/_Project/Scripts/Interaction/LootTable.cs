using UnityEngine;

namespace SombraDaSanidade.Interaction
{
    // Tabela de loot dos armários de uma ala (extra #46, pesquisa: Left 4 Dead, "structured unpredictability"
    // em docs/pesquisa/ritmo-e-tensao.md). As possibilidades são desenhadas aqui e sorteadas em tempo de execução.
    // Um asset por ala em ScriptableObjects/Config/Loot/. Pesos relativos: não precisam somar 1.
    // A chance de bateria da ala está no DifficultyProfile (#30, batteryChance); o teste da ala confere que a tabela bate.
    [CreateAssetMenu(fileName = "LootTable", menuName = "Sombra da Sanidade/Config/Tabela de loot")]
    public class LootTable : ScriptableObject
    {
        [Tooltip("Peso de armário vazio.")]
        [Min(0f)] public float nothingWeight = 0.4f;
        [Tooltip("Peso de bateria sobressalente (+30). Proposta 1 do GDD v1.1: 70/60/45/0% por ala.")]
        [Min(0f)] public float batteryWeight = 0.3f;
        [Tooltip("Peso de sedativo (efeito do Estabilizador de Lítio, GDD 6).")]
        [Min(0f)] public float sedativeWeight = 0.3f;
        [Tooltip("Peso de Módulo Adrena-Syn (GDD 6). Não está no GDD: ajustar no playtest.")]
        [Min(0f)] public float adrenaSynWeight = 0f;
        [Tooltip("Garante pelo menos uma bateria por ala: o último armário fechado da ala dá bateria se nenhuma saiu.")]
        public bool guaranteeBattery = true;

        public float Total => nothingWeight + batteryWeight + sedativeWeight + adrenaSynWeight;

        // Chance absoluta (0 a 1) de cada item.
        public float Chance(LootKind kind)
        {
            float total = Total;
            if (total <= 0f) return kind == LootKind.Nothing ? 1f : 0f;
            switch (kind)
            {
                case LootKind.Battery: return batteryWeight / total;
                case LootKind.Sedative: return sedativeWeight / total;
                case LootKind.AdrenaSyn: return adrenaSynWeight / total;
                default: return nothingWeight / total;
            }
        }
    }
}
