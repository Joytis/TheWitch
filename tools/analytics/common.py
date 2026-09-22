"""Shared plumbing for the analytics scripts: Supabase fetch, read-key discovery, and the
card-metadata lookups. Dispatched via tools/analytics/analytics.ps1. The interactive
charts live in pages/analytics.html, fed by export_stats.py."""

import json
import os
from datetime import datetime, timedelta, timezone
from pathlib import Path

import pandas as pd
import requests

SUPABASE_URL = "https://bjwqinohtgsvnbscnvmb.supabase.co/rest/v1/runs"
KEY_FILE = Path(__file__).with_name("supabase-service-key.local.txt")  # gitignored
REPO = Path(__file__).resolve().parents[2]

# Per-character config, mirroring Docs/card-data/characters.js (key / mod id / loc prefix /
# card db / archetype mechanics). `id` is what the runs table's `character` column holds and
# what --character takes; `key` names the per-character output folder (pages/analytics-data/<key>).
CHARACTERS = {
    "witch": {"key": "witch", "id": "TheWitch", "prefix": "THEWITCH-",
              "data_file": "Docs/card-data/cards.json",
              # dominant-archetype tie-break order (see export_stats.dominant_archetype)
              "mechanics": ["Hex", "Potions", "Familiars", "Brambles"]},
    "augur": {"key": "augur", "id": "TheAugur", "prefix": "THEAUGUR-",
                "data_file": "Docs/card-data/augur.json",
                "mechanics": ["Foretell", "Omen", "Augury"]},
}
DEFAULT_CHARACTER = "witch"
BASE_CLASS_FILES = ("silent", "necrobinder", "ironclad")


def character_config(character: str | None) -> dict:
    """Resolve a mod id ("TheWitch"), key ("witch") or None (default Witch) to its config."""
    if not character:
        return CHARACTERS[DEFAULT_CHARACTER]
    for ch in CHARACTERS.values():
        if character in (ch["id"], ch["key"]):
            return ch
    raise SystemExit(f"unknown character {character!r} (expected one of: "
                     + ", ".join(f"{c['id']}/{c['key']}" for c in CHARACTERS.values()) + ")")


def default_key() -> str | None:
    if key := os.environ.get("SUPABASE_READ_KEY"):
        return key
    if KEY_FILE.exists():
        return KEY_FILE.read_text(encoding="utf-8").strip()
    return None


def missing_key_message() -> str:
    return ("No read key: pass --key, set SUPABASE_READ_KEY, or create "
            f"{KEY_FILE.name} next to this script (anon key is insert-only).")


def add_common_args(parser) -> None:
    parser.add_argument("--key", default=default_key())
    parser.add_argument("--mod-version", default=None, help="filter to one mod release (default: all)")
    parser.add_argument("--game-version", default=None, help="filter to one StS2 build (default: all)")
    parser.add_argument("--days-back", type=int, default=None, help="only runs from the last N days")


# Supabase caps a single response at 1000 rows regardless of the requested limit, and with no
# explicit order the truncated tail was the NEWEST runs (the 2026-08-21/22 dashboard gap). Page
# through everything instead.
PAGE_SIZE = 1000


def fetch_runs(key: str, mod_version: str | None, game_version: str | None,
               days_back: int | None, character: str | None = None) -> pd.DataFrame:
    params = {"select": "victory,ascension,floor,data,mod_version,game_version,created_at",
              "order": "created_at.asc", "limit": str(PAGE_SIZE)}
    # `character` = uploading mod id ("TheWitch" / "TheAugur"); the shared runs table gained the
    # column with tools/analytics/migrations/001_character_column.sql. Opt-in so the export keeps
    # working until that migration is applied.
    if character:
        params["character"] = f"eq.{character}"
    if mod_version:
        params["mod_version"] = f"eq.{mod_version}"
    if game_version:
        params["game_version"] = f"eq.{game_version}"
    if days_back:
        since = datetime.now(timezone.utc) - timedelta(days=days_back)
        params["created_at"] = f"gte.{since.isoformat()}"
    rows: list[dict] = []
    offset = 0
    while True:
        resp = requests.get(
            SUPABASE_URL,
            params=params | {"offset": str(offset)},
            headers={"apikey": key, "Authorization": f"Bearer {key}"},
            timeout=30,
        )
        resp.raise_for_status()
        page = resp.json()
        rows.extend(page)
        if len(page) < PAGE_SIZE:
            return pd.DataFrame(rows)
        offset += PAGE_SIZE


def _load_cards(path: Path) -> list[dict]:
    data = json.loads(path.read_text(encoding="utf-8"))
    return data["cards"] if isinstance(data, dict) else data


def card_rarities(character: str | None = None) -> dict[str, str]:
    """Entry -> rarity, from the character's card db plus the base-game class dumps. The mod
    character's entries are mapped both bare and with its loc prefix (uploads carry the prefix;
    own-card charts strip it)."""
    ch = character_config(character)
    rarities: dict[str, str] = {}
    for name in BASE_CLASS_FILES:
        for card in _load_cards(REPO / f"Docs/card-data/{name}.json"):
            if card.get("entry") and card.get("rarity"):
                rarities.setdefault(card["entry"], card["rarity"])
    for card in _load_cards(REPO / ch["data_file"]):
        entry, rarity = card.get("entry"), card.get("rarity")
        if entry and rarity:
            rarities[entry] = rarities[f"{ch['prefix']}{entry}"] = rarity
    return rarities


def card_mechanics(character: str | None = None) -> dict[str, set[str]]:
    """Bare mod-card entry -> mechanics tags (Witch: Potions/Hex/Familiars/Brambles/None)."""
    ch = character_config(character)
    return {c["entry"]: set(c.get("mechanics", [])) for c in _load_cards(REPO / ch["data_file"])}
