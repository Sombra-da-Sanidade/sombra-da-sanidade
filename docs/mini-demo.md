# Mini demo: quinta 29/10/2026

Primeira entrega: demo jogável no navegador (WebGL) com os itens **Essenciais do MVP** (GDD seção 12.1):

1. Movimentação 2D, lanterna de cone e dreno de sanidade
2. IA perseguidora imortal com NavMesh 2D e pulso EMP
3. Reparo de disjuntores e extração das 3 gavetas de HD

Tudo em blocos cinzas. Arte, som, alucinações e narrativa ficam para depois.

## O que já está pronto

| Peça | Arquivo | Estado |
|---|---|---|
| Movimento + mira | `Player/PlayerController.cs` | pronto |
| Sprite do Thomas (8 direções, andar, reparo) | `Player/PlayerSpriteAnimator.cs` | pronto (ver [guia de estilo](arte/guia-de-estilo.md)) |
| Tileset do laboratório | `art-src/Tiles/lab_tiles.json` | rascunho (ainda não usado na Demo) |
| Lanterna (cone, bateria, sombras) | `Flashlight/FlashlightController.cs` | pronto |
| Câmera com tremor | `Core/CameraFollow.cs` | pronto |
| Estado da partida, HDs, vitória/derrota, tela de fim, R reinicia | `Core/GameManager.cs` | pronto |
| Ruído global (monstro escuta) | `Core/NoiseBus.cs` | pronto |
| Interação segurando E (cancela se mover) | `Interaction/PlayerInteractor.cs` | pronto |
| Servidor de HD | `Interaction/DriveServer.cs` | pronto |
| Porta da eclusa | `Interaction/ExitDoor.cs` | pronto |
| HUD provisório (bateria, sanidade, HDs) | `UI/DemoHud.cs` | pronto |
| Monstro (patrulha, ruído, luz, EMP, Zona Segura, contato, sprites) | `Monster/` | pronto (ver [monstro.md](monstro.md)) |
| Sanidade (dreno, efeitos, alucinações, pistas, Estabilizador) | `Sanity/` | pronto (ver [sanidade.md](sanidade.md)) |
| Disjuntor | `Interaction/Breaker.cs` | **parcial** (reparo e ruído; Zona Segura TODO) |
| EMP | `Flashlight/` | pronto |

Menus no Unity (**Sombra da Sanidade**):
- **Criar Sandbox > Frente N**: cena de teste da sua frente com o mapa da demo completo.
- **Gerar cena Demo (base)**: cria `Scenes/Main/Demo.unity`. A Frente 4 gera uma vez no 6000.5.10f1 e commita; depois disso só ela edita a cena.
- **Build WebGL**: gera `Builds/WebGL`.
- **Arte > Gerar cena de teste de arte**: sala com o tileset, o Thomas e Light2D (`Sandbox/ArteTeste.unity`).
- **Arte > Gerar Tile assets do laboratório**: cria os `Tile` em `Art/Tiles/Lab/` para usar no Tile Palette.

Procure `TODO(Frente N)` no código para achar o que falta na sua frente.

## Divisão em 5 frentes

Cada integrante escolhe **uma frente livre** e se atribui nas issues dela (como fazer: [equipe.md](equipe.md#como-pegar-uma-frente)).

### Frente 1: Lanterna e EMP
- **Essencial do MVP:** lanterna de cone, pulso EMP.
- **Issues:** #6
- **Entrega:** clique direito dispara o EMP: `TrySpend(empCost)`; se o monstro estiver iluminado (`MonsterController.IsLitByFlashlight`, com as mesmas alturas usadas pela detecção), chama `Stun(empStunDuration)`; depois `NoiseBus.Emit` e `CameraFollow.Shake`. Sem carga: aviso visual. Com a lanterna desligada: aviso “Lanterna desligada” e tremor, sem gasto, ruído ou atordoamento. Ajuste fino da lanterna e dos efeitos do EMP no `FlashlightConfig`.
- **Pode mexer em:** `Scripts/Flashlight/`, `Scripts/Player/`.

### Frente 2: Monstro
- **Essencial do MVP:** IA perseguidora imortal com NavMesh 2D.
- **Issues:** #4, #5
- **Entrega:** NavMeshPlus (Surface na cena, Modifier nas paredes), NavMeshAgent, patrulha por waypoints, perseguição, atração quando o cone da lanterna o toca, investigar ruídos (`Alert`), stun, `GameManager.Instance.TriggerGameOver(DeathCause.Monster)` ao encostar no jogador.
- **Pode mexer em:** `Scripts/Monster/`, prefab Monster.

### Frente 3: Sanidade e HUD
- **Essencial do MVP:** dreno de sanidade.
- **Issues:** #8, #9
- **Entrega:** dreno no escuro (`!flashlight.IsLit && !InSafeZone`), dreno com o monstro próximo, regeneração na Zona Segura, vinheta no Volume reagindo ao `Ratio`. Melhorar o HUD provisório se sobrar tempo.
- **Pode mexer em:** `Scripts/Sanity/`, `Scripts/UI/`, Volume profile.

### Frente 4: Disjuntor e mapa
- **Essencial do MVP:** reparo de disjuntores.
- **Issues:** #10, #12
- **Entrega:** ao completar o reparo, liga Light2D de teto + trigger de Zona Segura por 60s (chama `SetInSafeZone`, monstro não entra), depois libera novo reparo. Responsável pela cena `Demo.unity`: gerar, ajustar corredores e posições após o playtest.
- **Pode mexer em:** `Scripts/Interaction/Breaker.cs`, `Scenes/Main/Demo.unity`.

### Frente 5: Partida, HDs e build
- **Essencial do MVP:** extração das 3 gavetas de HD, vitória e derrota.
- **Issues:** #7, #11
- **Entrega:** validar `GameManager`, `DriveServer` e `ExitDoor` (já implementados) na `Demo.unity`, conduzir a integração e o playtest, ajustar números nos configs, gerar o build WebGL e publicar o link. Acompanhar o onboarding (#2).
- **Pode mexer em:** `Scripts/Core/`, `DriveServer.cs`, `ExitDoor.cs`, `ScriptableObjects/Config/`, `ProjectSettings/`, `Packages/`.

Regras: cada um testa **na Sandbox da própria frente** e só edita os arquivos da sua frente. Não mude a assinatura dos métodos públicos dos contratos sem avisar no grupo.

## Cronograma

| Quando | O quê |
|---|---|
| Ter 27, noite | Merge da estrutura. Todos: instalar Unity 6000.5.10f1 (+ Web Build Support), clonar, criar Sandbox (issue #2). |
| Qua 28, até 18h | Cada um abre o PR do seu sistema. |
| Qua 28, 18h | Frente sem PR: o grupo redistribui e completa a partir do contrato. |
| Qua 28, noite | Integração na `Demo.unity` (Frentes 4 e 5), playtest com todos, ajuste de números. |
| Qui 29, manhã | Frente 5: build WebGL, upload, testar o link em outro PC. Código congelado. |

## Roteiro do playtest

1. Começa no saguão (embaixo). Bateria cai com a lanterna ligada.
2. Apagar a lanterna: sanidade cai. Ficar na luz do disjuntor reparado: sanidade sobe.
3. Monstro patrulha; quando o feixe toca nele, ele vem. EMP atordoa.
4. Reparar disjuntor faz barulho e atrai o monstro.
5. Pegar os 3 HDs (cantos e topo). Cada HD deixa o monstro mais rápido.
6. Porta fica verde com 3 HDs: encostar = vitória.
7. Encostar no monstro ou sanidade 0 = Game Over. R reinicia.
