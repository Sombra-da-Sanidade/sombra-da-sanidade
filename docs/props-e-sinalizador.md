# Props das alas, mortos e sinalizador

Armários, disjuntor, gravações, mortos e mobília com arte nova, o sinalizador para segurar o Sonâmbulo e as luzes de alarme da purga.

Tudo é montado pelo `AlfaSceneBuilder` e pelo `AlfaSceneBuilder.Props`. A Beta, as alas, a Eclusa e a Alfa já foram geradas com estes props.

## Armários

**Distribuição:**
- **Na Beta:** cada armário vai para uma sala diferente (`BetaMapGenerator`).
- **Em qualquer cena:** ao montar, um armário colado em outro (até 2 tiles) ou fora de uma parede norte reta muda para o ponto livre mais perto (`SpreadLockers`). Esse ponto pode ser na parede norte ou numa lateral.
- **Nas laterais:** o armário fica de perfil (`locker_side_closed` e `locker_side_open`), e na parede direita ele é espelhado.

**Arte:**

- O sprite é um armário duplo em pé, com o tampo visível (`art-src/Props/lockers.json`, 28×40: `locker_tall_closed` e `locker_tall_open`).
- A base fica 0,3 u à frente do pé da parede (`PerspectiveFix.LockerDepth`), com uma sombra de contato no piso. Isso evita que ele pareça afundado no chão.
- Aberto, as portas ficam para os lados, e dentro aparecem uma prateleira, um cabide vazio e um jaleco caído.

## Disjuntor: luz da sala e queima

| O quê | Como funciona |
|---|---|
| **Painel** | É um sprite na faixa de baixo da parede (`breaker_panel.json`, 12×20), no lugar do tile `breaker_off`. Reparado, a alavanca sobe, o LED fica verde e os fusíveis acendem. |
| **Luz da sala** | Cada disjuntor tem arandelas (`RoomLamp`, `wall_lamp.json`) espalhadas pela parede norte da sala, uma a cada ~5 tiles, até 4. O reparo acende todas em sequência, piscando como fluorescente velha. Nos últimos 5 s elas falham, junto com a Zona Segura. |
| **Queima** | No fim dos 60 s, o painel pega fogo e solta faíscas por 1,6 s, e as arandelas estouram. Depois disso ele fica carbonizado e soltando fumaça, e **não pode ser reparado de novo**: cada disjuntor é uma proteção de uso único. O estouro faz ruído (raio 6). |
| **Zona Segura** | Não mudou: continua no mesmo lugar, com o mesmo raio. As arandelas são só a luz da sala. |

- A lâmpada de teto que aparecia "no chão" (tile `ceiling_lamp` nas células `L`) saiu.
- As arandelas evitam a parede atrás de armários, servidores, portas e terminais.
- Para voltar ao comportamento antigo, de reparar de novo, desmarque `Breaker.burnsOut`.

## Mobília das salas

`FurnishRooms` espalha móveis encostados nas paredes:
- **Parede norte:** de frente.
- **Paredes laterais:** de perfil, `*_side`, com a direita espelhada. O tanque, o soro e as caixas usam o mesmo desenho dos dois lados.

| Ala | Móveis |
|---|---|
| Médica | Maca com amarras, suporte de soro, estante de frascos, arquivo |
| Energia | Caixas empilhadas, arquivo, estante |
| Executiva | Tanque de espécime (bolhas e brilho verde), carrinho de EEG do Hipnos (ondas e brilho roxo), arquivo, estante |
| Outras | Mistura de tudo |

- **Entulho no chão:** cadeira caída, bandeja de instrumentos, vidro quebrado e, na Médica, cadeira de rodas.
- **Saguão (área de início):** fica mais limpo. Não tem manchas no chão (`BetaMapGenerator.Decorate`), entulho, mortos nem bancadas, e tem no máximo 2 móveis (`AlfaSceneBuilder.QuietArea`). Nas cenas sem saguão definido, vale um raio de 6 tiles em volta do início.
- **Regras gerais:**
  - Nada fica na frente de servidor, disjuntor, terminal, porta ou canos.
  - Nada se empilha: cada peça reserva as células em volta.
  - É tudo decoração, sem colisão.
  - O sorteio é fixo.
- **Arte:** `furniture.json` (de frente), `furniture_side.json` (perfil), `clutter.json` (chão) e `SpriteLoop` para as animações.

## Sirenes da purga

