# 0006: painéis de cutscene em Aseprite / PNG 320x180

**Data:** 22/11/2026 · **Status:** aceita

## Contexto
A ADR 0004 define que a arte do projeto sai do formato texto em `art-src/` (JSON indexado na paleta). Porém, as cutscenes do Release Candidate (`C0`, `M1`–`M3`, `C3`, `C4`, `C5`) exigem painéis de tela inteira em `320x180` pixels. Descrever cenas de 57.600 pixels manualmente em JSON de texto inviabiliza a produção no prazo do RC.

## Decisão
- **Exceção ao formato texto para painéis `320x180`.** Os painéis de cutscene são criados diretamente no Aseprite (`art-src/Cutscenes/*.aseprite`) ou gerados via `CutsceneRc3Builder` como PNGs `320x180` em `Assets/_Project/Art/Cutscenes/`.
- **Importação idêntica:** `FilterMode.Point`, sem compressão (`Uncompressed`) e `pixelsPerUnit = 16`, mantendo a nitidez da Pixel Perfect Camera.

## Consequências
- Mantém o versionamento leve (PNGs `320x180` sem compressão pesam poucos KB, sem precisar de Git LFS).
- Permite substituir qualquer painel provisório pelo export final do Aseprite sem alterar código ou `Cutscene` assets.
