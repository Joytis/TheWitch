"""Export the aggregate tables behind the balance dashboard (pages/analytics.html).

Every table is additive COUNTS keyed by the page's filter dimensions, so the page recomposes any
rate by summing. No raw rows, decks, or player hashes leave this script. Run by
.github/workflows/analytics.yml (SUPABASE_READ_KEY secret) and locally via
./tools/analytics/analytics.ps1 -Mode export.

Filter dimensions on every balance table (the page's filter bar):
  ver    mod release (v0.13.1); the page groups patches into release lines (v0.13)
  ab     ascension band index into ASC_BANDS
  mp     0 solo / 1 co-op
  world  0 = the run touched another mod's acts or encounters, 1 = base-game world only

Skill adjustment. Win rate swings with who is playing far more than with any card: the game's own
lifetime win rate per player (`totalWinRate` in the uploaded RunMetrics) runs from ~11% to ~75%
win rate across its deciles. Every run gets an expected win probability `p` from a logistic model
fitted here (ascension band, player skill, experience, party size, modded world), and every
table carries `xw` = sum(p) and `xv` = sum(p(1-p)) next to `wins`. The page then shows
"wins above expected" = (wins - xw) / runs with a 95% interval of +-1.96*sqrt(xv) / runs — the
variance of the win count if the slice were exactly as good as expected. All three stay additive.
`totalWinRate` already includes the run being uploaded, so the run's own result is backed out
using an estimated lifetime run count (total playtime / RUN_SECONDS) before it is used.

Card strength uses the draft, not the final deck: a card's "taken vs passed" lift compares the
runs that took it at a reward screen with the runs that were offered it and passed, both against
expectation. Final-deck win rates are survivorship-biased (runs that live longer hold more cards)
and are exported for reference only.

Per-character: `--character TheWitch|TheAugur` filters the runs by mod id and defaults the output
folder to pages/analytics-data/<key>; without it the export is the Witch into
pages/analytics-data/ (the page reads that folder for the Witch).
"""

import argparse
import json
import math
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import pandas as pd

import common

OUT_DIR = common.REPO / "pages" / "analytics-data"
BASE_NAMES = json.loads(Path(__file__).with_name("base_names.json").read_text(encoding="utf-8"))

# Files written by the pre-2026-11 exporter; removed from the output folder on export.
LEGACY_FILES = ["hours", "runs_daily", "runs_hourly", "cards_daily", "cards_meta", "choices_daily",
                "deaths_daily", "death_floors_daily", "encounters_daily", "archetypes_daily",
                "encounters_meta"]

ASC_BANDS = [("A0", 0, 0), ("A1–4", 1, 4), ("A5–9", 5, 9), ("A10+", 10, 99)]
# Player skill = lifetime win rate before this run (all characters, from the game's metrics).
SKILL_BANDS = [("Under 15%", 0.0, 0.15), ("15–30%", 0.15, 0.30), ("30–50%", 0.30, 0.50),
               ("50%+", 0.50, 1.01)]
RUN_SECONDS = 3000   # median lifetime-playtime per lifetime run, measured from consecutive uploads
MAX_FLOOR = 60       # deaths deeper than this are modded acts; folded into the last bucket
# Per-card / per-relic / per-encounter detail tables cover only the newest N release lines
# (balance reads are about current content; older lines only feed the history charts).
DETAIL_RELEASES = 3

# Unprefixed ids that are NOT base game (mods that skip BaseLib's id prefixing).
UNPREFIXED_MOD_PREFIXES = ("SLUGCAT_",)

# Dominant-archetype rules: a run commits to the mechanic with the most of the character's own
# cards in deck (duplicates count); fewer than COMMIT_THRESHOLD such cards = "Unfocused". Ties
# break in the character's fixed mechanics order (common.CHARACTERS[..]["mechanics"]).
COMMIT_THRESHOLD = 3


# ---------------------------------------------------------------- helpers

