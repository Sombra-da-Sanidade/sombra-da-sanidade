# Roteiro do RC: "Voluntário 08"

Issue #90. Base: [planejamento-rc.md](../planejamento-rc.md) seções 3 e 4. **Status: rascunho para o grupo revisar.** Mudanças de texto entram aqui primeiro; o código lê dos assets (`ScriptableObjects/Narrative/`) e das cutscenes.

## Resumo da história

Thomas Reis acha que é auditor da Aethelgard Dynamics, mandado ao Sub-Nível 4 do Complexo EREBUS para recuperar 3 HDs do Projeto Hipnos. Na verdade ele é o **Voluntário 08**, a próxima cobaia, com a memória apagada. A "auditoria" é o teste final: a empresa quer medir quanto ele aguenta. A Dra. Valerie Vance está viva, presa na sala de observação. Foi ela quem apagou a memória dele, e é ela quem o guia pelos logs, porque quer que alguém saia com a verdade.

| Ameaça | Quem é |
|---|---|
| Espécime Vazio | Voluntário 07. Cego, caça pelo som |
| Sonâmbulo | Voluntário 03. Só se move no escuro; a luz o congela |
| Câmeras | Os olhos da Aethelgard medindo o experimento |

Regra de escrita: frases curtas, uma legenda por linha, no máximo 70 caracteres. Nada de número na tela que o GDD esconde (posição do monstro, timers).

## 1. Logs da Dra. Vance

Cada HD toca o próximo log (`AudioLogPlayer.driveLogs`). Os textos abaixo já estão nos assets `Vance0N_*.asset`.

### VANCE-01: triagem do Voluntário 07 (HD-01, fase 1)

Ensina: **o Espécime é cego e caça pelo som.**

> Registro 04. Projeto Hipnos, Sub-Nível 4. Voluntário 07 estável.
> A sobrecarga de luz queimou as córneas dele. Ele não reage ao que vê.
> Mas quando um técnico respira do outro lado do vidro, ele vira a cabeça.
> Ele não precisa enxergar. Ele escuta.
> O 08 já está a caminho. Desta vez, sem erros no protocolo.

### VANCE-02: o Voluntário 03 (HD-02, fase 2)

Ensina: **o Sonâmbulo só se move no escuro; a luz o congela.**

> Registro 12. A contenção caiu na subestação três.
> O 03 foi o primeiro. Ele não dorme desde a segunda sessão.
> Enquanto alguém olha para ele, ele não se mexe. Nem respira.
> No escuro ele corre. Perdemos o turno da noite inteiro assim.
> Se você estiver ouvindo isto: mantenha a luz nele e não pisque.

### VANCE-03: o auditor (HD-03, fase 3)

Aponta a reviravolta sem fechar (a C3 fecha).

> Registro 19. A diretoria negou a evacuação.
> Pediram um auditor externo para recuperar os HDs. Um só.
> O perfil dele chegou antes dele: pressão, batimento, histórico de pânico.
> Eu reconheci a letra na ficha. Fui eu que preenchi.
> Eles não querem os dados de volta. Querem medir quanto ele aguenta.

## 2. Falas do Thomas (`InnerVoiceLine`, #89)

Cada fala toca uma vez, no gatilho indicado, com a versão da faixa de sanidade atual. Faixas do GDD 4: **Estável** (100 a 40: Lúcido e Ansioso) e **Abalado** (39 a 1: Alucinando e Colapsado). Bipe uma oitava abaixo do da Vance.

### Fase 1: Ala Médica

| # | Gatilho | Estável | Abalado |
|---|---|---|---|
| T1-1 | Início, depois do elevador | "Sub-Nível 4. Três HDs e eu volto pra casa." | "Três HDs. Três. Eu consigo contar até três." |
| T1-2 | Primeira vez no escuro total | "Escuro demais. A Lux-90 tem que durar." | "Tem alguém respirando no mesmo ritmo que eu." |
| T1-3 | Entrar na enfermaria | "Eu sabia que a enfermaria ficava à esquerda?" | "Eu já deitei nessa maca. Não. Não deitei." |
| T1-4 | Primeiro disjuntor consertado | "Luz. Ele não entra na luz do teto." | "A luz zumbe igual àquela sala branca." |
| T1-5 | Depois da C1 (Espécime) | "Ele não me viu. Ele me ouviu." | "Ele virou pra mim como se soubesse meu nome." |
| T1-6 | HD-01 ejetado | "Um. A Vance sabia que eu viria." | "Ela disse 'o 08'. Por que eu ouvi meu nome?" |

### Fase 2: Ala de Energia

| # | Gatilho | Estável | Abalado |
|---|---|---|---|
| T2-1 | Início, sons do Espécime atrás da parede | "Ele está do outro lado da parede. Andando junto." | "Ele está do outro lado da parede. Esperando por mim." |
| T2-2 | Depois da C2 (Sonâmbulo) | "Não tirar a luz dele. Não tirar a luz dele." | "Ele chegou mais perto. Eu não pisquei. Eu juro." |
| T2-3 | Primeira câmera vista | "Câmeras ligadas num prédio sem ninguém?" | "Alguém está anotando cada passo meu." |
| T2-4 | Escritório (antes da M2) | "Termos de consentimento. Centenas." | "Essa assinatura é minha. Essa é minha também." |
| T2-5 | Subestação religada | "Energia de volta. Agora o HD." | "O zumbido parou. Eu continuo ouvindo." |
| T2-6 | HD-02 ejetado | "Dois. O 03 foi o primeiro. Quem foi o 08?" | "Oito. Oito. Por que esse número não sai da cabeça?" |

