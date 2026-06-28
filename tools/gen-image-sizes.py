#!/usr/bin/env py
"""Generate missing big/small image variants for prototyping.

Card portraits and powers each ship in two sizes: a small portrait and a
"big" full-art version under a ``big/`` subfolder, sharing the same filename:

    TheWicken/images/card_portraits/foo.png              (small)
    TheWicken/images/card_portraits/big/foo.png          (big)
    TheWicken/images/card_portraits/familiar/foo.png     (small)
    TheWicken/images/card_portraits/big/familiar/foo.png (big)
    TheWicken/images/powers/foo.png                      (small)
    TheWicken/images/powers/big/foo.png                  (big)

This script fills in whichever side is missing:

  * small exists, big missing  -> upscale  small  -> big
  * big exists,   small missing -> downscale big   -> small
  * both exist                  -> skipped (unless --force)

Before that, a placeholder-seeding pass reads the card design data
(``Docs/card-data/cards.json``) and, for any card with NO art on either side,
copies the default ``card.png`` placeholder to its expected small path. The
round-trip pass then upscales that into a ``big/`` variant -- so a brand-new
card with zero art ends up with both sizes instead of nothing. The small image
filename is the card's ``entry`` lowercased (matching the in-game
``Id.Entry``-derived path); familiar cards (under ``Cards/Familiar/``) route to
the ``familiar/`` subfolder.

Variants are scaled by a fixed factor (default 4x), which matches the project's
existing assets (250x190 <-> 1000x760, 64x64 <-> 256x256). Aspect ratio is
preserved, so odd prototype sizes round-trip cleanly.

New PNGs are imported automatically; `dotnet publish` invokes Godot, which
writes the .import sidecars before packing the .pck.

Usage:
    py tools/gen-image-sizes.py                 # both categories, scale 4
    py tools/gen-image-sizes.py --dry-run       # show what would happen
    py tools/gen-image-sizes.py --category powers
    py tools/gen-image-sizes.py --scale 4 --force
    py tools/gen-image-sizes.py --nearest       # nearest-neighbour (pixel art)
"""
from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required. Install it with:  py -m pip install Pillow")

# Repo root is the parent of this tools/ folder.
REPO_ROOT = Path(__file__).resolve().parent.parent
IMAGES_ROOT = REPO_ROOT / "TheWicken" / "images"

# Category -> (small_dir, big_dir). Big art lives under a single "big/" mirror
# at each portrait root, with category subfolders nested inside it (matching
# BigCardImagePath, which prepends "big/" before the relative path):
#   card_portraits/familiar/foo.png  ->  card_portraits/big/familiar/foo.png
CATEGORIES = {
    "card_portraits": (IMAGES_ROOT / "card_portraits",
                       IMAGES_ROOT / "card_portraits" / "big"),
    "familiar": (IMAGES_ROOT / "card_portraits" / "familiar",
                 IMAGES_ROOT / "card_portraits" / "big" / "familiar"),
    "powers": (IMAGES_ROOT / "powers",
               IMAGES_ROOT / "powers" / "big"),
}

# Card design data + the default placeholder used to seed art for new cards.
CARDS_JSON = REPO_ROOT / "Docs" / "card-data" / "cards.json"
PLACEHOLDER = IMAGES_ROOT / "card_portraits" / "card.png"


def seed_card_placeholders(dry_run: bool) -> int:
    """Copy the placeholder image to the small path of any card in cards.json
    that has no art on either side. The round-trip pass then makes its big/.
    """
    if not CARDS_JSON.exists():
        print(f"[seed] cards.json not found: {CARDS_JSON} (skipped)")
        return 0
    if not PLACEHOLDER.exists():
        print(f"[seed] placeholder not found: {PLACEHOLDER} (skipped)")
        return 0

    cards = json.loads(CARDS_JSON.read_text(encoding="utf-8")).get("cards", [])
    seeded = 0
    for card in cards:
        entry = card.get("entry", "")
        if not entry:
            continue
        is_familiar = "Cards/Familiar/" in card.get("file", "").replace("\\", "/")
        small_dir, big_dir = CATEGORIES["familiar" if is_familiar else "card_portraits"]
        fname = f"{entry.lower()}.png"
        small_path = small_dir / fname
        big_path = big_dir / fname

        # Only seed when the card has no art at all; never clobber real art.
        if small_path.exists() or big_path.exists():
            continue

        rel_dst = small_path.relative_to(REPO_ROOT)
        print(f"  seed      placeholder -> {rel_dst}")
        if not dry_run:
            small_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(PLACEHOLDER, small_path)
        seeded += 1

    print(f"[seed] {seeded} placeholder(s){' (dry run)' if dry_run else ''}")
    return seeded


