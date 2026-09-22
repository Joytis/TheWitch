"""Export additive aggregate stats for the web analytics dashboard (pages/analytics.html).

Publishes COUNT tables (runs, wins, offered, picked, ...) keyed by every filter dimension
so the page can recompose any rate client-side by summing. No raw rows, decks, or player
hashes leave this script. Run by .github/workflows/analytics.yml (SUPABASE_READ_KEY secret)
and locally via ./tools/analytics/analytics.ps1 -Mode export.

Per-character: `--character TheWitch|TheAugur` filters the runs (needs the `character`
column — migrations/001_character_column.sql) and defaults the output folder to
pages/analytics-data/<key> (witch/augur). Without it the export is the historical Witch
export into pages/analytics-data/ (the page reads that folder for the Witch).
"""

import argparse
import json
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

import pandas as pd

import common

OUT_DIR = common.REPO / "pages" / "analytics-data"

# Dominant-archetype rules: a run commits to the mechanic with the most of the character's
# own cards in deck (duplicates count); fewer than COMMIT_THRESHOLD such cards = "Unfocused".
# Ties break in the character's fixed mechanics order (common.CHARACTERS[..]["mechanics"]).
COMMIT_THRESHOLD = 3


def base_keys(run, day: str, data: dict) -> dict:
    return {"day": day, "mod": run.mod_version, "game": run.game_version,
            "asc": int(run.ascension),
            "mp": 1 if data.get("numPlayers", 1) > 1 else 0}


def aggregate(rows: list[dict], keys: list[str], sums: list[str]) -> list[dict]:
    if not rows:
        return []
    return (pd.DataFrame(rows).groupby(keys)[sums].sum().reset_index()
            .to_dict("records"))


def copies_bucket(n: int) -> int:
    return min(n, 3)  # 1, 2, 3+ — enough signal, keeps cardinality flat


def dominant_archetype(deck: list[str], mechanics: dict[str, set[str]], ch: dict) -> str:
    prefix, order = ch["prefix"], ch["mechanics"]
    counts = Counter()
    for card in deck:
        if not card.startswith(prefix):
            continue
        for mech in mechanics.get(card.removeprefix(prefix), []):
            counts[mech] += 1
    best = max(order, key=lambda m: (counts.get(m, 0), -order.index(m)), default="Unfocused")
    return best if counts.get(best, 0) >= COMMIT_THRESHOLD else "Unfocused"


def build_tables(runs: pd.DataFrame, ch: dict) -> dict[str, list[dict]]:
    rarities = common.card_rarities(ch["key"])
    mechanics = common.card_mechanics(ch["key"])
    run_rows, card_rows, choice_rows, hour_rows = [], [], [], []
    death_rows, floor_rows, enc_rows, arch_rows = [], [], [], []

    for run in runs.itertuples():
        created = pd.to_datetime(run.created_at)
        day = created.strftime("%Y-%m-%d")
        win = int(run.victory)
        data = run.data or {}
        keys = base_keys(run, day, data)
        deck = data.get("deck", [])

        arch = dominant_archetype(deck, mechanics, ch)
        run_rows.append(keys | {"runs": 1, "wins": win})
        hour_rows.append(keys | {"hour": int(created.hour), "runs": 1, "wins": win})
        arch_rows.append(keys | {"arch": arch, "runs": 1, "wins": win})

        signal_cards = {c: n for c, n in Counter(deck).items()
                        if rarities.get(c) != "Starter"}  # forced picks carry no signal
        for card, copies in signal_cards.items():
            # arch rides along so the page can slice a card's win rate by the deck's
            # dominant archetype (step 1 of the cluster ladder); summing over it
            # recovers the plain per-card counts.
            card_rows.append(keys | {"card": card, "copies": copies_bucket(copies),
                                     "arch": arch, "runs": 1, "wins": win})

        for screen in data.get("cardChoices", []):
            for card in screen.get("picked", []):
                choice_rows.append(keys | {"card": card, "offered": 1, "picked": 1})
            for card in screen.get("skipped", []):
                choice_rows.append(keys | {"card": card, "offered": 1, "picked": 0})

        killed_by = data.get("killedByEncounter")
        if not win and killed_by and killed_by != "NONE":
            death_rows.append(keys | {"enc": killed_by, "deaths": 1})
            floor_rows.append(keys | {"floor": int(run.floor), "deaths": 1})

        for enc in data.get("encounters", []):
            enc_rows.append(keys | {"enc": enc["id"], "fights": 1,
                                    "dmg": int(enc.get("damage", 0)),
                                    "turns": int(enc.get("turns", 0))})

    keys = ["day", "mod", "game", "asc", "mp"]
    return {
        "runs_daily": aggregate(run_rows, keys, ["runs", "wins"]),
        # UTC hour-of-day rides on the daily keys so the page's filters still apply.
        "runs_hourly": aggregate(hour_rows, keys + ["hour"], ["runs", "wins"]),
        "cards_daily": aggregate(card_rows, keys + ["card", "copies", "arch"],
                                 ["runs", "wins"]),
        "choices_daily": aggregate(choice_rows, keys + ["card"], ["offered", "picked"]),
        "deaths_daily": aggregate(death_rows, keys + ["enc"], ["deaths"]),
        "death_floors_daily": aggregate(floor_rows, keys + ["floor"], ["deaths"]),
        "encounters_daily": aggregate(enc_rows, keys + ["enc"], ["fights", "dmg", "turns"]),
        "archetypes_daily": aggregate(arch_rows, keys + ["arch"], ["runs", "wins"]),
    }


