using System;

namespace SombraDaSanidade.Monster
{
    // Comportamentos que o monstro ganha por ala (#36, Proposta 8 em docs/pesquisa/propostas.md).
    // Como no Alien: Isolation, destravam por progresso (ala), nunca por número de mortes do jogador.
    // O conjunto ativo vem do MonsterConfig.behaviours e pode ser trocado com MonsterController.SetBehaviours
    // (o DifficultyProfile de cada ala, #30, chama isso quando existir).
    // Próximos (#37): checar disjuntores ativos de longe e atalhos (Energia), memória curta de ruídos (Executiva).
    [Flags]
    public enum MonsterBehaviour
    {
        None = 0,
        // Ala Médica: depois de escutar no ponto do ruído (ou onde perdeu o jogador), checa alguns pontos
        // por perto em ordem subótima e às vezes volta a um já visitado. Parece procurar, não saber.
        Search = 1 << 0,
    }
}
