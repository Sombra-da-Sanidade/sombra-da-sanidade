# 0004: pipeline de arte em texto

**Data:** 28/10/2026 · **Status:** aceita

## Contexto
Cinco pessoas precisam ver, revisar e editar a pixel art pelo GitHub. PNG é binário: no PR aparece só "arquivo mudou", sem diff linha a linha. Sem LFS ([0002](0002-sem-git-lfs.md)), arquivos de trabalho grandes também não entram no repo.

## Decisão
- A fonte da arte é texto: `art-src/<Categoria>/<nome>.json`, um caractere por pixel, chaves da paleta `art-src/palette.json`.
- `tools/art/export_sprites.py` (Python 3 + Pillow) gera os PNG em `Assets/_Project/Art/<Categoria>/`, com `.meta` de GUID determinístico.
- O Unity configura e fatia os PNG sozinho com `Scripts/Editor/PixelArtImporter.cs`, lendo o mesmo JSON (16 PPU, Point, sem compressão).
- Escala: tile 16x16, personagem de 24 a 28 px num frame 16x32, Pixel Perfect Camera 320x180.

## Consequências
- PR de arte mostra exatamente quais pixels mudaram.
- Quem prefere Aseprite/LibreSprite desenha lá com a paleta `art-src/sombra.gpl` e converte o PNG para JSON (Estúdio de Arte importa PNG).
- PNG em `Assets/_Project/Art/{Tiles,Sprites,Palette}` não deve ser editado direto: é sobrescrito pelo exportador.
- Python + Pillow vira requisito para quem mexe em arte (`pip install pillow`).
