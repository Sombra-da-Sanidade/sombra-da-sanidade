# Guia de estilo visual

Direção de arte de Sombra da Sanidade. Base: GDD seção 9 (pixel art 16-bit, paleta fria: cinza-concreto, verde-fósforo, azul-elétrico, vermelho de emergência). Referências de estilo: Yuppie Psycho (Baroque Decay) e o trabalho do pixel artist Penusbmic, além de Signalis e Darkwood.

> Imagens de referência ficam só na sua máquina, em `docs/referencias/arte/` (ignorado pelo git). Elas têm direitos autorais: **nunca** copie pixels, só estude proporção, paleta e técnica.

## 1. Pilares visuais

Cada regra de arte reforça uma regra de jogo:

| Pilar | Na arte | Por quê (design) |
|---|---|---|
| Luz é segurança | Áreas iluminadas usam tons quentes (âmbar) e contraste alto | O jogador lê a Zona Segura como recompensa |
| Escuro é ameaça | Fora da luz, tudo cai para azul-acinzentado quase preto | O dreno de sanidade precisa ser sentido, não só lido no HUD |
| Cor é informação | Verde = funciona/objetivo (HD, LED, disjuntor ativo); vermelho = perigo/quebrado; azul = eletricidade/EMP | Feedback do professor: recompensas e progresso claros |
| Silhueta primeiro | Objetos interativos têm contorno escuro e destacam do chão | Leitura rápida sob lanterna em movimento |

**Regra de ouro:** se uma cor saturada aparece, ela significa algo. Cenário é cinza; vermelho, verde, azul e âmbar são reservados para estado e interação.

## 2. Escala e técnica

- **Tile:** 16x16 px. **PPU:** 16 (1 tile = 1 unidade do Unity).
- **Personagens:** cerca de 32 px de altura (2 tiles), largura de 12 a 16 px. Espécime Vazio maior que Thomas (36 a 40 px), membros finos e longos.
- **Câmera:** Pixel Perfect Camera, resolução base 320x180 (escala inteira para 1280x720 e 1920x1080).
- **Perspectiva:** top-down 3/4, como Yuppie Psycho: chão visto de cima, paredes mostram a face frontal (2 tiles de altura) com topo escuro.
- **Importação no Unity:** Point (sem filtro), sem compressão, sem mipmap. O `PixelArtImporter` aplica isso sozinho.

## 3. Paleta

32 cores, definidas em [`art-src/palette.json`](../../art-src/palette.json). Arquivo `.gpl` para Aseprite/LibreSprite/GIMP: [`art-src/sombra.gpl`](../../art-src/sombra.gpl). Use **só** essas cores.

| Rampa | Chaves | Uso |
|---|---|---|
| Concreto (9 tons) | `0` a `8` | Cenário, metal, outline. Sombras puxam para o azul (hue shift frio) |
| Fósforo | `a` a `e` | Terminais, LEDs, HDs, disjuntor ativo |
| Elétrico | `f` a `j` | Água, faíscas, EMP, uniforme de Thomas |
| Emergência | `k` a `o` | Sangue, alarme, disjuntor queimado, luz de emergência |
| Pele | `p` a `s` | Personagens humanos, couro, madeira |
| Âmbar | `t`, `u` | Lâmpadas (Zona Segura), faixas de aviso |
| Hipnos | `v`, `w` | Carne do Espécime Vazio, alucinações |

## 4. Regras de desenho

Tiradas da prática do Penusbmic (tutoriais de paleta, tileset e personagem) e da leitura do Yuppie Psycho:

1. **Três tons por material, no mínimo:** base, sombra e brilho. Metal ganha um quarto tom de brilho especular.
2. **Hue shift:** sombra não é só "mais escura", é mais fria (azul/roxo). Brilho é mais quente.
3. **Luz vem de cima (norte):** bordas superiores claras, inferiores escuras. Sem *pillow shading* (sombra em volta de tudo).
4. **Outline seletivo:** objetos e personagens com contorno escuro (`0`, `1`, `p`), nunca preto puro em área iluminada. Chão e paredes não têm outline.
5. **Menos ruído:** textura em grupos de 2 a 3 pixels, nunca pixel solto aleatório ("sal e pimenta"). Deixe áreas lisas para o olho descansar.
6. **Detalhe concentrado:** o objeto que importa (disjuntor, HD, porta) recebe o detalhe; o entorno fica simples.
7. **Variações de tile:** um tile liso dominante (60%+) e variações raras (rachadura, mancha, junta). Repetição visível quebra a imersão.
8. **Animação:** poucos frames, bem escolhidos (idle 2 a 4, andar 4 a 6). Exagero na pose, não na quantidade.

## 5. Inventário de assets

Status: rascunho (R), revisado (V), final (F).

| Asset | Arquivo fonte | Status |
|---|---|---|
| Paleta | `art-src/palette.json` | R |
| Tileset laboratório (21 tiles) | `art-src/Tiles/lab_tiles.json` | R |
| Thomas Reis: idle, andar 4 dir., reparo | `art-src/Sprites/thomas.json` | a fazer |
| Espécime Vazio: patrulha, corrida, atordoado | `art-src/Sprites/especime.json` | a fazer |
| Itens: bateria, estabilizador, Adrena-Syn, HD | `art-src/Sprites/itens.json` | a fazer |

Tiles do laboratório: `floor_a`, `floor_seam`, `floor_b` (rachadura), `floor_c` (mancha), `floor_grate`, `floor_hazard`, `wall_top`, `wall_face`, `wall_face_pipes`, `wall_terminal`, `breaker_off`, `breaker_on`, `door_top`, `door_bottom`, `server_top`, `server_bottom`, `server_bottom_empty`, `puddle`, `blood_smear`, `papers`, `ceiling_lamp`.

## 6. Como editar ou criar arte

Ver [CONTRIBUTING.md, seção Arte](../../CONTRIBUTING.md#arte). Resumo:

1. Edite o JSON em `art-src/` (cada caractere é um pixel, chave da paleta) ou desenhe no Estúdio de Arte / Aseprite e converta.
2. `python3 tools/art/export_sprites.py` gera o PNG em `Assets/_Project/Art/`.
3. Abra o Unity: o PNG é fatiado e configurado sozinho. Teste em **Sombra da Sanidade > Arte > Gerar cena de teste de arte**.
4. PR com escopo `art`, com print da cena de teste.

## 7. Referências (só links)

- Yuppie Psycho, Baroque Decay (2019): [página na Wikipedia](https://en.wikipedia.org/wiki/Yuppie_Psycho). Observar: escritório em 3/4, paredes altas, retratos de diálogo, cutscenes animadas, humor grotesco.
- Penusbmic: [itch.io](https://penusbmic.itch.io/). Observar: séries *The DARK Series* e *Sci-Fi Series*, tutoriais "Designing Palettes", "Tileset Workflow", "Character Design".
- Signalis e Darkwood: atmosfera, uso de luz e cor como informação.
