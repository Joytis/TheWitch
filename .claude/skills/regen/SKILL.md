---
name: regen
description: Regenerate all card/art tracking docs from repo state — rebuilds Docs/card-data/cards.json from the card .cs sources AND regenerates the static art tracker page Docs/art-tracker.html. Use when the user says "/regen", "regen the docs", "update the art tracker", or after adding/editing cards or art assets.
---

# Regen (card database + art tracker)

One command regenerates both tracking docs:

```bash
node Docs/card-data/regen.js
```

- Rebuilds `Docs/card-data/cards.json` from the card `.cs` files + localization (preserves `tested`, `artFinal`, curated `note`/`mechanics`/`role`; auto-clears `TESTED` on mechanically-changed cards).
- Then automatically runs `Docs/art-tracker/regen-art-tracker.js`, which rewrites **`Docs/art-tracker.html`** — the static, self-contained art tracker (GitHub-Pages-servable; images by repo-relative path).

`node Docs/card-data/regen.js --check` = drift check only (exit 1 if stale), writes nothing.

## Art tracker data sources (hand-maintained, git-tracked)

- **`Docs/art-tracker/assets.json`** — non-card assets (Potions / Relics / Familiar Pets / Character & UI). Per asset: `artist` (who's on it) and `done: true` when final art lands.
- **`Docs/art-tracker/card-briefs.json`** — per-card `artist` + `brief`, keyed by card name. Card done-ness comes from `artFinal` in `cards.json` (settable via the card-designs page).
- **Status is derived, never stored**: `done`/`artFinal` → **Done** (green); `artist` set → **In Progress** (yellow); else **Placeholder** (grey).

When the user says "assign <artist> to <asset>" or "mark <asset> done": edit the matching JSON entry, then run the regen command. For card art marked done, set `artFinal: true` in `cards.json` (it is preserved across regens).

The generator also prints `MISSING IMAGE FILES` — cards/assets whose expected PNG doesn't exist yet (the game shows a generic fallback for these). Mention notable ones to the user.

## Report

After running: summarize added/removed/changed cards (regen.js prints them), the done-counts per tab if asked, and any missing-image changes.
