"""Push name / logo / description / settings from config + Docs/paratranz-description.md
to every existing ParaTranz project (PUT /projects/{id}).

    PARATRANZ_API_KEY=... python tools/localization/update_projects.py [lang ...]
"""
import sys

from paratranz_common import Client, api_key, load_config, project_body, projects


def main():
    config = load_config()
    client = Client(api_key())
    ids = projects(config)
    wanted = sys.argv[1:] or list(ids)
    for lang in wanted:
        pid = ids.get(lang)
        if pid is None:
            print(f"{lang}: no project in config, skipping")
            continue
        r = client.put_json(f"/projects/{pid}", project_body(config, lang))
        print(f"{lang} ({pid}): {'updated' if r.ok else f'FAILED {r.status_code} {r.text[:300]}'}")


if __name__ == "__main__":
    main()
