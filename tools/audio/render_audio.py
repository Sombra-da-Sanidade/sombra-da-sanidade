#!/usr/bin/env python3
"""Renderiza as receitas de audio-src/ (JSON em texto) para OGG em Assets/_Project/Audio/.

Uso:
  python3 tools/audio/render_audio.py              # tudo
  python3 tools/audio/render_audio.py breath ui_   # só receitas cujo nome contém um dos filtros
  python3 tools/audio/render_audio.py --list       # lista as receitas

Requisitos: python3 com numpy e scipy, ffmpeg no PATH. Receitas com MIDI precisam do fluidsynth e de uma
soundfont (padrão /usr/share/soundfonts/FluidR3_GM.sf2; outra: variável SOMBRA_SF2).

Formato e exemplos: docs/audio/pipeline.md. Decisões: ADR 0005 (efeitos pré-renderizados) e ADR 0002 (OGG).
Saída: Assets/_Project/Audio/<Categoria>/<nome>_NN[_variante].ogg (música: <nome>.ogg). Não editar à mão.
"""
import hashlib
import json
import os
import re
import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np
from scipy import signal

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "audio-src"
OUT = ROOT / "Assets" / "_Project" / "Audio"
RATE = 44100
SF2 = os.environ.get("SOMBRA_SF2", "/usr/share/soundfonts/FluidR3_GM.sf2")

# Alvo de loudness por grupo (docs/audio/sound-design.md seção 6), em LUFS integrado.
TARGET_LUFS = {"Sfx": -18.0, "Monster": -16.0, "Voice": -18.0, "Ui": -18.0, "Ambience": -28.0, "Music": -24.0}
SHORT_PEAK_DB = -6.0      # sons curtos demais para medir LUFS: normaliza pelo pico
TRUE_PEAK_DB = -1.0       # teto de todos os arquivos

# Grupo padrão pela pasta da receita (igual ao SoundEventBuilder no Unity).
FOLDER_BUS = {"Music": "Music", "Ambience": "Ambience", "Monster": "Monster", "UI": "Ui",
              "Voice": "Voice", "Narrative": "Voice"}


# ---------------------------------------------------------------- utilidades

def db(x):
    return 10 ** (x / 20.0)


def seconds(n):
    return int(round(n * RATE))


def butter(audio, kind, freq, q=0.707, order=2):
    nyq = RATE / 2
    if kind == "bandpass":
        lo, hi = freq if isinstance(freq, (list, tuple)) else (freq / 1.5, freq * 1.5)
        sos = signal.butter(order, [max(10, lo) / nyq, min(hi, nyq * 0.99) / nyq], btype="band", output="sos")
    else:
        f = min(max(10.0, float(freq)), nyq * 0.99)
        sos = signal.butter(order, f / nyq, btype="low" if kind == "lowpass" else "high", output="sos")
    return signal.sosfilt(sos, audio)


def apply_filter(audio, spec):
    if not spec:
        return audio
    for f in spec if isinstance(spec, list) else [spec]:
        audio = butter(audio, f["type"], f["freq"], order=f.get("order", 2))
    return audio


def envelope(n, spec):
    """ADSR em segundos ({"attack","decay","sustain","release"}) ou pontos [[t, v], ...]."""
    if spec is None:
        return np.ones(n)
    t = np.arange(n) / RATE
    if isinstance(spec, list):
        pts = np.array(spec, dtype=float)
        return np.interp(t, pts[:, 0], pts[:, 1])
    a, d = spec.get("attack", 0.005), spec.get("decay", 0.0)
    s, r = spec.get("sustain", 1.0), spec.get("release", 0.05)
    total = n / RATE
    pts = [(0, 0), (a, 1), (a + d, s), (max(a + d, total - r), s), (total, 0)]
    pts = np.array(pts)
    return np.interp(t, pts[:, 0], pts[:, 1])


def colored_noise(n, color, rng):
    white = rng.standard_normal(n)
    if color == "white":
        out = white
    else:
        spec = np.fft.rfft(white)
        f = np.fft.rfftfreq(n, 1 / RATE)
        f[0] = f[1] if n > 1 else 1
        spec /= np.sqrt(f) if color == "pink" else f
        out = np.fft.irfft(spec, n)
    return out / (np.abs(out).max() + 1e-9)


