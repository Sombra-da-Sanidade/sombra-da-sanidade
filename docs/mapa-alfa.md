# Mapa do alfa

Cena `Assets/_Project/Scenes/Main/Alfa.unity`, montada com os tiles do laboratório (`art-src/Tiles/lab_tiles.json`). É uma cena separada da `Demo.unity`, que é da Frente 4, para testar o loop completo com arte. A Frente 4 pode adotar a planta depois.

## Planta

44×30 tiles (16 px = 1 unidade), pequena de propósito para o alfa:

| Área | Onde | O que tem |
|---|---|---|
| Saguão | embaixo, no centro | Início do jogador, eclusa (porta dupla na parede norte, com faixa de perigo), estabilizador, pista "ELE SEGUE A LUZ" |
| Ala Médica | à esquerda | HD-01, 1 disjuntor com lâmpada, estabilizador, pista do fusível |
| Ala de Energia | à direita | HD-02, 2 disjuntores com lâmpada |
| Ala Executiva | em cima | HD-03, onde o monstro começa |

Corredores de 3 tiles de largura fecham um loop: Saguão → Médica → Executiva → Energia → Saguão. Não há becos sem saída (GDD 4.1), e o monstro grande passa por todos os corredores. São 14 pontos de patrulha, nos cruzamentos e no meio de cada sala.

## Como mudar o mapa

1. Edite `art-src/Maps/alfa.txt`. Cada caractere é um tile, e a legenda está no começo do arquivo: `#` parede, `.` chão, `,` corredor, `S` servidor de HD, `K` disjuntor, `E` eclusa, `@` jogador, `M` monstro, `m` patrulha etc.
2. No Unity, use **Sombra da Sanidade > Gerar cena Alfa (tiles)**.

O gerador (`Scripts/Editor/AlfaSceneBuilder.cs`) faz:
- **Paredes em 3/4:** a primeira célula de parede acima do chão vira face de baixo (servidor, disjuntor, terminal, canos ou porta), a segunda vira face de cima e a borda vira topo. O miolo das paredes fica preto.
- **Colisão e sombra:** as paredes são juntadas em retângulos com `BoxCollider2D` e `ShadowCaster2D`, na layer `Walls`. A lanterna e a alucinação respeitam esses retângulos.
- **NavMesh:** os tilemaps `Floor` (andável) e `Walls` (Not Walkable) têm `NavMeshModifier`, e o bake acontece ao carregar a cena.
- **Jogador, monstro, HUD e sanidade:** vêm do `PrototypeSceneBuilder`, então a cena se comporta igual ao protótipo.

Teste em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.SmokeTest.RunBatchAlfa
```

## Ala Médica (issue #12)

Cena `Assets/_Project/Scenes/Wings/AlaMedica.unity`, gerada pelo mesmo builder a partir de `art-src/Maps/ala-medica.txt` (menu **Sombra da Sanidade > Gerar cena Ala Médica (tiles)**). 72×50 tiles.

| Sala | Onde | O que tem |
|---|---|---|
| Antessala | embaixo | Início do jogador, porta para o saguão, estabilizador, armário |
| Enfermaria | oeste | Disjuntor com lâmpada, 3 armários |
| Laboratório | leste | Disjuntor com lâmpada, estabilizador, 2 armários |
| Salão central | meio | Disjuntor com lâmpada (atalho entre as alas laterais) |
| Arquivo | em cima | HD-01, onde o monstro dorme até o primeiro encontro |

- **Dois loops:** o anel externo (Antessala → Enfermaria → Arquivo → Laboratório → Antessala) e o atalho pelo Salão central, que divide o anel em duas voltas menores. Sem becos (GDD 4.1).
- **Saída:** a porta abre com 1 HD (`GameManager.drivesRequired = 1`, definido pelo builder).
- **Armários (`A`):** loot pela tabela da ala (bateria +30 ou sedativo, #29 e #46).
- **Introdução (Proposta 4 do v1.1, `Level/WingIntro`):** o monstro começa adormecido (invisível, parado, sem colisão). Depois de 60 s, quando o jogador entra na Enfermaria (gatilho `x`) ou no Laboratório (`y`), ele acorda no corredor norte e passa pelo fundo da sala, perto da lâmpada, até o Salão central (rota `1 2 3` ou `4 5 6`). Se o jogador demorar 100 s ou pegar o HD antes, ele acorda na rota mais longe do jogador.
- **Primeiro encontro:** na rota ele ignora ruído. Se for iluminado, dá o grito de estática e vai investigar onde o jogador estava, sem Perseguição; depois de 4 s a luz volta a valer a regra normal. Sem luz, termina a rota e começa a patrulha.
- **Checkpoint (#45, Proposta 2):** ao abrir a porta com o HD-01, `Level/WingCheckpoint` salva `checkpoint.json` em `Application.persistentDataPath` (no WebGL, IndexedDB) e a tela de vitória mostra **ALA MÉDICA CONCLUÍDA** e **PROGRESSO SALVO**. Carregar o checkpoint fica para as outras alas (`Checkpoint.TryLoad` já existe).
- **Duração:** a meta de 3 a 5 min (GDD 7) precisa ser medida no playtest (o tempo aparece na tela de vitória). A introdução já garante pelo menos 1 min. Se ficar curta, alongar os corredores do anel no `.txt`; se passar de 5 min, reduzir `calmSeconds` do `WingIntro` no builder.

Teste em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.SmokeTest.RunBatchAlaMedica
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.AlaMedicaIntroTest.RunBatch
```

