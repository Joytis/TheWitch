#!/usr/bin/env python3
"""Extract the card-frame layers the Frame Previewer (pages/frame-preview.html) composites.

Reads the base-game card template from the local decompile (gamedata/, gitignored) and the mod's
card pools, and writes a small committed bundle the static GitHub Pages tool loads:

    pages/frame-preview/assets.js   window.FRAME_ASSETS = {
        images: frame_{attack,skill,power}, border_{attack,skill,power}  (ui_atlas *_s slices, raw)
                banner, plaque, energy_*/text_energy_*                    (as PNG data URIs)
        fonts:  kreon_regular, kreon_bold                                  (SIL OFL, as data URIs)
        styles / rarities: HSV material params }

Everything is inlined as data URIs on purpose: the page recolours layers with getImageData, which
browsers refuse on file:// images — inlining keeps the tool working when opened straight from disk.

Colour is NOT baked in: the page runs the game's hsv.gdshader math per layer, using the frame
material of each style (mod pools: their H/S/V overrides, which BaseLib feeds to the same shader)
and the banner material of each rarity. Re-run after changing a pool's H/S/V or adding a character:

    py tools/build-frame-preview.py
"""
import base64
import io
import json
import re
import sys
from datetime import date
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
GAME = ROOT / "gamedata"
OUT = ROOT / "pages" / "frame-preview"

# Frame styles offered by the previewer. `pool` = mod card pool whose H/S/V overrides drive the
# frame material; `mat` = a base-game frame material instead. `energy` / `textEnergy` = cost orb
# and inline [E] icon: repo-relative png, or a gamedata atlas slice (.tres).
WITCH_ENERGY = {"energy": "TheWitch/TheWitch/images/charui/big_energy.png",
                "textEnergy": "TheWitch/TheWitch/images/charui/text_energy.png"}
STYLES = [
    {"id": "witch", "label": "Witch", "sub": "Rose Brown",
     "pool": "TheWitch/TheWitchCode/Character/WitchCardPool.cs", **WITCH_ENERGY},
    {"id": "familiar", "label": "Familiar", "sub": "Blue",
     "pool": "TheWitch/TheWitchCode/Character/WitchFamiliarCardPool.cs", **WITCH_ENERGY},
    {"id": "status", "label": "Status", "sub": "Grey",
     "mat": "materials/cards/frames/card_frame_colorless_mat.tres",
     "energy": "gamedata:images/atlases/ui_atlas.sprites/card/energy_colorless.tres",
     "textEnergy": "gamedata:images/atlases/ui_atlas.sprites/card/energy_colorless.tres"},
    {"id": "augur", "label": "Augur", "sub": "The Augur",
     "pool": "TheAugur/TheAugurCode/Character/AugurCardPool.cs",
     "energy": "TheAugur/TheAugur/images/charui/big_energy.png",
     "textEnergy": "TheAugur/TheAugur/images/charui/text_energy.png"},
]

# CardModel.BannerMaterialPath + NCard.GetTitleLabelOutlineColor (StsColors).
RARITIES = [
    {"id": "common", "label": "Common", "note": "also Starter / Token", "mat": "common", "outline": "4D4B40"},
    {"id": "uncommon", "label": "Uncommon", "mat": "uncommon", "outline": "005C75"},
    {"id": "rare", "label": "Rare", "mat": "rare", "outline": "6B4B00"},
    {"id": "status", "label": "Status", "mat": "status", "outline": "4F522F"},
    {"id": "curse", "label": "Curse", "mat": "curse", "outline": "550B9E"},
    {"id": "event", "label": "Event", "mat": "event", "outline": "1B6131"},
    {"id": "quest", "label": "Quest", "mat": "quest", "outline": "7E3E15"},
    {"id": "ancient", "label": "Ancient", "note": "full-art frame", "mat": "ancient", "outline": "4D4B40"},
]

CARD_SPRITES = "images/atlases/ui_atlas.sprites/card"
TEMPLATE_SPRITES = "images/atlases/compressed.sprites/card_template"


def fail(msg: str) -> None:
    print(f"build-frame-preview: {msg}", file=sys.stderr)
    sys.exit(1)


