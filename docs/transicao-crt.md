# Transição CRT entre cenas

Frente 5, issue #94. Ao sair de uma cena, a imagem achata numa linha horizontal de fósforo, a linha encolhe até o ponto e a tela apaga. A cena nova abre ao contrário. Código em `UI/CrtTransition.cs`, tempos em `Resources/CrtTransitionConfig.asset`.

## Quando aparece

| Momento | O que acontece |
|---|---|
| Abrir o jogo | A primeira cena abre da linha (`openOnBoot`) |
| JOGAR ou CONTINUAR no Menu | Fecha o Menu e abre na ala |
| PRÓXIMA ALA ou MENU na vitória | Fecha a ala e abre na seguinte (ou no Menu) |
| Reiniciar (R no fim ou na pausa) e SAIR da pausa | Fecha e abre de novo |
| Game Over | Apagão rápido: a imagem da morte fecha na linha, segura um instante no preto e abre já na tela de fim, sem trocar de cena. A tela de fim fica escondida enquanto fecha (`HidingEndScreen`) |

A transição é criada sozinha na primeira cena e fica viva entre as cenas, então não é preciso editar cena nem prefab. Ela é desenhada acima de todas as telas (HUD, resultado, pausa, cutscene).

## Para quem troca de cena

Use `CrtTransition.LoadScene(nome)` no lugar de `SceneManager.LoadScene(nome)`.

- Enquanto a tela fecha, a imagem fica congelada (`Time.timeScale = 0`). O tempo volta ao normal antes de a cena nova carregar.
- O passo de tempo por quadro é limitado: o quadro travado do carregamento não engole a abertura.
- Um segundo pedido durante a troca é ignorado (Enter duplo no Menu, R apertado de novo).
- Fora do Play, ou com uma cena que não está no Build Profile, carrega direto, como antes.

## Ajustes (`CrtTransitionConfig`)

| Campo | Padrão | O quê |
|---|---|---|
| `closeSeconds` | 0,35 | Tempo para fechar até apagar |
| `loadHoldSeconds` | 0,2 | Tempo no preto na cena nova antes de abrir (esconde a câmera se ajeitando no spawn) |
| `openSeconds` | 0,45 | Tempo para abrir na cena nova |
| `gameOverHoldSeconds` | 0,15 | Tempo no preto do Game Over antes de abrir sobre a tela de fim |
| `collapseShare` | 0,6 | Parte da animação em que a tela achata; o resto é a linha encolhendo |
| `openOnBoot` | ligado | Abrir o jogo com a linha |

As cores são da paleta: preto `Vazio`, linha `Fósforo 5` com brilho `Fósforo 4`.

## Som

O `CrtTransition` toca os sons pelo `UiSfx`, no grupo UI:
- ao fechar para trocar de cena, `GameSfx.crtOff` (`audio-src/UI/crt_desliga.json`): relé, chiado agudo caindo, a imagem sugando no grave e um pop;
- ao abrir na cena nova e ao abrir o jogo, `GameSfx.crtOn` (`audio-src/UI/crt_liga.json`): o "tum" da desmagnetização, estática e o chiado subindo.

O apagão do Game Over fica mudo, porque nele já tocam o grito do monstro e o sting do Game Over.

## Teste

Menu **Sombra da Sanidade > Rodar teste da transição CRT**, ou em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.CrtTransitionTest.RunBatch
```

O teste confere a forma da faixa, a abertura ao entrar, o apagão do Game Over e o reinício, que só carrega a cena com a tela apagada e abre na cena nova com o tempo normal.