### Fase 3: Ala Executiva

| # | Gatilho | Estável | Abalado |
|---|---|---|---|
| T3-1 | Início | "Os dois aqui. Luz para um, escuro para o outro." | "Os dois aqui. Os dois sabem o meu cheiro." |
| T3-2 | Primeira câmera que chama o Espécime | "A câmera chamou ele. Eles estão ajudando o monstro." | "Não é o monstro que me caça. É o prédio." |
| T3-3 | Diante do espelho (antes da M3) | "Só um espelho sujo." | "Esse não sou eu. Esse sou eu." |
| T3-4 | Pista da senha lida | "Os números só aparecem quando eu perco o controle." | "Eu já sabia essa senha. Eu escrevi essa senha." |
| T3-5 | Módulo Neural usado | "Dói. Mas eu enxergo o que estava escondido." | "Mais uma dose. Eles sempre davam mais uma dose." |
| T3-6 | HD-03 ejetado (antes da C3) | "Três. Acabou. Só falta o esconderijo." | "Três. Não acabou. Nunca acaba no três." |

### Esconderijo (opcional, se sobrar tempo)

| # | Gatilho | Estável | Abalado |
|---|---|---|---|
| T4-1 | Escolha feita: salvar | "Ela me apagou. E é a única que pode contar." | "Eu vou te tirar daí. Depois você me devolve quem eu era." |
| T4-2 | Escolha feita: deixar | "Ela fez isso comigo. Que fique." | "Que ela conte de 10 para trás sozinha." |

## 3. Pistas em fósforo (`SanityClue`)

Aparecem só com sanidade abaixo de 35. Texto em maiúsculas, verde fósforo.

| # | Fase | Onde | Texto |
|---|---|---|---|
| P1 | 1 | Parede da enfermaria, acima da maca | VOCÊ JÁ ESTEVE AQUI |
| P2 | 1 | Porta do vestiário | ARMÁRIO 08 |
| P3 | 2 | Chão perto da subestação | 03 NÃO DORME. 08 NÃO LEMBRA. |
| P4 | 2 | Mesa do escritório, sobre os termos | T. REIS |
| P5 | 2 | Parede ao lado de uma câmera | ELES ESTÃO MEDINDO |
| P6 | 3 | Espelho (some quando a M3 toca) | 08 |
| P7 | 3 | Sala da senha, junto dos dígitos | VOCÊ ESCREVEU ISTO |
| P8 | 3 | Porta da sala de observação | ELA AINDA ESTÁ AQUI |

Os dígitos da senha da Ala Executiva continuam como já estão (`CodeClue`).

## 4. Objetos que mudam com a sanidade (#89)

Cada objeto tem um sprite normal e um sprite abalado (abaixo de 35).

| Objeto | Fase | Normal | Abalado |
|---|---|---|---|
| Espelho | 3 | Reflexo do Thomas de terno, crachá | Thomas careca, de avental, pulseira 08 |
| Crachá do Thomas (HUD da pausa) | todas | "T. REIS / AUDITORIA" | "VOL-08 / HIPNOS" |
| Foto na mesa da Vance | 2 | Vance e a equipe sorrindo | Vance e Thomas de avental, lado a lado |
| Pulseira no chão da enfermaria | 1 | Pulseira hospitalar em branco | Pulseira "08" |
| Prontuário na maca | 1 | "PACIENTE: ______" | "PACIENTE: REIS, T. / VOL-08" |

## 5. Cutscenes e memórias (#91)

O planejamento cita 11 cutscenes; a tabela da seção 4 tem 9. As 2 que faltam viram intertítulos curtos de transição (I1 e I2), tocados na tela "Ala concluída", que amarram uma fase à outra.

Formato dos planos: `[tipo] descrição | legenda`. Painéis em 320x180.

### C0 Abertura (painéis, antes da fase 1)

1. `[painel]` Elevador de carga descendo, luz de emergência. | SUB-NÍVEL 4. COMPLEXO EREBUS. 1994.
2. `[painel]` Monitor com o logotipo da Aethelgard, texto corporativo. | AUDITOR T. REIS: RECUPERAR OS HDS 01, 02 E 03 DO PROJETO HIPNOS.
3. `[painel]` Mão do Thomas segurando a Lux-90, crachá no peito. | A BASE ESTÁ EM CONFINAMENTO. NENHUMA EQUIPE RESPONDE.
4. `[painel]` Porta do elevador abrindo para o escuro. | A AETHELGARD AGRADECE A SUA COLABORAÇÃO.

### C1 Espécime (no motor, primeiro encontro na fase 1)