def scaled(size: tuple[int, int], factor: float) -> tuple[int, int]:
    """Scale (w, h) by factor, rounding to at least 1px per side."""
    return (max(1, round(size[0] * factor)), max(1, round(size[1] * factor)))


def resize(src: Path, dst: Path, factor: float, resample, dry_run: bool) -> None:
    with Image.open(src) as img:
        target = scaled(img.size, factor)
        verb = "upscale" if factor > 1 else "downscale"
        rel_src = src.relative_to(REPO_ROOT)
        rel_dst = dst.relative_to(REPO_ROOT)
        print(f"  {verb:9} {img.size[0]}x{img.size[1]} -> {target[0]}x{target[1]}"
              f"  {rel_src}  ->  {rel_dst}")
        if dry_run:
            return
        out = img.resize(target, resample)
        dst.parent.mkdir(parents=True, exist_ok=True)
        out.save(dst)


def process_category(name: str, small_dir: Path, big_dir: Path, scale: float,
                     resample, force: bool, dry_run: bool) -> int:
    if not small_dir.exists():
        print(f"[{name}] small dir not found: {small_dir} (skipped)")
        return 0

    # Names present on either side (small dir is non-recursive so the big/
    # subfolder is not double-counted).
    small_names = {p.name for p in small_dir.glob("*.png")}
    big_names = {p.name for p in big_dir.glob("*.png")} if big_dir.exists() else set()

    print(f"[{name}] small={len(small_names)} big={len(big_names)}")
    generated = 0
    for fname in sorted(small_names | big_names):
        small_path = small_dir / fname
        big_path = big_dir / fname
        has_small = fname in small_names
        has_big = fname in big_names

        if has_small and not has_big:                       # make big from small
            resize(small_path, big_path, scale, resample, dry_run)
            generated += 1
        elif has_big and not has_small:                     # make small from big
            resize(big_path, small_path, 1 / scale, resample, dry_run)
            generated += 1
        elif has_small and has_big and force:               # regenerate big from small
            resize(small_path, big_path, scale, resample, dry_run)
            generated += 1
    if generated == 0:
        print("  nothing to do")
    return generated


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--category", choices=sorted(CATEGORIES),
                        help="Only process this category (default: all).")
    parser.add_argument("--scale", type=float, default=4.0,
                        help="big = small * scale (default: 4).")
    parser.add_argument("--force", action="store_true",
                        help="Regenerate big from small even when both exist.")
    parser.add_argument("--nearest", action="store_true",
                        help="Nearest-neighbour resampling (crisp pixel art) "
                             "instead of Lanczos.")
    parser.add_argument("--no-seed", action="store_true",
                        help="Skip seeding placeholders for cards in cards.json "
                             "that have no art yet.")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print actions without writing files.")
    args = parser.parse_args()

    if args.scale <= 0:
        sys.exit("--scale must be > 0")

    resample = Image.NEAREST if args.nearest else Image.LANCZOS
    cats = {args.category: CATEGORIES[args.category]} if args.category else CATEGORIES

    # Seed placeholders first so the round-trip pass below upscales them to big/.
    # Skipped when a single category other than the card ones is requested.
    if not args.no_seed and args.category in (None, "card_portraits", "familiar"):
        seed_card_placeholders(args.dry_run)
        print()

    total = 0
    for name, (small_dir, big_dir) in cats.items():
        total += process_category(name, small_dir, big_dir, args.scale, resample,
                                  args.force, args.dry_run)

    print()
    if args.dry_run:
        print(f"Dry run: {total} variant(s) would be generated.")
    else:
        print(f"Generated {total} variant(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
