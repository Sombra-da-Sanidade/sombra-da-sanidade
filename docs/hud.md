# HUD diegético

Frente 3 (Sanidade, HUD e narrativa), issues #16 e #39. Código em `Assets/_Project/Scripts/UI/DemoHud.cs`, números em `ScriptableObjects/Config/HudConfig.asset`.

**Princípio:** a tela é só a cena. Cada informação mora num objeto do Thomas e aparece quando muda, depois some. O GDD 8 pede sanidade só por efeitos e nenhum timer ou posição do monstro em número; o HUD não mostra número nenhum.

## O que aparece e quando

| Elemento | Onde | Quando aparece |
|---|---|---|
| Pilha da Lux-90 (4 segmentos) | Ao lado do Thomas | Ao mudar de segmento, ligar/desligar ou recarregar; some em 2,5 s. Abaixo de 25% fica fixa, piscando em vermelho |
| Marca do EMP | Em cima da pilha | Azul se a carga paga um pulso, vermelha se não |
| Gavetas de HD (3) | Canto superior esquerdo | Por 4 s depois de coletar um HD; a nova pisca |
| Relógio de pulso | Sobe do canto inferior direito | Enquanto o jogador **segura Tab** |
| Legendas dos logs | Embaixo, no centro, sem moldura, digitadas letra a letra | Enquanto um log toca (ao coletar um HD ou num terminal). "— sinal perdido —" em vermelho com o monstro perto |
| Dicas de controle | Acima do Thomas | Uma vez por sessão, quando o controle passa a importar |

### Relógio (segurar Tab)

Consultar custa algo: enquanto olha o pulso, o Thomas anda a 55% da velocidade (`watchSlowMultiplier`). A tela do relógio mostra:

- **ECG** desenhado com as batidas reais do `SanityHeartbeat`. A cor segue a faixa do monstro (verde longe, âmbar em alerta, vermelho em perigo). O traço treme com a mente Ansiosa e fica sujo e roxo com a mente Alucinando. É a única leitura de sanidade, e sem número;
- as 3 gavetas de HD;
- a pilha com a marca do EMP;
- um LED âmbar quando o Thomas está sob a luz de uma Zona Segura.

### Dicas (uma vez por sessão)

| Tecla | Aparece quando | Some quando |
|---|---|---|
| F, lanterna | 1,5 s depois do início, se o jogador ainda não usou a lanterna | Usar F ou o clique esquerdo |
| Tab, relógio | A primeira vez que a mente sai de Lúcido ou o monstro entra na faixa de alerta | Abrir o relógio |
| Botão direito, EMP | A primeira vez na faixa de perigo com a lanterna acesa e carga para o EMP | Depois de 6 s |

O prompt de interação (E) continua sendo do `PlayerInteractor` (Frente 1).

## Barra MENTE (issue #16)

Fica só para depuração: **F3** alterna no editor e em builds de desenvolvimento, ou ligue `HudConfig.showSanityDebugBar`. Ela mostra também o estado, a faixa e os bpm do batimento, o que ajuda no balanceamento. Falta o grupo confirmar que ela sai do jogo.

## Desenho

Tudo é desenhado em "pixels de jogo": a tela é dividida pela base 320x180 e cada pixel vira um quadrado inteiro, como a Pixel Perfect Camera. Cores só da paleta (`art-src/palette.json`), com a regra do guia de arte: âmbar é luz, verde é objetivo ou funcionando, vermelho é perigo e azul é EMP. Não há molduras na tela de jogo.

## Para as outras frentes

- **Frente 1:** o relógio multiplica e depois divide `PlayerController.SpeedMultiplier`, igual ao Estabilizador. Os dois se somam sem um apagar o outro.
- **Frente 5:** `DemoHud.SetWatchHeld(bool)` abre o relógio por código (tutorial, cutscene). O HUD some sozinho fora de `GameState.Playing`.
- **Cenas existentes:** `CreateHud` passa o `HudConfig` e o `FlashlightConfig` ao HUD. Numa cena gerada antes disso, o HUD usa os valores padrão e não desenha a marca do EMP até a cena ser gerada de novo.
