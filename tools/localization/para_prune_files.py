"""Remove ParaTranz files that no longer match the repo's English source set.

    PARATRANZ_API_KEY=... python tools/localization/para_prune_files.py            # dry run
    PARATRANZ_API_KEY=... python tools/localization/para_prune_files.py --apply
    PARATRANZ_API_KEY=... python tools/localization/para_prune_files.py --apply --force

Why: ParaTranz keys files by full path. Moving the repo's localization folder (2026-09-20,
TheWitch/localization -> TheWitch/TheWitch/localization) changed the path the upload script
computed, so it CREATED a second copy of every file instead of updating the existing ones -
doubling every project's string count and leaving the copies untranslated. The remote path is
now pinned by `remote_dir` in .github/configs/paratranz.json, and this script deletes whatever
does not belong at that path.

Safety: a stray file is deleted only when it holds zero translated strings. One with
translations is reported and kept (its strings would be lost) unless --force is given - move
them first with the web UI, or accept the loss knowingly.
"""
import sys

from paratranz_common import Client, api_key, expected_remote_paths, load_config, projects


def main():
    apply = "--apply" in sys.argv
    force = "--force" in sys.argv
    config = load_config()
    client = Client(api_key())
    stray_total = 0
    kept = 0
    failed = 0
    for lang, pid in projects(config).items():
        expected = expected_remote_paths(config, lang)
        print(f"\n--- {lang} (project {pid}) ---")
        for f in client.list_files(pid):
            if f["name"] in expected:
                continue
            stray_total += 1
            translated = f.get("translated", 0)
            label = f"{f['name']} (id {f['id']}, {translated}/{f.get('total', 0)} translated)"
            if translated and not force:
                kept += 1
                print(f"  KEEP  {label} - has translations; pass --force to delete anyway")
                continue
            if not apply:
                print(f"  would delete {label}")
                continue
            r = client.delete_file(pid, f["id"])
            if r.ok:
                print(f"  deleted {label}")
            else:
                failed += 1
                print(f"  FAILED {r.status_code}: {label} - {r.text[:200]}")
    print(f"\n{stray_total} stray file(s){'' if apply else ' (dry run - pass --apply to delete)'}; {kept} kept.")
    if failed:
        raise SystemExit(f"{failed} delete(s) failed")


if __name__ == "__main__":
    main()
