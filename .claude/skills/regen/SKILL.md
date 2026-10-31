---
name: regen
description: Regenerate all card/art tracking docs from repo state — rebuilds Docs/card-data/cards.json from the card .cs sources AND regenerates the static art tracker page pages/art-tracker.html. Use when the user says "/regen", "regen the docs", "update the art tracker", or after adding/editing cards or art assets.
---

# Regen (card database + art tracker)

One command regenerates both tracking docs:

```bash
node Docs/card-data/regen.js
```

- Rebuilds `Docs/card-data/cards.json` from the card `.cs` files + localization (preserves `tested`, `artFinal`, curated `note`/`mechanics`/`role`; auto-clears `TESTED` on mechanically-changed cards).
- Then automatically runs `Docs/art-tracker/regen-art-tracker.js`, which rewrites **`pages/art-tracker.html`** from `Docs/art-tracker/tracker.template.html` (edit the template, never the output). The page renders cards in their real frames via `pages/frame-preview/renderer.js`.
- On GitHub, `.github/workflows/art-tracker.yml` runs the same regen on every push to main that touches cards/art/briefs and commits the result, so the live page never goes stale; a local regen is still how you preview it.

`node Docs/card-data/regen.js --check` = drift check only (exit 1 if stale), writes nothing.

## Art tracker data sources (hand-maintained, git-tracked)

- **`Docs/art-tracker/assets.json`** — non-card assets (Potions / Relics / Familiar Pets / Character & UI). Per asset: `artist`, optional `stage`, and `done: true` when final art lands. Paths are repo-relative (`TheWitch/TheWitch/images/...`).
- **`Docs/art-tracker/card-briefs.json`** — per-card `artist` + `brief` + optional `stage`, keyed by card name (all three editable on the card-designs page). Card done-ness comes from `artFinal` in `cards.json` (the ★ final toggle there).
- **Status**: `done`/`artFinal` → **Final**; else `stage` (`assigned` | `sketching` → Assigned / Sketching); else `artist` set → **Assigned**; else **Unassigned**.

When the user says "assign <artist> to <asset>", "move <card> to sketching" or "mark <asset> done": edit the matching JSON entry (`artist` / `stage` / `done`), then run the regen command. For card art marked done, set `artFinal: true` in `cards.json` (it is preserved across regens).

The generator also prints `MISSING IMAGE FILES` (expected PNG absent) and a `DATA HEALTH` line (orphan briefs, Final art whose in-game `ArtBy` credit disagrees with the tracker's artist, off-shape or tall card art) — the same list the page's health panel shows. Mention notable ones to the user.

To change assignments by hand-editing in a UI, the user runs the "Art Tracker: Edit (local)" VS Code task (edit mode via the card-designs server); otherwise edit the JSON directly.

Useful links to hand out: `pages/art-tracker.html#char=witch&artist=<Name>` is that artist's queue; `pages/frame-preview.html#card=<Card Name>` opens the Frame Previewer on a card.

## Report

After running: summarize added/removed/changed cards (regen.js prints them), the done-counts per tab if asked, and any missing-image changes.
