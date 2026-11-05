using UnityEngine;

namespace SombraDaSanidade.Flashlight
{
    public enum LuxUpgradeEffect
    {
        RangeMultiplier,    // Lente Focal: alcance × valor
        MaxBattery,         // Célula Extra: bateria máxima = valor
        EmpCost,            // Capacitor Duplo: EMP custa valor
        StunDuration,       // Bobina Longa: stun de valor segundos
        AmberFilter,        // Filtro Âmbar: modo econômico (tecla Q) que não atrai o monstro
        NeuralResonator     // Ressonador Neural: pistas aparecem abaixo de valor de sanidade
    }

    // Upgrade da Lux-90 escolhido ao ejetar um HD (Proposta 2 do GDD v1.1, pendente da issue #3).
    // Um asset por upgrade em ScriptableObjects/Config/Upgrades/. Frente 1. Issue #31.
    [CreateAssetMenu(fileName = "LuxUpgrade", menuName = "Sombra da Sanidade/Config/Upgrade da Lux-90")]
    public class LuxUpgrade : ScriptableObject
    {
        [Tooltip("Nome na tela do terminal.")]
        public string displayName = "Upgrade";
        [Tooltip("Uma linha explicando o efeito.")]
        [TextArea] public string description = "";
        public LuxUpgradeEffect effect;
        [Tooltip("Valor do efeito (ver o comentário de cada efeito em LuxUpgradeEffect).")]
        public float value = 1f;
    }
}
