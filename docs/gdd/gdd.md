# GDD resumido: Sombra da Sanidade

Versão base: **GDD v1.0 (12/10/2026)**, PDF completo em [GDD-v1.0.pdf](GDD-v1.0.pdf). Este arquivo é a referência rápida para implementação. Mudanças aprovadas entram aqui e na tabela de histórico.

## 1. Visão geral

- **High concept:** survival horror psicológico 2D top-down. Um auditor equilibra uma lanterna de risco duplo contra a sanidade, usa ferramentas elétricas para extrair 3 HDs e foge de um perseguidor imortal e cego atraído pela luz.
- **Plataforma:** PC e WebGL, Unity 2D URP.
- **Duração:** ~20 min vencendo direto; 35 a 45 min com mortes.
- **Diferencial:** Luz de Risco Duplo (luz protege a mente e atrai o monstro) + Revelação pela Loucura (pistas só aparecem com sanidade baixa).
- **Referências:** Darkwood, Resident Evil 2, Alien: Isolation, SOMA, Signalis, Yuppie Psycho.

## 2. Pilares

1. **Gestão de Luz e Sanidade:** luz revela e acalma, mas atrai. Escuro esconde, mas corrói.
2. **Tecnologia Experimental e Risco:** módulos químicos dão vantagem com custo psíquico.
3. **Engenharia sob Pressão:** consertar disjuntores cria Zonas Seguras, mas deixa o jogador parado e exposto.
4. **Fuga de Perseguidor Imortal:** não há como matar; quebrar linha de visão ou atordoar com EMP.

## 3. Core loop

Explorar com o cone de luz → apagar a luz e se esconder ao ouvir o monstro → EMP em emergência → consertar disjuntor (Zona Segura) e ejetar HD → monstro acelera → repetir até a eclusa.

## 4. Objetos e números

| Objeto | Atributos | Estados |
|---|---|---|
| Jogador (Thomas Reis) | vel. 4.0, sanidade 0-100, bateria 0-100, HDs 0-3 | Lúcido (100-75), Ansioso (74-40), Alucinando (39-15), Colapsado (14-1), Morto (0); Modo Foco (`Tab`) |
| Espécime Vazio (Voluntário 07) | vel. 3.2 a 4.6, raio auditivo, stun 0-4s | Patrulha Cega, Investigação Auditiva, Perseguição Furiosa, Atordoado |
| Sonâmbulo (Voluntário 03) | vel. 5.0 a 6.0 no escuro | Congelado (sob luz), Perseguição no Escuro |
| Câmera de Segurança | cone que varre, alarme 5s, EMP 10s | Varrendo, Detectando, Alarme (apagão / chama Espécime), Desligada |
| Lanterna Lux-90 | carga 0-100, cone 60°, alcance 8 | Apagada, Iluminando, Disparo EMP (-40), Esgotada |
| Disjuntor | tensão, fusível, timer de luz 0-60s | Queimado, Em Reparo (4s), Ativo (Zona Segura) |
| Servidor de HD | ejeção 12 s, até 3 skill checks de multímetro | Carregado, Ejetando, Vazio |

