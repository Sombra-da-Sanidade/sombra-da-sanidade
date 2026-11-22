# Logs de áudio da Dra. Vance

Frente 3 (Sanidade, HUD e narrativa), issue #39. **Status: rascunho para o grupo revisar.**

Os textos dos 3 logs, das falas do Thomas, das pistas e das cutscenes do RC ficam em [roteiro-rc.md](roteiro-rc.md) (#90). **Cada HD coletado toca o próximo log**: o 1º HD toca o VANCE-01 (o 07 é cego), o 2º o VANCE-02 (o 03 só se move no escuro) e o 3º o VANCE-03 (o auditor é o 08), em qualquer ordem de servidor. O HD vira a recompensa narrativa da ala.

Este arquivo descreve como os logs soam e funcionam no jogo.

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
