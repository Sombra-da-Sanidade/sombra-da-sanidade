# Tempos das fases (playtest do RC)

Issue #87. Critério de pronto: cada fase dura entre 5 e 8 min num playtest de quem **não** montou a fase ([planejamento-rc.md](planejamento-rc.md) seção 8). O total precisa passar de 20 min.

## Piso calculado

Rota mais curta na planta (busca por tile), correndo a 4,0 u/s sem parar, mais os tempos fixos de cada fase. É o tempo de quem já sabe tudo; serve só para ver se a fase não é curta demais por construção.

| Fase | Deslocamento mínimo | Tempos fixos | Piso | Alvo |
|---|---|---|---|---|
| 1. Ala Médica | ~43 s | 1 min calmo, ejeção 12 s | ~2 min | 6 min |
| 2. Ala de Energia | ~42 s | subestação 6 s, ejeção 12 s | ~1 min | 7 min |
| 3. Ala Executiva | ~50 s | senha (achar 4 dígitos), ejeção 12 s | ~1,5 min | 8 min |
| Eclusa | ~26 s direto, ~52 s com a Vance | terminal 3 s, cela 6 s | ~0,5 a 1 min | 2,5 a 4 min |

## Playtest

O tempo de cada ala aparece na tela de "Ala concluída". Registrar uma linha por partida.

| Data | Quem jogou | Fase | Tempo | Mortes | Observação |
|---|---|---|---|---|---|
| | | | | | |

## O que ajustar se sair do alvo

| Sintoma | Ajuste (na planta `art-src/Maps/*.txt` e gerar a cena de novo) |
|---|---|
| Fase curta | Alongar os corredores do anel; mover o servidor (`S`) para a sala mais longe; mais um disjuntor (`K`) no caminho |
| Fase longa | Tirar um trecho de corredor; aproximar o servidor; na Médica, reduzir `calmSeconds` do `WingIntro` no builder |
| Muitas mortes num ponto | Lâmpada (`L`) ou armário (`A`) perto; tirar uma câmera (`C`) do trecho |
