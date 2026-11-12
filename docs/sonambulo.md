# Sonâmbulo (Voluntário 03)

Frente 2 (Monstro). Segundo monstro do jogo, feito para contrastar com o Espécime Vazio.

Ele é outra cobaia do Projeto Hipnos que nunca acordou da indução de sono. **Só para quando a lanterna ligada está apontada para ele.** No escuro, mesmo que o Thomas esteja olhando, ele vem, e muito rápido.

A luz atrai o Espécime e prende o Sonâmbulo, então segurar um com a lanterna pode chamar o outro.

## Estados

| Estado | Como entra | O que faz | Como sai |
|---|---|---|---|
| **Adormecido** | Início da cena | Parado na pose "dormindo em pé", com o visor aceso fraco. Não se move nem mata. | O Thomas o nota pela primeira vez: campo de visão ou lanterna |
| **Travado** | Lanterna nele, ou na folga de 1,5 s depois de acordar | Para na hora e mostra uma pose sorteada. A menos de 3 m, a pose é sempre o bote. | A luz sai dele (com 0,12 s de atraso) |
| **Andando** | Sem a lanterna nele | Corre a 7,5 m/s até o jogador pelo NavMesh. O jogador anda a 4 m/s. | A lanterna volta a pegar nele |

## Luz e visão

- **O que o trava:** a lanterna ligada, com o cone em pelo menos uma das três alturas testadas (pés, tronco e cabeça; `sampleHeights`). Vale `FlashlightController.IsPointInCone`, com o alcance, o ângulo e as paredes da lanterna. Lanterna apagada ou sem bateria não trava.
- **O que o acorda:** a primeira vez que o Thomas o nota. Isso acontece pela lanterna ou pelo campo de visão, mesmo no escuro: até 9 m (`viewDistance`), dentro de 110° em volta da mira (`viewAngle`) e sem parede da camada `Walls` no meio. O campo de visão só serve para acordá-lo.

## Regras

- **Encostar sem a luz nele mata.** Toca uma das duas cenas de morte (abaixo) e só depois vem o Game Over (`DeathCause.Monster`). Com a luz nele, ele não ataca, nem encostado.
- **Zona Segura:** ele nunca entra numa área com `MonsterRepellent` ligado. Se o jogador está dentro, ele espera na borda.
- **Modo seguro (#48):** fica 30% mais lento. Encostar empurra o jogador 2,5 m, tira 20 de sanidade e deixa ele travado por 2 s. Não há cena de morte.

Os números ficam em `ScriptableObjects/Config/SleepwalkerConfig.asset` e nenhum está no GDD: são para ajustar no playtest.

## Mortes

São duas, sorteadas no contato, numa cena de 80×80 com o monstro e o Thomas juntos. Ela entra no lugar dos dois sprites, espelhada para o lado do jogador. Cada uma tem 32 quadros a 12 fps (`killFps`) e segura o último quadro por 0,8 s (`killHold`).

1. **Engolir:** agarra o Thomas pelos ombros e o ergue. O bocejo desce sobre a cabeça dele e engole até os ombros, enquanto o corpo esperneia cada vez menos e o sangue escorre da mandíbula. Ele arranca a cabeça, larga o corpo e fica de pé com a mandíbula pingando.
2. **Pescoço:** abraça o Thomas por trás e tapa os olhos dele com os dedos compridos. Ergue, torce a cabeça ao contrário com um estalo e solta o corpo mole. Depois volta a dormir em pé ao lado do cadáver, e o visor apaga.

O `MonsterKillScene` é o mesmo do Espécime. Os dois monstros implementam `IMonsterKiller`: o evento `Caught` e `IsKilling`.

## Colocar no mapa

**Beta:** o `BetaSceneBuilder` já coloca um Sonâmbulo no centro da sala da Ala Médica mais longe do saguão, em qualquer seed. Para chegar lá rápido, use o painel F1 > "Perto do Sonâmbulo".

Em outra cena:

1. Abra a cena.
2. Centralize a Scene view no ponto onde ele deve ficar.
3. Use o menu **Sombra da Sanidade > Colocar Sonâmbulo na cena**.

O ponto precisa estar no NavMesh. Como ele começa adormecido, a sala onde ele fica é o "encontro": o jogador entra, vê o vulto parado e, a partir daí, ele passa a caçar.

## Arte

- `art-src/Monster/Sonambulo_sheet.json` e `_glow` (48×64, 8 colunas, 16 px/unidade). Linhas:
  - `pose_0..7`: estendendo os braços, no meio do passo, cabeça caída de lado, agachado, mãos no rosto, bote, de costas virado para você, dormindo em pé;
  - `walk_down`, `walk_up`, `walk_side`;
  - `seen`.
- `art-src/Monster/Sonambulo_kills.json` e `_glow` (80×80, 16 colunas): quadros 0–31 = engolir, 32–63 = pescoço.
- A camada de brilho (visor roxo, chaves `w`, `C` e `B`) usa Sprite-Unlit. Há também uma Light2D roxa fraca no visor.
- Travado, o visor falha de vez em quando: é o "piscar" dele.
- Design: quadros "Sonâmbulo · ficha" e "Sonâmbulo · mortes" no canvas de design do monstro.

## Arquivos

| Arquivo | Papel |
|---|---|
| `Scripts/Monster/SleepwalkerConfig.cs` | Números (ScriptableObject) |
| `Scripts/Monster/SleepwalkerController.cs` | Estados, luz, visão, NavMeshAgent, contato |
| `Scripts/Monster/SleepwalkerSpriteAnimator.cs` | Poses, caminhada, visor e criação da cena de morte |
| `Scripts/Monster/MonsterKillScene.cs` | Cena de morte (compartilhada com o Espécime) |
| `Scripts/Editor/SleepwalkerPrefabBuilder.cs` | Menus **Gerar prefab do Sonâmbulo** e **Colocar Sonâmbulo na cena** |
| `Scripts/Editor/SleepwalkerTest.cs` | Menu **Rodar teste do Sonâmbulo** |

## Teste

**Sombra da Sanidade > Rodar teste do Sonâmbulo** roda na cena de protótipo, com o Espécime desligado, e confere:

- que ele começa adormecido e acorda quando o Thomas olha para ele, mesmo no escuro;
- que fica travado com a lanterna nele;
- que, com a lanterna apagada, ele vem mesmo sendo olhado, bem mais rápido que o jogador;
- que trava na hora com a luz de novo, numa pose sorteada, e não mata com a luz nele;
- que pelas costas toca a cena de morte e depois o Game Over.

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.SleepwalkerTest.RunBatch
```
