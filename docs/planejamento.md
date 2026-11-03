# Planejamento: próximos passos depois da mini demo

**Estado em 03/11/2026.** A mini demo (29/10) entregou todos os Essenciais do MVP (issues #4 a #13). Próximo marco do GDD: **vertical slice com a Ala Médica (semana 8)**. Depois: **versão final com 3 alas e build WebGL (semana 12)**.

Todo item abaixo precisa reforçar o feedback do professor: **P** progresso, **D** dificuldade gradativa, **R** recompensa clara.

Fontes: [GDD resumido](gdd/gdd.md), [propostas v1.1](gdd/gdd-v1.1-propostas.md), [pesquisa](pesquisa/README.md), [skill check](skill-check-hd.md).

Divisão do trabalho em 4 frentes: [equipe.md](equipe.md). Board: [projeto no GitHub](https://github.com/orgs/Sombra-da-Sanidade/projects/1).

## Mapa de dependências

    #3 GDD v1.1 aprovado
     ├── #30 DifficultyProfile ──┬── #36 monstro por ala ── #37 Energia/Executiva
     │                           ├── #35 diretor de tensão
     │                           ├── #41 Ala Médica ── #42, #43 ── #44 Eclusa
     │                           └── #46 loot por tabela (extra)
     ├── #33 skill check do HD ── #49 bônus no rank (extra)
     ├── #31 upgrades da Lux-90
     └── #32 telas + rank ── #48 modo seguro (extra)
    Podem começar já: #29 armários, #38 batimento, #34 Adrena-Syn, #40 Ato 3

## Fase 0: decidir (esta semana)

| # | O quê | Quem | Tempo |
|---|---|---|---|
| 0.1 | Reunião para aprovar #3: Propostas 1 a 4 do v1.1 + Propostas 5 a 9 da pesquisa | grupo | 45 min |
| 0.2 | Atualizar `gdd.md` (seções 4, 5, 6, 7, 8) e o histórico com o que foi aprovado | qualquer pessoa | 1 h |
| 0.3 | Decidir #16 (barra MENTE no HUD) na mesma reunião | grupo | junto |
| 0.4 | Cada pessoa escolhe uma das 4 frentes e se atribui nas issues | todos | 10 min |

## Fase 1: base do vertical slice

| Item | Issue | Frente | P/D/R | Tempo | Depende de |
|---|---|---|---|---|---|
| Loot dos armários (bateria +30, sedativo) | #29 | 1 | R | 2 h | nada |
| Skill check de multímetro na ejeção de HD | #33 | 1 (+3 no visual) | D, R | 6 h | #3 |
| `DifficultyProfile` por ala | #30 | 4 (1, 2, 3 leem) | D | 3 h | #3 |
| Batimento em 3 faixas de distância | #38 | 3 | D | 2 h | nada |

Skill check, resumo (detalhes em [skill-check-hd.md](skill-check-hd.md)): ejeção de 12 s, 25%/s de chance após 2 s, máx. 3 checks, good 15/13/11% por HD, great 4% (+5%), falha -10% + pausa 1 s + ruído raio 12. Monstro perto: zona -40%. Sanidade < 35: ponteiro treme. Depois de 50% o servidor faz ruído baixo a cada 3 s.

## Fase 2: vertical slice (semana 8)

| Item | Issue | Frente | P/D/R | Tempo | Depende de |
|---|---|---|---|---|---|
| Diretor de tensão (pico e recuo de 30 a 45 s) | #35 | 2 | D, P | 5 h | #30 |
| Monstro destrava comportamentos por ala (Médica) | #36 | 2 | D, P | 3 h | #30 |
| Upgrade da Lux-90 ao ejetar HD (1 de 2) | #31 | 1 (+3 na tela) | R | 4 h | #3 |
| Menu, Briefing, Vitória com rank S/A/B/C | #32 | 3 | R, P | 4 h | #3 |
| Ala Médica completa: 1 min sem monstro, tileset, "Progresso salvo" | #41 | 4 | P, R | 1 dia | #29, #30 |

Entrega da semana 8: Ala Médica jogável do Menu até a tela de rank, com skill check, upgrade e diretor de tensão. Build WebGL publicada.

## Fase 3: versão final (semana 12)

| Item | Issue | Frente | P/D/R |
|---|---|---|---|
| Módulo Adrena-Syn | #34 | 1 | R |
| Comportamentos do monstro em Energia e Executiva | #37 | 2 | D |
| Logs de áudio da Dra. Vance | #39 | 3 | R, P |
| Reviravolta do Ato 3 | #40 | 3 + grupo | P |
| Ala de Energia | #42 | 4 | P, D |
| Ala Executiva | #43 | 4 | P, D |
| Fuga da Eclusa cronometrada | #44 | 4 | P, D |
| Checkpoint JSON entre alas | #45 | 4 | P |

## Extras (sem milestone, para quem terminar antes)

| Item | Issue | Frente | Base |
|---|---|---|---|
| Loot por tabela sorteada por ala | #46 | 1 | Left 4 Dead |
| Ajuste dinâmico leve da chance de bateria | #47 | 1 | Hamlet |
| Modo seguro opcional | #48 | 2 | SOMA |
| Bônus no rank por HD sem falha | #49 | 3 | Darkwood / Proposta 3 |

## Por frente

| Frente | Fase 1 | Fase 2 | Fase 3 | Extras |
|---|---|---|---|---|
| 1 Jogador, interação e itens | #29, #33 | #31 | #34 | #46, #47 |
| 2 Monstro e tensão | ler #30 | #35, #36 | #37 | #48 |
| 3 Sanidade, HUD e narrativa | #38, #16 | #32 | #39, #40 | #49 |
| 4 Níveis, partida e build | #30 | #41 | #42, #43, #44, #45 | |

## Como cada mudança entra

Branch `tipo/<issue>-descricao`, Conventional Commits, PR com `Closes #N`, teste na Sandbox da frente e `MenuItem` de teste no padrão de `BreakerTest.cs`. Números só em ScriptableObjects com tooltip.
