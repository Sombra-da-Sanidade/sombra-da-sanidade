#!/usr/bin/env python3
"""Gera os motivos da trilha (docs/audio/sound-design.md seção 4) como .mid em audio-src/Music/Motivos/.

Uso: python3 tools/audio/motifs.py
As receitas de audio-src/Music/ usam esses arquivos em camadas "midi" ("file": "Motivos/<nome>.mid").
Ré menor. Notas: [início em tempos, duração em tempos, nota, velocidade].
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True   # não deixar __pycache__ em tools/audio
from render_audio import SRC, write_midi  # noqa: E402

OUT = SRC / "Music" / "Motivos"

# nome: (bpm, programa General MIDI, notas)
MOTIFS = {
    # Contagem (Thomas): 4 notas descendo, "conte de 10 para trás". Celesta.
    "contagem": (50, 8, [[0, 1.5, "A5", 80], [1.5, 1.5, "G5", 74], [3, 1.5, "F5", 68], [4.5, 3, "E5", 62]]),
    # A mesma contagem invertida (subindo): é a que o menu toca. O jogador só reconhece a certa na Revelação.
    "contagem_invertida": (50, 8, [[0, 1.5, "E5", 62], [1.5, 1.5, "F5", 68], [3, 1.5, "G5", 74], [4.5, 3, "A5", 80]]),
    # Contagem resolvida em Ré maior (final Testemunha): o Mi desce até o Ré.
    "contagem_resolvida": (46, 0, [[0, 1.5, "A4", 76], [1.5, 1.5, "G4", 72], [3, 1.5, "F#4", 70], [4.5, 1.5, "E4", 66],
                                   [6, 4, "D4", 70], [6, 4, "F#4", 58], [6, 4, "A4", 58]]),
    # Hipnos (experimento): duas notas a um trítono, como bipe de equipamento médico.
    "hipnos": (90, 80, [[0, 0.5, "D6", 70], [1, 0.5, "G#5", 70]]),
    # Aethelgard (empresa): jingle corporativo anos 90, alegre. Vibrafone.
    "aethelgard": (112, 11, [[0, 0.5, "C5", 90], [0.5, 0.5, "E5", 90], [1, 0.5, "G5", 90], [1.5, 0.5, "A5", 95],
                             [2, 2, "G5", 85], [2, 2, "C5", 60], [2, 2, "E5", 60]]),
    # Vance: duas notas de piano, Ré e Fá.
    "vance": (40, 0, [[0, 2, "D4", 70], [2, 3, "F4", 64]]),
}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (bpm, program, notes) in MOTIFS.items():
        write_midi(OUT / f"{name}.mid", notes, bpm, program)
        print(f"audio-src/Music/Motivos/{name}.mid")


if __name__ == "__main__":
    main()
