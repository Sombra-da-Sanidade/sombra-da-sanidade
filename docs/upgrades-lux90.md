# Upgrades da Lux-90: o que já funciona

Frente 1, issue #31. Proposta 2 do [GDD v1.1](gdd/gdd-v1.1-propostas.md), ainda pendente da aprovação (#3).

Ao ejetar cada HD, abre a tela do terminal (CRT verde) com o jogo pausado. O jogador escolhe **1 de 2** upgrades com **1**/**2** ou clicando. A escolha é a recompensa concreta do HD: o mundo fica mais hostil (monstro +10%) e a lanterna fica mais forte.

| Depois de | Opção A | Opção B |
|---|---|---|
| HD-01 | **Lente Focal:** alcance +25% (8 → 10) | **Célula Extra:** bateria máx. 130 (a célula chega com +30) |
| HD-02 | **Capacitor Duplo:** EMP custa 30 | **Bobina Longa:** stun de 5,5 s |
| HD-03 | **Filtro Âmbar:** tecla **Q** liga o modo econômico (luz a 50%, não atrai o monstro, gasta metade) | **Ressonador Neural:** pistas de loucura com sanidade < 50 |

"Gasta metade" no modo econômico não está na proposta (`FlashlightConfig.ecoDrainMultiplier`, ajustar no playtest). O EMP continua acertando o monstro no modo econômico.

## Onde ajustar

- Cada upgrade é um asset em `ScriptableObjects/Config/Upgrades/` (`LuxUpgrade`: nome, descrição, efeito, valor).
- A ordem das escolhas fica em `LuxUpgradeTree.asset`, referenciado pelo `InteractionConfig.upgradeTree`. Sem árvore, não abre tela (vale como "upgrade desligado").
- Os efeitos são aplicados em tempo de execução (`FlashlightController.ApplyUpgrade`, `SanityController.SetRevealCluesBelow`). Os assets de config nunca são alterados.

## Contratos para as outras frentes

| Frente | O que usar |
|---|---|
| 2 (Monstro) | `MonsterController` só chama `OnLit` com `flashlight.AttractsMonster`. `IsLitByFlashlight` não mudou (EMP e tensão continuam iguais). |
| 3 (HUD) | `FlashlightController.MaxBattery` e `BatteryRatio` já consideram a Célula Extra. `IsEcoMode` para mostrar o filtro. |
| 5 (Telas e checkpoint) | `UpgradeTerminal` é provisório em `OnGUI`: a tela final segue o estilo das telas da Frente 5. Para o checkpoint (#45), `FlashlightController.Installed` lista os upgrades; reaplicar com `LuxUpgrades.Apply`. |

## Testes

Menu **Sombra da Sanidade > Rodar teste dos upgrades da Lux-90** (`UpgradeTest`). Batchmode: `-executeMethod SombraDaSanidade.EditorTools.UpgradeTest.RunBatch`.

Cenas geradas antes do #31: o `DriveServer` adiciona o `UpgradeTerminal` no jogador ao iniciar.
