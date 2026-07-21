#!/usr/bin/env py
"""Generate the locked character-select silhouette from the unlocked art.

    TheWitch/images/charui/char_select_char_name.png         (source)
    TheWitch/images/charui/char_select_char_name_locked.png  (output)

Matches the base-game style (see gamedata char_select_ironclad_locked.png):
an opaque image where the character is a flat (default black) silhouette and
the background is kept as a darkened greyscale of the unlocked art
(--bg-factor x luminance). If the source has real transparency the background
stays transparent instead. The character shape comes from:

  * the source's alpha channel, if it has real transparency; otherwise
  * background colour-keying: the dominant border colour is taken as the
    backdrop, and a flood fill from every border pixel matching it (within
    --tolerance) marks connected background; the silhouette is everything
    else. This handles full-bleed art with a (near-)uniform backdrop even
    when the subject touches the edges; interior pixels that happen to match
    the backdrop colour are kept because the flood can't reach them.

Edges are softened with a 1px blur so the silhouette isn't jaggy.

Usage:
    py tools/gen-locked-silhouette.py                 # default char select image
    py tools/gen-locked-silhouette.py path/to/img.png # any image -> *_locked.png
    py tools/gen-locked-silhouette.py --color 2b2b2b --tolerance 24
    py tools/gen-locked-silhouette.py --dry-run
"""

from __future__ import annotations

import argparse
import sys
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter

REPO = Path(__file__).resolve().parent.parent
DEFAULT_SRC = REPO / "TheWitch/images/charui/char_select_char_name.png"


def key_background_mask(img: Image.Image, tolerance: int) -> Image.Image:
    """Flood-fill background from the border; return an L mask (255 = silhouette)."""
    rgb = img.convert("RGB")
    w, h = rgb.size
    px = rgb.load()

    # The backdrop colour = dominant colour along the border (quantized so
    # slight noise still buckets together). Border pixels on the subject
    # (full-bleed art) lose the vote to the actual background.
    border = [(x, y) for x in range(w) for y in (0, h - 1)]
    border += [(x, y) for y in range(h) for x in (0, w - 1)]
    buckets: dict[tuple[int, int, int], int] = {}
    for (x, y) in border:
        p = px[x, y]
        q = (p[0] // 16, p[1] // 16, p[2] // 16)
        buckets[q] = buckets.get(q, 0) + 1
    qbest = max(buckets, key=buckets.get)
    matches = [px[x, y] for (x, y) in border
               if (px[x, y][0] // 16, px[x, y][1] // 16, px[x, y][2] // 16) == qbest]
    bg = tuple(sum(c[i] for c in matches) // len(matches) for i in range(3))

    def is_bg(p):
        return abs(p[0] - bg[0]) + abs(p[1] - bg[1]) + abs(p[2] - bg[2]) <= tolerance

    visited = bytearray(w * h)
    queue = deque()
    for (x, y) in border:
        if not visited[y * w + x] and is_bg(px[x, y]):
            queue.append((x, y))
            visited[y * w + x] = 1
    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not visited[ny * w + nx]:
                if is_bg(px[nx, ny]):
                    visited[ny * w + nx] = 1
                    queue.append((nx, ny))

    mask = Image.new("L", (w, h), 255)
    mpx = mask.load()
    for y in range(h):
        row = y * w
        for x in range(w):
            if visited[row + x]:
                mpx[x, y] = 0
    return mask


def make_silhouette(src: Path, color: tuple[int, int, int], tolerance: int,
                    bg_factor: float) -> tuple[Image.Image, str]:
    img = Image.open(src).convert("RGBA")
    alpha = img.getchannel("A")
    if alpha.getextrema()[0] < 255:
        # Transparent source: silhouette on a transparent background.
        mask = alpha.filter(ImageFilter.GaussianBlur(1))
        out = Image.new("RGBA", img.size, color + (0,))
        out.putalpha(mask)
        return out, "alpha channel"
    # Full-bleed source (base-game style): black character over the unlocked
    # background as darkened greyscale.
    mask = key_background_mask(img, tolerance).filter(ImageFilter.GaussianBlur(1))
    silhouette = Image.new("RGBA", img.size, color + (255,))
    grey = img.convert("L").point(lambda v: int(v * bg_factor)).convert("RGBA")
    return Image.composite(silhouette, grey, mask), "border colour-key"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("source", nargs="?", default=str(DEFAULT_SRC),
                    help="source image (default: char_select_char_name.png)")
    ap.add_argument("--out", help="output path (default: <source>_locked.png)")
    ap.add_argument("--color", default="000000",
                    help="silhouette fill colour, RRGGBB hex (default 000000)")
    ap.add_argument("--bg-factor", type=float, default=0.65,
                    help="background luminance multiplier for full-bleed sources (default 0.65)")
    ap.add_argument("--tolerance", type=int, default=48,
                    help="colour-key distance (sum of RGB deltas) for backgrounds (default 48)")
    ap.add_argument("--dry-run", action="store_true", help="report without writing")
    args = ap.parse_args()

    src = Path(args.source)
    if not src.is_file():
        print(f"source not found: {src}", file=sys.stderr)
        return 1
    out = Path(args.out) if args.out else src.with_name(src.stem + "_locked" + src.suffix)
    try:
        color = tuple(int(args.color.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    except ValueError:
        print(f"bad --color '{args.color}', expected RRGGBB hex", file=sys.stderr)
        return 1

    silhouette, how = make_silhouette(src, color, args.tolerance, args.bg_factor)
    if args.dry_run:
        print(f"would write {out} ({silhouette.size[0]}x{silhouette.size[1]}, shape from {how})")
        return 0
    silhouette.save(out)
    print(f"wrote {out} ({silhouette.size[0]}x{silhouette.size[1]}, shape from {how})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
