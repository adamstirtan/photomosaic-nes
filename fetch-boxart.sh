#!/usr/bin/env bash
# Download a spread of real NES box art from libretro-thumbnails
# (the set RetroArch uses) into ./boxart/.
#
# Picks 250 distinct US titles, spread across the alphabet, skipping
# hacks/variants. Box art is copyrighted by its publishers, so it is
# NOT committed to this repo — boxart/ is gitignored. Run this script
# to fetch it locally, then run the mosaic tool against it.
set -euo pipefail

OUT="${1:-boxart}"
mkdir -p "$OUT"
cd "$OUT"

INDEX="$(mktemp)"
trap 'rm -f "$INDEX"' EXIT
curl -s "https://thumbnails.libretro.com/Nintendo%20-%20Nintendo%20Entertainment%20System/Named_Boxarts/" -o "$INDEX"

python3 - "$INDEX" <<'EOF' > urls.txt
import re, html, sys, urllib.parse
from collections import OrderedDict
data = open(sys.argv[1], encoding='utf-8', errors='ignore').read()
seen = OrderedDict()
for h in re.findall(r'href="([^"]*\.png)"', data):
    name = html.unescape(urllib.parse.unquote(h))
    if '(US)' not in name and '(USA)' not in name:
        continue
    if '[' in name:
        continue
    title = name.split(' (')[0].strip()
    if title and title not in seen:
        seen[title] = name
titles = list(seen)
step = max(1, len(titles) // 250)
base = "https://thumbnails.libretro.com/Nintendo%20-%20Nintendo%20Entertainment%20System/Named_Boxarts/"
for t in titles[::step][:250]:
    print(base + urllib.parse.quote(seen[t]))
EOF

# Download in parallel, keeping the original filenames.
cat urls.txt | xargs -P 8 -I{} sh -c '
  f=$(basename "{}" | python3 -c "import sys,urllib.parse; print(urllib.parse.unquote(sys.stdin.read().strip()))")
  curl -sL -o "$f" "{}"'
rm -f urls.txt
echo "Downloaded $(ls *.png 2>/dev/null | wc -l) box art images into $OUT"
