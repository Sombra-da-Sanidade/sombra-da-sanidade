# Dificuldade por ala

Frente 5, issue #30. Um `DifficultyProfile` por ala, com os valores da [Proposta 1](gdd/gdd-v1.1-propostas.md). Os valores são os iniciais para playtest e podem mudar com a aprovação da #3.

## Assets

Ficam em `Assets/_Project/ScriptableObjects/Config/`. Ajustar um número é só editar o asset no Inspector.

| Campo | `DifficultyMedica` | `DifficultyEnergia` | `DifficultyExecutiva` | `DifficultyEclusa` |
|---|---|---|---|---|
| `monsterSpeed` | 3.5 | 3.85 | 4.2 | 4.6 |
| `monsterHearingRadius` | 5 | 6 | 7 | 9 |
| `monsterBehaviours` | Médica | Energia | Executiva | Executiva |
| `darknessDrain` (/s) | 1.0 | 1.5 | 2.0 | 2.0 |
| `hallucinations` | não | sim | sim | sim |
| `hallucinationInterval` (s) | | 20-30 | 15-25 | 10-15 |
| `lootTable` | `LootTable_Medica` (70% bateria) | `LootTable_Energia` (60%) | `LootTable_Executiva` (45%) | `LootTable_Eclusa` (0%) |
| `wing` | Medica | Energia | Executiva | Eclusa |
| `batteryChance` | 0.7 | 0.6 | 0.45 | 0 |
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

- **Velocidade:** com perfil, o bônus de +10% por HD do `MonsterConfig` não vale. A tabela já inclui esse aumento (3.5, 3.85, 4.2), e somar os dois daria 4.2 já na Ala de Energia. Assim o monstro acelera ao mudar de ala, não ao ejetar o HD. O teto `maxSpeed` e o galope da perseguição continuam valendo.
- **Armários com tabela própria:** a tabela posta pelo builder no armário vence a do perfil. Isso mantém a cena Beta, que tem várias alas no mesmo mapa, com a tabela certa em cada armário.

## Teste

Menu **Sombra da Sanidade > Rodar teste de dificuldade**, ou em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.DifficultyProfileTest.RunBatch
```
