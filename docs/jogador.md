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
| Adrena-Syn no loot | 0% do loot por enquanto (não está no GDD) | `lockerAdrenaSynShare` |

Abrir faz barulho de propósito: saquear com o monstro por perto é um risco (Pilar 1).

Na cena, os armários são o caractere `A` das plantas em `art-src/Maps/` (`AlfaSceneBuilder`). No protótipo há dois no saguão.

## Módulo Adrena-Syn (#34, GDD 6 e Pilar 2)

Ampola laranja usada com **E** na hora (sem inventário). Também pode sair de um armário.

| O quê | Valor | Onde |
|---|---|---|
| Bônus de velocidade | +35% (GDD 6) | `PlayerConfig.adrenaSynSpeedBonus` |
| Duração | 15 s (GDD 6) | `PlayerConfig.adrenaSynDuration` |
| Custo | -20 de sanidade na hora (GDD 6) | `SanityConfig.adrenaSynCost` |

- O efeito (`AdrenaSynEffect`, no Player) multiplica e divide o `SpeedMultiplier`: convive com a lentidão do Estabilizador (1.35 × 0.6 = 0.81).
- Tomar de novo durante o efeito só renova os 15 s, não acumula.
- Acima de 1.05× o `PlayerSpriteAnimator` troca para `run_<dir>`.
- Sem trava de sanidade: com menos de 20, o módulo pode matar. É o risco do Pilar 2.
- Na planta: caractere `a`.

## Contratos para as outras frentes

| Frente | O que usar |
|---|---|
| 3 (HUD) | `Locker.Opened` (armário e `LootKind`) para som e feedback. O texto sobre o armário é provisório (`OnGUI`). |
| 4 (Níveis) | Coloque `A` (armário, encostado na parede) ou `a` (Adrena-Syn) na planta. O builder cria o objeto com o componente. |
| 3 (HUD) | `AdrenaSynEffect.IsActive` e `Remaining` para um efeito de tela durante o boost. |
| 5 (Dificuldade) | `Locker.RollLoot()` é o único ponto do sorteio: o `DifficultyProfile` entra aí. |

## Testes

| Teste | Menu (Sombra da Sanidade >) | Batchmode |
|---|---|---|
| Armários | Rodar teste dos armários | `SombraDaSanidade.EditorTools.LockerTest.RunBatch` |
| Adrena-Syn | Rodar teste do Adrena-Syn | `SombraDaSanidade.EditorTools.AdrenaSynTest.RunBatch` |
