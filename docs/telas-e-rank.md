# Telas e rank

Frente 5, issue #32 (e o CONTINUAR da #45). Fluxo do GDD 8: Menu → Briefing → Jogo → Vitória com rank / Game Over com a causa.

## Fluxo

| Tela | Onde | O que faz |
|---|---|---|
| Menu | `Scenes/Main/Menu.unity` (`MainMenuScreen`), cena 0 do Build Profile e da build WebGL | CONTINUAR (só com checkpoint), JOGAR, CONFIGURAÇÕES, SAIR e o melhor rank da cena do jogo |
| Abertura | Canvas `StoryCanvas` do Menu (`StoryIntro`), uma vez por sessão | Cutscene em texto com a história (GDD 9), digitada letra a letra em TextMeshPro; Espaço avança, Esc pula |
| Briefing | Página do Menu, depois de JOGAR | Ordem de serviço e controles; INICIAR TURNO carrega a cena do jogo (`gameScene`, padrão `Beta`) |
| Vitória | `ResultScreen`, chamada pelo `GameManager` | Gavetas de HD, selo do rank, melhor rank e as 6 estatísticas |
| Game Over | `ResultScreen` | Selo e frase da causa (`DeathCause`); R ou o botão reiniciam |

- **Música e fonte:** o Menu toca `Audio/Music/menu_drone.wav` em loop (gerado por `python3 tools/audio/gen_menu_music.py`). Os textos em Canvas usam a VT323 (SIL OFL) com font atlas do TextMeshPro em `Art/Fonts/VT323/` (**Sombra da Sanidade > Arte > Gerar fonte TMP**). Os Canvas usam `CanvasScaler` por tamanho de tela (640×360) e âncoras nos cantos e no centro.
- **JOGAR** começa uma partida nova: zera as mortes e apaga o checkpoint antigo.
- **CONTINUAR** aparece quando há checkpoint salvo (#45) e a cena da próxima ala está no Build Profile. O nome da cena segue a Ala Médica: `Ala` + ala (`AlaEnergia`, `AlaExecutiva`). Enquanto essas cenas não existirem, o botão não aparece. O que o checkpoint guarda está em [checkpoint.md](checkpoint.md).
- **Estatísticas entre alas:** tempo, EMPs, sanidade média, mortes, HDs e falhas somam as alas anteriores, vindas do checkpoint.

## Estatísticas

O `GameManager` registra:

| Estatística | Como conta |
|---|---|
| Tempo | `ElapsedTime`, desde o último reinício da cena |
| Sanidade média | Média no tempo (não por frame) da sanidade de 0 a 100% |
| EMPs | Pulsos disparados (evento `FlashlightController.EmpFired`), desde o último reinício |
| Mortes | Game Overs da partida inteira. Sobrevivem ao reinício (`RunStats`) e zeram ao começar pelo Menu |
| Falhas no multímetro | Somadas dos servidores (`DriveServer.SkillCheckFailures`) |

## Rank (Proposta 3)

Cada critério vira uma nota de 0 a 1, e a soma ponderada dá a nota final:

| Critério | Peso | Nota cheia | Nota zero |
|---|---|---|---|
| Tempo | 30% | Até 240 s por HD da cena | 2,5 vezes o tempo ideal |
| Sanidade média | 30% | 100% | 0% |
| EMPs | 20% | Nenhum | 6 ou mais |
| Mortes | 20% | Nenhuma | 3 ou mais |

Nota final: **S** a partir de 0,85, **A** de 0,70, **B** de 0,50, **C** abaixo disso.

O tempo ideal escala com os HDs da cena: 4 min na Ala Médica (1 HD) e 12 min na Beta (3 HDs). Só os pesos vêm do GDD; os outros números são chutes para o playtest. Todos ficam em `Assets/_Project/Resources/RankConfig.asset`, em Resources para valer em todas as cenas sem editar nenhuma, como o `HudArt`.

O melhor rank de cada cena fica salvo em PlayerPrefs (`Sombra.BestRank.<cena>`). A vitória mostra "NOVO MELHOR RANK" quando ele sobe.

## Fora do #32

- As 4 conquistas da Proposta 3.
- O bônus no rank por HD sem falha no skill check (#49).

## Teste

Menu **Sombra da Sanidade > Rodar teste de rank e telas**, ou em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.RankTest.RunBatch
```