def load_atlas_tres(rel: str) -> Image.Image:
    """Godot AtlasTexture -> full-size image (region pasted at margin offset, margin grows the canvas)."""
    text = (GAME / rel).read_text(encoding="utf-8")
    atlas = re.search(r'path="res://([^"]+)"', text).group(1)
    rx, ry, rw, rh = map(float, re.search(r"region = Rect2\(([^)]*)\)", text).group(1).split(","))
    m = re.search(r"margin = Rect2\(([^)]*)\)", text)
    mx, my, mw, mh = map(float, m.group(1).split(",")) if m else (0, 0, 0, 0)
    with Image.open(GAME / atlas) as page:
        region = page.convert("RGBA").crop((int(rx), int(ry), int(rx + rw), int(ry + rh)))
    full = Image.new("RGBA", (int(rw + mw), int(rh + mh)), (0, 0, 0, 0))
    full.paste(region, (int(mx), int(my)))
    return full


def mat_hsv(rel: str) -> dict:
    text = (GAME / rel).read_text(encoding="utf-8")
    return {k: float(re.search(rf"shader_parameter/{k} = ([0-9.eE+-]+)", text).group(1)) for k in "hsv"}


def pool_hsv(rel: str) -> dict:
    text = (ROOT / rel).read_text(encoding="utf-8-sig")
    out = {}
    for k in "HSV":
        m = re.search(rf"override float {k}\s*=>\s*([0-9.]+)f?", text)
        if not m:
            fail(f"{rel}: no `override float {k} =>` (ShaderColor-based pools aren't parsed yet)")
        out[k.lower()] = float(m.group(1))
    return out


def ancient_layers() -> dict:
    """The Ancient (full-art) frame, baked from card.tscn's Ancient* nodes so the page needs no shader
    reads of the (possibly cross-origin) portrait:
      ancient_mask    PortraitCanvasGroup mask (canvas_group_mask_blur.gdshader with
                      card_canvas_group_mask_material's region/offset, edge-clamped), over the 598x842 portrait
      ancient_glass_add / ancient_glass_white
                      AncientBorderGlassOverlay (debug_ancient_card_border_overlay.gdshader): screen + 0.15,
                      mixed toward white by mask.g * 0.2 -> an additive pass + a white source-over pass
      ancient_border  AncientBorder: modulate (1, .978, .906, .502), blend_add -> drawn with 'lighter'
      ancient_textbg_*, ancient_banner, ancient_flame: plain textures (text bg is drawn as a .66 black silhouette)
    """
    out = {}
    # mask: atlas px = (605 + u*610, 1143 + v*855) clamped to [615, 1225] x [1151, 2006]
    with Image.open(GAME / "images/atlases/compressed_0.png") as page:
        a = page.convert("RGBA").split()[3]
    region = a.crop((615, 1151, 1225, 2006))
    padded = Image.new("L", (620, 863))
    padded.paste(region, (10, 8))
    padded.paste(region.crop((0, 0, 1, 855)).resize((10, 855)), (0, 8))
    padded.paste(padded.crop((0, 8, 620, 9)).resize((620, 8)), (0, 0))
    mask = padded.crop((0, 0, 610, 855)).resize((598, 842), Image.LANCZOS)
    out["ancient_mask"] = png_uri(Image.merge("RGBA", (mask, mask, mask, mask)))

    size = (591, 832)   # AncientBorderGlassOverlay rect (590.5 x 832) at 2x card units / 0.5 scale
    with Image.open(GAME / "images/vfx/ui/ui_card_mask.png") as m1,          Image.open(GAME / "images/vfx/ui/card/ancient/ui_card_ancient_border_main.png") as m2:
        main_a = m1.convert("RGBA").resize(size, Image.BILINEAR).split()[3]
        mr, mg, mb, ma = m2.convert("RGBA").resize(size, Image.BILINEAR).split()
    a = [x * y / 255 for x, y in zip(main_a.tobytes(), ma.tobytes())]
    k = [g / 255 * 0.2 for g in mg.tobytes()]
    white = Image.new("L", size, 255)
    add_a = Image.new("L", size); add_a.putdata([round(0.15 * (1 - kk) * aa) for aa, kk in zip(a, k)])
    wht_a = Image.new("L", size); wht_a.putdata([round(aa * kk) for aa, kk in zip(a, k)])
    out["ancient_glass_add"] = png_uri(Image.merge("RGBA", (white, white, white, add_a)))
    out["ancient_glass_white"] = png_uri(Image.merge("RGBA", (white, white, white, wht_a)))

    border = load_atlas_tres(f"{TEMPLATE_SPRITES}/ancient_card_border.tres")
    r, g, b, al = border.split()
    tint = lambda ch, f: ch.point(lambda v: round(v * f))
    out["ancient_border"] = png_uri(Image.merge("RGBA", (tint(r, 1.0), tint(g, 0.9776916), tint(b, 0.9058309), tint(al, 0.502))))
    for ty in ("attack", "skill", "power"):
        out[f"ancient_textbg_{ty}"] = png_uri(load_atlas_tres(f"{TEMPLATE_SPRITES}/ancient_card_text_bg_{ty}.tres"))
    out["ancient_banner"] = png_uri(load_atlas_tres(f"{CARD_SPRITES}/ancient_banner.tres"))
    out["ancient_flame"] = png_uri(load_atlas_tres(f"{TEMPLATE_SPRITES}/ancient_flame/ancient_card_flame_0.tres"))
    return out