def oscillator(n, wave, f0, f1, vibrato, rng):
    t = np.arange(n) / RATE
    # Glide exponencial de f0 a f1 ao longo do som.
    freq = f0 * (f1 / f0) ** (t / max(t[-1], 1e-6)) if f1 and f1 != f0 else np.full(n, f0)
    if vibrato:
        freq = freq * (1 + vibrato.get("depth", 0.01) * np.sin(2 * np.pi * vibrato.get("rate", 5) * t))
    phase = 2 * np.pi * np.cumsum(freq) / RATE + rng.uniform(0, 2 * np.pi)
    if wave == "sine":
        return np.sin(phase)
    if wave == "square":
        return np.sign(np.sin(phase))
    if wave == "triangle":
        return 2 / np.pi * np.arcsin(np.sin(phase))
    return 2 * ((phase / (2 * np.pi)) % 1.0) - 1  # saw


def decode(path):
    """Qualquer arquivo de áudio (wav, flac, ogg, mp3) para float mono em RATE, via ffmpeg."""
    out = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"],
                         check=True, capture_output=True).stdout
    return np.frombuffer(out, dtype=np.float32).astype(np.float64)


def resample_pitch(audio, ratio):
    if abs(ratio - 1.0) < 1e-4:
        return audio
    n = max(1, int(len(audio) / ratio))
    return signal.resample(audio, n)


def place(buffer, audio, start):
    end = min(len(buffer), start + len(audio))
    if end > start:
        buffer[start:end] += audio[: end - start]


def reverb(audio, mix, decay, rng):
    """Reverb simples: convolução com ruído de decaimento exponencial."""
    if mix <= 0:
        return audio
    n = seconds(decay)
    ir = rng.standard_normal(n) * np.exp(-6.9 * np.arange(n) / n)
    ir = butter(ir, "lowpass", 5000)
    wet = signal.fftconvolve(audio, ir)[: len(audio)]
    wet /= np.abs(wet).max() + 1e-9
    return (1 - mix) * audio + mix * wet * (np.abs(audio).max() + 1e-9)


# ---------------------------------------------------------------- MIDI

NOTE_NAMES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def note_number(note):
    if isinstance(note, int):
        return note
    m = re.fullmatch(r"([A-G])([#b]?)(-?\d)", note)
    if not m:
        raise ValueError(f"nota inválida: {note}")
    n = NOTE_NAMES[m.group(1)] + {"#": 1, "b": -1, "": 0}[m.group(2)]
    return n + 12 * (int(m.group(3)) + 1)


def write_midi(path, notes, bpm, program, channel=0):
    """SMF tipo 0. notes: [[início em tempos, duração em tempos, nota ("A4" ou 69), velocidade], ...]."""
    tpq = 480
    events = []
    for start, length, note, vel in notes:
        n = note_number(note)
        events.append((int(start * tpq), 0x90 | channel, n, int(vel)))
        events.append((int((start + length) * tpq), 0x80 | channel, n, 0))
    events.sort(key=lambda e: (e[0], e[1] & 0xF0 == 0x90))

    def vlq(v):
        out = [v & 0x7F]
        v >>= 7
        while v:
            out.insert(0, (v & 0x7F) | 0x80)
            v >>= 7
        return bytes(out)

    tempo = int(60_000_000 / bpm)
    track = vlq(0) + b"\xFF\x51\x03" + tempo.to_bytes(3, "big")
    track += vlq(0) + bytes([0xC0 | channel, program])
    last = 0
    for tick, status, n, v in events:
        track += vlq(tick - last) + bytes([status, n, v])
        last = tick
    track += vlq(tpq) + b"\xFF\x2F\x00"
    data = b"MThd" + struct.pack(">IHHH", 6, 0, 1, tpq) + b"MTrk" + struct.pack(">I", len(track)) + track
    Path(path).write_bytes(data)


def render_midi(layer, recipe_dir, tmp):
    if "file" in layer:
        mid = recipe_dir / layer["file"]
    else:
        mid = Path(tmp) / "layer.mid"
        write_midi(mid, layer["notes"], layer.get("bpm", 60), layer.get("program", 0))
    if not shutil.which("fluidsynth"):
        sys.exit("fluidsynth não encontrado (necessário para camadas midi).")
    if not Path(SF2).exists():
        sys.exit(f"soundfont não encontrada: {SF2} (defina SOMBRA_SF2).")
    wav = Path(tmp) / "midi.wav"
    subprocess.run(["fluidsynth", "-ni", "-q", "-g", "0.8", "-r", str(RATE), "-F", str(wav), SF2, str(mid)],
                   check=True, capture_output=True)
    return decode(wav)


# ---------------------------------------------------------------- receita