**Skill check do HD** (calibragem no multímetro, seção 4.4 do PDF; detalhes em [skill-check-hd.md](../skill-check-hd.md)): durante a ejeção, depois de 2 s, 25%/s de chance de um check (máx. 3). Bipe, o ponteiro varre o mostrador em 1,1 s e Espaço precisa cair na faixa: good 15/13/11% conforme os HDs já coletados, great 4% (+5% de progresso). Errar ou não apertar: -10% de progresso, 1 s parado, ruído de raio 12. Monstro perto encolhe a faixa em 40%; sanidade < 35 faz o ponteiro tremer. Depois de 50% o servidor faz ruído baixo (raio 6) a cada 3 s. Números iniciais, pendentes da aprovação do v1.1 (#3).

## 5. Regras

1. Espécime Vazio é imortal e cego (caça por som); Sonâmbulo congela sob qualquer luz e corre no escuro.
2. O cone de luz atrai o Espécime, mas paralisa o Sonâmbulo.
3. Escuro esconde o jogador, mas drena sanidade (-1.2/s a -2.0/s conforme a ala).
4. Modo Foco (`Tab`): reduz velocidade para 55%, corta o dreno no escuro pela metade e regenera sanidade até 60% com a lanterna acesa (quebrado por monstro próximo).
5. Pistas em fósforo, objetos reativos (`SanitySpriteSwap`) e falas abaladas só aparecem com sanidade < 35 (ou Módulo Neural).
6. Lâmpadas de teto criam áreas impenetráveis ao monstro por 60s; câmeras de segurança disparam apagão (Fase 2+) e chamam o Espécime (Fase 3+).

- **Vitória:** inserir os 3 HDs no terminal da eclusa, decidir se resgata ou abandona a Dra. Vance e alcançar a porta externa (finais *Testemunha* ou *Amostra entregue*).
- **Derrota:** sanidade chega a 0, monstro/Sonâmbulo encosta no jogador ou acaba o tempo da purga (150 s).

## 6. Economia

| Recurso | Fontes | Ralos |
|---|---|---|
| Bateria | Bateria sobressalente +30 | Luz ligada -1/s, EMP -40 |
| Sanidade | Zona Segura +2/s, Estabilizador +30 | Escuro -1.5/s, monstro perto -3/s, Adrena-Syn -20 |
| EMP | Usa a bateria (precisa de 40) | Clique direito no monstro; stun 4s |

| Item | Efeito | Custo |
|---|---|---|
| Adrena-Syn | +35% velocidade por 15s | -20 sanidade |
| Estabilizador de Lítio | +30 sanidade | lentidão por alguns segundos |

**Chance:** baterias/sedativos 60% por armário; monstro 70% segue ruído, 30% rota aleatória; alucinações a cada 15-25s com sanidade < 35.

Todos os números acima vivem em `Assets/_Project/ScriptableObjects/Config/`.

## 7. Progressão

| Fase | Mecânica nova | Desafio | Duração |
|---|---|---|---|
| 1. Ala Médica | Lanterna, furtividade, fusível, EMP | Aprender que a luz atrai o Espécime; HD-01 | ~6 min |
| 2. Ala de Energia | Sonâmbulo, câmeras com apagão, Adrena-Syn | Congelar o Sonâmbulo na luz; religar subestação; HD-02 | ~7 min |
| 3. Ala Executiva | Espécime + Sonâmbulo + câmeras, Módulo Neural | Dilema da luz; senhas com sanidade baixa; HD-03 | ~8 min |
| 4. Fuga da Eclusa | Purga 150 s e Escolha da Vance | Chegar à porta (direto ou salvando a Vance) | 2,5-4 min |

**Recompensa por HD** (Proposta 2 do v1.1, pendente da #3): ao ejetar cada HD o jogador escolhe 1 de 2 upgrades da Lux-90. HD-01: Lente Focal (alcance +25%) ou Célula Extra (bateria 130). HD-02: Capacitor Duplo (EMP 30) ou Bobina Longa (stun 5,5 s). HD-03: Filtro Âmbar (modo econômico que não atrai) ou Ressonador Neural (pistas com sanidade < 50). Ver [upgrades-lux90.md](../upgrades-lux90.md).

Curva dente-de-serra: cada HD é um pico de perigo, cada Zona Segura um alívio. Ajuste: abaixo de 15 de sanidade o monstro demora 5% a mais para procurar.

> Feedback do professor pediu progressão, dificuldade gradativa e recompensas mais claras. Ver [gdd-v1.1-propostas.md](gdd-v1.1-propostas.md).

## 8. Controles e interface

WASD move, mouse mira, F/clique esquerdo liga a lanterna, clique direito dispara EMP, E segura para interagir, Espaço calibra o multímetro no skill check, Tab segura para Modo Foco (amplia o relógio), H/botão `[-]` minimiza o relógio, Q liga o modo econômico (com o Filtro Âmbar), 1/2 escolhem o upgrade no terminal. Esc pausa (configurações de volume, brilho e tela cheia) ou pula cutscenes (segurar 0,8 s). HUD diegético: relógio Chrono-Vigil sempre visível no canto inferior direito com ECG, bateria, gavetas de HD, seta direcional 4 vias + sinal RF para o HD e timers de buffs com alerta nos 3 s finais.

Fluxo: Menu → C0 Abertura → Fase 1 (Ala Médica) → I1 → Fase 2 (Ala de Energia) → I2 → Fase 3 (Ala Executiva) → C3 Revelação → Eclusa → C4 / C5 → Laudo (Rank S/A/B/C).

## 9. Narrativa

1994. Thomas Reis acredita ser auditor externo da Aethelgard Dynamics enviado ao Sub-Nível 4 do Complexo EREBUS para recuperar 3 HDs do Projeto Hipnos. Na verdade ele é o **Voluntário 08**, com a memória apagada pela Dra. Valerie Vance — que sobreviveu presa na sala de observação e o guia pelos logs (`VANCE-01` a `03`) enquanto a empresa mede quanto ele aguenta. O arco se desenrola por falas internas do Thomas (`InnerVoiceLine` em variantes Estável/Abalado), pistas em fósforo, objetos reativos (`SanitySpriteSwap`), 3 memórias em paleta verde de 4 cores (`M1`–`M3`) e a revelação em `C3`, culminando na escolha final na Eclusa (*Testemunha* em `C4` ou *Amostra entregue* em `C5`). Ver [narrativa/roteiro-rc.md](../narrativa/roteiro-rc.md).

## 10. Técnico

- **Unity 6000.5.10f1**, C#, URP 2D (Light2D, ShadowCaster2D, Volume com Vignette e Chromatic Aberration). Ver [ADR 0001](../decisoes/0001-unity-6.md).
- IA com NavMeshPlus. Ver [ADR 0003](../decisoes/0003-navmeshplus.md).
- PlayerPrefs para áudio/brilho; JSON local para checkpoint entre alas.

## 11. Escopo MVP

| Funcionalidade | Prioridade |
|---|---|
| Movimento, cone de luz, dreno de sanidade | Essencial |
| IA perseguidor com NavMesh 2D e EMP | Essencial |
| Reparo de disjuntores e extração dos 3 HDs | Essencial |
| Alucinações e logs de áudio | Desejável |

Marcos: Protótipo (semana 4), Vertical slice com Ala Médica (semana 8), Versão final com 3 alas e build WebGL (semana 12).

## Histórico

| Versão | Data | Mudança |
|---|---|---|
| 1.0 | 12/10/2026 | GDD entregue |
| 1.0.1 | 27/10/2026 | Unity atualizada para 6000.5.10f1 (antes 2022.3/2023.x) |
| 1.0.2 | 05/11/2026 | Skill check de multímetro na ejeção de HD (12 s), números iniciais pendentes do v1.1 (#33) |
| 1.0.3 | 05/11/2026 | Upgrade da Lux-90 (1 de 2) ao ejetar cada HD, Proposta 2 pendente do v1.1 (#31) |
| 1.0.4 | 06/11/2026 | HUD e telas em pixel art (multímetro, prompt sem número, pausa com Esc), #63 e #64 |
| 1.0.5 | 09/11/2026 | Ala Médica do vertical slice: 1 min sem monstro e primeiro encontro roteirizado (Proposta 4), checkpoint JSON com "Progresso salvo" ao concluir a ala (Proposta 2, #45) |
| 1.0.6 | 09/11/2026 | Ala Executiva (#43): HD-03 com trava de senha, dígitos em pistas de fósforo (sanidade < 35) e Módulo Neural (-15 de sanidade, revela as pistas por 20 s); `DifficultyProfile` por ala com a tabela da Proposta 1 (#30) |
| 1.0.7 | 09/11/2026 | Fuga da Eclusa: terminal recebe os 3 HDs, purga de 150 s comunicada por sirene e luz (sem número), monstro em fúria (4.6, Proposta 1), Game Over próprio da purga e cena `Wings/Eclusa` (#44). Ver [eclusa.md](../eclusa.md) |
| 1.1.0 | 22/11/2026 | Release Candidate: encadeamento de 3 fases + Eclusa, Sonâmbulo, câmeras de segurança, sistema de áudio e trilha, HUD Chrono-Vigil fixo com Modo Foco (`Tab`), bússola 4 vias, monólogo do Thomas, objetos reativos à sanidade e 11 cutscenes/memórias do arco "Voluntário 08" (#88–#92, #99) |
