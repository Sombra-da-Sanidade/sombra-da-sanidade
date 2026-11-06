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

## Artes planejadas (ainda não ligadas ao código)

Fontes em `art-src/UI/`, PNG em `Assets/_Project/Art/UI/` (o `PixelArtImporter` fatia a categoria `UI`). Tudo segue as regras acima: pixel de jogo, só paleta, sem número na tela de jogo. Fora do jogo (menus, vitória) número pode aparecer.

| Folha | Célula | O que tem | Substitui | Frente |
|---|---|---|---|---|
| `skillcheck.json` | 48x32 | Multímetro analógico: `dial_body`, `dial_warn_0/1`, `dial_great_0/1`, `dial_fail_0..2`, `dial_hipnos` | `SkillCheckHud` (barra reta provisória) | 1, estilo da 3 |
| `hud_bars.json` | 16x8 | Barra de canalização `hold_0..8`, `hold_paused`, `hold_done`; teclas largas `key_space`, `key_tab`, `key_esc` | `%` do prompt do `PlayerInteractor` | 1 |
| `hud_icons.json` | 8x8 | Teclas (`key_e/f/q/r/1/2`, `mouse_l/r`), loot (`loot_*`), efeitos (`fx_adrena`, `fx_slow`, `fx_eco`), EMP (`emp_ready`, `emp_empty`), upgrades (`up_*`), `hd_full/empty`, `safe_zone` | Texto do `Locker` e do EMP; linha nova no relógio | 1 e 3 |
| `ui_font.json` | 6x8 | Fonte bitmap maiúscula com acentos pt-BR; `char` e `advance` por frame | `GUI.skin` padrão nas telas | 5 |
| `screens.json` | 16x16 | Kit CRT: `panel_*` (9-slice), `btn_*`, `cursor_*`, `slider_*`, `check_*`, selos de resultado, `rank_S/A/B/C`, `hd_slot_*` | `UpgradeTerminal`, telas do `GameManager`, menus novos | 5 (e 1 no terminal) |
| `logo.json` | 160x40 | `logo` e `logo_glitch` | (novo) menu inicial | 5 |

### Multímetro

Aparece ao lado do Thomas durante a ejeção do HD. O sprite é só a caixa e o mostrador; o código pinta por cima, lendo o bloco `dial` do JSON:

- faixa good em `d` e great em `e` nos pixels de `dial.arc` (cada um `[x, y, t]`, t de 0 a 1) entre `ZoneStart` e `ZoneStart + ZoneSize`;
- ponteiro em `8`: reta de `dial.pivot` até `dial.needleRadius` no ângulo `angles[0] + (angles[1] - angles[0]) * NeedlePosition`.

Estados: `Warning` alterna `dial_warn_0/1`; `Active` usa `dial_body` (ou `dial_hipnos` com sanidade < 35); `Result` usa `dial_great_*` ou `dial_fail_0..2`. A barra `hold_*` fica sob o Thomas e vira `hold_paused` durante a pausa do erro.

### Relógio

Ganha uma linha de ícones 8x8 embaixo (altura 46 → 56): efeitos ativos à esquerda (`fx_adrena` pisca nos últimos segundos), upgrades instalados à direita, e um LED âmbar do modo econômico ao lado do LED da Zona Segura. A marca do EMP ao lado da pilha pode virar `emp_ready`/`emp_empty`.

### Implementação sugerida

1. `refactor(ui)`: extrair paleta e primitivas do `DemoHud` (`Box`, `Fill`, `Fade`, `Hex`) para `UI/PixelHud.cs` e criar `UI/PixelFont.cs` (lê `ui_font` como máscara e recolore).
2. Uma issue por linha da tabela acima, cada uma na frente indicada.
