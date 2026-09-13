"""Create one ParaTranz project per language and record the ids in the config.

    PARATRANZ_API_KEY=... python tools/localization/create_projects.py [lang ...]

Idempotent: languages already present in config.projects are skipped.
POST /projects answers 200 (not the documented 201) with the created project;
403 if the account is not allowed to create projects yet.
Description = language header + Docs/paratranz-description.md (see project_body).
"""
import sys

from paratranz_common import Client, api_key, load_config, project_body, save_config


def main():
    config = load_config()
    client = Client(api_key())
    wanted = sys.argv[1:] or list(config["languages"])
    for lang in wanted:
        if lang in config["projects"]:
            print(f"{lang}: already project {config['projects'][lang]}")
            continue
        if lang not in config["languages"]:
            print(f"{lang}: no ParaTranz code in config.languages, skipping")
            continue
        r = client.post_json("/projects", project_body(config, lang))
        if not r.ok or "id" not in r.json():
            print(f"{lang}: FAILED {r.status_code} {r.text[:300]}")
            continue
        pid = r.json()["id"]
        config["projects"][lang] = pid
        save_config(config)
        print(f"{lang}: created project {pid}  https://paratranz.cn/projects/{pid}")


if __name__ == "__main__":
    main()
