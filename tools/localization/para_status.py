"""Snapshot ParaTranz project health for the dashboard (pages/analytics.html, Localization tab).

    PARATRANZ_API_KEY=... python tools/localization/para_status.py

Writes pages/analytics-data/paratranz.json: per language the project's string counts,
member count, and every join application that nobody has acted on yet (skimmed to
what the dashboard shows: who, when, what they wrote). Run by loc-download.yml right
after the translation sync so the page never lags the projects by more than a day.

/projects/{id}/applications is not in the public API docs; shape observed live:
  status  2 = accepted, 1 = rejected (operator + reason set), 0 = pending,
         -1 = expired without an answer (applicants re-apply), -2 = revoked by applicant
  (confirmed against the web app: Approve/Reject buttons render only for status 0)
  operator null <=> nobody has handled it yet.
"""
import json
from datetime import datetime, timezone
from pathlib import Path

from paratranz_common import LANG_NAMES, Client, api_key, load_config, projects

OUT = Path(__file__).resolve().parents[2] / "pages" / "analytics-data" / "paratranz.json"
MESSAGE_CHARS = 400
STATUS_NAMES = {0: "pending", -1: "expired", -2: "revoked", 1: "rejected", 2: "accepted"}


def skim_application(app: dict) -> dict:
    user = app.get("user") or {}
    content = (app.get("content") or "").strip()
    if len(content) > MESSAGE_CHARS:
        content = content[:MESSAGE_CHARS].rstrip() + "…"
    return {
        "id": app["id"],
        "created_at": app.get("createdAt"),
        "status": app.get("status"),
        "status_name": STATUS_NAMES.get(app.get("status"), str(app.get("status"))),
        "username": user.get("username"),
        "nickname": user.get("nickname"),
        "user_id": user.get("id"),
        "last_visit": user.get("lastVisit"),
        "message": content,
    }


def fetch_all(client: Client, path: str) -> list[dict]:
    """Paginated endpoints answer {results, page, pageCount}; plain lists come back as-is."""
    first = client.get_json(f"{path}?page=1&pageSize=100")
    if isinstance(first, list):
        return first
    rows = list(first.get("results", []))
    for page in range(2, int(first.get("pageCount", 1)) + 1):
        rows.extend(client.get_json(f"{path}?page={page}&pageSize=100").get("results", []))
    return rows


def main() -> int:
    config = load_config()
    client = Client(api_key())
    langs = []
    total_open = 0
    for lang, pid in sorted(projects(config).items()):
        info = client.get_json(f"/projects/{pid}")
        stats = info.get("stats") or {}
        members = fetch_all(client, f"/projects/{pid}/members")
        apps = fetch_all(client, f"/projects/{pid}/applications")
        open_apps = [skim_application(a) for a in apps if not a.get("operator")]
        open_apps.sort(key=lambda a: a["created_at"] or "", reverse=True)
        total_open += sum(1 for a in open_apps if a["status"] == 0)
        langs.append({
            "lang": lang,
            "name": LANG_NAMES.get(lang, lang),
            "project_id": pid,
            "url": f"https://paratranz.cn/projects/{pid}",
            "members": len(members),
            "total": stats.get("total", 0),
            "translated": stats.get("translated", 0),
            "reviewed": stats.get("reviewed", 0),
            "open_applications": open_apps,
        })
        print(f"{lang} ({pid}): {stats.get('translated', 0)}/{stats.get('total', 0)} translated,"
              f" {len(members)} members, {len(open_apps)} unanswered application(s)")
    payload = {
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "pending_total": total_open,
        "projects": langs,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, ensure_ascii=False, indent=1) + "\n",
                   encoding="utf-8", newline="\n")
    print(f"wrote {OUT.relative_to(OUT.parents[2]).as_posix()} ({total_open} pending)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
