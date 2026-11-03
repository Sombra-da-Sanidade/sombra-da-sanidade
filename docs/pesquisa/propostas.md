# Propostas a partir da pesquisa

**Status:** proposta, aguardando o grupo. Entram no GDD v1.1 (issue #3) como Propostas 5 em diante, ao lado das de [gdd-v1.1-propostas.md](../gdd/gdd-v1.1-propostas.md).

Cada proposta diz qual ponto do feedback do professor ela reforça: **P** (progresso), **D** (dificuldade gradativa), **R** (recompensa clara).

## Fazer primeiro

### 5. Skill check na ejeção de HD (D, R)
Base: DbD e Identity V ([dbd-identity-v.md](dbd-identity-v.md)). Plano de implementação: [skill-check-hd.md](../skill-check-hd.md).

DbD tem 90 s de gerador e checks de 8%/s. Nosso HD tem 12 s, então a frequência sobe para ter 2 ou 3 checks por HD:

| Parâmetro | DbD | Nosso valor inicial |
|---|---|---|
| Duração da tarefa | ~90 s | 12 s |
| Chance de check por segundo | 8% | 25%, a partir de 2 s, no máx. 3 por HD |
| Aviso antes do check | som curto | bipe, 0,6 s |
| Tempo do ponteiro | 1,1 s | 1,1 s |
| Zona good | 13% | 15% (HD-01), 13% (HD-02), 11% (HD-03) |
| Zona great | 3% | 4% |
| Bônus great | +1% | +5% |
| Falha | -10%, pausa 3 s, barulho | -10%, pausa 1 s, ruído raio 12 |

- Zona encolhe por HD coletado (tiers do Identity V): dificuldade gradativa visível.
- Monstro perto: zona -40% (Unnerving Presence).
- Sanidade < 35: ponteiro treme ou gira ao contrário (Madness do Doctor). Liga com a Revelação pela Loucura.
- Great maior que no DbD (+5%) porque é o prêmio visível de quem joga bem.

### 6. Servidor denuncia depois de 50% (D)
Base: cifra do Identity V treme depois de 30%.
- A partir de 50% de ejeção, o servidor emite ruído baixo a cada 3 s (raio 6, metade do ruído da falha).
- Cria a decisão de terminar rápido ou parar e voltar.

### 7. Diretor de tensão (D, P)
Base: menace gauge do Alien: Isolation e ciclo do Left 4 Dead ([ritmo-e-tensao.md](ritmo-e-tensao.md)).
- Componente separado do monstro (`Core/TensionDirector`). Sobe com distância curta ao monstro, cone tocando nele, perseguição ativa e falha de skill check.
- No pico, sustenta 3 a 5 s e depois manda o monstro para longe (ponto de patrulha mais distante do jogador) por 30 a 45 s.
- O monstro continua cego: o diretor só dá direção, quem acha o jogador são os sensores de luz e ruído.
- É o "dente-de-serra" do GDD 7 virando código.

### 8. Monstro destrava comportamentos por ala (D, P)
Base: behavior tree do Alien destravada por progresso, nunca por mortes.
- Ala Médica: patrulha e investigação de ruído.
- Ala de Energia: passa a checar disjuntores ativos de longe e usa atalhos.
- Ala Executiva: volta a pontos de ruído antigos (memória curta).
- Entra como campo do `DifficultyProfile` (#30).

### 9. Batimento em camadas (D)
Base: terror radius do DbD, passos do Mr. X.
- `SanityHeartbeat` já existe. Separar em 3 faixas de distância (ex.: 10, 6, 3 m) e uma camada de perseguição.
- Aviso justo: o jogador sempre sabe que o monstro está chegando, nunca a posição exata (regra do GDD 8).

## Depois

- **Loot com imprevisibilidade estruturada** (L4D): cada armário sorteia de uma tabela por ala, em vez de 60% fixo. Integra com #29.
- **Ajuste dinâmico leve** (Hamlet): se a bateria ficar abaixo de 20 por mais de 30 s, a próxima chance de bateria sobe 15 pontos. Sem avisar o jogador.
- **Modo seguro opcional** (SOMA): monstro mais lento e sem morte por contato, para acessibilidade.
- **Recompensa por trecho difícil** (Darkwood): ejetar HD sem falhar nenhum check dá bônus no rank final (Proposta 3).
