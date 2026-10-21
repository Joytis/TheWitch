"""Keep every configured language in step with the English source (the /loc-sync skill's engine).

    python tools/localization/loc_sync.py status   [--character Witch|Augur]
    python tools/localization/loc_sync.py pending  [langs...] [--audit] [--out work.json]
    python tools/localization/loc_sync.py apply    results.json [--force]
    python tools/localization/loc_sync.py check    [langs...]
    python tools/localization/loc_sync.py glossary <lang> <English term>...

Translations are written by Claude Code in-session (no MT API). This script only finds the
work, merges the results and validates them.

State: tools/localization/state/<ModId>/<lang>.json maps file -> key -> hash of the English
text the current translation was written (or verified) against. A key is pending when:
  missing     no translation in localization/<lang>/<file>
  changed     the English text changed since the translation was written/verified
  unverified  a translation exists but was never checked against this English (pre-pipeline
              human translations) - only listed with --audit
Translator edits that arrive through the nightly ParaTranz download keep their state entry
(English unchanged), so they are never flagged or overwritten.

Languages = the `languages` keys of .github/configs/paratranz.json.

apply input: {"<lang>": [{"file": "cards.json", "key": "...", "tr": "..."}, ...], ...}
`tr` = new text; `tr: null` = the existing translation was audited and is still correct
(records the hash, keeps the text). Entries that fail validation are skipped unless --force.
Files are written like para_download.py (English key order, indent 4, LF), and keys that no
longer exist in English are dropped.
"""
import argparse
import hashlib
import json
import re
import sys
from collections import Counter, OrderedDict
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
STATE_DIR = Path(__file__).resolve().parent / "state"
GAME_LOC = REPO_ROOT / "gamedata" / "localization"
# Mirrors tools/characters.ps1: character key -> (mod id, asset root).
CHARACTERS = {"Witch": ("TheWitch", "TheWitch/TheWitch"), "Augur": ("TheAugur", "TheAugur/TheAugur")}

sys.stdout.reconfigure(encoding="utf-8")


def languages():
    with open(REPO_ROOT / ".github" / "configs" / "paratranz.json", encoding="utf-8") as f:
        return list(json.load(f)["languages"])


def read_json(path):
    if not path.exists():
        return OrderedDict()
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=OrderedDict)


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(data, fh, ensure_ascii=False, indent=4)
        fh.write("\n")


# Keys whose value is read as data, not shown as text (BaseLib AncientDialogueUtil parses them:
# Enum.TryParse<ArchitectAttackers>, visit index, FMOD event). A translated value silently breaks
# the dialogue, so they are never translated: English fallback (or a verbatim copy) is correct.
CODE_KEY_RE = re.compile(r"-(?:attack|startattack|endattack|visit)$|\.sfx$")


def en_hash(text):
    return hashlib.sha1(text.encode("utf-8")).hexdigest()[:12]


class Mod:
    def __init__(self, character):
        self.mod_id, root = CHARACTERS[character]
        self.loc = REPO_ROOT / root / "localization"
        self.state_dir = STATE_DIR / self.mod_id
        self.state = {}  # lang -> file -> key -> hash, loaded per language (one file each, so
                         # parallel applies for different languages never clobber each other)

    def eng(self):
        return {p.name: read_json(p) for p in sorted((self.loc / "eng").glob("*.json"))}

    def tr(self, lang, filename):
        return read_json(self.loc / lang / filename)

    def lang_state(self, lang):
        if lang not in self.state:
            self.state[lang] = read_json(self.state_dir / f"{lang}.json")
        return self.state[lang]

    def save_state(self, lang):
        st = self.lang_state(lang)
        clean = OrderedDict((f, OrderedDict(sorted(k.items()))) for f, k in sorted(st.items()) if k)
        write_json(self.state_dir / f"{lang}.json", clean)

    def recorded(self, lang, filename, key):
        return self.lang_state(lang).get(filename, {}).get(key)

    def record(self, lang, filename, key, text):
        self.lang_state(lang).setdefault(filename, OrderedDict())[key] = en_hash(text)

    def classify(self, lang):
        """Yield (file, key, english, translation|None, reason|None) for every English key."""
        for filename, eng in self.eng().items():
            tr = self.tr(lang, filename)
            for key, en in eng.items():
                cur = tr.get(key)
                rec = self.recorded(lang, filename, key)
                if not en.strip() or CODE_KEY_RE.search(key):
                    reason = None  # empty English (e.g. a card with no description): nothing to translate
                elif not cur:
                    reason = "missing"
                elif rec is None:
                    reason = "unverified"
                elif rec != en_hash(en):
                    reason = "changed"
                else:
                    reason = None
                yield filename, key, en, cur, reason


# ---------------------------------------------------------------- validation

TAG_RE = re.compile(r"\[(/?)([A-Za-z_]+)(?:=[^\]]*)?\]")
SOFT_FORMATTERS = {"plural", "cond"}  # languages may legitimately rephrase around these


