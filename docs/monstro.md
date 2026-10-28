# Monstro: o que já funciona

Frente 2 (Monstro), issues #4 e #5.

Resumo do Espécime Vazio como ele está no código hoje. Os números ficam em `ScriptableObjects/Config/MonsterConfig.asset`, então dá para ajustar o balanceamento sem mexer em código. Os valores marcados como "não está no GDD" são chutes para o playtest.

## Estados (GDD 4.2)

| Estado | Como entra | O que faz | Como sai |
|---|---|---|---|
| **Patrulha Cega** | Início da partida, ou depois de investigar | Anda entre os pontos de patrulha, pausando 1 s em cada um | Ruído ouvido → Investigação. Luz → Perseguição |
| **Investigação Auditiva** | Ouviu um ruído (`NoiseBus`) | Vai até o ponto do ruído e fica 3 s parado escutando | Termina de escutar → Patrulha. Luz → Perseguição |
| **Perseguição Furiosa** | O cone da lanterna tocou nele | Grita parado por 0,8 s e depois corre atrás do jogador | Sem luz por mais de 1,5 s: vai até onde viu o jogador por último e escuta (Investigação) |
| **Atordoado** | Pulso EMP (`Stun`) | Fica parado 4 s e não mata ao toque | Se estava caçando, procura onde o jogador estava; senão volta à Patrulha |

Ele é cego: só acha o jogador pela luz ou pelo som. **Andar no escuro não faz barulho**, então apagar a lanterna é a forma de escapar.

## Regras do GDD e onde estão

| Regra | Como funciona | GDD |
|---|---|---|
| Luz atrai | `FlashlightController.IsPointInCone` testado nos pés, no tronco e na cabeça (o sprite tem ~3 m). Lanterna apagada não atrai, e parede bloqueia o feixe. | regra 2 |
| Ruído | Ouve qualquer ruído dentro do raio do emissor ou a até 6 m (`hearingRadius`), e vai investigar. Durante a perseguição, ignora ruídos. | 4.3 |
| 70% / 30% | Em cada ponto de patrulha: se houver um ruído recente (até 20 s) ainda não investigado, 70% de chance de ir até ele; senão, sorteia outro ponto. | 4.6 |
| +10% por HD | `GameManager.DriveCollected` → `SetSpeedMultiplier`. 3,5 → 3,85 → 4,2 → 4,55 m/s (teto de 4,8). | regra 4 |
| Toque mata | Trigger nos pés (raio 0,5) → `TriggerGameOver(DeathCause.Monster)`. Atordoado não mata. | 4.4 |
| Zona Segura | Nunca entra numa área com `MonsterRepellent` ligado. Se a luz acende em cima dele, sai pela borda. Na perseguição, espera na borda. | regra 6 |
| Sanidade < 15 | Grito, escuta e pausas ficam 5% mais longos. | 5 |

Não estão no GDD (ajustar no playtest): grito de 0,8 s (`detectionDelay`), memória de 1,5 s depois que a luz sai dele (`chaseMemory`), escuta de 3 s (`listenDuration`), pausa de 1 s por ponto (`waypointPause`), memória de ruído de 20 s (`noiseMemory`).

## Contratos para as outras frentes

| Frente | O que usar |
|---|---|
| 1 (EMP) | `monster.Stun(config.empStunDuration)` quando `IsPointInCone(monster.transform.position)`. Depois `NoiseBus.Emit(...)`: atordoado, ele não reage ao ruído do próprio EMP. |
| 3 (Sanidade) | `transform.position` para a proximidade (já usado). `State`, `IsDetecting`, `StateChanged` e `Detected` para efeitos e áudio. |
| 4 (Zona Segura) | Adicione `MonsterRepellent` no objeto da lâmpada de teto e ajuste `Radius` para o alcance da luz. Ligue o componente (`enabled = true`) quando o reparo terminar e desligue depois de 60 s. |
| 5 (HDs) | Nada a fazer: o monstro já escuta `DriveCollected`. |