def build_cards_meta(ch: dict) -> dict[str, dict]:
    """Card entry -> {rarity, mechanics[], own} for every entry the dashboard may see. The
    character's own entries appear under their uploaded prefixed id (THEWITCH-…). `own` =
    "belongs to this mod character"; `witch` is the same flag under its historical name so
    an older page build keeps working against a fresh export."""
    prefix = ch["prefix"]
    mechanics = common.card_mechanics(ch["key"])
    meta: dict[str, dict] = {}
    for entry, rarity in common.card_rarities(ch["key"]).items():
        if entry.startswith(prefix):
            bare = entry.removeprefix(prefix)
            meta[entry] = {"rarity": rarity, "own": True, "witch": True,
                           "mechanics": sorted(mechanics.get(bare, []))}
        elif entry not in mechanics:  # bare duplicates of the mod's entries stay out
            meta[entry] = {"rarity": rarity, "own": False, "witch": False}
    return meta


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    common.add_common_args(parser)
    parser.add_argument("--include-seed", action="store_true",
                        help="keep fabricated mod_version='seed-test' rows (local testing)")
    parser.add_argument("--character", default=None,
                        help="only runs uploaded by this mod id (TheWitch / TheAugur); needs the "
                             "character column (migrations/001_character_column.sql). Also "
                             "defaults --out-dir to pages/analytics-data/<witch|augur>")
    parser.add_argument("--out-dir", default=None,
                        help="where to write the JSON tables (default: pages/analytics-data, or "
                             "pages/analytics-data/<key> when --character is given)")
    args = parser.parse_args()
    if not args.key:
        print(common.missing_key_message(), file=sys.stderr)
        return 1

    ch = common.character_config(args.character)
    if args.out_dir:
        out_dir = Path(args.out_dir)
    elif args.character:
        out_dir = OUT_DIR / ch["key"]
    else:
        out_dir = OUT_DIR  # back-compat: the plain export stays the Witch export in the root folder

    runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back,
                             character=ch["id"] if args.character else None)
    if not args.include_seed and not runs.empty:
        runs = runs[runs["mod_version"] != "seed-test"]
    if runs.empty:
        # Not an error: an unshipped character (the Augur until release) or a quiet window has
        # no rows. Exit clean so the per-character workflow steps don't fail the whole job.
        print(f"No runs to export for {ch['id']} (after seed filter) — leaving existing data "
              "untouched.", file=sys.stderr)
        return 0

    tables = build_tables(runs, ch)
    days = sorted({r["day"] for r in tables["runs_daily"]})
    outputs = {f"{name}.json": rows for name, rows in tables.items()}
    outputs["meta.json"] = {
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "character": ch["id"],
        "total_runs": int(len(runs)),
        "mod_versions": sorted(runs["mod_version"].unique()),
        "game_versions": sorted(runs["game_version"].unique()),
        "first_day": days[0],
        "last_day": days[-1],
    }
    outputs["cards_meta.json"] = build_cards_meta(ch)
    # Base-game encounter whitelist (generated from gamedata/ — see base_encounters.json);
    # the page hides other mods' encounters unless "include other mods" is on.
    outputs["encounters_meta.json"] = json.loads(
        (Path(__file__).with_name("base_encounters.json")).read_text(encoding="utf-8"))

    out_dir.mkdir(parents=True, exist_ok=True)
    for name, payload in outputs.items():
        path = out_dir / name
        path.write_text(json.dumps(payload, separators=(",", ":"), sort_keys=True) + "\n",
                        encoding="utf-8")
        print(f"Wrote {path} ({len(payload)} rows)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
