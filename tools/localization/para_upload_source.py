"""Push the English source files to every ParaTranz project (create or update).

    PARATRANZ_API_KEY=... python tools/localization/para_upload_source.py
"""
from paratranz_common import Client, api_key, eng_files, load_config, projects, remote_path


def main():
    config = load_config()
    client = Client(api_key())
    files = eng_files(config)
    failed = 0
    for lang, pid in projects(config).items():
        print(f"\n--- {lang} (project {pid}) ---")
        existing = {f["name"]: f for f in client.list_files(pid)}
        for local in files:
            rpath = remote_path(config, lang, local.name)
            if rpath in existing:
                r = client.update_file(pid, existing[rpath]["id"], local, rpath)
                verb = "updated"
            else:
                r = client.create_file(pid, local, rpath)
                verb = "created"
            if r.ok:
                print(f"  {verb}: {rpath}")
            else:
                failed += 1
                print(f"  FAILED {r.status_code}: {rpath} - {r.text[:200]}")
    if failed:
        raise SystemExit(f"{failed} upload(s) failed")


if __name__ == "__main__":
    main()
