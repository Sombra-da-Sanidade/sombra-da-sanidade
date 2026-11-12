#!/usr/bin/env python3
"""Gera a música do menu: drone industrial em loop perfeito (32 s, mono, 22050 Hz).

Tudo é periódico no tamanho do loop (frequências com número inteiro de ciclos, ruído filtrado por FFT
circular, ecos somados com wrap), então o fim emenda no começo sem clique.

Uso: python3 tools/audio/gen_menu_music.py
Saída: Assets/_Project/Audio/Music/menu_drone.wav (não editar à mão, é sobrescrito)
"""
import wave
from pathlib import Path

import numpy as np

RATE = 22050
SECONDS = 32
N = RATE * SECONDS
OUT = Path(__file__).resolve().parents[2] / "Assets/_Project/Audio/Music/menu_drone.wav"

t = np.arange(N) / RATE
rng = np.random.default_rng(90)


def hz(f):
    """Arredonda a frequência para um número inteiro de ciclos no loop."""
    return round(f * SECONDS) / SECONDS


def sine(f, phase=0.0):
    return np.sin(2 * np.pi * hz(f) * t + phase)


def lfo(period, phase=0.0):
    """0..1, período em segundos (precisa dividir SECONDS)."""
    return 0.5 + 0.5 * np.sin(2 * np.pi * t / period + phase)


def band_noise(lo, hi):
    spec = np.fft.rfft(rng.standard_normal(N))
    freqs = np.fft.rfftfreq(N, 1 / RATE)
    spec[(freqs < lo) | (freqs > hi)] = 0
    out = np.fft.irfft(spec, N)
    return out / np.abs(out).max()


# Drone grave: Lá 55 Hz com batimento lento, quinta e um trítono distante (inquietação).
drone = (sine(55) + 0.8 * sine(55.25) + 0.35 * sine(82.5, 1.0) + 0.12 * sine(77.8, 2.0) * lfo(16))
drone *= 0.55 + 0.45 * lfo(32)

# Ar da ventilação: ruído filtrado que respira a cada 8 s.
air = band_noise(180, 900) * (0.15 + 0.85 * lfo(8, 1.3) ** 2)

# Zumbido elétrico de 120 Hz com harmônicos, entrando e saindo.
hum = (sine(120) + 0.4 * sine(240) + 0.15 * sine(360)) * lfo(16, 3.0) ** 3

# Pings metálicos distantes (canos), ecoados e somados com wrap para emendar.
pings = np.zeros(N)
for start, freq in [(3.0, 1318.5), (11.5, 987.8), (19.0, 1479.9), (27.25, 880.0)]:
    n = int(RATE * 4.0)
    k = np.arange(n) / RATE
    tone = (np.sin(2 * np.pi * freq * k) + 0.5 * np.sin(2 * np.pi * freq * 2.76 * k)) * np.exp(-k * 1.6)
    for delay, gain in [(0.0, 1.0), (0.37, 0.45), (0.81, 0.2)]:
        idx = (int((start + delay) * RATE) + np.arange(n)) % N
        np.add.at(pings, idx, tone * gain)

mix = 0.42 * drone + 0.22 * air + 0.06 * hum + 0.05 * pings
mix /= np.abs(mix).max()
mix *= 0.8

OUT.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(OUT), "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes((mix * 32767).astype("<i2").tobytes())
print(f"ok: {OUT} ({SECONDS}s)")
