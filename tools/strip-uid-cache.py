"""Strip game uid->path mappings out of an exported mod .pck (in place).

Godot 4.4+ always exports .godot/uid_cache.bin into the pck (the export exclude
filter does not apply to it), and the editor rebuilds that cache from every file it
can see - including the gamedata/ junctions (src/, scenes/, themes/, ...) - at the
start of every export, so pre-export filtering is undone. The mod pck thus carries a
snapshot of the GAME's uid table as of the decompile. At runtime the game merges the
mod cache over its own, so any game script/scene renamed since the decompile
resolves to a dead path (symptom: bestiary "NBestiaryActDivider.cs class could not
be found" on the beta branch).

This rewrites the pck's uid_cache.bin entry to keep only res://TheWitch/ mappings.
The filtered blob is smaller than the original, so it is written over the old data
at the same offset and the directory entry's size + md5 are patched; nothing moves.

Usage:  py tools/strip-uid-cache.py <path/to/TheWitch.pck>
"""
import hashlib
import struct
import sys
from pathlib import Path

KEEP_PREFIX = b"res://TheWitch/"
PACK_REL_FILEBASE = 2


def filter_cache(blob: bytes) -> tuple[bytes, int, int]:
    count = struct.unpack_from("<I", blob, 0)[0]
    off = 4
    kept = []
    for _ in range(count):
        uid, length = struct.unpack_from("<qI", blob, off)
        off += 12
        path = blob[off:off + length]
        off += length
        if path.startswith(KEEP_PREFIX):
            kept.append(struct.pack("<qI", uid, length) + path)
    if off != len(blob):
        raise SystemExit(f"uid_cache.bin: trailing bytes ({len(blob) - off}) - format changed?")
    return struct.pack("<I", len(kept)) + b"".join(kept), count, len(kept)


def main(pck_path: Path) -> None:
    with open(pck_path, "r+b") as f:
        magic = f.read(4)
        if magic != b"GDPC":
            raise SystemExit(f"{pck_path}: not a Godot pck")
        version = struct.unpack("<I", f.read(4))[0]
        f.read(12)  # engine major/minor/patch
        flags, files_base = struct.unpack("<IQ", f.read(12))
        if version >= 3:
            dir_offset = struct.unpack("<Q", f.read(8))[0]
        else:
            f.read(16 * 4)
            dir_offset = f.tell()
        f.seek(dir_offset)
        count = struct.unpack("<I", f.read(4))[0]
        for _ in range(count):
            name_len = struct.unpack("<I", f.read(4))[0]
            name = f.read(name_len).rstrip(b"\0").decode()
            entry_pos = f.tell()
            offset, size = struct.unpack("<QQ", f.read(16))
            f.read(16 + 4)  # md5 + entry flags
            if name.endswith(".godot/uid_cache.bin"):
                break
        else:
            print("strip-uid-cache: pck has no uid_cache.bin, nothing to do")
            return

        data_pos = offset + (files_base if flags & PACK_REL_FILEBASE else 0)
        f.seek(data_pos)
        blob = f.read(size)
        new_blob, total, kept = filter_cache(blob)
        if len(new_blob) > size:
            raise SystemExit("filtered cache larger than original?!")
        f.seek(data_pos)
        f.write(new_blob + b"\0" * (size - len(new_blob)))
        f.seek(entry_pos)
        f.write(struct.pack("<QQ", offset, len(new_blob)) + hashlib.md5(new_blob).digest())
    print(f"strip-uid-cache: {pck_path.name} uid_cache.bin {total} -> {kept} entries")


if __name__ == "__main__":
    main(Path(sys.argv[1]))
