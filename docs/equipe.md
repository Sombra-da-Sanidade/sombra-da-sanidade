# Organização da equipe: frentes de trabalho

O projeto é dividido em **5 frentes**, uma por pessoa. Ninguém é designado: cada integrante **escolhe uma frente livre** e se atribui nas issues dela no GitHub.

## Como pegar uma frente

1. Escolha uma frente que ainda não tenha responsável (veja quem está atribuído nas issues do [board](https://github.com/orgs/Sombra-da-Sanidade/projects/1)).
2. Avise no grupo qual frente você pegou.
3. No GitHub, abra cada issue da frente e clique em **Assignees > assign yourself**.
4. Crie sua Sandbox: menu **Sombra da Sanidade > Criar Sandbox > Frente N**.
5. Procure no código por `TODO(Frente N)`: é o que falta na sua frente.

Uma pessoa por frente. Se terminar antes, ajude outra frente via PR, com a revisão de quem é responsável por ela.

## As 5 frentes

Escopo atual: itens **Essenciais do MVP** (GDD seção 12.1) para a mini demo. Detalhes e prazos em [mini-demo.md](mini-demo.md).

| Frente | Sistema | Essencial do MVP | Issues | Label | Pastas / assets |
|---|---|---|---|---|---|
| 1 | Lanterna e EMP | Lanterna de cone; Pulso EMP | #6 | `frente:1-lanterna` | `Scripts/Flashlight/`, `Scripts/Player/` |
| 2 | Monstro | IA perseguidora imortal com NavMesh 2D | #4, #5 | `frente:2-monstro` | `Scripts/Monster/`, prefab Monster |
| 3 | Sanidade e HUD | Dreno de sanidade | #8, #9 | `frente:3-sanidade` | `Scripts/Sanity/`, `Scripts/UI/`, Volume profile |
| 4 | Disjuntor e mapa | Reparo de disjuntores (Zona Segura) | #10, #12 | `frente:4-disjuntor` | `Scripts/Interaction/Breaker.cs`, `Scenes/Main/Demo.unity` |
| 5 | Partida, HDs e build | Extração das 3 gavetas de HD; vitória/derrota | #7, #11 | `frente:5-integracao` | `Scripts/Core/`, `DriveServer`, `ExitDoor`, configs, `ProjectSettings/`, `Packages/` |

Comum a todos: **#2 (onboarding)**. Fora da mini demo: #3 (GDD v1.1) e #13 (direção de arte).

## Responsabilidades de quem pega uma frente

- Decide como implementar o sistema, dentro do contrato público já definido no código.
- É a única pessoa que edita as cenas/prefabs principais da frente.
- Revisa PRs de outras pessoas que mexam na frente.
- Mantém as issues da frente atualizadas no board (A fazer → Fazendo → Revisão → Feito).

## Sandboxes

Cada frente tem sua cena de teste: `Assets/_Project/Scenes/Sandbox/Frente<N>.unity`, com o mapa da demo completo. Só quem pegou a frente edita.

## Revisão de PR

- PR de uma frente: revisão de quem é responsável por ela (ou de qualquer outra pessoa, se o autor for o responsável).
- PR que toca `ProjectSettings/` ou `Packages/`: revisão da Frente 5.
- PR de GDD (`docs/gdd/`): qualquer pessoa, avisando no grupo.

## Depois da mini demo

As frentes continuam as mesmas e crescem com o vertical slice:

| Frente | Próximos passos |
|---|---|
| 1 | Módulos neurais (Adrena-Syn), upgrades da Lux-90 (proposta v1.1) |
| 2 | Estados completos da IA, balanceamento por ala |
| 3 | Alucinações, Revelação pela Loucura, HUD diegético final |
| 4 | Level design das 3 alas, arte e áudio do ambiente |
| 5 | Checkpoint, telas de menu/briefing/rank, builds e publicação |
