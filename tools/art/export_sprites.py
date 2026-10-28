#!/usr/bin/env python3
"""Exporta os sprites de art-src/ (JSON em texto) para PNG em Assets/_Project/Art/.

Uso: python3 tools/art/export_sprites.py

Formato de art-src/<Categoria>/<nome>.json:
{
  "cell": [16, 16],          # tamanho de cada frame/tile em pixels
  "columns": 8,              # frames por linha na folha exportada
  "frames": [{"name": "floor_a", "rows": ["0123...", ...]}, ...]
}
Cada caractere é uma chave de art-src/palette.json ("." é transparente).
Saída: Assets/_Project/Art/<Categoria>/<nome>.png. O fatiamento por frame é feito
no Unity pelo PixelArtImporter, lendo o mesmo JSON.

Também gera Assets/_Project/Art/Palette/sombra-palette.png e art-src/sombra.gpl
(paleta para Aseprite/LibreSprite/GIMP).
"""
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "art-src"
OUT = ROOT / "Assets" / "_Project" / "Art"


def load_palette():
    data = json.loads((SRC / "palette.json").read_text(encoding="utf-8"))
    colors = {}
    for c in data["colors"]:
        h = c["hex"].lstrip("#")
        colors[c["key"]] = (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
    colors[data["transparent"]] = (0, 0, 0, 0)
    return data, colors


def write_meta(path: Path, folder: bool = False):
    # .meta mínimo com GUID determinístico (md5 do caminho relativo): todos do grupo
    # geram o mesmo GUID. O Unity completa o resto na importação.
    meta = path.parent / (path.name + ".meta")
    if meta.exists():
        return
    rel = path.relative_to(ROOT).as_posix()
    guid = hashlib.md5(rel.encode("utf-8")).hexdigest()
    extra = "folderAsset: yes\n" if folder else ""
    meta.write_text(f"fileFormatVersion: 2\nguid: {guid}\n{extra}", encoding="utf-8", newline="\n")


def save_png(img: Image.Image, path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    write_meta(path.parent, folder=True)
    # Sem metadados variáveis: exportar duas vezes gera o mesmo arquivo.
    img.save(path, format="PNG", optimize=False)
    write_meta(path)


def export_palette(data, colors):
    keys = [c["key"] for c in data["colors"]]
    img = Image.new("RGBA", (len(keys), 1))
    for x, k in enumerate(keys):
        img.putpixel((x, 0), colors[k])
    save_png(img.resize((len(keys) * 8, 8), Image.NEAREST), OUT / "Palette" / "sombra-palette.png")

    lines = ["GIMP Palette", "Name: Sombra da Sanidade", "Columns: 9", "#"]
    for c in data["colors"]:
        r, g, b, _ = colors[c["key"]]
        lines.append(f"{r:3d} {g:3d} {b:3d}\t{c['name']} ({c['key']})")
    gpl = SRC / "sombra.gpl"
    gpl.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def export_sheet(src: Path, colors) -> list[str]:
    data = json.loads(src.read_text(encoding="utf-8"))
    cw, ch = data["cell"]
    frames = data["frames"]
    cols = max(1, min(data.get("columns", len(frames)), len(frames)))
    rows = (len(frames) + cols - 1) // cols
    img = Image.new("RGBA", (cw * cols, ch * rows), (0, 0, 0, 0))
    errors = []
    for i, frame in enumerate(frames):
        ox, oy = (i % cols) * cw, (i // cols) * ch
        grid = frame["rows"]
        if len(grid) != ch:
            errors.append(f"{src.name}:{frame['name']}: {len(grid)} linhas, esperado {ch}")
        for y, line in enumerate(grid[:ch]):
            if len(line) != cw:
                errors.append(f"{src.name}:{frame['name']}: linha {y} tem {len(line)} colunas, esperado {cw}")
            for x, key in enumerate(line[:cw]):
                if key not in colors:
                    errors.append(f"{src.name}:{frame['name']}: cor '{key}' fora da paleta em ({x},{y})")
                    continue
                img.putpixel((ox + x, oy + y), colors[key])
    category = src.parent.name
    save_png(img, OUT / category / f"{src.stem}.png")
    return errors


def main():
    data, colors = load_palette()
    export_palette(data, colors)
    errors = []
    sources = sorted(p for p in SRC.glob("*/*.json"))
    for src in sources:
        errors += export_sheet(src, colors)
        print(f"exportado: {src.relative_to(ROOT)}")
    if errors:
        print("\n".join(errors), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
