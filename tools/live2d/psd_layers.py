"""List a PSD's layers with blend mode, opacity, clipping and bounds (canvas pixels).

Use it on an artist's Live2D source PSD to find the layers that need a non-Normal blend in the
Cubism model: `mul ` = Multiply, `lddg` = Linear Dodge (Add). Those are the parts to check in the
game log's `[live2d] ...: parts by blend shader:` line (see Docs/live2d.md).

Stdlib only, Python 3.8+. Reads PSD (not PSB) layer records; skips group end markers.

Usage:  py -I tools/live2d/psd_layers.py <file.psd> [more.psd ...]
"""
import struct
import sys


def u32(b, o): return struct.unpack_from(">I", b, o)[0]
def i32(b, o): return struct.unpack_from(">i", b, o)[0]
def u16(b, o): return struct.unpack_from(">H", b, o)[0]
def i16(b, o): return struct.unpack_from(">h", b, o)[0]


def layers(path):
    b = open(path, "rb").read()
    if b[:4] != b"8BPS" or u16(b, 4) != 1:
        raise ValueError(f"{path}: not a PSD (PSB is not supported)")
    height, width = u32(b, 14), u32(b, 18)
    o = 26
    o += 4 + u32(b, o)  # color mode data
    o += 4 + u32(b, o)  # image resources
    o += 4              # layer and mask info length
    o += 4              # layer info length
    count = abs(i16(b, o)); o += 2
    out = []
    for _ in range(count):
        top, left, bottom, right = i32(b, o), i32(b, o + 4), i32(b, o + 8), i32(b, o + 12); o += 16
        channels = u16(b, o); o += 2 + channels * 6
        if b[o:o + 4] != b"8BIM":
            raise ValueError(f"{path}: unexpected layer record signature {b[o:o + 4]!r}")
        blend = b[o + 4:o + 8].decode(); o += 8
        opacity, clipping, flags = b[o], b[o + 1], b[o + 2]; o += 4
        end = o + 4 + u32(b, o); o += 4
        p = o
        p += 4 + u32(b, p)  # layer mask data
        p += 4 + u32(b, p)  # blending ranges
        n = b[p]; name = b[p + 1:p + 1 + n].decode("latin-1")
        p += ((1 + n + 3) // 4) * 4
        section = None
        while p + 12 <= end:
            key, length = b[p + 4:p + 8], u32(b, p + 8)
            data = b[p + 12:p + 12 + length]
            if key == b"luni":
                name = data[4:4 + 2 * u32(data, 0)].decode("utf-16-be")
            elif key in (b"lsct", b"lsdk"):
                section = u32(data, 0)
            p += 12 + length
        out.append((name, blend, opacity, clipping, (left, top, right, bottom), section, flags))
        o = end
    return width, height, out


def main(paths):
    for path in paths:
        width, height, recs = layers(path)
        print(f"== {path} ({width}x{height}), bottom layer first")
        for name, blend, opacity, clipping, box, section, flags in recs:
            if section == 3:  # group end marker
                continue
            group = {1: " [group]", 2: " [group]"}.get(section, "")
            hidden = " [hidden]" if flags & 2 else ""
            clip = " clipped" if clipping else ""
            mark = "   <-- " + blend.strip() if blend not in ("norm", "pass") else ""
            print(f"  {name!r:32} {blend} opacity={opacity:3}{clip} box={box}{group}{hidden}{mark}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    main(sys.argv[1:])
