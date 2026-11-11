# Monstro: o que já funciona

Frente 2 (Monstro), issues #4 e #5.

Resumo do Espécime Vazio como ele está no código hoje. Os números ficam em `ScriptableObjects/Config/MonsterConfig.asset`, então dá para ajustar o balanceamento sem mexer em código. Os valores marcados como "não está no GDD" são chutes para o playtest.

## Estados (GDD 4.2)

| Estado | Como entra | O que faz | Como sai |
|---|---|---|---|
| **Patrulha Cega** | Início da partida, ou depois de investigar | Anda entre os pontos de patrulha, pausando 1 s em cada um | Ruído ouvido → Investigação. Luz → Perseguição |
| **Investigação Auditiva** | Ouviu um ruído (`NoiseBus`) | Vai até o ponto do ruído e fica 3 s parado escutando | Termina de escutar → Patrulha. Luz → Perseguição |
| **Perseguição Furiosa** | O cone da lanterna tocou nele | Grita parado por 0,8 s, dá o bote e galopa atrás do jogador a 1,25× a velocidade (~4,4 m/s, mais rápido que os 4 m/s do jogador) | Sem luz por mais de 1,5 s: vai até onde viu o jogador por último e escuta (Investigação) |
| **Atordoado** | Pulso EMP (`Stun`) | Fica parado 4 s e não mata ao toque | Se estava caçando, procura onde o jogador estava; senão volta à Patrulha |

Ele é cego: só acha o jogador pela luz ou pelo som. **Andar no escuro não faz barulho**, então apagar a lanterna é a forma de escapar.

## Regras do GDD e onde estão

| Regra | Como funciona | GDD |
|---|---|---|
| Luz atrai | `FlashlightController.IsPointInCone` testado nos pés, no tronco e na cabeça (o sprite tem ~3 m). Lanterna apagada não atrai, e parede bloqueia o feixe. | regra 2 |
| Ruído | Ouve qualquer ruído dentro do raio do emissor ou a até 6 m (`hearingRadius`), e vai investigar. Durante a perseguição, ignora ruídos. | 4.3 |
| 70% / 30% | Em cada ponto de patrulha: se houver um ruído recente (até 20 s) ainda não investigado, 70% de chance de ir até ele; senão, sorteia outro ponto. | 4.6 |
| +10% por HD | `GameManager.DriveCollected` → `SetSpeedMultiplier`. 3,5 → 3,85 → 4,2 → 4,55 m/s (teto de 4,8). | regra 4 |
| Toque mata | Trigger nos pés (raio 0,5) → evento `Caught` → `MonsterKillScene` toca uma das duas mortes (~3,5 s) → `TriggerGameOver(DeathCause.Monster)`. Atordoado não mata. | 4.4 |
| Zona Segura | Nunca entra numa área com `MonsterRepellent` ligado. Se a luz acende em cima dele, sai pela borda. Na perseguição, espera na borda. | regra 6 |
| Sanidade < 15 | Grito, escuta e pausas ficam 5% mais longos. | 5 |

Não estão no GDD (ajustar no playtest): grito de 0,8 s (`detectionDelay`), memória de 1,5 s depois que a luz sai dele (`chaseMemory`), escuta de 3 s (`listenDuration`), pausa de 1 s por ponto (`waypointPause`), memória de ruído de 20 s (`noiseMemory`), galope de 1,25× com teto de 5,6 m/s (`chaseSpeedMultiplier`, `chaseMaxSpeed`). Como ele galopa mais rápido que o jogador, fugir correndo não basta: é preciso apagar a luz, quebrar a linha de visão ou usar o EMP.

## Contratos para as outras frentes

| Frente | O que usar |
|---|---|
| 1 (EMP) | Consulte `monster.IsLitByFlashlight()` antes de gastar carga: usa as mesmas alturas de `lightSampleHeights`, alcance e paredes da detecção (sem alturas configuradas, testa os pés). Se acertar, chame `monster.Stun(config.empStunDuration)`. Depois `NoiseBus.Emit(...)`: atordoado, ele não reage ao ruído do próprio EMP. |
| 3 (Sanidade) | `transform.position` para a proximidade (já usado). `State`, `IsDetecting`, `StateChanged` e `Detected` para efeitos e áudio. |
| 4 (Zona Segura) | Adicione `MonsterRepellent` no objeto da lâmpada de teto e ajuste `Radius` para o alcance da luz. Ligue o componente (`enabled = true`) quando o reparo terminar e desligue depois de 60 s. |
| 5 (HDs) | Nada a fazer: o monstro já escuta `DriveCollected`. |

