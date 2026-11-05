# Logs de áudio da Dra. Vance

Frente 3 (Sanidade, HUD e narrativa), issue #39. **Status: rascunho para o grupo revisar.**

O GDD 9 diz que os logs da Dra. Valerie Vance revelam três coisas: o Projeto Hipnos, a falha de contenção e a conspiração (Thomas foi enviado de propósito). Há um log para cada uma, e **cada HD coletado toca o próximo log**: o 1º HD toca o VANCE-01, o 2º o VANCE-02 e o 3º o VANCE-03, em qualquer ordem de servidor. O HD vira a recompensa narrativa da ala. A reviravolta exata do Ato 3 (#40) continua pendente: o log 3 só aponta a conspiração, sem resolvê-la.

Cada roteiro tem frases curtas, uma legenda por linha, e reforça uma regra do jogo.

## VANCE-01: triagem do Voluntário 07 (1º HD)

Regra que ensina: **o monstro é cego e caça pelo som.**

> Registro 04. Projeto Hipnos, Sub-Nível 4. Voluntário 07 estável.
> A sobrecarga de luz queimou as córneas dele. Ele não reage a nada que vê.
> Mas quando um técnico respira do outro lado do vidro, ele vira a cabeça.
> Ele não precisa enxergar. Ele escuta.

## VANCE-02: falha de contenção (2º HD)

Regra que ensina: **a luz da lanterna atrai, a das lâmpadas de teto protege (Zona Segura).**

> Registro 12. A contenção elétrica caiu na subestação três.
> O 07 atravessou a eclusa interna. Perdemos o turno da noite inteiro.
> A lanterna atrai, mas a luz das lâmpadas de teto ele não atravessa.
> Se alguém ouvir isto: religue os disjuntores e fique embaixo da luz.

## VANCE-03: o auditor (3º HD)

Revela: **Thomas foi mandado de propósito.**

> Registro 19. A diretoria negou a evacuação.
> Pediram um auditor externo para recuperar os HDs do projeto. Um só.
> O perfil dele chegou antes dele: pressão, batimento, histórico de pânico.
> Eles não querem os dados de volta. Querem medir quanto ele aguenta aqui.

## Para o grupo decidir

1. Tom e texto dos 3 roteiros.
2. Se o VANCE-03 revela demais antes do Ato 3.
3. **Ato 3 (#40):** proposta que veio do primeiro rascunho, ainda sem código no jogo. No 3º HD, a Aethelgard aciona a "Diretiva 88-Omega", uma purga termobárica da base. Começa uma contagem de 60 s até a eclusa, com sirene. Se o tempo acaba, é game over. Isso conversa com a "Fuga da Eclusa cronometrada" do GDD 7 (fase 4, issue #44, Frente 4).

## Como soa

Ainda não há voz gravada. A legenda é **digitada letra a letra**, e cada letra solta um bipe com timbre de vogal (a, e, i, o, u), como a fala de jogos antigos ou do Animal Crossing. Consoantes usam uma vogal sorteada pela letra, um pouco mais baixa. Há pausa depois de vírgula e de ponto, e um chiado de fita baixo enquanto a gravação toca. O tom é por personagem (`babblePitch` no log; a Dra. Vance usa 1,15).

Ao gravar a voz, arraste o áudio para o campo `voice` do log: os bipes param e a digitação passa a acompanhar a duração da gravação.

## Regras

- **Monstro a menos de 4,5 m do jogador:** a gravação pausa com estática e o HUD mostra "— sinal perdido —". Quando ele se afasta, ela continua de onde parou, então o jogador nunca perde o log.
- **Dois HDs seguidos:** o segundo log entra na fila e toca depois do primeiro.
- **Terminais (opcional):** um `AudioLogTerminal` toca um log extra segurando E. O jogador pode andar ouvindo, mas a mais de 7 m do terminal a fita para.
- Só conta como ouvido (`AudioLogArchive`) o log que chega ao fim.

## Onde fica no projeto

| O quê | Onde |
|---|---|
| Texto de cada log | `Assets/_Project/ScriptableObjects/Narrative/Vance0N_*.asset` (campo `lines`) |
| Tocador (HD, legenda digitada, bipes, sinal perdido) | `Scripts/Narrative/AudioLogPlayer.cs`. Um por cena, criado pelo builder com os 3 logs em `driveLogs` |
| Terminal com log extra | `Scripts/Narrative/AudioLogTerminal.cs` |
| Números (velocidade da digitação, pausas, volume dos bipes, raio do sinal perdido) | `ScriptableObjects/Config/NarrativeConfig.asset` |
| Logs ouvidos | `AudioLogArchive.Heard`, para a prancheta da pausa (Frente 5) |
| Pôr um terminal num mapa (Frente 4) | `PrototypeSceneBuilder.CreateAudioLog(parent, square, nome, posição, PrototypeSceneBuilder.LoadAudioLog("NomeDoLog"))` |

**Cenas:** o `AudioLogPlayer` entra pelo `CreateSanityEffects`, usado por todos os builders (protótipo, Demo, Sandboxes e Alfa). Uma cena gerada antes disto precisa ser gerada de novo para os HDs tocarem os logs.
