# Equipe e donos de sistema

**Status:** proposta inicial, ajustar em grupo.

Cada sistema tem um **dono**: a pessoa que decide a implementação, revisa PRs daquela área e é a única que edita as cenas/prefabs principais dele. Qualquer um pode contribuir via PR, com a revisão do dono.

| Sistema | Dono | GitHub | Pastas / assets |
|---|---|---|---|
| Player e Lanterna (porte do Escura) | Luis Filipe | `LuisFilipeOliveiraSantos` | `Scripts/Player`, `Scripts/Flashlight`, prefab Player |
| Core, integração, repositório e builds | Davi Vieira | `notsogreatdavi` | `Scripts/Core`, `Scenes/Main`, `ProjectSettings`, `Packages` |
| IA do Espécime Vazio (NavMeshPlus, EMP, stun) | Rony Elias | a definir | `Scripts/Monster`, prefab Monster |
| Sanidade, pós-processamento, alucinações e HUD | Marley Martins | a definir | `Scripts/Sanity`, `Scripts/UI`, Volume profiles |
| Level design das alas, disjuntores, HDs, arte e áudio | Thiago Brito | a definir | `Scenes/Wings`, `Scripts/Interaction`, `Art`, `Audio` |

## Sandboxes

Cada pessoa tem sua cena de teste: `Assets/_Project/Scenes/Sandbox/<Nome>.unity` (Luis, Davi, Rony, Marley, Thiago). Só o dono edita.

## Revisão de PR

- PR de um sistema: revisão do dono (ou de qualquer outro, se o autor for o dono).
- PR que toca `ProjectSettings/` ou `Packages/`: revisão do Davi.
- PR de GDD (`docs/gdd/`): qualquer pessoa, mas avisar no grupo.