## NavMesh 2D (NavMeshPlus, ADR 0003)

- A cena tem um objeto `NavMesh` (rotação X −90) com `NavMeshSurface` (geometria Render Meshes), `CollectSources2d` e `NavMeshRuntimeBaker`.
- **Só entra no NavMesh o que tem `NavMeshModifier`**: o chão (andável) e cada parede (área *Not Walkable*). Parede nova na Demo precisa do modifier, e o `CreateWall` do builder já adiciona.
- O bake acontece ao carregar a cena. Não há asset de NavMesh para commitar ou rebakear depois de mexer no mapa.

## Arte e animação

- Folhas em `Art/Monster/`: `EspecimeB_sheet.png` (48×64 por quadro, 16 px/unidade, 12 colunas, uma linha por animação; as linhas de 8 quadros são completadas com quadros vazios `*_pad*`) e `EspecimeB_sheet_glow.png` (LEDs, brasa e faíscas, na mesma grade). Design: Variante B do canvas de design do monstro, reduzida da versão 96×128 para a densidade de pixel dos tiles e para a paleta do projeto (rampa Espécime).
- A fonte é texto: `art-src/Monster/EspecimeB_sheet*.json`. Edite lá (ou no Estúdio) e rode `python3 tools/art/export_sprites.py`; não edite o PNG direto. Ver [guia de estilo](arte/guia-de-estilo.md).
- `MonsterSpriteAnimator` escolhe a animação pelo estado e a direção pela velocidade: `idle`, `walk_*`, `listen`, `scream`, `lunge` (o bote logo depois do grito), `gallop_*` (12 quadros, na perseguição) e `stun`. `run_*` ficou como reserva, para prefab antigo sem as linhas de galope. O lado esquerdo usa flipX.
- Mortes: `EspecimeB_kills.png` e `_glow` (80×80, 16 colunas, quadros 0–31 = mordida, 32–63 = partir ao meio). São o monstro e o Thomas juntos: o `MonsterKillScene` esconde os dois sprites e desenha a cena no lugar, espelhada para o lado do jogador. A morte é sorteada. No modo seguro, não toca.
- O corpo usa Sprite-Lit (some no escuro). A camada de brilho usa Sprite-Unlit, e há uma Light2D pequena nos eletrodos que muda de cor com o estado. É isso que deixa ver a "silhueta" de longe no escuro.
- Para trocar a arte (por exemplo, voltar para a Variante A), gere o JSON em `art-src/Monster/`, exporte, e ajuste `frameSize` no componente e a tabela de animações.

## Comportamentos por ala (#36 e #37)

O monstro ganha comportamentos por ala, como no Alien: Isolation. Eles destravam por progresso e **nunca** por número de mortes do jogador (Proposta 8).