def semver(v: str) -> tuple:
    m = re.match(r"v?(\d+)\.(\d+)\.(\d+)", v or "")
    return tuple(int(x) for x in m.groups()) if m else (-1, -1, -1)


def release_of(v: str) -> str:
    major, minor, _ = semver(v)
    return f"v{major}.{minor}"


def band(value: float, bands) -> int:
    for i, (_, lo, hi) in enumerate(bands):
        if lo <= value <= hi if isinstance(lo, int) else lo <= value < hi:
            return i
    return len(bands) - 1


def snake(name: str) -> str:
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_", name).upper()


def is_base(model_id: str, known: dict | set) -> bool:
    """Base-game content: in the decompile's id list, or an unprefixed id (BaseLib prefixes mod
    ids with MODID-) that is not a known unprefixed mod. The second clause keeps base content added
    after the last gamedata refresh (e.g. BATTLEWORN_DUMMY_EVENT_V2_ENCOUNTER) out of 'other mods'."""
    return model_id in known or ("-" not in model_id
                                 and not model_id.startswith(UNPREFIXED_MOD_PREFIXES))


def dominant_archetype(deck: list[str], mechanics: dict[str, set[str]], ch: dict) -> str:
    prefix, order = ch["prefix"], ch["mechanics"]
    counts = Counter()
    for card in deck:
        if card.startswith(prefix):
            for mech in mechanics.get(card.removeprefix(prefix), []):
                counts[mech] += 1
    best = max(order, key=lambda m: (counts.get(m, 0), -order.index(m)), default="Unfocused")
    return best if counts.get(best, 0) >= COMMIT_THRESHOLD else "Unfocused"


def aggregate(rows: list[dict], keys: list[str], sums: list[str]) -> dict:
    """Group-sum into a COLUMNAR table {"cols": [...], "rows": [[...], ...]} — a third of the size
    of a list of row objects (the page expands it back on load)."""
    if not rows:
        return {"cols": keys + sums, "rows": []}
    df = pd.DataFrame(rows).groupby(keys)[sums].sum().reset_index()
    for col in ("xw", "xv", "pxw", "pxv", "sxw", "sxv"):   # 3 decimals: plenty, keeps files small
        if col in df:
            df[col] = df[col].round(3)
    return columnar(df.to_dict("records"), keys + sums)


def columnar(records: list[dict], cols: list[str]) -> dict:
    def clean(v):
        if isinstance(v, float) and v.is_integer():
            return int(v)
        return v.item() if hasattr(v, "item") else v
    return {"cols": cols, "rows": [[clean(r[c]) for c in cols] for r in records]}


# ---------------------------------------------------------------- skill model

def pre_run_skill(data: dict, win: int) -> float:
    """Lifetime win rate BEFORE this run. The uploaded value already counts this run."""
    twr = float(data.get("totalWinRate") or 0.0)
    games = max(2.0, float(data.get("totalPlaytime") or 0.0) / RUN_SECONDS)
    return min(1.0, max(0.0, (twr * games - win) / (games - 1)))


FEATURES = ["intercept", "asc 1–4", "asc 5–9", "asc 10+", "skill (logit)", "experience (log h)",
            "2 players", "3+ players", "modded world"]


def features(asc_band: int, skill: float, hours: float, players: int, modded: int) -> list[float]:
    s = min(0.9, max(0.03, skill))
    return [1.0, asc_band == 1, asc_band == 2, asc_band == 3, math.log(s / (1 - s)),
            math.log1p(hours) / 5, players == 2, players >= 3, modded]


def fit_logistic(X: np.ndarray, y: np.ndarray, l2: float = 1.0, iters: int = 50) -> np.ndarray:
    """Ridge-penalised logistic regression by Newton/IRLS (intercept unpenalised)."""
    w = np.zeros(X.shape[1])
    pen = np.full(X.shape[1], l2)
    pen[0] = 0.0
    for _ in range(iters):
        p = 1 / (1 + np.exp(-(X @ w)))
        grad = X.T @ (p - y) + pen * w
        hess = (X * (p * (1 - p))[:, None]).T @ X + np.diag(pen)
        step = np.linalg.solve(hess, grad)
        w -= step
        if np.abs(step).max() < 1e-8:
            break
    return w