1. Câmera vai do Thomas até o Espécime parado no fim do corredor. | (sem legenda)
2. Ruído de passo do Thomas; o Espécime vira a cabeça para o som. | (sem legenda)
3. Zoom lento no rosto sem olhos; corte seco de volta ao Thomas. | THOMAS: "Ele não me viu."

### M1 Sala branca (memória, fase 1, perto da enfermaria)

1. `[memória]` Luz forte de teto, borrada. | (sem legenda)
2. `[memória]` Mão de luva segurando uma seringa. | VANCE: "Vai doer menos se você não lutar."
3. `[memória]` Rosto da Vance de máscara, de cima. | VANCE: "Conte de 10 para trás, Thomas."
4. `[memória]` Quadro repetido do rosto, mais escuro. | THOMAS: "Dez. Nove. Oito."
5. Glitch, volta ao presente.

### I1 Intertítulo (Ala concluída, fase 1 para 2)

1. `[texto]` | A ALA DE ENERGIA FICA ABAIXO. ALGO ANDA NO ESCURO DE LÁ.

### C2 Sonâmbulo (no motor, começo da fase 2)

1. Câmera mostra o Sonâmbulo parado sob uma lâmpada de teto. | (sem legenda)
2. A lâmpada pisca uma vez. | (sem legenda)
3. A luz volta: ele está dois passos mais perto. | THOMAS: "Ele se mexeu. Quando a luz apagou, ele se mexeu."

### M2 Assinatura (memória, fase 2, escritório)

1. `[memória]` Termo de consentimento sobre a mesa. | TERMO DE PARTICIPAÇÃO VOLUNTÁRIA. PROJETO HIPNOS.
2. `[memória]` Mão do Thomas pegando a caneta. | VANCE: "Você leu tudo? Pode desistir agora."
3. `[memória]` A assinatura sendo feita. | THOMAS: "Eu preciso do dinheiro."
4. `[memória]` Close no nome: "THOMAS REIS". Quadro repetido. | (sem legenda)
5. Glitch, volta ao presente.

### I2 Intertítulo (Ala concluída, fase 2 para 3)

1. `[texto]` | ÚLTIMO HD: ALA EXECUTIVA. OS DOIS ESTÃO SOLTOS.

### M3 Espelho (memória, fase 3, sanidade abaixo de 35)

1. `[motor]` Thomas diante do espelho; o reflexo demora a acompanhar. | (sem legenda)
2. `[memória]` Reflexo careca, de avental. | (sem legenda)
3. `[memória]` Close no pulso: pulseira "08". | VANCE: "Sessão final, Voluntário 08."
4. Glitch, volta ao presente; o espelho está rachado.

### C3 Revelação (painéis + motor, depois do HD-03)

1. `[painel]` Ficha do Voluntário 08 com a foto do Thomas. | VOLUNTÁRIO 08. NOME: THOMAS REIS. STATUS: EM TESTE.
2. `[painel]` Campo "Responsável": assinatura da Vance. | RESPONSÁVEL: DRA. V. VANCE.
3. `[motor]` Alto-falante da sala chia; a voz da Vance, limpa, sem fita. | VANCE: "Thomas. Não é gravação. Estou viva."
4. `[motor]` | VANCE: "Você não é auditor. A auditoria é o teste. Eu apaguei sua memória."
5. `[motor]` | VANCE: "Estou presa na observação. A Aethelgard ativou a autodestruição da base."
6. `[motor]` Sirene começa. | VANCE: "Vá para o esconderijo. Ou venha me buscar. A escolha é sua."

### C4 Final "Testemunha" (painéis, saiu com a Vance)

1. `[painel]` Porta externa abrindo para a neve, dois vultos. | (sem legenda)
2. `[painel]` Vance segurando os 3 HDs. | VANCE: "Com isto, a Aethelgard não apaga mais ninguém."
3. `[painel]` Manchete de jornal. | AETHELGARD DYNAMICS SOB INVESTIGAÇÃO. TESTEMUNHA SOBREVIVENTE.

### C5 Final "Amostra entregue" (painéis, saiu sozinho)

1. `[painel]` Porta externa abrindo; Thomas sozinho. | (sem legenda)
2. `[painel]` Um furgão da Aethelgard esperando. | AGENTE: "Voluntário 08. Bom trabalho."
3. `[painel]` Monitor: gráfico de batimento com o carimbo "APROVADO". | TESTE CONCLUÍDO. INICIAR VOLUNTÁRIO 09.

## 6. Textos do laudo (`ResultScreen`)

| Final | Título | Linha do laudo | Conquista |
|---|---|---|---|
| Testemunha | TESTEMUNHA | "As provas saíram do Sub-Nível 4. A Dra. Vance também." | **A Verdade Corporativa**: sair com a Vance. Bônus no rank |
| Amostra entregue | AMOSTRA ENTREGUE | "A Aethelgard recebeu os dados do Voluntário 08." | nenhuma |

## Para o grupo decidir

1. Tom dos 3 logs, principalmente se o VANCE-03 entrega demais antes da C3.
2. Ano da história: usei 1994 (GDD: "anos 90").
3. Se as falas da Eclusa (T4) entram no RC ou ficam como extra.
