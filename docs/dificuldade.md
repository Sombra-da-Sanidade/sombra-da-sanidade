# Dificuldade por ala

Frente 5, issue #30. Um `DifficultyProfile` por ala, com os valores da [Proposta 1](gdd/gdd-v1.1-propostas.md). Os valores são os iniciais para playtest e podem mudar com a aprovação da #3.

## Assets

Em 23/11 as duas primeiras fases ficaram mais difíceis:
- **Médica:** o Espécime ficou mais rápido e com o ouvido melhor, o escuro drena mais a sanidade, os armários dão menos bateria e ele acorda depois de 35 s (antes, 60 s). Ao pegar o HD-01 antes do encontro, ele surge na mesma sala, atrás do Thomas, e a patrulha dele não se afasta mais de 14 m do Thomas (`monsterLeashDistance`).
- **Energia:** o Sonâmbulo ficou mais rápido, o escuro drena mais e os armários dão menos bateria.

Ficam em `Assets/_Project/ScriptableObjects/Config/`. Ajustar um número é só editar o asset no Inspector.

| Campo | `DifficultyMedica` | `DifficultyEnergia` | `DifficultyExecutiva` | `DifficultyEclusa` |
|---|---|---|---|---|
| `monsterSpeed` | 3.6 | 3.85 (dorme) | 3.8 | 4.6 |
| `sleepwalkerSpeed` | - | 6.0 | 5.5 | 6.0 |
| `monsterHearingRadius` | 6.5 | 6 | 7 | 9 |
| `monsterLeashDistance` (m) | 14 | 0 (livre) | 0 | 0 |
| `monsterBehaviours` | Médica | Energia | Executiva | Executiva |
| `darknessDrain` (/s) | 1.3 | 1.8 | 2.0 | 2.0 |
| `hallucinations` | não | sim | sim | sim |
| `hallucinationInterval` (s) | | 20-30 | 15-25 | 10-15 |
| `lootTable` | `LootTable_Medica` (55% bateria) | `LootTable_Energia` (45%) | `LootTable_Executiva` (45%) | `LootTable_Eclusa` (0%) |
| `wing` | Medica | Energia | Executiva | Eclusa |
| `batteryChance` | 0.55 | 0.45 | 0.45 | 0 |
| `breakersAvailable` | 3 | 3 | 2 | 0 |
| `monsterDelaySeconds` | 0 | 0 | 0 | 0 |

Campos usados pelos builders e testes das alas (Frente 4):

- **`wing`:** casa o perfil com o checkpoint (#45).
- **`batteryChance`:** o teste da ala confere que a `lootTable` bate com ela.
- **`breakersAvailable`:** o builder confere com os disjuntores da planta.
- **`monsterDelaySeconds`:** fica 0 na Médica, porque o primeiro minuto sem monstro é feito pelo `WingIntro` da cena (ver [mapa-alfa.md](mapa-alfa.md)).

## Como usar

- **Frente 4:** arraste o asset da ala para o campo `Difficulty` do `GameManager` da cena, de preferência no builder. Sem perfil, a cena continua com os configs de sempre, como a `Alfa.unity` e o protótipo.
- **Quem lê:** `DifficultyProfile.Active` devolve o perfil ativo, ou `null` se não houver. Com `null`, use o valor do próprio config.
- **Troca em jogo:** `GameManager.SetProfile(perfil)` dispara o evento `ProfileChanged`.

## Quem lê cada valor

| Valor | Lido por |
|---|---|
| Velocidade, raio auditivo e comportamentos | `MonsterController` (também ao trocar de perfil) |
| Dreno no escuro | `SanityController` |
| Alucinações | `SanityHallucination` |
| Tabela de loot | `Locker`, no `Start`, só se o armário não tiver tabela própria |

## Decisões

- **Velocidade:** com perfil, o bônus de +10% por HD do `MonsterConfig` não vale. A tabela já tem o valor de cada ala, e somar os dois aceleraria duas vezes. Assim o monstro muda de velocidade ao mudar de ala, não ao ejetar o HD. No RC (#80) a curva segue o `planejamento-rc` seção 1: o Espécime é mais lento na fase 3 (3.8) porque divide a ala com o Sonâmbulo, e na Energia ele só dorme. O teto `maxSpeed` e o galope da perseguição continuam valendo.
- **Armários com tabela própria:** a tabela posta pelo builder no armário vence a do perfil. Isso mantém a cena Beta, que tem várias alas no mesmo mapa, com a tabela certa em cada armário.

## Teste

Menu **Sombra da Sanidade > Rodar teste de dificuldade**, ou em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.DifficultyProfileTest.RunBatch
```

## Sonâmbulo por ala (#80)

- `sleepwalkerSpeed`: velocidade do Sonâmbulo no escuro (planejamento-rc 1). 0 usa a do `SleepwalkerConfig` (7.5, a da Beta).
- Na fase 3 os dois monstros começam a cerca de 40 m um do outro. O agente do Sonâmbulo tem `avoidancePriority` 60 (o Espécime usa 50), então um desvia do outro no NavMesh em vez de travarem num corredor.

