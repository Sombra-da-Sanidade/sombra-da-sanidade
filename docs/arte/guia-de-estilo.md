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
- **Personagens:** 24 a 28 px de altura dentro de um frame 16x32, largura de 10 a 14 px. Cabeça grande (cerca de 1/3 da altura), como em Yuppie Psycho. Espécime Vazio bem maior que Thomas (36 a 40 px, frame 24x40), membros finos e longos.
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
4. **Silhueta por massa escura, não por contorno:** personagens se destacam por blocos escuros (cabelo, terno, casaco), não por linha preta em volta de tudo. Objetos interativos podem ter contorno escuro (`0`, `1`, `p`). Chão e paredes não têm outline.
5. **Menos ruído:** textura em grupos de 2 a 3 pixels, nunca pixel solto aleatório ("sal e pimenta"). Deixe áreas lisas para o olho descansar.
6. **Detalhe concentrado:** o objeto que importa (disjuntor, HD, porta) recebe o detalhe; o entorno fica simples.
7. **Variações de tile:** um tile liso dominante (60%+) e variações raras (rachadura, mancha, junta). Repetição visível quebra a imersão.
8. **Animação:** poucos frames, bem escolhidos (idle 2 a 4, andar 4 a 6). Exagero na pose, não na quantidade.
9. **Uma cor de destaque por personagem:** corpo em tons escuros e um único acento saturado (o laranja do casaco do herói do Penusbmic, a gravata vermelha em Yuppie Psycho). Para Thomas: terno escuro, lanterna em âmbar.
10. **Sombra no chão:** elipse escura (`1` ou `2`) sob os pés de todo personagem e objeto solto. Ancora o sprite no top-down.
11. **Rosto mínimo:** em jogo, olhos de 1 a 2 px e quase nada mais. O detalhe do rosto vai para o retrato de diálogo (64 a 96 px), como em Yuppie Psycho.

### O que as referências mostram

| Referência | Observação | Como aplicamos |
|---|---|---|
| Yuppie Psycho, lanterna no escuro | Feixe largo e suave; fora dele tudo vira névoa cinza-azulada | Light2D com borda suave, Global Light baixa e azulada |
| Yuppie Psycho, sangue | Vermelho saturado é a única cor forte da tela | Rampa Emergência só para perigo e sangue |
| Yuppie Psycho, personagens | ~24 px, cabeça grande, sem outline, sombra simples | Escala de personagem acima |
| Penusbmic, top-down sombrio | Luz como manchas chapadas de borda pixelada, sem gradiente liso; fundo dessaturado | Pixel Perfect Camera com upscale para a luz também sair pixelada |
| Penusbmic, personagens | Silhueta escura compacta, um acento de cor, sombra elíptica | Regras 4, 9 e 10 |
| Penusbmic, paletas | 6 a 10 cores por objeto, rampas curtas com hue shift | Regra 1 e 2; cada sprite usa só uma parte da paleta |

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

Imagens para estudo ficam em `docs/referencias/arte/` (local, ignorado). O arquivo `FONTES.md` dessa pasta diz de onde veio cada uma: screenshots da página Steam do Yuppie Psycho e previews das páginas do Penusbmic no itch.io.


- Yuppie Psycho, Baroque Decay (2019): [página na Wikipedia](https://en.wikipedia.org/wiki/Yuppie_Psycho). Observar: escritório em 3/4, paredes altas, retratos de diálogo, cutscenes animadas, humor grotesco.
- Penusbmic: [itch.io](https://penusbmic.itch.io/). Observar: séries *The DARK Series* e *Sci-Fi Series*, tutoriais "Designing Palettes", "Tileset Workflow", "Character Design".
- Signalis e Darkwood: atmosfera, uso de luz e cor como informação.
