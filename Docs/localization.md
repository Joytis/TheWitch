# Localization pipeline (in-repo MT + ParaTranz)

Every English change ships translated in all configured languages: Claude translates in-session with the `loc-sync` skill as part of the same change (see below). On top of that, translators can refine on [ParaTranz](https://paratranz.cn), one project per language, synced with the repo by GitHub Actions. The ParaTranz half is ported from the Downfall team's setup ([lamali292/Downfall](https://github.com/lamali292/Downfall), `.github/scripts/para_*.py`), rewritten on plain `requests`.

## In-session translation (`/loc-sync`)

`tools/localization/loc_sync.py` (`--character Witch|Augur`, default Witch):

| Command | Does |
|---|---|
| `status` | per language: ok / missing / changed / unverified |
| `pending [langs] [--audit] --out f.json` | work list (`file`, `key`, `reason`, `en`, current `tr`) |
| `apply results.json` | merge `{lang: [{file, key, tr}]}` (`tr: null` = existing text verified), write files in English key order, drop keys gone from English, record state; rejects entries failing validation |
| `check [langs] [--warnings]` | validate every translation; exit 1 on errors |
| `glossary <lang> <terms>` | official base-game translation of English terms (from `gamedata/localization/`) |

State lives in `tools/localization/state/<ModId>/<lang>.json`: per key, a hash of the English the translation was written or verified against. A key is **missing** (no translation), **changed** (English edited since), or **unverified** (translation predates the pipeline; `pending --audit` lists these). A translator edit pulled by the nightly download keeps its state, so it is never re-flagged. When English changes, the stale translation is overwritten (here and on ParaTranz).

Validation errors: unbalanced braces/markup, a variable or named formatter (`diff()`, `energyIcons()`, ...) lost, a plural with the wrong word count for the language's SmartFormat rule (`rus`/`pol` 3 words, CJK 1-2, all others 2 - the game throws on a wrong count, e.g. a two-word Russian plural at value 5). Warnings: markup counts, line breaks, plural/cond usage differ from English.

Repo-side translations reach ParaTranz when pushed to `main`: `loc-upload-source` uploads the English source, then `para_push_translations.py` force-imports only the translation strings that push changed. Push before the nightly `loc-download` (08:47 UTC), or it rewrites the files from ParaTranz and your pull conflicts.

## Layout

| Path | Role |
|---|---|
| `TheWitch/TheWitch/localization/eng/*.json` | English source. The only files humans edit. |
| `TheWitch/TheWitch/localization/<lang>/*.json` | Translations. Written by `loc_sync.py apply` (in-session MT) and the nightly download; never hand-edit. |
| `tools/localization/state/<ModId>/<lang>.json` | Per-key English hash each translation matches (loc-sync state). Commit with the translations. |
| `.github/configs/paratranz.json` | `languages` (game code → ParaTranz code) and `projects` (game code → ParaTranz project id). |
| `tools/localization/*.py` | Sync scripts. Need `PARATRANZ_API_KEY` in the environment (ParaTranz → Settings → API Token). |

**Remote paths are pinned.** Files on ParaTranz live at `<remote_dir>/localization/<lang>/<file>` (`remote_dir` in `.github/configs/paratranz.json`, currently `TheWitch`), independent of where the repo keeps them. ParaTranz keys files by full path, so changing that prefix makes the upload *create* a second copy of every file (doubling the string count, translations stranded on the old copies) instead of updating - which is what the 2026-09-20 folder move did. Never change `remote_dir`; if the projects ever drift, run `tools/localization/para_prune_files.py` (dry run by default, `--apply` deletes; the `loc-prune` workflow wraps it) - it removes files outside the expected set but refuses to delete any that hold translations unless `--force`.

Language codes are the game's (`LocManager.Languages`): `zhs deu esp fra ita jpn kor pol ptb rus spa tha tur`. The game loads `res://TheWitch/localization/<lang>/<file>.json` for the active language and falls back to the English table per key, so partial translations ship fine.

## Workflows

| Workflow | Trigger | Script |
|---|---|---|
| `loc-upload-source` | push to `main` touching `localization/`; manual | `para_upload_source.py` — create/update English source files in every project; then (push only) `para_push_translations.py <before> <after>` — force-import the translation strings that push changed |
| `loc-download` | nightly 08:47 UTC; manual | `para_download.py` — write translated strings (stage ≥ 1) in English key order; then `para_status.py` — snapshot per-project string counts, member counts and unanswered join applications to `pages/analytics-data/paratranz.json` (the **Localization** tab of `pages/analytics.html`); commit `Localization: sync translations from ParaTranz` |
| `loc-upload-translations` | manual, pick a language | `para_upload_translations.py` — seed a project from translations already in the repo |
| `loc-prune` | manual (`apply`/`force` inputs) | `para_prune_files.py` — delete files outside `<remote_dir>/localization/<lang>/` (dry run unless `apply`; never touches files holding translations unless `force`) |

Secret required: `PARATRANZ_API_KEY` (repo Settings → Secrets → Actions). The download job pushes with the default `GITHUB_TOKEN` (`contents: write`).

## One-time setup

```bash
PARATRANZ_API_KEY=... python tools/localization/create_projects.py          # all languages in config
PARATRANZ_API_KEY=... python tools/localization/create_projects.py deu fra  # subset
```

`POST /projects` creates each project (public, apply-to-join, one review pass, members can download) and writes the id into `paratranz.json`. Name/logo/one-line blurb (`tagline`) come from the config; push changes with `python tools/localization/update_projects.py`. ParaTranz shows `desc` on the project card, so keep it to one line. The translator rules in `Docs/paratranz-description.md` are published as a **pinned Announcement** in every project by `python tools/localization/update_announcements.py` (creates on first run, updates in place after); pinned announcements render on the project Overview under the stats. Commit the config, add the secret, then run `loc-upload-source` once. A `403` from `POST /projects` means the ParaTranz account cannot create projects yet — create them in the web UI and paste the ids into `projects` instead.

Join applications (`joinMode: 1`, apply-to-join — same as Downfall's projects) are answered on ParaTranz (project → Applications); the dashboard only surfaces them. `/projects/{id}/applications` is undocumented: `status` 2 accepted, 1 rejected, 0 pending, -1 expired unanswered, -2 revoked by the applicant; `operator: null` means nobody has acted on it. Applicants can re-apply once per 24 h.

Adding a language: add its ParaTranz code to `languages`, run `create_projects.py <code>`, add the code to the `loc-upload-translations` workflow choice list.

## Translator-facing rules

Strings are SmartFormat templates. Everything below must survive translation verbatim:

- `{Damage:diff()}`, `{Cards:plural:card|cards}`, `{Var:cond:>0?...|}` tokens (see [localization conventions](../CLAUDE.md#conventions--gotchas))
- `[gold]…[/gold]`, `[green]`, `#y` style markup and `\n` line breaks
- `{IfUpgraded:show:Name+|Name}` upgrade markers

Add such rules to `Docs/paratranz-description.md` and run `update_announcements.py` so every project's pinned announcement carries them.
