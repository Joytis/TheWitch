---
name: loc-sync
description: Propagate English localization changes to every configured language (zhs deu fra ita jpn kor ptb rus spa) by translating in-session, then validate. Use when the user says "/loc-sync", "translate", "update translations", "propagate loc", and ALWAYS as the last step of any change that edits a localization/eng/*.json file (new/changed card, relic, power, potion, UI text).
---

# Loc sync (in-session translation)

Every English change ships translated in all configured languages, done inline with the dev work. Claude is the translator; `tools/localization/loc_sync.py` finds the work, merges results, and validates. Languages = `languages` keys in `.github/configs/paratranz.json`. Default character is the Witch; pass `--character Augur` for the Augur.

## Steps

1. `python tools/localization/loc_sync.py status` - per language: ok / missing / changed / unverified.
2. `python tools/localization/loc_sync.py pending --out <scratchpad>/loc-pending.json` - work list per language: `file`, `key`, `reason`, `en`, and `tr` (current translation, if any). Add `--audit` to include `unverified` strings (translations never checked against the current English, e.g. pre-pipeline human work). Pass language codes to limit it.
3. Translate. Write `<scratchpad>/loc-results.json`:
   ```json
   {"rus": [{"file": "cards.json", "key": "THEWITCH-SMOLDER.description", "tr": "..."},
            {"file": "cards.json", "key": "THEWITCH-SMOLDER.title", "tr": null}]}
   ```
   `tr: null` = the existing translation was checked and still matches the English (records it as verified, text untouched).
   - `missing` -> translate.
   - `changed` / `unverified` -> compare `tr` against `en`. If meaning, numbers, variables, keywords and card/power names all still match, `null`. Otherwise fix it with the smallest edit that makes it correct, keeping the human translator's phrasing and terminology. The user's policy: when English changes, the stale translation IS overwritten (also on ParaTranz).
   - Big batches (many strings x many languages): one `general-purpose` subagent per language, each running pending -> translate -> apply -> check for its language only (state files are per language, so parallel applies are safe). Give each the rules below verbatim.
4. `python tools/localization/loc_sync.py apply <scratchpad>/loc-results.json` - writes `<mod>/localization/<lang>/*.json` (English key order, drops keys no longer in English) and records state. Entries failing validation are rejected; fix and re-apply (never `--force` to dodge a real token error).
5. `python tools/localization/loc_sync.py check` - exit 0 required. `--warnings` lists soft diffs (markup counts, line breaks, plural/cond usage) worth a look.
6. Report: counts per language, anything rejected/odd. Do not commit (the user commits). Remind that pushing to `main` runs `loc-upload-source`, which pushes the changed strings to ParaTranz - until pushed, the nightly download can conflict with local edits.

## Translation rules

- **Tokens survive verbatim**: `{Damage:diff()}`, `{Energy:energyIcons()}`, `{IfUpgraded:show:A|B}`, `{Var:cond:>0?...|}`, `{OnPlayer:A|B}` - selector names, formatters and argument syntax unchanged; only the human text inside branches is translated. Every numeric var keeps `:diff()`.
- **Plurals** - word count is enforced by `check` because SmartFormat THROWS on a wrong count: rus (and pol) need exactly three words `one|few|many` (1,21 / 2-4 / 0,5-20, e.g. `{Cards:plural:карту|карты|карт}`) - a two-word Russian plural crashes at 5; zhs/jpn/kor always use the first word (one or two words, e.g. `{Cards:plural:张牌|张牌}`); every other language exactly two (`singular|plural`; fra uses the singular for 0 and 1).
- **Markup**: `[gold]...[/gold]`, `[blue]...[/blue]`, `[green]` etc. wrap the same concept as in English; keep counts equal unless the grammar truly forces otherwise. Keep `\n` line structure (one effect per line).
- **Base-game terms use the official translation**: `python tools/localization/loc_sync.py glossary <lang> Block Vulnerable Exhaust Strike ...` looks them up in `gamedata/localization/` (keywords, powers, card names, intents). For a base name embedded in a longer card name (Strike, Defend, ...) keep the official translation of that base name inside the title (`Docs/paratranz-description.md` rule 4). Match base-game description phrasing ("Gain X Block", "Apply X Vulnerable") by finding a similar base-game string in `gamedata/localization/<lang>/cards.json`.
- **Mod terms stay consistent per language**: a card/power/keyword name referenced inside another string must match that item's translated `title` in the same language - grep `<mod>/localization/<lang>/` before inventing a term. Translate all names (titles, familiars, potions); never leave English in a translation.
- **Data keys are never translated**: `...-attack` / `-startattack` / `-endattack` / `-visit` / `.sfx` in `ancients.json` are parsed by BaseLib (`ArchitectAttackers` enum, visit index, FMOD event). `loc_sync.py` skips them and rejects/flags any value other than the exact English.
- `spa` is Spain Spanish (es-ES), `ptb` Brazilian Portuguese, `zhs` Simplified Chinese.
- Flavor text (`.flavor`, ancients dialogue, event prose) reads natural in the target language, not literal.

## How it fits the ParaTranz pipeline

- State: `tools/localization/state/<ModId>/<lang>.json` = key -> hash of the English each translation was written/verified against. Commit it with the translations.
- Translator edits arriving via the nightly `loc-download` keep their state (English unchanged), so they are never re-flagged or overwritten.
- `loc-upload-source` (push to main touching localization/) uploads English source, then `para_push_translations.py` force-imports only the translation strings that push changed.
- The Augur has no ParaTranz projects: its translations live only in the repo.
