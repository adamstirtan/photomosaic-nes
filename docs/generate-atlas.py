#!/usr/bin/env python3
"""Build the web demo's tile atlas from boxart/.

Packs all box art images into one 1024x1024 atlas (16x16 grid of 64px
tiles) and writes tiles.json with each tile's average colour (same
definition as the CLI: average of the whole image). The browser demo
loads one small JPEG + one JSON instead of 250 separate images.
"""
import json, os, sys
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BOXART = os.path.join(ROOT, "boxart")
DOCS = os.path.join(ROOT, "docs")
TILE = 64
COLS = 16

files = sorted(f for f in os.listdir(BOXART)
               if f.lower().endswith((".png", ".jpg", ".jpeg", ".webp", ".bmp")))
assert files, "no box art found in boxart/"
rows = (len(files) + COLS - 1) // COLS
atlas = Image.new("RGB", (COLS * TILE, rows * TILE), (0, 0, 0))
tiles = []
for i, name in enumerate(files):
    im = Image.open(os.path.join(BOXART, name)).convert("RGB")
    # Average colour over the whole image (matches the CLI).
    small = im.resize((1, 1))
    r, g, b = small.getpixel((0, 0))
    # Centre-crop to square, then resize to tile size.
    w, h = im.size
    s = min(w, h)
    im = im.crop(((w - s) // 2, (h - s) // 2, (w - s) // 2 + s, (h - s) // 2 + s))
    im = im.resize((TILE, TILE), Image.LANCZOS)
    x, y = (i % COLS) * TILE, (i // COLS) * TILE
    atlas.paste(im, (x, y))
    tiles.append({"name": name, "r": r, "g": g, "b": b, "col": i % COLS, "row": i // COLS})

os.makedirs(DOCS, exist_ok=True)
atlas.save(os.path.join(DOCS, "atlas.jpg"), quality=88)
with open(os.path.join(DOCS, "tiles.json"), "w") as f:
    json.dump({"tileSize": TILE, "cols": COLS, "rows": rows, "tiles": tiles}, f)
print(f"{len(files)} tiles -> docs/atlas.jpg {atlas.size}, docs/tiles.json")
