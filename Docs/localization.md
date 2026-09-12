# Localization pipeline (ParaTranz)

Translations are crowd-sourced on [ParaTranz](https://paratranz.cn), one project per language, and synced with the repo by GitHub Actions. Ported from the Downfall team's setup ([lamali292/Downfall](https://github.com/lamali292/Downfall), `.github/scripts/para_*.py`), rewritten on plain `requests`.

## Layout

| Path | Role |
|---|---|
| `TheWitch/localization/eng/*.json` | English source. The only files humans edit. |
| `TheWitch/localization/<lang>/*.json` | Translations. **Generated** by the nightly download; never hand-edit (the next sync overwrites). |
| `.github/configs/paratranz.json` | `languages` (game code → ParaTranz code) and `projects` (game code → ParaTranz project id). |
| `tools/localization/*.py` | Sync scripts. Need `PARATRANZ_API_KEY` in the environment (ParaTranz → Settings → API Token). |

Language codes are the game's (`LocManager.Languages`): `zhs deu esp fra ita jpn kor pol ptb rus spa tha tur`. The game loads `res://TheWitch/localization/<lang>/<file>.json` for the active language and falls back to the English table per key, so partial translations ship fine.

## Workflows

| Workflow | Trigger | Script |
|---|---|---|
| `loc-upload-source` | push to `main` touching `localization/eng/`; manual | `para_upload_source.py` — create/update English source files in every project |
| `loc-download` | nightly 08:47 UTC; manual | `para_download.py` — write translated strings (stage ≥ 1) in English key order, commit `Localization: sync translations from ParaTranz` |
| `loc-upload-translations` | manual, pick a language | `para_upload_translations.py` — seed a project from translations already in the repo |

Secret required: `PARATRANZ_API_KEY` (repo Settings → Secrets → Actions). The download job pushes with the default `GITHUB_TOKEN` (`contents: write`).

## One-time setup

```bash
PARATRANZ_API_KEY=... python tools/localization/create_projects.py          # all languages in config
PARATRANZ_API_KEY=... python tools/localization/create_projects.py deu fra  # subset
```

`POST /projects` creates each project (public, apply-to-join, one review pass, members can download) and writes the id into `paratranz.json`. Name/logo/description come from the config plus `Docs/paratranz-description.md` (the translator-facing rules); after editing either, push them to the live projects with `python tools/localization/update_projects.py`. Commit the config, add the secret, then run `loc-upload-source` once. A `403` from `POST /projects` means the ParaTranz account cannot create projects yet — create them in the web UI and paste the ids into `projects` instead.

Adding a language: add its ParaTranz code to `languages`, run `create_projects.py <code>`, add the code to the `loc-upload-translations` workflow choice list.

## Translator-facing rules

Strings are SmartFormat templates. Everything below must survive translation verbatim:

- `{Damage:diff()}`, `{Cards:plural:card|cards}`, `{Var:cond:>0?...|}` tokens (see [localization conventions](../CLAUDE.md#conventions--gotchas))
- `[gold]…[/gold]`, `[green]`, `#y` style markup and `\n` line breaks
- `{IfUpgraded:show:Name+|Name}` upgrade markers

Add such rules to `Docs/paratranz-description.md` and run `update_projects.py` so every project description carries them.
