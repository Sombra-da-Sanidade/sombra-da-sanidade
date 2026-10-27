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
| Jogador (Thomas Reis) | vel. 4.0, sanidade 0-100, bateria 0-100, HDs 0-3 | Lúcido (100-75), Ansioso (74-40), Alucinando (39-15), Colapsado (14-1), Morto (0) |
| Espécime Vazio | vel. 3.5 a 4.8, raio auditivo, stun 0-4s | Patrulha Cega, Investigação Auditiva, Perseguição Furiosa, Atordoado |
| Lanterna Lux-90 | carga 0-100, cone 60°, alcance 8 | Apagada, Iluminando, Disparo EMP (-40), Esgotada |
| Disjuntor | tensão, fusível, timer de luz 0-60s | Queimado, Em Reparo (4s), Ativo (Zona Segura) |

## 5. Regras

1. Monstro imortal e cego.
2. O cone de luz atrai o monstro se tocá-lo.
3. Escuro esconde o jogador, mas drena sanidade (-1.5/s).
4. Cada HD coletado acelera o monstro em +10%.
5. Pistas de enigma só aparecem com sanidade < 35.
6. Lâmpadas de teto criam áreas impenetráveis ao monstro por 60s.

- **Vitória:** inserir os 3 HDs no terminal da eclusa e alcançar a porta externa.
- **Derrota:** sanidade chega a 0 ou o monstro encosta no jogador.

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
| 1. Ala Médica | Lanterna, furtividade, fusível | Aprender que a luz atrai; HD-01 | 3-5 min |
| 2. Ala de Energia | EMP, disjuntores | Stun no tempo certo; religar subestação; HD-02 | 5-7 min |
| 3. Ala Executiva | Módulos neurais, Revelação pela Loucura | Senhas com sanidade baixa; HD-03 | 5-7 min |
| 4. Fuga da Eclusa | Corrida cronometrada | Chegar à porta antes da purga | 2-3 min |

Curva dente-de-serra: cada HD é um pico de perigo, cada Zona Segura um alívio. Ajuste: abaixo de 15 de sanidade o monstro demora 5% a mais para procurar.

> Feedback do professor pediu progressão, dificuldade gradativa e recompensas mais claras. Ver [gdd-v1.1-propostas.md](gdd-v1.1-propostas.md).

## 8. Controles e interface

WASD move, mouse mira, F/clique esquerdo liga a lanterna, clique direito dispara EMP. HUD diegético: medidor de bateria e contador de HDs no canto superior esquerdo; sanidade comunicada por vinheta, batimentos e aberração cromática. Posição do monstro e timers nunca aparecem em número.

Fluxo: Menu → Briefing → Jogo (Médica → Energia → Executiva → Eclusa) → Vitória (estatísticas) / Game Over (causa e reinício do checkpoint da ala).

## 9. Narrativa

Anos 90. Thomas Reis, auditor da Aethelgard Dynamics, vai ao Sub-Nível 4 do Complexo EREBUS. A base entra em confinamento; o Voluntário 07 do Projeto Hipnos (o Espécime Vazio) massacrou a equipe. Logs da Dra. Valerie Vance revelam o projeto, a falha de contenção e a conspiração (Thomas foi enviado de propósito). **Pendente:** definir a reviravolta exata do Ato 3.

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