# ---------------------------------------------------------------- content metadata

def own_relics(ch: dict) -> dict[str, dict]:
    """Mod relic id -> {name, rarity} from the relic classes + eng loc."""
    loc_file = common.REPO / ch["loc_dir"] / "relics.json"
    loc = json.loads(loc_file.read_text(encoding="utf-8")) if loc_file.exists() else {}
    out = {}
    for f in sorted((common.REPO / ch["code_dir"] / "Relics").glob("*.cs")):
        m = re.search(r"Rarity\s*=>\s*RelicRarity\.(\w+)", f.read_text(encoding="utf-8"))
        if not m:
            continue   # abstract base / helper
        rid = ch["prefix"] + snake(f.stem)
        out[rid] = {"name": loc.get(f"{rid}.title", f.stem), "rarity": m.group(1), "own": True}
    return out


def card_catalog(ch: dict) -> dict[str, dict]:
    """Card id (as uploaded) -> {name, rarity, own, mech[], type} for own + base cards."""
    out: dict[str, dict] = {}
    for name in common.BASE_CLASS_FILES:
        path = common.REPO / f"Docs/card-data/{name}.json"
        if path.exists():
            for c in common._load_cards(path):
                if c.get("entry"):
                    out.setdefault(c["entry"], {"name": c.get("name") or c["entry"],
                                                "rarity": c.get("rarity", "Other"), "own": False,
                                                "type": c.get("type", "")})
    for entry, title in BASE_NAMES["cards"].items():
        out.setdefault(entry, {"name": title, "rarity": "Other", "own": False, "type": ""})
    for c in common._load_cards(common.REPO / ch["data_file"]):
        if c.get("entry"):
            out[ch["prefix"] + c["entry"]] = {
                "name": c.get("name") or c["entry"], "rarity": c.get("rarity", "Other"),
                "own": True, "type": c.get("type", ""), "mech": sorted(c.get("mechanics", [])),
                "cost": c.get("cost")}
    return out


def parse_patch_notes(ch: dict, cards: dict, relics: dict) -> list[dict]:
    """Patch-note bullets that name a card/relic/potion, for the "What changed" view."""
    if not ch.get("patch_notes"):
        return []
    by_name: dict[str, tuple[str, str]] = {}
    for cid, c in cards.items():
        if c["own"]:
            by_name[c["name"].lower()] = ("card", cid)
    for rid, r in relics.items():
        if r.get("own"):
            by_name[r["name"].lower()] = ("relic", rid)
    loc_file = common.REPO / ch["loc_dir"] / "potions.json"
    if loc_file.exists():
        for k, v in json.loads(loc_file.read_text(encoding="utf-8")).items():
            if k.endswith(".title"):
                by_name.setdefault(v.lower(), ("potion", k.removesuffix(".title")))
    out = []
    for path in sorted((common.REPO / ch["patch_notes"]).glob("v*.md"), key=lambda p: semver(p.stem)):
        section = ""
        for line in path.read_text(encoding="utf-8").splitlines():
            if line.startswith("## "):
                section = line[3:].strip()
            elif line.lstrip().startswith("- ") and section.lower() != "new art":
                names = re.findall(r"\*\*(.+?)\*\*", line)
                refs = [{"kind": by_name[n.lower()][0], "id": by_name[n.lower()][1]}
                        for n in names if n.lower() in by_name]
                if refs:
                    text = re.sub(r"\*\*(.+?)\*\*", r"\1", line.lstrip()[2:]).strip()
                    out.append({"ver": path.stem, "section": section, "text": text, "refs": refs})
    return out


# ---------------------------------------------------------------- tables

