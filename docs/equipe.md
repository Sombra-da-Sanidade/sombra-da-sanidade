# Organização da equipe: frentes de trabalho

Depois da mini demo o projeto continua com **5 frentes**, uma por pessoa. Ninguém é designado: cada integrante **escolhe uma frente livre** e se atribui nas issues dela no GitHub. O roteiro completo, com fases e dependências, está em [planejamento.md](planejamento.md).

> As frentes foram redistribuídas depois da mini demo (ver [mini-demo.md](mini-demo.md) para a divisão antiga). A label `frente:5-integracao` virou `frente:5-partida`.

## Como pegar uma frente

1. Escolha uma frente que ainda não tenha responsável (veja quem está atribuído nas issues do [board](https://github.com/orgs/Sombra-da-Sanidade/projects/1)).
2. Avise no grupo qual frente você pegou.
3. No GitHub, abra cada issue da frente e clique em **Assignees > assign yourself**.
4. Crie sua Sandbox: menu **Sombra da Sanidade > Criar Sandbox > Frente N**.
5. Comece pelas issues do milestone **Vertical slice**; as de **Versão final** vêm depois.

Uma pessoa por frente. Se terminar antes, pegue uma issue **Extra** ou ajude outra frente via PR, com a revisão de quem é responsável por ela.

## As 5 frentes

| Frente | Sistema | Vertical slice (semana 8) | Versão final (semana 12) | Extras | Label | Pastas / assets |
|---|---|---|---|---|---|---|
| 1 | Jogador, interação e itens | #33 skill check do HD, #29 armários, #31 upgrades da Lux-90 | #34 Adrena-Syn | #46, #47 | `frente:1-jogador` | `Scripts/Player/`, `Scripts/Flashlight/`, `Scripts/Interaction/` (exceto `Breaker`), `DriveServer` |
| 2 | Monstro e tensão | #35 diretor de tensão, #36 comportamentos por ala (Médica) | #37 comportamentos Energia/Executiva | #48 | `frente:2-monstro` | `Scripts/Monster/`, prefab Monster, `Core/TensionDirector` |
| 3 | Sanidade, HUD e narrativa | #38 batimento em camadas, #16 barra MENTE | #39 logs de áudio, #40 Ato 3 | | `frente:3-sanidade-hud` | `Scripts/Sanity/`, HUD em jogo (`Scripts/UI/`), Volume profile, roteiros |
| 4 | Níveis | #41 Ala Médica completa | #42 Energia, #43 Executiva, #44 Eclusa | | `frente:4-niveis` | `Scenes/Wings/`, `Breaker`, `ExitDoor`, tiles e arte de ambiente |
| 5 | Partida, telas e build | #30 `DifficultyProfile`, #32 telas e rank | #45 checkpoint | #49 | `frente:5-partida` | `Scripts/Core/` (exceto `TensionDirector`), cenas de Menu/Briefing/Vitória, configs, `ProjectSettings/`, `Packages/`, build WebGL |

Comum a todos: **#3 (aprovar o GDD v1.1)** trava boa parte das issues, então a reunião vem primeiro. Qualquer sprite novo segue o [guia de estilo](arte/guia-de-estilo.md) e entra pelo `art-src/` (ver [CONTRIBUTING](../CONTRIBUTING.md#arte)).

### Onde as frentes se encostam

- **#33 skill check:** a lógica é da Frente 1; o mostrador final usa o estilo do HUD da Frente 3.
- **#31 upgrades:** efeitos na Frente 1; a tela de escolha (CRT verde) segue o estilo das telas da Frente 5.
- **#30 `DifficultyProfile`:** a Frente 5 cria o asset e o perfil ativo; as Frentes 1, 2, 3 e 4 só leem e preenchem os valores da própria parte.
- **#41 a #44 alas:** a Frente 4 monta as cenas; o checkpoint (#45) e a troca de ala são da Frente 5.
- **#49 bônus no rank:** a Frente 5 calcula; a contagem de falhas vem do skill check (#33).

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
