"""Seed ParaTranz with translations already committed in the repo for one language.

    PARATRANZ_API_KEY=... python tools/localization/para_upload_translations.py <lang> [--force]

--force overwrites strings that are already translated on ParaTranz.
"""
import sys

from paratranz_common import REPO_ROOT, Client, api_key, load_config, projects, remote_path


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    force = "--force" in sys.argv
    if len(args) != 1:
        sys.exit(__doc__)
    lang = args[0]
    config = load_config()
    pid = projects(config).get(lang)
    if pid is None:
        sys.exit(f"'{lang}' has no ParaTranz project in config.")
    client = Client(api_key())
    existing = {f["name"]: f for f in client.list_files(pid)}
    lang_dir = REPO_ROOT / config["mod_dir"] / "localization" / lang
    for local in sorted(lang_dir.glob("*.json")):
        rpath = remote_path(config, lang, local.name)
        if rpath not in existing:
            print(f"  skip (no source file on ParaTranz yet): {rpath}")
            continue
        r = client.update_translation(pid, existing[rpath]["id"], local, rpath, force=force)
        if r.ok:
            print(f"  uploaded: {rpath}")
        else:
            print(f"  FAILED {r.status_code}: {rpath} - {r.text[:200]}")


if __name__ == "__main__":
    main()
