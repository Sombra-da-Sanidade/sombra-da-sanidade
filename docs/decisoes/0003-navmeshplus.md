# 0003: NavMeshPlus para a IA do monstro

**Data:** 27/10/2026 · **Status:** aceita

## Contexto
O NavMesh nativo da Unity é pensado para 3D. O GDD (seção 10, riscos técnicos) já indicava o NavMeshPlus para navegação em 2D top-down.

## Decisão
Pacote `com.h8man.2d.navmeshplus` instalado via git URL no `Packages/manifest.json`.

## Consequências
- Ao abrir o projeto pela primeira vez, conferir no Package Manager se o NavMeshPlus resolveu. Se pedir dependência, instalar **AI Navigation** (`com.unity.ai.navigation`) pelo Package Manager e commitar o manifest.
- A URL aponta para `master`; quando o primeiro uso estiver estável, fixar um commit (`#<hash>`) para que todos tenham a mesma versão.
