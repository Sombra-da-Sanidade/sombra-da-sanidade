# 0007: paleta dedicada de 4 cores para memórias

**Data:** 22/11/2026 · **Status:** aceita

## Contexto
O roteiro "Voluntário 08" intercala o tempo presente (`C0`, `C3`, `C4`, `C5`) com flashbacks de memória (`M1` Sala Branca, `M2` Assinatura, `M3` Espelho). O jogador precisa distinguir passado e presente imediatamente ao bater o olho, sem texto explicativo na tela.

## Decisão
- **Paleta própria fora do `palette.json`:** as memórias usam 4 tons fixos de alto contraste inspirados em fósforo hospitalar estourado (`#0d1915`, `#1e4d3b`, `#74d482`, `#ecf7ee`).
- **Presente na paleta fria:** cutscenes do presente usam estritamente os 43 tons frios de `art-src/palette.json`.
- **Reforço no motor:** quando um `CutsceneShot` tem `memory = true`, o `CutscenePlayer` ativa o `MemoryEffect` (dessaturação, vinheta verde e linhas de TV) e toca `Music/memoria_memory.ogg`.

## Consequências
- Separação visual instantânea entre realidade e lembrança apagada.
- Qualquer nova cena de flashback deve respeitar as 4 cores definidas acima.