def render_layer(layer, n, rng, pitch, recipe_dir, tmp):
    kind = layer["type"]
    length = seconds(layer["length"]) if "length" in layer else n
    if kind == "tone":
        f0 = layer["freq"] * pitch
        f1 = layer.get("freq_end", layer["freq"]) * pitch
        audio = oscillator(length, layer.get("wave", "sine"), f0, f1, layer.get("vibrato"), rng)
    elif kind == "noise":
        audio = colored_noise(length, layer.get("color", "white"), rng)
    elif kind == "sample":
        audio = decode(recipe_dir / layer["file"])
        start, end = seconds(layer.get("start", 0)), layer.get("end")
        audio = audio[start: seconds(end) if end else None]
        audio = resample_pitch(audio, layer.get("pitch", 1.0) * pitch)
        audio = audio / (np.abs(audio).max() + 1e-9)
    elif kind == "midi":
        audio = render_midi(layer, recipe_dir, tmp)
        audio = audio / (np.abs(audio).max() + 1e-9)
    else:
        raise ValueError(f"camada desconhecida: {kind}")

    if "drive" in layer:
        audio = np.tanh(audio * layer["drive"]) / np.tanh(layer["drive"])
    audio = apply_filter(audio, layer.get("filter"))
    if layer.get("tremolo"):
        tr = layer["tremolo"]
        t = np.arange(len(audio)) / RATE
        audio = audio * (1 - tr.get("depth", 0.5) * (0.5 + 0.5 * np.sin(2 * np.pi * tr.get("rate", 4) * t)))
    audio = audio * envelope(len(audio), layer.get("env"))
    buf = np.zeros(n)
    place(buf, audio * layer.get("gain", 1.0), seconds(layer.get("delay", 0)))
    return buf


def render_take(recipe, take, recipe_dir, tmp):
    rng = np.random.default_rng(recipe.get("seed", 1) * 1000 + take)
    vary = recipe.get("vary", {})
    pitch = 1.0 + rng.uniform(-1, 1) * vary.get("pitch", 0.0)
    gain = 1.0 + rng.uniform(-1, 1) * vary.get("gain", 0.0)
    n = seconds(recipe["duration"])
    mix = np.zeros(n)
    for layer in recipe["layers"]:
        mix += render_layer(layer, n, rng, pitch, recipe_dir, tmp)
    mix = apply_filter(mix, recipe.get("filter"))
    if "drive" in recipe:
        mix = np.tanh(mix * recipe["drive"]) / np.tanh(recipe["drive"])
    if recipe.get("reverb"):
        mix = reverb(mix, recipe["reverb"].get("mix", 0.2), recipe["reverb"].get("decay", 1.0), rng)
    mix = mix * envelope(n, recipe.get("env"))
    if recipe.get("loop"):
        mix = make_loop(mix, seconds(recipe.get("loop_crossfade", 0.5)))
    return mix * gain


def make_loop(audio, xfade):
    """Emenda sem clique: o fim é misturado no começo e cortado."""
    xfade = min(xfade, len(audio) // 3)
    if xfade <= 0:
        return audio
    head, tail = audio[:xfade], audio[-xfade:]
    ramp = np.linspace(0, 1, xfade)
    out = audio[:-xfade].copy()
    out[:xfade] = head * ramp + tail * (1 - ramp)
    return out


# ---------------------------------------------------------------- variantes (ADR 0005)

def variant(audio, name, rng):
    if name == "dry":
        return audio
    if name == "muffled":     # atrás de parede (AudioOcclusion)
        return butter(butter(audio, "lowpass", 700, order=4), "highpass", 60)
    if name == "far":         # longe: perde grave e agudo, ganha sala
        out = butter(butter(audio, "highpass", 220), "lowpass", 2600)
        return reverb(out, 0.35, 1.2, rng)
    if name == "memory":      # fita gasta das memórias (sound-design.md seção 1, pilar 5)
        t = np.arange(len(audio)) / RATE
        wow = 0.004 * np.sin(2 * np.pi * 0.7 * t) + 0.0015 * np.sin(2 * np.pi * 6.3 * t)
        idx = np.clip(np.arange(len(audio)) + np.cumsum(wow) * 40, 0, len(audio) - 1)
        out = np.interp(idx, np.arange(len(audio)), audio)
        out = butter(butter(out, "lowpass", 3200), "highpass", 180)
        out = np.round(out * 96) / 96                       # leve redução de bits
        hiss = butter(rng.standard_normal(len(out)), "highpass", 4000) * 0.01
        return out + hiss
    raise ValueError(f"variante desconhecida: {name}")


# ---------------------------------------------------------------- loudness e saída

def write_wav(path, audio):
    pcm = (np.clip(audio, -1, 1) * 32767).astype("<i2")
    with open(path, "wb") as f:
        f.write(b"RIFF" + struct.pack("<I", 36 + pcm.nbytes) + b"WAVEfmt ")
        f.write(struct.pack("<IHHIIHH", 16, 1, 1, RATE, RATE * 2, 2, 16))
        f.write(b"data" + struct.pack("<I", pcm.nbytes) + pcm.tobytes())


def measure_lufs(audio, tmp):
    wav = Path(tmp) / "measure.wav"
    write_wav(wav, audio / max(1.0, np.abs(audio).max()))
    scale = max(1.0, np.abs(audio).max())
    res = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", str(wav), "-af", "ebur128", "-f", "null", "-"],
                         capture_output=True, text=True)
    m = re.findall(r"I:\s+(-?[\d.]+|-inf) LUFS", res.stderr)
    if not m or m[-1] == "-inf":
        return None
    value = float(m[-1]) + 20 * np.log10(scale)
    return value if value > -70 else None


