# Cena Beta: protótipo para testar tudo

`Assets/_Project/Scenes/Main/Beta.unity` junta numa cena só tudo o que já foi implementado pelas frentes. O mapa é **gerado por seed**: seed nova = mapa novo, com as mesmas regras. É a cena publicada no [link do Pages](https://sombra-da-sanidade.github.io/sombra-da-sanidade/).

## Como gerar

| Menu (Sombra da Sanidade > Beta) | O que faz |
|---|---|
| Gerar cena Beta (seed padrão) | Mapa da seed `BetaSceneBuilder.DefaultSeed` (2026). É a cena versionada |
| Gerar cena Beta (seed nova) | Sorteia uma seed e gera um mapa novo (não commite sem combinar) |
| Abrir cena Beta | Abre a cena (gera se não existir) |
| Rodar teste da Beta | `BetaSceneTest`: 30 seeds válidas e a cena completa |

Batchmode: `-executeMethod SombraDaSanidade.EditorTools.BetaSceneBuilder.Build`.

O gerador (`BetaMapGenerator`) descarta mapas inválidos e tenta a seed + 1000, + 2000... A seed usada de verdade aparece no canto da tela e no painel F1. A planta sai em `art-src/Maps/beta-gerado.txt`, na mesma legenda da `alfa.txt`.

## O mapa

- **Saguão** (embaixo, no meio): início do jogador e o terminal da eclusa (recebe os 3 HDs e inicia a purga). A porta externa fica na sala mais longe do saguão ([eclusa.md](eclusa.md)).
- **Ala Médica** (oeste), **Ala de Energia** (leste) e **Ala Executiva** (norte): 3 ou 4 salas cada, ligadas por corredores em loop (GDD 4.1, sem becos).
- Em cada ala: 1 servidor de HD, disjuntores com lâmpada, 2 ou 3 armários com a tabela de loot da ala, terminal com log da Dra. Vance, itens (Estabilizador, Adrena-Syn) e decalques.
- **Energia:** duas salas lado a lado separadas por uma parede de 3 tiles, com o atalho só do monstro (grade no chão das duas pontas).
- O monstro começa na sala da Executiva mais longe do saguão.

## O que dá para testar

| Sistema | Onde / como |
|---|---|
| Lanterna, EMP, bateria | Em qualquer lugar. EMP no monstro com clique direito |
| Sanidade, alucinações, batimento | Ficar no escuro; painel F1 põe a sanidade em 30 ou 10 |
| Pistas em fósforo | "FUSÍVEL 3-7-1" perto de um disjuntor da Médica, "ELE SEGUE A LUZ" no saguão (sanidade < 35) |
| Disjuntor e Zona Segura | Segurar E no disjuntor |
| Armários e loot por ala | Segurar E. O sprite muda para aberto |
| Adrena-Syn e Estabilizador | E na ampola laranja / azul |
| Skill check e upgrade da Lux-90 | Segurar E no servidor. F2 força um check durante a ejeção |
| Logs de áudio | E no terminal verde. Os 3 logs principais tocam ao coletar os HDs |
| HUD e relógio | Canto superior esquerdo. Tab mostra o relógio |
| Comportamento do monstro por ala | Entrar em cada ala troca os comportamentos (nome da ala aparece na tela) |
| Atalho do monstro | Ligado só na Energia (ou forçando "Energia" no painel) |
| Modo seguro | Painel F1 |
| Diretor de tensão | Valor e fase só no painel F1 (nunca no HUD) |
| Fuga da Eclusa (#44) | E no terminal do saguão com os 3 HDs; correr até a porta externa antes da purga |

## Painel de teste (F1)

Pausa o jogo. Mostra seed, ala, estado e comportamentos do monstro, tensão, sanidade e bateria.

- **Comportamentos do monstro:** seguir a ala do jogador, ou forçar Médica, Energia ou Executiva.
- **Monstro:** modo seguro, atordoar, ligar/desligar, ruído no jogador.
- **Jogador:** sanidade 100/30/10, bateria cheia ou 10, +1 HD.
- **Fuga da Eclusa:** +3 HDs, iniciar purga, purga em 10 s. O tempo que falta aparece só aqui (GDD 8).
- **Ir para:** cada servidor, disjuntor, armário, item, terminal, terminal da eclusa, porta externa, atalho e perto do monstro.

**F2** força um skill check enquanto o jogador segura E num servidor.

## Para as frentes

- A troca de comportamento por ala (`BetaWingTracker`) é provisória: quando o `DifficultyProfile` (#30) existir, ele faz isso.
- A Beta usa os mesmos builders da Alfa (`AlfaSceneBuilder.BuildFromLayout`) e do protótipo: mudar um prefab, sprite ou config muda a Beta ao regenerar.
- Sprites dos itens: `art-src/Props/props.json` (armário fechado/aberto, Adrena-Syn, Estabilizador, terminal). Todas as cenas passam a usá-los quando forem regeneradas.
