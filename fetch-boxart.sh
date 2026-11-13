#!/usr/bin/env bash
# Download a working NES box art set from libretro-thumbnails
# (the set RetroArch uses) into ./boxart/.
#
# This fetches the equivalent of the licensed North American library —
# 665 titles matched against Wikipedia's "List of Nintendo Entertainment
# System games", listed in boxart-urls.txt. The file names differ from the
# curated 560x800 set committed in this repo, but the mosaic tool only
# cares that the images are in boxart/.
#
# Box art is used with permission. Run this script to (re-)fetch a set anytime.
set -euo pipefail

OUT="${1:-boxart}"
mkdir -p "$OUT"
cd "$OUT"

# Download in parallel, keeping the original filenames; skip files already present.
cat ../boxart-urls.txt | xargs -P 8 -I{} sh -c '
  f=$(basename "{}" | python3 -c "import sys,urllib.parse; print(urllib.parse.unquote(sys.stdin.read().strip()))")
  [ -f "$f" ] || curl -sL -o "$f" "{}"'
echo "Box art in $OUT: $(ls *.png 2>/dev/null | wc -l) images"
