# Pesquisa de balanceamento

Notas para basear mecânicas novas e o balanceamento do Sombra da Sanidade. O foco é o feedback do professor: **progresso, dificuldade gradativa e recompensas claras**.

Aqui ficam só resumos próprios, números e links. As cópias das fontes (PDFs, páginas salvas) ficam em `docs/referencias/pesquisa/`, só local (repo público). A lista de origem está em `docs/referencias/pesquisa/FONTES.md`.

| Arquivo | Conteúdo |
|---|---|
| [dbd-identity-v.md](dbd-identity-v.md) | Reparo de gerador, skill check, cifras, terror radius |
| [ritmo-e-tensao.md](ritmo-e-tensao.md) | Alien: Isolation, Left 4 Dead (AI Director), Hamlet (dificuldade dinâmica) |
| [referencias-gdd.md](referencias-gdd.md) | Darkwood, Resident Evil 2, SOMA, Signalis, Yuppie Psycho |
| [propostas.md](propostas.md) | O que aplicar no nosso jogo, com números iniciais |
| [../skill-check-hd.md](../skill-check-hd.md) | Plano de implementação do skill check na ejeção de HD |
| [../planejamento.md](../planejamento.md) | Planejamento geral: fases, dependências e frentes |

## Cuidados com os números

- Valores de wikis de fãs mudam a cada patch. Usar como ordem de grandeza, não como verdade.
- DbD e Identity V são multiplayer (4 contra 1) e partidas de 10 a 15 min. Nosso jogo é solo, então os tempos precisam ser escalados (ver `propostas.md`).
- Número novo no jogo vai para um ScriptableObject em `Assets/_Project/ScriptableObjects/Config/`, com tooltip "não está no GDD: ajustar no playtest".