## NavMesh 2D (NavMeshPlus, ADR 0003)

- A cena tem um objeto `NavMesh` (rotação X −90) com `NavMeshSurface` (geometria Render Meshes), `CollectSources2d` e `NavMeshRuntimeBaker`.
- **Só entra no NavMesh o que tem `NavMeshModifier`**: o chão (andável) e cada parede (área *Not Walkable*). Parede nova na Demo precisa do modifier, e o `CreateWall` do builder já adiciona.
- O bake acontece ao carregar a cena. Não há asset de NavMesh para commitar ou rebakear depois de mexer no mapa.

## Arte e animação

- Folhas em `Art/Monster/`: `EspecimeB_sheet.png` (48×64 por quadro, 16 px/unidade, 8 colunas, 10 animações) e `EspecimeB_sheet_glow.png` (LEDs, brasa e faíscas, na mesma grade). Design: Variante B do canvas de design do monstro, reduzida da versão 96×128 para a densidade de pixel dos tiles e para a paleta do projeto (rampa Espécime).
- A fonte é texto: `art-src/Monster/EspecimeB_sheet*.json`. Edite lá (ou no Estúdio) e rode `python3 tools/art/export_sprites.py`; não edite o PNG direto. Ver [guia de estilo](arte/guia-de-estilo.md).
- `MonsterSpriteAnimator` escolhe a animação pelo estado e a direção pela velocidade: `idle`, `walk_*`, `listen`, `scream`, `run_*` e `stun`. O lado esquerdo usa flipX.
- O corpo usa Sprite-Lit (some no escuro). A camada de brilho usa Sprite-Unlit, e há uma Light2D pequena nos eletrodos que muda de cor com o estado. É isso que deixa ver a "silhueta" de longe no escuro.
- Para trocar a arte (por exemplo, voltar para a Variante A), gere o JSON em `art-src/Monster/`, exporte, e ajuste `frameSize` no componente e a tabela de animações.

## Arquivos

| Arquivo | Papel |
|---|---|
| `Scripts/Monster/MonsterConfig.cs` | Todos os números (ScriptableObject) |
| `Scripts/Monster/MonsterController.cs` | IA: estados, sentidos, NavMeshAgent, contato |
| `Scripts/Monster/MonsterRepellent.cs` | Área que o monstro não entra (Zona Segura) |
| `Scripts/Monster/MonsterSpriteAnimator.cs` | Animação por folha de sprites e LED |
| `Scripts/Monster/NavMeshRuntimeBaker.cs` | Bake do NavMesh ao carregar a cena |
| `Scripts/Editor/MonsterPrefabBuilder.cs` | Menu **Gerar prefab do Monstro** → `Prefabs/Monster.prefab` |
| `Scripts/Editor/MonsterTest.cs` | Menu **Rodar teste do monstro** |

O menu **Sombra da Sanidade > Gerar cena de protótipo** (e as Sandboxes) já cria o NavMesh, o monstro e 13 pontos de patrulha nos cruzamentos.

## Teste

**Sombra da Sanidade > Rodar teste do monstro** roda na cena de protótipo e confere:
- a patrulha;
- o ruído e a investigação;
- a lanterna apagada não atraindo;
- a luz iniciando a perseguição, com o grito antes;
- o EMP e a volta depois dele;
- o +10% por HD;
- a saída da Zona Segura;
- o Game Over ao encostar.

Em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.MonsterTest.RunBatch
```

O smoke test geral desliga o monstro, porque ele tem este teste próprio.

## Limitações conhecidas

- Se o caminho até o jogador cruza uma Zona Segura, ele para na borda em vez de dar a volta, porque o NavMesh não sabe da luz. Para a mini demo isso basta. Se incomodar, dá para trocar por `NavMeshObstacle` com *carve*.
- A IA não diferencia cruzamentos de outros pontos: a regra dos 70% roda em cada ponto de patrulha.
