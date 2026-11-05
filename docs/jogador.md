# Jogador, interação e itens: o que já funciona

Frente 1 (Jogador, interação e itens).

Resumo dos sistemas da frente como estão no código. Os números ficam em `ScriptableObjects/Config/` (`InteractionConfig`, `PlayerConfig`, `FlashlightConfig`), então dá para ajustar o balanceamento sem mexer em código. Valores marcados como "não está no GDD" são chutes para o playtest.

## Armários (#29, GDD 6)

Segurar **E** por 1 s abre o armário. Cada armário abre uma vez só e fica mais escuro depois de saqueado.

| O quê | Valor | Onde |
|---|---|---|
| Chance de ter algo | 60% (GDD 6) | `InteractionConfig.lockerLootChance` |
| Bateria x sedativo | 50% / 50% do loot (não está no GDD) | `lockerBatteryShare` |
| Bateria sobressalente | +30 de carga (GDD 6) | `FlashlightConfig.batteryPickupAmount` |
| Sedativo | mesmo efeito do Estabilizador de Lítio: +30 de sanidade e lentidão | `SanityConfig` (stabilizer*) |
| Ruído ao abrir | raio 6 (não está no GDD) | `lockerNoiseRadius` |
| Tempo para abrir | 1 s (não está no GDD) | `lockerOpenDuration` |

Abrir faz barulho de propósito: saquear com o monstro por perto é um risco (Pilar 1).

Na cena, os armários são o caractere `A` das plantas em `art-src/Maps/` (`AlfaSceneBuilder`). No protótipo há dois no saguão.

## Contratos para as outras frentes

| Frente | O que usar |
|---|---|
| 3 (HUD) | `Locker.Opened` (armário e `LootKind`) para som e feedback. O texto sobre o armário é provisório (`OnGUI`). |
| 4 (Níveis) | Coloque `A` na planta, encostado na parede. O builder cria o armário com o `Locker`. |
| 5 (Dificuldade) | `Locker.RollLoot()` é o único ponto do sorteio: o `DifficultyProfile` entra aí. |

## Testes

Menu **Sombra da Sanidade > Rodar teste dos armários** (`LockerTest`). Batchmode: `-executeMethod SombraDaSanidade.EditorTools.LockerTest.RunBatch`.
