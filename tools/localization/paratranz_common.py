"""Shared helpers for the ParaTranz sync scripts.

Config lives in .github/configs/paratranz.json:
  mod_dir      - repo folder holding localization/<lang>/*.json (TheWitch)
  languages    - game language code -> ParaTranz language code (used by create_projects.py)
  projects     - game language code -> ParaTranz project id (filled by create_projects.py)

Auth: PARATRANZ_API_KEY env var (ParaTranz -> Settings -> API Token).
API reference: https://paratranz.cn/api-docs
"""
import json
import os
import sys
import time
from pathlib import Path

import requests

API = "https://paratranz.cn/api"
REPO_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = REPO_ROOT / ".github" / "configs" / "paratranz.json"


def load_config():
    with open(CONFIG_PATH, encoding="utf-8") as f:
        return json.load(f)


def save_config(config):
    with open(CONFIG_PATH, "w", encoding="utf-8", newline="\n") as f:
        json.dump(config, f, ensure_ascii=False, indent=2)
        f.write("\n")


def api_key():
    token = os.environ.get("PARATRANZ_API_KEY")
    if not token:
        sys.exit("PARATRANZ_API_KEY is not set.")
    return token


class Client:
    def __init__(self, token):
        self.s = requests.Session()
        self.s.headers["Authorization"] = token
        self.s.headers["accept"] = "*/*"

    def _req(self, method, path, retries=3, **kw):
        url = f"{API}{path}"
        r = None
        for attempt in range(retries):
            r = self.s.request(method, url, timeout=60, **kw)
            if r.status_code < 500 and r.status_code != 429:
                return r
            if attempt < retries - 1:
                time.sleep(2 ** attempt)
        return r

    def get_json(self, path):
        r = self._req("GET", path)
        r.raise_for_status()
        return r.json()

    def post_json(self, path, body):
        return self._req("POST", path, json=body)

    def put_json(self, path, body):
        return self._req("PUT", path, json=body)

    def list_files(self, project_id):
        return self.get_json(f"/projects/{project_id}/files")

    def create_file(self, project_id, local_file, remote_path):
        """POST /projects/{id}/files  (multipart: file, path=<folder>/)"""
        folder = str(Path(remote_path).parent).replace("\\", "/")
        with open(local_file, "rb") as fh:
            return self._req(
                "POST", f"/projects/{project_id}/files",
                files={"file": (Path(remote_path).name, fh, "application/json")},
                data={"path": folder + "/" if folder and folder != "." else ""},
            )

    def update_file(self, project_id, file_id, local_file, remote_name):
        """POST /projects/{id}/files/{fid}  (replace source strings)"""
        with open(local_file, "rb") as fh:
            return self._req(
                "POST", f"/projects/{project_id}/files/{file_id}",
                files={"file": (Path(remote_name).name, fh, "application/json")},
            )

    def update_translation(self, project_id, file_id, local_file, remote_name, force=False):
        """POST /projects/{id}/files/{fid}/translation  (import translations)"""
        with open(local_file, "rb") as fh:
            return self._req(
                "POST", f"/projects/{project_id}/files/{file_id}/translation",
                files={"file": (Path(remote_name).name, fh, "application/json")},
                data={"force": "true"} if force else None,
            )

    def get_translation(self, project_id, file_id):
        return self.get_json(f"/projects/{project_id}/files/{file_id}/translation")


LANG_NAMES = {
    "zhs": "Simplified Chinese", "deu": "German", "fra": "French", "ita": "Italian",
    "jpn": "Japanese", "kor": "Korean", "ptb": "Brazilian Portuguese", "rus": "Russian",
    "spa": "Spanish", "esp": "Spanish (Spain)", "pol": "Polish", "tha": "Thai", "tur": "Turkish",
}


def project_body(config, lang):
    """Project fields shared by create_projects.py and update_projects.py.
    `desc` is the one-line blurb shown on the project card - keep it short.
    """
    tagline = config["tagline"].format(lang_name=LANG_NAMES.get(lang, lang))
    return {
        "name": config["project_name"].format(lang=lang),
        "logo": config.get("logo", ""),
        "desc": tagline,
        "source": "en",
        "dest": config["languages"][lang],
        "game": "other",
        "privacy": 0,      # public
        "download": 1,     # members can download
        "issueMode": 0,    # anyone can open discussions
        "reviewMode": 1,   # one review pass
        "joinMode": 1,     # apply to join
        "extra": {         # undocumented; shape observed on live projects
            "isMod": True,
            "link": config.get("workshop_url", config["repo_url"]),
        },
    }


def eng_files(config):
    eng_dir = REPO_ROOT / config["mod_dir"] / "localization" / "eng"
    if not eng_dir.is_dir():
        sys.exit(f"No English localization dir at {eng_dir}")
    # ParaTranz silently drops files with zero strings (e.g. an empty {} table).
    return [p for p in sorted(eng_dir.glob("*.json")) if json.loads(p.read_text(encoding="utf-8-sig"))]


def remote_path(config, lang, filename):
    return f"{config['mod_dir']}/localization/{lang}/{filename}"


def projects(config):
    if not config.get("projects"):
        sys.exit("No ParaTranz projects configured. Run tools/localization/create_projects.py first.")
    return {lang: int(pid) for lang, pid in config["projects"].items()}
