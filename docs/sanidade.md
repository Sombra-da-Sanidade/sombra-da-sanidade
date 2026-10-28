# Sanidade: o que já funciona

Frente 3 (Sanidade e HUD), issues #8 e #9.

Resumo do sistema de sanidade como ele está no código hoje, com os valores do GDD v1.0 e onde cada coisa mora. As seções citadas seguem a numeração do PDF ([GDD-v1.0.pdf](gdd/GDD-v1.0.pdf)); no [gdd.md](gdd/gdd.md) resumido os números mudam (ex.: "Controles e interface" é a 7 no PDF e a 8 no resumo). Os números ficam em `ScriptableObjects/Config/SanityConfig.asset`, então dá para ajustar o balanceamento sem mexer em código.

## Como a sanidade muda

| Situação | Efeito | GDD |
|---|---|---|
| Lanterna apagada e fora da Zona Segura | −1,5 por segundo | 4.4 regra 3, 6.3 |
| Monstro a até 5 m | −3 por segundo (soma com o escuro) | 6.3 |
| Dentro de uma Zona Segura | +2 por segundo até 100, sem nenhum dreno | 4.2, 6.3 |
| Estabilizador de Lítio | +30 na hora, mas Thomas fica lento | 4.3, 6.3 |
| Sanidade chega a 0 | Game over ("Sua mente colapsou no escuro") | 4.4 |

O raio de 5 m do monstro não está no GDD: é um chute para o playtest (`monsterProximityRadius`).

Estados (GDD 4.2): **Lúcido** 100–75, **Ansioso** 74–40, **Alucinando** 39–15, **Colapsado** 14–1, **Morto** 0.

## Efeitos por faixa de sanidade

O GDD (seção 7) pede que a sanidade seja comunicada na própria cena, não por números. Cada efeito entra numa faixa:

| Abaixo de | O que acontece | Script |
|---|---|---|
| 100 | Vinheta escura leve nas bordas | `SanityVignette` |
| 75 (Ansioso) | Batimento cardíaco começa (65 bpm, baixo) e acelera até 150 bpm perto do zero, mais alto. Aberração cromática começa a separar as cores. | `SanityHeartbeat`, `SanityVignette` |
| 50 | Vinheta começa a ficar vermelha | `SanityVignette` |
| 40 (Alucinando) | Vinheta e aberração pulsam; cada batida treme a câmera | `SanityVignette`, `SanityHeartbeat` |
| 35 | Pistas em fósforo aparecem; alucinações começam (uma a cada 15–25 s) | `SanityClue`, `SanityHallucination` |
| 15 (Colapso) | Pulso da vinheta fica mais rápido | `SanityVignette` |

### Alucinações (GDD 4.6)

Uma silhueta roxa escura parecida com o Espécime Vazio surge a 3–6 m do jogador, com linha de visão livre, avança devagar e some em cerca de 2 s. Ela aparece mesmo no escuro (material Unlit). Se o feixe da lanterna bater nela, ela se desfaz na hora: é o jeito de o jogador descobrir que não era real.

### Pistas em fósforo (GDD 1, 4.4 regra 5)

Qualquer objeto com `SanityClue` fica invisível com a sanidade alta e aparece, tremulando, abaixo de 35. Funciona com `SpriteRenderer` ou `TextMesh`, inclusive nos filhos. Os 3 textos da cena de protótipo ("FUSÍVEL 3-7-1", "FUSÍVEL 9-0-2", "ELE SEGUE A LUZ") são provisórios até a Frente 4 ter os puzzles.

### Estabilizador de Lítio (GDD 4.3)

Item azul-claro no chão, usado com **E** na hora (não há inventário). Com a sanidade cheia o prompt não aparece, para o item não ser desperdiçado. Dá +30 de sanidade e deixa o Thomas com 60% da velocidade por 4 s. O GDD só diz "lento por alguns segundos", então 60% e 4 s são valores de playtest (`stabilizerSlowMultiplier`, `stabilizerSlowDuration`).

## Arquivos

Todos em `Assets/_Project/Scripts/Sanity/`:

| Arquivo | Papel |
|---|---|
| `SanityConfig.cs` | Todos os números (ScriptableObject) |
| `SanityController.cs` | Valor, estados, dreno/regeneração, game over. Fica no Player. |
| `SanityVignette.cs` | Vinheta + aberração cromática. Cria o próprio Volume em runtime. |
| `SanityHeartbeat.cs` | Batimento e tremor da câmera |
| `SanityHallucination.cs` | Silhueta falsa |
| `SanityClue.cs` | Pista que só aparece com sanidade baixa |
| `LithiumStabilizer.cs` | Item Estabilizador de Lítio |

O menu **Sombra da Sanidade > Gerar cena de protótipo** (e as Sandboxes) já cria tudo isso na cena: efeitos, 2 estabilizadores e 3 pistas.

Se o `SanityConfig` estiver vazio no Player, o `SanityController` e todos os efeitos acima se desligam com um erro no Console, em vez de gerar erro a cada frame.

## Como testar

1. Gere a cena: **Sombra da Sanidade > Gerar cena de protótipo**.
2. Aperte Play e selecione o **Player** na Hierarchy.
3. No componente **SanityController**, clique no **⋮** e use **Debug: −25 de sanidade** (ou **+25** para voltar).
   - 2 cliques (50): batimento e aberração.
   - 3 cliques (25): tremor, pistas e, depois de 15–25 s, uma alucinação.
4. Ou jogue normal: apague a lanterna com **F** e espere.

O smoke test (**Sombra da Sanidade > Rodar smoke test**) confere que a sanidade drena no escuro, que os efeitos existem na cena e que as pistas começam escondidas.

## Para as outras frentes

- **Mudar a sanidade:** `SanityController.Add(valor)`. Positivo recupera, negativo drena. Ex.: Adrena-Syn = `Add(-sanity.Config.adrenaSynCost)`.
- **Zona Segura (Frente 4):** o trigger da lâmpada chama `SetInSafeZone(true)` ao entrar e `SetInSafeZone(false)` ao sair. Aceita zonas sobrepostas.
- **Pistas de puzzle (Frente 4):** coloque um `TextMesh` ou sprite com o componente `SanityClue`. Não precisa configurar nada.
- **Velocidade do jogador:** o Estabilizador multiplica e depois divide `PlayerController.SpeedMultiplier`, então dá para somar com o Adrena-Syn sem um apagar o outro.
- **Sincronizar com o batimento ou a alucinação:** eventos `SanityHeartbeat.Beat` e `SanityHallucination.Appeared(posição)`.

## O que ainda falta

| Item | GDD | Depende de |
|---|---|---|
| Adrena-Syn (−20 de sanidade, +35% de velocidade por 15 s) | 4.3, 6.2 | Frente 1 (jogador) |
| Monstro 5% mais lento para procurar o jogador abaixo de 15 de sanidade | 5 | Frente 2 (IA) |
| Zona Segura de verdade (trigger da lâmpada) | 4.4 regra 6 | Frente 4 (disjuntor) |
| Som real do batimento (hoje é sintético) | 9 | arte/áudio: arrastar o arquivo para o campo `Clip` |
| Conquista "Mente Inabalável" (vencer sempre acima de 70) | 6.4 | tela de vitória |
| Tirar a barra MENTE do HUD | 7 | decisão do grupo |

Sobre o último item: o GDD diz que a sanidade **não aparece como barra**, só pelos efeitos. A barra MENTE do HUD provisório ajuda a testar, mas pelo documento ela sai (ou vira modo debug) na versão final.
