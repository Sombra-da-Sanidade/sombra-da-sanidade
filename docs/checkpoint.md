# Checkpoint entre alas

Frente 5, issue #45. Um JSON local (`checkpoint.json` em `Application.persistentDataPath`; no WebGL fica no IndexedDB do navegador). Código em `Core/Checkpoint.cs` e `Level/WingCheckpoint.cs`.

## Quando salva e quando restaura

| Momento | O que acontece |
|---|---|
| Vencer uma ala | O `WingCheckpoint` da cena salva o estado e a tela de vitória mostra "PROGRESSO SALVO" (Proposta 2) |
| Entrar na ala para a qual o save aponta (`nextWing`) | O Thomas volta ao estado salvo. Vale para o CONTINUAR do Menu e para o reinício depois de um Game Over (GDD 8: "reinício do checkpoint da ala") |
| JOGAR no Menu | Partida nova: o save é apagado |

O CONTINUAR do Menu só aparece com save e com a cena da próxima ala no Build Profile, com o nome `Ala` + ala (`AlaEnergia`, `AlaExecutiva`).

## O que fica salvo

| Grupo | Campos |
|---|---|
| Ala | `lastWing`, `nextWing`, `elapsedSeconds` (tempo da ala concluída), `savedAtUtc` |
| Thomas | `battery`, `sanity`, `upgrades` (nomes dos assets `LuxUpgrade`) |
| Rank da partida (#32) | `drives`, `totalSeconds`, `deaths`, `emps`, `sanitySum` e `sanitySeconds` (para a sanidade média), `skillCheckFailures` |

- **Upgrades:** são reinstalados pela árvore do `InteractionConfig` dos servidores de HD, antes da bateria (a Célula Extra muda a carga máxima). Todo upgrade, inclusive o Ressonador Neural, fica na lista `FlashlightController.Installed`.
- **Sanidade:** volta no mínimo em 1, para a ala não começar em Game Over.
- **Estatísticas:** vão para o `RunStats` uma vez por save. No reinício depois de um Game Over, o Thomas volta ao save, mas a morte da tentativa continua contando.

## Para a Frente 4

Toda cena de ala precisa de um `WingCheckpoint` com `wing` (a própria ala) e `nextWing` (a seguinte), como a Ala Médica já tem.

## Teste

Menu **Sombra da Sanidade > Rodar teste do checkpoint**, ou em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.CheckpointTest.RunBatch
```

O teste usa uma pasta temporária e não mexe no save de quem está jogando.
