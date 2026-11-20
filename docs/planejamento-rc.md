# Planejamento do Release Candidate (Entrega 3)

Entrega: **23/11/2026, 19:59**. Substitui a divisão de frentes de [planejamento-rc-frentes.md](planejamento-rc-frentes.md) para o RC: o jogo passa a ter **3 fases separadas**, cada uma apresentando uma ameaça, e uma camada de narrativa com cutscenes e memórias do Thomas.

O que a entrega exige: pelo menos 3 níveis completos com objetivos claros, 5 a 7 min cada (mínimo de 20 min), ações do jogador e dos NPCs, recompensas e punições, efeitos sonoros, game over e restart, deploy web, vídeo no YouTube e apresentação.

## 1. Estrutura do jogo

Fluxo: Menu → C0 Abertura → Fase 1 → Ala concluída → Fase 2 → Ala concluída → Fase 3 → C3 Revelação → Eclusa (escolha) → C4 ou C5 → Laudo (rank).

| Fase | Ameaças | Mecânica nova | Beats (ordem) | Recompensa | Alvo |
|---|---|---|---|---|---|
| 1. Ala Médica | Espécime (Voluntário 07) | Lanterna, escuro e sanidade, disjuntor e Zona Segura, EMP, armários | 1 min calmo → M1 → C1 apresentação do Espécime → exploração livre → HD-01 (pico) → fuga até a saída | HD-01, upgrade 1, VANCE-01 | 6 min |
| 2. Ala de Energia | Sonâmbulo (Voluntário 03), câmeras | Luz que congela, câmeras com apagão, Adrena-Syn, bateria sob pressão | Sons do Espécime atrás das paredes → C2 apresentação do Sonâmbulo → 1ª câmera em área segura → M2 → subestação → HD-02 → apagão final | HD-02, upgrade 2, VANCE-02 | 7 min |
| 3. Ala Executiva | Espécime + Sonâmbulo + câmeras | O dilema da luz, senha com sanidade baixa, Módulo Neural | Os dois monstros longe um do outro → setores com câmera → M3 (espelho) → senha → HD-03 → C3 | HD-03, upgrade 3, VANCE-03 | 8 min |
| Eclusa | Os dois em fúria, câmeras | Purga de 150 s e escolha da Vance | Inserir os HDs → escolha → caminho direto, ou cela da Vance e atalho → porta externa | Final e conquista | 2,5 a 4 min |

O EMP entra na fase 1 (contra o Espécime). Na fase 2 ele não serve contra o Sonâmbulo: o jogador sente que a regra mudou sem precisar de tutorial. Ele volta a ser útil contra as câmeras.

### Curva de dificuldade

Valores iniciais, no `DifficultyProfile` de cada ala.

| | F1 | F2 | F3 | Eclusa |
|---|---|---|---|---|
| Velocidade do Espécime | 3.2 | - | 3.8 (+10% por HD) | 4.6 |
| Velocidade do Sonâmbulo no escuro | - | 5.0 | 5.5 | 6.0 |
| Câmeras | 0 | 2 a 3 (só apagão) | 4 a 5 (apagão + chamado) | todas |
| Chance de bateria por armário | 60% | 50% | 40% | - |
| Dreno de sanidade no escuro | -1.2/s | -1.5/s | -1.8/s | -2/s |

## 2. Sistemas novos

| Sistema | Comportamento | Ganchos existentes |
|---|---|---|
| `SecurityCamera` | Cone que varre. LED âmbar ao detectar e vermelho no alarme. Precisa de 0,6 s vendo o Thomas. Alarme de 5 s: apaga as luzes do setor e, da fase 3 em diante, chama o Espécime. Desliga no disjuntor do setor (até o fim) ou com EMP (10 s) | `Breaker`, `NoiseBus`, `FlashlightController.EmpFired` |
| Escolha da Vance | Terminal da Eclusa com 2 opções (1/2). Cela: segurar E por 6 s com 2 skill checks. Vance solta: câmeras do caminho desligadas e atalho aberto | `EclusaTerminal`, `SkillCheck`, `EscapeSequence` |
| Monólogo do Thomas | `InnerVoiceLine` (ScriptableObject): texto por faixa de sanidade, toca uma vez, legenda com bipe grave | `AudioLogPlayer` |
| Objeto que muda com a sanidade | Troca de sprite abaixo de um limite (espelho, crachá, foto, pulseira, prontuário) | `SanityClue` |
| `CutscenePlayer` | Lista de planos: imagem ou alvo da câmera, zoom e pan, duração, legenda, som, transição (corte, fade, glitch). Faixas pretas no motor, controle travado, segurar Esc pula | `CameraFollow`, `StoryIntro`, `PixelHud` |
| `MemoryEffect` | Volume com paleta de 4 cores, linhas de TV, quadros repetidos, áudio com passa-baixa | Volume do URP da sanidade |
| Encadeamento das fases | `WingCheckpoint` conclui a ala → "Ala concluída" → carrega a próxima cena. Continuar do menu lê o checkpoint | `Checkpoint`, `RunStats`, `MainMenuScreen` |
| Transição CRT | Fecha numa linha horizontal ao sair e abre ao entrar (0,6 s), também no Game Over | `ModalScreen` |

