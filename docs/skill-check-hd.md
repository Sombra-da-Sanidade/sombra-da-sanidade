# Skill check na ejeção de HD

**Status:** implementado (#33) com os números abaixo, que ainda dependem da aprovação do GDD v1.1 (#3). Números e justificativa: [pesquisa/propostas.md](pesquisa/propostas.md) (Propostas 5 e 6).

## Por quê
Extrair HD vira o "gerador" do Dead by Daylight / a "cifra" do Identity V: tarefa longa, parado e exposto, com checks que exigem atenção. O GDD v1.0 (seção 4.4, "Modos / minijogos") já previa uma "calibragem de circuitos no multímetro" que nunca foi implementada.

Decisões:
- Skill check com ponteiro do multímetro: apertar **Espaço** na faixa. Soltar E continua zerando o progresso.
- Só os HDs ganham o skill check. Disjuntores continuam com 4 s simples. A vitória não muda (3 HDs + porta).

## Comportamento
- Ejeção de **12 s** (hoje `ejectDuration = 2`).
- Depois de 2 s de canalização, 25% de chance por segundo, no máximo 3 checks por HD. Pelo menos 1 por ejeção (forçado num ponto sorteado se o acaso não vier), e nenhum começa nos últimos ~2,1 s (aviso + varredura + folga), para não ser cortado pelo HD saindo.
- Bipe de aviso, 0,6 s depois o ponteiro varre o mostrador em 1,1 s.
- Good (15% do mostrador): continua normal. Great (4%): +5% de progresso.
- Erro ou nenhum aperto: -10% de progresso, pausa de 1 s, faísca, `NoiseBus.Emit` (raio 12) e `CameraFollow.Shake`.
- Faixa good encolhe 2 pontos por HD já coletado (15, 13, 11). Monstro perto: -40%. Sanidade < 35: ponteiro treme.
- A partir de 50%, o servidor emite ruído baixo (raio 6) a cada 3 s.
- Todos os números no `InteractionConfig`, com tooltip "não está no GDD: ajustar no playtest".

## Arquivos
Novos (cada `.cs` com `.meta` e GUID aleatório no formato MonoImporter):
- `Scripts/Interaction/ISkillCheckable.cs`: interface opcional. `IInteractable` não muda, então `Breaker`, `LithiumStabilizer` e `SanityClue` continuam iguais.
- `Scripts/Interaction/SkillCheck.cs`: componente no Player. Estados Idle, Aviso, Ativo, Resultado. Só roda com `PlayerInteractor.Current is ISkillCheckable` e `IsChanneling`. Expõe `NeedlePosition`, `ZoneStart`, `ZoneSize`, `State` e `event Action<SkillCheckResult> Resolved`.
- `Scripts/UI/SkillCheckHud.cs`: multímetro em pixel art ao lado do Thomas (`art-src/UI/skillcheck.json`, #64; ver [hud.md](hud.md#arte-em-pixel-art-srcui)).
- `Scripts/Editor/SkillCheckTest.cs`: `MenuItem` de teste no padrão de `BreakerTest.cs` e `MonsterTest.cs`.

Alterados:
- `Interaction/PlayerInteractor.cs`: `public void AddProgress(float delta)` com clamp de 0 a 1.
- `Interaction/DriveServer.cs`: implementa `ISkillCheckable` e o ruído depois de 50%.
- `Interaction/InteractionConfig.cs` + `ScriptableObjects/Config/InteractionConfig.asset`: seção "Skill check" e `ejectDuration = 12`.
- `Editor/PrototypeSceneBuilder.cs`: adicionar `SkillCheck` e `SkillCheckHud` ao Player, onde hoje entra o `PlayerInteractor`.
- `docs/gdd/gdd.md`: seção 4 (HD), seção 8 (Espaço nos controles) e histórico.

Reaproveitar: `NoiseBus.Emit`, `CameraFollow.Shake`, `GameManager.Instance` (HDs coletados), `SanityController` (sanidade atual).

## Passos
1. Issue (Frente 5, `sistema:interacao`, `sistema:ui`, milestone Vertical slice).
2. Branch `feat/<n>-skill-check-hd`.
3. Commits atômicos: config, interface + `AddProgress`, `SkillCheck`, HUD, builder, teste, docs.
4. Compilação em batchmode numa cópia no scratchpad e `SkillCheckTest`.
5. PR com `Closes #<n>`, revisão da Frente 5.

## Como testar e ajustar

- Menu **Sombra da Sanidade > Rodar teste do skill check** (`SkillCheckTest`). Batchmode: `-executeMethod SombraDaSanidade.EditorTools.SkillCheckTest.RunBatch`.
- Números: `InteractionConfig`, seção "Skill check" (e `ejectLate*` para o ruído depois de 50%).
- Cenas geradas antes do #33: o `DriveServer` adiciona `SkillCheck` e `SkillCheckHud` no jogador ao iniciar, então não precisa regenerar.
- Para o rank (#49): `DriveServer.SkillCheckFailures` e o evento `SkillCheckResolved`.

## Verificação
- Batchmode compila e o `SkillCheckTest` passa (acerto, great, erro, ruído emitido, faixa menor depois do HD).
- Manual no Sandbox: aviso, ponteiro, acerto e erro; o monstro vem atrás do ruído da falha; a faixa encolhe no segundo HD.
- Build WebGL continua gerando (`WebGLBuilder`).
