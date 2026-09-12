"""Pull translations from ParaTranz into <mod>/localization/<lang>/*.json.

    PARATRANZ_API_KEY=... python tools/localization/para_download.py

Only strings with stage >= 1 (translated or better) are written, in English key
order. Keys left out fall back to English in-game (LocTable fallback), so partial
translations ship fine.
"""
import json
import re
from collections import OrderedDict
from pathlib import Path

from paratranz_common import REPO_ROOT, Client, api_key, load_config, projects


def main():
    config = load_config()
    client = Client(api_key())
    mod_root = REPO_ROOT / config["mod_dir"] / "localization"
    grand = 0
    for lang, pid in projects(config).items():
        print(f"\n--- {lang} (project {pid}) ---")
        files = [f for f in client.list_files(pid) if "TM" not in f["name"]]
        lang_total = 0
        for f in files:
            parts = Path(f["name"]).parts
            if len(parts) != 4 or parts[1] != "localization" or parts[2] != lang:
                print(f"  skip (unexpected path): {f['name']}")
                continue
            filename = parts[3]
            source_path = mod_root / "eng" / filename
            if not source_path.exists():
                print(f"  skip (no English source in repo): {filename}")
                continue
            translated = {}
            for item in client.get_translation(pid, f["id"]):
                if item.get("stage", 0) >= 1 and item.get("translation"):
                    translated[item["key"]] = re.sub(r'\\"', '"', item["translation"])
            out_path = mod_root / lang / filename
            if not translated:
                if out_path.exists():
                    out_path.unlink()
                print(f"  {filename}: 0 translated")
                continue
            source = json.loads(source_path.read_text(encoding="utf-8-sig"),
                                object_pairs_hook=OrderedDict)
            ordered = OrderedDict((k, translated[k]) for k in source if k in translated)
            out_path.parent.mkdir(parents=True, exist_ok=True)
            with open(out_path, "w", encoding="utf-8", newline="\n") as fh:
                json.dump(ordered, fh, ensure_ascii=False, indent=4)
                fh.write("\n")
            print(f"  {filename}: {len(ordered)}/{len(source)} translated")
            lang_total += len(ordered)
        grand += lang_total
        print(f"  {lang}: {lang_total} strings")
    print(f"\nDone. {grand} strings written.")


if __name__ == "__main__":
    main()
