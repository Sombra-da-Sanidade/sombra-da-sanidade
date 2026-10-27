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
| Lanterna (cone, bateria, sombras) | `Flashlight/FlashlightController.cs` | pronto |
| Câmera com tremor | `Core/CameraFollow.cs` | pronto |
| Estado da partida, HDs, vitória/derrota, tela de fim, R reinicia | `Core/GameManager.cs` | pronto |
| Ruído global (monstro escuta) | `Core/NoiseBus.cs` | pronto |
| Interação segurando E (cancela se mover) | `Interaction/PlayerInteractor.cs` | pronto |
| Servidor de HD | `Interaction/DriveServer.cs` | pronto |
| Porta da eclusa | `Interaction/ExitDoor.cs` | pronto |
| HUD provisório (bateria, HDs) | `UI/DemoHud.cs` | pronto |
| Monstro | `Monster/MonsterController.cs` | **stub** (API pronta, lógica TODO) |
| Sanidade | `Sanity/SanityController.cs` | **parcial** (valor, estados, morte; dreno TODO) |
| Disjuntor | `Interaction/Breaker.cs` | **parcial** (reparo e ruído; Zona Segura TODO) |
| EMP | `Flashlight/` | **a fazer** |

Menus no Unity (**Sombra da Sanidade**):
- **Criar minha Sandbox > <Nome>**: sua cena de teste com o mapa da demo completo.
- **Gerar cena Demo (base)**: cria `Scenes/Main/Demo.unity`. Davi gera uma vez no 6000.5.10f1 e commita; depois disso só o dono da cena edita.
- **Build WebGL**: gera `Builds/WebGL`.

Procure `TODO(<seu nome>)` no código para achar o que é seu.

## Divisão

| Pessoa | Entrega | Issue | Pode mexer em |
|---|---|---|---|
| Luis | EMP no clique direito: `TrySpend(empCost)`, se o monstro estiver no cone chama `Stun(empStunDuration)`, `NoiseBus.Emit`, `CameraFollow.Shake`. Ajuste fino da lanterna. | #6 | `Scripts/Flashlight/`, `Scripts/Player/` |
| Rony | Monstro: NavMeshPlus (Surface na cena, Modifier nas paredes), patrulha por waypoints, perseguição, atração quando `IsPointInCone` o toca, ouvir ruído, stun, `TriggerGameOver(Monster)` ao encostar. | #4, #5 | `Scripts/Monster/`, prefab Monster |
| Marley | Sanidade: dreno no escuro (`!flashlight.IsLit && !InSafeZone`), dreno perto do monstro, regen na Zona Segura; vinheta no Volume reagindo ao `Ratio`. Melhorar HUD se der tempo. | #8, #9 | `Scripts/Sanity/`, `Scripts/UI/`, Volume profile |
| Thiago | Disjuntor: ao completar, liga Light2D de teto + trigger de Zona Segura por 60s (chama `SetInSafeZone`, monstro não entra). Ajustar o mapa da `Demo.unity` (dono da cena). | #10, #12 | `Scripts/Interaction/Breaker.cs`, `Scenes/Main/Demo.unity` |
| Davi | Integração na `Demo.unity` (com o Thiago), playtest, números nos configs, build WebGL e publicação. | #7, #11 | `Scripts/Core/`, configs |

Regras: cada um testa **na própria Sandbox** e só edita os arquivos da sua linha. Não mude a assinatura dos métodos públicos dos stubs sem avisar no grupo.

## Cronograma

| Quando | O quê |
|---|---|
| Ter 27, noite | Merge da estrutura. Todos: instalar Unity 6000.5.10f1 (+ Web Build Support), clonar, criar Sandbox (issue #2). |
| Qua 28, até 18h | Cada um abre o PR do seu sistema. |
| Qua 28, 18h | O que não tiver PR, Davi completa a partir do stub. |
| Qua 28, noite | Integração na `Demo.unity`, playtest, ajuste de números. |
| Qui 29, manhã | Build WebGL, upload, testar o link em outro PC. Código congelado. |

## Roteiro do playtest

1. Começa no saguão (embaixo). Bateria cai com a lanterna ligada.
2. Apagar a lanterna: sanidade cai. Ficar na luz do disjuntor reparado: sanidade sobe.
3. Monstro patrulha; quando o feixe toca nele, ele vem. EMP atordoa.
4. Reparar disjuntor faz barulho e atrai o monstro.
5. Pegar os 3 HDs (cantos e topo). Cada HD deixa o monstro mais rápido.
6. Porta fica verde com 3 HDs: encostar = vitória.
7. Encostar no monstro ou sanidade 0 = Game Over. R reinicia.
