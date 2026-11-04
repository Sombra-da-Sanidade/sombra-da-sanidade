using System;

namespace SombraDaSanidade.Monster
{
    // Comportamentos que o monstro ganha por ala (#36 e #37, Proposta 8 em docs/pesquisa/propostas.md).
    // Como no Alien: Isolation, destravam por progresso (ala), nunca por número de mortes do jogador.
    // O conjunto ativo vem do MonsterConfig.behaviours e pode ser trocado com MonsterController.SetBehaviours
    // (o DifficultyProfile de cada ala, #30, chama isso com um dos presets de MonsterWingBehaviours).
    [Flags]
    public enum MonsterBehaviour
    {
        None = 0,
        // Ala Médica: depois de escutar no ponto do ruído (ou onde perdeu o jogador), checa alguns pontos
        // por perto em ordem subótima e às vezes volta a um já visitado. Parece procurar, não saber.
        Search = 1 << 0,
        // Ala de Energia: nos nós de decisão da patrulha vai olhar de longe uma Zona Segura acesa
        // (lâmpada de disjuntor). Para na borda da luz, escuta e segue. Nunca entra (GDD regra 6).
        BreakerCheck = 1 << 1,
        // Ala de Energia: usa os atalhos só dele (MonsterShortcut, ex.: dutos) que a Frente 4 coloca no mapa.
        Shortcuts = 1 << 2,
        // Ala Executiva: memória curta. Às vezes volta a um ruído antigo, não só ao último.
        NoiseMemory = 1 << 3,
    }

    // Conjuntos por ala, para o DifficultyProfile (#30) usar. Cada ala soma os comportamentos da anterior.
    public static class MonsterWingBehaviours
    {
        public const MonsterBehaviour Medica = MonsterBehaviour.Search;
        public const MonsterBehaviour Energia = Medica | MonsterBehaviour.BreakerCheck | MonsterBehaviour.Shortcuts;
        public const MonsterBehaviour Executiva = Energia | MonsterBehaviour.NoiseMemory;
    }
}
