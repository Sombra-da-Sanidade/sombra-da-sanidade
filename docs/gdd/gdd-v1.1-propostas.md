# GDD v1.1: propostas para o feedback do professor

**Status:** proposta, aguardando aprovação do grupo.

## O feedback

> "Jogo casual interessante com mecânica de perseguição/medo com inspirações muito boas [...]. Senti falta somente da noção de progresso, com dificuldade gradativa e sistema de recompensas mais claro."

O GDD v1.0 já tem a seção 5 (fases) e 6.4 (recompensas), mas:

- A dificuldade só cresce por um eixo (monstro +10% por HD).
- A principal "recompensa" de pegar um HD é uma punição (monstro mais rápido). Falta algo que o jogador **ganhe** e sinta.
- Não há retorno de desempenho no fim da partida além das conquistas.

## Proposta 1: tabela de dificuldade por ala

Vários eixos sobem juntos, de forma visível e previsível. Valores iniciais para playtest:

| Parâmetro | Ala Médica | Ala de Energia | Ala Executiva | Eclusa |
|---|---|---|---|---|
| Velocidade do monstro | 3.5 | 3.85 (+10%) | 4.2 (+20%) | 4.6 (fúria) |
| Raio auditivo do monstro | 5 | 6 | 7 | 9 |
| Dreno de sanidade no escuro (/s) | 1.0 | 1.5 | 2.0 | 2.0 |
| Chance de bateria por armário | 70% | 60% | 45% | 0% |
| Intervalo de alucinação (s) | não ocorre | 20-30 | 15-25 | 10-15 |
| Disjuntores disponíveis | 3 | 3 | 2 | 0 |
| Monstro presente desde | 1 min após entrar | entrada | entrada | entrada |

Implementação: um `DifficultyProfile` (ScriptableObject) por ala; o `GameManager` troca o perfil ao entrar na ala. Os sistemas leem do perfil ativo.

## Proposta 2: recompensa concreta por ala concluída

Ao ejetar cada HD, o jogador escolhe **1 de 2 upgrades** da Lux-90 (no terminal do servidor, tela diegética CRT verde). A escolha é a recompensa e cria decisão.

| Após | Opção A | Opção B |
|---|---|---|
| HD-01 (Médica) | **Lente Focal:** alcance +25% | **Célula Extra:** bateria máx. 130 |
| HD-02 (Energia) | **Capacitor Duplo:** EMP custa 30 em vez de 40 | **Bobina Longa:** stun 5.5s em vez de 4s |
| HD-03 (Executiva) | **Filtro Âmbar:** cone a 50% de intensidade não atrai o monstro (modo econômico) | **Ressonador Neural:** pistas de loucura aparecem com sanidade < 50 em vez de < 35 |

Outras recompensas:

- **Checkpoint** ao concluir a ala (já previsto no GDD, passa a ser comunicado como recompensa: "Progresso salvo").
- **Log de áudio** da Dra. Vance (narrativa como prêmio).
- **Mapa da ala seguinte** revelado no terminal.

Isso deixa o dente-de-serra explícito: HD = pico de perigo **e** ganho de poder; o jogador sente que fica mais forte enquanto o mundo fica mais hostil.

## Proposta 3: retorno de desempenho no fim

Tela de vitória com **avaliação do auditor** (rank S, A, B, C):

| Critério | Peso |
|---|---|
| Tempo total | 30% |
| Sanidade média | 30% |
| EMPs usados (menos é melhor) | 20% |
| Mortes | 20% |

Mais as 4 conquistas do GDD v1.0 (Mente Inabalável, Engenheiro Cego, Verdade Corporativa, À Beira do Abismo). Rank e conquistas ficam salvos em PlayerPrefs para incentivar rejogar.

## Proposta 4: introdução gradual de mecânicas

Já existe na seção 5 do GDD. Tornar explícito o "ensinar antes de cobrar":

- Ala Médica começa **sem monstro** por ~1 min: o jogador aprende lanterna e bateria em segurança. O primeiro encontro é roteirizado (o monstro passa ao fundo e reage à luz).
- Ala de Energia começa com um disjuntor ao lado da entrada: primeiro reparo sem pressão.
- Ala Executiva tem uma parede com pista escrita em fósforo perto da entrada, visível só com sanidade baixa, como dica do mecanismo.

## Como aplicar

1. Grupo discute e aprova (ou ajusta) cada proposta.
2. Atualizar o GDD no Google Docs (seções 5, 6.4 e 7) e gerar o PDF v1.1.
3. Atualizar `docs/gdd/gdd.md` e o histórico.
4. Abrir issues: `DifficultyProfile` por ala, tela de upgrade, tela de rank.

Escopo: Propostas 1 e 4 são baratas (dados e level design). Proposta 2 é média (UI + 6 efeitos simples). Proposta 3 é barata. Se faltar tempo, o upgrade pode virar fixo (sem escolha), mantendo a recompensa.