## Ala de Energia (issue #42)

Cena `Assets/_Project/Scenes/Wings/AlaEnergia.unity`. Ela é gerada a partir de `art-src/Maps/ala-energia.txt` pelo menu **Sombra da Sanidade > Gerar cena Ala de Energia (tiles)**. O mapa tem 72×50 tiles e segue o padrão da Médica.

| Sala | Onde | O que tem |
|---|---|---|
| Antessala | embaixo | Início do jogador, porta para o saguão, **disjuntor a 3,6 m do início** (Proposta 4: primeiro reparo sem pressão), armário |
| Casa de Máquinas | oeste | Disjuntor com lâmpada, 3 armários, estabilizador, bobina de cabo |
| Transformadores | leste | Disjuntor com lâmpada, 2 armários, estabilizador, transformadores |
| Controle A e B | meio | Duas salas lado a lado, separadas por uma parede de 3 tiles com o **atalho só do monstro** (`(` e `)` na planta). Adrena-Syn no Controle B |
| Subestação | em cima | Console da subestação (`U`), servidor do HD-02 e o monstro |

**Caminhos:**
- **Três loops, sem becos:** o anel externo (Antessala → Casa de Máquinas → Subestação → Transformadores → Antessala) e duas voltas menores, Casa → Controle A → Subestação e Antessala → Controle B → Transformadores.
- **Atalho do monstro:** o jogador dá a volta entre os dois Controles, mas o monstro atravessa a parede pelo atalho.

**Objetivo (GDD 7, Fase 2):**
- **Ordem:** religar a subestação, ejetar o HD-02 e voltar à porta. Uma linha no topo da tela (`Level/WingObjectives`) mostra o passo atual por 8 s sempre que ele muda.
- **Subestação (`Interaction/Substation`):**
  - Segurar E por 6 s. Soltar ou se mexer zera o progresso.
  - É barulhento: um estalo no começo (raio 10), descargas a cada 1,5 s (raio 10) e um zumbido ao ligar (raio 12).
  - O monstro começa na mesma sala, então ele chega antes do fim. O EMP dá 4 s de stun: precisa ser disparado quando ele chega, e não antes.
  - Até a subestação voltar, o servidor fica "sem energia" (`DriveServer.requiredPower`, com o LED vermelho escuro) e não deixa ejetar.

**Dificuldade (#30, Proposta 1):**
- O `GameManager` recebe o `Difficulty_Energia` e aplica os valores numa cópia do `MonsterConfig` e do `SanityConfig`: velocidade 3.85, raio auditivo 6, dreno de 1.5/s no escuro e alucinação a cada 20 a 30 s.
- O mesmo perfil dá ao monstro os comportamentos da Energia (`BreakerCheck` e `Shortcuts`, #37).
- O monstro está presente desde a entrada.
- São **3 disjuntores**, conforme a tabela da Proposta 1 (o mapa Alfa tinha 2). O builder acusa erro se a planta não bater com `breakersAvailable`.

**HD-02:**
- O `GameManager.TotalDrives` soma os HDs de alas anteriores: vem do checkpoint ou, se a cena for aberta direto, do `priorDrives = 1`.
- Assim o HD da cena toca o VANCE-02, abre a escolha Capacitor Duplo / Bobina Longa e usa o skill check do HD-02.
- A porta abre com 1 HD.

**Checkpoint (#45):** ao concluir, salva `lastWing = Energia`, `nextWing = Executiva` e `drives = 2`. Se o checkpoint aponta para a Energia, o `GameManager` lê os HDs dele.

**Arte nova:**
- Tiles em `lab_tiles.json`: `floor_cables`, `wall_face_panel`, `substation_top`, `substation_off`, `substation_on` e o decal `warning_high_voltage`.
- Props em `props.json`: `transformer` e `cable_coil`.
- Os chars novos estão na legenda do `.txt`.

**Duração:** a meta é de 5 a 7 min (GDD 7) e precisa ser medida no playtest. Se ficar curta, aumentar `repairDuration` ou os raios de ruído da `Substation`. Se passar, encurtar os corredores do anel no `.txt`.

Teste em batch:

```bash
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.AlfaSceneBuilder.BuildAlaEnergia -quit
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.AlaEnergiaTest.RunBatch
Unity -batchmode -projectPath . -executeMethod SombraDaSanidade.EditorTools.SmokeTest.RunBatchAlaEnergia
```

## Pendências

- **Zona Segura do disjuntor (Frente 4):** a lâmpada de teto é só desenho até o `Breaker` acender a luz e ligar o `MonsterRepellent`.