## 3. Narrativa: "Voluntário 08"

Thomas não é auditor: é a próxima cobaia do Projeto Hipnos, com a memória apagada, e a "auditoria" é o teste final. A Dra. Vance está viva, presa na sala de observação, guiou o Thomas pelos logs e foi quem apagou a memória dele.

| Momento | O jogador acredita | Pistas |
|---|---|---|
| F1 | Thomas é auditor; o 07 é o monstro | Falas leves ("Eu sabia o caminho da enfermaria?"), M1 |
| F2 | Há mais de uma cobaia, e algo está errado com ele | Espelho, "08" em fósforo, M2 (assinatura), VANCE-02 sobre o 03 |
| F3 | Thomas pode ser cobaia | M3, ficha do 08, VANCE-03 |
| Eclusa | Ele é o 08; a Vance está viva | C3 ao vivo e a escolha |

### Escolha da Vance

| | Deixar a Vance | Salvar a Vance |
|---|---|---|
| Caminho | Direto à porta externa | Desvio até a cela, do outro lado do mapa; abrir exige 6 s e 2 skill checks, com câmeras ligadas que chamam o Espécime |
| Ajuda | Nenhuma | Ela desliga as câmeras do caminho e abre um atalho até a eclusa. Sai pelo duto, sem escolta |
| Final | "Amostra entregue": Thomas escapa sozinho e a Aethelgard recebe os dados dele | "Testemunha": os dois saem com as provas. Conquista "A Verdade Corporativa" e bônus no rank |

### Câmeras: os olhos da Aethelgard

São o instrumento do experimento, a empresa "medindo quanto ele aguenta". Fase 1: não aparecem. Fase 2: o alarme apaga o setor (o Sonâmbulo corre no escuro). Fase 3: o alarme também chama o Espécime. Eclusa: ativas, salvo se a Vance foi solta.

### Roteiros a escrever

- Reescrever os 3 logs da Vance (VANCE-02 passa a falar do 03).
- Cerca de 18 falas do Thomas (6 por fase, 2 ou 3 versões por faixa de sanidade).
- Cerca de 8 pistas em fósforo ("08", assinatura do Thomas, "VOCÊ JÁ ESTEVE AQUI").
- Textos das cutscenes de painéis, das 3 memórias e dos 2 finais no laudo.

## 4. Cutscenes e direção

| Cena | Quando | Conteúdo | Tipo |
|---|---|---|---|
| C0 Abertura | Antes da fase 1 | Elevador desce ao Sub-Nível 4, briefing da Aethelgard, porta abre no escuro | Painéis |
| C1 Espécime | 1º encontro na fase 1 | A câmera vai até ele, ele vira a cabeça para o som, zoom | No motor |
| M1 Sala branca | Fase 1, perto da enfermaria | Luz forte, mão com luva, Vance: "Conte de 10 para trás, Thomas." | Memória |
| C2 Sonâmbulo | Começo da fase 2 | Parado sob a lanterna; a lanterna pisca e ele está mais perto | No motor |
| M2 Assinatura | Fase 2, escritório | Termo de consentimento, a caneta na mão do Thomas, o nome dele | Memória |
| M3 Espelho | Fase 3, sanidade < 35 | Reflexo careca, de avental, com a pulseira 08 | Memória |
| C3 Revelação | Depois do HD-03 | Ficha do Voluntário 08 com foto; a voz da Vance ao vivo, sem chiado | Painéis + motor |
| C4 / C5 Finais | Fim da Eclusa | "Testemunha" ou "Amostra entregue" | Painéis |

| Tipo | Linguagem visual |
|---|---|
| No motor | Faixas pretas, zoom lento, controle travado, ambiente abaixa e entra uma nota grave |
| Painéis (presente) | Pixel art 320x180 com pan lento, cortes secos, cores frias da paleta, legenda datilografada |
| Memórias (passado) | Paleta própria de 4 cores (branco hospitalar, verde fósforo), linhas de TV, quadros repetidos como fita gasta, cortes rápidos, áudio abafado, fim em glitch |

