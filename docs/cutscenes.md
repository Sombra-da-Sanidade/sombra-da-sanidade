# Cutscenes e memórias

Issue #88. Plano de direção: [planejamento-rc.md](planejamento-rc.md) seção 4. Textos: [narrativa/roteiro-rc.md](narrativa/roteiro-rc.md). Montar as 11 cutscenes é a #91.

## Peças

| Peça | O que faz |
|---|---|
| `Cutscene` (ScriptableObject) | Lista de planos, faixa de música e se congela o mundo. Criar em **Create > Sombra da Sanidade > Narrativa > Cutscene** |
| `CutscenePlayer` | Toca a cutscene: `CutscenePlayer.Play(cutscene, aoTerminar)`. Criado sozinho na primeira chamada |
| `CutsceneTrigger` | Collider trigger na cena que toca uma cutscene quando o Thomas entra. Uma vez por partida. `sanityBelow` para a M3 (35) |
| `CutsceneMarker` | Ponto da cena com um `id`: alvo da câmera (`focus`) e reação a um `cue` (UnityEvent) |
| `MemoryEffect` | Filtro das memórias: dessatura, puxa para o verde, estoura a exposição e põe linhas de TV. Ligado pelo plano com `memory` |

## Plano (`CutsceneShot`)

| Campo | Uso |
|---|---|
| `kind` | `Panel`: imagem 320x180. Sem imagem vira intertítulo (tela preta com o texto no centro). `World`: câmera do jogo com faixas pretas |
| `image` | Painel exportado do Aseprite (#98) |
| `focus` | `World`: `player`, `especime`, `sonambulo` ou o id de um `CutsceneMarker` |
| `zoom` | Começo e fim. Panel: 1 = painel inteiro, 1.5 = recorte. World: multiplica o tamanho da câmera (0.7 = mais perto) |
| `panFrom` / `panTo` | Panel: fração do painel (-0.5 a 0.5). World: unidades a partir do alvo |
| `duration` | Segundos. Espaço ou clique completa a legenda e depois avança |
| `speaker` / `caption` | Legenda digitada. O nome sai em âmbar |
| `sound` | `SoundEvent` tocado no começo do plano |
| `transition` | `Cut`, `Fade` ou `Glitch` (fim de memória) |
| `memory` | Liga o `MemoryEffect` e os quadros repetidos |
| `cue` | Dispara o `CutsceneMarker` de mesmo id (ex.: a lâmpada que pisca na C2) |

## Regras

- **Segurar Esc por 0,8 s pula** a cutscene inteira. Durante a cutscene o Esc não abre a pausa.
- O controle fica travado e a HUD some. Com `freezeWorld` o jogo congela (`Time.timeScale = 0`). Nas cutscenes no motor que precisam do mundo andando (C1, C2), desligar `freezeWorld`.
- A música da fase abaixa. `music` toca uma faixa no lugar: `Music/memoria_memory.ogg` nas memórias, `revelacao.ogg` na C3, `final_*.ogg` nos finais. O WebGL não aplica filtro em runtime (ADR 0005); a versão abafada sai do `render_audio.py`.
- A paleta de 4 cores exata fica nos painéis de memória. No motor o `MemoryEffect` só aproxima.
- `CutscenePlayer.HasPlayed(id)` diz se a cutscene já tocou nesta sessão.

## Exemplo: C2 (Sonâmbulo)

1. Na cena, um `CutsceneMarker` com id `c2_lampada` sobre a lâmpada. O `onCue` apaga a lâmpada e move o Sonâmbulo dois passos para a frente.
2. Asset `C2_Sonambulo`: `freezeWorld` desligado e 3 planos `World` com `focus = sonambulo`. O 2º plano tem `cue = c2_lampada`.
3. Um `CutsceneTrigger` na entrada da ala com o asset.

Teste: **Sombra da Sanidade > Rodar teste das cutscenes** (`CutsceneTest`).
