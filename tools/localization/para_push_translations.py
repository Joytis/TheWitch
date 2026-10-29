"""Push translations committed in the repo to ParaTranz - only the strings a commit range changed.

    PARATRANZ_API_KEY=... python tools/localization/para_push_translations.py <before-sha> <after-sha>

Run by loc-upload-source.yml on every push to main that touches localization/. Translations
written in-repo by /loc-sync (tools/localization/loc_sync.py) must reach ParaTranz, or the
nightly loc-download would overwrite them with the older ParaTranz text. For each
<mod_dir>/localization/<lang>/<file> the range touched, the keys whose value differs from
<before-sha> are imported with force (ParaTranz matches imported strings by key and leaves the
rest of the file alone). Bot commits never trigger this: pushes made with GITHUB_TOKEN start no
workflows, so the nightly download cannot echo back.
"""
import json
import subprocess
import sys
import tempfile
from pathlib import Path

from paratranz_common import REPO_ROOT, Client, api_key, load_config, projects, remote_path


def git_json(sha, path):
    r = subprocess.run(["git", "show", f"{sha}:{path}"], cwd=REPO_ROOT, capture_output=True)
    return json.loads(r.stdout.decode("utf-8-sig")) if r.returncode == 0 else {}


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    before, after = sys.argv[1:]
    config = load_config()
    loc = f"{config['mod_dir']}/localization"
    if set(before) == {"0"}:  # branch creation: no base to diff against
        sys.exit("No base commit; use the loc-upload-translations workflow instead.")
    changed = subprocess.run(["git", "diff", "--name-only", before, after, "--", loc],
                             cwd=REPO_ROOT, capture_output=True, text=True, check=True).stdout.split()
    by_lang = {}
    for path in changed:
        lang, filename = Path(path).parts[-2:]
        if lang != "eng" and filename.endswith(".json"):
            by_lang.setdefault(lang, []).append((path, filename))
    if not by_lang:
        print("No translation files changed.")
        return
    client = Client(api_key())
    pids = projects(config)
    failed = 0
    for lang, files in sorted(by_lang.items()):
        pid = pids.get(lang)
        if pid is None:
            print(f"skip {lang}: no ParaTranz project")
            continue
        existing = {f["name"]: f for f in client.list_files(pid)}
        for path, filename in files:
            old, new = git_json(before, path), git_json(after, path)
            delta = {k: v for k, v in new.items() if v and old.get(k) != v}
            rpath = remote_path(config, lang, filename)
            if not delta:
                continue
            if rpath not in existing:
                print(f"  {lang} skip (no source file on ParaTranz): {rpath}")
                continue
            with tempfile.TemporaryDirectory() as tmp:
                tmp_file = Path(tmp) / filename
                tmp_file.write_text(json.dumps(delta, ensure_ascii=False, indent=4), encoding="utf-8")
                r = client.update_translation(pid, existing[rpath]["id"], tmp_file, rpath, force=True)
            if r.ok:
                print(f"  {lang} {filename}: pushed {len(delta)} string(s)")
            else:
                failed += 1
                print(f"  {lang} FAILED {r.status_code}: {rpath} - {r.text[:200]}")
    if failed:
        raise SystemExit(f"{failed} upload(s) failed")


if __name__ == "__main__":
    main()
