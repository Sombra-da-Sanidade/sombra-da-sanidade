# Pipeline de áudio

Como os sons do jogo são feitos (#102). Plano geral em [sound-design.md](sound-design.md); decisões em [ADR 0005](../decisoes/0005-audio-no-webgl.md) (efeitos pré-renderizados) e [ADR 0002](../decisoes/0002-sem-git-lfs.md) (OGG, sem LFS).

## Fluxo

1. Escreva a receita em `audio-src/<Categoria>/<nome>.json` (ou grave foley em `audio-src/Raw/`).
2. `python3 tools/audio/render_audio.py <nome>` gera `Assets/_Project/Audio/<Categoria>/<nome>_NN.ogg` e as versões pedidas.
3. No Unity: **Sombra da Sanidade > Áudio > Gerar SoundEvents**. Cria `ScriptableObjects/Audio/<Categoria>/<nome>.asset`.
4. No código: `AudioService.Play(som, posição)` (ou `MusicLayers` para música da fase).

Requisitos: Python 3 com `numpy` e `scipy`, `ffmpeg` no PATH. Receitas com MIDI: `fluidsynth` e uma soundfont (Arch: `sudo pacman -S fluidsynth soundfont-fluid`; outra soundfont: variável `SOMBRA_SF2`). Windows: `pip install numpy scipy`, ffmpeg e fluidsynth no PATH.

Não edite os `.ogg` de `Assets/_Project/Audio/`: o render sobrescreve. Renderizar de novo gera arquivos idênticos (sem diff no git).

## Categorias

A pasta define o grupo do mixer quando a receita não diz `bus`.

| Pasta | Grupo | Import no Unity |
|---|---|---|
| `Music` | Music | CompressedInMemory, estéreo |
| `Ambience` | Ambience | CompressedInMemory, estéreo |
| `Monster` | Monster | DecompressOnLoad, mono |
| `UI` | Ui | DecompressOnLoad, mono |
| `Voice`, `Narrative` | Voice | DecompressOnLoad, mono |
| outras (`Player`, `Lux`, `World`, ...) | Sfx | DecompressOnLoad, mono |

Arquivo sem tomada `_NN` (música) também é importado como CompressedInMemory. O Unity não normaliza: o volume vem do render.

## Volume

O render ajusta cada tomada para o alvo do grupo (sound-design.md seção 6) e limita o pico em -1 dBTP:

| Grupo | Alvo |
|---|---|
| Sfx, Voice, Ui | -18 LUFS |
| Monster | -16 LUFS |
| Music | -24 LUFS |
| Ambience | -28 LUFS |

Sons com menos de 0,5 s vão pelo pico (-6 dBFS). As versões usam o mesmo ganho da versão limpa: a abafada fica naturalmente mais baixa.

## Receita

```json
{
  "bus": "Monster",
  "kind": "sfx",
  "duration": 4.5,
  "takes": 2,
  "seed": 11,
  "loop": true,
  "loop_crossfade": 0.6,
  "variants": ["muffled", "far"],
  "vary": { "pitch": 0.06, "gain": 0.05 },
  "layers": [ ... ],
  "filter": { "type": "lowpass", "freq": 4000 },
  "drive": 1.5,
  "reverb": { "mix": 0.2, "decay": 1.0 },
  "env": { "attack": 0.01, "decay": 0.1, "sustain": 0.8, "release": 0.3 },
  "event": { "spatial": true, "volume": [0.9, 1.0], "pitch": [0.97, 1.03], "noiseRadius": 0, "cooldown": 0, "maxDistance": 0 },
  "music_set": "Fase1",
  "layer": 0
}
```

| Campo | O que faz |
|---|---|
| `kind` | `sfx` (padrão, gera `nome_01..NN`) ou `music` (gera `nome.ogg`, sem tomadas) |
| `duration` | Segundos |
| `takes` | Quantas tomadas (3 a 8 para sons repetidos). Cada uma usa outra semente |
| `seed` | Semente: mesma receita, mesmo som |
| `vary` | Variação por tomada: `pitch` (fração) e `gain` (fração) |
| `loop` | Emenda o fim no começo (sem clique). `loop_crossfade` em segundos |
| `variants` | `muffled` (atrás de parede), `far` (longe), `memory` (fita das memórias) |
| `filter`, `drive`, `reverb`, `env` | Processamento do som inteiro, depois das camadas |
| `event` | Campos do `SoundEvent` no Unity. `noiseRadius` > 0 faz o Espécime ouvir |
| `music_set`, `layer` | Música em camadas: o builder põe o arquivo na camada `layer` (0 a 3) do `MusicSet` |

### Camadas

Toda camada aceita `gain`, `delay` (s), `length` (s), `env`, `filter` (um ou lista), `drive` e `tremolo` (`rate`, `depth`).

| `type` | Campos |
|---|---|
| `tone` | `wave` (`sine`, `saw`, `square`, `triangle`), `freq`, `freq_end` (glide), `vibrato` (`rate`, `depth`) |
| `noise` | `color` (`white`, `pink`, `brown`) |
| `sample` | `file` (relativo à receita, ex. `../Raw/porta.flac`), `start`, `end`, `pitch` |
| `midi` | `notes` (`[início, duração, nota, velocidade]` em tempos, nota `"A4"` ou número), `bpm`, `program` (General MIDI, 8 = celesta), ou `file` com um `.mid` |

`env` é ADSR (`attack`, `decay`, `sustain`, `release` em segundos) ou pontos `[[tempo, valor], ...]`. `filter` é `{ "type": "lowpass" | "highpass" | "bandpass", "freq": 800 }` (bandpass: `"freq": [lo, hi]`).

Exemplos completos em `audio-src/_Exemplos/`: bipe de UI (3 tomadas), respiração de monstro (loop com versões abafada e distante) e o motivo da contagem (MIDI com versão de memória).

## Foley gravado

Grave em `audio-src/Raw/` (FLAC ou WAV, até ~5 MB por arquivo, ADR 0002) e use numa camada `sample`. Registre autor, data e, se for de terceiros, link e licença em `docs/audio/fontes.md`.
