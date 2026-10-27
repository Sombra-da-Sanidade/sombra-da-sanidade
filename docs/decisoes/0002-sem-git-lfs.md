# 0002: sem Git LFS

**Data:** 27/10/2026 · **Status:** aceita

## Contexto
O plano gratuito do GitHub dá 1 GB de armazenamento e 1 GB/mês de banda de LFS por conta/organização. Com 5 pessoas clonando e puxando, a banda acaba rápido. A arte é pixel art (arquivos pequenos) e o áudio é pouco.

## Decisão
Não usar LFS. Binários são marcados como `binary` no `.gitattributes`.

## Consequências
- Evitar commitar arquivos grandes: PSD/Aseprite de trabalho acima de ~5 MB ficam fora do repo (Drive do grupo), só o PNG exportado entra.
- Áudio em `.ogg`, não `.wav`, quando possível.
- Revisar se o repositório passar de ~500 MB.