- **Onde ficam:** sirenes de parede (`siren.json`) na faixa de cima das paredes norte, de salas e corredores, a cada ~9 tiles.
- **Na purga:** quando os HDs entram no terminal da eclusa e a purga começa (`EscapeSequence.Started`), elas giram e pulsam em vermelho. As arandelas dos disjuntores também viram alarme (`wall_lamp_alarm_*`), inclusive as apagadas ou queimadas, como num circuito de emergência.
- **Ritmo:** o pulso segue o ritmo da sirene e acelera até o fim da purga.
- **Código:** é o próprio `RoomLamp`, com `alarmFrames`.

## Gravações

- **O que são:** os logs avulsos (`AudioLogTerminal`) agora são um gravador de voz digital, numa mesa ou na mão de um pesquisador morto.
- **Na Beta:** os terminais alternam entre os dois tipos.
  - **Mesa** (`desk_recorder.json`, 32×26): fica encostada na parede, embaixo do terminal de parede.
  - **Corpo** (`corpses.json`, `corpse_recorder_*`): fica no chão, um pouco à frente.
- **Tocando:** a tela mostra a onda da voz, o LED pisca (`AudioLogTerminal.playingFrames`) e uma luz verde fraca marca onde ele está.
- **Na construção:** `PrototypeSceneBuilder.CreateAudioLog(..., onCorpse)`. Sem a arte nova, ele cai no terminal antigo.

## Mortos

- São feitos a partir do sprite do Thomas, com o mesmo traço do jogo: pesquisador de jaleco, segurança de farda e cobaia de avental. Cada um fica deitado numa poça de sangue, como o Thomas nas cenas de morte (`corpses.json`, 36×22).
- O `AlfaSceneBuilder` espalha de 2 a 7 por cena. Eles só aparecem em chão liso e aberto, longe do início e espaçados entre si. O sorteio é fixo, então gerar de novo dá o mesmo resultado.
- São só decoração: não têm colisão e ficam no chão, debaixo dos personagens.

## Sinalizador

| O quê | Valor | Onde |
|---|---|---|
| Jogar | **G**, na direção do mouse, até 5 m. Uma parede no caminho corta o voo. | `FlareThrower` (no jogador) |
| Começa com | 1 | `FlareThrower.count` |
| Máximo | 4 | `maxCount` |
| Aceso | 8 s, luz vermelha com raio de 4 m | `burnDuration`, `lightRadius` |
| Pegar mais | Segurar **E** numa **caixa de ferramentas em cima de uma bancada** (`FlarePickup`, `toolbox.json`): a caixa abre e fica vazia. De 1 a 3 bancadas por cena, e na Beta uma perto da sala do Sonâmbulo (`AlfaSceneBuilder.ToolboxNear`). | `CreateToolboxBench` |

- **Efeito no Sonâmbulo:** enquanto o sinalizador queima, o Sonâmbulo dentro da luz fica parado, mesmo com a lanterna apagada e o Thomas de costas. É o tempo de reação para fugir ou lidar com o Espécime.
- **Efeito no Espécime:** nenhum. O sinalizador não acorda o Sonâmbulo nem atrai o Espécime (`SignalFlare.Illuminates`).
- **HUD:** o contador aparece embaixo à direita, ao lado do relógio, enquanto houver sinalizador.

## Arte

Ficam em `art-src/Props/` e são exportados com `python tools/art/export_sprites.py`:

| Arquivo | Conteúdo |
|---|---|
| `lockers.json` | Armário fechado e aberto |
| `breaker_panel.json` | Painel desligado, ligado, queimado e os 4 quadros da queima |
| `wall_lamp.json` | Arandela apagada, acesa e queimada |
| `smoke.json` | 4 quadros de fumaça |
| `corpses.json` | Mortos e o pesquisador com gravador (parado e 4 quadros tocando) |
| `desk_recorder.json` | Mesa com gravador (parado e 4 quadros tocando) |
| `flare.json` | Sinalizador apagado, aceso (4 quadros) e queimado |
| `toolbox.json` | Bancada com a caixa de ferramentas fechada e aberta |
| `furniture.json`, `furniture_side.json`, `clutter.json` | Mobília de frente, de perfil e entulho |
| `siren.json` | Sirene apagada e girando (4 quadros) |

O `PrototypeSceneBuilder.LoadPropSprite` procura o nome em todas as folhas de `Art/Props`.

## Testes

- `BreakerTest`: o disjuntor queima no fim do tempo e não pode ser reparado de novo.
- `SleepwalkerTest`: o sinalizador jogado no Sonâmbulo o segura parado enquanto queima, e ele volta a andar quando a luz apaga.
- `BetaSceneTest`: confere disjuntores com painel e arandelas, mortos, caixas de ferramentas e o sinalizador no jogador, mobília (inclusive nas laterais), sirenes, armários espalhados e gravações na mesa e no corpo.
- `EclusaEscapeTest`: na purga, todas as arandelas e sirenes viram alarme.