Cor fria é o presente, verde estourado é a memória: o jogador percebe sozinho quando está no passado.

## 5. Arte

| Asset | Quantidade | Formato |
|---|---|---|
| Painéis de cutscene (C0, C3, C4, C5) | cerca de 10 | 320x180, Aseprite |
| Painéis de memória (M1 a M3) | cerca de 9 | 320x180, paleta de memória |
| Câmera de segurança (parada, varrendo, alarme, desligada) | 4 estados | `art-src/` |
| Vance (sprite no mapa + retrato) | 2 | `art-src/` e Aseprite |
| Cela da Vance e porta | 1 | `art-src/` |
| Objetos que mudam com a sanidade | 5 pares | `art-src/` |
| Faixas pretas, borda de glitch, tela "Ala concluída" | UI | `PixelHud` |

Dois ADRs novos: painéis grandes saem do Aseprite (`art-src/Cutscenes/*.aseprite` + PNG exportado), exceção ao formato texto; e a paleta de memória de 4 cores, fora do `palette.json`.

## 6. Áudio

| Grupo | Sons |
|---|---|
| Jogador | Passos por piso, coleta de pilha, injeção do Adrena-Syn, Estabilizador, abrir armário |
| Lux-90 | Liga e desliga, EMP (carga e descarga), bateria baixa |
| Mundo | Disjuntor (reparo e estalo), ejeção do HD, câmera (motor, detecção, sirene), apagão, cela da Vance |
| Monstros | Respiração e passos do Espécime, rangido do Sonâmbulo ao se mover, som de morte |
| Música e ambiente | Drone por fase (cada vez mais denso), tema das memórias, vitória, derrota, purga |
| Vozes | Bipe da Vance (existe), bipe do Thomas (mais grave) |

Gerados por código (padrão do `SanityAudioHallucination`) ou CC0 com origem registrada.

## 7. Frentes do RC (4 integrantes)

| Frente | Tarefas |
|---|---|
| RC1. Jogador e monstros | `SecurityCamera`; Sonâmbulo nas cenas de ala e balanceamento com o Espécime; partículas do EMP e do skill check; cela da Vance e Vance no mapa; #74 e #37 |
| RC2. Níveis | Fase 1 só com o Espécime; fase 2 com Sonâmbulo e câmeras; fase 3 com os dois; Eclusa com cela, atalho e caminho direto; gatilhos de memória, falas e pistas, e playtest cronometrado |
| RC3. Narrativa e cutscenes | `CutscenePlayer` e `MemoryEffect`; monólogo do Thomas e objetos que mudam com a sanidade; roteiros e reescrita dos logs; montar as 11 cutscenes; GDD v1.1.0 (seções 4, 5, 7 e 9) |
| RC4. Partida, som e build | Encadeamento, "Ala concluída" e Continuar; transição CRT; escolha da Vance, finais e conquistas no laudo; pacote de áudio; build WebGL, Pages, roteiro do vídeo e slides |
| Arte compartilhada | Cada frente desenha os assets do próprio sistema. Painéis: RC1 desenha C0, RC2 desenha M1 a M3, RC3 desenha C3, RC4 desenha C4 e C5 |

Issues (milestone "Versão final", labels `rc:*`):

| Frente | Issues |
|---|---|
| RC1 | #79 câmera, #80 Sonâmbulo e balanceamento (absorve #37 e #74), #81 partículas, #82 cela da Vance |
| RC2 | #83 fase 1, #84 fase 2, #85 fase 3, #86 Eclusa, #87 gatilhos e playtest |
| RC3 | #88 CutscenePlayer, #89 monólogo e objetos, #90 roteiros (fecha #40), #91 cutscenes, #92 GDD, #99 ADRs |
| RC4 | #93 encadeamento, #94 transição CRT, #95 escolha e finais, #96 áudio, #97 build e artefatos |
| Arte | #98 painéis |

Dependências: o encadeamento (RC4) e o `CutscenePlayer` (RC3) vêm primeiro, porque destravam o resto. Os roteiros vêm antes dos painéis.

## 8. Critério de pronto

1. A partida completa, do menu ao laudo, roda no WebGL sem erro no console.
2. Cada fase dura entre 5 e 8 min num playtest de quem não participou dela.
3. As 11 cutscenes tocam e podem ser puladas.
4. Os dois finais são alcançáveis.
5. Toda ação do jogador tem som.
