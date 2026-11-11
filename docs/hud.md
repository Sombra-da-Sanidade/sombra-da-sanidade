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

## Arte em pixel (art-src/UI)

Fontes em `art-src/UI/`, PNG em `Assets/_Project/Art/UI/`. O código lê tudo pelo `Resources/HudArt.asset` (nomes dos frames, fonte e arco do multímetro); depois de mudar uma folha, rode `export_sprites.py` e **Sombra da Sanidade > Arte > Atualizar HudArt**. Desenho e paleta ficam em `UI/PixelHud.cs` (`Box`, `Sprite`, `Text`, `Panel`, `Button`, `Slider`). Sem o HudArt, cada tela cai na versão de texto antiga ou não desenha.

| Folha | Célula | O que tem | Quem usa |
|---|---|---|---|
| `skillcheck.json` | 48x32 | Multímetro: `dial_body`, `dial_warn_0/1`, `dial_great_0/1`, `dial_fail_0..2`, `dial_hipnos` | `SkillCheckHud` |
| `hud_bars.json` | 16x8 | Barra `hold_0..8`, `hold_paused`, `hold_done`; teclas `key_space`, `key_tab`, `key_esc` | `PlayerInteractor`, menus |
| `hud_icons.json` | 8x8 | Teclas, loot, efeitos, EMP, upgrades, HD | `Locker`, `DemoHud`, `UpgradeTerminal`, `ResultScreen` |
| `ui_font.json` | 6x8 | Fonte maiúscula com acentos pt-BR (`char` e `advance` por frame) | `PixelHud.Text` |
| `screens.json` | 16x16 | Kit CRT: `panel_*` (9-slice), `btn_*`, `cursor_*`, `slider_*`, `check_*`, selos, `rank_*`, `hd_slot_*` | Terminal, pausa, menu, fim de partida |
| `logo.json` | 160x40 | `logo`, `logo_glitch` | `MainMenuScreen` |

### O que mudou na tela

| Antes | Agora |
|---|---|
| Barra reta do skill check | Multímetro ao lado do Thomas. Aviso: LED e borda piscando. Ativo: ponteiro varre o arco e a faixa verde (ponta clara = great). Great acende o mostrador; falha solta faísca azul. Sanidade baixa: mostrador roxo (`SkillCheck.IsShaky`) |
| "Abrir armário... 45%" | Tecla E + nome da ação; segurando, barra sob os pés do Thomas. Vermelha durante a pausa do erro |
| "Bateria +30" | Ícone do item sobe do armário com a seta verde; vazio mostra a gaveta vazia |
| "Carga insuficiente para EMP" | Raio cortado piscando ao lado da pilha (`FlashlightController.EmpFailed`) |
| Relógio sem efeitos | Linha nova (altura 56): Adrena-Syn (pisca nos últimos 3 s), lentidão do Estabilizador (`PlayerController.SlowedUntil`) e upgrades instalados; LED âmbar do modo econômico |
| Terminal de upgrade em caixa padrão | Terminal Aethelgard com dois cartões, ícone, nome e efeito; clique ou 1/2 |
| Fim de partida em texto | `ResultScreen`: vitória com gavetas, selo do rank e estatísticas (#32); game over com selo da causa. R ou o botão reiniciam |

### Pausa e configurações

**Esc** pausa em qualquer cena com `GameManager` (o `PauseMenu` se cria sozinho, sem editar cena): continuar, configurações, reiniciar e sair. Sair carrega a cena `Menu` se ela estiver no Build Profile, senão fecha o jogo. Configurações (`GameSettings`, PlayerPrefs): volume, brilho (Volume global próprio com exposição, 0,5 neutro) e tela cheia. Teclado: W/S, A/D, Enter, Esc; mouse também funciona.

### Menu inicial (Frente 5, #32)

`MainMenuScreen` tem logo, CONTINUAR (só com checkpoint), JOGAR (abre o briefing com os controles e INICIAR TURNO), CONFIGURAÇÕES, SAIR e o melhor rank. **Sombra da Sanidade > Criar cena de Menu** gera `Scenes/Main/Menu.unity`, que é a cena 0 do Build Profile e a primeira da build WebGL. Fluxo, estatísticas e rank: [telas-e-rank.md](telas-e-rank.md).

### Testes

**Sombra da Sanidade > Rodar teste do HUD em pixel art** (`HudArtTest`): texturas, todo frame usado pelo código, cobertura da fonte, arco do multímetro e pausa.

Ainda não ligados: `fx_eco` e `safe_zone` (o LED do relógio cumpre o papel), `emp_ready`.
