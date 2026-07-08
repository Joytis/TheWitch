---
name: art-tracker-sync
description: Sync the "The Wicken — Card Art Tracker" Google Sheet with current card data from Docs/card-data/cards.json, preserving artist-edited Art Status and Art Brief columns. Use when the user says "update the art tracker", "sync the tracker", "push card changes to the sheet", or after card design changes when the artist spreadsheet needs refreshing.
---

# Art Tracker Sync

Sync the artist-facing Google Sheet with current card data. The sheet's **Art Status** and **Art Brief (artist notes)** columns are edited by humans and MUST survive the sync — everything else regenerates from `cards.json`.

## Known Drive files

- **Tracker sheet**: search Drive for the newest spreadsheet named `The Wicken — Card Art Tracker` (name may carry a date suffix). Original id: `1PxNKcT3e0kkH24aoyMpH-79eQDGzs29jEzXIunV_k4Q` (created 2026-07-08).
- **Companion doc** (rarely changes): `The Wicken — Artist Brief & Character Overview`, id `1RRWTip7pZ_9sZzbMzHswzS0-jipHUqVYcgUqaLgOTCs`.

## Constraint

The Google Drive MCP has **no update-in-place** — only `create_file`. A sync therefore creates a NEW sheet (dated title) after merging in the live sheet's artist edits, and the user trashes/renames the old one. Never skip the merge step: uploading straight from `cards.json` wipes artists' work.

## Procedure

1. **Refresh card data**: `node Docs/card-data/regen.js` (then `git diff Docs/card-data/cards.json` to see what changed — mention notable changes to the user).
2. **Export the live sheet**: use `mcp__claude_ai_Google_Drive__search_files` to find the newest tracker sheet, then `mcp__claude_ai_Google_Drive__read_file_content` on it. Save the returned content as CSV in the scratchpad, e.g. `<scratchpad>/tracker-live.csv`. If the content comes back as markdown/table rather than CSV, convert it faithfully — column order: Art Status, Card Name, Type, Rarity, Cost, Card Text, Upgrade, Mechanics, Design Notes, Art Brief (artist notes), Placeholder Art Path.
3. **Merge**:
   ```bash
   node Docs/card-data/gen-art-tracker.js <scratchpad>/tracker-new.csv <scratchpad>/tracker-live.csv
   ```
   The script keys rows by Card Name, carries over Art Status (unless `artFinal` is now true → forced `Final`) and Art Brief, sanitizes loc tokens, and prints ADDED/REMOVED card names. **Renamed cards look like one ADDED + one REMOVED — if both lists are non-empty, ask the user whether any pair is a rename, and if so hand-copy the old row's Art Status/Brief into the new row in the CSV before uploading.**
4. **Upload**: `mcp__claude_ai_Google_Drive__create_file` with title `The Wicken — Card Art Tracker (YYYY-MM-DD)`, `contentMimeType: text/csv`, and the merged CSV as `textContent` (Drive auto-converts to a Sheet).
5. **Report**: give the user the new sheet URL, the ADDED/REMOVED summary, and remind them to trash the old sheet (no delete tool available) and re-share the new one with artists.

## Edge cases

- **First run / live sheet missing**: run the script with no second argument (fresh CSV, all Placeholder, empty briefs).
- **`Final` in sheet but `artFinal` false in repo**: the artist marked art done before the repo flag was set. Keep `Final` in the sheet (script does this automatically via status carry-over) and tell the user to set `artFinal: true` in `cards.json` — that flag is preserved by `regen.js`.
- The companion doc only needs regenerating if mechanics/pillars materially change; if so, rewrite it manually (see the original in Drive) — it is prose, not generated.
