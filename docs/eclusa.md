# Fuga da Eclusa (#44)

Fase 4 do GDD (seção 7: corrida cronometrada de 2-3 min) e condição de vitória (seção 5: inserir os 3 HDs no terminal da eclusa e alcançar a porta externa). Frente 4.

## Como funciona

1. O jogador leva os 3 HDs ao **terminal da eclusa** (`EclusaTerminal`) e segura E. Sem os 3 HDs o terminal não aceita.
2. Os HDs entram e a **purga** começa (`EscapeSequence`):
   - a **porta externa** (`ExitDoor` ligado ao terminal) destrava (faixa vermelha → verde);
   - o barulho da eclusa abrindo chama o monstro;
   - o monstro entra em **fúria** (`MonsterController.SetFury`): velocidade 4.6 e raio auditivo 9 (coluna Eclusa da Proposta 1 do v1.1) e todos os comportamentos da Executiva. O galope e o modo seguro continuam valendo.
3. O tempo restante **nunca aparece em número** (GDD 8). Quem conta é:
   - a **sirene**, que acelera e fica mais alta com o tempo (lamento sintético até a pasta Audio ter o som de verdade);
   - a **luz**: a luz global pulsa para vermelho no ritmo da sirene e os giroflex (no terminal e na porta) giram mais rápido; na fase final (últimos 20%) piscam em estrobo.
4. **Chegar à porta**: tela de Vitória ("VOCÊ ESCAPOU"). O rank S/A/B/C entra com #32 e #49.
5. **Purga chega**: Game Over com causa própria (`DeathCause.Purge`, "A PURGA DA ECLUSA TE ALCANÇOU.").

Sem terminal na cena (porta da Ala Médica para o saguão, protótipo), o `ExitDoor` continua como antes: abre com os HDs da cena.

## Números (`ScriptableObjects/Config/EscapeConfig.asset`)

| Campo | Padrão | Origem |
|---|---|---|
| `purgeSeconds` | 150 | GDD 7 (2-3 min) |
| `insertHoldSeconds` | 3 | playtest |
| `activationNoiseRadius` | 40 | playtest |
| `finalPhaseFrom` | 0.8 | playtest |
| Sirene: período 2.4 → 0.8 s, volume 0.35 → 0.8 | | playtest |
| Giroflex: intensidade 0.6 → 1.2, voltas/s 0.5 → 1.6; luz global até 0.18; estrobo 4/s | | playtest |

A fúria fica no `MonsterConfig` (`furySpeed = 4.6`, `furyHearingRadius = 9`).

## Cenas

- **`Scenes/Wings/Eclusa.unity`**: gerada de `art-src/Maps/eclusa.txt` por **Sombra da Sanidade > Gerar cena Eclusa (tiles)** (batchmode: `-executeMethod SombraDaSanidade.EditorTools.AlfaSceneBuilder.BuildEclusa`). O jogador começa com os 3 HDs (`GameManager.startingDrives`). Cofre (início e terminal) embaixo a oeste, porta externa em cima a oeste. Caminho curto (~25 s correndo) pela Câmara central, onde o monstro está; caminho longo e seguro (~45 s) pelo sul, leste e norte. Armários com a tabela da Eclusa (sem bateria).
- **Beta e Alfa**: o terminal fica no saguão (onde era a eclusa) e a porta externa vai para a sala mais longe (Beta: pelo menos 25 tiles do saguão; Alfa: Executiva).
- Legenda nova nas plantas: `U` terminal da eclusa (na face da parede, como o `T`). `E` continua sendo a porta, agora a externa.

## Painel de teste (F1, Beta)

Mostra se a purga está ativa e quanto falta (só no painel). Botões **+3 HDs**, **Iniciar purga** e **Purga em 10 s**; "Ir para" tem o terminal e a porta externa.

## Teste

**Sombra da Sanidade > Rodar teste da Eclusa** (`EclusaEscapeTest`, batchmode `...EclusaEscapeTest.RunBatch`): começa com 3 HDs, porta trancada antes do terminal, terminal inicia purga e fúria (velocidade 4.6), porta destrava e vence, e o fim da purga dá Game Over com causa `Purge`. O `SmokeTest` e o `BetaSceneTest` passam pelo terminal antes da porta.

## Pendências

- Selo de Game Over próprio da purga (hoje usa o das garras) e som de sirene de verdade: arte e áudio.
- Cutscene final: fora de escopo.
- A cena da Eclusa ainda não carrega o checkpoint da Executiva; começa sempre com os 3 HDs.