def build(runs: pd.DataFrame, ch: dict, detail_releases: int = DETAIL_RELEASES) -> dict:
    prefix = ch["prefix"]
    mechanics = common.card_mechanics(ch["key"])
    cards = card_catalog(ch)
    relics = {rid: {"name": name, "rarity": BASE_NAMES["relic_rarity"].get(rid, "Other"),
                    "own": False} for rid, name in BASE_NAMES["relics"].items()}
    relics.update(own_relics(ch))
    base_enc = BASE_NAMES["encounters"]
    base_acts = set(BASE_NAMES["act_order"])

    def own(mid: str) -> bool:
        return mid.startswith(prefix)

    def keep_card(cid: str) -> bool:
        return own(cid) or is_base(cid, cards)

    def keep_relic(rid: str) -> bool:
        return own(rid) or is_base(rid, relics)

    # ---- pass 1: per-run derived fields + model features
    recs = []
    first_seen: dict[str, str] = {}
    for run in runs.sort_values("created_at").itertuples():
        data = run.data or {}
        win = int(bool(run.victory))
        created = pd.to_datetime(run.created_at)
        acts = [a.get("act", "") for a in data.get("actWins", [])]
        encs = data.get("encounters", [])
        modded = int(any(a not in base_acts for a in acts)
                     or any(not (own(e["id"]) or is_base(e["id"], base_enc)) for e in encs))
        players = int(data.get("numPlayers") or 1)
        asc = int(run.ascension or 0)
        ab = band(asc, ASC_BANDS)
        skill = pre_run_skill(data, win)
        hours = float(data.get("totalPlaytime") or 0) / 3600
        player = run.player_hash or ""
        new = player not in first_seen
        first_seen.setdefault(player, run.mod_version)
        recs.append({
            "run": run, "data": data, "win": win, "created": created,
            "ver": run.mod_version, "ab": ab, "mp": int(players > 1), "world": 1 - modded,
            "sk": band(skill, SKILL_BANDS), "player": player, "new": int(new),
            "x": features(ab, skill, hours, players, modded),
            "cleared": sum(1 for a in data.get("actWins", []) if a.get("win")),
            "arch": dominant_archetype(data.get("deck", []), mechanics, ch),
        })

    X = np.array([r["x"] for r in recs], dtype=float)
    y = np.array([r["win"] for r in recs], dtype=float)
    w = fit_logistic(X, y)
    p_all = 1 / (1 + np.exp(-(X @ w)))
    brier = float(np.mean((p_all - y) ** 2))
    base_brier = float(np.mean((y.mean() - y) ** 2))

    # ---- pass 2: rows
    all_releases = sorted({release_of(r["ver"]) for r in recs}, key=lambda r: semver(r + ".0"))
    detailed = set(all_releases[-detail_releases:])
    T = defaultdict(list)
    newest = max(r["created"] for r in recs)
    for rec, p in zip(recs, p_all):
        run, data, win = rec["run"], rec["data"], rec["win"]
        K = {"ver": rec["ver"], "ab": rec["ab"], "mp": rec["mp"], "world": rec["world"]}
        out = {"runs": 1, "wins": win, "xw": p, "xv": p * (1 - p)}
        cl = rec["cleared"]
        acts = {"a1": int(cl >= 1), "a2": int(cl >= 2), "a3": int(cl >= 3)}
        reload = int((data.get("numReloads") or 0) > 0)

        T["runs"].append(K | {"sk": rec["sk"]} | out | acts | {
            "rl": reload, "new": rec["new"], "pt": int(run.playtime or 0)})
        T["days"].append({"day": rec["created"].strftime("%Y-%m-%d"), "ver": rec["ver"],
                          "mp": rec["mp"], "world": rec["world"], "runs": 1, "wins": win})
        T["arch"].append(K | {"arch": rec["arch"]} | out | acts)
        killed = data.get("killedByEncounter")
        if not win and killed and killed != "NONE":
            T["floors"].append(K | {"floor": min(int(run.floor or 0), MAX_FLOOR), "deaths": 1})
        if release_of(rec["ver"]) not in detailed:
            continue

        deck = Counter(c for c in data.get("deck", []) if keep_card(c)
                       and cards.get(c, {}).get("rarity") not in ("Starter", "Basic"))
        upgrades = Counter(data.get("campfireUpgrades", []))
        for cid, copies in deck.items():
            T["deck"].append(K | {"card": cid} | out | {"cp": copies, "up": upgrades.get(cid, 0)})
            if own(cid):
                T["deck_arch"].append({"ver": rec["ver"], "mp": rec["mp"], "world": rec["world"],
                                       "card": cid, "arch": rec["arch"]} | out)
        for cid, n in upgrades.items():    # upgrades of starter cards (Strike/Defend/...) too
            if keep_card(cid) and cid not in deck:
                T["deck"].append(K | {"card": cid, "runs": 0, "wins": 0, "xw": 0, "xv": 0,
                                      "cp": 0, "up": n})

        for screen in data.get("cardChoices", []):
            for cid in screen.get("picked", []):
                if keep_card(cid):
                    T["offers"].append(K | {"card": cid, "off": 1, "pk": 1, "pw": win, "pxw": p,
                                            "pxv": p * (1 - p), "sw": 0, "sxw": 0, "sxv": 0})
            for cid in screen.get("skipped", []):
                if keep_card(cid):
                    T["offers"].append(K | {"card": cid, "off": 1, "pk": 0, "pw": 0, "pxw": 0,
                                            "pxv": 0, "sw": win, "sxw": p, "sxv": p * (1 - p)})

        buys = Counter(data.get("relicBuys", []))
        for rid in set(data.get("relics", [])):
            if keep_relic(rid):
                T["relics"].append(K | {"relic": rid} | out | {"buy": buys.get(rid, 0)})
        for choice in data.get("ancientChoices", []):
            options = [(choice.get("picked"), 1)] + [(s, 0) for s in choice.get("skipped", [])]
            for rid, took in options:
                if rid and keep_relic(rid):
                    T["ancients"].append(K | {"relic": rid, "off": 1, "pk": took,
                                              "pw": win * took, "pxw": p * took,
                                              "pxv": p * (1 - p) * took,
                                              "sw": win * (1 - took), "sxw": p * (1 - took),
                                              "sxv": p * (1 - p) * (1 - took)})

        for enc in data.get("encounters", []):
            eid = enc["id"] if (own(enc["id"]) or is_base(enc["id"], base_enc)) else "OTHER_MOD"
            T["encounters"].append(K | {"enc": eid, "fights": 1,
                                        "dmg": int(enc.get("damage", 0)),
                                        "turns": int(enc.get("turns", 0)), "deaths": 0})
        if not win and killed and killed != "NONE":
            eid = killed if (own(killed) or is_base(killed, base_enc)) else "OTHER_MOD"
            T["encounters"].append(K | {"enc": eid, "fights": 0, "dmg": 0, "turns": 0,
                                        "deaths": 1})

    k = ["ver", "ab", "mp", "world"]
    rate = ["runs", "wins", "xw", "xv"]
    actc = ["a1", "a2", "a3"]
    pick = ["off", "pk", "pw", "pxw", "pxv", "sw", "sxw", "sxv"]
    tables = {
        "runs": aggregate(T["runs"], k + ["sk"], rate + actc + ["rl", "new", "pt"]),
        "days": aggregate(T["days"], ["day", "ver", "mp", "world"], ["runs", "wins"]),
        "arch": aggregate(T["arch"], k + ["arch"], rate + actc),
        "deck": aggregate(T["deck"], k + ["card"], rate + ["cp", "up"]),
        "deck_arch": aggregate(T["deck_arch"], ["ver", "mp", "world", "card", "arch"], rate),
        "offers": aggregate(T["offers"], k + ["card"], pick),
        "relics": aggregate(T["relics"], k + ["relic"], rate + ["buy"]),
        "ancients": aggregate(T["ancients"], k + ["relic"], pick),
        "encounters": aggregate(T["encounters"], k + ["enc"], ["fights", "dmg", "turns", "deaths"]),
        "floors": aggregate(T["floors"], k + ["floor"], ["deaths"]),
    }

    # Distinct players are not additive, so they are counted per release line and per version.
    players = []
    for scope, keyfn in (("rel", lambda r: release_of(r["ver"])), ("ver", lambda r: r["ver"])):
        groups = defaultdict(lambda: defaultdict(int))
        for rec in recs:
            groups[(keyfn(rec), rec["mp"])][rec["player"]] += 1
        for (gid, mp), per in sorted(groups.items()):
            players.append({"scope": scope, "id": gid, "mp": mp, "players": len(per),
                            "repeat": sum(1 for n in per.values() if n >= 2)})
    tables["players"] = columnar(players, ["scope", "id", "mp", "players", "repeat"])

    # ---- names actually referenced (keeps meta small)
    def col(table: str, name: str) -> set:
        t = tables[table]
        i = t["cols"].index(name)
        return {row[i] for row in t["rows"]}
    seen_cards = col("deck", "card") | col("offers", "card")
    seen_relics = col("relics", "relic") | col("ancients", "relic")
    seen_enc = col("encounters", "enc")

    def pretty(mid: str) -> str:
        return mid.split("-", 1)[-1].replace("_", " ").title()

    card_meta = {c: cards.get(c, {"name": pretty(c), "rarity": "Other", "own": own(c)})
                 for c in sorted(seen_cards)}
    relic_meta = {r: relics.get(r, {"name": pretty(r), "rarity": "Other", "own": own(r)})
                  for r in sorted(seen_relics)}
    enc_meta = {}
    for e in sorted(seen_enc):
        tier = next((t for t in ("BOSS", "ELITE", "NORMAL", "WEAK") if e.endswith(t)), "EVENT")
        act = BASE_NAMES["encounter_act"].get(e)
        enc_meta[e] = {"name": base_enc.get(e) or ("Other mods' encounters" if e == "OTHER_MOD"
                                                   else pretty(e)),
                       "tier": tier.title(), "act": BASE_NAMES["act_order"].get(act)}

    versions = sorted({r["ver"] for r in recs}, key=semver)
    days_by_ver = defaultdict(list)
    for rec in recs:
        days_by_ver[rec["ver"]].append(rec["created"].strftime("%Y-%m-%d"))
    releases = []
    for rel in sorted({release_of(v) for v in versions}, key=lambda r: semver(r + ".0")):
        vs = [v for v in versions if release_of(v) == rel]
        ds = [d for v in vs for d in days_by_ver[v]]
        releases.append({"id": rel, "versions": vs, "first_day": min(ds), "last_day": max(ds),
                         "runs": len(ds)})

    meta = {
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "newest_run": newest.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "character": ch["id"], "total_runs": len(recs),
        "versions": versions, "releases": releases,
        "detail_releases": sorted(detailed, key=lambda r: semver(r + ".0")),
        "first_day": min(r["created"] for r in recs).strftime("%Y-%m-%d"),
        "last_day": newest.strftime("%Y-%m-%d"),
        "version_first_day": {v: min(days_by_ver[v]) for v in versions},
        "asc_bands": [b[0] for b in ASC_BANDS], "skill_bands": [b[0] for b in SKILL_BANDS],
        "archetypes": ch["mechanics"] + ["Unfocused"],
        "model": {"features": FEATURES, "coef": [round(float(c), 4) for c in w],
                  "runs": len(recs), "brier": round(brier, 4),
                  "brier_baseline": round(base_brier, 4)},
        "cards": card_meta, "relics": relic_meta, "encounters": enc_meta,
        "acts": {str(n): " / ".join(BASE_NAMES["acts"].get(a, a)
                                    for a, m in BASE_NAMES["act_order"].items() if m == n)
                 for n in sorted(set(BASE_NAMES["act_order"].values()))},
        "patches": parse_patch_notes(ch, cards, relics),
    }
    return {"meta": meta, **tables}