def true_peak_db(audio):
    up = signal.resample_poly(audio, 4, 1)
    return 20 * np.log10(np.abs(up).max() + 1e-12)


def normalize_gain(audio, bus, tmp):
    target = TARGET_LUFS.get(bus, -18.0)
    lufs = measure_lufs(audio, tmp) if len(audio) >= seconds(0.5) else None
    if lufs is None:
        return db(SHORT_PEAK_DB) / (np.abs(audio).max() + 1e-12)
    return db(target - lufs)


def limit_true_peak(audio):
    tp = true_peak_db(audio)
    return audio * db(TRUE_PEAK_DB - tp) if tp > TRUE_PEAK_DB else audio


def encode_ogg(audio, path, tmp):
    wav = Path(tmp) / "out.wav"
    write_wav(wav, audio)
    path.parent.mkdir(parents=True, exist_ok=True)
    # bitexact: renderizar de novo gera o mesmo arquivo (sem diff no git).
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-map_metadata", "-1", "-fflags", "+bitexact",
                    "-flags:a", "+bitexact", "-c:a", "libvorbis", "-q:a", "6", str(path)], check=True)
    write_meta(path)


def write_meta(path):
    """.meta mínimo com GUID determinístico (como o exportador de arte): o Unity completa o importador."""
    folder_extra = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    targets = []
    folder = path.parent
    while folder != OUT and OUT in folder.parents:
        targets.append((folder.parent / (folder.name + ".meta"), folder_extra))
        folder = folder.parent
    targets.append((path.parent / (path.name + ".meta"), ""))
    for target, extra in targets:
        if target.exists():
            continue
        rel = target.relative_to(ROOT).as_posix()[: -len(".meta")]
        guid = hashlib.md5(rel.encode("utf-8")).hexdigest()
        target.write_text(f"fileFormatVersion: 2\nguid: {guid}\n{extra}", encoding="utf-8", newline="\n")


def output_names(recipe, name):
    if recipe.get("kind") == "music":
        return [name]
    return [f"{name}_{i + 1:02d}" for i in range(recipe.get("takes", 1))]


def render_recipe(path):
    recipe = json.loads(path.read_text(encoding="utf-8"))
    category = path.parent.relative_to(SRC).as_posix()
    name = path.stem
    bus = recipe.get("bus", FOLDER_BUS.get(path.parent.name, "Sfx"))
    variants = ["dry"] + [v for v in recipe.get("variants", []) if v != "dry"]
    written = []
    with tempfile.TemporaryDirectory() as tmp:
        for take, base in enumerate(output_names(recipe, name)):
            audio = render_take(recipe, take, path.parent, tmp)
            gain = normalize_gain(audio, bus, tmp)   # ganho medido na versão limpa, igual para as variantes
            for v in variants:
                rng = np.random.default_rng(recipe.get("seed", 1) * 7919 + take)
                out = limit_true_peak(variant(audio, v, rng) * gain)
                file = OUT / category / (base + ("" if v == "dry" else f"_{v}") + ".ogg")
                encode_ogg(out, file, tmp)
                written.append(file.relative_to(ROOT).as_posix())
    return bus, written


def recipes():
    return sorted(p for p in SRC.rglob("*.json") if "Raw" not in p.relative_to(SRC).parts)


def main(args):
    if "--list" in args:
        for p in recipes():
            print(p.relative_to(SRC).as_posix())
        return
    filters = [a for a in args if not a.startswith("--")]
    todo = [p for p in recipes() if not filters or any(f in p.stem for f in filters)]
    if not todo:
        print("Nenhuma receita encontrada.")
        return
    for p in todo:
        bus, files = render_recipe(p)
        print(f"{p.relative_to(SRC).as_posix()} [{bus}] -> {len(files)} arquivo(s)")
        for f in files:
            print("   ", f)


if __name__ == "__main__":
    main(sys.argv[1:])