def placeholders(text):
    """(selector, formatter) for every SmartFormat placeholder, nested ones included.
    Returns (pairs, error)."""
    pairs, depth, i = [], 0, 0
    while i < len(text):
        c = text[i]
        if c == "\\" and i + 1 < len(text):
            i += 2
            continue
        if c == "{":
            depth += 1
            # {Sel:fmt(..)...} / {Sel:fmt:...} name a formatter; {Sel:yes|no} is an implicit conditional.
            m = re.match(r"([A-Za-z0-9_.]*)(?::([A-Za-z]+)(?=[(:]))?", text[i + 1:])
            pairs.append((m.group(1), m.group(2) or ""))
        elif c == "}":
            depth -= 1
            if depth < 0:
                return pairs, "unbalanced '}'"
        i += 1
    return pairs, ("unbalanced '{'" if depth else None)


# Word count {X:plural:a|b|...} needs per language. SmartFormat's rule for the game's culture
# (LocManager.CultureInfoFromThreeLetterCode -> PluralRules) returns an index into the words and
# THROWS when the index is past the end: Russian/Polish pick 0-2 (a 2-word plural crashes on 5),
# CJK/Thai always pick 0, the rest choose between 2 words (3+ words change meaning, e.g. zero|one|other).
PLURAL_WORDS = {"rus": (3,), "pol": (3,), "zhs": (1, 2), "jpn": (1, 2), "kor": (1, 2), "tha": (1, 2)}


def plural_word_counts(text):
    """Number of |-separated words in each {Sel:plural:...} placeholder (nested braces skipped)."""
    counts = []
    for m in re.finditer(r"\{[A-Za-z0-9_.]*:plural(?:\([^)]*\))?:", text):
        depth, words, i = 0, 1, m.end()
        while i < len(text):
            c = text[i]
            if c == "{":
                depth += 1
            elif c == "}":
                if depth == 0:
                    break
                depth -= 1
            elif c == "|" and depth == 0:
                words += 1
            i += 1
        counts.append(words)
    return counts


def validate(en, tr, lang=None):
    """(errors, warnings) for one translated string."""
    errors, warnings = [], []
    if not tr or not tr.strip():
        return ["empty translation"], []
    ep, _ = placeholders(en)
    tp, err = placeholders(tr)
    if err:
        errors.append(err)
    e_sel = {s for s, _ in ep if s}
    t_sel = {s for s, _ in tp if s}
    if e_sel != t_sel:
        errors.append(f"variables differ: missing {sorted(e_sel - t_sel)}, extra {sorted(t_sel - e_sel)}")
    # Every named formatter must survive ({Damage:diff()} keeps diff()). Extras are fine: naming the
    # selector of an anonymous {:diff()} inside a plural is equivalent.
    hard = lambda ps: {p for p in ps if p[0] and p[1] and p[1] not in SOFT_FORMATTERS}
    if hard(ep) - hard(tp):
        errors.append(f"formatters lost: {sorted(hard(ep) - hard(tp))}")
    allowed = PLURAL_WORDS.get(lang, (2,))
    bad_plurals = [n for n in plural_word_counts(tr) if n not in allowed]
    if lang and bad_plurals:
        errors.append(f"plural needs {'/'.join(map(str, allowed))} word(s) in {lang}, has {bad_plurals}")
    soft = lambda ps: Counter(p for p in ps if p[1] in SOFT_FORMATTERS)
    if soft(ep) != soft(tp):
        warnings.append("plural/cond usage differs")
    tags = lambda s: Counter(f"{m.group(1)}{m.group(2).lower()}" for m in TAG_RE.finditer(s))
    t = tags(tr)
    unbalanced = sorted(n for n in t if not n.startswith("/") and t[n] != t["/" + n])
    if unbalanced:
        errors.append(f"unbalanced markup: {unbalanced}")
    if tags(en) != t:
        warnings.append(f"markup differs: en {dict(tags(en))} vs {dict(t)}")
    if en.count("\n") != tr.count("\n"):
        warnings.append(f"line breaks {en.count(chr(10))} -> {tr.count(chr(10))}")
    return errors, warnings


# ---------------------------------------------------------------- commands

def cmd_status(mod, args):
    print(f"{mod.mod_id}: {sum(len(v) for v in mod.eng().values())} English strings")
    print(f"{'lang':6} {'ok':>5} {'missing':>8} {'changed':>8} {'unverified':>11}")
    for lang in languages():
        c = Counter(r or "ok" for *_, r in mod.classify(lang))
        print(f"{lang:6} {c['ok']:>5} {c['missing']:>8} {c['changed']:>8} {c['unverified']:>11}")


