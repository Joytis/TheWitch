"""Publish the translator rules (Docs/paratranz-description.md) as a pinned
Announcement in every ParaTranz project. Creates it on first run, updates it
in place afterwards (matched by title). Pinned announcements render on the
project Overview page, under the word/string stats.

    PARATRANZ_API_KEY=... python tools/localization/update_announcements.py [lang ...]

Endpoints (undocumented, verified live): GET/POST /projects/{id}/announcements,
PUT/DELETE /projects/{id}/announcements/{aid}. Content is Markdown; `bumped: true`
is the web UI's Pin switch and is what makes it show on the Overview.
"""
import sys

from paratranz_common import LANG_NAMES, REPO_ROOT, Client, api_key, load_config, projects

TITLE = "Translation rules & links (please read first)"


def announcement_content(config, lang):
    body = (REPO_ROOT / config["announcement_file"]).read_text(encoding="utf-8-sig").strip()
    header = (
        f"**{LANG_NAMES.get(lang, lang)} translation of The Witch**, a Slay the Spire 2 character mod.\n\n"
        f"- Steam Workshop: {config.get('workshop_url', '')}\n"
        f"- Source code: {config['repo_url']}\n"
        f"- English strings sync into this project automatically; finished translations are pulled "
        f"back into the mod nightly and ship with the next release.\n\n---\n\n"
    )
    return header + body


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
        payload = {"title": TITLE, "content": announcement_content(config, lang),
                   "category": "announcement", "internal": False, "bumped": True}  # bumped = pinned to Overview
        existing = [a for a in client.get_json(f"/projects/{pid}/announcements")["results"] if a["title"] == TITLE]
        if existing:
            r = client.put_json(f"/projects/{pid}/announcements/{existing[0]['id']}", payload)
            verb = "updated"
        else:
            r = client.post_json(f"/projects/{pid}/announcements", payload)
            verb = "created"
        print(f"{lang} ({pid}): {verb if r.ok else f'FAILED {r.status_code} {r.text[:200]}'}")


if __name__ == "__main__":
    main()
