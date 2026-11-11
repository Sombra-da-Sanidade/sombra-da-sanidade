using System;

namespace SombraDaSanidade.Monster
{
    // Comportamentos que o monstro ganha por ala (#36 e #37, Proposta 8 em docs/pesquisa/propostas.md).
    // Como no Alien: Isolation, destravam por progresso (ala), nunca por número de mortes do jogador.
    // O conjunto ativo vem do DifficultyProfile da ala (#30) ou, sem perfil, do MonsterConfig.behaviours.
    // Também pode ser trocado com MonsterController.SetBehaviours.
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

    // Conjuntos por ala, os mesmos dos assets de DifficultyProfile (#30). Cada ala soma os comportamentos da anterior.
    public static class MonsterWingBehaviours
    {
        public const MonsterBehaviour Medica = MonsterBehaviour.Search;
        public const MonsterBehaviour Energia = Medica | MonsterBehaviour.BreakerCheck | MonsterBehaviour.Shortcuts;
        public const MonsterBehaviour Executiva = Energia | MonsterBehaviour.NoiseMemory;
    }
}
