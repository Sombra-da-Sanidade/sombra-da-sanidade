#!/usr/bin/env python3
"""Exporta os painéis de cutscene de art-src/Cutscenes/*.aseprite para PNG (ADR 0006).

Uso:
  python3 tools/art/export_cutscenes.py            exporta todos
  python3 tools/art/export_cutscenes.py C0_01      exporta só os que começam com o prefixo
  python3 tools/art/export_cutscenes.py --check    só valida os PNG já exportados

Precisa do Aseprite no PATH (ou na variável ASEPRITE) e de pillow.

Saída: Assets/_Project/Art/Cutscenes/<nome>.png, 320x180, opaco. O Unity importa
com Point, sem compressão e 16 PPU (CutsceneRc3Builder.BuildPanel).

Validação de paleta:
  - Painéis M* (memórias) usam só as 4 cores de art-src/memoria.gpl (ADR 0007).
  - Os demais usam só as cores de art-src/palette.json.
"""
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "art-src" / "Cutscenes"
OUT = ROOT / "Assets" / "_Project" / "Art" / "Cutscenes"
SIZE = (320, 180)


def hex_to_rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))


def load_gpl(path):
    colors = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        parts = line.split()
        if len(parts) >= 3 and all(p.isdigit() for p in parts[:3]):
            colors.add(tuple(int(p) for p in parts[:3]))
    return colors


def palettes():
    data = json.loads((ROOT / "art-src" / "palette.json").read_text(encoding="utf-8"))
    present = {hex_to_rgb(c["hex"]) for c in data["colors"]}
    memory = load_gpl(ROOT / "art-src" / "memoria.gpl")
    return present, memory


def check(png, present, memory):
    """Devolve a lista de problemas do PNG (vazia se estiver ok)."""
    problems = []
    im = Image.open(png).convert("RGBA")
    if im.size != SIZE:
        problems.append(f"tamanho {im.size}, esperado {SIZE}")
    if im.getchannel("A").getextrema() != (255, 255):
        problems.append("tem transparência (painel deve ser opaco)")
    allowed = memory if png.name.startswith("M") else present
    used = {c[:3] for _, c in im.getcolors(1 << 20)}
    extra = used - allowed
    if extra:
        sample = ", ".join("#%02x%02x%02x" % c for c in sorted(extra)[:5])
        problems.append(f"{len(extra)} cor(es) fora da paleta: {sample}")
    return problems


def export(src, aseprite):
    dst = OUT / f"{src.stem}.png"
    subprocess.run([aseprite, "-b", str(src), "--save-as", str(dst)], check=True,
                   stdout=subprocess.DEVNULL)
    return dst


def main(args):
    present, memory = palettes()
    only_check = "--check" in args
    prefixes = [a for a in args if not a.startswith("--")]

    if only_check:
        targets = sorted(OUT.glob("*.png"))
    else:
        aseprite = os.environ.get("ASEPRITE") or shutil.which("aseprite")
        if not aseprite:
            sys.exit("Aseprite não encontrado. Instale ou defina ASEPRITE=/caminho/aseprite.")
        sources = sorted(SRC.glob("*.aseprite"))
        if prefixes:
            sources = [s for s in sources if any(s.stem.startswith(p) for p in prefixes)]
        if not sources:
            sys.exit("Nenhum .aseprite encontrado em art-src/Cutscenes/.")
        OUT.mkdir(parents=True, exist_ok=True)
        targets = [export(s, aseprite) for s in sources]

    failed = 0
    for png in targets:
        problems = check(png, present, memory)
        status = "ok" if not problems else "ERRO: " + "; ".join(problems)
        print(f"{png.name:32} {status}")
        failed += bool(problems)

    if failed:
        sys.exit(f"{failed} painel(is) com problema.")
    print(f"{len(targets)} painel(is) ok.")


if __name__ == "__main__":
    main(sys.argv[1:])
