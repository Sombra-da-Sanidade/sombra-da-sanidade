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

## Pendências

- **Zona Segura do disjuntor (Frente 4):** a lâmpada de teto é só desenho até o `Breaker` acender a luz e ligar o `MonsterRepellent`.