- **Lista:** `MonsterBehaviour`, em flags. O conjunto padrão fica em `MonsterConfig.behaviours`, e o padrão é o da Ala Médica.
- **Troca em jogo:** `MonsterController.SetBehaviours(...)` (a Beta usa ao trocar de ala). Nas cenas de ala, o `DifficultyProfile` (#30) já define `monsterBehaviours` na cópia do config. `Has(...)` diz se um comportamento está ligado.

| Ala | Preset (`MonsterWingBehaviours`) | Comportamentos |
|---|---|---|
| Médica | `Medica` | Patrulha e investigação de ruído (como antes) + **Busca** |
| Energia | `Energia` | + **Checar disjuntores** (`BreakerCheck`) + **Atalhos** (`Shortcuts`) |
| Executiva | `Executiva` | + **Memória curta** (`NoiseMemory`) |

Cada ala soma os comportamentos da anterior. O asset `Difficulty_<ala>` guarda o preset da ala (`DifficultyProfiles` no Editor cria os quatro).

**Busca (`Search`):** depois de escutar no ponto do ruído, ou onde perdeu o jogador, ele checa até 3 pontos de patrulha a até 10 m dali antes de voltar à rota.
- **Ordem subótima:** sorteia entre os 3 pontos mais próximos ainda não vistos, em vez de ir sempre ao mais perto.
- **Retorno:** às vezes (25%) volta a um ponto que já checou.
- **Escuta mais curta:** em cada ponto, escuta pela metade do tempo normal.
- **Recuo do diretor:** durante o Relax do diretor de tensão, ele não faz a busca. Escuta e vai embora.

Números em `MonsterConfig` (`searchPoints`, `searchRadius`, `searchCandidates`, `searchRevisitChance`, `searchListenFactor`).

**Checar disjuntores (`BreakerCheck`, Energia, #37):** em cada nó de decisão da patrulha, há 50% de chance de ele ir olhar uma Zona Segura acesa (a lâmpada de um disjuntor reparado).
- **De longe:** para 2 m além da borda da luz, escuta e segue. Nunca entra.
- **Sem trapaça:** ele não sabe se o jogador está lá. A luz é só um sinal de que alguém consertou algo.
- **Sem insistir:** não volta à mesma zona antes de 20 s.

**Atalhos (`Shortcuts`, Energia, #37):** `MonsterShortcut` é um `NavMeshLink` entre dois pontos de chão, por exemplo atravessando um duto na parede. Como o jogador não usa NavMesh, só o monstro passa. O link só fica ligado quando a ala dá esse comportamento.
- **Frente 4:** para colocar no mapa da Ala de Energia, crie um objeto com `NavMeshLink` + `MonsterShortcut` e posicione Start Point e End Point no chão dos dois lados. Um tile de grade (`floor_grate`) nas pontas ajuda o jogador a entender por onde ele aparece.

**Memória curta (`NoiseMemory`, Executiva, #37):** ele lembra os últimos 4 ruídos por 60 s. Em cada nó de decisão, há 30% de chance de voltar a um ruído antigo, e não só ao último (que já tem a regra dos 70%).

Números em `MonsterConfig` (`breakerCheck*`, `noiseMemory*`).

**Perfil por ala (#30):** cada cena de ala aponta um `Core/DifficultyProfile` no `GameManager` (assets em `ScriptableObjects/Config/Difficulty<Ala>.asset`, valores da Proposta 1 do v1.1). Detalhes em [dificuldade.md](dificuldade.md).
- O monstro lê velocidade, raio auditivo e comportamentos de `DifficultyProfile.Active`. Os assets de config não mudam.
- Com perfil, o bônus de +10% por HD não vale: a velocidade da tabela já inclui o aumento de ala em ala.
- Energia e Executiva usam o perfil. Ligar a Médica (raio 5) muda o balanceamento dela e fica para quando ela for revista.

## Fúria da Eclusa (#44)

Quando o terminal da eclusa recebe os 3 HDs, a purga (`Level/EscapeSequence`) chama `MonsterController.SetFury(true)`:

- **Velocidade:** `MonsterConfig.furySpeed` (4.6, coluna Eclusa da Proposta 1) no lugar da base + bônus dos HDs. O galope da Perseguição e o modo seguro continuam multiplicando.
- **Audição:** raio mínimo `furyHearingRadius` (9).
- **Comportamentos:** todos os da Executiva. O `BetaWingTracker` não troca mais os comportamentos enquanto ele estiver em fúria.
- O barulho da eclusa abrindo (raio 40 no `EscapeConfig`) o chama para o terminal.

Detalhes da fase em [eclusa.md](eclusa.md).

## Diretor de tensão (#35)

`Core/TensionDirector` é a curva dente-de-serra do GDD 7 em código. Base: o menace gauge do Alien: Isolation e o ciclo do Left 4 Dead ([pesquisa](pesquisa/ritmo-e-tensao.md), Proposta 7).

Ele mantém um medidor de 0 a 1:
- **Sobe com:** o monstro perto (até 7 m, com peso menor se houver parede no meio), o cone da lanterna tocando nele, a perseguição ativa e a falha de skill check.
- **Cai com:** o tempo, mas nunca enquanto ele persegue.

| Fase | O que acontece |
|---|---|
| Build Up | O medidor sobe e cai conforme o jogo |
| Sustain Peak | Ao chegar em 1, segura de 3 a 5 s |
| Peak Fade | Espera a perseguição acabar sozinha (não tira o monstro do meio da caçada) |
| Relax | 30 a 45 s com o monstro patrulhando os pontos mais longe do jogador; depois volta ao Build Up |

- **O monstro continua cego:** o diretor só dá a direção (`MonsterController.SetRetreat`). No Relax, ruído novo e luz ainda atraem o monstro; ele só deixa de voltar atrás de ruídos antigos (a regra dos 70%).
- **Nunca no HUD:** o medidor não aparece na tela (GDD 8). Quem quiser reagir a ele (batimento, música) lê `TensionDirector.Instance.Value` e `Phase`, ou o evento `PhaseChanged`.
- **Sem configurar a cena:** cena com monstro e sem diretor ganha um sozinho ao carregar. Os números vêm de `Config/TensionConfig.asset`, ligado no `MonsterConfig.tension`. Sem o asset, valem os padrões.
- **Frente 1 (#33):** chamar `TensionDirector.Instance?.ReportSkillCheckFailure()` quando o jogador errar o skill check.

Os números são iniciais para o playtest e podem mudar com o GDD v1.1 (#3).

## Arquivos

| Arquivo | Papel |
|---|---|
| `Scripts/Monster/MonsterConfig.cs` | Todos os números (ScriptableObject) |
| `Scripts/Monster/MonsterController.cs` | IA: estados, sentidos, NavMeshAgent, contato |
| `Scripts/Monster/MonsterRepellent.cs` | Área que o monstro não entra (Zona Segura) |
| `Scripts/Monster/MonsterSpriteAnimator.cs` | Animação por folha de sprites e LED |
| `Scripts/Monster/MonsterKillScene.cs` | Cena de morte antes do Game Over (criada pelo animador) |
| `Scripts/Monster/NavMeshRuntimeBaker.cs` | Bake do NavMesh ao carregar a cena |
| `Scripts/Editor/MonsterPrefabBuilder.cs` | Menu **Gerar prefab do Monstro** → `Prefabs/Monster.prefab` |
| `Scripts/Core/TensionDirector.cs`, `TensionConfig.cs` | Diretor de tensão (#35) e seus números |
| `Scripts/Monster/MonsterBehaviour.cs` | Lista de comportamentos por ala (#36) |
| `Scripts/Monster/MonsterShortcut.cs` | Atalho só do monstro (#37) |
| `Scripts/Editor/MonsterWingTest.cs` | Menu **Rodar teste das alas do monstro** |
| `Scripts/Editor/MonsterBehaviourTest.cs` | Menu **Rodar teste dos comportamentos do monstro** |
| `Scripts/Editor/TensionTest.cs` | Menu **Rodar teste da tensão** |
| `Scripts/Editor/MonsterTest.cs` | Menu **Rodar teste do monstro** |

O menu **Sombra da Sanidade > Gerar cena de protótipo** (e as Sandboxes) já cria o NavMesh, o monstro e 13 pontos de patrulha nos cruzamentos.

## Teste

**Sombra da Sanidade > Rodar teste do monstro** roda na cena de protótipo e confere:
- a patrulha;
- o ruído e a investigação;
- a lanterna apagada não atraindo;
- a luz iniciando a perseguição, com o grito antes;
- o EMP acertando só a cabeça iluminada com carga exata, o atordoamento antes do ruído e a volta depois dele;
- os avisos de carga insuficiente e lanterna desligada, sem ruído ou gasto nas tentativas bloqueadas;
- o +10% por HD;
- a saída da Zona Segura;
- o galope mais rápido que o jogador;
- a cena de morte e o Game Over ao encostar.

Em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.MonsterTest.RunBatch
```

O smoke test geral desliga o monstro, porque ele tem este teste próprio.

## Limitações conhecidas

- Se o caminho até o jogador cruza uma Zona Segura, ele para na borda em vez de dar a volta, porque o NavMesh não sabe da luz. Para a mini demo isso basta. Se incomodar, dá para trocar por `NavMeshObstacle` com *carve*.
- A IA não diferencia cruzamentos de outros pontos: a regra dos 70% roda em cada ponto de patrulha.
