"""Generate tools/analytics/base_names.json from the local gamedata/ decompile.

The analytics exporter runs in CI, where gamedata/ (proprietary, gitignored) does not exist, so
everything it needs to know about base-game content is distilled here and committed:

  cards / relics / potions / encounters / acts : model id -> English display name
  encounter_act                                : encounter id -> act id (from each act's
                                                 GenerateAllEncounters list)
  act_order                                    : act id -> act number (1-3)
  relic_rarity                                 : relic id -> RelicRarity (Starter/Common/
                                                 Uncommon/Rare/Shop/Event/Ancient/...)

Membership is what makes an id "base game" on the dashboard; everything else in an uploaded run
is another mod's content. Re-run after refreshing gamedata/ (new base content would otherwise
show up as "other mods" until the next refresh — the exporter also treats unprefixed ids as base,
see export_stats.is_base):

    py tools/analytics/build_base_names.py
"""

import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
LOC = REPO / "gamedata" / "localization" / "eng"
ACTS_SRC = REPO / "gamedata" / "src" / "Core" / "Models" / "Acts"
RELICS_SRC = REPO / "gamedata" / "src" / "Core" / "Models" / "Relics"
OUT = Path(__file__).with_name("base_names.json")

# Act number per base act. Overgrowth and Underdocks are the two Act 1 variants.
ACT_ORDER = {"OVERGROWTH": 1, "UNDERDOCKS": 1, "HIVE": 2, "GLORY": 3}


def snake(name: str) -> str:
    """C# class name -> model id entry (KaiserCrabBoss -> KAISER_CRAB_BOSS)."""
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_", name).upper()


def titles(file: str) -> dict[str, str]:
    data = json.loads((LOC / f"{file}.json").read_text(encoding="utf-8"))
    return {k.removesuffix(".title"): v for k, v in sorted(data.items()) if k.endswith(".title")}


def encounter_acts() -> dict[str, str]:
    out: dict[str, str] = {}
    for act_file in sorted(ACTS_SRC.glob("*.cs")):
        act = snake(act_file.stem)
        if act not in ACT_ORDER:
            continue
        src = act_file.read_text(encoding="utf-8")
        body = src.split("GenerateAllEncounters", 1)[-1]
        for cls in re.findall(r"ModelDb\.Encounter<(\w+)>", body):
            out.setdefault(snake(cls), act)
    return dict(sorted(out.items()))


def relic_rarities() -> dict[str, str]:
    out: dict[str, str] = {}
    for f in sorted(RELICS_SRC.glob("*.cs")):
        m = re.search(r"Rarity\s*=>\s*RelicRarity\.(\w+)", f.read_text(encoding="utf-8"))
        if m:
            out[snake(f.stem)] = m.group(1)
    return out


def main() -> int:
    if not LOC.exists():
        print(f"{LOC} not found — needs the local gamedata/ decompile.", file=sys.stderr)
        return 1
    payload = {
        "cards": titles("cards"),
        "relics": titles("relics"),
        "potions": titles("potions"),
        "encounters": titles("encounters"),
        "acts": {k: v for k, v in titles("acts").items() if k in ACT_ORDER},
        "encounter_act": encounter_acts(),
        "act_order": ACT_ORDER,
        "relic_rarity": relic_rarities(),
    }
    OUT.write_text(json.dumps(payload, indent=1, ensure_ascii=False, sort_keys=True) + "\n",
                   encoding="utf-8")
    print(f"Wrote {OUT} — " + ", ".join(f"{k} {len(v)}" for k, v in payload.items()))
    return 0


if __name__ == "__main__":
    sys.exit(main())
