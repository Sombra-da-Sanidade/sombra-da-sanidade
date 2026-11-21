# 0005: áudio no WebGL

**Data:** 21/11/2026 · **Status:** aceita

## Contexto
A entrega final roda no navegador (build WebGL). O plano de som ([docs/audio/sound-design.md](../audio/sound-design.md)) depende de música em camadas, oclusão e efeitos de memória. A documentação da Unity 6 (Audio in Web) diz que no Web o `AudioMixer` só muda volume e não aplica efeitos. Faltava medir o que o plano usa de fato.

## Teste (#100)
`Scripts/Audio/AudioWebProbe.cs` numa cena gerada por `Scripts/Editor/AudioWebProbeBuilder.cs` (menu **Sombra da Sanidade > Áudio > Build WebGL da sonda de áudio**). Build WebGL 6000.5.10f1, Chromium, saída a 48 kHz, 60 s de medição:

| Item | Resultado |
|---|---|
| 4 stems de 8 s importados (`CompressedInMemory`), começados juntos com `PlayScheduled` | Sincronizados: deriva máxima de 3,7 ms em 60 s (7 loops), sem acumular |
| Crossfade de volume entre duas versões do mesmo som (`_dry` e `_muffled`, `DecompressOnLoad`) | Funciona |
| `AudioClip.Create` (sem stream) + `SetData` + loop | Funciona |
| `OnAudioFilterRead` | Nunca chamado |
| `clip.frequency` e `clip.samples` de clip `CompressedInMemory` logo após o carregamento | 0 (o navegador ainda decodifica) |
| Aba em segundo plano | O loop do jogo para; o áudio continua |

A deriva é a posição informada por `AudioSource.time`; não foi conferida por gravação da saída.

## Decisão
- **Efeitos são pré-renderizados.** Passa-baixa, reverb, distorção e fita das memórias saem prontos do pipeline (`audio-src/` → WAV). Cada som que precisa de espaço tem versões (`_dry`, `_muffled`, `_memory`, `_far`).
- **No jogo só muda volume.** Oclusão, memórias e camadas da música são crossfade de volume entre `AudioSource`. O `AudioMixer` fica só com grupos e volumes expostos (menu de pausa).
- **Música em camadas com `PlayScheduled`.** Stems do mesmo tamanho, agendados no mesmo `dspTime`, cada um num `AudioSource` em loop.
- **Sem `OnAudioFilterRead` e sem componentes de filtro** (`AudioLowPassFilter`, `AudioReverbFilter` etc.).
- **Import:** SFX `DecompressOnLoad`; música e ambiente `CompressedInMemory`. Não ler `frequency`/`samples` de música logo no início; usar `AudioSource.time`.
- **Clips gerados em runtime continuam permitidos**, mas os sons do plano saem como WAV para soarem iguais no Editor e no navegador.

## Consequências
- O `AudioDirector` (#101) controla tudo por volume; não depende de snapshots com efeito.
- O pipeline de áudio (#102) precisa gerar as versões de cada som, o que aumenta o número de arquivos.
- O áudio só começa depois do primeiro clique ou tecla (regra do navegador); o Menu já exige isso.