def png_uri(img: Image.Image) -> str:
    buf = io.BytesIO()
    img.save(buf, "PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("ascii")


def icon(src: str) -> Image.Image:
    if src.startswith("gamedata:"):
        return load_atlas_tres(src.removeprefix("gamedata:"))
    with Image.open(ROOT / src) as im:
        return im.convert("RGBA")


def main() -> None:
    if not (GAME / CARD_SPRITES).is_dir():
        fail("gamedata/ decompile not found — this script needs the local base-game assets")
    OUT.mkdir(parents=True, exist_ok=True)

    images = {}
    for t in ("attack", "skill", "power"):
        images[f"frame_{t}"] = png_uri(load_atlas_tres(f"{CARD_SPRITES}/card_frame_{t}_s.tres"))
        images[f"border_{t}"] = png_uri(load_atlas_tres(f"{CARD_SPRITES}/card_portrait_border_{t}_s.tres"))
    images["banner"] = png_uri(load_atlas_tres(f"{CARD_SPRITES}/card_banner.tres"))
    with Image.open(GAME / "images/ui/cards/card_portrait_border_plaque2.png") as im:
        images["plaque"] = png_uri(im.convert("RGBA"))

    images.update(ancient_layers())

    styles = []
    for s in STYLES:
        for key in ("energy", "textEnergy"):
            name = f"{key}_{s['id']}"
            images[name] = png_uri(icon(s[key]))
        hsv = pool_hsv(s["pool"]) if "pool" in s else mat_hsv(s["mat"])
        styles.append({"id": s["id"], "label": s["label"], "sub": s["sub"], "hsv": hsv,
                       "source": s.get("pool") or f"gamedata/{s['mat']}"})

    rarities = [{"id": r["id"], "label": r["label"], "note": r.get("note", ""), "outline": r["outline"],
                 "hsv": mat_hsv(f"materials/cards/banners/card_banner_{r['mat']}_mat.tres")} for r in RARITIES]

    fonts = {f: "data:font/ttf;base64," + base64.b64encode((GAME / "fonts" / f"{f}.ttf").read_bytes()).decode("ascii")
             for f in ("kreon_regular", "kreon_bold")}

    bundle = {"generated": date.today().isoformat(), "styles": styles, "rarities": rarities,
              "images": images, "fonts": fonts}
    out = OUT / "assets.js"
    out.write_text(
        "// Generated by tools/build-frame-preview.py from gamedata/ + the mod card pools. Do not edit by hand.\n"
        "// Base-game card template art (c) Mega Crit; Kreon font (SIL Open Font License).\n"
        f"window.FRAME_ASSETS = {json.dumps(bundle, separators=(',', ':'))};\n", encoding="utf-8")
    print(f"frame-preview: {len(styles)} styles, {len(rarities)} rarities, {len(images)} images "
          f"-> {out.relative_to(ROOT)} ({out.stat().st_size // 1024} KB)")
    for s in styles:
        print(f"  {s['id']:9} h={s['hsv']['h']:<6} s={s['hsv']['s']:<6} v={s['hsv']['v']:<6} ({s['source']})")


if __name__ == "__main__":
    main()