def run_game_mode(data) -> str:
    """STANDARD / BLITZ from the uploaded payload; rows from before Blitz support carry no
    gameMode (or the client's hard-coded STANDARD) and were all Standard runs."""
    if isinstance(data, str):
        try:
            data = json.loads(data)
        except ValueError:
            return "STANDARD"
    mode = data.get("gameMode") if isinstance(data, dict) else None
    return (mode or "STANDARD").upper()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    common.add_common_args(parser)
    parser.add_argument("--include-seed", action="store_true",
                        help="keep fabricated mod_version='seed-test' rows (local testing)")
    parser.add_argument("--character", default=None,
                        help="only runs uploaded by this mod id (TheWitch / TheAugur). Also "
                             "defaults --out-dir to pages/analytics-data/<witch|augur>")
    parser.add_argument("--out-dir", default=None,
                        help="where to write the JSON tables (default: pages/analytics-data, or "
                             "pages/analytics-data/<key> when --character is given)")
    parser.add_argument("--game-mode", default="STANDARD", type=str.upper,
                        help="only runs played in this game mode (STANDARD / BLITZ; default "
                             "STANDARD). Read from the payload's gameMode, so it works before "
                             "migrations/002_game_mode_column.sql; rows without it are STANDARD")
    parser.add_argument("--from-pickle", default=None,
                        help="read runs from a pickled list of row dicts instead of Supabase "
                             "(local iteration; never committed)")
    args = parser.parse_args()

    ch = common.character_config(args.character)
    if args.out_dir:
        out_dir = Path(args.out_dir)
    elif args.character:
        out_dir = OUT_DIR / ch["key"]
    else:
        out_dir = OUT_DIR

    if args.from_pickle:
        import pickle
        with open(args.from_pickle, "rb") as fh:
            runs = pd.DataFrame(pickle.load(fh))
        if args.character and "character" in runs:
            runs = runs[runs["character"] == ch["id"]]
    else:
        if not args.key:
            print(common.missing_key_message(), file=sys.stderr)
            return 1
        runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back,
                                 character=ch["id"] if args.character else None)
    if not runs.empty:
        if not args.include_seed:
            runs = runs[runs["mod_version"] != "seed-test"]
        runs = runs[runs["mod_version"].map(lambda v: semver(v)[0] >= 0)]
        runs = runs[runs["data"].map(run_game_mode) == args.game_mode]
    if runs.empty:
        # Not an error: an unshipped character (the Augur until release) or a quiet window has no
        # rows. Exit clean so the per-character workflow steps don't fail the whole job.
        print(f"No runs to export for {ch['id']} — leaving existing data untouched.",
              file=sys.stderr)
        return 0

    tables = build(runs, ch)
    out_dir.mkdir(parents=True, exist_ok=True)
    for name in LEGACY_FILES:
        (out_dir / f"{name}.json").unlink(missing_ok=True)
    for name, payload in tables.items():
        path = out_dir / f"{name}.json"
        path.write_text(json.dumps(payload, separators=(",", ":"), sort_keys=True,
                                   ensure_ascii=False) + "\n", encoding="utf-8")
        size = path.stat().st_size
        rows = len(payload["rows"]) if "rows" in payload else len(payload)
        print(f"Wrote {path.name} ({rows} rows, {size / 1024:.0f} KB)")
    m = tables["meta"]["model"]
    print(f"Skill model: brier {m['brier']} vs {m['brier_baseline']} baseline; "
          + ", ".join(f"{f} {c:+.2f}" for f, c in zip(m["features"], m["coef"])))
    return 0


if __name__ == "__main__":
    sys.exit(main())