def cmd_pending(mod, args):
    langs = args.langs or languages()
    reasons = {"missing", "changed"} | ({"unverified"} if args.audit else set())
    work, total = OrderedDict(), 0
    for lang in langs:
        items = [{"file": f, "key": k, "reason": r, "en": en, **({"tr": cur} if cur else {})}
                 for f, k, en, cur, r in mod.classify(lang) if r in reasons]
        if items:
            work[lang] = items
            total += len(items)
    for lang, items in work.items():
        print(f"{lang}: {len(items)} ({dict(Counter(i['reason'] for i in items))})")
    if args.out:
        write_json(Path(args.out), work)
        print(f"{total} pending -> {args.out}")
    elif total:
        print(json.dumps(work, ensure_ascii=False, indent=2))
    else:
        print("Nothing pending.")


def cmd_apply(mod, args):
    results = read_json(Path(args.results))
    eng = mod.eng()
    bad = 0
    for lang, entries in results.items():
        if lang not in languages():
            sys.exit(f"'{lang}' is not a configured language")
        files = {}
        applied = verified = 0
        for e in entries:
            filename, key, text = e["file"], e["key"], e.get("tr")
            en = eng.get(filename, {}).get(key)
            if en is None:
                print(f"  {lang} skip (no English key): {filename} {key}")
                continue
            if CODE_KEY_RE.search(key) and text is not None and text != en:
                bad += 1
                print(f"  {lang} REJECTED {key}: data key, must stay {en!r}")
                continue
            tr = files.setdefault(filename, mod.tr(lang, filename))
            verify = text is None
            if verify:
                if not tr.get(key):
                    print(f"  {lang} skip (verified but no translation): {key}")
                    continue
                text = tr[key]
            errors, warnings = validate(en, text, lang)
            for w in warnings:
                print(f"  {lang} warn {key}: {w}")
            if errors and not args.force:
                bad += 1
                print(f"  {lang} REJECTED {key}: {'; '.join(errors)}")
                continue
            tr[key] = text
            mod.record(lang, filename, key, en)
            if verify:
                verified += 1
            else:
                applied += 1
        for filename, tr in files.items():
            ordered = OrderedDict((k, tr[k]) for k in eng[filename] if tr.get(k))
            dropped = [k for k in tr if k not in eng[filename]]
            if dropped:
                print(f"  {lang} {filename}: dropped {len(dropped)} key(s) no longer in English: {dropped}")
            write_json(mod.loc / lang / filename, ordered)
        mod.save_state(lang)
        print(f"{lang}: {applied} translated, {verified} verified")
    if bad:
        sys.exit(f"{bad} entr{'y' if bad == 1 else 'ies'} rejected - fix and re-apply")


def cmd_check(mod, args):
    errors = 0
    for lang in args.langs or languages():
        for filename, eng in mod.eng().items():
            tr = mod.tr(lang, filename)
            for key, text in tr.items():
                if key not in eng:
                    print(f"{lang} {filename} {key}: not in English")
                    continue
                if CODE_KEY_RE.search(key):
                    if text != eng[key]:
                        errors += 1
                        print(f"{lang} {key}: ERROR data key translated, must stay {eng[key]!r}")
                    continue
                errs, warns = validate(eng[key], text, lang)
                for e in errs:
                    errors += 1
                    print(f"{lang} {key}: ERROR {e}")
                if args.warnings:
                    for w in warns:
                        print(f"{lang} {key}: warn {w}")
    print(f"{errors} error(s)")
    sys.exit(1 if errors else 0)


def cmd_glossary(mod, args):
    """Official base-game translations of English terms (exact value match in gamedata/localization)."""
    if not GAME_LOC.is_dir():
        sys.exit(f"No base-game localization at {GAME_LOC}")
    terms = {t.lower() for t in args.terms}
    for en_file in sorted((GAME_LOC / "eng").glob("*.json")):
        try:
            eng = read_json(en_file)
        except json.JSONDecodeError:
            continue
        hits = [k for k, v in eng.items() if isinstance(v, str) and v.strip().lower() in terms]
        if not hits:
            continue
        tr = read_json(GAME_LOC / args.lang / en_file.name)
        for k in hits:
            print(f"{eng[k]!r:28} -> {tr.get(k)!r}   ({en_file.name} {k})")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--character", default="Witch", choices=list(CHARACTERS))
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("status")
    p = sub.add_parser("pending")
    p.add_argument("langs", nargs="*")
    p.add_argument("--audit", action="store_true", help="also list never-verified translations")
    p.add_argument("--out")
    p = sub.add_parser("apply")
    p.add_argument("results")
    p.add_argument("--force", action="store_true", help="write entries that fail validation")
    p = sub.add_parser("check")
    p.add_argument("langs", nargs="*")
    p.add_argument("--warnings", action="store_true")
    p = sub.add_parser("glossary")
    p.add_argument("lang")
    p.add_argument("terms", nargs="+")
    args = ap.parse_args()
    {"status": cmd_status, "pending": cmd_pending, "apply": cmd_apply,
     "check": cmd_check, "glossary": cmd_glossary}[args.cmd](Mod(args.character), args)


if __name__ == "__main__":
    main()
