# Dead by Daylight e Identity V

Os dois resolvem o mesmo problema que o nosso disjuntor/HD: o jogador precisa ficar **parado e exposto** fazendo uma tarefa longa enquanto o perseguidor anda pelo mapa. O skill check existe para que esse tempo parado não seja passivo.

## Dead by Daylight

Fonte principal: [wiki DbD, Skill Checks](https://deadbydaylight.wiki.gg/wiki/Skill_Checks).

### Gerador
- 90 cargas a 1 carga/s: ~90 s com um sobrevivente sozinho ([resumo de busca, wiki DbD](https://deadbydaylight.wiki.gg/wiki/Repairing)).
- Cada sobrevivente extra no mesmo gerador reduz a velocidade individual em 15% (era 10%) ([Developer Update abril 2020](https://forums.bhvr.com/dead-by-daylight/discussion/141304/developer-update-april-2020)).
- 5 geradores energizam as portas de saída.

### Skill check

| Interação | Chance por segundo | Zona good | Zona great | Ponteiro | Bônus great | Falha |
|---|---|---|---|---|---|---|
| Reparo de gerador | 8% | 13% | 3% | 1,1 s | +1% | -10% |
| Reparo com caixa de ferramentas | 40% | 13% | 3% | 1,1 s | +1% | -10% |
| Cura | 15% | 15% | 3% | 1,2 s | +3% | -10% |

- O jogo testa a chance **uma vez por segundo** durante a interação.
- Um som de aviso toca pouco antes do check aparecer. Isso é o que torna o check justo: o jogador sabe que vem.
- Falha: -10% de progresso, pausa de 3 s e notificação barulhenta para o killer.
- O bônus do great caiu de 2% para 1% no patch 3.7.0: o great é um prêmio pequeno, o castigo da falha é o que pesa.

### Variações que aumentam a dificuldade
- **Unnerving Presence:** dentro do terror radius do killer a zona good encolhe 40 a 60% e a chance de check sobe 10%. Pressão por proximidade.
- **Merciless Storm:** a partir de 90%, checks contínuos. Uma falha bloqueia o gerador por 16 a 20 s.
- **Madness (Doctor):** checks fora do centro ou invertidos. Distorção ligada a um estado mental (paralelo direto com a nossa sanidade).
- **Hyperfocus:** cada great dá um token que aumenta a velocidade do ponteiro e o bônus. Risco crescente para quem é bom.

### Terror radius
Fonte: [wiki DbD, Terror Radius](https://deadbydaylight.wiki.gg/wiki/Terror_Radius).
- Raio de 24 a 32 m em volta do killer, comunicado por batimento cardíaco.
- Três camadas de som por distância, mais uma quarta camada de música de perseguição.
- Existe opção de acessibilidade para mostrar o terror radius na tela ([PCGamesN](https://www.pcgamesn.com/dead-by-daylight/terror-radius-update-bhvr-horror-game)).

## Identity V

### Cifras
- 7 cifras no mapa, 5 decodificadas energizam os 2 portões. ~75 s por cifra, variando por personagem ([wiki Identity V, Exit Gates](https://id5.fandom.com/wiki/Exit_Gates); [resumo de busca](https://www.topuplive.com/news/identity-v-exit-gates-explained.html)).
- Depois de ~30% de progresso a máquina **treme visivelmente**, o que denuncia o sobrevivente para o caçador.
- Saída alternativa (masmorra) quando sobra um sobrevivente e pelo menos 2 cifras foram feitas. Dá esperança a quem está perdendo.

### Calibração (skill check)
- Falhar a calibração dispara um alarme e mostra ao caçador a posição exata da cifra ([guia de cifras](https://www.topuplive.com/news/identity-v-guide-to-cipher-machine.html)).
- Penalidade e bônus **escalam por rank** ([patch oficial de 19/05/2022](https://idv.163.com/en/news/announcement/20220519/35288_1018878.html)):

| Tier | Falha | Calibração perfeita |
|---|---|---|
| 1 e 2 | 0% | +2% |
| 3 e 4 | -1% | +2% |
| 5 e 6 | -1% | +1% |
| 7+ | -2% | +1% |

Isso é dificuldade gradativa explícita: o iniciante quase não é punido, o veterano ganha menos e perde mais.

## Lições para o nosso jogo

1. Tarefa longa + check aleatório + aviso sonoro = tempo parado que exige atenção.
2. O castigo da falha é **informação para o perseguidor** (barulho), não só progresso perdido.
3. O check fica mais difícil com a proximidade do perseguidor e com estados mentais. Para nós: monstro perto e sanidade baixa.
4. Escalar a punição com o progresso do jogador (tiers do IdV) é uma forma barata de dificuldade gradativa.
5. Tarefa que "denuncia" depois de um ponto (máquina tremendo) cria a decisão de parar ou arriscar.
